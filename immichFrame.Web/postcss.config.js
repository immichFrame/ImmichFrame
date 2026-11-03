import tailwindcss from '@tailwindcss/postcss';
import unwrapLayers from './postcss-unwrap-layers.js';

export default {
	plugins: [tailwindcss(), unwrapLayers()]
};
