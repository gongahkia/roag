import { spawnSync } from 'node:child_process';
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { checkDist } from './check-dist.mjs';
import { webRoot } from './export-assets.mjs';
const run = (script, args) => {
  const result = spawnSync(process.execPath, [script, ...args], { cwd: webRoot, stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
};
run('scripts/export-assets.mjs', []);
run('node_modules/typescript/bin/tsc', ['--noEmit']);
const evidence = resolve(webRoot, process.env.ROAG_EVIDENCE_DIR ?? '../artifacts/colony-web/P04');
mkdirSync(evidence, { recursive: true });
for (const mode of ['root', 'roag']) {
  const directory = resolve(evidence, `build-${mode}`);
  run('node_modules/vite/bin/vite.js', ['build', `--base=${mode === 'root' ? '/' : '/roag/'}`, `--outDir=${directory}`, '--emptyOutDir']);
  const sizes = checkDist(directory, mode === 'root' ? '/' : '/roag/');
  writeFileSync(resolve(evidence, `${mode}-sizes.json`), JSON.stringify(sizes, null, 2) + '\n');
  console.log(`${mode}: ${sizes.totals.raw} bytes raw; ${sizes.totals.gzip} bytes gzip; ${sizes.totals.brotli} bytes Brotli`);
}
run('node_modules/@playwright/test/cli.js', ['test']);
