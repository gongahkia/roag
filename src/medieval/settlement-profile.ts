import { commodityCatalogue, type JomonCommodityId } from './commodity-catalogue'
import { classifyMedievalContent, validateMedievalContentSafety, type ClassifiedMedievalContent, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'

/**
 * One closed, authored settlement reference for the first physical-economy
 * slice. It is not a generated settlement, world site, market, or contract.
 */
export const SETTLEMENT_PROFILE_VERSION = 1 as const

export const SETTLEMENT_PROFILE_IDS = ['settlement-profile:hearthford-mill-quay'] as const
export type SettlementProfileId = typeof SETTLEMENT_PROFILE_IDS[number]

export const SETTLEMENT_WATER_RELATIONSHIP_KINDS = ['shared-mill-reach'] as const
export type SettlementWaterRelationshipKind = typeof SETTLEMENT_WATER_RELATIONSHIP_KINDS[number]

export const SETTLEMENT_LABOUR_KINDS = ['millers', 'quay-carriers', 'weir-tenders'] as const
export type SettlementLabourKind = typeof SETTLEMENT_LABOUR_KINDS[number]

export const SETTLEMENT_AUTHORITY_KINDS = ['mill-lease-and-quay-ward'] as const
export type SettlementAuthorityKind = typeof SETTLEMENT_AUTHORITY_KINDS[number]

export const SETTLEMENT_SERVICE_KINDS = ['covered-landing', 'public-tally-table', 'witness-ledger'] as const
export type SettlementServiceKind = typeof SETTLEMENT_SERVICE_KINDS[number]

export const SETTLEMENT_LOCAL_PRESSURE_KINDS = ['mill-race-schedule'] as const
export type SettlementLocalPressureKind = typeof SETTLEMENT_LOCAL_PRESSURE_KINDS[number]

export const SETTLEMENT_CIVILIAN_PURPOSE_KINDS = ['milling-and-river-supply'] as const
export type SettlementCivilianPurposeKind = typeof SETTLEMENT_CIVILIAN_PURPOSE_KINDS[number]

export interface SettlementWaterRelationship {
  kind: SettlementWaterRelationshipKind
  description: string
}

export interface SettlementLabourProfile {
  id: string
  kind: SettlementLabourKind
  description: string
}

export interface SettlementAuthorityProfile {
  kind: SettlementAuthorityKind
  description: string
}

export interface SettlementServiceProfile {
  id: string
  kind: SettlementServiceKind
  description: string
}

export interface SettlementDemandProfile {
  commodityId: JomonCommodityId
  description: string
}

export interface SettlementLocalPressureProfile {
  kind: SettlementLocalPressureKind
  description: string
}

export interface SettlementCivilianPurposeProfile {
  kind: SettlementCivilianPurposeKind
  description: string
}

export interface NamedSettlementProfile {
  version: typeof SETTLEMENT_PROFILE_VERSION
  id: SettlementProfileId
  name: string
  waterRelationship: SettlementWaterRelationship
  labour: readonly SettlementLabourProfile[]
  authority: SettlementAuthorityProfile
  services: readonly SettlementServiceProfile[]
  demand: readonly SettlementDemandProfile[]
  localPressure: SettlementLocalPressureProfile
  civilianPurpose: SettlementCivilianPurposeProfile
  contentSafety: MedievalContentSafetyClassification
}

export type SettlementProfileDiagnosticCode =
  | 'settlement-profile.malformed-profile'
  | 'settlement-profile.invalid-version'
  | 'settlement-profile.invalid-id'
  | 'settlement-profile.invalid-labour'
  | 'settlement-profile.invalid-services'
  | 'settlement-profile.invalid-demand'
  | 'settlement-profile.invalid-commodity-reference'
  | 'settlement-profile.unknown-profile'
  | 'settlement-profile.noncanonical-profile'
  | MedievalContentSafetyDiagnosticCode

export interface SettlementProfileDiagnostic {
  profileId: string
  code: SettlementProfileDiagnosticCode
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const keys = [...expected].sort(compare)
  return actual.length === keys.length && actual.every((key, index) => key === keys[index])
}
const diagnostic = (profileId: string, code: SettlementProfileDiagnosticCode): SettlementProfileDiagnostic => ({ profileId, code })
const canonicalDiagnostics = (diagnostics: readonly SettlementProfileDiagnostic[]): readonly SettlementProfileDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.profileId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.profileId, right.profileId) || compare(left.code, right.code))
const contentSafety = (): MedievalContentSafetyClassification => classifyMedievalContent('data', ['civil-life', 'commerce', 'craft', 'navigation', 'settlement'], 'not-applicable', ['place', 'player-facing-text'])

const profileKeys = ['version', 'id', 'name', 'waterRelationship', 'labour', 'authority', 'services', 'demand', 'localPressure', 'civilianPurpose', 'contentSafety'] as const
const waterKeys = ['kind', 'description'] as const
const labourKeys = ['id', 'kind', 'description'] as const
const authorityKeys = ['kind', 'description'] as const
const serviceKeys = ['id', 'kind', 'description'] as const
const demandKeys = ['commodityId', 'description'] as const
const pressureKeys = ['kind', 'description'] as const
const purposeKeys = ['kind', 'description'] as const

const canonicalProfile: NamedSettlementProfile = {
  version: SETTLEMENT_PROFILE_VERSION,
  id: 'settlement-profile:hearthford-mill-quay',
  name: 'Hearthford Mill Quay',
  waterRelationship: {
    kind: 'shared-mill-reach',
    description: 'A shared landing below a mill race, where river water and working barges use the same reach.'
  },
  labour: [
    { id: 'settlement-profile:hearthford-mill-quay:labour:millers', kind: 'millers', description: 'Millers keep grain moving through the water-powered mill.' },
    { id: 'settlement-profile:hearthford-mill-quay:labour:quay-carriers', kind: 'quay-carriers', description: 'Quay carriers move counted sacks between the landing and nearby households.' },
    { id: 'settlement-profile:hearthford-mill-quay:labour:weir-tenders', kind: 'weir-tenders', description: 'Weir tenders keep the shared channel clear for mill work and local boats.' }
  ],
  authority: {
    kind: 'mill-lease-and-quay-ward',
    description: 'Mill leaseholders set the mill schedule while the quay ward keeps a public landing order.'
  },
  services: [
    { id: 'settlement-profile:hearthford-mill-quay:service:covered-landing', kind: 'covered-landing', description: 'A covered landing keeps ordinary goods out of the rain while they are counted.' },
    { id: 'settlement-profile:hearthford-mill-quay:service:public-tally-table', kind: 'public-tally-table', description: 'A public tally table records measured sacks and witnessed handoffs.' },
    { id: 'settlement-profile:hearthford-mill-quay:service:witness-ledger', kind: 'witness-ledger', description: 'A witness ledger is available for ordinary delivery and work records.' }
  ],
  demand: [
    { commodityId: 'commodity:ironwork', description: 'Ironwork is wanted for mill fittings, hand tools, and river hardware.' },
    { commodityId: 'commodity:salt-fish', description: 'Salt fish is wanted for household provisions during the busiest mill work.' }
  ],
  localPressure: {
    kind: 'mill-race-schedule',
    description: 'The mill race and landing share a narrow work schedule, so delayed unloading can hold up ordinary milling.'
  },
  civilianPurpose: {
    kind: 'milling-and-river-supply',
    description: 'Hearthford supplies flour and everyday river goods to nearby households through visible shared work.'
  },
  contentSafety: contentSafety()
}

/** Returns a fresh, exact compiled profile; callers cannot alter this owner. */
export const namedSettlementProfile = (): NamedSettlementProfile => structuredClone(canonicalProfile)

/** Content records are developer-authored data that can later become situated text. */
export const settlementProfileContentRecords = (profile: NamedSettlementProfile = canonicalProfile): readonly ClassifiedMedievalContent[] => [{
  id: profile.id,
  domain: 'data',
  classification: structuredClone(profile.contentSafety)
}]

const validText = (value: unknown): value is string => typeof value === 'string' && value.length > 0 && value.length <= 180
const validWaterRelationship = (value: unknown): value is SettlementWaterRelationship => record(value)
  && hasOnlyKeys(value, waterKeys)
  && oneOf(SETTLEMENT_WATER_RELATIONSHIP_KINDS, value.kind)
  && validText(value.description)
const validLabour = (value: unknown): value is SettlementLabourProfile => record(value)
  && hasOnlyKeys(value, labourKeys)
  && typeof value.id === 'string' && /^settlement-profile:hearthford-mill-quay:labour:[a-z-]+$/u.test(value.id)
  && oneOf(SETTLEMENT_LABOUR_KINDS, value.kind)
  && validText(value.description)
const validAuthority = (value: unknown): value is SettlementAuthorityProfile => record(value)
  && hasOnlyKeys(value, authorityKeys)
  && oneOf(SETTLEMENT_AUTHORITY_KINDS, value.kind)
  && validText(value.description)
const validService = (value: unknown): value is SettlementServiceProfile => record(value)
  && hasOnlyKeys(value, serviceKeys)
  && typeof value.id === 'string' && /^settlement-profile:hearthford-mill-quay:service:[a-z-]+$/u.test(value.id)
  && oneOf(SETTLEMENT_SERVICE_KINDS, value.kind)
  && validText(value.description)
const validDemandShape = (value: unknown): value is SettlementDemandProfile => record(value)
  && hasOnlyKeys(value, demandKeys)
  && typeof value.commodityId === 'string'
  && validText(value.description)
const validPressure = (value: unknown): value is SettlementLocalPressureProfile => record(value)
  && hasOnlyKeys(value, pressureKeys)
  && oneOf(SETTLEMENT_LOCAL_PRESSURE_KINDS, value.kind)
  && validText(value.description)
const validPurpose = (value: unknown): value is SettlementCivilianPurposeProfile => record(value)
  && hasOnlyKeys(value, purposeKeys)
  && oneOf(SETTLEMENT_CIVILIAN_PURPOSE_KINDS, value.kind)
  && validText(value.description)

/**
 * Accepts only the exact compiled profile. This is intentionally a content
 * boundary, not an open settlement template or an input parser.
 */
export const validateNamedSettlementProfile = (value: unknown): readonly SettlementProfileDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, profileKeys)) return [diagnostic('settlement-profile', 'settlement-profile.malformed-profile')]
  const profileId = typeof value.id === 'string' ? value.id : 'settlement-profile:unknown'
  const diagnostics: SettlementProfileDiagnostic[] = []
  if (value.version !== SETTLEMENT_PROFILE_VERSION) diagnostics.push(diagnostic(profileId, 'settlement-profile.invalid-version'))
  if (value.id !== canonicalProfile.id) diagnostics.push(diagnostic(profileId, value.id === undefined || typeof value.id !== 'string' ? 'settlement-profile.invalid-id' : 'settlement-profile.unknown-profile'))
  if (!validWaterRelationship(value.waterRelationship) || !validAuthority(value.authority) || !validPressure(value.localPressure) || !validPurpose(value.civilianPurpose)) diagnostics.push(diagnostic(profileId, 'settlement-profile.malformed-profile'))
  if (!Array.isArray(value.labour) || value.labour.length !== canonicalProfile.labour.length || !value.labour.every(validLabour)) diagnostics.push(diagnostic(profileId, 'settlement-profile.invalid-labour'))
  if (!Array.isArray(value.services) || value.services.length !== canonicalProfile.services.length || !value.services.every(validService)) diagnostics.push(diagnostic(profileId, 'settlement-profile.invalid-services'))
  if (!Array.isArray(value.demand) || value.demand.length !== canonicalProfile.demand.length || !value.demand.every(validDemandShape)) diagnostics.push(diagnostic(profileId, 'settlement-profile.invalid-demand'))
  else if (!value.demand.every(demand => commodityCatalogue().commodities.some(commodity => commodity.id === demand.commodityId))) diagnostics.push(diagnostic(profileId, 'settlement-profile.invalid-commodity-reference'))
  if (!same(value, canonicalProfile)) diagnostics.push(diagnostic(profileId, 'settlement-profile.noncanonical-profile'))
  const content = [{
    id: profileId,
    domain: 'data' as const,
    classification: value.contentSafety
  }]
  for (const safetyDiagnostic of validateMedievalContentSafety(content).diagnostics) diagnostics.push(diagnostic(safetyDiagnostic.contentId, safetyDiagnostic.code))
  return canonicalDiagnostics(diagnostics)
}

export const isNamedSettlementProfile = (value: unknown): value is NamedSettlementProfile => validateNamedSettlementProfile(value).length === 0
