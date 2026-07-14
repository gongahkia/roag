import { describe, expect, it } from 'vitest'
import { analysisFor, createRunTelemetry, observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import { createEnemy, createFloor, createHero, createRun } from './test/factories'

describe('run telemetry', () => {
  it('records turn deltas, resource use, damage, kills, and analysis snapshots', () => {
    const enemy = createEnemy({ id: 'rat', health: 6, maxHealth: 6 })
    const state = createRun({ floor: createFloor({ actors: [enemy] }), hero: createHero({ health: 22, gold: 0, xp: 0, bombs: 1, ropes: 1 }) })
    state.telemetry = createRunTelemetry(state)
    const before = telemetrySnapshot(state)
    state.turn = 1
    state.hero.health = 18
    state.hero.gold = 12
    state.hero.xp = 10
    state.hero.bombs = 0
    state.hero.ropes = 0
    state.floor.actors = []
    observeTelemetryTurn(state, before, [{ type: 'hit' }, { type: 'pickup' }, { type: 'boom' }, { type: 'rope' }], 'b')
    expect(state.telemetry).toMatchObject({ turns: 1, kills: 1, damageDealt: 6, damageTaken: 4, goldGained: 12, xpGained: 10, pickups: 1, bombsUsed: 1, ropesUsed: 1, actions: { attacks: 1, bombs: 1, ropes: 1 } })
    expect(analysisFor(state, 'suspended')).toMatchObject({ outcome: 'suspended', floor: 1, metrics: { samples: [{ turn: 0 }, { turn: 1 }] } })
  })
})
