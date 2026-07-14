import { describe, expect, it } from 'vitest'
import { isItemVisible } from './fog'

describe('isItemVisible', () => {
  const item = { id: 'rock', x: 4, y: 7, count: 1 }
  const foggedTile = { kind: 'floor' as const, explored: true, visible: false }

  it('keeps player-left items visible through fog without revealing other items', () => {
    expect(isItemVisible(foggedTile, item)).toBe(false)
    expect(isItemVisible(foggedTile, { ...item, visibleInFog: true })).toBe(true)
  })
})
