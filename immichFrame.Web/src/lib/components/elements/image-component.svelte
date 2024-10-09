<script lang="ts">
	import type { AssetResponseDto } from '$lib/immichFrameApi';
	import AssetDate from './asset-date.svelte';
	import Clock from './clock.svelte';
	import Image from './image.svelte';
	import LocationInfo from './location-info.svelte';

	export let imageData: Blob;
	export let assetData: AssetResponseDto;

	export let showClock: boolean = true;
	export let showLocation: boolean = true;
	export let showPhotoDate: boolean = true;

	let imageUrl: string;
	$: imageUrl = URL.createObjectURL(imageData ?? '');
</script>

{#if showClock}
	<Clock />
{/if}
{#if showPhotoDate}
	<AssetDate asset={assetData} />
{/if}
{#if showLocation}
	<LocationInfo asset={assetData} />
{/if}
<Image dataUrl={imageUrl} thumbHash={assetData.thumbhash ?? ''} />
