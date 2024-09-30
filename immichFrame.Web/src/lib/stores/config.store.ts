import type { ClientSettings } from '$lib/immichFrameApi';
import { writable } from 'svelte/store';

function createConfigStore(settings: ClientSettings) {
  const { subscribe, set } = writable(settings);

  function ps(settings: ClientSettings) {
    set(settings);
  }

  return { subscribe, ps }
}

export const configStore = createConfigStore({});