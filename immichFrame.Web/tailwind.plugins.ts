// tailwind.plugins.js
import plugin from 'tailwindcss/plugin';

export default plugin(function ({ addUtilities }) {
    addUtilities({
        '.h-dvh-safe': {
            height: 'calc(var(--vh) * 100)'
        },
        '.min-h-dvh-safe': {
            'min-height': 'calc(var(--vh) * 100)'
        },
        '.max-h-dvh-safe': {
            'max-height': 'calc(var(--vh) * 100)'
        }
    });
});
