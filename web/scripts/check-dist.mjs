import { readFileSync, readdirSync, statSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, relative, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { gzipSync, brotliCompressSync, constants } from 'node:zlib';
import { generate, hash, webRoot } from './export-assets.mjs';

export function checkDist(directory, base = './') {
  const root = resolve(directory);
  const walk = (path) => readdirSync(path).flatMap((name) => {
    const full = join(path, name);
    if (statSync(full).isDirectory()) return walk(full);
    return [{ path: relative(root, full).replaceAll('\\', '/'), bytes: readFileSync(full) }];
  });
  const files = walk(root);
  const expected = generate();
  const runtime = files.filter((f) => f.path.startsWith('game-assets/'));
  if (runtime.length !== Object.keys(expected).length) throw new Error('Runtime file count differs from selected export');
  for (const file of files) {
    if (file.path.startsWith('game-assets/')) {
      const original = expected[file.path.slice('game-assets/'.length)];
      if (!original || !original.equals(file.bytes)) throw new Error(`Unexpected/changed shipped artwork: ${file.path}`);
    } else if (!/^index\.html$|^assets\/[a-zA-Z0-9_-]+\.(js|css)$/.test(file.path)) throw new Error(`Unexpected distribution file: ${file.path}`);
  }
  const html = readFileSync(join(root, 'index.html'), 'utf8');
  for (const [, url] of html.matchAll(/(?:src|href)=["']([^"']+)["']/g)) {
    if (url.startsWith('/') && (base === './' || !url.startsWith(base))) throw new Error(`Production URL escapes configured base ${base}: ${url}`);
  }
  if (!files.some((f) => f.path.endsWith('.js')) || !files.some((f) => f.path.endsWith('.css'))) throw new Error('Missing application bundle');
  // Names inside manifest provenance are permitted; the actual research files are not.
  if (files.some((f) => /(?:catalog|atlas-index|atlas\.html|\.zip$|\.gz$|\.map$)/.test(f.path))) throw new Error('Research/archives shipped');
  const sizes = files.map((file) => ({ path: file.path, raw: file.bytes.length,
    gzip: gzipSync(file.bytes, { level: 9 }).length,
    brotli: brotliCompressSync(file.bytes, { params: { [constants.BROTLI_PARAM_QUALITY]: 11 } }).length,
    sha256: hash(file.bytes) }));
  const totals = sizes.reduce((sum, file) => ({ raw: sum.raw + file.raw, gzip: sum.gzip + file.gzip, brotli: sum.brotli + file.brotli }), { raw: 0, gzip: 0, brotli: 0 });
  return { directory: relative(webRoot, root), files: sizes, totals, compression: 'Sum of each file compressed independently, gzip level 9 / Brotli quality 11. Actual transfer depends on host encoding.' };
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const result = checkDist(process.argv[2] ?? resolve(webRoot, 'dist'));
  const report = resolve(webRoot, process.env.ROAG_EVIDENCE_DIR ?? '../artifacts/colony-web/P03', `${process.argv[3] ?? 'build'}-sizes.json`);
  mkdirSync(resolve(report, '..'), { recursive: true }); writeFileSync(report, JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
}
