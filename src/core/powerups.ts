import { activePlayer, addMessage } from './game-state';
import { MAX_SETTLE_SECONDS, simulateImpulse, tileAt } from './physics';
import { CHAOS_POWER_UPS, GADGET_POWER_UPS, RECOVERY_POWER_UPS, canStorePowerUp, isBallForm, physicsModifiersFor, storePowerUp, takePowerUp } from './player-effects';
import { Random } from './random';
import type { Course, GadgetKind, GameState, ItemPadKind, Player, Point, PowerUp } from './types';

const portalExitExists = (course: Course, id: string | undefined) => Boolean(id && course.portals?.some((pair) => pair.exit && `${pair.id}:exit` === id));

export const powerUpFor = (state: GameState, player: Player, kind: ItemPadKind, source: string): PowerUp => {
  const random = new Random(`${state.course.seed}:${source}:${player.id}:${state.coursePhase}:${player.ball.strokes}`);
  const leaderScore = Math.min(...state.players.map((candidate) => candidate.total + candidate.ball.strokes));
  const deficit = player.total + player.ball.strokes - leaderScore;
  if (deficit >= 2 && random.chance(Math.max(.2, state.holeRules.recoveryBias))) return random.pick(RECOVERY_POWER_UPS);
  if (kind === 'recovery' || random.chance(state.holeRules.recoveryBias)) return random.pick(RECOVERY_POWER_UPS);
  return random.pick(CHAOS_POWER_UPS);
};

export const awardPowerUp = (state: GameState, player: Player, kind: ItemPadKind, source: string, message: string) => {
  if (!state.holeRules.powerUps || !canStorePowerUp(player)) return false;
  const powerUp = powerUpFor(state, player, kind, source);
  storePowerUp(player, powerUp);
  addMessage(state, message.replace('{powerUp}', powerUp));
  return true;
};

export const armSecondWind = (state: GameState) => {
  const player = activePlayer(state);
  if (!player.secondWindAvailable || player.twoPuttsArmed) return;
  player.secondWindAvailable = false;
  player.twoPuttsArmed = true;
  addMessage(state, `${player.name} calls on second wind — two putts armed`);
};

const isGadget = (powerUp: PowerUp): powerUp is GadgetKind => GADGET_POWER_UPS.includes(powerUp);

export const canPlaceGadget = (state: GameState, ownerId: string, point: Point | undefined) => {
  if (!point || !Number.isInteger(point.x) || !Number.isInteger(point.y)) return false;
  if (state.gadgets?.some((gadget) => gadget.ownerId === ownerId)) return false;
  const tile = tileAt(state.course, point.x + .5, point.y + .5);
  if (!tile || !['fairway', 'rough', 'sand', 'ice', 'booster', 'conveyor'].includes(tile.surface)) return false;
  const occupied = (candidate: Point) => candidate.x === point.x && candidate.y === point.y;
  return !state.course.hazards.some((hazard) => occupied(hazard.point))
    && !(state.course.features ?? []).some((feature) => feature.kind === 'sinkhole' ? occupied(feature.entrance) || occupied(feature.exit) : occupied(feature.point))
    && !(state.course.portals ?? []).some((pair) => (pair.entrance && occupied(pair.entrance.point)) || (pair.exit && occupied(pair.exit.point)))
    && !state.course.itemPads.some((pad) => occupied(pad.point))
    && !(state.gadgets ?? []).some((gadget) => occupied(gadget.point));
};

const routePointBefore = (state: GameState, player: Player) => {
  const nearest = state.course.route.reduce((winner, point, index) => {
    const distance = Math.hypot(player.ball.x - point.x - .5, player.ball.y - point.y - .5);
    return distance < winner.distance ? { index, distance } : winner;
  }, { index: 0, distance: Number.POSITIVE_INFINITY });
  return state.course.route[Math.max(0, nearest.index - 4)]!;
};

export const usePowerUp = (state: GameState, powerUp: PowerUp, targetId?: string, portalExitId?: string, placement?: Point) => {
  const player = activePlayer(state);
  if (player.inventory !== powerUp && player.spareInventory !== powerUp) return;
  const target = state.players.find((candidate) => candidate.id === targetId);
  let used = false;
  if (isGadget(powerUp)) {
    if (!canPlaceGadget(state, player.id, placement)) return;
    state.gadgets = [...(state.gadgets ?? []), { id: `gadget-${state.hole}-${state.coursePhase}-${player.id}-${powerUp.replaceAll(' ', '-')}`, ownerId: player.id, kind: powerUp, point: { ...placement! } }];
    addMessage(state, `${player.name} places a ${powerUp}`);
    used = true;
  }
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
    const result = simulateImpulse(state.course, target.ball, { x: (target.ball.x - player.ball.x) / distance * 4.6, y: (target.ball.y - player.ball.y) / distance * 4.6 }, MAX_SETTLE_SECONDS, physicsModifiersFor(target, state.holeRules), state.coursePhase, state.holeRules.hazardPhaseCount);
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
  if (powerUp === 'two putts' && !player.twoPuttsArmed) {
    player.twoPuttsArmed = true;
    addMessage(state, `${player.name} arms two putts`);
    used = true;
  }
  if (powerUp === 'cup magnet') {
    player.cupMagnetArmed = true;
    addMessage(state, `${player.name} arms cup magnet`);
    used = true;
  }
  if (powerUp === 'slipstream') {
    player.slipstreamArmed = true;
    addMessage(state, `${player.name} catches a slipstream`);
    used = true;
  }
  if (powerUp === 'rebound rig') {
    player.reboundRigArmed = true;
    addMessage(state, `${player.name} arms rebound rig`);
    used = true;
  }
  if (powerUp === 'phase shift' && target && target.id !== player.id) {
    const point = routePointBefore(state, target);
    target.ball = { ...target.ball, x: point.x + .5, y: point.y + .5, z: 0, vx: 0, vy: 0, vz: 0 };
    addMessage(state, `${player.name} phase shifts ${target.name} back`);
    used = true;
  }
  if (powerUp === 'sandbag' && target && target.id !== player.id) {
    target.sandbagged = true;
    addMessage(state, `${player.name} sandbags ${target.name}'s next shot`);
    used = true;
  }
  if (isBallForm(powerUp)) {
    if (powerUp === 'portal' && !portalExitExists(state.course, portalExitId)) return;
    player.ballForm = powerUp;
    player.portalExitId = powerUp === 'portal' ? portalExitId : undefined;
    addMessage(state, `${player.name} becomes a ${powerUp} ball`);
    used = true;
  }
  if (!used) return;
  takePowerUp(player, powerUp);
  if (state.holeRules.powerUps && player.upgrades.includes('chaos magnet') && new Random(`${state.course.seed}:${player.id}:${player.ball.strokes}:${powerUp}`).chance(.65)) {
    awardPowerUp(state, player, 'chaos', 'chaos-magnet', `${player.name}'s chaos magnet pulls {powerUp}`);
  }
};
