<script lang="ts">
	import { type AssetResponseDto } from '$lib/immichFrameApi';
	import { format } from 'date-fns';
	import { configStore } from '$lib/stores/config.store';

	export let asset: AssetResponseDto;
	export let showLocation: boolean;
	export let showPhotoDate: boolean;
	export let showImageDesc: boolean;

	$: assetDate = asset.exifInfo?.dateTimeOriginal ?? '';
	$: desc = asset.exifInfo?.description ?? '';

	$: time = new Date(assetDate);

	$: formattedDate = format(time, $configStore.photoDateFormat ?? 'dd.MM.yyyy');

	let location: string;

	$: location = [asset.exifInfo?.city, asset.exifInfo?.country].filter((x) => x).join(', ');
</script>

{#if showPhotoDate || showPhotoDate || showImageDesc}
	<div class="absolute bottom-0 right-0 z-100 text-primary p-1">
		{#if showPhotoDate && formattedDate}
			<p class="text-sm text-right font-thin">{formattedDate}</p>
		{/if}
		{#if showImageDesc || desc}
			<p class="text-base text-right font-light">{desc}</p>
		{/if}
		{#if showLocation && location}
			<p class="text-base text-right font-light">{location}</p>
		{/if}
	</div>
{/if}
