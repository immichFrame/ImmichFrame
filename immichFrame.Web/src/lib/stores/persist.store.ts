import { writable } from 'svelte/store';

function persistStore(key: string, defaultValue: string) {
	let initialValue = defaultValue;
	try {
		const stored = localStorage.getItem(key);
		const value = stored && JSON.parse(stored);
		if (typeof value === 'string' && value) initialValue = value;
	} catch {
		/* Use an in-memory store when storage is unavailable. */
	}
	const store = writable(initialValue);
	store.subscribe((value) => {
		try {
			localStorage.setItem(key, JSON.stringify(value));
		} catch {
			/* The in-memory value still updates. */
		}
	});

	return store;
}

function generateGUID() {
	return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
		const r = (Math.random() * 16) | 0,
			v = c === 'x' ? r : (r & 0x3) | 0x8;
		return v.toString(16);
	});
}

export const clientIdentifierStore = persistStore('clientIdentifier', generateGUID());
export const authSecretStore = persistStore('authSecret', '');
