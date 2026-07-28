import { rngFor } from './rng'
import type { Biome, Point } from './types'
import type { RouteContract, RouteEdge, RouteEdgeMode, RouteNodeKind } from './route-contract'

export const MACRO_TOPOLOGIES = ['tight', 'broad', 'vertical', 'directedFlow', 'looped'] as const
export type MacroTopology = typeof MACRO_TOPOLOGIES[number]

export interface MacroFootprint { width: number; height: number }
export interface MacroNodeRole { footprint: MacroFootprint; landmark: string; terrain: string; visual: string }
export interface MacroEdgeRole { connector: 'horizontalFirst' | 'verticalFirst'; terrain: string; visual: string }
export interface RouteMacroRecipe {
  id: string
  biome: Biome
  topology: MacroTopology
  pilot: boolean
  nodeRoles: Record<RouteNodeKind, MacroNodeRole>
  edgeRoles: Record<RouteEdgeMode, MacroEdgeRole>
  anchors: Record<RouteNodeKind, Point>
}

export interface MacroNodePlacement { nodeId: string; kind: RouteNodeKind; footprint: { x: number; y: number; width: number; height: number }; terrain: string; landmark: string; visual: string }
export interface MacroEdgePlacement { edgeId: string; modes: RouteEdgeMode[]; from: Point; to: Point; cells: Point[]; terrain: string; visual: string }
export interface MacroRecipeDebug {
  valid: boolean
  recipeId: string
  topology: MacroTopology
  attempt: number
  nodes: MacroNodePlacement[]
  edges: MacroEdgePlacement[]
  diagnostics: string[]
}

export interface MacroCompileOptions { width: number; height: number; attempts?: number }

const role = (width: number, height: number, landmark: string, terrain: string, visual: string): MacroNodeRole => ({ footprint: { width, height }, landmark, terrain, visual })
const edgeRole = (connector: MacroEdgeRole['connector'], terrain: string, visual: string): MacroEdgeRole => ({ connector, terrain, visual })
const nodeRoles = (landmark: string, terrain: string): Record<RouteNodeKind, MacroNodeRole> => ({
  start: role(6, 6, 'entry', 'floor', 'entry-marker'),
  landmark: role(7, 6, landmark, terrain, 'landmark-marker'),
  fork: role(6, 6, 'fork', terrain, 'choice-marker'),
  objective: role(7, 6, 'objective', terrain, 'objective-marker'),
  optionalReward: role(6, 6, 'reward', terrain, 'reward-marker'),
  exit: role(4, 5, 'exit', 'floor', 'exit-marker'),
  boss: role(7, 7, 'boss', terrain, 'boss-marker')
})
const edgeRoles = (terrain: string): Record<RouteEdgeMode, MacroEdgeRole> => ({
  main: edgeRole('horizontalFirst', terrain, 'main-route'),
  safe: edgeRole('horizontalFirst', terrain, 'safe-route'),
  costly: edgeRole('verticalFirst', terrain, 'costly-route'),
  optional: edgeRole('verticalFirst', terrain, 'optional-route')
})

const tight: Record<RouteNodeKind, Point> = { start: { x: 11, y: 48 }, landmark: { x: 28, y: 48 }, fork: { x: 48, y: 48 }, objective: { x: 76, y: 48 }, optionalReward: { x: 63, y: 77 }, exit: { x: 92, y: 48 }, boss: { x: 91, y: 48 } }
const broad: Record<RouteNodeKind, Point> = { start: { x: 10, y: 52 }, landmark: { x: 28, y: 30 }, fork: { x: 49, y: 52 }, objective: { x: 72, y: 28 }, optionalReward: { x: 74, y: 78 }, exit: { x: 93, y: 52 }, boss: { x: 91, y: 52 } }
const vertical: Record<RouteNodeKind, Point> = { start: { x: 22, y: 84 }, landmark: { x: 32, y: 64 }, fork: { x: 45, y: 48 }, objective: { x: 70, y: 18 }, optionalReward: { x: 76, y: 66 }, exit: { x: 91, y: 12 }, boss: { x: 91, y: 14 } }
const directedFlow: Record<RouteNodeKind, Point> = { start: { x: 9, y: 28 }, landmark: { x: 28, y: 68 }, fork: { x: 48, y: 42 }, objective: { x: 80, y: 70 }, optionalReward: { x: 70, y: 18 }, exit: { x: 93, y: 38 }, boss: { x: 91, y: 42 } }
const looped: Record<RouteNodeKind, Point> = { start: { x: 10, y: 50 }, landmark: { x: 27, y: 26 }, fork: { x: 48, y: 42 }, objective: { x: 74, y: 50 }, optionalReward: { x: 48, y: 77 }, exit: { x: 93, y: 50 }, boss: { x: 91, y: 50 } }

const recipe = (id: string, biome: Biome, topology: MacroTopology, anchors: Record<RouteNodeKind, Point>, pilot = false): RouteMacroRecipe => ({ id, biome, topology, pilot, nodeRoles: nodeRoles(`${biome}-landmark`, topology === 'directedFlow' ? 'current' : topology === 'vertical' ? 'ledge' : 'floor'), edgeRoles: edgeRoles(topology === 'directedFlow' ? 'current' : topology === 'vertical' ? 'ledge' : 'floor'), anchors })
const recipes: RouteMacroRecipe[] = [
  recipe('mine:rail-spine', 'mine', 'tight', tight, true),
  recipe('mine:branching-drifts', 'mine', 'broad', broad, true),
  recipe('mine:collapse-loop', 'mine', 'looped', looped, true),
  recipe('caverns:tide-chambers', 'caverns', 'directedFlow', directedFlow, true),
  recipe('caverns:sinkhole-galleries', 'caverns', 'vertical', vertical, true),
  recipe('caverns:fault-tunnels', 'caverns', 'looped', looped, true),
  recipe('wilds:legacy', 'wilds', 'broad', broad),
  recipe('caverns:legacy', 'caverns', 'tight', tight),
  recipe('ruins:legacy', 'ruins', 'looped', looped),
  recipe('furnace:legacy', 'furnace', 'vertical', vertical),
  recipe('floodedRuins:legacy', 'floodedRuins', 'directedFlow', directedFlow),
  recipe('cliffs:legacy', 'cliffs', 'vertical', vertical),
  recipe('burial:legacy', 'burial', 'looped', looped),
  recipe('saltFlats:legacy', 'saltFlats', 'broad', broad),
  recipe('frostReliquary:legacy', 'frostReliquary', 'tight', tight)
]

const baseRecipeId = (id: string): string => id.replace(/-remix$/, '')
export const macroRecipeFor = (contract: RouteContract): RouteMacroRecipe => recipes.find(candidate => candidate.id === `${contract.biome}:${baseRecipeId(contract.recipeId)}`) ?? recipes.find(candidate => candidate.id === `${contract.biome}:legacy`)!

const center = (footprint: MacroNodePlacement['footprint']): Point => ({ x: footprint.x + Math.floor(footprint.width / 2), y: footprint.y + Math.floor(footprint.height / 2) })
const inside = (point: Point, width: number, height: number): boolean => point.x > 0 && point.y > 0 && point.x < width - 1 && point.y < height - 1
const overlaps = (left: MacroNodePlacement['footprint'], right: MacroNodePlacement['footprint']): boolean => left.x < right.x + right.width && left.x + left.width > right.x && left.y < right.y + right.height && left.y + left.height > right.y
const same = (left: Point, right: Point): boolean => left.x === right.x && left.y === right.y
const axisPath = (from: Point, to: Point, horizontalFirst: boolean): Point[] => {
  const cells: Point[] = []
  const append = (point: Point): void => { if (!cells.length || !same(cells[cells.length - 1], point)) cells.push(point) }
  const line = (start: Point, end: Point): void => {
    const dx = Math.sign(end.x - start.x)
    const dy = Math.sign(end.y - start.y)
    for (let x = start.x, y = start.y; ; x += dx, y += dy) {
      append({ x, y })
      if (x === end.x && y === end.y) return
    }
  }
  const bend = horizontalFirst ? { x: to.x, y: from.y } : { x: from.x, y: to.y }
  line(from, bend)
  line(bend, to)
  return cells
}
const connectorPoint = (from: MacroNodePlacement['footprint'], to: MacroNodePlacement['footprint']): Point => {
  const fromCenter = center(from)
  const toCenter = center(to)
  if (Math.abs(toCenter.x - fromCenter.x) >= Math.abs(toCenter.y - fromCenter.y)) return { x: toCenter.x >= fromCenter.x ? from.x + from.width - 1 : from.x, y: Math.max(from.y, Math.min(from.y + from.height - 1, toCenter.y)) }
  return { x: Math.max(from.x, Math.min(from.x + from.width - 1, toCenter.x)), y: toCenter.y >= fromCenter.y ? from.y + from.height - 1 : from.y }
}
const edgeMode = (edge: RouteEdge): RouteEdgeMode => edge.modes.includes('optional') ? 'optional' : edge.modes.includes('costly') ? 'costly' : edge.modes.includes('safe') ? 'safe' : 'main'

const compileAttempt = (contract: RouteContract, recipe: RouteMacroRecipe, options: MacroCompileOptions, attempt: number): MacroRecipeDebug => {
  const diagnostics: string[] = []
  const jitter = attempt ? rngFor(contract.campaignSeed, 'generation', contract.floorIndex, 'macro-retry', recipe.id, attempt) : undefined
  const nodes: MacroNodePlacement[] = []
  for (const node of contract.nodes) {
    const role = recipe.nodeRoles[node.kind]
    if (!role) { diagnostics.push(`node ${node.id}: recipe ${recipe.id} has no role`); continue }
    const anchor = recipe.anchors[node.kind]
    const x = Math.round(anchor.x / 100 * (options.width - 1)) - Math.floor(role.footprint.width / 2) + (jitter?.int(-2, 2) ?? 0)
    const y = Math.round(anchor.y / 100 * (options.height - 1)) - Math.floor(role.footprint.height / 2) + (jitter?.int(-2, 2) ?? 0)
    const footprint = { x, y, ...role.footprint }
    if (!inside({ x, y }, options.width, options.height) || !inside({ x: x + footprint.width - 1, y: y + footprint.height - 1 }, options.width, options.height)) diagnostics.push(`node ${node.id}: footprint exceeds ${options.width}x${options.height} on attempt ${attempt}`)
    const placed: MacroNodePlacement = { nodeId: node.id, kind: node.kind, footprint, terrain: role.terrain, landmark: role.landmark, visual: role.visual }
    const previous = nodes.find(other => overlaps(other.footprint, footprint))
    if (previous) diagnostics.push(`node ${node.id}: footprint overlaps node ${previous.nodeId} on attempt ${attempt}`)
    nodes.push(placed)
  }
  const byId = new Map(nodes.map(node => [node.nodeId, node]))
  const edges: MacroEdgePlacement[] = []
  for (const edge of contract.edges) {
    const from = byId.get(edge.from)
    const to = byId.get(edge.to)
    if (!from || !to) { diagnostics.push(`edge ${edge.id}: missing endpoint footprint on attempt ${attempt}`); continue }
    const role = recipe.edgeRoles[edgeMode(edge)]
    const fromPoint = connectorPoint(from.footprint, to.footprint)
    const toPoint = connectorPoint(to.footprint, from.footprint)
    const cells = axisPath(fromPoint, toPoint, role.connector === 'horizontalFirst')
    const invalid = cells.find(point => !inside(point, options.width, options.height))
    if (invalid) diagnostics.push(`edge ${edge.id}: connector leaves bounds at ${invalid.x},${invalid.y} on attempt ${attempt}`)
    edges.push({ edgeId: edge.id, modes: [...edge.modes], from: fromPoint, to: toPoint, cells, terrain: role.terrain, visual: role.visual })
  }
  return { valid: !diagnostics.length, recipeId: recipe.id, topology: recipe.topology, attempt, nodes, edges, diagnostics }
}

export const compileRouteContract = (contract: RouteContract, options: MacroCompileOptions): MacroRecipeDebug => {
  const recipe = macroRecipeFor(contract)
  const attempts = options.attempts ?? 3
  let failed: MacroRecipeDebug | undefined
  for (let attempt = 0; attempt < attempts; attempt++) {
    const compiled = compileAttempt(contract, recipe, options, attempt)
    if (compiled.valid) return compiled
    failed = compiled
  }
  return { ...failed!, diagnostics: Array.from({ length: attempts }, (_, attempt) => compileAttempt(contract, recipe, options, attempt).diagnostics.map(error => `attempt ${attempt}: ${error}`)).flat() }
}

export const macroConnectorPoints = (debug: MacroRecipeDebug): Point[] => debug.edges.flatMap(edge => edge.cells)
export const validateMacroRealization = (debug: MacroRecipeDebug, traversable: (point: Point) => boolean): string[] => debug.edges.flatMap(edge => {
  const blocked = edge.cells.find(point => !traversable(point))
  return blocked ? [`edge ${edge.edgeId}: connector blocked at ${blocked.x},${blocked.y}`] : []
})
