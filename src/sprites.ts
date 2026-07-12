import { ITEM } from './content'
import type { Actor, Tile } from './types'

const size = 8
const tileColor: Record<string, string> = {
  wall: '#798795', floor: '#4f5c6c', exit: '#f4d26a', door: '#c9935e', lockedDoor: '#e9c965', water: '#559dcc', lava: '#eb7258', pit: '#05070b', rope: '#d6a867', spikes: '#d4dae2', dart: '#d4dae2', fireVent: '#ff825e', crumble: '#99795f', boulder: '#abb0b4', web: '#d3d8e4', gas: '#8dbd82', crate: '#c99162', chest: '#f4d26a', altar: '#cda2e3', shop: '#f4d26a', rescue: '#83d6af'
}

export const ATLAS_SPEC = { cell: 16, columns: 16, rows: 8, path: 'public/sprites/expedition-atlas.png' } as const

export function drawTileSprite(ctx: CanvasRenderingContext2D, tile: Tile, x: number, y: number, dim: boolean): void {
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
