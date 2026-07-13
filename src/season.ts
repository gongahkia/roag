import { streamSeed } from './rng'

export type MineSeason = 'frost' | 'rain' | 'bloom' | 'emberfall'
export interface SeasonVisual { season: MineSeason; name: string; color: string; scene: string }

const visuals: SeasonVisual[] = [
  { season: 'frost', name: 'Frost Season', color: '#b9d7ed', scene: 'Frost glints on the Mine approach.' },
  { season: 'rain', name: 'Rain Season', color: '#78a8c9', scene: 'Rain darkens the Mine approach.' },
  { season: 'bloom', name: 'Bloom Season', color: '#96d38b', scene: 'Moss blooms along the Mine approach.' },
  { season: 'emberfall', name: 'Emberfall Season', color: '#f0a45d', scene: 'Emberfall warms the Mine approach.' }
]

export const mineSeason = (heirSeed: number): SeasonVisual => visuals[streamSeed(heirSeed, 'generation', 'mine-approach') % visuals.length]
