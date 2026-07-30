import type { Actor, Point, RunState } from '../types'
import { actorAt, getTile, isPassable } from '../world'
import { activeCompanionRoster, companionActorId, isCompanionActor } from './party'

export type PartyActorTeam = 'courier' | 'companion' | 'enemy' | 'neutral'
export type PartyResolutionReason = 'resolved' | 'blocked-bounds' | 'blocked-terrain' | 'blocked-occupant' | 'friendly-fire' | 'no-target' | 'duplicate-target' | 'source-missing'
export interface PartyResolution { sourceId: string; kind: 'move' | 'line' | 'area' | 'support'; resolved: boolean; reason: PartyResolutionReason; targets: string[]; fallback?: 'wait' }
export interface PartyTarget { id: string; team: PartyActorTeam; point: Point; actor?: Actor }

const courierId = 'courier'
const same = (left: Point, right: Point): boolean => left.x === right.x && left.y === right.y
export const partyTeam = (actor: Actor | undefined): PartyActorTeam => !actor ? 'courier' : actor.hostile ? 'enemy' : isCompanionActor(actor) ? 'companion' : 'neutral'
export const partyActionOrder = (state: RunState): string[] => [courierId, ...activeCompanionRoster(state.companions ?? []).map(companion => companionActorId(companion.id)), ...state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => actor.id).sort()]
const source = (state: RunState, id: string): PartyTarget | undefined => id === courierId ? { id, team: 'courier', point: { x: state.hero.x, y: state.hero.y } } : (() => {
  const actor = state.floor.actors.find(candidate => candidate.id === id && candidate.health > 0)
  return actor ? { id, team: partyTeam(actor), point: { x: actor.x, y: actor.y }, actor } : undefined
})()
const targetAt = (state: RunState, point: Point): PartyTarget | undefined => same(state.hero, point) ? { id: courierId, team: 'courier', point: { ...point } } : (() => {
  const actor = actorAt(state.floor, point.x, point.y)
  return actor ? { id: actor.id, team: partyTeam(actor), point: { ...point }, actor } : undefined
})()
const hostileTo = (sourceTeam: PartyActorTeam, targetTeam: PartyActorTeam): boolean => sourceTeam === 'enemy' ? targetTeam === 'courier' || targetTeam === 'companion' : sourceTeam === 'courier' || sourceTeam === 'companion' ? targetTeam === 'enemy' : false

export const resolvePartyMove = (state: RunState, sourceId: string, destination: Point): PartyResolution => {
  const current = source(state, sourceId)
  if (!current) return { sourceId, kind: 'move', resolved: false, reason: 'source-missing', targets: [], fallback: 'wait' }
  const tile = getTile(state.floor, destination.x, destination.y)
  if (!tile) return { sourceId, kind: 'move', resolved: false, reason: 'blocked-bounds', targets: [], fallback: 'wait' }
  const occupant = targetAt(state, destination)
  if (occupant && occupant.id !== sourceId) return { sourceId, kind: 'move', resolved: false, reason: 'blocked-occupant', targets: [occupant.id], fallback: 'wait' }
  if (!isPassable(state.floor, destination.x, destination.y)) return { sourceId, kind: 'move', resolved: false, reason: 'blocked-terrain', targets: [], fallback: 'wait' }
  if (current.actor) { current.actor.x = destination.x; current.actor.y = destination.y } else { state.hero.x = destination.x; state.hero.y = destination.y }
  return { sourceId, kind: 'move', resolved: true, reason: 'resolved', targets: [] }
}

export const resolvePartyTargets = (state: RunState, sourceId: string, kind: 'line' | 'area', cells: readonly Point[]): PartyResolution => {
  const current = source(state, sourceId)
  if (!current) return { sourceId, kind, resolved: false, reason: 'source-missing', targets: [], fallback: 'wait' }
  const seen = new Set<string>()
  const targets: string[] = []
  let friendly = false
  for (const cell of cells) {
    const target = targetAt(state, cell)
    if (!target || target.id === sourceId) continue
    if (seen.has(target.id)) continue
    seen.add(target.id)
    if (!hostileTo(current.team, target.team)) { friendly = true; continue }
    targets.push(target.id)
  }
  if (targets.length) return { sourceId, kind, resolved: true, reason: 'resolved', targets }
  return { sourceId, kind, resolved: false, reason: friendly ? 'friendly-fire' : 'no-target', targets: [], fallback: 'wait' }
}

export const resolvePartySupport = (state: RunState, sourceId: string, cells: readonly Point[]): PartyResolution => {
  const current = source(state, sourceId)
  if (!current) return { sourceId, kind: 'support', resolved: false, reason: 'source-missing', targets: [], fallback: 'wait' }
  const seen = new Set<string>()
  const targets: string[] = []
  let hostile = false
  for (const cell of cells) {
    const target = targetAt(state, cell)
    if (!target || target.id === sourceId || seen.has(target.id)) continue
    seen.add(target.id)
    if (hostileTo(current.team, target.team)) { hostile = true; continue }
    targets.push(target.id)
  }
  return targets.length ? { sourceId, kind: 'support', resolved: true, reason: 'resolved', targets } : { sourceId, kind: 'support', resolved: false, reason: hostile ? 'friendly-fire' : 'no-target', targets: [], fallback: 'wait' }
}

export const removeDefeatedPartyActors = (state: RunState): string[] => {
  const removed = state.floor.actors.filter(actor => actor.health <= 0).map(actor => actor.id).sort()
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  return removed
}
