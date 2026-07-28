import { describe, expect, it } from 'vitest'
import { isTelegraphVisible, presentTelegraph } from './telegraphs'

describe('telegraph presentation', () => {
  it('exposes impact timing, source, and danger class', () => {
    expect(presentTelegraph({ id: 'shot-1', sourceId: 'sapper-1', actionId: 'enemy-shot', cells: [], danger: 'major', resolveTurn: 9, collision: { point: { x: 2, y: 1 }, by: 'target' } }, 7, 'Powder Sapper')).toEqual({ glyph: '!', color: '#ee6f78', label: 'T-2 MAJ SHOT HIT Powder Sapper' })
  })

  it('uses a distinct minor marker after its impact turn', () => {
    expect(presentTelegraph({ id: 'mark-1', sourceId: 'rat-1', actionId: 'enemy-strike', cells: [], danger: 'minor', resolveTurn: 3 }, 5, 'Tunnel Rat')).toMatchObject({ glyph: ':', label: 'T-0 MIN ENEMY-STRIKE PATH Tunnel Rat' })
  })

  it('keeps hidden telegraphs out of terminal threat panels', () => {
    const floor = { width: 2, height: 1, tiles: [{ visible: false }, { visible: true }], actors: [{ id: 'source', x: 0, y: 0 }] }
    const hidden = { id: 'hidden', sourceId: 'source', actionId: 'enemy-shot', cells: [{ x: 0, y: 0 }], danger: 'major' as const, resolveTurn: 2 }
    const visible = { ...hidden, id: 'visible', cells: [{ x: 1, y: 0 }] }
    expect(isTelegraphVisible(floor, hidden)).toBe(false)
    expect(isTelegraphVisible(floor, visible)).toBe(true)
  })
})
