<script lang="ts">
	import * as api from '$lib/index';
	import ProgressBar from '$lib/components/elements/progress-bar.svelte';
	import { slideshowStore } from '$lib/stores/slideshow.store';
	import { clientIdentifierStore, authSecretStore } from '$lib/stores/persist.store';
	import { onDestroy, onMount, setContext, tick } from 'svelte';
	import OverlayControls from '../elements/overlay-controls.svelte';
	import AssetComponent from '../elements/asset-component.svelte';
	import type AssetComponentInstance from '../elements/asset-component.svelte';
	import { configStore } from '$lib/stores/config.store';
	import ErrorElement from '../elements/error-element.svelte';
	import Clock from '../elements/clock.svelte';
	import Appointments from '../elements/appointments.svelte';
	import LoadingElement from '../elements/LoadingElement.svelte';
	import { page } from '$app/state';
	import { ProgressBarLocation, ProgressBarStatus } from '../elements/progress-bar.types';
	import { isImageAsset, isVideoAsset } from '$lib/constants/asset-type';

	interface AssetsState {
		assets: [string, api.AssetResponseDto, api.AlbumResponseDto[]][];
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
	let assetComponent: AssetComponentInstance = $state() as AssetComponentInstance;
	let currentDuration: number = $state($configStore.interval ?? 20);

	let error: boolean = $state(false);
	let infoVisible: boolean = $state(false);
	let authError: boolean = $state(false);
	let errorMessage: string = $state() as string;
	let assetsState: AssetsState = $state({
		assets: [],
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
		await assetComponent?.play?.();
		await progressBar.play();
	}

	const showCursor = () => {
		cursorVisible = true;
		clearTimeout(timeoutId);
		timeoutId = setTimeout(hideCursor, 2000);
	};

	async function updateAssetPromises() {
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
				try {
					const [url] = await assetPromisesDict[key];
					revokeObjectUrl(url);
				} catch (err) {
					console.warn('Failed to resolve asset during cleanup:', err);
				} finally {
					delete assetPromisesDict[key];
				}
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
			await assetComponent?.play?.();
			progressBar.play();
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
			isPortrait(assetBacklog[0]) &&
			isPortrait(assetBacklog[1])
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
		await updateAssetPromises();
		assetsState = await pickAssets(next);
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
			isPortrait(assetHistory[assetHistory.length - 1]) &&
			isPortrait(assetHistory[assetHistory.length - 2])
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
		await updateAssetPromises();
		assetsState = await pickAssets(next);
	}

	function isPortrait(asset: api.AssetResponseDto) {
		if (isVideoAsset(asset)) {
			return false;
		}

		const isFlipped = (orientation: number) => [5, 6, 7, 8].includes(orientation);
		let assetHeight = asset.exifInfo?.exifImageHeight ?? 0;
		let assetWidth = asset.exifInfo?.exifImageWidth ?? 0;
		if (isFlipped(Number(asset.exifInfo?.orientation ?? 0))) {
			[assetHeight, assetWidth] = [assetWidth, assetHeight];
		}
		return assetHeight > assetWidth;
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
		const parts = duration.split(':').map((value) => value.trim().replace(',', '.'));

		if (parts.length === 0 || parts.length > 3) {
			return 0;
		}

		const multipliers = [3600, 60, 1]; // hours, minutes, seconds
		const offset = multipliers.length - parts.length;

		let total = 0;
		for (let i = 0; i < parts.length; i++) {
			const numeric = parseFloat(parts[i]);
			if (Number.isNaN(numeric)) {
				return 0;
			}
			total += numeric * multipliers[offset + i];
		}
		return total;
	}

	async function pickAssets(assets: api.AssetResponseDto[]) {
		let newAssets = [];
		try {
			updateCurrentDuration(assets);
			for (let asset of assets) {
				let img = await assetPromisesDict[asset.id];
				newAssets.push(img);
			}
			return {
				assets: newAssets,
				error: false,
				loaded: true,
				split: assets.length == 2 && assets.every(isImageAsset),
				hasBday: hasBirthday(assets)
			};
		} catch {
			updateCurrentDuration([]);
			return {
				assets: [],
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
			throw new Error(`Failed to load asset ${assetResponse.id}: status ${req.status}`);
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

	function revokeObjectUrl(url: string) {
		try {
			URL.revokeObjectURL(url);
		} catch {
			console.warn('Failed to revoke object URL:', url);
		}
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
				assetComponent?.play?.();
			}
		});

		unsubscribeStop = stopProgress.subscribe((value) => {
			if (value) {
				progressBar.restart(false);
				assetComponent?.pause?.();
			}
		});

		getNextAssets();

		return () => {
			window.removeEventListener('mousemove', showCursor);
			window.removeEventListener('click', showCursor);
		};
	});

	onDestroy(async () => {
		if (unsubscribeRestart) {
			unsubscribeRestart();
		}

		if (unsubscribeStop) {
			unsubscribeStop();
		}

		const revokes = Object.values(assetPromisesDict).map(async (p) => {
			try {
				const [url] = await p;
				revokeObjectUrl(url);
			} catch (err) {
				console.warn('Failed to resolve asset during destroy cleanup:', err);
			}
		});
		await Promise.allSettled(revokes);
		assetPromisesDict = {};
	});
</script>

<section class="fixed grid h-dvh-safe w-screen bg-black" class:cursor-none={!cursorVisible}>
	{#if error}
		<ErrorElement {authError} message={errorMessage} />
	{:else if displayingAssets}
		<div class="absolute h-screen w-screen">
			<AssetComponent
				showLocation={$configStore.showImageLocation}
				interval={$configStore.interval}
				showPhotoDate={$configStore.showPhotoDate}
				showImageDesc={$configStore.showImageDesc}
				showPeopleDesc={$configStore.showPeopleDesc}
				showTagsDesc={$configStore.showTagsDesc}
				showAlbumName={$configStore.showAlbumName}
				{...assetsState}
				imageFill={$configStore.imageFill}
				imageZoom={$configStore.imageZoom}
				imagePan={$configStore.imagePan}
				bind:this={assetComponent}
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
					await assetComponent?.play?.();
					await progressBar.play();
				} else {
					await assetComponent?.pause?.();
					await progressBar.pause();
				}
			}}
			showInfo={async () => {
				if (infoVisible) {
					infoVisible = false;
					await assetComponent?.play?.();
					await progressBar.play();
				} else {
					infoVisible = true;
					await assetComponent?.pause?.();
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
