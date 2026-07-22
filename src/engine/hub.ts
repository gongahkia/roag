import { streamSeed } from '../rng'
import type { HubState } from '../types'

export type HubAction = 'routes' | 'roster' | 'supplies'
export interface HubView { courierName: string; state: HubState }

export const createHubState = (seed: number): HubState => ({ season: streamSeed(seed, 'generation', 'hub-season') % 4, supplies: ['tonic', 'ropeBundle', 'rock'], rescued: [], unlockedAreas: ['mine'], completedAreas: [] })
export const hubView = (courierName: string, state: HubState): HubView => ({ courierName, state })
