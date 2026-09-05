import { describe, expect, it } from 'vitest'
import { JOMON_ASCII_GLYPH_CATALOG } from './ascii-glyphs'
import { createDetailedRendererAdapterModel, createDetailedRendererSourceBundle, reportDetailedRendererParity } from './detailed-renderer-adapter'
import { initialHouseholdActiveCrew } from './initial-household'
import { assessJomonDeckStep, type JomonDeckMovementDirection } from './jomon-navigation'
import { createManagementSidebarModel } from './management-sidebar'
import { defaultTerminalControlPreferences } from './terminal-controls'
import { createTerminalPresentationModel } from './terminal-presentation'
import type { FoundationWorld } from './types'
import { chooseInitialCourier, createFoundationWorld, moveFoundationWorldCourier, recordVesselStationReadout, replayFoundationWorldCausalHistory, switchTavernCourier, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'

const move = (world: FoundationWorld, direction: JomonDeckMovementDirection): FoundationWorld => {
  const result = moveFoundationWorldCourier(world, direction)
  if (result.status !== 'moved') throw new Error(`quay-to-vessel flow expected ${direction} to be walkable`)
  return result.world
}

/** A valid persisted source at the shore-side quay approach, never a navigation mutation. */
const quaySource = (seed: string): FoundationWorld => {
  let world = chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'watershed' } }), 'crew:0')
  for (const direction of ['south', 'west', 'west'] as const) world = move(world, direction)
  return world
}

const completeQuayToVesselFlow = (seed: string) => {
  const source = quaySource(seed)
  const sourceBefore = structuredClone(source)
  let world = source

  // Quay approach -> gangplank -> tavern ledger.
  for (const direction of ['east', 'east', 'north'] as const) world = move(world, direction)
  const candidate = initialHouseholdActiveCrew(world.crew).find(member => member.id !== world.state.courier.activeCourierId)
  if (!candidate) throw new Error('quay-to-vessel flow requires an alternate eligible courier')
  world = switchTavernCourier(world, candidate.id)

  // Return to the gangplank, acknowledge its bounded station readout, then leave.
  for (const direction of ['south', 'west'] as const) world = move(world, direction)
  world = recordVesselStationReadout(world, 'prop:gangplank')
  world = move(world, 'west')
  return { source, sourceBefore, candidate, world }
}

const canonicalFlow = completeQuayToVesselFlow('quay-to-vessel-flow')

describe('quay-to-vessel physical flow', () => {
  it('uses only public deterministic transitions to cross the gangplank, switch at the tavern, acknowledge a station, and return to the quay', () => {
    const first = canonicalFlow

    expect(first.source).toEqual(first.sourceBefore)
    expect(first.source.state.navigation).toMatchObject({ courierId: 'crew:0', coordinate: { column: 2, row: 5 } })
    expect(assessJomonDeckStep(first.source, first.source.state.navigation.coordinate!, 'east')).toMatchObject({ status: 'moved', to: { column: 3, row: 5 } })
    expect(first.world.state.courier).toEqual({ version: 3, initialCourierId: 'crew:0', activeCourierId: first.candidate.id, departedCourierIds: [] })
    expect(first.world.state.navigation).toEqual({ version: 1, courierId: first.candidate.id, coordinate: { column: 2, row: 5 } })
    expect(first.world.state.temporal).toMatchObject({ worldTime: 9, actionSequence: 9 })
    expect(first.world.state.jomon.propActions.records.find(item => item.propId === 'prop:gangplank')?.latestAction).toEqual({
      kind: 'station-readout-recorded', recordedAtWorldTime: 8, causalSequence: 11
    })
    expect(replayFoundationWorldCausalHistory(first.world)).toEqual(causalReplayProjectionForWorldState(first.world.state))
    expect(validateFoundationWorld(first.world)).toEqual([])
  })

  it('keeps the completed ASCII flow consumable by the presentation-only detailed-renderer boundary', () => {
    const { world } = canonicalFlow
    const terminal = createTerminalPresentationModel(world)
    const bundle = createDetailedRendererSourceBundle({
      version: 1,
      terminal,
      sidebar: createManagementSidebarModel(world),
      glyphCatalog: JOMON_ASCII_GLYPH_CATALOG,
      controls: defaultTerminalControlPreferences()
    })
    const detailed = createDetailedRendererAdapterModel(bundle)

    expect(terminal.messages).toMatchObject([
      {
        kind: 'vessel-prop-action',
        value: { propId: 'prop:gangplank', action: 'station-readout-recorded', recordedAtWorldTime: 8, causalSequence: 11 }
      },
      {
        kind: 'vessel-prop-action',
        value: { propId: 'prop:task-ledger', action: 'tavern-courier-switched', recordedAtWorldTime: 6, causalSequence: 8 }
      }
    ])
    expect(detailed.messages.map(item => item.message)).toEqual(terminal.messages)
    expect(detailed.status.map(item => item.status)).toEqual(terminal.status)
    expect(reportDetailedRendererParity(bundle, detailed)).toMatchObject({ status: 'accepted', diagnostics: [] })
    expect(Object.hasOwn(bundle as object, 'world')).toBe(false)
    expect(Object.hasOwn(detailed as object, 'world')).toBe(false)
  })
})
