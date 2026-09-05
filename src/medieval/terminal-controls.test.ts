import { describe, expect, it } from 'vitest'
import {
  TERMINAL_CONTROL_IDS,
  TERMINAL_CONTROLS_VERSION,
  captureTerminalControlBinding,
  createTerminalCommandHelpModel,
  createTerminalControlsEditorModel,
  cycleTerminalControlSelection,
  defaultTerminalControlPreferences,
  resetAllTerminalControls,
  resetTerminalControl,
  resolveTerminalWorldCommand,
  validateTerminalControlPreferences
} from './terminal-controls'

describe('terminal controls preferences and zero-time world intents', () => {
  it('provides canonical HJKL/YUBN defaults, cardinal arrow alternatives, and a bounded editor model', () => {
    const preferences = defaultTerminalControlPreferences()
    const help = createTerminalCommandHelpModel(preferences)
    const editor = createTerminalControlsEditorModel(preferences, 'move-north', false)

    expect(TERMINAL_CONTROLS_VERSION).toBe(1)
    expect(validateTerminalControlPreferences(preferences)).toEqual([])
    expect(preferences.bindings.map(binding => binding.controlId)).toEqual([...TERMINAL_CONTROL_IDS])
    expect(preferences.bindings).toEqual(expect.arrayContaining([
      { controlId: 'move-west', key: 'H' }, { controlId: 'move-south', key: 'J' }, { controlId: 'move-north', key: 'K' }, { controlId: 'move-east', key: 'L' },
      { controlId: 'move-north-west', key: 'Y' }, { controlId: 'move-north-east', key: 'U' }, { controlId: 'move-south-west', key: 'B' }, { controlId: 'move-south-east', key: 'N' }
    ]))
    expect(help.entries.find(entry => entry.controlId === 'move-north')?.bindingText).toBe('K / ArrowUp')
    expect(help.accessibilitySummary).toMatch(/movement is local and collision-checked.*contextual control opens the source-backed zero-time readout.*only the task ledger also provides courier switching/i)
    expect(editor.entries).toHaveLength(13)
    expect(editor.entries.find(entry => entry.controlId === 'move-north')).toMatchObject({ selected: true, fixedAliases: ['ArrowUp'] })
  })

  it('resolves all eight directions and every current world command deterministically without gameplay authority', () => {
    const preferences = defaultTerminalControlPreferences()
    const input = (key: string, context: 'world' | 'contextual-prompt' | 'command-help' | 'controls-editor' | 'controls-key-capture' = 'world') => resolveTerminalWorldCommand(preferences, { key, canvasFocused: true, context })

    expect(input('Y')).toMatchObject({ kind: 'move-courier', direction: 'north-west' })
    expect(input('K')).toMatchObject({ kind: 'move-courier', direction: 'north' })
    expect(input('U')).toMatchObject({ kind: 'move-courier', direction: 'north-east' })
    expect(input('H')).toMatchObject({ kind: 'move-courier', direction: 'west' })
    expect(input('L')).toMatchObject({ kind: 'move-courier', direction: 'east' })
    expect(input('B')).toMatchObject({ kind: 'move-courier', direction: 'south-west' })
    expect(input('J')).toMatchObject({ kind: 'move-courier', direction: 'south' })
    expect(input('N')).toMatchObject({ kind: 'move-courier', direction: 'south-east' })
    expect(input('ArrowUp')).toMatchObject({ kind: 'move-courier', direction: 'north' })
    expect(input('Enter')).toEqual({ kind: 'open-contextual-prompt' })
    expect(input('?')).toEqual({ kind: 'open-command-help' })
    expect(input('M')).toEqual({ kind: 'toggle-management' })
    expect(input('[')).toEqual({ kind: 'previous-management-section' })
    expect(input(']')).toEqual({ kind: 'next-management-section' })
    expect(input('F2')).toEqual({ kind: 'open-controls-editor' })
    expect(input('Escape')).toEqual({ kind: 'return-to-worlds' })
    expect(input('Escape', 'contextual-prompt')).toEqual({ kind: 'cancel-overlay', overlay: 'contextual-prompt' })
    expect(input('Enter', 'contextual-prompt')).toEqual({ kind: 'prompt-confirm' })
    expect(input('ArrowDown', 'controls-editor')).toEqual({ kind: 'controls-select', direction: 1 })
    expect(input('Enter', 'controls-editor')).toEqual({ kind: 'controls-begin-capture' })
  })

  it('canonicalizes capture case, rejects unsafe/conflicting capture, protects Escape, and supports pure resets', () => {
    const defaults = defaultTerminalControlPreferences()
    const accepted = captureTerminalControlBinding(defaults, 'move-west', { key: 'q' })
    expect(accepted).toMatchObject({ status: 'accepted', controlId: 'move-west', key: 'Q' })
    if (accepted.status !== 'accepted') throw new Error('capture should be accepted')
    expect(resolveTerminalWorldCommand(accepted.preferences, { key: 'q', canvasFocused: true, context: 'world' })).toMatchObject({ kind: 'move-courier', direction: 'west' })
    expect(createTerminalCommandHelpModel(accepted.preferences).entries.find(entry => entry.controlId === 'move-west')?.bindingText).toBe('Q / ArrowLeft')
    expect(captureTerminalControlBinding(accepted.preferences, 'move-west', { key: 'Escape' })).toEqual({ status: 'cancelled', controlId: 'move-west' })
    expect(captureTerminalControlBinding(accepted.preferences, 'move-west', { key: 'F2' })).toEqual({ status: 'rejected', controlId: 'move-west', code: 'terminal-controls.protected-key' })
    expect(captureTerminalControlBinding(accepted.preferences, 'move-west', { key: 'Q', ctrlKey: true })).toEqual({ status: 'rejected', controlId: 'move-west', code: 'terminal-controls.modifier-chord-rejected' })
    expect(captureTerminalControlBinding(accepted.preferences, 'move-west', { key: '💧' })).toEqual({ status: 'rejected', controlId: 'move-west', code: 'terminal-controls.unsupported-key' })
    expect(captureTerminalControlBinding(accepted.preferences, 'move-west', { key: 'K' })).toEqual({ status: 'rejected', controlId: 'move-west', code: 'terminal-controls.binding-conflict' })
    expect(resetTerminalControl(accepted.preferences, 'move-west')).toEqual(defaults)
    expect(resetAllTerminalControls(accepted.preferences)).toEqual(defaults)
    expect(cycleTerminalControlSelection('move-west', 1)).toBe('command-help')

    const remappedHelp = captureTerminalControlBinding(defaults, 'command-help', { key: 'p' })
    expect(remappedHelp).toMatchObject({ status: 'accepted', controlId: 'command-help', key: 'P' })
    if (remappedHelp.status !== 'accepted') throw new Error('command help remap should be accepted')
    expect(resolveTerminalWorldCommand(remappedHelp.preferences, { key: 'p', canvasFocused: true, context: 'world' })).toEqual({ kind: 'open-command-help' })
    expect(resolveTerminalWorldCommand(remappedHelp.preferences, { key: '?', canvasFocused: true, context: 'world' })).toEqual({ kind: 'ignored', reason: 'unbound-key' })
    expect(createTerminalCommandHelpModel(remappedHelp.preferences).entries.find(entry => entry.controlId === 'command-help')?.bindingText).toBe('P')
  })

  it('rejects malformed, unordered, duplicated, conflicting, and stale preference records fail closed', () => {
    const duplicate = structuredClone(defaultTerminalControlPreferences())
    duplicate.bindings[1]!.controlId = duplicate.bindings[0]!.controlId
    expect(validateTerminalControlPreferences(duplicate).map(item => item.code)).toContain('terminal-controls.duplicate-control')

    const conflicting = structuredClone(defaultTerminalControlPreferences())
    conflicting.bindings.find(binding => binding.controlId === 'move-west')!.key = 'K'
    expect(validateTerminalControlPreferences(conflicting).map(item => item.code)).toContain('terminal-controls.binding-conflict')

    const aliasConflict = structuredClone(defaultTerminalControlPreferences())
    aliasConflict.bindings.find(binding => binding.controlId === 'move-east')!.key = 'ArrowUp'
    expect(validateTerminalControlPreferences(aliasConflict).map(item => item.code)).toContain('terminal-controls.binding-conflict')

    const unordered = structuredClone(defaultTerminalControlPreferences())
    unordered.bindings = [...unordered.bindings].reverse()
    expect(validateTerminalControlPreferences(unordered).map(item => item.code)).toContain('terminal-controls.noncanonical-binding-order')

    const stale = { ...defaultTerminalControlPreferences(), version: 0 }
    expect(validateTerminalControlPreferences(stale).map(item => item.code)).toContain('terminal-controls.unknown-version')
  })

  it('does not resolve or consume keys outside a focused canvas', () => {
    const preferences = defaultTerminalControlPreferences()
    expect(resolveTerminalWorldCommand(preferences, { key: 'K', canvasFocused: false, context: 'world' })).toEqual({ kind: 'ignored', reason: 'canvas-not-focused' })
    expect(resolveTerminalWorldCommand(preferences, { key: 'K', ctrlKey: true, canvasFocused: true, context: 'world' })).toEqual({ kind: 'ignored', reason: 'modified-key' })
  })
})
