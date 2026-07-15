import { describe, expect, it } from 'vitest'
import { runAutoplay } from './autoplay-runner'
import { newRun } from './engine'
import type { Biome } from './types'
import { generateAreaFloor, validateGeneration } from './world'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins']

describe('autoplay smoke', () => {
  it('validates generated layouts over mixed seeds, biomes, and floors', () => {
    for (const seed of [7, 42, 999]) for (const biome of biomes) for (let floor = 0; floor < 4; floor++) expect(validateGeneration(generateAreaFloor(seed, biome, floor))).toEqual({ valid: true, errors: [] })
  })

  it('runs deterministic visible and full-map agents without engine errors', () => {
    const outcomes = new Map<string, number>()
    const reports: string[] = []
    for (const seed of [7, 42]) for (const biome of biomes) for (const mode of ['visible', 'omniscient'] as const) {
      const initial = newRun(seed, biome)
      const first = runAutoplay(initial, { mode, turnLimit: mode === 'visible' ? 350 : 600 })
      const second = runAutoplay(initial, { mode, turnLimit: mode === 'visible' ? 350 : 600 })
      expect(first.outcome).not.toBe('error')
      expect(first).toEqual(second)
      expect(first.commands.length).toBeGreaterThan(0)
      outcomes.set(`${mode}:${first.outcome}`, (outcomes.get(`${mode}:${first.outcome}`) ?? 0) + 1)
      reports.push(`${mode}/${biome}/${seed}: ${first.outcome} at ${first.turns} (${first.trace.slice(-6).map(entry => `${entry.command}/${entry.reason}/${entry.events.join('+') || '-'}`).join(' | ')})`)
    }
    if (process.env.AUTOPLAY_TRACE === '1') {
      console.info(`autoplay smoke outcomes: ${[...outcomes].map(([outcome, count]) => `${outcome}=${count}`).join(' ')}`)
      console.info(`autoplay smoke detail:\n${reports.join('\n')}`)
    }
  }, 180_000)
})
