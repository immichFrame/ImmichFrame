import { writable } from 'svelte/store';

export type AssetSelection = {
	accountName?: string;
	albumIds?: string;
};

// Read from the URL on every load and deliberately not persisted, unlike the client identifier.
export const assetSelectionStore = writable<AssetSelection>({});

export function selectionFromUrl(url: URL): AssetSelection {
	return {
		accountName: url.searchParams.get('account') ?? undefined,
		albumIds: url.searchParams.get('albums') ?? undefined
	};
}
