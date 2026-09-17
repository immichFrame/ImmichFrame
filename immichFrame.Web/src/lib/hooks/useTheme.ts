import type { ClientSettingsDto } from '$lib/immichFrameApi';

// The configured slideshow palette, backing the `frame-*` color utilities.
// Scoped to the slideshow, so both helpers return a cleanup that restores the
// defaults from app.css — the admin UI keeps the @immich/ui look.
export function applyFrameColors(config: ClientSettingsDto) {
	const root = document.documentElement;

	setVariable(root, '--primary-color', config.primaryColor);
	setVariable(root, '--secondary-color', config.secondaryColor);

	return () => {
		root.style.removeProperty('--primary-color');
		root.style.removeProperty('--secondary-color');
	};
}

// The base font size scales every rem-based size, so it stays scoped as well.
export function applyBaseFontSize(fontSize: string | null | undefined) {
	const root = document.documentElement;

	root.style.fontSize = fontSize ?? '';

	return () => {
		root.style.fontSize = '';
	};
}

function setVariable(root: HTMLElement, name: string, value: string | null | undefined) {
	if (value) {
		root.style.setProperty(name, value);
	} else {
		root.style.removeProperty(name);
	}
}
