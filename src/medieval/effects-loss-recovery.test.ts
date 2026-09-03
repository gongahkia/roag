import { describe, expect, it, vi } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import {
  EFFECT_MODEL_CONTRACT_VERSION,
  resolveEffects,
  validateEffectResolutionRequest,
  type EffectCandidate,
  type EffectKnownFact,
  type EffectResolutionRequest,
  type EffectSourceCategory
} from './effects'
import {
  COURIER_LOSS_POLICY_VERSION,
  assessCourierLoss,
  type CourierLossAssessmentRequest,
  type CourierLossSafeguardReservation
} from './courier-loss-policy'
import {
  JOMON_INTEGRITY_POLICY_VERSION,
  assessJomonIntegrity,
  type JomonIntegrityAssessmentRequest,
  type JomonIntegrityCausalEvidence,
  type JomonIntegritySafeguardReservation
} from './jomon-integrity-policy'
import {
  MYSTICAL_EFFECT_POLICY_VERSION,
  MYSTICAL_EFFECT_RARITY_AVAILABILITY,
  auditMysticalEffectPolicies,
  type MysticalEffectUltraRareReservationKind
} from './mystical-effect-policy'
import type { PersistentPersonValidationContext } from './persistent-person'
import { advanceFoundationWorldTime, chooseInitialCourier, chronicleExport, createFoundationWorld, finalizeWorldAsChronicle } from './world'

const dataSafety = () => classifyMedievalContent('data', ['craft', 'navigation'], 'not-applicable')
const actionSafety = () => classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const lossSafety = () => classifyMedievalContent('event', ['ordinary-hardship'], 'adults-only')
const incidentSafety = () => classifyMedievalContent('event', ['environment', 'navigation'], 'not-applicable')

/** These are the complete caller-known facts for this cross-contract fixture. */
const knownFacts: readonly EffectKnownFact[] = [
  { id: 'fact:actor:courier', kind: 'actor', revision: 1 },
  { id: 'fact:actor:jomon', kind: 'actor', revision: 1 },
  { id: 'fact:condition:damage', kind: 'condition', revision: 1 },
  { id: 'fact:crew:carpenter', kind: 'crew-support', revision: 1 },
  { id: 'fact:enemy:reef-hound', kind: 'enemy', revision: 1 },
  { id: 'fact:environment:storm', kind: 'environment', revision: 1 },
  { id: 'fact:equipment:lash', kind: 'equipment', revision: 1 },
  { id: 'fact:equipment:ward', kind: 'equipment', revision: 1 },
  { id: 'fact:object:courier-ward', kind: 'grounded-object', revision: 1 },
  { id: 'fact:object:jomon-ward', kind: 'grounded-object', revision: 1 },
  { id: 'fact:practice:watch', kind: 'grounded-practice', revision: 1 },
  { id: 'fact:preparation:line', kind: 'preparation', revision: 1 }
]

const sourceFor: Readonly<Record<EffectSourceCategory, { factId: string; grounding: EffectCandidate['source']['grounding'] }>> = {
  health: { factId: 'fact:condition:damage', grounding: 'ordinary-condition' },
  injury: { factId: 'fact:condition:damage', grounding: 'ordinary-condition' },
  exhaustion: { factId: 'fact:condition:damage', grounding: 'ordinary-condition' },
  preparation: { factId: 'fact:preparation:line', grounding: 'place-bound-practice' },
  equipment: { factId: 'fact:equipment:ward', grounding: 'material-object' },
  relic: { factId: 'fact:object:jomon-ward', grounding: 'material-object' },
  totem: { factId: 'fact:object:jomon-ward', grounding: 'material-object' },
  boon: { factId: 'fact:object:jomon-ward', grounding: 'material-object' },
  curse: { factId: 'fact:practice:watch', grounding: 'place-bound-practice' },
  'crew-support': { factId: 'fact:crew:carpenter', grounding: 'crew-relationship' },
  'enemy-weakness': { factId: 'fact:enemy:reef-hound', grounding: 'ordinary-condition' },
  'environmental-interaction': { factId: 'fact:environment:storm', grounding: 'environmental-condition' }
}

const targetFor = (category: EffectSourceCategory): EffectCandidate['target'] => category === 'enemy-weakness'
  ? { factId: 'fact:enemy:reef-hound', factRevision: 1, scope: 'known-enemy' }
  : category === 'environmental-interaction'
    ? { factId: 'fact:environment:storm', factRevision: 1, scope: 'known-environment' }
    : { factId: 'fact:actor:courier', factRevision: 1, scope: category === 'crew-support' ? 'known-ally' : 'self' }

const effect = (name: string, category: EffectSourceCategory, overrides: Partial<EffectCandidate> = {}): EffectCandidate => {
  const source = sourceFor[category]
  return {
    version: EFFECT_MODEL_CONTRACT_VERSION,
    id: `effect:${name}`,
    category,
    polarity: category === 'curse' ? 'hindrance' : 'aid',
    magnitude: 1,
    priority: 5,
    source: { factId: source.factId, factRevision: 1, grounding: source.grounding },
    target: targetFor(category),
    evidence: [{ id: `evidence:${name}`, factId: source.factId, factRevision: 1 }],
    duration: { startsAtWorldTime: 0, expiresAtWorldTime: 10 },
    stacking: { groupId: `stack:${name}`, mode: 'additive', cap: 4 },
    conflictsWith: [],
    chainAfter: [],
    counterplay: { id: `counterplay:${name}`, factId: 'fact:equipment:ward', factRevision: 1 },
    contentSafety: dataSafety(),
    ...overrides
  }
}

const effectRequest = (effects: readonly EffectCandidate[], worldTime = 5, overrides: Partial<EffectResolutionRequest> = {}): EffectResolutionRequest => ({
  version: EFFECT_MODEL_CONTRACT_VERSION,
  actionTime: { timeUnit: 'minute', worldTime },
  knownFacts,
  effects,
  appliedCounterplayIds: [],
  ...overrides
})

const selectedWorld = (seed: string) => advanceFoundationWorldTime(
  chooseInitialCourier(createFoundationWorld({ seed }), 'crew:0'),
  { id: `wait:${seed}`, kind: 'wait', durationMinutes: 1, contentSafety: actionSafety() }
)

const peopleContextFor = (world: ReturnType<typeof selectedWorld>): PersistentPersonValidationContext => ({
  seed: world.manifest.creation.seed,
  configurationFingerprint: world.manifest.creation.configurationFingerprint,
  initialWorld: world.initialWorld,
  frontier: world.state.geography.frontier,
  jomon: world.jomon,
  crew: world.crew,
  siteIds: world.state.sites.sites.map(site => site.id),
  worldTime: world.state.temporal.worldTime
})

const auditedSafeguard = (
  name: 'courier-ward' | 'jomon-ward',
  reservation: MysticalEffectUltraRareReservationKind,
  targetFactId: 'fact:actor:courier' | 'fact:actor:jomon',
  worldTime: number
) => {
  const sourceFactId = `fact:object:${name}` as const
  const availability = MYSTICAL_EFFECT_RARITY_AVAILABILITY['ultra-rare']
  const audit = auditMysticalEffectPolicies({
    version: MYSTICAL_EFFECT_POLICY_VERSION,
    actionTime: { timeUnit: 'minute', worldTime },
    knownFacts,
    definitions: [{
      version: MYSTICAL_EFFECT_POLICY_VERSION,
      id: `mystical-policy:${name}`,
      form: 'relic',
      rarity: 'ultra-rare',
      source: { sourceClass: 'material-object', factId: sourceFactId, factRevision: 1 },
      conditions: [{ id: `condition:${name}`, kind: 'object-available', factId: sourceFactId, factRevision: 1 }],
      availability: { scope: 'per-world', maximumDiscoveries: availability.maximumDiscoveriesPerWorld, maximumConcurrentEligibility: availability.maximumConcurrentEligibility },
      useBoundary: { kind: 'limited-use', maximumUses: 1 },
      costs: [{ id: `cost:${name}`, kind: 'material-expenditure', factId: 'fact:condition:damage', factRevision: 1 }],
      auditEvidence: [
        { id: `audit:${name}:availability`, kind: 'availability-bound', factId: sourceFactId, factRevision: 1 },
        { id: `audit:${name}:condition`, kind: 'eligibility-condition', factId: sourceFactId, factRevision: 1 },
        { id: `audit:${name}:cost`, kind: 'cost-trade-off', factId: 'fact:condition:damage', factRevision: 1 },
        { id: `audit:${name}:source`, kind: 'source-provenance', factId: sourceFactId, factRevision: 1 }
      ],
      effect: effect(`safeguard:${name}`, 'relic', {
        source: { factId: sourceFactId, factRevision: 1, grounding: 'material-object' },
        target: { factId: targetFactId, factRevision: 1, scope: 'self' },
        stacking: { groupId: `stack:safeguard:${name}`, mode: 'exclusive', cap: 1 }
      }),
      contentSafety: dataSafety(),
      futureReservation: { kind: reservation, status: 'deferred-no-authority' }
    }]
  })
  return audit.reviewed[0]!
}

const courierLossRequest = (world: ReturnType<typeof selectedWorld>): CourierLossAssessmentRequest => {
  const people = structuredClone(world.state.people.records)
  const courier = people.find(person => person.id === 'crew:0')
  if (!courier) throw new Error('fixture requires the selected courier')
  courier.life = { ...courier.life, status: 'dead', death: { atWorldTime: world.state.temporal.worldTime } }
  courier.work = { ...courier.work, availability: 'unavailable', current: { status: 'idle' } }
  const reservation: CourierLossSafeguardReservation = {
    kind: 'prevention',
    mysticalPolicyVersion: MYSTICAL_EFFECT_POLICY_VERSION,
    source: { factId: 'fact:object:courier-ward', factRevision: 1 },
    policyRecord: auditedSafeguard('courier-ward', 'future-courier-loss-safeguard', 'fact:actor:courier', world.state.temporal.worldTime)
  }
  return {
    version: COURIER_LOSS_POLICY_VERSION,
    actionTime: { timeUnit: 'minute', worldTime: world.state.temporal.worldTime },
    peopleContext: peopleContextFor(world),
    people,
    loss: {
      id: 'courier-loss:crew-0:cross-contract',
      kind: 'confirmed-courier-loss',
      courierId: 'crew:0',
      atWorldTime: world.state.temporal.worldTime,
      evidenceIds: ['loss-evidence:crew-0:cross-contract'],
      contentSafety: lossSafety()
    },
    knownFacts,
    safeguardReservations: [reservation]
  }
}

const partialEvidence = (): readonly JomonIntegrityCausalEvidence[] => [
  { id: 'jomon-evidence:00-cause', kind: 'grounded-cause', factId: 'fact:environment:storm', factRevision: 1 },
  { id: 'jomon-evidence:01-labour', kind: 'labour-cost', factId: 'fact:crew:carpenter', factRevision: 1, afterEvidenceId: 'jomon-evidence:00-cause' },
  { id: 'jomon-evidence:02-trade-off', kind: 'recovery-trade-off', factId: 'fact:condition:damage', factRevision: 1, afterEvidenceId: 'jomon-evidence:01-labour' },
  { id: 'jomon-evidence:03-counterplay', kind: 'adverse-counterplay', factId: 'fact:equipment:lash', factRevision: 1, afterEvidenceId: 'jomon-evidence:02-trade-off' }
]

const terminalEvidence = (): readonly JomonIntegrityCausalEvidence[] => [
  { id: 'jomon-evidence:00-cause', kind: 'grounded-cause', factId: 'fact:environment:storm', factRevision: 1 },
  { id: 'jomon-evidence:01-collapse', kind: 'collapse-evidence', factId: 'fact:condition:damage', factRevision: 1, afterEvidenceId: 'jomon-evidence:00-cause' },
  { id: 'jomon-evidence:02-trade-off', kind: 'recovery-trade-off', factId: 'fact:condition:damage', factRevision: 1, afterEvidenceId: 'jomon-evidence:01-collapse' },
  { id: 'jomon-evidence:03-counterplay', kind: 'adverse-counterplay', factId: 'fact:equipment:lash', factRevision: 1, afterEvidenceId: 'jomon-evidence:02-trade-off' }
]

const jomonRequest = (world: ReturnType<typeof selectedWorld>, terminal: boolean): JomonIntegrityAssessmentRequest => {
  const causalEvidence = terminal ? terminalEvidence() : partialEvidence()
  const reservation: JomonIntegritySafeguardReservation = {
    mysticalPolicyVersion: MYSTICAL_EFFECT_POLICY_VERSION,
    source: { factId: 'fact:object:jomon-ward', factRevision: 1 },
    policyRecord: auditedSafeguard('jomon-ward', 'future-jomon-loss-safeguard', 'fact:actor:jomon', world.state.temporal.worldTime),
    causalEvidenceIds: causalEvidence.map(item => item.id),
    adverseCounterplayEvidenceId: 'jomon-evidence:03-counterplay',
    recoveryTradeOffEvidenceId: 'jomon-evidence:02-trade-off'
  }
  const jomon = structuredClone(world.state.jomon)
  jomon.integrity.current = terminal ? 0 : 49
  return {
    version: JOMON_INTEGRITY_POLICY_VERSION,
    actionTime: { timeUnit: 'minute', worldTime: world.state.temporal.worldTime },
    jomon,
    knownFacts,
    incident: { id: terminal ? 'jomon-incident:terminal-cross-contract' : 'jomon-incident:partial-cross-contract', kind: terminal ? 'terminal-collapse' : 'partial-disaster', contentSafety: incidentSafety() },
    causalEvidence,
    safeguardReservation: reservation
  }
}

describe('deterministic effects, loss, finalization, and recovery handoffs', () => {
  it('resolves representative legal combinations in canonical order with caps, conflicts, chains, counterplay, expiry, and no ambient dependencies', () => {
    const root = effect('foxtrot-chain-root', 'relic', { priority: 1 })
    const child = effect('golf-chain-child', 'equipment', { priority: 9, chainAfter: [root.id] })
    const effects = [
      child,
      effect('hotel-countered-curse', 'curse', { priority: 5 }),
      effect('delta-conflict-curse', 'curse', { priority: 2, conflictsWith: ['effect:echo-conflict-curse'] }),
      effect('charlie-capped-health', 'health', { priority: 7, stacking: { groupId: 'stack:readiness', mode: 'additive', cap: 2 } }),
      root,
      effect('echo-conflict-curse', 'curse', { priority: 8, conflictsWith: ['effect:delta-conflict-curse'] }),
      effect('bravo-readiness-equipment', 'equipment', { priority: 8, stacking: { groupId: 'stack:readiness', mode: 'additive', cap: 2 } }),
      effect('alpha-readiness-preparation', 'preparation', { priority: 9, stacking: { groupId: 'stack:readiness', mode: 'additive', cap: 2 } })
    ]
    const input = effectRequest(effects, 5, { appliedCounterplayIds: ['counterplay:hotel-countered-curse'] })
    const before = structuredClone(input)
    const first = resolveEffects(input)
    const repeated = resolveEffects(effectRequest([...effects].reverse(), 5, {
      knownFacts: [...knownFacts].reverse(),
      appliedCounterplayIds: ['counterplay:hotel-countered-curse']
    }))

    expect(first).toEqual(repeated)
    expect(first.active.map(item => item.effectId)).toEqual(expect.arrayContaining([
      'effect:alpha-readiness-preparation',
      'effect:bravo-readiness-equipment',
      'effect:echo-conflict-curse',
      root.id,
      child.id
    ]))
    expect(first.suppressed).toEqual(expect.arrayContaining([
      { effectId: 'effect:charlie-capped-health', reason: 'stack-cap-reached', relatedId: 'stack:readiness' },
      { effectId: 'effect:delta-conflict-curse', reason: 'conflict-lost', relatedId: 'effect:echo-conflict-curse' },
      { effectId: 'effect:hotel-countered-curse', reason: 'countered-by-known-response', relatedId: 'counterplay:hotel-countered-curse' }
    ]))
    expect(first.groups.find(group => group.groupId === 'stack:readiness')).toMatchObject({
      totalMagnitude: 2,
      effectIds: ['effect:alpha-readiness-preparation', 'effect:bravo-readiness-equipment']
    })
    expect(first.active.map(item => item.effectId).indexOf(root.id)).toBeLessThan(first.active.map(item => item.effectId).indexOf(child.id))
    expect(input).toEqual(before)

    const timed = effect('timed-expiry', 'preparation', { duration: { startsAtWorldTime: 5, expiresAtWorldTime: 6 } })
    expect(resolveEffects(effectRequest([timed], 5)).active.map(item => item.effectId)).toEqual([timed.id])
    expect(resolveEffects(effectRequest([timed], 6)).suppressed).toEqual([{ effectId: timed.id, reason: 'expired-at-action-time' }])

    const cycleLeft = effect('cycle-alpha', 'preparation', { chainAfter: ['effect:cycle-beta'] })
    const cycleRight = effect('cycle-beta', 'equipment', { chainAfter: ['effect:cycle-alpha'] })
    expect(validateEffectResolutionRequest(effectRequest([cycleLeft, cycleRight])).diagnostics.map(item => item.code)).toContain('effects.cyclic-chain')

    const now = vi.spyOn(Date, 'now').mockImplementation(() => { throw new Error('wall clock must not be read') })
    const random = vi.spyOn(Math, 'random').mockImplementation(() => { throw new Error('unseeded randomness must not be read') })
    const locale = vi.spyOn(String.prototype, 'localeCompare').mockImplementation(() => { throw new Error('locale ordering must not be read') })
    let ambientFree
    try {
      ambientFree = resolveEffects(effectRequest([effect('ordinal-a', 'health'), effect('ordinal-z', 'injury')]))
    } finally {
      locale.mockRestore()
      random.mockRestore()
      now.mockRestore()
    }
    expect(ambientFree.active.map(item => item.effectId)).toEqual(['effect:ordinal-a', 'effect:ordinal-z'])
  })

  it('keeps courier loss permanent and intent-only, including valid future reservations and crew extinction', () => {
    const world = selectedWorld('effects-loss-courier')
    const worldBefore = structuredClone(world)
    const request = courierLossRequest(world)
    const requestBefore = structuredClone(request)
    const assessment = assessCourierLoss(request)

    expect(assessment.confirmedLoss.outcome).toBe('permanent-loss-required')
    expect(assessment.safeguards).toEqual(expect.arrayContaining([
      { kind: 'prevention', status: 'unavailable', reason: 'future-ultra-rare-reservation-only', policyRecordId: 'mystical-policy:courier-ward' },
      { kind: 'revival', status: 'unavailable', reason: 'no-current-safeguard-authority' }
    ]))
    expect(assessment.finalization).toEqual({ kind: 'none' })
    expect(assessment).not.toHaveProperty('selectedSuccessor')
    expect(assessment).not.toHaveProperty('replacement')
    expect(assessment).not.toHaveProperty('history')
    expect(assessment).not.toHaveProperty('possessions')
    expect(request).toEqual(requestBefore)
    expect(world).toEqual(worldBefore)

    const extinction = courierLossRequest(world)
    for (const person of extinction.people) {
      if (person.id === 'crew:0') continue
      person.life = { ...person.life, status: 'dead', death: { atWorldTime: extinction.actionTime.worldTime } }
      person.work = { ...person.work, availability: 'unavailable', current: { status: 'idle' } }
    }
    const extinctionAssessment = assessCourierLoss(extinction)
    expect(extinctionAssessment.eligibleLivingCrew).toEqual([])
    expect(extinctionAssessment.finalization).toEqual({
      kind: 'read-only-chronicle-finalization-intent',
      reason: 'crew-extinction',
      path: 'existing-read-only-chronicle-path'
    })
    expect(extinctionAssessment.finalization).not.toHaveProperty('world')
    expect(world).toEqual(worldBefore)
  })

  it('keeps partial disaster and terminal-collapse assessments deterministic, isolated, and unable to use a deferred Jomon safeguard', () => {
    const world = selectedWorld('effects-loss-jomon')
    const worldBefore = structuredClone(world)
    const partial = jomonRequest(world, false)
    const partialBefore = structuredClone(partial)
    const partialAssessment = assessJomonIntegrity(partial)
    const terminal = jomonRequest(world, true)
    const terminalBefore = structuredClone(terminal)
    const terminalAssessment = assessJomonIntegrity(terminal)

    expect(partialAssessment).toEqual(assessJomonIntegrity(structuredClone(partial)))
    expect(partialAssessment).toMatchObject({
      integrity: { band: 'critical' },
      repair: { kind: 'future-repair-required' },
      rescue: { kind: 'future-non-mystical-rescue-required' },
      terminalLoss: 'not-terminal',
      finalization: { kind: 'none' }
    })
    expect(terminalAssessment).toMatchObject({
      integrity: { band: 'collapsed' },
      collapse: { status: 'confirmed-collapse' },
      terminalLoss: 'confirmed-jomon-loss',
      finalization: { kind: 'read-only-chronicle-finalization-intent', reason: 'jomon-loss' },
      safeguard: { status: 'unavailable', reason: 'future-ultra-rare-reservation-only' }
    })
    expect(partial).toEqual(partialBefore)
    expect(terminal).toEqual(terminalBefore)
    expect(world).toEqual(worldBefore)
  })

  it('requires an explicit owner handoff from a valid Jomon-loss intent to a read-only deterministic chronicle export', () => {
    const world = selectedWorld('effects-loss-finalization')
    const before = structuredClone(world)
    const assessment = assessJomonIntegrity(jomonRequest(world, true))
    if (assessment.finalization.kind !== 'read-only-chronicle-finalization-intent') throw new Error('fixture requires a valid Jomon-loss intent')

    // The policy did not alter the valid active fixture. This explicit call is
    // the existing world-owner handoff; no policy-to-storage orchestration exists.
    expect(world).toEqual(before)
    const chronicle = finalizeWorldAsChronicle(world, assessment.finalization.reason)
    const exported = chronicleExport(chronicle)

    expect(chronicle).toMatchObject({
      version: 12,
      id: `chronicle:${world.id}`,
      status: 'finalized',
      reason: 'jomon-loss',
      world: before
    })
    expect(JSON.parse(exported)).toEqual(chronicle)
    expect(chronicleExport(finalizeWorldAsChronicle(world, 'jomon-loss'))).toBe(exported)
    chronicle.world.state.jomon.integrity.current = 1
    expect(world).toEqual(before)
  })
})
