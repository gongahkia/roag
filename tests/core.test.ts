import { describe, expect, it } from 'vitest';
import { chooseBotDecision, chooseBotDieAction } from '../src/core/bots';
import { CAMPAIGN_EXCAVATION_RADIUS, expandCourseAtCup } from '../src/core/campaign';
import { CONTENT } from '../src/core/catalog';
import { applyCommand, botMove, createGame, defaultConfig, previewShot, tickTurn } from '../src/core/game';
import { courseHashFor, defaultHoleRules, defaultTerrainSettings, generateCandidates, generateCourse, generateCoursePackages, randomTerrainSettings } from '../src/core/generator';
import { newBall, simulateShot, tileAt, tileCornerHeights } from '../src/core/physics';
import { powerUpFor } from '../src/core/powerups';
import { physicsModifiersFor } from '../src/core/player-effects';
import { courseForPlan, expansionForTransition, normalizeGameState } from '../src/core/game-state';
import { Random } from '../src/core/random';
import { GENERATOR_VERSION, LEGACY_GENERATOR_VERSION } from '../src/core/rulesets';
import { buyShopOffer, chooseBotShopOffer, openShop } from '../src/core/shop';
import type { GameState, PlannedHole } from '../src/core/types';
import { createArena as arena, gameOn } from './fixtures';

const resolveDie = (game: ReturnType<typeof createGame>) => {
  let resolved = game;
  while (resolved.status === 'rolling') {
    if (resolved.die && (resolved.die.phase === 'wagering' || resolved.die.phase === 'reroll-wagering')) resolved = resolved.players.reduce((next, player) => applyCommand(next, { type: 'ready-slot-spin', playerId: player.id }), resolved);
    resolved = tickTurn(resolved, 10);
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

  it('rebuilds historical recipes with their recorded generator geometry', () => {
    const seed = 'generator-v1-compatibility';
    const terrain = { ...defaultTerrainSettings(), laneWidth: 1, branches: 1, theme: 'balanced' as const };
    const rules = defaultHoleRules();
    const v1 = generateCourse(seed, terrain, rules.hazardPhaseCount, LEGACY_GENERATOR_VERSION);
    const v2 = generateCourse(seed, terrain, rules.hazardPhaseCount, GENERATOR_VERSION);
    const legacyPlan: PlannedHole = {
      id: 'legacy-generator-plan',
      label: 'legacy generator plan',
      courseSeed: seed,
      recipe: {
        terrain,
        rules,
        metadata: {
          schemaVersion: 1,
          generatorVersion: LEGACY_GENERATOR_VERSION,
          seed,
          resolvedReels: { biome: 'biome', layout: 'layout', rules: 'rules' },
          contentIds: [],
        },
      },
    };

    expect(courseHashFor(v1)).toBe('gwye-1a18f901');
    expect(courseHashFor(v2)).toBe('gwye-4d087f19');
    expect(courseHashFor(v1)).not.toBe(courseHashFor(v2));
    expect(courseHashFor(courseForPlan(legacyPlan))).toBe(courseHashFor(v1));
    expect(courseHashFor(courseForPlan({ ...legacyPlan, recipe: { ...legacyPlan.recipe, metadata: undefined } }))).toBe(courseHashFor(v1));
  });

  it('stitches the next generated hole onto the completed cup without rebuilding the prior course', () => {
    const previous = arena('campaign-previous');
    previous.tiles[previous.cup.y * previous.width + previous.cup.x] = { surface: 'cup', height: 3, corners: [3, 3, 3, 3] };
    previous.tiles[previous.cup.y * previous.width + previous.cup.x - 1] = { surface: 'wall', height: 3 };
    const next = { ...arena('campaign-next'), theme: 'speedway' as const };
    next.route = [next.tee, { x: next.tee.x + 1, y: next.tee.y }, { x: next.tee.x + 2, y: next.tee.y }, next.cup];
    const first = expandCourseAtCup(previous, next);
    const second = expandCourseAtCup(previous, next);
    expect(first.course).toEqual(second.course);
    expect(first.rotation).toBe(second.rotation);
    expect(first.course.tee).toEqual(first.anchor);
    expect(first.course.tiles[first.anchor.y * first.course.width + first.anchor.x]).toMatchObject({ surface: 'tee', theme: 'speedway' });
    expect(first.excavated.every((point) => Math.max(Math.abs(point.x - first.anchor.x), Math.abs(point.y - first.anchor.y)) <= CAMPAIGN_EXCAVATION_RADIUS)).toBe(true);
    expect(first.course.tiles[first.previous.tee.y * first.course.width + first.previous.tee.x]).toMatchObject({ surface: 'tee', theme: 'balanced' });
    expect(first.added.length).toBeGreaterThan(0);
    expect(first.previous.route.filter((point) => point.x !== first.anchor.x || point.y !== first.anchor.y).every((point) => first.course.tiles[point.y * first.course.width + point.x]?.surface === first.previous.tiles[point.y * first.course.width + point.x]?.surface)).toBe(true);
    expect(first.course.route.every((point) => {
      const surface = first.course.tiles[point.y * first.course.width + point.x]?.surface;
      return surface !== 'void' && surface !== 'wall';
    })).toBe(true);
    expect(first.course.tiles[first.course.cup.y * first.course.width + first.course.cup.x]?.height).toBeCloseTo(3);
    expect(first.course.tiles[first.anchor.y * first.course.width + first.anchor.x - 1]?.surface).not.toBe('wall');
    expect(first.excavated.filter((point) => point.x !== first.anchor.x || point.y !== first.anchor.y).every((point) => {
      const tile = first.course.tiles[point.y * first.course.width + point.x];
      return tile?.surface !== 'void';
    })).toBe(true);
    for (let y = 0; y < first.course.height; y += 1) for (let x = 0; x < first.course.width; x += 1) {
      const tile = first.course.tiles[y * first.course.width + x]!;
      if (tile.surface === 'void') continue;
      if (x < first.course.width - 1) {
        const right = first.course.tiles[y * first.course.width + x + 1]!;
        if (right.surface !== 'void') {
          expect(tileCornerHeights(tile)[1]).toBe(tileCornerHeights(right)[0]);
          expect(tileCornerHeights(tile)[2]).toBe(tileCornerHeights(right)[3]);
        }
      }
      if (y < first.course.height - 1) {
        const below = first.course.tiles[(y + 1) * first.course.width + x]!;
        if (below.surface !== 'void') {
          expect(tileCornerHeights(tile)[3]).toBe(tileCornerHeights(below)[0]);
          expect(tileCornerHeights(tile)[2]).toBe(tileCornerHeights(below)[1]);
        }
      }
    }
  });

  it('uses compact, standard, and full course packages within the host dimensions', () => {
    const options = generateCoursePackages('sized-course', 1, { width: 24, height: 16 });
    expect(options).toHaveLength(3);
    expect(options.map((option) => option.recipe.terrain.sizeProfile)).toEqual(['compact', 'standard', 'full']);
    expect(options.map((option) => option.course.width)).toEqual([16, 20, 24]);
    expect(options.map((option) => option.course.height)).toEqual([11, 13, 16]);
    expect(options.every((option) => option.course.width === option.recipe.terrain.width && option.course.height === option.recipe.terrain.height)).toBe(true);
    expect(options.every((option) => option.course.width <= 24 && option.course.height <= 16)).toBe(true);
  });

  it('keeps large requested dimensions instead of clamping them to the old board limit', () => {
    const dimensions = { width: 64, height: 36 };
    const course = generateCourse('large-course', { ...defaultTerrainSettings(), ...dimensions });
    const options = generateCoursePackages('large-options', 1, dimensions);
    expect(course).toMatchObject(dimensions);
    expect(course.cup.x).toBeGreaterThan(24);
    expect(options.map((option) => option.recipe.terrain.sizeProfile)).toEqual(['compact', 'standard', 'full']);
    expect(options[2]!.course).toMatchObject(dimensions);
    expect(options.every((option) => option.course.width <= dimensions.width && option.course.height <= dimensions.height)).toBe(true);
    expect(options[2]!.recipe.rules.strokeCap).toBeGreaterThanOrEqual(13);
  });

  it('keeps generated packages free of shared boons and starting supplies', () => {
    const options = generateCoursePackages('clubhouse-only', 1);
    expect(options.every((option) => option.recipe.rules.sharedBoons.length === 0 && option.recipe.rules.startingPowerUp === undefined)).toBe(true);
  });

  it('keeps solver routes inside playable terrain and across every configured phase', () => {
    const options = generateCoursePackages('phase-set', 3);
    expect(options).toHaveLength(3);
    for (const option of options) {
      expect(option.course.route.every((point) => tileAt(option.course, point.x + .5, point.y + .5)?.surface !== 'void')).toBe(true);
      for (let phase = 0; phase < option.recipe.rules.hazardPhaseCount; phase += 1) {
        expect(simulateShot(option.course, newBall(option.course), option.course.score.solverShots[0]!, 10, { phase, phaseCount: option.recipe.rules.hazardPhaseCount }).holed).toBe(true);
      }
    }
  });

  it('exposes deterministic granular recipes including independent surface, hazard, portal, pad, and gust quantities', () => {
    const terrain = randomTerrainSettings('granularity', 2);
    const generated = generateCourse('granularity-course', { ...terrain, wallCount: 5, sweeperCount: 2, gateCount: 2, portalPairs: 2, recoveryPads: 3, chaosPads: 4, updraftCount: 2, lowBarCount: 2, airRingCount: 2, gustCount: 2 });
    expect(terrain).toEqual(randomTerrainSettings('granularity', 2));
    expect(generated.hazards.filter((hazard) => hazard.kind === 'sweeper')).toHaveLength(2);
    expect(generated.hazards.filter((hazard) => hazard.kind === 'gate')).toHaveLength(2);
    expect(generated.portals).toHaveLength(2);
    expect(generated.itemPads.filter((pad) => pad.kind === 'recovery')).toHaveLength(3);
    expect(generated.itemPads.filter((pad) => pad.kind === 'chaos')).toHaveLength(4);
    expect(generated.hazards.filter((hazard) => hazard.kind === 'updraft')).toHaveLength(2);
    expect(generated.hazards.filter((hazard) => hazard.kind === 'low-bar')).toHaveLength(2);
    expect(generated.features.filter((feature) => feature.kind === 'air-ring')).toHaveLength(2);
    expect(generated.features.filter((feature) => feature.kind === 'gust')).toHaveLength(2);
  });

  it('keeps new terrain tiles off the primary route while retaining deterministic specialist terrain', () => {
    const spring = generateCourse('spring-terrain', { ...defaultTerrainSettings(), density: 1, roughRate: 0, sandRate: 0, iceRate: 0, boosterRate: 0, conveyorRate: 0, cushionRate: 0, springRate: 1, bumperCount: 3, gustCount: 2, theme: 'carnival' });
    const cushion = generateCourse('cushion-terrain', { ...defaultTerrainSettings(), density: 1, roughRate: 0, sandRate: 0, iceRate: 0, boosterRate: 0, conveyorRate: 0, cushionRate: 1, springRate: 0, bumperCount: 0, gustCount: 0, theme: 'marsh' });
    expect(spring.tiles.some((tile) => tile.surface === 'spring')).toBe(true);
    expect(spring.tiles.filter((tile) => tile.surface === 'bumper')).toHaveLength(5);
    expect(spring.features.filter((feature) => feature.kind === 'gust')).toHaveLength(2);
    expect(cushion.tiles.some((tile) => tile.surface === 'cushion')).toBe(true);
    expect([...spring.route, ...cushion.route].every((point) => {
      const course = spring.route.includes(point) ? spring : cushion;
      const tile = tileAt(course, point.x + .5, point.y + .5);
      return tile?.surface !== 'spring' && tile?.surface !== 'bumper' && tile?.surface !== 'void';
    })).toBe(true);
  });

  it('keeps a seeded package corpus varied and reproducible across terrain, shape, and size', () => {
    const corpus = ['terrain-corpus-01', 'terrain-corpus-02'];
    const generated = corpus.map((seed) => generateCoursePackages(seed, 2, { width: 28, height: 18 }));
    const reproduced = generateCoursePackages(corpus[0]!, 2, { width: 28, height: 18 });
    expect(generated[0]!.map((option) => option.course)).toEqual(reproduced.map((option) => option.course));
    for (const options of generated) {
      expect(new Set(options.map((option) => option.course.theme)).size).toBe(3);
      expect(new Set(options.map((option) => option.course.archetype)).size).toBe(3);
      expect(options.every((option) => option.course.score.playable)).toBe(true);
    }
  }, 30_000);

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

describe('clubhouse economy and reality cards', () => {
  it('defines a large, unique data-driven merchant catalog', () => {
    expect(CONTENT).toHaveLength(150);
    expect(new Set(CONTENT.map((entry) => entry.id)).size).toBe(CONTENT.length);
    expect(new Set(CONTENT.map((entry) => entry.category))).toEqual(new Set(['caddy', 'pocket', 'form', 'gadget', 'reality', 'chrono']));
  });

  it('opens a deterministic shared shelf after a hole and orders buyers by sink order', () => {
    let game = gameOn(arena('merchant-order'), { holeCount: 2, humanCount: 2, botCount: 0 });
    game.players[0]!.ball.complete = true;
    game.players[0]!.ball.strokes = 5;
    game.players[0]!.holeFinishOrder = 1;
    game.players[1]!.ball.complete = true;
    game.players[1]!.ball.strokes = 2;
    game.players[1]!.holeFinishOrder = 0;
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
    expect(game.status).toBe('shopping');
    expect(game.shop?.buyerOrder).toEqual([game.players[1]!.id, game.players[0]!.id]);
    expect(game.shop?.shelf.map((offer) => offer.category)).toEqual(['caddy', 'caddy', 'caddy', expect.any(String), expect.any(String), 'reality', 'chrono']);
    game = game.players.reduce((next, player) => applyCommand(next, { type: 'shop-vote-reroll', playerId: player.id, approve: false }), game);
    const buyer = game.players[1]!;
    buyer.cash = 20;
    const caddyOffer = game.shop!.shelf.find((offer) => offer.category === 'caddy')!;
    game = applyCommand(game, { type: 'shop-buy', playerId: buyer.id, offerId: caddyOffer.id });
    expect(game.players[1]!.caddies).toEqual([{ id: caddyOffer.contentId, stacks: 1 }]);
    expect(game.players[1]!.cash).toBe(14);
  });

  it('has bots rank legal merchant offers instead of taking the leftmost affordable card', () => {
    const game = gameOn(arena('shop-bot-ranking'), { humanCount: 0, botCount: 1 });
    const bot = game.players[0]!;
    bot.cash = 20;
    game.status = 'shopping';
    game.shop = {
      visit: 1,
      opening: true,
      shelf: [
        { id: 'leftmost-pocket', contentId: 'turbo', category: 'pocket', price: 4 },
        { id: 'reality-priority', contentId: 'wall is cup', category: 'reality', price: 8 },
      ],
      buyerOrder: [bot.id],
      buyerIndex: 0,
      completedBuyerIds: [],
      rerollVotes: { [bot.id]: false },
      rerollResolved: true,
      rerolled: false,
      secondsLeft: 20,
    };
    expect(chooseBotShopOffer(game, bot)?.id).toBe('reality-priority');
  });

  it('keeps a shop card’s visible duration on the purchased card instance', () => {
    const game = gameOn(arena('shop-card-duration'), { botCount: 0, holeCount: 2 });
    openShop(game, true);
    game.shop!.rerollResolved = true;
    game.shop!.shelf = [{ id: 'duration-offer', contentId: 'fairway draft', category: 'pocket', price: 5, duration: { unit: 'round', amount: 3 } }];
    game.players[0]!.cash = 10;
    buyShopOffer(game, game.players[0]!.id, 'duration-offer');
    expect(game.players[0]!.pockets).toMatchObject([{ id: 'fairway draft', source: 'shop', duration: { unit: 'round', amount: 3 } }]);
    expect(game.players[0]!.pockets[0]!.instanceId).toMatch(/^shop-card-/);
  });

  it('makes wall-is-cup and reverse controls deterministic simulation rules', () => {
    const course = arena('reality-wall');
    course.tiles[course.tee.y * course.width + (course.tee.x + 1)] = { surface: 'wall', height: 0 };
    expect(simulateShot(course, newBall(course), { angle: 0, power: 2 }, 3, { reality: 'wall is cup' }).holed).toBe(true);
    const game = gameOn(course, { botCount: 0 });
    const player = game.players[0]!;
    player.controlInverted = 1;
    expect(physicsModifiersFor(player).mass).toBeGreaterThan(0);
    const reversed = previewShot({ ...game, players: [player], turn: { playerIndex: 0, secondsLeft: 24, shotInFlight: false } }, { angle: 0, power: 2 });
    expect(reversed?.at(-1)?.[0].x).toBeLessThan(player.ball.x);
  });
});

/* Retired public-ballot coverage. The active coverage below exercises the die flow.
describe('public voting flow', () => {
  it('starts every match with three reproducible public voting options and no build state', () => {
    const first = createGame({ ...defaultConfig(), seed: 'ballot-seed', humanCount: 2, botCount: 1 });
    const second = createGame({ ...defaultConfig(), seed: 'ballot-seed', humanCount: 2, botCount: 1 });
    expect(first.status).toBe('voting');
    expect(first.vote?.options.map((option) => option.id)).toEqual(second.vote?.options.map((option) => option.id));
    expect(first.vote?.options.map((option) => option.course.tiles)).toEqual(second.vote?.options.map((option) => option.course.tiles));
  });

  it('quick-starts with a deterministic random package locked for every configured hole', () => {
    const config = { ...defaultConfig(), seed: 'quick-start', holeCount: 1, humanCount: 1, botCount: 1, courseWidth: 24, courseHeight: 16, skipVoting: true };
    const first = createGame(config);
    const second = createGame(config);
    expect(first.status).toBe('playing');
    expect(first.vote).toBeUndefined();
    expect(first.coursePlan).toHaveLength(1);
    expect(first.coursePlan).toEqual(second.coursePlan);
    expect(first.course.seed).toBe(first.coursePlan[0]!.courseSeed);
    expect(first.course.width).toBeLessThanOrEqual(24);
    expect(first.course.height).toBeLessThanOrEqual(16);
    expect(first.coursePlan.every((plan) => plan.recipe.terrain.width <= 24 && plan.recipe.terrain.height <= 16)).toBe(true);
    expect(first.players.every((player) => player.caddies.length === 0 && player.cash === 10)).toBe(true);
  }, 15_000);

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
    expect(game.status).toBe('rolling');
    // The assertion is about reset semantics, not full-catalog decoration.
    game.config.ruleset = 'party';
    game = resolveDie(game);
    expect(game.status).toBe('transitioning');
    expect(game.transition?.next.id).toBe(second.id);
    const expansion = expansionForTransition(game)!;
    game = applyCommand(game, { type: 'complete-transition' });
    expect(game.status).toBe('playing');
    expect(game.course.tee).toEqual(expansion.anchor);
    expect(game.course.cup).toEqual(expansion.course.cup);
    expect(game.course.tiles[game.course.tee.y * game.course.width + game.course.tee.x]?.surface).toBe('tee');
    expect(game.players.every((player) => player.ball.x === game.course.tee.x + .5 && player.ball.y === game.course.tee.y + .5)).toBe(true);
    expect(game.course.width * game.course.height).toBeGreaterThanOrEqual(second.course.width * second.course.height);
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
}); */

describe('shared course slot flow', () => {
  it('starts every match with three reproducible component reels', () => {
    const first = createGame({ ...defaultConfig(), seed: 'die-seed', humanCount: 2, botCount: 1 });
    const second = createGame({ ...defaultConfig(), seed: 'die-seed', humanCount: 2, botCount: 1 });
    expect(first.status).toBe('rolling');
    expect(first.die?.reels).toHaveLength(3);
    expect(first.die?.reels.map((reel) => reel.stops.map((stop) => stop.id))).toEqual(second.die?.reels.map((reel) => reel.stops.map((stop) => stop.id)));
    expect(first.die?.reels.map((reel) => reel.kind)).toEqual(['biome', 'layout', 'rules']);
    expect(first.die?.reels.every((reel) => reel.stops.every((stop) => stop.weight === 1))).toBe(true);
  });

  it('loads generated stops and duplicate tickets before every shared spin', () => {
    let game = createGame({ ...defaultConfig(), ruleset: 'custom', seed: 'weighted-die', holeCount: 1, humanCount: 1, botCount: 0 });
    game.players[0]!.cash = 40;
    const reelId = game.die!.reels[0]!.id;
    const stopId = game.die!.reels[0]!.stops[0]!.id;
    game = applyCommand(game, { type: 'add-slot-stop', playerId: 'human-0', reelId });
    game = applyCommand(game, { type: 'add-slot-stop', playerId: 'human-0', reelId });
    game = applyCommand(game, { type: 'augment-slot-stop', playerId: 'human-0', reelId, stopId });
    game = applyCommand(game, { type: 'augment-slot-stop', playerId: 'human-0', reelId, stopId });
    expect(game.die?.reels[0]?.stops).toHaveLength(5);
    expect(game.die?.reels[0]?.stops.find((stop) => stop.id === stopId)?.weight).toBe(3);
    expect(game.players[0]!.cash).toBe(35);
    expect(game.die?.wagers['human-0']).toMatchObject({ addedStops: 2, augmentations: { [`${reelId}:${stopId}`]: 2 } });
    game = applyCommand(game, { type: 'ready-slot-spin', playerId: 'human-0' });
    expect(game.die?.roll).toBeDefined();
    game = tickTurn(game, 2);
    expect(game.die?.phase).toBe('revealed');
    game = tickTurn(game, 10);
    expect(game.status).toBe('playing');
    expect(game.coursePlan).toHaveLength(1);
  });

  it('uses the same weighted result for the same wagers and makes bots deterministic participants', () => {
    const config = { ...defaultConfig(), seed: 'deterministic-die', holeCount: 1, humanCount: 1, botCount: 1 };
    const initial = createGame(config);
    const bot = initial.players[1]!;
    expect(chooseBotDieAction(initial.config.seed, 1, bot, initial.die!)).toEqual(chooseBotDieAction(initial.config.seed, 1, bot, initial.die!));
    const reelId = initial.die!.reels[2]!.id;
    const stopId = initial.die!.reels[2]!.stops[0]!.id;
    const resolve = (game: ReturnType<typeof createGame>) => {
      let next = applyCommand(game, { type: 'augment-slot-stop', playerId: 'human-0', reelId, stopId });
      next = applyCommand(next, { type: 'ready-slot-spin', playerId: 'human-0' });
      next = applyCommand(next, { type: 'ready-slot-spin', playerId: 'bot-0' });
      next = tickTurn(next, 2);
      return tickTurn(next, 10);
    };
    expect(resolve(initial).course.seed).toBe(resolve(createGame(config)).course.seed);
  }, 15_000);

  it('opens a fresh slot machine after the merchant instead of locking future holes up front', () => {
    let game = resolveDie(createGame({ ...defaultConfig(), seed: 'per-hole-die', holeCount: 2, humanCount: 1, botCount: 0 }));
    expect(game.coursePlan).toHaveLength(1);
    game.players[0]!.ball.complete = true;
    game.players[0]!.ball.strokes = 1;
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
    game = resolveShop(game);
    expect(game.status).toBe('rolling');
    expect(game.die?.reels).toHaveLength(3);
    expect(game.coursePlan).toHaveLength(1);
    game = resolveDie(game);
    expect(game.status).toBe('transitioning');
    expect(game.coursePlan).toHaveLength(2);
    const expansion = expansionForTransition(game)!;
    game = applyCommand(game, { type: 'complete-transition' });
    expect(game.status).toBe('playing');
    expect(game.course.tee).toEqual(expansion.anchor);
  }, 15_000);

  it('rejects unknown slot bettors and stops', () => {
    const game = createGame({ ...defaultConfig(), seed: 'invalid-die', botCount: 0 });
    expect(applyCommand(game, { type: 'add-slot-stop', playerId: 'unknown', reelId: 'biome' })).toEqual(game);
    expect(applyCommand(game, { type: 'augment-slot-stop', playerId: 'human-0', reelId: 'biome', stopId: 'missing' })).toEqual(game);
  });

  it('refunds an unfinished reroll pot and allows two funded rerolls with chaos reels', () => {
    let game = createGame({ ...defaultConfig(), seed: 'reroll-slots', holeCount: 1, humanCount: 1, botCount: 0 });
    game.players[0]!.cash = 20;
    game = applyCommand(game, { type: 'ready-slot-spin', playerId: 'human-0' });
    game = tickTurn(game, 2);
    const beforeRefund = game.players[0]!.cash;
    game = applyCommand(game, { type: 'contribute-reroll', playerId: 'human-0' });
    game = tickTurn(game, 10);
    expect(game.players[0]!.cash).toBe(beforeRefund);

    game = createGame({ ...defaultConfig(), seed: 'reroll-slots', holeCount: 1, humanCount: 1, botCount: 0 });
    game.players[0]!.cash = 20;
    game = applyCommand(game, { type: 'ready-slot-spin', playerId: 'human-0' });
    game = tickTurn(game, 2);
    // This fixture tests Custom Rules' permissive reroll funding, without
    // spending the test budget materializing an unrelated full-catalog hole.
    game.config.ruleset = 'custom';
    game = applyCommand(game, { type: 'contribute-reroll', playerId: 'human-0' });
    game = applyCommand(game, { type: 'contribute-reroll', playerId: 'human-0' });
    game = applyCommand(game, { type: 'contribute-reroll', playerId: 'human-0' });
    expect(game.die?.phase).toBe('reroll-wagering');
    game = applyCommand(game, { type: 'add-chaos-reel', playerId: 'human-0' });
    expect(game.die?.reels.filter((reel) => reel.kind === 'chaos')).toHaveLength(1);
    game.config.ruleset = 'party';
    game = applyCommand(game, { type: 'ready-slot-spin', playerId: 'human-0' });
    game = tickTurn(game, 2);
    expect(game.die?.revealed?.plan.label).toContain('·');
  });
});

describe('turns, shared rules, and bots', () => {
  it('plays one visible boon or curse onto any eligible player and consumes it on that player’s putt', () => {
    let game = gameOn(arena('targeted-boon'));
    const [caster, target] = game.players;
    const casterId = caster!.id;
    const targetId = target!.id;
    game.players[0]!.pockets = [{ id: 'tailwind', source: 'shop', instanceId: 'tailwind-1' }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'tailwind', cardId: 'tailwind-1', targetId });
    expect(game.players[0]!.pockets).toEqual([]);
    expect(game.turn.cardPlayed).toBe(true);
    expect(game.players[1]!.attachments).toMatchObject([{ cardId: 'tailwind', remaining: 1, unit: 'putt', casterId }]);
    expect(applyCommand(game, { type: 'use-power-up', powerUp: 'tailwind', targetId })).toEqual(game);

    game.turn = { ...game.turn, playerIndex: 1, cardPlayed: false };
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 3 } });
    expect(game.players[1]!.attachments).toEqual([]);
  });

  it('blocks, reflects, and cleanses targetable strategy cards', () => {
    let game = gameOn(arena('counterplay'));
    const [, target] = game.players;
    const targetId = target!.id;
    game.players[0]!.pockets = [{ id: 'umbrella cart', source: 'shop', instanceId: 'umbrella-1', duration: { unit: 'round', amount: 2 } }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'umbrella cart', cardId: 'umbrella-1', targetId });
    game.turn.cardPlayed = false;
    game.players[0]!.pockets = [{ id: 'sandbag slip', source: 'shop', instanceId: 'sandbag-1' }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'sandbag slip', cardId: 'sandbag-1', targetId });
    expect(game.players[1]!.attachments).toEqual([]);

    game.turn.cardPlayed = false;
    game.players[0]!.pockets = [{ id: 'headwind gust', source: 'shop', instanceId: 'headwind-1', duration: { unit: 'round', amount: 2 } }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'headwind gust', cardId: 'headwind-1', targetId });
    expect(game.players[1]!.attachments).toHaveLength(1);
    game.turn.cardPlayed = false;
    game.players[0]!.pockets = [{ id: 'clean slate', source: 'shop', instanceId: 'clean-1' }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'clean slate', cardId: 'clean-1', targetId });
    expect(game.players[1]!.attachments).toEqual([]);

    game.turn.cardPlayed = false;
    game.players[0]!.pockets = [{ id: 'mirror caddy', source: 'shop', instanceId: 'mirror-1', duration: { unit: 'round', amount: 2 } }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'mirror caddy', cardId: 'mirror-1', targetId });
    game.turn.cardPlayed = false;
    game.players[0]!.pockets = [{ id: 'sandbag slip', source: 'shop', instanceId: 'sandbag-2' }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'sandbag slip', cardId: 'sandbag-2', targetId });
    expect(game.players[1]!.attachments).toEqual([]);
    expect(game.players[0]!.attachments).toMatchObject([{ cardId: 'sandbag slip', polarity: 'curse' }]);
  });

  it('keeps round effects through a table rotation and resolves variable hole rewards', () => {
    let game = gameOn(arena('strategy-expiry'), { holeCount: 2 });
    const [, target] = game.players;
    const targetId = target!.id;
    game.players[0]!.pockets = [{ id: 'fairway draft', source: 'shop', instanceId: 'draft-1', duration: { unit: 'round', amount: 1 } }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'fairway draft', cardId: 'draft-1', targetId });
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.players[1]!.attachments).toHaveLength(1);
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.players[1]!.attachments).toEqual([]);

    game.turn = { ...game.turn, playerIndex: 0, cardPlayed: false };
    game.players[0]!.pockets = [{ id: 'sponsor tab', source: 'shop', instanceId: 'sponsor-1', duration: { unit: 'hole', amount: 1 } }];
    const cash = game.players[1]!.cash;
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'sponsor tab', cardId: 'sponsor-1', targetId });
    game.players.forEach((player) => { player.ball.complete = true; player.ball.strokes = 1; });
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
    expect(game.status).toBe('shopping');
    expect(game.players[1]!.cash).toBe(cash + 6);
    expect(game.players[1]!.attachments).toEqual([]);
  });

  it('supports targeted terrain boons and pairs co-op strategy attachments for caster and recipient', () => {
    let game = gameOn(arena('terrain-coop'));
    const [caster, target] = game.players;
    const targetId = target!.id;
    game.players[0]!.pockets = [{ id: 'wind sock', source: 'shop', instanceId: 'wind-sock-1' }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'wind sock', cardId: 'wind-sock-1', targetId });
    expect(game.players[1]!.gustReversed).toBe(true);
    expect(game.players[0]!.pockets).toEqual([]);

    game.turn = { ...game.turn, cardPlayed: false };
    game.players[0]!.pockets = [{ id: 'shared draft', source: 'shop', instanceId: 'shared-draft-1', duration: { unit: 'round', amount: 2 } }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'shared draft', cardId: 'shared-draft-1', targetId });
    expect(game.players[0]!.attachments).toMatchObject([{ cardId: 'shared draft', casterId: caster!.id, remaining: 2 }]);
    expect(game.players[1]!.attachments).toMatchObject([{ cardId: 'shared draft', casterId: caster!.id, remaining: 2 }]);

    game.turn = { ...game.turn, cardPlayed: false };
    game.players[0]!.pockets = [{ id: 'rescue pact', source: 'shop', instanceId: 'rescue-pact-1', duration: { unit: 'hole', amount: 1 } }];
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'rescue pact', cardId: 'rescue-pact-1', targetId });
    expect(game.players[0]!.attachments?.some((attachment) => attachment.cardId === 'rescue pact' && attachment.casterId === caster!.id && attachment.unit === 'hole')).toBe(true);
    expect(game.players[1]!.attachments?.some((attachment) => attachment.cardId === 'rescue pact' && attachment.casterId === caster!.id && attachment.unit === 'hole')).toBe(true);
  });

  it('applies specialist Caddy and terrain card modifiers without changing unrelated physics', () => {
    const player = gameOn(arena('terrain-modifiers')).players[0]!;
    player.upgrades = ['cushion keeper', 'spring coach', 'bumper apprentice', 'slope scout', 'wind warden'];
    player.cushionMapped = true;
    player.springPolished = true;
    player.bumperWaxed = true;
    player.slopeStabilized = true;
    player.gustReversed = true;
    const modifiers = physicsModifiersFor(player);
    expect(modifiers.cushionDragMultiplier).toBeLessThan(.4);
    expect(modifiers.springLiftMultiplier).toBeGreaterThan(1.5);
    expect(modifiers.bumperRestitutionMultiplier).toBeGreaterThan(1.3);
    expect(modifiers.slopeGravityMultiplier).toBeLessThan(.25);
    expect(modifiers.gustMultiplier).toBeLessThan(0);
  });

  it('freezes turns and shots while paused, then resumes the same match state', () => {
    let game = resolveDie(createGame({ ...defaultConfig(), seed: 'pause-state', holeCount: 1, humanCount: 1, botCount: 0 }));
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
    const game = resolveDie(createGame({ ...defaultConfig(), seed: 'animation-seed', holeCount: 1, botCount: 1 }));
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
    expect(game.status).toBe('rolling');
    game = resolveDie(game);
    expect(game.status).toBe('transitioning');
    expect(game.players[0]!.upgrades).toEqual(['heavy ball', 'bank shot']);
    game = applyCommand(game, { type: 'complete-transition' });
    expect(game.players[0]!.upgrades).toEqual(['heavy ball', 'bank shot']);
  }, 30_000);

  it('scores each resolved hole and finishes after the configured ninth hole', () => {
    let game = resolveDie(createGame({ ...defaultConfig(), seed: 'nine-hole', humanCount: 1, botCount: 0 }));
    for (let hole = 1; hole <= 9; hole += 1) {
      game.players[0]!.ball.complete = true;
      game.players[0]!.ball.strokes = 1;
      game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 1 } });
      if (hole < 9) {
        expect(game.status).toBe('shopping');
        game = resolveShop(game);
        expect(game.status).toBe('rolling');
        game = resolveDie(game);
        game = applyCommand(game, { type: 'complete-transition' });
      }
    }
    expect(game.status).toBe('finished');
    expect(game.players[0]!.total).toBeGreaterThanOrEqual(9);
  }, 120_000);

  it('uses temporary power-ups and produces finite bot decisions', () => {
    const course = arena('items-and-bots');
    let game = gameOn(course);
    game.players[0]!.inventory = 'freeze';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'freeze', targetId: game.players[1]!.id });
    expect(game.players[1]!.frozenTurns).toBe(1);
    const bot = game.players[1]!;
    const decision = chooseBotDecision(game.course, bot, game.players, game.hazardElapsedMs, game.holeRules);
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
    expect(game.turn.cardPlayed).toBe(true);
    game.turn.cardPlayed = false;
    game.players[0]!.inventory = 'popper pad';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'popper pad', placement: { x: 5, y: 3 } });
    expect(game.gadgets).toHaveLength(2);

    const beforeDrone = game.players[0]!.ball.x;
    game.turn.cardPlayed = false;
    game.players[0]!.inventory = 'rescue drone';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'rescue drone' });
    expect(game.players[0]!.ball.x).toBeGreaterThan(beforeDrone);

    game.turn.cardPlayed = false;
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
    expect(['turbo', 'shield', 'two putts', 'bouncy', 'ice', 'magnet', 'cup magnet', 'slipstream', 'rebound rig', 'wind sock', 'slope stabilizer', 'spring polish', 'bumper wax', 'cushion map', 'popper pad', 'snare patch', 'blast mine', 'slick patch', 'phase shift', 'sandbag']).toContain(powerUpFor(game, game.players[0]!, 'chaos', 'recovery-bias'));
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
