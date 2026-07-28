import { describe, expect, it } from 'vitest'
import { biomeVisualGrammar, biomeVisualIdentity, flowGlyph, motionCadenceMs, showMotionAt, terminalGlyph, terminalTerrainLegend, terminalTileGlyph, terrainInspection, terrainVisual, visualIdentitySnapshot } from './visual-grammar'

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

  it('keeps every terrain glyph inspectable without relying on color', () => {
    for (const terrain of Object.values(terminalTerrainLegend)) {
      expect(terrain.glyph).toMatch(/^[\x20-\x7e]$/)
      expect(terrain.label).not.toBe('')
      expect(terrain.explanation).not.toBe('')
    }
    expect(terrainInspection('frostReliquary', 'frostRime')).toBe('* rime crack: whiteout pressure point')
  })

  it('renders distinct normal, threat, event, and climax identity snapshots', () => {
    const snapshots = Object.fromEntries(Object.keys(biomeVisualIdentity).map(biome => [biome, Object.fromEntries(['normal', 'threat', 'event', 'climax'].map(state => [state, visualIdentitySnapshot(biome as keyof typeof biomeVisualIdentity, state as 'normal' | 'threat' | 'event' | 'climax').line]))]))
    expect(snapshots).toEqual({
      mine: { normal: '. stone / # shaft / = rail', threat: ', collapse / ! foreman / + support', event: '^ dust / + support / = rail', climax: 'F warden / W waycache / , collapse' },
      wilds: { normal: ', ground / # thicket / A root arch', threat: '% web / ! pack / ~ ford', event: '% nesting / ~ ford / A root arch', climax: 'H heartwood / W waycache / % web' },
      caverns: { normal: '. stone / # rock / * crystal', threat: '* gas / ! eel screen / ~ tide shelf', event: '= current / ~ tide shelf / * crystal', climax: 'G tidemaw / W waycache / * gas' },
      ruins: { normal: ': ash / # ward / M monolith', threat: '> dart / ! sentinel / _ altar', event: '. veil / _ altar / M monolith', climax: 'R keeper / W waycache / > dart' },
      furnace: { normal: '. slag / # kiln / + idol', threat: '^ vent / ! kiln line / H lift', event: '* smoke / H lift / + idol', climax: 'K kiln heart / W waycache / ^ vent' },
      floodedRuins: { normal: ': silt / # ruin / A anchor', threat: '~ tide / ! undertow / = bridge', event: '= current / = bridge / A anchor', climax: 'D regent / W waycache / ~ tide' },
      cliffs: { normal: '. ledge / ^ drop / i signal fire', threat: '^ gust / ! hunter / | rope', event: '> wind / | rope / i signal fire', climax: 'S sky warden / W waycache / ^ gust' },
      burial: { normal: '; soil / # tomb / A cairn gate', threat: '; grave lane / ! guardian / . spirit path', event: '. migration / . spirit path / A cairn gate', climax: 'B barrow king / W waycache / ; grave lane' },
      saltFlats: { normal: '. flat / # ridge / ! glass marker', threat: '~ brine / ! mirage / o mirror', event: '. haze / o mirror / ! glass marker', climax: 'S sovereign / W waycache / ~ brine' },
      frostReliquary: { normal: '. rime / # ice wall / V thaw valve', threat: '* rime crack / ! oracle / = ice bridge', event: '* whiteout / = ice bridge / V thaw valve', climax: 'R reliquary / W waycache / * rime crack' }
    })
    for (const state of ['normal', 'threat', 'event', 'climax'] as const) expect(new Set(Object.values(snapshots).map(snapshot => snapshot[state])).size).toBe(Object.keys(snapshots).length)
  })
})
