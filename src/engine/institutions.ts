import type { DestinationConsequence, DestinationPartition, GalaxyState, GeneralManifestEvent, InstitutionActor, InstitutionActorMemory, InstitutionCampaignState, InstitutionCausalEvent, InstitutionId, InstitutionOperation, InstitutionOperationKind, InstitutionReport, InstitutionRouteModifier, InstitutionStanding, InstitutionWorldState } from '../types'
import { appendGeneralManifest } from './manifest'

export const INSTITUTION_OPERATION_LIMIT = 12
export const INSTITUTION_RESOLVED_OPERATION_LIMIT = 24
export const INSTITUTION_RELATIONSHIP_EFFECT_LIMIT = 48
// This accommodates every bounded durable reference (the 160-entry Manifest,
// actor memories, reports, operations, and relationship provenance) while
// keeping the campaign history deliberately small.
export const INSTITUTION_CAUSAL_EVENT_LIMIT = 320
export const INSTITUTION_REPORT_LIMIT = 16
export const INSTITUTION_ACTOR_LIMIT = 6
export const INSTITUTION_ACTOR_MEMORY_LIMIT = 8
export const INSTITUTION_ROUTE_MODIFIER_LIMIT = 8
export const INSTITUTION_DECISION_LIMIT = 12

export interface InstitutionDefinition {
  id: InstitutionId
  name: string
  civilization: 'human' | 'taal' | 'mixed'
  jurisdiction: string
  agenda: string
  publicPosture: string
  methods: readonly string[]
  disfavouredConduct: string
  actorGrammar: string
}

export const INSTITUTION_DEFINITIONS: readonly InstitutionDefinition[] = [
  {
    id: 'institution:nerida-port-continuity',
    name: 'Nerida Port Continuity Office',
    civilization: 'human',
    jurisdiction: 'Nerida Pressure Chain imported machinery and departure access',
    agenda: 'Keep the degrading pump bank operating under emergency port custody.',
    publicPosture: 'Emergency continuity requires one accountable customs chain.',
    methods: ['inspection', 'requisition', 'access audit', 'selective confiscation'],
    disfavouredConduct: 'Independent pump-control custody outside emergency authority.',
    actorGrammar: 'human examiner and continuity-office clerk names'
  },
  {
    id: 'institution:closure-eight',
    name: 'Closure Eight Cooperative',
    civilization: 'mixed',
    jurisdiction: 'Nerida Pressure Chain pump maintenance frames',
    agenda: 'Maintain ceramic control vanes and secure independent custody of pump controls.',
    publicPosture: 'A pressure change requires a named implementer and a named liability.',
    methods: ['repair', 'work-order negotiation', 'maintenance custody', 'relief support'],
    disfavouredConduct: 'Falsified inspection records and unworkable emergency directives.',
    actorGrammar: 'Taal maintenance-delegate names and mixed-workforce roles'
  },
  {
    id: 'institution:blue-intake-board',
    name: "Blue Intake Residents' Board",
    civilization: 'mixed',
    jurisdiction: 'Nerida Pressure Chain intake habitats',
    agenda: 'Keep the first-flooding habitats supplied, habitable, and represented in pump decisions.',
    publicPosture: 'Residents asked to absorb a failure are entitled to the first warning.',
    methods: ['relief filing', 'intake monitoring', 'access testimony', 'habitat coordination'],
    disfavouredConduct: 'Treating flooded households as an acceptable maintenance externality.',
    actorGrammar: 'resident-board clerk and intake-watcher names'
  }
]

const definitions = new Map(INSTITUTION_DEFINITIONS.map(definition => [definition.id, definition]))
const institutionIds = INSTITUTION_DEFINITIONS.map(definition => definition.id)
const clamp = (value: number, min = -6, max = 6): number => Math.max(min, Math.min(max, Math.round(value)))
const appendBounded = <T>(items: readonly T[], item: T, limit: number): T[] => [...items, item].slice(-limit)
const unique = (values: readonly string[]): string[] => [...new Set(values)]
const currentDestination = 'destination:nerida' as const

const initialStanding = (influence: number): InstitutionStanding => ({ trust: 0, scrutiny: 0, obligation: 0, grievance: 0, influence })
const initialStates = (): Record<InstitutionId, InstitutionCampaignState> => ({
  'institution:nerida-port-continuity': { version: 1, id: 'institution:nerida-port-continuity', capacity: 4, currentConcern: 'pump-bank emergency custody', standing: initialStanding(5), relations: { 'institution:closure-eight': -2, 'institution:blue-intake-board': 0 } },
  'institution:closure-eight': { version: 1, id: 'institution:closure-eight', capacity: 3, currentConcern: 'ceramic vane maintenance and custody', standing: initialStanding(4), relations: { 'institution:nerida-port-continuity': -2, 'institution:blue-intake-board': 2 } },
  'institution:blue-intake-board': { version: 1, id: 'institution:blue-intake-board', capacity: 2, currentConcern: 'first-flooding intake habitats', standing: initialStanding(3), relations: { 'institution:nerida-port-continuity': -1, 'institution:closure-eight': 2 } }
})

const initialActors = (): InstitutionActor[] => [
  { version: 1, id: 'actor:iren-vos', name: 'Iren Vos', civilization: 'human', affiliationId: 'institution:nerida-port-continuity', role: 'customs official', rank: 'examiner', destinationId: currentDestination, status: 'active', methods: ['inspection', 'selective confiscation'], jomonGrievance: 0, memories: [], operationalHistory: [] },
  { version: 1, id: 'actor:sava-tesh', name: 'Sava Tesh', civilization: 'taal', affiliationId: 'institution:closure-eight', role: 'pump maintenance delegate', rank: 'closure witness', destinationId: currentDestination, status: 'active', methods: ['repair', 'liability split'], jomonGrievance: 0, memories: [], operationalHistory: [] },
  { version: 1, id: 'actor:mera-lio', name: 'Mera Lio', civilization: 'human', affiliationId: 'institution:blue-intake-board', role: 'intake emergency clerk', rank: 'board witness', destinationId: currentDestination, status: 'active', methods: ['relief filing', 'resident testimony'], jomonGrievance: 0, memories: [], operationalHistory: [] }
]

export const institutionDefinition = (id: InstitutionId): InstitutionDefinition => definitions.get(id)!
export const institutionStandingLabel = (standing: InstitutionStanding): string => standing.grievance >= 3 ? 'GRIEVANCE RECORDED' : standing.scrutiny >= 3 ? 'UNDER SCRUTINY' : standing.obligation >= 2 ? 'OBLIGATION OUTSTANDING' : standing.trust >= 2 ? 'CONDITIONAL RELIABILITY' : 'UNSETTLED'
export const institutionReportFreshness = (report: InstitutionReport, routeReckoning: number): 'current' | 'aging' | 'stale' => {
  const age = Math.max(0, routeReckoning - report.reportAtRouteReckoning)
  return age > 360 ? 'stale' : age > 120 ? 'aging' : 'current'
}

export const createInstitutionWorld = (_seed: number, _startAtRouteReckoning = 0, lastProcessedManifestSequence = -1): InstitutionWorldState => ({
  version: 1,
  contentRevision: 1,
  states: initialStates(),
  actors: initialActors(),
  operations: [],
  resolvedOperationIds: [],
  relationshipEffectIds: [],
  causalEvents: [],
  reports: [],
  knownRouteModifiers: [],
  lastProcessedManifestSequence: Math.max(-1, Math.floor(lastProcessedManifestSequence)),
  nextCausalSequence: 0,
  completedDecisionIds: [],
  successorCounts: {}
})

const cloneStanding = (standing: InstitutionStanding): InstitutionStanding => ({ ...standing })
const cloneState = (state: InstitutionCampaignState): InstitutionCampaignState => ({ ...state, standing: cloneStanding(state.standing), relations: { ...state.relations } })
const cloneMemory = (memory: InstitutionActorMemory): InstitutionActorMemory => ({ ...memory })
const cloneActor = (actor: InstitutionActor): InstitutionActor => ({ ...actor, methods: [...actor.methods], memories: actor.memories.map(cloneMemory), operationalHistory: [...actor.operationalHistory] })
const cloneOperation = (operation: InstitutionOperation): InstitutionOperation => ({ ...operation, parentEventIds: [...operation.parentEventIds] })
const cloneCausal = (event: InstitutionCausalEvent): InstitutionCausalEvent => ({ ...event, parentIds: [...event.parentIds], effects: { ...event.effects } })

export const cloneInstitutionWorld = (world: InstitutionWorldState): InstitutionWorldState => ({
  ...world,
  states: Object.fromEntries(institutionIds.map(id => [id, cloneState(world.states[id])])) as InstitutionWorldState['states'],
  actors: world.actors.map(cloneActor),
  operations: world.operations.map(cloneOperation),
  resolvedOperationIds: [...world.resolvedOperationIds],
  relationshipEffectIds: [...world.relationshipEffectIds],
  causalEvents: world.causalEvents.map(cloneCausal),
  reports: world.reports.map(report => ({ ...report })),
  knownRouteModifiers: world.knownRouteModifiers.map(modifier => ({ ...modifier })),
  ...(world.rival ? { rival: { ...world.rival } } : {}),
  completedDecisionIds: [...world.completedDecisionIds],
  successorCounts: { ...world.successorCounts }
})

const causalFor = (world: InstitutionWorldState, id: string): InstitutionCausalEvent | undefined => world.causalEvents.find(event => event.id === id)
const actorFor = (world: InstitutionWorldState, id: string | undefined): InstitutionActor | undefined => id ? world.actors.find(actor => actor.id === id) : undefined
const operationFor = (world: InstitutionWorldState, id: string): InstitutionOperation | undefined => world.operations.find(operation => operation.id === id)
const causalReferences = (world: InstitutionWorldState, manifestCausalEventIds: readonly string[]): Set<string> => new Set<string>([
  ...manifestCausalEventIds,
  ...world.actors.flatMap(actor => actor.memories.map(memory => memory.causalEventId)),
  ...world.operations.flatMap(operation => operation.parentEventIds),
  ...world.reports.map(report => report.causalEventId),
  ...world.relationshipEffectIds.flatMap(effectId => world.causalEvents.filter(event => effectId.includes(event.id)).map(event => event.id)),
  ...(world.rival ? [world.rival.emergenceCausalEventId] : [])
])

/**
 * Drops only causal leaves that no surviving durable record names. If a
 * private causal bridge is the only way to reach a retained event, its parent
 * references are folded into that child before the bridge is removed. The
 * applied state already lives in the destination/institution records, so this
 * retains deterministic continuation without leaving dangling references.
 */
const compactCausalEvents = (world: InstitutionWorldState, manifestCausalEventIds: readonly string[], retainedId?: string): void => {
  while (world.causalEvents.length > INSTITUTION_CAUSAL_EVENT_LIMIT) {
    const protectedIds = causalReferences(world, manifestCausalEventIds)
    if (retainedId) protectedIds.add(retainedId)
    const parentIds = new Set(world.causalEvents.flatMap(event => event.parentIds))
    const removable = world.causalEvents.findIndex(event => !protectedIds.has(event.id) && !parentIds.has(event.id))
    if (removable >= 0) {
      world.causalEvents.splice(removable, 1)
      continue
    }
    const bridgeIndex = world.causalEvents.findIndex(event => !protectedIds.has(event.id) && event.id !== retainedId && world.causalEvents.some(child => child.parentIds.includes(event.id)))
    if (bridgeIndex < 0) {
      // Valid bounded records can protect at most 285 direct causal events.
      // Reaching this branch means malformed save input exceeds its limits;
      // keep the retained records instead of silently severing their history.
      return
    }
    const bridge = world.causalEvents[bridgeIndex]!
    for (const child of world.causalEvents) {
      if (!child.parentIds.includes(bridge.id)) continue
      child.parentIds = unique([...child.parentIds.filter(parentId => parentId !== bridge.id), ...bridge.parentIds])
        .filter(parentId => {
          const parent = causalFor(world, parentId)
          return Boolean(parent && parent.sequence < child.sequence)
        })
      child.effects = { ...child.effects, compactedParentId: bridge.id }
    }
    world.causalEvents.splice(bridgeIndex, 1)
  }
}
const appendCausal = (galaxy: GalaxyState, input: Omit<InstitutionCausalEvent, 'version' | 'sequence'>): InstitutionCausalEvent => {
  const world = galaxy.institutionWorld
  const existing = causalFor(world, input.id)
  if (existing) return existing
  const parentIds = unique(input.parentIds).filter(parentId => {
    const parent = causalFor(world, parentId)
    return Boolean(parent && parent.sequence < world.nextCausalSequence)
  })
  const event: InstitutionCausalEvent = { version: 1, sequence: world.nextCausalSequence++, ...input, parentIds }
  world.causalEvents.push(event)
  compactCausalEvents(world, galaxy.generalManifest.entries.flatMap(entry => entry.causalEventId ? [entry.causalEventId] : []), event.id)
  return event
}

const appendActorMemory = (actor: InstitutionActor, memory: InstitutionActorMemory): void => {
  if (actor.memories.some(candidate => candidate.id === memory.id)) return
  actor.memories = appendBounded(actor.memories, memory, INSTITUTION_ACTOR_MEMORY_LIMIT)
}

const remember = (galaxy: GalaxyState, actorId: string, causal: InstitutionCausalEvent, kind: InstitutionActorMemory['kind'], summary: string, witnessed: boolean, courierId?: string): void => {
  const actor = actorFor(galaxy.institutionWorld, actorId)
  if (!actor) return
  appendActorMemory(actor, { version: 1, id: `memory:${actor.id}:${causal.id}:${kind}`, causalEventId: causal.id, kind, atRouteReckoning: causal.atRouteReckoning, witnessed, ...(courierId ? { courierId } : {}), summary })
}

const changeStanding = (galaxy: GalaxyState, institutionId: InstitutionId, causal: InstitutionCausalEvent, effectKey: string, changes: Partial<InstitutionStanding>): boolean => {
  const world = galaxy.institutionWorld
  if (world.relationshipEffectIds.includes(effectKey)) return false
  const state = world.states[institutionId]
  state.standing = {
    trust: clamp(state.standing.trust + (changes.trust ?? 0)),
    scrutiny: clamp(state.standing.scrutiny + (changes.scrutiny ?? 0)),
    obligation: clamp(state.standing.obligation + (changes.obligation ?? 0)),
    grievance: clamp(state.standing.grievance + (changes.grievance ?? 0)),
    influence: clamp(state.standing.influence + (changes.influence ?? 0), 0, 6)
  }
  world.relationshipEffectIds = appendBounded(world.relationshipEffectIds, effectKey, INSTITUTION_RELATIONSHIP_EFFECT_LIMIT)
  appendGeneralManifest(galaxy, {
    kind: 'institutionRelationshipChanged',
    detail: `${institutionDefinition(institutionId).name} updated its Jomon standing after ${causal.summary}`,
    source: 'institution',
    institutionId,
    causalEventId: causal.id,
    siteId: 'sector-00:site-03',
    payload: {
      trust: changes.trust ?? 0,
      scrutiny: changes.scrutiny ?? 0,
      obligation: changes.obligation ?? 0,
      grievance: changes.grievance ?? 0,
      influence: changes.influence ?? 0
    }
  })
  return true
}

const appendReport = (world: InstitutionWorldState, report: InstitutionReport): void => {
  if (world.reports.some(candidate => candidate.id === report.id)) return
  world.reports = appendBounded(world.reports, report, INSTITUTION_REPORT_LIMIT)
}

const appendRouteModifier = (world: InstitutionWorldState, modifier: InstitutionRouteModifier): void => {
  const index = world.knownRouteModifiers.findIndex(candidate => candidate.id === modifier.id)
  if (index >= 0) world.knownRouteModifiers[index] = modifier
  else world.knownRouteModifiers = appendBounded(world.knownRouteModifiers, modifier, INSTITUTION_ROUTE_MODIFIER_LIMIT)
}

const schedule = (world: InstitutionWorldState, operation: InstitutionOperation): void => {
  if (world.resolvedOperationIds.includes(operation.id) || operationFor(world, operation.id)) return
  world.operations = [...world.operations, operation].sort((left, right) => left.dueAtRouteReckoning - right.dueAtRouteReckoning || left.id.localeCompare(right.id)).slice(0, INSTITUTION_OPERATION_LIMIT)
}

const resolveOperationId = (world: InstitutionWorldState, operation: InstitutionOperation): void => {
  operation.status = 'resolved'
  if (!world.resolvedOperationIds.includes(operation.id)) world.resolvedOperationIds = appendBounded(world.resolvedOperationIds, operation.id, INSTITUTION_RESOLVED_OPERATION_LIMIT)
}

const addDestinationConsequence = (partition: DestinationPartition, id: string, kind: DestinationConsequence['kind'], atRouteReckoning: number): void => {
  const existing = partition.consequences.find(candidate => candidate.id === id)
  if (existing) { existing.active = true; return }
  partition.consequences = [...partition.consequences, { id, kind, createdAtRouteReckoning: atRouteReckoning, active: true }].slice(-8)
}

const adjustPressure = (partition: DestinationPartition, amount: number): void => {
  partition.pressure.value = Math.max(0, Math.min(partition.pressure.limit, Math.round(partition.pressure.value + amount)))
}

const operationDetail = (operation: InstitutionOperation): { summary: string; route?: Omit<InstitutionRouteModifier, 'version' | 'operationId' | 'knownAtRouteReckoning'>; pressure: number; consequence?: DestinationConsequence['kind'] } => {
  if (operation.kind === 'port-requisition') return { summary: 'The Port Continuity Office requisitioned ceramic feedstock under emergency custody.', pressure: 6, consequence: 'local-operation' }
  if (operation.kind === 'port-access-audit') return { summary: 'The Port Continuity Office opened a custody access audit on the Jomon.', pressure: 7, consequence: 'route-modifier', route: { id: `route-modifier:${operation.id}`, destinationId: currentDestination, warning: 'known Port Continuity access audit requires a recorded transfer window', durationMarks: 30, risk: 'elevated' } }
  if (operation.kind === 'closure-pump-repair') return { summary: 'Closure Eight installed a ceramic-vane repair order at Pump Bank Four.', pressure: -22, consequence: 'local-operation' }
  if (operation.kind === 'blue-intake-relief') return { summary: "Blue Intake redirected filtered feedstock so repair organisms would not clear inhabited intake structures.", pressure: -8, consequence: 'local-operation' }
  if (operation.kind === 'iren-evidence-inspection') {
    const delay = operation.purpose.includes('compliance review') ? 30 : 60
    return { summary: operation.purpose.includes('compliance review') ? 'Examiner Iren Vos filed a compliance review before releasing the route record.' : 'Examiner Iren Vos arranged an evidence inspection against the Jomon custody record.', pressure: 4, consequence: 'route-modifier', route: { id: `route-modifier:${operation.id}`, destinationId: currentDestination, warning: delay === 30 ? 'known Iren Vos compliance review may delay Nerida transfer' : 'known Iren Vos evidence inspection requires an extended transfer window', durationMarks: delay, risk: delay === 30 ? 'elevated' : 'high' } }
  }
  if (operation.kind === 'iren-bypass-custody-audit') return { summary: 'Examiner Iren Vos contested bypass custody and imposed a documented access audit.', pressure: 6, consequence: 'route-modifier', route: { id: `route-modifier:${operation.id}`, destinationId: currentDestination, warning: 'known Iren Vos bypass-custody audit restricts Nerida access', durationMarks: 60, risk: 'high' } }
  return { summary: 'The Port Continuity Office reassigned its examination desk after a documented vacancy.', pressure: 0 }
}

const knownOperation = (galaxy: GalaxyState, operation: InstitutionOperation, causal: InstitutionCausalEvent, detail: ReturnType<typeof operationDetail>): void => {
  const world = galaxy.institutionWorld
  appendReport(world, {
    version: 1,
    id: `report:${operation.id}`,
    institutionId: operation.institutionId,
    ...(operation.actorId ? { actorId: operation.actorId } : {}),
    destinationId: operation.destinationId,
    causalEventId: causal.id,
    source: 'institution-notice',
    reportAtRouteReckoning: galaxy.routeReckoning,
    confidence: operation.kind === 'port-requisition' ? 'contested' : 'confirmed',
    ...(operation.kind === 'port-requisition' ? { bias: 'Port Office emergency custody notice' } : {}),
    summary: detail.summary
  })
  if (detail.route) {
    if (operation.actorId === 'actor:iren-vos') world.knownRouteModifiers = world.knownRouteModifiers.filter(modifier => !modifier.operationId.startsWith('operation:iren-vos:'))
    appendRouteModifier(world, { version: 1, operationId: operation.id, knownAtRouteReckoning: galaxy.routeReckoning, ...detail.route })
  }
  appendGeneralManifest(galaxy, { kind: 'institutionOperationKnown', detail: detail.summary, source: 'institution', institutionId: operation.institutionId, ...(operation.actorId ? { actorId: operation.actorId } : {}), causalEventId: causal.id, siteId: 'sector-00:site-03', payload: { operationId: operation.id, operationKind: operation.kind, destinationId: operation.destinationId } })
}

const scheduleRivalAction = (galaxy: GalaxyState, causal: InstitutionCausalEvent, _memoryKind: InstitutionActorMemory['kind']): void => {
  const world = galaxy.institutionWorld
  const rival = world.rival
  if (!rival || rival.status !== 'active') return
  const actor = actorFor(world, rival.actorId)
  if (!actor || actor.status !== 'active') return
  const compliant = actor.memories.some(memory => memory.kind === 'compliance')
  const kind: InstitutionOperationKind = actor.memories.some(memory => memory.kind === 'seal-breach') ? 'iren-evidence-inspection' : 'iren-bypass-custody-audit'
  const id = `operation:iren-vos:${causal.id}`
  schedule(world, {
    version: 1,
    id,
    institutionId: 'institution:nerida-port-continuity',
    actorId: actor.id,
    destinationId: currentDestination,
    kind,
    initiatedAtRouteReckoning: galaxy.routeReckoning,
    dueAtRouteReckoning: galaxy.routeReckoning + 240,
    purpose: kind === 'iren-evidence-inspection' && compliant ? 'compliance review following a prior documented response' : kind === 'iren-evidence-inspection' ? 'evidence review following a seal breach' : 'bypass-custody audit following interference or refusal',
    status: 'scheduled',
    visibility: 'known',
    parentEventIds: [causal.id]
  })
}

const emergeRival = (galaxy: GalaxyState, parent: InstitutionCausalEvent, memoryKind: InstitutionActorMemory['kind'], courierId?: string): void => {
  const world = galaxy.institutionWorld
  const actor = actorFor(world, 'actor:iren-vos')
  if (!actor) return
  const causal = appendCausal(galaxy, {
    id: `causal:${galaxy.seed}:rival-emergence:${parent.id}`,
    atRouteReckoning: galaxy.routeReckoning,
    kind: 'rival-emerged',
    parentIds: [parent.id],
    institutionId: 'institution:nerida-port-continuity',
    actorId: actor.id,
    destinationId: currentDestination,
    ...(courierId ? { courierId } : {}),
    knownToJomon: true,
    learnedThrough: 'manifest',
    summary: `Examiner Iren Vos opened a personal continuity file on Jomon after ${memoryKind.replaceAll('-', ' ')}.`,
    effects: { trigger: memoryKind }
  })
  remember(galaxy, actor.id, parent, memoryKind, parent.summary, true, courierId)
  if (!world.rival) {
    world.rival = { version: 1, id: 'rival:iren-vos', actorId: actor.id, institutionId: 'institution:nerida-port-continuity', status: 'active', emergedAtRouteReckoning: galaxy.routeReckoning, emergenceCausalEventId: causal.id, ...(courierId ? { originalCourierId: courierId } : {}) }
    appendGeneralManifest(galaxy, { kind: 'rivalEmergence', detail: 'Examiner Iren Vos has opened a continuity file on Jomon.', source: 'institution', institutionId: 'institution:nerida-port-continuity', actorId: actor.id, causalEventId: causal.id, ...(courierId ? { courierId } : {}), siteId: 'sector-00:site-03', payload: { rivalId: 'rival:iren-vos', trigger: memoryKind } })
  }
  actor.jomonGrievance = clamp(actor.jomonGrievance + 2, 0, 6)
  scheduleRivalAction(galaxy, parent, memoryKind)
}

const processManifestEntry = (galaxy: GalaxyState, entry: GeneralManifestEvent): void => {
  const world = galaxy.institutionWorld
  const causal = appendCausal(galaxy, {
    id: `causal:${entry.id}`,
    atRouteReckoning: entry.routeReckoning ?? galaxy.routeReckoning,
    kind: 'manifest-observed',
    parentIds: [],
    ...(entry.courierId ? { courierId: entry.courierId } : {}),
    ...(entry.id ? { manifestId: entry.id } : {}),
    knownToJomon: true,
    learnedThrough: 'manifest',
    summary: entry.detail,
    effects: { manifestKind: entry.kind }
  })
  const port = 'institution:nerida-port-continuity' as const
  const closure = 'institution:closure-eight' as const
  if (entry.kind === 'sealViolated') {
    changeStanding(galaxy, port, causal, `standing:${causal.id}:port-seal`, { trust: -2, scrutiny: 2, grievance: 1 })
    emergeRival(galaxy, causal, 'seal-breach', entry.courierId)
  } else if (entry.kind === 'destinationIntervention' && entry.payload?.interventionId === 'intervention:nerida:bypass-installation') {
    changeStanding(galaxy, port, causal, `standing:${causal.id}:port-bypass`, { scrutiny: 2, grievance: 2 })
    changeStanding(galaxy, closure, causal, `standing:${causal.id}:closure-bypass`, { trust: 1, obligation: 1 })
    emergeRival(galaxy, causal, 'bypass-interference', entry.courierId)
  } else if (entry.kind === 'deliveryCompleted') {
    const packageRecord = entry.packageId ? galaxy.sealedPackages.find(candidate => candidate.id === entry.packageId) : undefined
    if (packageRecord?.sealState === 'intact') {
      changeStanding(galaxy, port, causal, `standing:${causal.id}:port-intact`, { trust: 2, scrutiny: -1 })
      changeStanding(galaxy, closure, causal, `standing:${causal.id}:closure-intact`, { trust: 1 })
    } else changeStanding(galaxy, port, causal, `standing:${causal.id}:port-tampered`, { trust: -2, scrutiny: 2, grievance: 2 })
  } else if (entry.kind === 'contractDeclined') changeStanding(galaxy, port, causal, `standing:${causal.id}:port-declined`, { trust: -1, obligation: 1 })
  else if (entry.kind === 'contractExpired') changeStanding(galaxy, port, causal, `standing:${causal.id}:port-expired`, { trust: -2, obligation: 2, grievance: 1 })
  else if (entry.kind === 'deliveryFailed' && entry.detail.startsWith('Delivery refused')) changeStanding(galaxy, port, causal, `standing:${causal.id}:port-refused`, { trust: -1, scrutiny: 2, grievance: 2 })
  else if (entry.kind === 'deliveryFailed' && entry.detail.startsWith('Abandonment closed')) changeStanding(galaxy, port, causal, `standing:${causal.id}:port-abandoned`, { trust: -3, obligation: 2, grievance: 2 })
  else if (entry.kind === 'deliveryFailed' && !entry.detail.startsWith('Deadline failure recorded')) changeStanding(galaxy, port, causal, `standing:${causal.id}:port-failed`, { trust: -1, scrutiny: 1, grievance: 1 })
  else if (entry.kind === 'packageLost') changeStanding(galaxy, closure, causal, `standing:${causal.id}:closure-loss`, { trust: -1, obligation: 1 })
  else if (entry.kind === 'courierStatusChanged' && entry.payload?.status === 'dead' && world.rival && entry.courierId) {
    const rival = world.rival
    const actor = actorFor(world, rival.actorId)
    if (actor && (rival.originalCourierId === entry.courierId || actor.memories.some(memory => memory.courierId === entry.courierId))) {
      remember(galaxy, actor.id, causal, 'courier-loss', `${entry.detail} Iren Vos retains the vessel file.`, false, entry.courierId)
      appendGeneralManifest(galaxy, { kind: 'rivalEncounter', detail: 'Iren Vos amended the Jomon continuity file after the courier loss.', source: 'institution', institutionId: rival.institutionId, actorId: actor.id, causalEventId: causal.id, courierId: entry.courierId, siteId: 'sector-00:site-03', payload: { rivalId: rival.id, continuity: 'vessel-retained' } })
    }
  }
}

/** Reconciles new known custody, destination, and courier records once. Call only on a cloned campaign state. */
export const reconcileInstitutionalManifest = (galaxy: GalaxyState): void => {
  const world = galaxy.institutionWorld
  const entries = galaxy.generalManifest.entries.filter(entry => entry.sequence > world.lastProcessedManifestSequence).sort((left, right) => left.sequence - right.sequence)
  if (!entries.length) return
  for (const entry of entries) processManifestEntry(galaxy, entry)
  world.lastProcessedManifestSequence = galaxy.generalManifest.nextSequence - 1
}

const scheduleConditionOperations = (galaxy: GalaxyState): void => {
  const world = galaxy.institutionWorld
  const nerida = galaxy.destinationWorld.partitions[currentDestination]
  if (!nerida || (nerida.condition !== 'cavitation-restriction' && nerida.condition !== 'bypass-stabilizing')) return
  const portOperationId = 'operation:port:nerida-emergency-requisition'
  const reliefOperationId = 'operation:blue-intake:habitat-relief'
  const hasPortOperation = world.resolvedOperationIds.includes(portOperationId) || Boolean(operationFor(world, portOperationId))
  const hasReliefOperation = world.resolvedOperationIds.includes(reliefOperationId) || Boolean(operationFor(world, reliefOperationId))
  if (hasPortOperation && hasReliefOperation) return
  const parent = appendCausal(galaxy, {
    id: `causal:${galaxy.seed}:nerida-pump-pressure:${nerida.condition}:${nerida.lastProcessedRouteReckoning}`,
    atRouteReckoning: galaxy.routeReckoning,
    kind: 'institution-operation',
    parentIds: [],
    destinationId: currentDestination,
    knownToJomon: false,
    learnedThrough: 'route-report',
    summary: 'Nerida pump pressure made institutional intervention materially necessary.',
    effects: { pressure: nerida.pressure.value, condition: nerida.condition }
  })
  if (!hasPortOperation) schedule(world, { version: 1, id: portOperationId, institutionId: 'institution:nerida-port-continuity', destinationId: currentDestination, kind: 'port-requisition', initiatedAtRouteReckoning: galaxy.routeReckoning, dueAtRouteReckoning: galaxy.routeReckoning + 60, purpose: 'secure feedstock under emergency custody', status: 'scheduled', visibility: 'hidden', parentEventIds: [parent.id] })
  if (!hasReliefOperation) schedule(world, { version: 1, id: reliefOperationId, institutionId: 'institution:blue-intake-board', actorId: 'actor:mera-lio', destinationId: currentDestination, kind: 'blue-intake-relief', initiatedAtRouteReckoning: galaxy.routeReckoning, dueAtRouteReckoning: galaxy.routeReckoning + 180, purpose: 'protect intake habitat filters from autonomous repair organisms', status: 'scheduled', visibility: 'hidden', parentEventIds: [parent.id] })
}

const resolveOperation = (galaxy: GalaxyState, operation: InstitutionOperation): void => {
  const world = galaxy.institutionWorld
  if (world.resolvedOperationIds.includes(operation.id)) return
  const detail = operationDetail(operation)
  const partition = galaxy.destinationWorld.partitions[operation.destinationId]
  if (partition) {
    adjustPressure(partition, detail.pressure)
    if (detail.consequence) addDestinationConsequence(partition, `consequence:${operation.id}`, detail.consequence, operation.dueAtRouteReckoning)
  }
  const causal = appendCausal(galaxy, {
    id: `causal:${galaxy.seed}:operation:${operation.id}`,
    atRouteReckoning: operation.dueAtRouteReckoning,
    kind: 'institution-operation',
    parentIds: operation.parentEventIds,
    institutionId: operation.institutionId,
    ...(operation.actorId ? { actorId: operation.actorId } : {}),
    destinationId: operation.destinationId,
    knownToJomon: operation.visibility === 'known',
    learnedThrough: operation.visibility === 'known' ? 'institution-notice' : 'route-report',
    summary: detail.summary,
    effects: { operationId: operation.id, operationKind: operation.kind, pressure: detail.pressure }
  })
  operation.resolvedAtRouteReckoning = operation.dueAtRouteReckoning
  resolveOperationId(world, operation)
  const actor = actorFor(world, operation.actorId)
  if (actor) actor.operationalHistory = appendBounded(actor.operationalHistory, operation.id, INSTITUTION_RESOLVED_OPERATION_LIMIT)
  if (operation.visibility === 'known') knownOperation(galaxy, operation, causal, detail)
  if (operation.kind === 'closure-pump-repair' && world.rival?.actorId === 'actor:iren-vos') {
    const rivalActor = actorFor(world, 'actor:iren-vos')
    if (rivalActor && rivalActor.status === 'active' && rivalActor.memories.some(memory => memory.kind === 'assistance')) {
      rivalActor.status = 'disgraced'
      world.rival.status = 'reassigned'
      const statusCausal = appendCausal(galaxy, { id: `causal:${galaxy.seed}:actor-status:${rivalActor.id}:disgraced:${operation.id}`, atRouteReckoning: galaxy.routeReckoning, kind: 'actor-status', parentIds: [causal.id], institutionId: rivalActor.affiliationId, actorId: rivalActor.id, destinationId: currentDestination, knownToJomon: true, learnedThrough: 'institution-notice', summary: 'The Port Office removed Iren Vos from the emergency examination desk after the Closure Eight repair order.', effects: { status: 'disgraced' } })
      appendGeneralManifest(galaxy, { kind: 'institutionActorStatusChanged', detail: statusCausal.summary, source: 'institution', institutionId: rivalActor.affiliationId, actorId: rivalActor.id, causalEventId: statusCausal.id, siteId: 'sector-00:site-03', payload: { status: 'disgraced' } })
      schedule(world, { version: 1, id: 'operation:port:iren-vos-successor', institutionId: 'institution:nerida-port-continuity', destinationId: currentDestination, kind: 'actor-reassignment', initiatedAtRouteReckoning: galaxy.routeReckoning, dueAtRouteReckoning: galaxy.routeReckoning + 240, purpose: 'fill the documented examination vacancy', status: 'scheduled', visibility: 'known', parentEventIds: [statusCausal.id] })
    }
  }
  if (operation.kind === 'actor-reassignment') appointSuccessor(galaxy, causal)
}

const appointSuccessor = (galaxy: GalaxyState, parent: InstitutionCausalEvent): void => {
  const world = galaxy.institutionWorld
  const office = 'institution:nerida-port-continuity' as const
  if ((world.successorCounts[office] ?? 0) >= 1 || world.actors.some(actor => actor.id === 'actor:nadi-rell')) return
  const predecessor = actorFor(world, 'actor:iren-vos')
  const successor: InstitutionActor = { version: 1, id: 'actor:nadi-rell', name: 'Nadi Rell', civilization: 'human', affiliationId: office, role: 'customs official', rank: 'acting examiner', destinationId: currentDestination, status: 'active', methods: ['briefing review', 'custody audit'], jomonGrievance: 0, memories: [], operationalHistory: [] }
  world.actors = appendBounded(world.actors, successor, INSTITUTION_ACTOR_LIMIT)
  world.successorCounts[office] = 1
  const causal = appendCausal(galaxy, { id: `causal:${galaxy.seed}:successor:${successor.id}`, atRouteReckoning: galaxy.routeReckoning, kind: 'actor-status', parentIds: [parent.id], institutionId: office, actorId: successor.id, destinationId: currentDestination, knownToJomon: true, learnedThrough: 'institution-notice', summary: 'Acting Examiner Nadi Rell received a Port Office briefing on the unresolved Jomon file.', effects: { predecessor: predecessor?.id ?? 'unknown', status: 'active' } })
  remember(galaxy, successor.id, causal, 'briefing', 'A Port Office briefing transferred institutional context, not first-hand witness memory.', false)
  if (world.rival) world.rival = { ...world.rival, actorId: successor.id, status: 'replaced', successorActorId: successor.id }
  appendGeneralManifest(galaxy, { kind: 'institutionSuccession', detail: causal.summary, source: 'institution', institutionId: office, actorId: successor.id, causalEventId: causal.id, siteId: 'sector-00:site-03', payload: { predecessor: predecessor?.id ?? 'unknown', successor: successor.id } })
}

/** Advances institutions only after Route Reckoning has advanced canonical world time. */
export const advanceInstitutionWorld = (galaxy: GalaxyState): void => {
  reconcileInstitutionalManifest(galaxy)
  scheduleConditionOperations(galaxy)
  const due = galaxy.institutionWorld.operations.filter(operation => operation.status === 'scheduled' && operation.dueAtRouteReckoning <= galaxy.routeReckoning).sort((left, right) => left.dueAtRouteReckoning - right.dueAtRouteReckoning || left.id.localeCompare(right.id))
  for (const operation of due) resolveOperation(galaxy, operation)
  galaxy.institutionWorld.operations = galaxy.institutionWorld.operations.filter(operation => operation.status === 'scheduled').slice(0, INSTITUTION_OPERATION_LIMIT)
  galaxy.institutionWorld.lastProcessedManifestSequence = galaxy.generalManifest.nextSequence - 1
}

export type InstitutionDecision = 'comply' | 'refuse' | 'assist'
export interface InstitutionDecisionResult { changed: boolean; message: string }

/** Applies the one authored Nerida custody decision after the caller has charged any explicit time cost. */
export const decideNeridaInstitutionRequest = (galaxy: GalaxyState, action: InstitutionDecision): InstitutionDecisionResult => {
  const world = galaxy.institutionWorld
  const decisionId = 'decision:nerida:continuity-request'
  if (galaxy.routeBoard.currentDestinationId !== currentDestination) return { changed: false, message: 'The Port Continuity request can only be answered while Jomon is docked at Nerida.' }
  if (world.completedDecisionIds.includes(decisionId)) return { changed: false, message: 'Jomon has already filed its response to this continuity request.' }
  const causal = appendCausal(galaxy, { id: `causal:${galaxy.seed}:${decisionId}:${action}`, atRouteReckoning: galaxy.routeReckoning, kind: 'institution-decision', parentIds: world.rival ? [world.rival.emergenceCausalEventId] : [], institutionId: 'institution:nerida-port-continuity', actorId: world.rival?.actorId ?? 'actor:iren-vos', destinationId: currentDestination, courierId: galaxy.activeCourierId, knownToJomon: true, learnedThrough: 'direct-encounter', summary: action === 'comply' ? 'Jomon disclosed the requested Nerida custody record.' : action === 'refuse' ? 'Jomon refused the Port Continuity custody request.' : 'Jomon authorized Closure Eight assistance for the pump-bank work order.', effects: { decision: action } })
  world.completedDecisionIds = appendBounded(world.completedDecisionIds, decisionId, INSTITUTION_DECISION_LIMIT)
  const port = 'institution:nerida-port-continuity' as const
  const closure = 'institution:closure-eight' as const
  if (action === 'comply') {
    changeStanding(galaxy, port, causal, `standing:${causal.id}:port`, { trust: 2, scrutiny: -2 })
    remember(galaxy, 'actor:iren-vos', causal, 'compliance', causal.summary, true, galaxy.activeCourierId)
    if (world.rival) scheduleRivalAction(galaxy, causal, 'compliance')
  } else if (action === 'refuse') {
    changeStanding(galaxy, port, causal, `standing:${causal.id}:port`, { scrutiny: 2, grievance: 2, trust: -1 })
    remember(galaxy, 'actor:iren-vos', causal, 'refusal', causal.summary, true, galaxy.activeCourierId)
    emergeRival(galaxy, causal, 'refusal', galaxy.activeCourierId)
    schedule(world, { version: 1, id: `operation:port:access-audit:${causal.id}`, institutionId: port, actorId: 'actor:iren-vos', destinationId: currentDestination, kind: 'port-access-audit', initiatedAtRouteReckoning: galaxy.routeReckoning, dueAtRouteReckoning: galaxy.routeReckoning + 60, purpose: 'record the refused continuity request', status: 'scheduled', visibility: 'known', parentEventIds: [causal.id] })
  } else {
    changeStanding(galaxy, closure, causal, `standing:${causal.id}:closure`, { trust: 2, obligation: -1 })
    changeStanding(galaxy, port, causal, `standing:${causal.id}:port`, { grievance: 1, scrutiny: 1 })
    remember(galaxy, 'actor:iren-vos', causal, 'assistance', causal.summary, true, galaxy.activeCourierId)
    schedule(world, { version: 1, id: `operation:closure-eight:pump-repair:${causal.id}`, institutionId: closure, actorId: 'actor:sava-tesh', destinationId: currentDestination, kind: 'closure-pump-repair', initiatedAtRouteReckoning: galaxy.routeReckoning, dueAtRouteReckoning: galaxy.routeReckoning + 180, purpose: 'install the independent ceramic-vane repair order', status: 'scheduled', visibility: 'known', parentEventIds: [causal.id] })
  }
  appendGeneralManifest(galaxy, { kind: 'institutionDecision', detail: causal.summary, source: 'institution', institutionId: action === 'assist' ? closure : port, actorId: 'actor:iren-vos', causalEventId: causal.id, courierId: galaxy.activeCourierId, siteId: 'sector-00:site-03', payload: { decision: action, destinationId: currentDestination, ...(action === 'assist' ? { costMarks: 60 } : {}) } })
  world.lastProcessedManifestSequence = galaxy.generalManifest.nextSequence - 1
  return { changed: true, message: action === 'comply' ? 'Compliance filed. The Port Office recorded a narrower review window.' : action === 'refuse' ? 'Refusal filed. The Port Office may answer with a custody audit.' : 'Closure Eight assistance filed. It consumed sixty Route Reckoning marks; a repair order is pending.' }
}

/** A courier handoff can be a known rival encounter without changing vessel-level standing. */
export const recordInstitutionCourierReplacement = (galaxy: GalaxyState, priorCourierId: string, replacementCourierId: string): void => {
  const rival = galaxy.institutionWorld.rival
  const actor = rival ? actorFor(galaxy.institutionWorld, rival.actorId) : undefined
  if (!rival || !actor || !actor.memories.some(memory => memory.kind === 'courier-loss' && memory.courierId === priorCourierId)) return
  const causal = appendCausal(galaxy, { id: `causal:${galaxy.seed}:courier-replacement:${priorCourierId}:${replacementCourierId}:${galaxy.routeReckoning}`, atRouteReckoning: galaxy.routeReckoning, kind: 'courier-continuity', parentIds: actor.memories.filter(memory => memory.kind === 'courier-loss' && memory.courierId === priorCourierId).map(memory => memory.causalEventId), institutionId: rival.institutionId, actorId: actor.id, destinationId: currentDestination, courierId: replacementCourierId, knownToJomon: true, learnedThrough: 'direct-encounter', summary: `${actor.name} retained the Jomon file while a replacement courier took the watch.`, effects: { priorCourierId, replacementCourierId, vesselContinuity: true } })
  remember(galaxy, actor.id, causal, 'courier-replacement', causal.summary, true, replacementCourierId)
  appendGeneralManifest(galaxy, { kind: 'rivalEncounter', detail: causal.summary, source: 'institution', institutionId: rival.institutionId, actorId: actor.id, causalEventId: causal.id, courierId: replacementCourierId, siteId: 'sector-00:site-03', payload: { rivalId: rival.id, priorCourierId, replacementCourierId } })
  galaxy.institutionWorld.lastProcessedManifestSequence = galaxy.generalManifest.nextSequence - 1
}

const isRecord = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const isInteger = (value: unknown): value is number => typeof value === 'number' && Number.isInteger(value)

/** Normalizes v5 state without simulating historical activity. Invalid nested data receives a deterministic v5 baseline. */
export const normalizeInstitutionWorld = (value: unknown, seed: number, currentRouteReckoning: number, lastProcessedManifestSequence: number, manifestCausalEventIds: readonly string[] = []): InstitutionWorldState => {
  const fallback = createInstitutionWorld(seed, currentRouteReckoning, lastProcessedManifestSequence)
  if (!isRecord(value) || value.version !== 1 || value.contentRevision !== 1 || !isRecord(value.states) || !Array.isArray(value.actors) || !Array.isArray(value.operations) || !Array.isArray(value.causalEvents) || !Array.isArray(value.reports)) return fallback
  try {
    const candidate = structuredClone(value) as unknown as InstitutionWorldState
    for (const id of institutionIds) {
      const state = candidate.states[id]
      if (!state || state.version !== 1 || state.id !== id || !isInteger(state.capacity) || !isRecord(state.standing)) return fallback
      const standing = state.standing
      if (![standing.trust, standing.scrutiny, standing.obligation, standing.grievance, standing.influence].every(isInteger)) return fallback
      candidate.states[id] = { version: 1, id, capacity: clamp(state.capacity, 0, 6), currentConcern: typeof state.currentConcern === 'string' ? state.currentConcern : fallback.states[id].currentConcern, standing: { trust: clamp(standing.trust), scrutiny: clamp(standing.scrutiny), obligation: clamp(standing.obligation), grievance: clamp(standing.grievance), influence: clamp(standing.influence, 0, 6) }, relations: isRecord(state.relations) ? Object.fromEntries(institutionIds.filter(other => other !== id && isInteger(state.relations[other])).map(other => [other, clamp(state.relations[other] as number)])) : {} }
    }
    candidate.actors = candidate.actors.filter(actor => actor && actor.version === 1 && typeof actor.id === 'string' && typeof actor.name === 'string' && institutionIds.includes(actor.affiliationId) && Array.isArray(actor.methods) && Array.isArray(actor.memories)).slice(0, INSTITUTION_ACTOR_LIMIT).map(actor => ({ ...actor, methods: actor.methods.filter((method): method is string => typeof method === 'string').slice(0, 4), memories: actor.memories.filter(memory => memory && memory.version === 1 && typeof memory.id === 'string' && typeof memory.causalEventId === 'string' && typeof memory.summary === 'string').slice(-INSTITUTION_ACTOR_MEMORY_LIMIT).map(memory => ({ ...memory })), operationalHistory: Array.isArray(actor.operationalHistory) ? actor.operationalHistory.filter((entry): entry is string => typeof entry === 'string').slice(-INSTITUTION_RESOLVED_OPERATION_LIMIT) : [] }))
    if (!candidate.actors.some(actor => actor.id === 'actor:iren-vos')) return fallback
    candidate.operations = candidate.operations.filter(operation => operation && operation.version === 1 && typeof operation.id === 'string' && institutionIds.includes(operation.institutionId) && operation.destinationId === currentDestination && typeof operation.kind === 'string' && isInteger(operation.initiatedAtRouteReckoning) && isInteger(operation.dueAtRouteReckoning) && operation.status === 'scheduled' && Array.isArray(operation.parentEventIds)).slice(0, INSTITUTION_OPERATION_LIMIT).sort((left, right) => left.dueAtRouteReckoning - right.dueAtRouteReckoning || left.id.localeCompare(right.id)).map(operation => ({ ...operation, parentEventIds: unique(operation.parentEventIds.filter((parent): parent is string => typeof parent === 'string')) }))
    candidate.resolvedOperationIds = Array.isArray(candidate.resolvedOperationIds) ? unique(candidate.resolvedOperationIds.filter((id): id is string => typeof id === 'string')).slice(-INSTITUTION_RESOLVED_OPERATION_LIMIT) : []
    candidate.relationshipEffectIds = Array.isArray(candidate.relationshipEffectIds) ? unique(candidate.relationshipEffectIds.filter((id): id is string => typeof id === 'string')).slice(-INSTITUTION_RELATIONSHIP_EFFECT_LIMIT) : []
    const validCausalEvents = candidate.causalEvents
      .filter(event => event && event.version === 1 && typeof event.id === 'string' && isInteger(event.sequence) && isInteger(event.atRouteReckoning) && Array.isArray(event.parentIds) && typeof event.summary === 'string' && isRecord(event.effects))
      .sort((left, right) => left.sequence - right.sequence)
    const normalizedReferences = new Set<string>([
      ...manifestCausalEventIds,
      ...candidate.actors.flatMap(actor => actor.memories.map(memory => memory.causalEventId)),
      ...candidate.operations.flatMap(operation => operation.parentEventIds),
      ...candidate.reports.flatMap(report => report.causalEventId ? [report.causalEventId] : []),
      ...(candidate.rival ? [candidate.rival.emergenceCausalEventId] : [])
    ])
    for (const effectId of candidate.relationshipEffectIds) {
      for (const event of validCausalEvents) if (effectId.includes(event.id)) normalizedReferences.add(event.id)
    }
    const recentCausalIds = new Set(validCausalEvents.slice(-INSTITUTION_CAUSAL_EVENT_LIMIT).map(event => event.id))
    const retainedCausalIds = new Set(validCausalEvents.filter(event => recentCausalIds.has(event.id) || normalizedReferences.has(event.id)).map(event => event.id))
    candidate.causalEvents = validCausalEvents.filter(event => retainedCausalIds.has(event.id)).map(event => ({
      ...event,
      parentIds: unique(event.parentIds.filter((id): id is string => typeof id === 'string'))
        .filter(id => retainedCausalIds.has(id) && validCausalEvents.some(parent => parent.id === id && parent.sequence < event.sequence)),
      effects: { ...event.effects }
    }))
    candidate.reports = candidate.reports.filter(report => report && report.version === 1 && typeof report.id === 'string' && institutionIds.includes(report.institutionId) && report.destinationId === currentDestination && typeof report.causalEventId === 'string' && isInteger(report.reportAtRouteReckoning) && typeof report.summary === 'string').slice(-INSTITUTION_REPORT_LIMIT).map(report => ({ ...report }))
    candidate.knownRouteModifiers = Array.isArray(candidate.knownRouteModifiers) ? candidate.knownRouteModifiers.filter(modifier => modifier && modifier.version === 1 && typeof modifier.id === 'string' && modifier.destinationId === currentDestination && typeof modifier.operationId === 'string' && typeof modifier.warning === 'string' && isInteger(modifier.durationMarks) && isInteger(modifier.knownAtRouteReckoning)).slice(-INSTITUTION_ROUTE_MODIFIER_LIMIT).map(modifier => ({ ...modifier })) : []
    candidate.completedDecisionIds = Array.isArray(candidate.completedDecisionIds) ? unique(candidate.completedDecisionIds.filter((id): id is string => typeof id === 'string')).slice(-INSTITUTION_DECISION_LIMIT) : []
    candidate.lastProcessedManifestSequence = isInteger(candidate.lastProcessedManifestSequence) ? Math.min(lastProcessedManifestSequence, Math.max(-1, candidate.lastProcessedManifestSequence)) : lastProcessedManifestSequence
    candidate.nextCausalSequence = Math.max(isInteger(candidate.nextCausalSequence) ? candidate.nextCausalSequence : 0, ...candidate.causalEvents.map(event => event.sequence + 1), 0)
    const successorCount = isRecord(candidate.successorCounts) ? candidate.successorCounts['institution:nerida-port-continuity'] : undefined
    candidate.successorCounts = isInteger(successorCount) && successorCount > 0 ? { 'institution:nerida-port-continuity': 1 } : {}
    if (candidate.rival && (!candidate.actors.some(actor => actor.id === candidate.rival!.actorId) || !institutionIds.includes(candidate.rival.institutionId))) delete candidate.rival
    compactCausalEvents(candidate, manifestCausalEventIds)
    return cloneInstitutionWorld(candidate)
  } catch { return fallback }
}
