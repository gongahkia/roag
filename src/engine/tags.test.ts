import { describe, expect, it } from 'vitest'
import { evaluateModifiers, hasTags, queryTags } from './tags'
import { createEnemy } from '../test/factories'

describe('tag and modifier evaluation', () => {
  it('queries item, skill, script, terrain, and actor tags canonically', () => {
    const query = { items: ['machete'], skills: ['str1'], scripts: ['ember'], terrain: ['gas'], actors: [createEnemy({ kind: 'sapper', status: ['marked'] })] } as const
    expect(queryTags(query)).toEqual(['actor', 'arcane', 'cleave', 'ember', 'equipment', 'gas', 'hostile', 'item', 'marked', 'monster', 'sapper', 'script', 'skill', 'strength', 'weapon', 'wilds'])
    expect(queryTags({ ...query, items: ['machete', 'machete'] })).toEqual(queryTags(query))
    expect(hasTags(query, ['cleave', 'script', 'gas', 'marked'])).toBe(true)
  })

  it('applies matching modifiers by stable id order and rejects invalid sources', () => {
    const modifiers = [
      { id: 'later', requires: ['fire'], add: { damage: 2 } }, { id: 'first', requires: ['weapon'], multiply: { damage: 2 } }, { id: 'blocked', excludes: ['fire'], add: { damage: 99 } }
    ]
    const result = evaluateModifiers(['fire', 'weapon'], modifiers, { damage: 3 })
    expect(result).toEqual({ tags: ['fire', 'weapon'], values: { damage: 8 }, applied: ['first', 'later'] })
    expect(evaluateModifiers(['weapon', 'fire'], [...modifiers].reverse(), { damage: 3 })).toEqual(result)
    expect(() => queryTags({ items: ['missing'] })).toThrow('unknown item tag source')
    expect(() => queryTags({ scripts: ['tonic'] })).toThrow('non-script tag source')
    expect(() => evaluateModifiers([], [{ id: 'same' }, { id: 'same' }])).toThrow('invalid modifier id')
  })
})
