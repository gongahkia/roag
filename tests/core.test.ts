import { describe, expect, it } from 'vitest';
import { chooseBotDecision } from '../src/core/bots';
import { applyCommand, beginCourse, createGame, defaultConfig } from '../src/core/game';
import { generateCourse } from '../src/core/generator';
import { newBall, simulateShot } from '../src/core/physics';

describe('course generation', () => {
  it('reproduces a valid course for a fixed seed', () => {
    const first = generateCourse('test-seed');
    const second = generateCourse('test-seed');
    expect(first.tiles).toEqual(second.tiles);
    expect(first.score.playable).toBe(true);
    expect(first.score.solverShots.length).toBeGreaterThan(0);
  });

  it('keeps the tee-to-cup solver line inside the simulation', () => {
    const course = generateCourse('solver-seed');
    const result = simulateShot(course, newBall(course), course.score.solverShots[0]!);
    expect(result.holed || result.ball.resetCount === 0).toBe(true);
  });
});

describe('turns and bots', () => {
  it('accepts a legal human shot and advances turn state', () => {
    const config = { ...defaultConfig(), seed: 'turn-seed', botCount: 1 };
    const game = beginCourse(createGame(config));
    const next = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 3 } });
    expect(next.players[0]!.ball.strokes).toBe(1);
    expect(next.turn.playerIndex).toBe(1);
  });

  it('gives bots a finite physics-valid shot', () => {
    const game = beginCourse(createGame({ ...defaultConfig(), seed: 'bot-seed', humanCount: 1, botCount: 1, botSkill: 8 }));
    const bot = game.players[1]!;
    const decision = chooseBotDecision(game.course, bot, game.players);
    expect(decision.shot.power).toBeGreaterThanOrEqual(1);
    expect(Number.isFinite(decision.shot.angle)).toBe(true);
  });
});
