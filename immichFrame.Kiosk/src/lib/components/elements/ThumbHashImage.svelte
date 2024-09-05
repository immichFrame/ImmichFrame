<script lang="ts">
	import { decodeBase64 } from '$lib/utils';
	import { thumbHashToDataURL } from 'thumbhash';
	export let thumbHash: string;
</script>

<img class="thumbHashImage" src={thumbHashToDataURL(decodeBase64(thumbHash))} alt="data" />

<style>
	.thumbHashImage {
		position: absolute;
		top: 0;
		left: 0;
		display: flex;
		width: 100%;
		height: 100%;
		z-index: -1;
	}
</style>
