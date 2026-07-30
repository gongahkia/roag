import { autoplayDecision, autoplayTraceFingerprint, createAutoplayContext, recordAutoplayTransitionSnapshot, snapshotAutoplayTransition } from './autoplay'
import { appendPolicyFeatureHistory, encodePolicyFeatures, type PolicyFeatureHistoryEntry } from './autoplay-features'
import { createPolicyProfile } from './autoplay-policy'
import { autoplayHeuristicProfile } from './autoplay-heuristics'
import { newRun, perform } from './engine'
import { observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import { assertAutoplayTraceDocument, type AutoplayTraceDocument } from './autoplay-trace'

export interface AutoplayTraceDivergence { turn: number; field: string; expected: unknown; actual: unknown; command?: string }
export interface AutoplayTraceReplayResult { valid: boolean; terminal?: AutoplayTraceDocument['terminal']; divergence?: AutoplayTraceDivergence }

const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const failure = (record: AutoplayTraceDocument['records'][number] | undefined, field: string, expected: unknown, actual: unknown): AutoplayTraceReplayResult => ({ valid: false, divergence: { turn: record?.turn ?? -1, field, expected, actual, ...(record ? { command: record.chosen.command } : {}) } })

export const replayAutoplayTrace = (document: AutoplayTraceDocument): AutoplayTraceReplayResult => {
  try { assertAutoplayTraceDocument(document) }
  catch (error) { return failure(undefined, 'trace-integrity', 'valid hash chain', error instanceof Error ? error.message : String(error)) }
  const first = document.records[0]
  if (!first) return document.terminal.turns === 0 ? { valid: true, terminal: document.terminal } : failure(undefined, 'trace-records', document.terminal.turns, 0)
  const profile = createPolicyProfile({ policy: document.episode.policy, informationMode: document.episode.informationMode })
  if (profile.version !== document.episode.policyVersion || profile.objectiveVersion !== document.episode.objectiveVersion) return failure(first, 'policy-metadata', { policyVersion: document.episode.policyVersion, objectiveVersion: document.episode.objectiveVersion }, { policyVersion: profile.version, objectiveVersion: profile.objectiveVersion })
  if (document.episode.seed !== first.replay.seed) return failure(first, 'seed', document.episode.seed, first.replay.seed)
  let heuristicProfile
  try {
    heuristicProfile = autoplayHeuristicProfile(document.episode.heuristicProfile?.id)
    if (document.episode.heuristicProfile && heuristicProfile.version !== document.episode.heuristicProfile.version) return failure(first, 'heuristic-profile', document.episode.heuristicProfile, { id: heuristicProfile.id, version: heuristicProfile.version })
  }
  catch (error) { return failure(first, 'heuristic-profile', document.episode.heuristicProfile, error instanceof Error ? error.message : String(error)) }
  let state = newRun(first.replay.seed, first.replay.biome, first.replay.areaFloor)
  let context = createAutoplayContext()
  let history: PolicyFeatureHistoryEntry[] = []
  for (const record of document.records) {
    if (state.turn !== record.turn) return failure(record, 'turn', record.turn, state.turn)
    if (record.features.informationMode !== document.episode.informationMode) return failure(record, 'information-mode', document.episode.informationMode, record.features.informationMode)
    const features = encodePolicyFeatures(state, document.episode.informationMode, history)
    if (!same(record.features, features)) return failure(record, 'features', record.features, features)
    const decision = autoplayDecision(state, document.episode.informationMode, document.episode.policy, context, heuristicProfile)
    if (!decision) return failure(record, 'candidate-legality', record.chosen, undefined)
    if (!same(record.legalCandidates, decision.candidates)) return failure(record, 'legal-candidates', record.legalCandidates, decision.candidates)
    if (record.resourceDiagnostics && !same(record.resourceDiagnostics, decision.resourceDiagnostics)) return failure(record, 'resource-diagnostics', record.resourceDiagnostics, decision.resourceDiagnostics)
    if (record.chosen.command !== decision.command || record.chosen.reason !== decision.reason) return failure(record, 'chosen-command', record.chosen, { command: decision.command, reason: decision.reason })
    const before = telemetrySnapshot(state)
    const beforeResources = { health: state.hero.health, focus: state.hero.focus, gold: state.hero.gold, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys }
    const transition = snapshotAutoplayTransition(state)
    const events = perform(state, record.chosen.command)
    observeTelemetryTurn(state, before, events, record.chosen.command)
    recordAutoplayTransitionSnapshot(context, transition, record.chosen.command, state)
    const resourceDelta = { health: state.hero.health - beforeResources.health, focus: state.hero.focus - beforeResources.focus, gold: state.hero.gold - beforeResources.gold, bombs: state.hero.bombs - beforeResources.bombs, ropes: state.hero.ropes - beforeResources.ropes, keys: state.hero.keys - beforeResources.keys }
    if (!same(record.resourceDelta, resourceDelta)) return failure(record, 'resource-delta', record.resourceDelta, resourceDelta)
    const outcome = { events: events.map(event => event.type), nextFingerprint: autoplayTraceFingerprint(state), status: state.status }
    if (!same(record.outcome, outcome)) return failure(record, 'state-outcome', record.outcome, outcome)
    history = appendPolicyFeatureHistory(history, { turn: record.turn, command: record.chosen.command, reason: record.chosen.reason, events: outcome.events, resourceDelta })
  }
  if (state.turn !== document.terminal.turns) return failure(document.records.at(-1), 'terminal-turn', document.terminal.turns, state.turn)
  const finalFingerprint = autoplayTraceFingerprint(state)
  if (finalFingerprint !== document.terminal.finalFingerprint) return failure(document.records.at(-1), 'terminal-fingerprint', document.terminal.finalFingerprint, finalFingerprint)
  if (document.terminal.outcome === 'dead' && state.status !== 'dead') return failure(document.records.at(-1), 'terminal-status', 'dead', state.status)
  if (document.terminal.outcome === 'turn-limit' && state.turn !== document.episode.turnBudget) return failure(document.records.at(-1), 'terminal-status', `turn ${document.episode.turnBudget}`, state.turn)
  return { valid: true, terminal: document.terminal }
}
