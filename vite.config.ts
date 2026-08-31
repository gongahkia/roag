import { defineConfig } from 'vite'

export default defineConfig({
  base: './',
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          const source = id.replace(/\\/g, '/')
          if (!source.includes('/src/')) return undefined
          if (source.includes('/src/autoplay')) return 'autoplay-tools'
          if (source.endsWith('/src/rng.ts')) return 'rng-core'
          if (source.includes('/src/engine/institutions') || source.includes('/src/engine/manifest') || source.includes('/src/engine/route-reckoning')) return 'institutions'
          if (source.includes('/src/engine/destination-')) return 'destination-world'
          if (source.includes('/src/engine/') || source.endsWith('/src/engine.ts') || source.endsWith('/src/world.ts') || source.endsWith('/src/content.ts') || source.endsWith('/src/props.ts') || source.endsWith('/src/objectives.ts') || source.endsWith('/src/ecology.ts')) return 'gameplay-core'
          return undefined
        }
      }
    }
  },
  test: { environment: 'node' }
})
