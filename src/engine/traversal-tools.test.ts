import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createHero, createRun } from '../test/factories'
import { bridge, dash, grapple, winch } from './inventory'

const withItem = (id: string) => createRun({ hero: createHero({ inventory: [id] }) })

describe('consumable traversal tools', () => {
  it('grapples over a gap to a clear landing', () => {
    const state = withItem('grappleLine')
    state.floor.tiles[indexOf(2, 1)].kind = 'pit'
    grapple(state, 'grappleLine', 'e')
    expect(state.hero).toMatchObject({ x: 4, y: 1, inventory: [] })
  })

  it('locks a bridge into hazardous water', () => {
    const state = withItem('bridgeKit')
    state.floor.tiles[indexOf(2, 1)].kind = 'deepWater'
    bridge(state, 'bridgeKit', 'e')
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('rope')
    expect(state.hero.inventory).toEqual([])
  })

  it('dashes through smoke and clears a pushed obstruction', () => {
    const dashState = withItem('steamJetpack')
    dashState.floor.tiles[indexOf(2, 1)].kind = 'smoke'
    dash(dashState, 'steamJetpack', 'e')
    expect(dashState.hero).toMatchObject({ x: 3, y: 1, inventory: [] })

    const winchState = withItem('portableWinch')
    winchState.floor.tiles[indexOf(2, 1)].kind = 'boulder'
    winch(winchState, 'portableWinch', 'e')
    expect(winchState.hero).toMatchObject({ x: 2, y: 1, inventory: [] })
    expect(winchState.floor.tiles[indexOf(2, 1)].kind).toBe('floor')
  })
})
