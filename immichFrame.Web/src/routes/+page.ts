import * as api from '$lib/immichFrameApi';
import { configStore } from '$lib/stores/config.store.js'
import { setBaseUrl } from '$lib/index.js';
import { base } from '$app/paths';

export const load = async () => {

  const normalizedBase = base.endsWith('/') ? base.slice(0, -1) : base;
  setBaseUrl(normalizedBase + "/");

  const configRequest = await api.getConfig({ clientIdentifier: "" });

  const config = configRequest.data;
  if (config.baseUrl) {
    const normalizedUrl = config.baseUrl.endsWith('/') ? config.baseUrl : config.baseUrl + '/';
    setBaseUrl(normalizedUrl);
  }

  configStore.ps(config);
};
