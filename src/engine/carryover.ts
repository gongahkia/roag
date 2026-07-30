import type { CampaignCarryoverDiagnostic, CampaignCarryoverSnapshot, CampaignRouteState, CampaignTier, Companion, Hero, LegacyRecord, LineageEvent, Records, RescuedNpc } from '../types'
import { cloneCompanions } from './companions'

const nextTier: Partial<Record<CampaignTier, CampaignTier>> = { base: 'ngPlus', ngPlus: 'ngPlusPlus' }
const cloneRescues = (rescues: readonly RescuedNpc[]): RescuedNpc[] => rescues.map(rescue => ({ ...rescue }))
const cloneLineage = (events: readonly LineageEvent[]): LineageEvent[] => events.map(event => ({ ...event }))
const cloneLegacy = (records: readonly LegacyRecord[]): LegacyRecord[] => records.map(record => ({ ...record }))
const multisetDifference = <T>(before: readonly T[], after: readonly T[]): T[] => {
  const remaining = [...after]
  return before.flatMap(value => {
    const index = remaining.indexOf(value)
    if (index >= 0) { remaining.splice(index, 1); return [] }
    return [value]
  })
}
const rosterDiff = (before: readonly Companion[], after: readonly Companion[]): CampaignCarryoverDiagnostic['roster'] => ({
  before: cloneCompanions(before),
  after: cloneCompanions(after),
  added: after.filter(companion => !before.some(previous => previous.id === companion.id)).map(companion => companion.id),
  removed: before.filter(companion => !after.some(next => next.id === companion.id)).map(companion => companion.id),
  changed: after.filter(companion => {
    const previous = before.find(candidate => candidate.id === companion.id)
    return previous !== undefined && JSON.stringify(previous) !== JSON.stringify(companion)
  }).map(companion => companion.id)
})
const carryoverDiagnostic = (fromTier: CampaignTier, toTier: CampaignTier, beforeHero: Hero, afterHero: Hero, beforeCompanions: readonly Companion[], afterCompanions: readonly Companion[]): CampaignCarryoverDiagnostic => ({
  version: 1,
  fromTier,
  toTier,
  inventory: { before: [...beforeHero.inventory], after: [...afterHero.inventory], added: multisetDifference(afterHero.inventory, beforeHero.inventory), removed: multisetDifference(beforeHero.inventory, afterHero.inventory) },
  roster: rosterDiff(beforeCompanions, afterCompanions)
})
export const cloneCarryoverDiagnostics = (diagnostics: readonly CampaignCarryoverDiagnostic[]): CampaignCarryoverDiagnostic[] => diagnostics.map(diagnostic => ({ version: diagnostic.version, fromTier: diagnostic.fromTier, toTier: diagnostic.toTier, inventory: { before: [...diagnostic.inventory.before], after: [...diagnostic.inventory.after], added: [...diagnostic.inventory.added], removed: [...diagnostic.inventory.removed] }, roster: rosterDiff(diagnostic.roster.before, diagnostic.roster.after) }))
export const snapshotCampaignCarryover = (hero: Hero, campaign: CampaignRouteState, records: Records): CampaignCarryoverSnapshot => ({
  version: 1,
  fromTier: campaign.cycle.currentTier,
  hero: structuredClone(hero),
  companions: cloneCompanions(campaign.companions, campaign.rescuedNpcs),
  rescuedNpcs: cloneRescues(campaign.rescuedNpcs),
  lineageEvents: cloneLineage(campaign.lineageEvents),
  legacyRecords: cloneLegacy(campaign.legacyRecords),
  alignment: { ...campaign.alignment },
  reputation: { trailfolk: campaign.reputation?.trailfolk ?? 0, kami: campaign.reputation?.kami ?? 0 },
  records: structuredClone(records)
})
export interface CampaignCarryoverTransfer { hero: Hero; campaign: CampaignRouteState; records: Records; diagnostic: CampaignCarryoverDiagnostic }
export const transferCampaignCarryover = (target: CampaignRouteState, snapshot: CampaignCarryoverSnapshot): CampaignCarryoverTransfer => {
  const toTier = nextTier[snapshot.fromTier]
  if (!toTier || target.cycle.currentTier !== toTier) throw new Error(`cannot transfer campaign carryover: expected ${toTier ?? 'no further tier'}, received ${target.cycle.currentTier}`)
  const hero = structuredClone(snapshot.hero)
  const companions = cloneCompanions(snapshot.companions, snapshot.rescuedNpcs)
  const diagnostic = carryoverDiagnostic(snapshot.fromTier, toTier, snapshot.hero, hero, snapshot.companions, companions)
  const diagnostics = cloneCarryoverDiagnostics(target.carryoverDiagnostics)
  const existing = diagnostics.find(entry => entry.fromTier === snapshot.fromTier && entry.toTier === toTier)
  const carryoverDiagnostics = existing ? diagnostics : [...diagnostics, diagnostic].slice(-12)
  return {
    hero,
    records: structuredClone(snapshot.records),
    campaign: { ...target, rescuedNpcs: cloneRescues(snapshot.rescuedNpcs), companions, carryoverDiagnostics, lineageEvents: cloneLineage(snapshot.lineageEvents), legacyRecords: cloneLegacy(snapshot.legacyRecords), alignment: { ...snapshot.alignment }, reputation: { ...snapshot.reputation } },
    diagnostic: existing ? cloneCarryoverDiagnostics([existing])[0]! : diagnostic
  }
}
