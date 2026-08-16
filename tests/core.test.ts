import { describe, expect, it } from 'vitest';
import { chooseBotDecision } from '../src/core/bots';
import { applyCommand, beginCourse, botMove, createGame, defaultConfig, previewShot, tickTurn } from '../src/core/game';
import { defaultTerrainSettings, generateCandidates, generateCourse, randomTerrainSettings } from '../src/core/generator';
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
  return { id: seed, seed, width, height, tiles, tee, cup, route: [tee, cup], hazards: [], itemPads: [], score: { playable: true, estimatedStrokes: 1, hazards: 0, elevation: 0, routes: 1, novelty: 0, total: 100, solverShots: [] } };
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

  it('keeps a solver line viable in every hazard phase and generates both major hazard types', () => {
    const courses = Array.from({ length: 8 }, (_, index) => generateCourse(`phase-set-${index}`));
    expect(courses.some((course) => course.hazards[0]?.kind === 'sweeper')).toBe(true);
    expect(courses.some((course) => course.hazards[0]?.kind === 'gate')).toBe(true);
    expect(courses.every((course) => course.itemPads.length === 3)).toBe(true);
    for (const course of courses) {
      for (let phase = 0; phase < 8; phase += 1) expect(simulateShot(course, newBall(course), course.score.solverShots[0]!, 10, { phase }).holed).toBe(true);
    }
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
    expect(Math.hypot(committed.vx, committed.vy, committed.vz)).toBe(0);

    const multiplayer = beginCourse(createGame({ ...defaultConfig(), seed: 'animation-multiplayer', botCount: 3 }));
    const multiplayerFrames = previewShot(multiplayer, shot)!;
    expect(multiplayerFrames.every((frame) => frame.length === multiplayer.players.length)).toBe(true);
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
    const bouncy = simulateShot(wallCourse, newBall(wallCourse), { angle: 0, power: 4 }, 1, { modifiers: { bouncy: true } });
    expect(bankShot.ball.x).toBeLessThan(normalBank.ball.x);
    expect(bouncy.ball.x).toBeLessThan(bankShot.ball.x);

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

  it('uses the same phase for gates, sweepers, previews, and committed turns', () => {
    const gateCourse = arena('gate-arena');
    gateCourse.hazards = [{ id: 'gate', kind: 'gate', point: { x: 3, y: 3 }, phaseOffset: 0 }];
    const openGate = simulateShot(gateCourse, newBall(gateCourse), { angle: 0, power: 4 }, 1, { phase: 0 });
    const closedGate = simulateShot(gateCourse, newBall(gateCourse), { angle: 0, power: 4 }, 1, { phase: 2 });
    expect(openGate.ball.x).toBeGreaterThan(3.2);
    expect(closedGate.ball.x).toBeLessThan(3);

    const sweeperCourse = arena('sweeper-arena');
    sweeperCourse.hazards = [{ id: 'sweeper', kind: 'sweeper', point: { x: 3, y: 3 }, phaseOffset: 0, radius: .78 }];
    const sweep = simulateShot(sweeperCourse, newBall(sweeperCourse), { angle: 0, power: 4 }, 1, { phase: 0 });
    expect(Math.abs(sweep.ball.y - 3.5)).toBeGreaterThan(.1);

    let game = gameOn(gateCourse);
    const frames = previewShot(game, { angle: 0, power: 4 })!;
    const committed = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 4 } });
    expect(frames.at(-1)?.[0]).toMatchObject({ x: committed.players[0]!.ball.x, y: committed.players[0]!.ball.y });
    expect(committed.coursePhase).toBe(1);
    game = committed;
    game.players[1]!.frozenTurns = 1;
    game = applyCommand({ ...game, turn: { ...game.turn, playerIndex: 1 } }, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.coursePhase).toBe(2);
    game = tickTurn({ ...game, turn: { ...game.turn, secondsLeft: .1 } }, 1);
    expect(game.coursePhase).toBe(3);
  });

  it('collects visible pads and favors recovery items for players substantially behind', () => {
    const course = arena('pad-arena');
    const padId = Array.from({ length: 12 }, (_, index) => `recovery-${index}`).find((id) => new Random(`${course.seed}:${id}:human-0:0:1`).chance(.7))!;
    course.itemPads = [{ id: padId, point: { x: 2, y: 3 }, kind: 'chaos' }];
    let game = gameOn(course);
    game.players[0]!.total = 3;
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(course.itemPads[0]!.collected).toBeUndefined();
    expect(game.course.itemPads[0]!.collected).toBe(true);
    expect(['turbo', 'shield', 'two putts', 'bouncy', 'ice', 'magnet']).toContain(game.players[0]!.inventory);
  });

  it('expires ball forms after a shot and keeps two putts on the same phase and turn', () => {
    const course = arena('two-putts-arena');
    let game = gameOn(course);
    game.players[0]!.inventory = 'ghost';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'ghost' });
    expect(game.players[0]!.ballForm).toBe('ghost');
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.players[0]!.ballForm).toBeUndefined();

    game = gameOn(course);
    game.players[0]!.inventory = 'two putts';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'two putts' });
    expect(game.players[0]!.twoPuttsArmed).toBe(true);
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.turn.playerIndex).toBe(0);
    expect(game.coursePhase).toBe(0);
    expect(game.players[0]!.twoPuttsArmed).toBeUndefined();
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.turn.playerIndex).toBe(1);
    expect(game.coursePhase).toBe(1);
  });

  it('gives scavenger a second item slot and arms a selected portal-ball exit', () => {
    const course = arena('scavenger-arena');
    course.itemPads = [{ id: 'scavenge-pad', point: { x: 2, y: 3 }, kind: 'recovery' }];
    course.portals = [
      { id: 'portal-1', entrance: { point: { x: 3, y: 3 }, direction: { x: 1, y: 0 } }, exit: { point: { x: 8, y: 3 }, direction: { x: 1, y: 0 } } },
      { id: 'portal-2', entrance: { point: { x: 5, y: 1 }, direction: { x: 1, y: 0 } }, exit: { point: { x: 10, y: 1 }, direction: { x: 1, y: 0 } } },
    ];
    let game = gameOn(course);
    game.players[0]!.upgrades = ['scavenger'];
    game.players[0]!.inventory = 'turbo';
    game = applyCommand(game, { type: 'shoot', shot: { angle: 0, power: 2 } });
    expect(game.players[0]!.spareInventory).toBeDefined();

    game = gameOn(course);
    game.players[0]!.inventory = 'portal';
    game = applyCommand(game, { type: 'use-power-up', powerUp: 'portal', portalExitId: 'portal-2:exit' });
    expect(game.players[0]!.ballForm).toBe('portal');
    expect(game.players[0]!.portalExitId).toBe('portal-2:exit');
  });
});

describe('authored course rounds', () => {
  it('starts with a blank canvas and lets an author build the required validation layout', () => {
    let game = createGame({ ...defaultConfig(), seed: 'manual-builder', humanCount: 1, botCount: 0 });
    expect(game.status).toBe('build');
    expect(game.course.tiles.every((tile) => tile.surface === 'void')).toBe(true);

    game = applyCommand(game, { type: 'build-settings', height: 2 });
    for (let x = 2; x <= 10; x += 1) game = applyCommand(game, { type: 'build-place', point: { x, y: 7 } });
    game = applyCommand(game, { type: 'build-place', point: { x: 2, y: 7 } });
    game = applyCommand(game, { type: 'build-settings', tool: 'tee' });
    game = applyCommand(game, { type: 'build-place', point: { x: 2, y: 7 } });
    game = applyCommand(game, { type: 'build-settings', tool: 'cup' });
    game = applyCommand(game, { type: 'build-place', point: { x: 10, y: 7 } });
    game = applyCommand(game, { type: 'begin-validation' });

    expect(game.status).toBe('validate');
    expect(game.course.tiles[7 * game.course.width + 5]!.height).toBe(2);
    expect(game.course.tiles[7 * game.course.width + 2]!.surface).toBe('tee');
    expect(game.course.tiles[7 * game.course.width + 10]!.surface).toBe('cup');
  });

  it('lets each author set one competition perk while building', () => {
    let game = createGame({ ...defaultConfig(), seed: 'builder-perk', humanCount: 1, botCount: 0 });
    game = applyCommand(game, { type: 'select-upgrade', upgrade: 'bank shot' });
    game = applyCommand(game, { type: 'select-upgrade', upgrade: 'hazard shield' });
    expect(game.players[0]!.upgrades).toEqual(['hazard shield']);
  });

  it('lets builders create independently linked portal pairs with oriented endpoints', () => {
    let game = createGame({ ...defaultConfig(), seed: 'builder-portals', humanCount: 1, botCount: 0 });
    game = applyCommand(game, { type: 'build-settings', tool: 'portal-entrance', portalPairId: 7, direction: { x: 1, y: 0 } });
    game = applyCommand(game, { type: 'build-place', point: { x: 4, y: 6 } });
    game = applyCommand(game, { type: 'build-settings', tool: 'portal-exit', direction: { x: 0, y: -1 } });
    game = applyCommand(game, { type: 'build-place', point: { x: 12, y: 8 } });
    expect(game.course.portals).toEqual([{
      id: 'portal-7',
      entrance: { point: { x: 4, y: 6 }, direction: { x: 1, y: 0 } },
      exit: { point: { x: 12, y: 8 }, direction: { x: 0, y: -1 } },
    }]);
  });

  it('keeps terrain generation deterministic while applying the authored settings', () => {
    const sparse = { ...defaultTerrainSettings(), density: .1, elevation: 0, hazards: 0, routeLength: .35, bendiness: 0, laneWidth: 1, branches: 0, chaos: 0 };
    const busy = { ...defaultTerrainSettings(), density: .9, elevation: 1, hazards: 3, routeLength: 1, bendiness: .9, laneWidth: 2, branches: 3, chaos: 1, theme: 'hazard-run' as const };
    const first = generateCourse('builder-settings', busy);
    const second = generateCourse('builder-settings', busy);
    const quiet = generateCourse('builder-settings', sparse);

    expect(first.tiles).toEqual(second.tiles);
    expect(first.hazards).toHaveLength(3);
    expect(quiet.hazards).toHaveLength(0);
    expect(first.tiles.filter((tile) => tile.surface !== 'void').length).toBeGreaterThan(quiet.tiles.filter((tile) => tile.surface !== 'void').length);
    expect(first.route).not.toEqual(quiet.route);
    expect(randomTerrainSettings('builder-settings', 2)).toEqual(randomTerrainSettings('builder-settings', 2));
  });

  it('keeps varied curated generator setups playable', () => {
    const generated = Array.from({ length: 20 }, (_, index) => generateCourse(`curated-variation-${index}`, randomTerrainSettings('curated-variation', index + 1)));
    expect(generated.every((course) => course.score.playable)).toBe(true);
    expect(new Set(generated.map((course) => course.cup.x)).size).toBeGreaterThan(2);
    expect(new Set(generated.map((course) => course.tiles.filter((tile) => tile.surface !== 'void').length)).size).toBeGreaterThan(4);
  });

  it('requires an author sink before moving to the next builder, then starts competition', () => {
    let game = createGame({ ...defaultConfig(), seed: 'author-validation', humanCount: 1, botCount: 1 });
    game = applyCommand(game, { type: 'build-generate' });
    game = applyCommand(game, { type: 'begin-validation' });
    const firstShot = game.course.score.solverShots[0];
    expect(firstShot).toBeDefined();
    game = applyCommand(game, { type: 'shoot', shot: firstShot! });
    expect(game.status).toBe('build');
    expect(game.authoredCourses).toHaveLength(1);
    expect(game.turn.playerIndex).toBe(1);
    expect(botMove(game)).toBeUndefined();

    game = applyCommand(game, { type: 'build-generate' });
    game = applyCommand(game, { type: 'begin-validation' });
    expect(game.status).toBe('validate');
    expect(botMove(game)).toBeDefined();
    const secondShot = game.course.score.solverShots[0];
    expect(secondShot).toBeDefined();
    game = applyCommand(game, { type: 'shoot', shot: secondShot! });

    expect(game.status).toBe('playing');
    expect(game.authoredCourses).toHaveLength(2);
    expect(game.courseIndex).toBe(0);
    expect(game.players.every((player) => player.ball.strokes === 0)).toBe(true);
  });

  it('keeps validation turns with the author when the timer expires', () => {
    let game = createGame({ ...defaultConfig(), seed: 'validation-timeout', humanCount: 1, botCount: 1 });
    game = applyCommand(game, { type: 'build-generate' });
    game = applyCommand(game, { type: 'begin-validation' });
    game = tickTurn({ ...game, turn: { ...game.turn, secondsLeft: .1 } }, 1);
    expect(game.status).toBe('validate');
    expect(game.turn.playerIndex).toBe(0);
    expect(game.turn.secondsLeft).toBe(game.config.timerSeconds);
  });

  it('scores each authored course in order and preserves the validated course copy', () => {
    const first = generateCourse('competition-first');
    const second = generateCourse('competition-second');
    let game = beginCourse(createGame({ ...defaultConfig(), seed: 'competition-round', humanCount: 1, botCount: 1 }));
    game.status = 'playing';
    game.course = first;
    game.authoredCourses = [{ authorId: 'human-0', course: first }, { authorId: 'bot-0', course: second }];
    game.players.forEach((player) => { player.ball = newBall(first); });
    game.players[1]!.ball.complete = true;
    game = applyCommand(game, { type: 'shoot', shot: first.score.solverShots[0]! });

    expect(game.status).toBe('playing');
    expect(game.courseIndex).toBe(1);
    expect(game.course.seed).toBe(second.seed);
    expect(game.authoredCourses[0]!.course.seed).toBe(first.seed);
    expect(game.players.every((player) => player.ball.strokes === 0)).toBe(true);
  });
});
