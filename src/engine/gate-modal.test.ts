import { describe, expect, it } from 'vitest'
import { gateModalLines, gateForArea } from './gates'
import { perform } from './input'
import { createRun } from '../test/factories'

describe('gate resolution modal', () => {
  it('renders keyboard-readable alternatives, irreversible effects, and confirmation', () => {
    const gate = gateForArea('mine')
    expect(gateModalLines(gate).join('\n')).toContain('FINAL')
    expect(gateModalLines(gate, 1).join('\n')).toContain('ENTER reviews confirmation')
    expect(gateModalLines(gate, 1, true).join('\n')).toContain('ENTER confirms this final passage choice')
  })

  it('requires selection then confirmation through keyboard input', () => {
    const state = createRun()
    state.hero.bombs = 1
    state.hero.gold = 8
    state.modal = { kind: 'gate', gateId: 'mine-wilds-pass' }
    expect(perform(state, '3')).toEqual([{ type: 'menu' }])
    expect(state.modal).toMatchObject({ kind: 'gate', choice: 2, confirming: false })
    expect(perform(state, 'Enter')).toEqual([{ type: 'menu' }])
    expect(state.modal).toMatchObject({ kind: 'gate', confirming: true })
    expect(perform(state, 'Enter')).toEqual([{ type: 'gateResolved' }])
    expect(state.modal).toBeUndefined()
    expect(state.gateDestination).toBe('wilds')
  })
})
