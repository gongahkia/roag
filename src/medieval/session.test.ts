import { describe, expect, it } from 'vitest'
import { MutableWorldSession } from './session'

describe('mutable world browser-session ownership', () => {
  it('permits one active world and releases it before another can become mutable', () => {
    const session = new MutableWorldSession()

    session.open('world:one')
    session.assertOwner('world:one')
    expect(session.current()).toBe('world:one')
    expect(() => session.open('world:two')).toThrow('already open')
    expect(() => session.release('world:two')).toThrow('not the mutable')

    session.release('world:one')
    session.open('world:two')
    expect(session.current()).toBe('world:two')
  })
})
