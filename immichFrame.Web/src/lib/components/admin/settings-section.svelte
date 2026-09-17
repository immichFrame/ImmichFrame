<script lang="ts">
	import type { Snippet } from 'svelte';
	import { Card, CardBody, CardHeader, CardTitle } from '@immich/ui';

	interface Props {
		title: string;
		open?: boolean;
		children?: Snippet;
	}

	let { title, open = true, children }: Props = $props();
</script>

<Card expandable expanded={open}>
	<CardHeader>
		<CardTitle>{title}</CardTitle>
	</CardHeader>
	<CardBody>
		{@render children?.()}
	</CardBody>
</Card>
