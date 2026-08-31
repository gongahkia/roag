import type { Actor, Telegraph, Tile } from './types'

export interface TelegraphPresentation { glyph: string; color: string; label: string }

const actionSignal: Record<string, string> = {
  'enemy-shot': 'SHOT', 'enemy-root': 'ROOT', 'enemy-web': 'WEB', 'enemy-fire': 'FIRE', 'enemy-pull': 'PULL', 'enemy-dart': 'DART', 'enemy-ritual': 'RITUAL', 'foreman-cavein': 'CAVE-IN', 'heartwood-charge': 'CHARGE', 'geode-fissure': 'FISSURE', 'regent-decree': 'DECREE', 'regent-judgment': 'JUDGMENT', 'guardian-slam': 'SLAM', 'pressure-vent-sweep': 'PRESSURE VENT', 'relay-discharge': 'RELAY DISCHARGE', 'intake-shear': 'INTAKE SHEAR', 'elite-pressure-sweep': 'ELITE SWEEP'
}

export const isTelegraphVisible = (floor: { width: number; height: number; tiles: readonly Pick<Tile, 'visible'>[]; actors: readonly Pick<Actor, 'id' | 'x' | 'y'>[] }, telegraph: Telegraph): boolean => {
  const visibleAt = (point: { x: number; y: number }) => point.x >= 0 && point.y >= 0 && point.x < floor.width && point.y < floor.height && Boolean(floor.tiles[point.y * floor.width + point.x]?.visible)
  const source = floor.actors.find(actor => actor.id === telegraph.sourceId)
  return telegraph.cells.some(visibleAt) || Boolean(source && visibleAt(source))
}

export const presentTelegraph = (telegraph: Telegraph, turn: number, source: string): TelegraphPresentation => {
  const impact = Math.max(0, telegraph.resolveTurn - turn)
  const major = telegraph.danger === 'major'
  const outcome = telegraph.cover ? 'COVER' : telegraph.collision ? 'HIT' : 'PATH'
  const signal = actionSignal[telegraph.actionId] ?? telegraph.actionId.toUpperCase()
  const responses = telegraph.responses?.map(response => response.toUpperCase()).join('/')
  return { glyph: major ? '!' : ':', color: major ? '#ee6f78' : '#f4d26a', label: `T-${impact} ${major ? 'MAJ' : 'MIN'} ${signal}${responses ? ` [${responses}]` : ''} ${outcome} ${source}` }
}
