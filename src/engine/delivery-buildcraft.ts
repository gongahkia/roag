import { ITEM } from '../content'
import { rngFor } from '../rng'
import type { CourierInjury, CourierModification, CourierModificationId, DeliveryEquipmentOffer, DeliveryItemDefinition, DeliveryOfferSource, DeliveryPressureTier, DeliveryRunContext, DeliveryRunResolution, DeliveryRunState, GalaxyState, Hero, ItemId, RunState } from '../types'
import { appendGeneralManifest } from './manifest'

export const DELIVERY_PRESSURE_CAP = 1920
export const DELIVERY_EQUIPMENT_LIMIT = 12
/** Each offer exposes three distinct choices; a delivery retains at most six resolved offers. */
export const DELIVERY_OFFER_LIMIT = 6
export const DELIVERY_INJURY_LIMIT = 3
export const DELIVERY_MODIFICATION_LIMIT = 2
export const DELIVERY_TECHNIQUE_LIMIT = 3

export const DELIVERY_PRESSURE_THRESHOLDS: ReadonlyArray<{ id: string; mark: number; tier: DeliveryPressureTier; label: string }> = [
  { id: 'pressure:compression', mark: 360, tier: 'compression', label: 'compression' },
  { id: 'pressure:cavitation', mark: 720, tier: 'cavitation', label: 'cavitation' },
  { id: 'pressure:cascade', mark: 1200, tier: 'cascade', label: 'cascade' }
]

export interface DeliveryModifierEvaluation {
  values: { hazardReduction: number; hazardChargeCap: number; custodyBuffer: number; repositionResponses: number; healingPenalty: number; groundingBonus: number; orphanRisk: number }
  applied: string[]
}

const deliveryDefinition = (itemId: ItemId): DeliveryItemDefinition | undefined => ITEM[itemId]?.delivery
export const deliveryItemIds = (): ItemId[] => Object.values(ITEM).filter(item => item.delivery).map(item => item.id).sort()
export const deliveryItemDescription = (itemId: ItemId): string | undefined => deliveryDefinition(itemId)?.description
export const deliveryStackLimit = (itemId: ItemId): number => deliveryDefinition(itemId)?.stackLimit ?? 0
export const deliveryItemStackCount = (run: DeliveryRunState | undefined, itemId: ItemId): number => run?.equipment.find(entry => entry.itemId === itemId)?.count ?? 0
export const deliveryPressureLabel = (tier: DeliveryPressureTier): string => tier === 'working-load' ? 'working load' : tier
export const deliveryNextThreshold = (run: DeliveryRunState | undefined): number | undefined => run ? DELIVERY_PRESSURE_THRESHOLDS.find(threshold => !run.crossedThresholdIds.includes(threshold.id))?.mark : undefined

const canonicalTier = (elapsedMarks: number): DeliveryPressureTier => {
  if (elapsedMarks >= 1200) return 'cascade'
  if (elapsedMarks >= 720) return 'cavitation'
  if (elapsedMarks >= 360) return 'compression'
  return 'working-load'
}

const currentRun = (galaxy: GalaxyState): DeliveryRunState | undefined => {
  const run = galaxy.deliveryRun
  if (!run || run.resolution) return undefined
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === run.contractId)
  return contract?.status === 'accepted' ? run : undefined
}

const addRunManifest = (galaxy: GalaxyState, run: DeliveryRunState, eventKey: string, input: Omit<Parameters<typeof appendGeneralManifest>[1], 'source'>): void => {
  if (run.manifestEventIds.includes(eventKey)) return
  appendGeneralManifest(galaxy, { ...input, source: 'delivery', deliveryRunId: run.id, courierId: input.courierId ?? run.courierId })
  run.manifestEventIds.push(eventKey)
  if (run.manifestEventIds.length > 24) run.manifestEventIds.splice(0, run.manifestEventIds.length - 24)
}

const emptyRun = (galaxy: GalaxyState, contractId: string, courierId: string): DeliveryRunState => {
  const seed = rngFor(galaxy.seed, 'galaxy', 'delivery-run', contractId).next()
  return {
    version: 1,
    id: `delivery-run:${galaxy.seed}:${contractId}`,
    contractId,
    courierId,
    acceptedAtRouteReckoning: galaxy.routeReckoning,
    runSeed: seed,
    elapsedMarks: 0,
    pressureTier: 'working-load',
    crossedThresholdIds: [],
    equipment: [],
    offers: [],
    resolvedOfferIds: [],
    activeEquipmentCooldowns: {},
    expeditionHistory: [],
    eliteHistory: [],
    manifestEventIds: []
  }
}

export const createDeliveryRun = (galaxy: GalaxyState, contractId: string, courierId: string): DeliveryRunState | undefined => {
  if (galaxy.deliveryRun && galaxy.deliveryRun.contractId === contractId) return galaxy.deliveryRun
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  if (!contract || contract.status !== 'accepted') return undefined
  const run = emptyRun(galaxy, contractId, courierId)
  galaxy.deliveryRun = run
  createDeliveryOffer(galaxy, 'requisition')
  return run
}

export const deliveryRunContext = (run: DeliveryRunState | undefined, atRouteReckoning = run ? run.acceptedAtRouteReckoning + run.elapsedMarks : 0): DeliveryRunContext | undefined => run && !run.resolution ? {
  version: 1,
  runId: run.id,
  pressureTier: run.pressureTier,
  elapsedMarks: run.elapsedMarks,
  atRouteReckoning,
  ...(deliveryNextThreshold(run) === undefined ? {} : { nextThresholdMark: deliveryNextThreshold(run) })
} : undefined

/** Mutates a cloned GalaxyState only at the canonical Route Reckoning boundary. */
export const advanceDeliveryPressure = (galaxy: GalaxyState): boolean => {
  const run = currentRun(galaxy)
  if (!run) return false
  const elapsedMarks = Math.min(DELIVERY_PRESSURE_CAP, Math.max(0, galaxy.routeReckoning - run.acceptedAtRouteReckoning))
  const priorTier = run.pressureTier
  run.elapsedMarks = elapsedMarks
  run.pressureTier = canonicalTier(elapsedMarks)
  for (const threshold of DELIVERY_PRESSURE_THRESHOLDS) {
    if (elapsedMarks < threshold.mark || run.crossedThresholdIds.includes(threshold.id)) continue
    run.crossedThresholdIds.push(threshold.id)
    addRunManifest(galaxy, run, threshold.id, {
      kind: 'deliveryPressureEscalated',
      detail: `Delivery pressure entered ${threshold.label} at ${elapsedMarks} Route Reckoning marks.`,
      contractId: run.contractId,
      payload: { tier: threshold.tier, elapsedMarks }
    })
  }
  return priorTier !== run.pressureTier
}

const offerChoices = (run: DeliveryRunState, source: DeliveryOfferSource, offerId: string): ItemId[] => {
  const candidateIds = deliveryItemIds().filter(itemId => {
    const definition = deliveryDefinition(itemId)
    const count = deliveryItemStackCount(run, itemId)
    return definition?.sourcePool === source && count < (definition?.stackLimit ?? 0) && !(definition?.incompatibilities ?? []).some(other => deliveryItemStackCount(run, other) > 0)
  })
  const rng = rngFor(run.runSeed, 'galaxy', 'delivery-offer', offerId)
  const weighted = candidateIds.flatMap(itemId => Array.from({ length: Math.max(1, deliveryDefinition(itemId)?.rarityWeight ?? 1) }, () => itemId))
  const choices: ItemId[] = []
  while (weighted.length && choices.length < 3) {
    const candidate = weighted.splice(rng.int(0, weighted.length - 1), 1)[0]!
    if (!choices.includes(candidate)) choices.push(candidate)
  }
  return choices.sort()
}

export const createDeliveryOffer = (galaxy: GalaxyState, source: DeliveryOfferSource): DeliveryEquipmentOffer | undefined => {
  const run = currentRun(galaxy)
  if (!run) return undefined
  const pending = run.offers.find(offer => offer.state === 'pending')
  if (pending) return pending
  if (run.offers.length >= DELIVERY_OFFER_LIMIT) return undefined
  const id = `delivery-offer:${run.id}:${source}:${run.offers.length}`
  const choices = offerChoices(run, source, id)
  if (!choices.length) return undefined
  const offer: DeliveryEquipmentOffer = { version: 1, id, source, createdAtRouteReckoning: galaxy.routeReckoning, choices, state: 'pending' }
  run.offers.push(offer)
  return offer
}

const courierForRun = (galaxy: GalaxyState, run: DeliveryRunState): Hero | undefined => galaxy.couriers.find(candidate => candidate.id === run.courierId)?.hero
const decreaseInventory = (hero: Hero, itemId: ItemId, count: number): boolean => {
  let remaining = count
  const kept = hero.inventory.filter(item => {
    if (item !== itemId || remaining === 0) return true
    remaining--
    return false
  })
  if (remaining) return false
  hero.inventory = kept
  return true
}

const addStack = (run: DeliveryRunState, itemId: ItemId): void => {
  const existing = run.equipment.find(entry => entry.itemId === itemId)
  if (existing) existing.count++
  else run.equipment.push({ itemId, count: 1 })
  run.equipment.sort((left, right) => left.itemId.localeCompare(right.itemId))
}

export const selectDeliveryOffer = (galaxy: GalaxyState, offerId: string, itemId: ItemId | undefined): { changed: boolean; message: string } => {
  const run = currentRun(galaxy)
  const offer = run?.offers.find(candidate => candidate.id === offerId)
  const hero = run ? courierForRun(galaxy, run) : undefined
  if (!run || !offer || offer.state !== 'pending' || !hero) return { changed: false, message: 'That delivery requisition is no longer available.' }
  if (itemId === undefined) {
    offer.state = 'declined'
    offer.resolvedAtRouteReckoning = galaxy.routeReckoning
    run.resolvedOfferIds.push(offer.id)
    return { changed: true, message: 'Requisition declined. No field equipment was added.' }
  }
  if (!offer.choices.includes(itemId) || deliveryItemStackCount(run, itemId) >= deliveryStackLimit(itemId) || run.equipment.reduce((total, entry) => total + entry.count, 0) >= DELIVERY_EQUIPMENT_LIMIT) return { changed: false, message: 'That field-equipment selection is not legal for this delivery.' }
  hero.inventory.push(itemId)
  addStack(run, itemId)
  offer.state = 'selected'
  offer.selectedItemId = itemId
  offer.resolvedAtRouteReckoning = galaxy.routeReckoning
  run.resolvedOfferIds.push(offer.id)
  addRunManifest(galaxy, run, `equipment:${offer.id}`, { kind: 'deliveryEquipmentAcquired', detail: `${ITEM[itemId].name} requisitioned for ${run.id}.`, contractId: run.contractId, itemId, payload: { offerId: offer.id, source: offer.source } })
  return { changed: true, message: `${ITEM[itemId].name} added to this delivery load.` }
}

/**
 * Removes only the recorded run-bound stack count from every live representation
 * of the assigned courier before marking the run closed.
 */
export const archiveDeliveryRun = (galaxy: GalaxyState, resolution: DeliveryRunResolution, callerHero?: Hero): boolean => {
  const run = galaxy.deliveryRun
  if (!run || run.resolution) return false
  const hero = courierForRun(galaxy, run)
  if (hero) for (const stack of run.equipment) decreaseInventory(hero, stack.itemId, stack.count)
  if (callerHero && callerHero !== hero) for (const stack of run.equipment) decreaseInventory(callerHero, stack.itemId, stack.count)
  run.resolution = resolution
  run.resolvedAtRouteReckoning = galaxy.routeReckoning
  addRunManifest(galaxy, run, `archive:${resolution}`, { kind: 'deliveryEquipmentArchived', detail: `Run-bound field equipment archived after ${resolution.replace('-', ' ')}.`, contractId: run.contractId, payload: { resolution } })
  return true
}

/** Records one materialized expedition; callers provide a GalaxyState already cloned at their mutation boundary. */
export const beginDeliveryExpedition = (galaxy: GalaxyState, destinationId: 'destination:nerida'): DeliveryRunState | undefined => {
  const run = currentRun(galaxy)
  if (!run) return undefined
  const existing = run.expeditionHistory.find(expedition => expedition.destinationId === destinationId)
  if (existing) return run
  const id = `delivery-expedition:${run.id}:${destinationId}:${run.expeditionHistory.length}`
  run.expeditionHistory.push({ id, destinationId, createdAtRouteReckoning: galaxy.routeReckoning, outcome: 'active' })
  run.expeditionHistory = run.expeditionHistory.slice(-8)
  return run
}

export const synchronizeDeliveryRunEquipment = (galaxy: GalaxyState): void => {
  const run = galaxy.deliveryRun
  const hero = run ? courierForRun(galaxy, run) : undefined
  if (!run || !hero || run.resolution) return
  run.equipment = run.equipment.flatMap(stack => {
    const count = Math.min(stack.count, hero.inventory.filter(item => item === stack.itemId).length)
    return count > 0 ? [{ ...stack, count }] : []
  })
  run.activeEquipmentCooldowns = Object.fromEntries(Object.entries(hero.cooldowns ?? {}).filter(([id, value]) => id.startsWith('delivery:') && value > 0).sort(([left], [right]) => left.localeCompare(right)))
}

/** Commits materialized expedition outcomes to the one durable delivery run. */
export const synchronizeDeliveryExpedition = (galaxy: GalaxyState, state: RunState): boolean => {
  const run = deliveryRunForContext(galaxy, state)
  const expedition = state.floor.deliveryExpedition
  if (!run || !expedition || expedition.runId !== run.id) return false
  const manifestSequence = galaxy.generalManifest.nextSequence
  let changed = false
  const history = run.expeditionHistory.find(candidate => candidate.id === `delivery-expedition:${run.id}:destination:nerida:0`)
  const activeElite = state.floor.actors.find(actor => actor.deliveryElite?.encounterId === expedition.id && actor.health > 0)
  const eliteHistory = run.eliteHistory.find(candidate => candidate.id === `delivery-elite:${run.id}:nerida-intake:${state.floor.seed}`)
  if (!activeElite && eliteHistory?.outcome === 'active') {
    eliteHistory.outcome = 'defeated'
    addRunManifest(galaxy, run, `elite:${eliteHistory.id}:defeated`, {
      kind: 'deliveryEliteResolved',
      detail: `Nerida intake elite resolved: ${eliteHistory.traitIds.join(', ')}.`,
      contractId: run.contractId,
      encounterId: eliteHistory.id,
      payload: { outcome: 'defeated', traits: eliteHistory.traitIds.join(',') }
    })
    changed = true
  }
  if (expedition.status === 'completed' && history?.outcome !== 'completed') {
    history!.outcome = 'completed'
    createDeliveryOffer(galaxy, 'nerida-intake')
    changed = true
  }
  if (expedition.techniqueGranted) addRunManifest(galaxy, run, 'technique:intake-routing', {
    kind: 'courierTechniqueLearned',
    detail: `${state.hero.name} learned Intake Routing at the Nerida diagnostic.`,
    contractId: run.contractId,
    payload: { technique: 'intake-routing', expeditionId: expedition.id }
  })
  for (const injury of state.hero.deliveryInjuries ?? []) if (injury.runId === run.id) addRunManifest(galaxy, run, `injury:${injury.id}`, {
    kind: 'courierPersistentInjury',
    detail: `${state.hero.name} retained ${injury.id.replaceAll('-', ' ')} from delivery pressure.`,
    contractId: run.contractId,
    payload: { injury: injury.id }
  })
  return changed || galaxy.generalManifest.nextSequence !== manifestSequence
}

export const reconcileDeliveryRun = (galaxy: GalaxyState): boolean => {
  const run = galaxy.deliveryRun
  if (!run || run.resolution) return false
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === run.contractId)
  if (contract?.status === 'accepted') return false
  const resolution: DeliveryRunResolution = contract?.status === 'completed' ? 'completed'
    : contract?.status === 'expired' ? 'expired'
      : contract?.status === 'failed' ? 'failed'
        : 'failed'
  return archiveDeliveryRun(galaxy, resolution)
}

const hasModification = (hero: Hero, id: CourierModificationId): boolean => hero.deliveryModifications?.some(modification => modification.id === id) ?? false
export const courierModificationChoices = (hero: Hero): CourierModificationId[] => (['pressure-baffles', 'relay-marrow-conduit'] as const).filter(id => !hasModification(hero, id))
export const installCourierModification = (galaxy: GalaxyState, modificationId: CourierModificationId): { changed: boolean; message: string } => {
  const run = currentRun(galaxy)
  const hero = run ? courierForRun(galaxy, run) : undefined
  if (!run || !hero || run.expeditionHistory.every(expedition => expedition.destinationId !== 'destination:nerida')) return { changed: false, message: 'Nerida intake authorization is required before modification review.' }
  if (hero.deliveryModifications?.length && !hasModification(hero, modificationId)) return { changed: false, message: 'This courier cannot safely accept a second incompatible field modification.' }
  if (hasModification(hero, modificationId)) return { changed: false, message: 'That modification is already installed on this courier.' }
  if (!decreaseInventory(hero, 'tonic', 1)) return { changed: false, message: 'Installation requires one Vital Gel treatment.' }
  const installed: CourierModification = { id: modificationId, installedAtRouteReckoning: galaxy.routeReckoning, runId: run.id }
  hero.deliveryModifications = [...(hero.deliveryModifications ?? []), installed].slice(-DELIVERY_MODIFICATION_LIMIT)
  addRunManifest(galaxy, run, `modification:${modificationId}`, { kind: 'courierModificationInstalled', detail: `${modificationId === 'pressure-baffles' ? 'Subdermal pressure baffles' : 'Relay-marrow conduit'} installed for ${hero.name}.`, contractId: run.contractId, payload: { modificationId } })
  return { changed: true, message: 'Modification installed. Its cost and field risk are now permanent to this courier.' }
}

const consumeRunItem = (hero: Hero, run: DeliveryRunState, itemId: ItemId): boolean => {
  const stack = run.equipment.find(entry => entry.itemId === itemId && entry.count > 0)
  if (!stack || !decreaseInventory(hero, itemId, 1)) return false
  stack.count--
  if (stack.count === 0) run.equipment = run.equipment.filter(entry => entry !== stack)
  return true
}

export const applyPressureInjury = (hero: Hero, run: DeliveryRunState, atRouteReckoning: number): 'prevented' | 'applied' | 'existing' => {
  if (consumeRunItem(hero, run, 'tissueStitchPatch')) return 'prevented'
  if (hero.deliveryInjuries?.some(injury => injury.id === 'pressure-scarring')) return 'existing'
  const injury: CourierInjury = { id: 'pressure-scarring', acquiredAtRouteReckoning: atRouteReckoning, runId: run.id }
  hero.deliveryInjuries = [...(hero.deliveryInjuries ?? []), injury].slice(-DELIVERY_INJURY_LIMIT)
  return 'applied'
}

export const grantIntakeRouting = (hero: Hero): boolean => {
  if (hero.learnedDeliveryTechniques?.includes('intake-routing')) return false
  hero.learnedDeliveryTechniques = ([...(hero.learnedDeliveryTechniques ?? []), 'intake-routing'] as const).slice(-DELIVERY_TECHNIQUE_LIMIT) as Hero['learnedDeliveryTechniques']
  return true
}

export const deliveryModifiers = (hero: Hero, run: DeliveryRunState | undefined): DeliveryModifierEvaluation => {
  const values: DeliveryModifierEvaluation['values'] = { hazardReduction: 0, hazardChargeCap: 0, custodyBuffer: 0, repositionResponses: 0, healingPenalty: 0, groundingBonus: 0, orphanRisk: 0 }
  const applied: string[] = []
  if (hero.learnedDeliveryTechniques?.includes('intake-routing')) { values.repositionResponses += 1; applied.push('technique:intake-routing') }
  if (hero.deliveryInjuries?.some(injury => injury.id === 'pressure-scarring')) { values.healingPenalty += 1; applied.push('injury:pressure-scarring') }
  if (hasModification(hero, 'pressure-baffles')) { values.hazardReduction += 1; values.healingPenalty += 1; applied.push('modification:pressure-baffles') }
  if (hasModification(hero, 'relay-marrow-conduit')) { values.groundingBonus += 1; values.orphanRisk += 1; applied.push('modification:relay-marrow-conduit') }
  const equipment = run?.equipment ?? deliveryItemIds().flatMap(itemId => {
    const count = hero.inventory.filter(candidate => candidate === itemId).length
    return count ? [{ itemId, count: Math.min(count, deliveryStackLimit(itemId)) }] : []
  })
  for (const entry of [...equipment].sort((left, right) => left.itemId.localeCompare(right.itemId))) {
    if (entry.itemId === 'pressureWeaveLiner') { values.hazardReduction += entry.count === 1 ? 2 : entry.count === 2 ? 3 : 4; applied.push(`item:${entry.itemId}:${entry.count}`) }
    if (entry.itemId === 'routeCurrentCapacitor') { values.hazardChargeCap += entry.count; applied.push(`item:${entry.itemId}:${entry.count}`) }
    if (entry.itemId === 'custodySealMesh') { values.custodyBuffer += entry.count; applied.push(`item:${entry.itemId}:${entry.count}`) }
    if (entry.itemId === 'vectorSkates') { values.repositionResponses += 1; applied.push(`item:${entry.itemId}`) }
    if (entry.itemId === 'groundingSpindle') { values.groundingBonus += 1; applied.push(`item:${entry.itemId}`) }
    if (entry.itemId === 'orphanPhaseSample') { values.groundingBonus += 1; values.orphanRisk += 1; applied.push(`item:${entry.itemId}`) }
  }
  values.hazardReduction = Math.min(5, values.hazardReduction)
  values.hazardChargeCap = Math.min(4, values.hazardChargeCap)
  values.custodyBuffer = Math.min(2, values.custodyBuffer)
  values.repositionResponses = Math.min(2, values.repositionResponses)
  return { values, applied }
}

export const deliveryRunForContext = (galaxy: GalaxyState, state: RunState): DeliveryRunState | undefined => state.deliveryContext?.runId === galaxy.deliveryRun?.id ? galaxy.deliveryRun : undefined

const record = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value)
const asStringList = (value: unknown): string[] => Array.isArray(value) ? [...new Set(value.filter((entry): entry is string => typeof entry === 'string'))] : []
const asInteger = (value: unknown, fallback = 0): number => typeof value === 'number' && Number.isInteger(value) && value >= 0 ? value : fallback

/** Normalizes M6 durable state without advancing time, rolling offers, or replaying events. */
export const normalizeDeliveryRun = (galaxy: GalaxyState, value: unknown): DeliveryRunState | undefined => {
  if (!record(value) || value.version !== 1 || typeof value.id !== 'string' || typeof value.contractId !== 'string' || typeof value.courierId !== 'string' || typeof value.runSeed !== 'number') return undefined
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === value.contractId)
  const courier = galaxy.couriers.find(candidate => candidate.id === value.courierId)
  if (!contract || !courier) return undefined
  const acceptedAtRouteReckoning = asInteger(value.acceptedAtRouteReckoning, contract.acceptedAtRouteReckoning ?? galaxy.routeReckoning)
  const closed = typeof value.resolution === 'string' && ['completed', 'failed', 'refused', 'abandoned', 'expired', 'courier-loss'].includes(value.resolution)
  const elapsedMarks = closed ? Math.min(DELIVERY_PRESSURE_CAP, asInteger(value.elapsedMarks)) : Math.min(DELIVERY_PRESSURE_CAP, Math.max(0, galaxy.routeReckoning - acceptedAtRouteReckoning))
  const equipment = Array.isArray(value.equipment) ? value.equipment.flatMap(entry => record(entry) && typeof entry.itemId === 'string' && deliveryDefinition(entry.itemId) && asInteger(entry.count) > 0 ? [{ itemId: entry.itemId, count: Math.min(deliveryStackLimit(entry.itemId), asInteger(entry.count)) }] : []) : []
  const offers = Array.isArray(value.offers) ? value.offers.flatMap(entry => {
    if (!record(entry) || entry.version !== 1 || typeof entry.id !== 'string' || !['requisition', 'transit-salvage', 'nerida-intake'].includes(String(entry.source)) || !['pending', 'selected', 'declined'].includes(String(entry.state))) return []
    const choices = asStringList(entry.choices).filter(itemId => deliveryDefinition(itemId))
    if (!choices.length) return []
    const selectedItemId = typeof entry.selectedItemId === 'string' && choices.includes(entry.selectedItemId) ? entry.selectedItemId : undefined
    const source = entry.source as DeliveryOfferSource
    const state = entry.state as DeliveryEquipmentOffer['state']
    return [{ version: 1 as const, id: entry.id, source, createdAtRouteReckoning: asInteger(entry.createdAtRouteReckoning, acceptedAtRouteReckoning), choices, state, ...(selectedItemId ? { selectedItemId } : {}), ...(asInteger(entry.resolvedAtRouteReckoning, -1) >= 0 ? { resolvedAtRouteReckoning: asInteger(entry.resolvedAtRouteReckoning) } : {}) }]
  }).slice(0, DELIVERY_OFFER_LIMIT) : []
  const pressureTier = canonicalTier(elapsedMarks)
  const crossedThresholdIds = asStringList(value.crossedThresholdIds).filter(id => DELIVERY_PRESSURE_THRESHOLDS.some(threshold => threshold.id === id && elapsedMarks >= threshold.mark))
  return {
    version: 1,
    id: value.id,
    contractId: value.contractId,
    courierId: value.courierId,
    acceptedAtRouteReckoning,
    runSeed: value.runSeed,
    elapsedMarks,
    pressureTier,
    crossedThresholdIds,
    equipment,
    offers,
    resolvedOfferIds: asStringList(value.resolvedOfferIds).filter(id => offers.some(offer => offer.id === id && offer.state !== 'pending')),
    activeEquipmentCooldowns: record(value.activeEquipmentCooldowns) ? Object.fromEntries(Object.entries(value.activeEquipmentCooldowns).flatMap(([id, turns]) => typeof turns === 'number' && Number.isInteger(turns) && turns >= 0 ? [[id, turns]] : [])) : {},
    expeditionHistory: Array.isArray(value.expeditionHistory) ? value.expeditionHistory.filter(record).flatMap(entry => typeof entry.id === 'string' && entry.destinationId === 'destination:nerida' && ['active', 'completed', 'escaped'].includes(String(entry.outcome)) ? [{ id: entry.id, destinationId: 'destination:nerida' as const, createdAtRouteReckoning: asInteger(entry.createdAtRouteReckoning, acceptedAtRouteReckoning), outcome: entry.outcome as 'active' | 'completed' | 'escaped', ...(entry.feedbackRecorded === true ? { feedbackRecorded: true } : {}) }] : []).slice(-8) : [],
    eliteHistory: Array.isArray(value.eliteHistory) ? value.eliteHistory.filter(record).flatMap(entry => typeof entry.id === 'string' && Array.isArray(entry.traitIds) && ['active', 'defeated', 'escaped'].includes(String(entry.outcome)) ? [{ id: entry.id, traitIds: asStringList(entry.traitIds).filter((id): id is DeliveryRunState['eliteHistory'][number]['traitIds'][number] => ['relay-bound', 'sweep-marshal', 'intake-tracker', 'custody-clamp'].includes(id)), outcome: entry.outcome as 'active' | 'defeated' | 'escaped', encounteredAtRouteReckoning: asInteger(entry.encounteredAtRouteReckoning, acceptedAtRouteReckoning) }] : []).slice(-8) : [],
    manifestEventIds: asStringList(value.manifestEventIds).slice(-24),
    ...(closed ? { resolution: value.resolution as DeliveryRunResolution, resolvedAtRouteReckoning: asInteger(value.resolvedAtRouteReckoning, galaxy.routeReckoning) } : {})
  }
}
