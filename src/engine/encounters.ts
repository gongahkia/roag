import { ITEM } from '../content'
import type { FloorEncounter, RunState } from '../types'
import { recordTelemetryCount } from '../telemetry'
import { getTile } from '../world'
import { advance } from './combat'
import { grantGold } from './economy'
import { event, log, type ActionResult } from './shared'
import { refreshFov } from './visibility'

export interface EncounterOption { label: string; detail: string; available: boolean }

const traversalRewards = ['grappleLine', 'bridgeKit', 'steamJetpack', 'portableWinch'] as const
const encounterAtReach = (state: RunState): FloorEncounter | undefined => state.floor.encounters?.find(encounter => encounter.state === 'dormant' && Math.max(Math.abs(encounter.x - state.hero.x), Math.abs(encounter.y - state.hero.y)) <= 1)
const encounter = (state: RunState, id: string): FloorEncounter | undefined => state.floor.encounters?.find(current => current.id === id && current.state === 'dormant')
const rewardFor = (state: RunState, source: FloorEncounter): string => traversalRewards[(state.seed + state.floor.index + source.x + source.y) % traversalRewards.length]
const grantItem = (state: RunState, id: string): void => {
  if (state.hero.inventory.length < 12) state.hero.inventory.push(id)
  else state.floor.items.push({ id, x: state.hero.x, y: state.hero.y, count: 1, visibleInFog: true })
}
const resolve = (state: RunState, source: FloorEncounter, outcome: string, events: ActionResult): ActionResult => {
  source.state = 'resolved'
  state.modal = undefined
  recordTelemetryCount(state, 'eventOutcomes', `${source.kind}:${outcome}`)
  return events
}

export const encounterOptions = (state: RunState, source: FloorEncounter): EncounterOption[] => {
  if (source.kind === 'wayfarer') return [
    { label: 'TRADE', detail: `35 cash for ${ITEM[rewardFor(state, source)].name}.`, available: state.hero.gold >= 35 },
    { label: 'ASK ROUTE', detail: 'Reveal the remaining floor; no payment.', available: true },
    { label: 'LEAVE', detail: 'Keep moving.', available: true }
  ]
  if (source.kind === 'bloodBargain') return [
    { label: 'GIVE VITALITY', detail: 'Lose 4 max HP for 75 cash and traversal gear.', available: state.hero.maxHealth > 8 },
    { label: 'GIVE FOCUS', detail: 'Lose 3 focus for 30 cash and a revealed floor.', available: state.hero.focus >= 3 },
    { label: 'DECLINE', detail: 'Leave the bargain untouched.', available: true }
  ]
  return [
    { label: 'OPEN CHAMBER', detail: 'Spend 2 focus to flatten nearby hazards and blockers.', available: state.hero.focus >= 2 },
    { label: 'SURGE CHAMBER', detail: 'Gain 90 cash; nearby floor becomes current.', available: true },
    { label: 'LEAVE', detail: 'Do not disturb the chamber.', available: true }
  ]
}

export const openEncounter = (state: RunState): ActionResult | undefined => {
  const source = encounterAtReach(state)
  if (!source) return undefined
  state.modal = { kind: 'encounter', encounterId: source.id }
  log(state, source.kind === 'wayfarer' ? 'A wandering wayfarer calls from the side trail.' : source.kind === 'bloodBargain' ? 'A sealed bargain waits for an answer.' : 'The chamber walls grind, awaiting a command.')
  return [event('menu')]
}

const chamberCells = (state: RunState, source: FloorEncounter, radius: number) => state.floor.tiles.flatMap((tile, index) => {
  const x = index % 48
  const y = Math.floor(index / 48)
  return Math.max(Math.abs(x - source.x), Math.abs(y - source.y)) <= radius ? [{ tile, x, y }] : []
}).filter(cell => !(cell.x === state.hero.x && cell.y === state.hero.y) && !(cell.x === state.floor.start.x && cell.y === state.floor.start.y) && !(cell.x === state.floor.exit.x && cell.y === state.floor.exit.y))

export const chooseEncounter = (state: RunState, encounterId: string, command: string): ActionResult => {
  const source = encounter(state, encounterId)
  if (!source) return []
  const index = Number(command) - 1
  const option = encounterOptions(state, source)[index]
  if (!option) return []
  if (!option.available) { log(state, 'You cannot meet that cost.'); return [event('menu')] }
  if (source.kind === 'wayfarer') {
    if (index === 0) { state.hero.gold -= 35; const reward = rewardFor(state, source); grantItem(state, reward); log(state, `The wayfarer trades ${ITEM[reward].name} for your cash.`); return resolve(state, source, 'trade', advance(state, [event('pickup')])) }
    if (index === 1) { state.floor.tiles.forEach(tile => { tile.explored = true }); refreshFov(state); log(state, 'The wayfarer maps the side routes.'); return resolve(state, source, 'route', [event('menu')]) }
    log(state, 'The wayfarer fades back into the side trail.')
    return resolve(state, source, 'leave', [event('menu')])
  }
  if (source.kind === 'bloodBargain') {
    if (index === 0) { state.hero.maxHealth -= 4; state.hero.health = Math.min(state.hero.health, state.hero.maxHealth); grantGold(state, 75); const reward = rewardFor(state, source); grantItem(state, reward); log(state, `The bargain takes vitality and leaves ${ITEM[reward].name}.`); return resolve(state, source, 'vitality', advance(state, [event('pickup')])) }
    if (index === 1) { state.hero.focus -= 3; grantGold(state, 30); state.floor.tiles.forEach(tile => { tile.explored = true }); refreshFov(state); log(state, 'The bargain drinks focus and exposes the trail.'); return resolve(state, source, 'focus', advance(state, [event('spell')])) }
    log(state, 'The sealed bargain remains unopened.')
    return resolve(state, source, 'decline', [event('menu')])
  }
  if (index === 0) {
    state.hero.focus -= 2
    const mutable = new Set(['rubble', 'bramble', 'boulder', 'breakwall', 'pit', 'water', 'deepWater', 'current', 'gas', 'smoke', 'fireVent'])
    const cells = chamberCells(state, source, 2).filter(cell => mutable.has(cell.tile.kind))
    cells.forEach(cell => { cell.tile.kind = 'floor' })
    refreshFov(state)
    log(state, `The chamber opens ${cells.length} nearby cells.`)
    return resolve(state, source, 'open', advance(state, [event('spell')]))
  }
  if (index === 1) {
    const cells = chamberCells(state, source, 2).filter(cell => cell.tile.kind === 'floor').slice(0, 5)
    cells.forEach(cell => { cell.tile.kind = 'current' })
    grantGold(state, 90)
    refreshFov(state)
    log(state, `The chamber surges through ${cells.length} nearby cells.`)
    return resolve(state, source, 'surge', advance(state, [event('spell')]))
  }
  log(state, 'The chamber settles without changing its shape.')
  return resolve(state, source, 'leave', [event('menu')])
}
