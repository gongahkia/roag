import { monsterById, monsterRoleFor } from '../content'
import type { Actor, MonsterRole } from '../types'

export interface EnemyRoleProfile { role: MonsterRole; actionId?: string; minimumRange?: number; maximumRange?: number; requiresLine?: boolean; counterplay: string }

const profiles: Record<MonsterRole, EnemyRoleProfile> = {
  guard: { role: 'guard', counterplay: 'draw it away from guarded ground' },
  skirmisher: { role: 'skirmisher', actionId: 'enemy-shot', minimumRange: 2, maximumRange: 5, requiresLine: true, counterplay: 'close distance or break line of sight' },
  artillery: { role: 'artillery', actionId: 'enemy-shot', minimumRange: 2, maximumRange: 7, requiresLine: true, counterplay: 'break line of sight or close distance' },
  controller: { role: 'controller', actionId: 'enemy-root', minimumRange: 2, maximumRange: 5, requiresLine: true, counterplay: 'move before the marked control effect resolves' },
  ambusher: { role: 'ambusher', actionId: 'enemy-web', minimumRange: 2, maximumRange: 4, requiresLine: true, counterplay: 'keep distance and clear the marked tile' },
  pursuer: { role: 'pursuer', counterplay: 'use terrain or reach to control approach' },
  support: { role: 'support', actionId: 'enemy-ward', counterplay: 'interrupt support before it can fortify' },
  scavenger: { role: 'scavenger', counterplay: 'deny isolated routes and finish quickly' },
  apex: { role: 'apex', counterplay: 'read phase telegraphs and preserve an escape route' }
}

export const roleProfileFor = (actor: Actor): EnemyRoleProfile => {
  const definition = monsterById(actor.kind)
  const role = actor.combatRole ?? (definition ? monsterRoleFor(definition) : actor.role === 'guardian' ? 'apex' : actor.ai === 'ranged' ? 'artillery' : actor.ai === 'wander' ? 'scavenger' : actor.speed >= 120 ? 'skirmisher' : 'pursuer')
  return profiles[role]
}
export const roleIntentFor = (profile: EnemyRoleProfile, range: number, hasLine: boolean): { actionId: string; reason: string } | undefined => {
  if (!profile.actionId || (profile.minimumRange !== undefined && range < profile.minimumRange) || (profile.maximumRange !== undefined && range > profile.maximumRange) || (profile.requiresLine && !hasLine)) return undefined
  return { actionId: profile.actionId, reason: `${profile.role} priority` }
}
