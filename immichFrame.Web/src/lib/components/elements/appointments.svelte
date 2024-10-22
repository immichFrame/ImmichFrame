<script lang="ts">
	import * as api from '$lib/immichFrameApi';
	import { onMount } from 'svelte';
	import { format, formatDate } from 'date-fns';
	import { configStore } from '$lib/stores/config.store';

	function formattedDate(time: string) {
		let date = new Date(time);
		console.log(date);
		return format(date, $configStore.clockFormat ?? 'HH:mm');
	}

	let appointments: api.IAppointment[];

	onMount(() => {
		GetAppointments();
		const appointmentInterval = setInterval(() => GetAppointments, 1 * 60 * 10000);

		return () => {
			clearInterval(appointmentInterval);
		};
	});

	async function GetAppointments() {
		let appointmentRequest = await api.getAppointments();
		if (appointmentRequest.status == 200) {
			appointments = appointmentRequest.data;

			appointments = appointmentRequest.data.sort((a, b) => {
				return new Date(a.startTime ?? '').getTime() - new Date(b.startTime ?? '').getTime();
			});
		}
	}
</script>

{#if appointments}
	<div class="absolute top-0 right-0 z-10 text-center text-primary">
		<h2 class="text-4xl">calendar</h2>
		{#each appointments as appointment}
			<div>
				{formattedDate(appointment.startTime ?? '')} - {formattedDate(appointment.endTime ?? '')}, {appointment.summary}
			</div>
		{/each}
	</div>
{/if}
