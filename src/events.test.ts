import { describe, expect, it } from 'vitest'
import { perform } from './engine'
import { newRun } from './engine/run'

describe('action event protocol', () => {
  it('returns state-free typed events for presentation consumers', () => {
    const events = perform(newRun(7), 'h')
    expect(events).toEqual([{ type: 'menu' }])
    expect(events.every(event => Object.keys(event).every(key => key === 'type'))).toBe(true)
  })

  it('opens a pause modal and emits suspension only after explicit confirmation', () => {
    const state = newRun(8)
    expect(perform(state, 'Escape')).toEqual([{ type: 'menu' }])
    expect(state.modal).toEqual({ kind: 'pause' })
    expect(perform(state, '2')).toEqual([{ type: 'suspend' }])
    expect(state.modal).toBeUndefined()
  })
})
