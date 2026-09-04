<script lang="ts">
	import { mdiArrowRight } from '@mdi/js';
	import {
		Alert,
		Button,
		Card,
		CardBody,
		Field,
		HelperText,
		PasswordInput,
		Text
	} from '@immich/ui';
	import AdminBrand from './admin-brand.svelte';

	interface Props {
		error?: string;
		onsetup: (password: string) => void;
	}

	let { error = '', onsetup }: Props = $props();

	let password = $state('');
	let confirmation = $state('');

	let mismatch = $derived(confirmation.length > 0 && confirmation !== password);
	let canSubmit = $derived(password.length > 0 && confirmation === password);

	function submit(e: SubmitEvent) {
		e.preventDefault();
		if (!canSubmit) return;
		onsetup(password);
	}
</script>

<Card class="mx-auto mt-24 max-w-md">
	<CardBody>
		<form class="flex flex-col gap-4" onsubmit={submit}>
			<AdminBrand subtitle="Welcome" />

			<Text color="muted" size="small">
				Choose a password for the admin UI. You will use it to sign in from now on, and you can
				configure your Immich accounts right after.
			</Text>

			<Field label="Admin password">
				<PasswordInput autocomplete="new-password" autofocus bind:value={password} />
			</Field>

			<Field label="Repeat password" invalid={mismatch}>
				<PasswordInput autocomplete="new-password" bind:value={confirmation} />
				{#if mismatch}
					<HelperText color="danger">The passwords do not match.</HelperText>
				{/if}
			</Field>

			{#if error}
				<Alert color="danger">{error}</Alert>
			{/if}

			<Button type="submit" trailingIcon={mdiArrowRight} fullWidth disabled={!canSubmit}>
				Get started
			</Button>
		</form>
	</CardBody>
</Card>
