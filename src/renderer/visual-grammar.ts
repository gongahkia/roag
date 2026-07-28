import type { Biome, Direction, TileKind } from '../types'

export type TerminalVisualMode = 'ascii' | 'runes'

export interface BiomeVisualGrammar {
  floor: string
  background: string
  floorGlyph: string
  runeFloorGlyph: string
  wall: string
  wallGlyph: string
  runeWallGlyph: string
  legend: string
}

export const biomeVisualGrammar: Record<Biome, BiomeVisualGrammar> = {
  mine: { floor: '#586470', background: '#11151d', floorGlyph: '.', runeFloorGlyph: '·', wall: '#7d8792', wallGlyph: '#', runeWallGlyph: '▓', legend: 'MINE . stone # wall = rail + support' },
  wilds: { floor: '#6c9f64', background: '#0e160d', floorGlyph: ',', runeFloorGlyph: '♧', wall: '#51784b', wallGlyph: '#', runeWallGlyph: '▓', legend: 'WILDS , ground # thicket " bramble ~ water' },
  caverns: { floor: '#6d8f9a', background: '#0a1621', floorGlyph: '.', runeFloorGlyph: '·', wall: '#71809d', wallGlyph: '#', runeWallGlyph: '▓', legend: 'CAVES . stone # wall ~ water * gas' },
  ruins: { floor: '#9e856f', background: '#17110e', floorGlyph: ':', runeFloorGlyph: '░', wall: '#95836f', wallGlyph: '#', runeWallGlyph: '▓', legend: 'RUINS : ash # wall ^ trap _ altar' },
  furnace: { floor: '#bc8266', background: '#1c0d0b', floorGlyph: '.', runeFloorGlyph: '·', wall: '#8d6259', wallGlyph: '#', runeWallGlyph: '▓', legend: 'FURNACE . slag # wall ^ vent ~ heat' },
  floodedRuins: { floor: '#63aab5', background: '#0a1920', floorGlyph: ':', runeFloorGlyph: '≈', wall: '#547c8c', wallGlyph: '#', runeWallGlyph: '▓', legend: 'FLOODED : silt # wall ~ tide = current' },
  cliffs: { floor: '#c2d4dc', background: '#101725', floorGlyph: '.', runeFloorGlyph: '·', wall: '#71809d', wallGlyph: '^', runeWallGlyph: '▲', legend: 'CLIFFS . ledge ^ drop | rope = path' },
  burial: { floor: '#95836f', background: '#17120e', floorGlyph: ';', runeFloorGlyph: ';', wall: '#78695d', wallGlyph: '#', runeWallGlyph: '▓', legend: 'BARROW ; soil # tomb ^ cairn . spirit' },
  saltFlats: { floor: '#d8d3ad', background: '#1a170d', floorGlyph: '.', runeFloorGlyph: '·', wall: '#b6ad86', wallGlyph: '#', runeWallGlyph: '▓', legend: 'SALT . flat # ridge o mirror ~ brine' },
  frostReliquary: { floor: '#bfe8f3', background: '#0b1821', floorGlyph: '.', runeFloorGlyph: '═', wall: '#7fa7b7', wallGlyph: '#', runeWallGlyph: '▓', legend: 'FROST . rime # wall = ice * whiteout' }
}

const asciiFallback: Partial<Record<TileKind, string>> = {
  support: '+', crate: 'o', chest: 'C', lift: 'H', current: '=', deepWater: '~', anchor: 'A', cliffWall: '^', cairn: '^', ossuary: 'O', spiritPath: '.', saltMirror: 'o', brine: '~'
}

const flowAscii: Record<Exclude<Direction, 'wait'>, string> = { n: 'N', ne: 'A', e: 'E', se: 'C', s: 'S', sw: 'Z', w: 'W', nw: 'Q' }
const flowRunes: Record<Exclude<Direction, 'wait'>, string> = { n: '↑', ne: '↗', e: '→', se: '↘', s: '↓', sw: '↙', w: '←', nw: '↖' }
export const motionCadenceMs = 180

export const terminalGlyph = (glyph: string, fallback = '?'): string => /^[\x20-\x7e]$/.test(glyph) ? glyph : fallback

export const terrainVisual = (biome: Biome, kind: TileKind, mode: TerminalVisualMode): { glyph?: string; color?: string; background?: string } => {
  const grammar = biomeVisualGrammar[biome]
  if (kind === 'floor') return { glyph: mode === 'runes' ? grammar.runeFloorGlyph : grammar.floorGlyph, color: grammar.floor, background: grammar.background }
  if (kind === 'wall') return { glyph: mode === 'runes' ? grammar.runeWallGlyph : grammar.wallGlyph, color: grammar.wall, background: grammar.background }
  return {}
}

export const terminalTileGlyph = (kind: TileKind, glyph: string): string => terminalGlyph(glyph, asciiFallback[kind] ?? '?')
export const flowGlyph = (direction: Exclude<Direction, 'wait'>, mode: TerminalVisualMode): string => mode === 'runes' ? flowRunes[direction] : flowAscii[direction]
export const showMotionAt = (time: number): boolean => Math.floor(time / motionCadenceMs) % 2 === 0
