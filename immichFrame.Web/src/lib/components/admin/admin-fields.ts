import type { GeneralSettings, ServerAccountSettings } from '$lib/immichFrameApi';

export type FieldType =
	| 'text'
	| 'number'
	| 'checkbox'
	| 'password'
	| 'select'
	| 'list'
	| 'date'
	| 'guid-list';

export interface FieldDef<T = GeneralSettings> {
	key: keyof T & string;
	label: string;
	type: FieldType;
	options?: string[];
	placeholder?: string;
	help?: string;
	step?: string;
	min?: number;
	max?: number;
}

// What setting-field.svelte accepts: any concrete FieldDef widens to this,
// since a literal key type is assignable to `string`.
export type AnyFieldDef = FieldDef<Record<string, unknown>>;

export interface SectionDef<T = GeneralSettings> {
	title: string;
	fields: FieldDef<T>[];
}

export const generalSections: SectionDef[] = [
	{
		title: 'Display',
		fields: [
			{ key: 'interval', label: 'Interval (seconds)', type: 'number' },
			{
				key: 'transitionDuration',
				label: 'Transition duration (seconds)',
				type: 'number',
				step: '0.5'
			},
			{ key: 'layout', label: 'Layout', type: 'select', options: ['single', 'splitview'] },
			{
				key: 'style',
				label: 'Style',
				type: 'select',
				options: ['none', 'solid', 'transition', 'blur']
			},
			{ key: 'imageZoom', label: 'Image zoom', type: 'checkbox' },
			{ key: 'imagePan', label: 'Image pan', type: 'checkbox' },
			{ key: 'imageFill', label: 'Image fill', type: 'checkbox' },
			{ key: 'playAudio', label: 'Play audio (videos)', type: 'checkbox' },
			{ key: 'showProgressBar', label: 'Show progress bar', type: 'checkbox' },
			{ key: 'primaryColor', label: 'Primary color', type: 'text', placeholder: '#f5deb3' },
			{ key: 'secondaryColor', label: 'Secondary color', type: 'text', placeholder: '#0f0f0f' },
			{ key: 'baseFontSize', label: 'Base font size', type: 'text', placeholder: '17px' },
			{ key: 'language', label: 'Language', type: 'text', placeholder: 'en' }
		]
	},
	{
		title: 'Clock & Metadata',
		fields: [
			{ key: 'showClock', label: 'Show clock', type: 'checkbox' },
			{ key: 'clockFormat', label: 'Clock format', type: 'text', placeholder: 'hh:mm' },
			{
				key: 'clockDateFormat',
				label: 'Clock date format',
				type: 'text',
				placeholder: 'eee, MMM d'
			},
			{ key: 'showPhotoDate', label: 'Show photo date', type: 'checkbox' },
			{
				key: 'photoDateFormat',
				label: 'Photo date format',
				type: 'text',
				placeholder: 'MM/dd/yyyy'
			},
			{ key: 'showImageDesc', label: 'Show image description', type: 'checkbox' },
			{ key: 'showPeopleDesc', label: 'Show people', type: 'checkbox' },
			{ key: 'showTagsDesc', label: 'Show tags', type: 'checkbox' },
			{ key: 'showAlbumName', label: 'Show album name', type: 'checkbox' },
			{ key: 'showImageLocation', label: 'Show image location', type: 'checkbox' },
			{
				key: 'imageLocationFormat',
				label: 'Image location format',
				type: 'text',
				placeholder: 'City,State,Country'
			}
		]
	},
	{
		title: 'Weather & Calendar',
		fields: [
			{
				key: 'weatherApiKey',
				label: 'OpenWeatherMap API key',
				type: 'password',
				help: 'Leave empty to disable the weather overlay.'
			},
			{
				key: 'weatherLatLong',
				label: 'Weather latitude,longitude',
				type: 'text',
				placeholder: '40.7128,74.0060'
			},
			{ key: 'unitSystem', label: 'Unit system', type: 'select', options: ['imperial', 'metric'] },
			{ key: 'showWeatherDescription', label: 'Show weather description', type: 'checkbox' },
			{
				key: 'weatherIconUrl',
				label: 'Weather icon URL',
				type: 'text',
				placeholder: 'https://openweathermap.org/img/wn/{IconId}.png'
			},
			{
				key: 'webcalendars',
				label: 'Web calendars (one .ics URL per line)',
				type: 'list',
				help: 'Changes are picked up within ~15 minutes.'
			}
		]
	},
	{
		title: 'Server',
		fields: [
			{ key: 'downloadImages', label: 'Download images (cache on disk)', type: 'checkbox' },
			{ key: 'renewImagesDuration', label: 'Renew cached images after (days)', type: 'number' },
			{
				key: 'refreshAlbumPeopleInterval',
				label: 'Refresh albums/people interval (hours)',
				type: 'number'
			},
			{ key: 'webhook', label: 'Webhook URL', type: 'text' },
			{
				key: 'authenticationSecret',
				label: 'Client authentication secret',
				type: 'password',
				help: 'When set, slideshow clients must authenticate with this secret.'
			},
			{
				key: 'adminPassword',
				label: 'Admin password',
				type: 'password',
				help: 'Password for this admin UI. Careful: saving a wrong value locks you out unless the IMMICHFRAME_ADMIN_PASSWORD environment variable is set (it always wins).'
			}
		]
	}
];

export const accountFields: FieldDef<ServerAccountSettings>[] = [
	{
		key: 'immichServerUrl',
		label: 'Immich server URL',
		type: 'text',
		placeholder: 'http://immich:2283'
	},
	{ key: 'apiKey', label: 'API key', type: 'password' },
	{
		key: 'apiKeyFile',
		label: 'API key file',
		type: 'text',
		placeholder: '/run/secrets/immich-api-key',
		help: 'Alternative to the API key.'
	},
	{ key: 'showMemories', label: 'Memories', type: 'checkbox' },
	{ key: 'showFavorites', label: 'Favorites', type: 'checkbox' },
	{ key: 'showArchived', label: 'Archived', type: 'checkbox' },
	{ key: 'showVideos', label: 'Videos', type: 'checkbox' },
	{ key: 'imagesFromDays', label: 'Images from (days back)', type: 'number' },
	{ key: 'rating', label: 'Minimum rating', type: 'number', min: 1, max: 5 },
	{ key: 'imagesFromDate', label: 'Images from date', type: 'date' },
	{ key: 'imagesUntilDate', label: 'Images until date', type: 'date' },
	{ key: 'albums', label: 'Albums', type: 'guid-list', help: 'One ID per line.' },
	{ key: 'excludedAlbums', label: 'Excluded albums', type: 'guid-list', help: 'One ID per line.' },
	{ key: 'people', label: 'People', type: 'guid-list', help: 'One ID per line.' },
	{ key: 'tags', label: 'Tags', type: 'list', help: 'One tag per line.' }
];
