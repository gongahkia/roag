import { rngFor } from '../rng'
import type { Actor, FloorMilestone, RelicId, RunState } from '../types'
import { addCondition, hasCondition } from './conditions'
import { log } from './shared'

export interface Relic { id: RelicId; name: string; glyph: string; text: string; priority: number }

export const RELICS: readonly Relic[] = [
  { id: 'ashCircuit', name: 'Ash Circuit', glyph: 'ϟ', text: 'Tool use arms your next Charm: -1 focus, then +2 focus.', priority: 28 },
  { id: 'markbreakerSeal', name: 'Markbreaker Seal', glyph: '◇', text: 'First hit marks; hit a marked foe to consume it for +5 damage.', priority: 32 },
  { id: 'cairnCoil', name: 'Cairn Coil', glyph: '◌', text: 'Move first, then kill: restore 3 focus.', priority: 25 },
  { id: 'tideFetter', name: 'Tide Fetter', glyph: '⚓', text: 'Cross water/current, then your next tool restores 3 HP.', priority: 22 }
]

const byId = Object.fromEntries(RELICS.map(relic => [relic.id, relic])) as Record<RelicId, Relic>
export const relicFor = (id: RelicId): Relic => byId[id]
export const hasRelic = (state: RunState, id: RelicId): boolean => Boolean(state.hero.relics?.includes(id))
export const relicChoices = (state: RunState, milestone: FloorMilestone): Relic[] => {
  const owned = new Set(state.hero.relics ?? [])
  return rngFor(state.seed, 'progression', state.floor.index, milestone.id, 'relic').shuffle(RELICS.filter(relic => !owned.has(relic.id))).slice(0, 3)
}

const arm = (state: RunState, id: RelicId): void => { if (hasRelic(state, id)) (state.hero.relicCharges ??= {})[id] = 1 }
const consume = (state: RunState, id: RelicId): boolean => {
  if (!state.hero.relicCharges?.[id]) return false
  delete state.hero.relicCharges[id]
  return true
}

export const armRelicMove = (state: RunState): void => arm(state, 'cairnCoil')
export const armRelicTraversal = (state: RunState): void => arm(state, 'ashCircuit')
export const armRelicWaterCrossing = (state: RunState): void => arm(state, 'tideFetter')
export const consumeRelicSpell = (state: RunState): boolean => consume(state, 'ashCircuit')
export const consumeRelicTool = (state: RunState): boolean => consume(state, 'tideFetter')

export const markbreakerDamage = (state: RunState, target: Actor): number => {
  if (!hasRelic(state, 'markbreakerSeal')) return 0
  if (hasCondition(target, 'marked')) {
    target.conditions = target.conditions?.filter(condition => condition.kind !== 'marked')
    log(state, 'Markbreaker Seal ruptures the mark.')
    return 5
  }
  addCondition(target, { kind: 'marked', duration: 2, potency: 1 })
  log(state, 'Markbreaker Seal marks the target.')
  return 0
}

export const resolveKillRelics = (state: RunState): void => {
  if (!consume(state, 'cairnCoil')) return
  state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 3)
  log(state, 'Cairn Coil returns focus for the moving kill.')
}
