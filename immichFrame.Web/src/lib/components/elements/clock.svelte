<script lang="ts">
	import * as api from '$lib/immichFrameApi';
	import { onMount } from 'svelte';
	import { format } from 'date-fns';
	import * as locale from 'date-fns/locale';
	import { configStore } from '$lib/stores/config.store';
	import { clientIdentifierStore } from '$lib/stores/identifier.store';

	let time = $state(new Date());
	let weather: api.IWeather = $state() as api.IWeather;

	const selectedLocale = $configStore.language;

	const localeToUse =
		(selectedLocale && locale[selectedLocale as keyof typeof locale]) || locale.enUS;

	let formattedDate = $derived(
		format(time, $configStore.photoDateFormat ?? 'dd.MM.yyyy', {
			locale: localeToUse
		})
	);
	let timePortion = $derived(format(time, $configStore.clockFormat ?? 'HH:mm:ss'));
	let clockPosition = $derived($configStore.clockPosition ?? 'bottom-0 left-0');

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
		let weatherRequest = await api.getWeather({ clientIdentifier: $clientIdentifierStore });
		if (weatherRequest.status == 200) {
			weather = weatherRequest.data;
		}
	}
</script>

<div
	class="absolute {clockPosition} z-10 text-center text-primary
	{$configStore.style == 'solid' ? 'bg-secondary rounded-tr-2xl' : ''}
	{$configStore.style == 'transition' ? 'bg-gradient-to-r from-secondary from-0% pr-10' : ''}
	{$configStore.style == 'blur' ? 'backdrop-blur-lg rounded-tr-2xl' : ''}	
	drop-shadow-2xl p-3"
>
	<p class="mt-2 text-sm sm:text-sm md:text-md lg:text-xl font-thin text-shadow-sm">
		{formattedDate}
	</p>
	<p class="mt-2 text-4xl sm:text-4xl md:text-6xl lg:text-8xl font-bold text-shadow-lg">
		{timePortion}
	</p>
	{#if weather}
		<div>
			<div class="text-xl sm:text-xl md:text-2xl lg:text-3xl font-semibold text-shadow-sm">
				{weather.location},
				{weather.temperature?.toFixed(1)}
				{weather.unit}
			</div>
			{#if $configStore.showWeatherDescription}
				<p class="text-sm sm:text-sm md:text-md lg:text-xl text-shadow-sm">{weather.description}</p>
			{/if}
		</div>
	{/if}
</div>
