import type { PolicyProfile } from './autoplay-policy'
import type { PolicyFeatureVector } from './autoplay-features'
import { autoplayHeuristicProfile, autoplayHeuristicProfileRef, type AutoplayHeuristicProfile, type AutoplayHeuristicProfileRef } from './autoplay-heuristics'
import { MAP_WIDTH, type AutoplayCandidate, type AutoplayMode, type AutoplayReplayMetadata, type AutoplayResourceAssessment, type AutoplayToolAssessment, type RunState, type TileKind } from './types'

export const AUTOPLAY_TRACE_VERSION = 1 as const
export interface AutoplayTraceEpisode { version: typeof AUTOPLAY_TRACE_VERSION; seed: number; policy: PolicyProfile['policy']; informationMode: PolicyProfile['informationMode']; policyVersion: PolicyProfile['version']; objectiveVersion: PolicyProfile['objectiveVersion']; heuristicProfile?: AutoplayHeuristicProfileRef; turnBudget: number }
export interface AutoplayTraceObservation { hero: { x: number; y: number; health: number; maxHealth: number; focus: number; gold: number; bombs: number; ropes: number; keys: number; inventory: string[]; cooldowns: Record<string, number> }; objective: { id: string; kind: string; status: string }; modal?: string; tiles: Array<{ x: number; y: number; kind: TileKind; explored: boolean }>; actors: Array<{ id: string; role: string; hostile: boolean; x: number; y: number; health: number }>; items: Array<{ id: string; x: number; y: number; count: number }> }
export interface AutoplayTraceResourceDelta { health: number; focus: number; gold: number; bombs: number; ropes: number; keys: number }
export interface AutoplayTraceRecord { version: typeof AUTOPLAY_TRACE_VERSION; sequence: number; episode: AutoplayTraceEpisode; turn: number; replay: AutoplayReplayMetadata; observation: AutoplayTraceObservation; features: PolicyFeatureVector; legalCandidates: AutoplayCandidate[]; resourceDiagnostics?: AutoplayResourceAssessment[]; toolDiagnostics?: AutoplayToolAssessment[]; chosen: { command: string; reason: string }; outcome: { events: string[]; nextFingerprint: string; status: RunState['status'] }; resourceDelta: AutoplayTraceResourceDelta; previousHash: string | null; hash: string }
export interface AutoplayTraceTerminal { outcome: 'complete' | 'dead' | 'stalled' | 'turn-limit' | 'error'; reason: string; turns: number; campaignComplete: boolean; finalFingerprint: string }
export interface AutoplayTraceDocument { version: typeof AUTOPLAY_TRACE_VERSION; episode: AutoplayTraceEpisode; records: AutoplayTraceRecord[]; terminal: AutoplayTraceTerminal; hash: string }

const hash = (value: unknown): string => {
  const input = JSON.stringify(value)
  let valueHash = 0x811c9dc5
  for (let index = 0; index < input.length; index++) valueHash = Math.imul(valueHash ^ input.charCodeAt(index), 0x01000193)
  return `fnv1a32:${(valueHash >>> 0).toString(16).padStart(8, '0')}`
}

const traceRecordValue = (record: Omit<AutoplayTraceRecord, 'hash'>): Omit<AutoplayTraceRecord, 'hash'> => ({ version: record.version, sequence: record.sequence, episode: record.episode, turn: record.turn, replay: record.replay, observation: record.observation, features: record.features, legalCandidates: record.legalCandidates, ...(record.resourceDiagnostics?.length ? { resourceDiagnostics: record.resourceDiagnostics } : {}), ...(record.toolDiagnostics?.length ? { toolDiagnostics: record.toolDiagnostics } : {}), chosen: record.chosen, outcome: record.outcome, resourceDelta: record.resourceDelta, previousHash: record.previousHash })
const traceDocumentValue = (document: Omit<AutoplayTraceDocument, 'hash'>): Omit<AutoplayTraceDocument, 'hash'> => ({ version: document.version, episode: document.episode, records: document.records, terminal: document.terminal })
const visibleAt = (state: RunState, x: number, y: number): boolean => state.floor.tiles[y * MAP_WIDTH + x]?.visible === true

export const createAutoplayTraceEpisode = (profile: PolicyProfile, seed: number, turnBudget: number, heuristicProfile?: AutoplayHeuristicProfile): AutoplayTraceEpisode => ({ version: AUTOPLAY_TRACE_VERSION, seed, policy: profile.policy, informationMode: profile.informationMode, policyVersion: profile.version, objectiveVersion: profile.objectiveVersion, ...(heuristicProfile ? { heuristicProfile: autoplayHeuristicProfileRef(heuristicProfile) } : {}), turnBudget })

export const observeAutoplayTrace = (state: RunState, mode: Exclude<AutoplayMode, 'off'>): AutoplayTraceObservation => {
  const visible = (x: number, y: number): boolean => mode === 'omniscient' || visibleAt(state, x, y)
  return {
    hero: { x: state.hero.x, y: state.hero.y, health: state.hero.health, maxHealth: state.hero.maxHealth, focus: state.hero.focus, gold: state.hero.gold, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys, inventory: [...state.hero.inventory], cooldowns: { ...(state.hero.cooldowns ?? {}) } },
    objective: { id: state.floor.objective.id, kind: state.floor.objective.kind, status: state.floor.objective.status },
    ...(state.modal ? { modal: state.modal.kind } : {}),
    tiles: state.floor.tiles.flatMap((tile, index) => visible(index % MAP_WIDTH, Math.floor(index / MAP_WIDTH)) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH), kind: tile.kind, explored: tile.explored }] : []),
    actors: state.floor.actors.filter(actor => actor.health > 0 && visible(actor.x, actor.y)).map(actor => ({ id: actor.id, role: actor.role, hostile: actor.hostile, x: actor.x, y: actor.y, health: actor.health })).sort((left, right) => left.id.localeCompare(right.id)),
    items: state.floor.items.filter(item => visible(item.x, item.y)).map(item => ({ id: item.id, x: item.x, y: item.y, count: item.count })).sort((left, right) => left.y - right.y || left.x - right.x || left.id.localeCompare(right.id))
  }
}

export const createAutoplayTraceRecord = (record: Omit<AutoplayTraceRecord, 'version' | 'hash'>): AutoplayTraceRecord => {
  const value = { version: AUTOPLAY_TRACE_VERSION, ...record }
  return { ...value, hash: hash(traceRecordValue(value)) }
}

export const createAutoplayTraceDocument = (episode: AutoplayTraceEpisode, records: readonly AutoplayTraceRecord[], terminal: AutoplayTraceTerminal): AutoplayTraceDocument => {
  const value = { version: AUTOPLAY_TRACE_VERSION, episode: structuredClone(episode), records: records.map(record => structuredClone(record)), terminal: structuredClone(terminal) }
  return { ...value, hash: hash(traceDocumentValue(value)) }
}

export const assertAutoplayTraceDocument = (document: AutoplayTraceDocument): void => {
  if (document.version !== AUTOPLAY_TRACE_VERSION || document.episode.version !== AUTOPLAY_TRACE_VERSION) throw new Error('unsupported autoplay trace version')
  if (document.episode.heuristicProfile) {
    const profile = autoplayHeuristicProfile(document.episode.heuristicProfile.id)
    if (profile.version !== document.episode.heuristicProfile.version) throw new Error('unsupported autoplay trace heuristic profile version')
  }
  let previousHash: string | null = null
  for (const [sequence, record] of document.records.entries()) {
    if (record.version !== AUTOPLAY_TRACE_VERSION || record.sequence !== sequence || record.previousHash !== previousHash) throw new Error(`invalid autoplay trace record ${sequence}`)
    if (record.hash !== hash(traceRecordValue(record))) throw new Error(`autoplay trace record hash mismatch at ${sequence}`)
    if (JSON.stringify(record.episode) !== JSON.stringify(document.episode)) throw new Error(`autoplay trace episode mismatch at ${sequence}`)
    previousHash = record.hash
  }
  if (document.hash !== hash(traceDocumentValue(document))) throw new Error('autoplay trace document hash mismatch')
}
