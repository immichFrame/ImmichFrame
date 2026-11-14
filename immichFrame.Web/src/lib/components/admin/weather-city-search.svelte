<script lang="ts">
	import { untrack } from 'svelte';
	import { Button, Icon, Text } from '@immich/ui';
	import { Combobox } from 'bits-ui';
	import { mdiCheck, mdiClose } from '@mdi/js';
	import type { GeneralSettings, WeatherCity } from '$lib/immichFrameApi';
	import { searchWeatherCities } from '$lib/services/admin';

	let { settings }: { settings: GeneralSettings } = $props();
	const id = $props.id();
	const emptyMessage = 'No cities found. Try another name or add a country code.';
	const label = (city: WeatherCity) => `${city.name}, ${city.country} (${city.id})`;
	const key = (city: WeatherCity) => `${city.id}:${city.latitude}:${city.longitude}`;
	let query = $state('');
	let cities: WeatherCity[] = $state([]);
	let loading = $state(false);
	let searched = $state(false);
	let error = $state('');
	let editing = $state(false);
	let open = $state(false);
	let value = $state('');
	let selected = $state(false);
	let input: HTMLInputElement | null = $state(null);
	const hasApiKey = $derived(Boolean(settings.weatherApiKey?.trim()));
	const items = $derived(cities.map((city) => ({ value: key(city), label: label(city) })));

	$effect(() => {
		const location = settings.weatherLocation;
		const cityId = settings.weatherCityId;
		untrack(() => {
			query = location ? label(location) : cityId ? `City ID ${cityId}` : '';
			value = location ? key(location) : '';
			editing = false;
		});
	});

	$effect(() => {
		const name = query.trim();
		const apiKey = settings.weatherApiKey ?? '';
		error = '';
		cities = [];
		searched = false;
		if (!editing || name.length < 2 || !apiKey.trim()) return;
		const controller = new AbortController();
		const timer = setTimeout(async () => {
			loading = true;
			try {
				const response = await searchWeatherCities(name, apiKey, controller.signal);
				if (controller.signal.aborted) return;
				if (response.status === 200) {
					cities = response.data;
					searched = true;
				} else if (response.status === 401) {
					error = 'Your admin session has expired. Sign in again to search.';
				} else {
					const problem = response.data as { detail?: string } | null;
					error =
						problem?.detail ?? 'City search failed. Check your weather API key and try again.';
				}
			} catch {
				if (!controller.signal.aborted) error = 'Could not reach the server. Try searching again.';
			} finally {
				if (!controller.signal.aborted) loading = false;
			}
		}, 300);
		return () => {
			clearTimeout(timer);
			controller.abort();
			loading = false;
		};
	});

	function typeQuery(event: Event) {
		query = (event.target as HTMLInputElement).value;
		editing = true;
		selected = false;
		open = true;
		if (!query.trim()) clear();
	}

	function selectCity(next: string) {
		const city = cities.find((city) => key(city) === next);
		if (!city) return;
		settings.weatherLocation = { ...city };
		settings.weatherCityId = null;
		query = label(city);
		value = key(city);
		editing = false;
		selected = true;
		open = false;
	}

	function clear() {
		settings.weatherLocation = undefined;
		settings.weatherCityId = null;
		query = '';
		value = '';
		editing = false;
		selected = false;
		open = false;
		input?.focus();
	}

	function close() {
		open = false;
		editing = false;
		query = settings.weatherLocation
			? label(settings.weatherLocation)
			: settings.weatherCityId
				? `City ID ${settings.weatherCityId}`
				: '';
	}
</script>

<div class="my-4 border-y border-current/10 py-4">
	<label for={id} class="mb-1 block text-sm font-medium">Weather city</label>
	<Combobox.Root
		type="single"
		bind:open
		bind:value
		{items}
		inputValue={query}
		allowDeselect={false}
		onValueChange={selectCity}
		onOpenChange={(next) => {
			if (!next) close();
		}}
	>
		<div class="relative flex items-center">
			<Combobox.Input
				{id}
				bind:ref={input}
				oninput={typeQuery}
				aria-describedby={`${id}-help ${id}-status`}
				aria-busy={loading}
				placeholder="Search for a city, e.g. London"
				maxlength={150}
				class="w-full rounded-lg bg-gray-100 px-4 py-3 pr-14 text-sm outline-none ring-1 ring-gray-200 focus-visible:ring-primary dark:bg-gray-800 dark:ring-neutral-900"
			/>
			{#if query || settings.weatherLocation || settings.weatherCityId}
				<Button
					type="button"
					variant="ghost"
					color="secondary"
					size="small"
					class="absolute right-1"
					aria-label="Clear weather city"
					onclick={clear}><Icon icon={mdiClose} size="16" /></Button
				>
			{/if}
		</div>
		<Combobox.Portal>
			<Combobox.Content
				sideOffset={6}
				class="z-50 w-[var(--bits-combobox-anchor-width)] rounded-lg border border-gray-200 bg-white p-1 shadow-lg dark:border-gray-700 dark:bg-gray-900"
			>
				<Combobox.Viewport class="max-h-72 overflow-y-auto">
					{#each cities as city (key(city))}
						<Combobox.Item
							value={key(city)}
							label={label(city)}
							class="cursor-pointer rounded-md px-3 py-2 text-sm outline-none data-highlighted:bg-gray-100 dark:data-highlighted:bg-gray-800"
						>
							<span class="block">{label(city)}</span>
							<span class="block text-xs opacity-70"
								>{city.latitude?.toFixed(4)}, {city.longitude?.toFixed(4)}</span
							>
						</Combobox.Item>
					{:else}
						<p class="px-3 py-2 text-sm opacity-70">
							{loading
								? 'Searching…'
								: error
									? error
									: searched
										? emptyMessage
										: 'Type at least two characters to search.'}
						</p>
					{/each}
				</Combobox.Viewport>
			</Combobox.Content>
		</Combobox.Portal>
	</Combobox.Root>
	<div id={`${id}-help`}>
		<Text color="muted" size="small" class="pt-1"
			>{hasApiKey
				? 'Choose a city, then Save to apply. Check the country and coordinates for duplicate names. Clear to use the coordinates below.'
				: 'Enter an OpenWeatherMap API key above to search. You can search before saving it.'}</Text
		>
	</div>
	<div id={`${id}-status`} aria-live="polite" aria-atomic="true">
		{#if selected && settings.weatherLocation}
			<Text color="success" size="small"
				><Icon icon={mdiCheck} size="16" class="inline-block" /> Selected {settings.weatherLocation
					.name}, {settings.weatherLocation.country}. Save to apply this location.</Text
			>
		{:else if error}
			<Text color="danger" size="small">{error}</Text>
		{:else if searched && cities.length === 0}
			<span class="sr-only">{emptyMessage}</span>
		{:else if settings.weatherCityId && !settings.weatherLocation}
			<Text color="muted" size="small"
				>This saved ID may refer to a surrounding district. Search and select the city again to use
				its exact coordinates and name.</Text
			>
		{/if}
	</div>
</div>
