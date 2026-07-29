import { describe, expect, it } from 'vitest'
import { buyHubItem, createHubState, equipHubItem, hubEquipment, hubView } from './hub'
import { initialRoute, navigate } from './routing'
import { newHero } from './run'

describe('hub state', () => {
  it('provides a courier name, routes, roster, and supplies', () => {
    const state = createHubState(42)
    expect(state).toEqual(createHubState(42))
    expect(state.unlockedAreas).toEqual(['mine'])
    expect(hubView('Mika', state)).toEqual({ courierName: 'Mika', state })
  })

  it('keeps hub destinations physical instead of binding them to menu keys', () => {
    const hub = { ...initialRoute(), screen: 'hub' as const }
    expect(navigate(hub, 'r', false)).toBe(hub)
    expect(navigate(hub, 'Enter', false)).toBe(hub)
    expect(navigate(hub, 'Escape', false).screen).toBe('title')
  })

  it('uses hub cash purchases and equipment swaps without a run state', () => {
    const hero = newHero({ name: 'Mika', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint' })
    hero.gold = 100
    expect(buyHubItem(hero, 'cap')).toMatchObject({ changed: true })
    expect(hero.gold).toBe(45)
    expect(equipHubItem(hero, 'cap')).toMatchObject({ changed: true })
    expect(hero.equipment.head).toBe('cap')
    hero.inventory.push('machete')
    expect(hubEquipment(hero)).toContain('machete')
    expect(equipHubItem(hero, 'machete')).toMatchObject({ changed: true })
    expect(hero.equipment.mainHand).toBe('machete')
    expect(hero.inventory).toContain('whip')
  })

  it('explains declined purchases and equipment changes', () => {
    const hero = newHero({ name: 'Mika', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint' })
    expect(buyHubItem(hero, 'cap')).toEqual({ changed: false, message: 'Need 55 more cash.' })
    expect(equipHubItem(hero, 'cap')).toEqual({ changed: false, message: 'Bark Cap is not in your pack.' })
  })
})
