import type { Actor, RunState } from '../types'
import { getTile } from '../world'
import { actionById, type ActionDefinition } from './actions'
import { canAffect } from './line-effect'
import { distance } from './shared'
import { guardianPhaseFor } from './guardians'
import { hasLight } from './visibility'

export type EnemyPhase = 'opening' | 'wounded' | 'desperate'
export interface EnemyIntent { action: ActionDefinition; phase: EnemyPhase; reason: string }

const action = (id: string): ActionDefinition => {
  const definition = actionById(id)
  if (!definition) throw new Error(`missing enemy action: ${id}`)
  return definition
}

const phaseFor = (actor: Actor): EnemyPhase => actor.health * 3 <= actor.maxHealth ? 'desperate' : actor.health * 3 <= actor.maxHealth * 2 ? 'wounded' : 'opening'
const hazardous = (kind: string | undefined): boolean => kind === 'lava' || kind === 'fireVent' || kind === 'gas' || kind === 'spikes' || kind === 'dart'

export const planEnemyIntent = (state: RunState, actor: Actor): EnemyIntent => {
  const range = distance(actor, state.hero)
  const phase = phaseFor(actor)
  const terrain = getTile(state.floor, actor.x, actor.y)?.kind
  if (hazardous(terrain) && range > 1 && actor.kind !== 'fumeeel') return { action: action('enemy-reposition'), phase, reason: `escaping ${terrain}` }
  if (actor.role === 'guardian' && guardianPhaseFor(actor) === 'cataclysm' && range <= 2) return { action: action('guardian-slam'), phase, reason: 'cataclysm arena pressure' }
  if (range <= 1) return { action: action('enemy-strike'), phase, reason: 'adjacent target' }
  if (actor.kind === 'vinebinder' && range <= 4 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-root'), phase, reason: `root line at range ${range}` }
  if (actor.kind === 'webweaver' && range <= 6 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-web'), phase, reason: `snare line at range ${range}` }
  if (actor.kind === 'cinderimp' && range <= 5 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-fire'), phase, reason: `fire line at range ${range}` }
  if (actor.kind === 'crystalpuller' && range <= 5 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-pull'), phase, reason: `pull line at range ${range}` }
  if (actor.kind === 'gloomseer' && hasLight(state)) return { action: action('enemy-reposition'), phase, reason: 'repelled by light' }
  if (actor.ai === 'ranged' && range <= 7 && (actor.kind === 'fusewarden' || canAffect(state.floor, actor, state.hero))) return { action: action('enemy-shot'), phase, reason: actor.kind === 'fusewarden' ? `fuse line at range ${range}` : `clear line at range ${range}` }
  return { action: action('enemy-approach'), phase, reason: `closing range ${range}` }
}
