import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentDomain, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { AutonomyObservation } from './autonomy'
import type { CausalCommandEvent, CausalHistoryCompactedSegment } from './causal-history'
import type { DelegatedTaskRecord } from './delegation'
import type { FrontierKnowledgeSourceKind, FrontierRevealedFact } from './frontier'
import type { PersistentPersonRecord } from './persistent-person'
import type { FoundationWorld } from './types'
import { validateFoundationWorld } from './world'

/**
 * The management sidebar is a strategic household-known projection. It is
 * deliberately not a map, notice, message feed, ledger, or conversation:
 * those future surfaces own spatial navigation, situated evidence/actions,
 * arrivals, full durable records/contracts, and person-specific consent and
 * negotiation respectively. This module owns no mutable world state.
 */
export const MANAGEMENT_SIDEBAR_CONTRACT_VERSION = 1 as const

export const MANAGEMENT_SIDEBAR_SECTION_IDS = [
  'overview',
  'people-work',
  'tasks',
  'risks',
  'known-sites-routes',
  'history'
] as const
export type ManagementSidebarSectionId = typeof MANAGEMENT_SIDEBAR_SECTION_IDS[number]

/** UI labels are a closed presentation map, never a source of world truth. */
export const MANAGEMENT_SIDEBAR_SECTION_LABELS: Readonly<Record<ManagementSidebarSectionId, string>> = {
  overview: 'OVERVIEW',
  'people-work': 'PEOPLE / WORK',
  tasks: 'TASKS',
  risks: 'RISKS',
  'known-sites-routes': 'KNOWN SITES / ROUTES',
  history: 'HISTORY'
}

export const MANAGEMENT_SIDEBAR_LIMITS = {
  overviewFacts: 4,
  peopleWorkFacts: 6,
  taskFacts: 6,
  riskFacts: 6,
  knownSiteRouteFacts: 6,
  historyFacts: 6,
  factsPerSection: 6,
  identityLength: 160
} as const

export type ManagementSidebarFactPriority = 'urgent' | 'essential' | 'standard'
export type ManagementSidebarFactCategory = ManagementSidebarSectionId
export type ManagementSidebarSourceType = 'household-state' | 'household-person-record' | 'delegation-record' | 'autonomy-observation' | 'household-journal' | 'compacted-household-journal' | 'frontier-knowledge'
export type ManagementSidebarSourceLabel = 'current-household-state' | 'crew-record' | 'delegated-task-record' | 'autonomy-record' | 'household-journal' | 'compacted-journal' | FrontierKnowledgeSourceKind

export const MANAGEMENT_SIDEBAR_SOURCE_LABELS: Readonly<Record<ManagementSidebarSourceLabel, string>> = {
  'current-household-state': 'HOUSEHOLD STATE',
  'crew-record': 'CREW RECORD',
  'delegated-task-record': 'TASK RECORD',
  'autonomy-record': 'AUTONOMY RECORD',
  'household-journal': 'HOUSEHOLD JOURNAL',
  'compacted-journal': 'COMPACTED JOURNAL',
  rumour: 'RUMOUR',
  chart: 'CHART',
  trader: 'TRADER',
  letter: 'LETTER',
  traveller: 'TRAVELLER',
  'cargo-mark': 'CARGO MARK',
  'institution-ledger': 'INSTITUTION LEDGER'
}

export type ManagementSidebarFreshness =
  | { kind: 'current' }
  | { kind: 'timeless' }
  | { kind: 'reported-freshness'; atWorldTime: number }

export interface ManagementSidebarFactSource {
  type: ManagementSidebarSourceType
  label: ManagementSidebarSourceLabel
  recordId: string
}

export type ManagementSidebarFactValue =
  | { kind: 'jomon-status'; vesselId: string; operationalStatus: 'moored'; locationKind: 'site' | 'quay'; locationId: string }
  | { kind: 'active-courier'; personId?: string; name?: string; role?: string }
  | { kind: 'world-time'; minutes: number }
  | { kind: 'world-era'; era: 'base' | 'ng-plus' | 'ng-plus-plus'; remixCycle: number }
  | { kind: 'household-person-work'; personId: string; name: string; role: string; availability: 'available' | 'committed' | 'unavailable'; currentWork: 'idle' | 'committed'; capacityCurrent: number; capacityMaximum: number; needsMaximum: number; health: 'steady' | 'strained' | 'injured' | 'recovering'; autonomyChoice?: AutonomyObservation['choice']; autonomyDetail?: AutonomyObservation['detail'] }
  | { kind: 'delegated-task'; taskId: string; family: string; status: DelegatedTaskRecord['status']; courierId: string; recipientId: string; risk: DelegatedTaskRecord['assessment']['risk']; plannedCompletionAtWorldTime?: number }
  | { kind: 'person-need-risk'; personId: string; name: string; needsMaximum: number }
  | { kind: 'person-health-risk'; personId: string; name: string; health: 'strained' | 'injured' | 'recovering' }
  | { kind: 'delegated-task-risk'; taskId: string; family: string; risk: DelegatedTaskRecord['assessment']['risk']; status: DelegatedTaskRecord['status'] }
  | { kind: 'no-known-active-risk' }
  | { kind: 'frontier-revealed-fact'; regionId: string; subjectKind: FrontierRevealedFact['subjectKind']; subjectId: string; factKind: FrontierRevealedFact['kind']; value: string }
  | { kind: 'causal-command'; sequence: number; commandKind: CausalCommandEvent['kind'] }
  | { kind: 'compacted-history-segment'; sequenceStart: number; sequenceEnd: number; worldTimeStart: number; worldTimeEnd: number; commandCount: number }

export type ManagementSidebarFactKind = ManagementSidebarFactValue['kind']

/** Every view fact is source-labelled, timestamped, safety-audited, and typed. */
export interface ManagementSidebarFact {
  id: string
  category: ManagementSidebarFactCategory
  kind: ManagementSidebarFactKind
  priority: ManagementSidebarFactPriority
  source: ManagementSidebarFactSource
  recordedAtWorldTime: number
  discoveredAtWorldTime: number
  freshness: ManagementSidebarFreshness
  value: ManagementSidebarFactValue
  contentDomain: MedievalContentDomain
  contentSafety: MedievalContentSafetyClassification
}

export interface ManagementSidebarSection {
  id: ManagementSidebarSectionId
  facts: readonly ManagementSidebarFact[]
}

export interface ManagementSidebarSummary {
  knownFactCount: number
  urgentFactCount: number
  sectionItemCounts: readonly { sectionId: ManagementSidebarSectionId; count: number }[]
}

export interface ManagementSidebarModel {
  version: typeof MANAGEMENT_SIDEBAR_CONTRACT_VERSION
  worldId: string
  worldTime: number
  sections: readonly ManagementSidebarSection[]
  summary: ManagementSidebarSummary
  contentSafetyAudit: MedievalContentSafetyAudit
}

export type ManagementSidebarDiagnosticCode =
  | 'management-sidebar.invalid-world'
  | 'management-sidebar.malformed-model'
  | 'management-sidebar.invalid-model'
  | 'management-sidebar.invalid-content-audit'
  | MedievalContentSafetyDiagnosticCode

export interface ManagementSidebarDiagnostic {
  id: string
  code: ManagementSidebarDiagnosticCode
}

export class ManagementSidebarContractError extends Error {
  constructor(readonly diagnostics: readonly ManagementSidebarDiagnostic[]) {
    super(`management sidebar rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'ManagementSidebarContractError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const priorityRank = (priority: ManagementSidebarFactPriority): number => priority === 'urgent' ? 0 : priority === 'essential' ? 1 : 2
const stateClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('player-facing-text', ['adult-labour', 'civil-life', 'navigation'], 'adults-only', ['data'])
const noRiskClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('player-facing-text', ['civil-life', 'navigation'], 'not-applicable', ['data'])

const householdStateSource = (recordId: string): ManagementSidebarFactSource => ({ type: 'household-state', label: 'current-household-state', recordId })
const personSource = (recordId: string): ManagementSidebarFactSource => ({ type: 'household-person-record', label: 'crew-record', recordId })
const taskSource = (recordId: string): ManagementSidebarFactSource => ({ type: 'delegation-record', label: 'delegated-task-record', recordId })
const autonomySource = (recordId: string): ManagementSidebarFactSource => ({ type: 'autonomy-observation', label: 'autonomy-record', recordId })
const journalSource = (recordId: string): ManagementSidebarFactSource => ({ type: 'household-journal', label: 'household-journal', recordId })
const segmentSource = (recordId: string): ManagementSidebarFactSource => ({ type: 'compacted-household-journal', label: 'compacted-journal', recordId })
const frontierSource = (fact: FrontierRevealedFact): ManagementSidebarFactSource => ({ type: 'frontier-knowledge', label: fact.source.kind, recordId: fact.source.sourceRecordId })

const fact = (value: Omit<ManagementSidebarFact, 'kind'> & { value: ManagementSidebarFactValue }): ManagementSidebarFact => ({ ...value, kind: value.value.kind })
const maximumNeed = (person: PersistentPersonRecord): number => Math.max(person.needs.nourishment, person.needs.rest, person.needs.shelter, person.needs.safety)
const roleLabel = (person: PersistentPersonRecord): string => person.work.role.kind === 'jomon-crew-role' ? person.work.role.role : person.work.role.occupation
const factLimitFor = (sectionId: ManagementSidebarSectionId): number => sectionId === 'overview'
  ? MANAGEMENT_SIDEBAR_LIMITS.overviewFacts
  : sectionId === 'people-work'
    ? MANAGEMENT_SIDEBAR_LIMITS.peopleWorkFacts
    : sectionId === 'tasks'
      ? MANAGEMENT_SIDEBAR_LIMITS.taskFacts
      : sectionId === 'risks'
        ? MANAGEMENT_SIDEBAR_LIMITS.riskFacts
        : sectionId === 'known-sites-routes'
          ? MANAGEMENT_SIDEBAR_LIMITS.knownSiteRouteFacts
          : MANAGEMENT_SIDEBAR_LIMITS.historyFacts

/** Urgent and essential facts precede standard facts; every tie is stable. */
const canonicalFacts = (facts: readonly ManagementSidebarFact[], limit: number): readonly ManagementSidebarFact[] => [...facts]
  .sort((left, right) => priorityRank(left.priority) - priorityRank(right.priority)
    || right.recordedAtWorldTime - left.recordedAtWorldTime
    || right.discoveredAtWorldTime - left.discoveredAtWorldTime
    || compare(left.id, right.id))
  .slice(0, limit)

const section = (id: ManagementSidebarSectionId, facts: readonly ManagementSidebarFact[]): ManagementSidebarSection => ({ id, facts: canonicalFacts(facts, factLimitFor(id)) })

const factContentRecords = (facts: readonly ManagementSidebarFact[]): readonly ClassifiedMedievalContent[] => facts.map(item => ({
  id: `management-sidebar:${item.id}`,
  domain: item.contentDomain,
  classification: item.contentSafety
}))

const commandTime = (world: FoundationWorld, command: CausalCommandEvent): number | undefined => {
  if (command.kind === 'initial-courier-selected') return 0
  if (command.kind === 'time-bearing-action') return world.state.temporal.causalRecords.find(record => record.kind === 'action-completed' && record.actionId === command.payload.action.id)?.atWorldTime
  if (command.kind === 'durable-jomon-growth') return command.payload.evidence.atWorldTime
  if (command.kind === 'delegation-offered') return world.state.delegation.tasks.find(task => task.offerId === command.payload.offer.id)?.offeredAtWorldTime
  return world.state.delegation.tasks.find(task => task.id === command.payload.interruption.taskId)?.outcome?.atWorldTime
}

const commandFacts = (world: FoundationWorld): readonly ManagementSidebarFact[] => world.state.causalHistory.tail.flatMap(command => {
  const atWorldTime = commandTime(world, command)
  return atWorldTime === undefined ? [] : [fact({
    id: `management:history:command:${command.id}`,
    category: 'history',
    priority: 'standard',
    source: journalSource(command.id),
    recordedAtWorldTime: atWorldTime,
    discoveredAtWorldTime: atWorldTime,
    freshness: { kind: 'timeless' },
    value: { kind: 'causal-command', sequence: command.sequence, commandKind: command.kind },
    contentDomain: 'event',
    contentSafety: command.contentSafety
  })]
})

const commandTotal = (segment: CausalHistoryCompactedSegment): number => segment.commandKinds.initialCourierSelected
  + segment.commandKinds.timeBearingAction
  + segment.commandKinds.durableJomonGrowth
  + segment.commandKinds.delegationOffered
  + segment.commandKinds.delegationInterrupted

const segmentFacts = (world: FoundationWorld): readonly ManagementSidebarFact[] => world.state.causalHistory.compactedSegments.map(segment => fact({
  id: `management:history:segment:${segment.id}`,
  category: 'history',
  priority: 'standard',
  source: segmentSource(segment.id),
  recordedAtWorldTime: segment.worldTimeEnd,
  discoveredAtWorldTime: segment.worldTimeEnd,
  freshness: { kind: 'timeless' },
  value: {
    kind: 'compacted-history-segment',
    sequenceStart: segment.sequenceStart,
    sequenceEnd: segment.sequenceEnd,
    worldTimeStart: segment.worldTimeStart,
    worldTimeEnd: segment.worldTimeEnd,
    commandCount: commandTotal(segment)
  },
  contentDomain: 'data',
  contentSafety: stateClassification()
}))

const frontierFacts = (world: FoundationWorld): readonly ManagementSidebarFact[] => world.state.geography.frontier.regions.flatMap(region => region.status === 'ungenerated'
  ? []
  : region.revealedFacts.map(revealed => fact({
      id: `management:known:${revealed.id}`,
      category: 'known-sites-routes',
      priority: 'standard',
      source: frontierSource(revealed),
      recordedAtWorldTime: revealed.source.reportedAtWorldTime,
      discoveredAtWorldTime: revealed.knownAtWorldTime,
      freshness: { kind: 'reported-freshness', atWorldTime: revealed.source.freshnessAtWorldTime },
      value: {
        kind: 'frontier-revealed-fact',
        regionId: revealed.regionId,
        subjectKind: revealed.subjectKind,
        subjectId: revealed.subjectId,
        factKind: revealed.kind,
        value: revealed.value
      },
      contentDomain: revealed.kind === 'history-link' ? 'history' : 'rumour',
      contentSafety: revealed.contentSafety
    })))

const overviewFacts = (world: FoundationWorld): readonly ManagementSidebarFact[] => {
  const courier = world.state.courier.initialCourierId === undefined ? undefined : world.state.people.records.find(person => person.id === world.state.courier.initialCourierId)
  const time = world.state.temporal.worldTime
  return [
    fact({
      id: 'management:overview:jomon', category: 'overview', priority: 'essential', source: householdStateSource(world.jomon.id), recordedAtWorldTime: time, discoveredAtWorldTime: 0, freshness: { kind: 'current' },
      value: { kind: 'jomon-status', vesselId: world.jomon.id, operationalStatus: world.state.jomon.operationalStatus, locationKind: world.state.jomon.location.kind, locationId: world.state.jomon.location.id },
      contentDomain: 'place', contentSafety: world.jomon.contentSafety
    }),
    fact({
      id: 'management:overview:courier', category: 'overview', priority: 'essential', source: courier === undefined ? householdStateSource('courier:unassigned') : personSource(courier.id), recordedAtWorldTime: time, discoveredAtWorldTime: 0, freshness: { kind: 'current' },
      value: courier === undefined ? { kind: 'active-courier' } : { kind: 'active-courier', personId: courier.id, name: courier.identity.name, role: roleLabel(courier) },
      contentDomain: 'person', contentSafety: courier?.identity.contentSafety ?? stateClassification()
    }),
    fact({
      id: 'management:overview:time', category: 'overview', priority: 'essential', source: householdStateSource('temporal:clock'), recordedAtWorldTime: time, discoveredAtWorldTime: 0, freshness: { kind: 'current' },
      value: { kind: 'world-time', minutes: time }, contentDomain: 'player-facing-text', contentSafety: stateClassification()
    }),
    fact({
      id: 'management:overview:era', category: 'overview', priority: 'standard', source: householdStateSource('world-era:state'), recordedAtWorldTime: time, discoveredAtWorldTime: 0, freshness: { kind: 'current' },
      value: { kind: 'world-era', era: world.state.era.era, remixCycle: world.state.era.remixCycle }, contentDomain: 'player-facing-text', contentSafety: stateClassification()
    })
  ]
}

const peopleFacts = (world: FoundationWorld): readonly ManagementSidebarFact[] => world.state.people.records.map(person => {
  const observation = world.state.autonomy.observations.find(candidate => candidate.personId === person.id)
  const essential = person.id === world.state.courier.initialCourierId || person.work.current.status === 'committed'
  const urgent = person.life.status === 'living' && (maximumNeed(person) >= 4 || person.health.condition === 'injured')
  return fact({
    id: `management:person:${person.id}`,
    category: 'people-work',
    priority: urgent ? 'urgent' : essential ? 'essential' : 'standard',
    source: observation === undefined ? personSource(person.id) : autonomySource(observation.id),
    recordedAtWorldTime: observation?.processedThroughWorldTime ?? world.state.temporal.worldTime,
    discoveredAtWorldTime: 0,
    freshness: { kind: 'current' },
    value: {
      kind: 'household-person-work',
      personId: person.id,
      name: person.identity.name,
      role: roleLabel(person),
      availability: person.work.availability,
      currentWork: person.work.current.status,
      capacityCurrent: person.work.capacity.current,
      capacityMaximum: person.work.capacity.maximum,
      needsMaximum: maximumNeed(person),
      health: person.health.condition,
      ...(observation === undefined ? {} : { autonomyChoice: observation.choice, autonomyDetail: observation.detail })
    },
    contentDomain: 'person',
    contentSafety: person.identity.contentSafety
  })
})

const taskFacts = (world: FoundationWorld): readonly ManagementSidebarFact[] => world.state.delegation.tasks.map(task => fact({
  id: `management:task:${task.id}`,
  category: 'tasks',
  priority: task.status === 'in-progress' ? 'essential' : 'standard',
  source: taskSource(task.id),
  recordedAtWorldTime: task.outcome?.atWorldTime ?? task.offeredAtWorldTime,
  discoveredAtWorldTime: task.offeredAtWorldTime,
  freshness: task.status === 'in-progress' ? { kind: 'current' } : { kind: 'timeless' },
  value: {
    kind: 'delegated-task',
    taskId: task.id,
    family: task.family,
    status: task.status,
    courierId: task.courierId,
    recipientId: task.recipientId,
    risk: task.assessment.risk,
    ...(task.plannedCompletionAtWorldTime === undefined ? {} : { plannedCompletionAtWorldTime: task.plannedCompletionAtWorldTime })
  },
  contentDomain: 'contract',
  contentSafety: task.contentSafety
}))

const riskFacts = (world: FoundationWorld): readonly ManagementSidebarFact[] => {
  const time = world.state.temporal.worldTime
  const personRisks = world.state.people.records.flatMap(person => {
    const needs = maximumNeed(person)
    const urgentNeeds = needs >= 4 ? [fact({
      id: `management:risk:needs:${person.id}`, category: 'risks', priority: 'urgent', source: personSource(person.id), recordedAtWorldTime: time, discoveredAtWorldTime: 0, freshness: { kind: 'current' },
      value: { kind: 'person-need-risk', personId: person.id, name: person.identity.name, needsMaximum: needs }, contentDomain: 'person', contentSafety: person.identity.contentSafety
    })] : []
    const health = person.health.condition === 'steady' ? [] : [fact({
      id: `management:risk:health:${person.id}`, category: 'risks', priority: person.health.condition === 'injured' ? 'urgent' : 'essential', source: personSource(person.id), recordedAtWorldTime: time, discoveredAtWorldTime: 0, freshness: { kind: 'current' },
      value: { kind: 'person-health-risk', personId: person.id, name: person.identity.name, health: person.health.condition }, contentDomain: 'person', contentSafety: person.identity.contentSafety
    })]
    return [...urgentNeeds, ...health]
  })
  const taskRisks = world.state.delegation.tasks.filter(task => task.status === 'in-progress').map(task => fact({
    id: `management:risk:task:${task.id}`, category: 'risks', priority: task.assessment.risk === 'high' ? 'urgent' : task.assessment.risk === 'guarded' ? 'essential' : 'standard', source: taskSource(task.id), recordedAtWorldTime: task.offeredAtWorldTime, discoveredAtWorldTime: task.offeredAtWorldTime, freshness: { kind: 'current' },
    value: { kind: 'delegated-task-risk', taskId: task.id, family: task.family, risk: task.assessment.risk, status: task.status }, contentDomain: 'contract', contentSafety: task.contentSafety
  }))
  const risks = [...personRisks, ...taskRisks]
  return risks.length ? risks : [fact({
    id: 'management:risk:none-known', category: 'risks', priority: 'standard', source: householdStateSource('household:risk-summary'), recordedAtWorldTime: time, discoveredAtWorldTime: 0, freshness: { kind: 'current' },
    value: { kind: 'no-known-active-risk' }, contentDomain: 'player-facing-text', contentSafety: noRiskClassification()
  })]
}

const rawModel = (world: FoundationWorld): ManagementSidebarModel => {
  const sections = [
    section('overview', overviewFacts(world)),
    section('people-work', peopleFacts(world)),
    section('tasks', taskFacts(world)),
    section('risks', riskFacts(world)),
    section('known-sites-routes', frontierFacts(world)),
    section('history', [...commandFacts(world), ...segmentFacts(world)])
  ] as const
  const allFacts = sections.flatMap(item => item.facts)
  const contentSafetyAudit = auditMedievalContentSafety(factContentRecords(allFacts))
  if (contentSafetyAudit.status === 'rejected') throw new ManagementSidebarContractError(contentSafetyAudit.diagnostics.map(diagnostic => ({ id: diagnostic.contentId, code: diagnostic.code })))
  return {
    version: MANAGEMENT_SIDEBAR_CONTRACT_VERSION,
    worldId: world.id,
    worldTime: world.state.temporal.worldTime,
    sections,
    summary: {
      knownFactCount: allFacts.length,
      urgentFactCount: allFacts.filter(item => item.priority === 'urgent').length,
      sectionItemCounts: sections.map(item => ({ sectionId: item.id, count: item.facts.length }))
    },
    contentSafetyAudit
  }
}

const ensureValidWorld = (world: FoundationWorld): void => {
  const diagnostics = validateFoundationWorld(world)
  if (diagnostics.length) throw new ManagementSidebarContractError(diagnostics.map(diagnostic => ({ id: diagnostic.recordId, code: 'management-sidebar.invalid-world' })))
}

/** Derives canonical, bounded household-known information without mutating the world. */
export const createManagementSidebarModel = (world: FoundationWorld): ManagementSidebarModel => {
  ensureValidWorld(world)
  return rawModel(world)
}

/** Exact reconstruction catches forged, reordered, unsafe, or omniscient view models. */
export const validateManagementSidebarModel = (world: FoundationWorld, value: unknown): readonly ManagementSidebarDiagnostic[] => {
  try {
    ensureValidWorld(world)
    const expected = rawModel(world)
    return same(value, expected) ? [] : [{ id: 'management-sidebar:model', code: 'management-sidebar.invalid-model' }]
  } catch (error) {
    if (error instanceof ManagementSidebarContractError) return error.diagnostics
    return [{ id: 'management-sidebar:model', code: 'management-sidebar.malformed-model' }]
  }
}

/** Compact counts for canvas ARIA; it intentionally contains no hidden world detail. */
export const managementSidebarAccessibleSummary = (model: ManagementSidebarModel, selectedSection: ManagementSidebarSectionId, expanded: boolean): string => {
  const selected = model.sections.find(section => section.id === selectedSection)
  if (!selected || !MANAGEMENT_SIDEBAR_SECTION_IDS.includes(selectedSection)) throw new ManagementSidebarContractError([{ id: 'management-sidebar:selection', code: 'management-sidebar.invalid-model' }])
  return `Management ${expanded ? 'expanded' : 'collapsed'}. ${MANAGEMENT_SIDEBAR_SECTION_LABELS[selectedSection]}, ${selected.facts.length} household-known facts. ${model.summary.urgentFactCount} urgent facts.`
}

/** Formatting helper for the canvas; all source/freshness inputs remain typed facts. */
export const managementSidebarFreshnessLabel = (freshness: ManagementSidebarFreshness): string => freshness.kind === 'current'
  ? 'CURRENT'
  : freshness.kind === 'timeless'
    ? 'TIMELESS'
    : `FRESH ${freshness.atWorldTime}M`
