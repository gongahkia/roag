import { ITEM } from './content'
import { actionCells, skillChoices } from './engine'
import { DIRECTIONS, MAP_WIDTH, type AutoplayMode, type Direction, type Point, type RunState, type TileKind } from './types'
import { actorAt, getTile } from './world'

export const AUTOPLAY_TURN_MS = Math.round(1000 / 6)
export const autoplayModes: readonly AutoplayMode[] = ['off', 'visible', 'omniscient']
const directionCommands: Record<Direction, string> = { nw: 'i', n: 'o', ne: 'p', w: 'k', wait: 'l', e: ';', sw: ',', s: '.', se: '/' }
const blockedTiles = new Set<TileKind>(['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest'])
const hazardTiles = new Set<TileKind>(['spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder'])
const directions = (Object.entries(DIRECTIONS) as Array<[Direction, Point]>).filter(([direction]) => direction !== 'wait') as Array<[Exclude<Direction, 'wait'>, Point]>
const key = (point: Point): string => `${point.x},${point.y}`
const chebyshev = (a: Point, b: Point): number => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))

export const nextAutoplayMode = (mode: AutoplayMode): AutoplayMode => autoplayModes[(autoplayModes.indexOf(mode) + 1) % autoplayModes.length]
export const autoplayModeLabel = (mode: AutoplayMode): string => mode === 'visible' ? 'VISIBLE' : mode === 'omniscient' ? 'FULL MAP' : 'OFF'

const known = (state: RunState, mode: AutoplayMode, point: Point): boolean => mode === 'omniscient' || Boolean(getTile(state.floor, point.x, point.y)?.explored)
const visible = (state: RunState, point: Point): boolean => Boolean(getTile(state.floor, point.x, point.y)?.visible)
const passable = (state: RunState, mode: AutoplayMode, point: Point, avoidHazards: boolean): boolean => {
  const tile = getTile(state.floor, point.x, point.y)
  if (!tile || !known(state, mode, point) || blockedTiles.has(tile.kind) || (tile.kind === 'lockedDoor' && state.hero.keys < 1)) return false
  if (avoidHazards && hazardTiles.has(tile.kind)) return false
  return !actorAt(state.floor, point.x, point.y)
}
const directionToward = (from: Point, to: Point): Exclude<Direction, 'wait'> => {
  const dx = Math.sign(to.x - from.x)
  const dy = Math.sign(to.y - from.y)
  return (dx === -1 && dy === -1 ? 'nw' : dx === 0 && dy === -1 ? 'n' : dx === 1 && dy === -1 ? 'ne' : dx === -1 && dy === 0 ? 'w' : dx === 1 && dy === 0 ? 'e' : dx === -1 && dy === 1 ? 'sw' : dx === 0 && dy === 1 ? 's' : 'se')
}
const isKnownItem = (state: RunState, mode: AutoplayMode, point: Point, visibleInFog = false): boolean => mode === 'omniscient' || visible(state, point) || visibleInFog

const stepTo = (state: RunState, mode: AutoplayMode, targets: readonly Point[], allowTargetOccupied = false): string | undefined => {
  if (!targets.length) return undefined
  const targetKeys = new Set(targets.map(key))
  const route = (avoidHazards: boolean): Direction | undefined => {
    const initial = { x: state.hero.x, y: state.hero.y }
    const queue: Array<{ point: Point; first?: Direction }> = [{ point: initial }]
    const seen = new Set([key(initial)])
    while (queue.length) {
      const current = queue.shift()!
      if (current.first && targetKeys.has(key(current.point))) return current.first
      for (const [direction, delta] of directions) {
        const point = { x: current.point.x + delta.x, y: current.point.y + delta.y }
        const pointKey = key(point)
        if (seen.has(pointKey)) continue
        const target = targetKeys.has(pointKey)
        if (!(target && allowTargetOccupied) && !passable(state, mode, point, avoidHazards)) continue
        if (target && allowTargetOccupied && !known(state, mode, point)) continue
        seen.add(pointKey)
        queue.push({ point, first: current.first ?? direction })
      }
    }
    return undefined
  }
  const direction = route(true) ?? route(false)
  return direction ? directionCommands[direction] : undefined
}

const adjacentCells = (point: Point): Point[] => directions.map(([, delta]) => ({ x: point.x + delta.x, y: point.y + delta.y }))
const hostileKnown = (state: RunState, mode: AutoplayMode) => state.floor.actors.filter(actor => actor.hostile && actor.health > 0 && (mode === 'omniscient' || visible(state, actor)))
const objectTargets = (state: RunState, mode: AutoplayMode): Point[] => {
  const objective = state.floor.objective.kind
  if (objective === 'recoverSupplies') return state.floor.tiles.flatMap((tile, index) => (tile.kind === 'crate' || tile.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  if (objective === 'rescueScout') return state.floor.tiles.flatMap((tile, index) => tile.kind === 'rescue' && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  if (objective === 'invokeAltar') return state.floor.tiles.flatMap((tile, index) => tile.kind === 'altar' && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  return hostileKnown(state, mode).filter(actor => actor.role === 'guardian').map(actor => ({ x: actor.x, y: actor.y }))
}

const combatCommand = (state: RunState, mode: AutoplayMode): string | undefined => {
  const foes = hostileKnown(state, mode).filter(actor => chebyshev(state.hero, actor) <= 3).sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b))
  const weapon = state.hero.equipment.mainHand ? ITEM[state.hero.equipment.mainHand]?.weapon : undefined
  const shape = weapon?.shape ?? 'adjacent'
  const reach = weapon?.reach ?? 1
  for (const foe of foes) for (const [direction] of directions) if (actionCells(shape, state.hero, direction, reach).some(point => point.x === foe.x && point.y === foe.y)) return directionCommands[direction]
  const nearest = foes[0]
  return nearest ? stepTo(state, mode, adjacentCells(nearest)) : undefined
}

const modalCommand = (state: RunState, mode: AutoplayMode): string | undefined => {
  const modal = state.modal
  if (!modal) return undefined
  if (modal.kind === 'skills') return skillChoices(state).length ? '1' : 'Escape'
  if (modal.kind === 'inventory') {
    if (modal.mode !== 'use') return 'Escape'
    const index = state.hero.inventory.findIndex(id => ITEM[id]?.use === 'heal')
    return index >= 0 ? String(index + 1) : 'Escape'
  }
  if (modal.kind === 'target') {
    if (modal.direction) return 'Enter'
    const foe = hostileKnown(state, mode).sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b))[0]
    return foe ? directionCommands[directionToward(state.hero, foe)] : 'Escape'
  }
  if (modal.kind === 'pause') return 'Enter'
  return 'Escape'
}

export const autoplayCommand = (state: RunState, mode: AutoplayMode): string | undefined => {
  if (mode === 'off' || state.status !== 'playing') return undefined
  const modal = modalCommand(state, mode)
  if (modal) return modal
  const atHero = { x: state.hero.x, y: state.hero.y }
  const item = state.floor.items.find(current => current.x === atHero.x && current.y === atHero.y && isKnownItem(state, mode, current, Boolean(current.visibleInFog)) && (current.id === 'gold' || current.id === 'key' || state.hero.inventory.length < 12))
  if (item) return 'g'
  const tile = getTile(state.floor, atHero.x, atHero.y)
  const nearbyContainer = adjacentCells(atHero).some(point => ['crate', 'chest'].includes(getTile(state.floor, point.x, point.y)?.kind ?? ''))
  const nearLockedDoor = adjacentCells(atHero).some(point => getTile(state.floor, point.x, point.y)?.kind === 'lockedDoor')
  const nearMerchant = state.floor.actors.some(actor => actor.role === 'merchant' && chebyshev(actor, state.hero) <= 1)
  if (!nearLockedDoor && !nearMerchant && (nearbyContainer || tile?.kind === 'rescue' || tile?.kind === 'altar' || state.floor.actors.some(actor => actor.role === 'ally' && chebyshev(actor, state.hero) <= 1))) {
    if (tile?.kind !== 'altar' || state.hero.gold >= 75) return 'c'
  }
  if (tile?.kind === 'exit' && state.floor.objective.status === 'complete' && state.floor.guardianDefeated) return 'q'
  const tonic = state.hero.inventory.find(id => ITEM[id]?.use === 'heal')
  if (tonic && state.hero.health * 3 <= state.hero.maxHealth) return 'u'
  const combat = combatCommand(state, mode)
  if (combat) return combat
  const objective = state.floor.objective
  if (objective.status !== 'complete') {
    const targets = objectTargets(state, mode)
    if (objective.kind === 'recoverSupplies') {
      const route = stepTo(state, mode, targets.flatMap(adjacentCells))
      if (route) return route
    } else if (objective.kind === 'defeatGuardian') {
      const route = stepTo(state, mode, targets.flatMap(adjacentCells))
      if (route) return route
    } else if (objective.kind === 'invokeAltar' && state.hero.gold < 75) {
      const containers = state.floor.tiles.flatMap((current, index) => (current.kind === 'crate' || current.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
      const route = stepTo(state, mode, containers.flatMap(adjacentCells))
      if (route) return route
    }
    const route = stepTo(state, mode, objective.kind === 'rescueScout' || objective.kind === 'invokeAltar' ? targets.flatMap(adjacentCells) : targets)
    if (route) return route
  }
  const items = state.floor.items.filter(current => isKnownItem(state, mode, current, Boolean(current.visibleInFog))).map(current => ({ x: current.x, y: current.y }))
  const itemRoute = stepTo(state, mode, items)
  if (itemRoute) return itemRoute
  const containers = state.floor.tiles.flatMap((current, index) => (current.kind === 'crate' || current.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  const containerRoute = stepTo(state, mode, containers.flatMap(adjacentCells))
  if (containerRoute) return containerRoute
  if (objective.status === 'complete' && state.floor.guardianDefeated) {
    const exitRoute = stepTo(state, mode, [state.floor.exit])
    if (exitRoute) return exitRoute
  }
  const frontier = state.floor.tiles.flatMap((tile, index) => {
    if (!tile.explored || blockedTiles.has(tile.kind)) return []
    const point = { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }
    return adjacentCells(point).some(next => getTile(state.floor, next.x, next.y) && !getTile(state.floor, next.x, next.y)!.explored) ? [point] : []
  })
  return stepTo(state, mode, frontier)
}
