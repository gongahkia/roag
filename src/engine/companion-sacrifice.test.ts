import { describe, expect, it } from 'vitest'
import { autoplayReplayMetadata } from '../autoplay-runner'
import { migrateRunRecord } from '../storage'
import { createRun } from '../test/factories'
import { companionLeadForRescue } from './companions'
import { initialCampaignRoute, recordCampaignSacrifice } from './campaign'
import { gateForArea, gateSacrificeCandidates, resolveAreaGate } from './gates'
import { perform } from './input'
import { isCompanionActor, synchronizePartyActors } from './party'

const rescue = { id: 'rescue:mine:1:mika', name: 'Mika', biome: 'mine' as const, floor: 1 }
const active = () => {
  const companion = companionLeadForRescue(rescue)
  companion.rosterStatus = 'active'
  return companion
}
const sacrificeRun = (companion = active()) => {
  const state = createRun({ area: 'mine', rescuedNpcs: [rescue], companions: [companion] })
  synchronizePartyActors(state, 'spawn')
  return { state, companion }
}

describe('companion gate sacrifice consent', () => {
  it('requires a named active companion and a distinct confirmation before an injury sacrifice', () => {
    const { state, companion } = sacrificeRun()
    state.modal = { kind: 'gate', gateId: 'mine-wilds-pass' }
    expect(perform(state, '1')).toEqual([{ type: 'menu' }])
    expect(perform(state, '1')).toEqual([{ type: 'menu' }])
    expect(state.modal).toMatchObject({ choice: 0, offeringId: companion.id, confirming: false })
    expect(perform(state, 'Enter')).toEqual([{ type: 'menu' }])
    expect(state.modal).toMatchObject({ offeringId: companion.id, confirming: true })
    expect(perform(state, 'Enter')).toEqual([{ type: 'gateResolved' }])
    expect(state.companions).toMatchObject([{ id: companion.id, injury: 'injured', rosterStatus: 'benched', permanentlyLost: false }])
    expect(state.rescuedNpcs).toEqual([rescue])
    expect(state.lineageEvents ?? []).toEqual([])
    expect(state.floor.actors.some(isCompanionActor)).toBe(false)
    expect(state.messages.join(' ')).toContain('Mika holds the passage and returns injured to the Lodge.')
  })

  it('declines without changing the roster or passage', () => {
    const { state, companion } = sacrificeRun()
    state.modal = { kind: 'gate', gateId: 'mine-wilds-pass' }
    perform(state, '1')
    perform(state, '1')
    perform(state, 'Escape')
    expect(state.modal).toBeUndefined()
    expect(state.companions).toMatchObject([{ id: companion.id, injury: 'healthy', rosterStatus: 'active', permanentlyLost: false }])
    expect(state.gateDestination).toBeUndefined()
  })

  it('excludes benched, injured, and unknown companions from selection', () => {
    const benched = active()
    benched.rosterStatus = 'benched'
    const state = createRun({ area: 'mine', rescuedNpcs: [rescue], companions: [benched] })
    expect(gateSacrificeCandidates(state)).toEqual([])
    expect(resolveAreaGate(state, gateForArea('mine'), 0, benched.id)).toMatchObject({ resolved: false, message: 'No eligible companion can hold this passage.' })
    expect(resolveAreaGate(state, gateForArea('mine'), 0, 'missing')).toMatchObject({ resolved: false })
    expect(state.companions).toMatchObject([{ injury: 'healthy', rosterStatus: 'benched', permanentlyLost: false }])
  })

  it('records permanent selection in saves and replay metadata when the courier chose permadeath', () => {
    const { state, companion } = sacrificeRun()
    state.companionDeathMode = 'permadeath'
    const result = resolveAreaGate(state, gateForArea('mine'), 0, companion.id)
    expect(result).toMatchObject({ resolved: true, sacrificedNpc: rescue, sacrificedCompanion: { id: companion.id, rosterStatus: 'lost', permanentlyLost: true }, consequence: 'permanent', lineageEvent: { npcId: rescue.id, npcName: rescue.name } })
    expect(state.rescuedNpcs).toEqual([])
    const campaign = initialCampaignRoute()
    campaign.rescuedNpcs = [rescue]
    campaign.companions = [active()]
    expect(recordCampaignSacrifice(campaign, state.lineageEvents![0]!)).toMatchObject({ rescuedNpcs: [], companions: [{ id: companion.id, rosterStatus: 'lost', permanentlyLost: true }] })
    expect(autoplayReplayMetadata(state)).toMatchObject({ companionDeathMode: 'permadeath', companions: [{ id: companion.id, rosterStatus: 'lost', permanentlyLost: true }] })
    expect(migrateRunRecord(JSON.parse(JSON.stringify(state)))).toMatchObject({ lineageEvents: [{ npcId: rescue.id }], companions: [{ id: companion.id, rosterStatus: 'lost', permanentlyLost: true }] })
  })
})
