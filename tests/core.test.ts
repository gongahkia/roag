import { describe, expect, it } from 'vitest';
import { chooseBotDecision, chooseBotVote } from '../src/core/bots';
import { applyCommand, botMove, createGame, defaultConfig, previewShot, tickTurn } from '../src/core/game';
import { defaultHoleRules, defaultTerrainSettings, generateCandidates, generateCourse, generateVotingOptions, randomTerrainSettings } from '../src/core/generator';
import { newBall, simulateShot, tileAt } from '../src/core/physics';
import { powerUpFor } from '../src/core/powerups';
import { Random } from '../src/core/random';
import { createArena as arena, gameOn } from './fixtures';

const resolveVote = (game: ReturnType<typeof createGame>, optionIndex = 0) => {
  const optionId = game.vote!.options[optionIndex]!.id;
  const resolved = game.players.reduce((next, player) => applyCommand(next, { type: 'cast-vote', playerId: player.id, optionId }), game);
  return applyCommand(resolved, { type: 'complete-assembly' });
};

describe('course generation', () => {
  it('reproduces valid courses and candidate pools for a fixed seed', () => {
    const first = generateCourse('test-seed');
    const second = generateCourse('test-seed');
    const candidates = generateCandidates('inspector-seed', 3);
    expect(first.tiles).toEqual(second.tiles);
    expect(first.score.playable).toBe(true);
    expect(candidates).toHaveLength(3);
    expect(candidates.every((course) => course.score.playable && course.score.solverShots.length > 0)).toBe(true);
  });

  it('keeps solver routes inside playable terrain and across every configured phase', () => {
    const options = generateVotingOptions('phase-set', 3);
    expect(options).toHaveLength(3);
    for (const option of options) {
      expect(option.course.route.every((point) => tileAt(option.course, point.x + .5, point.y + .5)?.surface !== 'void')).toBe(true);
      for (let phase = 0; phase < option.recipe.rules.hazardPhaseCount; phase += 1) {
        expect(simulateShot(option.course, newBall(option.course), option.course.score.solverShots[0]!, 10, { phase, phaseCount: option.recipe.rules.hazardPhaseCount }).holed).toBe(true);
      }
    }
  });

  it('exposes deterministic granular recipes including independent surface, hazard, portal, and pad quantities', () => {
    const terrain = randomTerrainSettings('granularity', 2);
    const generated = generateCourse('granularity-course', { ...terrain, wallCount: 5, sweeperCount: 2, gateCount: 2, portalPairs: 2, recoveryPads: 3, chaosPads: 4 });
    expect(terrain).toEqual(randomTerrainSettings('granularity', 2));
    expect(generated.hazards.filter((hazard) => hazard.kind === 'sweeper')).toHaveLength(2);
    expect(generated.hazards.filter((hazard) => hazard.kind === 'gate')).toHaveLength(2);
    expect(generated.portals).toHaveLength(2);
    expect(generated.itemPads.filter((pad) => pad.kind === 'recovery')).toHaveLength(3);
    expect(generated.itemPads.filter((pad) => pad.kind === 'chaos')).toHaveLength(4);
  });

  it('keeps generated courses guarded and solver-playable', () => {
    const course = generateCourse('edge-seed', { ...defaultTerrainSettings(), wallCount: 3 });
    const start = newBall(course);
    const result = simulateShot(course, start, { angle: Math.PI, power: 8 });
    expect(course.tiles.some((tile) => tile.surface === 'wall')).toBe(true);
    expect(result.reset).toBe(false);
    expect(result.ball.x).toBeGreaterThan(start.x - .5);
  });
});

describe('public voting flow', () => {
  it('starts every match with three reproducible public voting options and no build state', () => {
    const first = createGame({ ...defaultConfig(), seed: 'ballot-seed', humanCount: 2, botCount: 1 });
    const second = createGame({ ...defaultConfig(), seed: 'ballot-seed', humanCount: 2, botCount: 1 });
    expect(first.status).toBe('voting');
    expect(first.vote?.options.map((option) => option.id)).toEqual(second.vote?.options.map((option) => option.id));
    expect(first.vote?.options.map((option) => option.course.tiles)).toEqual(second.vote?.options.map((option) => option.course.tiles));
  });

  it('keeps named ballots public and editable until the final ballot resolves the plurality winner', () => {
    let game = createGame({ ...defaultConfig(), seed: 'public-ballot', humanCount: 2, botCount: 1 });
    const [first, second, third] = game.vote!.options;
    game = applyCommand(game, { type: 'cast-vote', playerId: 'human-0', optionId: first!.id });
    game = applyCommand(game, { type: 'cast-vote', playerId: 'human-0', optionId: second!.id });
    game = applyCommand(game, { type: 'cast-vote', playerId: 'human-1', optionId: second!.id });
    expect(game.status).toBe('voting');
    expect(game.vote!.ballots['human-0']).toBe(second!.id);
    game = applyCommand(game, { type: 'cast-vote', playerId: 'bot-0', optionId: third!.id });
    expect(game.status).toBe('assembling');
    expect(game.course.seed).toBe(second!.course.seed);
    expect(game.assembly).toMatchObject({ optionId: second!.id, label: second!.label, votes: 2, totalBallots: 3 });
    expect(game.players.every((player) => player.upgrades.join(',') === game.holeRules.sharedBoons.join(','))).toBe(true);
    game = applyCommand(game, { type: 'complete-assembly' });
    expect(game.status).toBe('playing');
  });

  it('breaks tied pluralities and bot ballots deterministically', () => {
    const initial = createGame({ ...defaultConfig(), seed: 'tied-ballot', humanCount: 1, botCount: 1 });
    expect(chooseBotVote(initial.config.seed, 1, initial.players[1]!, initial.vote!.options)).toBe(chooseBotVote(initial.config.seed, 1, initial.players[1]!, initial.vote!.options));
    const options = initial.vote!.options;
    let first = applyCommand(initial, { type: 'cast-vote', playerId: 'human-0', optionId: options[0]!.id });
    first = applyCommand(first, { type: 'cast-vote', playerId: 'bot-0', optionId: options[1]!.id });
    let second = applyCommand(createGame({ ...defaultConfig(), seed: 'tied-ballot', humanCount: 1, botCount: 1 }), { type: 'cast-vote', playerId: 'human-0', optionId: options[0]!.id });
    second = applyCommand(second, { type: 'cast-vote', playerId: 'bot-0', optionId: options[1]!.id });
    expect(first.course.seed).toBe(second.course.seed);
  });

  it('rejects unknown voters and options without advancing the ballot', () => {
    const game = createGame({ ...defaultConfig(), seed: 'invalid-vote', botCount: 0 });
    expect(applyCommand(game, { type: 'cast-vote', playerId: 'unknown', optionId: 'missing' })).toEqual(game);
  });
});

describe('turns, shared rules, and bots', () => {
  it('freezes turns and shots while paused, then resumes the same match state', () => {
    let game = resolveVote(createGame({ ...defaultConfig(), seed: 'pause-state', humanCount: 1, botCount: 0 }));
    const secondsLeft = game.turn.secondsLeft;
    game = applyCommand(game, { type: 'set-paused', paused: true });
    expect(game.paused).toBe(true);
    expect(previewShot(game, { angle: 0, power: 3 })).toBeUndefined();
    expect(tickTurn(game, 10)).toBe(game);
    expect(game.turn.secondsLeft).toBe(secondsLeft);
    game = applyCommand(game, { type: 'set-paused', paused: false });
    expect(game.paused).toBe(false);
    expect(previewShot(game, { angle: 0, power: 3 })).toBeDefined();
  });

  it('accepts legal shots and keeps previews equal to committed physics', () => {
    const game = resolveVote(createGame({ ...defaultConfig(), seed: 'animation-seed', botCount: 1 }));
    const shot = { angle: 0, power: 3 };
    const frames = previewShot(game, shot)!;
    const committed = applyCommand(game, { type: 'shoot', shot });
    expect(committed.players[0]!.ball.strokes).toBe(1);
    expect(frames.at(-1)?.[0]).toMatchObject({ x: committed.players[0]!.ball.x, y: committed.players[0]!.ball.y, z: committed.players[0]!.ball.z, complete: committed.players[0]!.ball.complete });
  });

  it('feeds selected all-player rules into simulation and expires them when the next vote begins', () => {
    const course = arena('shared-rules');
    let game = gameOn(course, { botCount: 0 });
    game.holeRules = { ...defaultHoleRules(), launchMultiplier: 1.16, collisions: false, sharedBoons: ['heavy ball', 'bank shot'], powerUps: true };
    game.players.forEach((player) => { player.upgrades = [...game.holeRules.sharedBoons]; });
    const before = game.players[0]!.ball;
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.players[0]!.ball.x).toBeGreaterThan(before.x + 2);
    game.players[0]!.ball.complete = true;
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
    expect(game.status).toBe('voting');
    expect(game.players[0]!.upgrades).toEqual(['heavy ball', 'bank shot']);
    game = resolveVote(game);
    expect(game.players[0]!.upgrades).toEqual(game.holeRules.sharedBoons);
  });

  it('scores each resolved hole and finishes after the configured ninth hole', () => {
    let game = resolveVote(createGame({ ...defaultConfig(), seed: 'nine-hole', humanCount: 1, botCount: 0 }));
    for (let hole = 1; hole <= 9; hole += 1) {
      game.players[0]!.ball.complete = true;
      game.players[0]!.ball.strokes = 1;
      game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
      if (hole < 9) game = resolveVote(game);
    }
    expect(game.status).toBe('finished');
    expect(game.players[0]!.total).toBeGreaterThanOrEqual(9);
  });

  it('uses temporary power-ups and produces finite bot decisions', () => {
    const course = arena('items-and-bots');
    let game = gameOn(course);
    game.players[0]!.inventory = 'freeze';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'freeze', targetId: game.players[1]!.id });
    expect(game.players[1]!.frozenTurns).toBe(1);
    const bot = game.players[1]!;
    const decision = chooseBotDecision(game.course, bot, game.players, game.coursePhase, game.holeRules);
    expect(decision.shot.power).toBeGreaterThanOrEqual(1);
    expect(Number.isFinite(decision.shot.angle)).toBe(true);
    game.turn.playerIndex = 1;
    expect(botMove(game)).toBeDefined();
  });

  it('keeps timers, collision settings, and recovery drops under the active rules', () => {
    const course = arena('rules-arena');
    let game = gameOn(course);
    game.holeRules = { ...defaultHoleRules(), timerSeconds: 1, collisions: false, powerUps: true, recoveryBias: 1 };
    game.players[1]!.ball = { ...game.players[1]!.ball, x: 4.5, y: 3.5 };
    const target = { ...game.players[1]!.ball };
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 4 } });
    expect(game.players[1]!.ball).toMatchObject({ x: target.x, y: target.y });
    game = tickTurn({ ...game, turn: { ...game.turn, secondsLeft: .1 } }, 1);
    expect(game.turn.secondsLeft).toBe(game.holeRules.timerSeconds);
    game.players[0]!.total = 3;
    expect(['turbo', 'shield', 'two putts', 'bouncy', 'ice', 'magnet']).toContain(powerUpFor(game, game.players[0]!, 'chaos', 'recovery-bias'));
  });

  it('preserves item-specific effects that the shared rules can enable', () => {
    const course = arena('forms');
    let game = gameOn(course);
    game.players[0]!.inventory = 'portal';
    course.portals = [{ id: 'portal-1', entrance: { point: { x: 3, y: 3 }, direction: { x: 1, y: 0 } }, exit: { point: { x: 8, y: 3 }, direction: { x: 1, y: 0 } } }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'portal', portalExitId: 'portal-1:exit' });
    expect(game.players[0]!.portalExitId).toBe('portal-1:exit');
    game.players[0]!.upgrades = ['chaos magnet'];
    game.holeRules = { ...game.holeRules, powerUps: true };
    game.players[0]!.inventory = 'turbo';
    game.course.seed = Array.from({ length: 12 }, (_, index) => `magnet-${index}`).find((seed) => new Random(`${seed}:human-0:0:turbo`).chance(.65))!;
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'turbo' });
    expect(game.players[0]!.inventory).toBeDefined();
  });
});
