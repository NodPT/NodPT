import { defineConfig } from 'vitest/config.js';
import riot from 'rollup-plugin-riot';

export default defineConfig({
	plugins: [riot()],
	test: {
		environment: 'jsdom',
	},
});
