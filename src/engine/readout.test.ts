import { describe, expect, it } from 'vitest'
import { perform } from './input'
import { fieldReadout } from './readout'
import { announceTelegraph } from './telegraphs'
import { createEnemy, createRun } from '../test/factories'

describe('field readout', () => {
  it('exposes visible intents, timed danger, and local options without advancing time', () => {
    const state = createRun()
    const foe = createEnemy({ id: 'watcher', name: 'Watch Rat', ai: 'ranged', x: 5, y: 1 })
    state.floor.actors = [foe]
    state.floor.items = [{ id: 'tonic', x: 1, y: 1, count: 1 }]
    announceTelegraph(state, { id: 'watcher-shot', sourceId: foe.id, actionId: 'enemy-shot', cells: [{ x: 1, y: 1 }], danger: 'major', windup: 2 })
    const readout = fieldReadout(state)
    expect(readout.brief).toContain('T-2 MAJ SHOT PATH Watch Rat')
    expect(readout.lines).toContain('INTENT: Watch Rat — Shot (clear line at range 4; pursuer: use terrain or reach to control approach)')
    expect(readout.lines).toContain('OPTION G: take Vital Gel')
    expect(perform(state, 'z')).toEqual([{ type: 'menu' }])
    expect(state.modal).toEqual({ kind: 'readout' })
    expect(state.turn).toBe(0)
    perform(state, 'Escape')
    expect(state.modal).toBeUndefined()
  })
})
