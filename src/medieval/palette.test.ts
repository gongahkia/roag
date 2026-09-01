import { describe, expect, it } from 'vitest'
import { DWARF_FORTRESS_CLASSIC_DARK_BRIGHT_PAIRS, DWARF_FORTRESS_CLASSIC_PALETTE, JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE, JOMON_PALETTE_CSS_PROPERTIES, MEDIEVAL_PALETTE_VERSION } from './palette'

describe('medieval terminal palette', () => {
  it('retains the exact historical pre-v50 sixteen-colour RGB and hex values', () => {
    expect(MEDIEVAL_PALETTE_VERSION).toBe(1)
    expect(DWARF_FORTRESS_CLASSIC_PALETTE).toEqual({
      black: { rgb: [0, 0, 0], hex: '#000000' },
      darkGray: { rgb: [128, 128, 128], hex: '#808080' },
      blue: { rgb: [0, 0, 128], hex: '#000080' },
      lightBlue: { rgb: [0, 0, 255], hex: '#0000ff' },
      green: { rgb: [0, 128, 0], hex: '#008000' },
      lightGreen: { rgb: [0, 255, 0], hex: '#00ff00' },
      cyan: { rgb: [0, 128, 128], hex: '#008080' },
      lightCyan: { rgb: [0, 255, 255], hex: '#00ffff' },
      red: { rgb: [128, 0, 0], hex: '#800000' },
      lightRed: { rgb: [255, 0, 0], hex: '#ff0000' },
      magenta: { rgb: [128, 0, 128], hex: '#800080' },
      lightMagenta: { rgb: [255, 0, 255], hex: '#ff00ff' },
      brown: { rgb: [128, 128, 0], hex: '#808000' },
      yellow: { rgb: [255, 255, 0], hex: '#ffff00' },
      lightGray: { rgb: [192, 192, 192], hex: '#c0c0c0' },
      white: { rgb: [255, 255, 255], hex: '#ffffff' }
    })
  })

  it('keeps all eight historical dark/bright pairs in their canonical order', () => {
    expect(DWARF_FORTRESS_CLASSIC_DARK_BRIGHT_PAIRS).toEqual([
      { dark: 'black', bright: 'darkGray' },
      { dark: 'blue', bright: 'lightBlue' },
      { dark: 'green', bright: 'lightGreen' },
      { dark: 'cyan', bright: 'lightCyan' },
      { dark: 'red', bright: 'lightRed' },
      { dark: 'magenta', bright: 'lightMagenta' },
      { dark: 'brown', bright: 'yellow' },
      { dark: 'lightGray', bright: 'white' }
    ])
  })

  it('provides complete original Jomon roles and the same values to CSS', () => {
    expect(Object.keys(JOMON_PALETTE)).toEqual([
      'consoleGround', 'panelSurface', 'panelBorder', 'bodyText', 'mutedText', 'titleText', 'selectedText', 'actionText',
      'warningText', 'errorText', 'water', 'route', 'statusReady', 'statusWaiting', 'statusRisk', 'statusNeutral'
    ])
    expect(Object.values(JOMON_PALETTE).every(value => Object.values(DWARF_FORTRESS_CLASSIC_PALETTE).some(color => color.hex === value))).toBe(true)
    expect(JOMON_PALETTE_CSS_PROPERTIES).toEqual(expect.objectContaining({
      '--jomon-palette-console-ground': JOMON_PALETTE.consoleGround,
      '--jomon-palette-body-text': JOMON_PALETTE.bodyText,
      '--jomon-palette-error-text': JOMON_PALETTE.errorText,
      '--jomon-palette-water': JOMON_PALETTE.water,
      '--jomon-palette-route': JOMON_PALETTE.route
    }))
    expect(Object.keys(JOMON_PALETTE_CSS_PROPERTIES)).toHaveLength(Object.keys(JOMON_PALETTE).length)
  })

  it('requires textual or glyph state cues alongside every current and reserved terminal state colour', () => {
    expect(JOMON_NON_COLOR_STATE_CUES).toEqual({
      selectionMarker: '>',
      activeState: 'ACTIVE',
      awaitingCourierState: 'CHOOSE COURIER',
      actionKeys: 'KEY',
      warningPrefix: 'WARNING //',
      errorPrefix: 'ERROR //'
    })
    expect(Object.values(JOMON_NON_COLOR_STATE_CUES).every(cue => cue.length > 0)).toBe(true)
  })
})
