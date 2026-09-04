/**
 * Versioned browser-UI controls for the current visible static deck map. These
 * preferences are deliberately outside FoundationWorld and have no world-time,
 * causal-history, or gameplay authority.
 */
export const TERMINAL_CONTROLS_VERSION = 1 as const

export const TERMINAL_CONTROLS_LIMITS = {
  bindings: 13,
  editorRows: 13
} as const

export const TERMINAL_MOVEMENT_DIRECTIONS = [
  'north-west', 'north', 'north-east', 'west', 'east', 'south-west', 'south', 'south-east'
] as const
export type TerminalMovementDirection = typeof TERMINAL_MOVEMENT_DIRECTIONS[number]

export const TERMINAL_CONTROL_IDS = [
  'command-help',
  'contextual-prompt',
  'management-next-section',
  'management-previous-section',
  'management-toggle',
  'move-east',
  'move-north',
  'move-north-east',
  'move-north-west',
  'move-south',
  'move-south-east',
  'move-south-west',
  'move-west'
] as const
export type TerminalControlId = typeof TERMINAL_CONTROL_IDS[number]

/** Capture accepts only unmodified browser `KeyboardEvent.key` values in this closed set. */
export const TERMINAL_CONTROL_KEYS = [
  'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter',
  'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
  'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', ']', '?'
] as const
export type TerminalControlKey = typeof TERMINAL_CONTROL_KEYS[number]

export interface TerminalControlDefinition {
  id: TerminalControlId
  label: string
  direction?: TerminalMovementDirection
  operationalState: 'movement-available' | 'opens-contextual-prompt' | 'operational-ui'
}

export const TERMINAL_CONTROL_DEFINITIONS: readonly TerminalControlDefinition[] = [
  { id: 'command-help', label: 'Command help', operationalState: 'operational-ui' },
  { id: 'contextual-prompt', label: 'Context prompt', operationalState: 'opens-contextual-prompt' },
  { id: 'management-next-section', label: 'Next management section', operationalState: 'operational-ui' },
  { id: 'management-previous-section', label: 'Previous management section', operationalState: 'operational-ui' },
  { id: 'management-toggle', label: 'Toggle management', operationalState: 'operational-ui' },
  { id: 'move-east', label: 'Move east', direction: 'east', operationalState: 'movement-available' },
  { id: 'move-north', label: 'Move north', direction: 'north', operationalState: 'movement-available' },
  { id: 'move-north-east', label: 'Move north east', direction: 'north-east', operationalState: 'movement-available' },
  { id: 'move-north-west', label: 'Move north west', direction: 'north-west', operationalState: 'movement-available' },
  { id: 'move-south', label: 'Move south', direction: 'south', operationalState: 'movement-available' },
  { id: 'move-south-east', label: 'Move south east', direction: 'south-east', operationalState: 'movement-available' },
  { id: 'move-south-west', label: 'Move south west', direction: 'south-west', operationalState: 'movement-available' },
  { id: 'move-west', label: 'Move west', direction: 'west', operationalState: 'movement-available' }
]

export interface TerminalControlBinding {
  controlId: TerminalControlId
  key: TerminalControlKey
}

export interface TerminalControlPreferences {
  version: typeof TERMINAL_CONTROLS_VERSION
  bindings: readonly TerminalControlBinding[]
}

/** Arrow aliases are accessible fixed fallbacks; they are not stored preferences. */
export const TERMINAL_FIXED_MOVEMENT_ALIASES: readonly TerminalControlBinding[] = [
  { controlId: 'move-east', key: 'ArrowRight' },
  { controlId: 'move-north', key: 'ArrowUp' },
  { controlId: 'move-south', key: 'ArrowDown' },
  { controlId: 'move-west', key: 'ArrowLeft' }
]

const DEFAULT_BINDING_BY_CONTROL: Readonly<Record<TerminalControlId, TerminalControlKey>> = {
  'command-help': '?',
  'contextual-prompt': 'Enter',
  'management-next-section': ']',
  'management-previous-section': '[',
  'management-toggle': 'M',
  'move-east': 'L',
  'move-north': 'K',
  'move-north-east': 'U',
  'move-north-west': 'Y',
  'move-south': 'J',
  'move-south-east': 'N',
  'move-south-west': 'B',
  'move-west': 'H'
}

export type TerminalControlsDiagnosticCode =
  | 'terminal-controls.malformed-preferences'
  | 'terminal-controls.unknown-version'
  | 'terminal-controls.invalid-binding'
  | 'terminal-controls.duplicate-control'
  | 'terminal-controls.noncanonical-binding-order'
  | 'terminal-controls.binding-conflict'
  | 'terminal-controls.unsupported-key'
  | 'terminal-controls.modifier-chord-rejected'
  | 'terminal-controls.protected-key'
  | 'terminal-controls.unknown-control'

export interface TerminalControlsDiagnostic {
  controlId: string
  code: TerminalControlsDiagnosticCode
}

export class TerminalControlsError extends Error {
  constructor(readonly diagnostics: readonly TerminalControlsDiagnostic[]) {
    super(`terminal controls rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'TerminalControlsError'
  }
}

export interface TerminalKeyCaptureInput {
  key: string
  ctrlKey?: boolean
  altKey?: boolean
  metaKey?: boolean
}

export type TerminalCaptureResult =
  | { status: 'accepted'; preferences: TerminalControlPreferences; controlId: TerminalControlId; key: TerminalControlKey }
  | { status: 'cancelled'; controlId: TerminalControlId }
  | { status: 'rejected'; controlId: TerminalControlId; code: Extract<TerminalControlsDiagnosticCode, 'terminal-controls.unsupported-key' | 'terminal-controls.modifier-chord-rejected' | 'terminal-controls.protected-key' | 'terminal-controls.binding-conflict'> }

export type TerminalWorldInteractionContext = 'world' | 'contextual-prompt' | 'command-help' | 'controls-editor' | 'controls-key-capture'

export interface TerminalWorldKeyInput extends TerminalKeyCaptureInput {
  canvasFocused: boolean
  context: TerminalWorldInteractionContext
}

export type TerminalWorldCommand =
  | { kind: 'ignored'; reason: 'canvas-not-focused' | 'modified-key' | 'unbound-key' }
  | { kind: 'return-to-worlds' }
  | { kind: 'cancel-overlay'; overlay: Exclude<TerminalWorldInteractionContext, 'world'> }
  | { kind: 'capture-control-key' }
  | { kind: 'controls-select'; direction: -1 | 1 }
  | { kind: 'controls-begin-capture' }
  | { kind: 'controls-reset-current' }
  | { kind: 'controls-reset-all' }
  | { kind: 'open-controls-editor' }
  | { kind: 'open-command-help' }
  | { kind: 'open-contextual-prompt' }
  | { kind: 'prompt-select'; direction: -1 | 1 }
  | { kind: 'prompt-confirm' }
  | { kind: 'toggle-management' }
  | { kind: 'previous-management-section' }
  | { kind: 'next-management-section' }
  | { kind: 'move-courier'; direction: TerminalMovementDirection }

export interface TerminalHelpEntry {
  controlId: TerminalControlId
  label: string
  bindingText: string
  operationalState: TerminalControlDefinition['operationalState']
  accessibilityText: string
}

export interface TerminalCommandHelpModel {
  entries: readonly TerminalHelpEntry[]
  accessibilitySummary: string
}

export interface TerminalControlsEditorEntry {
  controlId: TerminalControlId
  label: string
  key: TerminalControlKey
  fixedAliases: readonly TerminalControlKey[]
  selected: boolean
  accessibilityText: string
}

export interface TerminalControlsEditorModel {
  entries: readonly TerminalControlsEditorEntry[]
  selectedControlId: TerminalControlId
  capturePending: boolean
  accessibilitySummary: string
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort(compare)
  const expectedKeys = [...expected].sort(compare)
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const diagnostic = (controlId: string, code: TerminalControlsDiagnosticCode): TerminalControlsDiagnostic => ({ controlId, code })
const canonicalDiagnostics = (diagnostics: readonly TerminalControlsDiagnostic[]): readonly TerminalControlsDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.controlId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.controlId, right.controlId) || compare(left.code, right.code))
const clone = <Value>(value: Value): Value => structuredClone(value)

const canonicalKey = (key: string): TerminalControlKey | undefined => {
  const candidate = key.length === 1 && /^[a-z]$/iu.test(key) ? key.toUpperCase() : key
  return oneOf(TERMINAL_CONTROL_KEYS, candidate) ? candidate : undefined
}
const hasModifier = (input: TerminalKeyCaptureInput): boolean => input.ctrlKey === true || input.altKey === true || input.metaKey === true
const canonicalBindings = (bindings: readonly TerminalControlBinding[]): readonly TerminalControlBinding[] => [...bindings]
  .map(binding => ({ controlId: binding.controlId, key: binding.key }))
  .sort((left, right) => compare(left.controlId, right.controlId))

export const defaultTerminalControlPreferences = (): TerminalControlPreferences => ({
  version: TERMINAL_CONTROLS_VERSION,
  bindings: canonicalBindings(TERMINAL_CONTROL_IDS.map(controlId => ({ controlId, key: DEFAULT_BINDING_BY_CONTROL[controlId] })))
})

const validBinding = (value: unknown): value is TerminalControlBinding => record(value)
  && hasOnlyKeys(value, ['controlId', 'key'])
  && oneOf(TERMINAL_CONTROL_IDS, value.controlId)
  && oneOf(TERMINAL_CONTROL_KEYS, value.key)

/** Exact preference validation; saved malformed data is never repaired in place. */
export const validateTerminalControlPreferences = (value: unknown): readonly TerminalControlsDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'bindings']) || !Array.isArray(value.bindings) || value.bindings.length !== TERMINAL_CONTROLS_LIMITS.bindings) return [diagnostic('terminal-controls', 'terminal-controls.malformed-preferences')]
  const diagnostics: TerminalControlsDiagnostic[] = []
  if (value.version !== TERMINAL_CONTROLS_VERSION) diagnostics.push(diagnostic('terminal-controls', 'terminal-controls.unknown-version'))
  const controlIds = new Set<string>()
  const effectiveKeys = new Map<string, string>()
  const bindings = value.bindings
  for (const candidate of bindings) {
    const id = record(candidate) && typeof candidate.controlId === 'string' ? candidate.controlId : 'terminal-control'
    if (!validBinding(candidate)) {
      diagnostics.push(diagnostic(id, oneOf(TERMINAL_CONTROL_IDS, record(candidate) ? candidate.controlId : undefined) ? 'terminal-controls.invalid-binding' : 'terminal-controls.unknown-control'))
      continue
    }
    if (controlIds.has(candidate.controlId)) diagnostics.push(diagnostic(candidate.controlId, 'terminal-controls.duplicate-control'))
    controlIds.add(candidate.controlId)
    const existing = effectiveKeys.get(candidate.key)
    if (existing !== undefined && existing !== candidate.controlId) diagnostics.push(diagnostic(candidate.controlId, 'terminal-controls.binding-conflict'))
    effectiveKeys.set(candidate.key, candidate.controlId)
  }
  for (const alias of TERMINAL_FIXED_MOVEMENT_ALIASES) {
    const existing = effectiveKeys.get(alias.key)
    if (existing !== undefined && existing !== alias.controlId) diagnostics.push(diagnostic(existing, 'terminal-controls.binding-conflict'))
    effectiveKeys.set(alias.key, alias.controlId)
  }
  if (bindings.some((binding, index) => index > 0 && validBinding(binding) && validBinding(bindings[index - 1]) && compare((bindings[index - 1] as TerminalControlBinding).controlId, binding.controlId) >= 0)) diagnostics.push(diagnostic('terminal-controls', 'terminal-controls.noncanonical-binding-order'))
  if (!TERMINAL_CONTROL_IDS.every(id => controlIds.has(id))) diagnostics.push(diagnostic('terminal-controls', 'terminal-controls.malformed-preferences'))
  return canonicalDiagnostics(diagnostics)
}

export const isTerminalControlPreferences = (value: unknown): value is TerminalControlPreferences => validateTerminalControlPreferences(value).length === 0

const validatedPreferences = (preferences: TerminalControlPreferences): TerminalControlPreferences => {
  const diagnostics = validateTerminalControlPreferences(preferences)
  if (diagnostics.length) throw new TerminalControlsError(diagnostics)
  return clone(preferences)
}

const controlForKey = (preferences: TerminalControlPreferences, key: TerminalControlKey): TerminalControlId | undefined => {
  const direct = preferences.bindings.find(binding => binding.key === key)?.controlId
  return direct ?? TERMINAL_FIXED_MOVEMENT_ALIASES.find(binding => binding.key === key)?.controlId
}

const definitionFor = (controlId: TerminalControlId): TerminalControlDefinition => {
  const definition = TERMINAL_CONTROL_DEFINITIONS.find(candidate => candidate.id === controlId)
  if (!definition) throw new TerminalControlsError([diagnostic(controlId, 'terminal-controls.unknown-control')])
  return definition
}

/** A bounded key capture. Escape only cancels capture; it can never be rebound. */
export const captureTerminalControlBinding = (preferences: TerminalControlPreferences, controlId: TerminalControlId, input: TerminalKeyCaptureInput): TerminalCaptureResult => {
  const current = validatedPreferences(preferences)
  if (hasModifier(input)) return { status: 'rejected', controlId, code: 'terminal-controls.modifier-chord-rejected' }
  if (input.key === 'Escape') return { status: 'cancelled', controlId }
  if (input.key === 'F2') return { status: 'rejected', controlId, code: 'terminal-controls.protected-key' }
  const key = canonicalKey(input.key)
  if (!key) return { status: 'rejected', controlId, code: 'terminal-controls.unsupported-key' }
  const candidate: TerminalControlPreferences = {
    version: TERMINAL_CONTROLS_VERSION,
    bindings: canonicalBindings(current.bindings.map(binding => binding.controlId === controlId ? { controlId, key } : binding))
  }
  const diagnostics = validateTerminalControlPreferences(candidate)
  if (diagnostics.length) return { status: 'rejected', controlId, code: diagnostics.some(item => item.code === 'terminal-controls.binding-conflict') ? 'terminal-controls.binding-conflict' : 'terminal-controls.unsupported-key' }
  return { status: 'accepted', preferences: candidate, controlId, key }
}

export const resetTerminalControl = (preferences: TerminalControlPreferences, controlId: TerminalControlId): TerminalControlPreferences => {
  const current = validatedPreferences(preferences)
  return {
    version: TERMINAL_CONTROLS_VERSION,
    bindings: canonicalBindings(current.bindings.map(binding => binding.controlId === controlId ? { controlId, key: DEFAULT_BINDING_BY_CONTROL[controlId] } : binding))
  }
}

export const resetAllTerminalControls = (preferences: TerminalControlPreferences): TerminalControlPreferences => {
  validatedPreferences(preferences)
  return defaultTerminalControlPreferences()
}

export const cycleTerminalControlSelection = (selected: TerminalControlId, direction: -1 | 1): TerminalControlId => {
  const index = TERMINAL_CONTROL_IDS.indexOf(selected)
  return TERMINAL_CONTROL_IDS[(index + direction + TERMINAL_CONTROL_IDS.length) % TERMINAL_CONTROL_IDS.length]!
}

/** One deterministic resolver for all current world-view keyboard intent. */
export const resolveTerminalWorldCommand = (preferences: TerminalControlPreferences, input: TerminalWorldKeyInput): TerminalWorldCommand => {
  validatedPreferences(preferences)
  if (!input.canvasFocused) return { kind: 'ignored', reason: 'canvas-not-focused' }
  if (hasModifier(input)) return { kind: 'ignored', reason: 'modified-key' }
  if (input.context === 'controls-key-capture') return { kind: 'capture-control-key' }
  if (input.key === 'Escape') return input.context === 'world'
    ? { kind: 'return-to-worlds' }
    : { kind: 'cancel-overlay', overlay: input.context }
  if (input.context === 'contextual-prompt') {
    if (input.key === 'ArrowUp') return { kind: 'prompt-select', direction: -1 }
    if (input.key === 'ArrowDown') return { kind: 'prompt-select', direction: 1 }
    if (input.key === 'Enter') return { kind: 'prompt-confirm' }
    return { kind: 'ignored', reason: 'unbound-key' }
  }
  if (input.context === 'command-help') return { kind: 'ignored', reason: 'unbound-key' }
  if (input.context === 'controls-editor') {
    if (input.key === 'ArrowUp') return { kind: 'controls-select', direction: -1 }
    if (input.key === 'ArrowDown') return { kind: 'controls-select', direction: 1 }
    if (input.key === 'Enter') return { kind: 'controls-begin-capture' }
    if (input.key === 'R' || input.key === 'r') return { kind: 'controls-reset-current' }
    if (input.key === 'A' || input.key === 'a') return { kind: 'controls-reset-all' }
    return { kind: 'ignored', reason: 'unbound-key' }
  }
  if (input.key === 'F2') return { kind: 'open-controls-editor' }
  const key = canonicalKey(input.key)
  if (!key) return { kind: 'ignored', reason: 'unbound-key' }
  const controlId = controlForKey(preferences, key)
  if (!controlId) return { kind: 'ignored', reason: 'unbound-key' }
  const definition = definitionFor(controlId)
  if (definition.operationalState === 'movement-available') return { kind: 'move-courier', direction: definition.direction! }
  switch (controlId) {
    case 'contextual-prompt': return { kind: 'open-contextual-prompt' }
    case 'command-help': return { kind: 'open-command-help' }
    case 'management-toggle': return { kind: 'toggle-management' }
    case 'management-previous-section': return { kind: 'previous-management-section' }
    case 'management-next-section': return { kind: 'next-management-section' }
    default: return { kind: 'ignored', reason: 'unbound-key' }
  }
}

const bindingFor = (preferences: TerminalControlPreferences, controlId: TerminalControlId): TerminalControlKey => {
  const binding = preferences.bindings.find(candidate => candidate.controlId === controlId)
  if (!binding) throw new TerminalControlsError([diagnostic(controlId, 'terminal-controls.malformed-preferences')])
  return binding.key
}

export const createTerminalCommandHelpModel = (preferences: TerminalControlPreferences): TerminalCommandHelpModel => {
  const current = validatedPreferences(preferences)
  const entries = TERMINAL_CONTROL_DEFINITIONS.map(definition => {
    const key = bindingFor(current, definition.id)
    const alias = TERMINAL_FIXED_MOVEMENT_ALIASES.find(item => item.controlId === definition.id)?.key
    const bindingText = alias === undefined ? key : `${key} / ${alias}`
    const state = definition.operationalState === 'movement-available'
      ? 'Attempts one local deck step. A successful step advances one action minute; a blocked step changes no world state or time.'
      : definition.operationalState === 'opens-contextual-prompt'
        ? 'Opens the contextual prompt. At the tavern task ledger it can switch the active courier; elsewhere it reports why switching is unavailable.'
        : 'Operational browser UI control.'
    return { controlId: definition.id, label: definition.label, bindingText, operationalState: definition.operationalState, accessibilityText: `${definition.label}: ${bindingText}. ${state}` }
  })
  return { entries, accessibilitySummary: `Command help. ${entries.length} remappable world controls. The static Jomon deck is visible; movement is local and collision-checked. The contextual control opens the source-backed tavern ledger courier-switch prompt when available.` }
}

export const createTerminalControlsEditorModel = (preferences: TerminalControlPreferences, selectedControlId: TerminalControlId, capturePending: boolean): TerminalControlsEditorModel => {
  const current = validatedPreferences(preferences)
  if (!oneOf(TERMINAL_CONTROL_IDS, selectedControlId)) throw new TerminalControlsError([diagnostic(String(selectedControlId), 'terminal-controls.unknown-control')])
  const entries = TERMINAL_CONTROL_DEFINITIONS.map(definition => {
    const fixedAliases = TERMINAL_FIXED_MOVEMENT_ALIASES.filter(item => item.controlId === definition.id).map(item => item.key)
    return {
      controlId: definition.id,
      label: definition.label,
      key: bindingFor(current, definition.id),
      fixedAliases,
      selected: definition.id === selectedControlId,
      accessibilityText: `${definition.label}: ${bindingFor(current, definition.id)}${fixedAliases.length ? `; fixed alternate ${fixedAliases.join(', ')}` : ''}.`
    }
  })
  return {
    entries,
    selectedControlId,
    capturePending,
    accessibilitySummary: capturePending
      ? `Controls editor. Capturing one replacement key for ${definitionFor(selectedControlId).label}. Escape cancels capture without changing a binding.`
      : `Controls editor. ${definitionFor(selectedControlId).label} is selected. Enter captures a replacement; R resets current; A resets all; Escape returns.`
  }
}
