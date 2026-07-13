import { describe, expect, it } from 'vitest'
import { MONSTERS } from '../content'
import { advance, moveHero } from './combat'
import { createEnemy, createHero, createRun } from '../test/factories'
import { getTile } from '../world'

describe('Caverns enemy roster', () => {
  it('adds fire, gas, light, and displacement roles', () => {
    expect(MONSTERS.filter(monster => monster.biome === 'caverns').map(monster => monster.id)).toEqual(expect.arrayContaining(['cinderimp', 'fumeeel', 'gloomseer', 'crystalpuller']))
    expect(MONSTERS.find(monster => monster.id === 'crystalpuller')?.tags).toEqual(expect.arrayContaining(['displacement', 'telegraph']))
  })

  it('uses fire lines, gas momentum, light counterplay, and pulls', () => {
    const fire = createRun()
    fire.floor.actors = [createEnemy({ id: 'cinderimp', kind: 'cinderimp', name: 'Cinder Imp', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(fire, [])
    moveHero(fire, 'e')
    expect(getTile(fire.floor, 1, 1)?.kind).toBe('fireVent')

    const gas = createRun()
    const eel = createEnemy({ id: 'fumeeel', kind: 'fumeeel', name: 'Fume Eel', x: 4, y: 1, speed: 100, energy: 0 })
    gas.floor.tiles[1 * 48 + 4].kind = 'gas'
    gas.floor.actors = [eel]
    advance(gas, [])
    expect(eel).toMatchObject({ x: 3, energy: 50 })

    const dark = createRun()
    dark.floor.actors = [createEnemy({ id: 'gloomseer', kind: 'gloomseer', name: 'Gloom Seer', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(dark, [])
    expect(dark.floor.telegraphs).toMatchObject([{ actionId: 'enemy-shot' }])
    const light = createRun({ hero: createHero({ equipment: { offHand: 'lantern' } }) })
    light.floor.actors = [createEnemy({ id: 'gloomseer', kind: 'gloomseer', name: 'Gloom Seer', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(light, [])
    expect(light.floor.telegraphs).toEqual([])

    const pull = createRun()
    pull.floor.tiles[1 * 48 + 2].kind = 'gas'
    pull.floor.actors = [createEnemy({ id: 'crystalpuller', kind: 'crystalpuller', name: 'Crystal Puller', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(pull, [])
    advance(pull, [])
    expect(pull.hero).toMatchObject({ x: 2, y: 1, health: 20 })
  })
})
