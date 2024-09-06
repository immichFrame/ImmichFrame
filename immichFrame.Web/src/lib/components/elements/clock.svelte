<script lang="ts">
	import { fallbackLocale } from '$lib/constants';
	import { onMount } from 'svelte';
	let time = new Date();

	$: formattedDate = time.toLocaleString(fallbackLocale.code, {
		year: 'numeric',
		month: '2-digit',
		day: '2-digit'
	});
	$: timePortion = time.toLocaleString(fallbackLocale.code, {
		hour: '2-digit',
		minute: '2-digit'
	});
	$: selectedDate = `${formattedDate} ${timePortion}`;

	onMount(() => {
		const interval = setInterval(() => {
			time = new Date();
		}, 10000);

		return () => {
			clearInterval(interval);
		};
	});
</script>

<div id="clock-overlay" class="text-white">
	<p class="mt-2 text-lg">{selectedDate}</p>
</div>

<style>
	#clock-overlay {
		color: wheat;
		position: absolute;
		background-color: rgba(0, 0, 0, 0.4);
		padding: 2%;
		bottom: 0;
		left: 0;
		z-index: 1;
	}
</style>
