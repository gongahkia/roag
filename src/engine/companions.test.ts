import { describe, expect, it } from 'vitest'
import { autoplayReplayMetadata, runAutoplay } from '../autoplay-runner'
import { replayAutoplayTrace } from '../autoplay-trace-replay'
import type { RescuedNpc } from '../types'
import { newRun } from './run'
import { cloneCompanions, companionLeadForRescue, companionLeadsForRescues, loseCompanionForRescue } from './companions'

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
})
