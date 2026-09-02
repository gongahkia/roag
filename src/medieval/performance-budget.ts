import { FIDELITY_BUDGETS, type FidelityBudget } from './fidelity'
import { resolveWorldGenerationConfig, type SimulationFidelity, type WorldGenerationConfig, type WorldGenerationConfigRequest } from './generation-config'
import { INITIAL_WORLD_LIMITS } from './initial-world'

/**
 * Phase 1.6's reproducible planning contract. It owns benchmark fixture
 * identities, deterministic byte-accounting, and browser capability policy;
 * it does not alter a world, open storage, or select a browser implementation.
 */
export const MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION = 1 as const

export const MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES = 2 as const
export const MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES = 9 as const

export const MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_IDS = [
  'foundation-world-generation',
  'initial-courier-selection',
  'terminal-presentation-projection',
  'management-sidebar-projection',
  'detailed-adapter-projection',
  'bounded-temporal-action'
] as const
export type MedievalFoundationBenchmarkOperationId = typeof MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_IDS[number]

export const MEDIEVAL_FOUNDATION_BENCHMARK_OPERATION_LABELS: Readonly<Record<MedievalFoundationBenchmarkOperationId, string>> = {
  'foundation-world-generation': 'Foundation world generation',
  'initial-courier-selection': 'Initial courier selection',
  'terminal-presentation-projection': 'Terminal presentation projection',
  'management-sidebar-projection': 'Management sidebar projection',
  'detailed-adapter-projection': 'Deferred detailed-adapter projection',
  'bounded-temporal-action': 'Bounded 240-minute wait action'
}

export interface MedievalFoundationBenchmarkFixture {
  id: 'sheltered-reach-focused' | 'watershed-balanced' | 'far-coast-deep'
  label: string
  seed: string
  configuration: WorldGenerationConfigRequest
  expectedPreset: 'sheltered-reach' | 'watershed' | 'far-coast'
  expectedSimulationFidelity: SimulationFidelity
  /** Generated initial-world people are seeds, not mutable persistent people. */
  expectedInstantiatedPeople: 6
}

/** Fixed inputs make every measurement comparable without claiming a later world scale. */
export const MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES: readonly MedievalFoundationBenchmarkFixture[] = [
  {
    id: 'sheltered-reach-focused',
    label: 'Sheltered Reach / focused',
    seed: 'performance-sheltered-reach',
    configuration: { preset: 'sheltered-reach' },
    expectedPreset: 'sheltered-reach',
    expectedSimulationFidelity: 'focused',
    expectedInstantiatedPeople: 6
  },
  {
    id: 'watershed-balanced',
    label: 'Watershed / balanced',
    seed: 'performance-watershed',
    configuration: { preset: 'watershed' },
    expectedPreset: 'watershed',
    expectedSimulationFidelity: 'balanced',
    expectedInstantiatedPeople: 6
  },
  {
    id: 'far-coast-deep',
    label: 'Far Coast / deep',
    seed: 'performance-far-coast',
    configuration: { preset: 'far-coast' },
    expectedPreset: 'far-coast',
    expectedSimulationFidelity: 'deep',
    expectedInstantiatedPeople: 6
  }
] as const

export const MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES = {
  loaded: 1,
  nearby: 5,
  recurring: 30,
  distantIndividualSummary: 120,
  distantSettlementSummary: 240,
  distantInstitutionSummary: 240,
  deferredAndHistorical: 'not-scheduled'
} as const

export interface ResolvedMedievalFoundationBenchmarkFixture {
  fixture: MedievalFoundationBenchmarkFixture
  resolvedConfiguration: WorldGenerationConfig
  initialWorldCaps: typeof INITIAL_WORLD_LIMITS
  generatedInitialPersonSeedCap: typeof INITIAL_WORLD_LIMITS.people
  instantiatedPersistentPeople: number
  fidelityBudget: FidelityBudget
  schedulerCadenceMinutes: typeof MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES
}

export class MedievalPerformanceBudgetContractError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'MedievalPerformanceBudgetContractError'
  }
}

/** Resolves the same configuration authority used by world creation. */
export const resolveMedievalFoundationBenchmarkFixture = (fixture: MedievalFoundationBenchmarkFixture): ResolvedMedievalFoundationBenchmarkFixture => {
  const resolution = resolveWorldGenerationConfig(fixture.configuration)
  if (resolution.status !== 'valid'
    || resolution.configuration.preset !== fixture.expectedPreset
    || resolution.configuration.simulationFidelity !== fixture.expectedSimulationFidelity) {
    throw new MedievalPerformanceBudgetContractError(`invalid foundation benchmark fixture: ${fixture.id}`)
  }
  return {
    fixture: structuredClone(fixture),
    resolvedConfiguration: structuredClone(resolution.configuration),
    initialWorldCaps: { ...INITIAL_WORLD_LIMITS },
    generatedInitialPersonSeedCap: INITIAL_WORLD_LIMITS.people,
    instantiatedPersistentPeople: fixture.expectedInstantiatedPeople,
    fidelityBudget: { ...FIDELITY_BUDGETS[resolution.configuration.simulationFidelity] },
    schedulerCadenceMinutes: { ...MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES }
  }
}

export type MedievalPerformanceBudgetClassification = 'measured-current-baseline' | 'enforced-deterministic-size-budget' | 'future-performance-target' | 'storage-planning-band'
export type MedievalPerformanceBudgetUnit = 'milliseconds' | 'bytes'
export type MedievalPerformanceStatistic = 'p50' | 'p95' | 'maximum' | 'total'

export interface MedievalPerformanceBudget {
  id: string
  label: string
  unit: MedievalPerformanceBudgetUnit
  statistic: MedievalPerformanceStatistic
  maximum: number
  classification: MedievalPerformanceBudgetClassification
  /** The benchmark only measures the current foundation operations listed above. */
  scope: 'current-foundation' | 'future-browser-target' | 'future-storage-planning'
}

const kib = 1024
const mib = kib * kib

/**
 * Timing caps are review targets, not CI assertions: browser and device timing
 * varies. JSON byte caps below are deterministic fixture assertions only; they
 * are not a browser-quota policy or a runtime save rejection rule.
 */
export const MEDIEVAL_PERFORMANCE_STORAGE_BUDGETS: readonly MedievalPerformanceBudget[] = [
  { id: 'cold-browser-ready-focused-canvas', label: 'Cold browser app ready with focused canvas', unit: 'milliseconds', statistic: 'p95', maximum: 2_000, classification: 'future-performance-target', scope: 'future-browser-target' },
  { id: 'create-and-save-current-preset', label: 'Create and save each current preset', unit: 'milliseconds', statistic: 'p95', maximum: 1_500, classification: 'future-performance-target', scope: 'future-browser-target' },
  { id: 'selected-world-resume-load', label: 'Selected-world resume/load', unit: 'milliseconds', statistic: 'p95', maximum: 250, classification: 'future-performance-target', scope: 'future-browser-target' },
  { id: 'terminal-sidebar-detailed-projection', label: 'One terminal/sidebar/detailed-adapter pure projection', unit: 'milliseconds', statistic: 'p95', maximum: 500, classification: 'future-performance-target', scope: 'future-browser-target' },
  { id: 'zero-time-ui-input-response', label: 'One zero-time UI input response', unit: 'milliseconds', statistic: 'p95', maximum: 50, classification: 'future-performance-target', scope: 'future-browser-target' },
  { id: 'bounded-scheduler-time-action', label: 'One bounded current scheduler/time-bearing action', unit: 'milliseconds', statistic: 'p95', maximum: 500, classification: 'future-performance-target', scope: 'future-browser-target' },
  { id: 'indexeddb-world-save-load', label: 'IndexedDB active-world save or load', unit: 'milliseconds', statistic: 'p95', maximum: 250, classification: 'future-performance-target', scope: 'future-browser-target' },
  { id: 'active-world-json', label: 'Current fixture active-world canonical JSON', unit: 'bytes', statistic: 'maximum', maximum: 512 * kib, classification: 'enforced-deterministic-size-budget', scope: 'current-foundation' },
  { id: 'terminal-controls-json', label: 'Terminal-control preference canonical JSON', unit: 'bytes', statistic: 'maximum', maximum: 4 * kib, classification: 'enforced-deterministic-size-budget', scope: 'current-foundation' },
  { id: 'creation-settings-json', label: 'Creation-settings record canonical JSON', unit: 'bytes', statistic: 'maximum', maximum: 16 * kib, classification: 'enforced-deterministic-size-budget', scope: 'current-foundation' },
  { id: 'world-index-json', label: 'Three-world index canonical JSON', unit: 'bytes', statistic: 'maximum', maximum: 64 * kib, classification: 'enforced-deterministic-size-budget', scope: 'current-foundation' },
  { id: 'local-storage-planning-normal', label: 'Normal all-record local-storage planning band', unit: 'bytes', statistic: 'total', maximum: 16 * mib, classification: 'storage-planning-band', scope: 'future-storage-planning' },
  { id: 'local-storage-planning-warning', label: 'Advisory local-storage planning warning', unit: 'bytes', statistic: 'total', maximum: 24 * mib, classification: 'storage-planning-band', scope: 'future-storage-planning' },
  { id: 'local-storage-planning-pressure', label: 'Elevated local-storage pressure review point', unit: 'bytes', statistic: 'total', maximum: 32 * mib, classification: 'storage-planning-band', scope: 'future-storage-planning' }
]

export type MedievalSerializedRecordKind = 'active-world' | 'terminal-controls' | 'creation-settings' | 'world-index'

const sizeBudgetId: Readonly<Record<MedievalSerializedRecordKind, string>> = {
  'active-world': 'active-world-json',
  'terminal-controls': 'terminal-controls-json',
  'creation-settings': 'creation-settings-json',
  'world-index': 'world-index-json'
}

export interface SerializedByteBudgetResult {
  kind: MedievalSerializedRecordKind
  bytes: number
  maximumBytes: number
  withinBudget: boolean
}

const canonicalJson = (value: unknown): string => {
  if (value === null || typeof value === 'string' || typeof value === 'boolean') return JSON.stringify(value)
  if (typeof value === 'number') {
    if (!Number.isFinite(value)) throw new MedievalPerformanceBudgetContractError('canonical JSON byte accounting requires finite numbers')
    return JSON.stringify(Object.is(value, -0) ? 0 : value)
  }
  if (Array.isArray(value)) return `[${value.map(canonicalJson).join(',')}]`
  if (!value || typeof value !== 'object' || (Object.getPrototypeOf(value) !== Object.prototype && Object.getPrototypeOf(value) !== null)) {
    throw new MedievalPerformanceBudgetContractError('canonical JSON byte accounting requires serializable records')
  }
  const record = value as Record<string, unknown>
  return `{${Object.keys(record).sort().map(key => `${JSON.stringify(key)}:${canonicalJson(record[key])}`).join(',')}}`
}

/** Stable UTF-8 JSON bytes, suitable for fixture accounting but not quota prediction. */
export const canonicalSerializedJson = (value: unknown): string => canonicalJson(value)
export const canonicalSerializedByteLength = (value: unknown): number => new TextEncoder().encode(canonicalJson(value)).byteLength

const budgetFor = (id: string): MedievalPerformanceBudget => {
  const budget = MEDIEVAL_PERFORMANCE_STORAGE_BUDGETS.find(candidate => candidate.id === id)
  if (!budget || budget.unit !== 'bytes') throw new MedievalPerformanceBudgetContractError(`missing byte budget: ${id}`)
  return budget
}

export const serializedByteBudgetFor = (kind: MedievalSerializedRecordKind, value: unknown): SerializedByteBudgetResult => {
  const budget = budgetFor(sizeBudgetId[kind])
  const bytes = canonicalSerializedByteLength(value)
  return { kind, bytes, maximumBytes: budget.maximum, withinBudget: bytes <= budget.maximum }
}

export interface BenchmarkTimingSummary {
  samples: number
  minimumMilliseconds: number
  p50Milliseconds: number
  p95Milliseconds: number
  maximumMilliseconds: number
}

const percentile = (sorted: readonly number[], fraction: number): number => {
  const index = (sorted.length - 1) * fraction
  const lower = Math.floor(index)
  const upper = Math.ceil(index)
  if (lower === upper) return sorted[lower]!
  return sorted[lower]! + (sorted[upper]! - sorted[lower]!) * (index - lower)
}

/** p50/p95 use linear interpolation on the sorted finite sample set. */
export const summarizeBenchmarkTimings = (samples: readonly number[]): BenchmarkTimingSummary => {
  if (!Array.isArray(samples) || samples.length === 0 || samples.some(value => !Number.isFinite(value) || value < 0)) {
    throw new MedievalPerformanceBudgetContractError('benchmark timing samples must be a non-empty set of non-negative finite milliseconds')
  }
  const sorted = [...samples].sort((left, right) => left - right)
  return {
    samples: sorted.length,
    minimumMilliseconds: sorted[0]!,
    p50Milliseconds: percentile(sorted, 0.5),
    p95Milliseconds: percentile(sorted, 0.95),
    maximumMilliseconds: sorted[sorted.length - 1]!
  }
}

export const REQUIRED_DESKTOP_BROWSER_CAPABILITIES = [
  'es-modules',
  'canvas-2d',
  'keyboard-events-and-focus',
  'indexeddb-and-structured-clone',
  'blob-and-object-urls',
  'guarded-font-loading'
] as const
export type RequiredDesktopBrowserCapability = typeof REQUIRED_DESKTOP_BROWSER_CAPABILITIES[number]

/** Standards-and-feature-detection policy; no user-agent or vendor branch is permitted. */
export const DESKTOP_BROWSER_SUPPORT_POLICY = {
  supportedChannels: ['Chrome stable', 'Edge stable', 'Firefox stable', 'Safari stable'] as const,
  automatedBaseline: 'Playwright Chromium',
  requiredCapabilities: REQUIRED_DESKTOP_BROWSER_CAPABILITIES,
  optionalDiagnostics: ['navigator.storage.estimate'] as const,
  prohibitedBaselineRequirements: ['DedicatedWorker', 'SharedWorker', 'OPFS', 'requestIdleCallback', 'WebGPU', 'vendor-specific-api'] as const,
  reviewCadence: 'Review browser support and measured versions at least every six months and whenever this benchmark or browser automation changes.'
} as const

export interface BrowserCapabilityEvidence {
  readonly [capability: string]: boolean
}

export interface BrowserCapabilityReport {
  version: typeof MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION
  supported: boolean
  missing: readonly RequiredDesktopBrowserCapability[]
}

/** A pure feature-evidence evaluator for browser adapters and manual testing. */
export const evaluateRequiredDesktopBrowserCapabilities = (evidence: BrowserCapabilityEvidence): BrowserCapabilityReport => {
  const missing = REQUIRED_DESKTOP_BROWSER_CAPABILITIES.filter(capability => evidence[capability] !== true)
  return { version: MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION, supported: missing.length === 0, missing }
}

export interface OptionalStorageEstimate {
  quota?: number
  usage?: number
}

export interface OptionalStorageEstimateProvider {
  estimate: () => Promise<OptionalStorageEstimate>
}

export type OptionalStorageEstimateDiagnostic =
  | { status: 'unavailable' }
  | { status: 'available'; quota?: number; usage?: number }
  | { status: 'failed' }

/**
 * This is advisory only. A private-mode refusal, missing API, unusual quota,
 * or eviction behavior cannot stop play and must never trigger cleanup.
 */
export const probeOptionalStorageEstimate = async (provider: OptionalStorageEstimateProvider | undefined): Promise<OptionalStorageEstimateDiagnostic> => {
  if (!provider || typeof provider.estimate !== 'function') return { status: 'unavailable' }
  try {
    const estimate = await provider.estimate()
    const quota = Number.isFinite(estimate.quota) && (estimate.quota ?? 0) >= 0 ? estimate.quota : undefined
    const usage = Number.isFinite(estimate.usage) && (estimate.usage ?? 0) >= 0 ? estimate.usage : undefined
    return { status: 'available', ...(quota === undefined ? {} : { quota }), ...(usage === undefined ? {} : { usage }) }
  } catch {
    return { status: 'failed' }
  }
}
