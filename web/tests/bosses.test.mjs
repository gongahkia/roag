import test from 'node:test';
import assert from 'node:assert/strict';
import { createWorld } from '../src/simulation/world.ts';
import { createRaider, applyCombatCommand } from '../src/simulation/combat.ts';
import { step, enqueue } from '../src/simulation/step.ts';
import { BOSS_CONTENT as B } from '../src/simulation/boss-content.ts';
import { bossTurn, prepareBossSpawn, recordBossNoise, advanceBossEffects, damageBossFeature, bossDamage, bossCounterReason, bossCellMoveTicks, bossDied } from '../src/simulation/bosses.ts';
import { findBossLayout } from '../src/simulation/boss-layouts.ts';
import { distance, index, same, walkable, spatial } from '../src/simulation/model.ts';
import { fall } from '../src/simulation/needs.ts';

// Deliberately isolated mechanics fixtures; never used as ordinary balance evidence.
function fixture(kind) {
  const w = createWorld(42, { scenario: 'work-lab' }); w.terrain.fill(1); w.environment.wet = []; w.environment.ash = [];
  w.people.forEach((p, n) => { p.x = 30 + n * 2; p.y = 30; for (const duty of Object.keys(p.body.duties)) p.body.duties[duty] = 0; });
  w.combat.phase = 'combat'; w.combat.wave = 4;
  const ticket = { id: w.nextId++, kind, squadId: null, leaderId: null, edge: 'east', entryOffset: 0 };
  assert.equal(prepareBossSpawn(w, ticket), true);
  const e = w.bosses.encounters[0], r = createRaider(w, ticket, e.layout.center); w.combat.raiders.push(r); w.combat.stats.spawned++;
  return { w, r, e };
}
const at = c => ({ x: c.x, y: c.y });
function ticks(w, count) { for (let n = 0; n < count; n++) step(w); }
function until(w, predicate, limit = 200) { for (let n = 0; n < limit && !predicate(); n++) step(w); assert.ok(predicate(), `condition reached by tick ${w.tick}`); }
function counter(w, p, mode, f) { applyCombatCommand(w, { kind: 'battle', actor: p.id, mode, target: at(f) }); }
function api(w) {
  const value = { hits: [], moves: [],
    damage(ref, amount, origin) { value.hits.push({ ref, amount, origin: at(origin) }); },
    move(r, target, adjacent, allowedCells) { value.moves.push({ actor: r.id, target: at(target), adjacent, allowedCells }); },
    approach() {}, release() {}, shoveRaider() { return false; },
    shove(id, delta) { const p = w.people.find(p => p.id === id), next = { x: p.x + delta.x, y: p.y + delta.y }; if (p.next || !walkable(w, next) || spatial(w).has(index(next.x, next.y))) return false; Object.assign(p, next); return true; },
  }; return value;
}
function finishEffects(w, engine) { w.tick = Math.max(...w.bosses.effects.map(a => a.due)); advanceBossEffects(w, engine); }
function continueExactly(w, count) { const restored = JSON.parse(JSON.stringify(w)); for (let n = 0; n < count; n++) { step(w); step(restored); } assert.deepEqual(restored, w); }

test('boss spawn allocates complete safe overlay once and failed placement allocates no IDs', () => {
  for (const kind of ['bellkeeper', 'sluice', 'threadmother']) {
    const { w, r } = fixture(kind), terrain = [...w.terrain], rng = structuredClone(w.rng), ids = w.nextId;
    assert.equal(prepareBossSpawn(w, { id: r.id, kind, edge: 'east' }), true); assert.equal(w.nextId, ids); assert.deepEqual(w.terrain, terrain); assert.deepEqual(w.rng, rng);
    const placement = findBossLayout(w, kind, 'east'); assert.ok(placement.layout); assert.notDeepEqual(placement.layout.center, w.bosses.encounters[0].layout.center);
    w.terrain.fill(2); const before = structuredClone(w); assert.equal(prepareBossSpawn(w, { id: w.nextId, kind, edge: 'east' }), false); assert.match(w.bosses.status, /deferred/);
    before.bosses.status = w.bosses.status; assert.deepEqual(w, before);
  }
});

test('Bellkeeper noise is weighted, bounded and expiring; sound locks its cell independently of later noise', () => {
  const { w, r, e } = fixture('bellkeeper'), engine = api(w), target = { x: r.x - 5, y: r.y };
  recordBossNoise(w, target); assert.deepEqual(e.noise, { cell: target, at: 0 });
  bossTurn(w, r, engine); const sound = w.bosses.effects[0]; assert.equal(sound.kind, 'soundseek'); assert.deepEqual(sound.cells, [target]);
  recordBossNoise(w, { x: r.x, y: r.y + 1 }); assert.deepEqual(sound.cells, [target]); e.noise = null;
  for (let x = r.x - 3; x <= r.x - 1; x++) for (let y = 0; y < 128; y++) w.terrain[index(x, y)] = 2;
  recordBossNoise(w, target); assert.equal(e.noise, null);
  w.terrain.fill(1); recordBossNoise(w, target); w.tick = 501; advanceBossEffects(w, engine); assert.equal(e.noise, null);
});

test('ring cells stay clipped; breaking one resonator cancels its own pulses and exposes the boss', () => {
  const { w, r, e } = fixture('bellkeeper'), engine = api(w), f = w.bosses.features[0]; e.moves = 1;
  const removed = { x: f.x + 1, y: f.y }; w.terrain[index(removed.x, removed.y)] = 2;
  bossTurn(w, r, engine); assert.equal(w.bosses.effects.length, 9);
  const rings = w.bosses.effects.filter(a => a.feature === f.id); assert.ok(rings.every(a => !a.cells.some(c => same(c, removed))));
  w.terrain[index(removed.x, removed.y)] = 1; assert.ok(rings.every(a => !a.cells.some(c => same(c, removed))));
  damageBossFeature(w, f.id, 24, 2); assert.equal(w.bosses.effects.length, 6); assert.ok(w.bosses.effects.every(a => a.feature !== f.id));
  assert.equal(bossDamage(w, r.id, 10), 15); w.tick = 40; assert.equal(bossDamage(w, r.id, 10), 10);
});

test('physical counter work costs time, resets on interruption, releases orders and retains cargo', () => {
  const { w, e } = fixture('bellkeeper'), p = w.people[0], f = w.bosses.features[0]; e.readyAt = 100;
  p.cargo.wood = 3; counter(w, p, 'strike', f); ticks(w, 4); assert.equal(f.health, 24); assert.equal(w.bosses.work.length, 0);
  Object.assign(p, { x: f.x - 1, y: f.y, next: null, progress: 0, moveTicks: 4, route: [], destination: null });
  ticks(w, 5); assert.equal(w.bosses.work[0].progress, 5); assert.equal(f.health, 24);
  p.body.recoverAt = w.tick + 3; step(w); assert.equal(w.bosses.work.length, 0); p.body.recoverAt = 0;
  ticks(w, 9); assert.equal(f.health, 24); step(w); assert.equal(f.health, 12);
  ticks(w, 10); assert.equal(w.bosses.features.some(a => a.id === f.id), false); assert.equal(w.bosses.work.length, 0); assert.equal(w.combat.orders.some(o => o.actor === p.id), false); assert.equal(p.cargo.wood, 3);
});

test('paid valve drain cancels surge at its impact tick and prevents that channel healing again', () => {
  const { w, r, e } = fixture('sluice'), p = w.people[0], f = w.bosses.features[0], channel = e.layout.channels[0];
  Object.assign(p, at(f)); step(w); assert.equal(w.bosses.effects[0].kind, 'surge'); const due = w.bosses.effects[0].due;
  counter(w, p, 'drain', f); ticks(w, 19); assert.equal(e.drained[0], false); step(w);
  assert.equal(w.tick, due); assert.equal(e.drained[0], true); assert.ok(w.bosses.effects.every(a => a.channel !== 0)); assert.equal(bossCellMoveTicks(w, channel.pool), 4); assert.match(bossCounterReason(w, 'drain', f), /undrained/);
  r.health = 100; e.moves = 1; e.readyAt = 0; bossTurn(w, r, api(w)); assert.ok(w.bosses.effects.every(a => a.channel !== 0));
});

test('Sluice-Maw walks wet edges to pool, heals only on arrival and continues exactly mid-edge', () => {
  const { w, r, e } = fixture('sluice'); r.health = 100; e.moves = 1; step(w); assert.equal(w.bosses.effects[0].kind, 'feast'); const pool = e.layout.channels[0].pool;
  until(w, () => !!e.feast); assert.deepEqual(at(r), e.layout.center); assert.equal(r.health, 100);
  until(w, () => !!r.next && r.progress === 2); assert.equal(r.moveTicks, 6); const snapshot = structuredClone(w); let healed = false;
  for (let n = 0; n < 80; n++) { const previous = at(r); step(w); assert.ok(distance(previous, r) <= 1); if (r.health > 100) { assert.deepEqual(at(r), pool); assert.equal(r.health, 124); healed = true; break; } }
  assert.equal(healed, true); assert.equal(e.feast, null); continueExactly(snapshot, 100);
});

test('occupied pool or newly blocked channel denies feast without teleport or healing', () => {
  for (const obstruction of ['pool', 'channel']) {
    const { w, r, e } = fixture('sluice'); r.health = 100; e.moves = 1; step(w); const due = w.bosses.effects[0].due, channel = e.layout.channels[0];
    if (obstruction === 'pool') Object.assign(w.people[0], channel.pool); else w.terrain[index(e.layout.center.x, e.layout.center.y - 1)] = 2;
    while (w.tick < due) step(w); assert.equal(e.feast, null, obstruction); assert.equal(r.health, 100); assert.deepEqual(at(r), e.layout.center);
  }
});

test('Threadmother reel permits moving away, paid line cut, new-wall interception and downed bleed without movement', () => {
  for (const change of ['hit', 'move', 'cut', 'wall', 'downed']) {
    const { w, r } = fixture('threadmother'), p = w.people[0]; Object.assign(p, { x: r.x - 5, y: r.y }); if (change === 'downed') { p.body.health = 0; fall(w, p); }
    step(w); const reel = w.bosses.effects.find(a => a.kind === 'reel'); assert.ok(reel, change);
    const start = at(p), health = p.body.health, deathAt = p.body.deathAt, line = w.bosses.features.find(f => f.kind === 'line');
    if (change === 'move') assert.equal(enqueue(w, { kind: 'move', actor: p.id, target: { x: p.x, y: p.y + 1 } }), true);
    if (change === 'cut') { const cutter = w.people[1]; Object.assign(cutter, { x: line.x, y: line.y - 1 }); counter(w, cutter, 'cut', line); }
    if (change === 'wall') assert.equal(enqueue(w, { kind: 'build', target: at(line), building: 'wall' }), true);
    while (w.tick < reel.due) step(w); assert.equal(w.bosses.features.some(f => f.kind === 'line'), false, change);
    if (change === 'hit') { assert.equal(p.body.health, health - 12); assert.equal(distance(start, p), 1); }
    else if (change === 'downed') { assert.deepEqual(at(p), start); assert.equal(p.body.deathAt, deathAt - 120); }
    else assert.equal(p.body.health, health, change);
  }
});

test('anchor clear removes its group; regeneration preserves occupied ground, blueprints and the gap', () => {
  const { w, r, e } = fixture('threadmother'), anchor = w.bosses.features.find(f => f.kind === 'anchor'); damageBossFeature(w, anchor.id, anchor.health, 2); assert.equal(w.bosses.features.length, 0);
  step(w); const partition = w.bosses.effects.find(a => a.kind === 'partition'); assert.ok(partition);
  const protectedCell = e.layout.anchors[0]; Object.assign(w.people[0], protectedCell); assert.equal(enqueue(w, { kind: 'build', target: e.layout.webs[0], building: 'wall' }), true);
  while (w.tick < partition.due) step(w);
  assert.equal(w.bosses.features.some(f => same(f, protectedCell)), false); assert.equal(w.bosses.features.some(f => same(f, e.layout.webs[0])), false); assert.equal(w.bosses.features.some(f => same(f, { x: e.layout.center.x + 2, y: e.layout.center.y })), false);
  assert.ok(w.bosses.features.every(f => f.group === partition.id)); assert.equal(w.terrain[index(r.x, r.y)], 1);
});

test('connected fire stops at the gap, remains delayed and neutral, and persists after boss and igniter death', () => {
  const { w, r, e } = fixture('threadmother'), p = w.people[0], web = w.bosses.features.find(f => f.kind === 'web'); e.readyAt = 100;
  Object.assign(p, at(web)); counter(w, p, 'ignite', web); ticks(w, 23); assert.equal(w.bosses.effects.length, 0); step(w);
  const fire = w.bosses.effects.filter(a => a.kind === 'fire'); assert.equal(fire.length, 4); assert.ok(fire.every(a => a.origin.y < e.layout.center.y && a.due > w.tick)); assert.equal(new Set(fire.map(a => a.feature)).size, 4);
  const engine = api(w), first = fire[0], ally = w.people[1]; Object.assign(ally, first.origin);
  const ticket = { id: w.nextId++, kind: 'raider', squadId: null, leaderId: null, edge: 'east', entryOffset: 0 }, enemy = createRaider(w, ticket, { x: first.origin.x + 1, y: first.origin.y }); w.combat.raiders.push(enemy);
  p.life = 'dead'; w.combat.raiders = w.combat.raiders.filter(other => other.id !== r.id); bossDied(w, r.id);
  assert.equal(w.bosses.encounters[0].deadAt, w.tick); assert.equal(w.bosses.features.length, 0); assert.equal(w.bosses.effects.length, 4);
  finishEffects(w, engine); assert.ok(engine.hits.some(h => h.ref.kind === 'person' && h.ref.id === ally.id)); assert.ok(engine.hits.some(h => h.ref.kind === 'raider' && h.ref.id === enemy.id)); assert.equal(w.bosses.effects.length, 0); assert.equal(w.bosses.encounters.length, 0);
});

test('exhausted Sluice counters release normal pursuit instead of oscillating back to center', () => {
  const { w, r, e } = fixture('sluice'); e.arrived = true; e.drained = [true, true]; r.x -= 5;
  const engine = api(w); assert.equal(bossTurn(w, r, engine), false); assert.equal(engine.moves.length, 0);
});
