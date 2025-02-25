<script lang="ts">
	import '../app.css';
	import { base } from '$app/paths';
	interface Props {
		children?: import('svelte').Snippet;
	}

	let { children }: Props = $props();
</script>

<link rel="stylesheet" href="/static/custom.css" />

<base href={base} />
{@render children?.()}
