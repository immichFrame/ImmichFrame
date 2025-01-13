<script lang="ts">
	import '../app.css';
	interface Props {
		children?: import('svelte').Snippet;
	}

	let { children }: Props = $props();
</script>

<link rel="stylesheet" href="/static/custom.css" />

{@render children?.()}
