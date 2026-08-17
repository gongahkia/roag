import { describe, expect, it } from 'vitest';
import { bindingFor, defaultPreferences, normalizePreferences, setShortcut, shortcutForKey } from '../src/preferences';

describe('game preferences', () => {
  it('normalizes malformed or duplicate bindings to safe defaults', () => {
    const preferences = normalizePreferences({
      reducedMotion: true,
      highContrast: true,
      bindings: { shoot: 'p', usePowerUp: 'p', obsolete: 3 },
    });
    expect(preferences.reducedMotion).toBe(true);
    expect(preferences.highContrast).toBe(true);
    expect(bindingFor(preferences, 'shoot')).toBe(' ');
    expect(bindingFor(preferences, 'usePowerUp')).toBe('p');
    expect(shortcutForKey(preferences, 'p')).toBe('usePowerUp');
  });

  it('rejects a remap that collides with another command', () => {
    const preferences = defaultPreferences();
    expect(setShortcut(preferences, 'help', 'p')).toBe(preferences);
    const remapped = setShortcut(preferences, 'help', 'x');
    expect(bindingFor(remapped, 'help')).toBe('x');
  });
});
