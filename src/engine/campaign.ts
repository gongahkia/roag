import type { Alignment, Biome, CampaignCycle, CampaignRouteState, CampaignTier, CompanionControlMode, CompanionControlModeEvent, LegacyRecord, LineageEvent } from '../types'
import { rngFor } from '../rng'
import { cloneCompanions, loseCompanionForRescue } from './companions'
import { cloneCarryoverDiagnostics } from './carryover'

export const BIOME_POOL = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'] as const satisfies readonly Biome[]
export const AREA_ORDER = BIOME_POOL
export const LEGACY_AREA_ORDER = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'] as const satisfies readonly Biome[]
export const DEFAULT_AREA_ORDER = ['mine', 'wilds', 'caverns', 'ruins'] as const satisfies readonly Biome[]
export const CAMPAIGN_TIERS = ['base', 'ngPlus', 'ngPlusPlus'] as const satisfies readonly CampaignTier[]
export const isCampaignAreaOrder = (value: readonly Biome[]): boolean => value.length === 4 && value.every(area => BIOME_POOL.includes(area)) && new Set(value).size === 4
export const isLegacyCampaignAreaOrder = (value: readonly Biome[]): boolean => value.length === LEGACY_AREA_ORDER.length && value.every(area => (LEGACY_AREA_ORDER as readonly Biome[]).includes(area)) && new Set(value).size === LEGACY_AREA_ORDER.length
export const campaignOrderForSeed = (seed: number): Biome[] => {
  const rng = rngFor(seed, 'progression', 'campaign-area-order')
  return rng.shuffle([...rng.shuffle([...BIOME_POOL]).slice(0, 4)])
}
export const nextArea = (biome: Biome, areaOrder: readonly Biome[] = AREA_ORDER): Biome | undefined => areaOrder[areaOrder.indexOf(biome) + 1]
export const unlockNextArea = (unlocked: readonly Biome[], completed: Biome, areaOrder: readonly Biome[] = AREA_ORDER): Biome[] => {
  const next = nextArea(completed, areaOrder)
  return next && !unlocked.includes(next) ? [...unlocked, next] : [...unlocked]
}

const tierIndex = (tier: CampaignTier): number => CAMPAIGN_TIERS.indexOf(tier)
const expectedCycleEvents = (currentTier: CampaignTier, completedTiers: readonly CampaignTier[]): CampaignCycle['events'] => {
  const current = tierIndex(currentTier)
  const events: CampaignCycle['events'] = [{ sequence: 0, tier: 'base', kind: 'entered' }]
  for (let index = 0; index < completedTiers.length; index++) {
    const tier = CAMPAIGN_TIERS[index]!
    events.push({ sequence: events.length, tier, kind: 'victory' })
    if (index + 1 <= current) events.push({ sequence: events.length, tier: CAMPAIGN_TIERS[index + 1]!, kind: 'entered' })
  }
  return events
}
export const campaignCycleErrors = (cycle: CampaignCycle): string[] => {
  const errors: string[] = []
  const current = tierIndex(cycle.currentTier)
  if (cycle.version !== 1) errors.push('unsupported cycle version')
  if (current < 0) errors.push('invalid current tier')
  const expectedCompleted = CAMPAIGN_TIERS.slice(0, cycle.completedTiers.length)
  if (cycle.completedTiers.length > CAMPAIGN_TIERS.length || cycle.completedTiers.some((tier, index) => tier !== expectedCompleted[index])) errors.push('completed tiers must be an ordered prefix')
  if (current >= 0 && cycle.completedTiers.length !== current && cycle.completedTiers.length !== current + 1) errors.push('current tier is inconsistent with completed tiers')
  const completedCap = cycle.completedTiers.length === CAMPAIGN_TIERS.length
  if (cycle.completedCap !== completedCap || cycle.completedCap && cycle.currentTier !== 'ngPlusPlus') errors.push('invalid completed cap')
  const expectedEvents = expectedCycleEvents(cycle.currentTier, cycle.completedTiers)
  if (cycle.events.length !== expectedEvents.length || cycle.events.some((event, index) => event.sequence !== index || event.tier !== expectedEvents[index]?.tier || event.kind !== expectedEvents[index]?.kind)) errors.push('invalid cycle event history')
  return errors
}
export const assertCampaignCycle = (cycle: CampaignCycle): CampaignCycle => {
  const errors = campaignCycleErrors(cycle)
  if (errors.length) throw new Error(`invalid campaign cycle: ${errors.join('; ')}`)
  return cycle
}
export const initialCampaignCycle = (): CampaignCycle => ({ version: 1, currentTier: 'base', completedTiers: [], events: [{ sequence: 0, tier: 'base', kind: 'entered' }], completedCap: false })
export const cloneCampaignCycle = (cycle: CampaignCycle): CampaignCycle => {
  assertCampaignCycle(cycle)
  return { version: 1, currentTier: cycle.currentTier, completedTiers: [...cycle.completedTiers], events: cycle.events.map(event => ({ ...event })), completedCap: cycle.completedCap }
}
export const completeCampaignTier = (cycle: CampaignCycle): CampaignCycle => {
  assertCampaignCycle(cycle)
  if (cycle.completedTiers.includes(cycle.currentTier)) throw new Error(`cannot complete campaign tier ${cycle.currentTier}: victory already recorded`)
  const completedTiers = [...cycle.completedTiers, cycle.currentTier]
  return cloneCampaignCycle({ version: 1, currentTier: cycle.currentTier, completedTiers, events: [...cycle.events, { sequence: cycle.events.length, tier: cycle.currentTier, kind: 'victory' }], completedCap: cycle.currentTier === 'ngPlusPlus' })
}
export const advanceCampaignTier = (cycle: CampaignCycle): CampaignCycle => {
  assertCampaignCycle(cycle)
  if (cycle.completedCap) throw new Error('cannot advance campaign tier: NG++ completed cap reached')
  if (!cycle.completedTiers.includes(cycle.currentTier)) throw new Error(`cannot advance campaign tier ${cycle.currentTier}: victory not recorded`)
  const next = CAMPAIGN_TIERS[tierIndex(cycle.currentTier) + 1]
  if (!next) throw new Error('cannot advance campaign tier: NG++ completed cap reached')
  return cloneCampaignCycle({ version: 1, currentTier: next, completedTiers: [...cycle.completedTiers], events: [...cycle.events, { sequence: cycle.events.length, tier: next, kind: 'entered' }], completedCap: false })
}
export const campaignContinuationPending = (cycle: CampaignCycle): boolean => {
  assertCampaignCycle(cycle)
  return !cycle.completedCap && cycle.completedTiers.includes(cycle.currentTier)
}
export type CompanionControlModeChangeContext = 'lodge' | 'floor' | 'combat' | 'autoplay' | 'replay' | 'command'
export interface CompanionControlModeMutation { changed: boolean; message: string; state: CampaignRouteState }
const cloneCompanionControlHistory = (history: readonly CompanionControlModeEvent[]): CompanionControlModeEvent[] => history.map(event => ({ ...event }))
const companionsForControlMode = (companions: CampaignRouteState['companions'], rescues: CampaignRouteState['rescuedNpcs'], mode: CompanionControlMode): CampaignRouteState['companions'] => cloneCompanions(companions, rescues).map(companion => ({ ...companion, controlMode: mode }))
const cloneCompanionControl = (state: CampaignRouteState): Pick<CampaignRouteState, 'companionControlMode' | 'companionControlHistory' | 'companions'> => ({ companionControlMode: state.companionControlMode, companionControlHistory: cloneCompanionControlHistory(state.companionControlHistory), companions: companionsForControlMode(state.companions, state.rescuedNpcs, state.companionControlMode) })
export const changeCampaignCompanionControlMode = (state: CampaignRouteState, mode: CompanionControlMode, context: CompanionControlModeChangeContext): CompanionControlModeMutation => {
  const current = cloneCompanionControl(state)
  if (context !== 'lodge') return { changed: false, message: 'Companion control can change only at the Lodge between floors.', state: { ...state, ...current } }
  if (mode === state.companionControlMode) return { changed: false, message: `Companion control is already ${mode}.`, state: { ...state, ...current } }
  const companionControlHistory = [...current.companionControlHistory, { sequence: current.companionControlHistory.length, mode, source: 'lodge' as const }]
  return { changed: true, message: `Companion control changed to ${mode}.`, state: { ...state, companionControlMode: mode, companionControlHistory, companions: companionsForControlMode(state.companions, state.rescuedNpcs, mode) } }
}
export const continueCampaignRoute = (state: CampaignRouteState): CampaignRouteState => {
  const cycle = cloneCampaignCycle(state.cycle)
  const companionControl = cloneCompanionControl(state)
  if (cycle.completedCap) throw new Error('cannot continue campaign: NG++ completed cap reached')
  if (!campaignContinuationPending(cycle)) return { ...state, areaOrder: [...state.areaOrder], completedAreas: [...state.completedAreas], unlockedAreas: [...state.unlockedAreas], rescuedNpcs: state.rescuedNpcs.map(npc => ({ ...npc })), ...companionControl, carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics), lineageEvents: state.lineageEvents.map(event => ({ ...event })), legacyRecords: state.legacyRecords.map(record => ({ ...record })), alignment: { ...state.alignment }, reputation: { trailfolk: state.reputation?.trailfolk ?? 0, kami: state.reputation?.kami ?? 0 }, cycle }
  const selectedBiome = state.areaOrder[0]
  if (!selectedBiome) throw new Error('cannot continue campaign: missing area order')
  return { ...state, areaOrder: [...state.areaOrder], completedAreas: [], unlockedAreas: [selectedBiome], selectedBiome, rescuedNpcs: state.rescuedNpcs.map(npc => ({ ...npc })), ...companionControl, carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics), lineageEvents: state.lineageEvents.map(event => ({ ...event })), legacyRecords: state.legacyRecords.map(record => ({ ...record })), alignment: { ...state.alignment }, reputation: { trailfolk: state.reputation?.trailfolk ?? 0, kami: state.reputation?.kami ?? 0 }, cycle: advanceCampaignTier(cycle) }
}

export const initialCampaignRoute = (seed?: number, companionControlMode: CompanionControlMode = 'autonomous'): CampaignRouteState => {
  const areaOrder = seed === undefined ? [...DEFAULT_AREA_ORDER] : campaignOrderForSeed(seed)
  return { version: 5, areaOrder, completedAreas: [], unlockedAreas: [areaOrder[0]], selectedBiome: areaOrder[0], rescuedNpcs: [], companions: [], companionControlMode, companionControlHistory: [{ sequence: 0, mode: companionControlMode, source: 'creation' }], carryoverDiagnostics: [], lineageEvents: [], legacyRecords: [], alignment: { kami: 0, villagePact: 0 }, reputation: { trailfolk: 0, kami: 0 }, cycle: initialCampaignCycle() }
}
export const completeCampaignArea = (state: CampaignRouteState, completed: Biome): CampaignRouteState => ({ ...state, areaOrder: [...state.areaOrder], completedAreas: state.completedAreas.includes(completed) ? [...state.completedAreas] : [...state.completedAreas, completed], unlockedAreas: [...state.unlockedAreas], selectedBiome: completed, rescuedNpcs: [...state.rescuedNpcs], ...cloneCompanionControl(state), carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics), lineageEvents: [...state.lineageEvents], legacyRecords: [...state.legacyRecords], alignment: { ...state.alignment }, reputation: { trailfolk: state.reputation?.trailfolk ?? 0, kami: state.reputation?.kami ?? 0 }, cycle: cloneCampaignCycle(state.cycle) })
export const unlockCampaignArea = (state: CampaignRouteState, biome: Biome): CampaignRouteState => ({ ...state, areaOrder: [...state.areaOrder], completedAreas: [...state.completedAreas], unlockedAreas: state.unlockedAreas.includes(biome) ? [...state.unlockedAreas] : [...state.unlockedAreas, biome], selectedBiome: biome, rescuedNpcs: [...state.rescuedNpcs], ...cloneCompanionControl(state), carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics), lineageEvents: [...state.lineageEvents], legacyRecords: [...state.legacyRecords], alignment: { ...state.alignment }, reputation: { trailfolk: state.reputation?.trailfolk ?? 0, kami: state.reputation?.kami ?? 0 }, cycle: cloneCampaignCycle(state.cycle) })
export const recordCampaignSacrifice = (state: CampaignRouteState, event: LineageEvent): CampaignRouteState => {
  const rescuedNpcs = state.rescuedNpcs.filter(npc => npc.id !== event.npcId)
  return { ...state, rescuedNpcs, companionControlMode: state.companionControlMode, companionControlHistory: cloneCompanionControlHistory(state.companionControlHistory), companions: companionsForControlMode(state.companions.map(companion => loseCompanionForRescue(companion, event.npcId)), rescuedNpcs, state.companionControlMode), carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics), lineageEvents: state.lineageEvents.some(existing => existing.id === event.id) ? [...state.lineageEvents] : [...state.lineageEvents, event].slice(-12), alignment: { ...state.alignment }, reputation: { trailfolk: state.reputation?.trailfolk ?? 0, kami: state.reputation?.kami ?? 0 }, cycle: cloneCampaignCycle(state.cycle) }
}
export const appendLegacyRecord = (state: CampaignRouteState, record: LegacyRecord): CampaignRouteState => ({ ...state, ...cloneCompanionControl(state), carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics), legacyRecords: [...state.legacyRecords, { ...record }].slice(-12), alignment: { ...state.alignment }, reputation: { trailfolk: state.reputation?.trailfolk ?? 0, kami: state.reputation?.kami ?? 0 }, cycle: cloneCampaignCycle(state.cycle) })
export const addAlignment = (state: CampaignRouteState, alignment: Alignment): CampaignRouteState => ({ ...state, ...cloneCompanionControl(state), carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics), alignment: { ...state.alignment, [alignment]: state.alignment[alignment] + 1 }, cycle: cloneCampaignCycle(state.cycle) })
