import { describe, expect, it } from 'vitest'
import { runAutoplay } from '../autoplay-runner'
import { replayAutoplayTrace } from '../autoplay-trace-replay'
import { createAutoplayTraceDocument, createAutoplayTraceRecord } from '../autoplay-trace'
import { createHero, createRun } from '../test/factories'
import { indexOf } from '../types'
import { eventLabel } from './shared'
import { targetPreview } from './targeting'
import { useTool } from './buildcraft'
import { perform } from './input'
import { newRun } from './run'

describe('terrain mutation diagnostics', () => {
  it('previews known affected cells, risk, and blocked reason codes', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['antlerPrybar'] }) })
    state.floor.tiles[indexOf(2, 1)]!.kind = 'boulder'
    const preview = targetPreview(state, { kind: 'target', action: 'antlerPrybar', tool: 'antlerPrybar', direction: 'e' })
    expect(preview.mutation).toMatchObject({ ready: true, reason: 'ready', risk: 'marked', affected: [{ x: 2, y: 1 }, { x: 3, y: 1 }] })
    state.floor.tiles[indexOf(3, 1)]!.visible = false
    expect(targetPreview(state, { kind: 'target', action: 'antlerPrybar', tool: 'antlerPrybar', direction: 'e' }).mutation).toMatchObject({ ready: false, reason: 'destination-unseen', affected: [{ x: 2, y: 1 }] })
    state.hero.cooldowns = { 'tool:antlerPrybar': 2 }
    expect(targetPreview(state, { kind: 'target', action: 'antlerPrybar', tool: 'antlerPrybar', direction: 'e', overdrive: true }).mutation).toMatchObject({ ready: false, reason: 'cooldown', cooldown: 2, overdrive: true })
  })

  it('keeps a cooldown terrain tool selectable for its target preview', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['stoneAdze'], cooldowns: { 'tool:stoneAdze': 2 } }) })
    perform(state, 'y'); perform(state, '1'); perform(state, ';')
    expect(state.modal).toMatchObject({ kind: 'target', tool: 'stoneAdze', direction: 'e' })
    expect(targetPreview(state, state.modal as Extract<NonNullable<typeof state.modal>, { kind: 'target' }>).mutation).toMatchObject({ ready: false, reason: 'cooldown', cooldown: 2 })
  })

  it('does not disclose hidden mutation targets', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['resinFireBasket'] }) })
    state.floor.tiles[indexOf(2, 1)]!.kind = 'bramble'
    state.floor.tiles[indexOf(2, 1)]!.visible = false
    state.floor.tiles[indexOf(2, 1)]!.explored = false
    const preview = targetPreview(state, { kind: 'target', action: 'resinFireBasket', tool: 'resinFireBasket', direction: 'e' })
    expect(preview).toMatchObject({ path: [], cells: [], mutation: { ready: false, reason: 'target-unseen' } })
    expect(JSON.stringify(preview)).not.toContain('bramble')
    expect(useTool(state, 'resinFireBasket', 'e').map(eventLabel)).toEqual(['terrain:resinFireBasket:attempted', 'terrain:resinFireBasket:rejected:target-unseen'])
  })

  it('previews every terrain mutation class and its invalid target rejection', () => {
    const adze = createRun({ hero: createHero({ traversalTools: ['stoneAdze'] }) })
    adze.floor.tiles[indexOf(2, 1)]!.kind = 'crate'
    expect(targetPreview(adze, { kind: 'target', action: 'stoneAdze', tool: 'stoneAdze', direction: 'e' }).mutation).toMatchObject({ ready: true, affected: [{ x: 2, y: 1 }] })
    adze.floor.tiles[indexOf(2, 1)]!.kind = 'wall'
    expect(targetPreview(adze, { kind: 'target', action: 'stoneAdze', tool: 'stoneAdze', direction: 'e' }).mutation).toMatchObject({ ready: false, reason: 'target-invalid' })

    const resin = createRun({ hero: createHero({ traversalTools: ['resinFireBasket'] }) })
    resin.floor.tiles[indexOf(2, 1)]!.kind = 'web'
    expect(targetPreview(resin, { kind: 'target', action: 'resinFireBasket', tool: 'resinFireBasket', direction: 'e' }).mutation).toMatchObject({ ready: true, risk: 'burning', affected: [{ x: 2, y: 1 }] })
    resin.floor.tiles[indexOf(2, 1)]!.kind = 'wall'
    expect(targetPreview(resin, { kind: 'target', action: 'resinFireBasket', tool: 'resinFireBasket', direction: 'e' }).mutation).toMatchObject({ ready: false, reason: 'target-invalid' })

    const roller = createRun({ hero: createHero({ traversalTools: ['woodenLeverRoller'] }) })
    roller.floor.props = [{ id: 'cart', kind: 'mine.brokenCart', biome: 'mine', x: 2, y: 1, state: 'dormant', tags: ['route'], hooks: ['operate'] }]
    expect(targetPreview(roller, { kind: 'target', action: 'woodenLeverRoller', tool: 'woodenLeverRoller', direction: 'e' }).mutation).toMatchObject({ ready: true, affected: [{ x: 2, y: 1 }, { x: 3, y: 1 }] })
    roller.floor.props = []
    expect(targetPreview(roller, { kind: 'target', action: 'woodenLeverRoller', tool: 'woodenLeverRoller', direction: 'e' }).mutation).toMatchObject({ ready: false, reason: 'target-invalid' })
  })

  it('emits attempted, resolved, hazard, and rejection identifiers', () => {
    const burning = createRun({ hero: createHero({ traversalTools: ['resinFireBasket'] }) })
    burning.floor.tiles[indexOf(2, 1)]!.kind = 'web'
    expect(useTool(burning, 'resinFireBasket', 'e').map(eventLabel)).toEqual(expect.arrayContaining(['terrain:resinFireBasket:attempted', 'terrain:resinFireBasket:resolved', 'terrain:resinFireBasket:hazard:burning']))
    const rejected = createRun({ hero: createHero({ traversalTools: ['stoneAdze'] }) })
    rejected.floor.tiles[indexOf(2, 1)]!.kind = 'wall'
    const turn = rejected.turn
    expect(useTool(rejected, 'stoneAdze', 'e').map(eventLabel)).toEqual(['terrain:stoneAdze:attempted', 'terrain:stoneAdze:rejected:target-invalid'])
    expect(rejected.turn).toBe(turn)
    expect(rejected.messages).toContain('terrain:stoneAdze:attempted')
    expect(rejected.messages).toContain('terrain:stoneAdze:rejected:target-invalid')
    expect(burning.messages).toContain('terrain:resinFireBasket:hazard:burning')
  })

  it('preserves terrain identifiers in replay divergence fixtures', () => {
    const trace = runAutoplay(newRun(7), { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true }).traceDocument!
    let previousHash: string | null = null
    const records = trace.records.map((record, index) => {
      const { hash: _hash, version: _version, ...value } = record
      const outcome = index === 0 ? { ...value.outcome, events: [...value.outcome.events, 'terrain:stoneAdze:rejected:target-invalid'] } : value.outcome
      const next = createAutoplayTraceRecord({ ...value, outcome, previousHash })
      previousHash = next.hash
      return next
    })
    const divergence = replayAutoplayTrace(createAutoplayTraceDocument(trace.episode, records, trace.terminal)).divergence
    expect(divergence).toMatchObject({ field: 'state-outcome', expected: { events: expect.arrayContaining(['terrain:stoneAdze:rejected:target-invalid']) } })
  })
})
