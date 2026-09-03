import { FIDELITY_PLANNING_CONTRACT_VERSION } from './fidelity'
import { FRONTIER_CONTRACT_VERSION } from './frontier'
import { generationConfigurationFingerprint } from './generation-config'
import { PERSISTENCE_LAYOUT_CONTRACT_VERSION, persistenceDigestFor, persistenceWorldSourceFor } from './persistence-layout'
import { MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES, MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_IDS, MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES, MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION, canonicalSerializedJson, resolveMedievalFoundationBenchmarkFixture, type MedievalFoundationBenchmarkFixture } from './performance-budget'
import { SIMULATION_CATCH_UP_CONTRACT_VERSION } from './simulation-catchup'
import type { FoundationWorld } from './types'

/**
 * Versioned, pure evidence gate for future performance work. It deliberately
 * adds no cache, worker, packed storage, spatial index, or generation path.
 */
export const OPTIMIZATION_BOUNDARIES_CONTRACT_VERSION = 1 as const
export const OPTIMIZATION_DERIVED_BINDING_VERSION = 1 as const
export const OPTIONAL_WORKER_ADAPTER_CONTRACT_VERSION = 1 as const

export const OPTIMIZATION_STRATEGY_IDS = [
  'spatial-index',
  'incremental-initial-frontier-generation',
  'deterministic-scheduled-summary-batching',
  'compact-packed-derived-representation',
  'pure-projection-memoization',
  'optional-worker-execution-adapter'
] as const
export type OptimizationStrategyId = typeof OPTIMIZATION_STRATEGY_IDS[number]

export interface OptimizationAuthorityIdentity {
  version: typeof OPTIMIZATION_BOUNDARIES_CONTRACT_VERSION
  worldId: string
  revision: number
  digest: string
  canonicalBytes: number
  contractVersions: {
    worldEnvelope: 13
    performanceStorageBaseline: typeof MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION
    persistenceLayout: typeof PERSISTENCE_LAYOUT_CONTRACT_VERSION
    frontier: typeof FRONTIER_CONTRACT_VERSION
    fidelityPlanning: typeof FIDELITY_PLANNING_CONTRACT_VERSION
    simulationCatchUp: typeof SIMULATION_CATCH_UP_CONTRACT_VERSION
  }
}

/** The full validated envelope is always the source identity for derived work. */
export const optimizationAuthorityIdentityFor = (world: FoundationWorld): OptimizationAuthorityIdentity => {
  const source = persistenceWorldSourceFor(world)
  return {
    version: OPTIMIZATION_BOUNDARIES_CONTRACT_VERSION,
    worldId: source.worldId,
    revision: source.revision,
    digest: source.digest,
    canonicalBytes: source.canonicalBytes,
    contractVersions: {
      worldEnvelope: world.version,
      performanceStorageBaseline: MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION,
      persistenceLayout: PERSISTENCE_LAYOUT_CONTRACT_VERSION,
      frontier: FRONTIER_CONTRACT_VERSION,
      fidelityPlanning: FIDELITY_PLANNING_CONTRACT_VERSION,
      simulationCatchUp: SIMULATION_CATCH_UP_CONTRACT_VERSION
    }
  }
}

export const CANONICAL_EXECUTION_BOUNDARIES = {
  mutableAuthority: 'FoundationWorld-only',
  canonicalOutputs: 'main-thread-validated-and-replayable',
  rng: 'named-seeded-streams-only',
  contentSafety: 'validated-before-use',
  causalHistory: 'replay-preserved',
  execution: 'bounded-with-typed-failures',
  actionTime: 'time-bearing-actions-only',
  wallClock: 'never-a-simulation-input-or-canonical-persisted-world-fact',
  frontierKnowledge: 'known-facts-only-for-derived-forms'
} as const

export const SCHEDULED_SUMMARY_BATCH_BOUNDARY = {
  owner: 'simulation-catchup-canonical-cursor-window-owner',
  source: 'existing-canonical-cursor-window-order',
  cadence: 'existing-fidelity-cadence-semantics',
  outcomes: 'existing-outcome-evidence-and-bounded-records',
  partitioning: 'must-be-partition-invariant',
  currentScope: 'scheduling-and-provenance-not-full-domain-simulation',
  forbiddenDomainSimulation: ['economy', 'institutions', 'people', 'travel', 'combat'] as const
} as const

export type OptimizationPrerequisite =
  | 'reproducible-workload-profile'
  | 'measured-pressure'
  | 'canonical-output-equivalence'
  | 'source-identity-and-invalidation'
  | 'known-frontier-facts-only'
  | 'bounded-failure-path'
  | 'materialized-spatial-workload'
  | 'resumable-generation-boundary'
  | 'canonical-cursor-window-order'
  | 'partition-invariant-summaries'
  | 'derived-read-only-representation'
  | 'pure-projection-complete-key'
  | 'versioned-serializable-worker-input'
  | 'validated-main-thread-worker-equivalence'

export interface OptimizationStrategyBoundary {
  id: OptimizationStrategyId
  owner: string
  status: 'not-yet-eligible'
  prerequisites: readonly OptimizationPrerequisite[]
  currentBlockers: readonly string[]
}

/**
 * This matrix is deliberately explicit about ownership. “Owner” names the
 * future boundary responsible for a derived result, never a second authority.
 */
export const OPTIMIZATION_STRATEGY_BOUNDARIES: readonly OptimizationStrategyBoundary[] = [
  {
    id: 'spatial-index',
    owner: 'future-known-world-query-projection',
    status: 'not-yet-eligible',
    prerequisites: ['reproducible-workload-profile', 'measured-pressure', 'materialized-spatial-workload', 'canonical-output-equivalence', 'source-identity-and-invalidation', 'known-frontier-facts-only', 'bounded-failure-path'],
    currentBlockers: ['terminal-presentation-materializes-zero-map-cells', 'no-measured-spatial-query-workload']
  },
  {
    id: 'incremental-initial-frontier-generation',
    owner: 'initial-world-and-frontier-generation-authority',
    status: 'not-yet-eligible',
    prerequisites: ['reproducible-workload-profile', 'measured-pressure', 'resumable-generation-boundary', 'canonical-output-equivalence', 'source-identity-and-invalidation', 'known-frontier-facts-only', 'bounded-failure-path'],
    currentBlockers: ['initial-world-and-frontier-commitments-are-bounded', 'no-broad-generation-workload-is-profiled']
  },
  {
    id: 'deterministic-scheduled-summary-batching',
    owner: 'simulation-catchup-canonical-cursor-window-owner',
    status: 'not-yet-eligible',
    prerequisites: ['reproducible-workload-profile', 'measured-pressure', 'canonical-cursor-window-order', 'partition-invariant-summaries', 'canonical-output-equivalence', 'source-identity-and-invalidation', 'bounded-failure-path'],
    currentBlockers: ['catch-up-is-scheduling-and-provenance-not-domain-simulation', 'no-summary-batch-pressure-is-profiled']
  },
  {
    id: 'compact-packed-derived-representation',
    owner: 'future-derived-representation-adapter',
    status: 'not-yet-eligible',
    prerequisites: ['reproducible-workload-profile', 'measured-pressure', 'derived-read-only-representation', 'canonical-output-equivalence', 'source-identity-and-invalidation', 'known-frontier-facts-only', 'bounded-failure-path'],
    currentBlockers: ['full-envelope-json-remains-the-only-authority', 'no-measured-derived-byte-or-allocation-pressure', 'no-binary-or-packed-authority-data']
  },
  {
    id: 'pure-projection-memoization',
    owner: 'caller-owned-ephemeral-projection-cache',
    status: 'not-yet-eligible',
    prerequisites: ['reproducible-workload-profile', 'measured-pressure', 'pure-projection-complete-key', 'canonical-output-equivalence', 'source-identity-and-invalidation', 'known-frontier-facts-only', 'bounded-failure-path'],
    currentBlockers: ['no-measured-repeated-pure-projection-workload', 'no-cache-lifetime-is-authorized']
  },
  {
    id: 'optional-worker-execution-adapter',
    owner: 'future-authority-provided-optional-worker-adapter',
    status: 'not-yet-eligible',
    prerequisites: ['reproducible-workload-profile', 'measured-pressure', 'versioned-serializable-worker-input', 'validated-main-thread-worker-equivalence', 'canonical-output-equivalence', 'source-identity-and-invalidation', 'bounded-failure-path'],
    currentBlockers: ['no-worker-adapter-or-browser-capability-dependency', 'no-measured-main-thread-pressure', 'workers-cannot-be-authority']
  }
] as const

export interface OptimizationMeasurementQuality {
  source: 'reproducible-current-foundation-fixture'
  warmupSamples: typeof MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES
  measuredSamples: typeof MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES
  deterministicByteAccounting: true
  canonicalOutputsVerified: true
  seedAndRngProvenanceVerified: true
  contentSafetyValidated: true
  causalReplayVerified: true
  boundedExecutionVerified: true
  typedFailuresVerified: true
  actionTimeSemanticsVerified: true
  wallClockExcludedFromCanonicalState: true
}

export interface CurrentFoundationWorkloadProfile {
  fixtureId: MedievalFoundationBenchmarkFixture['id']
  fixtureSeed: string
  resolvedConfigurationFingerprint: string
  benchmarkOperations: readonly typeof MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_IDS[number][]
  boundedActionDurationMinutes: 240
  terminalMaterializedMapCells: 0
  frontierCommitments: 'bounded'
  catchUpScope: 'scheduling-and-provenance'
  mutableAuthority: 'FoundationWorld-only'
}

export interface CurrentFoundationTechniqueMeasurements {
  spatialIndex: { materializedMapCells: 0; canonicalSpatialQueries: 0; pressureObserved: false }
  incrementalGeneration: { broadGenerationRequests: 0; resumableBoundaryMeasured: false; pressureObserved: false }
  scheduledSummaryBatching: { fullDomainSimulation: false; partitionInvarianceVerified: true; pressureObserved: false }
  compactDerivedRepresentation: { measuredDerivedByteSavings: 0; derivedReadOnlyRepresentationVerified: false; pressureObserved: false }
  memoization: { repeatedPureProjectionCalls: 0; completeKeyVerified: false; boundedLifetimeVerified: false; pressureObserved: false }
  workerAdapter: { browserCapabilityRequired: false; versionedSerializableInputVerified: false; mainThreadEquivalenceVerified: false; pressureObserved: false }
}

/**
 * Evidence contains no timing values or timestamps. Benchmark timings are
 * review observations; this record only binds the verified workload and its
 * deterministic safety/equivalence requirements to one full envelope.
 */
export interface CurrentFoundationOptimizationEvidence {
  version: typeof OPTIMIZATION_BOUNDARIES_CONTRACT_VERSION
  authority: OptimizationAuthorityIdentity
  workload: CurrentFoundationWorkloadProfile
  quality: OptimizationMeasurementQuality
  measurements: CurrentFoundationTechniqueMeasurements
}

export type OptimizationEvidenceDiagnostic =
  | 'malformed-evidence'
  | 'incomplete-evidence'
  | 'noncanonical-evidence'
  | 'stale-authority-identity'
  | 'mismatched-contract-version'
  | 'mismatched-fixture-workload'

export interface OptimizationEvidenceValidation {
  accepted: boolean
  diagnostics: readonly OptimizationEvidenceDiagnostic[]
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const exactKeys = (value: Record<string, unknown>, keys: readonly string[]): boolean => {
  const actual = Object.keys(value).sort()
  const expected = [...keys].sort()
  return actual.length === expected.length && actual.every((key, index) => key === expected[index])
}
const same = (left: unknown, right: unknown): boolean => {
  try { return canonicalSerializedJson(left) === canonicalSerializedJson(right) } catch { return false }
}
const canonicalString = (value: string): boolean => {
  try { return canonicalSerializedJson(JSON.parse(value)) === value } catch { return false }
}
const safeIdentifier = (value: unknown): value is string => typeof value === 'string' && /^[a-z][a-z0-9:._-]*$/i.test(value) && value.length <= 192

export const currentFoundationOptimizationEvidenceFor = (
  world: FoundationWorld,
  fixture: MedievalFoundationBenchmarkFixture
): CurrentFoundationOptimizationEvidence => {
  const resolved = resolveMedievalFoundationBenchmarkFixture(fixture)
  if (world.manifest.creation.seed !== fixture.seed || world.manifest.creation.configurationFingerprint !== generationConfigurationFingerprint(resolved.resolvedConfiguration)) {
    throw new Error(`world does not match optimization fixture: ${fixture.id}`)
  }
  return {
    version: OPTIMIZATION_BOUNDARIES_CONTRACT_VERSION,
    authority: optimizationAuthorityIdentityFor(world),
    workload: {
      fixtureId: fixture.id,
      fixtureSeed: fixture.seed,
      resolvedConfigurationFingerprint: world.manifest.creation.configurationFingerprint,
      benchmarkOperations: [...MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_IDS],
      boundedActionDurationMinutes: 240,
      terminalMaterializedMapCells: 0,
      frontierCommitments: 'bounded',
      catchUpScope: 'scheduling-and-provenance',
      mutableAuthority: 'FoundationWorld-only'
    },
    quality: {
      source: 'reproducible-current-foundation-fixture',
      warmupSamples: MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES,
      measuredSamples: MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES,
      deterministicByteAccounting: true,
      canonicalOutputsVerified: true,
      seedAndRngProvenanceVerified: true,
      contentSafetyValidated: true,
      causalReplayVerified: true,
      boundedExecutionVerified: true,
      typedFailuresVerified: true,
      actionTimeSemanticsVerified: true,
      wallClockExcludedFromCanonicalState: true
    },
    measurements: {
      spatialIndex: { materializedMapCells: 0, canonicalSpatialQueries: 0, pressureObserved: false },
      incrementalGeneration: { broadGenerationRequests: 0, resumableBoundaryMeasured: false, pressureObserved: false },
      scheduledSummaryBatching: { fullDomainSimulation: false, partitionInvarianceVerified: true, pressureObserved: false },
      compactDerivedRepresentation: { measuredDerivedByteSavings: 0, derivedReadOnlyRepresentationVerified: false, pressureObserved: false },
      memoization: { repeatedPureProjectionCalls: 0, completeKeyVerified: false, boundedLifetimeVerified: false, pressureObserved: false },
      workerAdapter: { browserCapabilityRequired: false, versionedSerializableInputVerified: false, mainThreadEquivalenceVerified: false, pressureObserved: false }
    }
  }
}

/** Rejects unknown, incomplete, stale, reordered, or version-mismatched evidence. */
export const validateCurrentFoundationOptimizationEvidence = (
  value: unknown,
  world: FoundationWorld,
  fixture: MedievalFoundationBenchmarkFixture
): OptimizationEvidenceValidation => {
  if (!record(value) || !exactKeys(value, ['version', 'authority', 'workload', 'quality', 'measurements'])) return { accepted: false, diagnostics: ['malformed-evidence'] }
  if (value.version !== OPTIMIZATION_BOUNDARIES_CONTRACT_VERSION) return { accepted: false, diagnostics: ['mismatched-contract-version'] }
  let expected: CurrentFoundationOptimizationEvidence
  try { expected = currentFoundationOptimizationEvidenceFor(world, fixture) } catch { return { accepted: false, diagnostics: ['mismatched-fixture-workload'] } }
  const evidence = value as unknown as CurrentFoundationOptimizationEvidence
  if (!same(evidence.authority, expected.authority)) return { accepted: false, diagnostics: ['stale-authority-identity'] }
  if (!same(evidence.workload, expected.workload)) return { accepted: false, diagnostics: ['mismatched-fixture-workload'] }
  if (!same(evidence.quality, expected.quality) || !same(evidence.measurements, expected.measurements)) return { accepted: false, diagnostics: ['incomplete-evidence'] }
  if (!same(evidence, expected)) return { accepted: false, diagnostics: ['noncanonical-evidence'] }
  return { accepted: true, diagnostics: [] }
}

export interface OptimizationDecisionRecord {
  version: typeof OPTIMIZATION_BOUNDARIES_CONTRACT_VERSION
  strategy: OptimizationStrategyId
  owner: string
  status: 'not-yet-eligible'
  prerequisites: readonly OptimizationPrerequisite[]
  blockers: readonly string[]
}

/** v1 cannot authorize an implementation: current facts make every strategy ineligible. */
export const optimizationDecisionRecordsFor = (
  evidence: unknown,
  world: FoundationWorld,
  fixture: MedievalFoundationBenchmarkFixture
): readonly OptimizationDecisionRecord[] => {
  const validation = validateCurrentFoundationOptimizationEvidence(evidence, world, fixture)
  return OPTIMIZATION_STRATEGY_BOUNDARIES.map(boundary => ({
    version: OPTIMIZATION_BOUNDARIES_CONTRACT_VERSION,
    strategy: boundary.id,
    owner: boundary.owner,
    status: 'not-yet-eligible',
    prerequisites: [...boundary.prerequisites],
    blockers: validation.accepted ? [...boundary.currentBlockers] : ['measurement-evidence-rejected', ...validation.diagnostics]
  }))
}

export type DerivedRepresentationKind = 'spatial-index' | 'compact-packed-projection' | 'memoized-projection' | 'worker-result'

export interface DerivedRepresentationBinding {
  version: typeof OPTIMIZATION_DERIVED_BINDING_VERSION
  kind: DerivedRepresentationKind
  source: OptimizationAuthorityIdentity
  derivedContractVersion: number
  authority: 'read-only-derived'
  frontierVisibility: 'known-facts-only'
}

/** Durable derived data must be rebuilt, never patched, after any source mismatch. */
export const createDerivedRepresentationBinding = (
  world: FoundationWorld,
  kind: DerivedRepresentationKind,
  derivedContractVersion: number
): DerivedRepresentationBinding => {
  if (!Number.isSafeInteger(derivedContractVersion) || derivedContractVersion <= 0) throw new Error('derived contract version must be a positive safe integer')
  return { version: OPTIMIZATION_DERIVED_BINDING_VERSION, kind, source: optimizationAuthorityIdentityFor(world), derivedContractVersion, authority: 'read-only-derived', frontierVisibility: 'known-facts-only' }
}

export const derivedRepresentationIsUsable = (value: unknown, world: FoundationWorld, derivedContractVersion: number): boolean => {
  if (!record(value) || !exactKeys(value, ['version', 'kind', 'source', 'derivedContractVersion', 'authority', 'frontierVisibility'])) return false
  if (value.version !== OPTIMIZATION_DERIVED_BINDING_VERSION || !['spatial-index', 'compact-packed-projection', 'memoized-projection', 'worker-result'].includes(value.kind as DerivedRepresentationKind) || value.derivedContractVersion !== derivedContractVersion || value.authority !== 'read-only-derived' || value.frontierVisibility !== 'known-facts-only') return false
  try { return same(value.source, optimizationAuthorityIdentityFor(world)) } catch { return false }
}

export interface MemoizationBoundary {
  version: typeof OPTIMIZATION_DERIVED_BINDING_VERSION
  binding: DerivedRepresentationBinding
  projectionId: string
  completeKeyDigest: string
  ownership: 'caller-owned-ephemeral'
  lifetime: 'single-projection-pass' | 'single-action'
  purity: 'pure-projection'
  retainedWorldFacts: 'none'
  retainedPlayerKnowledge: 'none'
}

/** The key binds all supplied inputs plus the full envelope; no projection input is retained. */
export const createMemoizationBoundary = (
  world: FoundationWorld,
  projectionId: string,
  completeInputs: unknown,
  lifetime: MemoizationBoundary['lifetime'],
  projectionContractVersion: number
): MemoizationBoundary => {
  if (!safeIdentifier(projectionId)) throw new Error('memoization projection id is invalid')
  const binding = createDerivedRepresentationBinding(world, 'memoized-projection', projectionContractVersion)
  return {
    version: OPTIMIZATION_DERIVED_BINDING_VERSION,
    binding,
    projectionId,
    completeKeyDigest: persistenceDigestFor('optimization-memoization-key', { source: binding.source, projectionId, completeInputs }),
    ownership: 'caller-owned-ephemeral',
    lifetime,
    purity: 'pure-projection',
    retainedWorldFacts: 'none',
    retainedPlayerKnowledge: 'none'
  }
}

export const memoizationBoundaryIsUsable = (value: unknown, world: FoundationWorld, projectionContractVersion: number): boolean => {
  if (!record(value) || !exactKeys(value, ['version', 'binding', 'projectionId', 'completeKeyDigest', 'ownership', 'lifetime', 'purity', 'retainedWorldFacts', 'retainedPlayerKnowledge'])) return false
  return value.version === OPTIMIZATION_DERIVED_BINDING_VERSION
    && derivedRepresentationIsUsable(value.binding, world, projectionContractVersion)
    && safeIdentifier(value.projectionId)
    && typeof value.completeKeyDigest === 'string' && value.completeKeyDigest.length > 0
    && value.ownership === 'caller-owned-ephemeral'
    && (value.lifetime === 'single-projection-pass' || value.lifetime === 'single-action')
    && value.purity === 'pure-projection'
    && value.retainedWorldFacts === 'none'
    && value.retainedPlayerKnowledge === 'none'
}

export interface OptionalWorkerExecutionRequest {
  version: typeof OPTIONAL_WORKER_ADAPTER_CONTRACT_VERSION
  requestId: string
  authority: OptimizationAuthorityIdentity
  operation: 'generation-projection' | 'scheduled-summary-projection'
  input: { version: 1; canonicalJson: string; provider: 'FoundationWorld-authority' }
  rng: { kind: 'none' } | { kind: 'named-deterministic-streams'; streamNames: readonly string[] }
  permissions: { indexedDb: 'forbidden'; persistenceAuthority: 'forbidden'; simulationAuthority: 'forbidden'; mutableWorldAccess: 'forbidden' }
}

export interface OptionalWorkerExecutionResult {
  version: typeof OPTIONAL_WORKER_ADAPTER_CONTRACT_VERSION
  requestId: string
  authority: OptimizationAuthorityIdentity
  output: { version: 1; canonicalJson: string; digest: string }
}

export const OPTIONAL_WORKER_FAILURE_BOUNDARY = {
  cancellation: 'does-not-mutate-or-advance-world',
  unsupportedCapability: 'uses-normal-validated-execution-path',
  timeout: 'does-not-mutate-or-advance-world',
  failure: 'does-not-mutate-or-advance-world',
  authority: 'normal-validated-main-thread-path-remains-authoritative'
} as const

const canonicalWorkerStreams = (streams: readonly string[]): readonly string[] => [...streams].sort((left, right) => left.localeCompare(right))
const workerResultDigestFor = (request: Pick<OptionalWorkerExecutionRequest, 'requestId' | 'authority' | 'operation'>, canonicalOutput: string): string => persistenceDigestFor('optimization-worker-result', { requestId: request.requestId, authority: request.authority, operation: request.operation, canonicalOutput })

/** A future adapter may serialize this value; this module never constructs or starts a Worker. */
export const createOptionalWorkerExecutionRequest = (
  world: FoundationWorld,
  requestId: string,
  operation: OptionalWorkerExecutionRequest['operation'],
  authorityProvidedInput: unknown,
  rngStreamNames: readonly string[] = []
): OptionalWorkerExecutionRequest => {
  if (!safeIdentifier(requestId) || rngStreamNames.some(name => !safeIdentifier(name)) || new Set(rngStreamNames).size !== rngStreamNames.length) throw new Error('worker request identity or RNG stream is invalid')
  const streamNames = canonicalWorkerStreams(rngStreamNames)
  return {
    version: OPTIONAL_WORKER_ADAPTER_CONTRACT_VERSION,
    requestId,
    authority: optimizationAuthorityIdentityFor(world),
    operation,
    input: { version: 1, canonicalJson: canonicalSerializedJson(authorityProvidedInput), provider: 'FoundationWorld-authority' },
    rng: streamNames.length === 0 ? { kind: 'none' } : { kind: 'named-deterministic-streams', streamNames },
    permissions: { indexedDb: 'forbidden', persistenceAuthority: 'forbidden', simulationAuthority: 'forbidden', mutableWorldAccess: 'forbidden' }
  }
}

export const createOptionalWorkerExecutionResult = (request: OptionalWorkerExecutionRequest, output: unknown): OptionalWorkerExecutionResult => {
  const canonicalOutput = canonicalSerializedJson(output)
  return {
    version: OPTIONAL_WORKER_ADAPTER_CONTRACT_VERSION,
    requestId: request.requestId,
    authority: structuredClone(request.authority),
    output: { version: 1, canonicalJson: canonicalOutput, digest: workerResultDigestFor(request, canonicalOutput) }
  }
}

export type OptionalWorkerResultValidation =
  | { status: 'accepted'; canonicalOutput: string }
  | { status: 'rejected'; reason: 'malformed-request-or-result' | 'stale-authority' | 'main-thread-output-mismatch' }

/** A stale, malformed, or unequal worker result has no use and no mutation path. */
export const validateOptionalWorkerExecutionResult = (
  request: unknown,
  result: unknown,
  world: FoundationWorld,
  canonicalMainThreadOutput: unknown
): OptionalWorkerResultValidation => {
  const currentAuthority = optimizationAuthorityIdentityFor(world)
  if (!record(request) || !record(result)
    || !exactKeys(request, ['version', 'requestId', 'authority', 'operation', 'input', 'rng', 'permissions'])
    || !exactKeys(result, ['version', 'requestId', 'authority', 'output'])
    || request.version !== OPTIONAL_WORKER_ADAPTER_CONTRACT_VERSION || result.version !== OPTIONAL_WORKER_ADAPTER_CONTRACT_VERSION
    || !safeIdentifier(request.requestId) || request.requestId !== result.requestId
    || (request.operation !== 'generation-projection' && request.operation !== 'scheduled-summary-projection')
    || !record(request.input) || !exactKeys(request.input, ['version', 'canonicalJson', 'provider']) || request.input.version !== 1 || request.input.provider !== 'FoundationWorld-authority' || typeof request.input.canonicalJson !== 'string' || !canonicalString(request.input.canonicalJson)
    || !record(request.permissions) || !same(request.permissions, { indexedDb: 'forbidden', persistenceAuthority: 'forbidden', simulationAuthority: 'forbidden', mutableWorldAccess: 'forbidden' })
    || !record(result.output) || !exactKeys(result.output, ['version', 'canonicalJson', 'digest']) || result.output.version !== 1 || typeof result.output.canonicalJson !== 'string' || !canonicalString(result.output.canonicalJson) || typeof result.output.digest !== 'string') {
    return { status: 'rejected', reason: 'malformed-request-or-result' }
  }
  const typedRequest = request as unknown as OptionalWorkerExecutionRequest
  const typedResult = result as unknown as OptionalWorkerExecutionResult
  const rng = typedRequest.rng
  const validRng = record(rng)
    && ((rng.kind === 'none' && exactKeys(rng, ['kind']))
      || (rng.kind === 'named-deterministic-streams' && exactKeys(rng, ['kind', 'streamNames']) && Array.isArray(rng.streamNames) && rng.streamNames.every(safeIdentifier) && new Set(rng.streamNames).size === rng.streamNames.length && same(rng.streamNames, canonicalWorkerStreams(rng.streamNames))))
  if (!validRng) return { status: 'rejected', reason: 'malformed-request-or-result' }
  if (!same(typedRequest.authority, currentAuthority) || !same(typedResult.authority, currentAuthority)) return { status: 'rejected', reason: 'stale-authority' }
  if (typedResult.output.digest !== workerResultDigestFor(typedRequest, typedResult.output.canonicalJson) || typedResult.output.canonicalJson !== canonicalSerializedJson(canonicalMainThreadOutput)) return { status: 'rejected', reason: 'main-thread-output-mismatch' }
  return { status: 'accepted', canonicalOutput: typedResult.output.canonicalJson }
}
