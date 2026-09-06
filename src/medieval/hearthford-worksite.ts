import { classifyMedievalContent, type MedievalContentSafetyClassification } from './content-safety'
import { hashSeed } from './rng'
import type { TimeBearingTemporalAction } from './temporal'

/**
 * One consequential shore-side problem, deliberately bounded to the existing
 * quay approach. The result belongs to the named mill lease rather than to a
 * disposable encounter or a hidden simulation.
 */
export const HEARTHFORD_WORKSITE_CONTRACT_VERSION = 1 as const
export const HEARTHFORD_MILL_LEASE_ID = 'institution:hearthford-mill-lease' as const
export const HEARTHFORD_WORKSITE_ACTION_MINUTES = {
  'ironwork-fitted': 20,
  'lease-credit': 35
} as const

export const HEARTHFORD_WORKSITE_INCIDENTS = ['sluice-jam', 'silted-intake'] as const
export type HearthfordWorksiteIncident = typeof HEARTHFORD_WORKSITE_INCIDENTS[number]
export const HEARTHFORD_WORKSITE_RESOLUTIONS = ['ironwork-fitted', 'lease-credit'] as const
export type HearthfordWorksiteResolutionKind = typeof HEARTHFORD_WORKSITE_RESOLUTIONS[number]

export interface HearthfordWorksiteInstitution {
  id: typeof HEARTHFORD_MILL_LEASE_ID
  status: 'available' | 'relieved' | 'owed'
}

export interface HearthfordWorksiteUnresolved {
  version: typeof HEARTHFORD_WORKSITE_CONTRACT_VERSION
  incident: HearthfordWorksiteIncident
  institution: HearthfordWorksiteInstitution
}

export interface HearthfordWorksiteResolved extends HearthfordWorksiteUnresolved {
  institution: Omit<HearthfordWorksiteInstitution, 'status'> & { status: 'relieved' | 'owed' }
  resolution: {
    kind: HearthfordWorksiteResolutionKind
    recordedAtWorldTime: number
    causalSequence: number
  }
}

export type HearthfordWorksiteState = HearthfordWorksiteUnresolved | HearthfordWorksiteResolved

export type HearthfordWorksiteDiagnostic =
  | 'hearthford-worksite.malformed-state'
  | 'hearthford-worksite.invalid-version'
  | 'hearthford-worksite.invalid-incident'
  | 'hearthford-worksite.seed-mismatch'
  | 'hearthford-worksite.invalid-institution'
  | 'hearthford-worksite.invalid-resolution'

export class HearthfordWorksiteContractError extends Error {
  constructor(readonly diagnostics: readonly HearthfordWorksiteDiagnostic[]) {
    super(`Hearthford worksite rejected: ${diagnostics.join(', ')}`)
    this.name = 'HearthfordWorksiteContractError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const keys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort()
  const canonical = [...expected].sort()
  return actual.length === canonical.length && actual.every((key, index) => key === canonical[index])
}
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const sequence = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 1

export const hearthfordWorksiteIncidentForSeed = (seed: string): HearthfordWorksiteIncident =>
  HEARTHFORD_WORKSITE_INCIDENTS[hashSeed(`hearthford-worksite-v1:${seed}`) % HEARTHFORD_WORKSITE_INCIDENTS.length]!

/** The incident changes a real, walkable quay cell without adding a second map. */
export const hearthfordWorksiteAnchorFor = (state: Pick<HearthfordWorksiteState, 'incident'>): { column: number; row: number } =>
  state.incident === 'sluice-jam' ? { column: 2, row: 4 } : { column: 1, row: 6 }

export const hearthfordWorksiteSafety = (): MedievalContentSafetyClassification => classifyMedievalContent(
  'event',
  ['adult-labour', 'civil-life', 'commerce', 'craft', 'settlement'],
  'adults-only',
  ['player-facing-text']
)

export const createHearthfordWorksiteState = (seed: string): HearthfordWorksiteUnresolved => ({
  version: HEARTHFORD_WORKSITE_CONTRACT_VERSION,
  incident: hearthfordWorksiteIncidentForSeed(seed),
  institution: { id: HEARTHFORD_MILL_LEASE_ID, status: 'available' }
})

export const validateHearthfordWorksiteState = (seed: string, value: unknown, worldTime?: number, causalSequence?: number): readonly HearthfordWorksiteDiagnostic[] => {
  if (!record(value) || !(keys(value, ['version', 'incident', 'institution']) || keys(value, ['version', 'incident', 'institution', 'resolution']))) return ['hearthford-worksite.malformed-state']
  const diagnostics: HearthfordWorksiteDiagnostic[] = []
  if (value.version !== HEARTHFORD_WORKSITE_CONTRACT_VERSION) diagnostics.push('hearthford-worksite.invalid-version')
  if (!HEARTHFORD_WORKSITE_INCIDENTS.includes(value.incident as HearthfordWorksiteIncident)) diagnostics.push('hearthford-worksite.invalid-incident')
  else if (value.incident !== hearthfordWorksiteIncidentForSeed(seed)) diagnostics.push('hearthford-worksite.seed-mismatch')
  if (!record(value.institution) || !keys(value.institution, ['id', 'status']) || value.institution.id !== HEARTHFORD_MILL_LEASE_ID || !['available', 'relieved', 'owed'].includes(String(value.institution.status))) diagnostics.push('hearthford-worksite.invalid-institution')
  const resolved = Object.hasOwn(value, 'resolution')
  if (!resolved) {
    if (record(value.institution) && value.institution.status !== 'available') diagnostics.push('hearthford-worksite.invalid-resolution')
  } else if (!record(value.resolution) || !keys(value.resolution, ['kind', 'recordedAtWorldTime', 'causalSequence'])
    || !HEARTHFORD_WORKSITE_RESOLUTIONS.includes(value.resolution.kind as HearthfordWorksiteResolutionKind)
    || !safeInteger(value.resolution.recordedAtWorldTime) || !sequence(value.resolution.causalSequence)
    || (worldTime !== undefined && value.resolution.recordedAtWorldTime > worldTime)
    || (causalSequence !== undefined && value.resolution.causalSequence > causalSequence)
    || !record(value.institution)
    || (value.resolution.kind === 'ironwork-fitted' && value.institution.status !== 'relieved')
    || (value.resolution.kind === 'lease-credit' && value.institution.status !== 'owed')) diagnostics.push('hearthford-worksite.invalid-resolution')
  return [...new Set(diagnostics)].sort()
}

const checked = (state: HearthfordWorksiteState): HearthfordWorksiteState => {
  const resolution = 'resolution' in state ? state.resolution : undefined
  if (!HEARTHFORD_WORKSITE_INCIDENTS.includes(state.incident)
    || state.version !== HEARTHFORD_WORKSITE_CONTRACT_VERSION
    || state.institution.id !== HEARTHFORD_MILL_LEASE_ID
    || !['available', 'relieved', 'owed'].includes(state.institution.status)
    || (resolution === undefined && state.institution.status !== 'available')
    || (resolution !== undefined && (!HEARTHFORD_WORKSITE_RESOLUTIONS.includes(resolution.kind)
      || !safeInteger(resolution.recordedAtWorldTime)
      || !sequence(resolution.causalSequence)
      || (resolution.kind === 'ironwork-fitted' && state.institution.status !== 'relieved')
      || (resolution.kind === 'lease-credit' && state.institution.status !== 'owed')))) throw new HearthfordWorksiteContractError(['hearthford-worksite.malformed-state'])
  return state
}

export const resolveHearthfordWorksite = (
  state: HearthfordWorksiteState,
  kind: HearthfordWorksiteResolutionKind,
  recordedAtWorldTime: number,
  causalSequence: number
): HearthfordWorksiteResolved => {
  checked(state)
  if (Object.hasOwn(state, 'resolution') || !HEARTHFORD_WORKSITE_RESOLUTIONS.includes(kind) || !safeInteger(recordedAtWorldTime) || !sequence(causalSequence)) throw new HearthfordWorksiteContractError(['hearthford-worksite.invalid-resolution'])
  return {
    version: HEARTHFORD_WORKSITE_CONTRACT_VERSION,
    incident: state.incident,
    institution: { id: HEARTHFORD_MILL_LEASE_ID, status: kind === 'ironwork-fitted' ? 'relieved' : 'owed' },
    resolution: { kind, recordedAtWorldTime, causalSequence }
  }
}

const actionIdFor = (kind: HearthfordWorksiteResolutionKind, causalSequence: number): string => `hearthford-worksite:${kind}:${causalSequence}`

export const createHearthfordWorksiteTemporalAction = (kind: HearthfordWorksiteResolutionKind, causalSequence: number): TimeBearingTemporalAction => ({
  id: actionIdFor(kind, causalSequence),
  kind: 'work',
  durationMinutes: HEARTHFORD_WORKSITE_ACTION_MINUTES[kind],
  contentSafety: hearthfordWorksiteSafety()
})

/** Recognizes only the two canonical time-bearing worksite resolutions. */
export const hearthfordWorksiteResolutionFromAction = (action: unknown): { kind: HearthfordWorksiteResolutionKind; causalSequence: number } | undefined => {
  if (!record(action) || typeof action.id !== 'string' || action.kind !== 'work' || !safeInteger(action.durationMinutes)) return undefined
  const match = /^hearthford-worksite:(ironwork-fitted|lease-credit):([1-9][0-9]*)$/u.exec(action.id)
  if (!match) return undefined
  const kind = match[1] as HearthfordWorksiteResolutionKind
  const causalSequence = Number(match[2])
  return action.durationMinutes === HEARTHFORD_WORKSITE_ACTION_MINUTES[kind] ? { kind, causalSequence } : undefined
}
