import test from 'node:test';
import assert from 'node:assert/strict';
import { createWorld } from '../src/simulation/world.ts';
import { bossLayoutAt, findBossLayout, bossLayoutCells, BOSS_LAYOUT_SEARCH_LIMIT } from '../src/simulation/boss-layouts.ts';
import { createRaider } from '../src/simulation/combat.ts';
import { enqueue, step } from '../src/simulation/step.ts';
import { index, neighbors, same, walkable } from '../src/simulation/model.ts';
import { route } from '../src/simulation/navigation.ts';
import { emptyStock } from '../src/simulation/work-content.ts';

const kinds = ['bellkeeper', 'sluice', 'threadmother'], approaches = ['north', 'east', 'south', 'west'];
const snapshot = world => JSON.stringify(world);
function flat() { const w = createWorld(42, { scenario: 'work-lab' }); w.terrain.fill(1); return w; }
function blocking(layout) { return [...layout.resonators, ...layout.anchors]; }
function counters(layout) { return [...blocking(layout), ...layout.channels.map(channel => channel.valve)]; }
function wall(w, cell, kind = 'wall') {
  const structure = { id: w.nextId++, ...cell, kind, health: 100, open: kind === 'door', inventory: emptyStock(), embodied: emptyStock() };
  w.structures.push(structure); return structure;
}
function connected(cells) {
  const keys = new Set(cells.map(cell => index(cell.x, cell.y))), visited = new Set(), queue = [cells[0]];
  while (queue.length) {
    const cell = queue.shift(), key = index(cell.x, cell.y); if (visited.has(key)) continue; visited.add(key);
    for (const next of neighbors(cell)) if (keys.has(index(next.x, next.y)) && !visited.has(index(next.x, next.y))) queue.push(next);
  }
  return visited.size === keys.size;
}
function assertCounterRoutes(world, layout) {
  const copy = structuredClone(world);
  for (const cell of blocking(layout)) wall(copy, cell);
  for (const counter of counters(layout)) {
    assert.ok(neighbors(counter).some(goal => walkable(copy, goal) && copy.people.some(person => person.life === 'alive'
      && (same(person, goal) || route(copy, person, goal, new Map(), person.id, 16384).path.length > 0))), `${layout.kind} counter accessible through actual navigation at ${counter.x},${counter.y}`);
  }
}

test('canonical layouts contain explicit connected channels and leave Threadmother central walking gap', () => {
  const w = flat(), center = { x: 84, y: 64 }, before = snapshot(w);
  for (const kind of kinds) {
    const result = bossLayoutAt(w, kind, center); assert.equal(result.reason, null); assert.equal(result.attempts, 1);
    const layout = result.layout; assert.equal(layout.kind, kind); assert.deepEqual(layout.center, center);
    assertCounterRoutes(w, layout);
    if (kind === 'bellkeeper') assert.equal(layout.resonators.length, 3);
    if (kind === 'sluice') {
      assert.equal(layout.channels.length, 2); assert.equal(connected(layout.channels.flatMap(channel => channel.cells)), true);
      for (const channel of layout.channels) {
        assert.equal(channel.cells.length, 9); assert.equal(connected(channel.cells), true); assert.equal(new Set(channel.cells.map(cell => index(cell.x, cell.y))).size, 9);
        assert.ok(channel.cells.some(cell => same(cell, channel.pool))); assert.deepEqual(channel.flow, { x: 1, y: 0 });
        assert.ok(neighbors(channel.valve).some(cell => channel.cells.some(other => same(cell, other))));
      }
    }
    if (kind === 'threadmother') {
      assert.equal(layout.anchors.length, 2); assert.equal(layout.webs.length, 6);
      assert.ok([...layout.anchors, ...layout.webs].every(cell => cell.y !== center.y));
      assert.ok(bossLayoutCells(layout).some(cell => same(cell, { x: center.x + 2, y: center.y })));
    }
  }
  assert.equal(snapshot(w), before);
});

test('current and reserved bodies, colony work, sources, loot and hostile walls protect their footprints', () => {
  const initial = flat(), center = { x: 84, y: 64 }, target = bossLayoutAt(initial, 'bellkeeper', center).layout.resonators[0];
  const cases = {
    person(w) { Object.assign(w.people[0], target); },
    reservedPerson(w) { Object.assign(w.people[0], { x: target.x - 1, y: target.y, next: { ...target } }); },
    corpse(w) { Object.assign(w.people[0], { ...target, life: 'dead' }); },
    raider(w) { const ticket = { id: w.nextId++, kind: 'raider', squadId: null, leaderId: null, edge: 'east', entryOffset: 0 }; w.combat.raiders.push(createRaider(w, ticket, target)); },
    reservedRaider(w) { this.raider(w); const r = w.combat.raiders[0]; r.x--; r.next = { ...target }; },
    openGate(w) { wall(w, target, 'door'); },
    stockpile(w) { wall(w, target, 'stockpile'); },
    blueprint(w) { w.jobs.push({ id: w.nextId++, kind: 'build', state: 'active', ...target }); },
    suspendedBlueprint(w) { w.jobs.push({ id: w.nextId++, kind: 'build', state: 'suspended', ...target }); },
    source(w) { Object.assign(w.resources[0], { ...target, remaining: 0 }); },
    loot(w) { w.piles.push({ id: w.nextId++, ...target, inventory: emptyStock() }); },
    barricade(w) { w.combat.barricades.push({ id: w.nextId++, owner: 999, ...target, health: 36, expiresAt: 600 }); },
  };
  for (const [name, edit] of Object.entries(cases)) {
    const world = structuredClone(initial); edit.call(cases, world); const before = snapshot(world);
    const result = bossLayoutAt(world, 'bellkeeper', center); assert.equal(result.layout, null, name); assert.equal(result.reason, 'Candidate overlaps protected or occupied ground', name); assert.equal(snapshot(world), before, name);
  }
  assert.equal(bossLayoutAt(initial, 'bellkeeper', { x: 64, y: 63 }).layout, null, 'heart neighbors remain protected');
});

test('candidate in an isolated pocket is deferred even when every fixture cell is open', () => {
  const w = flat(), center = { x: 84, y: 64 };
  for (let y = 59; y <= 69; y++) for (let x = 79; x <= 89; x++) if (x === 79 || x === 89 || y === 59 || y === 69) w.terrain[index(x, y)] = 2;
  const before = snapshot(w);
  for (const kind of kinds) { const result = bossLayoutAt(w, kind, center); assert.equal(result.layout, null); assert.equal(result.reason, 'Candidate is disconnected from the colony'); }
  assert.equal(snapshot(w), before);
});

test('a resonator cannot close a corridor containing previously accessible ground', () => {
  const w = flat(); w.terrain.fill(2);
  const open = (x, y) => w.terrain[index(x, y)] = 1;
  for (let x = 65; x <= 80; x++) open(x, 64);
  for (let y = 62; y <= 67; y++) open(80, y);
  for (let x = 77; x <= 84; x++) open(x, 62);
  Object.assign(w.people[0], { x: 65, y: 64 });
  const before = snapshot(w), result = bossLayoutAt(w, 'bellkeeper', { x: 80, y: 64 });
  assert.equal(result.layout, null); assert.equal(result.reason, 'Blocking fixtures would seal an existing route'); assert.equal(snapshot(w), before);
});

test('a separate heart-adjacent chamber does not make inaccessible counters part of the occupied colony', () => {
  const w = flat();
  for (let x = 0; x < 128; x++) w.terrain[index(x, 64)] = 2;
  w.terrain[index(65, 64)] = 1; w.terrain[index(65, 63)] = 2;
  w.people.forEach((p, i) => { p.x = 65 + i; p.y = 66; });
  const before = snapshot(w), isolated = bossLayoutAt(w, 'bellkeeper', { x: 64, y: 52 });
  assert.equal(isolated.layout, null); assert.equal(isolated.reason, 'Candidate is disconnected from the colony');
  const accessible = bossLayoutAt(w, 'bellkeeper', { x: 84, y: 76 }); assert.ok(accessible.layout); assertCounterRoutes(w, accessible.layout);
  assert.equal(snapshot(w), before);
});

test('bounded fallback is stable by approach, skips occupied centers and defers when all64 are blocked', () => {
  const w = flat(), rng = structuredClone(w.rng), occupied = new Set();
  for (const approach of approaches) {
    const result = findBossLayout(w, 'bellkeeper', approach); assert.ok(result.layout); assert.ok(result.attempts <= 64);
    assert.deepEqual(findBossLayout(w, 'bellkeeper', approach), result);
  }
  for (let n = 0; n < BOSS_LAYOUT_SEARCH_LIMIT; n++) {
    const result = findBossLayout(w, 'bellkeeper', 'east'); if (!result.layout) break;
    const key = index(result.layout.center.x, result.layout.center.y); assert.equal(occupied.has(key), false); occupied.add(key); wall(w, result.layout.center, 'stockpile');
  }
  const before = snapshot(w), blocked = findBossLayout(w, 'bellkeeper', 'east');
  assert.equal(blocked.layout, null); assert.equal(blocked.reason, 'No safe site within the bounded search'); assert.equal(blocked.attempts, 64);
  assert.equal(snapshot(w), before); assert.deepEqual(w.rng, rng); assert.ok(occupied.size > 0);
  for (const neighbor of neighbors(w.heart)) w.terrain[index(neighbor.x, neighbor.y)] = 2;
  assert.deepEqual(findBossLayout(w, 'bellkeeper', 'east'), { layout: null, reason: 'The colony has no open heart approach', attempts: 0 });
});

test('ordinary player stockpile and blueprint edits invalidate the old site without repainting any colony state', () => {
  const w = createWorld(42), first = findBossLayout(w, 'bellkeeper', 'east').layout; assert.ok(first);
  assert.equal(enqueue(w, { kind: 'stockpile', target: first.center }), true); step(w);
  assert.equal(enqueue(w, { kind: 'build', building: 'wall', target: first.resonators[0] }), true); step(w);
  const before = snapshot(w), edited = bossLayoutAt(w, 'bellkeeper', first.center), replacement = findBossLayout(w, 'bellkeeper', 'east');
  assert.equal(edited.layout, null); assert.ok(replacement.layout); assert.notDeepEqual(replacement.layout.center, first.center);
  assert.equal(snapshot(w), before); assert.ok(w.structures.some(s => s.kind === 'stockpile' && same(s, first.center))); assert.ok(w.jobs.some(j => j.kind === 'build' && same(j, first.resonators[0])));
});

test('multi-seed and approach matrix produces safe layouts without changing terrain, RNG, IDs or work', () => {
  const seeds = [1, 2, 3, 4, 5, 7, 11, 19, 27, 42, 73, 99, 123, 456, 999, 1337, 4096, 65535, 2147483646];
  let checked = 0;
  for (const seed of seeds) {
    const w = createWorld(seed), before = snapshot(w);
    for (const kind of kinds) for (const approach of approaches) {
      const result = findBossLayout(w, kind, approach); assert.ok(result.layout, `${seed}/${kind}/${approach}: ${result.reason}`);
      assert.ok(result.attempts >= 1 && result.attempts <= BOSS_LAYOUT_SEARCH_LIMIT); assertCounterRoutes(w, result.layout);
      assert.deepEqual(bossLayoutAt(w, kind, result.layout.center).layout, result.layout); checked++;
    }
    assert.equal(snapshot(w), before, `seed${seed} stays byte-identical`);
  }
  assert.equal(checked, 228);
});

test('invalid bounds defer cleanly and returned coordinate objects do not alias the world or later calls', () => {
  const w = flat(), before = snapshot(w);
  for (const center of [{ x: 0, y: 0 }, { x: 127, y: 127 }, { x: NaN, y: 64 }, { x: 64.5, y: 64 }]) assert.equal(bossLayoutAt(w, 'threadmother', center).layout, null);
  assert.equal(findBossLayout(w, 'unknown', 'east').reason, 'Unknown boss kind'); assert.equal(findBossLayout(w, 'bellkeeper', 'sideways').reason, 'Unknown raid approach');
  const original = findBossLayout(w, 'sluice', 'east'), baseline = structuredClone(original); original.layout.channels[0].cells[0].x = -100; original.layout.center.x = -100;
  assert.deepEqual(findBossLayout(w, 'sluice', 'east'), baseline); assert.equal(snapshot(w), before);
});
