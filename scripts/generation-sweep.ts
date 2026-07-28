import { generateRouteContract } from '../src/route-contract'
import { escalationFor } from '../src/escalation'
import { areaFloorIndex, generateAreaFloor, layoutFor, validateGeneration } from '../src/world'

const seedCount = Number(process.env.GENERATION_SWEEP_SEEDS ?? 1000)
if (!Number.isInteger(seedCount) || seedCount < 1) throw new Error('GENERATION_SWEEP_SEEDS must be a positive integer')
const migratedBiomes = ['mine'] as const
for (const biome of migratedBiomes) for (let seed = 0; seed < seedCount; seed++) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
  const recipe = layoutFor(seed, biome, areaFloor)
  const escalation = escalationFor(seed, biome, areaFloor)
  const contract = generateRouteContract({ campaignSeed: seed, floorIndex: areaFloorIndex(biome, areaFloor), biome, areaFloor, recipeId: recipe, escalationVariant: `${escalation.arcId}:${escalation.phase}` })
  try {
    const validation = validateGeneration(generateAreaFloor(seed, biome, areaFloor))
    if (!validation.valid) throw new Error(validation.errors.join('; '))
  } catch (error) {
    throw new Error(`generation sweep failed seed=${seed} biome=${biome} floor=${areaFloor} recipe=${recipe} contract=${contract.id}: ${error instanceof Error ? error.message : String(error)}`)
  }
}
console.log(`generation sweep passed: ${seedCount} seeds across ${migratedBiomes.join(',')}`)
