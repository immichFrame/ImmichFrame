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

	$: location = formatLocation(
		$configStore.imageLocationFormat ?? 'City,State,Country',
		asset.exifInfo?.city ?? '',
		asset.exifInfo?.state ?? '',
		asset.exifInfo?.country ?? ''
	);

	$: availablePeople = asset.people?.filter((x) => x.name);
	function formatLocation(format: string, city?: string, state?: string, country?: string) {
    const locationParts: Set<string> = new Set();

    format.split(',').forEach(part => {
        const trimmedPart = part.trim();
        if (trimmedPart.toLowerCase() === 'city' && city) {
            locationParts.add(city);
        } else if (trimmedPart.toLowerCase() === 'state' && state) {
            locationParts.add(state);
        } else if (trimmedPart.toLowerCase() === 'country' && country) {
            locationParts.add(country);
        }
    });

    return Array.from(locationParts).join(', ');
}
</script>

{#if showPhotoDate || showPhotoDate || showImageDesc}
	<div class="absolute bottom-0 right-0 z-100 text-primary p-1 text-right">
		{#if showPhotoDate && formattedDate}
			<p class="text-sm font-thin">{formattedDate}</p>
		{/if}
		{#if showImageDesc || desc}
			<p class="text-base font-light">{desc}</p>
		{/if}
		{#if availablePeople}
			<p class="text-sm font-light">
				{availablePeople.map((x) => x.name).join(', ')}
			</p>
		{/if}
		{#if showLocation && location}
			<p class="text-base font-light">{location}</p>
		{/if}
	</div>
{/if}
