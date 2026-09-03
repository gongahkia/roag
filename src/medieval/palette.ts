/** Browser-only and renderer-independent; it is never world or save data. */
export const MEDIEVAL_PALETTE_VERSION = 2 as const

/** Original soot/olive surfaces, parchment text, moss readiness, and river accents. */
export const JOMON_PALETTE = {
  consoleGround: '#1c1b14',
  panelSurface: '#29271c',
  panelBorder: '#a79a6a',
  bodyText: '#f0dfb4',
  mutedText: '#c9bd92',
  titleText: '#f2cf7c',
  selectedText: '#83cbc3',
  actionText: '#adce80',
  warningText: '#eba94d',
  errorText: '#de8065',
  water: '#86bed1',
  route: '#73c1ad',
  statusReady: '#a7ca70',
  statusWaiting: '#c2add2',
  statusRisk: '#db765c',
  statusNeutral: '#ded0aa'
} as const

export type JomonPaletteToken = keyof typeof JOMON_PALETTE

export type JomonPaletteContrastKind = 'small-text' | 'non-text-cue'

export interface JomonPaletteContrastRequirement {
  foreground: JomonPaletteToken
  background: JomonPaletteToken
  kind: JomonPaletteContrastKind
  minimumRatio: number
}

const smallTextAgainstPanel = (foreground: JomonPaletteToken): JomonPaletteContrastRequirement => ({
  foreground,
  background: 'panelSurface',
  kind: 'small-text',
  minimumRatio: 4.5
})

/** Canvas text uses panelSurface; semantic marks may occur against either dark surface. */
export const JOMON_PALETTE_CONTRAST_REQUIREMENTS: readonly JomonPaletteContrastRequirement[] = [
  ...([
    'bodyText', 'mutedText', 'titleText', 'selectedText', 'actionText', 'warningText', 'errorText',
    'statusReady', 'statusWaiting', 'statusRisk', 'statusNeutral'
  ] as const).map(smallTextAgainstPanel),
  { foreground: 'panelBorder', background: 'consoleGround', kind: 'non-text-cue', minimumRatio: 3 },
  { foreground: 'panelBorder', background: 'panelSurface', kind: 'non-text-cue', minimumRatio: 3 },
  ...(['selectedText', 'water', 'route', 'statusReady', 'statusWaiting', 'statusRisk'] as const).flatMap(foreground => [
    { foreground, background: 'consoleGround' as const, kind: 'non-text-cue' as const, minimumRatio: 3 },
    { foreground, background: 'panelSurface' as const, kind: 'non-text-cue' as const, minimumRatio: 3 }
  ])
]

export interface JomonPaletteContrastDiagnostic extends JomonPaletteContrastRequirement {
  actualRatio: number
}

const channel = (hex: string): number => {
  const normalized = Number.parseInt(hex, 16) / 255
  return normalized <= 0.04045 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4
}

const relativeLuminance = (hex: string): number => {
  const red = channel(hex.slice(1, 3))
  const green = channel(hex.slice(3, 5))
  const blue = channel(hex.slice(5, 7))
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue
}

/** Deterministic WCAG contrast calculation for the closed semantic palette. */
export const jomonPaletteContrastRatio = (foreground: JomonPaletteToken, background: JomonPaletteToken): number => {
  const foregroundLuminance = relativeLuminance(JOMON_PALETTE[foreground])
  const backgroundLuminance = relativeLuminance(JOMON_PALETTE[background])
  return (Math.max(foregroundLuminance, backgroundLuminance) + 0.05) / (Math.min(foregroundLuminance, backgroundLuminance) + 0.05)
}

/** Returns all failed authored requirements; an empty list is an accessibility pass. */
export const validateJomonPaletteContrast = (): readonly JomonPaletteContrastDiagnostic[] => JOMON_PALETTE_CONTRAST_REQUIREMENTS
  .map(requirement => ({ ...requirement, actualRatio: jomonPaletteContrastRatio(requirement.foreground, requirement.background) }))
  .filter(requirement => requirement.actualRatio < requirement.minimumRatio)

/** Pure data for the browser adapter; CSS never owns a duplicate set of values. */
export const JOMON_PALETTE_CSS_PROPERTIES: Readonly<Record<string, string>> = Object.fromEntries(
  Object.entries(JOMON_PALETTE).map(([token, value]) => [
    `--jomon-palette-${token.replace(/[A-Z]/gu, letter => `-${letter.toLowerCase()}`)}`,
    value
  ])
)

/** Text and glyph cues required alongside colour for terminal state readability. */
export const JOMON_NON_COLOR_STATE_CUES = {
  selectionMarker: '>',
  readyState: '+',
  activeState: 'ACTIVE',
  awaitingCourierState: 'CHOOSE COURIER',
  actionKeys: 'KEY',
  warningPrefix: 'WARNING //',
  errorPrefix: 'ERROR //'
} as const
