import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { EFFECT_MODEL_CONTRACT_VERSION, type EffectCandidate, type EffectKnownFact } from './effects'
import {
  MYSTICAL_EFFECT_FORBIDDEN_CAPABILITIES,
  MYSTICAL_EFFECT_POLICY_VERSION,
  MYSTICAL_EFFECT_RARITIES,
  MYSTICAL_EFFECT_RARITY_AVAILABILITY,
  MYSTICAL_EFFECT_SOURCE_CLASSES,
  auditMysticalEffectPolicies,
  validateMysticalEffectPolicyAuditRequest,
  type MysticalEffectForm,
  type MysticalEffectPolicyAuditRequest,
  type MysticalEffectPolicyDefinition,
  type MysticalEffectRarity,
  type MysticalEffectSourceClass
} from './mystical-effect-policy'

const safety = () => classifyMedievalContent('data', ['craft', 'navigation'], 'not-applicable')

const knownFacts: readonly EffectKnownFact[] = [
  { id: 'fact:actor:courier', kind: 'actor', revision: 1 },
  { id: 'fact:condition:courier', kind: 'condition', revision: 1 },
  { id: 'fact:crew:mate', kind: 'crew-support', revision: 1 },
  { id: 'fact:environment:weir', kind: 'environment', revision: 1 },
  { id: 'fact:equipment:ward', kind: 'equipment', revision: 1 },
  { id: 'fact:object:relic', kind: 'grounded-object', revision: 1 },
  { id: 'fact:practice:watch', kind: 'grounded-practice', revision: 1 }
]

const sourceFor = (sourceClass: MysticalEffectSourceClass) => sourceClass === 'material-object'
  ? { factId: 'fact:object:relic', grounding: 'material-object' as const, conditionFactId: 'fact:object:relic' }
  : sourceClass === 'specific-place'
    ? { factId: 'fact:practice:watch', grounding: 'place-bound-practice' as const, conditionFactId: 'fact:environment:weir' }
    : sourceClass === 'bounded-practice'
      ? { factId: 'fact:practice:watch', grounding: 'place-bound-practice' as const, conditionFactId: 'fact:practice:watch' }
      : { factId: 'fact:crew:mate', grounding: 'crew-relationship' as const, conditionFactId: 'fact:crew:mate' }

const conditionKindFor = (sourceClass: MysticalEffectSourceClass) => sourceClass === 'material-object'
  ? 'object-available' as const
  : sourceClass === 'specific-place'
    ? 'specific-place-known' as const
    : sourceClass === 'bounded-practice'
      ? 'bounded-practice-observed' as const
      : 'relationship-established' as const

const categoryFor = (form: MysticalEffectForm, sourceClass: MysticalEffectSourceClass): EffectCandidate['category'] => sourceClass === 'relationship-condition'
  ? 'crew-support'
  : form

const policy = (
  id: string,
  form: MysticalEffectForm,
  rarity: MysticalEffectRarity,
  sourceClass: MysticalEffectSourceClass,
  options: { reservation?: MysticalEffectPolicyDefinition['futureReservation'] } = {}
): MysticalEffectPolicyDefinition => {
  const source = sourceFor(sourceClass)
  const conditionId = `condition:${id.slice('mystical-policy:'.length)}`
  const costId = `cost:${id.slice('mystical-policy:'.length)}`
  const effectId = `effect:${id.slice('mystical-policy:'.length)}`
  const effect: EffectCandidate = {
    version: EFFECT_MODEL_CONTRACT_VERSION,
    id: effectId,
    category: categoryFor(form, sourceClass),
    polarity: form === 'curse' ? 'hindrance' : 'aid',
    magnitude: 1,
    priority: 4,
    source: { factId: source.factId, factRevision: 1, grounding: source.grounding },
    target: { factId: 'fact:actor:courier', factRevision: 1, scope: 'self' },
    evidence: [{ id: `evidence:${id.slice('mystical-policy:'.length)}`, factId: source.factId, factRevision: 1 }],
    duration: { startsAtWorldTime: 5, expiresAtWorldTime: 20 },
    stacking: { groupId: `stack:${id.slice('mystical-policy:'.length)}`, mode: 'exclusive', cap: 1 },
    conflictsWith: [],
    chainAfter: [],
    counterplay: { id: `counterplay:${id.slice('mystical-policy:'.length)}`, factId: 'fact:equipment:ward', factRevision: 1 },
    contentSafety: safety()
  }
  const availability = MYSTICAL_EFFECT_RARITY_AVAILABILITY[rarity]
  const auditEvidence = [
    { id: `audit:${id.slice('mystical-policy:'.length)}:availability`, kind: 'availability-bound' as const, factId: source.factId, factRevision: 1 },
    { id: `audit:${id.slice('mystical-policy:'.length)}:condition`, kind: 'eligibility-condition' as const, factId: source.conditionFactId, factRevision: 1 },
    { id: `audit:${id.slice('mystical-policy:'.length)}:cost`, kind: 'cost-trade-off' as const, factId: 'fact:condition:courier', factRevision: 1 },
    ...(form === 'curse' ? [{ id: `audit:${id.slice('mystical-policy:'.length)}:counterplay`, kind: 'adverse-counterplay' as const, factId: 'fact:equipment:ward', factRevision: 1 }] : []),
    { id: `audit:${id.slice('mystical-policy:'.length)}:source`, kind: 'source-provenance' as const, factId: source.factId, factRevision: 1 }
  ].sort((left, right) => left.id < right.id ? -1 : left.id > right.id ? 1 : 0)
  return {
    version: MYSTICAL_EFFECT_POLICY_VERSION,
    id,
    form,
    rarity,
    source: { sourceClass, factId: source.factId, factRevision: 1 },
    conditions: [{ id: conditionId, kind: conditionKindFor(sourceClass), factId: source.conditionFactId, factRevision: 1 }],
    availability: {
      scope: 'per-world',
      maximumDiscoveries: availability.maximumDiscoveriesPerWorld,
      maximumConcurrentEligibility: availability.maximumConcurrentEligibility
    },
    useBoundary: { kind: 'finite-duration', maximumWorldMinutes: 15 },
    costs: [{ id: costId, kind: 'action-time-commitment', factId: 'fact:condition:courier', factRevision: 1 }],
    auditEvidence,
    effect,
    contentSafety: safety(),
    ...(options.reservation === undefined ? {} : { futureReservation: options.reservation })
  }
}

const definitions = (): readonly MysticalEffectPolicyDefinition[] => [
  policy('mystical-policy:boon-place-exceptional', 'boon', 'exceptional', 'specific-place'),
  policy('mystical-policy:boon-practice-ultra', 'boon', 'ultra-rare', 'bounded-practice', { reservation: { kind: 'future-jomon-loss-safeguard', status: 'deferred-no-authority' } }),
  policy('mystical-policy:boon-relationship-rare', 'boon', 'rare', 'relationship-condition'),
  policy('mystical-policy:curse-practice-rare', 'curse', 'rare', 'bounded-practice'),
  policy('mystical-policy:relic-material-scarce', 'relic', 'scarce', 'material-object'),
  policy('mystical-policy:totem-material-rare', 'totem', 'rare', 'material-object')
]

const request = (overrides: Partial<MysticalEffectPolicyAuditRequest> = {}): MysticalEffectPolicyAuditRequest => ({
  version: MYSTICAL_EFFECT_POLICY_VERSION,
  actionTime: { timeUnit: 'minute', worldTime: 5 },
  knownFacts,
  definitions: definitions(),
  ...overrides
})

const codes = (value: unknown): readonly string[] => validateMysticalEffectPolicyAuditRequest(value).diagnostics.map(diagnostic => diagnostic.code)

describe('low-mysticism effect policy and audit boundary', () => {
  it('audits canonical application-owned policy definitions deterministically without mutating them', () => {
    const input = request()
    const before = structuredClone(input)
    const first = auditMysticalEffectPolicies(input)
    const second = auditMysticalEffectPolicies(structuredClone(input))

    expect(validateMysticalEffectPolicyAuditRequest(input)).toEqual({ version: 1, status: 'accepted', diagnostics: [] })
    expect(first).toEqual(second)
    expect(first.reviewed.map(item => [item.id, item.rarity, item.sourceClass, item.effectCategory])).toEqual([
      ['mystical-policy:boon-place-exceptional', 'exceptional', 'specific-place', 'boon'],
      ['mystical-policy:boon-practice-ultra', 'ultra-rare', 'bounded-practice', 'boon'],
      ['mystical-policy:boon-relationship-rare', 'rare', 'relationship-condition', 'crew-support'],
      ['mystical-policy:curse-practice-rare', 'rare', 'bounded-practice', 'curse'],
      ['mystical-policy:relic-material-scarce', 'scarce', 'material-object', 'relic'],
      ['mystical-policy:totem-material-rare', 'rare', 'material-object', 'totem']
    ])
    expect(input).toEqual(before)
  })

  it('covers every permitted rarity and grounded source class with finite availability, conditions, costs, and audit evidence', () => {
    const input = request()
    const audit = auditMysticalEffectPolicies(input)

    expect(MYSTICAL_EFFECT_RARITIES).toEqual(['scarce', 'rare', 'exceptional', 'ultra-rare'])
    expect(new Set(audit.reviewed.map(item => item.rarity))).toEqual(new Set(MYSTICAL_EFFECT_RARITIES))
    expect(new Set(audit.reviewed.map(item => item.sourceClass))).toEqual(new Set(MYSTICAL_EFFECT_SOURCE_CLASSES))
    expect(audit.reviewed.every(item => item.availability.maximumDiscoveries > 0 && item.availability.maximumConcurrentEligibility > 0 && item.availability.maximumConcurrentEligibility <= item.availability.maximumDiscoveries && item.conditionIds.length > 0 && item.costIds.length > 0 && item.auditEvidenceIds.length >= 4 && item.status === 'policy-audited-only')).toBe(true)
    expect(audit.reviewed.find(item => item.id === 'mystical-policy:boon-practice-ultra')?.futureReservation).toEqual({ kind: 'future-jomon-loss-safeguard', status: 'deferred-no-authority' })
  })

  it('requires existing effect semantics, known facts, finite conditions, source provenance, cost/trade-off, and adverse counterplay', () => {
    const missingCounterplay = structuredClone(request())
    const curse = missingCounterplay.definitions.find(definition => definition.form === 'curse')!
    curse.auditEvidence = curse.auditEvidence.filter(evidence => evidence.kind !== 'adverse-counterplay')
    const staleCondition = structuredClone(request())
    staleCondition.definitions[0]!.conditions[0]!.factRevision = 2
    const ungrounded = structuredClone(request())
    ungrounded.definitions[0]!.source.factId = 'fact:object:not-known'
    const free = structuredClone(request())
    free.definitions[0]!.costs = []
    const unlimited = structuredClone(request())
    unlimited.definitions[0]!.availability.maximumDiscoveries = 99
    const unlimitedUse = structuredClone(request())
    unlimitedUse.definitions[0]!.useBoundary = { kind: 'limited-use', maximumUses: 99 }
    const contradictorySource = structuredClone(request())
    contradictorySource.definitions[0]!.source.sourceClass = 'material-object'
    const mismatchedCategory = structuredClone(request())
    const relic = mismatchedCategory.definitions.find(definition => definition.form === 'relic')!
    relic.effect.category = 'boon'
    const unauditable = structuredClone(request())
    unauditable.definitions[0]!.auditEvidence = unauditable.definitions[0]!.auditEvidence.filter(evidence => evidence.kind !== 'availability-bound')

    expect(codes(missingCounterplay)).toContain('mystical-effect-policy.missing-adverse-counterplay')
    expect(codes(staleCondition)).toContain('mystical-effect-policy.stale-condition-fact')
    expect(codes(ungrounded)).toEqual(expect.arrayContaining(['mystical-effect-policy.ungrounded-source', 'mystical-effect-policy.source-provenance-mismatch']))
    expect(codes(free)).toEqual(expect.arrayContaining(['mystical-effect-policy.cost-limit', 'mystical-effect-policy.free-or-no-trade-off']))
    expect(codes(unlimited)).toContain('mystical-effect-policy.unbounded-availability')
    expect(codes(unlimitedUse)).toContain('mystical-effect-policy.unbounded-use-boundary')
    expect(codes(contradictorySource)).toContain('mystical-effect-policy.invalid-source-class-combination')
    expect(codes(mismatchedCategory)).toContain('mystical-effect-policy.ineligible-effect-category')
    expect(codes(unauditable)).toContain('mystical-effect-policy.missing-audit-evidence')
  })

  it('fails closed for common, malformed, duplicate, noncanonical, unsafe, ungrounded, and extension-shaped definitions', () => {
    const common = structuredClone(request()) as unknown as { definitions: Array<{ rarity: string }> }
    common.definitions[0]!.rarity = 'common'
    const duplicate = structuredClone(request())
    duplicate.definitions[1]!.id = duplicate.definitions[0]!.id
    const noncanonical = structuredClone(request())
    noncanonical.definitions = [...noncanonical.definitions].reverse()
    const unsafe = structuredClone(request())
    ;(unsafe.definitions[0]!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const genericMage = structuredClone(request()) as unknown as { definitions: Array<Record<string, unknown>> }
    genericMage.definitions[0]!.archetype = 'generic-mage-class'
    const arbitraryCasting = structuredClone(request()) as unknown as { definitions: Array<Record<string, unknown>> }
    arbitraryCasting.definitions[0]!.casting = 'unlimited'

    expect(codes({})).toEqual(['mystical-effect-policy.malformed-request'])
    expect(codes(common)).toContain('mystical-effect-policy.unknown-rarity')
    expect(codes(duplicate)).toContain('mystical-effect-policy.duplicate-definition-id')
    expect(codes(noncanonical)).toContain('mystical-effect-policy.noncanonical-definition-order')
    expect(codes(unsafe)).toContain('content-safety.prohibited.torture')
    expect(codes(genericMage)).toContain('mystical-effect-policy.malformed-definition')
    expect(codes(arbitraryCasting)).toContain('mystical-effect-policy.malformed-definition')
  })

  it('keeps ultra-rare reservations policy-only and excludes generic casting, revival, and Jomon-saving implementation authority', () => {
    const audit = auditMysticalEffectPolicies(request())
    const prohibited = new Set(MYSTICAL_EFFECT_FORBIDDEN_CAPABILITIES)
    const encoded = JSON.stringify(audit)
    const revival = structuredClone(request()) as unknown as { definitions: Array<Record<string, unknown>> }
    revival.definitions[1]!.outcome = 'courier-revival'
    const jomonSaving = structuredClone(request()) as unknown as { definitions: Array<Record<string, unknown>> }
    jomonSaving.definitions[1]!.outcome = 'jomon-rescue'

    expect(prohibited).toEqual(new Set([
      'generic-mage-class', 'spell-list', 'arbitrary-casting', 'unlimited-supernatural-power', 'courier-death-prevention',
      'courier-revival', 'jomon-rescue', 'jomon-collapse-prevention', 'terminal-loss-override'
    ]))
    expect(audit.reviewed.every(item => item.status === 'policy-audited-only')).toBe(true)
    expect(encoded).not.toContain('courier-revival')
    expect(encoded).not.toContain('jomon-rescue')
    expect(codes(revival)).toContain('mystical-effect-policy.malformed-definition')
    expect(codes(jomonSaving)).toContain('mystical-effect-policy.malformed-definition')
  })
})
