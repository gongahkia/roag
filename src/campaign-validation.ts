import { runAutoplay, type AutoplayReport } from './autoplay-runner'
import { newRun } from './engine'
import { generateAreaFloor, validateGeneration } from './world'
import type { Biome } from './types'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins']
export const CAMPAIGN_CLEARANCE_TURN_LIMIT = 3200
export const CAMPAIGN_SEED_RETRY_LIMIT = 32

export type CampaignValidationKind = 'clear' | 'generation-invalid' | 'dead' | 'stalled' | 'turn-limit' | 'error'
export interface CampaignValidation {
  requestedSeed: number
  seed: number
  kind: CampaignValidationKind
  accepted: boolean
  report?: AutoplayReport
  errors: string[]
}
export interface CampaignValidationOptions { diagnostic?: boolean }

const normalizeSeed = (seed: number): number => seed >>> 0 & 0x7fffffff

export const validateCampaignTopology = (seed: number): string[] => {
  const errors: string[] = []
  for (const biome of biomes) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
    try {
      const validation = validateGeneration(generateAreaFloor(seed, biome, areaFloor))
      for (const error of validation.errors) errors.push(`${biome}:${areaFloor + 1}:${error}`)
    } catch (caught) { errors.push(`${biome}:${areaFloor + 1}:${caught instanceof Error ? caught.message : String(caught)}`) }
  }
  return errors
}

export const validateCampaignSeed = (seed: number, turnLimit = CAMPAIGN_CLEARANCE_TURN_LIMIT, options: CampaignValidationOptions = {}): CampaignValidation => {
  const normalized = normalizeSeed(seed)
  const errors = validateCampaignTopology(normalized)
  if (errors.length) return { requestedSeed: normalized, seed: normalized, kind: 'generation-invalid', accepted: false, errors }
  const report = runAutoplay(newRun(normalized), { mode: 'omniscient', policy: 'clear', turnLimit, captureTrace: Boolean(options.diagnostic), includeState: Boolean(options.diagnostic), includeDebug: Boolean(options.diagnostic) })
  const kind: CampaignValidationKind = report.campaignComplete ? 'clear' : report.outcome === 'complete' ? 'error' : report.outcome
  return { requestedSeed: normalized, seed: normalized, kind, accepted: kind === 'clear', report, errors: [] }
}

export const findPlayableCampaignSeed = (requestedSeed: number, retryLimit = CAMPAIGN_SEED_RETRY_LIMIT, turnLimit = CAMPAIGN_CLEARANCE_TURN_LIMIT): CampaignValidation => {
  const requested = normalizeSeed(requestedSeed)
  let last: CampaignValidation | undefined
  for (let offset = 0; offset < retryLimit; offset++) {
    const seed = normalizeSeed(requested + offset)
    const result = validateCampaignSeed(seed, turnLimit)
    if (result.accepted) return { ...result, requestedSeed: requested }
    last = result
  }
  return { ...(last ?? { seed: requested, kind: 'generation-invalid' as const, report: undefined, errors: ['no seed attempts'] }), requestedSeed: requested, accepted: false, errors: [...(last?.errors ?? []), `no accepted seed in ${retryLimit} attempts`] }
}

export const findStructurallyPlayableCampaignSeed = (requestedSeed: number, retryLimit = CAMPAIGN_SEED_RETRY_LIMIT): CampaignValidation => {
  const requested = normalizeSeed(requestedSeed)
  let last: CampaignValidation | undefined
  for (let offset = 0; offset < retryLimit; offset++) {
    const seed = normalizeSeed(requested + offset)
    const errors = validateCampaignTopology(seed)
    const result: CampaignValidation = { requestedSeed: requested, seed, kind: errors.length ? 'generation-invalid' : 'clear', accepted: errors.length === 0, errors }
    if (result.accepted) return result
    last = result
  }
  return { ...(last ?? { seed: requested, kind: 'generation-invalid' as const, errors: ['no seed attempts'] }), requestedSeed: requested, accepted: false, errors: [...(last?.errors ?? []), `no structurally valid seed in ${retryLimit} attempts`] }
}
