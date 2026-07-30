import { replayAutoplayTrace, type AutoplayTraceReplayResult } from './autoplay-trace-replay'
import type { AutoplayHeuristicProfileRef } from './autoplay-heuristics'
import type { AutoplayMode, AutoplayOptionalOutcomes, AutoplayPartyOutcomes, AutoplayPolicy, AutoplayReplayMetadata, AutoplayResourceOutcomes, AutoplayToolOutcomes, AutoplayTraceEntry } from './types'
import type { AutoplayOutcome } from './autoplay-runner'
import type { AutoplayTraceDocument } from './autoplay-trace'
import type { AutoplaySeedCorpusPartition } from './autoplay-seed-corpus'

export const AUTOPLAY_FAILURE_DIAGNOSIS_VERSION = 1 as const
export const autoplayFailureCodes = ['stall', 'death', 'illegal-action', 'hidden-route-miss', 'resource-waste', 'timeout', 'replay-divergence'] as const
export type AutoplayFailureCode = typeof autoplayFailureCodes[number]
export type AutoplayFailureTrace = Array<Pick<AutoplayTraceEntry, 'turn' | 'replay' | 'command' | 'reason' | 'events'>>
export interface AutoplayFailureDiagnosticInput {
  seed: number
  partition: AutoplaySeedCorpusPartition
  mode: Exclude<AutoplayMode, 'off'>
  policy: AutoplayPolicy
  heuristicProfile?: AutoplayHeuristicProfileRef
  turnLimit: number
  outcome: Exclude<AutoplayOutcome, 'complete'>
  replay: AutoplayReplayMetadata
  exitPath: 'clear' | 'actor-blocked' | 'terrain-blocked'
  trace: AutoplayFailureTrace
  resources: AutoplayResourceOutcomes & { bombsUsed: number; ropesUsed: number }
  tools: AutoplayToolOutcomes
  optional: AutoplayOptionalOutcomes
  party?: AutoplayPartyOutcomes
  reason?: string
  error?: string
  traceDocument?: AutoplayTraceDocument
}
export interface AutoplayFailureDiagnosis {
  version: typeof AUTOPLAY_FAILURE_DIAGNOSIS_VERSION
  code: AutoplayFailureCode
  evidence: string[]
  reproduction: { seed: number; partition: AutoplaySeedCorpusPartition; mode: Exclude<AutoplayMode, 'off'>; policy: AutoplayPolicy; heuristicProfile?: AutoplayHeuristicProfileRef; turnLimit: number; replay: AutoplayReplayMetadata }
  finalDecisions: AutoplayFailureTrace
  resources: AutoplayFailureDiagnosticInput['resources']
  traversal: { tools: AutoplayToolOutcomes; optional: AutoplayOptionalOutcomes }
  party?: AutoplayPartyOutcomes
}

const failedReplay = (traceDocument: AutoplayTraceDocument | undefined): AutoplayTraceReplayResult | undefined => traceDocument ? replayAutoplayTrace(traceDocument) : undefined

export const diagnoseAutoplayFailure = (input: AutoplayFailureDiagnosticInput): AutoplayFailureDiagnosis => {
  const replay = failedReplay(input.traceDocument)
  const finalReason = input.reason ?? input.trace.at(-1)?.reason
  const finiteResources = input.resources.bombsUsed + input.resources.ropesUsed
  const evidence = [`outcome=${input.outcome}`, ...(finalReason ? [`reason=${finalReason}`] : []), `exitPath=${input.exitPath}`]
  if (input.party) evidence.push(`system=party-autoplay roster=${input.party.roster.map(entry => `${entry.id}:${entry.role}:${entry.controlMode}`).join(',') || 'none'} active=${input.party.activeCompanionIds.join(',') || 'none'} actions=${Object.entries(input.party.actionsByCompanion).map(([id, actions]) => `${id}:${Object.entries(actions).map(([action, count]) => `${action}=${count}`).join('+')}`).join(',') || 'none'} injuries=${input.party.injuries.join(',') || 'none'} losses=${input.party.losses.join(',') || 'none'} resourceConsents=${input.party.finiteResourceConsents}`)
  let code: AutoplayFailureCode
  if (replay && !replay.valid) {
    code = 'replay-divergence'
    evidence.push(`replay=${replay.divergence?.field ?? 'unknown'} at turn=${replay.divergence?.turn ?? -1}`)
  } else if (input.error && /illegal|invalid (command|action|target)|unsupported command/i.test(input.error)) {
    code = 'illegal-action'
    evidence.push(`error=${input.error}`)
  } else if (input.outcome === 'dead') {
    code = 'death'
  } else if (input.mode === 'visible' && input.exitPath === 'terrain-blocked' && /find exit|reach frontier|survey/i.test(finalReason ?? '')) {
    code = 'hidden-route-miss'
    evidence.push('visible route search ended before a traversable exit route was observed')
  } else if (finiteResources > 0 && input.resources.criticalRouteSelections === 0 && input.exitPath === 'terrain-blocked') {
    code = 'resource-waste'
    evidence.push(`spent=${finiteResources} without a selected critical route`)
  } else if (input.outcome === 'turn-limit') {
    code = 'timeout'
  } else {
    code = 'stall'
  }
  return {
    version: AUTOPLAY_FAILURE_DIAGNOSIS_VERSION,
    code,
    evidence,
    reproduction: { seed: input.seed, partition: input.partition, mode: input.mode, policy: input.policy, ...(input.heuristicProfile ? { heuristicProfile: { ...input.heuristicProfile } } : {}), turnLimit: input.turnLimit, replay: { ...input.replay } },
    finalDecisions: input.trace.map(entry => ({ ...entry, replay: { ...entry.replay }, events: [...entry.events] })),
    resources: { ...input.resources },
    traversal: { tools: { ...input.tools }, optional: { ...input.optional } },
    ...(input.party ? { party: structuredClone(input.party) } : {})
  }
}

export const assertAutoplayFailureDiagnosis = (diagnosis: AutoplayFailureDiagnosis): void => {
  if (diagnosis.version !== AUTOPLAY_FAILURE_DIAGNOSIS_VERSION || !autoplayFailureCodes.includes(diagnosis.code)) throw new Error('autoplay failure diagnosis has an unsupported schema')
  if (!diagnosis.evidence.length || !Number.isSafeInteger(diagnosis.reproduction.seed) || diagnosis.reproduction.seed < 0 || !Number.isSafeInteger(diagnosis.reproduction.turnLimit) || diagnosis.reproduction.turnLimit < 1) throw new Error('autoplay failure diagnosis lacks reproducible evidence')
}

export const summarizeAutoplayFailureCodes = (diagnoses: readonly AutoplayFailureDiagnosis[]): Record<AutoplayFailureCode, number> => Object.fromEntries(autoplayFailureCodes.map(code => [code, diagnoses.filter(diagnosis => diagnosis.code === code).length])) as Record<AutoplayFailureCode, number>
