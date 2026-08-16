import { beginCourse, createGame, defaultConfig } from '../src/core/game';
import { newBall } from '../src/core/physics';
import type { Course, GameConfig, Surface, Tile } from '../src/core/types';

export const createArena = (seed = 'arena'): Course => {
  const width = 14;
  const height = 7;
  const tiles: Tile[] = Array.from({ length: width * height }, () => ({ surface: 'fairway', height: 0 }));
  const tee = { x: 1, y: 3 };
  const cup = { x: width - 2, y: height - 2 };
  tiles[tee.y * width + tee.x] = { surface: 'tee', height: 0 };
  tiles[cup.y * width + cup.x] = { surface: 'cup', height: 0 };
  return { id: seed, seed, width, height, tiles, tee, cup, route: [tee, cup], hazards: [], itemPads: [], score: { playable: true, estimatedStrokes: 1, hazards: 0, elevation: 0, routes: 1, novelty: 0, total: 100, solverShots: [] } };
};

export const createLane = (surface: Surface = 'fairway', width = 80): Course => {
  const height = 7;
  const tee = { x: 1, y: 3 };
  const cup = { x: width - 5, y: 3 };
  const tiles: Tile[] = Array.from({ length: width * height }, () => ({ surface, height: 0 }));
  tiles[tee.y * width + tee.x] = { surface: 'tee', height: 0 };
  tiles[cup.y * width + cup.x] = { surface: 'cup', height: 0 };
  return { id: `lane-${surface}`, seed: `lane-${surface}`, width, height, tiles, tee, cup, route: [tee, cup], hazards: [], itemPads: [], score: { playable: true, estimatedStrokes: 1, hazards: 0, elevation: 0, routes: 1, novelty: 0, total: 0, solverShots: [] } };
};

export const gameOn = (course: Course, options: Partial<GameConfig> = {}) => {
  const game = beginCourse(createGame({ ...defaultConfig(), seed: course.seed, humanCount: 1, botCount: 1, ...options }));
  game.course = course;
  game.players.forEach((player) => { player.ball = newBall(course); });
  return game;
};
