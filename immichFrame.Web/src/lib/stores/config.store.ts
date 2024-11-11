import type { WebClientSettings } from '$lib/immichFrameApi';
import { writable } from 'svelte/store';

function createConfigStore(settings: WebClientSettings) {
  const { subscribe, set } = writable(settings);

  function ps(settings: WebClientSettings) {
    if (settings.fontColor) {
      document.documentElement.style.setProperty('--primary-color', settings.fontColor);
    }
    set(settings);
  }

  return { subscribe, ps }
}

export const configStore = createConfigStore({});