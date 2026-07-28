import type { MacroRecipeDebug } from './macro-recipe'
import type { PlacementDebug } from './placement-contract'
import type { RouteContract } from './route-contract'
import type { Floor, Point } from './types'
import type { GenerationValidation } from './world'
import { traverseFloor } from './world'

export interface GenerationMetricInput {
  floor: Floor
  route?: RouteContract
  macro?: MacroRecipeDebug
  placements?: readonly PlacementDebug[]
  validation: GenerationValidation
}

export interface Count { id: string; count: number }
export interface GenerationMetrics {
  trace: { seed: number; biome: Floor['biome']; floor: number; recipe: string; contract?: string }
  topology: { nodes: number; edges: number; degrees: Count[]; chokepoints: string[]; loops: number; routeChoices: number; divergence: number; topology?: string }
  placements: { total: number; selected: number; fallbacks: number; rejected: number; roles: Array<{ id: string; selected: boolean; fallback: boolean; ranked: number; diagnostics: string[] }> }
  terrain: Count[]
  encounters: { events: Count[]; actors: Count[] }
  boonTiming: { milestones: Count[]; boonDistances: number[]; averageBoonDistance: number }
  validation: GenerationValidation
  acceptance: { valid: boolean; errors: string[] }
}

export interface GenerationMetricSummary {
  samples: number
  accepted: number
  failures: Array<{ trace: GenerationMetrics['trace']; errors: string[] }>
  recipes: Count[]
  topologies: Count[]
  acceptance: { valid: boolean; errors: string[] }
}

const count = (ids: readonly string[]): Count[] => {
  const values = new Map<string, number>()
  for (const id of ids) values.set(id, (values.get(id) ?? 0) + 1)
  return [...values].map(([id, value]) => ({ id, count: value })).sort((left, right) => left.id.localeCompare(right.id))
}

const distanceTo = (floor: Floor, point: Point): number | undefined => {
  const path = traverseFloor(floor, floor.start, { target: point }).path
  return path ? path.length - 1 : undefined
}

const topology = (macro: MacroRecipeDebug | undefined): GenerationMetrics['topology'] => {
  if (!macro) return { nodes: 0, edges: 0, degrees: [], chokepoints: [], loops: 0, routeChoices: 0, divergence: 0 }
  const ids = macro.nodes.map(node => node.nodeId)
  const graph = new Map(ids.map(id => [id, new Set<string>()]))
  const endpoint = (point: Point): string | undefined => macro.nodes.find(node => node.footprint.x <= point.x && point.x < node.footprint.x + node.footprint.width && node.footprint.y <= point.y && point.y < node.footprint.y + node.footprint.height)?.nodeId
  const edgeEndpoints = macro.edges.map(edge => ({ from: endpoint(edge.from), to: endpoint(edge.to) }))
  for (const { from, to } of edgeEndpoints) {
    if (!from || !to) continue
    graph.get(from)?.add(to)
    graph.get(to)?.add(from)
  }
  const start = macro.nodes.find(node => node.kind === 'start')?.nodeId
  const end = macro.nodes.find(node => node.kind === 'exit' || node.kind === 'boss')?.nodeId
  const connectedWithout = (removed?: string): boolean => {
    if (!start || !end || removed === start || removed === end) return false
    const seen = new Set<string>([start])
    const queue = [start]
    for (let index = 0; index < queue.length; index++) for (const next of graph.get(queue[index]) ?? []) if (next !== removed && !seen.has(next)) { seen.add(next); queue.push(next) }
    return seen.has(end)
  }
  const components = (() => {
    const unseen = new Set(ids)
    let value = 0
    while (unseen.size) {
      value++
      const [first] = unseen
      unseen.delete(first)
      const queue = [first]
      for (let index = 0; index < queue.length; index++) for (const next of graph.get(queue[index]) ?? []) if (unseen.delete(next)) queue.push(next)
    }
    return value
  })()
  const forks = macro.nodes.filter(node => node.kind === 'fork')
  const routeChoices = Math.max(0, ...forks.map(node => new Set(edgeEndpoints.filter(edge => edge.from === node.nodeId).map(edge => edge.to).filter((id): id is string => Boolean(id))).size))
  const divergence = forks.filter(node => new Set(edgeEndpoints.filter(edge => edge.from === node.nodeId).map(edge => edge.to).filter((id): id is string => Boolean(id))).size >= 2).length
  return {
    nodes: ids.length,
    edges: macro.edges.length,
    degrees: [...graph].map(([id, neighbors]) => ({ id, count: neighbors.size })).sort((left, right) => left.id.localeCompare(right.id)),
    chokepoints: ids.filter(id => id !== start && id !== end && !connectedWithout(id)).sort(),
    loops: Math.max(0, macro.edges.length - ids.length + components),
    routeChoices,
    divergence,
    topology: macro.topology
  }
}

const acceptance = (validation: GenerationValidation, macro: MacroRecipeDebug | undefined, metrics: GenerationMetrics['topology']): GenerationMetrics['acceptance'] => {
  const errors = validation.valid ? [] : validation.errors.map(error => `validation: ${error}`)
  if (!macro?.valid) errors.push('macro recipe is invalid')
  if (!macro?.nodes.some(node => node.kind === 'landmark')) errors.push('missing landmark')
  if (!macro?.nodes.some(node => node.kind === 'optionalReward')) errors.push('missing optional payoff')
  if (metrics.routeChoices < 2) errors.push(`route choices below threshold: ${metrics.routeChoices}`)
  return { valid: !errors.length, errors }
}

export const measureGeneration = ({ floor, route, macro, placements = [], validation }: GenerationMetricInput): GenerationMetrics => {
  const measuredTopology = topology(macro)
  const boonDistances = floor.milestones.filter(milestone => milestone.kind === 'boon').map(distance => distanceTo(floor, distance)).filter((distance): distance is number => distance !== undefined).sort((left, right) => left - right)
  return {
    trace: { seed: route?.campaignSeed ?? floor.seed, biome: floor.biome, floor: floor.index % 4, recipe: floor.layoutId, ...(route ? { contract: route.id } : {}) },
    topology: measuredTopology,
    placements: { total: placements.length, selected: placements.filter(placement => placement.selected).length, fallbacks: placements.filter(placement => placement.usedFallback).length, rejected: placements.filter(placement => !placement.selected).length, roles: placements.map(placement => ({ id: placement.id, selected: Boolean(placement.selected), fallback: placement.usedFallback, ranked: placement.ranked, diagnostics: [...placement.diagnostics] })) },
    terrain: count(floor.tiles.map(tile => tile.kind)),
    encounters: { events: count((floor.encounters ?? []).map(encounter => encounter.kind)), actors: count(floor.actors.map(actor => actor.kind)) },
    boonTiming: { milestones: count(floor.milestones.map(milestone => milestone.kind)), boonDistances, averageBoonDistance: boonDistances.length ? Number((boonDistances.reduce((sum, distance) => sum + distance, 0) / boonDistances.length).toFixed(2)) : 0 },
    validation,
    acceptance: acceptance(validation, macro, measuredTopology)
  }
}

export const summarizeGenerationMetrics = (samples: readonly GenerationMetrics[]): GenerationMetricSummary => {
  const failures = samples.filter(sample => !sample.acceptance.valid).map(sample => ({ trace: sample.trace, errors: sample.acceptance.errors }))
  const recipes = count(samples.map(sample => sample.trace.recipe))
  const topologies = count(samples.map(sample => sample.topology.topology ?? 'missing'))
  const errors = failures.map(failure => `seed=${failure.trace.seed} biome=${failure.trace.biome} floor=${failure.trace.floor} recipe=${failure.trace.recipe}: ${failure.errors.join('; ')}`)
  if (new Set(samples.map(sample => `${sample.trace.recipe}:${sample.topology.topology ?? 'missing'}`)).size < 2) errors.push('macrograph variance below threshold: fewer than two recipe/topology signatures')
  return { samples: samples.length, accepted: samples.length - failures.length, failures, recipes, topologies, acceptance: { valid: !errors.length, errors } }
}
