export const COURSE_WIDTH = 20;
export const COURSE_HEIGHT = 14;
export type PowerUp = 'turbo' | 'bomb' | 'freeze' | 'swap';
export type Upgrade = 'heavy ball' | 'ice skates' | 'extra charge' | 'bank shot' | 'hazard shield' | 'chaos magnet';
export const EMOTES = [
  { id: 'cheer', glyph: '\\o/', label: 'cheer' },
  { id: 'taunt', glyph: '>:]', label: 'taunt' },
  { id: 'panic', glyph: '!?', label: 'panic' },
  { id: 'wow', glyph: '*_*', label: 'wow' },
  { id: 'gg', glyph: 'GG', label: 'good game' },
] as const;
export type Emote = typeof EMOTES[number]['id'];

export type Surface =
  | 'void'
  | 'fairway'
  | 'rough'
  | 'sand'
  | 'ice'
  | 'wall'
  | 'tee'
  | 'cup'
  | 'booster'
  | 'conveyor';

export interface Tile {
  surface: Surface;
  height: number;
  slope?: { x: number; y: number };
  direction?: { x: number; y: number };
}

export interface Point {
  x: number;
  y: number;
}

export interface Course {
  id: string;
  seed: string;
  width: number;
  height: number;
  tiles: Tile[];
  tee: Point;
  cup: Point;
  route: Point[];
  score: CourseScore;
}

export interface CourseScore {
  playable: boolean;
  estimatedStrokes: number;
  hazards: number;
  elevation: number;
  routes: number;
  novelty: number;
  total: number;
  solverShots: ShotCommand[];
  rejection?: string;
}

export interface ShotCommand {
  angle: number;
  power: number;
}

export interface Ball {
  x: number;
  y: number;
  z: number;
  vx: number;
  vy: number;
  vz: number;
  strokes: number;
  complete: boolean;
  resetCount: number;
}

export interface Player {
  id: string;
  name: string;
  color: string;
  kind: 'human' | 'bot';
  skill: number | 'adaptive';
  ball: Ball;
  upgrades: Upgrade[];
  inventory?: PowerUp;
  turboArmed?: boolean;
  frozenTurns?: number;
  hazardShield?: boolean;
  total: number;
}

export interface GameConfig {
  seed: string;
  timerSeconds: number;
  strokeCap: number;
  collisions: boolean;
  powerUps: boolean;
  botCount: number;
  humanCount: number;
  botSkill: number | 'adaptive';
}

export interface TurnState {
  playerIndex: number;
  secondsLeft: number;
  shotInFlight: boolean;
}

export interface EmoteEvent {
  id: string;
  playerId: string;
  emote: Emote;
}

export interface GameState {
  config: GameConfig;
  course: Course;
  hole: number;
  emotes: EmoteEvent[];
  emoteSequence: number;
  players: Player[];
  turn: TurnState;
  status: 'lobby' | 'preview' | 'playing' | 'draft' | 'finished';
  messages: string[];
}

export type GameCommand =
  | { type: 'shoot'; shot: ShotCommand }
  | { type: 'use-power-up'; powerUp: PowerUp; targetId?: string }
  | { type: 'next-hole' }
  | { type: 'draft'; upgrade: Upgrade }
  | { type: 'emote'; playerId: string; emote: Emote };

export interface GameTransport {
  send(command: GameCommand): void;
  onCommand(listener: (command: GameCommand) => void): () => void;
}
