// f4b0ca4de98327762a72abe181a629b1dd1c5575
import { runAutoplay } from './autoplay-runner';
import { abandonGalaxyCargo, acceptGalaxyContract, acceptSealedPackageContract, advanceGalaxyRouteReckoning, advanceTransitWindow, applyGalaxySiteConditions, createGalaxy, deliverGalaxyContracts, deliverSealedPackage, discoverLinkedSites, loseGalaxyCourier, loseSealedPackagesForCourier, markSealedPackageDestinationReached, newHero, newRun, newTransitRun, recordGalaxyLanding, recoverGalaxyRouteCaches, recoverSealedPackageRouteCaches, ROUTE_RECKONING_WORLD_TICK_UNITS, violateSealedPackageSeal } from './engine';
export const AUTOPLAY_TASK_CATALOG_VERSION = 1;
export const AUTOPLAY_TASK_FIXTURE_TIME = 1735689600000;
const task = (id, category, priority, uiTaskId, run, prerequisites = []) => ({
  id,
  category,
  priority,
  prerequisites,
  uiTaskId,
  run
});
const hero = () => newHero({
  name: 'Task Courier'
});
const link = (origin, destination) => [origin, destination].sort().join('::');
const tacticalTask = () => {
  const report = runAutoplay(newRun(7, 'mine'), {
    mode: 'omniscient',
    policy: 'clear',
    turnLimit: 96,
    chainAreas: false,
    chainFloors: false,
    captureTrace: false
  });
  return {
    actions: report.commands,
    events: Object.keys(report.metrics.eventOutcomes).sort(),
    passed: report.commands.length > 0 && report.outcome !== 'error' && report.outcome !== 'unsupported',
    summary: {
      outcome: report.outcome,
      turns: report.turns,
      commands: report.commands.length,
      fingerprint: report.fingerprint
    },
    ...(report.error ? {
      reason: report.error
    } : {})
  };
};
const discoveryTask = () => {
  const galaxy = createGalaxy(91, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const origin = galaxy.activeSiteId;
  const surveyed = discoverLinkedSites(galaxy, origin, AUTOPLAY_TASK_FIXTURE_TIME + 1);
  const links = surveyed.sites[origin].links;
  const passed = surveyed.sites[origin].completed && links.length > 0 && links.every(id => {
    var _surveyed$sites$id;
    return (_surveyed$sites$id = surveyed.sites[id]) === null || _surveyed$sites$id === void 0 ? void 0 : _surveyed$sites$id.discovered;
  });
  return {
    actions: ['operate:bridge-console'],
    events: surveyed.events.map(event => event.kind),
    passed,
    summary: {
      origin,
      discoveredLinks: links.filter(id => {
        var _surveyed$sites$id2;
        return (_surveyed$sites$id2 = surveyed.sites[id]) === null || _surveyed$sites$id2 === void 0 ? void 0 : _surveyed$sites$id2.discovered;
      }).sort()
    },
    ...(passed ? {} : {
      reason: 'survey did not reveal every physical link'
    })
  };
};
const transitTask = () => {
  var _state$travel, _state$travel2, _state$travel3;
  const travel = {
    version: 1,
    fromSiteId: 'sector-00:site-00',
    toSiteId: 'sector-00:site-01',
    linkId: 'sector-00:site-00::sector-00:site-01',
    chunkCount: 5,
    residentStart: 0,
    activeChunk: 0,
    situations: ['quiet', 'patrol', 'hazard', 'trader', 'ecology']
  };
  const state = newTransitRun(71, 'wilds', hero(), travel);
  const chunkWidth = state.floor.width / 3;
  state.hero.x = chunkWidth * 2;
  const advanced = advanceTransitWindow(state);
  const passed = advanced && ((_state$travel = state.travel) === null || _state$travel === void 0 ? void 0 : _state$travel.residentStart) === 1 && state.travel.situations.length === 5;
  return {
    actions: ['walk:connector-window'],
    events: state.floor.actors.map(actor => actor.id).filter(id => id.startsWith('route-')).sort(),
    passed,
    summary: {
      residentStart: (_state$travel2 = state.travel) === null || _state$travel2 === void 0 ? void 0 : _state$travel2.residentStart,
      situations: (_state$travel3 = state.travel) === null || _state$travel3 === void 0 ? void 0 : _state$travel3.situations
    },
    ...(passed ? {} : {
      reason: 'resident transit window did not advance'
    })
  };
};
const cargoDeliveryTask = () => {
  const galaxy = createGalaxy(19, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const origin = galaxy.activeSiteId;
  const destination = galaxy.sites[origin].links[0];
  const surveyed = discoverLinkedSites(galaxy, origin, AUTOPLAY_TASK_FIXTURE_TIME + 1);
  const accepted = acceptGalaxyContract(surveyed, origin, destination, AUTOPLAY_TASK_FIXTURE_TIME + 2);
  const courier = hero();
  const delivered = deliverGalaxyContracts(accepted.galaxy, destination, courier, AUTOPLAY_TASK_FIXTURE_TIME + 3);
  const passed = accepted.galaxy.contracts.some(contract => contract.status === 'active') && delivered.galaxy.contracts.some(contract => contract.status === 'completed') && courier.gold > 0;
  return {
    actions: ['operate:origin-airlock', 'operate:destination-airlock'],
    events: delivered.galaxy.events.map(event => event.kind),
    passed,
    summary: {
      gold: courier.gold,
      cargo: delivered.galaxy.cargo,
      contractStates: delivered.galaxy.contracts.map(contract => contract.status)
    },
    ...(passed ? {} : {
      reason: 'contract cargo was not delivered'
    })
  };
};
const cargoRecoveryTask = () => {
  const galaxy = createGalaxy(23, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const origin = galaxy.activeSiteId;
  const destination = galaxy.sites[origin].links[0];
  const surveyed = discoverLinkedSites(galaxy, origin, AUTOPLAY_TASK_FIXTURE_TIME + 1);
  const accepted = acceptGalaxyContract(surveyed, origin, destination, AUTOPLAY_TASK_FIXTURE_TIME + 2);
  const route = link(origin, destination);
  const abandoned = abandonGalaxyCargo(accepted.galaxy, route, 1, AUTOPLAY_TASK_FIXTURE_TIME + 3);
  const recovered = recoverGalaxyRouteCaches(abandoned, route);
  const passed = abandoned.routeCaches.some(cache => !cache.recovered) && recovered.galaxy.routeCaches.every(cache => cache.recovered) && recovered.galaxy.cargo.length > 0;
  return {
    actions: ['abandon:cargo', 'operate:route-cache'],
    events: recovered.galaxy.events.map(event => event.kind),
    passed,
    summary: {
      caches: recovered.galaxy.routeCaches.map(cache => ({
        id: cache.id,
        recovered: cache.recovered
      })),
      cargo: recovered.galaxy.cargo
    },
    ...(passed ? {} : {
      reason: 'recoverable cargo cache was not restored'
    })
  };
};
const sealedPackageIntactTask = () => {
  var _delivered$galaxy$sea, _delivered$galaxy$sea2;
  const galaxy = createGalaxy(43, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const contract = galaxy.sealedPackageContracts[0];
  if (!contract) return {
    actions: [],
    events: [],
    passed: false,
    summary: {},
    reason: 'M1 package offer is missing'
  };
  const accepted = acceptSealedPackageContract(galaxy, contract.id, galaxy.activeCourierId);
  const courier = hero();
  const arrived = markSealedPackageDestinationReached(accepted.galaxy, contract.terms.destinationSiteId);
  const delivered = deliverSealedPackage(arrived, contract.id, courier);
  const packageRecord = delivered.galaxy.sealedPackages.find(candidate => candidate.contractId === contract.id);
  const passed = delivered.changed && ((_delivered$galaxy$sea = delivered.galaxy.sealedPackageContracts[0]) === null || _delivered$galaxy$sea === void 0 ? void 0 : _delivered$galaxy$sea.status) === 'completed' && (packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.sealState) === 'intact' && packageRecord.custody === 'recipient' && courier.gold === contract.terms.payment;
  return {
    actions: ['inspect:exterior', 'accept:assigned-courier', 'land:Kestrel', 'settle:intact'],
    events: delivered.galaxy.generalManifest.entries.map(entry => entry.kind),
    passed,
    summary: {
      contractStatus: (_delivered$galaxy$sea2 = delivered.galaxy.sealedPackageContracts[0]) === null || _delivered$galaxy$sea2 === void 0 ? void 0 : _delivered$galaxy$sea2.status,
      seal: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.sealState,
      custody: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.custody,
      gold: courier.gold
    },
    ...(passed ? {} : {
      reason: 'intact sealed package did not require explicit settlement'
    })
  };
};
const sealedPackageTamperedTask = () => {
  var _packageRecord$reveal;
  const galaxy = createGalaxy(47, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const contract = galaxy.sealedPackageContracts[0];
  if (!contract) return {
    actions: [],
    events: [],
    passed: false,
    summary: {},
    reason: 'M1 package offer is missing'
  };
  const accepted = acceptSealedPackageContract(galaxy, contract.id, galaxy.activeCourierId);
  const opened = violateSealedPackageSeal(accepted.galaxy, contract.id);
  const courier = hero();
  const delivered = deliverSealedPackage(markSealedPackageDestinationReached(opened.galaxy, contract.terms.destinationSiteId), contract.id, courier);
  const packageRecord = delivered.galaxy.sealedPackages.find(candidate => candidate.contractId === contract.id);
  const expectedPayment = contract.terms.payment - contract.terms.collateral;
  const passed = delivered.changed && (packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.sealState) === 'opened' && packageRecord.revealedContents !== undefined && courier.gold === expectedPayment;
  return {
    actions: ['accept:assigned-courier', 'confirm:open-seal', 'land:Kestrel', 'settle:tampered'],
    events: delivered.galaxy.generalManifest.entries.map(entry => entry.kind),
    passed,
    summary: {
      seal: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.sealState,
      revealed: packageRecord === null || packageRecord === void 0 || (_packageRecord$reveal = packageRecord.revealedContents) === null || _packageRecord$reveal === void 0 ? void 0 : _packageRecord$reveal.id,
      gold: courier.gold,
      expectedPayment
    },
    ...(passed ? {} : {
      reason: 'tampered package did not disclose contents and apply collateral settlement'
    })
  };
};
const sealedPackageRecoveryTask = () => {
  var _recovered$galaxy$sea, _recovered$galaxy$sea2;
  const galaxy = createGalaxy(53, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const contract = galaxy.sealedPackageContracts[0];
  if (!contract) return {
    actions: [],
    events: [],
    passed: false,
    summary: {},
    reason: 'M1 package offer is missing'
  };
  const accepted = acceptSealedPackageContract(galaxy, contract.id, galaxy.activeCourierId);
  const cached = loseSealedPackagesForCourier(accepted.galaxy, accepted.galaxy.activeCourierId, 'M1-Kestrel-connector', 1);
  const afterDeath = loseGalaxyCourier(cached, cached.activeCourierId, 'task fixture courier loss', AUTOPLAY_TASK_FIXTURE_TIME + 1);
  const recovered = recoverSealedPackageRouteCaches(afterDeath, 'M1-Kestrel-connector');
  const packageRecord = recovered.galaxy.sealedPackages.find(candidate => candidate.contractId === contract.id);
  const passed = recovered.changed && ((_recovered$galaxy$sea = recovered.galaxy.sealedPackageContracts[0]) === null || _recovered$galaxy$sea === void 0 ? void 0 : _recovered$galaxy$sea.status) === 'failed' && (packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.custody) === 'atJomon' && packageRecord.sealState === 'intact' && recovered.galaxy.generalManifest.entries.some(entry => entry.kind === 'packageRecovered');
  return {
    actions: ['accept:assigned-courier', 'death:connector', 'select:replacement-courier', 'operate:route-cache'],
    events: recovered.galaxy.generalManifest.entries.map(entry => entry.kind),
    passed,
    summary: {
      contractStatus: (_recovered$galaxy$sea2 = recovered.galaxy.sealedPackageContracts[0]) === null || _recovered$galaxy$sea2 === void 0 ? void 0 : _recovered$galaxy$sea2.status,
      seal: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.sealState,
      custody: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.custody
    },
    ...(passed ? {} : {
      reason: 'courier loss did not preserve an intact recoverable sealed package'
    })
  };
};
const ecologyTask = () => {
  const galaxy = createGalaxy(29, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const site = galaxy.sites[galaxy.activeSiteId];
  site.control = 'voidborn';
  site.integrity = 20;
  site.ecology = 20;
  const state = newRun(galaxy.seed, site.biome);
  const hostile = state.floor.actors.find(actor => actor.hostile);
  if (!hostile) return {
    actions: [],
    events: [],
    passed: false,
    summary: {},
    reason: 'fixture has no hostile actor'
  };
  const before = {
    health: hostile.maxHealth,
    attack: hostile.attack,
    defense: hostile.defense
  };
  applyGalaxySiteConditions(state, site);
  const passed = hostile.maxHealth > before.health && hostile.attack > before.attack && hostile.defense > before.defense;
  return {
    actions: ['land:contested-site'],
    events: ['landing-condition:territory', 'landing-condition:ecology'],
    passed,
    summary: {
      before,
      after: {
        health: hostile.maxHealth,
        attack: hostile.attack,
        defense: hostile.defense
      }
    },
    ...(passed ? {} : {
      reason: 'site conditions did not change encounter pressure'
    })
  };
};
const landingTask = () => {
  const galaxy = createGalaxy(31, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const site = galaxy.sites[galaxy.activeSiteId];
  const state = newRun(galaxy.seed, site.biome);
  const before = {
    salvage: site.salvage,
    supplies: site.supplies,
    gold: state.hero.gold
  };
  const settled = recordGalaxyLanding(galaxy, site.id, state, AUTOPLAY_TASK_FIXTURE_TIME + 1);
  const after = settled.sites[site.id];
  const passed = state.hero.gold > before.gold && after.salvage < before.salvage && after.supplies > before.supplies;
  return {
    actions: ['complete:landing'],
    events: settled.events.map(event => event.kind),
    passed,
    summary: {
      before,
      after: {
        salvage: after.salvage,
        supplies: after.supplies,
        gold: state.hero.gold
      }
    },
    ...(passed ? {} : {
      reason: 'landing settlement did not update the site economy'
    })
  };
};
const sectorClockTask = () => {
  const galaxy = createGalaxy(37, hero(), AUTOPLAY_TASK_FIXTURE_TIME);
  const advanced = advanceGalaxyRouteReckoning(galaxy, 2 * ROUTE_RECKONING_WORLD_TICK_UNITS);
  const passed = advanced.routeReckoning === 2 * ROUTE_RECKONING_WORLD_TICK_UNITS && advanced.lastWorldTick === 2 && advanced.events.length >= galaxy.events.length;
  return {
    actions: ['wait:route-reckoning'],
    events: advanced.events.map(event => event.kind),
    passed,
    summary: {
      routeReckoning: advanced.routeReckoning,
      worldTick: advanced.lastWorldTick,
      eventCount: advanced.events.length
    },
    ...(passed ? {} : {
      reason: 'Route Reckoning did not advance deterministically'
    })
  };
};
export const autoplayTaskCatalog = () => [task('tactical.core-loop', 'tactical', 100, 'ui.core-loop', tacticalTask), task('voyager.site-discovery', 'voyager', 90, 'ui.sector-navigation', discoveryTask, ['tactical.core-loop']), task('voyager.transit-window', 'voyager', 85, 'ui.transit', transitTask, ['voyager.site-discovery']), task('voyager.contract-delivery', 'economy', 80, 'ui.contract-delivery', cargoDeliveryTask, ['voyager.site-discovery']), task('voyager.cargo-recovery', 'economy', 75, 'ui.cargo-recovery', cargoRecoveryTask, ['voyager.contract-delivery']), task('voyager.sealed-package-intact', 'economy', 74, 'ui.sealed-package-intact', sealedPackageIntactTask, ['voyager.site-discovery']), task('voyager.sealed-package-tampered', 'economy', 73, 'ui.sealed-package-tampered', sealedPackageTamperedTask, ['voyager.sealed-package-intact']), task('voyager.sealed-package-recovery', 'lifecycle', 72, 'ui.sealed-package-recovery', sealedPackageRecoveryTask, ['voyager.sealed-package-tampered']), task('voyager.landing-conditions', 'ecology', 70, 'ui.landing-conditions', ecologyTask, ['voyager.site-discovery']), task('voyager.landing-settlement', 'lifecycle', 65, 'ui.landing-settlement', landingTask, ['voyager.landing-conditions']), task('voyager.sector-clock', 'lifecycle', 60, 'ui.sector-clock', sectorClockTask, ['voyager.landing-settlement'])];
export const assertAutoplayTaskCatalog = (catalog = autoplayTaskCatalog()) => {
  const ids = new Set();
  for (const entry of catalog) {
    if (ids.has(entry.id)) throw new Error(`duplicate autoplay task id: ${entry.id}`);
    ids.add(entry.id);
    if (!entry.uiTaskId) throw new Error(`autoplay task ${entry.id} lacks browser coverage id`);
  }
  for (const entry of catalog) for (const prerequisite of entry.prerequisites) if (!ids.has(prerequisite)) throw new Error(`autoplay task ${entry.id} has unknown prerequisite ${prerequisite}`);
};
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJydW5BdXRvcGxheSIsImFiYW5kb25HYWxheHlDYXJnbyIsImFjY2VwdEdhbGF4eUNvbnRyYWN0IiwiYWNjZXB0U2VhbGVkUGFja2FnZUNvbnRyYWN0IiwiYWR2YW5jZUdhbGF4eVJvdXRlUmVja29uaW5nIiwiYWR2YW5jZVRyYW5zaXRXaW5kb3ciLCJhcHBseUdhbGF4eVNpdGVDb25kaXRpb25zIiwiY3JlYXRlR2FsYXh5IiwiZGVsaXZlckdhbGF4eUNvbnRyYWN0cyIsImRlbGl2ZXJTZWFsZWRQYWNrYWdlIiwiZGlzY292ZXJMaW5rZWRTaXRlcyIsImxvc2VHYWxheHlDb3VyaWVyIiwibG9zZVNlYWxlZFBhY2thZ2VzRm9yQ291cmllciIsIm1hcmtTZWFsZWRQYWNrYWdlRGVzdGluYXRpb25SZWFjaGVkIiwibmV3SGVybyIsIm5ld1J1biIsIm5ld1RyYW5zaXRSdW4iLCJyZWNvcmRHYWxheHlMYW5kaW5nIiwicmVjb3ZlckdhbGF4eVJvdXRlQ2FjaGVzIiwicmVjb3ZlclNlYWxlZFBhY2thZ2VSb3V0ZUNhY2hlcyIsIlJPVVRFX1JFQ0tPTklOR19XT1JMRF9USUNLX1VOSVRTIiwidmlvbGF0ZVNlYWxlZFBhY2thZ2VTZWFsIiwiQVVUT1BMQVlfVEFTS19DQVRBTE9HX1ZFUlNJT04iLCJBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSIsInRhc2siLCJpZCIsImNhdGVnb3J5IiwicHJpb3JpdHkiLCJ1aVRhc2tJZCIsInJ1biIsInByZXJlcXVpc2l0ZXMiLCJoZXJvIiwibmFtZSIsImxpbmsiLCJvcmlnaW4iLCJkZXN0aW5hdGlvbiIsInNvcnQiLCJqb2luIiwidGFjdGljYWxUYXNrIiwicmVwb3J0IiwibW9kZSIsInBvbGljeSIsInR1cm5MaW1pdCIsImNoYWluQXJlYXMiLCJjaGFpbkZsb29ycyIsImNhcHR1cmVUcmFjZSIsImFjdGlvbnMiLCJjb21tYW5kcyIsImV2ZW50cyIsIk9iamVjdCIsImtleXMiLCJtZXRyaWNzIiwiZXZlbnRPdXRjb21lcyIsInBhc3NlZCIsImxlbmd0aCIsIm91dGNvbWUiLCJzdW1tYXJ5IiwidHVybnMiLCJmaW5nZXJwcmludCIsImVycm9yIiwicmVhc29uIiwiZGlzY292ZXJ5VGFzayIsImdhbGF4eSIsImFjdGl2ZVNpdGVJZCIsInN1cnZleWVkIiwibGlua3MiLCJzaXRlcyIsImNvbXBsZXRlZCIsImV2ZXJ5IiwiX3N1cnZleWVkJHNpdGVzJGlkIiwiZGlzY292ZXJlZCIsIm1hcCIsImV2ZW50Iiwia2luZCIsImRpc2NvdmVyZWRMaW5rcyIsImZpbHRlciIsIl9zdXJ2ZXllZCRzaXRlcyRpZDIiLCJ0cmFuc2l0VGFzayIsIl9zdGF0ZSR0cmF2ZWwiLCJfc3RhdGUkdHJhdmVsMiIsIl9zdGF0ZSR0cmF2ZWwzIiwidHJhdmVsIiwidmVyc2lvbiIsImZyb21TaXRlSWQiLCJ0b1NpdGVJZCIsImxpbmtJZCIsImNodW5rQ291bnQiLCJyZXNpZGVudFN0YXJ0IiwiYWN0aXZlQ2h1bmsiLCJzaXR1YXRpb25zIiwic3RhdGUiLCJjaHVua1dpZHRoIiwiZmxvb3IiLCJ3aWR0aCIsIngiLCJhZHZhbmNlZCIsImFjdG9ycyIsImFjdG9yIiwic3RhcnRzV2l0aCIsImNhcmdvRGVsaXZlcnlUYXNrIiwiYWNjZXB0ZWQiLCJjb3VyaWVyIiwiZGVsaXZlcmVkIiwiY29udHJhY3RzIiwic29tZSIsImNvbnRyYWN0Iiwic3RhdHVzIiwiZ29sZCIsImNhcmdvIiwiY29udHJhY3RTdGF0ZXMiLCJjYXJnb1JlY292ZXJ5VGFzayIsInJvdXRlIiwiYWJhbmRvbmVkIiwicmVjb3ZlcmVkIiwicm91dGVDYWNoZXMiLCJjYWNoZSIsImNhY2hlcyIsInNlYWxlZFBhY2thZ2VJbnRhY3RUYXNrIiwiX2RlbGl2ZXJlZCRnYWxheHkkc2VhIiwiX2RlbGl2ZXJlZCRnYWxheHkkc2VhMiIsInNlYWxlZFBhY2thZ2VDb250cmFjdHMiLCJhY3RpdmVDb3VyaWVySWQiLCJhcnJpdmVkIiwidGVybXMiLCJkZXN0aW5hdGlvblNpdGVJZCIsInBhY2thZ2VSZWNvcmQiLCJzZWFsZWRQYWNrYWdlcyIsImZpbmQiLCJjYW5kaWRhdGUiLCJjb250cmFjdElkIiwiY2hhbmdlZCIsInNlYWxTdGF0ZSIsImN1c3RvZHkiLCJwYXltZW50IiwiZ2VuZXJhbE1hbmlmZXN0IiwiZW50cmllcyIsImVudHJ5IiwiY29udHJhY3RTdGF0dXMiLCJzZWFsIiwic2VhbGVkUGFja2FnZVRhbXBlcmVkVGFzayIsIl9wYWNrYWdlUmVjb3JkJHJldmVhbCIsIm9wZW5lZCIsImV4cGVjdGVkUGF5bWVudCIsImNvbGxhdGVyYWwiLCJyZXZlYWxlZENvbnRlbnRzIiwidW5kZWZpbmVkIiwicmV2ZWFsZWQiLCJzZWFsZWRQYWNrYWdlUmVjb3ZlcnlUYXNrIiwiX3JlY292ZXJlZCRnYWxheHkkc2VhIiwiX3JlY292ZXJlZCRnYWxheHkkc2VhMiIsImNhY2hlZCIsImFmdGVyRGVhdGgiLCJlY29sb2d5VGFzayIsInNpdGUiLCJjb250cm9sIiwiaW50ZWdyaXR5IiwiZWNvbG9neSIsInNlZWQiLCJiaW9tZSIsImhvc3RpbGUiLCJiZWZvcmUiLCJoZWFsdGgiLCJtYXhIZWFsdGgiLCJhdHRhY2siLCJkZWZlbnNlIiwiYWZ0ZXIiLCJsYW5kaW5nVGFzayIsInNhbHZhZ2UiLCJzdXBwbGllcyIsInNldHRsZWQiLCJzZWN0b3JDbG9ja1Rhc2siLCJyb3V0ZVJlY2tvbmluZyIsImxhc3RXb3JsZFRpY2siLCJ3b3JsZFRpY2siLCJldmVudENvdW50IiwiYXV0b3BsYXlUYXNrQ2F0YWxvZyIsImFzc2VydEF1dG9wbGF5VGFza0NhdGFsb2ciLCJjYXRhbG9nIiwiaWRzIiwiU2V0IiwiaGFzIiwiRXJyb3IiLCJhZGQiLCJwcmVyZXF1aXNpdGUiXSwic291cmNlcyI6WyJhdXRvcGxheS10YXNrLWNhdGFsb2cudHMiXSwic291cmNlc0NvbnRlbnQiOlsiaW1wb3J0IHsgcnVuQXV0b3BsYXkgfSBmcm9tICcuL2F1dG9wbGF5LXJ1bm5lcidcbmltcG9ydCB7IGFiYW5kb25HYWxheHlDYXJnbywgYWNjZXB0R2FsYXh5Q29udHJhY3QsIGFjY2VwdFNlYWxlZFBhY2thZ2VDb250cmFjdCwgYWR2YW5jZUdhbGF4eVJvdXRlUmVja29uaW5nLCBhZHZhbmNlVHJhbnNpdFdpbmRvdywgYXBwbHlHYWxheHlTaXRlQ29uZGl0aW9ucywgY3JlYXRlR2FsYXh5LCBkZWxpdmVyR2FsYXh5Q29udHJhY3RzLCBkZWxpdmVyU2VhbGVkUGFja2FnZSwgZGlzY292ZXJMaW5rZWRTaXRlcywgbG9zZUdhbGF4eUNvdXJpZXIsIGxvc2VTZWFsZWRQYWNrYWdlc0ZvckNvdXJpZXIsIG1hcmtTZWFsZWRQYWNrYWdlRGVzdGluYXRpb25SZWFjaGVkLCBuZXdIZXJvLCBuZXdSdW4sIG5ld1RyYW5zaXRSdW4sIHJlY29yZEdhbGF4eUxhbmRpbmcsIHJlY292ZXJHYWxheHlSb3V0ZUNhY2hlcywgcmVjb3ZlclNlYWxlZFBhY2thZ2VSb3V0ZUNhY2hlcywgUk9VVEVfUkVDS09OSU5HX1dPUkxEX1RJQ0tfVU5JVFMsIHZpb2xhdGVTZWFsZWRQYWNrYWdlU2VhbCB9IGZyb20gJy4vZW5naW5lJ1xuXG5leHBvcnQgY29uc3QgQVVUT1BMQVlfVEFTS19DQVRBTE9HX1ZFUlNJT04gPSAxXG5leHBvcnQgY29uc3QgQVVUT1BMQVlfVEFTS19GSVhUVVJFX1RJTUUgPSAxXzczNV82ODlfNjAwXzAwMFxuXG5leHBvcnQgdHlwZSBBdXRvcGxheVRhc2tDYXRlZ29yeSA9ICd0YWN0aWNhbCcgfCAndm95YWdlcicgfCAnZWNvbm9teScgfCAnZWNvbG9neScgfCAnbGlmZWN5Y2xlJ1xuZXhwb3J0IHR5cGUgQXV0b3BsYXlUYXNrU3RhdHVzID0gJ3Bhc3NlZCcgfCAnZmFpbGVkJyB8ICdibG9ja2VkJyB8ICd0aW1lZC1vdXQnXG5cbmV4cG9ydCBpbnRlcmZhY2UgQXV0b3BsYXlUYXNrRXhlY3V0aW9uIHtcbiAgYWN0aW9uczogc3RyaW5nW11cbiAgZXZlbnRzOiBzdHJpbmdbXVxuICBwYXNzZWQ6IGJvb2xlYW5cbiAgc3VtbWFyeTogUmVjb3JkPHN0cmluZywgdW5rbm93bj5cbiAgcmVhc29uPzogc3RyaW5nXG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgQXV0b3BsYXlUYXNrIHtcbiAgaWQ6IHN0cmluZ1xuICBjYXRlZ29yeTogQXV0b3BsYXlUYXNrQ2F0ZWdvcnlcbiAgcHJpb3JpdHk6IG51bWJlclxuICBwcmVyZXF1aXNpdGVzOiByZWFkb25seSBzdHJpbmdbXVxuICB1aVRhc2tJZDogc3RyaW5nXG4gIHJ1bigpOiBBdXRvcGxheVRhc2tFeGVjdXRpb25cbn1cblxuY29uc3QgdGFzayA9IChpZDogc3RyaW5nLCBjYXRlZ29yeTogQXV0b3BsYXlUYXNrQ2F0ZWdvcnksIHByaW9yaXR5OiBudW1iZXIsIHVpVGFza0lkOiBzdHJpbmcsIHJ1bjogKCkgPT4gQXV0b3BsYXlUYXNrRXhlY3V0aW9uLCBwcmVyZXF1aXNpdGVzOiByZWFkb25seSBzdHJpbmdbXSA9IFtdKTogQXV0b3BsYXlUYXNrID0+ICh7IGlkLCBjYXRlZ29yeSwgcHJpb3JpdHksIHByZXJlcXVpc2l0ZXMsIHVpVGFza0lkLCBydW4gfSlcbmNvbnN0IGhlcm8gPSAoKSA9PiBuZXdIZXJvKHsgbmFtZTogJ1Rhc2sgQ291cmllcicgfSlcbmNvbnN0IGxpbmsgPSAob3JpZ2luOiBzdHJpbmcsIGRlc3RpbmF0aW9uOiBzdHJpbmcpID0+IFtvcmlnaW4sIGRlc3RpbmF0aW9uXS5zb3J0KCkuam9pbignOjonKVxuXG5jb25zdCB0YWN0aWNhbFRhc2sgPSAoKTogQXV0b3BsYXlUYXNrRXhlY3V0aW9uID0+IHtcbiAgY29uc3QgcmVwb3J0ID0gcnVuQXV0b3BsYXkobmV3UnVuKDcsICdtaW5lJyksIHsgbW9kZTogJ29tbmlzY2llbnQnLCBwb2xpY3k6ICdjbGVhcicsIHR1cm5MaW1pdDogOTYsIGNoYWluQXJlYXM6IGZhbHNlLCBjaGFpbkZsb29yczogZmFsc2UsIGNhcHR1cmVUcmFjZTogZmFsc2UgfSlcbiAgcmV0dXJuIHtcbiAgICBhY3Rpb25zOiByZXBvcnQuY29tbWFuZHMsXG4gICAgZXZlbnRzOiBPYmplY3Qua2V5cyhyZXBvcnQubWV0cmljcy5ldmVudE91dGNvbWVzKS5zb3J0KCksXG4gICAgcGFzc2VkOiByZXBvcnQuY29tbWFuZHMubGVuZ3RoID4gMCAmJiByZXBvcnQub3V0Y29tZSAhPT0gJ2Vycm9yJyAmJiByZXBvcnQub3V0Y29tZSAhPT0gJ3Vuc3VwcG9ydGVkJyxcbiAgICBzdW1tYXJ5OiB7IG91dGNvbWU6IHJlcG9ydC5vdXRjb21lLCB0dXJuczogcmVwb3J0LnR1cm5zLCBjb21tYW5kczogcmVwb3J0LmNvbW1hbmRzLmxlbmd0aCwgZmluZ2VycHJpbnQ6IHJlcG9ydC5maW5nZXJwcmludCB9LFxuICAgIC4uLihyZXBvcnQuZXJyb3IgPyB7IHJlYXNvbjogcmVwb3J0LmVycm9yIH0gOiB7fSlcbiAgfVxufVxuXG5jb25zdCBkaXNjb3ZlcnlUYXNrID0gKCk6IEF1dG9wbGF5VGFza0V4ZWN1dGlvbiA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNyZWF0ZUdhbGF4eSg5MSwgaGVybygpLCBBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSlcbiAgY29uc3Qgb3JpZ2luID0gZ2FsYXh5LmFjdGl2ZVNpdGVJZFxuICBjb25zdCBzdXJ2ZXllZCA9IGRpc2NvdmVyTGlua2VkU2l0ZXMoZ2FsYXh5LCBvcmlnaW4sIEFVVE9QTEFZX1RBU0tfRklYVFVSRV9USU1FICsgMSlcbiAgY29uc3QgbGlua3MgPSBzdXJ2ZXllZC5zaXRlc1tvcmlnaW5dIS5saW5rc1xuICBjb25zdCBwYXNzZWQgPSBzdXJ2ZXllZC5zaXRlc1tvcmlnaW5dIS5jb21wbGV0ZWQgJiYgbGlua3MubGVuZ3RoID4gMCAmJiBsaW5rcy5ldmVyeShpZCA9PiBzdXJ2ZXllZC5zaXRlc1tpZF0/LmRpc2NvdmVyZWQpXG4gIHJldHVybiB7IGFjdGlvbnM6IFsnb3BlcmF0ZTpicmlkZ2UtY29uc29sZSddLCBldmVudHM6IHN1cnZleWVkLmV2ZW50cy5tYXAoZXZlbnQgPT4gZXZlbnQua2luZCksIHBhc3NlZCwgc3VtbWFyeTogeyBvcmlnaW4sIGRpc2NvdmVyZWRMaW5rczogbGlua3MuZmlsdGVyKGlkID0+IHN1cnZleWVkLnNpdGVzW2lkXT8uZGlzY292ZXJlZCkuc29ydCgpIH0sIC4uLihwYXNzZWQgPyB7fSA6IHsgcmVhc29uOiAnc3VydmV5IGRpZCBub3QgcmV2ZWFsIGV2ZXJ5IHBoeXNpY2FsIGxpbmsnIH0pIH1cbn1cblxuY29uc3QgdHJhbnNpdFRhc2sgPSAoKTogQXV0b3BsYXlUYXNrRXhlY3V0aW9uID0+IHtcbiAgY29uc3QgdHJhdmVsID0geyB2ZXJzaW9uOiAxIGFzIGNvbnN0LCBmcm9tU2l0ZUlkOiAnc2VjdG9yLTAwOnNpdGUtMDAnLCB0b1NpdGVJZDogJ3NlY3Rvci0wMDpzaXRlLTAxJywgbGlua0lkOiAnc2VjdG9yLTAwOnNpdGUtMDA6OnNlY3Rvci0wMDpzaXRlLTAxJywgY2h1bmtDb3VudDogNSwgcmVzaWRlbnRTdGFydDogMCwgYWN0aXZlQ2h1bms6IDAsIHNpdHVhdGlvbnM6IFsncXVpZXQnLCAncGF0cm9sJywgJ2hhemFyZCcsICd0cmFkZXInLCAnZWNvbG9neSddIGFzIGNvbnN0IH1cbiAgY29uc3Qgc3RhdGUgPSBuZXdUcmFuc2l0UnVuKDcxLCAnd2lsZHMnLCBoZXJvKCksIHRyYXZlbClcbiAgY29uc3QgY2h1bmtXaWR0aCA9IHN0YXRlLmZsb29yLndpZHRoIC8gM1xuICBzdGF0ZS5oZXJvLnggPSBjaHVua1dpZHRoICogMlxuICBjb25zdCBhZHZhbmNlZCA9IGFkdmFuY2VUcmFuc2l0V2luZG93KHN0YXRlKVxuICBjb25zdCBwYXNzZWQgPSBhZHZhbmNlZCAmJiBzdGF0ZS50cmF2ZWw/LnJlc2lkZW50U3RhcnQgPT09IDEgJiYgc3RhdGUudHJhdmVsLnNpdHVhdGlvbnMubGVuZ3RoID09PSA1XG4gIHJldHVybiB7IGFjdGlvbnM6IFsnd2Fsazpjb25uZWN0b3Itd2luZG93J10sIGV2ZW50czogc3RhdGUuZmxvb3IuYWN0b3JzLm1hcChhY3RvciA9PiBhY3Rvci5pZCkuZmlsdGVyKGlkID0+IGlkLnN0YXJ0c1dpdGgoJ3JvdXRlLScpKS5zb3J0KCksIHBhc3NlZCwgc3VtbWFyeTogeyByZXNpZGVudFN0YXJ0OiBzdGF0ZS50cmF2ZWw/LnJlc2lkZW50U3RhcnQsIHNpdHVhdGlvbnM6IHN0YXRlLnRyYXZlbD8uc2l0dWF0aW9ucyB9LCAuLi4ocGFzc2VkID8ge30gOiB7IHJlYXNvbjogJ3Jlc2lkZW50IHRyYW5zaXQgd2luZG93IGRpZCBub3QgYWR2YW5jZScgfSkgfVxufVxuXG5jb25zdCBjYXJnb0RlbGl2ZXJ5VGFzayA9ICgpOiBBdXRvcGxheVRhc2tFeGVjdXRpb24gPT4ge1xuICBjb25zdCBnYWxheHkgPSBjcmVhdGVHYWxheHkoMTksIGhlcm8oKSwgQVVUT1BMQVlfVEFTS19GSVhUVVJFX1RJTUUpXG4gIGNvbnN0IG9yaWdpbiA9IGdhbGF4eS5hY3RpdmVTaXRlSWRcbiAgY29uc3QgZGVzdGluYXRpb24gPSBnYWxheHkuc2l0ZXNbb3JpZ2luXSEubGlua3NbMF0hXG4gIGNvbnN0IHN1cnZleWVkID0gZGlzY292ZXJMaW5rZWRTaXRlcyhnYWxheHksIG9yaWdpbiwgQVVUT1BMQVlfVEFTS19GSVhUVVJFX1RJTUUgKyAxKVxuICBjb25zdCBhY2NlcHRlZCA9IGFjY2VwdEdhbGF4eUNvbnRyYWN0KHN1cnZleWVkLCBvcmlnaW4sIGRlc3RpbmF0aW9uLCBBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSArIDIpXG4gIGNvbnN0IGNvdXJpZXIgPSBoZXJvKClcbiAgY29uc3QgZGVsaXZlcmVkID0gZGVsaXZlckdhbGF4eUNvbnRyYWN0cyhhY2NlcHRlZC5nYWxheHksIGRlc3RpbmF0aW9uLCBjb3VyaWVyLCBBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSArIDMpXG4gIGNvbnN0IHBhc3NlZCA9IGFjY2VwdGVkLmdhbGF4eS5jb250cmFjdHMuc29tZShjb250cmFjdCA9PiBjb250cmFjdC5zdGF0dXMgPT09ICdhY3RpdmUnKSAmJiBkZWxpdmVyZWQuZ2FsYXh5LmNvbnRyYWN0cy5zb21lKGNvbnRyYWN0ID0+IGNvbnRyYWN0LnN0YXR1cyA9PT0gJ2NvbXBsZXRlZCcpICYmIGNvdXJpZXIuZ29sZCA+IDBcbiAgcmV0dXJuIHsgYWN0aW9uczogWydvcGVyYXRlOm9yaWdpbi1haXJsb2NrJywgJ29wZXJhdGU6ZGVzdGluYXRpb24tYWlybG9jayddLCBldmVudHM6IGRlbGl2ZXJlZC5nYWxheHkuZXZlbnRzLm1hcChldmVudCA9PiBldmVudC5raW5kKSwgcGFzc2VkLCBzdW1tYXJ5OiB7IGdvbGQ6IGNvdXJpZXIuZ29sZCwgY2FyZ286IGRlbGl2ZXJlZC5nYWxheHkuY2FyZ28sIGNvbnRyYWN0U3RhdGVzOiBkZWxpdmVyZWQuZ2FsYXh5LmNvbnRyYWN0cy5tYXAoY29udHJhY3QgPT4gY29udHJhY3Quc3RhdHVzKSB9LCAuLi4ocGFzc2VkID8ge30gOiB7IHJlYXNvbjogJ2NvbnRyYWN0IGNhcmdvIHdhcyBub3QgZGVsaXZlcmVkJyB9KSB9XG59XG5cbmNvbnN0IGNhcmdvUmVjb3ZlcnlUYXNrID0gKCk6IEF1dG9wbGF5VGFza0V4ZWN1dGlvbiA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNyZWF0ZUdhbGF4eSgyMywgaGVybygpLCBBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSlcbiAgY29uc3Qgb3JpZ2luID0gZ2FsYXh5LmFjdGl2ZVNpdGVJZFxuICBjb25zdCBkZXN0aW5hdGlvbiA9IGdhbGF4eS5zaXRlc1tvcmlnaW5dIS5saW5rc1swXSFcbiAgY29uc3Qgc3VydmV5ZWQgPSBkaXNjb3ZlckxpbmtlZFNpdGVzKGdhbGF4eSwgb3JpZ2luLCBBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSArIDEpXG4gIGNvbnN0IGFjY2VwdGVkID0gYWNjZXB0R2FsYXh5Q29udHJhY3Qoc3VydmV5ZWQsIG9yaWdpbiwgZGVzdGluYXRpb24sIEFVVE9QTEFZX1RBU0tfRklYVFVSRV9USU1FICsgMilcbiAgY29uc3Qgcm91dGUgPSBsaW5rKG9yaWdpbiwgZGVzdGluYXRpb24pXG4gIGNvbnN0IGFiYW5kb25lZCA9IGFiYW5kb25HYWxheHlDYXJnbyhhY2NlcHRlZC5nYWxheHksIHJvdXRlLCAxLCBBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSArIDMpXG4gIGNvbnN0IHJlY292ZXJlZCA9IHJlY292ZXJHYWxheHlSb3V0ZUNhY2hlcyhhYmFuZG9uZWQsIHJvdXRlKVxuICBjb25zdCBwYXNzZWQgPSBhYmFuZG9uZWQucm91dGVDYWNoZXMuc29tZShjYWNoZSA9PiAhY2FjaGUucmVjb3ZlcmVkKSAmJiByZWNvdmVyZWQuZ2FsYXh5LnJvdXRlQ2FjaGVzLmV2ZXJ5KGNhY2hlID0+IGNhY2hlLnJlY292ZXJlZCkgJiYgcmVjb3ZlcmVkLmdhbGF4eS5jYXJnby5sZW5ndGggPiAwXG4gIHJldHVybiB7IGFjdGlvbnM6IFsnYWJhbmRvbjpjYXJnbycsICdvcGVyYXRlOnJvdXRlLWNhY2hlJ10sIGV2ZW50czogcmVjb3ZlcmVkLmdhbGF4eS5ldmVudHMubWFwKGV2ZW50ID0+IGV2ZW50LmtpbmQpLCBwYXNzZWQsIHN1bW1hcnk6IHsgY2FjaGVzOiByZWNvdmVyZWQuZ2FsYXh5LnJvdXRlQ2FjaGVzLm1hcChjYWNoZSA9PiAoeyBpZDogY2FjaGUuaWQsIHJlY292ZXJlZDogY2FjaGUucmVjb3ZlcmVkIH0pKSwgY2FyZ286IHJlY292ZXJlZC5nYWxheHkuY2FyZ28gfSwgLi4uKHBhc3NlZCA/IHt9IDogeyByZWFzb246ICdyZWNvdmVyYWJsZSBjYXJnbyBjYWNoZSB3YXMgbm90IHJlc3RvcmVkJyB9KSB9XG59XG5cbmNvbnN0IHNlYWxlZFBhY2thZ2VJbnRhY3RUYXNrID0gKCk6IEF1dG9wbGF5VGFza0V4ZWN1dGlvbiA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNyZWF0ZUdhbGF4eSg0MywgaGVybygpLCBBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSlcbiAgY29uc3QgY29udHJhY3QgPSBnYWxheHkuc2VhbGVkUGFja2FnZUNvbnRyYWN0c1swXVxuICBpZiAoIWNvbnRyYWN0KSByZXR1cm4geyBhY3Rpb25zOiBbXSwgZXZlbnRzOiBbXSwgcGFzc2VkOiBmYWxzZSwgc3VtbWFyeToge30sIHJlYXNvbjogJ00xIHBhY2thZ2Ugb2ZmZXIgaXMgbWlzc2luZycgfVxuICBjb25zdCBhY2NlcHRlZCA9IGFjY2VwdFNlYWxlZFBhY2thZ2VDb250cmFjdChnYWxheHksIGNvbnRyYWN0LmlkLCBnYWxheHkuYWN0aXZlQ291cmllcklkKVxuICBjb25zdCBjb3VyaWVyID0gaGVybygpXG4gIGNvbnN0IGFycml2ZWQgPSBtYXJrU2VhbGVkUGFja2FnZURlc3RpbmF0aW9uUmVhY2hlZChhY2NlcHRlZC5nYWxheHksIGNvbnRyYWN0LnRlcm1zLmRlc3RpbmF0aW9uU2l0ZUlkKVxuICBjb25zdCBkZWxpdmVyZWQgPSBkZWxpdmVyU2VhbGVkUGFja2FnZShhcnJpdmVkLCBjb250cmFjdC5pZCwgY291cmllcilcbiAgY29uc3QgcGFja2FnZVJlY29yZCA9IGRlbGl2ZXJlZC5nYWxheHkuc2VhbGVkUGFja2FnZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmNvbnRyYWN0SWQgPT09IGNvbnRyYWN0LmlkKVxuICBjb25zdCBwYXNzZWQgPSBkZWxpdmVyZWQuY2hhbmdlZCAmJiBkZWxpdmVyZWQuZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHNbMF0/LnN0YXR1cyA9PT0gJ2NvbXBsZXRlZCcgJiYgcGFja2FnZVJlY29yZD8uc2VhbFN0YXRlID09PSAnaW50YWN0JyAmJiBwYWNrYWdlUmVjb3JkLmN1c3RvZHkgPT09ICdyZWNpcGllbnQnICYmIGNvdXJpZXIuZ29sZCA9PT0gY29udHJhY3QudGVybXMucGF5bWVudFxuICByZXR1cm4geyBhY3Rpb25zOiBbJ2luc3BlY3Q6ZXh0ZXJpb3InLCAnYWNjZXB0OmFzc2lnbmVkLWNvdXJpZXInLCAnbGFuZDpLZXN0cmVsJywgJ3NldHRsZTppbnRhY3QnXSwgZXZlbnRzOiBkZWxpdmVyZWQuZ2FsYXh5LmdlbmVyYWxNYW5pZmVzdC5lbnRyaWVzLm1hcChlbnRyeSA9PiBlbnRyeS5raW5kKSwgcGFzc2VkLCBzdW1tYXJ5OiB7IGNvbnRyYWN0U3RhdHVzOiBkZWxpdmVyZWQuZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHNbMF0/LnN0YXR1cywgc2VhbDogcGFja2FnZVJlY29yZD8uc2VhbFN0YXRlLCBjdXN0b2R5OiBwYWNrYWdlUmVjb3JkPy5jdXN0b2R5LCBnb2xkOiBjb3VyaWVyLmdvbGQgfSwgLi4uKHBhc3NlZCA/IHt9IDogeyByZWFzb246ICdpbnRhY3Qgc2VhbGVkIHBhY2thZ2UgZGlkIG5vdCByZXF1aXJlIGV4cGxpY2l0IHNldHRsZW1lbnQnIH0pIH1cbn1cblxuY29uc3Qgc2VhbGVkUGFja2FnZVRhbXBlcmVkVGFzayA9ICgpOiBBdXRvcGxheVRhc2tFeGVjdXRpb24gPT4ge1xuICBjb25zdCBnYWxheHkgPSBjcmVhdGVHYWxheHkoNDcsIGhlcm8oKSwgQVVUT1BMQVlfVEFTS19GSVhUVVJFX1RJTUUpXG4gIGNvbnN0IGNvbnRyYWN0ID0gZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHNbMF1cbiAgaWYgKCFjb250cmFjdCkgcmV0dXJuIHsgYWN0aW9uczogW10sIGV2ZW50czogW10sIHBhc3NlZDogZmFsc2UsIHN1bW1hcnk6IHt9LCByZWFzb246ICdNMSBwYWNrYWdlIG9mZmVyIGlzIG1pc3NpbmcnIH1cbiAgY29uc3QgYWNjZXB0ZWQgPSBhY2NlcHRTZWFsZWRQYWNrYWdlQ29udHJhY3QoZ2FsYXh5LCBjb250cmFjdC5pZCwgZ2FsYXh5LmFjdGl2ZUNvdXJpZXJJZClcbiAgY29uc3Qgb3BlbmVkID0gdmlvbGF0ZVNlYWxlZFBhY2thZ2VTZWFsKGFjY2VwdGVkLmdhbGF4eSwgY29udHJhY3QuaWQpXG4gIGNvbnN0IGNvdXJpZXIgPSBoZXJvKClcbiAgY29uc3QgZGVsaXZlcmVkID0gZGVsaXZlclNlYWxlZFBhY2thZ2UobWFya1NlYWxlZFBhY2thZ2VEZXN0aW5hdGlvblJlYWNoZWQob3BlbmVkLmdhbGF4eSwgY29udHJhY3QudGVybXMuZGVzdGluYXRpb25TaXRlSWQpLCBjb250cmFjdC5pZCwgY291cmllcilcbiAgY29uc3QgcGFja2FnZVJlY29yZCA9IGRlbGl2ZXJlZC5nYWxheHkuc2VhbGVkUGFja2FnZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmNvbnRyYWN0SWQgPT09IGNvbnRyYWN0LmlkKVxuICBjb25zdCBleHBlY3RlZFBheW1lbnQgPSBjb250cmFjdC50ZXJtcy5wYXltZW50IC0gY29udHJhY3QudGVybXMuY29sbGF0ZXJhbFxuICBjb25zdCBwYXNzZWQgPSBkZWxpdmVyZWQuY2hhbmdlZCAmJiBwYWNrYWdlUmVjb3JkPy5zZWFsU3RhdGUgPT09ICdvcGVuZWQnICYmIHBhY2thZ2VSZWNvcmQucmV2ZWFsZWRDb250ZW50cyAhPT0gdW5kZWZpbmVkICYmIGNvdXJpZXIuZ29sZCA9PT0gZXhwZWN0ZWRQYXltZW50XG4gIHJldHVybiB7IGFjdGlvbnM6IFsnYWNjZXB0OmFzc2lnbmVkLWNvdXJpZXInLCAnY29uZmlybTpvcGVuLXNlYWwnLCAnbGFuZDpLZXN0cmVsJywgJ3NldHRsZTp0YW1wZXJlZCddLCBldmVudHM6IGRlbGl2ZXJlZC5nYWxheHkuZ2VuZXJhbE1hbmlmZXN0LmVudHJpZXMubWFwKGVudHJ5ID0+IGVudHJ5LmtpbmQpLCBwYXNzZWQsIHN1bW1hcnk6IHsgc2VhbDogcGFja2FnZVJlY29yZD8uc2VhbFN0YXRlLCByZXZlYWxlZDogcGFja2FnZVJlY29yZD8ucmV2ZWFsZWRDb250ZW50cz8uaWQsIGdvbGQ6IGNvdXJpZXIuZ29sZCwgZXhwZWN0ZWRQYXltZW50IH0sIC4uLihwYXNzZWQgPyB7fSA6IHsgcmVhc29uOiAndGFtcGVyZWQgcGFja2FnZSBkaWQgbm90IGRpc2Nsb3NlIGNvbnRlbnRzIGFuZCBhcHBseSBjb2xsYXRlcmFsIHNldHRsZW1lbnQnIH0pIH1cbn1cblxuY29uc3Qgc2VhbGVkUGFja2FnZVJlY292ZXJ5VGFzayA9ICgpOiBBdXRvcGxheVRhc2tFeGVjdXRpb24gPT4ge1xuICBjb25zdCBnYWxheHkgPSBjcmVhdGVHYWxheHkoNTMsIGhlcm8oKSwgQVVUT1BMQVlfVEFTS19GSVhUVVJFX1RJTUUpXG4gIGNvbnN0IGNvbnRyYWN0ID0gZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHNbMF1cbiAgaWYgKCFjb250cmFjdCkgcmV0dXJuIHsgYWN0aW9uczogW10sIGV2ZW50czogW10sIHBhc3NlZDogZmFsc2UsIHN1bW1hcnk6IHt9LCByZWFzb246ICdNMSBwYWNrYWdlIG9mZmVyIGlzIG1pc3NpbmcnIH1cbiAgY29uc3QgYWNjZXB0ZWQgPSBhY2NlcHRTZWFsZWRQYWNrYWdlQ29udHJhY3QoZ2FsYXh5LCBjb250cmFjdC5pZCwgZ2FsYXh5LmFjdGl2ZUNvdXJpZXJJZClcbiAgY29uc3QgY2FjaGVkID0gbG9zZVNlYWxlZFBhY2thZ2VzRm9yQ291cmllcihhY2NlcHRlZC5nYWxheHksIGFjY2VwdGVkLmdhbGF4eS5hY3RpdmVDb3VyaWVySWQsICdNMS1LZXN0cmVsLWNvbm5lY3RvcicsIDEpXG4gIGNvbnN0IGFmdGVyRGVhdGggPSBsb3NlR2FsYXh5Q291cmllcihjYWNoZWQsIGNhY2hlZC5hY3RpdmVDb3VyaWVySWQsICd0YXNrIGZpeHR1cmUgY291cmllciBsb3NzJywgQVVUT1BMQVlfVEFTS19GSVhUVVJFX1RJTUUgKyAxKVxuICBjb25zdCByZWNvdmVyZWQgPSByZWNvdmVyU2VhbGVkUGFja2FnZVJvdXRlQ2FjaGVzKGFmdGVyRGVhdGgsICdNMS1LZXN0cmVsLWNvbm5lY3RvcicpXG4gIGNvbnN0IHBhY2thZ2VSZWNvcmQgPSByZWNvdmVyZWQuZ2FsYXh5LnNlYWxlZFBhY2thZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5jb250cmFjdElkID09PSBjb250cmFjdC5pZClcbiAgY29uc3QgcGFzc2VkID0gcmVjb3ZlcmVkLmNoYW5nZWQgJiYgcmVjb3ZlcmVkLmdhbGF4eS5zZWFsZWRQYWNrYWdlQ29udHJhY3RzWzBdPy5zdGF0dXMgPT09ICdmYWlsZWQnICYmIHBhY2thZ2VSZWNvcmQ/LmN1c3RvZHkgPT09ICdhdEpvbW9uJyAmJiBwYWNrYWdlUmVjb3JkLnNlYWxTdGF0ZSA9PT0gJ2ludGFjdCcgJiYgcmVjb3ZlcmVkLmdhbGF4eS5nZW5lcmFsTWFuaWZlc3QuZW50cmllcy5zb21lKGVudHJ5ID0+IGVudHJ5LmtpbmQgPT09ICdwYWNrYWdlUmVjb3ZlcmVkJylcbiAgcmV0dXJuIHsgYWN0aW9uczogWydhY2NlcHQ6YXNzaWduZWQtY291cmllcicsICdkZWF0aDpjb25uZWN0b3InLCAnc2VsZWN0OnJlcGxhY2VtZW50LWNvdXJpZXInLCAnb3BlcmF0ZTpyb3V0ZS1jYWNoZSddLCBldmVudHM6IHJlY292ZXJlZC5nYWxheHkuZ2VuZXJhbE1hbmlmZXN0LmVudHJpZXMubWFwKGVudHJ5ID0+IGVudHJ5LmtpbmQpLCBwYXNzZWQsIHN1bW1hcnk6IHsgY29udHJhY3RTdGF0dXM6IHJlY292ZXJlZC5nYWxheHkuc2VhbGVkUGFja2FnZUNvbnRyYWN0c1swXT8uc3RhdHVzLCBzZWFsOiBwYWNrYWdlUmVjb3JkPy5zZWFsU3RhdGUsIGN1c3RvZHk6IHBhY2thZ2VSZWNvcmQ/LmN1c3RvZHkgfSwgLi4uKHBhc3NlZCA/IHt9IDogeyByZWFzb246ICdjb3VyaWVyIGxvc3MgZGlkIG5vdCBwcmVzZXJ2ZSBhbiBpbnRhY3QgcmVjb3ZlcmFibGUgc2VhbGVkIHBhY2thZ2UnIH0pIH1cbn1cblxuY29uc3QgZWNvbG9neVRhc2sgPSAoKTogQXV0b3BsYXlUYXNrRXhlY3V0aW9uID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY3JlYXRlR2FsYXh5KDI5LCBoZXJvKCksIEFVVE9QTEFZX1RBU0tfRklYVFVSRV9USU1FKVxuICBjb25zdCBzaXRlID0gZ2FsYXh5LnNpdGVzW2dhbGF4eS5hY3RpdmVTaXRlSWRdIVxuICBzaXRlLmNvbnRyb2wgPSAndm9pZGJvcm4nXG4gIHNpdGUuaW50ZWdyaXR5ID0gMjBcbiAgc2l0ZS5lY29sb2d5ID0gMjBcbiAgY29uc3Qgc3RhdGUgPSBuZXdSdW4oZ2FsYXh5LnNlZWQsIHNpdGUuYmlvbWUpXG4gIGNvbnN0IGhvc3RpbGUgPSBzdGF0ZS5mbG9vci5hY3RvcnMuZmluZChhY3RvciA9PiBhY3Rvci5ob3N0aWxlKVxuICBpZiAoIWhvc3RpbGUpIHJldHVybiB7IGFjdGlvbnM6IFtdLCBldmVudHM6IFtdLCBwYXNzZWQ6IGZhbHNlLCBzdW1tYXJ5OiB7fSwgcmVhc29uOiAnZml4dHVyZSBoYXMgbm8gaG9zdGlsZSBhY3RvcicgfVxuICBjb25zdCBiZWZvcmUgPSB7IGhlYWx0aDogaG9zdGlsZS5tYXhIZWFsdGgsIGF0dGFjazogaG9zdGlsZS5hdHRhY2ssIGRlZmVuc2U6IGhvc3RpbGUuZGVmZW5zZSB9XG4gIGFwcGx5R2FsYXh5U2l0ZUNvbmRpdGlvbnMoc3RhdGUsIHNpdGUpXG4gIGNvbnN0IHBhc3NlZCA9IGhvc3RpbGUubWF4SGVhbHRoID4gYmVmb3JlLmhlYWx0aCAmJiBob3N0aWxlLmF0dGFjayA+IGJlZm9yZS5hdHRhY2sgJiYgaG9zdGlsZS5kZWZlbnNlID4gYmVmb3JlLmRlZmVuc2VcbiAgcmV0dXJuIHsgYWN0aW9uczogWydsYW5kOmNvbnRlc3RlZC1zaXRlJ10sIGV2ZW50czogWydsYW5kaW5nLWNvbmRpdGlvbjp0ZXJyaXRvcnknLCAnbGFuZGluZy1jb25kaXRpb246ZWNvbG9neSddLCBwYXNzZWQsIHN1bW1hcnk6IHsgYmVmb3JlLCBhZnRlcjogeyBoZWFsdGg6IGhvc3RpbGUubWF4SGVhbHRoLCBhdHRhY2s6IGhvc3RpbGUuYXR0YWNrLCBkZWZlbnNlOiBob3N0aWxlLmRlZmVuc2UgfSB9LCAuLi4ocGFzc2VkID8ge30gOiB7IHJlYXNvbjogJ3NpdGUgY29uZGl0aW9ucyBkaWQgbm90IGNoYW5nZSBlbmNvdW50ZXIgcHJlc3N1cmUnIH0pIH1cbn1cblxuY29uc3QgbGFuZGluZ1Rhc2sgPSAoKTogQXV0b3BsYXlUYXNrRXhlY3V0aW9uID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY3JlYXRlR2FsYXh5KDMxLCBoZXJvKCksIEFVVE9QTEFZX1RBU0tfRklYVFVSRV9USU1FKVxuICBjb25zdCBzaXRlID0gZ2FsYXh5LnNpdGVzW2dhbGF4eS5hY3RpdmVTaXRlSWRdIVxuICBjb25zdCBzdGF0ZSA9IG5ld1J1bihnYWxheHkuc2VlZCwgc2l0ZS5iaW9tZSlcbiAgY29uc3QgYmVmb3JlID0geyBzYWx2YWdlOiBzaXRlLnNhbHZhZ2UsIHN1cHBsaWVzOiBzaXRlLnN1cHBsaWVzLCBnb2xkOiBzdGF0ZS5oZXJvLmdvbGQgfVxuICBjb25zdCBzZXR0bGVkID0gcmVjb3JkR2FsYXh5TGFuZGluZyhnYWxheHksIHNpdGUuaWQsIHN0YXRlLCBBVVRPUExBWV9UQVNLX0ZJWFRVUkVfVElNRSArIDEpXG4gIGNvbnN0IGFmdGVyID0gc2V0dGxlZC5zaXRlc1tzaXRlLmlkXSFcbiAgY29uc3QgcGFzc2VkID0gc3RhdGUuaGVyby5nb2xkID4gYmVmb3JlLmdvbGQgJiYgYWZ0ZXIuc2FsdmFnZSA8IGJlZm9yZS5zYWx2YWdlICYmIGFmdGVyLnN1cHBsaWVzID4gYmVmb3JlLnN1cHBsaWVzXG4gIHJldHVybiB7IGFjdGlvbnM6IFsnY29tcGxldGU6bGFuZGluZyddLCBldmVudHM6IHNldHRsZWQuZXZlbnRzLm1hcChldmVudCA9PiBldmVudC5raW5kKSwgcGFzc2VkLCBzdW1tYXJ5OiB7IGJlZm9yZSwgYWZ0ZXI6IHsgc2FsdmFnZTogYWZ0ZXIuc2FsdmFnZSwgc3VwcGxpZXM6IGFmdGVyLnN1cHBsaWVzLCBnb2xkOiBzdGF0ZS5oZXJvLmdvbGQgfSB9LCAuLi4ocGFzc2VkID8ge30gOiB7IHJlYXNvbjogJ2xhbmRpbmcgc2V0dGxlbWVudCBkaWQgbm90IHVwZGF0ZSB0aGUgc2l0ZSBlY29ub215JyB9KSB9XG59XG5cbmNvbnN0IHNlY3RvckNsb2NrVGFzayA9ICgpOiBBdXRvcGxheVRhc2tFeGVjdXRpb24gPT4ge1xuICBjb25zdCBnYWxheHkgPSBjcmVhdGVHYWxheHkoMzcsIGhlcm8oKSwgQVVUT1BMQVlfVEFTS19GSVhUVVJFX1RJTUUpXG4gIGNvbnN0IGFkdmFuY2VkID0gYWR2YW5jZUdhbGF4eVJvdXRlUmVja29uaW5nKGdhbGF4eSwgMiAqIFJPVVRFX1JFQ0tPTklOR19XT1JMRF9USUNLX1VOSVRTKVxuICBjb25zdCBwYXNzZWQgPSBhZHZhbmNlZC5yb3V0ZVJlY2tvbmluZyA9PT0gMiAqIFJPVVRFX1JFQ0tPTklOR19XT1JMRF9USUNLX1VOSVRTICYmIGFkdmFuY2VkLmxhc3RXb3JsZFRpY2sgPT09IDIgJiYgYWR2YW5jZWQuZXZlbnRzLmxlbmd0aCA+PSBnYWxheHkuZXZlbnRzLmxlbmd0aFxuICByZXR1cm4geyBhY3Rpb25zOiBbJ3dhaXQ6cm91dGUtcmVja29uaW5nJ10sIGV2ZW50czogYWR2YW5jZWQuZXZlbnRzLm1hcChldmVudCA9PiBldmVudC5raW5kKSwgcGFzc2VkLCBzdW1tYXJ5OiB7IHJvdXRlUmVja29uaW5nOiBhZHZhbmNlZC5yb3V0ZVJlY2tvbmluZywgd29ybGRUaWNrOiBhZHZhbmNlZC5sYXN0V29ybGRUaWNrLCBldmVudENvdW50OiBhZHZhbmNlZC5ldmVudHMubGVuZ3RoIH0sIC4uLihwYXNzZWQgPyB7fSA6IHsgcmVhc29uOiAnUm91dGUgUmVja29uaW5nIGRpZCBub3QgYWR2YW5jZSBkZXRlcm1pbmlzdGljYWxseScgfSkgfVxufVxuXG5leHBvcnQgY29uc3QgYXV0b3BsYXlUYXNrQ2F0YWxvZyA9ICgpOiByZWFkb25seSBBdXRvcGxheVRhc2tbXSA9PiBbXG4gIHRhc2soJ3RhY3RpY2FsLmNvcmUtbG9vcCcsICd0YWN0aWNhbCcsIDEwMCwgJ3VpLmNvcmUtbG9vcCcsIHRhY3RpY2FsVGFzayksXG4gIHRhc2soJ3ZveWFnZXIuc2l0ZS1kaXNjb3ZlcnknLCAndm95YWdlcicsIDkwLCAndWkuc2VjdG9yLW5hdmlnYXRpb24nLCBkaXNjb3ZlcnlUYXNrLCBbJ3RhY3RpY2FsLmNvcmUtbG9vcCddKSxcbiAgdGFzaygndm95YWdlci50cmFuc2l0LXdpbmRvdycsICd2b3lhZ2VyJywgODUsICd1aS50cmFuc2l0JywgdHJhbnNpdFRhc2ssIFsndm95YWdlci5zaXRlLWRpc2NvdmVyeSddKSxcbiAgdGFzaygndm95YWdlci5jb250cmFjdC1kZWxpdmVyeScsICdlY29ub215JywgODAsICd1aS5jb250cmFjdC1kZWxpdmVyeScsIGNhcmdvRGVsaXZlcnlUYXNrLCBbJ3ZveWFnZXIuc2l0ZS1kaXNjb3ZlcnknXSksXG4gIHRhc2soJ3ZveWFnZXIuY2FyZ28tcmVjb3ZlcnknLCAnZWNvbm9teScsIDc1LCAndWkuY2FyZ28tcmVjb3ZlcnknLCBjYXJnb1JlY292ZXJ5VGFzaywgWyd2b3lhZ2VyLmNvbnRyYWN0LWRlbGl2ZXJ5J10pLFxuICB0YXNrKCd2b3lhZ2VyLnNlYWxlZC1wYWNrYWdlLWludGFjdCcsICdlY29ub215JywgNzQsICd1aS5zZWFsZWQtcGFja2FnZS1pbnRhY3QnLCBzZWFsZWRQYWNrYWdlSW50YWN0VGFzaywgWyd2b3lhZ2VyLnNpdGUtZGlzY292ZXJ5J10pLFxuICB0YXNrKCd2b3lhZ2VyLnNlYWxlZC1wYWNrYWdlLXRhbXBlcmVkJywgJ2Vjb25vbXknLCA3MywgJ3VpLnNlYWxlZC1wYWNrYWdlLXRhbXBlcmVkJywgc2VhbGVkUGFja2FnZVRhbXBlcmVkVGFzaywgWyd2b3lhZ2VyLnNlYWxlZC1wYWNrYWdlLWludGFjdCddKSxcbiAgdGFzaygndm95YWdlci5zZWFsZWQtcGFja2FnZS1yZWNvdmVyeScsICdsaWZlY3ljbGUnLCA3MiwgJ3VpLnNlYWxlZC1wYWNrYWdlLXJlY292ZXJ5Jywgc2VhbGVkUGFja2FnZVJlY292ZXJ5VGFzaywgWyd2b3lhZ2VyLnNlYWxlZC1wYWNrYWdlLXRhbXBlcmVkJ10pLFxuICB0YXNrKCd2b3lhZ2VyLmxhbmRpbmctY29uZGl0aW9ucycsICdlY29sb2d5JywgNzAsICd1aS5sYW5kaW5nLWNvbmRpdGlvbnMnLCBlY29sb2d5VGFzaywgWyd2b3lhZ2VyLnNpdGUtZGlzY292ZXJ5J10pLFxuICB0YXNrKCd2b3lhZ2VyLmxhbmRpbmctc2V0dGxlbWVudCcsICdsaWZlY3ljbGUnLCA2NSwgJ3VpLmxhbmRpbmctc2V0dGxlbWVudCcsIGxhbmRpbmdUYXNrLCBbJ3ZveWFnZXIubGFuZGluZy1jb25kaXRpb25zJ10pLFxuICB0YXNrKCd2b3lhZ2VyLnNlY3Rvci1jbG9jaycsICdsaWZlY3ljbGUnLCA2MCwgJ3VpLnNlY3Rvci1jbG9jaycsIHNlY3RvckNsb2NrVGFzaywgWyd2b3lhZ2VyLmxhbmRpbmctc2V0dGxlbWVudCddKVxuXVxuXG5leHBvcnQgY29uc3QgYXNzZXJ0QXV0b3BsYXlUYXNrQ2F0YWxvZyA9IChjYXRhbG9nOiByZWFkb25seSBBdXRvcGxheVRhc2tbXSA9IGF1dG9wbGF5VGFza0NhdGFsb2coKSk6IHZvaWQgPT4ge1xuICBjb25zdCBpZHMgPSBuZXcgU2V0PHN0cmluZz4oKVxuICBmb3IgKGNvbnN0IGVudHJ5IG9mIGNhdGFsb2cpIHtcbiAgICBpZiAoaWRzLmhhcyhlbnRyeS5pZCkpIHRocm93IG5ldyBFcnJvcihgZHVwbGljYXRlIGF1dG9wbGF5IHRhc2sgaWQ6ICR7ZW50cnkuaWR9YClcbiAgICBpZHMuYWRkKGVudHJ5LmlkKVxuICAgIGlmICghZW50cnkudWlUYXNrSWQpIHRocm93IG5ldyBFcnJvcihgYXV0b3BsYXkgdGFzayAke2VudHJ5LmlkfSBsYWNrcyBicm93c2VyIGNvdmVyYWdlIGlkYClcbiAgfVxuICBmb3IgKGNvbnN0IGVudHJ5IG9mIGNhdGFsb2cpIGZvciAoY29uc3QgcHJlcmVxdWlzaXRlIG9mIGVudHJ5LnByZXJlcXVpc2l0ZXMpIGlmICghaWRzLmhhcyhwcmVyZXF1aXNpdGUpKSB0aHJvdyBuZXcgRXJyb3IoYGF1dG9wbGF5IHRhc2sgJHtlbnRyeS5pZH0gaGFzIHVua25vd24gcHJlcmVxdWlzaXRlICR7cHJlcmVxdWlzaXRlfWApXG59XG4iXSwibWFwcGluZ3MiOiJBQUFBLFNBQVNBLFdBQVcsUUFBUSxtQkFBbUI7QUFDL0MsU0FBU0Msa0JBQWtCLEVBQUVDLG9CQUFvQixFQUFFQywyQkFBMkIsRUFBRUMsMkJBQTJCLEVBQUVDLG9CQUFvQixFQUFFQyx5QkFBeUIsRUFBRUMsWUFBWSxFQUFFQyxzQkFBc0IsRUFBRUMsb0JBQW9CLEVBQUVDLG1CQUFt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