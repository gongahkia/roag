import type { KeyBindingId } from './types'

export interface GameSettings { version: 1; reducedFlash: boolean; bindings: Partial<Record<KeyBindingId, string>> }
export interface SettingsStore { getItem(key: string): string | null; setItem(key: string, value: string): void }
export interface KeyBinding { id: KeyBindingId; label: string; defaultKey: string; command: string }
export type SettingChoice = { kind: 'reducedFlash'; label: string; value: string } | { kind: 'binding'; binding: KeyBinding; label: string; value: string }

export const SETTINGS_KEY = 'jomon-settings'
export const KEY_BINDINGS: readonly KeyBinding[] = [
  { id: 'northwest', label: 'Northwest', defaultKey: 'i', command: 'i' }, { id: 'north', label: 'North', defaultKey: 'o', command: 'o' }, { id: 'northeast', label: 'Northeast', defaultKey: 'p', command: 'p' },
  { id: 'west', label: 'West', defaultKey: 'k', command: 'k' }, { id: 'east', label: 'East', defaultKey: ';', command: ';' }, { id: 'southwest', label: 'Southwest', defaultKey: ',', command: ',' }, { id: 'south', label: 'South', defaultKey: '.', command: '.' }, { id: 'southeast', label: 'Southeast', defaultKey: '/', command: '/' }, { id: 'wait', label: 'Wait', defaultKey: 'l', command: 'l' },
  { id: 'help', label: 'Help', defaultKey: 'h', command: 'h' }, { id: 'encyclopedia', label: 'Encyclopedia', defaultKey: 'j', command: 'j' }, { id: 'settings', label: 'Settings', defaultKey: 'F1', command: 'settings' },
  { id: 'use', label: 'Use', defaultKey: 'u', command: 'u' }, { id: 'drop', label: 'Drop', defaultKey: 'd', command: 'd' }, { id: 'throw', label: 'Throw', defaultKey: 't', command: 't' }, { id: 'equip', label: 'Equip', defaultKey: 'e', command: 'e' }, { id: 'skills', label: 'Skills', defaultKey: 'a', command: 'a' }, { id: 'bomb', label: 'Bomb', defaultKey: 'b', command: 'b' }, { id: 'rope', label: 'Rope', defaultKey: 'r', command: 'r' }, { id: 'get', label: 'Get', defaultKey: 'g', command: 'g' }, { id: 'operate', label: 'Operate', defaultKey: 'c', command: 'c' }, { id: 'descend', label: 'Descend', defaultKey: 'q', command: 'q' }, { id: 'swap', label: 'Swap', defaultKey: 'x', command: 'x' }, { id: 'script', label: 'Charm', defaultKey: 's', command: 's' }
]
export const SETTINGS_PAGE_SIZE = 9
export const defaultSettings = (): GameSettings => ({ version: 1, reducedFlash: false, bindings: {} })
const binding = (id: KeyBindingId): KeyBinding => {
  const found = KEY_BINDINGS.find(current => current.id === id)
  if (!found) throw new Error(`unknown key binding: ${id}`)
  return found
}
const normalizeKey = (key: string): string => key.length === 1 ? key.toLowerCase() : key
const keyFor = (settings: GameSettings, current: KeyBinding): string => settings.bindings[current.id] ?? current.defaultKey
const store = (): SettingsStore | undefined => { try { return typeof localStorage === 'undefined' ? undefined : localStorage } catch { return undefined } }

export const normalizeSettings = (value: unknown): GameSettings => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return defaultSettings()
  const source = value as Record<string, unknown>
  const bindings: Partial<Record<KeyBindingId, string>> = {}
  if (source.bindings && typeof source.bindings === 'object' && !Array.isArray(source.bindings)) for (const current of KEY_BINDINGS) {
    const key = (source.bindings as Record<string, unknown>)[current.id]
    if (typeof key === 'string' && key.length > 0 && key.length <= 32) bindings[current.id] = normalizeKey(key)
  }
  const candidate: GameSettings = { version: 1, reducedFlash: source.reducedFlash === true, bindings }
  for (const current of KEY_BINDINGS) if (KEY_BINDINGS.some(other => other.id !== current.id && keyFor(candidate, other) === keyFor(candidate, current))) delete candidate.bindings[current.id]
  return candidate
}

export const loadSettings = (settingsStore = store()): GameSettings => {
  if (!settingsStore) return defaultSettings()
  try { return normalizeSettings(JSON.parse(settingsStore.getItem(SETTINGS_KEY) ?? 'null')) } catch { return defaultSettings() }
}
export const saveSettings = (settings: GameSettings, settingsStore = store()): void => { if (settingsStore) try { settingsStore.setItem(SETTINGS_KEY, JSON.stringify(normalizeSettings(settings))) } catch { } }
export const bindingKey = (settings: GameSettings, id: KeyBindingId): string => keyFor(settings, binding(id))
export const settingChoices = (settings: GameSettings, page = 0): SettingChoice[] => [{ kind: 'reducedFlash' as const, label: 'Reduced flash', value: settings.reducedFlash ? 'ON' : 'OFF' }, ...KEY_BINDINGS.map(current => ({ kind: 'binding' as const, binding: current, label: current.label, value: keyFor(settings, current) }))].slice(Math.max(0, page) * SETTINGS_PAGE_SIZE, (Math.max(0, page) + 1) * SETTINGS_PAGE_SIZE)
export const settingsPageCount = (): number => Math.ceil((KEY_BINDINGS.length + 1) / SETTINGS_PAGE_SIZE)

export const setKeyBinding = (settings: GameSettings, id: KeyBindingId, key: string): GameSettings => {
  const nextKey = normalizeKey(key)
  if (!nextKey || nextKey.length > 32 || KEY_BINDINGS.some(current => current.id !== id && keyFor(settings, current) === nextKey)) return settings
  return { ...settings, bindings: { ...settings.bindings, [id]: nextKey } }
}
export const commandForKey = (key: string, settings: GameSettings): string | undefined => {
  const nextKey = normalizeKey(key)
  const custom = KEY_BINDINGS.find(current => keyFor(settings, current) === nextKey)
  if (custom) return custom.command
  if (KEY_BINDINGS.some(current => current.defaultKey === nextKey && settings.bindings[current.id] !== undefined)) return undefined
  return key
}
