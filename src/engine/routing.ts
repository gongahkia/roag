import type { Biome } from '../types'

export type Screen = 'title' | 'approach' | 'hub' | 'area' | 'level'
export interface ScreenRoute { screen: Screen; biome: Biome; heirSeed?: number }

export const initialRoute = (): ScreenRoute => ({ screen: 'title', biome: 'mine' })

export const navigate = (route: ScreenRoute, key: string, hasSavedRun: boolean): ScreenRoute => {
  const command = key.toLowerCase()
  if (route.screen === 'title') return command === 'n' ? { ...route, screen: 'approach' } : command === 'l' && hasSavedRun ? { ...route, screen: 'level' } : route
  if (route.screen === 'approach') return key === 'Enter' ? { ...route, screen: 'hub' } : key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'hub') return command === 'a' || key === 'Enter' ? { ...route, screen: 'area' } : key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'area') return command === 'e' || key === 'Enter' ? { ...route, screen: 'level' } : key === 'Escape' ? { ...route, screen: 'hub' } : route
  return key === 'Escape' ? { ...route, screen: 'area' } : route
}
