export type ShortcutId = 'shoot' | 'powerDown' | 'powerUp' | 'usePowerUp' | 'pause' | 'help' | 'settings';
export type MousePowerMode = 'scroll' | 'cursor';

export interface ShortcutBinding {
  id: ShortcutId;
  label: string;
  defaultKey: string;
}

export interface GamePreferences {
  version: 5;
  reducedMotion: boolean;
  highContrast: boolean;
  masterVolume: number;
  effectsVolume: number;
  controllerDeadzone: number;
  controllerAimSensitivity: number;
  controllerVibration: boolean;
  mousePowerMode: MousePowerMode;
  showMerchantHoldings: boolean;
  /** The Party Rules guide is deliberately local: it never changes a shared match. */
  partyGuideStep: number;
  /** Exposes only local diagnostic summaries for moderated playtests. */
  showPartyDiagnostics: boolean;
  onlineServerUrl: string;
  bindings: Partial<Record<ShortcutId, string>>;
}

export interface PreferencesStore {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export const PREFERENCES_KEY = 'golf-with-your-enemies-preferences';
export const SHORTCUTS: readonly ShortcutBinding[] = [
  { id: 'shoot', label: 'Shoot', defaultKey: ' ' },
  { id: 'powerDown', label: 'Power down', defaultKey: '-' },
  { id: 'powerUp', label: 'Power up', defaultKey: '=' },
  { id: 'usePowerUp', label: 'Use chaos item', defaultKey: 'p' },
  { id: 'pause', label: 'Pause', defaultKey: 'Escape' },
  { id: 'help', label: 'Show shortcuts', defaultKey: '?' },
  { id: 'settings', label: 'Open settings', defaultKey: 'F1' },
];

const normalizeKey = (key: string) => key.length === 1 ? key.toLowerCase() : key;
const fallbackStore = (): PreferencesStore | undefined => {
  try { return typeof localStorage === 'undefined' ? undefined : localStorage; } catch { return undefined; }
};
const shortcut = (id: ShortcutId) => SHORTCUTS.find((candidate) => candidate.id === id)!;

export const defaultPreferences = (): GamePreferences => ({ version: 5, reducedMotion: false, highContrast: false, masterVolume: .8, effectsVolume: .8, controllerDeadzone: .18, controllerAimSensitivity: 1, controllerVibration: true, mousePowerMode: 'scroll', showMerchantHoldings: true, partyGuideStep: 0, showPartyDiagnostics: false, onlineServerUrl: '', bindings: {} });
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
  const bounded = (candidate: unknown, fallback: number, minimum: number, maximum: number) => typeof candidate === 'number' && Number.isFinite(candidate) ? Math.max(minimum, Math.min(maximum, candidate)) : fallback;
  const onlineServerUrl = typeof source.onlineServerUrl === 'string' && source.onlineServerUrl.length <= 200 ? source.onlineServerUrl.trim() : '';
  const preferences: GamePreferences = {
    version: 5,
    reducedMotion: source.reducedMotion === true,
    highContrast: source.highContrast === true,
    masterVolume: bounded(source.masterVolume, .8, 0, 1),
    effectsVolume: bounded(source.effectsVolume, .8, 0, 1),
    controllerDeadzone: bounded(source.controllerDeadzone, .18, .05, .5),
    controllerAimSensitivity: bounded(source.controllerAimSensitivity, 1, .5, 2),
    controllerVibration: source.controllerVibration !== false,
    mousePowerMode: source.mousePowerMode === 'cursor' ? 'cursor' : 'scroll',
    showMerchantHoldings: source.showMerchantHoldings !== false,
    partyGuideStep: Number.isInteger(source.partyGuideStep) ? Math.max(0, Math.min(8, Number(source.partyGuideStep))) : 0,
    showPartyDiagnostics: source.showPartyDiagnostics === true,
    onlineServerUrl,
    bindings,
  };
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
