import { defaultTerrainSettings, generateCourse, randomTerrainSettings } from './generator';
import { COURSE_PHASES } from './hazards';
import { activePlayer, addMessage } from './game-state';
import { newBall } from './physics';
import { hashSeed } from './random';
import type { BuildState, Course, GameCommand, GameState, Point } from './types';

type BuildSettingsCommand = Extract<GameCommand, { type: 'build-settings' }>;

export const buildReady = (course: Course) => {
  const tee = course.tiles[course.tee.y * course.width + course.tee.x];
  const cup = course.tiles[course.cup.y * course.width + course.cup.x];
  return tee?.surface === 'tee' && cup?.surface === 'cup' && course.tiles.filter((tile) => tile.surface !== 'void' && tile.surface !== 'wall').length >= 8;
};

export const applyBuildSettings = (build: BuildState, command: BuildSettingsCommand) => {
  if (command.tool) build.tool = command.tool;
  if (command.height !== undefined) build.height = Math.max(0, Math.min(3, command.height));
  if (command.direction) build.direction = { ...command.direction };
  if (command.portalPairId !== undefined) build.portalPairId = Math.max(1, Math.floor(command.portalPairId));
  if (command.terrain) build.terrain = { ...build.terrain, ...command.terrain };
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

export const placeBuildTool = (state: GameState, point: Point) => {
  const build = state.build;
  if (!build || point.x < 0 || point.y < 0 || point.x >= state.course.width || point.y >= state.course.height) return;
  const tileIndex = point.y * state.course.width + point.x;
  const tool = build.tool;
  const clearFeatures = () => {
    state.course.hazards = state.course.hazards.filter((hazard) => hazard.point.x !== point.x || hazard.point.y !== point.y);
    state.course.itemPads = state.course.itemPads.filter((pad) => pad.point.x !== point.x || pad.point.y !== point.y);
    state.course.portals = (state.course.portals ?? []).map((pair) => ({
      ...pair,
      entrance: pair.entrance?.point.x === point.x && pair.entrance.point.y === point.y ? undefined : pair.entrance,
      exit: pair.exit?.point.x === point.x && pair.exit.point.y === point.y ? undefined : pair.exit,
    })).filter((pair) => pair.entrance || pair.exit);
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
  if (tool === 'portal-entrance' || tool === 'portal-exit') {
    if (state.course.tiles[tileIndex]!.surface === 'void') state.course.tiles[tileIndex] = { surface: 'fairway', height: build.height, corners: [build.height, build.height, build.height, build.height] };
    clearFeatures();
    const id = `portal-${build.portalPairId}`;
    const endpoint = tool === 'portal-entrance' ? 'entrance' : 'exit';
    const portals = state.course.portals ?? (state.course.portals = []);
    const pair = portals.find((candidate) => candidate.id === id);
    const placed = { point: { ...point }, direction: { ...build.direction } };
    if (pair) pair[endpoint] = placed;
    else portals.push({ id, [endpoint]: placed });
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

export const generateBuildTerrain = (state: GameState) => {
  const build = state.build;
  if (!build) return;
  const author = state.players[build.authorIndex]!;
  state.course = generateCourse(hashSeed(`${state.config.seed}-${author.id}-v${build.terrain.variation}`, state.authoredCourses.length + 1), build.terrain);
  build.generated = true;
  addMessage(state, `${author.name} generated terrain — edit it or validate it`);
};

export const randomizeBuildTerrain = (state: GameState) => {
  const build = state.build;
  if (!build) return;
  const author = state.players[build.authorIndex]!;
  const variation = build.terrain.variation + 1;
  build.terrain = randomTerrainSettings(`${state.config.seed}:${author.id}`, variation);
  build.generated = false;
  addMessage(state, `${author.name} rolls a new curated generator setup`);
};

export const selectCompetitivePerk = (state: GameState, upgrade: Extract<GameCommand, { type: 'select-upgrade' }>['upgrade']) => {
  const build = state.build;
  if (!build) return;
  const author = state.players[build.authorIndex]!;
  author.upgrades = [upgrade];
  addMessage(state, `${author.name} equips ${upgrade} for competition`);
};

export const beginValidation = (state: GameState) => {
  if (!buildReady(state.course)) {
    addMessage(state, 'place a tee, cup, and at least eight playable tiles first');
    return;
  }
  const authorIndex = state.build?.authorIndex ?? state.turn.playerIndex;
  state.players[authorIndex]!.ball = newBall(state.course);
  state.turn = { playerIndex: authorIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  state.coursePhase = 0;
  state.status = 'validate';
  addMessage(state, `${activePlayer(state).name} must sink this course once`);
};
