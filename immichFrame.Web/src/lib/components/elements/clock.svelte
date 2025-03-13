<script lang="ts">
	import * as api from '$lib/index';
	import { onMount } from 'svelte';
	import { format } from 'date-fns';
	import * as locale from 'date-fns/locale';
	import { configStore } from '$lib/stores/config.store';
	import { clientIdentifierStore } from '$lib/stores/persist.store';

	api.init();

	let time = $state(new Date());
	let weather: api.IWeather = $state() as api.IWeather;

	const selectedLocale = $configStore.language;
	const localeToUse = locale[selectedLocale as keyof typeof locale] || locale.enUS;

	function splitFormat(formatStr: string) {
		const dateRegex = /[yYMdEL]/;
		const timeRegex = /[Hhmsa]/;

		let dateFormat = '';
		let timeFormat = '';
		let currentSection = '';
		let isDate = false;
		let isTime = false;

		for (let char of formatStr) {
			if (dateRegex.test(char)) {
				if (!isDate && currentSection) {
					timeFormat += currentSection;
					currentSection = '';
				}
				isDate = true;
				isTime = false;
			} else if (timeRegex.test(char)) {
				if (!isTime && currentSection) {
					dateFormat += currentSection;
					currentSection = '';
				}
				isTime = true;
				isDate = false;
			}
			currentSection += char;
		}

		if (isDate) dateFormat += currentSection;
		else timeFormat += currentSection;

		return {
			dateFormat: dateFormat.trim() || null,
			timeFormat: timeFormat.trim() || null
		};
	}

	let { dateFormat, timeFormat } = splitFormat($configStore.clockFormat ?? 'HH:mm:ss');

	let formattedDate = $derived(dateFormat ? format(time, dateFormat, { locale: localeToUse }) : '');
	let formattedTime = $derived(timeFormat ? format(time, timeFormat, { locale: localeToUse }) : '');

	onMount(() => {
		const interval = setInterval(() => {
			time = new Date();
		}, 1000);

		GetWeather();
		const weatherInterval = setInterval(() => GetWeather(), 10 * 60 * 1000);

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
	id="clock"
	class="absolute bottom-0 left-0 z-10 text-center text-primary
	{$configStore.style == 'solid' ? 'bg-secondary rounded-tr-2xl' : ''}
	{$configStore.style == 'transition' ? 'bg-gradient-to-r from-secondary from-0% pr-10' : ''}
	{$configStore.style == 'blur' ? 'backdrop-blur-lg rounded-tr-2xl' : ''}	
	drop-shadow-2xl p-3"
>
	{#if formattedDate}
		<p
			id="clockdate"
			class="mt-2 text-sm sm:text-sm md:text-md lg:text-xl font-thin text-shadow-sm"
		>
			{formattedDate}
		</p>
	{/if}
	<p
		id="clocktime"
		class="mt-2 text-4xl sm:text-4xl md:text-6xl lg:text-8xl font-bold text-shadow-lg"
	>
		{formattedTime}
	</p>
	{#if weather}
		<div id="clockweather">
			<div id="clockweatherinfo" class="text-xl sm:text-xl md:text-2xl lg:text-3xl font-semibold">
				{weather.location},
				{weather.temperature?.toFixed(1)}
				{weather.unit}
			</div>
			{#if $configStore.showWeatherDescription}
				<p id="clockweatherdesc" class="text-sm sm:text-sm md:text-md lg:text-xl text-shadow-sm">
					{weather.description}
				</p>
			{/if}
		</div>
	{/if}
</div>
