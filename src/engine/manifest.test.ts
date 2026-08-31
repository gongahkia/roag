import { describe, expect, it } from 'vitest'
import { createGalaxy } from './galaxy'
import { appendGeneralManifest, GENERAL_MANIFEST_MAX_ENTRIES, normalizeGeneralManifest } from './manifest'
import { newHero } from './run'

describe('General Manifest', () => {
  it('uses stable sequence IDs, bounds history, and deduplicates legacy records', () => {
    const galaxy = createGalaxy(811, newHero(), 0)
    for (let index = 0; index < GENERAL_MANIFEST_MAX_ENTRIES + 20; index++) appendGeneralManifest(galaxy, { kind: 'packageInspected', detail: `inspection ${index}`, source: 'custody', contractId: `contract-${index}` })
    expect(galaxy.generalManifest.entries).toHaveLength(GENERAL_MANIFEST_MAX_ENTRIES)
    expect(galaxy.generalManifest.entries[0]).toMatchObject({ id: `manifest:${galaxy.seed}:21`, sequence: 21, routeReckoning: 0, source: 'custody' })
    expect(galaxy.generalManifest.nextSequence).toBe(GENERAL_MANIFEST_MAX_ENTRIES + 21)

    const duplicate = structuredClone(galaxy)
    duplicate.generalManifest.entries.push(structuredClone(duplicate.generalManifest.entries.at(-1)!))
    normalizeGeneralManifest(duplicate)
    expect(duplicate.generalManifest.entries).toHaveLength(GENERAL_MANIFEST_MAX_ENTRIES)
    expect(new Set(duplicate.generalManifest.entries.map(entry => entry.id)).size).toBe(GENERAL_MANIFEST_MAX_ENTRIES)
  })
})
