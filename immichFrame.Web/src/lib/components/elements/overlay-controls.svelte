<script lang="ts">
	import {
		mdiChevronRight,
		mdiPlay,
		mdiPause,
		mdiChevronLeft,
		mdiInformationOutline
	} from '@mdi/js';
	import Icon from './icon.svelte';
	import { ProgressBarStatus } from './progress-bar.svelte';

	interface Props {
		status: ProgressBarStatus;
		overlayVisible: boolean;
		infoVisible: boolean;
		next: () => void;
		back: () => void;
		pause: () => void;
		showInfo: () => void;
	}

	let {
		status = $bindable(),
		overlayVisible,
		infoVisible = $bindable(),
		next,
		back,
		pause,
		showInfo
	}: Props = $props();

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
			action: next
		},
		{
			key: 'ArrowLeft',
			action: back
		},
		{
			key: ' ',
			action: pause
		},
		{
			key: 'i',
			action: showInfo
		}
	];
</script>

<svelte:window use:shortcuts={shortcutList} />

{#if overlayVisible}
<div
	class={`fixed inset-0 z-[100] grid grid-cols-3 grid-rows-3 ${infoVisible ? 'hidden' : ''}`}
>
	<div id="overlayInfo" class="group flex justify-center items-center col-start-2 row-start-1">
		<button class="opacity-0 hover:opacity-100 text-primary" onclick={showInfo}>
			<Icon
				title="Info"
				class="h-32 w-32 max-h-[min(10rem,33vh)] max-w-[min(10rem,33vh)]"
				path={mdiInformationOutline}
			/>
		</button>
	</div>

	<div id="overlayback" class="group flex justify-center items-center col-start-1 row-start-2">
		<button class="opacity-0 group-hover:opacity-100 text-primary" onclick={back}>
			<Icon
				title="Back"
				class="h-32 w-32 max-h-[min(10rem,33vh)] max-w-[min(10rem,33vh)]"
				path={mdiChevronLeft}
			/>
		</button>
	</div>

	<div id="overlaypause" class="group flex justify-center items-center col-start-2 row-start-2">
		<button class="opacity-0 group-hover:opacity-100 text-primary" onclick={pause}>
			<Icon
				title={status == ProgressBarStatus.Paused ? 'Play' : 'Pause'}
				class="h-32 w-32 max-h-[min(10rem,33vh)] max-w-[min(10rem,33vh)]"
				path={status == ProgressBarStatus.Paused ? mdiPlay : mdiPause}
			/>
		</button>
	</div>

	<div id="overlaynext" class="group flex justify-center items-center col-start-3 row-start-2">
		<button class="opacity-0 group-hover:opacity-100 text-primary" onclick={next}>
			<Icon
				title="Next"
				class="h-32 w-32 max-h-[min(10rem,33vh)] max-w-[min(10rem,33vh)]"
				path={mdiChevronRight}
			/>
		</button>
	</div>
</div>

{/if}
