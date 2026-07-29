import { describe, expect, it } from 'vitest'
import { SCRIPTS } from '../content'
import { scriptCastProfile, scriptForItem } from './scripts'
import { createHero } from '../test/factories'

describe('script school framework', () => {
  it('declares school, tags, focus cost, shape, range, and upgrade hooks for every script', () => {
    expect(SCRIPTS).toHaveLength(11)
    for (const script of SCRIPTS) expect(script).toMatchObject({ school: expect.any(String), tags: expect.any(Array), focusCost: expect.any(Number), shape: expect.any(String), range: expect.any(Number), upgrades: expect.any(Array) })
    expect(scriptForItem('ember')).toMatchObject({ id: 'ember', school: 'ember', tags: ['fire', 'damage'], upgrades: ['potency', 'range'] })
  })

  it('builds shared cast profiles with focus and range upgrade hooks', () => {
    const hero = createHero({ skills: ['int1', 'int3', 'int5'] })
    expect(scriptCastProfile(hero, 'ember')).toMatchObject({ focusCost: 2, range: 3, script: { shape: 'line' } })
    expect(() => scriptForItem('tonic')).toThrow('unknown script item')
  })
})
