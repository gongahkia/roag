import { generateAreaFloor, validateGeneration } from '../src/world'

const seedCount = Number(process.env.GENERATION_SWEEP_SEEDS ?? 1000)
if (!Number.isInteger(seedCount) || seedCount < 1) throw new Error('GENERATION_SWEEP_SEEDS must be a positive integer')
const migratedBiomes = ['mine'] as const
for (const biome of migratedBiomes) for (let seed = 0; seed < seedCount; seed++) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
  const validation = validateGeneration(generateAreaFloor(seed, biome, areaFloor))
  if (!validation.valid) throw new Error(`generation sweep failed seed=${seed} biome=${biome} floor=${areaFloor}: ${validation.errors.join('; ')}`)
}
console.log(`generation sweep passed: ${seedCount} seeds across ${migratedBiomes.join(',')}`)
