import { initialHouseholdActiveCrew, type InitialHouseholdActiveCrewMember } from './initial-household'
import { deriveJomonDeckPlanForVerifiedWorld } from './jomon-deck-plan'
import type { CausalReplayProjection } from './causal-history'
import type { PersistentPersonRecord } from './persistent-person'
import type { FoundationWorld } from './types'

/**
 * Renderer-independent tavern courier switching. The immutable household owns
 * canonical eligible identity/order; this contract adds only current
 * availability and the existing source-backed ledger anchor.
 */
export const TAVERN_COURIER_SWITCH_CONTRACT_VERSION = 1 as const

export interface TavernCourierSwitchSource {
  propBindingId: 'deck-prop-binding:prop:task-ledger'
  propId: 'prop:task-ledger'
  areaId: 'tavern'
  coordinate: { column: 4; row: 4 }
}

export type TavernCourierSwitchUnavailableReason = 'not-at-tavern-ledger' | 'no-alternate-switchable-courier'

export interface TavernCourierSwitchAssessment {
  version: typeof TAVERN_COURIER_SWITCH_CONTRACT_VERSION
  source: TavernCourierSwitchSource
  current: InitialHouseholdActiveCrewMember
  candidates: readonly InitialHouseholdActiveCrewMember[]
  status: 'available' | 'unavailable'
  reason?: TavernCourierSwitchUnavailableReason
}

export type TavernCourierSwitchDiagnosticCode =
  | 'tavern-courier-switch.invalid-initial-courier'
  | 'tavern-courier-switch.invalid-active-courier'
  | 'tavern-courier-switch.invalid-navigation'
  | 'tavern-courier-switch.invalid-ledger-binding'
  | 'tavern-courier-switch.invalid-current-person'

export interface TavernCourierSwitchDiagnostic {
  recordId: string
  code: TavernCourierSwitchDiagnosticCode
}

export class TavernCourierSwitchContractError extends Error {
  constructor(readonly diagnostics: readonly TavernCourierSwitchDiagnostic[]) {
    super(`tavern courier switch rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'TavernCourierSwitchContractError'
  }
}

type CourierProjection = Pick<CausalReplayProjection, 'courier' | 'people'> & Partial<Pick<CausalReplayProjection, 'navigation'>>

const sameCoordinate = (left: { column: number; row: number }, right: { column: number; row: number }): boolean => left.column === right.column && left.row === right.row
const issue = (recordId: string, code: TavernCourierSwitchDiagnosticCode): TavernCourierSwitchDiagnostic => ({ recordId, code })
const switchable = (person: PersistentPersonRecord | undefined): boolean => person?.life.status === 'living' && person.work.availability === 'available'

/** The one physical operation source is derived from the validated static deck plan. */
export const tavernCourierSwitchSourceForVerifiedWorld = (world: FoundationWorld): TavernCourierSwitchSource => {
  const binding = deriveJomonDeckPlanForVerifiedWorld(world).propBindings.find(candidate => candidate.propId === 'prop:task-ledger')
  if (!binding || binding.id !== 'deck-prop-binding:prop:task-ledger' || binding.propKind !== 'ledger' || binding.areaId !== 'tavern' || binding.anchor.column !== 4 || binding.anchor.row !== 4) {
    throw new TavernCourierSwitchContractError([issue('prop:task-ledger', 'tavern-courier-switch.invalid-ledger-binding')])
  }
  return {
    propBindingId: 'deck-prop-binding:prop:task-ledger',
    propId: 'prop:task-ledger',
    areaId: 'tavern',
    coordinate: { column: 4, row: 4 }
  }
}

/**
 * Assesses only an already validated immutable world plus a reducer projection.
 * It deliberately returns only selection-safe household fields and never makes
 * a roster, persistence, location, or browser-state authority.
 */
export const assessTavernCourierSwitchForVerifiedWorld = (world: FoundationWorld, projection: CourierProjection): TavernCourierSwitchAssessment => {
  const source = tavernCourierSwitchSourceForVerifiedWorld(world)
  const initialCourierId = projection.courier.initialCourierId
  const activeCourierId = projection.courier.activeCourierId
  if (!initialCourierId) throw new TavernCourierSwitchContractError([issue('world-state:courier', 'tavern-courier-switch.invalid-initial-courier')])
  if (!activeCourierId) throw new TavernCourierSwitchContractError([issue('world-state:courier', 'tavern-courier-switch.invalid-active-courier')])
  const household = initialHouseholdActiveCrew(world.crew)
  const current = household.find(candidate => candidate.id === activeCourierId)
  const people = new Map(projection.people.records.map(person => [person.id, person]))
  if (!current || !switchable(people.get(activeCourierId))) throw new TavernCourierSwitchContractError([issue(`person:${activeCourierId}`, 'tavern-courier-switch.invalid-current-person')])
  const navigation = projection.navigation
  if (!navigation?.coordinate || navigation.courierId !== activeCourierId) throw new TavernCourierSwitchContractError([issue('world-state:navigation', 'tavern-courier-switch.invalid-navigation')])
  const candidates = household.filter(candidate => candidate.id !== activeCourierId && switchable(people.get(candidate.id)))
  if (!sameCoordinate(navigation.coordinate, source.coordinate)) return {
    version: TAVERN_COURIER_SWITCH_CONTRACT_VERSION,
    source,
    current,
    candidates,
    status: 'unavailable',
    reason: 'not-at-tavern-ledger'
  }
  if (!candidates.length) return {
    version: TAVERN_COURIER_SWITCH_CONTRACT_VERSION,
    source,
    current,
    candidates,
    status: 'unavailable',
    reason: 'no-alternate-switchable-courier'
  }
  return { version: TAVERN_COURIER_SWITCH_CONTRACT_VERSION, source, current, candidates, status: 'available' }
}
