import { describe, expect, it } from 'vitest'
import { ACTIONS, actionById } from './actions'

describe('action definitions', () => {
  it('declares names and tags for combat logs and the encyclopedia', () => {
    for (const action of ACTIONS) {
      expect(action.id).toBeTruthy()
      expect(action.name).toBeTruthy()
      expect(action.tags.length).toBeGreaterThan(0)
    }
  })

  it('resolves definitions by stable id', () => {
    expect(actionById('player-bomb')).toEqual({ id: 'player-bomb', name: 'Bomb', tags: ['area', 'terrain'] })
    expect(actionById('enemy-shot')).toEqual({ id: 'enemy-shot', name: 'Shot', tags: ['ranged', 'telegraphed'] })
    expect(actionById('unknown')).toBeUndefined()
  })
})
