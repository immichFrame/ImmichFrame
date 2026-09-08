<script lang="ts">
	import { Heading, Text } from '@immich/ui';

	interface Props {
		subtitle: string;
	}

	let { subtitle }: Props = $props();
</script>

<div class="flex items-center gap-3">
	<img src="/logo.svg" alt="" class="h-9 w-9 shrink-0" />
	<div class="flex flex-col">
		<Heading tag="h1" size="medium" class="leading-tight">ImmichFrame</Heading>
		<Text color="muted" size="small">{subtitle}</Text>
	</div>
</div>
