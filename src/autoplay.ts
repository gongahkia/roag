import { ITEM } from './content'
import { actionCells, perform, skillChoices } from './engine'
import { agilityMoveDistance, agilityReachBonus } from './engine/agility'
import { evaluateEquipmentEffects } from './engine/equipment'
import { resolveAreaGate, gateForArea } from './engine/gates'
import { canAffect, resolveLineEffect } from './engine/line-effect'
import { projectBolt } from './engine/projectiles'
import { merchantStock } from './engine/rewards'
import { scriptCastProfile } from './engine/scripts'
import { resolveSynergies } from './engine/synergies'
import { DIRECTIONS, MAP_WIDTH, type AutoplayCandidate, type AutoplayMode, type AutoplayPolicy, type Direction, type Point, type Prop, type PropEffectKind, type RunState, type TileKind } from './types'
import { actorAt, getTile, hasPassablePath } from './world'
import { isBlockingProp, propAt } from './props'

export const AUTOPLAY_TURN_MS = Math.round(1000 / 6)
export const AUTOPLAY_MAX_TURNS = 800
const AUTOPLAY_MAX_NON_TURN_COMMANDS = 8
const AUTOPLAY_MAX_RECOVERY_REPEATS = 8
export const autoplayModes: readonly AutoplayMode[] = ['off', 'visible', 'omniscient']
export const autoplayPolicies: readonly AutoplayPolicy[] = ['survival', 'clear', 'legacy']
const directionCommands: Record<Direction, string> = { nw: 'i', n: 'o', ne: 'p', w: 'k', wait: 'l', e: ';', sw: ',', s: '.', se: '/' }
const blockedTiles = new Set<TileKind>(['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest'])
const hazardTiles = new Set<TileKind>(['spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder'])
const directions = (Object.entries(DIRECTIONS) as Array<[Direction, Point]>).filter(([direction]) => direction !== 'wait') as Array<[Exclude<Direction, 'wait'>, Point]>
const pointKey = (point: Point): string => `${point.x},${point.y}`
const chebyshev = (a: Point, b: Point): number => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))
const propFingerprint = (prop: Prop): string => `${prop.id}:${prop.kind}:${prop.x},${prop.y}:${prop.state}:${prop.tags.join(',')}:${prop.hooks?.join(',') ?? '-'}:${prop.effectCells?.map(pointKey).sort().join(',') ?? '-'}:${prop.expiresAt ?? '-'}`
const isStrategicRouteReason = (reason: string | undefined): boolean => Boolean(reason && (reason === 'reach exit' || reason === 'operate objective' || reason.startsWith('objective:') || reason.startsWith('predictive objective route:') || reason.startsWith('predictive exit route') || reason.startsWith('continue predictive objective route:') || reason.startsWith('continue predictive exit route') || reason.startsWith('clear objective route:') || reason.startsWith('clear telegraph source:') || reason.startsWith('prop route:') || reason.startsWith('secure prop route:')))
const planningClone = (state: RunState): RunState => {
  const cloneConditions = <T extends { conditions?: Array<{ kind: string; duration: number; potency: number }> }>(target: T): T => ({ ...target, conditions: target.conditions?.map(condition => ({ ...condition })) })
  const floor = state.floor
  return {
    ...state,
    messages: [...state.messages],
    modal: state.modal ? { ...state.modal } : undefined,
    hero: {
      ...cloneConditions(state.hero),
      stats: { ...state.hero.stats },
      skills: [...state.hero.skills],
      inventory: [...state.hero.inventory],
      equipment: { ...state.hero.equipment },
      cooldowns: state.hero.cooldowns ? { ...state.hero.cooldowns } : undefined
    },
    floor: {
      ...floor,
      tiles: floor.tiles.map(tile => ({ ...tile })),
      actors: floor.actors.map(actor => ({ ...cloneConditions(actor), status: actor.status ? [...actor.status] : undefined })),
      items: floor.items.map(item => ({ ...item })),
      props: floor.props.map(prop => ({ ...prop, tags: [...prop.tags], hooks: prop.hooks ? [...prop.hooks] : undefined, effectCells: prop.effectCells?.map(point => ({ ...point })) })),
      start: { ...floor.start },
      exit: { ...floor.exit },
      objective: { ...floor.objective },
      telegraphs: floor.telegraphs?.map(telegraph => ({ ...telegraph, cells: telegraph.cells.map(cell => ({ ...cell })), collision: telegraph.collision ? { ...telegraph.collision, point: { ...telegraph.collision.point } } : undefined })),
      puzzleIds: floor.puzzleIds ? [...floor.puzzleIds] : undefined
    },
    rescuedNpcs: state.rescuedNpcs?.map(npc => ({ ...npc })),
    lineageEvents: state.lineageEvents?.map(event => ({ ...event })),
    encyclopedia: state.encyclopedia ? { ...state.encyclopedia, enemies: [...state.encyclopedia.enemies], telegraphs: [...state.encyclopedia.telegraphs], tags: [...state.encyclopedia.tags], gates: [...state.encyclopedia.gates], legacyRecords: state.encyclopedia.legacyRecords.map(record => ({ ...record, lineage: [...record.lineage], location: { ...record.location }, cache: { ...record.cache, items: [...record.cache.items] }, encounter: { ...record.encounter } })) } : undefined
  }
}

type Intent = { kind: 'use' | 'throw' | 'equip' | 'drop'; item: string }
type RoutePlan = { kind: 'objective' | 'exit'; targetKey: string; commands: string[] }
type TelegraphRoute = { sourceId: string; from: string; to: string }
type TargetOutcome = { direction: Exclude<Direction, 'wait'>; score: number; mobilityGain: number; terrainCleared: number }
type Candidate = AutoplayCandidate & { intent?: Intent; routePlan?: RoutePlan; propPlanId?: string; telegraphRoute?: TelegraphRoute }
export interface AutoplayDecision { command: string; reason: string; candidates: AutoplayCandidate[] }
export interface AutoplayContext { visits: Map<string, number>; strategicVisits: Map<string, number>; failed: Map<string, number>; recoveryVisits: Map<string, number>; closedMerchants: Set<string>; rejectedObjectiveTargets: Set<string>; recentPositions: string[]; intent?: Intent; objectiveId?: string; objectiveTarget?: string; objectiveTargetCount: number; propPlanId?: string; routePlan?: RoutePlan; lastTelegraphRoute?: TelegraphRoute; bestStrategicDistance?: number; startedTurn?: number; shopTurns: number; noProgressTurns: number; noTurnCommands: number; loopRecoveries: number; lastReason?: string }
export interface AutoplayTransitionSnapshot { stateKey: string; progressKey: string; position: string; strategicDistance: number; area?: string; areaFloor?: number; objectiveId: string; objectiveStatus: string; guardianDefeated: boolean; turn: number; modal?: string }

export const createAutoplayContext = (): AutoplayContext => ({ visits: new Map(), strategicVisits: new Map(), failed: new Map(), recoveryVisits: new Map(), closedMerchants: new Set(), rejectedObjectiveTargets: new Set(), recentPositions: [], objectiveTargetCount: 0, shopTurns: 0, noProgressTurns: 0, noTurnCommands: 0, loopRecoveries: 0 })
export const nextAutoplayMode = (mode: AutoplayMode): AutoplayMode => autoplayModes[(autoplayModes.indexOf(mode) + 1) % autoplayModes.length]
export const nextAutoplayPolicy = (policy: AutoplayPolicy): AutoplayPolicy => autoplayPolicies[(autoplayPolicies.indexOf(policy) + 1) % autoplayPolicies.length]
export const autoplayModeLabel = (mode: AutoplayMode): string => mode === 'visible' ? 'VISIBLE' : mode === 'omniscient' ? 'FULL MAP' : 'OFF'
export const autoplayPolicyLabel = (policy: AutoplayPolicy): string => policy === 'clear' ? 'CLEAR RATE' : policy === 'legacy' ? 'LEGACY' : 'SURVIVAL'

export const autoplayStateFingerprint = (state: RunState): string => {
  const hero = state.hero
  const actors = state.floor.actors.filter(actor => actor.health > 0).map(actor => `${actor.id}:${actor.x},${actor.y},${actor.health},${actor.energy}:${actor.conditions?.map(condition => `${condition.kind}${condition.duration}`).join(',') ?? '-'}`).sort().join('|')
  const inventory = hero.inventory.join(',')
  const equipment = Object.entries(hero.equipment).sort(([a], [b]) => a.localeCompare(b)).map(([slot, id]) => `${slot}:${id}`).join(',')
  const cooldowns = Object.entries(hero.cooldowns ?? {}).sort(([a], [b]) => a.localeCompare(b)).map(([id, turns]) => `${id}:${turns}`).join(',')
  const items = state.floor.items.map(item => `${item.id}:${item.x},${item.y},${item.count}`).sort().join('|')
  const props = state.floor.props.map(propFingerprint).sort().join('|')
  const tiles = state.floor.tiles.map(tile => `${tile.kind}:${tile.explored ? 1 : 0}`).join('|')
  const telegraphs = (state.floor.telegraphs ?? []).map(telegraph => `${telegraph.id}:${telegraph.resolveTurn}:${telegraph.cells.map(pointKey).join(',')}`).sort().join('|')
  return `${state.area ?? state.floor.biome}:${state.areaFloor ?? state.floor.index}:${hero.x},${hero.y}:${hero.health},${hero.focus}:${hero.gold},${hero.bombs},${hero.ropes},${hero.keys}:${hero.conditions?.map(condition => `${condition.kind}${condition.duration}`).join(',') ?? '-'}:${inventory}:${equipment}:${cooldowns}:${state.floor.objective.status}:${state.floor.guardianDefeated ? 1 : 0}:${state.modal?.kind ?? '-'}:${actors}:${items}:${props}:${telegraphs}:${tiles}`
}

// compact diagnostic identity; loop detection retains the full state signature above.
export const autoplayTraceFingerprint = (state: RunState): string => {
  const value = autoplayStateFingerprint(state)
  let hash = 0x811c9dc5
  for (let index = 0; index < value.length; index++) hash = Math.imul(hash ^ value.charCodeAt(index), 0x01000193)
  return (hash >>> 0).toString(36)
}

const strategicDistance = (state: RunState): number => {
  const objectiveComplete = state.floor.objective.status === 'complete' && state.floor.guardianDefeated
  if (objectiveComplete) return chebyshev(state.hero, state.floor.exit)
  const kind = state.floor.objective.kind
  const targets = kind === 'defeatGuardian'
    ? state.floor.actors.filter(actor => actor.role === 'guardian' && actor.health > 0).map(actor => ({ x: actor.x, y: actor.y }))
    : state.floor.tiles.flatMap((tile, index) => {
      const matches = kind === 'recoverSupplies' ? tile.kind === 'crate' || tile.kind === 'chest' : kind === 'rescueScout' ? tile.kind === 'rescue' : tile.kind === 'altar'
      return matches ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : []
    })
  const adjacent = kind === 'recoverSupplies' || kind === 'rescueScout'
  return targets.reduce((nearest, target) => Math.min(nearest, Math.max(0, chebyshev(state.hero, target) - Number(adjacent))), Number.POSITIVE_INFINITY)
}

const autoplayProgressFingerprint = (state: RunState, includePosition: boolean): string => {
  const hero = state.hero
  const hostiles = state.floor.actors.filter(actor => actor.hostile && actor.health > 0)
  const tileSummary = state.floor.tiles.reduce((summary, tile) => {
    if (tile.explored) summary.explored++
    if (tile.kind === 'crate' || tile.kind === 'chest') summary.containers++
    return summary
  }, { explored: 0, containers: 0 })
  const props = state.floor.props.map(propFingerprint).sort().join('|')
  const position = includePosition ? `${hero.x},${hero.y}:` : ''
  return `${state.area ?? state.floor.biome}:${state.areaFloor ?? state.floor.index}:${position}${state.floor.objective.kind}:${state.floor.objective.status}:${state.floor.guardianDefeated ? 1 : 0}:${hero.gold},${hero.bombs},${hero.ropes},${hero.keys}:${hero.inventory.join(',')}:${hostiles.length},${hostiles.reduce((sum, actor) => sum + actor.health, 0)}:${state.floor.items.length}:${tileSummary.explored},${tileSummary.containers}:${props}`
}

export const autoplayRecoveryFingerprint = (state: RunState): string => autoplayProgressFingerprint(state, true)

export const snapshotAutoplayTransition = (state: RunState): AutoplayTransitionSnapshot => ({
  stateKey: autoplayStateFingerprint(state),
  progressKey: autoplayProgressFingerprint(state, false),
  position: pointKey(state.hero),
  strategicDistance: strategicDistance(state),
  area: state.area,
  areaFloor: state.areaFloor,
  objectiveId: state.floor.objective.id,
  objectiveStatus: state.floor.objective.status,
  guardianDefeated: state.floor.guardianDefeated,
  turn: state.turn,
  modal: state.modal?.kind
})

export const recordAutoplayTransitionSnapshot = (context: AutoplayContext, before: AutoplayTransitionSnapshot, command: string, after: RunState): void => {
  const beforeKey = before.stateKey
  const afterKey = autoplayStateFingerprint(after)
  context.visits.set(afterKey, (context.visits.get(afterKey) ?? 0) + 1)
  if (after.turn <= before.turn) {
    if (beforeKey === afterKey) {
      context.noTurnCommands++
      context.failed.set(command, (context.failed.get(command) ?? 0) + 1)
    } else {
      context.noTurnCommands = 0
      context.failed.clear()
    }
    return
  }
  context.noTurnCommands = 0
  const lastTelegraphRoute = context.lastTelegraphRoute
  if (lastTelegraphRoute && (before.area !== after.area || before.areaFloor !== after.areaFloor || before.objectiveId !== after.floor.objective.id || !after.floor.actors.some(actor => actor.id === lastTelegraphRoute.sourceId && actor.hostile && actor.health > 0))) context.lastTelegraphRoute = undefined
  const beforeProgress = before.progressKey
  const afterProgress = autoplayProgressFingerprint(after, false)
  const objectiveChanged = before.area !== after.area || before.areaFloor !== after.areaFloor || before.objectiveId !== after.floor.objective.id || before.objectiveStatus !== after.floor.objective.status || before.guardianDefeated !== after.floor.guardianDefeated
  const distance = strategicDistance(after)
  if (objectiveChanged) context.bestStrategicDistance = undefined
  const routeAdvanced = Number.isFinite(distance) && (context.bestStrategicDistance === undefined || distance < context.bestStrategicDistance)
  context.bestStrategicDistance = Math.min(context.bestStrategicDistance ?? Number.POSITIVE_INFINITY, distance)
  const measuredProgress = beforeProgress !== afterProgress || routeAdvanced
  const objectiveRouteStep = isStrategicRouteReason(context.lastReason) && before.position !== pointKey(after.hero) && distance < before.strategicDistance
  const progressed = measuredProgress || objectiveRouteStep
  const movedToFreshPosition = before.position !== pointKey(after.hero) && !context.recentPositions.includes(pointKey(after.hero))
  if (measuredProgress) {
    context.strategicVisits.clear()
    context.recoveryVisits.clear()
    context.recentPositions = []
    context.loopRecoveries = 0
  }
  else if (objectiveRouteStep) context.strategicVisits.clear()
  else if (movedToFreshPosition) context.loopRecoveries = 0
  const strategicKey = autoplayProgressFingerprint(after, true)
  context.strategicVisits.set(strategicKey, (context.strategicVisits.get(strategicKey) ?? 0) + 1)
  context.noProgressTurns = !progressed ? context.noProgressTurns + 1 : 0
  if (beforeKey === afterKey) context.failed.set(command, (context.failed.get(command) ?? 0) + 1)
  else context.failed.clear()
  context.recentPositions.push(pointKey(after.hero))
  if (context.recentPositions.length > 24) context.recentPositions.shift()
  if (before.modal === 'shop' && /^\d+$/.test(command)) context.shopTurns++
  if (after.modal?.kind !== 'shop') context.shopTurns = 0
}

export const recordAutoplayTransition = (context: AutoplayContext, before: RunState, command: string, after: RunState): void => recordAutoplayTransitionSnapshot(context, snapshotAutoplayTransition(before), command, after)

const known = (state: RunState, mode: AutoplayMode, point: Point): boolean => mode === 'omniscient' || Boolean(getTile(state.floor, point.x, point.y)?.explored)
const visible = (state: RunState, point: Point): boolean => Boolean(getTile(state.floor, point.x, point.y)?.visible)
const telegraphDanger = (state: RunState, point: Point): boolean => (state.floor.telegraphs ?? []).some(telegraph => telegraph.resolveTurn <= state.turn + 1 && telegraph.cells.some(cell => cell.x === point.x && cell.y === point.y))

const commandForecastsTelegraph = (state: RunState, command: string): boolean => {
  if (telegraphDanger(state, state.hero)) return false
  const simulated = planningClone(state)
  const turn = simulated.turn
  perform(simulated, command)
  return simulated.status === 'dead' || (simulated.turn > turn && telegraphDanger(simulated, simulated.hero))
}
const passable = (state: RunState, mode: AutoplayMode, point: Point, avoidHazards: boolean, ignoreActors = false): boolean => {
  const tile = getTile(state.floor, point.x, point.y)
  if (!tile || !known(state, mode, point) || blockedTiles.has(tile.kind) || (tile.kind === 'lockedDoor' && state.hero.keys < 1) || isBlockingProp(propAt(state.floor.props, point.x, point.y))) return false
  if (avoidHazards && (hazardTiles.has(tile.kind) || telegraphDanger(state, point))) return false
  const occupant = actorAt(state.floor, point.x, point.y)
  return !occupant || (ignoreActors && occupant.hostile)
}
const isKnownItem = (state: RunState, mode: AutoplayMode, point: Point, visibleInFog = false): boolean => mode === 'omniscient' || visible(state, point) || visibleInFog
const adjacentCells = (point: Point): Point[] => directions.map(([, delta]) => ({ x: point.x + delta.x, y: point.y + delta.y }))
const hostileKnown = (state: RunState, mode: AutoplayMode) => state.floor.actors.filter(actor => actor.hostile && actor.health > 0 && (mode === 'omniscient' || visible(state, actor)))
const resourceReserve = (policy: AutoplayPolicy): number => policy === 'clear' ? 1 : policy === 'survival' ? 1 : 2

const heroAttackProfile = (state: RunState) => {
  const weapon = state.hero.equipment.mainHand ? ITEM[state.hero.equipment.mainHand] : undefined
  const profile = weapon?.weapon ?? { damage: 2, reach: 1, shape: 'adjacent' as const, cooldown: 0, tags: ['unarmed'] }
  const modified = evaluateEquipmentEffects(state.hero, 'action', { actionId: 'player-strike' }, { damage: profile.damage, range: profile.reach + agilityReachBonus(state.hero), cooldown: profile.cooldown }).values
  const synergy = resolveSynergies({ items: weapon ? [weapon.id] : [], skills: state.hero.skills }, { range: Math.max(1, Math.floor(modified.range ?? profile.reach)) })
  return { shape: profile.shape, reach: Math.max(1, Math.floor(synergy.values.range ?? profile.reach)), damage: Math.max(1, Math.floor(modified.damage ?? profile.damage)) }
}

const attackStances = (state: RunState, target: Point): Point[] => {
  const profile = heroAttackProfile(state)
  const stances: Point[] = []
  for (let y = target.y - profile.reach; y <= target.y + profile.reach; y++) for (let x = target.x - profile.reach; x <= target.x + profile.reach; x++) {
    const point = { x, y }
    if (pointKey(point) !== pointKey(target) && directions.some(([direction]) => actionCells(profile.shape, point, direction, profile.reach).some(cell => pointKey(cell) === pointKey(target)))) stances.push(point)
  }
  return stances
}

const hostilePressure = (state: RunState, mode: AutoplayMode, point: Point): number => hostileKnown(state, mode).reduce((pressure, actor) => {
  const range = chebyshev(actor, point)
  if (range <= 1) return pressure + 100 + actor.attack * 4
  if (range === 2) return pressure + 25 + actor.attack * 2
  if (actor.ai === 'ranged' && range <= 7 && canAffect(state.floor, actor, point)) return pressure + 100 + actor.attack * 3
  return pressure
}, 0)

const liveProjectileExposure = (state: RunState, mode: AutoplayMode): number => hostileKnown(state, mode)
  .filter(actor => actor.ai === 'ranged' && projectBolt(state.floor, actor, state.hero).collision?.by === 'target').length

const pendingProjectileExposure = (state: RunState): number => (state.floor.telegraphs ?? []).filter(telegraph => {
  if (telegraph.actionId !== 'enemy-shot' || telegraph.collision?.by !== 'target' || pointKey(telegraph.collision.point) !== pointKey(state.hero)) return false
  const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)
  return Boolean(source?.hostile && source.health > 0 && projectBolt(state.floor, source, telegraph.collision.point).collision?.by === 'target')
}).length

const projectileDefense = (state: RunState, mode: AutoplayMode): number => pendingProjectileExposure(state) * 3 + liveProjectileExposure(state, mode)

const playerRangedLineTargets = (state: RunState, mode: AutoplayMode): number => {
  const targets = new Set<string>()
  const knownHostile = (point: Point) => {
    const actor = actorAt(state.floor, point.x, point.y)
    return actor?.hostile && actor.health > 0 && (mode === 'omniscient' || visible(state, actor)) ? actor : undefined
  }
  if (state.hero.inventory.some(id => ITEM[id]?.throwable)) for (const [, delta] of directions) {
    for (const point of resolveLineEffect(state.floor, state.hero, { x: state.hero.x + delta.x * 5, y: state.hero.y + delta.y * 5 }).cells) {
      const actor = knownHostile(point)
      if (actor) targets.add(actor.id)
    }
  }
  for (const id of state.hero.inventory) {
    const spell = ITEM[id]?.spell
    if (!spell || !['ember', 'root', 'lull', 'gust', 'pull'].includes(spell) || state.hero.focus < scriptCastProfile(state.hero, id).focusCost) continue
    const range = spellTargetRange(state, id)
    for (const [, delta] of directions) {
      const point = { x: state.hero.x + delta.x * range, y: state.hero.y + delta.y * range }
      const actor = knownHostile(point)
      if (actor && canAffect(state.floor, state.hero, point)) targets.add(actor.id)
    }
  }
  return targets.size
}

const directionTargetsHostile = (state: RunState, direction: Exclude<Direction, 'wait'>): boolean => {
  const profile = heroAttackProfile(state)
  return actionCells(profile.shape, state.hero, direction, profile.reach).some(point => Boolean(actorAt(state.floor, point.x, point.y)?.hostile))
}

const projectedMove = (state: RunState, mode: AutoplayMode, from: Point, direction: Exclude<Direction, 'wait'>, avoidHazards: boolean, ignoreActors: boolean): Point | undefined => {
  if (pointKey(from) === pointKey(state.hero) && directionTargetsHostile(state, direction)) return undefined
  const delta = DIRECTIONS[direction]
  const first = { x: from.x + delta.x, y: from.y + delta.y }
  if (!passable(state, mode, first, avoidHazards, ignoreActors)) return undefined
  let destination = first
  let tile = getTile(state.floor, destination.x, destination.y)
  for (let step = 1; tile?.kind === 'floor' && step < agilityMoveDistance(state.hero); step++) {
    const next = { x: destination.x + delta.x, y: destination.y + delta.y }
    const nextTile = getTile(state.floor, next.x, next.y)
    if (!nextTile || nextTile.kind !== 'floor' || !passable(state, mode, next, avoidHazards, ignoreActors)) break
    destination = next
    tile = nextTile
  }
  return destination
}

const stepTo = (state: RunState, mode: AutoplayMode, targets: readonly Point[], allowTargetOccupied = false, avoidThreats = true, ignoreActors = false): { command: string; target: Point } | undefined => {
  if (!targets.length) return undefined
  const targetKeys = new Set(targets.map(pointKey))
  const firstMoveUnsafe = (direction: Direction): boolean => commandForecastsTelegraph(state, directionCommands[direction])
  const route = (avoidHazards: boolean, avoidHostiles: boolean): { command: string; target: Point } | undefined => {
    const initial = { x: state.hero.x, y: state.hero.y }
    const queue: Array<{ point: Point; first?: Direction }> = [{ point: initial }]
    let cursor = 0
    const seen = new Set([pointKey(initial)])
    let unsafeFallback: { command: string; target: Point } | undefined
    while (cursor < queue.length) {
      const current = queue[cursor++]
      if (current.first && targetKeys.has(pointKey(current.point))) {
        const candidate = { command: directionCommands[current.first], target: current.point }
        if (!firstMoveUnsafe(current.first)) return candidate
        unsafeFallback ??= candidate
        continue
      }
      for (const [direction] of directions) {
        const point = projectedMove(state, mode, current.point, direction, avoidHazards, ignoreActors)
        if (!point) continue
        const nextKey = pointKey(point)
        if (seen.has(nextKey)) continue
        const target = targetKeys.has(nextKey)
        if (!(target && allowTargetOccupied) && !passable(state, mode, point, avoidHazards, ignoreActors)) continue
        if (target && allowTargetOccupied && !known(state, mode, point)) continue
        if (avoidHostiles && !target && hostilePressure(state, mode, point) >= 100) continue
        seen.add(nextKey)
        queue.push({ point, first: current.first ?? direction })
      }
    }
    return unsafeFallback
  }
  return route(true, avoidThreats) ?? route(true, false) ?? route(false, false)
}

const telegraphSafeStepTo = (state: RunState, mode: AutoplayMode, targets: readonly Point[], avoidThreats: boolean, ignoreActors = false): { command: string; distance: number } | undefined => directions.flatMap(([direction]) => {
  const command = directionCommands[direction]
  const simulated = planningClone(state)
  const before = pointKey(simulated.hero)
  perform(simulated, command)
  if (simulated.status !== 'playing' || simulated.turn <= state.turn || pointKey(simulated.hero) === before || telegraphDanger(simulated, simulated.hero)) return []
  const continuation = stepTo(simulated, mode, targets, false, avoidThreats, ignoreActors)
  if (!continuation) return []
  const continued = planningClone(simulated)
  perform(continued, continuation.command)
  if (pointKey(continued.hero) === before) return []
  const distance = targets.reduce((nearest, target) => Math.min(nearest, chebyshev(simulated.hero, target)), Number.POSITIVE_INFINITY)
  return [{ command, distance, pressure: hostilePressure(simulated, mode, simulated.hero) }]
}).sort((a, b) => a.distance - b.distance || a.pressure - b.pressure || a.command.localeCompare(b.command))[0]

const routeDistanceField = (state: RunState, mode: AutoplayMode, targets: readonly Point[], avoidThreats: boolean): Map<string, number> => {
  const distances = new Map(targets.map(target => [pointKey(target), 0]))
  const queue = [...targets]
  let cursor = 0
  while (cursor < queue.length) {
    const current = queue[cursor++]!
    const distance = distances.get(pointKey(current))!
    for (const [direction, delta] of directions) {
      for (let step = 1; step <= agilityMoveDistance(state.hero); step++) {
        const source = { x: current.x - delta.x * step, y: current.y - delta.y * step }
        const sourceKey = pointKey(source)
        if (distances.has(sourceKey)) continue
        const destination = projectedMove(state, mode, source, direction, avoidThreats, true)
        if (!destination || pointKey(destination) !== pointKey(current)) continue
        distances.set(sourceKey, distance + 1)
        queue.push(source)
      }
    }
  }
  return distances
}

// simulate a short no-repeat movement sequence when normal routing has entered a cycle.
// this keeps actor movement in the plan instead of re-routing against a stale actor layout.
const predictiveRouteStep = (state: RunState, mode: AutoplayMode, targets: readonly Point[], avoidThreats: boolean, excludedPositions: ReadonlySet<string> = new Set()): { commands: string[] } | undefined => {
  const distances = routeDistanceField(state, mode, targets, avoidThreats)
  const initialDistance = distances.get(pointKey(state.hero))
  if (initialDistance === undefined) return undefined
  type Node = { state: RunState; commands: string[]; depth: number; path: Set<string>; healthLoss: number; targetDistance: number }
  const initialPath = new Set(excludedPositions)
  initialPath.delete(pointKey(state.hero))
  initialPath.add(pointKey(state.hero))
  const queue: Node[] = [{ state: planningClone(state), commands: [], depth: 0, path: initialPath, healthLoss: 0, targetDistance: initialDistance }]
  const seen = new Set([`${pointKey(state.hero)}:0`])
  let cursor = 0
  let best: { commands: string[]; distance: number; healthLoss: number; pressure: number; depth: number } | undefined
  while (cursor < queue.length && cursor < 512) {
    const current = queue[cursor++]!
    if (current.depth > 0 && current.commands.length) {
      const candidate = { commands: current.commands, distance: current.targetDistance, healthLoss: current.healthLoss, pressure: hostilePressure(current.state, mode, current.state.hero), depth: current.depth }
      if (!best || candidate.distance < best.distance || (candidate.distance === best.distance && (candidate.healthLoss < best.healthLoss || (candidate.healthLoss === best.healthLoss && (candidate.pressure < best.pressure || (candidate.pressure === best.pressure && candidate.depth < best.depth)))))) best = candidate
    }
    if (current.depth >= 12) continue
    for (const [direction] of directions) {
      const simulated = planningClone(current.state)
      const before = pointKey(simulated.hero)
      const health = simulated.hero.health
      perform(simulated, directionCommands[direction])
      const after = pointKey(simulated.hero)
      if (simulated.status !== 'playing' || simulated.turn <= current.state.turn || after === before || current.path.has(after) || telegraphDanger(simulated, simulated.hero)) continue
      const key = `${after}:${current.depth + 1}`
      if (seen.has(key)) continue
      seen.add(key)
      const targetDistance = distances.get(after)
      if (targetDistance === undefined) continue
      queue.push({ state: simulated, commands: [...current.commands, directionCommands[direction]], depth: current.depth + 1, path: new Set([...current.path, after]), healthLoss: current.healthLoss + Math.max(0, health - simulated.hero.health), targetDistance })
    }
  }
  return best && best.distance < initialDistance ? { commands: best.commands } : undefined
}

const objectiveTargets = (state: RunState, mode: AutoplayMode): Point[] => {
  const objective = state.floor.objective.kind
  if (objective === 'recoverSupplies') return state.floor.tiles.flatMap((tile, index) => (tile.kind === 'crate' || tile.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  if (objective === 'rescueScout') return state.floor.tiles.flatMap((tile, index) => tile.kind === 'rescue' && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  if (objective === 'invokeAltar') return state.floor.tiles.flatMap((tile, index) => tile.kind === 'altar' && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  return hostileKnown(state, mode).filter(actor => actor.role === 'guardian').map(actor => ({ x: actor.x, y: actor.y }))
}

const objectiveRouteTargets = (state: RunState, target: Point): Point[] => {
  const objective = state.floor.objective.kind
  if (objective === 'defeatGuardian') return attackStances(state, target)
  if (objective === 'recoverSupplies' || objective === 'rescueScout' || (objective === 'invokeAltar' && Boolean(actorAt(state.floor, target.x, target.y)))) return adjacentCells(target)
  return [target]
}

const hasStrategicRoute = (state: RunState, mode: AutoplayMode): boolean => {
  const routeExists = (candidate: RunState): boolean => {
    const objectiveComplete = candidate.floor.objective.status === 'complete' && candidate.floor.guardianDefeated
    if (objectiveComplete) return hasPassablePath(candidate.floor, candidate.hero, candidate.floor.exit)
    const targets = objectiveTargets(candidate, mode)
    return targets.some(target => {
      const routeTargets = objectiveRouteTargets(candidate, target)
      return routeTargets.some(point => hasPassablePath(candidate.floor, candidate.hero, point))
    })
  }
  if (routeExists(state)) return true
  if (state.hero.ropes < 1) return false
  const current = getTile(state.floor, state.hero.x, state.hero.y)
  const below = getTile(state.floor, state.hero.x, state.hero.y + 1)
  const anchor = current?.kind === 'pit' ? { x: state.hero.x, y: state.hero.y } : below?.kind === 'pit' ? { x: state.hero.x, y: state.hero.y + 1 } : undefined
  if (!anchor) return false
  const bridged = planningClone(state)
  const tile = getTile(bridged.floor, anchor.x, anchor.y)
  if (!tile) return false
  tile.kind = 'rope'
  bridged.hero.ropes--
  return routeExists(bridged)
}
export const autoplayHasStrategicRoute = hasStrategicRoute

const nearbyProps = (state: RunState): Prop[] => state.floor.props
  .filter(prop => prop.state !== 'destroyed' && chebyshev(state.hero, prop) <= 1)
  .sort((first, second) => first.id.localeCompare(second.id))

const urgentNear = (state: RunState, prop: Prop): boolean => hostilePressure(state, 'omniscient', state.hero) >= 100 || (state.floor.telegraphs ?? []).some(telegraph => telegraph.resolveTurn <= state.turn + 2 && telegraph.cells.some(cell => chebyshev(cell, prop) <= 3))
const shouldInspectProp = (state: RunState, prop: Prop): boolean => isBlockingProp(prop) || prop.tags.includes('cache') || prop.tags.includes('route') || (prop.tags.includes('warning') && urgentNear(state, prop)) || (prop.tags.includes('ritual') && urgentNear(state, prop)) || (prop.kind === 'wilds.rootShrine' && projectileDefense(state, 'omniscient') > 0) || (prop.kind === 'wilds.mushrooms' && state.hero.health <= state.hero.maxHealth - 8) || (prop.kind === 'caverns.glowingFungus' && state.hero.focus <= 2)
const canOpenPropWithOperate = (state: RunState, prop: Prop): boolean => (prop.kind === 'wilds.rootArch' && state.hero.equipment.mainHand === 'machete') || (prop.kind === 'ruins.collapsedArch' && state.hero.equipment.mainHand === 'pickaxe')
const canOpenPropWithRope = (state: RunState, prop: Prop): boolean => state.hero.ropes > 0 && (prop.kind === 'mine.brokenCart' || prop.kind === 'caverns.brokenBoat' || prop.kind === 'ruins.collapsedArch')

const propInteractionCandidate = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy, context: AutoplayContext): Candidate | undefined => {
  const props = nearbyProps(state)
  const planned = context.propPlanId ? props.find(prop => prop.id === context.propPlanId) : undefined
  const prop = planned ?? props.find(candidate => candidate.state === 'dormant' && shouldInspectProp(state, candidate))
  if (!prop) { if (context.propPlanId) context.propPlanId = undefined; return undefined }
  const simulated = planningClone(state)
  const beforeKey = autoplayStateFingerprint(state)
  const beforeItems = state.floor.items.length + state.hero.inventory.length
  const beforeHealth = state.hero.health
  const beforeShield = Boolean(state.hero.conditions?.some(condition => condition.kind === 'shielded'))
  const beforeBlock = isBlockingProp(prop)
  const beforeBombs = state.hero.bombs
  const beforeKeys = state.hero.keys
  const beforeFocus = state.hero.focus
  const tacticalProp = prop.kind === 'wilds.rootShrine' || prop.kind === 'ruins.brokenStatue'
  const beforeDefense = tacticalProp ? projectileDefense(state, mode) : 0
  const beforeRoute = tacticalProp ? hasStrategicRoute(state, mode) : false
  perform(simulated, 'c')
  const next = simulated.floor.props.find(candidate => candidate.id === prop.id)
  if (!next || autoplayStateFingerprint(simulated) === beforeKey) { if (planned) context.propPlanId = undefined; return undefined }
  const inspection = prop.state === 'dormant' && next.state === 'inspected'
  if (inspection) {
    if (!shouldInspectProp(state, prop)) return undefined
    const score = isBlockingProp(prop) ? 156 : prop.tags.includes('cache') ? 88 : urgentNear(state, prop) ? 108 : 58
    return { command: 'c', reason: `inspect prop:${prop.kind}`, score, propPlanId: prop.id }
  }
  const routeUnlocked = beforeBlock && !isBlockingProp(next)
  const routeGained = !hasStrategicRoute(state, mode) && hasStrategicRoute(simulated, mode)
  const itemGain = simulated.floor.items.length + simulated.hero.inventory.length - beforeItems
  const healthGain = simulated.hero.health - beforeHealth
  const shieldGain = !beforeShield && Boolean(simulated.hero.conditions?.some(condition => condition.kind === 'shielded'))
  const resourceSpend = beforeBombs - simulated.hero.bombs + beforeKeys - simulated.hero.keys + beforeFocus - simulated.hero.focus
  const charmSpent = state.hero.inventory.length - simulated.hero.inventory.length
  const createsHazard = getTile(state.floor, prop.x, prop.y)?.kind !== 'fireVent' && getTile(simulated.floor, prop.x, prop.y)?.kind === 'fireVent'
  const defensiveCover = tacticalProp && projectileDefense(simulated, mode) < beforeDefense && (!beforeRoute || hasStrategicRoute(simulated, mode))
  const useful = routeUnlocked || routeGained || itemGain > 0 || healthGain > 0 || shieldGain || defensiveCover
  if (!useful || (createsHazard && !routeUnlocked)) { if (planned) context.propPlanId = undefined; return undefined }
  if (charmSpent > 0 && !defensiveCover && healthGain < 1 && !shieldGain) { if (planned) context.propPlanId = undefined; return undefined }
  if (resourceSpend > 0 && !routeUnlocked && !routeGained && (policy === 'survival' || (beforeBombs > simulated.hero.bombs && simulated.hero.bombs < resourceReserve(policy)) || (beforeKeys > simulated.hero.keys && simulated.hero.keys < 1))) { if (planned) context.propPlanId = undefined; return undefined }
  const score = routeUnlocked || routeGained ? 180 : defensiveCover ? 210 : shieldGain && urgentNear(state, prop) ? 130 : healthGain > 0 ? 112 : 96
  return { command: 'c', reason: `operate prop:${prop.kind}`, score, propPlanId: prop.id }
}

const propRouteCandidate = (state: RunState, mode: AutoplayMode): Candidate | undefined => {
  if (hasStrategicRoute(state, mode)) return undefined
  const prop = state.floor.props.filter(candidate => candidate.state !== 'destroyed' && isBlockingProp(candidate) && (canOpenPropWithOperate(state, candidate) || canOpenPropWithRope(state, candidate)))
    .sort((first, second) => chebyshev(state.hero, first) - chebyshev(state.hero, second) || first.id.localeCompare(second.id))[0]
  if (!prop) return undefined
  const route = stepTo(state, mode, adjacentCells(prop), false, false)
  return route ? { command: route.command, reason: `prop route:${prop.kind}`, score: 158, propPlanId: prop.id } : undefined
}

const propRopeCandidate = (state: RunState, mode: AutoplayMode): Candidate | undefined => {
  if (state.hero.ropes < 1) return undefined
  const prop = nearbyProps(state).find(candidate => candidate.state === 'inspected' && canOpenPropWithRope(state, candidate))
  if (!prop) return undefined
  const simulated = planningClone(state)
  perform(simulated, 'r')
  const next = simulated.floor.props.find(candidate => candidate.id === prop.id)
  const routeGained = !hasStrategicRoute(state, mode) && hasStrategicRoute(simulated, mode)
  if (!next || !routeGained || (isBlockingProp(next) && prop.kind !== 'mine.brokenCart') || simulated.hero.ropes !== state.hero.ropes - 1) return undefined
  return { command: 'r', reason: `secure prop route:${prop.kind}`, score: 190, propPlanId: prop.id }
}

export const autoplayObjectiveRouteDiagnostics = (state: RunState, mode: Exclude<AutoplayMode, 'off'>, policy: AutoplayPolicy = 'clear'): Array<{ target: Point; direct?: string; blocked?: string }> => {
  const objective = state.floor.objective
  if (objective.status === 'complete') return []
  const availableTargets = objective.kind === 'invokeAltar' && state.hero.gold < 75 ? [] : objectiveTargets(state, mode)
  return availableTargets.map(target => {
    const targets = objectiveRouteTargets(state, target)
    return { target, direct: stepTo(state, mode, targets, false, policy !== 'clear')?.command, blocked: stepTo(state, mode, targets, false, false, true)?.command }
  })
}

const hostileInCells = (state: RunState, cells: readonly Point[]): number => cells.reduce((count, point) => count + Number(Boolean(actorAt(state.floor, point.x, point.y)?.hostile)), 0)
const targetEffects = (action: 'bomb' | 'throw' | 'spell', item?: string): readonly PropEffectKind[] => {
  if (action === 'bomb') return ['bomb']
  if (action === 'throw') return item === 'fireJar' ? ['bomb', 'fire'] : ['throw']
  const spell = ITEM[item ?? '']?.spell
  if (spell === 'ember') return ['fire']
  if (spell === 'root') return ['root']
  if (spell === 'water') return ['water']
  if (spell === 'gust' || spell === 'pull') return ['force']
  if (spell === 'ward') return ['ward']
  if (spell === 'gate') return ['gate']
  return []
}
const spellTargetRange = (state: RunState, item: string): number => {
  const profile = scriptCastProfile(state.hero, item)
  const geometry = resolveSynergies({ scripts: [item], skills: state.hero.skills }, { range: profile.range })
  return Math.max(1, Math.floor(geometry.values.range ?? profile.range))
}
const targetImpactCells = (state: RunState, action: 'bomb' | 'throw' | 'spell', direction: Exclude<Direction, 'wait'>, item?: string): Point[] => {
  if (action === 'bomb') return actionCells('burst', state.hero, direction, 2)
  const delta = DIRECTIONS[direction]
  if (action === 'throw') {
    const point = resolveLineEffect(state.floor, state.hero, { x: state.hero.x + delta.x * 5, y: state.hero.y + delta.y * 5 }).cells.at(-1)
    if (!point) return []
    return item === 'fireJar' ? actionCells('burst', { x: point.x - delta.x * 2, y: point.y - delta.y * 2 }, direction, 2) : [point]
  }
  if (!item) return []
  const range = spellTargetRange(state, item)
  return [{ x: state.hero.x + delta.x * range, y: state.hero.y + delta.y * range }]
}
const propTargetCount = (state: RunState, action: 'bomb' | 'throw' | 'spell', direction: Exclude<Direction, 'wait'>, item?: string): number => {
  const effects = targetEffects(action, item)
  if (!effects.length) return 0
  const targets = new Set(targetImpactCells(state, action, direction, item).map(pointKey))
  return state.floor.props.filter(prop => prop.state !== 'destroyed' && targets.has(pointKey(prop)) && effects.some(effect => prop.hooks?.includes(effect))).length
}
const targetDirection = (state: RunState, mode: AutoplayMode, action: 'bomb' | 'throw' | 'spell', item?: string): { direction: Exclude<Direction, 'wait'>; score: number } | undefined => {
  const scored = directions.map(([direction]) => {
    const propScore = propTargetCount(state, action, direction, item) * 12
    if (action === 'bomb') {
      const cells = actionCells('burst', state.hero, direction, 2).filter(point => known(state, mode, point))
      return { direction, score: hostileInCells(state, cells) * 70 + propScore }
    }
    if (action === 'throw') {
      const delta = DIRECTIONS[direction]
      const cells = resolveLineEffect(state.floor, state.hero, { x: state.hero.x + delta.x * 5, y: state.hero.y + delta.y * 5 }).cells
      return { direction, score: hostileInCells(state, cells) * (item === 'fireJar' ? 85 : item === 'spear' ? 54 : 34) + propScore }
    }
    const spell = ITEM[item ?? '']?.spell
    if (!spell || !item) return { direction, score: 0 }
    if (spell === 'mend') return { direction, score: state.hero.health <= state.hero.maxHealth - 6 ? 125 : 0 }
    if (spell === 'ward') return { direction, score: (hostilePressure(state, mode, state.hero) > 0 && state.hero.health <= state.hero.maxHealth - 4 ? 92 : 0) + propScore }
    if (spell === 'sight') return { direction, score: mode === 'visible' && state.floor.tiles.some(tile => !tile.explored) ? 44 : 0 }
    if (spell === 'gate') return { direction, score: (state.floor.objective.status === 'complete' && state.floor.guardianDefeated ? 175 : 0) + propScore }
    const range = spellTargetRange(state, item)
    const delta = DIRECTIONS[direction]
    const point = { x: state.hero.x + delta.x * range, y: state.hero.y + delta.y * range }
    if (spell === 'blink') return { direction, score: getTile(state.floor, point.x, point.y) && passable(state, mode, point, true) && hostilePressure(state, mode, point) + 35 < hostilePressure(state, mode, state.hero) ? 82 : 0 }
    const canHit = resolveLineEffect(state.floor, state.hero, point).cells.some(cell => cell.x === point.x && cell.y === point.y)
    const foe = canHit ? actorAt(state.floor, point.x, point.y) : undefined
    return { direction, score: (foe?.hostile ? (spell === 'ember' ? 94 : ['root', 'lull', 'gust', 'pull'].includes(spell) ? 58 : 0) : 0) + propScore }
  }).sort((a, b) => b.score - a.score || a.direction.localeCompare(b.direction))
  return scored[0]?.score ? scored[0] : undefined
}

const bestEquip = (state: RunState): string | undefined => state.hero.inventory.filter(id => ITEM[id]?.slot).filter(id => {
  const item = ITEM[id]
  const current = item.slot ? state.hero.equipment[item.slot] : undefined
  const currentItem = current ? ITEM[current] : undefined
  const value = (item.weapon?.damage ?? 0) * 20 + (item.defense ?? 0) * 12 + item.value / 10
  const currentValue = currentItem ? (currentItem.weapon?.damage ?? 0) * 20 + (currentItem.defense ?? 0) * 12 + currentItem.value / 10 : 0
  return value > currentValue
}).sort((a, b) => ITEM[b].value - ITEM[a].value)[0]

const tacticalEquip = (state: RunState, mode: AutoplayMode): string | undefined => {
  const current = heroAttackProfile(state)
  const blockedFoes = hostileKnown(state, mode).filter(foe => chebyshev(state.hero, foe) <= 1 && !directions.some(([direction]) => actionCells(current.shape, state.hero, direction, current.reach).some(point => point.x === foe.x && point.y === foe.y)))
  if (!blockedFoes.length) return undefined
  return state.hero.inventory.filter(id => ITEM[id]?.weapon && id !== state.hero.equipment.mainHand).map(id => {
    const simulated = planningClone(state)
    simulated.hero.equipment.mainHand = id
    const profile = heroAttackProfile(simulated)
    const targets = blockedFoes.filter(foe => directions.some(([direction]) => actionCells(profile.shape, state.hero, direction, profile.reach).some(point => point.x === foe.x && point.y === foe.y)))
    return { id, targets: targets.length, damage: profile.damage }
  }).filter(choice => choice.targets > 0).sort((a, b) => b.targets - a.targets || b.damage - a.damage || a.id.localeCompare(b.id))[0]?.id
}

const bestUse = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy): string | undefined => {
  const items = state.hero.inventory
  const underImmediateDanger = telegraphDanger(state, state.hero) || hostilePressure(state, mode, state.hero) >= 100
  const heal = items.find(id => ITEM[id]?.use === 'heal')
  if (heal && (state.hero.health + 8 <= state.hero.maxHealth || (hostilePressure(state, mode, state.hero) > 0 && state.hero.health < state.hero.maxHealth))) return heal
  const focus = items.find(id => ITEM[id]?.use === 'focus')
  if (focus && state.hero.focus <= 2 && items.some(id => ITEM[id]?.use === 'spell')) return focus
  const map = items.find(id => ITEM[id]?.use === 'map')
  if (map && mode === 'visible' && state.floor.tiles.some(tile => !tile.explored)) return map
  const bomb = items.find(id => ITEM[id]?.use === 'bomb')
  if (bomb && state.hero.bombs <= resourceReserve(policy) && !underImmediateDanger) return bomb
  const rope = items.find(id => ITEM[id]?.use === 'rope')
  if (rope && state.hero.ropes <= resourceReserve(policy) && !underImmediateDanger) return rope
  const key = items.find(id => ITEM[id]?.use === 'key')
  if (key && state.floor.tiles.some(tile => tile.kind === 'lockedDoor')) return key
  return undefined
}

const offeringDiscard = (state: RunState): string | undefined => state.hero.inventory
  .filter((id, index, items) => items.indexOf(id) !== index)
  .sort((a, b) => ITEM[a].value - ITEM[b].value || a.localeCompare(b))[0]

const bestShopItem = (state: RunState, policy: AutoplayPolicy): string | undefined => {
  const reserve = policy === 'clear' ? 0 : policy === 'survival' ? 20 : 45
  return merchantStock(state).filter(id => state.hero.gold - ITEM[id].value >= reserve && state.hero.inventory.length < 12).filter(id => {
    const item = ITEM[id]
    if (item.use === 'heal') return !state.hero.inventory.some(held => ITEM[held]?.use === 'heal')
    if (item.use === 'bomb') return state.hero.bombs <= resourceReserve(policy)
    if (item.use === 'rope') return state.hero.ropes <= resourceReserve(policy)
    if (!item.slot) return false
    return bestEquip({ ...state, hero: { ...state.hero, inventory: [...state.hero.inventory, id] } }) === id
  }).sort((a, b) => ITEM[b].value - ITEM[a].value)[0]
}

const gateChoice = (state: RunState, policy: AutoplayPolicy): number | undefined => {
  const gate = gateForArea(state.area ?? state.floor.biome)
  const choices = gate.tagAlternatives.map((option, index) => {
    const clone = planningClone(state)
    const resolution = resolveAreaGate(clone, gate, index)
    if (!resolution.resolved) return { index, score: Number.NEGATIVE_INFINITY }
    const cost = option.cost ?? gate.cost
    const irreversible = option.kind === 'npc' ? (policy === 'legacy' ? 1000 : policy === 'survival' ? 300 : 45) : option.kind === 'bomb' ? (policy === 'legacy' ? 180 : policy === 'survival' ? 80 : 20) : 0
    return { index, score: 1000 - cost.gold - cost.items.reduce((sum, id) => sum + ITEM[id].value, 0) - irreversible }
  }).filter(choice => Number.isFinite(choice.score)).sort((a, b) => b.score - a.score || a.index - b.index)
  return choices[0]?.index
}

const modalDecision = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy, context: AutoplayContext): Candidate | undefined => {
  const modal = state.modal
  if (!modal) return undefined
  if (modal.kind === 'skills') {
    const choices = skillChoices(state)
    const discipline = choices.map((choice, index) => ({ index, choice, score: policy === 'clear' ? choice.stat === 'strength' ? 30 : choice.stat === 'vitality' ? 25 : choice.stat === 'intellect' ? 18 : 8 : policy === 'survival' ? choice.stat === 'vitality' ? 30 : choice.stat === 'strength' ? 24 : choice.stat === 'intellect' ? 18 : 8 : choice.stat === 'intellect' ? 30 : choice.stat === 'strength' ? 24 : choice.stat === 'vitality' ? 18 : 8 }))
      .sort((a, b) => b.score - a.score || a.choice.id.localeCompare(b.choice.id))[0]
    return { command: discipline ? String(discipline.index + 1) : 'Escape', reason: discipline ? `discipline:${discipline.choice.id}` : 'close discipline', score: 200 }
  }
  if (modal.kind === 'inventory') {
    if (modal.mode === 'use') {
      const id = context.intent?.kind === 'use' ? context.intent.item : bestUse(state, mode, policy)
      context.intent = undefined
      const index = id ? state.hero.inventory.indexOf(id) : -1
      return { command: index >= 0 && safeInventoryChoice(state, String(index + 1)) ? String(index + 1) : 'Escape', reason: index >= 0 && id ? `use:${id}` : 'close inventory', score: 200 }
    }
    if (modal.mode === 'throw') {
      const id = context.intent?.kind === 'throw' ? context.intent.item : state.hero.inventory.find(id => ITEM[id]?.throwable)
      context.intent = undefined
      const index = id ? state.hero.inventory.indexOf(id) : -1
      return { command: index >= 0 ? String(index + 1) : 'Escape', reason: id ? `throw:${id}` : 'close throw', score: 200 }
    }
    if (modal.mode === 'drop') {
      const id = context.intent?.kind === 'drop' ? context.intent.item : undefined
      context.intent = undefined
      const index = id ? state.hero.inventory.indexOf(id) : -1
      return { command: index >= 0 && safeInventoryChoice(state, String(index + 1)) ? String(index + 1) : 'Escape', reason: index >= 0 && id ? `drop:${id}` : 'close inventory', score: 200 }
    }
    const id = context.intent?.kind === 'equip' ? context.intent.item : bestEquip(state)
    context.intent = undefined
    const index = id ? state.hero.inventory.indexOf(id) : -1
    return { command: index >= 0 && safeInventoryChoice(state, String(index + 1)) ? String(index + 1) : 'Escape', reason: index >= 0 && id ? `equip:${id}` : 'close equipment', score: 200 }
  }
  if (modal.kind === 'target') {
    if (modal.direction) return { command: 'Enter', reason: `confirm ${modal.action}`, score: 200 }
    const target = targetOutcome(state, mode, modal)
    return target ? { command: directionCommands[target.direction], reason: `${modal.action} target`, score: target.score } : { command: 'Escape', reason: `cancel ${modal.action}`, score: 200 }
  }
  if (modal.kind === 'shop') {
    const maxTurns = policy === 'clear' ? 2 : 1
    if (context.shopTurns >= maxTurns) { context.closedMerchants.add(modal.merchantId); return { command: 'Escape', reason: 'leave merchant', score: 200 } }
    const desired = bestShopItem(state, policy)
    const index = desired ? merchantStock(state).indexOf(desired) : -1
    if (index < 0) context.closedMerchants.add(modal.merchantId)
    return { command: index >= 0 ? String(index + 1) : 'Escape', reason: desired ? `buy:${desired}` : 'leave merchant', score: 200 }
  }
  if (modal.kind === 'gate') {
    if (modal.choice === undefined) {
      const choice = gateChoice(state, policy)
      return { command: choice === undefined ? 'Escape' : String(choice + 1), reason: choice === undefined ? 'no viable gate' : 'gate alternative', score: 200 }
    }
    return { command: 'Enter', reason: modal.confirming ? 'confirm gate' : 'review gate', score: 200 }
  }
  if (modal.kind === 'pause') return { command: 'Enter', reason: 'resume', score: 200 }
  return { command: 'Escape', reason: 'close modal', score: 200 }
}

const safeInventoryChoice = (state: RunState, command: string): boolean => {
  const simulated = planningClone(state)
  const turn = simulated.turn
  perform(simulated, command)
  if (simulated.status === 'dead') return false
  if (simulated.modal?.kind === 'target') return true
  return simulated.turn > turn
}

const evadeThreat = (state: RunState, mode: AutoplayMode, context: AutoplayContext, policy: AutoplayPolicy): Candidate | undefined => {
  const currentPressure = hostilePressure(state, mode, state.hero)
  const standingInTelegraph = telegraphDanger(state, state.hero)
  const canPressObjective = policy === 'clear' && state.hero.health * 2 >= state.hero.maxHealth
  if (!standingInTelegraph && (currentPressure < 100 || canPressObjective)) return undefined
  const options = directions.map(([direction, delta]) => ({ direction, point: { x: state.hero.x + delta.x, y: state.hero.y + delta.y } }))
    .filter(option => passable(state, mode, option.point, true) && !telegraphDanger(state, option.point))
    .map(option => ({ ...option, pressure: hostilePressure(state, mode, option.point), repeats: context.recentPositions.filter(key => key === pointKey(option.point)).length }))
  if (standingInTelegraph) {
    const urgent = directions.flatMap(([direction]) => {
      const point = projectedMove(state, mode, state.hero, direction, false, false)
      return point && !telegraphDanger(state, point) ? [{ direction, point, pressure: hostilePressure(state, mode, point), repeats: context.recentPositions.filter(key => key === pointKey(point)).length }] : []
    }).sort((a, b) => a.repeats - b.repeats || a.pressure - b.pressure || a.direction.localeCompare(b.direction))[0]
    if (urgent) return { command: directionCommands[urgent.direction], reason: 'evade telegraph', score: 152 }
  }
  const refuges = state.floor.tiles.flatMap((_, index) => {
    const point = { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }
    return known(state, mode, point) && passable(state, mode, point, true) && !telegraphDanger(state, point) && hostilePressure(state, mode, point) < 25 && chebyshev(state.hero, point) >= 2 ? [point] : []
  })
  const refuge = stepTo(state, mode, refuges)
  if (refuge) return { command: refuge.command, reason: standingInTelegraph ? 'withdraw telegraph' : 'withdraw threat', score: 152 }
  const fallback = options.filter(option => standingInTelegraph || option.pressure < currentPressure)
    .sort((a, b) => a.pressure - b.pressure || a.repeats - b.repeats || a.direction.localeCompare(b.direction))
  const option = fallback[0]
  return option ? { command: directionCommands[option.direction], reason: standingInTelegraph ? 'evade telegraph' : 'retreat threat', score: 152 } : undefined
}

const clearTelegraphSource = (state: RunState, mode: AutoplayMode, context: AutoplayContext): Candidate | undefined => {
  if (!breaksPositionCycle(context) && !context.recentPositions.includes(pointKey(state.hero))) return undefined
  const telegraph = (state.floor.telegraphs ?? []).find(current => current.actionId === 'enemy-shot' && current.resolveTurn <= state.turn + 1 && current.cells.some(cell => pointKey(cell) === pointKey(state.hero)))
  const source = telegraph ? state.floor.actors.find(actor => actor.id === telegraph.sourceId && actor.hostile && actor.health > 0) : undefined
  if (!source || state.hero.health <= source.attack + 4) return undefined
  const profile = heroAttackProfile(state)
  const attack = directions.find(([direction]) => actionCells(profile.shape, state.hero, direction, profile.reach).some(point => point.x === source.x && point.y === source.y))
  if (attack) return { command: directionCommands[attack[0]], reason: `clear telegraph source:${source.id}`, score: 650 }
  const route = stepTo(state, mode, adjacentCells(source), false, false)
  if (!route) return undefined
  const direction = directions.find(([current]) => directionCommands[current] === route.command)?.[0]
  const destination = direction ? projectedMove(state, mode, state.hero, direction, false, false) : undefined
  if (!destination || telegraphDanger(state, destination)) return undefined
  const from = pointKey(state.hero)
  const to = pointKey(destination)
  const previous = context.lastTelegraphRoute
  if (previous?.sourceId === source.id && (previous.to === to || previous.from === to && previous.to === from)) return undefined
  return { command: route.command, reason: `clear telegraph source:${source.id}`, score: 650, telegraphRoute: { sourceId: source.id, from, to } }
}

const breaksPositionCycle = (context: AutoplayContext): boolean => {
  const recent = context.recentPositions
  for (let period = 1; period <= Math.floor(recent.length / 3); period++) {
    const current = recent.slice(-period)
    const previous = recent.slice(-period * 2, -period)
    const earlier = recent.slice(-period * 3, -period * 2)
    if (current.every((position, index) => position === previous[index] && position === earlier[index])) return true
  }
  return false
}

const cycleBreakMove = (state: RunState, mode: AutoplayMode, context: AutoplayContext, forced = false): Candidate | undefined => {
  if (!forced && !breaksPositionCycle(context)) return undefined
  const recent = new Set(context.recentPositions.slice(-12))
  const options = directions.map(([direction]) => {
    const point = projectedMove(state, mode, state.hero, direction, true, false)
    if (!point || telegraphDanger(state, point)) return undefined
    return { direction, point, repeats: context.recentPositions.filter(position => position === pointKey(point)).length, pressure: hostilePressure(state, mode, point) }
  }).filter((option): option is { direction: Exclude<Direction, 'wait'>; point: Point; repeats: number; pressure: number } => Boolean(option))
  const fresh = options.filter(option => !recent.has(pointKey(option.point)))
  const selected = (fresh.length ? fresh : options.filter(option => option.repeats < 2))
    .sort((a, b) => a.repeats - b.repeats || a.pressure - b.pressure || a.direction.localeCompare(b.direction))[0]
  return selected ? { command: directionCommands[selected.direction], reason: 'break position cycle', score: 170 } : undefined
}

const resolveTargetSequence = (state: RunState, mode: AutoplayMode): void => {
  if (state.modal?.kind !== 'target') return
  const target = targetOutcome(state, mode, state.modal)
  if (!target) { perform(state, 'Escape'); return }
  perform(state, directionCommands[target.direction])
  if (state.modal?.kind === 'target' && state.modal.direction) perform(state, 'Enter')
}

const resolveCandidateSequence = (state: RunState, mode: AutoplayMode, candidate: Candidate): RunState => {
  const simulated = planningClone(state)
  perform(simulated, candidate.command)
  if (simulated.modal?.kind === 'inventory' && candidate.intent) {
    const index = simulated.hero.inventory.indexOf(candidate.intent.item)
    if (index >= 0) perform(simulated, String(index + 1))
    else perform(simulated, 'Escape')
  }
  resolveTargetSequence(simulated, mode)
  return simulated
}

const candidateLookahead = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy, candidate: Candidate, context: AutoplayContext): number => {
  if (candidate.command === 'q' && getTile(state.floor, state.hero.x, state.hero.y)?.kind === 'exit' && state.floor.objective.status === 'complete' && state.floor.guardianDefeated) return 260
  const health = state.hero.health
  const enemyHealth = state.floor.actors.filter(actor => actor.hostile && actor.health > 0).reduce((total, actor) => total + actor.health, 0)
  const enemies = state.floor.actors.filter(actor => actor.hostile && actor.health > 0).length
  const objective = state.floor.objective.status
  const gold = state.hero.gold
  const inventory = state.hero.inventory.length
  const turn = state.turn
  const hadStrategicRoute = hasStrategicRoute(state, mode)
  const simulated = candidate.intent || candidate.command === 'b'
    ? resolveCandidateSequence(state, mode, candidate)
    : planningClone(state)
  if (!candidate.intent && candidate.command !== 'b') perform(simulated, candidate.command)
  if (simulated.status === 'dead') return Number.NEGATIVE_INFINITY
  const healthAdjustment = (simulated.hero.health - health) * 18
  if (simulated.modal) {
    if (state.floor.biome !== 'ruins') return 0
    if (health * 3 <= state.hero.maxHealth && simulated.hero.health < health) return Number.NEGATIVE_INFINITY
    return healthAdjustment
  }
  const transitioned = simulated.floor.index !== state.floor.index || simulated.areaFloor !== state.areaFloor
  if (simulated.turn === turn && !transitioned && simulated.floor.objective.status === state.floor.objective.status && autoplayStateFingerprint(simulated) === autoplayStateFingerprint(state)) return Number.NEGATIVE_INFINITY
  if (transitioned) return 260 + healthAdjustment
  if (hadStrategicRoute && !hasStrategicRoute(simulated, mode)) return Number.NEGATIVE_INFINITY
  const remainingHealth = simulated.floor.actors.filter(actor => actor.hostile && actor.health > 0).reduce((total, actor) => total + actor.health, 0)
  const remainingEnemies = simulated.floor.actors.filter(actor => actor.hostile && actor.health > 0).length
  let adjustment = healthAdjustment + (enemyHealth - remainingHealth) * 9 + (enemies - remainingEnemies) * 48
  if (objective !== simulated.floor.objective.status) adjustment += 140
  adjustment += Math.max(0, simulated.hero.gold - gold) * 2
  if (simulated.hero.inventory.length > inventory && simulated.hero.inventory.length >= 12) adjustment -= 18
  if (telegraphDanger(simulated, simulated.hero)) adjustment -= 105
  if (pointKey(simulated.hero) !== pointKey(state.hero) && context.recentPositions.includes(pointKey(simulated.hero))) adjustment -= 75
  if (simulated.hero.health < health && hostilePressure(simulated, mode, simulated.hero) >= 100) adjustment -= 28
  if (hostilePressure(simulated, mode, simulated.hero) >= 100) adjustment -= 32
  if (policy === 'clear' && candidate.reason === 'cast:blink' && strategicDistance(simulated) >= strategicDistance(state)) adjustment -= 220
  return adjustment
}

const usableInventoryIntent = (state: RunState, mode: 'use' | 'equip', id: string): boolean => {
  const simulated = planningClone(state)
  simulated.modal = { kind: 'inventory', mode }
  const index = simulated.hero.inventory.indexOf(id)
  return index >= 0 && safeInventoryChoice(simulated, String(index + 1))
}

const combatMove = (state: RunState, mode: AutoplayMode): Candidate | undefined => {
  const objectiveComplete = state.floor.objective.status === 'complete' && state.floor.guardianDefeated
  const foes = hostileKnown(state, mode).filter(actor => chebyshev(state.hero, actor) <= 4).sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b))
  const profile = heroAttackProfile(state)
  const weaponId = state.hero.equipment.mainHand
  if (weaponId && (state.hero.cooldowns?.[weaponId] ?? 0) > 0) return undefined
  const estimatedDamage = profile.damage + state.hero.stats.strength + 3
  for (const foe of foes) for (const [direction] of directions) {
    if (!actionCells(profile.shape, state.hero, direction, profile.reach).some(point => point.x === foe.x && point.y === foe.y)) continue
    const lethal = foe.health <= estimatedDamage
    const safeExchange = state.hero.health > foe.attack * 2 + 4 && hostilePressure(state, mode, state.hero) < 180
    const rangedFinish = objectiveComplete && foe.ai === 'ranged' && state.hero.health > foe.attack + 4
    if (lethal || safeExchange || rangedFinish || foe.role === 'guardian') return { command: directionCommands[direction], reason: `melee:${foe.id}`, score: lethal ? 176 : safeExchange || rangedFinish ? 164 : foe.role === 'guardian' ? 82 : 64 }
  }
  const exitThreat = objectiveComplete && state.floor.biome !== 'ruins' ? foes.find(foe => foe.ai === 'ranged') : undefined
  const threatRoute = exitThreat ? stepTo(state, mode, adjacentCells(exitThreat), false, false) : undefined
  if (threatRoute) return { command: threatRoute.command, reason: `clear exit threat:${exitThreat!.id}`, score: 600 }
  const guardian = foes.find(foe => foe.role === 'guardian')
  const route = guardian ? stepTo(state, mode, attackStances(state, guardian), false, false) : undefined
  if (route) return { command: route.command, reason: `approach guardian:${guardian!.id}`, score: 58 }
  if (objectiveComplete && state.floor.biome !== 'ruins') {
    const blocker = hostileKnown(state, mode).sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b) || a.id.localeCompare(b.id))[0]
    const blockerRoute = blocker ? stepTo(state, mode, adjacentCells(blocker), false, false) : undefined
    if (blockerRoute) return { command: blockerRoute.command, reason: `clear exit blocker:${blocker!.id}`, score: 78 }
  }
  return undefined
}

const loopThreatMove = (state: RunState, mode: AutoplayMode): Candidate | undefined => {
  const maxRange = state.floor.biome === 'ruins' ? 4 : 10
  const threat = hostileKnown(state, mode).filter(actor => chebyshev(state.hero, actor) <= maxRange)
    .sort((a, b) => Number(b.ai === 'ranged') - Number(a.ai === 'ranged') || chebyshev(state.hero, a) - chebyshev(state.hero, b) || a.id.localeCompare(b.id))[0]
  const route = threat ? stepTo(state, mode, adjacentCells(threat), false, false) : undefined
  return route && threat ? { command: route.command, reason: `clear loop threat:${threat.id}`, score: 220 } : undefined
}

const guardianApproachMove = (state: RunState, mode: AutoplayMode, context: AutoplayContext): Candidate | undefined => {
  if (state.floor.objective.kind !== 'defeatGuardian' || state.floor.objective.status === 'complete') return undefined
  const guardian = hostileKnown(state, mode).find(actor => actor.role === 'guardian')
  if (!guardian) return undefined
  const options = [...directions.map(([direction]) => directionCommands[direction]), 'l']
    .flatMap(command => {
      const simulated = planningClone(state)
      const before = pointKey(simulated.hero)
      perform(simulated, command)
      if (simulated.status !== 'playing' || simulated.turn === state.turn) return []
      const target = simulated.floor.actors.find(actor => actor.id === guardian.id && actor.health > 0)
      if (!target) return []
      const profile = heroAttackProfile(simulated)
      const strikeReady = directions.some(([direction]) => actionCells(profile.shape, simulated.hero, direction, profile.reach).some(point => point.x === target.x && point.y === target.y))
      const stances = attackStances(simulated, target)
      const distance = stances.reduce((nearest, stance) => Math.min(nearest, chebyshev(simulated.hero, stance)), Number.POSITIVE_INFINITY)
      const moved = pointKey(simulated.hero) !== before
      if (!moved && !strikeReady) return []
      const repeats = context.recentPositions.filter(position => position === pointKey(simulated.hero)).length
      const danger = telegraphDanger(simulated, simulated.hero) ? 120 : hostilePressure(simulated, mode, simulated.hero) >= 100 ? 35 : 0
      return [{ command, strikeReady, distance, repeats, danger }]
    })
    .sort((a, b) => Number(b.strikeReady) - Number(a.strikeReady) || a.distance - b.distance || a.repeats - b.repeats || a.danger - b.danger || a.command.localeCompare(b.command))[0]
  if (!options) return undefined
  const score = options.strikeReady ? 210 : 176 - options.distance * 8 - options.repeats * 36 - options.danger
  return { command: options.command, reason: `track guardian:${guardian.id}`, score }
}

const guardianFinishMove = (state: RunState, mode: AutoplayMode, context: AutoplayContext): Candidate | undefined => {
  if (state.floor.objective.kind !== 'defeatGuardian' || state.floor.objective.status === 'complete') return undefined
  const guardian = hostileKnown(state, mode).find(actor => actor.role === 'guardian')
  const profile = heroAttackProfile(state)
  const estimatedDamage = profile.damage + state.hero.stats.strength + 3
  if (!guardian || guardian.health > estimatedDamage || state.hero.health <= guardian.attack * 2 + 8) return undefined
  const stances = attackStances(state, guardian)
  const distanceToStance = (point: Point) => stances.reduce((nearest, stance) => Math.min(nearest, chebyshev(point, stance)), Number.POSITIVE_INFINITY)
  const currentDistance = distanceToStance(state.hero)
  const option = directions.map(([direction]) => {
    const point = projectedMove(state, mode, state.hero, direction, false, false)
    return point ? { direction, point, distance: distanceToStance(point), repeats: context.recentPositions.filter(position => position === pointKey(point)).length } : undefined
  }).filter((candidate): candidate is { direction: Exclude<Direction, 'wait'>; point: Point; distance: number; repeats: number } => Boolean(candidate))
    .filter(candidate => candidate.distance < currentDistance)
    .sort((a, b) => a.distance - b.distance || a.repeats - b.repeats || a.direction.localeCompare(b.direction))[0]
  return option ? { command: directionCommands[option.direction], reason: `finish guardian:${guardian.id}`, score: 330 } : undefined
}

const targetOutcome = (state: RunState, mode: AutoplayMode, modal: Extract<NonNullable<RunState['modal']>, { kind: 'target' }>): TargetOutcome | undefined => {
  const beforeEnemyHealth = state.floor.actors.filter(actor => actor.hostile && actor.health > 0).reduce((sum, actor) => sum + actor.health, 0)
  const beforeEnemies = state.floor.actors.filter(actor => actor.hostile && actor.health > 0).length
  const beforeMobility = adjacentCells(state.hero).filter(point => passable(state, mode, point, false, true)).length
  const beforeProps = new Map(state.floor.props.map(prop => [prop.id, propFingerprint(prop)]))
  const beforeItems = state.floor.items.length + state.hero.inventory.length
  const beforeExplored = state.floor.tiles.filter(tile => tile.explored).length
  const beforeHazards = state.floor.tiles.filter(tile => hazardTiles.has(tile.kind)).length
  const beforeBlocked = state.floor.tiles.filter(tile => blockedTiles.has(tile.kind)).length
  const beforeRoute = hasStrategicRoute(state, mode)
  const effects = targetEffects(modal.action, modal.item)
  const canChangeLines = effects.includes('root') || effects.includes('force')
  const beforeDefense = canChangeLines ? projectileDefense(state, mode) : 0
  const beforeLiveExposure = canChangeLines ? liveProjectileExposure(state, mode) : 0
  const beforePlayerLines = canChangeLines ? playerRangedLineTargets(state, mode) : 0
  const pressure = hostilePressure(state, mode, state.hero)
  const imminentTelegraphs = state.floor.telegraphs?.filter(telegraph => telegraph.resolveTurn <= state.turn + 3) ?? []
  const scored = directions.flatMap(([direction]) => {
    const simulated = planningClone(state)
    const health = simulated.hero.health
    const shielded = Boolean(simulated.hero.conditions?.some(condition => condition.kind === 'shielded'))
    perform(simulated, directionCommands[direction])
    const events = perform(simulated, 'Enter')
    if (simulated.status === 'dead' || simulated.turn === state.turn) return []
    const enemyHealth = simulated.floor.actors.filter(actor => actor.hostile && actor.health > 0).reduce((sum, actor) => sum + actor.health, 0)
    const enemies = simulated.floor.actors.filter(actor => actor.hostile && actor.health > 0).length
    const damage = beforeEnemyHealth - enemyHealth
    const kills = beforeEnemies - enemies
    const harm = health - simulated.hero.health
    const mobility = adjacentCells(simulated.hero).filter(point => passable(simulated, mode, point, false, true)).length
    const mobilityGain = Math.max(0, mobility - beforeMobility)
    const terrainCleared = Math.max(0, beforeBlocked - simulated.floor.tiles.filter(tile => blockedTiles.has(tile.kind)).length)
    const base = targetDirection(state, mode, modal.action, modal.item)?.direction === direction ? 30 : 0
    const changedProps = simulated.floor.props.filter(prop => beforeProps.get(prop.id) !== propFingerprint(prop))
    const routeGained = !beforeRoute && hasStrategicRoute(simulated, mode)
    const itemGain = Math.max(0, simulated.floor.items.length + simulated.hero.inventory.length - beforeItems)
    const exploredGain = Math.max(0, simulated.floor.tiles.filter(tile => tile.explored).length - beforeExplored)
    const hazardsAdded = Math.max(0, simulated.floor.tiles.filter(tile => hazardTiles.has(tile.kind)).length - beforeHazards)
    const hazardsRemoved = Math.max(0, beforeHazards - simulated.floor.tiles.filter(tile => hazardTiles.has(tile.kind)).length)
    const shieldGain = !shielded && Boolean(simulated.hero.conditions?.some(condition => condition.kind === 'shielded'))
    const reachedExit = state.floor.objective.status === 'complete' && state.floor.guardianDefeated && pointKey(simulated.hero) === pointKey(simulated.floor.exit)
    const protectedTelegraph = changedProps.some(prop => (prop.effectCells ?? []).some(cell => imminentTelegraphs.some(telegraph => telegraph.cells.some(target => pointKey(target) === pointKey(cell)))))
    const changedDefensiveProp = changedProps.some(prop => prop.kind === 'wilds.rootShrine' || prop.kind === 'ruins.brokenStatue')
    const changedCrystal = changedProps.some(prop => prop.kind === 'caverns.crystalCluster' && prop.state === 'activated')
    const routePreserved = !(changedDefensiveProp || changedCrystal) || !beforeRoute || hasStrategicRoute(simulated, mode)
    const defensiveCover = changedDefensiveProp && projectileDefense(simulated, mode) < beforeDefense && routePreserved
    const refractedLine = changedCrystal && playerRangedLineTargets(simulated, mode) > beforePlayerLines && liveProjectileExposure(simulated, mode) <= beforeLiveExposure && routePreserved
    const tacticalShield = shieldGain && (pressure >= 100 || telegraphDanger(state, state.hero) || protectedTelegraph)
    const propValue = (routeGained ? 220 : 0) + (reachedExit ? 180 : 0) + itemGain * 72 + (mode === 'visible' ? exploredGain * 2 : 0) + hazardsRemoved * 64 + (tacticalShield ? 110 : 0) + (protectedTelegraph ? 130 : 0) + (defensiveCover ? 160 : 0) + (refractedLine ? 115 : 0)
    const usefulPropEffect = changedProps.length > 0 && propValue > 0
    const score = base + damage * 14 + kills * 90 + mobilityGain * 70 + propValue - harm * 42 - events.filter(event => event.type === 'danger').length * 10
    if (changedProps.length && !usefulPropEffect && damage < 1 && kills < 1) return []
    if ((defensiveCover || refractedLine) && !routePreserved) return []
    if (hazardsAdded > 0 && !routeGained && kills < 1) return []
    if (modal.action === 'bomb' && damage < 1 && mobilityGain < 1 && propValue < 1) return []
    if (state.floor.biome === 'ruins' && modal.action === 'bomb' && harm >= 4) return []
    if ((modal.action === 'throw' || modal.action === 'spell') && damage < 1 && base === 0 && propValue < 1) return []
    if (harm >= Math.max(8, Math.floor(health / 2)) && (state.floor.biome === 'ruins' || kills < 1)) return []
    return [{ direction, score, mobilityGain, terrainCleared }]
  }).sort((a, b) => b.score - a.score || a.direction.localeCompare(b.direction))
  return scored[0]
}

const usableTarget = (state: RunState, mode: AutoplayMode, action: 'bomb' | 'throw' | 'spell', item?: string): TargetOutcome | undefined => {
  const simulated = planningClone(state)
  if (action === 'bomb') simulated.modal = { kind: 'target', action }
  else if (item) simulated.modal = { kind: 'target', action, item }
  else return undefined
  return targetOutcome(simulated, mode, simulated.modal)
}

const explorationMove = (state: RunState, mode: AutoplayMode): Candidate | undefined => {
  const frontier = state.floor.tiles.flatMap((tile, index) => {
    if (!tile.explored || blockedTiles.has(tile.kind)) return []
    const point = { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }
    return adjacentCells(point).some(next => getTile(state.floor, next.x, next.y) && !getTile(state.floor, next.x, next.y)!.explored) ? [point] : []
  })
  const route = stepTo(state, mode, frontier)
  return route ? { command: route.command, reason: 'reach frontier', score: 24 } : undefined
}

const immediateCandidates = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy, context: AutoplayContext): Candidate[] => {
  const candidates: Candidate[] = []
  const heroPoint = { x: state.hero.x, y: state.hero.y }
  const objectiveComplete = state.floor.objective.status === 'complete' && state.floor.guardianDefeated
  const needsOffering = state.floor.objective.kind === 'invokeAltar' && state.hero.gold < 75
  const tile = getTile(state.floor, heroPoint.x, heroPoint.y)
  const rooted = Boolean(state.hero.conditions?.some(condition => condition.kind === 'rooted'))
  if (tile?.kind === 'exit' && objectiveComplete) return [{ command: 'q', reason: 'descend', score: 180 }]
  const canPick = (item: { id: string }): boolean => item.id === 'gold' || item.id === 'key' || state.hero.inventory.length < 12
  const groundItems = objectiveComplete ? [] : state.floor.items.filter(current => current.x === heroPoint.x && current.y === heroPoint.y && isKnownItem(state, mode, current, Boolean(current.visibleInFog)))
  const item = groundItems[0]
  const urgentOfferingPickup = needsOffering && item?.id === 'gold' && state.hero.health * 2 >= state.hero.maxHealth && !telegraphDanger(state, state.hero)
  const uncoverOfferingCash = needsOffering && Boolean(item) && groundItems.slice(1).some(current => current.id === 'gold') && state.hero.health * 2 >= state.hero.maxHealth && !telegraphDanger(state, state.hero)
  if (item && canPick(item) && (hostilePressure(state, mode, state.hero) < 100 || urgentOfferingPickup || uncoverOfferingCash)) candidates.push({ command: 'g', reason: `pickup:${item.id}`, score: urgentOfferingPickup || uncoverOfferingCash ? 200 : 160 })
  const discard = needsOffering && item && !canPick(item) && groundItems.some(current => current.id === 'gold') && state.hero.health * 2 >= state.hero.maxHealth && !telegraphDanger(state, state.hero) ? offeringDiscard(state) : undefined
  if (discard) candidates.push({ command: 'd', reason: `discard for offering:${discard}`, score: 210, intent: { kind: 'drop', item: discard } })
  const use = bestUse(state, mode, policy)
  if (use && usableInventoryIntent(state, 'use', use)) candidates.push({ command: 'u', reason: `use:${use}`, score: ITEM[use].use === 'heal' ? state.hero.health * 2 <= state.hero.maxHealth ? 220 : hostilePressure(state, mode, state.hero) > 0 ? 180 : 150 : 118, intent: { kind: 'use', item: use } })
  const equip = bestEquip(state)
  if (equip && usableInventoryIntent(state, 'equip', equip)) candidates.push({ command: 'e', reason: `equip:${equip}`, score: 88, intent: { kind: 'equip', item: equip } })
  const tacticalWeapon = tacticalEquip(state, mode)
  if (tacticalWeapon && usableInventoryIntent(state, 'equip', tacticalWeapon)) candidates.push({ command: 'e', reason: `tactical equip:${tacticalWeapon}`, score: 196, intent: { kind: 'equip', item: tacticalWeapon } })
  const pressure = hostilePressure(state, mode, state.hero)
  const standingInTelegraph = telegraphDanger(state, state.hero)
  const bombEmergency = state.hero.health * 2 <= state.hero.maxHealth && pressure >= 25
  const bomb = state.hero.bombs > 0 ? targetDirection(state, mode, 'bomb') : undefined
  const bombTarget = state.hero.bombs > 0 ? usableTarget(state, mode, 'bomb') : undefined
  const guardianBomb = Boolean(bomb && state.floor.objective.kind === 'defeatGuardian' && state.floor.objective.status !== 'complete' && actionCells('burst', state.hero, bomb.direction, 2).some(point => actorAt(state.floor, point.x, point.y)?.role === 'guardian'))
  const guardianRouteBomb = Boolean(bombTarget && state.floor.objective.kind === 'defeatGuardian' && state.floor.objective.status !== 'complete' && state.hero.bombs > resourceReserve(policy) && bombTarget.terrainCleared > 0 && bombTarget.mobilityGain > 0)
  const telegraphCounterBomb = Boolean(standingInTelegraph && bomb && bomb.score >= 70)
  const propBomb = Boolean(bombTarget && bombTarget.score >= 180 && propTargetCount(state, 'bomb', bombTarget.direction) > 0)
  const tacticalBomb = (bomb?.score ?? 0) >= 140 || ((bomb?.score ?? 0) >= 70 && (pressure >= 100 || bombEmergency || rooted))
  const bombAllowed = propBomb || telegraphCounterBomb || guardianBomb || guardianRouteBomb || ((state.hero.bombs > resourceReserve(policy) || bombEmergency) && tacticalBomb)
  if (bombTarget && bombTarget.score > 0 && bombAllowed) candidates.push({ command: 'b', reason: rooted ? 'break root: bomb' : propBomb ? 'bomb prop route' : telegraphCounterBomb ? 'bomb telegraph source' : guardianBomb ? 'bomb guardian' : guardianRouteBomb ? 'clear guardian route' : bombEmergency ? 'bomb emergency' : 'bomb tactical cluster', score: (rooted ? 360 : propBomb ? 240 : telegraphCounterBomb ? 310 : guardianBomb ? 290 : guardianRouteBomb ? 275 : bombEmergency ? 285 : pressure > 0 ? 185 : 110) + bombTarget.score / 10 })
  const throwable = state.hero.inventory.find(id => id === 'fireJar' || id === 'rock' || (id === 'spear' && state.hero.equipment.mainHand !== 'spear'))
  const throwTarget = throwable ? targetDirection(state, mode, 'throw', throwable) : undefined
  const safeThrowTarget = throwable ? usableTarget(state, mode, 'throw', throwable) : undefined
  const throwThreshold = throwable === 'fireJar' ? 85 : throwable === 'spear' ? 54 : 34
  if (throwable && throwTarget && safeThrowTarget && (throwTarget.score >= throwThreshold || safeThrowTarget.score >= 72)) candidates.push({ command: 't', reason: rooted ? `break root: throw:${throwable}` : safeThrowTarget.score >= 72 ? `throw prop:${throwable}` : `throw:${throwable}`, score: (rooted ? 330 : 96) + Math.max(throwTarget.score, safeThrowTarget.score) / 10 + (pressure >= 100 ? 60 : 0), intent: { kind: 'throw', item: throwable } })
  const spell = state.hero.inventory.filter(id => ITEM[id]?.use === 'spell').map(id => ({ id, target: targetDirection(state, mode, 'spell', id), resolved: usableTarget(state, mode, 'spell', id), profile: scriptCastProfile(state.hero, id) }))
    .filter((choice): choice is { id: string; target: { direction: Exclude<Direction, 'wait'>; score: number }; resolved: TargetOutcome; profile: ReturnType<typeof scriptCastProfile> } => Boolean(choice.target && choice.resolved) && state.hero.focus >= choice.profile.focusCost)
    .sort((a, b) => b.resolved.score - a.resolved.score || b.target.score - a.target.score || a.id.localeCompare(b.id))[0]
  if (spell) candidates.push({ command: 'u', reason: rooted ? `break root: cast:${spell.id}` : spell.resolved.score >= 72 ? `cast prop:${spell.id}` : `cast:${spell.id}`, score: (rooted ? 310 : 90) + Math.max(spell.target.score, spell.resolved.score) / 5 + (pressure >= 100 ? 60 : 0), intent: { kind: 'use', item: spell.id } })
  const nearbyContainer = adjacentCells(heroPoint).some(point => ['crate', 'chest'].includes(getTile(state.floor, point.x, point.y)?.kind ?? ''))
  const nearLockedDoor = adjacentCells(heroPoint).some(point => getTile(state.floor, point.x, point.y)?.kind === 'lockedDoor')
  const merchant = state.floor.actors.find(actor => actor.role === 'merchant' && chebyshev(actor, state.hero) <= 1)
  const nearMerchant = Boolean(merchant) && !context.closedMerchants.has(merchant!.id)
  const friendly = state.floor.actors.some(actor => actor.role === 'ally' && chebyshev(actor, state.hero) <= 1)
  const unlockedByKey = nearLockedDoor && state.hero.keys > 0
  const preserveOffering = state.floor.objective.kind === 'invokeAltar' && state.floor.objective.status !== 'complete' && state.hero.gold < 150
  const viableGate = nearLockedDoor && !unlockedByKey && !preserveOffering && gateChoice(state, policy) !== undefined
  const nearbyObjective = nearbyContainer || tile?.kind === 'rescue' || tile?.kind === 'altar' || friendly
  const standingObjective = tile?.kind === 'rescue' || (tile?.kind === 'altar' && state.hero.gold >= 75)
  if (standingObjective) candidates.push({ command: 'c', reason: 'operate objective', score: 300 })
  else if (nearbyObjective && (tile?.kind !== 'altar' || state.hero.gold >= 75)) candidates.push({ command: 'c', reason: 'operate objective', score: 135 })
  else if (unlockedByKey || viableGate) candidates.push({ command: 'c', reason: viableGate ? 'gate' : 'unlock door', score: 135 })
  if (nearMerchant && bestShopItem(state, policy)) candidates.push({ command: 'c', reason: 'merchant', score: 82 })
  if (state.hero.ropes > resourceReserve(policy) && (tile?.kind === 'pit' || getTile(state.floor, heroPoint.x, heroPoint.y + 1)?.kind === 'pit')) candidates.push({ command: 'r', reason: 'bridge pit', score: 122 })
  const propRope = propRopeCandidate(state, mode)
  if (propRope) candidates.push(propRope)
  const propInteraction = propInteractionCandidate(state, mode, policy, context)
  if (propInteraction) candidates.push(propInteraction)
  const propRoute = propRouteCandidate(state, mode)
  if (propRoute) candidates.push(propRoute)
  const cycleBreak = cycleBreakMove(state, mode, context)
  if (cycleBreak) candidates.push(cycleBreak)
  const evade = evadeThreat(state, mode, context, policy)
  if (evade) candidates.push(evade)
  const telegraphSource = clearTelegraphSource(state, mode, context)
  if (telegraphSource) candidates.push(telegraphSource)
  if (rooted) candidates.push({ command: 'l', reason: 'wait root', score: 175 })
  const combat = rooted ? undefined : combatMove(state, mode)
  if (combat) candidates.push(combat)
  const guardianFinish = guardianFinishMove(state, mode, context)
  if (guardianFinish) candidates.push(guardianFinish)
  const guardianApproach = guardianApproachMove(state, mode, context)
  if (guardianApproach) candidates.push(guardianApproach)
  const objective = state.floor.objective
  if (context.objectiveId !== objective.id) {
    context.objectiveId = objective.id
    context.objectiveTarget = undefined
    context.routePlan = undefined
    context.rejectedObjectiveTargets.clear()
  }
  let hasObjectiveRoute = false
  if (objective.status !== 'complete') {
    const availableTargets = needsOffering ? [] : objectiveTargets(state, mode)
    context.objectiveTargetCount = availableTargets.length
    if (context.objectiveTarget && !availableTargets.some(target => pointKey(target) === context.objectiveTarget)) context.objectiveTarget = undefined
    // Pin every objective target until it is resolved or proven unreachable; otherwise agility can alternate between nearby caches forever.
    const pinTarget = true
    let selectableTargets = availableTargets.filter(target => !context.rejectedObjectiveTargets.has(pointKey(target)))
    if (!selectableTargets.length) {
      context.rejectedObjectiveTargets.clear()
      selectableTargets = availableTargets
    }
    const orderedTargets = pinTarget && context.objectiveTarget
      ? [...selectableTargets.filter(target => pointKey(target) === context.objectiveTarget), ...selectableTargets.filter(target => pointKey(target) !== context.objectiveTarget)]
      : [...selectableTargets].sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b) || a.y - b.y || a.x - b.x)
    const routes: Array<{ target: Point; route: { command: string; target: Point }; blocked?: boolean }> = orderedTargets.flatMap(target => {
      const routeTargets = objectiveRouteTargets(state, target)
      if (routeTargets.some(point => point.x === state.hero.x && point.y === state.hero.y)) return []
      const direct = stepTo(state, mode, routeTargets, false, policy !== 'clear')
      if (direct) return [{ target, route: direct }]
      const blocked = stepTo(state, mode, routeTargets, false, false, true)
      return blocked ? [{ target, route: blocked, blocked: true }] : []
    })
    const route = routes.find(candidate => !candidate.blocked) ?? routes[0]
    if (route) {
      if (pinTarget) context.objectiveTarget = pointKey(route.target)
      hasObjectiveRoute = true
      const routeTargets = objectiveRouteTargets(state, route.target)
      const routeTelegraphed = commandForecastsTelegraph(state, route.route.command)
      const detour = routeTelegraphed ? telegraphSafeStepTo(state, mode, routeTargets, policy !== 'clear', Boolean(route.blocked)) : undefined
      const routeDistance = routeTargets.reduce((nearest, target) => Math.min(nearest, chebyshev(state.hero, target)), Number.POSITIVE_INFINITY)
      const stalledTelegraphRoute = routeTelegraphed && (!detour || detour.distance >= routeDistance)
      const cycle = breaksPositionCycle(context)
      const targetKey = pointKey(route.target)
      const predictive = policy === 'clear' && (stalledTelegraphRoute || cycle) ? predictiveRouteStep(state, mode, routeTargets, false, cycle ? new Set(context.recentPositions.slice(-12)) : undefined) : undefined
      candidates.push({ command: predictive?.commands[0] ?? detour?.command ?? route.route.command, reason: predictive ? `predictive objective route:${objective.kind}` : detour ? `avoid route telegraph:${objective.kind}` : route.blocked ? `clear objective route:${objective.kind}` : `objective:${objective.kind}`, routePlan: predictive && predictive.commands.length > 1 ? { kind: 'objective', targetKey, commands: predictive.commands.slice(1) } : undefined, score: policy === 'clear' ? predictive ? 205 : detour ? 166 : route.blocked ? 158 : 150 : detour ? 88 : route.blocked ? 76 : 70 })
      const weapon = state.hero.equipment.mainHand
      if (route.blocked && weapon && (state.hero.cooldowns?.[weapon] ?? 0) > 0 && !telegraphDanger(state, state.hero)) candidates.push({ command: 'l', reason: 'wait weapon cooldown', score: 185 })
    } else if (pinTarget && context.objectiveTarget) {
      context.rejectedObjectiveTargets.add(context.objectiveTarget)
      context.objectiveTarget = undefined
      context.routePlan = undefined
    }
  }
  if (objectiveComplete) {
    const exitRoute = stepTo(state, mode, [state.floor.exit], false, policy !== 'clear')
    const predictiveExit = policy === 'clear' && breaksPositionCycle(context) ? predictiveRouteStep(state, mode, [state.floor.exit], false, new Set(context.recentPositions.slice(-12))) : undefined
    if (exitRoute || predictiveExit) candidates.push({ command: predictiveExit?.commands[0] ?? exitRoute!.command, reason: predictiveExit ? 'predictive exit route' : 'reach exit', routePlan: predictiveExit && predictiveExit.commands.length > 1 ? { kind: 'exit', targetKey: pointKey(state.floor.exit), commands: predictiveExit.commands.slice(1) } : undefined, score: policy === 'clear' ? predictiveExit ? 260 : 240 : 140 })
    else if (!evade) candidates.push({ command: 'l', reason: 'await exit opening', score: 32 })
  } else {
    const collectForObjective = policy !== 'clear' || needsOffering
    const items = collectForObjective ? state.floor.items.filter(current => isKnownItem(state, mode, current, Boolean(current.visibleInFog)) && canPick(current) && (!needsOffering || current.id === 'gold')).map(current => ({ x: current.x, y: current.y })) : []
    const itemRoute = stepTo(state, mode, items)
    if (itemRoute) candidates.push({ command: itemRoute.command, reason: needsOffering ? 'reach offering cash' : 'reach loot', score: needsOffering ? 148 : 48 })
    const containers = state.floor.tiles.flatMap((current, index) => (current.kind === 'crate' || current.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
    const containerRoute = collectForObjective ? stepTo(state, mode, containers.flatMap(adjacentCells)) : undefined
    if (containerRoute) candidates.push({ command: containerRoute.command, reason: 'reach container', score: needsOffering ? 145 : 43 })
    const frontier = hasObjectiveRoute && policy === 'clear' ? undefined : explorationMove(state, mode)
    if (frontier) candidates.push(frontier)
  }
  return candidates
}

const scoredAutoplayCandidates = (state: RunState, mode: Exclude<AutoplayMode, 'off'>, policy: AutoplayPolicy, context: AutoplayContext): Candidate[] => {
  const candidates = immediateCandidates(state, mode, policy, context)
    .map(candidate => ({ ...candidate, score: candidate.score - (context.failed.get(candidate.command) ?? 0) * 60 }))
    .sort((a, b) => b.score - a.score || a.command.localeCompare(b.command) || a.reason.localeCompare(b.reason))
  return candidates
    .map(candidate => ({ ...candidate, score: candidate.score + candidateLookahead(state, mode, policy, candidate, context) }))
    .sort((a, b) => b.score - a.score || a.command.localeCompare(b.command) || a.reason.localeCompare(b.reason))
}

export const autoplayCandidateDiagnostics = (state: RunState, mode: Exclude<AutoplayMode, 'off'>, policy: AutoplayPolicy = 'survival', context: AutoplayContext = createAutoplayContext()): AutoplayCandidate[] => scoredAutoplayCandidates(state, mode, policy, context)
  .map(({ command, reason, score }) => ({ command, reason, score }))

export const autoplayDecision = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy = 'survival', context: AutoplayContext = createAutoplayContext()): AutoplayDecision | undefined => {
  if (mode === 'off' || state.status !== 'playing') return undefined
  context.startedTurn ??= state.turn
  if (state.turn - context.startedTurn >= AUTOPLAY_MAX_TURNS) {
    context.lastReason = `turn guard:${AUTOPLAY_MAX_TURNS}`
    return undefined
  }
  if (context.noTurnCommands >= AUTOPLAY_MAX_NON_TURN_COMMANDS) {
    context.lastReason = `non-turn guard:${AUTOPLAY_MAX_NON_TURN_COMMANDS}`
    return undefined
  }
  const modal = modalDecision(state, mode, policy, context)
  if (modal) return { command: modal.command, reason: modal.reason, candidates: [modal] }
  const routePlan = context.routePlan
  if (routePlan) {
    const validTarget = routePlan.kind === 'objective'
      ? state.floor.objective.status !== 'complete' && context.objectiveTarget === routePlan.targetKey
      : state.floor.objective.status === 'complete' && pointKey(state.floor.exit) === routePlan.targetKey
    if (!validTarget || !routePlan.commands.length) context.routePlan = undefined
    else {
      const command = routePlan.commands[0]!
      const simulated = planningClone(state)
      const before = pointKey(simulated.hero)
      perform(simulated, command)
      if (simulated.status === 'playing' && simulated.turn > state.turn && pointKey(simulated.hero) !== before && !telegraphDanger(simulated, simulated.hero)) {
        routePlan.commands.shift()
        if (!routePlan.commands.length) context.routePlan = undefined
        const candidate = { command, reason: routePlan.kind === 'exit' ? 'continue predictive exit route' : `continue predictive objective route:${state.floor.objective.kind}`, score: 205 }
        context.lastReason = candidate.reason
        return { command, reason: candidate.reason, candidates: [candidate] }
      }
      context.routePlan = undefined
    }
  }
  const fingerprint = autoplayStateFingerprint(state)
  const strategicVisits = context.strategicVisits.get(autoplayProgressFingerprint(state, true)) ?? 0
  if ((context.visits.get(fingerprint) ?? 0) >= 6 || strategicVisits >= 3 || context.noProgressTurns >= 32 || breaksPositionCycle(context)) {
    const recoveryKey = autoplayRecoveryFingerprint(state)
    const recoveryVisits = context.recoveryVisits.get(recoveryKey) ?? 0
    if (recoveryVisits >= AUTOPLAY_MAX_RECOVERY_REPEATS) {
      context.lastReason = 'recovery cycle guard'
      return undefined
    }
    context.loopRecoveries++
    context.recoveryVisits.set(recoveryKey, recoveryVisits + 1)
    if (recoveryVisits + 1 >= AUTOPLAY_MAX_RECOVERY_REPEATS) {
      context.lastReason = 'recovery cycle guard'
      return undefined
    }
    if (context.objectiveTarget && context.objectiveTargetCount > 1) {
      context.rejectedObjectiveTargets.add(context.objectiveTarget)
      context.objectiveTarget = undefined
      context.strategicVisits.clear()
      context.recentPositions = []
      context.noProgressTurns = 0
    } else {
      const combat = combatMove(state, mode)
      const cycleBreak = cycleBreakMove(state, mode, context, true)
      const telegraphSource = clearTelegraphSource(state, mode, context)
      const guardianAdvance = guardianApproachMove(state, mode, context)
      const guardianFinish = guardianFinishMove(state, mode, context)
      const strategicRoute = immediateCandidates(state, mode, policy, context)
        .filter(candidate => isStrategicRouteReason(candidate.reason))
        .sort((a, b) => b.score - a.score || a.command.localeCompare(b.command) || a.reason.localeCompare(b.reason))
        .find(candidate => Number.isFinite(candidateLookahead(state, mode, policy, candidate, context)))
      const recovery = [policy === 'clear' && state.hero.health * 2 >= state.hero.maxHealth ? strategicRoute : undefined, telegraphSource, evadeThreat(state, mode, context, policy), combat?.reason.startsWith('melee:') ? combat : undefined, guardianFinish, guardianAdvance, strategicRoute, cycleBreak, combat, loopThreatMove(state, mode)]
        .filter((candidate): candidate is Candidate => Boolean(candidate))
        .find(candidate => Number.isFinite(candidateLookahead(state, mode, policy, candidate, context)))
      if (!recovery) {
        context.lastReason = 'no recovery action'
        return undefined
      }
      if (recovery.routePlan) context.routePlan = recovery.routePlan
      if (recovery.telegraphRoute) context.lastTelegraphRoute = recovery.telegraphRoute
      context.lastReason = recovery.reason
      return { command: recovery.command, reason: recovery.reason, candidates: [recovery] }
    }
  }
  const candidates = scoredAutoplayCandidates(state, mode, policy, context).filter(candidate => Number.isFinite(candidate.score))
  const selected = candidates.sort((a, b) => b.score - a.score || a.command.localeCompare(b.command) || a.reason.localeCompare(b.reason))[0]
  if (!selected || selected.score < -500) {
    context.lastReason = 'no viable candidate'
    return undefined
  }
  context.intent = selected.intent
  if (selected.propPlanId) context.propPlanId = selected.propPlanId
  if (selected.routePlan) context.routePlan = selected.routePlan
  if (selected.telegraphRoute) context.lastTelegraphRoute = selected.telegraphRoute
  context.lastReason = selected.reason
  return { command: selected.command, reason: selected.reason, candidates: candidates.slice(0, 8).map(({ command, reason, score }) => ({ command, reason, score })) }
}

export const autoplayCommand = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy = 'survival', context?: AutoplayContext): string | undefined => autoplayDecision(state, mode, policy, context)?.command
