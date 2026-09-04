import type { GenerationDiagnostics, WorldGenerationConfig, WorldGenerationConfigSelection } from './generation-config'
import type { MedievalContentSafetyAudit, MedievalContentSafetyClassification } from './content-safety'
import type { InitialWorld, InitialWorldGenerationDiagnostics } from './initial-world'
import type { FrontierCausalAnchor, FrontierConnection, FrontierCoordinate, FrontierRegionKind } from './frontier'
import type { LegacyMedievalWorldStateV11, LegacyMedievalWorldStateV12, LegacyMedievalWorldStateV13, MedievalWorldState } from './world-state'

export const FOUNDATION_GENERATOR_VERSION = 'foundation-2' as const
export const FOUNDATION_MANIFEST_VERSION = 6 as const
export const WORLD_CREATION_PROVENANCE_VERSION = 1 as const
export const WORLD_MANIFEST_VALIDATION_HISTORY_VERSION = 1 as const
export const WORLD_MANIFEST_FRONTIER_PROVENANCE_VERSION = 1 as const
export const MEDIEVAL_DATABASE_NAME = 'jomon-medieval-worlds-v1' as const

/** All records here are immutable evidence from zero-time world creation. */
export interface WorldManifestContractVersions {
  foundationGenerator: typeof FOUNDATION_GENERATOR_VERSION
  initialWorldGenerator: string
  initialWorldDiagnostics: number
  frontierContract: number
  contentSafetyPolicy: number
}

/** A bounded ID index allows reconstruction to reject reordered or substituted initial data. */
export interface InitialWorldManifestIdentity {
  id: string
  candidateAttempt: number
  configurationFingerprint: string
  digest: string
  watershedId: string
  waterwayIds: readonly string[]
  climateId: string
  seasonIds: readonly string[]
  resourceIds: readonly string[]
  ecologyIds: readonly string[]
  settlementIds: readonly string[]
  institutionIds: readonly string[]
  personIds: readonly string[]
  routeIds: readonly string[]
  tradeLinkIds: readonly string[]
  routeHazardIds: readonly string[]
  historyEventIds: readonly string[]
}

/**
 * Commitment data only: it reproduces the bounded initial frontier roots but
 * deliberately excludes discovered facts, named people, and materialization.
 */
export interface FrontierRootManifestIdentity {
  id: string
  coordinate: FrontierCoordinate
  kind: FrontierRegionKind
  generationOrder: number
  generatorStream: string
  anchor: FrontierCausalAnchor
  connection: FrontierConnection
}

export interface FrontierManifestProvenance {
  version: typeof WORLD_MANIFEST_FRONTIER_PROVENANCE_VERSION
  contractVersion: number
  initialWorldId: string
  roots: readonly FrontierRootManifestIdentity[]
  digest: string
}

export interface WorldManifestValidationHistory {
  version: typeof WORLD_MANIFEST_VALIDATION_HISTORY_VERSION
  generation: GenerationDiagnostics
  initialWorld: InitialWorldGenerationDiagnostics
  frontier: { status: 'accepted'; issues: readonly [] }
}

export interface WorldCreationProvenance {
  version: typeof WORLD_CREATION_PROVENANCE_VERSION
  seed: string
  selectedConfiguration: WorldGenerationConfigSelection
  resolvedConfiguration: WorldGenerationConfig
  configurationFingerprint: string
  contractVersions: WorldManifestContractVersions
  validationHistory: WorldManifestValidationHistory
  initialWorld: InitialWorldManifestIdentity
  frontier: FrontierManifestProvenance
  label: string
  labelContentSafety: MedievalContentSafetyClassification
  contentSafetyAudit: MedievalContentSafetyAudit
  digest: string
}

export interface WorldManifest {
  version: typeof FOUNDATION_MANIFEST_VERSION
  creation: WorldCreationProvenance
}

export type CrewRole = 'bargemaster' | 'pilot' | 'factor' | 'carpenter' | 'guard' | 'cook' | 'healer' | 'scribe' | 'carter' | 'fisher' | 'bard'

export interface CrewRelationship {
  personId: string
  standing: -2 | -1 | 0 | 1 | 2
  basis: 'kinship' | 'work' | 'debt' | 'friendship' | 'rivalry'
}

/**
 * These are spatial contracts, not yet a map. Phase 2 makes them walkable
 * from the same world data rather than inventing a second vessel model.
 */
export type JomonDeckPartition = 'tavern' | 'chart-table' | 'cargo-hold' | 'repair-space' | 'stores' | 'berths' | 'galley' | 'gangplank'
export type JomonVesselPropKind = 'table' | 'ledger' | 'rack' | 'hearth' | 'berth' | 'gangplank'

export interface FoundationQuay {
  id: string
  name: string
}

export interface FoundationVesselProp {
  id: string
  kind: JomonVesselPropKind
  partition: JomonDeckPartition
}

export interface FoundationJomon {
  id: 'vessel:jomon'
  name: 'Jomon'
  contentSafety: MedievalContentSafetyClassification
  deckPartitions: readonly JomonDeckPartition[]
  quays: readonly FoundationQuay[]
  props: readonly FoundationVesselProp[]
}

/** Foundation representation only; Phase 1.3 expands this to full persistent-person state. */
export interface FoundationCrewMember {
  id: string
  name: string
  contentSafety: MedievalContentSafetyClassification
  role: CrewRole
  conversation: number
  equipment: readonly string[]
  history: string
  historyContentSafety: MedievalContentSafetyClassification
  relationships: readonly CrewRelationship[]
  eligible: boolean
}

export interface CausalRecord {
  sequence: number
  atWorldTime: number
  kind: 'world-created' | 'initial-courier-selected' | 'temporal-action' | 'scheduled-event-resolved'
  detail: string
  contentSafety: MedievalContentSafetyClassification
}

export interface FoundationWorld {
  /** v14 keeps the foundation envelope while mutable state evolves independently. */
  version: 14
  id: string
  status: 'active'
  manifest: WorldManifest
  jomon: FoundationJomon
  crew: readonly FoundationCrewMember[]
  initialWorld: InitialWorld
  state: MedievalWorldState
}

/** Read-only import shape accepted only by the deterministic v13 conversion. */
export interface LegacyFoundationWorldV13 extends Omit<FoundationWorld, 'version' | 'state'> {
  version: 13
  state: LegacyMedievalWorldStateV11
}

/** Read-only v14/v12 envelope accepted only by the active-courier upgrader. */
export interface LegacyFoundationWorldV14 extends Omit<FoundationWorld, 'state'> {
  version: 14
  state: LegacyMedievalWorldStateV12
}

/** Read-only v14/v13 envelope accepted only by the courier-continuity upgrader. */
export interface LegacyFoundationWorldV14V13 extends Omit<FoundationWorld, 'state'> {
  version: 14
  state: LegacyMedievalWorldStateV13
}

export type ChronicleReason = 'jomon-loss' | 'crew-extinction'

export interface WorldChronicle {
  version: 12
  id: string
  status: 'finalized'
  reason: ChronicleReason
  world: FoundationWorld
}

export interface ActiveWorldIndexEntry {
  id: string
  label: string
  initialCourierId?: string
}

export interface ChronicleIndexEntry {
  id: string
  label: string
  reason: ChronicleReason
}

export interface WorldIndex {
  version: 1
  activeWorlds: readonly ActiveWorldIndexEntry[]
  chronicles: readonly ChronicleIndexEntry[]
}

export type MedievalRoute = 'worlds' | 'create-world' | 'creation-profiles' | 'world-generation' | 'world-result' | 'choose-courier' | 'world' | 'chronicles' | 'chronicle'
