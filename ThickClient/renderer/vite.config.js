import { defineConfig } from 'vite'
import riot from 'rollup-plugin-riot'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const __dirname = dirname(fileURLToPath(import.meta.url))

// Vite config for the ThickClient renderer.
// The renderer is loaded by Electron either from the dev server (NODPT_DEV=1)
// or from the static `dist/` build output for packaged builds.
export default defineConfig({
  root: __dirname,
  base: './',
  plugins: [riot()],
  server: {
    port: 5173,
    strictPort: true,
  },
  build: {
    outDir: resolve(__dirname, 'dist'),
    emptyOutDir: true,
    minify: 'esbuild',
    target: 'esnext',
  },
})
