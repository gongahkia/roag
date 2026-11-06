import test from 'node:test';
import assert from 'node:assert/strict';
import { createWorld } from '../src/simulation/world.ts';
import { enqueue, step } from '../src/simulation/step.ts';
import { canonical } from '../src/simulation/canonical.ts';
import { index, neighbors, walkable } from '../src/simulation/model.ts';
import { makeSave, encodeSave, parseSave } from '../src/persistence/schema.ts';

const controls = { manualPaused: true, speed: 1 };
const saved = world => encodeSave(makeSave(world, controls));
function rejects(world, cases) {
  const original = saved(world);
  for (const [label, mutate] of cases) {
    const altered = JSON.parse(original); mutate(altered.world, altered);
    assert.throws(() => parseSave(JSON.stringify(altered)), /^Error: Invalid save:/, label);
    assert.equal(saved(world), original, `${label} does not alter the live world`);
  }
}
function continueExactly(world, ticks = 20) {
  const restored = parseSave(saved(world)).world;
  for (let i = 0; i < ticks; i++) {
    step(world); step(restored);
    assert.equal(saved(restored), saved(world), `exact saved continuation at tick ${world.tick}`);
  }
  assert.equal(canonical(restored), canonical(world));
}

test('schema 6 retains bounded biome cells and actual ground edits without regenerating them', () => {
  for (const [seed, biome] of [[42, 'woodland'], [43, 'marsh'], [44, 'ash']]) {
    const world = createWorld(seed), encoded = saved(world), save = JSON.parse(encoded);
    assert.equal(save.version, 6); assert.equal(save.contentVersion, 'colony-p07-1');
    assert.equal(world.environment.biome, biome);
    assert.equal(encodeSave(parseSave(encoded)), encoded);
    rejects(world, [
      ['previous schema', (_w, s) => s.version = 5],
      ['previous content', (_w, s) => s.contentVersion = 'colony-p06-1'],
      ['unknown environment field', w => w.environment.fluid = []],
      ['wrong seeded biome', w => w.environment.biome = 'ocean'],
      ['null environment', w => w.environment = null],
      ['null wet collection', w => w.environment.wet = null],
      ['oversized wet collection', w => w.environment.wet = Array(129).fill({ x: 40, y: 40 })],
      ['unknown world field', w => w.weather = {}],
    ]);
    if (biome === 'woodland') {
      rejects(world, [['woodland wet cells', w => w.environment.wet.push({ x: 40, y: 40 })]]);
      continue;
    }
    const field = biome === 'marsh' ? 'wet' : 'ash';
    assert.equal(world.environment[field].length, 128);
    rejects(world, [
      ['duplicate ground cell', w => w.environment[field][1] = w.environment[field][0]],
      ['non-cell ground entry', w => w.environment[field][0] = null],
      ['extra cell fields', w => w.environment[field][0].depth = 2],
      ['fractional cell', w => w.environment[field][0].x += 0.5],
      ['protected heart area', w => w.environment[field][0] = { x: 64, y: 64 }],
      ['outside patch radius', w => w.environment[field][0] = { x: 2, y: 2 }],
      ['wrong biome collection', w => w.environment[field === 'wet' ? 'ash' : 'wet'].push(w.environment[field][0])],
    ]);
    const originalGround = structuredClone(world.environment), cell = world.environment[field][0];
    world.terrain[index(cell.x, cell.y)] = 2; world.terrainVersion++;
    const restored = parseSave(saved(world)).world;
    assert.deepEqual(restored.environment, originalGround, 'saved patch is not repainted from current accessibility');
    assert.equal(restored.terrain[index(cell.x, cell.y)], 2);
  }
});

test('a physically started shallow-water edge retains its committed duration after terrain cost changes', () => {
  const world = createWorld(43, { scenario: 'work-lab' });
  const target = world.environment.wet.find(cell => neighbors(cell).some(n => walkable(world, n)));
  assert.ok(target);
  const origin = neighbors(target).find(cell => walkable(world, cell));
  // A focused movement fixture places one founder beside generated water. All
  // edge acquisition/progress and subsequent continuation use the real engine.
  const person = world.people[0]; person.x = origin.x; person.y = origin.y;
  assert.ok(enqueue(world, { kind: 'move', actor: person.id, target }));
  for (let i = 0; i < 5 && !person.next; i++) step(world);
  assert.deepEqual(person.next, target); assert.equal(person.moveTicks, 6);
  world.environment.wet = world.environment.wet.filter(cell => cell.x !== target.x || cell.y !== target.y);
  const restored = parseSave(saved(world)).world;
  assert.equal(restored.people[0].moveTicks, 6, 'duration is not recomputed from current terrain');
  rejects(world, [
    ['missing committed duration', w => delete w.people[0].moveTicks],
    ['nonstandard duration', w => w.people[0].moveTicks = 5],
    ['string duration', w => w.people[0].moveTicks = '6'],
    ['completed progress still reserved', w => w.people[0].progress = w.people[0].moveTicks],
    ['duration without edge', w => { w.people[0].next = null; w.people[0].progress = 0; }],
  ]);
  continueExactly(world);
  assert.equal(person.next, null); assert.equal(person.progress, 0); assert.equal(person.moveTicks, 4);
});

// Isolated fixtures finish three small waves through real combat before placing
// a boss beside its generated layout. They are not ordinary balance evidence.
async function bossFixture(kind) {
  const { addRaider } = await import('./helpers/combat.mjs');
  const { prepareBossSpawn } = await import('../src/simulation/bosses.ts');
  const world = createWorld(42, { scenario: 'work-lab' });
  for (let wave = 1; wave <= 3; wave++) {
    addRaider(world, 62, 64);
    assert.ok(enqueue(world, { kind: 'battle', actor: 0, mode: 'rally', target: { x: 62, y: 64 } }));
    for (let tick = 0; tick < 400 && world.combat.phase !== 'recovery'; tick++) step(world);
    assert.equal(world.combat.phase, 'recovery');
  }
  assert.ok(enqueue(world, { kind: 'battle', actor: 0, mode: 'release', target: { x: 62, y: 64 } })); step(world);
  const boss = addRaider(world, 80, 64, kind);
  assert.ok(prepareBossSpawn(world, { id: boss.id, kind, squadId: null, leaderId: null, edge: 'east', entryOffset: 0 }));
  const encounter = world.bosses.encounters[0];
  boss.x = encounter.layout.center.x; boss.y = encounter.layout.center.y;
  const person = world.people[0]; person.x = boss.x - 3; person.y = boss.y;
  assert.ok(walkable(world, person)); saved(world);
  return { world, boss, encounter, person };
}
function advanceValid(world, count = 1) {
  for (let tick = 0; tick < count; tick++) { step(world); saved(world); }
}
function untilValid(world, predicate, label, maximum = 250) {
  for (let tick = 0; tick < maximum && !predicate(); tick++) advanceValid(world);
  assert.ok(predicate(), `${label} at tick ${world.tick}`);
}

test('real Bellkeeper, Sluice-Maw and Threadmother telegraphs round-trip with exact continuation', async () => {
  for (const kind of ['bellkeeper', 'sluice', 'threadmother']) {
    const { world, encounter } = await bossFixture(kind);
    untilValid(world, () => world.bosses.effects.length > 0, `${kind} first committed effect`);
    const checkpoint = structuredClone(world);
    rejects(world, [
      ['null boss state', w => w.bosses = null],
      ['unknown boss field', w => w.bosses.guess = 1],
      ['malformed encounter', w => w.bosses.encounters[0] = null],
      ['null layout channels', w => w.bosses.encounters[0].layout.channels = null],
      ['missing layout geometry', w => w.bosses.encounters[0].layout.webs = null],
      ['wrong layout center', w => w.bosses.encounters[0].layout.center.x++],
      ['unknown boss kind', w => w.bosses.encounters[0].kind = 'dragon'],
      ['wrong owner', w => w.bosses.encounters[0].owner = w.people[0].id],
      ['invented dead encounter', w => w.bosses.encounters[0].deadAt = w.tick],
      ['duplicate feature ID', w => w.bosses.features[0].id = w.resources[0].id],
      ['duplicate feature cell', w => { w.bosses.features[1].x = w.bosses.features[0].x; w.bosses.features[1].y = w.bosses.features[0].y; }],
      ['effect ID alias', w => w.bosses.effects[0].id = w.bosses.features[0].id],
      ['expired effect', w => w.bosses.effects[0].due = w.tick],
      ['future effect', w => w.bosses.effects[0].due = w.tick + 201],
      ['wrong effect damage', w => w.bosses.effects[0].damage++],
      ['wrong effect source', w => w.bosses.effects[0].source = w.people[0].id],
      ['null effect cells', w => w.bosses.effects[0].cells = null],
      ['empty effect cells', w => w.bosses.effects[0].cells = []],
      ['effect cell extra field', w => w.bosses.effects[0].cells[0].owner = 1],
      ['duplicate effect cell', w => w.bosses.effects[0].cells.push(w.bosses.effects[0].cells[0])],
    ]);
    continueExactly(world, 80);
    assert.equal(checkpoint.bosses.encounters[0].layout.kind, encounter.kind);
  }
});

test('physical strike, drain, cut and ignite work resumes exactly without accepting forged claims', async () => {
  for (const [kind, mode, targetKind] of [
    ['bellkeeper', 'strike', 'resonator'], ['sluice', 'drain', 'valve'],
    ['threadmother', 'cut', 'web'], ['threadmother', 'ignite', 'anchor'],
  ]) {
    const { world, person } = await bossFixture(kind);
    const feature = world.bosses.features.find(value => value.kind === targetKind);
    const at = neighbors(feature).find(cell => walkable(world, cell) && !world.people.some(p => p.x === cell.x && p.y === cell.y) && !world.combat.raiders.some(r => r.x === cell.x && r.y === cell.y));
    assert.ok(at); person.x = at.x; person.y = at.y;
    assert.ok(enqueue(world, { kind: 'battle', actor: person.id, mode, target: { x: feature.x, y: feature.y } }));
    untilValid(world, () => world.bosses.work.some(work => work.actor === person.id && work.progress >= 3), `${mode} work began`);
    rejects(world, [
      ['unknown work field', w => w.bosses.work[0].parallel = true],
      ['duplicate worker', w => w.bosses.work.push(w.bosses.work[0])],
      ['wrong actor', w => w.bosses.work[0].actor = 99],
      ['missing target', w => w.bosses.work[0].target = w.resources[0].id],
      ['wrong mode', w => w.bosses.work[0].mode = 'rally'],
      ['zero progress claim', w => w.bosses.work[0].progress = 0],
      ['completed work still active', w => w.bosses.work[0].progress = 24],
      ['future work start', w => w.bosses.work[0].startedAt = w.tick + 1],
      ['progress before start', w => w.bosses.work[0].startedAt = w.tick],
      ['missing order', w => w.combat.orders = []],
      ['disagreed order target', w => w.combat.orders.find(order => order.actor === person.id).target.x++],
    ]);
    continueExactly(world, 45);
    assert.ok(!world.bosses.work.some(work => work.actor === person.id));
    if (mode === 'drain') assert.equal(world.bosses.encounters[0].drained[feature.channel], true);
    else assert.ok(!world.bosses.features.some(value => value.id === feature.id));
  }
});

test('locked fire survives historical feature, boss and founder death then expires exactly once', async () => {
  const { die } = await import('../src/simulation/needs.ts');
  const { clearTravel } = await import('../src/simulation/jobs.ts');
  const { world, boss, person } = await bossFixture('threadmother');
  const anchor = world.bosses.features.find(value => value.kind === 'anchor');
  const at = neighbors(anchor).find(cell => walkable(world, cell)); person.x = at.x; person.y = at.y;
  assert.ok(enqueue(world, { kind: 'battle', actor: person.id, mode: 'ignite', target: { x: anchor.x, y: anchor.y } }));
  untilValid(world, () => world.bosses.effects.some(effect => effect.kind === 'fire'), 'physical ignition completed');
  const fireCount = world.bosses.effects.filter(effect => effect.kind === 'fire').length; assert.ok(fireCount > 1);
  // A low-health adjacent boss is an explicit death-cleanup fixture. The kill
  // itself, reward and historical cleanup use the real guard/combat engine.
  const beside = neighbors(person).find(cell => walkable(world, cell) && !world.people.some(p => p.x === cell.x && p.y === cell.y));
  boss.x = beside.x; boss.y = beside.y; boss.health = 1; boss.next = null; boss.progress = 0; boss.moveTicks = 4; boss.route = [];
  assert.ok(enqueue(world, { kind: 'battle', actor: person.id, mode: 'rally', target: { x: boss.x, y: boss.y } }));
  untilValid(world, () => !world.combat.raiders.some(r => r.id === boss.id), 'real guard killed burning-web owner', 6);
  assert.ok(world.bosses.effects.some(effect => effect.kind === 'fire'));
  assert.equal(world.bosses.features.length, 0);
  assert.equal(world.bosses.encounters[0].deadAt, world.tick);
  die(world, person); clearTravel(person); saved(world);
  assert.ok(world.bosses.effects.every(effect => effect.kind === 'fire' && effect.source === person.id));
  rejects(world, [
    ['missing historical encounter', w => w.bosses.encounters = []],
    ['owner death in future', w => w.bosses.encounters[0].deadAt = w.tick + 1],
    ['dead encounter claiming live state', w => w.bosses.encounters[0].deadAt = null],
    ['fire source is boss', w => w.bosses.effects[0].source = boss.id],
    ['historical feature aliases living object', w => w.bosses.effects[0].feature = w.resources[0].id],
    ['historical feature beyond effect', w => w.bosses.effects[0].feature = w.bosses.effects[0].id],
    ['historical group aliases feature', w => w.bosses.effects[0].group = w.bosses.effects[0].feature],
    ['historical origin outside saved layout', w => { const effect = w.bosses.effects[0]; effect.origin.x++; effect.cells = [effect.origin]; }],
    ['fire expanded beyond locked cross', w => w.bosses.effects[0].cells.push({ x: 0, y: 0 })],
  ]);
  continueExactly(world, 60);
  assert.equal(world.bosses.encounters.length, 0); assert.equal(world.bosses.effects.length, 0);
  assert.equal(world.combat.stats.bosses, 1); assert.equal(world.combat.stats.kills, 4); assert.equal(world.combat.lootWood, 8);
});
