import { describe, expect, it } from 'vitest'
import { bossContractFor, bossContractForGuardian } from './boss-contracts'
import { resolveDefeatedActors } from './combat'
import { createEnemy, createRun } from '../test/factories'

describe('boss contracts', () => {
  it('keeps Mine and Sea Caves environmental climaxes distinct with explicit counterplay and relic payoff', () => {
    const mine = bossContractFor('mine')
    const caverns = bossContractForGuardian('geode')!
    expect(mine).toMatchObject({ arena: 'rail quarry', phases: { pressure: { terrain: 'rail', telegraph: 'foreman-cavein' } }, reward: 'relic' })
    expect(caverns).toMatchObject({ arena: 'storm-tide chamber', phases: { pressure: { terrain: 'current', telegraph: 'geode-fissure' } }, reward: 'relic' })
    expect(mine.phases.cataclysm.counterplay).not.toEqual('')
    expect(caverns.adds).toEqual(expect.arrayContaining(['artillery', 'controller']))
  })

  it('guarantees a biome reward from an elite route defender', () => {
    const state = createRun()
    state.floor.actors = [createEnemy({ id: 'elite-rat', kind: 'rat', health: 0, status: ['elite'] })]
    resolveDefeatedActors(state)
    expect(state.floor.items.some(item => item.id !== 'gold')).toBe(true)
  })
})
