import { describe, expect, it } from 'vitest'
import { shouldPreventKeyboardDefault } from './input-policy'

describe('input policy', () => {
  it('keeps Tab in the game while allowing unrelated browser shortcuts', () => {
    expect(shouldPreventKeyboardDefault('Tab')).toBe(true)
    expect(shouldPreventKeyboardDefault('ArrowDown')).toBe(true)
    expect(shouldPreventKeyboardDefault('F6')).toBe(false)
  })
})
