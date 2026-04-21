<script lang="ts">
	import { handlePromiseError } from '$lib/utils';
	import { onMount, untrack } from 'svelte';
	import { tweened } from 'svelte/motion';
	import { ProgressBarLocation, ProgressBarStatus } from './progress-bar.types';

	interface Props {
		autoplay?: boolean;
		status?: ProgressBarStatus;
		location?: ProgressBarLocation;
		hidden?: boolean;
		duration?: number;
		onDone: () => void;
		onPlaying?: () => void;
		onPaused?: () => void;
	}

	let {
		autoplay = false,
		status = $bindable(ProgressBarStatus.Paused),
		location = ProgressBarLocation.Bottom,
		hidden = false,
		duration = 5,
		onDone,
		onPlaying = () => {},
		onPaused = () => {}
	}: Props = $props();

	const onChange = async (progressDuration: number) => {
		progress = setDuration(progressDuration);
		await play();
	};

	let progress = setDuration(duration);

	$effect(() => {
		handlePromiseError(onChange(duration));
	});

	$effect(() => {
		if ($progress === 1) {
			untrack(() => onDone());
		}
	});

	onMount(async () => {
		if (autoplay) {
			await play();
		}
	});

	export const play = async () => {
		status = ProgressBarStatus.Playing;
		onPlaying();
		await progress.set(1);
	};

	export const pause = async () => {
		status = ProgressBarStatus.Paused;
		onPaused();
		await progress.set($progress);
	};

	export const restart = async (autoplay: boolean) => {
		await progress.set(0);

		if (autoplay) {
			await play();
		}
	};

	export const reset = async () => {
		status = ProgressBarStatus.Paused;
		await progress.set(0);
	};

	function setDuration(newDuration: number) {
		return tweened<number>(0, {
			duration: (from: number, to: number) => (to ? newDuration * 1000 * (to - from) : 0)
		});
	}
</script>

{#if !hidden}
	<span
		id="progressbar"
		class="fixed left-0 h-[3px] bg-primary z-[1000]
		{location == ProgressBarLocation.Top ? 'top-0' : 'bottom-0'}"
		style:width={`${$progress * 100}%`}
	></span>
{/if}
