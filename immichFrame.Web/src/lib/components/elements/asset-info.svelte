<script lang="ts">
	import { type AlbumResponseDto, type AssetResponseDto } from '$lib/immichFrameApi';
	import { format } from 'date-fns';
	import * as locale from 'date-fns/locale';
	import { configStore } from '$lib/stores/config.store';
	import Icon from './icon.svelte';
	import { mdiCalendar, mdiMapMarker, mdiAccount, mdiText, mdiImageAlbum } from '@mdi/js';

	interface Props {
		asset: AssetResponseDto;
		albums: AlbumResponseDto[];
		showLocation: boolean;
		showPhotoDate: boolean;
		showImageDesc: boolean;
		showPeopleDesc: boolean;
		showAlbumName: boolean;
	}

	let {
		asset,
		albums,
		showLocation,
		showPhotoDate,
		showImageDesc,
		showPeopleDesc,
		showAlbumName
	}: Props = $props();

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
	console.log(asset.exifInfo);
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

{#if showPhotoDate || showLocation || showImageDesc || showPeopleDesc || showAlbumName}
	<div
		id="imageinfo"
		class="immichframe_image_metadata absolute bottom-0 right-0 z-100 text-primary p-1 text-right
		{$configStore.style == 'solid' ? 'bg-secondary rounded-tl-2xl' : ''}
		{$configStore.style == 'transition' ? 'bg-gradient-to-l from-secondary from-0% pl-10' : ''}
		{$configStore.style == 'blur' ? 'backdrop-blur-lg rounded-tl-2xl' : ''}	"
	>
		{#if showPhotoDate && formattedDate}
			<p id="photodate" class="info-item">
				<Icon path={mdiCalendar} class="info-icon" />
				{formattedDate}
			</p>
		{/if}
		{#if showImageDesc && desc}
			<p id="imagedescription" class="info-item">
				<Icon path={mdiText} class="info-icon" />
				{desc}
			</p>
		{/if}
		{#if showAlbumName && albums && albums.length > 0}
			<p id="imagealbums" class="info-item">
				<Icon path={mdiImageAlbum} />
				{albums.map((x) => x.albumName).join(', ')}
			</p>
		{/if}
		{#if showPeopleDesc && availablePeople && availablePeople.length > 0}
			<p id="peopledescription" class="info-item">
				<Icon path={mdiAccount} />
				{availablePeople.map((x) => x.name).join(', ')}
			</p>
		{/if}
		{#if showLocation && location}
			<p id="imagelocation" class="info-item">
				<Icon path={mdiMapMarker} />
				{location}
			</p>
		{/if}
	</div>
{/if}

<style>
	.info-item {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		margin: 0.2rem 0.5rem;
	}
</style>
