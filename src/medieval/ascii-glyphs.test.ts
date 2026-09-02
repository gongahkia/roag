import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE } from './palette'
import {
  ASCII_GLYPH_CATALOG_VERSION,
  ASCII_GLYPH_CATEGORIES,
  ASCII_GLYPH_LIMITS,
  ASCII_GLYPH_VIEWPORT_USES,
  JOMON_ASCII_GLYPH_CATALOG,
  findAsciiGlyph,
  terminalGlyphCatalog,
  terminalGlyphReferenceFor,
  validateAsciiGlyphCatalog
} from './ascii-glyphs'
import { createTerminalPresentationModel, terminalNonColorCueFor, validateTerminalMaterializedCells, type TerminalMapViewport, type TerminalMaterializedCell } from './terminal-presentation'
import { chooseInitialCourier, createFoundationWorld } from './world'

const selectedWorld = () => chooseInitialCourier(createFoundationWorld({ seed: 'ascii-glyph-model', configuration: { preset: 'watershed' } }), 'crew:0')

const materializedViewport: TerminalMapViewport = {
  id: 'terminal-viewport:ascii-glyph-fixture',
  context: 'future-materialized',
  origin: { column: 0, row: 0 },
  width: 4,
  height: 4
}

const materializedCell = (): TerminalMaterializedCell => ({
  id: 'terminal-cell:ascii-glyph-fixture',
  coordinate: { column: 1, row: 1 },
  glyph: terminalGlyphReferenceFor('terrain:river-channel'),
  paletteToken: 'water',
  presentationState: 'neutral',
  nonColorCue: terminalNonColorCueFor('neutral'),
  textEquivalent: 'Known river channel.',
  accessibilityText: 'Known river channel; KEY cue accompanies the neutral state.',
  evidence: {
    source: { kind: 'authoritative-record', recordId: 'frontier-fact:ascii-glyph-fixture' },
    recordedAtWorldTime: 0,
    knownAtWorldTime: 0,
    freshness: { kind: 'timeless' }
  },
  contentDomain: 'place',
  contentSafety: classifyMedievalContent('place', ['environment', 'navigation'], 'not-applicable', ['player-facing-text'])
})

describe('Jomon ASCII glyph catalogue', () => {
  it('covers every required category with canonical, unique printable single-cell entries and paired semantics', () => {
    const entries = JOMON_ASCII_GLYPH_CATALOG.entries
    const terminalCatalog = terminalGlyphCatalog()

    expect(ASCII_GLYPH_CATALOG_VERSION).toBe(1)
    expect(validateAsciiGlyphCatalog(JOMON_ASCII_GLYPH_CATALOG)).toEqual([])
    expect(JOMON_ASCII_GLYPH_CATALOG.factAuthority).toBe('semantic-reference-only')
    expect(entries.map(entry => entry.id)).toEqual([...entries.map(entry => entry.id)].sort())
    expect(entries).toHaveLength(ASCII_GLYPH_CATEGORIES.length * ASCII_GLYPH_LIMITS.entriesPerCategoryMinimum)
    expect([...new Set(entries.map(entry => entry.id))]).toHaveLength(entries.length)
    expect([...new Set(entries.map(entry => entry.character))]).toHaveLength(entries.length)
    expect(entries.every(entry => entry.character.length === 1 && entry.character.charCodeAt(0) >= 0x21 && entry.character.charCodeAt(0) <= 0x7e)).toBe(true)
    expect(entries.every(entry => entry.label.length > 0 && entry.textEquivalent.length > 0 && entry.accessibilityText.length > 0)).toBe(true)
    expect(entries.every(entry => Object.hasOwn(JOMON_PALETTE, entry.paletteToken))).toBe(true)
    expect(entries.every(entry => entry.nonColorCue.text === JOMON_NON_COLOR_STATE_CUES[entry.nonColorCue.key])).toBe(true)
    expect(entries.every(entry => entry.viewportUses.length > 0 && entry.viewportUses.every(use => ASCII_GLYPH_VIEWPORT_USES.includes(use)))).toBe(true)
    for (const category of ASCII_GLYPH_CATEGORIES) expect(entries.filter(entry => entry.category === category)).toHaveLength(ASCII_GLYPH_LIMITS.entriesPerCategoryMinimum)
    expect(terminalCatalog.glyphIds).toEqual(entries.map(entry => entry.id))
  })

  it('fails closed for catalogue identity, character, category, palette, cue, text, viewport, and safety violations', () => {
    const duplicateCharacter = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    duplicateCharacter.entries[1]!.character = duplicateCharacter.entries[0]!.character
    expect(validateAsciiGlyphCatalog(duplicateCharacter).map(item => item.code)).toContain('ascii-glyphs.duplicate-character')

    const duplicateId = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    duplicateId.entries[1]!.id = duplicateId.entries[0]!.id
    expect(validateAsciiGlyphCatalog(duplicateId).map(item => item.code)).toContain('ascii-glyphs.duplicate-id')

    const nonAscii = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    nonAscii.entries[0]!.character = '🌊'
    expect(validateAsciiGlyphCatalog(nonAscii).map(item => item.code)).toContain('ascii-glyphs.invalid-character')

    const multiCell = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    multiCell.entries[0]!.character = '~~'
    expect(validateAsciiGlyphCatalog(multiCell).map(item => item.code)).toContain('ascii-glyphs.invalid-character')

    const missingTerrain = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    missingTerrain.entries = missingTerrain.entries.filter(entry => entry.category !== 'terrain')
    expect(validateAsciiGlyphCatalog(missingTerrain).map(item => item.code)).toContain('ascii-glyphs.missing-required-category')

    const rawPalette = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    rawPalette.entries[0]!.paletteToken = '#ffffff' as never
    expect(validateAsciiGlyphCatalog(rawPalette).map(item => item.code)).toContain('ascii-glyphs.invalid-palette-token')

    const wrongCue = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    wrongCue.entries[0]!.nonColorCue = terminalNonColorCueFor('risk')
    expect(validateAsciiGlyphCatalog(wrongCue).map(item => item.code)).toContain('ascii-glyphs.invalid-non-color-cue')

    const oversizedText = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    oversizedText.entries[0]!.label = `A${'a'.repeat(ASCII_GLYPH_LIMITS.labelLength)}`
    expect(validateAsciiGlyphCatalog(oversizedText).map(item => item.code)).toContain('ascii-glyphs.invalid-text')

    const invalidViewportUse = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    invalidViewportUse.entries[0]!.viewportUses = ['world'] as never
    expect(validateAsciiGlyphCatalog(invalidViewportUse).map(item => item.code)).toContain('ascii-glyphs.invalid-viewport-use')

    const unsafe = structuredClone(JOMON_ASCII_GLYPH_CATALOG)
    ;(unsafe.entries[0]!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    expect(validateAsciiGlyphCatalog(unsafe).map(item => item.code)).toContain('content-safety.prohibited.torture')
  })

  it('supplies the future materialized-cell seam with real catalogue references while preserving source-backed cell validation', () => {
    const cell = materializedCell()
    const catalog = terminalGlyphCatalog()

    expect(validateTerminalMaterializedCells(materializedViewport, [cell], catalog)).toEqual([])
    expect(findAsciiGlyph(cell.glyph.id)).toEqual(JOMON_ASCII_GLYPH_CATALOG.entries.find(entry => entry.id === cell.glyph.id))
    expect(() => terminalGlyphReferenceFor('terrain:not-present')).toThrow('ASCII glyph catalogue rejected')
  })

  it('keeps the current terminal map reserved, empty, deterministic, and free of generated world facts', () => {
    const world = selectedWorld()
    const before = structuredClone(world)
    const first = createTerminalPresentationModel(world)
    const second = createTerminalPresentationModel(world)
    const encoded = JSON.stringify(first)

    expect(first).toEqual(second)
    expect(first.map).toMatchObject({ state: 'reserved-unmaterialized', cells: [] })
    expect(first.map.cells).toHaveLength(0)
    expect(encoded).not.toContain(world.initialWorld.watershed.id)
    expect(encoded).not.toContain(world.initialWorld.settlements[0]!.id)
    expect(encoded).not.toContain(world.state.geography.frontier.regions[0]!.commitment.id)
    expect(world).toEqual(before)
  })
})
