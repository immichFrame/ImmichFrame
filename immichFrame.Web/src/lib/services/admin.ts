import { get } from 'svelte/store';
import * as api from '$lib/immichFrameApi';
import type { ServerAccountSettings, ServerSettings } from '$lib/immichFrameApi';
import { adminPasswordStore } from '$lib/stores/admin.store';

// Every admin endpoint authenticates with the dedicated admin password rather
// than the client auth secret that $lib/index installs globally, so these calls
// carry their own header instead of going through the shared defaults.
function authOpts() {
	return { headers: { Authorization: 'Bearer ' + get(adminPasswordStore) } };
}

export function getStatus() {
	return api.getAdminStatus();
}

// Anonymous on purpose — this is the only way into a fresh install. The server
// refuses it once an admin password exists.
export function setup(adminPassword: string) {
	return api.setupAdmin({ adminPassword });
}

export function getSettings() {
	return api.getAdminSettings(authOpts());
}

export function updateSettings(settings: ServerSettings) {
	return api.updateAdminSettings(clean(settings), authOpts());
}

export function testAccount(account: ServerAccountSettings) {
	return api.testAccount(account, authOpts());
}

// Drop empty values so the server falls back to its defaults instead of
// receiving empty strings / nulls for unset fields. The PUT replaces the whole
// document, so an omitted key deserialises to the C# default.
function clean(settings: ServerSettings): ServerSettings {
	return {
		General: dropEmpty(settings.General ?? {}),
		Accounts: (settings.Accounts ?? []).map((account) => dropEmpty(account))
	};
}

function dropEmpty<T extends Record<string, unknown>>(obj: T): T {
	const result: Record<string, unknown> = {};
	for (const [key, value] of Object.entries(obj)) {
		if (value === null || value === undefined || value === '') continue;
		result[key] = value;
	}
	return result as T;
}
