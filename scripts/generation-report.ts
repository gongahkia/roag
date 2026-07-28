import { mkdir, writeFile } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import { measureGeneration, summarizeGenerationMetrics } from '../src/generation-metrics'
import { generateRouteContract } from '../src/route-contract'
import type { Biome } from '../src/types'
import { areaFloorIndex, generateAreaFloor, layoutFor, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from '../src/world'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
const parseNumbers = (value: string | undefined, fallback: readonly number[], label: string): number[] => {
  const parsed = value?.split(',').filter(Boolean).map(Number) ?? [...fallback]
  if (!parsed.length || parsed.some(number => !Number.isInteger(number) || number < 0)) throw new Error(`invalid ${label}: ${value}`)
  return [...new Set(parsed)]
}
const parseBiomes = (value: string | undefined): Biome[] => {
  const parsed = value?.split(',').filter(Boolean) ?? ['mine']
  if (!parsed.length || parsed.some(biome => !biomes.includes(biome as Biome))) throw new Error(`invalid GENERATION_REPORT_BIOMES: ${value}`)
  return [...new Set(parsed as Biome[])]
}

const seeds = parseNumbers(process.env.GENERATION_REPORT_SEEDS, [7, 42, 999], 'GENERATION_REPORT_SEEDS')
const floors = parseNumbers(process.env.GENERATION_REPORT_FLOORS, [0, 1, 2, 3], 'GENERATION_REPORT_FLOORS')
if (floors.some(floor => floor > 3)) throw new Error(`invalid GENERATION_REPORT_FLOORS: ${process.env.GENERATION_REPORT_FLOORS}`)
const selectedBiomes = parseBiomes(process.env.GENERATION_REPORT_BIOMES)
const requestedPath = process.env.GENERATION_REPORT_PATH
const output = resolve(requestedPath || `generation-reports/generation-${Date.now()}.json`)
const samples = []
const failures: Array<{ seed: number; biome: Biome; floor: number; recipe: string; contract: string; error: string }> = []

for (const seed of seeds) for (const biome of selectedBiomes) for (const areaFloor of floors) {
  const recipe = layoutFor(seed, biome, areaFloor)
  const contract = generateRouteContract({ campaignSeed: seed, floorIndex: areaFloorIndex(biome, areaFloor), biome, areaFloor, recipeId: recipe, escalationVariant: `stage-${areaFloor + 1}` })
  try {
    const floor = generateAreaFloor(seed, biome, areaFloor)
    samples.push(measureGeneration({ floor, route: routeContractDebug(floor) ?? contract, macro: macroRecipeDebug(floor), placements: placementDebug(floor), validation: validateGeneration(floor) }))
  } catch (error) {
    failures.push({ seed, biome, floor: areaFloor, recipe, contract: contract.id, error: error instanceof Error ? error.message : String(error) })
  }
}

const summary = summarizeGenerationMetrics(samples)
const acceptance = { valid: !failures.length && summary.acceptance.valid, errors: [...summary.acceptance.errors, ...failures.map(failure => `seed=${failure.seed} biome=${failure.biome} floor=${failure.floor} recipe=${failure.recipe} contract=${failure.contract}: ${failure.error}`)] }
const report = { version: 1, config: { seeds, biomes: selectedBiomes, floors }, summary: { ...summary, acceptance }, failures, samples }
await mkdir(dirname(output), { recursive: true })
await writeFile(output, `${JSON.stringify(report, null, 2)}\n`)
console.log(JSON.stringify({ report: output, samples: samples.length, acceptance }, null, 2))
if (!acceptance.valid) process.exitCode = 1
