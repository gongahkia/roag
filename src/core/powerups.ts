import { activePlayer, addMessage } from './game-state';
import { BALL_RADIUS, MAX_SETTLE_SECONDS, floorHeightAt, simulateImpulse, tileAt } from './physics';
import { CHAOS_POWER_UPS, GADGET_POWER_UPS, RECOVERY_POWER_UPS, caddyCount, canStorePowerUp, hasCaddy, isBallForm, physicsModifiersFor, pocketsFor, storePowerUp, syncPocketMirrors, takePocketCard } from './player-effects';
import { Random } from './random';
import type { ChronoCard, Course, GadgetKind, GameState, ItemPadKind, Player, Point, PowerUp } from './types';

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
  const owner = state.players.find((player) => player.id === ownerId);
  const limit = owner ? 1 + caddyCount(owner, 'gadgeteer') : 1;
  if ((state.gadgets ?? []).filter((gadget) => gadget.ownerId === ownerId).length >= limit) return false;
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

const routePointAfter = (state: GameState, player: Player) => {
  const nearest = state.course.route.reduce((winner, point, index) => {
    const distance = Math.hypot(player.ball.x - point.x - .5, player.ball.y - point.y - .5);
    return distance < winner.distance ? { index, distance } : winner;
  }, { index: 0, distance: Number.POSITIVE_INFINITY });
  return state.course.route[Math.min(state.course.route.length - 2, nearest.index + 3)]!;
};

const chronoCards: readonly ChronoCard[] = ['undo drive', 'second chance', 'echo putt', 'future sight', 'time theft', 'frozen frame', 'parallel parking', 'grandfather clause'];
const isChrono = (value: PowerUp | ChronoCard): value is ChronoCard => chronoCards.includes(value as ChronoCard);

export const usePowerUp = (state: GameState, powerUp: PowerUp | ChronoCard, targetId?: string, portalExitId?: string, placement?: Point) => {
  const player = activePlayer(state);
  if (!pocketsFor(player).some((card) => card.id === powerUp)) return;
  const target = state.players.find((candidate) => candidate.id === targetId);
  let used = false;
  if (isChrono(powerUp)) {
    const targetPlayer = powerUp === 'grandfather clause' ? target : player;
    const history = targetPlayer?.shotHistory.at(-1);
    if ((powerUp === 'undo drive' || powerUp === 'second chance' || powerUp === 'grandfather clause') && history && targetPlayer) {
      targetPlayer.ball = { ...history.before, strokes: powerUp === 'second chance' ? history.before.strokes : targetPlayer.ball.strokes };
      addMessage(state, `${player.name} invokes ${powerUp}`);
      used = true;
    }
    if (powerUp === 'echo putt' || powerUp === 'time theft') { player.twoPuttsArmed = true; addMessage(state, `${player.name} bends the turn order with ${powerUp}`); used = true; }
    if (powerUp === 'future sight') { player.cupMagnetArmed = true; addMessage(state, `${player.name} reads three possible futures`); used = true; }
    if (powerUp === 'frozen frame') { player.hazardShield = true; player.reboundRigArmed = true; addMessage(state, `${player.name} freezes the frame around their next shot`); used = true; }
    if (powerUp === 'parallel parking') {
      const point = routePointAfter(state, player);
      player.ball = { ...player.ball, x: point.x + .5, y: point.y + .5, z: floorHeightAt(state.course, point.x + .5, point.y + .5) + BALL_RADIUS, vx: 0, vy: 0, vz: 0 };
      addMessage(state, `${player.name} parks in a parallel timeline`);
      used = true;
    }
  }
  if (!isChrono(powerUp) && isGadget(powerUp)) {
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
  if (powerUp === 'rescue drone') {
    const point = routePointAfter(state, player);
    player.ball = { ...player.ball, x: point.x + .5, y: point.y + .5, z: floorHeightAt(state.course, point.x + .5, point.y + .5) + BALL_RADIUS, vx: 0, vy: 0, vz: 0, falling: undefined };
    addMessage(state, `${player.name}'s rescue drone drops them ahead`);
    used = true;
  }
  if (powerUp === 'airhorn' && target && target.id !== player.id) {
    target.forcedChip = true;
    addMessage(state, `${player.name} airhorns ${target.name} into a chip`);
    used = true;
  }
  if (powerUp === 'club flipper' && target && target.id !== player.id) {
    target.controlInverted = 1;
    addMessage(state, `${player.name} flips ${target.name}'s controls`);
    used = true;
  }
  if (powerUp === 'time dilator' && target && target.id !== player.id) {
    target.timeDilated = 1;
    addMessage(state, `${player.name} slows ${target.name}'s next shot`);
    used = true;
  }
  if (powerUp === 'mugger' && target && target.id !== player.id) {
    const stolen = pocketsFor(target).shift();
    if (stolen && canStorePowerUp(player)) { pocketsFor(player).push(stolen); syncPocketMirrors(player); syncPocketMirrors(target); }
    else { target.cash = Math.max(0, target.cash - 3); player.cash += 3; }
    addMessage(state, `${player.name} mugs ${target.name}`);
    used = true;
  }
  if (powerUp === 'scramble') {
    const balls = state.players.filter((candidate) => !candidate.ball.complete).map((candidate) => ({ x: candidate.ball.x, y: candidate.ball.y, z: candidate.ball.z }));
    state.players.filter((candidate) => !candidate.ball.complete).forEach((candidate, index) => {
      const next = balls[(index + 1) % balls.length]!;
      candidate.ball = { ...candidate.ball, ...next, vx: 0, vy: 0, vz: 0 };
    });
    addMessage(state, `${player.name} scrambles the table`);
    used = true;
  }
  if (powerUp === 'gravity gloves') { player.cupMagnetArmed = true; addMessage(state, `${player.name} equips gravity gloves`); used = true; }
  if (powerUp === 'bunker buster') {
    const nearestWall = state.course.tiles.map((tile, index) => ({ tile, x: index % state.course.width, y: Math.floor(index / state.course.width) })).filter((candidate) => candidate.tile.surface === 'wall').sort((left, right) => Math.hypot(left.x + .5 - player.ball.x, left.y + .5 - player.ball.y) - Math.hypot(right.x + .5 - player.ball.x, right.y + .5 - player.ball.y))[0];
    const targetPoint = placement ?? (nearestWall ? { x: nearestWall.x, y: nearestWall.y } : undefined);
    const tile = targetPoint && tileAt(state.course, targetPoint.x + .5, targetPoint.y + .5);
    if (tile?.surface === 'wall') { tile.surface = 'fairway'; addMessage(state, `${player.name} busts a wall`); used = true; }
    const gate = targetPoint && state.course.hazards.find((hazard) => hazard.kind === 'gate' && hazard.point.x === targetPoint.x && hazard.point.y === targetPoint.y);
    if (gate) { state.course.hazards = state.course.hazards.filter((hazard) => hazard !== gate); addMessage(state, `${player.name} opens a gate`); used = true; }
  }
  if (powerUp === 'portal remote' && portalExitExists(state.course, portalExitId)) {
    const exit = state.course.portals!.find((pair) => `${pair.id}:exit` === portalExitId)!.exit!;
    player.ball = { ...player.ball, x: exit.point.x + .5, y: exit.point.y + .5, z: floorHeightAt(state.course, exit.point.x + .5, exit.point.y + .5) + BALL_RADIUS, vx: 0, vy: 0, vz: 0 };
    addMessage(state, `${player.name} remotes into ${portalExitId}`);
    used = true;
  }
  if (powerUp === 'red tee') { player.redTee = { x: Math.floor(player.ball.x), y: Math.floor(player.ball.y) }; addMessage(state, `${player.name} plants a red tee`); used = true; }
  if (powerUp === 'black flag' && target && target.id !== player.id) { state.forcedNextPlayerId = target.id; addMessage(state, `${player.name} waves a black flag: ${target.name} plays next`); used = true; }
  if (powerUp === 'cherry bomb') {
    state.players.filter((candidate) => candidate.id !== player.id && !candidate.ball.complete).forEach((candidate) => {
      const distance = Math.hypot(candidate.ball.x - player.ball.x, candidate.ball.y - player.ball.y) || 1;
      candidate.ball = simulateImpulse(state.course, candidate.ball, { x: (candidate.ball.x - player.ball.x) / distance * 3.8, y: (candidate.ball.y - player.ball.y) / distance * 3.8 }, MAX_SETTLE_SECONDS, physicsModifiersFor(candidate, state.holeRules), state.coursePhase, state.holeRules.hazardPhaseCount).ball;
    });
    addMessage(state, `${player.name} detonates a cherry bomb`);
    used = true;
  }
  if (powerUp === 'copycat' && target) {
    const copied = pocketsFor(target)[0];
    if (copied && canStorePowerUp(player)) { pocketsFor(player).push({ ...copied }); syncPocketMirrors(player); addMessage(state, `${player.name} copies ${target.name}'s ${copied.id}`); used = true; }
  }
  if (!isChrono(powerUp) && isBallForm(powerUp)) {
    if (powerUp === 'portal' && !portalExitExists(state.course, portalExitId)) return;
    player.ballForm = powerUp;
    player.portalExitId = powerUp === 'portal' ? portalExitId : undefined;
    addMessage(state, `${player.name} becomes a ${powerUp} ball`);
    used = true;
  }
  if (!used) return;
  takePocketCard(player, powerUp);
  if (!isChrono(powerUp) && state.holeRules.powerUps && hasCaddy(player, 'chaos magnet') && new Random(`${state.course.seed}:${player.id}:${player.ball.strokes}:${powerUp}`).chance(1 - .35 ** caddyCount(player, 'chaos magnet'))) {
    awardPowerUp(state, player, 'chaos', 'chaos-magnet', `${player.name}'s chaos magnet pulls {powerUp}`);
  }
};
