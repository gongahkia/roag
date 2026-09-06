import { describe, expect, it } from 'vitest'
import { createJomonDeckTerminalMap } from './terminal-presentation'
import { createHearthfordWorksiteState, createHearthfordWorksiteTemporalAction, hearthfordWorksiteResolutionFromAction, resolveHearthfordWorksite, validateHearthfordWorksiteState } from './hearthford-worksite'
import { chooseInitialCourier, createFoundationWorld, fitHearthfordMillIronwork, takeHearthfordMillLeaseCredit } from './world'

const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'watershed' } }), 'crew:0')

describe('Hearthford mill lease vertical slice', () => {
  it('derives one reproducible generated worksite and rejects seed or evidence tampering', () => {
    const first = selectedWorld('mill-race-determinism')
    const second = selectedWorld('mill-race-determinism')
    expect(first.state.hearthfordWorksite).toEqual(second.state.hearthfordWorksite)
    expect(validateHearthfordWorksiteState(first.manifest.creation.seed, first.state.hearthfordWorksite)).toEqual([])
    const forged = structuredClone(first.state.hearthfordWorksite)
    forged.incident = forged.incident === 'sluice-jam' ? 'silted-intake' : 'sluice-jam'
    expect(validateHearthfordWorksiteState(first.manifest.creation.seed, forged)).toContain('hearthford-worksite.seed-mismatch')
    const map = createJomonDeckTerminalMap(first)
    expect(map.cells.some(cell => cell.glyph.id === 'work:mill-race-response')).toBe(true)
  })

  it('defines closed time-bearing resolutions with opposite material and institutional outcomes', () => {
    const state = createHearthfordWorksiteState('mill-race-resolution')
    const fitted = resolveHearthfordWorksite(state, 'ironwork-fitted', 20, 7)
    const credited = resolveHearthfordWorksite(state, 'lease-credit', 35, 7)
    expect(fitted).toMatchObject({ institution: { status: 'relieved' }, resolution: { kind: 'ironwork-fitted', recordedAtWorldTime: 20, causalSequence: 7 } })
    expect(credited).toMatchObject({ institution: { status: 'owed' }, resolution: { kind: 'lease-credit', recordedAtWorldTime: 35, causalSequence: 7 } })
    expect(validateHearthfordWorksiteState('mill-race-resolution', fitted, 20, 7)).toEqual([])
    expect(validateHearthfordWorksiteState('mill-race-resolution', credited, 35, 7)).toEqual([])
    expect(hearthfordWorksiteResolutionFromAction(createHearthfordWorksiteTemporalAction('ironwork-fitted', 7))).toEqual({ kind: 'ironwork-fitted', causalSequence: 7 })
    expect(hearthfordWorksiteResolutionFromAction(createHearthfordWorksiteTemporalAction('lease-credit', 7))).toEqual({ kind: 'lease-credit', causalSequence: 7 })
  })

  it('rejects both public resolutions before the physical worksite and freight prerequisites are satisfied', () => {
    const source = selectedWorld('mill-race-public-guard')
    const before = structuredClone(source)
    expect(() => fitHearthfordMillIronwork(source)).toThrow(/worksite resolution requires exact mill-race occupancy/i)
    expect(() => takeHearthfordMillLeaseCredit(source)).toThrow(/worksite resolution requires exact mill-race occupancy/i)
    expect(source).toEqual(before)
  })
})
