import { describe, expect, it } from 'vitest'
import { replayAutoplayTrace } from '../autoplay-trace-replay'
import { runAutoplay } from '../autoplay-runner'
import { migrateRunRecord } from '../storage'
import { companionLeadForRescue } from './companions'
import { perform } from './input'
import { synchronizePartyActors } from './party'
import { newRun } from './run'

const companion = (id: string, biome: 'mine' | 'wilds' = 'mine') => {
  const value = companionLeadForRescue({ id: `rescue:${id}`, name: id, biome, floor: 1 })
  value.rosterStatus = 'active'
  value.controlMode = 'direct'
  return value
}
const commandPhase = (count = 2) => {
  const companions = [companion('zeta'), companion('alpha', 'wilds'), companion('mika')].slice(0, count)
  const state = newRun(881, 'mine', 0, undefined, [], [], undefined, undefined, companions)
  synchronizePartyActors(state, 'spawn')
  perform(state, 'l')
  return state
}

describe('direct companion command phase', () => {
  it('runs every active companion once in stable roster order after the courier acts', () => {
    const state = commandPhase(3)
    expect(state.modal).toMatchObject({ kind: 'companionCommand', companionIds: ['companion:rescue:alpha', 'companion:rescue:mika', 'companion:rescue:zeta'], index: 0 })
    expect(perform(state, 'Enter')).toMatchObject([{ type: 'companion', id: 'companion:rescue:alpha', reason: 'wait' }])
    expect(state.modal).toMatchObject({ kind: 'companionCommand', index: 1 })
    perform(state, 'Enter')
    expect(state.modal).toMatchObject({ kind: 'companionCommand', index: 2 })
    perform(state, 'Enter')
    expect(state.modal).toBeUndefined()
    expect(state.turn).toBe(1)
  })

  it('keeps invalid commands and cancellation in the current phase, while wait always resolves', () => {
    const state = commandPhase(2)
    expect(perform(state, 'x')).toEqual([])
    expect(state.modal).toMatchObject({ kind: 'companionCommand', index: 0 })
    expect(perform(state, 'Escape')).toEqual([])
    expect(state.messages[0]).toContain('cannot be cancelled')
    perform(state, 'Enter')
    perform(state, 'Enter')
    expect(state.modal).toBeUndefined()
  })

  it('persists mid-phase safely and refuses direct companion autoplay', () => {
    const state = commandPhase(2)
    expect(migrateRunRecord(JSON.parse(JSON.stringify(state)))?.modal).toEqual(state.modal)
    const report = runAutoplay(newRun(882, 'mine', 0, undefined, [], [], undefined, undefined, [companion('mika')]), { mode: 'visible', policy: 'clear', turnLimit: 2, captureTrace: true })
    expect(report).toMatchObject({ outcome: 'unsupported', commands: [], partyOutcomes: { controlMode: 'direct', directModeRefused: true }, unsupported: { kind: 'direct-companion-control', companionIds: ['companion:rescue:mika'] } })
    expect(replayAutoplayTrace(report.traceDocument!)).toMatchObject({ valid: true })
  })
})
