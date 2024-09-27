<script lang="ts">
	import * as api from '$lib/immichFrameApi';
	import Image from '../elements/image.svelte';
	import ThumbHashImage from '../elements/thumbHashImage.svelte';
	import ImageOverlay from '../elements/imageOverlay.svelte';
	import Clock from '../elements/clock.svelte';
	import ProgressBar, { ProgressBarStatus } from '$lib/components/elements/progress-bar.svelte';
	import { slideshowStore } from '$lib/stores/slideshow.store';
	import { onDestroy, onMount } from 'svelte';
	import LocationInfo from '../elements/location-info.svelte';
	import AssetDate from '../elements/asset-date.svelte';
	api.defaults.baseUrl = '/api';

	let imageData: Blob | null;
	let assetData: api.AssetResponseDto | null;

	let imageUrl: string;

	const { restartProgress, stopProgress } = slideshowStore;

	let progressBarStatus: ProgressBarStatus;
	let progressBar: ProgressBar;

	let unsubscribeRestart: () => void;
	let unsubscribeStop: () => void;

	async function loadImage() {
		let assetRequest = await api.getAsset();

		if (assetRequest.status != 200) {
			assetData = null;
			return;
		}

		let imageRequest = await api.getImage(assetRequest.data.id);

		if (imageRequest.status != 200) {
			imageData = null;
			return;
		}

		imageData = imageRequest.data;
		imageUrl = URL.createObjectURL(imageData);
		assetData = assetRequest.data;
	}

	onMount(async () => {
		unsubscribeRestart = restartProgress.subscribe((value) => {
			if (value) {
				progressBar.restart(value);
			}
		});

		unsubscribeStop = stopProgress.subscribe((value) => {
			if (value) {
				progressBar.restart(false);
			}
		});

		await loadImage();
	});

	onDestroy(() => {
		if (unsubscribeRestart) {
			unsubscribeRestart();
		}

		if (unsubscribeStop) {
			unsubscribeStop();
		}
	});

	const handleDone = async () => {
		await loadImage();
		progressBar.restart(true);
	};
</script>

<section id="home-page" class="fixed grid h-screen w-screen bg-black">
	{#if imageData && assetData}
		<Clock />
		<AssetDate asset={assetData} />
		<LocationInfo asset={assetData} />
		<ImageOverlay
			on:next={async () => {
				progressBar.restart(false);
				await loadImage();
				progressBar.restart(true);
			}}
			on:back={async () => {
				progressBar.restart(false);
				await loadImage();
				progressBar.restart(true);
			}}
			on:pause={async () => {
				if (progressBarStatus == ProgressBarStatus.Paused) {
					await progressBar.play();
				} else {
					await progressBar.pause();
				}
			}}
			bind:status={progressBarStatus}
		/>
		<ThumbHashImage thumbHash={assetData.thumbhash ?? ''} />
		<Image dataUrl={imageUrl} />
		<ProgressBar
			autoplay
			hidden={false}
			duration={45}
			bind:this={progressBar}
			bind:status={progressBarStatus}
			on:done={handleDone}
		/>
	{:else}
		<!-- maybe show immich logo?-->
		<div>
			<img
				id="logo"
				class="h-[50vh] sm:h-[50vh] md:h-[40vh] lg:h-[30vh]"
				src="/logo.svg"
				alt="logo"
			/>
			<p class="text-white text-center">LOADING ...</p>
		</div>
	{/if}
</section>

<style>
	#home-page {
		display: flex;
		justify-content: center;
		align-items: center;
	}
</style>
