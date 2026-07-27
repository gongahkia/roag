import { describe, expect, it } from 'vitest'
import { rankPlacementCandidates, selectPlacement, type PlacementContext } from './placement-contract'

const context = (blocked = false): PlacementContext => ({
  points: [{ x: 1, y: 1 }, { x: 4, y: 1 }, { x: 7, y: 1 }],
  terrainAt: () => 'floor',
  passableAt: () => true,
  blockedAt: point => blocked && point.x === 7,
  distanceFromStart: point => point.x - 1,
  visibleAt: point => point.x < 7,
  coveredAt: point => point.x === 4,
  chokepointAt: point => point.x === 7,
  adjacentTerrainAt: () => ['rail'],
  nodeKindsAt: point => point.x === 4 ? ['landmark'] : [],
  edgeModesAt: point => point.x === 7 ? ['costly', 'optional'] : point.x === 4 ? ['main', 'safe'] : []
})

describe('placement contracts', () => {
  it('ranks only candidates that satisfy spatial-role requirements', () => {
    expect(rankPlacementCandidates({ nodeKinds: ['landmark'], terrain: ['floor'], minDistance: 2, cover: true, adjacentTerrain: ['rail'] }, context())).toEqual([{ point: { x: 4, y: 1 }, score: 23 }])
  })

  it('uses only a declared deterministic fallback and reports primary failure', () => {
    const selection = selectPlacement({ id: 'dead-end-cache', requirements: { nodeKinds: ['optionalReward'] }, fallback: { edgeModes: ['costly'], optional: true, chokepoint: true } }, context())
    expect(selection).toEqual(expect.objectContaining({ point: { x: 7, y: 1 }, debug: expect.objectContaining({ usedFallback: true, diagnostics: [expect.stringMatching(/^placement dead-end-cache/)] }) }))
  })

  it('never relocates a contract when blocking props invalidate every candidate', () => {
    const selection = selectPlacement({ id: 'guarded-shrine', requirements: { edgeModes: ['costly'], optional: true } }, context(true))
    expect(selection.point).toBeUndefined()
    expect(selection.debug.diagnostics).toEqual([expect.stringMatching(/^placement guarded-shrine/)])
  })
})
