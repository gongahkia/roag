import { ITEM } from './content'
import { actionCells, perform, skillChoices } from './engine'
import { agilityMoveDistance } from './engine/agility'
import { resolveAreaGate, gateForArea } from './engine/gates'
import { canAffect, resolveLineEffect } from './engine/line-effect'
import { merchantStock } from './engine/rewards'
import { scriptCastProfile } from './engine/scripts'
import { DIRECTIONS, MAP_WIDTH, type AutoplayCandidate, type AutoplayMode, type AutoplayPolicy, type Direction, type Point, type RunState, type TileKind } from './types'
import { actorAt, getTile } from './world'

export const AUTOPLAY_TURN_MS = Math.round(1000 / 6)
export const autoplayModes: readonly AutoplayMode[] = ['off', 'visible', 'omniscient']
export const autoplayPolicies: readonly AutoplayPolicy[] = ['survival', 'clear', 'legacy']
const directionCommands: Record<Direction, string> = { nw: 'i', n: 'o', ne: 'p', w: 'k', wait: 'l', e: ';', sw: ',', s: '.', se: '/' }
const blockedTiles = new Set<TileKind>(['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest'])
const hazardTiles = new Set<TileKind>(['spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder'])
const directions = (Object.entries(DIRECTIONS) as Array<[Direction, Point]>).filter(([direction]) => direction !== 'wait') as Array<[Exclude<Direction, 'wait'>, Point]>
const pointKey = (point: Point): string => `${point.x},${point.y}`
const chebyshev = (a: Point, b: Point): number => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))

type Intent = { kind: 'use' | 'throw' | 'equip'; item: string }
type Candidate = AutoplayCandidate & { intent?: Intent }
export interface AutoplayDecision { command: string; reason: string; candidates: AutoplayCandidate[] }
export interface AutoplayContext { visits: Map<string, number>; failed: Map<string, number>; closedMerchants: Set<string>; recentPositions: string[]; intent?: Intent; shopTurns: number; lastReason?: string }

export const createAutoplayContext = (): AutoplayContext => ({ visits: new Map(), failed: new Map(), closedMerchants: new Set(), recentPositions: [], shopTurns: 0 })
export const nextAutoplayMode = (mode: AutoplayMode): AutoplayMode => autoplayModes[(autoplayModes.indexOf(mode) + 1) % autoplayModes.length]
export const nextAutoplayPolicy = (policy: AutoplayPolicy): AutoplayPolicy => autoplayPolicies[(autoplayPolicies.indexOf(policy) + 1) % autoplayPolicies.length]
export const autoplayModeLabel = (mode: AutoplayMode): string => mode === 'visible' ? 'VISIBLE' : mode === 'omniscient' ? 'FULL MAP' : 'OFF'
export const autoplayPolicyLabel = (policy: AutoplayPolicy): string => policy === 'clear' ? 'CLEAR RATE' : policy === 'legacy' ? 'LEGACY' : 'SURVIVAL'

export const autoplayStateFingerprint = (state: RunState): string => {
  const hero = state.hero
  const actors = state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => `${actor.id}:${actor.x},${actor.y},${actor.health}`).sort().join('|')
  return `${state.area ?? state.floor.biome}:${state.areaFloor ?? state.floor.index}:${hero.x},${hero.y}:${hero.health},${hero.focus}:${hero.gold},${hero.bombs},${hero.ropes},${hero.keys}:${state.floor.objective.status}:${state.floor.guardianDefeated ? 1 : 0}:${state.modal?.kind ?? '-'}:${actors}`
}

export const recordAutoplayTransition = (context: AutoplayContext, before: RunState, command: string, after: RunState): void => {
  const beforeKey = autoplayStateFingerprint(before)
  const afterKey = autoplayStateFingerprint(after)
  context.visits.set(afterKey, (context.visits.get(afterKey) ?? 0) + 1)
  if (beforeKey === afterKey) context.failed.set(command, (context.failed.get(command) ?? 0) + 1)
  else context.failed.clear()
  context.recentPositions.push(pointKey(after.hero))
  if (context.recentPositions.length > 12) context.recentPositions.shift()
  if (before.modal?.kind === 'shop' && /^\d+$/.test(command) && after.turn > before.turn) context.shopTurns++
  if (after.modal?.kind !== 'shop') context.shopTurns = 0
}

const known = (state: RunState, mode: AutoplayMode, point: Point): boolean => mode === 'omniscient' || Boolean(getTile(state.floor, point.x, point.y)?.explored)
const visible = (state: RunState, point: Point): boolean => Boolean(getTile(state.floor, point.x, point.y)?.visible)
const telegraphDanger = (state: RunState, point: Point): boolean => (state.floor.telegraphs ?? []).some(telegraph => telegraph.resolveTurn <= state.turn + 1 && telegraph.cells.some(cell => cell.x === point.x && cell.y === point.y))
const passable = (state: RunState, mode: AutoplayMode, point: Point, avoidHazards: boolean, ignoreActors = false): boolean => {
  const tile = getTile(state.floor, point.x, point.y)
  if (!tile || !known(state, mode, point) || blockedTiles.has(tile.kind) || (tile.kind === 'lockedDoor' && state.hero.keys < 1)) return false
  if (avoidHazards && (hazardTiles.has(tile.kind) || telegraphDanger(state, point))) return false
  return ignoreActors || !actorAt(state.floor, point.x, point.y)
}
const isKnownItem = (state: RunState, mode: AutoplayMode, point: Point, visibleInFog = false): boolean => mode === 'omniscient' || visible(state, point) || visibleInFog
const adjacentCells = (point: Point): Point[] => directions.map(([, delta]) => ({ x: point.x + delta.x, y: point.y + delta.y }))
const hostileKnown = (state: RunState, mode: AutoplayMode) => state.floor.actors.filter(actor => actor.hostile && actor.health > 0 && (mode === 'omniscient' || visible(state, actor)))
const resourceReserve = (policy: AutoplayPolicy): number => policy === 'clear' ? 1 : policy === 'survival' ? 1 : 2

const hostilePressure = (state: RunState, mode: AutoplayMode, point: Point): number => hostileKnown(state, mode).reduce((pressure, actor) => {
  const range = chebyshev(actor, point)
  if (range <= 1) return pressure + 100 + actor.attack * 4
  if (range === 2) return pressure + 25 + actor.attack * 2
  if (actor.ai === 'ranged' && range <= 7 && canAffect(state.floor, actor, point)) return pressure + 100 + actor.attack * 3
  return pressure
}, 0)

const projectedMove = (state: RunState, mode: AutoplayMode, from: Point, direction: Exclude<Direction, 'wait'>, avoidHazards: boolean, ignoreActors: boolean): Point | undefined => {
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
  const route = (avoidHazards: boolean, avoidHostiles: boolean): { command: string; target: Point } | undefined => {
    const initial = { x: state.hero.x, y: state.hero.y }
    const queue: Array<{ point: Point; first?: Direction }> = [{ point: initial }]
    let cursor = 0
    const seen = new Set([pointKey(initial)])
    while (cursor < queue.length) {
      const current = queue[cursor++]
      if (current.first && targetKeys.has(pointKey(current.point))) {
        const first = DIRECTIONS[current.first]
        if (ignoreActors && actorAt(state.floor, initial.x + first.x, initial.y + first.y)) return undefined
        return { command: directionCommands[current.first], target: current.point }
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
    return undefined
  }
  return route(true, avoidThreats) ?? route(true, false)
}

const objectiveTargets = (state: RunState, mode: AutoplayMode): Point[] => {
  const objective = state.floor.objective.kind
  if (objective === 'recoverSupplies') return state.floor.tiles.flatMap((tile, index) => (tile.kind === 'crate' || tile.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  if (objective === 'rescueScout') return state.floor.tiles.flatMap((tile, index) => tile.kind === 'rescue' && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  if (objective === 'invokeAltar') return state.floor.tiles.flatMap((tile, index) => tile.kind === 'altar' && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  return hostileKnown(state, mode).filter(actor => actor.role === 'guardian').map(actor => ({ x: actor.x, y: actor.y }))
}

const hostileInCells = (state: RunState, cells: readonly Point[]): number => cells.reduce((count, point) => count + Number(Boolean(actorAt(state.floor, point.x, point.y)?.hostile)), 0)
const targetDirection = (state: RunState, mode: AutoplayMode, action: 'bomb' | 'throw' | 'spell', item?: string): { direction: Exclude<Direction, 'wait'>; score: number } | undefined => {
  const scored = directions.map(([direction]) => {
    if (action === 'bomb') {
      const cells = actionCells('burst', state.hero, direction, 2).filter(point => known(state, mode, point))
      return { direction, score: hostileInCells(state, cells) * 70 }
    }
    if (action === 'throw') {
      const delta = DIRECTIONS[direction]
      const cells = resolveLineEffect(state.floor, state.hero, { x: state.hero.x + delta.x * 5, y: state.hero.y + delta.y * 5 }).cells
      return { direction, score: hostileInCells(state, cells) * (item === 'fireJar' ? 85 : item === 'spear' ? 54 : 34) }
    }
    const spell = ITEM[item ?? '']?.spell
    if (!spell || !item) return { direction, score: 0 }
    if (spell === 'mend') return { direction, score: state.hero.health <= state.hero.maxHealth - 6 ? 125 : 0 }
    if (spell === 'ward') return { direction, score: hostilePressure(state, mode, state.hero) > 0 && state.hero.health <= state.hero.maxHealth - 4 ? 92 : 0 }
    if (spell === 'sight') return { direction, score: mode === 'visible' && state.floor.tiles.some(tile => !tile.explored) ? 44 : 0 }
    if (spell === 'gate') return { direction, score: state.floor.objective.status === 'complete' && state.floor.guardianDefeated ? 175 : 0 }
    const profile = scriptCastProfile(state.hero, item)
    const delta = DIRECTIONS[direction]
    const point = { x: state.hero.x + delta.x * profile.range, y: state.hero.y + delta.y * profile.range }
    if (spell === 'blink') return { direction, score: getTile(state.floor, point.x, point.y) && passable(state, mode, point, true) && hostilePressure(state, mode, point) + 35 < hostilePressure(state, mode, state.hero) ? 82 : 0 }
    const canHit = resolveLineEffect(state.floor, state.hero, point).cells.some(cell => cell.x === point.x && cell.y === point.y)
    const foe = canHit ? actorAt(state.floor, point.x, point.y) : undefined
    return { direction, score: foe?.hostile ? (spell === 'ember' ? 94 : ['root', 'lull', 'gust', 'pull'].includes(spell) ? 58 : 0) : 0 }
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

const bestUse = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy): string | undefined => {
  const items = state.hero.inventory
  const heal = items.find(id => ITEM[id]?.use === 'heal')
  if (heal && (state.hero.health + 8 <= state.hero.maxHealth || (hostilePressure(state, mode, state.hero) > 0 && state.hero.health < state.hero.maxHealth))) return heal
  const focus = items.find(id => ITEM[id]?.use === 'focus')
  if (focus && state.hero.focus <= 2 && items.some(id => ITEM[id]?.use === 'spell')) return focus
  const map = items.find(id => ITEM[id]?.use === 'map')
  if (map && mode === 'visible' && state.floor.tiles.some(tile => !tile.explored)) return map
  const bomb = items.find(id => ITEM[id]?.use === 'bomb')
  if (bomb && state.hero.bombs <= resourceReserve(policy)) return bomb
  const rope = items.find(id => ITEM[id]?.use === 'rope')
  if (rope && state.hero.ropes <= resourceReserve(policy)) return rope
  const key = items.find(id => ITEM[id]?.use === 'key')
  if (key && state.floor.tiles.some(tile => tile.kind === 'lockedDoor')) return key
  return undefined
}

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
    const clone = structuredClone(state)
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
      return { command: index >= 0 ? String(index + 1) : 'Escape', reason: id ? `use:${id}` : 'close inventory', score: 200 }
    }
    if (modal.mode === 'throw') {
      const id = context.intent?.kind === 'throw' ? context.intent.item : state.hero.inventory.find(id => ITEM[id]?.throwable)
      context.intent = undefined
      const index = id ? state.hero.inventory.indexOf(id) : -1
      return { command: index >= 0 ? String(index + 1) : 'Escape', reason: id ? `throw:${id}` : 'close throw', score: 200 }
    }
    const id = context.intent?.kind === 'equip' ? context.intent.item : bestEquip(state)
    context.intent = undefined
    const index = id ? state.hero.inventory.indexOf(id) : -1
    return { command: index >= 0 ? String(index + 1) : 'Escape', reason: id ? `equip:${id}` : 'close equipment', score: 200 }
  }
  if (modal.kind === 'target') {
    if (modal.direction) return { command: 'Enter', reason: `confirm ${modal.action}`, score: 200 }
    const target = targetDirection(state, mode, modal.action, modal.item)
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

const evadeThreat = (state: RunState, mode: AutoplayMode, context: AutoplayContext): Candidate | undefined => {
  const currentPressure = hostilePressure(state, mode, state.hero)
  const standingInTelegraph = telegraphDanger(state, state.hero)
  if (!standingInTelegraph && currentPressure < 100) return undefined
  const rangedCommitment = hostileKnown(state, mode).filter(actor => actor.ai === 'ranged' && chebyshev(state.hero, actor) <= 7)
    .sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b) || b.attack - a.attack)[0]
  if (standingInTelegraph && rangedCommitment && state.hero.bombs < 1 && state.hero.health > rangedCommitment.attack * 3 + 6) return undefined
  const options = directions.map(([direction, delta]) => ({ direction, point: { x: state.hero.x + delta.x, y: state.hero.y + delta.y } }))
    .filter(option => passable(state, mode, option.point, true) && !telegraphDanger(state, option.point))
    .map(option => ({ ...option, pressure: hostilePressure(state, mode, option.point), repeats: context.recentPositions.filter(key => key === pointKey(option.point)).length }))
  const source = state.floor.telegraphs?.find(telegraph => telegraph.resolveTurn <= state.turn + 1 && telegraph.cells.some(cell => cell.x === state.hero.x && cell.y === state.hero.y))?.sourceId
  const ranged = source ? state.floor.actors.find(actor => actor.id === source && actor.hostile && actor.ai === 'ranged') : undefined
  if (ranged && state.hero.bombs < 1 && state.hero.health > ranged.attack * 3 + 6) {
    const approach = stepTo(state, mode, adjacentCells(ranged), false, false)
    if (approach) return { command: approach.command, reason: `close telegrapher:${ranged.id}`, score: 168 }
  }
  if (ranged && state.hero.bombs > 0) {
    const tactical = options.filter(option => chebyshev(option.point, ranged) >= 2)
      .sort((a, b) => Math.abs(chebyshev(a.point, ranged) - 2) - Math.abs(chebyshev(b.point, ranged) - 2) || a.repeats - b.repeats || a.pressure - b.pressure || a.direction.localeCompare(b.direction))[0]
    if (tactical) return { command: directionCommands[tactical.direction], reason: `evade toward ranged:${ranged.id}`, score: 158 }
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

const breaksPositionCycle = (context: AutoplayContext): boolean => {
  const recent = context.recentPositions
  if (recent.length < 4) return false
  const [a, b, c, d] = recent.slice(-4)
  return a === c && b === d && a !== b
}

const cycleBreakMove = (state: RunState, mode: AutoplayMode, context: AutoplayContext): Candidate | undefined => {
  if (!breaksPositionCycle(context)) return undefined
  const recent = new Set(context.recentPositions.slice(-6))
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

const candidateLookahead = (state: RunState, mode: AutoplayMode, candidate: Candidate, context: AutoplayContext): number => {
  if (candidate.intent || candidate.command === 'b') return 0
  const simulated = structuredClone(state)
  const health = simulated.hero.health
  const turn = simulated.turn
  const events = perform(simulated, candidate.command)
  if (simulated.status === 'dead') return -10000
  if (simulated.modal) return 0
  if (simulated.turn === turn) return -180
  let adjustment = (simulated.hero.health - health) * 18
  if (telegraphDanger(simulated, simulated.hero)) adjustment -= 105
  if (context.recentPositions.includes(pointKey(simulated.hero))) adjustment -= 75
  if (events.some(event => event.type === 'danger')) adjustment -= 14
  if (hostilePressure(simulated, mode, simulated.hero) >= 100) adjustment -= 32
  return adjustment
}

const combatMove = (state: RunState, mode: AutoplayMode): Candidate | undefined => {
  const objectiveComplete = state.floor.objective.status === 'complete' && state.floor.guardianDefeated
  const foes = hostileKnown(state, mode).filter(actor => chebyshev(state.hero, actor) <= 4).sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b))
  const weapon = state.hero.equipment.mainHand ? ITEM[state.hero.equipment.mainHand]?.weapon : undefined
  const shape = weapon?.shape ?? 'adjacent'
  const reach = weapon?.reach ?? 1
  const estimatedDamage = (weapon?.damage ?? 2) + state.hero.stats.strength + 3
  for (const foe of foes) for (const [direction] of directions) {
    if (!actionCells(shape, state.hero, direction, reach).some(point => point.x === foe.x && point.y === foe.y)) continue
    const lethal = foe.health <= estimatedDamage
    const safeExchange = state.hero.health > foe.attack * 2 + 4 && hostilePressure(state, mode, state.hero) < 180
    if (lethal || safeExchange || foe.role === 'guardian') return { command: directionCommands[direction], reason: `melee:${foe.id}`, score: lethal ? 176 : safeExchange ? 164 : foe.role === 'guardian' ? 82 : 64 }
  }
  if (objectiveComplete) return undefined
  const guardian = foes.find(foe => foe.role === 'guardian')
  const route = guardian ? stepTo(state, mode, adjacentCells(guardian), false, false) : undefined
  if (route) return { command: route.command, reason: `approach guardian:${guardian!.id}`, score: 58 }
  if (state.hero.health * 2 < state.hero.maxHealth) return undefined
  const ranged = hostileKnown(state, mode).filter(foe => foe.ai === 'ranged' && chebyshev(state.hero, foe) >= 3 && chebyshev(state.hero, foe) <= 7)
    .sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b) || b.attack - a.attack)[0]
  if (!ranged) return undefined
  const useBomb = state.hero.bombs > 0
  if (!useBomb && state.hero.health <= ranged.attack * 3 + 6) return undefined
  const staging = state.floor.tiles.flatMap((_, index) => {
    const point = { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }
    return known(state, mode, point) && passable(state, mode, point, true) && chebyshev(point, ranged) === (useBomb ? 2 : 1) ? [point] : []
  })
  const stagingRoute = stepTo(state, mode, staging, false, false)
  return stagingRoute ? { command: stagingRoute.command, reason: `${useBomb ? 'close' : 'engage'} ranged:${ranged.id}`, score: useBomb ? 96 : 86 } : undefined
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
  const tile = getTile(state.floor, heroPoint.x, heroPoint.y)
  if (tile?.kind === 'exit' && objectiveComplete) return [{ command: 'q', reason: 'descend', score: 180 }]
  const item = !objectiveComplete && state.floor.items.find(current => current.x === heroPoint.x && current.y === heroPoint.y && isKnownItem(state, mode, current, Boolean(current.visibleInFog)) && (current.id === 'gold' || current.id === 'key' || state.hero.inventory.length < 12))
  if (item && hostilePressure(state, mode, state.hero) < 100) candidates.push({ command: 'g', reason: `pickup:${item.id}`, score: 160 })
  const use = bestUse(state, mode, policy)
  if (use) candidates.push({ command: 'u', reason: `use:${use}`, score: ITEM[use].use === 'heal' ? state.hero.health * 2 <= state.hero.maxHealth ? 220 : hostilePressure(state, mode, state.hero) > 0 ? 180 : 150 : 118, intent: { kind: 'use', item: use } })
  const equip = bestEquip(state)
  if (equip) candidates.push({ command: 'e', reason: `equip:${equip}`, score: 88, intent: { kind: 'equip', item: equip } })
  const pressure = hostilePressure(state, mode, state.hero)
  const bomb = state.hero.bombs > resourceReserve(policy) ? targetDirection(state, mode, 'bomb') : undefined
  if (bomb && (bomb.score >= 140 || (bomb.score >= 70 && pressure >= 100))) candidates.push({ command: 'b', reason: 'bomb tactical cluster', score: (pressure > 0 ? 185 : 110) + bomb.score / 10 })
  const throwable = state.hero.inventory.find(id => id === 'fireJar' || id === 'rock' || (id === 'spear' && state.hero.equipment.mainHand !== 'spear'))
  const throwTarget = throwable ? targetDirection(state, mode, 'throw', throwable) : undefined
  const throwThreshold = throwable === 'fireJar' ? 85 : throwable === 'spear' ? 54 : 34
  if (throwable && throwTarget && throwTarget.score >= throwThreshold) candidates.push({ command: 't', reason: `throw:${throwable}`, score: 96 + throwTarget.score / 10, intent: { kind: 'throw', item: throwable } })
  const spell = state.hero.inventory.filter(id => ITEM[id]?.use === 'spell').map(id => ({ id, target: targetDirection(state, mode, 'spell', id), profile: scriptCastProfile(state.hero, id) }))
    .filter((choice): choice is { id: string; target: { direction: Exclude<Direction, 'wait'>; score: number }; profile: ReturnType<typeof scriptCastProfile> } => Boolean(choice.target) && state.hero.focus >= choice.profile.focusCost)
    .sort((a, b) => b.target.score - a.target.score || a.id.localeCompare(b.id))[0]
  if (spell) candidates.push({ command: 'u', reason: `cast:${spell.id}`, score: 90 + spell.target.score / 5, intent: { kind: 'use', item: spell.id } })
  const nearbyContainer = adjacentCells(heroPoint).some(point => ['crate', 'chest'].includes(getTile(state.floor, point.x, point.y)?.kind ?? ''))
  const nearLockedDoor = adjacentCells(heroPoint).some(point => getTile(state.floor, point.x, point.y)?.kind === 'lockedDoor')
  const merchant = state.floor.actors.find(actor => actor.role === 'merchant' && chebyshev(actor, state.hero) <= 1)
  const nearMerchant = Boolean(merchant) && !context.closedMerchants.has(merchant!.id)
  const friendly = state.floor.actors.some(actor => actor.role === 'ally' && chebyshev(actor, state.hero) <= 1)
  const viableGate = nearLockedDoor && gateChoice(state, policy) !== undefined
  if (viableGate || (!nearLockedDoor && !merchant && (nearbyContainer || tile?.kind === 'rescue' || tile?.kind === 'altar' || friendly))) {
    if (tile?.kind !== 'altar' || state.hero.gold >= 75) candidates.push({ command: 'c', reason: nearMerchant ? 'merchant' : viableGate ? 'gate' : 'operate objective', score: nearMerchant ? 82 : 135 })
  }
  if (nearMerchant && bestShopItem(state, policy)) candidates.push({ command: 'c', reason: 'merchant', score: 82 })
  if (state.hero.ropes > resourceReserve(policy) && (tile?.kind === 'pit' || getTile(state.floor, heroPoint.x, heroPoint.y + 1)?.kind === 'pit')) candidates.push({ command: 'r', reason: 'bridge pit', score: 122 })
  const cycleBreak = cycleBreakMove(state, mode, context)
  if (cycleBreak) candidates.push(cycleBreak)
  const evade = evadeThreat(state, mode, context)
  if (evade) candidates.push(evade)
  const combat = combatMove(state, mode)
  if (combat) candidates.push(combat)
  const objective = state.floor.objective
  if (objective.status !== 'complete') {
    const targets = objectiveTargets(state, mode)
    const needsAdjacent = objective.kind === 'recoverSupplies' || objective.kind === 'rescueScout' || objective.kind === 'invokeAltar' || objective.kind === 'defeatGuardian'
    const route = stepTo(state, mode, needsAdjacent ? targets.flatMap(adjacentCells) : targets)
    if (route) candidates.push({ command: route.command, reason: `objective:${objective.kind}`, score: 70 })
  }
  if (objectiveComplete) {
    const exitRoute = stepTo(state, mode, [state.floor.exit], false, true, true)
    if (exitRoute) candidates.push({ command: exitRoute.command, reason: 'reach exit', score: 140 })
    else if (!evade) candidates.push({ command: 'l', reason: 'await exit opening', score: 32 })
  } else {
    const items = state.floor.items.filter(current => isKnownItem(state, mode, current, Boolean(current.visibleInFog))).map(current => ({ x: current.x, y: current.y }))
    const itemRoute = stepTo(state, mode, items)
    if (itemRoute) candidates.push({ command: itemRoute.command, reason: 'reach loot', score: 48 })
    const containers = state.floor.tiles.flatMap((current, index) => (current.kind === 'crate' || current.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
    const containerRoute = stepTo(state, mode, containers.flatMap(adjacentCells))
    if (containerRoute) candidates.push({ command: containerRoute.command, reason: 'reach container', score: 43 })
    const frontier = explorationMove(state, mode)
    if (frontier) candidates.push(frontier)
  }
  return candidates
}

export const autoplayDecision = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy = 'survival', context: AutoplayContext = createAutoplayContext()): AutoplayDecision | undefined => {
  if (mode === 'off' || state.status !== 'playing') return undefined
  const modal = modalDecision(state, mode, policy, context)
  if (modal) return { command: modal.command, reason: modal.reason, candidates: [modal] }
  const fingerprint = autoplayStateFingerprint(state)
  if ((context.visits.get(fingerprint) ?? 0) >= 6) return undefined
  const candidates = immediateCandidates(state, mode, policy, context).map(candidate => ({ ...candidate, score: candidate.score - (context.failed.get(candidate.command) ?? 0) * 60 + candidateLookahead(state, mode, candidate, context) }))
  const selected = candidates.sort((a, b) => b.score - a.score || a.command.localeCompare(b.command) || a.reason.localeCompare(b.reason))[0]
  if (!selected || selected.score <= 0) return undefined
  context.intent = selected.intent
  context.lastReason = selected.reason
  return { command: selected.command, reason: selected.reason, candidates: candidates.slice(0, 8).map(({ command, reason, score }) => ({ command, reason, score })) }
}

export const autoplayCommand = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy = 'survival', context?: AutoplayContext): string | undefined => autoplayDecision(state, mode, policy, context)?.command
