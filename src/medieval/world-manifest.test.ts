import { describe, expect, it } from 'vitest'
import { createInitialFrontierState } from './frontier'
import type { WorldManifest } from './types'
import { chooseInitialCourier, createFoundationWorld, isReproducibleWorldManifest, recreateFoundationWorld, serializeWorldManifest } from './world'

const reversedObject = <Value extends object>(value: Value): Value => Object.fromEntries(Object.entries(value).reverse()) as Value

describe('world manifest provenance', () => {
  it('canonically serializes immutable creation evidence despite object insertion order', () => {
    const world = createFoundationWorld({ seed: 'canonical evidence', configuration: { preset: 'far-coast' } })
    const reordered: WorldManifest = {
      ...reversedObject(world.manifest),
      creation: reversedObject(world.manifest.creation)
    }

    expect(isReproducibleWorldManifest(reordered)).toBe(true)
    expect(serializeWorldManifest(reordered)).toBe(serializeWorldManifest(world.manifest))
    expect(JSON.parse(serializeWorldManifest(world.manifest))).toEqual(world.manifest)
  })

  it('reconstructs the same zero-time world from creation provenance and keeps later courier state outside it', () => {
    const world = createFoundationWorld({ seed: 'restore creation' })
    const selected = chooseInitialCourier(world, 'crew:0')

    expect(selected.manifest.creation).toEqual(world.manifest.creation)
    expect(recreateFoundationWorld(world.manifest)).toEqual(world)
    expect(recreateFoundationWorld(selected.manifest)).toEqual(selected)
  })

  it('records stable initial-world and frontier-root identities that reproduce the root commitments', () => {
    const first = createFoundationWorld({ seed: 'root identities' })
    const second = createFoundationWorld({ seed: 'root identities' })
    const creation = first.manifest.creation
    const frontier = createInitialFrontierState({ seed: creation.seed, configuration: creation.resolvedConfiguration, initialWorld: first.initialWorld })
    const expectedRoots = frontier.regions.map(region => ({
      id: region.commitment.id,
      coordinate: region.commitment.coordinate,
      kind: region.commitment.kind,
      generationOrder: region.commitment.generationOrder,
      generatorStream: region.commitment.generatorStream,
      anchor: region.commitment.anchor,
      connection: region.commitment.connection
    }))

    expect(second.manifest.creation.initialWorld).toEqual(creation.initialWorld)
    expect(creation.initialWorld.id).toBe(first.initialWorld.id)
    expect(creation.initialWorld.watershedId).toBe(first.initialWorld.watershed.id)
    expect(creation.frontier.initialWorldId).toBe(first.initialWorld.id)
    expect(creation.frontier.roots).toEqual(expectedRoots)
  })

  it('rejects altered candidate/retry history, including a forged rejected candidate', () => {
    const manifest = structuredClone(createFoundationWorld({ seed: 'candidate history' }).manifest)
    const history = manifest.creation.validationHistory
    const candidate = history.initialWorld.candidates[0]!
    ;(candidate as { status: string }).status = 'rejected'
    ;(candidate as { issues: unknown[] }).issues = [{ code: 'initial-world.invalid-root', recordId: 'initial:world' }]

    expect(isReproducibleWorldManifest(manifest)).toBe(false)
    expect(() => recreateFoundationWorld(manifest)).toThrow('does not reproduce')
  })

  it('fails closed on noncanonical ordering, mismatched identities, versions, safety provenance, and malformed data', () => {
    const source = createFoundationWorld({ seed: 'tamper evidence' }).manifest
    const reorderedRoots = structuredClone(source)
    reorderedRoots.creation.frontier.roots.reverse()
    const mismatchedInitialWorld = structuredClone(source)
    mismatchedInitialWorld.creation.frontier.initialWorldId = 'initial:other'
    const changedContract = structuredClone(source)
    changedContract.creation.contractVersions.frontierContract = 99
    const unsafeAudit = structuredClone(source)
    ;(unsafeAudit.creation.contentSafetyAudit.reviewed[0]!.classification.exclusions as unknown as Record<string, string>).torture = 'present'
    const malformed = structuredClone(source) as unknown as { creation: { validationHistory: { initialWorld: { candidates: unknown } } } }
    malformed.creation.validationHistory.initialWorld.candidates = []

    for (const candidate of [reorderedRoots, mismatchedInitialWorld, changedContract, unsafeAudit, malformed]) {
      expect(isReproducibleWorldManifest(candidate)).toBe(false)
    }
  })
})
