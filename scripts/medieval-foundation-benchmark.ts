import { performance } from 'node:perf_hooks'
import { createDetailedRendererAdapterModel, createDetailedRendererSourceBundle } from '../src/medieval/detailed-renderer-adapter'
import { JOMON_ASCII_GLYPH_CATALOG } from '../src/medieval/ascii-glyphs'
import { classifyMedievalContent } from '../src/medieval/content-safety'
import { createManagementSidebarModel } from '../src/medieval/management-sidebar'
import {
  MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES,
  MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES,
  MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_IDS,
  MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_LABELS,
  MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES,
  MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION,
  serializedByteBudgetFor,
  summarizeBenchmarkTimings,
  type BenchmarkTimingSummary,
  type MedievalFoundationBenchmarkFixture,
  type MedievalFoundationBenchmarkOperationId,
  type SerializedByteBudgetResult
} from '../src/medieval/performance-budget'
import {
  MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION,
  MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX,
  PERFORMANCE_FIXTURE_MEASUREMENT_BOUNDARIES,
  advancePerformanceFixtureDueEvents,
  createPerformanceFixtureCatalogue,
  resolveMedievalPerformanceFixtureMatrix,
  runPerformanceFixtureScenario,
  selectedPerformanceFixtureWorld
} from '../src/medieval/performance-fixtures'
import { defaultCreationSettings, emptyCreationSettingsRecord, saveCreationSettingsProfile } from '../src/medieval/settings'
import { defaultTerminalControlPreferences } from '../src/medieval/terminal-controls'
import { createTerminalPresentationModel } from '../src/medieval/terminal-presentation'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld } from '../src/medieval/world'
import type { FoundationWorld } from '../src/medieval/types'

interface BenchmarkResult {
  fixtureId: string
  operation: MedievalFoundationBenchmarkOperationId
  timing: BenchmarkTimingSummary
}

interface FixtureSizeResult {
  fixtureId: string
  initialActiveWorld: SerializedByteBudgetResult
  afterDueEventsActiveWorld: SerializedByteBudgetResult
  growthBytes: number
  sharedExplicitSaveOnlyRecords: readonly SerializedByteBudgetResult[]
}

const selectedWorld = (fixture: MedievalFoundationBenchmarkFixture): FoundationWorld => selectedPerformanceFixtureWorld(fixture)

const timed = (work: () => void): number => {
  const started = performance.now()
  work()
  return performance.now() - started
}

/** Setup sits outside timing so every named operation is measured on its own. */
const operation = (fixture: MedievalFoundationBenchmarkFixture, id: MedievalFoundationBenchmarkOperationId, sample: number): void => {
  if (id === 'foundation-world-generation') {
    createFoundationWorld({ seed: fixture.seed, configuration: fixture.configuration })
    return
  }
  if (id === 'initial-courier-selection') {
    const world = createFoundationWorld({ seed: fixture.seed, configuration: fixture.configuration })
    chooseInitialCourier(world, 'crew:0')
    return
  }
  const world = selectedWorld(fixture)
  if (id === 'terminal-presentation-projection') {
    createTerminalPresentationModel(world)
    return
  }
  if (id === 'management-sidebar-projection') {
    createManagementSidebarModel(world)
    return
  }
  if (id === 'detailed-adapter-projection') {
    const terminal = createTerminalPresentationModel(world)
    const sidebar = createManagementSidebarModel(world)
    const bundle = createDetailedRendererSourceBundle({ version: 1, terminal, sidebar, glyphCatalog: JOMON_ASCII_GLYPH_CATALOG, controls: defaultTerminalControlPreferences() })
    createDetailedRendererAdapterModel(bundle)
    return
  }
  if (id === 'scheduled-summary-due-event-sequence') {
    advancePerformanceFixtureDueEvents(world, fixture.id)
    return
  }
  advanceFoundationWorldTime(world, {
    id: `benchmark:${fixture.id}:${sample}`,
    kind: 'wait',
    durationMinutes: 240,
    contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
  })
}

const measure = (fixture: MedievalFoundationBenchmarkFixture, id: MedievalFoundationBenchmarkOperationId): BenchmarkTimingSummary => {
  for (let sample = 0; sample < MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES; sample += 1) operation(fixture, id, sample)
  const samples: number[] = []
  for (let sample = 0; sample < MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES; sample += 1) samples.push(timed(() => operation(fixture, id, sample + MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES)))
  return summarizeBenchmarkTimings(samples)
}

const byteResults = (): readonly FixtureSizeResult[] => {
  const scenarios = MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.map(runPerformanceFixtureScenario)
  const settings = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.reduce((record, fixture, index) => saveCreationSettingsProfile(record, `baseline ${index + 1}`, {
    ...defaultCreationSettings(),
    seed: fixture.seed,
    configuration: { preset: fixture.expectedPreset, advanced: {} },
    advancedMode: true
  }), emptyCreationSettingsRecord())
  const catalogue = createPerformanceFixtureCatalogue()
  return scenarios.map(scenario => ({
    fixtureId: scenario.fixtureId,
    initialActiveWorld: scenario.envelopeSizes.before,
    afterDueEventsActiveWorld: scenario.envelopeSizes.after,
    growthBytes: scenario.envelopeSizes.deltaBytes,
    sharedExplicitSaveOnlyRecords: [
      serializedByteBudgetFor('terminal-controls', defaultTerminalControlPreferences()),
      serializedByteBudgetFor('creation-settings', settings),
      catalogue.canonicalBytes
    ]
  }))
}

const rounded = (value: number): number => Number(value.toFixed(3))
const printableTiming = (summary: BenchmarkTimingSummary): string => `min ${rounded(summary.minimumMilliseconds)} ms | p50 ${rounded(summary.p50Milliseconds)} ms | p95 ${rounded(summary.p95Milliseconds)} ms | max ${rounded(summary.maximumMilliseconds)} ms (${summary.samples} samples)`

const resolvedFixtures = resolveMedievalPerformanceFixtureMatrix()
const results: BenchmarkResult[] = []
for (const fixture of MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX) {
  for (const id of MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_IDS) results.push({ fixtureId: fixture.id, operation: id, timing: measure(fixture, id) })
}
const sizes = byteResults()

const report = {
  contractVersion: MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION,
  fixtureContractVersion: MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION,
  measurementBoundaries: PERFORMANCE_FIXTURE_MEASUREMENT_BOUNDARIES,
  method: {
    wallClock: 'review-only Node performance.now() observations; never deterministic assertions or canonical state',
    warmupSamples: MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES,
    measuredSamples: MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES,
    percentile: 'linear interpolation on sorted samples',
    persistence: 'none; benchmark creates validated in-memory deterministic full envelopes only',
    network: 'none',
    processMemory: 'not collected: non-portable and GC-sensitive',
    browserResponsiveness: 'not measured',
    indexedDbTiming: 'not measured'
  },
  runtime: {
    node: process.version,
    platform: process.platform,
    architecture: process.arch
  },
  fixtures: resolvedFixtures.map(item => ({
    id: item.fixture.id,
    seed: item.fixture.seed,
    resolvedConfiguration: item.resolvedConfiguration,
    initialWorldCaps: item.initialWorldCaps,
    generatedInitialPersonSeedCap: item.generatedInitialPersonSeedCap,
    instantiatedPersistentPeople: item.instantiatedPersistentPeople,
    fidelityBudget: item.fidelityBudget,
    schedulerCadenceMinutes: item.schedulerCadenceMinutes
  })),
  deterministicFixtureScenarios: MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.map(runPerformanceFixtureScenario),
  deterministicCatalogue: createPerformanceFixtureCatalogue(),
  timing: results.map(item => ({ fixtureId: item.fixtureId, operation: item.operation, ...item.timing })),
  serializedBytes: sizes
}

console.log(`Jomon medieval foundation benchmark v${MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION}`)
console.log(`Runtime: Node ${process.version} on ${process.platform}/${process.arch}; no browser, IndexedDB, filesystem write, or network operation is timed.`)
console.log(`Method: ${MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES} warm-ups, ${MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES} measured samples; performance.now(); p50/p95 are review-only Node observations.`)
for (const item of results) console.log(`${item.fixtureId} | ${MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_LABELS[item.operation]} | ${printableTiming(item.timing)}`)
for (const item of sizes) console.log(`${item.fixtureId} | canonical full envelope bytes | initial ${item.initialActiveWorld.bytes}/${item.initialActiveWorld.maximumBytes} | after due events ${item.afterDueEventsActiveWorld.bytes}/${item.afterDueEventsActiveWorld.maximumBytes} | growth ${item.growthBytes} | ${item.sharedExplicitSaveOnlyRecords.map(record => `${record.kind} ${record.bytes}/${record.maximumBytes} ${record.withinBudget ? 'within' : 'over'}`).join(' | ')}`)
console.log(`RESULT_JSON ${JSON.stringify(report)}`)
