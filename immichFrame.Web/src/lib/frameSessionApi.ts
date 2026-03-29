import { get } from 'svelte/store';
import { authSecretStore } from '$lib/stores/persist.store';

export type FramePlaybackState = 'Playing' | 'Paused';
export type FrameSessionStatus = 'Active' | 'Stopped';
export type FrameAdminCommandType =
	| 'Previous'
	| 'Play'
	| 'Pause'
	| 'Next'
	| 'Refresh'
	| 'Shutdown';

export interface DisplayedAssetDto {
	id: string;
	originalFileName: string;
	type: number;
	immichServerUrl?: string | null;
	localDateTime?: string | null;
	description?: string | null;
	thumbhash?: string | null;
}

export interface DisplayEventDto {
	displayedAtUtc: string;
	durationSeconds?: number | null;
	assets: DisplayedAssetDto[];
}

export interface FrameSessionSnapshotDto {
	playbackState: FramePlaybackState;
	status: FrameSessionStatus;
	displayName?: string | null;
	currentDisplay?: DisplayEventDto | null;
	history: DisplayEventDto[];
}

export interface FrameSessionStateDto extends FrameSessionSnapshotDto {
	clientIdentifier: string;
	connectedAtUtc: string;
	lastSeenAtUtc: string;
	userAgent?: string | null;
}

export interface AdminCommandDto {
	commandId: number;
	commandType: FrameAdminCommandType;
	issuedAtUtc: string;
}

export interface AdminAuthSessionDto {
	isConfigured: boolean;
	isAuthenticated: boolean;
	username?: string | null;
}

export class FrameSessionApiError extends Error {
	status: number;

	constructor(message: string, status: number) {
		super(message);
		this.name = 'FrameSessionApiError';
		this.status = status;
	}
}

function getHeaders(includeJsonBody = false): HeadersInit {
	const headers: Record<string, string> = {};
	const authSecret = get(authSecretStore);

	if (authSecret) {
		headers.Authorization = `Bearer ${authSecret}`;
	}

	if (includeJsonBody) {
		headers['Content-Type'] = 'application/json';
	}

	return headers;
}

async function readJson<T>(response: Response): Promise<T> {
	return (await response.json()) as T;
}

function throwIfNotOk(response: Response, message: string) {
	if (!response.ok) {
		throw new FrameSessionApiError(message, response.status);
	}
}

export async function putFrameSessionSnapshot(
	clientIdentifier: string,
	snapshot: FrameSessionSnapshotDto
) {
	return fetch(`/api/frame-sessions/${encodeURIComponent(clientIdentifier)}`, {
		method: 'PUT',
		headers: getHeaders(true),
		body: JSON.stringify(snapshot)
	});
}

export async function getFrameSessionCommands(clientIdentifier: string) {
	try {
		const response = await fetch(
			`/api/frame-sessions/${encodeURIComponent(clientIdentifier)}/commands`,
			{
				headers: getHeaders()
			}
		);

		if (!response.ok) {
			const responseText = await response.text();
			console.error(
				`Failed to fetch frame session commands for ${clientIdentifier}: ${response.status} ${responseText}`
			);
			return [];
		}

		return await readJson<AdminCommandDto[]>(response);
	} catch (error) {
		console.error(`Failed to fetch frame session commands for ${clientIdentifier}:`, error);
		return [];
	}
}

export async function acknowledgeFrameSessionCommand(clientIdentifier: string, commandId: number) {
	return fetch(`/api/frame-sessions/${encodeURIComponent(clientIdentifier)}/commands/${commandId}/ack`, {
		method: 'POST',
		headers: getHeaders()
	});
}

export async function disconnectFrameSession(
	clientIdentifier: string,
	keepalive = false,
	authSecret?: string | null
) {
	return fetch(`/api/frame-sessions/${encodeURIComponent(clientIdentifier)}/disconnect`, {
		method: 'POST',
		headers: authSecret ? getHeaders(true) : getHeaders(),
		body: authSecret ? JSON.stringify({ authSecret }) : undefined,
		keepalive
	});
}

export function sendBeaconFrameSessionDisconnect(
	clientIdentifier: string,
	authSecret?: string | null
) {
	if (typeof navigator === 'undefined' || typeof navigator.sendBeacon !== 'function') {
		return false;
	}

	const payload = JSON.stringify({
		authSecret: authSecret ?? null
	});
	const body = new Blob([payload], { type: 'application/json' });
	return navigator.sendBeacon(
		`/api/frame-sessions/${encodeURIComponent(clientIdentifier)}/disconnect`,
		body
	);
}

export async function getAdminFrameSessions() {
	const response = await fetch('/api/admin/frame-sessions', {
		credentials: 'same-origin'
	});

	throwIfNotOk(response, `Failed to fetch frame sessions: ${response.status}`);

	return readJson<FrameSessionStateDto[]>(response);
}

export async function enqueueAdminCommand(
	clientIdentifier: string,
	commandType: FrameAdminCommandType
) {
	const response = await fetch(`/api/admin/frame-sessions/${encodeURIComponent(clientIdentifier)}/commands`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		credentials: 'same-origin',
		body: JSON.stringify({ commandType })
	});

	throwIfNotOk(response, `Failed to enqueue command: ${response.status}`);

	return readJson<AdminCommandDto>(response);
}

export async function updateAdminFrameSessionDisplayName(
	clientIdentifier: string,
	displayName: string | null
) {
	const response = await fetch(
		`/api/admin/frame-sessions/${encodeURIComponent(clientIdentifier)}/display-name`,
		{
			method: 'PUT',
			headers: { 'Content-Type': 'application/json' },
			credentials: 'same-origin',
			body: JSON.stringify({ displayName })
		}
	);

	throwIfNotOk(response, `Failed to update session name: ${response.status}`);
}

export async function getAdminAuthSession() {
	const response = await fetch('/api/admin/auth/session', {
		credentials: 'same-origin'
	});

	throwIfNotOk(response, `Failed to fetch admin session: ${response.status}`);
	return readJson<AdminAuthSessionDto>(response);
}

export async function loginAdmin(username: string, password: string) {
	const response = await fetch('/api/admin/auth/login', {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		credentials: 'same-origin',
		body: JSON.stringify({ username, password })
	});

	throwIfNotOk(response, `Failed to log in: ${response.status}`);
	return readJson<AdminAuthSessionDto>(response);
}

export async function logoutAdmin() {
	const response = await fetch('/api/admin/auth/logout', {
		method: 'POST',
		credentials: 'same-origin'
	});

	throwIfNotOk(response, `Failed to log out: ${response.status}`);
}
