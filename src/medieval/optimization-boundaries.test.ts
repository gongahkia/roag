import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES } from './performance-budget'
import { CANONICAL_EXECUTION_BOUNDARIES, OPTIONAL_WORKER_FAILURE_BOUNDARY, OPTIMIZATION_STRATEGY_BOUNDARIES, SCHEDULED_SUMMARY_BATCH_BOUNDARY, createDerivedRepresentationBinding, createMemoizationBoundary, createOptionalWorkerExecutionRequest, createOptionalWorkerExecutionResult, currentFoundationOptimizationEvidenceFor, derivedRepresentationIsUsable, memoizationBoundaryIsUsable, optimizationDecisionRecordsFor, validateCurrentFoundationOptimizationEvidence, validateOptionalWorkerExecutionResult } from './optimization-boundaries'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld } from './world'

const fixtureWorld = (fixture = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES[0]) => chooseInitialCourier(createFoundationWorld({ seed: fixture.seed, configuration: fixture.configuration }), 'crew:0')
const wait = (id: string) => ({
  id,
  kind: 'wait' as const,
  durationMinutes: 1,
  contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
})

describe('profile-guided optimization boundaries', () => {
  it('creates reproducible current-fixture records while keeping every strategy not yet eligible', () => {
    for (const fixture of MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES) {
      const world = fixtureWorld(fixture)
      const first = currentFoundationOptimizationEvidenceFor(world, fixture)
      const second = currentFoundationOptimizationEvidenceFor(world, fixture)
      const decisions = optimizationDecisionRecordsFor(first, world, fixture)

      expect(second).toEqual(first)
      expect(validateCurrentFoundationOptimizationEvidence(first, world, fixture)).toEqual({ accepted: true, diagnostics: [] })
      expect(decisions).toEqual(optimizationDecisionRecordsFor(second, world, fixture))
      expect(decisions.map(decision => decision.status)).toEqual(OPTIMIZATION_STRATEGY_BOUNDARIES.map(() => 'not-yet-eligible'))
      expect(first.workload).toMatchObject({ terminalMaterializedMapCells: 0, frontierCommitments: 'bounded', catchUpScope: 'scheduling-and-provenance', mutableAuthority: 'FoundationWorld-only' })
    }
  }, 30_000)

  it('names exact strategy owners and the non-speculative prerequisites for future review', () => {
    expect(OPTIMIZATION_STRATEGY_BOUNDARIES).toEqual(expect.arrayContaining([
      expect.objectContaining({ id: 'spatial-index', owner: 'future-known-world-query-projection', status: 'not-yet-eligible', prerequisites: expect.arrayContaining(['materialized-spatial-workload', 'measured-pressure']) }),
      expect.objectContaining({ id: 'incremental-initial-frontier-generation', owner: 'initial-world-and-frontier-generation-authority', status: 'not-yet-eligible', prerequisites: expect.arrayContaining(['resumable-generation-boundary', 'canonical-output-equivalence']) }),
      expect.objectContaining({ id: 'deterministic-scheduled-summary-batching', owner: 'simulation-catchup-canonical-cursor-window-owner', status: 'not-yet-eligible', prerequisites: expect.arrayContaining(['canonical-cursor-window-order', 'partition-invariant-summaries']) }),
      expect.objectContaining({ id: 'compact-packed-derived-representation', owner: 'future-derived-representation-adapter', status: 'not-yet-eligible', prerequisites: expect.arrayContaining(['derived-read-only-representation', 'source-identity-and-invalidation']) }),
      expect.objectContaining({ id: 'pure-projection-memoization', owner: 'caller-owned-ephemeral-projection-cache', status: 'not-yet-eligible', prerequisites: expect.arrayContaining(['pure-projection-complete-key']) }),
      expect.objectContaining({ id: 'optional-worker-execution-adapter', owner: 'future-authority-provided-optional-worker-adapter', status: 'not-yet-eligible', prerequisites: expect.arrayContaining(['versioned-serializable-worker-input', 'validated-main-thread-worker-equivalence']) })
    ]))
    expect(SCHEDULED_SUMMARY_BATCH_BOUNDARY).toMatchObject({ currentScope: 'scheduling-and-provenance-not-full-domain-simulation', partitioning: 'must-be-partition-invariant' })
  })

  it('fails closed for malformed, incomplete, noncanonical, stale, revision/digest, and contract-mismatched evidence', () => {
    const fixture = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES[1]
    const world = fixtureWorld(fixture)
    const evidence = currentFoundationOptimizationEvidenceFor(world, fixture)
    const malformed = { ...evidence, unexpected: true }
    const incomplete = structuredClone(evidence) as unknown as { quality: Record<string, unknown> }
    delete incomplete.quality.causalReplayVerified
    const noncanonical = structuredClone(evidence)
    noncanonical.workload.benchmarkOperations = [...noncanonical.workload.benchmarkOperations].reverse()
    const changedDigest = structuredClone(evidence)
    changedDigest.authority.digest = 'layout:changed'
    const changedRevision = structuredClone(evidence)
    changedRevision.authority.revision++
    const changedContract = structuredClone(evidence)
    changedContract.authority.contractVersions.frontier = 999 as never
    const advanced = advanceFoundationWorldTime(world, wait('optimization-evidence-stale'))

    expect(validateCurrentFoundationOptimizationEvidence(malformed, world, fixture).accepted).toBe(false)
    expect(validateCurrentFoundationOptimizationEvidence(incomplete, world, fixture).diagnostics).toContain('incomplete-evidence')
    expect(validateCurrentFoundationOptimizationEvidence(noncanonical, world, fixture).accepted).toBe(false)
    expect(validateCurrentFoundationOptimizationEvidence(changedDigest, world, fixture).diagnostics).toContain('stale-authority-identity')
    expect(validateCurrentFoundationOptimizationEvidence(changedRevision, world, fixture).diagnostics).toContain('stale-authority-identity')
    expect(validateCurrentFoundationOptimizationEvidence(changedContract, world, fixture).diagnostics).toContain('stale-authority-identity')
    expect(validateCurrentFoundationOptimizationEvidence(evidence, advanced, fixture).diagnostics).toContain('stale-authority-identity')
    expect(optimizationDecisionRecordsFor(changedDigest, world, fixture).every(decision => decision.blockers[0] === 'measurement-evidence-rejected')).toBe(true)
  }, 20_000)

  it('requires derived indexes, packed forms, and memoization to invalidate on the exact full-envelope identity', () => {
    const fixture = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES[0]
    const world = fixtureWorld(fixture)
    const binding = createDerivedRepresentationBinding(world, 'compact-packed-projection', 7)
    const memoization = createMemoizationBoundary(world, 'terminal:sidebar', { section: 'tasks', knownFactsOnly: true }, 'single-projection-pass', 4)
    const advanced = advanceFoundationWorldTime(world, wait('optimization-derived-stale'))
    const forged = structuredClone(binding)
    forged.authority = 'mutable-world' as never
    const staleKnowledge = structuredClone(memoization)
    staleKnowledge.retainedPlayerKnowledge = 'some' as never

    expect(derivedRepresentationIsUsable(binding, world, 7)).toBe(true)
    expect(derivedRepresentationIsUsable(binding, advanced, 7)).toBe(false)
    expect(derivedRepresentationIsUsable(binding, world, 8)).toBe(false)
    expect(derivedRepresentationIsUsable(forged, world, 7)).toBe(false)
    expect(memoizationBoundaryIsUsable(memoization, world, 4)).toBe(true)
    expect(memoizationBoundaryIsUsable(memoization, advanced, 4)).toBe(false)
    expect(memoizationBoundaryIsUsable(staleKnowledge, world, 4)).toBe(false)
  }, 20_000)

  it('rejects stale or non-equivalent optional-worker results before they can be used', () => {
    const world = fixtureWorld()
    const request = createOptionalWorkerExecutionRequest(world, 'worker:foundation-projection', 'generation-projection', { worldId: world.id, work: 'projection-only' }, ['rng:frontier', 'rng:initial-world'])
    const result = createOptionalWorkerExecutionResult(request, { materialized: false, result: 'canonical' })
    const advanced = advanceFoundationWorldTime(world, wait('optimization-worker-stale'))
    const mismatch = createOptionalWorkerExecutionResult(request, { materialized: true, result: 'different' })
    const malformedRng = structuredClone(request)
    malformedRng.rng = { kind: 'named-deterministic-streams', streamNames: ['rng:frontier', 'rng:frontier'] }

    expect(request.rng).toEqual({ kind: 'named-deterministic-streams', streamNames: ['rng:frontier', 'rng:initial-world'] })
    expect(request.permissions).toEqual({ indexedDb: 'forbidden', persistenceAuthority: 'forbidden', simulationAuthority: 'forbidden', mutableWorldAccess: 'forbidden' })
    expect(validateOptionalWorkerExecutionResult(request, result, world, { materialized: false, result: 'canonical' })).toMatchObject({ status: 'accepted' })
    expect(validateOptionalWorkerExecutionResult(request, result, advanced, { materialized: false, result: 'canonical' })).toEqual({ status: 'rejected', reason: 'stale-authority' })
    expect(validateOptionalWorkerExecutionResult(request, mismatch, world, { materialized: false, result: 'canonical' })).toEqual({ status: 'rejected', reason: 'main-thread-output-mismatch' })
    expect(validateOptionalWorkerExecutionResult(malformedRng, result, world, { materialized: false, result: 'canonical' })).toEqual({ status: 'rejected', reason: 'malformed-request-or-result' })
  }, 20_000)

  it('states that no derived path is an alternative authority and wall-clock data cannot become world state', () => {
    expect(CANONICAL_EXECUTION_BOUNDARIES).toMatchObject({
      mutableAuthority: 'FoundationWorld-only',
      wallClock: 'never-a-simulation-input-or-canonical-persisted-world-fact',
      actionTime: 'time-bearing-actions-only',
      causalHistory: 'replay-preserved',
      frontierKnowledge: 'known-facts-only-for-derived-forms'
    })
    expect(OPTIONAL_WORKER_FAILURE_BOUNDARY).toMatchObject({
      cancellation: 'does-not-mutate-or-advance-world',
      unsupportedCapability: 'uses-normal-validated-execution-path',
      timeout: 'does-not-mutate-or-advance-world',
      authority: 'normal-validated-main-thread-path-remains-authoritative'
    })
  })
})
