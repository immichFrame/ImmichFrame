<script lang="ts">
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';

	export let thumbHash: string;
</script>

<img
	class="absolute top-0 left-0 flex w-full h-full z-[-1]"
	src={thumbHashToDataURL(decodeBase64(thumbHash))}
	alt="data"
/>
