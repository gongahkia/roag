
export const MAP_WIDTH = 48
export const MAP_HEIGHT = 35
export const TERMINAL_WIDTH = 96
export const TERMINAL_HEIGHT = 60
export const FLOOR_COUNT = 40

export type Biome = 'mine' | 'wilds' | 'caverns' | 'ruins' | 'furnace' | 'floodedRuins' | 'cliffs' | 'burial' | 'saltFlats' | 'frostReliquary'
export type Direction = 'nw' | 'n' | 'ne' | 'w' | 'wait' | 'e' | 'sw' | 's' | 'se'
export type AutoplayMode = 'off' | 'visible' | 'omniscient'
export type AutoplayPolicy = 'survival' | 'clear' | 'explore' | 'legacy'
export type StatName = 'strength' | 'agility' | 'vitality' | 'intellect'
export type TrailcraftId = 'flintTemper' | 'windKnot' | 'barkBinding' | 'spiritThread' | 'sunstride' | 'prismLedger' | 'brineGrit' | 'rimeEdge' | 'iceNerve' | 'winterVow'
export type TraversalToolId = 'stoneWedge' | 'reedwing' | 'cordAnchor' | 'ashwayRites'
export type RelicId = 'ashCircuit' | 'markbreakerSeal' | 'cairnCoil' | 'tideFetter' | 'prismRelay' | 'winterSeal'
export type BoonId = string
export type CourierOrigin = 'mineborn' | 'mosswalker' | 'cavernSeeker' | 'tidebound'
export type CourierCalling = 'trailguard' | 'pathmaker' | 'spiritbearer'
export type DeathMode = 'checkpoint' | 'ironTrail'
export type Alignment = 'kami' | 'villagePact'
export type DeliveryEnding = Alignment | 'both' | 'plain'
export type SocialFaction = 'trailfolk' | 'kami'
export type SocialRole = 'trader' | 'strandedExplorer' | 'rival' | 'caretaker' | 'ritualist' | 'territorialGroup'
export type SocialOffer = 'routeReveal' | 'supplyCache' | 'shortcut'
export type SocialConsequence = 'alliance' | 'hostility' | 'routeChange'
export type SocialDisposition = 'neutral' | 'allied' | 'hostile'
export type TileKind = 'wall' | 'floor' | 'exit' | 'door' | 'lockedDoor' | 'water' | 'lava' | 'pit' | 'rope' | 'spikes' | 'dart' | 'fireVent' | 'crumble' | 'boulder' | 'web' | 'gas' | 'support' | 'rail' | 'rubble' | 'bramble' | 'darkness' | 'crate' | 'chest' | 'altar' | 'shop' | 'rescue' | 'smoke' | 'lift' | 'breakwall' | 'current' | 'deepWater' | 'anchor' | 'cliffWall' | 'ledge' | 'graveSoil' | 'cairn' | 'ossuary' | 'spiritPath' | 'saltMirror' | 'brine' | 'ice' | 'frostRime'
export type ActorRole = 'hero' | 'monster' | 'merchant' | 'ally' | 'guardian'
export const MONSTER_ROLES = ['guard', 'skirmisher', 'artillery', 'controller', 'ambusher', 'pursuer', 'support', 'scavenger', 'apex'] as const
export type MonsterRole = typeof MONSTER_ROLES[number]
export const TACTICAL_ENCOUNTERS = ['guardPost', 'ambush', 'artilleryCover', 'pursuitLane', 'nativeTerrainPack', 'objectiveDefense', 'roamingThreat'] as const
export type TacticalEncounter = typeof TACTICAL_ENCOUNTERS[number]
export type EquipmentSlot = 'mainHand' | 'offHand' | 'head' | 'body' | 'boots' | 'charm'
export type ItemId = string
export type ConditionKind = 'burning' | 'rooted' | 'staggered' | 'shielded' | 'marked' | 'slowed'
export type GuardianPhase = 'opening' | 'pressure' | 'cataclysm'
export type ObjectiveKind = 'recoverSupplies' | 'rescueScout' | 'invokeAltar' | 'defeatGuardian'
export type ObjectiveStatus = 'active' | 'complete'
export type PropId =
  | 'mine.oreVein' | 'mine.lanternPost' | 'mine.brokenCart' | 'mine.warningMarker' | 'mine.skullMarker' | 'mine.discardedParcel'
  | 'wilds.mushrooms' | 'wilds.danglingCharm' | 'wilds.birdNest' | 'wilds.rootShrine' | 'wilds.lostParcel' | 'wilds.rootArch'
  | 'caverns.crystalCluster' | 'caverns.glowingFungus' | 'caverns.barnacledShrine' | 'caverns.brokenBoat' | 'caverns.eelTunnel' | 'caverns.sealedParcel'
  | 'ruins.brokenStatue' | 'ruins.ritualBrazier' | 'ruins.glyphTablet' | 'ruins.collapsedArch' | 'ruins.sealedCache' | 'ruins.monolith'
  | 'furnace.bellows' | 'furnace.liftConsole' | 'furnace.breakwall' | 'furnace.cinderCache' | 'furnace.smokeStack' | 'furnace.forgeIdol'
  | 'floodedRuins.anchorPost' | 'floodedRuins.floodgate' | 'floodedRuins.sunkenCache' | 'floodedRuins.tideShrine' | 'floodedRuins.currentBell' | 'floodedRuins.mossBridge'
  | 'cliffs.ropeAnchor' | 'cliffs.windVane' | 'cliffs.nestCache' | 'cliffs.skyShrine' | 'cliffs.crackedLedge' | 'cliffs.signalFire'
  | 'burial.cairnGate' | 'burial.funeralLantern' | 'burial.ossuaryCache' | 'burial.graveBloom' | 'burial.ancestorStone' | 'burial.sealedTomb'
  | 'saltFlats.mirageCairn' | 'saltFlats.sunMirror' | 'saltFlats.brineWell' | 'saltFlats.caravanHusk' | 'saltFlats.glassMarker' | 'saltFlats.whiteCache'
  | 'frostReliquary.duelBell' | 'frostReliquary.rimeSarcophagus' | 'frostReliquary.iceForge' | 'frostReliquary.frozenCache' | 'frostReliquary.reliquaryWard' | 'frostReliquary.thawValve'
export type PropState = 'dormant' | 'inspected' | 'activated' | 'destroyed'
export type PropTag = 'salvage' | 'light' | 'route' | 'warning' | 'ritual' | 'growth' | 'water' | 'cache' | 'force' | 'fire' | 'root' | 'hazard' | 'smoke' | 'lift' | 'anchor' | 'current' | 'wind' | 'climb' | 'grave' | 'spirit' | 'ward' | 'salt' | 'mirror' | 'brine' | 'frost' | 'ice' | 'duel'
export type PropEffectKind = 'bomb' | 'fire' | 'water' | 'root' | 'force' | 'throw' | 'hazard' | 'ward' | 'gate' | 'wind' | 'spirit'
export type PropHook = 'operate' | PropEffectKind
export type EncounterKind = 'wayfarer' | 'bloodBargain' | 'shiftingChamber' | 'stormCache' | 'ancestorDebt' | 'cursedObject' | 'oathwell' | 'windTrial' | 'tombAuction'
  | 'sunTribute' | 'mirageMarket' | 'brineOath' | 'glassTrial' | 'whiteRoad' | 'saltCache'
  | 'iceDuel' | 'winterTithe' | 'rimeContract' | 'frostCache' | 'whiteout' | 'reliquaryTrial'
  | 'minePact' | 'mineKami' | 'wildsPact' | 'wildsKami' | 'cavernsPact' | 'cavernsKami' | 'ruinsPact' | 'ruinsKami'
  | 'furnacePact' | 'furnaceKami' | 'floodedPact' | 'floodedKami' | 'cliffsPact' | 'cliffsKami' | 'burialPact' | 'burialKami'
  | 'saltPact' | 'saltKami' | 'frostPact' | 'frostKami'
export type EncounterState = 'dormant' | 'resolved'

export interface Point { x: number; y: number }
export interface Tile {
  kind: TileKind
  explored: boolean
  visible: boolean
  elevation?: 0 | 1
  flow?: { direction: Exclude<Direction, 'wait'>; hazard?: 'undertow' | 'squall' }
}
export interface ConditionState { kind: ConditionKind; duration: number; potency: number }
export interface TacticalEncounterMetadata { id: string; archetype: TacticalEncounter; leader: boolean; answer: string }
export interface Actor {
  id: string
  role: ActorRole
  kind: string
  name: string
  x: number
  y: number
  health: number
  maxHealth: number
  attack: number
  defense: number
  speed: number
  energy: number
  glyph: string
  color: string
  hostile: boolean
  ai?: 'chase' | 'ranged' | 'wander' | 'guardian'
  combatRole?: MonsterRole
  tags?: string[]
  terrainAffinity?: TileKind[]
  encounter?: TacticalEncounterMetadata
  status?: string[]
  conditions?: ConditionState[]
  guardianPhase?: GuardianPhase
}

export interface GroundItem { id: ItemId; x: number; y: number; count: number; visibleInFog?: boolean }
export interface Prop {
  id: string
  kind: PropId
  x: number
  y: number
  biome: Biome
  state: PropState
  tags: PropTag[]
  hooks?: PropHook[]
  effectCells?: Point[]
  expiresAt?: number
}
export interface SocialContract { id: string; faction: SocialFaction; role: SocialRole; goal: string; visibility: 'visible' | 'rumored'; offer: SocialOffer; consequence: SocialConsequence; disposition: SocialDisposition }
export interface FloorEncounter { id: string; kind: EncounterKind; x: number; y: number; state: EncounterState; social?: SocialContract }
export interface FloorObjective { id: string; kind: ObjectiveKind; status: ObjectiveStatus; label: string }
export type RewardMilestoneId = 'waycache' | 'boon-teach' | 'boon-test' | 'boon-payoff'
export interface FloorMilestone { id: string; kind: 'waycache' | 'boon' | 'augment' | 'relic'; x: number; y: number; discovered: boolean; claimed: boolean; rewardKey?: RewardMilestoneId }
export type RewardRole = 'safe' | 'risky' | 'sidegrade'
export interface RewardContext { role: RewardRole; problem: string; terrain: TileKind; route: 'safe' | 'costly' | 'optional'; payoff: string; biomeFit: 'local' | 'global' }
export interface BoonRewardChoice extends RewardContext { id: BoonId }
export interface ToolRewardChoice extends RewardContext { id: TraversalToolId }
export interface BoonRewardOffer { id: string; milestoneId: Exclude<RewardMilestoneId, 'waycache'>; kind: 'boon'; choices: BoonRewardChoice[] }
export interface ToolRewardOffer { id: string; milestoneId: 'waycache'; kind: 'waycache'; choices: ToolRewardChoice[] }
export type RewardOffer = BoonRewardOffer | ToolRewardOffer
export type ExpeditionPhase = 'survey' | 'pressure' | 'counterroute' | 'climax'
export interface FloorEscalation { arcId: string; phase: ExpeditionPhase; topology: string; landmark: string; encounter: string; ecology: string; promise: string; payoff: string; encounterOffset: number; carried: ExpeditionPhase[] }
export interface AreaArcState { biome: Biome; arcId: string; clearedPhases: ExpeditionPhase[] }
export interface ClimbLink { id: string; lower: Point; upper: Point; anchored: boolean }
export interface DifficultyContext { routePosition: number; threat: number; healthMultiplier: number; attackBonus: number; defenseBonus: number; eliteChance: number; guardianPattern: number }
export interface TransientTerrain { x: number; y: number; original: TileKind; originalFlow?: Tile['flow']; expiresAt: number }
export const ECOLOGY_EVENT_KINDS = ['tide', 'wind', 'smoke', 'collapse', 'fire', 'migration', 'nesting', 'visibility'] as const
export type EcologyEventKind = typeof ECOLOGY_EVENT_KINDS[number]
export type EcologyEventState = 'waiting' | 'active' | 'resolved'
export interface EcologyEvent {
  id: string
  kind: EcologyEventKind
  source: string
  target: Point
  route?: string
  node?: string
  warning: string
  startsAt: number
  duration: number
  responses: string[]
  cleanup: string
  state: EcologyEventState
  original: TileKind
  originalFlow?: Tile['flow']
  effect: TileKind
  effectFlow?: Tile['flow']
  warned?: boolean
}
export type TelegraphDanger = 'minor' | 'major'
export interface Telegraph { id: string; sourceId: string; actionId: string; cells: Point[]; danger: TelegraphDanger; resolveTurn: number; collision?: { point: Point; by: string }; cover?: boolean }
export interface Floor {
  index: number
  biome: Biome
  seed: number
  width: number
  height: number
  layoutId: string
  tiles: Tile[]
  actors: Actor[]
  items: GroundItem[]
  props: Prop[]
  encounters?: FloorEncounter[]
  start: Point
  exit: Point
  guardianDefeated: boolean
  objective: FloorObjective
  milestones: FloorMilestone[]
  rewardOffers?: RewardOffer[]
  escalation?: FloorEscalation
  ecology?: EcologyEvent[]
  transientTerrain?: TransientTerrain[]
  telegraphs?: Telegraph[]
  puzzleIds?: string[]
  climbLinks?: ClimbLink[]
  difficulty?: DifficultyContext
}

export interface Hero {
  name: string
  origin: CourierOrigin
  calling: CourierCalling
  deathMode: DeathMode
  x: number
  y: number
  health: number
  maxHealth: number
  focus: number
  maxFocus: number
  gold: number
  bombs: number
  ropes: number
  keys: number
  xp: number
  level: number
  stats: Record<StatName, number>
  skills: string[]
  inventory: ItemId[]
  equipment: Partial<Record<EquipmentSlot, ItemId>>
  lastUnequipped?: ItemId
  conditions?: ConditionState[]
  cooldowns?: Record<string, number>
  trailcrafts?: Partial<Record<TrailcraftId, number>>
  traversalTools?: TraversalToolId[]
  relics?: RelicId[]
  relicCharges?: Partial<Record<RelicId, number>>
  boons?: Partial<Record<BoonId, number>>
  boonEvolutions?: Partial<Record<BoonId, number>>
  safePositions?: Point[]
  oaths?: OathState[]
  curse?: CurseState
}

export interface CourierIdentity { id: string; name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode; createdAt: string; parentId?: string }
export interface CourierSave { version: 1; identity: CourierIdentity; run?: RunState; checkpoint?: RunState; heir?: Hero; campaign: CampaignRouteState; records: Records; archived?: boolean }
export interface CourierMenuEntry { id: string; name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode; area?: Biome; floor?: number; turn?: number; archived?: boolean }
export interface CourierMenuView { entries: CourierMenuEntry[]; selectedId?: string; confirmingDelete?: boolean }
export interface CourierDraft { name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode; focus: 0 | 1 | 2 | 3 }

export interface HubState {
  season: number
  supplies: ItemId[]
  rescued: RescuedNpc[]
  unlockedAreas: Biome[]
  completedAreas: Biome[]
}

export interface RescuedNpc { id: string; name: string; biome: Biome; floor: number }
export interface LineageEvent { id: string; kind: 'npcSacrifice'; npcId: string; npcName: string; biome: Biome; floor: number; gateId: string; seed: number }
export interface OathState { id: 'noHealing' | 'noBombs' | 'noCharms'; remainingFloors: number }
export interface CurseState { itemId: ItemId; name: string; condition: string; remainingEncounters: number; lethal: boolean; failed?: boolean }
export type SocialReputation = Record<SocialFaction, number>
export interface CampaignRouteState { version: 3 | 4 | 5; areaOrder: Biome[]; completedAreas: Biome[]; unlockedAreas: Biome[]; selectedBiome: Biome; rescuedNpcs: RescuedNpc[]; lineageEvents: LineageEvent[]; legacyRecords: LegacyRecord[]; alignment: Record<Alignment, number>; reputation?: SocialReputation }

export interface LegacyRecord {
  id: string
  heirName: string
  biome: Biome
  floor: number
  seed: number
}

export type EncyclopediaSection = 'enemies' | 'telegraphs' | 'tags' | 'gates' | 'legacy'
export interface EncyclopediaState { enemies: string[]; telegraphs: string[]; tags: string[]; gates: string[]; legacyRecords: LegacyRecord[] }
export type KeyBindingId = 'northwest' | 'north' | 'northeast' | 'west' | 'east' | 'southwest' | 'south' | 'southeast' | 'wait' | 'help' | 'encyclopedia' | 'readout' | 'settings' | 'use' | 'drop' | 'throw' | 'equip' | 'skills' | 'bomb' | 'rope' | 'get' | 'operate' | 'descend' | 'swap' | 'script' | 'tool' | 'rewind'

export interface RunActions { moves: number; attacks: number; casts: number; pickups: number; bombs: number; ropes: number; rests: number }
export interface RunMetricSample { turn: number; floor: number; health: number; focus: number; gold: number; bombs: number; ropes: number; kills: number; damageDealt: number; damageTaken: number }
export interface RunFloorMetrics { floor: number; turns: number; kills: number; damageDealt: number; damageTaken: number; goldGained: number; xpGained: number; pickups: number; bombsUsed: number; ropesUsed: number }
export interface RunTelemetry { turns: number; actions: RunActions; kills: number; damageDealt: number; damageTaken: number; goldGained: number; goldSpent: number; xpGained: number; pickups: number; bombsUsed: number; ropesUsed: number; itemsUsed: Record<string, number>; boonPicks: Record<string, number>; boonAugments: Record<string, number>; relicPicks: Record<string, number>; purchases: Record<string, number>; enemyKills: Record<string, number>; eventOutcomes: Record<string, number>; deathCauses: Record<string, number>; samples: RunMetricSample[]; floors: RunFloorMetrics[] }
export interface AutoplayCandidate { command: string; reason: string; score: number }
export interface AutoplayReplayMetadata { seed: number; biome: Biome; areaFloor: number; floorIndex: number; layoutId: string; macroRecipeId: string; routeContractId: string; objectiveId: string; escalation?: string }
export interface AutoplayTraceEntry {
  turn: number
  replay: AutoplayReplayMetadata
  fingerprint: string
  command: string
  reason: string
  candidates: AutoplayCandidate[]
  events: string[]
  nextFingerprint: string
  before: { x: number; y: number; health: number; focus: number; bombs: number; ropes: number; objective: string }
  after: { x: number; y: number; health: number; focus: number; bombs: number; ropes: number; objective: string; modal?: string }
}
export interface AutoplayStall { turn: number; fingerprint: string; visits: number; lastReason?: string; failed: Array<{ command: string; count: number }>; recentPositions: string[]; guards: { strategicVisits: number; noProgressTurns: number; noTurnCommands: number; loopRecoveries: number; recoveryVisits: number } }
export type AutoplayTerminal = 'complete' | 'dead' | 'stalled' | 'turn-limit' | 'manual'
export interface AutoplayDiagnostic { id: string; date: string; seed: number; biome: Biome; floor: number; mode: Exclude<AutoplayMode, 'off'>; policy: AutoplayPolicy; outcome: AutoplayTerminal; turns: number; reason: string; trace: AutoplayTraceEntry[] }
export type RunOutcome = 'lost' | 'complete' | 'suspended'
export interface RunAnalysis { seed: number; biome: Biome; floor: number; outcome: RunOutcome; date: string; metrics: RunTelemetry }

export interface RunState {
  version: 4 | 5
  seed: number
  floor: Floor
  hero: Hero
  messages: string[]
  status: 'title' | 'playing' | 'dead' | 'victory'
  modal?: Modal
  turn: number
  area?: Biome
  areaFloor?: number
  areaArc?: AreaArcState
  areaOrder?: Biome[]
  gateDestination?: Biome
  rescuedNpcs?: RescuedNpc[]
  lineageEvents?: LineageEvent[]
  alignment?: Record<Alignment, number>
  reputation?: SocialReputation
  encyclopedia?: EncyclopediaState
  telemetry?: RunTelemetry
}

export type Modal =
  | { kind: 'help' }
  | { kind: 'readout' }
  | { kind: 'encyclopedia'; section: EncyclopediaSection; page?: number }
  | { kind: 'settings'; page?: number; awaiting?: KeyBindingId }
  | { kind: 'inventory'; mode: 'use' | 'drop' | 'throw' | 'equip' }
  | { kind: 'skills'; source?: 'level' }
  | { kind: 'trailcraft' }
  | { kind: 'boon'; milestoneId: string }
  | { kind: 'augment'; milestoneId: string; mode?: 'evolve' | 'reforge' | 'transmute'; selected?: BoonId[] }
  | { kind: 'tool'; milestoneId: string; replace?: number }
  | { kind: 'relic'; milestoneId: string; replace?: number }
  | { kind: 'encounter'; encounterId: string }
  | { kind: 'tools' }
  | { kind: 'pause' }
  | { kind: 'shop'; merchantId: string }
  | { kind: 'gate'; gateId: string; choice?: number; confirming?: boolean }
  | { kind: 'target'; action: 'throw' | 'spell' | 'bomb' | 'drill' | 'glide' | 'grapple' | 'bridge' | 'dash' | 'winch' | 'stoneWedge' | 'reedwing' | 'cordAnchor' | 'ashwayRites'; item?: ItemId; tool?: TraversalToolId; overdrive?: boolean; direction?: Exclude<Direction, 'wait'> }

export interface RunRecord { seed: number; floor: number; score: number; won: boolean; date: string }
export interface Records { bestDepth: number; wins: number; deaths: number; runs: RunRecord[]; analyses: RunAnalysis[] }

export const DIRECTIONS: Record<Direction, Point> = {
  nw: { x: -1, y: -1 }, n: { x: 0, y: -1 }, ne: { x: 1, y: -1 },
  w: { x: -1, y: 0 }, wait: { x: 0, y: 0 }, e: { x: 1, y: 0 },
  sw: { x: -1, y: 1 }, s: { x: 0, y: 1 }, se: { x: 1, y: 1 }
}

export const SLOT_NAMES: Record<EquipmentSlot, string> = {
  mainHand: 'Main hand', offHand: 'Off hand', head: 'Head', body: 'Body', boots: 'Boots', charm: 'Charm'
}

export const indexOf = (x: number, y: number) => y * MAP_WIDTH + x
export const inBounds = (x: number, y: number) => x >= 0 && x < MAP_WIDTH && y >= 0 && y < MAP_HEIGHT
export const floorIndex = (floor: Pick<Floor, 'width'>, x: number, y: number): number => y * floor.width + x
export const inFloorBounds = (floor: Pick<Floor, 'width' | 'height'>, x: number, y: number): boolean => x >= 0 && x < floor.width && y >= 0 && y < floor.height
export const floorPoint = (floor: Pick<Floor, 'width'>, index: number): Point => ({ x: index % floor.width, y: Math.floor(index / floor.width) })
