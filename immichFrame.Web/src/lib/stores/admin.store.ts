import { writable } from 'svelte/store';

// sessionStorage on purpose: the admin password evaporates when the tab closes,
// unlike the client auth secret which persists in localStorage.
function sessionStore(key: string) {
	const storedValue = typeof sessionStorage !== 'undefined' ? sessionStorage.getItem(key) : null;
	const store = writable<string | null>(storedValue);

	store.subscribe((value) => {
		if (typeof sessionStorage === 'undefined') return;
		if (value === null) {
			sessionStorage.removeItem(key);
		} else {
			sessionStorage.setItem(key, value);
		}
	});

	return store;
}

export const adminPasswordStore = sessionStore('adminPassword');
