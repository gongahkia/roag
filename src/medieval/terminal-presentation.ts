import { auditMedievalContentSafety, classifyMedievalContent, contentSafetyAuditMatches, type ClassifiedMedievalContent, type MedievalContentDomain, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { JOMON_ASCII_GLYPH_CATALOG, terminalGlyphCatalog, terminalGlyphReferenceFor } from './ascii-glyphs'
import { INITIAL_HOUSEHOLD_EQUIPMENT_BY_ROLE, INITIAL_HOUSEHOLD_ROSTER_SIZE, initialHouseholdActiveCrew } from './initial-household'
import { deriveJomonDeckPlan, deriveJomonDeckPlanForVerifiedWorld } from './jomon-deck-plan'
import { assessTavernCourierSwitchForVerifiedWorld, type TavernCourierSwitchSource } from './tavern-courier-switch'
import { assessVesselProximityOperationsForVerifiedWorld, type VesselProximityOperation, type VesselProximityOperationReason, type VesselProximityOperationSource } from './vessel-proximity-operation'
import { createVesselStationReadoutForVerifiedWorld, validateVesselStationReadout, type VesselStationReadout } from './vessel-station-readout'
import { vesselPropActionFeedbacks, type VesselPropActionFeedback, type VesselPropActionKind } from './vessel-prop-action'
import type { CourierLossResolvedCommand } from './causal-history'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE, type JomonPaletteToken } from './palette'
import {
  TERMINAL_GLYPH_VOCABULARY_ID,
  TERMINAL_PRESENTATION_STATES,
  TERMINAL_STATE_PRESENTATIONS,
  terminalNonColorCueFor,
  type TerminalGlyphCatalog,
  type TerminalGlyphReference,
  type TerminalNonColorCue,
  type TerminalNonColorCueKey,
  type TerminalPresentationState
} from './terminal-semantics'
import type { FoundationWorld, MedievalRoute } from './types'
import { validateFoundationWorld } from './world'

export {
  TERMINAL_GLYPH_VOCABULARY_ID,
  TERMINAL_PRESENTATION_STATES,
  TERMINAL_STATE_PRESENTATIONS,
  terminalNonColorCueFor
} from './terminal-semantics'
export type {
  TerminalGlyphCatalog,
  TerminalGlyphReference,
  TerminalNonColorCue,
  TerminalNonColorCueKey,
  TerminalPresentationState,
  TerminalStatePresentation
} from './terminal-semantics'

/**
 * Renderer-neutral terminal information contract. The current world projects
 * its validated static Jomon deck into source-backed ASCII cells. ASCII and a
 * future detailed adapter consume that same projection without omitting or
 * inventing consequential information.
 */
/** v12 adds persisted bounded vessel-prop action feedback to status/messages. */
export const TERMINAL_PRESENTATION_CONTRACT_VERSION = 12 as const
export const TERMINAL_MAP_LEGEND_CONTRACT_VERSION = 1 as const

export const TERMINAL_PRESENTATION_LIMITS = {
  viewportWidth: 120,
  viewportHeight: 80,
  viewportCells: 9_600,
  statusItems: 6,
  messages: 12,
  prompts: 1,
  promptOptions: 8,
  legendEntries: 64,
  inputCommands: 64,
  inputModes: 24,
  identityLength: 160,
  glyphReferenceLength: 96,
  textLength: 1_200
} as const

export const TERMINAL_RENDERER_PARITY_RULES = [
  'same-authoritative-terminal-model',
  'no-consequential-omission',
  'no-consequential-invention',
  'text-equivalent-required',
  'legend-help-information-required'
] as const
export type TerminalRendererParityRule = typeof TERMINAL_RENDERER_PARITY_RULES[number]

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

export type TerminalMapViewportContext = 'jomon-deck-plan' | 'future-materialized'

export interface TerminalMapViewport {
  id: string
  context: TerminalMapViewportContext
  origin: TerminalCellCoordinate
  width: number
  height: number
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

export interface TerminalMaterializedMap {
  state: 'materialized'
  viewport: TerminalMapViewport
  /** The whole bounded deck stays visible; this has no panning or visibility authority. */
  camera: {
    mode: 'fixed-full-deck'
    focus: { courierId?: string; coordinate?: TerminalCellCoordinate }
    visibility: 'all-static-deck-known'
  }
  cells: readonly TerminalMaterializedCell[]
  textEquivalent: string
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export type TerminalMapSurface = TerminalMaterializedMap

/**
 * A compact legend is derived only from the already materialized, source-backed
 * map cells and their closed glyph references. It adds no map geometry or
 * world fact, and remains a zero-time presentation surface.
 */
export interface TerminalMapLegendEntry {
  id: string
  glyph: TerminalGlyphReference
  label: string
  paletteToken: JomonPaletteToken
  presentationState: TerminalPresentationState
  nonColorCue: TerminalNonColorCue
  textEquivalent: string
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export interface TerminalMapLegend {
  version: typeof TERMINAL_MAP_LEGEND_CONTRACT_VERSION
  entries: readonly TerminalMapLegendEntry[]
  movementText: string
  limitationsText: string
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export type TerminalStatusValue =
  | { kind: 'jomon-deck-materialized'; cells: number }
  | { kind: 'courier-selection'; state: 'awaiting-initial' | 'active' | 'crew-extinct' }
  | { kind: 'world-minute'; minutes: number }
  | { kind: 'deck-focus'; courierId?: string; coordinate?: TerminalCellCoordinate }
  | { kind: 'creation-provenance'; seed: string; digest: string }
  | { kind: 'vessel-prop-action'; propId: string; action: VesselPropActionKind; recordedAtWorldTime: number; causalSequence: number }

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

export const TERMINAL_MESSAGE_KINDS = ['authoritative-arrival', 'authoritative-attention', 'vessel-prop-action'] as const
export type TerminalMessageKind = typeof TERMINAL_MESSAGE_KINDS[number]

/** Future messages may route only a real known source record; none exist today. */
export interface TerminalMessage {
  id: string
  kind: TerminalMessageKind
  state: TerminalPresentationState
  paletteToken: JomonPaletteToken
  nonColorCue: TerminalNonColorCue
  sourceRecordKind: 'causal-command' | 'delegation-record' | 'social-memory-record' | 'future-authoritative-record' | 'vessel-prop-action'
  /** Present only for the current bounded vessel-prop action message. */
  value?: { propId: string; action: VesselPropActionKind; recordedAtWorldTime: number; causalSequence: number }
  /** Present only for the current bounded vessel-prop action message. */
  text?: string
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export const TERMINAL_PROMPT_DISABLED_REASONS = [
  'no-contextual-action-materialized',
  'missing-required-location',
  'requires-future-domain-rule',
  'inspection-readout-only',
  'not-at-tavern-ledger',
  'no-alternate-switchable-courier'
] as const
export type TerminalPromptDisabledReason = typeof TERMINAL_PROMPT_DISABLED_REASONS[number]

export interface TerminalPromptOption {
  id: string
  key: TerminalKeyboardKey
  availability: 'available' | 'disabled'
  disabledReason?: TerminalPromptDisabledReason
  intent: 'future-contextual-action' | 'tavern-courier-switch' | 'vessel-station-readout-record'
  requiresConfirmation: boolean
  nonColorCue: TerminalNonColorCue
  accessibilityText: string
}

/** Prompt options express intent only. They have no reducer or time authority. */
export interface TerminalFuturePrompt {
  id: string
  kind: 'future-contextual-choice'
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
  options: readonly TerminalPromptOption[]
  cancellation: { key: 'Escape'; outcome: 'cancelled-no-mutation'; advancesWorldTime: false }
}

/** A physical source and operation fact with no coordinate, map, or world-state copy. */
export interface TerminalVesselPromptOperation {
  proximity: 'at-anchor'
  availability: 'implemented' | 'readout'
  reason?: Extract<VesselProximityOperationReason, 'route-comparison-not-implemented' | 'quay-travel-not-implemented'>
}

/**
 * A compact exact-anchor station surface. The source readout remains owned by
 * the renderer-neutral station contract; terminal adds prompt evidence only.
 */
export interface TerminalVesselStationReadoutPrompt {
  id: string
  kind: 'vessel-station-readout'
  label: VesselStationReadout['label']
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
  source: VesselProximityOperationSource
  operation: TerminalVesselPromptOperation
  readout: VesselStationReadout
  options: readonly TerminalPromptOption[]
  cancellation: { key: 'Escape'; outcome: 'cancelled-no-mutation'; advancesWorldTime: false }
}

/** Deliberately bounded selection data copied from the immutable household view. */
export interface TerminalCourierSwitchCandidate {
  id: string
  name: string
  role: string
  conversation: 1 | 2 | 3 | 4 | 5
}

export const TERMINAL_TAVERN_LEDGER_MEMBER_STATUSES = [
  'active-available',
  'active-committed',
  'active-temporarily-unavailable',
  'available',
  'committed',
  'temporarily-unavailable',
  'departed',
  'dead'
] as const
export type TerminalTavernLedgerMemberStatus = typeof TERMINAL_TAVERN_LEDGER_MEMBER_STATUSES[number]

/**
 * A member fact deliberately contains only canonical household selection data
 * plus a bounded current status. It is not a persistent-person copy.
 */
export interface TerminalTavernLedgerMember extends TerminalCourierSwitchCandidate {
  status: TerminalTavernLedgerMemberStatus
  paletteToken: JomonPaletteToken
  presentationState: TerminalPresentationState
  nonColorCue: TerminalNonColorCue
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: 'person'
  contentSafety: MedievalContentSafetyClassification
}

/** A direct retained causal command may establish this concise consequence. */
export interface TerminalTavernLedgerContinuity {
  kind: 'continued-after-recorded-loss'
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: 'event'
  contentSafety: MedievalContentSafetyClassification
}

/**
 * The ledger is shown only at its physical anchor. It has no reducer, location,
 * persistence, or roster authority; the prompt remains its presentation owner.
 */
export interface TerminalTavernLedgerReadout {
  id: 'terminal-ledger:task-ledger'
  members: readonly TerminalTavernLedgerMember[]
  continuity?: TerminalTavernLedgerContinuity
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: 'player-facing-text'
  contentSafety: MedievalContentSafetyClassification
}

/** The presentation-only counterpart of the source-backed tavern operation. */
export interface TerminalTavernCourierSwitchPrompt {
  id: string
  kind: 'tavern-courier-switch'
  accessibilityText: string
  evidence: TerminalEvidenceProvenance
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
  source: TavernCourierSwitchSource
  operation: TerminalVesselPromptOperation
  current: TerminalCourierSwitchCandidate
  candidates: readonly TerminalCourierSwitchCandidate[]
  /** Present only at the verified physical task-ledger anchor. */
  ledger?: TerminalTavernLedgerReadout
  options: readonly TerminalPromptOption[]
  cancellation: { key: 'Escape'; outcome: 'cancelled-no-mutation'; advancesWorldTime: false }
}

export type TerminalPrompt = TerminalFuturePrompt | TerminalVesselStationReadoutPrompt | TerminalTavernCourierSwitchPrompt

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
  'world-contextual-prompt',
  'world-command-help',
  'world-controls-editor',
  'world-controls-capture',
  'chronicles-list',
  'chronicle',
  'future-contextual-prompt'
] as const
export type TerminalInputContext = typeof TERMINAL_INPUT_CONTEXTS[number]

export const TERMINAL_KEYBOARD_KEYS = [
  'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter', 'Escape', 'Backspace',
  'A', 'N', 'C', 'E', 'R', 'S', 'M', '[', ']', '?', 'F2', 'Y', 'U', 'H', 'J', 'K', 'L', 'B'
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

/** The controls editor accepts one closed, validated key without binding Escape/F2. */
export interface TerminalKeyCaptureBinding {
  kind: 'single-key-capture'
  maximumKeys: 1
  cancellationKey: 'Escape'
  disallowsModifierChords: true
}

export type TerminalKeyboardBinding = TerminalKeyBinding | TerminalBoundedTextBinding | TerminalKeyCaptureBinding

export interface TerminalInputMode {
  id: TerminalInputContext
  route: MedievalRoute
  focus: 'list' | 'seed-entry' | 'preset' | 'settings-link' | 'advanced-setting' | 'profile-name-entry' | 'profile-save-action' | 'profile-load-action' | 'result' | 'management' | 'contextual-prompt' | 'command-help' | 'controls-editor' | 'controls-capture' | 'chronicle' | 'future-prompt'
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
  { id: 'world-contextual-prompt', route: 'world', focus: 'contextual-prompt' },
  { id: 'world-command-help', route: 'world', focus: 'command-help' },
  { id: 'world-controls-editor', route: 'world', focus: 'controls-editor' },
  { id: 'world-controls-capture', route: 'world', focus: 'controls-capture' },
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
  .map(command => ({ ...command, contexts: [...command.contexts].sort(inputCompare), bindings: [...command.bindings].sort((left, right) => {
    const leftKey = left.kind === 'key' ? `key:${left.key}` : left.kind === 'bounded-text-entry' ? `text:${left.field}` : 'capture'
    const rightKey = right.kind === 'key' ? `key:${right.key}` : right.kind === 'bounded-text-entry' ? `text:${right.field}` : 'capture'
    return inputCompare(leftKey, rightKey)
  }) }))
  .sort((left, right) => inputCompare(left.id, right.id))

const key = (value: TerminalKeyboardKey, caseHandling: TerminalKeyBinding['caseHandling'] = 'exact'): TerminalKeyBinding => ({ kind: 'key', key: value, caseHandling })
const textEntry = (field: TerminalTextInputField, maximumLength: number): TerminalBoundedTextBinding => ({ kind: 'bounded-text-entry', field, characterPolicy: 'ascii-word-space-period-comma-apostrophe-hyphen', maximumLength, deletionKey: 'Backspace', caseHandling: 'preserve-typed-case' })
const keyCapture = (): TerminalKeyCaptureBinding => ({ kind: 'single-key-capture', maximumKeys: 1, cancellationKey: 'Escape', disallowsModifierChords: true })

/** Implemented bindings mirror every current canvas key path. */
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
  { id: 'controls-begin-key-capture', availability: 'implemented', surface: 'remapping', contexts: ['world-controls-editor'], bindings: [key('Enter')], accessibilityLabel: 'Capture one replacement key for the selected world control' },
  { id: 'controls-capture-key', availability: 'implemented', surface: 'remapping', contexts: ['world-controls-capture'], bindings: [keyCapture()], accessibilityLabel: 'Capture one supported key; Escape cancels without changing a binding' },
  { id: 'controls-editor-next', availability: 'implemented', surface: 'remapping', contexts: ['world-controls-editor'], bindings: [key('ArrowDown')], accessibilityLabel: 'Select the next remappable world control' },
  { id: 'controls-editor-previous', availability: 'implemented', surface: 'remapping', contexts: ['world-controls-editor'], bindings: [key('ArrowUp')], accessibilityLabel: 'Select the previous remappable world control' },
  { id: 'controls-open-editor', availability: 'implemented', surface: 'remapping', contexts: ['world'], bindings: [key('F2')], accessibilityLabel: 'Open world controls and remapping preferences' },
  { id: 'controls-reset-all', availability: 'implemented', surface: 'remapping', contexts: ['world-controls-editor'], bindings: [key('A', 'ascii-case-insensitive')], accessibilityLabel: 'Reset every remappable world control to its default' },
  { id: 'controls-reset-current', availability: 'implemented', surface: 'remapping', contexts: ['world-controls-editor'], bindings: [key('R', 'ascii-case-insensitive')], accessibilityLabel: 'Reset the selected world control to its default' },
  { id: 'world-command-help', availability: 'implemented', surface: 'help', contexts: ['world'], bindings: [key('?')], accessibilityLabel: 'Open command help with current effective bindings' },
  { id: 'world-contextual-prompt', availability: 'implemented', surface: 'contextual-action', contexts: ['world'], bindings: [key('Enter')], accessibilityLabel: 'Open the current contextual prompt' },
  { id: 'tavern-courier-switch-confirm', availability: 'implemented', surface: 'contextual-action', contexts: ['world-contextual-prompt'], bindings: [key('Enter')], accessibilityLabel: 'Confirm the selected available tavern-ledger courier switch, or report the current reserved contextual option as unavailable; this is zero-time' },
  { id: 'tavern-courier-switch-next', availability: 'implemented', surface: 'contextual-action', contexts: ['world-contextual-prompt'], bindings: [key('ArrowDown')], accessibilityLabel: 'Select the next switchable tavern-ledger courier candidate' },
  { id: 'tavern-courier-switch-previous', availability: 'implemented', surface: 'contextual-action', contexts: ['world-contextual-prompt'], bindings: [key('ArrowUp')], accessibilityLabel: 'Select the previous switchable tavern-ledger courier candidate' },
  { id: 'world-move-east', availability: 'implemented', surface: 'movement', contexts: ['world'], bindings: [key('ArrowRight'), key('L', 'ascii-case-insensitive')], accessibilityLabel: 'Attempt one known deck step east; blocked steps do not advance time' },
  { id: 'world-move-north', availability: 'implemented', surface: 'movement', contexts: ['world'], bindings: [key('ArrowUp'), key('K', 'ascii-case-insensitive')], accessibilityLabel: 'Attempt one known deck step north; blocked steps do not advance time' },
  { id: 'world-move-north-east', availability: 'implemented', surface: 'movement', contexts: ['world'], bindings: [key('U', 'ascii-case-insensitive')], accessibilityLabel: 'Attempt one known deck step north east; blocked steps do not advance time' },
  { id: 'world-move-north-west', availability: 'implemented', surface: 'movement', contexts: ['world'], bindings: [key('Y', 'ascii-case-insensitive')], accessibilityLabel: 'Attempt one known deck step north west; blocked steps do not advance time' },
  { id: 'world-move-south', availability: 'implemented', surface: 'movement', contexts: ['world'], bindings: [key('ArrowDown'), key('J', 'ascii-case-insensitive')], accessibilityLabel: 'Attempt one known deck step south; blocked steps do not advance time' },
  { id: 'world-move-south-east', availability: 'implemented', surface: 'movement', contexts: ['world'], bindings: [key('N', 'ascii-case-insensitive')], accessibilityLabel: 'Attempt one known deck step south east; blocked steps do not advance time' },
  { id: 'world-move-south-west', availability: 'implemented', surface: 'movement', contexts: ['world'], bindings: [key('B', 'ascii-case-insensitive')], accessibilityLabel: 'Attempt one known deck step south west; blocked steps do not advance time' },
  { id: 'world-move-west', availability: 'implemented', surface: 'movement', contexts: ['world'], bindings: [key('ArrowLeft'), key('H', 'ascii-case-insensitive')], accessibilityLabel: 'Attempt one known deck step west; blocked steps do not advance time' },
  { id: 'world-overlay-cancel', availability: 'implemented', surface: 'navigation', contexts: ['world-contextual-prompt', 'world-command-help', 'world-controls-editor', 'world-controls-capture'], bindings: [key('Escape')], accessibilityLabel: 'Cancel the open terminal overlay without changing world state' },
  { id: 'reserved-future-prompt-cancel', availability: 'reserved', surface: 'contextual-action', contexts: ['future-contextual-prompt'], bindings: [key('Escape')], accessibilityLabel: 'Reserved future materialized prompt cancellation; never advances world time' },
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
  /** Static commands show defaults; effective world bindings come from this isolated UI preference contract. */
  worldBindingPreferences: {
    contract: 'terminal-controls-v1'
    scope: 'browser-ui-only'
    remappableCommands: 'world-movement-context-management-help'
    protectedCancellation: 'Escape'
    protectedEditorEntry: 'F2'
  }
  commands: readonly TerminalKeyboardCommand[]
}

export interface TerminalAccessibilityModel {
  accessibleName: 'Jomon terminal presentation'
  conciseSummary: string
  focusContext: 'keyboard-first-canvas'
  mapText: string
  legendText: string
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
  legend: TerminalMapLegend
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
  | 'terminal-presentation.invalid-deck-plan'
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
  const keys = Object.keys(value).sort(compare)
  const expectedKeys = [...expected].sort(compare)
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
  if (!validId(value.id) || !oneOf(['jomon-deck-plan', 'future-materialized'] as const, value.context) || !record(value.origin) || !hasOnlyKeys(value.origin, ['column', 'row']) || !safeInteger(value.origin.column) || !safeInteger(value.origin.row) || !safeInteger(value.width) || !safeInteger(value.height) || value.width < 1 || value.height < 1 || value.width > TERMINAL_PRESENTATION_LIMITS.viewportWidth || value.height > TERMINAL_PRESENTATION_LIMITS.viewportHeight || value.width * value.height > TERMINAL_PRESENTATION_LIMITS.viewportCells) return [issue('terminal-viewport', 'terminal-presentation.invalid-viewport')]
  return []
}

const validCamera = (value: unknown, viewport: TerminalMapViewport): boolean => {
  if (!record(value) || !hasOnlyKeys(value, ['mode', 'focus', 'visibility']) || value.mode !== 'fixed-full-deck' || value.visibility !== 'all-static-deck-known' || !record(value.focus)) return false
  const focus = value.focus
  if (!hasOnlyKeys(focus, focus.coordinate === undefined ? [] : ['courierId', 'coordinate'])) return false
  if (focus.coordinate === undefined) return focus.courierId === undefined
  return typeof focus.courierId === 'string' && validId(focus.courierId)
    && record(focus.coordinate)
    && hasOnlyKeys(focus.coordinate, ['column', 'row'])
    && safeInteger(focus.coordinate.column)
    && safeInteger(focus.coordinate.row)
    && focus.coordinate.column >= viewport.origin.column
    && focus.coordinate.column < viewport.origin.column + viewport.width
    && focus.coordinate.row >= viewport.origin.row
    && focus.coordinate.row < viewport.origin.row + viewport.height
}

const cellOrder = (left: TerminalMaterializedCell, right: TerminalMaterializedCell): number => left.coordinate.row - right.coordinate.row
  || left.coordinate.column - right.coordinate.column
  || compare(left.id, right.id)

const activeCourierMarker = (value: unknown): value is TerminalMaterializedCell => record(value)
  && value.id === 'terminal-marker:active-courier'
  && record(value.glyph)
  && value.glyph.id === 'person:active-courier'

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
  if (viewport.context !== 'jomon-deck-plan' && viewport.context !== 'future-materialized') diagnostics.push(issue(viewport.id, 'terminal-presentation.invalid-viewport'))
  if (!Array.isArray(value) || value.length > TERMINAL_PRESENTATION_LIMITS.viewportCells) return canonicalDiagnostics([...diagnostics, issue('terminal-cells', 'terminal-presentation.invalid-cell')])
  const ids = new Set<string>()
  const coordinates = new Map<string, TerminalMaterializedCell[]>()
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
  for (const cell of cells) {
    const coordinateId = `${cell.coordinate.column}:${cell.coordinate.row}`
    coordinates.set(coordinateId, [...(coordinates.get(coordinateId) ?? []), cell])
  }
  for (const cellsAtCoordinate of coordinates.values()) {
    if (cellsAtCoordinate.length <= 1) continue
    const marker = cellsAtCoordinate.find(activeCourierMarker)
    if (cellsAtCoordinate.length !== 2 || !marker || cellsAtCoordinate.filter(cell => activeCourierMarker(cell)).length !== 1) diagnostics.push(issue(marker?.id ?? 'terminal-cell', 'terminal-presentation.duplicate-cell'))
  }
  if (cells.some((cell, index) => index > 0 && cellOrder(cells[index - 1]!, cell) >= 0)) diagnostics.push(issue('terminal-cells', 'terminal-presentation.noncanonical-cell-order'))
  const content = auditMedievalContentSafety(cells.map(cell => ({ id: `terminal-cell:${cell.id}`, domain: cell.contentDomain, classification: cell.contentSafety })))
  if (content.status === 'rejected') diagnostics.push(...content.diagnostics.map(item => issue(item.contentId, item.code)))
  return canonicalDiagnostics(diagnostics)
}

const statusText = (item: TerminalStatusItem): string => {
  if (item.value.kind === 'jomon-deck-materialized') return `Static Jomon deck map visible with ${item.value.cells} source-backed deck and hull cells; local movement is available, all eight physical stations have bounded zero-time contextual surfaces, and the tavern task ledger alone supports courier switching.`
  if (item.value.kind === 'courier-selection') return item.value.state === 'active'
    ? 'An active courier is selected.'
    : item.value.state === 'crew-extinct'
      ? 'No active courier remains; crew-extinction is read-only.'
      : 'Choose an initial courier before active play.'
  if (item.value.kind === 'world-minute') return `Current world minute ${item.value.minutes}.`
  if (item.value.kind === 'deck-focus') return item.value.coordinate === undefined
    ? 'Fixed full-deck camera has no courier focus until selection.'
    : `Fixed full-deck camera follows the active courier at column ${item.value.coordinate.column}, row ${item.value.coordinate.row}.`
  if (item.value.kind === 'vessel-prop-action') return `Latest vessel prop action ${item.value.action.replaceAll('-', ' ')} at ${item.value.propId}, recorded at world minute ${item.value.recordedAtWorldTime} in causal sequence ${item.value.causalSequence}.`
  return `Creation provenance seed ${item.value.seed}; digest ${item.value.digest}.`
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

const terminalCellFromDeckPlan = (
  id: string,
  coordinate: TerminalCellCoordinate,
  semantic: {
    glyph: TerminalGlyphReference
    paletteToken: JomonPaletteToken
    presentationState: TerminalPresentationState
    nonColorCue: TerminalNonColorCue
    textEquivalent: string
    accessibilityText: string
    contentSafety: MedievalContentSafetyClassification
  }
): TerminalMaterializedCell => ({
  id: `terminal-cell:${id}`,
  coordinate: { ...coordinate },
  glyph: structuredClone(semantic.glyph),
  paletteToken: semantic.paletteToken,
  presentationState: semantic.presentationState,
  nonColorCue: structuredClone(semantic.nonColorCue),
  textEquivalent: semantic.textEquivalent,
  accessibilityText: semantic.accessibilityText,
  evidence: {
    source: { kind: 'authoritative-record', recordId: 'vessel:jomon' },
    recordedAtWorldTime: 0,
    knownAtWorldTime: 0,
    freshness: { kind: 'timeless' }
  },
  contentDomain: 'template',
  contentSafety: structuredClone(semantic.contentSafety)
})

const terminalCellOrder = (left: TerminalMaterializedCell, right: TerminalMaterializedCell): number => left.coordinate.row - right.coordinate.row
  || left.coordinate.column - right.coordinate.column
  || compare(left.id, right.id)

const courierMarkerFor = (world: FoundationWorld): TerminalMaterializedCell | undefined => {
  const courierId = world.state.courier.activeCourierId
  const coordinate = world.state.navigation.coordinate
  if (courierId === undefined || world.state.navigation.courierId !== courierId || coordinate === undefined) return undefined
  const glyph = terminalGlyphReferenceFor('person:active-courier')
  return {
    id: 'terminal-marker:active-courier',
    coordinate: structuredClone(coordinate),
    glyph,
    paletteToken: 'selectedText',
    presentationState: 'ready',
    nonColorCue: terminalNonColorCueFor('ready'),
    textEquivalent: 'Active adult courier position.',
    accessibilityText: `Active adult courier marker at deck column ${coordinate.column}, row ${coordinate.row}; focus follows this known local position.`,
    evidence: currentWorldEvidence('world-state:navigation', world.state.temporal.worldTime),
    contentDomain: 'person',
    contentSafety: classifyMedievalContent('person', ['adult-labour', 'civil-life', 'navigation'], 'adults-only', ['data'])
  }
}

const mapForDeckPlan = (world: FoundationWorld, plan: ReturnType<typeof deriveJomonDeckPlan>): TerminalMaterializedMap => {
  const staticCells = [
    ...plan.areas.flatMap(area => area.footprint.map(cell => terminalCellFromDeckPlan(cell.id, cell.coordinate, area.semantic))),
    ...plan.structuralCells.map(cell => terminalCellFromDeckPlan(cell.id, cell.coordinate, cell.semantic))
  ].sort(terminalCellOrder)
  const marker = courierMarkerFor(world)
  const cells = [...staticCells, ...(marker === undefined ? [] : [marker])].sort(terminalCellOrder)
  const map: TerminalMaterializedMap = {
    state: 'materialized',
    viewport: {
      id: `terminal-viewport:${world.id}:jomon-deck`,
      context: 'jomon-deck-plan',
      origin: { column: 0, row: 0 },
      width: plan.bounds.width,
      height: plan.bounds.height
    },
    camera: {
      mode: 'fixed-full-deck',
      focus: marker === undefined ? {} : { courierId: world.state.courier.activeCourierId, coordinate: structuredClone(marker.coordinate) },
      visibility: 'all-static-deck-known'
    },
    cells,
    textEquivalent: 'Jomon deck plan with a known active courier position, quay approach, gangplank, hull boundary, and deck spaces.',
    accessibilityText: `Known static Jomon deck map. Fixed ${plan.bounds.width} by ${plan.bounds.height} viewport with ${staticCells.length} deck and hull cells${marker === undefined ? '' : ' and one active courier marker'}. Symbols: # hull, = deck, / gangplank, ) quay, @ active adult courier. Full deck known; ${marker === undefined && world.state.courier.initialCourierId !== undefined ? 'no active courier remains and crew-extinction is read-only.' : 'each exact physical station provides a source-backed zero-time contextual readout. The tavern task ledger alone also supports zero-time courier switching and its availability/loss readout.'} Cargo contents, other people, hazards, travel, rest, and conversation remain unavailable; recorded loss continuity is read-only.`,
    evidence: presentationEvidence('terminal-presentation:jomon-deck-plan'),
    contentDomain: 'player-facing-text',
    contentSafety: baseClassification()
  }
  const cellDiagnostics = validateTerminalMaterializedCells(map.viewport, map.cells, terminalGlyphCatalog(JOMON_ASCII_GLYPH_CATALOG))
  if (cellDiagnostics.length) throw new TerminalPresentationContractError(cellDiagnostics)
  return map
}

/** The sole current common map projection derives every visible cell from the validated static deck plan. */
export const createJomonDeckTerminalMap = (world: FoundationWorld): TerminalMaterializedMap => {
  try {
    return mapForDeckPlan(world, deriveJomonDeckPlan(world))
  } catch {
    throw new TerminalPresentationContractError([issue('jomon-deck-plan', 'terminal-presentation.invalid-deck-plan')])
  }
}

/** Reuses a complete FoundationWorld validation already performed by the terminal presentation boundary. */
const createJomonDeckTerminalMapForVerifiedWorld = (world: FoundationWorld): TerminalMaterializedMap => {
  try {
    return mapForDeckPlan(world, deriveJomonDeckPlanForVerifiedWorld(world))
  } catch {
    throw new TerminalPresentationContractError([issue('jomon-deck-plan', 'terminal-presentation.invalid-deck-plan')])
  }
}

const staticCellsFor = (map: TerminalMaterializedMap): number => map.cells.filter(cell => !activeCourierMarker(cell)).length

const legendFreshnessText = (evidence: TerminalEvidenceProvenance): string => evidence.freshness.kind === 'current'
  ? 'current'
  : evidence.freshness.kind === 'timeless'
    ? 'timeless'
    : `reported at world minute ${evidence.freshness.atWorldTime}`

const legendEntryFor = (map: TerminalMaterializedMap, glyphId: string): TerminalMapLegendEntry => {
  const cell = map.cells.filter(candidate => candidate.glyph.id === glyphId).sort(cellOrder)[0]
  const glyph = JOMON_ASCII_GLYPH_CATALOG.entries.find(entry => entry.id === glyphId)
  if (!cell || !glyph || !same(cell.glyph, terminalGlyphReferenceFor(glyphId)) || cell.paletteToken !== glyph.paletteToken
    || cell.presentationState !== glyph.presentationState || !same(cell.nonColorCue, glyph.nonColorCue)) throw new Error('terminal map legend source is invalid')
  const evidence = structuredClone(cell.evidence)
  return {
    id: `terminal-map-legend:${glyph.id}`,
    glyph: terminalGlyphReferenceFor(glyph.id),
    label: glyph.label,
    paletteToken: glyph.paletteToken,
    presentationState: glyph.presentationState,
    nonColorCue: structuredClone(glyph.nonColorCue),
    textEquivalent: glyph.textEquivalent,
    accessibilityText: `${glyph.accessibilityText} Source ${evidence.source.kind} ${evidence.source.recordId}; known at world minute ${evidence.knownAtWorldTime}; ${legendFreshnessText(evidence)}.`,
    evidence,
    contentDomain: glyph.contentDomain,
    contentSafety: structuredClone(glyph.contentSafety)
  }
}

const rawTerminalMapLegend = (map: TerminalMaterializedMap): TerminalMapLegend => {
  const glyphIds = [...new Set(map.cells.map(cell => cell.glyph.id))].sort(compare)
  if (!glyphIds.length || glyphIds.length > TERMINAL_PRESENTATION_LIMITS.legendEntries) throw new Error('terminal map legend has an invalid visible glyph set')
  const entries = glyphIds.map(glyphId => legendEntryFor(map, glyphId))
  const movementText = 'Known fixed local deck only. A successful local step advances one action minute; a blocked hull, boundary, non-walkable, or diagonal-corner step changes no world state or time.'
  const limitationsText = 'Fixed known deck. Each exact station anchor has a zero-time bounded readout record; the tavern task ledger alone also supports zero-time courier switching. No cargo contents, NPCs, hazards, travel, fog, rest, conversation, or succession.'
  return {
    version: TERMINAL_MAP_LEGEND_CONTRACT_VERSION,
    entries,
    movementText,
    limitationsText,
    accessibilityText: `Map legend. ${entries.map(entry => entry.accessibilityText).join(' ')} ${movementText} ${limitationsText}`,
    evidence: structuredClone(map.evidence),
    contentDomain: 'player-facing-text',
    contentSafety: baseClassification()
  }
}

/** Builds a discardable legend from a validated source-backed terminal map, never from world state. */
export const createTerminalMapLegend = (map: TerminalMaterializedMap): TerminalMapLegend => {
  const diagnostics = validateTerminalMaterializedCells(map.viewport, map.cells, terminalGlyphCatalog(JOMON_ASCII_GLYPH_CATALOG))
  if (diagnostics.length) throw new TerminalPresentationContractError(diagnostics)
  return rawTerminalMapLegend(map)
}

/** Validates the complete, canonical legend against only its source-backed map projection. */
export const validateTerminalMapLegend = (map: TerminalMaterializedMap, value: unknown): readonly TerminalPresentationDiagnostic[] => {
  try {
    const mapDiagnostics = validateTerminalMaterializedCells(map.viewport, map.cells, terminalGlyphCatalog(JOMON_ASCII_GLYPH_CATALOG))
    if (mapDiagnostics.length) return mapDiagnostics
    const expected = rawTerminalMapLegend(map)
    return same(value, expected) ? [] : [issue('terminal-map-legend', 'terminal-presentation.invalid-model')]
  } catch (error) {
    if (error instanceof TerminalPresentationContractError) return error.diagnostics
    return [issue('terminal-map-legend', 'terminal-presentation.invalid-model')]
  }
}

const actionEvidence = (feedback: VesselPropActionFeedback, knownAtWorldTime: number): TerminalEvidenceProvenance => ({
  source: { kind: 'authoritative-record', recordId: `world-state:jomon:prop-actions:${feedback.propId}` },
  recordedAtWorldTime: feedback.action.recordedAtWorldTime,
  knownAtWorldTime,
  freshness: feedback.action.recordedAtWorldTime === knownAtWorldTime
    ? { kind: 'current' }
    : { kind: 'reported-at-world-time', atWorldTime: feedback.action.recordedAtWorldTime }
})

const actionValue = (feedback: VesselPropActionFeedback): Extract<TerminalStatusValue, { kind: 'vessel-prop-action' }> => ({
  kind: 'vessel-prop-action',
  propId: feedback.propId,
  action: feedback.action.kind,
  recordedAtWorldTime: feedback.action.recordedAtWorldTime,
  causalSequence: feedback.action.causalSequence
})

const latestVesselPropAction = (world: FoundationWorld): VesselPropActionFeedback | undefined => {
  const feedback = vesselPropActionFeedbacks(world.state.jomon.propActions)
  return [...feedback].sort((left, right) => right.action.recordedAtWorldTime - left.action.recordedAtWorldTime
    || right.action.causalSequence - left.action.causalSequence
    || compare(left.propId, right.propId))[0]
}

const terminalVesselPropActionMessages = (world: FoundationWorld): readonly TerminalMessage[] => vesselPropActionFeedbacks(world.state.jomon.propActions).map(feedback => {
  const state = feedback.presentationState
  const evidence = actionEvidence(feedback, world.state.temporal.worldTime)
  return {
    id: `terminal-message:vessel-prop-action:${feedback.propId}`,
    kind: 'vessel-prop-action',
    state,
    paletteToken: TERMINAL_STATE_PRESENTATIONS[state].paletteToken,
    nonColorCue: terminalNonColorCueFor(state),
    sourceRecordKind: 'vessel-prop-action',
    value: actionValue(feedback),
    text: feedback.text,
    accessibilityText: `${feedback.text} Source ${evidence.source.recordId}; recorded at world minute ${feedback.action.recordedAtWorldTime}; known at world minute ${world.state.temporal.worldTime}; ${terminalNonColorCueFor(state).text} cue.`,
    evidence,
    contentDomain: 'player-facing-text' as const,
    contentSafety: structuredClone(feedback.contentSafety)
  } satisfies TerminalMessage
}).sort((left, right) => compare(left.id, right.id))

const orderedStatus = (world: FoundationWorld, map: TerminalMaterializedMap): readonly TerminalStatusItem[] => canonicalById([
  statusItem('terminal-status:courier', world.state.courier.activeCourierId ? 'ready' : world.state.courier.initialCourierId ? 'warning' : 'waiting', {
    kind: 'courier-selection',
    state: world.state.courier.activeCourierId ? 'active' : world.state.courier.initialCourierId ? 'crew-extinct' : 'awaiting-initial'
  }, currentWorldEvidence('world-state:courier', world.state.temporal.worldTime)),
  statusItem('terminal-status:focus', world.state.navigation.coordinate === undefined ? 'waiting' : 'ready', { kind: 'deck-focus', ...(world.state.navigation.coordinate === undefined ? {} : { courierId: world.state.navigation.courierId, coordinate: structuredClone(world.state.navigation.coordinate) }) }, currentWorldEvidence('world-state:navigation', world.state.temporal.worldTime)),
  statusItem('terminal-status:map', 'neutral', { kind: 'jomon-deck-materialized', cells: staticCellsFor(map) }, presentationEvidence('terminal-presentation:jomon-deck-plan-status')),
  statusItem('terminal-status:provenance', 'neutral', { kind: 'creation-provenance', seed: world.manifest.creation.seed, digest: world.manifest.creation.digest }, presentationEvidence('world-manifest:creation')),
  statusItem('terminal-status:time', 'neutral', { kind: 'world-minute', minutes: world.state.temporal.worldTime }, currentWorldEvidence('world-state:temporal', world.state.temporal.worldTime)),
  ...(() => {
    const latest = latestVesselPropAction(world)
    return latest === undefined ? [] : [statusItem('terminal-status:vessel-prop-action', latest.presentationState, actionValue(latest), actionEvidence(latest, world.state.temporal.worldTime))]
  })()
])

const canonicalById = <Value extends { id: string }>(values: readonly Value[]): readonly Value[] => [...values].sort((left, right) => compare(left.id, right.id))

const ledgerContentRecordsForPrompt = (item: TerminalPrompt): readonly ClassifiedMedievalContent[] => {
  if (item.kind !== 'tavern-courier-switch' || item.ledger === undefined) return []
  const ledger = item.ledger
  return [
    { id: `terminal-presentation:ledger:${ledger.id}`, domain: ledger.contentDomain, classification: ledger.contentSafety },
    ...ledger.members.map(member => ({ id: `terminal-presentation:ledger-member:${member.id}`, domain: member.contentDomain, classification: member.contentSafety })),
    ...(ledger.continuity === undefined ? [] : [{ id: `terminal-presentation:ledger-continuity:${ledger.continuity.kind}`, domain: ledger.continuity.contentDomain, classification: ledger.continuity.contentSafety }])
  ]
}

const stationContentRecordsForPrompt = (item: TerminalPrompt): readonly ClassifiedMedievalContent[] => item.kind === 'vessel-station-readout'
  ? [{ id: `terminal-presentation:station:${item.readout.source.propId}`, domain: 'template', classification: item.readout.contentSafety }]
  : []

const modelContentRecords = (map: TerminalMapSurface, legend: TerminalMapLegend, status: readonly TerminalStatusItem[], messages: readonly TerminalMessage[], prompts: readonly TerminalPrompt[]): readonly ClassifiedMedievalContent[] => [
  { id: 'terminal-presentation:map', domain: map.contentDomain, classification: map.contentSafety },
  ...map.cells.map(cell => ({ id: `terminal-presentation:map-cell:${cell.id}`, domain: cell.contentDomain, classification: cell.contentSafety })),
  { id: 'terminal-presentation:legend', domain: legend.contentDomain, classification: legend.contentSafety },
  ...legend.entries.map(entry => ({ id: `terminal-presentation:legend-entry:${entry.id}`, domain: entry.contentDomain, classification: entry.contentSafety })),
  ...status.map(item => ({ id: `terminal-presentation:status:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...messages.map(item => ({ id: `terminal-presentation:message:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })),
  ...prompts.flatMap(item => [
    { id: `terminal-presentation:prompt:${item.id}`, domain: item.contentDomain, classification: item.contentSafety },
    ...ledgerContentRecordsForPrompt(item),
    ...stationContentRecordsForPrompt(item)
  ])
]

const accessibilityFor = (map: TerminalMapSurface, legend: TerminalMapLegend, status: readonly TerminalStatusItem[], messages: readonly TerminalMessage[], prompts: readonly TerminalPrompt[]): TerminalAccessibilityModel => ({
  accessibleName: 'Jomon terminal presentation',
  conciseSummary: `Materialized static Jomon deck map with ${map.cells.length} source-backed cells. ${status.length} immediate local status entries. ${messages.length} authoritative messages. ${prompts.length} contextual prompts.`,
  focusContext: 'keyboard-first-canvas',
  mapText: map.accessibilityText,
  legendText: legend.accessibilityText,
  statusText: status.map(item => item.accessibilityText),
  messageText: messages.length ? messages.map(item => item.accessibilityText) : ['No current authoritative messages.'],
  promptText: prompts.length ? prompts.map(item => item.accessibilityText) : ['No current contextual prompt.'],
  commandText: TERMINAL_KEYBOARD_COMMANDS.map(command => command.accessibilityLabel)
})

const rawTerminalPresentationModel = (world: FoundationWorld): TerminalPresentationModel => {
  const map = createJomonDeckTerminalMapForVerifiedWorld(world)
  const legend = rawTerminalMapLegend(map)
  const status = orderedStatus(world, map)
  const messages = terminalVesselPropActionMessages(world)
  const prompts: readonly TerminalPrompt[] = [createJomonDeckContextualPromptForVerifiedWorld(world)]
  const contentSafetyAudit = auditMedievalContentSafety(modelContentRecords(map, legend, status, messages, prompts))
  if (contentSafetyAudit.status === 'rejected') throw new TerminalPresentationContractError(contentSafetyAudit.diagnostics.map(item => issue(item.contentId, item.code)))
  return {
    version: TERMINAL_PRESENTATION_CONTRACT_VERSION,
    worldId: world.id,
    map,
    legend,
    status,
    messages,
    prompts,
    input: {
      canvasFocus: { keyboardFirst: true, pointerFocusAssist: true },
      modes: TERMINAL_INPUT_MODES,
      worldBindingPreferences: {
        contract: 'terminal-controls-v1',
        scope: 'browser-ui-only',
        remappableCommands: 'world-movement-context-management-help',
        protectedCancellation: 'Escape',
        protectedEditorEntry: 'F2'
      },
      commands: TERMINAL_KEYBOARD_COMMANDS
    },
    accessibility: accessibilityFor(map, legend, status, messages, prompts),
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

const promptCandidate = (candidate: { id: string; name: string; role: string; conversation: 1 | 2 | 3 | 4 | 5 }): TerminalCourierSwitchCandidate => ({
  id: candidate.id,
  name: candidate.name,
  role: candidate.role,
  conversation: candidate.conversation
})

const ledgerPresentation = (status: TerminalTavernLedgerMemberStatus): Pick<TerminalTavernLedgerMember, 'paletteToken' | 'presentationState' | 'nonColorCue'> => {
  if (status.startsWith('active-')) return {
    paletteToken: 'selectedText',
    presentationState: 'ready',
    nonColorCue: { key: 'activeState', text: JOMON_NON_COLOR_STATE_CUES.activeState }
  }
  if (status === 'available') return {
    paletteToken: TERMINAL_STATE_PRESENTATIONS.ready.paletteToken,
    presentationState: 'ready',
    nonColorCue: terminalNonColorCueFor('ready')
  }
  if (status === 'committed') return {
    paletteToken: TERMINAL_STATE_PRESENTATIONS.waiting.paletteToken,
    presentationState: 'waiting',
    nonColorCue: terminalNonColorCueFor('waiting')
  }
  if (status === 'temporarily-unavailable' || status === 'departed') return {
    paletteToken: TERMINAL_STATE_PRESENTATIONS.warning.paletteToken,
    presentationState: 'warning',
    nonColorCue: terminalNonColorCueFor('warning')
  }
  return {
    paletteToken: TERMINAL_STATE_PRESENTATIONS.risk.paletteToken,
    presentationState: 'risk',
    nonColorCue: terminalNonColorCueFor('risk')
  }
}

const ledgerStatusFor = (
  memberId: string,
  activeCourierId: string,
  departedCourierIds: ReadonlySet<string>,
  person: FoundationWorld['state']['people']['records'][number] | undefined
): TerminalTavernLedgerMemberStatus => {
  if (memberId === activeCourierId) {
    if (person?.work.availability === 'committed') return 'active-committed'
    if (person?.work.availability === 'unavailable') return 'active-temporarily-unavailable'
    return 'active-available'
  }
  if (person?.life.status === 'dead') return 'dead'
  if (departedCourierIds.has(memberId)) return 'departed'
  if (person?.work.availability === 'committed') return 'committed'
  if (person?.work.availability === 'unavailable') return 'temporarily-unavailable'
  return 'available'
}

const ledgerStatusText = (status: TerminalTavernLedgerMemberStatus): string => {
  if (status === 'active-available') return 'ACTIVE // AVAILABLE'
  if (status === 'active-committed') return 'ACTIVE // COMMITTED'
  if (status === 'active-temporarily-unavailable') return 'ACTIVE // TEMPORARILY UNAVAILABLE'
  if (status === 'available') return 'AVAILABLE'
  if (status === 'committed') return 'COMMITTED'
  if (status === 'temporarily-unavailable') return 'TEMPORARILY UNAVAILABLE'
  if (status === 'departed') return 'PERMANENTLY DEPARTED'
  return 'DEAD'
}

const ledgerMemberClassification = (world: FoundationWorld, memberId: string): MedievalContentSafetyClassification => {
  const source = world.crew.find(member => member.id === memberId)
  if (!source) throw new Error(`missing immutable household source for ${memberId}`)
  return structuredClone(source.contentSafety)
}

const continuedLossCommandFor = (world: FoundationWorld): CourierLossResolvedCommand | undefined => {
  const activeCourierId = world.state.courier.activeCourierId
  if (activeCourierId === undefined) return undefined
  return [...world.state.causalHistory.tail].reverse().find((command): command is CourierLossResolvedCommand => command.kind === 'courier-loss-resolved'
    && command.payload.finalization === 'continue'
    && command.payload.successorId === activeCourierId)
}

const ledgerContinuityFor = (world: FoundationWorld): TerminalTavernLedgerContinuity | undefined => {
  const command = continuedLossCommandFor(world)
  if (!command) return undefined
  const recordedAtWorldTime = command.payload.confirmation.atWorldTime
  return {
    kind: 'continued-after-recorded-loss',
    accessibilityText: `The active perspective continued after a recorded permanent loss. Source-backed record at world minute ${recordedAtWorldTime}; known current at world minute ${world.state.temporal.worldTime}.`,
    evidence: {
      source: { kind: 'authoritative-record', recordId: `causal-command:${command.id}` },
      recordedAtWorldTime,
      knownAtWorldTime: world.state.temporal.worldTime,
      freshness: { kind: 'reported-at-world-time', atWorldTime: recordedAtWorldTime }
    },
    contentDomain: 'event',
    contentSafety: structuredClone(command.contentSafety)
  }
}

const ledgerReadoutForVerifiedWorld = (world: FoundationWorld): TerminalTavernLedgerReadout => {
  const activeCourierId = world.state.courier.activeCourierId
  if (activeCourierId === undefined) throw new Error('a physical ledger needs an active courier')
  const people = new Map(world.state.people.records.map(person => [person.id, person]))
  const departed = new Set(world.state.courier.departedCourierIds)
  const knownAtWorldTime = world.state.temporal.worldTime
  const members = initialHouseholdActiveCrew(world.crew).map(member => {
    const status = ledgerStatusFor(member.id, activeCourierId, departed, people.get(member.id))
    const semantic = ledgerPresentation(status)
    const statusText = ledgerStatusText(status)
    return {
      ...promptCandidate(member),
      status,
      ...semantic,
      accessibilityText: `${member.name}, ${member.role}: ${statusText}. ${semantic.nonColorCue.text} cue; ${semantic.presentationState} semantic state. Source household state; current at world minute ${knownAtWorldTime}.`,
      evidence: currentWorldEvidence(`person:${member.id}`, knownAtWorldTime),
      contentDomain: 'person' as const,
      contentSafety: ledgerMemberClassification(world, member.id)
    } satisfies TerminalTavernLedgerMember
  })
  const continuity = ledgerContinuityFor(world)
  const statusSummary = members.map(member => `${member.name}: ${ledgerStatusText(member.status)}`).join('; ')
  return {
    id: 'terminal-ledger:task-ledger',
    members,
    ...(continuity === undefined ? {} : { continuity }),
    accessibilityText: `Physical tavern task ledger readout. ${members.length} canonical household members in household order: ${statusSummary}. ${continuity?.accessibilityText ?? 'No retained continuity-transfer summary is currently displayed.'} Inspection and selection are zero-time; Escape cancels without mutation.`,
    evidence: {
      source: { kind: 'authoritative-record', recordId: 'deck-prop-binding:prop:task-ledger' },
      recordedAtWorldTime: knownAtWorldTime,
      knownAtWorldTime,
      freshness: { kind: 'current' }
    },
    contentDomain: 'player-facing-text',
    contentSafety: baseClassification()
  }
}

const currentPromptEvidence = (recordId: string, worldMinute: number): TerminalEvidenceProvenance => ({
  source: { kind: 'authoritative-record', recordId },
  recordedAtWorldTime: worldMinute,
  knownAtWorldTime: worldMinute,
  freshness: { kind: 'current' }
})

const futureContextualPrompt = (
  world: FoundationWorld,
  recordId: string,
  disabledReason: TerminalPromptDisabledReason,
  accessibilityText: string
): TerminalFuturePrompt => {
  const prompt: TerminalFuturePrompt = {
    id: `terminal-prompt:jomon-deck:${world.id}`,
    kind: 'future-contextual-choice',
    accessibilityText,
    evidence: currentPromptEvidence(recordId, world.state.temporal.worldTime),
    contentDomain: 'player-facing-text',
    contentSafety: baseClassification(),
    options: [{
      id: `terminal-prompt-option:contextual-unavailable:${world.id}`,
      key: 'Enter',
      availability: 'disabled',
      disabledReason,
      intent: 'future-contextual-action',
      requiresConfirmation: false,
      nonColorCue: terminalNonColorCueFor('neutral'),
      accessibilityText: 'This contextual option is unavailable. Enter reports the bounded unavailable result; Escape cancels without mutation.'
    }],
    cancellation: { key: 'Escape', outcome: 'cancelled-no-mutation', advancesWorldTime: false }
  }
  const diagnostics = validateTerminalPrompt(prompt)
  if (diagnostics.length) throw new TerminalPresentationContractError(diagnostics)
  return prompt
}

const vesselOperationForPrompt = (operation: VesselProximityOperation): TerminalVesselPromptOperation => {
  if (operation.proximity !== 'at-anchor' || (operation.availability !== 'implemented' && operation.availability !== 'readout')) {
    throw new TerminalPresentationContractError([issue(operation.source.propId, 'terminal-presentation.invalid-prompt')])
  }
  if (operation.availability === 'implemented') {
    if (operation.reason !== undefined || operation.source.propId !== 'prop:task-ledger') throw new TerminalPresentationContractError([issue(operation.source.propId, 'terminal-presentation.invalid-prompt')])
    return { proximity: 'at-anchor', availability: 'implemented' }
  }
  return {
    proximity: 'at-anchor',
    availability: 'readout',
    ...(operation.reason === undefined ? {} : { reason: operation.reason as Extract<VesselProximityOperationReason, 'route-comparison-not-implemented' | 'quay-travel-not-implemented'> })
  }
}

const stationReadoutPromptFor = (world: FoundationWorld, operation: VesselProximityOperation): TerminalVesselStationReadoutPrompt => {
  const promptOperation = vesselOperationForPrompt(operation)
  if (promptOperation.availability !== 'readout') throw new TerminalPresentationContractError([issue(operation.source.propId, 'terminal-presentation.invalid-prompt')])
  const readout = createVesselStationReadoutForVerifiedWorld(world, operation.source.propId as Parameters<typeof createVesselStationReadoutForVerifiedWorld>[1])
  if (!same(operation.source, readout.source)) throw new TerminalPresentationContractError([issue(operation.source.propId, 'terminal-presentation.invalid-prompt')])
  const prompt: TerminalVesselStationReadoutPrompt = {
    id: `terminal-prompt:jomon-deck:${world.id}`,
    kind: 'vessel-station-readout',
    label: readout.label,
    accessibilityText: `${readout.accessibilityText} Enter records this bounded station readout at zero world time; Escape cancels without mutation or world-time change.`,
    evidence: currentPromptEvidence(readout.factSourceId, world.state.temporal.worldTime),
    contentDomain: 'player-facing-text',
    contentSafety: baseClassification(),
    source: structuredClone(operation.source),
    operation: promptOperation,
    readout,
    options: [{
      id: `terminal-prompt-option:${operation.source.propId}:${world.id}`,
      key: 'Enter',
      availability: 'available',
      intent: 'vessel-station-readout-record',
      requiresConfirmation: true,
      nonColorCue: terminalNonColorCueFor('ready'),
      accessibilityText: `${readout.label} is ready to record this bounded zero-time readout. Enter confirms; Escape cancels without mutation.`
    }],
    cancellation: { key: 'Escape', outcome: 'cancelled-no-mutation', advancesWorldTime: false }
  }
  const diagnostics = validateTerminalPrompt(prompt)
  if (diagnostics.length) throw new TerminalPresentationContractError(diagnostics)
  return prompt
}

/** Reuses complete world validation already performed by the presentation boundary. */
const createJomonDeckContextualPromptForVerifiedWorld = (world: FoundationWorld): TerminalPrompt => {
  if (world.state.courier.initialCourierId === undefined) {
    return futureContextualPrompt(world, 'world-state:courier', 'missing-required-location', 'Context prompt unavailable until initial courier selection completes. Escape cancels without mutation.')
  }
  if (world.state.courier.activeCourierId === undefined) {
    return futureContextualPrompt(world, 'world-state:courier', 'requires-future-domain-rule', 'No active courier remains after crew-extinction. The read-only chronicle has no contextual operation; Escape cancels without mutation.')
  }
  const vesselAssessment = assessVesselProximityOperationsForVerifiedWorld(world, world.state)
  const activeOperation = vesselAssessment.operations.find(operation => operation.proximity === 'at-anchor')
  if (!activeOperation) return futureContextualPrompt(world, 'world-state:navigation', 'no-contextual-action-materialized', 'No physical vessel prop is at the active courier\'s exact anchor. Contextual operation is unavailable; Escape cancels without mutation.')
  if (activeOperation.availability === 'readout') return stationReadoutPromptFor(world, activeOperation)
  if (activeOperation.source.propId !== 'prop:task-ledger') throw new TerminalPresentationContractError([issue(activeOperation.source.propId, 'terminal-presentation.invalid-prompt')])
  const assessment = assessTavernCourierSwitchForVerifiedWorld(world, world.state)
  const ledger = ledgerReadoutForVerifiedWorld(world)
  const disabledReason = assessment.status === 'unavailable' ? assessment.reason : undefined
  const option: TerminalPromptOption = {
    id: `terminal-prompt-option:tavern-ledger:${world.id}`,
    key: 'Enter',
    availability: assessment.status === 'available' ? 'available' : 'disabled',
    ...(disabledReason === undefined ? {} : { disabledReason }),
    intent: 'tavern-courier-switch',
    requiresConfirmation: assessment.status === 'available',
    nonColorCue: terminalNonColorCueFor(assessment.status === 'available' ? 'ready' : 'neutral'),
    accessibilityText: assessment.status === 'available'
      ? 'Switch courier at the task ledger. Arrow keys select an eligible available adult; Enter confirms a zero-time switch; Escape cancels without mutation.'
      : assessment.reason === 'not-at-tavern-ledger'
        ? 'Courier switching is unavailable away from the task ledger anchor. Return to the tavern ledger; Escape cancels without mutation.'
        : 'Courier switching is unavailable because no alternate eligible living available household member exists; Escape cancels without mutation.'
  }
  const prompt: TerminalTavernCourierSwitchPrompt = {
    id: `terminal-prompt:jomon-deck:${world.id}`,
    kind: 'tavern-courier-switch',
    accessibilityText: assessment.status === 'available'
      ? `Tavern task ledger courier switch. Current courier ${assessment.current.name}, ${assessment.current.role}; ${assessment.candidates.length} alternate eligible living available household member${assessment.candidates.length === 1 ? '' : 's'} in canonical household order. ${ledger?.accessibilityText ?? 'The physical ledger availability readout is unavailable away from its anchor.'} Arrow keys select, Enter confirms a zero-time viewpoint switch, Escape cancels. Cargo, travel, rest, conversation, and other prop actions remain unavailable; recorded loss continuity is read-only.`
      : `Tavern task ledger courier switch unavailable: ${option.accessibilityText} Current courier ${assessment.current.name}, ${assessment.current.role}.`,
    evidence: {
      source: { kind: 'authoritative-record', recordId: assessment.source.propBindingId },
      recordedAtWorldTime: world.state.temporal.worldTime,
      knownAtWorldTime: world.state.temporal.worldTime,
      freshness: { kind: 'current' }
    },
    contentDomain: 'player-facing-text',
    contentSafety: baseClassification(),
    source: structuredClone(assessment.source),
    operation: vesselOperationForPrompt(activeOperation),
    current: promptCandidate(assessment.current),
    candidates: assessment.candidates.map(promptCandidate),
    ...(ledger === undefined ? {} : { ledger }),
    options: [option],
    cancellation: { key: 'Escape', outcome: 'cancelled-no-mutation', advancesWorldTime: false }
  }
  const diagnostics = validateTerminalPrompt(prompt)
  if (diagnostics.length) throw new TerminalPresentationContractError(diagnostics)
  return prompt
}

/** Public prompt creation validates world evidence before yielding a renderer-only object. */
export const createJomonDeckContextualPrompt = (world: FoundationWorld): TerminalPrompt => {
  ensureValidWorld(world)
  return createJomonDeckContextualPromptForVerifiedWorld(world)
}

/** Validates bounded authoritative message routes without granting presentation world authority. */
export const validateTerminalMessages = (value: unknown): readonly TerminalPresentationDiagnostic[] => {
  if (!Array.isArray(value) || value.length > TERMINAL_PRESENTATION_LIMITS.messages) return [issue('terminal-messages', 'terminal-presentation.invalid-message')]
  const diagnostics: TerminalPresentationDiagnostic[] = []
  const ids = new Set<string>()
  for (const candidate of value) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'terminal-message'
    const actionValue = record(candidate) && record(candidate.value) ? candidate.value : undefined
    const vesselActionValid = actionValue !== undefined
      && hasOnlyKeys(actionValue, ['propId', 'action', 'recordedAtWorldTime', 'causalSequence'])
      && typeof actionValue.propId === 'string'
      && (actionValue.action === 'station-readout-recorded' || actionValue.action === 'tavern-courier-switched')
      && safeInteger(actionValue.recordedAtWorldTime)
      && safeInteger(actionValue.causalSequence)
      && actionValue.causalSequence > 0
    const validActionValue = vesselActionValid
      ? actionValue as { propId: string; action: VesselPropActionKind; recordedAtWorldTime: number; causalSequence: number }
      : undefined
    const vesselMessage = record(candidate) && candidate.kind === 'vessel-prop-action'
    const expectedKeys = vesselMessage
      ? ['id', 'kind', 'state', 'paletteToken', 'nonColorCue', 'sourceRecordKind', 'value', 'text', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety']
      : ['id', 'kind', 'state', 'paletteToken', 'nonColorCue', 'sourceRecordKind', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety']
    if (!record(candidate) || !hasOnlyKeys(candidate, expectedKeys) || !validId(candidate.id) || !oneOf(TERMINAL_MESSAGE_KINDS, candidate.kind) || !oneOf(TERMINAL_PRESENTATION_STATES, candidate.state) || !paletteToken(candidate.paletteToken) || candidate.paletteToken !== TERMINAL_STATE_PRESENTATIONS[candidate.state].paletteToken || !validCue(candidate.nonColorCue, candidate.state) || !oneOf(['causal-command', 'delegation-record', 'social-memory-record', 'future-authoritative-record', 'vessel-prop-action'] as const, candidate.sourceRecordKind) || (vesselMessage && (!validText(candidate.text) || validActionValue === undefined || candidate.sourceRecordKind !== 'vessel-prop-action' || candidate.id !== `terminal-message:vessel-prop-action:${validActionValue.propId}`)) || !validText(candidate.accessibilityText)) diagnostics.push(issue(id, 'terminal-presentation.invalid-message'))
    if (ids.has(id)) diagnostics.push(issue(id, 'terminal-presentation.invalid-message'))
    ids.add(id)
    const evidenceIssues = validateTerminalEvidence(candidate.evidence)
    if (evidenceIssues.length || !record(candidate.evidence) || !record(candidate.evidence.source) || candidate.evidence.source.kind !== 'authoritative-record' || (candidate.kind === 'vessel-prop-action' && (validActionValue === undefined || candidate.evidence.source.recordId !== `world-state:jomon:prop-actions:${validActionValue.propId}` || candidate.evidence.recordedAtWorldTime !== validActionValue.recordedAtWorldTime || candidate.evidence.knownAtWorldTime < validActionValue.recordedAtWorldTime))) diagnostics.push(issue(id, 'terminal-presentation.invalid-message'))
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
  && (value.intent === 'future-contextual-action' || value.intent === 'tavern-courier-switch' || value.intent === 'vessel-station-readout-record')
  && typeof value.requiresConfirmation === 'boolean'
  && validCue(value.nonColorCue, value.availability === 'available' ? 'ready' : 'neutral')
  && validText(value.accessibilityText)
  && (value.availability === 'disabled' ? oneOf(TERMINAL_PROMPT_DISABLED_REASONS, value.disabledReason) : value.disabledReason === undefined)

const validCourierIdentity = (value: unknown): value is TerminalCourierSwitchCandidate => record(value)
  && validId(value.id) && typeof value.name === 'string' && value.name.length <= 96 && /^[A-Za-z]+ [A-Za-z]+$/u.test(value.name)
  && typeof value.role === 'string' && Object.hasOwn(INITIAL_HOUSEHOLD_EQUIPMENT_BY_ROLE, value.role)
  && typeof value.conversation === 'number' && Number.isSafeInteger(value.conversation) && value.conversation >= 1 && value.conversation <= 5

const validCourierSwitchCandidate = (value: unknown): value is TerminalCourierSwitchCandidate => record(value)
  && hasOnlyKeys(value, ['id', 'name', 'role', 'conversation'])
  && validCourierIdentity(value)

const validTavernSource = (value: unknown): value is TavernCourierSwitchSource => record(value)
  && hasOnlyKeys(value, ['propBindingId', 'propId', 'areaId', 'coordinate'])
  && value.propBindingId === 'deck-prop-binding:prop:task-ledger' && value.propId === 'prop:task-ledger' && value.areaId === 'tavern'
  && record(value.coordinate) && hasOnlyKeys(value.coordinate, ['column', 'row']) && value.coordinate.column === 4 && value.coordinate.row === 4

const validVesselPromptOperation = (value: unknown, availability: 'implemented' | 'readout', reason?: TerminalVesselPromptOperation['reason']): value is TerminalVesselPromptOperation => record(value)
  && hasOnlyKeys(value, ['proximity', 'availability', 'reason'].filter(key => key !== 'reason' || Object.hasOwn(value, key)))
  && value.proximity === 'at-anchor'
  && value.availability === availability
  && (availability === 'implemented' ? !Object.hasOwn(value, 'reason') : value.reason === reason)

const validCurrentPromptEvidence = (value: unknown, sourceRecordId: string): boolean => record(value)
  && validateTerminalEvidence(value).length === 0
  && record(value.source)
  && value.source.kind === 'authoritative-record'
  && value.source.recordId === sourceRecordId
  && value.recordedAtWorldTime === value.knownAtWorldTime
  && record(value.freshness)
  && value.freshness.kind === 'current'

const promptWorldId = (id: string): string | undefined => id.startsWith('terminal-prompt:jomon-deck:')
  ? id.slice('terminal-prompt:jomon-deck:'.length)
  : undefined

const ledgerMemberSemanticMatches = (member: Record<string, unknown>): boolean => {
  if (!oneOf(TERMINAL_TAVERN_LEDGER_MEMBER_STATUSES, member.status)) return false
  const expected = ledgerPresentation(member.status)
  return member.paletteToken === expected.paletteToken
    && member.presentationState === expected.presentationState
    && same(member.nonColorCue, expected.nonColorCue)
}

const validLedgerMember = (value: unknown): value is TerminalTavernLedgerMember => {
  if (!record(value) || !hasOnlyKeys(value, ['id', 'name', 'role', 'conversation', 'status', 'paletteToken', 'presentationState', 'nonColorCue', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety']) || !validCourierIdentity(value) || !ledgerMemberSemanticMatches(value) || !validText(value.accessibilityText) || value.contentDomain !== 'person') return false
  const evidence = record(value.evidence) ? value.evidence : undefined
  const source = evidence && record(evidence.source) ? evidence.source : undefined
  const freshness = evidence && record(evidence.freshness) ? evidence.freshness : undefined
  if (validateTerminalEvidence(value.evidence).length || !source || !freshness || source.kind !== 'household-state' || source.recordId !== `person:${value.id}` || freshness.kind !== 'current') return false
  return true
}

const validLedgerContinuity = (value: unknown): value is TerminalTavernLedgerContinuity => {
  if (!record(value) || !hasOnlyKeys(value, ['kind', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety']) || value.kind !== 'continued-after-recorded-loss' || !validText(value.accessibilityText) || value.contentDomain !== 'event') return false
  const evidence = record(value.evidence) ? value.evidence : undefined
  const source = evidence && record(evidence.source) ? evidence.source : undefined
  const freshness = evidence && record(evidence.freshness) ? evidence.freshness : undefined
  if (validateTerminalEvidence(value.evidence).length || !source || !freshness || source.kind !== 'authoritative-record' || typeof source.recordId !== 'string' || !source.recordId.startsWith('causal-command:') || freshness.kind !== 'reported-at-world-time') return false
  return true
}

/** Structural boundary for the compact physical readout; world comparison stays at the full terminal boundary. */
export const validateTerminalTavernLedgerReadout = (value: unknown, includeContentSafety: boolean = true): readonly TerminalPresentationDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['id', 'members', 'continuity', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety'].filter(key => key !== 'continuity' || Object.hasOwn(value, key))) || value.id !== 'terminal-ledger:task-ledger' || !Array.isArray(value.members) || value.members.length !== INITIAL_HOUSEHOLD_ROSTER_SIZE || !validText(value.accessibilityText) || value.contentDomain !== 'player-facing-text') return [issue('terminal-ledger:task-ledger', 'terminal-presentation.invalid-prompt')]
  const diagnostics: TerminalPresentationDiagnostic[] = []
  const evidence = record(value.evidence) ? value.evidence : undefined
  const source = evidence && record(evidence.source) ? evidence.source : undefined
  const freshness = evidence && record(evidence.freshness) ? evidence.freshness : undefined
  if (validateTerminalEvidence(value.evidence).length || !source || !freshness || source.kind !== 'authoritative-record' || source.recordId !== 'deck-prop-binding:prop:task-ledger' || freshness.kind !== 'current') diagnostics.push(issue(value.id, 'terminal-presentation.invalid-prompt'))
  const members = value.members as unknown[]
  const names = new Set<string>()
  const roles = new Set<string>()
  for (const [index, candidate] of members.entries()) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : `terminal-ledger-member:${index}`
    if (!validLedgerMember(candidate) || !record(candidate) || candidate.id !== `crew:${index}`) {
      diagnostics.push(issue(id, 'terminal-presentation.invalid-prompt'))
      continue
    }
    if (names.has(candidate.name) || roles.has(candidate.role)) diagnostics.push(issue(id, 'terminal-presentation.invalid-prompt'))
    names.add(candidate.name)
    roles.add(candidate.role)
  }
  if (names.size !== INITIAL_HOUSEHOLD_ROSTER_SIZE || roles.size !== INITIAL_HOUSEHOLD_ROSTER_SIZE) diagnostics.push(issue(value.id, 'terminal-presentation.invalid-prompt'))
  if (Object.hasOwn(value, 'continuity') && !validLedgerContinuity(value.continuity)) diagnostics.push(issue(value.id, 'terminal-presentation.invalid-prompt'))
  const records: ClassifiedMedievalContent[] = [
    { id: 'terminal-ledger:task-ledger', domain: value.contentDomain, classification: value.contentSafety as MedievalContentSafetyClassification },
    ...members.filter(record).map(member => ({ id: `terminal-ledger-member:${typeof member.id === 'string' ? member.id : 'unknown'}`, domain: member.contentDomain as MedievalContentDomain, classification: member.contentSafety as MedievalContentSafetyClassification })),
    ...(validLedgerContinuity(value.continuity) ? [{ id: 'terminal-ledger-continuity', domain: value.continuity.contentDomain, classification: value.continuity.contentSafety }] : [])
  ]
  if (includeContentSafety) {
    const safety = auditMedievalContentSafety(records)
    if (safety.status === 'rejected') diagnostics.push(...safety.diagnostics.map(item => issue(item.contentId, item.code)))
  }
  return canonicalDiagnostics(diagnostics)
}

export const validateTerminalPrompt = (value: unknown, includeContentSafety: boolean = true): readonly TerminalPresentationDiagnostic[] => {
  const baseKeys = ['id', 'kind', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety', 'options', 'cancellation']
  const tavernKeys = [...baseKeys, 'source', 'operation', 'current', 'candidates', ...(record(value) && Object.hasOwn(value, 'ledger') ? ['ledger'] : [])]
  const stationKeys = [...baseKeys, 'label', 'source', 'operation', 'readout']
  if (!record(value) || !hasOnlyKeys(value, value.kind === 'tavern-courier-switch' ? tavernKeys : value.kind === 'vessel-station-readout' ? stationKeys : baseKeys) || !validId(value.id) || (value.kind !== 'future-contextual-choice' && value.kind !== 'vessel-station-readout' && value.kind !== 'tavern-courier-switch') || !validText(value.accessibilityText) || !Array.isArray(value.options) || value.options.length > TERMINAL_PRESENTATION_LIMITS.promptOptions || !value.options.every(validPromptOption) || !record(value.cancellation) || !hasOnlyKeys(value.cancellation, ['key', 'outcome', 'advancesWorldTime']) || value.cancellation.key !== 'Escape' || value.cancellation.outcome !== 'cancelled-no-mutation' || value.cancellation.advancesWorldTime !== false) return [issue('terminal-prompt', 'terminal-presentation.invalid-prompt')]
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
  if (value.kind === 'vessel-station-readout') {
    const readout = validateVesselStationReadout(value.readout) ? value.readout : undefined
    const worldId = promptWorldId(value.id)
    const option = options[0]
    if (!readout || !same(value.source, readout.source) || value.label !== readout.label || !validVesselPromptOperation(value.operation, 'readout', readout.reason) || !validCurrentPromptEvidence(value.evidence, readout.factSourceId) || worldId === undefined || options.length !== 1 || !option || option.id !== `terminal-prompt-option:${readout.source.propId}:${worldId}` || option.key !== 'Enter' || option.availability !== 'available' || option.disabledReason !== undefined || option.intent !== 'vessel-station-readout-record' || option.requiresConfirmation !== true || !validCue(option.nonColorCue, 'ready')) diagnostics.push(issue(value.id, 'terminal-presentation.invalid-prompt'))
  }
  if (value.kind === 'tavern-courier-switch') {
    const candidates = Array.isArray(value.candidates) && value.candidates.every(validCourierSwitchCandidate)
      ? value.candidates as TerminalCourierSwitchCandidate[]
      : undefined
    const current = validCourierSwitchCandidate(value.current) ? value.current : undefined
    const ledger = Object.hasOwn(value, 'ledger') ? value.ledger : undefined
    const ledgerDiagnostics = ledger === undefined ? [] : validateTerminalTavernLedgerReadout(ledger, includeContentSafety)
    const worldId = promptWorldId(value.id)
    if (!validTavernSource(value.source) || !validVesselPromptOperation(value.operation, 'implemented') || !current || !candidates || candidates.length > TERMINAL_PRESENTATION_LIMITS.promptOptions || candidates.some(candidate => candidate.id === current.id) || new Set(candidates.map(candidate => candidate.id)).size !== candidates.length || options.length !== 1 || options[0]!.id !== `terminal-prompt-option:tavern-ledger:${worldId}` || options[0]!.intent !== 'tavern-courier-switch' || (options[0]!.availability === 'available' && (!options[0]!.requiresConfirmation || candidates.length === 0)) || (options[0]!.availability === 'disabled' && options[0]!.requiresConfirmation) || !validCurrentPromptEvidence(value.evidence, (value.source as TavernCourierSwitchSource).propBindingId)) diagnostics.push(issue(value.id, 'terminal-presentation.invalid-prompt'))
    diagnostics.push(...ledgerDiagnostics)
    if (ledger !== undefined && !ledgerDiagnostics.length && current && candidates) {
      const members = (ledger as TerminalTavernLedgerReadout).members
      const currentMember = members.find(member => member.id === current.id)
      const candidateIndexes = candidates.map(candidate => members.findIndex(member => member.id === candidate.id))
      if (!currentMember || !same(promptCandidate(currentMember), current) || !currentMember.status.startsWith('active-') || candidateIndexes.some(index => index < 0) || candidateIndexes.some((index, itemIndex) => itemIndex > 0 && candidateIndexes[itemIndex - 1]! >= index) || candidates.some((candidate, index) => {
        const member = members[candidateIndexes[index]!]
        return !member || member.status !== 'available' || !same(promptCandidate(member), candidate)
      })) diagnostics.push(issue(value.id, 'terminal-presentation.invalid-prompt'))
    }
  }
  if (options.some((option, index) => index > 0 && compare(options[index - 1]!.id, option.id) >= 0)) diagnostics.push(issue(value.id, 'terminal-presentation.invalid-prompt'))
  if (includeContentSafety) {
    const content = auditMedievalContentSafety([{ id: `terminal-prompt:${value.id}`, domain: value.contentDomain as MedievalContentDomain, classification: value.contentSafety as MedievalContentSafetyClassification }])
    if (content.status === 'rejected') diagnostics.push(...content.diagnostics.map(item => issue(item.contentId, item.code)))
  }
  return canonicalDiagnostics(diagnostics)
}

/** Cancellation is an explicit renderer intent, never a world transition. */
export const cancelTerminalPrompt = (prompt: TerminalPrompt): TerminalPromptCancellation => {
  const diagnostics = validateTerminalPrompt(prompt)
  if (diagnostics.length) throw new TerminalPresentationContractError(diagnostics)
  return { id: `terminal-prompt-cancel:${prompt.id}`, promptId: prompt.id, outcome: 'cancelled-no-mutation', advancesWorldTime: false }
}

const terminalRoutes = ['worlds', 'create-world', 'creation-profiles', 'world-generation', 'world-result', 'choose-courier', 'world', 'chronicles', 'chronicle'] as const satisfies readonly MedievalRoute[]
const terminalInputFocuses = ['list', 'seed-entry', 'preset', 'settings-link', 'advanced-setting', 'profile-name-entry', 'profile-save-action', 'profile-load-action', 'result', 'management', 'contextual-prompt', 'command-help', 'controls-editor', 'controls-capture', 'chronicle', 'future-prompt'] as const satisfies readonly TerminalInputMode['focus'][]
const terminalInputSurfaces = ['navigation', 'management', 'settings', 'text-entry', 'export', 'movement', 'contextual-action', 'remapping', 'help'] as const satisfies readonly TerminalInputCommandSurface[]

const textEntrySpecification = (field: TerminalTextInputField): TerminalBoundedTextBinding => field === 'creation-seed'
  ? textEntry('creation-seed', 64)
  : textEntry('creation-profile-name', 32)
const bindingIdentity = (binding: TerminalKeyboardBinding): string => binding.kind === 'key' ? `key:${binding.key}` : binding.kind === 'bounded-text-entry' ? `text:${binding.field}` : 'capture'
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
const validKeyCaptureBinding = (value: unknown): value is TerminalKeyCaptureBinding => record(value)
  && hasOnlyKeys(value, ['kind', 'maximumKeys', 'cancellationKey', 'disallowsModifierChords'])
  && value.kind === 'single-key-capture'
  && value.maximumKeys === 1
  && value.cancellationKey === 'Escape'
  && value.disallowsModifierChords === true
const validKeyboardBinding = (value: unknown): value is TerminalKeyboardBinding => validKeyBinding(value) || validTextEntryBinding(value) || validKeyCaptureBinding(value)

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
    const captureBindings = bindings.filter(binding => record(binding) && binding.kind === 'single-key-capture') as TerminalKeyCaptureBinding[]
    if (captureBindings.length > 0 && (bindings.length !== 1 || contexts.length !== 1 || contexts[0] !== 'world-controls-capture')) diagnostics.push(issue(id, 'terminal-presentation.invalid-input-binding'))
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

/**
 * Validates a renderer-neutral presentation model without receiving world
 * state. This is deliberately structural and safety-focused: adapters may
 * consume this projection, but only world-facing creation validates it
 * against a FoundationWorld through `validateTerminalPresentationModel`.
 */
export const validateTerminalPresentationProjection = (
  value: unknown,
  glyphCatalog: TerminalGlyphCatalog
): readonly TerminalPresentationDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'worldId', 'map', 'legend', 'status', 'messages', 'prompts', 'input', 'accessibility', 'sidebarBoundary', 'rendererParity', 'contentSafetyAudit'])) {
    return [issue('terminal-presentation:projection', 'terminal-presentation.malformed-model')]
  }
  const diagnostics: TerminalPresentationDiagnostic[] = []
  if (value.version !== TERMINAL_PRESENTATION_CONTRACT_VERSION || !validId(value.worldId)) diagnostics.push(issue('terminal-presentation:projection', 'terminal-presentation.invalid-model'))

  const map = value.map
  if (!record(map) || !hasOnlyKeys(map, ['state', 'viewport', 'camera', 'cells', 'textEquivalent', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety']) || !validText(map.textEquivalent) || !validText(map.accessibilityText) || validateTerminalEvidence(map.evidence).length !== 0) {
    diagnostics.push(issue('terminal-presentation:map', 'terminal-presentation.invalid-model'))
  } else if (map.state === 'materialized') {
    if (!record(map.viewport) || ((map.viewport as { context?: unknown }).context !== 'jomon-deck-plan' && (map.viewport as { context?: unknown }).context !== 'future-materialized')) diagnostics.push(issue('terminal-presentation:map', 'terminal-presentation.invalid-model'))
    else {
      const viewport = map.viewport as unknown as TerminalMapViewport
      if (!validCamera(map.camera, viewport)) diagnostics.push(issue('terminal-presentation:map', 'terminal-presentation.invalid-model'))
      diagnostics.push(...validateTerminalMaterializedCells(viewport, map.cells, glyphCatalog))
    }
  } else diagnostics.push(issue('terminal-presentation:map', 'terminal-presentation.invalid-model'))
  if (record(map)) {
    const safety = auditMedievalContentSafety([{ id: 'terminal-presentation:map', domain: map.contentDomain as MedievalContentDomain, classification: map.contentSafety as MedievalContentSafetyClassification }])
    if (safety.status === 'rejected') diagnostics.push(...safety.diagnostics.map(item => issue(item.contentId, item.code)))
  }

  const legendDiagnostics = !record(map) || map.state !== 'materialized'
    ? [issue('terminal-map-legend', 'terminal-presentation.invalid-model')]
    : validateTerminalMapLegend(map as unknown as TerminalMaterializedMap, value.legend)
  diagnostics.push(...legendDiagnostics)

  const status = Array.isArray(value.status) ? value.status : []
  if (!Array.isArray(value.status) || status.length > TERMINAL_PRESENTATION_LIMITS.statusItems) diagnostics.push(issue('terminal-presentation:status', 'terminal-presentation.invalid-model'))
  const statusIds = new Set<string>()
  for (const candidate of status) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'terminal-status'
    const statusValue = record(candidate) && record(candidate.value) ? candidate.value : undefined
    const focusIsValid = statusValue?.kind !== 'deck-focus' || (hasOnlyKeys(statusValue, statusValue.coordinate === undefined ? ['kind'] : ['kind', 'courierId', 'coordinate']) && (statusValue.coordinate === undefined || (typeof statusValue.courierId === 'string' && validId(statusValue.courierId) && record(statusValue.coordinate) && hasOnlyKeys(statusValue.coordinate, ['column', 'row']) && safeInteger(statusValue.coordinate.column) && safeInteger(statusValue.coordinate.row))))
    const provenanceIsValid = statusValue?.kind !== 'creation-provenance' || (hasOnlyKeys(statusValue, ['kind', 'seed', 'digest']) && typeof statusValue.seed === 'string' && statusValue.seed.length > 0 && typeof statusValue.digest === 'string' && statusValue.digest.length > 0)
    const vesselActionIsValid = statusValue?.kind !== 'vessel-prop-action' || (hasOnlyKeys(statusValue, ['kind', 'propId', 'action', 'recordedAtWorldTime', 'causalSequence']) && typeof statusValue.propId === 'string' && (statusValue.action === 'station-readout-recorded' || statusValue.action === 'tavern-courier-switched') && safeInteger(statusValue.recordedAtWorldTime) && safeInteger(statusValue.causalSequence) && statusValue.causalSequence > 0)
  if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'state', 'paletteToken', 'nonColorCue', 'value', 'accessibilityText', 'evidence', 'contentDomain', 'contentSafety']) || !validId(candidate.id) || !oneOf(TERMINAL_PRESENTATION_STATES, candidate.state) || !paletteToken(candidate.paletteToken) || candidate.paletteToken !== TERMINAL_STATE_PRESENTATIONS[candidate.state].paletteToken || !validCue(candidate.nonColorCue, candidate.state) || !statusValue || !hasOnlyKeys(statusValue, statusValue.kind === 'courier-selection' ? ['kind', 'state'] : statusValue.kind === 'world-minute' ? ['kind', 'minutes'] : statusValue.kind === 'jomon-deck-materialized' ? ['kind', 'cells'] : statusValue.kind === 'deck-focus' ? statusValue.coordinate === undefined ? ['kind'] : ['kind', 'courierId', 'coordinate'] : statusValue.kind === 'creation-provenance' ? ['kind', 'seed', 'digest'] : statusValue.kind === 'vessel-prop-action' ? ['kind', 'propId', 'action', 'recordedAtWorldTime', 'causalSequence'] : ['kind']) || !oneOf(['jomon-deck-materialized', 'courier-selection', 'world-minute', 'deck-focus', 'creation-provenance', 'vessel-prop-action'] as const, statusValue.kind) || (statusValue.kind === 'jomon-deck-materialized' && (!safeInteger(statusValue.cells) || statusValue.cells === 0)) || (statusValue.kind === 'courier-selection' && !oneOf(['awaiting-initial', 'active', 'crew-extinct'] as const, statusValue.state)) || (statusValue.kind === 'world-minute' && !safeInteger(statusValue.minutes)) || !focusIsValid || !provenanceIsValid || !vesselActionIsValid || !validText(candidate.accessibilityText) || validateTerminalEvidence(candidate.evidence).length) diagnostics.push(issue(id, 'terminal-presentation.invalid-model'))
    if (statusIds.has(id)) diagnostics.push(issue(id, 'terminal-presentation.invalid-model'))
    statusIds.add(id)
  }
  const typedStatus = status.filter(record) as unknown as TerminalStatusItem[]
  if (typedStatus.some((item, index) => index > 0 && compare(typedStatus[index - 1]!.id, item.id) >= 0)) diagnostics.push(issue('terminal-presentation:status', 'terminal-presentation.invalid-model'))
  const statusSafety = auditMedievalContentSafety(typedStatus.map(item => ({ id: `terminal-presentation:status:${item.id}`, domain: item.contentDomain, classification: item.contentSafety })))
  if (statusSafety.status === 'rejected') diagnostics.push(...statusSafety.diagnostics.map(item => issue(item.contentId, item.code)))

  diagnostics.push(...validateTerminalMessages(value.messages))
  if (!Array.isArray(value.prompts) || value.prompts.length > TERMINAL_PRESENTATION_LIMITS.prompts) diagnostics.push(issue('terminal-presentation:prompts', 'terminal-presentation.invalid-prompt'))
  else {
    const promptIds = new Set<string>()
    for (const prompt of value.prompts) {
      // The complete projection audit below covers prompts and every ledger
      // member, so this pass keeps strict shape/evidence checks without
      // repeating the same content-safety audit for each adapter validation.
      diagnostics.push(...validateTerminalPrompt(prompt, false))
      const id = record(prompt) && typeof prompt.id === 'string' ? prompt.id : 'terminal-prompt'
      if (promptIds.has(id)) diagnostics.push(issue(id, 'terminal-presentation.invalid-prompt'))
      promptIds.add(id)
    }
    const prompts = value.prompts.filter(record) as unknown as TerminalPrompt[]
    if (prompts.some((prompt, index) => index > 0 && compare(prompts[index - 1]!.id, prompt.id) >= 0)) diagnostics.push(issue('terminal-presentation:prompts', 'terminal-presentation.invalid-prompt'))
  }

  if (!record(value.input) || !hasOnlyKeys(value.input, ['canvasFocus', 'modes', 'worldBindingPreferences', 'commands']) || !same(value.input.canvasFocus, { keyboardFirst: true, pointerFocusAssist: true }) || !same(value.input.modes, TERMINAL_INPUT_MODES) || !same(value.input.worldBindingPreferences, { contract: 'terminal-controls-v1', scope: 'browser-ui-only', remappableCommands: 'world-movement-context-management-help', protectedCancellation: 'Escape', protectedEditorEntry: 'F2' }) || !same(value.input.commands, TERMINAL_KEYBOARD_COMMANDS)) diagnostics.push(issue('terminal-presentation:input', 'terminal-presentation.invalid-input-binding'))
  else {
    diagnostics.push(...validateTerminalInputModes(value.input.modes))
    diagnostics.push(...validateTerminalKeyboardCommands(value.input.commands))
  }
  if (!same(value.sidebarBoundary, { relationship: 'separate-household-known-strategic-surface', duplicatedStrategicFactCategories: [] }) || !same(value.rendererParity, { asciiCanvas: 'current-adapter', detailedRenderer: 'future-adapter', requirements: TERMINAL_RENDERER_PARITY_RULES })) diagnostics.push(issue('terminal-presentation:parity', 'terminal-presentation.invalid-model'))

  if (record(map) && legendDiagnostics.length === 0 && Array.isArray(value.status) && Array.isArray(value.messages) && Array.isArray(value.prompts)) {
    const legend = value.legend as unknown as TerminalMapLegend
    const expectedAccessibility = accessibilityFor(map as unknown as TerminalMapSurface, legend, typedStatus, value.messages as TerminalMessage[], value.prompts as TerminalPrompt[])
    if (!same(value.accessibility, expectedAccessibility)) diagnostics.push(issue('terminal-presentation:accessibility', 'terminal-presentation.invalid-model'))
    const contentRecords = modelContentRecords(map as unknown as TerminalMapSurface, legend, typedStatus, value.messages as TerminalMessage[], value.prompts as TerminalPrompt[])
    if (!contentSafetyAuditMatches(contentRecords, value.contentSafetyAudit)) diagnostics.push(issue('terminal-presentation:audit', 'terminal-presentation.invalid-content-audit'))
  } else diagnostics.push(issue('terminal-presentation:accessibility', 'terminal-presentation.invalid-model'))
  return canonicalDiagnostics(diagnostics)
}

/** A type-level reminder that current route metadata is renderer input, not world authority. */
export type TerminalPresentationRoute = Extract<MedievalRoute, 'world' | 'worlds' | 'create-world' | 'world-result' | 'choose-courier'>
