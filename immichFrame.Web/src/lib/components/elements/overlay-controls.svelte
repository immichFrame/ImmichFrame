<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import { mdiChevronRight, mdiPlay, mdiPause } from '@mdi/js';
	import Icon from './icon.svelte';
	import { ProgressBarStatus } from './progress-bar.svelte';

	const dispatch = createEventDispatcher();

	export let status: ProgressBarStatus;
	export let overlayVisible: boolean;

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

	function shortcuts(node: any, shortcutList: any[]) {
		function handleKeyDown(event: { key: any; preventDefault: () => void }) {
			const shortcut = shortcutList.find((s) => s.key === event.key);
			if (shortcut && shortcut.action) {
				event.preventDefault();
				shortcut.action();
			}
		}

		window.addEventListener('keydown', handleKeyDown);

		return {
			destroy() {
				window.removeEventListener('keydown', handleKeyDown);
			}
		};
	}

	// Define your shortcut list
	const shortcutList = [
		{
			key: 'ArrowRight',
			action: clickNext
		},
		{
			key: 'ArrowLeft',
			action: clickBack
		},
		{
			key: ' ',
			action: clickPause
		}
	];
</script>

<svelte:window use:shortcuts={shortcutList} />

{#if overlayVisible}
	<div class="absolute h-full w-full top-0 left-0 z-[100] grid grid-cols-3 gap-2">
		<div class="group text-center content-center">
			<button class="opacity-0 group-hover:opacity-100 text-primary" on:click={clickBack}> </button>
		</div>

		<div class="grid grid-rows-3">
			<div class="group text-center content-center">
				<button class="opacity-0 hover:opacity-100 text-primary" on:click={clickSettings}> </button>
			</div>

			<div class="group text-center content-center">
				<button on:click={clickPause} class="opacity-0 group-hover:opacity-100 text-primary">
					<Icon
						class="max-h-[min(10rem,33vh)] max-w-[min(10rem,33vh)] h-[33vh] w-[33vw]"
						title={status == ProgressBarStatus.Paused ? 'Play' : 'Pause'}
						path={status == ProgressBarStatus.Paused ? mdiPlay : mdiPause}
						size=""
					/>
				</button>
			</div>

			<div class="group text-center content-center">
				<button class="opacity-0 hover:opacity-100 text-primary"> </button>
			</div>
		</div>

		<div class="group text-center content-center">
			<button class="opacity-0 group-hover:opacity-100 text-primary" on:click={clickNext}
				><Icon
					title="Next"
					class="max-h-[min(10rem,33vh)] max-w-[min(10rem,33vh)] h-[33vh] w-[33vw]top"
					path={mdiChevronRight}
					size=""
				/>
			</button>
		</div>
	</div>
{/if}
