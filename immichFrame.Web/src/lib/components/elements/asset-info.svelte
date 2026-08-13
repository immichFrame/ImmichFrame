<script lang="ts">
	import { type AlbumResponseDto, type AssetResponseDto } from '$lib/immichFrameApi';
	import { format } from 'date-fns';
	import * as locale from 'date-fns/locale';
	import { configStore } from '$lib/stores/config.store';
	import Icon from './icon.svelte';
	import {
		mdiCalendar,
		mdiMapMarker,
		mdiAccount,
		mdiText,
		mdiImageAlbum,
		mdiTag,
		mdiCamera,
		mdiCameraIris
	} from '@mdi/js';

	interface Props {
		asset: AssetResponseDto;
		albums: AlbumResponseDto[];
		showLocation: boolean;
		showPhotoDate: boolean;
		showImageDesc: boolean;
		showImageCamera: boolean;
		showImageExif: boolean;
		showPeopleDesc: boolean;
		showTagsDesc: boolean;
		showAlbumName: boolean;
		split: boolean;
	}

	let {
		asset,
		albums,
		showLocation,
		showPhotoDate,
		showImageDesc,
		showImageCamera,
		showImageExif,
		showPeopleDesc,
		showTagsDesc,
		showAlbumName,
		split
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

	function trimFloat(value: number) {
		return value.toFixed(2).replace(/\.?0+$/, '');
	}

	function containsWholeWord(value: string, word: string) {
		if (!word) return false;
		const escaped = word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
		return new RegExp(`(^|\\s)${escaped}(?=\\s|$)`, 'i').test(value);
	}

	function formatCamera(make?: string | null, model?: string | null) {
		const trimmedMake = make?.trim() ?? '';
		const trimmedModel = model?.trim() ?? '';

		if (containsWholeWord(trimmedMake, trimmedModel)) {
			return trimmedModel;
		}

		return `${trimmedMake} ${trimmedModel}`.trim();
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
	let imageCamera = $derived(formatCamera(asset.exifInfo?.make, asset.exifInfo?.model));
	let imageExif = $derived(
		[
			asset.exifInfo?.fNumber ? `ƒ/${asset.exifInfo.fNumber.toFixed(1)}` : null,
			asset.exifInfo?.exposureTime ? `${asset.exifInfo.exposureTime}s` : null,
			asset.exifInfo?.focalLength ? `${trimFloat(asset.exifInfo.focalLength)}mm` : null,
			asset.exifInfo?.iso ? `ISO ${asset.exifInfo.iso}` : null
		].filter((item): item is string => item !== null)
	);
	let availablePeople = $derived(asset.people?.filter((x) => x.name));
	let availableTags = $derived(asset.tags?.filter((x) => x.name));
</script>

{#if showPhotoDate || showLocation || showImageDesc || showImageCamera || showImageExif || showPeopleDesc || showTagsDesc || showAlbumName}
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
				<span class="info-text" class:short-text={split}>{formattedDate}</span>
			</p>
		{/if}
		{#if showImageDesc && desc}
			<p id="imagedescription" class="info-item">
				<Icon path={mdiText} class="info-icon" />
				<span class="info-text" class:short-text={split}>{desc}</span>
			</p>
		{/if}
		{#if showImageCamera && imageCamera}
			<p id="imagecamera" class="info-item">
				<Icon path={mdiCamera} />
				<span class="info-text" class:short-text={split}>{imageCamera}</span>
			</p>
		{/if}
		{#if showImageExif && imageExif.length > 0}
			<p id="imageexif" class="info-item">
				<Icon path={mdiCameraIris} />
				<span class="info-text" class:short-text={split}>{imageExif.join(' | ')}</span>
			</p>
		{/if}
		{#if showAlbumName && albums && albums.length > 0}
			<p id="imagealbums" class="info-item">
				<Icon path={mdiImageAlbum} />
				<span class="info-text" class:short-text={split}
					>{albums.map((x) => x.albumName).join(', ')}</span
				>
			</p>
		{/if}
		{#if showPeopleDesc && availablePeople && availablePeople.length > 0}
			<p id="peopledescription" class="info-item">
				<Icon path={mdiAccount} />
				<span class="info-text" class:short-text={split}
					>{availablePeople.map((x) => x.name).join(', ')}</span
				>
			</p>
		{/if}
		{#if showTagsDesc && availableTags && availableTags.length > 0}
			<p id="tagsdescription" class="info-item">
				<Icon path={mdiTag} />
				<span class="info-text" class:short-text={split}
					>{availableTags.map((x) => x.name).join(', ')}</span
				>
			</p>
		{/if}
		{#if showLocation && location}
			<p id="imagelocation" class="info-item">
				<Icon path={mdiMapMarker} />
				<span class="info-text" class:short-text={split}>{location}</span>
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
	.info-text {
		max-width: 40vw;
		overflow: hidden;
		text-wrap: nowrap;
		text-overflow: ellipsis;
	}
	.short-text {
		max-width: 22vw;
	}
</style>
