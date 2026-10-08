<script lang="ts">
	import { onMount } from 'svelte';
	import { Alert, Button, Heading, Input, Text } from '@immich/ui';
	import PickerTile from './picker-tile.svelte';
	import PickerChip from './picker-chip.svelte';

	export interface PickerItem {
		/** What gets stored in the settings (an ID, or the tag path). */
		key: string;
		name: string;
		subtitle?: string;
		loadImage?: () => Promise<string | null>;
	}

	interface Props {
		title: string;
		/** Shown under the title when something is picked; gets the count. */
		pickedText: (count: number) => string;
		/** Shown under the title when nothing is picked. */
		emptyText: string;
		/** Plural noun for messages, e.g. "albums". */
		noun: string;
		load: () => Promise<{ items: PickerItem[] } | { error: string }>;
		selected: string[];
		onchange: (keys: string[]) => void;
		variant?: 'tile' | 'round-tile' | 'chip';
	}

	let {
		title,
		pickedText,
		emptyText,
		noun,
		load,
		selected,
		onchange,
		variant = 'tile'
	}: Props = $props();

	let items: PickerItem[] = $state([]);
	let loading = $state(true);
	let error = $state('');
	let filter = $state('');

	let selectedSet = $derived(new Set(selected.map((k) => k.toLowerCase())));
	let visible = $derived(
		items.filter((i) => i.name.toLowerCase().includes(filter.trim().toLowerCase()))
	);
	// Selected values Immich no longer returns (deleted albums, renamed tags...). Kept
	// as-is so saving never silently drops them, but surfaced so they can be cleared.
	let missing = $derived(
		selected.filter((k) => !items.some((i) => i.key.toLowerCase() === k.toLowerCase()))
	);

	onMount(reload);

	async function reload() {
		loading = true;
		error = '';
		try {
			const result = await load();
			if ('error' in result) error = result.error;
			else items = result.items;
		} catch {
			error = `Could not load ${noun}. Is the server reachable?`;
		} finally {
			loading = false;
		}
	}

	function toggle(key: string) {
		onchange(
			selectedSet.has(key.toLowerCase())
				? selected.filter((k) => k.toLowerCase() !== key.toLowerCase())
				: [...selected, key]
		);
	}
</script>

<div class="flex flex-col gap-4">
	<div class="flex flex-wrap items-end justify-between gap-3">
		<div>
			<Heading size="medium">{title}</Heading>
			<Text color="muted" size="small">
				{selectedSet.size ? pickedText(selectedSet.size) : emptyText}
			</Text>
		</div>
		<div class="flex items-center gap-2">
			<Button
				size="small"
				variant="outline"
				color="secondary"
				onclick={() => onchange(items.map((i) => i.key))}
				disabled={!items.length}
			>
				Select all
			</Button>
			<Button
				size="small"
				variant="outline"
				color="secondary"
				onclick={() => onchange([])}
				disabled={!selectedSet.size}
			>
				Clear
			</Button>
		</div>
	</div>

	{#if items.length > 8}
		<Input placeholder="Search {noun}" bind:value={filter} />
	{/if}

	{#if loading}
		<Text color="muted" class="py-12 text-center">Loading {noun}…</Text>
	{:else if error}
		<Alert color="danger">
			{error}
			<Button size="small" variant="outline" color="secondary" class="mt-2" onclick={reload}>
				Try again
			</Button>
		</Alert>
	{:else if !items.length}
		<Text color="muted" class="py-12 text-center">This Immich account has no {noun}.</Text>
	{:else}
		{#if variant === 'chip'}
			<div class="flex flex-wrap gap-2" role="group" aria-label={title}>
				{#each visible as item (item.key)}
					<PickerChip
						name={item.name}
						selected={selectedSet.has(item.key.toLowerCase())}
						ontoggle={() => toggle(item.key)}
					/>
				{/each}
			</div>
		{:else}
			<div
				class="grid gap-4 {variant === 'round-tile'
					? 'grid-cols-2 sm:grid-cols-4 lg:grid-cols-5'
					: 'grid-cols-2 sm:grid-cols-3 lg:grid-cols-4'}"
				role="group"
				aria-label={title}
			>
				{#each visible as item (item.key)}
					<PickerTile
						name={item.name}
						subtitle={item.subtitle}
						loadImage={item.loadImage ?? (async () => null)}
						round={variant === 'round-tile'}
						selected={selectedSet.has(item.key.toLowerCase())}
						ontoggle={() => toggle(item.key)}
					/>
				{/each}
			</div>
		{/if}
		{#if !visible.length}
			<Text color="muted" class="py-8 text-center">No {noun} match “{filter}”.</Text>
		{/if}
	{/if}

	{#if missing.length && !loading && !error}
		<Alert color="warning">
			{missing.length}
			{missing.length === 1 ? 'picked item is' : 'picked items are'} no longer available in Immich.
			<Button
				size="small"
				variant="outline"
				color="secondary"
				class="mt-2"
				onclick={() => onchange(selected.filter((k) => !missing.includes(k)))}
			>
				Remove {missing.length === 1 ? 'it' : 'them'}
			</Button>
		</Alert>
	{/if}
</div>
