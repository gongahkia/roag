export const COURSE_WIDTH = 20;
export const COURSE_HEIGHT = 14;
export type BallForm = 'heavy' | 'bouncy' | 'ghost' | 'magnet' | 'ice' | 'portal' | 'glider' | 'sticky' | 'orbit' | 'quantum' | 'mirror' | 'anvil' | 'vampire' | 'boomerang';
export type GadgetKind = 'popper pad' | 'snare patch' | 'blast mine' | 'slick patch' | 'sky spring' | 'gravity well' | 'mirror plate' | 'toll booth' | 'control inverter' | 'portal gun';
export type StrategyCard = 'tailwind' | 'guardian pin' | 'line reader' | 'soft landing' | 'ghost pass' | 'mulligan relay' | 'clean slate' | 'fairway draft' | 'banker advice' | 'windbreak' | 'steady hands' | 'umbrella cart' | 'mirror caddy' | 'sponsor tab' | 'relay fund' | 'gadgeteer favor' | 'sandbag slip' | 'club flip' | 'slow clock' | 'forced chip' | 'headwind gust' | 'frayed grip' | 'bogey tax' | 'black pennant' | 'anchor line' | 'spring ticket' | 'cushion call' | 'bumper lease' | 'shared draft' | 'wind sail' | 'grounds crew' | 'rescue pact' | 'clubhouse pool' | 'sticky forecast' | 'crosswind debt' | 'dead bounce';
export type PowerUp = 'turbo' | 'shield' | 'bomb' | 'freeze' | 'swap' | 'two putts' | 'cup magnet' | 'slipstream' | 'rebound rig' | 'phase shift' | 'sandbag' | 'rescue drone' | 'airhorn' | 'club flipper' | 'time dilator' | 'mugger' | 'scramble' | 'gravity gloves' | 'bunker buster' | 'portal remote' | 'red tee' | 'black flag' | 'cherry bomb' | 'copycat' | 'wind sock' | 'slope stabilizer' | 'spring polish' | 'bumper wax' | 'cushion map' | GadgetKind | BallForm | StrategyCard;
export type CaddyId = 'heavy ball' | 'ice skates' | 'extra charge' | 'bank shot' | 'hazard shield' | 'chaos magnet' | 'portal savvy' | 'second wind' | 'scavenger' | 'aerial ace' | 'cup reader' | 'gadgeteer' | 'backboard' | 'pinball wizard' | 'rough rider' | 'sand wedge' | 'conveyor cultist' | 'gatecrasher' | 'thornmail' | 'air mail' | 'shock absorber' | 'first responder' | 'pickpocket' | 'revenge club' | 'headwind' | 'bogeyman' | 'coin slot' | 'broker' | 'echo chamber' | 'paradox partner' | 'hole hunter' | 'black market caddy' | 'cushion keeper' | 'spring coach' | 'bumper apprentice' | 'slope scout' | 'wind warden';
/** Kept as a public alias for saved games created before the clubhouse shop. */
export type Upgrade = CaddyId;
export type RealityCard = 'wall is cup' | 'void is fairway' | 'fairway is ice' | 'gravity is sideways' | 'cup walks' | 'everybody is ghost' | 'two is one' | 'portals are plenty' | 'turns are backwards' | 'gates are open' | 'cups are many' | 'ball is cup' | 'bank holiday' | 'spring fling' | 'high winds' | 'cushion league';
export type ChronoCard = 'undo drive' | 'second chance' | 'echo putt' | 'future sight' | 'time theft' | 'frozen frame' | 'parallel parking' | 'grandfather clause';
export type ContentId = CaddyId | PowerUp | RealityCard | ChronoCard;
export type ContentCategory = 'caddy' | 'pocket' | 'form' | 'gadget' | 'reality' | 'chrono';
export type CardTiming = 'immediate' | 'putt' | 'round' | 'hole';
export type CardPolarity = 'boon' | 'curse' | 'neutral';
export type CardTargetMode = 'player' | 'tile' | 'global';
export type CardDurationUnit = 'round' | 'hole';

export interface CardDurationRange {
  unit: CardDurationUnit;
  min: number;
  max: number;
}
export const EMOTES = [
  { id: 'cheer', glyph: '🙌', label: 'cheer' },
  { id: 'taunt', glyph: '😏', label: 'taunt' },
  { id: 'panic', glyph: '😱', label: 'panic' },
  { id: 'wow', glyph: '🤯', label: 'wow' },
  { id: 'gg', glyph: '🤝', label: 'good game' },
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
  | 'conveyor'
  | 'cushion'
  | 'spring'
  | 'bumper';

export interface Tile {
  surface: Surface;
  height: number;
  /** shared clockwise corner heights: north-west, north-east, south-east, south-west */
  corners?: [number, number, number, number];
  direction?: { x: number; y: number };
  /** Preserves each hole's palette after it becomes part of a stitched campaign course. */
  theme?: CourseTheme;
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
  /** Full rotation duration. Missing values in legacy courses use the standard pace. */
  motionPeriodMs?: number;
}

export interface GateHazard {
  id: string;
  kind: 'gate';
  point: Point;
  phaseOffset: number;
  /** Full open/closed pattern duration. Missing values in legacy courses use the standard pace. */
  motionPeriodMs?: number;
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

export interface GustFeature {
  id: string;
  kind: 'gust';
  point: Point;
  direction: Point;
  radius: number;
  strength: number;
}

export type CourseFeature = SinkholeFeature | ThornFeature | PulseFeature | AirRingFeature | GustFeature;
export interface PortalEndpoint {
  point: Point;
  direction: Point;
}

export interface PortalPair {
  id: string;
  entrance?: PortalEndpoint;
  exit?: PortalEndpoint;
}
export type ItemPadKind = 'recovery' | 'chaos' | 'cash';
export type CourseTheme = 'balanced' | 'speedway' | 'hazard-run' | 'ice-rink' | 'quarry' | 'drift' | 'bloom' | 'pulse' | 'carnival' | 'marsh' | 'zephyr';
export type CourseArchetype = 'ribbon' | 'switchback' | 'fork' | 'courtyard' | 'slalom';
export type CourseSizeProfile = 'compact' | 'standard' | 'full';

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
  archetype: CourseArchetype;
  sizeProfile: CourseSizeProfile;
  roughRate: number;
  sandRate: number;
  iceRate: number;
  boosterRate: number;
  conveyorRate: number;
  cushionRate: number;
  springRate: number;
  bumperCount: number;
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
  gustCount: number;
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
  /** @deprecated shop purchases replace package starting items. */
  startingPowerUp?: PowerUp;
  /** @deprecated legacy snapshots are migrated into personal Caddy stacks. */
  sharedBoons: Upgrade[];
}

export interface HoleRecipe {
  terrain: TerrainSettings;
  rules: HoleRules;
}

/** A complete generated course package that can occupy one face of the pre-hole die. */
export interface CoursePackage {
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

export interface DieFace extends CoursePackage {
  /** Base chance weight plus every paid augmentation on this face. */
  weight: number;
  addedBy?: string;
  augmentations: Record<string, number>;
}

export interface DieWager {
  /** Used to scale the cost of a player's successive extra faces. */
  addedSides: number;
  /** Paid chance-weight additions keyed by die face. */
  augmentations: Record<string, number>;
  ready: boolean;
}

export interface DieRoll {
  faceId: string;
  secondsLeft: number;
}

export interface DieState {
  faces: DieFace[];
  wagers: Record<string, DieWager>;
  secondsLeft: number;
  roll?: DieRoll;
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
  archetype?: CourseArchetype;
  sizeProfile?: CourseSizeProfile;
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
  cash: number;
  caddies: CaddyStack[];
  pockets: PocketCard[];
  shotHistory: ShotHistoryEntry[];
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
  controlInverted?: number;
  timeDilated?: number;
  gustReversed?: boolean;
  slopeStabilized?: boolean;
  springPolished?: boolean;
  bumperWaxed?: boolean;
  cushionMapped?: boolean;
  redTee?: Point;
  attachments?: CardAttachment[];
  /** The order a player sank this hole, used for the shared shop queue. */
  holeFinishOrder?: number;
  total: number;
}

export interface CaddyStack {
  id: CaddyId;
  stacks: number;
}

export interface PocketCard {
  id: PowerUp | ChronoCard;
  source: 'shop' | 'pad';
  /** Stable ID lets a player choose the correct copy when two cards share a name. */
  instanceId?: string;
  /** Shop cards with long lifetimes keep their visible roll after purchase. */
  duration?: { unit: CardDurationUnit; amount: number };
}

export interface CardAttachment {
  id: string;
  cardId: StrategyCard;
  effect: StrategyCard;
  casterId: string;
  polarity: Exclude<CardPolarity, 'neutral'>;
  unit: 'putt' | CardDurationUnit;
  remaining: number;
}

export interface ShotHistoryEntry {
  hole: number;
  before: Ball;
  after: Ball;
}

export interface ShopOffer {
  id: string;
  contentId: ContentId;
  category: ContentCategory;
  price: number;
  duration?: { unit: CardDurationUnit; amount: number };
  sold?: boolean;
}

export interface ShopState {
  visit: number;
  opening: boolean;
  shelf: ShopOffer[];
  buyerOrder: string[];
  buyerIndex: number;
  completedBuyerIds: string[];
  rerollVotes: Record<string, boolean>;
  rerollResolved: boolean;
  rerolled: boolean;
  secondsLeft: number;
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
  /** Skip the shared die window and use seed-selected automatic rolls. */
  skipDieBets?: boolean;
}

export interface TurnState {
  playerIndex: number;
  secondsLeft: number;
  shotInFlight: boolean;
  /** Only one pocket card may be committed before each putt. */
  cardPlayed?: boolean;
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
  holeFinishSequence: number;
  /** Continuously advancing active-play clock used by sweepers and timed gates. */
  hazardElapsedMs: number;
  /** Deterministic turn counter retained for saved-game compatibility and seeded item IDs; hazard movement uses hazardElapsedMs. */
  coursePhase: number;
  die?: DieState;
  coursePlan: PlannedHole[];
  transition?: CourseTransition;
  emotes: EmoteEvent[];
  emoteSequence: number;
  cardSequence: number;
  players: Player[];
  gadgets: Gadget[];
  shop?: ShopState;
  queuedReality?: RealityCard;
  activeReality?: RealityCard;
  /** A black flag temporarily overrides the normal next-player rotation. */
  forcedNextPlayerId?: string;
  turn: TurnState;
  paused: boolean;
  status: 'rolling' | 'shopping' | 'transitioning' | 'playing' | 'finished';
  messages: string[];
}

export type GameCommand =
  | { type: 'shoot'; shot: ShotCommand }
  | { type: 'add-die-side'; playerId: string }
  | { type: 'augment-die-face'; playerId: string; faceId: string }
  | { type: 'ready-die-roll'; playerId: string }
  | { type: 'complete-transition' }
  | { type: 'set-paused'; paused: boolean }
  | { type: 'use-power-up'; powerUp: PowerUp | ChronoCard; cardId?: string; targetId?: string; portalExitId?: string; placement?: Point }
  | { type: 'arm-second-wind' }
  | { type: 'shop-vote-reroll'; playerId: string; approve: boolean }
  | { type: 'shop-buy'; playerId: string; offerId: string; replaceCaddyId?: CaddyId }
  | { type: 'shop-sell-caddy'; playerId: string; caddyId: CaddyId }
  | { type: 'shop-skip'; playerId: string }
  | { type: 'emote'; playerId: string; emote: Emote };

export interface GameTransport {
  send(command: GameCommand): void;
  onCommand(listener: (command: GameCommand) => void): () => void;
}
