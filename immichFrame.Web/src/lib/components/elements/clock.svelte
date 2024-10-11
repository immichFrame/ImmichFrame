<script lang="ts">
	import { onMount } from 'svelte';
	import { format } from 'date-fns';
	import { configStore } from '$lib/stores/config.store';

	let time = new Date();

	$: formattedDate = format(time, $configStore.photoDateFormat ?? 'dd.MM.yyyy');
	$: timePortion = format(time, $configStore.clockFormat ?? 'HH:mm:ss');

	onMount(() => {
		const interval = setInterval(() => {
			time = new Date();
		}, 1000);

		return () => {
			clearInterval(interval);
		};
	});
</script>

<div class="absolute bottom-0 left-0 z-10 text-primary p-5">
	<p class="mt-2 text-center text-xl font-thin">{formattedDate}</p>
	<p class="mt-2 text-8xl font-bold">{timePortion}</p>
</div>
