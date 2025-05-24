/** @type {import('tailwindcss').Config} */

import screenSafePlugin from './tailwind.plugins';

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
        sm: '2px 2px 4px rgba(0, 0, 0, 0.8)', //for small text
        lg: '3px 3px 6px rgba(0, 0, 0, 0.5)', //for large text
      },
    },
  },
  plugins: [
    function ({ addUtilities, theme, e }) {
      const textShadows = theme('textShadow');
      const textShadowUtilities = Object.keys(textShadows).reduce((acc, key) => {
        acc[`.${e(`text-shadow-${key}`)}`] = {
          textShadow: textShadows[key],
        };
        return acc;
      }, {});

      addUtilities(textShadowUtilities);
    },
    screenSafePlugin
  ],
};
