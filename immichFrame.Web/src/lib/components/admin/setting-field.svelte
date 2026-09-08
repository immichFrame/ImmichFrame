<script lang="ts">
	import { untrack } from 'svelte';
	import type { AnyFieldDef } from './admin-fields';
	import {
		Checkbox,
		Field,
		HelperText,
		Input,
		NumberInput,
		PasswordInput,
		Select,
		Textarea
	} from '@immich/ui';

	interface Props {
		field: AnyFieldDef;
		target: Record<string, unknown>;
	}

	let { field, target }: Props = $props();

	const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

	// Deliberately captures the initial value only; the parent remounts on settings reload.
	// A $derived would re-split on every keystroke and swallow blank lines as you type,
	// so the read is untracked rather than reactive.
	// Guarded with Array.isArray because this line runs for every field type, not just lists.
	let listText = $state(
		untrack(() =>
			Array.isArray(target[field.key]) ? (target[field.key] as string[]).join('\n') : ''
		)
	);

	let invalidGuids: string[] = $state([]);

	let textValue = $derived((target[field.key] as string | null) ?? '');
	let dateValue = $derived(textValue.substring(0, 10));

	function updateList(text: string) {
		listText = text;
		const lines = text
			.split('\n')
			.map((line) => line.trim())
			.filter((line) => line.length > 0);

		if (field.type === 'guid-list') {
			invalidGuids = lines.filter((line) => !guidPattern.test(line));
		}
		target[field.key] = lines;
	}

	function updateDate(raw: string) {
		target[field.key] = raw === '' ? null : new Date(raw).toISOString();
	}
</script>

<Field label={field.label} invalid={invalidGuids.length > 0}>
	{#if field.type === 'checkbox'}
		<Checkbox
			checked={target[field.key] === true}
			onCheckedChange={(checked) => (target[field.key] = checked)}
		/>
	{:else if field.type === 'select'}
		<Select options={field.options ?? []} bind:value={target[field.key] as string} />
	{:else if field.type === 'number'}
		<NumberInput
			bind:value={target[field.key] as number}
			step={field.step ?? '1'}
			min={field.min}
			max={field.max}
		/>
	{:else if field.type === 'password'}
		<PasswordInput
			autocomplete="off"
			value={textValue}
			oninput={(e) => (target[field.key] = e.currentTarget.value)}
		/>
	{:else if field.type === 'date'}
		<Input type="date" value={dateValue} oninput={(e) => updateDate(e.currentTarget.value)} />
	{:else if field.type === 'list' || field.type === 'guid-list'}
		<Textarea rows={3} value={listText} oninput={(e) => updateList(e.currentTarget.value)} />
	{:else}
		<Input
			placeholder={field.placeholder ?? ''}
			value={textValue}
			oninput={(e) => (target[field.key] = e.currentTarget.value)}
		/>
	{/if}

	{#if invalidGuids.length}
		<HelperText color="danger">Not a valid ID: {invalidGuids.join(', ')}</HelperText>
	{:else if field.help}
		<HelperText>{field.help}</HelperText>
	{/if}
</Field>
