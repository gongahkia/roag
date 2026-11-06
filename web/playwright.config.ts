import { defineConfig } from '@playwright/test';
import { resolve } from 'node:path';

const evidence = resolve(import.meta.dirname, process.env.ROAG_EVIDENCE_DIR ?? '../artifacts/colony-web/P07');
const port = Number(process.env.ROAG_BROWSER_PORT_BASE ?? 4173);
if (!Number.isInteger(port) || port < 1024 || port > 65534) throw new Error('Invalid ROAG_BROWSER_PORT_BASE');
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
    { name: 'root', use: { baseURL: `http://127.0.0.1:${port}/` } },
    { name: 'roag', use: { baseURL: `http://127.0.0.1:${port + 1}/roag/` } },
  ],
  webServer: [
    { command: 'node scripts/serve-production.mjs root', url: `http://127.0.0.1:${port}/`, reuseExistingServer: false },
    { command: 'node scripts/serve-production.mjs roag', url: `http://127.0.0.1:${port + 1}/roag/`, reuseExistingServer: false },
  ],
});
