<script lang="ts">
	import { type AssetResponseDto } from '$lib/immichFrameApi';
	export let asset: AssetResponseDto;
</script>

{#if asset.exifInfo?.city}
	<div id="clock-overlay" class="text-white">
		<p class="mt-2 text-lg">{asset.exifInfo?.city ?? ''}</p>
	</div>
{/if}

<style>
	#clock-overlay {
		color: wheat;
		position: absolute;
		background-color: rgba(0, 0, 0, 0.4);
		padding: 2%;
		bottom: 0;
		left: 0;
		z-index: 1;
	}
</style>
