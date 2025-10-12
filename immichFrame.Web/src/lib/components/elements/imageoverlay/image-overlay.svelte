<script lang="ts">
	import {
		type AlbumResponseDto,
		type AssetResponseDto,
		type ExifResponseDto
	} from '$lib/immichFrameApi';
	import {
		mdiAccount,
		mdiCamera,
		mdiClose,
		mdiFile,
		mdiImageAlbum,
		mdiStar,
		mdiText
	} from '@mdi/js';
	import OverlayItem from './overlay-item.svelte';
	import OverlayQr from './overlay-qr.svelte';
	import Icon from '../icon.svelte';
	import { getContext } from 'svelte';

	interface Props {
		asset: AssetResponseDto;
		albums: AlbumResponseDto[];
	}
	let { asset, albums }: Props = $props();

	let availablePeople = $derived(asset.people?.filter((x) => x.name));
	let exif: ExifResponseDto = $derived(asset.exifInfo ?? ({} as ExifResponseDto));

	let close = getContext<() => void>('close');
</script>

<div class="p-0 absolute w-full h-full z-[200]">
	<div
		class="info-overlay-background bg-black bg-opacity-70 w-full h-full relative items-center justify-center flex pt-32 pb-8 max-h-full overflow-auto"
	>
		<button class="absolute top-0 right-0 m-4 text-primary" onclick={close}>
			<Icon
				path={mdiClose}
				size={30}
				class="info-overlay-close hover:scale-110 transition-transform duration-200 text-primary"
			/>
		</button>
		<div class="flex h-full flex-col gap-5">
			<div class="w-fit flex grow flex-col gap-3">
				{#if asset.originalFileName}
					<OverlayItem
						icon={mdiFile}
						header={asset.originalFileName}
						items={[
							`Size: ${((exif.fileSizeInByte ?? 0) / (1024 * 1024)).toFixed(1)} MB`,
							`Dimensions: ${exif.exifImageWidth} x ${exif.exifImageHeight}`
						]}
					/>
				{/if}
				{#if exif.make || exif.model || exif.fNumber || exif.exposureTime || exif.focalLength || exif.iso}
					<OverlayItem
						icon={mdiCamera}
						header={`${exif.make ?? ''} ${exif.model ?? ''}`.trim()}
						items={[
							exif.fNumber ? `ƒ/${exif.fNumber}` : null,
							exif.exposureTime ? `${exif.exposureTime} s` : null,
							exif.focalLength ? `${exif.focalLength} mm` : null,
							exif.iso ? `ISO ${exif.iso}` : null
						].filter((item): item is string => item !== null)}
					/>
				{/if}
				{#if exif.description}
					<OverlayItem icon={mdiText} header={exif.description} />
				{/if}
				{#if exif.rating}
					<OverlayItem icon={mdiStar} header="Rating" items={[exif.rating.toString()]} />
				{/if}
				{#if availablePeople && availablePeople.length > 0}
					<OverlayItem
						icon={mdiAccount}
						header="People"
						items={availablePeople.map((x) => x.name)}
					/>
				{/if}
				{#if albums && albums.length > 0}
					<OverlayItem icon={mdiImageAlbum} header="Album" items={albums.map((x) => x.albumName)} />
				{/if}
			</div>

			<OverlayQr id={asset.id} />
		</div>
	</div>
</div>
