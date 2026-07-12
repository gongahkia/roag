import { BlockType, CHUNKS_PER_AXIS, type Chunk, type World, WORLD_HEIGHT, WORLD_SIZE, type WorldSave } from './types'

const index = (x: number, y: number, z: number) => (y * WORLD_SIZE + z) * WORLD_SIZE + x
const inBounds = (x: number, y: number, z: number) => x >= 0 && x < WORLD_SIZE && y >= 0 && y < WORLD_HEIGHT && z >= 0 && z < WORLD_SIZE
const hash = (x: number, z: number, seed: number) => {
  let n = Math.imul(x ^ seed, 0x45d9f3b) ^ Math.imul(z + seed, 0x27d4eb2d)
  n ^= n >>> 16
  return (Math.imul(n, 0x45d9f3b) ^ (n >>> 16)) >>> 0
}

export class BlockWorld implements World {
  readonly chunks: Chunk[]
  private readonly blocks = new Uint8Array(WORLD_SIZE * WORLD_HEIGHT * WORLD_SIZE)
  private readonly initial: Uint8Array
  private readonly changes = new Map<number, BlockType>()

  constructor(readonly seed: number) {
    this.generate()
    this.initial = this.blocks.slice()
    this.chunks = Array.from({ length: CHUNKS_PER_AXIS ** 2 }, (_, i) => ({
      x: i % CHUNKS_PER_AXIS,
      z: Math.floor(i / CHUNKS_PER_AXIS),
      blocks: this.blocks
    }))
  }

  get(x: number, y: number, z: number): BlockType {
    return inBounds(x, y, z) ? this.blocks[index(x, y, z)] as BlockType : BlockType.Air
  }

  set(x: number, y: number, z: number, type: BlockType): boolean {
    if (!inBounds(x, y, z)) return false
    const i = index(x, y, z)
    if (this.blocks[i] === type) return false
    this.blocks[i] = type
    if (this.initial[i] === type) this.changes.delete(i)
    else this.changes.set(i, type)
    return true
  }

  highestSolid(x: number, z: number): number {
    for (let y = WORLD_HEIGHT - 1; y >= 0; y--) if (this.get(x, y, z) !== BlockType.Air && this.get(x, y, z) !== BlockType.Water) return y
    return -1
  }

  toSave(): WorldSave { return { version: 1, seed: this.seed, changes: [...this.changes.entries()] } }

  applySave(save: WorldSave): boolean {
    if (save.version !== 1 || save.seed !== this.seed) return false
    for (const [i, type] of save.changes) {
      if (i >= 0 && i < this.blocks.length && Number.isInteger(type)) this.blocks[i] = type
    }
    this.changes.clear()
    for (const [i, type] of save.changes) this.changes.set(i, type)
    return true
  }

  private generate(): void {
    for (let z = 0; z < WORLD_SIZE; z++) for (let x = 0; x < WORLD_SIZE; x++) {
      const n = hash(x, z, this.seed)
      const height = 3 + ((n & 3) + ((hash(x >> 1, z >> 1, this.seed) >> 2) & 3) > 4 ? 2 : 0) + ((n >> 5) & 1)
      const sandy = (n % 19) === 0
      for (let y = 0; y <= height; y++) this.blocks[index(x, y, z)] = y === height ? (sandy ? BlockType.Sand : BlockType.Grass) : y > height - 3 ? BlockType.Soil : BlockType.Stone
      if ((n % 41) === 0 && !sandy && x > 2 && z > 2 && x < WORLD_SIZE - 3 && z < WORLD_SIZE - 3) this.tree(x, height + 1, z)
    }
  }

  private tree(x: number, y: number, z: number): void {
    for (let h = 0; h < 3 && y + h < WORLD_HEIGHT; h++) this.blocks[index(x, y + h, z)] = BlockType.Trunk
    for (let dz = -1; dz <= 1; dz++) for (let dx = -1; dx <= 1; dx++) for (let dy = 2; dy <= 3; dy++) {
      if (Math.abs(dx) + Math.abs(dz) < 3 && y + dy < WORLD_HEIGHT) this.blocks[index(x + dx, y + dy, z + dz)] = BlockType.Leaf
    }
  }
}

export const generateWorld = (seed: number) => new BlockWorld(seed)
