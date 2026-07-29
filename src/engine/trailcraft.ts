import { rngFor } from '../rng'
import type { Biome, Hero, RunState, StatName, TrailcraftId } from '../types'
import { log } from './shared'

export interface Trailcraft { id: TrailcraftId; name: string; stat: StatName; text: string; biomes?: readonly Biome[] }

export const TRAILCRAFTS: readonly Trailcraft[] = [
  { id: 'flintTemper', name: 'Flint Temper', stat: 'strength', text: 'STR +1 · pair Wind Knot for +1 strike range' },
  { id: 'windKnot', name: 'Wind Knot', stat: 'agility', text: 'AGI +1 · pair Flint Temper for +1 strike range' },
  { id: 'barkBinding', name: 'Bark Binding', stat: 'vitality', text: 'VIT +1 · pair Spirit Thread for +1 focus on cast' },
  { id: 'spiritThread', name: 'Spirit Thread', stat: 'intellect', text: 'INT +1 · pair Bark Binding for +1 focus on cast' },
  { id: 'sunstride', name: 'Sunstride', stat: 'agility', biomes: ['saltFlats'], text: 'AGI +1 · pair Prism Ledger for +1 strike range' },
  { id: 'prismLedger', name: 'Prism Ledger', stat: 'intellect', biomes: ['saltFlats'], text: 'INT +1 · pair Sunstride for +1 strike range' },
  { id: 'brineGrit', name: 'Brine Grit', stat: 'vitality', biomes: ['saltFlats'], text: 'VIT +1 · brine crossings restore 1 HP' },
  { id: 'rimeEdge', name: 'Rime Edge', stat: 'strength', biomes: ['frostReliquary'], text: 'STR +1 · pair Ice Nerve for +1 strike damage' },
  { id: 'iceNerve', name: 'Ice Nerve', stat: 'agility', biomes: ['frostReliquary'], text: 'AGI +1 · pair Rime Edge for +1 strike damage' },
  { id: 'winterVow', name: 'Winter Vow', stat: 'vitality', biomes: ['frostReliquary'], text: 'VIT +1 · first frost hazard each floor grants 1 shield' }
]

export const trailcraftChoices = (state: RunState): Trailcraft[] => {
  const shuffled = rngFor(state.seed, 'progression', 'trailcraft', state.floor.index).shuffle([...TRAILCRAFTS])
  const local = shuffled.filter(trailcraft => trailcraft.biomes?.includes(state.floor.biome))
  const global = shuffled.filter(trailcraft => !trailcraft.biomes?.includes(state.floor.biome))
  return [...local.slice(0, 2), ...global].slice(0, 3)
}
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
