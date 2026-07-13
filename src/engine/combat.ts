import { ITEM } from '../content'
import type { Actor, Direction, RunState } from '../types'
import { DIRECTIONS } from '../types'
import { actorAt, getTile, isPassable } from '../world'
import { gainXp, monsterXp } from './progression'
import { event, distance, equipmentDefense, log, turnRng, type ActionResult } from './shared'
import { hasLine, refreshFov } from './visibility'

export function moveHero(state: RunState, direction: Direction): ActionResult {
  const delta = DIRECTIONS[direction]
  if (direction === 'wait') return advance(state, [event('move')])
  const x = state.hero.x + delta.x
  const y = state.hero.y + delta.y
  const target = actorAt(state.floor, x, y)
  if (target?.hostile) return heroAttack(state, target)
  const tile = getTile(state.floor, x, y)
  if (!tile) return []
  if (tile.kind === 'door') { tile.kind = 'floor'; log(state, 'You open the door.'); return advance(state, [event('move')]) }
  if (tile.kind === 'lockedDoor') {
    if (state.hero.keys < 1) { log(state, 'A key is required.'); return [] }
    state.hero.keys--
    tile.kind = 'floor'
    log(state, 'You unlock the door.')
    return advance(state, [event('move')])
  }
  if (!isPassable(state.floor, x, y)) { log(state, 'The way is blocked.'); return [] }
  state.hero.x = x
  state.hero.y = y
  const events: ActionResult = [event('move')]
  if (tile.kind === 'spikes' || tile.kind === 'dart' || tile.kind === 'fireVent') events.push(...damageHero(state, tile.kind === 'spikes' ? 3 : 4, 'a trap'))
  if (tile.kind === 'lava') events.push(...damageHero(state, 8, 'lava'))
  if (tile.kind === 'gas') events.push(...damageHero(state, 2, 'poison gas'))
  if (tile.kind === 'crumble') { tile.kind = 'pit'; log(state, 'The floor crumbles into a pit.'); events.push(event('danger')) }
  if (tile.kind === 'boulder') { tile.kind = 'floor'; events.push(...damageHero(state, 6, 'a rolling boulder')) }
  return advance(state, events)
}

export function advance(state: RunState, events: ActionResult): ActionResult {
  state.turn++
  for (const actor of [...state.floor.actors]) {
    if (!actor.hostile || actor.health <= 0) continue
    actor.energy += actor.speed
    while (actor.energy >= 100 && state.status === 'playing') {
      actor.energy -= 100
      events.push(...actorTurn(state, actor))
    }
  }
  tickEnvironment(state, events)
  refreshFov(state)
  return events
}

export function damageHero(state: RunState, amount: number, source: string): ActionResult {
  state.hero.health -= amount
  log(state, `${source} harms you for ${amount}.`)
  if (state.hero.health > 0) return [event('hurt')]
  state.hero.health = 0
  state.status = 'dead'
  state.modal = undefined
  log(state, 'Your expedition ends here.')
  return [event('death')]
}

export function explode(state: RunState, x: number, y: number, damage: number): void {
  for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
    const tx = x + dx
    const ty = y + dy
    const tile = getTile(state.floor, tx, ty)
    if (tile && tile.kind === 'wall' && tx > 0 && tx < 47 && ty > 0 && ty < 34) tile.kind = 'floor'
    const actor = actorAt(state.floor, tx, ty)
    if (actor?.hostile) actor.health -= damage
    if (state.hero.x === tx && state.hero.y === ty) damageHero(state, Math.max(1, Math.floor(damage / 3)), 'your bomb')
  }
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  log(state, 'The blast tears through the stone.')
}

function heroAttack(state: RunState, target: Actor): ActionResult {
  const weapon = state.hero.equipment.mainHand ? ITEM[state.hero.equipment.mainHand] : undefined
  const rng = turnRng(state, 'combat', `hero:${target.id}`)
  if (rng.int(1, 20) + state.hero.stats.strength + state.hero.level < target.defense) {
    log(state, `Your ${weapon?.name ?? 'fists'} miss ${target.name}.`)
    return advance(state, [event('hit')])
  }
  const damage = Math.max(1, (weapon?.damage ?? 2) + state.hero.stats.strength + rng.int(0, 3) - Math.floor(target.defense / 8))
  target.health -= damage
  log(state, `You strike ${target.name} for ${damage}.`)
  if (target.health <= 0) {
    log(state, `${target.name} falls.`)
    dropLoot(state, target)
    state.floor.actors = state.floor.actors.filter(actor => actor !== target)
    if (target.role === 'guardian') { state.floor.guardianDefeated = true; log(state, 'The way to the exit is open.') }
    gainXp(state, monsterXp(target.kind))
  }
  return advance(state, [event('hit')])
}

function actorTurn(state: RunState, actor: Actor): ActionResult {
  const range = distance(actor, state.hero)
  if (range <= 1) return monsterAttack(state, actor)
  if (actor.ai === 'ranged' && range <= 7 && hasLine(state, actor, state.hero)) return monsterAttack(state, actor, 1)
  if (range > 10 && actor.ai !== 'guardian') return []
  const candidates = Object.values(DIRECTIONS).filter(delta => delta.x || delta.y).map(delta => ({ x: actor.x + delta.x, y: actor.y + delta.y }))
  const valid = candidates.filter(point => isPassable(state.floor, point.x, point.y) && !(point.x === state.hero.x && point.y === state.hero.y))
  if (!valid.length) return []
  valid.sort((a, b) => distance(a, state.hero) - distance(b, state.hero))
  const rng = turnRng(state, 'combat', `move:${actor.id}`)
  const next = actor.ai === 'wander' && rng.chance(45) ? rng.pick(valid) : valid[0]
  actor.x = next.x
  actor.y = next.y
  return []
}

function monsterAttack(state: RunState, actor: Actor, ranged = 0): ActionResult {
  const rng = turnRng(state, 'combat', `attack:${actor.id}`)
  const dodge = 10 + state.hero.stats.agility + equipmentDefense(state.hero)
  if (rng.int(1, 20) + actor.attack < dodge) { log(state, `${actor.name} misses.`); return [event('hurt')] }
  const damage = Math.max(1, actor.attack + ranged + rng.int(0, 3) - Math.floor(state.hero.stats.vitality / 2) - equipmentDefense(state.hero))
  return damageHero(state, damage, actor.name)
}

function tickEnvironment(state: RunState, events: ActionResult): void {
  for (const actor of state.floor.actors) {
    const tile = getTile(state.floor, actor.x, actor.y)
    if (!tile || !actor.hostile) continue
    if (tile.kind === 'lava') actor.health -= 4
    if (tile.kind === 'fireVent' && turnRng(state, 'combat', `vent:${actor.id}`).chance(25)) actor.health -= 3
  }
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  if (tile?.kind === 'fireVent' && turnRng(state, 'combat', 'vent:hero').chance(20)) events.push(...damageHero(state, 3, 'a fire vent'))
  if (state.turn % 8 === 0 && state.hero.focus < state.hero.maxFocus) state.hero.focus++
}

function dropLoot(state: RunState, actor: Actor): void {
  const rng = turnRng(state, 'loot', `drop:${actor.id}`)
  state.floor.items.push({ id: 'gold', x: actor.x, y: actor.y, count: actor.role === 'guardian' ? rng.int(130, 210) : rng.int(5, 18) })
  const tables: Record<string, string[]> = {
    mine: ['rock', 'tonic', 'bombPack', 'key'], wilds: ['tonic', 'ropeBundle', 'machete', 'focusTonic'], caverns: ['focusTonic', 'ember', 'mend', 'spear'], ruins: ['mapScroll', 'ward', 'wardScript', 'blinkRune']
  }
  if (actor.role === 'guardian' || rng.chance(28)) state.floor.items.push({ id: rng.pick(tables[state.floor.biome]), x: actor.x, y: actor.y, count: 1 })
}
