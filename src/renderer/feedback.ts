import type { GameEventType } from '../engine'

export interface ParticleFeedback { color: string; count: number; speed: number; life: number }
export interface VisualFeedback { shake: number; flash: number; color: string; particles?: ParticleFeedback }

const feedback: Partial<Record<GameEventType, VisualFeedback>> = {
  hit: { shake: 35, flash: 48, color: '#f4d26a', particles: { color: '#f4d26a', count: 8, speed: .8, life: 410 } },
  hurt: { shake: 115, flash: 70, color: '#f0a45d', particles: { color: '#f0a45d', count: 5, speed: .5, life: 260 } },
  spell: { shake: 0, flash: 48, color: '#bea6ff', particles: { color: '#bea6ff', count: 15, speed: 1.25, life: 650 } },
  pickup: { shake: 0, flash: 48, color: '#96d38b', particles: { color: '#96d38b', count: 10, speed: .7, life: 540 } },
  boom: { shake: 210, flash: 105, color: '#ff9a61', particles: { color: '#ff9a61', count: 28, speed: 2.4, life: 780 } },
  danger: { shake: 115, flash: 70, color: '#d2a4e8' },
  level: { shake: 30, flash: 170, color: '#f4d26a', particles: { color: '#f4d26a', count: 26, speed: 1.5, life: 900 } },
  death: { shake: 210, flash: 105, color: '#d2a4e8', particles: { color: '#d2a4e8', count: 18, speed: 1.5, life: 700 } }
}

export const visualFeedback = (type: GameEventType): VisualFeedback | undefined => feedback[type]
