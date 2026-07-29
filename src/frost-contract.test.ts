import { describe, expect, it } from 'vitest'
import { advance, moveHero } from './engine/combat'
import { advanceGuardianPhase } from './engine/guardians'
import { newRun } from './engine'
import { fieldReadout } from './engine/readout'
import { refreshFov } from './engine/visibility'
import { measureGeneration } from './generation-metrics'
import { propDefinitionsFor } from './props'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 1, 2, 3, 4]

describe('Frost Basin generation contract', () => {
  it('realizes lake, ridge, crack, shore, and refuge landscapes with stable and exposed routes', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'frostReliquary', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['frozen-lake-crossing', 'ridge-hollow-loop', 'pressure-crack-maze', 'shore-reliquary-route', 'storm-refuge-chain']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['broad', 'looped', 'directedFlow', 'vertical', 'tight']))
    expect(propDefinitionsFor('frostReliquary').map(prop => prop.name)).toEqual(expect.arrayContaining(['wind shelter', 'trail marker', 'ice bridge', 'broken sled']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(safe.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'floor')).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'ice')).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'frostRime')).toBe(true)
      expect(['duelBell', 'rimeSarcophagus', 'iceForge', 'frozenCache'].every(id => floor.props.some(prop => prop.kind === `frostReliquary.${id}`))).toBe(true)
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'visibility', route: 'costly', original: 'ice', effect: 'frostRime', state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:visibility')).toMatchObject({ usedFallback: false })
      const offering = floor.rewardOffers?.find(offer => offer.milestoneId === 'boon-payoff')
      expect(offering?.choices.every(choice => choice.terrain === 'ice' && choice.route === 'optional')).toBe(true)
      costly.cells.forEach(point => { const tile = getTile(floor, point.x, point.y); if (tile?.kind === 'ice' || tile?.kind === 'frostRime') tile.kind = 'wall' })
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    }
  }, 30_000)

  it('escalates under-ice caves and refuge shelves from F1 through F4', () => {
    const floors = Array.from({ length: 4 }, (_, areaFloor) => generateAreaFloor(91, 'frostReliquary', areaFloor, 3))
    expect(floors.map(floor => floor.sideSpaces?.length)).toEqual([1, 2, 3, 4])
    for (const floor of floors) {
      expect(floor.frostLayout).toMatchObject({ shelves: expect.any(Array), cracks: expect.any(Array), shelters: expect.any(Array) })
      expect(floor.frostLayout!.shelves.length).toBeGreaterThan(8)
      expect(floor.frostLayout!.shelters.length).toBeGreaterThan(0)
      for (const cave of floor.sideSpaces ?? []) {
        expect(cave.kind).toBe('frost-cave')
        if (cave.kind !== 'frost-cave') throw new Error('missing frost cave')
        expect(getTile(floor, cave.entry.x, cave.entry.y)?.kind).toBe('breakwall')
        expect(cave.chamber.length).toBeGreaterThan(8)
        expect(cave.chamber.some(point => getTile(floor, point.x, point.y)?.kind === 'ice')).toBe(true)
      }
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('places fast ice pursuers and telegraphed whiteout artillery on every ordinary floor', () => {
    for (let areaFloor = 0; areaFloor < 3; areaFloor++) {
      const floor = generateAreaFloor(42, 'frostReliquary', areaFloor, 3)
      const hound = floor.actors.find(actor => actor.kind === 'shardHound' && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))
      const oracle = floor.actors.find(actor => actor.kind === 'whiteoutOracle' && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))
      expect(hound).toBeDefined()
      expect(getTile(floor, hound!.x, hound!.y)?.kind).toBe('ice')
      expect(hound?.encounter?.answer).toContain('wind shelter')
      expect(oracle).toMatchObject({ ai: 'ranged' })
      expect(getTile(floor, oracle!.x, oracle!.y)?.kind).toBe('ice')
      expect(oracle?.encounter?.answer).toContain('marked shore')
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('makes ice fast, rime harmful, and whiteout legible with a shelter response', () => {
    const ice = newRun(0, 'frostReliquary')
    ice.floor.actors = []
    ice.floor.props = []
    ice.floor.items = []
    ice.floor.milestones = []
    ice.floor.ecology = []
    ice.hero.skills = ['agi1']
    ice.hero.x = 4
    ice.hero.y = 4
    getTile(ice.floor, 4, 4)!.kind = 'floor'
    getTile(ice.floor, 5, 4)!.kind = 'ice'
    getTile(ice.floor, 6, 4)!.kind = 'ice'
    moveHero(ice, 'e')
    expect(ice.hero).toMatchObject({ x: 6, y: 4 })

    const rime = newRun(0, 'frostReliquary')
    rime.floor.actors = []
    rime.floor.props = []
    rime.floor.items = []
    rime.floor.milestones = []
    rime.floor.ecology = []
    rime.hero.x = 4
    rime.hero.y = 4
    getTile(rime.floor, 5, 4)!.kind = 'frostRime'
    const health = rime.hero.health
    moveHero(rime, 'e')
    expect(rime.hero.health).toBeLessThan(health)

    const whiteout = newRun(0, 'frostReliquary')
    whiteout.floor.actors = []
    advance(whiteout, [])
    expect(fieldReadout(whiteout).lines.some(line => line.startsWith('ECOLOGY: VISIBILITY T-') && line.includes('Whiteout gathers'))).toBe(true)
    const ecology = whiteout.floor.ecology![0]
    whiteout.turn = ecology.startsAt - 1
    advance(whiteout, [])
    expect(ecology).toMatchObject({ state: 'active', original: 'ice', effect: 'frostRime' })
    const shelter = whiteout.floor.props.find(prop => prop.kind === 'frostReliquary.duelBell')!
    whiteout.floor.props = []
    whiteout.hero.x = 5
    whiteout.hero.y = 5
    for (let x = 5; x <= 13; x++) getTile(whiteout.floor, x, 5)!.kind = 'floor'
    refreshFov(whiteout)
    expect(getTile(whiteout.floor, 13, 5)?.visible).toBe(false)
    shelter.x = 5
    shelter.y = 5
    whiteout.floor.props = [shelter]
    refreshFov(whiteout)
    expect(getTile(whiteout.floor, 13, 5)?.visible).toBe(true)
  })

  it('preserves the frozen reliquary escape through ice and rime phases', () => {
    const state = newRun(42, 'frostReliquary', 3)
    advance(state, [])
    const guardian = state.floor.actors.find(actor => actor.kind === 'reliquaryWarden')!
    guardian.health = Math.floor(guardian.maxHealth * .6)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'pressure', tile: 'ice' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    guardian.health = Math.floor(guardian.maxHealth * .3)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'cataclysm', tile: 'frostRime' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    expect(validateGeneration(state.floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
