import { chooseBotDecision, type BotDecision } from './bots';
import { generateCourse } from './generator';
import { COURSE_PHASES } from './hazards';
import { MAX_SETTLE_SECONDS, newBall, simulateImpulse, simulateShot, type BallPhysicsModifiers, type SimulationResult } from './physics';
import { hashSeed, Random } from './random';
import type { Ball, Course, GameCommand, GameConfig, GameState, GameTransport, ItemPadKind, Player, PowerUp, ShotCommand, Upgrade } from './types';

const colors = ['#f6c26b', '#8bd5ca', '#f38ba8', '#cba6f7', '#a6e3a1', '#89b4fa', '#fab387', '#f9e2af', '#94e2d5', '#eba0ac', '#b4befe', '#f5c2e7'];
const recoveryPowerUps: PowerUp[] = ['turbo', 'shield'];
const chaosPowerUps: PowerUp[] = ['turbo', 'bomb', 'freeze', 'swap'];
const upgrades: Upgrade[] = ['heavy ball', 'ice skates', 'extra charge', 'bank shot', 'hazard shield', 'chaos magnet'];

export const UPGRADE_DESCRIPTIONS: Record<Upgrade, string> = {
  'heavy ball': '+12% launch power and 1.45× collision mass',
  'ice skates': 'slides farther across ice',
  'extra charge': 'empty chaos slot refills after every shot',
  'bank shot': 'retains 82% speed on wall rebounds',
  'hazard shield': 'one void rebound each hole',
  'chaos magnet': '65% chance to refill after using an item',
};

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
    coursePhase: 0,
    emotes: [],
    emoteSequence: 0,
    players,
    turn: { playerIndex: 0, secondsLeft: config.timerSeconds, shotInFlight: false },
    status: 'preview',
    messages: [`seed ${course.seed} generated`],
  };
};

const cloneState = (state: GameState): GameState => ({ ...state, course: { ...state.course, hazards: state.course.hazards.map((hazard) => ({ ...hazard, point: { ...hazard.point } })), itemPads: state.course.itemPads.map((pad) => ({ ...pad, point: { ...pad.point } })) }, emotes: state.emotes.map((emote) => ({ ...emote })), players: state.players.map((player) => ({ ...player, ball: { ...player.ball }, upgrades: [...player.upgrades] })), turn: { ...state.turn }, messages: [...state.messages] });

const activePlayer = (state: GameState) => state.players[state.turn.playerIndex]!;

const adjustedShot = (player: Player, shot: ShotCommand): ShotCommand => {
  const multiplier = (player.turboArmed ? 1.55 : 1) * (player.upgrades.includes('heavy ball') ? 1.12 : 1);
  return { ...shot, power: shot.power * multiplier };
};

const modifiersFor = (player: Player): BallPhysicsModifiers => ({
  mass: player.upgrades.includes('heavy ball') ? 1.45 : 1,
  iceSkates: player.upgrades.includes('ice skates'),
  bankShot: player.upgrades.includes('bank shot'),
  hazardShield: player.hazardShield,
});

const simulatePlayerShot = (state: GameState, playerIndex: number, shot: ShotCommand) => {
  const player = state.players[playerIndex]!;
  return simulateShot(state.course, player.ball, adjustedShot(player, shot), undefined, {
    modifiers: modifiersFor(player),
    otherBalls: state.players.filter((_, index) => index !== playerIndex).map((candidate) => ({ ball: candidate.ball, modifiers: modifiersFor(candidate) })),
    collisions: state.config.collisions,
    phase: state.coursePhase,
    collectItems: state.config.powerUps && !player.inventory,
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
};

export const previewShot = (state: GameState, shot: ShotCommand): Ball[][] | undefined => {
  if (state.status !== 'playing' || state.turn.shotInFlight) return undefined;
  const playerIndex = state.turn.playerIndex;
  const player = state.players[playerIndex]!;
  if (player.frozenTurns) return undefined;
  const result = simulatePlayerShot(state, playerIndex, shot);
  return result.frames.map((frame) => {
    let otherIndex = 0;
    return state.players.map((_, index) => index === playerIndex ? frame.ball : frame.otherBalls[otherIndex++]!);
  });
};

const addMessage = (state: GameState, message: string) => {
  state.messages = [message, ...state.messages].slice(0, 5);
};

const advanceCoursePhase = (state: GameState) => {
  state.coursePhase = (state.coursePhase + 1) % COURSE_PHASES;
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
      advanceCoursePhase(state);
      advanceTurn(state);
      return state;
    }
    const playerIndex = state.turn.playerIndex;
    const result = simulatePlayerShot(state, playerIndex, command.shot);
    player.turboArmed = false;
    applySimulation(state, playerIndex, result);
    if (result.holed) addMessage(state, `${player.name} sinks it in ${player.ball.strokes}`);
    else if (result.reset) addMessage(state, `${player.name} finds the edge`);
    else addMessage(state, `${player.name} rolls to safety`);
    const pad = result.itemPadIds.map((id) => state.course.itemPads.find((candidate) => candidate.id === id)).find(Boolean);
    if (state.config.powerUps && !player.inventory && pad) {
      pad.collected = true;
      player.inventory = powerUpFor(state, player, pad.kind, pad.id);
      addMessage(state, `${player.name} taps a ${pad.kind} pad — ${player.inventory}`);
    } else if (state.config.powerUps && !player.inventory && player.upgrades.includes('extra charge')) {
      player.inventory = powerUpFor(state, player, 'recovery', 'extra-charge');
      addMessage(state, `${player.name}'s extra charge pulls ${player.inventory}`);
    }
    advanceCoursePhase(state);
    advanceTurn(state);
  }
  if (command.type === 'use-power-up' && state.status === 'playing') usePowerUp(state, command.powerUp, command.targetId);
  if (command.type === 'emote') {
    const player = state.players.find((candidate) => candidate.id === command.playerId);
    if (player) {
      state.emoteSequence += 1;
      state.emotes = [...state.emotes, { id: `${state.course.seed}:${command.playerId}:${state.emoteSequence}`, playerId: command.playerId, emote: command.emote }].slice(-16);
    }
  }
  if (command.type === 'draft' && state.status === 'draft') {
    const player = activePlayer(state);
    if (!player.upgrades.includes(command.upgrade)) player.upgrades.push(command.upgrade);
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
  state.coursePhase = 0;
  state.course = generateCourse(hashSeed(state.config.seed, hole));
  state.players.forEach((player) => {
    player.ball = newBall(state.course);
    player.inventory = undefined;
    player.turboArmed = false;
    player.frozenTurns = undefined;
    player.hazardShield = player.upgrades.includes('hazard shield');
  });
  state.turn = { playerIndex: 0, secondsLeft: state.config.timerSeconds, shotInFlight: false };
  state.status = 'preview';
  addMessage(state, `hole ${hole}: inspect, lock, and tee off`);
};

const usePowerUp = (state: GameState, powerUp: PowerUp, targetId?: string) => {
  const player = activePlayer(state);
  if (player.inventory !== powerUp) return;
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
    const result = simulateImpulse(state.course, target.ball, { x: (target.ball.x - player.ball.x) / distance * 4.6, y: (target.ball.y - player.ball.y) / distance * 4.6 }, MAX_SETTLE_SECONDS, modifiersFor(target), state.coursePhase);
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
  if (!used) return;
  player.inventory = undefined;
  if (state.config.powerUps && player.upgrades.includes('chaos magnet') && new Random(`${state.course.seed}:${player.id}:${player.ball.strokes}:${powerUp}`).chance(.65)) {
    player.inventory = powerUpFor(state, player, 'chaos', 'chaos-magnet');
    addMessage(state, `${player.name}'s chaos magnet pulls ${player.inventory}`);
  }
};

export const beginCourse = (state: GameState): GameState => ({ ...state, status: 'playing', messages: ['tee off — aim with the board, then shoot', ...state.messages] });

export const tickTurn = (current: GameState, elapsedSeconds: number): GameState => {
  if (current.status !== 'playing' || current.turn.shotInFlight) return current;
  const state = cloneState(current);
  state.turn.secondsLeft = Math.max(0, state.turn.secondsLeft - elapsedSeconds);
  if (state.turn.secondsLeft === 0) {
    addMessage(state, `${activePlayer(state).name} timed out`);
    advanceCoursePhase(state);
    advanceTurn(state);
  }
  return state;
};

const powerUpFor = (state: GameState, player: Player, kind: ItemPadKind, source: string): PowerUp => {
  const random = new Random(`${state.course.seed}:${source}:${player.id}:${state.coursePhase}:${player.ball.strokes}`);
  const leaderScore = Math.min(...state.players.map((candidate) => candidate.total + candidate.ball.strokes));
  const deficit = player.total + player.ball.strokes - leaderScore;
  if (deficit >= 2 && random.chance(.7)) return random.pick(recoveryPowerUps);
  return random.pick(kind === 'recovery' ? recoveryPowerUps : chaosPowerUps);
};

export const currentUpgradeChoices = (state: GameState): Upgrade[] => upgrades.map((_, index) => upgrades[(index + state.hole + state.turn.playerIndex) % upgrades.length]!);

export const botMove = (state: GameState): BotDecision | undefined => {
  const player = activePlayer(state);
  if (player.kind !== 'bot' || state.status !== 'playing' || state.turn.shotInFlight) return undefined;
  return chooseBotDecision(state.course, player, state.players, state.coursePhase);
};

export class LocalTransport implements GameTransport {
  private listeners = new Set<(command: GameCommand) => void>();
  send(command: GameCommand) { this.listeners.forEach((listener) => listener(command)); }
  onCommand(listener: (command: GameCommand) => void) { this.listeners.add(listener); return () => this.listeners.delete(listener); }
}
