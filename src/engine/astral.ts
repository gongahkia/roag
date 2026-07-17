import type { Point, RunState } from '../types'
import { actorAt, isPassable } from '../world'
import { addCondition } from './conditions'
import { resolveDisplacement } from './displacement'
import { intellectWardBonus } from './intellect'
import { log } from './shared'
import { applyPropEffects, moveCartByForce } from './props'

type AstralSpell = 'sight' | 'blink' | 'gust' | 'pull' | 'ward' | 'gate'

const spells: readonly AstralSpell[] = ['sight', 'blink', 'gust', 'pull', 'ward', 'gate']

export const isAstralSpell = (spell: string | undefined): spell is AstralSpell => Boolean(spell && spells.includes(spell as AstralSpell))

export const castAstral = (state: RunState, spell: AstralSpell, point: Point): void => {
  if (spell === 'sight') for (const tile of state.floor.tiles) tile.explored = true
  if (spell === 'blink' && isPassable(state.floor, point.x, point.y)) { state.hero.x = point.x; state.hero.y = point.y }
  const target = actorAt(state.floor, point.x, point.y)
  if (spell === 'gust' && target?.hostile) resolveDisplacement(state, state.hero, target, 'push')
  if (spell === 'pull' && target?.hostile) resolveDisplacement(state, state.hero, target, 'pull')
  if (spell === 'gust' || spell === 'pull') {
    const cartEvents = moveCartByForce(state, point, spell === 'pull')
    if (!cartEvents) applyPropEffects(state, [point], ['force'])
  }
  if (spell === 'ward') { const potency = 1 + intellectWardBonus(state.hero); state.hero.maxHealth += 2 + intellectWardBonus(state.hero); addCondition(state.hero, { kind: 'shielded', duration: 3, potency }) }
  if (spell === 'gate') { state.hero.x = state.floor.exit.x; state.hero.y = state.floor.exit.y }
  if (spell === 'blink' && (state.hero.x !== point.x || state.hero.y !== point.y)) log(state, 'The blink has no safe destination.')
}
