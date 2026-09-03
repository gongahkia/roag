import { describe, expect, it } from 'vitest'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE, JOMON_PALETTE_CONTRAST_REQUIREMENTS, JOMON_PALETTE_CSS_PROPERTIES, MEDIEVAL_PALETTE_VERSION, jomonPaletteContrastRatio, validateJomonPaletteContrast } from './palette'

describe('medieval semantic palette', () => {
  it('defines the original version-two full-colour autumnal role table', () => {
    expect(MEDIEVAL_PALETTE_VERSION).toBe(2)
    expect(JOMON_PALETTE).toEqual({
      consoleGround: '#1c1b14', panelSurface: '#29271c', panelBorder: '#a79a6a', bodyText: '#f0dfb4',
      mutedText: '#c9bd92', titleText: '#f2cf7c', selectedText: '#83cbc3', actionText: '#adce80',
      warningText: '#eba94d', errorText: '#de8065', water: '#86bed1', route: '#73c1ad',
      statusReady: '#a7ca70', statusWaiting: '#c2add2', statusRisk: '#db765c', statusNeutral: '#ded0aa'
    })
  })

  it('provides complete semantic roles and the same values to CSS', () => {
    expect(Object.keys(JOMON_PALETTE)).toEqual([
      'consoleGround', 'panelSurface', 'panelBorder', 'bodyText', 'mutedText', 'titleText', 'selectedText', 'actionText',
      'warningText', 'errorText', 'water', 'route', 'statusReady', 'statusWaiting', 'statusRisk', 'statusNeutral'
    ])
    expect(JOMON_PALETTE_CSS_PROPERTIES).toEqual(expect.objectContaining({
      '--jomon-palette-console-ground': JOMON_PALETTE.consoleGround,
      '--jomon-palette-panel-surface': JOMON_PALETTE.panelSurface,
      '--jomon-palette-panel-border': JOMON_PALETTE.panelBorder,
      '--jomon-palette-body-text': JOMON_PALETTE.bodyText,
      '--jomon-palette-error-text': JOMON_PALETTE.errorText,
      '--jomon-palette-water': JOMON_PALETTE.water,
      '--jomon-palette-route': JOMON_PALETTE.route
    }))
    expect(Object.keys(JOMON_PALETTE_CSS_PROPERTIES)).toHaveLength(Object.keys(JOMON_PALETTE).length)
  })

  it('enforces declared WCAG contrast for current small text and meaningful surface cues', () => {
    expect(validateJomonPaletteContrast()).toEqual([])
    expect(JOMON_PALETTE_CONTRAST_REQUIREMENTS.filter(requirement => requirement.kind === 'small-text')).toHaveLength(11)
    expect(JOMON_PALETTE_CONTRAST_REQUIREMENTS.filter(requirement => requirement.kind === 'small-text').every(requirement => jomonPaletteContrastRatio(requirement.foreground, requirement.background) >= 4.5)).toBe(true)
    expect(JOMON_PALETTE_CONTRAST_REQUIREMENTS.filter(requirement => requirement.kind === 'non-text-cue').every(requirement => jomonPaletteContrastRatio(requirement.foreground, requirement.background) >= 3)).toBe(true)
  })

  it('requires textual or glyph state cues alongside every current and reserved terminal state colour', () => {
    expect(JOMON_NON_COLOR_STATE_CUES).toEqual({
      selectionMarker: '>',
      readyState: '+',
      activeState: 'ACTIVE',
      awaitingCourierState: 'CHOOSE COURIER',
      actionKeys: 'KEY',
      warningPrefix: 'WARNING //',
      errorPrefix: 'ERROR //'
    })
    expect(Object.values(JOMON_NON_COLOR_STATE_CUES).every(cue => cue.length > 0)).toBe(true)
  })
})
