<script lang="ts">
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';
	import { fade } from 'svelte/transition';
	import { configStore } from '$lib/stores/config.store';

	export let thumbHash: string;
	export let dataUrl: string;
</script>

{#key dataUrl}
	<img
		transition:fade={{ duration: ($configStore.transitionDuration ?? 1) * 1000 }}
		class="absolute h-screen max-w-full m-0 object-contain place-self-center"
		src={dataUrl}
		alt="data"
	/>
	<img
		transition:fade={{ duration: ($configStore.transitionDuration ?? 1) * 1000 }}
		class="absolute top-0 left-0 flex w-full h-full z-[-1]"
		src={thumbHashToDataURL(decodeBase64(thumbHash))}
		alt="data"
	/>
{/key}
