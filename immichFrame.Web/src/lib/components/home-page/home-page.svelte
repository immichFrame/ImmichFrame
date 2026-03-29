<script lang="ts">
	import * as api from '$lib/index';
	import ProgressBar from '$lib/components/elements/progress-bar.svelte';
	import { slideshowStore } from '$lib/stores/slideshow.store';
	import {
		clientIdentifierStore,
		clientNameStore,
		authSecretStore
	} from '$lib/stores/persist.store';
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
	import {
		acknowledgeFrameSessionCommand,
		disconnectFrameSession,
		getFrameSessionCommands,
		putFrameSessionSnapshot,
		sendBeaconFrameSessionDisconnect
	} from '$lib/frameSessionApi';

	interface AssetsState {
		assets: [string, api.AssetResponseDto, api.AlbumResponseDto[]][];
		error: boolean;
		loaded: boolean;
		split: boolean;
		hasBday: boolean;
	}

	interface SessionDisplayEvent {
		displayedAtUtc: string;
		durationSeconds: number;
		assets: api.AssetResponseDto[];
	}

	api.init();

	const PRELOAD_ASSETS = 5;
	const HEARTBEAT_INTERVAL_MS = 10_000;
	const COMMAND_POLL_INTERVAL_MS = 2_000;
	const MAX_DISPLAY_HISTORY = 50;

	let assetHistory: api.AssetResponseDto[] = [];
	let assetBacklog: api.AssetResponseDto[] = [];
	let displayEventHistory: SessionDisplayEvent[] = [];

	let displayingAssets: api.AssetResponseDto[] = $state([]);
	let currentDisplayStartedAt: string | null = $state(null);
	let currentDisplayDurationSeconds: number = $state($configStore.interval ?? 20);
	let currentDisplayPausedAtMs: number | null = $state(null);
	let adminStopped: boolean = $state(false);

	const { restartProgress, stopProgress, instantTransition } = slideshowStore;

	let progressBarStatus: ProgressBarStatus = $state(ProgressBarStatus.Playing);
	let progressBar: ProgressBar = $state() as ProgressBar;
	let assetComponent: AssetComponentInstance = $state() as AssetComponentInstance;
	let currentDuration: number = $state($configStore.interval ?? 20);
	let userPaused: boolean = $state(false);

	let error: boolean = $state(false);
	let infoVisible: boolean = $state(false);
	let authError: boolean = $state(false);
	let errorMessage: string = $state('');
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
	let heartbeatIntervalId: number | undefined;
	let commandPollIntervalId: number | undefined;
	let isPollingCommands = false;
	let isHandlingRemoteCommand = false;
	let pendingDisplayNameSync = $state(false);
	let lastAppliedClientIdentifierFromUrl: string | null | undefined = $state(undefined);
	let lastAppliedClientNameFromUrl: string | null | undefined = $state(undefined);
	let lastAppliedAuthSecretFromUrl: string | null | undefined = $state(undefined);

	$effect(() => {
		const clientIdentifierFromUrl = page.url.searchParams.get('client');
		if (clientIdentifierFromUrl !== lastAppliedClientIdentifierFromUrl) {
			lastAppliedClientIdentifierFromUrl = clientIdentifierFromUrl;
			if (clientIdentifierFromUrl && clientIdentifierFromUrl !== $clientIdentifierStore) {
				clientIdentifierStore.set(clientIdentifierFromUrl);
			}
		}

		const clientNameFromUrl = page.url.searchParams.get('name');
		if (clientNameFromUrl !== lastAppliedClientNameFromUrl) {
			lastAppliedClientNameFromUrl = clientNameFromUrl;
			if (clientNameFromUrl && clientNameFromUrl !== $clientNameStore) {
				clientNameStore.set(clientNameFromUrl);
				pendingDisplayNameSync = true;
			}
		}

		const authSecretFromUrl = page.url.searchParams.get('authsecret');
		if (authSecretFromUrl !== lastAppliedAuthSecretFromUrl) {
			lastAppliedAuthSecretFromUrl = authSecretFromUrl;
			if (authSecretFromUrl && authSecretFromUrl !== $authSecretStore) {
				authSecretStore.set(authSecretFromUrl);
				api.init();
			}
		}
	});

	const hideCursor = () => {
		cursorVisible = false;
	};

	setContext('close', provideClose);

	async function provideClose() {
		await resumePlayback();
	}

	const showCursor = () => {
		cursorVisible = true;
		clearTimeout(timeoutId);
		timeoutId = setTimeout(hideCursor, 2000);
	};

	function toSessionDisplayEvent(
		assets: api.AssetResponseDto[],
		displayedAtUtc: string,
		durationSeconds: number
	): SessionDisplayEvent {
		return {
			displayedAtUtc,
			durationSeconds,
			assets: [...assets]
		};
	}

	function archiveCurrentDisplay() {
		if (!displayingAssets.length || !currentDisplayStartedAt) {
			return;
		}

		displayEventHistory = [
			toSessionDisplayEvent(displayingAssets, currentDisplayStartedAt, currentDisplayDurationSeconds),
			...displayEventHistory
		].slice(0, MAX_DISPLAY_HISTORY);
	}

	function setCurrentDisplay(assets: api.AssetResponseDto[]) {
		currentDisplayStartedAt = assets.length ? new Date().toISOString() : null;
		currentDisplayPausedAtMs = null;
	}

	function pauseCurrentDisplayClock() {
		if (currentDisplayPausedAtMs == null) {
			currentDisplayPausedAtMs = Date.now();
		}
	}

	function resumeCurrentDisplayClock() {
		if (currentDisplayPausedAtMs == null || !currentDisplayStartedAt) {
			currentDisplayPausedAtMs = null;
			return;
		}

		const pausedDurationMs = Date.now() - currentDisplayPausedAtMs;
		currentDisplayStartedAt = new Date(
			new Date(currentDisplayStartedAt).getTime() + pausedDurationMs
		).toISOString();
		currentDisplayPausedAtMs = null;
	}

	function toDisplayedAssetDto(asset: api.AssetResponseDto) {
		return {
			id: asset.id,
			originalFileName: asset.originalFileName,
			type: asset.type,
			immichServerUrl: asset.immichServerUrl ?? null,
			localDateTime: asset.localDateTime,
			description: asset.exifInfo?.description ?? null,
			thumbhash: asset.thumbhash ?? null
		};
	}

	function buildCurrentDisplay() {
		if (!displayingAssets.length || !currentDisplayStartedAt) {
			return null;
		}

		return {
			displayedAtUtc: currentDisplayStartedAt,
			durationSeconds: currentDisplayDurationSeconds,
			assets: displayingAssets.map(toDisplayedAssetDto)
		};
	}

	function buildHistory() {
		return displayEventHistory.map((displayEvent) => ({
			displayedAtUtc: displayEvent.displayedAtUtc,
			durationSeconds: displayEvent.durationSeconds,
			assets: displayEvent.assets.map(toDisplayedAssetDto)
		}));
	}

	async function syncFrameSession(status: 'Active' | 'Stopped' = adminStopped ? 'Stopped' : 'Active') {
		if (!$clientIdentifierStore) {
			return;
		}

		const shouldSyncDisplayName = pendingDisplayNameSync;
		try {
			await putFrameSessionSnapshot($clientIdentifierStore, {
				playbackState:
					adminStopped || progressBarStatus === ProgressBarStatus.Paused ? 'Paused' : 'Playing',
				status,
				displayName: shouldSyncDisplayName ? ($clientNameStore ?? null) : undefined,
				currentDisplay: buildCurrentDisplay(),
				history: buildHistory()
			});
			if (shouldSyncDisplayName) {
				pendingDisplayNameSync = false;
			}
		} catch (err) {
			console.warn('Failed to sync frame session:', err);
		}
	}

	function startSessionLoops() {
		stopSessionLoops();

		heartbeatIntervalId = window.setInterval(() => {
			void syncFrameSession();
		}, HEARTBEAT_INTERVAL_MS);

		commandPollIntervalId = window.setInterval(() => {
			void processPendingCommands();
		}, COMMAND_POLL_INTERVAL_MS);
	}

	function stopSessionLoops() {
		if (heartbeatIntervalId) {
			clearInterval(heartbeatIntervalId);
			heartbeatIntervalId = undefined;
		}

		if (commandPollIntervalId) {
			clearInterval(commandPollIntervalId);
			commandPollIntervalId = undefined;
		}
	}

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
		const keysToRemove = Object.keys(assetPromisesDict).filter(
			(key) =>
				!displayingAssets.find((item) => item.id === key) &&
				!assetBacklog.find((item) => item.id === key)
		);
		for (const key of keysToRemove) {
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

	async function loadAssets() {
		if (adminStopped) {
			return;
		}

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
		if (isHandlingAssetTransition || adminStopped) {
			return;
		}
		isHandlingAssetTransition = true;
		try {
			userPaused = false;
			progressBar.restart(false);
			$instantTransition = instant;
			if (previous) await getPreviousAssets();
			else await getNextAssets();
			await tick();
			await assetComponent?.play?.();
			void progressBar.play();
			await syncFrameSession();
		} finally {
			isHandlingAssetTransition = false;
		}
	};

	async function getNextAssets() {
		if (!assetBacklog.length) {
			await loadAssets();
		}

		if (!error && !assetBacklog.length) {
			error = true;
			errorMessage = 'No assets were found! Check your configuration.';
			return;
		}

		const useSplit = shouldUseSplitView(assetBacklog);
		const next = assetBacklog.splice(0, useSplit ? 2 : 1);
		assetBacklog = [...assetBacklog];

		if (displayingAssets.length) {
			assetHistory.push(...displayingAssets);
			archiveCurrentDisplay();
		}

		if (assetHistory.length > 250) {
			assetHistory = assetHistory.slice(-250);
		}

		displayingAssets = next;
		setCurrentDisplay(next);
		await updateAssetPromises();
		assetsState = await pickAssets(next);
		currentDisplayDurationSeconds = currentDuration;
	}

	async function getPreviousAssets() {
		if (!assetHistory.length) {
			if (displayingAssets.length) {
				setCurrentDisplay(displayingAssets);
				currentDisplayDurationSeconds = currentDuration;
			}
			return;
		}

		const useSplit = shouldUseSplitView(assetHistory.slice(-2));
		const next = assetHistory.splice(useSplit ? -2 : -1);
		assetHistory = [...assetHistory];

		if (displayingAssets.length) {
			assetBacklog.unshift(...displayingAssets);
			archiveCurrentDisplay();
		}

		displayingAssets = next;
		setCurrentDisplay(next);
		await updateAssetPromises();
		assetsState = await pickAssets(next);
		currentDisplayDurationSeconds = currentDuration;
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

	function shouldUseSplitView(assets: api.AssetResponseDto[]): boolean {
		return (
			$configStore.layout?.trim().toLowerCase() === 'splitview' &&
			assets.length > 1 &&
			isImageAsset(assets[0]) &&
			isImageAsset(assets[1]) &&
			isPortrait(assets[0]) &&
			isPortrait(assets[1])
		);
	}

	function hasBirthday(assets: api.AssetResponseDto[]) {
		let today = new Date();
		let hasBday: boolean = false;

		for (let asset of assets) {
			for (let person of asset.people ?? []) {
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

		const multipliers = [3600, 60, 1];
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
		let assetUrl: string;

		if (isVideoAsset(assetResponse)) {
			assetUrl = api.getAssetStreamUrl(
				assetResponse.id,
				$clientIdentifierStore,
				assetResponse.type
			);
		} else {
			const req = await api.getAsset(assetResponse.id, {
				clientIdentifier: $clientIdentifierStore,
				assetType: assetResponse.type
			});
			if (req.status != 200) {
				throw new Error(`Failed to load asset ${assetResponse.id}: status ${req.status}`);
			}
			assetUrl = getObjectUrl(req.data);
		}

		let album: api.AlbumResponseDto[] | null = null;
		if ($configStore.showAlbumName) {
			const albumReq = await api.getAlbumInfo(assetResponse.id, {
				clientIdentifier: $clientIdentifierStore
			});
			album = albumReq.data ?? [];
		}

		if ($configStore.showPeopleDesc && (assetResponse.people ?? []).length == 0) {
			const assetInfoRequest = await api.getAssetInfo(assetResponse.id, {
				clientIdentifier: $clientIdentifierStore
			});
			assetResponse.people = assetInfoRequest.data.people;
		}

		return [assetUrl, assetResponse, album] as [
			string,
			api.AssetResponseDto,
			api.AlbumResponseDto[]
		];
	}

	function getObjectUrl(image: Blob) {
		return URL.createObjectURL(image);
	}

	function revokeObjectUrl(url: string) {
		if (!url.startsWith('blob:')) return;
		try {
			URL.revokeObjectURL(url);
		} catch {
			console.warn('Failed to revoke object URL:', url);
		}
	}

	async function resumePlayback() {
		if (adminStopped) {
			return;
		}

		infoVisible = false;
		userPaused = false;
		resumeCurrentDisplayClock();
		await assetComponent?.play?.();
		void progressBar.play();
		await syncFrameSession();
	}

	async function pausePlayback() {
		if (adminStopped) {
			return;
		}

		infoVisible = false;
		userPaused = true;
		pauseCurrentDisplayClock();
		await assetComponent?.pause?.();
		await progressBar.pause();
		await syncFrameSession();
	}

	async function togglePlayback() {
		if (progressBarStatus == ProgressBarStatus.Paused) {
			await resumePlayback();
		} else {
			await pausePlayback();
		}
	}

	async function toggleInfo() {
		if (adminStopped) {
			return;
		}

		if (infoVisible) {
			await resumePlayback();
		} else {
			infoVisible = true;
			userPaused = true;
			pauseCurrentDisplayClock();
			await assetComponent?.pause?.();
			await progressBar.pause();
			await syncFrameSession();
		}
	}

	async function shutdownFromAdmin() {
		if (adminStopped) {
			return;
		}

		adminStopped = true;
		infoVisible = false;
		userPaused = true;
		stopSessionLoops();
		progressBar.restart(false);
		await assetComponent?.pause?.();
		await progressBar.pause();
		await syncFrameSession('Stopped');
	}

	async function processPendingCommands() {
		if (adminStopped || isPollingCommands || isHandlingRemoteCommand || !$clientIdentifierStore) {
			return;
		}

		isPollingCommands = true;
		try {
			const commands = await getFrameSessionCommands($clientIdentifierStore);
			for (const command of commands) {
				isHandlingRemoteCommand = true;
				try {
					switch (command.commandType) {
						case 'Previous':
							await handleDone(true, true);
							infoVisible = false;
							break;
						case 'Play':
							if (progressBarStatus === ProgressBarStatus.Paused || infoVisible || userPaused) {
								await resumePlayback();
							}
							break;
						case 'Pause':
							if (progressBarStatus !== ProgressBarStatus.Paused) {
								await pausePlayback();
							}
							break;
						case 'Next':
							await handleDone(false, true);
							infoVisible = false;
							break;
						case 'Refresh':
							await acknowledgeFrameSessionCommand($clientIdentifierStore, command.commandId);
							window.location.reload();
							return;
						case 'Shutdown':
							await acknowledgeFrameSessionCommand($clientIdentifierStore, command.commandId);
							await shutdownFromAdmin();
							continue;
							break;
					}

					await acknowledgeFrameSessionCommand($clientIdentifierStore, command.commandId);
				} catch (err) {
					console.warn('Failed to handle remote command:', err);
				} finally {
					isHandlingRemoteCommand = false;
				}
			}
		} finally {
			isPollingCommands = false;
		}
	}

	onMount(() => {
		window.addEventListener('mousemove', showCursor);
		window.addEventListener('click', showCursor);
		window.addEventListener('beforeunload', handleBeforeUnload);

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
				void syncFrameSession();
			}
		});

		unsubscribeStop = stopProgress.subscribe((value) => {
			if (value) {
				progressBar.restart(false);
				assetComponent?.pause?.();
				void syncFrameSession();
			}
		});

		startSessionLoops();
		void syncFrameSession();
		void getNextAssets().then(() => syncFrameSession());

		return () => {
			window.removeEventListener('mousemove', showCursor);
			window.removeEventListener('click', showCursor);
			window.removeEventListener('beforeunload', handleBeforeUnload);
		};
	});

	async function handleBeforeUnload() {
		if (!$clientIdentifierStore) {
			return;
		}

		if (sendBeaconFrameSessionDisconnect($clientIdentifierStore, $authSecretStore)) {
			return;
		}

		try {
			await disconnectFrameSession($clientIdentifierStore, true, $authSecretStore);
		} catch (err) {
			console.warn('Failed to disconnect frame session during unload:', err);
		}
	}

	onDestroy(async () => {
		stopSessionLoops();

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
	{#if adminStopped}
		<div class="place-self-center w-full max-w-2xl px-8">
			<div
				class="rounded-[2rem] border border-white/10 bg-white/10 p-10 text-center text-white shadow-2xl backdrop-blur"
			>
				<p class="text-xs uppercase tracking-[0.45em] text-white/60">Remote Control</p>
				<h1 class="mt-4 text-4xl font-semibold">Frame Stopped</h1>
				<p class="mt-4 text-lg text-white/70">
					This frame session was stopped from the admin dashboard. Refresh the page to reconnect.
				</p>
				{#if $clientIdentifierStore}
					<p class="mt-6 font-mono text-sm text-white/50">Session: {$clientIdentifierStore}</p>
				{/if}
			</div>
		</div>
	{:else if error}
		<ErrorElement {authError} message={errorMessage} />
	{:else if displayingAssets}
		<div class="absolute h-screen w-screen">
			<AssetComponent
				showLocation={$configStore.showImageLocation}
				interval={currentDuration}
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
				onVideoWaiting={async () => {
					pauseCurrentDisplayClock();
					await progressBar.pause();
					await syncFrameSession();
				}}
				onVideoPlaying={async () => {
					if (!userPaused) {
						resumeCurrentDisplayClock();
						await progressBar.play();
						await syncFrameSession();
					}
				}}
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
			pause={togglePlayback}
			showInfo={toggleInfo}
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
