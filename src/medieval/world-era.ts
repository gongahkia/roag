import { type EraPace } from './generation-config'
import { SeededRng, hashSeed } from './rng'

/**
 * Durable era progression is a small, renderer-independent contract. It has
 * no simulation authority: later systems may consume its profile, but only
 * time-bearing world actions and explicit physical Jomon growth evidence can
 * change this state.
 */
export const WORLD_ERA_CONTRACT_VERSION = 1 as const
export const WORLD_ERA_MODEL_VERSION = 1 as const
export const WORLD_ERA_REMIX_CYCLE_UNITS = 7_200 as const

export const WORLD_ERA_PACE_MULTIPLIERS = {
  measured: 1,
  brisk: 2,
  pressing: 3
} as const satisfies Readonly<Record<EraPace, number>>

export const WORLD_ERA_THRESHOLDS = {
  ngPlus: 7_200,
  ngPlusPlus: 21_600
} as const

export const WORLD_ERA_LIMITS = {
  growthEvidence: 32,
  transitions: 2,
  identityLength: 96,
  profileTags: 3
} as const

export const DURABLE_JOMON_GROWTH_KINDS = [
  'physical-expansion',
  'workspace-refit',
  'capacity-upgrade',
  'tool-installation',
  'small-craft'
] as const

export type WorldEra = 'base' | 'ng-plus' | 'ng-plus-plus'
export type DurableJomonGrowthKind = typeof DURABLE_JOMON_GROWTH_KINDS[number]
export type DurableJomonGrowthSourceKind = 'jomon-vessel' | 'jomon-prop'
export type WorldEraTransitionTrigger = 'active-play-time' | 'durable-jomon-growth'
export type WorldEraEscalation = 'base' | 'elevated' | 'plateau'
export type WorldEraPressureTag = 'route-pressure' | 'scarcity-pressure' | 'social-memory-pressure' | 'frontier-pressure' | 'relic-pressure'
export type WorldEraOpportunityTag = 'route-opportunity' | 'scarcity-opportunity' | 'social-memory-opportunity' | 'frontier-opportunity' | 'relic-opportunity'

/** Weights are model-owned; callers cannot submit arbitrary progression amounts. */
export const DURABLE_JOMON_GROWTH_WEIGHTS = {
  'physical-expansion': 2_400,
  'workspace-refit': 1_200,
  'capacity-upgrade': 1_800,
  'tool-installation': 900,
  'small-craft': 3_600
} as const satisfies Readonly<Record<DurableJomonGrowthKind, number>>

export interface DurableJomonGrowthSource {
  kind: DurableJomonGrowthSourceKind
  id: string
}

/**
 * A future physical-work system records one completed, durable change here.
 * There is intentionally no free-text note or caller-controlled weight.
 */
export interface DurableJomonGrowthEvidence {
  id: string
  kind: DurableJomonGrowthKind
  source: DurableJomonGrowthSource
  atWorldTime: number
}

export interface WorldEraTransitionEvidence {
  kind: 'combined-progress-threshold'
  trigger: WorldEraTransitionTrigger
  thresholdUnits: number
  activePlayMinutes: number
  pacedActivePlayUnits: number
  durableGrowthUnits: number
  growthEvidenceId?: string
}

export interface WorldEraTransition {
  id: string
  token: number
  from: Exclude<WorldEra, 'ng-plus-plus'>
  to: Exclude<WorldEra, 'base'>
  atWorldTime: number
  modelVersion: typeof WORLD_ERA_MODEL_VERSION
  evidence: WorldEraTransitionEvidence
}

export interface WorldEraProfile {
  version: typeof WORLD_ERA_CONTRACT_VERSION
  era: WorldEra
  paceMultiplier: number
  rawEscalation: WorldEraEscalation
  remixCycle: number
  remixIdentity: string
  pressureTags: readonly WorldEraPressureTag[]
  opportunityTags: readonly WorldEraOpportunityTag[]
}

export interface WorldEraState {
  version: typeof WORLD_ERA_CONTRACT_VERSION
  modelVersion: typeof WORLD_ERA_MODEL_VERSION
  era: WorldEra
  activePlayMinutes: number
  pacedActivePlayUnits: number
  durableGrowthEvidence: readonly DurableJomonGrowthEvidence[]
  durableGrowthUnits: number
  progressUnits: number
  remixCycle: number
  transitions: readonly WorldEraTransition[]
  profile: WorldEraProfile
}

/** Immutable and current-world values supplied by the owning world contract. */
export interface WorldEraContext {
  worldId: string
  creationDigest: string
  eraPace: EraPace
  worldTime: number
  jomonVesselId: string
  jomonPropIds: readonly string[]
}

export interface WorldEraProjection {
  version: typeof WORLD_ERA_CONTRACT_VERSION
  modelVersion: typeof WORLD_ERA_MODEL_VERSION
  era: WorldEra
  activePlayMinutes: number
  pacedActivePlayUnits: number
  durableGrowthEvidence: readonly DurableJomonGrowthEvidence[]
  durableGrowthUnits: number
  progressUnits: number
  remixCycle: number
  transitions: readonly WorldEraTransition[]
  profile: WorldEraProfile
}

export type WorldEraDiagnosticCode =
  | 'world-era.malformed-state'
  | 'world-era.invalid-contract-version'
  | 'world-era.invalid-model-version'
  | 'world-era.invalid-context'
  | 'world-era.invalid-era'
  | 'world-era.invalid-active-play-minutes'
  | 'world-era.invalid-paced-time'
  | 'world-era.invalid-growth-units'
  | 'world-era.invalid-progress-units'
  | 'world-era.invalid-remix-cycle'
  | 'world-era.growth-limit'
  | 'world-era.malformed-growth-evidence'
  | 'world-era.invalid-growth-id'
  | 'world-era.invalid-growth-kind'
  | 'world-era.invalid-growth-source'
  | 'world-era.invalid-growth-time'
  | 'world-era.duplicate-growth-id'
  | 'world-era.noncanonical-growth-order'
  | 'world-era.transition-limit'
  | 'world-era.malformed-transition'
  | 'world-era.invalid-transition-history'
  | 'world-era.invalid-transition-token'
  | 'world-era.invalid-profile'
  | 'world-era.invalid-action-transition'

export interface WorldEraDiagnostic {
  recordId: string
  code: WorldEraDiagnosticCode
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const validIdentity = (value: unknown): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= WORLD_ERA_LIMITS.identityLength
  && /^[a-z][a-z0-9:._-]*$/.test(value)
const canonicalIssues = (issues: readonly WorldEraDiagnostic[]): readonly WorldEraDiagnostic[] => [...new Map(issues.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const issue = (recordId: string, code: WorldEraDiagnosticCode): WorldEraDiagnostic => ({ recordId, code })
const validEraPace = (value: unknown): value is EraPace => value === 'measured' || value === 'brisk' || value === 'pressing'
const validWorldEra = (value: unknown): value is WorldEra => value === 'base' || value === 'ng-plus' || value === 'ng-plus-plus'
const validGrowthKind = (value: unknown): value is DurableJomonGrowthKind => DURABLE_JOMON_GROWTH_KINDS.includes(value as DurableJomonGrowthKind)
const sortedGrowthEvidence = (evidence: readonly DurableJomonGrowthEvidence[]): readonly DurableJomonGrowthEvidence[] => [...evidence].sort((left, right) => compare(left.id, right.id))
const chronologicalGrowthEvidence = (evidence: readonly DurableJomonGrowthEvidence[]): readonly DurableJomonGrowthEvidence[] => [...evidence].sort((left, right) => left.atWorldTime - right.atWorldTime || compare(left.id, right.id))
const validContext = (context: WorldEraContext): boolean => validIdentity(context.worldId)
  && validIdentity(context.creationDigest)
  && validEraPace(context.eraPace)
  && safeInteger(context.worldTime)
  && validIdentity(context.jomonVesselId)
  && Array.isArray(context.jomonPropIds)
  && context.jomonPropIds.every(validIdentity)
  && new Set(context.jomonPropIds).size === context.jomonPropIds.length

export const worldEraPaceMultiplier = (eraPace: EraPace): number => WORLD_ERA_PACE_MULTIPLIERS[eraPace]
export const durableJomonGrowthWeight = (kind: DurableJomonGrowthKind): number => DURABLE_JOMON_GROWTH_WEIGHTS[kind]

const validGrowthSource = (context: WorldEraContext, value: unknown): value is DurableJomonGrowthSource => record(value)
  && hasOnlyKeys(value, ['kind', 'id'])
  && validIdentity(value.id)
  && ((value.kind === 'jomon-vessel' && value.id === context.jomonVesselId)
    || (value.kind === 'jomon-prop' && context.jomonPropIds.includes(value.id)))

const validGrowthEvidence = (context: WorldEraContext, value: unknown): value is DurableJomonGrowthEvidence => record(value)
  && hasOnlyKeys(value, ['id', 'kind', 'source', 'atWorldTime'])
  && validIdentity(value.id)
  && validGrowthKind(value.kind)
  && validGrowthSource(context, value.source)
  && safeInteger(value.atWorldTime)
  && value.atWorldTime <= context.worldTime

const progressFor = (activePlayMinutes: number, evidence: readonly DurableJomonGrowthEvidence[], eraPace: EraPace): { pacedActivePlayUnits: number; durableGrowthUnits: number; progressUnits: number } => {
  const pacedActivePlayUnits = activePlayMinutes * worldEraPaceMultiplier(eraPace)
  const durableGrowthUnits = evidence.reduce((total, item) => total + durableJomonGrowthWeight(item.kind), 0)
  return { pacedActivePlayUnits, durableGrowthUnits, progressUnits: pacedActivePlayUnits + durableGrowthUnits }
}

const eraForProgress = (progressUnits: number): WorldEra => progressUnits >= WORLD_ERA_THRESHOLDS.ngPlusPlus
  ? 'ng-plus-plus'
  : progressUnits >= WORLD_ERA_THRESHOLDS.ngPlus
    ? 'ng-plus'
    : 'base'

const remixCycleFor = (era: WorldEra, progressUnits: number): number => era === 'ng-plus-plus'
  ? Math.floor((progressUnits - WORLD_ERA_THRESHOLDS.ngPlusPlus) / WORLD_ERA_REMIX_CYCLE_UNITS)
  : 0

const transitionIdFor = (context: WorldEraContext, from: Exclude<WorldEra, 'ng-plus-plus'>, to: Exclude<WorldEra, 'base'>, thresholdUnits: number, atWorldTime: number): string =>
  `era-transition:${hashSeed(`world-era:${WORLD_ERA_MODEL_VERSION}:${context.worldId}:${context.creationDigest}:${from}:${to}:${thresholdUnits}:${atWorldTime}`).toString(36)}`

const transitionTokenFor = (context: WorldEraContext, id: string, evidence: WorldEraTransitionEvidence): number =>
  hashSeed(`world-era-token:${WORLD_ERA_MODEL_VERSION}:${context.worldId}:${context.creationDigest}:${id}:${evidence.trigger}:${evidence.thresholdUnits}:${evidence.activePlayMinutes}:${evidence.pacedActivePlayUnits}:${evidence.durableGrowthUnits}:${evidence.growthEvidenceId ?? ''}`)

const thresholds = [WORLD_ERA_THRESHOLDS.ngPlus, WORLD_ERA_THRESHOLDS.ngPlusPlus] as const
const transitionSides = (thresholdUnits: number): { from: Exclude<WorldEra, 'ng-plus-plus'>; to: Exclude<WorldEra, 'base'> } => thresholdUnits === WORLD_ERA_THRESHOLDS.ngPlus
  ? { from: 'base', to: 'ng-plus' }
  : { from: 'ng-plus', to: 'ng-plus-plus' }

const transitionFor = (
  context: WorldEraContext,
  thresholdUnits: number,
  atWorldTime: number,
  trigger: WorldEraTransitionTrigger,
  durableGrowthUnits: number,
  growthEvidenceId?: string
): WorldEraTransition => {
  const { from, to } = transitionSides(thresholdUnits)
  const evidence: WorldEraTransitionEvidence = {
    kind: 'combined-progress-threshold',
    trigger,
    thresholdUnits,
    activePlayMinutes: atWorldTime,
    pacedActivePlayUnits: atWorldTime * worldEraPaceMultiplier(context.eraPace),
    durableGrowthUnits,
    ...(growthEvidenceId === undefined ? {} : { growthEvidenceId })
  }
  const id = transitionIdFor(context, from, to, thresholdUnits, atWorldTime)
  return { id, token: transitionTokenFor(context, id, evidence), from, to, atWorldTime, modelVersion: WORLD_ERA_MODEL_VERSION, evidence }
}

/**
 * Reconstructs threshold crossings from time and canonical growth evidence.
 * Time at a boundary is applied before evidence stamped at that same minute.
 */
const transitionsFor = (context: WorldEraContext, activePlayMinutes: number, evidence: readonly DurableJomonGrowthEvidence[]): readonly WorldEraTransition[] => {
  const multiplier = worldEraPaceMultiplier(context.eraPace)
  const transitions: WorldEraTransition[] = []
  let nextThresholdIndex = 0
  let durableGrowthUnits = 0
  let processedThrough = 0

  const addTimeTransitionsThrough = (worldTime: number): void => {
    const scoreAtEnd = worldTime * multiplier + durableGrowthUnits
    while (nextThresholdIndex < thresholds.length && thresholds[nextThresholdIndex]! <= scoreAtEnd) {
      const thresholdUnits = thresholds[nextThresholdIndex]!
      const firstCrossing = Math.ceil((thresholdUnits - durableGrowthUnits) / multiplier)
      const atWorldTime = Math.max(processedThrough, firstCrossing)
      transitions.push(transitionFor(context, thresholdUnits, atWorldTime, 'active-play-time', durableGrowthUnits))
      nextThresholdIndex++
    }
    processedThrough = worldTime
  }

  for (const item of chronologicalGrowthEvidence(evidence)) {
    addTimeTransitionsThrough(item.atWorldTime)
    durableGrowthUnits += durableJomonGrowthWeight(item.kind)
    const scoreAfterGrowth = item.atWorldTime * multiplier + durableGrowthUnits
    while (nextThresholdIndex < thresholds.length && thresholds[nextThresholdIndex]! <= scoreAfterGrowth) {
      transitions.push(transitionFor(context, thresholds[nextThresholdIndex]!, item.atWorldTime, 'durable-jomon-growth', durableGrowthUnits, item.id))
      nextThresholdIndex++
    }
  }
  addTimeTransitionsThrough(activePlayMinutes)
  return transitions
}

const chooseTags = <Tag extends string>(stream: string, candidates: readonly Tag[], count: number): readonly Tag[] => {
  const rng = new SeededRng(stream)
  const available = [...candidates]
  const selected: Tag[] = []
  while (selected.length < count && available.length) selected.push(available.splice(rng.integer(available.length), 1)![0]!)
  return selected.sort(compare)
}

const profileFor = (context: WorldEraContext, era: WorldEra, progressUnits: number): WorldEraProfile => {
  const remixCycle = remixCycleFor(era, progressUnits)
  if (era === 'base') return {
    version: WORLD_ERA_CONTRACT_VERSION,
    era,
    paceMultiplier: worldEraPaceMultiplier(context.eraPace),
    rawEscalation: 'base',
    remixCycle,
    remixIdentity: 'era-remix:base',
    pressureTags: ['route-pressure'],
    opportunityTags: ['route-opportunity']
  }
  if (era === 'ng-plus') return {
    version: WORLD_ERA_CONTRACT_VERSION,
    era,
    paceMultiplier: worldEraPaceMultiplier(context.eraPace),
    rawEscalation: 'elevated',
    remixCycle,
    remixIdentity: 'era-remix:ng-plus',
    pressureTags: ['frontier-pressure', 'route-pressure'],
    opportunityTags: ['frontier-opportunity', 'route-opportunity']
  }
  const stream = `world-era-remix:${WORLD_ERA_MODEL_VERSION}:${context.worldId}:${context.creationDigest}:${context.eraPace}:${remixCycle}`
  return {
    version: WORLD_ERA_CONTRACT_VERSION,
    era,
    paceMultiplier: worldEraPaceMultiplier(context.eraPace),
    rawEscalation: 'plateau',
    remixCycle,
    remixIdentity: `era-remix:${hashSeed(stream).toString(36)}`,
    pressureTags: chooseTags(`${stream}:pressure`, ['route-pressure', 'scarcity-pressure', 'social-memory-pressure', 'frontier-pressure', 'relic-pressure'], WORLD_ERA_LIMITS.profileTags),
    opportunityTags: chooseTags(`${stream}:opportunity`, ['route-opportunity', 'scarcity-opportunity', 'social-memory-opportunity', 'frontier-opportunity', 'relic-opportunity'], WORLD_ERA_LIMITS.profileTags)
  }
}

const stateFor = (context: WorldEraContext, activePlayMinutes: number, evidence: readonly DurableJomonGrowthEvidence[]): WorldEraState => {
  const canonicalEvidence = sortedGrowthEvidence(evidence).map(item => structuredClone(item))
  const progress = progressFor(activePlayMinutes, canonicalEvidence, context.eraPace)
  const era = eraForProgress(progress.progressUnits)
  const remixCycle = remixCycleFor(era, progress.progressUnits)
  return {
    version: WORLD_ERA_CONTRACT_VERSION,
    modelVersion: WORLD_ERA_MODEL_VERSION,
    era,
    activePlayMinutes,
    pacedActivePlayUnits: progress.pacedActivePlayUnits,
    durableGrowthEvidence: canonicalEvidence,
    durableGrowthUnits: progress.durableGrowthUnits,
    progressUnits: progress.progressUnits,
    remixCycle,
    transitions: transitionsFor(context, activePlayMinutes, canonicalEvidence),
    profile: profileFor(context, era, progress.progressUnits)
  }
}

export const createWorldEraState = (context: WorldEraContext): WorldEraState => {
  if (!validContext(context) || context.worldTime !== 0) throw new WorldEraContractError([issue('world-era:context', 'world-era.invalid-context')])
  return stateFor(context, 0, [])
}

const validTransitionShape = (value: unknown): value is WorldEraTransition => record(value)
  && hasOnlyKeys(value, ['id', 'token', 'from', 'to', 'atWorldTime', 'modelVersion', 'evidence'])
  && validIdentity(value.id)
  && safeInteger(value.token)
  && (value.from === 'base' || value.from === 'ng-plus')
  && (value.to === 'ng-plus' || value.to === 'ng-plus-plus')
  && safeInteger(value.atWorldTime)
  && value.modelVersion === WORLD_ERA_MODEL_VERSION
  && record(value.evidence)

/** Validates the durable subdomain independently before world-state validation combines it. */
export const validateWorldEraState = (context: WorldEraContext, value: unknown): readonly WorldEraDiagnostic[] => {
  if (!validContext(context)) return [issue('world-era:context', 'world-era.invalid-context')]
  if (!record(value) || !hasOnlyKeys(value, ['version', 'modelVersion', 'era', 'activePlayMinutes', 'pacedActivePlayUnits', 'durableGrowthEvidence', 'durableGrowthUnits', 'progressUnits', 'remixCycle', 'transitions', 'profile'])) return [issue('world-era', 'world-era.malformed-state')]
  const issues: WorldEraDiagnostic[] = []
  if (value.version !== WORLD_ERA_CONTRACT_VERSION) issues.push(issue('world-era', 'world-era.invalid-contract-version'))
  if (value.modelVersion !== WORLD_ERA_MODEL_VERSION) issues.push(issue('world-era', 'world-era.invalid-model-version'))
  if (!validWorldEra(value.era)) issues.push(issue('world-era', 'world-era.invalid-era'))
  if (!safeInteger(value.activePlayMinutes) || value.activePlayMinutes !== context.worldTime) issues.push(issue('world-era', 'world-era.invalid-active-play-minutes'))
  if (!Array.isArray(value.durableGrowthEvidence)) {
    issues.push(issue('world-era:growth', 'world-era.malformed-growth-evidence'))
  } else {
    if (value.durableGrowthEvidence.length > WORLD_ERA_LIMITS.growthEvidence) issues.push(issue('world-era:growth', 'world-era.growth-limit'))
    const ids = new Set<string>()
    for (const candidate of value.durableGrowthEvidence) {
      const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'world-era:growth'
      if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'kind', 'source', 'atWorldTime'])) issues.push(issue(id, 'world-era.malformed-growth-evidence'))
      if (!record(candidate) || !validIdentity(candidate.id)) issues.push(issue(id, 'world-era.invalid-growth-id'))
      else if (ids.has(candidate.id)) issues.push(issue(candidate.id, 'world-era.duplicate-growth-id'))
      else ids.add(candidate.id)
      if (!record(candidate) || !validGrowthKind(candidate.kind)) issues.push(issue(id, 'world-era.invalid-growth-kind'))
      if (!record(candidate) || !validGrowthSource(context, candidate.source)) issues.push(issue(id, 'world-era.invalid-growth-source'))
      if (!record(candidate) || !safeInteger(candidate.atWorldTime) || candidate.atWorldTime > context.worldTime) issues.push(issue(id, 'world-era.invalid-growth-time'))
    }
    const evidence = value.durableGrowthEvidence as DurableJomonGrowthEvidence[]
    if (!same(evidence, sortedGrowthEvidence(evidence))) issues.push(issue('world-era:growth', 'world-era.noncanonical-growth-order'))
  }
  if (!Array.isArray(value.transitions)) {
    issues.push(issue('world-era:transitions', 'world-era.malformed-transition'))
  } else {
    if (value.transitions.length > WORLD_ERA_LIMITS.transitions) issues.push(issue('world-era:transitions', 'world-era.transition-limit'))
    for (const candidate of value.transitions) {
      const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'world-era:transition'
      if (!validTransitionShape(candidate)) issues.push(issue(id, 'world-era.malformed-transition'))
    }
  }

  if (!issues.length && safeInteger(value.activePlayMinutes) && Array.isArray(value.durableGrowthEvidence)) {
    const expected = stateFor(context, value.activePlayMinutes, value.durableGrowthEvidence as DurableJomonGrowthEvidence[])
    if (value.pacedActivePlayUnits !== expected.pacedActivePlayUnits) issues.push(issue('world-era', 'world-era.invalid-paced-time'))
    if (value.durableGrowthUnits !== expected.durableGrowthUnits) issues.push(issue('world-era', 'world-era.invalid-growth-units'))
    if (value.progressUnits !== expected.progressUnits) issues.push(issue('world-era', 'world-era.invalid-progress-units'))
    if (value.era !== expected.era) issues.push(issue('world-era', 'world-era.invalid-era'))
    if (value.remixCycle !== expected.remixCycle) issues.push(issue('world-era', 'world-era.invalid-remix-cycle'))
    if (!same(value.profile, expected.profile)) issues.push(issue('world-era:profile', 'world-era.invalid-profile'))
    if (!same(value.transitions, expected.transitions)) {
      const supplied = Array.isArray(value.transitions) ? value.transitions as WorldEraTransition[] : []
      const tokenMismatch = supplied.some((transition, index) => transition.token !== expected.transitions[index]?.token)
      issues.push(issue('world-era:transitions', tokenMismatch ? 'world-era.invalid-transition-token' : 'world-era.invalid-transition-history'))
    }
  }
  return canonicalIssues(issues)
}

export const isWorldEraState = (context: WorldEraContext, value: unknown): value is WorldEraState => validateWorldEraState(context, value).length === 0

/** A compact, canonical projection for partition-invariance and future consumers. */
export const worldEraProjection = (state: WorldEraState): WorldEraProjection => ({
  version: state.version,
  modelVersion: state.modelVersion,
  era: state.era,
  activePlayMinutes: state.activePlayMinutes,
  pacedActivePlayUnits: state.pacedActivePlayUnits,
  durableGrowthEvidence: structuredClone(state.durableGrowthEvidence),
  durableGrowthUnits: state.durableGrowthUnits,
  progressUnits: state.progressUnits,
  remixCycle: state.remixCycle,
  transitions: structuredClone(state.transitions),
  profile: structuredClone(state.profile)
})

/**
 * Purely accounts for an already accepted temporal action. The caller retains
 * ownership of clock/catch-up ordering and cannot provide a separate weight.
 */
export const advanceWorldEraForTemporalAction = (
  state: WorldEraState,
  context: WorldEraContext,
  action: { startedAtWorldTime: number; atWorldTime: number; durationMinutes: number }
): WorldEraState => {
  const beforeContext = { ...context, worldTime: action.startedAtWorldTime }
  const diagnostics = validateWorldEraState(beforeContext, state)
  if (diagnostics.length) throw new WorldEraContractError(diagnostics)
  if (!safeInteger(action.durationMinutes) || action.durationMinutes < 1 || !safeInteger(action.startedAtWorldTime) || !safeInteger(action.atWorldTime) || action.startedAtWorldTime !== state.activePlayMinutes || action.atWorldTime !== action.startedAtWorldTime + action.durationMinutes || action.atWorldTime !== context.worldTime) {
    throw new WorldEraContractError([issue('world-era:action', 'world-era.invalid-action-transition')])
  }
  return stateFor(context, action.atWorldTime, state.durableGrowthEvidence)
}

/** Records a completed durable Jomon change without advancing world time. */
export const recordDurableJomonGrowthEvidence = (state: WorldEraState, context: WorldEraContext, evidence: DurableJomonGrowthEvidence): WorldEraState => {
  const diagnostics = validateWorldEraState(context, state)
  if (diagnostics.length) throw new WorldEraContractError(diagnostics)
  if (!validGrowthEvidence(context, evidence) || evidence.atWorldTime !== context.worldTime || state.durableGrowthEvidence.some(item => item.id === evidence.id)) {
    throw new WorldEraContractError([issue(validIdentity(evidence?.id) ? evidence.id : 'world-era:growth', state.durableGrowthEvidence.some(item => item.id === evidence?.id) ? 'world-era.duplicate-growth-id' : 'world-era.malformed-growth-evidence')])
  }
  if (state.durableGrowthEvidence.length >= WORLD_ERA_LIMITS.growthEvidence) throw new WorldEraContractError([issue('world-era:growth', 'world-era.growth-limit')])
  return stateFor(context, state.activePlayMinutes, [...state.durableGrowthEvidence, evidence])
}

export class WorldEraContractError extends Error {
  constructor(readonly diagnostics: readonly WorldEraDiagnostic[]) {
    super(`world era contract rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'WorldEraContractError'
  }
}
