<script lang="ts">
	import QrCode from 'svelte-qrcode';

	interface Props {
		baseUrl: string;
		id: string;
	}
	let { baseUrl, id }: Props = $props();
</script>

<div class="border-8 border-white rounded-lg m-5 self-center">
	<QrCode value={baseUrl + '/photos/' + id} />
</div>
