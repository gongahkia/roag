import { describe, expect, it } from 'vitest'
import { nextVisualMode, normalizeVisualMode, visualModeLabel } from './visual-mode'

describe('visual modes', () => {
  it('migrates legacy sprites to ascii and cycles through runes', () => {
    expect(normalizeVisualMode('sprites')).toBe('ascii')
    expect(normalizeVisualMode('runes')).toBe('runes')
    expect(normalizeVisualMode('unknown')).toBe('ascii')
    expect(nextVisualMode(nextVisualMode('ascii'))).toBe('ascii')
    expect(visualModeLabel('ascii')).toBe('runes')
  })
})
