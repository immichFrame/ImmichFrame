<script lang="ts">
	import * as api from '$lib/immichFrameApi';
	import ProgressBar, {
		ProgressBarLocation,
		ProgressBarStatus
	} from '$lib/components/elements/progress-bar.svelte';
	import { slideshowStore } from '$lib/stores/slideshow.store';
	import { onDestroy, onMount } from 'svelte';
	import OverlayControls from '../elements/overlay-controls.svelte';
	import ImageComponent from '../elements/image-component.svelte';
	import { configStore } from '$lib/stores/config.store';
	import ErrorElement from '../elements/error-element.svelte';

	let imageData: Blob | null;
	let assetData: api.AssetResponseDto | null;

	const { restartProgress, stopProgress } = slideshowStore;

	let progressBarStatus: ProgressBarStatus;
	let progressBar: ProgressBar;
	let error: boolean;

	let unsubscribeRestart: () => void;
	let unsubscribeStop: () => void;

	async function loadImage() {
		try {
			let assetRequest = await api.getAsset();
			console.log(assetRequest.status);
			if (assetRequest.status != 200) {
				assetData = null;
				error = true;
				return;
			}

			let imageRequest = await api.getImage(assetRequest.data.id);

			console.log(imageRequest.status);

			if (imageRequest.status != 200) {
				console.log(imageRequest);
				error = true;
				imageData = null;
				return;
			}

			error = false;
			imageData = imageRequest.data;
			assetData = assetRequest.data;
		} catch {
			error = true;
		}
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

<section class="fixed grid h-screen w-screen bg-black">
	{#if error}
		<ErrorElement />
	{:else if imageData && assetData}
		<ImageComponent
			showClock={$configStore.showClock}
			showLocation={$configStore.showImageLocation}
			showPhotoDate={$configStore.showPhotoDate}
			{assetData}
			{imageData}
		/>

		<OverlayControls
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

		<ProgressBar
			autoplay
			duration={$configStore.interval}
			hidden={false}
			location={ProgressBarLocation.Bottom}
			bind:this={progressBar}
			bind:status={progressBarStatus}
			on:done={handleDone}
		/>
	{:else}
		<div class="place-self-center">
			<img
				id="logo"
				class="h-[50vh] sm:h-[50vh] md:h-[40vh] lg:h-[30vh]"
				src="/logo.svg"
				alt="logo"
			/>
		</div>
	{/if}
</section>
