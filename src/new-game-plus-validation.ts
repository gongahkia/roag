import { runAutoplay } from './autoplay-runner'
import { advanceCampaignTier, completeCampaignTier, continueCampaignRoute, initialCampaignCycle, initialCampaignRoute } from './engine/campaign'
import { companionLeadForRescue } from './engine/companions'
import { perform } from './engine/input'
import { newHero, newSeededCampaignRun } from './engine/run'
import type { AutoplayReplayMetadata, AutoplayTraceEntry, Biome, CampaignCycle, CampaignDifficultyPackageMetadata, CampaignTier, Companion, DifficultyContext, Hero, RescuedNpc, RunState } from './types'
import { generationValidationFailures, generateAreaFloor, validateGeneration } from './world'

export const NEW_GAME_PLUS_VALIDATION_VERSION = 1 as const
export const NEW_GAME_PLUS_VALIDATION_MODES = ['smoke', 'full'] as const
export type NewGamePlusValidationMode = typeof NEW_GAME_PLUS_VALIDATION_MODES[number]
export const NEW_GAME_PLUS_VALIDATION_CORPUS: Record<NewGamePlusValidationMode, readonly number[]> = { smoke: [7], full: [0, 7, 41, 99] }
const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
const tiers: readonly CampaignTier[] = ['base', 'ngPlus', 'ngPlusPlus']

export interface NewGamePlusValidationRoute { biome: Biome; routePosition: number; areaFloor: number; floorIndex: number }
export interface NewGamePlusValidationFailure { seed: number; tier: CampaignTier; route: NewGamePlusValidationRoute; error: string; replay: AutoplayReplayMetadata; trace: AutoplayTraceEntry[]; difficulty?: DifficultyContext; difficultyPackage?: CampaignDifficultyPackageMetadata }
export interface NewGamePlusTierValidation { seed: number; tier: CampaignTier; accepted: boolean; replay: AutoplayReplayMetadata; trace: AutoplayTraceEntry[]; difficulty?: DifficultyContext; difficultyPackage?: CampaignDifficultyPackageMetadata; failures: NewGamePlusValidationFailure[] }
export interface NewGamePlusValidationReport { version: typeof NEW_GAME_PLUS_VALIDATION_VERSION; mode: NewGamePlusValidationMode; seeds: number[]; tiers: CampaignTier[]; entries: NewGamePlusTierValidation[]; terminalCap: { blocked: boolean; message?: string }; accepted: boolean; failures: NewGamePlusValidationFailure[] }

const cycleFor = (tier: CampaignTier): CampaignCycle => tier === 'base' ? initialCampaignCycle() : tier === 'ngPlus' ? advanceCampaignTier(completeCampaignTier(initialCampaignCycle())) : advanceCampaignTier(completeCampaignTier(advanceCampaignTier(completeCampaignTier(initialCampaignCycle()))))
const fixture = (): { hero: Hero; rescues: RescuedNpc[]; companions: Companion[] } => {
  const hero = newHero({ name: 'Validation Courier', origin: 'tidebound', calling: 'pathmaker', deathMode: 'checkpoint' })
  hero.gold = 173
  hero.inventory = ['tonic', 'focusTonic', 'rock', 'bombPack', 'ropeBundle', 'ember', 'tideSpear']
  hero.traversalTools = ['stoneWedge', 'cordAnchor']
  const rescue = { id: 'rescue:mine:1:validation', name: 'Validation Scout', biome: 'mine' as const, floor: 1 }
  const companion = companionLeadForRescue(rescue)
  companion.rosterStatus = 'active'
  return { hero, rescues: [rescue], companions: [companion] }
}
const routeFor = (state: RunState): NewGamePlusValidationRoute => ({ biome: state.floor.biome, routePosition: Math.max(0, (state.areaOrder ?? []).indexOf(state.floor.biome)), areaFloor: state.areaFloor ?? state.floor.index % 4, floorIndex: state.floor.index })
const carriedState = (hero: Hero, companions: readonly Companion[]) => ({ gold: hero.gold, inventory: [...hero.inventory], tools: [...(hero.traversalTools ?? [])], companions: companions.map(companion => ({ id: companion.id, rosterStatus: companion.rosterStatus, injury: companion.injury, permanentlyLost: companion.permanentlyLost, controlMode: companion.controlMode })) })
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)

export const campaignCycleForValidationTier = (tier: CampaignTier): CampaignCycle => cycleFor(tier)
export const newGamePlusValidationFailure = ({ seed, tier, route, error, replay, trace, difficulty }: Omit<NewGamePlusValidationFailure, 'difficultyPackage'>): NewGamePlusValidationFailure => ({ seed, tier, route: { ...route }, error, replay: structuredClone(replay), trace: structuredClone(trace), ...(difficulty ? { difficulty: structuredClone(difficulty) } : {}), ...(difficulty?.difficultyPackage ? { difficultyPackage: structuredClone(difficulty.difficultyPackage) } : {}) })

export const validateNewGamePlusTier = (seed: number, tier: CampaignTier): NewGamePlusTierValidation => {
  const cycle = cycleFor(tier)
  const carried = fixture()
  const initial = newSeededCampaignRun(seed, carried.hero, carried.rescues, [], cycle, carried.companions)
  const expectedCarryover = carriedState(carried.hero, carried.companions)
  const probe = runAutoplay(newSeededCampaignRun(seed, carried.hero, carried.rescues, [], cycle, carried.companions), { mode: 'omniscient', policy: 'clear', turnLimit: 1, captureTrace: true })
  const route = routeFor(initial)
  const difficulty = initial.floor.difficulty ? structuredClone(initial.floor.difficulty) : undefined
  const difficultyPackage = difficulty?.difficultyPackage ? structuredClone(difficulty.difficultyPackage) : undefined
  const failures: NewGamePlusValidationFailure[] = []
  const fail = (error: string, failureRoute = route, failureDifficulty = difficulty): void => {
    failures.push(newGamePlusValidationFailure({ seed, tier, route: failureRoute, error, replay: probe.replay, trace: probe.trace, ...(failureDifficulty ? { difficulty: failureDifficulty } : {}) }))
  }
  if (!same(initial.campaignCycle, cycle)) fail('tier initialization does not preserve CampaignCycle')
  if (!same(carriedState(initial.hero, initial.companions ?? []), expectedCarryover)) fail('carried inventory or party state changed during tier initialization')
  if (difficulty?.campaignTier !== tier || difficultyPackage?.tier !== tier) fail('resolved difficulty package does not match tier initialization')
  if (probe.outcome === 'error' || probe.replay.difficulty?.campaignTier !== tier || probe.replay.difficultyPackage?.tier !== tier) fail(`replay probe failed tier metadata: ${probe.error ?? probe.outcome}`)

  const routeState = newSeededCampaignRun(seed, carried.hero, carried.rescues, [], cycle, carried.companions)
  routeState.floor.objective.status = 'complete'
  routeState.floor.guardianDefeated = true
  routeState.hero.x = routeState.floor.exit.x
  routeState.hero.y = routeState.floor.exit.y
  const routeEvents = perform(routeState, 'q')
  if (!routeEvents.some(event => event.type === 'floor')) fail('base-route completion did not descend to the next floor')
  else if (routeState.floor.difficulty?.campaignTier !== tier || routeState.floor.difficulty.difficultyPackage?.tier !== tier) fail('base-route completion did not retain the resolved tier package', routeFor(routeState), routeState.floor.difficulty)
  else if (!same(carriedState(routeState.hero, routeState.companions ?? []), expectedCarryover)) fail('base-route completion changed carried inventory or party state', routeFor(routeState), routeState.floor.difficulty)

  for (const biome of biomes) for (let routePosition = 0; routePosition < 4; routePosition++) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
    const generationRoute = { biome, routePosition, areaFloor, floorIndex: (biomes.indexOf(biome) * 4) + areaFloor }
    try {
      const floor = generateAreaFloor(seed, biome, areaFloor, routePosition, cycle)
      const validation = validateGeneration(floor)
      if (!validation.valid) for (const problem of generationValidationFailures(floor, validation)) fail(`optional/generation safety: route=${problem.routeNode} invariant=${problem.invariant}`, generationRoute, floor.difficulty)
      if (floor.difficulty?.campaignTier !== tier || floor.difficulty.difficultyPackage?.tier !== tier) fail('generation bypassed resolved tier package', generationRoute, floor.difficulty)
    } catch (error) { fail(`generation error: ${error instanceof Error ? error.message : String(error)}`, generationRoute) }
  }
  return { seed, tier, accepted: failures.length === 0, replay: structuredClone(probe.replay), trace: structuredClone(probe.trace), ...(difficulty ? { difficulty } : {}), ...(difficultyPackage ? { difficultyPackage } : {}), failures }
}

const terminalCap = (): NewGamePlusValidationReport['terminalCap'] => {
  const capped = completeCampaignTier(cycleFor('ngPlusPlus'))
  try { continueCampaignRoute({ ...initialCampaignRoute(7), cycle: capped }); return { blocked: false, message: 'NG++ completed cap accepted a continuation' } }
  catch (error) { return error instanceof Error && error.message.includes('NG++ completed cap reached') ? { blocked: true } : { blocked: false, message: error instanceof Error ? error.message : String(error) } }
}

export const validateNewGamePlusCorpus = (mode: NewGamePlusValidationMode = 'smoke'): NewGamePlusValidationReport => {
  const seeds = [...NEW_GAME_PLUS_VALIDATION_CORPUS[mode]]
  const entries = seeds.flatMap(seed => tiers.map(tier => validateNewGamePlusTier(seed, tier)))
  const failures = entries.flatMap(entry => entry.failures)
  const cap = terminalCap()
  return { version: NEW_GAME_PLUS_VALIDATION_VERSION, mode, seeds, tiers: [...tiers], entries, terminalCap: cap, accepted: cap.blocked && failures.length === 0, failures }
}
