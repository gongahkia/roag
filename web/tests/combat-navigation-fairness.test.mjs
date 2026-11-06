import test from 'node:test';
import assert from 'node:assert/strict';
import { createWorld } from '../src/simulation/world.ts';
import { createRaider } from '../src/simulation/combat.ts';
import { step } from '../src/simulation/step.ts';
import { index, spatial } from '../src/simulation/model.ts';
import { COMBAT_CONTENT } from '../src/simulation/combat-content.ts';

// Explicit isolation fixtures for scheduling, not naturally funded wave/save
// claims. The 150-actor case deliberately exceeds the normal finite wave budget.
function congested(count) {
  const w = createWorld(42, { scenario: 'work-lab' }); w.terrain.fill(1);
  w.people.forEach((p, i) => { p.x = 110 + i; p.y = 110; for (const duty of Object.keys(p.body.duties)) p.body.duties[duty] = 0; });
  w.combat.phase = 'combat'; w.combat.wave = 20; w.combat.budget = 60; w.combat.spent = 60;
  for (let n = 0; n < count; n++) {
    const last = n === count - 1, x = last ? 80 : 4 + n % 20 * 5, y = last ? 64 : 4 + Math.floor(n / 20) * 5;
    const ticket = { id: w.nextId++, kind: 'raider', squadId: null, leaderId: null, edge: 'east', entryOffset: 0 };
    w.combat.raiders.push(createRaider(w, ticket, { x, y })); w.combat.stats.spawned++;
    if (!last) for (const [dx, dy] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) w.terrain[index(x + dx, y + dy)] = 2;
  }
  return w;
}
function advance(w, ticks) {
  for (let n = 0; n < ticks; n++) {
    const previously = new Map(w.combat.raiders.map(r => [r.id, r.retryAt])); step(w);
    const searched = w.combat.raiders.filter(r => r.retryAt === w.tick + 10 && r.retryAt !== previously.get(r.id));
    assert.ok(searched.length <= COMBAT_CONTENT.searchesPerTick, 'two bounded searches per tick');
    const expected = w.people.reduce((sum, p) => sum + 1 + Number(!!p.next), 0) + w.combat.raiders.reduce((sum, r) => sum + 1 + Number(!!r.next), 0);
    assert.equal(spatial(w).size, expected, 'queued searches reserve distinct cells');
  }
}

test('twenty unreachable early attackers cannot starve a later clear route', () => {
  const w = congested(21), reachable = w.combat.raiders.at(-1);
  advance(w, 11); assert.ok(reachable.next, 'twenty older initial requests consume only ten ticks'); assert.equal(reachable.retryAt, 21);
  advance(w, 29); assert.ok(reachable.x <= 73); assert.equal(w.combat.raiders.slice(0, -1).every(r => !r.next && !r.route.length), true);
});

test('150 actors share searches fairly and keep over-capacity tickets across exact JSON checkpoint continuation', () => {
  const w = congested(150);
  for (let n = 0; n < 3; n++) w.combat.pending.push({ id: w.nextId++, kind: 'raider', squadId: null, leaderId: null, edge: 'east', entryOffset: n });
  const pending = structuredClone(w.combat.pending); advance(w, 40);
  assert.equal(w.combat.raiders.at(-1).x, 80, 'the final actor is still waiting its bounded turn');
  // Runtime-state JSON round-trip: this artificial over-budget stress fixture is
  // intentionally not presented to the production save-schema validator.
  const restored = JSON.parse(JSON.stringify(w)); advance(w, 80); advance(restored, 80);
  assert.deepEqual(restored, w); assert.deepEqual(w.combat.pending, pending); assert.equal(w.combat.raiders.length, 150);
  assert.ok(w.combat.raiders.at(-1).x < 80, 'last actor receives a search even while149 remain unreachable');
  assert.ok(w.combat.raiders.every(r => r.retryAt > 0), 'every live actor receives search service');
});
