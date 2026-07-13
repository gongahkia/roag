import { describe, expect, it } from 'vitest'
import { gateModalLines, gateForArea } from './gates'
import { perform } from './input'
import { createRun } from '../test/factories'

describe('gate resolution modal', () => {
  it('renders keyboard-readable alternatives, irreversible effects, and confirmation', () => {
    const gate = gateForArea('mine')
    expect(gateModalLines(gate).join('\n')).toContain('IRREVOCABLE')
    expect(gateModalLines(gate, 1).join('\n')).toContain('ENTER reviews confirmation')
    expect(gateModalLines(gate, 1, true).join('\n')).toContain('ENTER confirms this irreversible gate choice')
  })

  it('requires selection then confirmation through keyboard input', () => {
    const state = createRun()
    state.modal = { kind: 'gate', gateId: 'mine-shaft' }
    expect(perform(state, '2')).toEqual([{ type: 'menu' }])
    expect(state.modal).toMatchObject({ kind: 'gate', choice: 1, confirming: false })
    expect(perform(state, 'Enter')).toEqual([{ type: 'menu' }])
    expect(state.modal).toMatchObject({ kind: 'gate', confirming: true })
    expect(perform(state, 'Enter')).toEqual([{ type: 'menu' }])
    expect(state.modal).toBeUndefined()
  })
})
