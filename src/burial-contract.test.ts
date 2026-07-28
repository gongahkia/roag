import { describe, expect, it } from 'vitest'
import { advance, moveHero } from './engine/combat'
import { advanceGuardianPhase } from './engine/guardians'
import { newRun } from './engine'
import { fieldReadout } from './engine/readout'
import { measureGeneration } from './generation-metrics'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 1, 2, 3, 11]

describe('Ancestor Fields generation contract', () => {
  it('realizes stone-circle, mound, cemetery-edge, ossuary, and ancestor-loop landscapes', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'burial', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['stone-circle-center', 'mound-procession', 'cemetery-settlement-edge', 'ossuary-hollow', 'ancestor-path-loop']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['broad', 'vertical', 'tight', 'directedFlow', 'looped']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(floor.tiles.filter(tile => tile.kind === 'cairn')).not.toHaveLength(0)
      expect(safe.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'spiritPath')).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'graveSoil')).toBe(true)
      expect(floor.tiles.some(tile => tile.kind === 'ossuary')).toBe(true)
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'migration', route: 'costly', effect: 'spiritPath', state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:migration')).toMatchObject({ usedFallback: false })
      const offering = floor.rewardOffers?.find(offer => offer.milestoneId === 'boon-payoff')
      expect(offering?.choices.every(choice => choice.terrain === 'spiritPath' && choice.route === 'optional')).toBe(true)
      costly.cells.forEach(point => { const tile = getTile(floor, point.x, point.y); if (tile?.kind === 'graveSoil') tile.kind = 'wall' })
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    }
  }, 30_000)

  it('places a tomb warden on the telegraphed trespass route of every ordinary floor', () => {
    for (let areaFloor = 0; areaFloor < 3; areaFloor++) {
      const floor = generateAreaFloor(42, 'burial', areaFloor, 3)
      const warden = floor.actors.find(actor => actor.kind === 'tombWarden' && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))
      expect(warden).toBeDefined()
      expect(getTile(floor, warden!.x, warden!.y)?.kind).toBe('graveSoil')
      expect(warden?.encounter?.answer).toContain('lit spirit path')
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('makes spirit paths a fast focus-restoring bypass and grave soil a slower trespass route', () => {
    const spirit = newRun(9, 'burial')
    spirit.floor.actors = []
    spirit.floor.props = []
    spirit.floor.items = []
    spirit.floor.milestones = []
    spirit.floor.ecology = []
    spirit.hero.skills = ['agi1']
    spirit.hero.focus = 0
    spirit.hero.x = 4
    spirit.hero.y = 4
    getTile(spirit.floor, 4, 4)!.kind = 'floor'
    getTile(spirit.floor, 5, 4)!.kind = 'spiritPath'
    getTile(spirit.floor, 6, 4)!.kind = 'spiritPath'
    moveHero(spirit, 'e')
    expect(spirit.hero).toMatchObject({ x: 6, y: 4, focus: 1 })

    const grave = newRun(9, 'burial')
    grave.floor.actors = []
    grave.floor.props = []
    grave.floor.items = []
    grave.floor.milestones = []
    grave.floor.ecology = []
    grave.hero.skills = ['agi1']
    grave.hero.x = 4
    grave.hero.y = 4
    getTile(grave.floor, 4, 4)!.kind = 'floor'
    getTile(grave.floor, 5, 4)!.kind = 'graveSoil'
    getTile(grave.floor, 6, 4)!.kind = 'spiritPath'
    moveHero(grave, 'e')
    expect(grave.hero).toMatchObject({ x: 5, y: 4 })
    expect(grave.messages.some(message => message.includes('grave soil slows'))).toBe(true)
  })

  it('telegraphs disturbed rites and preserves the Barrow King escape through ritual phases', () => {
    const state = newRun(42, 'burial', 3)
    advance(state, [])
    expect(fieldReadout(state).lines.some(line => line.startsWith('ECOLOGY: MIGRATION T-') && line.includes('trespass route'))).toBe(true)
    const guardian = state.floor.actors.find(actor => actor.kind === 'barrowKing')!
    guardian.health = Math.floor(guardian.maxHealth * .6)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'pressure', tile: 'spiritPath' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    guardian.health = Math.floor(guardian.maxHealth * .3)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'cataclysm', tile: 'graveSoil' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    expect(validateGeneration(state.floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
