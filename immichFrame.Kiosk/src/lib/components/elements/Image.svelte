<script lang="ts">
	import ThumbHashImage from '../elements/ThumbHashImage.svelte';
	export let data: Blob;
	export let thumbHash;
	const getUrl = (data: Blob) => {
		let url = URL.createObjectURL(data);
		return url;
	};
</script>

<div>
	<ThumbHashImage {thumbHash} />
	<img class="imageView" src={getUrl(data)} alt="data" />
</div>

<style>
	.imageView {
		height: 100vh; /* Image height will be constrained to the viewport height */
		max-width: 100%; /* Ensure the image doesn't exceed its original width */
		object-fit: contain; /* Maintain the aspect ratio */
	}
</style>
