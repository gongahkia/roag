import { describe, expect, it } from 'vitest'
import { auditMedievalContentSafety } from './content-safety'
import { JOMON_COMMODITY_IDS } from './commodity-catalogue'
import { SETTLEMENT_PROFILE_VERSION, isNamedSettlementProfile, namedSettlementProfile, settlementProfileContentRecords, validateNamedSettlementProfile } from './settlement-profile'

const profile = () => namedSettlementProfile()
const codes = (value: unknown): readonly string[] => validateNamedSettlementProfile(value).map(diagnostic => diagnostic.code)

describe('named Hearthford Mill Quay settlement profile', () => {
  it('defines one reproducible civilian settlement with every required material fact', () => {
    const first = profile()
    const second = profile()
    const altered = profile()
    altered.name = 'Altered quay'

    expect(SETTLEMENT_PROFILE_VERSION).toBe(1)
    expect(first).toEqual(second)
    expect(profile()).toEqual(first)
    expect(first).toMatchObject({
      id: 'settlement-profile:hearthford-mill-quay',
      name: 'Hearthford Mill Quay',
      waterRelationship: { kind: 'shared-mill-reach' },
      authority: { kind: 'mill-lease-and-quay-ward' },
      localPressure: { kind: 'mill-race-schedule' },
      civilianPurpose: { kind: 'milling-and-river-supply' }
    })
    expect(first.labour.map(record => record.kind)).toEqual(['millers', 'quay-carriers', 'weir-tenders'])
    expect(first.services.map(record => record.kind)).toEqual(['covered-landing', 'public-tally-table', 'witness-ledger'])
    expect(first.demand.map(record => record.commodityId)).toEqual(['commodity:ironwork', 'commodity:salt-fish'])
    expect(first.demand.every(record => JOMON_COMMODITY_IDS.includes(record.commodityId))).toBe(true)
    expect(validateNamedSettlementProfile(first)).toEqual([])
    expect(isNamedSettlementProfile(first)).toBe(true)
    expect(validateNamedSettlementProfile(altered)).toContainEqual({ profileId: first.id, code: 'settlement-profile.noncanonical-profile' })
  })

  it('safety-audits the complete named player-facing profile', () => {
    const first = profile()
    const unsafe = profile()
    ;(unsafe.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'

    expect(auditMedievalContentSafety(settlementProfileContentRecords(first)).status).toBe('accepted')
    expect(codes(unsafe)).toEqual(expect.arrayContaining([
      'settlement-profile.noncanonical-profile',
      'content-safety.prohibited.torture'
    ]))
  })

  it('fails closed for malformed, unknown, reordered, unclassified, and extended profiles', () => {
    const malformed = { version: 1, id: 'settlement-profile:hearthford-mill-quay' }
    const unknown = profile()
    unknown.id = 'settlement-profile:unknown' as never
    const reordered = profile()
    reordered.labour = [...reordered.labour].reverse()
    const unknownCommodity = profile()
    unknownCommodity.demand[0]!.commodityId = 'commodity:unknown' as never
    const extended = profile() as unknown as Record<string, unknown>
    extended.extension = 'not-a-settlement-pack'
    const unclassified = profile() as unknown as { contentSafety?: unknown }
    delete unclassified.contentSafety

    expect(codes(malformed)).toContain('settlement-profile.malformed-profile')
    expect(codes(unknown)).toEqual(expect.arrayContaining(['settlement-profile.unknown-profile', 'settlement-profile.noncanonical-profile']))
    expect(codes(reordered)).toContain('settlement-profile.noncanonical-profile')
    expect(codes(unknownCommodity)).toEqual(expect.arrayContaining(['settlement-profile.invalid-commodity-reference', 'settlement-profile.noncanonical-profile']))
    expect(codes(extended)).toContain('settlement-profile.malformed-profile')
    expect(codes(unclassified)).toContain('settlement-profile.malformed-profile')
    expect(isNamedSettlementProfile(reordered)).toBe(false)
  })

  it('does not create a world site, market, cargo, route, person, timer, or persistence record', () => {
    const encoded = JSON.stringify(profile())

    for (const forbidden of ['worldTime', 'coordinate', 'routeId', 'settlementId', 'personId', 'manifest', 'seed', 'inventory', 'location', 'price', 'marketId']) expect(encoded).not.toContain(forbidden)
  })
})
