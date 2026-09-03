import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { EFFECT_MODEL_CONTRACT_VERSION, type EffectCandidate, type EffectKnownFact } from './effects'
import {
  JOMON_INTEGRITY_LOSS_REASON,
  JOMON_INTEGRITY_POLICY_VERSION,
  JOMON_INTEGRITY_RECOVERY_OBLIGATIONS,
  assessJomonIntegrity,
  jomonIntegrityBand,
  validateJomonIntegrityAssessmentRequest,
  type JomonIntegrityAssessmentRequest,
  type JomonIntegrityCausalEvidence,
  type JomonIntegritySafeguardReservation
} from './jomon-integrity-policy'
import { MYSTICAL_EFFECT_POLICY_VERSION, MYSTICAL_EFFECT_RARITY_AVAILABILITY, auditMysticalEffectPolicies } from './mystical-effect-policy'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld } from './world'

const incidentSafety = () => classifyMedievalContent('event', ['environment', 'navigation'], 'not-applicable')
const policySafety = () => classifyMedievalContent('data', ['craft', 'navigation'], 'not-applicable')
const actionSafety = () => classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])

const sourceWorld = () => {
  const selected = chooseInitialCourier(createFoundationWorld({ seed: 'jomon-integrity-policy' }), 'crew:0')
  return advanceFoundationWorldTime(selected, {
    id: 'wait:jomon-integrity-policy',
    kind: 'wait',
    durationMinutes: 12,
    contentSafety: actionSafety()
  })
}

const knownFacts: readonly EffectKnownFact[] = [
  { id: 'fact:actor:jomon', kind: 'actor', revision: 1 },
  { id: 'fact:condition:damage', kind: 'condition', revision: 1 },
  { id: 'fact:crew:carpenter', kind: 'crew-support', revision: 1 },
  { id: 'fact:environment:storm', kind: 'environment', revision: 1 },
  { id: 'fact:equipment:lash', kind: 'equipment', revision: 1 },
  { id: 'fact:object:ward', kind: 'grounded-object', revision: 1 }
]

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

const jomonPolicyRecord = () => {
  const availability = MYSTICAL_EFFECT_RARITY_AVAILABILITY['ultra-rare']
  const effect: EffectCandidate = {
    version: EFFECT_MODEL_CONTRACT_VERSION,
    id: 'effect:jomon-ward',
    category: 'relic',
    polarity: 'aid',
    magnitude: 1,
    priority: 4,
    source: { factId: 'fact:object:ward', factRevision: 1, grounding: 'material-object' },
    target: { factId: 'fact:actor:jomon', factRevision: 1, scope: 'self' },
    evidence: [{ id: 'evidence:jomon-ward', factId: 'fact:object:ward', factRevision: 1 }],
    duration: { startsAtWorldTime: 12, expiresAtWorldTime: 24 },
    stacking: { groupId: 'stack:jomon-ward', mode: 'exclusive', cap: 1 },
    conflictsWith: [],
    chainAfter: [],
    counterplay: { id: 'counterplay:jomon-ward', factId: 'fact:equipment:lash', factRevision: 1 },
    contentSafety: policySafety()
  }
  const audit = auditMysticalEffectPolicies({
    version: MYSTICAL_EFFECT_POLICY_VERSION,
    actionTime: { timeUnit: 'minute', worldTime: 12 },
    knownFacts,
    definitions: [{
      version: MYSTICAL_EFFECT_POLICY_VERSION,
      id: 'mystical-policy:jomon-ward',
      form: 'relic',
      rarity: 'ultra-rare',
      source: { sourceClass: 'material-object', factId: 'fact:object:ward', factRevision: 1 },
      conditions: [{ id: 'condition:jomon-ward', kind: 'object-available', factId: 'fact:object:ward', factRevision: 1 }],
      availability: { scope: 'per-world', maximumDiscoveries: availability.maximumDiscoveriesPerWorld, maximumConcurrentEligibility: availability.maximumConcurrentEligibility },
      useBoundary: { kind: 'limited-use', maximumUses: 1 },
      costs: [{ id: 'cost:jomon-ward', kind: 'material-expenditure', factId: 'fact:condition:damage', factRevision: 1 }],
      auditEvidence: [
        { id: 'audit:jomon-ward:availability', kind: 'availability-bound', factId: 'fact:object:ward', factRevision: 1 },
        { id: 'audit:jomon-ward:condition', kind: 'eligibility-condition', factId: 'fact:object:ward', factRevision: 1 },
        { id: 'audit:jomon-ward:cost', kind: 'cost-trade-off', factId: 'fact:condition:damage', factRevision: 1 },
        { id: 'audit:jomon-ward:source', kind: 'source-provenance', factId: 'fact:object:ward', factRevision: 1 }
      ],
      effect,
      contentSafety: policySafety(),
      futureReservation: { kind: 'future-jomon-loss-safeguard', status: 'deferred-no-authority' }
    }]
  })
  return audit.reviewed[0]!
}

const safeguardReservation = (): JomonIntegritySafeguardReservation => ({
  mysticalPolicyVersion: MYSTICAL_EFFECT_POLICY_VERSION,
  source: { factId: 'fact:object:ward', factRevision: 1 },
  policyRecord: jomonPolicyRecord(),
  causalEvidenceIds: partialEvidence().map(item => item.id),
  adverseCounterplayEvidenceId: 'jomon-evidence:03-counterplay',
  recoveryTradeOffEvidenceId: 'jomon-evidence:02-trade-off'
})

const request = (overrides: Partial<JomonIntegrityAssessmentRequest> = {}): JomonIntegrityAssessmentRequest => {
  const world = sourceWorld()
  const jomon = structuredClone(world.state.jomon)
  jomon.integrity.current = 60
  return {
    version: JOMON_INTEGRITY_POLICY_VERSION,
    actionTime: { timeUnit: 'minute', worldTime: world.state.temporal.worldTime },
    jomon,
    knownFacts,
    incident: { id: 'jomon-incident:partial-storm', kind: 'partial-disaster', contentSafety: incidentSafety() },
    causalEvidence: partialEvidence(),
    ...overrides
  }
}

const codes = (value: unknown): readonly string[] => validateJomonIntegrityAssessmentRequest(value).diagnostics.map(diagnostic => diagnostic.code)

describe('Jomon integrity policy assessment boundary', () => {
  it('uses deterministic integer-safe bands with no locale, wall-clock, or random input', () => {
    expect(jomonIntegrityBand({ current: 100, maximum: 100 })).toBe('sound')
    expect(jomonIntegrityBand({ current: 75, maximum: 100 })).toBe('weathered')
    expect(jomonIntegrityBand({ current: 74, maximum: 100 })).toBe('damaged')
    expect(jomonIntegrityBand({ current: 49, maximum: 100 })).toBe('critical')
    expect(jomonIntegrityBand({ current: 0, maximum: 100 })).toBe('collapsed')
  })

  it('assesses a bounded partial disaster canonically without mutating inputs or making it terminal', () => {
    const input = request()
    const before = structuredClone(input)
    const first = assessJomonIntegrity(input)
    const second = assessJomonIntegrity(structuredClone(input))

    expect(validateJomonIntegrityAssessmentRequest(input)).toEqual({ version: 1, status: 'accepted', diagnostics: [] })
    expect(first).toEqual(second)
    expect(first.integrity).toEqual({ current: 60, maximum: 100, band: 'damaged' })
    expect(first.incident).toEqual({ id: 'jomon-incident:partial-storm', kind: 'partial-disaster', causalEvidenceIds: partialEvidence().map(item => item.id) })
    expect(first.repair).toEqual({ kind: 'future-repair-required', obligationIds: JOMON_INTEGRITY_RECOVERY_OBLIGATIONS, causalEvidenceIds: partialEvidence().map(item => item.id) })
    expect(first.rescue).toEqual({ kind: 'not-needed' })
    expect(first.collapse).toEqual({ status: 'not-collapsed' })
    expect(first.terminalLoss).toBe('not-terminal')
    expect(first.finalization).toEqual({ kind: 'none' })
    expect(input).toEqual(before)
  })

  it('marks critical partial damage for future non-mystical rescue without creating a rescue outcome', () => {
    const input = request()
    input.jomon.integrity.current = 49
    const assessment = assessJomonIntegrity(input)

    expect(assessment.integrity.band).toBe('critical')
    expect(assessment.repair.kind).toBe('future-repair-required')
    expect(assessment.rescue).toEqual({ kind: 'future-non-mystical-rescue-required', obligationIds: JOMON_INTEGRITY_RECOVERY_OBLIGATIONS, causalEvidenceIds: partialEvidence().map(item => item.id) })
    expect(assessment.finalization).toEqual({ kind: 'none' })
  })

  it('requires explicit collapse evidence and emits only the existing Jomon-loss finalization intent', () => {
    const input = request({
      incident: { id: 'jomon-incident:terminal-storm', kind: 'terminal-collapse', contentSafety: incidentSafety() },
      causalEvidence: terminalEvidence()
    })
    input.jomon.integrity.current = 0
    const before = structuredClone(input)
    const assessment = assessJomonIntegrity(input)

    expect(JOMON_INTEGRITY_LOSS_REASON).toBe('jomon-loss')
    expect(assessment.collapse).toEqual({ status: 'confirmed-collapse', causalEvidenceIds: terminalEvidence().map(item => item.id) })
    expect(assessment.terminalLoss).toBe('confirmed-jomon-loss')
    expect(assessment.finalization).toEqual({
      kind: 'read-only-chronicle-finalization-intent',
      reason: 'jomon-loss',
      path: 'existing-read-only-chronicle-path'
    })
    expect(Object.keys(assessment.finalization)).not.toContain('world')
    expect(input).toEqual(before)
  })

  it('keeps an existing ultra-rare Jomon-loss reservation visibly deferred and unavailable', () => {
    const assessment = assessJomonIntegrity(request({ safeguardReservation: safeguardReservation() }))

    expect(assessment.safeguard).toEqual({
      status: 'unavailable',
      reason: 'future-ultra-rare-reservation-only',
      policyRecordId: 'mystical-policy:jomon-ward',
      sourceFactId: 'fact:object:ward',
      conditionIds: ['condition:jomon-ward'],
      costIds: ['cost:jomon-ward'],
      causalEvidenceIds: partialEvidence().map(item => item.id),
      adverseCounterplayEvidenceId: 'jomon-evidence:03-counterplay',
      recoveryTradeOffEvidenceId: 'jomon-evidence:02-trade-off'
    })
    expect(assessment.terminalLoss).toBe('not-terminal')
    expect(assessment.finalization).toEqual({ kind: 'none' })
  })

  it('keeps a deferred safeguard unavailable even during terminal Jomon loss', () => {
    const input = request({
      incident: { id: 'jomon-incident:terminal-warded', kind: 'terminal-collapse', contentSafety: incidentSafety() },
      causalEvidence: terminalEvidence()
    })
    input.jomon.integrity.current = 0
    const reservation = safeguardReservation()
    reservation.causalEvidenceIds = terminalEvidence().map(item => item.id)
    reservation.adverseCounterplayEvidenceId = 'jomon-evidence:03-counterplay'
    reservation.recoveryTradeOffEvidenceId = 'jomon-evidence:02-trade-off'
    input.safeguardReservation = reservation

    expect(assessJomonIntegrity(input)).toMatchObject({
      terminalLoss: 'confirmed-jomon-loss',
      finalization: { reason: 'jomon-loss' },
      safeguard: { status: 'unavailable', reason: 'future-ultra-rare-reservation-only' }
    })
  })

  it('fails closed for malformed, stale, duplicate, noncanonical, unsafe, ungrounded, contradictory, and unauthorized evidence', () => {
    const stale = request()
    stale.causalEvidence[0]!.factRevision = 2
    const duplicate = request()
    duplicate.causalEvidence = [...duplicate.causalEvidence, { ...duplicate.causalEvidence[0]!, id: 'jomon-evidence:00-cause' }]
    const noncanonical = request()
    noncanonical.causalEvidence = [...noncanonical.causalEvidence].reverse()
    const unsafe = request()
    ;(unsafe.incident.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const ungrounded = request()
    ungrounded.causalEvidence[0]!.factId = 'fact:environment:not-known'
    const ineligibleCause = request()
    ineligibleCause.causalEvidence[0]!.factId = 'fact:actor:jomon'
    const contradictory = request()
    contradictory.jomon.integrity.current = 100
    const missingCollapse = request({ incident: { id: 'jomon-incident:missing-collapse', kind: 'terminal-collapse', contentSafety: incidentSafety() } })
    missingCollapse.jomon.integrity.current = 0
    const unauthorized = request({ safeguardReservation: safeguardReservation() })
    unauthorized.safeguardReservation!.policyRecord.rarity = 'rare'
    unauthorized.safeguardReservation!.policyRecord.availability = { scope: 'per-world', maximumDiscoveries: 3, maximumConcurrentEligibility: 1 }
    const ungroundedSafeguard = request({ safeguardReservation: safeguardReservation() })
    ungroundedSafeguard.safeguardReservation!.source.factId = 'fact:object:not-known'
    const malformedSafeguard = request({ safeguardReservation: safeguardReservation() }) as unknown as { safeguardReservation: Record<string, unknown> }
    malformedSafeguard.safeguardReservation.authority = 'granted'

    expect(codes({})).toEqual(['jomon-integrity-policy.malformed-request'])
    expect(codes(stale)).toContain('jomon-integrity-policy.stale-causal-fact')
    expect(codes(duplicate)).toContain('jomon-integrity-policy.duplicate-causal-evidence-id')
    expect(codes(noncanonical)).toContain('jomon-integrity-policy.noncanonical-causal-evidence-order')
    expect(codes(unsafe)).toContain('jomon-integrity-policy.invalid-incident-safety')
    expect(codes(ungrounded)).toContain('jomon-integrity-policy.unknown-causal-fact')
    expect(codes(ineligibleCause)).toContain('jomon-integrity-policy.ineligible-causal-fact')
    expect(codes(contradictory)).toContain('jomon-integrity-policy.contradictory-integrity-incident')
    expect(codes(missingCollapse)).toContain('jomon-integrity-policy.missing-collapse-evidence')
    expect(codes(unauthorized)).toContain('jomon-integrity-policy.unauthorized-safeguard')
    expect(codes(ungroundedSafeguard)).toEqual(expect.arrayContaining(['jomon-integrity-policy.ungrounded-safeguard', 'jomon-integrity-policy.stale-safeguard-source']))
    expect(codes(malformedSafeguard)).toContain('jomon-integrity-policy.malformed-safeguard-reservation')
  }, 10_000)
})
