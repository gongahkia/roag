import type { ActionResult } from './engine/shared'
import type { RunAnalysis, RunFloorMetrics, RunMetricSample, RunOutcome, RunState, RunTelemetry } from './types'

export interface TelemetrySnapshot { turn: number; floor: number; health: number; focus: number; gold: number; xp: number; bombs: number; ropes: number; hostiles: Map<string, number> }

const emptyActions = () => ({ moves: 0, attacks: 0, casts: 0, pickups: 0, bombs: 0, ropes: 0, rests: 0 })
const floorMetrics = (floor: number): RunFloorMetrics => ({ floor, turns: 0, kills: 0, damageDealt: 0, damageTaken: 0, goldGained: 0, xpGained: 0, pickups: 0, bombsUsed: 0, ropesUsed: 0 })

export const telemetrySnapshot = (state: RunState): TelemetrySnapshot => ({
  turn: state.turn,
  floor: state.floor.index + 1,
  health: state.hero.health,
  focus: state.hero.focus,
  gold: state.hero.gold,
  xp: state.hero.xp,
  bombs: state.hero.bombs,
  ropes: state.hero.ropes,
  hostiles: new Map(state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => [actor.id, actor.health]))
})

const sampleFor = (state: RunState, metrics: RunTelemetry): RunMetricSample => ({
  turn: state.turn,
  floor: state.floor.index + 1,
  health: state.hero.health,
  focus: state.hero.focus,
  gold: state.hero.gold,
  bombs: state.hero.bombs,
  ropes: state.hero.ropes,
  kills: metrics.kills,
  damageDealt: metrics.damageDealt,
  damageTaken: metrics.damageTaken
})

export const createRunTelemetry = (state: RunState): RunTelemetry => {
  const metrics: RunTelemetry = { turns: state.turn, actions: emptyActions(), kills: 0, damageDealt: 0, damageTaken: 0, goldGained: 0, xpGained: 0, pickups: 0, bombsUsed: 0, ropesUsed: 0, samples: [], floors: [floorMetrics(state.floor.index + 1)] }
  metrics.samples.push(sampleFor(state, metrics))
  return metrics
}

export const telemetryFor = (state: RunState): RunTelemetry => state.telemetry ??= createRunTelemetry(state)

const activeFloor = (metrics: RunTelemetry, floor: number): RunFloorMetrics => {
  let current = metrics.floors.find(entry => entry.floor === floor)
  if (!current) { current = floorMetrics(floor); metrics.floors.push(current) }
  return current
}

const isRest = (command: string): boolean => ['l', 'enter', 'numpad5'].includes(command.toLowerCase())

export const observeTelemetryTurn = (state: RunState, before: TelemetrySnapshot, events: ActionResult, command: string): void => {
  if (state.turn <= before.turn) return
  const metrics = telemetryFor(state)
  const floor = activeFloor(metrics, before.floor)
  const afterHostiles = new Map(state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => [actor.id, actor.health]))
  let damageDealt = 0
  let kills = 0
  if (before.floor === state.floor.index + 1) for (const [id, health] of before.hostiles) {
    const next = afterHostiles.get(id) ?? 0
    damageDealt += Math.max(0, health - next)
    if (next === 0) kills++
  }
  const damageTaken = Math.max(0, before.health - state.hero.health)
  const goldGained = Math.max(0, state.hero.gold - before.gold)
  const xpGained = Math.max(0, state.hero.xp - before.xp)
  const bombsUsed = Math.max(0, before.bombs - state.hero.bombs)
  const ropesUsed = Math.max(0, before.ropes - state.hero.ropes)
  const pickups = events.filter(event => event.type === 'pickup').length
  metrics.turns = state.turn
  metrics.kills += kills
  metrics.damageDealt += damageDealt
  metrics.damageTaken += damageTaken
  metrics.goldGained += goldGained
  metrics.xpGained += xpGained
  metrics.pickups += pickups
  metrics.bombsUsed += bombsUsed
  metrics.ropesUsed += ropesUsed
  floor.turns++
  floor.kills += kills
  floor.damageDealt += damageDealt
  floor.damageTaken += damageTaken
  floor.goldGained += goldGained
  floor.xpGained += xpGained
  floor.pickups += pickups
  floor.bombsUsed += bombsUsed
  floor.ropesUsed += ropesUsed
  if (isRest(command)) metrics.actions.rests++
  else if (events.some(event => event.type === 'hit')) metrics.actions.attacks++
  else if (events.some(event => event.type === 'spell')) metrics.actions.casts++
  else if (events.some(event => event.type === 'move')) metrics.actions.moves++
  if (pickups) metrics.actions.pickups += pickups
  metrics.actions.bombs += bombsUsed
  metrics.actions.ropes += ropesUsed
  metrics.samples.push(sampleFor(state, metrics))
}

export const analysisFor = (state: RunState, outcome: RunOutcome): RunAnalysis => ({
  seed: state.seed,
  biome: state.area ?? state.floor.biome,
  floor: state.floor.index + 1,
  outcome,
  date: new Date().toISOString(),
  metrics: structuredClone(telemetryFor(state))
})
