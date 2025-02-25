// import adapter from '@sveltejs/adapter-node';
import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	// Consult https://kit.svelte.dev/docs/integrations#preprocessors
	// for more information about preprocessors
	preprocess: vitePreprocess({ script: true }),

	kit: {
		// adapter-auto only supports some environments, see https://kit.svelte.dev/docs/adapter-auto for a list.
		// If your environment is not supported, or you settled on a specific environment, switch out the adapter.
		// See https://kit.svelte.dev/docs/adapters for more information about adapters.
		adapter: adapter({
			// default options are shown
			pages: 'build',
			assets: 'build',
			fallback: 'index.html',
			precompress: true,
			strict: true
		}),
		paths: {
			base: '/frame' // Basispfad auf /frame setzen
		}
	}
};

export default config;
