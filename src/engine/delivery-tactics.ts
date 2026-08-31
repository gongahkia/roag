import { rngFor } from '../rng'
import type { Actor, DeliveryEliteTraitId, DeliveryRunState, GalaxyState, Point, RunState, Telegraph } from '../types'
import { getTile, isPassable } from '../world'
import { actionCells } from './geometry'
import { cancelTelegraphs, announceTelegraph } from './telegraphs'
import { deliveryModifiers, deliveryRunContext, grantIntakeRouting } from './delivery-buildcraft'
import { distance, log } from './shared'

const directions: Point[] = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }]
const hazardActions = ['pressure-vent-sweep', 'relay-discharge', 'intake-shear'] as const
type DeliveryHazardAction = typeof hazardActions[number]

const pointKey = (point: Point): string => `${point.x}:${point.y}`
const passable = (state: RunState, point: Point): boolean => Boolean(getTile(state.floor, point.x, point.y) && isPassable(state.floor, point.x, point.y))
const pendingHazards = (state: RunState): Telegraph[] => (state.floor.telegraphs ?? []).filter(telegraph => telegraph.sourceKind === 'hazard' && (telegraph.state ?? 'pending') === 'pending')
const safeResponseExists = (state: RunState, cells: readonly Point[]): boolean => {
  const threatened = new Set(cells.map(pointKey))
  return directions.some(delta => {
    const next = { x: state.hero.x + delta.x, y: state.hero.y + delta.y }
    return passable(state, next) && !threatened.has(pointKey(next))
  })
}

const clampCells = (state: RunState, cells: readonly Point[]): Point[] => cells.filter(point => passable(state, point))
const hazardCells = (state: RunState, action: DeliveryHazardAction): Point[] => {
  if (action === 'pressure-vent-sweep') return clampCells(state, Array.from({ length: state.floor.width }, (_, x) => ({ x, y: state.hero.y })))
  if (action === 'relay-discharge') return clampCells(state, [{ x: state.hero.x, y: state.hero.y }, { x: state.hero.x + 1, y: state.hero.y }, { x: state.hero.x, y: state.hero.y + 1 }, { x: state.hero.x - 1, y: state.hero.y - 1 }])
  return clampCells(state, [{ x: state.hero.x, y: state.hero.y }, { x: state.hero.x + 1, y: state.hero.y - 1 }, { x: state.hero.x - 1, y: state.hero.y + 1 }])
}

const traitName = (trait: DeliveryEliteTraitId): string => trait === 'relay-bound' ? 'Relay-Bound' : trait === 'sweep-marshal' ? 'Sweep Marshal' : trait === 'intake-tracker' ? 'Intake Tracker' : 'Custody Clamp'
export const deliveryEliteLabel = (actor: Actor): string | undefined => actor.deliveryElite ? `${actor.deliveryElite.epithet} · ${actor.deliveryElite.traitIds.map(traitName).join(' / ')}` : undefined
const incompatibleEliteTraits: ReadonlyArray<readonly [DeliveryEliteTraitId, DeliveryEliteTraitId]> = [['relay-bound', 'intake-tracker']]
export const deliveryEliteTraitsCompatible = (traits: readonly DeliveryEliteTraitId[]): boolean => incompatibleEliteTraits.every(([left, right]) => !(traits.includes(left) && traits.includes(right)))

export const deliveryEliteTraitsFor = (run: DeliveryRunState, galaxy: GalaxyState): DeliveryEliteTraitId[] => {
  const candidates: DeliveryEliteTraitId[] = ['sweep-marshal', 'intake-tracker']
  if (run.pressureTier !== 'working-load') candidates.push('relay-bound')
  if (run.pressureTier === 'cavitation' || run.pressureTier === 'cascade') candidates.push('custody-clamp')
  if (galaxy.destinationWorld.partitions['destination:nerida']?.condition === 'cavitation-restriction') candidates.push('relay-bound')
  const rng = rngFor(run.runSeed, 'galaxy', 'nerida-elite', run.id, run.pressureTier)
  const ordered = [...new Set(candidates)].sort()
  const maximum = run.pressureTier === 'working-load' ? 1 : run.pressureTier === 'compression' ? 2 : 3
  const traits: DeliveryEliteTraitId[] = []
  while (ordered.length && traits.length < maximum) {
    const candidate = ordered.splice(rng.int(0, ordered.length - 1), 1)[0]!
    if (deliveryEliteTraitsCompatible([...traits, candidate])) traits.push(candidate)
  }
  return traits.sort()
}

const consolePoint = (state: RunState, run: DeliveryRunState): Point => {
  const candidates = state.floor.tiles.flatMap((tile, index) => {
    const point = { x: index % state.floor.width, y: Math.floor(index / state.floor.width) }
    return tile.kind === 'floor' && distance(point, state.floor.start) >= 5 && distance(point, state.floor.exit) >= 3 ? [point] : []
  }).sort((left, right) => pointKey(left).localeCompare(pointKey(right)))
  const rng = rngFor(run.runSeed, 'galaxy', 'nerida-console', state.floor.seed)
  return candidates[rng.int(0, Math.max(0, candidates.length - 1))] ?? { ...state.floor.start }
}

/** Materializes a single optional Nerida expedition. Repeated calls preserve the existing encounter exactly. */
export const materializeNeridaIntakeExpedition = (state: RunState, galaxy: GalaxyState, run: DeliveryRunState): boolean => {
  if (state.floor.deliveryExpedition?.runId === run.id) return false
  const console = consolePoint(state, run)
  const base = [...state.floor.actors].filter(actor => actor.hostile && actor.health > 0).sort((left, right) => distance(left, console) - distance(right, console) || left.id.localeCompare(right.id))[0]
  if (!base) return false
  const traits = deliveryEliteTraitsFor(run, galaxy)
  const eliteId = `delivery-elite:${run.id}:nerida-intake:${state.floor.seed}`
  base.id = eliteId
  base.name = `Intake Warden, ${traits.map(traitName).join(' ')}`
  base.role = 'guardian'
  base.status = [...new Set([...(base.status ?? []), 'delivery-elite'])]
  base.deliveryElite = { version: 1, id: eliteId, epithet: 'Intake Warden', traitIds: traits, rewardItemId: run.pressureTier === 'cascade' ? 'orphanPhaseSample' : 'pulseReverser', encounterId: `nerida-intake:${run.id}` }
  if (!run.eliteHistory.some(history => history.id === eliteId)) run.eliteHistory.push({ id: eliteId, traitIds: [...traits], outcome: 'active', encounteredAtRouteReckoning: galaxy.routeReckoning })
  run.eliteHistory = run.eliteHistory.slice(-8)
  state.floor.objective = { id: `objective:nerida-intake:${run.id}`, kind: 'stabilizeIntake', status: 'active', label: 'Stabilize the intake diagnostic after disabling the warden' }
  state.floor.guardianDefeated = false
  const partition = galaxy.destinationWorld.partitions['destination:nerida']
  const relayActivated = partition.condition === 'cavitation-restriction' || partition.condition === 'bypass-stabilizing' || galaxy.institutionWorld.knownRouteModifiers.some(modifier => modifier.destinationId === 'destination:nerida')
  state.floor.deliveryExpedition = { version: 1, id: `nerida-intake:${run.id}`, kind: 'nerida-intake', runId: run.id, objective: 'stabilize-intake', status: 'active', console, hazardIds: [], modificationAvailable: true, relayActivated }
  state.deliveryContext = deliveryRunContext(run, galaxy.routeReckoning)
  state.messages.unshift('Nerida intake-service file: stabilize the diagnostic, then return through the airlock.')
  return true
}

/** Announces one renderer-independent hazard only when a neighboring response tile exists. */
export const announceDeliveryHazard = (state: RunState): Telegraph | undefined => {
  const expedition = state.floor.deliveryExpedition
  if (!expedition || expedition.status !== 'active' || pendingHazards(state).length || state.turn < 2 || state.turn % 2) return undefined
  const prior = expedition.lastHeroPosition
  const dx = state.hero.x - (prior?.x ?? state.hero.x)
  const dy = state.hero.y - (prior?.y ?? state.hero.y)
  const heading = dx === 0 && dy === 0 ? undefined : `${Math.sign(dx)}:${Math.sign(dy)}`
  expedition.repeatedHeadingCount = heading && heading === expedition.repeatedHeading ? (expedition.repeatedHeadingCount ?? 0) + 1 : heading ? 1 : 0
  expedition.repeatedHeading = heading
  expedition.lastHeroPosition = { x: state.hero.x, y: state.hero.y }
  const tracker = state.floor.actors.some(actor => actor.deliveryElite?.traitIds.includes('intake-tracker') && actor.health > 0)
  const scheduled = tracker && (expedition.repeatedHeadingCount ?? 0) >= 2 ? 'intake-shear' : hazardActions[expedition.hazardIds.length % hazardActions.length]!
  const next = scheduled === 'relay-discharge' && !expedition.relayActivated ? 'intake-shear' : scheduled
  const cells = hazardCells(state, next)
  if (!cells.some(cell => cell.x === state.hero.x && cell.y === state.hero.y) || !safeResponseExists(state, cells)) return undefined
  const id = `hazard:${expedition.id}:${expedition.hazardIds.length}`
  const category = next === 'pressure-vent-sweep' ? 'pressureVent' : next === 'relay-discharge' ? 'relayDischarge' : 'intakeShear'
  const responses = next === 'relay-discharge' ? ['move', 'ground', 'interrupt'] as const : next === 'intake-shear' ? ['move', 'interrupt'] as const : ['move', 'block'] as const
  const telegraph = announceTelegraph(state, { id, sourceId: `hazard-source:${expedition.id}:${next}`, actionId: next, cells, danger: 'major', windup: 1, sourceKind: 'hazard', category, responses: [...responses] })
  expedition.hazardIds.push(id)
  if (expedition.hazardIds.length > 8) expedition.hazardIds.splice(0, expedition.hazardIds.length - 8)
  log(state, next === 'pressure-vent-sweep' ? 'Pressure valves cycle across the intake lane; step clear.' : next === 'relay-discharge' ? 'Relay capacitors begin a visible discharge; ground it or move clear.' : 'The intake shear begins its cycle; interrupt it or reposition.')
  return telegraph
}

export const announceEliteSweep = (state: RunState, actor: Actor): boolean => {
  if (!actor.deliveryElite?.traitIds.includes('sweep-marshal') || state.floor.telegraphs?.some(telegraph => telegraph.sourceId === actor.id && telegraph.actionId === 'elite-pressure-sweep')) return false
  const baseCells = actionCells('line', actor, actor.x <= state.hero.x ? 'e' : 'w', Math.max(2, Math.min(6, Math.abs(actor.x - state.hero.x))))
  const custodyProtected = deliveryModifiers(state.hero, undefined).values.custodyBuffer > 0
  const cells = actor.deliveryElite.traitIds.includes('custody-clamp') && !custodyProtected ? clampCells(state, [...baseCells, ...baseCells.map(cell => ({ x: cell.x, y: cell.y + 1 }))]) : baseCells
  if (!cells.length || !safeResponseExists(state, cells)) return false
  announceTelegraph(state, { id: `${actor.id}:elite-pressure-sweep`, sourceId: actor.id, actionId: 'elite-pressure-sweep', cells, danger: 'major', windup: 1, sourceKind: 'actor', category: 'pressureVent', responses: ['move', 'block'] })
  log(state, `${actor.name} aligns a pressure sweep; leave the marked lane.`)
  return true
}

export const deliveryEliteDamageCap = (state: RunState, actor: Actor): number | undefined => actor.deliveryElite?.traitIds.includes('relay-bound') && (state.floor.telegraphs ?? []).some(telegraph => telegraph.category === 'relayDischarge' && (telegraph.state ?? 'pending') === 'pending') ? 1 : undefined
/** @deprecated Use deliveryEliteDamageCap. */
export const deliveryEliteDamageFloor = (state: RunState, actor: Actor): number => deliveryEliteDamageCap(state, actor) ?? 0

export const resolveDeliveryHazard = (state: RunState, telegraph: Telegraph): { damage: number; source: string; risksInjury: boolean } | undefined => {
  if (!hazardActions.includes(telegraph.actionId as DeliveryHazardAction) && telegraph.actionId !== 'elite-pressure-sweep') return undefined
  const hit = telegraph.cells.some(cell => cell.x === state.hero.x && cell.y === state.hero.y)
  if (!hit) { log(state, `${telegraph.actionId.replaceAll('-', ' ')} clears harmlessly.`); return { damage: 0, source: telegraph.actionId, risksInjury: false } }
  const modifiers = deliveryModifiers(state.hero, undefined).values
  const base = telegraph.actionId === 'pressure-vent-sweep' || telegraph.actionId === 'elite-pressure-sweep' ? 5 : telegraph.actionId === 'relay-discharge' ? 4 : 6
  if (telegraph.category === 'relayDischarge') {
    const cap = 1 + modifiers.hazardChargeCap
    const stored = state.hero.cooldowns?.['delivery:relay-charge'] ?? 0
    const gained = 1 + modifiers.hazardChargeCap
    const overflow = Math.max(0, stored + gained - cap)
    ;(state.hero.cooldowns ??= {})['delivery:relay-charge'] = Math.min(cap, stored + gained)
    if (overflow && modifiers.orphanRisk) log(state, 'The Orphan Phase Sample releases a corrective pulse from the overfull conductor.')
  }
  return { damage: Math.max(1, base + modifiers.orphanRisk - modifiers.hazardReduction), source: telegraph.actionId.replaceAll('-', ' '), risksInjury: telegraph.actionId === 'intake-shear' }
}

export const applyDeliveryPersistentInjury = (state: RunState): 'prevented' | 'applied' | 'existing' => {
  const patch = state.hero.inventory.indexOf('tissueStitchPatch')
  if (patch >= 0) {
    state.hero.inventory.splice(patch, 1)
    log(state, 'A Tissue-Stitch Patch seals the pressure damage before it scars.')
    return 'prevented'
  }
  if (state.hero.deliveryInjuries?.some(injury => injury.id === 'pressure-scarring')) return 'existing'
  state.hero.deliveryInjuries = [...(state.hero.deliveryInjuries ?? []), { id: 'pressure-scarring' as const, acquiredAtRouteReckoning: state.deliveryContext?.atRouteReckoning ?? state.turn, runId: state.deliveryContext?.runId ?? 'unassigned-delivery' }].slice(-3)
  log(state, 'Pressure scarring persists: later recovery restores one less health.')
  return 'applied'
}

/** Uses the sole active M6 device, then falls back to a held grounding spindle. */
export const useDeliveryActiveEquipment = (state: RunState): boolean => {
  const pending = pendingHazards(state).sort((left, right) => left.resolveTurn - right.resolveTurn || left.id.localeCompare(right.id))
  if (!pending.length) return false
  const pulse = state.hero.inventory.includes('pulseReverser')
  const pulseCooldown = state.hero.cooldowns?.['delivery:pulseReverser'] ?? 0
  const interruptible = pending.find(telegraph => telegraph.category === 'relayDischarge' || telegraph.category === 'intakeShear')
  if (pulse && !pulseCooldown && interruptible) {
    cancelTelegraphs(state, telegraph => telegraph.id === interruptible.id, 'Pulse Reverser cycle')
    const storedCharge = state.hero.cooldowns?.['delivery:relay-charge'] ?? 0
    ;(state.hero.cooldowns ??= {})['delivery:pulseReverser'] = Math.max(2, 6 - storedCharge)
    delete state.hero.cooldowns!['delivery:relay-charge']
    log(state, `Pulse Reverser interrupts the declared machinery cycle (${Math.max(2, 6 - storedCharge)} turns to recharge).`)
    return true
  }
  const relay = pending.find(telegraph => telegraph.category === 'relayDischarge')
  if (state.hero.inventory.includes('groundingSpindle') && relay) {
    cancelTelegraphs(state, telegraph => telegraph.id === relay.id, 'Grounding Spindle discharge path')
    const modifiers = deliveryModifiers(state.hero, undefined).values
    const cap = 1 + modifiers.hazardChargeCap
    const charge = 1 + modifiers.groundingBonus
    const existing = state.hero.cooldowns?.['delivery:relay-charge'] ?? 0
    ;(state.hero.cooldowns ??= {})['delivery:relay-charge'] = Math.min(cap, existing + charge)
    if (existing + charge > cap && modifiers.orphanRisk) log(state, 'The Orphan Phase Sample vents an unstable surplus into the intake frame.')
    log(state, 'Grounding Spindle drains the relay discharge into your field equipment.')
    return true
  }
  return false
}

export const stabilizeNeridaIntake = (state: RunState): boolean => {
  const expedition = state.floor.deliveryExpedition
  if (!expedition || expedition.status !== 'active' || distance(state.hero, expedition.console) > 1) return false
  if (state.floor.actors.some(actor => actor.deliveryElite && actor.hostile && actor.health > 0)) { log(state, 'The intake warden still controls the diagnostic channel.'); return true }
  expedition.status = 'completed'
  expedition.modificationAvailable = true
  state.floor.objective.status = 'complete'
  if (grantIntakeRouting(state.hero)) expedition.techniqueGranted = true
  log(state, 'Intake diagnostic stabilized. The return airlock is clear; modification review is now available on Jomon.')
  return true
}
