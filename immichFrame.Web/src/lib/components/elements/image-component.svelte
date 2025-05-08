<script lang="ts">
	import { run } from 'svelte/legacy';

	import type { AssetResponseDto } from '$lib/immichFrameApi';
	import * as api from '$lib/index';
	import ErrorElement from './error-element.svelte';
	import Image from './image.svelte';
	import LoadingElement from './LoadingElement.svelte';
	import { fade } from 'svelte/transition';
	import { configStore } from '$lib/stores/config.store';
	import { Confetti } from 'svelte-confetti';
	import { clientIdentifierStore } from '$lib/stores/persist.store';
	import { slideshowStore } from '$lib/stores/slideshow.store';

	api.init();

	interface Props {
		images: [string, AssetResponseDto][];
		interval?: number;
		error?: boolean;
		loaded?: boolean;
		split?: boolean;
		hasBday?: boolean;
		showLocation?: boolean;
		showPhotoDate?: boolean;
		showImageDesc?: boolean;
		showPeopleDesc?: boolean;
		imageFill?: boolean;
		imageZoom?: boolean;
	}

	let {
		images,
		interval = 20,
		error = false,
		loaded = false,
		split = false,
		hasBday = false,
		showLocation = true,
		showPhotoDate = true,
		showImageDesc = true,
		showPeopleDesc = true,
		imageFill = false,
		imageZoom = false
	}: Props = $props();
	let instantTransition = slideshowStore.instantTransition;
	let transitionDuration = $derived(
		$instantTransition ? 0 : ($configStore.transitionDuration ?? 1) * 1000
	);
</script>

{#if hasBday}
	<div
		class="	z-[1000] top-[-50px] fixed l-0 h-dvh w-screen flex justify-center overflow-hidden pointer-events-none"
	>
		<Confetti
			x={[-5, 5]}
			y={[0, 0.1]}
			delay={[500, 2000]}
			infinite
			duration={5000}
			amount={200}
			fallDistance="100vh"
		/>
	</div>
{/if}

{#if error}
	<ErrorElement />
{:else if loaded}
	{#key images}
		<div class="grid absolute h-dvh w-screen" transition:fade={{ duration: transitionDuration }}>
			{#if split}
				<div class="grid grid-cols-2">
					<div id="image_portrait_1" class="relative grid border-r-2 border-primary h-dvh">
						<Image
							multi={true}
							image={images[0]}
							{interval}
							{showLocation}
							{showPhotoDate}
							{showImageDesc}
							{showPeopleDesc}
							{imageFill}
							{imageZoom}
						/>
					</div>
					<div id="image_portrait_2" class="relative grid border-l-2 border-primary h-dvh">
						<Image
							multi={true}
							image={images[1]}
							{interval}
							{showLocation}
							{showPhotoDate}
							{showImageDesc}
							{showPeopleDesc}
							{imageFill}
							{imageZoom}
						/>
					</div>
				</div>
			{:else}
				<div id="image_default" class="relative grid h-dvh w-screen">
					<Image
						image={images[0]}
						{interval}
						{showLocation}
						{showPhotoDate}
						{showImageDesc}
						{showPeopleDesc}
						{imageFill}
						{imageZoom}
					/>
				</div>
			{/if}
		</div>
	{/key}
{:else}
	<div class="grid absolute h-dvh w-screen">
		<LoadingElement />
	</div>
{/if}
