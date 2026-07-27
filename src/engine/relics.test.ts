import { describe, expect, it } from 'vitest'
import { createEnemy, createHero, createRun } from '../test/factories'
import { resolveDefeatedActors } from './combat'
import { perform } from './input'
import { relicAlignment, relicChoices } from './relics'

const relicSite = { id: 'relic-site', kind: 'relic' as const, x: 1, y: 1, discovered: true, claimed: false }

describe('guardian relics', () => {
  it('offers a deterministic run-only relic at a claimed guardian echo', () => {
    const state = createRun()
    state.floor.milestones = [relicSite]
    const offer = relicChoices(state, relicSite)[0]
    perform(state, 'c'); perform(state, '1')
    expect(state.floor.milestones[0].claimed).toBe(true)
    expect(state.hero.relics).toEqual([offer.id])
    expect(state.telemetry?.relicPicks).toEqual({ [offer.id]: 1 })
    expect(state.alignment?.[relicAlignment(offer.id)]).toBe(1)
  })

  it('creates one echo after a guardian dies', () => {
    const guardian = createEnemy({ id: 'foreman-echo', kind: 'foreman', role: 'guardian', ai: 'guardian', x: 4, y: 4, health: 0 })
    const state = createRun({ floor: createRun().floor })
    state.floor.actors = [guardian]
    resolveDefeatedActors(state)
    expect(state.floor.milestones).toContainEqual(expect.objectContaining({ kind: 'relic', x: 4, y: 4, discovered: true }))
    expect(state.telemetry?.enemyKills.foreman).toBe(1)
  })

  it('turns Markbreaker Seal into a two-hit finisher', () => {
    const enemy = createEnemy({ x: 2, y: 1, health: 100, maxHealth: 100, defense: 1, speed: 0 })
    const state = createRun({ hero: createHero({ relics: ['markbreakerSeal'], stats: { strength: 20, agility: 2, vitality: 2, intellect: 2 } }) })
    state.floor.actors = [enemy]
    perform(state, ';')
    expect(enemy.conditions).toContainEqual(expect.objectContaining({ kind: 'marked' }))
    perform(state, ';')
    expect(enemy.conditions?.some(condition => condition.kind === 'marked')).toBe(false)
    expect(state.messages).toContain('Markbreaker Seal ruptures the mark.')
  })
})
