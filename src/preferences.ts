export type ShortcutId = 'shoot' | 'powerDown' | 'powerUp' | 'lock' | 'reroll' | 'usePowerUp' | 'help' | 'settings';

export interface ShortcutBinding {
  id: ShortcutId;
  label: string;
  defaultKey: string;
}

export interface GamePreferences {
  version: 1;
  reducedMotion: boolean;
  highContrast: boolean;
  bindings: Partial<Record<ShortcutId, string>>;
}

export interface PreferencesStore {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export const PREFERENCES_KEY = 'golf-with-your-enemies-preferences';
export const SHORTCUTS: readonly ShortcutBinding[] = [
  { id: 'shoot', label: 'Shoot', defaultKey: 'Space' },
  { id: 'powerDown', label: 'Power down', defaultKey: '-' },
  { id: 'powerUp', label: 'Power up', defaultKey: '=' },
  { id: 'lock', label: 'Lock candidate', defaultKey: 'Enter' },
  { id: 'reroll', label: 'Reroll candidates', defaultKey: 'r' },
  { id: 'usePowerUp', label: 'Use chaos item', defaultKey: 'p' },
  { id: 'help', label: 'Show shortcuts', defaultKey: '?' },
  { id: 'settings', label: 'Open settings', defaultKey: 'F1' },
];

const normalizeKey = (key: string) => key.length === 1 ? key.toLowerCase() : key;
const fallbackStore = (): PreferencesStore | undefined => {
  try { return typeof localStorage === 'undefined' ? undefined : localStorage; } catch { return undefined; }
};
const shortcut = (id: ShortcutId) => SHORTCUTS.find((candidate) => candidate.id === id)!;

export const defaultPreferences = (): GamePreferences => ({ version: 1, reducedMotion: false, highContrast: false, bindings: {} });
export const bindingFor = (preferences: GamePreferences, id: ShortcutId) => preferences.bindings[id] ?? shortcut(id).defaultKey;

export const normalizePreferences = (value: unknown): GamePreferences => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return defaultPreferences();
  const source = value as Record<string, unknown>;
  const bindings: GamePreferences['bindings'] = {};
  if (source.bindings && typeof source.bindings === 'object' && !Array.isArray(source.bindings)) {
    const sourceBindings = source.bindings as Record<string, unknown>;
    for (const entry of SHORTCUTS) {
      const key = sourceBindings[entry.id];
      if (typeof key === 'string' && key.length > 0 && key.length <= 32) bindings[entry.id] = normalizeKey(key);
    }
  }
  const preferences: GamePreferences = { version: 1, reducedMotion: source.reducedMotion === true, highContrast: source.highContrast === true, bindings };
  for (const entry of SHORTCUTS) {
    const key = bindingFor(preferences, entry.id);
    if (SHORTCUTS.some((other) => other.id !== entry.id && bindingFor(preferences, other.id) === key)) delete preferences.bindings[entry.id];
  }
  return preferences;
};

export const loadPreferences = (store = fallbackStore()): GamePreferences => {
  if (!store) return defaultPreferences();
  try { return normalizePreferences(JSON.parse(store.getItem(PREFERENCES_KEY) ?? 'null')); } catch { return defaultPreferences(); }
};

export const savePreferences = (preferences: GamePreferences, store = fallbackStore()): void => {
  if (!store) return;
  try { store.setItem(PREFERENCES_KEY, JSON.stringify(normalizePreferences(preferences))); } catch { }
};

export const setShortcut = (preferences: GamePreferences, id: ShortcutId, key: string): GamePreferences => {
  const normalized = normalizeKey(key);
  if (!normalized || normalized.length > 32 || SHORTCUTS.some((entry) => entry.id !== id && bindingFor(preferences, entry.id) === normalized)) return preferences;
  return { ...preferences, bindings: { ...preferences.bindings, [id]: normalized } };
};

export const shortcutForKey = (preferences: GamePreferences, key: string): ShortcutId | undefined => {
  const normalized = normalizeKey(key);
  return SHORTCUTS.find((entry) => bindingFor(preferences, entry.id) === normalized)?.id;
};

export const isEditableElement = (target: EventTarget | null): boolean => target instanceof HTMLInputElement || target instanceof HTMLSelectElement || target instanceof HTMLTextAreaElement || (target instanceof HTMLElement && target.isContentEditable);
