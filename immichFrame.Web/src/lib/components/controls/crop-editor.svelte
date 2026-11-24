<script lang="ts">
	import { onMount, untrack } from 'svelte';
	type Crop = {
		x: number;
		y: number;
		width: number;
		height: number;
		turns: number;
		flipX: boolean;
		flipY: boolean;
	};
	let {
		image,
		initial,
		name,
		portraitRatio,
		lastCrop,
		loading,
		navigationError,
		canPrevious,
		canNext,
		onprevious,
		onnext,
		onsave,
		onclose
	}: {
		image: Blob;
		initial: Crop;
		name: string;
		portraitRatio: string;
		lastCrop: Crop | null;
		loading: boolean;
		navigationError: string;
		canPrevious: boolean;
		canNext: boolean;
		onprevious: () => void;
		onnext: () => void;
		onsave: (crop: Crop) => Promise<void>;
		onclose: () => void;
	} = $props();
	let dialog: HTMLDialogElement;
	let dragging = $state(false);
	let dirty = $state(false);
	let saved = $state(false);
	let fillPreview = $state(false);

	let canvas: HTMLCanvasElement;
	let source = $state<HTMLImageElement>(),
		crop = $state(untrack(() => ({ ...initial }))),
		ratio = $state('10:16'),
		zoom = $state(1);
	let busy = $state(false),
		error = $state('');
	let dimensions = $derived(
		source
			? crop.turns % 2
				? [source.naturalHeight, source.naturalWidth]
				: [source.naturalWidth, source.naturalHeight]
			: [1, 1]
	);
	let frameRatio = $derived(
		dimensions[0] > dimensions[1] ? portraitRatio.split(':').reverse().join(':') : portraitRatio
	);
	function changed() {
		dirty = true;
		saved = false;
	}
	onMount(() => {
		dialog.showModal();
		const url = URL.createObjectURL(image),
			img = new Image();
		img.onload = () => {
			source = img;
			const width = crop.width * dimensions[0];
			const height = crop.height * dimensions[1];
			const aspect = width / height;
			zoom = Math.min(8, Math.max(1, Math.min(dimensions[0] / width, dimensions[1] / height)));
			if (Math.abs(aspect - 10 / 16) < 0.00001) {
				ratio = '10:16';
			} else {
				const scale = Math.min(1, 100000 / Math.max(width, height));
				const w = Math.max(1, Math.round(width * scale));
				const h = Math.max(1, Math.round(height * scale));
				let a = w,
					b = h;
				while (b) [a, b] = [b, a % b];
				ratio = w / a + ':' + h / a;
			}
		};
		img.onerror = () => (error = 'Could not decode this image.');
		img.src = url;
		return () => {
			dialog.close();
			img.onload = null;
			URL.revokeObjectURL(url);
		};
	});
	function fit() {
		const [w, h] = ratio.split(':').map(Number);
		if (!/^\s*\d{1,5}\s*:\s*\d{1,5}\s*$/.test(ratio) || w < 1 || h < 1) {
			error = 'Enter two whole numbers, such as 10:16.';
			return;
		}
		error = '';
		changed();
		const aspect = ((w / h) * dimensions[1]) / dimensions[0];
		const width = Math.min(1, aspect) / zoom,
			height = Math.min(1, 1 / aspect) / zoom;
		crop = {
			...crop,
			x: Math.max(0, Math.min(1 - width, crop.x + (crop.width - width) / 2)),
			y: Math.max(0, Math.min(1 - height, crop.y + (crop.height - height) / 2)),
			width,
			height
		};
	}
	function fillDevice() {
		ratio = frameRatio;
		fit();
		fillPreview = true;
	}

	function applyLastCrop() {
		if (!lastCrop) return;
		crop = { ...lastCrop };
		const [w, h] = dimensions;
		// Keep the previous crop center and approximate zoom, adapting the window to this photo.
		const [rw, rh] = frameRatio.split(':').map(Number);
		const aspect = ((rw / rh) * h) / w;
		zoom = Math.max(
			1,
			Math.min(8, Math.min(1, aspect) / crop.width, Math.min(1, 1 / aspect) / crop.height)
		);
		fillDevice();
	}

	$effect(() => {
		if (!source || !canvas) return;
		const [w, h] = dimensions;
		const view = fillPreview ? crop : { x: 0, y: 0, width: 1, height: 1 };
		const scale = Math.min(900 / (view.width * w), 520 / (view.height * h));
		canvas.width = Math.max(1, Math.round(view.width * w * scale));
		canvas.height = Math.max(1, Math.round(view.height * h * scale));
		const ctx = canvas.getContext('2d');
		if (!ctx) return;
		ctx.save();
		ctx.scale(scale, scale);
		ctx.translate(-view.x * w + w / 2, -view.y * h + h / 2);
		ctx.scale(crop.flipX ? -1 : 1, crop.flipY ? -1 : 1);
		ctx.rotate((crop.turns * Math.PI) / 2);
		ctx.drawImage(source, -source.naturalWidth / 2, -source.naturalHeight / 2);
		ctx.restore();
		if (fillPreview) {
			ctx.strokeStyle = '#7ce4ff';
			ctx.setLineDash([6, 4]);
			ctx.lineWidth = 2;
			ctx.strokeRect(2, 2, canvas.width - 4, canvas.height - 4);
			return;
		}
		const [rw, rh] = frameRatio.split(':').map(Number);
		const aspect = ((rw / rh) * h) / w;
		const width = Math.min(crop.width, crop.height * aspect);
		const height = width / aspect;
		const x = crop.x + (crop.width - width) / 2;
		const y = crop.y + (crop.height - height) / 2;
		ctx.lineWidth = 2;
		ctx.strokeStyle = '#fff';
		ctx.strokeRect(
			crop.x * w * scale + 1,
			crop.y * h * scale + 1,
			Math.max(1, crop.width * w * scale - 2),
			Math.max(1, crop.height * h * scale - 2)
		);
		ctx.strokeStyle = '#7ce4ff';
		ctx.setLineDash([6, 4]);
		ctx.strokeRect(
			x * w * scale + 1,
			y * h * scale + 1,
			Math.max(1, width * w * scale - 2),
			Math.max(1, height * h * scale - 2)
		);
	});

	let last: { x: number; y: number } | null = null;
	function move(e: PointerEvent) {
		if (!last) return;
		const rect = canvas.getBoundingClientRect();
		crop.x = Math.max(
			0,
			Math.min(
				1 - crop.width,
				crop.x - ((e.clientX - last.x) / rect.width) * (fillPreview ? crop.width : 1)
			)
		);
		crop.y = Math.max(
			0,
			Math.min(
				1 - crop.height,
				crop.y - ((e.clientY - last.y) / rect.height) * (fillPreview ? crop.height : 1)
			)
		);
		last = { x: e.clientX, y: e.clientY };
		changed();
	}
	async function save(advance?: () => void) {
		busy = true;
		try {
			await onsave({ ...crop });
			dirty = false;
			saved = true;
			advance?.();
		} catch (e) {
			error = String(e);
		} finally {
			busy = false;
		}
	}
</script>

<dialog
	bind:this={dialog}
	aria-labelledby="crop-title"
	oncancel={(event) => {
		event.preventDefault();
		if (!busy && !loading) onclose();
	}}
>
	<header class="flex flex-wrap items-center justify-between gap-2">
		<div>
			<h3 id="crop-title">Frame crop</h3>
			<p class="break-all text-sm">{name}</p>
		</div>
		<button onclick={onclose} disabled={busy || loading}>Close</button>
	</header>
	<p class="text-sm">Dashed outline: {frameRatio} device window. Drag to pan the photo.</p>
	<div class="my-3 flex min-h-32 justify-center bg-black">
		<canvas
			aria-label="Drag photo to adjust crop"
			bind:this={canvas}
			class:dragging
			style="max-width:100%;max-height:45vh;object-fit:contain;align-self:center;touch-action:none"
			onpointerdown={(e) => {
				dragging = true;
				canvas.setPointerCapture(e.pointerId);
				last = { x: e.clientX, y: e.clientY };
			}}
			onpointermove={move}
			onlostpointercapture={() => {
				last = null;
				dragging = false;
			}}
		></canvas>
	</div>
	<fieldset disabled={busy || loading || !source} class="min-w-0 flex flex-wrap items-center gap-3">
		<label
			>Ratio <input
				class="w-28 border"
				placeholder="10:16"
				bind:value={ratio}
				onchange={() => {
					fillPreview = false;
					fit();
				}}
			/></label
		>
		<label
			>Zoom <input
				type="range"
				min="1"
				max="8"
				step=".05"
				bind:value={zoom}
				oninput={(e) => {
					zoom = +e.currentTarget.value;
					fit();
				}}
			/></label
		>
		<button
			onclick={() => {
				crop.turns = (crop.turns + 3) % 4;
				if (fillPreview) fillDevice();
				else fit();
			}}>↶ Left</button
		>
		<button
			onclick={() => {
				crop.turns = (crop.turns + 1) % 4;
				if (fillPreview) fillDevice();
				else fit();
			}}>↷ Right</button
		>
		<button aria-pressed={crop.flipX} onclick={() => (crop.flipX = !crop.flipX)}
			>↔ Flip horizontal</button
		>
		<button aria-pressed={crop.flipY} onclick={() => (crop.flipY = !crop.flipY)}
			>↕ Flip vertical</button
		>
		<button
			onclick={() => {
				crop = { x: 0, y: 0, width: 1, height: 1, turns: 0, flipX: false, flipY: false };
				zoom = 1;
				ratio = dimensions[0] + ':' + dimensions[1];
				fillPreview = false;
				changed();
				error = '';
			}}>Reset</button
		>
		<button onclick={fillDevice}>Crop to device ratio</button>
		<button
			aria-pressed={fillPreview}
			onclick={() => {
				if (!fillPreview) fillDevice();
				else fillPreview = false;
			}}
		>
			{fillPreview ? 'Show full photo' : 'Fill device preview'}
		</button>
		<button onclick={applyLastCrop} disabled={!lastCrop}>Apply last crop</button>
	</fieldset>
	<div class="mt-4 flex flex-wrap gap-2">
		<button
			disabled={!canPrevious || loading || busy || (dirty && !!error)}
			onclick={() => (dirty ? save(onprevious) : onprevious())}
		>
			{dirty ? 'Save & previous' : 'Previous photo'}
		</button>
		<button onclick={() => save()} disabled={busy || loading || !source || !!error}
			>Save crop</button
		>
		<button
			disabled={!canNext || loading || busy || (dirty && !!error)}
			onclick={() => (dirty ? save(onnext) : onnext())}
		>
			{dirty ? 'Save & next' : 'Next photo'}
		</button>
	</div>
	{#if saved}<p role="status">Frame crop saved.</p>{/if}
	{#if navigationError || error}<p role="alert">{navigationError || error}</p>{/if}
</dialog>

<style>
	dialog {
		position: fixed;
		inset: 0;
		margin: auto;
		width: min(94vw, 48rem);
		max-height: 92dvh;
		overflow: auto;
		border: 1px solid currentColor;
		border-radius: 1rem;
		padding: 1rem;
		background: Canvas;
		color: CanvasText;
	}
	dialog::backdrop {
		background: rgb(0 0 0 / 65%);
	}
	h3 {
		font-weight: 650;
		font-size: 1.2rem;
	}
	canvas {
		cursor: grab;
	}
	canvas.dragging {
		cursor: grabbing;
	}
	button:disabled {
		opacity: 0.5;
	}

	button {
		border: 1px solid currentColor;
		border-radius: 0.6rem;
		padding: 0.4rem 0.65rem;
	}
	button[aria-pressed='true'] {
		font-weight: bold;
	}
</style>
