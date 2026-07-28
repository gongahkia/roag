import { floorIndex, inFloorBounds, type Biome, type EcologyEvent, type EcologyEventKind, type Floor, type Point, type RunState, type Tile, type TileKind } from './types'
import { addCondition } from './engine/conditions'
import { event, log, type ActionResult } from './engine/shared'

export interface EcologyProfile { kind: EcologyEventKind; effect: TileKind; terrain: TileKind[]; warning: string; responses: string[]; cleanup: string }

const profiles: Record<Biome, EcologyProfile> = {
  mine: { kind: 'collapse', effect: 'crumble', terrain: ['crumble'], warning: 'The support beams groan; leave the marked shelf.', responses: ['step off unstable ground', 'draw pursuers through the collapse'], cleanup: 'The dust settles and the shelf holds.' },
  wilds: { kind: 'nesting', effect: 'web', terrain: ['web', 'water'], warning: 'Nest calls gather through the brush.', responses: ['leave the nesting ground', 'clear the webbed approach'], cleanup: 'The nesting calls fade.' },
  caverns: { kind: 'tide', effect: 'current', terrain: ['water', 'current'], warning: 'The tide marks the low shelves; move before the flood current turns.', responses: ['take the dry shelf', 'brace at an anchor'], cleanup: 'The ebb leaves the shelf clear.' },
  ruins: { kind: 'visibility', effect: 'darkness', terrain: ['darkness', 'dart'], warning: 'Ward lamps dim across the ritual passage.', responses: ['keep to the lit processional route', 'break the ward sightline'], cleanup: 'The ritual lamps brighten.' },
  furnace: { kind: 'smoke', effect: 'smoke', terrain: ['smoke', 'lift'], warning: 'The kiln stack opens over the firing lane.', responses: ['take the raised lift lane', 'quench the marked smoke stack'], cleanup: 'The stack cools and the smoke thins.' },
  floodedRuins: { kind: 'tide', effect: 'current', terrain: ['current', 'water'], warning: 'Water draws toward a sudden tide.', responses: ['brace at an anchor', 'take the higher route'], cleanup: 'The tide slackens.' },
  cliffs: { kind: 'wind', effect: 'ledge', terrain: ['ledge', 'rope'], warning: 'A crosswind gathers over the shelf.', responses: ['hold the anchor route', 'wait for the gust to pass'], cleanup: 'The wind drops.' },
  burial: { kind: 'migration', effect: 'spiritPath', terrain: ['spiritPath', 'graveSoil'], warning: 'Ancestor lights drift across the graves.', responses: ['leave the procession path', 'follow the lit route'], cleanup: 'The procession passes on.' },
  saltFlats: { kind: 'visibility', effect: 'darkness', terrain: ['saltMirror', 'brine'], warning: 'A salt haze swallows the far route.', responses: ['navigate by the mirrors', 'wait out the haze'], cleanup: 'The salt haze clears.' },
  frostReliquary: { kind: 'nesting', effect: 'ice', terrain: ['ice', 'frostRime'], warning: 'Rime shifts around a fresh nest.', responses: ['leave the rime', 'break the ice line'], cleanup: 'The rime settles.' }
}

const tileAt = (floor: Floor, point: Point): Tile | undefined => inFloorBounds(floor, point.x, point.y) ? floor.tiles[floorIndex(floor, point.x, point.y)] : undefined

export const ecologyProfileFor = (biome: Biome): EcologyProfile => profiles[biome]

export const ecologyEventFor = (floor: Floor, target: Point, source: string, node?: string, route?: string): EcologyEvent => {
  const profile = ecologyProfileFor(floor.biome)
  const tile = tileAt(floor, target)
  if (!tile) throw new Error(`ecology target is outside floor: ${target.x},${target.y}`)
  const areaFloor = floor.index % 4
  return {
    id: `ecology:${floor.index}:${profile.kind}:${target.x}:${target.y}`,
    kind: profile.kind,
    source,
    target: { ...target },
    ...(route ? { route } : {}),
    ...(node ? { node } : {}),
    warning: profile.warning,
    startsAt: 2 + areaFloor + floor.seed % 3,
    duration: 2 + areaFloor,
    responses: [...profile.responses],
    cleanup: profile.cleanup,
    state: 'waiting',
    original: tile.kind,
    ...(tile.flow ? { originalFlow: { ...tile.flow } } : {}),
    effect: profile.effect
  }
}

const restore = (floor: Floor, ecology: EcologyEvent): void => {
  const tile = tileAt(floor, ecology.target)
  if (!tile || tile.kind !== ecology.effect) return
  tile.kind = ecology.original
  if (ecology.originalFlow) tile.flow = { ...ecology.originalFlow }
  else delete tile.flow
}

const activate = (floor: Floor, ecology: EcologyEvent): boolean => {
  const tile = tileAt(floor, ecology.target)
  if (!tile) return false
  tile.kind = ecology.effect
  if (ecology.effectFlow) tile.flow = { ...ecology.effectFlow }
  else delete tile.flow
  return true
}

const react = (state: RunState, ecology: EcologyEvent): void => {
  if (ecology.kind !== 'collapse') return
  const source = state.floor.actors.find(actor => actor.id === ecology.source && actor.hostile && actor.health > 0)
  if (!source || (source.combatRole !== 'guard' && source.combatRole !== 'pursuer')) return
  addCondition(source, { kind: 'shielded', duration: ecology.duration, potency: 1 })
  log(state, `${source.name} braces behind the collapse.`)
}

export const advanceEcology = (state: RunState, events: ActionResult): void => {
  for (const ecology of state.floor.ecology ?? []) {
    if (ecology.state === 'waiting' && !ecology.warned && state.turn >= ecology.startsAt - 1) {
      ecology.warned = true
      log(state, `WARNING: ${ecology.warning}`)
    }
    if (ecology.state === 'waiting' && state.turn >= ecology.startsAt) {
      if (!activate(state.floor, ecology)) { ecology.state = 'resolved'; continue }
      ecology.state = 'active'
      react(state, ecology)
      log(state, `${ecology.kind} alters the route. ${ecology.responses[0]}.`)
      events.push(event('danger'))
    }
    if (ecology.state === 'active' && state.turn >= ecology.startsAt + ecology.duration) {
      restore(state.floor, ecology)
      ecology.state = 'resolved'
      log(state, ecology.cleanup)
    }
  }
}

export const ecologyReadout = (ecology: EcologyEvent, turn: number): string => ecology.state === 'waiting'
  ? `${ecology.kind.toUpperCase()} T-${Math.max(0, ecology.startsAt - turn)}: ${ecology.warning}`
  : ecology.state === 'active' ? `${ecology.kind.toUpperCase()} ACTIVE: ${ecology.responses[0]}` : `${ecology.kind.toUpperCase()} CLEARED`
