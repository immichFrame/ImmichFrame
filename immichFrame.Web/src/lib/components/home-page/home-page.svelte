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
	import Clock from '../elements/clock.svelte';
	import Appointments from '../elements/appointments.svelte';
	import LoadingElement from '../elements/LoadingElement.svelte';

	let assetData: api.AssetResponseDto[];
	let nextAssets: api.AssetResponseDto[];

	const { restartProgress, stopProgress } = slideshowStore;

	let progressBarStatus: ProgressBarStatus;
	let progressBar: ProgressBar;
	let error: boolean;
	let errorMessage: string;

	let unsubscribeRestart: () => void;
	let unsubscribeStop: () => void;

	let cursorVisible = true;
	let timeoutId: number;

	const hideCursor = () => {
		cursorVisible = false;
	};

	const showCursor = () => {
		cursorVisible = true;
		clearTimeout(timeoutId);
		timeoutId = setTimeout(hideCursor, 2000);
	};

	async function loadAssets() {
		try {
			let assetRequest = await api.getAsset();
			if (assetRequest.status != 200) {
				error = true;
				return;
			}

			error = false;
			assetData = assetRequest.data;
		} catch {
			error = true;
		}
	}

	const handleDone = async () => {
		await getNextAssets();
		progressBar.restart(true);
	};

	async function getNextAssets() {
		if (!assetData || assetData.length < 1) {
			await loadAssets();
		}

		if (assetData.length == 0) {
			error = true;
			errorMessage = 'No assets were found! Check your configuration.';
			return;
		}

		let next: api.AssetResponseDto[];
		if (assetData.length > 1 && isHorizontal(assetData[0]) && isHorizontal(assetData[1])) {
			next = assetData.splice(0, 2);
		} else {
			next = assetData.splice(0, 1);
		}
		assetData = [...assetData];
		nextAssets = next;
	}

	function isHorizontal(asset: api.AssetResponseDto) {
		const isFlipped = (orientation: number) => [5, 6, 7, 8].includes(orientation);
		let imageHeight = asset.exifInfo?.exifImageHeight ?? 0;
		let imageWidth = asset.exifInfo?.exifImageWidth ?? 0;
		if (isFlipped(Number(asset.exifInfo?.orientation ?? 0))) {
			[imageHeight, imageWidth] = [imageWidth, imageHeight];
		}
		return imageHeight > imageWidth; // or imageHeight > imageWidth * 1.25;
	}

	onMount(() => {
		window.addEventListener('mousemove', showCursor);
		window.addEventListener('click', showCursor);
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

		getNextAssets();

		return () => {
			window.removeEventListener('mousemove', showCursor);
			window.removeEventListener('click', showCursor);
		};
	});

	onDestroy(() => {
		if (unsubscribeRestart) {
			unsubscribeRestart();
		}

		if (unsubscribeStop) {
			unsubscribeStop();
		}
	});
</script>

<section class="fixed grid h-screen w-screen bg-black" class:cursor-none={!cursorVisible}>
	{#if error}
		<ErrorElement message={errorMessage} />
	{:else if nextAssets}
		<ImageComponent
			showLocation={$configStore.showImageLocation}
			showPhotoDate={$configStore.showPhotoDate}
			showImageDesc={$configStore.showImageDesc}
			sourceAssets={nextAssets}
		/>

		{#if $configStore.showClock}
			<Clock />
		{/if}

		<Appointments />

		<OverlayControls
			on:next={async () => {
				progressBar.restart(false);
				await getNextAssets();
				progressBar.restart(true);
			}}
			on:back={async () => {
				progressBar.restart(false);
				await getNextAssets();
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
			overlayVisible={cursorVisible}
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
		<LoadingElement />
	{/if}
</section>
