import { generateRouteContract } from '../src/route-contract'
import { escalationFor } from '../src/escalation'
import { areaFloorIndex, generateAreaFloor, generationValidationFailures, layoutFor, validateGeneration } from '../src/world'
import type { Biome } from '../src/types'

const seedCount = Number(process.env.GENERATION_SWEEP_SEEDS ?? 1000)
if (!Number.isInteger(seedCount) || seedCount < 1) throw new Error('GENERATION_SWEEP_SEEDS must be a positive integer')
const startSeed = Number(process.env.GENERATION_SWEEP_START ?? 0)
if (!Number.isInteger(startSeed) || startSeed < 0) throw new Error('GENERATION_SWEEP_START must be a non-negative integer')
const progressEvery = Number(process.env.GENERATION_SWEEP_PROGRESS_EVERY ?? 0)
if (!Number.isInteger(progressEvery) || progressEvery < 0) throw new Error('GENERATION_SWEEP_PROGRESS_EVERY must be a non-negative integer')
const knownBiomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
const requestedBiomes = (process.env.GENERATION_SWEEP_BIOMES ?? 'mine').split(',').map(biome => biome.trim()).filter(Boolean)
if (!requestedBiomes.length || requestedBiomes.some(biome => !knownBiomes.includes(biome as Biome))) throw new Error('GENERATION_SWEEP_BIOMES must contain known comma-separated biomes')
const migratedBiomes = requestedBiomes as Biome[]
for (const biome of migratedBiomes) for (let seed = startSeed; seed < startSeed + seedCount; seed++) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
  const recipe = layoutFor(seed, biome, areaFloor)
  const escalation = escalationFor(seed, biome, areaFloor)
  const contract = generateRouteContract({ campaignSeed: seed, floorIndex: areaFloorIndex(biome, areaFloor), biome, areaFloor, recipeId: recipe, escalationVariant: `${escalation.arcId}:${escalation.phase}` })
  try {
    const floor = generateAreaFloor(seed, biome, areaFloor)
    const validation = validateGeneration(floor)
    if (!validation.valid) throw new Error(generationValidationFailures(floor, validation).map(failure => `route=${failure.routeNode} invariant=${failure.invariant}`).join('; '))
  } catch (error) {
    throw new Error(`generation sweep failed seed=${seed} biome=${biome} floor=${areaFloor} recipe=${recipe} contract=${contract.id}: ${error instanceof Error ? error.message : String(error)}`)
  }
  if (progressEvery && areaFloor === 3 && (seed - startSeed + 1) % progressEvery === 0) console.log(`generation sweep progress: ${seed - startSeed + 1}/${seedCount}`)
}
console.log(`generation sweep passed: ${startSeed}-${startSeed + seedCount - 1} across ${migratedBiomes.join(',')}`)
