export const CHUNK_SIZE = 16
export const CHUNKS_PER_AXIS = 3
export const WORLD_SIZE = CHUNK_SIZE * CHUNKS_PER_AXIS
export const WORLD_HEIGHT = 12

export enum BlockType {
  Air,
  Grass,
  Soil,
  Stone,
  Sand,
  Water,
  Trunk,
  Leaf,
  Brick,
  Plank
}

export interface Chunk { x: number; z: number; blocks: Uint8Array }
export interface World { seed: number; chunks: Chunk[]; get(x: number, y: number, z: number): BlockType; set(x: number, y: number, z: number, type: BlockType): boolean }
export interface PlayerState { x: number; y: number; z: number; velocityY: number; grounded: boolean }
export interface CameraState { rotation: 0 | 1 | 2 | 3; zoom: number }
export interface WorldSave { version: 1; seed: number; changes: Array<[number, BlockType]> }

export const BLOCKS: Array<{ type: BlockType; name: string; color: string }> = [
  { type: BlockType.Grass, name: 'Grass', color: '#75a743' },
  { type: BlockType.Soil, name: 'Soil', color: '#8d5235' },
  { type: BlockType.Stone, name: 'Stone', color: '#778084' },
  { type: BlockType.Sand, name: 'Sand', color: '#d5b65f' },
  { type: BlockType.Water, name: 'Water', color: '#4d8fc9' },
  { type: BlockType.Trunk, name: 'Trunk', color: '#72513b' },
  { type: BlockType.Leaf, name: 'Leaf', color: '#437348' },
  { type: BlockType.Brick, name: 'Brick', color: '#ad4943' },
  { type: BlockType.Plank, name: 'Plank', color: '#c8874b' }
]

export const isSolid = (type: BlockType): boolean => type !== BlockType.Air && type !== BlockType.Water
