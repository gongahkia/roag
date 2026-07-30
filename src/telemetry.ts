import type { ActionResult } from './engine/shared'
import type { InteractionTelemetry, OptionalContentTelemetry, RunAnalysis, RunFloorMetrics, RunMetricSample, RunOutcome, RunState, RunTelemetry } from './types'

export interface TelemetrySnapshot { turn: number; floor: number; hero: { x: number; y: number }; health: number; focus: number; gold: number; xp: number; bombs: number; ropes: number; hostiles: Map<string, number>; guardians: Map<string, string> }
export type TelemetryCounter = 'itemsUsed' | 'boonPicks' | 'boonAugments' | 'relicPicks' | 'purchases' | 'enemyKills' | 'eventOutcomes' | 'deathCauses' | 'terrainInteractions' | 'bossPhases'

const emptyActions = () => ({ moves: 0, attacks: 0, casts: 0, pickups: 0, bombs: 0, ropes: 0, rests: 0 })
const emptyCounters = () => ({ itemsUsed: {}, boonPicks: {}, boonAugments: {}, relicPicks: {}, purchases: {}, enemyKills: {}, eventOutcomes: {}, deathCauses: {}, terrainInteractions: {}, bossPhases: {} })
const emptyOptionalContent = (): OptionalContentTelemetry => ({ generated: {}, discovered: {}, used: {}, failed: {} })
const emptyInteractions = (): InteractionTelemetry => ({ terrainToolUses: {}, rejectedInteractions: {}, routeFailures: {} })
const floorMetrics = (floor: number): RunFloorMetrics => ({ floor, turns: 0, kills: 0, damageDealt: 0, damageTaken: 0, goldGained: 0, xpGained: 0, pickups: 0, bombsUsed: 0, ropesUsed: 0, secretValue: 0 })

export const telemetrySnapshot = (state: RunState): TelemetrySnapshot => ({
  turn: state.turn,
  floor: state.floor.index + 1,
  hero: { x: state.hero.x, y: state.hero.y },
  health: state.hero.health,
  focus: state.hero.focus,
  gold: state.hero.gold,
  xp: state.hero.xp,
  bombs: state.hero.bombs,
  ropes: state.hero.ropes,
  hostiles: new Map(state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => [actor.id, actor.health])),
  guardians: new Map(state.floor.actors.filter(actor => actor.role === 'guardian' && actor.health > 0).map(actor => [actor.id, actor.guardianPhase ?? 'opening']))
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

const generatedOptionalContent = (state: RunState): OptionalContentTelemetry['generated'] => ({
  ...Object.fromEntries((state.floor.secretRooms ?? []).map(room => [`secret:${room.id}`, 1])),
  ...Object.fromEntries((state.floor.secretRoutes ?? []).filter(route => route.kind === 'rare-transition').map(route => [`shortcut:${route.id}`, 1]))
})

export const createRunTelemetry = (state: RunState): RunTelemetry => {
  const metrics: RunTelemetry = { turns: state.turn, actions: emptyActions(), kills: 0, damageDealt: 0, damageTaken: 0, goldGained: 0, goldSpent: 0, xpGained: 0, pickups: 0, bombsUsed: 0, ropesUsed: 0, secretValue: 0, ...emptyCounters(), optionalContent: { ...emptyOptionalContent(), generated: generatedOptionalContent(state) }, interactions: emptyInteractions(), samples: [], floors: [floorMetrics(state.floor.index + 1)] }
  metrics.samples.push(sampleFor(state, metrics))
  return metrics
}

const normalizeTelemetry = (metrics: RunTelemetry): RunTelemetry => {
  metrics.goldSpent ??= 0
  metrics.secretValue ??= 0
  for (const floor of metrics.floors) floor.secretValue ??= 0
  Object.assign(metrics, Object.fromEntries(Object.entries(emptyCounters()).filter(([key]) => !metrics[key as TelemetryCounter])))
  metrics.optionalContent ??= emptyOptionalContent()
  metrics.interactions ??= emptyInteractions()
  return metrics
}

export const telemetryFor = (state: RunState): RunTelemetry => normalizeTelemetry(state.telemetry ??= createRunTelemetry(state))

export const recordTelemetryCount = (state: RunState, counter: TelemetryCounter, key: string, amount = 1): void => {
  if (!key || !Number.isFinite(amount) || amount <= 0) return
  const metrics = telemetryFor(state)
  metrics[counter][key] = (metrics[counter][key] ?? 0) + amount
}

const record = (counter: Record<string, number>, key: string): void => { if (key) counter[key] = (counter[key] ?? 0) + 1 }
export const recordOptionalContent = (state: RunState, phase: keyof OptionalContentTelemetry, key: string): void => record(telemetryFor(state).optionalContent![phase], key)
export const recordGeneratedOptionalContent = (state: RunState): void => {
  const generated = telemetryFor(state).optionalContent!.generated
  for (const [key, count] of Object.entries(generatedOptionalContent(state))) generated[key] = (generated[key] ?? 0) + count
}
export const recordInteraction = (state: RunState, kind: keyof InteractionTelemetry, key: string): void => record(telemetryFor(state).interactions![kind], key)

export const recordTelemetryKill = (state: RunState, enemyId: string): void => {
  const metrics = telemetryFor(state)
  const floor = activeFloor(metrics, state.floor.index + 1)
  metrics.kills++
  floor.kills++
  metrics.enemyKills[enemyId] = (metrics.enemyKills[enemyId] ?? 0) + 1
}

export const recordSecretValue = (state: RunState, value: number): void => {
  if (!Number.isSafeInteger(value) || value < 1) return
  const metrics = telemetryFor(state)
  metrics.secretValue += value
  activeFloor(metrics, state.floor.index + 1).secretValue += value
}

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
  const afterGuardians = new Map(state.floor.actors.filter(actor => actor.role === 'guardian' && actor.health > 0).map(actor => [actor.id, actor.guardianPhase ?? 'opening']))
  let damageDealt = 0
  if (before.floor === state.floor.index + 1) for (const [id, health] of before.hostiles) {
    const next = afterHostiles.get(id) ?? 0
    damageDealt += Math.max(0, health - next)
  }
  const damageTaken = Math.max(0, before.health - state.hero.health)
  const goldGained = Math.max(0, state.hero.gold - before.gold)
  const goldSpent = Math.max(0, before.gold - state.hero.gold)
  const xpGained = Math.max(0, state.hero.xp - before.xp)
  const bombsUsed = Math.max(0, before.bombs - state.hero.bombs)
  const ropesUsed = Math.max(0, before.ropes - state.hero.ropes)
  const pickups = events.filter(event => event.type === 'pickup').length
  metrics.turns = state.turn
  metrics.damageDealt += damageDealt
  metrics.damageTaken += damageTaken
  metrics.goldGained += goldGained
  metrics.goldSpent += goldSpent
  metrics.xpGained += xpGained
  metrics.pickups += pickups
  metrics.bombsUsed += bombsUsed
  metrics.ropesUsed += ropesUsed
  floor.turns++
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
  if (before.floor === state.floor.index + 1 && (before.hero.x !== state.hero.x || before.hero.y !== state.hero.y)) {
    const terrain = state.floor.tiles[state.hero.y * state.floor.width + state.hero.x]?.kind
    if (terrain) recordTelemetryCount(state, 'terrainInteractions', `${state.floor.biome}:${terrain}`)
  }
  for (const [id, phase] of afterGuardians) if (before.guardians.get(id) !== phase) recordTelemetryCount(state, 'bossPhases', `${state.floor.biome}:${phase}`)
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
