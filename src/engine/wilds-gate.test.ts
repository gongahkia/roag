import { describe, expect, it } from 'vitest'
import { resolveAreaGate, gateForArea } from './gates'
import { createEnemy, createRun } from '../test/factories'

describe('Wilds gate solutions', () => {
  it('unlocks Verdant Wilds through NPC, fire-tag, or bomb alternatives', () => {
    const gate = gateForArea('mine')
    const npc = createRun()
    npc.floor.actors = [createEnemy({ role: 'ally', hostile: false })]
    expect(resolveAreaGate(npc, gate, 0)).toMatchObject({ resolved: true, destination: 'wilds' })
    const fire = createRun()
    fire.hero.inventory = ['ember']
    expect(resolveAreaGate(fire, gate, 1)).toMatchObject({ resolved: true, destination: 'wilds' })
    const bomb = createRun()
    bomb.hero.bombs = 1
    expect(resolveAreaGate(bomb, gate, 2)).toMatchObject({ resolved: true, destination: 'wilds' })
    expect(bomb.hero.bombs).toBe(0)
  })

  it('fails alternatives that lack their required offering, tag, or bomb', () => {
    const gate = gateForArea('mine')
    const state = createRun()
    expect(resolveAreaGate(state, gate, 0).resolved).toBe(false)
    expect(resolveAreaGate(state, gate, 1).resolved).toBe(false)
    expect(resolveAreaGate(state, gate, 2).resolved).toBe(false)
  })
})
