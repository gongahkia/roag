import type { Actor, RunState } from '../types'
import { DIRECTIONS } from '../types'
import { getTile } from '../world'
import { actionById, type ActionDefinition } from './actions'
import { canAffect } from './line-effect'
import { distance } from './shared'
import { guardianPhaseFor } from './guardians'
import { hasLight } from './visibility'
import { hasCondition } from './conditions'

export type EnemyPhase = 'opening' | 'wounded' | 'desperate'
export interface EnemyIntent { action: ActionDefinition; phase: EnemyPhase; reason: string }

const action = (id: string): ActionDefinition => {
  const definition = actionById(id)
  if (!definition) throw new Error(`missing enemy action: ${id}`)
  return definition
}

const phaseFor = (actor: Actor): EnemyPhase => actor.health * 3 <= actor.maxHealth ? 'desperate' : actor.health * 3 <= actor.maxHealth * 2 ? 'wounded' : 'opening'
const hazardous = (kind: string | undefined): boolean => kind === 'lava' || kind === 'fireVent' || kind === 'gas' || kind === 'spikes' || kind === 'dart'
const hasAdjacentDoor = (state: RunState, actor: Actor): boolean => Object.values(DIRECTIONS).some(delta => getTile(state.floor, actor.x + delta.x, actor.y + delta.y)?.kind === 'door')

export const planEnemyIntent = (state: RunState, actor: Actor): EnemyIntent => {
  const range = distance(actor, state.hero)
  const phase = phaseFor(actor)
  const terrain = getTile(state.floor, actor.x, actor.y)?.kind
  if (hazardous(terrain) && range > 1 && actor.kind !== 'fumeeel') return { action: action('enemy-reposition'), phase, reason: `escaping ${terrain}` }
  if (actor.kind === 'foreman' && guardianPhaseFor(actor) !== 'opening' && range <= 5 && canAffect(state.floor, actor, state.hero)) return { action: action('foreman-cavein'), phase, reason: `cave-in at range ${range}` }
  if (actor.kind === 'heartwood' && guardianPhaseFor(actor) !== 'opening' && range >= 2 && range <= 5 && canAffect(state.floor, actor, state.hero)) return { action: action('heartwood-charge'), phase, reason: `bramble charge at range ${range}` }
  if (actor.role === 'guardian' && guardianPhaseFor(actor) === 'cataclysm' && range <= 2) return { action: action('guardian-slam'), phase, reason: 'cataclysm arena pressure' }
  if (range <= 1) return { action: action('enemy-strike'), phase, reason: 'adjacent target' }
  if (actor.kind === 'wardacolyte' && !hasCondition(actor, 'shielded')) return { action: action('enemy-ward'), phase, reason: 'raising a ward' }
  if (actor.kind === 'lockkeeper' && hasAdjacentDoor(state, actor)) return { action: action('enemy-lock'), phase, reason: 'sealing a nearby door' }
  if (actor.kind === 'vinebinder' && range <= 4 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-root'), phase, reason: `root line at range ${range}` }
  if (actor.kind === 'webweaver' && range <= 6 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-web'), phase, reason: `snare line at range ${range}` }
  if (actor.kind === 'cinderimp' && range <= 5 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-fire'), phase, reason: `fire line at range ${range}` }
  if (actor.kind === 'crystalpuller' && range <= 5 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-pull'), phase, reason: `pull line at range ${range}` }
  if (actor.kind === 'dartadept' && range <= 6 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-dart'), phase, reason: `dart line at range ${range}` }
  if (actor.kind === 'ritualist' && range <= 5 && canAffect(state.floor, actor, state.hero)) return { action: action('enemy-ritual'), phase, reason: `marking ritual at range ${range}` }
  if (actor.kind === 'gloomseer' && hasLight(state)) return { action: action('enemy-reposition'), phase, reason: 'repelled by light' }
  if (actor.ai === 'ranged' && range <= 7 && (actor.kind === 'fusewarden' || canAffect(state.floor, actor, state.hero))) return { action: action('enemy-shot'), phase, reason: actor.kind === 'fusewarden' ? `fuse line at range ${range}` : `clear line at range ${range}` }
  return { action: action('enemy-approach'), phase, reason: `closing range ${range}` }
}
