import { describe, expect, it } from 'vitest'
import { nextVisualMode, normalizeVisualMode, visualModeLabel } from './visual-mode'

describe('visual modes', () => {
  it('keeps legacy saved modes and cycles through runes', () => {
    expect(normalizeVisualMode('sprites')).toBe('sprites')
    expect(normalizeVisualMode('runes')).toBe('runes')
    expect(normalizeVisualMode('unknown')).toBe('ascii')
    expect(nextVisualMode(nextVisualMode(nextVisualMode('ascii')))).toBe('ascii')
    expect(visualModeLabel('sprites')).toBe('runes')
  })
})
