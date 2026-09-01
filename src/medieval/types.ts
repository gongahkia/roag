export const FOUNDATION_GENERATOR_VERSION = 'foundation-1' as const
export const MEDIEVAL_DATABASE_NAME = 'jomon-medieval-worlds-v1' as const

export interface FoundationWorldConfiguration {
  version: 1
  profile: 'foundation'
}

export interface WorldManifest {
  version: 1
  seed: string
  configuration: FoundationWorldConfiguration
  generatorVersion: typeof FOUNDATION_GENERATOR_VERSION
  label: string
  initialCourierId?: string
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
  deckPartitions: readonly JomonDeckPartition[]
  quays: readonly FoundationQuay[]
  props: readonly FoundationVesselProp[]
}

/** Foundation representation only; Phase 1.3 expands this to full persistent-person state. */
export interface FoundationCrewMember {
  id: string
  name: string
  role: CrewRole
  conversation: number
  equipment: readonly string[]
  history: string
  relationships: readonly CrewRelationship[]
  eligible: boolean
}

export interface CausalRecord {
  sequence: number
  atWorldTime: number
  kind: 'world-created' | 'initial-courier-selected'
  detail: string
}

export interface FoundationWorld {
  version: 1
  id: string
  status: 'active'
  manifest: WorldManifest
  jomon: FoundationJomon
  crew: readonly FoundationCrewMember[]
  worldTime: 0
  causalHistory: readonly CausalRecord[]
}

export type ChronicleReason = 'jomon-loss' | 'crew-extinction'

export interface WorldChronicle {
  version: 1
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

export type MedievalRoute = 'worlds' | 'create-world' | 'choose-courier' | 'world' | 'chronicles' | 'chronicle'
