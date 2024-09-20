<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import { mdiChevronLeft, mdiChevronRight, mdiCog, mdiPause } from '@mdi/js';
	import Icon from './icon.svelte';

	const dispatch = createEventDispatcher();

	function clickNext() {
		dispatch('next');
	}
	function clickBack() {
		dispatch('back');
	}
	function clickPause() {
		dispatch('pause');
	}
	function clickSettings() {
		dispatch('settings');
	}
</script>

<div id="image-overlay" class="grid grid-cols-3 gap-2 content-stretch">
	<div class="overlay-item group">
		<button on:click={clickBack} class="opacity-0 group-hover:opacity-100 frame-color"
			><Icon title="Text" path={mdiChevronLeft} size="20em" /></button
		>
	</div>

	<div class="overlay-item grid grid-rows-3 gap-2 content-stretch">
		<button on:click={clickSettings} class="opacity-0 hover:opacity-100 frame-color"
			><Icon title="Text" path={mdiCog} size="20em" /></button
		>
		<button on:click={clickPause} class="opacity-0 hover:opacity-100 frame-color"
			><Icon title="Text" path={mdiPause} size="20em" /></button
		>
		<div></div>
	</div>

	<div class="overlay-item group">
		<button on:click={clickNext} class="opacity-0 group-hover:opacity-100 frame-color"
			><Icon title="Text" path={mdiChevronRight} size="20em" />
		</button>
	</div>
</div>

<style>
	#image-overlay {
		position: absolute;
		height: 100%;
		width: 100%;
		top: 0;
		left: 0;
		z-index: 100;
	}
	.overlay-item {
		text-align: center;
	}
	button {
		display: flex;
		justify-content: center; /* Centers the SVG horizontally */
		align-items: center; /* Optionally, centers the SVG vertically as well */
		/* background-color: rgba(75, 75, 75, 0.4); */
		height: 100%;
		width: 100%;
	}
	.frame-color {
		color: wheat;
	}
</style>
