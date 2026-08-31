import { createGame, defaultConfig } from '../src/core/game';
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
  return { id: seed, seed, theme: 'balanced', width, height, tiles, tee, cup, route: [tee, cup], hazards: [], features: [], itemPads: [], score: { playable: true, estimatedStrokes: 1, hazards: 0, elevation: 0, routes: 1, novelty: 0, total: 100, solverShots: [] } };
};

export const createLane = (surface: Surface = 'fairway', width = 80): Course => {
  const height = 7;
  const tee = { x: 1, y: 3 };
  const cup = { x: width - 5, y: 3 };
  const tiles: Tile[] = Array.from({ length: width * height }, () => ({ surface, height: 0 }));
  tiles[tee.y * width + tee.x] = { surface: 'tee', height: 0 };
  tiles[cup.y * width + cup.x] = { surface: 'cup', height: 0 };
  return { id: `lane-${surface}`, seed: `lane-${surface}`, theme: 'balanced', width, height, tiles, tee, cup, route: [tee, cup], hazards: [], features: [], itemPads: [], score: { playable: true, estimatedStrokes: 1, hazards: 0, elevation: 0, routes: 1, novelty: 0, total: 0, solverShots: [] } };
};

export const gameOn = (course: Course, options: Partial<GameConfig> = {}) => {
  const requestedRuleset = options.ruleset ?? 'custom';
  // Arena fixtures overwrite the materialized course immediately. Use the
  // bounded Party generator for that discarded setup, then restore the
  // requested ruleset for the behavior the fixture is exercising.
  const game = createGame({ ...defaultConfig(), seed: course.seed, holeCount: 1, humanCount: 1, botCount: 1, ...options, ruleset: requestedRuleset === 'custom' ? 'party' : requestedRuleset });
  game.course = course;
  game.config.ruleset = requestedRuleset;
  game.coursePlan = [];
  game.die = undefined;
  game.status = 'playing';
  game.turn = { playerIndex: 0, secondsLeft: game.holeRules.timerSeconds, shotInFlight: false, cardPlayed: false };
  game.players.forEach((player) => { player.ball = newBall(course); });
  return game;
};
