<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import { mdiChevronRight, mdiPlay, mdiPause } from '@mdi/js';
	import Icon from './icon.svelte';
	import { ProgressBarStatus } from './progress-bar.svelte';

	const dispatch = createEventDispatcher();

	export let status: ProgressBarStatus;

	function clickNext() {
		dispatch('next');
	}
	function clickBack() {
		// dispatch('back');
	}
	function clickPause() {
		dispatch('pause');
	}
	function clickSettings() {
		// dispatch('settings');
	}
</script>

<div class="absolute h-screen w-screen top-0 left-0 z-[100] grid grid-cols-3 gap-2 content-stretch">
	<div class="text-center group">
		<button
			class="flex justify-center items-center w-full h-full opacity-0 group-hover:opacity-100 text-primary"
			on:click={clickBack}
		>
			<!-- <Icon title="Back" path={mdiChevronLeft} size="20em" /> -->
		</button>
	</div>

	<div class="text-center grid grid-rows-3 gap-2 content-stretch">
		<button
			class="flex justify-center items-center w-full h-full opacity-0 hover:opacity-100 text-primary"
			on:click={clickSettings}
		>
			<!-- <Icon title="Text" path={mdiCog} size="20em" /> -->
		</button>
		<button
			on:click={clickPause}
			class="flex justify-center items-center w-full h-full opacity-0 hover:opacity-100 text-primary"
			><Icon
				title={status == ProgressBarStatus.Paused ? 'Play' : 'Pause'}
				path={status == ProgressBarStatus.Paused ? mdiPlay : mdiPause}
				size="20em"
			/></button
		>
		<div></div>
	</div>

	<div class="text-center group">
		<button
			class="flex justify-center items-center w-full h-full opacity-0 group-hover:opacity-100 text-primary"
			on:click={clickNext}
			><Icon title="Next" path={mdiChevronRight} size="20em" />
		</button>
	</div>
</div>
