import { ITEM } from '../content'
import { propDefinition } from '../props'
import { presentTelegraph } from '../telegraph-language'
import type { Prop, RunState } from '../types'
import { getTile } from '../world'
import { planEnemyIntent } from './intents'
import { distance } from './shared'
import { ecologyReadout } from '../ecology'

export interface FieldReadout { brief: string; lines: string[] }

const visibleHostiles = (state: RunState) => state.floor.actors
  .filter(actor => actor.hostile && getTile(state.floor, actor.x, actor.y)?.visible)
  .sort((first, second) => distance(first, state.hero) - distance(second, state.hero) || first.id.localeCompare(second.id))

const nearbyProps = (state: RunState): Prop[] => state.floor.props
  .filter(prop => prop.state !== 'destroyed' && distance(prop, state.hero) <= 1)
  .sort((first, second) => first.id.localeCompare(second.id))

const telegraphLabel = (state: RunState, id: string): string => {
  const telegraph = state.floor.telegraphs?.find(current => current.id === id)
  if (!telegraph) return ''
  const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)?.name ?? telegraph.sourceId
  return presentTelegraph(telegraph, state.turn, source).label
}

export const fieldReadout = (state: RunState): FieldReadout => {
  const telegraphs = [...(state.floor.telegraphs ?? [])].sort((first, second) => first.resolveTurn - second.resolveTurn || first.id.localeCompare(second.id))
  const foes = visibleHostiles(state)
  const lines = [`OBJECTIVE: ${state.floor.objective.status === 'complete' ? 'DONE — ' : ''}${state.floor.objective.label}`]
  if (state.floor.escalation) lines.push(`ARC: ${state.floor.escalation.arcId} / ${state.floor.escalation.phase} — ${state.floor.escalation.landmark}`)
  for (const ecology of (state.floor.ecology ?? []).filter(current => current.state !== 'resolved').sort((first, second) => first.startsAt - second.startsAt || first.id.localeCompare(second.id)).slice(0, 2)) lines.push(`ECOLOGY: ${ecologyReadout(ecology, state.turn)}`)
  for (const telegraph of telegraphs.slice(0, 3)) {
    lines.push(`THREAT: ${telegraphLabel(state, telegraph.id)}`)
  }
  for (const foe of foes.slice(0, 3)) {
    const intent = planEnemyIntent(state, foe)
    lines.push(`INTENT: ${foe.name} — ${intent.action.name} (${intent.reason}; ${intent.role}: ${intent.counterplay})`)
  }
  const marked = state.floor.milestones.filter(current => !current.claimed && (current.discovered || (state.hero.boons?.parcelMark ?? 0) > 0))
  const milestone = marked.sort((a, b) => distance(a, state.hero) - distance(b, state.hero))[0]
  if (milestone) {
    const vertical = milestone.y < state.hero.y ? 'north' : milestone.y > state.hero.y ? 'south' : ''
    const horizontal = milestone.x < state.hero.x ? 'west' : milestone.x > state.hero.x ? 'east' : ''
    lines.push(`MARK: ${milestone.kind === 'waycache' ? 'Waycache' : 'Boon site'} ${distance(milestone, state.hero)} tiles ${[vertical, horizontal].filter(Boolean).join('-') || 'here'}.`)
  }
  const encounter = state.floor.encounters?.find(current => current.state === 'dormant' && getTile(state.floor, current.x, current.y)?.visible)
  if (encounter) lines.push(`OPTION C: inspect ${encounter.kind === 'wayfarer' ? 'a wandering wayfarer' : encounter.kind === 'bloodBargain' ? 'a sealed bargain' : 'a shifting chamber'}`)
  const ground = state.floor.items.filter(item => item.x === state.hero.x && item.y === state.hero.y)
  for (const item of ground.slice(0, 2)) lines.push(`OPTION G: take ${ITEM[item.id]?.name ?? item.id}${item.count > 1 ? ` ×${item.count}` : ''}`)
  for (const prop of nearbyProps(state).slice(0, 2)) {
    const definition = propDefinition(prop.kind)
    lines.push(`OPTION C: ${prop.state === 'dormant' ? 'inspect' : 'work'} ${definition.name} [${definition.hooks.join(', ')}]`)
  }
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  if (tile?.kind === 'exit') lines.push(state.floor.objective.status === 'complete' && state.floor.guardianDefeated ? 'OPTION Q: descend' : `EXIT HELD: ${state.floor.objective.label}`)
  if (lines.length === 1) lines.push('OPTIONS: move, use gear, cast a charm, or wait.')
  const brief = telegraphs.length
    ? telegraphLabel(state, telegraphs[0].id)
    : foes.length ? `${foes[0].name}: ${planEnemyIntent(state, foes[0]).action.name}` : 'No visible threat.'
  return { brief, lines }
}
