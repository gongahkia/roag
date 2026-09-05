import { describe, expect, it } from 'vitest'
import { JOMON_ASCII_GLYPH_CATALOG, validateAsciiGlyphCatalog } from './ascii-glyphs'
import { classifyMedievalContent, validateMedievalContentSafety } from './content-safety'
import { commodityCatalogue, validateCommodityCatalogue } from './commodity-catalogue'
import { namedSettlementProfile, validateNamedSettlementProfile } from './settlement-profile'
import { DELEGATION_TASK_DEFINITIONS, DELEGATION_TASK_FAMILIES, validateDelegationTaskDefinitions } from './delegation'
import { createInitialFrontierState, validateFrontierState } from './frontier'
import { resolveWorldGenerationConfig } from './generation-config'
import { generateInitialWorld, validateInitialWorldCandidate } from './initial-world'
import {
  INTERNAL_CONTENT_CATALOGUE_VERSION,
  INTERNAL_CONTENT_EXTENSION_POLICY,
  internalContentCatalogue,
  validateCurrentInternalContentOwners,
  validateInternalContentCatalogue
} from './internal-content'
import { isValidFoundationWorld, createFoundationWorld } from './world'

const catalogue = () => internalContentCatalogue()
const codes = (value: unknown): readonly string[] => validateInternalContentCatalogue(value).map(diagnostic => diagnostic.code)

describe('closed internal medieval content boundary', () => {
  it('produces one canonical, reproducible, application-owned catalogue with no mutable registry', () => {
    const first = catalogue()
    const second = catalogue()
    const altered = catalogue()
    ;(altered.families[0] as unknown as { owner: string }).owner = 'not-an-owner'

    expect(first).toEqual(second)
    expect(catalogue()).toEqual(first)
    expect(INTERNAL_CONTENT_CATALOGUE_VERSION).toBe(3)
    expect(validateInternalContentCatalogue(first)).toEqual([])
    expect(first.extensionPolicy).toEqual(INTERNAL_CONTENT_EXTENSION_POLICY)
    expect(first.families.map(family => [family.id, family.owner, family.sourceCategory])).toEqual([
      ['commodity-definitions', 'commodity-catalogue', 'developer-authored-compiled-typescript'],
      ['content-safety-policy', 'content-safety', 'developer-authored-compiled-typescript'],
      ['delegation-task-definitions', 'delegation', 'runtime-task-definition-data'],
      ['forbidden-user-public-extension-inputs', 'application-internal-boundary', 'forbidden-user-public-extension-input'],
      ['frontier-generation', 'frontier', 'deterministic-generator-input'],
      ['generated-world-records', 'world', 'generated-world-record'],
      ['initial-world-generation', 'initial-world', 'deterministic-generator-input'],
      ['renderer-semantic-glyphs', 'ascii-glyphs', 'renderer-semantic-data'],
      ['settlement-profiles', 'settlement-profile', 'developer-authored-compiled-typescript'],
      ['world-generation-config', 'generation-config', 'deterministic-generator-input']
    ])
  })

  it('references validated owner data without copying a resolver, generator, safety taxonomy, glyph catalogue, or task logic', () => {
    const first = catalogue()

    expect(validateCurrentInternalContentOwners()).toEqual([])
    expect(first.families).toHaveLength(10)
    expect(first.families.map(family => family.id)).toEqual([...first.families.map(family => family.id)].sort())
    expect(first.families.every(family => family.ownerVersion.length > 0 && family.safety.policyVersion === 1 && family.safety.classification === 'owner-validated-canonical-classification-required' && family.safety.prohibitedContent === 'all-policy-prohibited-classes-affirmatively-excluded')).toBe(true)
    expect(first.families.find(family => family.id === 'forbidden-user-public-extension-inputs')).toMatchObject({ sourceImpact: 'forbidden-no-runtime-effect', changeRule: 'no-public-compatibility-or-extension-surface', visibility: 'forbidden' })
    expect(first.families.every(family => !Object.hasOwn(family, 'entries') && !Object.hasOwn(family, 'definitions') && !Object.hasOwn(family, 'presets') && !Object.hasOwn(family, 'loader'))).toBe(true)
  })

  it('fails closed for malformed, duplicate, noncanonical, unsafe, unknown, and extension-shaped descriptors', () => {
    const malformed = catalogue() as unknown as { families: Record<string, unknown>[] }
    delete malformed.families[0]!.safety
    expect(codes(malformed)).toContain('internal-content.invalid-family-descriptor')

    const duplicate = catalogue() as unknown as { families: Record<string, unknown>[] }
    duplicate.families[1]!.id = duplicate.families[0]!.id
    expect(codes(duplicate)).toContain('internal-content.duplicate-family-id')

    const unordered = catalogue() as unknown as { families: Record<string, unknown>[] }
    unordered.families.reverse()
    expect(codes(unordered)).toContain('internal-content.noncanonical-family-order')

    const noncanonicalId = catalogue() as unknown as { families: Record<string, unknown>[] }
    noncanonicalId.families[0]!.id = 'Public Content Pack'
    expect(codes(noncanonicalId)).toContain('internal-content.invalid-family-id')

    const unknownOwner = catalogue() as unknown as { families: Record<string, unknown>[] }
    unknownOwner.families[0]!.owner = 'community-loader'
    expect(codes(unknownOwner)).toContain('internal-content.unknown-owner')

    const unknownCategory = catalogue() as unknown as { families: Record<string, unknown>[] }
    unknownCategory.families[0]!.sourceCategory = 'user-file-json'
    expect(codes(unknownCategory)).toContain('internal-content.unknown-source-category')

    const missingSafety = catalogue() as unknown as { families: Record<string, unknown>[] }
    missingSafety.families[0]!.safety = { policyVersion: 1 }
    expect(codes(missingSafety)).toContain('internal-content.missing-safety-metadata')

    const unsafe = catalogue() as unknown as { families: { safety: Record<string, unknown> }[] }
    unsafe.families[0]!.safety.prohibitedContent = 'may-be-present'
    expect(codes(unsafe)).toContain('internal-content.unsafe-safety-requirement')

    const invalidImpact = catalogue() as unknown as { families: Record<string, unknown>[] }
    invalidImpact.families[0]!.sourceImpact = 'untracked-runtime-effect'
    expect(codes(invalidImpact)).toContain('internal-content.invalid-source-impact')

    const extension = catalogue() as unknown as { families: Record<string, unknown>[] }
    extension.families[0] = {
      ...extension.families[3]!,
      id: 'community-content-pack'
    }
    expect(codes(extension)).toEqual(expect.arrayContaining([
      'internal-content.forbidden-extension-source',
      'internal-content.unknown-family'
    ]))
  })

  it('keeps current owner outputs deterministic, bounded, safety-validated, and replay-valid without changing them', () => {
    const resolved = resolveWorldGenerationConfig({ preset: 'watershed' })
    if (resolved.status !== 'valid') throw new Error('fixture configuration must resolve')
    const initial = generateInitialWorld('internal-content-owner-fixture', resolved.configuration).world
    const frontier = createInitialFrontierState({ seed: 'internal-content-owner-fixture', configuration: resolved.configuration, initialWorld: initial })
    const world = createFoundationWorld({ seed: 'internal-content-owner-fixture', configuration: { preset: 'watershed' } })

    expect(generateInitialWorld('internal-content-owner-fixture', resolved.configuration).world).toEqual(initial)
    expect(validateInitialWorldCandidate(initial, resolved.configuration)).toEqual([])
    expect(validateFrontierState({ seed: 'internal-content-owner-fixture', configuration: resolved.configuration, initialWorld: initial }, frontier)).toEqual([])
    expect(validateAsciiGlyphCatalog(JOMON_ASCII_GLYPH_CATALOG)).toEqual([])
    expect(validateDelegationTaskDefinitions()).toEqual([])
    expect(validateCommodityCatalogue(commodityCatalogue())).toEqual([])
    expect(validateNamedSettlementProfile(namedSettlementProfile())).toEqual([])
    expect(DELEGATION_TASK_DEFINITIONS.map(definition => definition.family)).toEqual([...DELEGATION_TASK_FAMILIES].sort())
    expect(validateMedievalContentSafety([{ id: 'internal-content:safety-fixture', domain: 'data', classification: classifyMedievalContent('data', ['craft', 'navigation'], 'not-applicable') }])).toMatchObject({ status: 'accepted', diagnostics: [] })
    expect(isValidFoundationWorld(world)).toBe(true)
  }, 20_000)
})
