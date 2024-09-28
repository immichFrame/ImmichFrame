<script lang="ts">
	import * as api from '$lib/immichFrameApi';
	import ProgressBar, {
		ProgressBarLocation,
		ProgressBarStatus
	} from '$lib/components/elements/progress-bar.svelte';
	import { slideshowStore } from '$lib/stores/slideshow.store';
	import { onDestroy, onMount } from 'svelte';
	import OverlayControls from '../elements/overlay-controls.svelte';
	import Image2 from '../elements/image2.svelte';
	api.defaults.baseUrl = '/api';

	let imageData: Blob | null;
	let assetData: api.AssetResponseDto | null;

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

<section class="fixed items-center justify-center grid h-screen w-screen bg-black">
	{#if imageData && assetData}
		<Image2 showClock={true} showLocation={true} showAssetInfo={true} {assetData} {imageData} />

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
			hidden={false}
			location={ProgressBarLocation.Bottom}
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
		</div>
	{/if}
</section>
