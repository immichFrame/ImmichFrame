<script lang="ts">
	import type { AssetResponseDto } from '$lib/immichFrameApi';
	import Image from './image.svelte';

	export let imageData: Blob;
	export let assetData: AssetResponseDto;

	export let showLocation: boolean = true;
	export let showPhotoDate: boolean = true;
	export let showImageDesc: boolean = true;

	let imageUrl: string;
	$: imageUrl = URL.createObjectURL(imageData ?? '');
</script>

<Image
	dataUrl={imageUrl}
	asset={assetData}
	{showLocation}
	{showPhotoDate}
	{showImageDesc}
	thumbHash={assetData.thumbhash ?? ''}
/>
