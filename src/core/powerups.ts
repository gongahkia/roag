import { activePlayer, addMessage } from './game-state';
import { MAX_SETTLE_SECONDS, simulateImpulse } from './physics';
import { CHAOS_POWER_UPS, RECOVERY_POWER_UPS, canStorePowerUp, isBallForm, physicsModifiersFor, storePowerUp, takePowerUp } from './player-effects';
import { Random } from './random';
import type { Course, GameState, ItemPadKind, Player, PowerUp } from './types';

const portalExitExists = (course: Course, id: string | undefined) => Boolean(id && course.portals?.some((pair) => pair.exit && `${pair.id}:exit` === id));

export const powerUpFor = (state: GameState, player: Player, kind: ItemPadKind, source: string): PowerUp => {
  const random = new Random(`${state.course.seed}:${source}:${player.id}:${state.coursePhase}:${player.ball.strokes}`);
  const leaderScore = Math.min(...state.players.map((candidate) => candidate.total + candidate.ball.strokes));
  const deficit = player.total + player.ball.strokes - leaderScore;
  if (deficit >= 2 && random.chance(.7)) return random.pick(RECOVERY_POWER_UPS);
  return random.pick(kind === 'recovery' ? RECOVERY_POWER_UPS : CHAOS_POWER_UPS);
};

export const awardPowerUp = (state: GameState, player: Player, kind: ItemPadKind, source: string, message: string) => {
  if (!state.config.powerUps || !canStorePowerUp(player)) return false;
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

export const usePowerUp = (state: GameState, powerUp: PowerUp, targetId?: string, portalExitId?: string) => {
  const player = activePlayer(state);
  if (player.inventory !== powerUp && player.spareInventory !== powerUp) return;
  const target = state.players.find((candidate) => candidate.id === targetId);
  let used = false;
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
    const result = simulateImpulse(state.course, target.ball, { x: (target.ball.x - player.ball.x) / distance * 4.6, y: (target.ball.y - player.ball.y) / distance * 4.6 }, MAX_SETTLE_SECONDS, physicsModifiersFor(target), state.coursePhase);
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
  if (isBallForm(powerUp)) {
    if (powerUp === 'portal' && !portalExitExists(state.course, portalExitId)) return;
    player.ballForm = powerUp;
    player.portalExitId = powerUp === 'portal' ? portalExitId : undefined;
    addMessage(state, `${player.name} becomes a ${powerUp} ball`);
    used = true;
  }
  if (!used) return;
  takePowerUp(player, powerUp);
  if (state.config.powerUps && player.upgrades.includes('chaos magnet') && new Random(`${state.course.seed}:${player.id}:${player.ball.strokes}:${powerUp}`).chance(.65)) {
    awardPowerUp(state, player, 'chaos', 'chaos-magnet', `${player.name}'s chaos magnet pulls {powerUp}`);
  }
};
