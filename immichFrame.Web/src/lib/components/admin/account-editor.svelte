<script lang="ts">
	import { mdiConnection, mdiDelete } from '@mdi/js';
	import { Alert, Button, Card, CardBody, CardHeader, CardTitle } from '@immich/ui';
	import * as adminApi from '$lib/services/admin';
	import type { ServerAccountSettings } from '$lib/immichFrameApi';
	import { accountFields } from './admin-fields';
	import SettingField from './setting-field.svelte';

	interface Props {
		account: ServerAccountSettings;
		index: number;
		onremove: () => void;
	}

	let { account, index, onremove }: Props = $props();

	let testing = $state(false);
	let testResult: { success: boolean; message: string } | null = $state(null);

	async function test() {
		testing = true;
		testResult = null;
		try {
			const res = await adminApi.testAccount($state.snapshot(account) as ServerAccountSettings);
			if (res.status == 200) {
				testResult = { success: res.data.success ?? false, message: res.data.message ?? '' };
			} else {
				testResult = { success: false, message: 'Test request failed' };
			}
		} catch {
			testResult = { success: false, message: 'Test request failed' };
		} finally {
			testing = false;
		}
	}
</script>

<Card>
	<CardHeader>
		<div class="flex items-center justify-between">
			<CardTitle>Account {index + 1}</CardTitle>
			<Button
				leadingIcon={mdiDelete}
				variant="outline"
				color="danger"
				size="small"
				onclick={onremove}
			>
				Remove
			</Button>
		</div>
	</CardHeader>
	<CardBody>
		<div class="grid gap-x-8 sm:grid-cols-2">
			{#each accountFields as field (field.key)}
				<SettingField {field} target={account as Record<string, unknown>} />
			{/each}
		</div>

		<div class="mt-4 flex flex-col gap-3">
			<Button
				leadingIcon={mdiConnection}
				variant="outline"
				color="secondary"
				size="small"
				class="w-fit"
				loading={testing}
				onclick={test}
			>
				{testing ? 'Testing…' : 'Test connection'}
			</Button>
			{#if testResult}
				<Alert color={testResult.success ? 'success' : 'danger'}>{testResult.message}</Alert>
			{/if}
		</div>
	</CardBody>
</Card>
