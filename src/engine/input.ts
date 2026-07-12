import type { Direction, RunState } from '../types'
import { moveHero } from './combat'
import { bomb, castFirstSpell, castSpell, descend, inventoryChoice, operate, pickUp, quickCast, shopChoice, swap, throwItem, useRope } from './inventory'
import { chooseSkill } from './progression'
import type { GameEvent } from './shared'

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

function performModal(state: RunState, command: string): GameEvent[] {
  const modal = state.modal!
  if (command === 'Escape' || command === '`') { state.modal = undefined; return ['menu'] }
  if (modal.kind === 'help') { state.modal = undefined; return ['menu'] }
  if (modal.kind === 'inventory') return inventoryChoice(state, modal, command)
  if (modal.kind === 'skills') return chooseSkill(state, command) ? ['spell'] : []
  if (modal.kind === 'shop') return shopChoice(state, command)
  const direction = directionFor(command)
  if (!direction || direction === 'wait') return []
  state.modal = undefined
  if (modal.action === 'bomb') return bomb(state, direction)
  if (modal.action === 'throw' && modal.item) return throwItem(state, modal.item, direction)
  if (modal.action === 'spell' && modal.item) return castSpell(state, modal.item, direction)
  return []
}

export { quickCast }
