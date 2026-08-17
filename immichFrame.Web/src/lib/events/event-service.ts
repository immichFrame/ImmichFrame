import { get, writable } from 'svelte/store';
import { configStore } from '$lib/stores/config.store';

export type FrameEventMode = 'PopupText' | 'Close' | 'Banner';

export type FrameEventAckStatus = 'Shown' | 'Closed' | 'Timeout' | 'Error' | 'Dismissed';

export interface FrameEventAction {
  id: string;
  label: string;
  kind?: string | null;
}

export interface FrameEventInput {
  allowTouchDismiss: boolean;
  allowKeyboardDismiss: boolean;
}

export interface FrameEvent {
  id: string;
  type: string;
  mode: FrameEventMode;
  message?: string | null;
  timeoutMs?: number | null;
  priority: number;
  category?: string | null;
  title?: string | null;
  meta?: Record<string, unknown> | null;
  actions: FrameEventAction[];
  input: FrameEventInput;
  postedAt: string;
}

const activePopupStore = writable<FrameEvent | null>(null);
const activeBannerStore = writable<FrameEvent | null>(null);

export const activePopupEvent = {
  subscribe: activePopupStore.subscribe
};

export const activeBannerEvent = {
  subscribe: activeBannerStore.subscribe
};

export function clearActivePopupEvent() {
  activePopupStore.set(null);
}

export function clearActiveBannerEvent() {
  activeBannerStore.set(null);
}

let pollingController: AbortController | null = null;

export function startEventPolling(deviceId: string) {
  stopEventPolling();
  pollingController = new AbortController();
  void pollLoop(deviceId, pollingController);
}

export function stopEventPolling() {
  pollingController?.abort();
  pollingController = null;
}

async function pollLoop(deviceId: string, controller: AbortController) {
  const settings = get(configStore);
  const intervalMs = Math.max(500, (settings.eventPollingIntervalSeconds ?? 2) * 1000);

  while (!controller.signal.aborted) {
    if (!get(configStore).eventHostEnabled) {
      activePopupStore.set(null);
      activeBannerStore.set(null);
      await delay(intervalMs, controller.signal);
      continue;
    }

    await Promise.all([
      pollOne(deviceId, 'PopupText', activePopupStore, controller.signal),
      pollOne(deviceId, 'Banner', activeBannerStore, controller.signal)
    ]);

    await delay(intervalMs, controller.signal);
  }
}

async function pollOne(
  deviceId: string,
  mode: FrameEventMode,
  store: typeof activePopupStore,
  signal: AbortSignal
) {
  try {
    const url = `/api/events/next?deviceId=${encodeURIComponent(deviceId)}&mode=${mode}`;
    const response = await fetch(url, { method: 'GET', signal });

    if (response.status === 200) {
      const payload = (await response.json()) as FrameEvent;
      store.set(payload);
    } else if (response.status === 204) {
      store.set(null);
    } else if (!response.ok) {
      console.error(`event poll failed (mode=${mode}): HTTP ${response.status} ${response.statusText}`);
    }
  } catch (error) {
    if ((error as Error).name !== 'AbortError') {
      console.error(`event poll failed (mode=${mode})`, error);
    }
  }
}

export async function acknowledgeEvent(deviceId: string, eventId: string, status: FrameEventAckStatus) {
  if (!get(configStore).eventHostEnabled) {
    return;
  }
  try {
    const response = await fetch(`/api/events/${encodeURIComponent(eventId)}/ack?deviceId=${encodeURIComponent(deviceId)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status })
    });
    if (!response.ok) {
      console.error(`failed to acknowledge event ${eventId}: HTTP ${response.status} ${response.statusText}`);
    }
  } catch (error) {
    console.error('failed to acknowledge event', error);
  }
}

async function delay(durationMs: number, signal: AbortSignal) {
  return new Promise<void>((resolve) => {
    if (signal.aborted) {
      resolve();
      return;
    }

    const onAbort = () => {
      clearTimeout(timeout);
      resolve();
    };

    const timeout = setTimeout(() => {
      signal.removeEventListener('abort', onAbort);
      resolve();
    }, durationMs);

    signal.addEventListener('abort', onAbort, { once: true });
  });
}
