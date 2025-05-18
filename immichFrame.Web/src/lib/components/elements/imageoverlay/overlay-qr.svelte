<script lang="ts">
	import QrCode from 'svelte-qrcode';

	interface Props {
		baseUrl: string;
		id: string;
	}
	let { baseUrl, id }: Props = $props();

	let imageUrl = $derived(baseUrl + '/photos/' + id);
</script>

<div class="border-8 border-white rounded-lg self-center">
	<QrCode value={imageUrl} />
</div>
<!-- <a
	href={imageUrl}
	class="text-xl border-gray-400 border-2 rounded-md p-2 m-2 text-center text-primary"
	>Open in immich</a
> -->
