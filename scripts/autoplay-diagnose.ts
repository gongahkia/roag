import { autoplayDecision, autoplayStateFingerprint, autoplayTraceFingerprint, createAutoplayContext, recordAutoplayTransition } from '../src/autoplay'
import { perform } from '../src/engine'
import { newRun } from '../src/engine/run'
import { DIRECTIONS, MAP_WIDTH, type AutoplayMode, type AutoplayPolicy, type Biome, type Point, type RunState, type TileKind } from '../src/types'
import { getTile, validateGeneration } from '../src/world'

const biomes = ['mine', 'wilds', 'caverns', 'ruins'] as const
const modes = ['visible', 'omniscient'] as const
const policies = ['survival', 'clear', 'legacy'] as const
const biomeValue = process.env.BIOME ?? 'mine'
const modeValue = process.env.MODE ?? 'omniscient'
const policyValue = process.env.POLICY ?? 'clear'
const seed = Number(process.env.SEED ?? 7)
const turnLimit = Number(process.env.TURNS ?? 800)
if (!biomes.includes(biomeValue as Biome)) throw new Error(`invalid BIOME: ${biomeValue}`)
if (!modes.includes(modeValue as Exclude<AutoplayMode, 'off'>)) throw new Error(`invalid MODE: ${modeValue}`)
if (!policies.includes(policyValue as AutoplayPolicy)) throw new Error(`invalid POLICY: ${policyValue}`)
if (!Number.isInteger(seed) || seed < 0) throw new Error(`invalid SEED: ${process.env.SEED}`)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid TURNS: ${process.env.TURNS}`)

const blocked = new Set<TileKind>(['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest'])
const directionPoints = Object.entries(DIRECTIONS).filter(([direction]) => direction !== 'wait').map(([, point]) => point)
const key = (point: Point) => `${point.x},${point.y}`
const chebyshev = (a: Point, b: Point) => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))

const canWalk = (state: RunState, point: Point, blockActors: boolean): boolean => {
  const tile = getTile(state.floor, point.x, point.y)
  if (!tile || blocked.has(tile.kind) || (tile.kind === 'lockedDoor' && state.hero.keys < 1)) return false
  return !blockActors || !state.floor.actors.some(actor => actor.health > 0 && actor.x === point.x && actor.y === point.y)
}

const canReach = (state: RunState, targets: readonly Point[], adjacent: boolean, blockActors: boolean): boolean => {
  const queue = [{ x: state.hero.x, y: state.hero.y }]
  const seen = new Set([key(queue[0])])
  while (queue.length) {
    const point = queue.shift()!
    if (targets.some(target => adjacent ? chebyshev(point, target) <= 1 : point.x === target.x && point.y === target.y)) return true
    for (const delta of directionPoints) {
      const next = { x: point.x + delta.x, y: point.y + delta.y }
      if (seen.has(key(next)) || !canWalk(state, next, blockActors)) continue
      seen.add(key(next))
      queue.push(next)
    }
  }
  return false
}

const objectivePoints = (state: RunState): Point[] => {
  const objective = state.floor.objective.kind
  if (objective === 'defeatGuardian') return state.floor.actors.filter(actor => actor.role === 'guardian' && actor.health > 0).map(actor => ({ x: actor.x, y: actor.y }))
  const tileKind = objective === 'recoverSupplies' ? ['crate', 'chest'] : objective === 'rescueScout' ? ['rescue'] : ['altar']
  return state.floor.tiles.flatMap((tile, index) => tileKind.includes(tile.kind) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
}

const routeReport = (state: RunState, targets: readonly Point[], adjacent: boolean) => ({
  targets: targets.length,
  terrain: canReach(state, targets, adjacent, false),
  actorClear: canReach(state, targets, adjacent, true)
})

const currentDiagnostics = (state: RunState) => {
  const containers = state.floor.tiles.flatMap((tile, index) => tile.kind === 'crate' || tile.kind === 'chest' ? [{ point: { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }, gold: tile.kind === 'chest' ? 60 : 18 }] : [])
  const reachableContainerGold = containers.filter(container => canReach(state, [container.point], true, false)).reduce((sum, container) => sum + container.gold, 0)
  const targets = objectivePoints(state)
  return {
    exit: routeReport(state, [state.floor.exit], false),
    objective: routeReport(state, targets, true),
    offering: state.floor.objective.kind === 'invokeAltar' ? { gold: state.hero.gold, reachableContainerGold, possibleGold: state.hero.gold + reachableContainerGold, sufficient: state.hero.gold + reachableContainerGold >= 75 } : undefined,
    hostiles: state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => ({ id: actor.id, ai: actor.ai, x: actor.x, y: actor.y, health: actor.health })),
    telegraphs: (state.floor.telegraphs ?? []).map(telegraph => ({ id: telegraph.id, resolveTurn: telegraph.resolveTurn, cells: telegraph.cells }))
  }
}

const state = newRun(seed, biomeValue as Biome)
const initialValidation = validateGeneration(state.floor)
const context = createAutoplayContext()
const trace: Array<{ turn: number; command: string; reason: string; candidates: unknown; events: string[]; before: { x: number; y: number; health: number }; after: { x: number; y: number; health: number } }> = []
let outcome: 'complete' | 'dead' | 'stalled' | 'turn-limit' = 'turn-limit'
let halt = 'turn-limit'
while (state.status === 'playing' && state.turn < turnLimit) {
  const decision = autoplayDecision(state, modeValue as Exclude<AutoplayMode, 'off'>, policyValue as AutoplayPolicy, context)
  if (!decision) {
    const strategicVisits = Math.max(0, ...context.strategicVisits.values())
    halt = (context.visits.get(autoplayStateFingerprint(state)) ?? 0) >= 6 ? 'visit-limit' : strategicVisits >= 5 ? 'strategic-visit-limit' : context.noProgressTurns >= 48 ? 'no-progress-limit' : 'no-viable-candidate'
    outcome = 'stalled'
    break
  }
  const before = structuredClone(state)
  const events = perform(state, decision.command)
  recordAutoplayTransition(context, before, decision.command, state)
  trace.push({ turn: before.turn, command: decision.command, reason: decision.reason, candidates: decision.candidates, events: events.map(event => event.type), before: { x: before.hero.x, y: before.hero.y, health: before.hero.health }, after: { x: state.hero.x, y: state.hero.y, health: state.hero.health } })
  if (events.some(event => event.type === 'areaComplete')) { outcome = 'complete'; halt = 'area-complete'; break }
}
if (state.status === 'dead') { outcome = 'dead'; halt = 'hero-dead' }

console.log(JSON.stringify({
  seed,
  biome: biomeValue,
  mode: modeValue,
  policy: policyValue,
  initialValidation,
  outcome,
  halt,
  turn: state.turn,
  fingerprint: autoplayTraceFingerprint(state),
  visits: context.visits.get(autoplayStateFingerprint(state)) ?? 0,
  strategicVisits: Math.max(0, ...context.strategicVisits.values()),
  noProgressTurns: context.noProgressTurns,
  lastReason: context.lastReason,
  failed: [...context.failed.entries()].map(([command, count]) => ({ command, count })),
  recentPositions: context.recentPositions,
  state: { hero: { x: state.hero.x, y: state.hero.y, health: state.hero.health, maxHealth: state.hero.maxHealth, focus: state.hero.focus, gold: state.hero.gold, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys, inventory: state.hero.inventory }, objective: state.floor.objective, guardianDefeated: state.floor.guardianDefeated },
  routes: currentDiagnostics(state),
  trace: trace.slice(-12)
}, null, 2))
