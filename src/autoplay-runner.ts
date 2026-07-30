import { AUTOPLAY_MAX_TURNS, autoplayDecision, autoplayRecoveryFingerprint, autoplayStateFingerprint, autoplayTraceFingerprint, createAutoplayContext, recordAutoplayTransitionSnapshot, snapshotAutoplayTransition } from './autoplay'
import { newRun, perform } from './engine'
import { AREA_ORDER, nextArea } from './engine/campaign'
import { createPolicyProfile, createPolicyRunMetadata, scorePolicyEpisode, type PolicyRunMetadata } from './autoplay-policy'
import { autoplayHeuristicProfile, autoplayHeuristicProfileRef, parseAutoplayHeuristicProfile, type AutoplayHeuristicProfile, type AutoplayHeuristicProfileRef } from './autoplay-heuristics'
import { autoplayDirectCompanionIds, createAutoplayPartyOutcomes, recordAutoplayPartyOutcome } from './autoplay-party'
import { appendPolicyFeatureHistory, encodePolicyFeatures, type PolicyFeatureHistoryEntry } from './autoplay-features'
import { createAutoplayTraceDocument, createAutoplayTraceEpisode, createAutoplayTraceRecord, observeAutoplayTrace, type AutoplayTraceDocument, type AutoplayTraceRecord } from './autoplay-trace'
import { observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import { eventLabel } from './engine/shared'
import { campaignDifficultyPackageMetadata } from './campaign-difficulty'
import { getTile } from './world'
import { DIRECTIONS, type AutoplayMode, type AutoplayOptionalOutcomes, type AutoplayPartyOutcomes, type AutoplayPolicy, type AutoplayReplayMetadata, type AutoplayResourceOutcomes, type AutoplayToolOutcomes, type AutoplayStall, type AutoplayTraceEntry, type Biome, type RunTelemetry, type RunState } from './types'

export type AutoplayOutcome = 'complete' | 'dead' | 'stalled' | 'turn-limit' | 'unsupported' | 'error'
export interface AutoplayRunOptions { mode?: Exclude<AutoplayMode, 'off'>; policy?: AutoplayPolicy; heuristicProfile?: AutoplayHeuristicProfile; turnLimit?: number; stalledLimit?: number; chainAreas?: boolean; chainFloors?: boolean; captureTrace?: boolean; traceLimit?: number; includeState?: boolean; includeDebug?: boolean }
export interface AutoplayFinalState { status: RunState['status']; areaFloor: number; hero: { x: number; y: number; health: number; focus: number; gold: number; bombs: number; ropes: number; keys: number }; exit: { x: number; y: number }; objective: RunState['floor']['objective']; guardianDefeated: boolean; exitPath: 'clear' | 'actor-blocked' | 'terrain-blocked'; hostiles: Array<{ id: string; x: number; y: number; health: number; ai?: string }>; modal?: string }
export interface AutoplayReport { seed: number; biome: Biome; areaOrder: Biome[]; finalBiome: Biome; floor: number; mode: Exclude<AutoplayMode, 'off'>; policy: AutoplayPolicy; heuristicProfile: AutoplayHeuristicProfileRef; policyMetadata: PolicyRunMetadata; outcome: AutoplayOutcome; turns: number; commands: string[]; trace: AutoplayTraceEntry[]; traceDocument?: AutoplayTraceDocument; replay: AutoplayReplayMetadata; metrics: RunTelemetry; resourceOutcomes: AutoplayResourceOutcomes; toolOutcomes: AutoplayToolOutcomes; optionalOutcomes: AutoplayOptionalOutcomes; partyOutcomes: AutoplayPartyOutcomes; fingerprint: string; final: AutoplayFinalState; completedAreas: Biome[]; campaignComplete: boolean; unsupported?: { kind: 'direct-companion-control'; companionIds: string[]; message: string }; state?: RunState; debug?: { objectiveId?: string; objectiveTarget?: string; objectiveTargetCount: number; rejectedObjectiveTargets: string[]; bestStrategicDistance?: number; noProgressTurns: number; noTurnCommands: number; loopRecoveries: number; recentPositions: string[] }; stall?: AutoplayStall; error?: string }

export const isCompleteCampaign = (outcome: AutoplayOutcome, completedAreas: readonly Biome[], areaOrder: readonly Biome[] = AREA_ORDER): boolean => outcome === 'complete' && completedAreas.length === areaOrder.length && completedAreas.every((biome, index) => biome === areaOrder[index])

export const autoplayReplayMetadata = (state: RunState): AutoplayReplayMetadata => {
  const floor = state.floor
  const areaFloor = state.areaFloor ?? floor.index % 4
  const escalation = floor.escalation ? `${floor.escalation.arcId}:${floor.escalation.phase}` : undefined
  const recipeId = floor.layoutId.replace(/-remix$/, '')
  const difficultyPackage = state.campaignCycle ? campaignDifficultyPackageMetadata(state.campaignCycle) : undefined
  return {
    seed: state.seed,
    biome: floor.biome,
    areaFloor,
    floorIndex: floor.index,
    layoutId: floor.layoutId,
    macroRecipeId: `${floor.biome}:${recipeId}`,
    routeContractId: `route:${floor.biome}:${floor.index}:${floor.layoutId}:${escalation ?? 'legacy'}`,
    objectiveId: floor.objective.id,
    ...(escalation ? { escalation } : {}),
    ...(state.campaignCycle ? { campaignCycle: structuredClone(state.campaignCycle) } : {}),
    ...(difficultyPackage ? { difficultyPackage } : {}),
    ...(floor.difficulty ? { difficulty: structuredClone(floor.difficulty) } : {}),
    ...(state.companions?.length ? { companions: structuredClone(state.companions) } : {}),
    companionDeathMode: state.companionDeathMode ?? 'injury'
  }
}

const fingerprint = (state: RunState): string => JSON.stringify({
  status: state.status,
  turn: state.turn,
  floor: state.floor.index,
  hero: { x: state.hero.x, y: state.hero.y, health: state.hero.health, focus: state.hero.focus, gold: state.hero.gold, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys, xp: state.hero.xp, level: state.hero.level, skills: [...state.hero.skills].sort(), inventory: [...state.hero.inventory], equipment: state.hero.equipment, relics: [...(state.hero.relics ?? [])], relicCharges: state.hero.relicCharges },
  objective: state.floor.objective,
  guardianDefeated: state.floor.guardianDefeated,
  actors: state.floor.actors.filter(actor => actor.health > 0).map(actor => ({ id: actor.id, x: actor.x, y: actor.y, health: actor.health })).sort((a, b) => a.id.localeCompare(b.id)),
  companionDeathMode: state.companionDeathMode ?? 'injury',
  items: state.floor.items.map(item => ({ id: item.id, x: item.x, y: item.y, count: item.count })).sort((a, b) => `${a.x},${a.y},${a.id}`.localeCompare(`${b.x},${b.y},${b.id}`))
})

const exitPathState = (state: RunState): AutoplayFinalState['exitPath'] => {
  const blocked = new Set(['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest', 'deepWater', 'breakwall', 'cliffWall'])
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
  const areaOrder = state.areaOrder ?? [...AREA_ORDER]
  const mode = options.mode ?? 'omniscient'
  const policy = options.policy ?? 'clear'
  const heuristicProfile = options.heuristicProfile ? parseAutoplayHeuristicProfile(options.heuristicProfile) : autoplayHeuristicProfile()
  const turnLimit = options.turnLimit ?? AUTOPLAY_MAX_TURNS * AREA_ORDER.length * 12
  const policyProfile = createPolicyProfile({ policy, informationMode: mode })
  const stalledLimit = options.stalledLimit ?? 12
  const captureTrace = options.captureTrace ?? true
  const traceLimit = options.traceLimit
  const captureTraceDocument = options.captureTrace === true && traceLimit === undefined
  const commands: string[] = []
  const trace: AutoplayTraceEntry[] = []
  const traceRecords: AutoplayTraceRecord[] = []
  const resourceOutcomes: AutoplayResourceOutcomes = { selected: 0, deferred: 0, rejected: 0, projectedRouteGains: 0, criticalRouteSelections: 0 }
  const toolOutcomes: AutoplayToolOutcomes = { selected: 0, deferred: 0, rejected: 0, uses: 0, retirements: 0 }
  const optionalOutcomes: AutoplayOptionalOutcomes = { pursued: 0, deferred: 0, declined: 0, secrets: 0, shortcuts: 0 }
  const partyOutcomes = createAutoplayPartyOutcomes(state)
  const traceEpisode = createAutoplayTraceEpisode(policyProfile, state.seed, turnLimit, heuristicProfile)
  let featureHistory: PolicyFeatureHistoryEntry[] = []
  let context = createAutoplayContext()
  const completedAreas: Biome[] = []
  let stalled = 0
  let stall: AutoplayStall | undefined
  let outcome: AutoplayOutcome = 'turn-limit'
  let error: string | undefined
  let unsupported: AutoplayReport['unsupported']
  const stallSnapshot = (): AutoplayStall => {
    const stateFingerprint = autoplayStateFingerprint(state)
    const recoveryKey = autoplayRecoveryFingerprint(state)
    return {
      turn: state.turn,
      fingerprint: autoplayTraceFingerprint(state),
      visits: context.visits.get(stateFingerprint) ?? 0,
      ...(context.lastReason ? { lastReason: context.lastReason } : {}),
      failed: [...context.failed.entries()].map(([command, count]) => ({ command, count })).sort((a, b) => b.count - a.count || a.command.localeCompare(b.command)),
      recentPositions: [...context.recentPositions],
      guards: { strategicVisits: Math.max(0, ...context.strategicVisits.values()), noProgressTurns: context.noProgressTurns, noTurnCommands: context.noTurnCommands, loopRecoveries: context.loopRecoveries, recoveryVisits: context.recoveryVisits.get(recoveryKey) ?? 0 }
    }
  }
  try {
    while (state.status === 'playing' && state.turn < turnLimit) {
      const decision = autoplayDecision(state, mode, policy, context, heuristicProfile)
      if (!decision) {
        const companionIds = autoplayDirectCompanionIds(state)
        if (companionIds.length) {
          partyOutcomes.directModeRefused = true
          unsupported = { kind: 'direct-companion-control', companionIds, message: 'Autoplay does not issue direct companion commands.' }
          outcome = 'unsupported'
        } else {
          stall = stallSnapshot()
          outcome = context.lastReason?.startsWith('turn guard:') ? 'turn-limit' : 'stalled'
        }
        break
      }
      const command = decision.command
      for (const assessment of decision.resourceDiagnostics) {
        resourceOutcomes[assessment.disposition === 'select' ? 'selected' : assessment.disposition === 'defer' ? 'deferred' : 'rejected']++
        if (assessment.disposition === 'select' && assessment.projectedRouteGain) resourceOutcomes.projectedRouteGains++
        if (assessment.disposition === 'select' && assessment.knownCriticalRoute) resourceOutcomes.criticalRouteSelections++
      }
      if (decision.reason.startsWith('tool:') || decision.reason.startsWith('wait tool cooldown:')) for (const assessment of decision.toolDiagnostics) toolOutcomes[assessment.disposition === 'select' ? 'selected' : assessment.disposition === 'defer' ? 'deferred' : 'rejected']++
      if (decision.reason.startsWith('pursue optional') || decision.reason.startsWith('open optional')) for (const assessment of decision.optionalDiagnostics) {
        optionalOutcomes[assessment.disposition === 'pursue' ? 'pursued' : assessment.disposition === 'defer' ? 'deferred' : 'declined']++
        if (assessment.disposition === 'pursue') optionalOutcomes[assessment.kind === 'shortcut' ? 'shortcuts' : 'secrets']++
      }
      const before = telemetrySnapshot(state)
      const partyBefore = structuredClone(state)
      const transition = snapshotAutoplayTransition(state)
      const beforeTools = [...(state.hero.traversalTools ?? [])]
      const beforeResources = { health: state.hero.health, focus: state.hero.focus, gold: state.hero.gold, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys }
      const beforeTrace = captureTrace ? { turn: state.turn, replay: autoplayReplayMetadata(state), fingerprint: autoplayTraceFingerprint(state), x: state.hero.x, y: state.hero.y, health: state.hero.health, focus: state.hero.focus, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys, objective: state.floor.objective.status } : undefined
      const traceObservation = captureTraceDocument ? observeAutoplayTrace(state, mode) : undefined
      const traceFeatures = captureTraceDocument ? encodePolicyFeatures(state, mode, featureHistory) : undefined
      const events = perform(state, command)
      recordAutoplayPartyOutcome(partyOutcomes, partyBefore, state, events)
      if (decision.reason.startsWith('confirm tool:')) {
        toolOutcomes.uses++
        if (beforeTools.some(tool => !(state.hero.traversalTools ?? []).includes(tool))) toolOutcomes.retirements++
      }
      observeTelemetryTurn(state, before, events, command)
      recordAutoplayTransitionSnapshot(context, transition, command, state)
      if (captureTrace) trace.push({
        turn: beforeTrace!.turn,
        replay: beforeTrace!.replay,
        fingerprint: beforeTrace!.fingerprint,
        command,
        reason: decision.reason,
        candidates: decision.candidates,
        ...(decision.resourceDiagnostics.length ? { resourceDiagnostics: structuredClone(decision.resourceDiagnostics) } : {}),
        ...(decision.toolDiagnostics.length ? { toolDiagnostics: structuredClone(decision.toolDiagnostics) } : {}),
        ...(decision.optionalDiagnostics.length ? { optionalDiagnostics: structuredClone(decision.optionalDiagnostics) } : {}),
        events: events.map(eventLabel),
        nextFingerprint: autoplayTraceFingerprint(state),
        before: { x: beforeTrace!.x, y: beforeTrace!.y, health: beforeTrace!.health, focus: beforeTrace!.focus, bombs: beforeTrace!.bombs, ropes: beforeTrace!.ropes, objective: beforeTrace!.objective },
        after: { x: state.hero.x, y: state.hero.y, health: state.hero.health, focus: state.hero.focus, bombs: state.hero.bombs, ropes: state.hero.ropes, objective: state.floor.objective.status, ...(state.modal ? { modal: state.modal.kind } : {}) }
      })
      const resourceDelta = { health: state.hero.health - beforeResources.health, focus: state.hero.focus - beforeResources.focus, gold: state.hero.gold - beforeResources.gold, bombs: state.hero.bombs - beforeResources.bombs, ropes: state.hero.ropes - beforeResources.ropes, keys: state.hero.keys - beforeResources.keys }
      if (traceObservation && traceFeatures) traceRecords.push(createAutoplayTraceRecord({ sequence: traceRecords.length, episode: traceEpisode, turn: beforeTrace!.turn, replay: beforeTrace!.replay, observation: traceObservation, features: traceFeatures, legalCandidates: decision.candidates.map(candidate => ({ ...candidate })), ...(decision.resourceDiagnostics.length ? { resourceDiagnostics: structuredClone(decision.resourceDiagnostics) } : {}), ...(decision.toolDiagnostics.length ? { toolDiagnostics: structuredClone(decision.toolDiagnostics) } : {}), ...(decision.optionalDiagnostics.length ? { optionalDiagnostics: structuredClone(decision.optionalDiagnostics) } : {}), chosen: { command, reason: decision.reason }, outcome: { events: events.map(eventLabel), nextFingerprint: autoplayTraceFingerprint(state), status: state.status }, resourceDelta, previousHash: traceRecords.at(-1)?.hash ?? null }))
      featureHistory = appendPolicyFeatureHistory(featureHistory, { turn: before.turn, command, reason: decision.reason, events: events.map(eventLabel), resourceDelta })
      if (traceLimit !== undefined && trace.length > traceLimit) trace.splice(0, trace.length - traceLimit)
      commands.push(command)
      if (events.some(event => event.type === 'floor')) {
        if (options.chainFloors === false) { outcome = 'complete'; break }
        context = createAutoplayContext()
        stalled = 0
      }
      if (events.some(event => event.type === 'areaComplete')) {
        const completed = state.area ?? state.floor.biome
        completedAreas.push(completed)
        const successor = options.chainAreas === false ? undefined : nextArea(completed, areaOrder)
        if (!successor) { outcome = 'complete'; break }
        const next = newRun(state.seed, successor, 0, state.hero, state.rescuedNpcs, [], areaOrder, state.campaignCycle, state.companions, state.companionDeathMode ?? 'injury')
        next.turn = state.turn
        next.lineageEvents = structuredClone(state.lineageEvents ?? [])
        next.telemetry = structuredClone(state.telemetry!)
        next.reputation = { ...(state.reputation ?? { trailfolk: 0, kami: 0 }) }
        state = next
        context = createAutoplayContext()
        stalled = 0
        continue
      }
      stalled = state.turn === before.turn ? stalled + 1 : 0
      if (stalled >= stalledLimit) {
        stall = stallSnapshot()
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
  const debug = { objectiveId: context.objectiveId, objectiveTarget: context.objectiveTarget, objectiveTargetCount: context.objectiveTargetCount, rejectedObjectiveTargets: [...context.rejectedObjectiveTargets].sort(), bestStrategicDistance: context.bestStrategicDistance, noProgressTurns: context.noProgressTurns, noTurnCommands: context.noTurnCommands, loopRecoveries: context.loopRecoveries, recentPositions: [...context.recentPositions] }
  const campaignComplete = isCompleteCampaign(outcome, completedAreas, areaOrder)
  const metrics = structuredClone(state.telemetry!)
  const exploredTiles = state.floor.tiles.filter(tile => tile.explored).length
  const retainedResources = state.hero.bombs + state.hero.ropes + state.hero.keys
  const policyMetadata = createPolicyRunMetadata(policyProfile, state.seed, turnLimit, scorePolicyEpisode({ campaignComplete, outcome, exploredTiles, metrics, retainedResources }))
  const traceDocument = captureTraceDocument ? createAutoplayTraceDocument(traceEpisode, traceRecords, { outcome, reason: error ?? stall?.lastReason ?? (outcome === 'complete' ? 'complete' : outcome), turns: state.turn, campaignComplete, finalFingerprint: autoplayTraceFingerprint(state) }) : undefined
  return { seed: state.seed, biome: startBiome, areaOrder: [...areaOrder], finalBiome, floor: state.floor.index + 1, mode, policy, heuristicProfile: autoplayHeuristicProfileRef(heuristicProfile), policyMetadata, outcome, turns: state.turn, commands, trace, ...(traceDocument ? { traceDocument } : {}), replay: autoplayReplayMetadata(state), metrics, resourceOutcomes, toolOutcomes, optionalOutcomes, partyOutcomes, fingerprint: fingerprint(state), final, completedAreas, campaignComplete, ...(unsupported ? { unsupported } : {}), ...(options.includeState ? { state: structuredClone(state) } : {}), ...(options.includeDebug ? { debug } : {}), ...(stall ? { stall } : {}), ...(error ? { error } : {}) }
}
