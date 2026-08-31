import { biomeName } from '../content'
import { rngFor } from '../rng'
import type { Biome, CargoKind, GalaxyCourier, GalaxyCourierRoutine, GalaxyEvent, GalaxyFaction, GalaxyFactionId, GalaxyMarket, GalaxySite, GalaxySiteSnapshot, GalaxyState, Hero, RunState } from '../types'
import { appendGeneralManifest, normalizeGeneralManifest } from './manifest'
import { addKestrelSealedPackageOffer, applySealedPackageDeadlineTransitions } from './sealed-packages'
import { ROUTE_RECKONING_UNITS_PER_CYCLE, ROUTE_RECKONING_WORLD_TICK_UNITS, routeReckoningFromLegacyDay, routeWorldTickFor, sectorDayFromRouteReckoning } from './route-reckoning'

export const OUTER_SECTOR_COUNT = 20
export const SITES_PER_SECTOR = 10
/** @deprecated Legacy wall-clock constants retained only for callers migrating to Route Reckoning. */
export const GALAXY_HOUR_MS = 60 * 60 * 1000
/** @deprecated Legacy wall-clock constants retained only for callers migrating to Route Reckoning. */
export const GALAXY_TICK_MS = 6 * GALAXY_HOUR_MS
/** @deprecated Legacy wall-clock constants retained only for callers migrating to Route Reckoning. */
export const GALAXY_OFFLINE_CAP_MS = 7 * 24 * GALAXY_HOUR_MS

const BIOMES: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
const sectorNames = ['Helios Reach', 'Aster Drift', 'Cinder Verge', 'Nerida Veil', 'Orison Belt', 'Kestrel March', 'Vesper Array', 'Halcyon Expanse', 'Borealis Wake', 'Tethys Fold', 'Morrow Chain', 'Lumen Scar', 'Icarus Shelf', 'Sable Meridian', 'Pallas Run', 'Caldera Span', 'Crown of Mira', 'Rook Nebula', 'Morrowline', 'Carthage Deep', 'New Edo Fringe']
const crewTemplates: ReadonlyArray<Pick<GalaxyCourier, 'name' | 'role' | 'origin' | 'calling' | 'routine'>> = [
  { name: 'Ari Venn', role: 'security specialist', origin: 'mineborn', calling: 'trailguard', routine: 'maintain' },
  { name: 'Mika Sol', role: 'systems specialist', origin: 'mosswalker', calling: 'pathmaker', routine: 'scout' },
  { name: 'Cato Ren', role: 'xenoarchivist', origin: 'cavernSeeker', calling: 'spiritbearer', routine: 'research' },
  { name: 'Dara Ilyan', role: 'field medic', origin: 'tidebound', calling: 'trailguard', routine: 'recover' }
]
const factionNames: Record<GalaxyFactionId, string> = { voyager: 'Jomon', salvagers: 'Free Salvagers', relayGuild: 'Relay Guild', voidborn: 'Voidborn', settlers: 'New Edo Settlers' }
const factions = (): GalaxyFaction[] => [
  { id: 'voyager', name: factionNames.voyager, influence: 18, disposition: 40 },
  { id: 'salvagers', name: factionNames.salvagers, influence: 24, disposition: -8 },
  { id: 'relayGuild', name: factionNames.relayGuild, influence: 27, disposition: 12 },
  { id: 'voidborn', name: factionNames.voidborn, influence: 19, disposition: -28 },
  { id: 'settlers', name: factionNames.settlers, influence: 12, disposition: 28 }
]

const siteId = (sector: number, index: number) => `sector-${String(sector).padStart(2, '0')}:site-${String(index).padStart(2, '0')}`
const sectorId = (sector: number) => `sector-${String(sector).padStart(2, '0')}`
const integerCoordinate = (value: number): number => {
  const rounded = Math.round(value)
  return Object.is(rounded, -0) ? 0 : rounded
}
const copyEvent = (event: GalaxyEvent): GalaxyEvent => ({ ...event })
const copySite = (site: GalaxySite): GalaxySite => ({ ...site, links: [...site.links] })
const copySnapshot = (snapshot: GalaxySiteSnapshot): GalaxySiteSnapshot => ({ version: 1, savedAt: snapshot.savedAt, run: structuredClone(snapshot.run) })

const siteName = (biome: Biome, sector: string): string => `${biomeName[biome].replace(' Colony', '')} // ${sector}`
const initialFaction = (sector: number, index: number): GalaxyFactionId => (['salvagers', 'relayGuild', 'voidborn', 'settlers'] as const)[(sector * 7 + index * 3) % 4]!
const courierId = (index: number) => `voyager-crew-${index}`
const cargoKinds: readonly CargoKind[] = ['provisions', 'components', 'salvage', 'biosamples']
const marketFor = (rng: ReturnType<typeof rngFor>): GalaxyMarket => {
  const stock = {} as GalaxyMarket['stock']; const demand = {} as GalaxyMarket['demand']; const prices = {} as GalaxyMarket['prices']
  for (const kind of cargoKinds) { stock[kind] = 8 + rng.int(0, 18); demand[kind] = 6 + rng.int(0, 16); prices[kind] = 8 + rng.int(0, 12) }
  return { stock, demand, prices }
}
const crewHero = (primary: Hero, template: Pick<GalaxyCourier, 'name' | 'origin' | 'calling'>, index: number): Hero => {
  const hero = structuredClone(primary)
  hero.name = template.name
  hero.origin = template.origin
  hero.calling = template.calling
  hero.deathMode = 'ironTrail'
  hero.health = hero.maxHealth
  hero.focus = hero.maxFocus
  hero.gold = 0
  hero.inventory = index % 2 ? ['tonic', 'rock', 'ropeBundle'] : ['focusTonic', 'rock', 'bombPack']
  hero.equipment = { mainHand: index % 2 ? 'whip' : 'tideSpear' }
  hero.stats = { ...hero.stats, strength: Math.max(1, hero.stats.strength + (index === 0 ? 1 : 0)), agility: Math.max(1, hero.stats.agility + (index === 1 ? 1 : 0)), vitality: Math.max(1, hero.stats.vitality + (index === 3 ? 1 : 0)), intellect: Math.max(1, hero.stats.intellect + (index === 2 ? 1 : 0)) }
  return hero
}

export const createGalaxy = (seed: number, primary: Hero, legacyCreatedAt = 0): GalaxyState => {
  const sectors = [] as GalaxyState['sectors']
  const sites: Record<string, GalaxySite> = {}
  for (let sectorIndex = 0; sectorIndex <= OUTER_SECTOR_COUNT; sectorIndex++) {
    const id = sectorId(sectorIndex)
    const rng = rngFor(seed, 'galaxy', 'sector', sectorIndex)
    const angle = rng.int(0, 359) * Math.PI / 180
    const distance = sectorIndex === 0 ? 0 : 9 + Math.floor(Math.sqrt(sectorIndex) * 7) + rng.int(0, 5)
    const name = sectorNames[sectorIndex] ?? `Uncatalogued ${sectorIndex}`
    const ordered = sectorIndex === 0 ? [...BIOMES] : rng.shuffle([...BIOMES])
    const ids = ordered.map((biome, siteIndex) => {
      const currentId = siteId(sectorIndex, siteIndex)
      sites[currentId] = {
        id: currentId, sectorId: id, name: siteName(biome, name), biome,
        x: integerCoordinate(Math.cos(angle) * distance * 10) + (siteIndex % 5) * 3,
        y: integerCoordinate(Math.sin(angle) * distance * 7) + Math.floor(siteIndex / 5) * 3,
        links: [], discovered: sectorIndex === 0 && siteIndex === 0, completed: false,
        control: initialFaction(sectorIndex, siteIndex), integrity: 55 + rng.int(0, 35), ecology: 35 + rng.int(0, 40), construction: 0, supplies: 30 + rng.int(0, 45), salvage: 25 + rng.int(0, 55), market: marketFor(rng), lastChangedAt: 0
      }
      return currentId
    })
    for (let siteIndex = 0; siteIndex < ids.length; siteIndex++) {
      const current = sites[ids[siteIndex]!]!
      const connect = (other: string | undefined) => { if (other && !current.links.includes(other)) current.links.push(other) }
      connect(ids[(siteIndex + 1) % ids.length])
      connect(ids[(siteIndex + ids.length - 1) % ids.length])
      if (siteIndex % 2 === 0) connect(ids[(siteIndex + 3) % ids.length])
    }
    sectors.push({ id, name, x: integerCoordinate(Math.cos(angle) * distance), y: integerCoordinate(Math.sin(angle) * distance), discovered: sectorIndex === 0, siteIds: ids })
  }
  for (let sectorIndex = 0; sectorIndex < OUTER_SECTOR_COUNT; sectorIndex++) {
    const source = sites[siteId(sectorIndex, 8)]!
    const destination = siteId(sectorIndex + 1, 1)
    source.links.push(destination)
    sites[destination]!.links.push(source.id)
  }
  const activeId = siteId(0, 0)
  const primaryCourier: GalaxyCourier = { id: courierId(0), name: primary.name, role: 'Jomon specialist', origin: primary.origin, calling: primary.calling, routine: 'socialize', status: 'available', affinity: 12, siteId: activeId, personalItems: [], hero: structuredClone(primary) }
  const couriers = [primaryCourier, ...crewTemplates.map((template, index): GalaxyCourier => ({ id: courierId(index + 1), ...template, status: 'available', affinity: (index % 2 ? -4 : 7), ...(index === 1 ? { rivalId: courierId(3) } : {}), personalItems: [], hero: crewHero(primary, template, index) }))]
  return addKestrelSealedPackageOffer({ version: 2, seed, ...(legacyCreatedAt ? { createdAt: legacyCreatedAt } : {}), routeReckoning: 0, lastWorldTick: 0, sectorDay: 0, activeSiteId: activeId, activeCourierId: primaryCourier.id, sectors, sites, couriers, factions: factions(), events: [{ id: `event:${seed}:arrival`, at: 0, routeReckoning: 0, kind: 'discovery', siteId: activeId, headline: 'Jomon enters Helios Reach', detail: 'Jomon has arrived at an uncharted frontier. Only Kestrel is reachable until its landing routes are surveyed.' }], siteSnapshots: {}, sharedStash: [], cargo: [], contracts: [], sealedPackageContracts: [], sealedPackages: [], generalManifest: { version: 2, nextSequence: 0, entries: [] }, routeCaches: [] })
}

export const cloneGalaxy = (galaxy: GalaxyState): GalaxyState => ({ ...galaxy, sectors: galaxy.sectors.map(sector => ({ ...sector, siteIds: [...sector.siteIds] })), sites: Object.fromEntries(Object.entries(galaxy.sites).map(([id, site]) => [id, { ...copySite(site), market: structuredClone(site.market) }])), couriers: galaxy.couriers.map(courier => ({ ...courier, personalItems: [...courier.personalItems], hero: structuredClone(courier.hero) })), factions: galaxy.factions.map(faction => ({ ...faction })), events: galaxy.events.map(copyEvent), siteSnapshots: Object.fromEntries(Object.entries(galaxy.siteSnapshots).map(([id, snapshot]) => [id, copySnapshot(snapshot)])), sharedStash: [...galaxy.sharedStash], cargo: galaxy.cargo.map(cargo => ({ ...cargo })), contracts: galaxy.contracts.map(contract => ({ ...contract, cargo: { ...contract.cargo } })), sealedPackageContracts: galaxy.sealedPackageContracts.map(contract => ({ ...contract, terms: { ...contract.terms, prohibitedActions: [...contract.terms.prohibitedActions] } })), sealedPackages: galaxy.sealedPackages.map(packageRecord => ({ ...packageRecord, exterior: { ...packageRecord.exterior }, ...(packageRecord.revealedContents ? { revealedContents: { ...packageRecord.revealedContents } } : {}) })), generalManifest: { ...galaxy.generalManifest, entries: galaxy.generalManifest.entries.map(entry => ({ ...entry })) }, routeCaches: galaxy.routeCaches.map(cache => ({ ...cache, cargo: cache.cargo.map(cargo => ({ ...cargo })), packages: [...cache.packages] })) })

const appendEvent = (galaxy: GalaxyState, event: GalaxyEvent): void => {
  if (galaxy.events.some(candidate => candidate.id === event.id)) return
  galaxy.events.unshift(event)
  galaxy.events = galaxy.events.slice(0, 240)
}
const eventId = (galaxy: GalaxyState, tick: number, suffix: string) => `event:${galaxy.seed}:${tick}:${suffix}`
const clamp = (value: number, min = 0, max = 100): number => Math.max(min, Math.min(max, value))
const shiftSite = (galaxy: GalaxyState, site: GalaxySite, tick: number): void => {
  const rng = rngFor(galaxy.seed, 'galaxy', 'tick', tick, site.id)
  const change = rng.int(-12, 12)
  const kind = rng.int(0, 4)
  const before = { control: site.control, integrity: site.integrity, ecology: site.ecology, construction: site.construction, supplies: site.supplies }
  if (kind === 0) {
    site.integrity = clamp(site.integrity + change)
    appendEvent(galaxy, { id: eventId(galaxy, tick, `${site.id}:integrity`), at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'ecology', siteId: site.id, headline: `${site.name}: terrain shifted`, detail: change < 0 ? 'A regional disturbance damaged known routes.' : 'Local crews stabilized part of the terrain.' })
  } else if (kind === 1) {
    site.ecology = clamp(site.ecology + change)
    appendEvent(galaxy, { id: eventId(galaxy, tick, `${site.id}:ecology`), at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'ecology', siteId: site.id, headline: `${site.name}: ecological change`, detail: change < 0 ? 'A hostile bloom spread through the site.' : 'A dormant habitat recovered.' })
  } else if (kind === 2) {
    const control = (['salvagers', 'relayGuild', 'voidborn', 'settlers'] as const)[rng.int(0, 3)]!
    if (control !== site.control) {
      site.control = control
      appendEvent(galaxy, { id: eventId(galaxy, tick, `${site.id}:territory`), at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'territory', siteId: site.id, headline: `${site.name}: control changed`, detail: `${factionNames[control]} now holds the visible approaches.` })
    }
  } else {
    site.construction = clamp(site.construction + change)
    site.supplies = clamp(site.supplies + rng.int(-8, 12))
    site.salvage = clamp(site.salvage + rng.int(-10, 10))
    appendEvent(galaxy, { id: eventId(galaxy, tick, `${site.id}:construction`), at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'construction', siteId: site.id, headline: `${site.name}: construction changed`, detail: 'Autonomous crews altered the site while Jomon was elsewhere.' })
  }
  site.lastChangedAt = galaxy.routeReckoning
  for (const kind of cargoKinds) {
    const market = site.market
    market.stock[kind] = clamp(market.stock[kind] + rng.int(-3, 4))
    market.demand[kind] = clamp(market.demand[kind] + rng.int(-2, 3))
    market.prices[kind] = clamp(8 + Math.round((market.demand[kind] - market.stock[kind]) / 3) + (site.control === 'voidborn' ? 4 : 0), 3, 30)
  }
  const manifest = (kind: Parameters<typeof appendGeneralManifest>[1]['kind'], detail: string, payload: Record<string, number | string>) => appendGeneralManifest(galaxy, { kind, detail, source: 'site', siteId: site.id, payload })
  if (before.supplies >= 25 && site.supplies < 25) manifest('siteSupplyCrisis', `${site.name} entered a supply crisis.`, { before: before.supplies, after: site.supplies })
  else if (before.supplies < 25 && site.supplies >= 25) manifest('siteSupplyRecovery', `${site.name} recovered above its supply-crisis threshold.`, { before: before.supplies, after: site.supplies })
  if (before.integrity >= 40 && site.integrity < 40) manifest('siteIntegrityDegraded', `${site.name} approach integrity degraded.`, { before: before.integrity, after: site.integrity })
  else if (before.integrity < 40 && site.integrity >= 40) manifest('siteIntegrityRecovered', `${site.name} approach integrity recovered.`, { before: before.integrity, after: site.integrity })
  if ((before.ecology >= 35 && site.ecology < 35) || (before.ecology < 35 && site.ecology >= 35)) manifest('siteEcologyShift', `${site.name} ecological operating conditions shifted.`, { before: before.ecology, after: site.ecology })
  if (before.construction < 80 && site.construction >= 80) manifest('siteConstructionCompleted', `${site.name} construction reached operational completion.`, { before: before.construction, after: site.construction })
  else if (before.construction >= 25 && site.construction < 25) manifest('siteConstructionLost', `${site.name} lost operational construction capacity.`, { before: before.construction, after: site.construction })
  if (before.control !== site.control) manifest('siteControlChanged', `${site.name} is now controlled by ${factionNames[site.control]}.`, { before: before.control, after: site.control })
}
const evolveCouriers = (galaxy: GalaxyState, tick: number): void => {
  const rng = rngFor(galaxy.seed, 'galaxy', 'crew', tick)
  for (const courier of [...galaxy.couriers].sort((left, right) => left.id.localeCompare(right.id))) {
    if (courier.status === 'dead' || courier.status === 'retired') continue
    if (courier.id === galaxy.activeCourierId) continue
    courier.affinity = clamp(courier.affinity + rng.int(-3, 3), -100, 100)
    if (courier.routine === 'trade') {
      const sites = Object.values(galaxy.sites).sort((left, right) => left.id.localeCompare(right.id))
      const site = sites[rng.int(0, sites.length - 1)]
      if (site) {
        const kind = cargoKinds[rng.int(0, cargoKinds.length - 1)]!
        site.market.stock[kind] = clamp(site.market.stock[kind] + rng.int(1, 4))
        site.market.demand[kind] = clamp(site.market.demand[kind] - 1)
        appendEvent(galaxy, { id: eventId(galaxy, tick, `${courier.id}:trade`), at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'construction', siteId: site.id, courierId: courier.id, headline: `${courier.name} completed a trade survey`, detail: `${courier.name} improved ${kind} availability at ${site.name}; the Jomon hold was not altered.` })
      }
    }
    if (rng.chance(4)) {
      courier.status = 'injured'
      appendEvent(galaxy, { id: eventId(galaxy, tick, `${courier.id}:injured`), at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'loss', courierId: courier.id, headline: `${courier.name} was injured off-watch`, detail: `${courier.name}'s ${courier.routine} shift encountered a sector hazard.` })
      appendGeneralManifest(galaxy, { kind: 'courierStatusChanged', detail: `${courier.name} was injured while off-watch.`, source: 'courier', courierId: courier.id, payload: { status: 'injured', routine: courier.routine } })
    } else if (rng.chance(1)) {
      courier.status = 'dead'
      appendEvent(galaxy, { id: eventId(galaxy, tick, `${courier.id}:lost`), at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'loss', courierId: courier.id, headline: `${courier.name} is lost`, detail: `Their final autonomous ${courier.routine} shift is now part of the Jomon chronicle.` })
      appendGeneralManifest(galaxy, { kind: 'courierStatusChanged', detail: `${courier.name} is lost during an autonomous ${courier.routine} shift.`, source: 'courier', courierId: courier.id, payload: { status: 'dead', routine: courier.routine } })
    }
  }
}

const advanceGenericContractDeadlines = (galaxy: GalaxyState): void => {
  for (const contract of galaxy.contracts) {
    if ((contract.status !== 'open' && contract.status !== 'active') || contract.deadlineReckoning >= galaxy.routeReckoning) continue
    contract.status = 'failed'
    galaxy.cargo = galaxy.cargo.filter(cargo => cargo.contractId !== contract.id)
    appendEvent(galaxy, { id: `event:${galaxy.seed}:contract:${contract.id}:expired`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'trade', siteId: contract.destinationSiteId, headline: 'Contract window expired', detail: `${contract.cargo.units} units of ${contract.cargo.kind} are no longer accepted at ${galaxy.sites[contract.destinationSiteId]?.name ?? contract.destinationSiteId}.` })
    appendGeneralManifest(galaxy, { kind: 'contractExpired', detail: `Legacy cargo contract ${contract.id} expired.`, source: 'contract', contractId: contract.id, siteId: contract.destinationSiteId, payload: { cargoKind: contract.cargo.kind, units: contract.cargo.units } })
  }
}

/** Advances canonical state by integer Route Reckoning marks only. */
export const advanceGalaxyRouteReckoning = (source: GalaxyState, marks: number): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  const steps = Math.max(0, Math.floor(marks))
  for (let step = 0; step < steps; step++) {
    galaxy.routeReckoning++
    galaxy.sectorDay = sectorDayFromRouteReckoning(galaxy.routeReckoning)
    applySealedPackageDeadlineTransitions(galaxy)
    advanceGenericContractDeadlines(galaxy)
    if (galaxy.routeReckoning % ROUTE_RECKONING_WORLD_TICK_UNITS !== 0) continue
    const tick = routeWorldTickFor(galaxy.routeReckoning)
    if (tick <= galaxy.lastWorldTick) continue
    galaxy.lastWorldTick = tick
    const candidates = Object.values(galaxy.sites).sort((left, right) => left.id.localeCompare(right.id))
    const site = candidates[rngFor(galaxy.seed, 'galaxy', 'site', tick).int(0, candidates.length - 1)]
    if (site) shiftSite(galaxy, site, tick)
    evolveCouriers(galaxy, tick)
  }
  return galaxy
}

/** @deprecated No-op compatibility shim. Offline wall-clock reconciliation is forbidden in M2. */
export const reconcileGalaxy = (source: GalaxyState, _legacyNow?: number): GalaxyState => cloneGalaxy(source)

export const discoverLinkedSites = (source: GalaxyState, sourceId: string, _legacyNow?: number): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  const site = galaxy.sites[sourceId]
  if (!site) return galaxy
  site.completed = true
  site.lastChangedAt = galaxy.routeReckoning
  for (const id of site.links) {
    const destination = galaxy.sites[id]
    if (!destination || destination.discovered) continue
    destination.discovered = true
    const sector = galaxy.sectors.find(candidate => candidate.id === destination.sectorId)
    if (sector) sector.discovered = true
    appendEvent(galaxy, { id: `event:${galaxy.seed}:link:${sourceId}:${id}`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'discovery', siteId: id, headline: `Route to ${destination.name} surveyed`, detail: `A physical approach has been found from ${site.name}.` })
    if (!galaxy.contracts.some(contract => contract.sourceSiteId === sourceId && contract.destinationSiteId === id && contract.status === 'open')) {
      const kind = cargoKinds[rngFor(galaxy.seed, 'galaxy', 'contract', sourceId, id).int(0, cargoKinds.length - 1)]!
      galaxy.contracts.push({ id: `contract:${sourceId}:${id}`, sourceSiteId: sourceId, destinationSiteId: id, cargo: { kind, units: 2 }, fee: destination.market.prices[kind] * 3, deadlineDay: galaxy.sectorDay + 18, deadlineReckoning: galaxy.routeReckoning + 18 * ROUTE_RECKONING_UNITS_PER_CYCLE, factionId: destination.control, status: 'open', collateral: destination.market.prices[kind] })
    }
  }
  appendEvent(galaxy, { id: `event:${galaxy.seed}:complete:${sourceId}:${galaxy.routeReckoning}`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'discovery', siteId: sourceId, headline: `${site.name} survey archived`, detail: 'Jomon has preserved this landing as a persistent site.' })
  return galaxy
}

export const setActiveGalaxySite = (source: GalaxyState, id: string): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  if (galaxy.sites[id]?.discovered) galaxy.activeSiteId = id
  return galaxy
}
export const setCourierRoutine = (source: GalaxyState, courierId: string, routine: GalaxyCourierRoutine): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  const courier = galaxy.couriers.find(candidate => candidate.id === courierId)
  if (courier && courier.status !== 'dead' && courier.status !== 'retired') courier.routine = routine
  return galaxy
}
export const selectGalaxyCourier = (source: GalaxyState, courierId: string): { galaxy: GalaxyState; hero?: Hero } => {
  const galaxy = cloneGalaxy(source)
  const courier = galaxy.couriers.find(candidate => candidate.id === courierId)
  if (!courier || courier.status !== 'available') return { galaxy }
  galaxy.activeCourierId = courier.id
  appendEvent(galaxy, { id: `event:${galaxy.seed}:handoff:${courier.id}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'discovery', courierId: courier.id, headline: `${courier.name} takes the landing watch`, detail: `${courier.role} has been assigned as Jomon's active specialist.` })
  return { galaxy, hero: structuredClone(courier.hero) }
}
export const loseGalaxyCourier = (source: GalaxyState, courierId: string, detail: string, _legacyNow?: number): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  const courier = galaxy.couriers.find(candidate => candidate.id === courierId)
  if (!courier) return galaxy
  courier.status = 'dead'
  appendEvent(galaxy, { id: `event:${galaxy.seed}:loss:${courier.id}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'loss', courierId: courier.id, headline: `${courier.name} is lost`, detail })
  appendGeneralManifest(galaxy, { kind: 'courierStatusChanged', detail: `${courier.name} is lost.`, source: 'courier', courierId: courier.id, payload: { status: 'dead' } })
  return galaxy
}
export const saveGalaxySite = (source: GalaxyState, siteIdValue: string, run: RunState, _legacyNow?: number): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  if (!galaxy.sites[siteIdValue]) return galaxy
  galaxy.siteSnapshots[siteIdValue] = { version: 1, run: structuredClone(run), savedAt: galaxy.routeReckoning }
  return galaxy
}

const factionPressure: Record<GalaxyFactionId, { health: number; attack: number; defense: number; reward: number; label: string }> = {
  voyager: { health: 0, attack: 0, defense: 0, reward: 1.1, label: 'Voyager survey teams keep the approach supplied.' },
  salvagers: { health: 0, attack: 1, defense: 0, reward: 1.3, label: 'Salvager patrols contest valuable caches.' },
  relayGuild: { health: -1, attack: 0, defense: 0, reward: 1.2, label: 'Relay Guild infrastructure shortens routes but prices supplies.' },
  voidborn: { health: 2, attack: 1, defense: 1, reward: 1.45, label: 'Voidborn pressure turns every approach into a fight.' },
  settlers: { health: -1, attack: 0, defense: 0, reward: .9, label: 'Settler routes are safer, but most salvage is already spoken for.' }
}

export const applyGalaxySiteConditions = (run: RunState, site: GalaxySite): RunState => {
  const pressure = factionPressure[site.control]
  const integrityPressure = site.integrity < 40 ? 1 : 0
  const ecologyPressure = site.ecology < 40 ? 1 : 0
  const healthMultiplier = 1 + Math.max(0, pressure.health + integrityPressure) * .12
  const attackBonus = pressure.attack + ecologyPressure
  const defenseBonus = pressure.defense + (site.construction > 65 ? 1 : 0)
  for (const actor of run.floor.actors) {
    if (!actor.hostile) continue
    actor.maxHealth = Math.max(1, Math.round(actor.maxHealth * healthMultiplier))
    actor.health = Math.min(actor.maxHealth, Math.round(actor.health * healthMultiplier))
    actor.attack += attackBonus
    actor.defense += defenseBonus
  }
  run.floor.difficulty = { ...(run.floor.difficulty ?? { routePosition: 0, threat: 0, healthMultiplier: 1, attackBonus: 0, defenseBonus: 0, eliteChance: 0, guardianPattern: 0 }), healthMultiplier, attackBonus, defenseBonus, rewardMultiplier: pressure.reward }
  const terrain = run.floor.tiles.filter((tile, index) => tile.kind === 'floor' && index !== run.floor.start.y * run.floor.width + run.floor.start.x)
  if (site.ecology < 35) terrain.filter((_, index) => index % 37 === 0).forEach(tile => { tile.kind = 'gas' })
  if (site.integrity < 35) terrain.filter((_, index) => index % 41 === 0).forEach(tile => { tile.kind = 'rubble' })
  run.messages.unshift(`${site.name}: ${pressure.label}`)
  if (site.ecology < 35) run.messages.unshift('Ecology alert: invasive spores have altered the landing route.')
  if (site.integrity < 35) run.messages.unshift('Integrity alert: collapse debris has narrowed the landing route.')
  return run
}

export const recordGalaxyLanding = (source: GalaxyState, siteIdValue: string, run: RunState, _legacyNow?: number): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  const site = galaxy.sites[siteIdValue]
  if (!site) return galaxy
  const pressure = factionPressure[site.control]
  const kills = run.telemetry?.kills ?? 0
  const reward = Math.max(4, Math.round((6 + kills + Math.floor(site.salvage / 18)) * pressure.reward))
  run.hero.gold += reward
  site.salvage = clamp(site.salvage - Math.max(2, reward / 2))
  site.supplies = clamp(site.supplies + 4 + Math.min(10, kills))
  site.integrity = clamp(site.integrity + 3)
  site.ecology = clamp(site.ecology + (site.ecology < 45 ? 2 : 0))
  site.construction = clamp(site.construction + 2)
  site.lastChangedAt = galaxy.routeReckoning
  appendEvent(galaxy, { id: `event:${galaxy.seed}:landing:${siteIdValue}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'construction', siteId: siteIdValue, headline: `${site.name}: landing returned ${reward} credits`, detail: `Survey work increased supplies to ${site.supplies} and stabilized the approach.` })
  return galaxy
}
export const galaxyRouteLength = (galaxy: GalaxyState, fromSiteId: string, toSiteId: string): number => 1 + rngFor(galaxy.seed, 'galaxy', 'link', [fromSiteId, toSiteId].sort().join('::')).int(0, 4)
export const galaxyRouteSituation = (galaxy: GalaxyState, fromSiteId: string, toSiteId: string, chunk: number): 'quiet' | 'patrol' | 'hazard' | 'trader' | 'ecology' => {
  const outcomes = ['quiet', 'patrol', 'hazard', 'trader', 'ecology'] as const
  return outcomes[rngFor(galaxy.seed, 'galaxy', 'route-situation', [fromSiteId, toSiteId].sort().join('::'), chunk, routeWorldTickFor(galaxy.routeReckoning)).int(0, outcomes.length - 1)]!
}
export const recordGalaxyRouteSituations = (source: GalaxyState, linkId: string, situations: readonly ('quiet' | 'patrol' | 'hazard' | 'trader' | 'ecology')[]): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  situations.forEach((situation, chunk) => {
    if (situation === 'quiet') return
    appendGeneralManifest(galaxy, { kind: 'routeSituationActivated', detail: `${situation} conditions were reported on route ${linkId}, connector segment ${chunk + 1}.`, source: 'route', payload: { linkId, chunk, situation, status: 'reported' } })
  })
  return galaxy
}
export const resolveGalaxyRouteSituations = (source: GalaxyState, linkId: string, situations: readonly ('quiet' | 'patrol' | 'hazard' | 'trader' | 'ecology')[]): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  situations.forEach((situation, chunk) => {
    if (situation === 'quiet') return
    appendGeneralManifest(galaxy, { kind: 'routeSituationResolved', detail: `${situation} conditions concluded on route ${linkId}, connector segment ${chunk + 1}.`, source: 'route', payload: { linkId, chunk, situation, status: 'confirmed' } })
  })
  return galaxy
}
export const acceptGalaxyContract = (source: GalaxyState, fromSiteId: string, toSiteId: string, _legacyNow?: number): { galaxy: GalaxyState; message: string } => {
  const galaxy = cloneGalaxy(source)
  const contract = galaxy.contracts.find(candidate => candidate.sourceSiteId === fromSiteId && candidate.destinationSiteId === toSiteId && candidate.status === 'open')
  if (!contract) return { galaxy, message: 'No open contract is registered for this airlock.' }
  if (galaxy.cargo.reduce((total, cargo) => total + cargo.units, 0) + contract.cargo.units > 12) return { galaxy, message: 'Jomon cargo hold lacks capacity for this contract.' }
  const sourceSite = galaxy.sites[fromSiteId]
  if (!sourceSite || sourceSite.market.stock[contract.cargo.kind] < contract.cargo.units) return { galaxy, message: 'The source market cannot load that cargo today.' }
  sourceSite.market.stock[contract.cargo.kind] -= contract.cargo.units
  galaxy.cargo.push({ ...contract.cargo, contractId: contract.id })
  contract.status = 'active'
  appendEvent(galaxy, { id: `event:${galaxy.seed}:contract:${contract.id}:accepted`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'trade', siteId: fromSiteId, headline: 'Contract cargo loaded', detail: `${contract.cargo.units} units of ${contract.cargo.kind} are bound for ${galaxy.sites[toSiteId]?.name ?? toSiteId}.` })
  return { galaxy, message: `Loaded ${contract.cargo.units} units of ${contract.cargo.kind}.` }
}
export const deliverGalaxyContracts = (source: GalaxyState, siteId: string, hero: Hero, _legacyNow?: number): { galaxy: GalaxyState; message?: string } => {
  const galaxy = cloneGalaxy(source)
  const deliverable = galaxy.contracts.filter(contract => contract.destinationSiteId === siteId && contract.status === 'active' && contract.deadlineReckoning >= galaxy.routeReckoning)
  if (!deliverable.length) return { galaxy }
  let fee = 0
  for (const contract of deliverable) {
    contract.status = 'completed'
    fee += contract.fee
    galaxy.cargo = galaxy.cargo.filter(cargo => cargo.contractId !== contract.id)
    galaxy.sites[siteId]!.market.stock[contract.cargo.kind] = clamp(galaxy.sites[siteId]!.market.stock[contract.cargo.kind] + contract.cargo.units)
  }
  hero.gold += fee
  appendEvent(galaxy, { id: `event:${galaxy.seed}:contract:${siteId}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}:delivered`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'trade', siteId, headline: 'Contract delivered', detail: `${deliverable.length} delivery${deliverable.length === 1 ? '' : 'ies'} paid ${fee} credits.` })
  return { galaxy, message: `Delivery complete: ${fee} credits.` }
}
export const abandonGalaxyCargo = (source: GalaxyState, linkId: string, chunk: number, _legacyNow?: number): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  const cargo = galaxy.cargo.filter(candidate => candidate.contractId)
  if (!cargo.length) return galaxy
  galaxy.cargo = galaxy.cargo.filter(candidate => !candidate.contractId)
  for (const contract of galaxy.contracts) if (cargo.some(candidate => candidate.contractId === contract.id)) contract.status = 'failed'
  galaxy.routeCaches.push({ id: `cache:${linkId}:${chunk}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`, linkId, chunk, cargo, packages: [], recovered: false })
  appendEvent(galaxy, { id: `event:${galaxy.seed}:cache:${linkId}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`, at: galaxy.routeReckoning, routeReckoning: galaxy.routeReckoning, kind: 'loss', headline: 'Contract cargo abandoned', detail: 'A recoverable route cache marks the last known position of the cargo.' })
  return galaxy
}
export const recoverGalaxyRouteCaches = (source: GalaxyState, linkId: string): { galaxy: GalaxyState; message: string } => {
  const galaxy = cloneGalaxy(source)
  const caches = galaxy.routeCaches.filter(cache => cache.linkId === linkId && !cache.recovered && cache.cargo.length)
  const cargo = caches.flatMap(cache => cache.cargo)
  const capacity = 12 - galaxy.cargo.reduce((total, entry) => total + entry.units, 0)
  const recovered = cargo.reduce((total, entry) => total + entry.units, 0)
  if (!caches.length) return { galaxy, message: 'No recoverable cargo cache is recorded on this route.' }
  if (recovered > capacity) return { galaxy, message: 'Voyager cargo hold lacks room for the recovered cache.' }
  galaxy.cargo.push(...cargo.map(entry => ({ ...entry, contractId: undefined })))
  caches.forEach(cache => { cache.recovered = true })
  return { galaxy, message: `Recovered ${recovered} cargo units. The failed contracts remain closed.` }
}
export const galaxySnapshot = (galaxy: GalaxyState, siteIdValue: string): RunState | undefined => galaxy.siteSnapshots[siteIdValue] ? structuredClone(galaxy.siteSnapshots[siteIdValue].run) : undefined
export const availableGalaxySites = (galaxy: GalaxyState): GalaxySite[] => Object.values(galaxy.sites).filter(site => site.discovered).sort((left, right) => left.id.localeCompare(right.id))
export const galaxyChronicle = (galaxy: GalaxyState): readonly GalaxyEvent[] => galaxy.events

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value)
export const migrateGalaxy = (value: unknown): GalaxyState | undefined => {
  if (!isRecord(value) || (value.version !== 1 && value.version !== 2) || typeof value.seed !== 'number' || !Array.isArray(value.sectors) || !isRecord(value.sites) || !Array.isArray(value.couriers) || !Array.isArray(value.factions) || !Array.isArray(value.events) || !isRecord(value.siteSnapshots)) return undefined
  try {
    const legacy = structuredClone(value) as unknown as GalaxyState
    const legacyDay = typeof legacy.sectorDay === 'number' && Number.isFinite(legacy.sectorDay) ? legacy.sectorDay : 0
    const routeReckoning = Number.isInteger(legacy.routeReckoning) && legacy.routeReckoning >= 0 ? legacy.routeReckoning : routeReckoningFromLegacyDay(legacyDay)
    const galaxy: GalaxyState = {
      ...legacy,
      version: 2,
      routeReckoning,
      lastWorldTick: Number.isInteger(legacy.lastWorldTick) && legacy.lastWorldTick >= 0 ? Math.min(legacy.lastWorldTick, routeWorldTickFor(routeReckoning)) : routeWorldTickFor(routeReckoning),
      sectorDay: sectorDayFromRouteReckoning(routeReckoning),
      sharedStash: Array.isArray((value as Record<string, unknown>).sharedStash) ? [...legacy.sharedStash] : []
    }
    galaxy.cargo ??= []
    galaxy.contracts ??= []
    galaxy.sealedPackageContracts ??= []
    galaxy.sealedPackages ??= []
    galaxy.generalManifest ??= { version: 1, nextSequence: 0, entries: [] }
    galaxy.routeCaches ??= []
    for (const cache of galaxy.routeCaches) cache.packages ??= []
    for (const courier of galaxy.couriers) if (courier.rivalId === undefined) delete courier.rivalId
    for (const sector of galaxy.sectors) {
      if (Object.is(sector.x, -0)) sector.x = 0
      if (Object.is(sector.y, -0)) sector.y = 0
    }
    for (const contract of galaxy.contracts) {
      contract.deadlineReckoning ??= routeReckoningFromLegacyDay(contract.deadlineDay)
      contract.deadlineDay = sectorDayFromRouteReckoning(contract.deadlineReckoning)
    }
    for (const contract of galaxy.sealedPackageContracts) {
      contract.version = 2
      contract.offeredAtRouteReckoning ??= routeReckoningFromLegacyDay(contract.offeredAtSectorDay)
      contract.acceptedAtRouteReckoning ??= contract.acceptedAtSectorDay === undefined ? undefined : routeReckoningFromLegacyDay(contract.acceptedAtSectorDay)
      contract.destinationReachedAtRouteReckoning ??= contract.destinationReachedAtSectorDay === undefined ? undefined : routeReckoningFromLegacyDay(contract.destinationReachedAtSectorDay)
      contract.resolvedAtRouteReckoning ??= contract.resolvedAtSectorDay === undefined ? undefined : routeReckoningFromLegacyDay(contract.resolvedAtSectorDay)
      contract.terms.deadlineReckoning ??= routeReckoningFromLegacyDay(contract.terms.deadlineDay)
      contract.terms.deadlineDay = sectorDayFromRouteReckoning(contract.terms.deadlineReckoning)
    }
    for (const packageRecord of galaxy.sealedPackages) packageRecord.version = 2
    galaxy.events = galaxy.events.map(event => {
      const existingReckoning = event.routeReckoning
      const eventReckoning = typeof existingReckoning === 'number' && Number.isInteger(existingReckoning) && existingReckoning >= 0 ? existingReckoning : routeReckoning
      return { ...event, at: eventReckoning, routeReckoning: eventReckoning }
    })
    for (const site of Object.values(galaxy.sites)) {
      site.supplies ??= 45
      site.salvage ??= 45
      site.market ??= { stock: { provisions: 12, components: 12, salvage: 12, biosamples: 12 }, demand: { provisions: 12, components: 12, salvage: 12, biosamples: 12 }, prices: { provisions: 10, components: 10, salvage: 10, biosamples: 10 } }
      if (Object.is(site.x, -0)) site.x = 0
      if (Object.is(site.y, -0)) site.y = 0
      site.lastChangedAt = Number.isInteger(site.lastChangedAt) && site.lastChangedAt >= 0 && site.lastChangedAt <= routeReckoning ? site.lastChangedAt : routeReckoning
    }
    for (const snapshot of Object.values(galaxy.siteSnapshots)) snapshot.savedAt = Number.isInteger(snapshot.savedAt) && snapshot.savedAt >= 0 && snapshot.savedAt <= routeReckoning ? snapshot.savedAt : routeReckoning
    normalizeGeneralManifest(galaxy)
    if (!galaxy.sites[galaxy.activeSiteId] || !galaxy.couriers.some(courier => courier.id === galaxy.activeCourierId)) return undefined
    return cloneGalaxy(galaxy)
  } catch { return undefined }
}
