<script lang="ts">
	import { mdiLogin } from '@mdi/js';
	import { Alert, Button, Card, CardBody, Field, PasswordInput } from '@immich/ui';
	import AdminBrand from './admin-brand.svelte';

	interface Props {
		error?: string;
		onlogin: (password: string) => void;
	}

	let { error = '', onlogin }: Props = $props();
	let password = $state('');

	function submit(e: SubmitEvent) {
		e.preventDefault();
		if (!password) return;
		onlogin(password);
	}
</script>

<Card class="mx-auto mt-24 max-w-sm">
	<CardBody>
		<form class="flex flex-col gap-4" onsubmit={submit}>
			<AdminBrand subtitle="Admin sign-in" />

			<Field label="Admin password">
				<PasswordInput autofocus bind:value={password} />
			</Field>

			{#if error}
				<Alert color="danger">{error}</Alert>
			{/if}

			<Button type="submit" leadingIcon={mdiLogin} fullWidth>Sign in</Button>
		</form>
	</CardBody>
</Card>
