import { describe, expect, it } from 'vitest'
import { BOONS, boonRank } from './buildcraft'
import { perform } from './input'
import { createHero, createRun } from '../test/factories'

const augment = { id: 'augment', kind: 'augment' as const, x: 1, y: 1, discovered: true, claimed: false }

describe('build-up moments', () => {
  it('evolves an owned Boon without a tier cap and claims the site', () => {
    const state = createRun({ hero: createHero({ boons: { coolAsh: 1 } }) })
    state.floor.milestones = [augment]
    perform(state, 'c'); perform(state, '1'); perform(state, '1')
    expect(state.hero.boonEvolutions?.coolAsh).toBe(1)
    expect(boonRank(state, 'coolAsh')).toBe(2)
    expect(state.floor.milestones[0].claimed).toBe(true)
  })

  it('reforges and transmutes selected Boons into deterministic build choices', () => {
    const reforge = createRun({ hero: createHero({ boons: { coolAsh: 1 } }) })
    reforge.floor.milestones = [{ ...augment, claimed: false }]
    perform(reforge, 'c'); perform(reforge, '2'); perform(reforge, '1'); perform(reforge, '1')
    expect(reforge.floor.milestones[0].claimed).toBe(true)
    expect(Object.keys(reforge.hero.boons ?? {})).not.toEqual(['coolAsh'])

    const transmute = createRun({ hero: createHero({ boons: { coolAsh: 1 } }) })
    transmute.floor.milestones = [{ ...augment, claimed: false }]
    perform(transmute, 'c'); perform(transmute, '3'); perform(transmute, '1'); perform(transmute, '1')
    expect(Object.keys(transmute.hero.boons ?? {}).some(id => BOONS.find(boon => boon.id === id)?.rare)).toBe(true)
  })
})
