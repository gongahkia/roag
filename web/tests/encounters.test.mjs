import test from 'node:test';
import assert from 'node:assert/strict';
import { createWorld } from '../src/simulation/world.ts';
import { ENCOUNTER_CONTENT as E } from '../src/simulation/encounter-content.ts';
import { encounterTurn, advanceEncounters, resolveEncounterImpact, defendedDamage } from '../src/simulation/encounters.ts';
import { chargeCells, rampartCells, heartReachable, inFront, lineReaches } from '../src/simulation/encounter-geometry.ts';
import { index, spatial, walkable, same } from '../src/simulation/model.ts';
import { shotCells } from '../src/simulation/combat-geometry.ts';
import { emptyStock, WORK_CONTENT } from '../src/simulation/work-content.ts';
import { step, enqueue } from '../src/simulation/step.ts';
import { advanceCombat } from '../src/simulation/combat.ts';

// Deliberately isolated encounter fixtures, never represented as ordinary colony balance.
function fixture() {
  const w = createWorld(42, { scenario: 'work-lab' });
  w.combat.barricades ??= []; w.combat.phase = 'combat'; w.combat.wave = 3;
  for (let y = 45; y <= 85; y++) for (let x = 45; x <= 85; x++) w.terrain[index(x, y)] = 1;
  w.people.forEach((p, i) => { p.x = 30 + i * 2; p.y = 30; for (const duty of Object.keys(p.body.duties)) p.body.duties[duty] = 0; });
  w.resources = w.resources.filter(r => r.x < 45 || r.x > 85 || r.y < 45 || r.y > 85);
  return w;
}
function actor(w, kind, x = 60, y = 60, options = {}) {
  const r = { id: w.nextId++, kind, x, y, health: E.profiles[kind].health, facing: 'east', squadId: 1, leaderId: null, loweredUntil: 0, exposedUntil: 0, specialCount: 0, charge: null,
    next: null, progress: 0, route: [], routeVersion: w.terrainVersion, retryAt: w.tick, cooldown: 0, target: null, ...options };
  w.combat.raiders.push(r); return r;
}
function wall(w, x, y) {
  const s = { id: w.nextId++, kind: 'wall', x, y, health: 100, open: false, inventory: emptyStock(), embodied: { ...WORK_CONTENT.buildings.wall.cost } };
  w.structures.push(s); w.terrainVersion++; return s;
}
function api(w) {
  const result = {
    moves: [], hits: [],
    launch(source, origin, target, kind, faction, damage, delay, extra = {}) {
      w.combat.attacks.push({ id: w.nextId++, source: { ...source }, origin: { x: origin.x, y: origin.y }, target: { x: target.x, y: target.y }, kind, faction, damage, due: w.tick + delay, recipient: extra.recipient ?? null,
        cells: extra.cells?.map(c => ({ ...c })) ?? (['arrow', 'heal'].includes(kind) ? shotCells(w, origin, target) : [{ x: target.x, y: target.y }]) }); return true;
    },
    damage(ref, amount, faction, origin) {
      result.hits.push({ ref: { ...ref }, amount, faction, origin: { ...origin } });
      if (ref.kind === 'person') w.people.find(p => p.id === ref.id).body.health -= amount;
      if (ref.kind === 'structure') w.structures.find(s => s.id === ref.id).health -= amount;
      if (ref.kind === 'heart') w.heart.health -= amount;
    },
    move(r, target, adjacent) { result.moves.push({ actor: r.id, target: { x: target.x, y: target.y }, adjacent }); },
    shove(id, delta) {
      const p = w.people.find(p => p.id === id), target = { x: p.x + delta.x, y: p.y + delta.y };
      if (p.life !== 'alive' || p.next || !walkable(w, target) || spatial(w).has(index(target.x, target.y))) return false;
      p.x = target.x; p.y = target.y; p.route = []; p.destination = null; return true;
    },
  }; return result;
}
function impact(w, attack, engine) { w.tick = attack.due; w.combat.attacks = w.combat.attacks.filter(a => a.id !== attack.id); resolveEncounterImpact(w, attack, engine); }

test('shield facing has a 90-degree front wedge, opens while striking, and recovers', () => {
  const w = fixture(), r = actor(w, 'shield'), engine = api(w), p = w.people[0]; p.x = 61; p.y = 60;
  assert.equal(inFront(r, 'east', { x: 62, y: 62 }), true);
  assert.equal(defendedDamage(w, r, 20, { x: 62, y: 60 }), 4);
  assert.equal(defendedDamage(w, r, 20, { x: 60, y: 62 }), 20);
  assert.equal(defendedDamage(w, r, 20, { x: 58, y: 60 }), 20);
  assert.equal(encounterTurn(w, r, engine), true); assert.equal(w.combat.attacks[0].kind, 'melee');
  assert.equal(defendedDamage(w, r, 20, p), 20);
  w.tick = r.loweredUntil; assert.equal(defendedDamage(w, r, 20, p), 4);
});

test('archers require cardinal range and a clear first-body lane, then request shared-budget reposition', () => {
  const w = fixture(), r = actor(w, 'archer'), p = w.people[0], engine = api(w); p.x = 66; p.y = 60;
  encounterTurn(w, r, engine); assert.equal(w.combat.attacks[0].kind, 'arrow'); assert.equal(w.combat.attacks[0].damage, E.profiles.archer.damage);
  w.combat.attacks = []; r.cooldown = 0; const shield = actor(w, 'shield', 63, 60);
  assert.equal(lineReaches(w, r, p, { kind: 'person', id: p.id }), false);
  encounterTurn(w, r, engine); assert.equal(w.combat.attacks.length, 0); assert.equal(engine.moves.length, 1);
  w.combat.raiders = w.combat.raiders.filter(a => a !== shield); p.x = 61;
  encounterTurn(w, r, engine); assert.equal(w.combat.attacks.length, 0, 'minimum firing distance is two');
});

test('healer locks lowest-health fraction and identity; movement, new walls and death invalidate healing', () => {
  for (const change of ['none', 'move', 'wall', 'death', 'source-death']) {
    const w = fixture(), healer = actor(w, 'healer'), rusher = actor(w, 'rusher', 62, 60, { health: 7 }), shield = actor(w, 'shield', 60, 63, { health: 30 }), engine = api(w);
    encounterTurn(w, healer, engine); const a = w.combat.attacks[0]; assert.equal(a.recipient, rusher.id);
    if (change === 'move') rusher.y++;
    if (change === 'wall') wall(w, 61, 60);
    if (change === 'death') w.combat.raiders = w.combat.raiders.filter(r => r !== rusher);
    if (change === 'source-death') w.combat.raiders = w.combat.raiders.filter(r => r !== healer);
    impact(w, a, engine); assert.equal(rusher.health, change === 'none' ? 23 : 7, change); assert.equal(shield.health, 30);
  }
});

test('wounded rushers withdraw toward their living healer; removing support restores aggression', () => {
  const w = fixture(), healer = actor(w, 'healer', 55, 60), r = actor(w, 'rusher', 60, 60, { health: 10, leaderId: healer.id }), engine = api(w);
  assert.equal(encounterTurn(w, r, engine), true); assert.equal(engine.moves[0].actor, r.id); assert.deepEqual(r.target, { kind: 'raider', id: healer.id });
  w.combat.raiders = w.combat.raiders.filter(a => a !== healer); assert.equal(encounterTurn(w, r, engine), false);
});

test('Captain turning, windup and post-charge exposure permit counters; specials alternate', () => {
  const w = fixture(), r = actor(w, 'captain', 60, 60), p = w.people[0], engine = api(w); p.x = 60; p.y = 64;
  assert.equal(defendedDamage(w, r, 20, { x: 62, y: 60 }), 4);
  encounterTurn(w, r, engine); const a = w.combat.attacks[0]; assert.equal(a.kind, 'charge'); assert.equal(r.facing, 'south'); assert.equal(defendedDamage(w, r, 20, p), 30);
  assert.deepEqual(a.cells, chargeCells(r, 'south'));
  impact(w, a, engine); for (let i = 0; i < 4; i++) { w.tick++; advanceEncounters(w, engine); }
  assert.equal(r.charge, null); assert.ok(r.exposedUntil > w.tick);
  w.tick = r.cooldown; encounterTurn(w, r, engine); assert.equal(w.combat.attacks[0].kind, 'rampart'); assert.equal(r.specialCount, 2);
});

test('Captain charge rechecks a wall built after telegraph and never jumps through it', () => {
  const w = fixture(), r = actor(w, 'captain'), p = w.people[0], engine = api(w); p.x = 65; p.y = 60;
  encounterTurn(w, r, engine); const a = w.combat.attacks[0], barrier = wall(w, 62, 60); impact(w, a, engine);
  w.tick++; advanceEncounters(w, engine); assert.equal(r.x, 61);
  w.tick++; advanceEncounters(w, engine); assert.equal(r.x, 61); assert.equal(r.charge, null); assert.equal(barrier.health, 80);
  assert.equal(engine.hits.length, 1); assert.equal(p.body.health, 100);
});

test('charge hits each identity once and moves at most one cell per tick with legal shoves', () => {
  const w = fixture(), r = actor(w, 'captain'), p = w.people[0], engine = api(w); p.x = 61; p.y = 60;
  encounterTurn(w, r, engine); impact(w, w.combat.attacks[0], engine);
  for (let n = 0; n < 4; n++) { const before = r.x; w.tick++; advanceEncounters(w, engine); assert.ok(r.x - before <= 1); }
  assert.equal(r.x, 64); assert.equal(p.x, 65); assert.equal(p.body.health, 80); assert.equal(engine.hits.length, 1); assert.equal(r.charge, null);
});

test('charge respects a reserved edge or rejected shove, still applying one physical impact', () => {
  const w = fixture(), r = actor(w, 'captain'), p = w.people[0], engine = api(w); p.x = 61; p.y = 60; p.next = { x: 62, y: 60 };
  encounterTurn(w, r, engine); impact(w, w.combat.attacks[0], engine); w.tick++; advanceEncounters(w, engine);
  assert.equal(r.x, 60); assert.equal(p.x, 61); assert.equal(p.body.health, 80); assert.equal(r.charge, null);
});

test('ramparts preserve their center gap, occupied tiles and all previous heart reachability', () => {
  const w = fixture(), r = actor(w, 'captain', 60, 60, { specialCount: 1 }), p = w.people[0], engine = api(w); p.x = 65; p.y = 60;
  const occupied = w.people[1]; occupied.x = 62; occupied.y = 58;
  const before = heartReachable(w); encounterTurn(w, r, engine); const a = w.combat.attacks[0]; assert.deepEqual(a.cells, rampartCells(r, 'east')); impact(w, a, engine);
  assert.equal(w.combat.barricades.length, 3); assert.equal(w.combat.barricades.some(b => same(b, occupied)), false); assert.equal(w.combat.barricades.some(b => same(b, { x: 62, y: 60 })), false);
  const after = heartReachable(w), barricadeKeys = new Set(w.combat.barricades.map(b => index(b.x, b.y)));
  before.forEach((reachable, i) => { if (reachable && !barricadeKeys.has(i)) assert.equal(after[i], 1); });
  w.tick += E.rampart.lifetime; advanceEncounters(w, engine); assert.equal(w.combat.barricades.length, 0);
});

test('a rampart cannot seal the single entrance to an existing heart corridor', () => {
  const w = fixture(), r = actor(w, 'captain', 60, 60, { specialCount: 1 }), engine = api(w);
  // Isolate the heart area behind a solid line whose only opening is a proposed rampart cell.
  for (let y = 0; y < 128; y++) w.terrain[index(62, y)] = 2;
  w.terrain[index(62, 59)] = 1;
  const a = { id: w.nextId++, kind: 'rampart', recipient: null, source: { kind: 'raider', id: r.id }, origin: { x: 60, y: 60 }, target: { x: 62, y: 62 }, cells: rampartCells(r, 'east'), due: w.tick + 20, damage: 0, faction: 'hostile' };
  impact(w, a, engine); assert.equal(w.combat.barricades.some(b => b.x === 62 && b.y === 59), false);
});

test('mid-charge state and hit history continue exactly across a cloned checkpoint', () => {
  const w = fixture(), r = actor(w, 'captain'), p = w.people[0], engine = api(w); p.x = 61; p.y = 60;
  encounterTurn(w, r, engine); impact(w, w.combat.attacks[0], engine); w.tick++; advanceEncounters(w, engine);
  assert.deepEqual(r.charge.hits, [p.id]); assert.equal(r.charge.cursor, 1);
  const restored = structuredClone(w), restoredApi = api(restored);
  for (let n = 0; n < 5; n++) { w.tick++; restored.tick++; advanceEncounters(w, engine); advanceEncounters(restored, restoredApi); }
  assert.deepEqual(restored, w); assert.equal(p.body.health, 80); assert.equal(restored.people[0].body.health, 80);
});

test('real tick ordering prevents charge from moving a colonist twice in one tick', () => {
  const w = fixture(), r = actor(w, 'captain'), p = w.people[0];
  p.x = 61; p.y = 59; p.next = { x: 61, y: 60 }; p.progress = 3; p.destination = { x: 61, y: 60 };
  r.charge = { cells: chargeCells(r, 'east'), cursor: 0, hits: [] };
  step(w);
  assert.deepEqual({ x: p.x, y: p.y }, { x: 61, y: 60 }); assert.equal(p.body.health, 80);
  assert.equal(r.x, 60); assert.equal(r.charge, null); assert.ok(r.exposedUntil > w.tick);
});

test('real charge shove preserves cargo and work intent while clearing travel', () => {
  const w = fixture(), r = actor(w, 'captain'), p = w.people[0]; p.x = 61; p.y = 60; p.cargo.wood = 3;
  r.charge = { cells: chargeCells(r, 'east'), cursor: 0, hits: [] };
  step(w); assert.equal(r.x, 61); assert.equal(p.x, 62); assert.equal(p.body.health, 80); assert.equal(p.cargo.wood, 3);
  assert.equal(p.next, null); assert.deepEqual(p.route, []); assert.equal(p.destination, null);
});

test('clear order physically approaches and strikes a barricade through three paid windups', () => {
  const w = fixture(), owner = actor(w, 'captain', 80, 80), p = w.people[0]; p.x = 61; p.y = 60;
  const b = { id: w.nextId++, owner: owner.id, x: 62, y: 60, health: E.rampart.health, expiresAt: 600 };
  w.combat.barricades.push(b); w.terrainVersion++;
  assert.equal(enqueue(w, { kind: 'battle', actor: p.id, mode: 'clear', target: { x: b.x, y: b.y } }), true);
  step(w); assert.equal(b.health, 36); assert.ok(w.combat.attacks.some(a => a.source.id === p.id && a.due === 4));
  while (w.tick < 4) step(w); assert.equal(b.health, 24);
  while (w.tick < 14) step(w); assert.equal(b.health, 12);
  while (w.tick < 24) step(w); assert.equal(w.combat.barricades.some(other => other.id === b.id), false);
  assert.equal(w.combat.orders.some(o => o.actor === p.id), false); assert.equal(walkable(w, b), true);
});

function finiteTower(w, x, y) {
  const tower = { id: w.nextId++, x, y, kind: 'tower', health: 100, open: false, inventory: emptyStock(), embodied: { ...WORK_CONTENT.buildings.tower.cost } };
  tower.inventory.ammo = 1; w.structures.push(tower); return tower;
}

test('actual Captain death cancels windup and barricades and pays five wood exactly once', () => {
  const w = fixture(), captain = actor(w, 'captain', 60, 60, { health: 1 }), engine = api(w);
  actor(w, 'archer', 80, 80); finiteTower(w, 58, 60);
  const barricade = { id: w.nextId++, owner: captain.id, x: 62, y: 59, health: 36, expiresAt: 600 }; w.combat.barricades.push(barricade);
  engine.launch({ kind: 'raider', id: captain.id }, captain, { x: 64, y: 60 }, 'charge', 'hostile', 20, 20, { cells: chargeCells(captain, 'east') });
  advanceCombat(w); for (let n = 0; n < 30; n++) step(w);
  assert.equal(w.combat.raiders.some(r => r.id === captain.id), false); assert.equal(w.combat.attacks.some(a => a.source.id === captain.id), false); assert.equal(w.combat.barricades.length, 0);
  assert.equal(w.combat.stats.bosses, 1); assert.equal(w.combat.stats.kills, 1); assert.equal(w.combat.lootWood, 5); assert.equal(w.produced.wood, 5);
  assert.equal(w.piles.reduce((sum, p) => sum + p.inventory.wood, 0), 5); assert.equal(w.combat.stats.shots, 1); assert.equal(w.consumed.ammo, 1);
});

test('real tower death during a healer windup cannot resurrect the locked recipient', () => {
  const w = fixture(), healer = actor(w, 'healer'), patient = actor(w, 'rusher', 62, 60, { health: 7, retryAt: 1000 }), engine = api(w);
  patient.target = { kind: 'heart', id: w.heart.id }; finiteTower(w, 62, 58); encounterTurn(w, healer, engine);
  const heal = w.combat.attacks.find(a => a.kind === 'heal'); assert.equal(heal.recipient, patient.id);
  advanceCombat(w); for (let n = 0; n < 20; n++) step(w);
  assert.equal(w.combat.raiders.some(r => r.id === patient.id), false); assert.equal(w.combat.stats.kills, 1); assert.equal(w.combat.lootWood, 1);
  assert.equal(w.combat.attacks.some(a => a.id === heal.id), false);
});
