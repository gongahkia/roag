import { activePlayer, addMessage, beginCourseTransition } from './game-state';
import { simulateShot, type SimulationResult } from './physics';
import { adjustedShotFor, canStorePowerUp, physicsModifiersFor } from './player-effects';
import { awardPowerUp } from './powerups';
import type { Ball, GameState, ShotCommand } from './types';

const advanceCoursePhase = (state: GameState) => {
  state.coursePhase = (state.coursePhase + 1) % state.holeRules.hazardPhaseCount;
};

const simulatePlayerShot = (state: GameState, playerIndex: number, shot: ShotCommand) => {
  const player = state.players[playerIndex]!;
  const effectiveShot = player.forcedChip ? { ...shot, kind: 'chip' as const } : shot;
  return simulateShot(state.course, player.ball, adjustedShotFor(player, effectiveShot, state.holeRules), undefined, {
    modifiers: physicsModifiersFor(player, state.holeRules),
    otherBalls: state.players.filter((_, index) => index !== playerIndex).map((candidate) => ({ ball: candidate.ball, modifiers: physicsModifiersFor(candidate, state.holeRules) })),
    collisions: state.holeRules.collisions,
    phase: state.coursePhase,
    phaseCount: state.holeRules.hazardPhaseCount,
    collectItems: state.holeRules.powerUps && canStorePowerUp(player),
    gadgets: state.gadgets ?? [],
  });
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
  if (result.gadgetIds.length) state.gadgets = (state.gadgets ?? []).filter((gadget) => !result.gadgetIds.includes(gadget.id));
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
  beginCourseTransition(state);
};

const advanceTurn = (state: GameState) => {
  for (let offset = 1; offset <= state.players.length; offset += 1) {
    const index = (state.turn.playerIndex + offset) % state.players.length;
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
  const result = simulatePlayerShot(state, playerIndex, shot);
  player.turboArmed = false;
  player.cupMagnetArmed = false;
  player.slipstreamArmed = false;
  player.reboundRigArmed = false;
  player.sandbagged = false;
  player.forcedChip = undefined;
  applySimulation(state, playerIndex, result);
  const consumedForm = player.ballForm;
  player.ballForm = undefined;
  player.portalExitId = undefined;
  if (forcedChip) addMessage(state, `${player.name}'s airhorn forces a chip`);
  if (result.holed) addMessage(state, `${player.name} sinks it in ${player.ball.strokes}`);
  else if (result.reset) addMessage(state, `${player.name} falls into the void`);
  else addMessage(state, `${player.name} rolls to safety`);
  const pad = result.itemPadIds.map((id) => state.course.itemPads.find((candidate) => candidate.id === id)).find(Boolean);
  if (pad && awardPowerUp(state, player, pad.kind, pad.id, `${player.name} taps a ${pad.kind} pad — {powerUp}`)) pad.collected = true;
  else if (!pad && player.upgrades.includes('extra charge')) awardPowerUp(state, player, 'recovery', 'extra-charge', `${player.name}'s extra charge pulls {powerUp}`);
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
  if (current.status !== 'playing' || current.paused || current.turn.shotInFlight) return current;
  const state = { ...current, turn: { ...current.turn }, players: current.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades] })), messages: [...current.messages] };
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
