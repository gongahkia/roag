import { describe, expect, it } from 'vitest'
import { advanceStory, createStory, endingLore, isStoryPageComplete, openingLore, shrineChiefName, storyText, successionLore, TYPEWRITER_INTERVAL, villageElderName } from './lore'
import { createLegacy, createRun } from './test/factories'

describe('Voyager lore', () => {
  it('keeps relief scenes deterministic and identifies the lost specialist and colony', () => {
    const record = createLegacy({ heirName: 'Ari Vale', biome: 'wilds', floor: 2, seed: 77 })
    expect(successionLore(record, 91)).toEqual(successionLore(record, 91))
    expect(successionLore(record, 91).pages.join(' ')).toContain('Ari Vale')
    expect(successionLore(record, 91).pages.join(' ')).toContain('Verdant Colony')
    expect(openingLore(91, 'Mika').pages.join(' ')).toContain('Mika')
  })

  it('reveals, completes, advances, and finishes pages deterministically', () => {
    const story = createStory({ title: 'TEST', vignette: 'opening', pages: ['abc', 'de'] }, 0)
    expect(storyText(story, TYPEWRITER_INTERVAL)).toBe('a')
    expect(isStoryPageComplete(story, TYPEWRITER_INTERVAL * 3)).toBe(true)
    const next = advanceStory(story, TYPEWRITER_INTERVAL * 3)
    expect(next.story).toMatchObject({ page: 1 })
    expect(advanceStory(next.story!, TYPEWRITER_INTERVAL * 5)).toEqual({ finished: true })
  })

  it('names mission command and navigation in the opening', () => {
    const seed = 41
    const scene = openingLore(seed, 'Ari')
    expect(scene.pages.join('\n')).toContain(`${villageElderName(seed)} (mission commander)`)
    expect(scene.pages.join('\n')).toContain(`${shrineChiefName(seed)} (navigation)`)
  })

  it('frames specialist loss as an incomplete report and keeps all four endings distinct', () => {
    expect(successionLore({ id: 'death', heirName: 'Ari', biome: 'mine', floor: 1, seed: 8 }, 12).pages.join('\n')).toContain('record is incomplete')
    const state = createRun()
    expect(endingLore(state, ['mine'], { kami: 0, villagePact: 0 }).pages.at(-1)).toContain('New Edo')
    expect(endingLore(state, ['mine'], { kami: 4, villagePact: 0 }).pages.at(-1)).toContain('idealist')
    expect(endingLore(state, ['mine'], { kami: 0, villagePact: 4 }).pages.at(-1)).toContain('practical')
    expect(endingLore(state, ['mine'], { kami: 4, villagePact: 4 }).pages.at(-1)).toContain('both the data and the people')
  })
})
