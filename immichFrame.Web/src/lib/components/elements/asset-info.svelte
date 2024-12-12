<script lang="ts">
	import { type AssetResponseDto } from '$lib/immichFrameApi';
	import { format } from 'date-fns';
	import * as locale from 'date-fns/locale';
	import { configStore } from '$lib/stores/config.store';

	interface Props {
		asset: AssetResponseDto;
		showLocation: boolean;
		showPhotoDate: boolean;
		showImageDesc: boolean;
		showPeopleDesc: boolean;
	}

	let { asset, showLocation, showPhotoDate, showImageDesc, showPeopleDesc }: Props = $props();

	function formatLocation(format: string, city?: string, state?: string, country?: string) {
		const locationParts: Array<string> = new Array();

		format.split(',').forEach((part) => {
			const trimmedPart = part.trim().toLowerCase();
			if (trimmedPart === 'city' && city) {
				locationParts.push(city);
			} else if (trimmedPart === 'state' && state) {
				locationParts.push(state);
			} else if (trimmedPart === 'country' && country) {
				locationParts.push(country);
			}
		});

		return Array.from(locationParts).join(', ');
	}
	let assetDate = $derived(asset.exifInfo?.dateTimeOriginal);
	let desc = $derived(asset.exifInfo?.description ?? '');
	let time = $derived(assetDate ? new Date(assetDate) : null);
	const selectedLocale = $configStore.language;

	const localeToUse =
		(selectedLocale && locale[selectedLocale as keyof typeof locale]) || locale.enUS;
	let formattedDate = $derived(
		time
			? format(time, $configStore.photoDateFormat ?? 'dd.MM.yyyy', { locale: localeToUse })
			: null
	);
	let location = $derived(
		formatLocation(
			$configStore.imageLocationFormat ?? 'City,State,Country',
			asset.exifInfo?.city ?? '',
			asset.exifInfo?.state ?? '',
			asset.exifInfo?.country ?? ''
		)
	);
	let availablePeople = $derived(asset.people?.filter((x) => x.name));
</script>

{#if showPhotoDate || showPhotoDate || showImageDesc || showPeopleDesc}
	<div
		class="absolute bottom-0 right-0 z-100 text-primary p-1 text-right
		{$configStore.style == 'solid' ? 'bg-secondary rounded-tl-2xl' : ''}
		{$configStore.style == 'transition' ? 'bg-gradient-to-l from-secondary from-0% pl-10' : ''}
		{$configStore.style == 'blur' ? 'backdrop-blur-lg rounded-tl-2xl' : ''}	"
	>
		{#if showPhotoDate && formattedDate}
			<p class="text-sm font-thin text-shadow-sm">{formattedDate}</p>
		{/if}
		{#if showImageDesc && desc}
			<p class="text-base font-light text-shadow-sm">{desc}</p>
		{/if}
		{#if showPeopleDesc && availablePeople}
			<p class="text-sm font-light text-shadow-sm">
				{availablePeople.map((x) => x.name).join(', ')}
			</p>
		{/if}
		{#if showLocation && location}
			<p class="text-base font-light text-shadow-sm">{location}</p>
		{/if}
	</div>
{/if}
