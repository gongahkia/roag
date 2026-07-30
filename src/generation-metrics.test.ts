import { describe, expect, it } from 'vitest'
import { measureGeneration, summarizeGenerationMetrics } from './generation-metrics'
import type { MacroRecipeDebug } from './macro-recipe'
import type { RouteContract } from './route-contract'
import { createFloor } from './test/factories'

const macro: MacroRecipeDebug = {
  valid: true,
  recipeId: 'mine:rail-spine',
  topology: 'tight',
  attempt: 0,
  nodes: [
    { nodeId: 'start', kind: 'start', footprint: { x: 1, y: 1, width: 1, height: 1 }, terrain: 'floor', landmark: 'entry', visual: 'entry' },
    { nodeId: 'landmark', kind: 'landmark', footprint: { x: 2, y: 1, width: 1, height: 1 }, terrain: 'floor', landmark: 'shaft', visual: 'landmark' },
    { nodeId: 'fork', kind: 'fork', footprint: { x: 3, y: 1, width: 1, height: 1 }, terrain: 'floor', landmark: 'fork', visual: 'choice' },
    { nodeId: 'optional', kind: 'optionalReward', footprint: { x: 4, y: 2, width: 1, height: 1 }, terrain: 'floor', landmark: 'reward', visual: 'reward' },
    { nodeId: 'objective', kind: 'objective', footprint: { x: 5, y: 1, width: 1, height: 1 }, terrain: 'floor', landmark: 'objective', visual: 'objective' },
    { nodeId: 'exit', kind: 'exit', footprint: { x: 6, y: 1, width: 1, height: 1 }, terrain: 'floor', landmark: 'exit', visual: 'exit' }
  ],
  edges: [
    { edgeId: 'start-landmark', modes: ['main'], from: { x: 1, y: 1 }, to: { x: 2, y: 1 }, cells: [], terrain: 'floor', visual: 'main' },
    { edgeId: 'landmark-fork', modes: ['main'], from: { x: 2, y: 1 }, to: { x: 3, y: 1 }, cells: [], terrain: 'floor', visual: 'main' },
    { edgeId: 'fork-objective', modes: ['main', 'safe'], from: { x: 3, y: 1 }, to: { x: 5, y: 1 }, cells: [], terrain: 'floor', visual: 'safe' },
    { edgeId: 'fork-optional', modes: ['costly', 'optional'], from: { x: 3, y: 1 }, to: { x: 4, y: 2 }, cells: [], terrain: 'floor', visual: 'costly' },
    { edgeId: 'optional-objective', modes: ['costly', 'optional'], from: { x: 4, y: 2 }, to: { x: 5, y: 1 }, cells: [], terrain: 'floor', visual: 'return' },
    { edgeId: 'objective-exit', modes: ['main'], from: { x: 5, y: 1 }, to: { x: 6, y: 1 }, cells: [], terrain: 'floor', visual: 'main' }
  ],
  diagnostics: []
}

const floor = createFloor({ layoutId: 'rail-spine', seed: 42, milestones: [
  { id: 'boon-a', kind: 'boon', x: 2, y: 1, discovered: false, claimed: false },
  { id: 'boon-b', kind: 'boon', x: 3, y: 1, discovered: false, claimed: false },
  { id: 'boon-c', kind: 'boon', x: 4, y: 1, discovered: false, claimed: false }
] })
const input = { floor, route: { id: 'route:mine' } as RouteContract, macro, placements: [{ id: 'actor:guard', selected: { x: 3, y: 1 }, ranked: 3, usedFallback: false, diagnostics: [], requirements: {} }], validation: { valid: true, errors: [] } }

describe('generation metrics', () => {
  it('reports a route-structured recipe with placement and terrain evidence', () => {
    const report = measureGeneration(input)
    expect(report.acceptance).toEqual({ valid: true, errors: [] })
    expect(report.trace).toMatchObject({ seed: 42, biome: 'mine', recipe: 'rail-spine', contract: 'route:mine' })
    expect(report.topology).toMatchObject({ nodes: 6, edges: 6, loops: 1, routeChoices: 2, divergence: 1 })
    expect(report.placements).toMatchObject({ selected: 1, fallbacks: 0 })
    expect(report.terrain.find(entry => entry.id === 'floor')?.count).toBeGreaterThan(0)
    expect(report.boonTiming.boonDistances).toHaveLength(3)
    expect(report.validationFailures).toEqual([])
  })

  it('rejects an open macrograph and confirms recipe variance across the focused sweep', () => {
    const report = measureGeneration(input)
    const open = measureGeneration({ ...input, macro: { ...macro, edges: [] } })
    expect(open.acceptance.errors).toContain('route choices below threshold: 0')
    expect(measureGeneration({ ...input, validation: { valid: false, errors: ['exit unreachable'] } }).validationFailures).toEqual([expect.objectContaining({ seed: 42, biome: 'mine', floor: 0, routeNode: 'floor:0:exit', invariant: 'exit unreachable' })])
    expect(summarizeGenerationMetrics([report, { ...report, trace: { ...report.trace, recipe: 'alternate-recipe' }, topology: { ...report.topology, topology: 'broad' } }]).acceptance).toEqual({ valid: true, errors: [] })
  })
})
