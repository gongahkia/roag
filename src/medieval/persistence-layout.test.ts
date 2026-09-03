import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { createActiveWorldBackupBundle, createChronicleBackupBundle, createPersistencePreflight, createWorldRecordIndex, emptySnapshotRing, parsePersistenceBackupBundle, persistenceDigestFor, persistenceWorldSourceFor, serializePersistenceBackupBundle, snapshotRingAfterReplacement, validateFoundationWorldSnapshotRing, validatePersistenceBackupBundle, validateWorldRecordIndex } from './persistence-layout'
import { canonicalSerializedByteLength } from './performance-budget'
import { createCausalHistoryState } from './causal-history'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, finalizeWorldAsChronicle } from './world'

const selected = (seed = 'layout-contract'): ReturnType<typeof chooseInitialCourier> => chooseInitialCourier(createFoundationWorld({ seed }), 'crew:0')
const wait = (id: string, minutes: number) => ({ id, kind: 'wait' as const, durationMinutes: minutes, contentSafety: classifyMedievalContent('event', ['adult-labour'], 'adults-only', ['data']) })

describe('compact medieval persistence layout', () => {
  it('derives canonical, digest-bound locators from whole authoritative worlds only', () => {
    const world = selected()
    const index = createWorldRecordIndex(world)
    expect(validateWorldRecordIndex(index, world)).toBe(true)
    expect(index.locators).toEqual(createWorldRecordIndex(world).locators)
    expect(index.locators.filter(item => item.kind === 'person').map(item => item.targetId)).toEqual(world.state.people.records.map(person => person.id))
    expect(index.locators.some(item => item.targetId.startsWith('initial:person:'))).toBe(false)
    expect(index.locators.some(item => item.container === 'causal-checkpoint')).toBe(true)
    const stale = structuredClone(index)
    stale.source.digest = 'layout:forged'
    expect(validateWorldRecordIndex(stale, world)).toBe(false)
    const unsafe = structuredClone(index)
    ;(unsafe.locators[0]!.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'permitted'
    expect(validateWorldRecordIndex(unsafe)).toBe(false)
    const duplicate = structuredClone(index)
    duplicate.locators = [...duplicate.locators, structuredClone(duplicate.locators[0]!)]
    expect(validateWorldRecordIndex(duplicate)).toBe(false)
    const malformed = structuredClone(index)
    malformed.locators[0]!.container = 'hidden-world' as never
    expect(validateWorldRecordIndex(malformed)).toBe(false)
  })

  it('rotates bounded complete-envelope snapshots deterministically without changing their source', () => {
    let world = selected('snapshot-ring')
    let ring = emptySnapshotRing(world.id)
    const retained: string[] = []
    for (let index = 0; index < 5; index++) {
      ring = snapshotRingAfterReplacement(ring, world)
      retained.push(persistenceWorldSourceFor(world).digest)
      world = advanceFoundationWorldTime(world, wait(`snapshot-wait-${index}`, 1))
    }
    expect(validateFoundationWorldSnapshotRing(ring)).toBe(true)
    expect(ring.snapshots).toHaveLength(3)
    expect(ring.snapshots.map(item => item.sequence)).toEqual([3, 4, 5])
    expect(ring.snapshots.map(item => item.source.digest)).toEqual(retained.slice(-3))
    const forged = structuredClone(ring)
    forged.snapshots = [...forged.snapshots].reverse()
    expect(validateFoundationWorldSnapshotRing(forged)).toBe(false)
  }, 30_000)

  it('exports canonical offline active-world and read-only-chronicle bundles and rejects tampering', () => {
    const world = selected('backup-layout')
    const active = createActiveWorldBackupBundle(world)
    const serialized = serializePersistenceBackupBundle(active)
    expect(parsePersistenceBackupBundle(serialized)).toEqual(active)
    expect(() => parsePersistenceBackupBundle(JSON.stringify({ world: active.world, source: active.source, kind: active.kind, version: active.version }))).toThrow('noncanonical')
    const tampered = structuredClone(active)
    tampered.source.digest = 'layout:tampered'
    expect(validatePersistenceBackupBundle(tampered)).toBe(false)
    expect(() => parsePersistenceBackupBundle('x'.repeat(1_048_577))).toThrow('backup size limit')
    const chronicle = finalizeWorldAsChronicle(world, 'jomon-loss')
    const chronicleBundle = createChronicleBackupBundle(chronicle)
    expect(parsePersistenceBackupBundle(serializePersistenceBackupBundle(chronicleBundle))).toEqual(chronicleBundle)
  }, 10_000)

  it('imports only a canonical, source-verified v13 active-world backup through the explicit navigation conversion', () => {
    const world = selected('legacy-backup-layout')
    const legacy = structuredClone(world) as unknown as Record<string, any>
    legacy.version = 13
    legacy.state.version = 11
    delete legacy.state.navigation
    const checkpointProjection = structuredClone(legacy.state.causalHistory.checkpoint.projection)
    delete checkpointProjection.navigation
    legacy.state.causalHistory = {
      ...legacy.state.causalHistory,
      checkpoint: createCausalHistoryState({ worldId: legacy.id, creationDigest: legacy.manifest.creation.digest }, checkpointProjection).checkpoint
    }
    const legacyBundle = {
      version: 1,
      kind: 'active-world',
      source: {
        worldId: legacy.id,
        revision: legacy.state.causalHistory.checkpoint.sequence + legacy.state.causalHistory.tail.length,
        digest: persistenceDigestFor('foundation-world', legacy),
        canonicalBytes: canonicalSerializedByteLength(legacy)
      },
      world: legacy
    }

    expect(parsePersistenceBackupBundle(JSON.stringify(legacyBundle))).toMatchObject({ kind: 'active-world', world: { version: 14, state: { version: 12, navigation: { coordinate: { column: 4, row: 4 } } } } })
    const corrupt = structuredClone(legacyBundle)
    corrupt.source.digest = 'layout:forged'
    expect(() => parsePersistenceBackupBundle(JSON.stringify(corrupt))).toThrow('invalid bundle')
  })

  it('reports advisory byte preflight without treating it as a quota reservation or mutating a world', () => {
    const world = selected('preflight-layout')
    const before = structuredClone(world)
    const ready = createPersistencePreflight(world, 0)
    const advisory = createPersistencePreflight(world, 16 * 1024 * 1024)
    expect(ready.status).toBe('ready')
    expect(advisory.status).toBe('advisory-warning')
    expect(advisory.reasons).toContain('advisory-write-warning')
    expect(world).toEqual(before)
  })
})
