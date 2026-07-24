import { describe, expect, it } from 'vitest'
import { createRun } from '../test/factories'
import { descend } from './inventory'
import { perform } from './input'
import { chooseTrailcraft, trailcraftChoices } from './trailcraft'

describe('trailcraft', () => {
  it('offers three deterministic upgrades after a cleared non-final floor', () => {
    const state = createRun()
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    expect(descend(state)).toEqual([{ type: 'floor' }])
    expect(state.modal).toEqual({ kind: 'trailcraft' })
    const choices = trailcraftChoices(state)
    expect(choices).toHaveLength(3)
    expect(choices).toEqual(trailcraftChoices(structuredClone(state)))
    const choice = choices[0]
    const stat = state.hero.stats[choice.stat]
    expect(chooseTrailcraft(state, '1')).toBe(true)
    expect(state.hero.stats[choice.stat]).toBe(stat + 1)
    expect(state.hero.trailcrafts?.[choice.id]).toBe(1)
    expect(state.modal).toBeUndefined()
  })

  it('stacks the selected craft, restores vitality or intellect, and can be skipped', () => {
    const offer = (stat: 'vitality' | 'intellect') => {
      const state = createRun()
      for (let index = 0; index < 16; index++) {
        state.floor.index = index
        const choice = trailcraftChoices(state).findIndex(candidate => candidate.stat === stat)
        if (choice >= 0) return { state, choice }
      }
      throw new Error(`missing ${stat} trailcraft offer`)
    }
    const vitality = offer('vitality')
    vitality.state.modal = { kind: 'trailcraft' }
    const health = vitality.state.hero.health
    expect(perform(vitality.state, 'Escape')).toEqual([{ type: 'menu' }])
    expect(vitality.state.modal).toBeUndefined()
    vitality.state.modal = { kind: 'trailcraft' }
    expect(chooseTrailcraft(vitality.state, String(vitality.choice + 1))).toBe(true)
    expect(vitality.state.hero.health).toBeGreaterThan(health)

    const intellect = offer('intellect')
    intellect.state.hero.focus = 0
    intellect.state.modal = { kind: 'trailcraft' }
    expect(chooseTrailcraft(intellect.state, String(intellect.choice + 1))).toBe(true)
    expect(intellect.state.hero.focus).toBeGreaterThan(0)
  })
})
