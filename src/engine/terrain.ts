import type { Floor, Point, TileKind } from '../types'
import { getTile } from '../world'

export type TerrainTag = 'fire' | 'gas' | 'water' | 'rubble' | 'pit' | 'bomb' | 'volatile'
export type TerrainReaction = 'steam' | 'quenched' | 'ignited-gas' | 'cleared-rubble' | 'detonated-volatile' | 'flooded-pit'
export interface TerrainEffect { point: Point; reaction: TerrainReaction }

export const terrainTags = (kind: TileKind): TerrainTag[] => {
  if (kind === 'lava' || kind === 'fireVent') return ['fire']
  if (kind === 'gas') return ['gas']
  if (kind === 'water') return ['water']
  if (kind === 'crumble' || kind === 'boulder' || kind === 'rubble') return ['rubble']
  if (kind === 'pit') return ['pit']
  if (kind === 'crate' || kind === 'chest') return ['volatile']
  return []
}

export const resolveTerrainReactions = (floor: Floor, points: readonly Point[], incoming: readonly TerrainTag[]): TerrainEffect[] => {
  const effects: TerrainEffect[] = []
  const seen = new Set<string>()
  for (const point of points) {
    const key = `${point.x},${point.y}`
    if (seen.has(key)) continue
    seen.add(key)
    const tile = getTile(floor, point.x, point.y)
    if (!tile) continue
    const tags = new Set([...terrainTags(tile.kind), ...incoming])
    let reaction: TerrainReaction | undefined
    if (incoming.includes('water') && tags.has('fire')) { tile.kind = 'floor'; reaction = 'quenched' }
    else if (incoming.includes('fire') && tags.has('water')) { tile.kind = 'floor'; reaction = 'steam' }
    else if ((incoming.includes('fire') || incoming.includes('bomb')) && tags.has('gas')) { tile.kind = 'floor'; reaction = 'ignited-gas' }
    else if (incoming.includes('bomb') && tags.has('rubble')) { tile.kind = 'floor'; reaction = 'cleared-rubble' }
    else if ((incoming.includes('fire') || incoming.includes('bomb')) && tags.has('volatile')) { tile.kind = 'floor'; reaction = 'detonated-volatile' }
    else if (incoming.includes('water') && tags.has('pit')) { tile.kind = 'water'; reaction = 'flooded-pit' }
    if (reaction) effects.push({ point: { ...point }, reaction })
  }
  return effects
}
