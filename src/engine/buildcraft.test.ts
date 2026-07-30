import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createHero, createRun } from '../test/factories'
import { perform } from './input'
import { boonAlignment, boonChoices } from './buildcraft'

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

  it('shifts eligible terrain with Antler Prybar without consuming invalid uses', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['antlerPrybar'] }) })
    state.floor.tiles[indexOf(2, 1)].kind = 'boulder'
    state.floor.tiles[indexOf(3, 1)].kind = 'floor'
    perform(state, 'y'); perform(state, '1'); perform(state, ';'); perform(state, 'Enter')
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('floor')
    expect(state.floor.tiles[indexOf(3, 1)].kind).toBe('boulder')
    expect(state.hero.cooldowns?.['tool:antlerPrybar']).toBeGreaterThan(0)
    expect(state.hero.conditions).toContainEqual({ kind: 'marked', duration: 1, potency: 1 })
    const turn = state.turn
    perform(state, 'y'); perform(state, '1'); perform(state, ';'); perform(state, 'Enter')
    expect(state.turn).toBe(turn)
    expect(state.messages[0]).toContain('recovers')
  })

  it('cuts only weak-route targets with Stone Adze and retires on overdrive', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['stoneAdze'] }) })
    state.floor.tiles[indexOf(2, 1)].kind = 'crate'
    perform(state, 'y'); perform(state, '1'); perform(state, ';'); perform(state, 'Enter')
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('floor')
    expect(state.hero.cooldowns?.['tool:stoneAdze']).toBeGreaterThan(0)
    state.hero.cooldowns = {}
    state.floor.tiles[indexOf(2, 1)].kind = 'bramble'
    perform(state, 'y'); perform(state, '1'); perform(state, ';'); perform(state, 'Enter')
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('bramble')
    expect(state.messages[0]).toContain('cuts only')
    state.floor.tiles[indexOf(2, 1)].kind = 'crumble'
    perform(state, 'y'); perform(state, '1'); perform(state, 'o'); perform(state, ';'); perform(state, 'Enter')
    expect(state.hero.traversalTools).toEqual([])
  })

  it('burns only adjacent unoccupied vegetation into deterministic smoke', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['resinFireBasket'] }) })
    state.floor.tiles[indexOf(2, 1)].kind = 'web'
    perform(state, 'y'); perform(state, '1'); perform(state, ';'); perform(state, 'Enter')
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('smoke')
    expect(state.hero.cooldowns?.['tool:resinFireBasket']).toBeGreaterThan(0)
    expect(state.messages[0]).toContain('smoking')
    state.hero.cooldowns = {}
    state.floor.tiles[indexOf(2, 1)].kind = 'wall'
    perform(state, 'y'); perform(state, '1'); perform(state, ';'); perform(state, 'Enter')
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('wall')
    expect(state.messages[0]).toContain('needs an unoccupied')
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
    const choice = boonChoices(state, boonSite)[0]
    perform(state, 'c'); perform(state, '1')
    expect(state.floor.milestones[0].claimed).toBe(true)
    expect(Object.values(state.hero.boons ?? {}).reduce<number>((sum, rank) => sum + (rank ?? 0), 0)).toBe(1)
    expect(state.alignment?.[boonAlignment(choice.id)]).toBe(1)
  })
})
