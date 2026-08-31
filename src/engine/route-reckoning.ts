export const ROUTE_RECKONING_STEP_MS = 100
export const ROUTE_RECKONING_UNITS_PER_CYCLE = 1_440
export const ROUTE_RECKONING_UNITS_PER_WATCH = 60
export const ROUTE_RECKONING_WORLD_TICK_UNITS = 360
export const ROUTE_RECKONING_NEAR_EXPIRY_UNITS = 240
export const ROUTE_RECKONING_MAX_CATCH_UP_STEPS = 20

export const routeReckoningFromLegacyDay = (sectorDay: number): number => Math.max(0, Math.round(sectorDay * ROUTE_RECKONING_UNITS_PER_CYCLE))
export const sectorDayFromRouteReckoning = (routeReckoning: number): number => routeReckoning / ROUTE_RECKONING_UNITS_PER_CYCLE
export const routeCycleFor = (routeReckoning: number): number => Math.floor(routeReckoning / ROUTE_RECKONING_UNITS_PER_CYCLE) + 1
export const routeWatchFor = (routeReckoning: number): number => Math.floor((routeReckoning % ROUTE_RECKONING_UNITS_PER_CYCLE) / ROUTE_RECKONING_UNITS_PER_WATCH) + 1
export const routeMarkFor = (routeReckoning: number): number => routeReckoning % ROUTE_RECKONING_UNITS_PER_WATCH
export const routeWorldTickFor = (routeReckoning: number): number => Math.floor(routeReckoning / ROUTE_RECKONING_WORLD_TICK_UNITS)

export const formatRouteReckoning = (routeReckoning: number): string => `ROUTE ${String(routeCycleFor(routeReckoning)).padStart(3, '0')} · WATCH ${String(routeWatchFor(routeReckoning)).padStart(2, '0')} · MARK ${String(routeMarkFor(routeReckoning)).padStart(2, '0')}`
