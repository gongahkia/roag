import { ITEM, biomeName, shopStock } from '../content'
import { type Direction, type Modal, type RunState, DIRECTIONS } from '../types'
import { actorAt, generateAreaFloor, getTile } from '../world'
import { advance, explode } from './combat'
import { resolveLineEffect } from './line-effect'
import { modifyIncomingDamage } from './conditions'
import { gateForArea } from './gates'
import { gainXp } from './progression'
import { recordRescue } from './rescue'
import { completeObjective } from '../objectives'
import { consume, distance, event, log, turnRng, type ActionResult } from './shared'
import { refreshFov } from './visibility'
import { evaluateEquipmentEffects } from './equipment'
import { vitalityRecovery, vitalityRescueRecovery } from './vitality'
import { scriptCastProfile } from './scripts'
import { castEmber } from './ember'
import { castVerdant, isVerdantSpell } from './verdant'
import { castAstral, isAstralSpell } from './astral'
import { announceSynergies, resolveSynergies } from './synergies'

export function pickUp(state: RunState): ActionResult {
  const item = state.floor.items.find(current => current.x === state.hero.x && current.y === state.hero.y)
  if (!item) { log(state, 'Nothing here to take.'); return [] }
  if (item.id === 'gold') { state.hero.gold += item.count; state.floor.items = state.floor.items.filter(current => current !== item); log(state, `You recover ${item.count} gold.`); return advance(state, [event('pickup')]) }
  if (item.id === 'key') { state.hero.keys += item.count; state.floor.items = state.floor.items.filter(current => current !== item); log(state, 'You take an iron key.'); return advance(state, [event('pickup')]) }
  if (state.hero.inventory.length >= 12) { log(state, 'Your pack is full.'); return [] }
  state.hero.inventory.push(item.id)
  state.floor.items = state.floor.items.filter(current => current !== item)
  log(state, `You take ${ITEM[item.id].name}.`)
  return advance(state, [event('pickup')])
}

export function operate(state: RunState): ActionResult {
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  const friend = state.floor.actors.find(actor => !actor.hostile && distance(actor, state.hero) <= 1)
  const container = nearbyContainer(state)
  if (nearbyGate(state)) {
    const gate = gateForArea(state.area ?? state.floor.biome)
    state.modal = { kind: 'gate', gateId: gate.id }
    log(state, gate.npcOffering)
    return [event('menu')]
  }
  if (friend?.role === 'merchant') { state.modal = { kind: 'shop', merchantId: friend.id }; return [event('menu')] }
  if (container) {
    container.tile.kind = 'floor'
    const loot = turnRng(state, 'loot', `container:${container.x},${container.y}`).pick(['tonic', 'focusTonic', 'bombPack', 'ropeBundle', 'rock', 'mapScroll', 'ward'])
    state.hero.gold += container.kind === 'chest' ? 60 : 18
    if (state.hero.inventory.length < 12) state.hero.inventory.push(loot)
    else state.floor.items.push({ id: loot, x: container.x, y: container.y, count: 1 })
    log(state, `You open the ${container.kind} and find ${ITEM[loot].name}.`)
    if (completeObjective(state, 'recoverSupplies')) log(state, 'Objective complete: supply cache secured.')
    return advance(state, [event('pickup')])
  }
  if (tile?.kind === 'rescue' || friend?.name === 'lost scout') {
    const npc = recordRescue(state, friend)
    state.hero.maxHealth += 2
    state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 8 + vitalityRescueRecovery(state.hero))
    state.hero.gold += 35
    state.floor.actors = state.floor.actors.filter(actor => actor !== friend)
    const eventTile = friend ? getTile(state.floor, friend.x, friend.y) : tile
    if (eventTile?.kind === 'rescue' || eventTile?.kind === 'altar') eventTile.kind = 'floor'
    log(state, `${npc.name} reaches the expedition hub.`)
    if (completeObjective(state, 'rescueScout')) log(state, 'Objective complete: scout rescued.')
    return advance(state, [event('rescue')])
  }
  if (tile?.kind === 'altar') {
    if (state.hero.gold < 75) { log(state, 'The altar asks for 75 gold.'); return [] }
    state.hero.gold -= 75
    gainXp(state, 35)
    log(state, 'The altar grants insight.')
    if (completeObjective(state, 'invokeAltar')) log(state, 'Objective complete: altar invoked.')
    return advance(state, [event('spell')])
  }
  log(state, 'Nothing answers.')
  return []
}

export function descend(state: RunState): ActionResult {
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  if (tile?.kind !== 'exit') { log(state, 'You are not at the exit.'); return [] }
  if (state.floor.objective.status !== 'complete') { log(state, `Objective incomplete: ${state.floor.objective.label}.`); return [] }
  if (!state.floor.guardianDefeated) { log(state, 'A guardian still seals the route.'); return [] }
  const areaFloor = state.areaFloor ?? state.floor.index % 4
  if (areaFloor === 3) { state.modal = undefined; log(state, `${biomeName[state.area ?? state.floor.biome]} is secured. Return to the hub.`); return [event('areaComplete')] }
  const nextAreaFloor = areaFloor + 1
  state.floor = generateAreaFloor(state.seed, state.area ?? state.floor.biome, nextAreaFloor)
  state.areaFloor = nextAreaFloor
  state.hero.x = state.floor.start.x
  state.hero.y = state.floor.start.y
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 4 + vitalityRecovery(state.hero))
  state.hero.focus = state.hero.maxFocus
  log(state, `You descend into the ${biomeName[state.floor.biome]}.`)
  refreshFov(state)
  return [event('floor')]
}

export function inventoryChoice(state: RunState, modal: Extract<Modal, { kind: 'inventory' }>, command: string): ActionResult {
  const index = Number(command) - 1
  if (!Number.isInteger(index) || index < 0 || index >= state.hero.inventory.length) return []
  const id = state.hero.inventory[index]
  state.modal = undefined
  if (modal.mode === 'use') return useItem(state, id, index)
  if (modal.mode === 'drop') {
    state.hero.inventory.splice(index, 1)
    state.floor.items.push({ id, x: state.hero.x, y: state.hero.y, count: 1 })
    log(state, `You drop ${ITEM[id].name}.`)
    return advance(state, [event('pickup')])
  }
  if (modal.mode === 'throw') { state.modal = { kind: 'target', action: 'throw', item: id }; return [event('menu')] }
  return equip(state, id, index)
}

export function useRope(state: RunState): ActionResult {
  if (state.hero.ropes < 1) { log(state, 'No ropes remain.'); return [] }
  const tile = getTile(state.floor, state.hero.x, state.hero.y)!
  if (tile.kind === 'pit') tile.kind = 'rope'
  else {
    const below = getTile(state.floor, state.hero.x, state.hero.y + 1)
    if (below?.kind === 'pit') below.kind = 'rope'
    else { log(state, 'There is nowhere to anchor a rope.'); return [] }
  }
  state.hero.ropes--
  log(state, 'You secure a rope.')
  return advance(state, [event('pickup')])
}

export function castFirstSpell(state: RunState): ActionResult {
  const id = state.hero.inventory.find(item => ITEM[item].use === 'spell')
  if (!id) { log(state, 'You know no ready script.'); return [] }
  state.modal = { kind: 'target', action: 'spell', item: id }
  return [event('menu')]
}

export function quickCast(state: RunState, direction: Direction): ActionResult {
  const id = state.hero.inventory.find(item => ITEM[item].use === 'spell')
  if (!id) { log(state, 'You know no ready script.'); return [] }
  return castSpell(state, id, direction)
}

export function bomb(state: RunState, direction: Direction): ActionResult {
  if (state.hero.bombs < 1) { log(state, 'No bombs remain.'); return [] }
  state.hero.bombs--
  const delta = DIRECTIONS[direction]
  explode(state, state.hero.x + delta.x, state.hero.y + delta.y, 12)
  return advance(state, [event('boom')])
}

export function throwItem(state: RunState, id: string, direction: Direction): ActionResult {
  const index = state.hero.inventory.indexOf(id)
  if (index === -1) return []
  state.hero.inventory.splice(index, 1)
  const delta = DIRECTIONS[direction]
  const destination = { x: state.hero.x + delta.x * 5, y: state.hero.y + delta.y * 5 }
  const cells = resolveLineEffect(state.floor, state.hero, destination).cells
  const point = cells.at(-1) ?? { x: state.hero.x, y: state.hero.y }
  const target = actorAt(state.floor, point.x, point.y)
  if (target?.hostile) { target.health -= modifyIncomingDamage(target, 3 + state.hero.stats.strength); log(state, `${ITEM[id].name} hits ${target.name}.`) }
  if (id === 'fireJar') explode(state, point.x, point.y, 5, ['bomb', 'fire'])
  else state.floor.items.push({ id, x: point.x, y: point.y, count: 1 })
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  return advance(state, [event(id === 'fireJar' ? 'boom' : 'hit')])
}

export function castSpell(state: RunState, id: string, direction: Direction): ActionResult {
  const item = ITEM[id]
  const profile = scriptCastProfile(state.hero, id)
  if (state.hero.focus < profile.focusCost) { log(state, 'You lack focus.'); return [] }
  state.hero.focus -= profile.focusCost
  const geometry = resolveSynergies({ scripts: [id], skills: state.hero.skills }, { range: profile.range })
  const delta = DIRECTIONS[direction]
  const point = { x: state.hero.x + delta.x * Math.max(1, Math.floor(geometry.values.range ?? profile.range)), y: state.hero.y + delta.y * Math.max(1, Math.floor(geometry.values.range ?? profile.range)) }
  const tile = getTile(state.floor, point.x, point.y)
  const impact = resolveSynergies({ scripts: [id], terrain: tile ? [tile.kind] : [] })
  if (item.spell === 'ember') castEmber(state, point, impact.values.damage ?? 0)
  if (isVerdantSpell(item.spell)) castVerdant(state, item.spell, point)
  if (isAstralSpell(item.spell)) castAstral(state, item.spell, point)
  announceSynergies(state, geometry)
  announceSynergies(state, impact)
  const effect = evaluateEquipmentEffects(state.hero, 'triggered', { trigger: 'spell', scripts: [id] })
  state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + (effect.values.focus ?? 0))
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  refreshFov(state)
  log(state, `${item.name} takes effect.`)
  return advance(state, [event('spell')])
}

export function shopChoice(state: RunState, command: string): ActionResult {
  const id = shopStock(state.floor.biome)[Number(command) - 1]
  if (!id) return []
  const item = ITEM[id]
  if (state.hero.gold < item.value) { log(state, 'Not enough gold.'); return [event('menu')] }
  if (state.hero.inventory.length >= 12) { log(state, 'Your pack is full.'); return [event('menu')] }
  state.hero.gold -= item.value
  state.hero.inventory.push(id)
  log(state, `You buy ${item.name}.`)
  return advance(state, [event('pickup')])
}

function useItem(state: RunState, id: string, inventoryIndex: number): ActionResult {
  const item = ITEM[id]
  if (item.slot) return equip(state, id, inventoryIndex)
  if (item.use === 'heal') { state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 10 + vitalityRecovery(state.hero)); consume(state, inventoryIndex); log(state, 'Warmth returns to your limbs.'); return advance(state, [event('spell')]) }
  if (item.use === 'focus') { state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 8); consume(state, inventoryIndex); log(state, 'Your mind sharpens.'); return advance(state, [event('spell')]) }
  if (item.use === 'map') { for (const tile of state.floor.tiles) tile.explored = true; consume(state, inventoryIndex); log(state, 'The floor map unfolds in your mind.'); return advance(state, [event('spell')]) }
  if (item.use === 'teleport') {
    const choices = state.floor.tiles.flatMap((tile, i) => tile.kind === 'floor' && tile.explored ? [{ x: i % 48, y: Math.floor(i / 48) }] : [])
    if (choices.length) { const target = turnRng(state, 'combat', 'blink').pick(choices); state.hero.x = target.x; state.hero.y = target.y }
    consume(state, inventoryIndex); refreshFov(state); log(state, 'Space folds.'); return advance(state, [event('spell')])
  }
  if (item.use === 'bomb') { state.hero.bombs += 3; consume(state, inventoryIndex); log(state, 'You gain three bombs.'); return advance(state, [event('pickup')]) }
  if (item.use === 'rope') { state.hero.ropes += 3; consume(state, inventoryIndex); log(state, 'You gain three ropes.'); return advance(state, [event('pickup')]) }
  if (item.use === 'key') { state.hero.keys++; consume(state, inventoryIndex); return advance(state, [event('pickup')]) }
  if (item.use === 'spell') { state.modal = { kind: 'target', action: 'spell', item: id }; return [event('menu')] }
  log(state, 'That cannot be used here.')
  return []
}

function equip(state: RunState, id: string, index: number): ActionResult {
  const item = ITEM[id]
  if (!item.slot) { log(state, 'That cannot be equipped.'); return [] }
  const previous = state.hero.equipment[item.slot]
  state.hero.inventory.splice(index, 1)
  if (previous) { state.hero.inventory.push(previous); state.hero.lastUnequipped = previous }
  state.hero.equipment[item.slot] = id
  log(state, `You equip ${item.name}.`)
  return advance(state, [event('pickup')])
}

export function swap(state: RunState): ActionResult {
  const id = state.hero.lastUnequipped
  if (!id || state.hero.inventory.length >= 12) { log(state, 'No item is ready to swap.'); return [] }
  state.hero.inventory.push(id)
  state.hero.lastUnequipped = undefined
  log(state, 'You stow your last unequipped item.')
  return advance(state, [event('pickup')])
}

const nearbyContainer = (state: RunState): { tile: NonNullable<ReturnType<typeof getTile>>; kind: 'crate' | 'chest'; x: number; y: number } | undefined => {
  for (const delta of Object.values(DIRECTIONS)) {
    const x = state.hero.x + delta.x
    const y = state.hero.y + delta.y
    const tile = getTile(state.floor, x, y)
    if (tile?.kind === 'crate' || tile?.kind === 'chest') return { tile, kind: tile.kind, x, y }
  }
  return undefined
}

const nearbyGate = (state: RunState): boolean => Object.values(DIRECTIONS).some(delta => getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)?.kind === 'lockedDoor')
