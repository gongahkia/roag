import { defineConfig } from 'vite';

export default defineConfig({
  base: process.env.GITHUB_ACTIONS ? '/golf-with-your-enemies/' : '/',
  test: {
    testTimeout: 15_000,
  },
});
