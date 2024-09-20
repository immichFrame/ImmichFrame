<script lang="ts">
	import ProgressBar, { ProgressBarStatus } from '$lib/components/elements/progress-bar.svelte';
	import { slideshowStore } from '$lib/stores/slideshow.store';
	import { createEventDispatcher, onDestroy, onMount } from 'svelte';

	const dispatch = createEventDispatcher<{
		done: void;
	}>();

	const { restartProgress, stopProgress } = slideshowStore;

	let progressBarStatus: ProgressBarStatus;
	let progressBar: ProgressBar;

	let unsubscribeRestart: () => void;
	let unsubscribeStop: () => void;

	onMount(() => {
		unsubscribeRestart = restartProgress.subscribe((value) => {
			if (value) {
				progressBar.restart(value);
			}
		});

		unsubscribeStop = stopProgress.subscribe((value) => {
			if (value) {
				progressBar.restart(false);
			}
		});
	});

	onDestroy(() => {
		if (unsubscribeRestart) {
			unsubscribeRestart();
		}

		if (unsubscribeStop) {
			unsubscribeStop();
		}
	});

	const handleDone = () => {
		dispatch('done');
		progressBar.restart(true);
	};
</script>

<ProgressBar
	autoplay
	hidden={false}
	duration={5}
	bind:this={progressBar}
	bind:status={progressBarStatus}
	on:done={handleDone}
/>
