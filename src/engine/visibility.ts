import { type RunState } from '../types'
import { getTile } from '../world'
import { observeEncyclopedia } from './encyclopedia'
import { isBlockingProp, isSightBlockingProp, propAt } from '../props'

export function refreshFov(state: RunState): void {
  for (const tile of state.floor.tiles) tile.visible = false
  const saltHaze = state.floor.biome === 'saltFlats' && state.floor.ecology?.some(ecology => ecology.kind === 'visibility' && ecology.state === 'active')
  const sheltered = state.floor.props.some(prop => prop.kind === 'saltFlats.caravanHusk' && Math.max(Math.abs(prop.x - state.hero.x), Math.abs(prop.y - state.hero.y)) <= 1)
  const range = (state.floor.biome === 'caverns' && !hasLight(state) ? 6 : 10) + Math.min(3, state.hero.boons?.mapMoss ?? 0) - (saltHaze && !sheltered ? 3 : 0)
  for (let y = Math.max(0, state.hero.y - range); y <= Math.min(state.floor.height - 1, state.hero.y + range); y++) for (let x = Math.max(0, state.hero.x - range); x <= Math.min(state.floor.width - 1, state.hero.x + range); x++) {
    if (hasLine(state, state.hero, { x, y })) { const tile = getTile(state.floor, x, y)!; tile.visible = true; tile.explored = true }
  }
  for (const lantern of state.floor.props.filter(prop => prop.kind === 'mine.lanternPost' && prop.state === 'activated')) {
    for (let y = Math.max(0, lantern.y - 4); y <= Math.min(state.floor.height - 1, lantern.y + 4); y++) for (let x = Math.max(0, lantern.x - 4); x <= Math.min(state.floor.width - 1, lantern.x + 4); x++) {
      if (hasLine(state, lantern, { x, y }, true)) { const tile = getTile(state.floor, x, y)!; tile.visible = true; tile.explored = true }
    }
  }
  for (const fungus of state.floor.props.filter(prop => prop.kind === 'caverns.glowingFungus' && prop.state !== 'destroyed')) {
    const radius = fungus.state === 'activated' ? 4 : 2
    for (let y = Math.max(0, fungus.y - radius); y <= Math.min(state.floor.height - 1, fungus.y + radius); y++) for (let x = Math.max(0, fungus.x - radius); x <= Math.min(state.floor.width - 1, fungus.x + radius); x++) {
      if (hasLine(state, fungus, { x, y }, true)) { const tile = getTile(state.floor, x, y)!; tile.visible = true; tile.explored = true }
    }
  }
  for (const milestone of state.floor.milestones) if (getTile(state.floor, milestone.x, milestone.y)?.visible || (state.hero.boons?.watchfulStep ?? 0) > 0 && Math.max(Math.abs(milestone.x - state.hero.x), Math.abs(milestone.y - state.hero.y)) <= 4 + (state.hero.boons?.watchfulStep ?? 0)) milestone.discovered = true
  observeEncyclopedia(state)
}

export function hasLine(state: RunState, from: { x: number; y: number }, to: { x: number; y: number }, lit = hasLight(state)): boolean {
  let x = from.x
  let y = from.y
  const dx = Math.abs(to.x - from.x)
  const dy = -Math.abs(to.y - from.y)
  const sx = from.x < to.x ? 1 : -1
  const sy = from.y < to.y ? 1 : -1
  let error = dx + dy
  while (true) {
    if (x === to.x && y === to.y) return true
    if (!(x === from.x && y === from.y) && ['wall', 'rubble', 'bramble'].includes(getTile(state.floor, x, y)?.kind ?? '')) return false
    if (!(x === from.x && y === from.y) && isBlockingProp(propAt(state.floor.props, x, y))) return false
    if (!(x === from.x && y === from.y) && isSightBlockingProp(propAt(state.floor.props, x, y))) return false
    if (!lit && getTile(state.floor, x, y)?.kind === 'darkness') return false
    const twice = 2 * error
    if (twice >= dy) { error += dy; x += sx }
    if (twice <= dx) { error += dx; y += sy }
  }
}

export const hasLight = (state: RunState): boolean => state.hero.equipment.offHand === 'lantern' || state.hero.inventory.includes('sight')
