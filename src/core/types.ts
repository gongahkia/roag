export const COURSE_WIDTH = 20;
export const COURSE_HEIGHT = 14;
export type PowerUp = 'turbo' | 'shield' | 'bomb' | 'freeze' | 'swap';
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
  /** shared clockwise corner heights: north-west, north-east, south-east, south-west */
  corners?: [number, number, number, number];
  direction?: { x: number; y: number };
}

export interface Point {
  x: number;
  y: number;
}

export interface SweeperHazard {
  id: string;
  kind: 'sweeper';
  point: Point;
  phaseOffset: number;
  radius: number;
}

export interface GateHazard {
  id: string;
  kind: 'gate';
  point: Point;
  phaseOffset: number;
}

export type CourseHazard = SweeperHazard | GateHazard;
export type ItemPadKind = 'recovery' | 'chaos';
export type BuildTool = 'erase' | 'fairway' | 'rough' | 'sand' | 'ice' | 'wall' | 'booster' | 'conveyor' | 'tee' | 'cup' | 'sweeper' | 'gate' | 'recovery-pad' | 'chaos-pad';

export interface TerrainSettings {
  density: number;
  elevation: number;
  hazards: number;
}

export interface BuildState {
  authorIndex: number;
  tool: BuildTool;
  height: number;
  direction: Point;
  terrain: TerrainSettings;
  generated: boolean;
}

export interface AuthoredCourse {
  authorId: string;
  course: Course;
}

export interface ItemPad {
  id: string;
  point: Point;
  kind: ItemPadKind;
  collected?: boolean;
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
  hazards: CourseHazard[];
  itemPads: ItemPad[];
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
  coursePhase: number;
  authoredCourses: AuthoredCourse[];
  courseIndex: number;
  build?: BuildState;
  emotes: EmoteEvent[];
  emoteSequence: number;
  players: Player[];
  turn: TurnState;
  status: 'lobby' | 'build' | 'validate' | 'preview' | 'playing' | 'draft' | 'finished';
  messages: string[];
}

export type GameCommand =
  | { type: 'shoot'; shot: ShotCommand }
  | { type: 'use-power-up'; powerUp: PowerUp; targetId?: string }
  | { type: 'next-hole' }
  | { type: 'draft'; upgrade: Upgrade }
  | { type: 'build-place'; point: Point }
  | { type: 'build-settings'; tool?: BuildTool; height?: number; direction?: Point; terrain?: Partial<TerrainSettings> }
  | { type: 'build-generate' }
  | { type: 'begin-validation' }
  | { type: 'select-upgrade'; upgrade: Upgrade }
  | { type: 'emote'; playerId: string; emote: Emote };

export interface GameTransport {
  send(command: GameCommand): void;
  onCommand(listener: (command: GameCommand) => void): () => void;
}
