import type { GroundItem, Tile } from '../types'

export const isItemVisible = (tile: Tile, item?: GroundItem): boolean => Boolean(item && (tile.visible || item.visibleInFog))
