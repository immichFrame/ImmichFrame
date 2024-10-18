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

	let transitionDuration = ($configStore.transitionDuration ?? 1) * 1000;

	let interval = $configStore.interval ?? 1;

	$: hasPerson = asset.people?.length ?? 0 > 0;

	function GetFace() {
		let person = asset.people as PersonWithFacesResponseDto[];

		person = person.filter((x) => x.name);

		return person[0].faces[0];
	}

	function GetPosX() {
		if (hasPerson) {
			let face = GetFace();

			let midX = (face.boundingBoxX2 ?? 0) - (face.boundingBoxX1 ?? 0);

			let part = (face.boundingBoxX1 ?? 0) + midX;

			return (part / (face.imageWidth ?? 1)) * 100;
		} else {
			return 0;
		}
	}

	function GetPosY() {
		if (hasPerson) {
			let face = GetFace();

			let midY = (face.boundingBoxY2 ?? 0) - (face.boundingBoxY1 ?? 0);

			let part = (face.boundingBoxY1 ?? 0) + midY;

			return (part / (face.imageHeight ?? 1)) * 100;
		} else {
			return 0;
		}
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
		<img
			style="--interval: {interval + 2}s; --posX: {GetPosX()}%; --posY: {GetPosY()}%;"
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
