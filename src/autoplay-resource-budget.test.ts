import { describe, expect, it } from 'vitest'
import { autoplayDecision, autoplayResourceDiagnostics, createAutoplayContext } from './autoplay'
import { runAutoplay } from './autoplay-runner'
import { createEnemy, createRun } from './test/factories'
import { indexOf } from './types'

const criticalRopeRoute = () => {
  const state = createRun()
  state.hero.ropes = 1
  state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true; tile.visible = true })
  state.floor.tiles[indexOf(1, 1)].kind = 'floor'
  state.floor.tiles[indexOf(1, 2)].kind = 'pit'
  state.floor.tiles[indexOf(1, 3)].kind = 'exit'
  state.floor.exit = { x: 1, y: 3 }
  state.floor.objective = { id: 'complete', kind: 'defeatGuardian', label: 'Clear the route', status: 'complete' }
  state.floor.guardianDefeated = true
  return state
}

const criticalKitRoute = () => {
  const state = createRun()
  state.hero.inventory = ['auger']
  state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true; tile.visible = true })
  state.floor.tiles[indexOf(1, 1)].kind = 'floor'
  state.floor.tiles[indexOf(2, 1)].kind = 'wall'
  state.floor.tiles[indexOf(3, 1)].kind = 'exit'
  state.floor.exit = { x: 3, y: 1 }
  state.floor.objective = { id: 'complete', kind: 'defeatGuardian', label: 'Clear the route', status: 'complete' }
  state.floor.guardianDefeated = true
  return state
}

const unsafeBombShortcut = () => {
  const state = createRun()
  state.hero.bombs = 1
  state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true; tile.visible = true })
  state.floor.tiles[indexOf(1, 1)].kind = 'floor'
  state.floor.tiles[indexOf(1, 2)].kind = 'floor'
  state.floor.tiles[indexOf(5, 1)].kind = 'exit'
  state.floor.exit = { x: 5, y: 1 }
  state.floor.objective = { id: 'complete', kind: 'defeatGuardian', label: 'Clear the route', status: 'complete' }
  state.floor.guardianDefeated = true
  state.floor.actors = [
    { ...createEnemy(), id: 'bomb-target', x: 3, y: 1 },
    { ...createEnemy(), id: 'unsafe-attacker', x: 1, y: 2 }
  ]
  return state
}

describe('autoplay resource budget', () => {
  it('selects a rope for a known critical route and records its rationale and delta', () => {
    const state = criticalRopeRoute()
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    const rope = decision?.resourceDiagnostics.find(entry => entry.action === 'rope')
    expect(rope).toMatchObject({ disposition: 'select', rationale: 'unlocks known critical route', projectedRouteGain: true, survivalProbability: 1, utilityScore: expect.any(Number), knownCriticalRoute: true, resourceDelta: { bombs: 0, ropes: -1, kit: 0 } })
    expect(decision).toMatchObject({ command: 'r', reason: 'secure critical rope route' })
    const report = runAutoplay(state, { mode: 'omniscient', policy: 'clear', turnLimit: 1, captureTrace: true })
    expect(report.traceDocument?.records[0]).toMatchObject({ resourceDiagnostics: expect.arrayContaining([expect.objectContaining({ action: 'rope', disposition: 'select', rationale: 'unlocks known critical route', resourceDelta: { bombs: 0, ropes: -1, kit: 0 }, rejectedAlternatives: expect.arrayContaining(['bomb:resource unavailable']) })]), resourceDelta: { ropes: -1 } })
    expect(report.resourceOutcomes).toMatchObject({ selected: 1, projectedRouteGains: 1, criticalRouteSelections: 1 })
  })

  it('defers optional loot, rejects an unsafe shortcut, and falls back without resources', () => {
    const optional = createRun()
    optional.hero.bombs = 1
    const bomb = autoplayResourceDiagnostics(optional, 'omniscient', 'clear').find(entry => entry.action === 'bomb')
    expect(bomb).toMatchObject({ disposition: 'reject', rationale: 'no projected route or survival gain' })
    const reserved = criticalRopeRoute()
    reserved.hero.bombs = 1
    expect(autoplayResourceDiagnostics(reserved, 'omniscient', 'clear').find(entry => entry.action === 'bomb')).toMatchObject({ disposition: 'defer', rationale: 'reserve for known critical route' })
    const unsafe = autoplayResourceDiagnostics(unsafeBombShortcut(), 'omniscient', 'clear').find(entry => entry.action === 'bomb')
    expect(unsafe).toMatchObject({ disposition: 'reject', rationale: 'unsafe shortcut', projectedRouteGain: true, survivalProbability: expect.any(Number) })
    const missing = autoplayResourceDiagnostics(createRun(), 'omniscient', 'clear')
    expect(missing.filter(entry => entry.action === 'bomb' || entry.action === 'rope')).toEqual(expect.arrayContaining([
      expect.objectContaining({ action: 'bomb', disposition: 'reject', rationale: 'resource unavailable' }),
      expect.objectContaining({ action: 'rope', disposition: 'reject', rationale: 'resource unavailable' })
    ]))
  })

  it('selects a relevant kit only when it opens a critical route', () => {
    const state = criticalKitRoute()
    const kit = autoplayResourceDiagnostics(state, 'omniscient', 'clear').find(entry => entry.action === 'kit')
    expect(kit).toMatchObject({ item: 'auger', disposition: 'select', projectedRouteGain: true, knownCriticalRoute: true, resourceDelta: { bombs: 0, ropes: 0, kit: -1 } })
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())).toMatchObject({ command: 'u', reason: 'drill route:auger' })
  })

  it('does not infer a hidden critical route in visible mode', () => {
    const state = criticalKitRoute()
    state.floor.tiles[indexOf(3, 1)].explored = false
    state.floor.tiles[indexOf(3, 1)].visible = false
    const kit = autoplayResourceDiagnostics(state, 'visible', 'clear').find(entry => entry.action === 'kit')
    expect(kit).toMatchObject({ item: 'auger', knownCriticalRoute: false, projectedRouteGain: false, disposition: 'reject' })
  })

  it('does not treat an undiscovered alternate path as a safe resource-free route', () => {
    const state = criticalKitRoute()
    for (const point of [{ x: 1, y: 2 }, { x: 2, y: 2 }, { x: 3, y: 2 }]) {
      const tile = state.floor.tiles[indexOf(point.x, point.y)]!
      tile.kind = 'floor'
      tile.explored = false
      tile.visible = false
    }
    const kit = autoplayResourceDiagnostics(state, 'visible', 'clear').find(entry => entry.action === 'kit')
    expect(kit).toMatchObject({ item: 'auger', knownCriticalRoute: true, projectedRouteGain: true, disposition: 'select' })
  })
})
