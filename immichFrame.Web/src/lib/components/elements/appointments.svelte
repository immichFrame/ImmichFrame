<script lang="ts">
	import * as api from '$lib/index';
	import { onMount } from 'svelte';
	import { format } from 'date-fns';
	import { configStore } from '$lib/stores/config.store';
	import { clientIdentifierStore } from '$lib/stores/persist.store';
	import Icon from './icon.svelte';
	import { mdiMapMarker} from '@mdi/js';

	api.init();

	function formatDates(startTime: string, endTime: string) {
		let startDate = new Date(startTime);
		let endDate = new Date(endTime);
		let sameDay = startDate.getDate() == endDate.getDate();

		let clockFormat = $configStore.clockFormat ?? 'HH:mm';
		let clockDateFormat = $configStore.clockDateFormat ?? 'eee, MMM d';
		let fullFormat = clockDateFormat + ' ' + clockFormat;

		if (sameDay) {
			return format(startDate, clockFormat) + ' - ' + format(endDate, clockFormat);
		}

		return format(startDate, fullFormat) + ' - ' + format(endDate, fullFormat);
	}

	let appointments: api.IAppointment[] = $state() as api.IAppointment[];

	onMount(() => {
		GetAppointments();
		const appointmentInterval = setInterval(() => GetAppointments(), 10 * 60 * 1000); //every 10 minutes

		return () => {
			clearInterval(appointmentInterval);
		};
	});

	async function GetAppointments() {
		let appointmentRequest = await api.getAppointments({
			clientIdentifier: $clientIdentifierStore
		});
		if (appointmentRequest.status == 200) {
			appointments = appointmentRequest.data;

			appointments = appointmentRequest.data.sort((a, b) => {
				return new Date(a.startTime ?? '').getTime() - new Date(b.startTime ?? '').getTime();
			});
		}
	}
</script>

{#if appointments}
	<div
		id="appointments"
		class="fixed top-0 right-0 w-auto z-10 text-center text-primary mr-1 mt-5 max-w-[20%] hidden lg:block md:min-w-[10%]"
	>
		<!-- <div class="text-4xl mx-8 font-bold">Appointments</div> -->
		<div class="">
			{#each appointments as appointment}
				<div class="bg-gray-600 bg-opacity-90 mb-2 text-left rounded-md p-3">
					<p class="text-s">
						<b><u>{format(new Date(appointment.startTime ?? ''), 'EEEE MMMM dd, yyyy')}</u></b>
					</p>
					<p class="text-xs">
						{format(appointment.startTime ?? '', 'hh:mm b')} - {format(appointment.endTime ?? '', 'hh:mm b')}
					</p>
					{appointment.summary}
					{#if appointment.location}
						<p class="text-xs font-light">
							<!-- <Icon path={mdiMapMarker}/> -->
							<i>{appointment.location}</i>
						</p>
					{/if}
					<!--
					{#if appointment.description}
						<br />
						<p class="text-xs font-light">{appointment.description}</p>
					{/if}
					-->
				</div>
			{/each}
		</div>
	</div>
{/if}
