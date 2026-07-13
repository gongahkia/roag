import type { Biome } from '../types'

export type Screen = 'title' | 'hub' | 'area' | 'level'
export interface ScreenRoute { screen: Screen; biome: Biome }

export const initialRoute = (): ScreenRoute => ({ screen: 'title', biome: 'mine' })

export const navigate = (route: ScreenRoute, key: string, hasSavedRun: boolean): ScreenRoute => {
  const command = key.toLowerCase()
  if (route.screen === 'title') return command === 'n' ? { ...route, screen: 'hub' } : command === 'l' && hasSavedRun ? { ...route, screen: 'level' } : route
  if (route.screen === 'hub') return command === 'a' || key === 'Enter' ? { ...route, screen: 'area' } : key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'area') return command === 'e' || key === 'Enter' ? { ...route, screen: 'level' } : key === 'Escape' ? { ...route, screen: 'hub' } : route
  return key === 'Escape' ? { ...route, screen: 'area' } : route
}
