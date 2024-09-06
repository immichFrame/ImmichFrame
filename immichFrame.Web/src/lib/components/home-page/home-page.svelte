<script lang="ts">
	import * as api from '$lib/immichFrameApi';
	import Image from '../elements/image.svelte';
	import { onMount } from 'svelte';
	import ThumbHashImage from '../elements/thumbHashImage.svelte';
	import ImageOverlay from '../elements/imageOverlay.svelte';
	import Clock from '../elements/clock.svelte';
	api.defaults.baseUrl = 'http://localhost:8080/'; // TODO: replace configurable settings

	let imageData: Blob | null;
	let assetData: api.AssetResponseDto | null;

	async function loadImage() {
		let assetRequest = await api.getAsset();

		if (assetRequest.status != 200) {
			assetData = null;
			return;
		}

		assetData = assetRequest.data;
		let imageRequest = await api.getImage(assetRequest.data.id);

		if (imageRequest.status != 200) {
			imageData = null;
			return;
		}
		imageData = imageRequest.data;
	}

	onMount(async () => loadImage());
</script>

<section id="home-page" class="fixed grid h-screen w-screen bg-black">
	{#if imageData && assetData}
		<Clock />
		<ImageOverlay on:next={async () => loadImage()} />
		<ThumbHashImage thumbHash={assetData.thumbhash ?? ''} />
		<Image data={imageData} />
	{:else}
		<!-- maybe show immich logo?-->
		<p class="text-white">LOADING ...</p>
	{/if}
</section>

<style>
	#home-page {
		display: flex;
		justify-content: center;
		align-items: center;
	}
</style>
