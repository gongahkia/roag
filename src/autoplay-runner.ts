import { autoplayDecision, autoplayStateFingerprint, autoplayTraceFingerprint, createAutoplayContext, recordAutoplayTransition } from './autoplay'
import { newRun, perform } from './engine'
import { nextArea } from './engine/campaign'
import { observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import { getTile } from './world'
import { DIRECTIONS, type AutoplayMode, type AutoplayPolicy, type AutoplayStall, type AutoplayTraceEntry, type Biome, type RunTelemetry, type RunState } from './types'

export type AutoplayOutcome = 'complete' | 'dead' | 'stalled' | 'turn-limit' | 'error'
export interface AutoplayRunOptions { mode?: Exclude<AutoplayMode, 'off'>; policy?: AutoplayPolicy; turnLimit?: number; stalledLimit?: number; chainAreas?: boolean }
export interface AutoplayFinalState { status: RunState['status']; areaFloor: number; hero: { x: number; y: number; health: number; focus: number; gold: number; bombs: number; ropes: number; keys: number }; exit: { x: number; y: number }; objective: RunState['floor']['objective']; guardianDefeated: boolean; exitPath: 'clear' | 'actor-blocked' | 'terrain-blocked'; hostiles: Array<{ id: string; x: number; y: number; health: number; ai?: string }>; modal?: string }
export interface AutoplayReport { seed: number; biome: Biome; finalBiome: Biome; floor: number; mode: Exclude<AutoplayMode, 'off'>; policy: AutoplayPolicy; outcome: AutoplayOutcome; turns: number; commands: string[]; trace: AutoplayTraceEntry[]; metrics: RunTelemetry; fingerprint: string; final: AutoplayFinalState; completedAreas: Biome[]; campaignComplete: boolean; stall?: AutoplayStall; error?: string }

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

const exitPathState = (state: RunState): AutoplayFinalState['exitPath'] => {
  const blocked = new Set(['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest'])
  const key = (x: number, y: number) => `${x},${y}`
  const reachesExit = (blockActors: boolean): boolean => {
    const start = { x: state.hero.x, y: state.hero.y }
    const queue = [start]
    const seen = new Set([key(start.x, start.y)])
    while (queue.length) {
      const point = queue.shift()!
      if (point.x === state.floor.exit.x && point.y === state.floor.exit.y) return true
      for (const delta of Object.values(DIRECTIONS)) {
        const x = point.x + delta.x
        const y = point.y + delta.y
        const pointKey = key(x, y)
        const tile = getTile(state.floor, x, y)
        if (seen.has(pointKey) || !tile || blocked.has(tile.kind) || (tile.kind === 'lockedDoor' && state.hero.keys < 1)) continue
        if (blockActors && state.floor.actors.some(actor => actor.health > 0 && actor.x === x && actor.y === y)) continue
        seen.add(pointKey)
        queue.push({ x, y })
      }
    }
    return false
  }
  if (!reachesExit(false)) return 'terrain-blocked'
  return reachesExit(true) ? 'clear' : 'actor-blocked'
}

export const runAutoplay = (input: RunState, options: AutoplayRunOptions = {}): AutoplayReport => {
  let state = structuredClone(input)
  const startBiome = state.area ?? state.floor.biome
  const mode = options.mode ?? 'omniscient'
  const policy = options.policy ?? 'clear'
  const turnLimit = options.turnLimit ?? 3200
  const stalledLimit = options.stalledLimit ?? 12
  const commands: string[] = []
  const trace: AutoplayTraceEntry[] = []
  let context = createAutoplayContext()
  const completedAreas: Biome[] = []
  let stalled = 0
  let stall: AutoplayStall | undefined
  let outcome: AutoplayOutcome = 'turn-limit'
  let error: string | undefined
  try {
    while (state.status === 'playing' && state.turn < turnLimit) {
      const decision = autoplayDecision(state, mode, policy, context)
      if (!decision) {
        const stateFingerprint = autoplayStateFingerprint(state)
        stall = {
          turn: state.turn,
          fingerprint: autoplayTraceFingerprint(state),
          visits: context.visits.get(stateFingerprint) ?? 0,
          ...(context.lastReason ? { lastReason: context.lastReason } : {}),
          failed: [...context.failed.entries()].map(([command, count]) => ({ command, count })).sort((a, b) => b.count - a.count || a.command.localeCompare(b.command)),
          recentPositions: [...context.recentPositions]
        }
        outcome = 'stalled'
        break
      }
      const command = decision.command
      const before = telemetrySnapshot(state)
      const beforeState = structuredClone(state)
      const beforeFingerprint = autoplayTraceFingerprint(state)
      const events = perform(state, command)
      observeTelemetryTurn(state, before, events, command)
      recordAutoplayTransition(context, beforeState, command, state)
      trace.push({
        turn: before.turn,
        fingerprint: beforeFingerprint,
        command,
        reason: decision.reason,
        candidates: decision.candidates,
        events: events.map(event => event.type),
        nextFingerprint: autoplayTraceFingerprint(state),
        before: { x: beforeState.hero.x, y: beforeState.hero.y, health: beforeState.hero.health, focus: beforeState.hero.focus, bombs: beforeState.hero.bombs, ropes: beforeState.hero.ropes, objective: beforeState.floor.objective.status },
        after: { x: state.hero.x, y: state.hero.y, health: state.hero.health, focus: state.hero.focus, bombs: state.hero.bombs, ropes: state.hero.ropes, objective: state.floor.objective.status, ...(state.modal ? { modal: state.modal.kind } : {}) }
      })
      commands.push(command)
      if (events.some(event => event.type === 'areaComplete')) {
        const completed = state.area ?? state.floor.biome
        completedAreas.push(completed)
        const successor = options.chainAreas === false ? undefined : nextArea(completed)
        if (!successor) { outcome = 'complete'; break }
        const next = newRun(state.seed, successor, 0, state.hero, state.rescuedNpcs, [])
        next.turn = state.turn
        next.lineageEvents = structuredClone(state.lineageEvents ?? [])
        next.telemetry = structuredClone(state.telemetry!)
        state = next
        context = createAutoplayContext()
        stalled = 0
        continue
      }
      stalled = state.turn === before.turn ? stalled + 1 : 0
      if (stalled >= stalledLimit) {
        stall = {
          turn: state.turn,
          fingerprint: autoplayTraceFingerprint(state),
          visits: context.visits.get(autoplayStateFingerprint(state)) ?? 0,
          ...(context.lastReason ? { lastReason: context.lastReason } : {}),
          failed: [...context.failed.entries()].map(([command, count]) => ({ command, count })).sort((a, b) => b.count - a.count || a.command.localeCompare(b.command)),
          recentPositions: [...context.recentPositions]
        }
        outcome = 'stalled'
        break
      }
    }
    if (state.status === 'dead') outcome = 'dead'
  } catch (caught) {
    outcome = 'error'
    error = caught instanceof Error ? caught.message : String(caught)
  }
  const final: AutoplayFinalState = {
    status: state.status,
    areaFloor: state.areaFloor ?? state.floor.index % 4,
    hero: { x: state.hero.x, y: state.hero.y, health: state.hero.health, focus: state.hero.focus, gold: state.hero.gold, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys },
    exit: { ...state.floor.exit },
    objective: structuredClone(state.floor.objective),
    guardianDefeated: state.floor.guardianDefeated,
    exitPath: exitPathState(state),
    hostiles: state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => ({ id: actor.id, x: actor.x, y: actor.y, health: actor.health, ...(actor.ai ? { ai: actor.ai } : {}) })),
    ...(state.modal ? { modal: state.modal.kind } : {})
  }
  const finalBiome = state.area ?? state.floor.biome
  return { seed: state.seed, biome: startBiome, finalBiome, floor: state.floor.index + 1, mode, policy, outcome, turns: state.turn, commands, trace, metrics: structuredClone(state.telemetry!), fingerprint: fingerprint(state), final, completedAreas, campaignComplete: outcome === 'complete' && completedAreas.at(-1) === 'ruins', ...(stall ? { stall } : {}), ...(error ? { error } : {}) }
}
