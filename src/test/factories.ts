import { MAP_HEIGHT, MAP_WIDTH, indexOf, type Actor, type AreaState, type Biome, type Floor, type Hero, type LegacyRecord, type RunState, type Tile } from '../types'

export interface GateFixture { id: string; biome: Biome; state: 'locked' | 'open'; requirements: string[] }

const tiles = (): Tile[] => Array.from({ length: MAP_WIDTH * MAP_HEIGHT }, () => ({ kind: 'floor', explored: true, visible: true }))

export const createArea = (overrides: Partial<AreaState> = {}): AreaState => ({ biome: 'mine', status: 'active', floor: 0, completed: false, ...overrides })

export const createHero = (overrides: Partial<Hero> = {}): Hero => ({
  x: 1, y: 1, health: 22, maxHealth: 22, focus: 8, maxFocus: 8, gold: 0, bombs: 0, ropes: 0, keys: 0, xp: 0, level: 1,
  stats: { strength: 2, agility: 2, vitality: 2, intellect: 2 }, skills: [], inventory: [], equipment: {}, conditions: [], cooldowns: {}, ...overrides
})

export const createEnemy = (overrides: Partial<Actor> = {}): Actor => ({
  id: 'enemy-1', role: 'monster', kind: 'rat', name: 'Test Rat', x: 3, y: 1, health: 5, maxHealth: 5, attack: 2, defense: 8, speed: 100, energy: 0, glyph: 'r', color: '#ffffff', hostile: true, ai: 'chase', conditions: [], ...overrides
})

export const createGate = (overrides: Partial<GateFixture> = {}): GateFixture => ({ id: 'gate-1', biome: 'mine', state: 'locked', requirements: ['key'], ...overrides })

export const createLegacy = (overrides: Partial<LegacyRecord> = {}): LegacyRecord => ({ id: 'legacy-1', heirName: 'Ari', cause: 'defeated', biome: 'mine', floor: 0, seed: 1, ...overrides })

export const createFloor = (overrides: Partial<Floor> = {}): Floor => {
  const start = { x: 1, y: 1 }
  const exit = { x: MAP_WIDTH - 2, y: MAP_HEIGHT - 2 }
  const floorTiles = tiles()
  floorTiles[indexOf(exit.x, exit.y)] = { kind: 'exit', explored: true, visible: true }
  return { index: 0, biome: 'mine', seed: 1, tiles: floorTiles, actors: [], items: [], start, exit, guardianDefeated: true, telegraphs: [], ...overrides }
}

export const createRun = (overrides: Partial<RunState> = {}): RunState => ({ version: 2, seed: 1, floor: createFloor(), hero: createHero(), messages: [], status: 'playing', turn: 0, ...overrides })
