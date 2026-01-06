import * as api from '$lib/immichFrameApi';
import { configStore } from '$lib/stores/config.store.js'
import { setBaseUrl } from '$lib/index.js';

export const load = async () => {

  const configRequest = await api.getConfig({ clientIdentifier: "" });

  const config = configRequest.data;
  if (config.baseUrl) {
    setBaseUrl(config.baseUrl);
  }

  configStore.ps(config);
};
