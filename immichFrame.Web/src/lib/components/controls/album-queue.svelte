<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { mdiPencil, mdiUpload } from '@mdi/js';
	import Icon from '$lib/components/elements/icon.svelte';
	import { authSecretStore } from '$lib/stores/persist.store';
	import Editor from './crop-editor.svelte';
	type Photo = { key: string; name: string };
	type Album = { account: string; album: string; name: string };
	type Crop = {
		x: number;
		y: number;
		width: number;
		height: number;
		turns: number;
		flipX: boolean;
		flipY: boolean;
	};
	type Upload = { name: string; status: string; target: string; asset?: string; receipt?: string };
	let { device, frameRatio, onauth }: { device: string; frameRatio: string; onauth: () => void } =
		$props();
	let albums = $state<Album[]>([]),
		photos = $state<Photo[]>([]),
		total = $state(0),
		target = $state('');
	let status = $state<{ supported: boolean; frame?: string; current?: string }>({
		supported: false
	});
	let lastCrop = $state<Crop | null>(null);
	let editorBusy = $state(false);
	let editorError = $state('');
	let edit = $state<{
		photo: Photo;
		image: Blob;
		crop: Crop;
		frame: string;
		device: string;
	} | null>(null);
	let nextOffset = $state(0);
	let loadingPage = $state(false);
	let pageRequest: Promise<void> | undefined;
	let listRevision = 0;
	let pollRevision = 0;
	let message = $state(''),
		frameMessage = $state(''),
		uploads = $state<Upload[]>([]),
		working = $state(false);
	const controller = new AbortController();
	let alive = true;
	let filePicker: HTMLInputElement;
	function auth() {
		return $authSecretStore
			? { Authorization: 'Bearer ' + $authSecretStore }
			: ({} as Record<string, string>);
	}
	async function api(path: string, opts: RequestInit = {}) {
		const r = await fetch(path, {
			...opts,
			signal: controller.signal,
			headers: { ...auth(), ...opts.headers }
		});
		if (r.status === 401) onauth();
		if (!r.ok) throw Error((await r.text()) || 'Request failed.');
		return r;
	}
	async function list(offset = 0) {
		const revision = offset ? listRevision : ++listRevision;
		const data: { albums: Album[]; photos: Photo[]; total: number } = await (
			await api('/api/gallery?offset=' + offset)
		).json();
		if (revision !== listRevision) return;
		albums = data.albums;
		total = data.total;
		nextOffset = offset + data.photos.length;
		const seen = new Set(offset ? photos.map((photo) => photo.key) : []);
		const unique = data.photos.filter((photo) => {
			if (seen.has(photo.key)) return false;
			seen.add(photo.key);
			return true;
		});
		photos = offset ? [...photos, ...unique] : unique;
		if (albums.length === 1) target = albums[0].account + '/' + albums[0].album;
	}
	function loadMore() {
		if (pageRequest) return pageRequest;
		loadingPage = true;
		pageRequest = list(nextOffset).finally(() => {
			pageRequest = undefined;
			loadingPage = false;
		});
		return pageRequest;
	}
	function remote(command: string, key?: string, address = device) {
		return api('/api/controls/command', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ device: address, command, key })
		});
	}
	async function poll(address = device) {
		const revision = ++pollRevision;
		if (!address) {
			status = { supported: false };
			frameMessage = '';
			return;
		}
		try {
			const nextStatus = await (await remote('status', undefined, address)).json();
			if (!alive || address !== device || revision !== pollRevision) return;
			status = nextStatus;
			frameMessage = status.supported
				? ''
				: 'Install the gallery test app on this frame to select and crop photos.';
		} catch (e) {
			if (!alive || address !== device || revision !== pollRevision) return;
			status = { supported: false };
			frameMessage = String(e);
		}
	}
	$effect(() => {
		const address = device;
		status = { supported: false };
		frameMessage = '';
		edit = null;
		lastCrop = null;
		untrack(() => void poll(address));
	});
	onMount(() => {
		void list().catch((e) => (message = String(e)));
		let timer: ReturnType<typeof setTimeout>;
		async function tick() {
			await poll();
			if (alive) timer = setTimeout(tick, 3000);
		}
		timer = setTimeout(tick, 3000);
		return () => {
			alive = false;
			clearTimeout(timer);
			controller.abort();
		};
	});
	async function jump(p: Photo) {
		try {
			await remote('goto', p.key);
			await poll();
		} catch (e) {
			message = String(e);
		}
	}
	async function openEditor(p: Photo) {
		if (editorBusy || !status.frame) return;
		editorBusy = true;
		try {
			await loadEditor(p);
		} finally {
			editorBusy = false;
		}
	}
	async function loadEditor(p: Photo) {
		const frame = status.frame;
		const address = device;
		if (!frame) return;
		editorError = '';
		try {
			const base = '/api/gallery/' + p.key;
			const [image, crop] = await Promise.all([
				api(base + '/image').then((r) => r.blob()),
				api(base + '/crop/' + frame).then((r) => r.json())
			]);
			if (alive && device === address && status.frame === frame)
				edit = { photo: p, image, crop, frame, device: address };
		} catch (e) {
			editorError = String(e);
			if (!edit) message = editorError;
		}
	}
	async function save(crop: Crop) {
		if (!edit || edit.device !== device) throw Error('Select the frame and reopen the editor.');
		const { photo, frame, device: address } = edit;
		await api('/api/gallery/' + photo.key + '/crop/' + frame, {
			method: 'PUT',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify(crop)
		});
		await poll(address);
		if (device === address && status.frame === frame && status.current === photo.key)
			await remote('goto', photo.key, address);
		lastCrop = crop;
		message = 'Frame crop saved.';
	}
	let editIndex = $derived(edit ? photos.findIndex((photo) => photo.key === edit?.photo.key) : -1);
	async function navigate(step: number) {
		if (editorBusy || editIndex < 0) return;
		const index = editIndex + step;
		editorBusy = true;
		try {
			while (index === photos.length && nextOffset < total) {
				const offset = nextOffset;
				await loadMore();
				if (nextOffset <= offset) break;
			}
			if (photos[index]) await loadEditor(photos[index]);
		} catch (error) {
			editorError = String(error);
		} finally {
			editorBusy = false;
		}
	}

	function thumbnail(node: HTMLImageElement, key: string) {
		let url = '',
			active = true;
		const observer = new IntersectionObserver((entries) => {
			if (!entries.some((e) => e.isIntersecting)) return;
			observer.disconnect();
			api('/api/gallery/' + key + '/image?thumbnail=true')
				.then((r) => r.blob())
				.then((blob) => {
					if (active) {
						url = URL.createObjectURL(blob);
						node.src = url;
					}
				})
				.catch(() => {
					node.alt = 'Preview unavailable';
				});
		});
		observer.observe(node);
		return {
			destroy() {
				active = false;
				observer.disconnect();
				if (url) URL.revokeObjectURL(url);
			}
		};
	}
	async function attach(row: Upload) {
		await api('/api/gallery/' + row.target + '/attach/' + row.asset, {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify(row.receipt)
		});
		row.status = 'In album';
		row.receipt = undefined;
		await list();
	}
	async function upload(files: File[]) {
		working = true;
		try {
			for (const file of files) {
				uploads.push({ name: file.name, status: 'Starting…', target });
				const row = uploads[uploads.length - 1];
				try {
					const result = await new Promise<{ asset: string; receipt: string }>(
						(resolve, reject) => {
							const xhr = new XMLHttpRequest();
							xhr.open('POST', '/api/gallery/' + row.target + '/upload');
							xhr.setRequestHeader('X-Frame-Upload', '1');
							if ($authSecretStore)
								xhr.setRequestHeader('Authorization', 'Bearer ' + $authSecretStore);
							xhr.upload.onprogress = (e) => {
								if (e.lengthComputable) row.status = Math.round((100 * e.loaded) / e.total) + '%';
							};
							xhr.onerror = () => reject(Error('Upload connection failed.'));
							xhr.onload = () => {
								if (xhr.status === 401) onauth();
								if (xhr.status < 200 || xhr.status >= 300)
									return reject(Error(xhr.responseText || 'Upload failed'));
								try {
									resolve(JSON.parse(xhr.responseText));
								} catch (e) {
									reject(e);
								}
							};
							const form = new FormData();
							form.append('file', file);
							xhr.send(form);
						}
					);
					row.asset = result.asset;
					row.receipt = result.receipt;
					await attach(row);
				} catch (e) {
					row.status = String(e);
				}
			}
		} finally {
			working = false;
		}
	}
	let current = $derived(
		status.current
			? (photos.find((p) => p.key === status.current) ?? {
					key: status.current,
					name: 'Current photo'
				})
			: null
	);
</script>

<section class="min-w-0 space-y-3 rounded-xl border p-4">
	<h2>Now showing</h2>
	{#if current}{#key current.key}
			<div class="relative w-fit">
				<img class="max-h-52 max-w-full rounded" alt={current.name} use:thumbnail={current.key} />
				<button
					class="photo-edit"
					aria-label="Edit current photo"
					title="Edit frame crop"
					onclick={() => openEditor(current!)}
				>
					<Icon path={mdiPencil} ariaHidden={true} />
				</button>
			</div>
		{/key}{:else}<p>
			{frameMessage ||
				(device
					? 'Waiting for playback confirmation…'
					: 'Select a frame to see its current photo.')}
		</p>{/if}
	{#if edit}
		{#key edit.photo.key}
			<Editor
				image={edit.image}
				initial={edit.crop}
				name={edit.photo.name}
				{frameRatio}
				{lastCrop}
				loading={editorBusy}
				navigationError={editorError}
				canPrevious={editIndex > 0}
				canNext={editIndex >= 0 && editIndex + 1 < total}
				onprevious={() => navigate(-1)}
				onnext={() => navigate(1)}
				onsave={save}
				onclose={() => (edit = null)}
			/>
		{/key}
	{/if}
	<h2>Up next <small>({total})</small></h2>
	{#if !albums.length}<p>
			Choose albums in the server account settings to populate this gallery.
		</p>{/if}
	<div class="grid grid-cols-2 gap-2 sm:grid-cols-4">
		{#each photos as p (p.key)}
			<div class="relative min-w-0 rounded border p-1" class:current={p.key === status.current}>
				<button
					class="w-full"
					disabled={!status.supported}
					title={'Show ' + p.name}
					onclick={() => jump(p)}
				>
					<img
						class="aspect-square w-full rounded object-cover"
						alt={p.name}
						use:thumbnail={p.key}
					/>
				</button>
				<button
					class="photo-edit"
					disabled={!status.supported}
					aria-label={'Edit ' + p.name}
					title="Edit frame crop"
					onclick={() => openEditor(p)}
				>
					<Icon path={mdiPencil} ariaHidden={true} />
				</button>
			</div>
		{/each}
	</div>
	{#if nextOffset < total}<button
			disabled={working || loadingPage}
			onclick={async () => {
				try {
					await loadMore();
				} catch (e) {
					message = String(e);
				}
			}}>Load more</button
		>{/if}
	<h2>Upload to album</h2>
	<select aria-label="Upload destination album" bind:value={target} disabled={working}>
		<option value="">Select an album</option>{#each albums as a}<option
				value={a.account + '/' + a.album}>{a.name}</option
			>{/each}
	</select>
	<button class="upload-button" disabled={!target || working} onclick={() => filePicker.click()}>
		<Icon path={mdiUpload} ariaHidden={true} /> Choose files
	</button>
	<input
		aria-label="Upload images"
		bind:this={filePicker}
		hidden
		disabled={!target || working}
		type="file"
		multiple
		accept="image/jpeg,image/png,image/gif,image/webp"
		onchange={(e) => {
			const files = Array.from(e.currentTarget.files ?? []);
			e.currentTarget.value = '';
			void upload(files);
		}}
	/>
	{#each uploads as row}<p>
			{row.name} · {row.status}
			{#if row.receipt}<button onclick={() => attach(row).catch((e) => (row.status = String(e)))}
					>Retry adding to album</button
				>{/if}
		</p>{/each}
	<p role="status">{message}</p>
</section>

<style>
	.photo-edit {
		position: absolute;
		top: 0.45rem;
		right: 0.45rem;
		display: grid;
		place-items: center;
		min-width: 2.5rem;
		min-height: 2.5rem;
		border-radius: 0.65rem;
		background: rgb(0 0 0 / 75%);
		color: white;
	}
	.upload-button {
		display: inline-flex;
		align-items: center;
		gap: 0.4rem;
		padding: 0.55rem 0.85rem;
		border: 1px solid currentColor;
		border-radius: 0.65rem;
	}

	h2 {
		font-size: 1.15rem;
		font-weight: 600;
	}
	small {
		font-weight: 400;
	}
	button:disabled {
		opacity: 0.55;
	}
	button:not(:disabled):hover {
		text-decoration: underline;
	}
	.current {
		outline: 2px solid currentColor;
	}
	select {
		border: 1px solid;
		border-radius: 0.5rem;
		padding: 0.45rem;
		max-width: 100%;
	}
</style>
