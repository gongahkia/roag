import { companionLeadForRescue } from './engine/companions'
import { autonomousCompanionDecision } from './engine/companion-autonomy'
import { isCompanionActor, synchronizePartyActors } from './engine/party'
import { createRun } from './test/factories'
import { indexOf, type CompanionControlMode, type CompanionDeathMode, type CompanionRole, type RunState } from './types'

export type PartyAutoplayFixtureRisk = 'intercept' | 'observation' | 'terrain' | 'hazard' | 'direct-control' | 'gate-sacrifice' | 'injury' | 'permadeath'
export interface PartyAutoplayFixture { partition: 'development' | 'held-out'; id: string; seed: number; role: CompanionRole; controlMode: CompanionControlMode; deathMode: CompanionDeathMode; risk: PartyAutoplayFixtureRisk; expectedAction?: string }

export const PARTY_AUTOPLAY_FIXTURES: readonly PartyAutoplayFixture[] = [
  { partition: 'development', id: 'guard-intercept', seed: 1901, role: 'guard', controlMode: 'autonomous', deathMode: 'injury', risk: 'intercept', expectedAction: 'intercept' },
  { partition: 'development', id: 'scout-observe', seed: 1902, role: 'scout', controlMode: 'autonomous', deathMode: 'injury', risk: 'observation', expectedAction: 'observe' },
  { partition: 'development', id: 'pathmaker-terrain', seed: 1903, role: 'pathmaker', controlMode: 'autonomous', deathMode: 'injury', risk: 'terrain', expectedAction: 'stabilizeTerrain' },
  { partition: 'development', id: 'ritualist-hazard', seed: 1904, role: 'ritualist', controlMode: 'autonomous', deathMode: 'injury', risk: 'hazard', expectedAction: 'stabilizeHazard' },
  { partition: 'development', id: 'guard-injury', seed: 1905, role: 'guard', controlMode: 'autonomous', deathMode: 'injury', risk: 'injury', expectedAction: 'intercept' },
  { partition: 'held-out', id: 'guard-direct', seed: 2901, role: 'guard', controlMode: 'direct', deathMode: 'injury', risk: 'direct-control' },
  { partition: 'held-out', id: 'scout-direct', seed: 2906, role: 'scout', controlMode: 'direct', deathMode: 'injury', risk: 'direct-control' },
  { partition: 'held-out', id: 'pathmaker-direct', seed: 2907, role: 'pathmaker', controlMode: 'direct', deathMode: 'injury', risk: 'direct-control' },
  { partition: 'held-out', id: 'ritualist-direct', seed: 2908, role: 'ritualist', controlMode: 'direct', deathMode: 'injury', risk: 'direct-control' },
  { partition: 'held-out', id: 'scout-gate', seed: 2902, role: 'scout', controlMode: 'autonomous', deathMode: 'injury', risk: 'gate-sacrifice' },
  { partition: 'held-out', id: 'guard-permadeath', seed: 2903, role: 'guard', controlMode: 'autonomous', deathMode: 'permadeath', risk: 'permadeath', expectedAction: 'intercept' },
  { partition: 'held-out', id: 'pathmaker-held-terrain', seed: 2904, role: 'pathmaker', controlMode: 'autonomous', deathMode: 'injury', risk: 'terrain', expectedAction: 'stabilizeTerrain' },
  { partition: 'held-out', id: 'ritualist-held-hazard', seed: 2905, role: 'ritualist', controlMode: 'autonomous', deathMode: 'injury', risk: 'hazard', expectedAction: 'stabilizeHazard' }
]

export const assertPartyAutoplayFixtures = (fixtures: readonly PartyAutoplayFixture[] = PARTY_AUTOPLAY_FIXTURES): void => {
  const risks = new Set(fixtures.map(fixture => fixture.risk))
  if (fixtures.length !== new Set(fixtures.map(fixture => fixture.id)).size || fixtures.length !== new Set(fixtures.map(fixture => fixture.seed)).size) throw new Error('party autoplay fixtures must have unique ids and seeds')
  if (!['development', 'held-out'].every(partition => ['guard', 'scout', 'pathmaker', 'ritualist'].every(role => fixtures.some(fixture => fixture.partition === partition && fixture.role === role as CompanionRole)))) throw new Error('party autoplay fixtures must cover every role in every partition')
  if (!['intercept', 'observation', 'terrain', 'hazard', 'direct-control', 'gate-sacrifice', 'injury', 'permadeath'].every(risk => risks.has(risk as PartyAutoplayFixtureRisk))) throw new Error('party autoplay fixtures are missing a required risk')
  if (!fixtures.some(fixture => fixture.partition === 'development') || !fixtures.some(fixture => fixture.partition === 'held-out')) throw new Error('party autoplay fixtures must cover development and held-out partitions')
}

export const partyAutoplayFixtureState = (fixture: PartyAutoplayFixture): { state: RunState; companionId: string } => {
  const rescue = { id: `rescue:party:${fixture.id}`, name: fixture.id, biome: fixture.risk === 'gate-sacrifice' ? 'wilds' as const : 'mine' as const, floor: 1 }
  const companion = companionLeadForRescue(rescue, fixture.controlMode)
  companion.role = fixture.role
  companion.rosterStatus = 'active'
  const state = createRun({ seed: fixture.seed, companions: [companion], rescuedNpcs: [rescue], companionDeathMode: fixture.deathMode, ...(fixture.risk === 'gate-sacrifice' ? { area: 'wilds' as const, areaOrder: ['wilds', 'caverns'] } : {}) })
  synchronizePartyActors(state, 'spawn')
  if (fixture.role === 'guard') state.floor.actors.push({ id: `threat:${fixture.id}`, role: 'monster', kind: 'rat', name: 'threat', x: 2, y: 1, health: 5, maxHealth: 5, attack: 2, defense: 0, speed: 0, energy: 0, glyph: 'r', color: '#fff', hostile: true })
  if (fixture.role === 'scout' && fixture.risk !== 'gate-sacrifice') state.floor.milestones.push({ id: `sign:${fixture.id}`, kind: 'waycache', x: 3, y: 1, discovered: true, claimed: false })
  if (fixture.role === 'pathmaker') state.floor.tiles[indexOf(2, 1)]!.kind = 'rubble'
  if (fixture.role === 'ritualist') state.floor.tiles[indexOf(2, 1)]!.kind = 'fireVent'
  return { state, companionId: companion.id }
}

export const partyAutoplayFixtureAction = (fixture: PartyAutoplayFixture): string | undefined => {
  const { state, companionId } = partyAutoplayFixtureState(fixture)
  const companion = state.companions!.find(candidate => candidate.id === companionId)!
  const actor = state.floor.actors.find(isCompanionActor)!
  return autonomousCompanionDecision(state, companion, actor).action
}
