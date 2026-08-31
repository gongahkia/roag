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

  it('bounds persistent audio and controller settings while retaining the pause shortcut', () => {
    const preferences = normalizePreferences({ masterVolume: 2, effectsVolume: -1, controllerDeadzone: .9, controllerAimSensitivity: .1, mousePowerMode: 'cursor', showMerchantHoldings: false });
    expect(preferences.masterVolume).toBe(1);
    expect(preferences.effectsVolume).toBe(0);
    expect(preferences.controllerDeadzone).toBe(.5);
    expect(preferences.controllerAimSensitivity).toBe(.5);
    expect(preferences.mousePowerMode).toBe('cursor');
    expect(preferences.showMerchantHoldings).toBe(false);
    expect(bindingFor(preferences, 'pause')).toBe('Escape');
  });

  it('keeps Party Rules guide progress local and bounded for resettable onboarding', () => {
    expect(defaultPreferences().partyGuideStep).toBe(0);
    expect(normalizePreferences({ partyGuideStep: 99, showPartyDiagnostics: true })).toMatchObject({ partyGuideStep: 8, showPartyDiagnostics: true });
    expect(normalizePreferences({ partyGuideStep: -2 }).partyGuideStep).toBe(0);
  });
});
