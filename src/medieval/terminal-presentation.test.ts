import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { createManagementSidebarModel } from './management-sidebar'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE } from './palette'
import {
  TERMINAL_GLYPH_VOCABULARY_ID,
  TERMINAL_KEYBOARD_COMMANDS,
  TERMINAL_PRESENTATION_LIMITS,
  TERMINAL_STATE_PRESENTATIONS,
  cancelTerminalPrompt,
  createTerminalPresentationModel,
  terminalNonColorCueFor,
  validateTerminalKeyboardCommands,
  validateTerminalMaterializedCells,
  validateTerminalMessages,
  validateTerminalPresentationModel,
  validateTerminalPrompt,
  type TerminalGlyphCatalog,
  type TerminalMapViewport,
  type TerminalMaterializedCell,
  type TerminalPrompt
} from './terminal-presentation'
import { chooseInitialCourier, createFoundationWorld } from './world'

const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'watershed' } }), 'crew:0')
const safety = (domain: 'place' | 'event' | 'player-facing-text' = 'player-facing-text') => classifyMedievalContent(domain, ['civil-life', 'navigation'], 'not-applicable', ['data'])

const viewport: TerminalMapViewport = {
  id: 'terminal-viewport:fixture',
  context: 'future-materialized',
  origin: { column: 0, row: 0 },
  width: 4,
  height: 4
}

const glyphCatalog: TerminalGlyphCatalog = {
  vocabulary: TERMINAL_GLYPH_VOCABULARY_ID,
  version: 1,
  glyphIds: ['future:known-cell']
}

const materializedCell = (id: string = 'terminal-cell:fixture'): TerminalMaterializedCell => ({
  id,
  coordinate: { column: 1, row: 1 },
  glyph: { vocabulary: TERMINAL_GLYPH_VOCABULARY_ID, vocabularyVersion: 1, id: 'future:known-cell' },
  paletteToken: 'water',
  presentationState: 'neutral',
  nonColorCue: terminalNonColorCueFor('neutral'),
  textEquivalent: 'Known water cell.',
  accessibilityText: 'Known water cell. KEY cue accompanies the neutral state.',
  evidence: {
    source: { kind: 'authoritative-record', recordId: 'frontier-fact:fixture' },
    recordedAtWorldTime: 0,
    knownAtWorldTime: 0,
    freshness: { kind: 'timeless' }
  },
  contentDomain: 'place',
  contentSafety: safety('place')
})

const futurePrompt = (): TerminalPrompt => ({
  id: 'terminal-prompt:fixture',
  kind: 'future-contextual-choice',
  accessibilityText: 'A future contextual choice is available but is not executed by this contract.',
  evidence: {
    source: { kind: 'authoritative-record', recordId: 'vessel-prop:future-fixture' },
    recordedAtWorldTime: 0,
    knownAtWorldTime: 0,
    freshness: { kind: 'current' }
  },
  contentDomain: 'player-facing-text',
  contentSafety: safety(),
  options: [{
    id: 'terminal-prompt-option:fixture',
    key: 'Enter',
    availability: 'disabled',
    disabledReason: 'no-contextual-action-materialized',
    intent: 'future-contextual-action',
    requiresConfirmation: true,
    nonColorCue: terminalNonColorCueFor('neutral'),
    accessibilityText: 'Future contextual action unavailable until a materialized rule exists.'
  }],
  cancellation: { key: 'Escape', outcome: 'cancelled-no-mutation', advancesWorldTime: false }
})

describe('terminal presentation contract', () => {
  it('projects a selected world deterministically without mutation, fabricated spatial facts, or strategic-sidebar duplication', () => {
    const world = selectedWorld('terminal-projection')
    const before = structuredClone(world)
    const first = createTerminalPresentationModel(world)
    const second = createTerminalPresentationModel(world)
    const sidebar = createManagementSidebarModel(world)
    const encoded = JSON.stringify(first)

    expect(second).toEqual(first)
    expect(validateTerminalPresentationModel(world, first)).toEqual([])
    expect(world).toEqual(before)
    expect(first.map.state).toBe('reserved-unmaterialized')
    expect(first.map.cells).toEqual([])
    expect(first.map.accessibilityText).toMatch(/no terrain, deck, actor, route, site, or hidden-world cells/i)
    expect(first.messages).toEqual([])
    expect(first.prompts).toEqual([])
    expect(first.sidebarBoundary).toEqual({ relationship: 'separate-household-known-strategic-surface', duplicatedStrategicFactCategories: [] })
    expect(encoded).not.toContain(world.initialWorld.id)
    expect(encoded).not.toContain(world.initialWorld.settlements[0]!.id)
    expect(encoded).not.toContain(world.initialWorld.people[0]!.id)
    expect(encoded).not.toContain(world.state.geography.frontier.regions[0]!.commitment.id)
    expect(encoded).not.toContain(sidebar.sections[0]!.facts[0]!.id)
  })

  it('uses only semantic palette tokens and paired non-colour cues in bounded, canonical local status and accessible text', () => {
    const model = createTerminalPresentationModel(selectedWorld('terminal-accessibility'))

    expect(model.status).toHaveLength(TERMINAL_PRESENTATION_LIMITS.statusItems)
    expect(model.status.map(item => item.id)).toEqual([...model.status.map(item => item.id)].sort())
    expect(model.status.every(item => Object.hasOwn(JOMON_PALETTE, item.paletteToken))).toBe(true)
    expect(model.status.every(item => item.paletteToken === TERMINAL_STATE_PRESENTATIONS[item.state].paletteToken)).toBe(true)
    expect(model.status.every(item => item.nonColorCue.text === JOMON_NON_COLOR_STATE_CUES[item.nonColorCue.key])).toBe(true)
    expect(model.status.every(item => item.accessibilityText.includes(item.nonColorCue.text) && item.accessibilityText.includes(item.state))).toBe(true)
    expect(model.accessibility.mapText).toEqual(model.map.accessibilityText)
    expect(model.accessibility.statusText).toEqual(model.status.map(item => item.accessibilityText))
    expect(model.accessibility.conciseSummary).toMatch(/reserved unmaterialized map.*0 authoritative messages.*0 contextual prompts/i)
    expect(model.rendererParity.requirements).toEqual([
      'same-authoritative-terminal-model',
      'no-consequential-omission',
      'no-consequential-invention',
      'text-equivalent-required'
    ])
  })

  it('rejects invalid materialized-cell coordinates, ordering, glyphs, palette roles, cues, evidence, and safety classifications', () => {
    const valid = materializedCell()
    expect(validateTerminalMaterializedCells(viewport, [valid], glyphCatalog)).toEqual([])

    const outside = structuredClone(valid)
    outside.coordinate.column = 4
    expect(validateTerminalMaterializedCells(viewport, [outside], glyphCatalog).map(item => item.code)).toContain('terminal-presentation.invalid-cell')

    const duplicate = structuredClone(valid)
    duplicate.id = 'terminal-cell:second'
    expect(validateTerminalMaterializedCells(viewport, [valid, duplicate], glyphCatalog).map(item => item.code)).toContain('terminal-presentation.duplicate-cell')

    const unsorted = structuredClone(valid)
    unsorted.coordinate = { column: 2, row: 1 }
    expect(validateTerminalMaterializedCells(viewport, [unsorted, valid], glyphCatalog).map(item => item.code)).toContain('terminal-presentation.noncanonical-cell-order')

    const reservedGlyph = structuredClone(valid)
    reservedGlyph.glyph.id = 'reserved:future-cell'
    expect(validateTerminalMaterializedCells(viewport, [reservedGlyph], glyphCatalog).map(item => item.code)).toContain('terminal-presentation.reserved-glyph-reference')

    const rawPalette = structuredClone(valid)
    rawPalette.paletteToken = '#ffffff' as never
    expect(validateTerminalMaterializedCells(viewport, [rawPalette], glyphCatalog).map(item => item.code)).toContain('terminal-presentation.unknown-palette-token')

    const missingCue = structuredClone(valid)
    missingCue.nonColorCue = terminalNonColorCueFor('ready')
    expect(validateTerminalMaterializedCells(viewport, [missingCue], glyphCatalog).map(item => item.code)).toContain('terminal-presentation.invalid-non-color-cue')

    const staleEvidence = structuredClone(valid)
    staleEvidence.evidence.freshness = { kind: 'reported-at-world-time', atWorldTime: 1 }
    expect(validateTerminalMaterializedCells(viewport, [staleEvidence], glyphCatalog).map(item => item.code)).toContain('terminal-presentation.invalid-evidence')

    const unsafe = structuredClone(valid)
    ;(unsafe.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    expect(validateTerminalMaterializedCells(viewport, [unsafe], glyphCatalog).map(item => item.code)).toContain('content-safety.prohibited.torture')
  })

  it('keeps messages and prompts source-backed, bounded, and cancellation-only until future contextual actions exist', () => {
    const message = {
      id: 'terminal-message:fixture',
      kind: 'authoritative-attention' as const,
      state: 'warning' as const,
      paletteToken: 'warningText' as const,
      nonColorCue: terminalNonColorCueFor('warning'),
      sourceRecordKind: 'causal-command' as const,
      accessibilityText: 'A recorded authoritative update needs attention.',
      evidence: {
        source: { kind: 'authoritative-record' as const, recordId: 'causal-command:fixture' },
        recordedAtWorldTime: 0,
        knownAtWorldTime: 0,
        freshness: { kind: 'timeless' as const }
      },
      contentDomain: 'event' as const,
      contentSafety: safety('event')
    }
    const prompt = futurePrompt()
    const world = selectedWorld('terminal-prompt-cancel')
    const before = structuredClone(world)

    expect(validateTerminalMessages([message])).toEqual([])
    expect(validateTerminalPrompt(prompt)).toEqual([])
    expect(cancelTerminalPrompt(prompt)).toEqual({ id: 'terminal-prompt-cancel:terminal-prompt:fixture', promptId: prompt.id, outcome: 'cancelled-no-mutation', advancesWorldTime: false })
    expect(world).toEqual(before)

    const sourceLess = {
      ...message,
      evidence: { ...message.evidence, source: { ...message.evidence.source, kind: 'presentation-contract' } }
    }
    expect(validateTerminalMessages([sourceLess]).map(item => item.code)).toContain('terminal-presentation.invalid-message')
    const unsafePrompt = structuredClone(prompt)
    ;(unsafePrompt.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'present'
    expect(validateTerminalPrompt(unsafePrompt).map(item => item.code)).toContain('content-safety.prohibited.slavery')
  })

  it('detects keyboard binding conflicts and distinguishes current navigation/management controls from reserved movement and interaction surfaces', () => {
    expect(validateTerminalKeyboardCommands(TERMINAL_KEYBOARD_COMMANDS)).toEqual([])
    expect(TERMINAL_KEYBOARD_COMMANDS.filter(command => command.availability === 'implemented').every(command => command.surface === 'navigation' || command.surface === 'management')).toBe(true)
    expect(TERMINAL_KEYBOARD_COMMANDS.filter(command => command.surface === 'movement')).toHaveLength(8)
    expect(TERMINAL_KEYBOARD_COMMANDS.filter(command => command.surface === 'movement').every(command => command.availability === 'reserved')).toBe(true)
    expect(TERMINAL_KEYBOARD_COMMANDS.find(command => command.id === 'reserved-contextual-action')?.availability).toBe('reserved')
    expect(TERMINAL_KEYBOARD_COMMANDS.find(command => command.id === 'reserved-remap-controls')?.availability).toBe('reserved')
    expect(TERMINAL_KEYBOARD_COMMANDS.find(command => command.id === 'reserved-help')?.availability).toBe('reserved')

    const conflict = [...TERMINAL_KEYBOARD_COMMANDS, {
      id: 'zzz-binding-conflict',
      availability: 'implemented' as const,
      surface: 'navigation' as const,
      contexts: ['worlds'] as const,
      binding: { key: 'N' as const },
      accessibilityLabel: 'Conflicting fixture binding'
    }]
    expect(validateTerminalKeyboardCommands(conflict).map(item => item.code)).toContain('terminal-presentation.input-binding-conflict')
  })

  it('fails closed for a malformed world or any model that adds current spatial or strategic detail', () => {
    const world = selectedWorld('terminal-fail-closed')
    const model = createTerminalPresentationModel(world)
    const malformed = structuredClone(world)
    malformed.state.temporal.worldTime = -1
    const forged = structuredClone(model)
    ;(forged.map as unknown as { cells: unknown[] }).cells = [materializedCell()]

    expect(() => createTerminalPresentationModel(malformed)).toThrow('terminal presentation rejected')
    expect(validateTerminalPresentationModel(world, forged).map(item => item.code)).toContain('terminal-presentation.invalid-model')
  })
})
