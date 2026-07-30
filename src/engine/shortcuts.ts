import { secretShortcutReport } from '../secrets'
import type { RunState, SecretRoute } from '../types'
import { generateAreaFloor, getTile, isPassable } from '../world'
import { applyAreaArcState, recordAreaArcPhase } from '../escalation'
import { recordGeneratedOptionalContent, recordOptionalContent } from '../telemetry'
import { advance } from './combat'
import { event, log, type ActionResult } from './shared'
import { refreshFov } from './visibility'
import { synchronizePartyActors } from './party'

const at = (left: { x: number; y: number }, right: { x: number; y: number }): boolean => left.x === right.x && left.y === right.y
const routeAtHero = (state: RunState): SecretRoute | undefined => (state.floor.secretRoutes ?? []).find(route => route.kind === 'rare-transition' && at(route.entry, state.hero) && isPassable(state.floor, route.entry.x, route.entry.y) && state.floor.secretRooms?.find(room => room.id === route.roomId)?.discovery)
const safeLanding = (state: RunState, route: SecretRoute): boolean => Boolean(route.destination && route.destination.biome === (state.area ?? state.floor.biome) && route.destination.floor === (state.areaFloor ?? state.floor.index % 4) + 2)

export const takeSecretShortcut = (state: RunState): ActionResult | undefined => {
  if (state.shortcutReturn && at(state.hero, state.floor.start)) {
    const returning = state.shortcutReturn
    const arrival = getTile(returning.floor, returning.arrival.x, returning.arrival.y)
    if (!arrival || !isPassable(returning.floor, returning.arrival.x, returning.arrival.y)) { recordOptionalContent(state, 'failed', `shortcut-return:${returning.routeId}:unsafe-landing`); log(state, 'The return link has no safe landing.'); return [] }
    state.floor = returning.floor
    state.areaFloor = returning.areaFloor
    state.hero.x = returning.arrival.x
    state.hero.y = returning.arrival.y
    delete state.shortcutReturn
    synchronizePartyActors(state, 'shortcut')
    refreshFov(state)
    recordOptionalContent(state, 'used', `shortcut-return:${returning.routeId}`)
    log(state, 'You follow the return link to the opened secret entry.')
    return advance(state, [event('floor')])
  }
  const route = routeAtHero(state)
  if (!route) return undefined
  if (!safeLanding(state, route) || !route.destination || route.arrival !== 'floor-start' || !route.direction || !route.returnSemantics) { recordOptionalContent(state, 'failed', `shortcut:${route.id}:invalid-landing`); log(state, 'This shortcut cannot leave its biome or bypass an unapproved floor.'); return [] }
  const areaArc = state.areaArc
  if (state.floor.escalation && areaArc) recordAreaArcPhase(areaArc, state.floor.escalation.phase)
  const routePosition = Math.max(0, (state.areaOrder ?? []).indexOf(route.destination.biome))
  const destination = generateAreaFloor(state.seed, route.destination.biome, route.destination.floor, routePosition)
  if (!isPassable(destination, destination.start.x, destination.start.y)) { recordOptionalContent(state, 'failed', `shortcut:${route.id}:unsafe-landing`); log(state, 'The shortcut landing is unsafe.'); return [] }
  if (route.direction === 'two-way') state.shortcutReturn = { version: 1, routeId: route.id, floor: structuredClone(state.floor), areaFloor: state.areaFloor ?? state.floor.index % 4, arrival: { ...route.entry } }
  state.floor = destination
  if (areaArc) applyAreaArcState(state.floor, areaArc)
  recordGeneratedOptionalContent(state)
  state.areaFloor = route.destination.floor
  state.hero.x = state.floor.start.x
  state.hero.y = state.floor.start.y
  synchronizePartyActors(state, 'shortcut')
  refreshFov(state)
  recordOptionalContent(state, 'used', `shortcut:${route.id}`)
  log(state, `Shortcut taken: ${secretShortcutReport(route)}.`)
  return advance(state, [event('floor')])
}
