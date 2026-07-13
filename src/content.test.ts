import { describe, expect, it } from 'vitest'
import { CONTENT, ITEMS, SKILLS, SHOP_STOCK, validateContent } from './content'

describe('content registry validation', () => {
  it('accepts the startup registry', () => {
    expect(() => validateContent(CONTENT)).not.toThrow()
  })

  it('rejects invalid ids and tags', () => {
    expect(() => validateContent({ ...CONTENT, items: [...ITEMS, { ...ITEMS[0], id: 'bad id' }] })).toThrow('invalid item id')
    expect(() => validateContent({ ...CONTENT, items: [{ ...ITEMS[0], tags: ['missing'] }, ...ITEMS.slice(1)] })).toThrow('invalid item tag')
  })

  it('rejects invalid references and prerequisites', () => {
    expect(() => validateContent({ ...CONTENT, shopStock: { ...SHOP_STOCK, mine: ['missing'] } })).toThrow('unknown shop item')
    expect(() => validateContent({ ...CONTENT, skills: [{ ...SKILLS[0], prerequisites: ['missing'] }, ...SKILLS.slice(1)] })).toThrow('unknown skill prerequisite')
  })
})
