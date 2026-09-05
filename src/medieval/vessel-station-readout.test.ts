import { describe, expect, it } from 'vitest'
import { chooseInitialCourier, createFoundationWorld } from './world'
import { createVesselStationReadoutForVerifiedWorld, VESSEL_STATION_READOUT_CONTRACT_VERSION, validateVesselStationReadout, vesselStationReadoutMatchesVerifiedWorld } from './vessel-station-readout'

const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'watershed' } }), 'crew:0')

/** Unit fixtures retain the verified source and substitute only a known plan anchor projection. */
const atAnchor = (seed: string, coordinate: { column: number; row: number }) => {
  const world = structuredClone(selectedWorld(seed))
  world.state.navigation.coordinate = coordinate
  return world
}

describe('vessel station readouts', () => {
  const stations = [
    { propId: 'prop:berth' as const, coordinate: { column: 7, row: 1 }, label: 'Berth', value: { kind: 'berth-capacity', berthSlots: 6 }, source: 'world-state:jomon' },
    { propId: 'prop:cargo-hold-rack' as const, coordinate: { column: 10, row: 4 }, label: 'Cargo hold rack', value: { kind: 'cargo-capacity', cargoUnits: 12, usedUnits: 0, lots: [] }, source: 'world-state:jomon:cargo' },
    { propId: 'prop:chart-table' as const, coordinate: { column: 7, row: 4 }, label: 'Chart table', value: { kind: 'route-comparison-unavailable' }, source: 'deck-prop-binding:prop:chart-table' },
    { propId: 'prop:galley-hearth' as const, coordinate: { column: 11, row: 1 }, label: 'Galley hearth', value: { kind: 'galley-unmodeled' }, source: 'deck-prop-binding:prop:galley-hearth' },
    { propId: 'prop:gangplank' as const, coordinate: { column: 3, row: 5 }, label: 'Gangplank', value: { kind: 'quay-travel-unavailable', operationalStatus: 'moored' }, source: 'world-state:jomon' },
    { propId: 'prop:repair-space-rack' as const, coordinate: { column: 14, row: 4 }, label: 'Repair-space rack', value: { kind: 'repair-integrity', current: 100, maximum: 100 }, source: 'world-state:jomon' },
    { propId: 'prop:stores-rack' as const, coordinate: { column: 4, row: 1 }, label: 'Stores rack', value: { kind: 'stores-unmodeled' }, source: 'deck-prop-binding:prop:stores-rack' }
  ]

  it.each(stations)('derives $label from validated state only', station => {
      const world = atAnchor(`station-readout:${station.propId}`, station.coordinate)
      const before = structuredClone(world)
      const readout = createVesselStationReadoutForVerifiedWorld(world, station.propId)

      expect(readout).toMatchObject({
        version: VESSEL_STATION_READOUT_CONTRACT_VERSION,
        source: { propId: station.propId },
        label: station.label,
        value: station.value,
        factSourceId: station.source
      })
      expect(validateVesselStationReadout(readout)).toBe(true)
      expect(vesselStationReadoutMatchesVerifiedWorld(world, readout)).toBe(true)
      expect(readout.accessibilityText).toMatch(/zero-time readout/i)
      expect(readout.nonColorCue.text).not.toHaveLength(0)
      const encoded = JSON.stringify(readout)
      expect(encoded).not.toContain('coordinate')
      expect(encoded).not.toContain('worldTime')
      expect(encoded).not.toContain(world.initialWorld.id)
      expect(encoded).not.toContain(world.crew[0]!.name)
      expect(encoded).not.toContain('frontier')
      expect(world).toEqual(before)
  })

  it('fails closed for forged source, value, safety, hidden fields, and non-anchor use', () => {
    const world = atAnchor('station-readout-rejection', { column: 7, row: 4 })
    const readout = createVesselStationReadoutForVerifiedWorld(world, 'prop:chart-table')
    const forgedSource = structuredClone(readout)
    forgedSource.source.areaId = 'tavern'
    const forgedValue = structuredClone(readout)
    forgedValue.value = { kind: 'stores-unmodeled' }
    const unsafe = structuredClone(readout)
    ;(unsafe.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const hidden = { ...structuredClone(readout), coordinate: { column: 7, row: 4 } }
    const before = structuredClone(world)

    for (const candidate of [forgedSource, forgedValue, unsafe, hidden]) {
      expect(validateVesselStationReadout(candidate)).toBe(false)
      expect(vesselStationReadoutMatchesVerifiedWorld(world, candidate)).toBe(false)
    }
    expect(() => createVesselStationReadoutForVerifiedWorld(selectedWorld('station-readout-away'), 'prop:chart-table')).toThrow('invalid-operation')
    expect(world).toEqual(before)
  })
})
