import type { Ball, Course, Point, ShotCommand, Surface, Tile, WorldRotation } from './types';

const STEP = 1 / 60;
const GRAVITY = -14;
const BALL_RADIUS = 0.18;

const surfaceFriction: Record<Surface, number> = {
  void: 0.995,
  fairway: 0.994,
  rough: 0.987,
  sand: 0.978,
  ice: 0.998,
  wall: 0.65,
  tee: 0.994,
  cup: 0.994,
  booster: 0.995,
  conveyor: 0.991,
};

export interface SimulationResult {
  ball: Ball;
  frames: Ball[];
  holed: boolean;
  reset: boolean;
  pickupIds: string[];
}

export const tileAt = (course: Course, x: number, y: number, rotation: WorldRotation = 0): Tile | undefined => {
  const column = Math.floor(x);
  const row = Math.floor(y);
  if (column < 0 || row < 0 || column >= course.width || row >= course.height) return undefined;
  const tile = course.tiles[row * course.width + column];
  return tile?.rotationGate === undefined || tile.rotationGate === rotation ? tile : undefined;
};

export const tileCenter = (point: Point) => ({ x: point.x + 0.5, y: point.y + 0.5 });

export const newBall = (course: Course, rotation: WorldRotation = 0): Ball => {
  const tee = tileCenter(course.tee);
  const tile = tileAt(course, tee.x, tee.y, rotation)!;
  return { x: tee.x, y: tee.y, z: tile.height + BALL_RADIUS, vx: 0, vy: 0, vz: 0, strokes: 0, complete: false, resetCount: 0 };
};

export const applyShot = (ball: Ball, shot: ShotCommand): Ball => ({
  ...ball,
  vx: Math.cos(shot.angle) * shot.power,
  vy: Math.sin(shot.angle) * shot.power,
  vz: Math.min(2.8, shot.power * 0.11),
  strokes: ball.strokes + 1,
});

const speed = (ball: Ball) => Math.hypot(ball.vx, ball.vy, ball.vz);

export const isStopped = (ball: Ball) => speed(ball) < 0.12;

export const simulateShot = (course: Course, initial: Ball, shot: ShotCommand, maxSeconds = 10, rotation: WorldRotation = 0): SimulationResult => {
  let ball = applyShot(initial, shot);
  const frames: Ball[] = [];
  const tee = tileCenter(course.tee);
  const cup = tileCenter(course.cup);
  let reset = false;
  let holed = false;
  const pickupIds = new Set<string>();

  for (let frame = 0; frame < maxSeconds / STEP; frame += 1) {
    const previous = { ...ball };
    ball.vz += GRAVITY * STEP;
    ball.x += ball.vx * STEP;
    ball.y += ball.vy * STEP;
    ball.z += ball.vz * STEP;

    const tile = tileAt(course, ball.x, ball.y, rotation);
    if (!tile || tile.surface === 'void') {
      ball = { ...previous, vx: 0, vy: 0, vz: 0 };
      reset = true;
      frames.push({ ...ball });
      break;
    }
    if (tile.surface === 'wall') {
      ball = { ...previous, vx: -previous.vx * 0.52, vy: -previous.vy * 0.52, vz: Math.max(0, previous.vz * 0.5) };
      frames.push({ ...ball });
      continue;
    }

    const floor = tile.height + BALL_RADIUS;
    if (ball.z <= floor) {
      ball.z = floor;
      ball.vz = ball.vz < -0.5 ? -ball.vz * 0.24 : 0;
      const friction = surfaceFriction[tile.surface];
      ball.vx *= friction;
      ball.vy *= friction;
      if (tile.slope) {
        ball.vx += tile.slope.x * STEP * 2.4;
        ball.vy += tile.slope.y * STEP * 2.4;
      }
      if (tile.surface === 'booster') {
        const direction = tile.direction ?? { x: 1, y: 0 };
        ball.vx += direction.x * 0.18;
        ball.vy += direction.y * 0.18;
      }
      if (tile.surface === 'conveyor') {
        const direction = tile.direction ?? { x: 1, y: 0 };
        ball.vx += direction.x * 0.045;
        ball.vy += direction.y * 0.045;
      }
    }

    for (const pickup of course.pickups) {
      if (!pickup.collected && pickup.rotation === rotation && Math.hypot(ball.x - pickup.point.x - 0.5, ball.y - pickup.point.y - 0.5) < 0.34) pickupIds.add(pickup.id);
    }

    if (Math.hypot(ball.x - cup.x, ball.y - cup.y) < 0.28 && Math.hypot(ball.vx, ball.vy) < 1.9 && ball.z <= (tileAt(course, cup.x, cup.y, rotation)?.height ?? 0) + 0.3) {
      ball = { ...ball, x: cup.x, y: cup.y, vx: 0, vy: 0, vz: 0, complete: true };
      holed = true;
      frames.push({ ...ball });
      break;
    }

    if (isStopped(ball) && ball.z <= floor + 0.001) {
      ball = { ...ball, vx: 0, vy: 0, vz: 0 };
      frames.push({ ...ball });
      break;
    }
    if (frame % 2 === 0) frames.push({ ...ball });
  }

  if (!frames.length) frames.push({ ...ball, x: tee.x, y: tee.y });
  else if (frames.at(-1)!.x !== ball.x || frames.at(-1)!.y !== ball.y || frames.at(-1)!.z !== ball.z || frames.at(-1)!.complete !== ball.complete) frames.push({ ...ball });
  return { ball, frames, holed, reset, pickupIds: [...pickupIds] };
};

export const distanceToCup = (course: Course, ball: Ball) => {
  const cup = tileCenter(course.cup);
  return Math.hypot(cup.x - ball.x, cup.y - ball.y);
};
