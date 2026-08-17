export const COURSE_WIDTH = 20;
export const COURSE_HEIGHT = 14;
export type BallForm = 'heavy' | 'bouncy' | 'ghost' | 'magnet' | 'ice' | 'portal' | 'glider' | 'sticky' | 'orbit';
export type GadgetKind = 'popper pad' | 'snare patch' | 'blast mine' | 'slick patch' | 'sky spring';
export type PowerUp = 'turbo' | 'shield' | 'bomb' | 'freeze' | 'swap' | 'two putts' | 'cup magnet' | 'slipstream' | 'rebound rig' | 'phase shift' | 'sandbag' | 'rescue drone' | 'airhorn' | GadgetKind | BallForm;
export type Upgrade = 'heavy ball' | 'ice skates' | 'extra charge' | 'bank shot' | 'hazard shield' | 'chaos magnet' | 'portal savvy' | 'second wind' | 'scavenger' | 'aerial ace' | 'cup reader' | 'gadgeteer';
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

export interface UpdraftHazard {
  id: string;
  kind: 'updraft';
  point: Point;
  direction: Point;
  radius: number;
  strength: number;
}

export interface LowBarHazard {
  id: string;
  kind: 'low-bar';
  point: Point;
  clearance: number;
}

export type CourseHazard = SweeperHazard | GateHazard | UpdraftHazard | LowBarHazard;

export interface SinkholeFeature {
  id: string;
  kind: 'sinkhole';
  entrance: Point;
  exit: Point;
}

export interface ThornFeature {
  id: string;
  kind: 'thorn';
  point: Point;
  radius: number;
}

export interface PulseFeature {
  id: string;
  kind: 'pulse';
  point: Point;
  direction: Point;
  strength: number;
}

export interface AirRingFeature {
  id: string;
  kind: 'air-ring';
  point: Point;
  radius: number;
  boost: number;
}

export type CourseFeature = SinkholeFeature | ThornFeature | PulseFeature | AirRingFeature;
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
export type CourseTheme = 'balanced' | 'speedway' | 'hazard-run' | 'ice-rink' | 'quarry' | 'drift' | 'bloom' | 'pulse';

export interface TerrainSettings {
  width: number;
  height: number;
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
  sinkholePairs: number;
  thornCount: number;
  pulseCount: number;
  updraftCount: number;
  lowBarCount: number;
  airRingCount: number;
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

export interface PlannedHole {
  id: string;
  label: string;
  recipe: HoleRecipe;
  courseSeed: string;
}

export interface VoteState {
  options: VotingOption[];
  ballots: Record<string, string>;
}

export interface CourseTransition {
  next: PlannedHole;
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
  theme: CourseTheme;
  width: number;
  height: number;
  tiles: Tile[];
  tee: Point;
  cup: Point;
  route: Point[];
  hazards: CourseHazard[];
  features: CourseFeature[];
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

export type ShotKind = 'putt' | 'chip';

export interface ShotCommand {
  angle: number;
  power: number;
  /** Omitted legacy commands are grounded putts. */
  kind?: ShotKind;
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
  cupMagnetArmed?: boolean;
  slipstreamArmed?: boolean;
  reboundRigArmed?: boolean;
  sandbagged?: boolean;
  forcedChip?: boolean;
  total: number;
}

export interface Gadget {
  id: string;
  ownerId: string;
  kind: GadgetKind;
  point: Point;
}

export interface GameConfig {
  seed: string;
  holeCount: number;
  botCount: number;
  humanCount: number;
  botSkill: number | 'adaptive';
  courseWidth?: number;
  courseHeight?: number;
  /** Lock a seed-selected package for every hole before the first tee shot. */
  skipVoting?: boolean;
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
  coursePlan: PlannedHole[];
  transition?: CourseTransition;
  emotes: EmoteEvent[];
  emoteSequence: number;
  players: Player[];
  gadgets: Gadget[];
  turn: TurnState;
  paused: boolean;
  status: 'voting' | 'transitioning' | 'playing' | 'finished';
  messages: string[];
}

export type GameCommand =
  | { type: 'shoot'; shot: ShotCommand }
  | { type: 'cast-vote'; playerId: string; optionId: string }
  | { type: 'complete-transition' }
  | { type: 'set-paused'; paused: boolean }
  | { type: 'use-power-up'; powerUp: PowerUp; targetId?: string; portalExitId?: string; placement?: Point }
  | { type: 'arm-second-wind' }
  | { type: 'emote'; playerId: string; emote: Emote };

export interface GameTransport {
  send(command: GameCommand): void;
  onCommand(listener: (command: GameCommand) => void): () => void;
}
