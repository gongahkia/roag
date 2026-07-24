import { rngFor } from '../rng'
import type { Hero, RunState, StatName, TrailcraftId } from '../types'
import { log } from './shared'

export interface Trailcraft { id: TrailcraftId; name: string; stat: StatName; text: string }

export const TRAILCRAFTS: readonly Trailcraft[] = [
  { id: 'flintTemper', name: 'Flint Temper', stat: 'strength', text: 'STR +1 · pair Wind Knot for +1 strike range' },
  { id: 'windKnot', name: 'Wind Knot', stat: 'agility', text: 'AGI +1 · pair Flint Temper for +1 strike range' },
  { id: 'barkBinding', name: 'Bark Binding', stat: 'vitality', text: 'VIT +1 · pair Spirit Thread for +1 focus on cast' },
  { id: 'spiritThread', name: 'Spirit Thread', stat: 'intellect', text: 'INT +1 · pair Bark Binding for +1 focus on cast' }
]

export const trailcraftChoices = (state: RunState): Trailcraft[] => rngFor(state.seed, 'progression', 'trailcraft', state.floor.index).shuffle([...TRAILCRAFTS]).slice(0, 3)
export const trailcraftTags = (hero: Hero): TrailcraftId[] => (Object.entries(hero.trailcrafts ?? {}) as Array<[TrailcraftId, number]>).filter(([, rank]) => rank > 0).map(([id]) => id).sort()

export function chooseTrailcraft(state: RunState, command: string): boolean {
  const choice = trailcraftChoices(state)[Number(command) - 1]
  if (!choice) return false
  state.hero.stats[choice.stat]++
  state.hero.trailcrafts ??= {}
  state.hero.trailcrafts[choice.id] = (state.hero.trailcrafts[choice.id] ?? 0) + 1
  if (choice.stat === 'vitality') { state.hero.maxHealth += 2; state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 4) }
  if (choice.stat === 'intellect') { state.hero.maxFocus += 2; state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 2) }
  state.modal = undefined
  log(state, `Trailcraft: ${choice.name}.`)
  return true
}
