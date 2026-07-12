import { ITEM, MONSTERS, SKILLS, biomeName, shopStock } from './content'
import { mixSeed, Rng } from './rng'
import { DIRECTIONS, FLOOR_COUNT, type Actor, type Direction, type Hero, type Modal, type RunState, type StatName, inBounds } from './types'
import { actorAt, generateFloor, getTile, isPassable } from './world'

export type GameEvent = 'move' | 'hit' | 'hurt' | 'pickup' | 'spell' | 'boom' | 'danger' | 'menu' | 'death' | 'win' | 'floor'

export const newHero = (): Hero => ({
  x: 0, y: 0, health: 22, maxHealth: 22, focus: 8, maxFocus: 8, gold: 0, bombs: 4, ropes: 4, keys: 0, xp: 0, level: 1,
  stats: { strength: 2, agility: 2, vitality: 2, intellect: 2 }, skills: [], inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'], equipment: { mainHand: 'whip' }
})

export function newRun(seed = Math.floor(Math.random() * 0x7fffffff)): RunState {
  const floor = generateFloor(seed, 0)
  const hero = newHero()
  hero.x = floor.start.x
  hero.y = floor.start.y
  const state: RunState = { version: 2, seed, floor, hero, messages: ['You enter the Shale Mine.', 'H opens help.'], status: 'playing', turn: 0 }
  refreshFov(state)
  return state
}

export function perform(state: RunState, command: string): GameEvent[] {
  if (state.status !== 'playing') return []
  if (state.modal) return performModal(state, command)
  const lower = command.toLowerCase()
  if (lower === 'h') { state.modal = { kind: 'help' }; return ['menu'] }
  if (lower === 'u') { state.modal = { kind: 'inventory', mode: 'use' }; return ['menu'] }
  if (lower === 'd') { state.modal = { kind: 'inventory', mode: 'drop' }; return ['menu'] }
  if (lower === 't') { state.modal = { kind: 'inventory', mode: 'throw' }; return ['menu'] }
  if (lower === 'e') { state.modal = { kind: 'inventory', mode: 'equip' }; return ['menu'] }
  if (lower === 'a') { state.modal = { kind: 'skills' }; return ['menu'] }
  if (lower === 'b') { state.modal = { kind: 'target', action: 'bomb' }; return ['menu'] }
  if (lower === 'r') return useRope(state)
  if (lower === 'g') return pickUp(state)
  if (lower === 'c') return operate(state)
  if (lower === 'q') return descend(state)
  if (lower === 'x') return swap(state)
  if (lower === 's') return castFirstSpell(state)
  const direction = directionFor(command)
  if (direction) return moveHero(state, direction)
  return []
}

function performModal(state: RunState, command: string): GameEvent[] {
  const modal = state.modal!
  if (command === 'Escape' || command === '`') { state.modal = undefined; return ['menu'] }
  if (modal.kind === 'help') { state.modal = undefined; return ['menu'] }
  if (modal.kind === 'inventory') return inventoryChoice(state, modal, command)
  if (modal.kind === 'skills') return skillChoice(state, command)
  if (modal.kind === 'shop') return shopChoice(state, command)
  if (modal.kind === 'target') {
    const direction = directionFor(command)
    if (!direction || direction === 'wait') return []
    state.modal = undefined
    if (modal.action === 'bomb') return bomb(state, direction)
    if (modal.action === 'throw' && modal.item) return throwItem(state, modal.item, direction)
    if (modal.action === 'spell' && modal.item) return castSpell(state, modal.item, direction)
  }
  return []
}

function directionFor(command: string): Direction | undefined {
  const key = command.toLowerCase()
  const keys: Record<string, Direction> = {
    i: 'nw', o: 'n', p: 'ne', k: 'w', ';': 'e', ',': 'sw', '.': 's', '/': 'se',
    ArrowUp: 'n', ArrowDown: 's', ArrowLeft: 'w', ArrowRight: 'e',
    Numpad7: 'nw', Numpad8: 'n', Numpad9: 'ne', Numpad4: 'w', Numpad5: 'wait', Numpad6: 'e', Numpad1: 'sw', Numpad2: 's', Numpad3: 'se', l: 'wait', Enter: 'wait'
  }
  return keys[command] ?? keys[key]
}

function moveHero(state: RunState, direction: Direction): GameEvent[] {
  const delta = DIRECTIONS[direction]
  if (direction === 'wait') return advance(state, ['move'])
  const x = state.hero.x + delta.x
  const y = state.hero.y + delta.y
  const target = actorAt(state.floor, x, y)
  if (target?.hostile) return heroAttack(state, target)
  const tile = getTile(state.floor, x, y)
  if (!tile) return []
  if (tile.kind === 'door') { tile.kind = 'floor'; log(state, 'You open the door.'); return advance(state, ['move']) }
  if (tile.kind === 'lockedDoor') {
    if (state.hero.keys < 1) { log(state, 'A key is required.'); return [] }
    state.hero.keys--
    if (tile) tile.kind = 'floor'
    log(state, 'You unlock the door.')
    return advance(state, ['move'])
  }
  if (!isPassable(state.floor, x, y)) { log(state, 'The way is blocked.'); return [] }
  state.hero.x = x
  state.hero.y = y
  const events: GameEvent[] = ['move']
  if (tile.kind === 'spikes' || tile.kind === 'dart' || tile.kind === 'fireVent') events.push(...damageHero(state, tile.kind === 'spikes' ? 3 : 4, 'a trap'))
  if (tile.kind === 'lava') events.push(...damageHero(state, 8, 'lava'))
  if (tile.kind === 'gas') events.push(...damageHero(state, 2, 'poison gas'))
  if (tile.kind === 'crumble') { tile.kind = 'pit'; log(state, 'The floor crumbles into a pit.'); events.push('danger') }
  if (tile.kind === 'boulder') { tile.kind = 'floor'; events.push(...damageHero(state, 6, 'a rolling boulder')) }
  return advance(state, events)
}

function heroAttack(state: RunState, target: Actor): GameEvent[] {
  const weapon = state.hero.equipment.mainHand ? ITEM[state.hero.equipment.mainHand] : undefined
  const rng = turnRng(state)
  const roll = rng.int(1, 20) + state.hero.stats.strength + state.hero.level
  if (roll < target.defense) {
    log(state, `Your ${weapon?.name ?? 'fists'} miss ${target.name}.`)
    return advance(state, ['hit'])
  }
  const damage = Math.max(1, (weapon?.damage ?? 2) + state.hero.stats.strength + rng.int(0, 3) - Math.floor(target.defense / 8))
  target.health -= damage
  log(state, `You strike ${target.name} for ${damage}.`)
  if (target.health <= 0) {
    log(state, `${target.name} falls.`)
    dropLoot(state, target, rng)
    state.floor.actors = state.floor.actors.filter(actor => actor !== target)
    if (target.role === 'guardian') { state.floor.guardianDefeated = true; log(state, 'The way to the exit is open.'); }
    gainXp(state, monsterXp(target.kind))
  }
  return advance(state, ['hit'])
}

function advance(state: RunState, events: GameEvent[]): GameEvent[] {
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

function actorTurn(state: RunState, actor: Actor): GameEvent[] {
  const range = Math.max(Math.abs(actor.x - state.hero.x), Math.abs(actor.y - state.hero.y))
  if (range <= 1) return monsterAttack(state, actor)
  if (actor.ai === 'ranged' && range <= 7 && hasLine(state, { x: actor.x, y: actor.y }, state.hero)) return monsterAttack(state, actor, 1)
  if (range > 10 && actor.ai !== 'guardian') return []
  const candidates = Object.values(DIRECTIONS).filter(delta => delta.x || delta.y).map(delta => ({ x: actor.x + delta.x, y: actor.y + delta.y }))
  const valid = candidates.filter(point => isPassable(state.floor, point.x, point.y) && !(point.x === state.hero.x && point.y === state.hero.y))
  if (!valid.length) return []
  valid.sort((a, b) => dist(a, state.hero) - dist(b, state.hero))
  const next = actor.ai === 'wander' && turnRng(state).chance(45) ? turnRng(state).pick(valid) : valid[0]
  actor.x = next.x
  actor.y = next.y
  return []
}

function monsterAttack(state: RunState, actor: Actor, ranged = 0): GameEvent[] {
  const rng = turnRng(state)
  const roll = rng.int(1, 20) + actor.attack
  const dodge = 10 + state.hero.stats.agility + equipmentDefense(state.hero)
  if (roll < dodge) { log(state, `${actor.name} misses.`); return ['hurt'] }
  const damage = Math.max(1, actor.attack + ranged + rng.int(0, 3) - Math.floor(state.hero.stats.vitality / 2) - equipmentDefense(state.hero))
  return damageHero(state, damage, actor.name)
}

function damageHero(state: RunState, amount: number, source: string): GameEvent[] {
  state.hero.health -= amount
  log(state, `${source} harms you for ${amount}.`)
  if (state.hero.health <= 0) {
    state.hero.health = 0
    state.status = 'dead'
    state.modal = undefined
    log(state, 'Your expedition ends here.')
    return ['death']
  }
  return ['hurt']
}

function tickEnvironment(state: RunState, events: GameEvent[]): void {
  for (const actor of state.floor.actors) {
    const tile = getTile(state.floor, actor.x, actor.y)
    if (!tile || !actor.hostile) continue
    if (tile.kind === 'lava') actor.health -= 4
    if (tile.kind === 'fireVent' && turnRng(state).chance(25)) actor.health -= 3
  }
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  if (tile?.kind === 'fireVent' && turnRng(state).chance(20)) events.push(...damageHero(state, 3, 'a fire vent'))
  if (state.turn % 8 === 0 && state.hero.focus < state.hero.maxFocus) state.hero.focus++
}

function pickUp(state: RunState): GameEvent[] {
  const item = state.floor.items.find(current => current.x === state.hero.x && current.y === state.hero.y)
  if (!item) { log(state, 'Nothing here to take.'); return [] }
  if (item.id === 'gold') {
    state.hero.gold += item.count
    state.floor.items = state.floor.items.filter(current => current !== item)
    log(state, `You recover ${item.count} gold.`)
    return advance(state, ['pickup'])
  }
  if (item.id === 'key') { state.hero.keys += item.count; state.floor.items = state.floor.items.filter(current => current !== item); log(state, 'You take an iron key.'); return advance(state, ['pickup']) }
  if (state.hero.inventory.length >= 12) { log(state, 'Your pack is full.'); return [] }
  state.hero.inventory.push(item.id)
  state.floor.items = state.floor.items.filter(current => current !== item)
  log(state, `You take ${ITEM[item.id].name}.`)
  return advance(state, ['pickup'])
}

function operate(state: RunState): GameEvent[] {
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  const friend = state.floor.actors.find(actor => !actor.hostile && dist(actor, state.hero) <= 1)
  const container = nearbyContainer(state)
  if (friend?.role === 'merchant') { state.modal = { kind: 'shop', merchantId: friend.id }; return ['menu'] }
  if (container) {
    container.tile.kind = 'floor'
    const loot = turnRng(state).pick(['tonic', 'focusTonic', 'bombPack', 'ropeBundle', 'rock', 'mapScroll', 'ward'])
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
    log(state, 'The scout shares supplies and leaves.');
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

function descend(state: RunState): GameEvent[] {
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  if (tile?.kind !== 'exit') { log(state, 'You are not at the exit.'); return [] }
  if (!state.floor.guardianDefeated) { log(state, 'A guardian still seals the route.'); return [] }
  if (state.floor.index === FLOOR_COUNT - 1) {
    state.status = 'victory'
    state.modal = undefined
    log(state, 'The Ash Regent is defeated. You return with the dawn.')
    return ['win']
  }
  state.floor = generateFloor(state.seed, state.floor.index + 1)
  state.hero.x = state.floor.start.x
  state.hero.y = state.floor.start.y
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 4)
  state.hero.focus = state.hero.maxFocus
  log(state, `You descend into the ${biomeName[state.floor.biome]}.`)
  refreshFov(state)
  return ['floor']
}

function inventoryChoice(state: RunState, modal: Extract<Modal, { kind: 'inventory' }>, command: string): GameEvent[] {
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

function useItem(state: RunState, id: string, inventoryIndex: number): GameEvent[] {
  const item = ITEM[id]
  if (item.slot) return equip(state, id, inventoryIndex)
  if (item.use === 'heal') { state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 10); consume(state, inventoryIndex); log(state, 'Warmth returns to your limbs.'); return advance(state, ['spell']) }
  if (item.use === 'focus') { state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 8); consume(state, inventoryIndex); log(state, 'Your mind sharpens.'); return advance(state, ['spell']) }
  if (item.use === 'map') { for (const tile of state.floor.tiles) tile.explored = true; consume(state, inventoryIndex); log(state, 'The floor map unfolds in your mind.'); return advance(state, ['spell']) }
  if (item.use === 'teleport') {
    const choices = state.floor.tiles.flatMap((tile, i) => tile.kind === 'floor' && tile.explored ? [{ x: i % 48, y: Math.floor(i / 48) }] : [])
    if (choices.length) { const target = turnRng(state).pick(choices); state.hero.x = target.x; state.hero.y = target.y }
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

function swap(state: RunState): GameEvent[] {
  const id = state.hero.lastUnequipped
  if (!id || state.hero.inventory.length >= 12) { log(state, 'No item is ready to swap.'); return [] }
  state.hero.inventory.push(id)
  state.hero.lastUnequipped = undefined
  log(state, 'You stow your last unequipped item.')
  return advance(state, ['pickup'])
}

function throwItem(state: RunState, id: string, direction: Direction): GameEvent[] {
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

function bomb(state: RunState, direction: Direction): GameEvent[] {
  if (state.hero.bombs < 1) { log(state, 'No bombs remain.'); return [] }
  state.hero.bombs--
  const delta = DIRECTIONS[direction]
  explode(state, state.hero.x + delta.x, state.hero.y + delta.y, 12)
  return advance(state, ['boom'])
}

function explode(state: RunState, x: number, y: number, damage: number): void {
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

function useRope(state: RunState): GameEvent[] {
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

function castFirstSpell(state: RunState): GameEvent[] {
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

function castSpell(state: RunState, id: string, direction: Direction): GameEvent[] {
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

function shopChoice(state: RunState, command: string): GameEvent[] {
  const stock = shopStock(state.floor.biome)
  const index = Number(command) - 1
  if (!Number.isInteger(index) || !stock[index]) return []
  const id = stock[index]
  const item = ITEM[id]
  if (state.hero.gold < item.value) { log(state, 'Not enough gold.'); return ['menu'] }
  if (state.hero.inventory.length >= 12) { log(state, 'Your pack is full.'); return ['menu'] }
  state.hero.gold -= item.value
  state.hero.inventory.push(id)
  log(state, `You buy ${item.name}.`)
  return advance(state, ['pickup'])
}

function skillChoice(state: RunState, command: string): GameEvent[] {
  const choices = skillChoices(state)
  const choice = choices[Number(command) - 1]
  if (!choice) return []
  state.hero.skills.push(choice.id)
  state.hero.stats[choice.stat]++
  if (choice.stat === 'vitality') { state.hero.maxHealth += 2; state.hero.health += 2 }
  if (choice.stat === 'intellect') { state.hero.maxFocus += 2; state.hero.focus += 2 }
  state.modal = undefined
  log(state, `You learn ${choice.name}.`)
  return ['spell']
}

export const skillChoices = (state: RunState) => (['strength', 'agility', 'vitality', 'intellect'] as StatName[]).map(stat => SKILLS.find(skill => skill.stat === stat && !state.hero.skills.includes(skill.id))).filter((skill): skill is typeof SKILLS[number] => Boolean(skill))

function gainXp(state: RunState, amount: number): void {
  state.hero.xp += amount
  const threshold = state.hero.level * 35
  if (state.hero.xp >= threshold) {
    state.hero.level++
    state.hero.maxHealth += 1
    state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 4)
    state.modal = { kind: 'skills' }
    log(state, `Level ${state.hero.level}: choose a discipline.`)
  }
}

export function refreshFov(state: RunState): void {
  for (const tile of state.floor.tiles) tile.visible = false
  for (let y = Math.max(0, state.hero.y - 10); y <= Math.min(34, state.hero.y + 10); y++) for (let x = Math.max(0, state.hero.x - 10); x <= Math.min(47, state.hero.x + 10); x++) {
    if (hasLine(state, state.hero, { x, y })) { const tile = getTile(state.floor, x, y)!; tile.visible = true; tile.explored = true }
  }
}

function hasLine(state: RunState, from: { x: number; y: number }, to: { x: number; y: number }): boolean {
  let x = from.x
  let y = from.y
  const dx = Math.abs(to.x - from.x)
  const dy = -Math.abs(to.y - from.y)
  const sx = from.x < to.x ? 1 : -1
  const sy = from.y < to.y ? 1 : -1
  let error = dx + dy
  while (true) {
    if (x === to.x && y === to.y) return true
    if (!(x === from.x && y === from.y) && getTile(state.floor, x, y)?.kind === 'wall') return false
    const twice = 2 * error
    if (twice >= dy) { error += dy; x += sx }
    if (twice <= dx) { error += dx; y += sy }
  }
}

const equipmentDefense = (hero: Hero) => Object.values(hero.equipment).reduce((total, id) => total + (id ? ITEM[id].defense ?? 0 : 0), 0)
const monsterXp = (kind: string) => MONSTERS.find(monster => monster.id === kind)?.xp ?? 10
const consume = (state: RunState, index: number) => state.hero.inventory.splice(index, 1)
const dist = (a: { x: number; y: number }, b: { x: number; y: number }) => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))
const log = (state: RunState, message: string) => { state.messages.unshift(message); state.messages = state.messages.slice(0, 9) }
const turnRng = (state: RunState) => new Rng(mixSeed(state.seed, state.turn * 97 + state.floor.index * 13))
const dropLoot = (state: RunState, actor: Actor, rng: Rng) => {
  const gold = actor.role === 'guardian' ? rng.int(130, 210) : rng.int(5, 18)
  state.floor.items.push({ id: 'gold', x: actor.x, y: actor.y, count: gold })
  const tables: Record<string, string[]> = {
    mine: ['rock', 'tonic', 'bombPack', 'key'], wilds: ['tonic', 'ropeBundle', 'machete', 'focusTonic'], caverns: ['focusTonic', 'ember', 'mend', 'spear'], ruins: ['mapScroll', 'ward', 'wardScript', 'blinkRune']
  }
  if (actor.role === 'guardian' || rng.chance(28)) state.floor.items.push({ id: rng.pick(tables[state.floor.biome]), x: actor.x, y: actor.y, count: 1 })
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
