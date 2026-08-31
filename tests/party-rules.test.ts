import { describe, expect, it } from 'vitest';
import { applyCommand, createGame, defaultConfig, tickTurn } from '../src/core/game';
import { validateCourse } from '../src/core/generator';
import { simulateShot } from '../src/core/physics';
import { PARTY_BIOMES, PARTY_LAYOUTS, PARTY_TRICK_CARDS } from '../src/core/rulesets';
import { openShop } from '../src/core/shop';
import { normalizeGameState } from '../src/core/game-state';
import { partyAwardsFor, partyPacingFor, partyReceiptsFor, partyTelemetryReportFor } from '../src/core/party-insights';
import { createArena as arena } from './fixtures';

const partyConfig = (seed: string) => ({ ...defaultConfig(), seed, ruleset: 'party' as const, humanCount: 2, botCount: 0, holeCount: 1 });

const resolveReveal = (state: ReturnType<typeof createGame>) => {
  let current = state;
  for (const player of current.players) current = applyCommand(current, { type: 'ready-slot-spin', playerId: player.id });
  return tickTurn(current, 2);
};

describe('Party Rules vertical slice', () => {
  it('is the default constrained ruleset with the selected biome and layout reels', () => {
    const game = createGame(partyConfig('party-reels'));
    expect(game.config.ruleset).toBe('party');
    expect(PARTY_TRICK_CARDS).toHaveLength(8);
    expect(game.die?.reels.find((reel) => reel.kind === 'biome')?.stops.map((stop) => stop.theme)).toEqual(PARTY_BIOMES);
    expect(game.die?.reels.find((reel) => reel.kind === 'layout')?.stops.map((stop) => stop.archetype)).toEqual(PARTY_LAYOUTS);
  });

  it('permits one influence action per player and one Party Rules chaos modifier', () => {
    let game = createGame(partyConfig('party-influence'));
    const player = game.players[0]!;
    const before = game.die!.reels.find((reel) => reel.id === 'layout')!.stops.length;
    game = applyCommand(game, { type: 'add-slot-stop', playerId: player.id, reelId: 'biome' });
    game = applyCommand(game, { type: 'add-slot-stop', playerId: player.id, reelId: 'layout' });
    expect(game.die!.wagers[player.id]!.influenceActions).toBe(1);
    expect(game.die!.reels.find((reel) => reel.id === 'layout')!.stops).toHaveLength(before);

    game.die!.phase = 'reroll-wagering';
    game.die!.wagers[player.id]!.influenceActions = 0;
    game = applyCommand(game, { type: 'add-chaos-reel', playerId: player.id });
    game.die!.wagers[game.players[1]!.id]!.influenceActions = 0;
    game = applyCommand(game, { type: 'add-chaos-reel', playerId: game.players[1]!.id });
    expect(game.die!.reels.filter((reel) => reel.kind === 'chaos')).toHaveLength(1);
  });

  it('materializes a versioned, replayable recipe and exposes valid route roles', () => {
    const first = resolveReveal(createGame(partyConfig('party-recipe')));
    const second = resolveReveal(createGame(partyConfig('party-recipe')));
    const firstPlan = first.die!.revealed!.plan;
    const secondPlan = second.die!.revealed!.plan;
    expect(firstPlan.recipe.metadata).toMatchObject({ schemaVersion: 1, generatorVersion: 'party-slice-v1', seed: firstPlan.courseSeed });
    expect(firstPlan.recipe.metadata).toEqual(secondPlan.recipe.metadata);
    const restored = normalizeGameState(JSON.parse(JSON.stringify(first)));
    expect(restored.die?.revealed?.plan.recipe.metadata).toEqual(firstPlan.recipe.metadata);
    const generated = first.course;
    expect(validateCourse(generated)).toEqual({ valid: true, failures: [] });
    expect(generated.routeRoles?.map((assignment) => assignment.role)).toEqual(['safe', 'skill', 'conflict']);
  });

  it('uses exactly three clubhouse Trick Card offers and enforces the two-card, one-card-per-shot contract', () => {
    let game = createGame(partyConfig('party-cards'));
    openShop(game, true);
    expect(game.shop?.shelf).toHaveLength(3);
    expect(game.shop?.shelf.every((offer) => PARTY_TRICK_CARDS.includes(offer.contentId as typeof PARTY_TRICK_CARDS[number]))).toBe(true);

    game.status = 'playing';
    const player = game.players[0]!;
    player.pockets = [
      { id: 'heavy', source: 'pad', instanceId: 'heavy' },
      { id: 'shield', source: 'pad', instanceId: 'shield' },
    ];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'heavy', cardId: 'heavy', targetId: player.id });
    expect(game.players[0]!.ballForm).toBe('heavy');
    expect(game.turn.cardPlayed).toBe(true);
    const afterHeavy = applyCommand(game, { type: 'use-power-up', powerUp: 'shield', cardId: 'shield', targetId: player.id });
    expect(afterHeavy.players[0]!.pockets.map((card) => card.id)).toEqual(['shield']);
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2, kind: 'putt' } });
    expect(game.players[0]!.ballForm).toBeUndefined();

    game.turn.playerIndex = 0;
    game.turn.cardPlayed = false;
    game.course.hazards = [{ id: 'party-gate', kind: 'gate', point: { x: 4, y: 3 }, phaseOffset: 0 }];
    game.players[0]!.pockets = [{ id: 'freeze', source: 'pad', instanceId: 'freeze' }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'freeze', cardId: 'freeze', hazardId: 'party-gate' });
    expect(game.players[0]!.frozenObstacleId).toBe('party-gate');
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1, kind: 'putt' } });
    expect(game.players[0]!.frozenObstacleId).toBeUndefined();
  });

  it('keeps ball attacks spatial and records direct collision attribution', () => {
    const course = arena('party-collision');
    const moving = { ...course.tee, x: 1 };
    const active = { x: moving.x + .5, y: moving.y + .5, z: .18, vx: 0, vy: 0, vz: 0, strokes: 0, complete: false, resetCount: 0 };
    const target = { ...active, x: 3.5 };
    const result = simulateShot(course, active, { angle: 0, power: 4 }, 3, { collisions: true, otherBalls: [{ ball: target }] });
    expect(result.collidedOtherIndexes).toContain(0);
  });

  it('turns structured Party Rules telemetry into bounded social receipts and factual awards', () => {
    const game = createGame({ ...partyConfig('party-receipts'), humanCount: 3 });
    game.instrumentation = { events: [
      { type: 'collision', hole: 1, playerId: 'human-0', targetId: 'human-1', detail: 'ball collision' },
      { type: 'card', hole: 1, playerId: 'human-2', targetId: 'human-0', detail: 'airhorn' },
      { type: 'slot-action', hole: 1, playerId: 'human-1', detail: 'hold:biome:ticket' },
      { type: 'turn-duration', hole: 1, playerId: 'human-0', detail: 'shot', value: 9 },
      { type: 'turn-duration', hole: 1, playerId: 'human-1', detail: 'shot', value: 14 },
      { type: 'turn-duration', hole: 1, playerId: 'human-1', detail: 'shot', value: 21 },
      { type: 'hole-duration', hole: 1, detail: 'active seconds', value: 118 },
    ] };
    expect(partyReceiptsFor(game).map((receipt) => receipt.text)).toEqual(['golfer-3 played airhorn on golfer-1', 'golfer-1 banked into golfer-2']);
    expect(partyAwardsFor(game)).toHaveLength(3);
    expect(partyPacingFor(game)).toMatchObject({ measuredTurns: 3, medianTurnSeconds: 14, p90TurnSeconds: 21, medianHoleSeconds: 118, withinTurnBudget: false, withinHoleBudget: true });
  });

  it('exports a shareable telemetry report without player display names', () => {
    const game = createGame(partyConfig('anonymous-telemetry'));
    game.players[0]!.name = 'private person';
    game.instrumentation = { events: [{ type: 'collision', hole: 1, playerId: game.players[0]!.id, targetId: game.players[1]!.id, detail: 'ball contact' }] };
    const report = partyTelemetryReportFor(game, 'party-session-01', '2026-08-31T00:00:00.000Z');
    expect(report).toMatchObject({ schemaVersion: 1, sessionId: 'party-session-01', players: [{ id: 'P1' }, { id: 'P2' }], events: [{ playerId: 'P1', targetId: 'P2' }] });
    expect(JSON.stringify(report)).not.toContain('private person');
  });
});
