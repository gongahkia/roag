import { biomeName } from '../content'
import { rngFor } from '../rng'
import type { Biome, GalaxyCourier, GalaxyCourierRoutine, GalaxyEvent, GalaxyFaction, GalaxyFactionId, GalaxySite, GalaxySiteSnapshot, GalaxyState, Hero, RunState } from '../types'

export const OUTER_SECTOR_COUNT = 20
export const SITES_PER_SECTOR = 10
export const GALAXY_HOUR_MS = 60 * 60 * 1000
export const GALAXY_TICK_MS = 6 * GALAXY_HOUR_MS
export const GALAXY_OFFLINE_CAP_MS = 7 * 24 * GALAXY_HOUR_MS

const BIOMES: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
const sectorNames = ['Helios Reach', 'Aster Drift', 'Cinder Verge', 'Nerida Veil', 'Orison Belt', 'Kestrel March', 'Vesper Array', 'Halcyon Expanse', 'Borealis Wake', 'Tethys Fold', 'Morrow Chain', 'Lumen Scar', 'Icarus Shelf', 'Sable Meridian', 'Pallas Run', 'Caldera Span', 'Crown of Mira', 'Rook Nebula', 'Morrowline', 'Carthage Deep', 'New Edo Fringe']
const crewTemplates: ReadonlyArray<Pick<GalaxyCourier, 'name' | 'role' | 'origin' | 'calling' | 'routine'>> = [
  { name: 'Ari Venn', role: 'security specialist', origin: 'mineborn', calling: 'trailguard', routine: 'maintain' },
  { name: 'Mika Sol', role: 'systems specialist', origin: 'mosswalker', calling: 'pathmaker', routine: 'scout' },
  { name: 'Cato Ren', role: 'xenoarchivist', origin: 'cavernSeeker', calling: 'spiritbearer', routine: 'research' },
  { name: 'Dara Ilyan', role: 'field medic', origin: 'tidebound', calling: 'trailguard', routine: 'recover' }
]
const factionNames: Record<GalaxyFactionId, string> = { voyager: 'Jomon Voyager', salvagers: 'Free Salvagers', relayGuild: 'Relay Guild', voidborn: 'Voidborn', settlers: 'New Edo Settlers' }
const factions = (): GalaxyFaction[] => [
  { id: 'voyager', name: factionNames.voyager, influence: 18, disposition: 40 },
  { id: 'salvagers', name: factionNames.salvagers, influence: 24, disposition: -8 },
  { id: 'relayGuild', name: factionNames.relayGuild, influence: 27, disposition: 12 },
  { id: 'voidborn', name: factionNames.voidborn, influence: 19, disposition: -28 },
  { id: 'settlers', name: factionNames.settlers, influence: 12, disposition: 28 }
]

const siteId = (sector: number, index: number) => `sector-${String(sector).padStart(2, '0')}:site-${String(index).padStart(2, '0')}`
const sectorId = (sector: number) => `sector-${String(sector).padStart(2, '0')}`
const copyEvent = (event: GalaxyEvent): GalaxyEvent => ({ ...event })
const copySite = (site: GalaxySite): GalaxySite => ({ ...site, links: [...site.links] })
const copySnapshot = (snapshot: GalaxySiteSnapshot): GalaxySiteSnapshot => ({ version: 1, savedAt: snapshot.savedAt, run: structuredClone(snapshot.run) })

const siteName = (biome: Biome, sector: string): string => `${biomeName[biome].replace(' Colony', '')} // ${sector}`
const initialFaction = (sector: number, index: number): GalaxyFactionId => (['salvagers', 'relayGuild', 'voidborn', 'settlers'] as const)[(sector * 7 + index * 3) % 4]!
const courierId = (index: number) => `voyager-crew-${index}`

export const createGalaxy = (seed: number, primary: Hero, now = Date.now()): GalaxyState => {
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
        x: Math.round(Math.cos(angle) * distance * 10) + (siteIndex % 5) * 3,
        y: Math.round(Math.sin(angle) * distance * 7) + Math.floor(siteIndex / 5) * 3,
        links: [], discovered: sectorIndex === 0 && siteIndex === 0, completed: false,
        control: initialFaction(sectorIndex, siteIndex), integrity: 55 + rng.int(0, 35), ecology: 35 + rng.int(0, 40), construction: 0, lastChangedAt: now
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
    sectors.push({ id, name, x: Math.round(Math.cos(angle) * distance), y: Math.round(Math.sin(angle) * distance), discovered: sectorIndex === 0, siteIds: ids })
  }
  for (let sectorIndex = 0; sectorIndex < OUTER_SECTOR_COUNT; sectorIndex++) {
    const source = sites[siteId(sectorIndex, 8)]!
    const destination = siteId(sectorIndex + 1, 1)
    source.links.push(destination)
    sites[destination]!.links.push(source.id)
  }
  const activeId = siteId(0, 0)
  const primaryCourier: GalaxyCourier = { id: courierId(0), name: primary.name, role: 'voyager specialist', origin: primary.origin, calling: primary.calling, routine: 'socialize', status: 'available', affinity: 12, siteId: activeId, personalItems: [] }
  const couriers = [primaryCourier, ...crewTemplates.map((template, index): GalaxyCourier => ({ id: courierId(index + 1), ...template, status: 'available', affinity: (index % 2 ? -4 : 7), rivalId: index === 1 ? courierId(3) : undefined, personalItems: [] }))]
  return { version: 1, seed, createdAt: now, lastSimulatedAt: now, sectorDay: 0, activeSiteId: activeId, activeCourierId: primaryCourier.id, sectors, sites, couriers, factions: factions(), events: [{ id: `event:${seed}:arrival`, at: now, kind: 'discovery', siteId: activeId, headline: 'Voyager enters Helios Reach', detail: 'The Jomon Voyager has arrived at an uncharted frontier. Only Kestrel is reachable until its landing routes are surveyed.' }], siteSnapshots: {} }
}

export const cloneGalaxy = (galaxy: GalaxyState): GalaxyState => ({ ...galaxy, sectors: galaxy.sectors.map(sector => ({ ...sector, siteIds: [...sector.siteIds] })), sites: Object.fromEntries(Object.entries(galaxy.sites).map(([id, site]) => [id, copySite(site)])), couriers: galaxy.couriers.map(courier => ({ ...courier, personalItems: [...courier.personalItems] })), factions: galaxy.factions.map(faction => ({ ...faction })), events: galaxy.events.map(copyEvent), siteSnapshots: Object.fromEntries(Object.entries(galaxy.siteSnapshots).map(([id, snapshot]) => [id, copySnapshot(snapshot)])) })

const appendEvent = (galaxy: GalaxyState, event: GalaxyEvent): void => {
  galaxy.events.unshift(event)
  galaxy.events = galaxy.events.slice(0, 240)
}
const eventId = (galaxy: GalaxyState, tick: number, suffix: string) => `event:${galaxy.seed}:${tick}:${suffix}`
const clamp = (value: number, min = 0, max = 100): number => Math.max(min, Math.min(max, value))
const shiftSite = (galaxy: GalaxyState, site: GalaxySite, tick: number): void => {
  const rng = rngFor(galaxy.seed, 'galaxy', 'tick', tick, site.id)
  const change = rng.int(-12, 12)
  const kind = rng.int(0, 4)
  if (kind === 0) {
    site.integrity = clamp(site.integrity + change)
    appendEvent(galaxy, { id: eventId(galaxy, tick, `${site.id}:integrity`), at: galaxy.lastSimulatedAt, kind: 'ecology', siteId: site.id, headline: `${site.name}: terrain shifted`, detail: change < 0 ? 'A regional disturbance damaged known routes.' : 'Local crews stabilized part of the terrain.' })
  } else if (kind === 1) {
    site.ecology = clamp(site.ecology + change)
    appendEvent(galaxy, { id: eventId(galaxy, tick, `${site.id}:ecology`), at: galaxy.lastSimulatedAt, kind: 'ecology', siteId: site.id, headline: `${site.name}: ecological change`, detail: change < 0 ? 'A hostile bloom spread through the site.' : 'A dormant habitat recovered.' })
  } else if (kind === 2) {
    const control = (['salvagers', 'relayGuild', 'voidborn', 'settlers'] as const)[rng.int(0, 3)]!
    if (control !== site.control) {
      site.control = control
      appendEvent(galaxy, { id: eventId(galaxy, tick, `${site.id}:territory`), at: galaxy.lastSimulatedAt, kind: 'territory', siteId: site.id, headline: `${site.name}: control changed`, detail: `${factionNames[control]} now holds the visible approaches.` })
    }
  } else {
    site.construction = clamp(site.construction + Math.abs(change))
    appendEvent(galaxy, { id: eventId(galaxy, tick, `${site.id}:construction`), at: galaxy.lastSimulatedAt, kind: 'construction', siteId: site.id, headline: `${site.name}: new structures detected`, detail: 'Autonomous crews altered the site while the Voyager was away.' })
  }
  site.lastChangedAt = galaxy.lastSimulatedAt
}
const evolveCouriers = (galaxy: GalaxyState, tick: number): void => {
  const rng = rngFor(galaxy.seed, 'galaxy', 'crew', tick)
  for (const courier of galaxy.couriers) {
    if (courier.status === 'dead' || courier.status === 'retired') continue
    if (courier.id === galaxy.activeCourierId) continue
    courier.affinity = clamp(courier.affinity + rng.int(-3, 3), -100, 100)
    if (rng.chance(4)) {
      courier.status = 'injured'
      appendEvent(galaxy, { id: eventId(galaxy, tick, `${courier.id}:injured`), at: galaxy.lastSimulatedAt, kind: 'loss', courierId: courier.id, headline: `${courier.name} was injured off-watch`, detail: `${courier.name}'s ${courier.routine} shift encountered a sector hazard.` })
    } else if (rng.chance(1)) {
      courier.status = 'dead'
      appendEvent(galaxy, { id: eventId(galaxy, tick, `${courier.id}:lost`), at: galaxy.lastSimulatedAt, kind: 'loss', courierId: courier.id, headline: `${courier.name} is lost`, detail: `Their final autonomous ${courier.routine} shift is now part of the Voyager chronicle.` })
    }
  }
}

export const reconcileGalaxy = (source: GalaxyState, now = Date.now()): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  const elapsed = Math.max(0, Math.min(GALAXY_OFFLINE_CAP_MS, now - galaxy.lastSimulatedAt))
  const ticks = Math.floor(elapsed / GALAXY_TICK_MS)
  if (!ticks) return galaxy
  for (let offset = 1; offset <= ticks; offset++) {
    const tick = Math.floor(galaxy.sectorDay * GALAXY_HOUR_MS / GALAXY_TICK_MS) + offset
    galaxy.lastSimulatedAt += GALAXY_TICK_MS
    galaxy.sectorDay += GALAXY_TICK_MS / GALAXY_HOUR_MS
    const candidates = Object.values(galaxy.sites)
    const site = candidates[rngFor(galaxy.seed, 'galaxy', 'site', tick).int(0, candidates.length - 1)]
    if (site) shiftSite(galaxy, site, tick)
    evolveCouriers(galaxy, tick)
  }
  return galaxy
}

export const discoverLinkedSites = (source: GalaxyState, sourceId: string, now = Date.now()): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  const site = galaxy.sites[sourceId]
  if (!site) return galaxy
  site.completed = true
  site.lastChangedAt = now
  for (const id of site.links) {
    const destination = galaxy.sites[id]
    if (!destination || destination.discovered) continue
    destination.discovered = true
    const sector = galaxy.sectors.find(candidate => candidate.id === destination.sectorId)
    if (sector) sector.discovered = true
    appendEvent(galaxy, { id: `event:${galaxy.seed}:link:${sourceId}:${id}`, at: now, kind: 'discovery', siteId: id, headline: `Route to ${destination.name} surveyed`, detail: `A physical approach has been found from ${site.name}.` })
  }
  appendEvent(galaxy, { id: `event:${galaxy.seed}:complete:${sourceId}:${now}`, at: now, kind: 'discovery', siteId: sourceId, headline: `${site.name} survey archived`, detail: 'The Voyager has preserved this landing as a persistent site.' })
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
export const saveGalaxySite = (source: GalaxyState, siteIdValue: string, run: RunState, now = Date.now()): GalaxyState => {
  const galaxy = cloneGalaxy(source)
  if (!galaxy.sites[siteIdValue]) return galaxy
  galaxy.siteSnapshots[siteIdValue] = { version: 1, run: structuredClone(run), savedAt: now }
  return galaxy
}
export const galaxySnapshot = (galaxy: GalaxyState, siteIdValue: string): RunState | undefined => galaxy.siteSnapshots[siteIdValue] ? structuredClone(galaxy.siteSnapshots[siteIdValue].run) : undefined
export const availableGalaxySites = (galaxy: GalaxyState): GalaxySite[] => Object.values(galaxy.sites).filter(site => site.discovered).sort((left, right) => left.id.localeCompare(right.id))
export const galaxyChronicle = (galaxy: GalaxyState): readonly GalaxyEvent[] => galaxy.events

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value)
export const migrateGalaxy = (value: unknown): GalaxyState | undefined => {
  if (!isRecord(value) || value.version !== 1 || typeof value.seed !== 'number' || !Array.isArray(value.sectors) || !isRecord(value.sites) || !Array.isArray(value.couriers) || !Array.isArray(value.factions) || !Array.isArray(value.events) || !isRecord(value.siteSnapshots)) return undefined
  try {
    const galaxy = value as unknown as GalaxyState
    if (!galaxy.sites[galaxy.activeSiteId] || !galaxy.couriers.some(courier => courier.id === galaxy.activeCourierId)) return undefined
    return cloneGalaxy(galaxy)
  } catch { return undefined }
}
