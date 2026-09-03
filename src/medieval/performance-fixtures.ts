import { JOMON_ASCII_GLYPH_CATALOG } from './ascii-glyphs'
import { classifyMedievalContent } from './content-safety'
import { createDetailedRendererAdapterModel, createDetailedRendererSourceBundle } from './detailed-renderer-adapter'
import { createManagementSidebarModel } from './management-sidebar'
import { MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES, canonicalSerializedByteLength, canonicalSerializedJson, resolveMedievalFoundationBenchmarkFixture, serializedByteBudgetFor, type MedievalFoundationBenchmarkFixture, type MedievalFoundationBenchmarkFixtureId, type SerializedByteBudgetResult } from './performance-budget'
import { simulationCatchUpProjection } from './simulation-catchup'
import { addChronicleToIndex, addWorldToIndex, emptyWorldIndex } from './storage'
import { defaultTerminalControlPreferences } from './terminal-controls'
import { createTerminalPresentationModel } from './terminal-presentation'
import { finalizeWorldAsChronicle, advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld } from './world'
import type { FoundationWorld, WorldIndex } from './types'

/**
 * v1 owns deterministic fixture inputs and regression projections only. It
 * neither measures browser/IndexedDB time nor retains a performance cache.
 */
export const MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION = 1 as const

/** Labels make the benchmark report's assertions and observations non-interchangeable. */
export const PERFORMANCE_FIXTURE_MEASUREMENT_BOUNDARIES = {
  deterministicAssertions: [
    'fixture-identity-and-production-configuration-resolution',
    'canonical-full-envelope-byte-ceilings-and-growth-deltas',
    'retained-state-count-growth',
    'scheduled-summary-partition-equivalence',
    'pure-presentation-zero-time-projections',
    'bounded-in-memory-catalogue-ordering'
  ],
  reviewOnlyObservations: [
    'Node wall-clock operation and projection timing',
    'Node process memory is not collected because it is non-portable and GC-sensitive'
  ],
  nonClaims: [
    'canonical serialized bytes are neither browser RAM nor browser quota usage',
    'no browser responsiveness percentile is measured',
    'no IndexedDB timing is measured'
  ]
} as const

const baselineFixture = (id: Extract<MedievalFoundationBenchmarkFixtureId, 'sheltered-reach-focused' | 'watershed-balanced' | 'far-coast-deep'>): MedievalFoundationBenchmarkFixture => {
  const fixture = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.find(candidate => candidate.id === id)
  if (!fixture) throw new Error(`missing established benchmark fixture: ${id}`)
  return fixture
}

// Kept separate from the v1 diagonal baseline so its historic identities,
// seeds, and benchmark observations remain directly comparable.
export const MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX: readonly MedievalFoundationBenchmarkFixture[] = [
  baselineFixture('sheltered-reach-focused'),
  {
    id: 'sheltered-reach-balanced',
    label: 'Sheltered Reach / balanced',
    seed: 'performance-sheltered-reach-balanced',
    configuration: { preset: 'sheltered-reach', advanced: { simulationFidelity: 'balanced' } },
    expectedPreset: 'sheltered-reach',
    expectedSimulationFidelity: 'balanced',
    expectedInstantiatedPeople: 6
  },
  {
    id: 'sheltered-reach-deep',
    label: 'Sheltered Reach / deep',
    seed: 'performance-sheltered-reach-deep',
    configuration: { preset: 'sheltered-reach', advanced: { simulationFidelity: 'deep' } },
    expectedPreset: 'sheltered-reach',
    expectedSimulationFidelity: 'deep',
    expectedInstantiatedPeople: 6
  },
  {
    id: 'watershed-focused',
    label: 'Watershed / focused',
    seed: 'performance-watershed-focused',
    configuration: { preset: 'watershed', advanced: { simulationFidelity: 'focused' } },
    expectedPreset: 'watershed',
    expectedSimulationFidelity: 'focused',
    expectedInstantiatedPeople: 6
  },
  baselineFixture('watershed-balanced'),
  {
    id: 'watershed-deep',
    label: 'Watershed / deep',
    seed: 'performance-watershed-deep',
    configuration: { preset: 'watershed', advanced: { simulationFidelity: 'deep' } },
    expectedPreset: 'watershed',
    expectedSimulationFidelity: 'deep',
    expectedInstantiatedPeople: 6
  },
  {
    id: 'far-coast-focused',
    label: 'Far Coast / focused',
    seed: 'performance-far-coast-focused',
    configuration: { preset: 'far-coast', advanced: { simulationFidelity: 'focused' } },
    expectedPreset: 'far-coast',
    expectedSimulationFidelity: 'focused',
    expectedInstantiatedPeople: 6
  },
  {
    id: 'far-coast-balanced',
    label: 'Far Coast / balanced',
    seed: 'performance-far-coast-balanced',
    configuration: { preset: 'far-coast', advanced: { simulationFidelity: 'balanced' } },
    expectedPreset: 'far-coast',
    expectedSimulationFidelity: 'balanced',
    expectedInstantiatedPeople: 6
  },
  baselineFixture('far-coast-deep')
] as const

export const PERFORMANCE_FIXTURE_DUE_EVENT_SEQUENCE_MINUTES = [60, 180] as const
export const PERFORMANCE_FIXTURE_DUE_EVENT_TOTAL_MINUTES = 240 as const
export const PERFORMANCE_FIXTURE_CATALOGUE_ACTIVE_IDS = ['sheltered-reach-focused', 'watershed-balanced', 'far-coast-deep'] as const
export const PERFORMANCE_FIXTURE_CATALOGUE_CHRONICLE_IDS = ['sheltered-reach-balanced', 'sheltered-reach-deep', 'watershed-focused', 'watershed-deep'] as const

export interface RetainedFoundationStateCounts {
  persistentPeople: number
  frontierRegions: number
  temporalCausalRecords: number
  temporalPendingEvents: number
  catchUpCursors: number
  catchUpRecords: number
  delegatedWork: number
  delegationTasks: number
  causalHistoryTail: number
  causalHistorySegments: number
  socialMemoryRecords: number
}

export interface PerformanceFixtureStateGrowth {
  before: RetainedFoundationStateCounts
  after: RetainedFoundationStateCounts
  delta: RetainedFoundationStateCounts
}

export interface PerformanceFixtureEnvelopeSizes {
  before: SerializedByteBudgetResult
  after: SerializedByteBudgetResult
  deltaBytes: number
}

export interface PerformanceFixturePresentationMetrics {
  terminalMapCells: number
  detailedAdapterMapCells: number
  terminalCanonicalBytes: number
  sidebarCanonicalBytes: number
  detailedAdapterCanonicalBytes: number
  sidebarKnownFactCount: number
  worldTimeBefore: number
  worldTimeAfter: number
}

export interface PerformanceFixtureDueEventMetrics {
  singleActionMinutes: typeof PERFORMANCE_FIXTURE_DUE_EVENT_TOTAL_MINUTES
  partitionMinutes: readonly typeof PERFORMANCE_FIXTURE_DUE_EVENT_SEQUENCE_MINUTES[number][]
  partitionInvariant: boolean
  singleProjectionCanonicalBytes: number
  partitionedProjectionCanonicalBytes: number
}

export interface PerformanceFixtureScenario {
  version: typeof MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION
  fixtureId: MedievalFoundationBenchmarkFixtureId
  seed: string
  dueEvents: PerformanceFixtureDueEventMetrics
  retainedStateGrowth: PerformanceFixtureStateGrowth
  envelopeSizes: PerformanceFixtureEnvelopeSizes
  presentation: PerformanceFixturePresentationMetrics
}

export interface PerformanceFixtureCatalogue {
  version: typeof MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION
  activeWorldFixtureIds: readonly string[]
  chronicleFixtureIds: readonly string[]
  index: WorldIndex
  canonicalBytes: SerializedByteBudgetResult
}

const fixtureFor = (id: MedievalFoundationBenchmarkFixtureId): MedievalFoundationBenchmarkFixture => {
  const fixture = MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.find(candidate => candidate.id === id)
  if (!fixture) throw new Error(`missing performance fixture: ${id}`)
  return fixture
}
const waitAction = (fixtureId: string, sequence: number, durationMinutes: number) => ({
  id: `performance-fixture:${fixtureId}:wait:${sequence}`,
  kind: 'wait' as const,
  durationMinutes,
  contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
})
const projectionFor = (world: FoundationWorld) => simulationCatchUpProjection({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime
}, world.state.simulation)
const stateCountsFor = (world: FoundationWorld): RetainedFoundationStateCounts => ({
  persistentPeople: world.state.people.records.length,
  frontierRegions: world.state.geography.frontier.regions.length,
  temporalCausalRecords: world.state.temporal.causalRecords.length,
  temporalPendingEvents: world.state.temporal.pendingEvents.length,
  catchUpCursors: world.state.simulation.cursors.length,
  catchUpRecords: world.state.simulation.records.length,
  delegatedWork: world.state.simulation.delegatedWork.length,
  delegationTasks: world.state.delegation.tasks.length,
  causalHistoryTail: world.state.causalHistory.tail.length,
  causalHistorySegments: world.state.causalHistory.compactedSegments.length,
  socialMemoryRecords: world.state.socialMemory.records.length
})
const stateDelta = (before: RetainedFoundationStateCounts, after: RetainedFoundationStateCounts): RetainedFoundationStateCounts => ({
  persistentPeople: after.persistentPeople - before.persistentPeople,
  frontierRegions: after.frontierRegions - before.frontierRegions,
  temporalCausalRecords: after.temporalCausalRecords - before.temporalCausalRecords,
  temporalPendingEvents: after.temporalPendingEvents - before.temporalPendingEvents,
  catchUpCursors: after.catchUpCursors - before.catchUpCursors,
  catchUpRecords: after.catchUpRecords - before.catchUpRecords,
  delegatedWork: after.delegatedWork - before.delegatedWork,
  delegationTasks: after.delegationTasks - before.delegationTasks,
  causalHistoryTail: after.causalHistoryTail - before.causalHistoryTail,
  causalHistorySegments: after.causalHistorySegments - before.causalHistorySegments,
  socialMemoryRecords: after.socialMemoryRecords - before.socialMemoryRecords
})

/** Every row resolves through the production generation-config authority. */
export const resolveMedievalPerformanceFixtureMatrix = (): readonly ReturnType<typeof resolveMedievalFoundationBenchmarkFixture>[] => {
  const expected = [
    ['sheltered-reach', 'focused'], ['sheltered-reach', 'balanced'], ['sheltered-reach', 'deep'],
    ['watershed', 'focused'], ['watershed', 'balanced'], ['watershed', 'deep'],
    ['far-coast', 'focused'], ['far-coast', 'balanced'], ['far-coast', 'deep']
  ] as const
  if (MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.length !== expected.length) throw new Error('performance fixture matrix is incomplete')
  return MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.map((fixture, index) => {
    const resolved = resolveMedievalFoundationBenchmarkFixture(fixture)
    const [preset, fidelity] = expected[index]!
    if (resolved.resolvedConfiguration.preset !== preset || resolved.resolvedConfiguration.simulationFidelity !== fidelity) throw new Error(`performance fixture does not resolve its declared matrix row: ${fixture.id}`)
    return resolved
  })
}

/** Builds a valid selected full envelope; no fixture invents a world shape. */
export const selectedPerformanceFixtureWorld = (fixture: MedievalFoundationBenchmarkFixture): FoundationWorld => chooseInitialCourier(createFoundationWorld({ seed: fixture.seed, configuration: fixture.configuration }), 'crew:0')

/** The two-action sequence crosses every current 1/5/30/120/240 cadence family. */
export const advancePerformanceFixtureDueEvents = (world: FoundationWorld, fixtureId: string): FoundationWorld => PERFORMANCE_FIXTURE_DUE_EVENT_SEQUENCE_MINUTES.reduce((current, durationMinutes, index) => advanceFoundationWorldTime(current, waitAction(fixtureId, index, durationMinutes)), world)

/** A comparable one-action path is kept solely to prove canonical window partition invariance. */
export const advancePerformanceFixtureDueEventsAsSingleAction = (world: FoundationWorld, fixtureId: string): FoundationWorld => advanceFoundationWorldTime(world, waitAction(fixtureId, 0, PERFORMANCE_FIXTURE_DUE_EVENT_TOTAL_MINUTES))

/** Pure presentation work is exposed separately from any browser responsiveness claim. */
export const projectPerformanceFixturePresentation = (world: FoundationWorld) => {
  const worldTimeBefore = world.state.temporal.worldTime
  const terminal = createTerminalPresentationModel(world)
  const sidebar = createManagementSidebarModel(world)
  const source = createDetailedRendererSourceBundle({ version: 1, terminal, sidebar, glyphCatalog: JOMON_ASCII_GLYPH_CATALOG, controls: defaultTerminalControlPreferences() })
  const detailed = createDetailedRendererAdapterModel(source)
  return {
    terminal,
    sidebar,
    detailed,
    metrics: {
      terminalMapCells: terminal.map.cells.length,
      detailedAdapterMapCells: detailed.map.cells.length,
      terminalCanonicalBytes: canonicalSerializedByteLength(terminal),
      sidebarCanonicalBytes: canonicalSerializedByteLength(sidebar),
      detailedAdapterCanonicalBytes: canonicalSerializedByteLength(detailed),
      sidebarKnownFactCount: sidebar.summary.knownFactCount,
      worldTimeBefore,
      worldTimeAfter: world.state.temporal.worldTime
    } satisfies PerformanceFixturePresentationMetrics
  }
}

/** Deterministic size/count proxies, not browser memory, quota, or timing measurements. */
export const runPerformanceFixtureScenario = (fixture: MedievalFoundationBenchmarkFixture): PerformanceFixtureScenario => {
  const before = selectedPerformanceFixtureWorld(fixture)
  const presentation = projectPerformanceFixturePresentation(before).metrics
  const partitioned = advancePerformanceFixtureDueEvents(before, fixture.id)
  const single = advancePerformanceFixtureDueEventsAsSingleAction(selectedPerformanceFixtureWorld(fixture), fixture.id)
  const beforeCounts = stateCountsFor(before)
  const afterCounts = stateCountsFor(partitioned)
  const beforeBytes = serializedByteBudgetFor('active-world', before)
  const afterBytes = serializedByteBudgetFor('active-world', partitioned)
  const singleProjection = projectionFor(single)
  const partitionedProjection = projectionFor(partitioned)
  return {
    version: MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION,
    fixtureId: fixture.id,
    seed: fixture.seed,
    dueEvents: {
      singleActionMinutes: PERFORMANCE_FIXTURE_DUE_EVENT_TOTAL_MINUTES,
      partitionMinutes: [...PERFORMANCE_FIXTURE_DUE_EVENT_SEQUENCE_MINUTES],
      partitionInvariant: canonicalSerializedJson(singleProjection) === canonicalSerializedJson(partitionedProjection),
      singleProjectionCanonicalBytes: canonicalSerializedByteLength(singleProjection),
      partitionedProjectionCanonicalBytes: canonicalSerializedByteLength(partitionedProjection)
    },
    retainedStateGrowth: { before: beforeCounts, after: afterCounts, delta: stateDelta(beforeCounts, afterCounts) },
    envelopeSizes: { before: beforeBytes, after: afterBytes, deltaBytes: afterBytes.bytes - beforeBytes.bytes },
    presentation
  }
}

/** Uses real full envelopes and finalized chronicles, entirely in memory. */
export const createPerformanceFixtureCatalogue = (): PerformanceFixtureCatalogue => {
  const activeWorlds = PERFORMANCE_FIXTURE_CATALOGUE_ACTIVE_IDS.map(id => selectedPerformanceFixtureWorld(fixtureFor(id)))
  const chronicles = PERFORMANCE_FIXTURE_CATALOGUE_CHRONICLE_IDS.map((id, index) => finalizeWorldAsChronicle(selectedPerformanceFixtureWorld(fixtureFor(id)), index % 2 === 0 ? 'jomon-loss' : 'crew-extinction'))
  const index = chronicles.reduce((current, chronicle) => addChronicleToIndex(current, chronicle), activeWorlds.reduce((current, world) => addWorldToIndex(current, world), emptyWorldIndex()))
  return {
    version: MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION,
    activeWorldFixtureIds: [...PERFORMANCE_FIXTURE_CATALOGUE_ACTIVE_IDS],
    chronicleFixtureIds: [...PERFORMANCE_FIXTURE_CATALOGUE_CHRONICLE_IDS],
    index,
    canonicalBytes: serializedByteBudgetFor('world-index', index)
  }
}
