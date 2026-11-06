import test from 'node:test';
import assert from 'node:assert/strict';
import { establish } from './helpers/survival.mjs';
import { step, enqueue } from '../src/simulation/step.ts';
import { materialAudit } from '../src/simulation/jobs.ts';
import { WORK_CONTENT } from '../src/simulation/work-content.ts';
import { canonical } from '../src/simulation/canonical.ts';
import { makeSave, encodeSave, parseSave } from '../src/persistence/schema.ts';

const baseline = establish('work-lab').w;
const controls = { manualPaused: true, speed: 4 };

function validate(world) {
  for (const [material, amounts] of Object.entries(materialAudit(world)))
    assert.equal(amounts.accounted, amounts.produced, `${material} conservation`);
  return makeSave(world, controls);
}

function setup() {
  const world = structuredClone(baseline);
  const tick = () => { step(world); validate(world); };
  const command = input => { assert.ok(enqueue(world, input)); tick(); };
  const until = (condition, label, limit = 800) => {
    for (let i = 0; i < limit && !condition(); i++) tick();
    assert.ok(condition(), label);
  };
  for (const job of [...world.jobs].filter(job => job.state === 'active'))
    command({ kind: 'work', job: job.id, action: 'cancel' });
  return { world, tick, command, until };
}

function continueFromImmediateSave(world, ticks) {
  const restored = parseSave(encodeSave(validate(world))).world;
  assert.equal(canonical(restored), canonical(world));
  for (let i = 0; i < ticks; i++) {
    step(world);
    step(restored);
    validate(world);
    validate(restored);
    assert.equal(canonical(restored), canonical(world), `continuation tick ${i + 1}`);
  }
}

for (const need of ['meal', 'medicine']) test(`dismantling storage settles a ${need} care claim before saving that tick`, () => {
  const { world, command, until } = setup();
  const claimant = world.people[5];
  if (need === 'medicine') {
    // Keep the future rescuer separate from the worker removing the storage.
    for (const person of world.people)
      command({ kind: 'duty', actor: person.id, duty: 'healer', value: person === claimant ? 3 : 0 });
  }
  command({ kind: 'move', actor: claimant.id, target: { x: 80, y: 64 } });
  until(() => claimant.x === 80 && claimant.y === 64 && !claimant.next && !claimant.destination, 'claimant arrives away from supplies');
  const storage = world.structures.find(s => s.kind === 'stockpile' && s.x === (need === 'meal' ? 63 : 62) && s.y === 66);
  assert.ok(storage.inventory[need] > 0);
  command({ kind: 'dismantle', structure: storage.id });
  const job = world.jobs.find(j => j.kind === 'dismantle' && j.ref === storage.id);
  until(() => job.progress === WORK_CONTENT.dismantleTicks - 1, 'storage reaches final work tick');
  assert.notEqual(job.worker, claimant.id);
  const consumedBefore = world.consumed[need];
  const patient = world.people[4];
  command({ kind: 'dev', action: need === 'meal' ? 'hungry' : 'down', id: need === 'meal' ? claimant.id : patient.id });
  assert.equal(job.state, 'done');
  assert.ok(!world.structures.some(s => s.id === storage.id));
  assert.equal(claimant.body.routine?.kind, need === 'meal' ? 'eat' : 'rescue');
  assert.equal(claimant.body.routine.reservation, null, 'removed storage has no surviving care claim');
  assert.equal(world.consumed[need], consumedBefore, 'supply removal does not remotely consume it');
  assert.ok(world.piles.some(pile => pile.x === storage.x && pile.y === storage.y && pile.inventory[need] > 0), 'physical supplies remain recoverable');

  continueFromImmediateSave(world, 500);
  assert.equal(world.consumed[need], consumedBefore + (need === 'meal' ? 1 : 2));
  if (need === 'medicine') {
    assert.equal(patient.life, 'alive');
    assert.equal(patient.body.health, 100);
    assert.equal(world.statistics.rescues, 1);
    assert.equal(world.statistics.treatments, 1);
  }
});

for (const facility of ['bed', 'infirmary']) test(`dismantling a claimed ${facility} releases its care reference atomically`, () => {
  const { world, command, until } = setup();
  const person = world.people[5];
  command({ kind: 'dev', action: facility === 'bed' ? 'tired' : 'injure', id: person.id });
  const structure = world.structures.find(s => s.id === person.body.routine?.facility);
  assert.equal(structure?.kind, facility);
  command({ kind: 'dismantle', structure: structure.id });
  until(() => !world.structures.some(s => s.id === structure.id), 'claimed facility is removed');
  assert.ok(world.people.every(p => p.body.routine?.facility !== structure.id));
  continueFromImmediateSave(world, 40);
});
