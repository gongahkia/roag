import { distanceToCup, newBall, simulateShot, tileAt } from './physics';
import { Random } from './random';
import type { Course, CourseScore, Point, ShotCommand, Surface, Tile } from './types';
import { COURSE_HEIGHT, COURSE_WIDTH } from './types';

const directions = [
  { x: 1, y: 0 },
  { x: 0, y: 1 },
  { x: -1, y: 0 },
  { x: 0, y: -1 },
];

const indexOf = (course: Pick<Course, 'width'>, point: Point) => point.y * course.width + point.x;

const baseTile = (): Tile => ({ surface: 'void', height: 0 });

const writeTile = (tiles: Tile[], point: Point, surface: Surface, height: number, width = COURSE_WIDTH) => {
  if (point.x < 0 || point.y < 0 || point.x >= width || point.y >= COURSE_HEIGHT) return;
  tiles[point.y * width + point.x] = { surface, height };
};

const carve = (tiles: Tile[], point: Point, height: number) => {
  for (let y = point.y - 1; y <= point.y + 1; y += 1) {
    for (let x = point.x - 1; x <= point.x + 1; x += 1) {
      if (x > 0 && y > 0 && x < COURSE_WIDTH - 1 && y < COURSE_HEIGHT - 1) writeTile(tiles, { x, y }, 'fairway', height);
    }
  }
};

const routeFor = (random: Random): Point[] => {
  const route: Point[] = [{ x: 1, y: random.int(3, COURSE_HEIGHT - 4) }];
  let position = { ...route[0]! };
  let steps = 0;
  while (position.x < COURSE_WIDTH - 3 && steps < 38) {
    const moves = position.x < COURSE_WIDTH - 6
      ? [{ x: 1, y: 0 }, { x: 1, y: 1 }, { x: 1, y: -1 }, { x: 0, y: random.pick([-1, 1]) }]
      : [{ x: 1, y: 0 }, { x: 1, y: 1 }, { x: 1, y: -1 }];
    const move = random.pick(moves);
    position = {
      x: Math.min(COURSE_WIDTH - 3, position.x + move.x),
      y: Math.max(2, Math.min(COURSE_HEIGHT - 3, position.y + move.y)),
    };
    if (route.at(-1)!.x !== position.x || route.at(-1)!.y !== position.y) route.push({ ...position });
    steps += 1;
  }
  route.push({ x: COURSE_WIDTH - 2, y: position.y });
  return route;
};

const reachable = (course: Course): boolean => {
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
      if (tile && tile.surface !== 'void' && tile.surface !== 'wall' && !seen.has(key(next))) queue.push(next);
    }
  }
  return false;
};

const solve = (course: Course): ShotCommand[] => {
  const ball = newBall(course);
  const cup = { x: course.cup.x + 0.5, y: course.cup.y + 0.5 };
  const baseAngle = Math.atan2(cup.y - ball.y, cup.x - ball.x);
  const candidates: { shot: ShotCommand; distance: number; holed: boolean }[] = [];
  for (let offset = -0.55; offset <= 0.55; offset += 0.11) {
    for (let power = 1.8; power <= 8; power += 0.35) {
      const shot = { angle: baseAngle + offset, power };
      const result = simulateShot(course, ball, shot);
      candidates.push({ shot, distance: distanceToCup(course, result.ball), holed: result.holed });
    }
  }
  const winner = candidates.filter((candidate) => candidate.holed).sort((a, b) => a.shot.power - b.shot.power)[0];
  if (winner) return [winner.shot];
  const close = candidates.sort((a, b) => a.distance - b.distance)[0];
  return close && close.distance < 2.4 ? [close.shot] : [];
};

const scoreCourse = (course: Course): CourseScore => {
  const hazards = course.tiles.filter((tile) => tile.surface === 'sand' || tile.surface === 'ice' || tile.surface === 'booster' || tile.surface === 'conveyor').length;
  const elevation = course.tiles.reduce((total, tile) => total + tile.height, 0);
  const branches = course.tiles.filter((tile) => tile.surface === 'fairway').length - course.route.length * 6;
  const solverShots = reachable(course) ? solve(course) : [];
  const playable = solverShots.length > 0;
  const estimatedStrokes = Math.max(1, Math.round(course.route.length / 7 + hazards / 15));
  const routes = Math.max(1, Math.min(4, Math.round(branches / 12) + 1));
  const novelty = Math.min(100, Math.round(hazards * 2.2 + elevation * 3.5 + routes * 12));
  const total = playable ? Math.round(55 + novelty * 0.25 + Math.min(20, estimatedStrokes * 4) + routes * 5) : 0;
  return { playable, estimatedStrokes, hazards, elevation, routes, novelty, total, solverShots, rejection: playable ? undefined : 'shot solver found no safe cup line' };
};

const decorate = (course: Course, random: Random) => {
  for (let index = 0; index < course.route.length; index += 1) {
    const point = course.route[index]!;
    const tile = course.tiles[indexOf(course, point)]!;
    if (index > 1 && index < course.route.length - 2 && random.chance(0.19)) {
      tile.height = Math.min(3, tile.height + random.int(1, 2));
      tile.slope = { x: random.pick([-1, 1]), y: random.pick([-1, 0, 1]) };
    }
  }
  for (let attempt = 0; attempt < 30; attempt += 1) {
    const point = { x: random.int(2, COURSE_WIDTH - 3), y: random.int(2, COURSE_HEIGHT - 3) };
    const tile = course.tiles[indexOf(course, point)]!;
    if (tile.surface !== 'fairway' || point.x === course.tee.x || point.x === course.cup.x) continue;
    if (random.chance(0.25)) tile.surface = 'sand';
    else if (random.chance(0.25)) tile.surface = 'ice';
    else if (random.chance(0.3)) {
      tile.surface = random.chance(0.5) ? 'booster' : 'conveyor';
      tile.direction = random.pick(directions);
    }
  }
};

export const generateCourse = (seed: string): Course => {
  const random = new Random(seed);
  const tiles = Array.from({ length: COURSE_WIDTH * COURSE_HEIGHT }, baseTile);
  const route = routeFor(random);
  let elevation = 0;
  for (const [index, point] of route.entries()) {
    if (index > 2 && random.chance(0.23)) elevation = Math.max(0, Math.min(3, elevation + random.pick([-1, 1])));
    carve(tiles, point, elevation);
  }
  const tee = route[0]!;
  const cup = route.at(-1)!;
  writeTile(tiles, tee, 'tee', tiles[indexOf({ width: COURSE_WIDTH }, tee)]!.height);
  writeTile(tiles, cup, 'cup', tiles[indexOf({ width: COURSE_WIDTH }, cup)]!.height);
  const course: Course = { id: `course-${seed}`, seed, width: COURSE_WIDTH, height: COURSE_HEIGHT, tiles, tee, cup, route, score: {} as CourseScore };
  decorate(course, random);
  course.score = scoreCourse(course);
  return course;
};

export const generateCandidates = (seed: string, count = 3): Course[] => {
  const accepted: Course[] = [];
  for (let attempt = 0; accepted.length < count && attempt < count * 25; attempt += 1) {
    const course = generateCourse(`${seed}-${attempt}`);
    if (course.score.playable) accepted.push(course);
  }
  return accepted.sort((a, b) => b.score.total - a.score.total);
};
