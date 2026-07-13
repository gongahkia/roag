import { ITEM, biomeName, shopStock } from '../content'
import { FLOOR_COUNT, type Direction, type Modal, type RunState, DIRECTIONS, inBounds } from '../types'
import { actorAt, generateFloor, getTile } from '../world'
import { advance, explode } from './combat'
import { gainXp } from './progression'
import { consume, distance, log, turnRng, type GameEvent } from './shared'
import { refreshFov } from './visibility'

export function pickUp(state: RunState): GameEvent[] {
  const item = state.floor.items.find(current => current.x === state.hero.x && current.y === state.hero.y)
  if (!item) { log(state, 'Nothing here to take.'); return [] }
  if (item.id === 'gold') { state.hero.gold += item.count; state.floor.items = state.floor.items.filter(current => current !== item); log(state, `You recover ${item.count} gold.`); return advance(state, ['pickup']) }
  if (item.id === 'key') { state.hero.keys += item.count; state.floor.items = state.floor.items.filter(current => current !== item); log(state, 'You take an iron key.'); return advance(state, ['pickup']) }
  if (state.hero.inventory.length >= 12) { log(state, 'Your pack is full.'); return [] }
  state.hero.inventory.push(item.id)
  state.floor.items = state.floor.items.filter(current => current !== item)
  log(state, `You take ${ITEM[item.id].name}.`)
  return advance(state, ['pickup'])
}

export function operate(state: RunState): GameEvent[] {
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  const friend = state.floor.actors.find(actor => !actor.hostile && distance(actor, state.hero) <= 1)
  const container = nearbyContainer(state)
  if (friend?.role === 'merchant') { state.modal = { kind: 'shop', merchantId: friend.id }; return ['menu'] }
  if (container) {
    container.tile.kind = 'floor'
    const loot = turnRng(state, 'loot', `container:${container.x},${container.y}`).pick(['tonic', 'focusTonic', 'bombPack', 'ropeBundle', 'rock', 'mapScroll', 'ward'])
    state.hero.gold += container.kind === 'chest' ? 60 : 18
    if (state.hero.inventory.length < 12) state.hero.inventory.push(loot)
    else state.floor.items.push({ id: loot, x: container.x, y: container.y, count: 1 })
    log(state, `You open the ${container.kind} and find ${ITEM[loot].name}.`)
    return advance(state, ['pickup'])
  }
  if (tile?.kind === 'rescue' || friend?.kind === 'ally') {
    state.hero.maxHealth += 2
    state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 8)
    state.hero.gold += 35
    state.floor.actors = state.floor.actors.filter(actor => actor !== friend)
    const eventTile = friend ? getTile(state.floor, friend.x, friend.y) : tile
    if (eventTile?.kind === 'rescue' || eventTile?.kind === 'altar') eventTile.kind = 'floor'
    log(state, 'The scout shares supplies and leaves.')
    return advance(state, ['pickup'])
  }
  if (tile?.kind === 'altar') {
    if (state.hero.gold < 75) { log(state, 'The altar asks for 75 gold.'); return [] }
    state.hero.gold -= 75
    gainXp(state, 35)
    log(state, 'The altar grants insight.')
    return advance(state, ['spell'])
  }
  log(state, 'Nothing answers.')
  return []
}

export function descend(state: RunState): GameEvent[] {
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  if (tile?.kind !== 'exit') { log(state, 'You are not at the exit.'); return [] }
  if (!state.floor.guardianDefeated) { log(state, 'A guardian still seals the route.'); return [] }
  if (state.floor.index === FLOOR_COUNT - 1) { state.status = 'victory'; state.modal = undefined; log(state, 'The Ash Regent is defeated. You return with the dawn.'); return ['win'] }
  state.floor = generateFloor(state.seed, state.floor.index + 1)
  state.hero.x = state.floor.start.x
  state.hero.y = state.floor.start.y
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 4)
  state.hero.focus = state.hero.maxFocus
  log(state, `You descend into the ${biomeName[state.floor.biome]}.`)
  refreshFov(state)
  return ['floor']
}

export function inventoryChoice(state: RunState, modal: Extract<Modal, { kind: 'inventory' }>, command: string): GameEvent[] {
  const index = Number(command) - 1
  if (!Number.isInteger(index) || index < 0 || index >= state.hero.inventory.length) return []
  const id = state.hero.inventory[index]
  state.modal = undefined
  if (modal.mode === 'use') return useItem(state, id, index)
  if (modal.mode === 'drop') {
    state.hero.inventory.splice(index, 1)
    state.floor.items.push({ id, x: state.hero.x, y: state.hero.y, count: 1 })
    log(state, `You drop ${ITEM[id].name}.`)
    return advance(state, ['pickup'])
  }
  if (modal.mode === 'throw') { state.modal = { kind: 'target', action: 'throw', item: id }; return ['menu'] }
  return equip(state, id, index)
}

export function useRope(state: RunState): GameEvent[] {
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
  return advance(state, ['pickup'])
}

export function castFirstSpell(state: RunState): GameEvent[] {
  const id = state.hero.inventory.find(item => ITEM[item].use === 'spell')
  if (!id) { log(state, 'You know no ready script.'); return [] }
  state.modal = { kind: 'target', action: 'spell', item: id }
  return ['menu']
}

export function quickCast(state: RunState, direction: Direction): GameEvent[] {
  const id = state.hero.inventory.find(item => ITEM[item].use === 'spell')
  if (!id) { log(state, 'You know no ready script.'); return [] }
  return castSpell(state, id, direction)
}

export function bomb(state: RunState, direction: Direction): GameEvent[] {
  if (state.hero.bombs < 1) { log(state, 'No bombs remain.'); return [] }
  state.hero.bombs--
  const delta = DIRECTIONS[direction]
  explode(state, state.hero.x + delta.x, state.hero.y + delta.y, 12)
  return advance(state, ['boom'])
}

export function throwItem(state: RunState, id: string, direction: Direction): GameEvent[] {
  const index = state.hero.inventory.indexOf(id)
  if (index === -1) return []
  state.hero.inventory.splice(index, 1)
  const delta = DIRECTIONS[direction]
  let point = { x: state.hero.x, y: state.hero.y }
  for (let i = 0; i < 5; i++) {
    const next = { x: point.x + delta.x, y: point.y + delta.y }
    if (!inBounds(next.x, next.y) || getTile(state.floor, next.x, next.y)?.kind === 'wall') break
    point = next
    const target = actorAt(state.floor, point.x, point.y)
    if (target?.hostile) { target.health -= 3 + state.hero.stats.strength; log(state, `${ITEM[id].name} hits ${target.name}.`); break }
  }
  if (id === 'fireJar') explode(state, point.x, point.y, 5)
  else state.floor.items.push({ id, x: point.x, y: point.y, count: 1 })
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  return advance(state, [id === 'fireJar' ? 'boom' : 'hit'])
}

export function castSpell(state: RunState, id: string, direction: Direction): GameEvent[] {
  const item = ITEM[id]
  if (state.hero.focus < 3) { log(state, 'You lack focus.'); return [] }
  state.hero.focus -= 3
  const delta = DIRECTIONS[direction]
  const target = actorAt(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)
  if (item.spell === 'ember') { if (target) target.health -= 8 + state.hero.stats.intellect; else getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)!.kind = 'fireVent' }
  if (item.spell === 'mend') state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 7 + state.hero.stats.intellect)
  if (item.spell === 'sight') for (const tile of state.floor.tiles) tile.explored = true
  if (item.spell === 'gust' && target) { target.x += delta.x; target.y += delta.y }
  if (item.spell === 'ward') state.hero.maxHealth += 2
  if (item.spell === 'gate') { state.hero.x = state.floor.exit.x; state.hero.y = state.floor.exit.y }
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  refreshFov(state)
  log(state, `${item.name} takes effect.`)
  return advance(state, ['spell'])
}

export function shopChoice(state: RunState, command: string): GameEvent[] {
  const id = shopStock(state.floor.biome)[Number(command) - 1]
  if (!id) return []
  const item = ITEM[id]
  if (state.hero.gold < item.value) { log(state, 'Not enough gold.'); return ['menu'] }
  if (state.hero.inventory.length >= 12) { log(state, 'Your pack is full.'); return ['menu'] }
  state.hero.gold -= item.value
  state.hero.inventory.push(id)
  log(state, `You buy ${item.name}.`)
  return advance(state, ['pickup'])
}

function useItem(state: RunState, id: string, inventoryIndex: number): GameEvent[] {
  const item = ITEM[id]
  if (item.slot) return equip(state, id, inventoryIndex)
  if (item.use === 'heal') { state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 10); consume(state, inventoryIndex); log(state, 'Warmth returns to your limbs.'); return advance(state, ['spell']) }
  if (item.use === 'focus') { state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 8); consume(state, inventoryIndex); log(state, 'Your mind sharpens.'); return advance(state, ['spell']) }
  if (item.use === 'map') { for (const tile of state.floor.tiles) tile.explored = true; consume(state, inventoryIndex); log(state, 'The floor map unfolds in your mind.'); return advance(state, ['spell']) }
  if (item.use === 'teleport') {
    const choices = state.floor.tiles.flatMap((tile, i) => tile.kind === 'floor' && tile.explored ? [{ x: i % 48, y: Math.floor(i / 48) }] : [])
    if (choices.length) { const target = turnRng(state, 'combat', 'blink').pick(choices); state.hero.x = target.x; state.hero.y = target.y }
    consume(state, inventoryIndex); refreshFov(state); log(state, 'Space folds.'); return advance(state, ['spell'])
  }
  if (item.use === 'bomb') { state.hero.bombs += 3; consume(state, inventoryIndex); log(state, 'You gain three bombs.'); return advance(state, ['pickup']) }
  if (item.use === 'rope') { state.hero.ropes += 3; consume(state, inventoryIndex); log(state, 'You gain three ropes.'); return advance(state, ['pickup']) }
  if (item.use === 'key') { state.hero.keys++; consume(state, inventoryIndex); return advance(state, ['pickup']) }
  if (item.use === 'spell') { state.modal = { kind: 'target', action: 'spell', item: id }; return ['menu'] }
  log(state, 'That cannot be used here.')
  return []
}

function equip(state: RunState, id: string, index: number): GameEvent[] {
  const item = ITEM[id]
  if (!item.slot) { log(state, 'That cannot be equipped.'); return [] }
  const previous = state.hero.equipment[item.slot]
  state.hero.inventory.splice(index, 1)
  if (previous) { state.hero.inventory.push(previous); state.hero.lastUnequipped = previous }
  state.hero.equipment[item.slot] = id
  log(state, `You equip ${item.name}.`)
  return advance(state, ['pickup'])
}

export function swap(state: RunState): GameEvent[] {
  const id = state.hero.lastUnequipped
  if (!id || state.hero.inventory.length >= 12) { log(state, 'No item is ready to swap.'); return [] }
  state.hero.inventory.push(id)
  state.hero.lastUnequipped = undefined
  log(state, 'You stow your last unequipped item.')
  return advance(state, ['pickup'])
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
