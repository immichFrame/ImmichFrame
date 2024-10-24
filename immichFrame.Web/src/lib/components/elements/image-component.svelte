<script lang="ts">
	import type { AssetResponseDto } from '$lib/immichFrameApi';
	import * as api from '$lib/immichFrameApi';
	import ErrorElement from './error-element.svelte';
	import Image from './image.svelte';
	import LoadingElement from './LoadingElement.svelte';
	import { fade } from 'svelte/transition';
	import { configStore } from '$lib/stores/config.store';

	export let sourceAssets: AssetResponseDto[];

	export let showLocation: boolean = true;
	export let showPhotoDate: boolean = true;
	export let showImageDesc: boolean = true;
	let split: boolean = true;

	let transitionDuration = ($configStore.transitionDuration ?? 1) * 1000;

	let error: boolean;
	let loaded: boolean;

	let images: [string, AssetResponseDto][];

	$: loadImages(sourceAssets);

	function getImageUrl(image: Blob) {
		return URL.createObjectURL(image);
	}

	async function loadImages(assets: AssetResponseDto[]) {
		let newImages = [];

		for (let asset of assets) {
			let img = await loadImage(asset);
			newImages.push(img);
		}

		if (assets.length == 2) {
			split = true;
		} else {
			split = false;
		}
		images = newImages;
		loaded = true;
	}

	async function loadImage(a: AssetResponseDto) {
		try {
			let req = await api.getImage(a.id);

			if (req.status != 200) {
				error = true;
				return ['', a] as [string, AssetResponseDto];
			}

			error = false;
			return [getImageUrl(req.data), a] as [string, AssetResponseDto];
		} catch {
			error = true;
		}
		return ['', a] as [string, AssetResponseDto];
	}
</script>

{#if error}
	<ErrorElement />
{:else if loaded}
	{#key images}
		<div class="grid absolute h-full w-full" transition:fade={{ duration: transitionDuration }}>
			{#if split}
				<div class="grid grid-cols-2">
					<div class="grid border-r-2 border-primary">
						<Image multi={true} image={images[0]} {showLocation} {showPhotoDate} {showImageDesc} />
					</div>
					<div class="grid border-l-2 border-primary">
						<Image multi={true} image={images[1]} {showLocation} {showPhotoDate} {showImageDesc} />
					</div>
				</div>
			{:else}
				<div class="grid grid-cols-2">
					<Image image={images[0]} {showLocation} {showPhotoDate} {showImageDesc} />
				</div>
			{/if}
		</div>
	{/key}
{:else}
	<LoadingElement />
{/if}
