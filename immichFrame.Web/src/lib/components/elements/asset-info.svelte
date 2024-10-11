<script lang="ts">
	import { type AssetResponseDto } from '$lib/immichFrameApi';
	import AssetDate from './asset-date.svelte';
	import LocationInfo from './location-info.svelte';
	export let asset: AssetResponseDto;
	export let showLocation: boolean;
	export let showPhotoDate: boolean;
</script>

{#if showLocation}
	<LocationInfo {asset} />
{/if}
{#if showPhotoDate}
	<AssetDate {asset} />
{/if}
