import { ITEM } from './content'
import manifestData from './assets/generated-sprites/sprite-manifest.json'
import { CELL_HEIGHT, CELL_WIDTH } from './renderer/metrics'
import { PROP_IDS, propDefinition } from './props'
import type { Actor, Biome, Prop, Tile } from './types'

const spriteSize = 14
const sourceSize = 16
const terrainBase: Record<Biome, string> = { mine: '#211f1a', wilds: '#18301c', caverns: '#162b32', ruins: '#28222c' }

const sheetUrls = {
  'terrain-mine': new URL('./assets/generated-sprites/terrain-mine.png', import.meta.url).href,
  'terrain-wilds': new URL('./assets/generated-sprites/terrain-wilds.png', import.meta.url).href,
  'terrain-caverns': new URL('./assets/generated-sprites/terrain-caverns.png', import.meta.url).href,
  'terrain-ruins': new URL('./assets/generated-sprites/terrain-ruins.png', import.meta.url).href,
  hero: new URL('./assets/generated-sprites/hero.png', import.meta.url).href,
  'npcs-gold': new URL('./assets/generated-sprites/npcs-gold.png', import.meta.url).href,
  'actors-mine': new URL('./assets/generated-sprites/actors-mine.png', import.meta.url).href,
  'actors-wilds': new URL('./assets/generated-sprites/actors-wilds.png', import.meta.url).href,
  'actors-caverns': new URL('./assets/generated-sprites/actors-caverns.png', import.meta.url).href,
  'actors-ruins': new URL('./assets/generated-sprites/actors-ruins.png', import.meta.url).href,
  items: new URL('./assets/generated-sprites/items.png', import.meta.url).href,
  effects: new URL('./assets/generated-sprites/effects.png', import.meta.url).href
} as const

export type SpriteSheetId = keyof typeof sheetUrls
export type HeroAnimation = 'idle' | 'walk' | 'attack' | 'hit' | 'death'
export interface SpriteOffset { x: number; y: number }
export interface SpriteRef { sheet: SpriteSheetId; column: number; row: number; frames: number; frameDurationMs: number; sourceOffset?: SpriteOffset; frameOffsets?: SpriteOffset[] }
interface ManifestAnimation { id: string; row: number; column: number; frames: number; frameDurationMs: number; flipSafe: boolean }
interface ManifestSheet {
  id: SpriteSheetId
  file: string
  columns: number
  rows: number
  props?: string[]
  cellOffsets?: SpriteOffset[]
  animations?: ManifestAnimation[]
  actorRows?: string[]
  itemLayout?: Array<string | null>
  effectRows?: string[]
  frames?: number
  frameDurationMs?: number
}
interface SpriteManifest { cellSize: number; terrainLayout: Array<Tile['kind']>; sheets: ManifestSheet[] }
export interface SpriteSheetSpec { id: SpriteSheetId; file: string; url: string; columns: number; rows: number; labels: string[]; cellOffsets: SpriteOffset[] }

const manifest = manifestData as SpriteManifest
const manifestSheets = new Map(manifest.sheets.map(sheet => [sheet.id, sheet]))
const terrainSheet: Record<Biome, SpriteSheetId> = { mine: 'terrain-mine', wilds: 'terrain-wilds', caverns: 'terrain-caverns', ruins: 'terrain-ruins' }
export const tileSprite = Object.fromEntries(manifest.terrainLayout.map((id, index) => [id, index])) as Record<Tile['kind'], number>

const manifestPropIds = manifest.sheets.filter(sheet => sheet.id.startsWith('terrain-')).flatMap(sheet => sheet.props ?? [])
for (const id of manifestPropIds) propDefinition(id as Prop['kind'])
for (const id of PROP_IDS) if (!manifestPropIds.includes(id)) throw new Error(`prop sprite missing from manifest: ${id}`)

const ref = (sheet: SpriteSheetId, column: number, row: number, frames = 1, frameDurationMs = 160, sourceOffset?: SpriteOffset, frameOffsets?: SpriteOffset[]): SpriteRef => ({ sheet, column, row, frames, frameDurationMs, sourceOffset, frameOffsets })
const rowRefs = (sheetId: SpriteSheetId): Record<string, SpriteRef> => {
  const sheet = manifestSheets.get(sheetId)!
  return Object.fromEntries((sheet.actorRows ?? []).map((id, row) => [id, ref(sheetId, 0, row, sheet.frames, sheet.frameDurationMs, undefined, sheet.cellOffsets?.slice(row * sheet.columns, row * sheet.columns + (sheet.frames ?? 1)))]))
}
export const actorSprite: Record<string, SpriteRef> = {
  ...rowRefs('actors-mine'),
  ...rowRefs('actors-wilds'),
  ...rowRefs('actors-caverns'),
  ...rowRefs('actors-ruins'),
  merchant: ref('npcs-gold', 0, 0, 4, 240),
  ally: ref('npcs-gold', 0, 1, 4, 240)
}

const itemSheet = manifestSheets.get('items')!
export const itemSprite = Object.fromEntries((itemSheet.itemLayout ?? []).flatMap((id, index) => id ? [[id, ref('items', index % itemSheet.columns, Math.floor(index / itemSheet.columns), 1, 160, itemSheet.cellOffsets?.[index])]] : [])) as Record<string, SpriteRef>
itemSprite.gold = ref('npcs-gold', 0, 2, 4, 160)

const heroSheet = manifestSheets.get('hero')!
const heroSprite = Object.fromEntries((heroSheet.animations ?? []).map(animation => [animation.id.replace('hero.', ''), ref('hero', animation.column, animation.row, animation.frames, animation.frameDurationMs)])) as Record<HeroAnimation, SpriteRef>
const effectSheet = manifestSheets.get('effects')!
export const effectSprite = Object.fromEntries((effectSheet.effectRows ?? []).map((id, row) => [id, ref('effects', 0, row, effectSheet.frames, effectSheet.frameDurationMs, undefined, effectSheet.cellOffsets?.slice(row * effectSheet.columns, row * effectSheet.columns + (effectSheet.frames ?? 1)))])) as Record<string, SpriteRef>

function labelsFor(sheet: ManifestSheet): string[] {
  const labels = Array<string>(sheet.columns * sheet.rows).fill('')
  if (sheet.id.startsWith('terrain-')) {
    manifest.terrainLayout.forEach((id, index) => { labels[index] = `tile.${id}` })
    sheet.props?.forEach((id, index) => { labels[manifest.terrainLayout.length + index] = `prop.${id}` })
  }
  sheet.animations?.forEach(animation => {
    for (let frame = 0; frame < animation.frames; frame++) labels[animation.row * sheet.columns + animation.column + frame] = `${animation.id}.${frame + 1}`
  })
  sheet.actorRows?.forEach((id, row) => {
    for (let frame = 0; frame < (sheet.frames ?? 1); frame++) labels[row * sheet.columns + frame] = `actor.${id}.${frame + 1}`
  })
  sheet.itemLayout?.forEach((id, index) => { if (id) labels[index] = `item.${id}` })
  sheet.effectRows?.forEach((id, row) => {
    for (let frame = 0; frame < (sheet.frames ?? 1); frame++) labels[row * sheet.columns + frame] = `effect.${id}.${frame + 1}`
  })
  return labels
}

export const spriteSheetSpecs: SpriteSheetSpec[] = manifest.sheets.map(sheet => ({ ...sheet, url: sheetUrls[sheet.id], labels: labelsFor(sheet), cellOffsets: sheet.cellOffsets ?? Array.from({ length: sheet.columns * sheet.rows }, () => ({ x: 0, y: 0 })) }))

export class TextureAtlas {
  private readonly images = new Map<SpriteSheetId, HTMLImageElement>()
  private readonly listeners = new Set<() => void>()
  private settled = 0
  private ready = false

  constructor() {
    if (typeof Image === 'undefined') { this.ready = true; return }
    for (const sheet of spriteSheetSpecs) {
      const image = new Image()
      image.onload = () => this.settle()
      image.onerror = () => this.settle()
      image.src = sheet.url
      this.images.set(sheet.id, image)
    }
  }

  onReady(listener: () => void): void { if (this.ready) listener(); else this.listeners.add(listener) }

  draw(ctx: CanvasRenderingContext2D, sprite: SpriteRef | undefined, x: number, y: number, dim = false, flip = false, frameOverride?: number): boolean {
    if (!sprite || !this.ready) return false
    const image = this.images.get(sprite.sheet)
    if (!image?.complete || !image.naturalWidth) return false
    const frame = frameOverride === undefined ? Math.floor(performance.now() / sprite.frameDurationMs) % sprite.frames : Math.max(0, Math.min(sprite.frames - 1, frameOverride))
    const sourceOffset = sprite.frameOffsets?.[frame] ?? sprite.sourceOffset
    const destinationX = x * CELL_WIDTH - 2 + Math.round((sourceOffset?.x ?? 0) * spriteSize / sourceSize)
    const destinationY = y * CELL_HEIGHT + Math.round((sourceOffset?.y ?? 0) * spriteSize / sourceSize)
    ctx.save()
    ctx.globalAlpha = dim ? .38 : 1
    ctx.imageSmoothingEnabled = false
    if (flip) { ctx.translate(destinationX + spriteSize, 0); ctx.scale(-1, 1) }
    ctx.drawImage(image, (sprite.column + frame) * sourceSize, sprite.row * sourceSize, sourceSize, sourceSize, flip ? 0 : destinationX, destinationY, spriteSize, spriteSize)
    ctx.restore()
    return true
  }

  private settle(): void {
    this.settled++
    if (this.settled !== spriteSheetSpecs.length) return
    this.ready = true
    this.listeners.forEach(listener => listener())
    this.listeners.clear()
  }
}

export const textureAtlas = new TextureAtlas()

export function drawTileSprite(ctx: CanvasRenderingContext2D, tile: Tile, biome: Biome, x: number, y: number, dim: boolean, clip = false): void {
  if (clip) {
    ctx.save()
    ctx.beginPath()
    ctx.rect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT)
    ctx.clip()
  }
  ctx.save()
  ctx.globalAlpha = dim ? .38 : 1
  ctx.fillStyle = terrainBase[biome]
  ctx.fillRect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT)
  ctx.restore()
  const index = tileSprite[tile.kind]
  const sheet = manifestSheets.get(terrainSheet[biome])!
  if (textureAtlas.draw(ctx, ref(sheet.id, index % 8, Math.floor(index / 8), 1, 160, sheet.cellOffsets?.[index]), x, y, dim)) {
    if (clip) ctx.restore()
    return
  }
  fallbackTile(ctx, tile, x, y, dim)
  if (clip) ctx.restore()
}

export function drawPropSprite(ctx: CanvasRenderingContext2D, prop: Prop, x: number, y: number, dim: boolean): void {
  if (prop.state === 'destroyed') return
  const sheet = manifestSheets.get(terrainSheet[prop.biome])!
  const propIndex = sheet.props?.indexOf(prop.kind) ?? -1
  if (propIndex < 0) throw new Error(`prop sprite missing: ${prop.kind}`)
  const index = manifest.terrainLayout.length + propIndex
  if (!textureAtlas.draw(ctx, ref(sheet.id, index % sheet.columns, Math.floor(index / sheet.columns), 1, 160, sheet.cellOffsets?.[index]), x, y, dim)) fallbackProp(ctx, prop, x, y, dim)
  if (prop.state !== 'inspected' && prop.state !== 'activated') return
  ctx.save()
  ctx.globalAlpha = dim ? .32 : .78
  ctx.strokeStyle = prop.state === 'inspected' ? '#8fd6c2' : prop.kind === 'mine.lanternPost' ? '#fff1a8' : '#ffe181'
  ctx.lineWidth = 1
  ctx.strokeRect(x * CELL_WIDTH + .5, y * CELL_HEIGHT + .5, CELL_WIDTH - 1, CELL_HEIGHT - 1)
  ctx.restore()
}

export function drawActorSprite(ctx: CanvasRenderingContext2D, actor: Actor | undefined, hero: boolean, x: number, y: number, dim = false, flip = false, animation: HeroAnimation = 'idle'): void {
  const sprite = hero ? heroSprite[animation] : actor ? actorSprite[actor.kind] : undefined
  if (textureAtlas.draw(ctx, sprite, x, y, dim, flip)) return
  fallbackActor(ctx, actor, hero, x, y, dim)
}

export function drawItemSprite(ctx: CanvasRenderingContext2D, id: string, x: number, y: number, clip = false): void {
  if (clip) {
    ctx.save()
    ctx.beginPath()
    ctx.rect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT)
    ctx.clip()
  }
  if (textureAtlas.draw(ctx, itemSprite[id], x, y)) {
    if (clip) ctx.restore()
    return
  }
  const item = ITEM[id]
  ctx.fillStyle = item?.color ?? '#f4d26a'
  ctx.fillRect(x * CELL_WIDTH + 3, y * CELL_HEIGHT + 4, 5, 6)
  if (clip) ctx.restore()
}

export function drawEffectSprite(ctx: CanvasRenderingContext2D, id: string, x: number, y: number, frame: number): boolean {
  return textureAtlas.draw(ctx, effectSprite[id], x, y, false, false, frame)
}

function fallbackTile(ctx: CanvasRenderingContext2D, tile: Tile, x: number, y: number, dim: boolean): void {
  ctx.save()
  ctx.globalAlpha = dim ? .38 : 1
  ctx.fillStyle = tile.kind === 'wall' ? '#798795' : tile.kind === 'floor' ? '#4f5c6c' : '#f4d26a'
  ctx.fillRect(x * CELL_WIDTH + 1, y * CELL_HEIGHT + 3, 8, 8)
  ctx.restore()
}

function fallbackActor(ctx: CanvasRenderingContext2D, actor: Actor | undefined, hero: boolean, x: number, y: number, dim: boolean): void {
  ctx.save()
  ctx.globalAlpha = dim ? .38 : 1
  ctx.fillStyle = hero ? '#f4d26a' : actor?.color ?? '#d6dce8'
  ctx.fillRect(x * CELL_WIDTH + 2, y * CELL_HEIGHT + 2, 6, 9)
  ctx.restore()
}

function fallbackProp(ctx: CanvasRenderingContext2D, prop: Prop, x: number, y: number, dim: boolean): void {
  const definition = propDefinition(prop.kind)
  ctx.save()
  ctx.globalAlpha = dim ? .38 : 1
  ctx.fillStyle = definition.color
  ctx.fillRect(x * CELL_WIDTH + 2, y * CELL_HEIGHT + 3, 7, 7)
  ctx.restore()
}
