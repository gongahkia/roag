import test from 'node:test';
import assert from 'node:assert/strict';
import { createWorld } from '../src/simulation/world.ts';
import { enqueue, step } from '../src/simulation/step.ts';
import { canonical } from '../src/simulation/canonical.ts';
import { distance } from '../src/simulation/model.ts';
import { materialAudit } from '../src/simulation/jobs.ts';
import { makeSave, encodeSave, parseSave } from '../src/persistence/schema.ts';
import { addRaider } from './helpers/combat.mjs';

const controls = { manualPaused: true, speed: 4 };
const save = world => makeSave(world, controls);
function check(world) {
  save(world);
  for (const [material, count] of Object.entries(materialAudit(world)))
    assert.equal(count.produced, count.accounted, `${material} conservation`);
}
function advance(world, count = 1) {
  for (let i = 0; i < count; i++) { step(world); check(world); }
}
function until(world, predicate, label, limit = 400) {
  for (let i = 0; i < limit && !predicate(); i++) advance(world);
  assert.ok(predicate(), `${label} at tick ${world.tick}`);
}
function continuation(world, ticks) {
  const encoded = encodeSave(save(world)), restored = parseSave(encoded).world;
  assert.equal(encodeSave(save(restored)), encoded, 'exact immediate JSON round-trip');
  for (let i = 0; i < ticks; i++) { advance(world); advance(restored); }
  assert.equal(canonical(restored), canonical(world), 'exact simulation continuation');
  return world;
}
function rejects(world, cases) {
  const original = encodeSave(save(world));
  for (const [label, change] of cases) {
    const altered = JSON.parse(original); change(altered.world, altered);
    assert.throws(() => parseSave(JSON.stringify(altered)), /Invalid save/, label);
    assert.equal(encodeSave(save(world)), original, `${label} leaves current state intact`);
  }
}
function order(world, input) { assert.ok(enqueue(world, input)); }

// Small combat fixtures use the real engine and physical guards. Prior summaries
// are produced by actual deaths/completion, never inserted or marked complete.
function completedWaves(count) {
  const world = createWorld(42, { scenario: 'work-lab' });
  for (let wave = 1; wave <= count; wave++) {
    addRaider(world, 62, 64);
    order(world, { kind: 'battle', actor: 0, mode: 'rally', target: { x: 62, y: 64 } });
    until(world, () => world.combat.phase === 'recovery', `wave ${wave} completed`);
  }
  return world;
}
const threeWaves = completedWaves(3);
function captainCheckpoints() {
  const world = structuredClone(threeWaves);
  order(world, { kind: 'move', actor: 2, target: { x: 60, y: 61 } });
  advance(world, 40);
  const captain = addRaider(world, 60, 60, 'captain'), checkpoints = {};
  until(world, () => {
    for (const kind of ['charge', 'rampart']) if (!checkpoints[kind] && world.combat.attacks.some(a => a.kind === kind)) checkpoints[kind] = structuredClone(world);
    if (!checkpoints.moving && captain.charge?.hits.length) checkpoints.moving = structuredClone(world);
    if (!checkpoints.walls && world.combat.barricades.length) checkpoints.walls = structuredClone(world);
    return Boolean(checkpoints.walls);
  }, 'Captain telegraphs, charge and ramparts');
  return checkpoints;
}
const captain = captainCheckpoints();

test('announced FIFO ticket identities and raids RNG survive warning JSON and exact spawning', () => {
  const world = createWorld(42);
  world.combat.warningAt = 1; world.combat.spawnAt = 20;
  advance(world);
  const committed = structuredClone(world.combat.pending), rng = world.rng.raids;
  assert.equal(world.combat.phase, 'warning'); assert.equal(committed.length, 5);
  assert.equal(world.combat.edge, 'east');
  rejects(world, [
    ['old schema', (_w, s) => s.version = 4],
    ['old content', (_w, s) => s.contentVersion = 'colony-p05-1'],
    ['missing ticket metadata', w => delete w.combat.pending[0].entryOffset],
    ['unknown ticket role', w => w.combat.pending[0].kind = 'dragon'],
    ['duplicate global ID', w => w.combat.pending[0].id = w.resources[0].id],
    ['duplicate ticket ID', w => w.combat.pending[1].id = w.combat.pending[0].id],
    ['future ticket ID', w => w.combat.pending[0].id = w.nextId],
    ['reordered FIFO', w => w.combat.pending.reverse()],
    ['mismatched entry edge', w => w.combat.pending[0].edge = 'west'],
    ['invalid offset', w => w.combat.pending[0].entryOffset = 63],
    ['partial group', w => w.combat.pending[0].squadId = w.combat.pending[0].id],
    ['future leader', w => { const t = w.combat.pending[0]; t.squadId = t.leaderId = t.id + 1; }],
    ['wrong expenditure', w => w.combat.spent--],
    ['unbounded budget', w => w.combat.budget = 999],
    ['inverted deadlines', w => w.combat.recoveryUntil = w.combat.spawnAt + 1],
    ['impossible wave baseline', w => w.combat.waveStart.spawned++],
    ['premature completion', w => w.combat.completed++],
    ['queue overload', w => w.combat.pending = Array(151).fill(w.combat.pending[0])],
    ['malformed barricade', w => w.combat.barricades = [null]],
    ['malformed raider', w => w.combat.raiders = [null]],
  ]);
  continuation(world, 45);
  assert.equal(world.rng.raids, rng, 'spawning does not reroll commitments');
  assert.deepEqual(world.combat.raiders.map(r => r.id), committed.map(t => t.id));
  assert.equal(world.combat.pending.length, 0);
});

test('support recipient commitments and archer lanes continue exactly from real windups', () => {
  const support = createWorld(42, { scenario: 'work-lab' });
  const healer = addRaider(support, 68, 60, 'healer'), rusher = addRaider(support, 70, 60, 'rusher');
  healer.squadId = healer.leaderId = healer.id;
  rusher.squadId = rusher.leaderId = healer.id; rusher.health = 7;
  advance(support);
  const healing = support.combat.attacks.find(a => a.kind === 'heal');
  assert.equal(healing.recipient, rusher.id);
  rejects(support, [
    ['missing patient', w => w.combat.attacks[0].recipient = null],
    ['self heal', w => w.combat.attacks[0].recipient = healer.id],
    ['colony patient', w => w.combat.attacks[0].recipient = 2],
    ['future patient', w => w.combat.attacks[0].recipient = w.nextId],
    ['nonzero heal damage', w => w.combat.attacks[0].damage = 16],
    ['source role substitution', w => w.combat.raiders[0].kind = 'rusher'],
    ['squad substitution', w => w.combat.raiders[1].squadId = null],
    ['altered source cooldown', w => w.combat.raiders[0].cooldown--],
    ['non-Captain charge', w => w.combat.raiders[0].charge = { cells: [{ x: 69, y: 60 }], cursor: 0, hits: [] }],
  ]);
  continuation(support, 14);
  assert.equal(support.combat.raiders.find(r => r.id === rusher.id).health, 23, 'real heal applies once');

  const archer = createWorld(42, { scenario: 'work-lab' });
  addRaider(archer, 68, 62, 'archer');
  until(archer, () => archer.combat.attacks.some(a => a.kind === 'arrow'), 'actual cardinal arrow');
  rejects(archer, [
    ['redirected arrow', w => w.combat.attacks[0].target.x++],
    ['skipped arrow cell', w => w.combat.attacks[0].cells.shift()],
    ['healing arrow', w => w.combat.attacks[0].recipient = w.combat.raiders[0].id],
    ['overlong cooldown', w => w.combat.raiders[0].cooldown += 100],
  ]);
  continuation(archer, 35);
});

test('Captain charge and rampart telegraphs, mid-charge hit history and live walls round-trip every tick', () => {
  for (const checkpoint of Object.values(captain)) continuation(structuredClone(checkpoint), 50);
  const moving = captain.moving.combat.raiders.find(r => r.kind === 'captain');
  assert.equal(moving.charge.cursor, 1); assert.deepEqual(moving.charge.hits, [2]);
  rejects(captain.moving, [
    ['charge cursor skips cells', w => w.combat.raiders[0].charge.cursor++],
    ['charge direction changed', w => w.combat.raiders[0].facing = 'north'],
    ['charge path changed', w => w.combat.raiders[0].charge.cells[2].x++],
    ['duplicate hit history', w => w.combat.raiders[0].charge.hits.push(2)],
    ['future hit identity', w => w.combat.raiders[0].charge.hits.push(w.nextId)],
    ['charge sequence changed', w => w.combat.raiders[0].specialCount++],
    ['unbounded exposure', w => w.combat.raiders[0].exposedUntil = w.tick + 31],
  ]);
  for (const kind of ['charge', 'rampart']) rejects(captain[kind], [
    ['altered locked cells', w => w.combat.attacks[0].cells[0].x++],
    ['altered locked target', w => w.combat.attacks[0].target.y++],
    ['altered commitment sequence', w => w.combat.raiders[0].specialCount++],
    ['altered special damage', w => w.combat.attacks[0].damage++],
    ['teleported origin', w => w.combat.attacks[0].origin.x++],
  ]);
  rejects(captain.walls, [
    ['missing wall owner', w => w.combat.barricades[0].owner = 2],
    ['duplicate wall identity', w => w.combat.barricades[0].id = w.combat.raiders[0].id],
    ['overlapping walls', w => Object.assign(w.combat.barricades[1], { x: w.combat.barricades[0].x, y: w.combat.barricades[0].y })],
    ['wall on founder', w => Object.assign(w.combat.barricades[0], { x: w.people[0].x, y: w.people[0].y })],
    ['expired wall', w => w.combat.barricades[0].expiresAt = w.tick],
    ['immortal wall', w => w.combat.barricades[0].expiresAt = w.tick + 601],
    ['dead wall retained', w => w.combat.barricades[0].health = 0],
    ['too many walls', w => w.combat.barricades = Array(9).fill(w.combat.barricades[0])],
  ]);
});

test('shoved guards retain their original strike and reload through the Captain death and five-wood reward', () => {
  const world = structuredClone(captain.walls);
  order(world, { kind: 'battle', actor: 0, mode: 'rally', target: { x: 60, y: 64 } });
  let displaced = null;
  until(world, () => {
    if (!displaced && world.combat.attacks.some(a => a.source.kind === 'person' && distance(a.origin, world.people.find(p => p.id === a.source.id)) > 1)) displaced = structuredClone(world);
    return world.combat.phase === 'recovery';
  }, 'Captain killed by physical guards');
  assert.ok(displaced, 'regression reaches guard displaced twice during pending strike');
  continuation(displaced, 100);
  assert.equal(displaced.combat.stats.bosses, 1);
  assert.equal(world.combat.stats.kills, 4); assert.equal(world.combat.lootWood, 8);
  assert.equal(world.produced.wood, 8); assert.equal(world.combat.barricades.length, 0);
  assert.equal(world.combat.history.at(-1).wave, 4);
  rejects(world, [
    ['duplicate boss reward', w => w.combat.lootWood += 5],
    ['unrecorded boss', w => w.combat.stats.bosses = 0],
    ['invented boss', w => w.combat.stats.bosses++],
    ['missing completed summary', w => w.combat.history.pop()],
    ['altered completed kills', w => w.combat.history.at(-1).kills++],
    ['unearned ammunition summary', w => w.combat.history.at(-1).ammoSpent++],
    ['unearned repair summary', w => w.combat.history.at(-1).repairs++],
  ]);
});

test('clear-barricade orders and frozen terminal commitments preserve exact JSON state', () => {
  const world = structuredClone(captain.walls), wall = world.combat.barricades[0];
  order(world, { kind: 'battle', actor: 0, mode: 'clear', target: { x: wall.x, y: wall.y } });
  assert.equal(save(world).world.pending[0].input.mode, 'clear');
  continuation(world, 100);
  assert.ok(!world.combat.barricades.some(b => b.id === wall.id), 'guards physically remove designated wall');
  const terminal = structuredClone(captain.charge);
  terminal.combat.stats.heartDamage += terminal.heart.health; terminal.heart.health = 0; terminal.terminal = 'heart-destroyed';
  const before = encodeSave(save(terminal));
  continuation(terminal, 20); assert.equal(encodeSave(save(terminal)), before);
});

test('twenty real fixture completions retain sixteen ordered summaries and lifetime totals across reload', () => {
  const world = completedWaves(20);
  assert.equal(world.combat.completed, 20); assert.equal(world.combat.history.length, 16);
  assert.deepEqual(world.combat.history.map(s => s.wave), Array.from({ length: 16 }, (_, i) => i + 5));
  assert.equal(world.combat.stats.kills, 20); assert.equal(world.combat.lootWood, 20);
  rejects(world, [
    ['unbounded history', w => w.combat.history.push(w.combat.history.at(-1))],
    ['history gap', w => w.combat.history[0].wave--],
    ['unordered chronology', w => w.combat.history[1].startedAt = 0],
    ['summary extends into future', w => w.combat.history.at(-1).endedAt = w.tick + 1],
    ['forged lifetime expenditure', w => w.combat.history[0].structuresLost = 999],
    ['wrong next wave baseline', w => w.combat.waveStart.kills--],
  ]);
  continuation(world, 10);
});
