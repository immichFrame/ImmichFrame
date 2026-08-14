import * as api from '$lib/immichFrameApi';
import { configStore } from '$lib/stores/config.store.js'
import { clientIdentifierStore } from '$lib/stores/persist.store';
import { get } from 'svelte/store';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ url }) => {

  const clientParam = url.searchParams.get('client');
  if (clientParam) {
    clientIdentifierStore.set(clientParam);
  }

  const configRequest = await api.getConfig({ clientIdentifier: get(clientIdentifierStore) });

  const config = configRequest.data;

  configStore.ps(config);
};
