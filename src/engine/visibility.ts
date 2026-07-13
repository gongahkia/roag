import type { RunState } from '../types'
import { getTile } from '../world'
import { observeEncyclopedia } from './encyclopedia'

export function refreshFov(state: RunState): void {
  for (const tile of state.floor.tiles) tile.visible = false
  const range = state.floor.biome === 'caverns' && !hasLight(state) ? 6 : 10
  for (let y = Math.max(0, state.hero.y - range); y <= Math.min(34, state.hero.y + range); y++) for (let x = Math.max(0, state.hero.x - range); x <= Math.min(47, state.hero.x + range); x++) {
    if (hasLine(state, state.hero, { x, y })) { const tile = getTile(state.floor, x, y)!; tile.visible = true; tile.explored = true }
  }
  observeEncyclopedia(state)
}

export function hasLine(state: RunState, from: { x: number; y: number }, to: { x: number; y: number }): boolean {
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
    if (!hasLight(state) && getTile(state.floor, x, y)?.kind === 'darkness') return false
    const twice = 2 * error
    if (twice >= dy) { error += dy; x += sx }
    if (twice <= dx) { error += dx; y += sy }
  }
}

export const hasLight = (state: RunState): boolean => state.hero.equipment.offHand === 'lantern' || state.hero.inventory.includes('sight')
