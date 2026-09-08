<script lang="ts">
	import { onMount } from 'svelte';
	import { get } from 'svelte/store';
	import { mdiContentSave, mdiLogout, mdiPlus } from '@mdi/js';
	import {
		Alert,
		AppShell,
		AppShellHeader,
		Button,
		Card,
		CardBody,
		Code,
		Heading,
		IconButton,
		Text
	} from '@immich/ui';
	import * as adminApi from '$lib/services/admin';
	import { AdminUiState, type ServerSettings } from '$lib/immichFrameApi';
	import { adminPasswordStore } from '$lib/stores/admin.store';
	import { generalSections } from './admin-fields';
	import AdminBrand from './admin-brand.svelte';
	import AdminLogin from './admin-login.svelte';
	import AdminSetup from './admin-setup.svelte';
	import SettingsSection from './settings-section.svelte';
	import SettingField from './setting-field.svelte';
	import AccountEditor from './account-editor.svelte';

	type PageState = 'loading' | 'setup' | 'disabled' | 'login' | 'editor' | 'error';

	let pageState: PageState = $state('loading');
	let settings: ServerSettings = $state({ General: {}, Accounts: [] });
	let loginError = $state('');
	let setupError = $state('');
	let saving = $state(false);
	let saveError = $state('');
	let saveSuccess = $state(false);
	let saveWarnings: string[] = $state([]);

	// Remount account editors after (re)loading settings so their local list texts reset
	let accountsVersion = $state(0);

	onMount(loadStatus);

	async function loadStatus() {
		try {
			const res = await adminApi.getStatus();
			if (res.status != 200) {
				pageState = 'error';
				return;
			}
			if (res.data.state === AdminUiState.Setup) {
				pageState = 'setup';
				return;
			}
			if (res.data.state === AdminUiState.Disabled) {
				pageState = 'disabled';
				return;
			}
			if (get(adminPasswordStore)) {
				await loadSettings();
			} else {
				pageState = 'login';
			}
		} catch {
			pageState = 'error';
		}
	}

	// Claims the instance, then signs in with the password just chosen so the user
	// lands straight in the editor to add their first account.
	async function setup(password: string) {
		setupError = '';
		try {
			const res = await adminApi.setup(password);
			if (res.status != 200) {
				setupError = 'Setup failed. Check the server logs and try again.';
				return;
			}
			adminPasswordStore.set(password);
			await loadSettings();
		} catch {
			setupError = 'Setup failed. Is the server reachable?';
		}
	}

	async function loadSettings() {
		try {
			const res = await adminApi.getSettings();
			if (res.status == 200) {
				settings = { General: res.data.General ?? {}, Accounts: res.data.Accounts ?? [] };
				accountsVersion++;
				pageState = 'editor';
				loginError = '';
			} else {
				adminPasswordStore.set(null);
				loginError = 'Could not authenticate. Check the admin password.';
				pageState = 'login';
			}
		} catch {
			pageState = 'error';
		}
	}

	function login(password: string) {
		adminPasswordStore.set(password);
		loadSettings();
	}

	function logout() {
		adminPasswordStore.set(null);
		loginError = '';
		pageState = 'login';
	}

	function addAccount() {
		settings.Accounts = [...(settings.Accounts ?? []), {}];
	}

	function removeAccount(index: number) {
		settings.Accounts = (settings.Accounts ?? []).filter((_, i) => i != index);
		accountsVersion++;
	}

	async function save() {
		saving = true;
		saveError = '';
		saveSuccess = false;
		saveWarnings = [];
		try {
			const res = await adminApi.updateSettings($state.snapshot(settings) as ServerSettings);
			if (res.status == 200) {
				saveSuccess = true;
				saveWarnings = res.data.warnings ?? [];
			} else if (res.status == 401) {
				adminPasswordStore.set(null);
				loginError = 'Session expired. Sign in again.';
				pageState = 'login';
			} else {
				const data = res.data as { detail?: string } | string | null;
				saveError =
					(typeof data === 'object' && data?.detail) ||
					(typeof data === 'string' && data) ||
					'Saving failed.';
			}
		} catch {
			saveError = 'Saving failed. Is the server reachable?';
		} finally {
			saving = false;
		}
	}
</script>

<AppShell class="bg-light px-4">
	{#if pageState === 'editor'}
		<AppShellHeader>
			<div class="w-full p-4">
				<div class="mx-auto flex w-full max-w-3xl items-center justify-between gap-4">
					<AdminBrand subtitle="Server settings" />

					<div class="flex items-center gap-2">
						<Button leadingIcon={mdiContentSave} loading={saving} onclick={save}>
							{saving ? 'Saving…' : 'Save'}
						</Button>
						<IconButton
							icon={mdiLogout}
							variant="outline"
							color="secondary"
							aria-label="Sign out"
							title="Sign out"
							onclick={logout}
						/>
					</div>
				</div>
			</div>
		</AppShellHeader>
	{/if}
	<div class="p-4 pb-24">
		{#if pageState === 'loading'}
			<Text color="muted" class="block pt-24 text-center">Loading…</Text>
		{:else if pageState === 'error'}
			<Text color="muted" class="block pt-24 text-center">
				Could not reach the ImmichFrame server. Check the container logs and reload this page.
			</Text>
		{:else if pageState === 'setup'}
			<AdminSetup error={setupError} onsetup={setup} />
		{:else if pageState === 'disabled'}
			<Card class="mx-auto mt-24 max-w-lg">
				<CardBody class="text-center">
					<Heading size="large" class="mb-4">Admin UI is disabled</Heading>
					<Text color="muted">
						This instance is already configured but has no admin password. Set the
						<Code>IMMICHFRAME_ADMIN_PASSWORD</Code> environment variable and restart the container to
						get in.
					</Text>
				</CardBody>
			</Card>
		{:else if pageState === 'login'}
			<AdminLogin error={loginError} onlogin={login} />
		{:else}
			<div class="mx-auto max-w-3xl">
				{#if saveSuccess}
					<Alert color="success" title="Saved" class="mb-4">
						Changes are applied live. Slideshow devices pick up display changes on their next
						reload; weather and calendars refresh within ~15 minutes.
						{#if saveWarnings.length}
							<ul class="mt-2 list-disc pl-5">
								{#each saveWarnings as warning (warning)}
									<li>{warning}</li>
								{/each}
							</ul>
						{/if}
					</Alert>
				{/if}
				{#if saveError}
					<Alert color="danger" class="mb-4">{saveError}</Alert>
				{/if}

				<div class="flex flex-col gap-4">
					{#each generalSections as section (section.title)}
						<SettingsSection title={section.title} open={section.title === 'Display'}>
							<div class="grid gap-x-8 sm:grid-cols-2">
								{#each section.fields as field (field.key)}
									<SettingField {field} target={settings.General ?? {}} />
								{/each}
							</div>
						</SettingsSection>
					{/each}

					<SettingsSection title="Accounts">
						<div class="flex flex-col gap-4">
							{#key accountsVersion}
								{#each settings.Accounts ?? [] as account, index (index)}
									<AccountEditor {account} {index} onremove={() => removeAccount(index)} />
								{/each}
							{/key}
							{#if !(settings.Accounts ?? []).length}
								<Text color="muted" size="small">
									No accounts configured yet — the slideshow has nothing to show. Add your first
									Immich account below.
								</Text>
							{/if}
							<Button
								leadingIcon={mdiPlus}
								variant="outline"
								color="secondary"
								size="small"
								class="w-fit"
								onclick={addAccount}
							>
								Add account
							</Button>
						</div>
					</SettingsSection>
				</div>
			</div>
		{/if}
	</div>
</AppShell>
