import { rngFor } from './rng'
import type { Biome } from './types'

export const ROUTE_NODE_KINDS = ['start', 'landmark', 'fork', 'safeRoute', 'riskRoute', 'objective', 'optionalReward', 'exit', 'boss'] as const
export type RouteNodeKind = typeof ROUTE_NODE_KINDS[number]
export const ROUTE_EDGE_KINDS = ['main', 'safe', 'risk', 'optional'] as const
export type RouteEdgeKind = typeof ROUTE_EDGE_KINDS[number]

export interface RouteNode {
  id: string
  kind: RouteNodeKind
  tags: string[]
}

export interface RouteEdge {
  id: string
  from: string
  to: string
  kind: RouteEdgeKind
  tags: string[]
}

export interface RouteContract {
  id: string
  biome: Biome
  areaFloor: number
  recipeId: string
  escalationVariant: string
  nodes: RouteNode[]
  edges: RouteEdge[]
}

export interface RouteContractValidation { valid: boolean; errors: string[] }

const biomeRouteTags: Record<Biome, readonly [string, string]> = {
  mine: ['rail', 'collapse'],
  wilds: ['water', 'bramble'],
  caverns: ['tide', 'darkness'],
  ruins: ['ward', 'sightline'],
  furnace: ['smoke', 'lift'],
  floodedRuins: ['current', 'anchor'],
  cliffs: ['wind', 'climb'],
  burial: ['ritual', 'spirit'],
  saltFlats: ['brine', 'mirror'],
  frostReliquary: ['ice', 'whiteout']
}

const reaches = (contract: RouteContract, from: string, target: string): boolean => {
  const queue = [from]
  const seen = new Set<string>([from])
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const current = queue[cursor]
    if (current === target) return true
    for (const edge of contract.edges) if (edge.from === current && !seen.has(edge.to)) {
      seen.add(edge.to)
      queue.push(edge.to)
    }
  }
  return false
}

const nodesOfKind = (contract: RouteContract, kind: RouteNodeKind): RouteNode[] => contract.nodes.filter(node => node.kind === kind)

export const validateRouteContract = (contract: RouteContract): RouteContractValidation => {
  const errors: string[] = []
  const nodeIds = new Set<string>()
  for (const node of contract.nodes) {
    if (!node.id || nodeIds.has(node.id)) errors.push(`duplicate or missing node id: ${node.id || '<empty>'}`)
    nodeIds.add(node.id)
    if (!ROUTE_NODE_KINDS.includes(node.kind)) errors.push(`invalid node kind: ${node.kind}`)
  }
  const required: RouteNodeKind[] = ['start', 'landmark', 'fork', 'safeRoute', 'riskRoute', 'objective', 'optionalReward']
  for (const kind of required) if (nodesOfKind(contract, kind).length !== 1) errors.push(`expected one ${kind} node`)
  const endNodes = [...nodesOfKind(contract, 'exit'), ...nodesOfKind(contract, 'boss')]
  if (endNodes.length !== 1) errors.push('expected one end node')
  const edgeIds = new Set<string>()
  for (const edge of contract.edges) {
    if (!edge.id || edgeIds.has(edge.id)) errors.push(`duplicate or missing edge id: ${edge.id || '<empty>'}`)
    edgeIds.add(edge.id)
    if (!ROUTE_EDGE_KINDS.includes(edge.kind)) errors.push(`invalid edge kind: ${edge.kind}`)
    if (!nodeIds.has(edge.from) || !nodeIds.has(edge.to)) errors.push(`edge references unknown node: ${edge.id}`)
  }
  const start = nodesOfKind(contract, 'start')[0]
  const fork = nodesOfKind(contract, 'fork')[0]
  const safe = nodesOfKind(contract, 'safeRoute')[0]
  const risk = nodesOfKind(contract, 'riskRoute')[0]
  const objective = nodesOfKind(contract, 'objective')[0]
  const reward = nodesOfKind(contract, 'optionalReward')[0]
  const end = endNodes[0]
  if (start && objective && !reaches(contract, start.id, objective.id)) errors.push('objective is unreachable from start')
  if (start && end && !reaches(contract, start.id, end.id)) errors.push('end is unreachable from start')
  if (fork && safe && risk) {
    const exits = contract.edges.filter(edge => edge.from === fork.id).map(edge => edge.to)
    if (!exits.includes(safe.id) || !exits.includes(risk.id)) errors.push('fork lacks safe and risk branches')
  }
  if (safe && end && !reaches(contract, safe.id, end.id)) errors.push('safe route does not reach end')
  if (risk && end && !reaches(contract, risk.id, end.id)) errors.push('risk route does not reach end')
  if (reward && start && !reaches(contract, start.id, reward.id)) errors.push('optional reward is unreachable from start')
  return { valid: errors.length === 0, errors }
}

export const generateRouteContract = (runSeed: number, index: number, biome: Biome, areaFloor: number, recipeId: string): RouteContract => {
  const rng = rngFor(runSeed, 'generation', index, 'route-contract')
  const [terrain, pressure] = biomeRouteTags[biome]
  const routeTag = rng.chance(50) ? terrain : pressure
  const endKind: RouteNodeKind = areaFloor === 3 ? 'boss' : 'exit'
  const prefix = `${biome}:${index}:${recipeId}`
  const node = (kind: RouteNodeKind, tags: string[] = []): RouteNode => ({ id: `${prefix}:${kind}`, kind, tags })
  const nodes = [
    node('start', ['entry']),
    node('landmark', ['visible', terrain]),
    node('fork', ['choice', routeTag]),
    node('safeRoute', ['safe', terrain]),
    node('riskRoute', ['risk', pressure]),
    node('objective', [`objective:${areaFloor}`, terrain]),
    node('optionalReward', ['reward', pressure]),
    node(endKind, ['area-end', routeTag])
  ]
  const edge = (from: RouteNodeKind, to: RouteNodeKind, kind: RouteEdgeKind, tags: string[] = []): RouteEdge => ({ id: `${prefix}:${from}:${to}:${kind}`, from: `${prefix}:${from}`, to: `${prefix}:${to}`, kind, tags })
  const edges = [
    edge('start', 'landmark', 'main', ['arrival']),
    edge('landmark', 'fork', 'main', ['orientation']),
    edge('fork', 'safeRoute', 'safe', ['lower-cost']),
    edge('fork', 'riskRoute', 'risk', ['higher-reward', routeTag]),
    edge('safeRoute', 'objective', 'main', [terrain]),
    edge('riskRoute', 'optionalReward', 'optional', [pressure]),
    edge('optionalReward', 'objective', 'optional', ['payoff']),
    edge('riskRoute', 'objective', 'risk', [routeTag]),
    edge('objective', endKind, 'main', ['resolution'])
  ]
  return { id: `route:${prefix}`, biome, areaFloor, recipeId, escalationVariant: `${recipeId}:${rng.int(0, 2)}`, nodes, edges }
}
