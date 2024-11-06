<script lang="ts">
	import type { AssetResponseDto } from '$lib/immichFrameApi';
	import * as api from '$lib/immichFrameApi';
	import ErrorElement from './error-element.svelte';
	import Image from './image.svelte';
	import LoadingElement from './LoadingElement.svelte';
	import { fade } from 'svelte/transition';
	import { configStore } from '$lib/stores/config.store';
	import { Confetti } from 'svelte-confetti';

	export let sourceAssets: AssetResponseDto[];

	export let showLocation: boolean = true;
	export let showPhotoDate: boolean = true;
	export let showImageDesc: boolean = true;
	let split: boolean = true;

	let transitionDuration = ($configStore.transitionDuration ?? 1) * 1000;

	let error: boolean;
	let loaded: boolean;

	let images: [string, AssetResponseDto][];

	$: loadImages(sourceAssets);

	$: hasBday = hasBirthday(sourceAssets);

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

		console.log(hasBday);
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
			let req = await api.getImage(a.id);

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
					<div class="relative grid border-r-2 border-primary h-screen">
						<Image multi={true} image={images[0]} {showLocation} {showPhotoDate} {showImageDesc} />
					</div>
					<div class="relative grid border-l-2 border-primary h-screen">
						<Image multi={true} image={images[1]} {showLocation} {showPhotoDate} {showImageDesc} />
					</div>
				</div>
			{:else}
				<div class="relative grid h-screen w-screen">
					<Image image={images[0]} {showLocation} {showPhotoDate} {showImageDesc} />
				</div>
			{/if}
		</div>
	{/key}
{:else}
	<LoadingElement />
{/if}
