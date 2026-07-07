<script lang="ts">
	import * as api from '$lib/index';
	import { onMount } from 'svelte';
	import { configStore } from '$lib/stores/config.store';
	import { clientIdentifierStore } from '$lib/stores/persist.store';

	api.init();

	let widgetData: api.CustomWidgetData[] = $state([]);

	const positionClasses: Record<string, string> = {
		'top-left': 'top-0 left-0',
		'top-right': 'top-0 right-0',
		'bottom-left': 'bottom-0 left-0',
		'bottom-right': 'bottom-0 right-0'
	};

	let position = $derived(
		positionClasses[$configStore.customWidgetPosition ?? 'top-left'] ?? positionClasses['top-left']
	);

	onMount(() => {
		fetchWidgetData();
		const interval = setInterval(() => fetchWidgetData(), 60 * 1000);

		return () => {
			clearInterval(interval);
		};
	});

	async function fetchWidgetData() {
		try {
			let response = await api.getCustomWidgetData({
				clientIdentifier: $clientIdentifierStore
			});
			if (response.status === 200) {
				widgetData = response.data;
			}
		} catch (err) {
			console.error('Error fetching custom widget data:', err);
		}
	}
</script>

{#if widgetData.length > 0}
	<div
		class="fixed {position} w-auto z-10 text-primary m-5 max-w-[20%] hidden lg:block md:min-w-[10%]"
	>
		{#each widgetData as widget}
			<div class="bg-gray-600 bg-opacity-90 mb-2 rounded-md p-3">
				{#if widget.title}
					<p class="text-xs font-bold mb-1">{widget.title}</p>
				{/if}
				{#each widget.items ?? [] as item}
					<div class="flex justify-between items-baseline gap-2 py-0.5">
						<span class="text-xs opacity-80">{item.label}</span>
						<span class="text-sm font-medium">{item.value}</span>
					</div>
					{#if item.secondary}
						<p class="text-xs opacity-60 text-right -mt-0.5">{item.secondary}</p>
					{/if}
				{/each}
			</div>
		{/each}
	</div>
{/if}
