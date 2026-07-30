import type { Actor, Companion, CompanionControlMode, CompanionRole, RunState } from '../types'
import { companionActorId, companionAppearance } from './party'

export type PartyHudStatus = 'lead' | 'active' | 'benched' | 'injured' | 'recovering' | 'lost'
export interface PartyHudEntry {
  id: string
  name: string
  role: CompanionRole
  glyph: string
  color: string
  status: PartyHudStatus
  health?: { current: number; maximum: number }
  conditions: string[]
  cooldowns: Array<{ action: string; turns: number }>
  turnOrder?: number
  focused: boolean
}
export interface PartyHud {
  controlMode: CompanionControlMode
  order: string[]
  entries: PartyHudEntry[]
}

const statusFor = (companion: Companion): PartyHudStatus => companion.permanentlyLost ? 'lost' : companion.injury === 'recovering' ? 'recovering' : companion.injury === 'injured' ? 'injured' : companion.rosterStatus
const actorFor = (state: RunState, companion: Companion): Actor | undefined => state.floor.actors.find(actor => actor.id === companionActorId(companion.id) && actor.health > 0)
const conditionsFor = (actor: Actor | undefined): string[] => [
  ...(actor?.conditions ?? []).map(condition => `${condition.kind} ${condition.duration}T`),
  ...(actor?.status ?? []).filter(status => status.startsWith('intercept:')).map(() => 'intercept')
]

export const partyHud = (state: RunState): PartyHud => {
  const companions = [...(state.companions ?? [])].sort((left, right) => left.id.localeCompare(right.id))
  const activeIds = companions.filter(companion => companion.rosterStatus === 'active' && companion.injury === 'healthy' && !companion.permanentlyLost).map(companion => companion.id)
  const command = state.modal?.kind === 'companionCommand' ? state.modal : undefined
  const controlMode = companions.find(companion => activeIds.includes(companion.id))?.controlMode ?? companions.find(companion => !companion.permanentlyLost)?.controlMode ?? 'autonomous'
  return {
    controlMode,
    order: ['courier', ...activeIds],
    entries: companions.map(companion => {
      const actor = actorFor(state, companion)
      const turnOrder = activeIds.indexOf(companion.id)
      return {
        id: companion.id,
        name: companion.name,
        role: companion.role,
        ...companionAppearance(companion.role),
        status: statusFor(companion),
        health: actor ? { current: actor.health, maximum: actor.maxHealth } : undefined,
        conditions: conditionsFor(actor),
        cooldowns: Object.entries(companion.abilityState.cooldowns).filter(([, turns]) => turns > 0).sort(([left], [right]) => left.localeCompare(right)).map(([action, turns]) => ({ action, turns })),
        turnOrder: turnOrder < 0 ? undefined : turnOrder + 1,
        focused: command?.companionIds[command.index] === companion.id
      }
    })
  }
}
