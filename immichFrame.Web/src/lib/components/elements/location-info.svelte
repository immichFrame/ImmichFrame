<script lang="ts">
	import { type AssetResponseDto } from '$lib/immichFrameApi';
	export let asset: AssetResponseDto;

	let location: string;

	$: location = [asset.exifInfo?.city, asset.exifInfo?.state, asset.exifInfo?.country]
		.filter((x) => x)
		.join(', ');
</script>

{#if location}
	<div
		class="absolute top-0 left-0 z-10 text-primary bg-secondary bg-opacity-40 rounded-br-2xl p-3"
	>
		<p class="mt-2 text-lg">{location}</p>
	</div>
{/if}
