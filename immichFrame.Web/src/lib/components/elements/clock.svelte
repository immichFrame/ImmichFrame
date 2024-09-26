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

<div
	class="absolute bottom-0 left-0 z-10 text-primary bg-secondary bg-opacity-40 rounded-tr-2xl p-3"
>
	<p class="mt-2 text-xl">{selectedDate}</p>
</div>
