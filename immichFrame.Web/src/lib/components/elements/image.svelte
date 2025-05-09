<script lang="ts">
	import { type AssetResponseDto, type PersonWithFacesResponseDto } from '$lib/immichFrameApi';
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';
	import AssetInfo from './asset-info.svelte';

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

	let hasPerson = $derived(image[1].people?.filter((x) => x.name).length ?? 0 > 0);
	let zoomIn = $derived(zoomEffect());

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
</script>

<div class="immichframe_image relative place-self-center overflow-hidden">
	<!-- Container with zoom-effect -->
	<div
		class="relative w-full h-full {imageZoom ? 'zoom' : ''}"
		style="
			--interval: {interval + 2}s;
			--originX: {hasPerson ? getFaceMetric(0, 'centerX') + '%' : 'center'};
			--originY: {hasPerson ? getFaceMetric(0, 'centerY') + '%' : 'center'};
			--start-scale: {zoomIn ? 1 : 1.3};
			--end-scale: {zoomIn ? 1.3 : 1};"
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

		<img
			class="{multi || imageFill
				? 'w-screen h-dvh object-cover'
				: 'max-h-screen h-dvh max-w-full object-contain'} w-full h-full"
			src={image[0]}
			alt="data"
		/>
	</div>
</div>
<AssetInfo asset={image[1]} {showLocation} {showPhotoDate} {showImageDesc} {showPeopleDesc} />
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

	@keyframes zoom {
		from {
			transform: scale(var(--start-scale, 1));
		}
		to {
			transform: scale(var(--end-scale, 1.3));
		}
	}
</style>
