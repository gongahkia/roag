import { streamSeed } from './rng'

export type MineSeason = 'spring' | 'summer' | 'autumn' | 'winter'
export type CalendarEra = 'AD' | 'BC'
export interface SeasonVisual { season: MineSeason; name: string; year: number; era: CalendarEra; color: string; scene: string }

const visuals: Omit<SeasonVisual, 'year' | 'era'>[] = [
  { season: 'spring', name: 'Spring', color: '#96d38b', scene: 'Spring stirs along the village trail.' },
  { season: 'summer', name: 'Summer', color: '#f0c56a', scene: 'Summer warms the village trail.' },
  { season: 'autumn', name: 'Autumn', color: '#f0a45d', scene: 'Autumn leaves gather along the village trail.' },
  { season: 'winter', name: 'Winter', color: '#b9d7ed', scene: 'Winter frost glints along the village trail.' }
]

export const mineSeason = (heirSeed: number): SeasonVisual => {
  const calendar = streamSeed(heirSeed, 'lore', 'mine-calendar')
  return {
    ...visuals[streamSeed(heirSeed, 'generation', 'mine-approach') % visuals.length],
    era: calendar % 7 === 0 ? 'BC' : 'AD',
    year: 100 + Math.floor(calendar / 7) % 900
  }
}

export const seasonLabel = (season: SeasonVisual): string => `${season.name.toUpperCase()} · ${season.year} ${season.era}`
