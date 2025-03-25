<script lang="ts">
	import { type AssetResponseDto, type PersonWithFacesResponseDto } from '$lib/immichFrameApi';
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';
	import { configStore } from '$lib/stores/config.store';
	import AssetInfo from './asset-info.svelte';

	interface Props {
		image: [url: string, asset: AssetResponseDto];
		showLocation: boolean;
		showPhotoDate: boolean;
		showImageDesc: boolean;
		showPeopleDesc: boolean;
		multi?: boolean;
	}

	let {
		image,
		showLocation,
		showPhotoDate,
		showImageDesc,
		showPeopleDesc,
		multi = false
	}: Props = $props();

	let debug = false;

	let interval = $configStore.interval ?? 1;

	let hasPerson = $derived(image[1].people?.filter((x) => x.name).length ?? 0 > 0);

	function GetFace(i: number) {
		let person = image[1].people as PersonWithFacesResponseDto[];

		person = person.filter((x) => x.name);

		return person[i].faces[0];
	}

	function GetPosX(i: number) {
		if (hasPerson) {
			let face = GetFace(i);
			if (!face) return;
			return calcPercent(face.boundingBoxX1 ?? 0, face.imageWidth ?? 1);
		} else {
			return 0;
		}
	}

	function GetPosY(i: number) {
		if (hasPerson) {
			let face = GetFace(i);
			if (!face) return;
			return calcPercent(face.boundingBoxY1 ?? 0, face.imageHeight ?? 1);
		} else {
			return 0;
		}
	}

	function getCenterX(i: number) {
		if (hasPerson && $configStore.imageZoom) {
			let face = GetFace(i);
			if (!face) return;

			let midX = ((face.boundingBoxX2 ?? 0) - (face.boundingBoxX1 ?? 0)) / 2;

			let part = (face.boundingBoxX1 ?? 0) + midX;

			return calcPercent(part, face.imageWidth ?? 1);
		} else {
			return 0;
		}
	}

	function getCenterY(i: number) {
		if (hasPerson && $configStore.imageZoom) {
			let face = GetFace(i);
			if (!face) return;

			let midY = ((face.boundingBoxY2 ?? 0) - (face.boundingBoxY1 ?? 0)) / 2;

			let part = (face.boundingBoxY1 ?? 0) + midY;

			return calcPercent(part, face.imageHeight ?? 1);
		} else {
			return 0;
		}
	}

	function getWidth(i: number) {
		if (hasPerson) {
			let face = GetFace(i);
			if (!face) return;

			return calcPercent(
				(face.boundingBoxX2 ?? 0) - (face.boundingBoxX1 ?? 0),
				face.imageWidth ?? 1
			);
		} else {
			return 0;
		}
	}

	function getHeight(i: number) {
		if (hasPerson) {
			let face = GetFace(i);
			if (!face) return;

			return calcPercent(
				(face.boundingBoxY2 ?? 0) - (face.boundingBoxY1 ?? 0),
				face.imageHeight ?? 1
			);
		} else {
			return 0;
		}
	}

	function calcPercent(number1: number, number2: number) {
		return (number1 / number2) * 100;
	}

	function zoomEffect() {
		return 0.5 > Math.random();
	}
</script>

<div class="immichframe_image place-self-center overflow-hidden">
	{#if debug}
		{#each image[1].people?.map((x) => x.name) ?? [] as _, i}
			<div
				class="face z-[900] bg-red-600 absolute"
				style="top: {GetPosY(i)}%;
					left: {GetPosX(i)}%;
					width: {getWidth(i)}%;
					height: {getHeight(i)}%;"
			></div>
			<div
				class="centerface z-[999] w-1 h-1 bg-blue-600 absolute"
				style="top: {getCenterY(i)}%;
					left: {getCenterX(i)}%;"
			></div>
		{/each}
	{/if}

	<img
		style="--interval: {interval + 2}s; --posX: {getCenterX(0)}%; --posY: {getCenterY(0)}%;"
		class="{multi
			? 'w-screen'
			: 'max-h-screen h-screen max-w-full'} object-contain {$configStore.imageZoom
			? zoomEffect()
				? hasPerson
					? 'zoom-in-person'
					: 'zoom-in'
				: hasPerson
					? 'zoom-out-person'
					: 'zoom-out'
			: ''}"
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
	.zoom-in {
		animation: zoom-in var(--interval) ease-out normal forwards;
	}
	.zoom-in-person {
		animation: zoom-in-person var(--interval) ease-out normal forwards;
	}
	.zoom-out {
		animation: zoom-out var(--interval) ease-out normal forwards;
	}
	.zoom-out-person {
		animation: zoom-out-person var(--interval) ease-out normal forwards;
	}

	@keyframes zoom-in {
		from {
			transform: scale(1);
		}
		to {
			transform: scale(1.3);
		}
	}

	@keyframes zoom-in-person {
		from {
			transform: scale(1);
			transform-origin: center;
		}
		to {
			transform: scale(1.5);
			transform-origin: var(--posX) var(--posY);
		}
	}

	@keyframes zoom-out {
		from {
			transform: scale(1.3);
		}
		to {
			transform: scale(1);
		}
	}

	@keyframes zoom-out-person {
		from {
			transform: scale(1.5);
			transform-origin: var(--posX) var(--posY);
		}
		to {
			transform: scale(1);
			transform-origin: center;
		}
	}
</style>
