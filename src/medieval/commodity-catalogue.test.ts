import { describe, expect, it } from 'vitest'
import { auditMedievalContentSafety } from './content-safety'
import { COMMODITY_CATALOGUE_VERSION, COMMODITY_CONDITIONS, COMMODITY_FAILURE_MODES, COMMODITY_HANDLING_REQUIREMENTS, COMMODITY_WEIGHTS, JOMON_COMMODITY_IDS, commodityCatalogue, commodityContentRecords, isCommodityCatalogue, validateCommodityCatalogue } from './commodity-catalogue'

const catalogue = () => commodityCatalogue()
const codes = (value: unknown): readonly string[] => validateCommodityCatalogue(value).map(diagnostic => diagnostic.code)

describe('closed Jomon commodity catalogue', () => {
  it('defines the small canonical set with every required material trade fact', () => {
    const first = catalogue()
    const second = catalogue()
    const altered = catalogue()
    altered.commodities[0]!.name = 'Altered charcoal'

    expect(COMMODITY_CATALOGUE_VERSION).toBe(1)
    expect(first).toEqual(second)
    expect(catalogue()).toEqual(first)
    expect(first.commodities.map(definition => definition.id)).toEqual(JOMON_COMMODITY_IDS)
    expect(first.commodities).toHaveLength(8)
    expect(first.commodities).toEqual(expect.arrayContaining([
      expect.objectContaining({ id: 'commodity:grain', source: 'river-mill', use: 'household-bread', weight: 'heavy', bulk: 'bulky', condition: 'dry', handlingRequirement: 'covered-sacks', failureMode: 'damp-spoilage', buyer: 'town-baker' }),
      expect.objectContaining({ id: 'commodity:salt-fish', source: 'coastal-curing-yard', use: 'preserved-provisions', condition: 'salt-cured', handlingRequirement: 'dry-packing', buyer: 'inland-provisioner' }),
      expect.objectContaining({ id: 'commodity:timber', source: 'riverside-yard', use: 'vessel-and-building-work', weight: 'heavy', bulk: 'bulky', handlingRequirement: 'lashed-bundles', failureMode: 'warping', buyer: 'shipwright' }),
      expect.objectContaining({ id: 'commodity:paper', source: 'paper-mill', use: 'records-and-letters', weight: 'light', bulk: 'compact', handlingRequirement: 'wrapped-bundles', failureMode: 'water-damage', buyer: 'scribe-house' })
    ]))
    expect(first.commodities.every(definition => definition.name.length > 0 && definition.source.length > 0 && definition.use.length > 0 && COMMODITY_WEIGHTS.includes(definition.weight) && definition.bulk.length > 0 && COMMODITY_CONDITIONS.includes(definition.condition) && COMMODITY_HANDLING_REQUIREMENTS.includes(definition.handlingRequirement) && COMMODITY_FAILURE_MODES.includes(definition.failureMode) && definition.buyer.length > 0)).toBe(true)
    expect(validateCommodityCatalogue(first)).toEqual([])
    expect(isCommodityCatalogue(first)).toBe(true)
    expect(validateCommodityCatalogue(altered)).toContainEqual({ commodityId: 'commodity:charcoal', code: 'commodity-catalogue.noncanonical-definition' })
  })

  it('safety-audits every authored name and material description as closed player-facing data', () => {
    const first = catalogue()
    const records = commodityContentRecords(first.commodities)
    const unsafe = structuredClone(first)
    ;(unsafe.commodities[0]!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'

    expect(records.map(record => record.id)).toEqual(JOMON_COMMODITY_IDS)
    expect(auditMedievalContentSafety(records).status).toBe('accepted')
    expect(codes(unsafe)).toEqual(expect.arrayContaining([
      'commodity-catalogue.noncanonical-definition',
      'content-safety.prohibited.torture'
    ]))
  })

  it('fails closed for malformed, reordered, duplicate, unknown, extended, and unclassified definitions', () => {
    const malformed = { version: 1, commodities: [] }
    const reordered = catalogue()
    reordered.commodities = [...reordered.commodities].reverse()
    const duplicate = catalogue()
    duplicate.commodities[1]!.id = duplicate.commodities[0]!.id
    const unknown = catalogue()
    unknown.commodities[0]!.id = 'commodity:unknown' as never
    const extended = catalogue() as unknown as { commodities: Record<string, unknown>[] }
    extended.commodities[0]!.extension = 'not-a-content-pack'
    const unclassified = catalogue() as unknown as { commodities: { contentSafety?: unknown }[] }
    delete unclassified.commodities[0]!.contentSafety

    expect(codes(malformed)).toContain('commodity-catalogue.invalid-bounds')
    expect(codes(reordered)).toContain('commodity-catalogue.noncanonical-order')
    expect(codes(duplicate)).toEqual(expect.arrayContaining(['commodity-catalogue.duplicate-id', 'commodity-catalogue.noncanonical-order']))
    expect(codes(unknown)).toEqual(expect.arrayContaining(['commodity-catalogue.unknown-commodity', 'commodity-catalogue.noncanonical-order']))
    expect(codes(extended)).toContain('commodity-catalogue.malformed-definition')
    expect(codes(unclassified)).toEqual(expect.arrayContaining(['commodity-catalogue.malformed-definition', 'content-safety.missing-classification']))
    expect(isCommodityCatalogue(reordered)).toBe(false)
  })

  it('does not create cargo, market, vessel, persistence, geometry, person, or generated-world state', () => {
    const encoded = JSON.stringify(catalogue())

    for (const forbidden of ['cargoUnits', 'commodityStates', 'worldTime', 'coordinate', 'routeId', 'settlementId', 'personId', 'manifest', 'seed', 'inventory', 'location']) expect(encoded).not.toContain(forbidden)
  })
})
