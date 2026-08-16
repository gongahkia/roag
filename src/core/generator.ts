import { distanceToCup, newBall, simulateShot, tileAt } from './physics';
import { closedGateAt, COURSE_PHASES } from './hazards';
import { Random } from './random';
import type { Course, CourseScore, CourseTheme, Point, ShotCommand, Surface, TerrainSettings, Tile } from './types';
import { COURSE_HEIGHT, COURSE_WIDTH } from './types';

const directions = [
  { x: 1, y: 0 },
  { x: 0, y: 1 },
  { x: -1, y: 0 },
  { x: 0, y: -1 },
];

const themes: CourseTheme[] = ['balanced', 'speedway', 'hazard-run', 'ice-rink', 'quarry'];

export const defaultTerrainSettings = (): TerrainSettings => ({
  density: .55,
  elevation: .55,
  hazards: 1,
  routeLength: .7,
  bendiness: .4,
  laneWidth: 1,
  branches: 1,
  chaos: .45,
  theme: 'balanced',
  variation: 0,
});

export const randomTerrainSettings = (seed: string, variation: number): TerrainSettings => {
  const random = new Random(`${seed}:terrain:${variation}`);
  const stepped = (min: number, max: number, step: number) => Number((min + random.int(0, Math.round((max - min) / step)) * step).toFixed(2));
  return {
    density: stepped(.1, 1, .05),
    elevation: stepped(0, 1, .05),
    hazards: random.int(0, 4),
    routeLength: stepped(.35, 1, .05),
    bendiness: stepped(0, 1, .05),
    laneWidth: random.int(1, 3),
    branches: random.int(0, 3),
    chaos: stepped(0, 1, .05),
    theme: random.pick(themes),
    variation,
  };
};

const indexOf = (course: Pick<Course, 'width'>, point: Point) => point.y * course.width + point.x;

const baseTile = (): Tile => ({ surface: 'void', height: 0 });

const writeTile = (tiles: Tile[], point: Point, surface: Surface, height: number, width = COURSE_WIDTH) => {
  if (point.x < 0 || point.y < 0 || point.x >= width || point.y >= COURSE_HEIGHT) return;
  tiles[point.y * width + point.x] = { surface, height };
};

const carve = (tiles: Tile[], point: Point, height: number, laneWidth: number) => {
  const radius = Math.max(1, Math.min(2, laneWidth));
  for (let y = point.y - radius; y <= point.y + radius; y += 1) {
    for (let x = point.x - radius; x <= point.x + radius; x += 1) {
      if (x > 0 && y > 0 && x < COURSE_WIDTH - 1 && y < COURSE_HEIGHT - 1) writeTile(tiles, { x, y }, 'fairway', height);
    }
  }
};

const smoothRampHeights = (course: Course) => {
  const vertexWidth = course.width + 1;
  const vertexHeights = Array.from({ length: vertexWidth * (course.height + 1) }, () => 0);
  const vertexIndex = (x: number, y: number) => y * vertexWidth + x;
  for (let y = 0; y <= course.height; y += 1) {
    for (let x = 0; x <= course.width; x += 1) {
      const nearby: number[] = [];
      for (let tileY = y - 1; tileY <= y; tileY += 1) {
        for (let tileX = x - 1; tileX <= x; tileX += 1) {
          const tile = tileAt(course, tileX + .5, tileY + .5);
          if (tile && tile.surface !== 'void') nearby.push(tile.height);
        }
      }
      vertexHeights[vertexIndex(x, y)] = nearby.length ? nearby.reduce((sum, height) => sum + height, 0) / nearby.length : 0;
    }
  }
  for (let y = 0; y < course.height; y += 1) {
    for (let x = 0; x < course.width; x += 1) {
      const tile = course.tiles[indexOf(course, { x, y })]!;
      if (tile.surface === 'void') continue;
      const corners: [number, number, number, number] = [
        vertexHeights[vertexIndex(x, y)]!,
        vertexHeights[vertexIndex(x + 1, y)]!,
        vertexHeights[vertexIndex(x + 1, y + 1)]!,
        vertexHeights[vertexIndex(x, y + 1)]!,
      ];
      tile.corners = corners;
      tile.height = corners.reduce((sum, height) => sum + height, 0) / 4;
    }
  }
};

const routeFor = (random: Random, settings: TerrainSettings): Point[] => {
  const route: Point[] = [{ x: 1, y: random.int(3, COURSE_HEIGHT - 4) }];
  let position = { ...route[0]! };
  const cupX = Math.max(10, Math.min(COURSE_WIDTH - 2, Math.round(10 + settings.routeLength * 8)));
  const bendChance = .05 + settings.bendiness * .34;
  const verticalChance = settings.bendiness * .14;
  let steps = 0;
  while (position.x < cupX && steps < 48) {
    let deltaY = random.chance(bendChance) ? random.pick([-1, 1]) : 0;
    if (settings.theme === 'speedway' && random.chance(.65)) deltaY = 0;
    if (settings.theme === 'hazard-run' && random.chance(.16 + settings.bendiness * .18)) deltaY = random.pick([-1, 1]);
    position = {
      x: Math.min(cupX, position.x + 1),
      y: Math.max(2, Math.min(COURSE_HEIGHT - 3, position.y + deltaY)),
    };
    if (route.at(-1)!.x !== position.x || route.at(-1)!.y !== position.y) route.push({ ...position });
    if (position.x < cupX - 1 && random.chance(verticalChance)) {
      position = { ...position, y: Math.max(2, Math.min(COURSE_HEIGHT - 3, position.y + random.pick([-1, 1]))) };
      if (route.at(-1)!.x !== position.x || route.at(-1)!.y !== position.y) route.push({ ...position });
    }
    steps += 1;
  }
  route.push({ x: cupX, y: position.y });
  return route;
};

const carveBranches = (tiles: Tile[], route: readonly Point[], random: Random, settings: TerrainSettings) => {
  for (let index = 0; index < settings.branches; index += 1) {
    const base = random.pick(route.slice(Math.min(2, route.length - 1), Math.max(3, route.length - 2)));
    const direction = random.pick([-1, 1]);
    const length = random.int(2, 3 + Math.round(settings.chaos * 3));
    const baseHeight = tiles[base.y * COURSE_WIDTH + base.x]!.height;
    let point = { ...base };
    for (let step = 0; step < length; step += 1) {
      point = { x: Math.max(1, Math.min(COURSE_WIDTH - 2, point.x + (random.chance(.35) ? 1 : 0))), y: Math.max(1, Math.min(COURSE_HEIGHT - 2, point.y + direction)) };
      carve(tiles, point, baseHeight, Math.max(1, settings.laneWidth - 1));
    }
  }
};

const reachable = (course: Course, phase = 0): boolean => {
  const seen = new Set<string>();
  const queue = [course.tee];
  const key = (point: Point) => `${point.x},${point.y}`;
  while (queue.length) {
    const current = queue.shift()!;
    if (current.x === course.cup.x && current.y === course.cup.y) return true;
    if (seen.has(key(current))) continue;
    seen.add(key(current));
    for (const direction of directions) {
      const next = { x: current.x + direction.x, y: current.y + direction.y };
      const tile = tileAt(course, next.x + 0.5, next.y + 0.5);
      if (tile && tile.surface !== 'void' && tile.surface !== 'wall' && !closedGateAt(course, next.x + .5, next.y + .5, phase) && !seen.has(key(next))) queue.push(next);
    }
  }
  return false;
};

const solve = (course: Course, phase: number): ShotCommand[] => {
  const ball = newBall(course);
  const cup = { x: course.cup.x + 0.5, y: course.cup.y + 0.5 };
  const baseAngle = Math.atan2(cup.y - ball.y, cup.x - ball.x);
  const candidates: { shot: ShotCommand; distance: number; holed: boolean }[] = [];
  for (let offset = -0.55; offset <= 0.55; offset += 0.11) {
    for (let power = 1.8; power <= 8; power += 0.35) {
      const shot = { angle: baseAngle + offset, power };
      const result = simulateShot(course, ball, shot, undefined, { phase });
      candidates.push({ shot, distance: distanceToCup(course, result.ball), holed: result.holed });
    }
  }
  const winner = candidates.filter((candidate) => candidate.holed).sort((a, b) => a.shot.power - b.shot.power)[0];
  if (winner) return [winner.shot];
  return [];
};

const scoreCourse = (course: Course): CourseScore => {
  const hazards = course.tiles.filter((tile) => tile.surface === 'sand' || tile.surface === 'ice' || tile.surface === 'booster' || tile.surface === 'conveyor').length + course.hazards.length;
  const elevation = Math.round(course.tiles.reduce((total, tile) => total + tile.height, 0));
  const branches = course.tiles.filter((tile) => tile.surface === 'fairway').length - course.route.length * 6;
  const solverShots = solve(course, 0);
  const playable = solverShots.length > 0 && Array.from({ length: COURSE_PHASES }, (_, phase) => {
    if (!reachable(course, phase)) return false;
    const result = simulateShot(course, newBall(course), solverShots[0]!, undefined, { phase });
    return result.holed && result.settled;
  }).every(Boolean);
  const estimatedStrokes = Math.max(1, Math.round(course.route.length / 7 + hazards / 15));
  const routes = Math.max(1, Math.min(4, Math.round(branches / 12) + 1));
  const novelty = Math.min(100, Math.round(hazards * 2.2 + elevation * 3.5 + routes * 12));
  const total = playable ? Math.round(55 + novelty * 0.25 + Math.min(20, estimatedStrokes * 4) + routes * 5) : 0;
  return { playable, estimatedStrokes, hazards, elevation, routes, novelty, total, solverShots, rejection: playable ? undefined : 'shot solver found no safe cup line' };
};

const addHazard = (course: Course, random: Random, index: number, theme: CourseTheme) => {
  const routeCells = new Set(course.route.map((point) => `${point.x},${point.y}`));
  const choices: Point[] = [];
  for (let y = 1; y < course.height - 1; y += 1) {
    for (let x = 1; x < course.width - 1; x += 1) {
      const point = { x, y };
      const tile = course.tiles[indexOf(course, point)]!;
      const openNeighbors = directions.filter((direction) => {
        const adjacent = tileAt(course, x + direction.x + .5, y + direction.y + .5);
        return adjacent && adjacent.surface !== 'void' && adjacent.surface !== 'wall';
      }).length;
      if (tile.surface !== 'void' && tile.surface !== 'wall' && !routeCells.has(`${x},${y}`) && openNeighbors >= 2) choices.push(point);
    }
  }
  const point = random.pick(choices.length ? choices : course.route.slice(2, -2));
  const sweeperChance = theme === 'hazard-run' ? .72 : theme === 'speedway' ? .42 : .56;
  if (random.chance(sweeperChance)) course.hazards.push({ id: `sweeper-${index}`, kind: 'sweeper', point, phaseOffset: random.int(0, COURSE_PHASES - 1), radius: .78 });
  else course.hazards.push({ id: `gate-${index}`, kind: 'gate', point, phaseOffset: random.int(0, COURSE_PHASES - 1) });
};

const addItemPads = (course: Course, random: Random, count: number) => {
  const used = new Set<string>();
  const sideSteps = [{ x: 0, y: 0 }, { x: 0, y: 1 }, { x: 0, y: -1 }, { x: 1, y: 0 }, { x: -1, y: 0 }];
  const fractions = Array.from({ length: count }, (_, index) => (index + 1) / (count + 1));
  fractions.forEach((fraction, index) => {
    const base = course.route[Math.min(course.route.length - 2, Math.max(2, Math.round((course.route.length - 1) * fraction)))]!;
    const candidates = sideSteps.map((step) => ({ x: base.x + step.x, y: base.y + step.y })).filter((point) => {
      const tile = tileAt(course, point.x + .5, point.y + .5);
      const key = `${point.x},${point.y}`;
      return tile && tile.surface !== 'void' && tile.surface !== 'wall' && !closedGateAt(course, point.x + .5, point.y + .5, 0) && !used.has(key);
    });
    const point = random.pick(candidates.length ? candidates : [base]);
    used.add(`${point.x},${point.y}`);
    course.itemPads.push({ id: `pad-${index}`, point, kind: index === 0 ? 'recovery' : random.chance(.5) ? 'recovery' : 'chaos' });
  });
};

const addBarrierWalls = (course: Course) => {
  const candidates = new Map<string, { point: Point; height: number }>();
  for (let y = 0; y < course.height; y += 1) {
    for (let x = 0; x < course.width; x += 1) {
      const tile = course.tiles[indexOf(course, { x, y })]!;
      if (tile.surface === 'void' || tile.surface === 'wall') continue;
      for (const direction of directions) {
        const point = { x: x + direction.x, y: y + direction.y };
        const neighbor = tileAt(course, point.x + .5, point.y + .5);
        if (neighbor?.surface !== 'void') continue;
        const key = `${point.x},${point.y}`;
        const existing = candidates.get(key);
        candidates.set(key, { point, height: Math.max(existing?.height ?? 0, tile.height) });
      }
    }
  }
  for (const { point, height } of candidates.values()) course.tiles[indexOf(course, point)] = { surface: 'wall', height };
};

const decorate = (course: Course, random: Random, settings: TerrainSettings) => {
  const featureAttempts = Math.round(6 + settings.density * 24 + settings.chaos * 26);
  for (let attempt = 0; attempt < featureAttempts; attempt += 1) {
    const point = { x: random.int(2, COURSE_WIDTH - 3), y: random.int(2, COURSE_HEIGHT - 3) };
    const tile = course.tiles[indexOf(course, point)]!;
    if (tile.surface !== 'fairway' || point.x === course.tee.x || point.x === course.cup.x) continue;
    const surfaceRoll = random.next();
    const sandChance = settings.theme === 'quarry' ? .5 : settings.theme === 'ice-rink' ? .08 : .24;
    const iceChance = settings.theme === 'ice-rink' ? .56 : settings.theme === 'quarry' ? .08 : .24;
    const speedChance = settings.theme === 'speedway' ? .58 : .3 + settings.chaos * .12;
    if (surfaceRoll < sandChance) tile.surface = 'sand';
    else if (surfaceRoll < sandChance + iceChance) tile.surface = 'ice';
    else if (surfaceRoll < sandChance + iceChance + speedChance) {
      tile.surface = random.chance(0.5) ? 'booster' : 'conveyor';
      tile.direction = random.pick(directions);
    }
  }
  let walls = 0;
  const wallTarget = Math.max(0, Math.min(5, Math.round(settings.density * 2 + settings.chaos * 2 + (settings.theme === 'quarry' ? 1 : 0))));
  for (let attempt = 0; attempt < 36 && walls < wallTarget; attempt += 1) {
    const routePoint = random.pick(course.route.slice(2, -2));
    const direction = random.pick(directions);
    const point = { x: routePoint.x + direction.x, y: routePoint.y + direction.y };
    const tile = tileAt(course, point.x + .5, point.y + .5);
    const onRoute = course.route.some((route) => route.x === point.x && route.y === point.y);
    if (!tile || tile.surface !== 'fairway' || onRoute) continue;
    const openNeighbors = directions.filter((neighbor) => {
      const adjacent = tileAt(course, point.x + neighbor.x + .5, point.y + neighbor.y + .5);
      return adjacent && adjacent.surface !== 'void' && adjacent.surface !== 'wall';
    }).length;
    if (openNeighbors < 2) continue;
    tile.surface = 'wall';
    walls += 1;
  }
  for (let index = 0; index < settings.hazards; index += 1) addHazard(course, random, index, settings.theme);
  addItemPads(course, random, 3 + Math.max(0, Math.round((settings.chaos - .5) * 4)));
  addBarrierWalls(course);
};

const buildCourse = (seed: string, settings: TerrainSettings): Course => {
  const random = new Random(seed);
  const tiles = Array.from({ length: COURSE_WIDTH * COURSE_HEIGHT }, baseTile);
  const route = routeFor(random, settings);
  let elevation = 0;
  for (const [index, point] of route.entries()) {
    const elevationChance = .06 + settings.elevation * .3 + (settings.theme === 'quarry' ? .06 : 0);
    if (index > 2 && random.chance(elevationChance)) elevation = Math.max(0, Math.min(3, elevation + random.pick([-1, 1])));
    carve(tiles, point, elevation, settings.laneWidth);
  }
  carveBranches(tiles, route, random, settings);
  const tee = route[0]!;
  const cup = route.at(-1)!;
  writeTile(tiles, tee, 'tee', tiles[indexOf({ width: COURSE_WIDTH }, tee)]!.height);
  writeTile(tiles, cup, 'cup', tiles[indexOf({ width: COURSE_WIDTH }, cup)]!.height);
  const course: Course = { id: `course-${seed}`, seed, width: COURSE_WIDTH, height: COURSE_HEIGHT, tiles, tee, cup, route, hazards: [], portals: [], itemPads: [], score: {} as CourseScore };
  decorate(course, random, settings);
  smoothRampHeights(course);
  course.score = scoreCourse(course);
  return course;
};

export const generateCourse = (seed: string, settings: Partial<TerrainSettings> = {}): Course => {
  const resolvedSettings = { ...defaultTerrainSettings(), ...settings };
  let fallback = buildCourse(seed, resolvedSettings);
  if (fallback.score.playable) return fallback;
  for (let attempt = 1; attempt <= 40; attempt += 1) {
    const candidate = buildCourse(`${seed}-retry-${attempt}`, resolvedSettings);
    if (candidate.score.playable) return candidate;
    if (candidate.score.total > fallback.score.total) fallback = candidate;
  }
  return fallback;
};

export const generateCandidates = (seed: string, count = 3): Course[] => {
  const accepted: Course[] = [];
  for (let attempt = 0; accepted.length < count && attempt < count * 25; attempt += 1) {
    const course = generateCourse(`${seed}-${attempt}`);
    if (course.score.playable) accepted.push(course);
  }
  return accepted.sort((a, b) => b.score.total - a.score.total);
};
