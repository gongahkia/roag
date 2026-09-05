import { classifyMedievalContent, validateMedievalContentSafety, type ClassifiedMedievalContent, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'

/**
 * Closed, compiled trade-goods definitions for the later physical-economy
 * work. This is not cargo state, a market, a content-pack format, or a
 * generated world fact.
 */
export const COMMODITY_CATALOGUE_VERSION = 1 as const

export const JOMON_COMMODITY_IDS = [
  'commodity:charcoal',
  'commodity:grain',
  'commodity:ironwork',
  'commodity:lime',
  'commodity:paper',
  'commodity:salt-fish',
  'commodity:timber',
  'commodity:wool'
] as const
export type JomonCommodityId = typeof JOMON_COMMODITY_IDS[number]

export const COMMODITY_SOURCES = [
  'coastal-curing-yard',
  'lime-kiln',
  'paper-mill',
  'river-mill',
  'riverside-yard',
  'sheepfold',
  'woodland-kiln',
  'yard-forge'
] as const
export type CommoditySource = typeof COMMODITY_SOURCES[number]

export const COMMODITY_USES = [
  'building-mortar',
  'cloth-making',
  'forge-fuel',
  'household-bread',
  'preserved-provisions',
  'records-and-letters',
  'tools-and-fittings',
  'vessel-and-building-work'
] as const
export type CommodityUse = typeof COMMODITY_USES[number]

export const COMMODITY_WEIGHTS = ['light', 'medium', 'heavy'] as const
export type CommodityWeight = typeof COMMODITY_WEIGHTS[number]

export const COMMODITY_BULKS = ['compact', 'bulky'] as const
export type CommodityBulk = typeof COMMODITY_BULKS[number]

export const COMMODITY_CONDITIONS = ['dry', 'salt-cured', 'sound'] as const
export type CommodityCondition = typeof COMMODITY_CONDITIONS[number]

export const COMMODITY_HANDLING_REQUIREMENTS = [
  'covered-bales',
  'covered-sacks',
  'dry-packing',
  'lashed-bundles',
  'sealed-casks',
  'sealed-sacks',
  'secured-crates',
  'wrapped-bundles'
] as const
export type CommodityHandlingRequirement = typeof COMMODITY_HANDLING_REQUIREMENTS[number]

export const COMMODITY_FAILURE_MODES = [
  'crushing',
  'damp-spoilage',
  'moth-damage',
  'rust',
  'slaking',
  'water-damage',
  'warping'
] as const
export type CommodityFailureMode = typeof COMMODITY_FAILURE_MODES[number]

export const COMMODITY_BUYERS = [
  'clothier',
  'forge-master',
  'inland-provisioner',
  'mason',
  'scribe-house',
  'shipwright',
  'town-baker'
] as const
export type CommodityBuyer = typeof COMMODITY_BUYERS[number]

export interface CommodityDefinition {
  version: typeof COMMODITY_CATALOGUE_VERSION
  id: JomonCommodityId
  name: string
  source: CommoditySource
  use: CommodityUse
  weight: CommodityWeight
  bulk: CommodityBulk
  condition: CommodityCondition
  handlingRequirement: CommodityHandlingRequirement
  failureMode: CommodityFailureMode
  buyer: CommodityBuyer
  contentSafety: MedievalContentSafetyClassification
}

export interface CommodityCatalogue {
  version: typeof COMMODITY_CATALOGUE_VERSION
  commodities: readonly CommodityDefinition[]
}

export type CommodityCatalogueDiagnosticCode =
  | 'commodity-catalogue.malformed-catalogue'
  | 'commodity-catalogue.invalid-version'
  | 'commodity-catalogue.invalid-bounds'
  | 'commodity-catalogue.malformed-definition'
  | 'commodity-catalogue.invalid-id'
  | 'commodity-catalogue.duplicate-id'
  | 'commodity-catalogue.unknown-commodity'
  | 'commodity-catalogue.noncanonical-order'
  | 'commodity-catalogue.noncanonical-definition'
  | MedievalContentSafetyDiagnosticCode

export interface CommodityCatalogueDiagnostic {
  commodityId: string
  code: CommodityCatalogueDiagnosticCode
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const keys = [...expected].sort(compare)
  return actual.length === keys.length && actual.every((key, index) => key === keys[index])
}
const diagnostic = (commodityId: string, code: CommodityCatalogueDiagnosticCode): CommodityCatalogueDiagnostic => ({ commodityId, code })
const canonicalDiagnostics = (diagnostics: readonly CommodityCatalogueDiagnostic[]): readonly CommodityCatalogueDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.commodityId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.commodityId, right.commodityId) || compare(left.code, right.code))
const commoditySafety = (): MedievalContentSafetyClassification => classifyMedievalContent('data', ['civil-life', 'commerce', 'craft'], 'not-applicable', ['player-facing-text'])
const definitionKeys = ['version', 'id', 'name', 'source', 'use', 'weight', 'bulk', 'condition', 'handlingRequirement', 'failureMode', 'buyer', 'contentSafety'] as const

const canonicalCatalogue: CommodityCatalogue = {
  version: COMMODITY_CATALOGUE_VERSION,
  commodities: [
    {
      version: COMMODITY_CATALOGUE_VERSION,
      id: 'commodity:charcoal',
      name: 'Charcoal',
      source: 'woodland-kiln',
      use: 'forge-fuel',
      weight: 'light',
      bulk: 'bulky',
      condition: 'dry',
      handlingRequirement: 'sealed-sacks',
      failureMode: 'crushing',
      buyer: 'forge-master',
      contentSafety: commoditySafety()
    },
    {
      version: COMMODITY_CATALOGUE_VERSION,
      id: 'commodity:grain',
      name: 'Grain',
      source: 'river-mill',
      use: 'household-bread',
      weight: 'heavy',
      bulk: 'bulky',
      condition: 'dry',
      handlingRequirement: 'covered-sacks',
      failureMode: 'damp-spoilage',
      buyer: 'town-baker',
      contentSafety: commoditySafety()
    },
    {
      version: COMMODITY_CATALOGUE_VERSION,
      id: 'commodity:ironwork',
      name: 'Ironwork',
      source: 'yard-forge',
      use: 'tools-and-fittings',
      weight: 'heavy',
      bulk: 'compact',
      condition: 'sound',
      handlingRequirement: 'secured-crates',
      failureMode: 'rust',
      buyer: 'shipwright',
      contentSafety: commoditySafety()
    },
    {
      version: COMMODITY_CATALOGUE_VERSION,
      id: 'commodity:lime',
      name: 'Lime',
      source: 'lime-kiln',
      use: 'building-mortar',
      weight: 'heavy',
      bulk: 'compact',
      condition: 'dry',
      handlingRequirement: 'sealed-casks',
      failureMode: 'slaking',
      buyer: 'mason',
      contentSafety: commoditySafety()
    },
    {
      version: COMMODITY_CATALOGUE_VERSION,
      id: 'commodity:paper',
      name: 'Paper',
      source: 'paper-mill',
      use: 'records-and-letters',
      weight: 'light',
      bulk: 'compact',
      condition: 'dry',
      handlingRequirement: 'wrapped-bundles',
      failureMode: 'water-damage',
      buyer: 'scribe-house',
      contentSafety: commoditySafety()
    },
    {
      version: COMMODITY_CATALOGUE_VERSION,
      id: 'commodity:salt-fish',
      name: 'Salt fish',
      source: 'coastal-curing-yard',
      use: 'preserved-provisions',
      weight: 'medium',
      bulk: 'compact',
      condition: 'salt-cured',
      handlingRequirement: 'dry-packing',
      failureMode: 'damp-spoilage',
      buyer: 'inland-provisioner',
      contentSafety: commoditySafety()
    },
    {
      version: COMMODITY_CATALOGUE_VERSION,
      id: 'commodity:timber',
      name: 'Timber',
      source: 'riverside-yard',
      use: 'vessel-and-building-work',
      weight: 'heavy',
      bulk: 'bulky',
      condition: 'sound',
      handlingRequirement: 'lashed-bundles',
      failureMode: 'warping',
      buyer: 'shipwright',
      contentSafety: commoditySafety()
    },
    {
      version: COMMODITY_CATALOGUE_VERSION,
      id: 'commodity:wool',
      name: 'Wool',
      source: 'sheepfold',
      use: 'cloth-making',
      weight: 'medium',
      bulk: 'bulky',
      condition: 'dry',
      handlingRequirement: 'covered-bales',
      failureMode: 'moth-damage',
      buyer: 'clothier',
      contentSafety: commoditySafety()
    }
  ]
}

const definitionFor = (id: unknown): CommodityDefinition | undefined => typeof id === 'string'
  ? canonicalCatalogue.commodities.find(definition => definition.id === id)
  : undefined

/** Returns a fresh compiled catalogue; callers cannot mutate the content owner. */
export const commodityCatalogue = (): CommodityCatalogue => structuredClone(canonicalCatalogue)

/** Each entry is safety-audited as data that can later become player-facing text. */
export const commodityContentRecords = (definitions: readonly CommodityDefinition[] = canonicalCatalogue.commodities): readonly ClassifiedMedievalContent[] => definitions.map(definition => ({
  id: definition.id,
  domain: 'data',
  classification: structuredClone(definition.contentSafety)
}))

/**
 * Strict validation recognizes only the bounded compiled definition set. It
 * never loads, extends, generates, or mutates commodity content.
 */
export const validateCommodityCatalogue = (value: unknown): readonly CommodityCatalogueDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'commodities'])) return [diagnostic('commodity-catalogue', 'commodity-catalogue.malformed-catalogue')]
  const diagnostics: CommodityCatalogueDiagnostic[] = []
  if (value.version !== COMMODITY_CATALOGUE_VERSION) diagnostics.push(diagnostic('commodity-catalogue', 'commodity-catalogue.invalid-version'))
  if (!Array.isArray(value.commodities) || value.commodities.length !== JOMON_COMMODITY_IDS.length) diagnostics.push(diagnostic('commodity-catalogue', 'commodity-catalogue.invalid-bounds'))
  const commodities = Array.isArray(value.commodities) ? value.commodities : []
  const ids: string[] = []
  const seen = new Set<string>()
  for (const candidate of commodities) {
    const commodityId = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'commodity:unknown'
    if (!record(candidate) || !hasOnlyKeys(candidate, definitionKeys)) {
      diagnostics.push(diagnostic(commodityId, 'commodity-catalogue.malformed-definition'))
      continue
    }
    if (typeof candidate.id !== 'string' || !/^commodity:[a-z][a-z0-9-]*$/u.test(candidate.id)) diagnostics.push(diagnostic(commodityId, 'commodity-catalogue.invalid-id'))
    else {
      ids.push(candidate.id)
      if (seen.has(candidate.id)) diagnostics.push(diagnostic(candidate.id, 'commodity-catalogue.duplicate-id'))
      seen.add(candidate.id)
    }
    const expected = definitionFor(candidate.id)
    if (!expected) diagnostics.push(diagnostic(commodityId, 'commodity-catalogue.unknown-commodity'))
    else if (!same(candidate, expected)) diagnostics.push(diagnostic(commodityId, 'commodity-catalogue.noncanonical-definition'))
  }
  if (!same(ids, JOMON_COMMODITY_IDS)) diagnostics.push(diagnostic('commodity-catalogue', 'commodity-catalogue.noncanonical-order'))
  const content = commodities.filter(record).map(candidate => ({
    id: typeof candidate.id === 'string' && candidate.id ? candidate.id : 'commodity:unknown',
    domain: 'data' as const,
    classification: candidate.contentSafety
  }))
  for (const safetyDiagnostic of validateMedievalContentSafety(content).diagnostics) diagnostics.push(diagnostic(safetyDiagnostic.contentId, safetyDiagnostic.code))
  return canonicalDiagnostics(diagnostics)
}

export const isCommodityCatalogue = (value: unknown): value is CommodityCatalogue => validateCommodityCatalogue(value).length === 0
