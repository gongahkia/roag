import type { GalaxyState, GeneralManifestEvent, GeneralManifestEventKind, GeneralManifestSource } from '../types'
import { sectorDayFromRouteReckoning } from './route-reckoning'

export const GENERAL_MANIFEST_MAX_ENTRIES = 160

export interface ManifestReferences {
  contractId?: string
  packageId?: string
  courierId?: string
  routeCacheId?: string
  siteId?: string
  routeId?: string
  transitId?: string
}

export interface ManifestEntryInput extends ManifestReferences {
  kind: GeneralManifestEventKind
  detail: string
  source: GeneralManifestSource
  payload?: Record<string, string | number | boolean | null>
}

const affectedEntityIds = (references: ManifestReferences): string[] => [references.contractId, references.packageId, references.courierId, references.routeCacheId, references.siteId, references.routeId, references.transitId].filter((value): value is string => Boolean(value))

export const appendGeneralManifest = (galaxy: GalaxyState, input: ManifestEntryInput): number => {
  const sequence = galaxy.generalManifest.nextSequence
  const id = `manifest:${galaxy.seed}:${sequence}`
  const existing = galaxy.generalManifest.entries.find(entry => entry.id === id)
  if (existing) return existing.sequence
  const entry: GeneralManifestEvent = {
    version: 2,
    id,
    sequence,
    kind: input.kind,
    routeReckoning: galaxy.routeReckoning,
    sectorDay: sectorDayFromRouteReckoning(galaxy.routeReckoning),
    affectedEntityIds: affectedEntityIds(input),
    source: input.source,
    payload: input.payload ?? {},
    ...(input.contractId ? { contractId: input.contractId } : {}),
    ...(input.packageId ? { packageId: input.packageId } : {}),
    ...(input.courierId ? { courierId: input.courierId } : {}),
    ...(input.routeCacheId ? { routeCacheId: input.routeCacheId } : {}),
    ...(input.siteId ? { siteId: input.siteId } : {}),
    ...(input.routeId ? { routeId: input.routeId } : {}),
    ...(input.transitId ? { transitId: input.transitId } : {}),
    detail: input.detail
  }
  galaxy.generalManifest.entries.push(entry)
  galaxy.generalManifest.entries = galaxy.generalManifest.entries.slice(-GENERAL_MANIFEST_MAX_ENTRIES)
  galaxy.generalManifest.nextSequence = sequence + 1
  return sequence
}

export const normalizeGeneralManifest = (galaxy: GalaxyState): void => {
  const legacyEntries = Array.isArray(galaxy.generalManifest?.entries) ? galaxy.generalManifest.entries : []
  const byId = new Map<string, GeneralManifestEvent>()
  for (const [index, raw] of legacyEntries.entries()) {
    const sequence = Number.isInteger(raw.sequence) ? raw.sequence : index
    const existingReckoning = raw.routeReckoning
    const routeReckoning = typeof existingReckoning === 'number' && Number.isInteger(existingReckoning) ? Math.max(0, existingReckoning) : Math.round((raw.sectorDay ?? 0) * 1_440)
    const references: ManifestReferences = { contractId: raw.contractId, packageId: raw.packageId, courierId: raw.courierId, routeCacheId: raw.routeCacheId, siteId: raw.siteId, routeId: raw.routeId, transitId: raw.transitId }
    const entry: GeneralManifestEvent = {
      ...raw,
      version: 2,
      id: typeof raw.id === 'string' ? raw.id : `manifest:${galaxy.seed}:${sequence}`,
      sequence,
      routeReckoning,
      sectorDay: sectorDayFromRouteReckoning(routeReckoning),
      affectedEntityIds: Array.isArray(raw.affectedEntityIds) ? [...new Set(raw.affectedEntityIds.filter((value): value is string => typeof value === 'string'))] : affectedEntityIds(references),
      source: raw.source ?? 'custody',
      payload: raw.payload ?? {}
    }
    byId.set(entry.id, entry)
  }
  const entries = [...byId.values()].sort((left, right) => left.sequence - right.sequence).slice(-GENERAL_MANIFEST_MAX_ENTRIES)
  const nextSequence = Math.max(Number.isInteger(galaxy.generalManifest?.nextSequence) ? galaxy.generalManifest.nextSequence : 0, ...entries.map(entry => entry.sequence + 1), 0)
  galaxy.generalManifest = { version: 2, nextSequence, entries }
}
