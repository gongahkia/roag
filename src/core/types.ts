export const COURSE_WIDTH = 20;
export const COURSE_HEIGHT = 14;
export type BallForm = 'heavy' | 'bouncy' | 'ghost' | 'magnet' | 'ice' | 'portal';
export type PowerUp = 'turbo' | 'shield' | 'bomb' | 'freeze' | 'swap' | 'two putts' | BallForm;
export type Upgrade = 'heavy ball' | 'ice skates' | 'extra charge' | 'bank shot' | 'hazard shield' | 'chaos magnet' | 'portal savvy' | 'second wind' | 'scavenger';
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
export interface PortalEndpoint {
  point: Point;
  direction: Point;
}

export interface PortalPair {
  id: string;
  entrance?: PortalEndpoint;
  exit?: PortalEndpoint;
}
export type ItemPadKind = 'recovery' | 'chaos';
export type CourseTheme = 'balanced' | 'speedway' | 'hazard-run' | 'ice-rink' | 'quarry';

export interface TerrainSettings {
  density: number;
  elevation: number;
  maxElevation: number;
  routeLength: number;
  bendiness: number;
  laneWidth: number;
  branches: number;
  chaos: number;
  theme: CourseTheme;
  roughRate: number;
  sandRate: number;
  iceRate: number;
  boosterRate: number;
  conveyorRate: number;
  wallCount: number;
  sweeperCount: number;
  gateCount: number;
  portalPairs: number;
  recoveryPads: number;
  chaosPads: number;
  variation: number;
}

export interface HoleRules {
  timerSeconds: number;
  strokeCap: number;
  collisions: boolean;
  powerUps: boolean;
  recoveryBias: number;
  launchMultiplier: number;
  rollingResistanceMultiplier: number;
  wallRestitutionMultiplier: number;
  terrainAccelerationMultiplier: number;
  hazardImpulseMultiplier: number;
  portalSpeedMultiplier: number;
  cupRadius: number;
  hazardPhaseCount: number;
  scoreMultiplier: number;
  startingPowerUp?: PowerUp;
  sharedBoons: Upgrade[];
}

export interface HoleRecipe {
  terrain: TerrainSettings;
  rules: HoleRules;
}

export interface VotingOption {
  id: string;
  label: string;
  recipe: HoleRecipe;
  course: Course;
}

export interface VoteState {
  options: VotingOption[];
  ballots: Record<string, string>;
}

export interface AssemblyState {
  optionId: string;
  label: string;
  theme: TerrainSettings['theme'];
  votes: number;
  totalBallots: number;
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
  /** portal overlays remain optional so saved pre-portal courses stay readable. */
  portals?: PortalPair[];
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
  falling?: boolean;
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
  spareInventory?: PowerUp;
  ballForm?: BallForm;
  portalExitId?: string;
  twoPuttsArmed?: boolean;
  secondWindAvailable?: boolean;
  turboArmed?: boolean;
  frozenTurns?: number;
  hazardShield?: boolean;
  total: number;
}

export interface GameConfig {
  seed: string;
  holeCount: number;
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
  holeRules: HoleRules;
  hole: number;
  coursePhase: number;
  vote?: VoteState;
  assembly?: AssemblyState;
  emotes: EmoteEvent[];
  emoteSequence: number;
  players: Player[];
  turn: TurnState;
  paused: boolean;
  status: 'voting' | 'assembling' | 'playing' | 'finished';
  messages: string[];
}

export type GameCommand =
  | { type: 'shoot'; shot: ShotCommand }
  | { type: 'cast-vote'; playerId: string; optionId: string }
  | { type: 'complete-assembly' }
  | { type: 'set-paused'; paused: boolean }
  | { type: 'use-power-up'; powerUp: PowerUp; targetId?: string; portalExitId?: string }
  | { type: 'arm-second-wind' }
  | { type: 'emote'; playerId: string; emote: Emote };

export interface GameTransport {
  send(command: GameCommand): void;
  onCommand(listener: (command: GameCommand) => void): () => void;
}
