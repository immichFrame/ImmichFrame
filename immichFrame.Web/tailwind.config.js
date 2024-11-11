/** @type {import('tailwindcss').Config} */
export default {
	mode: 'jit',
	content: ['./src/**/*.{html,js,svelte,ts}'],
	theme: {
		extend: {
			colors: {
				primary: 'var(--primary-color)',
				secondary: 'var(--secondary-color)'
			}
		}
	},
	plugins: []
};
