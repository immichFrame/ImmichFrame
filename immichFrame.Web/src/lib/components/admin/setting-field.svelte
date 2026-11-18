<script lang="ts">
	import { untrack } from 'svelte';
	import type { AnyFieldDef } from './admin-fields';
	import { CUSTOM_FORMAT, formatExample } from './format-presets';
	import {
		Checkbox,
		Field,
		HelperText,
		Input,
		NumberInput,
		PasswordInput,
		Select,
		Text,
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

	// 'format' fields: a list of examples, or a pattern typed by hand. Custom holds its own
	// draft: the settings only receive a pattern date-fns can format, so an invalid draft stays
	// on screen for correcting but can never be saved and break the frame's clock or date.
	const uid = $props.id();
	let presets = $derived(field.presets ?? []);
	let customFormat = $state(false);
	let customDraft = $state('');

	// Re-derive the mode whenever the settings document is replaced (e.g. a reload), but not
	// while typing: a typed pattern that happens to match a preset keeps Custom open.
	$effect.pre(() => {
		const doc = target;
		untrack(() => {
			const saved = (doc[field.key] as string | null) ?? '';
			customDraft = saved;
			customFormat = field.type === 'format' && !presets.includes(saved);
		});
	});

	let formatOptions = $derived([
		...presets.map((pattern) => ({
			value: pattern,
			label: `${formatExample(pattern, target.language, field.localized === true) ?? pattern}  (${pattern})`
		})),
		{ value: CUSTOM_FORMAT, label: 'Custom…' }
	]);
	let formatPreview = $derived(
		customDraft === '' ? '' : formatExample(customDraft, target.language, field.localized === true)
	);

	function selectFormat(value: string) {
		customFormat = value === CUSTOM_FORMAT;
		if (customFormat) {
			customDraft = textValue;
		} else {
			target[field.key] = value;
		}
	}

	function updateCustom(value: string) {
		customDraft = value;
		if (value === '' || formatExample(value, target.language, field.localized === true) !== null) {
			target[field.key] = value;
		}
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
	{:else if field.type === 'format'}
		<Select
			options={formatOptions}
			value={customFormat ? CUSTOM_FORMAT : textValue}
			onChange={selectFormat}
		/>
		{#if customFormat}
			<div class="mt-2">
				<Field label="Custom pattern" invalid={formatPreview === null}>
					<Input
						placeholder={presets[0] ?? ''}
						value={customDraft}
						aria-invalid={formatPreview === null}
						aria-describedby="{uid}-format-help"
						oninput={(e) => updateCustom(e.currentTarget.value)}
					/>
					<!-- Text, not HelperText: HelperText renders in the Field's slot, outside this live region. -->
					<div id="{uid}-format-help" aria-live="polite">
						{#if formatPreview === null}
							<Text color="danger" size="small" class="pt-1">
								Not a valid date-fns pattern. It isn't saved until it's valid.
							</Text>
						{:else}
							<Text color="muted" size="small" class="pt-1">
								{formatPreview ? `Shows as: ${formatPreview}. ` : ''}Patterns follow date-fns
								(date-fns.org/docs/format).
							</Text>
						{/if}
					</div>
				</Field>
			</div>
		{/if}
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
