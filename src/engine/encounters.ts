import { ITEM } from '../content'
import type { FloorEncounter, ItemId, RunState } from '../types'
import { recordTelemetryCount } from '../telemetry'
import { advance } from './combat'
import { grantGold } from './economy'
import { event, log, type ActionResult } from './shared'
import { refreshFov } from './visibility'
import { boonRank } from './buildcraft'
import { settleCurseAfterEncounter } from './curses'
import { addCondition } from './conditions'

export interface EncounterOption { label: string; detail: string; available: boolean }

const expansionEncounterKinds = ['sunTribute', 'mirageMarket', 'brineOath', 'glassTrial', 'whiteRoad', 'saltCache', 'iceDuel', 'winterTithe', 'rimeContract', 'frostCache', 'whiteout', 'reliquaryTrial'] as const
type ExpansionEncounterKind = typeof expansionEncounterKinds[number]
type ExpansionCost = 'health' | 'maxHealth' | 'focus' | 'item'
type ExpansionRisk = 'curse' | 'terrain' | 'map' | 'shield'
interface ExpansionProfile { title: string; cost: ExpansionCost; value: number; item?: ItemId; boon: string; reward?: ItemId; gold: number; risk: ExpansionRisk; terrain: 'brine' | 'frostRime' }
const expansionProfiles: Record<ExpansionEncounterKind, ExpansionProfile> = {
  sunTribute: { title: 'SUN TRIBUTE', cost: 'health', value: 3, boon: 'sunstep', reward: 'fireJar', gold: 100, risk: 'terrain', terrain: 'brine' },
  mirageMarket: { title: 'MIRAGE MARKET', cost: 'focus', value: 3, boon: 'mirageMap', reward: 'blink', gold: 75, risk: 'map', terrain: 'brine' },
  brineOath: { title: 'BRINE OATH', cost: 'maxHealth', value: 3, boon: 'brineWard', reward: 'focusTonic', gold: 120, risk: 'curse', terrain: 'brine' },
  glassTrial: { title: 'GLASS TRIAL', cost: 'item', value: 1, item: 'tonic', boon: 'mirrorHunt', reward: 'sight', gold: 95, risk: 'terrain', terrain: 'brine' },
  whiteRoad: { title: 'WHITE ROAD', cost: 'health', value: 4, boon: 'whiteRoad', reward: 'bridgeKit', gold: 115, risk: 'shield', terrain: 'brine' },
  saltCache: { title: 'SALT CACHE', cost: 'focus', value: 2, boon: 'saltLedger', reward: 'mapScroll', gold: 90, risk: 'curse', terrain: 'brine' },
  iceDuel: { title: 'ICE DUEL', cost: 'health', value: 4, boon: 'duelistOath', reward: 'ward', gold: 120, risk: 'shield', terrain: 'frostRime' },
  winterTithe: { title: 'WINTER TITHE', cost: 'maxHealth', value: 3, boon: 'winterRations', reward: 'mend', gold: 125, risk: 'curse', terrain: 'frostRime' },
  rimeContract: { title: 'RIME CONTRACT', cost: 'item', value: 1, item: 'focusTonic', boon: 'shatterMark', reward: 'grappleLine', gold: 105, risk: 'terrain', terrain: 'frostRime' },
  frostCache: { title: 'FROST CACHE', cost: 'focus', value: 3, boon: 'coldRead', reward: 'sight', gold: 90, risk: 'map', terrain: 'frostRime' },
  whiteout: { title: 'WHITEOUT', cost: 'health', value: 3, boon: 'thawStep', reward: 'tonic', gold: 110, risk: 'curse', terrain: 'frostRime' },
  reliquaryTrial: { title: 'RELIQUARY TRIAL', cost: 'focus', value: 3, boon: 'reliquaryEcho', reward: 'wardScript', gold: 110, risk: 'shield', terrain: 'frostRime' }
}
const expansionProfileFor = (kind: FloorEncounter['kind']): ExpansionProfile | undefined => expansionEncounterKinds.includes(kind as ExpansionEncounterKind) ? expansionProfiles[kind as ExpansionEncounterKind] : undefined

const traversalRewards = ['grappleLine', 'bridgeKit', 'steamJetpack', 'portableWinch'] as const
const encounterAtReach = (state: RunState): FloorEncounter | undefined => state.floor.encounters?.find(encounter => encounter.state === 'dormant' && Math.max(Math.abs(encounter.x - state.hero.x), Math.abs(encounter.y - state.hero.y)) <= 1)
const encounter = (state: RunState, id: string): FloorEncounter | undefined => state.floor.encounters?.find(current => current.id === id && current.state === 'dormant')
const rewardFor = (state: RunState, source: FloorEncounter): string => traversalRewards[(state.seed + state.floor.index + source.x + source.y) % traversalRewards.length]
const grantItem = (state: RunState, id: string): void => {
  if (state.hero.inventory.length < 12) state.hero.inventory.push(id)
  else state.floor.items.push({ id, x: state.hero.x, y: state.hero.y, count: 1, visibleInFog: true })
}
const grantContextGold = (state: RunState, amount: number): void => { grantGold(state, amount + (state.floor.difficulty?.threat ?? 0) * 5) }
const contextualCost = (state: RunState, amount: number): number => amount + (state.floor.difficulty?.threat ?? 0) * 3
const resolve = (state: RunState, source: FloorEncounter, outcome: string, events: ActionResult): ActionResult => {
  source.state = 'resolved'
  state.modal = undefined
  if (source.kind !== 'cursedObject') settleCurseAfterEncounter(state)
  const dividend = boonRank(state, 'echoDividend') * 10
  if (dividend) grantContextGold(state, dividend)
  const ward = boonRank(state, 'ossuaryWard')
  if (ward) state.hero.conditions = [...(state.hero.conditions ?? []), { kind: 'shielded', duration: 2, potency: ward }]
  const reliquaryEcho = boonRank(state, 'reliquaryEcho')
  if (reliquaryEcho) state.hero.conditions = [...(state.hero.conditions ?? []), { kind: 'shielded', duration: 2, potency: reliquaryEcho }]
  recordTelemetryCount(state, 'eventOutcomes', `${source.kind}:${outcome}`)
  return [event('encounter'), ...events]
}

export const encounterOptions = (state: RunState, source: FloorEncounter): EncounterOption[] => {
  const expansion = expansionProfileFor(source.kind)
  if (expansion) {
    const primary = expansion.cost === 'health' ? { label: 'BLEED FOR THE OFFER', detail: `Lose ${expansion.value} HP; gain ${expansion.gold} cash, ${expansion.boon}, and ${expansion.reward ? ITEM[expansion.reward].name : 'a reward'}.`, available: state.hero.health > expansion.value + 2 }
      : expansion.cost === 'maxHealth' ? { label: 'PAY VITALITY', detail: `Lose ${expansion.value} maximum HP; gain ${expansion.gold} cash and ${expansion.boon}.`, available: state.hero.maxHealth > expansion.value + 6 }
        : expansion.cost === 'focus' ? { label: 'SPEND FOCUS', detail: `Spend ${expansion.value} focus; gain ${expansion.gold} cash and ${expansion.boon}.`, available: state.hero.focus >= expansion.value }
          : { label: 'GIVE AN OFFERING', detail: `Consume ${ITEM[expansion.item!].name}; gain ${expansion.gold} cash and ${expansion.boon}.`, available: state.hero.inventory.includes(expansion.item!) }
    const risk = expansion.risk === 'curse' ? { label: 'TAKE THE CURSE', detail: 'Take a two-encounter damage curse for 150 cash.', available: !state.hero.curse }
      : expansion.risk === 'terrain' ? { label: 'CRACK THE GROUND', detail: `Gain 90 cash; nearby ground becomes dangerous ${expansion.terrain === 'brine' ? 'brine' : 'rime'}.`, available: true }
        : expansion.risk === 'map' ? { label: 'READ THE OMEN', detail: 'Reveal the floor and gain 2 focus.', available: true }
          : { label: 'TAKE THE WARD', detail: 'Gain a three-turn shield and 70 cash.', available: true }
    return [primary, risk, { label: 'LEAVE', detail: 'Leave the offer untouched.', available: true }]
  }
  if (source.kind === 'stormCache') return [
    { label: 'OPEN CACHE', detail: 'Gain climbing gear, 60 cash, and one traversal Boon rank.', available: true },
    { label: 'MAP WIND', detail: 'Reveal the floor and gain 2 focus.', available: true },
    { label: 'LEAVE', detail: 'Leave the storm cache sealed.', available: true }
  ]
  if (source.kind === 'windTrial') return [
    { label: 'RIDE THE GALE', detail: 'Lose 3 HP; gain 100 cash and one combat Boon rank.', available: state.hero.health > 5 },
    { label: 'BIND THE GALE', detail: 'Spend 3 focus; gain a grappling line.', available: state.hero.focus >= 3 },
    { label: 'LEAVE', detail: 'Keep your footing.', available: true }
  ]
  if (source.kind === 'ancestorDebt') return [
    { label: 'PAY VITALITY', detail: 'Lose 3 maximum HP; gain 90 cash and a spirit item.', available: state.hero.maxHealth > 8 },
    { label: 'PAY AN OATH', detail: 'No healing for two floors; gain 110 cash.', available: (state.hero.oaths?.length ?? 0) < 3 },
    { label: 'LEAVE', detail: 'Leave the debt unsettled.', available: true }
  ]
  if (source.kind === 'tombAuction') return [
    { label: 'BUY RELIC', detail: `Spend ${contextualCost(state, 55)} cash for a grave relic and 1 Boon rank.`, available: state.hero.gold >= contextualCost(state, 55) },
    { label: 'SELL BLOOD', detail: 'Lose 5 HP for 85 cash.', available: state.hero.health > 6 },
    { label: 'LEAVE', detail: 'Do not bid.', available: true }
  ]
  if (source.kind === 'oathwell') return [
    { label: 'SWEAR', detail: 'Take a two-floor no-charms oath for 90 cash and 1 Boon rank.', available: (state.hero.oaths?.length ?? 0) < 3 },
    { label: 'SCOUR WELL', detail: 'Spend 2 focus to remove nearby hazards.', available: state.hero.focus >= 2 },
    { label: 'LEAVE', detail: 'Leave the well untouched.', available: true }
  ]
  if (source.kind === 'cursedObject') return [
    { label: 'TAKE MIRROR', detail: 'Rare lethal: take no damage across the next 2 encounters; gain 180 cash.', available: !state.hero.curse },
    { label: 'TAKE FLEECE', detail: 'Severe: take no damage across the next 2 encounters; gain 110 cash.', available: !state.hero.curse },
    { label: 'TAKE IDOL', detail: 'Severe: take no damage across the next 2 encounters; gain 125 cash.', available: !state.hero.curse },
    { label: 'TAKE SHARD', detail: 'Severe: take no damage across the next 2 encounters; gain 125 cash.', available: !state.hero.curse },
    { label: 'LEAVE', detail: 'Leave the object alone.', available: true }
  ]
  if (source.kind === 'wayfarer') return [
    { label: 'TRADE', detail: `${contextualCost(state, 35)} cash for ${ITEM[rewardFor(state, source)].name}.`, available: state.hero.gold >= contextualCost(state, 35) },
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
  const expansion = expansionProfileFor(source.kind)
  log(state, expansion ? `${expansion.title} presents a dangerous offer.` : source.kind === 'wayfarer' ? 'A wandering wayfarer calls from the side trail.' : source.kind === 'bloodBargain' ? 'A sealed bargain waits for an answer.' : source.kind === 'cursedObject' ? 'A cursed object hums from the side trail.' : source.kind === 'stormCache' || source.kind === 'windTrial' ? 'The cliff wind presents a dangerous offer.' : source.kind === 'ancestorDebt' || source.kind === 'tombAuction' ? 'The dead offer a price.' : source.kind === 'oathwell' ? 'An oathwell asks for a binding.' : 'The chamber walls grind, awaiting a command.')
  return [event('encounter'), event('menu')]
}

const chamberCells = (state: RunState, source: FloorEncounter, radius: number) => state.floor.tiles.flatMap((tile, index) => {
  const x = index % 48
  const y = Math.floor(index / 48)
  return Math.max(Math.abs(x - source.x), Math.abs(y - source.y)) <= radius ? [{ tile, x, y }] : []
}).filter(cell => !(cell.x === state.hero.x && cell.y === state.hero.y) && !(cell.x === state.floor.start.x && cell.y === state.floor.start.y) && !(cell.x === state.floor.exit.x && cell.y === state.floor.exit.y))

const resolveExpansionEncounter = (state: RunState, source: FloorEncounter, index: number): ActionResult | undefined => {
  const profile = expansionProfileFor(source.kind)
  if (!profile) return undefined
  if (index === 0) {
    if (profile.cost === 'health') state.hero.health -= profile.value
    if (profile.cost === 'maxHealth') { state.hero.maxHealth -= profile.value; state.hero.health = Math.min(state.hero.health, state.hero.maxHealth) }
    if (profile.cost === 'focus') state.hero.focus -= profile.value
    if (profile.cost === 'item') state.hero.inventory.splice(state.hero.inventory.indexOf(profile.item!), 1)
    state.hero.boons ??= {}
    state.hero.boons[profile.boon] = (state.hero.boons[profile.boon] ?? 0) + 1
    grantContextGold(state, profile.gold)
    if (profile.reward) grantItem(state, profile.reward)
    log(state, `${profile.title} grants ${profile.boon}.`)
    return resolve(state, source, 'offer', advance(state, [event('pickup')]))
  }
  if (index === 1) {
    if (profile.risk === 'curse') {
      state.hero.curse = { itemId: 'cursedMirror', name: ITEM.cursedMirror.name, condition: 'Take no damage before resolving two encounters.', remainingEncounters: 2, lethal: false }
      grantItem(state, 'cursedMirror')
      grantContextGold(state, 150)
      return resolve(state, source, 'curse', [event('pickup')])
    }
    if (profile.risk === 'terrain') {
      const cells = chamberCells(state, source, 2).filter(cell => cell.tile.kind === 'floor').slice(0, 5)
      cells.forEach(cell => { cell.tile.kind = profile.terrain })
      grantContextGold(state, 90)
      refreshFov(state)
      return resolve(state, source, 'terrain', advance(state, [event('danger')]))
    }
    if (profile.risk === 'map') {
      state.floor.tiles.forEach(tile => { tile.explored = true })
      state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 2)
      refreshFov(state)
      return resolve(state, source, 'map', [event('menu')])
    }
    addCondition(state.hero, { kind: 'shielded', duration: 3, potency: 2 })
    grantContextGold(state, 70)
    return resolve(state, source, 'ward', [event('spell')])
  }
  return resolve(state, source, 'leave', [event('menu')])
}

export const chooseEncounter = (state: RunState, encounterId: string, command: string): ActionResult => {
  const source = encounter(state, encounterId)
  if (!source) return []
  const index = Number(command) - 1
  const option = encounterOptions(state, source)[index]
  if (!option) return []
  if (!option.available) { log(state, 'You cannot meet that cost.'); return [event('menu')] }
  const expansion = resolveExpansionEncounter(state, source, index)
  if (expansion) return expansion
  if (source.kind === 'stormCache') {
    if (index === 0) { grantContextGold(state, 60); grantItem(state, 'cliffSpool'); state.hero.boons ??= {}; state.hero.boons.galeThread = (state.hero.boons.galeThread ?? 0) + 1; return resolve(state, source, 'open', advance(state, [event('pickup')])) }
    if (index === 1) { state.floor.tiles.forEach(tile => { tile.explored = true }); state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 2); refreshFov(state); return resolve(state, source, 'map', [event('menu')]) }
    return resolve(state, source, 'leave', [event('menu')])
  }
  if (source.kind === 'windTrial') {
    if (index === 0) { state.hero.health -= 3; grantContextGold(state, 100); state.hero.boons ??= {}; state.hero.boons.skyhookReprisal = (state.hero.boons.skyhookReprisal ?? 0) + 1; return resolve(state, source, 'ride', advance(state, [event('hurt')])) }
    if (index === 1) { state.hero.focus -= 3; grantItem(state, 'grappleLine'); return resolve(state, source, 'bind', advance(state, [event('pickup')])) }
    return resolve(state, source, 'leave', [event('menu')])
  }
  if (source.kind === 'ancestorDebt') {
    if (index === 0) { state.hero.maxHealth -= 3; state.hero.health = Math.min(state.hero.health, state.hero.maxHealth); grantContextGold(state, 90); grantItem(state, 'ancestorToken'); return resolve(state, source, 'vitality', advance(state, [event('pickup')])) }
    if (index === 1) { state.hero.oaths = [...(state.hero.oaths ?? []), { id: 'noHealing', remainingFloors: 2 }]; grantContextGold(state, 110); return resolve(state, source, 'oath', [event('encounter')]) }
    return resolve(state, source, 'leave', [event('menu')])
  }
  if (source.kind === 'tombAuction') {
    if (index === 0) { state.hero.gold -= contextualCost(state, 55); grantItem(state, 'mourningBell'); state.hero.boons ??= {}; state.hero.boons.graveLedger = (state.hero.boons.graveLedger ?? 0) + 1; return resolve(state, source, 'buy', advance(state, [event('pickup')])) }
    if (index === 1) { state.hero.health -= 5; grantContextGold(state, 85); return resolve(state, source, 'blood', advance(state, [event('hurt')])) }
    return resolve(state, source, 'leave', [event('menu')])
  }
  if (source.kind === 'oathwell') {
    if (index === 0) { state.hero.oaths = [...(state.hero.oaths ?? []), { id: 'noCharms', remainingFloors: 2 }]; grantContextGold(state, 90); state.hero.boons ??= {}; state.hero.boons.bridgeOfNames = (state.hero.boons.bridgeOfNames ?? 0) + 1; return resolve(state, source, 'swear', [event('encounter')]) }
    if (index === 1) { state.hero.focus -= 2; const cells = chamberCells(state, source, 2).filter(cell => ['rubble', 'bramble', 'boulder', 'pit', 'water', 'deepWater', 'current', 'gas', 'smoke', 'fireVent'].includes(cell.tile.kind)); cells.forEach(cell => { cell.tile.kind = 'floor' }); return resolve(state, source, 'scour', advance(state, [event('spell')])) }
    return resolve(state, source, 'leave', [event('menu')])
  }
  if (source.kind === 'cursedObject') {
    if (index < 4) {
      const lethal = index === 0
      const itemId = ['cursedMirror', 'graveFleece', 'stormIdol', 'oathShard'][index] as 'cursedMirror' | 'graveFleece' | 'stormIdol' | 'oathShard'
      state.hero.curse = { itemId, name: ITEM[itemId].name, condition: 'Take no damage before resolving two encounters.', remainingEncounters: 2, lethal }
      grantItem(state, itemId)
      grantContextGold(state, lethal ? 180 : itemId === 'graveFleece' ? 110 : 125)
      return resolve(state, source, itemId, [event('pickup')])
    }
    return resolve(state, source, 'leave', [event('menu')])
  }
  if (source.kind === 'wayfarer') {
    if (index === 0) { state.hero.gold -= contextualCost(state, 35); const reward = rewardFor(state, source); grantItem(state, reward); log(state, `The wayfarer trades ${ITEM[reward].name} for your cash.`); return resolve(state, source, 'trade', advance(state, [event('pickup')])) }
    if (index === 1) { state.floor.tiles.forEach(tile => { tile.explored = true }); refreshFov(state); log(state, 'The wayfarer maps the side routes.'); return resolve(state, source, 'route', [event('menu')]) }
    log(state, 'The wayfarer fades back into the side trail.')
    return resolve(state, source, 'leave', [event('menu')])
  }
  if (source.kind === 'bloodBargain') {
    if (index === 0) { state.hero.maxHealth -= 4; state.hero.health = Math.min(state.hero.health, state.hero.maxHealth); grantContextGold(state, 75); const reward = rewardFor(state, source); grantItem(state, reward); log(state, `The bargain takes vitality and leaves ${ITEM[reward].name}.`); return resolve(state, source, 'vitality', advance(state, [event('pickup')])) }
    if (index === 1) { state.hero.focus -= 3; grantContextGold(state, 30); state.floor.tiles.forEach(tile => { tile.explored = true }); refreshFov(state); log(state, 'The bargain drinks focus and exposes the trail.'); return resolve(state, source, 'focus', advance(state, [event('spell')])) }
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
    grantContextGold(state, 90)
    refreshFov(state)
    log(state, `The chamber surges through ${cells.length} nearby cells.`)
    return resolve(state, source, 'surge', advance(state, [event('spell')]))
  }
  log(state, 'The chamber settles without changing its shape.')
  return resolve(state, source, 'leave', [event('menu')])
}
