import { DIRECTIONS, type Point, type Prop, type PropEffectKind, type RunState } from '../types'
import { propAt, propDefinition } from '../props'
import { log } from './shared'

const pointKey = (point: Point): string => `${point.x},${point.y}`

const reward = (state: RunState, prop: Prop, id: string): void => {
  state.floor.items.push({ id, x: prop.x, y: prop.y, count: 1, visibleInFog: true })
}

const nearbyProp = (state: RunState): Prop | undefined => {
  const points = [{ x: state.hero.x, y: state.hero.y }, ...Object.values(DIRECTIONS).filter(delta => delta.x || delta.y).map(delta => ({ x: state.hero.x + delta.x, y: state.hero.y + delta.y }))]
  return points.map(point => propAt(state.floor.props, point.x, point.y)).find((prop): prop is Prop => Boolean(prop))
}

export const operateProp = (state: RunState): boolean => {
  const prop = nearbyProp(state)
  if (!prop || prop.state !== 'dormant' || !prop.hooks?.includes('operate')) return false
  const definition = propDefinition(prop.kind)
  prop.state = 'activated'
  reward(state, prop, definition.activationReward)
  log(state, `You study the ${definition.name} and recover ${definition.activationReward}.`)
  return true
}

export const applyPropEffects = (state: RunState, points: readonly Point[], effects: readonly PropEffectKind[]): string[] => {
  const targets = new Set(points.map(pointKey))
  const changed: string[] = []
  for (const prop of state.floor.props) {
    if (prop.state === 'destroyed' || !targets.has(pointKey(prop))) continue
    const effect = effects.find(candidate => prop.hooks?.includes(candidate))
    if (!effect) continue
    const definition = propDefinition(prop.kind)
    prop.state = 'destroyed'
    reward(state, prop, definition.effectReward)
    log(state, `The ${effect} breaks the ${definition.name}; it leaves ${definition.effectReward}.`)
    changed.push(prop.id)
  }
  return changed
}
