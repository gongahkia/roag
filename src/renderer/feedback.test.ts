import { describe, expect, it } from 'vitest'
import { visualFeedback } from './feedback'

describe('non-gory combat feedback', () => {
  it('keeps shake, flash, and particles without text popups', () => {
    for (const type of ['hit', 'hurt', 'boom', 'death'] as const) {
      const feedback = visualFeedback(type)!
      expect(feedback.flash).toBeGreaterThan(0)
      expect(feedback.shake).toBeGreaterThan(0)
      expect(feedback).not.toHaveProperty('text')
    }
    expect(visualFeedback('hit')?.particles).toBeDefined()
  })

  it('uses distinct sound-compatible visual classes for combat events', () => {
    expect(visualFeedback('hit')?.color).toBe('#f4d26a')
    expect(visualFeedback('hurt')?.color).toBe('#f0a45d')
    expect(visualFeedback('death')?.color).toBe('#d2a4e8')
  })
})
