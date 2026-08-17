import { describe, expect, it } from 'vitest';
import { generateCourse } from '../src/core/generator';
import { BALL_RADIUS, CUP_CAPTURE_COVERAGE, MAX_DOWNHILL_ROLL_SPEED, MAX_SURFACE_SPEED, cupCoverageAt, floorHeightAt, newBall, simulateImpulse, simulateShot, tileAt, tileCornerHeights } from '../src/core/physics';
import { aimPathFor, projectWorldDirection } from '../src/ui/render';
import type { Course } from '../src/core/types';
import { createLane as lane } from './fixtures';

const travel = (course: Course, power: number) => {
  const start = newBall(course);
  const result = simulateShot(course, start, { angle: 0, power });
  return { result, distance: result.ball.x - start.x };
};

describe('grounded physics invariants', () => {
  it('projects aim directions through the isometric world transform', () => {
    const metrics = { tileWidth: 40, tileHeight: 20 };
    expect(projectWorldDirection({ x: 1, y: 0 }, metrics)).toEqual({ x: 20, y: 10 });
    expect(projectWorldDirection({ x: 0, y: 1 }, metrics)).toEqual({ x: -20, y: 10 });
  });

  it('accepts a slow ball with a meaningful overlap on the cup instead of requiring a center hit', () => {
    const course = lane('fairway', 28);
    const cup = { x: course.cup.x + .5, y: course.cup.y + .5 };
    expect(cupCoverageAt(.32, .28)).toBeGreaterThanOrEqual(CUP_CAPTURE_COVERAGE);
    expect(cupCoverageAt(.4, .28)).toBeLessThan(CUP_CAPTURE_COVERAGE);
    const ball = { ...newBall(course), x: cup.x + .32, y: cup.y, z: floorHeightAt(course, cup.x + .32, cup.y) + BALL_RADIUS };
    const result = simulateImpulse(course, ball, { x: -.1, y: 0 }, .01);

    expect(result.holed).toBe(true);
    expect(result.ball).toMatchObject({ x: cup.x, y: cup.y, complete: true });
  });

  it('uses an airborne chip arc to clear a wall while putts stay grounded', () => {
    const course = lane('fairway', 28);
    course.tiles[3 * course.width + 3] = { surface: 'wall', height: 0 };
    const putt = simulateShot(course, newBall(course), { angle: 0, power: 5, kind: 'putt' }, 1.2);
    const chip = simulateShot(course, newBall(course), { angle: 0, power: 5, kind: 'chip' }, 1.2);
    const arc = aimPathFor(course, newBall(course), { angle: 0, power: 5, kind: 'chip' });

    expect(putt.ball.x).toBeLessThan(3);
    expect(chip.ball.x).toBeGreaterThan(3.5);
    expect(Math.max(...chip.frames.map((frame) => frame.ball.z))).toBeGreaterThan(.9);
    expect(Math.max(...arc.map((point) => point.z))).toBeGreaterThan(arc[0]!.z + .7);
  });

  it('never lets a descending chip settle on a wall tile', () => {
    const course = lane('fairway', 14);
    course.tiles[3 * course.width + 3] = { surface: 'wall', height: 0 };
    const result = simulateShot(course, newBall(course), { angle: 0, power: 4, kind: 'chip' });
    expect(tileAt(course, result.ball.x, result.ball.y)?.surface).not.toBe('wall');
    expect(result.ball.x).toBeLessThan(3);
  });

  it('makes updrafts, low bars, air rings, and sky springs meaningful airborne challenges', () => {
    const plain = lane('fairway', 28);
    const updraft = lane('fairway', 28);
    updraft.hazards = [{ id: 'updraft', kind: 'updraft', point: { x: 3, y: 3 }, direction: { x: 1, y: 0 }, radius: 1.2, strength: 6 }];
    const ring = lane('fairway', 28);
    ring.features = [{ id: 'ring', kind: 'air-ring', point: { x: 3, y: 3 }, radius: .48, boost: 1.4 }];
    const barred = lane('fairway', 28);
    barred.hazards = [{ id: 'bar', kind: 'low-bar', point: { x: 3, y: 3 }, clearance: 1.1 }];
    const sprung = lane('fairway', 28);
    const clean = simulateShot(plain, newBall(plain), { angle: 0, power: 5, kind: 'chip' }, 1.2);
    const winded = simulateShot(updraft, newBall(updraft), { angle: 0, power: 5, kind: 'chip' }, 1.2);
    const boosted = simulateShot(ring, newBall(ring), { angle: 0, power: 5, kind: 'chip' }, 1.2);
    const blocked = simulateShot(barred, newBall(barred), { angle: 0, power: 5, kind: 'chip' }, 1.2);
    const launched = simulateShot(sprung, newBall(sprung), { angle: 0, power: 3 }, 1.2, { gadgets: [{ id: 'spring', ownerId: 'human-0', kind: 'sky spring', point: { x: 3, y: 3 } }] });

    expect(winded.ball.x).toBeGreaterThan(clean.ball.x);
    expect(boosted.ball.x).toBeGreaterThan(clean.ball.x);
    expect(blocked.ball.x).toBeLessThan(clean.ball.x);
    expect(Math.max(...launched.frames.map((frame) => frame.ball.z))).toBeGreaterThan(.8);
  });

  it('preserves tangential velocity on a glancing wall rebound', () => {
    const course = lane();
    course.tiles[3 * course.width + 3] = { surface: 'wall', height: 0 };
    const result = simulateShot(course, newBall(course), { angle: .1, power: 4 }, .8);
    expect(result.ball.vx).toBeLessThan(0);
    expect(result.ball.vy).toBeGreaterThan(.2);
  });

  it('animates an edge fall before returning a ball to its last legal position', () => {
    const course = lane('fairway', 14);
    const start = newBall(course);
    const result = simulateShot(course, start, { angle: Math.PI, power: 4 });
    const fallingFrames = result.frames.map((frame) => frame.ball).filter((ball) => ball.falling);

    expect(result.reset).toBe(true);
    expect(fallingFrames.length).toBeGreaterThan(5);
    expect(fallingFrames.at(-1)!.z).toBeLessThan(fallingFrames[0]!.z);
    expect(result.ball).toMatchObject({ falling: undefined, resetCount: 1 });
    expect(result.ball.x).toBeLessThan(start.x);
  });

  it('uses a calibrated nonlinear fairway power curve and settles standard shots', () => {
    const course = lane();
    const low = travel(course, 1);
    const medium = travel(course, 2);
    const strong = travel(course, 4);
    const calibrated = travel(course, 5.6);
    expect(low.distance).toBeLessThan(medium.distance);
    expect(medium.distance).toBeLessThan(strong.distance);
    expect(strong.distance - medium.distance).toBeGreaterThan(medium.distance - low.distance);
    expect(calibrated.distance).toBeCloseTo(17, 0);
    expect(calibrated.result.settled).toBe(true);
    expect(Math.hypot(calibrated.result.ball.vx, calibrated.result.ball.vy)).toBe(0);
  });

  it('orders surface resistance and never commits a moving ball', () => {
    const distances = (['sand', 'rough', 'fairway', 'ice'] as const).map((surface) => travel(lane(surface), 3));
    expect(distances[0]!.distance).toBeLessThan(distances[1]!.distance);
    expect(distances[1]!.distance).toBeLessThan(distances[2]!.distance);
    expect(distances[2]!.distance).toBeLessThan(distances[3]!.distance);
    for (const { result } of distances) expect(Math.hypot(result.ball.vx, result.ball.vy, result.ball.vz)).toBe(0);

    const fast = travel(lane(), 8).result;
    expect(fast.settled).toBe(true);
    expect(Math.hypot(fast.ball.vx, fast.ball.vy, fast.ball.vz)).toBe(0);

    const iceLane = lane('ice', 160);
    const skates = simulateShot(iceLane, newBall(iceLane), { angle: 0, power: 8 }, undefined, { modifiers: { iceSkates: true } });
    expect(skates.settled).toBe(false);
    expect(Math.hypot(skates.ball.vx, skates.ball.vy, skates.ball.vz)).toBe(0);
  });

  it('uses continuous shared-height ramps without vertical snapping', () => {
    const course = lane();
    for (let index = 0; index < course.tiles.length; index += 1) {
      const tile = course.tiles[index]!;
      const x = index % course.width;
      const left = x <= 3 ? 0 : 1;
      const right = x < 3 ? 0 : 1;
      tile.corners = [left, right, right, left];
      tile.height = (left + right) / 2;
    }
    const flat = simulateShot(lane(), newBall(lane()), { angle: 0, power: 4 }, .8);
    const uphill = simulateShot(course, newBall(course), { angle: 0, power: 4 }, .8);
    expect(uphill.ball.x).toBeLessThan(flat.ball.x);
    for (const frame of uphill.frames) expect(frame.ball.z).toBeCloseTo(floorHeightAt(course, frame.ball.x, frame.ball.y) + .18, 6);

    const generated = generateCourse('shared-ramp-corners');
    for (let y = 0; y < generated.height; y += 1) {
      for (let x = 0; x < generated.width; x += 1) {
        const tile = generated.tiles[y * generated.width + x]!;
        if (tile.surface === 'void') continue;
        const corners = tileCornerHeights(tile);
        const east = x + 1 < generated.width ? generated.tiles[y * generated.width + x + 1]! : undefined;
        const south = y + 1 < generated.height ? generated.tiles[(y + 1) * generated.width + x]! : undefined;
        if (east && east.surface !== 'void') {
          const eastCorners = tileCornerHeights(east);
          expect(corners[1]).toBe(eastCorners[0]);
          expect(corners[2]).toBe(eastCorners[3]);
        }
        if (south && south.surface !== 'void') {
          const southCorners = tileCornerHeights(south);
          expect(corners[3]).toBe(southCorners[0]);
          expect(corners[2]).toBe(southCorners[1]);
        }
      }
    }
  });

  it('lets a nearly stopped ball roll down a meaningful hill without unlimited downhill acceleration', () => {
    const course = lane('fairway', 80);
    course.tiles.forEach((tile) => {
      tile.corners = [1, 0, 0, 1];
      tile.height = .5;
    });
    const start = newBall(course);
    const result = simulateImpulse(course, { ...start, vx: .05 }, { x: .05, y: 0 }, 3);
    const downhillSpeeds = result.frames.map((frame) => frame.ball.vx);

    expect(result.ball.x).toBeGreaterThan(start.x + 1);
    expect(Math.max(...downhillSpeeds)).toBeLessThanOrEqual(MAX_DOWNHILL_ROLL_SPEED + .03);
    expect(result.frames.some((frame) => frame.ball.vx > .12)).toBe(true);
  });

  it('restarts a low-energy wall rebound when the landing point slopes downhill', () => {
    const course = lane('fairway', 80);
    course.tiles.forEach((tile) => {
      tile.corners = [1, 0, 0, 1];
      tile.height = .5;
    });
    course.tiles[3 * course.width + 5] = { surface: 'wall', height: .5 };
    const ball = { ...newBall(course), x: 6.05, y: 3.5, z: floorHeightAt(course, 6.05, 3.5) + .18 };
    const result = simulateImpulse(course, ball, { x: -.25, y: 0 }, 1.5);

    expect(result.frames.some((frame) => frame.ball.vx < 0)).toBe(true);
    expect(result.frames.some((frame) => frame.ball.vx > .12)).toBe(true);
    expect(result.ball.x).toBeGreaterThan(6.25);
  });

  it('applies time-scaled terrain acceleration under a hard speed cap', () => {
    const course = lane('booster');
    course.tiles.forEach((tile) => { tile.direction = { x: 1, y: 0 }; });
    const result = simulateShot(course, newBall(course), { angle: 0, power: 1 }, 5);
    expect(result.ball.vx).toBeGreaterThan(7);
    expect(Math.hypot(result.ball.vx, result.ball.vy)).toBeLessThanOrEqual(MAX_SURFACE_SPEED);
  });

  it('teleports through paired portals with rotated momentum and supports a portal-ball redirect', () => {
    const course = lane('fairway', 28);
    course.portals = [
      { id: 'portal-1', entrance: { point: { x: 3, y: 3 }, direction: { x: 1, y: 0 } }, exit: { point: { x: 10, y: 3 }, direction: { x: 1, y: 0 } } },
      { id: 'portal-2', entrance: { point: { x: 5, y: 1 }, direction: { x: 1, y: 0 } }, exit: { point: { x: 18, y: 3 }, direction: { x: 1, y: 0 } } },
    ];
    const paired = simulateShot(course, newBall(course), { angle: 0, power: 4 }, .6);
    const redirected = simulateShot(course, newBall(course), { angle: 0, power: 4 }, .6, { modifiers: { portalExitId: 'portal-2:exit' } });
    const savvy = simulateShot(course, newBall(course), { angle: 0, power: 4 }, .6, { modifiers: { portalSpeedMultiplier: 1.18 } });
    expect(paired.ball.x).toBeGreaterThan(10);
    expect(redirected.ball.x).toBeGreaterThan(18);
    expect(savvy.ball.x).toBeGreaterThan(paired.ball.x);
  });

  it('applies ghost and magnet ball effects during the active shot', () => {
    const wallCourse = lane();
    wallCourse.tiles[3 * wallCourse.width + 3] = { surface: 'wall', height: 0 };
    const normal = simulateShot(wallCourse, newBall(wallCourse), { angle: 0, power: 4 }, .8);
    const ghost = simulateShot(wallCourse, newBall(wallCourse), { angle: 0, power: 4 }, .8, { modifiers: { ghostBall: true } });
    expect(normal.ball.x).toBeLessThan(3);
    expect(ghost.ball.x).toBeGreaterThan(3);

    const magnetCourse = lane();
    magnetCourse.itemPads = [{ id: 'magnet-pad', point: { x: 4, y: 4 }, kind: 'recovery' }];
    const flat = simulateShot(magnetCourse, newBall(magnetCourse), { angle: 0, power: 3 }, 1);
    const magnet = simulateShot(magnetCourse, newBall(magnetCourse), { angle: 0, power: 3 }, 1, { modifiers: { magnetBall: true } });
    expect(magnet.ball.y).toBeGreaterThan(flat.ball.y);
  });

  it('resolves biome interactions and universal gadget triggers in the deterministic simulation', () => {
    const drift = lane('fairway', 30);
    drift.theme = 'drift';
    drift.features = [{ id: 'sink', kind: 'sinkhole', entrance: { x: 3, y: 3 }, exit: { x: 15, y: 3 } }];
    const rerouted = simulateShot(drift, newBall(drift), { angle: 0, power: 4 }, .7);
    expect(rerouted.ball.x).toBeGreaterThan(15);

    const pulse = lane('fairway', 80);
    pulse.theme = 'pulse';
    pulse.features = [{ id: 'pulse', kind: 'pulse', point: { x: 2, y: 3 }, direction: { x: 1, y: 0 }, strength: 3 }];
    const launched = simulateShot(pulse, newBall(pulse), { angle: 0, power: 2 }, .8);
    const plain = simulateShot(lane('fairway', 80), newBall(lane('fairway', 80)), { angle: 0, power: 2 }, .8);
    expect(launched.ball.x).toBeGreaterThan(plain.ball.x + .4);

    const gadgets = lane('fairway', 40);
    const trapped = simulateShot(gadgets, newBall(gadgets), { angle: 0, power: 4 }, 1, { gadgets: [{ id: 'mine', ownerId: 'human-0', kind: 'blast mine', point: { x: 4, y: 3 } }] });
    expect(trapped.gadgetIds).toEqual(['mine']);
    expect(trapped.ball.x).toBeLessThan(4);
  });
});
