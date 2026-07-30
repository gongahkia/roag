import { replayAutoplayTrace } from './autoplay-trace-replay'
import { runAutoplay } from './autoplay-runner'
import { completeCampaignTier, continueCampaignRoute, initialCampaignRoute } from './engine/campaign'
import { companionLeadForRescue, loseCompanionForRescue } from './engine/companions'
import { newHero, newRun, newSeededCampaignRun } from './engine/run'
import { takeSecretShortcut } from './engine/shortcuts'
import { migrateCampaignRoute, migrateRunRecord } from './storage'
import type { CampaignDifficultyPackageMetadata, CampaignRouteState, CampaignTier, Hero, Records, RescuedNpc } from './types'
import { getTile } from './world'
import { snapshotCampaignCarryover, transferCampaignCarryover } from './engine/carryover'

export const NEW_GAME_PLUS_LIFECYCLE_VERSION = 1 as const
export const NEW_GAME_PLUS_LIFECYCLE_MODES = ['smoke', 'full'] as const
export type NewGamePlusLifecycleMode = typeof NEW_GAME_PLUS_LIFECYCLE_MODES[number]
export const NEW_GAME_PLUS_LIFECYCLE_CORPUS: Record<NewGamePlusLifecycleMode, readonly number[]> = { smoke: [7], full: [0, 7, 41, 99] }
const tiers: readonly CampaignTier[] = ['base', 'ngPlus', 'ngPlusPlus']

export interface NewGamePlusLifecycleFixture { hero: Hero; campaign: CampaignRouteState; records: Records }
export interface NewGamePlusLifecycleFailure { seed: number; tier: CampaignTier; stage: 'reload' | 'difficulty' | 'replay' | 'optional-content' | 'victory' | 'continuation' | 'terminal-cap'; error: string }
export interface NewGamePlusLifecycleStage { tier: CampaignTier; replayValid: boolean; reloadValid: boolean; optionalContentValid: boolean; difficultyPackage?: CampaignDifficultyPackageMetadata; victoryRecorded: boolean; continuationPending: boolean; carryoverPreserved: boolean; carryoverDiagnostics: number }
export interface NewGamePlusLifecycleRun { seed: number; accepted: boolean; stages: NewGamePlusLifecycleStage[]; terminalCap: boolean; failures: NewGamePlusLifecycleFailure[] }
export interface NewGamePlusLifecycleReport { version: typeof NEW_GAME_PLUS_LIFECYCLE_VERSION; mode: NewGamePlusLifecycleMode; seeds: number[]; runs: NewGamePlusLifecycleRun[]; accepted: boolean; failures: NewGamePlusLifecycleFailure[] }

const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const carryoverState = (hero: Hero, campaign: CampaignRouteState, records: Records) => ({ hero: structuredClone(hero), rescuedNpcs: structuredClone(campaign.rescuedNpcs), companions: structuredClone(campaign.companions), companionControlMode: campaign.companionControlMode, companionControlHistory: structuredClone(campaign.companionControlHistory), lineageEvents: structuredClone(campaign.lineageEvents), legacyRecords: structuredClone(campaign.legacyRecords), alignment: structuredClone(campaign.alignment), reputation: structuredClone(campaign.reputation), records: structuredClone(records) })

export const newGamePlusLifecycleFixture = (seed: number): NewGamePlusLifecycleFixture => {
  const hero = newHero({ name: 'Lifecycle Courier', origin: 'tidebound', calling: 'pathmaker', deathMode: 'ironTrail' })
  hero.gold = 487
  hero.bombs = 2
  hero.ropes = 1
  hero.keys = 3
  hero.xp = 333
  hero.level = 8
  hero.inventory = ['tonic', 'tonic', 'focusTonic', 'rock', 'bombPack', 'ropeBundle', 'ember', 'sight', 'tideSpear', 'cap', 'boots', 'sunseal']
  hero.traversalTools = ['stoneWedge', 'cordAnchor']
  hero.relics = ['ashCircuit']
  hero.relicCharges = { ashCircuit: 0 }
  hero.boons = { windfall: 2 }
  hero.boonEvolutions = { windfall: 1 }
  const activeRescue: RescuedNpc = { id: 'rescue:mine:2:mika', name: 'Mika', biome: 'mine', floor: 2 }
  const lostRescue: RescuedNpc = { id: 'rescue:wilds:1:bo', name: 'Bo', biome: 'wilds', floor: 1 }
  const injured = companionLeadForRescue(activeRescue, 'direct')
  injured.rosterStatus = 'benched'
  injured.injury = 'injured'
  injured.abilityState = { cooldowns: { intercept: 2 }, retired: ['old-call'] }
  injured.toolState = { cooldown: 4, retired: true }
  const lost = loseCompanionForRescue(companionLeadForRescue(lostRescue, 'direct'), lostRescue.id)
  const campaign = { ...initialCampaignRoute(seed, 'direct'), rescuedNpcs: [activeRescue], companions: [injured, lost], companionControlMode: 'direct' as const, companionControlHistory: [{ sequence: 0, mode: 'direct' as const, source: 'creation' as const }], lineageEvents: [{ id: 'sacrifice:bo', kind: 'npcSacrifice' as const, npcId: lostRescue.id, npcName: 'Bo', biome: 'wilds' as const, floor: 1, gateId: 'wilds-caverns-pass', seed }], legacyRecords: [{ id: `legacy:${seed}`, heirName: 'Lifecycle Courier', biome: 'mine' as const, floor: 3, seed }], alignment: { kami: 2, villagePact: 4 }, reputation: { trailfolk: 3, kami: -2 } }
  const records: Records = { bestDepth: 17, wins: 3, deaths: 2, runs: [{ seed, floor: 4, score: 221, won: true, date: '2026-07-30T00:00:00.000Z' }], analyses: [] }
  return { hero, campaign, records }
}

const optionalContentValid = (seed: number, hero: Hero, campaign: CampaignRouteState): boolean => {
  const state = newRun(seed, 'mine', 1, hero, campaign.rescuedNpcs, campaign.legacyRecords, campaign.areaOrder, campaign.cycle, campaign.companions, 'permadeath')
  const route = state.floor.secretRoutes?.find(candidate => candidate.kind === 'rare-transition')
  const room = route ? state.floor.secretRooms?.find(candidate => candidate.id === route.roomId) : undefined
  if (!route || !room) return false
  room.discovery = { channel: 'terrain', turn: 0 }
  const entry = getTile(state.floor, route.entry.x, route.entry.y)
  if (!entry) return false
  entry.kind = 'floor'
  state.hero.x = route.entry.x
  state.hero.y = route.entry.y
  const events = takeSecretShortcut(state) ?? []
  const restored = migrateRunRecord(JSON.parse(JSON.stringify(state)))
  return events.some(event => event.type === 'floor') && restored !== undefined && restored.floor.difficulty?.campaignTier === campaign.cycle.currentTier && restored.floor.difficulty.difficultyPackage?.tier === campaign.cycle.currentTier
}

export const validateNewGamePlusLifecycle = (seed: number): NewGamePlusLifecycleRun => {
  let { hero, campaign, records } = newGamePlusLifecycleFixture(seed)
  const failures: NewGamePlusLifecycleFailure[] = []
  const stages: NewGamePlusLifecycleStage[] = []
  const fail = (tier: CampaignTier, stage: NewGamePlusLifecycleFailure['stage'], error: string): void => { failures.push({ seed, tier, stage, error }) }
  for (const tier of tiers) {
    if (campaign.cycle.currentTier !== tier) fail(tier, 'continuation', `expected ${tier}, received ${campaign.cycle.currentTier}`)
    const state = newSeededCampaignRun(seed, hero, campaign.rescuedNpcs, campaign.legacyRecords, campaign.cycle, campaign.companions, 'permadeath')
    state.alignment = structuredClone(campaign.alignment)
    state.reputation = structuredClone(campaign.reputation ?? { trailfolk: 0, kami: 0 })
    const restoredRun = migrateRunRecord(JSON.parse(JSON.stringify(state)))
    const restoredCampaign = migrateCampaignRoute(JSON.parse(JSON.stringify(campaign)))
    const reloadValid = Boolean(restoredRun && same(restoredCampaign, campaign) && same(restoredRun.replayHero, hero) && same(restoredRun.companions, campaign.companions))
    if (!reloadValid) fail(tier, 'reload', 'save reload changed tier, carried state, or replay hero')
    if (!restoredRun) {
      stages.push({ tier, replayValid: false, reloadValid, optionalContentValid: false, victoryRecorded: false, continuationPending: false, carryoverPreserved: false, carryoverDiagnostics: campaign.carryoverDiagnostics.length })
      continue
    }
    hero = restoredRun.hero
    campaign = restoredCampaign
    const difficultyPackage = state.floor.difficulty?.difficultyPackage
    const difficultyValid = state.floor.difficulty?.campaignTier === tier && difficultyPackage?.tier === tier
    if (!difficultyValid) fail(tier, 'difficulty', 'resolved tier package is missing or mismatched')
    const probe = runAutoplay(restoredRun, { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true })
    const replayValid = Boolean(probe.traceDocument && replayAutoplayTrace(probe.traceDocument).valid)
    if (!replayValid) fail(tier, 'replay', 'carried-state trace diverged during replay')
    const optionalValid = optionalContentValid(seed, hero, campaign)
    if (!optionalValid) fail(tier, 'optional-content', 'secret shortcut transition or reload failed')
    const completed = completeCampaignTier(campaign.cycle)
    const victoryRecorded = completed.completedTiers.includes(tier)
    if (!victoryRecorded) fail(tier, 'victory', 'tier victory was not recorded')
    campaign = { ...campaign, cycle: completed }
    records = { ...records, wins: records.wins + 1, runs: [{ seed, floor: state.floor.index + 1, score: hero.gold, won: true, date: '2026-07-31T00:00:00.000Z' }, ...records.runs].slice(0, 20) }
    const continuationPending = tier !== 'ngPlusPlus'
    let carryoverPreserved = true
    if (continuationPending) {
      const before = carryoverState(hero, campaign, records)
      const pending = migrateCampaignRoute(JSON.parse(JSON.stringify(campaign)))
      if (!same(pending, campaign)) fail(tier, 'continuation', 'partially completed tier changed after reload')
      const snapshot = snapshotCampaignCarryover(hero, pending, records)
      const transfer = transferCampaignCarryover(continueCampaignRoute(pending), snapshot)
      const retry = transferCampaignCarryover(transfer.campaign, snapshot)
      carryoverPreserved = same(carryoverState(transfer.hero, transfer.campaign, transfer.records), before) && transfer.campaign.carryoverDiagnostics.length === (tier === 'base' ? 1 : 2)
      carryoverPreserved &&= same(retry.diagnostic, transfer.diagnostic) && retry.campaign.carryoverDiagnostics.length === transfer.campaign.carryoverDiagnostics.length
      if (!carryoverPreserved) fail(tier, 'continuation', 'carryover changed or duplicated across deferred continuation')
      hero = transfer.hero
      campaign = transfer.campaign
      records = transfer.records
    } else {
      try { continueCampaignRoute(campaign); carryoverPreserved = false; fail(tier, 'terminal-cap', 'NG++ accepted a continuation') }
      catch (error) { if (!(error instanceof Error) || !error.message.includes('NG++ completed cap reached')) { carryoverPreserved = false; fail(tier, 'terminal-cap', error instanceof Error ? error.message : String(error)) } }
    }
    stages.push({ tier, replayValid, reloadValid, optionalContentValid: optionalValid, ...(difficultyPackage ? { difficultyPackage } : {}), victoryRecorded, continuationPending, carryoverPreserved, carryoverDiagnostics: campaign.carryoverDiagnostics.length })
  }
  const terminalCap = campaign.cycle.currentTier === 'ngPlusPlus' && campaign.cycle.completedCap
  if (!terminalCap) fail('ngPlusPlus', 'terminal-cap', 'terminal campaign cap was not recorded')
  return { seed, accepted: failures.length === 0, stages, terminalCap, failures }
}

export const validateNewGamePlusLifecycleCorpus = (mode: NewGamePlusLifecycleMode = 'smoke'): NewGamePlusLifecycleReport => {
  const seeds = [...NEW_GAME_PLUS_LIFECYCLE_CORPUS[mode]]
  const runs = seeds.map(validateNewGamePlusLifecycle)
  const failures = runs.flatMap(run => run.failures)
  return { version: NEW_GAME_PLUS_LIFECYCLE_VERSION, mode, seeds, runs, accepted: failures.length === 0, failures }
}
