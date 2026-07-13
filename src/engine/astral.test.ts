import { describe, expect, it } from 'vitest'
import { castSpell } from './inventory'
import { createEnemy, createHero, createRun } from '../test/factories'

describe('Astral', () => {
  it('reveals terrain, blinks across obstacles, and raises a ward', () => {
    const sight = createRun({ hero: createHero({ inventory: ['sight'] }) })
    sight.floor.tiles[1 * 48 + 4].explored = false
    castSpell(sight, 'sight', 'e')
    expect(sight.floor.tiles[1 * 48 + 4].explored).toBe(true)

    const blink = createRun({ hero: createHero({ inventory: ['blink'] }) })
    blink.floor.tiles[1 * 48 + 2].kind = 'wall'
    blink.floor.tiles[1 * 48 + 3].kind = 'wall'
    castSpell(blink, 'blink', 'e')
    expect(blink.hero).toMatchObject({ x: 4, y: 1 })

    const ward = createRun({ hero: createHero({ inventory: ['wardScript'] }) })
    castSpell(ward, 'wardScript', 'e')
    expect(ward.hero.maxHealth).toBe(24)
    expect(ward.hero.conditions).toContainEqual({ kind: 'shielded', duration: 2, potency: 1 })
  })

  it('pulls creatures and gates to the exit', () => {
    const target = createEnemy({ x: 3, y: 1, energy: 0 })
    const pull = createRun({ hero: createHero({ inventory: ['pull'] }) })
    pull.floor.actors = [target]
    castSpell(pull, 'pull', 'e')
    expect(target.x).toBe(2)

    const gate = createRun({ hero: createHero({ inventory: ['gate'] }) })
    castSpell(gate, 'gate', 'e')
    expect(gate.hero).toMatchObject(gate.floor.exit)
  })
})
