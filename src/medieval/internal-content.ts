import { ASCII_GLYPH_CATALOG_VERSION, JOMON_ASCII_GLYPH_CATALOG, validateAsciiGlyphCatalog } from './ascii-glyphs'
import { DELEGATION_CONTRACT_VERSION, DELEGATION_TASK_DEFINITIONS, DELEGATION_TASK_FAMILIES, validateDelegationTaskDefinitions } from './delegation'
import { FRONTIER_CONTRACT_VERSION, FRONTIER_LIMITS } from './frontier'
import { GENERATION_CONFIG_PRESETS, WORLD_GENERATION_CONFIG_VERSION, resolveWorldGenerationConfig } from './generation-config'
import { INITIAL_WORLD_GENERATOR_VERSION, INITIAL_WORLD_GENERATION_STAGES } from './initial-world'
import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, PROHIBITED_MEDIEVAL_CONTENT_CLASSES } from './content-safety'
import { FOUNDATION_GENERATOR_VERSION, FOUNDATION_MANIFEST_VERSION } from './types'

/**
 * This version owns only the closed catalogue policy, never the data owned by
 * the referenced domain modules. It is not a content-pack format or loader.
 */
export const INTERNAL_CONTENT_CATALOGUE_VERSION = 1 as const

export const INTERNAL_CONTENT_SOURCE_CATEGORIES = [
  'developer-authored-compiled-typescript',
  'deterministic-generator-input',
  'renderer-semantic-data',
  'runtime-task-definition-data',
  'generated-world-record',
  'forbidden-user-public-extension-input'
] as const
export type InternalContentSourceCategory = typeof INTERNAL_CONTENT_SOURCE_CATEGORIES[number]

export const INTERNAL_CONTENT_OWNERS = [
  'content-safety',
  'generation-config',
  'initial-world',
  'frontier',
  'ascii-glyphs',
  'delegation',
  'world',
  'application-internal-boundary'
] as const
export type InternalContentOwner = typeof INTERNAL_CONTENT_OWNERS[number]

export const INTERNAL_CONTENT_SOURCE_IMPACTS = [
  'policy-validation',
  'generator-provenance-and-recreation',
  'renderer-semantic-contract',
  'persisted-task-contract-and-replay',
  'validated-world-record-and-replay',
  'forbidden-no-runtime-effect'
] as const
export type InternalContentSourceImpact = typeof INTERNAL_CONTENT_SOURCE_IMPACTS[number]

export const INTERNAL_CONTENT_CHANGE_RULES = [
  'owner-policy-version-and-replay-decision-required',
  'owner-generator-and-provenance-decision-required',
  'owner-renderer-contract-version-and-replay-decision-required',
  'owner-task-contract-version-and-replay-decision-required',
  'owner-world-manifest-and-replay-decision-required',
  'no-public-compatibility-or-extension-surface'
] as const
export type InternalContentChangeRule = typeof INTERNAL_CONTENT_CHANGE_RULES[number]

export interface InternalContentSafetyRequirement {
  policyVersion: typeof MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
  classification: 'owner-validated-canonical-classification-required'
  prohibitedContent: 'all-policy-prohibited-classes-affirmatively-excluded'
}

export interface InternalContentFamilyDescriptor {
  id: string
  owner: InternalContentOwner
  ownerVersion: string
  sourceCategory: InternalContentSourceCategory
  safety: InternalContentSafetyRequirement
  sourceImpact: InternalContentSourceImpact
  changeRule: InternalContentChangeRule
  visibility: 'not-a-world-fact' | 'known-facts-only' | 'validated-world-records' | 'forbidden'
}

export const INTERNAL_CONTENT_EXTENSION_POLICY = {
  ownership: 'application-owned-closed',
  userProvidedContent: 'forbidden',
  runtimeContentLoader: 'forbidden',
  dynamicModuleLoading: 'forbidden',
  executableContentOrHooks: 'forbidden',
  networkOrBrowserFetch: 'forbidden',
  persistenceRegistry: 'forbidden',
  publicCompatibilityPromise: 'none'
} as const

export interface InternalContentCatalogue {
  version: typeof INTERNAL_CONTENT_CATALOGUE_VERSION
  extensionPolicy: typeof INTERNAL_CONTENT_EXTENSION_POLICY
  families: readonly InternalContentFamilyDescriptor[]
}

export type InternalContentDiagnosticCode =
  | 'internal-content.malformed-catalogue'
  | 'internal-content.unknown-catalogue-version'
  | 'internal-content.invalid-extension-policy'
  | 'internal-content.catalogue-bounds-exceeded'
  | 'internal-content.invalid-family-descriptor'
  | 'internal-content.invalid-family-id'
  | 'internal-content.duplicate-family-id'
  | 'internal-content.noncanonical-family-order'
  | 'internal-content.unknown-family'
  | 'internal-content.unknown-owner'
  | 'internal-content.unknown-source-category'
  | 'internal-content.missing-safety-metadata'
  | 'internal-content.unsafe-safety-requirement'
  | 'internal-content.invalid-source-impact'
  | 'internal-content.invalid-change-rule'
  | 'internal-content.invalid-visibility-rule'
  | 'internal-content.noncanonical-family-rule'
  | 'internal-content.forbidden-extension-source'
  | 'internal-content.owner-data-invalid'

export interface InternalContentDiagnostic {
  familyId: string
  code: InternalContentDiagnosticCode
}

const MAXIMUM_INTERNAL_CONTENT_FAMILIES = 8
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const hasOnlyKeys = (value: Record<string, unknown>, keys: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const expected = [...keys].sort(compare)
  return actual.length === expected.length && actual.every((key, index) => key === expected[index])
}
const diagnostic = (familyId: string, code: InternalContentDiagnosticCode): InternalContentDiagnostic => ({ familyId, code })
const canonicalDiagnostics = (diagnostics: readonly InternalContentDiagnostic[]): readonly InternalContentDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.familyId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.familyId, right.familyId) || compare(left.code, right.code))
const familyId = (value: unknown): value is string => typeof value === 'string' && /^[a-z][a-z0-9-]*$/u.test(value)

const safetyRequirement = (): InternalContentSafetyRequirement => ({
  policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
  classification: 'owner-validated-canonical-classification-required',
  prohibitedContent: 'all-policy-prohibited-classes-affirmatively-excluded'
})

const canonicalCatalogue: InternalContentCatalogue = {
  version: INTERNAL_CONTENT_CATALOGUE_VERSION,
  extensionPolicy: INTERNAL_CONTENT_EXTENSION_POLICY,
  families: [
    {
      id: 'content-safety-policy',
      owner: 'content-safety',
      ownerVersion: `policy-v${MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION}`,
      sourceCategory: 'developer-authored-compiled-typescript',
      safety: safetyRequirement(),
      sourceImpact: 'policy-validation',
      changeRule: 'owner-policy-version-and-replay-decision-required',
      visibility: 'not-a-world-fact'
    },
    {
      id: 'delegation-task-definitions',
      owner: 'delegation',
      ownerVersion: `delegation-v${DELEGATION_CONTRACT_VERSION}`,
      sourceCategory: 'runtime-task-definition-data',
      safety: safetyRequirement(),
      sourceImpact: 'persisted-task-contract-and-replay',
      changeRule: 'owner-task-contract-version-and-replay-decision-required',
      visibility: 'validated-world-records'
    },
    {
      id: 'forbidden-user-public-extension-inputs',
      owner: 'application-internal-boundary',
      ownerVersion: `internal-content-v${INTERNAL_CONTENT_CATALOGUE_VERSION}`,
      sourceCategory: 'forbidden-user-public-extension-input',
      safety: safetyRequirement(),
      sourceImpact: 'forbidden-no-runtime-effect',
      changeRule: 'no-public-compatibility-or-extension-surface',
      visibility: 'forbidden'
    },
    {
      id: 'frontier-generation',
      owner: 'frontier',
      ownerVersion: `frontier-v${FRONTIER_CONTRACT_VERSION}`,
      sourceCategory: 'deterministic-generator-input',
      safety: safetyRequirement(),
      sourceImpact: 'generator-provenance-and-recreation',
      changeRule: 'owner-generator-and-provenance-decision-required',
      visibility: 'known-facts-only'
    },
    {
      id: 'generated-world-records',
      owner: 'world',
      ownerVersion: `${FOUNDATION_GENERATOR_VERSION}/manifest-v${FOUNDATION_MANIFEST_VERSION}`,
      sourceCategory: 'generated-world-record',
      safety: safetyRequirement(),
      sourceImpact: 'validated-world-record-and-replay',
      changeRule: 'owner-world-manifest-and-replay-decision-required',
      visibility: 'validated-world-records'
    },
    {
      id: 'initial-world-generation',
      owner: 'initial-world',
      ownerVersion: INITIAL_WORLD_GENERATOR_VERSION,
      sourceCategory: 'deterministic-generator-input',
      safety: safetyRequirement(),
      sourceImpact: 'generator-provenance-and-recreation',
      changeRule: 'owner-generator-and-provenance-decision-required',
      visibility: 'validated-world-records'
    },
    {
      id: 'renderer-semantic-glyphs',
      owner: 'ascii-glyphs',
      ownerVersion: `glyph-catalogue-v${ASCII_GLYPH_CATALOG_VERSION}`,
      sourceCategory: 'renderer-semantic-data',
      safety: safetyRequirement(),
      sourceImpact: 'renderer-semantic-contract',
      changeRule: 'owner-renderer-contract-version-and-replay-decision-required',
      visibility: 'not-a-world-fact'
    },
    {
      id: 'world-generation-config',
      owner: 'generation-config',
      ownerVersion: `generation-config-v${WORLD_GENERATION_CONFIG_VERSION}`,
      sourceCategory: 'deterministic-generator-input',
      safety: safetyRequirement(),
      sourceImpact: 'generator-provenance-and-recreation',
      changeRule: 'owner-generator-and-provenance-decision-required',
      visibility: 'not-a-world-fact'
    }
  ]
}

/** Returns a clone so callers cannot mutate the closed application catalogue. */
export const internalContentCatalogue = (): InternalContentCatalogue => structuredClone(canonicalCatalogue)

const validSafetyRequirement = (value: unknown): value is InternalContentSafetyRequirement => record(value)
  && hasOnlyKeys(value, ['policyVersion', 'classification', 'prohibitedContent'])
  && value.policyVersion === MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
  && value.classification === 'owner-validated-canonical-classification-required'
  && value.prohibitedContent === 'all-policy-prohibited-classes-affirmatively-excluded'

const validFamilyShape = (value: unknown): value is InternalContentFamilyDescriptor => record(value)
  && hasOnlyKeys(value, ['id', 'owner', 'ownerVersion', 'sourceCategory', 'safety', 'sourceImpact', 'changeRule', 'visibility'])
  && familyId(value.id)
  && oneOf(INTERNAL_CONTENT_OWNERS, value.owner)
  && typeof value.ownerVersion === 'string' && value.ownerVersion.length > 0
  && oneOf(INTERNAL_CONTENT_SOURCE_CATEGORIES, value.sourceCategory)
  && validSafetyRequirement(value.safety)
  && oneOf(INTERNAL_CONTENT_SOURCE_IMPACTS, value.sourceImpact)
  && oneOf(INTERNAL_CONTENT_CHANGE_RULES, value.changeRule)
  && (value.visibility === 'not-a-world-fact' || value.visibility === 'known-facts-only' || value.visibility === 'validated-world-records' || value.visibility === 'forbidden')

/**
 * Fails closed for every descriptor that is not the one closed, compiled
 * application policy recognizes. Validation performs no loading or mutation.
 */
export const validateInternalContentCatalogue = (value: unknown): readonly InternalContentDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'extensionPolicy', 'families'])) return [diagnostic('internal-content-catalogue', 'internal-content.malformed-catalogue')]
  const diagnostics: InternalContentDiagnostic[] = []
  if (value.version !== INTERNAL_CONTENT_CATALOGUE_VERSION) diagnostics.push(diagnostic('internal-content-catalogue', 'internal-content.unknown-catalogue-version'))
  if (!same(value.extensionPolicy, INTERNAL_CONTENT_EXTENSION_POLICY)) diagnostics.push(diagnostic('internal-content-catalogue', 'internal-content.invalid-extension-policy'))
  if (!Array.isArray(value.families) || value.families.length !== canonicalCatalogue.families.length || value.families.length > MAXIMUM_INTERNAL_CONTENT_FAMILIES) {
    diagnostics.push(diagnostic('internal-content-catalogue', 'internal-content.catalogue-bounds-exceeded'))
  }
  const families = Array.isArray(value.families) ? value.families : []
  const seen = new Set<string>()
  for (const candidate of families) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'internal-content-family'
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'owner', 'ownerVersion', 'sourceCategory', 'safety', 'sourceImpact', 'changeRule', 'visibility'])) {
      diagnostics.push(diagnostic(id, 'internal-content.invalid-family-descriptor'))
      continue
    }
    if (!familyId(candidate.id)) diagnostics.push(diagnostic(id, 'internal-content.invalid-family-id'))
    if (typeof candidate.id === 'string' && seen.has(candidate.id)) diagnostics.push(diagnostic(candidate.id, 'internal-content.duplicate-family-id'))
    if (typeof candidate.id === 'string') seen.add(candidate.id)
    if (!oneOf(INTERNAL_CONTENT_OWNERS, candidate.owner)) diagnostics.push(diagnostic(id, 'internal-content.unknown-owner'))
    if (!oneOf(INTERNAL_CONTENT_SOURCE_CATEGORIES, candidate.sourceCategory)) diagnostics.push(diagnostic(id, 'internal-content.unknown-source-category'))
    if (!record(candidate.safety) || !hasOnlyKeys(candidate.safety, ['policyVersion', 'classification', 'prohibitedContent'])) diagnostics.push(diagnostic(id, 'internal-content.missing-safety-metadata'))
    else if (!validSafetyRequirement(candidate.safety)) diagnostics.push(diagnostic(id, 'internal-content.unsafe-safety-requirement'))
    if (!oneOf(INTERNAL_CONTENT_SOURCE_IMPACTS, candidate.sourceImpact)) diagnostics.push(diagnostic(id, 'internal-content.invalid-source-impact'))
    if (!oneOf(INTERNAL_CONTENT_CHANGE_RULES, candidate.changeRule)) diagnostics.push(diagnostic(id, 'internal-content.invalid-change-rule'))
    if (candidate.visibility !== 'not-a-world-fact' && candidate.visibility !== 'known-facts-only' && candidate.visibility !== 'validated-world-records' && candidate.visibility !== 'forbidden') diagnostics.push(diagnostic(id, 'internal-content.invalid-visibility-rule'))
    const expected = typeof candidate.id === 'string' ? canonicalCatalogue.families.find(family => family.id === candidate.id) : undefined
    if (!expected) diagnostics.push(diagnostic(id, 'internal-content.unknown-family'))
    else if (!validFamilyShape(candidate) || !same(candidate, expected)) diagnostics.push(diagnostic(id, 'internal-content.noncanonical-family-rule'))
    if (candidate.sourceCategory === 'forbidden-user-public-extension-input' && candidate.id !== 'forbidden-user-public-extension-inputs') diagnostics.push(diagnostic(id, 'internal-content.forbidden-extension-source'))
  }
  const canonicalIds = families.map(candidate => record(candidate) && typeof candidate.id === 'string' ? candidate.id : '').filter(Boolean)
  if (canonicalIds.some((id, index) => index > 0 && compare(canonicalIds[index - 1]!, id) >= 0)) diagnostics.push(diagnostic('internal-content-catalogue', 'internal-content.noncanonical-family-order'))
  return canonicalDiagnostics(diagnostics)
}

/** Verifies that the descriptors point at the current owners instead of copied data. */
export const validateCurrentInternalContentOwners = (): readonly InternalContentDiagnostic[] => {
  const diagnostics: InternalContentDiagnostic[] = []
  const catalogue = internalContentCatalogue()
  const family = (id: string): InternalContentFamilyDescriptor => catalogue.families.find(candidate => candidate.id === id)!
  if (family('content-safety-policy').ownerVersion !== `policy-v${MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION}` || PROHIBITED_MEDIEVAL_CONTENT_CLASSES.length !== 4) diagnostics.push(diagnostic('content-safety-policy', 'internal-content.owner-data-invalid'))
  if (family('world-generation-config').ownerVersion !== `generation-config-v${WORLD_GENERATION_CONFIG_VERSION}` || Object.keys(GENERATION_CONFIG_PRESETS).some(preset => resolveWorldGenerationConfig({ preset: preset as keyof typeof GENERATION_CONFIG_PRESETS }).status !== 'valid')) diagnostics.push(diagnostic('world-generation-config', 'internal-content.owner-data-invalid'))
  if (family('initial-world-generation').ownerVersion !== INITIAL_WORLD_GENERATOR_VERSION || (INITIAL_WORLD_GENERATION_STAGES as readonly string[]).length === 0) diagnostics.push(diagnostic('initial-world-generation', 'internal-content.owner-data-invalid'))
  if (family('frontier-generation').ownerVersion !== `frontier-v${FRONTIER_CONTRACT_VERSION}` || FRONTIER_LIMITS.regions <= 0) diagnostics.push(diagnostic('frontier-generation', 'internal-content.owner-data-invalid'))
  if (family('renderer-semantic-glyphs').ownerVersion !== `glyph-catalogue-v${ASCII_GLYPH_CATALOG_VERSION}` || validateAsciiGlyphCatalog(JOMON_ASCII_GLYPH_CATALOG).length !== 0) diagnostics.push(diagnostic('renderer-semantic-glyphs', 'internal-content.owner-data-invalid'))
  if (family('delegation-task-definitions').ownerVersion !== `delegation-v${DELEGATION_CONTRACT_VERSION}` || DELEGATION_TASK_DEFINITIONS.length !== DELEGATION_TASK_FAMILIES.length || validateDelegationTaskDefinitions().length !== 0) diagnostics.push(diagnostic('delegation-task-definitions', 'internal-content.owner-data-invalid'))
  if (family('generated-world-records').ownerVersion !== `${FOUNDATION_GENERATOR_VERSION}/manifest-v${FOUNDATION_MANIFEST_VERSION}`) diagnostics.push(diagnostic('generated-world-records', 'internal-content.owner-data-invalid'))
  return canonicalDiagnostics(diagnostics)
}
