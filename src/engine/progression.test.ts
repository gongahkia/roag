import { describe, expect, it } from 'vitest'
import { chooseSkill, gainXp, skillChoices } from './progression'
import { SKILLS } from '../content'
import { createRun } from '../test/factories'

describe('discipline offers', () => {
  it('creates three seeded, prerequisite-valid offers and only accepts an offered choice', () => {
    const state = createRun({ seed: 622 })
    const offers = skillChoices(state)
    expect(offers).toHaveLength(3)
    expect(offers).toEqual(skillChoices(structuredClone(state)))
    expect(offers.every(skill => !state.hero.skills.includes(skill.id) && skill.prerequisites.every(prerequisite => state.hero.skills.includes(prerequisite)))).toBe(true)
    expect(chooseSkill(state, '1')).toBe(true)
    expect(state.hero.skills).toEqual([offers[0].id])
    expect(chooseSkill(state, '4')).toBe(false)
  })

  it('reserves a progression continuation for an invested build after level-up', () => {
    const state = createRun({ seed: 623 })
    state.hero.skills = ['str1']
    expect(skillChoices(state).map(skill => skill.id)).toContain('str2')
    gainXp(state, 35)
    expect(state).toMatchObject({ hero: { level: 2 }, modal: { kind: 'skills' } })
    expect(skillChoices(state)).toHaveLength(3)
    expect(SKILLS.find(skill => skill.id === 'str2')!.prerequisites).toEqual(['str1'])
  })
})
