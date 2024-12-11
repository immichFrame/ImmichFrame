/** @type {import('tailwindcss').Config} */
export default {
    mode: 'jit',
    content: ['./src/**/*.{html,js,svelte,ts}'],
    theme: {
        extend: {
            colors: {
                primary: 'var(--primary-color)',
                secondary: 'var(--secondary-color)',
            },
            textShadow: {
                custom: '2px 2px 4px rgba(0, 0, 0, 0.5)',
            },
        },
    },
    plugins: [
        function ({ addUtilities }) {
            addUtilities({
                '.text-shadow-custom': {
                    textShadow: '2px 2px 4px rgba(0, 0, 0, 0.5)',
                },
            });
        },
    ],
};
