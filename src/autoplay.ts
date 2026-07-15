import { ITEM } from './content'
import { actionCells, skillChoices } from './engine'
import { resolveAreaGate, gateForArea } from './engine/gates'
import { merchantStock } from './engine/rewards'
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
export interface AutoplayContext { visits: Map<string, number>; failed: Map<string, number>; intent?: Intent; shopTurns: number; lastReason?: string }

export const createAutoplayContext = (): AutoplayContext => ({ visits: new Map(), failed: new Map(), shopTurns: 0 })
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
  if (before.modal?.kind === 'shop' && /^\d+$/.test(command) && after.turn > before.turn) context.shopTurns++
  if (after.modal?.kind !== 'shop') context.shopTurns = 0
}

const known = (state: RunState, mode: AutoplayMode, point: Point): boolean => mode === 'omniscient' || Boolean(getTile(state.floor, point.x, point.y)?.explored)
const visible = (state: RunState, point: Point): boolean => Boolean(getTile(state.floor, point.x, point.y)?.visible)
const telegraphDanger = (state: RunState, point: Point): boolean => (state.floor.telegraphs ?? []).some(telegraph => telegraph.resolveTurn <= state.turn + 1 && telegraph.cells.some(cell => cell.x === point.x && cell.y === point.y))
const passable = (state: RunState, mode: AutoplayMode, point: Point, avoidHazards: boolean): boolean => {
  const tile = getTile(state.floor, point.x, point.y)
  if (!tile || !known(state, mode, point) || blockedTiles.has(tile.kind) || (tile.kind === 'lockedDoor' && state.hero.keys < 1)) return false
  if (avoidHazards && (hazardTiles.has(tile.kind) || telegraphDanger(state, point))) return false
  return !actorAt(state.floor, point.x, point.y)
}
const directionToward = (from: Point, to: Point): Exclude<Direction, 'wait'> => {
  const dx = Math.sign(to.x - from.x)
  const dy = Math.sign(to.y - from.y)
  return (dx === -1 && dy === -1 ? 'nw' : dx === 0 && dy === -1 ? 'n' : dx === 1 && dy === -1 ? 'ne' : dx === -1 && dy === 0 ? 'w' : dx === 1 && dy === 0 ? 'e' : dx === -1 && dy === 1 ? 'sw' : dx === 0 && dy === 1 ? 's' : 'se')
}
const isKnownItem = (state: RunState, mode: AutoplayMode, point: Point, visibleInFog = false): boolean => mode === 'omniscient' || visible(state, point) || visibleInFog
const adjacentCells = (point: Point): Point[] => directions.map(([, delta]) => ({ x: point.x + delta.x, y: point.y + delta.y }))
const hostileKnown = (state: RunState, mode: AutoplayMode) => state.floor.actors.filter(actor => actor.hostile && actor.health > 0 && (mode === 'omniscient' || visible(state, actor)))
const resourceReserve = (policy: AutoplayPolicy): number => policy === 'clear' ? 0 : policy === 'survival' ? 1 : 2

const stepTo = (state: RunState, mode: AutoplayMode, targets: readonly Point[], allowTargetOccupied = false): { command: string; target: Point } | undefined => {
  if (!targets.length) return undefined
  const targetKeys = new Set(targets.map(pointKey))
  const route = (avoidHazards: boolean): { command: string; target: Point } | undefined => {
    const initial = { x: state.hero.x, y: state.hero.y }
    const queue: Array<{ point: Point; first?: Direction }> = [{ point: initial }]
    const seen = new Set([pointKey(initial)])
    while (queue.length) {
      const current = queue.shift()!
      if (current.first && targetKeys.has(pointKey(current.point))) return { command: directionCommands[current.first], target: current.point }
      for (const [direction, delta] of directions) {
        const point = { x: current.point.x + delta.x, y: current.point.y + delta.y }
        const nextKey = pointKey(point)
        if (seen.has(nextKey)) continue
        const target = targetKeys.has(nextKey)
        if (!(target && allowTargetOccupied) && !passable(state, mode, point, avoidHazards)) continue
        if (target && allowTargetOccupied && !known(state, mode, point)) continue
        seen.add(nextKey)
        queue.push({ point, first: current.first ?? direction })
      }
    }
    return undefined
  }
  return route(true) ?? route(false)
}

const objectiveTargets = (state: RunState, mode: AutoplayMode): Point[] => {
  const objective = state.floor.objective.kind
  if (objective === 'recoverSupplies') return state.floor.tiles.flatMap((tile, index) => (tile.kind === 'crate' || tile.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  if (objective === 'rescueScout') return state.floor.tiles.flatMap((tile, index) => tile.kind === 'rescue' && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  if (objective === 'invokeAltar') return state.floor.tiles.flatMap((tile, index) => tile.kind === 'altar' && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  return hostileKnown(state, mode).filter(actor => actor.role === 'guardian').map(actor => ({ x: actor.x, y: actor.y }))
}

const hostileInCells = (state: RunState, cells: readonly Point[]): number => cells.reduce((count, point) => count + Number(Boolean(actorAt(state.floor, point.x, point.y)?.hostile)), 0)
const terrainInCells = (state: RunState, cells: readonly Point[]): number => cells.reduce((count, point) => count + Number(['rubble', 'bramble', 'crate', 'chest', 'wall'].includes(getTile(state.floor, point.x, point.y)?.kind ?? '')), 0)
const targetDirection = (state: RunState, mode: AutoplayMode, action: 'bomb' | 'throw' | 'spell', item?: string): { direction: Exclude<Direction, 'wait'>; score: number } | undefined => {
  const foes = hostileKnown(state, mode)
  const scored = directions.map(([direction]) => {
    if (action === 'bomb') {
      const cells = actionCells('burst', state.hero, direction, 2)
      return { direction, score: hostileInCells(state, cells) * 70 + terrainInCells(state, cells) * 20 }
    }
    if (action === 'throw') {
      const cells = actionCells('line', state.hero, direction, 5)
      return { direction, score: hostileInCells(state, cells) * (item === 'fireJar' ? 85 : 42) }
    }
    const spell = ITEM[item ?? '']?.spell
    if (spell === 'mend' || spell === 'ward' || spell === 'sight' || spell === 'gate') return { direction, score: 1 }
    const foe = foes.filter(actor => directionToward(state.hero, actor) === direction).sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b))[0]
    return { direction, score: foe ? (spell === 'ember' ? 90 : ['root', 'lull', 'gust', 'pull'].includes(spell ?? '') ? 48 : 15) : 0 }
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

const bestUse = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy, context: AutoplayContext): string | undefined => {
  const items = state.hero.inventory
  const heal = items.find(id => ITEM[id]?.use === 'heal')
  if (heal && state.hero.health * (policy === 'survival' ? 2 : 3) <= state.hero.maxHealth) return heal
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
  const teleport = items.find(id => ITEM[id]?.use === 'teleport')
  if (teleport && (context.visits.get(autoplayStateFingerprint(state)) ?? 0) >= 3) return teleport
  return undefined
}

const gateChoice = (state: RunState, policy: AutoplayPolicy): number | undefined => {
  const gate = gateForArea(state.area ?? state.floor.biome)
  return gate.tagAlternatives.map((option, index) => {
    const clone = structuredClone(state)
    const resolution = resolveAreaGate(clone, gate, index)
    if (!resolution.resolved) return { index, score: Number.NEGATIVE_INFINITY }
    const cost = option.cost ?? gate.cost
    const irreversible = option.kind === 'npc' ? (policy === 'legacy' ? 1000 : policy === 'survival' ? 300 : 45) : option.kind === 'bomb' ? (policy === 'legacy' ? 180 : policy === 'survival' ? 80 : 20) : 0
    return { index, score: 1000 - cost.gold - cost.items.reduce((sum, id) => sum + ITEM[id].value, 0) - irreversible }
  }).sort((a, b) => b.score - a.score || a.index - b.index)[0]?.index
}

const modalDecision = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy, context: AutoplayContext): Candidate | undefined => {
  const modal = state.modal
  if (!modal) return undefined
  if (modal.kind === 'skills') return { command: skillChoices(state).length ? '1' : 'Escape', reason: 'discipline', score: 200 }
  if (modal.kind === 'inventory') {
    if (modal.mode === 'use') {
      const id = context.intent?.kind === 'use' ? context.intent.item : bestUse(state, mode, policy, context)
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
    if (context.shopTurns >= maxTurns) return { command: 'Escape', reason: 'leave merchant', score: 200 }
    const stock = merchantStock(state).filter(id => state.hero.gold >= ITEM[id].value && state.hero.inventory.length < 12)
    const desired = stock.filter(id => ITEM[id].use === 'heal' || ITEM[id].use === 'bomb' || ITEM[id].use === 'rope' || Boolean(ITEM[id].slot)).sort((a, b) => ITEM[b].value - ITEM[a].value)[0]
    const index = desired ? merchantStock(state).indexOf(desired) : -1
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

const combatMove = (state: RunState, mode: AutoplayMode): Candidate | undefined => {
  const foes = hostileKnown(state, mode).filter(actor => chebyshev(state.hero, actor) <= 4).sort((a, b) => chebyshev(state.hero, a) - chebyshev(state.hero, b))
  const weapon = state.hero.equipment.mainHand ? ITEM[state.hero.equipment.mainHand]?.weapon : undefined
  const shape = weapon?.shape ?? 'adjacent'
  const reach = weapon?.reach ?? 1
  for (const foe of foes) for (const [direction] of directions) if (actionCells(shape, state.hero, direction, reach).some(point => point.x === foe.x && point.y === foe.y)) return { command: directionCommands[direction], reason: `melee:${foe.id}`, score: 105 }
  const nearest = foes[0]
  const route = nearest ? stepTo(state, mode, adjacentCells(nearest)) : undefined
  return route ? { command: route.command, reason: `approach:${nearest.id}`, score: 78 } : undefined
}

const explorationMove = (state: RunState, mode: AutoplayMode): Candidate | undefined => {
  const unknown = directions.filter(([, delta]) => !known(state, mode, { x: state.hero.x + delta.x, y: state.hero.y + delta.y }))
  if (unknown.length) return { command: directionCommands[unknown[0][0]], reason: 'explore frontier', score: 25 }
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
  const item = state.floor.items.find(current => current.x === heroPoint.x && current.y === heroPoint.y && isKnownItem(state, mode, current, Boolean(current.visibleInFog)) && (current.id === 'gold' || current.id === 'key' || state.hero.inventory.length < 12))
  if (item) candidates.push({ command: 'g', reason: `pickup:${item.id}`, score: 160 })
  const tile = getTile(state.floor, heroPoint.x, heroPoint.y)
  if (tile?.kind === 'exit' && state.floor.objective.status === 'complete' && state.floor.guardianDefeated) candidates.push({ command: 'q', reason: 'descend', score: 180 })
  const use = bestUse(state, mode, policy, context)
  if (use) candidates.push({ command: 'u', reason: `use:${use}`, score: ITEM[use].use === 'heal' ? 150 : 118, intent: { kind: 'use', item: use } })
  const equip = bestEquip(state)
  if (equip) candidates.push({ command: 'e', reason: `equip:${equip}`, score: 88, intent: { kind: 'equip', item: equip } })
  const bomb = state.hero.bombs > resourceReserve(policy) ? targetDirection(state, mode, 'bomb') : undefined
  if (bomb && bomb.score >= (policy === 'clear' ? 20 : 70)) candidates.push({ command: 'b', reason: 'bomb tactical cluster', score: 110 + bomb.score / 10 })
  const throwable = state.hero.inventory.filter(id => ITEM[id]?.throwable).sort((a, b) => Number(b === 'fireJar') - Number(a === 'fireJar'))[0]
  const throwTarget = throwable ? targetDirection(state, mode, 'throw', throwable) : undefined
  if (throwable && throwTarget && throwTarget.score >= 42) candidates.push({ command: 't', reason: `throw:${throwable}`, score: 105 + throwTarget.score / 10, intent: { kind: 'throw', item: throwable } })
  const spell = state.hero.inventory.find(id => ITEM[id]?.use === 'spell' && targetDirection(state, mode, 'spell', id))
  if (spell && state.hero.focus >= 3) candidates.push({ command: 'u', reason: `cast:${spell}`, score: spell === 'mend' && state.hero.health * 2 <= state.hero.maxHealth ? 145 : 100, intent: { kind: 'use', item: spell } })
  const nearbyContainer = adjacentCells(heroPoint).some(point => ['crate', 'chest'].includes(getTile(state.floor, point.x, point.y)?.kind ?? ''))
  const nearLockedDoor = adjacentCells(heroPoint).some(point => getTile(state.floor, point.x, point.y)?.kind === 'lockedDoor')
  const nearMerchant = state.floor.actors.some(actor => actor.role === 'merchant' && chebyshev(actor, state.hero) <= 1)
  const friendly = state.floor.actors.some(actor => actor.role === 'ally' && chebyshev(actor, state.hero) <= 1)
  if (nearLockedDoor || nearMerchant || nearbyContainer || tile?.kind === 'rescue' || tile?.kind === 'altar' || friendly) {
    if (tile?.kind !== 'altar' || state.hero.gold >= 75) candidates.push({ command: 'c', reason: nearMerchant ? 'merchant' : nearLockedDoor ? 'gate' : 'operate objective', score: nearMerchant ? 82 : 135 })
  }
  if (state.hero.ropes > resourceReserve(policy) && (tile?.kind === 'pit' || getTile(state.floor, heroPoint.x, heroPoint.y + 1)?.kind === 'pit')) candidates.push({ command: 'r', reason: 'bridge pit', score: 122 })
  const combat = combatMove(state, mode)
  if (combat) candidates.push(combat)
  const objective = state.floor.objective
  if (objective.status !== 'complete') {
    const targets = objectiveTargets(state, mode)
    const needsAdjacent = objective.kind === 'recoverSupplies' || objective.kind === 'rescueScout' || objective.kind === 'invokeAltar' || objective.kind === 'defeatGuardian'
    const route = stepTo(state, mode, needsAdjacent ? targets.flatMap(adjacentCells) : targets)
    if (route) candidates.push({ command: route.command, reason: `objective:${objective.kind}`, score: 70 })
  }
  const items = state.floor.items.filter(current => isKnownItem(state, mode, current, Boolean(current.visibleInFog))).map(current => ({ x: current.x, y: current.y }))
  const itemRoute = stepTo(state, mode, items)
  if (itemRoute) candidates.push({ command: itemRoute.command, reason: 'reach loot', score: 48 })
  const containers = state.floor.tiles.flatMap((current, index) => (current.kind === 'crate' || current.kind === 'chest') && known(state, mode, { x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH) }] : [])
  const containerRoute = stepTo(state, mode, containers.flatMap(adjacentCells))
  if (containerRoute) candidates.push({ command: containerRoute.command, reason: 'reach container', score: 43 })
  if (objective.status === 'complete' && state.floor.guardianDefeated) {
    const exitRoute = stepTo(state, mode, [state.floor.exit])
    if (exitRoute) candidates.push({ command: exitRoute.command, reason: 'reach exit', score: 140 })
  }
  const frontier = explorationMove(state, mode)
  if (frontier) candidates.push(frontier)
  return candidates
}

export const autoplayDecision = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy = 'survival', context: AutoplayContext = createAutoplayContext()): AutoplayDecision | undefined => {
  if (mode === 'off' || state.status !== 'playing') return undefined
  const modal = modalDecision(state, mode, policy, context)
  if (modal) return { command: modal.command, reason: modal.reason, candidates: [modal] }
  const fingerprint = autoplayStateFingerprint(state)
  if ((context.visits.get(fingerprint) ?? 0) >= 6) return undefined
  const candidates = immediateCandidates(state, mode, policy, context).map(candidate => ({ ...candidate, score: candidate.score - (context.failed.get(candidate.command) ?? 0) * 60 }))
  const selected = candidates.sort((a, b) => b.score - a.score || a.command.localeCompare(b.command) || a.reason.localeCompare(b.reason))[0]
  if (!selected || selected.score <= 0) return undefined
  context.intent = selected.intent
  context.lastReason = selected.reason
  return { command: selected.command, reason: selected.reason, candidates: candidates.slice(0, 8).map(({ command, reason, score }) => ({ command, reason, score })) }
}

export const autoplayCommand = (state: RunState, mode: AutoplayMode, policy: AutoplayPolicy = 'survival', context?: AutoplayContext): string | undefined => autoplayDecision(state, mode, policy, context)?.command
