import { distanceToCup, newBall, simulateShot, tileAt } from './physics';
import { closedGateAt, COURSE_PHASES, elapsedMsForPhase } from './hazards';
import { Random } from './random';
import type { Course, CourseArchetype, CoursePackage, CourseScore, CourseSizeProfile, CourseTheme, HoleRules, Point, RouteRoleAssignment, ShotCommand, Surface, TerrainSettings, Tile } from './types';
import { COURSE_HEIGHT, COURSE_WIDTH } from './types';

const directions = [
  { x: 1, y: 0 },
  { x: 0, y: 1 },
  { x: -1, y: 0 },
  { x: 0, y: -1 },
];

const themes: CourseTheme[] = ['balanced', 'speedway', 'hazard-run', 'ice-rink', 'quarry', 'drift', 'bloom', 'pulse', 'carnival', 'marsh', 'zephyr'];
const archetypes: CourseArchetype[] = ['ribbon', 'switchback', 'fork', 'courtyard', 'slalom'];
const sizeProfiles: CourseSizeProfile[] = ['compact', 'standard', 'full'];

export const defaultTerrainSettings = (): TerrainSettings => ({
  width: COURSE_WIDTH,
  height: COURSE_HEIGHT,
  density: .55,
  elevation: .55,
  maxElevation: 3,
  routeLength: .7,
  bendiness: .4,
  laneWidth: 1,
  branches: 1,
  chaos: .45,
  theme: 'balanced',
  archetype: 'ribbon',
  sizeProfile: 'standard',
  roughRate: .13,
  sandRate: .16,
  iceRate: .12,
  boosterRate: .13,
  conveyorRate: .11,
  cushionRate: .08,
  springRate: .06,
  bumperCount: 1,
  wallCount: 2,
  sweeperCount: 1,
  gateCount: 1,
  portalPairs: 1,
  recoveryPads: 2,
  chaosPads: 2,
  sinkholePairs: 1,
  thornCount: 2,
  pulseCount: 2,
  updraftCount: 1,
  lowBarCount: 1,
  airRingCount: 1,
  gustCount: 1,
  variation: 0,
});

type CourseDimensions = Pick<TerrainSettings, 'width' | 'height'>;

const normalizeDimensions = (dimensions: Partial<CourseDimensions>): CourseDimensions => {
  const normalize = (value: number | undefined, fallback: number, minimum: number) => {
    const rounded = Number.isFinite(value) ? Math.round(value!) : fallback;
    return Number.isSafeInteger(rounded) ? Math.max(minimum, rounded) : fallback;
  };
  return {
    width: normalize(dimensions.width, COURSE_WIDTH, 14),
    height: normalize(dimensions.height, COURSE_HEIGHT, 10),
  };
};

const dimensionsForProfile = (dimensions: Partial<CourseDimensions>, profile: CourseSizeProfile): CourseDimensions => {
  const maximum = normalizeDimensions(dimensions);
  if (profile === 'full') return maximum;
  const scale = profile === 'compact' ? .7 : .86;
  return normalizeDimensions({ width: Math.floor(maximum.width * scale), height: Math.floor(maximum.height * scale) });
};

export const randomTerrainSettings = (seed: string, variation: number, dimensions: Partial<CourseDimensions> = {}): TerrainSettings => {
  const random = new Random(`${seed}:terrain:${variation}`);
  const size = normalizeDimensions(dimensions);
  const stepped = (min: number, max: number, step: number) => Number((min + random.int(0, Math.round((max - min) / step)) * step).toFixed(2));
  const theme = random.pick(themes);
  return {
    ...size,
    density: stepped(.1, 1, .05),
    elevation: stepped(0, 1, .05),
    maxElevation: random.int(1, 3),
    routeLength: stepped(.35, 1, .05),
    bendiness: stepped(0, 1, .05),
    laneWidth: random.int(1, 3),
    // Prefer a legible main run with occasional shortcuts over dense lawn-like
    // side paths; each course still retains deterministic route roles.
    branches: random.int(0, 2),
    chaos: stepped(0, 1, .05),
    theme,
    archetype: random.pick(archetypes),
    sizeProfile: 'standard',
    roughRate: stepped(.04, .3, .02),
    sandRate: stepped(.04, .34, .02),
    iceRate: stepped(.04, .34, .02),
    boosterRate: stepped(0, .3, .02),
    conveyorRate: stepped(0, .3, .02),
    cushionRate: stepped(0, .2, .02),
    springRate: stepped(0, .16, .02),
    bumperCount: random.int(0, 5),
    wallCount: random.int(0, 6),
    sweeperCount: random.int(0, 3),
    gateCount: random.int(0, 3),
    portalPairs: random.int(0, 2),
    recoveryPads: random.int(1, 4),
    chaosPads: random.int(1, 4),
    sinkholePairs: theme === 'drift' ? random.int(1, 2) : random.int(0, 1),
    thornCount: theme === 'bloom' ? random.int(1, 3) : random.int(0, 1),
    pulseCount: theme === 'pulse' ? random.int(1, 3) : random.int(0, 1),
    updraftCount: theme === 'pulse' ? random.int(1, 3) : random.int(0, 2),
    lowBarCount: random.int(0, 2),
    airRingCount: theme === 'speedway' ? random.int(1, 3) : random.int(0, 2),
    gustCount: theme === 'zephyr' ? random.int(1, 3) : random.int(0, 2),
    variation,
  };
};

const indexOf = (course: Pick<Course, 'width'>, point: Point) => point.y * course.width + point.x;

const baseTile = (): Tile => ({ surface: 'void', height: 0 });

const writeTile = (tiles: Tile[], point: Point, surface: Surface, height: number, width: number, courseHeight: number) => {
  if (point.x < 0 || point.y < 0 || point.x >= width || point.y >= courseHeight) return;
  tiles[point.y * width + point.x] = { surface, height };
};

const carve = (tiles: Tile[], point: Point, height: number, laneWidth: number, width: number, courseHeight: number) => {
  const radius = Math.max(1, Math.min(2, laneWidth));
  for (let y = point.y - radius; y <= point.y + radius; y += 1) {
    for (let x = point.x - radius; x <= point.x + radius; x += 1) {
      if (x > 0 && y > 0 && x < width - 1 && y < courseHeight - 1) writeTile(tiles, { x, y }, 'fairway', height, width, courseHeight);
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
  const verticalPadding = Math.max(2, Math.round(settings.height * .21));
  const minY = verticalPadding;
  const maxY = Math.max(minY, settings.height - verticalPadding - 1);
  const route: Point[] = [{ x: 1, y: random.int(minY, maxY) }];
  let position = { ...route[0]! };
  const minCupX = Math.max(8, Math.floor(settings.width / 2));
  const cupX = Math.max(minCupX, Math.min(settings.width - 2, Math.round(minCupX + settings.routeLength * (settings.width - 2 - minCupX))));
  const bendChance = .05 + settings.bendiness * .34 + (settings.archetype === 'slalom' ? .34 : 0);
  const verticalChance = settings.bendiness * .14 + (settings.archetype === 'switchback' ? .32 : 0);
  while (position.x < cupX) {
    let deltaY = random.chance(bendChance) ? random.pick([-1, 1]) : 0;
    if (settings.theme === 'speedway' && random.chance(.65)) deltaY = 0;
    if (settings.theme === 'drift' && random.chance(.32)) deltaY = random.pick([-1, 1]);
    if (settings.theme === 'pulse' && settings.archetype !== 'slalom' && random.chance(.7)) deltaY = 0;
    if (settings.theme === 'hazard-run' && random.chance(.16 + settings.bendiness * .18)) deltaY = random.pick([-1, 1]);
    position = {
      x: Math.min(cupX, position.x + 1),
      y: Math.max(minY, Math.min(maxY, position.y + deltaY)),
    };
    if (route.at(-1)!.x !== position.x || route.at(-1)!.y !== position.y) route.push({ ...position });
    if (position.x < cupX - 1 && random.chance(verticalChance)) {
      const direction = position.y <= minY ? 1 : position.y >= maxY ? -1 : random.pick([-1, 1]);
      const steps = settings.archetype === 'switchback' ? random.int(2, 4) : 1;
      for (let step = 0; step < steps; step += 1) {
        const nextY = Math.max(minY, Math.min(maxY, position.y + direction));
        if (nextY === position.y) break;
        position = { ...position, y: nextY };
        route.push({ ...position });
      }
    }
  }
  route.push({ x: cupX, y: position.y });
  return route;
};

const carveBranches = (tiles: Tile[], route: readonly Point[], random: Random, settings: TerrainSettings) => {
  for (let index = 0; index < settings.branches; index += 1) {
    const base = random.pick(route.slice(Math.min(2, route.length - 1), Math.max(3, route.length - 2)));
    const direction = random.pick([-1, 1]);
    const length = random.int(2, 3 + Math.round(settings.chaos * 3));
    const baseHeight = tiles[base.y * settings.width + base.x]!.height;
    let point = { ...base };
    for (let step = 0; step < length; step += 1) {
      point = { x: Math.max(1, Math.min(settings.width - 2, point.x + (random.chance(.35) ? 1 : 0))), y: Math.max(1, Math.min(settings.height - 2, point.y + direction)) };
      carve(tiles, point, baseHeight, Math.max(1, settings.laneWidth - 1), settings.width, settings.height);
    }
  }
};

const carveArchetype = (tiles: Tile[], route: readonly Point[], random: Random, settings: TerrainSettings) => {
  if (settings.archetype === 'courtyard') {
    const center = route[Math.floor(route.length / 2)]!;
    const height = tiles[indexOf({ width: settings.width }, center)]!.height;
    carve(tiles, center, height, 3, settings.width, settings.height);
    return;
  }
  if (settings.archetype !== 'fork' || route.length < 7) return;
  const start = route[Math.floor(route.length * .27)]!;
  const end = route[Math.floor(route.length * .73)]!;
  const side = start.y < settings.height / 2 ? 1 : -1;
  const height = tiles[indexOf({ width: settings.width }, start)]!.height;
  const span = Math.max(1, end.x - start.x);
  for (let x = start.x; x <= end.x; x += 1) {
    const progress = (x - start.x) / span;
    const arc = Math.sin(progress * Math.PI) * side * Math.max(2, Math.round(1 + settings.chaos * 3));
    const y = Math.max(1, Math.min(settings.height - 2, Math.round(start.y + (end.y - start.y) * progress + arc)));
    carve(tiles, { x, y }, height, Math.max(1, settings.laneWidth - 1), settings.width, settings.height);
  }
  if (random.chance(.5)) carveBranches(tiles, route.slice(Math.floor(route.length / 3), Math.floor(route.length * .75)), random, { ...settings, branches: 1 });
};

const reachable = (course: Course, phase = 0, phaseCount = COURSE_PHASES): boolean => {
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
      if (tile && tile.surface !== 'void' && tile.surface !== 'wall' && tile.surface !== 'bumper' && !closedGateAt(course, next.x + .5, next.y + .5, elapsedMsForPhase(phase), phaseCount) && !seen.has(key(next))) queue.push(next);
    }
  }
  return false;
};

const markerFor = (course: Course, fraction: number) => course.route[Math.min(course.route.length - 2, Math.max(1, Math.round((course.route.length - 1) * fraction)))]!;

/**
 * These roles are generation grammar annotations, not a second unbounded route
 * solver. They remain legible even when a Ribbon intentionally shares much of
 * its path: safe is broad and stable, skill calls out a timed/elevated section,
 * and conflict points at the shared contest space.
 */
const routeRolesFor = (course: Course): RouteRoleAssignment[] => {
  const safe = markerFor(course, .22);
  const skill = markerFor(course, .5);
  const conflict = markerFor(course, .72);
  return [
    { role: 'safe', label: 'safe line', marker: { ...safe }, points: course.route.slice(0, Math.max(2, Math.round(course.route.length * .56))).map((point) => ({ ...point })) },
    { role: 'skill', label: 'skill line', marker: { ...skill }, points: course.route.slice(Math.max(1, Math.round(course.route.length * .24)), Math.max(3, Math.round(course.route.length * .82))).map((point) => ({ ...point })) },
    { role: 'conflict', label: 'conflict line', marker: { ...conflict }, points: course.route.slice(Math.max(1, Math.round(course.route.length * .48))).map((point) => ({ ...point })) },
  ];
};

export interface CourseValidation {
  valid: boolean;
  failures: string[];
}

/** Validates the bounded authored grammar after terrain and hazard decoration. */
export const validateCourse = (course: Course, phaseCount = COURSE_PHASES): CourseValidation => {
  const failures: string[] = [];
  const tee = tileAt(course, course.tee.x + .5, course.tee.y + .5);
  const cup = tileAt(course, course.cup.x + .5, course.cup.y + .5);
  if (!tee || tee.surface === 'void' || tee.surface === 'wall' || tee.surface === 'bumper') failures.push('tee obstructed');
  if (!cup || cup.surface === 'void' || cup.surface === 'wall' || cup.surface === 'bumper') failures.push('cup obstructed');
  if (!course.route.length || !course.route.every((point) => {
    const tile = tileAt(course, point.x + .5, point.y + .5);
    return tile && tile.surface !== 'void' && tile.surface !== 'wall' && tile.surface !== 'bumper';
  })) failures.push('safe route obstructed');
  if (!Array.from({ length: phaseCount }, (_, phase) => reachable(course, phase, phaseCount)).every(Boolean)) failures.push('tee-to-cup route blocked by hazards');
  const roles = course.routeRoles ?? [];
  if (new Set(roles.map((assignment) => assignment.role)).size !== 3) failures.push('route roles missing');
  if (!roles.every((assignment) => assignment.points.length && tileAt(course, assignment.marker.x + .5, assignment.marker.y + .5)?.surface !== 'void')) failures.push('route role marker invalid');
  const chaosBudget = course.hazards.length + (course.features ?? []).length + course.tiles.filter((tile) => tile.surface === 'bumper' || tile.surface === 'spring').length;
  if (chaosBudget > 28) failures.push('chaos budget exceeded');
  return { valid: failures.length === 0, failures };
};

const solve = (course: Course, phase: number, phaseCount = COURSE_PHASES): ShotCommand[] => {
  const ball = newBall(course);
  const cup = { x: course.cup.x + 0.5, y: course.cup.y + 0.5 };
  const baseAngle = Math.atan2(cup.y - ball.y, cup.x - ball.x);
  const candidates: { shot: ShotCommand; distance: number; holed: boolean }[] = [];
  for (let offset = -0.55; offset <= 0.55; offset += 0.11) {
    for (let power = 1.8; power <= 8; power += 0.35) {
      const shot = { angle: baseAngle + offset, power };
      const result = simulateShot(course, ball, shot, undefined, { phase, phaseCount });
      candidates.push({ shot, distance: distanceToCup(course, result.ball), holed: result.holed });
    }
  }
  const winner = candidates.filter((candidate) => candidate.holed).sort((a, b) => a.shot.power - b.shot.power)[0];
  if (winner) return [winner.shot];
  return [];
};

const scoreCourse = (course: Course, phaseCount: number): CourseScore => {
  const hazards = course.tiles.filter((tile) => tile.surface === 'sand' || tile.surface === 'ice' || tile.surface === 'booster' || tile.surface === 'conveyor' || tile.surface === 'spring' || tile.surface === 'bumper').length + course.hazards.length + (course.features ?? []).filter((feature) => feature.kind === 'gust').length;
  const elevation = Math.round(course.tiles.reduce((total, tile) => total + tile.height, 0));
  const branches = course.tiles.filter((tile) => tile.surface === 'fairway').length - course.route.length * 6;
  const directShotDistance = Math.hypot(course.cup.x - course.tee.x, course.cup.y - course.tee.y);
  const needsSingleShotProof = directShotDistance <= 28;
  const solverShots = needsSingleShotProof ? solve(course, 0, phaseCount) : [];
  const reachableAcrossPhases = Array.from({ length: phaseCount }, (_, phase) => reachable(course, phase, phaseCount)).every(Boolean);
  const validation = validateCourse(course, phaseCount);
  const playable = validation.valid && reachableAcrossPhases && (!needsSingleShotProof || (solverShots.length > 0 && Array.from({ length: phaseCount }, (_, phase) => {
    const result = simulateShot(course, newBall(course), solverShots[0]!, undefined, { phase, phaseCount });
    return result.holed && result.settled;
  }).every(Boolean)));
  const estimatedStrokes = Math.max(1, Math.round(course.route.length / 7 + hazards / 15));
  const routes = Math.max(1, Math.min(4, Math.round(branches / 12) + 1));
  const novelty = Math.min(100, Math.round(hazards * 2.2 + elevation * 3.5 + routes * 12));
  const total = playable ? Math.round(55 + novelty * 0.25 + Math.min(20, estimatedStrokes * 4) + routes * 5) : 0;
  return { playable, estimatedStrokes, hazards, elevation, routes, novelty, total, solverShots, rejection: playable ? undefined : validation.failures[0] ?? 'shot solver found no safe cup line' };
};

const addHazard = (course: Course, random: Random, index: number, kind: 'sweeper' | 'gate' | 'updraft' | 'low-bar', phaseCount: number) => {
  const routeCells = new Set(course.route.map((point) => `${point.x},${point.y}`));
  const choices: Point[] = [];
  for (let y = 1; y < course.height - 1; y += 1) {
    for (let x = 1; x < course.width - 1; x += 1) {
      const point = { x, y };
      const tile = course.tiles[indexOf(course, point)]!;
      const openNeighbors = directions.filter((direction) => {
        const adjacent = tileAt(course, x + direction.x + .5, y + direction.y + .5);
      return adjacent && adjacent.surface !== 'void' && adjacent.surface !== 'wall' && adjacent.surface !== 'bumper';
    }).length;
      if (tile.surface !== 'void' && tile.surface !== 'wall' && tile.surface !== 'bumper' && !routeCells.has(`${x},${y}`) && openNeighbors >= 2) choices.push(point);
    }
  }
  const point = random.pick(choices.length ? choices : course.route.slice(2, -2));
  if (kind === 'sweeper') course.hazards.push({ id: `sweeper-${index}`, kind: 'sweeper', point, phaseOffset: random.int(0, phaseCount - 1), radius: .78, motionPeriodMs: random.pick([6_000, 8_000, 12_000]) });
  else if (kind === 'gate') course.hazards.push({ id: `gate-${index}`, kind: 'gate', point, phaseOffset: random.int(0, phaseCount - 1), motionPeriodMs: random.pick([6_000, 8_000, 12_000]) });
  else if (kind === 'updraft') course.hazards.push({ id: `updraft-${index}`, kind: 'updraft', point, direction: random.pick(directions), radius: .72 + random.next() * .26, strength: 2.1 + random.next() * 1.2 });
  else course.hazards.push({ id: `low-bar-${index}`, kind: 'low-bar', point, clearance: .66 + random.next() * .22 });
};

const addItemPads = (course: Course, random: Random, recoveryPads: number, chaosPads: number) => {
  const used = new Set<string>();
  const sideSteps = [{ x: 0, y: 0 }, { x: 0, y: 1 }, { x: 0, y: -1 }, { x: 1, y: 0 }, { x: -1, y: 0 }];
  const cashPads = Math.max(1, Math.round((recoveryPads + chaosPads) / 4));
  const pads = [...Array.from({ length: recoveryPads }, () => 'recovery' as const), ...Array.from({ length: chaosPads }, () => 'chaos' as const), ...Array.from({ length: cashPads }, () => 'cash' as const)];
  const fractions = pads.map((_, index) => (index + 1) / (pads.length + 1));
  fractions.forEach((fraction, index) => {
    const base = course.route[Math.min(course.route.length - 2, Math.max(2, Math.round((course.route.length - 1) * fraction)))]!;
    const candidates = sideSteps.map((step) => ({ x: base.x + step.x, y: base.y + step.y })).filter((point) => {
      const tile = tileAt(course, point.x + .5, point.y + .5);
      const key = `${point.x},${point.y}`;
      return tile && tile.surface !== 'void' && tile.surface !== 'wall' && tile.surface !== 'bumper' && !closedGateAt(course, point.x + .5, point.y + .5, 0) && !used.has(key);
    });
    const point = random.pick(candidates.length ? candidates : [base]);
    used.add(`${point.x},${point.y}`);
    course.itemPads.push({ id: `pad-${index}`, point, kind: pads[index]! });
  });
};

const addPortals = (course: Course, random: Random, count: number) => {
  const used = new Set<string>([`${course.tee.x},${course.tee.y}`, `${course.cup.x},${course.cup.y}`]);
  const candidates = course.tiles.flatMap((tile, index) => {
    if (tile.surface === 'void' || tile.surface === 'wall' || tile.surface === 'bumper') return [];
    const point = { x: index % course.width, y: Math.floor(index / course.width) };
    return course.route.some((route) => route.x === point.x && route.y === point.y) ? [] : [point];
  });
  for (let index = 0; index < count && candidates.length >= 2; index += 1) {
    const choose = () => {
      const available = candidates.filter((point) => !used.has(`${point.x},${point.y}`));
      return available.length ? random.pick(available) : undefined;
    };
    const entrance = choose();
    if (!entrance) break;
    used.add(`${entrance.x},${entrance.y}`);
    const exit = choose();
    if (!exit) break;
    used.add(`${exit.x},${exit.y}`);
    const directions = [
      { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }, { x: 0, y: -1 },
    ];
    course.portals!.push({ id: `portal-${index + 1}`, entrance: { point: entrance, direction: random.pick(directions) }, exit: { point: exit, direction: random.pick(directions) } });
  }
};

const featureCandidates = (course: Course, avoidRoute = false) => course.tiles.flatMap((tile, index) => {
  if (tile.surface === 'void' || tile.surface === 'wall' || tile.surface === 'bumper' || tile.surface === 'tee' || tile.surface === 'cup') return [];
  const point = { x: index % course.width, y: Math.floor(index / course.width) };
  if (avoidRoute && course.route.some((route) => route.x === point.x && route.y === point.y)) return [];
  if (course.itemPads.some((pad) => pad.point.x === point.x && pad.point.y === point.y)) return [];
  return [point];
});

const addBiomeFeatures = (course: Course, random: Random, settings: TerrainSettings) => {
  if (settings.theme === 'drift') {
    const used = new Set<string>();
    for (let index = 0; index < settings.sinkholePairs; index += 1) {
      const candidates = featureCandidates(course, true).filter((point) => !used.has(`${point.x},${point.y}`));
      if (candidates.length < 2) break;
      const entrance = random.pick(candidates);
      const distant = candidates.filter((candidate) => Math.hypot(candidate.x - entrance.x, candidate.y - entrance.y) >= 4);
      const exit = random.pick(distant.length ? distant : candidates.filter((candidate) => candidate.x !== entrance.x || candidate.y !== entrance.y));
      used.add(`${entrance.x},${entrance.y}`);
      used.add(`${exit.x},${exit.y}`);
      course.features.push({ id: `sinkhole-${index + 1}`, kind: 'sinkhole', entrance, exit });
    }
  }
  if (settings.theme === 'bloom') {
    const candidates = featureCandidates(course, true);
    for (let index = 0; index < settings.thornCount && candidates.length; index += 1) {
      const point = random.pick(candidates);
      course.features.push({ id: `thorn-${index + 1}`, kind: 'thorn', point, radius: .52 + settings.chaos * .12 });
    }
  }
  if (settings.theme === 'pulse') {
    const candidates = featureCandidates(course);
    for (let index = 0; index < settings.pulseCount && candidates.length; index += 1) {
      const point = random.pick(candidates);
      course.features.push({ id: `pulse-${index + 1}`, kind: 'pulse', point, direction: random.pick(directions), strength: 1.7 + settings.chaos * .8 });
    }
  }
};

const addAirRings = (course: Course, random: Random, count: number) => {
  const used = new Set<string>();
  for (let index = 0; index < count; index += 1) {
    const candidates = featureCandidates(course).filter((point) => !used.has(`${point.x},${point.y}`));
    if (!candidates.length) break;
    const point = random.pick(candidates);
    used.add(`${point.x},${point.y}`);
    course.features.push({ id: `air-ring-${index + 1}`, kind: 'air-ring', point, radius: .34 + random.next() * .12, boost: 1.16 + random.next() * .14 });
  }
};

const addGusts = (course: Course, random: Random, count: number) => {
  const used = new Set<string>();
  for (let index = 0; index < count; index += 1) {
    const candidates = featureCandidates(course).filter((point) => !used.has(`${point.x},${point.y}`));
    if (!candidates.length) break;
    const point = random.pick(candidates);
    used.add(`${point.x},${point.y}`);
    course.features.push({ id: `gust-${index + 1}`, kind: 'gust', point, direction: random.pick(directions), radius: .72 + random.next() * .3, strength: 1.35 + random.next() * 1.1 });
  }
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

const decorate = (course: Course, random: Random, settings: TerrainSettings, phaseCount: number) => {
  const featureAttempts = Math.round(6 + settings.density * 24 + settings.chaos * 26);
  const routeCells = new Set(course.route.map((point) => `${point.x},${point.y}`));
  for (let attempt = 0; attempt < featureAttempts; attempt += 1) {
    const point = { x: random.int(2, course.width - 3), y: random.int(2, course.height - 3) };
    const tile = course.tiles[indexOf(course, point)]!;
    if (tile.surface !== 'fairway' || point.x === course.tee.x || point.x === course.cup.x) continue;
    const surfaceRoll = random.next();
    const themeBoost = settings.theme === 'quarry' ? { sand: .12, ice: -.04, speed: -.03, cushion: 0, spring: 0 } : settings.theme === 'ice-rink' ? { sand: -.04, ice: .14, speed: -.02, cushion: 0, spring: 0 } : settings.theme === 'speedway' ? { sand: -.03, ice: -.02, speed: .13, cushion: -.03, spring: .04 } : settings.theme === 'drift' ? { sand: .03, ice: .08, speed: .02, cushion: 0, spring: 0 } : settings.theme === 'bloom' ? { sand: .09, ice: -.03, speed: -.04, cushion: .04, spring: 0 } : settings.theme === 'pulse' ? { sand: -.03, ice: -.02, speed: .1, cushion: 0, spring: .03 } : settings.theme === 'carnival' ? { sand: -.04, ice: -.04, speed: .04, cushion: -.04, spring: .16 } : settings.theme === 'marsh' ? { sand: .04, ice: -.05, speed: -.05, cushion: .18, spring: -.04 } : settings.theme === 'zephyr' ? { sand: -.02, ice: .02, speed: .04, cushion: 0, spring: .04 } : { sand: 0, ice: 0, speed: 0, cushion: 0, spring: 0 };
    const rough = settings.roughRate;
    const sand = Math.max(0, settings.sandRate + themeBoost.sand);
    const ice = Math.max(0, settings.iceRate + themeBoost.ice);
    const booster = Math.max(0, settings.boosterRate + themeBoost.speed);
    const conveyor = Math.max(0, settings.conveyorRate + themeBoost.speed / 2);
    const cushion = Math.max(0, settings.cushionRate + themeBoost.cushion);
    const spring = Math.max(0, settings.springRate + themeBoost.spring);
    const mayAlterPrimaryRoute = !routeCells.has(`${point.x},${point.y}`);
    if (surfaceRoll < rough) tile.surface = 'rough';
    else if (surfaceRoll < rough + sand) tile.surface = 'sand';
    else if (surfaceRoll < rough + sand + ice) tile.surface = 'ice';
    else if (surfaceRoll < rough + sand + ice + booster + conveyor) {
      tile.surface = surfaceRoll < rough + sand + ice + booster ? 'booster' : 'conveyor';
      tile.direction = random.pick(directions);
    } else if (mayAlterPrimaryRoute && surfaceRoll < rough + sand + ice + booster + conveyor + cushion) tile.surface = 'cushion';
    else if (mayAlterPrimaryRoute && surfaceRoll < rough + sand + ice + booster + conveyor + cushion + spring) tile.surface = 'spring';
  }
  let bumpers = 0;
  const bumperTarget = settings.bumperCount + (settings.theme === 'carnival' ? 2 : 0) + (settings.archetype === 'slalom' ? 1 : 0);
  for (let attempt = 0; attempt < 48 && bumpers < bumperTarget; attempt += 1) {
    const routePoint = random.pick(course.route.slice(2, -2));
    const direction = random.pick(directions);
    const point = { x: routePoint.x + direction.x, y: routePoint.y + direction.y };
    const tile = tileAt(course, point.x + .5, point.y + .5);
    if (!tile || tile.surface !== 'fairway' || routeCells.has(`${point.x},${point.y}`)) continue;
    tile.surface = 'bumper';
    bumpers += 1;
  }
  let walls = 0;
  const wallTarget = settings.wallCount;
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
  for (let index = 0; index < settings.sweeperCount; index += 1) addHazard(course, random, index, 'sweeper', phaseCount);
  for (let index = 0; index < settings.gateCount; index += 1) addHazard(course, random, index, 'gate', phaseCount);
  for (let index = 0; index < settings.updraftCount; index += 1) addHazard(course, random, index, 'updraft', phaseCount);
  for (let index = 0; index < settings.lowBarCount; index += 1) addHazard(course, random, index, 'low-bar', phaseCount);
  addPortals(course, random, settings.portalPairs);
  addItemPads(course, random, settings.recoveryPads, settings.chaosPads);
  addBiomeFeatures(course, random, settings);
  addAirRings(course, random, settings.airRingCount);
  addGusts(course, random, settings.gustCount + (settings.theme === 'zephyr' ? 1 : 0));
  addBarrierWalls(course);
};

const buildCourse = (seed: string, settings: TerrainSettings, phaseCount: number): Course => {
  const random = new Random(seed);
  const tiles = Array.from({ length: settings.width * settings.height }, baseTile);
  const route = routeFor(random, settings);
  let elevation = 0;
  for (const [index, point] of route.entries()) {
    const elevationChance = .06 + settings.elevation * .3 + (settings.theme === 'quarry' ? .06 : 0);
    if (index > 2 && random.chance(elevationChance)) elevation = Math.max(0, Math.min(settings.maxElevation, elevation + random.pick([-1, 1])));
    carve(tiles, point, elevation, settings.laneWidth, settings.width, settings.height);
  }
  carveBranches(tiles, route, random, settings);
  carveArchetype(tiles, route, random, settings);
  const tee = route[0]!;
  const cup = route.at(-1)!;
  writeTile(tiles, tee, 'tee', tiles[indexOf({ width: settings.width }, tee)]!.height, settings.width, settings.height);
  writeTile(tiles, cup, 'cup', tiles[indexOf({ width: settings.width }, cup)]!.height, settings.width, settings.height);
  const course: Course = { id: `course-${seed}`, seed, theme: settings.theme, archetype: settings.archetype, sizeProfile: settings.sizeProfile, width: settings.width, height: settings.height, tiles, tee, cup, route, hazards: [], features: [], portals: [], itemPads: [], score: {} as CourseScore };
  decorate(course, random, settings, phaseCount);
  smoothRampHeights(course);
  course.routeRoles = routeRolesFor(course);
  course.score = scoreCourse(course, phaseCount);
  return course;
};

/** Stable, compact hash of the materialized structure rather than the generator seed alone. */
export const courseHashFor = (course: Course) => {
  const material = JSON.stringify({
    seed: course.seed,
    theme: course.theme,
    archetype: course.archetype,
    sizeProfile: course.sizeProfile,
    width: course.width,
    height: course.height,
    tee: course.tee,
    cup: course.cup,
    route: course.route,
    routeRoles: course.routeRoles,
    tiles: course.tiles.map((tile) => [tile.surface, Number(tile.height.toFixed(6)), tile.corners?.map((height) => Number(height.toFixed(6))), tile.direction]),
    hazards: course.hazards,
    features: course.features,
    portals: course.portals,
    itemPads: course.itemPads,
  });
  let hash = 2_166_136_261;
  for (let index = 0; index < material.length; index += 1) hash = Math.imul(hash ^ material.charCodeAt(index), 16_777_619);
  return `gwye-${(hash >>> 0).toString(16).padStart(8, '0')}`;
};

export const generateCourse = (seed: string, settings: Partial<TerrainSettings> = {}, phaseCount = COURSE_PHASES): Course => {
  const resolvedSettings = { ...defaultTerrainSettings(), ...settings, ...normalizeDimensions(settings) };
  let fallback = buildCourse(seed, resolvedSettings, phaseCount);
  if (fallback.score.playable) return fallback;
  for (let attempt = 1; attempt <= 40; attempt += 1) {
    const candidate = buildCourse(`${seed}-retry-${attempt}`, resolvedSettings, phaseCount);
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

export const defaultHoleRules = (): HoleRules => ({
  timerSeconds: 24,
  strokeCap: 10,
  collisions: true,
  powerUps: true,
  recoveryBias: .5,
  launchMultiplier: 1,
  rollingResistanceMultiplier: 1,
  wallRestitutionMultiplier: 1,
  terrainAccelerationMultiplier: 1,
  hazardImpulseMultiplier: 1,
  portalSpeedMultiplier: 1,
  cupRadius: .28,
  hazardPhaseCount: COURSE_PHASES,
  scoreMultiplier: 1,
  sharedBoons: [],
});

const packageLabels = ['steady hands', 'hazard holiday', 'speed council', 'ice caucus', 'quarry motion', 'chaos compact'];

const randomHoleRules = (random: Random, dimensions: CourseDimensions): HoleRules => {
  const rules = defaultHoleRules();
  const powerUps = random.chance(.82);
  const longHoleCap = Math.ceil((dimensions.width - 2) / 6) + 2;
  return {
    ...rules,
    timerSeconds: random.pick([14, 18, 24, 30]),
    strokeCap: Math.max(random.pick([7, 9, 10, 12]), longHoleCap),
    collisions: random.chance(.7),
    powerUps,
    recoveryBias: random.pick([.25, .5, .75]),
    launchMultiplier: random.pick([.86, .94, 1, 1.08, 1.16]),
    rollingResistanceMultiplier: random.pick([.72, .86, 1, 1.16, 1.3]),
    wallRestitutionMultiplier: random.pick([.8, .94, 1, 1.12]),
    terrainAccelerationMultiplier: random.pick([.75, .9, 1, 1.15, 1.3]),
    hazardImpulseMultiplier: random.pick([.75, .9, 1, 1.18, 1.35]),
    portalSpeedMultiplier: random.pick([.86, 1, 1.15, 1.3]),
    cupRadius: random.pick([.23, .28, .33, .37]),
    hazardPhaseCount: random.pick([4, 6, 8, 10]),
    scoreMultiplier: random.pick([.75, 1, 1.25]),
    // Persistent Caddies and contraband are earned in the shared clubhouse shop,
    // never injected by a course package.
    startingPowerUp: undefined,
    sharedBoons: [],
  };
};

const optionLabel = (terrain: TerrainSettings, rules: HoleRules, index: number) => {
  const pace = rules.timerSeconds <= 18 ? 'quickfire' : rules.timerSeconds >= 30 ? 'long clock' : 'standard clock';
  return `${packageLabels[index % packageLabels.length]} · ${terrain.archetype} ${terrain.sizeProfile} · ${pace}`;
};

const guaranteedFallbackCourse = (seed: string, phaseCount: number, dimensions: CourseDimensions): Course => {
  const tee = { x: 1, y: Math.floor(dimensions.height / 2) };
  const cup = { x: dimensions.width - 2, y: tee.y };
  const tiles: Tile[] = Array.from({ length: dimensions.width * dimensions.height }, () => ({ surface: 'fairway', height: 0 }));
  tiles[indexOf({ width: dimensions.width }, tee)] = { surface: 'tee', height: 0 };
  tiles[indexOf({ width: dimensions.width }, cup)] = { surface: 'cup', height: 0 };
  const route = Array.from({ length: cup.x - tee.x + 1 }, (_, index) => ({ x: tee.x + index, y: tee.y }));
  const course: Course = { id: `fallback-${seed}`, seed, theme: 'balanced', archetype: 'ribbon', sizeProfile: 'full', width: dimensions.width, height: dimensions.height, tiles, tee, cup, route, hazards: [], features: [], portals: [], itemPads: [], score: {} as CourseScore };
  course.routeRoles = routeRolesFor(course);
  course.score = scoreCourse(course, phaseCount);
  return course;
};

export const generateCoursePackages = (seed: string, hole: number, dimensions: Partial<CourseDimensions> = {}): CoursePackage[] => {
  const courseSize = normalizeDimensions(dimensions);
  const options: CoursePackage[] = [];
  const seenCourses = new Set<string>();
  for (let attempt = 0; options.length < 3 && attempt < 96; attempt += 1) {
    const index = options.length;
    const sizeProfile = sizeProfiles[index]!;
    const optionSize = dimensionsForProfile(courseSize, sizeProfile);
    const random = new Random(`${seed}:hole:${hole}:option:${attempt}`);
    const terrain = {
      ...randomTerrainSettings(`${seed}:hole:${hole}`, attempt + 1, optionSize),
      variation: hole * 100 + attempt,
      // keep the package set readable: retries may change the layout seed, but not
      // the biome assigned to this particular compact/standard/full slot.
      theme: themes[(hole * 3 + index) % themes.length]!,
      archetype: archetypes[(hole + index) % archetypes.length]!,
      sizeProfile,
    };
    const rules = randomHoleRules(random, optionSize);
    const course = generateCourse(`${seed}:hole:${hole}:course:${attempt}`, terrain, rules.hazardPhaseCount);
    const signature = `${course.seed}:${course.tee.x},${course.tee.y}:${course.cup.x},${course.cup.y}`;
    if (!course.score.playable || seenCourses.has(signature)) continue;
    rules.strokeCap = Math.max(rules.strokeCap, course.score.estimatedStrokes + 2);
    seenCourses.add(signature);
    options.push({ id: `hole-${hole}-option-${index + 1}`, label: optionLabel(terrain, rules, index), recipe: { terrain, rules }, course });
  }
  while (options.length < 3) {
    const index = options.length;
    const sizeProfile = sizeProfiles[index]!;
    const optionSize = dimensionsForProfile(courseSize, sizeProfile);
    const terrain = { ...defaultTerrainSettings(), ...optionSize, variation: hole * 100 + 90 + index, theme: themes[(hole * 3 + index) % themes.length]!, archetype: archetypes[(hole + index) % archetypes.length]!, sizeProfile };
    const rules = defaultHoleRules();
    const generated = generateCourse(`${seed}:hole:${hole}:fallback:${index}`, terrain, rules.hazardPhaseCount);
    const course = generated.score.playable ? generated : guaranteedFallbackCourse(`${seed}:hole:${hole}:fallback:${index}`, rules.hazardPhaseCount, optionSize);
    rules.strokeCap = Math.max(rules.strokeCap, course.score.estimatedStrokes + 2);
    options.push({ id: `hole-${hole}-option-${index + 1}`, label: optionLabel(terrain, rules, index), recipe: { terrain, rules }, course });
  }
  return options;
};
