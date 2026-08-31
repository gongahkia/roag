import { runAutoplay } from './autoplay-runner'
import { abandonGalaxyCargo, acceptGalaxyContract, acceptSealedPackageContract, advanceTransitWindow, applyGalaxySiteConditions, createGalaxy, deliverGalaxyContracts, deliverSealedPackage, discoverLinkedSites, loseGalaxyCourier, loseSealedPackagesForCourier, markSealedPackageDestinationReached, newHero, newRun, newTransitRun, reconcileGalaxy, recordGalaxyLanding, recoverGalaxyRouteCaches, recoverSealedPackageRouteCaches, violateSealedPackageSeal } from './engine'

export const AUTOPLAY_TASK_CATALOG_VERSION = 1
export const AUTOPLAY_TASK_FIXTURE_TIME = 1_735_689_600_000

export type AutoplayTaskCategory = 'tactical' | 'voyager' | 'economy' | 'ecology' | 'lifecycle'
export type AutoplayTaskStatus = 'passed' | 'failed' | 'blocked' | 'timed-out'

export interface AutoplayTaskExecution {
  actions: string[]
  events: string[]
  passed: boolean
  summary: Record<string, unknown>
  reason?: string
}

export interface AutoplayTask {
  id: string
  category: AutoplayTaskCategory
  priority: number
  prerequisites: readonly string[]
  uiTaskId: string
  run(): AutoplayTaskExecution
}

const task = (id: string, category: AutoplayTaskCategory, priority: number, uiTaskId: string, run: () => AutoplayTaskExecution, prerequisites: readonly string[] = []): AutoplayTask => ({ id, category, priority, prerequisites, uiTaskId, run })
const hero = () => newHero({ name: 'Task Courier' })
const link = (origin: string, destination: string) => [origin, destination].sort().join('::')

const tacticalTask = (): AutoplayTaskExecution => {
  const report = runAutoplay(newRun(7, 'mine'), { mode: 'omniscient', policy: 'clear', turnLimit: 96, chainAreas: false, chainFloors: false, captureTrace: false })
  return {
    actions: report.commands,
    events: Object.keys(report.metrics.eventOutcomes).sort(),
    passed: report.commands.length > 0 && report.outcome !== 'error' && report.outcome !== 'unsupported',
    summary: { outcome: report.outcome, turns: report.turns, commands: report.commands.length, fingerprint: report.fingerprint },
    ...(report.error ? { reason: report.error } : {})
  }
}

const discoveryTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(91, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const origin = galaxy.activeSiteId
  const surveyed = discoverLinkedSites(galaxy, origin, AUTOPLAY_TASK_FIXTURE_TIME + 1)
  const links = surveyed.sites[origin]!.links
  const passed = surveyed.sites[origin]!.completed && links.length > 0 && links.every(id => surveyed.sites[id]?.discovered)
  return { actions: ['operate:bridge-console'], events: surveyed.events.map(event => event.kind), passed, summary: { origin, discoveredLinks: links.filter(id => surveyed.sites[id]?.discovered).sort() }, ...(passed ? {} : { reason: 'survey did not reveal every physical link' }) }
}

const transitTask = (): AutoplayTaskExecution => {
  const travel = { version: 1 as const, fromSiteId: 'sector-00:site-00', toSiteId: 'sector-00:site-01', linkId: 'sector-00:site-00::sector-00:site-01', chunkCount: 5, residentStart: 0, activeChunk: 0, situations: ['quiet', 'patrol', 'hazard', 'trader', 'ecology'] as const }
  const state = newTransitRun(71, 'wilds', hero(), travel)
  const chunkWidth = state.floor.width / 3
  state.hero.x = chunkWidth * 2
  const advanced = advanceTransitWindow(state)
  const passed = advanced && state.travel?.residentStart === 1 && state.travel.situations.length === 5
  return { actions: ['walk:connector-window'], events: state.floor.actors.map(actor => actor.id).filter(id => id.startsWith('route-')).sort(), passed, summary: { residentStart: state.travel?.residentStart, situations: state.travel?.situations }, ...(passed ? {} : { reason: 'resident transit window did not advance' }) }
}

const cargoDeliveryTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(19, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const origin = galaxy.activeSiteId
  const destination = galaxy.sites[origin]!.links[0]!
  const surveyed = discoverLinkedSites(galaxy, origin, AUTOPLAY_TASK_FIXTURE_TIME + 1)
  const accepted = acceptGalaxyContract(surveyed, origin, destination, AUTOPLAY_TASK_FIXTURE_TIME + 2)
  const courier = hero()
  const delivered = deliverGalaxyContracts(accepted.galaxy, destination, courier, AUTOPLAY_TASK_FIXTURE_TIME + 3)
  const passed = accepted.galaxy.contracts.some(contract => contract.status === 'active') && delivered.galaxy.contracts.some(contract => contract.status === 'completed') && courier.gold > 0
  return { actions: ['operate:origin-airlock', 'operate:destination-airlock'], events: delivered.galaxy.events.map(event => event.kind), passed, summary: { gold: courier.gold, cargo: delivered.galaxy.cargo, contractStates: delivered.galaxy.contracts.map(contract => contract.status) }, ...(passed ? {} : { reason: 'contract cargo was not delivered' }) }
}

const cargoRecoveryTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(23, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const origin = galaxy.activeSiteId
  const destination = galaxy.sites[origin]!.links[0]!
  const surveyed = discoverLinkedSites(galaxy, origin, AUTOPLAY_TASK_FIXTURE_TIME + 1)
  const accepted = acceptGalaxyContract(surveyed, origin, destination, AUTOPLAY_TASK_FIXTURE_TIME + 2)
  const route = link(origin, destination)
  const abandoned = abandonGalaxyCargo(accepted.galaxy, route, 1, AUTOPLAY_TASK_FIXTURE_TIME + 3)
  const recovered = recoverGalaxyRouteCaches(abandoned, route)
  const passed = abandoned.routeCaches.some(cache => !cache.recovered) && recovered.galaxy.routeCaches.every(cache => cache.recovered) && recovered.galaxy.cargo.length > 0
  return { actions: ['abandon:cargo', 'operate:route-cache'], events: recovered.galaxy.events.map(event => event.kind), passed, summary: { caches: recovered.galaxy.routeCaches.map(cache => ({ id: cache.id, recovered: cache.recovered })), cargo: recovered.galaxy.cargo }, ...(passed ? {} : { reason: 'recoverable cargo cache was not restored' }) }
}

const sealedPackageIntactTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(43, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const contract = galaxy.sealedPackageContracts[0]
  if (!contract) return { actions: [], events: [], passed: false, summary: {}, reason: 'M1 package offer is missing' }
  const accepted = acceptSealedPackageContract(galaxy, contract.id, galaxy.activeCourierId)
  const courier = hero()
  const arrived = markSealedPackageDestinationReached(accepted.galaxy, contract.terms.destinationSiteId)
  const delivered = deliverSealedPackage(arrived, contract.id, courier)
  const packageRecord = delivered.galaxy.sealedPackages.find(candidate => candidate.contractId === contract.id)
  const passed = delivered.changed && delivered.galaxy.sealedPackageContracts[0]?.status === 'completed' && packageRecord?.sealState === 'intact' && packageRecord.custody === 'recipient' && courier.gold === contract.terms.payment
  return { actions: ['inspect:exterior', 'accept:assigned-courier', 'land:Kestrel', 'settle:intact'], events: delivered.galaxy.generalManifest.entries.map(entry => entry.kind), passed, summary: { contractStatus: delivered.galaxy.sealedPackageContracts[0]?.status, seal: packageRecord?.sealState, custody: packageRecord?.custody, gold: courier.gold }, ...(passed ? {} : { reason: 'intact sealed package did not require explicit settlement' }) }
}

const sealedPackageTamperedTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(47, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const contract = galaxy.sealedPackageContracts[0]
  if (!contract) return { actions: [], events: [], passed: false, summary: {}, reason: 'M1 package offer is missing' }
  const accepted = acceptSealedPackageContract(galaxy, contract.id, galaxy.activeCourierId)
  const opened = violateSealedPackageSeal(accepted.galaxy, contract.id)
  const courier = hero()
  const delivered = deliverSealedPackage(markSealedPackageDestinationReached(opened.galaxy, contract.terms.destinationSiteId), contract.id, courier)
  const packageRecord = delivered.galaxy.sealedPackages.find(candidate => candidate.contractId === contract.id)
  const expectedPayment = contract.terms.payment - contract.terms.collateral
  const passed = delivered.changed && packageRecord?.sealState === 'opened' && packageRecord.revealedContents !== undefined && courier.gold === expectedPayment
  return { actions: ['accept:assigned-courier', 'confirm:open-seal', 'land:Kestrel', 'settle:tampered'], events: delivered.galaxy.generalManifest.entries.map(entry => entry.kind), passed, summary: { seal: packageRecord?.sealState, revealed: packageRecord?.revealedContents?.id, gold: courier.gold, expectedPayment }, ...(passed ? {} : { reason: 'tampered package did not disclose contents and apply collateral settlement' }) }
}

const sealedPackageRecoveryTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(53, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const contract = galaxy.sealedPackageContracts[0]
  if (!contract) return { actions: [], events: [], passed: false, summary: {}, reason: 'M1 package offer is missing' }
  const accepted = acceptSealedPackageContract(galaxy, contract.id, galaxy.activeCourierId)
  const cached = loseSealedPackagesForCourier(accepted.galaxy, accepted.galaxy.activeCourierId, 'M1-Kestrel-connector', 1)
  const afterDeath = loseGalaxyCourier(cached, cached.activeCourierId, 'task fixture courier loss', AUTOPLAY_TASK_FIXTURE_TIME + 1)
  const recovered = recoverSealedPackageRouteCaches(afterDeath, 'M1-Kestrel-connector')
  const packageRecord = recovered.galaxy.sealedPackages.find(candidate => candidate.contractId === contract.id)
  const passed = recovered.changed && recovered.galaxy.sealedPackageContracts[0]?.status === 'failed' && packageRecord?.custody === 'atJomon' && packageRecord.sealState === 'intact' && recovered.galaxy.generalManifest.entries.some(entry => entry.kind === 'packageRecovered')
  return { actions: ['accept:assigned-courier', 'death:connector', 'select:replacement-courier', 'operate:route-cache'], events: recovered.galaxy.generalManifest.entries.map(entry => entry.kind), passed, summary: { contractStatus: recovered.galaxy.sealedPackageContracts[0]?.status, seal: packageRecord?.sealState, custody: packageRecord?.custody }, ...(passed ? {} : { reason: 'courier loss did not preserve an intact recoverable sealed package' }) }
}

const ecologyTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(29, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const site = galaxy.sites[galaxy.activeSiteId]!
  site.control = 'voidborn'
  site.integrity = 20
  site.ecology = 20
  const state = newRun(galaxy.seed, site.biome)
  const hostile = state.floor.actors.find(actor => actor.hostile)
  if (!hostile) return { actions: [], events: [], passed: false, summary: {}, reason: 'fixture has no hostile actor' }
  const before = { health: hostile.maxHealth, attack: hostile.attack, defense: hostile.defense }
  applyGalaxySiteConditions(state, site)
  const passed = hostile.maxHealth > before.health && hostile.attack > before.attack && hostile.defense > before.defense
  return { actions: ['land:contested-site'], events: ['landing-condition:territory', 'landing-condition:ecology'], passed, summary: { before, after: { health: hostile.maxHealth, attack: hostile.attack, defense: hostile.defense } }, ...(passed ? {} : { reason: 'site conditions did not change encounter pressure' }) }
}

const landingTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(31, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const site = galaxy.sites[galaxy.activeSiteId]!
  const state = newRun(galaxy.seed, site.biome)
  const before = { salvage: site.salvage, supplies: site.supplies, gold: state.hero.gold }
  const settled = recordGalaxyLanding(galaxy, site.id, state, AUTOPLAY_TASK_FIXTURE_TIME + 1)
  const after = settled.sites[site.id]!
  const passed = state.hero.gold > before.gold && after.salvage < before.salvage && after.supplies > before.supplies
  return { actions: ['complete:landing'], events: settled.events.map(event => event.kind), passed, summary: { before, after: { salvage: after.salvage, supplies: after.supplies, gold: state.hero.gold } }, ...(passed ? {} : { reason: 'landing settlement did not update the site economy' }) }
}

const sectorClockTask = (): AutoplayTaskExecution => {
  const galaxy = createGalaxy(37, hero(), AUTOPLAY_TASK_FIXTURE_TIME)
  const advanced = reconcileGalaxy(galaxy, AUTOPLAY_TASK_FIXTURE_TIME + 12 * 60 * 60 * 1_000)
  const passed = advanced.sectorDay === 12 && advanced.events.length > galaxy.events.length
  return { actions: ['wait:sector-clock'], events: advanced.events.map(event => event.kind), passed, summary: { sectorDay: advanced.sectorDay, eventCount: advanced.events.length }, ...(passed ? {} : { reason: 'sector simulation did not advance deterministically' }) }
}

export const autoplayTaskCatalog = (): readonly AutoplayTask[] => [
  task('tactical.core-loop', 'tactical', 100, 'ui.core-loop', tacticalTask),
  task('voyager.site-discovery', 'voyager', 90, 'ui.sector-navigation', discoveryTask, ['tactical.core-loop']),
  task('voyager.transit-window', 'voyager', 85, 'ui.transit', transitTask, ['voyager.site-discovery']),
  task('voyager.contract-delivery', 'economy', 80, 'ui.contract-delivery', cargoDeliveryTask, ['voyager.site-discovery']),
  task('voyager.cargo-recovery', 'economy', 75, 'ui.cargo-recovery', cargoRecoveryTask, ['voyager.contract-delivery']),
  task('voyager.sealed-package-intact', 'economy', 74, 'ui.sealed-package-intact', sealedPackageIntactTask, ['voyager.site-discovery']),
  task('voyager.sealed-package-tampered', 'economy', 73, 'ui.sealed-package-tampered', sealedPackageTamperedTask, ['voyager.sealed-package-intact']),
  task('voyager.sealed-package-recovery', 'lifecycle', 72, 'ui.sealed-package-recovery', sealedPackageRecoveryTask, ['voyager.sealed-package-tampered']),
  task('voyager.landing-conditions', 'ecology', 70, 'ui.landing-conditions', ecologyTask, ['voyager.site-discovery']),
  task('voyager.landing-settlement', 'lifecycle', 65, 'ui.landing-settlement', landingTask, ['voyager.landing-conditions']),
  task('voyager.sector-clock', 'lifecycle', 60, 'ui.sector-clock', sectorClockTask, ['voyager.landing-settlement'])
]

export const assertAutoplayTaskCatalog = (catalog: readonly AutoplayTask[] = autoplayTaskCatalog()): void => {
  const ids = new Set<string>()
  for (const entry of catalog) {
    if (ids.has(entry.id)) throw new Error(`duplicate autoplay task id: ${entry.id}`)
    ids.add(entry.id)
    if (!entry.uiTaskId) throw new Error(`autoplay task ${entry.id} lacks browser coverage id`)
  }
  for (const entry of catalog) for (const prerequisite of entry.prerequisites) if (!ids.has(prerequisite)) throw new Error(`autoplay task ${entry.id} has unknown prerequisite ${prerequisite}`)
}
