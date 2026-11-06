import test from 'node:test';
import assert from 'node:assert/strict';
import { createWorld } from '../src/simulation/world.ts';
import { enqueue, step } from '../src/simulation/step.ts';
import { fall, die } from '../src/simulation/needs.ts';
import { makeSave, encodeSave, parseSave } from '../src/persistence/schema.ts';
import { addRaider } from './helpers/combat.mjs';

const controls = { manualPaused: true, speed: 1 };
const save = world => encodeSave(makeSave(world, controls));
function retired(world, person) {
  assert.equal(world.combat.orders.some(order => order.actor === person.id), false);
  assert.equal(world.combat.cooldowns.some(cooldown => cooldown.actor === person.id), false);
  assert.equal(world.combat.attacks.some(attack => attack.source.kind === 'person' && attack.source.id === person.id), false);
}
function continuation(world) {
  const restored = parseSave(save(world)).world;
  for (let tick = 0; tick < 100; tick++) {
    step(world); step(restored);
    assert.equal(save(restored), save(world), `exact continuation at tick ${world.tick}`);
  }
}
function threeCompletedWaves() {
  const world = createWorld(42, { scenario: 'work-lab' });
  for (let wave = 1; wave <= 3; wave++) {
    addRaider(world, 62, 64);
    assert.ok(enqueue(world, { kind: 'battle', actor: 0, mode: 'rally', target: { x: 62, y: 64 } }));
    for (let tick = 0; tick < 400 && world.combat.phase !== 'recovery'; tick++) step(world);
    assert.equal(world.combat.completed, wave); save(world);
  }
  return world;
}

test('lethal Captain charge retires guard intent on the same public tick and saves immediately', () => {
  const world = threeCompletedWaves(), person = world.people[0];
  // Explicit isolated injury/position fixture; real prior completions, charge,
  // movement, damage, cleanup and subsequent continuation all use the engine.
  person.x = 60; person.y = 61; person.body.health = 1;
  addRaider(world, 60, 60, 'captain');
  assert.ok(enqueue(world, { kind: 'battle', actor: person.id, mode: 'rally', target: { x: 60, y: 61 } }));
  let lethal = null;
  for (let ticks = 0; ticks < 100 && person.life === 'alive'; ticks++) {
    const before = makeSave(world, controls); step(world);
    if (person.life === 'downed') {
      lethal = before.world.combat.attacks.find(attack => attack.kind === 'charge' && attack.due === world.tick);
      assert.ok(lethal, 'A committed Captain charge causes the late fall');
      assert.ok(before.world.combat.orders.some(order => order.actor === person.id));
      assert.ok(before.world.combat.cooldowns.some(cooldown => cooldown.actor === person.id));
      retired(world, person);
    }
    save(world);
  }
  assert.ok(lethal); assert.equal(person.life, 'downed'); assert.equal(world.tick, 55);
  continuation(world);
});

test('direct fall and death atomically retire an actual pending guard strike and are idempotent', () => {
  for (const [transition, life] of [[fall, 'downed'], [die, 'dead']]) {
    const world = createWorld(42, { scenario: 'work-lab' }), person = world.people[0];
    addRaider(world, 62, 64);
    assert.ok(enqueue(world, { kind: 'battle', actor: person.id, mode: 'rally', target: { x: 62, y: 64 } }));
    step(world); save(world);
    assert.ok(world.combat.orders.some(order => order.actor === person.id));
    assert.ok(world.combat.cooldowns.some(cooldown => cooldown.actor === person.id));
    assert.ok(world.combat.attacks.some(attack => attack.source.kind === 'person' && attack.source.id === person.id));
    transition(world, person); assert.equal(person.life, life); retired(world, person);
    const frozen = save(world); transition(world, person); assert.equal(save(world), frozen);
    continuation(world);
  }
});
