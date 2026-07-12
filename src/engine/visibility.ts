import type { RunState } from '../types'
import { getTile } from '../world'

export function refreshFov(state: RunState): void {
  for (const tile of state.floor.tiles) tile.visible = false
  for (let y = Math.max(0, state.hero.y - 10); y <= Math.min(34, state.hero.y + 10); y++) for (let x = Math.max(0, state.hero.x - 10); x <= Math.min(47, state.hero.x + 10); x++) {
    if (hasLine(state, state.hero, { x, y })) { const tile = getTile(state.floor, x, y)!; tile.visible = true; tile.explored = true }
  }
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
    if (!(x === from.x && y === from.y) && getTile(state.floor, x, y)?.kind === 'wall') return false
    const twice = 2 * error
    if (twice >= dy) { error += dy; x += sx }
    if (twice <= dx) { error += dx; y += sy }
  }
}
