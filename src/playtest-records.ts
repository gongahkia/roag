import { BIOME_POOL } from './engine/campaign'
import type { Biome } from './types'

export type PlaytestFailureKind = 'opacity' | 'unfairness' | 'other'

export interface PlaytestFailure {
  kind: PlaytestFailureKind
  code: string
  detail: string
  issue?: number
}

export interface PlaytestSession {
  id: string
  date: string
  participantId: string
  sessionKind: 'human'
  seed: number
  routeChosen: string
  landmarkRecall: string
  threatComprehension: string
  terrainUse: string
  boonRelevance: string
  encounterRead: string
  confusion: string
  memorableMoments: string
  funRating: number
  funReason: string
  thesisWithoutName: string
  thesisAssessment: 'clear' | 'partial' | 'missed'
  failureModes: PlaytestFailure[]
}

export interface BiomePlaytestRecord {
  biome: Biome
  targetThesis: string
  sourceGrounded: { note: string; url: string }
  fantasyInvention: string
  sessions: PlaytestSession[]
}

export interface PlaytestRecords { version: 1; records: BiomePlaytestRecord[] }
export interface PlaytestBiomeSummary { sessions: number; complete: boolean; meanFunRating: number | null; thesisAssessment: { id: string; count: number }[]; failureModes: { id: string; count: number }[]; linkedFixIssues: number[] }
export interface PlaytestSummary { valid: boolean; errors: string[]; biomes: Record<Biome, PlaytestBiomeSummary>; complete: boolean }

const textFields: readonly (keyof Omit<PlaytestSession, 'seed' | 'funRating' | 'failureModes' | 'sessionKind' | 'thesisAssessment'>)[] = ['id', 'date', 'participantId', 'routeChosen', 'landmarkRecall', 'threatComprehension', 'terrainUse', 'boonRelevance', 'encounterRead', 'confusion', 'memorableMoments', 'funReason', 'thesisWithoutName']
const isBiome = (value: unknown): value is Biome => typeof value === 'string' && BIOME_POOL.includes(value as Biome)
const asObject = (value: unknown): Record<string, unknown> | undefined => value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : undefined
const message = (biome: string, value: string): string => `${biome}: ${value}`
const modeId = (failure: PlaytestFailure): string => `${failure.kind}:${failure.code.trim().toLocaleLowerCase()}`

export const summarizePlaytestRecords = (value: unknown, requireComplete = false): PlaytestSummary => {
  const errors: string[] = []
  const document = asObject(value)
  if (document?.version !== 1) errors.push('records: version must be 1')
  if (!Array.isArray(document?.records)) errors.push('records: records must be an array')
  const records = Array.isArray(document?.records) ? document.records : []
  const seen = new Set<Biome>()
  const rawByBiome = new Map<Biome, Record<string, unknown>>()
  for (const raw of records) {
    const record = asObject(raw)
    if (!isBiome(record?.biome)) { errors.push('record: invalid biome'); continue }
    if (seen.has(record.biome)) errors.push(message(record.biome, 'duplicate record'))
    seen.add(record.biome)
    rawByBiome.set(record.biome, record)
  }
  for (const biome of BIOME_POOL) if (!seen.has(biome)) errors.push(message(biome, 'missing record'))
  const biomes = Object.fromEntries(BIOME_POOL.map(biome => {
    const record = rawByBiome.get(biome)
    const failures = new Map<string, { count: number; issues: Set<number>; participants: Set<string> }>()
    if (!record) return [biome, { sessions: 0, complete: false, meanFunRating: null, thesisAssessment: [], failureModes: [], linkedFixIssues: [] } satisfies PlaytestBiomeSummary]
    for (const field of ['targetThesis', 'fantasyInvention'] as const) if (typeof record[field] !== 'string' || !record[field].trim()) errors.push(message(biome, `${field} is required`))
    const source = asObject(record.sourceGrounded)
    if (!source || typeof source.note !== 'string' || !source.note.trim() || typeof source.url !== 'string' || !source.url.startsWith('https://')) errors.push(message(biome, 'sourceGrounded note and https url are required'))
    if (!Array.isArray(record.sessions)) {
      errors.push(message(biome, 'sessions must be an array'))
      return [biome, { sessions: 0, complete: false, meanFunRating: null, thesisAssessment: [], failureModes: [], linkedFixIssues: [] } satisfies PlaytestBiomeSummary]
    }
    const thesisAssessment: Record<PlaytestSession['thesisAssessment'], number> = { clear: 0, partial: 0, missed: 0 }
    let funTotal = 0
    let funCount = 0
    for (const rawSession of record.sessions) {
      const session = asObject(rawSession)
      if (!session) { errors.push(message(biome, 'session must be an object')); continue }
      for (const field of textFields) if (typeof session[field] !== 'string' || !session[field].trim()) errors.push(message(biome, `session ${String(field)} is required`))
      if (session.sessionKind !== 'human') errors.push(message(biome, 'sessionKind must be human'))
      if (!Number.isInteger(session.seed) || (session.seed as number) < 0) errors.push(message(biome, 'session seed must be a non-negative integer'))
      if (!Number.isInteger(session.funRating) || (session.funRating as number) < 1 || (session.funRating as number) > 5) errors.push(message(biome, 'session funRating must be 1-5'))
      else { funTotal += session.funRating as number; funCount++ }
      if (!['clear', 'partial', 'missed'].includes(session.thesisAssessment as string)) errors.push(message(biome, 'session thesisAssessment must be clear, partial, or missed'))
      else thesisAssessment[session.thesisAssessment as PlaytestSession['thesisAssessment']]++
      if (!Array.isArray(session.failureModes)) { errors.push(message(biome, 'session failureModes must be an array')); continue }
      for (const rawFailure of session.failureModes) {
        const failure = asObject(rawFailure)
        if (!failure || !['opacity', 'unfairness', 'other'].includes(failure.kind as string) || typeof failure.code !== 'string' || !failure.code.trim() || typeof failure.detail !== 'string' || !failure.detail.trim() || (failure.issue !== undefined && (!Number.isInteger(failure.issue) || (failure.issue as number) < 1))) { errors.push(message(biome, 'invalid failure mode')); continue }
        const checked: PlaytestFailure = { kind: failure.kind as PlaytestFailureKind, code: failure.code as string, detail: failure.detail as string, ...(typeof failure.issue === 'number' ? { issue: failure.issue } : {}) }
        const id = modeId(checked)
        const current = failures.get(id) ?? { count: 0, issues: new Set<number>(), participants: new Set<string>() }
        current.count++
        if (typeof checked.issue === 'number') current.issues.add(checked.issue)
        if (typeof session.participantId === 'string') current.participants.add(session.participantId)
        failures.set(id, current)
      }
    }
    if (requireComplete && !record.sessions.length) errors.push(message(biome, 'no human session'))
    for (const [id, failure] of failures) if ((id.startsWith('opacity:') || id.startsWith('unfairness:')) && failure.participants.size >= 2 && !failure.issues.size) errors.push(message(biome, `recurrent ${id} needs a linked GitHub issue`))
    const linkedFixIssues = [...new Set([...failures.values()].flatMap(failure => [...failure.issues]))].sort((left, right) => left - right)
    return [biome, {
      sessions: record.sessions.length,
      complete: record.sessions.length > 0,
      meanFunRating: funCount ? Number((funTotal / funCount).toFixed(2)) : null,
      thesisAssessment: Object.entries(thesisAssessment).filter(([, count]) => count).map(([id, count]) => ({ id, count })),
      failureModes: [...failures.entries()].map(([id, failure]) => ({ id, count: failure.count })).sort((left, right) => right.count - left.count || left.id.localeCompare(right.id)),
      linkedFixIssues
    } satisfies PlaytestBiomeSummary]
  })) as Record<Biome, PlaytestBiomeSummary>
  errors.sort()
  return { valid: errors.length === 0, errors, biomes, complete: BIOME_POOL.every(biome => biomes[biome].complete) }
}
