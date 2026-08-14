import { chooseBotDecision } from './bots';
import { generateCourse } from './generator';
import { newBall, simulateShot } from './physics';
import { hashSeed } from './random';
import type { Ball, Course, GameCommand, GameConfig, GameState, GameTransport, Player, PowerUp, ShotCommand, WorldRotation } from './types';

const colors = ['#f6c26b', '#8bd5ca', '#f38ba8', '#cba6f7', '#a6e3a1', '#89b4fa', '#fab387', '#f9e2af', '#94e2d5', '#eba0ac', '#b4befe', '#f5c2e7'];
const powerUps: PowerUp[] = ['turbo', 'bomb', 'freeze', 'swap'];
const upgrades = ['heavy ball', 'ice skates', 'extra charge', 'bank shot', 'hazard shield', 'chaos magnet'];

export const defaultConfig = (): GameConfig => ({
  seed: `enemy-${Math.random().toString(36).slice(2, 8)}`,
  timerSeconds: 24,
  strokeCap: 10,
  collisions: true,
  powerUps: true,
  botCount: 3,
  humanCount: 1,
  botSkill: 5,
});

const emptyPlayer = (id: string, index: number, kind: Player['kind'], skill: Player['skill'], course: Course): Player => ({
  id,
  name: kind === 'bot' ? `enemy-${index + 1}` : `golfer-${index + 1}`,
  color: colors[index]!,
  kind,
  skill,
  ball: newBall(course),
  upgrades: [],
  total: 0,
});

export const createGame = (config: GameConfig): GameState => {
  const course = generateCourse(hashSeed(config.seed, 1));
  const players = Array.from({ length: config.humanCount }, (_, index) => emptyPlayer(`human-${index}`, index, 'human', 0, course));
  players.push(...Array.from({ length: config.botCount }, (_, index) => emptyPlayer(`bot-${index}`, players.length + index, 'bot', config.botSkill, course)));
  return {
    config,
    course,
    hole: 1,
    rotation: 0,
    players,
    turn: { playerIndex: 0, secondsLeft: config.timerSeconds, shotInFlight: false },
    status: 'preview',
    messages: [`seed ${course.seed} generated`],
  };
};

const cloneState = (state: GameState): GameState => ({ ...state, course: { ...state.course, pickups: state.course.pickups.map((pickup) => ({ ...pickup })) }, players: state.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades] })), turn: { ...state.turn }, messages: [...state.messages] });

const activePlayer = (state: GameState) => state.players[state.turn.playerIndex]!;

const adjustedShot = (player: Player, shot: ShotCommand): ShotCommand => {
  const multiplier = (player.turboArmed ? 1.55 : 1) * (player.upgrades.includes('heavy ball') ? 1.12 : 1);
  return { ...shot, power: shot.power * multiplier };
};

export const previewShot = (state: GameState, shot: ShotCommand): Ball[] | undefined => {
  if (state.status !== 'playing' || state.turn.shotInFlight) return undefined;
  const player = activePlayer(state);
  if (player.frozenTurns) return undefined;
  return simulateShot(state.course, player.ball, adjustedShot(player, shot), 10, state.rotation).frames;
};

const addMessage = (state: GameState, message: string) => {
  state.messages = [message, ...state.messages].slice(0, 5);
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

const finishHole = (state: GameState) => {
  for (const player of state.players) {
    const strokes = player.ball.complete ? player.ball.strokes : state.config.strokeCap;
    player.total += strokes;
  }
  if (state.hole >= 9) {
    state.status = 'finished';
    addMessage(state, 'campaign complete');
  } else {
    state.status = 'draft';
    addMessage(state, `hole ${state.hole} scored — choose an upgrade`);
  }
};

export const applyCommand = (current: GameState, command: GameCommand): GameState => {
  const state = cloneState(current);
  if (command.type === 'shoot' && state.status === 'playing' && !state.turn.shotInFlight) {
    const player = activePlayer(state);
    if (player.frozenTurns) {
      player.frozenTurns -= 1;
      addMessage(state, `${player.name} is frozen solid`);
      advanceTurn(state);
      return state;
    }
    const result = simulateShot(state.course, player.ball, adjustedShot(player, command.shot), 10, state.rotation);
    player.turboArmed = false;
    player.ball = result.ball;
    if (result.holed) addMessage(state, `${player.name} sinks it in ${player.ball.strokes}`);
    else if (result.reset) addMessage(state, `${player.name} finds the edge`);
    else addMessage(state, `${player.name} rolls to safety`);
    if (!player.inventory) {
      const pickup = state.course.pickups.find((candidate) => result.pickupIds.includes(candidate.id) && !candidate.collected);
      if (pickup) {
        pickup.collected = true;
        player.inventory = pickup.powerUp;
        addMessage(state, `${player.name} collects ${pickup.powerUp}`);
      }
    }
    if (state.config.powerUps && !player.inventory && (player.ball.strokes % 2 === 0 || player.upgrades.includes('extra charge'))) player.inventory = powerUps[(state.hole + state.turn.playerIndex) % powerUps.length]!;
    advanceTurn(state);
  }
  if (command.type === 'use-power-up' && state.status === 'playing') usePowerUp(state, command.powerUp, command.targetId);
  if (command.type === 'rotate-world' && (state.status === 'preview' || state.status === 'playing')) {
    state.rotation = ((state.rotation + command.direction + 4) % 4) as WorldRotation;
    addMessage(state, `world turns ${rotationName(state.rotation)}`);
  }
  if (command.type === 'draft' && state.status === 'draft') {
    const player = activePlayer(state);
    player.upgrades.push(command.upgrade);
    addMessage(state, `${player.name} drafts ${command.upgrade}`);
    advanceDraftOrHole(state);
  }
  if (command.type === 'next-hole' && state.status === 'draft') startHole(state, state.hole + 1);
  return state;
};

const advanceDraftOrHole = (state: GameState) => {
  const next = state.players.findIndex((player) => player.upgrades.length < state.hole);
  if (next === -1) startHole(state, state.hole + 1);
  else state.turn = { playerIndex: next, secondsLeft: state.config.timerSeconds, shotInFlight: false };
};

const startHole = (state: GameState, hole: number) => {
  state.hole = hole;
  state.course = generateCourse(hashSeed(state.config.seed, hole));
  state.rotation = 0;
  state.players.forEach((player) => {
    player.ball = newBall(state.course);
    player.inventory = undefined;
    player.turboArmed = false;
    player.frozenTurns = undefined;
  });
  state.turn = { playerIndex: 0, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  state.status = 'preview';
  addMessage(state, `hole ${hole}: inspect, lock, and tee off`);
};

const usePowerUp = (state: GameState, powerUp: PowerUp, targetId?: string) => {
  const player = activePlayer(state);
  if (player.inventory !== powerUp) return;
  const target = state.players.find((candidate) => candidate.id === targetId);
  if (powerUp === 'turbo') {
    player.turboArmed = true;
    addMessage(state, `${player.name} arms turbo`);
  }
  if (powerUp === 'bomb' && target) {
    target.ball.vx += target.ball.x > player.ball.x ? 2.8 : -2.8;
    target.ball.vy += 1.6;
    addMessage(state, `${player.name} bombs ${target.name}`);
  }
  if (powerUp === 'freeze' && target) {
    target.frozenTurns = 1;
    addMessage(state, `${player.name} freezes ${target.name}`);
  }
  if (powerUp === 'swap' && target) {
    [player.ball.x, target.ball.x] = [target.ball.x, player.ball.x];
    [player.ball.y, target.ball.y] = [target.ball.y, player.ball.y];
    addMessage(state, `${player.name} swaps with ${target.name}`);
  }
  player.inventory = undefined;
};

export const beginCourse = (state: GameState): GameState => ({ ...state, status: 'playing', messages: ['tee off — aim with the board, then shoot', ...state.messages] });

export const tickTurn = (current: GameState, elapsedSeconds: number): GameState => {
  if (current.status !== 'playing' || current.turn.shotInFlight) return current;
  const state = cloneState(current);
  state.turn.secondsLeft = Math.max(0, state.turn.secondsLeft - elapsedSeconds);
  if (state.turn.secondsLeft === 0) {
    addMessage(state, `${activePlayer(state).name} timed out`);
    advanceTurn(state);
  }
  return state;
};

export const currentUpgradeChoices = (state: GameState) => upgrades.map((_, index) => upgrades[(index + state.hole + state.turn.playerIndex) % upgrades.length]!);

const rotationName = (rotation: WorldRotation) => ['north', 'east', 'south', 'west'][rotation]!;

export const botMove = (state: GameState): ShotCommand | undefined => {
  const player = activePlayer(state);
  if (player.kind !== 'bot' || state.status !== 'playing' || state.turn.shotInFlight) return undefined;
  return chooseBotDecision(state.course, player, state.players, state.rotation).shot;
};

export class LocalTransport implements GameTransport {
  private listeners = new Set<(command: GameCommand) => void>();
  send(command: GameCommand) { this.listeners.forEach((listener) => listener(command)); }
  onCommand(listener: (command: GameCommand) => void) { this.listeners.add(listener); return () => this.listeners.delete(listener); }
}
