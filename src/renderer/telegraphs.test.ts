import { describe, expect, it } from 'vitest'
import { presentTelegraph } from './telegraphs'

describe('telegraph presentation', () => {
  it('exposes impact timing, source, and danger class', () => {
    expect(presentTelegraph({ id: 'shot-1', sourceId: 'sapper-1', actionId: 'enemy-shot', cells: [], danger: 'major', resolveTurn: 9, collision: { point: { x: 2, y: 1 }, by: 'target' } }, 7, 'Powder Sapper')).toEqual({ glyph: '!', color: '#ee6f78', label: 'T-2 MAJ HIT Powder Sapper' })
  })

  it('uses a distinct minor marker after its impact turn', () => {
    expect(presentTelegraph({ id: 'mark-1', sourceId: 'rat-1', actionId: 'enemy-strike', cells: [], danger: 'minor', resolveTurn: 3 }, 5, 'Tunnel Rat')).toMatchObject({ glyph: ':', label: 'T-0 MIN PATH Tunnel Rat' })
  })
})
