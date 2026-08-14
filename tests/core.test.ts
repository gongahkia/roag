import { describe, expect, it } from 'vitest';
import { chooseBotDecision } from '../src/core/bots';
import { applyCommand, beginCourse, createGame, defaultConfig, previewShot } from '../src/core/game';
import { generateCandidates, generateCourse } from '../src/core/generator';
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
    expect(result.holed).toBe(true);
  });

  it('stops a ball at its last legal position instead of returning it to the tee', () => {
    const course = generateCourse('edge-seed');
    const start = newBall(course);
    const result = simulateShot(course, start, { angle: Math.PI, power: 8 });
    expect(result.reset).toBe(true);
    expect(result.ball.resetCount).toBe(0);
    expect(result.ball.x).toBeLessThan(start.x);
  });

  it('returns only validated candidates for the inspector', () => {
    const candidates = generateCandidates('inspector-seed', 3);
    expect(candidates).toHaveLength(3);
    expect(candidates.every((course) => course.score.playable && course.score.solverShots.length > 0)).toBe(true);
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

  it('provides animation frames that finish at the same ball state as the committed shot', () => {
    const game = beginCourse(createGame({ ...defaultConfig(), seed: 'animation-seed', botCount: 1 }));
    const shot = { angle: 0, power: 3 };
    const frames = previewShot(game, shot)!;
    const committed = applyCommand(game, { type: 'shoot', shot }).players[0]!.ball;
    expect(frames.length).toBeGreaterThan(1);
    expect(frames.at(-1)).toMatchObject({ x: committed.x, y: committed.y, z: committed.z, complete: committed.complete });
  });

  it('gives bots a finite physics-valid shot', () => {
    const game = beginCourse(createGame({ ...defaultConfig(), seed: 'bot-seed', humanCount: 1, botCount: 1, botSkill: 8 }));
    const bot = game.players[1]!;
    const decision = chooseBotDecision(game.course, bot, game.players);
    expect(decision.shot.power).toBeGreaterThanOrEqual(1);
    expect(Number.isFinite(decision.shot.angle)).toBe(true);
  });

  it('applies chaos items through game state instead of granting bot-only effects', () => {
    let game = beginCourse(createGame({ ...defaultConfig(), seed: 'item-seed', humanCount: 1, botCount: 1 }));
    game.players[0]!.inventory = 'freeze';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'freeze', targetId: game.players[1]!.id });
    expect(game.players[1]!.frozenTurns).toBe(1);
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 3 } });
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 3 } });
    expect(game.players[1]!.ball.strokes).toBe(0);
  });
});
