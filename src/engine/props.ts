import { DIRECTIONS, type Point, type Prop, type PropEffectKind, type RunState } from '../types'
import { isBlockingProp, propAt, propDefinition } from '../props'
import { actorAt, getTile } from '../world'
import { damageHero, explode, resolveDefeatedActors } from './combat'
import { modifyIncomingDamage } from './conditions'
import { event, log, type ActionResult } from './shared'

const pointKey = (point: Point): string => `${point.x},${point.y}`
const mineProp = (prop: Prop): boolean => prop.biome === 'mine' && prop.kind.startsWith('mine.')
const cardinal = (point: Point): boolean => Math.abs(point.x) + Math.abs(point.y) === 1
const hazardKinds = new Set(['spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'gas', 'lava', 'pit'])

const reward = (state: RunState, prop: Prop, id: string, count = 1): void => {
  state.floor.items.push({ id, x: prop.x, y: prop.y, count, visibleInFog: true })
}

const nearbyProp = (state: RunState): Prop | undefined => {
  const points = [{ x: state.hero.x, y: state.hero.y }, ...Object.values(DIRECTIONS).filter(delta => delta.x || delta.y).map(delta => ({ x: state.hero.x + delta.x, y: state.hero.y + delta.y }))]
  return points.map(point => propAt(state.floor.props, point.x, point.y)).find((prop): prop is Prop => Boolean(prop))
}

const inspect = (state: RunState, prop: Prop, followup: string): PropOperation => {
  prop.state = 'inspected'
  const definition = propDefinition(prop.kind)
  log(state, `You examine the ${definition.name}: ${definition.description} ${followup}`)
  return { kind: 'examined', events: [] }
}

const cartCandidates = (state: RunState, cart: Prop, previous: Point | undefined): Point[] => {
  const choices = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }]
    .map(delta => ({ x: cart.x + delta.x, y: cart.y + delta.y }))
    .filter(point => getTile(state.floor, point.x, point.y)?.kind === 'rail')
    .filter(point => !previous || point.x !== previous.x || point.y !== previous.y)
    .filter(point => !isBlockingProp(propAt(state.floor.props, point.x, point.y)))
  return choices
}

const triggerCartHazards = (state: RunState, cart: Prop): ActionResult => {
  const events: ActionResult = []
  for (const delta of Object.values(DIRECTIONS)) {
    const tile = getTile(state.floor, cart.x + delta.x, cart.y + delta.y)
    if (tile?.kind !== 'dart' && tile?.kind !== 'fireVent') continue
    const hazard = tile.kind
    tile.kind = 'floor'
    log(state, `The cart triggers the ${hazard === 'dart' ? 'dart' : 'fire'} trap.`)
    events.push(event('danger'))
  }
  return events
}

const moveCart = (state: RunState, cart: Prop, first: Point): ActionResult => {
  const events: ActionResult = [event('move')]
  if (!cardinal(first) || getTile(state.floor, cart.x + first.x, cart.y + first.y)?.kind !== 'rail') { log(state, 'The cart has no rail in that direction.'); return [] }
  let direction = { ...first }
  let previous = { x: cart.x, y: cart.y }
  while (true) {
    const next = { x: cart.x + direction.x, y: cart.y + direction.y }
    if (getTile(state.floor, next.x, next.y)?.kind !== 'rail' || isBlockingProp(propAt(state.floor.props, next.x, next.y))) break
    const actor = actorAt(state.floor, next.x, next.y)
    if (actor?.hostile) {
      actor.health -= modifyIncomingDamage(actor, 8)
      log(state, `The cart crushes ${actor.name}.`)
      events.push(event('hit'))
      break
    }
    if (state.hero.x === next.x && state.hero.y === next.y) {
      events.push(...damageHero(state, 8, 'The rail cart', true))
      break
    }
    cart.x = next.x
    cart.y = next.y
    events.push(...triggerCartHazards(state, cart))
    const options = cartCandidates(state, cart, previous)
    const straight = options.find(point => point.x - cart.x === direction.x && point.y - cart.y === direction.y)
    if (straight) {
      previous = { x: cart.x, y: cart.y }
      continue
    }
    if (options.length !== 1) break
    direction = { x: options[0].x - cart.x, y: options[0].y - cart.y }
    previous = { x: cart.x, y: cart.y }
  }
  resolveDefeatedActors(state)
  log(state, 'The cart grinds to a halt.')
  return events
}

const revealWarnings = (state: RunState, prop: Prop, skull: boolean): number => {
  const points = state.floor.tiles.flatMap((tile, index) => hazardKinds.has(tile.kind) ? [{ x: index % 48, y: Math.floor(index / 48), priority: 0 }] : [])
  if (skull) points.push(...state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => ({ x: actor.x, y: actor.y, priority: 1 })))
  const warnings = points
    .filter(point => Math.max(Math.abs(point.x - prop.x), Math.abs(point.y - prop.y)) <= 5)
    .sort((first, second) => first.priority - second.priority || Math.max(Math.abs(first.x - prop.x), Math.abs(first.y - prop.y)) - Math.max(Math.abs(second.x - prop.x), Math.abs(second.y - prop.y)) || first.y - second.y || first.x - second.x)
    .slice(0, 3)
  for (const point of warnings) getTile(state.floor, point.x, point.y)!.explored = true
  return warnings.length
}

const operateMineProp = (state: RunState, prop: Prop): PropOperation | undefined => {
  if (prop.kind === 'mine.oreVein') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Equip a pickaxe, then press C to mine it.')
    if (prop.state !== 'inspected') { log(state, 'The vein has already been worked.'); return { kind: 'examined', events: [] } }
    if (state.hero.equipment.mainHand !== 'pickaxe') { log(state, 'A pickaxe is required to work this vein.'); return { kind: 'examined', events: [] } }
    prop.state = 'destroyed'
    const tile = getTile(state.floor, prop.x, prop.y)
    if (tile) tile.kind = 'rubble'
    reward(state, prop, 'rock', 2)
    log(state, 'You chip ore free and leave a mound of rubble.')
    return { kind: 'activated', events: [event('pickup'), event('boom')] }
  }
  if (prop.kind === 'mine.lanternPost') {
    if (prop.state === 'dormant') return inspect(state, prop, 'An Ember Charm or Fire Jar can relight it.')
    log(state, prop.state === 'activated' ? 'The lantern post burns with a steady local glow.' : 'The lantern needs flame, not a hand.')
    return { kind: 'examined', events: [] }
  }
  if (prop.kind === 'mine.brokenCart') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Stand beside it on a rail, then press C to push it.')
    const direction = { x: Math.sign(prop.x - state.hero.x), y: Math.sign(prop.y - state.hero.y) }
    if (!cardinal(direction)) { log(state, 'Stand on a cardinal side of the cart to push it.'); return { kind: 'examined', events: [] } }
    const events = moveCart(state, prop, direction)
    if (!events.length) return { kind: 'examined', events }
    prop.state = 'activated'
    return { kind: 'moved', events }
  }
  if (prop.kind === 'mine.warningMarker' || prop.kind === 'mine.skullMarker') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to trace its local warning.')
    if (prop.state === 'activated') { log(state, 'The warning has already been traced.'); return { kind: 'examined', events: [] } }
    prop.state = 'activated'
    const count = revealWarnings(state, prop, prop.kind === 'mine.skullMarker')
    log(state, count ? `The marker exposes ${count} nearby danger${count === 1 ? '' : 's'}.` : 'The marker points to no nearby danger.')
    return { kind: 'activated', events: [event('danger')] }
  }
  if (prop.kind === 'mine.discardedParcel') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to recover it, or leave it untouched.')
    if (prop.state === 'activated') { log(state, 'The parcel has already been recovered.'); return { kind: 'examined', events: [] } }
    prop.state = 'activated'
    reward(state, prop, propDefinition(prop.kind).activationReward)
    log(state, 'You recover the parcel before its charge can flare.')
    return { kind: 'activated', events: [event('pickup')] }
  }
  return undefined
}

export interface PropOperation { kind: 'examined' | 'activated' | 'moved'; events: ActionResult }

export const operateProp = (state: RunState): PropOperation | undefined => {
  const prop = nearbyProp(state)
  if (!prop || !prop.hooks?.includes('operate')) return undefined
  if (mineProp(prop)) return operateMineProp(state, prop)
  const definition = propDefinition(prop.kind)
  if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to activate it.')
  if (prop.state === 'activated') {
    log(state, `The ${definition.name} has already been activated.`)
    return { kind: 'examined', events: [] }
  }
  if (prop.state !== 'inspected') return undefined
  prop.state = 'activated'
  reward(state, prop, definition.activationReward)
  log(state, `You study the ${definition.name} and recover ${definition.activationReward}.`)
  return { kind: 'activated', events: [event('pickup')] }
}

export const moveCartByForce = (state: RunState, point: Point, pull: boolean): ActionResult | undefined => {
  const cart = propAt(state.floor.props, point.x, point.y)
  if (cart?.kind !== 'mine.brokenCart' || cart.state === 'destroyed') return undefined
  const away = { x: Math.sign(cart.x - state.hero.x), y: Math.sign(cart.y - state.hero.y) }
  const direction = pull ? { x: -away.x, y: -away.y } : away
  if (!cardinal(direction)) { log(state, 'The cart cannot find a rail-aligned force path.'); return [] }
  cart.state = 'activated'
  return moveCart(state, cart, direction)
}

const destroyProp = (state: RunState, prop: Prop, effect: PropEffectKind): void => {
  const definition = propDefinition(prop.kind)
  prop.state = 'destroyed'
  reward(state, prop, definition.effectReward)
  log(state, `The ${effect} breaks the ${definition.name}; it leaves ${definition.effectReward}.`)
}

export const applyPropEffects = (state: RunState, points: readonly Point[], effects: readonly PropEffectKind[]): string[] => {
  const targets = new Set(points.map(pointKey))
  const changed: string[] = []
  for (const prop of state.floor.props) {
    if (prop.state === 'destroyed' || !targets.has(pointKey(prop))) continue
    const effect = effects.find(candidate => prop.hooks?.includes(candidate))
    if (!effect) continue
    if (prop.kind === 'mine.lanternPost') {
      if (effect === 'fire') { prop.state = 'activated'; log(state, 'The lantern post catches and spills local light.'); changed.push(prop.id); continue }
      if (effect === 'water' || effect === 'hazard') { prop.state = 'dormant'; log(state, 'The lantern post gutters out.'); changed.push(prop.id); continue }
    }
    if (prop.kind === 'mine.oreVein' && effect === 'bomb') {
      prop.state = 'destroyed'
      const tile = getTile(state.floor, prop.x, prop.y)
      if (tile) tile.kind = 'floor'
      reward(state, prop, 'rock', 2)
      log(state, 'The blast clears the ore vein and scatters rock.');
      changed.push(prop.id)
      continue
    }
    if (prop.kind === 'mine.brokenCart' && effect === 'force') continue
    if (prop.kind === 'mine.discardedParcel') {
      prop.state = 'destroyed'
      log(state, 'The abandoned parcel bursts into a noisy blast.')
      changed.push(prop.id)
      explode(state, prop.x, prop.y, 5, ['bomb'], 'the discarded parcel')
      continue
    }
    destroyProp(state, prop, effect)
    changed.push(prop.id)
  }
  return changed
}
