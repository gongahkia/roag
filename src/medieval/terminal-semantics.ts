import { JOMON_NON_COLOR_STATE_CUES, type JomonPaletteToken } from './palette'

/** Shared renderer-neutral semantic primitives for terminal projections and glyph data. */
export const TERMINAL_GLYPH_VOCABULARY_ID = 'jomon-original-ascii-glyphs' as const

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

export interface TerminalGlyphReference {
  vocabulary: typeof TERMINAL_GLYPH_VOCABULARY_ID
  vocabularyVersion: number
  id: string
}

export interface TerminalGlyphCatalog {
  vocabulary: typeof TERMINAL_GLYPH_VOCABULARY_ID
  version: number
  glyphIds: readonly string[]
}
