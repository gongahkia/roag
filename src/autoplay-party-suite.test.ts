import { describe, expect, it } from 'vitest'
import { autoplayDecision, autoplayStateFingerprint, createAutoplayContext } from './autoplay'
import { PARTY_AUTOPLAY_FIXTURES, assertPartyAutoplayFixtures, partyAutoplayFixtureAction, partyAutoplayFixtureState } from './autoplay-party-suite'
import { gateSacrificeCandidates, gateSacrificeConsequence } from './engine/gates'

describe('party autoplay seed suites', () => {
  it('covers every companion role and risk in fixed development and held-out fixtures', () => {
    expect(() => assertPartyAutoplayFixtures()).not.toThrow()
  })

  it.each(PARTY_AUTOPLAY_FIXTURES.filter(fixture => fixture.expectedAction))('$partition/$id seed=$seed $role action', fixture => {
    expect(partyAutoplayFixtureAction(fixture)).toBe(fixture.expectedAction)
  })

  it('keeps direct control unsupported and keeps gate loss explicit for injury and permadeath fixtures', () => {
    for (const direct of PARTY_AUTOPLAY_FIXTURES.filter(fixture => fixture.risk === 'direct-control')) {
      const directState = partyAutoplayFixtureState(direct).state
      const context = createAutoplayContext()
      expect(autoplayDecision(directState, 'visible', 'clear', context)).toBeUndefined()
      expect(context.lastReason).toBe('unsupported direct companion control')
    }
    const gate = PARTY_AUTOPLAY_FIXTURES.find(fixture => fixture.risk === 'gate-sacrifice')!
    const gateState = partyAutoplayFixtureState(gate).state
    expect(gateSacrificeConsequence(gateState, gateSacrificeCandidates(gateState)[0]!)).toBe('injury')
    const permanent = PARTY_AUTOPLAY_FIXTURES.find(fixture => fixture.risk === 'permadeath')!
    const permanentState = partyAutoplayFixtureState(permanent).state
    expect(permanentState.companionDeathMode).toBe('permadeath')
  })

  it('rebuilds each fixed fixture deterministically', () => {
    for (const fixture of PARTY_AUTOPLAY_FIXTURES) expect(autoplayStateFingerprint(partyAutoplayFixtureState(fixture).state)).toBe(autoplayStateFingerprint(partyAutoplayFixtureState(fixture).state))
  })
})
