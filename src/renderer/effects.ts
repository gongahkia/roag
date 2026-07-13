import type { ActionResult } from '../engine'
import type { RunState } from '../types'
import { visualFeedback } from './feedback'

interface Particle { x: number; y: number; vx: number; vy: number; life: number; maxLife: number; color: string; size: number }
export const flashDuration = (duration: number, reducedFlash: boolean): number => reducedFlash ? Math.max(1, Math.round(duration / 4)) : duration

export class TerminalEffects {
  private shakeUntil = 0
  private flashUntil = 0
  private flashColor = '#ffffff'
  private particles: Particle[] = []
  private lastUpdate = performance.now()
  private reducedFlash = false

  constructor(private readonly cellWidth: number, private readonly cellHeight: number, private readonly mapWidth: number, private readonly mapHeight: number) {}
  setReducedFlash(value: boolean): void { this.reducedFlash = value }

  trigger(events: ActionResult, state: RunState | undefined, canvas: HTMLCanvasElement): void {
    const now = performance.now()
    const point = state ? { x: state.hero.x * this.cellWidth + this.cellWidth / 2, y: state.hero.y * this.cellHeight + this.cellHeight / 2 } : { x: canvas.width / 2, y: canvas.height / 2 }
    for (const type of new Set(events.map(event => event.type))) {
      const feedback = visualFeedback(type)
      if (!feedback) continue
      this.shakeUntil = Math.max(this.shakeUntil, now + feedback.shake)
      this.flashUntil = Math.max(this.flashUntil, now + flashDuration(feedback.flash, this.reducedFlash))
      this.flashColor = feedback.color
      if (feedback.particles) this.burst(point.x, point.y, feedback.particles.color, feedback.particles.count, feedback.particles.speed, feedback.particles.life)
    }
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
    this.particles = this.particles.filter(particle => particle.life > 0)
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
    ctx.restore()
  }

  needsFrame(now: number): boolean { return now < Math.max(this.shakeUntil, this.flashUntil) || this.particles.length > 0 }

  private burst(x: number, y: number, color: string, count: number, speed: number, life: number): void {
    for (let i = 0; i < count; i++) {
      const angle = Math.random() * Math.PI * 2
      const magnitude = (.35 + Math.random()) * speed / 1000
      this.particles.push({ x, y, vx: Math.cos(angle) * magnitude, vy: Math.sin(angle) * magnitude - .2, life, maxLife: life, color, size: Math.random() > .72 ? 2 : 1 })
    }
  }
}
