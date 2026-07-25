import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createHero, createRun } from '../test/factories'
import { perform } from './input'

const waycache = { id: 'waycache', kind: 'waycache' as const, x: 1, y: 1, discovered: true, claimed: false }
const boonSite = { id: 'boon', kind: 'boon' as const, x: 1, y: 1, discovered: true, claimed: false }

describe('buildcraft', () => {
  it('binds a Waycache tool, applies its cooldown, and reports recovery', () => {
    const state = createRun({ floor: createRun().floor })
    state.floor.milestones = [waycache]
    perform(state, 'c')
    expect(state.modal).toMatchObject({ kind: 'tool' })
    perform(state, '1')
    expect(state.hero.traversalTools).toHaveLength(1)
    state.hero.traversalTools = ['stoneWedge']
    state.floor.tiles[indexOf(2, 1)].kind = 'rubble'
    perform(state, 'y'); perform(state, '1'); perform(state, ';'); perform(state, 'Enter')
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('floor')
    expect(state.hero.cooldowns?.['tool:stoneWedge']).toBeGreaterThan(0)
    perform(state, 'y'); perform(state, '1')
    expect(state.messages[0]).toContain('recovers')
  })

  it('retires an overdriven tool after its stronger traversal', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['reedwing'] }) })
    state.floor.tiles[indexOf(2, 1)].kind = 'pit'
    state.floor.tiles[indexOf(3, 1)].kind = 'pit'
    perform(state, 'y'); perform(state, '1'); perform(state, 'o'); perform(state, ';'); perform(state, 'Enter')
    expect(state.hero).toMatchObject({ x: 4, y: 1 })
    expect(state.hero.traversalTools).toEqual([])
    expect(state.messages[0]).toContain('burns out')
  })

  it('stacks Boons and rewinds position without restoring world state', () => {
    const state = createRun({ hero: createHero({ boons: { timeKnot: 1 }, safePositions: [{ x: 1, y: 1 }, { x: 3, y: 1 }], x: 3, y: 1 }) })
    state.floor.tiles[indexOf(2, 1)].kind = 'lava'
    perform(state, 'w')
    expect(state.hero).toMatchObject({ x: 1, y: 1 })
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('lava')
    expect(state.hero.boons?.timeKnot).toBeUndefined()
  })

  it('claims deterministic Boon drafts from traversed sites', () => {
    const state = createRun()
    state.floor.milestones = [boonSite]
    perform(state, 'c'); perform(state, '1')
    expect(state.floor.milestones[0].claimed).toBe(true)
    expect(Object.values(state.hero.boons ?? {}).reduce<number>((sum, rank) => sum + (rank ?? 0), 0)).toBe(1)
  })
})
