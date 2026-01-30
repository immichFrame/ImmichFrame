<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import {
		type AlbumResponseDto,
		type AssetResponseDto,
		type PersonWithFacesResponseDto
	} from '$lib/immichFrameApi';
	import { isVideoAsset } from '$lib/constants/asset-type';
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';
	import AssetInfo from '$lib/components/elements/asset-info.svelte';
	import ImageOverlay from '$lib/components/elements/imageoverlay/image-overlay.svelte';

	interface Props {
		image: [url: string, asset: AssetResponseDto, albums: AlbumResponseDto[]];
		showLocation: boolean;
		showPhotoDate: boolean;
		showImageDesc: boolean;
		showPeopleDesc: boolean;
		showTagsDesc: boolean;
		showAlbumName: boolean;
		imageFill: boolean;
		imageZoom: boolean;
		imagePan: boolean;
		interval: number;
		split: boolean;
		showInfo: boolean;
		playAudio: boolean;
	}

	let {
		image,
		showLocation,
		showPhotoDate,
		showImageDesc,
		showPeopleDesc,
		showTagsDesc,
		showAlbumName,
		imageFill,
		imageZoom,
		imagePan,
		interval,
		split,
		showInfo = $bindable(false),
		playAudio
	}: Props = $props();

	const dispatch = createEventDispatcher<{ ended: void }>();

	let debug = false;
	const isVideo = $derived(isVideoAsset(image[1]));

	let videoElement = $state<HTMLVideoElement | null>(null);

	let hasPerson = $derived(image[1].people?.filter((x) => x.name).length ?? 0 > 0);
	let zoomIn = $derived(zoomEffect());
	let panDirection = $derived(panEffect());
	const enableZoom = $derived(imageZoom && !isVideo);
	const enablePan = $derived(imagePan && !isVideo);

	function GetFace(i: number) {
		const people = image[1].people as PersonWithFacesResponseDto[];
		const namedPeople = people.filter((x) => x.name);
		return namedPeople[i]?.faces[0] ?? null;
	}

	function getFaceMetric(
		i: number,
		prop: 'x1' | 'x2' | 'y1' | 'y2' | 'centerX' | 'centerY' | 'width' | 'height'
	): number {
		const face = GetFace(i);
		if (!face) return 0;

		const {
			boundingBoxX1 = 0,
			boundingBoxX2 = 0,
			boundingBoxY1 = 0,
			boundingBoxY2 = 0,
			imageWidth = 1,
			imageHeight = 1
		} = face;

		switch (prop) {
			case 'x1':
				return calcPercent(boundingBoxX1, imageWidth);
			case 'x2':
				return calcPercent(boundingBoxX2, imageWidth);
			case 'y1':
				return calcPercent(boundingBoxY1, imageHeight);
			case 'y2':
				return calcPercent(boundingBoxY2, imageHeight);
			case 'width':
				return calcPercent(boundingBoxX2 - boundingBoxX1, imageWidth);
			case 'height':
				return calcPercent(boundingBoxY2 - boundingBoxY1, imageHeight);
			case 'centerX':
				return calcPercent(boundingBoxX1 + (boundingBoxX2 - boundingBoxX1) / 2, imageWidth);
			case 'centerY':
				return calcPercent(boundingBoxY1 + (boundingBoxY2 - boundingBoxY1) / 2, imageHeight);
		}
	}

	function calcPercent(number1: number, number2: number) {
		return (number1 / number2) * 100;
	}

	function zoomEffect() {
		return 0.5 > Math.random();
	}

	function panEffect() {
		const directions = ['left', 'right', 'up', 'down'];
		return directions[Math.floor(Math.random() * directions.length)];
	}

	function getScaleValues() {
		if (imageZoom && imagePan) {
			// When both zoom and pan are enabled make sure we have minimum zoom to cover pan offset
			const minScale = 1.15;
			const maxScale = 1.35;
			return {
				startScale: zoomIn ? minScale : maxScale,
				endScale: zoomIn ? maxScale : minScale
			};
		} else if (imageZoom) {
			// Original zoom behavior when only zoom is enabled
			return {
				startScale: zoomIn ? 1 : 1.3,
				endScale: zoomIn ? 1.3 : 1
			};
		} else {
			// No zoom but pan give pan slight scale to avoid edges
			const panScale = imagePan ? 1.1 : 1;
			return {
				startScale: panScale,
				endScale: panScale
			};
		}
	}

	let scaleValues = $derived(getScaleValues());

	export const pause = async () => {
		if (isVideo && videoElement) {
			videoElement.pause();
		}
	};

	export const play = async () => {
		if (isVideo && videoElement) {
			try {
				await videoElement.play();
			} catch {
				// Autoplay might be blocked; ignore.
			}
		}
	};
</script>

{#if showInfo}
	<ImageOverlay asset={image[1]} albums={image[2]} />
{/if}

<div class="immichframe_image relative place-self-center overflow-hidden">
	<!-- Container with zoom-effect -->
	<div
		class="relative w-full h-full {enableZoom ? 'zoom' : ''} {enablePan ? 'pan' : ''}"
		style="
			--interval: {interval + 2}s;
			--originX: {hasPerson ? getFaceMetric(0, 'centerX') + '%' : 'center'};
			--originY: {hasPerson ? getFaceMetric(0, 'centerY') + '%' : 'center'};
			--start-scale: {scaleValues.startScale};
			--end-scale: {scaleValues.endScale};
			--pan-start-x: {panDirection === 'left' ? '5%' : panDirection === 'right' ? '-5%' : '0'};
			--pan-end-x: {panDirection === 'left' ? '-5%' : panDirection === 'right' ? '5%' : '0'};
			--pan-start-y: {panDirection === 'up' ? '5%' : panDirection === 'down' ? '-5%' : '0'};
			--pan-end-y: {panDirection === 'up' ? '-5%' : panDirection === 'down' ? '5%' : '0'};"
	>
		{#if debug}
			{#each image[1].people?.map((x) => x.name) ?? [] as _, i}
				<div
					class="face z-[900] bg-red-600 absolute"
					style="top: {getFaceMetric(i, 'y1')}%;
						   left: {getFaceMetric(i, 'x1')}%;
						   width: {getFaceMetric(i, 'width')}%;
						   height: {getFaceMetric(i, 'height')}%;"
				></div>
				<div
					class="centerface z-[999] w-1 h-1 bg-blue-600 absolute"
					style="top: {getFaceMetric(i, 'centerY')}%;
						   left: {getFaceMetric(i, 'centerX')}%;"
				></div>
			{/each}
		{/if}

		{#if isVideo}
			<video
				bind:this={videoElement}
				class="{imageFill
					? 'w-screen max-h-screen h-dvh-safe object-cover'
					: 'max-h-screen h-dvh-safe max-w-full object-contain'} w-full h-full"
				src={image[0]}
				autoplay
				muted={!playAudio}
				playsinline
				poster={thumbHashToDataURL(decodeBase64(image[1].thumbhash ?? ''))}
				onended={() => dispatch('ended')}
			></video>
		{:else}
			<img
				class="{imageFill
					? 'w-screen max-h-screen h-dvh-safe object-cover'
					: 'max-h-screen h-dvh-safe max-w-full object-contain'} w-full h-full"
				src={image[0]}
				alt="data"
			/>
		{/if}
	</div>
</div>
<AssetInfo
	asset={image[1]}
	albums={image[2]}
	{showLocation}
	{showPhotoDate}
	{showImageDesc}
	{showPeopleDesc}
	{showTagsDesc}
	{showAlbumName}
	{split}
/>
<img
	class="absolute flex w-full h-full z-[-1]"
	src={thumbHashToDataURL(decodeBase64(image[1].thumbhash ?? ''))}
	alt="data"
/>

<style>
	.zoom {
		animation: zoom var(--interval) ease-out forwards;
		transform-origin: var(--originX, center) var(--originY, center);
	}

	.pan {
		animation: pan var(--interval) ease-in-out forwards;
	}

	.zoom.pan {
		animation: zoom-pan var(--interval) ease-in-out forwards;
		transform-origin: var(--originX, center) var(--originY, center);
	}

	@keyframes zoom {
		from {
			transform: scale(var(--start-scale, 1));
		}
		to {
			transform: scale(var(--end-scale, 1.3));
		}
	}

	@keyframes pan {
		from {
			transform: translateX(var(--pan-start-x, 0)) translateY(var(--pan-start-y, 0))
				scale(var(--start-scale, 1));
		}
		to {
			transform: translateX(var(--pan-end-x, 0)) translateY(var(--pan-end-y, 0))
				scale(var(--end-scale, 1));
		}
	}

	@keyframes zoom-pan {
		from {
			transform: translateX(var(--pan-start-x, 0)) translateY(var(--pan-start-y, 0))
				scale(var(--start-scale, 1));
		}
		to {
			transform: translateX(var(--pan-end-x, 0)) translateY(var(--pan-end-y, 0))
				scale(var(--end-scale, 1.3));
		}
	}
</style>
