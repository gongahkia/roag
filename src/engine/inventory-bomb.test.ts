import { describe, expect, it } from 'vitest'
import { createRun } from '../test/factories'
import { bomb } from './inventory'

describe('bomb placement', () => {
  it('does not hit the hero when placed in a direction', () => {
    const state = createRun()
    state.hero.bombs = 1
    const health = state.hero.health
    bomb(state, 'e')
    expect(state.hero.health).toBe(health)
    expect(state.hero.bombs).toBe(0)
    expect(state.messages).toContain('You place a bomb.')
  })
})
