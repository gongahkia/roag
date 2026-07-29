import type { CampaignRouteState, LegacyRecord, RunState } from '../types'
import { appendLegacyRecord } from './campaign'

export const legacyRecordForDeath = (state: RunState, heirName: string): LegacyRecord => ({
  id: `legacy:${state.seed}:${state.floor.index}:${state.turn}`,
  heirName,
  biome: state.area ?? state.floor.biome,
  floor: state.areaFloor ?? state.floor.index % 4,
  seed: state.seed
})

export const recordDeath = (campaign: CampaignRouteState, state: RunState, heirName: string): CampaignRouteState => appendLegacyRecord(campaign, legacyRecordForDeath(state, heirName))
