// place files you want to import through the `$lib` alias in this folder.
import { defaults } from './immichFrameApi.js';
import { authSecretStore } from '$lib/stores/persist.store';
import { get } from 'svelte/store';

export * from './immichFrameApi.js';


export const init = () => {
	setBearer();
	sendAuthSecretToServiceWorker();
};

export const sendAuthSecretToServiceWorker = () => {
	if (typeof navigator === 'undefined' || !('serviceWorker' in navigator)) return;

	const sendMessage = () => {
		if (navigator.serviceWorker.controller) {
			navigator.serviceWorker.controller.postMessage({
				type: 'SET_AUTH_SECRET',
				authSecret: get(authSecretStore)
			});
		}
	};

	// Send immediately if controller is ready
	sendMessage();

	// Also send when service worker becomes ready (for initial page load)
	navigator.serviceWorker.ready.then(sendMessage);

	// Listen for auth secret requests from service worker
	navigator.serviceWorker.addEventListener('message', (event) => {
		if (event.data && event.data.type === 'REQUEST_AUTH_SECRET') {
			sendMessage();
		}
	});
};

export const getBaseUrl = () => defaults.baseUrl;

export const setBaseUrl = (baseUrl: string) => {
	defaults.baseUrl = baseUrl;
};

export const setBearer = () => {
	defaults.headers = defaults.headers || {};
	defaults.headers['Authorization'] = "Bearer " + get(authSecretStore);
};

export const getAssetStreamUrl = (id: string, clientIdentifier?: string, assetType?: number) => {
	const params = new URLSearchParams();
	if (clientIdentifier) params.set('clientIdentifier', clientIdentifier);
	if (assetType !== undefined) params.set('assetType', String(assetType));
	const query = params.toString();
	return `/api/Asset/${encodeURIComponent(id)}/Asset${query ? '?' + query : ''}`;
};
