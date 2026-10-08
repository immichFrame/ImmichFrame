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
	import {
		AdminUiState,
		type ServerAccountSettings,
		type ServerSettings
	} from '$lib/immichFrameApi';
	import { adminPasswordStore } from '$lib/stores/admin.store';
	import { generalSections } from './admin-fields';
	import AdminBrand from './admin-brand.svelte';
	import AdminLogin from './admin-login.svelte';
	import AdminSetup from './admin-setup.svelte';
	import SettingsSection from './settings-section.svelte';
	import SettingField from './setting-field.svelte';
	import AccountEditor from './account-editor.svelte';
	import ContentPicker from './content-picker.svelte';

	// The "Saved" notice dismisses itself after this long; it can also be closed by hand.
	const SAVED_NOTICE_MS = 2 * 60 * 1000;

	type Tab = 'content' | 'settings';
	type PageState = 'loading' | 'setup' | 'disabled' | 'login' | 'editor' | 'error';

	let pageState: PageState = $state('loading');
	let settings: ServerSettings = $state({ General: {}, Accounts: [] });
	let loginError = $state('');
	let setupError = $state('');
	let saving = $state(false);
	let saveError = $state('');
	let saveSuccess = $state(false);
	let saveWarnings: string[] = $state([]);

	let tab: Tab = $state('content');
	let contentAccount = $state(0);

	// What the server knows about each account (url + credentials) as of the last load/save.
	// The content pickers query the server by account index, so they only work for
	// accounts that still match this.
	let savedAccountKeys: string[] = $state([]);
	// Bumped on load/save so the pickers refetch from Immich
	let contentVersion = $state(0);

	function accountKey(a: ServerAccountSettings) {
		return [a.immichServerUrl, a.apiKey, a.apiKeyFile].join('|');
	}

	function markSaved() {
		savedAccountKeys = (settings.Accounts ?? []).map(accountKey);
		contentVersion++;
	}

	function accountLabel(a: ServerAccountSettings, index: number) {
		return a.name?.trim() || `Account ${index + 1}`;
	}

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
				markSaved();
				tab = settings.Accounts?.length ? 'content' : 'settings';
				contentAccount = 0;
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
				markSaved();
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
			<div class="mx-auto {tab === 'content' ? 'max-w-5xl' : 'max-w-3xl'}">
				<div class="mb-4 flex gap-2" role="tablist">
					{#each [['content', 'What to show'], ['settings', 'Settings']] as [id, label] (id)}
						<Button
							role="tab"
							aria-selected={tab === id}
							size="small"
							shape="round"
							variant={tab === id ? 'filled' : 'ghost'}
							color={tab === id ? 'primary' : 'secondary'}
							onclick={() => (tab = id as Tab)}
						>
							{label}
						</Button>
					{/each}
				</div>

				{#if saveSuccess}
					<Alert
						color="success"
						title="Saved"
						class="mb-4"
						closable
						duration={SAVED_NOTICE_MS}
						onClose={() => (saveSuccess = false)}
					>
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

				{#if tab === 'content'}
					{@const accounts = settings.Accounts ?? []}
					{#if !accounts.length}
						<Card>
							<CardBody class="text-center">
								<Text color="muted" class="mb-4">
									Connect an Immich account first, then you can pick what to show here.
								</Text>
								<Button onclick={() => (tab = 'settings')}>Go to settings</Button>
							</CardBody>
						</Card>
					{:else}
						{@const index = Math.min(contentAccount, accounts.length - 1)}
						<div class="flex flex-col gap-4">
							{#if accounts.length > 1}
								<div class="flex flex-wrap gap-2">
									{#each accounts as account, i (i)}
										<Button
											size="small"
											variant={i === index ? 'filled' : 'outline'}
											color={i === index ? 'primary' : 'secondary'}
											onclick={() => (contentAccount = i)}
										>
											{accountLabel(account, i)}
										</Button>
									{/each}
								</div>
							{/if}
							{#if savedAccountKeys[index] !== accountKey(accounts[index])}
								<Alert color="warning">
									Save your changes to connect this account, then you can pick what to show from it.
								</Alert>
							{:else}
								{#key `${index}:${contentVersion}`}
									<ContentPicker account={accounts[index]} accountIndex={index} />
								{/key}
							{/if}
						</div>
					{/if}
				{:else}
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
				{/if}
			</div>
		{/if}
	</div>
</AppShell>
