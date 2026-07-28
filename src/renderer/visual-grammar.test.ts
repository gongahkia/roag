import { describe, expect, it } from 'vitest'
import { biomeVisualGrammar, flowGlyph, motionCadenceMs, showMotionAt, terminalGlyph, terminalTileGlyph, terrainVisual } from './visual-grammar'

describe('terminal visual grammar', () => {
  it('gives each campaign biome a distinct inspectable terrain vocabulary', () => {
    expect(new Set(Object.values(biomeVisualGrammar).map(grammar => grammar.legend)).size).toBe(Object.keys(biomeVisualGrammar).length)
    expect(terrainVisual('mine', 'floor', 'ascii')).toMatchObject({ glyph: '.', color: '#586470' })
    expect(terrainVisual('wilds', 'floor', 'ascii')).toMatchObject({ glyph: ',', color: '#6c9f64' })
    expect(terrainVisual('ruins', 'floor', 'ascii')).toMatchObject({ glyph: ':', color: '#9e856f' })
  })

  it('uses single-cell ASCII fallbacks for terminal-only maps', () => {
    expect(terminalGlyph('⚓')).toBe('?')
    expect(terminalTileGlyph('anchor', '⚓')).toBe('A')
    expect(terminalTileGlyph('lift', '↕')).toBe('H')
    expect(flowGlyph('ne', 'ascii')).toBe('A')
    expect(flowGlyph('ne', 'runes')).toBe('↗')
    expect(showMotionAt(0)).toBe(true)
    expect(showMotionAt(motionCadenceMs)).toBe(false)
  })
})
