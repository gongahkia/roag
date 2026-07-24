import { describe, expect, it } from 'vitest'
import { MONSTERS } from './content'
import { actorSprite, itemSprite, spriteSheetSpecs } from './sprites'

describe('sprite registry', () => {
  it('maps every canonical monster to a manifest sprite', () => {
    for (const monster of MONSTERS) expect(actorSprite[monster.id]).toBeDefined()
    expect(actorSprite.startledBirds).toBe(actorSprite.wisp)
  })

  it('maps animated Cordmark and Reedstep item sprites', () => {
    expect(spriteSheetSpecs.find(sheet => sheet.id === 'items-wayfinder')).toMatchObject({ columns: 4, rows: 2 })
    expect(itemSprite.cordmarkTalisman).toMatchObject({ sheet: 'items-wayfinder', row: 0, frames: 4 })
    expect(itemSprite.reedstepBoots).toMatchObject({ sheet: 'items-wayfinder', row: 1, frames: 4 })
  })
})
