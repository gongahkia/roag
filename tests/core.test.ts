import { describe, expect, it } from 'vitest';
import { chooseBotDecision, chooseBotVote } from '../src/core/bots';
import { applyCommand, botMove, createGame, defaultConfig, previewShot, tickTurn } from '../src/core/game';
import { defaultHoleRules, defaultTerrainSettings, generateCandidates, generateCourse, generateVotingOptions, randomTerrainSettings } from '../src/core/generator';
import { newBall, simulateShot, tileAt } from '../src/core/physics';
import { powerUpFor } from '../src/core/powerups';
import { physicsModifiersFor } from '../src/core/player-effects';
import { normalizeGameState } from '../src/core/game-state';
import { Random } from '../src/core/random';
import type { GameState } from '../src/core/types';
import { createArena as arena, gameOn } from './fixtures';

const resolveVote = (game: ReturnType<typeof createGame>, optionIndex = 0) => {
  let resolved = game;
  while (resolved.status === 'voting') {
    const optionId = resolved.vote!.options[Math.min(optionIndex, resolved.vote!.options.length - 1)]!.id;
    resolved = resolved.players.reduce((next, player) => applyCommand(next, { type: 'cast-vote', playerId: player.id, optionId }), resolved);
  }
  return resolved;
};

const resolveShop = (game: ReturnType<typeof createGame>) => {
  let resolved = game;
  if (resolved.status !== 'shopping') return resolved;
  resolved = resolved.players.reduce((next, player) => applyCommand(next, { type: 'shop-vote-reroll', playerId: player.id, approve: false }), resolved);
  while (resolved.status === 'shopping') {
    const playerId = resolved.shop!.buyerOrder[resolved.shop!.buyerIndex]!;
    resolved = applyCommand(resolved, { type: 'shop-skip', playerId });
  }
  return resolved;
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

  it('generates the requested dimensions into every voting package', () => {
    const options = generateVotingOptions('sized-course', 1, { width: 24, height: 16 });
    expect(options).toHaveLength(3);
    expect(options.every((option) => option.course.width === 24 && option.course.height === 16)).toBe(true);
    expect(options.every((option) => option.recipe.terrain.width === 24 && option.recipe.terrain.height === 16)).toBe(true);
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
    const generated = generateCourse('granularity-course', { ...terrain, wallCount: 5, sweeperCount: 2, gateCount: 2, portalPairs: 2, recoveryPads: 3, chaosPads: 4, updraftCount: 2, lowBarCount: 2, airRingCount: 2 });
    expect(terrain).toEqual(randomTerrainSettings('granularity', 2));
    expect(generated.hazards.filter((hazard) => hazard.kind === 'sweeper')).toHaveLength(2);
    expect(generated.hazards.filter((hazard) => hazard.kind === 'gate')).toHaveLength(2);
    expect(generated.portals).toHaveLength(2);
    expect(generated.itemPads.filter((pad) => pad.kind === 'recovery')).toHaveLength(3);
    expect(generated.itemPads.filter((pad) => pad.kind === 'chaos')).toHaveLength(4);
    expect(generated.hazards.filter((hazard) => hazard.kind === 'updraft')).toHaveLength(2);
    expect(generated.hazards.filter((hazard) => hazard.kind === 'low-bar')).toHaveLength(2);
    expect(generated.features.filter((feature) => feature.kind === 'air-ring')).toHaveLength(2);
  });

  it('keeps generated courses guarded and solver-playable', () => {
    const course = generateCourse('edge-seed', { ...defaultTerrainSettings(), wallCount: 3 });
    const start = newBall(course);
    const result = simulateShot(course, start, { angle: Math.PI, power: 8 });
    expect(course.tiles.some((tile) => tile.surface === 'wall')).toBe(true);
    expect(result.reset).toBe(false);
    expect(result.ball.x).toBeGreaterThan(start.x - .5);
  });

  it('gives every abstract biome a deterministic presentation feature without changing the shared terrain controls', () => {
    const themes = ['drift', 'bloom', 'pulse'] as const;
    for (const theme of themes) {
      const first = generateCourse(`biome-${theme}`, { ...defaultTerrainSettings(), theme, chaos: .8 });
      const second = generateCourse(`biome-${theme}`, { ...defaultTerrainSettings(), theme, chaos: .8 });
      expect(first.theme).toBe(theme);
      expect(first.features).toEqual(second.features);
      expect(first.features.length).toBeGreaterThan(0);
    }
  });

  it('keeps biome feature quantities in the backend recipe for game logic to tune', () => {
    const drift = generateCourse('drift-quantities', { ...defaultTerrainSettings(), theme: 'drift', sinkholePairs: 2 });
    const bloom = generateCourse('bloom-quantities', { ...defaultTerrainSettings(), theme: 'bloom', thornCount: 3 });
    const pulse = generateCourse('pulse-quantities', { ...defaultTerrainSettings(), theme: 'pulse', pulseCount: 3 });
    expect(drift.features.filter((feature) => feature.kind === 'sinkhole')).toHaveLength(2);
    expect(bloom.features.filter((feature) => feature.kind === 'thorn')).toHaveLength(3);
    expect(pulse.features.filter((feature) => feature.kind === 'pulse')).toHaveLength(3);
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

  it('quick-starts with a deterministic random package locked for every configured hole', () => {
    const config = { ...defaultConfig(), seed: 'quick-start', holeCount: 3, humanCount: 1, botCount: 1, courseWidth: 24, courseHeight: 16, skipVoting: true };
    const first = createGame(config);
    const second = createGame(config);
    expect(first.status).toBe('playing');
    expect(first.vote).toBeUndefined();
    expect(first.coursePlan).toHaveLength(3);
    expect(first.coursePlan).toEqual(second.coursePlan);
    expect(first.course.seed).toBe(first.coursePlan[0]!.courseSeed);
    expect(first.course).toMatchObject({ width: 24, height: 16 });
    expect(first.coursePlan.every((plan) => plan.recipe.terrain.width === 24 && plan.recipe.terrain.height === 16)).toBe(true);
    expect(first.players.every((player) => player.caddies.length === 0 && player.cash === 10)).toBe(true);
  });

  it('keeps named ballots public and editable until the final ballot locks the whole match plan', () => {
    let game = createGame({ ...defaultConfig(), seed: 'public-ballot', holeCount: 1, humanCount: 2, botCount: 1 });
    const [first, second, third] = game.vote!.options;
    game = applyCommand(game, { type: 'cast-vote', playerId: 'human-0', optionId: first!.id });
    game = applyCommand(game, { type: 'cast-vote', playerId: 'human-0', optionId: second!.id });
    game = applyCommand(game, { type: 'cast-vote', playerId: 'human-1', optionId: second!.id });
    expect(game.status).toBe('voting');
    expect(game.vote!.ballots['human-0']).toBe(second!.id);
    game = applyCommand(game, { type: 'cast-vote', playerId: 'bot-0', optionId: third!.id });
    expect(game.status).toBe('playing');
    expect(game.course.seed).toBe(second!.course.seed);
    expect(game.coursePlan).toMatchObject([{ id: second!.id, label: second!.label, courseSeed: second!.course.seed }]);
    expect(game.players.every((player) => player.caddies.length === 0 && player.cash === 10)).toBe(true);
  });

  it('collects every hole package before opening the first turn and transitions through the locked plan', () => {
    let game = createGame({ ...defaultConfig(), seed: 'frontloaded-plan', holeCount: 2, humanCount: 1, botCount: 0 });
    const first = game.vote!.options[1]!;
    game = game.players.reduce((next, player) => applyCommand(next, { type: 'cast-vote', playerId: player.id, optionId: first.id }), game);
    expect(game.status).toBe('voting');
    expect(game.coursePlan).toHaveLength(1);
    const second = game.vote!.options[2]!;
    game = game.players.reduce((next, player) => applyCommand(next, { type: 'cast-vote', playerId: player.id, optionId: second.id }), game);
    expect(game.status).toBe('playing');
    expect(game.coursePlan.map((plan) => plan.id)).toEqual([first.id, second.id]);
    game.players[0]!.ball.complete = true;
    game.players[0]!.ball.strokes = 1;
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
    expect(game.status).toBe('shopping');
    game = resolveShop(game);
    expect(game.status).toBe('transitioning');
    expect(game.transition?.next.id).toBe(second.id);
    game = applyCommand(game, { type: 'complete-transition' });
    expect(game.status).toBe('playing');
    expect(game.course.seed).toBe(second.course.seed);
  });

  it('breaks tied pluralities and bot ballots deterministically', () => {
    const config = { ...defaultConfig(), seed: 'tied-ballot', holeCount: 1, humanCount: 1, botCount: 1 };
    const initial = createGame(config);
    expect(chooseBotVote(initial.config.seed, 1, initial.players[1]!, initial.vote!.options)).toBe(chooseBotVote(initial.config.seed, 1, initial.players[1]!, initial.vote!.options));
    const options = initial.vote!.options;
    let first = applyCommand(initial, { type: 'cast-vote', playerId: 'human-0', optionId: options[0]!.id });
    first = applyCommand(first, { type: 'cast-vote', playerId: 'bot-0', optionId: options[1]!.id });
    let second = applyCommand(createGame(config), { type: 'cast-vote', playerId: 'human-0', optionId: options[0]!.id });
    second = applyCommand(second, { type: 'cast-vote', playerId: 'bot-0', optionId: options[1]!.id });
    expect(first.course.seed).toBe(second.course.seed);
  }, 15_000);

  it('rejects unknown voters and options without advancing the ballot', () => {
    const game = createGame({ ...defaultConfig(), seed: 'invalid-vote', botCount: 0 });
    expect(applyCommand(game, { type: 'cast-vote', playerId: 'unknown', optionId: 'missing' })).toEqual(game);
  });
});

describe('turns, shared rules, and bots', () => {
  it('freezes turns and shots while paused, then resumes the same match state', () => {
    let game = resolveVote(createGame({ ...defaultConfig(), seed: 'pause-state', holeCount: 1, humanCount: 1, botCount: 0 }));
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
    const game = resolveVote(createGame({ ...defaultConfig(), seed: 'animation-seed', holeCount: 1, botCount: 1 }));
    const shot = { angle: 0, power: 3 };
    const frames = previewShot(game, shot)!;
    const committed = applyCommand(game, { type: 'shoot', shot });
    expect(committed.players[0]!.ball.strokes).toBe(1);
    expect(frames.at(-1)?.[0]).toMatchObject({ x: committed.players[0]!.ball.x, y: committed.players[0]!.ball.y, z: committed.players[0]!.ball.z, complete: committed.players[0]!.ball.complete });
  }, 15_000);

  it('feeds selected all-player rules into simulation and refreshes them when the next planned course lands', () => {
    const course = arena('shared-rules');
    let game = gameOn(course, { botCount: 0, holeCount: 2 });
    game.holeRules = { ...defaultHoleRules(), launchMultiplier: 1.16, collisions: false, sharedBoons: ['heavy ball', 'bank shot'], powerUps: true };
    game.players.forEach((player) => { player.upgrades = [...game.holeRules.sharedBoons]; });
    const before = game.players[0]!.ball;
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.players[0]!.ball.x).toBeGreaterThan(before.x + 2);
    game.players[0]!.ball.complete = true;
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
    expect(game.status).toBe('shopping');
    game = resolveShop(game);
    expect(game.status).toBe('transitioning');
    expect(game.players[0]!.upgrades).toEqual(['heavy ball', 'bank shot']);
    game = applyCommand(game, { type: 'complete-transition' });
    expect(game.players[0]!.upgrades).toEqual(['heavy ball', 'bank shot']);
  });

  it('scores each resolved hole and finishes after the configured ninth hole', () => {
    let game = resolveVote(createGame({ ...defaultConfig(), seed: 'nine-hole', humanCount: 1, botCount: 0 }));
    for (let hole = 1; hole <= 9; hole += 1) {
      game.players[0]!.ball.complete = true;
      game.players[0]!.ball.strokes = 1;
      game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
      if (hole < 9) {
        expect(game.status).toBe('shopping');
        game = resolveShop(game);
        game = applyCommand(game, { type: 'complete-transition' });
      }
    }
    expect(game.status).toBe('finished');
    expect(game.players[0]!.total).toBeGreaterThanOrEqual(9);
  }, 30_000);

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

  it('adds aerial forms, team boons, tactical items, and a second gadget slot', () => {
    const course = arena('expanded-content');
    course.route = [{ x: 1, y: 3 }, { x: 4, y: 3 }, { x: 7, y: 3 }, course.cup];
    let game = gameOn(course);
    const player = game.players[0]!;
    player.upgrades = ['aerial ace', 'cup reader', 'gadgeteer'];
    player.ballForm = 'glider';
    const modifiers = physicsModifiersFor(player, game.holeRules);
    expect(modifiers.chipGravityMultiplier).toBeLessThan(.6);
    expect(modifiers.cupRadius).toBeGreaterThan(game.holeRules.cupRadius);

    player.inventory = 'sky spring';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'sky spring', placement: { x: 4, y: 3 } });
    game.players[0]!.inventory = 'popper pad';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'popper pad', placement: { x: 5, y: 3 } });
    expect(game.gadgets).toHaveLength(2);

    const beforeDrone = game.players[0]!.ball.x;
    game.players[0]!.inventory = 'rescue drone';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'rescue drone' });
    expect(game.players[0]!.ball.x).toBeGreaterThan(beforeDrone);

    game.players[0]!.inventory = 'airhorn';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'airhorn', targetId: game.players[1]!.id });
    game.turn.playerIndex = 1;
    const forcedFrames = previewShot(game, { angle: 0, power: 3, kind: 'putt' })!;
    expect(game.players[1]!.forcedChip).toBe(true);
    expect(Math.max(...forcedFrames.map((frame) => frame[1]!.z))).toBeGreaterThan(game.players[1]!.ball.z + .5);
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
    expect(['turbo', 'shield', 'two putts', 'bouncy', 'ice', 'magnet', 'cup magnet', 'slipstream', 'rebound rig', 'popper pad', 'snare patch', 'blast mine', 'slick patch', 'phase shift', 'sandbag']).toContain(powerUpFor(game, game.players[0]!, 'chaos', 'recovery-bias'));
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

  it('places one legal universal gadget per player and keeps legacy snapshots readable', () => {
    const course = arena('gadget-placement');
    let game = gameOn(course, { botCount: 0 });
    game.players[0]!.inventory = 'blast mine';
    const invalid = applyCommand(game, { type: 'use-power-up', powerUp: 'blast mine', placement: course.tee });
    expect(invalid.gadgets).toHaveLength(0);
    expect(invalid.players[0]!.inventory).toBe('blast mine');
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'blast mine', placement: { x: 5, y: 3 } });
    expect(game.gadgets).toMatchObject([{ ownerId: 'human-0', kind: 'blast mine', point: { x: 5, y: 3 } }]);
    game.players[0]!.inventory = 'slick patch';
    expect(applyCommand(game, { type: 'use-power-up', powerUp: 'slick patch', placement: { x: 6, y: 3 } }).gadgets).toHaveLength(1);

    const legacy = structuredClone(game) as unknown as { gadgets?: unknown; course: { features?: unknown; theme?: unknown } };
    delete legacy.gadgets;
    delete legacy.course.features;
    delete legacy.course.theme;
    const restored = normalizeGameState(legacy as unknown as GameState);
    expect(restored.gadgets).toEqual([]);
    expect(restored.course).toMatchObject({ theme: 'balanced', features: [] });
  });
});
