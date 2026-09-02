import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { createActiveWorldBackupBundle, createChronicleBackupBundle, createPersistencePreflight, createWorldRecordIndex, emptySnapshotRing, parsePersistenceBackupBundle, persistenceWorldSourceFor, serializePersistenceBackupBundle, snapshotRingAfterReplacement, validateFoundationWorldSnapshotRing, validatePersistenceBackupBundle, validateWorldRecordIndex } from './persistence-layout'
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
  }, 20_000)

  it('exports canonical offline active-world and read-only-chronicle bundles and rejects tampering', () => {
    const world = selected('backup-layout')
    const active = createActiveWorldBackupBundle(world)
    const serialized = serializePersistenceBackupBundle(active)
    expect(parsePersistenceBackupBundle(serialized)).toEqual(active)
    expect(() => parsePersistenceBackupBundle(JSON.stringify({ world: active.world, source: active.source, kind: active.kind, version: active.version }))).toThrow('noncanonical')
    const tampered = structuredClone(active)
    tampered.source.digest = 'layout:tampered'
    expect(validatePersistenceBackupBundle(tampered)).toBe(false)
    const chronicle = finalizeWorldAsChronicle(world, 'jomon-loss')
    const chronicleBundle = createChronicleBackupBundle(chronicle)
    expect(parsePersistenceBackupBundle(serializePersistenceBackupBundle(chronicleBundle))).toEqual(chronicleBundle)
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
