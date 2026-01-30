<script lang="ts">
	import type { AssetResponseDto } from '$lib/immichFrameApi';
	import { createEventDispatcher } from 'svelte';
	import * as api from '$lib/index';
	import ErrorElement from './error-element.svelte';
	import Asset from './asset.svelte';
	import type AssetComponent from './asset.svelte';
	import LoadingElement from './LoadingElement.svelte';
	import { fade } from 'svelte/transition';
	import { configStore } from '$lib/stores/config.store';
	import { Confetti } from 'svelte-confetti';
	import { slideshowStore } from '$lib/stores/slideshow.store';

	api.init();

	interface Props {
		assets: [string, AssetResponseDto, api.AlbumResponseDto[]][];
		interval?: number;
		error?: boolean;
		loaded?: boolean;
		split?: boolean;
		hasBday?: boolean;
		showLocation?: boolean;
		showPhotoDate?: boolean;
		showImageDesc?: boolean;
		showPeopleDesc?: boolean;
		showTagsDesc?: boolean;
		showAlbumName?: boolean;
		imageFill?: boolean;
		imageZoom?: boolean;
		imagePan?: boolean;
		showInfo: boolean;
		playAudio?: boolean;
	}

	let {
		assets,
		interval = 20,
		error = false,
		loaded = false,
		split = false,
		hasBday = false,
		showLocation = true,
		showPhotoDate = true,
		showImageDesc = true,
		showPeopleDesc = true,
		showTagsDesc = true,
		showAlbumName = true,
		imageFill = false,
		imageZoom = false,
		imagePan = false,
		showInfo = $bindable(false),
		playAudio = false
	}: Props = $props();
	let instantTransition = slideshowStore.instantTransition;
	let transitionDuration = $derived(
		$instantTransition ? 0 : ($configStore.transitionDuration ?? 1) * 1000
	);
	let transitionDelay = $derived($instantTransition ? 0 : transitionDuration / 2 + 25);

	let primaryAssetComponent = $state<AssetComponent | undefined>(undefined);
	let secondaryAssetComponent = $state<AssetComponent | undefined>(undefined);

	export const pause = async () => {
		await primaryAssetComponent?.pause?.();
		await secondaryAssetComponent?.pause?.();
	};

	export const play = async () => {
		await primaryAssetComponent?.play?.();
		await secondaryAssetComponent?.play?.();
	};
</script>

{#if hasBday}
	<div
		class="z-[1000] top-[-50px] fixed l-0 h-dvh-safe w-screen flex justify-center overflow-hidden pointer-events-none"
	>
		<Confetti
			x={[-5, 5]}
			y={[0, 0.1]}
			delay={[500, 2000]}
			infinite
			duration={5000}
			amount={200}
			fallDistance="100vh"
		/>
	</div>
{/if}

{#if error}
	<ErrorElement />
{:else if loaded}
	{#key assets}
		<div
			class="grid absolute h-dvh-safe w-screen"
			out:fade={{ duration: transitionDuration / 2 }}
			in:fade={{ duration: transitionDuration / 2, delay: transitionDelay }}
		>
			{#if split}
				<div class="grid grid-cols-2">
					<div id="image_portrait_1" class="relative grid border-r-2 border-primary h-dvh-safe">
						<Asset
							asset={assets[0]}
							{interval}
							{showLocation}
							{showPhotoDate}
							{showImageDesc}
							{showPeopleDesc}
							{showTagsDesc}
							{showAlbumName}
							{imageFill}
							{imageZoom}
							{imagePan}
							{split}
							{playAudio}
							bind:this={primaryAssetComponent}
							bind:showInfo
						/>
					</div>
					<div id="image_portrait_2" class="relative grid border-l-2 border-primary h-dvh-safe">
						<Asset
							asset={assets[1]}
							{interval}
							{showLocation}
							{showPhotoDate}
							{showImageDesc}
							{showPeopleDesc}
							{showTagsDesc}
							{showAlbumName}
							{imageFill}
							{imageZoom}
							{imagePan}
							{split}
							{playAudio}
							bind:this={secondaryAssetComponent}
							bind:showInfo
						/>
					</div>
				</div>
			{:else}
				<div id="image_default" class="relative grid h-dvh-safe w-screen">
					<Asset
						asset={assets[0]}
						{interval}
						{showLocation}
						{showPhotoDate}
						{showImageDesc}
						{showPeopleDesc}
						{showTagsDesc}
						{showAlbumName}
						{imageFill}
						{imageZoom}
						{imagePan}
						{split}
						{playAudio}
						bind:this={primaryAssetComponent}
						bind:showInfo
					/>
				</div>
			{/if}
		</div>
	{/key}
{:else}
	<div class="grid absolute h-dvh-safe w-screen">
		<LoadingElement />
	</div>
{/if}
