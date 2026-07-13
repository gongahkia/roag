import type { Actor, Hero, RunState, TileKind } from '../types'
import { actorAt, getTile } from '../world'
import { log } from './shared'

export type DisplacementKind = 'push' | 'pull' | 'swap' | 'knockback'
export interface DisplacementOutcome { kind: DisplacementKind; moved: boolean; from: { x: number; y: number }; to?: { x: number; y: number }; blocked?: string; hazard?: TileKind }

type Displaceable = Actor | Hero

const blockingTile = (kind: TileKind): boolean => ['wall', 'door', 'lockedDoor', 'rubble', 'bramble', 'crate', 'chest', 'pit'].some(blocker => blocker === kind)
const label = (target: Displaceable): string => 'name' in target ? target.name : 'You'
const hazardDamage = (kind: TileKind): number => kind === 'lava' ? 8 : kind === 'boulder' ? 6 : kind === 'spikes' || kind === 'dart' || kind === 'fireVent' ? 4 : kind === 'gas' ? 2 : 0

export const resolveDisplacement = (state: RunState, source: Displaceable, target: Displaceable, kind: DisplacementKind): DisplacementOutcome => {
  const from = { x: target.x, y: target.y }
  if (kind === 'swap') {
    const sourcePosition = { x: source.x, y: source.y }
    source.x = target.x
    source.y = target.y
    target.x = sourcePosition.x
    target.y = sourcePosition.y
    log(state, `${label(source)} and ${label(target)} swap places.`)
    return { kind, moved: true, from, to: { x: target.x, y: target.y } }
  }
  const direction = { x: Math.sign(target.x - source.x), y: Math.sign(target.y - source.y) }
  if (!direction.x && !direction.y) { log(state, `${label(target)} cannot be moved.`); return { kind, moved: false, from, blocked: 'source' } }
  const multiplier = kind === 'pull' ? -1 : 1
  const destination = { x: target.x + direction.x * multiplier, y: target.y + direction.y * multiplier }
  const tile = getTile(state.floor, destination.x, destination.y)
  if (!tile) { log(state, `${label(target)} is blocked by the boundary.`); return { kind, moved: false, from, blocked: 'bounds' } }
  if (blockingTile(tile.kind)) { log(state, `${label(target)} is blocked by ${tile.kind}.`); return { kind, moved: false, from, blocked: tile.kind } }
  const occupant = actorAt(state.floor, destination.x, destination.y)
  if ((occupant && occupant !== target) || (state.hero !== target && state.hero.x === destination.x && state.hero.y === destination.y)) {
    log(state, `${label(target)} is blocked by an occupant.`)
    return { kind, moved: false, from, blocked: 'occupant' }
  }
  target.x = destination.x
  target.y = destination.y
  const damage = hazardDamage(tile.kind)
  if (damage) target.health = Math.max(0, target.health - damage)
  log(state, `${label(target)} is ${kind === 'knockback' ? 'knocked back' : `${kind}ed`} to ${destination.x},${destination.y}.`)
  return { kind, moved: true, from, to: destination, ...(damage ? { hazard: tile.kind } : {}) }
}
