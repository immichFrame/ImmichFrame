<script lang="ts">
	import * as api from '$lib/index';
	import { onMount } from 'svelte';
	import { format } from 'date-fns';
	import * as locale from 'date-fns/locale';
	import { configStore } from '$lib/stores/config.store';
	import { clientIdentifierStore } from '$lib/stores/persist.store';
	import * as iconsImport from '$lib/assets/icons';
	const icons: Record<string, string> = iconsImport as Record<string, string>;

    api.init();

	let weather = $state<api.IWeather | null>(null);
    let now = $state(new Date());

	onMount(() => {
		const interval = setInterval(() => {
			now = new Date();
		}, 1000);

		getWeather();
		const weatherInterval = setInterval(() => getWeather(), 10 * 60 * 1000);

		return () => {
			clearInterval(interval);
			clearInterval(weatherInterval);
		};
	});
    
    async function getWeather() {
		try {
			const weatherRequest = await api.getWeather({ clientIdentifier: $clientIdentifierStore });
			if (weatherRequest.status === 200) {
				weather = weatherRequest.data;
			} else {
				console.warn('Unexpected weather status:', weatherRequest.status);
			}
		} catch (err) {
			console.error('Error fetching weather:', err);
		}
	}

</script>

<div
	id="weather"
	class="fixed top-0 left-0 z-10 text-primary
	{$configStore.style == 'solid' ? 'bg-secondary rounded-tr-2xl' : ''}
	{$configStore.style == 'transition' ? 'bg-gradient-to-r from-secondary from-0% pr-10' : ''}
	{$configStore.style == 'blur' ? 'backdrop-blur-lg rounded-tr-2xl' : ''}	
	drop-shadow-2xl p-3"
>
	{#if weather}
        <div
            id="weatherinfo"
            class="text-xl sm:text-xl md:text-2xl lg:text-3xl font-semibold text-shadow-sm weather-info current-conditions-wrapper"
        >
			{#if weather.iconId}
				<img src="{icons[weather.iconId.replaceAll('-','')]}" class="current icon" alt="{weather.iconId.replaceAll('-','')}">
			{/if}
         
            <div class="current temperature">{weather.temperature?.toFixed(1)}{weather.unit}</div>
        </div>
        <div id = "weatherHighLow" style="margin-bottom: 10px;" class="text-sm sm:text-sm md:text-md lg:text-xl text-shadow-sm">
				<span style="color:#F8DD70;">H {weather.tempHigh?.toFixed(0)}°</span> / <span style="color:#6FC4F5;margin-right:15px">L {weather.tempLow?.toFixed(0)}°</span> 
				<img style = "display: inline-block;"src="{icons['irain']}" alt="An umbrella"/> {((weather?.precip ?? 0)*100).toFixed(0)}% 
        </div>

        {#if $configStore.showWeatherDescription}
            <p id="weatherdesc" class="text-sm sm:text-sm md:text-md lg:text-xl text-shadow-sm"
			style="max-width:350px; margin: 15px 0;">
                {weather.description}
            </p>
        {/if}

		<div class="forecast-container">
			<div class="header-row">
				<span class="date-time-header">&nbsp;</span>
				<span class="weather-icon-header">&nbsp;</span>
				<span class="temperature-header">&nbsp;</span>

				<span class="precipitation-header">
					<img class="inline-icon rain" src="{icons['irain']}" alt="Rain"/>
				</span>
			</div>

			{#each weather.hourlyForecast ?? [] as forecast, index}
			<div class="forecast-item hourly">
				<span class="time">{forecast.time != null ? new Date(Number(forecast.time) * 1000).toLocaleTimeString(undefined, { hour: 'numeric', hour12: true }) : ''}</span>
				<span class="forecast-icon-container"><img src="{icons[(forecast.icon ?? '').replaceAll('-','')]}" class="forecast-icon" alt="{(forecast.icon ?? '').replaceAll('-','')}"></span>
				<span class="temperature small">{forecast.temperature !== undefined ? Math.round(forecast.temperature) : '--'}°</span>

				 <span class="precipitation-container">
					 <span class="pop">{(Number(forecast?.precipProbability ?? 0) * 100).toFixed(0)}%</span>
				 </span>
			</div>
			{/each}

			{#each weather.dailyForecast ?? [] as forecast, index}
			<div class="forecast-item hourly">
				<span class="day-name">{forecast.time != null ? new Date(Number(forecast.time) * 1000).toLocaleDateString(undefined, { weekday: 'short' }) : ''}</span>
				<span class="forecast-icon-container"><img class="forecast-icon" src="{icons[(forecast.icon ?? '').replaceAll('-','')]}" alt="{(forecast.icon ?? '').replaceAll('-','')}"/></span>
				<span class="temperature-container small">
					<span class="high-temperature" style="color:#F8DD70;">H {forecast.temperatureHigh !== undefined ? Math.round(forecast.temperatureHigh) : '--'}°</span>
					<span class="temperature-separator dimmed">/</span>
					<span class="low-temperature" style="color:#6FC4F5;margin-right:15px">L {forecast.temperatureLow !== undefined ? Math.round(forecast.temperatureLow) : '--'}</span>
				</span>
				<span class="precipitation-container">
					<span class="pop">{(Number(forecast?.precipProbability ?? 0) * 100).toFixed(0)}%</span>
				</span>
			</div>
			{/each}
		</div>
	{/if}
</div>