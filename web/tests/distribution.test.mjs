import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { generate } from '../scripts/export-assets.mjs';
import { checkDist } from '../scripts/check-dist.mjs';

test('distribution rejects research files, altered artwork and URLs escaping the configured deployment base', () => {
  const dir = mkdtempSync(resolve(tmpdir(), 'roag-distribution-'));
  try {
    mkdirSync(resolve(dir, 'game-assets')); mkdirSync(resolve(dir, 'assets'));
    for (const [name, bytes] of Object.entries(generate())) writeFileSync(resolve(dir, 'game-assets', name), bytes);
    writeFileSync(resolve(dir, 'assets/app.js'), 'console.log("fixture")');
    writeFileSync(resolve(dir, 'assets/app.css'), 'body{margin:0}');
    const html = resolve(dir, 'index.html');
    writeFileSync(html, '<script src="./assets/app.js"></script>');
    assert.equal(checkDist(dir).files.length, 7);
    writeFileSync(resolve(dir, 'catalog.json'), '{}');
    assert.throws(() => checkDist(dir), /Unexpected distribution file/);
    rmSync(resolve(dir, 'catalog.json'));
    writeFileSync(html, '<script src="/assets/app.js"></script>');
    assert.throws(() => checkDist(dir), /escapes configured base/);
    assert.doesNotThrow(() => checkDist(dir, '/'));
    assert.throws(() => checkDist(dir, '/roag/'), /escapes configured base/);
    writeFileSync(html, '<script src="/roag/assets/app.js"></script>');
    assert.doesNotThrow(() => checkDist(dir, '/roag/'));
    writeFileSync(resolve(dir, 'game-assets/manifest.json'), '{}');
    assert.throws(() => checkDist(dir, '/roag/'), /Unexpected\/changed shipped artwork/);
  } finally { rmSync(dir, { recursive: true, force: true }); }
});
