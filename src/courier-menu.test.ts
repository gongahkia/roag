import { describe, expect, it } from 'vitest'
import { nextCourierSelection } from './courier-menu'
import type { CourierMenuEntry } from './types'

const couriers = ['aya', 'bo', 'cy'].map(id => ({ id, name: id, origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint' } satisfies CourierMenuEntry))

describe('courier menu', () => {
  it('moves selection with arrow keys and wraps at both ends', () => {
    expect(nextCourierSelection(couriers, 'bo', 'ArrowDown')).toBe('cy')
    expect(nextCourierSelection(couriers, 'cy', 'ArrowDown')).toBe('aya')
    expect(nextCourierSelection(couriers, 'aya', 'ArrowUp')).toBe('cy')
    expect(nextCourierSelection([], undefined, 'ArrowDown')).toBeUndefined()
  })
})
