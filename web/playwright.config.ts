import { defineConfig } from '@playwright/test';
import { resolve } from 'node:path';

const evidence = resolve(import.meta.dirname, process.env.ROAG_EVIDENCE_DIR ?? '../artifacts/colony-web/P07');
export default defineConfig({
  testDir: './tests/browser',
  outputDir: resolve(evidence, 'browser-results'),
  timeout: 30_000,
  expect: { timeout: 8_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['json', { outputFile: resolve(evidence, 'browser-results.json') }]],
  use: { browserName: 'chromium', viewport: { width: 1440, height: 1000 }, trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  projects: [
    { name: 'root', use: { baseURL: 'http://127.0.0.1:4173/' } },
    { name: 'roag', use: { baseURL: 'http://127.0.0.1:4174/roag/' } },
  ],
  webServer: [
    { command: 'node scripts/serve-production.mjs root', url: 'http://127.0.0.1:4173/', reuseExistingServer: false },
    { command: 'node scripts/serve-production.mjs roag', url: 'http://127.0.0.1:4174/roag/', reuseExistingServer: false },
  ],
});
