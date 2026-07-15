import { autoplayDecision, autoplayStateFingerprint, createAutoplayContext, recordAutoplayTransition } from './autoplay'
import { perform } from './engine'
import { observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import type { AutoplayMode, AutoplayPolicy, AutoplayTraceEntry, Biome, RunTelemetry, RunState } from './types'

export type AutoplayOutcome = 'complete' | 'dead' | 'stalled' | 'turn-limit' | 'error'
export interface AutoplayRunOptions { mode?: Exclude<AutoplayMode, 'off'>; policy?: AutoplayPolicy; turnLimit?: number; stalledLimit?: number }
export interface AutoplayReport { seed: number; biome: Biome; floor: number; mode: Exclude<AutoplayMode, 'off'>; policy: AutoplayPolicy; outcome: AutoplayOutcome; turns: number; commands: string[]; trace: AutoplayTraceEntry[]; metrics: RunTelemetry; fingerprint: string; error?: string }

const fingerprint = (state: RunState): string => JSON.stringify({
  status: state.status,
  turn: state.turn,
  floor: state.floor.index,
  hero: { x: state.hero.x, y: state.hero.y, health: state.hero.health, focus: state.hero.focus, gold: state.hero.gold, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys, xp: state.hero.xp, level: state.hero.level, skills: [...state.hero.skills].sort(), inventory: [...state.hero.inventory], equipment: state.hero.equipment },
  objective: state.floor.objective,
  guardianDefeated: state.floor.guardianDefeated,
  actors: state.floor.actors.filter(actor => actor.health > 0).map(actor => ({ id: actor.id, x: actor.x, y: actor.y, health: actor.health })).sort((a, b) => a.id.localeCompare(b.id)),
  items: state.floor.items.map(item => ({ id: item.id, x: item.x, y: item.y, count: item.count })).sort((a, b) => `${a.x},${a.y},${a.id}`.localeCompare(`${b.x},${b.y},${b.id}`))
})

export const runAutoplay = (input: RunState, options: AutoplayRunOptions = {}): AutoplayReport => {
  const state = structuredClone(input)
  const mode = options.mode ?? 'omniscient'
  const policy = options.policy ?? 'clear'
  const turnLimit = options.turnLimit ?? 600
  const stalledLimit = options.stalledLimit ?? 12
  const commands: string[] = []
  const trace: AutoplayTraceEntry[] = []
  const context = createAutoplayContext()
  let stalled = 0
  let outcome: AutoplayOutcome = 'turn-limit'
  let error: string | undefined
  try {
    while (state.status === 'playing' && state.turn < turnLimit) {
      const decision = autoplayDecision(state, mode, policy, context)
      if (!decision) { outcome = 'stalled'; break }
      const command = decision.command
      const before = telemetrySnapshot(state)
      const beforeState = structuredClone(state)
      const beforeFingerprint = autoplayStateFingerprint(state)
      const events = perform(state, command)
      observeTelemetryTurn(state, before, events, command)
      recordAutoplayTransition(context, beforeState, command, state)
      trace.push({ turn: before.turn, fingerprint: beforeFingerprint, command, reason: decision.reason, candidates: decision.candidates, events: events.map(event => event.type), nextFingerprint: autoplayStateFingerprint(state) })
      commands.push(command)
      if (events.some(event => event.type === 'areaComplete')) { outcome = 'complete'; break }
      stalled = state.turn === before.turn ? stalled + 1 : 0
      if (stalled >= stalledLimit) { outcome = 'stalled'; break }
    }
    if (state.status === 'dead') outcome = 'dead'
  } catch (caught) {
    outcome = 'error'
    error = caught instanceof Error ? caught.message : String(caught)
  }
  return { seed: state.seed, biome: state.area ?? state.floor.biome, floor: state.floor.index + 1, mode, policy, outcome, turns: state.turn, commands, trace, metrics: structuredClone(state.telemetry!), fingerprint: fingerprint(state), ...(error ? { error } : {}) }
}
