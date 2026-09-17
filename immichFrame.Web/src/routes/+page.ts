import * as api from '$lib/immichFrameApi';
import { AdminUiState } from '$lib/immichFrameApi';
import { configStore } from '$lib/stores/config.store.js';
import { clientIdentifierStore } from '$lib/stores/persist.store';
import { redirect } from '@sveltejs/kit';
import { get } from 'svelte/store';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ url }) => {
	const clientParam = url.searchParams.get('client');
	if (clientParam) {
		clientIdentifierStore.set(clientParam);
	}

	const [configRequest, adminState] = await Promise.all([
		api.getConfig({ clientIdentifier: get(clientIdentifierStore) }),
		// Never let the slideshow fail over a status check.
		api
			.getAdminStatus()
			.then((res) => res.data.state)
			.catch(() => undefined)
	]);

	// Nothing configured yet: send the first visitor into onboarding rather than
	// showing an empty frame. Only on a fresh install — a configured instance
	// without an admin password reports "disabled" and is left alone.
	if (adminState === AdminUiState.Setup) {
		redirect(307, '/admin');
	}

	configStore.ps(configRequest.data);
};
