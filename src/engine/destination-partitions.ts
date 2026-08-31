import { rngFor } from '../rng'
import type { DestinationCondition, DestinationConsequence, DestinationDevelopmentKind, DestinationHistoryEntry, DestinationIntervention, DestinationKnownReport, DestinationPartition, DestinationPartitionId, DestinationReportSource, DestinationScheduledDevelopment, DestinationWorldState } from '../types'

export const DESTINATION_PARTITION_IDS = ['destination:kestrel', 'destination:orison', 'destination:halcyon', 'destination:nerida', 'destination:borealis'] as const satisfies readonly DestinationPartitionId[]
export const DESTINATION_PARTITION_HISTORY_LIMIT = 12
export const DESTINATION_PARTITION_RESOLVED_LIMIT = 24
export const DESTINATION_PARTITION_SCHEDULE_LIMIT = 8
export const DESTINATION_PARTITION_CONSEQUENCE_LIMIT = 8
export const DESTINATION_PARTITION_INTERVENTION_LIMIT = 8
export const NERIDA_BYPASS_COST_MARKS = 60
export const NERIDA_BYPASS_VERIFICATION_MARKS = 180

const conditionLabels: Record<DestinationCondition, string> = {
  'calibration-queue': 'calibration queue',
  'approach-inspection': 'approach inspection',
  'relay-balanced': 'relay balanced',
  'relay-overheated': 'relay overheated',
  'relay-throttled': 'relay throttled',
  'tender-cycle': 'tender cycle',
  'dock-congested': 'dock congestion',
  'pump-watch': 'pump watch',
  'cavitation-restriction': 'cavitation restriction',
  'bypass-stabilizing': 'bypass stabilizing',
  'pump-stabilized': 'pump stabilized',
  'kiln-nominal': 'kiln nominal',
  'kiln-backlog': 'kiln backlog',
  'kiln-cooldown': 'kiln cooldown'
}

const conditionDetails: Record<DestinationCondition, string> = {
  'calibration-queue': 'Instrumentation clerks are clearing a bounded calibration queue.',
  'approach-inspection': 'Approach instruments are under an inspection hold.',
  'relay-balanced': 'Relay crews report balanced thermal load.',
  'relay-overheated': 'Relay heat exchangers are carrying more load than planned.',
  'relay-throttled': 'Relay staff have imposed a controlled transfer window.',
  'tender-cycle': 'Salvage tenders are cycling through ordinary drydock work.',
  'dock-congested': 'Tender berths are congested and local dock work is delayed.',
  'pump-watch': 'Pump Bank Four is on watch for wear in the pressure chain.',
  'cavitation-restriction': 'Pump cavitation has narrowed the safe transfer approaches.',
  'bypass-stabilizing': 'A temporary bypass is carrying load while the pump chain is verified.',
  'pump-stabilized': 'Pump crews have stabilized the bypass, but the repair remains provisional.',
  'kiln-nominal': 'Ceramic kilns are operating inside their planned thermal envelope.',
  'kiln-backlog': 'Ceramic orders are accumulating behind maintenance work.',
  'kiln-cooldown': 'Glassworks crews are using a controlled cooldown to protect the kilns.'
}

const developmentKinds = new Set<DestinationDevelopmentKind>([
  'kestrel-inspection-audit', 'orison-relay-thermal-load', 'orison-throttle-window', 'halcyon-tender-backlog', 'nerida-pump-cavitation', 'nerida-bypass-verification', 'borealis-kiln-debt', 'borealis-controlled-cooldown'
])
const conditions = new Set<DestinationCondition>(Object.keys(conditionLabels) as DestinationCondition[])

const clamp = (value: number, min = 0, max = 100): number => Math.max(min, Math.min(max, Math.round(value)))
const isRecord = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const isNonNegativeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isInteger(value) && value >= 0
const isCondition = (value: unknown): value is DestinationCondition => typeof value === 'string' && conditions.has(value as DestinationCondition)
const sortDevelopments = (developments: readonly DestinationScheduledDevelopment[]): DestinationScheduledDevelopment[] => [...developments].sort((left, right) => left.dueAtRouteReckoning - right.dueAtRouteReckoning || left.id.localeCompare(right.id))

const appendBounded = <T>(items: readonly T[], item: T, limit: number): T[] => [...items, item].slice(-limit)
const addConsequence = (partition: DestinationPartition, consequence: DestinationConsequence): void => {
  const index = partition.consequences.findIndex(candidate => candidate.id === consequence.id)
  if (index >= 0) partition.consequences[index] = consequence
  else partition.consequences = appendBounded(partition.consequences, consequence, DESTINATION_PARTITION_CONSEQUENCE_LIMIT)
}
const deactivateConsequence = (partition: DestinationPartition, id: string): void => {
  const current = partition.consequences.find(candidate => candidate.id === id)
  if (current) current.active = false
}
const addHistory = (partition: DestinationPartition, history: DestinationHistoryEntry): void => {
  if (partition.history.some(candidate => candidate.id === history.id)) return
  partition.history = appendBounded(partition.history, history, DESTINATION_PARTITION_HISTORY_LIMIT)
}
const schedule = (partition: DestinationPartition, development: DestinationScheduledDevelopment): void => {
  if (partition.resolvedDevelopmentIds.includes(development.id) || partition.scheduledDevelopments.some(candidate => candidate.id === development.id)) return
  partition.scheduledDevelopments = sortDevelopments([...partition.scheduledDevelopments, development]).slice(0, DESTINATION_PARTITION_SCHEDULE_LIMIT)
}
const resolveId = (partition: DestinationPartition, id: string): void => {
  if (!partition.resolvedDevelopmentIds.includes(id)) partition.resolvedDevelopmentIds = appendBounded(partition.resolvedDevelopmentIds, id, DESTINATION_PARTITION_RESOLVED_LIMIT)
  partition.scheduledDevelopments = partition.scheduledDevelopments.filter(candidate => candidate.id !== id)
}

const initialPartitions = (startAtRouteReckoning: number): Record<DestinationPartitionId, DestinationPartition> => ({
  'destination:kestrel': {
    version: 1, id: 'destination:kestrel', contentRevision: 1, lastProcessedRouteReckoning: startAtRouteReckoning, condition: 'calibration-queue', pressure: { id: 'calibration-backlog', label: 'calibration backlog', value: 18, limit: 100 },
    scheduledDevelopments: [{ id: 'development:kestrel:inspection-audit', kind: 'kestrel-inspection-audit', dueAtRouteReckoning: startAtRouteReckoning + 720 }], resolvedDevelopmentIds: [], consequences: [], interventions: [], history: []
  },
  'destination:orison': {
    version: 1, id: 'destination:orison', contentRevision: 1, lastProcessedRouteReckoning: startAtRouteReckoning, condition: 'relay-balanced', pressure: { id: 'relay-thermal-load', label: 'relay thermal load', value: 32, limit: 100 },
    scheduledDevelopments: [{ id: 'development:orison:thermal-load', kind: 'orison-relay-thermal-load', dueAtRouteReckoning: startAtRouteReckoning + 600 }], resolvedDevelopmentIds: [], consequences: [], interventions: [], history: []
  },
  'destination:halcyon': {
    version: 1, id: 'destination:halcyon', contentRevision: 1, lastProcessedRouteReckoning: startAtRouteReckoning, condition: 'tender-cycle', pressure: { id: 'dock-congestion', label: 'dock congestion', value: 28, limit: 100 },
    scheduledDevelopments: [{ id: 'development:halcyon:tender-backlog', kind: 'halcyon-tender-backlog', dueAtRouteReckoning: startAtRouteReckoning + 360 }], resolvedDevelopmentIds: [], consequences: [], interventions: [], history: []
  },
  'destination:nerida': {
    version: 1, id: 'destination:nerida', contentRevision: 1, lastProcessedRouteReckoning: startAtRouteReckoning, condition: 'pump-watch', pressure: { id: 'pump-wear', label: 'pump wear', value: 46, limit: 100 },
    scheduledDevelopments: [{ id: 'development:nerida:pump-cavitation', kind: 'nerida-pump-cavitation', dueAtRouteReckoning: startAtRouteReckoning + 480 }], resolvedDevelopmentIds: [], consequences: [], interventions: [], history: []
  },
  'destination:borealis': {
    version: 1, id: 'destination:borealis', contentRevision: 1, lastProcessedRouteReckoning: startAtRouteReckoning, condition: 'kiln-nominal', pressure: { id: 'kiln-thermal-debt', label: 'kiln thermal debt', value: 34, limit: 100 },
    scheduledDevelopments: [{ id: 'development:borealis:kiln-debt', kind: 'borealis-kiln-debt', dueAtRouteReckoning: startAtRouteReckoning + 540 }], resolvedDevelopmentIds: [], consequences: [], interventions: [], history: []
  }
})

const initialReport = (partition: DestinationPartition): DestinationKnownReport => ({ version: 1, destinationId: partition.id, reportedCondition: partition.condition, source: 'initial-chart', observedAtRouteReckoning: partition.lastProcessedRouteReckoning, receivedAtRouteReckoning: partition.lastProcessedRouteReckoning, confidence: 'estimated' })

export const createDestinationWorld = (_seed: number, startAtRouteReckoning = 0): DestinationWorldState => {
  const partitions = initialPartitions(Math.max(0, Math.floor(startAtRouteReckoning)))
  return {
    version: 1,
    partitions,
    reports: Object.fromEntries(DESTINATION_PARTITION_IDS.map(id => [id, initialReport(partitions[id])])) as DestinationWorldState['reports']
  }
}

const clonePartition = (partition: DestinationPartition): DestinationPartition => ({
  ...partition,
  pressure: { ...partition.pressure },
  scheduledDevelopments: partition.scheduledDevelopments.map(development => ({ ...development })),
  resolvedDevelopmentIds: [...partition.resolvedDevelopmentIds],
  consequences: partition.consequences.map(consequence => ({ ...consequence })),
  interventions: partition.interventions.map(intervention => ({ ...intervention })),
  history: partition.history.map(entry => ({ ...entry }))
})

export const cloneDestinationWorld = (world: DestinationWorldState): DestinationWorldState => ({
  version: 1,
  partitions: Object.fromEntries(DESTINATION_PARTITION_IDS.map(id => [id, clonePartition(world.partitions[id])])) as DestinationWorldState['partitions'],
  reports: Object.fromEntries(DESTINATION_PARTITION_IDS.map(id => [id, { ...world.reports[id] }])) as DestinationWorldState['reports']
})

const optionalConsequenceId = (partition: DestinationPartition): string | undefined => partition.consequences.find(consequence => consequence.active)?.id
const reportFor = (partition: DestinationPartition, source: DestinationReportSource, atRouteReckoning: number): DestinationKnownReport => ({
  version: 1,
  destinationId: partition.id,
  reportedCondition: partition.condition,
  source,
  observedAtRouteReckoning: atRouteReckoning,
  receivedAtRouteReckoning: atRouteReckoning,
  confidence: source === 'initial-chart' ? 'estimated' : 'confirmed',
  ...(optionalConsequenceId(partition) ? { knownConsequenceId: optionalConsequenceId(partition) } : {})
})

export interface DestinationReportRefresh {
  world: DestinationWorldState
  report: DestinationKnownReport
  conditionChanged: boolean
}

export const refreshDestinationReport = (source: DestinationWorldState, destinationId: DestinationPartitionId, sourceType: Exclude<DestinationReportSource, 'initial-chart'>, atRouteReckoning: number): DestinationReportRefresh => {
  const world = cloneDestinationWorld(source)
  const previous = world.reports[destinationId]
  const report = reportFor(world.partitions[destinationId], sourceType, atRouteReckoning)
  world.reports[destinationId] = report
  return { world, report, conditionChanged: previous.reportedCondition !== report.reportedCondition || previous.knownConsequenceId !== report.knownConsequenceId }
}

export const destinationReportFreshness = (report: DestinationKnownReport, routeReckoning: number): 'current' | 'aging' | 'stale' => {
  const age = Math.max(0, routeReckoning - report.observedAtRouteReckoning)
  return age > 360 ? 'stale' : age > 120 ? 'aging' : 'current'
}
export const destinationConditionLabel = (condition: DestinationCondition): string => conditionLabels[condition]
export const destinationConditionDetail = (condition: DestinationCondition): string => conditionDetails[condition]
export const destinationInterventionAvailable = (partition: DestinationPartition): boolean => partition.id === 'destination:nerida' && partition.condition === 'cavitation-restriction' && !partition.interventions.some(intervention => intervention.kind === 'nerida-bypass-installation')

export interface DestinationDevelopmentResolution { id: string; destinationId: DestinationPartitionId; kind: DestinationDevelopmentKind; condition: DestinationCondition; atRouteReckoning: number }

const resolveDevelopment = (seed: number, partition: DestinationPartition, development: DestinationScheduledDevelopment): DestinationDevelopmentResolution => {
  const variation = rngFor(seed, 'galaxy', 'destination-partition', partition.id, development.id).int(0, 5)
  const atRouteReckoning = development.dueAtRouteReckoning
  if (development.kind === 'kestrel-inspection-audit') {
    partition.condition = 'approach-inspection'
    partition.pressure.value = clamp(partition.pressure.value + 9 + variation)
    addConsequence(partition, { id: 'consequence:kestrel:approach-inspection', kind: 'local-operation', createdAtRouteReckoning: atRouteReckoning, active: true })
  } else if (development.kind === 'orison-relay-thermal-load') {
    partition.condition = 'relay-overheated'
    partition.pressure.value = clamp(partition.pressure.value + 14 + variation)
    addConsequence(partition, { id: 'consequence:orison:relay-thermal-load', kind: 'route-modifier', createdAtRouteReckoning: atRouteReckoning, active: true })
    schedule(partition, { id: 'development:orison:throttle-window', kind: 'orison-throttle-window', dueAtRouteReckoning: atRouteReckoning + 360 })
  } else if (development.kind === 'orison-throttle-window') {
    partition.condition = 'relay-throttled'
    partition.pressure.value = clamp(partition.pressure.value + 6 + variation)
    deactivateConsequence(partition, 'consequence:orison:relay-thermal-load')
    addConsequence(partition, { id: 'consequence:orison:relay-throttle-window', kind: 'route-modifier', createdAtRouteReckoning: atRouteReckoning, active: true })
  } else if (development.kind === 'halcyon-tender-backlog') {
    partition.condition = 'dock-congested'
    partition.pressure.value = clamp(partition.pressure.value + 12 + variation)
    addConsequence(partition, { id: 'consequence:halcyon:tender-backlog', kind: 'local-operation', createdAtRouteReckoning: atRouteReckoning, active: true })
  } else if (development.kind === 'nerida-pump-cavitation') {
    partition.condition = 'cavitation-restriction'
    partition.pressure.value = clamp(partition.pressure.value + 16 + variation)
    addConsequence(partition, { id: 'consequence:nerida:cavitation-restriction', kind: 'route-modifier', createdAtRouteReckoning: atRouteReckoning, active: true })
    addConsequence(partition, { id: 'consequence:nerida:bypass-operation', kind: 'local-operation', createdAtRouteReckoning: atRouteReckoning, active: true })
  } else if (development.kind === 'nerida-bypass-verification') {
    partition.condition = 'pump-stabilized'
    partition.pressure.value = clamp(partition.pressure.value - 24 - variation)
    deactivateConsequence(partition, 'consequence:nerida:cavitation-restriction')
    deactivateConsequence(partition, 'consequence:nerida:bypass-operation')
  } else if (development.kind === 'borealis-kiln-debt') {
    partition.condition = 'kiln-backlog'
    partition.pressure.value = clamp(partition.pressure.value + 13 + variation)
    addConsequence(partition, { id: 'consequence:borealis:kiln-backlog', kind: 'local-operation', createdAtRouteReckoning: atRouteReckoning, active: true })
    schedule(partition, { id: 'development:borealis:controlled-cooldown', kind: 'borealis-controlled-cooldown', dueAtRouteReckoning: atRouteReckoning + 360 })
  } else {
    partition.condition = 'kiln-cooldown'
    partition.pressure.value = clamp(partition.pressure.value - 10 + variation)
    deactivateConsequence(partition, 'consequence:borealis:kiln-backlog')
    addConsequence(partition, { id: 'consequence:borealis:controlled-cooldown', kind: 'local-operation', createdAtRouteReckoning: atRouteReckoning, active: true })
  }
  resolveId(partition, development.id)
  addHistory(partition, { id: `history:${development.id}`, atRouteReckoning, kind: development.kind === 'nerida-bypass-verification' ? 'resolution' : 'development', condition: partition.condition })
  return { id: development.id, destinationId: partition.id, kind: development.kind, condition: partition.condition, atRouteReckoning }
}

export interface DestinationAdvanceResult { world: DestinationWorldState; resolved: DestinationDevelopmentResolution[] }

/** Advances saved destination truth only from one explicit canonical Route Reckoning target. */
export const advanceDestinationWorld = (source: DestinationWorldState, seed: number, targetRouteReckoning: number, partitionOrder: readonly DestinationPartitionId[] = DESTINATION_PARTITION_IDS): DestinationAdvanceResult => {
  const world = cloneDestinationWorld(source)
  const resolved: DestinationDevelopmentResolution[] = []
  const target = Math.max(0, Math.floor(targetRouteReckoning))
  for (const destinationId of [...partitionOrder].sort()) {
    const partition = world.partitions[destinationId]
    if (!partition || target <= partition.lastProcessedRouteReckoning) continue
    while (true) {
      const next = sortDevelopments(partition.scheduledDevelopments).find(development => development.dueAtRouteReckoning > partition.lastProcessedRouteReckoning && development.dueAtRouteReckoning <= target && !partition.resolvedDevelopmentIds.includes(development.id))
      if (!next) break
      resolved.push(resolveDevelopment(seed, partition, next))
    }
    partition.lastProcessedRouteReckoning = target
  }
  return { world, resolved }
}

export interface NeridaBypassMutation { world: DestinationWorldState; changed: boolean; message: string; intervention?: DestinationIntervention }

export const installNeridaBypass = (source: DestinationWorldState, atRouteReckoning: number): NeridaBypassMutation => {
  const world = cloneDestinationWorld(source)
  const partition = world.partitions['destination:nerida']
  if (!destinationInterventionAvailable(partition)) return { world, changed: false, message: 'No Nerida bypass installation is currently available.' }
  const intervention: DestinationIntervention = { id: 'intervention:nerida:bypass-installation', kind: 'nerida-bypass-installation', startedAtRouteReckoning: atRouteReckoning, completedAtRouteReckoning: atRouteReckoning, costMarks: NERIDA_BYPASS_COST_MARKS }
  partition.interventions = appendBounded(partition.interventions, intervention, DESTINATION_PARTITION_INTERVENTION_LIMIT)
  partition.condition = 'bypass-stabilizing'
  partition.pressure.value = clamp(partition.pressure.value - 12)
  deactivateConsequence(partition, 'consequence:nerida:bypass-operation')
  addConsequence(partition, { id: 'consequence:nerida:bypass-stabilizing', kind: 'route-modifier', createdAtRouteReckoning: atRouteReckoning, active: true })
  schedule(partition, { id: 'development:nerida:bypass-verification', kind: 'nerida-bypass-verification', dueAtRouteReckoning: atRouteReckoning + NERIDA_BYPASS_VERIFICATION_MARKS })
  addHistory(partition, { id: `history:${intervention.id}`, atRouteReckoning, kind: 'intervention', condition: partition.condition })
  return { world, changed: true, message: `Bypass installation recorded. It consumed ${NERIDA_BYPASS_COST_MARKS} Route Reckoning marks; pump verification is scheduled in ${NERIDA_BYPASS_VERIFICATION_MARKS} marks.`, intervention }
}

const validScheduledDevelopment = (value: unknown): value is DestinationScheduledDevelopment => isRecord(value) && typeof value.id === 'string' && developmentKinds.has(value.kind as DestinationDevelopmentKind) && isNonNegativeInteger(value.dueAtRouteReckoning)
const validConsequence = (value: unknown): value is DestinationConsequence => isRecord(value) && typeof value.id === 'string' && (value.kind === 'route-modifier' || value.kind === 'local-operation') && isNonNegativeInteger(value.createdAtRouteReckoning) && typeof value.active === 'boolean'
const validIntervention = (value: unknown): value is DestinationIntervention => isRecord(value) && typeof value.id === 'string' && value.kind === 'nerida-bypass-installation' && isNonNegativeInteger(value.startedAtRouteReckoning) && isNonNegativeInteger(value.completedAtRouteReckoning) && isNonNegativeInteger(value.costMarks)
const validHistory = (value: unknown): value is DestinationHistoryEntry => isRecord(value) && typeof value.id === 'string' && isNonNegativeInteger(value.atRouteReckoning) && (value.kind === 'development' || value.kind === 'intervention' || value.kind === 'resolution') && isCondition(value.condition)

const normalizePartition = (value: unknown, fallback: DestinationPartition, currentRouteReckoning: number): DestinationPartition => {
  if (!isRecord(value) || value.version !== 1 || value.id !== fallback.id || !isCondition(value.condition) || !isRecord(value.pressure) || typeof value.pressure.id !== 'string' || typeof value.pressure.label !== 'string' || !isNonNegativeInteger(value.pressure.value) || !isNonNegativeInteger(value.pressure.limit) || value.pressure.limit === 0) return fallback
  const lastProcessedRouteReckoning = isNonNegativeInteger(value.lastProcessedRouteReckoning) && value.lastProcessedRouteReckoning <= currentRouteReckoning ? value.lastProcessedRouteReckoning : currentRouteReckoning
  const resolvedDevelopmentIds = Array.isArray(value.resolvedDevelopmentIds) ? [...new Set(value.resolvedDevelopmentIds.filter((id): id is string => typeof id === 'string'))].slice(-DESTINATION_PARTITION_RESOLVED_LIMIT) : []
  const scheduledDevelopments = Array.isArray(value.scheduledDevelopments) ? sortDevelopments(value.scheduledDevelopments.filter(validScheduledDevelopment).filter(development => !resolvedDevelopmentIds.includes(development.id))).slice(0, DESTINATION_PARTITION_SCHEDULE_LIMIT).map(development => ({ ...development })) : []
  const consequences = Array.isArray(value.consequences) ? value.consequences.filter(validConsequence).slice(-DESTINATION_PARTITION_CONSEQUENCE_LIMIT).map(consequence => ({ ...consequence })) : []
  const interventions = Array.isArray(value.interventions) ? value.interventions.filter(validIntervention).slice(-DESTINATION_PARTITION_INTERVENTION_LIMIT).map(intervention => ({ ...intervention })) : []
  const history = Array.isArray(value.history) ? value.history.filter(validHistory).slice(-DESTINATION_PARTITION_HISTORY_LIMIT).map(entry => ({ ...entry })) : []
  return { version: 1, id: fallback.id, contentRevision: 1, lastProcessedRouteReckoning, condition: value.condition, pressure: { id: value.pressure.id, label: value.pressure.label, value: clamp(value.pressure.value, 0, value.pressure.limit), limit: value.pressure.limit }, scheduledDevelopments, resolvedDevelopmentIds, consequences, interventions, history }
}

const normalizeReport = (value: unknown, fallback: DestinationKnownReport, destinationId: DestinationPartitionId, currentRouteReckoning: number): DestinationKnownReport => {
  if (!isRecord(value) || value.version !== 1 || value.destinationId !== destinationId || !isCondition(value.reportedCondition) || (value.source !== 'initial-chart' && value.source !== 'arrival' && value.source !== 'local-inspection') || !isNonNegativeInteger(value.observedAtRouteReckoning) || !isNonNegativeInteger(value.receivedAtRouteReckoning) || value.observedAtRouteReckoning > currentRouteReckoning || value.receivedAtRouteReckoning > currentRouteReckoning || (value.confidence !== 'confirmed' && value.confidence !== 'estimated')) return fallback
  return { version: 1, destinationId, reportedCondition: value.reportedCondition, source: value.source, observedAtRouteReckoning: value.observedAtRouteReckoning, receivedAtRouteReckoning: value.receivedAtRouteReckoning, confidence: value.confidence, ...(typeof value.knownConsequenceId === 'string' ? { knownConsequenceId: value.knownConsequenceId } : {}) }
}

/** Normalizes v4 state without simulating elapsed historical or offline time. */
export const normalizeDestinationWorld = (value: unknown, seed: number, currentRouteReckoning: number): DestinationWorldState => {
  const fallback = createDestinationWorld(seed, currentRouteReckoning)
  if (!isRecord(value) || value.version !== 1) return fallback
  const rawPartitions = value.partitions
  const rawReports = value.reports
  if (!isRecord(rawPartitions) || !isRecord(rawReports)) return fallback
  const partitions = Object.fromEntries(DESTINATION_PARTITION_IDS.map(id => [id, normalizePartition(rawPartitions[id], fallback.partitions[id], currentRouteReckoning)])) as DestinationWorldState['partitions']
  const reports = Object.fromEntries(DESTINATION_PARTITION_IDS.map(id => [id, normalizeReport(rawReports[id], fallback.reports[id], id, currentRouteReckoning)])) as DestinationWorldState['reports']
  return { version: 1, partitions, reports }
}
