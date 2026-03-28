<script lang="ts">
	import { mdiCheck, mdiLogout, mdiPencilOutline } from '@mdi/js';
	import { onDestroy, onMount } from 'svelte';
	import ErrorElement from '$lib/components/elements/error-element.svelte';
	import Icon from '$lib/components/elements/icon.svelte';
	import {
		enqueueAdminCommand,
		FrameSessionApiError,
		getAdminAuthSession,
		getAdminFrameSessions,
		loginAdmin,
		logoutAdmin,
		updateAdminFrameSessionDisplayName
	} from '$lib/frameSessionApi';
	import type {
		AdminAuthSessionDto,
		DisplayedAssetDto,
		FrameAdminCommandType,
		FrameSessionStateDto
	} from '$lib/frameSessionApi';

	const REFRESH_INTERVAL_MS = 5_000;
	const PROGRESS_REFRESH_INTERVAL_MS = 250;

	interface FrozenProgressSnapshot {
		displayedAtUtc: string;
		durationSeconds: number;
		progress: number;
	}

	let adminSession: AdminAuthSessionDto | null = $state(null);
	let sessions: FrameSessionStateDto[] = $state([]);
	let sessionOrder: string[] = $state([]);
	let authLoading = $state(true);
	let loading = $state(false);
	let hasLoadedSessionsOnce = $state(false);
	let fatalErrorMessage = $state('');
	let errorMessage = $state('');
	let authErrorMessage = $state('');
	let loginUsername = $state('');
	let loginPassword = $state('');
	let loginPending = $state(false);
	let nowMs = $state(Date.now());
	let actionState: Record<string, FrameAdminCommandType | null> = $state({});
	let displayNameDrafts: Record<string, string> = $state({});
	let savingDisplayName: Record<string, boolean> = $state({});
	let frozenProgressBySession: Record<string, FrozenProgressSnapshot> = $state({});
	let editingSessionId: string | null = $state(null);
	let refreshIntervalId: number | undefined;
	let progressIntervalId: number | undefined;

	function formatDateTime(value?: string | null) {
		if (!value) {
			return 'Unknown';
		}

		return new Intl.DateTimeFormat(undefined, {
			dateStyle: 'medium',
			timeStyle: 'short'
		}).format(new Date(value));
	}

	function formatAssetType(type: number) {
		switch (type) {
			case 0:
				return 'Image';
			case 1:
				return 'Video';
			case 2:
				return 'Audio';
			default:
				return 'Other';
		}
	}

	function getCurrentAssets(session: FrameSessionStateDto) {
		return session.currentDisplay?.assets ?? [];
	}

	function getHistory(session: FrameSessionStateDto) {
		return session.history ?? [];
	}

	function getDisplayName(session: FrameSessionStateDto) {
		return session.displayName?.trim() || `Frame ${session.clientIdentifier.slice(0, 8)}`;
	}

	function parseUserAgent(userAgent?: string | null) {
		if (!userAgent) {
			return {
				summary: 'Unknown device',
				details: ''
			};
		}

		const ua = userAgent;
		const os = ua.includes('Windows')
			? 'Windows'
			: ua.includes('Mac OS X')
				? 'macOS'
				: ua.includes('Android')
					? 'Android'
					: ua.includes('iPhone') || ua.includes('iPad')
						? 'iOS'
						: ua.includes('Linux')
							? 'Linux'
							: 'Unknown OS';

		const browser = ua.includes('Edg/')
			? 'Microsoft Edge'
			: ua.includes('Chrome/')
				? 'Chrome'
				: ua.includes('Firefox/')
					? 'Firefox'
					: ua.includes('Safari/') && !ua.includes('Chrome/')
						? 'Safari'
						: 'Browser';

		return {
			summary: `${browser} on ${os}`,
			details: userAgent
		};
	}

	function getDurationSeconds(session: FrameSessionStateDto) {
		return session.currentDisplay?.durationSeconds ?? null;
	}

	function clampProgress(progress: number) {
		return Math.max(0, Math.min(1, progress));
	}

	function computeRawProgress(session: FrameSessionStateDto, referenceMs: number) {
		const currentDisplay = session.currentDisplay;
		const durationSeconds = getDurationSeconds(session);
		if (!currentDisplay?.displayedAtUtc || !durationSeconds || durationSeconds <= 0) {
			return 0;
		}

		const startedAtMs = new Date(currentDisplay.displayedAtUtc).getTime();
		const durationMs = durationSeconds * 1000;
		return clampProgress((referenceMs - startedAtMs) / durationMs);
	}

	function syncDrafts(data: FrameSessionStateDto[]) {
		const nextDrafts: Record<string, string> = {};
		for (const session of data) {
			nextDrafts[session.clientIdentifier] =
				displayNameDrafts[session.clientIdentifier] ?? session.displayName ?? '';
		}
		displayNameDrafts = nextDrafts;
	}

	function syncFrozenProgress(data: FrameSessionStateDto[]) {
		const nextFrozenProgress: Record<string, FrozenProgressSnapshot> = {};

		for (const session of data) {
			const currentDisplay = session.currentDisplay;
			const durationSeconds = getDurationSeconds(session);

			if (!currentDisplay?.displayedAtUtc || !durationSeconds || durationSeconds <= 0) {
				continue;
			}

			if (session.playbackState !== 'Paused') {
				continue;
			}

			const existing = frozenProgressBySession[session.clientIdentifier];
			if (
				existing &&
				existing.displayedAtUtc === currentDisplay.displayedAtUtc &&
				existing.durationSeconds === durationSeconds
			) {
				nextFrozenProgress[session.clientIdentifier] = existing;
				continue;
			}

			nextFrozenProgress[session.clientIdentifier] = {
				displayedAtUtc: currentDisplay.displayedAtUtc,
				durationSeconds,
				progress: computeRawProgress(session, Date.now())
			};
		}

		frozenProgressBySession = nextFrozenProgress;
	}

	function orderSessions(data: FrameSessionStateDto[]) {
		const presentSessionIds = new Set(data.map((session) => session.clientIdentifier));
		const nextOrder = sessionOrder.filter((sessionId) => presentSessionIds.has(sessionId));

		for (const session of data) {
			if (!nextOrder.includes(session.clientIdentifier)) {
				nextOrder.push(session.clientIdentifier);
			}
		}

		sessionOrder = nextOrder;
		const orderIndex = new Map(nextOrder.map((sessionId, index) => [sessionId, index]));

		return [...data].sort(
			(a, b) =>
				(orderIndex.get(a.clientIdentifier) ?? Number.MAX_SAFE_INTEGER) -
				(orderIndex.get(b.clientIdentifier) ?? Number.MAX_SAFE_INTEGER)
		);
	}

	function getProgress(session: FrameSessionStateDto) {
		const currentDisplay = session.currentDisplay;
		const durationSeconds = getDurationSeconds(session);
		if (!currentDisplay?.displayedAtUtc || !durationSeconds || durationSeconds <= 0) {
			return 0;
		}

		const frozen = frozenProgressBySession[session.clientIdentifier];
		if (
			session.playbackState === 'Paused' &&
			frozen &&
			frozen.displayedAtUtc === currentDisplay.displayedAtUtc &&
			frozen.durationSeconds === durationSeconds
		) {
			return frozen.progress;
		}

		return computeRawProgress(session, nowMs);
	}

	function getProgressWidth(session: FrameSessionStateDto) {
		return `${Math.round(getProgress(session) * 100)}%`;
	}

	function getCurrentMediaCardStyle(session: FrameSessionStateDto) {
		const progressWidth = getProgressWidth(session);
		const fillColor = 'color-mix(in srgb, var(--primary-color) 30%, transparent)';
		const baseColor = 'rgba(0, 0, 0, 0.28)';

		return `background-image: linear-gradient(90deg, ${fillColor} 0%, ${fillColor} ${progressWidth}, ${baseColor} ${progressWidth}, ${baseColor} 100%);`;
	}

	function getDisplayNameInputWidth(clientIdentifier: string) {
		const draftLength = displayNameDrafts[clientIdentifier]?.length ?? 0;
		return `${Math.min(Math.max(draftLength + 2, 16), 28)}ch`;
	}

	function getAssetUrl(asset: DisplayedAssetDto) {
		const baseUrl = asset.immichServerUrl?.trim();
		if (!baseUrl) {
			return null;
		}

		return `${baseUrl.replace(/\/+$/, '')}/photos/${encodeURIComponent(asset.id)}`;
	}

	function isUnauthorizedError(error: unknown) {
		return error instanceof FrameSessionApiError && error.status === 401;
	}

	function clearDashboardState() {
		sessions = [];
		sessionOrder = [];
		actionState = {};
		displayNameDrafts = {};
		savingDisplayName = {};
		frozenProgressBySession = {};
		editingSessionId = null;
		loading = false;
		hasLoadedSessionsOnce = false;
		errorMessage = '';
	}

	function applyLoggedOutState(message = '') {
		adminSession = {
			isConfigured: adminSession?.isConfigured ?? true,
			isAuthenticated: false,
			username: null
		};
		authErrorMessage = message;
		loginPassword = '';
		stopRefreshing();
		clearDashboardState();
	}

	async function refreshAdminSession() {
		const session = await getAdminAuthSession();
		adminSession = session;
		return session;
	}

	async function loadSessions(showSpinner = false) {
		if (showSpinner && !hasLoadedSessionsOnce) {
			loading = true;
		}

		try {
			const data = await getAdminFrameSessions();
			sessions = orderSessions(data);
			syncDrafts(sessions);
			syncFrozenProgress(sessions);
			hasLoadedSessionsOnce = true;
			errorMessage = '';
		} catch (err) {
			if (isUnauthorizedError(err)) {
				applyLoggedOutState('Your admin session expired. Sign in again to keep managing frames.');
				return;
			}

			console.warn('Failed to load frame sessions:', err);
			errorMessage = 'The admin dashboard could not load its session data right now.';
		} finally {
			if (showSpinner) {
				loading = false;
			}
		}
	}

	async function initializeAdminPage() {
		authLoading = true;
		fatalErrorMessage = '';

		try {
			const session = await refreshAdminSession();
			authErrorMessage = '';

			if (!session.isAuthenticated) {
				stopRefreshing();
				clearDashboardState();
				return;
			}

			await loadSessions(true);
			startRefreshing();
		} catch (err) {
			console.warn('Failed to initialize admin dashboard:', err);
			fatalErrorMessage =
				'The admin login page could not reach the server. Check that the backend is running and refresh.';
			stopRefreshing();
			clearDashboardState();
		} finally {
			authLoading = false;
		}
	}

	async function submitLogin() {
		loginPending = true;
		authErrorMessage = '';

		try {
			adminSession = await loginAdmin(loginUsername.trim(), loginPassword);
			loginPassword = '';
			await loadSessions(true);
			startRefreshing();
		} catch (err) {
			if (err instanceof FrameSessionApiError) {
				if (err.status === 401) {
					authErrorMessage = 'That username or password was not accepted.';
				} else if (err.status === 503) {
					authErrorMessage =
						'Admin login is not configured. Add IMMICHFRAME_AUTH_BASIC_* credentials to your environment and restart the app.';
				} else {
					authErrorMessage = 'The admin login request failed. Try again in a moment.';
				}
			} else {
				authErrorMessage = 'The admin login request failed. Try again in a moment.';
			}
		} finally {
			loginPending = false;
		}
	}

	async function handleLogout() {
		try {
			await logoutAdmin();
		} catch (err) {
			if (!isUnauthorizedError(err)) {
				console.warn('Failed to log out admin session:', err);
			}
		}

		applyLoggedOutState('');
	}

	async function sendCommand(clientIdentifier: string, commandType: FrameAdminCommandType) {
		actionState = {
			...actionState,
			[clientIdentifier]: commandType
		};

		try {
			await enqueueAdminCommand(clientIdentifier, commandType);
			await loadSessions();
		} catch (err) {
			if (isUnauthorizedError(err)) {
				applyLoggedOutState('Your admin session expired. Sign in again to keep managing frames.');
				return;
			}

			console.warn('Failed to enqueue admin command:', err);
		} finally {
			actionState = {
				...actionState,
				[clientIdentifier]: null
			};
		}
	}

	function beginEditingName(session: FrameSessionStateDto) {
		displayNameDrafts = {
			...displayNameDrafts,
			[session.clientIdentifier]: session.displayName ?? ''
		};
		editingSessionId = session.clientIdentifier;
	}

	function cancelEditingName(session: FrameSessionStateDto) {
		displayNameDrafts = {
			...displayNameDrafts,
			[session.clientIdentifier]: session.displayName ?? ''
		};
		if (editingSessionId === session.clientIdentifier) {
			editingSessionId = null;
		}
	}

	async function saveDisplayName(clientIdentifier: string) {
		savingDisplayName = {
			...savingDisplayName,
			[clientIdentifier]: true
		};

		try {
			const nextName = displayNameDrafts[clientIdentifier]?.trim() || null;
			await updateAdminFrameSessionDisplayName(clientIdentifier, nextName);
			editingSessionId = null;
			await loadSessions();
		} catch (err) {
			if (isUnauthorizedError(err)) {
				applyLoggedOutState('Your admin session expired. Sign in again to keep managing frames.');
				return;
			}

			console.warn('Failed to update session name:', err);
		} finally {
			savingDisplayName = {
				...savingDisplayName,
				[clientIdentifier]: false
			};
		}
	}

	function handleEditBlur(session: FrameSessionStateDto, event: FocusEvent) {
		const relatedTarget = event.relatedTarget as HTMLElement | null;
		if (relatedTarget?.dataset.saveNameFor === session.clientIdentifier) {
			return;
		}

		cancelEditingName(session);
	}

	function getActionLabel(session: FrameSessionStateDto) {
		return session.playbackState === 'Paused' ? 'Play' : 'Pause';
	}

	function getActionCommand(session: FrameSessionStateDto): FrameAdminCommandType {
		return session.playbackState === 'Paused' ? 'Play' : 'Pause';
	}

	function startRefreshing() {
		stopRefreshing();

		refreshIntervalId = window.setInterval(() => {
			void loadSessions();
		}, REFRESH_INTERVAL_MS);

		progressIntervalId = window.setInterval(() => {
			nowMs = Date.now();
		}, PROGRESS_REFRESH_INTERVAL_MS);
	}

	function stopRefreshing() {
		if (refreshIntervalId) {
			clearInterval(refreshIntervalId);
			refreshIntervalId = undefined;
		}

		if (progressIntervalId) {
			clearInterval(progressIntervalId);
			progressIntervalId = undefined;
		}
	}

	onMount(() => {
		void initializeAdminPage();
	});

	onDestroy(() => {
		stopRefreshing();
	});
</script>

<svelte:head>
	<title>immichFrame Admin</title>
</svelte:head>

{#if fatalErrorMessage}
	<section class="min-h-dvh bg-[#10100e]">
		<ErrorElement message={fatalErrorMessage} />
	</section>
{:else if authLoading}
	<section class="min-h-dvh bg-[radial-gradient(circle_at_top,_rgba(245,222,179,0.16),_transparent_34%),linear-gradient(180deg,#14130f_0%,#0f100d_48%,#0b0c09_100%)] px-4 py-6 text-stone-100 sm:px-6 lg:px-10">
		<div class="mx-auto max-w-xl rounded-[2rem] border border-white/10 bg-white/5 px-6 py-12 text-center shadow-2xl backdrop-blur">
			Checking admin session...
		</div>
	</section>
{:else if !adminSession?.isConfigured}
	<section class="min-h-dvh bg-[radial-gradient(circle_at_top,_rgba(245,222,179,0.16),_transparent_34%),linear-gradient(180deg,#14130f_0%,#0f100d_48%,#0b0c09_100%)] px-4 py-6 text-stone-100 sm:px-6 lg:px-10">
		<div class="mx-auto flex max-w-xl flex-col gap-5 rounded-[2rem] border border-white/10 bg-white/5 px-6 py-8 shadow-2xl backdrop-blur">
			<p class="text-xs uppercase tracking-[0.45em] text-[color:var(--primary-color)]">
				Admin Login
			</p>
			<h1 class="text-3xl font-semibold tracking-tight">Admin access is not configured.</h1>
			<p class="text-sm text-stone-300">
				Add at least one `IMMICHFRAME_AUTH_BASIC_*_USER` and matching
				`IMMICHFRAME_AUTH_BASIC_*_HASH` value to your environment file, then restart the app.
			</p>
			<div class="rounded-2xl border border-white/10 bg-black/25 px-4 py-4 text-sm text-stone-300">
				The admin login page uses the same `.env` htpasswd credentials you already configured for
				admin access. No separate in-app account store is used.
			</div>
		</div>
	</section>
{:else if !adminSession.isAuthenticated}
	<section class="min-h-dvh bg-[radial-gradient(circle_at_top,_rgba(245,222,179,0.16),_transparent_34%),linear-gradient(180deg,#14130f_0%,#0f100d_48%,#0b0c09_100%)] px-4 py-6 text-stone-100 sm:px-6 lg:px-10">
		<div class="mx-auto flex max-w-xl flex-col gap-6 rounded-[2rem] border border-white/10 bg-white/5 px-6 py-8 shadow-2xl backdrop-blur">
			<div>
				<p class="text-xs uppercase tracking-[0.45em] text-[color:var(--primary-color)]">
					Admin Login
				</p>
				<h1 class="mt-3 text-3xl font-semibold tracking-tight">Sign in to ImmichFrame Admin</h1>
				<p class="mt-3 text-sm text-stone-300">
					Use one of the admin usernames defined in your environment file.
				</p>
			</div>

			<form
				class="flex flex-col gap-4"
				onsubmit={(event) => {
					event.preventDefault();
					void submitLogin();
				}}
			>
				<label class="flex flex-col gap-2">
					<span class="text-sm font-medium text-stone-200">Username</span>
					<input
						class="rounded-2xl border border-white/12 bg-stone-950/85 px-4 py-3 text-stone-100 outline-none transition placeholder:text-stone-500 focus:border-[color:var(--primary-color)]/75"
						type="text"
						autocomplete="username"
						bind:value={loginUsername}
						placeholder="admin"
					/>
				</label>

				<label class="flex flex-col gap-2">
					<span class="text-sm font-medium text-stone-200">Password</span>
					<input
						class="rounded-2xl border border-white/12 bg-stone-950/85 px-4 py-3 text-stone-100 outline-none transition placeholder:text-stone-500 focus:border-[color:var(--primary-color)]/75"
						type="password"
						autocomplete="current-password"
						bind:value={loginPassword}
						placeholder="Password"
					/>
				</label>

				{#if authErrorMessage}
					<div class="rounded-2xl border border-rose-400/25 bg-rose-400/10 px-4 py-3 text-sm text-rose-100">
						{authErrorMessage}
					</div>
				{/if}

				<button
					class="rounded-full border border-[color:var(--primary-color)]/40 bg-[color:var(--primary-color)]/15 px-5 py-3 text-sm font-medium text-[color:var(--primary-color)] transition hover:bg-[color:var(--primary-color)]/25 disabled:cursor-wait disabled:opacity-50"
					type="submit"
					disabled={loginPending || loginUsername.trim().length === 0 || loginPassword.length === 0}
				>
					{loginPending ? 'Signing In...' : 'Sign In'}
				</button>
			</form>
		</div>
	</section>
{:else}
	<section class="min-h-dvh bg-[radial-gradient(circle_at_top,_rgba(245,222,179,0.16),_transparent_34%),linear-gradient(180deg,#14130f_0%,#0f100d_48%,#0b0c09_100%)] text-stone-100">
		<div class="mx-auto flex max-w-7xl flex-col gap-8 px-4 py-6 sm:px-6 lg:px-10">
			<header class="rounded-[2rem] border border-white/10 bg-white/5 px-5 py-6 shadow-2xl backdrop-blur sm:px-6">
				<div class="flex flex-col gap-5">
					<div class="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
						<div class="min-w-0">
							<p class="text-xs uppercase tracking-[0.45em] text-[color:var(--primary-color)]">
								Live Control
							</p>
							<h1 class="mt-3 text-3xl font-semibold tracking-tight sm:text-4xl">
								ImmichFrame Sessions
							</h1>
							<p class="mt-3 max-w-2xl text-sm text-stone-300">
								Monitor and control active ImmichFrame sessions. Inspect currently displayed media
								or review any recently displayed media.
							</p>
						</div>

						<div class="flex flex-wrap items-center gap-3">
							<span class="rounded-full border border-white/12 bg-black/25 px-4 py-2 text-sm text-stone-300">
								Signed in as {adminSession.username}
							</span>
							<button
								class="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/5 px-4 py-2 text-sm text-stone-100 transition hover:bg-white/10"
								onclick={() => void handleLogout()}
							>
								<Icon path={mdiLogout} title="Log out" size="1rem" />
								Log Out
							</button>
						</div>
					</div>
				</div>
			</header>

			{#if errorMessage}
				<div class="rounded-[2rem] border border-amber-300/20 bg-amber-400/10 px-5 py-4 text-sm text-amber-100">
					{errorMessage}
				</div>
			{/if}

			{#if loading}
				<div class="rounded-[2rem] border border-white/10 bg-white/5 px-6 py-12 text-center text-stone-300">
					Loading active sessions...
				</div>
			{:else if sessions.length === 0}
				<div class="rounded-[2rem] border border-dashed border-white/15 bg-black/20 px-6 py-14 text-center">
					<p class="text-xs uppercase tracking-[0.4em] text-stone-400">No Active Sessions</p>
					<h2 class="mt-4 text-3xl font-semibold">No frames are currently connected.</h2>
					<p class="mt-3 text-sm text-stone-400">
						Open the frame UI on a client device and it will appear here once it starts heartbeating.
					</p>
				</div>
			{:else}
				<div class="grid gap-6 xl:grid-cols-2">
					{#each sessions as session (session.clientIdentifier)}
						<div class="overflow-hidden rounded-[2rem] border border-white/10 bg-black/30 shadow-2xl backdrop-blur">
							<div class="border-b border-white/10 px-5 py-5 sm:px-6">
								<div class="flex flex-col gap-4">
									<div class="flex min-w-0 flex-wrap items-center gap-3">
										{#if editingSessionId === session.clientIdentifier}
											<div class="inline-flex min-w-0 max-w-full items-center gap-2">
												<input
													class="min-w-0 rounded-full border border-[color:var(--primary-color)]/45 bg-stone-950/90 px-4 py-2 text-lg font-semibold text-stone-100 outline-none transition placeholder:text-stone-400 focus:border-[color:var(--primary-color)]/75 focus:bg-stone-950"
													style:width={getDisplayNameInputWidth(session.clientIdentifier)}
													value={displayNameDrafts[session.clientIdentifier] ?? ''}
													placeholder="Name this frame"
													oninput={(event) => {
														const target = event.currentTarget as HTMLInputElement;
														displayNameDrafts = {
															...displayNameDrafts,
															[session.clientIdentifier]: target.value
														};
													}}
													onkeydown={(event) => {
														if (event.key === 'Enter') {
															event.preventDefault();
															void saveDisplayName(session.clientIdentifier);
														}
														if (event.key === 'Escape') {
															event.preventDefault();
															cancelEditingName(session);
														}
													}}
													onblur={(event) => handleEditBlur(session, event)}
												/>
												<button
													class="shrink-0 rounded-full border border-[color:var(--primary-color)]/40 bg-[color:var(--primary-color)]/15 p-2 text-[color:var(--primary-color)] transition hover:bg-[color:var(--primary-color)]/25 disabled:opacity-50"
													data-save-name-for={session.clientIdentifier}
													disabled={savingDisplayName[session.clientIdentifier] === true}
													onclick={() => saveDisplayName(session.clientIdentifier)}
												>
													<Icon path={mdiCheck} title="Save frame name" size="1.1rem" />
												</button>
											</div>
										{:else}
											<div class="inline-flex min-w-0 max-w-full items-center gap-2">
												<h2 class="min-w-0 truncate text-2xl font-semibold">
													{getDisplayName(session)}
												</h2>
												<button
													class="shrink-0 rounded-full border border-white/15 bg-white/5 p-2 text-stone-200 transition hover:bg-white/10"
													onclick={() => beginEditingName(session)}
												>
													<Icon path={mdiPencilOutline} title="Edit frame name" size="1rem" />
												</button>
											</div>
										{/if}

										<span
											class="rounded-full border border-emerald-400/30 bg-emerald-400/15 px-3 py-1 text-xs uppercase tracking-[0.25em] text-emerald-300"
										>
											{session.playbackState}
										</span>
									</div>

									<div class="min-w-0 space-y-2">
										<p class="text-sm text-stone-300">{parseUserAgent(session.userAgent).summary}</p>
										<p class="break-all font-mono text-xs text-stone-500">
											ID: {session.clientIdentifier}
										</p>
										<p class="text-sm text-stone-400">
											Connected {formatDateTime(session.connectedAtUtc)}
										</p>
										{#if session.userAgent}
											<p class="break-words text-xs text-stone-500">
												{parseUserAgent(session.userAgent).details}
											</p>
										{/if}
									</div>

									<div class="flex flex-wrap gap-2">
										<button
											class="rounded-full border border-white/15 bg-white/5 px-4 py-2 text-sm transition hover:bg-white/10 disabled:cursor-wait disabled:opacity-50"
											disabled={actionState[session.clientIdentifier] != null}
											onclick={() => sendCommand(session.clientIdentifier, 'Previous')}
										>
											Previous
										</button>
										<button
											class="rounded-full border border-[color:var(--primary-color)]/40 bg-[color:var(--primary-color)]/15 px-4 py-2 text-sm text-[color:var(--primary-color)] transition hover:bg-[color:var(--primary-color)]/25 disabled:cursor-wait disabled:opacity-50"
											disabled={actionState[session.clientIdentifier] != null}
											onclick={() => sendCommand(session.clientIdentifier, getActionCommand(session))}
										>
											{getActionLabel(session)}
										</button>
										<button
											class="rounded-full border border-white/15 bg-white/5 px-4 py-2 text-sm transition hover:bg-white/10 disabled:cursor-wait disabled:opacity-50"
											disabled={actionState[session.clientIdentifier] != null}
											onclick={() => sendCommand(session.clientIdentifier, 'Next')}
										>
											Next
										</button>
										<button
											class="rounded-full border border-white/15 bg-white/5 px-4 py-2 text-sm transition hover:bg-white/10 disabled:cursor-wait disabled:opacity-50"
											disabled={actionState[session.clientIdentifier] != null}
											onclick={() => sendCommand(session.clientIdentifier, 'Refresh')}
										>
											Refresh
										</button>
										<button
											class="rounded-full border border-rose-400/35 bg-rose-400/15 px-4 py-2 text-sm text-rose-200 transition hover:bg-rose-400/25 disabled:cursor-wait disabled:opacity-50"
											disabled={actionState[session.clientIdentifier] != null}
											onclick={() => sendCommand(session.clientIdentifier, 'Shutdown')}
										>
											Shutdown
										</button>
									</div>
								</div>
							</div>

							<div class="flex flex-col gap-4 px-5 py-5 sm:px-6">
								<section class="min-w-0 rounded-[1.5rem] border border-white/10 bg-white/[0.03] p-5">
									<div class="flex min-w-0 flex-col items-start gap-1 sm:flex-row sm:items-center sm:justify-between sm:gap-3">
										<h3 class="min-w-0 text-lg font-semibold">Current Media</h3>
										{#if session.currentDisplay}
											<span class="max-w-full text-xs uppercase tracking-[0.3em] text-stone-400 sm:shrink-0">
												{formatDateTime(session.currentDisplay.displayedAtUtc)}
											</span>
										{/if}
									</div>

									{#if getCurrentAssets(session).length === 0}
										<p class="mt-4 text-sm text-stone-400">No media currently reported.</p>
									{:else}
										<div class="mt-4 space-y-3">
											{#each getCurrentAssets(session) as asset}
												{@const assetUrl = getAssetUrl(asset)}
												{#if assetUrl}
													<a
														class="block min-w-0 rounded-2xl border border-white/8 transition-[background-image,border-color] duration-200 ease-linear hover:border-[color:var(--primary-color)]/55"
														style={getCurrentMediaCardStyle(session)}
														href={assetUrl}
														target="_blank"
														rel="noreferrer"
													>
														<div class="px-4 py-3">
															<div class="flex min-w-0 flex-col items-start gap-2 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
																<p class="min-w-0 break-words font-medium text-stone-100">
																	{asset.originalFileName}
																</p>
																<span class="shrink-0 text-xs uppercase tracking-[0.2em] text-stone-300">
																	{formatAssetType(asset.type)}
																</span>
															</div>
															{#if asset.localDateTime}
																<p class="mt-1 text-sm text-stone-300">
																	Taken {formatDateTime(asset.localDateTime)}
																</p>
															{/if}
															{#if asset.description}
																<p class="mt-2 break-words text-sm text-stone-200">{asset.description}</p>
															{/if}
														</div>
													</a>
												{:else}
													<div
														class="min-w-0 rounded-2xl border border-white/8 transition-[background-image] duration-200 ease-linear"
														style={getCurrentMediaCardStyle(session)}
													>
														<div class="px-4 py-3">
															<div class="flex min-w-0 flex-col items-start gap-2 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
																<p class="min-w-0 break-words font-medium text-stone-100">
																	{asset.originalFileName}
																</p>
																<span class="shrink-0 text-xs uppercase tracking-[0.2em] text-stone-300">
																	{formatAssetType(asset.type)}
																</span>
															</div>
															{#if asset.localDateTime}
																<p class="mt-1 text-sm text-stone-300">
																	Taken {formatDateTime(asset.localDateTime)}
																</p>
															{/if}
															{#if asset.description}
																<p class="mt-2 break-words text-sm text-stone-200">{asset.description}</p>
															{/if}
														</div>
													</div>
												{/if}
											{/each}
										</div>
									{/if}
								</section>

								<section class="min-w-0 rounded-[1.5rem] border border-white/10 bg-white/[0.03] p-5">
									<div class="flex items-center justify-between gap-3">
										<h3 class="text-lg font-semibold">Recent History</h3>
										<span class="shrink-0 text-xs uppercase tracking-[0.3em] text-stone-400">
											{getHistory(session).length} events
										</span>
									</div>

									{#if getHistory(session).length === 0}
										<p class="mt-4 text-sm text-stone-400">No previous media has been reported yet.</p>
									{:else}
										<div
											class="mt-4 space-y-3"
											class:max-h-[18rem]={getHistory(session).length > 3}
											class:overflow-y-auto={getHistory(session).length > 3}
											class:pr-1={getHistory(session).length > 3}
										>
											{#each getHistory(session) as event, index (event.displayedAtUtc + index)}
												<div class="min-w-0 rounded-2xl border border-white/8 bg-black/25 px-4 py-3">
													<div class="flex items-center justify-between gap-3">
														<p class="min-w-0 truncate text-xs uppercase tracking-[0.25em] text-stone-400">
															{formatDateTime(event.displayedAtUtc)}
														</p>
														<span class="shrink-0 text-xs text-stone-500">
															{event.assets.length} item{event.assets.length === 1 ? '' : 's'}
														</span>
													</div>
													<div class="mt-3 flex flex-wrap gap-2">
														{#each event.assets as asset}
															{@const assetUrl = getAssetUrl(asset)}
															{#if assetUrl}
																<a
																	class="max-w-full break-all rounded-full border border-white/10 bg-white/5 px-3 py-1 text-sm text-stone-200 transition hover:border-[color:var(--primary-color)]/50 hover:bg-white/10"
																	href={assetUrl}
																	target="_blank"
																	rel="noreferrer"
																>
																	{asset.originalFileName}
																</a>
															{:else}
																<span
																	class="max-w-full break-all rounded-full border border-white/10 bg-white/5 px-3 py-1 text-sm text-stone-200"
																>
																	{asset.originalFileName}
																</span>
															{/if}
														{/each}
													</div>
												</div>
											{/each}
										</div>
									{/if}
								</section>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>
	</section>
{/if}
