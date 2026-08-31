import { rngFor } from '../rng'
import type { RouteBoardConnection, RouteBoardDestination, RouteBoardState, RouteBoardTransitConsequence } from '../types'

export const ROUTE_BOARD_NETWORK_ID = 'helios-intake-v1'
export const ROUTE_BOARD_HISTORY_LIMIT = 24

const destinations: readonly RouteBoardDestination[] = [
  { id: 'destination:kestrel', siteId: 'sector-00:site-00', label: 'Kestrel Landing', summary: 'instrumentation dock and survey exchange', x: 12, y: 20 },
  { id: 'destination:orison', siteId: 'sector-00:site-01', label: 'Orison Relay', summary: 'relay maintenance habitat', x: 35, y: 10 },
  { id: 'destination:halcyon', siteId: 'sector-00:site-03', label: 'Halcyon Dock', summary: 'salvage tender and drydock', x: 35, y: 31 },
  { id: 'destination:nerida', siteId: 'sector-00:site-09', label: 'Nerida Pressure Chain', summary: 'submerged pump settlements', x: 61, y: 20 },
  { id: 'destination:borealis', siteId: 'sector-00:site-08', label: 'Borealis Glassworks', summary: 'ceramic furnace cooperative', x: 80, y: 10 }
]

const connections: readonly RouteBoardConnection[] = [
  { id: 'route:kestrel-orison', fromDestinationId: 'destination:kestrel', toDestinationId: 'destination:orison', durationMarks: 360, risk: 'low', opportunity: 'relay maintenance allotment', warning: 'surveyed beacon chain', confidence: 'reported', status: 'open' },
  { id: 'route:kestrel-halcyon', fromDestinationId: 'destination:kestrel', toDestinationId: 'destination:halcyon', durationMarks: 180, risk: 'elevated', opportunity: 'salvage tender exchange', warning: 'intermittent approach aperture', confidence: 'estimated', status: 'open', consequenceProfile: 'possibleDelay' },
  { id: 'route:orison-nerida', fromDestinationId: 'destination:orison', toDestinationId: 'destination:nerida', durationMarks: 300, risk: 'elevated', opportunity: 'pressure-ceramic contracts', warning: 'dense-atmosphere transfer window', confidence: 'reported', status: 'open' },
  { id: 'route:halcyon-nerida', fromDestinationId: 'destination:halcyon', toDestinationId: 'destination:nerida', durationMarks: 240, risk: 'low', opportunity: 'pump-chain route intelligence', warning: 'old tether observations', confidence: 'stale', status: 'open' },
  { id: 'route:nerida-borealis', fromDestinationId: 'destination:nerida', toDestinationId: 'destination:borealis', durationMarks: 210, risk: 'high', opportunity: 'furnace repair priority', warning: 'thermal debris field', confidence: 'estimated', status: 'open', consequenceProfile: 'possibleDelay' },
  { id: 'route:orison-borealis', fromDestinationId: 'destination:orison', toDestinationId: 'destination:borealis', durationMarks: 150, risk: 'high', opportunity: 'fast ceramic transfer', warning: 'relay aperture remains sealed', confidence: 'reported', status: 'unavailable', unavailableReason: 'the aperture is held closed pending a pressure-safety inspection' }
]

export const routeBoardDestinations = (): readonly RouteBoardDestination[] => destinations
export const routeBoardConnections = (): readonly RouteBoardConnection[] => connections
export const routeBoardDestination = (id: string): RouteBoardDestination | undefined => destinations.find(destination => destination.id === id)
export const routeBoardDestinationForSite = (siteId: string): RouteBoardDestination | undefined => destinations.find(destination => destination.siteId === siteId)
export const routeBoardConnection = (id: string): RouteBoardConnection | undefined => connections.find(connection => connection.id === id)
export const routeBoardOtherDestination = (connection: RouteBoardConnection, currentDestinationId: string): RouteBoardDestination | undefined => routeBoardDestination(connection.fromDestinationId === currentDestinationId ? connection.toDestinationId : connection.fromDestinationId)
export const routeBoardConnectionsFor = (currentDestinationId: string): readonly RouteBoardConnection[] => connections.filter(connection => connection.fromDestinationId === currentDestinationId || connection.toDestinationId === currentDestinationId)
export const routeBoardConnectionAvailable = (board: RouteBoardState, connection: RouteBoardConnection): boolean => connection.status === 'open' && !board.unavailableConnectionIds.includes(connection.id)

export const createRouteBoardState = (activeSiteId: string): RouteBoardState => {
  const current = routeBoardDestinationForSite(activeSiteId) ?? routeBoardDestination('destination:kestrel')!
  return {
    version: 1,
    networkId: ROUTE_BOARD_NETWORK_ID,
    currentDestinationId: current.id,
    knownDestinationIds: destinations.map(destination => destination.id),
    unavailableConnectionIds: connections.filter(connection => connection.status === 'unavailable').map(connection => connection.id),
    history: [],
    nextTransitSequence: 0
  }
}

export const cloneRouteBoardState = (board: RouteBoardState): RouteBoardState => ({
  ...board,
  knownDestinationIds: [...board.knownDestinationIds],
  unavailableConnectionIds: [...board.unavailableConnectionIds],
  ...(board.transit ? { transit: { ...board.transit, consequence: { ...board.transit.consequence } } } : {}),
  history: board.history.map(entry => ({ ...entry, consequence: { ...entry.consequence } }))
})

export const routeBoardTransitConsequence = (seed: number, transitId: string, connection: RouteBoardConnection): RouteBoardTransitConsequence => {
  if (connection.consequenceProfile !== 'possibleDelay') return { kind: 'none', additionalMarks: 0, detail: 'No reportable transit interruption occurred.' }
  const delayed = rngFor(seed, 'galaxy', 'route-board', 'transit-consequence', transitId, connection.id).int(0, 1) === 0
  return delayed
    ? { kind: 'navigationDelay', additionalMarks: 60, detail: 'A relay aperture drift required a sixty-mark navigation hold.' }
    : { kind: 'none', additionalMarks: 0, detail: 'The aperture held its published alignment.' }
}
