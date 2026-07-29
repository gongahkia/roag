import { describe, expect, it } from 'vitest'
import { advance, moveHero } from './combat'
import { createEnemy, createRun } from '../test/factories'
import { getTile } from '../world'

const regent = (id: string, health: number, guardianPhase: 'opening' | 'pressure' | 'cataclysm') => createEnemy({ id, kind: 'regent', name: 'The Ash Regent', role: 'guardian', ai: 'guardian', x: 5, y: 1, health, maxHealth: 84, speed: 100, energy: 0, guardianPhase })

describe('Ash Regent encounter', () => {
  it('uses readable ward, decree, and judgment phases across prior systems', () => {
    const opening = createRun()
    const warded = regent('regent-opening', 84, 'opening')
    opening.floor.actors = [warded]
    advance(opening, [])
    expect(warded.conditions).toContainEqual({ kind: 'shielded', duration: 2, potency: 3 })

    const pressure = createRun()
    const decreeing = regent('regent-pressure', 56, 'opening')
    pressure.floor.actors = [decreeing]
    advance(pressure, [])
    expect(decreeing.guardianPhase).toBe('pressure')
    expect(pressure.floor.telegraphs).toMatchObject([{ actionId: 'regent-decree', sourceId: 'regent-pressure' }])
    advance(pressure, [])
    expect(pressure.hero.conditions).toContainEqual({ kind: 'marked', duration: 1, potency: 2 })
    expect(getTile(pressure.floor, 1, 1)?.kind).toBe('dart')

    const cataclysm = createRun()
    const judging = regent('regent-cataclysm', 28, 'pressure')
    cataclysm.floor.actors = [judging]
    advance(cataclysm, [])
    expect(judging.guardianPhase).toBe('cataclysm')
    expect(cataclysm.floor.tiles.some(tile => tile.kind === 'darkness')).toBe(true)
    expect(cataclysm.floor.telegraphs).toMatchObject([{ actionId: 'regent-judgment', sourceId: 'regent-cataclysm' }])
    moveHero(cataclysm, 's')
    expect(getTile(cataclysm.floor, 3, 1)?.kind).toBe('fireVent')
  })
})
