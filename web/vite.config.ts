import { defineConfig } from 'vite';

export default defineConfig({
  base: './',
  build: {
    target: 'es2022',
    // Phaser is intentionally one cached vendor chunk; report its size separately.
    rolldownOptions: { output: { codeSplitting: { groups: [{ name: 'phaser', test: /node_modules\/phaser/ }] } } },
    chunkSizeWarningLimit: 1600,
  },
});
