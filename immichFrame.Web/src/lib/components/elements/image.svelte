<script lang="ts">
	export let data: Blob;
	const getUrl = (data: Blob) => {
		let url = URL.createObjectURL(data);
		return url;
	};
</script>

<div>
	<img class="imageView" src={getUrl(data)} alt="data" />
</div>

<style>
	.imageView {
		height: 100vh; /* Image height will be constrained to the viewport height */
		max-width: 100%; /* Ensure the image doesn't exceed its original width */
		object-fit: contain; /* Maintain the aspect ratio */
	}
</style>
