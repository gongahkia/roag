import { describe, expect, it } from 'vitest'
import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, classifyMedievalContent } from './content-safety'
import { addChronicleToIndex, addWorldToIndex, emptyWorldIndex, removeWorldFromIndex } from './storage'
import { generationRetryPlan } from './generation-config'
import { INITIAL_WORLD_GENERATION_STAGES } from './initial-world'
import { InvalidWorldGenerationConfigurationError, advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, finalizeWorldAsChronicle, foundationWorldContentSatisfiesSafetyPolicy, recordDurableJomonGrowth, recreateFoundationWorld, serializeWorldManifest, validateFoundationWorld } from './world'
import { worldEraProjection } from './world-era'

const safety = () => classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])

describe('medieval foundation worlds', () => {
  it('recreates the same world and generated household from its manifest inputs', () => {
    const first = createFoundationWorld({ seed: 'river-ash-17' })
    const second = createFoundationWorld({ seed: 'river-ash-17' })

    expect(second).toEqual(first)
    expect(first.crew).toHaveLength(6)
    expect(new Set(first.crew.map(member => member.id)).size).toBe(first.crew.length)
    expect(first.state.courier.initialCourierId).toBeUndefined()
    expect(first.jomon).toMatchObject({ id: 'vessel:jomon', name: 'Jomon', deckPartitions: expect.arrayContaining(['tavern', 'chart-table', 'cargo-hold', 'gangplank']) })
  })

  it('makes a generated eligible crew member the selected initial courier without rerolling the household', () => {
    const world = createFoundationWorld({ seed: 'reed-sky-22' })
    const chosen = world.crew.find(member => member.eligible)
    expect(chosen).toBeDefined()

    const selected = chooseInitialCourier(world, chosen!.id)

    expect(selected.state.courier.initialCourierId).toBe(chosen!.id)
    expect(selected.crew).toEqual(world.crew)
    expect(() => chooseInitialCourier(world, 'crew:not-present')).toThrow('eligible')
  })

  it('changes deterministically when the seed changes', () => {
    const first = createFoundationWorld({ seed: 'river-ash-17' })
    const second = createFoundationWorld({ seed: 'salt-fog-17' })

    expect(second.id).not.toBe(first.id)
    expect(second.crew).not.toEqual(first.crew)
  })

  it('passes actual bounded generator progress to creation UI callers without mutating zero-time state', () => {
    const trace: string[] = []
    const world = createFoundationWorld({ seed: 'observable-creation', onGenerationProgress: progress => trace.push(`${progress.stage}:${progress.status}`) })

    expect(trace).toContain('watershed-hydrology:started')
    expect(trace).toContain('routes-trade-history:completed')
    expect(trace.at(-1)).toBe('validation:accepted')
    expect(trace.filter(item => item.endsWith(':started'))).toHaveLength(INITIAL_WORLD_GENERATION_STAGES.length - 1)
    expect(world.state.temporal.worldTime).toBe(0)
    expect(world.state.courier.initialCourierId).toBeUndefined()
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
      version: 6,
      creation: {
        seed: 'river ash',
        contractVersions: {
          foundationGenerator: 'foundation-2',
          initialWorldGenerator: 'initial-world-1',
          contentSafetyPolicy: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
        },
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
        validationHistory: {
          generation: { validation: { status: 'accepted', issues: [] } },
          initialWorld: {
            version: 1,
            generatorVersion: 'initial-world-1',
            contentSafetyPolicyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
            selectedAttempt: 0
          }
        }
      }
    })
    expect(world.manifest.creation.validationHistory.generation.retryPlan).toEqual(generationRetryPlan('river ash', world.manifest.creation.resolvedConfiguration))
    expect(world.initialWorld.configurationFingerprint).toBeDefined()
    expect(world.initialWorld.historyHorizonYears).toBe(350)
    expect(recreateFoundationWorld(world.manifest)).toEqual(world)
  })

  it('audits generated foundation people, histories, places, events, and player-facing text under the versioned policy', () => {
    const world = createFoundationWorld({ seed: 'policy-ledger' })

    expect(foundationWorldContentSatisfiesSafetyPolicy(world)).toBe(true)
    expect(world.manifest.creation.contentSafetyAudit).toMatchObject({
      policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      status: 'accepted',
      diagnostics: []
    })
    expect(world.state.contentSafetyAudit).toMatchObject({
      policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      status: 'accepted',
      diagnostics: []
    })
    expect(world.manifest.creation.contentSafetyAudit.reviewed).toEqual(expect.arrayContaining([
      { id: 'world:label', domain: 'place', classification: expect.objectContaining({ domains: ['place', 'player-facing-text'] }) },
      { id: 'vessel:jomon', domain: 'place', classification: expect.objectContaining({ domains: ['place', 'player-facing-text'] }) },
      { id: 'causal:0:world-created', domain: 'event', classification: expect.objectContaining({ domains: ['event', 'player-facing-text'] }) }
    ]))
    expect(world.manifest.creation.contentSafetyAudit.reviewed.filter(record => record.domain === 'person')).toHaveLength(world.crew.length + world.initialWorld.people.length)
    expect(world.manifest.creation.contentSafetyAudit.reviewed.filter(record => record.domain === 'history')).toHaveLength(world.crew.length + world.initialWorld.people.length + world.initialWorld.history.length)
    expect(world.manifest.creation.contentSafetyAudit.reviewed).toEqual(expect.arrayContaining([
      { id: world.initialWorld.watershed.id, domain: 'place', classification: expect.any(Object) },
      { id: world.initialWorld.routeHazards[0]!.id, domain: 'hazard', classification: expect.objectContaining({ domains: ['hazard', 'player-facing-text'] }) }
    ]))
    expect(recreateFoundationWorld(world.manifest).state.contentSafetyAudit).toEqual(world.state.contentSafetyAudit)

    const forgedManifest = structuredClone(world.manifest)
    const auditedHistory = forgedManifest.creation.contentSafetyAudit.reviewed.find(record => record.domain === 'history')
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

  it('serializes a valid manifest canonically and anchors world identity in immutable creation provenance', () => {
    const world = createFoundationWorld({ seed: 'manifest-canonical', configuration: { preset: 'far-coast' } })
    const reordered = Object.fromEntries(Object.entries(world.manifest).reverse()) as typeof world.manifest

    expect(serializeWorldManifest(reordered)).toBe(serializeWorldManifest(world.manifest))
    expect(world.id).toBe(`world:${world.manifest.creation.digest}`)
    expect(world.manifest.creation.initialWorld.id).toBe(world.initialWorld.id)
    expect(world.manifest.creation.frontier.initialWorldId).toBe(world.initialWorld.id)
    expect(world.manifest.creation.frontier.roots.map(root => root.id)).toEqual([
      `frontier:region:${world.initialWorld.id}:-1:0`,
      `frontier:region:${world.initialWorld.id}:1:0`,
      `frontier:region:${world.initialWorld.id}:2:0`
    ])
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

  it('rejects immutable provenance tampering at every mutable-world boundary without altering the submitted world', () => {
    const source = chooseInitialCourier(createFoundationWorld({ seed: 'action-boundary-provenance' }), 'crew:0')
    const cases: readonly [string, (world: typeof source) => void][] = [
      ['initial-world', world => { world.initialWorld.watershed.spanLeagues++ }],
      ['jomon', world => { world.jomon.props[0]!.partition = 'berths' }],
      ['crew', world => { world.crew[0]!.name = 'Altered Crew' }],
      ['persistent-person', world => { world.state.people.records[0]!.identity.name = 'Altered Person' }],
      ['manifest-linked', world => { world.manifest.creation.initialWorld.digest = 'forged-initial-world-digest' }]
    ]

    for (const [, tamper] of cases) {
      const tampered = structuredClone(source)
      tamper(tampered)
      const before = structuredClone(tampered)

      expect(validateFoundationWorld(tampered)).not.toEqual([])
      expect(() => advanceFoundationWorldTime(tampered, {
        id: 'wait:rejected-provenance',
        kind: 'wait',
        durationMinutes: 1,
        contentSafety: safety()
      })).toThrow('complete medieval foundation contract')
      expect(tampered).toEqual(before)
    }

    const malformed = structuredClone(source)
    ;(malformed.jomon as { name: string }).name = 'Not Jomon'
    expect(() => chooseInitialCourier(malformed, 'crew:1')).toThrow('complete medieval foundation contract')
    expect(() => finalizeWorldAsChronicle(malformed, 'jomon-loss')).toThrow('complete medieval foundation contract')
  })

  it('turns a terminal world into a read-only chronicle without losing its causal record', () => {
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'last-lantern' }), 'crew:0')
    const chronicle = finalizeWorldAsChronicle(selected, 'crew-extinction')

    expect(chronicle.status).toBe('finalized')
    expect(chronicle.reason).toBe('crew-extinction')
    expect(chronicle.world.state.courier.initialCourierId).toBe('crew:0')
    expect(chronicle.world.crew).toEqual(selected.crew)
  })

  it('accounts only accepted time-bearing actions in a partition-invariant era projection', () => {
    const source = chooseInitialCourier(createFoundationWorld({ seed: 'era-partition' }), 'crew:0')
    const contentSafety = safety()
    const once = advanceFoundationWorldTime(source, { id: 'travel:five', kind: 'travel', durationMinutes: 5, contentSafety })
    let partitioned = source
    for (let minute = 1; minute <= 5; minute++) {
      partitioned = advanceFoundationWorldTime(partitioned, { id: `wait:${minute}`, kind: 'wait', durationMinutes: 1, contentSafety })
    }

    expect(worldEraProjection(once.state.era)).toEqual(worldEraProjection(partitioned.state.era))
    expect(once.state.era.activePlayMinutes).toBe(5)
    expect(partitioned.state.temporal.actionSequence).toBe(5)
    expect(once.state.temporal.actionSequence).toBe(1)
    expect(() => advanceFoundationWorldTime(source, { kind: 'inspect' })).toThrow('temporal contract rejected')
    expect(source.state.era).toEqual(createFoundationWorld({ seed: 'era-partition' }).state.era)
  })

  it('records typed durable Jomon growth at the current world minute without changing unrelated world state', () => {
    const source = chooseInitialCourier(createFoundationWorld({ seed: 'era-growth' }), 'crew:0')
    const contentSafety = safety()
    const advanced = advanceFoundationWorldTime(source, { id: 'travel:era-growth', kind: 'travel', durationMinutes: 4_800, contentSafety })
    const immutable = { manifest: structuredClone(advanced.manifest), jomon: structuredClone(advanced.jomon), crew: structuredClone(advanced.crew) }
    const simulation = structuredClone(advanced.state.simulation)
    const grown = recordDurableJomonGrowth(advanced, {
      id: 'growth:era-growth:deck',
      kind: 'physical-expansion',
      source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
      atWorldTime: 4_800
    })

    expect(grown.state.temporal).toEqual(advanced.state.temporal)
    expect(grown.state.simulation).toEqual(simulation)
    expect(grown.manifest).toEqual(immutable.manifest)
    expect(grown.jomon).toEqual(immutable.jomon)
    expect(grown.crew).toEqual(immutable.crew)
    expect(grown.state.era).toMatchObject({ era: 'ng-plus', activePlayMinutes: 4_800, durableGrowthUnits: 2_400 })
    expect(grown.state.era.transitions[0]).toMatchObject({ atWorldTime: 4_800, evidence: { trigger: 'durable-jomon-growth', growthEvidenceId: 'growth:era-growth:deck' } })
    expect(() => recordDurableJomonGrowth(grown, { id: 'growth:era-growth:deck', kind: 'small-craft', source: { kind: 'jomon-vessel', id: 'vessel:jomon' }, atWorldTime: 4_800 })).toThrow('duplicate-growth-id')
  })
})

describe('medieval world index', () => {
  it('keeps active worlds and finalized chronicles in separate local index collections', () => {
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'many-ledgers' }), 'crew:0')
    const chronicle = finalizeWorldAsChronicle(world, 'jomon-loss')

    const active = addWorldToIndex(emptyWorldIndex(), world)
    const finalized = addChronicleToIndex(removeWorldFromIndex(active, world.id), chronicle)

    expect(active.activeWorlds).toEqual([{ id: world.id, label: world.manifest.creation.label, initialCourierId: 'crew:0' }])
    expect(active.chronicles).toEqual([])
    expect(finalized.activeWorlds).toEqual([])
    expect(finalized.chronicles).toEqual([{ id: chronicle.id, label: chronicle.world.manifest.creation.label, reason: 'jomon-loss' }])
  })
})
