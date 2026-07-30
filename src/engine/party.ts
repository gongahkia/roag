import type { Actor, Companion, CompanionRole, Point, RunState } from '../types'
import { getTile, isPassable } from '../world'
import { log } from './shared'

export type PartyPlacementReason = 'spawn' | 'floorTransition' | 'shortcut' | 'summon' | 'activation' | 'removal'
export interface PartyPlacementResult { reason: PartyPlacementReason; placed: boolean; companionIds: string[]; positions: Point[]; message?: string }

const active = (companion: Companion): boolean => companion.rosterStatus === 'active' && companion.injury === 'healthy' && !companion.permanentlyLost
export const activeCompanionRoster = (companions: readonly Companion[]): Companion[] => companions.filter(active).map(companion => structuredClone(companion)).sort((left, right) => left.id.localeCompare(right.id))
export const companionActorId = (companionId: string): string => `party:${companionId}`
export const isCompanionActor = (actor: Actor): boolean => actor.role === 'ally' && actor.status?.some(status => status.startsWith('companion:')) === true
const companionAppearances: Record<CompanionRole, { glyph: string; color: string }> = {
  guard: { glyph: 'G', color: '#e9c965' },
  scout: { glyph: 'S', color: '#8fd39b' },
  pathmaker: { glyph: 'P', color: '#8fb8ed' },
  ritualist: { glyph: 'R', color: '#d2a4e8' }
}
export const companionAppearance = (role: CompanionRole): { glyph: string; color: string } => companionAppearances[role]
const actorForCompanion = (companion: Companion, position: Point): Actor => ({
  id: companionActorId(companion.id),
  role: 'ally',
  kind: 'ally',
  name: companion.name,
  x: position.x,
  y: position.y,
  health: 12,
  maxHealth: 12,
  attack: 0,
  defense: 0,
  speed: 0,
  energy: 0,
  ...companionAppearance(companion.role),
  hostile: false,
  status: [`companion:${companion.id}`, `role:${companion.role}`]
})
const samePoint = (left: Point, right: Point): boolean => left.x === right.x && left.y === right.y
const candidatePositions = (state: RunState): Point[] => {
  const points: Point[] = []
  const radiusLimit = Math.max(state.floor.width, state.floor.height)
  for (let radius = 2; radius <= radiusLimit; radius++) {
    for (let y = state.hero.y - radius; y <= state.hero.y + radius; y++) for (let x = state.hero.x - radius; x <= state.hero.x + radius; x++) {
      if (Math.max(Math.abs(x - state.hero.x), Math.abs(y - state.hero.y)) === radius) points.push({ x, y })
    }
  }
  return points
}
const legalPosition = (state: RunState, point: Point): boolean => !samePoint(point, state.hero) && !samePoint(point, state.floor.exit) && Boolean(getTile(state.floor, point.x, point.y)) && isPassable(state.floor, point.x, point.y)

export const synchronizePartyActors = (state: RunState, reason: PartyPlacementReason): PartyPlacementResult => {
  const roster = activeCompanionRoster(state.companions ?? [])
  state.floor.actors = state.floor.actors.filter(actor => !isCompanionActor(actor))
  if (!roster.length) return { reason, placed: true, companionIds: [], positions: [] }
  const positions: Point[] = []
  for (const point of candidatePositions(state)) {
    if (!legalPosition(state, point)) continue
    positions.push(point)
    state.floor.actors.push({ id: `reservation:${positions.length}`, role: 'ally', kind: 'ally', name: 'formation reservation', x: point.x, y: point.y, health: 1, maxHealth: 1, attack: 0, defense: 0, speed: 0, energy: 0, glyph: '_', color: '#000', hostile: false, status: ['party-reservation'] })
    if (positions.length === roster.length) break
  }
  state.floor.actors = state.floor.actors.filter(actor => !actor.status?.includes('party-reservation'))
  if (positions.length !== roster.length) {
    const message = `Companion formation failed: no legal tile for ${roster[positions.length]?.name ?? 'the party'}.`
    log(state, message)
    return { reason, placed: false, companionIds: roster.map(companion => companion.id), positions: [], message }
  }
  state.floor.actors.push(...roster.map((companion, index) => actorForCompanion(companion, positions[index]!)))
  log(state, `${roster.map(companion => companion.name).join(', ')} join${roster.length === 1 ? 's' : ''} the party formation.`)
  return { reason, placed: true, companionIds: roster.map(companion => companion.id), positions }
}
