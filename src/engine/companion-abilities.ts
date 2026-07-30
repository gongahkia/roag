import type { Actor, Companion, CompanionActionCategory, Point, RunState, TileKind } from '../types'
import { getTile } from '../world'
import { isLegalCompanionRoleAction } from './companion-roles'
import { companionTerrainMutationAssessment, companionTerrainTargets, type CompanionTerrainAction } from './companion-traversal'
import { resolvePartySupport, resolvePartyTargets } from './party-resolution'
import { distance } from './shared'

const hazards = new Set<TileKind>(['spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder', 'bramble', 'rubble', 'water', 'deepWater', 'lava', 'pit', 'brine', 'frostRime'])
const pointCompare = (left: Point, right: Point): number => left.y - right.y || left.x - right.x
const samePoint = (left: Point, right: Point): boolean => left.x === right.x && left.y === right.y
const visible = (state: RunState, point: Point): boolean => getTile(state.floor, point.x, point.y)?.visible === true
const rangeFor = (action: CompanionActionCategory): number => action === 'intercept' ? 2 : action === 'protect' || action === 'traverse' ? 1 : action === 'stabilizeTerrain' || action === 'stabilizeHazard' ? 2 : action === 'ward' ? 4 : 6
const effectFor = (action: CompanionActionCategory): string => action === 'intercept' ? 'intercept stance armed' : action === 'protect' ? 'courier shielded' : action === 'observe' ? 'trail sign assessed' : action === 'mark' ? 'hazard marked' : action === 'traverse' ? 'crossing shielded' : action === 'stabilizeTerrain' ? 'terrain stabilized' : action === 'ward' ? 'ward shielded courier' : 'hazard stabilized'
const ordered = (state: RunState, points: readonly Point[], range: number): Point[] => [...points].filter(point => distance(state.hero, point) <= range).sort((left, right) => distance(state.hero, left) - distance(state.hero, right) || pointCompare(left, right))
const visibleHostiles = (state: RunState, range: number): Actor[] => state.floor.actors.filter(actor => actor.hostile && actor.health > 0 && visible(state, actor) && distance(state.hero, actor) <= range).sort((left, right) => distance(state.hero, left) - distance(state.hero, right) || left.id.localeCompare(right.id))
const signTargets = (state: RunState): Point[] => ordered(state, [
  ...state.floor.props.filter(prop => prop.state !== 'destroyed' && visible(state, prop)).map(prop => ({ x: prop.x, y: prop.y })),
  ...state.floor.milestones.filter(milestone => !milestone.claimed && visible(state, milestone)).map(milestone => ({ x: milestone.x, y: milestone.y }))
], rangeFor('observe'))
const hazardTargets = (state: RunState): Point[] => ordered(state, state.floor.tiles.flatMap((tile, index) => tile.visible && hazards.has(tile.kind) ? [{ x: index % state.floor.width, y: Math.floor(index / state.floor.width) }] : []), rangeFor('mark'))
const targetLabel = (state: RunState, point: Point): string => samePoint(state.hero, point) ? 'courier' : state.floor.actors.find(actor => actor.health > 0 && samePoint(actor, point))?.name ?? state.floor.props.find(prop => prop.state !== 'destroyed' && samePoint(prop, point))?.id ?? state.floor.milestones.find(milestone => samePoint(milestone, point))?.id ?? getTile(state.floor, point.x, point.y)?.kind ?? 'ground'

export type CompanionAbilityReason = 'ready' | 'illegal-action' | 'cooldown' | 'no-visible-target' | 'target-unseen' | 'target-out-of-range' | 'target-invalid' | 'target-protected' | 'target-occupied' | 'route-blocked' | 'source-out-of-range' | 'friendly-fire'
export interface CompanionAbilityAssessment { action: CompanionActionCategory; ready: boolean; reason: CompanionAbilityReason; target?: Point; targetLabel?: string; range: number; cooldown: number; cost: 'companion cooldown'; effect: string }

export const companionAbilityTarget = (state: RunState, _companion: Companion, _actor: Actor, action: CompanionActionCategory): Point | undefined => {
  if (action === 'intercept') {
    const target = visibleHostiles(state, rangeFor(action))[0]
    return target ? { x: target.x, y: target.y } : undefined
  }
  if (action === 'protect' || action === 'traverse') return { x: state.hero.x, y: state.hero.y }
  if (action === 'observe') return signTargets(state)[0]
  if (action === 'mark') return hazardTargets(state)[0]
  if (action === 'ward') return ordered(state, state.floor.tiles.flatMap((tile, index) => tile.kind === 'altar' && tile.visible && tile.explored ? [{ x: index % state.floor.width, y: Math.floor(index / state.floor.width) }] : []), rangeFor(action))[0]
  const terrainAction = action as CompanionTerrainAction
  return companionTerrainTargets(state, terrainAction).find(point => distance(state.hero, point) <= rangeFor(action))
}

export const assessCompanionAbility = (state: RunState, companion: Companion, actor: Actor, action: CompanionActionCategory, target = companionAbilityTarget(state, companion, actor, action)): CompanionAbilityAssessment => {
  const cooldown = companion.abilityState.cooldowns[action] ?? 0
  const base = { action, target: target ? { ...target } : undefined, targetLabel: target ? targetLabel(state, target) : undefined, range: rangeFor(action), cooldown, cost: 'companion cooldown' as const, effect: effectFor(action) }
  if (!isLegalCompanionRoleAction(companion.role, action)) return { ...base, ready: false, reason: 'illegal-action' }
  if (cooldown) return { ...base, ready: false, reason: 'cooldown' }
  if (!target) return { ...base, ready: false, reason: 'no-visible-target' }
  if (!visible(state, target)) return { ...base, ready: false, reason: 'target-unseen' }
  if (distance(state.hero, target) > rangeFor(action)) return { ...base, ready: false, reason: 'target-out-of-range' }
  if (action === 'intercept') {
    const resolution = resolvePartyTargets(state, actor.id, 'line', [target])
    return resolution.resolved ? { ...base, ready: true, reason: 'ready' } : { ...base, ready: false, reason: resolution.reason === 'friendly-fire' ? 'friendly-fire' : 'target-invalid' }
  }
  if (action === 'protect') {
    if (!samePoint(target, state.hero)) return { ...base, ready: false, reason: 'target-invalid' }
    if (distance(actor, state.hero) > rangeFor(action)) return { ...base, ready: false, reason: 'source-out-of-range' }
    return resolvePartySupport(state, actor.id, [target]).resolved ? { ...base, ready: true, reason: 'ready' } : { ...base, ready: false, reason: 'friendly-fire' }
  }
  if (action === 'observe') return signTargets(state).some(point => samePoint(point, target)) ? { ...base, ready: true, reason: 'ready' } : { ...base, ready: false, reason: 'target-invalid' }
  if (action === 'mark') return hazardTargets(state).some(point => samePoint(point, target)) ? { ...base, ready: true, reason: 'ready' } : { ...base, ready: false, reason: 'target-invalid' }
  if (action === 'traverse') {
    if (!samePoint(target, state.hero) || !hazards.has(getTile(state.floor, target.x, target.y)?.kind ?? 'floor')) return { ...base, ready: false, reason: 'target-invalid' }
    return resolvePartySupport(state, actor.id, [target]).resolved ? { ...base, ready: true, reason: 'ready' } : { ...base, ready: false, reason: 'friendly-fire' }
  }
  if (action === 'ward') {
    if (getTile(state.floor, target.x, target.y)?.kind !== 'altar' || !getTile(state.floor, target.x, target.y)?.explored) return { ...base, ready: false, reason: 'target-invalid' }
    return resolvePartySupport(state, actor.id, [{ x: state.hero.x, y: state.hero.y }]).resolved ? { ...base, ready: true, reason: 'ready' } : { ...base, ready: false, reason: 'friendly-fire' }
  }
  const terrain = companionTerrainMutationAssessment(state, action as CompanionTerrainAction, target)
  return terrain.ready ? { ...base, ready: true, reason: 'ready' } : { ...base, ready: false, reason: terrain.reason }
}
