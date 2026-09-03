import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import {
  EFFECT_MODEL_CONTRACT_VERSION,
  EFFECT_MODEL_LIMITS,
  EFFECT_SOURCE_CATEGORIES,
  resolveEffects,
  validateEffectResolutionRequest,
  type EffectCandidate,
  type EffectKnownFact,
  type EffectResolutionRequest,
  type EffectSourceCategory
} from './effects'

const safety = () => classifyMedievalContent('data', ['craft', 'navigation'], 'not-applicable')

const knownFacts: readonly EffectKnownFact[] = [
  { id: 'fact:actor:courier', kind: 'actor', revision: 1 },
  { id: 'fact:condition:courier', kind: 'condition', revision: 1 },
  { id: 'fact:crew:mate', kind: 'crew-support', revision: 1 },
  { id: 'fact:enemy:reef-hound', kind: 'enemy', revision: 1 },
  { id: 'fact:environment:shoal', kind: 'environment', revision: 1 },
  { id: 'fact:equipment:ward', kind: 'equipment', revision: 1 },
  { id: 'fact:object:relic', kind: 'grounded-object', revision: 1 },
  { id: 'fact:practice:watch', kind: 'grounded-practice', revision: 1 },
  { id: 'fact:preparation:line', kind: 'preparation', revision: 1 }
]

const sourceFor: Readonly<Record<EffectSourceCategory, { factId: string; grounding: EffectCandidate['source']['grounding'] }>> = {
  health: { factId: 'fact:condition:courier', grounding: 'ordinary-condition' },
  injury: { factId: 'fact:condition:courier', grounding: 'ordinary-condition' },
  exhaustion: { factId: 'fact:condition:courier', grounding: 'ordinary-condition' },
  preparation: { factId: 'fact:preparation:line', grounding: 'place-bound-practice' },
  equipment: { factId: 'fact:equipment:ward', grounding: 'material-object' },
  relic: { factId: 'fact:object:relic', grounding: 'material-object' },
  totem: { factId: 'fact:object:relic', grounding: 'material-object' },
  boon: { factId: 'fact:object:relic', grounding: 'material-object' },
  curse: { factId: 'fact:practice:watch', grounding: 'place-bound-practice' },
  'crew-support': { factId: 'fact:crew:mate', grounding: 'crew-relationship' },
  'enemy-weakness': { factId: 'fact:enemy:reef-hound', grounding: 'ordinary-condition' },
  'environmental-interaction': { factId: 'fact:environment:shoal', grounding: 'environmental-condition' }
}

const targetFor = (category: EffectSourceCategory): EffectCandidate['target'] => category === 'enemy-weakness'
  ? { factId: 'fact:enemy:reef-hound', factRevision: 1, scope: 'known-enemy' }
  : category === 'environmental-interaction'
    ? { factId: 'fact:environment:shoal', factRevision: 1, scope: 'known-environment' }
    : { factId: 'fact:actor:courier', factRevision: 1, scope: category === 'crew-support' ? 'known-ally' : 'self' }

const effect = (suffix: string, category: EffectSourceCategory, overrides: Partial<EffectCandidate> = {}): EffectCandidate => {
  const source = sourceFor[category]
  return {
    version: EFFECT_MODEL_CONTRACT_VERSION,
    id: `effect:${suffix}`,
    category,
    polarity: category === 'curse' ? 'hindrance' : 'aid',
    magnitude: 1,
    priority: 5,
    source: { factId: source.factId, factRevision: 1, grounding: source.grounding },
    target: targetFor(category),
    evidence: [{ id: `evidence:${suffix}`, factId: source.factId, factRevision: 1 }],
    duration: { startsAtWorldTime: 0, expiresAtWorldTime: 20 },
    stacking: { groupId: `stack:${suffix}`, mode: 'additive', cap: 4 },
    conflictsWith: [],
    chainAfter: [],
    counterplay: { id: 'counterplay:ward', factId: 'fact:equipment:ward', factRevision: 1 },
    contentSafety: safety(),
    ...overrides
  }
}

const request = (effects: readonly EffectCandidate[], worldTime = 5, overrides: Partial<EffectResolutionRequest> = {}): EffectResolutionRequest => ({
  version: EFFECT_MODEL_CONTRACT_VERSION,
  actionTime: { timeUnit: 'minute', worldTime },
  knownFacts,
  effects,
  appliedCounterplayIds: [],
  ...overrides
})

const codes = (value: unknown): readonly string[] => validateEffectResolutionRequest(value).diagnostics.map(diagnostic => diagnostic.code)

describe('renderer-independent v1 effect model', () => {
  it('resolves every required semantic category in one canonical, reproducible order regardless of caller set order', () => {
    const allCategories = EFFECT_SOURCE_CATEGORIES.map(category => effect(`category:${category}`, category))
    const firstInput = request(allCategories)
    const secondInput = request([...allCategories].reverse(), 5, { knownFacts: [...knownFacts].reverse() })

    const first = resolveEffects(firstInput)
    const second = resolveEffects(secondInput)

    expect(validateEffectResolutionRequest(firstInput)).toMatchObject({ status: 'accepted', diagnostics: [] })
    expect(second).toEqual(first)
    expect(first.active.map(item => item.category).sort()).toEqual([...EFFECT_SOURCE_CATEGORIES].sort())
    expect(first.active.map(item => item.effectId)).toEqual([...first.active.map(item => item.effectId)].sort())
    expect(first.groups).toHaveLength(EFFECT_SOURCE_CATEGORIES.length)
  })

  it('uses only canonical action-world minutes for duration, expiry, and repeatable zero-time resolution', () => {
    const timed = effect('timed:preparation', 'preparation', { duration: { startsAtWorldTime: 5, expiresAtWorldTime: 10 } })
    const beforeStart = resolveEffects(request([timed], 4))
    const active = resolveEffects(request([timed], 5))
    const beforeExpiry = resolveEffects(request([timed], 9))
    const expired = resolveEffects(request([timed], 10))
    const input = request([timed], 5)
    const before = structuredClone(input)

    expect(beforeStart.suppressed).toEqual([{ effectId: 'effect:timed:preparation', reason: 'not-started-at-action-time' }])
    expect(active.active.map(item => item.effectId)).toEqual(['effect:timed:preparation'])
    expect(beforeExpiry).toEqual(resolveEffects(request([timed], 9)))
    expect(expired.suppressed).toEqual([{ effectId: 'effect:timed:preparation', reason: 'expired-at-action-time' }])
    expect(resolveEffects(input)).toEqual(resolveEffects(structuredClone(input)))
    expect(input).toEqual(before)

    const wallClock = structuredClone(input) as unknown as { actionTime: Record<string, unknown> }
    wallClock.actionTime.wallClockMilliseconds = 999
    expect(codes(wallClock)).toEqual(expect.arrayContaining(['effects.wall-clock-input', 'effects.malformed-action-time']))
  })

  it('applies bounded additive caps, deterministic conflicts, suppression, and topologically ordered chains', () => {
    const capped = [
      effect('stack:one', 'health', { priority: 9, stacking: { groupId: 'stack:health', mode: 'additive', cap: 2 } }),
      effect('stack:two', 'injury', { priority: 8, stacking: { groupId: 'stack:health', mode: 'additive', cap: 2 } }),
      effect('stack:three', 'exhaustion', { priority: 7, stacking: { groupId: 'stack:health', mode: 'additive', cap: 2 } })
    ]
    const conflictLeft = effect('conflict:left', 'curse', {
      priority: 2,
      stacking: { groupId: 'stack:curse', mode: 'exclusive', cap: 1 },
      conflictsWith: ['effect:conflict:right']
    })
    const conflictRight = effect('conflict:right', 'curse', {
      priority: 8,
      stacking: { groupId: 'stack:curse', mode: 'exclusive', cap: 1 },
      conflictsWith: ['effect:conflict:left']
    })
    const root = effect('chain:root', 'preparation', {
      priority: 1,
      counterplay: { id: 'counterplay:root', factId: 'fact:equipment:ward', factRevision: 1 }
    })
    const child = effect('chain:child', 'equipment', { priority: 9, chainAfter: ['effect:chain:root'] })

    const resolved = resolveEffects(request([child, conflictLeft, ...capped, root, conflictRight]))
    const countered = resolveEffects(request([root, child], 5, { appliedCounterplayIds: ['counterplay:root'] }))

    expect(resolved.active.map(item => item.effectId)).toEqual(expect.arrayContaining([
      'effect:stack:one',
      'effect:stack:two',
      'effect:conflict:right',
      'effect:chain:root',
      'effect:chain:child'
    ]))
    expect(resolved.suppressed).toEqual(expect.arrayContaining([
      { effectId: 'effect:stack:three', reason: 'stack-cap-reached', relatedId: 'stack:health' },
      { effectId: 'effect:conflict:left', reason: 'conflict-lost', relatedId: 'effect:conflict:right' }
    ]))
    expect(resolved.groups.find(group => group.groupId === 'stack:health')).toMatchObject({ totalMagnitude: 2, effectIds: ['effect:stack:one', 'effect:stack:two'] })
    expect(resolved.active.map(item => item.effectId).indexOf('effect:chain:root')).toBeLessThan(resolved.active.map(item => item.effectId).indexOf('effect:chain:child'))
    expect(countered.suppressed).toEqual(expect.arrayContaining([
      { effectId: 'effect:chain:root', reason: 'countered-by-known-response', relatedId: 'counterplay:root' },
      { effectId: 'effect:chain:child', reason: 'chain-prerequisite-suppressed', relatedId: 'effect:chain:root' }
    ]))
  })

  it('fails closed for malformed, unsafe, stale, ineligible, duplicate, noncanonical, cyclic, and unknown input', () => {
    const valid = effect('validation:valid', 'health')
    const unsafe = structuredClone(valid)
    ;(unsafe.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const stale = structuredClone(valid)
    stale.source.factRevision = 2
    const ineligible = structuredClone(valid)
    ineligible.target = { factId: 'fact:enemy:reef-hound', factRevision: 1, scope: 'known-ally' }
    const ineligibleSource = structuredClone(valid)
    ineligibleSource.source = { factId: 'fact:actor:courier', factRevision: 1, grounding: 'ordinary-condition' }
    const ineligibleCounterplay = structuredClone(valid)
    ineligibleCounterplay.counterplay = { id: 'counterplay:invalid', factId: 'fact:actor:courier', factRevision: 1 }
    const duplicate = request([valid, structuredClone(valid)])
    const noncanonical = structuredClone(valid)
    noncanonical.evidence = [
      { id: 'evidence:validation:z', factId: 'fact:condition:courier', factRevision: 1 },
      { id: 'evidence:validation:a', factId: 'fact:condition:courier', factRevision: 1 }
    ]
    const cycleLeft = effect('cycle:left', 'preparation', { chainAfter: ['effect:cycle:right'] })
    const cycleRight = effect('cycle:right', 'equipment', { chainAfter: ['effect:cycle:left'] })
    const tooDeep = Array.from({ length: EFFECT_MODEL_LIMITS.chainDepth + 2 }, (_, index) => effect(`depth:n${index}`, index === 0 ? 'preparation' : 'equipment', {
      chainAfter: index === 0 ? [] : [`effect:depth:n${index - 1}`]
    }))

    expect(codes({})).toEqual(['effects.malformed-request'])
    expect(codes(request([unsafe]))).toContain('content-safety.prohibited.torture')
    expect(codes(request([stale]))).toContain('effects.stale-fact')
    expect(codes(request([ineligible]))).toContain('effects.ineligible-target')
    expect(codes(request([ineligibleSource]))).toContain('effects.ineligible-source')
    expect(codes(request([ineligibleCounterplay]))).toContain('effects.ineligible-counterplay')
    expect(codes(duplicate)).toContain('effects.duplicate-effect-id')
    expect(codes(request([noncanonical]))).toContain('effects.noncanonical-evidence-order')
    expect(codes(request([cycleLeft, cycleRight]))).toContain('effects.cyclic-chain')
    expect(codes(request(tooDeep))).toContain('effects.chain-depth-limit')
    expect(codes(request([valid], 5, { appliedCounterplayIds: ['counterplay:unknown'] }))).toContain('effects.unknown-counterplay-selection')
    expect(codes(request(Array.from({ length: EFFECT_MODEL_LIMITS.effects + 1 }, (_, index) => effect(`limit:${index}`, 'health'))))).toContain('effects.effect-limit')
  })

  it('requires known source/target/counterplay facts and returns provenance without leaking facts or mutating inputs', () => {
    const supported = effect('provenance:crew', 'crew-support', {
      target: { factId: 'fact:actor:courier', factRevision: 1, scope: 'known-ally' },
      counterplay: { id: 'counterplay:crew', factId: 'fact:equipment:ward', factRevision: 1 }
    })
    const input = request([supported], 5, { appliedCounterplayIds: ['counterplay:crew'] })
    const before = structuredClone(input)
    const resolved = resolveEffects(input)
    const knownIds = new Set(input.knownFacts.map(fact => fact.id))
    const outputFactIds = [
      ...resolved.active.flatMap(item => [item.sourceFactId, item.targetFactId]),
      ...resolved.explanations.flatMap(item => [item.sourceFactId, item.targetFactId])
    ]

    expect(validateEffectResolutionRequest(input)).toMatchObject({ status: 'accepted', diagnostics: [] })
    expect(resolved.suppressed).toEqual([{ effectId: 'effect:provenance:crew', reason: 'countered-by-known-response', relatedId: 'counterplay:crew' }])
    expect(resolved.explanations).toEqual([expect.objectContaining({
      effectId: 'effect:provenance:crew',
      sourceFactId: 'fact:crew:mate',
      targetFactId: 'fact:actor:courier',
      evidenceIds: ['evidence:provenance:crew'],
      counterplayId: 'counterplay:crew'
    })])
    expect(outputFactIds.every(id => knownIds.has(id))).toBe(true)
    expect(input).toEqual(before)
  })
})
