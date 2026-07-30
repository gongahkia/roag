import { describe, expect, it } from 'vitest'
import { autoplayReplayMetadata, runAutoplay } from '../autoplay-runner'
import { replayAutoplayTrace } from '../autoplay-trace-replay'
import type { RescuedNpc } from '../types'
import { newRun } from './run'
import { addCompanionLeads, changeCompanionRoster, cloneCompanions, COMPANION_ACTIVE_CAPACITY, companionLeadForRescue, companionLeadsForRescues, loseCompanionForRescue } from './companions'

const rescue: RescuedNpc = { id: 'rescue:mine:2:scout', name: 'Mika', biome: 'mine', floor: 2 }

describe('companion persistence schema', () => {
  it('creates deterministic recruitable leads without activating them', () => {
    const lead = companionLeadForRescue(rescue)
    expect(lead).toMatchObject({ version: 1, id: 'companion:rescue:mine:2:scout', templateId: 'rescue:mine', name: 'Mika', role: 'guard', recruitment: { rescueId: rescue.id, biome: 'mine', floor: 2 }, rosterStatus: 'lead', controlMode: 'autonomous', injury: 'healthy', abilityState: { cooldowns: {}, retired: [] }, toolState: { cooldown: 0, retired: false }, permanentlyLost: false })
    expect(companionLeadsForRescues([rescue])).toEqual([lead])
  })

  it('clones ability and tool state without aliasing and records permanent rescue loss', () => {
    const source = companionLeadForRescue(rescue)
    source.abilityState.cooldowns.guard = 3
    source.toolState.equipped = 'stoneWedge'
    const clone = cloneCompanions([source], [rescue])
    clone[0]!.abilityState.cooldowns.guard = 0
    clone[0]!.toolState.equipped = undefined
    expect(source).toMatchObject({ abilityState: { cooldowns: { guard: 3 } }, toolState: { equipped: 'stoneWedge' } })
    const lost = loseCompanionForRescue(source, rescue.id)
    expect(cloneCompanions([lost], [])).toMatchObject([{ rosterStatus: 'lost', permanentlyLost: true }])
  })

  it('rejects invalid rescue references and carries companions into deterministic replay metadata', () => {
    const lead = companionLeadForRescue(rescue)
    expect(() => cloneCompanions([lead], [])).toThrow(`recruitment rescue ${rescue.id} is missing`)
    const state = newRun(7, 'mine', 0, undefined, [rescue], [], undefined, undefined, [lead])
    expect(autoplayReplayMetadata(state)).toMatchObject({ companions: [lead] })
    const trace = runAutoplay(state, { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true }).traceDocument!
    expect(replayAutoplayTrace(trace)).toMatchObject({ valid: true })
  })

  it('creates leads from new rescues and requires free confirmed roster transitions', () => {
    const rescues = [
      rescue,
      { id: 'rescue:wilds:1:bo', name: 'Bo', biome: 'wilds' as const, floor: 1 },
      { id: 'rescue:caverns:1:ren', name: 'Ren', biome: 'caverns' as const, floor: 1 },
      { id: 'rescue:ruins:1:sai', name: 'Sai', biome: 'ruins' as const, floor: 1 }
    ]
    const leads = addCompanionLeads([], rescues)
    expect(addCompanionLeads(leads, rescues)).toEqual(leads)
    const recruited = changeCompanionRoster(leads, rescues, leads[0]!.id, 'recruit')
    expect(recruited).toMatchObject({ changed: true, message: 'Mika joined the lodge bench at no cost.' })
    expect(recruited.companions[0]).toMatchObject({ rosterStatus: 'benched' })
    expect(changeCompanionRoster(recruited.companions, rescues, leads[0]!.id, 'recruit')).toMatchObject({ changed: false })
    const duplicateRescue = { id: 'rescue:mine:3:rin', name: 'Rin', biome: 'mine' as const, floor: 3 }
    const withDuplicate = addCompanionLeads(recruited.companions, [...rescues, duplicateRescue])
    expect(changeCompanionRoster(withDuplicate, [...rescues, duplicateRescue], `companion:${duplicateRescue.id}`, 'recruit')).toMatchObject({ changed: false, message: "Rin's template is already represented in the lodge." })
    const injured = structuredClone(recruited.companions)
    injured[0]!.injury = 'injured'
    expect(changeCompanionRoster(injured, rescues, injured[0]!.id, 'activate')).toMatchObject({ changed: false, message: 'Mika is unavailable while injured.' })
    let roster = recruited.companions
    for (const companion of roster.slice(1, COMPANION_ACTIVE_CAPACITY + 1)) {
      const next = changeCompanionRoster(roster, rescues, companion.id, 'recruit')
      roster = next.companions
    }
    for (const companion of roster.filter(candidate => candidate.rosterStatus === 'benched').slice(0, COMPANION_ACTIVE_CAPACITY)) roster = changeCompanionRoster(roster, rescues, companion.id, 'activate').companions
    const benched = roster.find(companion => companion.rosterStatus === 'benched')!
    expect(changeCompanionRoster(roster, rescues, benched.id, 'activate')).toMatchObject({ changed: false, message: `Active companion capacity is ${COMPANION_ACTIVE_CAPACITY}.` })
    const active = roster.find(companion => companion.rosterStatus === 'active')!
    roster = changeCompanionRoster(roster, rescues, active.id, 'bench').companions
    expect(changeCompanionRoster(roster, rescues, benched.id, 'activate')).toMatchObject({ changed: true })
  })
})
