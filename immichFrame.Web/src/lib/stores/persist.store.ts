
import { writable } from 'svelte/store';
import type { AssetResponseDto } from '$lib/immichFrameApi';

function persistStore(key: string, defaultValue: string | null) {
    const storedValue = localStorage?.getItem(key);
    const initialValue: string = storedValue ? JSON.parse(storedValue) : defaultValue;

    const store = writable(initialValue);

    store.subscribe((value) => {
        localStorage?.setItem(key, JSON.stringify(value));
    });

    return store;
}

function loadPersistedArray<T>(key: string, defaultValue: T[]): T[] {
    const storedValue = localStorage?.getItem(key);
    if (storedValue == null) {
        return defaultValue;
    }

    try {
        const initialValue = JSON.parse(storedValue);
        if (Array.isArray(initialValue)){
            return initialValue as T[];
        }
    } catch {
        // Corrupt value - fall back to the default.
    }

    return defaultValue;
}

function persistArrayStore<T>(key: string, defaultValue: T[]) {
    const store = writable(loadPersistedArray(key, defaultValue));

    store.subscribe((value) => {
            localStorage?.setItem(key, JSON.stringify(value));
    });

    return store;
}

export function clearPersistedStore(key: string) {
    localStorage?.removeItem(key);
}

function generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = (Math.random() * 16) | 0,
            v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}

export const clientIdentifierStore = persistStore('clientIdentifier', generateGUID());
export const authSecretStore = persistStore('authSecret', null);
export const serverSessionIdStore = persistStore('serverSessionId', null);
export const assetBacklogStore = persistArrayStore<AssetResponseDto>('assetBacklog', []);
export const assetHistoryStore = persistArrayStore<AssetResponseDto>('assetHistory', []);
export const displayingAssetsStore = persistArrayStore<AssetResponseDto>('displayingAssets', []);