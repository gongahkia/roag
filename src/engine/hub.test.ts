import { describe, expect, it } from 'vitest'
import { createHubState, hubView } from './hub'
import { initialRoute, navigate } from './routing'

describe('hub state', () => {
  it('provides a courier name, routes, roster, and supplies', () => {
    const state = createHubState(42)
    expect(state).toEqual(createHubState(42))
    expect(state.unlockedAreas).toEqual(['mine'])
    expect(hubView('Mika', state)).toEqual({ courierName: 'Mika', state })
  })

  it('selects hub actions without DOM state', () => {
    const hub = { ...initialRoute(), screen: 'hub' as const }
    expect(navigate(hub, 'r', false).hubAction).toBe('roster')
    expect(navigate(hub, 's', false).hubAction).toBe('supplies')
    expect(navigate(hub, 'h', false).hubAction).toBe('routes')
  })
})
