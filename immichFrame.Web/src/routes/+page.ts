import * as api from '$lib/immichFrameApi';
import { configStore } from '$lib/stores/config.store.js'

export const load = async ({ fetch }) => {

  const configRequest = await api.getConfig({ fetch });

  const config = configRequest.data;

  console.log(config)

  configStore.ps(config);
};
