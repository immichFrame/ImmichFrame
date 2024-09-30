<script lang="ts">
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';

	export let thumbHash: string;
	export let dataUrl: string;
</script>

<div>
	<img class="h-screen max-w-full object-contain" src={dataUrl} alt="data" />
	<img
		class="absolute top-0 left-0 flex w-full h-full z-[-1]"
		src={thumbHashToDataURL(decodeBase64(thumbHash))}
		alt="data"
	/>
</div>
