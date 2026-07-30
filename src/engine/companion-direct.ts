import { DIRECTIONS, type Actor, type Companion, type Direction, type Modal, type RunState } from '../types'
import { executeCompanionRoleAction } from './companion-autonomy'
import { companionAbilityTarget } from './companion-abilities'
import { companionRoleContract } from './companion-roles'
import { isCompanionActor } from './party'
import { resolveTurnAfterParty } from './combat'
import { resolvePartyMove } from './party-resolution'
import { event, log, type ActionResult } from './shared'

const companionIdForActor = (actor: Actor): string | undefined => actor.status?.find(status => status.startsWith('companion:'))?.slice('companion:'.length)
const directionFor = (command: string): Direction | undefined => ({ i: 'nw', o: 'n', p: 'ne', k: 'w', ';': 'e', ',': 'sw', '.': 's', '/': 'se', ArrowUp: 'n', ArrowDown: 's', ArrowLeft: 'w', ArrowRight: 'e', Numpad7: 'nw', Numpad8: 'n', Numpad9: 'ne', Numpad4: 'w', Numpad6: 'e', Numpad1: 'sw', Numpad2: 's', Numpad3: 'se' } as const)[command] as Direction | undefined
const current = (state: RunState, modal: Extract<Modal, { kind: 'companionCommand' }>): { companion: Companion; actor: Actor } | undefined => {
  const id = modal.companionIds[modal.index]
  const companion = state.companions?.find(candidate => candidate.id === id)
  const actor = state.floor.actors.find(candidate => isCompanionActor(candidate) && companionIdForActor(candidate) === id)
  return companion && actor ? { companion, actor } : undefined
}
const next = (state: RunState, modal: Extract<Modal, { kind: 'companionCommand' }>, companion: Companion, action: string): ActionResult => {
  const events: ActionResult = [event('companion', companion.id, action)]
  const index = modal.index + 1
  if (index < modal.companionIds.length) {
    const id = modal.companionIds[index]!
    const following = state.companions?.find(candidate => candidate.id === id)
    state.modal = { ...modal, index }
    log(state, `${following?.name ?? 'Next companion'} awaits a command.`)
    return events
  }
  state.modal = undefined
  return resolveTurnAfterParty(state, events)
}

export const directCompanionControls = (companion: Companion): string[] => companionRoleContract(companion.role).directActions.map((action, index) => `${index + 1} ${action}`)
export const performDirectCompanionCommand = (state: RunState, modal: Extract<Modal, { kind: 'companionCommand' }>, command: string): ActionResult => {
  const entry = current(state, modal)
  if (!entry) { state.modal = undefined; log(state, 'Companion command phase cancelled: roster changed.'); return resolveTurnAfterParty(state, []) }
  const { companion, actor } = entry
  if (command === 'Escape' || command === '`') { log(state, 'Companion commands cannot be cancelled; Enter waits.'); return [] }
  if (command === 'Enter' || command === ' ' || command.toLowerCase() === 'l') { log(state, `${companion.name} waits.`); return next(state, modal, companion, 'wait') }
  const direction = directionFor(command)
  if (direction && direction !== 'wait') {
    const delta = DIRECTIONS[direction]
    const target = { x: actor.x + delta.x, y: actor.y + delta.y }
    const resolution = resolvePartyMove(state, actor.id, target)
    if (!resolution.resolved) { log(state, `${companion.name}'s move is blocked (${resolution.reason}).`); return [] }
    log(state, `${companion.name} moves.`)
    return next(state, modal, companion, 'move')
  }
  const action = companionRoleContract(companion.role).directActions[Number(command) - 1]
  if (!action) { log(state, `${companion.name}: choose direction, ${directCompanionControls(companion).join(' / ')}, or Enter.`); return [] }
  if ((companion.abilityState.cooldowns[action] ?? 0) > 0) { log(state, `${companion.name}'s ${action} is recovering.`); return [] }
  const execution = executeCompanionRoleAction(state, companion, actor, action, companionAbilityTarget(state, companion, actor, action), `${action} by direct command`)
  if (!execution.executed) return []
  const target = execution.target ? `${execution.target.x},${execution.target.y}` : 'self'
  return next(state, modal, companion, `${action}:${target}:${execution.result}`)
}
