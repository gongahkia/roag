import { autoplayCommand } from './autoplay'
import { perform } from './engine'
import { observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import type { AutoplayMode, Biome, RunTelemetry, RunState } from './types'

export type AutoplayOutcome = 'complete' | 'dead' | 'stalled' | 'turn-limit' | 'error'
export interface AutoplayRunOptions { mode?: Exclude<AutoplayMode, 'off'>; turnLimit?: number; stalledLimit?: number }
export interface AutoplayReport { seed: number; biome: Biome; floor: number; mode: Exclude<AutoplayMode, 'off'>; outcome: AutoplayOutcome; turns: number; commands: string[]; metrics: RunTelemetry; fingerprint: string; error?: string }

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
  const turnLimit = options.turnLimit ?? 600
  const stalledLimit = options.stalledLimit ?? 12
  const commands: string[] = []
  let stalled = 0
  let outcome: AutoplayOutcome = 'turn-limit'
  let error: string | undefined
  try {
    while (state.status === 'playing' && state.turn < turnLimit) {
      const command = autoplayCommand(state, mode)
      if (!command) { outcome = 'stalled'; break }
      const before = telemetrySnapshot(state)
      const events = perform(state, command)
      observeTelemetryTurn(state, before, events, command)
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
  return { seed: state.seed, biome: state.area ?? state.floor.biome, floor: state.floor.index + 1, mode, outcome, turns: state.turn, commands, metrics: structuredClone(state.telemetry!), fingerprint: fingerprint(state), ...(error ? { error } : {}) }
}
