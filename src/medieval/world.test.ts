import { describe, expect, it } from 'vitest'
import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION } from './content-safety'
import { addChronicleToIndex, addWorldToIndex, emptyWorldIndex, removeWorldFromIndex } from './storage'
import { generationRetryPlan } from './generation-config'
import { InvalidWorldGenerationConfigurationError, chooseInitialCourier, createFoundationWorld, finalizeWorldAsChronicle, foundationWorldContentSatisfiesSafetyPolicy, recreateFoundationWorld } from './world'

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

  it('normalizes selected generation settings and preserves their resolved provenance in the manifest', () => {
    const world = createFoundationWorld({
      seed: '  river   ash  ',
      configuration: {
        preset: 'far-coast',
        advanced: { climate: 'temperate', historyYears: 350 }
      }
    })

    expect(world.manifest).toMatchObject({
      version: 3,
      seed: 'river ash',
      generatorVersion: 'foundation-1',
      selectedConfiguration: {
        preset: 'far-coast',
        advanced: { climate: 'temperate', historyYears: 350 }
      },
      resolvedConfiguration: {
        version: 1,
        preset: 'far-coast',
        regionSize: 'broad',
        climate: 'temperate',
        historyYears: 350,
        terrainRuggedness: 4,
        simulationFidelity: 'deep'
      },
      generationDiagnostics: {
        validation: { status: 'accepted', issues: [] }
      }
    })
    expect(world.manifest.generationDiagnostics.retryPlan).toEqual(generationRetryPlan('river ash', world.manifest.resolvedConfiguration))
    expect(recreateFoundationWorld(world.manifest)).toEqual(world)
  })

  it('audits generated foundation people, histories, places, events, and player-facing text under the versioned policy', () => {
    const world = createFoundationWorld({ seed: 'policy-ledger' })

    expect(foundationWorldContentSatisfiesSafetyPolicy(world)).toBe(true)
    expect(world.manifest.contentSafetyAudit).toMatchObject({
      policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      status: 'accepted',
      diagnostics: []
    })
    expect(world.manifest.contentSafetyAudit.reviewed).toEqual(expect.arrayContaining([
      { id: 'world:label', domain: 'place', classification: expect.objectContaining({ domains: ['place', 'player-facing-text'] }) },
      { id: 'vessel:jomon', domain: 'place', classification: expect.objectContaining({ domains: ['place', 'player-facing-text'] }) },
      { id: 'causal:0:world-created', domain: 'event', classification: expect.objectContaining({ domains: ['event', 'player-facing-text'] }) }
    ]))
    expect(world.manifest.contentSafetyAudit.reviewed.filter(record => record.domain === 'person')).toHaveLength(world.crew.length)
    expect(world.manifest.contentSafetyAudit.reviewed.filter(record => record.domain === 'history')).toHaveLength(world.crew.length)
    expect(recreateFoundationWorld(world.manifest).manifest.contentSafetyAudit).toEqual(world.manifest.contentSafetyAudit)

    const forgedManifest = structuredClone(world.manifest)
    const auditedHistory = forgedManifest.contentSafetyAudit.reviewed.find(record => record.domain === 'history')
    if (!auditedHistory) throw new Error('foundation audit must include a generated history')
    auditedHistory.classification.tags = ['commerce']
    expect(() => recreateFoundationWorld(forgedManifest)).toThrow('does not reproduce')
  })

  it('uses resolved generation settings in the foundation world identity and household stream', () => {
    const defaultConfiguration = createFoundationWorld({ seed: 'same-bank' })
    const alteredConfiguration = createFoundationWorld({ seed: 'same-bank', configuration: { preset: 'far-coast' } })

    expect(alteredConfiguration.id).not.toBe(defaultConfiguration.id)
    expect(alteredConfiguration.crew).not.toEqual(defaultConfiguration.crew)
  })

  it('rejects invalid generation settings before it creates a world or manifest', () => {
    expect(() => createFoundationWorld({
      seed: 'broken-bank',
      configuration: {
        preset: 'far-coast',
        advanced: { historyYears: 175 } as never
      }
    })).toThrow(InvalidWorldGenerationConfigurationError)
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
