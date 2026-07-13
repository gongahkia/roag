import { describe, expect, it } from 'vitest'
import { createHubState, heirNameFor, hubView } from './hub'
import { initialRoute, navigate } from './routing'

describe('hub state', () => {
  it('provides a deterministic heir, routes, roster, and supplies', () => {
    const state = createHubState(42)
    expect(state).toEqual(createHubState(42))
    expect(state.unlockedAreas).toEqual(['mine'])
    expect(hubView(42, state).heirName).toBe(heirNameFor(42))
    expect(hubView(42, state).state.rescued).toEqual([])
  })

  it('selects hub actions without DOM state', () => {
    const hub = { ...initialRoute(), screen: 'hub' as const }
    expect(navigate(hub, 'r', false).hubAction).toBe('roster')
    expect(navigate(hub, 's', false).hubAction).toBe('supplies')
    expect(navigate(hub, 'h', false).hubAction).toBe('routes')
  })
})
