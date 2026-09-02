import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentDomain, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE, type JomonPaletteToken } from './palette'
import type { FoundationWorld, MedievalRoute } from './types'
import { validateFoundationWorld } from './world'

/**
 * Renderer-neutral terminal information contract. The current world has no
 * materialized spatial cells, so this module represents that absence rather
 * than leaking generator data or inventing a deck/map. ASCII and a future
 * detailed adapter must consume the same model without omitting or inventing
 * consequential information.
 */
export const TERMINAL_PRESENTATION_CONTRACT_VERSION = 2 as const

export const TERMINAL_PRESENTATION_LIMITS = {
  viewportWidth: 120,
  viewportHeight: 80,
  viewportCells: 9_600,
  statusItems: 3,
  messages: 12,
  prompts: 1,
  promptOptions: 8,
  inputCommands: 48,
  inputModes: 20,
  identityLength: 160,
  glyphReferenceLength: 96,
  textLength: 280
} as const

export const TERMINAL_GLYPH_VOCABULARY_ID = 'jomon-original-ascii-glyphs' as const

export const TERMINAL_RENDERER_PARITY_RULES = [
  'same-authoritative-terminal-model',
  'no-consequential-omission',
  'no-consequential-invention',
  'text-equivalent-required'
] as const
export type TerminalRendererParityRule = typeof TERMINAL_RENDERER_PARITY_RULES[number]

export const TERMINAL_PRESENTATION_STATES = ['ready', 'waiting', 'warning', 'risk', 'neutral'] as const
export type TerminalPresentationState = typeof TERMINAL_PRESENTATION_STATES[number]
export type TerminalNonColorCueKey = keyof typeof JOMON_NON_COLOR_STATE_CUES

export interface TerminalStatePresentation {
  paletteToken: JomonPaletteToken
  nonColorCueKey: TerminalNonColorCueKey
}

/** Every consequential visual state has both a semantic palette role and a textual/glyph cue. */
export const TERMINAL_STATE_PRESENTATIONS: Readonly<Record<TerminalPresentationState, TerminalStatePresentation>> = {
  ready: { paletteToken: 'statusReady', nonColorCueKey: 'readyState' },
  waiting: { paletteToken: 'statusWaiting', nonColorCueKey: 'awaitingCourierState' },
  warning: { paletteToken: 'warningText', nonColorCueKey: 'warningPrefix' },
  risk: { paletteToken: 'statusRisk', nonColorCueKey: 'errorPrefix' },
  neutral: { paletteToken: 'statusNeutral', nonColorCueKey: 'actionKeys' }
}

export interface TerminalNonColorCue {
  key: TerminalNonColorCueKey
  text: string
}

export const terminalNonColorCueFor = (state: TerminalPresentationState): TerminalNonColorCue => {
  const presentation = TERMINAL_STATE_PRESENTATIONS[state]
  return { key: presentation.nonColorCueKey, text: JOMON_NON_COLOR_STATE_CUES[presentation.nonColorCueKey] }
}

export type TerminalEvidenceSourceKind = 'presentation-contract' | 'household-state' | 'authoritative-record'
export type TerminalEvidenceFreshness =
  | { kind: 'current' }
  | { kind: 'timeless' }
  | { kind: 'reported-at-world-time'; atWorldTime: number }

/** Source and knowledge time are mandatory on every fact a renderer can show. */
export interface TerminalEvidenceProvenance {
  source: { kind: TerminalEvidenceSourceKind; recordId: string }
  recordedAtWorldTime: number
  knownAtWorldTime: number
  freshness: TerminalEvidenceFreshness
}

export interface TerminalCellCoordinate {
  column: number
  row: number
}

export type TerminalMapViewportContext = 'reserved-unmaterialized' | 'future-materialized'

export interface TerminalMapViewport {
  id: string
  context: TerminalMapViewportContext
  origin: TerminalCellCoordinate
  width: number
  height: number
}

/** This is a reference seam only; the following roadmap item owns glyph assignments. */
export interface TerminalGlyphReference {
  vocabulary: typeof TERMINAL_GLYPH_VOCABULARY_ID
  vocabularyVersion: number
  id: string
}

/** A future glyph vocabulary supplies its own closed ID set to this validator. */
export interface TerminalGlyphCatalog {
  vocabulary: typeof TERMINAL_GLYPH_VOCABULARY_ID
  version: number
  glyphIds: readonly string[]
}

export interface TerminalMaterializedCell {
  id: string
  coordinate: TerminalCellCoordinate
  glyph: TerminalGlyphReference
  paletteToken: JomonPaletteToken
  presentationState: TerminalPresentationState
  nonColorCue: TerminalNonColorCue
  textEquivalent: string
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export interface TerminalReservedMap {
  state: 'reserved-unmaterialized'
  viewport: TerminalMapViewport
  cells: readonly []
  textEquivalent: string
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export interface TerminalMaterializedMap {
  state: 'materialized'
  viewport: TerminalMapViewport
  cells: readonly TerminalMaterializedCell[]
  textEquivalent: string
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export type TerminalMapSurface = TerminalReservedMap | TerminalMaterializedMap

export type TerminalStatusValue =
  | { kind: 'map-reserved' }
  | { kind: 'courier-selection'; selected: boolean }
  | { kind: 'world-minute'; minutes: number }

/** Status is only immediate local/action context, never a second household ledger. */
export interface TerminalStatusItem {
  id: string
  state: TerminalPresentationState
  paletteToken: JomonPaletteToken
  nonColorCue: TerminalNonColorCue
  value: TerminalStatusValue
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export const TERMINAL_MESSAGE_KINDS = ['authoritative-arrival', 'authoritative-attention'] as const
export type TerminalMessageKind = typeof TERMINAL_MESSAGE_KINDS[number]

/** Future messages may route only a real known source record; none exist today. */
export interface TerminalMessage {
  id: string
  kind: TerminalMessageKind
  state: TerminalPresentationState
  paletteToken: JomonPaletteToken
  nonColorCue: TerminalNonColorCue
  sourceRecordKind: 'causal-command' | 'delegation-record' | 'social-memory-record' | 'future-authoritative-record'
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export const TERMINAL_PROMPT_DISABLED_REASONS = [
  'no-contextual-action-materialized',
  'missing-required-location',
  'requires-future-domain-rule'
] as const
export type TerminalPromptDisabledReason = typeof TERMINAL_PROMPT_DISABLED_REASONS[number]

export interface TerminalPromptOption {
  id: string
  key: TerminalKeyboardKey
  availability: 'available' | 'disabled'
  disabledReason?: TerminalPromptDisabledReason
  intent: 'future-contextual-action'
  requiresConfirmation: boolean
  nonColorCue: TerminalNonColorCue
  accessibilityText: string
}

/** Prompt options express intent only. They have no reducer or time authority. */
export interface TerminalPrompt {
  id: string
  kind: 'future-contextual-choice'
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
  options: readonly TerminalPromptOption[]
  cancellation: { key: 'Escape'; outcome: 'cancelled-no-mutation'; advancesWorldTime: false }
}

export interface TerminalPromptCancellation {
  id: string
  promptId: string
  outcome: 'cancelled-no-mutation'
  advancesWorldTime: false
}

/**
 * Input contexts are focus-specific canvas modes, not gameplay commands. This
 * lets the contract state exactly which existing `Enter`, text, and reset
 * behaviour is active without treating printable characters as commands.
 */
export const TERMINAL_INPUT_CONTEXTS = [
  'worlds-list',
  'settings-basic-seed-entry',
  'settings-basic-preset',
  'settings-basic-advanced-link',
  'settings-basic-profiles-link',
  'settings-basic-create-world',
  'settings-advanced-setting',
  'creation-profiles-name-entry',
  'creation-profiles-save-action',
  'creation-profiles-load-profile',
  'world-generation',
  'world-result',
  'choose-courier',
  'world',
  'chronicles-list',
  'chronicle',
  'future-contextual-prompt'
] as const
export type TerminalInputContext = typeof TERMINAL_INPUT_CONTEXTS[number]

export const TERMINAL_KEYBOARD_KEYS = [
  'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter', 'Escape', 'Backspace',
  'N', 'C', 'E', 'R', 'S', 'M', '[', ']', '?', 'F2', 'Y', 'U', 'H', 'J', 'K', 'L', 'B'
] as const
export type TerminalKeyboardKey = typeof TERMINAL_KEYBOARD_KEYS[number]

export type TerminalInputCommandAvailability = 'implemented' | 'reserved'
export type TerminalInputCommandSurface = 'navigation' | 'management' | 'settings' | 'text-entry' | 'export' | 'movement' | 'contextual-action' | 'remapping' | 'help'
export type TerminalInputCaseHandling = 'exact' | 'ascii-case-insensitive' | 'preserve-typed-case'
export type TerminalTextInputField = 'creation-seed' | 'creation-profile-name'
export type TerminalTextCharacterPolicy = 'ascii-word-space-period-comma-apostrophe-hyphen'

export interface TerminalKeyBinding {
  kind: 'key'
  key: TerminalKeyboardKey
  caseHandling: Exclude<TerminalInputCaseHandling, 'preserve-typed-case'>
}

/** A bounded character-entry surface, not one command per printable key. */
export interface TerminalBoundedTextBinding {
  kind: 'bounded-text-entry'
  field: TerminalTextInputField
  characterPolicy: TerminalTextCharacterPolicy
  maximumLength: number
  deletionKey: 'Backspace'
  caseHandling: 'preserve-typed-case'
}

export type TerminalKeyboardBinding = TerminalKeyBinding | TerminalBoundedTextBinding

export interface TerminalInputMode {
  id: TerminalInputContext
  route: MedievalRoute
  focus: 'list' | 'seed-entry' | 'preset' | 'settings-link' | 'advanced-setting' | 'profile-name-entry' | 'profile-save-action' | 'profile-load-action' | 'result' | 'management' | 'chronicle' | 'future-prompt'
  textEntry?: { field: TerminalTextInputField; maximumLength: number; characterPolicy: TerminalTextCharacterPolicy }
}

const inputCompare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const canonicalInputModes = (modes: readonly TerminalInputMode[]): readonly TerminalInputMode[] => [...modes].sort((left, right) => inputCompare(left.id, right.id))

/** Exact focused input surfaces currently implemented by the canvas adapter. */
export const TERMINAL_INPUT_MODES: readonly TerminalInputMode[] = canonicalInputModes([
  { id: 'worlds-list', route: 'worlds', focus: 'list' },
  { id: 'settings-basic-seed-entry', route: 'create-world', focus: 'seed-entry', textEntry: { field: 'creation-seed', maximumLength: 64, characterPolicy: 'ascii-word-space-period-comma-apostrophe-hyphen' } },
  { id: 'settings-basic-preset', route: 'create-world', focus: 'preset' },
  { id: 'settings-basic-advanced-link', route: 'create-world', focus: 'settings-link' },
  { id: 'settings-basic-profiles-link', route: 'create-world', focus: 'settings-link' },
  { id: 'settings-basic-create-world', route: 'create-world', focus: 'settings-link' },
  { id: 'settings-advanced-setting', route: 'create-world', focus: 'advanced-setting' },
  { id: 'creation-profiles-name-entry', route: 'creation-profiles', focus: 'profile-name-entry', textEntry: { field: 'creation-profile-name', maximumLength: 32, characterPolicy: 'ascii-word-space-period-comma-apostrophe-hyphen' } },
  { id: 'creation-profiles-save-action', route: 'creation-profiles', focus: 'profile-save-action' },
  { id: 'creation-profiles-load-profile', route: 'creation-profiles', focus: 'profile-load-action' },
  { id: 'world-generation', route: 'world-generation', focus: 'result' },
  { id: 'world-result', route: 'world-result', focus: 'result' },
  { id: 'choose-courier', route: 'choose-courier', focus: 'list' },
  { id: 'world', route: 'world', focus: 'management' },
  { id: 'chronicles-list', route: 'chronicles', focus: 'list' },
  { id: 'chronicle', route: 'chronicle', focus: 'chronicle' },
  { id: 'future-contextual-prompt', route: 'world', focus: 'future-prompt' }
])

export interface TerminalKeyboardCommand {
  id: string
  availability: TerminalInputCommandAvailability
  surface: TerminalInputCommandSurface
  contexts: readonly TerminalInputContext[]
  bindings: readonly TerminalKeyboardBinding[]
  accessibilityLabel: string
}

const canonicalCommands = (commands: readonly TerminalKeyboardCommand[]): readonly TerminalKeyboardCommand[] => [...commands]
  .map(command => ({ ...command, contexts: [...command.contexts].sort((left, right) => left.localeCompare(right)), bindings: [...command.bindings].sort((left, right) => {
    const leftKey = left.kind === 'key' ? `key:${left.key}` : `text:${left.field}`
    const rightKey = right.kind === 'key' ? `key:${right.key}` : `text:${right.field}`
    return inputCompare(leftKey, rightKey)
  }) }))
  .sort((left, right) => left.id.localeCompare(right.id))

const key = (value: TerminalKeyboardKey, caseHandling: TerminalKeyBinding['caseHandling'] = 'exact'): TerminalKeyBinding => ({ kind: 'key', key: value, caseHandling })
const textEntry = (field: TerminalTextInputField, maximumLength: number): TerminalBoundedTextBinding => ({ kind: 'bounded-text-entry', field, characterPolicy: 'ascii-word-space-period-comma-apostrophe-hyphen', maximumLength, deletionKey: 'Backspace', caseHandling: 'preserve-typed-case' })

/** Implemented bindings mirror every current canvas key path; future controls remain explicitly reserved. */
export const TERMINAL_KEYBOARD_COMMANDS: readonly TerminalKeyboardCommand[] = canonicalCommands([
  { id: 'canvas-cancel-return', availability: 'implemented', surface: 'navigation', contexts: ['settings-advanced-setting', 'settings-basic-seed-entry', 'settings-basic-preset', 'settings-basic-advanced-link', 'settings-basic-profiles-link', 'settings-basic-create-world', 'creation-profiles-name-entry', 'creation-profiles-save-action', 'creation-profiles-load-profile', 'world-generation', 'world-result', 'choose-courier', 'world', 'chronicles-list', 'chronicle'], bindings: [key('Escape')], accessibilityLabel: 'Return to the preceding canvas view' },
  { id: 'canvas-confirm-selection', availability: 'implemented', surface: 'navigation', contexts: ['worlds-list', 'world-generation', 'world-result', 'choose-courier'], bindings: [key('Enter')], accessibilityLabel: 'Confirm the current canvas selection' },
  { id: 'canvas-navigate-next', availability: 'implemented', surface: 'navigation', contexts: ['worlds-list', 'settings-basic-seed-entry', 'settings-basic-preset', 'settings-basic-advanced-link', 'settings-basic-profiles-link', 'settings-basic-create-world', 'settings-advanced-setting', 'creation-profiles-name-entry', 'creation-profiles-save-action', 'creation-profiles-load-profile', 'choose-courier', 'chronicles-list'], bindings: [key('ArrowDown')], accessibilityLabel: 'Move to the next canvas row' },
  { id: 'canvas-navigate-previous', availability: 'implemented', surface: 'navigation', contexts: ['worlds-list', 'settings-basic-seed-entry', 'settings-basic-preset', 'settings-basic-advanced-link', 'settings-basic-profiles-link', 'settings-basic-create-world', 'settings-advanced-setting', 'creation-profiles-name-entry', 'creation-profiles-save-action', 'creation-profiles-load-profile', 'choose-courier', 'chronicles-list'], bindings: [key('ArrowUp')], accessibilityLabel: 'Move to the previous canvas row' },
  { id: 'chronicle-export', availability: 'implemented', surface: 'export', contexts: ['chronicle'], bindings: [key('E', 'ascii-case-insensitive')], accessibilityLabel: 'Export the open finalized chronicle' },
  { id: 'creation-profile-edit-name', availability: 'implemented', surface: 'text-entry', contexts: ['creation-profiles-name-entry'], bindings: [textEntry('creation-profile-name', 32)], accessibilityLabel: 'Enter or delete the bounded saved-profile name' },
  { id: 'creation-profile-load', availability: 'implemented', surface: 'settings', contexts: ['creation-profiles-load-profile'], bindings: [key('Enter')], accessibilityLabel: 'Load the selected local creation settings profile' },
  { id: 'creation-profile-save', availability: 'implemented', surface: 'settings', contexts: ['creation-profiles-save-action'], bindings: [key('Enter'), key('S', 'ascii-case-insensitive')], accessibilityLabel: 'Save the named local creation settings profile' },
  { id: 'management-next-section', availability: 'implemented', surface: 'management', contexts: ['world'], bindings: [key(']')], accessibilityLabel: 'Show the next management section when management is expanded' },
  { id: 'management-previous-section', availability: 'implemented', surface: 'management', contexts: ['world'], bindings: [key('[')], accessibilityLabel: 'Show the previous management section when management is expanded' },
  { id: 'management-toggle', availability: 'implemented', surface: 'management', contexts: ['world'], bindings: [key('M', 'ascii-case-insensitive')], accessibilityLabel: 'Expand or collapse the management sidebar' },
  { id: 'open-chronicles', availability: 'implemented', surface: 'navigation', contexts: ['worlds-list'], bindings: [key('C', 'ascii-case-insensitive')], accessibilityLabel: 'Open finalized chronicles' },
  { id: 'open-world-settings', availability: 'implemented', surface: 'navigation', contexts: ['worlds-list'], bindings: [key('N', 'ascii-case-insensitive')], accessibilityLabel: 'Open world settings' },
  { id: 'reserved-contextual-action', availability: 'reserved', surface: 'contextual-action', contexts: ['world'], bindings: [key('Enter')], accessibilityLabel: 'Reserved future contextual action; not implemented' },
  { id: 'reserved-help', availability: 'reserved', surface: 'help', contexts: ['world'], bindings: [key('?')], accessibilityLabel: 'Reserved future command help; not implemented' },
  { id: 'reserved-move-east', availability: 'reserved', surface: 'movement', contexts: ['world'], bindings: [key('L', 'ascii-case-insensitive')], accessibilityLabel: 'Reserved future move east; not implemented' },
  { id: 'reserved-move-north', availability: 'reserved', surface: 'movement', contexts: ['world'], bindings: [key('K', 'ascii-case-insensitive')], accessibilityLabel: 'Reserved future move north; not implemented' },
  { id: 'reserved-move-north-east', availability: 'reserved', surface: 'movement', contexts: ['world'], bindings: [key('U', 'ascii-case-insensitive')], accessibilityLabel: 'Reserved future move north east; not implemented' },
  { id: 'reserved-move-north-west', availability: 'reserved', surface: 'movement', contexts: ['world'], bindings: [key('Y', 'ascii-case-insensitive')], accessibilityLabel: 'Reserved future move north west; not implemented' },
  { id: 'reserved-move-south', availability: 'reserved', surface: 'movement', contexts: ['world'], bindings: [key('J', 'ascii-case-insensitive')], accessibilityLabel: 'Reserved future move south; not implemented' },
  { id: 'reserved-move-south-east', availability: 'reserved', surface: 'movement', contexts: ['world'], bindings: [key('N', 'ascii-case-insensitive')], accessibilityLabel: 'Reserved future move south east; not implemented' },
  { id: 'reserved-move-south-west', availability: 'reserved', surface: 'movement', contexts: ['world'], bindings: [key('B', 'ascii-case-insensitive')], accessibilityLabel: 'Reserved future move south west; not implemented' },
  { id: 'reserved-move-west', availability: 'reserved', surface: 'movement', contexts: ['world'], bindings: [key('H', 'ascii-case-insensitive')], accessibilityLabel: 'Reserved future move west; not implemented' },
  { id: 'reserved-prompt-cancel', availability: 'reserved', surface: 'contextual-action', contexts: ['future-contextual-prompt'], bindings: [key('Escape')], accessibilityLabel: 'Reserved future prompt cancellation; never advances world time' },
  { id: 'reserved-remap-controls', availability: 'reserved', surface: 'remapping', contexts: ['world'], bindings: [key('F2')], accessibilityLabel: 'Reserved future control remapping; not implemented' },
  { id: 'settings-create-world', availability: 'implemented', surface: 'settings', contexts: ['settings-basic-create-world'], bindings: [key('Enter')], accessibilityLabel: 'Create a world from the selected valid settings' },
  { id: 'settings-edit-seed', availability: 'implemented', surface: 'text-entry', contexts: ['settings-basic-seed-entry'], bindings: [textEntry('creation-seed', 64)], accessibilityLabel: 'Enter or delete the bounded creation seed' },
  { id: 'settings-open-advanced', availability: 'implemented', surface: 'settings', contexts: ['settings-basic-advanced-link'], bindings: [key('Enter')], accessibilityLabel: 'Open advanced generation settings' },
  { id: 'settings-open-profiles', availability: 'implemented', surface: 'settings', contexts: ['settings-basic-profiles-link'], bindings: [key('Enter')], accessibilityLabel: 'Open local creation settings profiles' },
  { id: 'settings-reset-advanced-value', availability: 'implemented', surface: 'settings', contexts: ['settings-advanced-setting'], bindings: [key('R', 'ascii-case-insensitive')], accessibilityLabel: 'Reset the selected advanced setting to its preset value' },
  { id: 'settings-select-next-preset', availability: 'implemented', surface: 'settings', contexts: ['settings-basic-preset'], bindings: [key('ArrowRight'), key('Enter')], accessibilityLabel: 'Select the next generation preset' },
  { id: 'settings-select-previous-preset', availability: 'implemented', surface: 'settings', contexts: ['settings-basic-preset'], bindings: [key('ArrowLeft')], accessibilityLabel: 'Select the previous generation preset' },
  { id: 'settings-advance-advanced-value', availability: 'implemented', surface: 'settings', contexts: ['settings-advanced-setting'], bindings: [key('ArrowRight'), key('Enter')], accessibilityLabel: 'Select the next value for the focused advanced setting' },
  { id: 'settings-reduce-advanced-value', availability: 'implemented', surface: 'settings', contexts: ['settings-advanced-setting'], bindings: [key('ArrowLeft')], accessibilityLabel: 'Select the previous value for the focused advanced setting' }
])

export interface TerminalInputContract {
  canvasFocus: { keyboardFirst: true; pointerFocusAssist: true }
  modes: readonly TerminalInputMode[]
  commands: readonly TerminalKeyboardCommand[]
}

export interface TerminalAccessibilityModel {
  accessibleName: 'Jomon terminal presentation'
  conciseSummary: string
  focusContext: 'keyboard-first-canvas'
  mapText: string
  statusText: readonly string[]
  messageText: readonly string[]
  promptText: readonly string[]
  commandText: readonly string[]
}

export interface TerminalSidebarBoundary {
  relationship: 'separate-household-known-strategic-surface'
  duplicatedStrategicFactCategories: readonly []
}

export interface TerminalRendererParity {
  asciiCanvas: 'current-adapter'
  detailedRenderer: 'future-adapter'
  requirements: readonly TerminalRendererParityRule[]
}

export interface TerminalPresentationModel {
  version: typeof TERMINAL_PRESENTATION_CONTRACT_VERSION
  worldId: string
  map: TerminalMapSurface
  status: readonly TerminalStatusItem[]
  messages: readonly TerminalMessage[]
  prompts: readonly TerminalPrompt[]
  input: TerminalInputContract
  accessibility: TerminalAccessibilityModel
  sidebarBoundary: TerminalSidebarBoundary
  rendererParity: TerminalRendererParity
  contentSafetyAudit: MedievalContentSafetyAudit
}

export type TerminalPresentationDiagnosticCode =
  | 'terminal-presentation.invalid-world'
  | 'terminal-presentation.malformed-model'
  | 'terminal-presentation.invalid-model'
  | 'terminal-presentation.invalid-viewport'
  | 'terminal-presentation.invalid-cell'
  | 'terminal-presentation.duplicate-cell'
  | 'terminal-presentation.noncanonical-cell-order'
  | 'terminal-presentation.reserved-glyph-reference'
  | 'terminal-presentation.invalid-glyph-reference'
  | 'terminal-presentation.unknown-palette-token'
  | 'terminal-presentation.invalid-non-color-cue'
  | 'terminal-presentation.invalid-evidence'
  | 'terminal-presentation.invalid-message'
  | 'terminal-presentation.invalid-prompt'
  | 'terminal-presentation.invalid-input-binding'
  | 'terminal-presentation.input-binding-conflict'
  | 'terminal-presentation.invalid-content-audit'
  | MedievalContentSafetyDiagnosticCode

export interface TerminalPresentationDiagnostic {
  recordId: string
  code: TerminalPresentationDiagnosticCode
}

export class TerminalPresentationContractError extends Error {
  constructor(readonly diagnostics: readonly TerminalPresentationDiagnostic[]) {
    super(`terminal presentation rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'TerminalPresentationContractError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const validId = (value: unknown, limit: number = TERMINAL_PRESENTATION_LIMITS.identityLength): value is string => typeof value === 'string' && value.length > 0 && value.length <= limit && /^[a-z][a-z0-9:._-]*$/iu.test(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const issue = (recordId: string, code: TerminalPresentationDiagnosticCode): TerminalPresentationDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly TerminalPresentationDiagnostic[]): readonly TerminalPresentationDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const validText = (value: unknown): value is string => typeof value === 'string' && value.length > 0 && value.length <= TERMINAL_PRESENTATION_LIMITS.textLength
const paletteToken = (value: unknown): value is JomonPaletteToken => typeof value === 'string' && Object.hasOwn(JOMON_PALETTE, value)
const cueKey = (value: unknown): value is TerminalNonColorCueKey => typeof value === 'string' && Object.hasOwn(JOMON_NON_COLOR_STATE_CUES, value)

const baseClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('player-facing-text', ['civil-life', 'navigation'], 'not-applicable', ['data'])

const validFreshness = (value: unknown, knownAtWorldTime: number): value is TerminalEvidenceFreshness => record(value)
  && ((hasOnlyKeys(value, ['kind']) && (value.kind === 'current' || value.kind === 'timeless'))
    || (hasOnlyKeys(value, ['kind', 'atWorldTime']) && value.kind === 'reported-at-world-time' && safeInteger(value.atWorldTime) && value.atWorldTime <= knownAtWorldTime))

export const validateTerminalEvidence = (value: unknown): readonly TerminalPresentationDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['source', 'recordedAtWorldTime', 'knownAtWorldTime', 'freshness'])) return [issue('terminal-evidence', 'terminal-presentation.invalid-evidence')]
  if (!record(value.source) || !hasOnlyKeys(value.source, ['kind', 'recordId']) || !oneOf(['presentation-contract', 'household-state', 'authoritative-record'] as const, value.source.kind) || !validId(value.source.recordId)) return [issue('terminal-evidence:source', 'terminal-presentation.invalid-evidence')]
  if (!safeInteger(value.recordedAtWorldTime) || !safeInteger(value.knownAtWorldTime) || value.recordedAtWorldTime > value.knownAtWorldTime || !validFreshness(value.freshness, value.knownAtWorldTime)) return [issue('terminal-evidence:time', 'terminal-presentation.invalid-evidence')]
  return []
}

const validCue = (value: unknown, state: TerminalPresentationState): value is TerminalNonColorCue => record(value)
  && hasOnlyKeys(value, ['key', 'text'])
  && cueKey(value.key)
  && value.key === TERMINAL_STATE_PRESENTATIONS[state].nonColorCueKey
  && value.text === JOMON_NON_COLOR_STATE_CUES[value.key]

export const validateTerminalMapViewport = (value: unknown): readonly TerminalPresentationDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['id', 'context', 'origin', 'width', 'height'])) return [issue('terminal-viewport', 'terminal-presentation.invalid-viewport')]
  if (!validId(value.id) || !oneOf(['reserved-unmaterialized', 'future-materialized'] as const, value.context) || !record(value.origin) || !hasOnlyKeys(value.origin, ['column', 'row']) || !safeInteger(value.origin.column) || !safeInteger(value.origin.row) || !safeInteger(value.width) || !safeInteger(value.height) || value.width < 1 || value.height < 1 || value.width > TERMINAL_PRESENTATION_LIMITS.viewportWidth || value.height > TERMINAL_PRESENTATION_LIMITS.viewportHeight || value.width * value.height > TERMINAL_PRESENTATION_LIMITS.viewportCells) return [issue('terminal-viewport', 'terminal-presentation.invalid-viewport')]
  return []
}

const cellOrder = (left: TerminalMaterializedCell, right: TerminalMaterializedCell): number => left.coordinate.row - right.coordinate.row
  || left.coordinate.column - right.coordinate.column
  || compare(left.id, right.id)

const glyphCatalogIssues = (value: unknown): readonly TerminalPresentationDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['vocabulary', 'version', 'glyphIds']) || value.vocabulary !== TERMINAL_GLYPH_VOCABULARY_ID || !safeInteger(value.version) || value.version < 1 || !Array.isArray(value.glyphIds)) return [issue('terminal-glyph-catalog', 'terminal-presentation.invalid-glyph-reference')]
  const glyphIds = value.glyphIds
  if (glyphIds.length === 0 || glyphIds.some(id => !validId(id, TERMINAL_PRESENTATION_LIMITS.glyphReferenceLength)) || glyphIds.some((id, index) => index > 0 && compare(glyphIds[index - 1] as string, id as string) >= 0)) return [issue('terminal-glyph-catalog', 'terminal-presentation.invalid-glyph-reference')]
  return []
}

/**
 * Validation seam for future materialized map data. No production model feeds
 * it cells today; the ASCII glyph catalogue supplies semantic references, not
 * present-world cells.
 */
export const validateTerminalMaterializedCells = (
  viewport: TerminalMapViewport,
  value: unknown,
  glyphCatalog: TerminalGlyphCatalog
): readonly TerminalPresentationDiagnostic[] => {
  const diagnostics: TerminalPresentationDiagnostic[] = [...validateTerminalMapViewport(viewport), ...glyphCatalogIssues(glyphCatalog)]
  if (viewport.context !== 'future-materialized') diagnostics.push(issue(viewport.id, 'terminal-presentation.invalid-viewport'))
  if (!Array.isArray(value) || value.length > TERMINAL_PRESENTATION_LIMITS.viewportCells) return canonicalDiagnostics([...diagnostics, issue('terminal-cells', 'terminal-presentation.invalid-cell')])
  const ids = new Set<string>()
  const coordinates = new Set<string>()
  const cells: TerminalMaterializedCell[] = []
  for (const candidate of value) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'terminal-cell'
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'coordinate', 'glyph', 'paletteToken', 'presentationState', 'nonColorCue', 'textEquivalent', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety'])) {
      diagnostics.push(issue(id, 'terminal-presentation.invalid-cell'))
      continue
    }
    if (!validId(candidate.id) || !record(candidate.coordinate) || !hasOnlyKeys(candidate.coordinate, ['column', 'row']) || !safeInteger(candidate.coordinate.column) || !safeInteger(candidate.coordinate.row) || candidate.coordinate.column < viewport.origin.column || candidate.coordinate.column >= viewport.origin.column + viewport.width || candidate.coordinate.row < viewport.origin.row || candidate.coordinate.row >= viewport.origin.row + viewport.height) diagnostics.push(issue(id, 'terminal-presentation.invalid-cell'))
    const cellId = typeof candidate.id === 'string' ? candidate.id : id
    if (ids.has(cellId)) diagnostics.push(issue(id, 'terminal-presentation.duplicate-cell'))
    ids.add(cellId)
    const coordinateId = record(candidate.coordinate) ? `${candidate.coordinate.column}:${candidate.coordinate.row}` : 'invalid'
    if (coordinates.has(coordinateId)) diagnostics.push(issue(id, 'terminal-presentation.duplicate-cell'))
    coordinates.add(coordinateId)
    if (!record(candidate.glyph) || !hasOnlyKeys(candidate.glyph, ['vocabulary', 'vocabularyVersion', 'id']) || candidate.glyph.vocabulary !== TERMINAL_GLYPH_VOCABULARY_ID || !safeInteger(candidate.glyph.vocabularyVersion) || candidate.glyph.vocabularyVersion !== glyphCatalog.version || !validId(candidate.glyph.id, TERMINAL_PRESENTATION_LIMITS.glyphReferenceLength)) {
      diagnostics.push(issue(id, 'terminal-presentation.invalid-glyph-reference'))
    } else if (candidate.glyph.id.startsWith('reserved:')) {
      diagnostics.push(issue(id, 'terminal-presentation.reserved-glyph-reference'))
    } else if (!glyphCatalog.glyphIds.includes(candidate.glyph.id)) {
      diagnostics.push(issue(id, 'terminal-presentation.invalid-glyph-reference'))
    }
    if (!paletteToken(candidate.paletteToken)) diagnostics.push(issue(id, 'terminal-presentation.unknown-palette-token'))
    if (!oneOf(TERMINAL_PRESENTATION_STATES, candidate.presentationState) || !validCue(candidate.nonColorCue, candidate.presentationState)) diagnostics.push(issue(id, 'terminal-presentation.invalid-non-color-cue'))
    if (!validText(candidate.textEquivalent) || !validText(candidate.accessibilityText)) diagnostics.push(issue(id, 'terminal-presentation.invalid-cell'))
    diagnostics.push(...validateTerminalEvidence(candidate.evidence).map(item => ({ ...item, recordId: id })))
    cells.push(candidate as unknown as TerminalMaterializedCell)
  }
  if (cells.some((cell, index) => index > 0 && cellOrder(cells[index - 1]!, cell) >= 0)) diagnostics.push(issue('terminal-cells', 'terminal-presentation.noncanonical-cell-order'))
  const content = auditMedievalContentSafety(cells.map(cell => ({ id: `terminal-cell:${cell.id}`, domain: cell.contentDomain, classification: cell.contentSafety })))
  if (content.status === 'rejected') diagnostics.push(...content.diagnostics.map(item => issue(item.contentId, item.code)))
  return canonicalDiagnostics(diagnostics)
}

const statusText = (item: TerminalStatusItem): string => {
  if (item.value.kind === 'map-reserved') return 'Map reserved; no spatial cells are materialized.'
  if (item.value.kind === 'courier-selection') return item.value.selected ? 'An active courier is selected.' : 'Choose an initial courier before active play.'
  return `Current world minute ${item.value.minutes}.`
}

const statusItem = (
  id: string,
  state: TerminalPresentationState,
  value: TerminalStatusValue,
  evidence: TerminalEvidenceProvenance
): TerminalStatusItem => ({
  id,
  state,
  paletteToken: TERMINAL_STATE_PRESENTATIONS[state].paletteToken,
  nonColorCue: terminalNonColorCueFor(state),
  value,
  accessibilityText: `${statusText({ value } as TerminalStatusItem)} State ${state}; cue ${terminalNonColorCueFor(state).text}.`,
  evidence,
  contentDomain: 'player-facing-text',
  contentSafety: baseClassification()
})

const presentationEvidence = (recordId: string): TerminalEvidenceProvenance => ({
  source: { kind: 'presentation-contract', recordId },
  recordedAtWorldTime: 0,
  knownAtWorldTime: 0,
  freshness: { kind: 'timeless' }
})

const currentWorldEvidence = (recordId: string, worldTime: number): TerminalEvidenceProvenance => ({
  source: { kind: 'household-state', recordId },
  recordedAtWorldTime: worldTime,
  knownAtWorldTime: worldTime,
  freshness: { kind: 'current' }
})

const currentReservedMap = (world: FoundationWorld): TerminalReservedMap => ({
  state: 'reserved-unmaterialized',
  viewport: { id: `terminal-viewport:${world.id}:reserved`, context: 'reserved-unmaterialized', origin: { column: 0, row: 0 }, width: 64, height: 24 },
  cells: [],
  textEquivalent: 'Reserved map area. No spatial cells are materialized.',
  accessibilityText: 'Reserved map viewport. No terrain, deck, actor, route, site, or hidden-world cells are materialized.',
  evidence: presentationEvidence('terminal-presentation:reserved-map'),
  contentDomain: 'player-facing-text',
  contentSafety: baseClassification()
})

const orderedStatus = (world: FoundationWorld): readonly TerminalStatusItem[] => canonicalById([
  statusItem('terminal-status:courier', world.state.courier.initialCourierId ? 'ready' : 'waiting', { kind: 'courier-selection', selected: Boolean(world.state.courier.initialCourierId) }, currentWorldEvidence('world-state:courier', world.state.temporal.worldTime)),
  statusItem('terminal-status:map', 'neutral', { kind: 'map-reserved' }, presentationEvidence('terminal-presentation:reserved-map-status')),
  statusItem('terminal-status:time', 'neutral', { kind: 'world-minute', minutes: world.state.temporal.worldTime }, currentWorldEvidence('world-state:temporal', world.state.temporal.worldTime))
])

const canonicalById = <Value extends { id: string }>(values: readonly Value[]): readonly Value[] => [...values].sort((left, right) => compare(left.id, right.id))

const modelContentRecords = (map: TerminalMapSurface, status: readonly TerminalStatusItem[], messages: readonly TerminalMessage[], prompts: readonly TerminalPrompt[]): readonly ClassifiedMedievalContent[] => [
  { id: 'terminal-presentation:map', domain: map.contentDomain, classification: map.contentSafety },
  ...status.map(item => ({ id: `terminal-presentation:status:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...messages.map(item => ({ id: `terminal-presentation:message:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...prompts.map(item => ({ id: `terminal-presentation:prompt:${item.id}`, domain: item.contentDomain, classification: item.contentSafety }))
]

const accessibilityFor = (map: TerminalMapSurface, status: readonly TerminalStatusItem[], messages: readonly TerminalMessage[], prompts: readonly TerminalPrompt[]): TerminalAccessibilityModel => ({
  accessibleName: 'Jomon terminal presentation',
  conciseSummary: `Reserved unmaterialized map. ${status.length} immediate local status entries. ${messages.length} authoritative messages. ${prompts.length} contextual prompts.`,
  focusContext: 'keyboard-first-canvas',
  mapText: map.accessibilityText,
  statusText: status.map(item => item.accessibilityText),
  messageText: messages.length ? messages.map(item => item.accessibilityText) : ['No current authoritative messages.'],
  promptText: prompts.length ? prompts.map(item => item.accessibilityText) : ['No current contextual prompt.'],
  commandText: TERMINAL_KEYBOARD_COMMANDS.map(command => command.accessibilityLabel)
})

const rawTerminalPresentationModel = (world: FoundationWorld): TerminalPresentationModel => {
  const map = currentReservedMap(world)
  const status = orderedStatus(world)
  const messages: readonly TerminalMessage[] = []
  const prompts: readonly TerminalPrompt[] = []
  const contentSafetyAudit = auditMedievalContentSafety(modelContentRecords(map, status, messages, prompts))
  if (contentSafetyAudit.status === 'rejected') throw new TerminalPresentationContractError(contentSafetyAudit.diagnostics.map(item => issue(item.contentId, item.code)))
  return {
    version: TERMINAL_PRESENTATION_CONTRACT_VERSION,
    worldId: world.id,
    map,
    status,
    messages,
    prompts,
    input: { canvasFocus: { keyboardFirst: true, pointerFocusAssist: true }, modes: TERMINAL_INPUT_MODES, commands: TERMINAL_KEYBOARD_COMMANDS },
    accessibility: accessibilityFor(map, status, messages, prompts),
    sidebarBoundary: { relationship: 'separate-household-known-strategic-surface', duplicatedStrategicFactCategories: [] },
    rendererParity: { asciiCanvas: 'current-adapter', detailedRenderer: 'future-adapter', requirements: TERMINAL_RENDERER_PARITY_RULES },
    contentSafetyAudit
  }
}

const ensureValidWorld = (world: FoundationWorld): void => {
  const diagnostics = validateFoundationWorld(world)
  if (diagnostics.length) throw new TerminalPresentationContractError(diagnostics.map(item => issue(item.recordId, 'terminal-presentation.invalid-world')))
}

/** Pure selected-world projection. It exposes no initial-world or frontier detail. */
export const createTerminalPresentationModel = (world: FoundationWorld): TerminalPresentationModel => {
  ensureValidWorld(world)
  return rawTerminalPresentationModel(world)
}

/** Validates future message routing independently; the current projection has no messages. */
export const validateTerminalMessages = (value: unknown): readonly TerminalPresentationDiagnostic[] => {
  if (!Array.isArray(value) || value.length > TERMINAL_PRESENTATION_LIMITS.messages) return [issue('terminal-messages', 'terminal-presentation.invalid-message')]
  const diagnostics: TerminalPresentationDiagnostic[] = []
  const ids = new Set<string>()
  for (const candidate of value) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'terminal-message'
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'kind', 'state', 'paletteToken', 'nonColorCue', 'sourceRecordKind', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety']) || !validId(candidate.id) || !oneOf(TERMINAL_MESSAGE_KINDS, candidate.kind) || !oneOf(TERMINAL_PRESENTATION_STATES, candidate.state) || !paletteToken(candidate.paletteToken) || candidate.paletteToken !== TERMINAL_STATE_PRESENTATIONS[candidate.state].paletteToken || !validCue(candidate.nonColorCue, candidate.state) || !oneOf(['causal-command', 'delegation-record', 'social-memory-record', 'future-authoritative-record'] as const, candidate.sourceRecordKind) || !validText(candidate.accessibilityText)) diagnostics.push(issue(id, 'terminal-presentation.invalid-message'))
    if (ids.has(id)) diagnostics.push(issue(id, 'terminal-presentation.invalid-message'))
    ids.add(id)
    const evidenceIssues = validateTerminalEvidence(candidate.evidence)
    if (evidenceIssues.length || !record(candidate.evidence) || !record(candidate.evidence.source) || candidate.evidence.source.kind !== 'authoritative-record') diagnostics.push(issue(id, 'terminal-presentation.invalid-message'))
    else diagnostics.push(...evidenceIssues.map(item => ({ ...item, recordId: id })))
  }
  const messages = value.filter(record) as unknown as TerminalMessage[]
  if (messages.some((item, index) => index > 0 && compare(messages[index - 1]!.id, item.id) >= 0)) diagnostics.push(issue('terminal-messages', 'terminal-presentation.invalid-message'))
  const content = auditMedievalContentSafety(messages.map(item => ({ id: `terminal-message:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })))
  if (content.status === 'rejected') diagnostics.push(...content.diagnostics.map(item => issue(item.contentId, item.code)))
  return canonicalDiagnostics(diagnostics)
}

const validPromptOption = (value: unknown): value is TerminalPromptOption => record(value)
  && hasOnlyKeys(value, ['id', 'key', 'availability', 'disabledReason', 'intent', 'requiresConfirmation', 'nonColorCue', 'accessibilityText'].filter(key => key !== 'disabledReason' || Object.hasOwn(value, key)))
  && validId(value.id)
  && oneOf(TERMINAL_KEYBOARD_KEYS, value.key)
  && (value.availability === 'available' || value.availability === 'disabled')
  && value.intent === 'future-contextual-action'
  && typeof value.requiresConfirmation === 'boolean'
  && validCue(value.nonColorCue, 'neutral')
  && validText(value.accessibilityText)
  && (value.availability === 'disabled' ? oneOf(TERMINAL_PROMPT_DISABLED_REASONS, value.disabledReason) : value.disabledReason === undefined)

export const validateTerminalPrompt = (value: unknown): readonly TerminalPresentationDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['id', 'kind', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety', 'options', 'cancellation']) || !validId(value.id) || value.kind !== 'future-contextual-choice' || !validText(value.accessibilityText) || !Array.isArray(value.options) || value.options.length > TERMINAL_PRESENTATION_LIMITS.promptOptions || !value.options.every(validPromptOption) || !record(value.cancellation) || !hasOnlyKeys(value.cancellation, ['key', 'outcome', 'advancesWorldTime']) || value.cancellation.key !== 'Escape' || value.cancellation.outcome !== 'cancelled-no-mutation' || value.cancellation.advancesWorldTime !== false) return [issue('terminal-prompt', 'terminal-presentation.invalid-prompt')]
  const evidence = validateTerminalEvidence(value.evidence)
  if (evidence.length || !record(value.evidence) || !record(value.evidence.source) || value.evidence.source.kind !== 'authoritative-record') return [issue(value.id, 'terminal-presentation.invalid-prompt')]
  const options = value.options as TerminalPromptOption[]
  const optionIds = new Set<string>()
  const keys = new Set<string>()
  const diagnostics: TerminalPresentationDiagnostic[] = []
  for (const option of options) {
    if (optionIds.has(option.id) || keys.has(option.key)) diagnostics.push(issue(option.id, 'terminal-presentation.invalid-prompt'))
    optionIds.add(option.id)
    keys.add(option.key)
  }
  if (options.some((option, index) => index > 0 && compare(options[index - 1]!.id, option.id) >= 0)) diagnostics.push(issue(value.id, 'terminal-presentation.invalid-prompt'))
  const content = auditMedievalContentSafety([{ id: `terminal-prompt:${value.id}`, domain: value.contentDomain as MedievalContentDomain, classification: value.contentSafety as MedievalContentSafetyClassification }])
  if (content.status === 'rejected') diagnostics.push(...content.diagnostics.map(item => issue(item.contentId, item.code)))
  return canonicalDiagnostics(diagnostics)
}

/** Cancellation is an explicit renderer intent, never a world transition. */
export const cancelTerminalPrompt = (prompt: TerminalPrompt): TerminalPromptCancellation => {
  const diagnostics = validateTerminalPrompt(prompt)
  if (diagnostics.length) throw new TerminalPresentationContractError(diagnostics)
  return { id: `terminal-prompt-cancel:${prompt.id}`, promptId: prompt.id, outcome: 'cancelled-no-mutation', advancesWorldTime: false }
}

const terminalRoutes = ['worlds', 'create-world', 'creation-profiles', 'world-generation', 'world-result', 'choose-courier', 'world', 'chronicles', 'chronicle'] as const satisfies readonly MedievalRoute[]
const terminalInputFocuses = ['list', 'seed-entry', 'preset', 'settings-link', 'advanced-setting', 'profile-name-entry', 'profile-save-action', 'profile-load-action', 'result', 'management', 'chronicle', 'future-prompt'] as const satisfies readonly TerminalInputMode['focus'][]
const terminalInputSurfaces = ['navigation', 'management', 'settings', 'text-entry', 'export', 'movement', 'contextual-action', 'remapping', 'help'] as const satisfies readonly TerminalInputCommandSurface[]

const textEntrySpecification = (field: TerminalTextInputField): TerminalBoundedTextBinding => field === 'creation-seed'
  ? textEntry('creation-seed', 64)
  : textEntry('creation-profile-name', 32)
const bindingIdentity = (binding: TerminalKeyboardBinding): string => binding.kind === 'key' ? `key:${binding.key}` : `text:${binding.field}`
const validTextEntryBinding = (value: unknown): value is TerminalBoundedTextBinding => record(value)
  && hasOnlyKeys(value, ['kind', 'field', 'characterPolicy', 'maximumLength', 'deletionKey', 'caseHandling'])
  && value.kind === 'bounded-text-entry'
  && (value.field === 'creation-seed' || value.field === 'creation-profile-name')
  && value.characterPolicy === 'ascii-word-space-period-comma-apostrophe-hyphen'
  && same(value, textEntrySpecification(value.field))
const validKeyBinding = (value: unknown): value is TerminalKeyBinding => record(value)
  && hasOnlyKeys(value, ['kind', 'key', 'caseHandling'])
  && value.kind === 'key'
  && oneOf(TERMINAL_KEYBOARD_KEYS, value.key)
  && (value.caseHandling === 'exact' || value.caseHandling === 'ascii-case-insensitive')
const validKeyboardBinding = (value: unknown): value is TerminalKeyboardBinding => validKeyBinding(value) || validTextEntryBinding(value)

/** Ensures focused input modes remain an exact, bounded description of the canvas adapter. */
export const validateTerminalInputModes = (value: unknown): readonly TerminalPresentationDiagnostic[] => {
  if (!Array.isArray(value) || value.length !== TERMINAL_INPUT_CONTEXTS.length || value.length > TERMINAL_PRESENTATION_LIMITS.inputModes) return [issue('terminal-input-modes', 'terminal-presentation.invalid-input-binding')]
  const diagnostics: TerminalPresentationDiagnostic[] = []
  const ids = new Set<string>()
  for (const candidate of value) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'terminal-input-mode'
    const expectedText = id === 'settings-basic-seed-entry'
      ? { field: 'creation-seed' as const, maximumLength: 64 }
      : id === 'creation-profiles-name-entry'
        ? { field: 'creation-profile-name' as const, maximumLength: 32 }
        : undefined
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'route', 'focus', 'textEntry'].filter(keyName => keyName !== 'textEntry' || Object.hasOwn(candidate, keyName))) || !oneOf(TERMINAL_INPUT_CONTEXTS, candidate.id) || !oneOf(terminalRoutes, candidate.route) || !oneOf(terminalInputFocuses, candidate.focus)) {
      diagnostics.push(issue(id, 'terminal-presentation.invalid-input-binding'))
      continue
    }
    if (ids.has(candidate.id)) diagnostics.push(issue(id, 'terminal-presentation.invalid-input-binding'))
    ids.add(candidate.id)
    if (expectedText === undefined ? candidate.textEntry !== undefined : !record(candidate.textEntry) || !hasOnlyKeys(candidate.textEntry, ['field', 'maximumLength', 'characterPolicy']) || candidate.textEntry.field !== expectedText.field || candidate.textEntry.maximumLength !== expectedText.maximumLength || candidate.textEntry.characterPolicy !== 'ascii-word-space-period-comma-apostrophe-hyphen') diagnostics.push(issue(id, 'terminal-presentation.invalid-input-binding'))
  }
  const modes = value.filter(record) as unknown as TerminalInputMode[]
  if (modes.some((mode, index) => index > 0 && compare(modes[index - 1]!.id, mode.id) >= 0) || !same(modes.map(mode => mode.id), [...TERMINAL_INPUT_CONTEXTS].sort(compare))) diagnostics.push(issue('terminal-input-modes', 'terminal-presentation.invalid-input-binding'))
  return canonicalDiagnostics(diagnostics)
}

export const validateTerminalKeyboardCommands = (value: unknown): readonly TerminalPresentationDiagnostic[] => {
  if (!Array.isArray(value) || value.length > TERMINAL_PRESENTATION_LIMITS.inputCommands) return [issue('terminal-input', 'terminal-presentation.invalid-input-binding')]
  const diagnostics: TerminalPresentationDiagnostic[] = []
  const ids = new Set<string>()
  const contextKeys = new Set<string>()
  const commands: TerminalKeyboardCommand[] = []
  for (const candidate of value) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'terminal-input-command'
    const contexts = record(candidate) && Array.isArray(candidate.contexts) ? candidate.contexts : []
    const bindings = record(candidate) && Array.isArray(candidate.bindings) ? candidate.bindings : []
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'availability', 'surface', 'contexts', 'bindings', 'accessibilityLabel']) || !validId(candidate.id) || !oneOf(['implemented', 'reserved'] as const, candidate.availability) || !oneOf(terminalInputSurfaces, candidate.surface) || !Array.isArray(candidate.contexts) || contexts.length === 0 || !contexts.every(context => oneOf(TERMINAL_INPUT_CONTEXTS, context)) || contexts.some((context, index) => index > 0 && compare(contexts[index - 1] as string, context as string) >= 0) || !Array.isArray(candidate.bindings) || bindings.length === 0 || bindings.length > 3 || !bindings.every(validKeyboardBinding) || bindings.some((binding, index) => index > 0 && compare(bindingIdentity(bindings[index - 1] as TerminalKeyboardBinding), bindingIdentity(binding as TerminalKeyboardBinding)) >= 0) || !validText(candidate.accessibilityLabel)) {
      diagnostics.push(issue(id, 'terminal-presentation.invalid-input-binding'))
      continue
    }
    const textBindings = bindings.filter(binding => record(binding) && binding.kind === 'bounded-text-entry') as TerminalBoundedTextBinding[]
    if (textBindings.length > 0 && (bindings.length !== 1 || contexts.length !== 1 || (textBindings[0]!.field === 'creation-seed' ? contexts[0] !== 'settings-basic-seed-entry' : contexts[0] !== 'creation-profiles-name-entry'))) diagnostics.push(issue(id, 'terminal-presentation.invalid-input-binding'))
    if (ids.has(candidate.id)) diagnostics.push(issue(id, 'terminal-presentation.invalid-input-binding'))
    ids.add(candidate.id)
    for (const context of contexts as TerminalInputContext[]) {
      for (const binding of bindings as TerminalKeyboardBinding[]) {
        if (binding.kind !== 'key') continue
        const keyId = `${context}\u0000${binding.key}`
        if (contextKeys.has(keyId)) diagnostics.push(issue(candidate.id, 'terminal-presentation.input-binding-conflict'))
        contextKeys.add(keyId)
      }
    }
    commands.push(candidate as unknown as TerminalKeyboardCommand)
  }
  if (commands.some((command, index) => index > 0 && compare(commands[index - 1]!.id, command.id) >= 0)) diagnostics.push(issue('terminal-input', 'terminal-presentation.invalid-input-binding'))
  return canonicalDiagnostics(diagnostics)
}

export const validateTerminalPresentationModel = (world: FoundationWorld, value: unknown): readonly TerminalPresentationDiagnostic[] => {
  try {
    ensureValidWorld(world)
    const expected = rawTerminalPresentationModel(world)
    if (!same(value, expected)) return [issue('terminal-presentation:model', 'terminal-presentation.invalid-model')]
    return []
  } catch (error) {
    if (error instanceof TerminalPresentationContractError) return error.diagnostics
    return [issue('terminal-presentation:model', 'terminal-presentation.malformed-model')]
  }
}

/** A type-level reminder that current route metadata is renderer input, not world authority. */
export type TerminalPresentationRoute = Extract<MedievalRoute, 'world' | 'worlds' | 'create-world' | 'world-result' | 'choose-courier'>
