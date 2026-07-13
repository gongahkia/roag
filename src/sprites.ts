import { ITEM } from './content'
import type { Actor, Tile } from './types'

const size = 8
const atlasUrl = new URL('./assets/jomon-atlas-source.png', import.meta.url).href
const tileColor: Record<string, string> = {
  wall: '#798795', floor: '#4f5c6c', exit: '#f4d26a', door: '#c9935e', lockedDoor: '#e9c965', water: '#559dcc', lava: '#eb7258', pit: '#05070b', rope: '#d6a867', spikes: '#d4dae2', dart: '#d4dae2', fireVent: '#ff825e', crumble: '#99795f', boulder: '#abb0b4', web: '#d3d8e4', gas: '#8dbd82', support: '#86633f', rail: '#725637', rubble: '#8e9298', bramble: '#6c9f64', darkness: '#30384d', crate: '#c99162', chest: '#f4d26a', altar: '#cda2e3', shop: '#f4d26a', rescue: '#83d6af'
}

export const ATLAS_SPEC = { columns: 16, rows: 8, path: 'src/assets/jomon-atlas-source.png' } as const
export const HERO_SPRITE = 21
export type AtlasSourceRect = { x: number; y: number; width: number; height: number }
const atlasSourceColumns = [14, 125, 232, 337, 442, 549, 656, 763, 869, 972, 1077, 1186, 1294, 1403, 1512, 1623, 1745] as const
const atlasSourceRows = [13, 130, 257, 387, 525, 648, 748, 829, 887] as const
const rect = (x: number, y: number, width: number, height: number): AtlasSourceRect => ({ x, y, width, height })
export const atlasSourceOverrides: Partial<Record<number, AtlasSourceRect>> = {
  32: rect(23, 299, 105, 65), 33: rect(137, 289, 89, 82), 34: rect(239, 267, 87, 110), 35: rect(343, 278, 93, 100),
  36: rect(450, 294, 97, 80), 37: rect(557, 265, 100, 113), 38: rect(672, 272, 85, 105), 39: rect(772, 278, 104, 95),
  40: rect(889, 280, 96, 98), 41: rect(1014, 275, 54, 100), 42: rect(1093, 294, 89, 84), 43: rect(1212, 262, 96, 125),
  44: rect(1328, 289, 90, 85), 45: rect(1431, 302, 111, 78), 46: rect(1544, 292, 110, 84), 47: rect(1656, 284, 87, 96),
  48: rect(24, 422, 103, 85), 49: rect(137, 396, 95, 119), 50: rect(245, 424, 102, 85), 51: rect(357, 404, 83, 107),
  52: rect(454, 407, 93, 106), 53: rect(571, 401, 81, 110), 54: rect(678, 405, 83, 108), 55: rect(781, 388, 117, 129),
  56: rect(919, 436, 82, 69), 57: rect(1031, 407, 112, 110), 58: rect(1170, 421, 91, 86), 59: rect(1287, 433, 103, 74),
  60: rect(1409, 443, 100, 63), 61: rect(1527, 438, 109, 73), 62: rect(0, 0, 1, 1), 63: rect(0, 0, 1, 1),
  64: rect(23, 546, 82, 75), 65: rect(137, 541, 81, 89), 66: rect(240, 535, 100, 97), 67: rect(356, 535, 88, 96),
  68: rect(460, 536, 90, 98), 69: rect(574, 541, 87, 91), 70: rect(684, 552, 88, 82), 71: rect(800, 549, 75, 82),
  72: rect(904, 535, 84, 98), 73: rect(1010, 538, 102, 97), 74: rect(1132, 540, 97, 95), 75: rect(1248, 554, 88, 72),
  76: rect(1349, 540, 82, 90), 77: rect(1457, 541, 62, 101), 78: rect(1557, 552, 71, 79), 79: rect(1668, 538, 58, 91),
  80: rect(34, 654, 56, 87), 81: rect(123, 661, 96, 77), 82: rect(249, 658, 77, 80), 83: rect(359, 654, 61, 86),
  84: rect(457, 661, 93, 78), 85: rect(579, 655, 78, 83), 86: rect(687, 669, 64, 66), 87: rect(799, 654, 80, 87),
  88: rect(913, 654, 67, 87), 89: rect(1028, 656, 71, 85), 90: rect(1140, 655, 70, 86), 91: rect(1246, 656, 61, 85),
  92: rect(1353, 654, 56, 86), 93: rect(1448, 655, 67, 86), 94: rect(0, 0, 1, 1), 95: rect(0, 0, 1, 1)
}

export function atlasSourceRect(index: number): AtlasSourceRect {
  const override = atlasSourceOverrides[index]
  if (override) return override
  const column = index % ATLAS_SPEC.columns
  const row = Math.floor(index / ATLAS_SPEC.columns)
  return { x: atlasSourceColumns[column], y: atlasSourceRows[row], width: atlasSourceColumns[column + 1] - atlasSourceColumns[column], height: atlasSourceRows[row + 1] - atlasSourceRows[row] }
}

export const tileSprite: Partial<Record<Tile['kind'], number>> = {
  wall: 0, floor: 1, exit: 2, door: 3, lockedDoor: 4, water: 5, lava: 6, pit: 7, rope: 8, spikes: 9, dart: 10, fireVent: 11, crumble: 12, boulder: 13, web: 14, gas: 15,
  crate: 16, chest: 17, altar: 18, shop: 19, rescue: 20
}
export const actorSprite: Record<string, number> = {
  rat: 32, mole: 33, sapper: 34, beetle: 35, driller: 36, foreman: 37, thornling: 38, boar: 39, spitter: 40, wisp: 41, frog: 42, heartwood: 43, crawler: 44, magma: 45, echo: 46, seer: 47,
  slug: 48, geode: 49, scarab: 50, sentinel: 51, oracle: 52, shade: 53, cultist: 54, regent: 55, merchant: 19, ally: 20
}
export const itemSprite: Record<string, number> = {
  whip: 64, machete: 65, pickaxe: 66, spear: 67, sunblade: 68, buckler: 69, lantern: 70, cap: 71, mask: 72, coat: 73, mail: 74, boots: 75, featherboots: 76, ward: 77, sunseal: 78, tonic: 79,
  focusTonic: 80, mapScroll: 81, blinkRune: 82, bombPack: 83, ropeBundle: 84, key: 85, rock: 86, fireJar: 87, ember: 88, mend: 89, sight: 90, gust: 91, wardScript: 92, gate: 93, gold: 114
}

export class TextureAtlas {
  private readonly image = new Image()
  private readonly cells = document.createElement('canvas')
  private ready = false
  private sourceDetail = false
  private readonly listeners = new Set<() => void>()

  constructor() {
    this.image.onload = () => {
      this.cells.width = ATLAS_SPEC.columns * 16
      this.cells.height = ATLAS_SPEC.rows * 16
      const ctx = this.cells.getContext('2d')!
      ctx.imageSmoothingEnabled = true
      ctx.imageSmoothingQuality = 'high'
      for (let index = 0; index < ATLAS_SPEC.columns * ATLAS_SPEC.rows; index++) {
        const source = atlasSourceRect(index)
        const targetX = index % ATLAS_SPEC.columns * 16
        const targetY = Math.floor(index / ATLAS_SPEC.columns) * 16
        ctx.drawImage(this.image, source.x, source.y, source.width, source.height, targetX, targetY, 16, 16)
      }
      this.ready = true
      this.listeners.forEach(listener => listener())
    }
    this.image.src = atlasUrl
  }

  onReady(listener: () => void): void { if (this.ready) listener(); else this.listeners.add(listener) }
  setSourceDetail(value: boolean): void { this.sourceDetail = value }

  draw(ctx: CanvasRenderingContext2D, index: number | undefined, x: number, y: number, dim = false, flip = false): boolean {
    if (!this.ready || index === undefined) return false
    const sourceX = index % ATLAS_SPEC.columns * 16
    const sourceY = Math.floor(index / ATLAS_SPEC.columns) * 16
    const destinationX = x * 10 - 2
    const destinationY = y * 14
    ctx.save()
    ctx.globalAlpha = dim ? .38 : 1
    ctx.imageSmoothingEnabled = false
    if (flip) { ctx.translate(destinationX + 14, 0); ctx.scale(-1, 1) }
    if (this.sourceDetail) {
      const source = atlasSourceRect(index)
      ctx.drawImage(this.image, source.x, source.y, source.width, source.height, flip ? 0 : destinationX, destinationY, 14, 14)
    } else ctx.drawImage(this.cells, sourceX, sourceY, 16, 16, flip ? 0 : destinationX, destinationY, 14, 14)
    ctx.restore()
    return true
  }
}

export const textureAtlas = new TextureAtlas()

export function drawTileSprite(ctx: CanvasRenderingContext2D, tile: Tile, x: number, y: number, dim: boolean): void {
  if (textureAtlas.draw(ctx, tileSprite[tile.kind], x, y, dim)) return
  const px = x * 10 + 1
  const py = y * 14 + 3
  ctx.save()
  if (dim) ctx.globalAlpha = .38
  ctx.fillStyle = tile.kind === 'pit' ? '#05070b' : '#16202c'
  ctx.fillRect(px, py, size, size)
  const color = tileColor[tile.kind]
  if (tile.kind === 'floor' || tile.kind === 'crumble') {
    ctx.fillStyle = color
    ctx.fillRect(px + 1, py + 1, 1, 1)
    ctx.fillRect(px + 5, py + 5, 1, 1)
  } else if (tile.kind === 'wall') {
    ctx.fillStyle = color
    ctx.fillRect(px, py, 8, 8)
    ctx.fillStyle = '#46515d'
    ctx.fillRect(px, py + 3, 8, 1)
    ctx.fillRect(px + 3, py, 1, 8)
  } else if (tile.kind === 'water' || tile.kind === 'lava' || tile.kind === 'gas') {
    ctx.fillStyle = color
    ctx.fillRect(px, py + 3, 8, 3)
    ctx.fillRect(px + 2, py + 1, 3, 2)
    ctx.fillRect(px + 5, py + 6, 2, 1)
  } else if (tile.kind === 'support') {
    ctx.fillStyle = color
    ctx.fillRect(px + 1, py, 2, 8)
    ctx.fillRect(px + 5, py, 2, 8)
    ctx.fillStyle = '#4e3828'
    ctx.fillRect(px, py + 2, 8, 2)
    ctx.fillRect(px + 2, py + 6, 4, 1)
  } else if (tile.kind === 'rail') {
    ctx.fillStyle = color
    ctx.fillRect(px, py + 3, 8, 3)
    ctx.fillStyle = '#a08052'
    ctx.fillRect(px + 1, py + 3, 6, 1)
    ctx.fillRect(px + 2, py + 5, 4, 1)
    ctx.fillStyle = '#3f3024'
    ctx.fillRect(px, py + 4, 2, 1)
    ctx.fillRect(px + 5, py + 4, 3, 1)
  } else if (tile.kind === 'rubble') {
    ctx.fillStyle = color
    ctx.fillRect(px + 1, py + 5, 3, 2)
    ctx.fillRect(px + 4, py + 3, 3, 3)
    ctx.fillRect(px + 2, py + 1, 2, 2)
  } else if (tile.kind === 'bramble') {
    ctx.fillStyle = color
    ctx.fillRect(px + 1, py + 3, 6, 4)
    ctx.fillRect(px + 2, py + 1, 2, 3)
    ctx.fillRect(px + 5, py, 2, 4)
  } else if (tile.kind === 'darkness') {
    ctx.fillStyle = color
    ctx.fillRect(px, py, 8, 8)
    ctx.fillStyle = '#66708d'
    ctx.fillRect(px + 3, py + 3, 1, 1)
  } else if (tile.kind === 'spikes' || tile.kind === 'dart' || tile.kind === 'fireVent') {
    ctx.fillStyle = color
    for (let i = 0; i < 8; i += 2) { ctx.fillRect(px + i, py + 5 - (i % 3), 1, 3 + (i % 3)) }
  } else {
    ctx.fillStyle = color
    ctx.fillRect(px + 1, py + 1, 6, 6)
    ctx.fillStyle = '#ffffff44'
    ctx.fillRect(px + 2, py + 2, 2, 1)
  }
  ctx.restore()
}

export function drawActorSprite(ctx: CanvasRenderingContext2D, actor: Actor | undefined, hero: boolean, x: number, y: number, dim = false, flip = false): void {
  if (textureAtlas.draw(ctx, hero ? HERO_SPRITE : actor ? actorSprite[actor.kind] : undefined, x, y, dim, flip)) return
  const px = x * 10 + 1
  const py = y * 14 + 3
  const color = hero ? '#e8edf4' : actor?.color ?? '#d6dce8'
  ctx.save()
  if (dim) ctx.globalAlpha = .38
  ctx.fillStyle = '#111725'
  ctx.fillRect(px + 1, py + 1, 6, 7)
  ctx.fillStyle = color
  if (hero) {
    ctx.fillRect(px + 2, py, 4, 3)
    ctx.fillRect(px + 1, py + 3, 6, 3)
    ctx.fillRect(px + 2, py + 6, 2, 2)
    ctx.fillRect(px + 5, py + 6, 2, 2)
    ctx.fillStyle = '#f4d26a'
    ctx.fillRect(px + 2, py, 4, 1)
  } else if (actor?.role === 'merchant') {
    ctx.fillRect(px + 2, py, 4, 2)
    ctx.fillRect(px + 1, py + 2, 6, 4)
    ctx.fillStyle = '#5a3d29'
    ctx.fillRect(px, py + 1, 8, 1)
  } else if (actor?.role === 'ally') {
    ctx.fillRect(px + 2, py, 4, 3)
    ctx.fillRect(px + 1, py + 3, 6, 3)
    ctx.fillRect(px + 3, py + 6, 2, 2)
  } else {
    ctx.fillRect(px + 1, py + 2, 6, 4)
    ctx.fillRect(px + 2, py, 4, 2)
    ctx.fillStyle = '#111725'
    ctx.fillRect(px + 2, py + 3, 1, 1)
    ctx.fillRect(px + 5, py + 3, 1, 1)
  }
  ctx.restore()
}

export function drawItemSprite(ctx: CanvasRenderingContext2D, id: string, x: number, y: number): void {
  if (textureAtlas.draw(ctx, itemSprite[id], x, y)) return
  const px = x * 10 + 1
  const py = y * 14 + 3
  const item = ITEM[id]
  ctx.fillStyle = '#111725'
  ctx.fillRect(px + 2, py + 2, 4, 4)
  ctx.fillStyle = item?.color ?? '#f4d26a'
  if (item?.slot === 'mainHand') { ctx.fillRect(px + 3, py + 1, 2, 6); ctx.fillRect(px + 1, py + 5, 6, 1) }
  else if (item?.use === 'heal' || item?.use === 'focus') { ctx.fillRect(px + 2, py + 2, 4, 5); ctx.fillRect(px + 3, py + 1, 2, 1) }
  else { ctx.fillRect(px + 2, py + 2, 4, 4); ctx.fillRect(px + 3, py + 1, 2, 6) }
}
