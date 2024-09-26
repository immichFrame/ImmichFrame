<script lang="ts">
	import { type AssetResponseDto } from '$lib/immichFrameApi';
	import { fallbackLocale } from '$lib/constants';
	export let asset: AssetResponseDto;

	$: assetDate = asset.exifInfo?.dateTimeOriginal ?? '';

	$: time = new Date(assetDate);

	$: formattedDate = time.toLocaleString(fallbackLocale.code, {
		year: 'numeric',
		month: '2-digit',
		day: '2-digit'
	});
	$: timePortion = time.toLocaleString(fallbackLocale.code, {
		hour: '2-digit',
		minute: '2-digit'
	});
	$: selectedDate = `${formattedDate} ${timePortion}`;
</script>

{#if selectedDate}
	<div
		class="absolute bottom-0 right-0 z-10 text-primary bg-secondary bg-opacity-40 rounded-tl-2xl p-3"
	>
		<p class="mt-2 text-lg">{selectedDate}</p>
	</div>
{/if}
