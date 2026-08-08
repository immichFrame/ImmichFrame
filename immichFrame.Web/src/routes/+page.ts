import * as api from '$lib/immichFrameApi';
import { configStore } from '$lib/stores/config.store.js'
import { clientIdentifierStore } from '$lib/stores/persist.store';
import { get } from 'svelte/store';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ url }) => {

  const clientIdentifier = url.searchParams.get('client') ?? get(clientIdentifierStore);

  const configRequest = await api.getConfig({ clientIdentifier });

  const config = configRequest.data;

  configStore.ps(config);
};
