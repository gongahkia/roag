import { bossContractFor } from './engine/boss-contracts'
import { BIOME_POOL } from './engine/campaign'
import type { Biome, RunTelemetry } from './types'
import { generateAreaFloor, getTile, routeContractDebug } from './world'

export interface BalanceCount { id: string; count: number }
export interface BalanceFloorSample { seed: number; biome: Biome; areaFloor: number; recipe: string; routeChoices: Record<'safe' | 'costly' | 'optional', number>; encounterRoles: string[]; terrain: string[]; boonTiming: string[]; boonUsefulness: string[]; scheduledEvents: string[]; expectedBossPhases: string[]; qualityErrors: string[] }
export interface BalanceRun { seed: number; biome: Biome; outcome: string; complete: boolean; floor: number; turns: number; metrics: RunTelemetry; floors: BalanceFloorSample[] }
export interface BalanceBiomeDashboard { samples: number; probeCompletion: { completed: number; total: number; rate: number }; recipes: BalanceCount[]; routeChoiceCost: BalanceCount[]; encounterRoles: BalanceCount[]; terrain: { generated: BalanceCount[]; interactions: BalanceCount[] }; boonTiming: BalanceCount[]; boonUsefulness: BalanceCount[]; events: { scheduled: BalanceCount[]; outcomes: BalanceCount[] }; deaths: BalanceCount[]; bossPhases: { expected: BalanceCount[]; observed: BalanceCount[] }; quality: { valid: boolean; errors: string[] } }
export interface BalanceDashboard { version: 2; mode: 'omniscient-clear'; seeds: number[]; biomes: Record<Biome, BalanceBiomeDashboard>; acceptance: { valid: boolean; errors: string[] } }

type Counts = Record<string, number>

const add = (target: Counts, values: readonly string[]): Counts => {
  for (const value of values) target[value] = (target[value] ?? 0) + 1
  return target
}

const addCounters = (target: Counts, source: Counts): Counts => {
  for (const [id, count] of Object.entries(source)) target[id] = (target[id] ?? 0) + count
  return target
}

const ranked = (counts: Counts): BalanceCount[] => Object.entries(counts).map(([id, count]) => ({ id, count })).sort((left, right) => right.count - left.count || left.id.localeCompare(right.id))
const rate = (completed: number, total: number): number => total ? Number((completed / total).toFixed(3)) : 0
const recipeBase = (recipe: string): string => recipe.replace(/-remix$/, '')

export const balanceFloorSamples = (seed: number, biome: Biome, routePosition = 0): BalanceFloorSample[] => Array.from({ length: 4 }, (_, areaFloor) => {
  const floor = generateAreaFloor(seed, biome, areaFloor, routePosition)
  const route = routeContractDebug(floor)
  const routeChoices = { safe: route?.edges.filter(edge => edge.modes.includes('safe')).length ?? 0, costly: route?.edges.filter(edge => edge.modes.includes('costly')).length ?? 0, optional: route?.edges.filter(edge => edge.modes.includes('optional')).length ?? 0 }
  const rewards = floor.rewardOffers ?? []
  const roles = new Set(['safe', 'risky', 'sidegrade'])
  const qualityErrors: string[] = []
  if (!route || !routeChoices.safe || !routeChoices.costly || !routeChoices.optional) qualityErrors.push('route choices incomplete')
  if (floor.props.length < 2 || floor.actors.filter(actor => actor.hostile).length < 1) qualityErrors.push('sparse map')
  for (const offer of rewards) {
    if (offer.choices.length !== 3 || offer.choices.some(choice => !roles.has(choice.role) || !choice.problem || !choice.terrain || !choice.route || !choice.payoff)) qualityErrors.push(`uncontextualized reward:${offer.milestoneId}`)
  }
  for (const actor of floor.actors.filter(actor => actor.encounter?.archetype === 'nativeTerrainPack' && actor.encounter.leader)) if (!actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall')) qualityErrors.push(`non-native enemy:${actor.id}`)
  return {
    seed,
    biome,
    areaFloor,
    recipe: floor.layoutId,
    routeChoices,
    encounterRoles: floor.actors.flatMap(actor => actor.combatRole ? [actor.combatRole] : []),
    terrain: floor.tiles.filter(tile => tile.kind !== 'wall').map(tile => tile.kind),
    boonTiming: rewards.filter(offer => offer.kind === 'boon').map(offer => `${areaFloor + 1}:${offer.milestoneId}`),
    boonUsefulness: rewards.filter(offer => offer.kind === 'boon').flatMap(offer => offer.choices.map(choice => `${choice.route}:${choice.terrain}:${choice.payoff}`)),
    scheduledEvents: (floor.ecology ?? []).map(event => event.kind),
    expectedBossPhases: areaFloor === 3 ? ['opening', 'pressure', 'cataclysm'].map(phase => `${bossContractFor(biome).id}:${phase}`) : [],
    qualityErrors
  }
})

const scoped = (counts: Counts, biome: Biome): Counts => Object.fromEntries(Object.entries(counts).flatMap(([id, count]) => id.startsWith(`${biome}:`) ? [[id.slice(biome.length + 1), count]] : []))

export const summarizeBalanceRuns = (runs: readonly BalanceRun[], seeds: readonly number[]): BalanceDashboard => {
  const biomes = Object.fromEntries(BIOME_POOL.map(biome => {
    const current = runs.filter(run => run.biome === biome)
    const floors = current.flatMap(run => run.floors)
    const recipes: Counts = {}
    const routeChoiceCost: Counts = {}
    const encounterRoles: Counts = {}
    const terrain: Counts = {}
    const boonTiming: Counts = {}
    const boonUsefulness: Counts = {}
    const scheduledEvents: Counts = {}
    const expectedBossPhases: Counts = {}
    const outcomes: Counts = {}
    const deaths: Counts = {}
    const interactions: Counts = {}
    const observedBossPhases: Counts = {}
    const errors = floors.flatMap(floor => floor.qualityErrors.map(error => `seed ${floor.seed} floor ${floor.areaFloor + 1}: ${error}`))
    for (const floor of floors) {
      add(recipes, [recipeBase(floor.recipe)])
      for (const [choice, count] of Object.entries(floor.routeChoices)) routeChoiceCost[choice] = (routeChoiceCost[choice] ?? 0) + count
      add(encounterRoles, floor.encounterRoles)
      add(terrain, floor.terrain)
      add(boonTiming, floor.boonTiming)
      add(boonUsefulness, floor.boonUsefulness)
      add(scheduledEvents, floor.scheduledEvents)
      add(expectedBossPhases, floor.expectedBossPhases)
    }
    for (const run of current) {
      addCounters(outcomes, run.metrics.eventOutcomes)
      addCounters(deaths, scoped(run.metrics.deathCauses, biome))
      addCounters(interactions, scoped(run.metrics.terrainInteractions, biome))
      addCounters(observedBossPhases, scoped(run.metrics.bossPhases, biome))
    }
    for (const seed of seeds) {
      const recipesForSeed = new Set(floors.filter(floor => floor.seed === seed).map(floor => recipeBase(floor.recipe)))
      if (recipesForSeed.size < 3) errors.push(`seed ${seed}: duplicate recipes`)
    }
    errors.sort()
    const completed = current.filter(run => run.complete).length
    return [biome, {
      samples: floors.length,
      probeCompletion: { completed, total: current.length, rate: rate(completed, current.length) },
      recipes: ranked(recipes),
      routeChoiceCost: ranked(routeChoiceCost),
      encounterRoles: ranked(encounterRoles),
      terrain: { generated: ranked(terrain), interactions: ranked(interactions) },
      boonTiming: ranked(boonTiming),
      boonUsefulness: ranked(boonUsefulness),
      events: { scheduled: ranked(scheduledEvents), outcomes: ranked(outcomes) },
      deaths: ranked(deaths),
      bossPhases: { expected: ranked(expectedBossPhases), observed: ranked(observedBossPhases) },
      quality: { valid: errors.length === 0, errors }
    } satisfies BalanceBiomeDashboard]
  })) as Record<Biome, BalanceBiomeDashboard>
  const errors = BIOME_POOL.flatMap(biome => biomes[biome].quality.errors.map(error => `${biome}: ${error}`))
  return { version: 2, mode: 'omniscient-clear', seeds: [...seeds], biomes, acceptance: { valid: errors.length === 0, errors } }
}
