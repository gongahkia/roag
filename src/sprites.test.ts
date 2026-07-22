import { describe, expect, it } from 'vitest'
import { MONSTERS } from './content'
import { actorSprite } from './sprites'

describe('sprite registry', () => {
  it('maps every canonical monster to a manifest sprite', () => {
    for (const monster of MONSTERS) expect(actorSprite[monster.id]).toBeDefined()
    expect(actorSprite.startledBirds).toBe(actorSprite.wisp)
  })
})
