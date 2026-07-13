import { ITEM } from '../content'
import type { Actor, Direction, RunState } from '../types'
import { DIRECTIONS } from '../types'
import { actorAt, getTile, isPassable } from '../world'
import { gainXp, monsterXp } from './progression'
import { event, distance, equipmentDefense, log, turnRng, type ActionResult } from './shared'
import { announceTelegraph, resolveTelegraphs } from './telegraphs'
import { refreshFov } from './visibility'
import { conditionSpeed, hasCondition, modifyIncomingDamage, tickConditions } from './conditions'
import { resolveTerrainReactions, type TerrainTag } from './terrain'
import { actionCells } from './geometry'
import { planEnemyIntent } from './intents'
import { projectBolt } from './projectiles'
import { advanceGuardianPhase } from './guardians'
import { completeObjective } from '../objectives'

export function moveHero(state: RunState, direction: Direction): ActionResult {
  const delta = DIRECTIONS[direction]
  if (direction === 'wait') return advance(state, [event('move')])
  if (hasCondition(state.hero, 'rooted')) { log(state, 'Roots hold you in place.'); return advance(state, [event('danger')]) }
  const x = state.hero.x + delta.x
  const y = state.hero.y + delta.y
  const weapon = state.hero.equipment.mainHand ? ITEM[state.hero.equipment.mainHand] : undefined
  const profile = weapon?.weapon ?? { damage: 2, reach: 1, shape: 'adjacent' as const, cooldown: 0, tags: ['unarmed'] }
  const targets = actionCells(profile.shape, state.hero, direction, profile.reach).map(point => actorAt(state.floor, point.x, point.y)).filter((target): target is Actor => Boolean(target?.hostile))
  if (targets.length) return heroAttack(state, targets, weapon?.id, profile.damage, profile.cooldown)
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
  const resolvedTelegraphs = resolveTelegraphs(state)
  for (const telegraph of resolvedTelegraphs) {
    if (telegraph.actionId !== 'enemy-shot' && telegraph.actionId !== 'guardian-slam') continue
    const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)
    const hit = telegraph.cells.some(cell => cell.x === state.hero.x && cell.y === state.hero.y)
    if (source?.hostile && hit) events.push(...monsterAttack(state, source, telegraph.actionId === 'guardian-slam' ? 2 : 1))
    else log(state, `${telegraph.actionId === 'guardian-slam' ? 'The slam' : 'The bolt'} passes harmlessly.`)
  }
  if (resolvedTelegraphs.length) events.push(event('danger'))
  for (const actor of [...state.floor.actors]) {
    if (!actor.hostile || actor.health <= 0) continue
    actor.energy += conditionSpeed(actor, actor.speed)
    while (actor.energy >= 100 && state.status === 'playing') {
      actor.energy -= 100
      events.push(...actorTurn(state, actor))
    }
  }
  tickEnvironment(state, events)
  tickConditionEffects(state, events)
  for (const [id, cooldown] of Object.entries(state.hero.cooldowns ?? {})) {
    if (cooldown <= 1) delete state.hero.cooldowns![id]
    else state.hero.cooldowns![id] = cooldown - 1
  }
  refreshFov(state)
  return events
}

export function damageHero(state: RunState, amount: number, source: string): ActionResult {
  amount = modifyIncomingDamage(state.hero, amount)
  state.hero.health -= amount
  log(state, `${source} harms you for ${amount}.`)
  if (state.hero.health > 0) return [event('hurt')]
  state.hero.health = 0
  state.status = 'dead'
  state.modal = undefined
  log(state, 'Your expedition ends here.')
  return [event('death')]
}

export function explode(state: RunState, x: number, y: number, damage: number, tags: TerrainTag[] = ['bomb']): void {
  const points = [] as Array<{ x: number; y: number }>
  for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
    const tx = x + dx
    const ty = y + dy
    points.push({ x: tx, y: ty })
    const tile = getTile(state.floor, tx, ty)
    if (tile && tile.kind === 'wall' && tx > 0 && tx < 47 && ty > 0 && ty < 34) tile.kind = 'floor'
    const actor = actorAt(state.floor, tx, ty)
    if (actor?.hostile) actor.health -= modifyIncomingDamage(actor, damage)
    if (state.hero.x === tx && state.hero.y === ty) damageHero(state, Math.max(1, Math.floor(damage / 3)), 'your bomb')
  }
  for (const effect of resolveTerrainReactions(state.floor, points, tags)) log(state, `Terrain reaction: ${effect.reaction}.`)
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  log(state, 'The blast tears through the stone.')
}

function heroAttack(state: RunState, targets: Actor[], weaponId: string | undefined, baseDamage: number, cooldown: number): ActionResult {
  if (weaponId && (state.hero.cooldowns?.[weaponId] ?? 0) > 0) { log(state, 'Your weapon is recovering.'); return [] }
  for (const target of targets) {
    const rng = turnRng(state, 'combat', `hero:${target.id}`)
    if (rng.int(1, 20) + state.hero.stats.strength + state.hero.level < target.defense) { log(state, `Your attack misses ${target.name}.`); continue }
    const damage = modifyIncomingDamage(target, Math.max(1, baseDamage + state.hero.stats.strength + rng.int(0, 3) - Math.floor(target.defense / 8)))
    target.health -= damage
    log(state, `You strike ${target.name} for ${damage}.`)
    if (target.health <= 0) {
      log(state, `${target.name} falls.`)
      dropLoot(state, target)
      if (target.role === 'guardian') { state.floor.guardianDefeated = true; if (completeObjective(state, 'defeatGuardian')) log(state, 'Objective complete: guardian defeated.'); log(state, 'The way to the exit is open.') }
      gainXp(state, monsterXp(target.kind))
    }
  }
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  const events = advance(state, [event('hit')])
  if (weaponId && cooldown) (state.hero.cooldowns ??= {})[weaponId] = cooldown
  return events
}

function actorTurn(state: RunState, actor: Actor): ActionResult {
  if (hasCondition(actor, 'staggered')) return []
  advanceGuardianPhase(state, actor)
  const intent = planEnemyIntent(state, actor)
  log(state, `${actor.name}: ${intent.action.name} (${intent.reason}).`)
  if (intent.action.id === 'enemy-strike') return monsterAttack(state, actor)
  if (intent.action.id === 'enemy-shot') return announceProjectile(state, actor)
  if (intent.action.id === 'guardian-slam') return announceGuardianSlam(state, actor)
  const candidates = Object.values(DIRECTIONS).filter(delta => delta.x || delta.y).map(delta => ({ x: actor.x + delta.x, y: actor.y + delta.y }))
  const valid = candidates.filter(point => isPassable(state.floor, point.x, point.y) && !(point.x === state.hero.x && point.y === state.hero.y))
  if (!valid.length) return []
  valid.sort((a, b) => intent.action.id === 'enemy-reposition' ? distance(b, state.hero) - distance(a, state.hero) : distance(a, state.hero) - distance(b, state.hero))
  const rng = turnRng(state, 'combat', `move:${actor.id}`)
  const next = actor.ai === 'wander' && rng.chance(45) ? rng.pick(valid) : valid[0]
  actor.x = next.x
  actor.y = next.y
  return []
}

function announceProjectile(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:shot`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  const bolt = projectBolt(state.floor, actor, state.hero)
  if (bolt.cover || !bolt.collision) { log(state, `${actor.name}'s shot is blocked by cover.`); return [] }
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'enemy-shot', cells: bolt.cells, danger: 'major', windup: 1, collision: { point: bolt.collision.point, by: bolt.collision.by }, cover: bolt.cover })
  return [event('danger')]
}

function announceGuardianSlam(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:slam`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  const direction = directionToward(actor, state.hero)
  const cells = actionCells('cross', actor, direction, 1)
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'guardian-slam', cells, danger: 'major', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function directionToward(from: { x: number; y: number }, to: { x: number; y: number }): Exclude<Direction, 'wait'> {
  const x = Math.sign(to.x - from.x)
  const y = Math.sign(to.y - from.y)
  if (x === 0 && y < 0) return 'n'
  if (x > 0 && y < 0) return 'ne'
  if (x > 0 && y === 0) return 'e'
  if (x > 0 && y > 0) return 'se'
  if (x === 0 && y > 0) return 's'
  if (x < 0 && y > 0) return 'sw'
  if (x < 0 && y === 0) return 'w'
  return 'nw'
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

function tickConditionEffects(state: RunState, events: ActionResult): void {
  const heroTick = tickConditions(state.hero)
  if (heroTick.burningDamage) events.push(...damageHero(state, heroTick.burningDamage, 'burning'))
  for (const actor of state.floor.actors) {
    const tick = tickConditions(actor)
    if (tick.burningDamage) { actor.health -= tick.burningDamage; log(state, `${actor.name} burns for ${tick.burningDamage}.`) }
  }
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
}

function dropLoot(state: RunState, actor: Actor): void {
  const rng = turnRng(state, 'loot', `drop:${actor.id}`)
  state.floor.items.push({ id: 'gold', x: actor.x, y: actor.y, count: actor.role === 'guardian' ? rng.int(130, 210) : rng.int(5, 18) })
  const tables: Record<string, string[]> = {
    mine: ['rock', 'tonic', 'bombPack', 'key'], wilds: ['tonic', 'ropeBundle', 'machete', 'focusTonic'], caverns: ['focusTonic', 'ember', 'mend', 'spear'], ruins: ['mapScroll', 'ward', 'wardScript', 'blinkRune']
  }
  if (actor.role === 'guardian' || rng.chance(28)) state.floor.items.push({ id: rng.pick(tables[state.floor.biome]), x: actor.x, y: actor.y, count: 1 })
}
