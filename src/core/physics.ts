import { closedGateAt, sweeperDirection } from './hazards';
import type { Ball, Course, Point, PortalEndpoint, PortalPair, ShotCommand, Surface, Tile } from './types';

const STEP = 1 / 60;
const BALL_RADIUS = 0.18;
const BALL_DIAMETER = BALL_RADIUS * 2;
const SLOPE_GRAVITY = 9.8;
const HEIGHT_TO_WORLD = 0.18;
const STOP_SPEED = 0.12;
const BOOST_ACCELERATION = 4.5;
const CONVEYOR_ACCELERATION = 1.2;

export const MAX_SETTLE_SECONDS = 18;
export const MAX_SURFACE_SPEED = 8;

const rollingDeceleration: Record<Surface, number> = {
  void: 0,
  fairway: 0.92,
  rough: 1.45,
  sand: 2.35,
  ice: 0.55,
  wall: 0,
  tee: 0.92,
  cup: 0.92,
  booster: 0.82,
  conveyor: 0.92,
};

export interface BallPhysicsModifiers {
  mass?: number;
  iceSkates?: boolean;
  bankShot?: boolean;
  bouncy?: boolean;
  ghostBall?: boolean;
  magnetBall?: boolean;
  portalExitId?: string;
  portalSpeedMultiplier?: number;
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
  phase?: number;
  collectItems?: boolean;
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
  itemPadIds: string[];
  /** false means the safety ceiling stopped an otherwise active simulation */
  settled: boolean;
}

interface Participant {
  ball: Ball;
  modifiers: BallPhysicsModifiers;
  reset: boolean;
  shieldUsed: boolean;
  ghostUsed: boolean;
  portalCooldown: number;
}

export const tileAt = (course: Course, x: number, y: number): Tile | undefined => {
  const column = Math.floor(x);
  const row = Math.floor(y);
  if (column < 0 || row < 0 || column >= course.width || row >= course.height) return undefined;
  return course.tiles[row * course.width + column];
};

export const tileCenter = (point: Point) => ({ x: point.x + 0.5, y: point.y + 0.5 });

export const tileCornerHeights = (tile: Tile): [number, number, number, number] => tile.corners ?? [tile.height, tile.height, tile.height, tile.height];

export const floorHeightAt = (course: Course, x: number, y: number): number => {
  const tile = tileAt(course, x, y);
  if (!tile) return 0;
  const [northWest, northEast, southEast, southWest] = tileCornerHeights(tile);
  const localX = x - Math.floor(x);
  const localY = y - Math.floor(y);
  return northWest * (1 - localX) * (1 - localY)
    + northEast * localX * (1 - localY)
    + southEast * localX * localY
    + southWest * (1 - localX) * localY;
};

export const floorGradientAt = (course: Course, x: number, y: number): Point => {
  const tile = tileAt(course, x, y);
  if (!tile) return { x: 0, y: 0 };
  const [northWest, northEast, southEast, southWest] = tileCornerHeights(tile);
  const localX = x - Math.floor(x);
  const localY = y - Math.floor(y);
  return {
    x: (northEast - northWest) * (1 - localY) + (southEast - southWest) * localY,
    y: (southWest - northWest) * (1 - localX) + (southEast - northEast) * localX,
  };
};

export const newBall = (course: Course): Ball => {
  const tee = tileCenter(course.tee);
  return { x: tee.x, y: tee.y, z: floorHeightAt(course, tee.x, tee.y) + BALL_RADIUS, vx: 0, vy: 0, vz: 0, strokes: 0, complete: false, resetCount: 0 };
};

export const applyShot = (ball: Ball, shot: ShotCommand): Ball => ({
  ...ball,
  vx: Math.cos(shot.angle) * shot.power,
  vy: Math.sin(shot.angle) * shot.power,
  vz: 0,
  strokes: ball.strokes + 1,
});

const planarSpeed = (ball: Ball) => Math.hypot(ball.vx, ball.vy);

const speed = (ball: Ball) => Math.hypot(ball.vx, ball.vy, ball.vz);

export const isStopped = (ball: Ball) => speed(ball) < STOP_SPEED;

const isOnGround = (course: Course, ball: Ball) => {
  const tile = tileAt(course, ball.x, ball.y);
  return Boolean(tile && tile.surface !== 'void' && Math.abs(ball.z - floorHeightAt(course, ball.x, ball.y) - BALL_RADIUS) < .01);
};

const snapshot = (participants: readonly Participant[]): SimulationFrame => ({
  ball: { ...participants[0]!.ball },
  otherBalls: participants.slice(1).map((participant) => ({ ...participant.ball })),
});

const sameFrame = (left: SimulationFrame, right: SimulationFrame) => JSON.stringify(left) === JSON.stringify(right);

const limitPlanarSpeed = (ball: Ball) => {
  const currentSpeed = planarSpeed(ball);
  if (currentSpeed <= MAX_SURFACE_SPEED) return;
  const scale = MAX_SURFACE_SPEED / currentSpeed;
  ball.vx *= scale;
  ball.vy *= scale;
};

const bounceNormal = (previous: Ball, next: Ball): Point => {
  const previousColumn = Math.floor(previous.x);
  const previousRow = Math.floor(previous.y);
  const nextColumn = Math.floor(next.x);
  const nextRow = Math.floor(next.y);
  const crossedX = nextColumn !== previousColumn;
  const crossedY = nextRow !== previousRow;
  if (crossedX && (!crossedY || Math.abs(next.vx) >= Math.abs(next.vy))) return { x: nextColumn > previousColumn ? -1 : 1, y: 0 };
  if (crossedY) return { x: 0, y: nextRow > previousRow ? -1 : 1 };
  return Math.abs(next.vx) >= Math.abs(next.vy) ? { x: next.vx > 0 ? -1 : 1, y: 0 } : { x: 0, y: next.vy > 0 ? -1 : 1 };
};

const reflect = (ball: Ball, normal: Point, restitution: number) => {
  const approach = ball.vx * normal.x + ball.vy * normal.y;
  if (approach >= 0) return { vx: ball.vx, vy: ball.vy };
  return {
    vx: ball.vx - (1 + restitution) * approach * normal.x,
    vy: ball.vy - (1 + restitution) * approach * normal.y,
  };
};

export const portalPairs = (course: Course): readonly PortalPair[] => course.portals ?? [];

export const portalEntranceAt = (course: Course, x: number, y: number): PortalPair | undefined => portalPairs(course).find((pair) => pair.entrance && pair.exit && pair.entrance.point.x === Math.floor(x) && pair.entrance.point.y === Math.floor(y));

const portalExitFor = (course: Course, pair: PortalPair, requestedId?: string): PortalEndpoint | undefined => {
  if (requestedId) return portalPairs(course).find((candidate) => `${candidate.id}:exit` === requestedId)?.exit;
  return pair.exit;
};

const rotateVelocity = (velocity: Point, entrance: Point, exit: Point, speedMultiplier: number) => {
  const rotation = Math.atan2(exit.y, exit.x) - Math.atan2(entrance.y, entrance.x);
  const cosine = Math.cos(rotation);
  const sine = Math.sin(rotation);
  return {
    x: (velocity.x * cosine - velocity.y * sine) * speedMultiplier,
    y: (velocity.x * sine + velocity.y * cosine) * speedMultiplier,
  };
};

const teleportThroughPortal = (course: Course, participant: Participant): boolean => {
  if (participant.portalCooldown > 0) {
    participant.portalCooldown -= 1;
    return false;
  }
  const pair = portalEntranceAt(course, participant.ball.x, participant.ball.y);
  if (!pair?.entrance) return false;
  const exit = portalExitFor(course, pair, participant.modifiers.portalExitId);
  if (!exit) return false;
  const velocity = rotateVelocity(participant.ball, pair.entrance.direction, exit.direction, participant.modifiers.portalSpeedMultiplier ?? 1);
  participant.ball = {
    ...participant.ball,
    x: exit.point.x + .5 + exit.direction.x * .34,
    y: exit.point.y + .5 + exit.direction.y * .34,
    z: floorHeightAt(course, exit.point.x + .5, exit.point.y + .5) + BALL_RADIUS,
    vx: velocity.x,
    vy: velocity.y,
    vz: 0,
  };
  participant.portalCooldown = 8;
  limitPlanarSpeed(participant.ball);
  return true;
};

const stopParticipant = (course: Course, participant: Participant) => {
  const tile = tileAt(course, participant.ball.x, participant.ball.y);
  participant.ball = {
    ...participant.ball,
    z: tile && tile.surface !== 'void' ? floorHeightAt(course, participant.ball.x, participant.ball.y) + BALL_RADIUS : participant.ball.z,
    vx: 0,
    vy: 0,
    vz: 0,
  };
};

const applySurfaceForces = (course: Course, ball: Ball, tile: Tile, modifiers: BallPhysicsModifiers) => {
  const gradient = floorGradientAt(course, ball.x, ball.y);
  ball.vx -= gradient.x * SLOPE_GRAVITY * HEIGHT_TO_WORLD * STEP;
  ball.vy -= gradient.y * SLOPE_GRAVITY * HEIGHT_TO_WORLD * STEP;
  if (tile.surface === 'booster' || tile.surface === 'conveyor') {
    const direction = tile.direction ?? { x: 1, y: 0 };
    const acceleration = tile.surface === 'booster' ? BOOST_ACCELERATION : CONVEYOR_ACCELERATION;
    ball.vx += direction.x * acceleration * STEP;
    ball.vy += direction.y * acceleration * STEP;
  }
  if (modifiers.magnetBall) {
    const pad = course.itemPads.filter((candidate) => !candidate.collected).map((candidate) => ({ pad: candidate, distance: Math.hypot(candidate.point.x + .5 - ball.x, candidate.point.y + .5 - ball.y) })).filter((candidate) => candidate.distance > .05 && candidate.distance < 3.25).sort((left, right) => left.distance - right.distance)[0];
    if (pad) {
      const pull = 2.1 * (1 - pad.distance / 3.25) * STEP;
      ball.vx += (pad.pad.point.x + .5 - ball.x) / pad.distance * pull;
      ball.vy += (pad.pad.point.y + .5 - ball.y) / pad.distance * pull;
    }
  }
  const currentSpeed = planarSpeed(ball);
  if (currentSpeed > 0) {
    const deceleration = tile.surface === 'ice' && modifiers.iceSkates ? .36 : rollingDeceleration[tile.surface];
    const reduction = Math.min(currentSpeed, deceleration * STEP);
    ball.vx -= ball.vx / currentSpeed * reduction;
    ball.vy -= ball.vy / currentSpeed * reduction;
  }
  limitPlanarSpeed(ball);
};

const stepTerrain = (course: Course, participant: Participant, phase: number) => {
  const previous = { ...participant.ball };
  const ball = participant.ball;
  ball.x += ball.vx * STEP;
  ball.y += ball.vy * STEP;
  ball.vz = 0;

  if (teleportThroughPortal(course, participant)) return;

  const tile = tileAt(course, ball.x, ball.y);
  if (!tile || tile.surface === 'void') {
    if (participant.modifiers.hazardShield && !participant.shieldUsed) {
      participant.shieldUsed = true;
      const normal = bounceNormal(previous, ball);
      const velocity = reflect(previous, normal, .38);
      participant.ball = { ...previous, ...velocity, z: floorHeightAt(course, previous.x, previous.y) + BALL_RADIUS, vz: 0 };
    } else {
      participant.reset = true;
      participant.ball = { ...previous, vx: 0, vy: 0, vz: 0, resetCount: previous.resetCount + 1 };
    }
    return;
  }
  if (tile.surface === 'wall' || closedGateAt(course, ball.x, ball.y, phase)) {
    if (participant.modifiers.ghostBall && !participant.ghostUsed) {
      participant.ghostUsed = true;
      if (tile.surface === 'wall') {
        const normal = bounceNormal(previous, ball);
        if (normal.x) ball.x = normal.x < 0 ? Math.floor(ball.x) + 1.001 : Math.floor(ball.x) - .001;
        if (normal.y) ball.y = normal.y < 0 ? Math.floor(ball.y) + 1.001 : Math.floor(ball.y) - .001;
      }
      const passedTile = tileAt(course, ball.x, ball.y);
      if (!passedTile || passedTile.surface === 'void') {
        participant.reset = true;
        participant.ball = { ...previous, vx: 0, vy: 0, vz: 0, resetCount: previous.resetCount + 1 };
        return;
      }
      ball.z = floorHeightAt(course, ball.x, ball.y) + BALL_RADIUS;
      applySurfaceForces(course, ball, passedTile, participant.modifiers);
      return;
    }
    const normal = bounceNormal(previous, ball);
    const restitution = participant.modifiers.bouncy ? .94 : participant.modifiers.bankShot ? .82 : .52;
    const velocity = reflect(previous, normal, restitution);
    participant.ball = { ...previous, ...velocity, z: floorHeightAt(course, previous.x, previous.y) + BALL_RADIUS, vz: 0 };
    return;
  }

  ball.z = floorHeightAt(course, ball.x, ball.y) + BALL_RADIUS;
  applySurfaceForces(course, ball, tile, participant.modifiers);
};

const collideWithSweepers = (course: Course, participants: Participant[], phase: number) => {
  for (const hazard of course.hazards) {
    if (hazard.kind !== 'sweeper') continue;
    const center = { x: hazard.point.x + .5, y: hazard.point.y + .5 };
    const direction = sweeperDirection(hazard, phase);
    const perpendicular = { x: -direction.y, y: direction.x };
    for (const participant of participants) {
      if (participant.ball.complete || !isOnGround(course, participant.ball)) continue;
      const relative = { x: participant.ball.x - center.x, y: participant.ball.y - center.y };
      const along = relative.x * direction.x + relative.y * direction.y;
      const sideways = relative.x * perpendicular.x + relative.y * perpendicular.y;
      const thickness = .16 + BALL_RADIUS;
      if (along < .04 || along > hazard.radius || Math.abs(sideways) > thickness) continue;
      const side = sideways === 0 ? 1 : Math.sign(sideways);
      const normal = { x: perpendicular.x * side, y: perpendicular.y * side };
      const overlap = thickness - Math.abs(sideways) + .01;
      participant.ball.x += normal.x * overlap;
      participant.ball.y += normal.y * overlap;
      participant.ball.z = floorHeightAt(course, participant.ball.x, participant.ball.y) + BALL_RADIUS;
      participant.ball.vx += normal.x * 1.55 + direction.x * .45;
      participant.ball.vy += normal.y * 1.55 + direction.y * .45;
      limitPlanarSpeed(participant.ball);
    }
  }
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
      left.ball.z = floorHeightAt(course, left.ball.x, left.ball.y) + BALL_RADIUS;
      right.ball.z = floorHeightAt(course, right.ball.x, right.ball.y) + BALL_RADIUS;
      limitPlanarSpeed(left.ball);
      limitPlanarSpeed(right.ball);
    }
  }
};

const allSettled = (course: Course, participants: Participant[]) => participants.every((participant) => participant.ball.complete || (isStopped(participant.ball) && isOnGround(course, participant.ball)));

const simulateMotion = (course: Course, initial: Ball, maxSeconds: number, options: SimulationOptions): SimulationResult => {
  const participants: Participant[] = [
    { ball: { ...initial }, modifiers: options.modifiers ?? {}, reset: false, shieldUsed: false, ghostUsed: false, portalCooldown: 0 },
    ...(options.otherBalls ?? []).map(({ ball, modifiers }) => ({ ball: { ...ball }, modifiers: modifiers ?? {}, reset: false, shieldUsed: false, ghostUsed: false, portalCooldown: 0 })),
  ];
  const frames: SimulationFrame[] = [];
  const cup = tileCenter(course.cup);
  const phase = options.phase ?? 0;
  const itemPadIds = new Set<string>();
  const settleAtEnd = maxSeconds >= MAX_SETTLE_SECONDS;
  let holed = false;
  let settled = false;

  for (let frame = 0; frame < maxSeconds / STEP; frame += 1) {
    participants.forEach((participant) => {
      if (!participant.ball.complete && !isStopped(participant.ball)) stepTerrain(course, participant, phase);
    });
    collideWithSweepers(course, participants, phase);
    if (options.collisions && participants.length > 1) collide(course, participants);

    const active = participants[0]!;
    const activeTile = tileAt(course, active.ball.x, active.ball.y);
    if (!active.ball.complete && activeTile && activeTile.surface !== 'void' && Math.hypot(active.ball.x - cup.x, active.ball.y - cup.y) < .28 && planarSpeed(active.ball) < 1.9 && active.ball.z <= floorHeightAt(course, cup.x, cup.y) + .3) {
      active.ball = { ...active.ball, x: cup.x, y: cup.y, z: floorHeightAt(course, cup.x, cup.y) + BALL_RADIUS, vx: 0, vy: 0, vz: 0, complete: true };
      holed = true;
    }
    if (options.collectItems && !itemPadIds.size && !active.ball.complete) {
      const pad = course.itemPads.find((candidate) => !candidate.collected && Math.hypot(active.ball.x - candidate.point.x - .5, active.ball.y - candidate.point.y - .5) < .32);
      if (pad) itemPadIds.add(pad.id);
    }

    participants.forEach((participant) => {
      if (!participant.ball.complete && isStopped(participant.ball) && isOnGround(course, participant.ball)) stopParticipant(course, participant);
    });
    if (frame % 2 === 0) frames.push(snapshot(participants));
    if (allSettled(course, participants)) {
      settled = true;
      break;
    }
  }

  if (!settled && settleAtEnd) participants.forEach((participant) => { if (!participant.ball.complete) stopParticipant(course, participant); });
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
    itemPadIds: [...itemPadIds],
    settled,
  };
};

export const simulateShot = (course: Course, initial: Ball, shot: ShotCommand, maxSeconds = MAX_SETTLE_SECONDS, options: SimulationOptions = {}): SimulationResult => simulateMotion(course, applyShot(initial, shot), maxSeconds, options);

export const simulateImpulse = (course: Course, initial: Ball, velocity: Point, maxSeconds = MAX_SETTLE_SECONDS, modifiers: BallPhysicsModifiers = {}, phase = 0): SimulationResult => simulateMotion(course, { ...initial, vx: velocity.x, vy: velocity.y, vz: 0 }, maxSeconds, { modifiers, phase });

export const distanceToCup = (course: Course, ball: Ball) => {
  const cup = tileCenter(course.cup);
  return Math.hypot(cup.x - ball.x, cup.y - ball.y);
};
