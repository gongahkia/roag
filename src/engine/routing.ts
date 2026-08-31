import type { Biome, CompanionControlMode } from '../types'
import type { HubAction } from './hub'
import type { CompanionLodgeAction } from './companions'

export type Screen = 'title' | 'splash' | 'codex' | 'createCourier' | 'approach' | 'hub' | 'sector' | 'area' | 'level' | 'loading' | 'transit' | 'analysis'
export interface ScreenRoute { screen: Screen; biome: Biome; siteId?: string; heirSeed?: number; hubAction?: HubAction; sealedPackageAction?: 'open' | 'deliver' | 'refuse' | 'abandon'; destinationIntervention?: 'nerida-bypass'; companionAction?: { id: string; action: CompanionLodgeAction }; companionControlMode?: CompanionControlMode; routeBoardConfirmation?: 'transit' | 'landing'; codexPage?: number }

export const initialRoute = (): ScreenRoute => ({ screen: 'title', biome: 'mine' })

export const navigate = (route: ScreenRoute, key: string, hasSavedRun: boolean): ScreenRoute => {
  const command = key.toLowerCase()
  if (route.screen === 'splash') {
    const next = navigate({ ...route, screen: 'title' }, key, hasSavedRun)
    return next.screen === 'title' ? route : next
  }
  if (route.screen === 'title') return command === 'n' ? { ...route, screen: 'approach' } : command === 'w' ? { ...route, screen: 'codex', codexPage: 0 } : command === 'l' && hasSavedRun ? { ...route, screen: 'level' } : route
  if (route.screen === 'codex') return key === 'Escape' || command === 'w' ? { ...route, screen: 'title', codexPage: undefined } : route
  if (route.screen === 'createCourier') return key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'approach') return key === 'Enter' ? { ...route, screen: 'hub' } : key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'hub') return key === 'Escape' ? { ...route, screen: 'title' } : route
  if (route.screen === 'area') return command === 'e' || key === 'Enter' ? { ...route, screen: 'level' } : key === 'Escape' ? { ...route, screen: 'hub' } : route
  if (route.screen === 'loading' || route.screen === 'transit' || route.screen === 'analysis') return route
  return key === 'Escape' ? { ...route, screen: 'area' } : route
}
