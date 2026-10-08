<script lang="ts">
	import { onMount } from 'svelte';
	import { mdiCheckCircle, mdiImageOffOutline } from '@mdi/js';
	import { Icon, Text } from '@immich/ui';

	interface Props {
		name: string;
		subtitle?: string;
		/** Resolves to an object URL for the cover picture, or null when there is none. */
		loadImage: () => Promise<string | null>;
		/** Square cover with a round crop, for faces. */
		round?: boolean;
		selected: boolean;
		ontoggle: () => void;
	}

	let { name, subtitle, loadImage, round = false, selected, ontoggle }: Props = $props();

	let src: string | null = $state(null);
	let loaded = $state(false);

	onMount(() => {
		let url: string | null = null;
		let disposed = false;
		loadImage().then((result) => {
			if (disposed && result) URL.revokeObjectURL(result);
			else url = src = result;
			loaded = true;
		});
		return () => {
			disposed = true;
			if (url) URL.revokeObjectURL(url);
		};
	});
</script>

<button
	type="button"
	role="checkbox"
	aria-checked={selected}
	class="group flex flex-col overflow-hidden rounded-2xl border-2 bg-subtle text-left transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary {selected
		? 'border-primary shadow-md'
		: 'border-transparent hover:border-gray-400/60'}"
	onclick={ontoggle}
>
	<div
		class="relative w-full overflow-hidden bg-gray-500/15 {round
			? 'flex aspect-square items-center justify-center p-4'
			: 'aspect-4/3'}"
	>
		<div class={round ? 'aspect-square w-full overflow-hidden rounded-full' : 'h-full w-full'}>
			{#if src}
				<img
					{src}
					alt=""
					class="h-full w-full object-cover transition group-hover:scale-105 {selected
						? ''
						: 'opacity-90'}"
				/>
			{:else if loaded}
				<div class="flex h-full w-full items-center justify-center text-gray-500">
					<Icon icon={mdiImageOffOutline} size="40" />
				</div>
			{/if}
		</div>
		{#if selected}
			<div class="absolute top-2 right-2 rounded-full bg-white text-primary">
				<Icon icon={mdiCheckCircle} size="28" />
			</div>
		{/if}
	</div>
	<div class="flex flex-col gap-0.5 p-3">
		<span class="truncate font-medium">{name}</span>
		{#if subtitle}
			<Text color="muted" size="small">{subtitle}</Text>
		{/if}
	</div>
</button>
