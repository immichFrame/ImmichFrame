<script lang="ts">
	import { type AssetResponseDto, type PersonWithFacesResponseDto } from '$lib/immichFrameApi';
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';
	import { fade } from 'svelte/transition';
	import { configStore } from '$lib/stores/config.store';
	import AssetInfo from './asset-info.svelte';

	export let thumbHash: string;
	export let dataUrl: string;
	export let asset: AssetResponseDto;
	export let showLocation: boolean;
	export let showPhotoDate: boolean;
	export let showImageDesc: boolean;

	let debug = false;

	let transitionDuration = ($configStore.transitionDuration ?? 1) * 1000;

	let interval = $configStore.interval ?? 1;

	$: hasPerson = asset.people?.length ?? 0 > 0;

	function GetFace(i: number) {
		let person = asset.people as PersonWithFacesResponseDto[];

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
		if (hasPerson) {
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
		if (hasPerson) {
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

{#key dataUrl}
	<div
		transition:fade={{ duration: transitionDuration }}
		class="absolute place-self-center overflow-hidden"
	>
		{#if debug}
			{#each asset.people?.map((x) => x.name) ?? [] as _, i}
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
			class="max-h-screen h-screen max-w-full object-contain {$configStore.imageZoom
				? zoomEffect()
					? hasPerson
						? 'zoom-in-person'
						: 'zoom-in'
					: hasPerson
						? 'zoom-out-person'
						: 'zoom-out'
				: ''}"
			src={dataUrl}
			alt="data"
		/>
		<AssetInfo {asset} {showLocation} {showPhotoDate} {showImageDesc} />
	</div>
	<img
		transition:fade={{ duration: transitionDuration }}
		class="absolute top-0 left-0 flex w-full h-full z-[-1]"
		src={thumbHashToDataURL(decodeBase64(thumbHash))}
		alt="data"
	/>
{/key}

<style>
	.zoom-in {
		animation: zoom-in var(--interval) ease-out normal;
	}
	.zoom-in-person {
		animation: zoom-in-person var(--interval) ease-out normal;
	}
	.zoom-out {
		animation: zoom-out var(--interval) ease-out normal;
	}
	.zoom-out-person {
		animation: zoom-out-person var(--interval) ease-out normal;
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
