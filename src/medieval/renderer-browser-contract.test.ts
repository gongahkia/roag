import { describe, expect, it } from 'vitest'
import { JOMON_ASCII_GLYPH_CATALOG, terminalGlyphCatalog, terminalGlyphReferenceFor } from './ascii-glyphs'
import { auditMedievalContentSafety } from './content-safety'
import {
  createDetailedRendererAdapterModel,
  createDetailedRendererSourceBundle,
  reportDetailedRendererParity
} from './detailed-renderer-adapter'
import { createManagementSidebarModel } from './management-sidebar'
import { defaultTerminalControlPreferences } from './terminal-controls'
import {
  createTerminalPresentationModel,
  validateTerminalMaterializedCells,
  validateTerminalPresentationProjection,
  type TerminalMaterializedCell,
  type TerminalPresentationModel
} from './terminal-presentation'
import { chooseInitialCourier, createFoundationWorld } from './world'

const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'watershed' } }), 'crew:0')

const terminalAudit = (terminal: TerminalPresentationModel) => auditMedievalContentSafety([
  { id: 'terminal-presentation:map', domain: terminal.map.contentDomain, classification: terminal.map.contentSafety },
  ...terminal.map.cells.map(item => ({ id: `terminal-presentation:map-cell:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...terminal.status.map(item => ({ id: `terminal-presentation:status:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...terminal.messages.map(item => ({ id: `terminal-presentation:message:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...terminal.prompts.map(item => ({ id: `terminal-presentation:prompt:${item.id}`, domain: item.contentDomain, classification: item.contentSafety }))
])

/** A future-only seam fixture: it exercises catalogue parity without materializing the selected world. */
const terminalWithEveryRealGlyph = (world = selectedWorld('renderer-browser-real-glyphs')): TerminalPresentationModel => {
  const terminal = structuredClone(createTerminalPresentationModel(world))
  const width = 8
  const cells: TerminalMaterializedCell[] = JOMON_ASCII_GLYPH_CATALOG.entries.map((entry, index) => ({
    id: `terminal-cell:real-glyph:${index}`,
    coordinate: { column: index % width, row: Math.floor(index / width) },
    glyph: terminalGlyphReferenceFor(entry.id),
    paletteToken: entry.paletteToken,
    presentationState: entry.presentationState,
    nonColorCue: structuredClone(entry.nonColorCue),
    textEquivalent: entry.textEquivalent,
    accessibilityText: entry.accessibilityText,
    evidence: {
      source: { kind: 'authoritative-record', recordId: `glyph-evidence:${entry.id}` },
      recordedAtWorldTime: 0,
      knownAtWorldTime: 0,
      freshness: { kind: 'timeless' }
    },
    contentDomain: entry.contentDomain,
    contentSafety: structuredClone(entry.contentSafety)
  }))
  terminal.map = {
    state: 'materialized',
    viewport: {
      id: 'terminal-viewport:real-glyph-fixture',
      context: 'future-materialized',
      origin: { column: 0, row: 0 },
      width,
      height: Math.ceil(cells.length / width)
    },
    cells,
    textEquivalent: 'Future source-backed glyph catalogue fixture.',
    accessibilityText: 'Future materialized glyph catalogue fixture. Each cell has a source and text equivalent.',
    evidence: {
      source: { kind: 'presentation-contract', recordId: 'terminal-presentation:real-glyph-fixture' },
      recordedAtWorldTime: 0,
      knownAtWorldTime: 0,
      freshness: { kind: 'timeless' }
    },
    contentDomain: 'player-facing-text',
    contentSafety: structuredClone(terminal.map.contentSafety)
  }
  terminal.accessibility = {
    ...terminal.accessibility,
    conciseSummary: `Materialized static Jomon deck map with ${terminal.map.cells.length} source-backed cells. ${terminal.status.length} immediate local status entries. 0 authoritative messages. 0 contextual prompts.`,
    mapText: terminal.map.accessibilityText
  }
  const audit = terminalAudit(terminal)
  if (audit.status === 'rejected') throw new Error('real glyph fixture must remain content-safe')
  terminal.contentSafetyAudit = audit
  return terminal
}

describe('renderer/browser presentation boundary', () => {
  it('carries every real Jomon glyph through the future terminal cell seam and deferred detailed parity model', () => {
    const world = selectedWorld('renderer-browser-glyph-parity')
    const before = structuredClone(world)
    const terminal = terminalWithEveryRealGlyph(world)
    const catalog = terminalGlyphCatalog()

    expect(validateTerminalMaterializedCells(terminal.map.viewport, terminal.map.cells, catalog)).toEqual([])
    expect(validateTerminalPresentationProjection(terminal, catalog)).toEqual([])

    const bundle = createDetailedRendererSourceBundle({
      version: 1,
      terminal,
      sidebar: createManagementSidebarModel(world),
      glyphCatalog: JOMON_ASCII_GLYPH_CATALOG,
      controls: defaultTerminalControlPreferences()
    })
    const detailed = createDetailedRendererAdapterModel(bundle)

    expect(detailed.glyphCatalog).toEqual(JOMON_ASCII_GLYPH_CATALOG)
    expect(detailed.map.cells).toHaveLength(JOMON_ASCII_GLYPH_CATALOG.entries.length)
    expect(detailed.map.cells.map(item => ({
      id: item.glyph.id,
      paletteToken: item.metadata.paletteToken,
      state: item.metadata.presentationState,
      cue: item.metadata.nonColorCue,
      text: item.metadata.accessibilityText
    }))).toEqual(JOMON_ASCII_GLYPH_CATALOG.entries.map(entry => ({
      id: entry.id,
      paletteToken: entry.paletteToken,
      state: entry.presentationState,
      cue: entry.nonColorCue,
      text: entry.accessibilityText
    })))
    expect(detailed.map.cells.map(item => item.glyph.id)).toEqual(catalog.glyphIds)
    expect(reportDetailedRendererParity(bundle, detailed)).toMatchObject({ status: 'accepted', diagnostics: [] })
    expect(world).toEqual(before)
  })

  it('keeps current selected worlds on the source-backed static deck and exposes only immediate terminal status plus explicit message absence', () => {
    const world = selectedWorld('renderer-browser-materialized-deck')
    const before = structuredClone(world)
    const terminal = createTerminalPresentationModel(world)
    const bundle = createDetailedRendererSourceBundle({
      version: 1,
      terminal,
      sidebar: createManagementSidebarModel(world),
      glyphCatalog: JOMON_ASCII_GLYPH_CATALOG,
      controls: defaultTerminalControlPreferences()
    })
    const detailed = createDetailedRendererAdapterModel(bundle)
    const encoded = JSON.stringify({ terminal, detailed })
    const jomonLocationId = world.state.jomon.location.id
    const undisclosedSettlement = world.initialWorld.settlements.find(site => site.id !== jomonLocationId)
    const ungeneratedCommitment = world.state.geography.frontier.regions.find(region => region.status === 'ungenerated')

    expect(terminal.map).toMatchObject({ state: 'materialized', viewport: { context: 'jomon-deck-plan', width: 18, height: 8 } })
    expect(terminal.map.cells).toHaveLength(113)
    expect(detailed.map.map).toEqual(terminal.map)
    expect(detailed.map.cells).toHaveLength(113)
    expect(terminal.status.map(item => item.value.kind)).toEqual(['courier-selection', 'jomon-deck-materialized', 'world-minute'])
    expect(terminal.accessibility.messageText).toEqual(['No current authoritative messages.'])
    expect(detailed.accessibility.messageText).toEqual(terminal.accessibility.messageText)
    expect(detailed.interactionBoundary).toEqual({ sharesEffectiveCommandIds: true, promptCancellation: 'cancelled-no-mutation', executesInput: false, advancesWorldTime: false, mutatesWorld: false })
    expect(encoded).not.toContain(world.initialWorld.watershed.id)
    expect(undisclosedSettlement).toBeDefined()
    expect(encoded).not.toContain(undisclosedSettlement!.id)
    expect(encoded).not.toContain(world.initialWorld.people[0]!.id)
    expect(ungeneratedCommitment).toBeDefined()
    expect(encoded).not.toContain(ungeneratedCommitment!.commitment.id)
    expect(world).toEqual(before)
  })

  it('rejects altered real-glyph source, semantic, cue, accessibility, and unexpected detailed mapping data', () => {
    const world = selectedWorld('renderer-browser-parity-rejections')
    const terminal = terminalWithEveryRealGlyph(world)
    const bundle = createDetailedRendererSourceBundle({
      version: 1,
      terminal,
      sidebar: createManagementSidebarModel(world),
      glyphCatalog: JOMON_ASCII_GLYPH_CATALOG,
      controls: defaultTerminalControlPreferences()
    })
    const model = createDetailedRendererAdapterModel(bundle)

    const sourceMismatch = structuredClone(model)
    sourceMismatch.map.cells[0]!.cell.textEquivalent = 'Altered source text.'
    expect(reportDetailedRendererParity(bundle, sourceMismatch).diagnostics.map(item => item.code)).toContain('detailed-renderer.source-mismatch')

    const alteredAccessibility = structuredClone(model)
    alteredAccessibility.map.cells[0]!.metadata.accessibilityText = 'Altered accessibility text.'
    expect(reportDetailedRendererParity(bundle, alteredAccessibility).diagnostics.map(item => item.code)).toContain('detailed-renderer.altered-accessibility-text')

    const alteredSemantic = structuredClone(model)
    alteredSemantic.map.cells[0]!.metadata.nonColorCue = { key: 'errorPrefix', text: 'ERROR //' }
    expect(reportDetailedRendererParity(bundle, alteredSemantic).diagnostics.map(item => item.code)).toContain('detailed-renderer.missing-semantic-state')

    const alteredState = structuredClone(model)
    alteredState.map.cells[0]!.metadata.presentationState = 'risk'
    expect(reportDetailedRendererParity(bundle, alteredState).diagnostics.map(item => item.code)).toContain('detailed-renderer.missing-semantic-state')

    const alteredPalette = structuredClone(model)
    alteredPalette.map.cells[0]!.metadata.paletteToken = 'statusRisk'
    expect(reportDetailedRendererParity(bundle, alteredPalette).diagnostics.map(item => item.code)).toContain('detailed-renderer.missing-semantic-state')

    const alteredGlyph = structuredClone(model)
    alteredGlyph.map.cells[0]!.glyph.id = 'terrain:not-real'
    expect(reportDetailedRendererParity(bundle, alteredGlyph).diagnostics.map(item => item.code)).toContain('detailed-renderer.invalid-glyph-mapping')

    const unexpected = structuredClone(model)
    ;(unexpected.map.cells as unknown as Array<(typeof unexpected.map.cells)[number]>).push(structuredClone(unexpected.map.cells[0]!))
    expect(reportDetailedRendererParity(bundle, unexpected).diagnostics.map(item => item.code)).toContain('detailed-renderer.duplicate-source-item')
  })
})
