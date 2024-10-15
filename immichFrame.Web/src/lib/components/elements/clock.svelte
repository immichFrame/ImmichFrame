<script lang="ts">
	import * as api from '$lib/immichFrameApi';
	import { onMount } from 'svelte';
	import { format } from 'date-fns';
	import { configStore } from '$lib/stores/config.store';

	let time = new Date();
	let weather: api.IWeather;

	$: formattedDate = format(time, $configStore.photoDateFormat ?? 'dd.MM.yyyy');
	$: timePortion = format(time, $configStore.clockFormat ?? 'HH:mm:ss');

	onMount(() => {
		const interval = setInterval(() => {
			time = new Date();
		}, 1000);

		GetWeather();
		const weatherInterval = setInterval(() => GetWeather, 1 * 60 * 10000);

		return () => {
			clearInterval(interval);
			clearInterval(weatherInterval);
		};
	});

	async function GetWeather() {
		let weatherRequest = await api.getWeather();
		if (weatherRequest.status == 200) {
			weather = weatherRequest.data;
		}
	}
</script>

<div class="absolute bottom-0 left-0 z-10 text-center text-primary p-5 drop-shadow-2xl">
	<p class="mt-2 text-xl font-thin">{formattedDate}</p>
	<p class="mt-2 text-8xl font-bold">{timePortion}</p>
	{#if weather}
		<div>
			<div class="text-3xl font-semibold">
				{weather.location},
				{weather.temperatureUnit}
			</div>
			{#if $configStore.showWeatherDescription}
				<p>{weather.description}</p>
			{/if}
		</div>
	{/if}
</div>
