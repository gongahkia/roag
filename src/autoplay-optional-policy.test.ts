import { describe, expect, it } from 'vitest'
import { autoplayDecision, autoplayOptionalDiagnostics, createAutoplayContext } from './autoplay'
import { runAutoplay } from './autoplay-runner'
import { createEnemy, createRun } from './test/factories'
import { indexOf } from './types'

const optionalSecret = () => {
  const state = createRun()
  state.hero.bombs = 2
  state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true; tile.visible = true })
  for (const [x, y, kind] of [[1, 1, 'floor'], [2, 1, 'breakwall'], [3, 1, 'floor'], [1, 2, 'floor'], [1, 3, 'floor'], [1, 4, 'exit']] as const) state.floor.tiles[indexOf(x, y)].kind = kind
  state.floor.exit = { x: 1, y: 4 }
  state.floor.objective = { id: 'complete', kind: 'defeatGuardian', label: 'Clear the route', status: 'complete' }
  state.floor.guardianDefeated = true
  const reward = { id: 'sunblade', x: 3, y: 1, count: 1, visibleInFog: true }
  state.floor.items = [reward]
  state.floor.sideSpaces = [{ id: 'secret', kind: 'mine-breach-room', approach: { x: 1, y: 1 }, entry: { x: 2, y: 1 }, chamber: [{ x: 3, y: 1 }], reward }]
  state.floor.secretRooms = [{ version: 1, id: 'secret-room:secret', sourceId: 'secret', kind: 'hidden-room', approach: { x: 1, y: 1 }, entries: [{ x: 2, y: 1 }], chamber: [{ x: 3, y: 1 }], entryCondition: 'sealed-breakwall', discoveryClue: 'fractured rail stone', clueChannel: 'terrain', discovery: { channel: 'terrain', turn: 0 }, accessMethod: 'breach', rewardClass: 'supplies', risk: 'dust', safeFallback: true }]
  return state
}

describe('autoplay optional-secret expected value policy', () => {
  it('pursues a profitable visible secret and records its evidence in traces and reports', () => {
    const state = optionalSecret()
    expect(autoplayOptionalDiagnostics(state, 'visible', 'explore')).toEqual(expect.arrayContaining([
      expect.objectContaining({ id: 'secret', kind: 'secret', disposition: 'pursue', evidence: 'visible', rationale: 'profitable optional secret', resourceCost: { bombs: 1, ropes: 0 }, escapeRoute: true })
    ]))
    expect(autoplayDecision(state, 'visible', 'explore', createAutoplayContext())).toMatchObject({ command: 'b', reason: 'open optional secret:secret' })
    const report = runAutoplay(state, { mode: 'visible', policy: 'explore', turnLimit: 1, captureTrace: true })
    expect(report.traceDocument?.records[0]).toMatchObject({ optionalDiagnostics: expect.arrayContaining([expect.objectContaining({ id: 'secret', disposition: 'pursue', evidence: 'visible' })]) })
    expect(report.optionalOutcomes).toMatchObject({ pursued: 1, secrets: 1, shortcuts: 0 })
  })

  it('declines a lethal secret and defers one requiring the reserved critical bomb', () => {
    const lethal = optionalSecret()
    lethal.hero.health = 10
    lethal.floor.actors = [{ ...createEnemy(), x: 3, y: 1, attack: 8 }]
    expect(autoplayOptionalDiagnostics(lethal, 'visible', 'explore')).toEqual(expect.arrayContaining([expect.objectContaining({ id: 'secret', disposition: 'decline', rationale: 'lethal optional threat' })]))
    const reserved = optionalSecret()
    reserved.hero.bombs = 1
    reserved.floor.tiles[indexOf(1, 2)].kind = 'wall'
    expect(autoplayOptionalDiagnostics(reserved, 'visible', 'explore')).toEqual(expect.arrayContaining([expect.objectContaining({ id: 'secret', disposition: 'defer', rationale: 'reserved critical resource required' })]))
  })

  it('labels omniscient shortcut evaluation and never commits to hidden visible-mode metadata', () => {
    const shortcut = optionalSecret()
    const space = shortcut.floor.sideSpaces![0]!
    if (space.kind !== 'mine-breach-room') throw new Error('missing mine breach fixture')
    shortcut.floor.sideSpaces = [{ ...space, rareTransition: { kind: 'floorSkip', targetBiome: 'mine', targetFloor: 3 } }]
    expect(autoplayOptionalDiagnostics(shortcut, 'omniscient', 'explore')).toEqual(expect.arrayContaining([expect.objectContaining({ id: 'secret', kind: 'shortcut', disposition: 'pursue', evidence: 'omniscient-diagnostic', rationale: 'profitable same-biome shortcut' })]))
    expect(autoplayDecision(shortcut, 'omniscient', 'explore', createAutoplayContext())).toMatchObject({ command: 'b', reason: 'open optional shortcut:secret' })
    const hidden = optionalSecret()
    hidden.floor.secretRooms![0]!.discovery = undefined
    for (const point of [{ x: 2, y: 1 }, { x: 3, y: 1 }]) {
      const tile = hidden.floor.tiles[indexOf(point.x, point.y)]!
      tile.explored = false
      tile.visible = false
    }
    expect(autoplayOptionalDiagnostics(hidden, 'visible', 'explore')).toEqual([])
    expect(autoplayDecision(hidden, 'visible', 'explore', createAutoplayContext())?.reason).not.toMatch(/^pursue optional|^open optional/)
  })
})
