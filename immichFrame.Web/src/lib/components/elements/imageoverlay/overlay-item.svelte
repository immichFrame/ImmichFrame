<script lang="ts">
	import Icon from '../icon.svelte';
	import { mdiTestTube } from '@mdi/js';

	interface Props {
		icon?: string;
		header: string;
		items?: string[];
	}
	let { icon = mdiTestTube, header, items = [] }: Props = $props();
</script>

<div class="text-xl text-primary text-left w-fit flex flex-col">
	<span class="flex gap-2 items-center"><Icon path={icon} class="info-icon" />{header}</span>
	{#if items.length > 0}
		<span>{items.map((x) => x).join(', ')}</span>
	{/if}
</div>
