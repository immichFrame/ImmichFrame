<script lang="ts">
	import { type AssetResponseDto, type PersonWithFacesResponseDto } from '$lib/immichFrameApi';
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';
	import AssetInfo from './asset-info.svelte';
	import { onMount } from 'svelte';

	interface Props {
		image: [url: string, asset: AssetResponseDto];
		showLocation: boolean;
		showPhotoDate: boolean;
		showImageDesc: boolean;
		showPeopleDesc: boolean;
		imageFill: boolean;
		imageZoom: boolean;
		interval: number;
		multi?: boolean;
	}

	let {
		image,
		showLocation,
		showPhotoDate,
		showImageDesc,
		showPeopleDesc,
		imageFill,
		imageZoom,
		interval,
		multi = false
	}: Props = $props();

	let debug = false;
	let imageElement: HTMLImageElement;
	let frameElement: HTMLDivElement;
	
	let frameWidth = $state(0);
	let frameHeight = $state(0);
	let centerX = $state(0);
	let centerY = $state(0);
	let naturalWidth = $state(0);
	let naturalHeight = $state(0);
	let scale = $state(0.0);
	
	let focusFace : any = $state(null);
	let focusX = $state(0);
	let focusY = $state(0);

	let hasPerson = $derived(image[1].people?.filter((x) => x.name).length ?? 0 > 0);

	function GetFaceBounds(face: any, scale: number) {
		let x1 = face.boundingBoxX1 ?? 0;
		let x2 = face.boundingBoxX2 ?? 0;
		let y1 = face.boundingBoxY1 ?? 0;
		let y2 = face.boundingBoxY2 ?? 0;
		const centerX = (x1 + x2) / 2;
		const centerY = (y1 + y2) / 2;
		const width = (x2 - x1) / 2;
		const height = (y2 - y1) / 2;
		let bounds = {
			X1: centerX - width * scale,
			Y1: centerY - height * scale,
			X2: centerX + width * scale,
			Y2: centerY + height * scale
		};
		bounds.X1 = Math.max(bounds.X1, 0);
		bounds.Y1 = Math.max(bounds.Y1, 0);
		bounds.X2 = Math.min(bounds.X2, naturalWidth);
		bounds.Y2 = Math.min(bounds.Y2, naturalHeight);
		return bounds;
	}

	function GetFacesBounds() {
		let persons = image[1].people as PersonWithFacesResponseDto[];
		persons = persons.filter((x) => x.name);
		
		const faceScale = 1.5;
		let faceBoundsX1 = 1000000;
		let faceBoundsY1 = 1000000;
		let faceBoundsX2 = 0;
		let faceBoundsY2 = 0;
		for (let i = 0; i < persons.length; i++) {
			for (let j = 0; j < persons[i].faces.length; j++) {
				let bounds = GetFaceBounds(persons[i].faces[j], faceScale);
				faceBoundsX1 = Math.min(bounds.X1, faceBoundsX1);
				faceBoundsY1 = Math.min(bounds.Y1, faceBoundsY1);
				faceBoundsX2 = Math.max(bounds.X2, faceBoundsX2);
				faceBoundsY2 = Math.max(bounds.Y2, faceBoundsY2);
			}
		}
		return {X1: faceBoundsX1, Y1: faceBoundsY1, X2: faceBoundsX2, Y2: faceBoundsY2};
	}

	function GetRandomFace() {
		let persons = image[1].people as PersonWithFacesResponseDto[];
		persons = persons.filter((x) => x.name);
		
		let count = 0;
		for (let i = 0; i < persons.length; i++) {
			count += persons[i].faces.length;
		}
		
		let index = Math.floor(Math.random() * count);
		count = 0;
		for (let i = 0; i < persons.length; i++) {
			for (let j = 0; j < persons[i].faces.length; j++) {
				if (count == index)
					return persons[i].faces[j];
				count++;
			}
		}
		return null;
	}

	onMount(() => {
		function updateImageMetrics() {
			naturalWidth = imageElement.naturalWidth;
			naturalHeight = imageElement.naturalHeight;
			frameWidth = frameElement.clientWidth;
			frameHeight = frameElement.clientHeight;

			// 1. fill screen without letterbox
			centerX = naturalWidth / 2;
			centerY = naturalHeight / 2;
			
			let aspectImg = naturalWidth / naturalHeight;
			let aspectDiv = frameWidth / frameHeight;
			
			if (imageFill) {
				if (aspectImg > aspectDiv) {
					scale = frameHeight / naturalHeight;
				} else {
					scale = frameWidth / naturalWidth;
				}
			} else {
				if (aspectImg > aspectDiv) {
					scale = frameWidth / naturalWidth;
				} else {
					scale = frameHeight / naturalHeight;
				}
			}

			focusX = centerX;
			focusY = centerY;

			if (!hasPerson) {
				return;
			}

			const faceBounds = GetFacesBounds();

			// todo: add option
			const focusSingle = false;
			const focusBounds = focusSingle ? GetFaceBounds(focusFace, 1) : faceBounds;
			focusX = (focusBounds.X2+focusBounds.X1)/2;
			focusY = (focusBounds.Y2+focusBounds.Y1)/2;
			
			// 2. decrease scale until face rect could fit into screen
			const faceBoundsWidth = faceBounds.X2 - faceBounds.X1;
			const faceBoundsHeight = faceBounds.Y2 - faceBounds.Y1;
			scale *= Math.min(frameWidth / (scale * faceBoundsWidth), 1)
			scale *= Math.min(frameHeight / (scale * faceBoundsHeight), 1)
			
			// 3. move center until faces are on screen
			const visibleWidth = frameWidth / scale;
			const visibleHeight = frameHeight / scale;
			
			const visibleX1 = centerX - (visibleWidth / 2);
			const visibleX2 = centerX + (visibleWidth / 2);
			const visibleY1 = centerY - (visibleHeight / 2);
			const visibleY2 = centerY + (visibleHeight / 2);

			if (visibleX1 > faceBounds.X1) {
				centerX -= visibleX1 - faceBounds.X1;
			} else if (visibleX2 < faceBounds.X2) {
				centerX += faceBounds.X2 - visibleX2;
			}

			if (visibleY1 > faceBounds.Y1) {
				centerY -= visibleY1 - faceBounds.Y1;
			} else if (visibleY2 < faceBounds.Y2) {
				centerY += faceBounds.Y2 - visibleY2;
			}
		}
		
		// random face once
		focusFace = GetRandomFace();
		
		updateImageMetrics();

		imageElement.addEventListener('load', updateImageMetrics);
		window.addEventListener('resize', updateImageMetrics);

		return () => {
			imageElement.removeEventListener('load', updateImageMetrics);
			window.removeEventListener('resize', updateImageMetrics);
		};
	});
	
	function zoomEffect() {
		return 0.5 > Math.random();
	}
</script>

<div bind:this={frameElement} class="immichframe_image overflow-hidden">
	{#if debug}
		<div
			class="face z-[900] bg-red-600 absolute"
			style="top: {(-centerY*scale + frameHeight/2) + GetFacesBounds().Y1*scale}px;
			left: {(-centerX*scale + frameWidth/2) + GetFacesBounds().X1*scale}px;
			width: {(GetFacesBounds().X2-GetFacesBounds().X1)*scale}px;
			height: {(GetFacesBounds().Y2-GetFacesBounds().Y1)*scale}px;"
		></div>
	{/if}
	
	<img
		bind:this={imageElement}
		style="
			max-width: none;
			transform-origin: 0px 0px;
			width: {naturalWidth*scale}px; height: {naturalHeight*scale}px;
			
			--interval: {interval + 2}s;
			--focusX: {-focusX*scale}px;
			--focusY: {-focusY*scale}px;
			--centerX: {-centerX*scale}px;
			--centerY: {-centerY*scale}px;
			--frameX: {frameWidth/2}px;
			--frameY: {frameHeight/2}px;
			--zoom: {hasPerson ? 1.5 : 1.3};
		"
		class="{imageZoom
			? zoomEffect()
				? 'zoom-in'
				: 'zoom-out'
			: 'static'}"
		src={image[0]}
		alt="data"
	/>
</div>
<AssetInfo asset={image[1]} {showLocation} {showPhotoDate} {showImageDesc} {showPeopleDesc} />
<img
		class="absolute flex w-full h-full z-[-1]"
		src={thumbHashToDataURL(decodeBase64(image[1].thumbhash ?? ''))}
		alt="data"
/>

<style>
	.static {
		transform: translate(var(--frameX), var(--frameY)) translate(var(--centerX), var(--centerY));
	}
	.zoom-in {
		animation: zoom-in var(--interval) ease-out normal forwards;
	}
	.zoom-out {
		animation: zoom-out var(--interval) ease-out normal forwards;
	}

	@keyframes zoom-in {
		from {
			transform: translate(var(--frameX), var(--frameY)) translate(var(--centerX), var(--centerY));
		}
		to {
			transform: translate(var(--frameX), var(--frameY)) scale(var(--zoom)) translate(var(--focusX), var(--focusY));
		}
	}
	
	@keyframes zoom-out {
		from {
			transform: translate(var(--frameX), var(--frameY)) scale(var(--zoom)) translate(var(--focusX), var(--focusY));
		}
		to {
			transform: translate(var(--frameX), var(--frameY)) translate(var(--centerX), var(--centerY));
		}
	}
</style>