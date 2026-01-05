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

	type GeneralSettingsDto = {
		interval: number;
		transitionDuration: number;
		downloadImages: boolean;
		renewImagesDuration: number;
		imageZoom: boolean;
		imagePan: boolean;
		imageFill: boolean;
		layout: string;
		webcalendars: string[];
		refreshAlbumPeopleInterval: number;
		showClock: boolean;
		clockFormat?: string | null;
		clockDateFormat?: string | null;
		showProgressBar: boolean;
		showPhotoDate: boolean;
		photoDateFormat?: string | null;
		showImageDesc: boolean;
		showPeopleDesc: boolean;
		showAlbumName: boolean;
		showImageLocation: boolean;
		imageLocationFormat?: string | null;
		primaryColor?: string | null;
		secondaryColor?: string | null;
		style: string;
		baseFontSize?: string | null;
		weatherApiKey?: string | null;
		showWeatherDescription: boolean;
		weatherIconUrl?: string | null;
		unitSystem?: string | null;
		weatherLatLong?: string | null;
		language: string;
		webhook?: string | null;
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

	const emptyGeneral: GeneralSettingsDto = {
		interval: 45,
		transitionDuration: 1,
		downloadImages: false,
		renewImagesDuration: 30,
		imageZoom: true,
		imagePan: false,
		imageFill: false,
		layout: 'splitview',
		webcalendars: [],
		refreshAlbumPeopleInterval: 12,
		showClock: true,
		clockFormat: 'hh:mm',
		clockDateFormat: 'eee, MMM d',
		showProgressBar: true,
		showPhotoDate: true,
		photoDateFormat: 'MM/dd/yyyy',
		showImageDesc: true,
		showPeopleDesc: true,
		showAlbumName: true,
		showImageLocation: true,
		imageLocationFormat: 'City,State,Country',
		primaryColor: null,
		secondaryColor: null,
		style: 'none',
		baseFontSize: null,
		weatherApiKey: '',
		showWeatherDescription: true,
		weatherIconUrl: 'https://openweathermap.org/img/wn/{IconId}.png',
		unitSystem: 'imperial',
		weatherLatLong: '40.7128,74.0060',
		language: 'en',
		webhook: null
	};

	let loading = $state(true);
	let saving = $state(false);
	let error = $state<string | null>(null);
	let ok = $state<string | null>(null);

	let overrides: AccountOverrideDto = $state(structuredClone(empty));
	let general: GeneralSettingsDto = $state(structuredClone(emptyGeneral));

	let albumsText = $state('');
	let excludedAlbumsText = $state('');
	let peopleText = $state('');
	let webcalendarsText = $state('');

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

	function parseStringList(text: string): string[] {
		return text
			.split(/[\n,]+/g)
			.map((x) => x.trim())
			.filter(Boolean);
	}

	function fmtStringList(list: string[] | undefined | null): string {
		return (list ?? []).join('\n');
	}

	async function loadAll() {
		loading = true;
		error = null;
		ok = null;
		try {
			api.init();

			const [overridesRes, generalRes] = await Promise.all([
				fetch('/api/Config/account-overrides', { headers: { ...(api.defaults.headers ?? {}) } }),
				fetch('/api/Config/general-settings', { headers: { ...(api.defaults.headers ?? {}) } })
			]);

			if (!overridesRes.ok) {
				error = await overridesRes.text();
				return;
			}
			if (!generalRes.ok) {
				error = await generalRes.text();
				return;
			}

			const overridesData = (await overridesRes.json()) as AccountOverrideDto | null;
			overrides = structuredClone(overridesData ?? empty);
			albumsText = fmtGuidList(overrides.albums);
			excludedAlbumsText = fmtGuidList(overrides.excludedAlbums);
			peopleText = fmtGuidList(overrides.people);

			const generalData = (await generalRes.json()) as GeneralSettingsDto;
			general = structuredClone(generalData ?? emptyGeneral);
			webcalendarsText = fmtStringList(general.webcalendars);
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

			const generalPayload: GeneralSettingsDto = {
				...general,
				layout: (general.layout ?? '').trim(),
				style: (general.style ?? '').trim(),
				language: (general.language ?? '').trim(),
				webcalendars: parseStringList(webcalendarsText),
				clockFormat: normalizeOptionalText(general.clockFormat),
				clockDateFormat: normalizeOptionalText(general.clockDateFormat),
				photoDateFormat: normalizeOptionalText(general.photoDateFormat),
				imageLocationFormat: normalizeOptionalText(general.imageLocationFormat),
				primaryColor: normalizeOptionalText(general.primaryColor),
				secondaryColor: normalizeOptionalText(general.secondaryColor),
				baseFontSize: normalizeOptionalText(general.baseFontSize),
				weatherApiKey: normalizeOptionalText(general.weatherApiKey),
				weatherIconUrl: normalizeOptionalText(general.weatherIconUrl),
				unitSystem: normalizeOptionalText(general.unitSystem),
				weatherLatLong: normalizeOptionalText(general.weatherLatLong),
				webhook: normalizeOptionalText(general.webhook)
			};

			const [overridesPut, generalPut] = await Promise.all([
				fetch('/api/Config/account-overrides', {
					method: 'PUT',
					headers: { 'Content-Type': 'application/json', ...(api.defaults.headers ?? {}) },
					body: JSON.stringify(payload)
				}),
				fetch('/api/Config/general-settings', {
					method: 'PUT',
					headers: { 'Content-Type': 'application/json', ...(api.defaults.headers ?? {}) },
					body: JSON.stringify(generalPayload)
				})
			]);

			if (!overridesPut.ok) {
				error = await overridesPut.text();
				return;
			}
			if (!generalPut.ok) {
				error = await generalPut.text();
				return;
			}

			ok = 'Saved. Running clients should refresh automatically.';
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			saving = false;
		}
	}

	onMount(loadAll);
</script>

<svelte:head>
	<title>ImmichFrame Admin</title>
</svelte:head>

<main style="max-width: 900px; margin: 0 auto; padding: 16px; font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;">
	<h1>Admin Configuration</h1>
  <p style="margin: 12px 0; padding: 10px; background: #f0f7ff; border: 1px solid #b3d9ff; border-radius: 6px;">
    📖 Need help with configuration? See the{' '}
    <a
      href="https://immichframe.dev/docs/getting-started/configuration#filtering-on-albums-or-people"
      target="_blank"
      rel="noreferrer"
      style="color: #0066cc; text-decoration: underline; font-weight: 500;"
    >
      documentation
    </a>.
  </p>
	<p style="opacity: 0.8; line-height: 1.4; margin-bottom: 12px;">
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
				<legend style="padding: 0 6px;">General settings</legend>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px;">
					<label style="display: grid; gap: 6px;">
						<span>Interval (seconds)</span>
						<input type="number" min="1" step="1" bind:value={general.interval} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>

					<label style="display: grid; gap: 6px;">
						<span>Transition duration (seconds)</span>
						<input type="number" min="0" step="0.1" bind:value={general.transitionDuration} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.downloadImages} />
						Download images
					</label>
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.imageZoom} />
						Image zoom
					</label>
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.imagePan} />
						Image pan
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 12px;">
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.imageFill} />
						Image fill
					</label>
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.showClock} />
						Show clock
					</label>
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.showProgressBar} />
						Show progress bar
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Layout</span>
						<input type="text" bind:value={general.layout} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
					<label style="display: grid; gap: 6px;">
						<span>Style</span>
						<input type="text" bind:value={general.style} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Renew images duration (minutes)</span>
						<input type="number" min="0" step="1" bind:value={general.renewImagesDuration} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
					<label style="display: grid; gap: 6px;">
						<span>Language</span>
						<input type="text" bind:value={general.language} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.showPhotoDate} />
						Show photo date
					</label>
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.showAlbumName} />
						Show album name
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Photo date format</span>
						<input type="text" bind:value={general.photoDateFormat} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
					<label style="display: grid; gap: 6px;">
						<span>Base font size (e.g. 17px)</span>
						<input type="text" bind:value={general.baseFontSize} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.showImageDesc} />
						Show image description
					</label>
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.showPeopleDesc} />
						Show people description
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.showImageLocation} />
						Show image location
					</label>
					<label style="display: grid; gap: 6px;">
						<span>Image location format</span>
						<input type="text" bind:value={general.imageLocationFormat} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Primary color</span>
						<input type="text" bind:value={general.primaryColor} placeholder="#F5DEB3" style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
					<label style="display: grid; gap: 6px;">
						<span>Secondary color</span>
						<input type="text" bind:value={general.secondaryColor} placeholder="#000000" style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Clock format</span>
						<input type="text" bind:value={general.clockFormat} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
					<label style="display: grid; gap: 6px;">
						<span>Clock date format</span>
						<input type="text" bind:value={general.clockDateFormat} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Webcalendars (one URL per line)</span>
						<textarea rows="4" bind:value={webcalendarsText} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px; font-family: ui-monospace, SFMono-Regular, Menlo, monospace;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Refresh album/people interval (hours)</span>
						<input type="number" min="0" step="1" bind:value={general.refreshAlbumPeopleInterval} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
					<label style="display: grid; gap: 6px;">
						<span>Webhook URL</span>
						<input type="text" bind:value={general.webhook} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Weather API key</span>
						<input type="text" bind:value={general.weatherApiKey} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
					<label style="display: block; margin: 8px 0;">
						<input type="checkbox" bind:checked={general.showWeatherDescription} />
						Show weather description
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Weather icon URL template</span>
						<input type="text" bind:value={general.weatherIconUrl} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
					<label style="display: grid; gap: 6px;">
						<span>Unit system</span>
						<input type="text" bind:value={general.unitSystem} placeholder="imperial/metric" style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>

				<div style="display: grid; grid-template-columns: 1fr; gap: 12px; margin-top: 10px;">
					<label style="display: grid; gap: 6px;">
						<span>Weather lat/long (e.g. 40.7128,74.0060)</span>
						<input type="text" bind:value={general.weatherLatLong} style="padding: 8px; border: 1px solid #ccc; border-radius: 6px;" />
					</label>
				</div>
			</fieldset>

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


