import { describe, expect, it } from 'vitest'
import { telegraphBeam } from './telegraph-overlay'

describe('telegraphBeam', () => {
  it('connects contiguous line attacks only', () => {
    expect(telegraphBeam({ x: 1, y: 1 }, [{ x: 2, y: 1 }, { x: 3, y: 1 }])).toEqual([{ x: 1, y: 1 }, { x: 2, y: 1 }, { x: 3, y: 1 }])
    expect(telegraphBeam({ x: 1, y: 1 }, [{ x: 2, y: 1 }, { x: 2, y: 2 }, { x: 3, y: 1 }])).toBeUndefined()
  })
})
