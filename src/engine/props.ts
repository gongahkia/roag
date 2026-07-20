import { DIRECTIONS, type Point, type Prop, type PropEffectKind, type RunState } from '../types'
import { isBlockingProp, propAt, propDefinition } from '../props'
import { actorAt, getTile, isPassable, preservesExitPath } from '../world'
import { damageHero, explode, resolveDefeatedActors } from './combat'
import { addCondition, modifyIncomingDamage } from './conditions'
import { event, log, type ActionResult } from './shared'

const pointKey = (point: Point): string => `${point.x},${point.y}`
const mineProp = (prop: Prop): boolean => prop.biome === 'mine' && prop.kind.startsWith('mine.')
const wildsProp = (prop: Prop): boolean => prop.biome === 'wilds' && prop.kind.startsWith('wilds.')
const cardinal = (point: Point): boolean => Math.abs(point.x) + Math.abs(point.y) === 1
const hazardKinds = new Set(['spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'gas', 'lava', 'pit'])

const reward = (state: RunState, prop: Prop, id: string, count = 1): void => {
  state.floor.items.push({ id, x: prop.x, y: prop.y, count, visibleInFog: true })
}

const nearbyPoints = (point: Point, radius = 1): Point[] => {
  const points: Point[] = []
  for (let y = point.y - radius; y <= point.y + radius; y++) for (let x = point.x - radius; x <= point.x + radius; x++) if (x !== point.x || y !== point.y) points.push({ x, y })
  return points.sort((first, second) => Math.max(Math.abs(first.x - point.x), Math.abs(first.y - point.y)) - Math.max(Math.abs(second.x - point.x), Math.abs(second.y - point.y)) || first.y - second.y || first.x - second.x)
}

const revealLocal = (state: RunState, point: Point, radius = 3): number => {
  let revealed = 0
  for (const candidate of nearbyPoints(point, radius)) {
    const tile = getTile(state.floor, candidate.x, candidate.y)
    if (!tile || tile.explored) continue
    tile.explored = true
    revealed++
  }
  return revealed
}

const clearBramble = (state: RunState, point: Point): boolean => {
  const candidate = nearbyPoints(point, 2).find(current => getTile(state.floor, current.x, current.y)?.kind === 'bramble')
  if (!candidate) return false
  getTile(state.floor, candidate.x, candidate.y)!.kind = 'floor'
  return true
}

const growBramble = (state: RunState, point: Point): Point[] => {
  const grown: Point[] = []
  for (const candidate of nearbyPoints(point)) {
    const tile = getTile(state.floor, candidate.x, candidate.y)
    if (!tile || tile.kind !== 'floor' || actorAt(state.floor, candidate.x, candidate.y) || (state.hero.x === candidate.x && state.hero.y === candidate.y)) continue
    if (!preservesExitPath(state.floor, state.floor.start, candidate, 'bramble')) continue
    tile.kind = 'bramble'
    grown.push(candidate)
    if (grown.length === 2) break
  }
  return grown
}

const clearEffectCells = (state: RunState, prop: Prop): void => {
  for (const point of prop.effectCells ?? []) {
    const tile = getTile(state.floor, point.x, point.y)
    if (tile?.kind === 'bramble') tile.kind = 'floor'
  }
  prop.effectCells = undefined
}

const disturbNest = (state: RunState, prop: Prop): boolean => {
  const candidate = nearbyPoints(prop, 3).find(point => Math.max(Math.abs(point.x - state.hero.x), Math.abs(point.y - state.hero.y)) > 1 && isPassable(state.floor, point.x, point.y) && !actorAt(state.floor, point.x, point.y))
  if (!candidate) return false
  state.floor.actors.push({ id: `${prop.id}:flock`, role: 'monster', kind: 'startledBirds', name: 'startled birds', x: candidate.x, y: candidate.y, health: 4, maxHealth: 4, attack: 3, defense: 8, speed: 125, energy: 0, glyph: 'b', color: '#d8bc82', hostile: true, ai: 'chase', conditions: [] })
  return true
}

const consumeWildsCharm = (state: RunState): 'root' | 'mend' | 'wardScript' | undefined => {
  const id = state.hero.inventory.find(item => item === 'root' || item === 'mend' || item === 'wardScript') as 'root' | 'mend' | 'wardScript' | undefined
  if (!id) return undefined
  state.hero.inventory.splice(state.hero.inventory.indexOf(id), 1)
  return id
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
  const events: ActionResult = []
  if (!cardinal(first) || getTile(state.floor, cart.x + first.x, cart.y + first.y)?.kind !== 'rail') { log(state, 'The cart has no rail in that direction.'); return [] }
  let direction = { ...first }
  let previous = { x: cart.x, y: cart.y }
  while (true) {
    const next = { x: cart.x + direction.x, y: cart.y + direction.y }
    if (getTile(state.floor, next.x, next.y)?.kind !== 'rail' || isBlockingProp(propAt(state.floor.props, next.x, next.y))) break
    const actor = actorAt(state.floor, next.x, next.y)
    if (actor) {
      if (!actor.hostile) {
        log(state, `The cart is blocked by ${actor.name}.`)
        break
      }
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
    if (!events.some(entry => entry.type === 'move')) events.push(event('move'))
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

const operateWildsProp = (state: RunState, prop: Prop): PropOperation | undefined => {
  if (prop.kind === 'wilds.mushrooms') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Harvest them with C, burn them for flame, or crush them to release spores.')
    if (prop.state === 'activated') { log(state, 'The mushroom patch has already been harvested.'); return { kind: 'examined', events: [] } }
    prop.state = 'activated'
    reward(state, prop, 'tonic')
    log(state, 'You harvest a vital tonic and leave the spores undisturbed.')
    return { kind: 'activated', events: [event('pickup')] }
  }
  if (prop.kind === 'wilds.danglingCharm') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to take its ward, or use a Brush Blade, Root, or fire to cut the nearby growth.')
    if (prop.state === 'activated') { log(state, 'The branch hangs bare.'); return { kind: 'examined', events: [] } }
    if (state.hero.equipment.mainHand === 'machete') {
      prop.state = 'destroyed'
      const cleared = clearBramble(state, prop)
      log(state, cleared ? 'You cut the charm loose and clear a bramble choke point.' : 'You cut the charm loose before its curse can take hold.')
      return { kind: 'activated', events: [event('move')] }
    }
    prop.state = 'activated'
    reward(state, prop, 'wardScript')
    log(state, 'You take the charm as a ward and leave the roots untouched.')
    return { kind: 'activated', events: [event('pickup')] }
  }
  if (prop.kind === 'wilds.birdNest') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to loot it, or throw or burn it to startle the flock.')
    if (prop.state === 'activated') { log(state, 'The nest is empty.'); return { kind: 'examined', events: [] } }
    prop.state = 'activated'
    reward(state, prop, 'tonic')
    log(state, 'You take a tonic without disturbing the nest.')
    return { kind: 'activated', events: [event('pickup')] }
  }
  if (prop.kind === 'wilds.rootShrine') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Offer a Root, Mending, or Ward Charm with C to raise a short-lived thorn screen.')
    if (prop.state === 'activated') { log(state, 'The shrine has spent its roots.'); return { kind: 'examined', events: [] } }
    const charm = consumeWildsCharm(state)
    if (!charm) { log(state, 'The shrine answers only Root, Mending, or Ward Charms.'); return { kind: 'examined', events: [] } }
    prop.state = 'activated'
    const grown = growBramble(state, prop)
    prop.effectCells = grown.map(point => ({ ...point }))
    prop.expiresAt = state.turn + 4
    if (charm === 'mend') state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 6)
    if (charm === 'wardScript') addCondition(state.hero, { kind: 'shielded', duration: 4, potency: 1 })
    log(state, `The ${charm === 'wardScript' ? 'ward' : charm} charm raises ${grown.length || 'a'} thorn screen beside the shrine.`)
    return { kind: 'activated', events: [event('spell')] }
  }
  if (prop.kind === 'wilds.lostParcel') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to recover rope and cut a nearby bramble route, or leave the parcel for later.')
    if (prop.state === 'activated') { log(state, 'The lost parcel has already been recovered.'); return { kind: 'examined', events: [] } }
    prop.state = 'activated'
    reward(state, prop, 'ropeBundle')
    const cleared = clearBramble(state, prop)
    log(state, cleared ? 'You recover rope and uncover a clear alternate trail.' : 'You recover rope from the lost parcel.')
    return { kind: 'activated', events: [event('pickup')] }
  }
  if (prop.kind === 'wilds.rootArch') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Use a Brush Blade with C, Root to open a living detour, or burn through the arch.')
    if (prop.state === 'activated') { log(state, 'The roots hold open a clear passage.'); return { kind: 'examined', events: [] } }
    if (state.hero.equipment.mainHand !== 'machete') { log(state, 'A Brush Blade, Root Charm, or flame can open the living arch.'); return { kind: 'examined', events: [] } }
    prop.state = 'activated'
    log(state, 'You cut a bounded passage through the living roots.')
    return { kind: 'activated', events: [event('move')] }
  }
  return undefined
}

export interface PropOperation { kind: 'examined' | 'activated' | 'moved'; events: ActionResult }

export const operateProp = (state: RunState): PropOperation | undefined => {
  const prop = nearbyProp(state)
  if (!prop || !prop.hooks?.includes('operate')) return undefined
  if (mineProp(prop)) return operateMineProp(state, prop)
  if (wildsProp(prop)) return operateWildsProp(state, prop)
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
  const events = moveCart(state, cart, direction)
  if (events.length) cart.state = 'activated'
  return events
}

export const releaseCartWithRope = (state: RunState): ActionResult | undefined => {
  const cart = Object.values(DIRECTIONS)
    .filter(cardinal)
    .map(delta => propAt(state.floor.props, state.hero.x + delta.x, state.hero.y + delta.y))
    .find((prop): prop is Prop => prop?.kind === 'mine.brokenCart' && prop.state !== 'destroyed')
  if (!cart) return undefined
  if (cart.state === 'dormant') { log(state, 'Examine the cart before rigging it with a rope.'); return [] }
  const direction = { x: Math.sign(cart.x - state.hero.x), y: Math.sign(cart.y - state.hero.y) }
  const events = moveCart(state, cart, direction)
  if (events.length) cart.state = 'activated'
  return events
}

const destroyProp = (state: RunState, prop: Prop, effect: PropEffectKind): void => {
  const definition = propDefinition(prop.kind)
  prop.state = 'destroyed'
  reward(state, prop, definition.effectReward)
  log(state, `The ${effect} breaks the ${definition.name}; it leaves ${definition.effectReward}.`)
}

export const expirePropEffects = (state: RunState): void => {
  for (const prop of state.floor.props) {
    if (prop.expiresAt === undefined || prop.expiresAt > state.turn) continue
    clearEffectCells(state, prop)
    prop.expiresAt = undefined
    if (prop.kind === 'wilds.rootShrine') log(state, 'The shrine\'s thorn screen withers away.')
    if (prop.kind === 'wilds.birdNest') {
      state.floor.actors = state.floor.actors.filter(actor => actor.id !== `${prop.id}:flock`)
      log(state, 'The startled birds scatter back into the canopy.')
    }
  }
}

const applyWildsEffect = (state: RunState, prop: Prop, effect: PropEffectKind): boolean => {
  if (prop.kind === 'wilds.mushrooms') {
    const tile = getTile(state.floor, prop.x, prop.y)
    prop.state = effect === 'water' ? 'activated' : 'destroyed'
    if (effect === 'water') { const revealed = revealLocal(state, prop); log(state, `The wet mushrooms glow and reveal ${revealed} nearby tiles.`) }
    else if (effect === 'fire') { if (tile?.kind === 'floor' || tile?.kind === 'web') tile.kind = 'fireVent'; log(state, 'The mushrooms flare into a visible fire patch.') }
    else { if (tile?.kind === 'floor' || tile?.kind === 'web') tile.kind = 'gas'; log(state, 'The mushrooms burst and release a visible spore cloud.') }
    return true
  }
  if (prop.kind === 'wilds.danglingCharm') {
    prop.state = 'destroyed'
    const cleared = clearBramble(state, prop)
    log(state, cleared ? 'The severed charm pulls a bramble choke point apart.' : 'The dangling charm unravels into harmless roots.')
    return true
  }
  if (prop.kind === 'wilds.birdNest') {
    if (prop.state !== 'activated') {
      prop.state = 'activated'
      const disturbed = disturbNest(state, prop)
      if (disturbed) prop.expiresAt = state.turn + 4
      log(state, disturbed ? 'The disturbed nest draws startled birds into the path.' : 'The startled flock scatters beyond the trail.')
    }
    return true
  }
  if (prop.kind === 'wilds.rootShrine') {
    if (effect === 'root') {
      prop.state = 'activated'
      const grown = growBramble(state, prop)
      prop.effectCells = grown.map(point => ({ ...point }))
      prop.expiresAt = state.turn + 4
      log(state, `The shrine sends up ${grown.length || 'a'} thorn screen.`)
    } else {
      clearEffectCells(state, prop)
      prop.state = 'destroyed'
      log(state, 'The shrine\'s roots char and fall away.')
    }
    return true
  }
  if (prop.kind === 'wilds.lostParcel') {
    prop.state = 'destroyed'
    const cleared = clearBramble(state, prop)
    log(state, cleared ? 'The parcel bursts open and clears a bramble route.' : 'The lost parcel bursts into scattered trail gear.')
    return true
  }
  if (prop.kind === 'wilds.rootArch') {
    prop.state = effect === 'fire' ? 'destroyed' : 'activated'
    log(state, effect === 'fire' ? 'Flame burns a passage through the root arch.' : 'The living roots fold aside into a detour.')
    return true
  }
  return false
}

export const applyPropEffects = (state: RunState, points: readonly Point[], effects: readonly PropEffectKind[]): string[] => {
  const targets = new Set(points.map(pointKey))
  const changed: string[] = []
  for (const prop of state.floor.props) {
    if (prop.state === 'destroyed' || !targets.has(pointKey(prop))) continue
    const effect = effects.find(candidate => prop.hooks?.includes(candidate))
    if (!effect) continue
    if (wildsProp(prop) && applyWildsEffect(state, prop, effect)) { changed.push(prop.id); continue }
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
