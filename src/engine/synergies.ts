import type { RunState } from '../types'
import { log } from './shared'
import { evaluateModifiers, type ModifierEvaluation, type TagModifier, type TagQuery } from './tags'

export interface SynergyResolution extends ModifierEvaluation { synergies: string[] }

const modifiers: readonly TagModifier[] = [
  { id: 'ember-gas', requires: ['ember', 'gas'], add: { damage: 2 } },
  { id: 'strength-reach', requires: ['strength', 'reach'], add: { range: 1 } },
  { id: 'tempered-gale', requires: ['flintTemper', 'windKnot'], add: { range: 1 } },
  { id: 'hearth-thread', requires: ['barkBinding', 'spiritThread'], add: { focus: 1 } },
  { id: 'cordmark-reedstep', requires: ['cordmark', 'reedstep'], add: { range: 1 } }
]

const labels: Record<string, string> = {
  'ember-gas': 'Ember ignites the gas with extra force.',
  'strength-reach': 'Iron Grip extends your reach.',
  'tempered-gale': 'Flint Temper and Wind Knot extend your strike.',
  'hearth-thread': 'Bark Binding and Spirit Thread restore focus.',
  'cordmark-reedstep': 'Cordmark Talisman and Reedstep Boots lengthen your strike.'
}

export const resolveSynergies = (query: TagQuery | readonly string[], base: Readonly<Record<string, number>> = {}): SynergyResolution => {
  const evaluation = evaluateModifiers(query, modifiers, base)
  return { ...evaluation, synergies: evaluation.applied }
}

export const announceSynergies = (state: RunState, resolution: SynergyResolution): void => {
  for (const synergy of resolution.synergies) log(state, `Synergy: ${labels[synergy] ?? synergy}`)
}
