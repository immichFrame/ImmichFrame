<script lang="ts">
	import {
		type AlbumResponseDto,
		type AssetResponseDto,
		type ExifResponseDto
	} from '$lib/immichFrameApi';
	import { mdiAccount, mdiCamera, mdiFile, mdiImageAlbum, mdiStar, mdiText } from '@mdi/js';
	import OverlayItem from './overlay-item.svelte';

	interface Props {
		asset: AssetResponseDto;
		albums: AlbumResponseDto[];
	}

	let { asset, albums }: Props = $props();
	let availablePeople = $derived(asset.people?.filter((x) => x.name));

	let exif: ExifResponseDto = $derived(asset.exifInfo ?? ({} as ExifResponseDto));
</script>

<div class="p-0 absolute w-full h-full">
	<div
		class="bg-black bg-opacity-70 z-[50] p-32 w-full h-full relative items-center justify-center flex"
	>
		<div class="w-fit flex flex-col gap-3">
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
			<OverlayItem
				icon={mdiCamera}
				header="{exif.make} {exif.model}"
				items={[
					`ISO: ${exif.iso}`,
					`Aperture: f/${exif.fNumber}`,
					`Shutter: 1/${exif.iso}`,
					`Focal Length: ${exif.focalLength} mm`
				]}
			/>
			{#if exif.description}
				<OverlayItem icon={mdiText} header={exif.description} />
			{/if}
			{#if exif.rating}
				<OverlayItem icon={mdiStar} header="Rating" items={[exif.rating.toString()]} />
			{/if}
			{#if availablePeople && availablePeople.length > 0}
				<OverlayItem icon={mdiAccount} header="People" items={availablePeople.map((x) => x.name)} />
			{/if}
			{#if albums && albums.length > 0}
				<OverlayItem icon={mdiImageAlbum} header="Album" items={albums.map((x) => x.albumName)} />
			{/if}
		</div>
	</div>
</div>
