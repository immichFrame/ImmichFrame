<script lang="ts">
	import { mdiCheck } from '@mdi/js';
	import { Icon } from '@immich/ui';

	interface Props {
		name: string;
		selected: boolean;
		ontoggle: () => void;
	}

	let { name, selected, ontoggle }: Props = $props();
</script>

<button
	type="button"
	role="checkbox"
	aria-checked={selected}
	class="flex items-center gap-1.5 rounded-full border-2 px-4 py-2 text-left transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary {selected
		? 'border-primary bg-primary text-white'
		: 'border-transparent bg-subtle hover:border-gray-400/60'}"
	onclick={ontoggle}
>
	{#if selected}<Icon icon={mdiCheck} size="18" />{/if}
	<span class="break-all">{name}</span>
</button>
