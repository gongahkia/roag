/**
 * Jomon's terminal palette contract, versioned independently of any renderer.
 *
 * The exact source colours are the historical pre-v50 Dwarf Fortress Classic
 * default scheme: https://dwarffortresswiki.org/index.php/Color_scheme
 * Jomon uses only the values and dark/bright pairing discipline; its semantic
 * roles, text, layout, glyphs, and game systems remain original.
 */
export const MEDIEVAL_PALETTE_VERSION = 1 as const

export type DwarfFortressClassicColorName =
  | 'black' | 'darkGray'
  | 'blue' | 'lightBlue'
  | 'green' | 'lightGreen'
  | 'cyan' | 'lightCyan'
  | 'red' | 'lightRed'
  | 'magenta' | 'lightMagenta'
  | 'brown' | 'yellow'
  | 'lightGray' | 'white'

export interface TerminalPaletteColor {
  readonly rgb: readonly [red: number, green: number, blue: number]
  readonly hex: `#${string}`
}

const color = (red: number, green: number, blue: number): TerminalPaletteColor => ({
  rgb: [red, green, blue],
  hex: `#${[red, green, blue].map(channel => channel.toString(16).padStart(2, '0')).join('')}`
})

/** Exact historical pre-v50 default values, retained as plain serializable data. */
export const DWARF_FORTRESS_CLASSIC_PALETTE: Readonly<Record<DwarfFortressClassicColorName, TerminalPaletteColor>> = {
  black: color(0, 0, 0),
  darkGray: color(128, 128, 128),
  blue: color(0, 0, 128),
  lightBlue: color(0, 0, 255),
  green: color(0, 128, 0),
  lightGreen: color(0, 255, 0),
  cyan: color(0, 128, 128),
  lightCyan: color(0, 255, 255),
  red: color(128, 0, 0),
  lightRed: color(255, 0, 0),
  magenta: color(128, 0, 128),
  lightMagenta: color(255, 0, 255),
  brown: color(128, 128, 0),
  yellow: color(255, 255, 0),
  lightGray: color(192, 192, 192),
  white: color(255, 255, 255)
}

export const DWARF_FORTRESS_CLASSIC_DARK_BRIGHT_PAIRS = [
  { dark: 'black', bright: 'darkGray' },
  { dark: 'blue', bright: 'lightBlue' },
  { dark: 'green', bright: 'lightGreen' },
  { dark: 'cyan', bright: 'lightCyan' },
  { dark: 'red', bright: 'lightRed' },
  { dark: 'magenta', bright: 'lightMagenta' },
  { dark: 'brown', bright: 'yellow' },
  { dark: 'lightGray', bright: 'white' }
] as const satisfies readonly { dark: DwarfFortressClassicColorName; bright: DwarfFortressClassicColorName }[]

/** Original Jomon role names; values always resolve through the sixteen-colour contract. */
export const JOMON_PALETTE = {
  consoleGround: DWARF_FORTRESS_CLASSIC_PALETTE.black.hex,
  panelSurface: DWARF_FORTRESS_CLASSIC_PALETTE.black.hex,
  panelBorder: DWARF_FORTRESS_CLASSIC_PALETTE.darkGray.hex,
  bodyText: DWARF_FORTRESS_CLASSIC_PALETTE.lightGray.hex,
  mutedText: DWARF_FORTRESS_CLASSIC_PALETTE.darkGray.hex,
  titleText: DWARF_FORTRESS_CLASSIC_PALETTE.yellow.hex,
  selectedText: DWARF_FORTRESS_CLASSIC_PALETTE.lightCyan.hex,
  actionText: DWARF_FORTRESS_CLASSIC_PALETTE.lightGreen.hex,
  warningText: DWARF_FORTRESS_CLASSIC_PALETTE.yellow.hex,
  errorText: DWARF_FORTRESS_CLASSIC_PALETTE.lightRed.hex,
  water: DWARF_FORTRESS_CLASSIC_PALETTE.lightBlue.hex,
  route: DWARF_FORTRESS_CLASSIC_PALETTE.cyan.hex,
  statusReady: DWARF_FORTRESS_CLASSIC_PALETTE.lightGreen.hex,
  statusWaiting: DWARF_FORTRESS_CLASSIC_PALETTE.lightMagenta.hex,
  statusRisk: DWARF_FORTRESS_CLASSIC_PALETTE.lightRed.hex,
  statusNeutral: DWARF_FORTRESS_CLASSIC_PALETTE.lightGray.hex
} as const

export type JomonPaletteToken = keyof typeof JOMON_PALETTE

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
