import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import { JOMON_ASCII_GLYPH_CATALOG, terminalGlyphReferenceFor } from './ascii-glyphs'
import { auditMedievalContentSafety, classifyMedievalContent } from './content-safety'
import {
  DETAILED_RENDERER_ADAPTER_CONTRACT_VERSION,
  DETAILED_RENDERER_PARITY_RULES,
  DetailedRendererAdapterError,
  assertDetailedRendererParity,
  createDetailedRendererAdapterModel,
  createDetailedRendererSourceBundle,
  reportDetailedRendererParity,
  validateDetailedRendererAdapterModel,
  validateDetailedRendererSourceBundle,
  type DetailedRendererSourceBundle,
  type DetailedRendererSourceBundleInput
} from './detailed-renderer-adapter'
import { createManagementSidebarModel } from './management-sidebar'
import { defaultTerminalControlPreferences } from './terminal-controls'
import { createJomonDeckContextualPrompt, createTerminalMapLegend, createTerminalPresentationModel, terminalNonColorCueFor, type TerminalMessage, type TerminalPresentationModel } from './terminal-presentation'
import { chooseInitialCourier, createFoundationWorld } from './world'

const selectedWorld = (seed: string = 'detailed-renderer-adapter') => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'watershed' } }), 'crew:0')

const sourceBundle = (seed?: string): DetailedRendererSourceBundle => {
  const world = selectedWorld(seed)
  return createDetailedRendererSourceBundle({
    version: 1,
    terminal: createTerminalPresentationModel(world),
    sidebar: createManagementSidebarModel(world),
    glyphCatalog: JOMON_ASCII_GLYPH_CATALOG,
    controls: defaultTerminalControlPreferences()
  })
}

const auditTerminal = (terminal: TerminalPresentationModel) => auditMedievalContentSafety([
  { id: 'terminal-presentation:map', domain: terminal.map.contentDomain, classification: terminal.map.contentSafety },
  ...terminal.map.cells.map(item => ({ id: `terminal-presentation:map-cell:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  { id: 'terminal-presentation:legend', domain: terminal.legend.contentDomain, classification: terminal.legend.contentSafety },
  ...terminal.legend.entries.map(item => ({ id: `terminal-presentation:legend-entry:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...terminal.status.map(item => ({ id: `terminal-presentation:status:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...terminal.messages.map(item => ({ id: `terminal-presentation:message:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...terminal.prompts.flatMap(item => [
    { id: `terminal-presentation:prompt:${item.id}`, domain: item.contentDomain, classification: item.contentSafety },
    ...(item.kind !== 'tavern-courier-switch' || item.ledger === undefined ? [] : [
      { id: `terminal-presentation:ledger:${item.ledger.id}`, domain: item.ledger.contentDomain, classification: item.ledger.contentSafety },
      ...item.ledger.members.map(member => ({ id: `terminal-presentation:ledger-member:${member.id}`, domain: member.contentDomain, classification: member.contentSafety })),
      ...(item.ledger.continuity === undefined ? [] : [{ id: `terminal-presentation:ledger-continuity:${item.ledger.continuity.kind}`, domain: item.ledger.continuity.contentDomain, classification: item.ledger.continuity.contentSafety }])
    ])
  ])
])

const terminalWithFutureShapes = (world = selectedWorld('detailed-future-shapes')): TerminalPresentationModel => {
  const terminal = structuredClone(createTerminalPresentationModel(world))
  const message: TerminalMessage = {
    id: 'terminal-message:future-detail',
    kind: 'authoritative-attention',
    state: 'warning',
    paletteToken: 'warningText',
    nonColorCue: terminalNonColorCueFor('warning'),
    sourceRecordKind: 'future-authoritative-record',
    accessibilityText: 'A future authoritative attention message is available for parity only.',
    evidence: {
      source: { kind: 'authoritative-record', recordId: 'future-record:detail-message' },
      recordedAtWorldTime: 0,
      knownAtWorldTime: 0,
      freshness: { kind: 'timeless' }
    },
    contentDomain: 'event',
    contentSafety: classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])
  }
  const prompt = createJomonDeckContextualPrompt(world)
  terminal.messages = [message]
  terminal.prompts = [prompt]
  terminal.accessibility = {
    ...terminal.accessibility,
    conciseSummary: `Materialized static Jomon deck map with ${terminal.map.cells.length} source-backed cells. ${terminal.status.length} immediate local status entries. 1 authoritative messages. 1 contextual prompts.`,
    messageText: [message.accessibilityText],
    promptText: [prompt.accessibilityText]
  }
  const audit = auditTerminal(terminal)
  if (audit.status === 'rejected') throw new Error('test fixture content safety rejected')
  terminal.contentSafetyAudit = audit
  return terminal
}

const futureCellBundle = (): DetailedRendererSourceBundle => {
  const world = selectedWorld('detailed-future-cell')
  const terminal = structuredClone(createTerminalPresentationModel(world))
  terminal.map = {
    state: 'materialized',
    viewport: { id: 'terminal-viewport:future-detail', context: 'future-materialized', origin: { column: 0, row: 0 }, width: 4, height: 4 },
    camera: { mode: 'fixed-full-deck', focus: {}, visibility: 'all-static-deck-known' },
    cells: [{
      id: 'terminal-cell:future-detail',
      coordinate: { column: 1, row: 1 },
      glyph: terminalGlyphReferenceFor('terrain:river-channel'),
      paletteToken: 'water',
      presentationState: 'neutral',
      nonColorCue: terminalNonColorCueFor('neutral'),
      textEquivalent: 'Known river channel.',
      accessibilityText: 'Known river channel; KEY cue accompanies the neutral state.',
      evidence: { source: { kind: 'authoritative-record', recordId: 'future-fact:river-channel' }, recordedAtWorldTime: 0, knownAtWorldTime: 0, freshness: { kind: 'timeless' } },
      contentDomain: 'place',
      contentSafety: classifyMedievalContent('place', ['environment', 'navigation'], 'not-applicable', ['player-facing-text'])
    }]
    ,
    textEquivalent: 'One source-backed future materialized cell.',
    accessibilityText: 'Future materialized map fixture with one known river channel cell.',
    evidence: { source: { kind: 'presentation-contract', recordId: 'terminal-presentation:future-detail' }, recordedAtWorldTime: 0, knownAtWorldTime: 0, freshness: { kind: 'timeless' } },
    contentDomain: 'player-facing-text',
    contentSafety: classifyMedievalContent('player-facing-text', ['civil-life', 'navigation'], 'not-applicable', ['data'])
  }
  terminal.legend = createTerminalMapLegend(terminal.map)
  terminal.accessibility = {
    ...terminal.accessibility,
    conciseSummary: `Materialized static Jomon deck map with ${terminal.map.cells.length} source-backed cells. ${terminal.status.length} immediate local status entries. 0 authoritative messages. 1 contextual prompts.`,
    mapText: terminal.map.accessibilityText,
    legendText: terminal.legend.accessibilityText
  }
  const audit = auditTerminal(terminal)
  if (audit.status === 'rejected') throw new Error('test fixture content safety rejected')
  terminal.contentSafetyAudit = audit
  return createDetailedRendererSourceBundle({ version: 1, terminal, sidebar: createManagementSidebarModel(world), glyphCatalog: JOMON_ASCII_GLYPH_CATALOG, controls: defaultTerminalControlPreferences() })
}

describe('detailed renderer adapter contract', () => {
  it('adapts one validated terminal/sidebar/glyph/control bundle deterministically without mutating its permitted sources', () => {
    const world = selectedWorld()
    const input: DetailedRendererSourceBundleInput = {
      version: 1,
      terminal: createTerminalPresentationModel(world),
      sidebar: createManagementSidebarModel(world),
      glyphCatalog: JOMON_ASCII_GLYPH_CATALOG,
      controls: defaultTerminalControlPreferences()
    }
    const beforeWorld = structuredClone(world)
    const beforeInput = structuredClone(input)
    const bundle = createDetailedRendererSourceBundle(input)
    const first = createDetailedRendererAdapterModel(bundle)
    const second = createDetailedRendererAdapterModel(bundle)

    expect(first).toEqual(second)
    expect(first.version).toBe(DETAILED_RENDERER_ADAPTER_CONTRACT_VERSION)
    expect(first.parityRules).toEqual(DETAILED_RENDERER_PARITY_RULES)
    expect(first.glyphCatalog).toEqual(JOMON_ASCII_GLYPH_CATALOG)
    expect(first.source.id).toMatch(/^detailed-source:/)
    expect(first.source.fingerprint).toMatch(/^dr1-[0-9a-f]{8}$/)
    expect(first.interactionBoundary).toEqual({ sharesEffectiveCommandIds: true, promptCancellation: 'cancelled-no-mutation', executesInput: false, advancesWorldTime: false, mutatesWorld: false })
    expect(reportDetailedRendererParity(bundle, first)).toMatchObject({ status: 'accepted', diagnostics: [] })
    expect(assertDetailedRendererParity(bundle, first)).toEqual(first)
    expect(world).toEqual(beforeWorld)
    expect(input).toEqual(beforeInput)
  })

  it('derives a canonical source fingerprint independently of source-bundle property insertion order', () => {
    const world = selectedWorld('detailed-fingerprint-canonical')
    const terminal = createTerminalPresentationModel(world)
    const sidebar = createManagementSidebarModel(world)
    const controls = defaultTerminalControlPreferences()
    const first = createDetailedRendererSourceBundle({ version: 1, terminal, sidebar, glyphCatalog: JOMON_ASCII_GLYPH_CATALOG, controls })
    const reordered = createDetailedRendererSourceBundle({ controls, glyphCatalog: JOMON_ASCII_GLYPH_CATALOG, sidebar, terminal, version: 1 })

    expect(reordered.source).toEqual(first.source)
  })

  it('carries the current materialized deck through detailed parity and includes only separately-addressable household-known sidebar facts', () => {
    const bundle = sourceBundle('detailed-materialized-map')
    const model = createDetailedRendererAdapterModel(bundle)
    const encoded = JSON.stringify(model)

    expect(model.map.map).toMatchObject({ state: 'materialized', viewport: { context: 'jomon-deck-plan', width: 18, height: 8 } })
    expect(model.map.cells).toHaveLength(114)
    expect(model.map.map.cells).toHaveLength(114)
    expect(model.map.legend.legend).toEqual(bundle.terminal.legend)
    expect(model.map.legend.entries.map(item => item.entry.id)).toEqual(bundle.terminal.legend.entries.map(entry => entry.id))
    expect(model.map.legend.entries.every(item => item.metadata.accessibilityText.includes('Source '))).toBe(true)
    expect(model.sidebar.sourceItemId).toBe('management-sidebar')
    expect(model.status.map(item => item.sourceItemId)).toEqual(bundle.terminal.status.map(item => item.id))
    expect(model.commands.map(item => item.sourceItemId)).toEqual(bundle.terminal.input.commands.map(item => item.id))
    expect(model.sidebar.sections.flatMap(section => section.facts).map(item => item.sourceItemId)).toEqual(bundle.sidebar.sections.flatMap(section => section.facts).map(item => item.id))
    expect(encoded).not.toContain('initial:watershed')
    expect(encoded).not.toContain('adultHouseholdBand')
    expect(encoded).not.toContain('anonymousRoles')
    expect(model.accessibility.mapText).toEqual(bundle.terminal.accessibility.mapText)
    expect(model.accessibility.sidebarText.every(text => /Source .*Known at world minute/u.test(text))).toBe(true)
  })

  it('forwards the exact source-backed physical ledger readout through parity without admitting world or input authority', () => {
    const bundle = sourceBundle('detailed-ledger-readout')
    const model = createDetailedRendererAdapterModel(bundle)
    const terminalPrompt = bundle.terminal.prompts[0]
    const detailedPrompt = model.prompts[0]
    if (!terminalPrompt || terminalPrompt.kind !== 'tavern-courier-switch' || !terminalPrompt.ledger || !detailedPrompt) throw new Error('expected a task-ledger presentation prompt')

    expect(detailedPrompt.prompt).toEqual(terminalPrompt)
    expect(detailedPrompt.metadata).toMatchObject({ sourceItemId: terminalPrompt.id, evidence: terminalPrompt.evidence, contentDomain: 'player-facing-text' })
    expect((detailedPrompt.prompt as typeof terminalPrompt).ledger).toEqual(terminalPrompt.ledger)
    expect(model.interactionBoundary).toEqual({ sharesEffectiveCommandIds: true, promptCancellation: 'cancelled-no-mutation', executesInput: false, advancesWorldTime: false, mutatesWorld: false })
    expect(JSON.stringify(model)).not.toContain('initial:watershed')
    expect(reportDetailedRendererParity(bundle, model)).toMatchObject({ status: 'accepted', diagnostics: [] })
  })

  it('preserves source-to-detailed one-to-one mappings for empty and future typed message/prompt shapes, effective controls, and accessibility', () => {
    const world = selectedWorld('detailed-future-prompt')
    const terminal = terminalWithFutureShapes(world)
    const bundle = createDetailedRendererSourceBundle({ version: 1, terminal, sidebar: createManagementSidebarModel(world), glyphCatalog: JOMON_ASCII_GLYPH_CATALOG, controls: defaultTerminalControlPreferences() })
    const model = createDetailedRendererAdapterModel(bundle)

    expect(model.messages).toHaveLength(1)
    expect(model.messages[0]!.sourceItemId).toBe(terminal.messages[0]!.id)
    expect(model.prompts).toHaveLength(1)
    expect(model.prompts[0]!.prompt.cancellation).toEqual({ key: 'Escape', outcome: 'cancelled-no-mutation', advancesWorldTime: false })
    expect(model.commands.map(item => item.commandId)).toEqual(terminal.input.commands.map(command => command.id))
    expect(model.controls.map(item => item.entry.controlId)).toHaveLength(13)
    expect(model.accessibility.messageText).toEqual(terminal.accessibility.messageText)
    expect(model.accessibility.promptText).toEqual(terminal.accessibility.promptText)
    expect(model.accessibility.commandText).toContain(model.controls[0]!.entry.accessibilityText)
    expect(reportDetailedRendererParity(bundle, model).checked).toMatchObject({ messages: 1, prompts: 1, controls: 13, legendEntries: 5 })
  })

  it('supports a source-backed future glyph-cell fixture alongside the current materialized deck', () => {
    const future = futureCellBundle()
    const model = createDetailedRendererAdapterModel(future)
    const current = createDetailedRendererAdapterModel(sourceBundle('detailed-current-deck'))

    expect(model.map.map.state).toBe('materialized')
    expect(model.map.cells).toHaveLength(1)
    expect(model.map.cells[0]!.glyph).toEqual(terminalGlyphReferenceFor('terrain:river-channel'))
    expect(model.map.cells[0]!.metadata.nonColorCue).toEqual(terminalNonColorCueFor('neutral'))
    expect(current.map.map.state).toBe('materialized')
    expect(current.map.cells).toHaveLength(114)
  })

  it('fails closed for malformed, direct-world, mixed/stale, unsafe, and invalid source bundles', () => {
    const bundle = sourceBundle('detailed-source-failures')
    const world = selectedWorld('detailed-direct-world')
    const mixed = structuredClone(bundle)
    mixed.sidebar.worldId = 'world:mixed-source'
    const stale = structuredClone(bundle)
    stale.source.fingerprint = 'dr1-00000000'
    const unsafe = structuredClone(bundle)
    ;(unsafe.glyphCatalog.entries[0]!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'

    expect(() => createDetailedRendererSourceBundle(world as unknown as DetailedRendererSourceBundleInput)).toThrow(DetailedRendererAdapterError)
    expect(validateDetailedRendererSourceBundle(world).map(item => item.code)).toContain('detailed-renderer.direct-world-input')
    expect(validateDetailedRendererSourceBundle(mixed).map(item => item.code)).toContain('detailed-renderer.mixed-world-source')
    expect(validateDetailedRendererSourceBundle(stale).map(item => item.code)).toContain('detailed-renderer.invalid-source-fingerprint')
    expect(validateDetailedRendererSourceBundle(unsafe).map(item => item.code)).toContain('detailed-renderer.unsafe-content')
  })

  it('reports canonical parity failures for omission, invention, duplication, altered source/accessibility/state/glyph data, hidden data, and input authority', () => {
    const futureBundle = futureCellBundle()
    const source = sourceBundle('detailed-parity-failures')
    const expected = createDetailedRendererAdapterModel(source)

    const missing = structuredClone(expected)
    missing.status = []
    expect(validateDetailedRendererAdapterModel(source, missing).map(item => item.code)).toContain('detailed-renderer.missing-source-item')

    const unexpected = structuredClone(expected)
    ;(unexpected.status as unknown as Array<(typeof unexpected.status)[number]>).push({ ...unexpected.status[0]!, sourceItemId: 'terminal-status:unexpected' })
    expect(validateDetailedRendererAdapterModel(source, unexpected).map(item => item.code)).toContain('detailed-renderer.unexpected-source-item')

    const duplicate = structuredClone(expected)
    ;(duplicate.status as unknown as Array<(typeof duplicate.status)[number]>).push(structuredClone(duplicate.status[0]!))
    expect(validateDetailedRendererAdapterModel(source, duplicate).map(item => item.code)).toContain('detailed-renderer.duplicate-source-item')

    const missingLegend = structuredClone(expected)
    missingLegend.map.legend.entries = []
    expect(validateDetailedRendererAdapterModel(source, missingLegend).map(item => item.code)).toContain('detailed-renderer.missing-source-item')

    const alteredSource = structuredClone(expected)
    alteredSource.status[0]!.status.id = 'terminal-status:altered'
    expect(validateDetailedRendererAdapterModel(source, alteredSource).map(item => item.code)).toContain('detailed-renderer.source-mismatch')

    const alteredAccessibility = structuredClone(expected)
    alteredAccessibility.status[0]!.metadata.accessibilityText = 'Different accessibility text.'
    expect(validateDetailedRendererAdapterModel(source, alteredAccessibility).map(item => item.code)).toContain('detailed-renderer.altered-accessibility-text')

    const missingCue = structuredClone(expected)
    missingCue.status[0]!.metadata.nonColorCue = terminalNonColorCueFor('risk')
    expect(validateDetailedRendererAdapterModel(source, missingCue).map(item => item.code)).toContain('detailed-renderer.missing-semantic-state')

    const rawPalette = structuredClone(expected)
    ;(rawPalette.status[0]!.metadata as unknown as { paletteToken: string }).paletteToken = '#ffffff'
    expect(validateDetailedRendererAdapterModel(source, rawPalette).map(item => item.code)).toContain('detailed-renderer.missing-semantic-state')

    const changedState = structuredClone(expected)
    ;(changedState.status[0]!.metadata as unknown as { presentationState: string }).presentationState = 'risk'
    expect(validateDetailedRendererAdapterModel(source, changedState).map(item => item.code)).toContain('detailed-renderer.missing-semantic-state')

    const unsafeContent = structuredClone(expected)
    ;(unsafeContent.status[0]!.metadata.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'present'
    expect(validateDetailedRendererAdapterModel(source, unsafeContent).map(item => item.code)).toContain('detailed-renderer.unsafe-content')

    const glyph = createDetailedRendererAdapterModel(futureBundle)
    glyph.map.cells[0]!.glyph.id = 'terrain:not-real'
    expect(validateDetailedRendererAdapterModel(futureBundle, glyph).map(item => item.code)).toContain('detailed-renderer.invalid-glyph-mapping')

    const hidden = structuredClone(expected)
    ;(hidden.map.map as unknown as { cells: unknown[] }).cells = [{ concealed: 'not a presentation cell' }]
    ;(hidden.map as unknown as { cells: unknown[] }).cells = [{ sourceItemId: 'concealed', cell: {}, glyph: {}, metadata: {} }]
    expect(validateDetailedRendererAdapterModel(source, hidden).map(item => item.code)).toContain('detailed-renderer.source-mismatch')

    const decoration = structuredClone(expected)
    ;(decoration.decorations as unknown as unknown[]).push({ id: 'detailed-decoration:bad', kind: 'frame', nonAuthoritative: true, text: 'concealed world fact' })
    expect(validateDetailedRendererAdapterModel(source, decoration).map(item => item.code)).toContain('detailed-renderer.hidden-data-attempt')

    const inputAuthority = structuredClone(expected)
    ;(inputAuthority.interactionBoundary as unknown as { executesInput: boolean }).executesInput = true
    expect(validateDetailedRendererAdapterModel(source, inputAuthority).map(item => item.code)).toContain('detailed-renderer.input-authority-violation')
  })

  it('has no world, persistence, time, causal-command, or input-execution authority', () => {
    const source = readFileSync(new URL('./detailed-renderer-adapter.ts', import.meta.url), 'utf8')
    const model = createDetailedRendererAdapterModel(sourceBundle('detailed-boundary'))

    expect(source).not.toMatch(/from '\.\/world'|from '\.\/storage'|from '\.\/causal-history'/u)
    expect(model.interactionBoundary.executesInput).toBe(false)
    expect(model.interactionBoundary.advancesWorldTime).toBe(false)
    expect(model.interactionBoundary.mutatesWorld).toBe(false)
    expect(model.decorations).toEqual([])
  })
})
