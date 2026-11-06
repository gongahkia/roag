import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { PNG } from 'pngjs';
import { gunzipSync } from 'node:zlib';
import { generate, exportAssets, webRoot, repoRoot, hash } from '../scripts/export-assets.mjs';

test('export is byte deterministic, exact original crops survive the documented background transform, gutters are empty', () => {
  const a = generate(); const b = generate();
  assert.deepEqual(a, b);
  const m = JSON.parse(a['manifest.json']); const sheet = PNG.sync.read(a['colony.png']);
  assert.equal(Object.keys(m.sprites).length, 43);
  assert.equal(hash(a['colony.png']), m.imageSha256);
  for (const sprite of Object.values(m.sprites)) {
    const source = PNG.sync.read(readFileSync(resolve(repoRoot, 'assets', sprite.source)));
    const [sx, sy, w, h] = sprite.rect; const [dx, dy] = sprite.frame;
    let foreground = 0;
    for (let y = -2; y < h + 2; y++) for (let x = -2; x < w + 2; x++) {
      const pixel = Array.from(sheet.data.subarray(((dy + y) * sheet.width + dx + x) * 4, ((dy + y) * sheet.width + dx + x) * 4 + 4));
      if (x < 0 || y < 0 || x >= w || y >= h) { assert.deepEqual(pixel, [0,0,0,0]); continue; }
      const offset = ((sy + y) * source.width + sx + x) * 4;
      const original = Array.from(source.data.subarray(offset, offset + 4));
      const background = original.slice(0,3).every((c) => c <= 8);
      assert.deepEqual(pixel, background ? [0,0,0,0] : original);
      if (pixel[3]) foreground++;
    }
    assert.ok(foreground > 0, sprite.id);
  }
});
test('freshness check rejects corruption, extra research files, and unknown selections', () => {
  const dir = mkdtempSync(resolve(tmpdir(), 'roag-assets-'));
  try {
    exportAssets({ output: dir }); exportAssets({ output: dir, check: true });
    writeFileSync(resolve(dir, 'colony.png'), 'corrupt');
    assert.throws(() => exportAssets({ output: dir, check: true }), /Stale\/corrupt/);
    exportAssets({ output: dir }); writeFileSync(resolve(dir, 'catalog.json'), '{}');
    assert.throws(() => exportAssets({ output: dir, check: true }), /Unexpected or missing/);
    const input = resolve(dir, 'invalid-selection.json');
    writeFileSync(input, JSON.stringify({version: 1, bindings: { ghost: 'unpreserved-id' }}));
    assert.throws(() => generate(input), /Unknown\/empty source ID/);
  } finally { rmSync(dir, { recursive: true, force: true }); }
});
test('every authored person and structure binds to the compact manifest', async () => {
  const { founders } = await import('../src/content.ts');
  const m = JSON.parse(generate()['manifest.json']);
  for (const object of founders) assert.ok(m.sprites[object.sprite]);
  for (const alias of ['grass', 'grass-alt', 'path', 'stones', 'rock', 'heart', 'oak', 'apple']) assert.ok(m.sprites[alias]);
  const selection = JSON.parse(readFileSync(resolve(webRoot, 'content/asset-selection.json')));
  assert.deepEqual(Object.keys(m.sprites), Object.keys(selection.bindings).sort());
});

test('boss and hostile roles retain exact catalog identities, uncertainty and distinct founder crops', async () => {
  const art = JSON.parse(readFileSync(resolve(webRoot, 'content/boss-art.json')));
  const catalogBytes = readFileSync(resolve(repoRoot, art.catalog.path));
  const records = new Map(JSON.parse(gunzipSync(catalogBytes)).sprites.map(record => [record.id, record]));
  const manifest = JSON.parse(generate()['manifest.json']);
  const { founders } = await import('../src/content.ts');
  const { RAID_KINDS } = await import('../src/simulation/raid-model.ts');
  const founderHashes = new Set(founders.map(person => manifest.sprites[person.sprite].cropRgbaSha256));
  assert.equal(art.version, 1);
  assert.equal(art.catalog.path, 'assets/catalog.json.gz');
  assert.equal(art.catalog.sha256, hash(catalogBytes));
  assert.deepEqual(Object.keys(art.hostiles).sort(), [...RAID_KINDS].sort());
  assert.equal(new Set(Object.values(art.hostiles).map(alias => manifest.sprites[alias].cropRgbaSha256)).size, RAID_KINDS.length);
  for (const alias of ['boss-bellkeeper', 'boss-sluice-maw', 'boss-threadmother', 'feature-resonator', 'feature-valve', 'hazard-web']) assert.ok(art.roles[alias], alias);
  for (const [alias, role] of Object.entries(art.roles)) {
    const original = records.get(role.source.id), sprite = manifest.sprites[alias];
    assert.ok(original && sprite, alias);
    assert.equal(sprite.id, original.id);
    assert.deepEqual(role.source, { id: original.id, pack: original.pack, path: original.source, rect: original.rect, rgbaSha256: original.rgba_sha256, recordName: original.name, identity: original.identity });
    assert.deepEqual(role.artwork, { name: original.asset.name, description: original.asset.description, type: original.asset.type, classificationConfidence: original.asset.classification_confidence, subjects: original.asset.subjects, composition: original.asset.composition, references: original.asset.evidence.references, uncertainties: original.asset.evidence.uncertainties, sourceNotes: original.notes ?? null });
    assert.equal(role.review.animationFrames, 1);
    assert.equal(role.artwork.composition.animation, 'unconfirmed');
    assert.equal(role.review.catalogChanged, false);
    assert.ok(role.authoredRole.identityScope.includes('Local colony game casting'));
    if (role.authoredRole.kind === 'boss' || role.authoredRole.kind === 'hostile') assert.ok(!founderHashes.has(sprite.cropRgbaSha256), alias);
  }
  assert.equal(art.roles['channel-water-h'].review.orientation, 'H');
  assert.equal(art.roles['channel-water-v'].review.orientation, 'V');
});
