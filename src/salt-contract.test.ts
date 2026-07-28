import { describe, expect, it } from 'vitest'
import { advance, moveHero } from './engine/combat'
import { advanceGuardianPhase } from './engine/guardians'
import { newRun } from './engine'
import { fieldReadout } from './engine/readout'
import { refreshFov } from './engine/visibility'
import { measureGeneration } from './generation-metrics'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 1, 2, 6, 8]

describe('Salt Expanse generation contract', () => {
  it('realizes crust, brine, caravan, mirror, and ridge families with stable and hazardous routes', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'saltFlats', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['crust-island-chain', 'brine-maze', 'caravan-causeway', 'mirror-basin-loop', 'salt-ridge-refuge']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['broad', 'directedFlow', 'vertical', 'looped', 'tight']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(safe.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'floor')).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'brine')).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'saltMirror')).toBe(true)
      expect(floor.props.some(prop => prop.kind === 'saltFlats.caravanHusk')).toBe(true)
      expect(floor.props.some(prop => prop.kind === 'saltFlats.glassMarker')).toBe(true)
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'visibility', route: 'costly', original: 'saltMirror', effect: 'darkness', state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:visibility')).toMatchObject({ usedFallback: false })
      const offering = floor.rewardOffers?.find(offer => offer.milestoneId === 'boon-payoff')
      expect(offering?.choices.every(choice => choice.terrain === 'brine' && choice.route === 'optional')).toBe(true)
      costly.cells.forEach(point => { const tile = getTile(floor, point.x, point.y); if (tile?.kind === 'brine') tile.kind = 'wall' })
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    }
  }, 30_000)

  it('places a mirage skirmisher on a reflective horizon on every ordinary floor', () => {
    for (let areaFloor = 0; areaFloor < 3; areaFloor++) {
      const floor = generateAreaFloor(42, 'saltFlats', areaFloor, 3)
      const skirmisher = floor.actors.find(actor => actor.kind === 'mirageSkirmisher' && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))
      expect(skirmisher).toBeDefined()
      expect(getTile(floor, skirmisher!.x, skirmisher!.y)?.kind).toBe('saltMirror')
      expect(skirmisher?.encounter?.answer).toContain('caravan refuge')
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('makes mirrors fast, brine damaging, and heat haze legible before it reduces sight', () => {
    const mirror = newRun(0, 'saltFlats')
    mirror.floor.actors = []
    mirror.floor.props = []
    mirror.floor.items = []
    mirror.floor.milestones = []
    mirror.floor.ecology = []
    mirror.hero.skills = ['agi1']
    mirror.hero.x = 4
    mirror.hero.y = 4
    getTile(mirror.floor, 4, 4)!.kind = 'floor'
    getTile(mirror.floor, 5, 4)!.kind = 'saltMirror'
    getTile(mirror.floor, 6, 4)!.kind = 'saltMirror'
    moveHero(mirror, 'e')
    expect(mirror.hero).toMatchObject({ x: 6, y: 4 })

    const brine = newRun(0, 'saltFlats')
    brine.floor.actors = []
    brine.floor.props = []
    brine.floor.items = []
    brine.floor.milestones = []
    brine.floor.ecology = []
    brine.hero.x = 4
    brine.hero.y = 4
    getTile(brine.floor, 5, 4)!.kind = 'brine'
    const health = brine.hero.health
    moveHero(brine, 'e')
    expect(brine.hero.health).toBeLessThan(health)

    const haze = newRun(0, 'saltFlats')
    haze.floor.actors = []
    advance(haze, [])
    expect(fieldReadout(haze).lines.some(line => line.startsWith('ECOLOGY: VISIBILITY T-') && line.includes('Heat haze gathers'))).toBe(true)
    const ecology = haze.floor.ecology![0]
    haze.turn = ecology.startsAt - 1
    advance(haze, [])
    expect(ecology.state).toBe('active')
    const husk = haze.floor.props.find(prop => prop.kind === 'saltFlats.caravanHusk')!
    haze.floor.props = []
    haze.hero.x = 5
    haze.hero.y = 5
    for (let x = 5; x <= 13; x++) getTile(haze.floor, x, 5)!.kind = 'floor'
    refreshFov(haze)
    expect(getTile(haze.floor, 13, 5)?.visible).toBe(false)
    husk.x = 5
    husk.y = 5
    haze.floor.props = [husk]
    refreshFov(haze)
    expect(getTile(haze.floor, 13, 5)?.visible).toBe(true)
  })

  it('preserves the Salt Sovereign route through mirror and brine phases', () => {
    const state = newRun(42, 'saltFlats', 3)
    advance(state, [])
    const guardian = state.floor.actors.find(actor => actor.kind === 'saltSovereign')!
    guardian.health = Math.floor(guardian.maxHealth * .6)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'pressure', tile: 'saltMirror' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    guardian.health = Math.floor(guardian.maxHealth * .3)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'cataclysm', tile: 'brine' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    expect(validateGeneration(state.floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
