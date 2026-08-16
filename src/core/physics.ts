import type { Ball, Course, Point, ShotCommand, Surface, Tile } from './types';

const STEP = 1 / 60;
const GRAVITY = -14;
const BALL_RADIUS = 0.18;
const BALL_DIAMETER = BALL_RADIUS * 2;

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

export interface BallPhysicsModifiers {
  mass?: number;
  iceSkates?: boolean;
  bankShot?: boolean;
  hazardShield?: boolean;
}

export interface OtherBallSimulation {
  ball: Ball;
  modifiers?: BallPhysicsModifiers;
}

export interface SimulationOptions {
  modifiers?: BallPhysicsModifiers;
  otherBalls?: readonly OtherBallSimulation[];
  collisions?: boolean;
}

export interface SimulationFrame {
  ball: Ball;
  otherBalls: Ball[];
}

export interface SimulationResult {
  ball: Ball;
  otherBalls: Ball[];
  frames: SimulationFrame[];
  holed: boolean;
  reset: boolean;
  otherResets: boolean[];
  shieldUsed: boolean;
  otherShieldUsed: boolean[];
}

interface Participant {
  ball: Ball;
  modifiers: BallPhysicsModifiers;
  reset: boolean;
  shieldUsed: boolean;
}

export const tileAt = (course: Course, x: number, y: number): Tile | undefined => {
  const column = Math.floor(x);
  const row = Math.floor(y);
  if (column < 0 || row < 0 || column >= course.width || row >= course.height) return undefined;
  return course.tiles[row * course.width + column];
};

export const tileCenter = (point: Point) => ({ x: point.x + 0.5, y: point.y + 0.5 });

export const newBall = (course: Course): Ball => {
  const tee = tileCenter(course.tee);
  const tile = tileAt(course, tee.x, tee.y)!;
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

const isOnGround = (course: Course, ball: Ball) => {
  const tile = tileAt(course, ball.x, ball.y);
  return Boolean(tile && tile.surface !== 'void' && ball.z <= tile.height + BALL_RADIUS + .01);
};

const snapshot = (participants: readonly Participant[]): SimulationFrame => ({
  ball: { ...participants[0]!.ball },
  otherBalls: participants.slice(1).map((participant) => ({ ...participant.ball })),
});

const sameFrame = (left: SimulationFrame, right: SimulationFrame) => JSON.stringify(left) === JSON.stringify(right);

const stepTerrain = (course: Course, participant: Participant) => {
  const previous = { ...participant.ball };
  let ball = participant.ball;
  ball.vz += GRAVITY * STEP;
  ball.x += ball.vx * STEP;
  ball.y += ball.vy * STEP;
  ball.z += ball.vz * STEP;

  const tile = tileAt(course, ball.x, ball.y);
  if (!tile || tile.surface === 'void') {
    if (participant.modifiers.hazardShield && !participant.shieldUsed) {
      participant.shieldUsed = true;
      participant.ball = { ...previous, vx: -previous.vx * .38, vy: -previous.vy * .38, vz: Math.max(0, previous.vz * .2) };
    } else {
      participant.reset = true;
      participant.ball = { ...previous, vx: 0, vy: 0, vz: 0, resetCount: previous.resetCount + 1 };
    }
    return;
  }
  if (tile.surface === 'wall') {
    const rebound = participant.modifiers.bankShot ? .82 : .52;
    participant.ball = { ...previous, vx: -previous.vx * rebound, vy: -previous.vy * rebound, vz: Math.max(0, previous.vz * .5) };
    return;
  }

  const floor = tile.height + BALL_RADIUS;
  if (ball.z <= floor) {
    ball.z = floor;
    ball.vz = ball.vz < -0.5 ? -ball.vz * .24 : 0;
    const friction = tile.surface === 'ice' && participant.modifiers.iceSkates ? .9994 : surfaceFriction[tile.surface];
    ball.vx *= friction;
    ball.vy *= friction;
    if (tile.slope) {
      ball.vx += tile.slope.x * STEP * 2.4;
      ball.vy += tile.slope.y * STEP * 2.4;
    }
    if (tile.surface === 'booster') {
      const direction = tile.direction ?? { x: 1, y: 0 };
      ball.vx += direction.x * .18;
      ball.vy += direction.y * .18;
    }
    if (tile.surface === 'conveyor') {
      const direction = tile.direction ?? { x: 1, y: 0 };
      ball.vx += direction.x * .045;
      ball.vy += direction.y * .045;
    }
  }
  participant.ball = ball;
};

const collide = (course: Course, participants: Participant[]) => {
  for (let first = 0; first < participants.length; first += 1) {
    for (let second = first + 1; second < participants.length; second += 1) {
      const left = participants[first]!;
      const right = participants[second]!;
      if (left.ball.complete || right.ball.complete) continue;
      if (!isOnGround(course, left.ball) || !isOnGround(course, right.ball)) continue;
      const dx = right.ball.x - left.ball.x;
      const dy = right.ball.y - left.ball.y;
      const distance = Math.hypot(dx, dy);
      if (distance > BALL_DIAMETER) continue;
      const normal = distance > .0001 ? { x: dx / distance, y: dy / distance } : { x: 1, y: 0 };
      const relativeVelocity = (left.ball.vx - right.ball.vx) * normal.x + (left.ball.vy - right.ball.vy) * normal.y;
      const leftMass = left.modifiers.mass ?? 1;
      const rightMass = right.modifiers.mass ?? 1;
      if (relativeVelocity <= 0) continue;
      const impulse = (1.78 * relativeVelocity) / (1 / leftMass + 1 / rightMass);
      left.ball.vx -= impulse / leftMass * normal.x;
      left.ball.vy -= impulse / leftMass * normal.y;
      right.ball.vx += impulse / rightMass * normal.x;
      right.ball.vy += impulse / rightMass * normal.y;
      const separation = (BALL_DIAMETER - distance + .001) / (1 / leftMass + 1 / rightMass);
      left.ball.x -= normal.x * separation / leftMass;
      left.ball.y -= normal.y * separation / leftMass;
      right.ball.x += normal.x * separation / rightMass;
      right.ball.y += normal.y * separation / rightMass;
    }
  }
};

const simulateMotion = (course: Course, initial: Ball, maxSeconds: number, options: SimulationOptions): SimulationResult => {
  const participants: Participant[] = [
    { ball: { ...initial }, modifiers: options.modifiers ?? {}, reset: false, shieldUsed: false },
    ...(options.otherBalls ?? []).map(({ ball, modifiers }) => ({ ball: { ...ball }, modifiers: modifiers ?? {}, reset: false, shieldUsed: false })),
  ];
  const frames: SimulationFrame[] = [];
  const cup = tileCenter(course.cup);
  let holed = false;

  for (let frame = 0; frame < maxSeconds / STEP; frame += 1) {
    participants.forEach((participant) => {
      if (!participant.ball.complete && !isStopped(participant.ball)) stepTerrain(course, participant);
    });
    if (options.collisions && participants.length > 1) collide(course, participants);

    const active = participants[0]!;
    const activeTile = tileAt(course, active.ball.x, active.ball.y);
    if (!active.ball.complete && activeTile && activeTile.surface !== 'void' && Math.hypot(active.ball.x - cup.x, active.ball.y - cup.y) < .28 && Math.hypot(active.ball.vx, active.ball.vy) < 1.9 && active.ball.z <= (tileAt(course, cup.x, cup.y)?.height ?? 0) + .3) {
      active.ball = { ...active.ball, x: cup.x, y: cup.y, vx: 0, vy: 0, vz: 0, complete: true };
      holed = true;
    }

    participants.forEach((participant) => {
      if (!participant.ball.complete && isStopped(participant.ball) && isOnGround(course, participant.ball)) participant.ball = { ...participant.ball, vx: 0, vy: 0, vz: 0 };
    });
    if (frame % 2 === 0) frames.push(snapshot(participants));
    if (participants.every((participant) => participant.ball.complete || (isStopped(participant.ball) && isOnGround(course, participant.ball)))) break;
  }

  const finalFrame = snapshot(participants);
  if (!frames.length || !sameFrame(frames.at(-1)!, finalFrame)) frames.push(finalFrame);
  return {
    ball: finalFrame.ball,
    otherBalls: finalFrame.otherBalls,
    frames,
    holed,
    reset: participants[0]!.reset,
    otherResets: participants.slice(1).map((participant) => participant.reset),
    shieldUsed: participants[0]!.shieldUsed,
    otherShieldUsed: participants.slice(1).map((participant) => participant.shieldUsed),
  };
};

export const simulateShot = (course: Course, initial: Ball, shot: ShotCommand, maxSeconds = 10, options: SimulationOptions = {}): SimulationResult => simulateMotion(course, applyShot(initial, shot), maxSeconds, options);

export const simulateImpulse = (course: Course, initial: Ball, velocity: Point, maxSeconds = 4, modifiers: BallPhysicsModifiers = {}): SimulationResult => simulateMotion(course, { ...initial, vx: velocity.x, vy: velocity.y, vz: Math.min(1.5, Math.hypot(velocity.x, velocity.y) * .08) }, maxSeconds, { modifiers });

export const distanceToCup = (course: Course, ball: Ball) => {
  const cup = tileCenter(course.cup);
  return Math.hypot(cup.x - ball.x, cup.y - ball.y);
};
