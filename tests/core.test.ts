import { describe, expect, it } from 'vitest';
import { chooseBotDecision } from '../src/core/bots';
import { applyCommand, beginCourse, botMove, createGame, defaultConfig, previewShot } from '../src/core/game';
import { generateCandidates, generateCourse } from '../src/core/generator';
import { newBall, simulateShot, tileAt } from '../src/core/physics';
import { Random } from '../src/core/random';
import type { Course, Player, Tile } from '../src/core/types';

const arena = (seed = 'arena'): Course => {
  const width = 14;
  const height = 7;
  const tiles: Tile[] = Array.from({ length: width * height }, () => ({ surface: 'fairway', height: 0 }));
  const tee = { x: 1, y: 3 };
  const cup = { x: width - 2, y: height - 2 };
  tiles[tee.y * width + tee.x] = { surface: 'tee', height: 0 };
  tiles[cup.y * width + cup.x] = { surface: 'cup', height: 0 };
  return { id: seed, seed, width, height, tiles, tee, cup, route: [tee, cup], score: { playable: true, estimatedStrokes: 1, hazards: 0, elevation: 0, routes: 1, novelty: 0, total: 100, solverShots: [] } };
};

const gameOn = (course: Course, options = {}) => {
  const game = beginCourse(createGame({ ...defaultConfig(), seed: course.seed, humanCount: 1, botCount: 1, ...options }));
  game.course = course;
  game.players.forEach((player) => { player.ball = newBall(course); });
  return game;
};

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
    expect(result.ball.resetCount).toBe(1);
    expect(result.ball.x).toBeLessThan(start.x);
  });

  it('returns only validated candidates for the inspector', () => {
    const candidates = generateCandidates('inspector-seed', 3);
    expect(candidates).toHaveLength(3);
    expect(candidates.every((course) => course.score.playable && course.score.solverShots.length > 0)).toBe(true);
  });

  it('keeps every generated route tile available in the fixed view', () => {
    const course = generateCourse('fixed-view-seed');
    expect(course.route.every((point) => tileAt(course, point.x + .5, point.y + .5)?.surface !== 'void')).toBe(true);
  });

  it('adds sparse wall bumpers without invalidating generated courses', () => {
    const course = generateCourse('wall-bumper-seed');
    expect(course.tiles.some((tile) => tile.surface === 'wall')).toBe(true);
    expect(course.score.playable).toBe(true);
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

  it('records a deterministic emote for a valid player', () => {
    const game = beginCourse(createGame({ ...defaultConfig(), seed: 'emote-seed', botCount: 1 }));
    const player = game.players[0]!;
    const next = applyCommand(game, { type: 'emote', playerId: player.id, emote: 'cheer' });
    expect(next.emotes).toEqual([{ id: `${game.course.seed}:${player.id}:1`, playerId: player.id, emote: 'cheer' }]);
    expect(next.emoteSequence).toBe(1);
  });

  it('ignores an emote from an unknown player', () => {
    const game = beginCourse(createGame({ ...defaultConfig(), seed: 'invalid-emote', botCount: 1 }));
    const next = applyCommand(game, { type: 'emote', playerId: 'not-a-player', emote: 'gg' });
    expect(next.emotes).toEqual([]);
    expect(next.emoteSequence).toBe(0);
  });

  it('provides animation frames that finish at the same ball state as the committed shot', () => {
    const game = beginCourse(createGame({ ...defaultConfig(), seed: 'animation-seed', botCount: 1 }));
    const shot = { angle: 0, power: 3 };
    const frames = previewShot(game, shot)!;
    const committed = applyCommand(game, { type: 'shoot', shot }).players[0]!.ball;
    const committedOpponent = applyCommand(game, { type: 'shoot', shot }).players[1]!.ball;
    expect(frames.length).toBeGreaterThan(1);
    expect(frames.at(-1)?.[0]).toMatchObject({ x: committed.x, y: committed.y, z: committed.z, complete: committed.complete });
    expect(frames.at(-1)?.[1]).toMatchObject({ x: committedOpponent.x, y: committedOpponent.y, z: committedOpponent.z, complete: committedOpponent.complete });
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

  it('knocks resting balls only while collisions are enabled', () => {
    const course = arena('collision-arena');
    const initial = newBall(course);
    const target = { ...initial, x: 4.5, y: 3.5 };
    const shot = { angle: 0, power: 4 };
    const disabled = simulateShot(course, initial, shot, 2, { otherBalls: [{ ball: target }], collisions: false });
    const enabled = simulateShot(course, initial, shot, 2, { otherBalls: [{ ball: target }], collisions: true });
    expect(disabled.otherBalls[0]).toMatchObject({ x: target.x, y: target.y });
    expect(enabled.otherBalls[0]!.x).toBeGreaterThan(target.x + .2);

    const disabledGame = gameOn(course, { collisions: false });
    disabledGame.players[1]!.ball = target;
    const enabledGame = gameOn(course, { collisions: true });
    enabledGame.players[1]!.ball = target;
    const disabledCommit = applyCommand(disabledGame, { type: 'shoot', shot });
    const enabledCommit = applyCommand(enabledGame, { type: 'shoot', shot });
    expect(disabledCommit.players[1]!.ball.x).toBe(target.x);
    expect(enabledCommit.players[1]!.ball.x).toBeGreaterThan(target.x + .2);
  });

  it('applies heavy-ball mass, ice skates, bank shots, and shields in physics', () => {
    const course = arena('modifier-arena');
    const initial = newBall(course);
    const target = { ...initial, x: 4.5, y: 3.5 };
    const regularHit = simulateShot(course, initial, { angle: 0, power: 4 }, 1, { otherBalls: [{ ball: target }], collisions: true });
    const heavyHit = simulateShot(course, initial, { angle: 0, power: 4 }, 1, { modifiers: { mass: 1.45 }, otherBalls: [{ ball: target }], collisions: true });
    expect(heavyHit.otherBalls[0]!.x).toBeGreaterThan(regularHit.otherBalls[0]!.x);

    course.tiles.forEach((tile) => { if (tile.surface === 'fairway') tile.surface = 'ice'; });
    const regularIce = simulateShot(course, initial, { angle: 0, power: 2 }, .8);
    const skates = simulateShot(course, initial, { angle: 0, power: 2 }, .8, { modifiers: { iceSkates: true } });
    expect(skates.ball.x).toBeGreaterThan(regularIce.ball.x);

    const wallCourse = arena('bank-arena');
    wallCourse.tiles[3 * wallCourse.width + 4] = { surface: 'wall', height: 0 };
    const normalBank = simulateShot(wallCourse, newBall(wallCourse), { angle: 0, power: 4 }, 1);
    const bankShot = simulateShot(wallCourse, newBall(wallCourse), { angle: 0, power: 4 }, 1, { modifiers: { bankShot: true } });
    expect(bankShot.ball.x).toBeLessThan(normalBank.ball.x);

    const voidCourse = arena('shield-arena');
    voidCourse.tiles = voidCourse.tiles.map(() => ({ surface: 'void', height: 0 }));
    voidCourse.tiles[3 * voidCourse.width + 1] = { surface: 'tee', height: 0 };
    voidCourse.tiles[3 * voidCourse.width + 2] = { surface: 'fairway', height: 0 };
    const shielded = simulateShot(voidCourse, newBall(voidCourse), { angle: 0, power: 8 }, .3, { modifiers: { hazardShield: true } });
    expect(shielded.shieldUsed).toBe(true);
    expect(shielded.reset).toBe(false);
  });

  it('grants extra-charge drops, chaos-magnet refills, and bomb displacement', () => {
    const course = arena('chaos-arena');
    let extraCharge = gameOn(course, { botCount: 0 });
    extraCharge.players[0]!.upgrades = ['extra charge'];
    extraCharge = applyCommand(extraCharge, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(extraCharge.players[0]!.inventory).toBeDefined();

    let magnet = gameOn(course, { botCount: 0 });
    magnet.players[0]!.upgrades = ['chaos magnet'];
    magnet.players[0]!.inventory = 'turbo';
    magnet.course.seed = Array.from({ length: 12 }, (_, index) => `magnet-${index}`).find((seed) => new Random(`${seed}:human-0:0:turbo`).chance(.65))!;
    magnet = applyCommand(magnet, { type: 'use-power-up', powerUp: 'turbo' });
    expect(magnet.players[0]!.inventory).toBeDefined();

    let bomb = gameOn(course);
    bomb.players[0]!.inventory = 'bomb';
    bomb.players[1]!.ball = { ...bomb.players[1]!.ball, x: 5.5, y: 3.5 };
    const before = { ...bomb.players[1]!.ball };
    bomb = applyCommand(bomb, { type: 'use-power-up', powerUp: 'bomb', targetId: bomb.players[1]!.id });
    expect(Math.hypot(bomb.players[1]!.ball.x - before.x, bomb.players[1]!.ball.y - before.y)).toBeGreaterThan(.2);
  });

  it('returns and applies bot item decisions before their shot', () => {
    const course = arena('bot-item-arena');
    const bot = { id: 'bot-0', name: 'enemy-1', color: '#fff', kind: 'bot', skill: 10, ball: { ...newBall(course), x: 3.5, y: 3.5 }, upgrades: [], inventory: 'turbo', total: 0 } satisfies Player;
    const human = { id: 'human-0', name: 'golfer-1', color: '#000', kind: 'human', skill: 0, ball: newBall(course), upgrades: [], total: 0 } satisfies Player;
    const selected = Array.from({ length: 24 }, (_, index) => {
      const candidate = { ...course, seed: `bot-item-${index}` };
      return { course: candidate, decision: chooseBotDecision(candidate, bot, [human, bot]) };
    }).find(({ decision }) => decision.powerUp);
    expect(selected).toBeDefined();
    const decision = selected!.decision;
    expect(decision.powerUp).toEqual({ type: 'turbo' });

    let game = gameOn(selected!.course);
    game.players[1] = bot;
    game.turn.playerIndex = 1;
    const move = botMove(game)!;
    expect(move.powerUp).toEqual({ type: 'turbo' });
    game = applyCommand(game, { type: 'use-power-up', powerUp: move.powerUp!.type });
    expect(game.players[1]!.turboArmed).toBe(true);
    game = applyCommand(game, { type: 'shoot', shot: move.shot });
    expect(game.players[1]!.turboArmed).toBe(false);
    expect(game.players[1]!.ball.strokes).toBe(1);
  });
});
