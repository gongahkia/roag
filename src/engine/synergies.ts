import type { RunState } from '../types'
import { log } from './shared'
import { evaluateModifiers, type ModifierEvaluation, type TagModifier, type TagQuery } from './tags'

export interface SynergyResolution extends ModifierEvaluation { synergies: string[] }

const modifiers: readonly TagModifier[] = [
  { id: 'ember-gas', requires: ['ember', 'gas'], add: { damage: 2 } },
  { id: 'strength-reach', requires: ['strength', 'reach'], add: { range: 1 } },
  { id: 'tempered-gale', requires: ['flintTemper', 'windKnot'], add: { range: 1 } },
  { id: 'hearth-thread', requires: ['barkBinding', 'spiritThread'], add: { focus: 1 } },
  { id: 'sun-prism', requires: ['sunstride', 'prismLedger'], add: { range: 1 } },
  { id: 'rime-nerve', requires: ['rimeEdge', 'iceNerve'], add: { damage: 1 } },
  { id: 'cordmark-reedstep', requires: ['cordmark', 'reedstep'], add: { range: 1 } },
  { id: 'hammer-guard', requires: ['hammer', 'guard'], add: { damage: 2 } },
  { id: 'hook-lift', requires: ['hook', 'lift'], add: { range: 1 } },
  { id: 'anchor-current', requires: ['anchor', 'current'], add: { damage: 1, focus: 1 } },
  { id: 'blade-smoke', requires: ['blade', 'smoke'], add: { damage: 1 } },
  { id: 'boon-hook-line', requires: ['boon:hookLine', 'reach'], add: { damage: 1 } },
  { id: 'boon-cinder-edge', requires: ['boon:cinderEdge', 'fire'], add: { damage: 1 } },
  { id: 'boon-tide-edge', requires: ['boon:tideEdge', 'water'], add: { damage: 1 } },
  { id: 'boon-glass-nerve', requires: ['boon:glassNerve'], add: { damage: 3 } }
]

const labels: Record<string, string> = {
  'ember-gas': 'Ember ignites the gas with extra force.',
  'strength-reach': 'Iron Grip extends your reach.',
  'tempered-gale': 'Flint Temper and Wind Knot extend your strike.',
  'hearth-thread': 'Bark Binding and Spirit Thread restore focus.',
  'sun-prism': 'Sunstride and Prism Ledger extend your strike.',
  'rime-nerve': 'Rime Edge and Ice Nerve sharpen your strike.',
  'cordmark-reedstep': 'Cordmark Talisman and Reedstep Boots lengthen your strike.',
  'hammer-guard': 'Hammer and guard lock into a crushing stance.',
  'hook-lift': 'Lift Hook and chain gear lengthen your reach.',
  'anchor-current': 'Anchor gear catches the current and feeds momentum.',
  'blade-smoke': 'Smoke hides the blade’s next cut.',
  'boon-hook-line': 'Hook Line extends your moving reach.',
  'boon-cinder-edge': 'Cinder Edge heats the strike.',
  'boon-tide-edge': 'Tide Edge rides the waterline.',
  'boon-glass-nerve': 'Glass Nerve sharpens the opening blow.'
}

export const resolveSynergies = (query: TagQuery | readonly string[], base: Readonly<Record<string, number>> = {}): SynergyResolution => {
  const evaluation = evaluateModifiers(query, modifiers, base)
  return { ...evaluation, synergies: evaluation.applied }
}

export const announceSynergies = (state: RunState, resolution: SynergyResolution): void => {
  for (const synergy of resolution.synergies) log(state, `Synergy: ${labels[synergy] ?? synergy}`)
}
