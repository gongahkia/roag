import type { Point, RunState } from '../types'
import { actorAt, getTile } from '../world'
import { explode } from './combat'
import { addCondition, modifyIncomingDamage } from './conditions'
import { resolveTerrainReactions } from './terrain'
import { log } from './shared'
import { applyPropEffects } from './props'

export const castEmber = (state: RunState, point: Point, bonusDamage = 0): void => {
  const tile = getTile(state.floor, point.x, point.y)
  if (!tile) return
  const target = actorAt(state.floor, point.x, point.y)
  if (target?.hostile) {
    target.health -= modifyIncomingDamage(target, 8 + state.hero.stats.intellect + bonusDamage)
    addCondition(target, { kind: 'burning', duration: 2, potency: 2 })
  }
  const reactions = resolveTerrainReactions(state.floor, [point], ['fire'])
  applyPropEffects(state, [point], ['fire'])
  for (const reaction of reactions) log(state, `Terrain reaction: ${reaction.reaction}.`)
  if (reactions.some(reaction => reaction.reaction === 'ignited-gas' || reaction.reaction === 'detonated-volatile')) {
    explode(state, point.x, point.y, 6 + bonusDamage, ['fire'], 'the ignition')
    return
  }
  if (!reactions.length && tile.kind === 'floor') tile.kind = 'fireVent'
}
