import type { Direction, Modal, RunState } from '../types'
import { advance, moveHero } from './combat'
import { bomb, castFirstSpell, castSpell, descend, inventoryChoice, operate, pickUp, quickCast, shopChoice, swap, throwItem, useRope } from './inventory'
import { chooseSkill } from './progression'
import { event, log, type ActionResult } from './shared'
import { hasCondition } from './conditions'
import { gateForArea, resolveAreaGate } from './gates'

export function perform(state: RunState, command: string): ActionResult {
  if (state.status !== 'playing') return []
  if (hasCondition(state.hero, 'staggered')) { log(state, 'You are staggered.'); return advance(state, [event('danger')]) }
  if (state.modal) return performModal(state, command)
  if (command === 'settings') { state.modal = { kind: 'settings' }; return [event('menu')] }
  const lower = command.toLowerCase()
  if (lower === 'h') { state.modal = { kind: 'help' }; return [event('menu')] }
  if (lower === 'j') { state.modal = { kind: 'encyclopedia', section: 'enemies' }; return [event('menu')] }
  if (lower === 'u') { state.modal = { kind: 'inventory', mode: 'use' }; return [event('menu')] }
  if (lower === 'd') { state.modal = { kind: 'inventory', mode: 'drop' }; return [event('menu')] }
  if (lower === 't') { state.modal = { kind: 'inventory', mode: 'throw' }; return [event('menu')] }
  if (lower === 'e') { state.modal = { kind: 'inventory', mode: 'equip' }; return [event('menu')] }
  if (lower === 'a') { state.modal = { kind: 'skills' }; return [event('menu')] }
  if (lower === 'b') {
    if (state.hero.bombs < 1) { log(state, 'No bombs remain.'); return [] }
    state.modal = { kind: 'target', action: 'bomb' }
    return [event('menu')]
  }
  if (command === 'Escape') { state.modal = { kind: 'pause' }; return [event('menu')] }
  if (lower === 'r') return useRope(state)
  if (lower === 'g') return pickUp(state)
  if (lower === 'c') return operate(state)
  if (lower === 'q') return descend(state)
  if (lower === 'x') return swap(state)
  if (lower === 's') return castFirstSpell(state)
  const direction = directionFor(command)
  return direction ? moveHero(state, direction) : []
}

export function directionFor(command: string): Direction | undefined {
  const key = command.toLowerCase()
  const keys: Record<string, Direction> = {
    i: 'nw', o: 'n', p: 'ne', k: 'w', ';': 'e', ',': 'sw', '.': 's', '/': 'se', ArrowUp: 'n', ArrowDown: 's', ArrowLeft: 'w', ArrowRight: 'e',
    Numpad7: 'nw', Numpad8: 'n', Numpad9: 'ne', Numpad4: 'w', Numpad5: 'wait', Numpad6: 'e', Numpad1: 'sw', Numpad2: 's', Numpad3: 'se', l: 'wait', Enter: 'wait'
  }
  return keys[command] ?? keys[key]
}

function performModal(state: RunState, command: string): ActionResult {
  const modal = state.modal!
  if (command === 'Escape' || command === '`') { state.modal = undefined; return [event('menu')] }
  if (modal.kind === 'help') { state.modal = undefined; return [event('menu')] }
  if (modal.kind === 'settings') return []
  if (modal.kind === 'pause') {
    if (command === 'Escape' || command === '`' || command === 'Enter' || command === '1') { state.modal = undefined; return [event('menu')] }
    if (command === '2' || command.toLowerCase() === 'q') { state.modal = undefined; return [event('suspend')] }
    return []
  }
  if (modal.kind === 'encyclopedia') return performEncyclopediaModal(state, modal, command)
  if (modal.kind === 'inventory') return inventoryChoice(state, modal, command)
  if (modal.kind === 'skills') return chooseSkill(state, command) ? [event('spell')] : []
  if (modal.kind === 'shop') return shopChoice(state, command)
  if (modal.kind === 'gate') return performGateModal(state, modal, command)
  if (command === 'Enter' && modal.direction) return commitTarget(state, modal)
  const direction = directionFor(command)
  if (!direction || direction === 'wait') return []
  state.modal = { ...modal, direction }
  return [event('menu')]
}

function performEncyclopediaModal(state: RunState, modal: Extract<Modal, { kind: 'encyclopedia' }>, command: string): ActionResult {
  const sections = ['enemies', 'telegraphs', 'tags', 'gates', 'legacy'] as const
  const choice = Number(command) - 1
  if (Number.isInteger(choice) && sections[choice]) { state.modal = { kind: 'encyclopedia', section: sections[choice] }; return [event('menu')] }
  if (command === 'ArrowLeft' || command === '[') { state.modal = { ...modal, page: Math.max(0, (modal.page ?? 0) - 1) }; return [event('menu')] }
  if (command === 'ArrowRight' || command === ']') { state.modal = { ...modal, page: (modal.page ?? 0) + 1 }; return [event('menu')] }
  return []
}

function performGateModal(state: RunState, modal: Extract<Modal, { kind: 'gate' }>, command: string): ActionResult {
  const choice = Number(command) - 1
  const gate = gateForArea(state.area ?? state.floor.biome)
  if (Number.isInteger(choice) && choice >= 0 && choice < gate.tagAlternatives.length) { state.modal = { ...modal, choice, confirming: false }; return [event('menu')] }
  if (command !== 'Enter') return []
  if (modal.choice === undefined) { log(state, 'Choose a gate alternative first.'); return [] }
  if (!modal.confirming) { state.modal = { ...modal, confirming: true }; return [event('menu')] }
  const resolution = resolveAreaGate(state, gate, modal.choice)
  log(state, resolution.message)
  if (!resolution.resolved) return []
  state.modal = undefined
  return [event('gateResolved')]
}

function commitTarget(state: RunState, modal: Extract<Modal, { kind: 'target' }>): ActionResult {
  const direction = modal.direction!
  state.modal = undefined
  if (modal.action === 'bomb') return bomb(state, direction)
  if (modal.action === 'throw' && modal.item) return throwItem(state, modal.item, direction)
  if (modal.action === 'spell' && modal.item) return castSpell(state, modal.item, direction)
  return []
}

export { quickCast }
