import { Random } from './random';
import type { ArchitectContract, ArchitectContractKind, BuildPieceId, BuildSocket, ConstructionState, Course, GameState, Player, Point, Tile } from './types';

export const BUILD_PIECES = ['bank', 'spring', 'bridge', 'gate', 'splitter', 'cushion'] as const satisfies readonly BuildPieceId[];

export const buildPieceDetail: Record<BuildPieceId, { label: string; description: string; glyph: string }> = {
  bank: { label: 'bank wall', description: 'a loud bumper that rewards a clean angle', glyph: '◇' },
  spring: { label: 'springboard', description: 'kicks a rolling ball into the air', glyph: '⌃' },
  bridge: { label: 'high bridge', description: 'a raised fast lane over the rough', glyph: '═' },
  gate: { label: 'timed gate', description: 'a predictable barrier with a visible rhythm', glyph: '▥' },
  splitter: { label: 'splitter', description: 'a conveyor junction that opens a fast exit', glyph: '⇄' },
  cushion: { label: 'cushion run', description: 'a forgiving brake zone for a safe recovery', glyph: '▤' },
};

const contractDetail: Record<ArchitectContractKind, { label: string; description: string }> = {
  traffic: { label: 'rush hour', description: 'another golfer uses one of your modules' },
  bank: { label: 'angle finder', description: 'a ball banks through one of your modules' },
  airtime: { label: 'air mail', description: 'a ball leaves the ground at one of your modules' },
  shortcut: { label: 'fast lane', description: 'a ball takes a powered route through one of your modules' },
};

const tile = (surface: Tile['surface'] = 'void', height = 0): Tile => ({ surface, height });
const keyFor = (point: Point) => `${point.x}:${point.y}`;
const inBounds = (course: Course, point: Point) => point.x >= 0 && point.y >= 0 && point.x < course.width && point.y < course.height;
const tileFor = (course: Course, point: Point) => course.tiles[point.y * course.width + point.x];
const setTile = (course: Course, point: Point, next: Tile) => {
  if (!inBounds(course, point)) return;
  course.tiles[point.y * course.width + point.x] = next;
};

const carve = (course: Course, point: Point, radius = 0) => {
  for (let y = point.y - radius; y <= point.y + radius; y += 1) {
    for (let x = point.x - radius; x <= point.x + radius; x += 1) {
      const target = { x, y };
      if (!inBounds(course, target)) continue;
      const current = tileFor(course, target);
      if (current?.surface === 'tee' || current?.surface === 'cup') continue;
      setTile(course, target, { surface: 'fairway', height: 0 });
    }
  }
};

const carveLine = (course: Course, from: Point, to: Point, radius = 0) => {
  let x = from.x;
  let y = from.y;
  const dx = Math.abs(to.x - from.x);
  const dy = -Math.abs(to.y - from.y);
  const stepX = from.x < to.x ? 1 : -1;
  const stepY = from.y < to.y ? 1 : -1;
  let error = dx + dy;
  while (true) {
    carve(course, { x, y }, radius);
    if (x === to.x && y === to.y) return;
    const twice = 2 * error;
    if (twice >= dy) { error += dy; x += stepX; }
    if (twice <= dx) { error += dx; y += stepY; }
  }
};

const socketPointsFor = (width: number, height: number, count: number) => {
  const middle = Math.floor(height / 2);
  const xStart = 3;
  const xEnd = Math.max(xStart, width - 4);
  const offsets = [-2, 2, -1, 1, -2, 2, -1, 1];
  const points: Point[] = [];
  for (let index = 0; index < count; index += 1) {
    const column = Math.round(xStart + (xEnd - xStart) * (count === 1 ? .5 : index / (count - 1)));
    const y = Math.max(1, Math.min(height - 2, middle + offsets[index % offsets.length]!));
    const candidate = { x: column, y };
    // Width can be as low as fourteen tiles. Keep every socket addressable even
    // then by moving a duplicated column onto the opposite side of the spine.
    if (points.some((point) => keyFor(point) === keyFor(candidate))) candidate.y = candidate.y === middle - 2 ? middle + 2 : middle - 2;
    points.push(candidate);
  }
  return points;
};

/** Creates a complete, conservative fairway spine with optional construction
 * bays. Every installed module changes a readable local route but leaves the
 * tee-to-cup backbone intact, preventing dead-hole griefing. */
export const buildShellFor = (seed: string, hole: number, width: number, height: number, playerCount: number): Course => {
  const resolvedWidth = Math.max(14, width);
  const resolvedHeight = Math.max(10, height);
  const middle = Math.floor(resolvedHeight / 2);
  const tee = { x: 1, y: middle };
  const cup = { x: resolvedWidth - 2, y: middle };
  const sockets = socketPointsFor(resolvedWidth, resolvedHeight, Math.max(2, playerCount * 2));
  const course: Course = {
    id: `coursewright-${seed}-${hole}`,
    seed: `${seed}:coursewright:${hole}`,
    theme: (['speedway', 'quarry', 'carnival'] as const)[(hole - 1) % 3]!,
    archetype: 'fork',
    sizeProfile: 'standard',
    width: resolvedWidth,
    height: resolvedHeight,
    tiles: Array.from({ length: resolvedWidth * resolvedHeight }, () => tile()),
    tee,
    cup,
    route: [],
    hazards: [],
    features: [],
    portals: [],
    itemPads: [],
    buildSockets: sockets.map((point, index) => ({ id: `socket-${hole}-${index + 1}`, point })),
    score: { playable: true, estimatedStrokes: 4, hazards: 0, elevation: 0, routes: 2, novelty: 0, total: 0, solverShots: [] },
  };
  carveLine(course, tee, cup, 1);
  sockets.forEach((socket) => {
    carveLine(course, { x: socket.x, y: middle }, socket, 1);
    carve(course, socket, 1);
  });
  setTile(course, tee, tile('tee'));
  setTile(course, cup, tile('cup'));
  course.route = Array.from({ length: cup.x - tee.x + 1 }, (_, index) => ({ x: tee.x + index, y: middle }));
  return course;
};

const contractFor = (random: Random, playerId: string, hole: number): ArchitectContract => {
  const kind = random.pick(['traffic', 'bank', 'airtime', 'shortcut'] as const);
  const detail = contractDetail[kind];
  return { id: `contract-${hole}-${playerId}`, ownerId: playerId, kind, label: detail.label, description: detail.description };
};

export const constructionFor = (seed: string, hole: number, players: readonly Player[]): ConstructionState => {
  const random = new Random(`${seed}:coursewright:build:${hole}`);
  const ids = players.map((player) => player.id);
  const hands: ConstructionState['hands'] = {};
  ids.forEach((id) => {
    const choices = [...BUILD_PIECES];
    hands[id] = [choices.splice(random.int(0, choices.length - 1), 1)[0]!, choices.splice(random.int(0, choices.length - 1), 1)[0]!];
  });
  return {
    placementOrder: [...ids, ...[...ids].reverse()],
    placementIndex: 0,
    hands,
    contracts: ids.map((id) => contractFor(random, id, hole)),
  };
};

export const activeBuilderId = (state: Pick<GameState, 'construction'>) => state.construction?.placementOrder[state.construction.placementIndex];

export const canPlaceBuildPiece = (state: Pick<GameState, 'construction' | 'course'>, playerId: string, pieceId: BuildPieceId, socketId: string) => {
  const construction = state.construction;
  const socket = state.course.buildSockets?.find((candidate) => candidate.id === socketId);
  return Boolean(construction && activeBuilderId(state) === playerId && construction.hands[playerId]?.includes(pieceId) && socket && !socket.pieceId);
};

const localTilesFor = (course: Course, point: Point) => [
  point,
  { x: point.x - 1, y: point.y },
  { x: point.x + 1, y: point.y },
  { x: point.x, y: point.y - 1 },
  { x: point.x, y: point.y + 1 },
].filter((candidate) => inBounds(course, candidate));

/** Mutates only the socket's local module. The shell's fairway spine remains
 * passable, so no legal build command can create an unwinnable hole. */
export const installBuildPiece = (course: Course, socket: BuildSocket, pieceId: BuildPieceId, ownerId: string) => {
  const point = socket.point;
  localTilesFor(course, point).forEach((candidate) => carve(course, candidate));
  const center = tileFor(course, point);
  if (!center) return;
  if (pieceId === 'bank') setTile(course, point, { ...center, surface: 'bumper' });
  if (pieceId === 'spring') setTile(course, point, { ...center, surface: 'spring' });
  if (pieceId === 'bridge') {
    setTile(course, point, { ...center, surface: 'fairway', height: .5, corners: [.5, .5, .5, .5] });
    [point.x - 1, point.x + 1].forEach((x) => {
      const neighbor = { x, y: point.y };
      const current = tileFor(course, neighbor);
      if (current?.surface !== 'void') setTile(course, neighbor, { ...current, surface: 'fairway', height: .25, corners: [.25, .25, .25, .25] });
    });
  }
  if (pieceId === 'gate') {
    course.hazards = course.hazards.filter((hazard) => hazard.id !== `build-gate-${socket.id}`);
    course.hazards.push({ id: `build-gate-${socket.id}`, kind: 'gate', point: { ...point }, phaseOffset: course.buildSockets?.findIndex((candidate) => candidate.id === socket.id) ?? 0, motionPeriodMs: 8_000 });
  }
  if (pieceId === 'splitter') setTile(course, point, { ...center, surface: 'conveyor', direction: { x: 1, y: 0 } });
  if (pieceId === 'cushion') setTile(course, point, { ...center, surface: 'cushion' });
  socket.ownerId = ownerId;
  socket.pieceId = pieceId;
};

const touchedSocketIds = (state: Pick<GameState, 'course'>, frames: readonly { ball: { x: number; y: number; z: number } }[]) => {
  const sockets = state.course.buildSockets ?? [];
  return new Set(sockets.filter((socket) => frames.some((frame) => Math.hypot(frame.ball.x - socket.point.x - .5, frame.ball.y - socket.point.y - .5) < .8)).map((socket) => socket.id));
};

export const resolveArchitectContracts = (state: GameState, shooterId: string, result: { frames: readonly { ball: { x: number; y: number; z: number } }[]; ricochetCount: number; airtimeSeconds: number }) => {
  const touched = touchedSocketIds(state, result.frames);
  const sockets = state.course.buildSockets ?? [];
  const completed: ArchitectContract[] = [];
  state.construction?.contracts.forEach((contract) => {
    if (contract.completed || !contract.kind) return;
    const ownedSockets = sockets.filter((socket) => socket.ownerId === contract.ownerId && touched.has(socket.id));
    if (!ownedSockets.length) return;
    const success = contract.kind === 'traffic'
      ? shooterId !== contract.ownerId
      : contract.kind === 'bank'
        ? result.ricochetCount > 0
        : contract.kind === 'airtime'
          ? result.airtimeSeconds >= .2
          : ownedSockets.some((socket) => socket.pieceId === 'splitter');
    if (!success) return;
    contract.completed = true;
    contract.revealed = true;
    completed.push(contract);
  });
  return completed;
};

export const architectContractDetail = (contract: ArchitectContract) => contract.kind && contract.kind in contractDetail ? contractDetail[contract.kind] : undefined;
