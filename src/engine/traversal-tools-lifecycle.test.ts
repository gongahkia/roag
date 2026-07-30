import { describe, expect, it } from 'vitest'
import { autoplayToolDiagnostics } from '../autoplay'
import { indexOf } from '../types'
import { createHero, createRun } from '../test/factories'
import { toolFor } from './buildcraft'
import { perform } from './input'
import { newRun } from './run'
import { migrateRunRecord } from '../storage'

const tools = ['antlerPrybar', 'stoneAdze', 'resinFireBasket', 'woodenLeverRoller'] as const

const configureTarget = (tool: typeof tools[number]) => {
  const state = createRun({ hero: createHero({ traversalTools: [tool] }) })
  if (tool === 'antlerPrybar') state.floor.tiles[indexOf(2, 1)].kind = 'boulder'
  else if (tool === 'stoneAdze') state.floor.tiles[indexOf(2, 1)].kind = 'crate'
  else if (tool === 'resinFireBasket') state.floor.tiles[indexOf(2, 1)].kind = 'web'
  else state.floor.props = [{ id: 'cart', kind: 'mine.brokenCart', biome: 'mine', x: 2, y: 1, state: 'dormant', tags: ['route'], hooks: ['operate'] }]
  return state
}

describe('new traversal tool lifecycle', () => {
  it.each(tools)('registers %s with a complete ready/cooldown/overdrive presentation', tool => {
    const definition = toolFor(tool)
    expect(definition).toMatchObject({ id: tool, name: expect.any(String), cooldown: expect.any(Number), text: expect.any(String), overdrive: expect.any(String) })
    expect(definition.cooldown).toBeGreaterThan(0)
  })

  it.each(tools)('preserves %s through save validation and replay planning', tool => {
    const saved = newRun(200 + tools.indexOf(tool))
    saved.hero.traversalTools = [tool]
    saved.hero.cooldowns = { [`tool:${tool}`]: 2 }
    expect(migrateRunRecord(structuredClone(saved))?.hero).toMatchObject({ traversalTools: [tool], cooldowns: { [`tool:${tool}`]: 2 } })
    expect(autoplayToolDiagnostics(configureTarget(tool), 'omniscient')).toEqual(expect.arrayContaining([expect.objectContaining({ tool, overdrive: false, legalDirections: expect.arrayContaining(['e']) })]))
  })

  it.each(['stoneAdze', 'resinFireBasket'] as const)('allows visible replay planning for one-cell %s targets', tool => {
    const state = configureTarget(tool)
    state.floor.tiles[indexOf(3, 1)]!.explored = false
    state.floor.tiles[indexOf(3, 1)]!.visible = false
    expect(autoplayToolDiagnostics(state, 'visible')).toEqual(expect.arrayContaining([expect.objectContaining({ tool, overdrive: false, legalDirections: expect.arrayContaining(['e']) })]))
  })

  it('does not leave a retired tool selectable', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['stoneAdze'] }) })
    state.floor.tiles[1 * state.floor.width + 2]!.kind = 'crate'
    perform(state, 'y'); perform(state, '1'); perform(state, 'o'); perform(state, ';'); perform(state, 'Enter')
    expect(state.hero.traversalTools).toEqual([])
    perform(state, 'y')
    expect(state.messages[0]).toContain('No ritual traversal tool')
  })
})
