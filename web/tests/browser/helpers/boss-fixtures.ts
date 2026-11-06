import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { canonical } from '../../../src/simulation/canonical.ts';
import { parseSave } from '../../../src/persistence/schema.ts';
import type { Save } from '../../../src/persistence/schema.ts';
import type { BossCounterMode } from '../../../src/simulation/boss-model.ts';
import type { CommandInput, World } from '../../../src/simulation/model.ts';
import { distance, same } from '../../../src/simulation/model.ts';
import { enqueue, step } from '../../../src/simulation/step.ts';
import { BOSS_CONTENT } from '../../../src/simulation/boss-content.ts';

export const bossCases = [
  { seed: 43, kind: 'bellkeeper', name: 'Bellkeeper', mode: 'strike', effects: ['soundseek', 'ring'], alias: 'boss-bellkeeper', sprite: 'urizen-onebit-r42-c110', biome: 'marsh' },
  { seed: 44, kind: 'sluice', name: 'Sluice-Maw', mode: 'drain', effects: ['surge', 'feast'], alias: 'boss-sluice-maw', sprite: 'urizen-onebit-r42-c108', biome: 'ash' },
  { seed: 45, kind: 'threadmother', name: 'Threadmother', mode: 'ignite', effects: ['reel', 'fire'], alias: 'boss-threadmother', sprite: 'urizen-onebit-r15-c002', biome: 'woodland' },
] as const;
export const bossFixtureDirectory = resolve(process.env.ROAG_BOSS_FIXTURE_DIR ?? resolve(import.meta.dirname, '../../..', process.env.ROAG_EVIDENCE_DIR ?? '../artifacts/colony-web/P07', 'acceptance-bosses'));
export const worldHash = (value: unknown): string => createHash('sha256').update(canonical(value)).digest('hex');
type Metadata = { origin: string; seed: number; replayHash: string; counters: string[]; specials: string[]; saveChecks: number;
  continuations: { label: string; start: number; end: number; hash: string }[];
  final: { tick: number; heart: number; hash: string; people: World['people']; combat: World['combat']['stats'] } };
const metadataCache = new Map<number, Promise<Metadata>>();
async function metadata(seed: number): Promise<Metadata> {
  let pending = metadataCache.get(seed);
  if (!pending) {
    pending = (async () => {
      const directory = resolve(bossFixtureDirectory, `seed-${seed}`), value = JSON.parse(await readFile(resolve(directory, 'boss-scenarios.json'), 'utf8')) as Metadata;
      assert.equal(value.seed, seed); assert.match(value.origin, /ordinary commands/); assert.match(value.origin, /no supplied inventory/);
      assert.equal(value.replayHash, value.final.hash); assert.equal(value.final.combat.bosses, 1); assert.ok(value.final.heart > 0);
      assert.equal(value.final.people.length, 6); assert.ok(value.final.people.every(p => p.life === 'alive' && p.body.health === 100));
      const final = parseSave(await readFile(resolve(directory, 'final.json'), 'utf8'));
      assert.equal(worldHash(final.world), value.replayHash); assert.equal(final.world.combat.completed, 4); assert.equal(final.world.terminal, null);
      assert.equal(final.world.combat.lootWood, final.world.combat.stats.kills + 4 * final.world.combat.stats.bosses);
      assert.ok(value.saveChecks > 0 && value.continuations.length > 0);
      return value;
    })(); metadataCache.set(seed, pending);
  }
  return pending;
}
export async function bossFixture(seed: number, label: string): Promise<{ saved: Save; metadata: Metadata }> {
  const report = await metadata(seed); assert.ok(report.continuations.some(c => c.label === label), `accepted continuation for ${label}`);
  const saved = parseSave(await readFile(resolve(bossFixtureDirectory, `seed-${seed}`, `${label}.json`), 'utf8'));
  assert.equal(saved.world.seed, seed); assert.equal(saved.world.scenario, 'colony'); assert.equal(saved.controls.manualPaused, true);
  return { saved, metadata: report };
}
/** A real command in a copy identifies the first paid completion; browser state is never injected. */
export function counterPlan(saved: Save, mode: BossCounterMode): { actor: number; target: number; input: CommandInput; completedAt: number; healthBefore: number; position: { x: number; y: number }; progressTicks: number } {
  for (const work of saved.world.bosses.work.filter(w => w.mode === mode)) {
    const feature = saved.world.bosses.features.find(f => f.id === work.target); assert.ok(feature);
    const input: CommandInput = { kind: 'battle', actor: work.actor, mode, target: { x: feature.x, y: feature.y } }, copy = structuredClone(saved.world);
    if (!enqueue(copy, input)) continue;
    let progressed = 0;
    for (let n = 0; n < 250 && !copy.terminal; n++) {
      const current = copy.bosses.work.find(w => w.actor === work.actor && w.mode === mode), before = copy.bosses.features.find(f => f.id === feature.id), person = copy.people.find(p => p.id === work.actor)!;
      const finishing = current?.progress === BOSS_CONTENT.counters[mode] - 1;
      if (current) progressed++;
      step(copy);
      if (!before || !finishing) continue;
      const after = copy.bosses.features.find(f => f.id === feature.id), encounter = copy.bosses.encounters.find(e => e.owner === feature.owner);
      const completed = mode === 'drain' ? encounter?.drained[feature.channel!] === true
        : mode === 'ignite' ? copy.bosses.effects.some(e => e.kind === 'fire' && e.feature === feature.id && e.source === work.actor)
          : !after && (mode !== 'strike' || (encounter?.exposedUntil ?? 0) > copy.tick);
      if (completed) {
        assert.ok(person.life === 'alive' && distance(person, feature) <= 1);
        assert.ok(copy.tick - saved.world.tick >= BOSS_CONTENT.counters[mode]);
        return { actor: work.actor, target: feature.id, input, completedAt: copy.tick, healthBefore: feature.health, position: { x: person.x, y: person.y }, progressTicks: progressed };
      }
    }
  }
  throw new Error(`No paid ${mode} completion from accepted mid-counter checkpoint`);
}
export function exactContinuation(before: Save, after: Save): void {
  const expected = structuredClone(before.world); while (expected.tick < after.world.tick) step(expected);
  assert.equal(worldHash(after.world), worldHash(expected));
}
export function pendingCounter(world: World, input: CommandInput): boolean {
  return input.kind === 'battle' && world.pending.some(c => c.input.kind === 'battle' && c.input.actor === input.actor && c.input.mode === input.mode && same(c.input.target, input.target));
}
