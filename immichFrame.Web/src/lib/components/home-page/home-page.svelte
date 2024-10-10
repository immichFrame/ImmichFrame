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
		<div
			class="place-self-center container flex flex-col items-center justify-center px-5 mx-auto my-8 space-y-8 text-center sm:max-w-md"
		>
			<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" class="w-40 h-40 text-gray-400">
				<path
					fill="currentColor"
					d="M256,16C123.452,16,16,123.452,16,256S123.452,496,256,496,496,388.548,496,256,388.548,16,256,16ZM403.078,403.078a207.253,207.253,0,1,1,44.589-66.125A207.332,207.332,0,0,1,403.078,403.078Z"
				></path>
				<rect width="176" height="32" x="168" y="320" fill="currentColor"></rect>
				<polygon
					fill="currentColor"
					points="210.63 228.042 186.588 206.671 207.958 182.63 184.042 161.37 162.671 185.412 138.63 164.042 117.37 187.958 141.412 209.329 120.042 233.37 143.958 254.63 165.329 230.588 189.37 251.958 210.63 228.042"
				></polygon>
				<polygon
					fill="currentColor"
					points="383.958 182.63 360.042 161.37 338.671 185.412 314.63 164.042 293.37 187.958 317.412 209.329 296.042 233.37 319.958 254.63 341.329 230.588 365.37 251.958 386.63 228.042 362.588 206.671 383.958 182.63"
				></polygon>
			</svg>
			<p class="text-3xl text-gray-400">
				Looks like your immich-server is offline or you misconfigured immichFrame, check the
				container logs
			</p>
		</div>
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
