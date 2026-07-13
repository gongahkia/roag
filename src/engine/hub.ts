import { streamSeed } from '../rng'
import type { HubState } from '../types'

export type HubAction = 'routes' | 'roster' | 'supplies'
export interface HubView { heirName: string; state: HubState }

const givenNames = ['Ari', 'Bea', 'Cato', 'Dara']
const familyNames = ['Vale', 'Morrow', 'Rook', 'Sable']

export const heirNameFor = (seed: number): string => `${givenNames[streamSeed(seed, 'generation', 'heir-given') % givenNames.length]} ${familyNames[streamSeed(seed, 'generation', 'heir-family') % familyNames.length]}`
export const createHubState = (seed: number): HubState => ({ season: streamSeed(seed, 'generation', 'hub-season') % 4, supplies: ['tonic', 'ropeBundle', 'rock'], rescued: [], unlockedAreas: ['mine'] })
export const hubView = (seed: number, state: HubState): HubView => ({ heirName: heirNameFor(seed), state })
