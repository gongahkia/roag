import { describe, expect, it } from 'vitest'
import { animationFrame, advanceStory, createStory, endingLore, isStoryPageComplete, loadingAnimation, openingLore, storyText, successionLore, TYPEWRITER_INTERVAL } from './lore'
import { newRun } from './engine/run'
import { createLegacy } from './test/factories'

describe('trail lore', () => {
  it('builds deterministic seasonal successor lore from a legacy record', () => {
    const record = createLegacy({ heirName: 'Ari Vale', biome: 'wilds', floor: 2, seed: 77 })
    expect(successionLore(record, 91)).toEqual(successionLore(record, 91))
    expect(successionLore(record, 91).pages.join(' ')).toContain('Ari Vale')
    expect(successionLore(record, 91).pages.join(' ')).toContain('Cedar Wilds')
    expect(openingLore(91, 'Mika').pages.join(' ')).toContain('Mika')
  })

  it('reveals, completes, advances, and finishes pages deterministically', () => {
    const story = createStory({ title: 'TEST', pages: ['abc', 'de'] }, 0)
    expect(storyText(story, TYPEWRITER_INTERVAL)).toBe('a')
    expect(isStoryPageComplete(story, TYPEWRITER_INTERVAL * 3)).toBe(true)
    const next = advanceStory(story, TYPEWRITER_INTERVAL * 3)
    expect(next.story).toMatchObject({ page: 1 })
    expect(advanceStory(next.story!, TYPEWRITER_INTERVAL * 5)).toEqual({ finished: true })
  })

  it('cycles fixed-width ASCII animation frames deterministically', () => {
    expect(animationFrame(loadingAnimation, 0)).toBe(loadingAnimation.frames[0])
    expect(animationFrame(loadingAnimation, loadingAnimation.frameMs)).toBe(loadingAnimation.frames[1])
    expect(animationFrame(loadingAnimation, loadingAnimation.frameMs * loadingAnimation.frames.length)).toBe(loadingAnimation.frames[0])
  })

  it('builds varied deterministic delivery scenes before the run analysis', () => {
    const state = newRun(91)
    state.hero.name = 'Ari Vale'
    state.rescuedNpcs = [{ id: 'scout-1', name: 'Nami', biome: 'wilds', floor: 1 }]
    state.telemetry!.kills = 12
    const scene = endingLore(state, ['mine', 'wilds', 'caverns', 'ruins'])
    expect(scene).toEqual(endingLore(state, ['mine', 'wilds', 'caverns', 'ruins']))
    expect(scene.pages.join(' ')).toContain('Ari Vale')
    expect(scene.pages.join(' ')).toContain('Nami')
    expect(new Set(Array.from({ length: 32 }, (_, seed) => endingLore(newRun(seed), ['mine', 'wilds', 'caverns', 'ruins']).pages[0])).size).toBeGreaterThan(1)
  })
})
