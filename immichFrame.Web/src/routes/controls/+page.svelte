<script lang="ts">
	import { onMount } from 'svelte';
	import { Button, Card, CardBody, themeManager, TooltipProvider } from '@immich/ui';
	import {
		mdiWifi,
		mdiChevronLeft,
		mdiChevronRight,
		mdiPlayPause,
		mdiPower,
		mdiPowerSleep,
		mdiRestore,
		mdiCog
	} from '@mdi/js';
	import AlbumQueue from '$lib/components/controls/album-queue.svelte';
	import AdminBrand from '$lib/components/admin/admin-brand.svelte';

	import { authSecretStore } from '$lib/stores/persist.store';

	const storageKey = 'immichframe.controls.device';
	let devices = $state<string[]>([]);
	let device = $state('');
	let busy = $state(false);
	let message = $state('');
	let brightness = $state(0.5);
	let brightnessLabel = $state('Unknown');
	let needsSecret = $state(false);
	let secret = $state('');

	onMount(() => {
		themeManager.setPreference(themeManager.preference);
		try {
			device = localStorage.getItem(storageKey) ?? '';
			if (device) devices = [device];
		} catch {
			/* Storage may be unavailable in private browsing. */
		}
	});

	function selectDevice(address: string) {
		device = address;
		brightnessLabel = 'Unknown';
		brightness = 0.5;
		try {
			if (address) localStorage.setItem(storageKey, address);
		} catch {
			/* Selection still works for this session. */
		}
	}

	async function request(action: string, body?: object) {
		const response = await fetch('/api/controls/' + action, {
			method: 'POST',
			headers: {
				...(action === 'command' && $authSecretStore
					? { Authorization: 'Bearer ' + $authSecretStore }
					: {}),
				'Content-Type': 'application/json'
			},
			body: JSON.stringify(body ?? {})
		});
		if (response.status === 401) {
			needsSecret = true;
			throw new Error('Enter the slideshow secret to send commands.');
		}
		if (!response.ok) throw new Error((await response.text()) || 'Request failed.');
		return response;
	}

	async function scan() {
		busy = true;
		message = 'Searching the frame LAN…';
		try {
			devices = await (await request('scan')).json();
			if (!devices.includes(device)) selectDevice(devices.length === 1 ? devices[0] : '');
			message = devices.length
				? ''
				: 'No frames found. Check the frame is running and the server can reach its LAN.';
		} catch (error) {
			message = String(error);
		} finally {
			busy = false;
		}
	}

	async function send(command: string, value?: number) {
		busy = true;
		try {
			await request('command', { device, command, value });
			if (command === 'brightness')
				brightnessLabel = value === -1 ? 'System default' : Math.round(brightness * 100) + '%';
			message = 'Command received by ' + device + '.';
		} catch (error) {
			if (command === 'brightness') brightnessLabel = 'Unknown';
			message = String(error);
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head><title>Frame controls · ImmichFrame</title></svelte:head>
<TooltipProvider>
	<main class="mx-auto flex min-h-screen max-w-2xl flex-col gap-4 px-4 py-8 sm:py-12">
		<header class="mb-3 flex items-center justify-between">
			<AdminBrand subtitle="Frame controls" />
			<a href="/admin" class="text-sm underline">Server settings</a>
		</header>

		<Card
			><CardBody>
				<h2>Target device</h2>
				<div class="flex flex-wrap items-center gap-3">
					<select
						aria-label="Target device"
						class="min-h-11 min-w-36 flex-1 rounded-lg border px-3"
						value={device}
						disabled={busy || !devices.length}
						onchange={(e) => selectDevice(e.currentTarget.value)}
					>
						<option value="">Select a frame</option>
						{#each devices as address (address)}<option value={address}>{address}</option>{/each}
					</select>
					<Button leadingIcon={mdiWifi} disabled={busy} onclick={scan}>Scan LAN</Button>
				</div>
			</CardBody></Card
		>
		{#if needsSecret}
			<form
				class="flex flex-wrap items-center gap-2"
				onsubmit={(event) => {
					event.preventDefault();
					authSecretStore.set(secret);
					secret = '';
					needsSecret = false;
					message = 'Secret saved. Select a command to continue.';
				}}
			>
				<label for="frame-secret">Slideshow secret</label>
				<input
					id="frame-secret"
					type="password"
					required
					bind:value={secret}
					class="rounded border p-2"
				/>
				<Button type="submit">Save</Button>
			</form>
		{/if}
		<fieldset
			disabled={busy || !device}
			class="flex min-w-0 flex-col gap-4 disabled:opacity-60"
			aria-label="Frame controls"
		>
			<Card
				><CardBody>
					<h2>Playback</h2>
					<div class="playback grid grid-cols-3 gap-2">
						<Button leadingIcon={mdiChevronLeft} variant="outline" onclick={() => send('previous')}
							>Previous</Button
						>
						<Button leadingIcon={mdiPlayPause} onclick={() => send('pause')}>Play / pause</Button>
						<Button leadingIcon={mdiChevronRight} variant="outline" onclick={() => send('next')}
							>Next</Button
						>
					</div>
				</CardBody></Card
			>
			<Card
				><CardBody>
					<h2>Display</h2>
					<div class="flex flex-wrap gap-2">
						<Button leadingIcon={mdiPower} variant="outline" onclick={() => send('undim')}
							>Frame on</Button
						>
						<Button leadingIcon={mdiPowerSleep} variant="outline" onclick={() => send('dim')}
							>Frame off</Button
						>
					</div>
					<div class="mt-6 flex items-baseline justify-between">
						<label for="brightness">Brightness</label><output for="brightness" class="text-sm"
							>{brightnessLabel}</output
						>
					</div>
					<div class="mt-2 flex items-center gap-4">
						<input
							id="brightness"
							class="min-w-0 flex-1"
							type="range"
							min="0"
							max="1"
							step="0.01"
							bind:value={brightness}
							oninput={(e) => (brightnessLabel = Math.round(+e.currentTarget.value * 100) + '%')}
							onchange={() => send('brightness', brightness)}
						/>
						<Button
							leadingIcon={mdiRestore}
							variant="outline"
							onclick={() => send('brightness', -1)}>Reset</Button
						>
					</div>
				</CardBody></Card
			>
			<div>
				<Button leadingIcon={mdiCog} variant="outline" onclick={() => send('settings')}
					>Open frame settings</Button
				>
			</div>
		</fieldset>
		<p role="status" class="text-sm">{message}</p>
        {#key device + ($authSecretStore ?? '')}<AlbumQueue {device} onauth={()=>needsSecret=true}/>{/key}
	</main>
</TooltipProvider>

<style>
	h2 {
		font-size: 1.125rem;
		font-weight: 600;
		margin-bottom: 1rem;
	}
	select,
	option {
		color: inherit;
		background: Canvas;
	}
	@media (max-width: 32rem) {
		.playback :global(button) {
			flex-direction: column;
			padding-inline: 0.25rem;
		}
	}
</style>
