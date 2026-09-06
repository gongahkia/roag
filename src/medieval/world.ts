import { SeededRng, hashSeed } from './rng'
import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, auditMedievalContentSafety, classifyMedievalContent, contentSafetyAuditMatches, isMedievalContentSafetyAudit, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyDiagnostic } from './content-safety'
import { FRONTIER_CONTRACT_VERSION, createInitialFrontierState, frontierContentRecords, type FrontierState } from './frontier'
import { generationConfigurationFingerprint, generationRetryPlan, isReproducibleGenerationDiagnostics, resolveWorldGenerationConfig, type WorldGenerationConfig, type WorldGenerationConfigIssue, type WorldGenerationConfigRequest } from './generation-config'
import { INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION, INITIAL_WORLD_GENERATOR_VERSION, generateInitialWorld, initialWorldContentRecords, isInitialWorld, type InitialWorld, type InitialWorldGenerationDiagnostics, type InitialWorldGenerationProgressObserver } from './initial-world'
import { normalizeCreationSeed } from './settings'
import { createInitialHousehold, initialHouseholdActiveCrew, initialHouseholdContentRecords, validateInitialHouseholdRoster, validateInitialHouseholdStructure } from './initial-household'
import { advanceMedievalTemporalState, createMedievalTemporalState, isMedievalTemporalState, type TemporalCommand, type TemporalProvenance, type TimeBearingTemporalAction } from './temporal'
import { causalReplayProjectionForWorldState, createMedievalWorldState, isMedievalWorldState, medievalWorldStateContentRecords, WORLD_DECK_NAVIGATION_STATE_VERSION, type MedievalWorldState, type WorldAutonomyState, type WorldDeckNavigationState, type WorldDelegationState, type WorldPeopleState, type WorldSocialMemoryState } from './world-state'
import { createFidelityPlanForVerifiedWorld } from './fidelity'
import { advanceSimulationCatchUpState, reconcileSimulationCatchUpPlanState, resolveDelegatedWorkPlaceholder, validateSimulationCatchUpPlanState, validateSimulationCatchUpState, withDelegatedWorkPlaceholder } from './simulation-catchup'
import { advanceWorldEraForTemporalAction, recordDurableJomonGrowthEvidence, type DurableJomonGrowthEvidence, type WorldEraContext } from './world-era'
import { appendCausalCommand, causalReplayProjection, createCausalCommand, rebaseLegacyCausalHistoryCheckpointV4, replayCausalHistory, upgradeLegacyCausalHistoryStateV4, upgradeLegacyCausalHistoryStateV5, upgradeLegacyCausalHistoryStateV6, validateCausalHistoryReplay, type CausalCommandEvent, type CausalHistoryContext, type CausalReplayProjection } from './causal-history'
import { CONVERSATION_CONTRACT_VERSION, assessCourierConversation, assessCourierConversationForValidatedReplay, type ConversationAssessment } from './conversation'
import { advanceDelegatedTasks, delegatedWorkPlaceholderForTask, delegationInterruptionTemporalAction, delegationOfferTemporalAction, interruptDelegatedTask, isDelegationInterruptionInput, isDelegationOfferInput, offerDelegatedTask as offerDelegationTransition, type DelegationInterruptionInput, type DelegationOfferInput } from './delegation'
import { advanceAutonomyState, createAutonomyState, reconcileAutonomyState, validateAutonomyPlanState } from './autonomy'
import { assessJomonDeckStep, canonicalJomonDeckSpawn, isWalkableJomonDeckCoordinate, jomonDeckCoordinateId, type JomonDeckCollision, type JomonDeckCoordinate, type JomonDeckMovementDirection } from './jomon-navigation'
import { assessTavernCourierSwitchForVerifiedWorld, type TavernCourierSwitchAssessment } from './tavern-courier-switch'
import { assessCourierContinuityLoss, type CourierContinuityAssessment, type CourierContinuityConfirmation } from './courier-continuity'
import { initialVesselPropActionState, recordVesselPropAction } from './vessel-prop-action'
import { createVesselStationReadoutForVerifiedWorld, type VesselStationReadoutPropId } from './vessel-station-readout'
import { assessVesselPropOperationForVerifiedWorld } from './vessel-proximity-operation'
import { initialVesselCargoState, loadVesselCargo, recoverVesselCargo, resolveVesselCargoFailure, unloadVesselCargo, type VesselCargoFailureOutcome } from './cargo-hold'
import type { JomonCommodityId } from './commodity-catalogue'
import { createLegacyWorldMarketsStateV2, createWorldMarketsState } from './market'
import { acceptSettlementTradeContract as acceptSettlementTradeState, deliverSettlementTradeContract as deliverSettlementTradeState, initialSettlementTradingState, refuseSettlementTradeContract as refuseSettlementTradeState, settlementTradingAtSourceAnchor } from './settlement-trading'
import { createHearthfordWorksiteState, createHearthfordWorksiteTemporalAction, hearthfordWorksiteAnchorFor, hearthfordWorksiteResolutionFromAction, resolveHearthfordWorksite, type HearthfordWorksiteResolutionKind } from './hearthford-worksite'
import { deriveJomonDeckPlanForVerifiedWorld } from './jomon-deck-plan'
import { FOUNDATION_GENERATOR_VERSION, FOUNDATION_JOMON_PROP_DEFINITIONS, FOUNDATION_MANIFEST_VERSION, WORLD_CREATION_PROVENANCE_VERSION, WORLD_MANIFEST_FRONTIER_PROVENANCE_VERSION, WORLD_MANIFEST_VALIDATION_HISTORY_VERSION, type CausalRecord, type ChronicleReason, type FoundationCrewMember, type FoundationJomon, type FoundationWorld, type FrontierManifestProvenance, type FrontierRootManifestIdentity, type InitialWorldManifestIdentity, type LegacyFoundationWorldV13, type LegacyFoundationWorldV14, type LegacyFoundationWorldV14V13, type WorldChronicle, type WorldCreationProvenance, type WorldManifest } from './types'

const LEGACY_FOUNDATION_JOMON_PROPS = [
  { id: 'prop:chart-table', kind: 'table', partition: 'chart-table' },
  { id: 'prop:task-ledger', kind: 'ledger', partition: 'tavern' },
  { id: 'prop:gangplank', kind: 'gangplank', partition: 'gangplank' }
] as const satisfies readonly FoundationJomon['props'][number][]

const foundationJomon = (legacy = false): FoundationJomon => ({
  id: 'vessel:jomon',
  name: 'Jomon',
  contentSafety: classifyMedievalContent('place', ['navigation', 'settlement'], 'not-applicable', ['player-facing-text']),
  deckPartitions: ['tavern', 'chart-table', 'cargo-hold', 'repair-space', 'stores', 'berths', 'galley', 'gangplank'],
  quays: [],
  props: structuredClone(legacy ? LEGACY_FOUNDATION_JOMON_PROPS : FOUNDATION_JOMON_PROP_DEFINITIONS)
})

/** Upgrade-only audit rebinding; it never accepts a legacy record before validation. */
const currentStateContentSafetyAudit = (state: MedievalWorldState): MedievalContentSafetyAudit => {
  const audit = auditMedievalContentSafety(medievalWorldStateContentRecords(state))
  if (audit.status === 'rejected') throw new Error('upgraded world state content is unsafe')
  return audit
}

export interface FoundationWorldInput {
  seed?: string
  configuration?: WorldGenerationConfigRequest
  /** Actual initial-world generator progress for the local creation surface. */
  onGenerationProgress?: InitialWorldGenerationProgressObserver
}

export class InvalidWorldGenerationConfigurationError extends Error {
  constructor(readonly issues: readonly WorldGenerationConfigIssue[]) {
    super(`world generation configuration is invalid: ${issues.map(item => item.field).join(', ')}`)
    this.name = 'InvalidWorldGenerationConfigurationError'
  }
}

export class MedievalContentSafetyPolicyError extends Error {
  constructor(readonly diagnostics: readonly MedievalContentSafetyDiagnostic[]) {
    super(`generated medieval content violates policy: ${diagnostics.map(item => item.code).join(', ')}`)
    this.name = 'MedievalContentSafetyPolicyError'
  }
}

export const normalizeSeed = normalizeCreationSeed

const labelForSeed = (seed: string, configuration: WorldGenerationConfig): string => {
  const rng = new SeededRng(`label:${seed}:${generationConfigurationFingerprint(configuration)}`)
  return `${rng.pick(['Ash', 'Brackish', 'Candle', 'Drowned', 'Eel', 'Far', 'Grey', 'Hollow', 'Ivy', 'Low'])} ${rng.pick(['Basin', 'Current', 'Estuary', 'Ford', 'Mooring', 'Reach', 'Sound', 'Weir', 'Wick', 'Wold'])}`
}

const worldCreatedRecord = (label: string, seed: string): CausalRecord => ({
  sequence: 0,
  atWorldTime: 0,
  kind: 'world-created',
  detail: `Foundation world ${label} created from seed ${seed}.`,
  contentSafety: classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])
})

const foundationContentRecords = (
  labelContentSafety: WorldCreationProvenance['labelContentSafety'],
  jomon: FoundationJomon,
  crew: readonly FoundationCrewMember[],
  causalHistory: readonly CausalRecord[],
  initialWorld: InitialWorld,
  frontier: FrontierState
): readonly ClassifiedMedievalContent[] => [
  { id: 'world:label', domain: 'place', classification: labelContentSafety },
  { id: jomon.id, domain: 'place', classification: jomon.contentSafety },
  ...initialHouseholdContentRecords(crew),
  ...causalHistory.map(record => ({ id: `causal:${record.sequence}:${record.kind}`, domain: 'event' as const, classification: record.contentSafety })),
  ...initialWorldContentRecords(initialWorld),
  ...frontierContentRecords(frontier)
]

const auditFoundationContent = (
  labelContentSafety: WorldCreationProvenance['labelContentSafety'],
  jomon: FoundationJomon,
  crew: readonly FoundationCrewMember[],
  causalHistory: readonly CausalRecord[],
  initialWorld: InitialWorld,
  frontier: FrontierState
): MedievalContentSafetyAudit => {
  const audit = auditMedievalContentSafety(foundationContentRecords(labelContentSafety, jomon, crew, causalHistory, initialWorld, frontier))
  if (audit.status === 'rejected') throw new MedievalContentSafetyPolicyError(audit.diagnostics)
  return audit
}

interface ExpectedFoundationState {
  label: string
  labelContentSafety: WorldCreationProvenance['labelContentSafety']
  jomon: FoundationJomon
  crew: readonly FoundationCrewMember[]
  initialWorld: InitialWorld
  initialWorldGeneration: InitialWorldGenerationDiagnostics
  frontier: FrontierState
  causalHistory: readonly CausalRecord[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

const expectedFoundationState = (
  seed: string,
  configuration: WorldGenerationConfig,
  onGenerationProgress?: InitialWorldGenerationProgressObserver,
  legacyJomon = false
): ExpectedFoundationState | undefined => {
  const label = labelForSeed(seed, configuration)
  const labelContentSafety = classifyMedievalContent('place', ['environment', 'settlement'], 'not-applicable', ['player-facing-text'])
  const jomon = foundationJomon(legacyJomon)
  const household = createInitialHousehold({ seed, configuration })
  const crew = household.roster
  const initialWorldGeneration = generateInitialWorld(seed, configuration, onGenerationProgress)
  const frontier = createInitialFrontierState({ seed, configuration, initialWorld: initialWorldGeneration.world })
  const causalHistory = [worldCreatedRecord(label, seed)]
  return {
    label,
    labelContentSafety,
    jomon,
    crew,
    initialWorld: initialWorldGeneration.world,
    initialWorldGeneration: initialWorldGeneration.diagnostics,
    frontier,
    causalHistory,
    contentSafetyAudit: auditFoundationContent(labelContentSafety, jomon, crew, causalHistory, initialWorldGeneration.world, frontier)
  }
}

const canonicalJson = (value: unknown): string => {
  if (value === null) return 'null'
  if (typeof value === 'string' || typeof value === 'boolean') return JSON.stringify(value)
  if (typeof value === 'number') {
    if (!Number.isFinite(value)) throw new Error('canonical manifest values must be finite')
    return JSON.stringify(Object.is(value, -0) ? 0 : value)
  }
  if (Array.isArray(value)) return `[${value.map(canonicalJson).join(',')}]`
  if (!value || typeof value !== 'object' || (Object.getPrototypeOf(value) !== Object.prototype && Object.getPrototypeOf(value) !== null)) throw new Error('canonical manifest values must be serializable records')
  const record = value as Record<string, unknown>
  return `{${Object.keys(record).sort().map(key => `${JSON.stringify(key)}:${canonicalJson(record[key])}`).join(',')}}`
}

const equivalent = (left: unknown, right: unknown): boolean => {
  try { return canonicalJson(left) === canonicalJson(right) } catch { return false }
}

const digestFor = (scope: string, value: unknown): string => {
  const source = `${scope}|${canonicalJson(value)}`
  const forward = hashSeed(source).toString(36)
  const reverse = hashSeed([...source].reverse().join(''), 0x9e3779b9).toString(36)
  return `${forward}-${reverse}`
}

const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const sortedExpected = [...expected].sort()
  return keys.length === sortedExpected.length && keys.every((key, index) => key === sortedExpected[index])
}

const initialWorldIdentityFor = (world: InitialWorld): InitialWorldManifestIdentity => ({
  id: world.id,
  candidateAttempt: world.candidateAttempt,
  configurationFingerprint: world.configurationFingerprint,
  digest: digestFor('initial-world', world),
  watershedId: world.watershed.id,
  waterwayIds: world.waterways.map(record => record.id),
  climateId: world.climate.id,
  seasonIds: world.seasons.map(record => record.id),
  resourceIds: world.resources.map(record => record.id),
  ecologyIds: world.ecologies.map(record => record.id),
  settlementIds: world.settlements.map(record => record.id),
  institutionIds: world.institutions.map(record => record.id),
  personIds: world.people.map(record => record.id),
  routeIds: world.routes.map(record => record.id),
  tradeLinkIds: world.tradeLinks.map(record => record.id),
  routeHazardIds: world.routeHazards.map(record => record.id),
  historyEventIds: world.history.map(record => record.id)
})

const frontierRootIdentityFor = (frontier: FrontierState): readonly FrontierRootManifestIdentity[] => frontier.regions
  .map(region => region.commitment)
  .sort((left, right) => left.generationOrder - right.generationOrder || left.id.localeCompare(right.id))
  .map(commitment => ({
    id: commitment.id,
    coordinate: { ...commitment.coordinate },
    kind: commitment.kind,
    generationOrder: commitment.generationOrder,
    generatorStream: commitment.generatorStream,
    anchor: { ...commitment.anchor },
    connection: { ...commitment.connection }
  }))

const frontierProvenanceFor = (frontier: FrontierState): FrontierManifestProvenance => {
  const roots = frontierRootIdentityFor(frontier)
  const draft = {
    version: WORLD_MANIFEST_FRONTIER_PROVENANCE_VERSION,
    contractVersion: FRONTIER_CONTRACT_VERSION,
    initialWorldId: frontier.provenance.initialWorldId,
    roots
  }
  return { ...draft, digest: digestFor('frontier-roots', draft) }
}

const creationProvenanceFor = (
  seed: string,
  selectedConfiguration: WorldCreationProvenance['selectedConfiguration'],
  resolvedConfiguration: WorldGenerationConfig,
  state: ExpectedFoundationState
): WorldCreationProvenance => {
  const draft: Omit<WorldCreationProvenance, 'digest'> = {
    version: WORLD_CREATION_PROVENANCE_VERSION,
    seed,
    selectedConfiguration,
    resolvedConfiguration,
    configurationFingerprint: generationConfigurationFingerprint(resolvedConfiguration),
    contractVersions: {
      foundationGenerator: FOUNDATION_GENERATOR_VERSION,
      initialWorldGenerator: INITIAL_WORLD_GENERATOR_VERSION,
      initialWorldDiagnostics: INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION,
      frontierContract: FRONTIER_CONTRACT_VERSION,
      contentSafetyPolicy: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
    },
    validationHistory: {
      version: WORLD_MANIFEST_VALIDATION_HISTORY_VERSION,
      generation: { validation: { status: 'accepted', issues: [] }, retryPlan: generationRetryPlan(seed, resolvedConfiguration) },
      initialWorld: state.initialWorldGeneration,
      frontier: { status: 'accepted', issues: [] }
    },
    initialWorld: initialWorldIdentityFor(state.initialWorld),
    frontier: frontierProvenanceFor(state.frontier),
    label: state.label,
    labelContentSafety: state.labelContentSafety,
    contentSafetyAudit: state.contentSafetyAudit
  }
  return { ...draft, digest: digestFor('world-creation', draft) }
}

const idForCreationProvenance = (creation: WorldCreationProvenance): string => `world:${creation.digest}`

const temporalProvenanceForCreation = (creation: WorldCreationProvenance): TemporalProvenance => ({
  version: 1,
  worldId: idForCreationProvenance(creation),
  creationDigest: creation.digest,
  seed: creation.seed
})

const expectedCreationProvenance = (seed: string, selectedConfiguration: WorldCreationProvenance['selectedConfiguration'], resolvedConfiguration: WorldGenerationConfig): WorldCreationProvenance | undefined => {
  const state = expectedFoundationState(seed, resolvedConfiguration)
  return state === undefined ? undefined : creationProvenanceFor(seed, selectedConfiguration, resolvedConfiguration, state)
}

const cloneWorld = (world: FoundationWorld): FoundationWorld => structuredClone(world)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)

export type FoundationWorldValidationCode =
  | 'foundation-world.malformed-record'
  | 'foundation-world.invalid-version'
  | 'foundation-world.invalid-id'
  | 'foundation-world.invalid-status'
  | 'foundation-world.invalid-manifest'
  | 'foundation-world.invalid-household'
  | 'foundation-world.invalid-initial-world'
  | 'foundation-world.invalid-immutable-content'
  | 'foundation-world.invalid-mutable-state'
  | 'foundation-world.invalid-catch-up'
  | 'foundation-world.invalid-autonomy'
  | 'foundation-world.invalid-navigation'
  | 'foundation-world.invalid-causal-history'

export interface FoundationWorldValidationIssue {
  code: FoundationWorldValidationCode
  recordId: string
}

const foundationWorldIssue = (recordId: string, code: FoundationWorldValidationCode): FoundationWorldValidationIssue => ({ recordId, code })

/**
 * One fail-closed boundary for repository reads and public mutable-world
 * transitions. Keeping this evidence together prevents action-time checks
 * from accepting a world that local persistence would reject.
 */
export const validateFoundationWorld = (value: unknown): readonly FoundationWorldValidationIssue[] => {
  if (!record(value)) return [foundationWorldIssue('foundation-world', 'foundation-world.malformed-record')]
  if (!hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state'])) return [foundationWorldIssue('foundation-world', 'foundation-world.malformed-record')]
  const issues: FoundationWorldValidationIssue[] = []
  if (value.version !== 15) issues.push(foundationWorldIssue('foundation-world', 'foundation-world.invalid-version'))
  if (typeof value.id !== 'string' || !value.id) issues.push(foundationWorldIssue('foundation-world', 'foundation-world.invalid-id'))
  if (value.status !== 'active') issues.push(foundationWorldIssue('foundation-world', 'foundation-world.invalid-status'))
  if (!isReproducibleWorldManifest(value.manifest)) {
    issues.push(foundationWorldIssue('foundation-world:manifest', 'foundation-world.invalid-manifest'))
    return issues
  }
  const world = value as unknown as FoundationWorld
  if (world.id !== foundationWorldIdForManifest(world.manifest)) issues.push(foundationWorldIssue('foundation-world:id', 'foundation-world.invalid-id'))
  if (validateInitialHouseholdRoster({ seed: world.manifest.creation.seed, configurationFingerprint: world.manifest.creation.configurationFingerprint }, world.crew).length) issues.push(foundationWorldIssue('foundation-world:household', 'foundation-world.invalid-household'))
  if (!isInitialWorld(world.initialWorld)) issues.push(foundationWorldIssue('foundation-world:initial-world', 'foundation-world.invalid-initial-world'))
  try {
    if (!foundationWorldInitialWorldMatchesManifest(world)) issues.push(foundationWorldIssue('foundation-world:initial-world', 'foundation-world.invalid-initial-world'))
    if (!foundationWorldContentSatisfiesSafetyPolicy(world)) issues.push(foundationWorldIssue('foundation-world:immutable-content', 'foundation-world.invalid-immutable-content'))
    if (!foundationWorldTemporalStateMatches(world)) issues.push(foundationWorldIssue('foundation-world:mutable-state', 'foundation-world.invalid-mutable-state'))
    if (!foundationWorldNavigationStateMatches(world)) issues.push(foundationWorldIssue('foundation-world:navigation', 'foundation-world.invalid-navigation'))
    if (!foundationWorldCatchUpStateMatches(world)) issues.push(foundationWorldIssue('foundation-world:catch-up', 'foundation-world.invalid-catch-up'))
    if (!foundationWorldAutonomyStateMatches(world)) issues.push(foundationWorldIssue('foundation-world:autonomy', 'foundation-world.invalid-autonomy'))
    if (!foundationWorldCausalHistoryMatches(world)) issues.push(foundationWorldIssue('foundation-world:causal-history', 'foundation-world.invalid-causal-history'))
  } catch {
    issues.push(foundationWorldIssue('foundation-world', 'foundation-world.invalid-mutable-state'))
  }
  return issues
}

export const isValidFoundationWorld = (value: unknown): value is FoundationWorld => validateFoundationWorld(value).length === 0

/**
 * v14 has the same immutable manifest and mutable schema but only the three
 * then-existing static props. This proof intentionally runs before adding the
 * v15 provenance so a forged old envelope cannot become valid by conversion.
 */
const legacyFoundationWorldContentSatisfiesSafetyPolicy = (world: FoundationWorld): boolean => {
  try {
    const staticState = expectedFoundationState(world.manifest.creation.seed, world.manifest.creation.resolvedConfiguration, undefined, true)
    if (!staticState) return false
    return equivalent(world.jomon, staticState.jomon)
      && equivalent(world.crew, staticState.crew)
      && contentSafetyAuditMatches(foundationContentRecords(world.manifest.creation.labelContentSafety, world.jomon, world.crew, staticState.causalHistory, world.initialWorld, staticState.frontier), world.manifest.creation.contentSafetyAudit)
  } catch { return false }
}

/** A local v15 static façade is used only to prove unchanged plan-derived replay/navigation. */
const v15StaticFacadeForLegacy = (world: FoundationWorld): FoundationWorld => ({
  ...structuredClone(world),
  version: 15,
  jomon: foundationJomon()
})

const legacyFoundationWorldV14IsValid = (world: FoundationWorld): boolean => {
  try {
    return isReproducibleWorldManifest(world.manifest)
      && isInitialWorld(world.initialWorld)
      && world.id === foundationWorldIdForManifest(world.manifest)
      && validateInitialHouseholdRoster({ seed: world.manifest.creation.seed, configurationFingerprint: world.manifest.creation.configurationFingerprint }, world.crew).length === 0
      && foundationWorldInitialWorldMatchesManifest(world)
      && legacyFoundationWorldContentSatisfiesSafetyPolicy(world)
      && foundationWorldTemporalStateMatches(world)
  } catch { return false }
}

/**
 * Strict read-only v14 -> v15 conversion for the deterministic eight-prop
 * fixture. It replaces no mutable state and the final current validator proves
 * replay, navigation, catch-up, autonomy, and immutable recreation again.
 */
export const upgradeFoundationWorldV15 = (value: unknown): FoundationWorld => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state']) || value.version !== 14 || value.status !== 'active') throw new Error('foundation world is not a v14 active envelope')
  const legacy = value as unknown as FoundationWorld
  if (!legacyFoundationWorldV14IsValid(legacy)) throw new Error('foundation world v14 envelope is invalid')
  const staticFacade = v15StaticFacadeForLegacy(legacy)
  if (!foundationWorldNavigationStateMatches(staticFacade)
    || !foundationWorldCatchUpStateMatches(staticFacade)
    || !foundationWorldAutonomyStateMatches(staticFacade)) throw new Error('foundation world v14 replay evidence is invalid')
  return upgradeFoundationWorldStateV15(staticFacade)
}

/**
 * Strict read-only v15/state-v14 conversion.  The old source is structurally
 * and replay-validated before the new bounded latest-action projection is
 * introduced.  It never writes the source record; repositories may persist
 * the resulting full envelope only through a later normal save.
 */
export const upgradeFoundationWorldStateV15 = (value: unknown): FoundationWorld => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state']) || value.version !== 15 || value.status !== 'active') throw new Error('foundation world is not a v15/state-v14 active envelope')
  const legacy = value as unknown as FoundationWorld
  if ((legacy.state as unknown as { version?: unknown }).version !== 14
    || !isReproducibleWorldManifest(legacy.manifest)
    || !isInitialWorld(legacy.initialWorld)
    || !foundationWorldInitialWorldMatchesManifest(legacy)
    || !foundationWorldContentSatisfiesSafetyPolicy(legacy)
    || !foundationWorldTemporalStateMatches(legacy)
    || !foundationWorldNavigationStateMatches(legacy)
    || !foundationWorldCatchUpStateMatches(legacy)
    || !foundationWorldAutonomyStateMatches(legacy)) throw new Error('foundation world v15/state-v14 envelope is invalid')

  const context = causalHistoryContextFor(legacy)
  const checkpoint = legacy.state.causalHistory.checkpoint.projection as unknown as Omit<CausalReplayProjection, 'version' | 'jomon'>
  const checkpointProjection = causalReplayProjection({
    ...checkpoint,
    jomon: {
      ...(legacy.state.jomon as unknown as Omit<MedievalWorldState['jomon'], 'version' | 'propActions' | 'cargo'>),
      version: 3,
      propActions: initialVesselPropActionState(legacy.jomon),
      cargo: initialVesselCargoState()
    },
    settlementTrading: initialSettlementTradingState()
  })
  const history = upgradeLegacyCausalHistoryStateV4(context, legacy.state.causalHistory, checkpointProjection)
  const provisional = {
    ...structuredClone(legacy),
    state: {
      ...structuredClone(legacy.state),
      version: 17,
      jomon: structuredClone(checkpointProjection.jomon),
      settlementTrading: structuredClone(checkpointProjection.settlementTrading),
      causalHistory: history
    }
  } as unknown as FoundationWorld
  const replayed = replayCausalHistory(context, history, (projection, command) => replayCommandProjection(provisional, projection, command))
  const legacyProjection = legacy.state as unknown as Pick<MedievalWorldState, 'courier' | 'navigation' | 'people' | 'temporal' | 'simulation' | 'era' | 'delegation' | 'autonomy' | 'socialMemory'>
  if (!equivalent(replayed.courier, legacyProjection.courier)
    || !equivalent(replayed.navigation, legacyProjection.navigation)
    || !equivalent(replayed.people, legacyProjection.people)
    || !equivalent(replayed.temporal, legacyProjection.temporal)
    || !equivalent(replayed.simulation, legacyProjection.simulation)
    || !equivalent(replayed.era, legacyProjection.era)
    || !equivalent(replayed.delegation, legacyProjection.delegation)
    || !equivalent(replayed.autonomy, legacyProjection.autonomy)
    || !equivalent(replayed.socialMemory, legacyProjection.socialMemory)
    || !equivalent(replayed.settlementTrading, initialSettlementTradingState())) throw new Error('foundation world v15/state-v14 replay evidence is invalid')
  const upgraded = {
    ...structuredClone(legacy),
    state: {
      ...structuredClone(legacy.state),
      version: 17,
      courier: structuredClone(replayed.courier),
      navigation: structuredClone(replayed.navigation ?? { version: WORLD_DECK_NAVIGATION_STATE_VERSION }),
      people: structuredClone(replayed.people),
      temporal: structuredClone(replayed.temporal),
      simulation: structuredClone(replayed.simulation),
      era: structuredClone(replayed.era),
      delegation: structuredClone(replayed.delegation),
      autonomy: structuredClone(replayed.autonomy),
      socialMemory: structuredClone(replayed.socialMemory),
      settlementTrading: structuredClone(replayed.settlementTrading),
      jomon: structuredClone(replayed.jomon),
      causalHistory: history
    }
  } as unknown as FoundationWorld
  upgraded.state.contentSafetyAudit = currentStateContentSafetyAudit(upgraded.state)
  const validation = validateFoundationWorld(upgraded)
  if (validation.length) throw new Error(`foundation world v15/state-v14 conversion did not reproduce a valid v17 envelope: ${validation.map(item => item.code).join(', ')}`)
  return upgradeFoundationWorldStateV19(upgradeFoundationWorldStateV18(upgraded))
}

/**
 * Strict read-only state-v15 -> state-v16 conversion. The v15 envelope is
 * first validated with its no-cargo replay evidence; then an empty bounded
 * hold is added to its checkpoint and full replay proves the upgraded result.
 */
export const upgradeFoundationWorldStateV16 = (value: unknown): FoundationWorld => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state']) || value.version !== 15 || value.status !== 'active') throw new Error('foundation world is not a v15/state-v15 active envelope')
  const legacy = value as unknown as FoundationWorld
  if ((legacy.state as unknown as { version?: unknown }).version !== 15
    || !isReproducibleWorldManifest(legacy.manifest)
    || !isInitialWorld(legacy.initialWorld)
    || !foundationWorldInitialWorldMatchesManifest(legacy)
    || !foundationWorldContentSatisfiesSafetyPolicy(legacy)
    || !foundationWorldTemporalStateMatches(legacy)
    || !foundationWorldNavigationStateMatches(legacy)
    || !foundationWorldCatchUpStateMatches(legacy)
    || !foundationWorldAutonomyStateMatches(legacy)) throw new Error('foundation world v15/state-v15 envelope is invalid')

  const context = causalHistoryContextFor(legacy)
  const checkpoint = legacy.state.causalHistory.checkpoint.projection as unknown as Omit<CausalReplayProjection, 'version' | 'jomon'>
  const checkpointProjection = causalReplayProjection({
    ...checkpoint,
    jomon: {
      ...(legacy.state.jomon as unknown as Omit<MedievalWorldState['jomon'], 'version' | 'cargo'>),
      version: 3,
      cargo: initialVesselCargoState()
    },
    settlementTrading: initialSettlementTradingState()
  })
  const history = upgradeLegacyCausalHistoryStateV5(context, legacy.state.causalHistory, checkpointProjection)
  const provisional = {
    ...structuredClone(legacy),
    state: {
      ...structuredClone(legacy.state),
      version: 17,
      jomon: structuredClone(checkpointProjection.jomon),
      settlementTrading: structuredClone(checkpointProjection.settlementTrading),
      causalHistory: history
    }
  } as unknown as FoundationWorld
  const replayed = replayCausalHistory(context, history, (projection, command) => replayCommandProjection(provisional, projection, command))
  const legacyProjection = legacy.state as unknown as Pick<MedievalWorldState, 'courier' | 'navigation' | 'people' | 'temporal' | 'simulation' | 'era' | 'delegation' | 'autonomy' | 'socialMemory'>
  if (!equivalent(replayed.courier, legacyProjection.courier)
    || !equivalent(replayed.navigation, legacyProjection.navigation)
    || !equivalent(replayed.people, legacyProjection.people)
    || !equivalent(replayed.temporal, legacyProjection.temporal)
    || !equivalent(replayed.simulation, legacyProjection.simulation)
    || !equivalent(replayed.era, legacyProjection.era)
    || !equivalent(replayed.delegation, legacyProjection.delegation)
    || !equivalent(replayed.autonomy, legacyProjection.autonomy)
    || !equivalent(replayed.socialMemory, legacyProjection.socialMemory)
    || !equivalent(replayed.jomon.cargo, initialVesselCargoState())
    || !equivalent(replayed.settlementTrading, initialSettlementTradingState())) throw new Error('foundation world v15/state-v15 replay evidence is invalid')
  const upgraded = {
    ...structuredClone(legacy),
    state: {
      ...structuredClone(legacy.state),
      version: 17,
      courier: structuredClone(replayed.courier),
      navigation: structuredClone(replayed.navigation ?? { version: WORLD_DECK_NAVIGATION_STATE_VERSION }),
      people: structuredClone(replayed.people),
      temporal: structuredClone(replayed.temporal),
      simulation: structuredClone(replayed.simulation),
      era: structuredClone(replayed.era),
      delegation: structuredClone(replayed.delegation),
      autonomy: structuredClone(replayed.autonomy),
      socialMemory: structuredClone(replayed.socialMemory),
      settlementTrading: structuredClone(replayed.settlementTrading),
      jomon: structuredClone(replayed.jomon),
      causalHistory: history
    }
  } as unknown as FoundationWorld
  upgraded.state.contentSafetyAudit = currentStateContentSafetyAudit(upgraded.state)
  const validation = validateFoundationWorld(upgraded)
  if (validation.length) throw new Error(`foundation world v15/state-v15 conversion did not reproduce a valid v17 envelope: ${validation.map(item => item.code).join(', ')}`)
  return upgradeFoundationWorldStateV19(upgradeFoundationWorldStateV18(upgraded))
}

/**
 * Strict read-only state-v16 -> state-v17 conversion. A valid cargo envelope
 * is fully proved under its original causal v6 projection before the local
 * settlement contract is added to the checkpoint and current replay result.
 */
export const upgradeFoundationWorldStateV17 = (value: unknown): FoundationWorld => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state']) || value.version !== 15 || value.status !== 'active') throw new Error('foundation world is not a v15/state-v16 active envelope')
  const legacy = value as unknown as FoundationWorld
  if ((legacy.state as unknown as { version?: unknown }).version !== 16
    || !isReproducibleWorldManifest(legacy.manifest)
    || !isInitialWorld(legacy.initialWorld)
    || !foundationWorldInitialWorldMatchesManifest(legacy)
    || !foundationWorldContentSatisfiesSafetyPolicy(legacy)
    || !foundationWorldTemporalStateMatches(legacy)
    || !foundationWorldNavigationStateMatches(legacy)
    || !foundationWorldCatchUpStateMatches(legacy)
    || !foundationWorldAutonomyStateMatches(legacy)) throw new Error('foundation world v15/state-v16 envelope is invalid')

  const context = causalHistoryContextFor(legacy)
  const checkpoint = legacy.state.causalHistory.checkpoint.projection as unknown as Omit<CausalReplayProjection, 'version' | 'settlementTrading'>
  const checkpointProjection = causalReplayProjection({ ...checkpoint, settlementTrading: initialSettlementTradingState() })
  const history = upgradeLegacyCausalHistoryStateV6(context, legacy.state.causalHistory, checkpointProjection)
  const provisional = {
    ...structuredClone(legacy),
    state: {
      ...structuredClone(legacy.state),
      version: 17,
      settlementTrading: structuredClone(checkpointProjection.settlementTrading),
      causalHistory: history
    }
  } as unknown as FoundationWorld
  const replayed = replayCausalHistory(context, history, (projection, command) => replayCommandProjection(provisional, projection, command))
  const legacyProjection = legacy.state as unknown as Pick<MedievalWorldState, 'courier' | 'navigation' | 'people' | 'temporal' | 'simulation' | 'era' | 'delegation' | 'autonomy' | 'socialMemory' | 'jomon'>
  if (!equivalent(replayed.courier, legacyProjection.courier)
    || !equivalent(replayed.navigation, legacyProjection.navigation)
    || !equivalent(replayed.people, legacyProjection.people)
    || !equivalent(replayed.temporal, legacyProjection.temporal)
    || !equivalent(replayed.simulation, legacyProjection.simulation)
    || !equivalent(replayed.era, legacyProjection.era)
    || !equivalent(replayed.delegation, legacyProjection.delegation)
    || !equivalent(replayed.autonomy, legacyProjection.autonomy)
    || !equivalent(replayed.socialMemory, legacyProjection.socialMemory)
    || !equivalent(replayed.jomon, legacyProjection.jomon)
    || !equivalent(replayed.settlementTrading, initialSettlementTradingState())) throw new Error('foundation world v15/state-v16 replay evidence is invalid')
  const upgraded = {
    ...structuredClone(legacy),
    state: {
      ...structuredClone(legacy.state),
      version: 17,
      courier: structuredClone(replayed.courier),
      navigation: structuredClone(replayed.navigation ?? { version: WORLD_DECK_NAVIGATION_STATE_VERSION }),
      people: structuredClone(replayed.people),
      temporal: structuredClone(replayed.temporal),
      simulation: structuredClone(replayed.simulation),
      era: structuredClone(replayed.era),
      delegation: structuredClone(replayed.delegation),
      autonomy: structuredClone(replayed.autonomy),
      socialMemory: structuredClone(replayed.socialMemory),
      settlementTrading: structuredClone(replayed.settlementTrading),
      jomon: structuredClone(replayed.jomon),
      causalHistory: history
    }
  } as unknown as FoundationWorld
  upgraded.state.contentSafetyAudit = currentStateContentSafetyAudit(upgraded.state)
  const validation = validateFoundationWorld(upgraded)
  if (validation.length) throw new Error(`foundation world v15/state-v16 conversion did not reproduce a valid v17 envelope: ${validation.map(item => item.code).join(', ')}`)
  return upgradeFoundationWorldStateV19(upgradeFoundationWorldStateV18(upgraded))
}

/**
 * Strict read-only state-v17 -> state-v18 conversion. Local market conditions
 * are an exact seed and settlement-trade projection, so no causal command,
 * checkpoint, immutable source, or time value is altered by this bridge.
 */
export const upgradeFoundationWorldStateV18 = (value: unknown): FoundationWorld => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state']) || value.version !== 15 || value.status !== 'active') throw new Error('foundation world is not a v15/state-v17 active envelope')
  const legacy = value as unknown as FoundationWorld
  if ((legacy.state as unknown as { version?: unknown }).version !== 17
    || !isReproducibleWorldManifest(legacy.manifest)
    || !isInitialWorld(legacy.initialWorld)
    || !foundationWorldInitialWorldMatchesManifest(legacy)
    || !foundationWorldContentSatisfiesSafetyPolicy(legacy)
    || !foundationWorldTemporalStateMatches(legacy)
    || !foundationWorldNavigationStateMatches(legacy)
    || !foundationWorldCatchUpStateMatches(legacy)
    || !foundationWorldAutonomyStateMatches(legacy)) throw new Error('foundation world v15/state-v17 envelope is invalid')

  const upgraded = {
    ...structuredClone(legacy),
    state: {
      ...structuredClone(legacy.state),
      version: 18,
      markets: createLegacyWorldMarketsStateV2({
        seed: legacy.manifest.creation.seed,
        siteIds: legacy.state.sites.sites.map(site => site.id),
        settlementTrading: legacy.state.settlementTrading
      })
    }
  } as unknown as FoundationWorld
  upgraded.state.contentSafetyAudit = currentStateContentSafetyAudit(upgraded.state)
  const validation = validateFoundationWorld(upgraded)
  if (validation.length) throw new Error(`foundation world v15/state-v17 conversion did not reproduce a valid v18 envelope: ${validation.map(item => item.code).join(', ')}`)
  return upgraded
}

/**
 * Strict read-only state-v18 -> state-v19 conversion. The unresolved
 * mill-race condition is derived only from the immutable creation seed; the
 * market is then rebuilt from that state and the existing tally evidence.
 */
export const upgradeFoundationWorldStateV19 = (value: unknown): FoundationWorld => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state']) || value.version !== 15 || value.status !== 'active') throw new Error('foundation world is not a v15/state-v18 active envelope')
  const legacy = value as unknown as FoundationWorld
  if ((legacy.state as unknown as { version?: unknown }).version !== 18
    || !isReproducibleWorldManifest(legacy.manifest)
    || !isInitialWorld(legacy.initialWorld)
    || !foundationWorldInitialWorldMatchesManifest(legacy)
    || !foundationWorldContentSatisfiesSafetyPolicy(legacy)
    || !foundationWorldTemporalStateMatches(legacy)
    || !foundationWorldNavigationStateMatches(legacy)
    || !foundationWorldCatchUpStateMatches(legacy)
    || !foundationWorldAutonomyStateMatches(legacy)) throw new Error('foundation world v15/state-v18 envelope is invalid')
  const hearthfordWorksite = createHearthfordWorksiteState(legacy.manifest.creation.seed)
  const upgraded = {
    ...structuredClone(legacy),
    state: {
      ...structuredClone(legacy.state),
      version: 19,
      hearthfordWorksite,
      markets: createWorldMarketsState({
        seed: legacy.manifest.creation.seed,
        siteIds: legacy.state.sites.sites.map(site => site.id),
        settlementTrading: legacy.state.settlementTrading,
        hearthfordWorksite
      })
    }
  } as unknown as FoundationWorld
  upgraded.state.contentSafetyAudit = currentStateContentSafetyAudit(upgraded.state)
  const validation = validateFoundationWorld(upgraded)
  if (validation.length) throw new Error(`foundation world v15/state-v18 conversion did not reproduce a valid v19 envelope: ${validation.map(item => item.code).join(', ')}`)
  return upgraded
}

/**
 * Explicit v13 -> v14 conversion for the local courier coordinate. It reads
 * only the old full envelope, changes no immutable provenance or IDs, and
 * rebuilds the affected replay checkpoint before the normal v14 validator
 * accepts it. Invalid or ambiguous input throws and is left for storage to
 * preserve unchanged.
 */
const rebaseLegacyStateToV14 = (
  world: FoundationWorld,
  courier: { version: 3; initialCourierId?: string; activeCourierId?: string; departedCourierIds?: readonly string[] },
  navigation: WorldDeckNavigationState
): unknown => {
  const checkpoint = world.state.causalHistory.checkpoint.projection as unknown as Record<string, unknown>
  const checkpointCourierSource = record(checkpoint.courier) ? checkpoint.courier : undefined
  const checkpointInitialCourierId = typeof checkpointCourierSource?.initialCourierId === 'string' ? checkpointCourierSource.initialCourierId : undefined
  const checkpointActiveCourierId = checkpointCourierSource?.version === 2 && typeof checkpointCourierSource.activeCourierId === 'string'
    ? checkpointCourierSource.activeCourierId
    : checkpointInitialCourierId
  const checkpointCourier = checkpointInitialCourierId === undefined
    ? { version: 3 as const }
    : { version: 3 as const, initialCourierId: checkpointInitialCourierId, activeCourierId: checkpointActiveCourierId!, departedCourierIds: [] as const }
  const checkpointNavigationSource = record(checkpoint.navigation) ? checkpoint.navigation : undefined
  const checkpointNavigation: WorldDeckNavigationState = checkpointActiveCourierId !== undefined
    && checkpointNavigationSource?.courierId === checkpointActiveCourierId
    && record(checkpointNavigationSource.coordinate)
    && typeof checkpointNavigationSource.coordinate.column === 'number'
    && typeof checkpointNavigationSource.coordinate.row === 'number'
    ? { version: WORLD_DECK_NAVIGATION_STATE_VERSION, courierId: checkpointActiveCourierId, coordinate: { column: checkpointNavigationSource.coordinate.column, row: checkpointNavigationSource.coordinate.row } }
    : checkpointActiveCourierId === undefined
      ? { version: WORLD_DECK_NAVIGATION_STATE_VERSION }
      : { version: WORLD_DECK_NAVIGATION_STATE_VERSION, courierId: checkpointActiveCourierId, coordinate: canonicalJomonDeckSpawn(world) }
  const projection: Record<string, unknown> = {
    ...structuredClone(checkpoint),
    version: 6,
    courier: checkpointCourier,
    navigation: checkpointNavigation
  }
  delete projection.jomon
  const history = rebaseLegacyCausalHistoryCheckpointV4(causalHistoryContextFor(world), world.state.causalHistory, projection)
  return {
    ...structuredClone(world.state),
    version: 14,
    courier: structuredClone(courier),
    navigation: structuredClone(navigation),
    causalHistory: history
  }
}

export const upgradeFoundationWorldV13 = (value: unknown): FoundationWorld => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state']) || value.version !== 13 || value.status !== 'active') throw new Error('foundation world is not a v13 active envelope')
  const legacy = value as unknown as LegacyFoundationWorldV13
  if (!isReproducibleWorldManifest(legacy.manifest) || !isInitialWorld(legacy.initialWorld)
    || !foundationWorldInitialWorldMatchesManifest(legacy as unknown as FoundationWorld)
    || !legacyFoundationWorldContentSatisfiesSafetyPolicy(legacy as unknown as FoundationWorld)
    || !foundationWorldTemporalStateMatches(legacy as unknown as FoundationWorld)) throw new Error('foundation world v13 envelope is invalid')
  const planFacade = v15StaticFacadeForLegacy(legacy as unknown as FoundationWorld)
  const selectedCourierId = legacy.state.courier.initialCourierId
  const navigation: WorldDeckNavigationState = selectedCourierId === undefined
    ? { version: WORLD_DECK_NAVIGATION_STATE_VERSION }
    : { version: WORLD_DECK_NAVIGATION_STATE_VERSION, courierId: selectedCourierId, coordinate: canonicalJomonDeckSpawn(planFacade) }
  const state = rebaseLegacyStateToV14(legacy as unknown as FoundationWorld,
    selectedCourierId === undefined
      ? { version: 3 }
      : { version: 3, initialCourierId: selectedCourierId, activeCourierId: selectedCourierId, departedCourierIds: [] },
    navigation)
  return upgradeFoundationWorldStateV15({ ...planFacade, state })
}

/**
 * Explicit read-only v14/v12 conversion. It retains the selected courier as
 * the initial and current active courier, preserves its validated local
 * coordinate, and rebases only the replay checkpoint's versioned projection.
 */
export const upgradeFoundationWorldV14 = (value: unknown): FoundationWorld => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state']) || value.version !== 14 || value.status !== 'active') throw new Error('foundation world is not a v14 active envelope')
  const legacy = value as unknown as LegacyFoundationWorldV14 | LegacyFoundationWorldV14V13
  if (!isReproducibleWorldManifest(legacy.manifest) || !isInitialWorld(legacy.initialWorld)
    || !foundationWorldInitialWorldMatchesManifest(legacy as unknown as FoundationWorld)
    || !legacyFoundationWorldContentSatisfiesSafetyPolicy(legacy as unknown as FoundationWorld)
    || !foundationWorldTemporalStateMatches(legacy as unknown as FoundationWorld)) throw new Error('foundation world v14 envelope is invalid')
  const currentState = value as unknown as { state: { version: number; courier: { version: number } } }
  if (currentState.state.version === 14 && currentState.state.courier.version === 3) return upgradeFoundationWorldV15(value)
  const planFacade = v15StaticFacadeForLegacy(legacy as unknown as FoundationWorld)
  const selectedCourierId = legacy.state.courier.initialCourierId
  const activeCourierId = legacy.state.version === 13 ? legacy.state.courier.activeCourierId : selectedCourierId
  const navigation = legacy.state.navigation
  if (selectedCourierId === undefined
    ? navigation.courierId !== undefined || navigation.coordinate !== undefined
    : activeCourierId === undefined || navigation.courierId !== activeCourierId || navigation.coordinate === undefined || !isWalkableJomonDeckCoordinate(planFacade, navigation.coordinate)) throw new Error('foundation world v14 navigation is invalid')
  const state = rebaseLegacyStateToV14(legacy as unknown as FoundationWorld,
    selectedCourierId === undefined
      ? { version: 3 }
      : { version: 3, initialCourierId: selectedCourierId, activeCourierId: activeCourierId!, departedCourierIds: [] },
    navigation)
  return upgradeFoundationWorldStateV15({ ...planFacade, state })
}

export const foundationWorldIdForManifest = (manifest: WorldManifest): string => idForCreationProvenance(manifest.creation)

/** Stable JSON for sharing/export; it refuses a manifest outside the current contract. */
export const serializeWorldManifest = (manifest: WorldManifest): string => {
  if (!isReproducibleWorldManifest(manifest)) throw new Error('world manifest does not reproduce the current medieval generation contract')
  return canonicalJson(manifest)
}

export const isReproducibleWorldManifest = (value: unknown): value is WorldManifest => {
  if (!record(value) || value.version !== FOUNDATION_MANIFEST_VERSION || !hasOnlyKeys(value, ['version', 'creation']) || !record(value.creation)) return false
  const creation = value.creation
  if (creation.version !== WORLD_CREATION_PROVENANCE_VERSION || typeof creation.seed !== 'string' || !creation.seed || normalizeSeed(creation.seed) !== creation.seed || typeof creation.configurationFingerprint !== 'string' || !record(creation.validationHistory) || creation.validationHistory.version !== WORLD_MANIFEST_VALIDATION_HISTORY_VERSION || !isReproducibleGenerationDiagnostics(creation.seed, creation.selectedConfiguration, creation.resolvedConfiguration, creation.validationHistory.generation)) return false
  const resolvedConfiguration = creation.resolvedConfiguration as WorldGenerationConfig
  if (creation.configurationFingerprint !== generationConfigurationFingerprint(resolvedConfiguration)) return false
  const contractVersions = creation.contractVersions
  if (!record(contractVersions) || contractVersions.foundationGenerator !== FOUNDATION_GENERATOR_VERSION || contractVersions.initialWorldGenerator !== INITIAL_WORLD_GENERATOR_VERSION || contractVersions.initialWorldDiagnostics !== INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION || contractVersions.frontierContract !== FRONTIER_CONTRACT_VERSION || contractVersions.contentSafetyPolicy !== MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION || !isMedievalContentSafetyAudit(creation.contentSafetyAudit)) return false
  const expectedCreation = expectedCreationProvenance(creation.seed, creation.selectedConfiguration as WorldCreationProvenance['selectedConfiguration'], resolvedConfiguration)
  return expectedCreation !== undefined && equivalent(creation, expectedCreation)
}

const worldFromCreationProvenance = (
  creation: WorldCreationProvenance,
  precomputedState?: ExpectedFoundationState
): FoundationWorld => {
  const state = precomputedState ?? expectedFoundationState(creation.seed, creation.resolvedConfiguration)
  if (!state) throw new Error('creation provenance does not identify a valid foundation world')
  const temporal = createMedievalTemporalState(temporalProvenanceForCreation(creation))
  return {
    version: 15,
    id: idForCreationProvenance(creation),
    status: 'active',
    manifest: {
      version: FOUNDATION_MANIFEST_VERSION,
      creation
    },
    jomon: state.jomon,
    crew: state.crew,
    initialWorld: state.initialWorld,
    state: createMedievalWorldState({
      seed: creation.seed,
      configuration: creation.resolvedConfiguration,
      initialWorld: state.initialWorld,
      jomon: state.jomon,
      crew: state.crew,
      frontier: state.frontier,
      temporal
    })
  }
}

export const createFoundationWorld = (input: FoundationWorldInput = {}): FoundationWorld => {
  const seed = normalizeSeed(input.seed)
  const configurationResolution = resolveWorldGenerationConfig(input.configuration)
  if (configurationResolution.status !== 'valid') throw new InvalidWorldGenerationConfigurationError(configurationResolution.issues)
  const state = expectedFoundationState(seed, configurationResolution.configuration, input.onGenerationProgress)
  if (!state) throw new Error('foundation world creation requires an unselected courier state')
  const creation = creationProvenanceFor(seed, configurationResolution.selectedConfiguration, configurationResolution.configuration, state)
  return worldFromCreationProvenance(creation, state)
}

export const recreateFoundationWorld = (manifest: WorldManifest): FoundationWorld => {
  if (!isReproducibleWorldManifest(manifest)) throw new Error('world manifest does not reproduce the current medieval generation contract')
  return worldFromCreationProvenance(manifest.creation)
}

/** Storage uses this after structural validation so saved worlds cannot bypass the policy. */
export const foundationWorldContentSatisfiesSafetyPolicy = (world: FoundationWorld): boolean => {
  try {
    const staticState = expectedFoundationState(world.manifest.creation.seed, world.manifest.creation.resolvedConfiguration)
    if (!staticState) return false
    return equivalent(world.jomon, staticState.jomon)
      && equivalent(world.crew, staticState.crew)
      && contentSafetyAuditMatches(foundationContentRecords(world.manifest.creation.labelContentSafety, world.jomon, world.crew, staticState.causalHistory, world.initialWorld, staticState.frontier), world.manifest.creation.contentSafetyAudit)
  } catch { return false }
}

/** Storage uses this to reject a modified region even when its tags still look safe. */
export const foundationWorldInitialWorldMatchesManifest = (world: FoundationWorld): boolean => {
  try {
    const expected = generateInitialWorld(world.manifest.creation.seed, world.manifest.creation.resolvedConfiguration)
    return equivalent(world.initialWorld, expected.world) && equivalent(world.manifest.creation.initialWorld, initialWorldIdentityFor(expected.world)) && equivalent(world.manifest.creation.validationHistory.initialWorld, expected.diagnostics)
  } catch { return false }
}

/** The mutable scheduler is bound to immutable creation evidence but never modifies it. */
export const foundationWorldTemporalStateMatches = (world: FoundationWorld): boolean => {
  try {
    const provenance = temporalProvenanceForCreation(world.manifest.creation)
    if (!isMedievalTemporalState(world.state.temporal, provenance)) return false
    return isMedievalWorldState({
      seed: world.manifest.creation.seed,
      configuration: world.manifest.creation.resolvedConfiguration,
      initialWorld: world.initialWorld,
      jomon: world.jomon,
      crew: world.crew
    }, world.state)
  } catch { return false }
}

/** The mutable coordinate belongs to state, while valid cells remain plan-derived. */
export const foundationWorldNavigationStateMatches = (world: FoundationWorld): boolean => {
  const courierId = world.state.courier.activeCourierId
  const navigation = world.state.navigation
  if (courierId === undefined) return navigation.courierId === undefined && navigation.coordinate === undefined
  return navigation.courierId === courierId
    && navigation.coordinate !== undefined
    && isWalkableJomonDeckCoordinate(world, navigation.coordinate)
}

/**
 * Catch-up is a separate proof from temporal shape: it must account for each
 * canonical cadence window implied by the current deterministic fidelity plan.
 */
export const foundationWorldCatchUpStateMatches = (world: FoundationWorld): boolean => {
  try {
    const courierId = world.state.courier.activeCourierId
    if (courierId === undefined) {
      return validateSimulationCatchUpState({
        worldId: world.id,
        creationDigest: world.manifest.creation.digest,
        worldTime: world.state.temporal.worldTime,
        personIds: world.state.people.records.map(person => person.id),
        marketIds: world.state.markets.markets.map(market => market.id),
        institutionIds: world.state.institutions.registry.map(institution => institution.id)
      }, world.state.simulation).length === 0
    }
    const plan = createFidelityPlanForVerifiedWorld(world, courierId)
    return validateSimulationCatchUpPlanState(world.state.simulation, plan).length === 0
  } catch { return false }
}

/** Autonomy observations are a canonical view of current fidelity/catch-up state. */
export const foundationWorldAutonomyStateMatches = (world: FoundationWorld): boolean => {
  try {
    const courierId = world.state.courier.activeCourierId
    if (courierId === undefined) return world.state.autonomy.observations.length === 0
    const plan = createFidelityPlanForVerifiedWorld(world, courierId)
    return validateAutonomyPlanState({
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: world.state.temporal.worldTime,
      activeCourierId: courierId,
      people: world.state.people.records,
      delegation: world.state.delegation,
      era: world.state.era,
      simulation: world.state.simulation,
      plan
    }, world.state.autonomy).length === 0
  } catch { return false }
}

const worldEraContextFor = (world: FoundationWorld, worldTime = world.state.temporal.worldTime): WorldEraContext => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  eraPace: world.manifest.creation.resolvedConfiguration.eraPace,
  worldTime,
  jomonVesselId: world.jomon.id,
  jomonPropIds: world.jomon.props.map(prop => prop.id)
})

const causalHistoryContextFor = (world: FoundationWorld): CausalHistoryContext => ({ worldId: world.id, creationDigest: world.manifest.creation.digest })

const stateFromProjection = (world: FoundationWorld, projection: CausalReplayProjection, causalHistoryState = world.state.causalHistory): MedievalWorldState => createMedievalWorldState({
  seed: world.manifest.creation.seed,
  configuration: world.manifest.creation.resolvedConfiguration,
  initialWorld: world.initialWorld,
  jomon: world.jomon,
  crew: world.crew,
  frontier: world.state.geography.frontier,
  temporal: projection.temporal,
  ...(projection.courier.initialCourierId === undefined ? {} : { initialCourierId: projection.courier.initialCourierId }),
  ...(projection.courier.activeCourierId === undefined ? {} : { activeCourierId: projection.courier.activeCourierId }),
  ...(projection.courier.initialCourierId === undefined ? {} : { departedCourierIds: projection.courier.departedCourierIds ?? [] }),
  ...(projection.courier.initialCourierId !== undefined && projection.courier.activeCourierId === undefined ? { terminalCrewExtinct: true as const } : {}),
  ...(projection.navigation === undefined ? {} : { navigationState: projection.navigation }),
  jomonState: projection.jomon,
  peopleState: projection.people as WorldPeopleState,
  simulationState: projection.simulation,
  eraState: projection.era,
  delegationState: projection.delegation as WorldDelegationState,
  autonomyState: projection.autonomy as WorldAutonomyState,
  socialMemoryState: projection.socialMemory as WorldSocialMemoryState,
  settlementTradingState: projection.settlementTrading,
  hearthfordWorksiteState: projection.hearthfordWorksite ?? world.state.hearthfordWorksite,
  causalHistoryState
})

const worldFromProjection = (world: FoundationWorld, projection: CausalReplayProjection, causalHistoryState = world.state.causalHistory): FoundationWorld => ({
  ...cloneWorld(world),
  state: stateFromProjection(world, projection, causalHistoryState)
})

/** The shared pure reducer for an initial-courier journal command. */
const selectCourierProjection = (world: FoundationWorld, projection: CausalReplayProjection, courierId: string): CausalReplayProjection => {
  if (projection.courier.initialCourierId !== undefined) throw new Error('initial courier has already been selected')
  if (projection.temporal.worldTime !== 0 || projection.temporal.actionSequence !== 0 || projection.temporal.pendingEvents.length !== 0) throw new Error('initial courier must be selected before time-bearing actions')
  const candidate = initialHouseholdActiveCrew(world.crew).find(member => member.id === courierId)
  const person = projection.people.records.find(candidatePerson => candidatePerson.id === courierId)
  if (!candidate || person?.life.status !== 'living' || person.work.availability !== 'available') throw new Error('selected courier must be an eligible living available crew member')
  return causalReplayProjection({ ...projection, courier: { version: 3, initialCourierId: courierId, activeCourierId: courierId, departedCourierIds: [] }, navigation: { version: 1, courierId, coordinate: canonicalJomonDeckSpawn(world) } })
}

/** The shared pure reducer for an accepted time-bearing journal command. */
const advanceTimeProjection = (world: FoundationWorld, projection: CausalReplayProjection, command: TemporalCommand | unknown): CausalReplayProjection => {
  const transition = advanceMedievalTemporalState(projection.temporal, command)
  const eraPlan = advanceWorldEraForTemporalAction(projection.era, worldEraContextFor(world, transition.state.worldTime), transition.action)
  // Fidelity is selected from the valid state at the action boundary. The
  // reducer then folds its windows through the action; task completion and
  // any resulting tier change are reconciled at the action's end below.
  const beforeAction = worldFromProjection(world, projection)
  const courierId = beforeAction.state.courier.activeCourierId
  if (courierId === undefined) throw new Error('time-bearing simulation requires an active courier')
  const beforePlan = createFidelityPlanForVerifiedWorld(beforeAction, courierId)
  const catchUp = advanceSimulationCatchUpState(projection.simulation, beforePlan, transition.action)
  const delegation = advanceDelegatedTasks({
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: transition.state.worldTime,
    people: projection.people.records
  }, projection.delegation, projection.people.records, projection.socialMemory, transition.action)
  const resolvedSimulation = delegation.resolvedTaskIds.reduce((state, taskId) => {
    const task = delegation.state.tasks.find(candidate => candidate.id === taskId)!
    return resolveDelegatedWorkPlaceholder(state, {
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: transition.state.worldTime,
      personIds: delegation.people.map(person => person.id),
      marketIds: world.state.markets.markets.map(market => market.id),
      institutionIds: world.state.institutions.registry.map(institution => institution.id)
    }, taskId, task.outcome!.atWorldTime)
  }, catchUp.state)
  const finalProjection = causalReplayProjection({
    courier: projection.courier,
    jomon: projection.jomon,
    ...(projection.navigation === undefined ? {} : { navigation: projection.navigation }),
    people: { version: 5, records: delegation.people },
    temporal: transition.state,
    simulation: resolvedSimulation,
    era: eraPlan,
    delegation: delegation.state,
    // This transient planning view must carry end-time processing cursors;
    // the real reducer below folds the prior autonomy state before return.
    autonomy: createAutonomyState(delegation.people, transition.state.worldTime),
    socialMemory: delegation.socialMemory,
    settlementTrading: projection.settlementTrading,
    ...(projection.hearthfordWorksite === undefined ? {} : { hearthfordWorksite: projection.hearthfordWorksite })
  })
  const finalWorld = worldFromProjection(world, finalProjection)
  const simulation = reconcileSimulationCatchUpPlanState(
    resolvedSimulation,
    createFidelityPlanForVerifiedWorld(finalWorld, courierId)
  )
  const autonomyWorld = worldFromProjection(world, causalReplayProjection({ ...finalProjection, simulation }))
  const finalPlan = createFidelityPlanForVerifiedWorld(autonomyWorld, courierId)
  const autonomy = advanceAutonomyState(
    projection.autonomy,
    {
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: transition.action.startedAtWorldTime,
      activeCourierId: courierId,
      people: projection.people.records,
      delegation: projection.delegation,
      era: projection.era,
      simulation: projection.simulation,
      plan: beforePlan
    },
    {
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: transition.action.atWorldTime,
      activeCourierId: courierId,
      people: delegation.people,
      delegation: delegation.state,
      era: eraPlan,
      simulation,
      plan: finalPlan
    },
    transition.action
  )
  return causalReplayProjection({
    courier: projection.courier,
    jomon: projection.jomon,
    ...(projection.navigation === undefined ? {} : { navigation: projection.navigation }),
    people: { version: 5, records: autonomy.people },
    temporal: transition.state,
    simulation,
    era: eraPlan,
    delegation: delegation.state,
    autonomy: autonomy.state,
    socialMemory: delegation.socialMemory,
    settlementTrading: projection.settlementTrading,
    ...(projection.hearthfordWorksite === undefined ? {} : { hearthfordWorksite: projection.hearthfordWorksite })
  })
}

/** The shared pure reducer for a completed physical Jomon-growth command. */
const recordGrowthProjection = (world: FoundationWorld, projection: CausalReplayProjection, evidence: DurableJomonGrowthEvidence): CausalReplayProjection => {
  const era = recordDurableJomonGrowthEvidence(projection.era, worldEraContextFor(world, projection.temporal.worldTime), evidence)
  const advanced = causalReplayProjection({ ...projection, era })
  const courierId = advanced.courier.activeCourierId
  if (courierId === undefined) return advanced
  // An era profile is an input to choice explanation, so zero-time durable
  // growth reprojects observations without advancing needs or scheduling work.
  const advancedWorld = worldFromProjection(world, advanced)
  const plan = createFidelityPlanForVerifiedWorld(advancedWorld, courierId)
  const autonomy = reconcileAutonomyState(advanced.autonomy, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: advanced.temporal.worldTime,
    activeCourierId: courierId,
    people: advanced.people.records,
    delegation: advanced.delegation,
    era,
    simulation: advanced.simulation,
    plan
  })
  return causalReplayProjection({ ...advanced, autonomy })
}

/** Shared pure reducer for the zero-time tavern-ledger perspective transfer. */
const switchTavernCourierProjection = (
  world: FoundationWorld,
  projection: CausalReplayProjection,
  payload: { fromCourierId: string; toCourierId: string; propId: 'prop:task-ledger'; coordinate: { column: number; row: number } },
  causalSequence: number
): CausalReplayProjection => {
  const assessment = assessTavernCourierSwitchForVerifiedWorld(world, projection)
  if (assessment.status !== 'available') throw new Error(`tavern courier switch is unavailable: ${assessment.reason}`)
  if (payload.fromCourierId !== assessment.current.id
    || !assessment.candidates.some(candidate => candidate.id === payload.toCourierId)
    || payload.propId !== assessment.source.propId
    || payload.coordinate.column !== assessment.source.coordinate.column
    || payload.coordinate.row !== assessment.source.coordinate.row) throw new Error('tavern courier switch does not match the validated ledger operation')
  const switched = causalReplayProjection({
    ...projection,
    jomon: {
      ...projection.jomon,
      propActions: recordVesselPropAction(world.jomon, projection.jomon.propActions, 'prop:task-ledger', {
        kind: 'tavern-courier-switched',
        recordedAtWorldTime: projection.temporal.worldTime,
        causalSequence
      })
    },
    courier: { version: 3, initialCourierId: projection.courier.initialCourierId!, activeCourierId: payload.toCourierId, departedCourierIds: structuredClone(projection.courier.departedCourierIds ?? []) },
    navigation: { version: WORLD_DECK_NAVIGATION_STATE_VERSION, courierId: payload.toCourierId, coordinate: structuredClone(assessment.source.coordinate) }
  })
  // Fidelity and autonomy are current-courier projections. Reconcile them at
  // this zero-time boundary so no old-courier planning state survives.
  const projectedWorld = (next: CausalReplayProjection): FoundationWorld => ({
    ...world,
    state: {
      ...world.state,
      courier: structuredClone(next.courier),
      navigation: structuredClone(next.navigation!),
      people: structuredClone(next.people),
      temporal: structuredClone(next.temporal),
      simulation: structuredClone(next.simulation),
      era: structuredClone(next.era),
      delegation: structuredClone(next.delegation),
      autonomy: structuredClone(next.autonomy),
      socialMemory: structuredClone(next.socialMemory)
      ,jomon: structuredClone(next.jomon)
    }
  })
  const simulation = reconcileSimulationCatchUpPlanState(switched.simulation, createFidelityPlanForVerifiedWorld(projectedWorld(switched), payload.toCourierId))
  const reconciledWorld = projectedWorld(causalReplayProjection({ ...switched, simulation }))
  const plan = createFidelityPlanForVerifiedWorld(reconciledWorld, payload.toCourierId)
  const autonomy = reconcileAutonomyState(switched.autonomy, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: switched.temporal.worldTime,
    activeCourierId: payload.toCourierId,
    people: switched.people.records,
    delegation: switched.delegation,
    era: switched.era,
    simulation,
    plan
  })
  return causalReplayProjection({ ...switched, simulation, autonomy })
}

/** A reducer-only view for reprojecting courier-dependent fidelity at one minute. */
const projectedFoundationWorld = (world: FoundationWorld, projection: CausalReplayProjection): FoundationWorld => ({
  ...world,
  state: {
    ...world.state,
    courier: structuredClone(projection.courier),
    navigation: structuredClone(projection.navigation ?? { version: WORLD_DECK_NAVIGATION_STATE_VERSION }),
    people: structuredClone(projection.people),
    temporal: structuredClone(projection.temporal),
    simulation: structuredClone(projection.simulation),
    era: structuredClone(projection.era),
    delegation: structuredClone(projection.delegation),
    autonomy: structuredClone(projection.autonomy),
    socialMemory: structuredClone(projection.socialMemory)
    ,jomon: structuredClone(projection.jomon)
  }
})

const continuityAssessmentFor = (
  world: FoundationWorld,
  projection: CausalReplayProjection,
  confirmation: CourierContinuityConfirmation
): CourierContinuityAssessment => assessCourierContinuityLoss({
  version: 1,
  peopleContext: {
    seed: world.manifest.creation.seed,
    configurationFingerprint: world.manifest.creation.configurationFingerprint,
    initialWorld: world.initialWorld,
    frontier: world.state.geography.frontier,
    jomon: world.jomon,
    crew: world.crew,
    siteIds: world.state.sites.sites.map(site => site.id),
    worldTime: projection.temporal.worldTime
  },
  people: projection.people.records,
  courier: {
    ...(projection.courier.initialCourierId === undefined ? {} : { initialCourierId: projection.courier.initialCourierId }),
    ...(projection.courier.activeCourierId === undefined ? {} : { activeCourierId: projection.courier.activeCourierId }),
    departedCourierIds: projection.courier.departedCourierIds ?? []
  },
  navigation: projection.navigation === undefined
    ? {}
    : {
        ...(projection.navigation.courierId === undefined ? {} : { courierId: projection.navigation.courierId }),
        ...(projection.navigation.coordinate === undefined ? {} : { coordinate: projection.navigation.coordinate })
      },
  confirmation
})

/** Shared pure reducer for a confirmed permanent loss and its deterministic continuity result. */
const resolveCourierLossProjection = (
  world: FoundationWorld,
  projection: CausalReplayProjection,
  payload: { confirmation: CourierContinuityConfirmation; finalization: 'continue' | 'crew-extinction'; successorId?: string }
): CausalReplayProjection => {
  const assessment = continuityAssessmentFor(world, projection, payload.confirmation)
  const expectedFinalization = assessment.finalization.kind === 'continue' ? 'continue' : 'crew-extinction'
  const expectedSuccessorId = assessment.finalization.kind === 'continue' ? assessment.finalization.successorId : undefined
  if (payload.finalization !== expectedFinalization || payload.successorId !== expectedSuccessorId) throw new Error('courier loss finalization does not match canonical continuity evidence')
  const navigation = projection.navigation
  if (!navigation?.coordinate || navigation.courierId !== payload.confirmation.courierId) throw new Error('courier loss requires the current authoritative deck coordinate')
  const resolved = causalReplayProjection({
    ...projection,
    courier: assessment.finalization.kind === 'continue'
      ? { version: 3, initialCourierId: projection.courier.initialCourierId!, activeCourierId: assessment.finalization.successorId, departedCourierIds: structuredClone(assessment.departedCourierIds) }
      : { version: 3, initialCourierId: projection.courier.initialCourierId!, departedCourierIds: structuredClone(assessment.departedCourierIds) },
    navigation: assessment.finalization.kind === 'continue'
      ? { version: WORLD_DECK_NAVIGATION_STATE_VERSION, courierId: assessment.finalization.successorId, coordinate: structuredClone(navigation.coordinate) }
      : { version: WORLD_DECK_NAVIGATION_STATE_VERSION },
    people: { version: 5, records: assessment.people },
    // A fresh state removes stale observations for a deceased/departed prior courier.
    autonomy: createAutonomyState(assessment.people, projection.temporal.worldTime)
  })
  if (assessment.finalization.kind === 'crew-extinction') return resolved
  const successorId = assessment.finalization.successorId
  const simulation = reconcileSimulationCatchUpPlanState(resolved.simulation, createFidelityPlanForVerifiedWorld(projectedFoundationWorld(world, resolved), successorId))
  const withSimulation = causalReplayProjection({ ...resolved, simulation })
  const plan = createFidelityPlanForVerifiedWorld(projectedFoundationWorld(world, withSimulation), successorId)
  const autonomy = reconcileAutonomyState(withSimulation.autonomy, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: withSimulation.temporal.worldTime,
    activeCourierId: successorId,
    people: withSimulation.people.records,
    delegation: withSimulation.delegation,
    era: withSimulation.era,
    simulation,
    plan
  })
  return causalReplayProjection({ ...withSimulation, autonomy })
}

/** Shared pure reducer for a successful one-minute local deck step. */
const moveDeckProjection = (world: FoundationWorld, projection: CausalReplayProjection, payload: { actionId: string; courierId: string; direction: JomonDeckMovementDirection; from: JomonDeckCoordinate; to: JomonDeckCoordinate }): CausalReplayProjection => {
  const navigation = projection.navigation
  if (!navigation?.coordinate || navigation.courierId !== payload.courierId || projection.courier.activeCourierId !== payload.courierId) throw new Error('deck movement requires the active courier position')
  if (navigation.coordinate.column !== payload.from.column || navigation.coordinate.row !== payload.from.row) throw new Error('deck movement source position does not match replay state')
  const assessment = assessJomonDeckStep(world, navigation.coordinate, payload.direction)
  if (assessment.status !== 'moved' || assessment.to.column !== payload.to.column || assessment.to.row !== payload.to.row) throw new Error('deck movement is not a valid plan step')
  const advanced = advanceTimeProjection(world, projection, {
    id: payload.actionId,
    kind: 'movement',
    durationMinutes: 1,
    contentSafety: classifyMedievalContent('event', ['adult-labour', 'navigation'], 'adults-only', ['data'])
  })
  return causalReplayProjection({ ...advanced, navigation: { version: 1, courierId: payload.courierId, coordinate: structuredClone(payload.to) } })
}

/** Shared pure zero-time reducer for one non-ledger physical station acknowledgement. */
const recordVesselStationReadoutProjection = (
  world: FoundationWorld,
  projection: CausalReplayProjection,
  propId: VesselStationReadoutPropId,
  causalSequence: number
): CausalReplayProjection => {
  const projected = projectedFoundationWorld(world, projection)
  const operation = assessVesselPropOperationForVerifiedWorld(projected, projection, propId)
  if (operation.proximity !== 'at-anchor' || operation.availability !== 'readout') throw new Error('station readout requires exact source-anchor occupancy')
  const readout = createVesselStationReadoutForVerifiedWorld(projected, propId)
  if (readout.source.propId !== propId || readout.source.propBindingId !== operation.source.propBindingId) throw new Error('station readout source does not match current prop operation')
  return causalReplayProjection({
    ...projection,
    jomon: {
      ...projection.jomon,
      propActions: recordVesselPropAction(world.jomon, projection.jomon.propActions, propId, {
        kind: 'station-readout-recorded',
        recordedAtWorldTime: projection.temporal.worldTime,
        causalSequence
      })
    }
  })
}

/** All physical cargo handling stays at the existing source-backed hold rack. */
const cargoHoldProjection = (world: FoundationWorld, projection: CausalReplayProjection) => {
  const projected = projectedFoundationWorld(world, projection)
  const operation = assessVesselPropOperationForVerifiedWorld(projected, projection, 'prop:cargo-hold-rack')
  if (operation.proximity !== 'at-anchor' || operation.availability !== 'readout') throw new Error('cargo handling requires exact cargo-hold anchor occupancy')
  const readout = createVesselStationReadoutForVerifiedWorld(projected, 'prop:cargo-hold-rack')
  if (readout.source.propId !== 'prop:cargo-hold-rack' || readout.source.propBindingId !== operation.source.propBindingId) throw new Error('cargo hold source does not match current prop operation')
  return projected
}

const withCargoAction = (
  world: FoundationWorld,
  projection: CausalReplayProjection,
  cargo: CausalReplayProjection['jomon']['cargo'],
  kind: 'vessel-cargo-loaded' | 'vessel-cargo-unloaded' | 'vessel-cargo-failure-resolved' | 'vessel-cargo-recovered',
  causalSequence: number
): CausalReplayProjection => causalReplayProjection({
  ...projection,
  jomon: {
    ...projection.jomon,
    cargo,
    propActions: recordVesselPropAction(world.jomon, projection.jomon.propActions, 'prop:cargo-hold-rack', {
      kind,
      recordedAtWorldTime: projection.temporal.worldTime,
      causalSequence
    })
  }
})

const loadVesselCargoProjection = (world: FoundationWorld, projection: CausalReplayProjection, payload: { propId: 'prop:cargo-hold-rack'; commodityId: JomonCommodityId; quantity: number }, causalSequence: number): CausalReplayProjection => {
  if (payload.propId !== 'prop:cargo-hold-rack') throw new Error('cargo loading requires the cargo hold rack')
  cargoHoldProjection(world, projection)
  return withCargoAction(world, projection, loadVesselCargo(projection.jomon.cargo, projection.jomon.capacity.cargoUnits, causalSequence, payload.commodityId, payload.quantity), 'vessel-cargo-loaded', causalSequence)
}

const unloadVesselCargoProjection = (world: FoundationWorld, projection: CausalReplayProjection, payload: { propId: 'prop:cargo-hold-rack'; cargoId: string }, causalSequence: number): CausalReplayProjection => {
  if (payload.propId !== 'prop:cargo-hold-rack') throw new Error('cargo unloading requires the cargo hold rack')
  cargoHoldProjection(world, projection)
  return withCargoAction(world, projection, unloadVesselCargo(projection.jomon.cargo, projection.jomon.capacity.cargoUnits, payload.cargoId), 'vessel-cargo-unloaded', causalSequence)
}

const resolveVesselCargoFailureProjection = (world: FoundationWorld, projection: CausalReplayProjection, payload: { propId: 'prop:cargo-hold-rack'; cargoId: string; outcome: VesselCargoFailureOutcome }, causalSequence: number): CausalReplayProjection => {
  if (payload.propId !== 'prop:cargo-hold-rack') throw new Error('cargo failure requires the cargo hold rack')
  cargoHoldProjection(world, projection)
  return withCargoAction(world, projection, resolveVesselCargoFailure(projection.jomon.cargo, projection.jomon.capacity.cargoUnits, payload.cargoId, payload.outcome), 'vessel-cargo-failure-resolved', causalSequence)
}

const recoverVesselCargoProjection = (world: FoundationWorld, projection: CausalReplayProjection, payload: { propId: 'prop:cargo-hold-rack'; cargoId: string }, causalSequence: number): CausalReplayProjection => {
  if (payload.propId !== 'prop:cargo-hold-rack') throw new Error('cargo recovery requires the cargo hold rack')
  cargoHoldProjection(world, projection)
  return withCargoAction(world, projection, recoverVesselCargo(projection.jomon.cargo, projection.jomon.capacity.cargoUnits, payload.cargoId), 'vessel-cargo-recovered', causalSequence)
}

/** The public tally is a local physical source, not a route, market, or site map. */
const settlementTradeSourceProjection = (world: FoundationWorld, projection: CausalReplayProjection): FoundationWorld => {
  const projected = projectedFoundationWorld(world, projection)
  const activeCourierId = projected.state.courier.activeCourierId
  const navigation = projected.state.navigation
  if (activeCourierId === undefined || navigation.courierId !== activeCourierId || navigation.coordinate === undefined) throw new Error('settlement trade requires an active courier at the public tally')
  if (!settlementTradingAtSourceAnchor(deriveJomonDeckPlanForVerifiedWorld(projected), navigation.coordinate)) throw new Error('settlement trade requires exact public-tally occupancy')
  return projected
}

/** One offered contract can be accepted or refused only at its source tally. */
const settlementTradeDecisionProjection = (
  world: FoundationWorld,
  projection: CausalReplayProjection,
  kind: 'settlement-trade-accepted' | 'settlement-trade-refused',
  causalSequence: number
): CausalReplayProjection => {
  settlementTradeSourceProjection(world, projection)
  const settlementTrading = kind === 'settlement-trade-accepted'
    ? acceptSettlementTradeState(projection.settlementTrading, projection.temporal.worldTime, causalSequence)
    : refuseSettlementTradeState(projection.settlementTrading, projection.temporal.worldTime, causalSequence)
  return causalReplayProjection({ ...projection, settlementTrading })
}

/** Delivery converts the accepted physical burden into one canonical hold lot. */
const deliverSettlementTradeProjection = (
  world: FoundationWorld,
  projection: CausalReplayProjection,
  causalSequence: number
): CausalReplayProjection => {
  cargoHoldProjection(world, projection)
  const settlementTrading = deliverSettlementTradeState(projection.settlementTrading, projection.temporal.worldTime, causalSequence)
  const cargo = loadVesselCargo(projection.jomon.cargo, projection.jomon.capacity.cargoUnits, causalSequence, 'commodity:ironwork', 1)
  return withCargoAction(world, causalReplayProjection({ ...projection, settlementTrading }), cargo, 'vessel-cargo-loaded', causalSequence)
}

/** Resolves the named mill lease only from its visible shore-side worksite. */
const resolveHearthfordWorksiteProjection = (
  world: FoundationWorld,
  projection: CausalReplayProjection,
  action: TimeBearingTemporalAction,
  causalSequence: number
): CausalReplayProjection => {
  const parsed = hearthfordWorksiteResolutionFromAction(action)
  if (!parsed || parsed.causalSequence !== causalSequence) throw new Error('worksite resolution action is not canonical')
  const worksite = projection.hearthfordWorksite ?? createHearthfordWorksiteState(world.manifest.creation.seed)
  if (Object.hasOwn(worksite, 'resolution')) throw new Error('Hearthford mill lease has already been resolved')
  const navigation = projection.navigation
  const anchor = hearthfordWorksiteAnchorFor(worksite)
  if (!navigation?.coordinate || navigation.courierId !== projection.courier.activeCourierId
    || navigation.coordinate.column !== anchor.column || navigation.coordinate.row !== anchor.row) throw new Error('worksite resolution requires exact mill-race occupancy')
  const contract = projection.settlementTrading.contracts[0]
  if (contract.status !== 'delivered') throw new Error('mill lease resolution requires the delivered Hearthford burden')
  const deliveredCargoId = contract.status === 'delivered' ? contract.cargoId : undefined
  if (parsed.kind === 'ironwork-fitted') {
    const lot = projection.jomon.cargo.lots.find(candidate => candidate.id === contract.cargoId)
    if (!lot || lot.commodityId !== 'commodity:ironwork' || lot.quantity !== 1 || lot.condition !== 'sound' || lot.status !== 'in-hold') throw new Error('ironwork fitting requires the sound delivered cargo lot')
  }
  const advanced = advanceTimeProjection(world, projection, action)
  const resolved = resolveHearthfordWorksite(worksite, parsed.kind, advanced.temporal.worldTime, causalSequence)
  const cargo = parsed.kind === 'ironwork-fitted'
    ? unloadVesselCargo(advanced.jomon.cargo, advanced.jomon.capacity.cargoUnits, deliveredCargoId!)
    : advanced.jomon.cargo
  return causalReplayProjection({
    ...advanced,
    hearthfordWorksite: resolved,
    jomon: { ...advanced.jomon, cargo }
  })
}

const delegationContextFor = (world: FoundationWorld, projection: CausalReplayProjection): {
  worldId: string
  creationDigest: string
  worldTime: number
  people: WorldPeopleState['records']
} => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: projection.temporal.worldTime,
  people: projection.people.records
})

/** Shared pure offer reducer: conversation gates the one-minute commitment action. */
const offerDelegationProjection = (world: FoundationWorld, projection: CausalReplayProjection, offer: DelegationOfferInput | unknown, replay = false): CausalReplayProjection => {
  if (!isDelegationOfferInput(offer)) throw new Error('delegation offer is invalid')
  const beforeOffer = worldFromProjection(world, projection)
  const assessmentRequest = {
    version: CONVERSATION_CONTRACT_VERSION,
    courierId: offer.courierId,
    recipientId: offer.recipientId,
    proposal: offer.proposal
  } as const
  const assessment: ConversationAssessment = replay
    ? assessCourierConversationForValidatedReplay(beforeOffer, assessmentRequest)
    : assessCourierConversation(beforeOffer, assessmentRequest)
  const afterAction = advanceTimeProjection(world, projection, delegationOfferTemporalAction(offer))
  const transition = offerDelegationTransition(delegationContextFor(world, afterAction), afterAction.delegation, afterAction.people.records, afterAction.socialMemory, offer, assessment)
  const simulation = transition.task.status === 'in-progress'
    ? withDelegatedWorkPlaceholder(afterAction.simulation, {
        worldId: world.id,
        creationDigest: world.manifest.creation.digest,
        worldTime: afterAction.temporal.worldTime,
        personIds: transition.people.map(person => person.id),
        marketIds: world.state.markets.markets.map(market => market.id),
        institutionIds: world.state.institutions.registry.map(institution => institution.id)
      }, delegatedWorkPlaceholderForTask(transition.task))
    : afterAction.simulation
  const offeredProjection = causalReplayProjection({
    ...afterAction,
    people: { version: 5, records: transition.people },
    simulation,
    delegation: transition.state,
    socialMemory: transition.socialMemory
  })
  const offeredWorld = worldFromProjection(world, offeredProjection)
  const reconciledSimulation = reconcileSimulationCatchUpPlanState(simulation, createFidelityPlanForVerifiedWorld(offeredWorld, offer.courierId))
  const autonomyWorld = worldFromProjection(world, causalReplayProjection({ ...offeredProjection, simulation: reconciledSimulation }))
  const plan = createFidelityPlanForVerifiedWorld(autonomyWorld, offer.courierId)
  const autonomy = reconcileAutonomyState(offeredProjection.autonomy, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: offeredProjection.temporal.worldTime,
    activeCourierId: offer.courierId,
    people: transition.people,
    delegation: transition.state,
    era: offeredProjection.era,
    simulation: reconciledSimulation,
    plan
  })
  return causalReplayProjection({ ...offeredProjection, simulation: reconciledSimulation, autonomy })
}

/** Shared pure interruption reducer. Completion wins if its due minute is crossed. */
const interruptDelegationProjection = (world: FoundationWorld, projection: CausalReplayProjection, interruption: DelegationInterruptionInput | unknown): CausalReplayProjection => {
  if (!isDelegationInterruptionInput(interruption)) throw new Error('delegation interruption is invalid')
  if (projection.courier.activeCourierId !== interruption.courierId) throw new Error('only the active courier can interrupt delegated work')
  const afterAction = advanceTimeProjection(world, projection, delegationInterruptionTemporalAction(interruption))
  const transition = interruptDelegatedTask(delegationContextFor(world, afterAction), afterAction.delegation, afterAction.people.records, afterAction.socialMemory, interruption)
  const task = transition.state.tasks.find(candidate => candidate.id === interruption.taskId)!
  const simulation = resolveDelegatedWorkPlaceholder(afterAction.simulation, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: afterAction.temporal.worldTime,
    personIds: transition.people.map(person => person.id),
    marketIds: world.state.markets.markets.map(market => market.id),
    institutionIds: world.state.institutions.registry.map(institution => institution.id)
  }, task.id, task.outcome!.atWorldTime)
  const interruptedProjection = causalReplayProjection({
    ...afterAction,
    people: { version: 5, records: transition.people },
    simulation,
    delegation: transition.state,
    socialMemory: transition.socialMemory
  })
  const interruptedWorld = worldFromProjection(world, interruptedProjection)
  const reconciledSimulation = reconcileSimulationCatchUpPlanState(simulation, createFidelityPlanForVerifiedWorld(interruptedWorld, interruption.courierId))
  const autonomyWorld = worldFromProjection(world, causalReplayProjection({ ...interruptedProjection, simulation: reconciledSimulation }))
  const plan = createFidelityPlanForVerifiedWorld(autonomyWorld, interruption.courierId)
  const autonomy = reconcileAutonomyState(interruptedProjection.autonomy, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: interruptedProjection.temporal.worldTime,
    activeCourierId: interruption.courierId,
    people: transition.people,
    delegation: transition.state,
    era: interruptedProjection.era,
    simulation: reconciledSimulation,
    plan
  })
  return causalReplayProjection({ ...interruptedProjection, simulation: reconciledSimulation, autonomy })
}

const replayCommandProjection = (world: FoundationWorld, projection: CausalReplayProjection, command: CausalCommandEvent): CausalReplayProjection => {
  if (command.kind === 'initial-courier-selected') return selectCourierProjection(world, projection, command.payload.courierId)
  if (command.kind === 'tavern-courier-switched') return switchTavernCourierProjection(world, projection, command.payload, command.sequence)
  if (command.kind === 'vessel-station-readout-recorded') return recordVesselStationReadoutProjection(world, projection, command.payload.propId, command.sequence)
  if (command.kind === 'vessel-cargo-loaded') return loadVesselCargoProjection(world, projection, command.payload, command.sequence)
  if (command.kind === 'vessel-cargo-unloaded') return unloadVesselCargoProjection(world, projection, command.payload, command.sequence)
  if (command.kind === 'vessel-cargo-failure-resolved') return resolveVesselCargoFailureProjection(world, projection, command.payload, command.sequence)
  if (command.kind === 'vessel-cargo-recovered') return recoverVesselCargoProjection(world, projection, command.payload, command.sequence)
  if (command.kind === 'settlement-trade-accepted' || command.kind === 'settlement-trade-refused') return settlementTradeDecisionProjection(world, projection, command.kind, command.sequence)
  if (command.kind === 'settlement-trade-delivered') return deliverSettlementTradeProjection(world, projection, command.sequence)
  if (command.kind === 'courier-loss-resolved') return resolveCourierLossProjection(world, projection, command.payload)
  if (command.kind === 'time-bearing-action') {
    return hearthfordWorksiteResolutionFromAction(command.payload.action) === undefined
      ? advanceTimeProjection(world, projection, command.payload.action)
      : resolveHearthfordWorksiteProjection(world, projection, command.payload.action, command.sequence)
  }
  if (command.kind === 'deck-moved') return moveDeckProjection(world, projection, command.payload)
  if (command.kind === 'durable-jomon-growth') return recordGrowthProjection(world, projection, command.payload.evidence)
  if (command.kind === 'delegation-offered') return offerDelegationProjection(world, projection, command.payload.offer, true)
  if (command.kind === 'delegation-interrupted') return interruptDelegationProjection(world, projection, command.payload.interruption)
  throw new Error('unsupported causal command')
}

/** Replays the authoritative journal through internal pure reducers for inspection/tests. */
export const replayFoundationWorldCausalHistory = (world: FoundationWorld): CausalReplayProjection => replayCausalHistory(
  causalHistoryContextFor(world),
  world.state.causalHistory,
  (projection, command) => replayCommandProjection(world, projection, command)
)

/**
 * Proves that the global journal reproduces the command-owned present state.
 * A sequence-zero checkpoint is additionally bound to immutable world genesis.
 */
export const foundationWorldCausalHistoryMatches = (world: FoundationWorld): boolean => {
  try {
    if (validateInitialHouseholdStructure(world.crew).length) return false
    const context = causalHistoryContextFor(world)
    if (world.state.causalHistory.checkpoint.sequence === 0) {
      const genesisTemporal = createMedievalTemporalState(temporalProvenanceForCreation(world.manifest.creation))
      const genesis = createMedievalWorldState({
        seed: world.manifest.creation.seed,
        configuration: world.manifest.creation.resolvedConfiguration,
        initialWorld: world.initialWorld,
        jomon: world.jomon,
        crew: world.crew,
        frontier: world.state.geography.frontier,
        temporal: genesisTemporal
      })
      if (!equivalent(world.state.causalHistory.checkpoint.projection, causalReplayProjectionForWorldState(genesis))) return false
    }
    return validateCausalHistoryReplay(context, world.state.causalHistory, causalReplayProjectionForWorldState(world.state), (projection, command) => replayCommandProjection(world, projection, command)).length === 0
  } catch { return false }
}

export const chooseInitialCourier = (world: FoundationWorld, courierId: string): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can select an initial courier')
  const projection = selectCourierProjection(world, causalReplayProjectionForWorldState(world.state), courierId)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'initial-courier-selected', { courierId })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return worldFromProjection(world, projection, history)
}

/** Renderer-independent assessment for the existing task-ledger operation. */
export const assessTavernCourierSwitch = (world: FoundationWorld): TavernCourierSwitchAssessment => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  return assessTavernCourierSwitchForVerifiedWorld(world, causalReplayProjectionForWorldState(world.state))
}

/** Applies exactly one source-bound, zero-time current-courier transition. */
export const switchTavernCourier = (world: FoundationWorld, courierId: string): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can switch its courier at the tavern ledger')
  const projection = causalReplayProjectionForWorldState(world.state)
  const assessment = assessTavernCourierSwitchForVerifiedWorld(world, projection)
  if (assessment.status !== 'available') throw new Error(`tavern courier switch is unavailable: ${assessment.reason}`)
  if (!assessment.candidates.some(candidate => candidate.id === courierId)) throw new Error('tavern courier switch does not match the validated ledger operation')
  const payload = {
    fromCourierId: assessment.current.id,
    toCourierId: courierId,
    propId: assessment.source.propId,
    coordinate: assessment.source.coordinate
  } as const
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'tavern-courier-switched', payload)
  const switched = switchTavernCourierProjection(world, projection, payload, command.sequence)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, switched)
  return worldFromProjection(world, switched, history)
}

/** Records exactly one source-backed non-ledger station acknowledgement at zero time. */
export const recordVesselStationReadout = (world: FoundationWorld, propId: VesselStationReadoutPropId): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can record a vessel station readout')
  requirePlayableActiveCourier(world, 'vessel station readout')
  const projection = causalReplayProjectionForWorldState(world.state)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'vessel-station-readout-recorded', { propId })
  const recorded = recordVesselStationReadoutProjection(world, projection, propId, command.sequence)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, recorded)
  return worldFromProjection(world, recorded, history)
}

/** Zero-time physical hold handling. Acquisition, trade, and travel stay outside this contract. */
const recordVesselCargoCommand = (
  world: FoundationWorld,
  kind: 'vessel-cargo-loaded' | 'vessel-cargo-unloaded' | 'vessel-cargo-failure-resolved' | 'vessel-cargo-recovered',
  payload: { propId: 'prop:cargo-hold-rack'; commodityId: JomonCommodityId; quantity: number } | { propId: 'prop:cargo-hold-rack'; cargoId: string } | { propId: 'prop:cargo-hold-rack'; cargoId: string; outcome: VesselCargoFailureOutcome }
): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can handle vessel cargo')
  requirePlayableActiveCourier(world, 'vessel cargo handling')
  const projection = causalReplayProjectionForWorldState(world.state)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, kind, payload)
  const next = replayCommandProjection(world, projection, command)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, next)
  return worldFromProjection(world, next, history)
}

export const loadCargoHold = (world: FoundationWorld, commodityId: JomonCommodityId, quantity: number): FoundationWorld => recordVesselCargoCommand(world, 'vessel-cargo-loaded', { propId: 'prop:cargo-hold-rack', commodityId, quantity })
export const unloadCargoHold = (world: FoundationWorld, cargoId: string): FoundationWorld => recordVesselCargoCommand(world, 'vessel-cargo-unloaded', { propId: 'prop:cargo-hold-rack', cargoId })
export const recordCargoHoldFailure = (world: FoundationWorld, cargoId: string, outcome: VesselCargoFailureOutcome): FoundationWorld => recordVesselCargoCommand(world, 'vessel-cargo-failure-resolved', { propId: 'prop:cargo-hold-rack', cargoId, outcome })
export const recoverCargoHold = (world: FoundationWorld, cargoId: string): FoundationWorld => recordVesselCargoCommand(world, 'vessel-cargo-recovered', { propId: 'prop:cargo-hold-rack', cargoId })

/** Accepts the one source-backed freight burden at the public tally, at zero time. */
export const acceptSettlementTradeContract = (world: FoundationWorld): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can accept a settlement trade contract')
  requirePlayableActiveCourier(world, 'settlement trade acceptance')
  const projection = causalReplayProjectionForWorldState(world.state)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'settlement-trade-accepted', {
    locationId: 'settlement-location:hearthford-mill-quay',
    contractId: 'settlement-contract:hearthford-mill-ironwork'
  })
  const next = settlementTradeDecisionProjection(world, projection, 'settlement-trade-accepted', command.sequence)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, next)
  return worldFromProjection(world, next, history)
}

/** Records the bounded refusal outcome at the same physical public tally. */
export const refuseSettlementTradeContract = (world: FoundationWorld): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can refuse a settlement trade contract')
  requirePlayableActiveCourier(world, 'settlement trade refusal')
  const projection = causalReplayProjectionForWorldState(world.state)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'settlement-trade-refused', {
    locationId: 'settlement-location:hearthford-mill-quay',
    contractId: 'settlement-contract:hearthford-mill-ironwork'
  })
  const next = settlementTradeDecisionProjection(world, projection, 'settlement-trade-refused', command.sequence)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, next)
  return worldFromProjection(world, next, history)
}

/** Delivers the accepted burden at Jomon's existing cargo-hold rack, at zero time. */
export const deliverSettlementTradeContract = (world: FoundationWorld): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can deliver a settlement trade contract')
  requirePlayableActiveCourier(world, 'settlement trade delivery')
  const projection = causalReplayProjectionForWorldState(world.state)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'settlement-trade-delivered', {
    locationId: 'settlement-location:hearthford-mill-quay',
    contractId: 'settlement-contract:hearthford-mill-ironwork',
    propId: 'prop:cargo-hold-rack'
  })
  const next = deliverSettlementTradeProjection(world, projection, command.sequence)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, next)
  return worldFromProjection(world, next, history)
}

/** Uses the delivered ironwork case to relieve the mill lease after 20 minutes of work. */
export const fitHearthfordMillIronwork = (world: FoundationWorld): FoundationWorld => recordHearthfordWorksiteResolution(world, 'ironwork-fitted')

/** Takes the slower lease-credit obligation without consuming the cargo case. */
export const takeHearthfordMillLeaseCredit = (world: FoundationWorld): FoundationWorld => recordHearthfordWorksiteResolution(world, 'lease-credit')

const recordHearthfordWorksiteResolution = (world: FoundationWorld, kind: HearthfordWorksiteResolutionKind): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can resolve the Hearthford mill lease')
  requirePlayableActiveCourier(world, 'Hearthford mill lease work')
  const nextSequence = world.state.causalHistory.checkpoint.sequence + world.state.causalHistory.tail.length + 1
  const action = createHearthfordWorksiteTemporalAction(kind, nextSequence)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'time-bearing-action', { action })
  const projection = causalReplayProjectionForWorldState(world.state)
  const next = replayCommandProjection(world, projection, command)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, next)
  return worldFromProjection(world, next, history)
}

export type CourierContinuityResolution =
  | { status: 'continued'; world: FoundationWorld }
  | { status: 'crew-extinction'; chronicle: WorldChronicle }

/**
 * Resolves one already-confirmed permanent loss. This has no input, renderer,
 * or storage authority: callers must provide the bounded evidence at the
 * current canonical minute and persist the returned authoritative result.
 */
/** Internal repository/reducer entry for a source already accepted by the full boundary. */
export const resolveCourierContinuityLossForVerifiedWorld = (
  world: FoundationWorld,
  confirmation: CourierContinuityConfirmation
): CourierContinuityResolution => {
  if (world.status !== 'active') throw new Error('only an active world can resolve courier continuity')
  const projection = causalReplayProjectionForWorldState(world.state)
  const assessment = continuityAssessmentFor(world, projection, confirmation)
  const payload = assessment.finalization.kind === 'continue'
    ? { confirmation: structuredClone(confirmation), finalization: 'continue' as const, successorId: assessment.finalization.successorId }
    : { confirmation: structuredClone(confirmation), finalization: 'crew-extinction' as const }
  const resolved = resolveCourierLossProjection(world, projection, payload)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'courier-loss-resolved', payload)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, resolved)
  const next = worldFromProjection(world, resolved, history)
  if (assessment.finalization.kind === 'continue') return { status: 'continued', world: next }
  return { status: 'crew-extinction', chronicle: finalizeWorldAsChronicle(next, 'crew-extinction') }
}

export const resolveCourierContinuityLoss = (
  world: FoundationWorld,
  confirmation: CourierContinuityConfirmation
): CourierContinuityResolution => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  return resolveCourierContinuityLossForVerifiedWorld(world, confirmation)
}

const requirePlayableActiveCourier = (world: FoundationWorld, operation: string): string => {
  const activeCourierId = world.state.courier.activeCourierId
  if (activeCourierId === undefined) throw new Error(`${operation} is unavailable after crew extinction`)
  return activeCourierId
}

export type FoundationDeckMovementResult =
  | { status: 'moved'; world: FoundationWorld; direction: JomonDeckMovementDirection; from: JomonDeckCoordinate; to: JomonDeckCoordinate }
  | { status: 'blocked'; direction: JomonDeckMovementDirection; from: JomonDeckCoordinate; collision: JomonDeckCollision }

/** Attempts one canonical local deck move. Blocked results intentionally have no world write. */
export const moveFoundationWorldCourier = (world: FoundationWorld, direction: JomonDeckMovementDirection): FoundationDeckMovementResult => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can move its courier')
  const courierId = requirePlayableActiveCourier(world, 'deck movement')
  const navigation = world.state.navigation
  if (courierId === undefined || navigation.courierId !== courierId || navigation.coordinate === undefined) throw new Error('an active courier with a deck coordinate is required')
  const assessment = assessJomonDeckStep(world, navigation.coordinate, direction)
  if (assessment.status === 'blocked') return assessment
  const nextSequence = world.state.causalHistory.checkpoint.sequence + world.state.causalHistory.tail.length + 1
  const payload = {
    actionId: `deck-move:${nextSequence}:${courierId}:${jomonDeckCoordinateId(assessment.from)}:${jomonDeckCoordinateId(assessment.to)}`,
    courierId,
    direction,
    from: assessment.from,
    to: assessment.to
  }
  const projection = moveDeckProjection(world, causalReplayProjectionForWorldState(world.state), payload)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'deck-moved', payload)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return { status: 'moved', world: worldFromProjection(world, projection, history), direction, from: assessment.from, to: assessment.to }
}

/** Applies the temporal contract and its canonical catch-up/era reducers, then journals one command. */
export const advanceFoundationWorldTime = (world: FoundationWorld, command: TemporalCommand | unknown): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can advance time')
  requirePlayableActiveCourier(world, 'time advancement')
  const projection = advanceTimeProjection(world, causalReplayProjectionForWorldState(world.state), command)
  const journalCommand = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'time-bearing-action', { action: command as TimeBearingTemporalAction })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, journalCommand, projection)
  return worldFromProjection(world, projection, history)
}

/** Records a completed durable Jomon change without advancing time, then journals it. */
export const recordDurableJomonGrowth = (world: FoundationWorld, evidence: DurableJomonGrowthEvidence): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can record durable Jomon growth')
  requirePlayableActiveCourier(world, 'durable growth')
  const projection = recordGrowthProjection(world, causalReplayProjectionForWorldState(world.state), evidence)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'durable-jomon-growth', { evidence })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return worldFromProjection(world, projection, history)
}

/** Offers a constrained task after a required pure conversation assessment. */
export const offerFoundationWorldDelegatedTask = (world: FoundationWorld, offer: DelegationOfferInput): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can offer delegated work')
  requirePlayableActiveCourier(world, 'delegated work')
  const projection = offerDelegationProjection(world, causalReplayProjectionForWorldState(world.state), offer)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'delegation-offered', { offer })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return worldFromProjection(world, projection, history)
}

/** Resolves the only v1 interruption causes after their explicit one-minute action. */
export const interruptFoundationWorldDelegatedTask = (world: FoundationWorld, interruption: DelegationInterruptionInput): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can interrupt delegated work')
  requirePlayableActiveCourier(world, 'delegated work')
  const projection = interruptDelegationProjection(world, causalReplayProjectionForWorldState(world.state), interruption)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'delegation-interrupted', { interruption })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return worldFromProjection(world, projection, history)
}

export const finalizeWorldAsChronicle = (world: FoundationWorld, reason: ChronicleReason): WorldChronicle => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  return {
    version: 12,
    id: `chronicle:${world.id}`,
    status: 'finalized',
    reason,
    world: cloneWorld(world)
  }
}

export const chronicleExport = (chronicle: WorldChronicle): string => JSON.stringify(chronicle, null, 2)
