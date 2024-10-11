<script lang="ts">
	import { type AssetResponseDto } from '$lib/immichFrameApi';
	import { configStore } from '$lib/stores/config.store';
	import { format } from 'date-fns';
	export let asset: AssetResponseDto;

	$: assetDate = asset.exifInfo?.dateTimeOriginal ?? '';

	$: time = new Date(assetDate);

	$: formattedDate = format(time, $configStore.photoDateFormat ?? 'dd.MM.yyyy');
	$: timePortion = format(time, $configStore.clockFormat ?? 'HH:mm:ss');
	$: selectedDate = `${formattedDate} ${timePortion}`;
</script>

{#if selectedDate}
	<div class="absolute bottom-0 right-0 z-100 text-primary p-3">
		<p class="mt-2 text-lg">{selectedDate}</p>
	</div>
{/if}
