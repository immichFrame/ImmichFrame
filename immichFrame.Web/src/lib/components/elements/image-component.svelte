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

	api.init();

	interface Props {
		sourceAssets: AssetResponseDto[];
		showLocation?: boolean;
		showPhotoDate?: boolean;
		showImageDesc?: boolean;
		showPeopleDesc?: boolean;
	}

	let {
		sourceAssets,
		showLocation = true,
		showPhotoDate = true,
		showImageDesc = true,
		showPeopleDesc = true
	}: Props = $props();
	let split: boolean = $state(true);

	let transitionDuration = ($configStore.transitionDuration ?? 1) * 1000;

	let error: boolean = $state(false);
	let loaded: boolean = $state(false);

	let images: [string, AssetResponseDto][] = $state() as [string, AssetResponseDto][];

	function hasBirthday(assets: AssetResponseDto[]) {
		let today = new Date();
		let hasBday: boolean = false;

		for (let asset of assets) {
			for (let person of asset.people ?? new Array()) {
				let birthdate = new Date(person.birthDate ?? '');
				if (birthdate.getDate() === today.getDate() && birthdate.getMonth() === today.getMonth()) {
					hasBday = true;
					break;
				}
			}
			if (hasBday) break;
		}

		return hasBday;
	}

	function getImageUrl(image: Blob) {
		return URL.createObjectURL(image);
	}

	async function loadImages(assets: AssetResponseDto[]) {
		let newImages = [];

		for (let asset of assets) {
			let img = await loadImage(asset);
			newImages.push(img);
		}

		if (assets.length == 2) {
			split = true;
		} else {
			split = false;
		}
		images = newImages;
		loaded = true;
	}

	async function loadImage(a: AssetResponseDto) {
		try {
			let req = await api.getImage(a.id, { clientIdentifier: $clientIdentifierStore });

			if (req.status != 200) {
				error = true;
				return ['', a] as [string, AssetResponseDto];
			}

			error = false;
			return [getImageUrl(req.data), a] as [string, AssetResponseDto];
		} catch {
			error = true;
		}
		return ['', a] as [string, AssetResponseDto];
	}
	run(() => {
		loadImages(sourceAssets);
	});
	let hasBday = $derived(hasBirthday(sourceAssets));
</script>

{#if hasBday}
	<div
		class="	z-[1000] top-[-50px] fixed l-0 h-screen w-screen flex justify-center overflow-hidden pointer-events-none"
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
		<div class="grid absolute h-screen w-screen" transition:fade={{ duration: transitionDuration }}>
			{#if split}
				<div class="grid grid-cols-2">
					<div id="image_portrait_1" class="relative grid border-r-2 border-primary h-screen">
						<Image
							multi={true}
							image={images[0]}
							{showLocation}
							{showPhotoDate}
							{showImageDesc}
							{showPeopleDesc}
						/>
					</div>
					<div id="image_portrait_2" class="relative grid border-l-2 border-primary h-screen">
						<Image
							multi={true}
							image={images[1]}
							{showLocation}
							{showPhotoDate}
							{showImageDesc}
							{showPeopleDesc}
						/>
					</div>
				</div>
			{:else}
				<div id="image_default" class="relative grid h-screen w-screen">
					<Image
						image={images[0]}
						{showLocation}
						{showPhotoDate}
						{showImageDesc}
						{showPeopleDesc}
					/>
				</div>
			{/if}
		</div>
	{/key}
{:else}
	<LoadingElement />
{/if}
