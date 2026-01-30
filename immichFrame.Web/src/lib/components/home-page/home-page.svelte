<script lang="ts">
	import * as api from '$lib/index';
	import ProgressBar from '$lib/components/elements/progress-bar.svelte';
	import { slideshowStore } from '$lib/stores/slideshow.store';
	import { clientIdentifierStore, authSecretStore } from '$lib/stores/persist.store';
	import { onDestroy, onMount, setContext, tick } from 'svelte';
	import OverlayControls from '../elements/overlay-controls.svelte';
	import ImageComponent from '../elements/image-component.svelte';
	import type ImageComponentInstance from '../elements/image-component.svelte';
	import { configStore } from '$lib/stores/config.store';
	import ErrorElement from '../elements/error-element.svelte';
	import Clock from '../elements/clock.svelte';
	import Appointments from '../elements/appointments.svelte';
	import LoadingElement from '../elements/LoadingElement.svelte';
	import { page } from '$app/state';
	import { ProgressBarLocation, ProgressBarStatus } from '../elements/progress-bar.types';
	import { isImageAsset, isVideoAsset } from '$lib/constants/asset-type';

	interface ImagesState {
		images: [string, api.AssetResponseDto, api.AlbumResponseDto[]][];
		error: boolean;
		loaded: boolean;
		split: boolean;
		hasBday: boolean;
	}

	api.init();

	// TODO: make this configurable?
	const PRELOAD_ASSETS = 5;

	let assetHistory: api.AssetResponseDto[] = [];
	let assetBacklog: api.AssetResponseDto[] = [];

	let displayingAssets: api.AssetResponseDto[] = $state() as api.AssetResponseDto[];

	const { restartProgress, stopProgress, instantTransition } = slideshowStore;

	let progressBarStatus: ProgressBarStatus = $state(ProgressBarStatus.Playing);
	let progressBar: ProgressBar = $state() as ProgressBar;
	let imageComponent: ImageComponentInstance = $state() as ImageComponentInstance;
	let currentDuration: number = $state($configStore.interval ?? 20);

	let error: boolean = $state(false);
	let infoVisible: boolean = $state(false);
	let authError: boolean = $state(false);
	let errorMessage: string = $state() as string;
	let imagesState: ImagesState = $state({
		images: [],
		error: false,
		loaded: false,
		split: false,
		hasBday: false
	});
	let assetPromisesDict: Record<
		string,
		Promise<[string, api.AssetResponseDto, api.AlbumResponseDto[]]>
	> = {};

	let unsubscribeRestart: () => void;
	let unsubscribeStop: () => void;

	let cursorVisible = $state(true);
	let timeoutId: number;

	const clientIdentifier = page.url.searchParams.get('client');
	const authsecret = page.url.searchParams.get('authsecret');

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

	setContext('close', provideClose);

	async function provideClose() {
		infoVisible = false;
		await progressBar.play();
	}

	const showCursor = () => {
		cursorVisible = true;
		clearTimeout(timeoutId);
		timeoutId = setTimeout(hideCursor, 2000);
	};

	function updateAssetPromises() {
		for (let asset of displayingAssets) {
			if (!(asset.id in assetPromisesDict)) {
				assetPromisesDict[asset.id] = loadAsset(asset);
			}
		}
		for (let i = 0; i < PRELOAD_ASSETS; i++) {
			if (i >= assetBacklog.length) {
				break;
			}
			if (!(assetBacklog[i].id in assetPromisesDict)) {
				assetPromisesDict[assetBacklog[i].id] = loadAsset(assetBacklog[i]);
			}
		}
		// originally just deleted displayingAssets after they were no longer needed
		// but this is more bulletproof to edge cases I think
		for (let key in assetPromisesDict) {
			if (
				!(
					displayingAssets.find((item) => item.id == key) ||
					assetBacklog.find((item) => item.id == key)
				)
			) {
				delete assetPromisesDict[key];
			}
		}
	}

	async function loadAssets() {
		try {
			let assetRequest = await api.getAssets();

			if (assetRequest.status != 200) {
				if (assetRequest.status == 401) {
					authError = true;
				}
				error = true;
				return;
			}

			error = false;
			assetBacklog = assetRequest.data.filter(
				(asset) => isImageAsset(asset) || isVideoAsset(asset)
			);
		} catch {
			error = true;
		}
	}

	let isHandlingAssetTransition = false;
	const handleDone = async (previous: boolean = false, instant: boolean = false) => {
		if (isHandlingAssetTransition) {
			return;
		}
		isHandlingAssetTransition = true;
		try {
			progressBar.restart(false);
			$instantTransition = instant;
			if (previous) await getPreviousAssets();
			else await getNextAssets();
			await tick();
			await imageComponent?.play?.();
			await progressBar.play();
		} finally {
			isHandlingAssetTransition = false;
		}
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
			isImageAsset(assetBacklog[0]) &&
			isImageAsset(assetBacklog[1]) &&
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
		updateAssetPromises();
		imagesState = await loadImages(next);
	}

	async function getPreviousAssets() {
		if (!assetHistory || assetHistory.length < 1) {
			return;
		}

		let next: api.AssetResponseDto[];
		if (
			$configStore.layout?.trim().toLowerCase() == 'splitview' &&
			assetHistory.length > 1 &&
			isImageAsset(assetHistory[assetHistory.length - 1]) &&
			isImageAsset(assetHistory[assetHistory.length - 2]) &&
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
		updateAssetPromises();
		imagesState = await loadImages(next);
	}

	function isHorizontal(asset: api.AssetResponseDto) {
		if (isVideoAsset(asset)) {
			return false;
		}

		const isFlipped = (orientation: number) => [5, 6, 7, 8].includes(orientation);
		let imageHeight = asset.exifInfo?.exifImageHeight ?? 0;
		let imageWidth = asset.exifInfo?.exifImageWidth ?? 0;
		if (isFlipped(Number(asset.exifInfo?.orientation ?? 0))) {
			[imageHeight, imageWidth] = [imageWidth, imageHeight];
		}
		return imageHeight > imageWidth; // or imageHeight > imageWidth * 1.25;
	}

	function hasBirthday(assets: api.AssetResponseDto[]) {
		let today = new Date();
		let hasBday: boolean = false;

		for (let asset of assets) {
			for (let person of asset.people ?? new Array()) {
				let birthdate = new Date(person.birthDate ?? '');
				if (birthdate.getDate() === today.getDate() && birthdate.getMonth() === today.getMonth()) {
					hasBday = true;
					break;
				}
			}
			if (hasBday) break;
		}

		return hasBday;
	}

	function updateCurrentDuration(assets: api.AssetResponseDto[]) {
		const durations = assets
			.map((asset) => getAssetDurationSeconds(asset))
			.filter((value) => value > 0);
		const fallback = $configStore.interval ?? 20;
		currentDuration = durations.length ? Math.max(...durations) : fallback;
	}

	function getAssetDurationSeconds(asset: api.AssetResponseDto) {
		if (isVideoAsset(asset)) {
			const parsed = parseAssetDuration(asset.duration);
			const fallback = $configStore.interval ?? 20;
			return parsed > 0 ? parsed : fallback;
		}
		return $configStore.interval ?? 20;
	}

	function parseAssetDuration(duration?: string | null) {
		if (!duration) {
			return 0;
		}
		const parts = duration.split(':').map((value) => value.trim());
		if (!parts.length) {
			return 0;
		}
		let total = 0;
		let multiplier = 1;
		while (parts.length) {
			const value = parts.pop();
			if (!value) {
				continue;
			}
			const normalized = value.replace(',', '.');
			const numeric = parseFloat(normalized);
			if (Number.isNaN(numeric)) {
				return 0;
			}
			total += numeric * multiplier;
			multiplier *= 60;
		}
		return total;
	}

	async function loadImages(assets: api.AssetResponseDto[]) {
		let newImages = [];
		try {
			updateCurrentDuration(assets);
			for (let asset of assets) {
				let img = await assetPromisesDict[asset.id];
				newImages.push(img);
			}
			return {
				images: newImages,
				error: false,
				loaded: true,
				split: assets.length == 2 && assets.every(isImageAsset),
				hasBday: hasBirthday(assets)
			};
		} catch {
			updateCurrentDuration([]);
			return {
				images: [],
				error: true,
				loaded: false,
				split: false,
				hasBday: false
			};
		}
	}

	async function loadAsset(assetResponse: api.AssetResponseDto) {
		let req = await api.getAsset(assetResponse.id, {
			clientIdentifier: $clientIdentifierStore,
			assetType: assetResponse.type
		});
		let album: api.AlbumResponseDto[] | null = null;
		if ($configStore.showAlbumName) {
			let albumReq = await api.getAlbumInfo(assetResponse.id, {
				clientIdentifier: $clientIdentifierStore
			});
			album = albumReq.data;
		}

		if (req.status != 200 || ($configStore.showAlbumName && album == null)) {
			return ['', assetResponse, []] as [string, api.AssetResponseDto, api.AlbumResponseDto[]];
		}

		// if the people array is already populated, there is no need to call the API again
		if ($configStore.showPeopleDesc && (assetResponse.people ?? []).length == 0) {
			let assetInfoRequest = await api.getAssetInfo(assetResponse.id, {
				clientIdentifier: $clientIdentifierStore
			});
			assetResponse.people = assetInfoRequest.data.people;
			// assetResponse.exifInfo = assetInfoRequest.data.exifInfo;
		}

		return [getObjectUrl(req.data), assetResponse, album] as [
			string,
			api.AssetResponseDto,
			api.AlbumResponseDto[]
		];
	}

	function getObjectUrl(image: Blob) {
		return URL.createObjectURL(image);
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
				imageComponent?.play?.();
			}
		});

		unsubscribeStop = stopProgress.subscribe((value) => {
			if (value) {
				progressBar.restart(false);
				imageComponent?.pause?.();
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

<section class="fixed grid h-dvh-safe w-screen bg-black" class:cursor-none={!cursorVisible}>
	{#if error}
		<ErrorElement {authError} message={errorMessage} />
	{:else if displayingAssets}
		<div class="absolute h-screen w-screen">
			<ImageComponent
				showLocation={$configStore.showImageLocation}
				interval={$configStore.interval}
				showPhotoDate={$configStore.showPhotoDate}
				showImageDesc={$configStore.showImageDesc}
				showPeopleDesc={$configStore.showPeopleDesc}
				showTagsDesc={$configStore.showTagsDesc}
				showAlbumName={$configStore.showAlbumName}
				{...imagesState}
				imageFill={$configStore.imageFill}
				imageZoom={$configStore.imageZoom}
				imagePan={$configStore.imagePan}
				bind:this={imageComponent}
				on:ended={() => handleDone(false, false)}
				bind:showInfo={infoVisible}
				playAudio={$configStore.playAudio}
			/>
		</div>

		{#if $configStore.showClock}
			<Clock />
		{/if}

		<Appointments />

		<OverlayControls
			next={async () => {
				await handleDone(false, true);
				infoVisible = false;
			}}
			back={async () => {
				await handleDone(true, true);
				infoVisible = false;
			}}
			pause={async () => {
				infoVisible = false;
				if (progressBarStatus == ProgressBarStatus.Paused) {
					await imageComponent?.play?.();
					await progressBar.play();
				} else {
					await imageComponent?.pause?.();
					await progressBar.pause();
				}
			}}
			showInfo={async () => {
				if (infoVisible) {
					infoVisible = false;
					await imageComponent?.play?.();
					await progressBar.play();
				} else {
					infoVisible = true;
					await imageComponent?.pause?.();
					await progressBar.pause();
				}
			}}
			bind:status={progressBarStatus}
			bind:infoVisible
			overlayVisible={cursorVisible}
		/>

		<ProgressBar
			autoplay
			duration={currentDuration}
			hidden={!$configStore.showProgressBar}
			location={ProgressBarLocation.Bottom}
			bind:this={progressBar}
			bind:status={progressBarStatus}
			onDone={handleDone}
		/>
	{:else}
		<LoadingElement />
	{/if}
</section>
