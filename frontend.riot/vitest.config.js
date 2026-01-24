import { defineConfig } from 'vitest/config'
import riot from 'rollup-plugin-riot'

export default defineConfig({
  plugins: [riot()],
  test: {
    environment: 'jsdom'
  },
})
