// Build-time only. Original artwork and the canonical catalog are read-only.
import { readFileSync, writeFileSync, mkdirSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve, relative } from 'node:path';
import { gunzipSync } from 'node:zlib';
import { createHash } from 'node:crypto';
import { PNG } from 'pngjs';

export const webRoot = fileURLToPath(new URL('../', import.meta.url));
export const repoRoot = resolve(webRoot, '..');
export const hash = (bytes) => createHash('sha256').update(bytes).digest('hex');
const json = (value) => Buffer.from(JSON.stringify(value) + '\n');

export function generate(selectionPath = resolve(webRoot, 'content/asset-selection.json')) {
  const catalogBytes = readFileSync(resolve(repoRoot, 'assets/catalog.json.gz'));
  const catalog = JSON.parse(gunzipSync(catalogBytes));
  const selection = JSON.parse(readFileSync(selectionPath));
  if (selection.version !== 1 || !selection.bindings || Object.keys(selection.bindings).length === 0) {
    throw new Error('Expected a nonempty version 1 asset selection');
  }
  const records = new Map(catalog.sprites.map((record) => [record.id, record]));
  const aliases = Object.keys(selection.bindings).sort();
  const sources = new Map();
  const sprites = {};
  const cell = 20; // 16px maximum crop + two transparent gutter pixels on every edge.
  const columns = 8;
  const width = columns * cell;
  const height = Math.ceil(aliases.length / columns) * cell;
  const sheet = new PNG({ width, height, colorType: 6 });
  const frames = {};
  aliases.forEach((alias, index) => {
    if (!/^[a-z][a-z0-9-]*$/.test(alias)) throw new Error(`Invalid alias: ${alias}`);
    const id = selection.bindings[alias];
    const record = records.get(id);
    if (!record || record.kind === 'empty' || !record.foreground_pixels) throw new Error(`Unknown/empty source ID: ${id}`);
    const sourcePath = resolve(repoRoot, 'assets', record.source);
    if (relative(resolve(repoRoot, 'assets'), sourcePath).startsWith('..')) throw new Error('Source escapes assets');
    if (!sources.has(record.source)) {
      const bytes = readFileSync(sourcePath);
      sources.set(record.source, { image: PNG.sync.read(bytes), sha256: hash(bytes) });
    }
    const { image, sha256 } = sources.get(record.source);
    const [x, y, w, h] = record.rect;
    if (![x, y, w, h].every(Number.isInteger) || x < 0 || y < 0 || w < 1 || h < 1 || w > 16 || h > 16 || x + w > image.width || y + h > image.height) {
      throw new Error(`Invalid crop for ${id}: ${record.rect}`);
    }
    const crop = Buffer.alloc(w * h * 4);
    for (let row = 0; row < h; row++) {
      image.data.copy(crop, row * w * 4, ((y + row) * image.width + x) * 4, ((y + row) * image.width + x + w) * 4);
    }
    if (hash(crop) !== record.rgba_sha256) throw new Error(`Catalog pixel hash mismatch: ${id}`);
    const dx = (index % columns) * cell + 2;
    const dy = Math.floor(index / columns) * cell + 2;
    for (let row = 0; row < h; row++) {
      for (let col = 0; col < w; col++) {
        const offset = (row * w + col) * 4;
        // Same documented foreground rule as the preserved catalog; no recoloring.
        const isBackground = crop[offset] <= 8 && crop[offset + 1] <= 8 && crop[offset + 2] <= 8;
        if (!isBackground) crop.copy(sheet.data, ((dy + row) * width + dx + col) * 4, offset, offset + 4);
      }
    }
    frames[alias] = { frame: { x: dx, y: dy, w, h }, rotated: false, trimmed: false,
      spriteSourceSize: { x: 0, y: 0, w, h }, sourceSize: { w, h } };
    sprites[alias] = { id, source: record.source, rect: record.rect, sourceSha256: sha256,
      cropRgbaSha256: record.rgba_sha256, originalName: record.name,
      identityConfidence: record.identity?.confidence ?? 'unconfirmed', frame: [dx, dy, w, h] };
  });
  const png = PNG.sync.write(sheet, { colorType: 6, filterType: 0, deflateLevel: 9 });
  const manifest = { version: 1, catalog: 'assets/catalog.json.gz', catalogSha256: hash(catalogBytes),
    transform: 'RGB channels all <=8 become transparent in derived output only; other RGBA unchanged. No inferred animation or collision.',
    image: 'colony.png', imageSha256: hash(png), imageSize: [width, height], gutter: 2, sprites };
  const atlas = { frames, meta: { image: 'colony.png', format: 'RGBA8888', size: { w: width, h: height }, scale: '1' } };
  const credits = 'Roag colony — shipped artwork\n\nUrizen 1Bit Tileset v2.0 — vurmux — CC0\nhttps://vurmux.itch.io/urizen-onebit-tileset\n\nGodot node icons — pixel-boy — CC0\nhttps://pixel-boy.itch.io/icon-godot-node\n\nSelected original crops only. No font files shipped; UI uses system fonts.\nColony roles are local assignments, not catalog identity claims.\n';
  return { 'colony.png': png, 'colony-atlas.json': json(atlas), 'manifest.json': json(manifest), 'CREDITS.txt': Buffer.from(credits) };
}

export function exportAssets({ check = false, output = resolve(webRoot, 'public/game-assets'), selectionPath } = {}) {
  const files = generate(selectionPath);
  if (check) {
    const actual = readdirSync(output).sort();
    if (JSON.stringify(actual) !== JSON.stringify(Object.keys(files).sort())) throw new Error('Unexpected or missing runtime assets');
    for (const [name, bytes] of Object.entries(files)) {
      if (!readFileSync(resolve(output, name)).equals(bytes)) throw new Error(`Stale/corrupt generated asset: ${name}`);
    }
  } else {
    mkdirSync(output, { recursive: true });
    for (const [name, bytes] of Object.entries(files)) writeFileSync(resolve(output, name), bytes);
  }
  return { files: Object.keys(files), sprites: Object.keys(JSON.parse(files['manifest.json']).sprites).length,
    bytes: Object.values(files).reduce((sum, bytes) => sum + bytes.length, 0) };
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { console.log(JSON.stringify({ check: process.argv.includes('--check'), ...exportAssets({ check: process.argv.includes('--check') }) }, null, 2)); }
  catch (error) { console.error(error.message); process.exitCode = 1; }
}
