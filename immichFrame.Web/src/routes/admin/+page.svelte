<script lang="ts">
	import { onMount } from 'svelte';
	import * as api from '$lib/index';
	import { authSecretStore } from '$lib/stores/persist.store';

	type AccountOverrideDto = {
		showMemories: boolean;
		showFavorites: boolean;
		showArchived: boolean;
		imagesFromDays: number;
		imagesFromDate?: string | null;
		imagesUntilDate?: string | null;
		albums: string[];
		excludedAlbums: string[];
		people: string[];
		rating: number;
	};

	const empty: AccountOverrideDto = {
		showMemories: true,
		showFavorites: true,
		showArchived: false,
		imagesFromDays: 0,
		imagesFromDate: null,
		imagesUntilDate: null,
		albums: [],
		excludedAlbums: [],
		people: [],
		rating: 0
	};

	let loading = $state(true);
	let saving = $state(false);
	let error = $state<string | null>(null);
	let ok = $state<string | null>(null);

	let overrides: AccountOverrideDto = $state(structuredClone(empty));

	let albumsText = $state('');
	let excludedAlbumsText = $state('');
	let peopleText = $state('');

	function parseGuidList(text: string): string[] {
		return text
			.split(/[\n,]+/g)
			.map((x) => x.trim())
			.filter(Boolean);
	}

	function fmtGuidList(list: string[] | undefined | null): string {
		return (list ?? []).join('\n');
	}

	function normalizeOptionalText(value: string | null | undefined): string | null {
		const v = (value ?? '').trim();
		return v.length === 0 ? null : v;
	}

	async function loadOverrides() {
		loading = true;
		error = null;
		ok = null;
		try {
			api.init();
			const res = await fetch('/api/Config/account-overrides', {
				headers: { ...(api.defaults.headers ?? {}) }
			});
			if (!res.ok) {
				error = await res.text();
				return;
			}
			const data = (await res.json()) as AccountOverrideDto | null;
			overrides = structuredClone(data ?? empty);
			albumsText = fmtGuidList(overrides.albums);
			excludedAlbumsText = fmtGuidList(overrides.excludedAlbums);
			peopleText = fmtGuidList(overrides.people);
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			loading = false;
		}
	}

	async function save() {
		saving = true;
		error = null;
		ok = null;
		try {
			api.init();
			const payload: AccountOverrideDto = {
				...overrides,
				imagesFromDate: normalizeOptionalText(overrides.imagesFromDate),
				imagesUntilDate: normalizeOptionalText(overrides.imagesUntilDate),
				albums: parseGuidList(albumsText),
				excludedAlbums: parseGuidList(excludedAlbumsText),
				people: parseGuidList(peopleText)
			};

			const res = await fetch('/api/Config/account-overrides', {
				method: 'PUT',
				headers: {
					'Content-Type': 'application/json',
					...(api.defaults.headers ?? {})
				},
				body: JSON.stringify(payload)
			});

			if (!res.ok) {
				error = await res.text();
				return;
			}

			ok = 'Saved. Running clients should refresh automatically.';
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			saving = false;
		}
	}

	onMount(loadOverrides);
</script>

<svelte:head>
	<title>ImmichFrame Admin</title>
</svelte:head>

<main style="max-width: 900px; margin: 0 auto; padding: 16px; font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;">
	<h1>Admin</h1>
	<p style="opacity: 0.8; line-height: 1.4;">
		To authenticate, set your
		<code>authsecret</code> in local storage (or open the main page once with
		<code>?authsecret=...</code>). This is not required if not using authentication in your .env file.
	</p>

	<section style="margin: 12px 0; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
		<label style="display: block; font-weight: 600; margin-bottom: 6px;">Current authsecret</label>
		<input
			readonly
			value={$authSecretStore}
			style="width: 100%; padding: 8px; border: 1px solid #ccc; border-radius: 6px; background: #f7f7f7;"
		/>
	</section>

	{#if loading}
		<p>Loading…</p>
	{:else}
		{#if error}
			<p style="color: #b00020; white-space: pre-wrap;">{error}</p>
		{/if}
		{#if ok}
			<p style="color: #0b6b0b; white-space: pre-wrap;">{ok}</p>
		{/if}

		<form on:submit|preventDefault={save} style="display: grid; gap: 14px;">
			<fieldset style="border: 1px solid #ddd; border-radius: 8px; padding: 12px;">
				<legend style="padding: 0 6px;">Filters</legend>

				<label style="display: block; margin: 8px 0;">
					<input type="checkbox" bind:checked={overrides.showMemories} />
					Show memories
				</label>

				<label style="display: block; margin: 8px 0;">
					<input type="checkbox" bind:checked={overrides.showFavorites} />
					Show favorites
				</label>

				<label style="display: block; margin: 8px 0;">
					<input type="checkbox" bind:checked={overrides.showArchived} />
					Show archived
				</label>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Images from last N days (0 = disabled)</span>
						<input
							type="number"
							min="0"
							step="1"
							bind:value={overrides.imagesFromDays}
							style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;"
						/>
					</label>

					<label style="display: grid; gap: 6px;">
						<span>Minimum rating (0–5)</span>
						<input
							type="number"
							min="0"
							max="5"
							step="1"
							bind:value={overrides.rating}
							style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;"
						/>
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Images from date (YYYY-MM-DD, optional)</span>
						<input
							type="text"
							placeholder="2025-01-01"
							bind:value={overrides.imagesFromDate}
							style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;"
						/>
					</label>

					<label style="display: grid; gap: 6px;">
						<span>Images until date (YYYY-MM-DD, optional)</span>
						<input
							type="text"
							placeholder="2025-12-31"
							bind:value={overrides.imagesUntilDate}
							style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;"
						/>
					</label>
				</div>
			</fieldset>

			<fieldset style="border: 1px solid #ddd; border-radius: 8px; padding: 12px;">
				<legend style="padding: 0 6px;">Lists (one GUID per line)</legend>

				<label style="display: grid; gap: 6px;">
					<span>Albums (include)</span>
					<textarea
						rows="6"
						bind:value={albumsText}
						style="padding: 8px; border: 1px solid #ccc; border-radius: 6px; font-family: ui-monospace, SFMono-Regular, Menlo, monospace;"
					/>
				</label>

				<label style="display: grid; gap: 6px;">
					<span>Excluded albums</span>
					<textarea
						rows="6"
						bind:value={excludedAlbumsText}
						style="padding: 8px; border: 1px solid #ccc; border-radius: 6px; font-family: ui-monospace, SFMono-Regular, Menlo, monospace;"
					/>
				</label>

				<label style="display: grid; gap: 6px;">
					<span>People</span>
					<textarea
						rows="6"
						bind:value={peopleText}
						style="padding: 8px; border: 1px solid #ccc; border-radius: 6px; font-family: ui-monospace, SFMono-Regular, Menlo, monospace;"
					/>
				</label>
			</fieldset>

			<button
				type="submit"
				disabled={saving}
				style="padding: 10px 14px; border: 1px solid #333; border-radius: 8px; background: #111; color: #fff; cursor: pointer;"
			>
				{saving ? 'Saving…' : 'Save'}
			</button>
		</form>
	{/if}
</main>


