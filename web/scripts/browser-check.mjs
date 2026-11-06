import { spawnSync } from 'node:child_process';
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { checkDist } from './check-dist.mjs';
import { webRoot } from './export-assets.mjs';
const run = (script, args, environment = process.env) => {
  const result = spawnSync(process.execPath, [script, ...args], { cwd: webRoot, stdio: 'inherit', env: environment });
  if (result.status !== 0) process.exit(result.status ?? 1);
};
run('scripts/export-assets.mjs', []);
run('node_modules/typescript/bin/tsc', ['--noEmit']);
const evidence = resolve(webRoot, process.env.ROAG_EVIDENCE_DIR ?? '../artifacts/colony-web/P07');
mkdirSync(evidence, { recursive: true });
// Standalone browser checks generate their own ordinary, replay-verified inputs.
// A coordinator may provide the same frozen source's retained scenario directory.
if (!process.env.ROAG_RAID_FIXTURE_DIR) {
  process.env.ROAG_RAID_FIXTURE_DIR = resolve(evidence, 'acceptance-scenarios');
  run('scripts/raid-scenarios.mjs', [], { ...process.env, ROAG_EVIDENCE_DIR: process.env.ROAG_RAID_FIXTURE_DIR });
}
if (!process.env.ROAG_BOSS_FIXTURE_DIR) {
  process.env.ROAG_BOSS_FIXTURE_DIR = resolve(evidence, 'boss-scenarios');
  for (const seed of [43, 44, 45]) run('scripts/boss-scenarios.mjs', [], {
    ...process.env, ROAG_BOSS_SEED: String(seed),
    ROAG_EVIDENCE_DIR: resolve(process.env.ROAG_BOSS_FIXTURE_DIR, `seed-${seed}`),
  });
}
for (const mode of ['root', 'roag']) {
  const directory = resolve(evidence, `build-${mode}`);
  run('node_modules/vite/bin/vite.js', ['build', `--base=${mode === 'root' ? '/' : '/roag/'}`, `--outDir=${directory}`, '--emptyOutDir']);
  const sizes = checkDist(directory, mode === 'root' ? '/' : '/roag/');
  writeFileSync(resolve(evidence, `${mode}-sizes.json`), JSON.stringify(sizes, null, 2) + '\n');
  console.log(`${mode}: ${sizes.totals.raw} bytes raw; ${sizes.totals.gzip} bytes gzip; ${sizes.totals.brotli} bytes Brotli`);
}
run('node_modules/@playwright/test/cli.js', ['test']);
