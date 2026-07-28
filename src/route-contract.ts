import { rngFor } from './rng'
import { rewardOffersFor } from './reward-contract'
import type { Biome, RewardOffer } from './types'

export const ROUTE_NODE_KINDS = ['start', 'landmark', 'fork', 'objective', 'optionalReward', 'exit', 'boss'] as const
export type RouteNodeKind = typeof ROUTE_NODE_KINDS[number]
export const ROUTE_EDGE_MODES = ['main', 'safe', 'costly', 'optional'] as const
export type RouteEdgeMode = typeof ROUTE_EDGE_MODES[number]

export interface RouteTags {
  terrain: string[]
  encounter: string[]
  reward: string[]
  gate: string[]
  visual: string[]
  escalation: string[]
}

export interface RouteNode {
  id: string
  kind: RouteNodeKind
  tags: RouteTags
}

export interface RouteEdge {
  id: string
  from: string
  to: string
  modes: RouteEdgeMode[]
  tags: RouteTags
}

export interface RouteContractInput {
  campaignSeed: number
  floorIndex: number
  biome: Biome
  areaFloor: number
  recipeId: string
  escalationVariant: string
}

export interface RouteContract extends RouteContractInput {
  id: string
  nodes: RouteNode[]
  edges: RouteEdge[]
  rewardOffers: RewardOffer[]
}

export interface RouteContractValidation { valid: boolean; errors: string[] }

const tagKeys = ['terrain', 'encounter', 'reward', 'gate', 'visual', 'escalation'] as const
const tagSet = (tags: Partial<RouteTags> = {}): RouteTags => ({
  terrain: [...(tags.terrain ?? [])],
  encounter: [...(tags.encounter ?? [])],
  reward: [...(tags.reward ?? [])],
  gate: [...(tags.gate ?? [])],
  visual: [...(tags.visual ?? [])],
  escalation: [...(tags.escalation ?? [])]
})

const biomeRouteTags: Record<Biome, { terrain: string; pressure: string; encounter: string; landmark: string }> = {
  mine: { terrain: 'rail', pressure: 'collapse', encounter: 'mine-guard', landmark: 'shaft' },
  wilds: { terrain: 'water', pressure: 'bramble', encounter: 'wilds-hunter', landmark: 'grove' },
  caverns: { terrain: 'tide', pressure: 'darkness', encounter: 'cavern-stalker', landmark: 'chamber' },
  ruins: { terrain: 'ward', pressure: 'sightline', encounter: 'ruin-sentinel', landmark: 'precinct' },
  furnace: { terrain: 'smoke', pressure: 'lift', encounter: 'cinder-guard', landmark: 'kiln' },
  floodedRuins: { terrain: 'current', pressure: 'anchor', encounter: 'flood-hunter', landmark: 'floodgate' },
  cliffs: { terrain: 'wind', pressure: 'climb', encounter: 'ledge-hunter', landmark: 'anchor' },
  burial: { terrain: 'ritual', pressure: 'spirit', encounter: 'grave-guardian', landmark: 'stone-circle' },
  saltFlats: { terrain: 'brine', pressure: 'mirror', encounter: 'salt-stalker', landmark: 'caravan' },
  frostReliquary: { terrain: 'ice', pressure: 'whiteout', encounter: 'frost-hunter', landmark: 'reliquary' }
}

const nodesOfKind = (contract: RouteContract, kind: RouteNodeKind): RouteNode[] => contract.nodes.filter(node => node.kind === kind)
const edgesFrom = (contract: RouteContract, id: string, predicate: (edge: RouteEdge) => boolean = () => true): RouteEdge[] => contract.edges.filter(edge => edge.from === id && predicate(edge))

const reaches = (contract: RouteContract, from: string, target: string): boolean => {
  const queue = [from]
  const seen = new Set<string>([from])
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const current = queue[cursor]
    if (current === target) return true
    for (const edge of edgesFrom(contract, current)) if (!seen.has(edge.to)) {
      seen.add(edge.to)
      queue.push(edge.to)
    }
  }
  return false
}

const findSimplePaths = (contract: RouteContract, from: string, target: string, predicate: (edge: RouteEdge) => boolean, limit = 2): RouteEdge[][] => {
  const paths: RouteEdge[][] = []
  const visit = (current: string, seen: ReadonlySet<string>, path: RouteEdge[]): void => {
    if (paths.length >= limit) return
    if (current === target) { paths.push(path); return }
    for (const edge of edgesFrom(contract, current, predicate)) if (!seen.has(edge.to)) {
      const next = new Set(seen)
      next.add(edge.to)
      visit(edge.to, next, [...path, edge])
    }
  }
  visit(from, new Set([from]), [])
  return paths
}

const validTagSet = (tags: unknown): tags is RouteTags => {
  if (!tags || typeof tags !== 'object') return false
  const values = tags as Record<string, unknown>
  return tagKeys.every(key => Array.isArray(values[key]) && values[key].length > 0 && values[key].every(value => typeof value === 'string' && value.length > 0))
}

const validateTags = (owner: string, tags: unknown, errors: string[]): void => {
  if (validTagSet(tags)) return
  if (!tags || typeof tags !== 'object') { errors.push(`${owner}: missing tag set`); return }
  for (const key of tagKeys) {
    const values = (tags as Record<string, unknown>)[key]
    if (!Array.isArray(values) || !values.length) errors.push(`${owner}: missing ${key} tags`)
    else if (values.some(value => typeof value !== 'string' || !value.length)) errors.push(`${owner}: invalid ${key} tag`)
  }
}

const validateRewardOffers = (offers: unknown, errors: string[]): void => {
  if (!Array.isArray(offers)) { errors.push('reward offers: missing'); return }
  const expected: Array<[RewardOffer['milestoneId'], RewardOffer['kind']]> = [['waycache', 'waycache'], ['boon-teach', 'boon'], ['boon-test', 'boon'], ['boon-payoff', 'boon']]
  if (offers.length !== expected.length) errors.push(`reward offers: expected ${expected.length}, found ${offers.length}`)
  for (const [milestoneId, kind] of expected) {
    const offer = offers.find(candidate => typeof candidate === 'object' && candidate !== null && (candidate as { milestoneId?: unknown }).milestoneId === milestoneId)
    if (!offer || typeof offer !== 'object') { errors.push(`reward offer ${milestoneId}: missing`); continue }
    const value = offer as Record<string, unknown>
    if (!value.id || typeof value.id !== 'string') errors.push(`reward offer ${milestoneId}: missing id`)
    if (value.kind !== kind) errors.push(`reward offer ${milestoneId}: invalid kind`)
    if (!Array.isArray(value.choices) || value.choices.length !== 3) { errors.push(`reward offer ${milestoneId}: expected three choices`); continue }
    const roles = new Set<string>()
    for (const choice of value.choices) {
      if (!choice || typeof choice !== 'object') { errors.push(`reward offer ${milestoneId}: invalid choice`); continue }
      const annotation = choice as Record<string, unknown>
      if (typeof annotation.id !== 'string' || !annotation.id) errors.push(`reward offer ${milestoneId}: choice missing id`)
      if (annotation.role !== 'safe' && annotation.role !== 'risky' && annotation.role !== 'sidegrade') errors.push(`reward offer ${milestoneId}: choice invalid role`)
      else roles.add(annotation.role)
      if (typeof annotation.problem !== 'string' || typeof annotation.terrain !== 'string' || typeof annotation.route !== 'string' || typeof annotation.payoff !== 'string' || (annotation.biomeFit !== 'local' && annotation.biomeFit !== 'global')) errors.push(`reward offer ${milestoneId}: choice missing annotation`)
    }
    if (roles.size !== 3) errors.push(`reward offer ${milestoneId}: requires safe, risky, and sidegrade choices`)
  }
}

export const validateRouteContract = (contract: RouteContract): RouteContractValidation => {
  const errors: string[] = []
  validateRewardOffers(contract.rewardOffers, errors)
  const nodeIds = new Set<string>()
  for (const node of contract.nodes) {
    const owner = `node ${node.id || '<empty>'}`
    if (!node.id) errors.push(`${owner}: missing id`)
    else if (nodeIds.has(node.id)) errors.push(`${owner}: duplicate id`)
    nodeIds.add(node.id)
    if (!ROUTE_NODE_KINDS.includes(node.kind)) errors.push(`${owner}: invalid kind ${node.kind}`)
    validateTags(owner, node.tags, errors)
  }
  const required: RouteNodeKind[] = ['start', 'landmark', 'fork', 'objective', 'optionalReward']
  for (const kind of required) {
    const count = nodesOfKind(contract, kind).length
    if (count !== 1) errors.push(`node kind ${kind}: expected one, found ${count}`)
  }
  const ends = [...nodesOfKind(contract, 'exit'), ...nodesOfKind(contract, 'boss')]
  if (ends.length !== 1) errors.push(`node kind end: expected one exit or boss, found ${ends.length}`)

  const edgeIds = new Set<string>()
  for (const edge of contract.edges) {
    const owner = `edge ${edge.id || '<empty>'}`
    if (!edge.id) errors.push(`${owner}: missing id`)
    else if (edgeIds.has(edge.id)) errors.push(`${owner}: duplicate id`)
    edgeIds.add(edge.id)
    if (!nodeIds.has(edge.from)) errors.push(`${owner}: unknown source node ${edge.from}`)
    if (!nodeIds.has(edge.to)) errors.push(`${owner}: unknown target node ${edge.to}`)
    if (!edge.modes.length) errors.push(`${owner}: missing route modes`)
    for (const mode of edge.modes) if (!ROUTE_EDGE_MODES.includes(mode)) errors.push(`${owner}: invalid route mode ${mode}`)
    validateTags(owner, edge.tags, errors)
  }

  const start = nodesOfKind(contract, 'start')[0]
  const fork = nodesOfKind(contract, 'fork')[0]
  const reward = nodesOfKind(contract, 'optionalReward')[0]
  const end = ends[0]
  if (start && end) {
    if (!reaches(contract, start.id, end.id)) errors.push(`node ${end.id}: unreachable from start ${start.id}`)
    const mainPaths = findSimplePaths(contract, start.id, end.id, edge => edge.modes.includes('main'))
    if (mainPaths.length !== 1) errors.push(`node ${start.id}: expected exactly one main route to ${end.id}, found ${mainPaths.length}`)
    if (mainPaths.length === 1) {
      const mainPathEdges = new Set(mainPaths[0].map(edge => edge.id))
      for (const edge of contract.edges) if (edge.modes.includes('main') && !mainPathEdges.has(edge.id)) errors.push(`edge ${edge.id}: main route edge is disconnected from the main route`)
    }
  }
  if (fork && end) {
    const choices = edgesFrom(contract, fork.id).filter(edge => nodeIds.has(edge.to))
    const safe = choices.filter(edge => edge.modes.includes('safe'))
    const costly = choices.filter(edge => edge.modes.includes('costly'))
    if (!safe.length) errors.push(`node ${fork.id}: missing safe route choice`)
    if (!costly.length) errors.push(`node ${fork.id}: missing costly route choice`)
    if (safe.length && costly.length && safe.some(edge => costly.some(other => other.to === edge.to))) errors.push(`node ${fork.id}: safe and costly routes share a destination`)
    for (const edge of [...safe, ...costly]) if (!reaches(contract, edge.to, end.id)) errors.push(`edge ${edge.id}: route choice cannot reach ${end.id}`)
  }
  if (reward && start && end) {
    if (!reaches(contract, start.id, reward.id)) errors.push(`node ${reward.id}: optional payoff is unreachable from ${start.id}`)
    if (!reaches(contract, reward.id, end.id)) errors.push(`node ${reward.id}: optional payoff cannot rejoin route to ${end.id}`)
    const optionalEdges = contract.edges.filter(edge => edge.to === reward.id && edge.modes.includes('optional'))
    if (!optionalEdges.length) errors.push(`node ${reward.id}: missing optional route edge`)
  }
  return { valid: errors.length === 0, errors }
}

const assertGenerationInput = (input: RouteContractInput): void => {
  if (!Number.isInteger(input.campaignSeed)) throw new Error('route contract campaign seed must be an integer')
  if (!Number.isInteger(input.floorIndex) || input.floorIndex < 0) throw new Error('route contract floor index must be non-negative')
  if (!Number.isInteger(input.areaFloor) || input.areaFloor < 0 || input.areaFloor > 3) throw new Error('route contract area floor must be between 0 and 3')
  if (!input.recipeId) throw new Error('route contract recipe id is required')
  if (!input.escalationVariant) throw new Error('route contract escalation variant is required')
}

export const generateRouteContract = (input: RouteContractInput): RouteContract => {
  assertGenerationInput(input)
  const rng = rngFor(input.campaignSeed, 'generation', input.floorIndex, 'route-contract', input.biome, input.recipeId, input.escalationVariant)
  const biome = biomeRouteTags[input.biome]
  const routeTerrain = rng.chance(50) ? biome.terrain : biome.pressure
  const endKind: RouteNodeKind = input.areaFloor === 3 ? 'boss' : 'exit'
  const prefix = `${input.biome}:${input.floorIndex}:${input.recipeId}:${input.escalationVariant}`
  const tags = (overrides: Partial<RouteTags>): RouteTags => tagSet({ escalation: [input.escalationVariant], ...overrides })
  const node = (kind: RouteNodeKind, overrides: Partial<RouteTags>): RouteNode => ({ id: `${prefix}:${kind}`, kind, tags: tags(overrides) })
  const nodes = [
    node('start', { terrain: ['entry'], encounter: ['none'], reward: ['none'], gate: ['open'], visual: ['entry-marker'] }),
    node('landmark', { terrain: [biome.terrain], encounter: ['none'], reward: ['orientation'], gate: ['open'], visual: [biome.landmark] }),
    node('fork', { terrain: [routeTerrain], encounter: [biome.encounter], reward: ['choice'], gate: ['open'], visual: ['branch-marker'] }),
    node('objective', { terrain: [biome.terrain], encounter: [biome.encounter], reward: ['objective'], gate: ['key-or-counterroute'], visual: ['objective-marker'] }),
    node('optionalReward', { terrain: [biome.pressure], encounter: [biome.encounter], reward: ['optional-payoff'], gate: ['costly-gate'], visual: ['reward-marker'] }),
    node(endKind, { terrain: [routeTerrain], encounter: [endKind === 'boss' ? 'boss' : 'none'], reward: ['exit-payoff'], gate: ['resolved'], visual: [endKind === 'boss' ? 'boss-marker' : 'exit-marker'] })
  ]
  const edge = (from: RouteNodeKind, to: RouteNodeKind, modes: RouteEdgeMode[], overrides: Partial<RouteTags>): RouteEdge => ({ id: `${prefix}:${from}:${to}:${modes.join('+')}`, from: `${prefix}:${from}`, to: `${prefix}:${to}`, modes, tags: tags(overrides) })
  const edges = [
    edge('start', 'landmark', ['main'], { terrain: ['approach'], encounter: ['none'], reward: ['orientation'], gate: ['open'], visual: ['route-line'] }),
    edge('landmark', 'fork', ['main'], { terrain: [biome.terrain], encounter: [biome.encounter], reward: ['choice'], gate: ['open'], visual: ['fork-line'] }),
    edge('fork', 'objective', ['main', 'safe'], { terrain: [biome.terrain], encounter: ['guarded'], reward: ['objective'], gate: ['key'], visual: ['safe-route'] }),
    edge('fork', 'optionalReward', ['costly', 'optional'], { terrain: [biome.pressure], encounter: ['ambush'], reward: ['optional-payoff'], gate: ['costly-gate'], visual: ['risk-route'] }),
    edge('optionalReward', 'objective', ['costly', 'optional'], { terrain: [routeTerrain], encounter: [biome.encounter], reward: ['payoff'], gate: ['rejoin'], visual: ['return-route'] }),
    edge('objective', endKind, ['main'], { terrain: [routeTerrain], encounter: [endKind === 'boss' ? 'boss' : 'none'], reward: ['completion'], gate: ['resolved'], visual: ['exit-route'] })
  ]
  return { id: `route:${prefix}`, ...input, nodes, edges, rewardOffers: rewardOffersFor(input) }
}
