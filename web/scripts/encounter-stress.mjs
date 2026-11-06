import assert from 'node:assert/strict';
import { performance } from 'node:perf_hooks';
import { createWorld } from '../src/simulation/world.ts';
import { createRaider } from '../src/simulation/combat.ts';
import { step } from '../src/simulation/step.ts';
import { index, spatial, walkable } from '../src/simulation/model.ts';
import { COMBAT_CONTENT } from '../src/simulation/combat-content.ts';
import { ENCOUNTER_CONTENT } from '../src/simulation/encounter-content.ts';
import { emptyStock, WORK_CONTENT } from '../src/simulation/work-content.ts';

// Explicit artificial maximum-actor stress fixture. This bypasses the ordinary
// finite wave budget, is not imported as a save, and is not a balance claim.
const w = createWorld(42, { scenario: 'work-lab' });
w.terrain.fill(1); w.people.forEach((p, i) => { p.x = 110 + i; p.y = 110; for (const duty of Object.keys(p.body.duties)) p.body.duties[duty] = 0; });
for (const c of [{ x: 64, y: 63 }, { x: 65, y: 64 }, { x: 64, y: 65 }, { x: 63, y: 64 }]) w.terrain[index(c.x, c.y)] = 2;
w.combat.phase = 'combat'; w.combat.wave = 100; w.combat.budget = 60; w.combat.spent = 60;
const kinds = ['shield', 'archer', 'healer', 'rusher', 'raider', 'breacher'];
for (let n = 0; n < COMBAT_CONTENT.maxRaiders; n++) {
  const kind = n < 4 ? 'captain' : kinds[n % kinds.length];
  const ticket = { id: w.nextId++, kind, squadId: null, leaderId: null, edge: 'east', entryOffset: 0 };
  const position = n === 0 ? { x: 60, y: 60 } : n === 1 ? { x: 68, y: 60 } : n === 2 ? { x: 60, y: 68 } : n === 3 ? { x: 68, y: 68 } : { x: 10 + n % 15 * 3, y: 10 + Math.floor(n / 15) * 3 };
  const r = createRaider(w, ticket, position); w.combat.raiders.push(r); w.combat.stats.spawned++;
}
const victim = w.combat.raiders.at(-1); victim.kind = 'rusher'; victim.health = ENCOUNTER_CONTENT.profiles.rusher.health;
victim.x = 90; victim.y = 90; victim.retryAt = 1000; victim.target = { kind: 'heart', id: w.heart.id };
for (let i = 0; i < 12; i++) w.combat.pending.push({ id: w.nextId++, kind: 'raider', squadId: null, leaderId: null, edge: 'east', entryOffset: i });
const pendingIds = w.combat.pending.map(t => t.id), start = performance.now();
let maxActors = 0, maxAttacks = 0, maxBarricades = 0, ticks = 0;
const tick = () => {
  const positions = new Map(w.combat.raiders.map(r => [r.id, { x: r.x, y: r.y }])); step(w); ticks++;
  assert.equal(w.terminal, null); assert.ok(w.combat.raiders.length <= COMBAT_CONTENT.maxRaiders); assert.ok(w.combat.attacks.length <= COMBAT_CONTENT.maxAttacks); assert.ok(w.combat.barricades.length <= ENCOUNTER_CONTENT.rampart.maxBarricades);
  maxActors = Math.max(maxActors, w.combat.raiders.length); maxAttacks = Math.max(maxAttacks, w.combat.attacks.length); maxBarricades = Math.max(maxBarricades, w.combat.barricades.length);
  const occupied = spatial(w), expected = w.people.filter(p => p.life !== 'dead').reduce((n, p) => n + 1 + Number(!!p.next), 0) + w.combat.raiders.reduce((n, r) => n + 1 + Number(!!r.next), 0);
  assert.equal(occupied.size, expected, 'no current or reserved occupancy overlap');
  for (const r of w.combat.raiders) { const old = positions.get(r.id); if (old) assert.ok(Math.abs(r.x - old.x) + Math.abs(r.y - old.y) <= 1); assert.ok(walkable(w, r)); }
  for (const attack of w.combat.attacks) assert.ok(attack.source.kind === 'raider' ? w.combat.raiders.some(r => r.id === attack.source.id) : w.structures.some(s => s.id === attack.source.id) || w.people.some(p => p.id === attack.source.id && p.life === 'alive'));
  assert.equal(w.combat.lootWood, w.combat.stats.kills + 4 * w.combat.stats.bosses);
};
for (let n = 0; n < 200; n++) tick();
assert.deepEqual(w.combat.pending.map(t => t.id), pendingIds, 'full cap retains every pending identity');
assert.equal(w.combat.stats.kills, 0);
const tower = { id: w.nextId++, x: 90, y: 88, kind: 'tower', health: 100, open: false, inventory: emptyStock(), embodied: { ...WORK_CONTENT.buildings.tower.cost } }; tower.inventory.ammo = 2; w.structures.push(tower);
for (let n = 0; n < 100; n++) tick();
assert.equal(w.combat.stats.kills, 1); assert.equal(w.combat.stats.shots, 2); assert.equal(w.consumed.ammo, 2); assert.equal(w.combat.lootWood, 1); assert.equal(w.combat.raiders.length, 150);
assert.deepEqual(w.combat.pending.map(t => t.id), pendingIds.slice(1)); assert.ok(w.combat.raiders.some(r => r.id === pendingIds[0]));
assert.equal(w.combat.raiders.some(r => r.id === victim.id), false); assert.ok(maxAttacks > 0); assert.ok(maxBarricades > 0);
const clone = structuredClone(w), initialLoot = w.combat.lootWood;
for (let n = 0; n < 100; n++) { tick(); step(clone); }
assert.deepEqual(w, clone); assert.equal(w.combat.lootWood, initialLoot);
console.log(JSON.stringify({ fixture: 'artificial-150-actor-cap; not ordinary wave balance or a save-valid budget', ticks, elapsedMs: Math.round(performance.now() - start), maxActors, maxAttacks, maxBarricades, pendingRetained: w.combat.pending.length, kills: w.combat.stats.kills, lootWood: w.combat.lootWood, exactContinuation: true }, null, 2));
