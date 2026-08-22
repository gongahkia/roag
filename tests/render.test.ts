import { describe, expect, it } from 'vitest';
import { floorHeightAt, newBall } from '../src/core/physics';
import { createArena } from './fixtures';
import { cameraFor, campaignSlotsFor } from '../src/ui/render';

describe('dynamic course camera', () => {
  it('fits compact levels and follows the focus ball across oversized levels', () => {
    const fullCourse = (seed: string, width: number, height: number) => {
      const course = createArena(seed);
      course.width = width;
      course.height = height;
      course.tee = { x: 1, y: Math.floor(height / 2) };
      course.cup = { x: width - 2, y: Math.floor(height / 2) };
      course.route = [course.tee, course.cup];
      course.tiles = Array.from({ length: width * height }, () => ({ surface: 'fairway' as const, height: 0 }));
      course.tiles[course.tee.y * width + course.tee.x] = { surface: 'tee', height: 0 };
      course.tiles[course.cup.y * width + course.cup.x] = { surface: 'cup', height: 0 };
      return course;
    };
    const compact = fullCourse('camera-compact', 16, 10);
    const compactCamera = cameraFor(compact, 700, 500, newBall(compact));
    expect(compactCamera.followsFocus).toBe(false);

    const large = fullCourse('camera-large', 96, 48);
    const tee = newBall(large);
    const cup = { ...tee, x: large.cup.x + .5, y: large.cup.y + .5, z: floorHeightAt(large, large.cup.x + .5, large.cup.y + .5) + .18 };
    const fromTee = cameraFor(large, 320, 300, tee);
    const fromCup = cameraFor(large, 320, 300, cup);
    expect(fromTee.followsFocus).toBe(true);
    expect(fromTee.metrics.tileWidth).toBeGreaterThanOrEqual(16);
    expect(fromCup.offset).not.toEqual(fromTee.offset);
  });
});

describe('campaign atlas layout', () => {
  it('assigns every planned hole a stable, visible island district', () => {
    const slots = campaignSlotsFor(9, 1440, 900);
    expect(slots).toHaveLength(9);
    expect(new Set(slots.map((slot) => `${slot.x}:${slot.y}`)).size).toBe(9);
    slots.forEach((slot) => {
      expect(slot.x).toBeGreaterThan(0);
      expect(slot.x).toBeLessThan(1440);
      expect(slot.y).toBeGreaterThan(0);
      expect(slot.y).toBeLessThan(900);
      expect(slot.width).toBeGreaterThan(0);
      expect(slot.height).toBeGreaterThan(0);
    });
    expect(slots[0]).toMatchObject({ index: 0 });
    expect(slots.at(-1)).toMatchObject({ index: 8 });
  });
});
