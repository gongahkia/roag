import test from 'node:test';
import assert from 'node:assert/strict';
import { constrain, zoomAt, WORLD } from '../src/presentation/view.ts';
import { founders } from '../src/content.ts';
import { createWorld } from '../src/simulation/world.ts';
import { personAt } from '../src/simulation/model.ts';

const size = { width: 1000, height: 600 };
test('camera clamps every edge and integer zoom while keeping the viewport inside the map', () => {
  for (const x of [-10000, 0, 400, 10000]) for (const y of [-10000, 0, 280, 10000]) for (const zoom of [-20, 1, 2, 3.7, 6, 20]) {
    const view = constrain({ x, y, zoom }, size);
    assert.equal(view.zoom, Math.round(view.zoom));
    assert.ok(view.x - size.width / view.zoom / 2 >= 0);
    assert.ok(view.x + size.width / view.zoom / 2 <= WORLD.width);
    assert.ok(view.y - size.height / view.zoom / 2 >= 0);
    assert.ok(view.y + size.height / view.zoom / 2 <= WORLD.height);
  }
});
test('zoom anchors the world coordinate under the mouse away from map bounds', () => {
  const before = { x: 384, y: 288, zoom: 3 };
  const pointer = { x: 625, y: 270 };
  const after = zoomAt(before, size, 1, pointer);
  assert.equal(before.x + (pointer.x - size.width / 2) / before.zoom, after.x + (pointer.x - size.width / 2) / after.zoom);
  assert.equal(before.y + (pointer.y - size.height / 2) / before.zoom, after.y + (pointer.y - size.height / 2) / after.zoom);
});
test('oversized displays and resize still keep bounds valid', () => {
  const large = { width: 6000, height: 3000 };
  const view = constrain({ x: -5, y: 5000, zoom: 2 }, large);
  assert.ok(view.zoom >= Math.ceil(large.width / WORLD.width));
  assert.ok(view.x >= large.width / view.zoom / 2);
  assert.ok(view.y <= WORLD.height - large.height / view.zoom / 2);
});
test('all six generated founders have stable identities, distinct bindings, and spatial hit lookup', () => {
  const world = createWorld(42);
  assert.equal(founders.length, 6);
  assert.equal(new Set(world.people.map((p) => p.id)).size, 6);
  assert.equal(new Set(founders.map((p) => p.sprite)).size, 6);
  const before = JSON.stringify(world);
  for (const p of world.people) assert.equal(personAt(world, p), p);
  assert.equal(personAt(world, {x:0,y:0}), undefined);
  assert.equal(JSON.stringify(world), before);
});
