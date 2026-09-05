import { JOMON_ASCII_GLYPH_CATALOG, terminalGlyphCatalog, validateAsciiGlyphCatalog, type AsciiGlyphCatalog } from './ascii-glyphs'
import { auditMedievalContentSafety, classifyMedievalContent, type MedievalContentDomain, type MedievalContentSafetyClassification } from './content-safety'
import { MANAGEMENT_SIDEBAR_SECTION_LABELS, managementSidebarFreshnessLabel, validateManagementSidebarProjection, type ManagementSidebarFact, type ManagementSidebarModel } from './management-sidebar'
import { terminalNonColorCueFor, validateTerminalPresentationProjection, type TerminalEvidenceProvenance, type TerminalGlyphReference, type TerminalMapLegend, type TerminalMapLegendEntry, type TerminalMapSurface, type TerminalMessage, type TerminalNonColorCue, type TerminalPresentationModel, type TerminalPresentationState, type TerminalPrompt, type TerminalStatusItem } from './terminal-presentation'
import { createTerminalCommandHelpModel, validateTerminalControlPreferences, type TerminalControlPreferences, type TerminalHelpEntry } from './terminal-controls'
import type { JomonPaletteToken } from './palette'

/**
 * This is a view-boundary contract for Phase 9, not a renderer. A detailed
 * renderer may only consume this already-permitted presentation bundle; it
 * never receives foundation state, persistence, or commands.
 */
/** v6 forwards persisted vessel-prop action status and message feedback. */
export const DETAILED_RENDERER_ADAPTER_CONTRACT_VERSION = 6 as const
export const DETAILED_RENDERER_SOURCE_BUNDLE_VERSION = 1 as const

export const DETAILED_RENDERER_ADAPTER_LIMITS = {
  sourceIdentityLength: 256,
  visualItems: 9_800,
  decorations: 16,
  accessibilityTextLength: 320
} as const

export const DETAILED_RENDERER_PARITY_RULES = [
  'source-item-required',
  'one-to-one-consequential-mapping',
  'same-semantic-state-and-cue',
  'same-source-time-freshness',
  'same-or-equivalent-accessibility-text',
  'glyph-semantics-identifiable',
  'map-legend-help-required',
  'effective-input-contract-only',
  'non-authoritative-nontextual-decoration-only'
] as const
export type DetailedRendererParityRule = typeof DETAILED_RENDERER_PARITY_RULES[number]

export interface DetailedRendererSourceIdentity {
  id: string
  fingerprint: string
  terminalContractVersion: number
  sidebarContractVersion: number
  glyphCatalogVersion: number
  controlsVersion: number
}

/** Raw input intentionally has no world/state field. */
export interface DetailedRendererSourceBundleInput {
  version: typeof DETAILED_RENDERER_SOURCE_BUNDLE_VERSION
  terminal: TerminalPresentationModel
  sidebar: ManagementSidebarModel
  glyphCatalog: AsciiGlyphCatalog
  controls: TerminalControlPreferences
}

/** A canonical, validated presentation-only bundle for a future detailed view. */
export interface DetailedRendererSourceBundle extends DetailedRendererSourceBundleInput {
  source: DetailedRendererSourceIdentity
}

export interface DetailedRendererSemanticMetadata {
  paletteToken: JomonPaletteToken
  presentationState: TerminalPresentationState
  nonColorCue: TerminalNonColorCue
}

export interface DetailedRendererSourceMetadata extends DetailedRendererSemanticMetadata {
  sourceItemId: string
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export interface DetailedRendererMapCell {
  sourceItemId: string
  cell: Extract<TerminalMapSurface, { state: 'materialized' }>['cells'][number]
  glyph: TerminalGlyphReference
  metadata: DetailedRendererSourceMetadata
}

export interface DetailedRendererMapLegendEntry {
  sourceItemId: string
  entry: TerminalMapLegendEntry
  glyph: TerminalGlyphReference
  metadata: DetailedRendererSourceMetadata
}

export interface DetailedRendererMapLegend {
  sourceItemId: 'terminal-map-legend'
  legend: TerminalMapLegend
  metadata: DetailedRendererSourceMetadata
  entries: readonly DetailedRendererMapLegendEntry[]
}

export interface DetailedRendererMapSurface {
  sourceItemId: 'terminal-map'
  map: TerminalMapSurface
  metadata: DetailedRendererSourceMetadata
  cells: readonly DetailedRendererMapCell[]
  legend: DetailedRendererMapLegend
}

export interface DetailedRendererStatusItem {
  sourceItemId: string
  status: TerminalStatusItem
  metadata: DetailedRendererSourceMetadata
}

export interface DetailedRendererMessageItem {
  sourceItemId: string
  message: TerminalMessage
  metadata: DetailedRendererSourceMetadata
}

export interface DetailedRendererPrompt {
  sourceItemId: string
  prompt: TerminalPrompt
  metadata: DetailedRendererSourceMetadata
}

export interface DetailedRendererCommand {
  sourceItemId: string
  commandId: string
  availability: 'implemented' | 'reserved'
  accessibilityText: string
  metadata: DetailedRendererSourceMetadata
}

export interface DetailedRendererControlHelp {
  sourceItemId: string
  entry: TerminalHelpEntry
  metadata: DetailedRendererSourceMetadata
}

export interface DetailedRendererSidebarFact {
  sourceItemId: string
  fact: ManagementSidebarFact
  metadata: DetailedRendererSourceMetadata
}

/** Decorations are allowed only as bounded, non-textual, non-authoritative layout hints. */
export interface DetailedRendererDecoration {
  id: string
  kind: 'frame' | 'texture' | 'spacing'
  nonAuthoritative: true
}

export interface DetailedRendererAccessibilityProjection {
  conciseSummary: string
  mapText: string
  legendText: string
  statusText: readonly string[]
  messageText: readonly string[]
  promptText: readonly string[]
  commandText: readonly string[]
  sidebarText: readonly string[]
}

/**
 * A renderer-neutral, future detailed view model. Its nested source records
 * remain addressable, so grouping/layout cannot hide consequential facts.
 */
export interface DetailedRendererAdapterModel {
  version: typeof DETAILED_RENDERER_ADAPTER_CONTRACT_VERSION
  source: DetailedRendererSourceIdentity
  parityRules: readonly DetailedRendererParityRule[]
  /** Semantic catalogue only; it does not materialize a world cell. */
  glyphCatalog: AsciiGlyphCatalog
  map: DetailedRendererMapSurface
  status: readonly DetailedRendererStatusItem[]
  messages: readonly DetailedRendererMessageItem[]
  prompts: readonly DetailedRendererPrompt[]
  commands: readonly DetailedRendererCommand[]
  controls: readonly DetailedRendererControlHelp[]
  sidebar: {
    sourceItemId: 'management-sidebar'
    sections: readonly { sectionId: string; facts: readonly DetailedRendererSidebarFact[] }[]
    summary: ManagementSidebarModel['summary']
  }
  accessibility: DetailedRendererAccessibilityProjection
  decorations: readonly DetailedRendererDecoration[]
  interactionBoundary: {
    sharesEffectiveCommandIds: true
    promptCancellation: 'cancelled-no-mutation'
    executesInput: false
    advancesWorldTime: false
    mutatesWorld: false
  }
}

export type DetailedRendererAdapterDiagnosticCode =
  | 'detailed-renderer.malformed-source-bundle'
  | 'detailed-renderer.direct-world-input'
  | 'detailed-renderer.invalid-terminal-projection'
  | 'detailed-renderer.invalid-sidebar-projection'
  | 'detailed-renderer.invalid-glyph-catalog'
  | 'detailed-renderer.invalid-controls'
  | 'detailed-renderer.mixed-world-source'
  | 'detailed-renderer.invalid-source-fingerprint'
  | 'detailed-renderer.malformed-model'
  | 'detailed-renderer.stale-source'
  | 'detailed-renderer.missing-source-item'
  | 'detailed-renderer.unexpected-source-item'
  | 'detailed-renderer.duplicate-source-item'
  | 'detailed-renderer.source-mismatch'
  | 'detailed-renderer.altered-accessibility-text'
  | 'detailed-renderer.missing-semantic-state'
  | 'detailed-renderer.invalid-glyph-mapping'
  | 'detailed-renderer.hidden-data-attempt'
  | 'detailed-renderer.unsafe-content'
  | 'detailed-renderer.invalid-decoration'
  | 'detailed-renderer.input-authority-violation'

export interface DetailedRendererAdapterDiagnostic {
  sourceItemId: string
  code: DetailedRendererAdapterDiagnosticCode
}

export interface DetailedRendererParityReport {
  status: 'accepted' | 'rejected'
  source: DetailedRendererSourceIdentity | undefined
  checked: {
    mapCells: number
    legendEntries: number
    status: number
    messages: number
    prompts: number
    commands: number
    controls: number
    sidebarFacts: number
  }
  diagnostics: readonly DetailedRendererAdapterDiagnostic[]
}

export class DetailedRendererAdapterError extends Error {
  constructor(readonly diagnostics: readonly DetailedRendererAdapterDiagnostic[]) {
    super(`detailed renderer adapter rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'DetailedRendererAdapterError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const clone = <Value>(value: Value): Value => structuredClone(value)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort(compare)
  const expectedKeys = [...expected].sort(compare)
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const validId = (value: unknown, limit: number = DETAILED_RENDERER_ADAPTER_LIMITS.sourceIdentityLength): value is string => typeof value === 'string' && value.length > 0 && value.length <= limit && /^[a-z][a-z0-9:._-]*$/iu.test(value)
const issue = (sourceItemId: string, code: DetailedRendererAdapterDiagnosticCode): DetailedRendererAdapterDiagnostic => ({ sourceItemId, code })
const canonicalDiagnostics = (diagnostics: readonly DetailedRendererAdapterDiagnostic[]): readonly DetailedRendererAdapterDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.sourceItemId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.sourceItemId, right.sourceItemId) || compare(left.code, right.code))
const staticContentSafety = (): MedievalContentSafetyClassification => classifyMedievalContent('player-facing-text', ['civil-life', 'navigation'], 'not-applicable', ['data'])
const presentationEvidence = (recordId: string): TerminalEvidenceProvenance => ({
  source: { kind: 'presentation-contract', recordId },
  recordedAtWorldTime: 0,
  knownAtWorldTime: 0,
  freshness: { kind: 'timeless' }
})
const neutralMetadata = (sourceItemId: string, accessibilityText: string): DetailedRendererSourceMetadata => ({
  sourceItemId,
  paletteToken: 'statusNeutral',
  presentationState: 'neutral',
  nonColorCue: terminalNonColorCueFor('neutral'),
  accessibilityText,
  evidence: presentationEvidence(sourceItemId),
  contentDomain: 'player-facing-text',
  contentSafety: staticContentSafety()
})
const priorityState = (priority: ManagementSidebarFact['priority']): TerminalPresentationState => priority === 'urgent' ? 'risk' : priority === 'essential' ? 'ready' : 'neutral'
const sidebarAccessibilityText = (fact: ManagementSidebarFact): string => `${MANAGEMENT_SIDEBAR_SECTION_LABELS[fact.category]}. ${fact.kind}. Source ${fact.source.label}. Known at world minute ${fact.discoveredAtWorldTime}. ${managementSidebarFreshnessLabel(fact.freshness)}.`
const sidebarMetadata = (fact: ManagementSidebarFact): DetailedRendererSourceMetadata => {
  const state = priorityState(fact.priority)
  return {
    sourceItemId: fact.id,
    paletteToken: state === 'risk' ? 'statusRisk' : state === 'ready' ? 'statusReady' : 'statusNeutral',
    presentationState: state,
    nonColorCue: terminalNonColorCueFor(state),
    accessibilityText: sidebarAccessibilityText(fact),
    evidence: {
      source: { kind: fact.source.type === 'frontier-knowledge' ? 'authoritative-record' : 'household-state', recordId: fact.source.recordId },
      recordedAtWorldTime: fact.recordedAtWorldTime,
      knownAtWorldTime: fact.discoveredAtWorldTime,
      freshness: fact.freshness.kind === 'reported-freshness' ? { kind: 'reported-at-world-time', atWorldTime: fact.freshness.atWorldTime } : { kind: fact.freshness.kind }
    },
    contentDomain: fact.contentDomain,
    contentSafety: fact.contentSafety
  }
}
const statusMetadata = (status: TerminalStatusItem): DetailedRendererSourceMetadata => ({
  sourceItemId: status.id,
  paletteToken: status.paletteToken,
  presentationState: status.state,
  nonColorCue: status.nonColorCue,
  accessibilityText: status.accessibilityText,
  evidence: status.evidence,
  contentDomain: status.contentDomain,
  contentSafety: status.contentSafety
})
const messageMetadata = (message: TerminalMessage): DetailedRendererSourceMetadata => ({
  sourceItemId: message.id,
  paletteToken: message.paletteToken,
  presentationState: message.state,
  nonColorCue: message.nonColorCue,
  accessibilityText: message.accessibilityText,
  evidence: message.evidence,
  contentDomain: message.contentDomain,
  contentSafety: message.contentSafety
})
const promptMetadata = (prompt: TerminalPrompt): DetailedRendererSourceMetadata => ({
  ...neutralMetadata(prompt.id, prompt.accessibilityText),
  evidence: prompt.evidence,
  contentDomain: prompt.contentDomain,
  contentSafety: prompt.contentSafety
})
const mapMetadata = (map: TerminalMapSurface): DetailedRendererSourceMetadata => ({
  ...neutralMetadata('terminal-map', map.accessibilityText),
  evidence: map.evidence,
  contentDomain: map.contentDomain,
  contentSafety: map.contentSafety
})
const legendMetadata = (legend: TerminalMapLegend): DetailedRendererSourceMetadata => ({
  ...neutralMetadata('terminal-map-legend', legend.accessibilityText),
  evidence: legend.evidence,
  contentDomain: legend.contentDomain,
  contentSafety: legend.contentSafety
})
const legendEntryMetadata = (entry: TerminalMapLegendEntry): DetailedRendererSourceMetadata => ({
  sourceItemId: entry.id,
  paletteToken: entry.paletteToken,
  presentationState: entry.presentationState,
  nonColorCue: entry.nonColorCue,
  accessibilityText: entry.accessibilityText,
  evidence: entry.evidence,
  contentDomain: entry.contentDomain,
  contentSafety: entry.contentSafety
})

const canonicalJson = (value: unknown): string => {
  if (Array.isArray(value)) return `[${value.map(canonicalJson).join(',')}]`
  if (record(value)) return `{${Object.keys(value).sort(compare).map(key => `${JSON.stringify(key)}:${canonicalJson(value[key])}`).join(',')}}`
  return JSON.stringify(value)
}

const fingerprint = (value: unknown): string => {
  const encoded = canonicalJson(value)
  let hash = 0x811c9dc5
  for (let index = 0; index < encoded.length; index += 1) {
    hash ^= encoded.charCodeAt(index)
    hash = Math.imul(hash, 0x01000193)
  }
  return `dr1-${(hash >>> 0).toString(16).padStart(8, '0')}`
}

const sourcePayload = (bundle: DetailedRendererSourceBundleInput): DetailedRendererSourceBundleInput => ({
  version: DETAILED_RENDERER_SOURCE_BUNDLE_VERSION,
  terminal: bundle.terminal,
  sidebar: bundle.sidebar,
  glyphCatalog: bundle.glyphCatalog,
  controls: bundle.controls
})

const identityFor = (bundle: DetailedRendererSourceBundleInput): DetailedRendererSourceIdentity => {
  const sourceFingerprint = fingerprint(sourcePayload(bundle))
  return {
    id: `detailed-source:${bundle.terminal.worldId}:${sourceFingerprint}`,
    fingerprint: sourceFingerprint,
    terminalContractVersion: bundle.terminal.version,
    sidebarContractVersion: bundle.sidebar.version,
    glyphCatalogVersion: bundle.glyphCatalog.version,
    controlsVersion: bundle.controls.version
  }
}

const isDirectWorldShape = (value: Record<string, unknown>): boolean => Object.hasOwn(value, 'state') || Object.hasOwn(value, 'initialWorld') || Object.hasOwn(value, 'manifest') || Object.hasOwn(value, 'jomon') || Object.hasOwn(value, 'crew')

/** Validates only renderer-neutral source inputs. It cannot validate or access a world. */
export const validateDetailedRendererSourceBundle = (value: unknown): readonly DetailedRendererAdapterDiagnostic[] => {
  if (!record(value)) return [issue('detailed-source', 'detailed-renderer.malformed-source-bundle')]
  if (isDirectWorldShape(value)) return [issue('detailed-source', 'detailed-renderer.direct-world-input')]
  if (!hasOnlyKeys(value, ['version', 'terminal', 'sidebar', 'glyphCatalog', 'controls', 'source'])) return [issue('detailed-source', 'detailed-renderer.malformed-source-bundle')]
  const diagnostics: DetailedRendererAdapterDiagnostic[] = []
  if (value.version !== DETAILED_RENDERER_SOURCE_BUNDLE_VERSION) diagnostics.push(issue('detailed-source', 'detailed-renderer.malformed-source-bundle'))
  const glyphDiagnostics = validateAsciiGlyphCatalog(value.glyphCatalog)
  if (glyphDiagnostics.length) diagnostics.push(...glyphDiagnostics.map(diagnostic => issue(diagnostic.recordId, diagnostic.code.startsWith('content-safety.') ? 'detailed-renderer.unsafe-content' : 'detailed-renderer.invalid-glyph-catalog')))
  const glyphCatalog = glyphDiagnostics.length === 0 ? terminalGlyphCatalog(value.glyphCatalog as AsciiGlyphCatalog) : terminalGlyphCatalog(JOMON_ASCII_GLYPH_CATALOG)
  const terminalDiagnostics = validateTerminalPresentationProjection(value.terminal, glyphCatalog)
  if (terminalDiagnostics.length) diagnostics.push(...terminalDiagnostics.map(diagnostic => issue(diagnostic.recordId, diagnostic.code.startsWith('content-safety.') ? 'detailed-renderer.unsafe-content' : 'detailed-renderer.invalid-terminal-projection')))
  const sidebarDiagnostics = validateManagementSidebarProjection(value.sidebar)
  if (sidebarDiagnostics.length) diagnostics.push(...sidebarDiagnostics.map(diagnostic => issue(diagnostic.id, diagnostic.code.startsWith('content-safety.') ? 'detailed-renderer.unsafe-content' : 'detailed-renderer.invalid-sidebar-projection')))
  const controlsDiagnostics = validateTerminalControlPreferences(value.controls)
  if (controlsDiagnostics.length) diagnostics.push(...controlsDiagnostics.map(diagnostic => issue(diagnostic.controlId, 'detailed-renderer.invalid-controls')))
  if (record(value.terminal) && record(value.sidebar) && value.terminal.worldId !== value.sidebar.worldId) diagnostics.push(issue('detailed-source', 'detailed-renderer.mixed-world-source'))
  if (!diagnostics.length) {
    const untrusted = value as unknown as DetailedRendererSourceBundle
    const expected = identityFor(untrusted)
    if (!same(untrusted.source, expected)) diagnostics.push(issue('detailed-source', 'detailed-renderer.invalid-source-fingerprint'))
  }
  return canonicalDiagnostics(diagnostics)
}

/** Creates the only supported input object for a future detailed renderer. */
export const createDetailedRendererSourceBundle = (input: DetailedRendererSourceBundleInput): DetailedRendererSourceBundle => {
  if (!record(input) || isDirectWorldShape(input)) throw new DetailedRendererAdapterError([issue('detailed-source', isDirectWorldShape(input as unknown as Record<string, unknown>) ? 'detailed-renderer.direct-world-input' : 'detailed-renderer.malformed-source-bundle')])
  if (!hasOnlyKeys(input, ['version', 'terminal', 'sidebar', 'glyphCatalog', 'controls'])) throw new DetailedRendererAdapterError([issue('detailed-source', 'detailed-renderer.malformed-source-bundle')])
  let provisional: DetailedRendererSourceBundle
  try {
    provisional = { ...clone(input), source: identityFor(input) }
  } catch {
    throw new DetailedRendererAdapterError([issue('detailed-source', 'detailed-renderer.malformed-source-bundle')])
  }
  const diagnostics = validateDetailedRendererSourceBundle(provisional)
  if (diagnostics.length) throw new DetailedRendererAdapterError(diagnostics)
  return provisional
}

const buildDetailedRendererAdapterModel = (bundle: DetailedRendererSourceBundle): DetailedRendererAdapterModel => {
  const terminal = bundle.terminal
  const sidebar = bundle.sidebar
  const map = terminal.map
  const help = createTerminalCommandHelpModel(bundle.controls)
  const legend: DetailedRendererMapLegend = {
    sourceItemId: 'terminal-map-legend',
    legend: clone(terminal.legend),
    metadata: legendMetadata(terminal.legend),
    entries: terminal.legend.entries.map(entry => ({
      sourceItemId: entry.id,
      entry: clone(entry),
      glyph: clone(entry.glyph),
      metadata: legendEntryMetadata(entry)
    }))
  }
  const detailedMap: DetailedRendererMapSurface = {
    sourceItemId: 'terminal-map',
    map: clone(map),
    metadata: mapMetadata(map),
    cells: map.state === 'materialized'
      ? map.cells.map(cell => ({ sourceItemId: cell.id, cell: clone(cell), glyph: clone(cell.glyph), metadata: {
          sourceItemId: cell.id,
          paletteToken: cell.paletteToken,
          presentationState: cell.presentationState,
          nonColorCue: clone(cell.nonColorCue),
          accessibilityText: cell.accessibilityText,
          evidence: clone(cell.evidence),
          contentDomain: cell.contentDomain,
          contentSafety: clone(cell.contentSafety)
        } }))
      : [],
    legend
  }
  const status = terminal.status.map(item => ({ sourceItemId: item.id, status: clone(item), metadata: statusMetadata(item) }))
  const messages = terminal.messages.map(item => ({ sourceItemId: item.id, message: clone(item), metadata: messageMetadata(item) }))
  const prompts = terminal.prompts.map(prompt => ({ sourceItemId: prompt.id, prompt: clone(prompt), metadata: promptMetadata(prompt) }))
  const commands = terminal.input.commands.map(command => ({
    sourceItemId: command.id,
    commandId: command.id,
    availability: command.availability,
    accessibilityText: command.accessibilityLabel,
    metadata: neutralMetadata(command.id, command.accessibilityLabel)
  }))
  const controls = help.entries.map(entry => ({
    sourceItemId: `terminal-control:${entry.controlId}`,
    entry: clone(entry),
    metadata: neutralMetadata(`terminal-control:${entry.controlId}`, entry.accessibilityText)
  }))
  const sidebarSections = sidebar.sections.map(section => ({
    sectionId: section.id,
    facts: section.facts.map(fact => ({ sourceItemId: fact.id, fact: clone(fact), metadata: sidebarMetadata(fact) }))
  }))
  const sidebarFacts = sidebarSections.flatMap(section => section.facts)
  return {
    version: DETAILED_RENDERER_ADAPTER_CONTRACT_VERSION,
    source: clone(bundle.source),
    parityRules: DETAILED_RENDERER_PARITY_RULES,
    glyphCatalog: clone(bundle.glyphCatalog),
    map: detailedMap,
    status,
    messages,
    prompts,
    commands,
    controls,
    sidebar: { sourceItemId: 'management-sidebar', sections: sidebarSections, summary: clone(sidebar.summary) },
    accessibility: {
      conciseSummary: `${terminal.accessibility.conciseSummary} ${sidebar.summary.knownFactCount} household-known sidebar facts remain separately addressable.`,
      mapText: terminal.accessibility.mapText,
      legendText: terminal.accessibility.legendText,
      statusText: terminal.accessibility.statusText,
      messageText: terminal.accessibility.messageText,
      promptText: terminal.accessibility.promptText,
      commandText: [...terminal.accessibility.commandText, ...help.entries.map(entry => entry.accessibilityText)],
      sidebarText: sidebarFacts.map(item => item.metadata.accessibilityText)
    },
    decorations: [],
    interactionBoundary: { sharesEffectiveCommandIds: true, promptCancellation: 'cancelled-no-mutation', executesInput: false, advancesWorldTime: false, mutatesWorld: false }
  }
}

/** Pure adaptation of a validated bundle; this performs no renderer or world action. */
export const createDetailedRendererAdapterModel = (bundle: DetailedRendererSourceBundle): DetailedRendererAdapterModel => {
  const diagnostics = validateDetailedRendererSourceBundle(bundle)
  if (diagnostics.length) throw new DetailedRendererAdapterError(diagnostics)
  return buildDetailedRendererAdapterModel(clone(bundle))
}

const checked = (model: DetailedRendererAdapterModel | undefined): DetailedRendererParityReport['checked'] => ({
  mapCells: model?.map.cells.length ?? 0,
  legendEntries: model?.map.legend.entries.length ?? 0,
  status: model?.status.length ?? 0,
  messages: model?.messages.length ?? 0,
  prompts: model?.prompts.length ?? 0,
  commands: model?.commands.length ?? 0,
  controls: model?.controls.length ?? 0,
  sidebarFacts: model?.sidebar.sections.flatMap(section => section.facts).length ?? 0
})

const sourceItems = (model: DetailedRendererAdapterModel): readonly { id: string; payload: unknown; accessibility: string; semantic: unknown; glyph?: unknown }[] => [
  ...model.map.cells.map(item => ({ id: `map:${item.sourceItemId}`, payload: item.cell, accessibility: item.metadata.accessibilityText, semantic: { paletteToken: item.metadata.paletteToken, presentationState: item.metadata.presentationState, nonColorCue: item.metadata.nonColorCue }, glyph: item.glyph })),
  { id: `legend:${model.map.legend.sourceItemId}`, payload: model.map.legend.legend, accessibility: model.map.legend.metadata.accessibilityText, semantic: { paletteToken: model.map.legend.metadata.paletteToken, presentationState: model.map.legend.metadata.presentationState, nonColorCue: model.map.legend.metadata.nonColorCue } },
  ...model.map.legend.entries.map(item => ({ id: `legend-entry:${item.sourceItemId}`, payload: item.entry, accessibility: item.metadata.accessibilityText, semantic: { paletteToken: item.metadata.paletteToken, presentationState: item.metadata.presentationState, nonColorCue: item.metadata.nonColorCue }, glyph: item.glyph })),
  ...model.status.map(item => ({ id: `status:${item.sourceItemId}`, payload: item.status, accessibility: item.metadata.accessibilityText, semantic: { paletteToken: item.metadata.paletteToken, presentationState: item.metadata.presentationState, nonColorCue: item.metadata.nonColorCue } })),
  ...model.messages.map(item => ({ id: `message:${item.sourceItemId}`, payload: item.message, accessibility: item.metadata.accessibilityText, semantic: { paletteToken: item.metadata.paletteToken, presentationState: item.metadata.presentationState, nonColorCue: item.metadata.nonColorCue } })),
  ...model.prompts.map(item => ({ id: `prompt:${item.sourceItemId}`, payload: item.prompt, accessibility: item.metadata.accessibilityText, semantic: { paletteToken: item.metadata.paletteToken, presentationState: item.metadata.presentationState, nonColorCue: item.metadata.nonColorCue } })),
  ...model.commands.map(item => ({ id: `command:${item.sourceItemId}`, payload: { commandId: item.commandId, availability: item.availability }, accessibility: item.accessibilityText, semantic: { paletteToken: item.metadata.paletteToken, presentationState: item.metadata.presentationState, nonColorCue: item.metadata.nonColorCue } })),
  ...model.controls.map(item => ({ id: `control:${item.sourceItemId}`, payload: item.entry, accessibility: item.metadata.accessibilityText, semantic: { paletteToken: item.metadata.paletteToken, presentationState: item.metadata.presentationState, nonColorCue: item.metadata.nonColorCue } })),
  ...model.sidebar.sections.flatMap(section => section.facts.map(item => ({ id: `sidebar:${item.sourceItemId}`, payload: item.fact, accessibility: item.metadata.accessibilityText, semantic: { paletteToken: item.metadata.paletteToken, presentationState: item.metadata.presentationState, nonColorCue: item.metadata.nonColorCue } })))
]

const modelMetadata = (model: DetailedRendererAdapterModel): readonly DetailedRendererSourceMetadata[] => [
  model.map.metadata,
  ...model.map.cells.map(item => item.metadata),
  model.map.legend.metadata,
  ...model.map.legend.entries.map(item => item.metadata),
  ...model.status.map(item => item.metadata),
  ...model.messages.map(item => item.metadata),
  ...model.prompts.map(item => item.metadata),
  ...model.commands.map(item => item.metadata),
  ...model.controls.map(item => item.metadata),
  ...model.sidebar.sections.flatMap(section => section.facts.map(item => item.metadata))
]

const validateDecorations = (value: unknown): readonly DetailedRendererAdapterDiagnostic[] => {
  if (!Array.isArray(value) || value.length > DETAILED_RENDERER_ADAPTER_LIMITS.decorations) return [issue('detailed-decoration', 'detailed-renderer.invalid-decoration')]
  const diagnostics: DetailedRendererAdapterDiagnostic[] = []
  const ids = new Set<string>()
  for (const candidate of value) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'detailed-decoration'
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'kind', 'nonAuthoritative'])) {
      diagnostics.push(issue(id, record(candidate) && Object.hasOwn(candidate, 'text') ? 'detailed-renderer.hidden-data-attempt' : 'detailed-renderer.invalid-decoration'))
      continue
    }
    if (!validId(candidate.id) || (candidate.kind !== 'frame' && candidate.kind !== 'texture' && candidate.kind !== 'spacing') || candidate.nonAuthoritative !== true) diagnostics.push(issue(id, 'detailed-renderer.invalid-decoration'))
    if (ids.has(id)) diagnostics.push(issue(id, 'detailed-renderer.duplicate-source-item'))
    ids.add(id)
  }
  return canonicalDiagnostics(diagnostics)
}

/**
 * Reports whether a candidate detailed model is an exact equal-information
 * representation of the bundle. The report is deterministic and has no
 * authority to execute input, persist preferences, or mutate a world.
 */
const reportDetailedRendererParityUnchecked = (bundle: DetailedRendererSourceBundle, value: unknown): DetailedRendererParityReport => {
  const bundleDiagnostics = validateDetailedRendererSourceBundle(bundle)
  if (bundleDiagnostics.length) return { status: 'rejected', source: undefined, checked: checked(undefined), diagnostics: bundleDiagnostics }
  const expected = buildDetailedRendererAdapterModel(bundle)
  if (!record(value) || !hasOnlyKeys(value, ['version', 'source', 'parityRules', 'glyphCatalog', 'map', 'status', 'messages', 'prompts', 'commands', 'controls', 'sidebar', 'accessibility', 'decorations', 'interactionBoundary'])) return { status: 'rejected', source: expected.source, checked: checked(undefined), diagnostics: [issue('detailed-model', record(value) && isDirectWorldShape(value) ? 'detailed-renderer.direct-world-input' : 'detailed-renderer.malformed-model')] }
  const candidate = value as unknown as DetailedRendererAdapterModel
  if (!record(candidate.map) || !Array.isArray(candidate.map.cells) || !record(candidate.map.legend) || !Array.isArray(candidate.map.legend.entries) || !Array.isArray(candidate.status) || !Array.isArray(candidate.messages) || !Array.isArray(candidate.prompts) || !Array.isArray(candidate.commands) || !Array.isArray(candidate.controls) || !record(candidate.sidebar) || !Array.isArray(candidate.sidebar.sections) || !record(candidate.accessibility) || !record(candidate.interactionBoundary)) {
    return { status: 'rejected', source: expected.source, checked: checked(undefined), diagnostics: [issue('detailed-model', 'detailed-renderer.malformed-model')] }
  }
  const diagnostics: DetailedRendererAdapterDiagnostic[] = []
  if (candidate.version !== DETAILED_RENDERER_ADAPTER_CONTRACT_VERSION || !same(candidate.parityRules, DETAILED_RENDERER_PARITY_RULES)) diagnostics.push(issue('detailed-model', 'detailed-renderer.source-mismatch'))
  if (!same(candidate.source, expected.source)) diagnostics.push(issue('detailed-source', same(candidate.source?.id, expected.source.id) ? 'detailed-renderer.stale-source' : 'detailed-renderer.source-mismatch'))
  if (!same(candidate.glyphCatalog, expected.glyphCatalog) || validateAsciiGlyphCatalog(candidate.glyphCatalog).length) diagnostics.push(issue('detailed-glyph-catalog', 'detailed-renderer.invalid-glyph-mapping'))
  if (!same(candidate.map.map, expected.map.map) || !same(candidate.map.sourceItemId, expected.map.sourceItemId)) diagnostics.push(issue('terminal-map', 'detailed-renderer.source-mismatch'))
  const expectedItems = sourceItems(expected)
  const candidateItems = sourceItems(candidate)
  if (candidateItems.length > DETAILED_RENDERER_ADAPTER_LIMITS.visualItems) diagnostics.push(issue('detailed-model', 'detailed-renderer.malformed-model'))
  const candidateById = new Map<string, typeof candidateItems[number]>()
  for (const item of candidateItems) {
    if (candidateById.has(item.id)) diagnostics.push(issue(item.id, 'detailed-renderer.duplicate-source-item'))
    candidateById.set(item.id, item)
  }
  const expectedIds = new Set(expectedItems.map(item => item.id))
  for (const expectedItem of expectedItems) {
    const actual = candidateById.get(expectedItem.id)
    if (!actual) { diagnostics.push(issue(expectedItem.id, 'detailed-renderer.missing-source-item')); continue }
    if (!same(actual.payload, expectedItem.payload)) diagnostics.push(issue(expectedItem.id, 'detailed-renderer.source-mismatch'))
    if (actual.accessibility !== expectedItem.accessibility) diagnostics.push(issue(expectedItem.id, 'detailed-renderer.altered-accessibility-text'))
    if (!same(actual.semantic, expectedItem.semantic)) diagnostics.push(issue(expectedItem.id, 'detailed-renderer.missing-semantic-state'))
    if (!same(actual.glyph, expectedItem.glyph)) diagnostics.push(issue(expectedItem.id, 'detailed-renderer.invalid-glyph-mapping'))
  }
  for (const candidateItem of candidateItems) if (!expectedIds.has(candidateItem.id)) diagnostics.push(issue(candidateItem.id, 'detailed-renderer.unexpected-source-item'))
  if (!same(candidate.sidebar.summary, expected.sidebar.summary) || !same(candidate.sidebar.sourceItemId, expected.sidebar.sourceItemId)) diagnostics.push(issue('management-sidebar', 'detailed-renderer.source-mismatch'))
  if (!same(candidate.accessibility, expected.accessibility)) diagnostics.push(issue('detailed-accessibility', 'detailed-renderer.altered-accessibility-text'))
  if (!same(candidate.interactionBoundary, expected.interactionBoundary)) diagnostics.push(issue('detailed-input', 'detailed-renderer.input-authority-violation'))
  const safety = auditMedievalContentSafety(modelMetadata(candidate).map(item => ({ id: `detailed-renderer:${item.sourceItemId}`, domain: item.contentDomain, classification: item.contentSafety })))
  if (safety.status === 'rejected') diagnostics.push(...safety.diagnostics.map(item => issue(item.contentId, 'detailed-renderer.unsafe-content')))
  diagnostics.push(...validateDecorations(candidate.decorations))
  return { status: diagnostics.length ? 'rejected' : 'accepted', source: expected.source, checked: checked(candidate), diagnostics: canonicalDiagnostics(diagnostics) }
}

/** A malformed prospective renderer model is contained as a canonical rejection. */
export const reportDetailedRendererParity = (bundle: DetailedRendererSourceBundle, value: unknown): DetailedRendererParityReport => {
  try {
    return reportDetailedRendererParityUnchecked(bundle, value)
  } catch {
    return {
      status: 'rejected',
      source: undefined,
      checked: checked(undefined),
      diagnostics: [issue('detailed-model', 'detailed-renderer.malformed-model')]
    }
  }
}

/** Throws on any malformed, stale, unsafe, or unequal-information model. */
export const validateDetailedRendererAdapterModel = (bundle: DetailedRendererSourceBundle, value: unknown): readonly DetailedRendererAdapterDiagnostic[] => reportDetailedRendererParity(bundle, value).diagnostics

/** Convenience guard for consumers that need a fail-closed adapter boundary. */
export const assertDetailedRendererParity = (bundle: DetailedRendererSourceBundle, value: unknown): DetailedRendererAdapterModel => {
  const diagnostics = validateDetailedRendererAdapterModel(bundle, value)
  if (diagnostics.length) throw new DetailedRendererAdapterError(diagnostics)
  return clone(value as DetailedRendererAdapterModel)
}
