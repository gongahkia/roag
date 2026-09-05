import { describe, expect, it } from 'vitest'
import { moveFoundationWorldCourier, chooseInitialCourier, createFoundationWorld } from './world'
import { assessTavernCourierSwitch, switchTavernCourier } from './world'
import {
  VesselProximityOperationContractError,
  assessVesselPropOperationForVerifiedWorld,
  assessVesselProximityOperations,
  assessVesselProximityOperationsForVerifiedWorld,
  validateVesselProximityOperationAssessment,
  type VesselProximityOperation,
  type VesselProximityOperationAssessment
} from './vessel-proximity-operation'

const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'watershed' } }), 'crew:0')

const moved = (world: ReturnType<typeof selectedWorld>, direction: Parameters<typeof moveFoundationWorldCourier>[1]) => {
  const result = moveFoundationWorldCourier(world, direction)
  if (result.status !== 'moved') throw new Error(`expected ${direction} to be a canonical deck step`)
  return result.world
}

/** The verified projection seam accepts only a valid active/navigation pair and derives walkability from the real plan. */
const projectionAt = (world: ReturnType<typeof selectedWorld>, column: number, row: number) => ({
  ...world.state,
  navigation: { ...world.state.navigation, coordinate: { column, row } }
})

describe('vessel proximity and operation contract', () => {
  it('derives the three canonical bindings with only anchor-derived proximity and closed availability', () => {
    const world = selectedWorld('vessel-proximity-canonical')
    const before = structuredClone(world)
    const assessment = assessVesselProximityOperations(world, world.state)

    expect(assessment).toMatchObject({ version: 1, activeAnchor: { status: 'at-prop-anchor', propId: 'prop:task-ledger' } })
    expect(assessment.operations.map(operation => ({
      source: operation.source,
      proximity: operation.proximity,
      availability: operation.availability,
      reason: operation.reason
    }))).toEqual([
      {
        source: { propBindingId: 'deck-prop-binding:prop:chart-table', propId: 'prop:chart-table', propKind: 'table', areaId: 'chart-table' },
        proximity: 'away-from-anchor', availability: 'unavailable', reason: 'not-at-prop-anchor'
      },
      {
        source: { propBindingId: 'deck-prop-binding:prop:gangplank', propId: 'prop:gangplank', propKind: 'gangplank', areaId: 'gangplank' },
        proximity: 'away-from-anchor', availability: 'unavailable', reason: 'not-at-prop-anchor'
      },
      {
        source: { propBindingId: 'deck-prop-binding:prop:task-ledger', propId: 'prop:task-ledger', propKind: 'ledger', areaId: 'tavern' },
        proximity: 'at-anchor', availability: 'implemented', reason: undefined
      }
    ])
    expect(assessment.operations.every(operation => operation.contentSafety.participantScope === 'not-applicable')).toBe(true)
    expect(validateVesselProximityOperationAssessment(assessment)).toEqual([])
    expect(JSON.stringify(assessment)).not.toContain(world.id)
    expect(JSON.stringify(assessment)).not.toContain(world.initialWorld.id)
    expect(JSON.stringify(assessment)).not.toContain(world.crew[0]!.name)
    expect(JSON.stringify(assessment)).not.toContain('worldTime')
    expect(JSON.stringify(assessment)).not.toContain('coordinate')
    expect(JSON.stringify(assessment)).not.toContain('footprint')
    expect(world).toEqual(before)
  })

  it('requires exact anchor occupancy and keeps reserved chart and gangplank domains bounded', () => {
    const chart = selectedWorld('vessel-proximity-chart')
    const chartAssessment = assessVesselProximityOperationsForVerifiedWorld(chart, projectionAt(chart, 7, 4))
    expect(chartAssessment).toMatchObject({ activeAnchor: { status: 'at-prop-anchor', propId: 'prop:chart-table' } })
    expect(chartAssessment.operations.map(operation => [operation.source.propId, operation.proximity, operation.availability, operation.reason])).toEqual([
      ['prop:chart-table', 'at-anchor', 'reserved', 'route-comparison-not-implemented'],
      ['prop:gangplank', 'away-from-anchor', 'unavailable', 'not-at-prop-anchor'],
      ['prop:task-ledger', 'away-from-anchor', 'unavailable', 'not-at-prop-anchor']
    ])

    expect(assessVesselPropOperationForVerifiedWorld(chart, projectionAt(chart, 7, 5), 'prop:chart-table')).toMatchObject({
      proximity: 'away-from-anchor', availability: 'unavailable', reason: 'not-at-prop-anchor'
    })

    const gangplank = selectedWorld('vessel-proximity-gangplank')
    expect(assessVesselPropOperationForVerifiedWorld(gangplank, projectionAt(gangplank, 3, 5), 'prop:gangplank')).toMatchObject({
      proximity: 'at-anchor', availability: 'reserved', reason: 'quay-travel-not-implemented'
    })
  })

  it('returns a bounded no-prop result for adjacent and off-prop occupancy without mutating time or navigation', () => {
    const source = selectedWorld('vessel-proximity-no-prop')
    const before = structuredClone(source)
    const projection = projectionAt(source, 5, 4)
    const assessment = assessVesselProximityOperationsForVerifiedWorld(source, projection)

    expect(assessment.activeAnchor).toEqual({ status: 'no-prop-anchor' })
    expect(assessment.operations.map(operation => [operation.proximity, operation.availability, operation.reason])).toEqual([
      ['away-from-anchor', 'unavailable', 'not-at-prop-anchor'],
      ['away-from-anchor', 'unavailable', 'not-at-prop-anchor'],
      ['away-from-anchor', 'unavailable', 'not-at-prop-anchor']
    ])
    expect(assessVesselPropOperationForVerifiedWorld(source, projection, 'prop:task-ledger')).toMatchObject({ availability: 'unavailable', reason: 'not-at-prop-anchor' })
    expect(source).toEqual(before)
  })

  it('fails closed for malformed active/navigation evidence, unknown props, and tampered deck-plan bindings without mutation', () => {
    const source = selectedWorld('vessel-proximity-rejection')
    const before = structuredClone(source)
    const invalidActive = { ...source.state, courier: { ...source.state.courier, activeCourierId: 'crew:unknown' } }
    const invalidNavigation = { ...source.state, navigation: { ...source.state.navigation, courierId: 'crew:1' } }
    const invalidCoordinate = { ...source.state, navigation: { ...source.state.navigation, coordinate: { column: 99, row: 99 } } }

    for (const projection of [invalidActive, invalidNavigation, invalidCoordinate]) {
      expect(() => assessVesselProximityOperationsForVerifiedWorld(source, projection)).toThrow(VesselProximityOperationContractError)
    }
    expect(() => assessVesselPropOperationForVerifiedWorld(source, source.state, 'prop:not-present' as never)).toThrow(VesselProximityOperationContractError)
    expect(source).toEqual(before)

    const mutations: readonly ((world: typeof source) => void)[] = [
      world => { world.jomon.props = world.jomon.props.filter(prop => prop.id !== 'prop:task-ledger') },
      world => { world.jomon.props = [...world.jomon.props, structuredClone(world.jomon.props[0]!)] },
      world => { world.jomon.props = [...world.jomon.props, { id: 'prop:unknown', kind: 'table', partition: 'chart-table' }] },
      world => { world.jomon.props = [...world.jomon.props].reverse() },
      world => { world.jomon.props[0]!.kind = 'ledger' },
      world => { world.jomon.props[0]!.partition = 'tavern' }
    ]
    for (const mutate of mutations) {
      const forged = structuredClone(source)
      mutate(forged)
      const forgedBefore = structuredClone(forged)
      expect(() => assessVesselProximityOperations(forged, forged.state)).toThrow(VesselProximityOperationContractError)
      expect(forged).toEqual(forgedBefore)
    }
  })

  it('strictly rejects reordered, duplicate, forged, unsafe, and hidden public assessments', () => {
    const source = selectedWorld('vessel-proximity-assessment-validation')
    const assessment = assessVesselProximityOperations(source, source.state)
    const mutable = (value: VesselProximityOperationAssessment): { version: 1; activeAnchor: VesselProximityOperationAssessment['activeAnchor']; operations: VesselProximityOperation[] } => structuredClone(value) as unknown as { version: 1; activeAnchor: VesselProximityOperationAssessment['activeAnchor']; operations: VesselProximityOperation[] }
    const reordered = mutable(assessment)
    reordered.operations = [...reordered.operations].reverse()
    const duplicate = mutable(assessment)
    duplicate.operations[1] = structuredClone(duplicate.operations[0]!)
    const mismatchedArea = mutable(assessment)
    mismatchedArea.operations[0]!.source.areaId = 'tavern'
    const unsafe = mutable(assessment)
    unsafe.operations[0]!.contentSafety = {
      ...unsafe.operations[0]!.contentSafety,
      exclusions: { ...unsafe.operations[0]!.contentSafety.exclusions, torture: 'permitted' }
    } as never
    const hidden = { ...structuredClone(assessment), coordinate: { column: 4, row: 4 } }

    expect(validateVesselProximityOperationAssessment(reordered).map(item => item.code)).toContain('vessel-proximity-operation.noncanonical-operation-order')
    expect(validateVesselProximityOperationAssessment(duplicate).map(item => item.code)).toContain('vessel-proximity-operation.noncanonical-operation-order')
    expect(validateVesselProximityOperationAssessment(mismatchedArea).map(item => item.code)).toContain('vessel-proximity-operation.invalid-operation')
    expect(validateVesselProximityOperationAssessment(unsafe).map(item => item.code)).toContain('vessel-proximity-operation.invalid-content-safety')
    expect(validateVesselProximityOperationAssessment(hidden).map(item => item.code)).toContain('vessel-proximity-operation.malformed-assessment')
  })

  it('routes the existing tavern switch through the shared exact-anchor assessment without changing eligibility', () => {
    const source = selectedWorld('vessel-proximity-tavern')
    const before = structuredClone(source)
    const atLedger = assessVesselPropOperationForVerifiedWorld(source, source.state, 'prop:task-ledger')
    const tavern = assessTavernCourierSwitch(source)

    expect(atLedger).toMatchObject({ proximity: 'at-anchor', availability: 'implemented' })
    expect(tavern).toMatchObject({ status: 'available', source: { propId: 'prop:task-ledger', coordinate: { column: 4, row: 4 } } })
    expect(tavern.candidates.map(candidate => candidate.id)).toEqual(['crew:1', 'crew:2', 'crew:3', 'crew:4', 'crew:5'])
    expect(source).toEqual(before)

    const away = moved(source, 'north')
    const awayBefore = structuredClone(away)
    expect(assessVesselPropOperationForVerifiedWorld(away, away.state, 'prop:task-ledger')).toMatchObject({ proximity: 'away-from-anchor', availability: 'unavailable', reason: 'not-at-prop-anchor' })
    expect(assessTavernCourierSwitch(away)).toMatchObject({ status: 'unavailable', reason: 'not-at-tavern-ledger' })
    expect(() => switchTavernCourier(away, 'crew:1')).toThrow('not-at-tavern-ledger')
    expect(away).toEqual(awayBefore)
  })
})
