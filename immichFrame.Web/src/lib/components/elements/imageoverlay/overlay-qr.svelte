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
<a
	href={imageUrl}
	target="_blank"
	class="text-xl w-fit self-center rounded-md p-2 m-2 text-center text-secondary bg-primary"
	>Open in immich</a
>
