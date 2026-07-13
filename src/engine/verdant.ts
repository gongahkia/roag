import type { Point, RunState } from '../types'
import { actorAt, getTile } from '../world'
import { addCondition } from './conditions'
import { log } from './shared'
import { resolveTerrainReactions } from './terrain'

type VerdantSpell = 'mend' | 'sight' | 'root' | 'water' | 'lull'

const spells: readonly VerdantSpell[] = ['mend', 'sight', 'root', 'water', 'lull']

export const isVerdantSpell = (spell: string | undefined): spell is VerdantSpell => Boolean(spell && spells.includes(spell as VerdantSpell))

export const castVerdant = (state: RunState, spell: VerdantSpell, point: Point): void => {
  if (spell === 'mend') state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 7 + state.hero.stats.intellect)
  if (spell === 'sight') for (const tile of state.floor.tiles) tile.explored = true
  const target = actorAt(state.floor, point.x, point.y)
  if (spell === 'root' && target?.hostile) addCondition(target, { kind: 'rooted', duration: 3, potency: 1 })
  if (spell === 'lull' && target?.hostile) addCondition(target, { kind: 'staggered', duration: 3, potency: 1 })
  if (spell !== 'water') return
  const tile = getTile(state.floor, point.x, point.y)
  if (!tile) return
  const reactions = resolveTerrainReactions(state.floor, [point], ['water'])
  for (const reaction of reactions) log(state, `Terrain reaction: ${reaction.reaction}.`)
  if (!reactions.length && tile.kind === 'floor') tile.kind = 'water'
}
