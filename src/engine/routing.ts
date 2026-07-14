import type { Biome } from '../types'
import type { HubAction } from './hub'

export type Screen = 'title' | 'approach' | 'hub' | 'area' | 'level' | 'loading'
export interface ScreenRoute { screen: Screen; biome: Biome; heirSeed?: number; hubAction?: HubAction }

export const initialRoute = (): ScreenRoute => ({ screen: 'title', biome: 'mine' })

export const navigate = (route: ScreenRoute, key: string, hasSavedRun: boolean): ScreenRoute => {
  const command = key.toLowerCase()
  if (route.screen === 'title') return command === 'n' ? { ...route, screen: 'approach' } : command === 'l' && hasSavedRun ? { ...route, screen: 'level' } : route
  if (route.screen === 'approach') return key === 'Enter' ? { ...route, screen: 'hub' } : key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'hub') {
    if (command === 'a' || key === 'Enter') return { ...route, screen: 'area' }
    if (command === 'r') return { ...route, hubAction: 'roster' }
    if (command === 's') return { ...route, hubAction: 'supplies' }
    if (command === 'h') return { ...route, hubAction: 'routes' }
    return key === 'Escape' ? { ...route, screen: 'title' } : route
  }
  if (route.screen === 'area') return command === 'e' || key === 'Enter' ? { ...route, screen: 'level' } : key === 'Escape' ? { ...route, screen: 'hub' } : route
  if (route.screen === 'loading') return route
  return key === 'Escape' ? { ...route, screen: 'area' } : route
}
