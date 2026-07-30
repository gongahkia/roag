import corpusManifest from './autoplay-seed-corpus.json'
import type { AutoplayMode, Biome } from './types'

export const AUTOPLAY_SEED_CORPUS_VERSION = 1 as const
export const AUTOPLAY_SEED_CORPUS_PARTITIONS = ['development', 'held-out'] as const
export type AutoplaySeedCorpusPartition = typeof AUTOPLAY_SEED_CORPUS_PARTITIONS[number]
export type CampaignSeedValidation = 'non-error'
export interface AutoplaySeedCorpusEntry { seed: number; gameVersion: string; routeConfiguration: { id: string; areaOrder: Biome[] }; policyModes: Array<Exclude<AutoplayMode, 'off'>>; turnBudget: number; expectedValidation: CampaignSeedValidation }
export interface AutoplaySeedCorpus { version: typeof AUTOPLAY_SEED_CORPUS_VERSION; partitions: Record<AutoplaySeedCorpusPartition, AutoplaySeedCorpusEntry[]> }

const biomeSet = new Set<Biome>(['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'])
const policyModeSet = new Set<Exclude<AutoplayMode, 'off'>>(['visible', 'omniscient'])

const object = (value: unknown): Record<string, unknown> | undefined => value !== null && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : undefined
const string = (value: unknown, name: string): string => { if (typeof value !== 'string' || !value) throw new Error(`invalid autoplay seed corpus ${name}`); return value }
const positiveInteger = (value: unknown, name: string): number => { if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1) throw new Error(`invalid autoplay seed corpus ${name}`); return value }

export const parseAutoplaySeedCorpus = (raw: unknown): AutoplaySeedCorpus => {
  const manifest = object(raw)
  if (!manifest) throw new Error('invalid autoplay seed corpus manifest')
  if (manifest.version !== AUTOPLAY_SEED_CORPUS_VERSION) throw new Error(`unsupported autoplay seed corpus version: ${manifest.version}`)
  const gameVersion = string(manifest.gameVersion, 'gameVersion')
  const routeConfiguration = string(manifest.routeConfiguration, 'routeConfiguration')
  const policyModes = manifest.policyModes
  if (!Array.isArray(policyModes) || !policyModes.length || policyModes.some(mode => !policyModeSet.has(mode as Exclude<AutoplayMode, 'off'>))) throw new Error('invalid autoplay seed corpus policyModes')
  const uniqueModes = [...new Set(policyModes)]
  if (uniqueModes.length !== policyModes.length) throw new Error('duplicate autoplay seed corpus policy mode')
  const turnBudget = positiveInteger(manifest.turnBudget, 'turnBudget')
  if (manifest.expectedValidation !== 'non-error') throw new Error(`invalid autoplay seed corpus expectedValidation: ${manifest.expectedValidation}`)
  const rawPartitions = object(manifest.partitions)
  if (!rawPartitions) throw new Error('invalid autoplay seed corpus partitions')
  const unknownPartitions = Object.keys(rawPartitions).filter(partition => !AUTOPLAY_SEED_CORPUS_PARTITIONS.includes(partition as AutoplaySeedCorpusPartition))
  if (unknownPartitions.length) throw new Error(`unknown autoplay seed corpus partition: ${unknownPartitions.sort().join(',')}`)
  const seeds = new Set<number>()
  const entries = (partition: AutoplaySeedCorpusPartition): AutoplaySeedCorpusEntry[] => {
    const records = rawPartitions[partition]
    if (!Array.isArray(records) || records.length !== 48) throw new Error(`autoplay seed corpus ${partition} must contain exactly 48 seeds`)
    return records.map((record, index) => {
      if (!Array.isArray(record) || record.length !== 2) throw new Error(`invalid autoplay seed corpus ${partition}[${index}]`)
      const seed = positiveInteger(record[0], `${partition}[${index}].seed`)
      if (seeds.has(seed)) throw new Error(`duplicate autoplay seed corpus seed: ${seed}`)
      seeds.add(seed)
      const route = record[1]
      if (!Array.isArray(route) || route.length !== 4 || route.some(biome => !biomeSet.has(biome as Biome)) || new Set(route).size !== route.length) throw new Error(`invalid autoplay seed corpus ${partition}[${index}].route`)
      return { seed, gameVersion, routeConfiguration: { id: routeConfiguration, areaOrder: [...route] as Biome[] }, policyModes: [...uniqueModes] as Array<Exclude<AutoplayMode, 'off'>>, turnBudget, expectedValidation: 'non-error' }
    })
  }
  return { version: AUTOPLAY_SEED_CORPUS_VERSION, partitions: { development: entries('development'), 'held-out': entries('held-out') } }
}

export const autoplaySeedCorpusManifest: unknown = corpusManifest
export const AUTOPLAY_SEED_CORPUS = parseAutoplaySeedCorpus(autoplaySeedCorpusManifest)

export const autoplaySeedCorpusPartition = (partition: string): AutoplaySeedCorpusEntry[] => {
  if (!AUTOPLAY_SEED_CORPUS_PARTITIONS.includes(partition as AutoplaySeedCorpusPartition)) throw new Error(`unknown autoplay seed corpus partition: ${partition}`)
  return AUTOPLAY_SEED_CORPUS.partitions[partition as AutoplaySeedCorpusPartition].map(entry => ({ ...entry, routeConfiguration: { ...entry.routeConfiguration, areaOrder: [...entry.routeConfiguration.areaOrder] }, policyModes: [...entry.policyModes] }))
}

export const autoplaySeedCorpusEntry = (partition: string, seed: number): AutoplaySeedCorpusEntry => {
  const entry = autoplaySeedCorpusPartition(partition).find(candidate => candidate.seed === seed)
  if (!entry) throw new Error(`unknown autoplay seed corpus seed: ${seed}`)
  return entry
}
