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
  resolveMedievalFoundationBenchmarkFixture,
  serializedByteBudgetFor,
  summarizeBenchmarkTimings,
  type BenchmarkTimingSummary,
  type MedievalFoundationBenchmarkFixture,
  type MedievalFoundationBenchmarkOperationId,
  type SerializedByteBudgetResult
} from '../src/medieval/performance-budget'
import { defaultCreationSettings, emptyCreationSettingsRecord, saveCreationSettingsProfile } from '../src/medieval/settings'
import { addWorldToIndex, emptyWorldIndex } from '../src/medieval/storage'
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
  records: readonly SerializedByteBudgetResult[]
}

const selectedWorld = (fixture: MedievalFoundationBenchmarkFixture): FoundationWorld => chooseInitialCourier(createFoundationWorld({ seed: fixture.seed, configuration: fixture.configuration }), 'crew:0')

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
  const worlds = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.map(selectedWorld)
  const advancedWorlds = worlds.map((world, index) => advanceFoundationWorldTime(world, {
    id: `benchmark:size:${index}`,
    kind: 'wait',
    durationMinutes: 240,
    contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
  }))
  const settings = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.reduce((record, fixture, index) => saveCreationSettingsProfile(record, `baseline ${index + 1}`, {
    ...defaultCreationSettings(),
    seed: fixture.seed,
    configuration: { preset: fixture.expectedPreset, advanced: {} },
    advancedMode: true
  }), emptyCreationSettingsRecord())
  const index = worlds.reduce((catalog, world) => addWorldToIndex(catalog, world), emptyWorldIndex())
  return advancedWorlds.map(world => ({
    fixtureId: MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.find(fixture => fixture.seed === world.manifest.creation.seed)!.id,
    records: [
      serializedByteBudgetFor('active-world', world),
      serializedByteBudgetFor('terminal-controls', defaultTerminalControlPreferences()),
      serializedByteBudgetFor('creation-settings', settings),
      serializedByteBudgetFor('world-index', index)
    ]
  }))
}

const rounded = (value: number): number => Number(value.toFixed(3))
const printableTiming = (summary: BenchmarkTimingSummary): string => `min ${rounded(summary.minimumMilliseconds)} ms | p50 ${rounded(summary.p50Milliseconds)} ms | p95 ${rounded(summary.p95Milliseconds)} ms | max ${rounded(summary.maximumMilliseconds)} ms (${summary.samples} samples)`

const resolvedFixtures = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.map(resolveMedievalFoundationBenchmarkFixture)
const results: BenchmarkResult[] = []
for (const fixture of MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES) {
  for (const id of MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_IDS) results.push({ fixtureId: fixture.id, operation: id, timing: measure(fixture, id) })
}
const sizes = byteResults()

const report = {
  contractVersion: MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION,
  method: {
    wallClock: 'node:perf_hooks performance.now()',
    warmupSamples: MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES,
    measuredSamples: MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES,
    percentile: 'linear interpolation on sorted samples',
    persistence: 'none; benchmark creates in-memory deterministic worlds only',
    network: 'none'
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
  timing: results.map(item => ({ fixtureId: item.fixtureId, operation: item.operation, ...item.timing })),
  serializedBytes: sizes
}

console.log(`Jomon medieval foundation benchmark v${MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION}`)
console.log(`Runtime: Node ${process.version} on ${process.platform}/${process.arch}; no browser, IndexedDB, filesystem write, or network operation is timed.`)
console.log(`Method: ${MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES} warm-ups, ${MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES} measured samples; performance.now(); p50/p95 linear interpolation.`)
for (const item of results) console.log(`${item.fixtureId} | ${MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_LABELS[item.operation]} | ${printableTiming(item.timing)}`)
for (const item of sizes) console.log(`${item.fixtureId} | serialized bytes | ${item.records.map(record => `${record.kind} ${record.bytes}/${record.maximumBytes} ${record.withinBudget ? 'within' : 'over'}`).join(' | ')}`)
console.log(`RESULT_JSON ${JSON.stringify(report)}`)
