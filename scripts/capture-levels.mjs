import { spawn, spawnSync } from 'node:child_process';
import { mkdtemp, mkdir, rm, stat, writeFile } from 'node:fs/promises';
import { randomBytes } from 'node:crypto';
import { tmpdir } from 'node:os';
import path from 'node:path';

const usage = 'usage: npm run capture:levels -- [--count 1-25] [--out directory] [--seed prefix]';

const parseArguments = (argumentsList) => {
  const options = { count: 25, out: undefined, seed: undefined };
  for (let index = 0; index < argumentsList.length; index += 1) {
    const argument = argumentsList[index];
    if (argument === '--count' || argument === '--out' || argument === '--seed') {
      const value = argumentsList[index + 1];
      if (!value || value.startsWith('--')) throw new Error(usage);
      if (argument === '--count') options.count = Number(value);
      if (argument === '--out') options.out = value;
      if (argument === '--seed') options.seed = value;
      index += 1;
      continue;
    }
    throw new Error(usage);
  }
  if (!Number.isInteger(options.count) || options.count < 1 || options.count > 25) throw new Error('--count must be an integer from 1 through 25');
  return options;
};

const waitForServer = async (url) => {
  const deadline = Date.now() + 15_000;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(url);
      if (response.ok) return;
    } catch { }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`Vite did not start at ${url}`);
};

const terminate = async (child) => {
  if (child.exitCode !== null) return;
  child.kill('SIGTERM');
  await new Promise((resolve) => child.once('exit', resolve));
};

const timestamp = () => new Date().toISOString().replace(/[:.]/g, '-');
const slug = (value) => value.replace(/[^a-z0-9-]+/gi, '-').replace(/^-+|-+$/g, '').slice(0, 48) || 'levels';

const main = async () => {
  const options = parseArguments(process.argv.slice(2));
  const baseSeed = options.seed ?? `capture-${randomBytes(5).toString('hex')}`;
  const outputDirectory = path.resolve(options.out ?? path.join('output', 'levels', `${timestamp()}-${slug(baseSeed)}`));
  const port = Number(process.env.LEVEL_CAPTURE_PORT ?? 4173);
  const browser = process.env.BROWSER_BIN ?? 'chromium-browser';
  const baseUrl = `http://127.0.0.1:${port}`;
  const profileDirectory = await mkdtemp(path.join(tmpdir(), 'golf-level-capture-'));
  const server = spawn('npm', ['run', 'dev', '--', '--host', '127.0.0.1', '--port', String(port), '--strictPort'], {
    stdio: 'pipe',
    env: process.env,
  });

  await mkdir(outputDirectory, { recursive: true });
  const manifest = { baseSeed, count: options.count, createdAt: new Date().toISOString(), levels: [] };
  try {
    await waitForServer(baseUrl);
    for (let index = 0; index < options.count; index += 1) {
      const seed = `${baseSeed}-${String(index + 1).padStart(2, '0')}`;
      const filename = `${String(index + 1).padStart(2, '0')}-${slug(seed)}.png`;
      const file = path.join(outputDirectory, filename);
      const url = `${baseUrl}/?seed=${encodeURIComponent(seed)}&capture=1`;
      const result = spawnSync(browser, [
        '--headless',
        '--disable-gpu',
        '--hide-scrollbars',
        '--run-all-compositor-stages-before-draw',
        '--virtual-time-budget=900',
        '--window-size=1440,900',
        `--user-data-dir=${profileDirectory}`,
        `--screenshot=${file}`,
        url,
      ], { encoding: 'utf8' });
      if (result.error) throw new Error(`Could not start ${browser}: ${result.error.message}`);
      if (result.status !== 0) throw new Error(`Screenshot ${index + 1} failed: ${result.stderr || result.stdout}`);
      const size = (await stat(file)).size;
      if (!size) throw new Error(`Screenshot ${index + 1} was empty`);
      manifest.levels.push({ index: index + 1, seed, file: filename });
      process.stdout.write(`captured ${index + 1}/${options.count}: ${filename}\n`);
    }
    await writeFile(path.join(outputDirectory, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
    process.stdout.write(`saved ${options.count} levels to ${outputDirectory}\n`);
  } finally {
    await terminate(server);
    await rm(profileDirectory, { recursive: true, force: true });
  }
};

main().catch((error) => {
  process.stderr.write(`${error instanceof Error ? error.message : String(error)}\n`);
  process.exitCode = 1;
});
