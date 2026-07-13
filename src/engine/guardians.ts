import type { Actor, GuardianPhase, RunState, TileKind } from '../types'
import { rngFor } from '../rng'
import { actorAt, getTile } from '../world'
import { log } from './shared'

export type ArenaPhase = 'stable' | 'hazard' | 'collapse'
export interface GuardianTransition { from: GuardianPhase; to: GuardianPhase; arena: ArenaPhase; tile: TileKind }

export const guardianPhaseFor = (guardian: Actor): GuardianPhase => guardian.health * 3 <= guardian.maxHealth ? 'cataclysm' : guardian.health * 3 <= guardian.maxHealth * 2 ? 'pressure' : 'opening'
export const arenaPhaseFor = (phase: GuardianPhase): ArenaPhase => phase === 'opening' ? 'stable' : phase === 'pressure' ? 'hazard' : 'collapse'

export const advanceGuardianPhase = (state: RunState, guardian: Actor): GuardianTransition | undefined => {
  if (guardian.role !== 'guardian') return undefined
  const next = guardianPhaseFor(guardian)
  const previous = guardian.guardianPhase ?? 'opening'
  guardian.guardianPhase = next
  if (previous === next) return undefined
  const tile: TileKind = guardian.kind === 'foreman' ? next === 'pressure' ? 'rail' : 'crumble' : guardian.kind === 'heartwood' ? next === 'pressure' ? 'bramble' : 'water' : guardian.kind === 'geode' ? next === 'pressure' ? 'gas' : 'lava' : next === 'pressure' ? 'gas' : 'fireVent'
  const guardianTile = getTile(state.floor, guardian.x, guardian.y)
  const candidates = guardian.kind === 'foreman' && next === 'pressure' && (guardianTile?.kind === 'floor' || guardianTile?.kind === 'rail')
    ? [{ x: guardian.x, y: guardian.y }]
    : state.floor.tiles.flatMap((current, index) => current.kind === 'floor' ? [{ x: index % 48, y: Math.floor(index / 48) }] : []).filter(point => !(point.x === state.hero.x && point.y === state.hero.y) && !actorAt(state.floor, point.x, point.y))
  if (candidates.length) {
    const point = rngFor(state.seed, 'combat', state.floor.index, state.turn, `arena:${guardian.id}:${next}`).pick(candidates)
    getTile(state.floor, point.x, point.y)!.kind = tile
  }
  const arena = arenaPhaseFor(next)
  log(state, `${guardian.name} enters ${next}; arena ${arena}.`)
  return { from: previous, to: next, arena, tile }
}
