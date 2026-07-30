import { describe, expect, it } from 'vitest'
import { createEnemy } from '../test/factories'
import { companionLeadForRescue, companionLodgeAction } from './companions'
import { autoplayReplayMetadata } from '../autoplay-runner'
import { isCompanionActor, synchronizePartyActors } from './party'
import { newRun } from './run'
import { perform } from './input'

const guardState = (adjacent: boolean, companionDeathMode: 'injury' | 'permadeath' = 'injury') => {
  const guard = companionLeadForRescue({ id: 'rescue:guard', name: 'Mika', biome: 'mine', floor: 1 })
  guard.rosterStatus = 'active'
  guard.controlMode = 'autonomous'
  const state = newRun(991, 'mine', 0, undefined, [], [], undefined, undefined, [guard], companionDeathMode)
  synchronizePartyActors(state, 'spawn')
  const actor = state.floor.actors.find(isCompanionActor)!
  actor.x = state.hero.x + (adjacent ? 1 : 4)
  actor.y = state.hero.y
  state.floor.actors.push(createEnemy({ id: 'attacker', x: state.hero.x, y: state.hero.y + 1, attack: 99, speed: 100, energy: 0 }))
  return { state, guard }
}

describe('Guard intercept', () => {
  it('redirects an adjacent eligible enemy hit with a visible cost and cooldown', () => {
    const { state, guard } = guardState(true)
    perform(state, 'l')
    expect(state.messages.join(' ')).toContain('intercepts')
    expect(state.companions?.find(companion => companion.id === guard.id)).toMatchObject({ abilityState: { cooldowns: { intercept: 2 } }, injury: 'injured', rosterStatus: 'benched' })
    expect(state.floor.actors.some(isCompanionActor)).toBe(false)
  })

  it('leaves damage normal when the Guard is not adjacent or intercept is cooling down', () => {
    const far = guardState(false)
    perform(far.state, 'l')
    expect(far.state.messages.join(' ')).not.toContain('intercepts')
    const cooldown = guardState(true)
    cooldown.state.companions![0]!.abilityState.cooldowns.intercept = 2
    perform(cooldown.state, 'l')
    expect(cooldown.state.messages.join(' ')).not.toContain('intercepts')
  })

  it('permanently removes a Guard only when the immutable courier setting selects it', () => {
    const { state, guard } = guardState(true, 'permadeath')
    expect(autoplayReplayMetadata(state).companionDeathMode).toBe('permadeath')
    perform(state, 'l')
    const lost = state.companions!.find(companion => companion.id === guard.id)!
    expect(lost).toMatchObject({ injury: 'healthy', rosterStatus: 'lost', permanentlyLost: true })
    expect(companionLodgeAction(lost)).toBeUndefined()
    expect(state.floor.actors.some(isCompanionActor)).toBe(false)
    expect(state.messages.join(' ')).toContain('permanently lost')
    expect(state.companionDeathMode).toBe('permadeath')
  })
})
