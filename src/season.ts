import { streamSeed } from './rng'

export type MineSeason = 'frost' | 'rain' | 'bloom' | 'emberfall'
export interface SeasonVisual { season: MineSeason; name: string; color: string; scene: string }

const visuals: SeasonVisual[] = [
  { season: 'frost', name: 'Frost Season', color: '#b9d7ed', scene: 'Frost glints along the village trail.' },
  { season: 'rain', name: 'Rain Season', color: '#78a8c9', scene: 'Rain darkens the village trail.' },
  { season: 'bloom', name: 'Bloom Season', color: '#96d38b', scene: 'Moss blooms beside the village trail.' },
  { season: 'emberfall', name: 'Emberfall Season', color: '#f0a45d', scene: 'Emberfall warms the village trail.' }
]

export const mineSeason = (heirSeed: number): SeasonVisual => visuals[streamSeed(heirSeed, 'generation', 'mine-approach') % visuals.length]
