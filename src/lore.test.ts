import { describe, expect, it } from 'vitest'
import { createRun } from './test/factories'
import { endingLore, openingLore, shrineChiefName, successionLore, villageElderName } from './lore'

describe('delivery lore', () => {
  it('names the elder and shrine chief in the opening', () => {
    const seed = 41
    const scene = openingLore(seed, 'Ari')
    expect(scene.pages.join('\n')).toContain(`${villageElderName(seed)} (village elder)`)
    expect(scene.pages.join('\n')).toContain(`${shrineChiefName(seed)} (shrine chief)`)
  })

  it('frames iron-trail loss as a finished generation and keeps all four endings distinct', () => {
    expect(successionLore({ id: 'death', heirName: 'Ari', biome: 'mine', floor: 1, seed: 8 }, 12).pages.join('\n')).toContain('That generation ends.')
    const state = createRun()
    expect(endingLore(state, ['mine'], { kami: 0, villagePact: 0 }).pages.at(-1)).toContain('time to choose')
    expect(endingLore(state, ['mine'], { kami: 4, villagePact: 0 }).pages.at(-1)).toContain('kami guide')
    expect(endingLore(state, ['mine'], { kami: 0, villagePact: 4 }).pages.at(-1)).toContain('coastal villages gather')
    expect(endingLore(state, ['mine'], { kami: 4, villagePact: 4 }).pages.at(-1)).toContain('villages will stand together')
  })
})
