import type { Biome } from '../types'
import type { HubAction } from './hub'

export type Screen = 'title' | 'splash' | 'createCourier' | 'approach' | 'hub' | 'area' | 'level' | 'loading' | 'analysis'
export interface ScreenRoute { screen: Screen; biome: Biome; heirSeed?: number; hubAction?: HubAction }

export const initialRoute = (): ScreenRoute => ({ screen: 'title', biome: 'mine' })

export const navigate = (route: ScreenRoute, key: string, hasSavedRun: boolean): ScreenRoute => {
  const command = key.toLowerCase()
  if (route.screen === 'splash') {
    const next = navigate({ ...route, screen: 'title' }, key, hasSavedRun)
    return next.screen === 'title' ? route : next
  }
  if (route.screen === 'title') return command === 'n' ? { ...route, screen: 'approach' } : command === 'l' && hasSavedRun ? { ...route, screen: 'level' } : route
  if (route.screen === 'createCourier') return key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'approach') return key === 'Enter' ? { ...route, screen: 'hub' } : key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'hub') {
    if (command === 'a' || key === 'Enter') return { ...route, screen: 'area' }
    if (command === 'r') return { ...route, hubAction: 'roster' }
    if (command === 's') return { ...route, hubAction: 'shop' }
    if (command === 'o') return { ...route, hubAction: 'outfitter' }
    if (command === 'h') return { ...route, hubAction: 'routes' }
    return key === 'Escape' ? { ...route, screen: 'title' } : route
  }
  if (route.screen === 'area') return command === 'e' || key === 'Enter' ? { ...route, screen: 'level' } : key === 'Escape' ? { ...route, screen: 'hub' } : route
  if (route.screen === 'loading' || route.screen === 'analysis') return route
  return key === 'Escape' ? { ...route, screen: 'area' } : route
}
