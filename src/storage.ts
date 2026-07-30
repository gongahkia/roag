import { isItemId, isMonsterId, isSkillId } from './content'
import { objectiveForFloor } from './objectives'
import { ECOLOGY_EVENT_KINDS, FLOOR_COUNT, MAP_HEIGHT, MAP_WIDTH, MONSTER_ROLES, TACTICAL_ENCOUNTERS, floorIndex, inFloorBounds } from './types'
import type { Actor, Alignment, AreaArcState, Biome, CampaignCarryoverDiagnostic, CampaignCycle, CampaignRouteState, ClimbLink, Companion, CompanionControlMode, CompanionControlModeEvent, CompanionDeathMode, ConditionState, CourierCalling, CourierMenuEntry, CourierOrigin, CourierSave, CurseState, DeathMode, DifficultyContext, EncounterKind, EncyclopediaState, Floor, FloorEncounter, FloorEscalation, FloorMilestone, GroundItem, Hero, InteractionTelemetry, LegacyRecord, LineageEvent, Modal, OathState, OptionalContentTelemetry, Point, Prop, Records, RelicId, RescuedNpc, RewardOffer, RunAnalysis, RunFloorMetrics, RunMetricSample, RunState, RunTelemetry, SecretClueChannel, SecretRoom, SecretRoute, ShortcutReturn, SocialContract, SocialReputation, Telegraph, Tile, TransientTerrain, TraversalToolId } from './types'
import { createRunTelemetry } from './telemetry'
import { secretRulesForSourceId } from './secrets'
import { PROP_IDS } from './props'
import { campaignCycleErrors, cloneCampaignCycle, DEFAULT_AREA_ORDER, LEGACY_AREA_ORDER, initialCampaignCycle, initialCampaignRoute, isCampaignAreaOrder, isLegacyCampaignAreaOrder } from './engine/campaign'
import { cloneCarryoverDiagnostics } from './engine/carryover'
import { cloneCompanions, companionLeadsForRescues } from './engine/companions'

const DB = 'jomon-expedition-v2'
const STORE = 'state'
const RUN = 'active-run'
const RECORDS = 'records'
const CAMPAIGN_ROUTE = 'campaign-route'
const COURIER_INDEX = 'courier-index'
const COURIER_PREFIX = 'courier:'

type RunRecord = Omit<RunState, 'version'> & { version: number }
type UnknownRecord = Record<string, unknown>
interface CampaignRouteRecord {
  version: 1 | 2 | 3 | 4 | 5
  areaOrder?: Biome[]
  completedAreas: Biome[]
  unlockedAreas: Biome[]
  selectedBiome: Biome
  rescuedNpcs?: RescuedNpc[]
  companions?: Companion[]
  companionControlMode?: CompanionControlMode
  companionControlHistory?: CompanionControlModeEvent[]
  carryoverDiagnostics?: CampaignCarryoverDiagnostic[]
  lineageEvents?: LineageEvent[]
  legacyRecords?: LegacyRecord[]
  alignment?: Record<Alignment, number>
  reputation?: SocialReputation
  cycle?: CampaignCycle
}

export class SerialWriteQueue {
  private tail: Promise<void> = Promise.resolve()

  enqueue<T>(operation: () => Promise<T>): Promise<T> {
    const result = this.tail.then(operation)
    this.tail = result.then(() => undefined, () => undefined)
    return result
  }

  flush(): Promise<void> { return this.tail }
}

const isRecord = (value: unknown): value is UnknownRecord => typeof value === 'object' && value !== null && !Array.isArray(value)
const isNumber = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value)
const isInteger = (value: unknown): value is number => isNumber(value) && Number.isInteger(value)
const isNonNegativeInteger = (value: unknown): value is number => isInteger(value) && value >= 0
const isString = (value: unknown): value is string => typeof value === 'string'
const isCourierOrigin = (value: unknown): value is CourierOrigin => value === 'mineborn' || value === 'mosswalker' || value === 'cavernSeeker' || value === 'tidebound'
const isCourierCalling = (value: unknown): value is CourierCalling => value === 'trailguard' || value === 'pathmaker' || value === 'spiritbearer'
const isDeathMode = (value: unknown): value is DeathMode => value === 'checkpoint' || value === 'ironTrail'
const isCompanionControlMode = (value: unknown): value is CompanionControlMode => value === 'autonomous' || value === 'direct'
const isCompanionDeathMode = (value: unknown): value is CompanionDeathMode => value === 'injury' || value === 'permadeath'
const oneOf = <T extends string>(value: unknown, values: readonly T[]): value is T => typeof value === 'string' && values.some(current => current === value)
const BIOMES = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'] as const
const isAreaOrder = (value: unknown): value is Biome[] => Array.isArray(value) && value.every(area => oneOf(area, BIOMES)) && (isCampaignAreaOrder(value) || isLegacyCampaignAreaOrder(value))
const TILE_KINDS = ['wall', 'floor', 'exit', 'door', 'lockedDoor', 'water', 'lava', 'pit', 'rope', 'spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'web', 'gas', 'support', 'rail', 'rubble', 'bramble', 'darkness', 'crate', 'chest', 'altar', 'shop', 'rescue', 'smoke', 'lift', 'breakwall', 'current', 'deepWater', 'anchor', 'cliffWall', 'ledge', 'graveSoil', 'cairn', 'ossuary', 'spiritPath', 'saltMirror', 'brine', 'ice', 'frostRime'] as const
const isPoint = (value: unknown): value is Point => isRecord(value) && isNumber(value.x) && isNumber(value.y)
const isTile = (value: unknown): value is Tile => isRecord(value) && oneOf(value.kind, TILE_KINDS) && typeof value.explored === 'boolean' && typeof value.visible === 'boolean' && (value.elevation === undefined || value.elevation === 0 || value.elevation === 1 || value.elevation === 2) && (value.flow === undefined || (isRecord(value.flow) && oneOf(value.flow.direction, ['nw', 'n', 'ne', 'w', 'e', 'sw', 's', 'se']) && (value.flow.hazard === undefined || oneOf(value.flow.hazard, ['undertow', 'squall']))))
const isFlow = (value: unknown): boolean => isRecord(value) && oneOf(value.direction, ['nw', 'n', 'ne', 'w', 'e', 'sw', 's', 'se']) && (value.hazard === undefined || oneOf(value.hazard, ['undertow', 'squall']))
const isCondition = (value: unknown): value is ConditionState => isRecord(value) && oneOf(value.kind, ['burning', 'rooted', 'staggered', 'shielded', 'marked', 'slowed']) && isNumber(value.duration) && isNumber(value.potency)
const isActor = (value: unknown): value is Actor => isRecord(value) && isString(value.id) && oneOf(value.role, ['hero', 'monster', 'merchant', 'ally', 'guardian']) && isString(value.kind) && isString(value.name) && isNumber(value.x) && isNumber(value.y) && isNumber(value.health) && isNumber(value.maxHealth) && isNumber(value.attack) && isNumber(value.defense) && isNumber(value.speed) && isNumber(value.energy) && isString(value.glyph) && isString(value.color) && typeof value.hostile === 'boolean' && (value.role === 'monster' || value.role === 'guardian' ? isMonsterId(value.kind) : value.role === 'merchant' ? value.kind === 'merchant' : value.role === 'ally' ? value.kind === 'ally' : true) && (value.ai === undefined || oneOf(value.ai, ['chase', 'ranged', 'wander', 'guardian'])) && (value.combatRole === undefined || oneOf(value.combatRole, MONSTER_ROLES)) && (value.tags === undefined || (Array.isArray(value.tags) && value.tags.every(isString))) && (value.terrainAffinity === undefined || (Array.isArray(value.terrainAffinity) && value.terrainAffinity.every(kind => oneOf(kind, TILE_KINDS)))) && (value.encounter === undefined || (isRecord(value.encounter) && isString(value.encounter.id) && oneOf(value.encounter.archetype, TACTICAL_ENCOUNTERS) && typeof value.encounter.leader === 'boolean' && isString(value.encounter.answer))) && (value.status === undefined || (Array.isArray(value.status) && value.status.every(isString))) && (value.conditions === undefined || (Array.isArray(value.conditions) && value.conditions.every(isCondition))) && (value.guardianPhase === undefined || oneOf(value.guardianPhase, ['opening', 'pressure', 'cataclysm']))
const isGroundItem = (value: unknown): value is GroundItem => isRecord(value) && (value.id === 'gold' || isItemId(value.id)) && isInteger(value.x) && isInteger(value.y) && isNonNegativeInteger(value.count) && value.count > 0 && (value.tool === undefined || isTraversalTool(value.tool)) && (value.secretId === undefined || isString(value.secretId))
const isProp = (value: unknown): value is Prop => isRecord(value) && isString(value.id) && oneOf(value.kind, PROP_IDS) && isNumber(value.x) && isNumber(value.y) && oneOf(value.biome, BIOMES) && oneOf(value.state, ['dormant', 'inspected', 'activated', 'destroyed']) && Array.isArray(value.tags) && value.tags.every(isString) && (value.hooks === undefined || (Array.isArray(value.hooks) && value.hooks.every(hook => hook === 'operate' || ['bomb', 'fire', 'water', 'root', 'force', 'throw', 'hazard', 'ward', 'gate', 'wind', 'spirit'].includes(hook)))) && (value.effectCells === undefined || (Array.isArray(value.effectCells) && value.effectCells.every(isPoint))) && (value.expiresAt === undefined || isNumber(value.expiresAt))
const ENCOUNTER_KINDS: readonly EncounterKind[] = ['wayfarer', 'bloodBargain', 'shiftingChamber', 'stormCache', 'ancestorDebt', 'cursedObject', 'oathwell', 'windTrial', 'tombAuction', 'sunTribute', 'mirageMarket', 'brineOath', 'glassTrial', 'whiteRoad', 'saltCache', 'iceDuel', 'winterTithe', 'rimeContract', 'frostCache', 'whiteout', 'reliquaryTrial', 'minePact', 'mineKami', 'wildsPact', 'wildsKami', 'cavernsPact', 'cavernsKami', 'ruinsPact', 'ruinsKami', 'furnacePact', 'furnaceKami', 'floodedPact', 'floodedKami', 'cliffsPact', 'cliffsKami', 'burialPact', 'burialKami', 'saltPact', 'saltKami', 'frostPact', 'frostKami']
const isEncounterKind = (value: unknown): value is EncounterKind => oneOf(value, ENCOUNTER_KINDS)
const isSocialContract = (value: unknown): value is SocialContract => isRecord(value) && isString(value.id) && oneOf(value.faction, ['trailfolk', 'kami']) && oneOf(value.role, ['trader', 'strandedExplorer', 'rival', 'caretaker', 'ritualist', 'territorialGroup']) && isString(value.goal) && oneOf(value.visibility, ['visible', 'rumored']) && oneOf(value.offer, ['routeReveal', 'supplyCache', 'shortcut']) && oneOf(value.consequence, ['alliance', 'hostility', 'routeChange']) && oneOf(value.disposition, ['neutral', 'allied', 'hostile'])
const isFloorEncounter = (value: unknown): value is FloorEncounter => isRecord(value) && isString(value.id) && isEncounterKind(value.kind) && isPoint(value) && oneOf(value.state, ['dormant', 'resolved']) && (value.social === undefined || isSocialContract(value.social)) && (value.toolOffer === undefined || isTraversalTool(value.toolOffer))
const isRewardChoice = (value: unknown): boolean => isRecord(value) && isString(value.id) && oneOf(value.role, ['safe', 'risky', 'sidegrade']) && isString(value.problem) && oneOf(value.terrain, TILE_KINDS) && oneOf(value.route, ['safe', 'costly', 'optional']) && isString(value.payoff) && oneOf(value.biomeFit, ['local', 'global'])
const isRewardOffer = (value: unknown): value is RewardOffer => isRecord(value) && isString(value.id) && ((value.kind === 'waycache' && value.milestoneId === 'waycache') || (value.kind === 'boon' && oneOf(value.milestoneId, ['boon-teach', 'boon-test', 'boon-payoff']))) && Array.isArray(value.choices) && value.choices.length === 3 && value.choices.every(isRewardChoice)
const isExpeditionPhase = (value: unknown): value is FloorEscalation['phase'] => oneOf(value, ['survey', 'pressure', 'counterroute', 'climax'])
const isFloorEscalation = (value: unknown): value is FloorEscalation => isRecord(value) && isString(value.arcId) && isExpeditionPhase(value.phase) && ['topology', 'landmark', 'encounter', 'ecology', 'promise', 'payoff'].every(key => isString(value[key])) && isNonNegativeInteger(value.encounterOffset) && Array.isArray(value.carried) && value.carried.every(isExpeditionPhase)
const isAreaArcState = (value: unknown): value is AreaArcState => isRecord(value) && oneOf(value.biome, BIOMES) && isString(value.arcId) && Array.isArray(value.clearedPhases) && value.clearedPhases.every(isExpeditionPhase)
const isSecretRewardProfile = (value: unknown): boolean => isRecord(value) && oneOf(value.kind, ['kit-choice', 'lore-relic', 'companion-lead', 'shortcut-access', 'high-value-resource']) && isString(value.label) && isNonNegativeInteger(value.value) && value.value > 0 && value.cap === 1 && value.duplicateRule === 'once-per-run'
const isSecretRiskProfile = (value: unknown): boolean => isRecord(value) && oneOf(value.kind, ['ambush', 'terrain-hazard', 'tool-cooldown', 'route-isolation', 'resource-opportunity-cost']) && isString(value.label) && isString(value.detail)
const isSecretRoom = (value: unknown): value is SecretRoom => isRecord(value) && value.version === 1 && isString(value.id) && isString(value.sourceId) && oneOf(value.kind, ['hidden-room', 'side-pocket']) && isPoint(value.approach) && Array.isArray(value.entries) && value.entries.length > 0 && value.entries.every(isPoint) && Array.isArray(value.chamber) && value.chamber.length > 0 && value.chamber.every(isPoint) && oneOf(value.entryCondition, ['sealed-breakwall', 'anchored-rope']) && isString(value.discoveryClue) && oneOf(value.clueChannel, ['sight', 'sound', 'prop', 'terrain', 'ritual']) && (value.discovery === undefined || (isRecord(value.discovery) && oneOf(value.discovery.channel, ['sight', 'sound', 'prop', 'terrain', 'ritual']) && isNonNegativeInteger(value.discovery.turn))) && isSecretRewardProfile(value.rewardProfile) && isSecretRiskProfile(value.riskProfile) && (value.resolution === undefined || (isRecord(value.resolution) && isNonNegativeInteger(value.resolution.turn) && oneOf(value.resolution.rewardKind, ['kit-choice', 'lore-relic', 'companion-lead', 'shortcut-access', 'high-value-resource']) && isNonNegativeInteger(value.resolution.rewardValue) && value.resolution.rewardValue > 0 && oneOf(value.resolution.riskKind, ['ambush', 'terrain-hazard', 'tool-cooldown', 'route-isolation', 'resource-opportunity-cost']))) && oneOf(value.accessMethod, ['breach', 'climb']) && oneOf(value.rewardClass, ['supplies', 'ritual', 'shortcut']) && oneOf(value.risk, ['dust', 'undertow', 'ward', 'smoke', 'fall', 'spirits', 'cold']) && value.safeFallback === true
const isSecretRoute = (value: unknown): value is SecretRoute => isRecord(value) && value.version === 1 && isString(value.id) && isString(value.roomId) && oneOf(value.kind, ['concealed-passage', 'rare-transition']) && isPoint(value.from) && isPoint(value.entry) && oneOf(value.entryCondition, ['sealed-breakwall', 'anchored-rope']) && isString(value.discoveryClue) && oneOf(value.accessMethod, ['breach', 'climb']) && oneOf(value.rewardClass, ['supplies', 'ritual', 'shortcut']) && oneOf(value.risk, ['dust', 'undertow', 'ward', 'smoke', 'fall', 'spirits', 'cold']) && value.safeFallback === true && (value.destination === undefined || (isRecord(value.destination) && oneOf(value.destination.biome, BIOMES) && isNonNegativeInteger(value.destination.floor) && value.destination.floor < 4)) && (value.direction === undefined || oneOf(value.direction, ['one-way', 'two-way'])) && (value.arrival === undefined || value.arrival === 'floor-start') && (value.returnSemantics === undefined || oneOf(value.returnSemantics, ['no-return', 'return-link']))
const isObjective = (value: unknown): boolean => isRecord(value) && isString(value.id) && oneOf(value.kind, ['recoverSupplies', 'rescueScout', 'invokeAltar', 'defeatGuardian']) && oneOf(value.status, ['active', 'complete']) && isString(value.label)
const isRescuedNpc = (value: unknown): value is RescuedNpc => isRecord(value) && isString(value.id) && isString(value.name) && oneOf(value.biome, BIOMES) && isNumber(value.floor)
const isCompanion = (value: unknown): value is Companion => isRecord(value) && value.version === 1 && isString(value.id) && isString(value.templateId) && isString(value.name) && oneOf(value.role, ['guard', 'scout', 'pathmaker', 'ritualist']) && isRecord(value.recruitment) && value.recruitment.kind === 'rescue' && isString(value.recruitment.rescueId) && oneOf(value.recruitment.biome, BIOMES) && isNonNegativeInteger(value.recruitment.floor) && oneOf(value.rosterStatus, ['lead', 'benched', 'active', 'lost']) && oneOf(value.controlMode, ['autonomous', 'direct']) && oneOf(value.injury, ['healthy', 'injured', 'recovering']) && (value.recoveryFloors === undefined || value.injury === 'recovering' && isNonNegativeInteger(value.recoveryFloors)) && isRecord(value.abilityState) && isRecord(value.abilityState.cooldowns) && Object.values(value.abilityState.cooldowns).every(isNonNegativeInteger) && Array.isArray(value.abilityState.retired) && value.abilityState.retired.every(isString) && isRecord(value.toolState) && (value.toolState.equipped === undefined || isTraversalTool(value.toolState.equipped)) && isNonNegativeInteger(value.toolState.cooldown) && typeof value.toolState.retired === 'boolean' && typeof value.permanentlyLost === 'boolean'
const isCarryoverDiagnostic = (value: unknown): value is CampaignCarryoverDiagnostic => {
  if (!isRecord(value) || value.version !== 1 || !oneOf(value.fromTier, ['base', 'ngPlus', 'ngPlusPlus']) || !oneOf(value.toTier, ['base', 'ngPlus', 'ngPlusPlus']) || !isRecord(value.inventory) || !isRecord(value.roster)) return false
  const inventory = value.inventory
  const roster = value.roster
  return ['before', 'after', 'added', 'removed'].every(key => Array.isArray(inventory[key]) && inventory[key].every(isItemId)) && Array.isArray(roster.before) && roster.before.every(isCompanion) && Array.isArray(roster.after) && roster.after.every(isCompanion) && ['added', 'removed', 'changed'].every(key => Array.isArray(roster[key]) && roster[key].every(isString))
}
const isLineageEvent = (value: unknown): value is LineageEvent => isRecord(value) && isString(value.id) && value.kind === 'npcSacrifice' && isString(value.npcId) && isString(value.npcName) && oneOf(value.biome, BIOMES) && isNumber(value.floor) && isString(value.gateId) && isNumber(value.seed)
const isLegacyRecord = (value: unknown): value is LegacyRecord => isRecord(value) && isString(value.id) && isString(value.heirName) && oneOf(value.biome, BIOMES) && isNumber(value.floor) && isNumber(value.seed)
const isCompanionControlModeEvent = (value: unknown): value is CompanionControlModeEvent => isRecord(value) && isNonNegativeInteger(value.sequence) && isCompanionControlMode(value.mode) && oneOf(value.source, ['creation', 'migration', 'lodge'])
const copyLegacyRecords = (records: readonly LegacyRecord[]): LegacyRecord[] => records.slice(-12).map(record => ({ id: record.id, heirName: record.heirName, biome: record.biome, floor: record.floor, seed: record.seed }))
const isEncyclopedia = (value: unknown): value is EncyclopediaState => isRecord(value) && Array.isArray(value.enemies) && value.enemies.every(isString) && Array.isArray(value.telegraphs) && value.telegraphs.every(isString) && Array.isArray(value.tags) && value.tags.every(isString) && Array.isArray(value.gates) && value.gates.every(isString) && Array.isArray(value.legacyRecords) && value.legacyRecords.every(isLegacyRecord)
const isRunMetricSample = (value: unknown): value is RunMetricSample => isRecord(value) && isNumber(value.turn) && isNumber(value.floor) && isNumber(value.health) && isNumber(value.focus) && isNumber(value.gold) && isNumber(value.bombs) && isNumber(value.ropes) && isNumber(value.kills) && isNumber(value.damageDealt) && isNumber(value.damageTaken)
const isRunFloorMetrics = (value: unknown): value is RunFloorMetrics => isRecord(value) && isNumber(value.floor) && isNumber(value.turns) && isNumber(value.kills) && isNumber(value.damageDealt) && isNumber(value.damageTaken) && isNumber(value.goldGained) && isNumber(value.xpGained) && isNumber(value.pickups) && isNumber(value.bombsUsed) && isNumber(value.ropesUsed) && (value.secretValue === undefined || isNumber(value.secretValue))
const isRunActions = (value: unknown): boolean => isRecord(value) && ['moves', 'attacks', 'casts', 'pickups', 'bombs', 'ropes', 'rests'].every(key => isNumber(value[key]))
const isCounterMap = (value: unknown): boolean => isRecord(value) && Object.values(value).every(isNumber)
const isOptionalContentTelemetry = (value: unknown): value is OptionalContentTelemetry => isRecord(value) && ['generated', 'discovered', 'used', 'failed'].every(key => isCounterMap(value[key]))
const isInteractionTelemetry = (value: unknown): value is InteractionTelemetry => isRecord(value) && ['terrainToolUses', 'rejectedInteractions', 'routeFailures'].every(key => isCounterMap(value[key]))
const isRunTelemetry = (value: unknown): value is RunTelemetry => isRecord(value) && isNumber(value.turns) && isRunActions(value.actions) && isNumber(value.kills) && isNumber(value.damageDealt) && isNumber(value.damageTaken) && isNumber(value.goldGained) && (value.goldSpent === undefined || isNumber(value.goldSpent)) && (value.secretValue === undefined || isNumber(value.secretValue)) && isNumber(value.xpGained) && isNumber(value.pickups) && isNumber(value.bombsUsed) && isNumber(value.ropesUsed) && ['itemsUsed', 'boonPicks', 'boonAugments', 'relicPicks', 'purchases', 'enemyKills', 'eventOutcomes', 'deathCauses', 'terrainInteractions', 'bossPhases'].every(key => value[key] === undefined || isCounterMap(value[key])) && (value.optionalContent === undefined || isOptionalContentTelemetry(value.optionalContent)) && (value.interactions === undefined || isInteractionTelemetry(value.interactions)) && Array.isArray(value.samples) && value.samples.every(isRunMetricSample) && Array.isArray(value.floors) && value.floors.every(isRunFloorMetrics)
const isRunAnalysis = (value: unknown): value is RunAnalysis => isRecord(value) && isNumber(value.seed) && oneOf(value.biome, BIOMES) && isNumber(value.floor) && oneOf(value.outcome, ['lost', 'complete', 'suspended']) && isString(value.date) && isRunTelemetry(value.metrics)
const isTelegraph = (value: unknown): value is Telegraph => isRecord(value) && isString(value.id) && isString(value.sourceId) && isString(value.actionId) && Array.isArray(value.cells) && value.cells.every(isPoint) && oneOf(value.danger, ['minor', 'major']) && isNumber(value.resolveTurn) && (value.collision === undefined || (isRecord(value.collision) && isPoint(value.collision.point) && isString(value.collision.by))) && (value.cover === undefined || typeof value.cover === 'boolean')
const isMilestone = (value: unknown): value is FloorMilestone => isRecord(value) && isString(value.id) && oneOf(value.kind, ['waycache', 'boon', 'augment', 'relic']) && isPoint(value) && typeof value.discovered === 'boolean' && typeof value.claimed === 'boolean' && (value.rewardKey === undefined || oneOf(value.rewardKey, ['waycache', 'boon-teach', 'boon-test', 'boon-payoff']))
const isTransientTerrain = (value: unknown): value is TransientTerrain => isRecord(value) && isPoint(value) && oneOf(value.original, TILE_KINDS) && (value.originalFlow === undefined || (isRecord(value.originalFlow) && oneOf(value.originalFlow.direction, ['nw', 'n', 'ne', 'w', 'e', 'sw', 's', 'se']) && (value.originalFlow.hazard === undefined || oneOf(value.originalFlow.hazard, ['undertow', 'squall'])))) && isNonNegativeInteger(value.expiresAt)
const isEcologyEvent = (value: unknown): boolean => isRecord(value) && isString(value.id) && oneOf(value.kind, ECOLOGY_EVENT_KINDS) && isString(value.source) && isPoint(value.target) && (value.route === undefined || isString(value.route)) && (value.node === undefined || isString(value.node)) && isString(value.warning) && isNonNegativeInteger(value.startsAt) && value.startsAt > 0 && isNonNegativeInteger(value.duration) && value.duration > 0 && Array.isArray(value.responses) && value.responses.every(isString) && isString(value.cleanup) && oneOf(value.state, ['waiting', 'active', 'resolved']) && oneOf(value.original, TILE_KINDS) && (value.originalFlow === undefined || isFlow(value.originalFlow)) && oneOf(value.effect, TILE_KINDS) && (value.effectFlow === undefined || isFlow(value.effectFlow)) && (value.warned === undefined || typeof value.warned === 'boolean')
const isClimbLink = (value: unknown): value is ClimbLink => isRecord(value) && isString(value.id) && isPoint(value.lower) && isPoint(value.upper) && typeof value.anchored === 'boolean'
const isDifficultyPackage = (value: unknown): boolean => isRecord(value) && value.version === 1 && oneOf(value.id, ['base-v1', 'ng-plus-v1', 'ng-plus-plus-v1']) && oneOf(value.tier, ['base', 'ngPlus', 'ngPlusPlus']) && isString(value.name) && isString(value.rationale)
const isDifficulty = (value: unknown): value is DifficultyContext => isRecord(value) && isNonNegativeInteger(value.routePosition) && isNonNegativeInteger(value.threat) && isNumber(value.healthMultiplier) && isNonNegativeInteger(value.attackBonus) && isNonNegativeInteger(value.defenseBonus) && isNonNegativeInteger(value.eliteChance) && isNonNegativeInteger(value.guardianPattern) && (value.hazardMultiplier === undefined || isNumber(value.hazardMultiplier)) && (value.rewardMultiplier === undefined || isNumber(value.rewardMultiplier)) && (value.campaignTier === undefined || oneOf(value.campaignTier, ['base', 'ngPlus', 'ngPlusPlus'])) && (value.difficultyPackage === undefined || isDifficultyPackage(value.difficultyPackage))
const isFloorBase = (value: unknown): value is Omit<Floor, 'milestones' | 'transientTerrain'> & Partial<Pick<Floor, 'milestones' | 'transientTerrain'>> => isRecord(value) && isInteger(value.index) && oneOf(value.biome, BIOMES) && isNumber(value.seed) && isNonNegativeInteger(value.width) && value.width > 0 && isNonNegativeInteger(value.height) && value.height > 0 && isString(value.layoutId) && Array.isArray(value.tiles) && value.tiles.length === value.width * value.height && value.tiles.every(isTile) && Array.isArray(value.actors) && value.actors.every(isActor) && Array.isArray(value.items) && value.items.every(isGroundItem) && Array.isArray(value.props) && value.props.every(isProp) && (value.encounters === undefined || (Array.isArray(value.encounters) && value.encounters.every(isFloorEncounter))) && isPoint(value.start) && isPoint(value.exit) && typeof value.guardianDefeated === 'boolean' && isObjective(value.objective) && (value.secretRooms === undefined || (Array.isArray(value.secretRooms) && value.secretRooms.every(isSecretRoom))) && (value.secretRoutes === undefined || (Array.isArray(value.secretRoutes) && value.secretRoutes.every(isSecretRoute))) && (value.rewardOffers === undefined || (Array.isArray(value.rewardOffers) && value.rewardOffers.every(isRewardOffer))) && (value.escalation === undefined || isFloorEscalation(value.escalation)) && (value.ecology === undefined || (Array.isArray(value.ecology) && value.ecology.every(isEcologyEvent))) && (value.telegraphs === undefined || (Array.isArray(value.telegraphs) && value.telegraphs.every(isTelegraph))) && (value.puzzleIds === undefined || (Array.isArray(value.puzzleIds) && value.puzzleIds.every(isString))) && (value.climbLinks === undefined || (Array.isArray(value.climbLinks) && value.climbLinks.every(isClimbLink))) && (value.difficulty === undefined || isDifficulty(value.difficulty))
const isFloor = (value: unknown): value is Floor => isFloorBase(value) && Array.isArray(value.milestones) && value.milestones.every(isMilestone) && (value.transientTerrain === undefined || (Array.isArray(value.transientTerrain) && value.transientTerrain.every(isTransientTerrain)))
const isShortcutReturn = (value: unknown): value is ShortcutReturn => isRecord(value) && value.version === 1 && isString(value.routeId) && isFloor(value.floor) && isNonNegativeInteger(value.areaFloor) && value.areaFloor < 4 && isPoint(value.arrival)
const isCampaignCycle = (value: unknown): value is CampaignCycle => isRecord(value) && value.version === 1 && (value.currentTier === 'base' || value.currentTier === 'ngPlus' || value.currentTier === 'ngPlusPlus') && Array.isArray(value.completedTiers) && value.completedTiers.every(tier => tier === 'base' || tier === 'ngPlus' || tier === 'ngPlusPlus') && Array.isArray(value.events) && value.events.every(event => isRecord(event) && isNonNegativeInteger(event.sequence) && (event.tier === 'base' || event.tier === 'ngPlus' || event.tier === 'ngPlusPlus') && (event.kind === 'entered' || event.kind === 'victory')) && typeof value.completedCap === 'boolean' && campaignCycleErrors(value as unknown as CampaignCycle).length === 0
const isTraversalTool = (value: unknown): value is TraversalToolId => oneOf(value, ['stoneWedge', 'reedwing', 'cordAnchor', 'ashwayRites', 'antlerPrybar', 'stoneAdze', 'resinFireBasket', 'woodenLeverRoller'])
const isRelic = (value: unknown): value is RelicId => oneOf(value, ['ashCircuit', 'markbreakerSeal', 'cairnCoil', 'tideFetter', 'prismRelay', 'winterSeal'])
const isOath = (value: unknown): value is OathState => isRecord(value) && oneOf(value.id, ['noHealing', 'noBombs', 'noCharms']) && isNonNegativeInteger(value.remainingFloors)
const isCurse = (value: unknown): value is CurseState => isRecord(value) && isItemId(value.itemId) && isString(value.name) && isString(value.condition) && isNonNegativeInteger(value.remainingEncounters) && typeof value.lethal === 'boolean' && (value.failed === undefined || typeof value.failed === 'boolean')
const isHero = (value: unknown): value is Hero => isRecord(value) && isInteger(value.x) && isInteger(value.y) && isNonNegativeInteger(value.health) && isInteger(value.maxHealth) && value.maxHealth > 0 && value.health <= value.maxHealth && isNonNegativeInteger(value.focus) && isInteger(value.maxFocus) && value.maxFocus > 0 && value.focus <= value.maxFocus && isNonNegativeInteger(value.gold) && isNonNegativeInteger(value.bombs) && isNonNegativeInteger(value.ropes) && isNonNegativeInteger(value.keys) && isNonNegativeInteger(value.xp) && isInteger(value.level) && value.level > 0 && isRecord(value.stats) && isInteger(value.stats.strength) && isInteger(value.stats.agility) && isInteger(value.stats.vitality) && isInteger(value.stats.intellect) && Array.isArray(value.skills) && value.skills.every(isSkillId) && Array.isArray(value.inventory) && value.inventory.every(isItemId) && isRecord(value.equipment) && Object.values(value.equipment).every(item => item === undefined || isItemId(item)) && isString(value.name) && value.name.trim().length > 0 && isCourierOrigin(value.origin) && isCourierCalling(value.calling) && isDeathMode(value.deathMode) && (value.lastUnequipped === undefined || isItemId(value.lastUnequipped)) && (value.conditions === undefined || (Array.isArray(value.conditions) && value.conditions.every(isCondition))) && (value.cooldowns === undefined || (isRecord(value.cooldowns) && Object.values(value.cooldowns).every(isNonNegativeInteger))) && (value.traversalTools === undefined || (Array.isArray(value.traversalTools) && value.traversalTools.length <= 2 && value.traversalTools.every(isTraversalTool))) && (value.relics === undefined || (Array.isArray(value.relics) && value.relics.length <= 3 && value.relics.every(isRelic))) && (value.relicCharges === undefined || (isRecord(value.relicCharges) && Object.entries(value.relicCharges).every(([id, value]) => isRelic(id) && isNonNegativeInteger(value)))) && (value.boons === undefined || (isRecord(value.boons) && Object.values(value.boons).every(isNonNegativeInteger))) && (value.boonEvolutions === undefined || (isRecord(value.boonEvolutions) && Object.values(value.boonEvolutions).every(isNonNegativeInteger))) && (value.safePositions === undefined || (Array.isArray(value.safePositions) && value.safePositions.every(isPoint))) && (value.oaths === undefined || (Array.isArray(value.oaths) && value.oaths.every(isOath))) && (value.curse === undefined || isCurse(value.curse))

const isModal = (value: unknown): value is Modal | undefined => {
  if (value === undefined) return true
  if (!isRecord(value)) return false
  if (value.kind === 'help' || value.kind === 'readout' || value.kind === 'skills' || value.kind === 'pause' || value.kind === 'trailcraft') return true
  if (value.kind === 'encyclopedia') return oneOf(value.section, ['enemies', 'telegraphs', 'tags', 'gates', 'legacy']) && (value.page === undefined || isNumber(value.page))
  if (value.kind === 'settings') return (value.page === undefined || isNumber(value.page)) && (value.awaiting === undefined || oneOf(value.awaiting, ['northwest', 'north', 'northeast', 'west', 'east', 'southwest', 'south', 'southeast', 'wait', 'help', 'encyclopedia', 'settings', 'use', 'drop', 'throw', 'equip', 'skills', 'bomb', 'rope', 'get', 'operate', 'descend', 'swap', 'script']))
  if (value.kind === 'inventory') return oneOf(value.mode, ['use', 'drop', 'throw', 'equip'])
  if (value.kind === 'shop') return isString(value.merchantId)
  if (value.kind === 'gate') return isString(value.gateId) && (value.choice === undefined || isNumber(value.choice)) && (value.offeringId === undefined || isString(value.offeringId)) && (value.confirming === undefined || typeof value.confirming === 'boolean')
  if (value.kind === 'companionCommand') return Array.isArray(value.companionIds) && value.companionIds.every(isString) && isNonNegativeInteger(value.index) && value.index < value.companionIds.length
  if (value.kind === 'boon') return isString(value.milestoneId)
  if (value.kind === 'augment') return isString(value.milestoneId) && (value.mode === undefined || oneOf(value.mode, ['evolve', 'reforge', 'transmute'])) && (value.selected === undefined || (Array.isArray(value.selected) && value.selected.every(isString)))
  if (value.kind === 'tool') return isString(value.milestoneId) && (value.replace === undefined || isNonNegativeInteger(value.replace))
  if (value.kind === 'relic') return isString(value.milestoneId) && (value.replace === undefined || isNonNegativeInteger(value.replace))
  if (value.kind === 'encounter') return isString(value.encounterId)
  if (value.kind === 'tools') return true
  return value.kind === 'target' && oneOf(value.action, ['throw', 'spell', 'bomb', 'drill', 'glide', 'grapple', 'bridge', 'dash', 'winch', 'stoneWedge', 'reedwing', 'cordAnchor', 'ashwayRites', 'antlerPrybar', 'stoneAdze', 'resinFireBasket', 'woodenLeverRoller']) && (value.item === undefined || isItemId(value.item)) && (value.tool === undefined || isTraversalTool(value.tool)) && (value.overdrive === undefined || typeof value.overdrive === 'boolean')
}

const isRunRecord = (value: unknown): value is RunRecord => isRecord(value) && isNumber(value.version) && isNumber(value.seed) && isFloor(value.floor) && isHero(value.hero) && Array.isArray(value.messages) && value.messages.every(isString) && oneOf(value.status, ['title', 'playing', 'dead', 'victory']) && isModal(value.modal) && isNumber(value.turn) && (value.area === undefined || oneOf(value.area, BIOMES)) && (value.areaFloor === undefined || isNumber(value.areaFloor)) && (value.areaArc === undefined || isAreaArcState(value.areaArc)) && (value.areaOrder === undefined || isAreaOrder(value.areaOrder)) && (value.gateDestination === undefined || oneOf(value.gateDestination, BIOMES)) && (value.shortcutReturn === undefined || isShortcutReturn(value.shortcutReturn)) && (value.campaignCycle === undefined || isCampaignCycle(value.campaignCycle)) && (value.rescuedNpcs === undefined || (Array.isArray(value.rescuedNpcs) && value.rescuedNpcs.every(isRescuedNpc))) && (value.companions === undefined || (Array.isArray(value.companions) && value.companions.every(isCompanion))) && (value.companionDeathMode === undefined || isCompanionDeathMode(value.companionDeathMode)) && (value.lineageEvents === undefined || (Array.isArray(value.lineageEvents) && value.lineageEvents.every(isLineageEvent))) && (value.alignment === undefined || isAlignment(value.alignment)) && (value.reputation === undefined || isSocialReputation(value.reputation)) && (value.encyclopedia === undefined || isEncyclopedia(value.encyclopedia)) && (value.telemetry === undefined || isRunTelemetry(value.telemetry))
const isRunState = (value: unknown): value is RunState => isRunRecord(value) && (value.version === 4 || value.version === 5)
const isAlignment = (value: unknown): value is Record<Alignment, number> => isRecord(value) && isNonNegativeInteger(value.kami) && isNonNegativeInteger(value.villagePact)
const isSocialReputation = (value: unknown): value is SocialReputation => isRecord(value) && isInteger(value.trailfolk) && isInteger(value.kami)
const isCampaignRoute = (value: unknown): value is CampaignRouteRecord => isRecord(value) && (value.version === 1 || value.version === 2 || value.version === 3 || value.version === 4 || value.version === 5) && Array.isArray(value.completedAreas) && value.completedAreas.every(area => oneOf(area, BIOMES)) && Array.isArray(value.unlockedAreas) && value.unlockedAreas.every(area => oneOf(area, BIOMES)) && (value.reputation === undefined || isSocialReputation(value.reputation)) && oneOf(value.selectedBiome, BIOMES) && value.unlockedAreas.includes(value.selectedBiome) && (value.version === 3 ? Array.isArray(value.areaOrder) && isLegacyCampaignAreaOrder(value.areaOrder as Biome[]) : value.version === 4 || value.version === 5 ? Array.isArray(value.areaOrder) && isCampaignAreaOrder(value.areaOrder as Biome[]) : true) && (value.alignment === undefined || isAlignment(value.alignment)) && (value.rescuedNpcs === undefined || (Array.isArray(value.rescuedNpcs) && value.rescuedNpcs.every(isRescuedNpc))) && (value.companions === undefined || (Array.isArray(value.companions) && value.companions.every(isCompanion))) && (value.companionControlMode === undefined || isCompanionControlMode(value.companionControlMode)) && (value.companionControlHistory === undefined || (Array.isArray(value.companionControlHistory) && value.companionControlHistory.every(isCompanionControlModeEvent))) && (value.carryoverDiagnostics === undefined || (Array.isArray(value.carryoverDiagnostics) && value.carryoverDiagnostics.every(isCarryoverDiagnostic))) && (value.lineageEvents === undefined || (Array.isArray(value.lineageEvents) && value.lineageEvents.every(isLineageEvent))) && (value.legacyRecords === undefined || (Array.isArray(value.legacyRecords) && value.legacyRecords.every(isLegacyRecord))) && (value.cycle === undefined || isCampaignCycle(value.cycle))

type LegacyHero = Omit<Hero, 'name' | 'origin' | 'calling' | 'deathMode'>
type LegacyFloor = Omit<Floor, 'width' | 'height' | 'layoutId' | 'props' | 'objective' | 'milestones' | 'ecology' | 'transientTerrain'> & { props?: Floor['props']; objective?: Floor['objective']; milestones?: Floor['milestones']; ecology?: Floor['ecology']; transientTerrain?: Floor['transientTerrain'] }
type LegacyRunState = Omit<RunState, 'version' | 'floor' | 'hero'> & { version: 1 | 2 | 3 | 4 | 5; floor: LegacyFloor; hero: LegacyHero | Hero }

const isLegacyHero = (value: unknown): value is LegacyHero => isRecord(value) && isInteger(value.x) && isInteger(value.y) && isNonNegativeInteger(value.health) && isInteger(value.maxHealth) && value.maxHealth > 0 && value.health <= value.maxHealth && isNonNegativeInteger(value.focus) && isInteger(value.maxFocus) && value.maxFocus > 0 && value.focus <= value.maxFocus && isNonNegativeInteger(value.gold) && isNonNegativeInteger(value.bombs) && isNonNegativeInteger(value.ropes) && isNonNegativeInteger(value.keys) && isNonNegativeInteger(value.xp) && isInteger(value.level) && value.level > 0 && isRecord(value.stats) && isInteger(value.stats.strength) && isInteger(value.stats.agility) && isInteger(value.stats.vitality) && isInteger(value.stats.intellect) && Array.isArray(value.skills) && value.skills.every(isSkillId) && Array.isArray(value.inventory) && value.inventory.every(isItemId) && isRecord(value.equipment) && Object.values(value.equipment).every(item => item === undefined || isItemId(item)) && (value.lastUnequipped === undefined || isItemId(value.lastUnequipped)) && (value.conditions === undefined || (Array.isArray(value.conditions) && value.conditions.every(isCondition))) && (value.cooldowns === undefined || (isRecord(value.cooldowns) && Object.values(value.cooldowns).every(isNonNegativeInteger)))
const isLegacyFloor = (value: unknown): value is LegacyFloor => isRecord(value) && isInteger(value.index) && oneOf(value.biome, BIOMES) && isNumber(value.seed) && Array.isArray(value.tiles) && value.tiles.length === MAP_WIDTH * MAP_HEIGHT && value.tiles.every(isTile) && Array.isArray(value.actors) && value.actors.every(isActor) && Array.isArray(value.items) && value.items.every(isGroundItem) && (value.props === undefined || (Array.isArray(value.props) && value.props.every(isProp))) && isPoint(value.start) && isPoint(value.exit) && typeof value.guardianDefeated === 'boolean' && (value.objective === undefined || isObjective(value.objective)) && (value.telegraphs === undefined || (Array.isArray(value.telegraphs) && value.telegraphs.every(isTelegraph))) && (value.puzzleIds === undefined || (Array.isArray(value.puzzleIds) && value.puzzleIds.every(isString)))
const isLegacyRunState = (value: unknown): value is LegacyRunState => isRecord(value) && (value.version === 1 || value.version === 2 || value.version === 3 || value.version === 4 || value.version === 5) && isNumber(value.seed) && isLegacyFloor(value.floor) && (isLegacyHero(value.hero) || isHero(value.hero)) && Array.isArray(value.messages) && value.messages.every(isString) && oneOf(value.status, ['title', 'playing', 'dead', 'victory']) && isModal(value.modal) && isNonNegativeInteger(value.turn) && (value.area === undefined || oneOf(value.area, BIOMES)) && (value.areaFloor === undefined || isInteger(value.areaFloor))
const migrateLegacyRun = (legacy: LegacyRunState): RunState => ({
  version: 5,
  seed: legacy.seed,
  floor: { ...legacy.floor, width: MAP_WIDTH, height: MAP_HEIGHT, layoutId: 'legacy', props: legacy.floor.props ?? [], objective: legacy.floor.objective ?? objectiveForFloor(legacy.floor.index), milestones: legacy.floor.milestones ?? [], ecology: legacy.floor.ecology, transientTerrain: legacy.floor.transientTerrain },
  hero: { ...legacy.hero, name: 'name' in legacy.hero ? legacy.hero.name : 'Existing Courier', origin: 'origin' in legacy.hero ? legacy.hero.origin : 'mineborn', calling: 'calling' in legacy.hero ? legacy.hero.calling : 'trailguard', deathMode: 'deathMode' in legacy.hero ? legacy.hero.deathMode : 'checkpoint', conditions: legacy.hero.conditions ?? [], cooldowns: legacy.hero.cooldowns ?? {}, traversalTools: legacy.hero.traversalTools ?? [], relics: [], relicCharges: {}, boons: legacy.hero.boons ?? {}, boonEvolutions: legacy.hero.boonEvolutions ?? {}, safePositions: legacy.hero.safePositions ?? [] },
  messages: [...legacy.messages],
  status: legacy.status,
  turn: legacy.turn,
  area: legacy.area ?? legacy.floor.biome,
  areaFloor: legacy.areaFloor ?? legacy.floor.index % 4
})

const pointOnMap = (floor: Floor, point: Point): boolean => isInteger(point.x) && isInteger(point.y) && inFloorBounds(floor, point.x, point.y)
const secretClueChannelFor = (sourceId: string): SecretClueChannel => sourceId.startsWith('wilds-cave:') || sourceId.startsWith('cliff-alcove:') ? 'sight' : sourceId.startsWith('cavern-hidden:') || sourceId.startsWith('furnace-service:') ? 'sound' : sourceId.startsWith('burial-crypt:') ? 'prop' : sourceId.startsWith('ritual-hidden:') ? 'ritual' : 'terrain'
const migrateSecretMetadata = (value: unknown): unknown => {
  if (!isRecord(value) || !isRecord(value.floor) || !Array.isArray(value.floor.secretRooms)) return value
  return { ...value, floor: { ...value.floor, secretRooms: value.floor.secretRooms.map(room => {
    if (!isRecord(room) || !isString(room.sourceId)) return room
    const rules = room.rewardProfile === undefined || room.riskProfile === undefined ? secretRulesForSourceId(room.sourceId) : undefined
    return { ...room, ...(room.clueChannel === undefined ? { clueChannel: secretClueChannelFor(room.sourceId) } : {}), ...(rules ?? {}) }
  }) } }
}
const validPersistedRun = (run: RunState): boolean => {
  const floor = run.floor
  if (!isNonNegativeInteger(run.turn) || floor.index < 0 || floor.index >= FLOOR_COUNT) return false
  if (!pointOnMap(floor, run.hero) || !pointOnMap(floor, floor.start) || !pointOnMap(floor, floor.exit) || floor.tiles[floorIndex(floor, floor.exit.x, floor.exit.y)]?.kind !== 'exit') return false
  if (run.area !== undefined && run.area !== floor.biome) return false
  if (run.areaFloor !== undefined && (!isInteger(run.areaFloor) || run.areaFloor < 0 || run.areaFloor > 3)) return false
  if (run.shortcutReturn && !pointOnMap(run.shortcutReturn.floor, run.shortcutReturn.arrival)) return false
  if (floor.actors.some(actor => !pointOnMap(floor, actor)) || floor.items.some(item => !pointOnMap(floor, item)) || floor.props.some(prop => !pointOnMap(floor, prop) || (prop.effectCells?.some(point => !pointOnMap(floor, point)) ?? false)) || (floor.encounters?.some(encounter => !pointOnMap(floor, encounter)) ?? false) || (floor.secretRooms?.some(room => !pointOnMap(floor, room.approach) || room.entries.some(point => !pointOnMap(floor, point)) || room.chamber.some(point => !pointOnMap(floor, point))) ?? false) || (floor.secretRoutes?.some(route => !pointOnMap(floor, route.from) || !pointOnMap(floor, route.entry)) ?? false) || (floor.ecology?.some(ecology => !pointOnMap(floor, ecology.target)) ?? false)) return false
  return !(floor.telegraphs?.some(telegraph => telegraph.cells.some(point => !pointOnMap(floor, point)) || (telegraph.collision !== undefined && !pointOnMap(floor, telegraph.collision.point))) ?? false)
}

export const migrateRunRecord = (value: unknown): RunState | undefined => {
  const migrated = migrateSecretMetadata(value)
  const run = isRunState(migrated) ? { ...migrated } : isLegacyRunState(migrated) && migrated.version < 5 ? migrateLegacyRun(migrated) : undefined
  if (!run || !validPersistedRun(run)) return undefined
  if (run.encyclopedia) run.encyclopedia = { ...run.encyclopedia, legacyRecords: copyLegacyRecords(run.encyclopedia.legacyRecords) }
  const telemetry = run.telemetry ??= createRunTelemetry(run)
  telemetry.goldSpent ??= 0
  for (const key of ['itemsUsed', 'boonPicks', 'boonAugments', 'relicPicks', 'purchases', 'enemyKills', 'eventOutcomes', 'deathCauses', 'terrainInteractions', 'bossPhases'] as const) telemetry[key] ??= {}
  telemetry.optionalContent ??= { generated: {}, discovered: {}, used: {}, failed: {} }
  telemetry.interactions ??= { terrainToolUses: {}, rejectedInteractions: {}, routeFailures: {} }
  run.hero.relics ??= []
  run.hero.relicCharges ??= {}
  run.alignment ??= { kami: 0, villagePact: 0 }
  run.reputation ??= { trailfolk: 0, kami: 0 }
  run.areaOrder ??= [...DEFAULT_AREA_ORDER]
  run.campaignCycle = cloneCampaignCycle(run.campaignCycle ?? initialCampaignCycle())
  run.companions = cloneCompanions(run.companions ?? [])
  run.companionDeathMode ??= 'injury'
  return run
}

export const migrateCampaignRoute = (value: unknown): CampaignRouteState => {
  if (!isCampaignRoute(value)) return initialCampaignRoute()
  const rescuedNpcs = (value.rescuedNpcs ?? []).map(npc => ({ ...npc }))
  const companionControlMode = value.companionControlMode ?? 'autonomous'
  const companionControlHistory = value.companionControlHistory === undefined ? [{ sequence: 0, mode: 'autonomous' as const, source: 'migration' as const }] : value.companionControlHistory.map(event => ({ ...event }))
  const companions = (value.companions === undefined ? companionLeadsForRescues(rescuedNpcs) : cloneCompanions(value.companions, rescuedNpcs)).map(companion => ({ ...companion, controlMode: companionControlMode }))
  return { version: 5, areaOrder: value.version === 4 || value.version === 5 ? [...value.areaOrder!] : value.version === 3 ? [...value.areaOrder!] : [...LEGACY_AREA_ORDER], completedAreas: [...value.completedAreas], unlockedAreas: [...value.unlockedAreas], selectedBiome: value.selectedBiome, rescuedNpcs, companions, companionControlMode, companionControlHistory, carryoverDiagnostics: cloneCarryoverDiagnostics(value.carryoverDiagnostics ?? []), lineageEvents: (value.lineageEvents ?? []).map(event => ({ ...event })), legacyRecords: copyLegacyRecords(value.legacyRecords ?? []), alignment: { kami: value.alignment?.kami ?? 0, villagePact: value.alignment?.villagePact ?? 0 }, reputation: { trailfolk: value.reputation?.trailfolk ?? 0, kami: value.reputation?.kami ?? 0 }, cycle: value.cycle ? cloneCampaignCycle(value.cycle) : initialCampaignCycle() }
}

const database = (): Promise<IDBDatabase> => new Promise((resolve, reject) => {
  const request = indexedDB.open(DB, 1)
  request.onupgradeneeded = () => request.result.createObjectStore(STORE)
  request.onsuccess = () => resolve(request.result)
  request.onerror = () => reject(request.error)
})

const requestError = (error: DOMException | null, fallback: string): Error => error ?? new Error(fallback)
const get = async <T>(key: string): Promise<T | undefined> => {
  const db = await database()
  return new Promise((resolve, reject) => {
    const request = db.transaction(STORE).objectStore(STORE).get(key)
    request.onsuccess = () => resolve(request.result as T | undefined)
    request.onerror = () => reject(requestError(request.error, `failed to read ${key}`))
  })
}

const put = async <T>(key: string, value: T): Promise<void> => {
  const db = await database()
  await new Promise<void>((resolve, reject) => {
    const request = db.transaction(STORE, 'readwrite').objectStore(STORE).put(value, key)
    request.onsuccess = () => resolve()
    request.onerror = () => reject(requestError(request.error, `failed to write ${key}`))
  })
}

const remove = async (key: string): Promise<void> => {
  const db = await database()
  await new Promise<void>((resolve, reject) => {
    const request = db.transaction(STORE, 'readwrite').objectStore(STORE).delete(key)
    request.onsuccess = () => resolve()
    request.onerror = () => reject(requestError(request.error, `failed to delete ${key}`))
  })
}

export async function loadRun(): Promise<RunState | undefined> {
  const record = await get<unknown>(RUN)
  return migrateRunRecord(record)
}
export const saveRun = (state: RunState) => put(RUN, state)
export const deleteRun = () => remove(RUN)

const emptyRecords = (): Records => ({ bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] })
const migrateRecords = (value: unknown): Records => {
  if (!isRecord(value)) return emptyRecords()
  const runs = Array.isArray(value.runs) ? value.runs.filter(run => isRecord(run) && isNumber(run.seed) && isNumber(run.floor) && isNumber(run.score) && typeof run.won === 'boolean' && isString(run.date)).map(run => ({ seed: run.seed, floor: run.floor, score: run.score, won: run.won, date: run.date })) : []
  const analyses = Array.isArray(value.analyses) ? value.analyses.filter(isRunAnalysis).slice(0, 20) : []
  return { bestDepth: isNumber(value.bestDepth) ? value.bestDepth : 0, wins: isNumber(value.wins) ? value.wins : 0, deaths: isNumber(value.deaths) ? value.deaths : 0, runs, analyses }
}
export const loadRecords = async (): Promise<Records> => migrateRecords(await get<unknown>(RECORDS))
export const saveRecords = (records: Records) => put(RECORDS, records)
export const loadCampaignRoute = async (): Promise<CampaignRouteState> => migrateCampaignRoute(await get<unknown>(CAMPAIGN_ROUTE))
export const saveCampaignRoute = (route: CampaignRouteState) => put(CAMPAIGN_ROUTE, migrateCampaignRoute(route))

interface CourierIndex { version: 1; ids: string[]; selectedId?: string }
const emptyCourierIndex = (): CourierIndex => ({ version: 1, ids: [] })
const isCourierIndex = (value: unknown): value is CourierIndex => isRecord(value) && value.version === 1 && Array.isArray(value.ids) && value.ids.every(isString) && (value.selectedId === undefined || isString(value.selectedId))
const courierKey = (id: string): string => `${COURIER_PREFIX}${id}`
const courierWrites = new SerialWriteQueue()

const writeTransaction = async (operation: (store: IDBObjectStore, fail: (error: Error) => void) => void): Promise<void> => {
  const db = await database()
  await new Promise<void>((resolve, reject) => {
    const transaction = db.transaction(STORE, 'readwrite')
    let failure: Error | undefined
    const fail = (error: Error): void => {
      failure ??= error
      try { transaction.abort() } catch { }
    }
    transaction.oncomplete = () => resolve()
    transaction.onerror = () => reject(failure ?? requestError(transaction.error, 'courier persistence transaction failed'))
    transaction.onabort = () => reject(failure ?? requestError(transaction.error, 'courier persistence transaction aborted'))
    try { operation(transaction.objectStore(STORE), fail) } catch (caught) { fail(caught instanceof Error ? caught : new Error(String(caught))) }
  })
}

const putInTransaction = (store: IDBObjectStore, key: string, value: unknown, fail: (error: Error) => void): void => {
  const request = store.put(value, key)
  request.onerror = () => fail(requestError(request.error, `failed to write ${key}`))
}

const mutateCourierIndex = (operation: (store: IDBObjectStore, index: CourierIndex, fail: (error: Error) => void) => void): Promise<void> => writeTransaction((store, fail) => {
  const request = store.get(COURIER_INDEX)
  request.onerror = () => fail(requestError(request.error, 'failed to read courier index'))
  request.onsuccess = () => {
    try { operation(store, isCourierIndex(request.result) ? request.result : emptyCourierIndex(), fail) }
    catch (caught) { fail(caught instanceof Error ? caught : new Error(String(caught))) }
  }
})
const courierIdentity = (value: unknown): CourierSave['identity'] | undefined => {
  if (!isRecord(value) || !isString(value.id) || !isString(value.name) || !isCourierOrigin(value.origin) || !isCourierCalling(value.calling) || !isDeathMode(value.deathMode) || !isString(value.createdAt)) return undefined
  return { id: value.id, name: value.name, origin: value.origin, calling: value.calling, deathMode: value.deathMode, companionControlMode: isCompanionControlMode(value.companionControlMode) ? value.companionControlMode : 'autonomous', companionDeathMode: isCompanionDeathMode(value.companionDeathMode) ? value.companionDeathMode : 'injury', createdAt: value.createdAt, ...(isString(value.parentId) ? { parentId: value.parentId } : {}) }
}
const migrateCourier = (value: unknown): CourierSave | undefined => {
  if (!isRecord(value) || value.version !== 1) return undefined
  const identity = courierIdentity(value.identity)
  if (!identity) return undefined
  const run = migrateRunRecord(value.run)
  const checkpoint = migrateRunRecord(value.checkpoint)
  const heir = isHero(value.heir) ? { ...value.heir, name: value.heir.name ?? identity.name, origin: value.heir.origin ?? identity.origin, calling: value.heir.calling ?? identity.calling, deathMode: value.heir.deathMode ?? identity.deathMode } : undefined
  const campaign = migrateCampaignRoute(value.campaign)
  const records = migrateRecords(value.records)
  return { version: 1, identity, ...(run ? { run } : {}), ...(checkpoint ? { checkpoint } : {}), ...(heir ? { heir } : {}), campaign, records, ...(value.archived === true ? { archived: true } : {}) }
}
const entryFor = (courier: CourierSave): CourierMenuEntry => ({
  id: courier.identity.id, name: courier.identity.name, origin: courier.identity.origin, calling: courier.identity.calling, deathMode: courier.identity.deathMode,
  ...(courier.run ? { area: courier.run.area ?? courier.run.floor.biome, floor: courier.run.floor.index + 1, turn: courier.run.turn } : {}), ...(courier.archived ? { archived: true } : {})
})

export async function loadCouriers(): Promise<{ couriers: CourierSave[]; selectedId?: string }> {
  await courierWrites.flush()
  const indexed = await get<unknown>(COURIER_INDEX)
  const index = isCourierIndex(indexed) ? indexed : undefined
  if (index) {
    const couriers = (await Promise.all(index.ids.map(async id => migrateCourier(await get<unknown>(courierKey(id)))))).filter((courier): courier is CourierSave => Boolean(courier))
    return { couriers, ...(couriers.some(courier => courier.identity.id === index.selectedId) ? { selectedId: index.selectedId } : { selectedId: couriers.find(courier => !courier.archived)?.identity.id }) }
  }
  const [run, records, campaign] = await Promise.all([loadRun(), loadRecords(), loadCampaignRoute()])
  if (!run && records.runs.length === 0 && campaign.completedAreas.length === 0 && campaign.rescuedNpcs.length === 0) return { couriers: [] }
  const id = crypto.randomUUID()
  const legacy: CourierSave = { version: 1, identity: { id, name: run?.hero.name ?? 'Existing Courier', origin: run?.hero.origin ?? 'mineborn', calling: run?.hero.calling ?? 'trailguard', deathMode: run?.hero.deathMode ?? 'checkpoint', companionControlMode: campaign.companionControlMode, companionDeathMode: run?.companionDeathMode ?? 'injury', createdAt: new Date().toISOString() }, ...(run ? { run, checkpoint: structuredClone(run), heir: structuredClone(run.hero) } : {}), campaign, records }
  await saveCourier(legacy, id)
  return { couriers: [legacy], selectedId: id }
}

export async function saveCourier(courier: CourierSave, selectedId?: string): Promise<void> {
  const snapshot = structuredClone(courier)
  await courierWrites.enqueue(() => mutateCourierIndex((store, index, fail) => {
    const ids = index.ids.includes(snapshot.identity.id) ? index.ids : [...index.ids, snapshot.identity.id]
    putInTransaction(store, courierKey(snapshot.identity.id), snapshot, fail)
    putInTransaction(store, COURIER_INDEX, { version: 1, ids, selectedId: selectedId ?? index.selectedId ?? snapshot.identity.id } satisfies CourierIndex, fail)
  }))
}

export async function selectCourier(id: string): Promise<void> {
  await courierWrites.enqueue(() => mutateCourierIndex((store, index, fail) => {
    if (index.ids.includes(id)) putInTransaction(store, COURIER_INDEX, { ...index, selectedId: id }, fail)
  }))
}

export async function deleteCourier(id: string): Promise<void> {
  await courierWrites.enqueue(() => mutateCourierIndex((store, index, fail) => {
    const ids = index.ids.filter(currentId => currentId !== id)
    const selectedId = index.selectedId === id ? ids[0] : index.selectedId
    const deletion = store.delete(courierKey(id))
    deletion.onerror = () => fail(requestError(deletion.error, `failed to delete courier ${id}`))
    putInTransaction(store, COURIER_INDEX, { version: 1, ids, ...(selectedId ? { selectedId } : {}) } satisfies CourierIndex, fail)
  }))
}

export const flushCourierWrites = (): Promise<void> => courierWrites.flush()

export const courierMenuEntries = (couriers: readonly CourierSave[]): CourierMenuEntry[] => couriers.filter(courier => !courier.archived).map(entryFor)
