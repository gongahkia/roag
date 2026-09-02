import { auditMedievalContentSafety, classifyMedievalContent, contentSafetyAuditMatches, type MedievalContentDomain, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode, type MedievalContentSafetyTag } from './content-safety'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE, type JomonPaletteToken } from './palette'
import {
  TERMINAL_GLYPH_VOCABULARY_ID,
  TERMINAL_PRESENTATION_STATES,
  TERMINAL_STATE_PRESENTATIONS,
  terminalNonColorCueFor,
  type TerminalGlyphCatalog,
  type TerminalGlyphReference,
  type TerminalNonColorCue,
  type TerminalPresentationState
} from './terminal-presentation'

/**
 * Jomon's authored, renderer-neutral ASCII vocabulary. A glyph is only a
 * semantic reference: a materialized cell still needs source-backed evidence
 * and accessibility text under the terminal-presentation contract.
 */
export const ASCII_GLYPH_CATALOG_VERSION = 1 as const

export const ASCII_GLYPH_LIMITS = {
  entries: 64,
  entriesPerCategoryMinimum: 4,
  idLength: 96,
  labelLength: 48,
  textLength: 160,
  viewportUses: 4
} as const

export const ASCII_GLYPH_CATEGORIES = [
  'terrain',
  'vessel-part',
  'person',
  'goods',
  'work',
  'hazard',
  'weather',
  'route'
] as const
export type AsciiGlyphCategory = typeof ASCII_GLYPH_CATEGORIES[number]

/** These are future placement contexts, not evidence that a map is materialized today. */
export const ASCII_GLYPH_VIEWPORT_USES = [
  'future-jomon-deck',
  'future-river-map',
  'future-settlement-map',
  'future-route-map'
] as const
export type AsciiGlyphViewportUse = typeof ASCII_GLYPH_VIEWPORT_USES[number]

export interface AsciiGlyphEntry {
  id: string
  category: AsciiGlyphCategory
  character: string
  label: string
  textEquivalent: string
  accessibilityText: string
  paletteToken: JomonPaletteToken
  presentationState: TerminalPresentationState
  nonColorCue: TerminalNonColorCue
  viewportUses: readonly AsciiGlyphViewportUse[]
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export interface AsciiGlyphCatalog {
  vocabulary: typeof TERMINAL_GLYPH_VOCABULARY_ID
  version: typeof ASCII_GLYPH_CATALOG_VERSION
  /** Catalog entries never reveal a world fact on their own. */
  factAuthority: 'semantic-reference-only'
  entries: readonly AsciiGlyphEntry[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export type AsciiGlyphDiagnosticCode =
  | 'ascii-glyphs.malformed-catalog'
  | 'ascii-glyphs.unknown-catalog-version'
  | 'ascii-glyphs.invalid-entry'
  | 'ascii-glyphs.duplicate-id'
  | 'ascii-glyphs.duplicate-character'
  | 'ascii-glyphs.noncanonical-order'
  | 'ascii-glyphs.missing-required-category'
  | 'ascii-glyphs.invalid-character'
  | 'ascii-glyphs.invalid-palette-token'
  | 'ascii-glyphs.invalid-non-color-cue'
  | 'ascii-glyphs.invalid-viewport-use'
  | 'ascii-glyphs.invalid-text'
  | 'ascii-glyphs.invalid-safety-audit'
  | MedievalContentSafetyDiagnosticCode

export interface AsciiGlyphDiagnostic {
  recordId: string
  code: AsciiGlyphDiagnosticCode
}

export class AsciiGlyphCatalogError extends Error {
  constructor(readonly diagnostics: readonly AsciiGlyphDiagnostic[]) {
    super(`ASCII glyph catalogue rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'AsciiGlyphCatalogError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort(compare)
  const expectedKeys = [...expected].sort(compare)
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const issue = (recordId: string, code: AsciiGlyphDiagnosticCode): AsciiGlyphDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly AsciiGlyphDiagnostic[]): readonly AsciiGlyphDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const validId = (value: unknown): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= ASCII_GLYPH_LIMITS.idLength
  && /^[a-z][a-z0-9:._-]*$/u.test(value)
const validAsciiCharacter = (value: unknown): value is string => typeof value === 'string'
  && value.length === 1
  && value.codePointAt(0) !== undefined
  && (value.codePointAt(0) as number) >= 0x21
  && (value.codePointAt(0) as number) <= 0x7e
const validLabel = (value: unknown): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= ASCII_GLYPH_LIMITS.labelLength
  && /^[A-Za-z][A-Za-z -]*$/u.test(value)
const validText = (value: unknown): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= ASCII_GLYPH_LIMITS.textLength
  && /^[A-Za-z0-9][A-Za-z0-9 .,;:'()/-]*$/u.test(value)
const paletteToken = (value: unknown): value is JomonPaletteToken => typeof value === 'string' && Object.hasOwn(JOMON_PALETTE, value)
const expectedCue = (state: TerminalPresentationState): TerminalNonColorCue => terminalNonColorCueFor(state)
const validCue = (value: unknown, state: TerminalPresentationState): value is TerminalNonColorCue => record(value)
  && hasOnlyKeys(value, ['key', 'text'])
  && same(value, expectedCue(state))
const canonicalStringList = <Value extends string>(value: unknown, allowed: readonly Value[], maximumLength: number): value is readonly Value[] => Array.isArray(value)
  && value.length > 0
  && value.length <= maximumLength
  && value.every(item => oneOf(allowed, item))
  && value.every((item, index) => index === 0 || compare(value[index - 1] as string, item as string) < 0)

const glyph = (
  id: string,
  category: AsciiGlyphCategory,
  character: string,
  label: string,
  textEquivalent: string,
  accessibilityText: string,
  paletteToken: JomonPaletteToken,
  presentationState: TerminalPresentationState,
  viewportUses: readonly AsciiGlyphViewportUse[],
  tags: readonly MedievalContentSafetyTag[],
  participantScope: 'adults-only' | 'not-applicable' = 'not-applicable'
): AsciiGlyphEntry => ({
  id,
  category,
  character,
  label,
  textEquivalent,
  accessibilityText,
  paletteToken,
  presentationState,
  nonColorCue: expectedCue(presentationState),
  viewportUses: [...viewportUses].sort(compare),
  contentDomain: 'template',
  contentSafety: classifyMedievalContent('template', tags, participantScope, ['player-facing-text'])
})

const authoredEntries = [
  glyph('terrain:reed-marsh', 'terrain', '"', 'Reed marsh', 'Reed marsh terrain.', 'Reed marsh terrain; neutral state cue is present.', 'water', 'neutral', ['future-river-map', 'future-route-map'], ['environment', 'navigation']),
  glyph('terrain:river-channel', 'terrain', '~', 'River channel', 'River channel terrain.', 'River channel terrain; neutral state cue is present.', 'water', 'neutral', ['future-river-map', 'future-route-map'], ['environment', 'navigation']),
  glyph('terrain:shallow-shoal', 'terrain', ':', 'Shallow shoal', 'Shallow shoal terrain.', 'Shallow shoal terrain; warning cue is present.', 'warningText', 'warning', ['future-river-map', 'future-route-map'], ['environment', 'navigation', 'ordinary-hardship']),
  glyph('terrain:tidal-shore', 'terrain', ',', 'Tidal shore', 'Tidal shore terrain.', 'Tidal shore terrain; neutral state cue is present.', 'water', 'neutral', ['future-river-map', 'future-settlement-map'], ['environment', 'navigation']),
  glyph('vessel:gangplank', 'vessel-part', '/', 'Gangplank', 'Vessel gangplank.', 'Vessel gangplank; route cue is present.', 'route', 'ready', ['future-jomon-deck', 'future-settlement-map'], ['craft', 'navigation']),
  glyph('vessel:hull-planking', 'vessel-part', '#', 'Hull planking', 'Vessel hull planking.', 'Vessel hull planking; neutral state cue is present.', 'bodyText', 'neutral', ['future-jomon-deck'], ['craft', 'navigation']),
  glyph('vessel:mast-rigging', 'vessel-part', '|', 'Mast rigging', 'Vessel mast and rigging.', 'Vessel mast and rigging; neutral state cue is present.', 'bodyText', 'neutral', ['future-jomon-deck'], ['craft', 'navigation']),
  glyph('vessel:open-deck', 'vessel-part', '=', 'Open deck', 'Open vessel deck.', 'Open vessel deck; neutral state cue is present.', 'bodyText', 'neutral', ['future-jomon-deck'], ['craft', 'navigation']),
  glyph('person:active-courier', 'person', '@', 'Active courier', 'Active adult courier.', 'Active adult courier; ready state cue is present.', 'selectedText', 'ready', ['future-jomon-deck', 'future-river-map', 'future-settlement-map'], ['adult-labour', 'civil-life', 'navigation'], 'adults-only'),
  glyph('person:crew-adult', 'person', 'p', 'Crew adult', 'Adult household crew member.', 'Adult household crew member; neutral state cue is present.', 'bodyText', 'neutral', ['future-jomon-deck', 'future-river-map', 'future-settlement-map'], ['adult-labour', 'civil-life'], 'adults-only'),
  glyph('person:quay-worker', 'person', 'w', 'Quay worker', 'Adult quay worker.', 'Adult quay worker; neutral state cue is present.', 'bodyText', 'neutral', ['future-settlement-map'], ['adult-labour', 'civil-life', 'settlement'], 'adults-only'),
  glyph('person:traveller-adult', 'person', 'v', 'Travelling adult', 'Adult traveller.', 'Adult traveller; neutral state cue is present.', 'mutedText', 'neutral', ['future-river-map', 'future-route-map', 'future-settlement-map'], ['adult-labour', 'civil-life', 'travel'], 'adults-only'),
  glyph('goods:bale', 'goods', 'b', 'Goods bale', 'Bound goods bale.', 'Bound goods bale; neutral state cue is present.', 'bodyText', 'neutral', ['future-jomon-deck', 'future-settlement-map'], ['commerce', 'craft']),
  glyph('goods:cask', 'goods', 'o', 'Goods cask', 'Sealed goods cask.', 'Sealed goods cask; neutral state cue is present.', 'bodyText', 'neutral', ['future-jomon-deck', 'future-settlement-map'], ['commerce', 'craft']),
  glyph('goods:crate', 'goods', 'c', 'Goods crate', 'Packed goods crate.', 'Packed goods crate; neutral state cue is present.', 'bodyText', 'neutral', ['future-jomon-deck', 'future-settlement-map'], ['commerce', 'craft']),
  glyph('goods:paper-bundle', 'goods', 'd', 'Paper bundle', 'Bound paper records.', 'Bound paper records; neutral state cue is present.', 'bodyText', 'neutral', ['future-jomon-deck', 'future-settlement-map'], ['commerce', 'craft']),
  glyph('work:galley-duty', 'work', 'g', 'Galley duty', 'Active galley work.', 'Active galley work; ready state cue is present.', 'actionText', 'ready', ['future-jomon-deck'], ['adult-labour', 'craft'], 'adults-only'),
  glyph('work:rigging-work', 'work', '^', 'Rigging work', 'Active rigging work.', 'Active rigging work; ready state cue is present.', 'actionText', 'ready', ['future-jomon-deck'], ['adult-labour', 'craft'], 'adults-only'),
  glyph('work:repair-work', 'work', 'r', 'Repair work', 'Active repair work.', 'Active repair work; ready state cue is present.', 'actionText', 'ready', ['future-jomon-deck', 'future-settlement-map'], ['adult-labour', 'craft'], 'adults-only'),
  glyph('work:watch-duty', 'work', '!', 'Watch duty', 'Active watch duty.', 'Active watch duty; ready state cue is present.', 'actionText', 'ready', ['future-jomon-deck', 'future-settlement-map', 'future-route-map'], ['adult-labour', 'navigation'], 'adults-only'),
  glyph('hazard:foul-water', 'hazard', '*', 'Foul water', 'Material water hazard.', 'Material water hazard; risk cue is present.', 'statusRisk', 'risk', ['future-river-map', 'future-settlement-map'], ['environment', 'ordinary-hardship']),
  glyph('hazard:open-flame', 'hazard', 'f', 'Open flame', 'Material fire hazard.', 'Material fire hazard; risk cue is present.', 'statusRisk', 'risk', ['future-jomon-deck', 'future-settlement-map'], ['ordinary-hardship']),
  glyph('hazard:snapped-line', 'hazard', 'x', 'Snapped line', 'Material line hazard.', 'Material line hazard; warning cue is present.', 'warningText', 'warning', ['future-jomon-deck', 'future-route-map'], ['craft', 'ordinary-hardship']),
  glyph('hazard:unstable-boarding', 'hazard', '%', 'Unstable boarding', 'Material boarding hazard.', 'Material boarding hazard; risk cue is present.', 'statusRisk', 'risk', ['future-jomon-deck', 'future-settlement-map'], ['ordinary-hardship']),
  glyph('weather:fog', 'weather', '?', 'Fog', 'Fog weather condition.', 'Fog weather condition; waiting cue is present.', 'statusWaiting', 'waiting', ['future-river-map', 'future-route-map'], ['environment', 'navigation']),
  glyph('weather:ice', 'weather', '_', 'River ice', 'River ice weather condition.', 'River ice weather condition; warning cue is present.', 'warningText', 'warning', ['future-river-map', 'future-route-map'], ['environment', 'navigation', 'ordinary-hardship']),
  glyph('weather:rain', 'weather', '.', 'Rain', 'Rain weather condition.', 'Rain weather condition; neutral state cue is present.', 'water', 'neutral', ['future-river-map', 'future-route-map', 'future-settlement-map'], ['environment', 'navigation']),
  glyph('weather:wind', 'weather', '>', 'Wind', 'Wind weather condition.', 'Wind weather condition; waiting cue is present.', 'statusWaiting', 'waiting', ['future-river-map', 'future-route-map'], ['environment', 'navigation']),
  glyph('route:canal-link', 'route', ']', 'Canal link', 'Known canal route link.', 'Known canal route link; ready state cue is present.', 'route', 'ready', ['future-river-map', 'future-route-map'], ['navigation', 'travel']),
  glyph('route:charted-waterway', 'route', '}', 'Charted waterway', 'Known charted waterway route.', 'Known charted waterway route; ready state cue is present.', 'route', 'ready', ['future-river-map', 'future-route-map'], ['navigation', 'travel']),
  glyph('route:coastal-passage', 'route', '<', 'Coastal passage', 'Known coastal route passage.', 'Known coastal route passage; ready state cue is present.', 'route', 'ready', ['future-river-map', 'future-route-map'], ['navigation', 'travel']),
  glyph('route:quay-approach', 'route', ')', 'Quay approach', 'Known quay approach route.', 'Known quay approach route; ready state cue is present.', 'route', 'ready', ['future-jomon-deck', 'future-settlement-map', 'future-route-map'], ['navigation', 'travel'])
].sort((left, right) => compare(left.id, right.id))

const authoredAudit = auditMedievalContentSafety(authoredEntries)
if (authoredAudit.status === 'rejected') throw new AsciiGlyphCatalogError(authoredAudit.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))

/** The single closed Jomon catalogue. It does not create any current map cells. */
export const JOMON_ASCII_GLYPH_CATALOG: AsciiGlyphCatalog = {
  vocabulary: TERMINAL_GLYPH_VOCABULARY_ID,
  version: ASCII_GLYPH_CATALOG_VERSION,
  factAuthority: 'semantic-reference-only',
  entries: authoredEntries,
  contentSafetyAudit: authoredAudit
}

const validEntry = (value: unknown): value is AsciiGlyphEntry => record(value)
  && hasOnlyKeys(value, ['id', 'category', 'character', 'label', 'textEquivalent', 'accessibilityText', 'paletteToken', 'presentationState', 'nonColorCue', 'viewportUses', 'contentDomain', 'contentSafety'])
  && validId(value.id)
  && oneOf(ASCII_GLYPH_CATEGORIES, value.category)
  && validAsciiCharacter(value.character)
  && validLabel(value.label)
  && validText(value.textEquivalent)
  && validText(value.accessibilityText)
  && paletteToken(value.paletteToken)
  && oneOf(TERMINAL_PRESENTATION_STATES, value.presentationState)
  && validCue(value.nonColorCue, value.presentationState)
  && canonicalStringList(value.viewportUses, ASCII_GLYPH_VIEWPORT_USES, ASCII_GLYPH_LIMITS.viewportUses)
  && value.contentDomain === 'template'

/** Deterministic, fail-closed validation for authored or loaded catalogue data. */
export const validateAsciiGlyphCatalog = (value: unknown): readonly AsciiGlyphDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['vocabulary', 'version', 'factAuthority', 'entries', 'contentSafetyAudit'])) return [issue('ascii-glyph-catalog', 'ascii-glyphs.malformed-catalog')]
  const diagnostics: AsciiGlyphDiagnostic[] = []
  if (value.vocabulary !== TERMINAL_GLYPH_VOCABULARY_ID) diagnostics.push(issue('ascii-glyph-catalog', 'ascii-glyphs.malformed-catalog'))
  if (value.version !== ASCII_GLYPH_CATALOG_VERSION) diagnostics.push(issue('ascii-glyph-catalog', 'ascii-glyphs.unknown-catalog-version'))
  if (value.factAuthority !== 'semantic-reference-only' || !Array.isArray(value.entries) || value.entries.length > ASCII_GLYPH_LIMITS.entries) diagnostics.push(issue('ascii-glyph-catalog', 'ascii-glyphs.malformed-catalog'))
  const entries = Array.isArray(value.entries) ? value.entries : []
  const ids = new Set<string>()
  const characters = new Set<string>()
  const categoryCounts = new Map<AsciiGlyphCategory, number>()
  for (const candidate of entries) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'ascii-glyph-entry'
    if (!validEntry(candidate)) {
      diagnostics.push(issue(id, 'ascii-glyphs.invalid-entry'))
      if (record(candidate) && !validAsciiCharacter(candidate.character)) diagnostics.push(issue(id, 'ascii-glyphs.invalid-character'))
      if (record(candidate) && !paletteToken(candidate.paletteToken)) diagnostics.push(issue(id, 'ascii-glyphs.invalid-palette-token'))
      if (record(candidate) && oneOf(TERMINAL_PRESENTATION_STATES, candidate.presentationState) && !validCue(candidate.nonColorCue, candidate.presentationState)) diagnostics.push(issue(id, 'ascii-glyphs.invalid-non-color-cue'))
      if (record(candidate) && !canonicalStringList(candidate.viewportUses, ASCII_GLYPH_VIEWPORT_USES, ASCII_GLYPH_LIMITS.viewportUses)) diagnostics.push(issue(id, 'ascii-glyphs.invalid-viewport-use'))
      if (record(candidate) && (!validLabel(candidate.label) || !validText(candidate.textEquivalent) || !validText(candidate.accessibilityText))) diagnostics.push(issue(id, 'ascii-glyphs.invalid-text'))
      continue
    }
    if (ids.has(candidate.id)) diagnostics.push(issue(candidate.id, 'ascii-glyphs.duplicate-id'))
    ids.add(candidate.id)
    if (characters.has(candidate.character)) diagnostics.push(issue(candidate.id, 'ascii-glyphs.duplicate-character'))
    characters.add(candidate.character)
    categoryCounts.set(candidate.category, (categoryCounts.get(candidate.category) ?? 0) + 1)
  }
  const typedEntries = entries.filter(validEntry)
  if (typedEntries.some((entry, index) => index > 0 && compare(typedEntries[index - 1]!.id, entry.id) >= 0)) diagnostics.push(issue('ascii-glyph-catalog', 'ascii-glyphs.noncanonical-order'))
  for (const category of ASCII_GLYPH_CATEGORIES) {
    if ((categoryCounts.get(category) ?? 0) < ASCII_GLYPH_LIMITS.entriesPerCategoryMinimum) diagnostics.push(issue(`ascii-glyph-category:${category}`, 'ascii-glyphs.missing-required-category'))
  }
  if (!contentSafetyAuditMatches(typedEntries, value.contentSafetyAudit)) diagnostics.push(issue('ascii-glyph-catalog', 'ascii-glyphs.invalid-safety-audit'))
  const safety = auditMedievalContentSafety(typedEntries)
  if (safety.status === 'rejected') diagnostics.push(...safety.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  return canonicalDiagnostics(diagnostics)
}

/** The terminal seam receives only references; it never receives renderer-owned glyph text or colours. */
export const terminalGlyphCatalog = (catalog: AsciiGlyphCatalog = JOMON_ASCII_GLYPH_CATALOG): TerminalGlyphCatalog => {
  const diagnostics = validateAsciiGlyphCatalog(catalog)
  if (diagnostics.length) throw new AsciiGlyphCatalogError(diagnostics)
  return {
    vocabulary: catalog.vocabulary,
    version: catalog.version,
    glyphIds: catalog.entries.map(entry => entry.id)
  }
}

export const findAsciiGlyph = (id: string, catalog: AsciiGlyphCatalog = JOMON_ASCII_GLYPH_CATALOG): AsciiGlyphEntry | undefined => {
  const diagnostics = validateAsciiGlyphCatalog(catalog)
  if (diagnostics.length) throw new AsciiGlyphCatalogError(diagnostics)
  return catalog.entries.find(entry => entry.id === id)
}

export const terminalGlyphReferenceFor = (id: string, catalog: AsciiGlyphCatalog = JOMON_ASCII_GLYPH_CATALOG): TerminalGlyphReference => {
  const entry = findAsciiGlyph(id, catalog)
  if (!entry) throw new AsciiGlyphCatalogError([issue(id, 'ascii-glyphs.invalid-entry')])
  return { vocabulary: catalog.vocabulary, vocabularyVersion: catalog.version, id: entry.id }
}
