import { describe, expect, it } from 'vitest';
import { floorHeightAt, newBall } from '../src/core/physics';
import { createArena } from './fixtures';
import { cameraFor } from '../src/ui/render';

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

    const large = fullCourse('camera-large', 28, 20);
    const tee = newBall(large);
    const cup = { ...tee, x: large.cup.x + .5, y: large.cup.y + .5, z: floorHeightAt(large, large.cup.x + .5, large.cup.y + .5) + .18 };
    const fromTee = cameraFor(large, 360, 300, tee);
    const fromCup = cameraFor(large, 360, 300, cup);
    expect(fromTee.followsFocus).toBe(true);
    expect(fromTee.metrics.tileWidth).toBeGreaterThanOrEqual(16);
    expect(fromCup.offset).not.toEqual(fromTee.offset);
  });
});
