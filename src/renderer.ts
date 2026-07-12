import { BlockType, type CameraState, type PlayerState, WORLD_HEIGHT, WORLD_SIZE } from './types'
import type { BlockWorld } from './world'

const W = 16
const H = 8
const BH = 12
const palette: Record<number, [string, string, string]> = {
  [BlockType.Grass]: ['#90bd4c', '#5d8a39', '#456c35'],
  [BlockType.Soil]: ['#a45e3b', '#743f32', '#59302d'],
  [BlockType.Stone]: ['#939ca0', '#697377', '#4c555b'],
  [BlockType.Sand]: ['#e4c86d', '#c9a851', '#a88742'],
  [BlockType.Water]: ['#67b5d5', '#3d80bc', '#30639a'],
  [BlockType.Trunk]: ['#8d6441', '#61432e', '#472f29'],
  [BlockType.Leaf]: ['#5e9650', '#3e7041', '#2f5738'],
  [BlockType.Brick]: ['#c4574b', '#913b3b', '#6e3034'],
  [BlockType.Plank]: ['#dda15d', '#af7145', '#835038']
}

export interface HoveredBlock { x: number; y: number; z: number }

const transform = (x: number, z: number, rotation: number): [number, number] => {
  switch (rotation) {
    case 1: return [z, WORLD_SIZE - 1 - x]
    case 2: return [WORLD_SIZE - 1 - x, WORLD_SIZE - 1 - z]
    case 3: return [WORLD_SIZE - 1 - z, x]
    default: return [x, z]
  }
}

export class Renderer {
  readonly canvas: HTMLCanvasElement
  private readonly ctx: CanvasRenderingContext2D
  private width = 960
  private height = 640
  private centerX = 480
  private centerY = 165
  private readonly occlusionCanvas = document.createElement('canvas')
  private readonly playerMaskCanvas = document.createElement('canvas')

  constructor(canvas: HTMLCanvasElement) {
    this.canvas = canvas
    const ctx = canvas.getContext('2d')
    if (!ctx) throw new Error('Canvas 2D unavailable')
    this.ctx = ctx
    this.resize()
    new ResizeObserver(() => this.resize()).observe(canvas)
  }

  draw(world: BlockWorld, player: PlayerState, camera: CameraState, hover?: HoveredBlock): void {
    const { ctx } = this
    ctx.clearRect(0, 0, this.width, this.height)
    const sky = ctx.createLinearGradient(0, 0, 0, this.height)
    sky.addColorStop(0, '#4a718f')
    sky.addColorStop(1, '#17202c')
    ctx.fillStyle = sky
    ctx.fillRect(0, 0, this.width, this.height)
    ctx.fillStyle = '#90b65c'
    ctx.beginPath()
    ctx.arc(this.width * .78, 74, 26, 0, Math.PI * 2)
    ctx.fill()
    const blocks: HoveredBlock[] = []
    for (let z = 0; z < WORLD_SIZE; z++) for (let x = 0; x < WORLD_SIZE; x++) for (let y = 0; y < WORLD_HEIGHT; y++) {
      if (world.get(x, y, z) !== BlockType.Air) blocks.push({ x, y, z })
    }
    blocks.sort((a, b) => {
      const [ax, az] = transform(a.x, a.z, camera.rotation)
      const [bx, bz] = transform(b.x, b.z, camera.rotation)
      return (ax + az + a.y * .01) - (bx + bz + b.y * .01)
    })
    const [playerX, playerZ] = transform(player.x, player.z, camera.rotation)
    const playerDepth = playerX + playerZ + player.y * .01
    let drewPlayer = false
    const foreground: HoveredBlock[] = []
    for (const block of blocks) {
      const [x, z] = transform(block.x, block.z, camera.rotation)
      if (!drewPlayer && x + z + block.y * .01 > playerDepth) {
        this.player(player, camera)
        drewPlayer = true
      }
      this.block(world.get(block.x, block.y, block.z), block.x, block.y, block.z, camera, hover?.x === block.x && hover.y === block.y && hover.z === block.z)
      if (drewPlayer) foreground.push(block)
    }
    if (!drewPlayer) this.player(player, camera)
    if (drewPlayer && this.occluded(player, camera, foreground)) this.outline(player, camera)
    this.vignette()
  }

  pick(world: BlockWorld, camera: CameraState, px: number, py: number): HoveredBlock | undefined {
    const candidates: HoveredBlock[] = []
    for (let z = 0; z < WORLD_SIZE; z++) for (let x = 0; x < WORLD_SIZE; x++) for (let y = 0; y < WORLD_HEIGHT; y++) {
      if (world.get(x, y, z) === BlockType.Air) continue
      const [sx, sy] = this.point(x, y, z, camera)
      if (diamond(px, py, sx, sy - BH * camera.zoom, W * camera.zoom, H * camera.zoom)) candidates.push({ x, y, z })
    }
    return candidates.sort((a, b) => {
      const [ax, az] = transform(a.x, a.z, camera.rotation)
      const [bx, bz] = transform(b.x, b.z, camera.rotation)
      return (bx + bz + b.y * .01) - (ax + az + a.y * .01)
    })[0]
  }

  private point(x: number, y: number, z: number, camera: CameraState): [number, number] {
    const [rx, rz] = transform(x, z, camera.rotation)
    const zoom = camera.zoom
    return [this.centerX + (rx - rz) * W * zoom, this.centerY + (rx + rz) * H * zoom - y * BH * zoom]
  }

  private block(type: BlockType, x: number, y: number, z: number, camera: CameraState, highlighted: boolean): void {
    const colors = palette[type]
    if (!colors) return
    const [cx, cy] = this.point(x, y, z, camera)
    const scale = camera.zoom
    const w = W * scale
    const h = H * scale
    const bh = BH * scale
    const { ctx } = this
    ctx.beginPath()
    ctx.moveTo(cx, cy - bh - h)
    ctx.lineTo(cx + w, cy - bh)
    ctx.lineTo(cx, cy - bh + h)
    ctx.lineTo(cx - w, cy - bh)
    ctx.closePath()
    ctx.fillStyle = colors[0]
    ctx.fill()
    ctx.beginPath()
    ctx.moveTo(cx - w, cy - bh)
    ctx.lineTo(cx, cy - bh + h)
    ctx.lineTo(cx, cy + h)
    ctx.lineTo(cx - w, cy)
    ctx.closePath()
    ctx.fillStyle = colors[1]
    ctx.fill()
    ctx.beginPath()
    ctx.moveTo(cx + w, cy - bh)
    ctx.lineTo(cx, cy - bh + h)
    ctx.lineTo(cx, cy + h)
    ctx.lineTo(cx + w, cy)
    ctx.closePath()
    ctx.fillStyle = colors[2]
    ctx.fill()
    if (type === BlockType.Water) {
      ctx.strokeStyle = '#a5d7e4'
      ctx.lineWidth = Math.max(1, scale)
      ctx.beginPath()
      ctx.moveTo(cx - w * .55, cy - bh)
      ctx.lineTo(cx + w * .35, cy - bh)
      ctx.stroke()
    }
    if (highlighted) {
      ctx.strokeStyle = '#fff5ad'
      ctx.lineWidth = 2 * scale
      ctx.beginPath()
      ctx.moveTo(cx, cy - bh - h)
      ctx.lineTo(cx + w, cy - bh)
      ctx.lineTo(cx, cy - bh + h)
      ctx.lineTo(cx - w, cy - bh)
      ctx.closePath()
      ctx.stroke()
    }
  }

  private player(player: PlayerState, camera: CameraState): void {
    const [x, y] = this.point(player.x, player.y, player.z, camera)
    const s = camera.zoom
    const { ctx } = this
    ctx.fillStyle = '#28283a'
    ctx.fillRect(x - 4 * s, y - 25 * s, 8 * s, 14 * s)
    ctx.fillStyle = '#edbc92'
    ctx.fillRect(x - 4 * s, y - 32 * s, 8 * s, 8 * s)
    ctx.fillStyle = '#f0d25f'
    ctx.fillRect(x - 5 * s, y - 38 * s, 10 * s, 7 * s)
    ctx.fillStyle = '#15151f'
    ctx.fillRect(x - 3 * s, y - 11 * s, 2 * s, 8 * s)
    ctx.fillRect(x + 1 * s, y - 11 * s, 2 * s, 8 * s)
  }

  private occluded(player: PlayerState, camera: CameraState, blocks: HoveredBlock[]): boolean {
    if (!blocks.length) return false
    const [x, y] = this.point(player.x, player.y, player.z, camera)
    const scale = camera.zoom
    const width = Math.ceil(22 * scale)
    const height = Math.ceil(40 * scale)
    const originX = x - 11 * scale
    const originY = y - 40 * scale
    const mask = this.occlusionCanvas
    const playerMask = this.playerMaskCanvas
    mask.width = playerMask.width = width
    mask.height = playerMask.height = height
    const maskCtx = mask.getContext('2d')!
    const playerCtx = playerMask.getContext('2d')!
    for (const block of blocks) this.blockMask(maskCtx, block, camera, originX, originY)
    maskCtx.globalCompositeOperation = 'destination-in'
    this.playerMask(maskCtx, x - originX, y - originY, scale)
    maskCtx.globalCompositeOperation = 'source-over'
    this.playerMask(playerCtx, x - originX, y - originY, scale)
    const covered = maskCtx.getImageData(0, 0, width, height).data
    const total = playerCtx.getImageData(0, 0, width, height).data
    let hidden = 0
    let body = 0
    for (let i = 3; i < total.length; i += 4) {
      if (total[i] > 0) {
        body++
        if (covered[i] > 0) hidden++
      }
    }
    return body > 0 && hidden / body > .75
  }

  private blockMask(ctx: CanvasRenderingContext2D, block: HoveredBlock, camera: CameraState, originX: number, originY: number): void {
    const [cx, cy] = this.point(block.x, block.y, block.z, camera)
    const w = W * camera.zoom
    const h = H * camera.zoom
    const bh = BH * camera.zoom
    ctx.fillStyle = '#fff'
    ctx.beginPath()
    ctx.moveTo(cx - originX, cy - originY - bh - h)
    ctx.lineTo(cx + w - originX, cy - originY - bh)
    ctx.lineTo(cx + w - originX, cy - originY)
    ctx.lineTo(cx - originX, cy - originY + h)
    ctx.lineTo(cx - w - originX, cy - originY)
    ctx.lineTo(cx - w - originX, cy - originY - bh)
    ctx.closePath()
    ctx.fill()
  }

  private playerMask(ctx: CanvasRenderingContext2D, x: number, y: number, s: number): void {
    ctx.fillStyle = '#fff'
    ctx.fillRect(x - 4 * s, y - 25 * s, 8 * s, 14 * s)
    ctx.fillRect(x - 4 * s, y - 32 * s, 8 * s, 8 * s)
    ctx.fillRect(x - 5 * s, y - 38 * s, 10 * s, 7 * s)
    ctx.fillRect(x - 3 * s, y - 11 * s, 2 * s, 8 * s)
    ctx.fillRect(x + 1 * s, y - 11 * s, 2 * s, 8 * s)
  }

  private outline(player: PlayerState, camera: CameraState): void {
    const [x, y] = this.point(player.x, player.y, player.z, camera)
    const s = camera.zoom
    const { ctx } = this
    ctx.strokeStyle = '#fff'
    ctx.lineWidth = Math.max(1.5, 2 * s)
    ctx.lineJoin = 'round'
    ctx.beginPath()
    ctx.moveTo(x - 5 * s, y - 38 * s)
    ctx.lineTo(x + 5 * s, y - 38 * s)
    ctx.lineTo(x + 5 * s, y - 31 * s)
    ctx.lineTo(x + 4 * s, y - 31 * s)
    ctx.lineTo(x + 4 * s, y - 11 * s)
    ctx.lineTo(x + 3 * s, y - 11 * s)
    ctx.lineTo(x + 3 * s, y - 3 * s)
    ctx.lineTo(x + 1 * s, y - 3 * s)
    ctx.lineTo(x + 1 * s, y - 11 * s)
    ctx.lineTo(x - 1 * s, y - 11 * s)
    ctx.lineTo(x - 1 * s, y - 3 * s)
    ctx.lineTo(x - 3 * s, y - 3 * s)
    ctx.lineTo(x - 3 * s, y - 11 * s)
    ctx.lineTo(x - 4 * s, y - 11 * s)
    ctx.lineTo(x - 4 * s, y - 31 * s)
    ctx.lineTo(x - 5 * s, y - 31 * s)
    ctx.closePath()
    ctx.stroke()
  }

  private vignette(): void {
    const { ctx } = this
    const v = ctx.createRadialGradient(this.centerX, this.height * .43, this.height * .2, this.centerX, this.height * .43, this.height * .85)
    v.addColorStop(.65, 'rgba(0,0,0,0)')
    v.addColorStop(1, 'rgba(5,8,15,.55)')
    ctx.fillStyle = v
    ctx.fillRect(0, 0, this.width, this.height)
  }

  private resize(): void {
    const rect = this.canvas.getBoundingClientRect()
    const ratio = Math.min(window.devicePixelRatio || 1, 2)
    this.width = Math.max(320, Math.floor(rect.width * ratio))
    this.height = Math.max(320, Math.floor(rect.height * ratio))
    this.canvas.width = this.width
    this.canvas.height = this.height
    this.centerX = this.width / 2
    this.centerY = Math.max(130, this.height * .21)
    this.ctx.imageSmoothingEnabled = false
  }
}

const diamond = (x: number, y: number, cx: number, cy: number, w: number, h: number) => Math.abs(x - cx) / w + Math.abs(y - cy) / h <= 1
