import test from 'node:test';
import assert from 'node:assert/strict';
import { establish, harness } from './helpers/survival.mjs';
import { createWorld } from '../src/simulation/world.ts';
import { step, enqueue } from '../src/simulation/step.ts';
import { canonical } from '../src/simulation/canonical.ts';
import { materialAudit, destroyStructure } from '../src/simulation/jobs.ts';
import { COMBAT_CONTENT } from '../src/simulation/combat-content.ts';
import { makeSave, encodeSave, parseSave } from '../src/persistence/schema.ts';

const controls = { manualPaused: true, speed: 4 };
const save = world => makeSave(world, controls);
function conserved(world) {
  for (const [material, amount] of Object.entries(materialAudit(world)))
    assert.equal(amount.produced, amount.accounted, `${material} conservation`);
}
function continuation(world, ticks) {
  const copy = parseSave(encodeSave(save(world))).world;
  for (let i = 0; i < ticks; i++) {
    step(world); step(copy); conserved(world); conserved(copy);
    save(world); save(copy);
  }
  assert.equal(canonical(copy), canonical(world));
}
function attacker(world, kind = 'raider', x = 73, y = 62) {
  // Focused combat fixtures supply enemies; colony facilities and ammunition below
  // are made through ordinary resource, production, construction and reload jobs.
  Object.assign(world.combat, { phase: 'combat', wave: 1, warningAt: 0, spawnAt: 0 });
  const raider = { id: world.nextId++, kind, x, y, health: COMBAT_CONTENT[kind].health,
    next: null, progress: 0, route: [], routeVersion: world.terrainVersion,
    retryAt: world.tick, cooldown: 0, target: null };
  world.combat.raiders.push(raider); world.combat.stats.spawned++;
  return raider;
}

const prepared = establish('work-lab');
for (const job of [...prepared.w.jobs].filter(j => j.state === 'active')) prepared.cmd({ kind: 'work', job: job.id, action: 'cancel' });
prepared.cmd({ kind: 'stockpile', target: { x: 58, y: 66 } });
const tower = prepared.build('tower', 68, 62);
const workshop = prepared.w.structures.find(s => s.kind === 'workshop');
prepared.cmd({ kind: 'produce', structure: workshop.id, recipe: 'ammo' });
prepared.until(() => prepared.w.recipeCounts.ammo >= 2, 'two physical ammunition batches');
prepared.cmd({ kind: 'work', job: prepared.w.jobs.find(j => j.state === 'active' && j.recipe === 'ammo').id, action: 'cancel' });
prepared.cmd({ kind: 'reload', structure: tower.id });
const reload = prepared.w.jobs.find(j => j.state === 'active' && j.kind === 'reload');
prepared.until(() => reload.progress > 0, 'ammunition delivered for reload');
const midReload = structuredClone(prepared.w);
prepared.until(() => reload.state === 'done', 'reload completion');
const defenseWorld = structuredClone(prepared.w);

test('schema 4 preserves physical reload supplies and ammunition exactly across continuation', () => {
  const world = structuredClone(midReload);
  assert.equal(world.jobs.find(j => j.id === reload.id).site.ammo, 4);
  assert.equal(world.structures.find(s => s.id === tower.id).inventory.ammo, 0);
  // Natural hunger interrupts this reload at tick 3600; it must resume with the
  // same delivered rounds after eating, without consuming or duplicating them.
  continuation(world, 150);
  assert.equal(world.structures.find(s => s.id === tower.id).inventory.ammo, 4);
  assert.equal(world.consumed.ammo, 0);
  for (const change of [s => s.version = 3, s => s.contentVersion = 'colony-p04-1']) {
    const invalid = save(world); change(invalid); assert.throws(() => parseSave(JSON.stringify(invalid)), /Invalid save/);
  }
});

test('moving attackers and pending shots round-trip every tick through death, loot and recovery', () => {
  const world = structuredClone(defenseWorld);
  attacker(world);
  let windup = null;
  for (let i = 0; i < 300 && world.combat.phase !== 'recovery'; i++) {
    step(world); save(world); conserved(world);
    if (!windup && world.combat.attacks.some(a => a.kind === 'bolt')) windup = structuredClone(world);
  }
  assert.ok(windup, 'real tower shot has a saved windup');
  assert.equal(world.combat.phase, 'recovery');
  assert.equal(world.combat.stats.kills, 1);
  assert.equal(world.combat.lootWood, 1);
  assert.equal(world.combat.stats.shots, 2);
  assert.equal(world.consumed.ammo, 2);
  const restored = parseSave(encodeSave(save(windup))).world;
  continuation(restored, 100);
  assert.equal(restored.combat.stats.kills, 1);
  assert.equal(restored.combat.lootWood, 1);
});

test('malformed combat imports reject geometry, identities, claims, damage, deadlines and ledger forgery', () => {
  const world = structuredClone(defenseWorld); attacker(world); step(world);
  assert.ok(world.combat.attacks.some(a => a.kind === 'bolt'));
  const original = encodeSave(save(world));
  const cases = [
    ['missing combat', s => delete s.world.combat],
    ['unknown fields', s => s.world.combat.unknown = true],
    ['duplicate global ID', s => s.world.combat.raiders[0].id = s.world.structures[0].id],
    ['future global ID', s => s.world.combat.attacks[0].id = s.world.nextId],
    ['dead raider retained', s => s.world.combat.raiders[0].health = 0],
    ['invalid movement', s => s.world.combat.raiders[0].progress = 4],
    ['occupied cell', s => Object.assign(s.world.combat.raiders[0], { x: s.world.people[0].x, y: s.world.people[0].y })],
    ['route jump', s => s.world.combat.raiders[0].route = [{ x: 1, y: 1 }]],
    ['unknown target', s => s.world.combat.raiders[0].target = { kind: 'person', id: 999999 }],
    ['wrong source', s => s.world.combat.attacks[0].source = { kind: 'heart', id: 1 }],
    ['wrong faction', s => s.world.combat.attacks[0].faction = 'hostile'],
    ['wrong attack kind', s => s.world.combat.attacks[0].kind = 'melee'],
    ['forged damage', s => s.world.combat.attacks[0].damage = 999],
    ['expired windup', s => s.world.combat.attacks[0].due = s.world.tick],
    ['unbounded windup', s => s.world.combat.attacks[0].due = s.world.tick + 100],
    ['moved tower origin', s => s.world.combat.attacks[0].origin.x--],
    ['empty attack path', s => s.world.combat.attacks[0].cells = []],
    ['altered attack path', s => s.world.combat.attacks[0].cells[0].y++],
    ['moving locked target', s => s.world.combat.attacks[0].target.y++],
    ['duplicate source windup', s => { const a = structuredClone(s.world.combat.attacks[0]); a.id = s.world.nextId++; s.world.combat.attacks.push(a); }],
    ['missing defense', s => s.world.combat.defenses[0].structure = 999999],
    ['far defense cooldown', s => s.world.combat.defenses[0].cooldown = s.world.tick + 999],
    ['missing launch cooldown', s => s.world.combat.defenses = []],
    ['altered launch cooldown', s => s.world.combat.defenses[0].cooldown--],
    ['wrong magazine contents', s => s.world.structures.find(a => a.kind === 'tower').inventory.wood = 1],
    ['double death reward', s => s.world.combat.lootWood++],
    ['missing kill', s => s.world.combat.stats.kills++],
    ['unpaid ammunition', s => s.world.consumed.ammo = 0],
    ['unrecorded shot', s => s.world.combat.stats.shots = 0],
    ['unrecorded heart damage', s => s.world.heart.health--],
    ['invalid order owner', s => s.world.combat.orders = [{ actor: 0, mode: 'rally', target: { x: 70, y: 60 }, resume: null }]],
    ['invalid order mode', s => s.world.combat.orders = [{ actor: 2, mode: 'teleport', target: { x: 70, y: 60 }, resume: null }]],
    ['invalid guard cooldown owner', s => s.world.combat.cooldowns = [{ actor: 999999, until: s.world.tick + 1 }]],
    ['pending overload', s => s.world.combat.pending = Array(151).fill('raider')],
  ];
  for (const [label, change] of cases) {
    const invalid = JSON.parse(original); change(invalid);
    assert.throws(() => parseSave(JSON.stringify(invalid)), /Invalid save/, label);
    assert.equal(encodeSave(save(world)), original, `${label} leaves current state intact`);
  }
});

test('source removal cancels paid projectiles and keeps terminal frozen attacks saveable', () => {
  const world = structuredClone(defenseWorld); attacker(world); step(world);
  const shots = world.combat.stats.shots;
  const structure = world.structures.find(s => s.id === tower.id);
  destroyStructure(world, structure);
  assert.ok(world.combat.attacks.every(a => a.source.kind !== 'structure' || a.source.id !== tower.id));
  assert.ok(world.combat.defenses.every(d => d.structure !== tower.id));
  assert.equal(world.combat.stats.structuresLost, 1);
  assert.equal(world.combat.stats.shots, shots); assert.equal(world.consumed.ammo, shots);
  save(world); continuation(world, 30);

  const terminal = structuredClone(defenseWorld); attacker(terminal); step(terminal);
  terminal.combat.stats.heartDamage += terminal.heart.health;
  terminal.heart.health = 0; terminal.terminal = 'heart-destroyed';
  assert.ok(terminal.combat.attacks.length > 0);
  const frozen = encodeSave(save(terminal)); step(terminal);
  assert.equal(encodeSave(save(terminal)), frozen);
  assert.equal(encodeSave(parseSave(frozen)), frozen);
});

test('battle and reload pending commands reject unknown ownership and survive exact dispatch', () => {
  const world = structuredClone(defenseWorld);
  assert.ok(enqueue(world, { kind: 'battle', actor: 0, mode: 'shelter', target: { x: 60, y: 68 } }));
  assert.ok(enqueue(world, { kind: 'reload', structure: tower.id }));
  const pending = save(world);
  for (const change of [s => s.world.pending[0].input.actor = 999999, s => s.world.pending[1].input.structure = 1]) {
    const invalid = structuredClone(pending); change(invalid);
    assert.throws(() => parseSave(JSON.stringify(invalid)), /Invalid save/);
  }
  continuation(world, 40);
  assert.equal(world.combat.orders.length, 6);
  assert.ok(world.combat.orders.every(o => o.mode === 'shelter'));
});

test('peace and warning schemas reject phase contradictions while a real warning saves exactly', () => {
  const world = createWorld(42, { scenario: 'work-lab' });
  const invalid = save(world); invalid.world.combat.phase = 'warning';
  assert.throws(() => parseSave(JSON.stringify(invalid)), /Invalid save/);
  const h = harness('work-lab', structuredClone(defenseWorld));
  h.w.scenario = 'colony'; h.w.combat.warningAt = h.w.tick + 1; h.w.combat.spawnAt = h.w.tick + 100;
  h.tick(); assert.equal(h.w.combat.phase, 'warning');
  assert.equal(h.w.combat.pending.length, COMBAT_CONTENT.wave.length);
  continuation(h.w, 10);
});

test('breacher telegraphs, heart defeat and downed guard cleanup remain saveable every tick', () => {
  const world = createWorld(42, { scenario: 'work-lab' });
  attacker(world, 'breacher', 65, 64);
  let telegraph = null;
  for (let i = 0; i < 100 && !world.terminal; i++) {
    step(world); save(world);
    if (!telegraph && world.combat.attacks.length) telegraph = structuredClone(world);
  }
  assert.ok(telegraph.combat.attacks.some(a => a.damage === COMBAT_CONTENT.breacher.structureDamage));
  assert.equal(world.terminal, 'heart-destroyed');
  assert.equal(world.combat.stats.heartDamage, 100);
  continuation(telegraph, 100);
  assert.equal(telegraph.terminal, 'heart-destroyed');

  const guarded = createWorld(42, { scenario: 'work-lab' });
  attacker(guarded, 'raider', 62, 64);
  assert.ok(enqueue(guarded, { kind: 'battle', actor: 2, mode: 'rally', target: { x: 61, y: 64 } }));
  step(guarded); save(guarded);
  assert.ok(guarded.combat.attacks.some(a => a.source.kind === 'person' && a.source.id === 2));
  assert.ok(enqueue(guarded, { kind: 'dev', action: 'down', id: 2 }));
  step(guarded); save(guarded);
  assert.equal(guarded.people[0].life, 'downed');
  assert.ok(!guarded.combat.attacks.some(a => a.source.kind === 'person' && a.source.id === 2));
  assert.ok(!guarded.combat.orders.some(o => o.actor === 2));
  assert.ok(!guarded.combat.cooldowns.some(c => c.actor === 2));
  continuation(guarded, 20);
});

test('a physically loaded trap preserves its one-round consumption across pending impact reload', () => {
  const h = harness('work-lab', structuredClone(defenseWorld));
  const trap = h.build('trap', 74, 62);
  h.cmd({ kind: 'reload', structure: trap.id });
  h.until(() => trap.inventory.ammo === 1, 'trap physically loaded');
  const raider = attacker(h.w, 'raider', trap.x, trap.y);
  const healthBefore = raider.health;
  h.tick(); save(h.w);
  assert.equal(h.w.combat.stats.traps, 1);
  assert.equal(trap.inventory.ammo, 0);
  assert.equal(raider.health, healthBefore, 'damage waits for the saved impact');
  assert.ok(h.w.combat.attacks.some(a => a.kind === 'trap'));
  continuation(h.w, 10);
  assert.equal(h.w.combat.stats.traps, 1);
  assert.equal(h.w.consumed.ammo, h.w.combat.stats.shots + 1);
});
