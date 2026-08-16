import { createBuildState, activePlayer, addMessage, beginBuild, cloneCourse } from './game-state';
import { COURSE_PHASES } from './hazards';
import { simulateShot, type SimulationResult } from './physics';
import { adjustedShotFor, canStorePowerUp, physicsModifiersFor, resetPlayerForCourse } from './player-effects';
import { awardPowerUp } from './powerups';
import type { Ball, GameState, ShotCommand } from './types';

const advanceCoursePhase = (state: GameState) => {
  state.coursePhase = (state.coursePhase + 1) % COURSE_PHASES;
};

const resetPlayersForCourse = (state: GameState) => {
  state.players.forEach((player) => resetPlayerForCourse(player, state.course));
};

const simulatePlayerShot = (state: GameState, playerIndex: number, shot: ShotCommand) => {
  const player = state.players[playerIndex]!;
  if (state.status === 'validate') return simulateShot(state.course, player.ball, adjustedShotFor(player, shot), undefined, {
    modifiers: physicsModifiersFor(player),
    phase: state.coursePhase,
  });
  return simulateShot(state.course, player.ball, adjustedShotFor(player, shot), undefined, {
    modifiers: physicsModifiersFor(player),
    otherBalls: state.players.filter((_, index) => index !== playerIndex).map((candidate) => ({ ball: candidate.ball, modifiers: physicsModifiersFor(candidate) })),
    collisions: state.config.collisions,
    phase: state.coursePhase,
    collectItems: state.config.powerUps && canStorePowerUp(player),
  });
};

const applySimulation = (state: GameState, playerIndex: number, result: SimulationResult) => {
  state.players[playerIndex]!.ball = result.ball;
  state.players[playerIndex]!.hazardShield = state.players[playerIndex]!.hazardShield && !result.shieldUsed;
  if (state.status === 'validate') return;
  let otherIndex = 0;
  state.players.forEach((player, index) => {
    if (index === playerIndex) return;
    player.ball = result.otherBalls[otherIndex]!;
    player.hazardShield = player.hazardShield && !result.otherShieldUsed[otherIndex];
    otherIndex += 1;
  });
};

export const previewShot = (state: GameState, shot: ShotCommand): Ball[][] | undefined => {
  if ((state.status !== 'playing' && state.status !== 'validate') || state.turn.shotInFlight) return undefined;
  const playerIndex = state.turn.playerIndex;
  if (state.players[playerIndex]!.frozenTurns) return undefined;
  const result = simulatePlayerShot(state, playerIndex, shot);
  return result.frames.map((frame) => {
    let otherIndex = 0;
    return state.players.map((player, index) => index === playerIndex ? frame.ball : frame.otherBalls[otherIndex++] ?? player.ball);
  });
};

const finishValidation = (state: GameState) => {
  const author = activePlayer(state);
  state.authoredCourses.push({ authorId: author.id, course: cloneCourse(state.course) });
  const nextAuthor = state.authoredCourses.length;
  if (nextAuthor < state.players.length) {
    beginBuild(state, nextAuthor);
    return;
  }
  state.courseIndex = 0;
  state.course = cloneCourse(state.authoredCourses[0]!.course);
  state.hole = 1;
  state.coursePhase = 0;
  resetPlayersForCourse(state);
  state.turn = { playerIndex: 0, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  state.build = undefined;
  state.status = 'playing';
  addMessage(state, 'all courses validated — competitive play begins');
};

const finishHole = (state: GameState) => {
  for (const player of state.players) {
    const strokes = player.ball.complete ? player.ball.strokes : state.config.strokeCap;
    player.total += strokes;
  }
  if (state.courseIndex + 1 >= state.authoredCourses.length) {
    state.status = 'finished';
    addMessage(state, 'all authored courses scored');
    return;
  }
  state.courseIndex += 1;
  state.hole = state.courseIndex + 1;
  state.course = cloneCourse(state.authoredCourses[state.courseIndex]!.course);
  state.coursePhase = 0;
  resetPlayersForCourse(state);
  state.turn = { playerIndex: 0, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  addMessage(state, `playing ${state.players.find((player) => player.id === state.authoredCourses[state.courseIndex]!.authorId)?.name ?? 'player'}'s course`);
};

const advanceTurn = (state: GameState) => {
  for (let offset = 1; offset <= state.players.length; offset += 1) {
    const index = (state.turn.playerIndex + offset) % state.players.length;
    const player = state.players[index]!;
    if (!player.ball.complete && player.ball.strokes < state.config.strokeCap) {
      state.turn = { playerIndex: index, secondsLeft: state.config.timerSeconds, shotInFlight: false };
      return;
    }
  }
  finishHole(state);
};

export const resolveShot = (state: GameState, shot: ShotCommand) => {
  if ((state.status !== 'playing' && state.status !== 'validate') || state.turn.shotInFlight) return;
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
  const result = simulatePlayerShot(state, playerIndex, shot);
  player.turboArmed = false;
  applySimulation(state, playerIndex, result);
  const consumedForm = player.ballForm;
  player.ballForm = undefined;
  player.portalExitId = undefined;
  if (result.holed) addMessage(state, `${player.name} sinks it in ${player.ball.strokes}`);
  else if (result.reset) addMessage(state, `${player.name} finds the edge`);
  else addMessage(state, `${player.name} rolls to safety`);
  if (state.status === 'validate') {
    advanceCoursePhase(state);
    if (result.holed) finishValidation(state);
    else if (player.ball.strokes >= state.config.strokeCap) {
      state.status = 'build';
      state.build = { ...(state.build ?? createBuildState(playerIndex)), authorIndex: playerIndex };
      addMessage(state, `${player.name} needs to revise this course before it can be played`);
    } else state.turn = { playerIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
    return;
  }
  const pad = result.itemPadIds.map((id) => state.course.itemPads.find((candidate) => candidate.id === id)).find(Boolean);
  if (pad && awardPowerUp(state, player, pad.kind, pad.id, `${player.name} taps a ${pad.kind} pad — {powerUp}`)) pad.collected = true;
  else if (!pad && player.upgrades.includes('extra charge')) awardPowerUp(state, player, 'recovery', 'extra-charge', `${player.name}'s extra charge pulls {powerUp}`);
  if (player.twoPuttsArmed && !player.ball.complete && player.ball.strokes < state.config.strokeCap) {
    player.twoPuttsArmed = undefined;
    addMessage(state, `${player.name} takes the second putt${consumedForm ? ` after ${consumedForm} ball` : ''}`);
    state.turn = { playerIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
    return;
  }
  advanceCoursePhase(state);
  advanceTurn(state);
};

export const beginCourse = (state: GameState): GameState => ({ ...state, status: 'playing', messages: ['tee off — aim with the board, then shoot', ...state.messages] });

export const tickTurn = (current: GameState, elapsedSeconds: number): GameState => {
  if ((current.status !== 'playing' && current.status !== 'validate') || current.turn.shotInFlight) return current;
  const state = { ...current, turn: { ...current.turn }, players: current.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades] })), messages: [...current.messages] };
  state.turn.secondsLeft = Math.max(0, state.turn.secondsLeft - elapsedSeconds);
  if (state.turn.secondsLeft === 0) {
    const player = activePlayer(state);
    player.twoPuttsArmed = undefined;
    addMessage(state, `${player.name} timed out`);
    advanceCoursePhase(state);
    if (state.status === 'validate') {
      state.turn = { playerIndex: state.turn.playerIndex, secondsLeft: state.config.timerSeconds, shotInFlight: false };
      return state;
    }
    advanceTurn(state);
  }
  return state;
};
