import { describe, expect, it } from 'vitest'
import { initialRoute, navigate } from './routing'

describe('screen routing', () => {
  it('moves through title, hub, area, and level using keyboard input', () => {
    const title = initialRoute()
    const hub = navigate(title, 'n', false)
    const area = navigate(hub, 'a', false)
    const level = navigate(area, 'Enter', false)
    expect([title.screen, hub.screen, area.screen, level.screen]).toEqual(['title', 'hub', 'area', 'level'])
    expect(navigate(level, 'Escape', false).screen).toBe('area')
  })

  it('only routes title resume when a saved run exists', () => {
    expect(navigate(initialRoute(), 'l', false).screen).toBe('title')
    expect(navigate(initialRoute(), 'l', true).screen).toBe('level')
  })
})
