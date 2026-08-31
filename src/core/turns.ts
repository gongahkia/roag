import { activePlayer, addMessage, cloneGameState, recordInstrumentation, tickDie } from './game-state';
import { distanceToCup, simulateShot, type BallPhysicsModifiers, type SimulationResult } from './physics';
import { adjustedShotFor, caddyCount, canStorePowerUp, hasCaddy, physicsModifiersFor } from './player-effects';
import { awardPowerUp } from './powerups';
import { rulesetFor } from './rulesets';
import { awardHoleCash, openShop, tickShop } from './shop';
import { consumePuttAttachments, hasAttachment, tickRoundAttachments } from './strategy';
import type { Ball, GameState, ShotCommand } from './types';

/** Preserves seeded legacy item and gadget identity; moving hazards use hazardElapsedMs instead. */
const advanceLegacyCoursePhase = (state: GameState) => {
  state.coursePhase = (state.coursePhase + 1) % state.holeRules.hazardPhaseCount;
};

const simulatePlayerShot = (state: GameState, playerIndex: number, shot: ShotCommand) => {
  const player = state.players[playerIndex]!;
  const ruleset = rulesetFor(state.config);
  const handLimit = ruleset.id === 'party' ? ruleset.cards.handLimit : undefined;
  const effectiveShot = player.forcedChip || hasAttachment(player, 'forced chip') ? { ...shot, kind: 'chip' as const } : shot;
  const modifiersFor = (candidate: typeof player): BallPhysicsModifiers => {
    const base = physicsModifiersFor(candidate, state.holeRules);
    return {
    ...base,
    ghostBall: candidate.ballForm === 'ghost' || state.activeReality === 'everybody is ghost',
    sidewaysGravity: state.activeReality === 'gravity is sideways',
    bumperRestitutionMultiplier: (base.bumperRestitutionMultiplier ?? 1) * (state.activeReality === 'bank holiday' ? 1.26 : 1),
    gustMultiplier: (base.gustMultiplier ?? 1) * (state.activeReality === 'high winds' ? 1.6 : 1),
  };
  };
  const simulate = (candidateShot: ShotCommand) => simulateShot(state.course, player.ball, adjustedShotFor(player, candidateShot, state.holeRules), undefined, {
    modifiers: modifiersFor(player),
    otherBalls: state.players.filter((_, index) => index !== playerIndex).map((candidate) => ({ ball: candidate.ball, modifiers: modifiersFor(candidate) })),
    collisions: state.holeRules.collisions,
    hazardElapsedMs: state.hazardElapsedMs,
    phaseCount: state.holeRules.hazardPhaseCount,
    collectItems: (state.holeRules.powerUps && canStorePowerUp(player, handLimit)) || state.course.itemPads.some((pad) => pad.kind === 'cash' && !pad.collected),
    gadgets: state.gadgets ?? [],
    reality: state.activeReality,
    frozenHazardId: player.frozenObstacleId,
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
    recordInstrumentation(state, { type: 'hole-complete', hole: state.hole, playerId: player.id, detail: 'strokes', value: strokes });
  }
  recordInstrumentation(state, { type: 'hole-duration', hole: state.hole, detail: 'active seconds', value: state.hazardElapsedMs / 1_000 });
  if (state.hole >= state.config.holeCount) {
    state.status = 'finished';
    addMessage(state, 'nine holes scored — campaign complete');
    return;
  }
  awardHoleCash(state);
  openShop(state);
};

const advanceTurn = (state: GameState) => {
  const beginTurn = (playerIndex: number) => {
    const player = state.players[playerIndex]!;
    tickRoundAttachments(state, player.id);
    state.turn = { playerIndex, secondsLeft: state.holeRules.timerSeconds, shotInFlight: false, cardPlayed: false };
  };
  const direction = state.activeReality === 'turns are backwards' ? -1 : 1;
  if (state.forcedNextPlayerId) {
    const index = state.players.findIndex((player) => player.id === state.forcedNextPlayerId && !player.ball.complete && player.ball.strokes < state.holeRules.strokeCap);
    state.forcedNextPlayerId = undefined;
    if (index >= 0) {
      beginTurn(index);
      return;
    }
  }
  for (let offset = 1; offset <= state.players.length; offset += 1) {
    const index = (state.turn.playerIndex + direction * offset + state.players.length * 2) % state.players.length;
    const player = state.players[index]!;
    if (!player.ball.complete && player.ball.strokes < state.holeRules.strokeCap) {
      beginTurn(index);
      return;
    }
  }
  finishHole(state);
};

export const resolveShot = (state: GameState, shot: ShotCommand) => {
  if (state.status !== 'playing' || state.paused || state.turn.shotInFlight) return;
  const player = activePlayer(state);
  const turnSeconds = Math.max(0, state.holeRules.timerSeconds - state.turn.secondsLeft);
  if (player.frozenTurns) {
    player.frozenTurns -= 1;
    player.twoPuttsArmed = undefined;
    recordInstrumentation(state, { type: 'turn-duration', hole: state.hole, playerId: player.id, detail: 'frozen', value: turnSeconds });
    addMessage(state, `${player.name} is frozen solid`);
    advanceLegacyCoursePhase(state);
    advanceTurn(state);
    return;
  }
  const playerIndex = state.turn.playerIndex;
  const forcedChip = player.forcedChip || hasAttachment(player, 'forced chip');
  const affectedControls = player.controlInverted || player.timeDilated || hasAttachment(player, 'club flip') || hasAttachment(player, 'slow clock') || hasAttachment(player, 'frayed grip');
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
  player.gustReversed = undefined;
  player.slopeStabilized = undefined;
  player.springPolished = undefined;
  player.bumperWaxed = undefined;
  player.cushionMapped = undefined;
  player.frozenObstacleId = undefined;
  applySimulation(state, playerIndex, result);
  const routeRole = state.course.routeRoles?.reduce<{ role: string; distance: number } | undefined>((closest, assignment) => {
    const distance = Math.hypot(before.x - (assignment.marker.x + .5), before.y - (assignment.marker.y + .5));
    return !closest || distance < closest.distance ? { role: assignment.role, distance } : closest;
  }, undefined)?.role ?? 'unknown';
  recordInstrumentation(state, { type: 'turn-duration', hole: state.hole, playerId: player.id, detail: 'shot', value: turnSeconds });
  recordInstrumentation(state, { type: 'shot', hole: state.hole, playerId: player.id, detail: `${shot.kind ?? 'putt'}:${routeRole}`, value: shot.power });
  result.collidedOtherIndexes.forEach((otherIndex) => {
    const target = state.players.filter((_, index) => index !== playerIndex)[otherIndex];
    if (!target) return;
    recordInstrumentation(state, { type: 'collision', hole: state.hole, playerId: player.id, detail: target.id });
    addMessage(state, `${player.name} banks into ${target.name}`);
  });
  if (hasAttachment(player, 'mulligan relay')) player.twoPuttsArmed = true;
  consumePuttAttachments(player);
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
  else if (result.reset) {
    recordInstrumentation(state, { type: 'recovery', hole: state.hole, playerId: player.id, detail: 'out-of-bounds' });
    addMessage(state, `${player.name} falls into the void — automatic recovery returns the ball`);
  }
  else addMessage(state, `${player.name} rolls to safety`);
  if (result.holed && player.holeFinishOrder === undefined) player.holeFinishOrder = state.holeFinishSequence++;
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
    state.turn = { playerIndex, secondsLeft: state.holeRules.timerSeconds, shotInFlight: false, cardPlayed: state.turn.cardPlayed };
    return;
  }
  advanceLegacyCoursePhase(state);
  advanceTurn(state);
};

export const tickTurn = (current: GameState, elapsedSeconds: number): GameState => {
  if ((current.status !== 'playing' && current.status !== 'shopping' && current.status !== 'rolling') || current.paused) return current;
  const state = cloneGameState(current);
  const elapsed = Math.max(0, Number.isFinite(elapsedSeconds) ? elapsedSeconds : 0);
  if (state.status === 'rolling') {
    tickDie(state, elapsed);
    return state;
  }
  if (state.status === 'shopping') {
    tickShop(state, elapsed);
    return state;
  }
  state.hazardElapsedMs += elapsed * 1_000;
  if (state.turn.shotInFlight) return state;
  state.turn.secondsLeft = Math.max(0, state.turn.secondsLeft - elapsed);
  if (state.turn.secondsLeft === 0) {
    const player = activePlayer(state);
    player.twoPuttsArmed = undefined;
    recordInstrumentation(state, { type: 'turn-duration', hole: state.hole, playerId: player.id, detail: 'timeout', value: state.holeRules.timerSeconds });
    addMessage(state, `${player.name} timed out`);
    advanceLegacyCoursePhase(state);
    advanceTurn(state);
  }
  return state;
};
