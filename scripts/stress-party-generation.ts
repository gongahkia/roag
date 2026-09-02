import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { createGame, defaultConfig } from '../src/core/game';

const usage = 'usage: npm run stress:party -- [--count 1-10000] [--seed prefix] [--out file]';
const valueFor = (name: string) => {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : undefined;
};

const main = async () => {
  const count = Number(valueFor('--count') ?? 10_000);
  const seedPrefix = valueFor('--seed') ?? 'party-stress';
  const out = path.resolve(valueFor('--out') ?? path.join('output', 'stress', `party-${count}.json`));
  if (!Number.isInteger(count) || count < 1 || count > 10_000) throw new Error(usage);
  const startedAt = performance.now();
  const failures: { seed: string; detail: string }[] = [];
  const hashes = new Set<string>();
  const archetypes = new Map<string, number>();
  const themes = new Map<string, number>();
  for (let index = 0; index < count; index += 1) {
    const seed = `${seedPrefix}-${String(index + 1).padStart(5, '0')}`;
    const state = createGame({ ...defaultConfig(), seed, ruleset: 'party', holeCount: 1, humanCount: 2, botCount: 0 });
    const metadata = state.coursePlan[0]?.recipe.metadata;
    if (!state.course.score.playable || !metadata?.courseHash) failures.push({ seed, detail: state.course.score.rejection ?? 'missing playable score or course hash' });
    for (const event of state.instrumentation?.events ?? []) if (event.type === 'generation-failure') failures.push({ seed, detail: event.detail });
    if (metadata?.courseHash) hashes.add(metadata.courseHash);
    archetypes.set(state.course.archetype ?? 'unknown', (archetypes.get(state.course.archetype ?? 'unknown') ?? 0) + 1);
    themes.set(state.course.theme, (themes.get(state.course.theme) ?? 0) + 1);
  }
  const report = {
    schemaVersion: 1,
    kind: 'party-generator-stress',
    count,
    seedPrefix,
    elapsedSeconds: Number(((performance.now() - startedAt) / 1_000).toFixed(3)),
    failures: failures.slice(0, 50),
    failureCount: failures.length,
    uniqueCourseHashes: hashes.size,
    archetypes: Object.fromEntries(archetypes),
    themes: Object.fromEntries(themes),
  };
  await mkdir(path.dirname(out), { recursive: true });
  await writeFile(out, `${JSON.stringify(report, null, 2)}\n`);
  process.stdout.write(`${JSON.stringify({ output: out, ...report }, null, 2)}\n`);
  if (failures.length) process.exitCode = 1;
};

main().catch((error) => {
  process.stderr.write(`${error instanceof Error ? error.message : String(error)}\n`);
  process.exitCode = 1;
});
