<script lang="ts">
	import * as api from '$lib/immichFrameApi';
	import Image from '../elements/Image.svelte';
	import { onMount } from 'svelte';
	api.defaults.baseUrl = 'https://localhost:7018/'; // TODO: replace configurable settings

	let imageData: Blob;
	let assetData: api.AssetResponseDto;

	async function loadImage() {
		let assetRequest = await api.getAsset();
		assetData = assetRequest.data;
		let imageRequest = await api.getImage(assetRequest.data.id);
		imageData = imageRequest.data;
	}

	onMount(async () => loadImage());
</script>

<button on:click={async () => loadImage()}>Next</button>
<section id="home-page" class="fixed grid h-screen w-screen bg-black">
	{#if imageData && assetData}
		<Image thumbHash={assetData.thumbhash ?? ''} data={imageData} />
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
