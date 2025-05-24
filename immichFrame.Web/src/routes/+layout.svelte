<script lang="ts">
	import '../app.css';
	import { onMount } from 'svelte';
	import { useViewportHeight } from '$lib/hooks/useViewportHeight';

	interface Props {
		children?: import('svelte').Snippet;
	}

	let { children }: Props = $props();

	onMount(() => {
		useViewportHeight();
	});
</script>

<link rel="stylesheet" href="/static/custom.css" />

{@render children?.()}
