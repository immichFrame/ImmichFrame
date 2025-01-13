<script lang="ts">
	import * as api from '$lib/index';
	import ProgressBar, {
		ProgressBarLocation,
		ProgressBarStatus
	} from '$lib/components/elements/progress-bar.svelte';
	import { slideshowStore } from '$lib/stores/slideshow.store';
	import { clientIdentifierStore, authSecretStore } from '$lib/stores/persist.store';
	import { onDestroy, onMount } from 'svelte';
	import OverlayControls from '../elements/overlay-controls.svelte';
	import ImageComponent from '../elements/image-component.svelte';
	import { configStore } from '$lib/stores/config.store';
	import ErrorElement from '../elements/error-element.svelte';
	import Clock from '../elements/clock.svelte';
	import Appointments from '../elements/appointments.svelte';
	import LoadingElement from '../elements/LoadingElement.svelte';
	import { page } from '$app/stores';

	api.init();

	let assetHistory: api.AssetResponseDto[] = [];
	let assetBacklog: api.AssetResponseDto[] = [];

	let displayingAssets: api.AssetResponseDto[] = $state() as api.AssetResponseDto[];

	const { restartProgress, stopProgress } = slideshowStore;

	let progressBarStatus: ProgressBarStatus = $state(ProgressBarStatus.Playing);
	let progressBar: ProgressBar = $state() as ProgressBar;
	let error: boolean = $state(false);
	let authError: boolean = $state(false);
	let errorMessage: string = $state() as string;

	let unsubscribeRestart: () => void;
	let unsubscribeStop: () => void;

	let cursorVisible = $state(true);
	let timeoutId: number;

	const clientIdentifier = $page.url.searchParams.get('client');
	const authsecret = $page.url.searchParams.get('authsecret');

	if (clientIdentifier && clientIdentifier != $clientIdentifierStore) {
		clientIdentifierStore.set(clientIdentifier);
	}

	if (authsecret && authsecret != $authSecretStore) {
		authSecretStore.set(authsecret);
		api.init();
	}

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
				if (assetRequest.status == 401) {
					authError = true;
				}
				error = true;
				return;
			}

			error = false;
			assetBacklog = assetRequest.data;
		} catch {
			error = true;
		}
	}

	const handleDone = async (previous: boolean = false) => {
		progressBar.restart(false);
		if (previous) await getPreviousAssets();
		else await getNextAssets();
		progressBar.play();
	};

	async function getNextAssets() {
		if (!assetBacklog || assetBacklog.length < 1) {
			await loadAssets();
		}

		if (!error && assetBacklog.length == 0) {
			error = true;
			errorMessage = 'No assets were found! Check your configuration.';
			return;
		}

		let next: api.AssetResponseDto[];
		if (
			$configStore.layout?.trim().toLowerCase() == 'splitview' &&
			assetBacklog.length > 1 &&
			isHorizontal(assetBacklog[0]) &&
			isHorizontal(assetBacklog[1])
		) {
			next = assetBacklog.splice(0, 2);
		} else {
			next = assetBacklog.splice(0, 1);
		}
		assetBacklog = [...assetBacklog];

		if (displayingAssets) {
			// Push to History
			assetHistory.push(...displayingAssets);
		}

		// History max 250 Items
		if (assetHistory.length > 250) {
			assetHistory = assetHistory.splice(assetHistory.length - 250, 250);
		}

		displayingAssets = next;
	}

	function getPreviousAssets() {
		if (!assetHistory || assetHistory.length < 1) {
			return;
		}

		let next: api.AssetResponseDto[];
		if (
			$configStore.layout?.trim().toLowerCase() == 'splitview' &&
			assetHistory.length > 1 &&
			isHorizontal(assetHistory[assetHistory.length - 1]) &&
			isHorizontal(assetHistory[assetHistory.length - 2])
		) {
			next = assetHistory.splice(assetHistory.length - 2, 2);
		} else {
			next = assetHistory.splice(assetHistory.length - 1, 1);
		}

		assetHistory = [...assetHistory];

		// Unshift to Backlog
		if (displayingAssets) {
			assetBacklog.unshift(...displayingAssets);
		}
		displayingAssets = next;
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
		if ($configStore.primaryColor) {
			document.documentElement.style.setProperty('--primary-color', $configStore.primaryColor);
		}

		if ($configStore.secondaryColor) {
			document.documentElement.style.setProperty('--secondary-color', $configStore.secondaryColor);
		}

		if ($configStore.baseFontSize) {
			document.documentElement.style.fontSize = $configStore.baseFontSize;
		}

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
		<ErrorElement {authError} message={errorMessage} />
	{:else if displayingAssets}
		<ImageComponent
			showLocation={$configStore.showImageLocation}
			showPhotoDate={$configStore.showPhotoDate}
			showImageDesc={$configStore.showImageDesc}
			showPeopleDesc={$configStore.showPeopleDesc}
			sourceAssets={displayingAssets}
		/>

		{#if $configStore.showClock}
			<Clock />
		{/if}

		<Appointments />

		<OverlayControls
			on:next={async () => {
				await handleDone();
			}}
			on:back={async () => {
				await handleDone(true);
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
			onDone={handleDone}
		/>
	{:else}
		<LoadingElement />
	{/if}
</section>
