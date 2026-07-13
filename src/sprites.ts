import { ITEM } from './content'
import type { Actor, Tile } from './types'

const size = 8
const atlasUrl = new URL('./assets/expedition-atlas.png', import.meta.url).href
const tileColor: Record<string, string> = {
  wall: '#798795', floor: '#4f5c6c', exit: '#f4d26a', door: '#c9935e', lockedDoor: '#e9c965', water: '#559dcc', lava: '#eb7258', pit: '#05070b', rope: '#d6a867', spikes: '#d4dae2', dart: '#d4dae2', fireVent: '#ff825e', crumble: '#99795f', boulder: '#abb0b4', web: '#d3d8e4', gas: '#8dbd82', support: '#b99b72', rail: '#c5b2a0', rubble: '#8e9298', bramble: '#6c9f64', crate: '#c99162', chest: '#f4d26a', altar: '#cda2e3', shop: '#f4d26a', rescue: '#83d6af'
}

export const ATLAS_SPEC = { columns: 16, rows: 8, path: 'src/assets/expedition-atlas.png' } as const

const tileSprite: Partial<Record<Tile['kind'], number>> = {
  wall: 0, floor: 1, exit: 2, door: 3, lockedDoor: 4, water: 5, lava: 6, pit: 7, rope: 8, spikes: 9, dart: 10, fireVent: 11, crumble: 12, boulder: 13, web: 14, gas: 15,
  crate: 16, chest: 17, altar: 18, shop: 19, rescue: 20
}
const actorSprite: Record<string, number> = {
  rat: 32, mole: 33, sapper: 34, beetle: 35, driller: 36, foreman: 37, thornling: 38, boar: 39, spitter: 40, wisp: 41, frog: 42, heartwood: 43, crawler: 44, magma: 45, echo: 46, seer: 47,
  slug: 48, geode: 49, scarab: 50, sentinel: 51, oracle: 52, shade: 53, cultist: 54, regent: 55, merchant: 19, ally: 20
}
const itemSprite: Record<string, number> = {
  whip: 65, machete: 65, pickaxe: 66, spear: 67, sunblade: 68, buckler: 69, lantern: 70, cap: 71, mask: 72, coat: 73, mail: 74, boots: 75, featherboots: 76, ward: 77, sunseal: 78, tonic: 79,
  focusTonic: 80, mapScroll: 81, blinkRune: 82, bombPack: 83, ropeBundle: 84, key: 85, rock: 86, fireJar: 87, ember: 88, mend: 89, sight: 90, gust: 91, wardScript: 92, gate: 93, gold: 114
}

export class TextureAtlas {
  private readonly image = new Image()
  private readonly cells = document.createElement('canvas')
  private ready = false
  private readonly listeners = new Set<() => void>()

  constructor() {
    this.image.onload = () => {
      this.cells.width = ATLAS_SPEC.columns * 16
      this.cells.height = ATLAS_SPEC.rows * 16
      const ctx = this.cells.getContext('2d')!
      ctx.imageSmoothingEnabled = true
      ctx.imageSmoothingQuality = 'high'
      const sourceWidth = this.image.naturalWidth / ATLAS_SPEC.columns
      const sourceHeight = this.image.naturalHeight / ATLAS_SPEC.rows
      for (let index = 0; index < ATLAS_SPEC.columns * ATLAS_SPEC.rows; index++) {
        const sourceX = index % ATLAS_SPEC.columns * sourceWidth
        const sourceY = Math.floor(index / ATLAS_SPEC.columns) * sourceHeight
        const targetX = index % ATLAS_SPEC.columns * 16
        const targetY = Math.floor(index / ATLAS_SPEC.columns) * 16
        ctx.drawImage(this.image, sourceX, sourceY, sourceWidth, sourceHeight, targetX, targetY, 16, 16)
      }
      this.ready = true
      this.listeners.forEach(listener => listener())
    }
    this.image.src = atlasUrl
  }

  onReady(listener: () => void): void { if (this.ready) listener(); else this.listeners.add(listener) }

  draw(ctx: CanvasRenderingContext2D, index: number | undefined, x: number, y: number, dim = false): boolean {
    if (!this.ready || index === undefined) return false
    const sourceX = index % ATLAS_SPEC.columns * 16
    const sourceY = Math.floor(index / ATLAS_SPEC.columns) * 16
    ctx.save()
    ctx.globalAlpha = dim ? .38 : 1
    ctx.imageSmoothingEnabled = false
    ctx.drawImage(this.cells, sourceX, sourceY, 16, 16, x * 10 - 2, y * 14, 14, 14)
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
    ctx.fillRect(px + 3, py, 2, 8)
    ctx.fillRect(px, py + 2, 8, 1)
  } else if (tile.kind === 'rail') {
    ctx.fillStyle = color
    ctx.fillRect(px, py + 2, 8, 1)
    ctx.fillRect(px, py + 6, 8, 1)
    for (let i = 1; i < 8; i += 3) ctx.fillRect(px + i, py + 1, 1, 7)
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

export function drawActorSprite(ctx: CanvasRenderingContext2D, actor: Actor | undefined, hero: boolean, x: number, y: number, dim = false): void {
  if (textureAtlas.draw(ctx, hero ? 21 : actor ? actorSprite[actor.kind] : undefined, x, y, dim)) return
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
