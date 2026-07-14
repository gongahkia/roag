import { describe, expect, it } from 'vitest'
import { advanceStory, createStory, isStoryPageComplete, openingLore, storyText, successionLore, TYPEWRITER_INTERVAL } from './lore'
import { createLegacy } from './test/factories'

describe('trail lore', () => {
  it('builds deterministic seasonal successor lore from a legacy record', () => {
    const record = createLegacy({ heirName: 'Ari Vale', biome: 'wilds', floor: 2, seed: 77 })
    expect(successionLore(record, 91)).toEqual(successionLore(record, 91))
    expect(successionLore(record, 91).pages.join(' ')).toContain('Ari Vale')
    expect(successionLore(record, 91).pages.join(' ')).toContain('Cedar Wilds')
    expect(openingLore(91).pages.join(' ')).toContain('courier')
  })

  it('reveals, completes, advances, and finishes pages deterministically', () => {
    const story = createStory({ title: 'TEST', pages: ['abc', 'de'] }, 0)
    expect(storyText(story, TYPEWRITER_INTERVAL)).toBe('a')
    expect(isStoryPageComplete(story, TYPEWRITER_INTERVAL * 3)).toBe(true)
    const next = advanceStory(story, TYPEWRITER_INTERVAL * 3)
    expect(next.story).toMatchObject({ page: 1 })
    expect(advanceStory(next.story!, TYPEWRITER_INTERVAL * 5)).toEqual({ finished: true })
  })
})
