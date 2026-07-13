import { describe, expect, it } from 'vitest'
import { commandForKey, defaultSettings, loadSettings, saveSettings, setKeyBinding } from './settings'
import { flashDuration } from './renderer/effects'

const memoryStore = () => {
  const values = new Map<string, string>()
  return { getItem: (key: string) => values.get(key) ?? null, setItem: (key: string, value: string) => values.set(key, value) }
}

describe('settings', () => {
  it('persists defaults and remapped bindings locally', () => {
    const store = memoryStore()
    const initial = defaultSettings()
    expect(loadSettings(store)).toEqual(initial)
    const remapped = { ...setKeyBinding(initial, 'north', 'w'), reducedFlash: true }
    saveSettings(remapped, store)
    expect(loadSettings(store)).toEqual(remapped)
    expect(commandForKey('w', remapped)).toBe('o')
    expect(commandForKey('o', remapped)).toBeUndefined()
  })

  it('preserves conflicts and scales reduced flashes', () => {
    const initial = defaultSettings()
    expect(setKeyBinding(initial, 'north', 'i')).toBe(initial)
    expect(flashDuration(80, false)).toBe(80)
    expect(flashDuration(80, true)).toBe(20)
  })
})
