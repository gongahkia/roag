import { describe, expect, it } from 'vitest';
import { floorHeightAt, newBall } from '../src/core/physics';
import { createArena } from './fixtures';
import { cameraFor } from '../src/ui/render';

describe('dynamic course camera', () => {
  it('follows the active ball on compact and oversized courses', () => {
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
    expect(compactCamera.followsFocus).toBe(true);
    expect(compactCamera.offset).not.toEqual(cameraFor(compact, 700, 500).offset);

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
