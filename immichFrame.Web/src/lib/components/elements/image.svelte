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
</script>

{#key dataUrl}
	<div
		transition:fade={{ duration: ($configStore.transitionDuration ?? 1) * 1000 }}
		class="absolute place-self-center"
	>
		<img class="max-h-screen max-w-full object-contain" src={dataUrl} alt="data" />
		<AssetInfo {asset} {showLocation} {showPhotoDate} {showImageDesc} />
	</div>
	<img
		transition:fade={{ duration: ($configStore.transitionDuration ?? 1) * 1000 }}
		class="absolute top-0 left-0 flex w-full h-full z-[-1]"
		src={thumbHashToDataURL(decodeBase64(thumbHash))}
		alt="data"
	/>
{/key}
