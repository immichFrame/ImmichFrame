<script lang="ts">
	import { type AssetResponseDto } from '$lib/immichFrameApi';
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
			style="--interval: {interval + 2}s;"
			class="max-h-screen h-screen max-w-full object-contain {$configStore.imageZoom
				? zoomEffect()
					? 'zoom-in'
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
		-webkit-animation: zoom-in var(--interval) ease-out normal;
		animation: zoom-in var(--interval) ease-out normal;
		-webkit-font-smoothing: antialiased;
	}
	.zoom-out {
		-webkit-animation: zoom-out var(--interval) ease-out normal;
		animation: zoom-out var(--interval) ease-out normal;
		-webkit-font-smoothing: antialiased;
	}

	@-webkit-keyframes zoom-in {
		from {
			-webkit-transform: scale(1);
			transform: scale(1);
		}
		to {
			-webkit-transform: scale(1.3);
			transform: scale(1.3);
		}
	}

	@keyframes zoom-in {
		from {
			-webkit-transform: scale(1);
			transform: scale(1);
		}
		to {
			-webkit-transform: scale(1.3);
			transform: scale(1.3);
		}
	}

	@-webkit-keyframes zoom-out {
		from {
			-webkit-transform: scale(1.3);
			transform: scale(1.3);
		}
		to {
			-webkit-transform: scale(1);
			transform: scale(1);
		}
	}

	@keyframes zoom-out {
		from {
			-webkit-transform: scale(1.3);
			transform: scale(1.3);
		}
		to {
			-webkit-transform: scale(1);
			transform: scale(1);
		}
	}
</style>
