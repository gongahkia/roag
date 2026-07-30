import { POLICY_FEATURE_VERSION, type PolicyFeatureVector } from './autoplay-features'
import { AUTOPLAY_TRACE_VERSION, assertAutoplayTraceDocument, type AutoplayTraceDocument } from './autoplay-trace'
import { AUTOPLAY_SEED_CORPUS_PARTITIONS, type AutoplaySeedCorpusPartition } from './autoplay-seed-corpus'

export const AUTOPLAY_POLICY_DATASET_VERSION = 1 as const
export const AUTOPLAY_POLICY_DATASET_FIELD_PROVENANCE = {
  observation: 'AP-03 decision-time observation',
  history: 'AP-03 bounded prior action/outcome history',
  legalActions: 'AP-03 legal candidates at decision time',
  chosenAction: 'AP-03 selected candidate',
  reward: 'AP-03 transition events and resource deltas',
  terminal: 'AP-03 episode terminal result',
  metadata: 'AP-03 trace and policy version metadata'
} as const

export interface AutoplayPolicyDatasetMetadata { partition: AutoplaySeedCorpusPartition; seed: number; sequence: number; traceVersion: typeof AUTOPLAY_TRACE_VERSION; featureVersion: typeof POLICY_FEATURE_VERSION; policyVersion: number; objectiveVersion: number; informationMode: 'visible' | 'omniscient'; policy: string; heuristicProfile?: { id: string; version: number }; fieldProvenance: typeof AUTOPLAY_POLICY_DATASET_FIELD_PROVENANCE }
export interface AutoplayPolicyDatasetRecord { version: typeof AUTOPLAY_POLICY_DATASET_VERSION; metadata: AutoplayPolicyDatasetMetadata; observation: AutoplayTraceDocument['records'][number]['observation']; history: PolicyFeatureVector['history']; legalActions: Array<{ command: string; reason: string; score: number }>; chosenAction: { command: string; reason: string }; reward: { events: string[]; resourceDelta: AutoplayTraceDocument['records'][number]['resourceDelta'] }; terminal: AutoplayTraceDocument['terminal'] }

const hiddenVisibleFields = new Set(['replay', 'layoutId', 'macroRecipeId', 'routeContractId', 'objectiveId', 'escalation', 'hiddenMap', 'fullMap'])
const object = (value: unknown): Record<string, unknown> | undefined => value !== null && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : undefined
const partition = (value: unknown): AutoplaySeedCorpusPartition => {
  if (!AUTOPLAY_SEED_CORPUS_PARTITIONS.includes(value as AutoplaySeedCorpusPartition)) throw new Error(`invalid autoplay policy dataset partition: ${String(value)}`)
  return value as AutoplaySeedCorpusPartition
}
const assertNoHiddenVisibleFields = (value: unknown): void => {
  if (Array.isArray(value)) { value.forEach(assertNoHiddenVisibleFields); return }
  const record = object(value)
  if (!record) return
  for (const [key, child] of Object.entries(record)) {
    if (hiddenVisibleFields.has(key)) throw new Error(`visible autoplay policy dataset contains forbidden field: ${key}`)
    assertNoHiddenVisibleFields(child)
  }
}

export const assertAutoplayPolicyDatasetRecord = (record: AutoplayPolicyDatasetRecord): void => {
  if (!record || record.version !== AUTOPLAY_POLICY_DATASET_VERSION) throw new Error('unsupported autoplay policy dataset version')
  const metadata = record.metadata
  if (!metadata || partition(metadata.partition) !== metadata.partition || !Number.isSafeInteger(metadata.seed) || metadata.seed < 0 || !Number.isSafeInteger(metadata.sequence) || metadata.sequence < 0) throw new Error('invalid autoplay policy dataset metadata')
  if (metadata.traceVersion !== AUTOPLAY_TRACE_VERSION || metadata.featureVersion !== POLICY_FEATURE_VERSION || !metadata.policy || !['visible', 'omniscient'].includes(metadata.informationMode)) throw new Error('unsupported autoplay policy dataset trace or feature version')
  if (!record.observation || !Array.isArray(record.history) || !Array.isArray(record.legalActions) || !record.chosenAction?.command || !Array.isArray(record.reward?.events) || !record.reward.resourceDelta || !record.terminal?.outcome) throw new Error('invalid autoplay policy dataset record fields')
  if (metadata.informationMode === 'visible') assertNoHiddenVisibleFields(record)
}

export const autoplayPolicyDatasetRecords = (documents: readonly AutoplayTraceDocument[], value: AutoplaySeedCorpusPartition): AutoplayPolicyDatasetRecord[] => {
  const datasetPartition = partition(value)
  const sorted = [...documents].sort((left, right) => left.episode.seed - right.episode.seed || left.episode.informationMode.localeCompare(right.episode.informationMode) || left.episode.policy.localeCompare(right.episode.policy) || left.hash.localeCompare(right.hash))
  const records = sorted.flatMap(document => {
    assertAutoplayTraceDocument(document)
    return document.records.map(trace => ({
      version: AUTOPLAY_POLICY_DATASET_VERSION,
      metadata: {
        partition: datasetPartition,
        seed: document.episode.seed,
        sequence: trace.sequence,
        traceVersion: document.version,
        featureVersion: trace.features.version,
        policyVersion: document.episode.policyVersion,
        objectiveVersion: document.episode.objectiveVersion,
        informationMode: document.episode.informationMode,
        policy: document.episode.policy,
        ...(document.episode.heuristicProfile ? { heuristicProfile: { ...document.episode.heuristicProfile } } : {}),
        fieldProvenance: AUTOPLAY_POLICY_DATASET_FIELD_PROVENANCE
      },
      observation: structuredClone(trace.observation),
      history: structuredClone(trace.features.history),
      legalActions: trace.legalCandidates.map(candidate => ({ ...candidate })),
      chosenAction: { ...trace.chosen },
      reward: { events: [...trace.outcome.events], resourceDelta: { ...trace.resourceDelta } },
      terminal: structuredClone(document.terminal)
    } satisfies AutoplayPolicyDatasetRecord))
  })
  records.forEach(assertAutoplayPolicyDatasetRecord)
  return records
}

export const exportAutoplayPolicyDatasetJsonl = (documents: readonly AutoplayTraceDocument[], partition: AutoplaySeedCorpusPartition): string => {
  const records = autoplayPolicyDatasetRecords(documents, partition)
  return records.length ? `${records.map(record => JSON.stringify(record)).join('\n')}\n` : ''
}
