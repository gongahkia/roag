// Strict static server for acceptance tests: no SPA fallback and no repository exposure.
import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { resolve, extname, sep } from 'node:path';
import { webRoot } from './export-assets.mjs';
const mode = process.argv[2];
if (!['root', 'roag'].includes(mode)) throw new Error('Expected root or roag');
const mount = mode === 'root' ? '/' : '/roag/';
const root = resolve(webRoot, process.env.ROAG_EVIDENCE_DIR ?? '../artifacts/colony-web/P07', `build-${mode}`);
const basePort = Number(process.env.ROAG_BROWSER_PORT_BASE ?? 4173);
if (!Number.isInteger(basePort) || basePort < 1024 || basePort > 65534) throw new Error('Invalid ROAG_BROWSER_PORT_BASE');
const port = basePort + (mode === 'root' ? 0 : 1);
const mime = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css', '.png': 'image/png', '.json': 'application/json', '.txt': 'text/plain; charset=utf-8' };
const server = createServer(async (request, response) => {
  try {
    const path = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
    if (path === mount.slice(0, -1) && mount !== '/') { response.writeHead(308, { Location: mount }); response.end(); return; }
    if (!path.startsWith(mount)) throw new Error('Outside mount');
    const file = resolve(root, path.slice(mount.length) || 'index.html');
    if (!file.startsWith(root + sep)) throw new Error('Outside build');
    if (!(await stat(file)).isFile()) throw new Error('Not a file');
    const bytes = await readFile(file);
    response.writeHead(200, { 'Content-Type': mime[extname(file)] ?? 'application/octet-stream', 'Cache-Control': 'no-store' }); response.end(bytes);
  } catch { response.writeHead(404); response.end('Not found'); }
});
server.listen(port, '127.0.0.1', () => console.log(`Production ${mode}: http://127.0.0.1:${port}${mount}`));
