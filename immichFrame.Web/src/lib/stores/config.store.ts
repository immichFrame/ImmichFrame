import type { ClientSettingsDto } from '$lib/immichFrameApi';
import { writable } from 'svelte/store';

function createConfigStore(settings: ClientSettingsDto) {
  const { subscribe, set } = writable(settings);

  function ps(settings: ClientSettingsDto) {
    set(settings);
  }

  return { subscribe, ps }
}

export const configStore = createConfigStore({});