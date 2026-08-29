import { describe, expect, it } from 'vitest'
import { initialRoute, navigate } from './routing'

describe('screen routing', () => {
  it('routes title and approach, leaving hub destinations to physical interaction', () => {
    const title = initialRoute()
    const approach = navigate(title, 'n', false)
    const hub = navigate(approach, 'Enter', false)
    const area = { ...hub, screen: 'area' as const }
    const level = navigate(area, 'Enter', false)
    expect([title.screen, approach.screen, hub.screen, area.screen, level.screen]).toEqual(['title', 'approach', 'hub', 'area', 'level'])
    expect(navigate(hub, 'a', false)).toBe(hub)
    expect(navigate(level, 'Escape', false).screen).toBe('area')
  })

  it('only routes title resume when a saved run exists', () => {
    expect(navigate(initialRoute(), 'l', false).screen).toBe('title')
    expect(navigate(initialRoute(), 'l', true).screen).toBe('level')
  })

  it('keeps the session load screen until new or resume is selected', () => {
    const route = { screen: 'splash' as const, biome: 'mine' as const }
    expect(navigate(route, 'Enter', false)).toBe(route)
    expect(navigate(route, 'n', false)).toMatchObject({ screen: 'approach' })
    expect(navigate(route, 'l', true)).toMatchObject({ screen: 'level' })
  })

  it('holds input while an inter-colony transit is playing', () => {
    const transit = { screen: 'transit' as const, biome: 'wilds' as const }
    expect(navigate(transit, 'Enter', false)).toBe(transit)
  })
})
