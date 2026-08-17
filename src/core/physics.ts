import { closedGateAt, sweeperDirection } from './hazards';
import type { Ball, Course, Gadget, Point, PortalEndpoint, PortalPair, RealityCard, ShotCommand, Surface, Tile } from './types';

const STEP = 1 / 60;
export const BALL_RADIUS = 0.18;
const BALL_DIAMETER = BALL_RADIUS * 2;
const SLOPE_GRAVITY = 9.8;
const HEIGHT_TO_WORLD = 0.18;
const STOP_SPEED = 0.12;
const BOOST_ACCELERATION = 4.5;
const CONVEYOR_ACCELERATION = 1.2;
const FALL_GRAVITY = 8.6;
const FALL_DURATION_FRAMES = 30;
export const CHIP_GRAVITY = 7.6;
const CHIP_HORIZONTAL_MULTIPLIER = .88;
const CHIP_MIN_LIFT = 1.2;
const CHIP_LIFT_PER_POWER = .5;
const CHIP_MAX_LIFT = 5.2;
const WALL_CLEARANCE_HEIGHT = .7;
const GATE_CLEARANCE_HEIGHT = .38;
/** A ball drops once roughly one third of its area overlaps the cup. */
export const CUP_CAPTURE_COVERAGE = .3;

export const MAX_SETTLE_SECONDS = 18;
export const MAX_SURFACE_SPEED = 8;
export const MAX_DOWNHILL_ROLL_SPEED = 3.6;

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
  rollingResistanceMultiplier?: number;
  wallRestitutionMultiplier?: number;
  terrainAccelerationMultiplier?: number;
  hazardImpulseMultiplier?: number;
  cupRadius?: number;
  cupMagnet?: boolean;
  slipstream?: boolean;
  reboundRig?: boolean;
  chipGravityMultiplier?: number;
  roughRider?: boolean;
  sandWedge?: boolean;
  gatecrasher?: boolean;
  thornmail?: boolean;
  anvilBall?: boolean;
  mirrorBall?: boolean;
  sidewaysGravity?: boolean;
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
  phaseCount?: number;
  collectItems?: boolean;
  gadgets?: readonly Gadget[];
  reality?: RealityCard;
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
  gadgetIds: string[];
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
  fallFramesRemaining: number;
  featureCooldown: number;
  returnBall?: Ball;
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

const chipLiftFor = (power: number) => Math.min(CHIP_MAX_LIFT, CHIP_MIN_LIFT + power * CHIP_LIFT_PER_POWER);

export const chipFlightSeconds = (shot: ShotCommand) => shot.kind === 'chip' ? chipLiftFor(shot.power) * 2 / CHIP_GRAVITY : 0;

export const shotVelocityFor = (shot: ShotCommand) => {
  const horizontalPower = shot.kind === 'chip' ? shot.power * CHIP_HORIZONTAL_MULTIPLIER : shot.power;
  return {
    vx: Math.cos(shot.angle) * horizontalPower,
    vy: Math.sin(shot.angle) * horizontalPower,
    vz: shot.kind === 'chip' ? chipLiftFor(shot.power) : 0,
  };
};

export const applyShot = (ball: Ball, shot: ShotCommand): Ball => {
  const velocity = shotVelocityFor(shot);
  return { ...ball, ...velocity, strokes: ball.strokes + 1 };
};

/** Returns the fraction of the ball's circular footprint sitting over the cup. */
export const cupCoverageAt = (distance: number, cupRadius: number) => {
  if (distance >= BALL_RADIUS + cupRadius) return 0;
  if (distance <= Math.abs(cupRadius - BALL_RADIUS)) return cupRadius >= BALL_RADIUS ? 1 : cupRadius ** 2 / BALL_RADIUS ** 2;
  const clamp = (value: number) => Math.max(-1, Math.min(1, value));
  const ballArea = BALL_RADIUS ** 2 * Math.acos(clamp((distance ** 2 + BALL_RADIUS ** 2 - cupRadius ** 2) / (2 * distance * BALL_RADIUS)));
  const cupArea = cupRadius ** 2 * Math.acos(clamp((distance ** 2 + cupRadius ** 2 - BALL_RADIUS ** 2) / (2 * distance * cupRadius)));
  const sharedArea = ballArea + cupArea - .5 * Math.sqrt(Math.max(0, (-distance + BALL_RADIUS + cupRadius) * (distance + BALL_RADIUS - cupRadius) * (distance - BALL_RADIUS + cupRadius) * (distance + BALL_RADIUS + cupRadius)));
  return sharedArea / (Math.PI * BALL_RADIUS ** 2);
};

const planarSpeed = (ball: Ball) => Math.hypot(ball.vx, ball.vy);

const speed = (ball: Ball) => Math.hypot(ball.vx, ball.vy, ball.vz);

export const isStopped = (ball: Ball) => speed(ball) < STOP_SPEED;

const isOnGround = (course: Course, ball: Ball) => {
  const tile = tileAt(course, ball.x, ball.y);
  return Boolean(tile && tile.surface !== 'void' && Math.abs(ball.z - floorHeightAt(course, ball.x, ball.y) - BALL_RADIUS) < .01);
};

const rollingDecelerationFor = (tile: Tile, modifiers: BallPhysicsModifiers) => {
  const base = tile.surface === 'ice' && modifiers.iceSkates ? .36 : rollingDeceleration[tile.surface];
  const roughBonus = tile.surface === 'rough' && modifiers.roughRider ? .52 : 1;
  return base * roughBonus * (modifiers.rollingResistanceMultiplier ?? 1) * (modifiers.slipstream ? .66 : 1);
};

const downhillForceAt = (course: Course, ball: Ball, modifiers: BallPhysicsModifiers) => {
  const tile = tileAt(course, ball.x, ball.y);
  if (!tile || tile.surface === 'void' || tile.surface === 'wall') return undefined;
  const gradient = floorGradientAt(course, ball.x, ball.y);
  const steepness = Math.hypot(gradient.x, gradient.y);
  if (steepness < .0001) return undefined;
  const direction = { x: -gradient.x / steepness, y: -gradient.y / steepness };
  return {
    direction,
    acceleration: steepness * SLOPE_GRAVITY * HEIGHT_TO_WORLD,
    rollingDeceleration: rollingDecelerationFor(tile, modifiers),
  };
};

const canRollDownhill = (course: Course, ball: Ball, modifiers: BallPhysicsModifiers) => {
  if (!isOnGround(course, ball)) return false;
  const downhill = downhillForceAt(course, ball, modifiers);
  if (!downhill || downhill.acceleration <= downhill.rollingDeceleration) return false;
  const downhillSpeed = ball.vx * downhill.direction.x + ball.vy * downhill.direction.y;
  return downhillSpeed < MAX_DOWNHILL_ROLL_SPEED;
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

const portalExitFor = (course: Course, pair: PortalPair, requestedId?: string, portalsArePlenty = false): PortalEndpoint | undefined => {
  if (requestedId) return portalPairs(course).find((candidate) => `${candidate.id}:exit` === requestedId)?.exit;
  if (portalsArePlenty) {
    const exits = portalPairs(course).map((candidate) => candidate.exit).filter((exit): exit is PortalEndpoint => Boolean(exit));
    const sourceIndex = portalPairs(course).findIndex((candidate) => candidate.id === pair.id);
    return exits[(sourceIndex + 1) % exits.length] ?? pair.exit;
  }
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

const teleportThroughPortal = (course: Course, participant: Participant, reality?: RealityCard): boolean => {
  if (participant.portalCooldown > 0) {
    participant.portalCooldown -= 1;
    return false;
  }
  const pair = portalEntranceAt(course, participant.ball.x, participant.ball.y);
  if (!pair?.entrance) return false;
  const exit = portalExitFor(course, pair, participant.modifiers.portalExitId, reality === 'portals are plenty');
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

const beginFall = (participant: Participant, previous: Ball) => {
  participant.reset = true;
  participant.returnBall = { ...previous, vx: 0, vy: 0, vz: 0, falling: undefined, resetCount: previous.resetCount + 1 };
  participant.fallFramesRemaining = FALL_DURATION_FRAMES;
  participant.ball = { ...participant.ball, z: previous.z, vz: -FALL_GRAVITY * STEP, falling: true };
};

const continueFall = (participant: Participant) => {
  const ball = participant.ball;
  ball.x += ball.vx * STEP;
  ball.y += ball.vy * STEP;
  ball.vx *= .985;
  ball.vy *= .985;
  ball.vz -= FALL_GRAVITY * STEP;
  ball.z += ball.vz * STEP;
  participant.fallFramesRemaining -= 1;
  if (participant.fallFramesRemaining === 0) participant.ball = participant.returnBall!;
};

const applySurfaceForces = (course: Course, ball: Ball, tile: Tile, modifiers: BallPhysicsModifiers) => {
  const downhill = downhillForceAt(course, ball, modifiers);
  if (downhill) {
    const downhillSpeed = ball.vx * downhill.direction.x + ball.vy * downhill.direction.y;
    const allowedAcceleration = Math.max(0, (MAX_DOWNHILL_ROLL_SPEED - downhillSpeed) / STEP);
    const acceleration = Math.min(downhill.acceleration, allowedAcceleration);
    ball.vx += downhill.direction.x * acceleration * STEP;
    ball.vy += downhill.direction.y * acceleration * STEP;
  }
  if (tile.surface === 'booster' || tile.surface === 'conveyor') {
    const direction = tile.direction ?? { x: 1, y: 0 };
    const acceleration = (tile.surface === 'booster' ? BOOST_ACCELERATION : CONVEYOR_ACCELERATION) * (modifiers.terrainAccelerationMultiplier ?? 1);
    ball.vx += direction.x * acceleration * STEP;
    ball.vy += direction.y * acceleration * STEP;
  }
  if (modifiers.sidewaysGravity) ball.vx += .7 * STEP;
  if (tile.surface === 'sand' && modifiers.sandWedge) {
    const moving = planarSpeed(ball) || 1;
    ball.vx += ball.vx / moving * .75 * STEP;
    ball.vy += ball.vy / moving * .75 * STEP;
  }
  if (modifiers.magnetBall) {
    const pad = course.itemPads.filter((candidate) => !candidate.collected).map((candidate) => ({ pad: candidate, distance: Math.hypot(candidate.point.x + .5 - ball.x, candidate.point.y + .5 - ball.y) })).filter((candidate) => candidate.distance > .05 && candidate.distance < 3.25).sort((left, right) => left.distance - right.distance)[0];
    if (pad) {
      const pull = 2.1 * (1 - pad.distance / 3.25) * STEP;
      ball.vx += (pad.pad.point.x + .5 - ball.x) / pad.distance * pull;
      ball.vy += (pad.pad.point.y + .5 - ball.y) / pad.distance * pull;
    }
  }
  if (modifiers.cupMagnet) {
    const cup = tileCenter(course.cup);
    const distance = Math.hypot(cup.x - ball.x, cup.y - ball.y);
    if (distance > .1 && distance < 4.2) {
      const pull = 2.8 * (1 - distance / 4.2) * STEP;
      ball.vx += (cup.x - ball.x) / distance * pull;
      ball.vy += (cup.y - ball.y) / distance * pull;
    }
  }
  const currentSpeed = planarSpeed(ball);
  if (currentSpeed > 0) {
    const reduction = Math.min(currentSpeed, rollingDecelerationFor(tile, modifiers) * STEP);
    ball.vx -= ball.vx / currentSpeed * reduction;
    ball.vy -= ball.vy / currentSpeed * reduction;
  }
  limitPlanarSpeed(ball);
};

const applyCourseInteractions = (course: Course, participant: Participant, gadgets: readonly Gadget[], triggeredGadgets: Set<string>) => {
  const ball = participant.ball;
  if (participant.featureCooldown > 0) {
    participant.featureCooldown -= 1;
    return false;
  }
  const feature = (course.features ?? []).find((candidate) => candidate.kind !== 'air-ring' && (candidate.kind === 'sinkhole'
    ? Math.hypot(ball.x - candidate.entrance.x - .5, ball.y - candidate.entrance.y - .5) < .34
    : Math.hypot(ball.x - candidate.point.x - .5, ball.y - candidate.point.y - .5) < (candidate.kind === 'thorn' ? candidate.radius : .4)));
  if (feature?.kind === 'sinkhole') {
    ball.x = feature.exit.x + .5;
    ball.y = feature.exit.y + .5;
    ball.z = floorHeightAt(course, ball.x, ball.y) + BALL_RADIUS;
    participant.featureCooldown = 10;
    return true;
  }
  if (feature?.kind === 'thorn') {
    const dx = participant.modifiers.thornmail ? course.cup.x + .5 - ball.x : ball.x - feature.point.x - .5;
    const dy = participant.modifiers.thornmail ? course.cup.y + .5 - ball.y : ball.y - feature.point.y - .5;
    const distance = Math.hypot(dx, dy) || 1;
    ball.vx += dx / distance * 2.4;
    ball.vy += dy / distance * 2.4;
    participant.featureCooldown = 12;
  }
  if (feature?.kind === 'pulse') {
    ball.vx += feature.direction.x * feature.strength;
    ball.vy += feature.direction.y * feature.strength;
    participant.featureCooldown = 12;
  }
  const gadget = gadgets.find((candidate) => !triggeredGadgets.has(candidate.id) && Math.hypot(ball.x - candidate.point.x - .5, ball.y - candidate.point.y - .5) < .34);
  if (!gadget) return false;
  if (gadget.kind === 'slick patch') {
    ball.vx *= 1.025;
    ball.vy *= 1.025;
    return false;
  }
  triggeredGadgets.add(gadget.id);
  const dx = ball.x - gadget.point.x - .5;
  const dy = ball.y - gadget.point.y - .5;
  const distance = Math.hypot(dx, dy) || 1;
  if (gadget.kind === 'popper pad') {
    ball.vx += dx / distance * 2.7;
    ball.vy += dy / distance * 2.7;
  }
  if (gadget.kind === 'blast mine') {
    ball.vx += dx / distance * 4.8;
    ball.vy += dy / distance * 4.8;
  }
  if (gadget.kind === 'snare patch') {
    ball.vx *= .18;
    ball.vy *= .18;
  }
  if (gadget.kind === 'sky spring') {
    ball.vz = Math.max(ball.vz, 3.8);
    ball.vx *= 1.12;
    ball.vy *= 1.12;
  }
  if (gadget.kind === 'gravity well') {
    ball.vx -= dx / distance * 3.4;
    ball.vy -= dy / distance * 3.4;
  }
  if (gadget.kind === 'mirror plate') {
    ball.vx *= -1;
    ball.vy *= -1;
  }
  if (gadget.kind === 'toll booth') {
    ball.vx *= .76;
    ball.vy *= .76;
  }
  if (gadget.kind === 'control inverter') {
    ball.vx *= .72;
    ball.vy *= .72;
  }
  if (gadget.kind === 'portal gun') {
    const exit = gadgets.find((candidate) => candidate.id !== gadget.id && candidate.kind === 'portal gun' && candidate.ownerId === gadget.ownerId);
    if (exit) {
      ball.x = exit.point.x + .5;
      ball.y = exit.point.y + .5;
      ball.z = floorHeightAt(course, ball.x, ball.y) + BALL_RADIUS;
    }
  }
  participant.featureCooldown = 8;
  limitPlanarSpeed(ball);
  return false;
};

const stepGroundTerrain = (course: Course, participant: Participant, phase: number, phaseCount: number, gadgets: readonly Gadget[], triggeredGadgets: Set<string>, reality?: RealityCard) => {
  const previous = { ...participant.ball };
  const ball = participant.ball;
  ball.x += ball.vx * STEP;
  ball.y += ball.vy * STEP;
  ball.vz = 0;

  if (teleportThroughPortal(course, participant, reality)) return;

  const tile = tileAt(course, ball.x, ball.y);
  if (!tile || tile.surface === 'void') {
    if (participant.modifiers.hazardShield && !participant.shieldUsed) {
      participant.shieldUsed = true;
      const normal = bounceNormal(previous, ball);
      const velocity = reflect(previous, normal, .38);
      participant.ball = { ...previous, ...velocity, z: floorHeightAt(course, previous.x, previous.y) + BALL_RADIUS, vz: 0 };
    } else {
      beginFall(participant, previous);
    }
    return;
  }
  const closedGate = reality !== 'gates are open' && closedGateAt(course, ball.x, ball.y, phase, phaseCount);
  if (tile.surface === 'wall' || closedGate) {
    if (tile.surface === 'wall' && reality === 'wall is cup') {
      participant.ball = { ...previous, vx: 0, vy: 0, vz: 0, complete: true };
      return;
    }
    if (tile.surface === 'wall' && participant.modifiers.anvilBall && !participant.ghostUsed) {
      participant.ghostUsed = true;
      ball.z = floorHeightAt(course, ball.x, ball.y) + BALL_RADIUS;
      applySurfaceForces(course, ball, tile, participant.modifiers);
      return;
    }
    if ((participant.modifiers.ghostBall || (closedGate && participant.modifiers.gatecrasher)) && !participant.ghostUsed) {
      participant.ghostUsed = true;
      if (tile.surface === 'wall') {
        const normal = bounceNormal(previous, ball);
        if (normal.x) ball.x = normal.x < 0 ? Math.floor(ball.x) + 1.001 : Math.floor(ball.x) - .001;
        if (normal.y) ball.y = normal.y < 0 ? Math.floor(ball.y) + 1.001 : Math.floor(ball.y) - .001;
      }
      const passedTile = tileAt(course, ball.x, ball.y);
      if (!passedTile || passedTile.surface === 'void') {
        beginFall(participant, previous);
        return;
      }
      ball.z = floorHeightAt(course, ball.x, ball.y) + BALL_RADIUS;
      applySurfaceForces(course, ball, passedTile, participant.modifiers);
      return;
    }
    const normal = bounceNormal(previous, ball);
    const restitution = Math.min(.98, (participant.modifiers.bouncy ? .94 : participant.modifiers.reboundRig ? .9 : participant.modifiers.bankShot ? .82 : .52) * (participant.modifiers.mirrorBall ? 1.18 : 1) * (participant.modifiers.wallRestitutionMultiplier ?? 1));
    const velocity = reflect(previous, normal, restitution);
    participant.ball = { ...previous, ...velocity, z: floorHeightAt(course, previous.x, previous.y) + BALL_RADIUS, vz: 0 };
    return;
  }

  ball.z = floorHeightAt(course, ball.x, ball.y) + BALL_RADIUS;
  if (applyCourseInteractions(course, participant, gadgets, triggeredGadgets)) return;
  applySurfaceForces(course, ball, tile, participant.modifiers);
};

const bounceAirborneBall = (course: Course, participant: Participant, previous: Ball) => {
  const normal = bounceNormal(previous, participant.ball);
  const velocity = reflect(previous, normal, .38);
  participant.ball = {
    ...previous,
    ...velocity,
    vz: Math.max(.12, Math.abs(previous.vz) * .24),
    z: Math.max(previous.z, floorHeightAt(course, previous.x, previous.y) + BALL_RADIUS),
  };
};

const applyAirborneInteractions = (course: Course, participant: Participant, previous: Ball): boolean => {
  const ball = participant.ball;
  const lowBar = course.hazards.find((hazard) => hazard.kind === 'low-bar'
    && Math.hypot(ball.x - hazard.point.x - .5, ball.y - hazard.point.y - .5) < .42);
  if (lowBar?.kind === 'low-bar') {
    const height = floorHeightAt(course, lowBar.point.x + .5, lowBar.point.y + .5) + lowBar.clearance;
    if (ball.z - BALL_RADIUS < height && ball.z + BALL_RADIUS > height) {
      bounceAirborneBall(course, participant, previous);
      return true;
    }
  }
  for (const hazard of course.hazards) {
    if (hazard.kind !== 'updraft') continue;
    const distance = Math.hypot(ball.x - hazard.point.x - .5, ball.y - hazard.point.y - .5);
    if (distance > hazard.radius) continue;
    const force = hazard.strength * (1 - distance / hazard.radius) * STEP;
    ball.vx += hazard.direction.x * force;
    ball.vy += hazard.direction.y * force;
    ball.vz += force * .52;
    limitPlanarSpeed(ball);
  }
  if (participant.featureCooldown > 0) {
    participant.featureCooldown -= 1;
    return false;
  }
  const ring = (course.features ?? []).find((feature) => feature.kind === 'air-ring'
    && Math.hypot(ball.x - feature.point.x - .5, ball.y - feature.point.y - .5) < feature.radius
    && ball.z - BALL_RADIUS > floorHeightAt(course, feature.point.x + .5, feature.point.y + .5) + .38);
  if (ring?.kind !== 'air-ring') return false;
  ball.vx *= ring.boost;
  ball.vy *= ring.boost;
  ball.vz += .58;
  participant.featureCooldown = 12;
  limitPlanarSpeed(ball);
  return false;
};

const stepAirborne = (course: Course, participant: Participant, phase: number, phaseCount: number, gadgets: readonly Gadget[], triggeredGadgets: Set<string>, reality?: RealityCard) => {
  const previous = { ...participant.ball };
  const ball = participant.ball;
  ball.x += ball.vx * STEP;
  ball.y += ball.vy * STEP;
  ball.vz -= CHIP_GRAVITY * (participant.modifiers.chipGravityMultiplier ?? 1) * STEP;
  ball.z += ball.vz * STEP;

  const tile = tileAt(course, ball.x, ball.y);
  if (!tile || tile.surface === 'void') {
    if (ball.z > BALL_RADIUS + .01) return;
    if (participant.modifiers.hazardShield && !participant.shieldUsed) {
      participant.shieldUsed = true;
      bounceAirborneBall(course, participant, previous);
    } else {
      beginFall(participant, previous);
    }
    return;
  }

  const blocked = tile.surface === 'wall' || (reality !== 'gates are open' && closedGateAt(course, ball.x, ball.y, phase, phaseCount));
  if (blocked) {
    const obstructionHeight = floorHeightAt(course, ball.x, ball.y) + (tile.surface === 'wall' ? WALL_CLEARANCE_HEIGHT : GATE_CLEARANCE_HEIGHT);
    if (ball.z - BALL_RADIUS > obstructionHeight) return;
    bounceAirborneBall(course, participant, previous);
    return;
  }

  if (applyAirborneInteractions(course, participant, previous)) return;

  const landingHeight = floorHeightAt(course, ball.x, ball.y) + BALL_RADIUS;
  if (ball.z > landingHeight || ball.vz > 0) return;
  ball.z = landingHeight;
  ball.vz = 0;
  if (applyCourseInteractions(course, participant, gadgets, triggeredGadgets)) return;
  applySurfaceForces(course, ball, tile, participant.modifiers);
};

const stepTerrain = (course: Course, participant: Participant, phase: number, phaseCount: number, gadgets: readonly Gadget[], triggeredGadgets: Set<string>, reality?: RealityCard) => {
  if (participant.fallFramesRemaining > 0) {
    continueFall(participant);
    return;
  }
  if (participant.ball.vz > .001 || !isOnGround(course, participant.ball)) {
    stepAirborne(course, participant, phase, phaseCount, gadgets, triggeredGadgets, reality);
    return;
  }
  stepGroundTerrain(course, participant, phase, phaseCount, gadgets, triggeredGadgets, reality);
};

const collideWithSweepers = (course: Course, participants: Participant[], phase: number, phaseCount: number) => {
  for (const hazard of course.hazards) {
    if (hazard.kind !== 'sweeper') continue;
    const center = { x: hazard.point.x + .5, y: hazard.point.y + .5 };
    const direction = sweeperDirection(hazard, phase, phaseCount);
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
      const impulse = participant.modifiers.hazardImpulseMultiplier ?? 1;
      participant.ball.vx += (normal.x * 1.55 + direction.x * .45) * impulse;
      participant.ball.vy += (normal.y * 1.55 + direction.y * .45) * impulse;
      limitPlanarSpeed(participant.ball);
    }
  }
};

const collide = (course: Course, participants: Participant[], reality?: RealityCard) => {
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
      if (reality === 'ball is cup') {
        if (first === 0 || second === 0) {
          const active = participants[0]!;
          active.ball = { ...active.ball, vx: 0, vy: 0, vz: 0, complete: true };
        }
        continue;
      }
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

const allSettled = (course: Course, participants: Participant[]) => participants.every((participant) => participant.ball.complete || (isStopped(participant.ball) && isOnGround(course, participant.ball) && !canRollDownhill(course, participant.ball, participant.modifiers)));

const simulateMotion = (course: Course, initial: Ball, maxSeconds: number, options: SimulationOptions): SimulationResult => {
  const participants: Participant[] = [
    { ball: { ...initial }, modifiers: options.modifiers ?? {}, reset: false, shieldUsed: false, ghostUsed: false, portalCooldown: 0, fallFramesRemaining: 0, featureCooldown: 0 },
    ...(options.otherBalls ?? []).map(({ ball, modifiers }) => ({ ball: { ...ball }, modifiers: modifiers ?? {}, reset: false, shieldUsed: false, ghostUsed: false, portalCooldown: 0, fallFramesRemaining: 0, featureCooldown: 0 })),
  ];
  const frames: SimulationFrame[] = [];
  const cups = [tileCenter(course.cup), ...(options.reality === 'cups are many' ? course.itemPads.map((pad) => tileCenter(pad.point)) : [])];
  const phase = options.phase ?? 0;
  const phaseCount = options.phaseCount ?? 8;
  const itemPadIds = new Set<string>();
  const gadgetIds = new Set<string>();
  const settleAtEnd = maxSeconds >= MAX_SETTLE_SECONDS;
  let holed = false;
  let settled = false;

  for (let frame = 0; frame < maxSeconds / STEP; frame += 1) {
    participants.forEach((participant) => {
      if (!participant.ball.complete && (!isStopped(participant.ball) || canRollDownhill(course, participant.ball, participant.modifiers))) stepTerrain(course, participant, phase, phaseCount, options.gadgets ?? [], gadgetIds, options.reality);
    });
    collideWithSweepers(course, participants, phase, phaseCount);
    if (options.collisions && participants.length > 1) collide(course, participants, options.reality);

    const active = participants[0]!;
    const activeTile = tileAt(course, active.ball.x, active.ball.y);
    const captureCup = cups.find((cup) => cupCoverageAt(Math.hypot(active.ball.x - cup.x, active.ball.y - cup.y), active.modifiers.cupRadius ?? .28) >= CUP_CAPTURE_COVERAGE);
    if (!active.ball.complete && activeTile && activeTile.surface !== 'void' && isOnGround(course, active.ball) && captureCup && planarSpeed(active.ball) < 1.9) {
      active.ball = { ...active.ball, x: captureCup.x, y: captureCup.y, z: floorHeightAt(course, captureCup.x, captureCup.y) + BALL_RADIUS, vx: 0, vy: 0, vz: 0, complete: true };
      holed = true;
    }
    if (active.ball.complete) holed = true;
    if (options.collectItems && !itemPadIds.size && !active.ball.complete) {
      const pad = course.itemPads.find((candidate) => !candidate.collected && Math.hypot(active.ball.x - candidate.point.x - .5, active.ball.y - candidate.point.y - .5) < .32);
      if (pad) itemPadIds.add(pad.id);
    }

    participants.forEach((participant) => {
      if (!participant.ball.complete && isStopped(participant.ball) && isOnGround(course, participant.ball) && !canRollDownhill(course, participant.ball, participant.modifiers)) stopParticipant(course, participant);
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
    gadgetIds: [...gadgetIds],
    settled,
  };
};

export const simulateShot = (course: Course, initial: Ball, shot: ShotCommand, maxSeconds = MAX_SETTLE_SECONDS, options: SimulationOptions = {}): SimulationResult => simulateMotion(course, applyShot(initial, shot), maxSeconds, options);

export const simulateImpulse = (course: Course, initial: Ball, velocity: Point, maxSeconds = MAX_SETTLE_SECONDS, modifiers: BallPhysicsModifiers = {}, phase = 0, phaseCount = 8): SimulationResult => simulateMotion(course, { ...initial, vx: velocity.x, vy: velocity.y, vz: 0 }, maxSeconds, { modifiers, phase, phaseCount });

export const distanceToCup = (course: Course, ball: Ball) => {
  const cup = tileCenter(course.cup);
  return Math.hypot(cup.x - ball.x, cup.y - ball.y);
};
