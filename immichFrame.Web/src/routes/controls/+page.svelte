<script lang="ts">
	import { onMount } from 'svelte';
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
	import Icon from '$lib/components/elements/icon.svelte';

	import { authSecretStore } from '$lib/stores/persist.store';

	const storageKey = 'immichframe.controls.device';
	const namesKey = 'immichframe.controls.deviceNames';
	let names = $state<Record<string, string>>({});
	let frameName = $state('');
	let devices = $state<string[]>([]);
	let device = $state('');
	let busy = $state(false);
	let message = $state('');
	let brightness = $state(0.5);
	let brightnessLabel = $state('Unknown');
	let needsSecret = $state(false);
	let secret = $state('');

	onMount(() => {
		try {
			device = localStorage.getItem(storageKey) ?? '';
			const stored = JSON.parse(localStorage.getItem(namesKey) ?? '{}');
			names = Object.fromEntries(
				Object.entries(stored ?? {}).filter(
					([address, name]) =>
						/^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$/.test(address) && typeof name === 'string'
				)
			) as Record<string, string>;
			devices = [...new Set([...Object.keys(names), ...(device ? [device] : [])])];
			frameName = names[device] ?? '';
		} catch {
			/* Storage may be unavailable in private browsing. */
		}
	});

	function selectDevice(address: string) {
		device = address;
		frameName = names[address] ?? '';
		brightnessLabel = 'Unknown';
		brightness = 0.5;
		try {
			if (address) localStorage.setItem(storageKey, address);
			else localStorage.removeItem(storageKey);
		} catch {
			/* Selection still works for this session. */
		}
	}

	function saveName(event: SubmitEvent) {
		event.preventDefault();
		const name = frameName.trim();
		if (!device || !name) return;
		names[device] = name;
		frameName = name;
		try {
			localStorage.setItem(namesKey, JSON.stringify(names));
			message = 'Frame name saved.';
		} catch {
			message = 'Frame name saved for this session.';
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
			const found: string[] = await (await request('scan')).json();
			devices = [...new Set([...Object.keys(names), ...found, ...(device ? [device] : [])])];
			if (!device && found.length === 1) selectDevice(found[0]);
			message = found.length
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

<main class="mx-auto flex min-h-screen max-w-2xl flex-col gap-4 px-4 py-8 sm:py-12">
	<header class="mb-3 flex items-center justify-between">
		<div class="flex items-center gap-3">
			<img src="/logo.svg" alt="" class="h-9 w-9" />
			<div>
				<h1>ImmichFrame</h1>
				<p>Frame controls</p>
			</div>
		</div>
		<a href="/admin" class="text-sm underline">Server settings</a>
	</header>

	<section class="panel">
		<h2>Target device</h2>
		<div class="flex flex-wrap items-center gap-3">
			<select
				aria-label="Target device"
				class="min-h-11 min-w-36 max-w-full flex-1 rounded-lg border px-3"
				value={device}
				disabled={busy || !devices.length}
				onchange={(e) => selectDevice(e.currentTarget.value)}
			>
				<option value="">Select a frame</option>
				{#each devices as address (address)}<option value={address}
						>{names[address] ? names[address] + ' (' + address + ')' : address}</option
					>{/each}
			</select>
			<button class="control-button" disabled={busy} onclick={scan}
				><Icon path={mdiWifi} ariaHidden={true} />Scan LAN</button
			>
		</div>
		{#if device}
			<form class="mt-3 flex flex-wrap items-end gap-2" onsubmit={saveName}>
				<label class="flex min-w-0 flex-1 flex-col gap-1">
					Frame name
					<input
						class="rounded-lg border px-3 py-2"
						maxlength="48"
						bind:value={frameName}
						placeholder="Living room"
					/>
				</label>
				<button class="control-button" disabled={!frameName.trim() || busy}>Save name</button>
			</form>
		{/if}
	</section>
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
			<button class="control-button" type="submit">Save</button>
		</form>
	{/if}
	<fieldset
		disabled={busy || !device}
		class="flex min-w-0 flex-col gap-4 disabled:opacity-60"
		aria-label="Frame controls"
	>
		<section class="panel">
			<h2>Playback</h2>
			<div class="playback grid grid-cols-3 gap-2">
				<button class="control-button" onclick={() => send('previous')}
					><Icon path={mdiChevronLeft} ariaHidden={true} />Previous</button
				>
				<button class="control-button" onclick={() => send('pause')}
					><Icon path={mdiPlayPause} ariaHidden={true} />Play / pause</button
				>
				<button class="control-button" onclick={() => send('next')}
					><Icon path={mdiChevronRight} ariaHidden={true} />Next</button
				>
			</div>
		</section>
		<section class="panel">
			<h2>Display</h2>
			<div class="flex flex-wrap gap-2">
				<button class="control-button" onclick={() => send('undim')}
					><Icon path={mdiPower} ariaHidden={true} />Frame on</button
				>
				<button class="control-button" onclick={() => send('dim')}
					><Icon path={mdiPowerSleep} ariaHidden={true} />Frame off</button
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
				<button class="control-button" onclick={() => send('brightness', -1)}
					><Icon path={mdiRestore} ariaHidden={true} />Reset</button
				>
			</div>
		</section>
		<div>
			<button class="control-button" onclick={() => send('settings')}
				><Icon path={mdiCog} ariaHidden={true} />Open frame settings</button
			>
		</div>
	</fieldset>
	<p role="status" class="text-sm">{message}</p>
	{#key device + ($authSecretStore ?? '')}<AlbumQueue
			{device}
			onauth={() => (needsSecret = true)}
		/>{/key}
</main>

<style>
	main {
		color: CanvasText;
		background: Canvas;
	}
	h1 {
		font-size: 1.3rem;
		font-weight: 650;
	}
	.panel {
		border: 1px solid color-mix(in srgb, CanvasText 20%, Canvas);
		border-radius: 1rem;
		padding: 1rem;
	}
	.control-button {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.45rem;
		border: 1px solid currentColor;
		border-radius: 0.75rem;
		min-height: 2.75rem;
		padding: 0.5rem 0.85rem;
		color: light-dark(#344294, #accbfa);
	}
	.control-button:disabled {
		opacity: 0.6;
	}
	.control-button:hover:enabled {
		background: color-mix(in srgb, currentColor 12%, Canvas);
	}

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
