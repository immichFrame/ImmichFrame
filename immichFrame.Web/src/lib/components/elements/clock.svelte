<script lang="ts">
	import * as api from '$lib/index';
	import { onMount } from 'svelte';
	import { format } from 'date-fns';
	import * as locale from 'date-fns/locale';
	import { configStore } from '$lib/stores/config.store';
	import { clientIdentifierStore } from '$lib/stores/persist.store';
	import * as icons from '$lib/assets/icons';

	api.init();

	const localeToUse = $derived(
		() => locale[$configStore.language as keyof typeof locale] ?? locale.enUS
	);

	let now = $state(new Date());
	
	const formattedDate = $derived(() =>
		format(now, $configStore.clockDateFormat ?? 'eee, MMM d', {
			locale: localeToUse()
		})
	);

	const timePortion = $derived(() => format(now, $configStore.clockFormat ?? 'HH:mm:ss'));

	onMount(() => {
		const interval = setInterval(() => {
			now = new Date();
		}, 1000);

		return () => {
			clearInterval(interval);
		};
	});

</script>

<div
	id="clock"
	class="fixed bottom-0 left-0 z-10 text-center text-primary
	{$configStore.style == 'solid' ? 'bg-secondary rounded-tr-2xl' : ''}
	{$configStore.style == 'transition' ? 'bg-gradient-to-r from-secondary from-0% pr-10' : ''}
	{$configStore.style == 'blur' ? 'backdrop-blur-lg rounded-tr-2xl' : ''}	
	drop-shadow-2xl p-3"
>
	<p id="clockdate" class="mt-2 text-sm sm:text-sm md:text-md lg:text-xl font-thin text-shadow-sm">
		{formattedDate()}
	</p>
	<p
		id="clocktime"
		class="mt-2 text-4xl sm:text-4xl md:text-6xl lg:text-8xl font-bold text-shadow-lg"
	>
		{timePortion()}
	</p>
</div>
