import type { GameEvent } from '../engine'
import type { RunState } from '../types'

interface Particle { x: number; y: number; vx: number; vy: number; life: number; maxLife: number; color: string; size: number }
interface FloatText { x: number; y: number; life: number; maxLife: number; color: string; text: string }

export class TerminalEffects {
  private shakeUntil = 0
  private flashUntil = 0
  private flashColor = '#ffffff'
  private particles: Particle[] = []
  private floats: FloatText[] = []
  private lastUpdate = performance.now()

  constructor(private readonly cellWidth: number, private readonly cellHeight: number, private readonly mapWidth: number, private readonly mapHeight: number) {}

  trigger(events: GameEvent[], state: RunState | undefined, canvas: HTMLCanvasElement): void {
    const now = performance.now()
    const point = state ? { x: state.hero.x * this.cellWidth + this.cellWidth / 2, y: state.hero.y * this.cellHeight + this.cellHeight / 2 } : { x: canvas.width / 2, y: canvas.height / 2 }
    if (events.includes('death') || events.includes('boom')) { this.shakeUntil = Math.max(this.shakeUntil, now + 210); this.flashUntil = Math.max(this.flashUntil, now + 105); this.flashColor = events.includes('death') ? '#ef5968' : '#ffe58a' }
    else if (events.includes('hurt') || events.includes('danger')) { this.shakeUntil = Math.max(this.shakeUntil, now + 115); this.flashUntil = Math.max(this.flashUntil, now + 70); this.flashColor = '#ef5968' }
    else if (events.includes('hit') || events.includes('spell') || events.includes('pickup')) { this.flashUntil = Math.max(this.flashUntil, now + 48); this.flashColor = events.includes('spell') ? '#bea6ff' : '#f4d26a' }
    if (events.includes('hit')) this.burst(point.x, point.y, '#f4d26a', 8, .8, 410, 'HIT')
    if (events.includes('pickup')) this.burst(point.x, point.y, '#96d38b', 10, .7, 540, 'LOOT')
    if (events.includes('spell')) this.burst(point.x, point.y, '#bea6ff', 15, 1.25, 650, 'ARCANE')
    if (events.includes('boom')) this.burst(point.x, point.y, '#ff9a61', 28, 2.4, 780, 'BOOM')
  }

  update(now: number): void {
    const delta = Math.min(34, now - this.lastUpdate)
    this.lastUpdate = now
    for (const particle of this.particles) {
      particle.x += particle.vx * delta
      particle.y += particle.vy * delta
      particle.vy += .002 * delta
      particle.life -= delta
    }
    for (const floating of this.floats) { floating.y -= .018 * delta; floating.life -= delta }
    this.particles = this.particles.filter(particle => particle.life > 0)
    this.floats = this.floats.filter(floating => floating.life > 0)
  }

  applyShake(ctx: CanvasRenderingContext2D, now: number): void {
    if (now >= this.shakeUntil) return
    const scale = Math.max(1, (this.shakeUntil - now) / 30)
    ctx.translate((Math.random() - .5) * scale, (Math.random() - .5) * scale)
  }

  drawFlash(ctx: CanvasRenderingContext2D, canvas: HTMLCanvasElement, now: number): void {
    if (now >= this.flashUntil) return
    ctx.save()
    ctx.globalAlpha = Math.max(.04, (this.flashUntil - now) / 280)
    ctx.fillStyle = this.flashColor
    ctx.fillRect(0, 0, canvas.width, canvas.height)
    ctx.restore()
  }

  drawMap(ctx: CanvasRenderingContext2D): void {
    ctx.save()
    ctx.beginPath()
    ctx.rect(0, 0, this.mapWidth * this.cellWidth, this.mapHeight * this.cellHeight)
    ctx.clip()
    for (const particle of this.particles) {
      ctx.globalAlpha = particle.life / particle.maxLife
      ctx.fillStyle = particle.color
      ctx.fillRect(Math.round(particle.x), Math.round(particle.y), particle.size, particle.size)
    }
    ctx.font = 'bold 11px ui-monospace, SFMono-Regular, Menlo, monospace'
    ctx.textAlign = 'center'
    for (const floating of this.floats) {
      ctx.globalAlpha = floating.life / floating.maxLife
      ctx.fillStyle = floating.color
      ctx.fillText(floating.text, floating.x, floating.y)
    }
    ctx.restore()
  }

  needsFrame(now: number): boolean { return now < Math.max(this.shakeUntil, this.flashUntil) || this.particles.length > 0 || this.floats.length > 0 }

  private burst(x: number, y: number, color: string, count: number, speed: number, life: number, text: string): void {
    for (let i = 0; i < count; i++) {
      const angle = Math.random() * Math.PI * 2
      const magnitude = (.35 + Math.random()) * speed / 1000
      this.particles.push({ x, y, vx: Math.cos(angle) * magnitude, vy: Math.sin(angle) * magnitude - .2, life, maxLife: life, color, size: Math.random() > .72 ? 2 : 1 })
    }
    this.floats.push({ x, y: y - 7, life, maxLife: life, color, text })
  }
}
