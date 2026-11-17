import type { ClientSettingsDto } from '$lib/immichFrameApi';
import { writable } from 'svelte/store';

export type ClientSettingsWithUx = ClientSettingsDto & {
  eventPollingIntervalSeconds?: number;
  eventDefaultTimeoutMs?: number;
  eventHostEnabled?: boolean;
};

function createConfigStore(settings: ClientSettingsWithUx) {
  const { subscribe, set, update } = writable<ClientSettingsWithUx>(settings);

  function ps(settings: ClientSettingsDto) {
    set({
      ...settings,
      eventHostEnabled: settings.eventHostEnabled ?? false
    } as ClientSettingsWithUx);
  }

  function patch(partial: Partial<ClientSettingsWithUx>) {
    update((current) => ({ ...current, ...partial }));
  }

  return { subscribe, ps, patch };
}

export const configStore = createConfigStore({} as ClientSettingsWithUx);