import { describe, expect, it } from 'vitest'
import { ACTIONS, ENEMY_ACTIONS, PLAYER_ACTIONS, actionById } from './actions'

describe('action definitions', () => {
  it('declares player and enemy actions with tactical metadata', () => {
    expect(PLAYER_ACTIONS.length).toBeGreaterThan(0)
    expect(ENEMY_ACTIONS.length).toBeGreaterThan(0)
    for (const action of ACTIONS) {
      expect(action.range).toBeGreaterThan(0)
      expect(action.tags.length).toBeGreaterThan(0)
      expect(action.shape).toBeTruthy()
      expect(action.resolver).toBeTruthy()
      expect(action.cost.resource).toBeTruthy()
    }
  })

  it('resolves definitions by stable id', () => {
    expect(actionById('player-bomb')).toMatchObject({ owner: 'player', shape: 'burst', resolver: 'bomb' })
    expect(actionById('enemy-shot')).toMatchObject({ owner: 'enemy', range: 7, resolver: 'projectile' })
    expect(actionById('unknown')).toBeUndefined()
  })
})
