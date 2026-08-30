import type { Biome, Direction, TileKind } from '../types'

export type TerminalVisualMode = 'ascii' | 'runes'
export type VisualIdentityState = 'normal' | 'threat' | 'event' | 'climax'

export interface VisualIdentityCue {
  glyph: string
  label: string
  explanation: string
}

export interface BiomeVisualIdentity {
  floor: VisualIdentityCue
  wall: VisualIdentityCue
  landmark: VisualIdentityCue
  route: VisualIdentityCue
  hazard: VisualIdentityCue
  enemyIntent: VisualIdentityCue
  reward: VisualIdentityCue
  weather: VisualIdentityCue
  boss: VisualIdentityCue
}

export interface VisualIdentitySnapshot {
  biome: Biome
  state: VisualIdentityState
  cues: readonly VisualIdentityCue[]
  line: string
}

export interface TerrainLegendEntry {
  glyph: string
  label: string
  explanation: string
}

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

export const biomeVisualIdentity: Record<Biome, BiomeVisualIdentity> = {
  mine: {
    floor: { glyph: '.', label: 'stone', explanation: 'worked drift floor' }, wall: { glyph: '#', label: 'shaft', explanation: 'mined wall' }, landmark: { glyph: '=', label: 'rail', explanation: 'ore route landmark' }, route: { glyph: '+', label: 'support', explanation: 'braced counterroute' }, hazard: { glyph: ',', label: 'collapse', explanation: 'unstable shelf' }, enemyIntent: { glyph: '!', label: 'foreman', explanation: 'cordon pressure' }, reward: { glyph: 'W', label: 'waycache', explanation: 'survey reward' }, weather: { glyph: '^', label: 'dust', explanation: 'collapse warning' }, boss: { glyph: 'F', label: 'warden', explanation: 'Obsidian Warden' }
  },
  wilds: {
    floor: { glyph: ',', label: 'ground', explanation: 'leaf-litter floor' }, wall: { glyph: '#', label: 'thicket', explanation: 'rooted wall' }, landmark: { glyph: 'A', label: 'root arch', explanation: 'overgrown landmark' }, route: { glyph: '~', label: 'ford', explanation: 'water crossing route' }, hazard: { glyph: '%', label: 'web', explanation: 'nesting snare' }, enemyIntent: { glyph: '!', label: 'pack', explanation: 'canopy hunt' }, reward: { glyph: 'W', label: 'waycache', explanation: 'forager reward' }, weather: { glyph: '%', label: 'nesting', explanation: 'web surge warning' }, boss: { glyph: 'H', label: 'heartwood', explanation: 'Heartwood Stag' }
  },
  caverns: {
    floor: { glyph: '.', label: 'stone', explanation: 'cave shelf floor' }, wall: { glyph: '#', label: 'rock', explanation: 'cavern wall' }, landmark: { glyph: '*', label: 'crystal', explanation: 'lit cave landmark' }, route: { glyph: '~', label: 'tide shelf', explanation: 'water route' }, hazard: { glyph: '*', label: 'gas', explanation: 'gas pocket' }, enemyIntent: { glyph: '!', label: 'eel screen', explanation: 'tide ambush' }, reward: { glyph: 'W', label: 'waycache', explanation: 'crystal reward' }, weather: { glyph: '=', label: 'current', explanation: 'tide turn warning' }, boss: { glyph: 'G', label: 'tidemaw', explanation: 'Tidemaw' }
  },
  ruins: {
    floor: { glyph: ':', label: 'ash', explanation: 'ritual court floor' }, wall: { glyph: '#', label: 'ward', explanation: 'ruin wall' }, landmark: { glyph: 'M', label: 'monolith', explanation: 'warded landmark' }, route: { glyph: '_', label: 'altar', explanation: 'lit processional route' }, hazard: { glyph: '>', label: 'dart', explanation: 'gallery threat' }, enemyIntent: { glyph: '!', label: 'sentinel', explanation: 'sightline pressure' }, reward: { glyph: 'W', label: 'waycache', explanation: 'ward reward' }, weather: { glyph: '.', label: 'veil', explanation: 'darkness warning' }, boss: { glyph: 'R', label: 'keeper', explanation: 'Stone Keeper' }
  },
  furnace: {
    floor: { glyph: '.', label: 'slag', explanation: 'kiln floor' }, wall: { glyph: '#', label: 'kiln', explanation: 'furnace wall' }, landmark: { glyph: '+', label: 'idol', explanation: 'forge landmark' }, route: { glyph: 'H', label: 'lift', explanation: 'raised route' }, hazard: { glyph: '^', label: 'vent', explanation: 'heat burst' }, enemyIntent: { glyph: '!', label: 'kiln line', explanation: 'terrace pressure' }, reward: { glyph: 'W', label: 'waycache', explanation: 'cinder reward' }, weather: { glyph: '*', label: 'smoke', explanation: 'stack warning' }, boss: { glyph: 'K', label: 'kiln heart', explanation: 'Kiln Heart' }
  },
  floodedRuins: {
    floor: { glyph: ':', label: 'silt', explanation: 'flooded court floor' }, wall: { glyph: '#', label: 'ruin', explanation: 'submerged wall' }, landmark: { glyph: 'A', label: 'anchor', explanation: 'tide landmark' }, route: { glyph: '=', label: 'bridge', explanation: 'dry island route' }, hazard: { glyph: '~', label: 'tide', explanation: 'water pressure' }, enemyIntent: { glyph: '!', label: 'undertow', explanation: 'current screen' }, reward: { glyph: 'W', label: 'waycache', explanation: 'salvage reward' }, weather: { glyph: '=', label: 'current', explanation: 'floodgate warning' }, boss: { glyph: 'D', label: 'regent', explanation: 'Drowned Regent' }
  },
  cliffs: {
    floor: { glyph: '.', label: 'ledge', explanation: 'high shelf floor' }, wall: { glyph: '^', label: 'drop', explanation: 'cliff face' }, landmark: { glyph: 'i', label: 'signal fire', explanation: 'sky landmark' }, route: { glyph: '|', label: 'rope', explanation: 'vertical route' }, hazard: { glyph: '^', label: 'gust', explanation: 'exposed ledge' }, enemyIntent: { glyph: '!', label: 'hunter', explanation: 'switchback screen' }, reward: { glyph: 'W', label: 'waycache', explanation: 'nest reward' }, weather: { glyph: '>', label: 'wind', explanation: 'squall warning' }, boss: { glyph: 'S', label: 'sky warden', explanation: 'Sky Warden' }
  },
  burial: {
    floor: { glyph: ';', label: 'soil', explanation: 'grave floor' }, wall: { glyph: '#', label: 'tomb', explanation: 'barrow wall' }, landmark: { glyph: 'A', label: 'cairn gate', explanation: 'procession landmark' }, route: { glyph: '.', label: 'spirit path', explanation: 'ancestor route' }, hazard: { glyph: ';', label: 'grave lane', explanation: 'disturbed soil' }, enemyIntent: { glyph: '!', label: 'guardian', explanation: 'procession line' }, reward: { glyph: 'W', label: 'waycache', explanation: 'ossuary reward' }, weather: { glyph: '.', label: 'migration', explanation: 'spirit warning' }, boss: { glyph: 'B', label: 'barrow king', explanation: 'Barrow King' }
  },
  saltFlats: {
    floor: { glyph: '.', label: 'flat', explanation: 'salt crust floor' }, wall: { glyph: '#', label: 'ridge', explanation: 'salt ridge' }, landmark: { glyph: '!', label: 'glass marker', explanation: 'horizon landmark' }, route: { glyph: 'o', label: 'mirror', explanation: 'caravan route' }, hazard: { glyph: '~', label: 'brine', explanation: 'caustic basin' }, enemyIntent: { glyph: '!', label: 'mirage', explanation: 'mirror screen' }, reward: { glyph: 'W', label: 'waycache', explanation: 'caravan reward' }, weather: { glyph: '.', label: 'haze', explanation: 'sight warning' }, boss: { glyph: 'S', label: 'sovereign', explanation: 'Salt Sovereign' }
  },
  frostReliquary: {
    floor: { glyph: '.', label: 'rime', explanation: 'shore snow floor' }, wall: { glyph: '#', label: 'ice wall', explanation: 'frozen barrier' }, landmark: { glyph: 'V', label: 'thaw valve', explanation: 'reliquary landmark' }, route: { glyph: '=', label: 'ice bridge', explanation: 'safe crossing route' }, hazard: { glyph: '*', label: 'rime crack', explanation: 'pressure crack' }, enemyIntent: { glyph: '!', label: 'oracle', explanation: 'whiteout screen' }, reward: { glyph: 'W', label: 'waycache', explanation: 'sled reward' }, weather: { glyph: '*', label: 'whiteout', explanation: 'visibility warning' }, boss: { glyph: 'R', label: 'reliquary', explanation: 'Reliquary Warden' }
  }
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

export const terminalTerrainLegend: Record<TileKind, TerrainLegendEntry> = {
  wall: { glyph: '#', label: 'wall', explanation: 'blocks passage' }, floor: { glyph: '.', label: 'floor', explanation: 'open ground' }, exit: { glyph: '>', label: 'exit', explanation: 'descend when cleared' }, door: { glyph: '+', label: 'door', explanation: 'open passage' }, lockedDoor: { glyph: '+', label: 'locked door', explanation: 'requires a key' }, water: { glyph: '~', label: 'water', explanation: 'costly crossing' }, lava: { glyph: '~', label: 'lava', explanation: 'burning barrier' }, pit: { glyph: ' ', label: 'pit', explanation: 'requires a rope' }, rope: { glyph: '|', label: 'rope', explanation: 'safe vertical crossing' }, spikes: { glyph: '^', label: 'spikes', explanation: 'damage on contact' }, dart: { glyph: '>', label: 'dart gallery', explanation: 'ranged trap lane' }, fireVent: { glyph: '^', label: 'fire vent', explanation: 'timed heat burst' }, crumble: { glyph: ',', label: 'crumble', explanation: 'unstable ground' }, boulder: { glyph: 'O', label: 'boulder', explanation: 'moving impact hazard' }, web: { glyph: '%', label: 'web', explanation: 'snaring ground' }, gas: { glyph: '*', label: 'gas', explanation: 'poison cloud' }, support: { glyph: '+', label: 'support', explanation: 'braced route' }, rail: { glyph: '=', label: 'rail', explanation: 'worked route' }, rubble: { glyph: ':', label: 'rubble', explanation: 'blocked debris' }, bramble: { glyph: '"', label: 'bramble', explanation: 'cutting thicket' }, darkness: { glyph: '.', label: 'darkness', explanation: 'reduced sight' }, crate: { glyph: 'o', label: 'crate', explanation: 'movable cover' }, chest: { glyph: 'C', label: 'chest', explanation: 'sealed reward' }, altar: { glyph: '_', label: 'altar', explanation: 'ritual station' }, shop: { glyph: '$', label: 'trader', explanation: 'supply stop' }, rescue: { glyph: '&', label: 'rescue', explanation: 'ally opportunity' }, smoke: { glyph: '*', label: 'smoke', explanation: 'obscures sight' }, lift: { glyph: 'H', label: 'lift', explanation: 'raised route' }, breakwall: { glyph: '#', label: 'breakwall', explanation: 'breachable wall' }, current: { glyph: '=', label: 'current', explanation: 'directed water' }, deepWater: { glyph: '~', label: 'deep water', explanation: 'impassable water' }, anchor: { glyph: 'A', label: 'anchor', explanation: 'holds against tide' }, cliffWall: { glyph: '^', label: 'cliff wall', explanation: 'vertical barrier' }, ledge: { glyph: '=', label: 'ledge', explanation: 'exposed traverse' }, graveSoil: { glyph: ';', label: 'grave soil', explanation: 'disturbed lane' }, cairn: { glyph: '^', label: 'cairn', explanation: 'ancestral marker' }, ossuary: { glyph: 'O', label: 'ossuary', explanation: 'bone cache' }, spiritPath: { glyph: '.', label: 'spirit path', explanation: 'marked procession route' }, saltMirror: { glyph: 'o', label: 'salt mirror', explanation: 'false horizon route' }, brine: { glyph: '~', label: 'brine', explanation: 'caustic basin' }, ice: { glyph: '=', label: 'ice', explanation: 'fast exposed route' }, frostRime: { glyph: '*', label: 'rime crack', explanation: 'whiteout pressure point' }, airlock: { glyph: 'O', label: 'airlock', explanation: 'physical route terminal' }
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

export const terminalTileGlyph = (kind: TileKind, glyph: string): string => terminalGlyph(glyph, terminalTerrainLegend[kind].glyph)
export const terrainInspection = (biome: Biome, kind: TileKind): string => {
  const terrain = terminalTerrainLegend[kind]
  const grammar = biomeVisualGrammar[biome]
  const glyph = kind === 'floor' ? grammar.floorGlyph : kind === 'wall' ? grammar.wallGlyph : terrain.glyph
  return `${glyph} ${terrain.label}: ${terrain.explanation}`
}
const snapshotCues: Record<VisualIdentityState, readonly (keyof BiomeVisualIdentity)[]> = {
  normal: ['floor', 'wall', 'landmark'],
  threat: ['hazard', 'enemyIntent', 'route'],
  event: ['weather', 'route', 'landmark'],
  climax: ['boss', 'reward', 'hazard']
}
export const visualIdentitySnapshot = (biome: Biome, state: VisualIdentityState): VisualIdentitySnapshot => {
  const identity = biomeVisualIdentity[biome]
  const cues = snapshotCues[state].map(slot => identity[slot])
  return { biome, state, cues, line: cues.map(cue => `${cue.glyph} ${cue.label}`).join(' / ') }
}
export const flowGlyph = (direction: Exclude<Direction, 'wait'>, mode: TerminalVisualMode): string => mode === 'runes' ? flowRunes[direction] : flowAscii[direction]
export const showMotionAt = (time: number): boolean => Math.floor(time / motionCadenceMs) % 2 === 0
