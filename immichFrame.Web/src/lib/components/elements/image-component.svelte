<script lang="ts">
	import type { AssetResponseDto } from '$lib/immichFrameApi';
	import Clock from './clock.svelte';
	import Image from './image.svelte';

	export let imageData: Blob;
	export let assetData: AssetResponseDto;

	export let showClock: boolean = true;
	export let showLocation: boolean = true;
	export let showPhotoDate: boolean = true;
	export let showImageDesc: boolean = true;

	let imageUrl: string;
	$: imageUrl = URL.createObjectURL(imageData ?? '');
</script>

{#if showClock}
	<Clock />
{/if}

<Image
	dataUrl={imageUrl}
	asset={assetData}
	{showLocation}
	{showPhotoDate}
	{showImageDesc}
	thumbHash={assetData.thumbhash ?? ''}
/>
