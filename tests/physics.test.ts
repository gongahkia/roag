import { describe, expect, it } from 'vitest';
import { generateCourse } from '../src/core/generator';
import { MAX_SURFACE_SPEED, floorHeightAt, newBall, simulateShot, tileCornerHeights } from '../src/core/physics';
import { projectWorldDirection } from '../src/ui/render';
import type { Course, Surface, Tile } from '../src/core/types';

const lane = (surface: Surface = 'fairway', width = 80): Course => {
  const height = 7;
  const tee = { x: 1, y: 3 };
  const cup = { x: width - 5, y: 3 };
  const tiles: Tile[] = Array.from({ length: width * height }, () => ({ surface, height: 0 }));
  tiles[tee.y * width + tee.x] = { surface: 'tee', height: 0 };
  tiles[cup.y * width + cup.x] = { surface: 'cup', height: 0 };
  return { id: `lane-${surface}`, seed: `lane-${surface}`, width, height, tiles, tee, cup, route: [tee, cup], hazards: [], itemPads: [], score: { playable: true, estimatedStrokes: 1, hazards: 0, elevation: 0, routes: 1, novelty: 0, total: 0, solverShots: [] } };
};

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

  it('preserves tangential velocity on a glancing wall rebound', () => {
    const course = lane();
    course.tiles[3 * course.width + 3] = { surface: 'wall', height: 0 };
    const result = simulateShot(course, newBall(course), { angle: .1, power: 4 }, .8);
    expect(result.ball.vx).toBeLessThan(0);
    expect(result.ball.vy).toBeGreaterThan(.2);
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
});
