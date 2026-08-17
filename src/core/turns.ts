import { activePlayer, addMessage } from './game-state';
import { distanceToCup, simulateShot, type BallPhysicsModifiers, type SimulationResult } from './physics';
import { adjustedShotFor, caddyCount, canStorePowerUp, hasCaddy, physicsModifiersFor } from './player-effects';
import { awardPowerUp } from './powerups';
import { awardHoleCash, openShop, tickShop } from './shop';
import type { Ball, GameState, ShotCommand } from './types';

const advanceCoursePhase = (state: GameState) => {
  state.coursePhase = (state.coursePhase + 1) % state.holeRules.hazardPhaseCount;
};

const simulatePlayerShot = (state: GameState, playerIndex: number, shot: ShotCommand) => {
  const player = state.players[playerIndex]!;
  const effectiveShot = player.forcedChip ? { ...shot, kind: 'chip' as const } : shot;
  const modifiersFor = (candidate: typeof player): BallPhysicsModifiers => ({
    ...physicsModifiersFor(candidate, state.holeRules),
    ghostBall: candidate.ballForm === 'ghost' || state.activeReality === 'everybody is ghost',
    sidewaysGravity: state.activeReality === 'gravity is sideways',
  });
  const simulate = (candidateShot: ShotCommand) => simulateShot(state.course, player.ball, adjustedShotFor(player, candidateShot, state.holeRules), undefined, {
    modifiers: modifiersFor(player),
    otherBalls: state.players.filter((_, index) => index !== playerIndex).map((candidate) => ({ ball: candidate.ball, modifiers: modifiersFor(candidate) })),
    collisions: state.holeRules.collisions,
    phase: state.coursePhase,
    phaseCount: state.holeRules.hazardPhaseCount,
    collectItems: (state.holeRules.powerUps && canStorePowerUp(player)) || state.course.itemPads.some((pad) => pad.kind === 'cash' && !pad.collected),
    gadgets: state.gadgets ?? [],
    reality: state.activeReality,
  });
  if (player.ballForm !== 'quantum' && state.activeReality !== 'two is one') return simulate(effectiveShot);
  const branch = Math.PI / 24;
  const left = simulate({ ...effectiveShot, angle: effectiveShot.angle - branch });
  const right = simulate({ ...effectiveShot, angle: effectiveShot.angle + branch });
  if (left.holed !== right.holed) return left.holed ? left : right;
  return distanceToCup(state.course, left.ball) <= distanceToCup(state.course, right.ball) ? left : right;
};

const applySimulation = (state: GameState, playerIndex: number, result: SimulationResult) => {
  state.players[playerIndex]!.ball = result.ball;
  state.players[playerIndex]!.hazardShield = state.players[playerIndex]!.hazardShield && !result.shieldUsed;
  let otherIndex = 0;
  state.players.forEach((player, index) => {
    if (index === playerIndex) return;
    player.ball = result.otherBalls[otherIndex]!;
    player.hazardShield = player.hazardShield && !result.otherShieldUsed[otherIndex];
    otherIndex += 1;
  });
  if (result.gadgetIds.length) {
    const triggered = (state.gadgets ?? []).filter((gadget) => result.gadgetIds.includes(gadget.id));
    triggered.forEach((gadget) => {
      if (gadget.kind === 'toll booth') {
        const owner = state.players.find((player) => player.id === gadget.ownerId);
        if (owner) { owner.cash += 2; addMessage(state, `${owner.name}'s toll booth collects $2`); }
      }
      if (gadget.kind === 'control inverter') {
        const struck = state.players[playerIndex]!;
        struck.controlInverted = 1;
        addMessage(state, `${struck.name} crosses a control inverter`);
      }
    });
    state.gadgets = (state.gadgets ?? []).filter((gadget) => !result.gadgetIds.includes(gadget.id));
  }
};

export const previewShot = (state: GameState, shot: ShotCommand): Ball[][] | undefined => {
  if (state.status !== 'playing' || state.paused || state.turn.shotInFlight) return undefined;
  const playerIndex = state.turn.playerIndex;
  if (state.players[playerIndex]!.frozenTurns) return undefined;
  const result = simulatePlayerShot(state, playerIndex, shot);
  return result.frames.map((frame) => {
    let otherIndex = 0;
    return state.players.map((player, index) => index === playerIndex ? frame.ball : frame.otherBalls[otherIndex++] ?? player.ball);
  });
};

const finishHole = (state: GameState) => {
  for (const player of state.players) {
    const strokes = player.ball.complete ? player.ball.strokes : state.holeRules.strokeCap;
    player.total += Math.max(1, Math.round(strokes * state.holeRules.scoreMultiplier));
  }
  if (state.hole >= state.config.holeCount) {
    state.status = 'finished';
    addMessage(state, 'nine holes scored — campaign complete');
    return;
  }
  awardHoleCash(state);
  openShop(state);
};

const advanceTurn = (state: GameState) => {
  const direction = state.activeReality === 'turns are backwards' ? -1 : 1;
  if (state.forcedNextPlayerId) {
    const index = state.players.findIndex((player) => player.id === state.forcedNextPlayerId && !player.ball.complete && player.ball.strokes < state.holeRules.strokeCap);
    state.forcedNextPlayerId = undefined;
    if (index >= 0) {
      state.turn = { playerIndex: index, secondsLeft: state.holeRules.timerSeconds, shotInFlight: false };
      return;
    }
  }
  for (let offset = 1; offset <= state.players.length; offset += 1) {
    const index = (state.turn.playerIndex + direction * offset + state.players.length * 2) % state.players.length;
    const player = state.players[index]!;
    if (!player.ball.complete && player.ball.strokes < state.holeRules.strokeCap) {
      state.turn = { playerIndex: index, secondsLeft: state.holeRules.timerSeconds, shotInFlight: false };
      return;
    }
  }
  finishHole(state);
};

export const resolveShot = (state: GameState, shot: ShotCommand) => {
  if (state.status !== 'playing' || state.paused || state.turn.shotInFlight) return;
  const player = activePlayer(state);
  if (player.frozenTurns) {
    player.frozenTurns -= 1;
    player.twoPuttsArmed = undefined;
    addMessage(state, `${player.name} is frozen solid`);
    advanceCoursePhase(state);
    advanceTurn(state);
    return;
  }
  const playerIndex = state.turn.playerIndex;
  const forcedChip = player.forcedChip;
  const affectedControls = player.controlInverted || player.timeDilated;
  const before = { ...player.ball };
  const result = simulatePlayerShot(state, playerIndex, shot);
  player.turboArmed = false;
  player.cupMagnetArmed = false;
  player.slipstreamArmed = false;
  player.reboundRigArmed = false;
  player.sandbagged = false;
  player.forcedChip = undefined;
  player.controlInverted = undefined;
  player.timeDilated = undefined;
  applySimulation(state, playerIndex, result);
  if (result.reset && player.redTee) {
    const x = player.redTee.x + .5;
    const y = player.redTee.y + .5;
    player.ball = { ...player.ball, x, y, z: 0, vx: 0, vy: 0, vz: 0, falling: undefined };
    addMessage(state, `${player.name}'s red tee rewinds their landing`);
  }
  player.shotHistory = [...player.shotHistory, { hole: state.hole, before, after: { ...player.ball } }].slice(-12);
  const consumedForm = player.ballForm;
  player.ballForm = undefined;
  player.portalExitId = undefined;
  if (forcedChip) addMessage(state, `${player.name}'s airhorn forces a chip`);
  if (affectedControls) addMessage(state, `${player.name} shakes off the temporal control glitch`);
  if (result.holed) addMessage(state, `${player.name} sinks it in ${player.ball.strokes}`);
  else if (result.reset) addMessage(state, `${player.name} falls into the void`);
  else addMessage(state, `${player.name} rolls to safety`);
  const pad = result.itemPadIds.map((id) => state.course.itemPads.find((candidate) => candidate.id === id)).find(Boolean);
  if (pad?.kind === 'cash') {
    const leaderScore = Math.min(...state.players.map((candidate) => candidate.total + candidate.ball.strokes));
    const behind = player.total + player.ball.strokes - leaderScore >= 2;
    const value = (behind ? 5 : 2) * (1 + caddyCount(player, 'coin slot'));
    player.cash += value;
    pad.collected = true;
    addMessage(state, `${player.name} collects $${value} from a cash pad`);
  } else if (pad && awardPowerUp(state, player, pad.kind, pad.id, `${player.name} taps a ${pad.kind} pad — {powerUp}`)) pad.collected = true;
  else if (!pad && hasCaddy(player, 'extra charge')) awardPowerUp(state, player, 'recovery', 'extra-charge', `${player.name}'s extra charge pulls {powerUp}`);
  if (state.activeReality === 'cup walks' && !result.holed) {
    const index = state.course.route.findIndex((point) => point.x === state.course.cup.x && point.y === state.course.cup.y);
    const next = state.course.route[Math.min(state.course.route.length - 1, Math.max(1, index + 1))];
    if (next) state.course.cup = { ...next };
  }
  if (result.holed && hasCaddy(player, 'headwind')) {
    state.players.filter((candidate) => candidate.id !== player.id && !candidate.ball.complete).forEach((candidate) => { candidate.timeDilated = Math.max(candidate.timeDilated ?? 0, caddyCount(player, 'headwind')); });
    addMessage(state, `${player.name}'s headwind slows the remaining field`);
  }
  if (player.twoPuttsArmed && !player.ball.complete && player.ball.strokes < state.holeRules.strokeCap) {
    player.twoPuttsArmed = undefined;
    addMessage(state, `${player.name} takes the second putt${consumedForm ? ` after ${consumedForm} ball` : ''}`);
    state.turn = { playerIndex, secondsLeft: state.holeRules.timerSeconds, shotInFlight: false };
    return;
  }
  advanceCoursePhase(state);
  advanceTurn(state);
};

export const tickTurn = (current: GameState, elapsedSeconds: number): GameState => {
  if ((current.status !== 'playing' && current.status !== 'shopping') || current.paused || current.turn.shotInFlight) return current;
  const state = { ...current, turn: { ...current.turn }, players: current.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades], caddies: player.caddies.map((caddy) => ({ ...caddy })), pockets: player.pockets.map((pocket) => ({ ...pocket })), shotHistory: player.shotHistory.map((entry) => ({ ...entry, before: { ...entry.before }, after: { ...entry.after } })) })), messages: [...current.messages] } as GameState;
  if (state.status === 'shopping') {
    tickShop(state, elapsedSeconds);
    return state;
  }
  state.turn.secondsLeft = Math.max(0, state.turn.secondsLeft - elapsedSeconds);
  if (state.turn.secondsLeft === 0) {
    const player = activePlayer(state);
    player.twoPuttsArmed = undefined;
    addMessage(state, `${player.name} timed out`);
    advanceCoursePhase(state);
    advanceTurn(state);
  }
  return state;
};
