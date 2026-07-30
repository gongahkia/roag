import { describe, expect, it } from 'vitest'
import { autoplayDecision, autoplayToolDiagnostics, createAutoplayContext } from './autoplay'
import { runAutoplay } from './autoplay-runner'
import { perform } from './engine'
import { createRun } from './test/factories'
import { indexOf } from './types'

const completedToolRoute = (tool: 'stoneWedge' | 'reedwing' | 'cordAnchor') => {
  const state = createRun()
  state.hero.traversalTools = [tool]
  state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true; tile.visible = true })
  state.floor.tiles[indexOf(1, 1)].kind = 'floor'
  state.floor.tiles[indexOf(2, 1)].kind = tool === 'reedwing' ? 'pit' : 'wall'
  state.floor.tiles[indexOf(3, 1)].kind = 'exit'
  state.floor.exit = { x: 3, y: 1 }
  state.floor.objective = { id: 'complete', kind: 'defeatGuardian', label: 'Clear the route', status: 'complete' }
  state.floor.guardianDefeated = true
  return state
}

describe('autoplay traversal-tool planning', () => {
  it('prefers and executes a legal reusable tool route over a bomb with trace rationale', () => {
    const state = completedToolRoute('stoneWedge')
    state.hero.bombs = 1
    const context = createAutoplayContext()
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'y', reason: 'tool:stoneWedge:reusable tool opens known route' })
    perform(state, 'y')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: '1', reason: 'select tool:stoneWedge' })
    perform(state, '1')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: ';', reason: 'target tool:stoneWedge' })
    perform(state, ';')
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toMatchObject({ command: 'Enter', reason: 'confirm tool:stoneWedge' })
    const report = runAutoplay(completedToolRoute('stoneWedge'), { mode: 'omniscient', policy: 'clear', turnLimit: 1, captureTrace: true })
    expect(report.traceDocument?.records).toEqual(expect.arrayContaining([expect.objectContaining({ toolDiagnostics: expect.arrayContaining([expect.objectContaining({ tool: 'stoneWedge', disposition: 'select', overdrive: false, rationale: 'reusable tool opens known route' })]) })]))
    expect(report.toolOutcomes).toMatchObject({ uses: 1, retirements: 0 })
  })

  it('waits for a critical tool cooldown and never selects an illegal target', () => {
    const cooling = completedToolRoute('stoneWedge')
    cooling.hero.cooldowns = { 'tool:stoneWedge': 2 }
    expect(autoplayToolDiagnostics(cooling, 'omniscient')).toEqual(expect.arrayContaining([expect.objectContaining({ tool: 'stoneWedge', overdrive: false, disposition: 'defer', rationale: 'wait for critical tool cooldown' })]))
    expect(autoplayDecision(cooling, 'omniscient', 'clear', createAutoplayContext())).toMatchObject({ command: 'l', reason: 'wait tool cooldown:stoneWedge' })
    const invalid = completedToolRoute('cordAnchor')
    expect(autoplayToolDiagnostics(invalid, 'omniscient')).toEqual(expect.arrayContaining([expect.objectContaining({ tool: 'cordAnchor', disposition: 'reject', rationale: 'no legal observed tool target' })]))
  })

  it('uses overdrive only to cross an otherwise blocked critical route', () => {
    const state = createRun()
    state.hero.traversalTools = ['reedwing']
    state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true; tile.visible = true })
    state.floor.tiles[indexOf(1, 1)].kind = 'floor'
    state.floor.tiles[indexOf(2, 1)].kind = 'pit'
    state.floor.tiles[indexOf(3, 1)].kind = 'pit'
    state.floor.tiles[indexOf(4, 1)].kind = 'exit'
    state.floor.exit = { x: 4, y: 1 }
    state.floor.objective = { id: 'complete', kind: 'defeatGuardian', label: 'Clear the route', status: 'complete' }
    state.floor.guardianDefeated = true
    expect(autoplayToolDiagnostics(state, 'omniscient')).toEqual(expect.arrayContaining([expect.objectContaining({ tool: 'reedwing', overdrive: true, disposition: 'select', rationale: 'retire tool for blocked critical route' })]))
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())).toMatchObject({ command: 'y', reason: 'tool:reedwing:overdrive:retire tool for blocked critical route' })
    expect(runAutoplay(state, { mode: 'omniscient', policy: 'clear', turnLimit: 1 }).toolOutcomes).toMatchObject({ uses: 1, retirements: 1 })
  })
})
