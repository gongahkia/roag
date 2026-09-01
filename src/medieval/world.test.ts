import { describe, expect, it } from 'vitest'
import { addChronicleToIndex, addWorldToIndex, emptyWorldIndex, removeWorldFromIndex } from './storage'
import { chooseInitialCourier, createFoundationWorld, finalizeWorldAsChronicle } from './world'

describe('medieval foundation worlds', () => {
  it('recreates the same world and generated household from its manifest inputs', () => {
    const first = createFoundationWorld({ seed: 'river-ash-17' })
    const second = createFoundationWorld({ seed: 'river-ash-17' })

    expect(second).toEqual(first)
    expect(first.crew).toHaveLength(6)
    expect(new Set(first.crew.map(member => member.id)).size).toBe(first.crew.length)
    expect(first.manifest.initialCourierId).toBeUndefined()
    expect(first.jomon).toMatchObject({ id: 'vessel:jomon', name: 'Jomon', deckPartitions: expect.arrayContaining(['tavern', 'chart-table', 'cargo-hold', 'gangplank']) })
  })

  it('makes a generated eligible crew member the selected initial courier without rerolling the household', () => {
    const world = createFoundationWorld({ seed: 'reed-sky-22' })
    const chosen = world.crew.find(member => member.eligible)
    expect(chosen).toBeDefined()

    const selected = chooseInitialCourier(world, chosen!.id)

    expect(selected.manifest.initialCourierId).toBe(chosen!.id)
    expect(selected.crew).toEqual(world.crew)
    expect(() => chooseInitialCourier(world, 'crew:not-present')).toThrow('eligible')
  })

  it('changes deterministically when the seed changes', () => {
    const first = createFoundationWorld({ seed: 'river-ash-17' })
    const second = createFoundationWorld({ seed: 'salt-fog-17' })

    expect(second.id).not.toBe(first.id)
    expect(second.crew).not.toEqual(first.crew)
  })

  it('turns a terminal world into a read-only chronicle without losing its causal record', () => {
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'last-lantern' }), 'crew:0')
    const chronicle = finalizeWorldAsChronicle(selected, 'crew-extinction')

    expect(chronicle.status).toBe('finalized')
    expect(chronicle.reason).toBe('crew-extinction')
    expect(chronicle.world.manifest.initialCourierId).toBe('crew:0')
    expect(chronicle.world.crew).toEqual(selected.crew)
  })
})

describe('medieval world index', () => {
  it('keeps active worlds and finalized chronicles in separate local index collections', () => {
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'many-ledgers' }), 'crew:0')
    const chronicle = finalizeWorldAsChronicle(world, 'jomon-loss')

    const active = addWorldToIndex(emptyWorldIndex(), world)
    const finalized = addChronicleToIndex(removeWorldFromIndex(active, world.id), chronicle)

    expect(active.activeWorlds).toEqual([{ id: world.id, label: world.manifest.label, initialCourierId: 'crew:0' }])
    expect(active.chronicles).toEqual([])
    expect(finalized.activeWorlds).toEqual([])
    expect(finalized.chronicles).toEqual([{ id: chronicle.id, label: chronicle.world.manifest.label, reason: 'jomon-loss' }])
  })
})
