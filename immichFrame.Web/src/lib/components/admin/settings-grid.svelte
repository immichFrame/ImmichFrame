<script lang="ts">
	import { checkboxesLast, type FieldDef } from './admin-fields';
	import { Text } from '@immich/ui';
	import SettingField from './setting-field.svelte';

	interface Props {
		fields: FieldDef<any>[];
		target: Record<string, unknown>;
		// Optional heading shown above the checkbox group
		checkboxLabel?: string;
	}

	let { fields, target, checkboxLabel }: Props = $props();

	// Checkboxes are grouped after the other fields, with a gap above them
	const ordered = $derived(checkboxesLast(fields));
</script>

<div class="grid gap-x-8 sm:grid-cols-2">
	{#each ordered as field, i (field.key)}
		{#if field.type === 'checkbox' && i > 0 && ordered[i - 1].type !== 'checkbox'}
			{#if checkboxLabel}
				<div class="mt-5 mb-1 sm:col-span-2">
					<Text size="small" color="muted">{checkboxLabel}</Text>
				</div>
			{:else}
				<div class="h-5 sm:col-span-2"></div>
			{/if}
		{/if}
		<SettingField {field} {target} />
	{/each}
</div>
