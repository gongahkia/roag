import { describe, expect, it } from 'vitest'
import { perform } from './input'
import { announceTelegraph } from './telegraphs'
import { encyclopediaEntries, hydrateEncyclopediaLegacy } from './encyclopedia'
import { refreshFov } from './visibility'
import { createEnemy, createLegacy, createRun } from '../test/factories'

describe('encyclopedia', () => {
  it('records seen enemies, telegraphs, tags, gates, and legacy records', () => {
    const state = createRun()
    state.floor.actors = [createEnemy({ id: 'fusewarden-1', kind: 'fusewarden', name: 'Fire Keeper', x: 3, y: 1 })]
    hydrateEncyclopediaLegacy(state, [createLegacy({ heirName: 'Ari' })])
    refreshFov(state)
    announceTelegraph(state, { id: 'shot-1', sourceId: 'fusewarden-1', actionId: 'enemy-shot', cells: [{ x: 1, y: 1 }], danger: 'major', windup: 1 })
    expect(encyclopediaEntries(state, 'enemies')).toContain('Fire Keeper — mine, telegraph, cover, explosive')
    expect(encyclopediaEntries(state, 'telegraphs')).toContain('Shot — ranged, telegraphed')
    expect(encyclopediaEntries(state, 'tags')).toContain('#explosive')
    expect(encyclopediaEntries(state, 'gates')[0]).toContain('Obsidian Mine → Cedar Wilds')
    expect(encyclopediaEntries(state, 'legacy')).toContain('Ari fell in Obsidian Mine 1')
  })

  it('opens and navigates the encyclopedia modal without advancing time', () => {
    const state = createRun()
    expect(perform(state, 'j')).toEqual([{ type: 'menu' }])
    expect(state.modal).toEqual({ kind: 'encyclopedia', section: 'enemies' })
    perform(state, '2')
    expect(state.modal).toEqual({ kind: 'encyclopedia', section: 'telegraphs' })
    perform(state, ']')
    expect(state.modal).toEqual({ kind: 'encyclopedia', section: 'telegraphs', page: 1 })
    expect(state.turn).toBe(0)
  })
})
