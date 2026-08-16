import { chooseBotDecision, type BotDecision } from './bots';
import { defaultTerrainSettings, generateCourse, randomTerrainSettings } from './generator';
import { COURSE_PHASES } from './hazards';
import { MAX_SETTLE_SECONDS, newBall, simulateImpulse, simulateShot, type BallPhysicsModifiers, type SimulationResult } from './physics';
import { hashSeed, Random } from './random';
import type { Ball, BuildTool, Course, GameCommand, GameConfig, GameState, GameTransport, ItemPadKind, Player, Point, PowerUp, ShotCommand, Upgrade } from './types';

const colors = ['#f6c26b', '#8bd5ca', '#f38ba8', '#cba6f7', '#a6e3a1', '#89b4fa', '#fab387', '#f9e2af', '#94e2d5', '#eba0ac', '#b4befe', '#f5c2e7'];
const recoveryPowerUps: PowerUp[] = ['turbo', 'shield'];
const chaosPowerUps: PowerUp[] = ['turbo', 'bomb', 'freeze', 'swap'];
const upgrades: Upgrade[] = ['heavy ball', 'ice skates', 'extra charge', 'bank shot', 'hazard shield', 'chaos magnet'];

export const UPGRADE_DESCRIPTIONS: Record<Upgrade, string> = {
  'heavy ball': '+12% launch power and 1.45× collision mass',
  'ice skates': 'slides farther across ice',
  'extra charge': 'empty chaos slot refills after every shot',
  'bank shot': 'retains 82% speed on wall rebounds',
  'hazard shield': 'one void rebound each hole',
  'chaos magnet': '65% chance to refill after using an item',
};

export const defaultConfig = (): GameConfig => ({
  seed: `enemy-${Math.random().toString(36).slice(2, 8)}`,
  timerSeconds: 24,
  strokeCap: 10,
  collisions: true,
  powerUps: true,
  botCount: 3,
  humanCount: 1,
  botSkill: 5,
});

const emptyPlayer = (id: string, index: number, kind: Player['kind'], skill: Player['skill'], course: Course): Player => ({
  id,
  name: kind === 'bot' ? `enemy-${index + 1}` : `golfer-${index + 1}`,
  color: colors[index]!,
  kind,
  skill,
  ball: newBall(course),
  upgrades: [],
  total: 0,
});

const blankCourse = (seed: string): Course => {
  const tee = { x: 2, y: Math.floor(7) };
  const cup = { x: 17, y: Math.floor(7) };
  return {
    id: `build-${seed}`,
    seed,
    width: 20,
    height: 14,
    tiles: Array.from({ length: 280 }, () => ({ surface: 'void', height: 0 })),
    tee,
    cup,
    route: [],
    hazards: [],
    itemPads: [],
    score: { playable: false, estimatedStrokes: 0, hazards: 0, elevation: 0, routes: 0, novelty: 0, total: 0, solverShots: [], rejection: 'builder has not validated this course' },
  };
};

export const createGame = (config: GameConfig): GameState => {
  const course = blankCourse(hashSeed(config.seed, 0));
  const players = Array.from({ length: config.humanCount }, (_, index) => emptyPlayer(`human-${index}`, index, 'human', 0, course));
  players.push(...Array.from({ length: config.botCount }, (_, index) => emptyPlayer(`bot-${index}`, players.length + index, 'bot', config.botSkill, course)));
  return {
    config,
    course,
    hole: 1,
    coursePhase: 0,
    emotes: [],
    emoteSequence: 0,
    players,
    turn: { playerIndex: 0, secondsLeft: config.timerSeconds, shotInFlight: false },
    authoredCourses: [],
    courseIndex: 0,
    build: { authorIndex: 0, tool: 'fairway', height: 0, direction: { x: 1, y: 0 }, terrain: defaultTerrainSettings(), generated: false },
    status: 'build',
    messages: ['build a course, then sink it once to validate'],
  };
};

const cloneCourse = (course: Course): Course => ({ ...course, tiles: course.tiles.map((tile) => ({ ...tile, corners: tile.corners ? [...tile.corners] as [number, number, number, number] : undefined, direction: tile.direction ? { ...tile.direction } : undefined })), route: course.route.map((point) => ({ ...point })), tee: { ...course.tee }, cup: { ...course.cup }, hazards: course.hazards.map((hazard) => ({ ...hazard, point: { ...hazard.point } })), itemPads: course.itemPads.map((pad) => ({ ...pad, point: { ...pad.point } })) });

const cloneState = (state: GameState): GameState => ({ ...state, course: cloneCourse(state.course), authoredCourses: state.authoredCourses.map((entry) => ({ ...entry, course: cloneCourse(entry.course) })), build: state.build ? { ...state.build, direction: { ...state.build.direction }, terrain: { ...state.build.terrain } } : undefined, emotes: state.emotes.map((emote) => ({ ...emote })), players: state.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades] })), turn: { ...state.turn }, messages: [...state.messages] });

const activePlayer = (state: GameState) => state.players[state.turn.playerIndex]!;

const adjustedShot = (player: Player, shot: ShotCommand): ShotCommand => {
  const multiplier = (player.turboArmed ? 1.55 : 1) * (player.upgrades.includes('heavy ball') ? 1.12 : 1);
  return { ...shot, power: shot.power * multiplier };
};

const modifiersFor = (player: Player): BallPhysicsModifiers => ({
  mass: player.upgrades.includes('heavy ball') ? 1.45 : 1,
  iceSkates: player.upgrades.includes('ice skates'),
  bankShot: player.upgrades.includes('bank shot'),
  hazardShield: player.hazardShield,
});

const simulatePlayerShot = (state: GameState, playerIndex: number, shot: ShotCommand) => {
  const player = state.players[playerIndex]!;
  if (state.status === 'validate') return simulateShot(state.course, player.ball, adjustedShot(player, shot), undefined, {
    modifiers: modifiersFor(player),
    phase: state.coursePhase,
  });
  return simulateShot(state.course, player.ball, adjustedShot(player, shot), undefined, {
    modifiers: modifiersFor(player),
    otherBalls: state.players.filter((_, index) => index !== playerIndex).map((candidate) => ({ ball: candidate.ball, modifiers: modifiersFor(candidate) })),
    collisions: state.config.collisions,
    phase: state.coursePhase,
    collectItems: state.config.powerUps && !player.inventory,
  });
};

const applySimulation = (state: GameState, playerIndex: number, result: SimulationResult) => {
  state.players[playerIndex]!.ball = result.ball;
  state.players[playerIndex]!.hazardShield = state.players[playerIndex]!.hazardShield && !result.shieldUsed;
  if (state.status === 'validate') return;
  let otherIndex = 0;
  state.players.forEach((player, index) => {
    if (index === playerIndex) return;
    player.ball = result.otherBalls[otherIndex]!;
    player.hazardShield = player.hazardShield && !result.otherShieldUsed[otherIndex];
    otherIndex += 1;
  });
};

export const previewShot = (state: GameState, shot: ShotCommand): Ball[][] | undefined => {
  if ((state.status !== 'playing' && state.status !== 'validate') || state.turn.shotInFlight) return undefined;
  const playerIndex = state.turn.playerIndex;
  const player = state.players[playerIndex]!;
  if (player.frozenTurns) return undefined;
  const result = simulatePlayerShot(state, playerIndex, shot);
  return result.frames.map((frame) => {
    let otherIndex = 0;
    return state.players.map((player, index) => index === playerIndex ? frame.ball : frame.otherBalls[otherIndex++] ?? player.ball);
  });
};

const addMessage = (state: GameState, message: string) => {
  state.messages = [message, ...state.messages].slice(0, 5);
};

const advanceCoursePhase = (state: GameState) => {
  state.coursePhase = (state.coursePhase + 1) % COURSE_PHASES;
};

const resetPlayersForCourse = (state: GameState) => {
  state.players.forEach((player) => {
    player.ball = newBall(state.course);
    player.inventory = undefined;
    player.turboArmed = false;
    player.frozenTurns = undefined;
    player.hazardShield = player.upgrades.includes('hazard shield');
  });
};

const beginBuild = (state: GameState, authorIndex: number) => {
  const author = state.players[authorIndex]!;
  state.course = blankCourse(hashSeed(state.config.seed, state.authoredCourses.length + authorIndex + 1));
  state.coursePhase = 0;
  state.build = { authorIndex, tool: 'fairway', height: 0, direction: { x: 1, y: 0 }, terrain: defaultTerrainSettings(), generated: false };
  state.turn = { playerIndex: authorIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  state.status = 'build';
  addMessage(state, `${author.name} is building a course`);
};

const buildReady = (course: Course) => {
  const tee = course.tiles[course.tee.y * course.width + course.tee.x];
  const cup = course.tiles[course.cup.y * course.width + course.cup.x];
  return tee?.surface === 'tee' && cup?.surface === 'cup' && course.tiles.filter((tile) => tile.surface !== 'void' && tile.surface !== 'wall').length >= 8;
};

const recenterTileHeight = (tile: Course['tiles'][number]) => {
  const corners = tile.corners ?? [tile.height, tile.height, tile.height, tile.height];
  tile.corners = corners;
  tile.height = corners.reduce((total, height) => total + height, 0) / 4;
};

const setBuildHeight = (course: Course, point: Point, height: number) => {
  const at = (x: number, y: number) => x >= 0 && y >= 0 && x < course.width && y < course.height ? course.tiles[y * course.width + x] : undefined;
  const tile = at(point.x, point.y);
  if (!tile || tile.surface === 'void') return;
  tile.corners = [height, height, height, height];
  tile.height = height;
  const north = at(point.x, point.y - 1);
  const east = at(point.x + 1, point.y);
  const south = at(point.x, point.y + 1);
  const west = at(point.x - 1, point.y);
  if (north && north.surface !== 'void') { const corners = north.corners ?? [north.height, north.height, north.height, north.height]; corners[3] = height; corners[2] = height; north.corners = corners; recenterTileHeight(north); }
  if (east && east.surface !== 'void') { const corners = east.corners ?? [east.height, east.height, east.height, east.height]; corners[0] = height; corners[3] = height; east.corners = corners; recenterTileHeight(east); }
  if (south && south.surface !== 'void') { const corners = south.corners ?? [south.height, south.height, south.height, south.height]; corners[0] = height; corners[1] = height; south.corners = corners; recenterTileHeight(south); }
  if (west && west.surface !== 'void') { const corners = west.corners ?? [west.height, west.height, west.height, west.height]; corners[1] = height; corners[2] = height; west.corners = corners; recenterTileHeight(west); }
};

const placeBuildTool = (state: GameState, point: Point) => {
  const build = state.build;
  if (!build || point.x < 0 || point.y < 0 || point.x >= state.course.width || point.y >= state.course.height) return;
  const tileIndex = point.y * state.course.width + point.x;
  const tool = build.tool;
  const clearFeatures = () => {
    state.course.hazards = state.course.hazards.filter((hazard) => hazard.point.x !== point.x || hazard.point.y !== point.y);
    state.course.itemPads = state.course.itemPads.filter((pad) => pad.point.x !== point.x || pad.point.y !== point.y);
  };
  if (tool === 'erase') {
    state.course.tiles[tileIndex] = { surface: 'void', height: 0 };
    clearFeatures();
    return;
  }
  if (tool === 'tee' || tool === 'cup') {
    const previous = tool === 'tee' ? state.course.tee : state.course.cup;
    const previousTile = state.course.tiles[previous.y * state.course.width + previous.x];
    if (previousTile?.surface === tool) state.course.tiles[previous.y * state.course.width + previous.x] = { surface: 'void', height: 0 };
    state.course[tool] = { ...point };
    state.course.tiles[tileIndex] = { surface: tool, height: build.height, corners: [build.height, build.height, build.height, build.height] };
    clearFeatures();
    return;
  }
  if (tool === 'sweeper' || tool === 'gate') {
    if (state.course.tiles[tileIndex]!.surface === 'void') state.course.tiles[tileIndex] = { surface: 'fairway', height: build.height, corners: [build.height, build.height, build.height, build.height] };
    state.course.hazards = state.course.hazards.filter((hazard) => hazard.point.x !== point.x || hazard.point.y !== point.y);
    state.course.hazards.push(tool === 'sweeper' ? { id: `builder-sweeper-${point.x}-${point.y}`, kind: 'sweeper', point: { ...point }, phaseOffset: (point.x + point.y) % COURSE_PHASES, radius: .78 } : { id: `builder-gate-${point.x}-${point.y}`, kind: 'gate', point: { ...point }, phaseOffset: (point.x + point.y) % COURSE_PHASES });
    return;
  }
  if (tool === 'recovery-pad' || tool === 'chaos-pad') {
    if (state.course.tiles[tileIndex]!.surface === 'void') state.course.tiles[tileIndex] = { surface: 'fairway', height: build.height, corners: [build.height, build.height, build.height, build.height] };
    state.course.itemPads = state.course.itemPads.filter((pad) => pad.point.x !== point.x || pad.point.y !== point.y);
    state.course.itemPads.push({ id: `builder-pad-${point.x}-${point.y}`, point: { ...point }, kind: tool === 'recovery-pad' ? 'recovery' : 'chaos' });
    return;
  }
  state.course.tiles[tileIndex] = { surface: tool, height: build.height, corners: [build.height, build.height, build.height, build.height], direction: tool === 'booster' || tool === 'conveyor' ? { ...build.direction } : undefined };
  clearFeatures();
  setBuildHeight(state.course, point, build.height);
};

const finishValidation = (state: GameState) => {
  const author = activePlayer(state);
  state.authoredCourses.push({ authorId: author.id, course: cloneCourse(state.course) });
  const nextAuthor = state.authoredCourses.length;
  if (nextAuthor < state.players.length) {
    beginBuild(state, nextAuthor);
    return;
  }
  state.courseIndex = 0;
  state.course = cloneCourse(state.authoredCourses[0]!.course);
  state.hole = 1;
  state.coursePhase = 0;
  resetPlayersForCourse(state);
  state.turn = { playerIndex: 0, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  state.build = undefined;
  state.status = 'playing';
  addMessage(state, 'all courses validated — competitive play begins');
};

const advanceTurn = (state: GameState) => {
  for (let offset = 1; offset <= state.players.length; offset += 1) {
    const index = (state.turn.playerIndex + offset) % state.players.length;
    const player = state.players[index]!;
    if (!player.ball.complete && player.ball.strokes < state.config.strokeCap) {
      state.turn = { playerIndex: index, secondsLeft: state.config.timerSeconds, shotInFlight: false };
      return;
    }
  }
  finishHole(state);
};

const finishHole = (state: GameState) => {
  for (const player of state.players) {
    const strokes = player.ball.complete ? player.ball.strokes : state.config.strokeCap;
    player.total += strokes;
  }
  if (state.courseIndex + 1 >= state.authoredCourses.length) {
    state.status = 'finished';
    addMessage(state, 'all authored courses scored');
    return;
  }
  state.courseIndex += 1;
  state.hole = state.courseIndex + 1;
  state.course = cloneCourse(state.authoredCourses[state.courseIndex]!.course);
  state.coursePhase = 0;
  resetPlayersForCourse(state);
  state.turn = { playerIndex: 0, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  addMessage(state, `playing ${state.players.find((player) => player.id === state.authoredCourses[state.courseIndex]!.authorId)?.name ?? 'player'}'s course`);
};

export const applyCommand = (current: GameState, command: GameCommand): GameState => {
  const state = cloneState(current);
  if (command.type === 'build-settings' && state.status === 'build' && state.build) {
    if (command.tool) state.build.tool = command.tool;
    if (command.height !== undefined) state.build.height = Math.max(0, Math.min(3, command.height));
    if (command.direction) state.build.direction = { ...command.direction };
    if (command.terrain) state.build.terrain = { ...state.build.terrain, ...command.terrain };
    return state;
  }
  if (command.type === 'build-place' && state.status === 'build') {
    placeBuildTool(state, command.point);
    return state;
  }
  if (command.type === 'build-generate' && state.status === 'build' && state.build) {
    const author = state.players[state.build.authorIndex]!;
    state.course = generateCourse(hashSeed(`${state.config.seed}-${author.id}-v${state.build.terrain.variation}`, state.authoredCourses.length + 1), state.build.terrain);
    state.build.generated = true;
    addMessage(state, `${author.name} generated terrain — edit it or validate it`);
    return state;
  }
  if (command.type === 'build-randomize' && state.status === 'build' && state.build) {
    const author = state.players[state.build.authorIndex]!;
    const variation = state.build.terrain.variation + 1;
    state.build.terrain = randomTerrainSettings(`${state.config.seed}:${author.id}`, variation);
    state.build.generated = false;
    addMessage(state, `${author.name} rolls a new curated generator setup`);
    return state;
  }
  if (command.type === 'select-upgrade' && state.status === 'build' && state.build) {
    const author = state.players[state.build.authorIndex]!;
    author.upgrades = [command.upgrade];
    addMessage(state, `${author.name} equips ${command.upgrade} for competition`);
    return state;
  }
  if (command.type === 'begin-validation' && state.status === 'build') {
    if (!buildReady(state.course)) {
      addMessage(state, 'place a tee, cup, and at least eight playable tiles first');
      return state;
    }
    const authorIndex = state.build?.authorIndex ?? state.turn.playerIndex;
    state.players[authorIndex]!.ball = newBall(state.course);
    state.turn = { playerIndex: authorIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
    state.coursePhase = 0;
    state.status = 'validate';
    addMessage(state, `${activePlayer(state).name} must sink this course once`);
    return state;
  }
  if (command.type === 'shoot' && (state.status === 'playing' || state.status === 'validate') && !state.turn.shotInFlight) {
    const player = activePlayer(state);
    if (player.frozenTurns) {
      player.frozenTurns -= 1;
      addMessage(state, `${player.name} is frozen solid`);
      advanceCoursePhase(state);
      advanceTurn(state);
      return state;
    }
    const playerIndex = state.turn.playerIndex;
    const result = simulatePlayerShot(state, playerIndex, command.shot);
    player.turboArmed = false;
    applySimulation(state, playerIndex, result);
    if (result.holed) addMessage(state, `${player.name} sinks it in ${player.ball.strokes}`);
    else if (result.reset) addMessage(state, `${player.name} finds the edge`);
    else addMessage(state, `${player.name} rolls to safety`);
    if (state.status === 'validate') {
      advanceCoursePhase(state);
      if (result.holed) finishValidation(state);
      else if (player.ball.strokes >= state.config.strokeCap) {
        state.status = 'build';
        state.build = { ...(state.build ?? { authorIndex: playerIndex, tool: 'fairway' as BuildTool, height: 0, direction: { x: 1, y: 0 }, terrain: defaultTerrainSettings(), generated: false }), authorIndex: playerIndex };
        addMessage(state, `${player.name} needs to revise this course before it can be played`);
      } else state.turn = { playerIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
      return state;
    }
    const pad = result.itemPadIds.map((id) => state.course.itemPads.find((candidate) => candidate.id === id)).find(Boolean);
    if (state.config.powerUps && !player.inventory && pad) {
      pad.collected = true;
      player.inventory = powerUpFor(state, player, pad.kind, pad.id);
      addMessage(state, `${player.name} taps a ${pad.kind} pad — ${player.inventory}`);
    } else if (state.config.powerUps && !player.inventory && player.upgrades.includes('extra charge')) {
      player.inventory = powerUpFor(state, player, 'recovery', 'extra-charge');
      addMessage(state, `${player.name}'s extra charge pulls ${player.inventory}`);
    }
    advanceCoursePhase(state);
    advanceTurn(state);
  }
  if (command.type === 'use-power-up' && state.status === 'playing') usePowerUp(state, command.powerUp, command.targetId);
  if (command.type === 'emote') {
    const player = state.players.find((candidate) => candidate.id === command.playerId);
    if (player) {
      state.emoteSequence += 1;
      state.emotes = [...state.emotes, { id: `${state.course.seed}:${command.playerId}:${state.emoteSequence}`, playerId: command.playerId, emote: command.emote }].slice(-16);
    }
  }
  if (command.type === 'draft' && state.status === 'draft') {
    const player = activePlayer(state);
    if (!player.upgrades.includes(command.upgrade)) player.upgrades.push(command.upgrade);
    addMessage(state, `${player.name} drafts ${command.upgrade}`);
    advanceDraftOrHole(state);
  }
  if (command.type === 'next-hole' && state.status === 'draft') startHole(state, state.hole + 1);
  return state;
};

const advanceDraftOrHole = (state: GameState) => {
  const next = state.players.findIndex((player) => player.upgrades.length < state.hole);
  if (next === -1) startHole(state, state.hole + 1);
  else state.turn = { playerIndex: next, secondsLeft: state.config.timerSeconds, shotInFlight: false };
};

const startHole = (state: GameState, hole: number) => {
  state.hole = hole;
  state.coursePhase = 0;
  state.course = generateCourse(hashSeed(state.config.seed, hole));
  state.players.forEach((player) => {
    player.ball = newBall(state.course);
    player.inventory = undefined;
    player.turboArmed = false;
    player.frozenTurns = undefined;
    player.hazardShield = player.upgrades.includes('hazard shield');
  });
  state.turn = { playerIndex: 0, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  state.status = 'preview';
  addMessage(state, `hole ${hole}: inspect, lock, and tee off`);
};

const usePowerUp = (state: GameState, powerUp: PowerUp, targetId?: string) => {
  const player = activePlayer(state);
  if (player.inventory !== powerUp) return;
  const target = state.players.find((candidate) => candidate.id === targetId);
  let used = false;
  if (powerUp === 'turbo') {
    player.turboArmed = true;
    addMessage(state, `${player.name} arms turbo`);
    used = true;
  }
  if (powerUp === 'shield') {
    player.hazardShield = true;
    addMessage(state, `${player.name} arms a hazard shield`);
    used = true;
  }
  if (powerUp === 'bomb' && target) {
    const distance = Math.hypot(target.ball.x - player.ball.x, target.ball.y - player.ball.y) || 1;
    const result = simulateImpulse(state.course, target.ball, { x: (target.ball.x - player.ball.x) / distance * 4.6, y: (target.ball.y - player.ball.y) / distance * 4.6 }, MAX_SETTLE_SECONDS, modifiersFor(target), state.coursePhase);
    target.ball = result.ball;
    target.hazardShield = target.hazardShield && !result.shieldUsed;
    addMessage(state, `${player.name} bombs ${target.name}`);
    used = true;
  }
  if (powerUp === 'freeze' && target) {
    target.frozenTurns = 1;
    addMessage(state, `${player.name} freezes ${target.name}`);
    used = true;
  }
  if (powerUp === 'swap' && target) {
    const playerPosition = { x: player.ball.x, y: player.ball.y, z: player.ball.z };
    player.ball = { ...player.ball, x: target.ball.x, y: target.ball.y, z: target.ball.z, vx: 0, vy: 0, vz: 0 };
    target.ball = { ...target.ball, ...playerPosition, vx: 0, vy: 0, vz: 0 };
    addMessage(state, `${player.name} swaps with ${target.name}`);
    used = true;
  }
  if (!used) return;
  player.inventory = undefined;
  if (state.config.powerUps && player.upgrades.includes('chaos magnet') && new Random(`${state.course.seed}:${player.id}:${player.ball.strokes}:${powerUp}`).chance(.65)) {
    player.inventory = powerUpFor(state, player, 'chaos', 'chaos-magnet');
    addMessage(state, `${player.name}'s chaos magnet pulls ${player.inventory}`);
  }
};

export const beginCourse = (state: GameState): GameState => ({ ...state, status: 'playing', messages: ['tee off — aim with the board, then shoot', ...state.messages] });

export const tickTurn = (current: GameState, elapsedSeconds: number): GameState => {
  if ((current.status !== 'playing' && current.status !== 'validate') || current.turn.shotInFlight) return current;
  const state = cloneState(current);
  state.turn.secondsLeft = Math.max(0, state.turn.secondsLeft - elapsedSeconds);
  if (state.turn.secondsLeft === 0) {
    addMessage(state, `${activePlayer(state).name} timed out`);
    advanceCoursePhase(state);
    if (state.status === 'validate') {
      state.turn = { playerIndex: state.turn.playerIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
      return state;
    }
    advanceTurn(state);
  }
  return state;
};

const powerUpFor = (state: GameState, player: Player, kind: ItemPadKind, source: string): PowerUp => {
  const random = new Random(`${state.course.seed}:${source}:${player.id}:${state.coursePhase}:${player.ball.strokes}`);
  const leaderScore = Math.min(...state.players.map((candidate) => candidate.total + candidate.ball.strokes));
  const deficit = player.total + player.ball.strokes - leaderScore;
  if (deficit >= 2 && random.chance(.7)) return random.pick(recoveryPowerUps);
  return random.pick(kind === 'recovery' ? recoveryPowerUps : chaosPowerUps);
};

export const currentUpgradeChoices = (state: GameState): Upgrade[] => upgrades.map((_, index) => upgrades[(index + state.hole + state.turn.playerIndex) % upgrades.length]!);

export const botMove = (state: GameState): BotDecision | undefined => {
  const player = activePlayer(state);
  if (player.kind !== 'bot' || (state.status !== 'playing' && state.status !== 'validate') || state.turn.shotInFlight) return undefined;
  return chooseBotDecision(state.course, player, state.players, state.coursePhase);
};

export class LocalTransport implements GameTransport {
  private listeners = new Set<(command: GameCommand) => void>();
  send(command: GameCommand) { this.listeners.forEach((listener) => listener(command)); }
  onCommand(listener: (command: GameCommand) => void) { this.listeners.add(listener); return () => this.listeners.delete(listener); }
}
