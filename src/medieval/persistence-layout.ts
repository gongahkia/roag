import { auditMedievalContentSafety, classifyMedievalContent, contentSafetyAuditMatches, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification } from './content-safety'
import { causalDigestFor } from './causal-history'
import { canonicalSerializedByteLength, canonicalSerializedJson, MEDIEVAL_PERFORMANCE_STORAGE_BUDGETS } from './performance-budget'
import { isValidFoundationWorld, upgradeFoundationWorldV13, upgradeFoundationWorldV14, upgradeFoundationWorldV15 } from './world'
import type { FoundationWorld, WorldChronicle } from './types'

/**
 * v1 stores a full validated FoundationWorld as the only mutable authority.
 * The records in this module are digest-bound locators, recovery copies, and
 * offline transfer envelopes. They must never be used as a second world state.
 */
export const PERSISTENCE_LAYOUT_CONTRACT_VERSION = 1 as const
export const MEDIEVAL_PERSISTENCE_DATABASE_VERSION = 4 as const
export const PERSISTENCE_SNAPSHOT_VERSION = 1 as const
export const PERSISTENCE_BACKUP_VERSION = 1 as const

export const PERSISTENCE_LAYOUT_LIMITS = {
  locatorCount: 512,
  locatorIdLength: 192,
  snapshotsPerWorld: 3,
  snapshotSequenceMaximum: Number.MAX_SAFE_INTEGER - 1,
  backupBytes: 1_048_576,
  backupIdLength: 160
} as const

export const PERSISTENCE_LAYOUT_STORES = {
  catalog: 'catalog',
  worlds: 'worlds',
  chronicles: 'chronicles',
  creationSettings: 'creation-settings',
  terminalControls: 'terminal-controls',
  recordIndexes: 'world-record-indexes',
  snapshots: 'world-snapshots'
} as const

export const PERSISTENCE_RECORD_LOCATOR_KINDS = ['world', 'person', 'region', 'task', 'event', 'history'] as const
export type PersistenceRecordLocatorKind = typeof PERSISTENCE_RECORD_LOCATOR_KINDS[number]

export type PersistenceLocatorContainer =
  | 'foundation-world'
  | 'persistent-people'
  | 'known-frontier-regions'
  | 'delegation-tasks'
  | 'temporal-events'
  | 'causal-tail'
  | 'causal-checkpoint'
  | 'causal-segment'
  | 'social-memory'

const PERSISTENCE_LOCATOR_CONTAINERS: readonly PersistenceLocatorContainer[] = ['foundation-world', 'persistent-people', 'known-frontier-regions', 'delegation-tasks', 'temporal-events', 'causal-tail', 'causal-checkpoint', 'causal-segment', 'social-memory']

export interface PersistenceWorldSource {
  worldId: string
  revision: number
  digest: string
  canonicalBytes: number
}

export interface PersistenceRecordLocator {
  id: string
  kind: PersistenceRecordLocatorKind
  targetId: string
  container: PersistenceLocatorContainer
  contentSafety: MedievalContentSafetyClassification
}

export interface WorldRecordIndex {
  version: typeof PERSISTENCE_LAYOUT_CONTRACT_VERSION
  worldId: string
  source: PersistenceWorldSource
  locators: readonly PersistenceRecordLocator[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export interface FoundationWorldSnapshot {
  version: typeof PERSISTENCE_SNAPSHOT_VERSION
  id: string
  parentWorldId: string
  sequence: number
  slot: number
  source: PersistenceWorldSource
  world: FoundationWorld
}

export interface FoundationWorldSnapshotRing {
  version: typeof PERSISTENCE_SNAPSHOT_VERSION
  parentWorldId: string
  snapshots: readonly FoundationWorldSnapshot[]
}

export interface ActiveWorldBackupBundle {
  version: typeof PERSISTENCE_BACKUP_VERSION
  kind: 'active-world'
  source: PersistenceWorldSource
  world: FoundationWorld
}

export interface ReadOnlyChronicleBackupBundle {
  version: typeof PERSISTENCE_BACKUP_VERSION
  kind: 'read-only-chronicle'
  source: { chronicleId: string; worldId: string; digest: string; canonicalBytes: number }
  chronicle: WorldChronicle
}

export type PersistenceBackupBundle = ActiveWorldBackupBundle | ReadOnlyChronicleBackupBundle

export type PersistencePreflightReason =
  | 'known-layout-bytes-unavailable'
  | 'within-normal-planning-band'
  | 'advisory-write-warning'
  | 'elevated-storage-pressure'
  | 'record-exceeds-current-fixture-budget'

export interface PersistencePreflight {
  version: typeof PERSISTENCE_LAYOUT_CONTRACT_VERSION
  projectedBytes: number
  knownLayoutBytes?: number
  projectedTotalBytes?: number
  status: 'ready' | 'advisory-warning'
  reasons: readonly PersistencePreflightReason[]
}

export type PersistenceFailureCode =
  | 'storage-unavailable'
  | 'storage-upgrade-blocked'
  | 'storage-transaction-aborted'
  | 'storage-quota-exceeded'
  | 'storage-request-failed'
  | 'invalid-submitted-world'
  | 'corrupt-existing-world'
  | 'corrupt-existing-index'
  | 'corrupt-existing-snapshots'
  | 'backup-malformed'
  | 'backup-incompatible'
  | 'backup-collision'
  | 'snapshot-not-found'

export class MedievalPersistenceLayoutError extends Error {
  constructor(readonly code: PersistenceFailureCode, detail?: string) {
    super(`${code}${detail === undefined ? '' : `: ${detail}`}`)
    this.name = 'MedievalPersistenceLayoutError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const validString = (value: unknown, maximum: number = PERSISTENCE_LAYOUT_LIMITS.backupIdLength): value is string => typeof value === 'string' && value.length > 0 && value.length <= maximum && /^[a-z][a-z0-9:._-]*$/i.test(value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const same = (left: unknown, right: unknown): boolean => {
  try { return canonicalSerializedJson(left) === canonicalSerializedJson(right) } catch { return false }
}
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const wanted = [...expected].sort()
  return keys.length === wanted.length && keys.every((key, index) => key === wanted[index])
}

const locatorClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const locatorIdFor = (kind: PersistenceRecordLocatorKind, container: PersistenceLocatorContainer, targetId: string): string => `locator:${kind}:${container}:${targetId}`
const locator = (kind: PersistenceRecordLocatorKind, container: PersistenceLocatorContainer, targetId: string): PersistenceRecordLocator => ({
  id: locatorIdFor(kind, container, targetId), kind, targetId, container, contentSafety: locatorClassification()
})
const locatorRank: Readonly<Record<PersistenceRecordLocatorKind, number>> = { world: 0, person: 1, region: 2, task: 3, event: 4, history: 5 }
const canonicalLocators = (locators: readonly PersistenceRecordLocator[]): readonly PersistenceRecordLocator[] => [...locators]
  .sort((left, right) => locatorRank[left.kind] - locatorRank[right.kind] || compare(left.container, right.container) || compare(left.targetId, right.targetId) || compare(left.id, right.id))

/** A non-cryptographic canonical digest for integrity binding, not tamper-proofing. */
export const persistenceDigestFor = (scope: string, value: unknown): string => `layout:${causalDigestFor(scope, value)}`

export const persistenceWorldRevision = (world: FoundationWorld): number => world.state.causalHistory.checkpoint.sequence + world.state.causalHistory.tail.length

export const persistenceWorldSourceFor = (world: FoundationWorld): PersistenceWorldSource => {
  if (!isValidFoundationWorld(world)) throw new MedievalPersistenceLayoutError('invalid-submitted-world')
  return {
    worldId: world.id,
    revision: persistenceWorldRevision(world),
    digest: persistenceDigestFor('foundation-world', world),
    canonicalBytes: canonicalSerializedByteLength(world)
  }
}

const knownRegionsFor = (world: FoundationWorld): readonly string[] => world.state.geography.frontier.regions
  .filter(region => region.status === 'materialized' || (region.status === 'known-but-unvisited' && region.revealedFacts.length > 0))
  .map(region => region.commitment.id)

const recordIndexContent = (index: Pick<WorldRecordIndex, 'locators'>) => index.locators.map(item => ({ id: `persistence-locator:${item.id}`, domain: 'event' as const, classification: item.contentSafety }))
const recordIndexAudit = (index: Pick<WorldRecordIndex, 'locators'>): MedievalContentSafetyAudit => {
  const result = auditMedievalContentSafety(recordIndexContent(index))
  if (result.status === 'rejected') throw new MedievalPersistenceLayoutError('backup-malformed', 'unsafe derived locator')
  return result
}

/** Builds compact locators from authoritative records only; it never opens hidden population commitments. */
export const createWorldRecordIndex = (world: FoundationWorld): WorldRecordIndex => {
  const source = persistenceWorldSourceFor(world)
  const locators = canonicalLocators([
    locator('world', 'foundation-world', world.id),
    ...world.state.people.records.map(person => locator('person', 'persistent-people', person.id)),
    ...knownRegionsFor(world).map(regionId => locator('region', 'known-frontier-regions', regionId)),
    ...world.state.delegation.tasks.map(task => locator('task', 'delegation-tasks', task.id)),
    ...world.state.temporal.pendingEvents.map(event => locator('event', 'temporal-events', event.id)),
    ...world.state.temporal.causalRecords.map(event => locator('event', 'temporal-events', event.id)),
    ...world.state.causalHistory.tail.map(event => locator('history', 'causal-tail', event.id)),
    locator('history', 'causal-checkpoint', world.state.causalHistory.checkpoint.id),
    ...world.state.causalHistory.compactedSegments.map(segment => locator('history', 'causal-segment', segment.id)),
    ...world.state.socialMemory.records.map(memory => locator('history', 'social-memory', memory.id))
  ])
  if (locators.length > PERSISTENCE_LAYOUT_LIMITS.locatorCount) throw new MedievalPersistenceLayoutError('invalid-submitted-world', 'derived locator limit')
  const index = { version: PERSISTENCE_LAYOUT_CONTRACT_VERSION, worldId: world.id, source, locators, contentSafetyAudit: undefined as unknown as MedievalContentSafetyAudit }
  return { ...index, contentSafetyAudit: recordIndexAudit(index) }
}

const validSource = (value: unknown, worldId?: string): value is PersistenceWorldSource => record(value) && hasOnlyKeys(value, ['worldId', 'revision', 'digest', 'canonicalBytes'])
  && validString(value.worldId) && (worldId === undefined || value.worldId === worldId) && safeInteger(value.revision)
  && typeof value.digest === 'string' && value.digest.length > 0 && value.digest.length <= PERSISTENCE_LAYOUT_LIMITS.backupIdLength * 2
  && safeInteger(value.canonicalBytes)

const validLocator = (value: unknown): value is PersistenceRecordLocator => record(value) && hasOnlyKeys(value, ['id', 'kind', 'targetId', 'container', 'contentSafety'])
  && validString(value.id, PERSISTENCE_LAYOUT_LIMITS.locatorIdLength) && PERSISTENCE_RECORD_LOCATOR_KINDS.includes(value.kind as PersistenceRecordLocatorKind)
  && validString(value.targetId, PERSISTENCE_LAYOUT_LIMITS.locatorIdLength) && PERSISTENCE_LOCATOR_CONTAINERS.includes(value.container as PersistenceLocatorContainer)
  && locatorIdFor(value.kind as PersistenceRecordLocatorKind, value.container as PersistenceLocatorContainer, value.targetId) === value.id

/** Validation against a source world is exact, so a reordered, stale, or forged index fails closed. */
export const validateWorldRecordIndex = (value: unknown, world?: FoundationWorld): boolean => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'worldId', 'source', 'locators', 'contentSafetyAudit']) || value.version !== PERSISTENCE_LAYOUT_CONTRACT_VERSION || !validString(value.worldId) || !validSource(value.source, value.worldId) || !Array.isArray(value.locators) || value.locators.length > PERSISTENCE_LAYOUT_LIMITS.locatorCount || !value.locators.every(validLocator)) return false
  const locators = value.locators as readonly PersistenceRecordLocator[]
  if (!same(locators, canonicalLocators(locators)) || new Set(locators.map(item => item.id)).size !== locators.length) return false
  try {
    recordIndexAudit({ locators })
    if (!contentSafetyAuditMatches(recordIndexContent({ locators }), value.contentSafetyAudit)) return false
    return world === undefined ? true : same(value, createWorldRecordIndex(world))
  } catch { return false }
}

/**
 * Checks the exact derived index against a source envelope that has already
 * passed an explicit historical world upgrader. It exists only so an explicit
 * later save may replace that valid old envelope; read paths never write it.
 */
export const validateLegacyWorldRecordIndex = (value: unknown, world: unknown): boolean => {
  const rawState = record(world) && record(world.state) ? world.state : undefined
  const rawHistory = rawState !== undefined && record(rawState.causalHistory) ? rawState.causalHistory : undefined
  const rawCheckpoint = rawHistory !== undefined && record(rawHistory.checkpoint) ? rawHistory.checkpoint : undefined
  if (!record(world) || !validString(world.id) || !rawState || !rawHistory || !rawCheckpoint || !safeInteger(rawCheckpoint.sequence) || !Array.isArray(rawHistory.tail) || !record(rawState.people) || !Array.isArray(rawState.people.records) || !record(rawState.geography) || !record(rawState.geography.frontier) || !Array.isArray(rawState.geography.frontier.regions) || !record(rawState.delegation) || !Array.isArray(rawState.delegation.tasks) || !record(rawState.temporal) || !Array.isArray(rawState.temporal.pendingEvents) || !Array.isArray(rawState.temporal.causalRecords) || !record(rawState.socialMemory) || !Array.isArray(rawState.socialMemory.records)) return false
  try {
    const source = {
      worldId: world.id,
      revision: rawCheckpoint.sequence + rawHistory.tail.length,
      digest: persistenceDigestFor('foundation-world', world),
      canonicalBytes: canonicalSerializedByteLength(world)
    }
    const legacy = world as unknown as FoundationWorld
    const locators = canonicalLocators([
      locator('world', 'foundation-world', legacy.id),
      ...legacy.state.people.records.map(person => locator('person', 'persistent-people', person.id)),
      ...knownRegionsFor(legacy).map(regionId => locator('region', 'known-frontier-regions', regionId)),
      ...legacy.state.delegation.tasks.map(task => locator('task', 'delegation-tasks', task.id)),
      ...legacy.state.temporal.pendingEvents.map(event => locator('event', 'temporal-events', event.id)),
      ...legacy.state.temporal.causalRecords.map(event => locator('event', 'temporal-events', event.id)),
      ...legacy.state.causalHistory.tail.map(event => locator('history', 'causal-tail', event.id)),
      locator('history', 'causal-checkpoint', legacy.state.causalHistory.checkpoint.id),
      ...legacy.state.causalHistory.compactedSegments.map(segment => locator('history', 'causal-segment', segment.id)),
      ...legacy.state.socialMemory.records.map(memory => locator('history', 'social-memory', memory.id))
    ])
    const expected = { version: PERSISTENCE_LAYOUT_CONTRACT_VERSION, worldId: legacy.id, source, locators, contentSafetyAudit: undefined as unknown as MedievalContentSafetyAudit }
    return same(value, { ...expected, contentSafetyAudit: recordIndexAudit(expected) })
  } catch { return false }
}

export const emptySnapshotRing = (worldId: string): FoundationWorldSnapshotRing => ({ version: PERSISTENCE_SNAPSHOT_VERSION, parentWorldId: worldId, snapshots: [] })
const snapshotIdFor = (worldId: string, sequence: number, source: PersistenceWorldSource): string => `snapshot:${worldId}:${sequence}:${source.digest}`
const snapshotFor = (world: FoundationWorld, sequence: number): FoundationWorldSnapshot => {
  const source = persistenceWorldSourceFor(world)
  return { version: PERSISTENCE_SNAPSHOT_VERSION, id: snapshotIdFor(world.id, sequence, source), parentWorldId: world.id, sequence, slot: sequence % PERSISTENCE_LAYOUT_LIMITS.snapshotsPerWorld, source, world: structuredClone(world) }
}

export const validateFoundationWorldSnapshotRing = (value: unknown): value is FoundationWorldSnapshotRing => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'parentWorldId', 'snapshots']) || value.version !== PERSISTENCE_SNAPSHOT_VERSION || !validString(value.parentWorldId) || !Array.isArray(value.snapshots) || value.snapshots.length > PERSISTENCE_LAYOUT_LIMITS.snapshotsPerWorld) return false
  const snapshots = value.snapshots
  let previous = -1
  const slots = new Set<number>()
  for (const candidate of snapshots) {
    if (!record(candidate) || !hasOnlyKeys(candidate, ['version', 'id', 'parentWorldId', 'sequence', 'slot', 'source', 'world']) || candidate.version !== PERSISTENCE_SNAPSHOT_VERSION || candidate.parentWorldId !== value.parentWorldId || !safeInteger(candidate.sequence) || candidate.sequence <= previous || candidate.sequence > PERSISTENCE_LAYOUT_LIMITS.snapshotSequenceMaximum || !safeInteger(candidate.slot) || candidate.slot !== candidate.sequence % PERSISTENCE_LAYOUT_LIMITS.snapshotsPerWorld || slots.has(candidate.slot) || !isValidFoundationWorld(candidate.world) || candidate.world.id !== value.parentWorldId || !validSource(candidate.source, value.parentWorldId)) return false
    const source = persistenceWorldSourceFor(candidate.world)
    if (!same(source, candidate.source) || candidate.id !== snapshotIdFor(value.parentWorldId, candidate.sequence, source)) return false
    previous = candidate.sequence
    slots.add(candidate.slot)
  }
  return true
}

/** Snapshot the replaced valid envelope and rotate only the deterministic slot. */
export const snapshotRingAfterReplacement = (ring: FoundationWorldSnapshotRing | undefined, previous: FoundationWorld): FoundationWorldSnapshotRing => {
  const current = ring === undefined ? emptySnapshotRing(previous.id) : structuredClone(ring)
  if (!validateFoundationWorldSnapshotRing(current) || current.parentWorldId !== previous.id) throw new MedievalPersistenceLayoutError('corrupt-existing-snapshots')
  const sequence = (current.snapshots.at(-1)?.sequence ?? 0) + 1
  if (!Number.isSafeInteger(sequence) || sequence > PERSISTENCE_LAYOUT_LIMITS.snapshotSequenceMaximum) throw new MedievalPersistenceLayoutError('corrupt-existing-snapshots', 'snapshot sequence exhausted')
  const snapshot = snapshotFor(previous, sequence)
  return { version: PERSISTENCE_SNAPSHOT_VERSION, parentWorldId: previous.id, snapshots: [...current.snapshots.filter(item => item.slot !== snapshot.slot), snapshot].sort((left, right) => left.sequence - right.sequence) }
}

const validChronicle = (value: unknown): value is WorldChronicle => record(value) && hasOnlyKeys(value, ['version', 'id', 'status', 'reason', 'world']) && value.version === 12 && validString(value.id) && value.id === `chronicle:${(value.world as { id?: unknown })?.id ?? ''}` && value.status === 'finalized' && (value.reason === 'jomon-loss' || value.reason === 'crew-extinction') && isValidFoundationWorld(value.world)

export const createActiveWorldBackupBundle = (world: FoundationWorld): ActiveWorldBackupBundle => ({ version: PERSISTENCE_BACKUP_VERSION, kind: 'active-world', source: persistenceWorldSourceFor(world), world: structuredClone(world) })
export const createChronicleBackupBundle = (chronicle: WorldChronicle): ReadOnlyChronicleBackupBundle => {
  if (!validChronicle(chronicle)) throw new MedievalPersistenceLayoutError('backup-malformed', 'invalid chronicle')
  const canonicalBytes = canonicalSerializedByteLength(chronicle)
  return { version: PERSISTENCE_BACKUP_VERSION, kind: 'read-only-chronicle', source: { chronicleId: chronicle.id, worldId: chronicle.world.id, digest: persistenceDigestFor('read-only-chronicle', chronicle), canonicalBytes }, chronicle: structuredClone(chronicle) }
}

export const validatePersistenceBackupBundle = (value: unknown): value is PersistenceBackupBundle => {
  if (!record(value) || value.version !== PERSISTENCE_BACKUP_VERSION || (value.kind !== 'active-world' && value.kind !== 'read-only-chronicle')) return false
  try {
    if (value.kind === 'active-world') return hasOnlyKeys(value, ['version', 'kind', 'source', 'world']) && isValidFoundationWorld(value.world) && same(value, createActiveWorldBackupBundle(value.world))
    return hasOnlyKeys(value, ['version', 'kind', 'source', 'chronicle']) && validChronicle(value.chronicle) && same(value, createChronicleBackupBundle(value.chronicle))
  } catch { return false }
}

export const serializePersistenceBackupBundle = (bundle: PersistenceBackupBundle): string => {
  if (!validatePersistenceBackupBundle(bundle)) throw new MedievalPersistenceLayoutError('backup-malformed')
  // Contract constructors supply deterministic key/array order. JSON.stringify
  // retains that order on parse, which matters to legacy exact-reconstruction
  // validators; the digest still uses key-sorted canonical JSON.
  const serialized = JSON.stringify(bundle)
  if (new TextEncoder().encode(serialized).byteLength > PERSISTENCE_LAYOUT_LIMITS.backupBytes) throw new MedievalPersistenceLayoutError('backup-incompatible', 'backup size limit')
  return serialized
}

/** Verifies the old full-envelope source before the explicit world conversion. */
const legacyWorldSourceMatches = (source: unknown, world: unknown): boolean => {
  if (!record(world) || !record(world.state) || !record(world.state.causalHistory) || !record(world.state.causalHistory.checkpoint) || !safeInteger(world.state.causalHistory.checkpoint.sequence) || !Array.isArray(world.state.causalHistory.tail) || !validSource(source)) return false
  const expected = {
    worldId: world.id,
    revision: world.state.causalHistory.checkpoint.sequence + world.state.causalHistory.tail.length,
    digest: persistenceDigestFor('foundation-world', world),
    canonicalBytes: canonicalSerializedByteLength(world)
  }
  return same(source, expected)
}

/** Converts only canonical v13 or original-three-prop v14 backups; malformed legacy bytes remain rejected. */
const upgradeLegacyPersistenceBackupBundle = (value: unknown): PersistenceBackupBundle | undefined => {
  if (!record(value) || value.version !== PERSISTENCE_BACKUP_VERSION || (value.kind !== 'active-world' && value.kind !== 'read-only-chronicle')) return undefined
  try {
    if (value.kind === 'active-world') {
      if (!hasOnlyKeys(value, ['version', 'kind', 'source', 'world']) || !legacyWorldSourceMatches(value.source, value.world)) return undefined
      let world: FoundationWorld
      try { world = upgradeFoundationWorldV15(value.world) } catch { try { world = upgradeFoundationWorldV14(value.world) } catch { world = upgradeFoundationWorldV13(value.world) } }
      return createActiveWorldBackupBundle(world)
    }
    if (!hasOnlyKeys(value, ['version', 'kind', 'source', 'chronicle']) || !record(value.chronicle) || !hasOnlyKeys(value.chronicle, ['version', 'id', 'status', 'reason', 'world']) || value.chronicle.version !== 12 || value.chronicle.status !== 'finalized' || (value.chronicle.reason !== 'jomon-loss' && value.chronicle.reason !== 'crew-extinction')) return undefined
    let chronicleWorld: FoundationWorld
    try { chronicleWorld = upgradeFoundationWorldV15(value.chronicle.world) } catch { try { chronicleWorld = upgradeFoundationWorldV14(value.chronicle.world) } catch { chronicleWorld = upgradeFoundationWorldV13(value.chronicle.world) } }
    const legacyChronicleSource = { chronicleId: value.chronicle.id, worldId: chronicleWorld.id, digest: persistenceDigestFor('read-only-chronicle', value.chronicle), canonicalBytes: canonicalSerializedByteLength(value.chronicle) }
    if (!same(value.source, legacyChronicleSource) || value.chronicle.id !== `chronicle:${chronicleWorld.id}`) return undefined
    return createChronicleBackupBundle({ version: 12, id: value.chronicle.id, status: 'finalized', reason: value.chronicle.reason, world: chronicleWorld })
  } catch { return undefined }
}

export const parsePersistenceBackupBundle = (serialized: string): PersistenceBackupBundle => {
  if (typeof serialized !== 'string' || new TextEncoder().encode(serialized).byteLength > PERSISTENCE_LAYOUT_LIMITS.backupBytes) throw new MedievalPersistenceLayoutError('backup-incompatible', 'backup size limit')
  let value: unknown
  try { value = JSON.parse(serialized) } catch { throw new MedievalPersistenceLayoutError('backup-malformed', 'invalid JSON') }
  if (!validatePersistenceBackupBundle(value)) {
    const upgraded = upgradeLegacyPersistenceBackupBundle(value)
    if (upgraded === undefined || serialized !== JSON.stringify(value)) throw new MedievalPersistenceLayoutError('backup-incompatible', 'invalid bundle')
    return upgraded
  }
  const canonicalBundle = value.kind === 'active-world' ? createActiveWorldBackupBundle(value.world) : createChronicleBackupBundle(value.chronicle)
  if (serialized !== JSON.stringify(canonicalBundle)) throw new MedievalPersistenceLayoutError('backup-incompatible', 'noncanonical bundle')
  return structuredClone(value)
}

const budgetMaximum = (id: string): number => MEDIEVAL_PERFORMANCE_STORAGE_BUDGETS.find(item => item.id === id)!.maximum
export const createPersistencePreflight = (value: unknown, knownLayoutBytes?: number): PersistencePreflight => {
  const projectedBytes = canonicalSerializedByteLength(value)
  if (knownLayoutBytes !== undefined && (!safeInteger(knownLayoutBytes))) throw new MedievalPersistenceLayoutError('backup-malformed', 'invalid known layout byte count')
  const total = knownLayoutBytes === undefined ? undefined : knownLayoutBytes + projectedBytes
  const reasons: PersistencePreflightReason[] = []
  if (projectedBytes > budgetMaximum('active-world-json')) reasons.push('record-exceeds-current-fixture-budget')
  if (total === undefined) reasons.push('known-layout-bytes-unavailable')
  else if (total >= budgetMaximum('local-storage-planning-warning')) reasons.push('elevated-storage-pressure')
  else if (total >= budgetMaximum('local-storage-planning-normal')) reasons.push('advisory-write-warning')
  else reasons.push('within-normal-planning-band')
  return { version: PERSISTENCE_LAYOUT_CONTRACT_VERSION, projectedBytes, ...(knownLayoutBytes === undefined ? {} : { knownLayoutBytes, projectedTotalBytes: total }), status: reasons.includes('advisory-write-warning') || reasons.includes('elevated-storage-pressure') ? 'advisory-warning' : 'ready', reasons: [...reasons].sort(compare) }
}
