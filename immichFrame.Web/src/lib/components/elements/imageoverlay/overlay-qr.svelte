<script lang="ts">
	import QrCode from '@castlenine/svelte-qrcode';

	interface Props {
		baseUrl: string;
		id: string;
	}
	let { baseUrl, id }: Props = $props();

	let imageUrl = `${baseUrl}/photos/${id}`;
</script>

<div
	class="info-overlay-qrcode border-4 border-white rounded-lg self-center min-h-[128px] min-w-[128px] grow max-h-[200px] max-w-[200px] aspect-[1/1]"
>
	<QrCode data={imageUrl} isResponsive />
</div>
<a
	href={imageUrl}
	target="_blank"
	class="info-overlay-openimmich text-xl w-fit self-center rounded-md p-2 m-2 text-center text-secondary bg-primary"
	>Open in immich</a
>
