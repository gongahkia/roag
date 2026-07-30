import { describe, expect, it } from 'vitest'
import { autoplayCandidateDiagnostics } from './autoplay'
import { autoplayPartyCandidateScore, createAutoplayPartyOutcomes, recordAutoplayPartyOutcome } from './autoplay-party'
import { runAutoplay } from './autoplay-runner'
import { event } from './engine/shared'
import { companionLeadForRescue, injureCompanion } from './engine/companions'
import { synchronizePartyActors } from './engine/party'
import { createEnemy, createRun } from './test/factories'

const companion = (id: string) => {
  const value = companionLeadForRescue({ id: `rescue:${id}`, name: id, biome: 'mine', floor: 1 })
  value.rosterStatus = 'active'
  return value
}

describe('autoplay party planning', () => {
  it('keeps autonomous companion planning isolated and reports replayable party state', () => {
    const guard = companion('mika')
    const state = createRun({ companions: [guard] })
    synchronizePartyActors(state, 'spawn')
    state.floor.actors.push(createEnemy({ id: 'threat', x: 2, y: 1 }))
    const before = structuredClone(state.companions)
    autoplayCandidateDiagnostics(state, 'visible', 'clear')
    expect(state.companions).toEqual(before)
    const report = runAutoplay(state, { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true })
    expect(report.partyOutcomes).toMatchObject({ controlMode: 'autonomous', roster: [{ id: guard.id, role: 'guard', controlMode: 'autonomous', rosterStatus: 'active', injury: 'healthy', permanentlyLost: false }], activeCompanionIds: [guard.id], directModeRefused: false, finiteResourceConsents: 0 })
    expect(report.traceDocument).toBeDefined()
  })

  it('tracks companion actions, injuries, losses, traversal help, and blocked turns separately', () => {
    const guard = companion('mika')
    const before = createRun({ companions: [guard] })
    const after = structuredClone(before)
    const afterGuard = after.companions![0]!
    injureCompanion(afterGuard)
    const outcomes = createAutoplayPartyOutcomes(before)
    recordAutoplayPartyOutcome(outcomes, before, after, [event('companion', guard.id, 'intercept:1,1:shield'), event('companion', guard.id, 'stabilizeTerrain:2,1:route'), event('companion', guard.id, 'wait:self:waited:the follow path is blocked')])
    expect(outcomes).toMatchObject({ actions: { intercept: 1, stabilizeTerrain: 1 }, actionsByCompanion: { [guard.id]: { intercept: 1, stabilizeTerrain: 1, wait: 1 } }, intercepts: 1, traversalAssists: 1, injuries: [guard.id], blockedTurns: 1 })
    expect(autoplayPartyCandidateScore(before, after)).toBeLessThan(0)
  })
})
