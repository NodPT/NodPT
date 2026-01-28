import { defineConfig } from 'vite'
import riot from 'rollup-plugin-riot'

// https://vite.dev/config/
export default defineConfig({
  plugins: [riot()],
  build: {
    minify: 'esbuild',
    target: 'esnext'
  }
})
