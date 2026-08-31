// 322b28bdd8333c6ef3f17f733b928da200b3a9db
import { biomeName } from '../content';
import { rngFor } from '../rng';
import { appendGeneralManifest, normalizeGeneralManifest } from './manifest';
import { addKestrelSealedPackageOffer, applySealedPackageDeadlineTransitions } from './sealed-packages';
import { ROUTE_RECKONING_UNITS_PER_CYCLE, ROUTE_RECKONING_WORLD_TICK_UNITS, routeReckoningFromLegacyDay, routeWorldTickFor, sectorDayFromRouteReckoning } from './route-reckoning';
export const OUTER_SECTOR_COUNT = 20;
export const SITES_PER_SECTOR = 10;
/** @deprecated Legacy wall-clock constants retained only for callers migrating to Route Reckoning. */
export const GALAXY_HOUR_MS = 60 * 60 * 1000;
/** @deprecated Legacy wall-clock constants retained only for callers migrating to Route Reckoning. */
export const GALAXY_TICK_MS = 6 * GALAXY_HOUR_MS;
/** @deprecated Legacy wall-clock constants retained only for callers migrating to Route Reckoning. */
export const GALAXY_OFFLINE_CAP_MS = 7 * 24 * GALAXY_HOUR_MS;
const BIOMES = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'];
const sectorNames = ['Helios Reach', 'Aster Drift', 'Cinder Verge', 'Nerida Veil', 'Orison Belt', 'Kestrel March', 'Vesper Array', 'Halcyon Expanse', 'Borealis Wake', 'Tethys Fold', 'Morrow Chain', 'Lumen Scar', 'Icarus Shelf', 'Sable Meridian', 'Pallas Run', 'Caldera Span', 'Crown of Mira', 'Rook Nebula', 'Morrowline', 'Carthage Deep', 'New Edo Fringe'];
const crewTemplates = [{
  name: 'Ari Venn',
  role: 'security specialist',
  origin: 'mineborn',
  calling: 'trailguard',
  routine: 'maintain'
}, {
  name: 'Mika Sol',
  role: 'systems specialist',
  origin: 'mosswalker',
  calling: 'pathmaker',
  routine: 'scout'
}, {
  name: 'Cato Ren',
  role: 'xenoarchivist',
  origin: 'cavernSeeker',
  calling: 'spiritbearer',
  routine: 'research'
}, {
  name: 'Dara Ilyan',
  role: 'field medic',
  origin: 'tidebound',
  calling: 'trailguard',
  routine: 'recover'
}];
const factionNames = {
  voyager: 'Jomon',
  salvagers: 'Free Salvagers',
  relayGuild: 'Relay Guild',
  voidborn: 'Voidborn',
  settlers: 'New Edo Settlers'
};
const factions = () => [{
  id: 'voyager',
  name: factionNames.voyager,
  influence: 18,
  disposition: 40
}, {
  id: 'salvagers',
  name: factionNames.salvagers,
  influence: 24,
  disposition: -8
}, {
  id: 'relayGuild',
  name: factionNames.relayGuild,
  influence: 27,
  disposition: 12
}, {
  id: 'voidborn',
  name: factionNames.voidborn,
  influence: 19,
  disposition: -28
}, {
  id: 'settlers',
  name: factionNames.settlers,
  influence: 12,
  disposition: 28
}];
const siteId = (sector, index) => `sector-${String(sector).padStart(2, '0')}:site-${String(index).padStart(2, '0')}`;
const sectorId = sector => `sector-${String(sector).padStart(2, '0')}`;
const integerCoordinate = value => {
  const rounded = Math.round(value);
  return Object.is(rounded, -0) ? 0 : rounded;
};
const copyEvent = event => ({
  ...event
});
const copySite = site => ({
  ...site,
  links: [...site.links]
});
const copySnapshot = snapshot => ({
  version: 1,
  savedAt: snapshot.savedAt,
  run: structuredClone(snapshot.run)
});
const siteName = (biome, sector) => `${biomeName[biome].replace(' Colony', '')} // ${sector}`;
const initialFaction = (sector, index) => ['salvagers', 'relayGuild', 'voidborn', 'settlers'][(sector * 7 + index * 3) % 4];
const courierId = index => `voyager-crew-${index}`;
const cargoKinds = ['provisions', 'components', 'salvage', 'biosamples'];
const marketFor = rng => {
  const stock = {};
  const demand = {};
  const prices = {};
  for (const kind of cargoKinds) {
    stock[kind] = 8 + rng.int(0, 18);
    demand[kind] = 6 + rng.int(0, 16);
    prices[kind] = 8 + rng.int(0, 12);
  }
  return {
    stock,
    demand,
    prices
  };
};
const crewHero = (primary, template, index) => {
  const hero = structuredClone(primary);
  hero.name = template.name;
  hero.origin = template.origin;
  hero.calling = template.calling;
  hero.deathMode = 'ironTrail';
  hero.health = hero.maxHealth;
  hero.focus = hero.maxFocus;
  hero.gold = 0;
  hero.inventory = index % 2 ? ['tonic', 'rock', 'ropeBundle'] : ['focusTonic', 'rock', 'bombPack'];
  hero.equipment = {
    mainHand: index % 2 ? 'whip' : 'tideSpear'
  };
  hero.stats = {
    ...hero.stats,
    strength: Math.max(1, hero.stats.strength + (index === 0 ? 1 : 0)),
    agility: Math.max(1, hero.stats.agility + (index === 1 ? 1 : 0)),
    vitality: Math.max(1, hero.stats.vitality + (index === 3 ? 1 : 0)),
    intellect: Math.max(1, hero.stats.intellect + (index === 2 ? 1 : 0))
  };
  return hero;
};
export const createGalaxy = (seed, primary, legacyCreatedAt = 0) => {
  const sectors = [];
  const sites = {};
  for (let sectorIndex = 0; sectorIndex <= OUTER_SECTOR_COUNT; sectorIndex++) {
    var _sectorNames$sectorIn;
    const id = sectorId(sectorIndex);
    const rng = rngFor(seed, 'galaxy', 'sector', sectorIndex);
    const angle = rng.int(0, 359) * Math.PI / 180;
    const distance = sectorIndex === 0 ? 0 : 9 + Math.floor(Math.sqrt(sectorIndex) * 7) + rng.int(0, 5);
    const name = (_sectorNames$sectorIn = sectorNames[sectorIndex]) !== null && _sectorNames$sectorIn !== void 0 ? _sectorNames$sectorIn : `Uncatalogued ${sectorIndex}`;
    const ordered = sectorIndex === 0 ? [...BIOMES] : rng.shuffle([...BIOMES]);
    const ids = ordered.map((biome, siteIndex) => {
      const currentId = siteId(sectorIndex, siteIndex);
      sites[currentId] = {
        id: currentId,
        sectorId: id,
        name: siteName(biome, name),
        biome,
        x: integerCoordinate(Math.cos(angle) * distance * 10) + siteIndex % 5 * 3,
        y: integerCoordinate(Math.sin(angle) * distance * 7) + Math.floor(siteIndex / 5) * 3,
        links: [],
        discovered: sectorIndex === 0 && siteIndex === 0,
        completed: false,
        control: initialFaction(sectorIndex, siteIndex),
        integrity: 55 + rng.int(0, 35),
        ecology: 35 + rng.int(0, 40),
        construction: 0,
        supplies: 30 + rng.int(0, 45),
        salvage: 25 + rng.int(0, 55),
        market: marketFor(rng),
        lastChangedAt: 0
      };
      return currentId;
    });
    for (let siteIndex = 0; siteIndex < ids.length; siteIndex++) {
      const current = sites[ids[siteIndex]];
      const connect = other => {
        if (other && !current.links.includes(other)) current.links.push(other);
      };
      connect(ids[(siteIndex + 1) % ids.length]);
      connect(ids[(siteIndex + ids.length - 1) % ids.length]);
      if (siteIndex % 2 === 0) connect(ids[(siteIndex + 3) % ids.length]);
    }
    sectors.push({
      id,
      name,
      x: integerCoordinate(Math.cos(angle) * distance),
      y: integerCoordinate(Math.sin(angle) * distance),
      discovered: sectorIndex === 0,
      siteIds: ids
    });
  }
  for (let sectorIndex = 0; sectorIndex < OUTER_SECTOR_COUNT; sectorIndex++) {
    const source = sites[siteId(sectorIndex, 8)];
    const destination = siteId(sectorIndex + 1, 1);
    source.links.push(destination);
    sites[destination].links.push(source.id);
  }
  const activeId = siteId(0, 0);
  const primaryCourier = {
    id: courierId(0),
    name: primary.name,
    role: 'Jomon specialist',
    origin: primary.origin,
    calling: primary.calling,
    routine: 'socialize',
    status: 'available',
    affinity: 12,
    siteId: activeId,
    personalItems: [],
    hero: structuredClone(primary)
  };
  const couriers = [primaryCourier, ...crewTemplates.map((template, index) => ({
    id: courierId(index + 1),
    ...template,
    status: 'available',
    affinity: index % 2 ? -4 : 7,
    ...(index === 1 ? {
      rivalId: courierId(3)
    } : {}),
    personalItems: [],
    hero: crewHero(primary, template, index)
  }))];
  return addKestrelSealedPackageOffer({
    version: 2,
    seed,
    ...(legacyCreatedAt ? {
      createdAt: legacyCreatedAt
    } : {}),
    routeReckoning: 0,
    lastWorldTick: 0,
    sectorDay: 0,
    activeSiteId: activeId,
    activeCourierId: primaryCourier.id,
    sectors,
    sites,
    couriers,
    factions: factions(),
    events: [{
      id: `event:${seed}:arrival`,
      at: 0,
      routeReckoning: 0,
      kind: 'discovery',
      siteId: activeId,
      headline: 'Jomon enters Helios Reach',
      detail: 'Jomon has arrived at an uncharted frontier. Only Kestrel is reachable until its landing routes are surveyed.'
    }],
    siteSnapshots: {},
    sharedStash: [],
    cargo: [],
    contracts: [],
    sealedPackageContracts: [],
    sealedPackages: [],
    generalManifest: {
      version: 2,
      nextSequence: 0,
      entries: []
    },
    routeCaches: []
  });
};
export const cloneGalaxy = galaxy => ({
  ...galaxy,
  sectors: galaxy.sectors.map(sector => ({
    ...sector,
    siteIds: [...sector.siteIds]
  })),
  sites: Object.fromEntries(Object.entries(galaxy.sites).map(([id, site]) => [id, {
    ...copySite(site),
    market: structuredClone(site.market)
  }])),
  couriers: galaxy.couriers.map(courier => ({
    ...courier,
    personalItems: [...courier.personalItems],
    hero: structuredClone(courier.hero)
  })),
  factions: galaxy.factions.map(faction => ({
    ...faction
  })),
  events: galaxy.events.map(copyEvent),
  siteSnapshots: Object.fromEntries(Object.entries(galaxy.siteSnapshots).map(([id, snapshot]) => [id, copySnapshot(snapshot)])),
  sharedStash: [...galaxy.sharedStash],
  cargo: galaxy.cargo.map(cargo => ({
    ...cargo
  })),
  contracts: galaxy.contracts.map(contract => ({
    ...contract,
    cargo: {
      ...contract.cargo
    }
  })),
  sealedPackageContracts: galaxy.sealedPackageContracts.map(contract => ({
    ...contract,
    terms: {
      ...contract.terms,
      prohibitedActions: [...contract.terms.prohibitedActions]
    }
  })),
  sealedPackages: galaxy.sealedPackages.map(packageRecord => ({
    ...packageRecord,
    exterior: {
      ...packageRecord.exterior
    },
    ...(packageRecord.revealedContents ? {
      revealedContents: {
        ...packageRecord.revealedContents
      }
    } : {})
  })),
  generalManifest: {
    ...galaxy.generalManifest,
    entries: galaxy.generalManifest.entries.map(entry => ({
      ...entry
    }))
  },
  routeCaches: galaxy.routeCaches.map(cache => ({
    ...cache,
    cargo: cache.cargo.map(cargo => ({
      ...cargo
    })),
    packages: [...cache.packages]
  }))
});
const appendEvent = (galaxy, event) => {
  if (galaxy.events.some(candidate => candidate.id === event.id)) return;
  galaxy.events.unshift(event);
  galaxy.events = galaxy.events.slice(0, 240);
};
const eventId = (galaxy, tick, suffix) => `event:${galaxy.seed}:${tick}:${suffix}`;
const clamp = (value, min = 0, max = 100) => Math.max(min, Math.min(max, value));
const shiftSite = (galaxy, site, tick) => {
  const rng = rngFor(galaxy.seed, 'galaxy', 'tick', tick, site.id);
  const change = rng.int(-12, 12);
  const kind = rng.int(0, 4);
  const before = {
    control: site.control,
    integrity: site.integrity,
    ecology: site.ecology,
    construction: site.construction,
    supplies: site.supplies
  };
  if (kind === 0) {
    site.integrity = clamp(site.integrity + change);
    appendEvent(galaxy, {
      id: eventId(galaxy, tick, `${site.id}:integrity`),
      at: galaxy.routeReckoning,
      routeReckoning: galaxy.routeReckoning,
      kind: 'ecology',
      siteId: site.id,
      headline: `${site.name}: terrain shifted`,
      detail: change < 0 ? 'A regional disturbance damaged known routes.' : 'Local crews stabilized part of the terrain.'
    });
  } else if (kind === 1) {
    site.ecology = clamp(site.ecology + change);
    appendEvent(galaxy, {
      id: eventId(galaxy, tick, `${site.id}:ecology`),
      at: galaxy.routeReckoning,
      routeReckoning: galaxy.routeReckoning,
      kind: 'ecology',
      siteId: site.id,
      headline: `${site.name}: ecological change`,
      detail: change < 0 ? 'A hostile bloom spread through the site.' : 'A dormant habitat recovered.'
    });
  } else if (kind === 2) {
    const control = ['salvagers', 'relayGuild', 'voidborn', 'settlers'][rng.int(0, 3)];
    if (control !== site.control) {
      site.control = control;
      appendEvent(galaxy, {
        id: eventId(galaxy, tick, `${site.id}:territory`),
        at: galaxy.routeReckoning,
        routeReckoning: galaxy.routeReckoning,
        kind: 'territory',
        siteId: site.id,
        headline: `${site.name}: control changed`,
        detail: `${factionNames[control]} now holds the visible approaches.`
      });
    }
  } else {
    site.construction = clamp(site.construction + change);
    site.supplies = clamp(site.supplies + rng.int(-8, 12));
    site.salvage = clamp(site.salvage + rng.int(-10, 10));
    appendEvent(galaxy, {
      id: eventId(galaxy, tick, `${site.id}:construction`),
      at: galaxy.routeReckoning,
      routeReckoning: galaxy.routeReckoning,
      kind: 'construction',
      siteId: site.id,
      headline: `${site.name}: construction changed`,
      detail: 'Autonomous crews altered the site while Jomon was elsewhere.'
    });
  }
  site.lastChangedAt = galaxy.routeReckoning;
  for (const kind of cargoKinds) {
    const market = site.market;
    market.stock[kind] = clamp(market.stock[kind] + rng.int(-3, 4));
    market.demand[kind] = clamp(market.demand[kind] + rng.int(-2, 3));
    market.prices[kind] = clamp(8 + Math.round((market.demand[kind] - market.stock[kind]) / 3) + (site.control === 'voidborn' ? 4 : 0), 3, 30);
  }
  const manifest = (kind, detail, payload) => appendGeneralManifest(galaxy, {
    kind,
    detail,
    source: 'site',
    siteId: site.id,
    payload
  });
  if (before.supplies >= 25 && site.supplies < 25) manifest('siteSupplyCrisis', `${site.name} entered a supply crisis.`, {
    before: before.supplies,
    after: site.supplies
  });else if (before.supplies < 25 && site.supplies >= 25) manifest('siteSupplyRecovery', `${site.name} recovered above its supply-crisis threshold.`, {
    before: before.supplies,
    after: site.supplies
  });
  if (before.integrity >= 40 && site.integrity < 40) manifest('siteIntegrityDegraded', `${site.name} approach integrity degraded.`, {
    before: before.integrity,
    after: site.integrity
  });else if (before.integrity < 40 && site.integrity >= 40) manifest('siteIntegrityRecovered', `${site.name} approach integrity recovered.`, {
    before: before.integrity,
    after: site.integrity
  });
  if (before.ecology >= 35 && site.ecology < 35 || before.ecology < 35 && site.ecology >= 35) manifest('siteEcologyShift', `${site.name} ecological operating conditions shifted.`, {
    before: before.ecology,
    after: site.ecology
  });
  if (before.construction < 80 && site.construction >= 80) manifest('siteConstructionCompleted', `${site.name} construction reached operational completion.`, {
    before: before.construction,
    after: site.construction
  });else if (before.construction >= 25 && site.construction < 25) manifest('siteConstructionLost', `${site.name} lost operational construction capacity.`, {
    before: before.construction,
    after: site.construction
  });
  if (before.control !== site.control) manifest('siteControlChanged', `${site.name} is now controlled by ${factionNames[site.control]}.`, {
    before: before.control,
    after: site.control
  });
};
const evolveCouriers = (galaxy, tick) => {
  const rng = rngFor(galaxy.seed, 'galaxy', 'crew', tick);
  for (const courier of [...galaxy.couriers].sort((left, right) => left.id.localeCompare(right.id))) {
    if (courier.status === 'dead' || courier.status === 'retired') continue;
    if (courier.id === galaxy.activeCourierId) continue;
    courier.affinity = clamp(courier.affinity + rng.int(-3, 3), -100, 100);
    if (courier.routine === 'trade') {
      const sites = Object.values(galaxy.sites).sort((left, right) => left.id.localeCompare(right.id));
      const site = sites[rng.int(0, sites.length - 1)];
      if (site) {
        const kind = cargoKinds[rng.int(0, cargoKinds.length - 1)];
        site.market.stock[kind] = clamp(site.market.stock[kind] + rng.int(1, 4));
        site.market.demand[kind] = clamp(site.market.demand[kind] - 1);
        appendEvent(galaxy, {
          id: eventId(galaxy, tick, `${courier.id}:trade`),
          at: galaxy.routeReckoning,
          routeReckoning: galaxy.routeReckoning,
          kind: 'construction',
          siteId: site.id,
          courierId: courier.id,
          headline: `${courier.name} completed a trade survey`,
          detail: `${courier.name} improved ${kind} availability at ${site.name}; the Jomon hold was not altered.`
        });
      }
    }
    if (rng.chance(4)) {
      courier.status = 'injured';
      appendEvent(galaxy, {
        id: eventId(galaxy, tick, `${courier.id}:injured`),
        at: galaxy.routeReckoning,
        routeReckoning: galaxy.routeReckoning,
        kind: 'loss',
        courierId: courier.id,
        headline: `${courier.name} was injured off-watch`,
        detail: `${courier.name}'s ${courier.routine} shift encountered a sector hazard.`
      });
      appendGeneralManifest(galaxy, {
        kind: 'courierStatusChanged',
        detail: `${courier.name} was injured while off-watch.`,
        source: 'courier',
        courierId: courier.id,
        payload: {
          status: 'injured',
          routine: courier.routine
        }
      });
    } else if (rng.chance(1)) {
      courier.status = 'dead';
      appendEvent(galaxy, {
        id: eventId(galaxy, tick, `${courier.id}:lost`),
        at: galaxy.routeReckoning,
        routeReckoning: galaxy.routeReckoning,
        kind: 'loss',
        courierId: courier.id,
        headline: `${courier.name} is lost`,
        detail: `Their final autonomous ${courier.routine} shift is now part of the Jomon chronicle.`
      });
      appendGeneralManifest(galaxy, {
        kind: 'courierStatusChanged',
        detail: `${courier.name} is lost during an autonomous ${courier.routine} shift.`,
        source: 'courier',
        courierId: courier.id,
        payload: {
          status: 'dead',
          routine: courier.routine
        }
      });
    }
  }
};
const advanceGenericContractDeadlines = galaxy => {
  for (const contract of galaxy.contracts) {
    var _galaxy$sites$contrac, _galaxy$sites$contrac2;
    if (contract.status !== 'open' && contract.status !== 'active' || contract.deadlineReckoning >= galaxy.routeReckoning) continue;
    contract.status = 'failed';
    galaxy.cargo = galaxy.cargo.filter(cargo => cargo.contractId !== contract.id);
    appendEvent(galaxy, {
      id: `event:${galaxy.seed}:contract:${contract.id}:expired`,
      at: galaxy.routeReckoning,
      routeReckoning: galaxy.routeReckoning,
      kind: 'trade',
      siteId: contract.destinationSiteId,
      headline: 'Contract window expired',
      detail: `${contract.cargo.units} units of ${contract.cargo.kind} are no longer accepted at ${(_galaxy$sites$contrac = (_galaxy$sites$contrac2 = galaxy.sites[contract.destinationSiteId]) === null || _galaxy$sites$contrac2 === void 0 ? void 0 : _galaxy$sites$contrac2.name) !== null && _galaxy$sites$contrac !== void 0 ? _galaxy$sites$contrac : contract.destinationSiteId}.`
    });
    appendGeneralManifest(galaxy, {
      kind: 'contractExpired',
      detail: `Legacy cargo contract ${contract.id} expired.`,
      source: 'contract',
      contractId: contract.id,
      siteId: contract.destinationSiteId,
      payload: {
        cargoKind: contract.cargo.kind,
        units: contract.cargo.units
      }
    });
  }
};

/** Advances canonical state by integer Route Reckoning marks only. */
export const advanceGalaxyRouteReckoning = (source, marks) => {
  const galaxy = cloneGalaxy(source);
  const steps = Math.max(0, Math.floor(marks));
  for (let step = 0; step < steps; step++) {
    galaxy.routeReckoning++;
    galaxy.sectorDay = sectorDayFromRouteReckoning(galaxy.routeReckoning);
    applySealedPackageDeadlineTransitions(galaxy);
    advanceGenericContractDeadlines(galaxy);
    if (galaxy.routeReckoning % ROUTE_RECKONING_WORLD_TICK_UNITS !== 0) continue;
    const tick = routeWorldTickFor(galaxy.routeReckoning);
    if (tick <= galaxy.lastWorldTick) continue;
    galaxy.lastWorldTick = tick;
    const candidates = Object.values(galaxy.sites).sort((left, right) => left.id.localeCompare(right.id));
    const site = candidates[rngFor(galaxy.seed, 'galaxy', 'site', tick).int(0, candidates.length - 1)];
    if (site) shiftSite(galaxy, site, tick);
    evolveCouriers(galaxy, tick);
  }
  return galaxy;
};

/** @deprecated No-op compatibility shim. Offline wall-clock reconciliation is forbidden in M2. */
export const reconcileGalaxy = (source, _legacyNow) => cloneGalaxy(source);
export const discoverLinkedSites = (source, sourceId, _legacyNow) => {
  const galaxy = cloneGalaxy(source);
  const site = galaxy.sites[sourceId];
  if (!site) return galaxy;
  site.completed = true;
  site.lastChangedAt = galaxy.routeReckoning;
  for (const id of site.links) {
    const destination = galaxy.sites[id];
    if (!destination || destination.discovered) continue;
    destination.discovered = true;
    const sector = galaxy.sectors.find(candidate => candidate.id === destination.sectorId);
    if (sector) sector.discovered = true;
    appendEvent(galaxy, {
      id: `event:${galaxy.seed}:link:${sourceId}:${id}`,
      at: galaxy.routeReckoning,
      routeReckoning: galaxy.routeReckoning,
      kind: 'discovery',
      siteId: id,
      headline: `Route to ${destination.name} surveyed`,
      detail: `A physical approach has been found from ${site.name}.`
    });
    if (!galaxy.contracts.some(contract => contract.sourceSiteId === sourceId && contract.destinationSiteId === id && contract.status === 'open')) {
      const kind = cargoKinds[rngFor(galaxy.seed, 'galaxy', 'contract', sourceId, id).int(0, cargoKinds.length - 1)];
      galaxy.contracts.push({
        id: `contract:${sourceId}:${id}`,
        sourceSiteId: sourceId,
        destinationSiteId: id,
        cargo: {
          kind,
          units: 2
        },
        fee: destination.market.prices[kind] * 3,
        deadlineDay: galaxy.sectorDay + 18,
        deadlineReckoning: galaxy.routeReckoning + 18 * ROUTE_RECKONING_UNITS_PER_CYCLE,
        factionId: destination.control,
        status: 'open',
        collateral: destination.market.prices[kind]
      });
    }
  }
  appendEvent(galaxy, {
    id: `event:${galaxy.seed}:complete:${sourceId}:${galaxy.routeReckoning}`,
    at: galaxy.routeReckoning,
    routeReckoning: galaxy.routeReckoning,
    kind: 'discovery',
    siteId: sourceId,
    headline: `${site.name} survey archived`,
    detail: 'Jomon has preserved this landing as a persistent site.'
  });
  return galaxy;
};
export const setActiveGalaxySite = (source, id) => {
  var _galaxy$sites$id;
  const galaxy = cloneGalaxy(source);
  if ((_galaxy$sites$id = galaxy.sites[id]) !== null && _galaxy$sites$id !== void 0 && _galaxy$sites$id.discovered) galaxy.activeSiteId = id;
  return galaxy;
};
export const setCourierRoutine = (source, courierId, routine) => {
  const galaxy = cloneGalaxy(source);
  const courier = galaxy.couriers.find(candidate => candidate.id === courierId);
  if (courier && courier.status !== 'dead' && courier.status !== 'retired') courier.routine = routine;
  return galaxy;
};
export const selectGalaxyCourier = (source, courierId) => {
  const galaxy = cloneGalaxy(source);
  const courier = galaxy.couriers.find(candidate => candidate.id === courierId);
  if (!courier || courier.status !== 'available') return {
    galaxy
  };
  galaxy.activeCourierId = courier.id;
  appendEvent(galaxy, {
    id: `event:${galaxy.seed}:handoff:${courier.id}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`,
    at: galaxy.routeReckoning,
    routeReckoning: galaxy.routeReckoning,
    kind: 'discovery',
    courierId: courier.id,
    headline: `${courier.name} takes the landing watch`,
    detail: `${courier.role} has been assigned as Jomon's active specialist.`
  });
  return {
    galaxy,
    hero: structuredClone(courier.hero)
  };
};
export const loseGalaxyCourier = (source, courierId, detail, _legacyNow) => {
  const galaxy = cloneGalaxy(source);
  const courier = galaxy.couriers.find(candidate => candidate.id === courierId);
  if (!courier) return galaxy;
  courier.status = 'dead';
  appendEvent(galaxy, {
    id: `event:${galaxy.seed}:loss:${courier.id}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`,
    at: galaxy.routeReckoning,
    routeReckoning: galaxy.routeReckoning,
    kind: 'loss',
    courierId: courier.id,
    headline: `${courier.name} is lost`,
    detail
  });
  appendGeneralManifest(galaxy, {
    kind: 'courierStatusChanged',
    detail: `${courier.name} is lost.`,
    source: 'courier',
    courierId: courier.id,
    payload: {
      status: 'dead'
    }
  });
  return galaxy;
};
export const saveGalaxySite = (source, siteIdValue, run, _legacyNow) => {
  const galaxy = cloneGalaxy(source);
  if (!galaxy.sites[siteIdValue]) return galaxy;
  galaxy.siteSnapshots[siteIdValue] = {
    version: 1,
    run: structuredClone(run),
    savedAt: galaxy.routeReckoning
  };
  return galaxy;
};
const factionPressure = {
  voyager: {
    health: 0,
    attack: 0,
    defense: 0,
    reward: 1.1,
    label: 'Voyager survey teams keep the approach supplied.'
  },
  salvagers: {
    health: 0,
    attack: 1,
    defense: 0,
    reward: 1.3,
    label: 'Salvager patrols contest valuable caches.'
  },
  relayGuild: {
    health: -1,
    attack: 0,
    defense: 0,
    reward: 1.2,
    label: 'Relay Guild infrastructure shortens routes but prices supplies.'
  },
  voidborn: {
    health: 2,
    attack: 1,
    defense: 1,
    reward: 1.45,
    label: 'Voidborn pressure turns every approach into a fight.'
  },
  settlers: {
    health: -1,
    attack: 0,
    defense: 0,
    reward: .9,
    label: 'Settler routes are safer, but most salvage is already spoken for.'
  }
};
export const applyGalaxySiteConditions = (run, site) => {
  var _run$floor$difficulty;
  const pressure = factionPressure[site.control];
  const integrityPressure = site.integrity < 40 ? 1 : 0;
  const ecologyPressure = site.ecology < 40 ? 1 : 0;
  const healthMultiplier = 1 + Math.max(0, pressure.health + integrityPressure) * .12;
  const attackBonus = pressure.attack + ecologyPressure;
  const defenseBonus = pressure.defense + (site.construction > 65 ? 1 : 0);
  for (const actor of run.floor.actors) {
    if (!actor.hostile) continue;
    actor.maxHealth = Math.max(1, Math.round(actor.maxHealth * healthMultiplier));
    actor.health = Math.min(actor.maxHealth, Math.round(actor.health * healthMultiplier));
    actor.attack += attackBonus;
    actor.defense += defenseBonus;
  }
  run.floor.difficulty = {
    ...((_run$floor$difficulty = run.floor.difficulty) !== null && _run$floor$difficulty !== void 0 ? _run$floor$difficulty : {
      routePosition: 0,
      threat: 0,
      healthMultiplier: 1,
      attackBonus: 0,
      defenseBonus: 0,
      eliteChance: 0,
      guardianPattern: 0
    }),
    healthMultiplier,
    attackBonus,
    defenseBonus,
    rewardMultiplier: pressure.reward
  };
  const terrain = run.floor.tiles.filter((tile, index) => tile.kind === 'floor' && index !== run.floor.start.y * run.floor.width + run.floor.start.x);
  if (site.ecology < 35) terrain.filter((_, index) => index % 37 === 0).forEach(tile => {
    tile.kind = 'gas';
  });
  if (site.integrity < 35) terrain.filter((_, index) => index % 41 === 0).forEach(tile => {
    tile.kind = 'rubble';
  });
  run.messages.unshift(`${site.name}: ${pressure.label}`);
  if (site.ecology < 35) run.messages.unshift('Ecology alert: invasive spores have altered the landing route.');
  if (site.integrity < 35) run.messages.unshift('Integrity alert: collapse debris has narrowed the landing route.');
  return run;
};
export const recordGalaxyLanding = (source, siteIdValue, run, _legacyNow) => {
  var _run$telemetry$kills, _run$telemetry;
  const galaxy = cloneGalaxy(source);
  const site = galaxy.sites[siteIdValue];
  if (!site) return galaxy;
  const pressure = factionPressure[site.control];
  const kills = (_run$telemetry$kills = (_run$telemetry = run.telemetry) === null || _run$telemetry === void 0 ? void 0 : _run$telemetry.kills) !== null && _run$telemetry$kills !== void 0 ? _run$telemetry$kills : 0;
  const reward = Math.max(4, Math.round((6 + kills + Math.floor(site.salvage / 18)) * pressure.reward));
  run.hero.gold += reward;
  site.salvage = clamp(site.salvage - Math.max(2, reward / 2));
  site.supplies = clamp(site.supplies + 4 + Math.min(10, kills));
  site.integrity = clamp(site.integrity + 3);
  site.ecology = clamp(site.ecology + (site.ecology < 45 ? 2 : 0));
  site.construction = clamp(site.construction + 2);
  site.lastChangedAt = galaxy.routeReckoning;
  appendEvent(galaxy, {
    id: `event:${galaxy.seed}:landing:${siteIdValue}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`,
    at: galaxy.routeReckoning,
    routeReckoning: galaxy.routeReckoning,
    kind: 'construction',
    siteId: siteIdValue,
    headline: `${site.name}: landing returned ${reward} credits`,
    detail: `Survey work increased supplies to ${site.supplies} and stabilized the approach.`
  });
  return galaxy;
};
export const galaxyRouteLength = (galaxy, fromSiteId, toSiteId) => 1 + rngFor(galaxy.seed, 'galaxy', 'link', [fromSiteId, toSiteId].sort().join('::')).int(0, 4);
export const galaxyRouteSituation = (galaxy, fromSiteId, toSiteId, chunk) => {
  const outcomes = ['quiet', 'patrol', 'hazard', 'trader', 'ecology'];
  return outcomes[rngFor(galaxy.seed, 'galaxy', 'route-situation', [fromSiteId, toSiteId].sort().join('::'), chunk, routeWorldTickFor(galaxy.routeReckoning)).int(0, outcomes.length - 1)];
};
export const recordGalaxyRouteSituations = (source, linkId, situations) => {
  const galaxy = cloneGalaxy(source);
  situations.forEach((situation, chunk) => {
    if (situation === 'quiet') return;
    appendGeneralManifest(galaxy, {
      kind: 'routeSituationActivated',
      detail: `${situation} conditions were reported on route ${linkId}, connector segment ${chunk + 1}.`,
      source: 'route',
      payload: {
        linkId,
        chunk,
        situation,
        status: 'reported'
      }
    });
  });
  return galaxy;
};
export const resolveGalaxyRouteSituations = (source, linkId, situations) => {
  const galaxy = cloneGalaxy(source);
  situations.forEach((situation, chunk) => {
    if (situation === 'quiet') return;
    appendGeneralManifest(galaxy, {
      kind: 'routeSituationResolved',
      detail: `${situation} conditions concluded on route ${linkId}, connector segment ${chunk + 1}.`,
      source: 'route',
      payload: {
        linkId,
        chunk,
        situation,
        status: 'confirmed'
      }
    });
  });
  return galaxy;
};
export const acceptGalaxyContract = (source, fromSiteId, toSiteId, _legacyNow) => {
  var _galaxy$sites$toSiteI, _galaxy$sites$toSiteI2;
  const galaxy = cloneGalaxy(source);
  const contract = galaxy.contracts.find(candidate => candidate.sourceSiteId === fromSiteId && candidate.destinationSiteId === toSiteId && candidate.status === 'open');
  if (!contract) return {
    galaxy,
    message: 'No open contract is registered for this airlock.'
  };
  if (galaxy.cargo.reduce((total, cargo) => total + cargo.units, 0) + contract.cargo.units > 12) return {
    galaxy,
    message: 'Jomon cargo hold lacks capacity for this contract.'
  };
  const sourceSite = galaxy.sites[fromSiteId];
  if (!sourceSite || sourceSite.market.stock[contract.cargo.kind] < contract.cargo.units) return {
    galaxy,
    message: 'The source market cannot load that cargo today.'
  };
  sourceSite.market.stock[contract.cargo.kind] -= contract.cargo.units;
  galaxy.cargo.push({
    ...contract.cargo,
    contractId: contract.id
  });
  contract.status = 'active';
  appendEvent(galaxy, {
    id: `event:${galaxy.seed}:contract:${contract.id}:accepted`,
    at: galaxy.routeReckoning,
    routeReckoning: galaxy.routeReckoning,
    kind: 'trade',
    siteId: fromSiteId,
    headline: 'Contract cargo loaded',
    detail: `${contract.cargo.units} units of ${contract.cargo.kind} are bound for ${(_galaxy$sites$toSiteI = (_galaxy$sites$toSiteI2 = galaxy.sites[toSiteId]) === null || _galaxy$sites$toSiteI2 === void 0 ? void 0 : _galaxy$sites$toSiteI2.name) !== null && _galaxy$sites$toSiteI !== void 0 ? _galaxy$sites$toSiteI : toSiteId}.`
  });
  return {
    galaxy,
    message: `Loaded ${contract.cargo.units} units of ${contract.cargo.kind}.`
  };
};
export const deliverGalaxyContracts = (source, siteId, hero, _legacyNow) => {
  const galaxy = cloneGalaxy(source);
  const deliverable = galaxy.contracts.filter(contract => contract.destinationSiteId === siteId && contract.status === 'active' && contract.deadlineReckoning >= galaxy.routeReckoning);
  if (!deliverable.length) return {
    galaxy
  };
  let fee = 0;
  for (const contract of deliverable) {
    contract.status = 'completed';
    fee += contract.fee;
    galaxy.cargo = galaxy.cargo.filter(cargo => cargo.contractId !== contract.id);
    galaxy.sites[siteId].market.stock[contract.cargo.kind] = clamp(galaxy.sites[siteId].market.stock[contract.cargo.kind] + contract.cargo.units);
  }
  hero.gold += fee;
  appendEvent(galaxy, {
    id: `event:${galaxy.seed}:contract:${siteId}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}:delivered`,
    at: galaxy.routeReckoning,
    routeReckoning: galaxy.routeReckoning,
    kind: 'trade',
    siteId,
    headline: 'Contract delivered',
    detail: `${deliverable.length} delivery${deliverable.length === 1 ? '' : 'ies'} paid ${fee} credits.`
  });
  return {
    galaxy,
    message: `Delivery complete: ${fee} credits.`
  };
};
export const abandonGalaxyCargo = (source, linkId, chunk, _legacyNow) => {
  const galaxy = cloneGalaxy(source);
  const cargo = galaxy.cargo.filter(candidate => candidate.contractId);
  if (!cargo.length) return galaxy;
  galaxy.cargo = galaxy.cargo.filter(candidate => !candidate.contractId);
  for (const contract of galaxy.contracts) if (cargo.some(candidate => candidate.contractId === contract.id)) contract.status = 'failed';
  galaxy.routeCaches.push({
    id: `cache:${linkId}:${chunk}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`,
    linkId,
    chunk,
    cargo,
    packages: [],
    recovered: false
  });
  appendEvent(galaxy, {
    id: `event:${galaxy.seed}:cache:${linkId}:${galaxy.routeReckoning}:${galaxy.generalManifest.nextSequence}`,
    at: galaxy.routeReckoning,
    routeReckoning: galaxy.routeReckoning,
    kind: 'loss',
    headline: 'Contract cargo abandoned',
    detail: 'A recoverable route cache marks the last known position of the cargo.'
  });
  return galaxy;
};
export const recoverGalaxyRouteCaches = (source, linkId) => {
  const galaxy = cloneGalaxy(source);
  const caches = galaxy.routeCaches.filter(cache => cache.linkId === linkId && !cache.recovered && cache.cargo.length);
  const cargo = caches.flatMap(cache => cache.cargo);
  const capacity = 12 - galaxy.cargo.reduce((total, entry) => total + entry.units, 0);
  const recovered = cargo.reduce((total, entry) => total + entry.units, 0);
  if (!caches.length) return {
    galaxy,
    message: 'No recoverable cargo cache is recorded on this route.'
  };
  if (recovered > capacity) return {
    galaxy,
    message: 'Voyager cargo hold lacks room for the recovered cache.'
  };
  galaxy.cargo.push(...cargo.map(entry => ({
    ...entry,
    contractId: undefined
  })));
  caches.forEach(cache => {
    cache.recovered = true;
  });
  return {
    galaxy,
    message: `Recovered ${recovered} cargo units. The failed contracts remain closed.`
  };
};
export const galaxySnapshot = (galaxy, siteIdValue) => galaxy.siteSnapshots[siteIdValue] ? structuredClone(galaxy.siteSnapshots[siteIdValue].run) : undefined;
export const availableGalaxySites = galaxy => Object.values(galaxy.sites).filter(site => site.discovered).sort((left, right) => left.id.localeCompare(right.id));
export const galaxyChronicle = galaxy => galaxy.events;
const isRecord = value => typeof value === 'object' && value !== null && !Array.isArray(value);
export const migrateGalaxy = value => {
  if (!isRecord(value) || value.version !== 1 && value.version !== 2 || typeof value.seed !== 'number' || !Array.isArray(value.sectors) || !isRecord(value.sites) || !Array.isArray(value.couriers) || !Array.isArray(value.factions) || !Array.isArray(value.events) || !isRecord(value.siteSnapshots)) return undefined;
  try {
    var _galaxy$cargo, _galaxy$contracts, _galaxy$sealedPackage, _galaxy$sealedPackage2, _galaxy$generalManife, _galaxy$routeCaches;
    const legacy = structuredClone(value);
    const legacyDay = typeof legacy.sectorDay === 'number' && Number.isFinite(legacy.sectorDay) ? legacy.sectorDay : 0;
    const routeReckoning = Number.isInteger(legacy.routeReckoning) && legacy.routeReckoning >= 0 ? legacy.routeReckoning : routeReckoningFromLegacyDay(legacyDay);
    const galaxy = {
      ...legacy,
      version: 2,
      routeReckoning,
      lastWorldTick: Number.isInteger(legacy.lastWorldTick) && legacy.lastWorldTick >= 0 ? Math.min(legacy.lastWorldTick, routeWorldTickFor(routeReckoning)) : routeWorldTickFor(routeReckoning),
      sectorDay: sectorDayFromRouteReckoning(routeReckoning),
      sharedStash: Array.isArray(value.sharedStash) ? [...legacy.sharedStash] : []
    };
    (_galaxy$cargo = galaxy.cargo) !== null && _galaxy$cargo !== void 0 ? _galaxy$cargo : galaxy.cargo = [];
    (_galaxy$contracts = galaxy.contracts) !== null && _galaxy$contracts !== void 0 ? _galaxy$contracts : galaxy.contracts = [];
    (_galaxy$sealedPackage = galaxy.sealedPackageContracts) !== null && _galaxy$sealedPackage !== void 0 ? _galaxy$sealedPackage : galaxy.sealedPackageContracts = [];
    (_galaxy$sealedPackage2 = galaxy.sealedPackages) !== null && _galaxy$sealedPackage2 !== void 0 ? _galaxy$sealedPackage2 : galaxy.sealedPackages = [];
    (_galaxy$generalManife = galaxy.generalManifest) !== null && _galaxy$generalManife !== void 0 ? _galaxy$generalManife : galaxy.generalManifest = {
      version: 1,
      nextSequence: 0,
      entries: []
    };
    (_galaxy$routeCaches = galaxy.routeCaches) !== null && _galaxy$routeCaches !== void 0 ? _galaxy$routeCaches : galaxy.routeCaches = [];
    for (const cache of galaxy.routeCaches) {
      var _cache$packages;
      (_cache$packages = cache.packages) !== null && _cache$packages !== void 0 ? _cache$packages : cache.packages = [];
    }
    for (const courier of galaxy.couriers) if (courier.rivalId === undefined) delete courier.rivalId;
    for (const sector of galaxy.sectors) {
      if (Object.is(sector.x, -0)) sector.x = 0;
      if (Object.is(sector.y, -0)) sector.y = 0;
    }
    for (const contract of galaxy.contracts) {
      var _contract$deadlineRec;
      (_contract$deadlineRec = contract.deadlineReckoning) !== null && _contract$deadlineRec !== void 0 ? _contract$deadlineRec : contract.deadlineReckoning = routeReckoningFromLegacyDay(contract.deadlineDay);
      contract.deadlineDay = sectorDayFromRouteReckoning(contract.deadlineReckoning);
    }
    for (const contract of galaxy.sealedPackageContracts) {
      var _contract$offeredAtRo, _contract$acceptedAtR, _contract$destination, _contract$resolvedAtR, _contract$terms, _contract$terms$deadl;
      contract.version = 2;
      (_contract$offeredAtRo = contract.offeredAtRouteReckoning) !== null && _contract$offeredAtRo !== void 0 ? _contract$offeredAtRo : contract.offeredAtRouteReckoning = routeReckoningFromLegacyDay(contract.offeredAtSectorDay);
      (_contract$acceptedAtR = contract.acceptedAtRouteReckoning) !== null && _contract$acceptedAtR !== void 0 ? _contract$acceptedAtR : contract.acceptedAtRouteReckoning = contract.acceptedAtSectorDay === undefined ? undefined : routeReckoningFromLegacyDay(contract.acceptedAtSectorDay);
      (_contract$destination = contract.destinationReachedAtRouteReckoning) !== null && _contract$destination !== void 0 ? _contract$destination : contract.destinationReachedAtRouteReckoning = contract.destinationReachedAtSectorDay === undefined ? undefined : routeReckoningFromLegacyDay(contract.destinationReachedAtSectorDay);
      (_contract$resolvedAtR = contract.resolvedAtRouteReckoning) !== null && _contract$resolvedAtR !== void 0 ? _contract$resolvedAtR : contract.resolvedAtRouteReckoning = contract.resolvedAtSectorDay === undefined ? undefined : routeReckoningFromLegacyDay(contract.resolvedAtSectorDay);
      (_contract$terms$deadl = (_contract$terms = contract.terms).deadlineReckoning) !== null && _contract$terms$deadl !== void 0 ? _contract$terms$deadl : _contract$terms.deadlineReckoning = routeReckoningFromLegacyDay(contract.terms.deadlineDay);
      contract.terms.deadlineDay = sectorDayFromRouteReckoning(contract.terms.deadlineReckoning);
    }
    for (const packageRecord of galaxy.sealedPackages) packageRecord.version = 2;
    galaxy.events = galaxy.events.map(event => {
      const existingReckoning = event.routeReckoning;
      const eventReckoning = typeof existingReckoning === 'number' && Number.isInteger(existingReckoning) && existingReckoning >= 0 ? existingReckoning : routeReckoning;
      return {
        ...event,
        at: eventReckoning,
        routeReckoning: eventReckoning
      };
    });
    for (const site of Object.values(galaxy.sites)) {
      var _site$supplies, _site$salvage, _site$market;
      (_site$supplies = site.supplies) !== null && _site$supplies !== void 0 ? _site$supplies : site.supplies = 45;
      (_site$salvage = site.salvage) !== null && _site$salvage !== void 0 ? _site$salvage : site.salvage = 45;
      (_site$market = site.market) !== null && _site$market !== void 0 ? _site$market : site.market = {
        stock: {
          provisions: 12,
          components: 12,
          salvage: 12,
          biosamples: 12
        },
        demand: {
          provisions: 12,
          components: 12,
          salvage: 12,
          biosamples: 12
        },
        prices: {
          provisions: 10,
          components: 10,
          salvage: 10,
          biosamples: 10
        }
      };
      if (Object.is(site.x, -0)) site.x = 0;
      if (Object.is(site.y, -0)) site.y = 0;
      site.lastChangedAt = Number.isInteger(site.lastChangedAt) && site.lastChangedAt >= 0 && site.lastChangedAt <= routeReckoning ? site.lastChangedAt : routeReckoning;
    }
    for (const snapshot of Object.values(galaxy.siteSnapshots)) snapshot.savedAt = Number.isInteger(snapshot.savedAt) && snapshot.savedAt >= 0 && snapshot.savedAt <= routeReckoning ? snapshot.savedAt : routeReckoning;
    normalizeGeneralManifest(galaxy);
    if (!galaxy.sites[galaxy.activeSiteId] || !galaxy.couriers.some(courier => courier.id === galaxy.activeCourierId)) return undefined;
    return cloneGalaxy(galaxy);
  } catch {
    return undefined;
  }
};
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJiaW9tZU5hbWUiLCJybmdGb3IiLCJhcHBlbmRHZW5lcmFsTWFuaWZlc3QiLCJub3JtYWxpemVHZW5lcmFsTWFuaWZlc3QiLCJhZGRLZXN0cmVsU2VhbGVkUGFja2FnZU9mZmVyIiwiYXBwbHlTZWFsZWRQYWNrYWdlRGVhZGxpbmVUcmFuc2l0aW9ucyIsIlJPVVRFX1JFQ0tPTklOR19VTklUU19QRVJfQ1lDTEUiLCJST1VURV9SRUNLT05JTkdfV09STERfVElDS19VTklUUyIsInJvdXRlUmVja29uaW5nRnJvbUxlZ2FjeURheSIsInJvdXRlV29ybGRUaWNrRm9yIiwic2VjdG9yRGF5RnJvbVJvdXRlUmVja29uaW5nIiwiT1VURVJfU0VDVE9SX0NPVU5UIiwiU0lURVNfUEVSX1NFQ1RPUiIsIkdBTEFYWV9IT1VSX01TIiwiR0FMQVhZX1RJQ0tfTVMiLCJHQUxBWFlfT0ZGTElORV9DQVBfTVMiLCJCSU9NRVMiLCJzZWN0b3JOYW1lcyIsImNyZXdUZW1wbGF0ZXMiLCJuYW1lIiwicm9sZSIsIm9yaWdpbiIsImNhbGxpbmciLCJyb3V0aW5lIiwiZmFjdGlvbk5hbWVzIiwidm95YWdlciIsInNhbHZhZ2VycyIsInJlbGF5R3VpbGQiLCJ2b2lkYm9ybiIsInNldHRsZXJzIiwiZmFjdGlvbnMiLCJpZCIsImluZmx1ZW5jZSIsImRpc3Bvc2l0aW9uIiwic2l0ZUlkIiwic2VjdG9yIiwiaW5kZXgiLCJTdHJpbmciLCJwYWRTdGFydCIsInNlY3RvcklkIiwiaW50ZWdlckNvb3JkaW5hdGUiLCJ2YWx1ZSIsInJvdW5kZWQiLCJNYXRoIiwicm91bmQiLCJPYmplY3QiLCJpcyIsImNvcHlFdmVudCIsImV2ZW50IiwiY29weVNpdGUiLCJzaXRlIiwibGlua3MiLCJjb3B5U25hcHNob3QiLCJzbmFwc2hvdCIsInZlcnNpb24iLCJzYXZlZEF0IiwicnVuIiwic3RydWN0dXJlZENsb25lIiwic2l0ZU5hbWUiLCJiaW9tZSIsInJlcGxhY2UiLCJpbml0aWFsRmFjdGlvbiIsImNvdXJpZXJJZCIsImNhcmdvS2luZHMiLCJtYXJrZXRGb3IiLCJybmciLCJzdG9jayIsImRlbWFuZCIsInByaWNlcyIsImtpbmQiLCJpbnQiLCJjcmV3SGVybyIsInByaW1hcnkiLCJ0ZW1wbGF0ZSIsImhlcm8iLCJkZWF0aE1vZGUiLCJoZWFsdGgiLCJtYXhIZWFsdGgiLCJmb2N1cyIsIm1heEZvY3VzIiwiZ29sZCIsImludmVudG9yeSIsImVxdWlwbWVudCIsIm1haW5IYW5kIiwic3RhdHMiLCJzdHJlbmd0aCIsIm1heCIsImFnaWxpdHkiLCJ2aXRhbGl0eSIsImludGVsbGVjdCIsImNyZWF0ZUdhbGF4eSIsInNlZWQiLCJsZWdhY3lDcmVhdGVkQXQiLCJzZWN0b3JzIiwic2l0ZXMiLCJzZWN0b3JJbmRleCIsIl9zZWN0b3JOYW1lcyRzZWN0b3JJbiIsImFuZ2xlIiwiUEkiLCJkaXN0YW5jZSIsImZsb29yIiwic3FydCIsIm9yZGVyZWQiLCJzaHVmZmxlIiwiaWRzIiwibWFwIiwic2l0ZUluZGV4IiwiY3VycmVudElkIiwieCIsImNvcyIsInkiLCJzaW4iLCJkaXNjb3ZlcmVkIiwiY29tcGxldGVkIiwiY29udHJvbCIsImludGVncml0eSIsImVjb2xvZ3kiLCJjb25zdHJ1Y3Rpb24iLCJzdXBwbGllcyIsInNhbHZhZ2UiLCJtYXJrZXQiLCJsYXN0Q2hhbmdlZEF0IiwibGVuZ3RoIiwiY3VycmVudCIsImNvbm5lY3QiLCJvdGhlciIsImluY2x1ZGVzIiwicHVzaCIsInNpdGVJZHMiLCJzb3VyY2UiLCJkZXN0aW5hdGlvbiIsImFjdGl2ZUlkIiwicHJpbWFyeUNvdXJpZXIiLCJzdGF0dXMiLCJhZmZpbml0eSIsInBlcnNvbmFsSXRlbXMiLCJjb3VyaWVycyIsInJpdmFsSWQiLCJjcmVhdGVkQXQiLCJyb3V0ZVJlY2tvbmluZyIsImxhc3RXb3JsZFRpY2siLCJzZWN0b3JEYXkiLCJhY3RpdmVTaXRlSWQiLCJhY3RpdmVDb3VyaWVySWQiLCJldmVudHMiLCJhdCIsImhlYWRsaW5lIiwiZGV0YWlsIiwic2l0ZVNuYXBzaG90cyIsInNoYXJlZFN0YXNoIiwiY2FyZ28iLCJjb250cmFjdHMiLCJzZWFsZWRQYWNrYWdlQ29udHJhY3RzIiwic2VhbGVkUGFja2FnZXMiLCJnZW5lcmFsTWFuaWZlc3QiLCJuZXh0U2VxdWVuY2UiLCJlbnRyaWVzIiwicm91dGVDYWNoZXMiLCJjbG9uZUdhbGF4eSIsImdhbGF4eSIsImZyb21FbnRyaWVzIiwiY291cmllciIsImZhY3Rpb24iLCJjb250cmFjdCIsInRlcm1zIiwicHJvaGliaXRlZEFjdGlvbnMiLCJwYWNrYWdlUmVjb3JkIiwiZXh0ZXJpb3IiLCJyZXZlYWxlZENvbnRlbnRzIiwiZW50cnkiLCJjYWNoZSIsInBhY2thZ2VzIiwiYXBwZW5kRXZlbnQiLCJzb21lIiwiY2FuZGlkYXRlIiwidW5zaGlmdCIsInNsaWNlIiwiZXZlbnRJZCIsInRpY2siLCJzdWZmaXgiLCJjbGFtcCIsIm1pbiIsInNoaWZ0U2l0ZSIsImNoYW5nZSIsImJlZm9yZSIsIm1hbmlmZXN0IiwicGF5bG9hZCIsImFmdGVyIiwiZXZvbHZlQ291cmllcnMiLCJzb3J0IiwibGVmdCIsInJpZ2h0IiwibG9jYWxlQ29tcGFyZSIsInZhbHVlcyIsImNoYW5jZSIsImFkdmFuY2VHZW5lcmljQ29udHJhY3REZWFkbGluZXMiLCJfZ2FsYXh5JHNpdGVzJGNvbnRyYWMiLCJfZ2FsYXh5JHNpdGVzJGNvbnRyYWMyIiwiZGVhZGxpbmVSZWNrb25pbmciLCJmaWx0ZXIiLCJjb250cmFjdElkIiwiZGVzdGluYXRpb25TaXRlSWQiLCJ1bml0cyIsImNhcmdvS2luZCIsImFkdmFuY2VHYWxheHlSb3V0ZVJlY2tvbmluZyIsIm1hcmtzIiwic3RlcHMiLCJzdGVwIiwiY2FuZGlkYXRlcyIsInJlY29uY2lsZUdhbGF4eSIsIl9sZWdhY3lOb3ciLCJkaXNjb3ZlckxpbmtlZFNpdGVzIiwic291cmNlSWQiLCJmaW5kIiwic291cmNlU2l0ZUlkIiwiZmVlIiwiZGVhZGxpbmVEYXkiLCJmYWN0aW9uSWQiLCJjb2xsYXRlcmFsIiwic2V0QWN0aXZlR2FsYXh5U2l0ZSIsIl9nYWxheHkkc2l0ZXMkaWQiLCJzZXRDb3VyaWVyUm91dGluZSIsInNlbGVjdEdhbGF4eUNvdXJpZXIiLCJsb3NlR2FsYXh5Q291cmllciIsInNhdmVHYWxheHlTaXRlIiwic2l0ZUlkVmFsdWUiLCJmYWN0aW9uUHJlc3N1cmUiLCJhdHRhY2siLCJkZWZlbnNlIiwicmV3YXJkIiwibGFiZWwiLCJhcHBseUdhbGF4eVNpdGVDb25kaXRpb25zIiwiX3J1biRmbG9vciRkaWZmaWN1bHR5IiwicHJlc3N1cmUiLCJpbnRlZ3JpdHlQcmVzc3VyZSIsImVjb2xvZ3lQcmVzc3VyZSIsImhlYWx0aE11bHRpcGxpZXIiLCJhdHRhY2tCb251cyIsImRlZmVuc2VCb251cyIsImFjdG9yIiwiYWN0b3JzIiwiaG9zdGlsZSIsImRpZmZpY3VsdHkiLCJyb3V0ZVBvc2l0aW9uIiwidGhyZWF0IiwiZWxpdGVDaGFuY2UiLCJndWFyZGlhblBhdHRlcm4iLCJyZXdhcmRNdWx0aXBsaWVyIiwidGVycmFpbiIsInRpbGVzIiwidGlsZSIsInN0YXJ0Iiwid2lkdGgiLCJfIiwiZm9yRWFjaCIsIm1lc3NhZ2VzIiwicmVjb3JkR2FsYXh5TGFuZGluZyIsIl9ydW4kdGVsZW1ldHJ5JGtpbGxzIiwiX3J1biR0ZWxlbWV0cnkiLCJraWxscyIsInRlbGVtZXRyeSIsImdhbGF4eVJvdXRlTGVuZ3RoIiwiZnJvbVNpdGVJZCIsInRvU2l0ZUlkIiwiam9pbiIsImdhbGF4eVJvdXRlU2l0dWF0aW9uIiwiY2h1bmsiLCJvdXRjb21lcyIsInJlY29yZEdhbGF4eVJvdXRlU2l0dWF0aW9ucyIsImxpbmtJZCIsInNpdHVhdGlvbnMiLCJzaXR1YXRpb24iLCJyZXNvbHZlR2FsYXh5Um91dGVTaXR1YXRpb25zIiwiYWNjZXB0R2FsYXh5Q29udHJhY3QiLCJfZ2FsYXh5JHNpdGVzJHRvU2l0ZUkiLCJfZ2FsYXh5JHNpdGVzJHRvU2l0ZUkyIiwibWVzc2FnZSIsInJlZHVjZSIsInRvdGFsIiwic291cmNlU2l0ZSIsImRlbGl2ZXJHYWxheHlDb250cmFjdHMiLCJkZWxpdmVyYWJsZSIsImFiYW5kb25HYWxheHlDYXJnbyIsInJlY292ZXJlZCIsInJlY292ZXJHYWxheHlSb3V0ZUNhY2hlcyIsImNhY2hlcyIsImZsYXRNYXAiLCJjYXBhY2l0eSIsInVuZGVmaW5lZCIsImdhbGF4eVNuYXBzaG90IiwiYXZhaWxhYmxlR2FsYXh5U2l0ZXMiLCJnYWxheHlDaHJvbmljbGUiLCJpc1JlY29yZCIsIkFycmF5IiwiaXNBcnJheSIsIm1pZ3JhdGVHYWxheHkiLCJfZ2FsYXh5JGNhcmdvIiwiX2dhbGF4eSRjb250cmFjdHMiLCJfZ2FsYXh5JHNlYWxlZFBhY2thZ2UiLCJfZ2FsYXh5JHNlYWxlZFBhY2thZ2UyIiwiX2dhbGF4eSRnZW5lcmFsTWFuaWZlIiwiX2dhbGF4eSRyb3V0ZUNhY2hlcyIsImxlZ2FjeSIsImxlZ2FjeURheSIsIk51bWJlciIsImlzRmluaXRlIiwiaXNJbnRlZ2VyIiwiX2NhY2hlJHBhY2thZ2VzIiwiX2NvbnRyYWN0JGRlYWRsaW5lUmVjIiwiX2NvbnRyYWN0JG9mZmVyZWRBdFJvIiwiX2NvbnRyYWN0JGFjY2VwdGVkQXRSIiwiX2NvbnRyYWN0JGRlc3RpbmF0aW9uIiwiX2NvbnRyYWN0JHJlc29sdmVkQXRSIiwiX2NvbnRyYWN0JHRlcm1zIiwiX2NvbnRyYWN0JHRlcm1zJGRlYWRsIiwib2ZmZXJlZEF0Um91dGVSZWNrb25pbmciLCJvZmZlcmVkQXRTZWN0b3JEYXkiLCJhY2NlcHRlZEF0Um91dGVSZWNrb25pbmciLCJhY2NlcHRlZEF0U2VjdG9yRGF5IiwiZGVzdGluYXRpb25SZWFjaGVkQXRSb3V0ZVJlY2tvbmluZyIsImRlc3RpbmF0aW9uUmVhY2hlZEF0U2VjdG9yRGF5IiwicmVzb2x2ZWRBdFJvdXRlUmVja29uaW5nIiwicmVzb2x2ZWRBdFNlY3RvckRheSIsImV4aXN0aW5nUmVja29uaW5nIiwiZXZlbnRSZWNrb25pbmciLCJfc2l0ZSRzdXBwbGllcyIsIl9zaXRlJHNhbHZhZ2UiLCJfc2l0ZSRtYXJrZXQiLCJwcm92aXNpb25zIiwiY29tcG9uZW50cyIsImJpb3NhbXBsZXMiXSwic291cmNlcyI6WyJnYWxheHkudHMiXSwic291cmNlc0NvbnRlbnQiOlsiaW1wb3J0IHsgYmlvbWVOYW1lIH0gZnJvbSAnLi4vY29udGVudCdcbmltcG9ydCB7IHJuZ0ZvciB9IGZyb20gJy4uL3JuZydcbmltcG9ydCB0eXBlIHsgQmlvbWUsIENhcmdvS2luZCwgR2FsYXh5Q291cmllciwgR2FsYXh5Q291cmllclJvdXRpbmUsIEdhbGF4eUV2ZW50LCBHYWxheHlGYWN0aW9uLCBHYWxheHlGYWN0aW9uSWQsIEdhbGF4eU1hcmtldCwgR2FsYXh5U2l0ZSwgR2FsYXh5U2l0ZVNuYXBzaG90LCBHYWxheHlTdGF0ZSwgSGVybywgUnVuU3RhdGUgfSBmcm9tICcuLi90eXBlcydcbmltcG9ydCB7IGFwcGVuZEdlbmVyYWxNYW5pZmVzdCwgbm9ybWFsaXplR2VuZXJhbE1hbmlmZXN0IH0gZnJvbSAnLi9tYW5pZmVzdCdcbmltcG9ydCB7IGFkZEtlc3RyZWxTZWFsZWRQYWNrYWdlT2ZmZXIsIGFwcGx5U2VhbGVkUGFja2FnZURlYWRsaW5lVHJhbnNpdGlvbnMgfSBmcm9tICcuL3NlYWxlZC1wYWNrYWdlcydcbmltcG9ydCB7IFJPVVRFX1JFQ0tPTklOR19VTklUU19QRVJfQ1lDTEUsIFJPVVRFX1JFQ0tPTklOR19XT1JMRF9USUNLX1VOSVRTLCByb3V0ZVJlY2tvbmluZ0Zyb21MZWdhY3lEYXksIHJvdXRlV29ybGRUaWNrRm9yLCBzZWN0b3JEYXlGcm9tUm91dGVSZWNrb25pbmcgfSBmcm9tICcuL3JvdXRlLXJlY2tvbmluZydcblxuZXhwb3J0IGNvbnN0IE9VVEVSX1NFQ1RPUl9DT1VOVCA9IDIwXG5leHBvcnQgY29uc3QgU0lURVNfUEVSX1NFQ1RPUiA9IDEwXG4vKiogQGRlcHJlY2F0ZWQgTGVnYWN5IHdhbGwtY2xvY2sgY29uc3RhbnRzIHJldGFpbmVkIG9ubHkgZm9yIGNhbGxlcnMgbWlncmF0aW5nIHRvIFJvdXRlIFJlY2tvbmluZy4gKi9cbmV4cG9ydCBjb25zdCBHQUxBWFlfSE9VUl9NUyA9IDYwICogNjAgKiAxMDAwXG4vKiogQGRlcHJlY2F0ZWQgTGVnYWN5IHdhbGwtY2xvY2sgY29uc3RhbnRzIHJldGFpbmVkIG9ubHkgZm9yIGNhbGxlcnMgbWlncmF0aW5nIHRvIFJvdXRlIFJlY2tvbmluZy4gKi9cbmV4cG9ydCBjb25zdCBHQUxBWFlfVElDS19NUyA9IDYgKiBHQUxBWFlfSE9VUl9NU1xuLyoqIEBkZXByZWNhdGVkIExlZ2FjeSB3YWxsLWNsb2NrIGNvbnN0YW50cyByZXRhaW5lZCBvbmx5IGZvciBjYWxsZXJzIG1pZ3JhdGluZyB0byBSb3V0ZSBSZWNrb25pbmcuICovXG5leHBvcnQgY29uc3QgR0FMQVhZX09GRkxJTkVfQ0FQX01TID0gNyAqIDI0ICogR0FMQVhZX0hPVVJfTVNcblxuY29uc3QgQklPTUVTOiByZWFkb25seSBCaW9tZVtdID0gWydtaW5lJywgJ3dpbGRzJywgJ2NhdmVybnMnLCAncnVpbnMnLCAnZnVybmFjZScsICdmbG9vZGVkUnVpbnMnLCAnY2xpZmZzJywgJ2J1cmlhbCcsICdzYWx0RmxhdHMnLCAnZnJvc3RSZWxpcXVhcnknXVxuY29uc3Qgc2VjdG9yTmFtZXMgPSBbJ0hlbGlvcyBSZWFjaCcsICdBc3RlciBEcmlmdCcsICdDaW5kZXIgVmVyZ2UnLCAnTmVyaWRhIFZlaWwnLCAnT3Jpc29uIEJlbHQnLCAnS2VzdHJlbCBNYXJjaCcsICdWZXNwZXIgQXJyYXknLCAnSGFsY3lvbiBFeHBhbnNlJywgJ0JvcmVhbGlzIFdha2UnLCAnVGV0aHlzIEZvbGQnLCAnTW9ycm93IENoYWluJywgJ0x1bWVuIFNjYXInLCAnSWNhcnVzIFNoZWxmJywgJ1NhYmxlIE1lcmlkaWFuJywgJ1BhbGxhcyBSdW4nLCAnQ2FsZGVyYSBTcGFuJywgJ0Nyb3duIG9mIE1pcmEnLCAnUm9vayBOZWJ1bGEnLCAnTW9ycm93bGluZScsICdDYXJ0aGFnZSBEZWVwJywgJ05ldyBFZG8gRnJpbmdlJ11cbmNvbnN0IGNyZXdUZW1wbGF0ZXM6IFJlYWRvbmx5QXJyYXk8UGljazxHYWxheHlDb3VyaWVyLCAnbmFtZScgfCAncm9sZScgfCAnb3JpZ2luJyB8ICdjYWxsaW5nJyB8ICdyb3V0aW5lJz4+ID0gW1xuICB7IG5hbWU6ICdBcmkgVmVubicsIHJvbGU6ICdzZWN1cml0eSBzcGVjaWFsaXN0Jywgb3JpZ2luOiAnbWluZWJvcm4nLCBjYWxsaW5nOiAndHJhaWxndWFyZCcsIHJvdXRpbmU6ICdtYWludGFpbicgfSxcbiAgeyBuYW1lOiAnTWlrYSBTb2wnLCByb2xlOiAnc3lzdGVtcyBzcGVjaWFsaXN0Jywgb3JpZ2luOiAnbW9zc3dhbGtlcicsIGNhbGxpbmc6ICdwYXRobWFrZXInLCByb3V0aW5lOiAnc2NvdXQnIH0sXG4gIHsgbmFtZTogJ0NhdG8gUmVuJywgcm9sZTogJ3hlbm9hcmNoaXZpc3QnLCBvcmlnaW46ICdjYXZlcm5TZWVrZXInLCBjYWxsaW5nOiAnc3Bpcml0YmVhcmVyJywgcm91dGluZTogJ3Jlc2VhcmNoJyB9LFxuICB7IG5hbWU6ICdEYXJhIElseWFuJywgcm9sZTogJ2ZpZWxkIG1lZGljJywgb3JpZ2luOiAndGlkZWJvdW5kJywgY2FsbGluZzogJ3RyYWlsZ3VhcmQnLCByb3V0aW5lOiAncmVjb3ZlcicgfVxuXVxuY29uc3QgZmFjdGlvbk5hbWVzOiBSZWNvcmQ8R2FsYXh5RmFjdGlvbklkLCBzdHJpbmc+ID0geyB2b3lhZ2VyOiAnSm9tb24nLCBzYWx2YWdlcnM6ICdGcmVlIFNhbHZhZ2VycycsIHJlbGF5R3VpbGQ6ICdSZWxheSBHdWlsZCcsIHZvaWRib3JuOiAnVm9pZGJvcm4nLCBzZXR0bGVyczogJ05ldyBFZG8gU2V0dGxlcnMnIH1cbmNvbnN0IGZhY3Rpb25zID0gKCk6IEdhbGF4eUZhY3Rpb25bXSA9PiBbXG4gIHsgaWQ6ICd2b3lhZ2VyJywgbmFtZTogZmFjdGlvbk5hbWVzLnZveWFnZXIsIGluZmx1ZW5jZTogMTgsIGRpc3Bvc2l0aW9uOiA0MCB9LFxuICB7IGlkOiAnc2FsdmFnZXJzJywgbmFtZTogZmFjdGlvbk5hbWVzLnNhbHZhZ2VycywgaW5mbHVlbmNlOiAyNCwgZGlzcG9zaXRpb246IC04IH0sXG4gIHsgaWQ6ICdyZWxheUd1aWxkJywgbmFtZTogZmFjdGlvbk5hbWVzLnJlbGF5R3VpbGQsIGluZmx1ZW5jZTogMjcsIGRpc3Bvc2l0aW9uOiAxMiB9LFxuICB7IGlkOiAndm9pZGJvcm4nLCBuYW1lOiBmYWN0aW9uTmFtZXMudm9pZGJvcm4sIGluZmx1ZW5jZTogMTksIGRpc3Bvc2l0aW9uOiAtMjggfSxcbiAgeyBpZDogJ3NldHRsZXJzJywgbmFtZTogZmFjdGlvbk5hbWVzLnNldHRsZXJzLCBpbmZsdWVuY2U6IDEyLCBkaXNwb3NpdGlvbjogMjggfVxuXVxuXG5jb25zdCBzaXRlSWQgPSAoc2VjdG9yOiBudW1iZXIsIGluZGV4OiBudW1iZXIpID0+IGBzZWN0b3ItJHtTdHJpbmcoc2VjdG9yKS5wYWRTdGFydCgyLCAnMCcpfTpzaXRlLSR7U3RyaW5nKGluZGV4KS5wYWRTdGFydCgyLCAnMCcpfWBcbmNvbnN0IHNlY3RvcklkID0gKHNlY3RvcjogbnVtYmVyKSA9PiBgc2VjdG9yLSR7U3RyaW5nKHNlY3RvcikucGFkU3RhcnQoMiwgJzAnKX1gXG5jb25zdCBpbnRlZ2VyQ29vcmRpbmF0ZSA9ICh2YWx1ZTogbnVtYmVyKTogbnVtYmVyID0+IHtcbiAgY29uc3Qgcm91bmRlZCA9IE1hdGgucm91bmQodmFsdWUpXG4gIHJldHVybiBPYmplY3QuaXMocm91bmRlZCwgLTApID8gMCA6IHJvdW5kZWRcbn1cbmNvbnN0IGNvcHlFdmVudCA9IChldmVudDogR2FsYXh5RXZlbnQpOiBHYWxheHlFdmVudCA9PiAoeyAuLi5ldmVudCB9KVxuY29uc3QgY29weVNpdGUgPSAoc2l0ZTogR2FsYXh5U2l0ZSk6IEdhbGF4eVNpdGUgPT4gKHsgLi4uc2l0ZSwgbGlua3M6IFsuLi5zaXRlLmxpbmtzXSB9KVxuY29uc3QgY29weVNuYXBzaG90ID0gKHNuYXBzaG90OiBHYWxheHlTaXRlU25hcHNob3QpOiBHYWxheHlTaXRlU25hcHNob3QgPT4gKHsgdmVyc2lvbjogMSwgc2F2ZWRBdDogc25hcHNob3Quc2F2ZWRBdCwgcnVuOiBzdHJ1Y3R1cmVkQ2xvbmUoc25hcHNob3QucnVuKSB9KVxuXG5jb25zdCBzaXRlTmFtZSA9IChiaW9tZTogQmlvbWUsIHNlY3Rvcjogc3RyaW5nKTogc3RyaW5nID0+IGAke2Jpb21lTmFtZVtiaW9tZV0ucmVwbGFjZSgnIENvbG9ueScsICcnKX0gLy8gJHtzZWN0b3J9YFxuY29uc3QgaW5pdGlhbEZhY3Rpb24gPSAoc2VjdG9yOiBudW1iZXIsIGluZGV4OiBudW1iZXIpOiBHYWxheHlGYWN0aW9uSWQgPT4gKFsnc2FsdmFnZXJzJywgJ3JlbGF5R3VpbGQnLCAndm9pZGJvcm4nLCAnc2V0dGxlcnMnXSBhcyBjb25zdClbKHNlY3RvciAqIDcgKyBpbmRleCAqIDMpICUgNF0hXG5jb25zdCBjb3VyaWVySWQgPSAoaW5kZXg6IG51bWJlcikgPT4gYHZveWFnZXItY3Jldy0ke2luZGV4fWBcbmNvbnN0IGNhcmdvS2luZHM6IHJlYWRvbmx5IENhcmdvS2luZFtdID0gWydwcm92aXNpb25zJywgJ2NvbXBvbmVudHMnLCAnc2FsdmFnZScsICdiaW9zYW1wbGVzJ11cbmNvbnN0IG1hcmtldEZvciA9IChybmc6IFJldHVyblR5cGU8dHlwZW9mIHJuZ0Zvcj4pOiBHYWxheHlNYXJrZXQgPT4ge1xuICBjb25zdCBzdG9jayA9IHt9IGFzIEdhbGF4eU1hcmtldFsnc3RvY2snXTsgY29uc3QgZGVtYW5kID0ge30gYXMgR2FsYXh5TWFya2V0WydkZW1hbmQnXTsgY29uc3QgcHJpY2VzID0ge30gYXMgR2FsYXh5TWFya2V0WydwcmljZXMnXVxuICBmb3IgKGNvbnN0IGtpbmQgb2YgY2FyZ29LaW5kcykgeyBzdG9ja1traW5kXSA9IDggKyBybmcuaW50KDAsIDE4KTsgZGVtYW5kW2tpbmRdID0gNiArIHJuZy5pbnQoMCwgMTYpOyBwcmljZXNba2luZF0gPSA4ICsgcm5nLmludCgwLCAxMikgfVxuICByZXR1cm4geyBzdG9jaywgZGVtYW5kLCBwcmljZXMgfVxufVxuY29uc3QgY3Jld0hlcm8gPSAocHJpbWFyeTogSGVybywgdGVtcGxhdGU6IFBpY2s8R2FsYXh5Q291cmllciwgJ25hbWUnIHwgJ29yaWdpbicgfCAnY2FsbGluZyc+LCBpbmRleDogbnVtYmVyKTogSGVybyA9PiB7XG4gIGNvbnN0IGhlcm8gPSBzdHJ1Y3R1cmVkQ2xvbmUocHJpbWFyeSlcbiAgaGVyby5uYW1lID0gdGVtcGxhdGUubmFtZVxuICBoZXJvLm9yaWdpbiA9IHRlbXBsYXRlLm9yaWdpblxuICBoZXJvLmNhbGxpbmcgPSB0ZW1wbGF0ZS5jYWxsaW5nXG4gIGhlcm8uZGVhdGhNb2RlID0gJ2lyb25UcmFpbCdcbiAgaGVyby5oZWFsdGggPSBoZXJvLm1heEhlYWx0aFxuICBoZXJvLmZvY3VzID0gaGVyby5tYXhGb2N1c1xuICBoZXJvLmdvbGQgPSAwXG4gIGhlcm8uaW52ZW50b3J5ID0gaW5kZXggJSAyID8gWyd0b25pYycsICdyb2NrJywgJ3JvcGVCdW5kbGUnXSA6IFsnZm9jdXNUb25pYycsICdyb2NrJywgJ2JvbWJQYWNrJ11cbiAgaGVyby5lcXVpcG1lbnQgPSB7IG1haW5IYW5kOiBpbmRleCAlIDIgPyAnd2hpcCcgOiAndGlkZVNwZWFyJyB9XG4gIGhlcm8uc3RhdHMgPSB7IC4uLmhlcm8uc3RhdHMsIHN0cmVuZ3RoOiBNYXRoLm1heCgxLCBoZXJvLnN0YXRzLnN0cmVuZ3RoICsgKGluZGV4ID09PSAwID8gMSA6IDApKSwgYWdpbGl0eTogTWF0aC5tYXgoMSwgaGVyby5zdGF0cy5hZ2lsaXR5ICsgKGluZGV4ID09PSAxID8gMSA6IDApKSwgdml0YWxpdHk6IE1hdGgubWF4KDEsIGhlcm8uc3RhdHMudml0YWxpdHkgKyAoaW5kZXggPT09IDMgPyAxIDogMCkpLCBpbnRlbGxlY3Q6IE1hdGgubWF4KDEsIGhlcm8uc3RhdHMuaW50ZWxsZWN0ICsgKGluZGV4ID09PSAyID8gMSA6IDApKSB9XG4gIHJldHVybiBoZXJvXG59XG5cbmV4cG9ydCBjb25zdCBjcmVhdGVHYWxheHkgPSAoc2VlZDogbnVtYmVyLCBwcmltYXJ5OiBIZXJvLCBsZWdhY3lDcmVhdGVkQXQgPSAwKTogR2FsYXh5U3RhdGUgPT4ge1xuICBjb25zdCBzZWN0b3JzID0gW10gYXMgR2FsYXh5U3RhdGVbJ3NlY3RvcnMnXVxuICBjb25zdCBzaXRlczogUmVjb3JkPHN0cmluZywgR2FsYXh5U2l0ZT4gPSB7fVxuICBmb3IgKGxldCBzZWN0b3JJbmRleCA9IDA7IHNlY3RvckluZGV4IDw9IE9VVEVSX1NFQ1RPUl9DT1VOVDsgc2VjdG9ySW5kZXgrKykge1xuICAgIGNvbnN0IGlkID0gc2VjdG9ySWQoc2VjdG9ySW5kZXgpXG4gICAgY29uc3Qgcm5nID0gcm5nRm9yKHNlZWQsICdnYWxheHknLCAnc2VjdG9yJywgc2VjdG9ySW5kZXgpXG4gICAgY29uc3QgYW5nbGUgPSBybmcuaW50KDAsIDM1OSkgKiBNYXRoLlBJIC8gMTgwXG4gICAgY29uc3QgZGlzdGFuY2UgPSBzZWN0b3JJbmRleCA9PT0gMCA/IDAgOiA5ICsgTWF0aC5mbG9vcihNYXRoLnNxcnQoc2VjdG9ySW5kZXgpICogNykgKyBybmcuaW50KDAsIDUpXG4gICAgY29uc3QgbmFtZSA9IHNlY3Rvck5hbWVzW3NlY3RvckluZGV4XSA/PyBgVW5jYXRhbG9ndWVkICR7c2VjdG9ySW5kZXh9YFxuICAgIGNvbnN0IG9yZGVyZWQgPSBzZWN0b3JJbmRleCA9PT0gMCA/IFsuLi5CSU9NRVNdIDogcm5nLnNodWZmbGUoWy4uLkJJT01FU10pXG4gICAgY29uc3QgaWRzID0gb3JkZXJlZC5tYXAoKGJpb21lLCBzaXRlSW5kZXgpID0+IHtcbiAgICAgIGNvbnN0IGN1cnJlbnRJZCA9IHNpdGVJZChzZWN0b3JJbmRleCwgc2l0ZUluZGV4KVxuICAgICAgc2l0ZXNbY3VycmVudElkXSA9IHtcbiAgICAgICAgaWQ6IGN1cnJlbnRJZCwgc2VjdG9ySWQ6IGlkLCBuYW1lOiBzaXRlTmFtZShiaW9tZSwgbmFtZSksIGJpb21lLFxuICAgICAgICB4OiBpbnRlZ2VyQ29vcmRpbmF0ZShNYXRoLmNvcyhhbmdsZSkgKiBkaXN0YW5jZSAqIDEwKSArIChzaXRlSW5kZXggJSA1KSAqIDMsXG4gICAgICAgIHk6IGludGVnZXJDb29yZGluYXRlKE1hdGguc2luKGFuZ2xlKSAqIGRpc3RhbmNlICogNykgKyBNYXRoLmZsb29yKHNpdGVJbmRleCAvIDUpICogMyxcbiAgICAgICAgbGlua3M6IFtdLCBkaXNjb3ZlcmVkOiBzZWN0b3JJbmRleCA9PT0gMCAmJiBzaXRlSW5kZXggPT09IDAsIGNvbXBsZXRlZDogZmFsc2UsXG4gICAgICAgIGNvbnRyb2w6IGluaXRpYWxGYWN0aW9uKHNlY3RvckluZGV4LCBzaXRlSW5kZXgpLCBpbnRlZ3JpdHk6IDU1ICsgcm5nLmludCgwLCAzNSksIGVjb2xvZ3k6IDM1ICsgcm5nLmludCgwLCA0MCksIGNvbnN0cnVjdGlvbjogMCwgc3VwcGxpZXM6IDMwICsgcm5nLmludCgwLCA0NSksIHNhbHZhZ2U6IDI1ICsgcm5nLmludCgwLCA1NSksIG1hcmtldDogbWFya2V0Rm9yKHJuZyksIGxhc3RDaGFuZ2VkQXQ6IDBcbiAgICAgIH1cbiAgICAgIHJldHVybiBjdXJyZW50SWRcbiAgICB9KVxuICAgIGZvciAobGV0IHNpdGVJbmRleCA9IDA7IHNpdGVJbmRleCA8IGlkcy5sZW5ndGg7IHNpdGVJbmRleCsrKSB7XG4gICAgICBjb25zdCBjdXJyZW50ID0gc2l0ZXNbaWRzW3NpdGVJbmRleF0hXSFcbiAgICAgIGNvbnN0IGNvbm5lY3QgPSAob3RoZXI6IHN0cmluZyB8IHVuZGVmaW5lZCkgPT4geyBpZiAob3RoZXIgJiYgIWN1cnJlbnQubGlua3MuaW5jbHVkZXMob3RoZXIpKSBjdXJyZW50LmxpbmtzLnB1c2gob3RoZXIpIH1cbiAgICAgIGNvbm5lY3QoaWRzWyhzaXRlSW5kZXggKyAxKSAlIGlkcy5sZW5ndGhdKVxuICAgICAgY29ubmVjdChpZHNbKHNpdGVJbmRleCArIGlkcy5sZW5ndGggLSAxKSAlIGlkcy5sZW5ndGhdKVxuICAgICAgaWYgKHNpdGVJbmRleCAlIDIgPT09IDApIGNvbm5lY3QoaWRzWyhzaXRlSW5kZXggKyAzKSAlIGlkcy5sZW5ndGhdKVxuICAgIH1cbiAgICBzZWN0b3JzLnB1c2goeyBpZCwgbmFtZSwgeDogaW50ZWdlckNvb3JkaW5hdGUoTWF0aC5jb3MoYW5nbGUpICogZGlzdGFuY2UpLCB5OiBpbnRlZ2VyQ29vcmRpbmF0ZShNYXRoLnNpbihhbmdsZSkgKiBkaXN0YW5jZSksIGRpc2NvdmVyZWQ6IHNlY3RvckluZGV4ID09PSAwLCBzaXRlSWRzOiBpZHMgfSlcbiAgfVxuICBmb3IgKGxldCBzZWN0b3JJbmRleCA9IDA7IHNlY3RvckluZGV4IDwgT1VURVJfU0VDVE9SX0NPVU5UOyBzZWN0b3JJbmRleCsrKSB7XG4gICAgY29uc3Qgc291cmNlID0gc2l0ZXNbc2l0ZUlkKHNlY3RvckluZGV4LCA4KV0hXG4gICAgY29uc3QgZGVzdGluYXRpb24gPSBzaXRlSWQoc2VjdG9ySW5kZXggKyAxLCAxKVxuICAgIHNvdXJjZS5saW5rcy5wdXNoKGRlc3RpbmF0aW9uKVxuICAgIHNpdGVzW2Rlc3RpbmF0aW9uXSEubGlua3MucHVzaChzb3VyY2UuaWQpXG4gIH1cbiAgY29uc3QgYWN0aXZlSWQgPSBzaXRlSWQoMCwgMClcbiAgY29uc3QgcHJpbWFyeUNvdXJpZXI6IEdhbGF4eUNvdXJpZXIgPSB7IGlkOiBjb3VyaWVySWQoMCksIG5hbWU6IHByaW1hcnkubmFtZSwgcm9sZTogJ0pvbW9uIHNwZWNpYWxpc3QnLCBvcmlnaW46IHByaW1hcnkub3JpZ2luLCBjYWxsaW5nOiBwcmltYXJ5LmNhbGxpbmcsIHJvdXRpbmU6ICdzb2NpYWxpemUnLCBzdGF0dXM6ICdhdmFpbGFibGUnLCBhZmZpbml0eTogMTIsIHNpdGVJZDogYWN0aXZlSWQsIHBlcnNvbmFsSXRlbXM6IFtdLCBoZXJvOiBzdHJ1Y3R1cmVkQ2xvbmUocHJpbWFyeSkgfVxuICBjb25zdCBjb3VyaWVycyA9IFtwcmltYXJ5Q291cmllciwgLi4uY3Jld1RlbXBsYXRlcy5tYXAoKHRlbXBsYXRlLCBpbmRleCk6IEdhbGF4eUNvdXJpZXIgPT4gKHsgaWQ6IGNvdXJpZXJJZChpbmRleCArIDEpLCAuLi50ZW1wbGF0ZSwgc3RhdHVzOiAnYXZhaWxhYmxlJywgYWZmaW5pdHk6IChpbmRleCAlIDIgPyAtNCA6IDcpLCAuLi4oaW5kZXggPT09IDEgPyB7IHJpdmFsSWQ6IGNvdXJpZXJJZCgzKSB9IDoge30pLCBwZXJzb25hbEl0ZW1zOiBbXSwgaGVybzogY3Jld0hlcm8ocHJpbWFyeSwgdGVtcGxhdGUsIGluZGV4KSB9KSldXG4gIHJldHVybiBhZGRLZXN0cmVsU2VhbGVkUGFja2FnZU9mZmVyKHsgdmVyc2lvbjogMiwgc2VlZCwgLi4uKGxlZ2FjeUNyZWF0ZWRBdCA/IHsgY3JlYXRlZEF0OiBsZWdhY3lDcmVhdGVkQXQgfSA6IHt9KSwgcm91dGVSZWNrb25pbmc6IDAsIGxhc3RXb3JsZFRpY2s6IDAsIHNlY3RvckRheTogMCwgYWN0aXZlU2l0ZUlkOiBhY3RpdmVJZCwgYWN0aXZlQ291cmllcklkOiBwcmltYXJ5Q291cmllci5pZCwgc2VjdG9ycywgc2l0ZXMsIGNvdXJpZXJzLCBmYWN0aW9uczogZmFjdGlvbnMoKSwgZXZlbnRzOiBbeyBpZDogYGV2ZW50OiR7c2VlZH06YXJyaXZhbGAsIGF0OiAwLCByb3V0ZVJlY2tvbmluZzogMCwga2luZDogJ2Rpc2NvdmVyeScsIHNpdGVJZDogYWN0aXZlSWQsIGhlYWRsaW5lOiAnSm9tb24gZW50ZXJzIEhlbGlvcyBSZWFjaCcsIGRldGFpbDogJ0pvbW9uIGhhcyBhcnJpdmVkIGF0IGFuIHVuY2hhcnRlZCBmcm9udGllci4gT25seSBLZXN0cmVsIGlzIHJlYWNoYWJsZSB1bnRpbCBpdHMgbGFuZGluZyByb3V0ZXMgYXJlIHN1cnZleWVkLicgfV0sIHNpdGVTbmFwc2hvdHM6IHt9LCBzaGFyZWRTdGFzaDogW10sIGNhcmdvOiBbXSwgY29udHJhY3RzOiBbXSwgc2VhbGVkUGFja2FnZUNvbnRyYWN0czogW10sIHNlYWxlZFBhY2thZ2VzOiBbXSwgZ2VuZXJhbE1hbmlmZXN0OiB7IHZlcnNpb246IDIsIG5leHRTZXF1ZW5jZTogMCwgZW50cmllczogW10gfSwgcm91dGVDYWNoZXM6IFtdIH0pXG59XG5cbmV4cG9ydCBjb25zdCBjbG9uZUdhbGF4eSA9IChnYWxheHk6IEdhbGF4eVN0YXRlKTogR2FsYXh5U3RhdGUgPT4gKHsgLi4uZ2FsYXh5LCBzZWN0b3JzOiBnYWxheHkuc2VjdG9ycy5tYXAoc2VjdG9yID0+ICh7IC4uLnNlY3Rvciwgc2l0ZUlkczogWy4uLnNlY3Rvci5zaXRlSWRzXSB9KSksIHNpdGVzOiBPYmplY3QuZnJvbUVudHJpZXMoT2JqZWN0LmVudHJpZXMoZ2FsYXh5LnNpdGVzKS5tYXAoKFtpZCwgc2l0ZV0pID0+IFtpZCwgeyAuLi5jb3B5U2l0ZShzaXRlKSwgbWFya2V0OiBzdHJ1Y3R1cmVkQ2xvbmUoc2l0ZS5tYXJrZXQpIH1dKSksIGNvdXJpZXJzOiBnYWxheHkuY291cmllcnMubWFwKGNvdXJpZXIgPT4gKHsgLi4uY291cmllciwgcGVyc29uYWxJdGVtczogWy4uLmNvdXJpZXIucGVyc29uYWxJdGVtc10sIGhlcm86IHN0cnVjdHVyZWRDbG9uZShjb3VyaWVyLmhlcm8pIH0pKSwgZmFjdGlvbnM6IGdhbGF4eS5mYWN0aW9ucy5tYXAoZmFjdGlvbiA9PiAoeyAuLi5mYWN0aW9uIH0pKSwgZXZlbnRzOiBnYWxheHkuZXZlbnRzLm1hcChjb3B5RXZlbnQpLCBzaXRlU25hcHNob3RzOiBPYmplY3QuZnJvbUVudHJpZXMoT2JqZWN0LmVudHJpZXMoZ2FsYXh5LnNpdGVTbmFwc2hvdHMpLm1hcCgoW2lkLCBzbmFwc2hvdF0pID0+IFtpZCwgY29weVNuYXBzaG90KHNuYXBzaG90KV0pKSwgc2hhcmVkU3Rhc2g6IFsuLi5nYWxheHkuc2hhcmVkU3Rhc2hdLCBjYXJnbzogZ2FsYXh5LmNhcmdvLm1hcChjYXJnbyA9PiAoeyAuLi5jYXJnbyB9KSksIGNvbnRyYWN0czogZ2FsYXh5LmNvbnRyYWN0cy5tYXAoY29udHJhY3QgPT4gKHsgLi4uY29udHJhY3QsIGNhcmdvOiB7IC4uLmNvbnRyYWN0LmNhcmdvIH0gfSkpLCBzZWFsZWRQYWNrYWdlQ29udHJhY3RzOiBnYWxheHkuc2VhbGVkUGFja2FnZUNvbnRyYWN0cy5tYXAoY29udHJhY3QgPT4gKHsgLi4uY29udHJhY3QsIHRlcm1zOiB7IC4uLmNvbnRyYWN0LnRlcm1zLCBwcm9oaWJpdGVkQWN0aW9uczogWy4uLmNvbnRyYWN0LnRlcm1zLnByb2hpYml0ZWRBY3Rpb25zXSB9IH0pKSwgc2VhbGVkUGFja2FnZXM6IGdhbGF4eS5zZWFsZWRQYWNrYWdlcy5tYXAocGFja2FnZVJlY29yZCA9PiAoeyAuLi5wYWNrYWdlUmVjb3JkLCBleHRlcmlvcjogeyAuLi5wYWNrYWdlUmVjb3JkLmV4dGVyaW9yIH0sIC4uLihwYWNrYWdlUmVjb3JkLnJldmVhbGVkQ29udGVudHMgPyB7IHJldmVhbGVkQ29udGVudHM6IHsgLi4ucGFja2FnZVJlY29yZC5yZXZlYWxlZENvbnRlbnRzIH0gfSA6IHt9KSB9KSksIGdlbmVyYWxNYW5pZmVzdDogeyAuLi5nYWxheHkuZ2VuZXJhbE1hbmlmZXN0LCBlbnRyaWVzOiBnYWxheHkuZ2VuZXJhbE1hbmlmZXN0LmVudHJpZXMubWFwKGVudHJ5ID0+ICh7IC4uLmVudHJ5IH0pKSB9LCByb3V0ZUNhY2hlczogZ2FsYXh5LnJvdXRlQ2FjaGVzLm1hcChjYWNoZSA9PiAoeyAuLi5jYWNoZSwgY2FyZ286IGNhY2hlLmNhcmdvLm1hcChjYXJnbyA9PiAoeyAuLi5jYXJnbyB9KSksIHBhY2thZ2VzOiBbLi4uY2FjaGUucGFja2FnZXNdIH0pKSB9KVxuXG5jb25zdCBhcHBlbmRFdmVudCA9IChnYWxheHk6IEdhbGF4eVN0YXRlLCBldmVudDogR2FsYXh5RXZlbnQpOiB2b2lkID0+IHtcbiAgaWYgKGdhbGF4eS5ldmVudHMuc29tZShjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmlkID09PSBldmVudC5pZCkpIHJldHVyblxuICBnYWxheHkuZXZlbnRzLnVuc2hpZnQoZXZlbnQpXG4gIGdhbGF4eS5ldmVudHMgPSBnYWxheHkuZXZlbnRzLnNsaWNlKDAsIDI0MClcbn1cbmNvbnN0IGV2ZW50SWQgPSAoZ2FsYXh5OiBHYWxheHlTdGF0ZSwgdGljazogbnVtYmVyLCBzdWZmaXg6IHN0cmluZykgPT4gYGV2ZW50OiR7Z2FsYXh5LnNlZWR9OiR7dGlja306JHtzdWZmaXh9YFxuY29uc3QgY2xhbXAgPSAodmFsdWU6IG51bWJlciwgbWluID0gMCwgbWF4ID0gMTAwKTogbnVtYmVyID0+IE1hdGgubWF4KG1pbiwgTWF0aC5taW4obWF4LCB2YWx1ZSkpXG5jb25zdCBzaGlmdFNpdGUgPSAoZ2FsYXh5OiBHYWxheHlTdGF0ZSwgc2l0ZTogR2FsYXh5U2l0ZSwgdGljazogbnVtYmVyKTogdm9pZCA9PiB7XG4gIGNvbnN0IHJuZyA9IHJuZ0ZvcihnYWxheHkuc2VlZCwgJ2dhbGF4eScsICd0aWNrJywgdGljaywgc2l0ZS5pZClcbiAgY29uc3QgY2hhbmdlID0gcm5nLmludCgtMTIsIDEyKVxuICBjb25zdCBraW5kID0gcm5nLmludCgwLCA0KVxuICBjb25zdCBiZWZvcmUgPSB7IGNvbnRyb2w6IHNpdGUuY29udHJvbCwgaW50ZWdyaXR5OiBzaXRlLmludGVncml0eSwgZWNvbG9neTogc2l0ZS5lY29sb2d5LCBjb25zdHJ1Y3Rpb246IHNpdGUuY29uc3RydWN0aW9uLCBzdXBwbGllczogc2l0ZS5zdXBwbGllcyB9XG4gIGlmIChraW5kID09PSAwKSB7XG4gICAgc2l0ZS5pbnRlZ3JpdHkgPSBjbGFtcChzaXRlLmludGVncml0eSArIGNoYW5nZSlcbiAgICBhcHBlbmRFdmVudChnYWxheHksIHsgaWQ6IGV2ZW50SWQoZ2FsYXh5LCB0aWNrLCBgJHtzaXRlLmlkfTppbnRlZ3JpdHlgKSwgYXQ6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZywgcm91dGVSZWNrb25pbmc6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZywga2luZDogJ2Vjb2xvZ3knLCBzaXRlSWQ6IHNpdGUuaWQsIGhlYWRsaW5lOiBgJHtzaXRlLm5hbWV9OiB0ZXJyYWluIHNoaWZ0ZWRgLCBkZXRhaWw6IGNoYW5nZSA8IDAgPyAnQSByZWdpb25hbCBkaXN0dXJiYW5jZSBkYW1hZ2VkIGtub3duIHJvdXRlcy4nIDogJ0xvY2FsIGNyZXdzIHN0YWJpbGl6ZWQgcGFydCBvZiB0aGUgdGVycmFpbi4nIH0pXG4gIH0gZWxzZSBpZiAoa2luZCA9PT0gMSkge1xuICAgIHNpdGUuZWNvbG9neSA9IGNsYW1wKHNpdGUuZWNvbG9neSArIGNoYW5nZSlcbiAgICBhcHBlbmRFdmVudChnYWxheHksIHsgaWQ6IGV2ZW50SWQoZ2FsYXh5LCB0aWNrLCBgJHtzaXRlLmlkfTplY29sb2d5YCksIGF0OiBnYWxheHkucm91dGVSZWNrb25pbmcsIHJvdXRlUmVja29uaW5nOiBnYWxheHkucm91dGVSZWNrb25pbmcsIGtpbmQ6ICdlY29sb2d5Jywgc2l0ZUlkOiBzaXRlLmlkLCBoZWFkbGluZTogYCR7c2l0ZS5uYW1lfTogZWNvbG9naWNhbCBjaGFuZ2VgLCBkZXRhaWw6IGNoYW5nZSA8IDAgPyAnQSBob3N0aWxlIGJsb29tIHNwcmVhZCB0aHJvdWdoIHRoZSBzaXRlLicgOiAnQSBkb3JtYW50IGhhYml0YXQgcmVjb3ZlcmVkLicgfSlcbiAgfSBlbHNlIGlmIChraW5kID09PSAyKSB7XG4gICAgY29uc3QgY29udHJvbCA9IChbJ3NhbHZhZ2VycycsICdyZWxheUd1aWxkJywgJ3ZvaWRib3JuJywgJ3NldHRsZXJzJ10gYXMgY29uc3QpW3JuZy5pbnQoMCwgMyldIVxuICAgIGlmIChjb250cm9sICE9PSBzaXRlLmNvbnRyb2wpIHtcbiAgICAgIHNpdGUuY29udHJvbCA9IGNvbnRyb2xcbiAgICAgIGFwcGVuZEV2ZW50KGdhbGF4eSwgeyBpZDogZXZlbnRJZChnYWxheHksIHRpY2ssIGAke3NpdGUuaWR9OnRlcnJpdG9yeWApLCBhdDogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCByb3V0ZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCBraW5kOiAndGVycml0b3J5Jywgc2l0ZUlkOiBzaXRlLmlkLCBoZWFkbGluZTogYCR7c2l0ZS5uYW1lfTogY29udHJvbCBjaGFuZ2VkYCwgZGV0YWlsOiBgJHtmYWN0aW9uTmFtZXNbY29udHJvbF19IG5vdyBob2xkcyB0aGUgdmlzaWJsZSBhcHByb2FjaGVzLmAgfSlcbiAgICB9XG4gIH0gZWxzZSB7XG4gICAgc2l0ZS5jb25zdHJ1Y3Rpb24gPSBjbGFtcChzaXRlLmNvbnN0cnVjdGlvbiArIGNoYW5nZSlcbiAgICBzaXRlLnN1cHBsaWVzID0gY2xhbXAoc2l0ZS5zdXBwbGllcyArIHJuZy5pbnQoLTgsIDEyKSlcbiAgICBzaXRlLnNhbHZhZ2UgPSBjbGFtcChzaXRlLnNhbHZhZ2UgKyBybmcuaW50KC0xMCwgMTApKVxuICAgIGFwcGVuZEV2ZW50KGdhbGF4eSwgeyBpZDogZXZlbnRJZChnYWxheHksIHRpY2ssIGAke3NpdGUuaWR9OmNvbnN0cnVjdGlvbmApLCBhdDogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCByb3V0ZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCBraW5kOiAnY29uc3RydWN0aW9uJywgc2l0ZUlkOiBzaXRlLmlkLCBoZWFkbGluZTogYCR7c2l0ZS5uYW1lfTogY29uc3RydWN0aW9uIGNoYW5nZWRgLCBkZXRhaWw6ICdBdXRvbm9tb3VzIGNyZXdzIGFsdGVyZWQgdGhlIHNpdGUgd2hpbGUgSm9tb24gd2FzIGVsc2V3aGVyZS4nIH0pXG4gIH1cbiAgc2l0ZS5sYXN0Q2hhbmdlZEF0ID0gZ2FsYXh5LnJvdXRlUmVja29uaW5nXG4gIGZvciAoY29uc3Qga2luZCBvZiBjYXJnb0tpbmRzKSB7XG4gICAgY29uc3QgbWFya2V0ID0gc2l0ZS5tYXJrZXRcbiAgICBtYXJrZXQuc3RvY2tba2luZF0gPSBjbGFtcChtYXJrZXQuc3RvY2tba2luZF0gKyBybmcuaW50KC0zLCA0KSlcbiAgICBtYXJrZXQuZGVtYW5kW2tpbmRdID0gY2xhbXAobWFya2V0LmRlbWFuZFtraW5kXSArIHJuZy5pbnQoLTIsIDMpKVxuICAgIG1hcmtldC5wcmljZXNba2luZF0gPSBjbGFtcCg4ICsgTWF0aC5yb3VuZCgobWFya2V0LmRlbWFuZFtraW5kXSAtIG1hcmtldC5zdG9ja1traW5kXSkgLyAzKSArIChzaXRlLmNvbnRyb2wgPT09ICd2b2lkYm9ybicgPyA0IDogMCksIDMsIDMwKVxuICB9XG4gIGNvbnN0IG1hbmlmZXN0ID0gKGtpbmQ6IFBhcmFtZXRlcnM8dHlwZW9mIGFwcGVuZEdlbmVyYWxNYW5pZmVzdD5bMV1bJ2tpbmQnXSwgZGV0YWlsOiBzdHJpbmcsIHBheWxvYWQ6IFJlY29yZDxzdHJpbmcsIG51bWJlciB8IHN0cmluZz4pID0+IGFwcGVuZEdlbmVyYWxNYW5pZmVzdChnYWxheHksIHsga2luZCwgZGV0YWlsLCBzb3VyY2U6ICdzaXRlJywgc2l0ZUlkOiBzaXRlLmlkLCBwYXlsb2FkIH0pXG4gIGlmIChiZWZvcmUuc3VwcGxpZXMgPj0gMjUgJiYgc2l0ZS5zdXBwbGllcyA8IDI1KSBtYW5pZmVzdCgnc2l0ZVN1cHBseUNyaXNpcycsIGAke3NpdGUubmFtZX0gZW50ZXJlZCBhIHN1cHBseSBjcmlzaXMuYCwgeyBiZWZvcmU6IGJlZm9yZS5zdXBwbGllcywgYWZ0ZXI6IHNpdGUuc3VwcGxpZXMgfSlcbiAgZWxzZSBpZiAoYmVmb3JlLnN1cHBsaWVzIDwgMjUgJiYgc2l0ZS5zdXBwbGllcyA+PSAyNSkgbWFuaWZlc3QoJ3NpdGVTdXBwbHlSZWNvdmVyeScsIGAke3NpdGUubmFtZX0gcmVjb3ZlcmVkIGFib3ZlIGl0cyBzdXBwbHktY3Jpc2lzIHRocmVzaG9sZC5gLCB7IGJlZm9yZTogYmVmb3JlLnN1cHBsaWVzLCBhZnRlcjogc2l0ZS5zdXBwbGllcyB9KVxuICBpZiAoYmVmb3JlLmludGVncml0eSA+PSA0MCAmJiBzaXRlLmludGVncml0eSA8IDQwKSBtYW5pZmVzdCgnc2l0ZUludGVncml0eURlZ3JhZGVkJywgYCR7c2l0ZS5uYW1lfSBhcHByb2FjaCBpbnRlZ3JpdHkgZGVncmFkZWQuYCwgeyBiZWZvcmU6IGJlZm9yZS5pbnRlZ3JpdHksIGFmdGVyOiBzaXRlLmludGVncml0eSB9KVxuICBlbHNlIGlmIChiZWZvcmUuaW50ZWdyaXR5IDwgNDAgJiYgc2l0ZS5pbnRlZ3JpdHkgPj0gNDApIG1hbmlmZXN0KCdzaXRlSW50ZWdyaXR5UmVjb3ZlcmVkJywgYCR7c2l0ZS5uYW1lfSBhcHByb2FjaCBpbnRlZ3JpdHkgcmVjb3ZlcmVkLmAsIHsgYmVmb3JlOiBiZWZvcmUuaW50ZWdyaXR5LCBhZnRlcjogc2l0ZS5pbnRlZ3JpdHkgfSlcbiAgaWYgKChiZWZvcmUuZWNvbG9neSA+PSAzNSAmJiBzaXRlLmVjb2xvZ3kgPCAzNSkgfHwgKGJlZm9yZS5lY29sb2d5IDwgMzUgJiYgc2l0ZS5lY29sb2d5ID49IDM1KSkgbWFuaWZlc3QoJ3NpdGVFY29sb2d5U2hpZnQnLCBgJHtzaXRlLm5hbWV9IGVjb2xvZ2ljYWwgb3BlcmF0aW5nIGNvbmRpdGlvbnMgc2hpZnRlZC5gLCB7IGJlZm9yZTogYmVmb3JlLmVjb2xvZ3ksIGFmdGVyOiBzaXRlLmVjb2xvZ3kgfSlcbiAgaWYgKGJlZm9yZS5jb25zdHJ1Y3Rpb24gPCA4MCAmJiBzaXRlLmNvbnN0cnVjdGlvbiA+PSA4MCkgbWFuaWZlc3QoJ3NpdGVDb25zdHJ1Y3Rpb25Db21wbGV0ZWQnLCBgJHtzaXRlLm5hbWV9IGNvbnN0cnVjdGlvbiByZWFjaGVkIG9wZXJhdGlvbmFsIGNvbXBsZXRpb24uYCwgeyBiZWZvcmU6IGJlZm9yZS5jb25zdHJ1Y3Rpb24sIGFmdGVyOiBzaXRlLmNvbnN0cnVjdGlvbiB9KVxuICBlbHNlIGlmIChiZWZvcmUuY29uc3RydWN0aW9uID49IDI1ICYmIHNpdGUuY29uc3RydWN0aW9uIDwgMjUpIG1hbmlmZXN0KCdzaXRlQ29uc3RydWN0aW9uTG9zdCcsIGAke3NpdGUubmFtZX0gbG9zdCBvcGVyYXRpb25hbCBjb25zdHJ1Y3Rpb24gY2FwYWNpdHkuYCwgeyBiZWZvcmU6IGJlZm9yZS5jb25zdHJ1Y3Rpb24sIGFmdGVyOiBzaXRlLmNvbnN0cnVjdGlvbiB9KVxuICBpZiAoYmVmb3JlLmNvbnRyb2wgIT09IHNpdGUuY29udHJvbCkgbWFuaWZlc3QoJ3NpdGVDb250cm9sQ2hhbmdlZCcsIGAke3NpdGUubmFtZX0gaXMgbm93IGNvbnRyb2xsZWQgYnkgJHtmYWN0aW9uTmFtZXNbc2l0ZS5jb250cm9sXX0uYCwgeyBiZWZvcmU6IGJlZm9yZS5jb250cm9sLCBhZnRlcjogc2l0ZS5jb250cm9sIH0pXG59XG5jb25zdCBldm9sdmVDb3VyaWVycyA9IChnYWxheHk6IEdhbGF4eVN0YXRlLCB0aWNrOiBudW1iZXIpOiB2b2lkID0+IHtcbiAgY29uc3Qgcm5nID0gcm5nRm9yKGdhbGF4eS5zZWVkLCAnZ2FsYXh5JywgJ2NyZXcnLCB0aWNrKVxuICBmb3IgKGNvbnN0IGNvdXJpZXIgb2YgWy4uLmdhbGF4eS5jb3VyaWVyc10uc29ydCgobGVmdCwgcmlnaHQpID0+IGxlZnQuaWQubG9jYWxlQ29tcGFyZShyaWdodC5pZCkpKSB7XG4gICAgaWYgKGNvdXJpZXIuc3RhdHVzID09PSAnZGVhZCcgfHwgY291cmllci5zdGF0dXMgPT09ICdyZXRpcmVkJykgY29udGludWVcbiAgICBpZiAoY291cmllci5pZCA9PT0gZ2FsYXh5LmFjdGl2ZUNvdXJpZXJJZCkgY29udGludWVcbiAgICBjb3VyaWVyLmFmZmluaXR5ID0gY2xhbXAoY291cmllci5hZmZpbml0eSArIHJuZy5pbnQoLTMsIDMpLCAtMTAwLCAxMDApXG4gICAgaWYgKGNvdXJpZXIucm91dGluZSA9PT0gJ3RyYWRlJykge1xuICAgICAgY29uc3Qgc2l0ZXMgPSBPYmplY3QudmFsdWVzKGdhbGF4eS5zaXRlcykuc29ydCgobGVmdCwgcmlnaHQpID0+IGxlZnQuaWQubG9jYWxlQ29tcGFyZShyaWdodC5pZCkpXG4gICAgICBjb25zdCBzaXRlID0gc2l0ZXNbcm5nLmludCgwLCBzaXRlcy5sZW5ndGggLSAxKV1cbiAgICAgIGlmIChzaXRlKSB7XG4gICAgICAgIGNvbnN0IGtpbmQgPSBjYXJnb0tpbmRzW3JuZy5pbnQoMCwgY2FyZ29LaW5kcy5sZW5ndGggLSAxKV0hXG4gICAgICAgIHNpdGUubWFya2V0LnN0b2NrW2tpbmRdID0gY2xhbXAoc2l0ZS5tYXJrZXQuc3RvY2tba2luZF0gKyBybmcuaW50KDEsIDQpKVxuICAgICAgICBzaXRlLm1hcmtldC5kZW1hbmRba2luZF0gPSBjbGFtcChzaXRlLm1hcmtldC5kZW1hbmRba2luZF0gLSAxKVxuICAgICAgICBhcHBlbmRFdmVudChnYWxheHksIHsgaWQ6IGV2ZW50SWQoZ2FsYXh5LCB0aWNrLCBgJHtjb3VyaWVyLmlkfTp0cmFkZWApLCBhdDogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCByb3V0ZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCBraW5kOiAnY29uc3RydWN0aW9uJywgc2l0ZUlkOiBzaXRlLmlkLCBjb3VyaWVySWQ6IGNvdXJpZXIuaWQsIGhlYWRsaW5lOiBgJHtjb3VyaWVyLm5hbWV9IGNvbXBsZXRlZCBhIHRyYWRlIHN1cnZleWAsIGRldGFpbDogYCR7Y291cmllci5uYW1lfSBpbXByb3ZlZCAke2tpbmR9IGF2YWlsYWJpbGl0eSBhdCAke3NpdGUubmFtZX07IHRoZSBKb21vbiBob2xkIHdhcyBub3QgYWx0ZXJlZC5gIH0pXG4gICAgICB9XG4gICAgfVxuICAgIGlmIChybmcuY2hhbmNlKDQpKSB7XG4gICAgICBjb3VyaWVyLnN0YXR1cyA9ICdpbmp1cmVkJ1xuICAgICAgYXBwZW5kRXZlbnQoZ2FsYXh5LCB7IGlkOiBldmVudElkKGdhbGF4eSwgdGljaywgYCR7Y291cmllci5pZH06aW5qdXJlZGApLCBhdDogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCByb3V0ZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCBraW5kOiAnbG9zcycsIGNvdXJpZXJJZDogY291cmllci5pZCwgaGVhZGxpbmU6IGAke2NvdXJpZXIubmFtZX0gd2FzIGluanVyZWQgb2ZmLXdhdGNoYCwgZGV0YWlsOiBgJHtjb3VyaWVyLm5hbWV9J3MgJHtjb3VyaWVyLnJvdXRpbmV9IHNoaWZ0IGVuY291bnRlcmVkIGEgc2VjdG9yIGhhemFyZC5gIH0pXG4gICAgICBhcHBlbmRHZW5lcmFsTWFuaWZlc3QoZ2FsYXh5LCB7IGtpbmQ6ICdjb3VyaWVyU3RhdHVzQ2hhbmdlZCcsIGRldGFpbDogYCR7Y291cmllci5uYW1lfSB3YXMgaW5qdXJlZCB3aGlsZSBvZmYtd2F0Y2guYCwgc291cmNlOiAnY291cmllcicsIGNvdXJpZXJJZDogY291cmllci5pZCwgcGF5bG9hZDogeyBzdGF0dXM6ICdpbmp1cmVkJywgcm91dGluZTogY291cmllci5yb3V0aW5lIH0gfSlcbiAgICB9IGVsc2UgaWYgKHJuZy5jaGFuY2UoMSkpIHtcbiAgICAgIGNvdXJpZXIuc3RhdHVzID0gJ2RlYWQnXG4gICAgICBhcHBlbmRFdmVudChnYWxheHksIHsgaWQ6IGV2ZW50SWQoZ2FsYXh5LCB0aWNrLCBgJHtjb3VyaWVyLmlkfTpsb3N0YCksIGF0OiBnYWxheHkucm91dGVSZWNrb25pbmcsIHJvdXRlUmVja29uaW5nOiBnYWxheHkucm91dGVSZWNrb25pbmcsIGtpbmQ6ICdsb3NzJywgY291cmllcklkOiBjb3VyaWVyLmlkLCBoZWFkbGluZTogYCR7Y291cmllci5uYW1lfSBpcyBsb3N0YCwgZGV0YWlsOiBgVGhlaXIgZmluYWwgYXV0b25vbW91cyAke2NvdXJpZXIucm91dGluZX0gc2hpZnQgaXMgbm93IHBhcnQgb2YgdGhlIEpvbW9uIGNocm9uaWNsZS5gIH0pXG4gICAgICBhcHBlbmRHZW5lcmFsTWFuaWZlc3QoZ2FsYXh5LCB7IGtpbmQ6ICdjb3VyaWVyU3RhdHVzQ2hhbmdlZCcsIGRldGFpbDogYCR7Y291cmllci5uYW1lfSBpcyBsb3N0IGR1cmluZyBhbiBhdXRvbm9tb3VzICR7Y291cmllci5yb3V0aW5lfSBzaGlmdC5gLCBzb3VyY2U6ICdjb3VyaWVyJywgY291cmllcklkOiBjb3VyaWVyLmlkLCBwYXlsb2FkOiB7IHN0YXR1czogJ2RlYWQnLCByb3V0aW5lOiBjb3VyaWVyLnJvdXRpbmUgfSB9KVxuICAgIH1cbiAgfVxufVxuXG5jb25zdCBhZHZhbmNlR2VuZXJpY0NvbnRyYWN0RGVhZGxpbmVzID0gKGdhbGF4eTogR2FsYXh5U3RhdGUpOiB2b2lkID0+IHtcbiAgZm9yIChjb25zdCBjb250cmFjdCBvZiBnYWxheHkuY29udHJhY3RzKSB7XG4gICAgaWYgKChjb250cmFjdC5zdGF0dXMgIT09ICdvcGVuJyAmJiBjb250cmFjdC5zdGF0dXMgIT09ICdhY3RpdmUnKSB8fCBjb250cmFjdC5kZWFkbGluZVJlY2tvbmluZyA+PSBnYWxheHkucm91dGVSZWNrb25pbmcpIGNvbnRpbnVlXG4gICAgY29udHJhY3Quc3RhdHVzID0gJ2ZhaWxlZCdcbiAgICBnYWxheHkuY2FyZ28gPSBnYWxheHkuY2FyZ28uZmlsdGVyKGNhcmdvID0+IGNhcmdvLmNvbnRyYWN0SWQgIT09IGNvbnRyYWN0LmlkKVxuICAgIGFwcGVuZEV2ZW50KGdhbGF4eSwgeyBpZDogYGV2ZW50OiR7Z2FsYXh5LnNlZWR9OmNvbnRyYWN0OiR7Y29udHJhY3QuaWR9OmV4cGlyZWRgLCBhdDogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCByb3V0ZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCBraW5kOiAndHJhZGUnLCBzaXRlSWQ6IGNvbnRyYWN0LmRlc3RpbmF0aW9uU2l0ZUlkLCBoZWFkbGluZTogJ0NvbnRyYWN0IHdpbmRvdyBleHBpcmVkJywgZGV0YWlsOiBgJHtjb250cmFjdC5jYXJnby51bml0c30gdW5pdHMgb2YgJHtjb250cmFjdC5jYXJnby5raW5kfSBhcmUgbm8gbG9uZ2VyIGFjY2VwdGVkIGF0ICR7Z2FsYXh5LnNpdGVzW2NvbnRyYWN0LmRlc3RpbmF0aW9uU2l0ZUlkXT8ubmFtZSA/PyBjb250cmFjdC5kZXN0aW5hdGlvblNpdGVJZH0uYCB9KVxuICAgIGFwcGVuZEdlbmVyYWxNYW5pZmVzdChnYWxheHksIHsga2luZDogJ2NvbnRyYWN0RXhwaXJlZCcsIGRldGFpbDogYExlZ2FjeSBjYXJnbyBjb250cmFjdCAke2NvbnRyYWN0LmlkfSBleHBpcmVkLmAsIHNvdXJjZTogJ2NvbnRyYWN0JywgY29udHJhY3RJZDogY29udHJhY3QuaWQsIHNpdGVJZDogY29udHJhY3QuZGVzdGluYXRpb25TaXRlSWQsIHBheWxvYWQ6IHsgY2FyZ29LaW5kOiBjb250cmFjdC5jYXJnby5raW5kLCB1bml0czogY29udHJhY3QuY2FyZ28udW5pdHMgfSB9KVxuICB9XG59XG5cbi8qKiBBZHZhbmNlcyBjYW5vbmljYWwgc3RhdGUgYnkgaW50ZWdlciBSb3V0ZSBSZWNrb25pbmcgbWFya3Mgb25seS4gKi9cbmV4cG9ydCBjb25zdCBhZHZhbmNlR2FsYXh5Um91dGVSZWNrb25pbmcgPSAoc291cmNlOiBHYWxheHlTdGF0ZSwgbWFya3M6IG51bWJlcik6IEdhbGF4eVN0YXRlID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY2xvbmVHYWxheHkoc291cmNlKVxuICBjb25zdCBzdGVwcyA9IE1hdGgubWF4KDAsIE1hdGguZmxvb3IobWFya3MpKVxuICBmb3IgKGxldCBzdGVwID0gMDsgc3RlcCA8IHN0ZXBzOyBzdGVwKyspIHtcbiAgICBnYWxheHkucm91dGVSZWNrb25pbmcrK1xuICAgIGdhbGF4eS5zZWN0b3JEYXkgPSBzZWN0b3JEYXlGcm9tUm91dGVSZWNrb25pbmcoZ2FsYXh5LnJvdXRlUmVja29uaW5nKVxuICAgIGFwcGx5U2VhbGVkUGFja2FnZURlYWRsaW5lVHJhbnNpdGlvbnMoZ2FsYXh5KVxuICAgIGFkdmFuY2VHZW5lcmljQ29udHJhY3REZWFkbGluZXMoZ2FsYXh5KVxuICAgIGlmIChnYWxheHkucm91dGVSZWNrb25pbmcgJSBST1VURV9SRUNLT05JTkdfV09STERfVElDS19VTklUUyAhPT0gMCkgY29udGludWVcbiAgICBjb25zdCB0aWNrID0gcm91dGVXb3JsZFRpY2tGb3IoZ2FsYXh5LnJvdXRlUmVja29uaW5nKVxuICAgIGlmICh0aWNrIDw9IGdhbGF4eS5sYXN0V29ybGRUaWNrKSBjb250aW51ZVxuICAgIGdhbGF4eS5sYXN0V29ybGRUaWNrID0gdGlja1xuICAgIGNvbnN0IGNhbmRpZGF0ZXMgPSBPYmplY3QudmFsdWVzKGdhbGF4eS5zaXRlcykuc29ydCgobGVmdCwgcmlnaHQpID0+IGxlZnQuaWQubG9jYWxlQ29tcGFyZShyaWdodC5pZCkpXG4gICAgY29uc3Qgc2l0ZSA9IGNhbmRpZGF0ZXNbcm5nRm9yKGdhbGF4eS5zZWVkLCAnZ2FsYXh5JywgJ3NpdGUnLCB0aWNrKS5pbnQoMCwgY2FuZGlkYXRlcy5sZW5ndGggLSAxKV1cbiAgICBpZiAoc2l0ZSkgc2hpZnRTaXRlKGdhbGF4eSwgc2l0ZSwgdGljaylcbiAgICBldm9sdmVDb3VyaWVycyhnYWxheHksIHRpY2spXG4gIH1cbiAgcmV0dXJuIGdhbGF4eVxufVxuXG4vKiogQGRlcHJlY2F0ZWQgTm8tb3AgY29tcGF0aWJpbGl0eSBzaGltLiBPZmZsaW5lIHdhbGwtY2xvY2sgcmVjb25jaWxpYXRpb24gaXMgZm9yYmlkZGVuIGluIE0yLiAqL1xuZXhwb3J0IGNvbnN0IHJlY29uY2lsZUdhbGF4eSA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBfbGVnYWN5Tm93PzogbnVtYmVyKTogR2FsYXh5U3RhdGUgPT4gY2xvbmVHYWxheHkoc291cmNlKVxuXG5leHBvcnQgY29uc3QgZGlzY292ZXJMaW5rZWRTaXRlcyA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBzb3VyY2VJZDogc3RyaW5nLCBfbGVnYWN5Tm93PzogbnVtYmVyKTogR2FsYXh5U3RhdGUgPT4ge1xuICBjb25zdCBnYWxheHkgPSBjbG9uZUdhbGF4eShzb3VyY2UpXG4gIGNvbnN0IHNpdGUgPSBnYWxheHkuc2l0ZXNbc291cmNlSWRdXG4gIGlmICghc2l0ZSkgcmV0dXJuIGdhbGF4eVxuICBzaXRlLmNvbXBsZXRlZCA9IHRydWVcbiAgc2l0ZS5sYXN0Q2hhbmdlZEF0ID0gZ2FsYXh5LnJvdXRlUmVja29uaW5nXG4gIGZvciAoY29uc3QgaWQgb2Ygc2l0ZS5saW5rcykge1xuICAgIGNvbnN0IGRlc3RpbmF0aW9uID0gZ2FsYXh5LnNpdGVzW2lkXVxuICAgIGlmICghZGVzdGluYXRpb24gfHwgZGVzdGluYXRpb24uZGlzY292ZXJlZCkgY29udGludWVcbiAgICBkZXN0aW5hdGlvbi5kaXNjb3ZlcmVkID0gdHJ1ZVxuICAgIGNvbnN0IHNlY3RvciA9IGdhbGF4eS5zZWN0b3JzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gZGVzdGluYXRpb24uc2VjdG9ySWQpXG4gICAgaWYgKHNlY3Rvcikgc2VjdG9yLmRpc2NvdmVyZWQgPSB0cnVlXG4gICAgYXBwZW5kRXZlbnQoZ2FsYXh5LCB7IGlkOiBgZXZlbnQ6JHtnYWxheHkuc2VlZH06bGluazoke3NvdXJjZUlkfToke2lkfWAsIGF0OiBnYWxheHkucm91dGVSZWNrb25pbmcsIHJvdXRlUmVja29uaW5nOiBnYWxheHkucm91dGVSZWNrb25pbmcsIGtpbmQ6ICdkaXNjb3ZlcnknLCBzaXRlSWQ6IGlkLCBoZWFkbGluZTogYFJvdXRlIHRvICR7ZGVzdGluYXRpb24ubmFtZX0gc3VydmV5ZWRgLCBkZXRhaWw6IGBBIHBoeXNpY2FsIGFwcHJvYWNoIGhhcyBiZWVuIGZvdW5kIGZyb20gJHtzaXRlLm5hbWV9LmAgfSlcbiAgICBpZiAoIWdhbGF4eS5jb250cmFjdHMuc29tZShjb250cmFjdCA9PiBjb250cmFjdC5zb3VyY2VTaXRlSWQgPT09IHNvdXJjZUlkICYmIGNvbnRyYWN0LmRlc3RpbmF0aW9uU2l0ZUlkID09PSBpZCAmJiBjb250cmFjdC5zdGF0dXMgPT09ICdvcGVuJykpIHtcbiAgICAgIGNvbnN0IGtpbmQgPSBjYXJnb0tpbmRzW3JuZ0ZvcihnYWxheHkuc2VlZCwgJ2dhbGF4eScsICdjb250cmFjdCcsIHNvdXJjZUlkLCBpZCkuaW50KDAsIGNhcmdvS2luZHMubGVuZ3RoIC0gMSldIVxuICAgICAgZ2FsYXh5LmNvbnRyYWN0cy5wdXNoKHsgaWQ6IGBjb250cmFjdDoke3NvdXJjZUlkfToke2lkfWAsIHNvdXJjZVNpdGVJZDogc291cmNlSWQsIGRlc3RpbmF0aW9uU2l0ZUlkOiBpZCwgY2FyZ286IHsga2luZCwgdW5pdHM6IDIgfSwgZmVlOiBkZXN0aW5hdGlvbi5tYXJrZXQucHJpY2VzW2tpbmRdICogMywgZGVhZGxpbmVEYXk6IGdhbGF4eS5zZWN0b3JEYXkgKyAxOCwgZGVhZGxpbmVSZWNrb25pbmc6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZyArIDE4ICogUk9VVEVfUkVDS09OSU5HX1VOSVRTX1BFUl9DWUNMRSwgZmFjdGlvbklkOiBkZXN0aW5hdGlvbi5jb250cm9sLCBzdGF0dXM6ICdvcGVuJywgY29sbGF0ZXJhbDogZGVzdGluYXRpb24ubWFya2V0LnByaWNlc1traW5kXSB9KVxuICAgIH1cbiAgfVxuICBhcHBlbmRFdmVudChnYWxheHksIHsgaWQ6IGBldmVudDoke2dhbGF4eS5zZWVkfTpjb21wbGV0ZToke3NvdXJjZUlkfToke2dhbGF4eS5yb3V0ZVJlY2tvbmluZ31gLCBhdDogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCByb3V0ZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCBraW5kOiAnZGlzY292ZXJ5Jywgc2l0ZUlkOiBzb3VyY2VJZCwgaGVhZGxpbmU6IGAke3NpdGUubmFtZX0gc3VydmV5IGFyY2hpdmVkYCwgZGV0YWlsOiAnSm9tb24gaGFzIHByZXNlcnZlZCB0aGlzIGxhbmRpbmcgYXMgYSBwZXJzaXN0ZW50IHNpdGUuJyB9KVxuICByZXR1cm4gZ2FsYXh5XG59XG5cbmV4cG9ydCBjb25zdCBzZXRBY3RpdmVHYWxheHlTaXRlID0gKHNvdXJjZTogR2FsYXh5U3RhdGUsIGlkOiBzdHJpbmcpOiBHYWxheHlTdGF0ZSA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNsb25lR2FsYXh5KHNvdXJjZSlcbiAgaWYgKGdhbGF4eS5zaXRlc1tpZF0/LmRpc2NvdmVyZWQpIGdhbGF4eS5hY3RpdmVTaXRlSWQgPSBpZFxuICByZXR1cm4gZ2FsYXh5XG59XG5leHBvcnQgY29uc3Qgc2V0Q291cmllclJvdXRpbmUgPSAoc291cmNlOiBHYWxheHlTdGF0ZSwgY291cmllcklkOiBzdHJpbmcsIHJvdXRpbmU6IEdhbGF4eUNvdXJpZXJSb3V0aW5lKTogR2FsYXh5U3RhdGUgPT4ge1xuICBjb25zdCBnYWxheHkgPSBjbG9uZUdhbGF4eShzb3VyY2UpXG4gIGNvbnN0IGNvdXJpZXIgPSBnYWxheHkuY291cmllcnMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmlkID09PSBjb3VyaWVySWQpXG4gIGlmIChjb3VyaWVyICYmIGNvdXJpZXIuc3RhdHVzICE9PSAnZGVhZCcgJiYgY291cmllci5zdGF0dXMgIT09ICdyZXRpcmVkJykgY291cmllci5yb3V0aW5lID0gcm91dGluZVxuICByZXR1cm4gZ2FsYXh5XG59XG5leHBvcnQgY29uc3Qgc2VsZWN0R2FsYXh5Q291cmllciA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBjb3VyaWVySWQ6IHN0cmluZyk6IHsgZ2FsYXh5OiBHYWxheHlTdGF0ZTsgaGVybz86IEhlcm8gfSA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNsb25lR2FsYXh5KHNvdXJjZSlcbiAgY29uc3QgY291cmllciA9IGdhbGF4eS5jb3VyaWVycy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUuaWQgPT09IGNvdXJpZXJJZClcbiAgaWYgKCFjb3VyaWVyIHx8IGNvdXJpZXIuc3RhdHVzICE9PSAnYXZhaWxhYmxlJykgcmV0dXJuIHsgZ2FsYXh5IH1cbiAgZ2FsYXh5LmFjdGl2ZUNvdXJpZXJJZCA9IGNvdXJpZXIuaWRcbiAgYXBwZW5kRXZlbnQoZ2FsYXh5LCB7IGlkOiBgZXZlbnQ6JHtnYWxheHkuc2VlZH06aGFuZG9mZjoke2NvdXJpZXIuaWR9OiR7Z2FsYXh5LnJvdXRlUmVja29uaW5nfToke2dhbGF4eS5nZW5lcmFsTWFuaWZlc3QubmV4dFNlcXVlbmNlfWAsIGF0OiBnYWxheHkucm91dGVSZWNrb25pbmcsIHJvdXRlUmVja29uaW5nOiBnYWxheHkucm91dGVSZWNrb25pbmcsIGtpbmQ6ICdkaXNjb3ZlcnknLCBjb3VyaWVySWQ6IGNvdXJpZXIuaWQsIGhlYWRsaW5lOiBgJHtjb3VyaWVyLm5hbWV9IHRha2VzIHRoZSBsYW5kaW5nIHdhdGNoYCwgZGV0YWlsOiBgJHtjb3VyaWVyLnJvbGV9IGhhcyBiZWVuIGFzc2lnbmVkIGFzIEpvbW9uJ3MgYWN0aXZlIHNwZWNpYWxpc3QuYCB9KVxuICByZXR1cm4geyBnYWxheHksIGhlcm86IHN0cnVjdHVyZWRDbG9uZShjb3VyaWVyLmhlcm8pIH1cbn1cbmV4cG9ydCBjb25zdCBsb3NlR2FsYXh5Q291cmllciA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBjb3VyaWVySWQ6IHN0cmluZywgZGV0YWlsOiBzdHJpbmcsIF9sZWdhY3lOb3c/OiBudW1iZXIpOiBHYWxheHlTdGF0ZSA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNsb25lR2FsYXh5KHNvdXJjZSlcbiAgY29uc3QgY291cmllciA9IGdhbGF4eS5jb3VyaWVycy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUuaWQgPT09IGNvdXJpZXJJZClcbiAgaWYgKCFjb3VyaWVyKSByZXR1cm4gZ2FsYXh5XG4gIGNvdXJpZXIuc3RhdHVzID0gJ2RlYWQnXG4gIGFwcGVuZEV2ZW50KGdhbGF4eSwgeyBpZDogYGV2ZW50OiR7Z2FsYXh5LnNlZWR9Omxvc3M6JHtjb3VyaWVyLmlkfToke2dhbGF4eS5yb3V0ZVJlY2tvbmluZ306JHtnYWxheHkuZ2VuZXJhbE1hbmlmZXN0Lm5leHRTZXF1ZW5jZX1gLCBhdDogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCByb3V0ZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCBraW5kOiAnbG9zcycsIGNvdXJpZXJJZDogY291cmllci5pZCwgaGVhZGxpbmU6IGAke2NvdXJpZXIubmFtZX0gaXMgbG9zdGAsIGRldGFpbCB9KVxuICBhcHBlbmRHZW5lcmFsTWFuaWZlc3QoZ2FsYXh5LCB7IGtpbmQ6ICdjb3VyaWVyU3RhdHVzQ2hhbmdlZCcsIGRldGFpbDogYCR7Y291cmllci5uYW1lfSBpcyBsb3N0LmAsIHNvdXJjZTogJ2NvdXJpZXInLCBjb3VyaWVySWQ6IGNvdXJpZXIuaWQsIHBheWxvYWQ6IHsgc3RhdHVzOiAnZGVhZCcgfSB9KVxuICByZXR1cm4gZ2FsYXh5XG59XG5leHBvcnQgY29uc3Qgc2F2ZUdhbGF4eVNpdGUgPSAoc291cmNlOiBHYWxheHlTdGF0ZSwgc2l0ZUlkVmFsdWU6IHN0cmluZywgcnVuOiBSdW5TdGF0ZSwgX2xlZ2FjeU5vdz86IG51bWJlcik6IEdhbGF4eVN0YXRlID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY2xvbmVHYWxheHkoc291cmNlKVxuICBpZiAoIWdhbGF4eS5zaXRlc1tzaXRlSWRWYWx1ZV0pIHJldHVybiBnYWxheHlcbiAgZ2FsYXh5LnNpdGVTbmFwc2hvdHNbc2l0ZUlkVmFsdWVdID0geyB2ZXJzaW9uOiAxLCBydW46IHN0cnVjdHVyZWRDbG9uZShydW4pLCBzYXZlZEF0OiBnYWxheHkucm91dGVSZWNrb25pbmcgfVxuICByZXR1cm4gZ2FsYXh5XG59XG5cbmNvbnN0IGZhY3Rpb25QcmVzc3VyZTogUmVjb3JkPEdhbGF4eUZhY3Rpb25JZCwgeyBoZWFsdGg6IG51bWJlcjsgYXR0YWNrOiBudW1iZXI7IGRlZmVuc2U6IG51bWJlcjsgcmV3YXJkOiBudW1iZXI7IGxhYmVsOiBzdHJpbmcgfT4gPSB7XG4gIHZveWFnZXI6IHsgaGVhbHRoOiAwLCBhdHRhY2s6IDAsIGRlZmVuc2U6IDAsIHJld2FyZDogMS4xLCBsYWJlbDogJ1ZveWFnZXIgc3VydmV5IHRlYW1zIGtlZXAgdGhlIGFwcHJvYWNoIHN1cHBsaWVkLicgfSxcbiAgc2FsdmFnZXJzOiB7IGhlYWx0aDogMCwgYXR0YWNrOiAxLCBkZWZlbnNlOiAwLCByZXdhcmQ6IDEuMywgbGFiZWw6ICdTYWx2YWdlciBwYXRyb2xzIGNvbnRlc3QgdmFsdWFibGUgY2FjaGVzLicgfSxcbiAgcmVsYXlHdWlsZDogeyBoZWFsdGg6IC0xLCBhdHRhY2s6IDAsIGRlZmVuc2U6IDAsIHJld2FyZDogMS4yLCBsYWJlbDogJ1JlbGF5IEd1aWxkIGluZnJhc3RydWN0dXJlIHNob3J0ZW5zIHJvdXRlcyBidXQgcHJpY2VzIHN1cHBsaWVzLicgfSxcbiAgdm9pZGJvcm46IHsgaGVhbHRoOiAyLCBhdHRhY2s6IDEsIGRlZmVuc2U6IDEsIHJld2FyZDogMS40NSwgbGFiZWw6ICdWb2lkYm9ybiBwcmVzc3VyZSB0dXJucyBldmVyeSBhcHByb2FjaCBpbnRvIGEgZmlnaHQuJyB9LFxuICBzZXR0bGVyczogeyBoZWFsdGg6IC0xLCBhdHRhY2s6IDAsIGRlZmVuc2U6IDAsIHJld2FyZDogLjksIGxhYmVsOiAnU2V0dGxlciByb3V0ZXMgYXJlIHNhZmVyLCBidXQgbW9zdCBzYWx2YWdlIGlzIGFscmVhZHkgc3Bva2VuIGZvci4nIH1cbn1cblxuZXhwb3J0IGNvbnN0IGFwcGx5R2FsYXh5U2l0ZUNvbmRpdGlvbnMgPSAocnVuOiBSdW5TdGF0ZSwgc2l0ZTogR2FsYXh5U2l0ZSk6IFJ1blN0YXRlID0+IHtcbiAgY29uc3QgcHJlc3N1cmUgPSBmYWN0aW9uUHJlc3N1cmVbc2l0ZS5jb250cm9sXVxuICBjb25zdCBpbnRlZ3JpdHlQcmVzc3VyZSA9IHNpdGUuaW50ZWdyaXR5IDwgNDAgPyAxIDogMFxuICBjb25zdCBlY29sb2d5UHJlc3N1cmUgPSBzaXRlLmVjb2xvZ3kgPCA0MCA/IDEgOiAwXG4gIGNvbnN0IGhlYWx0aE11bHRpcGxpZXIgPSAxICsgTWF0aC5tYXgoMCwgcHJlc3N1cmUuaGVhbHRoICsgaW50ZWdyaXR5UHJlc3N1cmUpICogLjEyXG4gIGNvbnN0IGF0dGFja0JvbnVzID0gcHJlc3N1cmUuYXR0YWNrICsgZWNvbG9neVByZXNzdXJlXG4gIGNvbnN0IGRlZmVuc2VCb251cyA9IHByZXNzdXJlLmRlZmVuc2UgKyAoc2l0ZS5jb25zdHJ1Y3Rpb24gPiA2NSA/IDEgOiAwKVxuICBmb3IgKGNvbnN0IGFjdG9yIG9mIHJ1bi5mbG9vci5hY3RvcnMpIHtcbiAgICBpZiAoIWFjdG9yLmhvc3RpbGUpIGNvbnRpbnVlXG4gICAgYWN0b3IubWF4SGVhbHRoID0gTWF0aC5tYXgoMSwgTWF0aC5yb3VuZChhY3Rvci5tYXhIZWFsdGggKiBoZWFsdGhNdWx0aXBsaWVyKSlcbiAgICBhY3Rvci5oZWFsdGggPSBNYXRoLm1pbihhY3Rvci5tYXhIZWFsdGgsIE1hdGgucm91bmQoYWN0b3IuaGVhbHRoICogaGVhbHRoTXVsdGlwbGllcikpXG4gICAgYWN0b3IuYXR0YWNrICs9IGF0dGFja0JvbnVzXG4gICAgYWN0b3IuZGVmZW5zZSArPSBkZWZlbnNlQm9udXNcbiAgfVxuICBydW4uZmxvb3IuZGlmZmljdWx0eSA9IHsgLi4uKHJ1bi5mbG9vci5kaWZmaWN1bHR5ID8/IHsgcm91dGVQb3NpdGlvbjogMCwgdGhyZWF0OiAwLCBoZWFsdGhNdWx0aXBsaWVyOiAxLCBhdHRhY2tCb251czogMCwgZGVmZW5zZUJvbnVzOiAwLCBlbGl0ZUNoYW5jZTogMCwgZ3VhcmRpYW5QYXR0ZXJuOiAwIH0pLCBoZWFsdGhNdWx0aXBsaWVyLCBhdHRhY2tCb251cywgZGVmZW5zZUJvbnVzLCByZXdhcmRNdWx0aXBsaWVyOiBwcmVzc3VyZS5yZXdhcmQgfVxuICBjb25zdCB0ZXJyYWluID0gcnVuLmZsb29yLnRpbGVzLmZpbHRlcigodGlsZSwgaW5kZXgpID0+IHRpbGUua2luZCA9PT0gJ2Zsb29yJyAmJiBpbmRleCAhPT0gcnVuLmZsb29yLnN0YXJ0LnkgKiBydW4uZmxvb3Iud2lkdGggKyBydW4uZmxvb3Iuc3RhcnQueClcbiAgaWYgKHNpdGUuZWNvbG9neSA8IDM1KSB0ZXJyYWluLmZpbHRlcigoXywgaW5kZXgpID0+IGluZGV4ICUgMzcgPT09IDApLmZvckVhY2godGlsZSA9PiB7IHRpbGUua2luZCA9ICdnYXMnIH0pXG4gIGlmIChzaXRlLmludGVncml0eSA8IDM1KSB0ZXJyYWluLmZpbHRlcigoXywgaW5kZXgpID0+IGluZGV4ICUgNDEgPT09IDApLmZvckVhY2godGlsZSA9PiB7IHRpbGUua2luZCA9ICdydWJibGUnIH0pXG4gIHJ1bi5tZXNzYWdlcy51bnNoaWZ0KGAke3NpdGUubmFtZX06ICR7cHJlc3N1cmUubGFiZWx9YClcbiAgaWYgKHNpdGUuZWNvbG9neSA8IDM1KSBydW4ubWVzc2FnZXMudW5zaGlmdCgnRWNvbG9neSBhbGVydDogaW52YXNpdmUgc3BvcmVzIGhhdmUgYWx0ZXJlZCB0aGUgbGFuZGluZyByb3V0ZS4nKVxuICBpZiAoc2l0ZS5pbnRlZ3JpdHkgPCAzNSkgcnVuLm1lc3NhZ2VzLnVuc2hpZnQoJ0ludGVncml0eSBhbGVydDogY29sbGFwc2UgZGVicmlzIGhhcyBuYXJyb3dlZCB0aGUgbGFuZGluZyByb3V0ZS4nKVxuICByZXR1cm4gcnVuXG59XG5cbmV4cG9ydCBjb25zdCByZWNvcmRHYWxheHlMYW5kaW5nID0gKHNvdXJjZTogR2FsYXh5U3RhdGUsIHNpdGVJZFZhbHVlOiBzdHJpbmcsIHJ1bjogUnVuU3RhdGUsIF9sZWdhY3lOb3c/OiBudW1iZXIpOiBHYWxheHlTdGF0ZSA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNsb25lR2FsYXh5KHNvdXJjZSlcbiAgY29uc3Qgc2l0ZSA9IGdhbGF4eS5zaXRlc1tzaXRlSWRWYWx1ZV1cbiAgaWYgKCFzaXRlKSByZXR1cm4gZ2FsYXh5XG4gIGNvbnN0IHByZXNzdXJlID0gZmFjdGlvblByZXNzdXJlW3NpdGUuY29udHJvbF1cbiAgY29uc3Qga2lsbHMgPSBydW4udGVsZW1ldHJ5Py5raWxscyA/PyAwXG4gIGNvbnN0IHJld2FyZCA9IE1hdGgubWF4KDQsIE1hdGgucm91bmQoKDYgKyBraWxscyArIE1hdGguZmxvb3Ioc2l0ZS5zYWx2YWdlIC8gMTgpKSAqIHByZXNzdXJlLnJld2FyZCkpXG4gIHJ1bi5oZXJvLmdvbGQgKz0gcmV3YXJkXG4gIHNpdGUuc2FsdmFnZSA9IGNsYW1wKHNpdGUuc2FsdmFnZSAtIE1hdGgubWF4KDIsIHJld2FyZCAvIDIpKVxuICBzaXRlLnN1cHBsaWVzID0gY2xhbXAoc2l0ZS5zdXBwbGllcyArIDQgKyBNYXRoLm1pbigxMCwga2lsbHMpKVxuICBzaXRlLmludGVncml0eSA9IGNsYW1wKHNpdGUuaW50ZWdyaXR5ICsgMylcbiAgc2l0ZS5lY29sb2d5ID0gY2xhbXAoc2l0ZS5lY29sb2d5ICsgKHNpdGUuZWNvbG9neSA8IDQ1ID8gMiA6IDApKVxuICBzaXRlLmNvbnN0cnVjdGlvbiA9IGNsYW1wKHNpdGUuY29uc3RydWN0aW9uICsgMilcbiAgc2l0ZS5sYXN0Q2hhbmdlZEF0ID0gZ2FsYXh5LnJvdXRlUmVja29uaW5nXG4gIGFwcGVuZEV2ZW50KGdhbGF4eSwgeyBpZDogYGV2ZW50OiR7Z2FsYXh5LnNlZWR9OmxhbmRpbmc6JHtzaXRlSWRWYWx1ZX06JHtnYWxheHkucm91dGVSZWNrb25pbmd9OiR7Z2FsYXh5LmdlbmVyYWxNYW5pZmVzdC5uZXh0U2VxdWVuY2V9YCwgYXQ6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZywgcm91dGVSZWNrb25pbmc6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZywga2luZDogJ2NvbnN0cnVjdGlvbicsIHNpdGVJZDogc2l0ZUlkVmFsdWUsIGhlYWRsaW5lOiBgJHtzaXRlLm5hbWV9OiBsYW5kaW5nIHJldHVybmVkICR7cmV3YXJkfSBjcmVkaXRzYCwgZGV0YWlsOiBgU3VydmV5IHdvcmsgaW5jcmVhc2VkIHN1cHBsaWVzIHRvICR7c2l0ZS5zdXBwbGllc30gYW5kIHN0YWJpbGl6ZWQgdGhlIGFwcHJvYWNoLmAgfSlcbiAgcmV0dXJuIGdhbGF4eVxufVxuZXhwb3J0IGNvbnN0IGdhbGF4eVJvdXRlTGVuZ3RoID0gKGdhbGF4eTogR2FsYXh5U3RhdGUsIGZyb21TaXRlSWQ6IHN0cmluZywgdG9TaXRlSWQ6IHN0cmluZyk6IG51bWJlciA9PiAxICsgcm5nRm9yKGdhbGF4eS5zZWVkLCAnZ2FsYXh5JywgJ2xpbmsnLCBbZnJvbVNpdGVJZCwgdG9TaXRlSWRdLnNvcnQoKS5qb2luKCc6OicpKS5pbnQoMCwgNClcbmV4cG9ydCBjb25zdCBnYWxheHlSb3V0ZVNpdHVhdGlvbiA9IChnYWxheHk6IEdhbGF4eVN0YXRlLCBmcm9tU2l0ZUlkOiBzdHJpbmcsIHRvU2l0ZUlkOiBzdHJpbmcsIGNodW5rOiBudW1iZXIpOiAncXVpZXQnIHwgJ3BhdHJvbCcgfCAnaGF6YXJkJyB8ICd0cmFkZXInIHwgJ2Vjb2xvZ3knID0+IHtcbiAgY29uc3Qgb3V0Y29tZXMgPSBbJ3F1aWV0JywgJ3BhdHJvbCcsICdoYXphcmQnLCAndHJhZGVyJywgJ2Vjb2xvZ3knXSBhcyBjb25zdFxuICByZXR1cm4gb3V0Y29tZXNbcm5nRm9yKGdhbGF4eS5zZWVkLCAnZ2FsYXh5JywgJ3JvdXRlLXNpdHVhdGlvbicsIFtmcm9tU2l0ZUlkLCB0b1NpdGVJZF0uc29ydCgpLmpvaW4oJzo6JyksIGNodW5rLCByb3V0ZVdvcmxkVGlja0ZvcihnYWxheHkucm91dGVSZWNrb25pbmcpKS5pbnQoMCwgb3V0Y29tZXMubGVuZ3RoIC0gMSldIVxufVxuZXhwb3J0IGNvbnN0IHJlY29yZEdhbGF4eVJvdXRlU2l0dWF0aW9ucyA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBsaW5rSWQ6IHN0cmluZywgc2l0dWF0aW9uczogcmVhZG9ubHkgKCdxdWlldCcgfCAncGF0cm9sJyB8ICdoYXphcmQnIHwgJ3RyYWRlcicgfCAnZWNvbG9neScpW10pOiBHYWxheHlTdGF0ZSA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNsb25lR2FsYXh5KHNvdXJjZSlcbiAgc2l0dWF0aW9ucy5mb3JFYWNoKChzaXR1YXRpb24sIGNodW5rKSA9PiB7XG4gICAgaWYgKHNpdHVhdGlvbiA9PT0gJ3F1aWV0JykgcmV0dXJuXG4gICAgYXBwZW5kR2VuZXJhbE1hbmlmZXN0KGdhbGF4eSwgeyBraW5kOiAncm91dGVTaXR1YXRpb25BY3RpdmF0ZWQnLCBkZXRhaWw6IGAke3NpdHVhdGlvbn0gY29uZGl0aW9ucyB3ZXJlIHJlcG9ydGVkIG9uIHJvdXRlICR7bGlua0lkfSwgY29ubmVjdG9yIHNlZ21lbnQgJHtjaHVuayArIDF9LmAsIHNvdXJjZTogJ3JvdXRlJywgcGF5bG9hZDogeyBsaW5rSWQsIGNodW5rLCBzaXR1YXRpb24sIHN0YXR1czogJ3JlcG9ydGVkJyB9IH0pXG4gIH0pXG4gIHJldHVybiBnYWxheHlcbn1cbmV4cG9ydCBjb25zdCByZXNvbHZlR2FsYXh5Um91dGVTaXR1YXRpb25zID0gKHNvdXJjZTogR2FsYXh5U3RhdGUsIGxpbmtJZDogc3RyaW5nLCBzaXR1YXRpb25zOiByZWFkb25seSAoJ3F1aWV0JyB8ICdwYXRyb2wnIHwgJ2hhemFyZCcgfCAndHJhZGVyJyB8ICdlY29sb2d5JylbXSk6IEdhbGF4eVN0YXRlID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY2xvbmVHYWxheHkoc291cmNlKVxuICBzaXR1YXRpb25zLmZvckVhY2goKHNpdHVhdGlvbiwgY2h1bmspID0+IHtcbiAgICBpZiAoc2l0dWF0aW9uID09PSAncXVpZXQnKSByZXR1cm5cbiAgICBhcHBlbmRHZW5lcmFsTWFuaWZlc3QoZ2FsYXh5LCB7IGtpbmQ6ICdyb3V0ZVNpdHVhdGlvblJlc29sdmVkJywgZGV0YWlsOiBgJHtzaXR1YXRpb259IGNvbmRpdGlvbnMgY29uY2x1ZGVkIG9uIHJvdXRlICR7bGlua0lkfSwgY29ubmVjdG9yIHNlZ21lbnQgJHtjaHVuayArIDF9LmAsIHNvdXJjZTogJ3JvdXRlJywgcGF5bG9hZDogeyBsaW5rSWQsIGNodW5rLCBzaXR1YXRpb24sIHN0YXR1czogJ2NvbmZpcm1lZCcgfSB9KVxuICB9KVxuICByZXR1cm4gZ2FsYXh5XG59XG5leHBvcnQgY29uc3QgYWNjZXB0R2FsYXh5Q29udHJhY3QgPSAoc291cmNlOiBHYWxheHlTdGF0ZSwgZnJvbVNpdGVJZDogc3RyaW5nLCB0b1NpdGVJZDogc3RyaW5nLCBfbGVnYWN5Tm93PzogbnVtYmVyKTogeyBnYWxheHk6IEdhbGF4eVN0YXRlOyBtZXNzYWdlOiBzdHJpbmcgfSA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNsb25lR2FsYXh5KHNvdXJjZSlcbiAgY29uc3QgY29udHJhY3QgPSBnYWxheHkuY29udHJhY3RzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5zb3VyY2VTaXRlSWQgPT09IGZyb21TaXRlSWQgJiYgY2FuZGlkYXRlLmRlc3RpbmF0aW9uU2l0ZUlkID09PSB0b1NpdGVJZCAmJiBjYW5kaWRhdGUuc3RhdHVzID09PSAnb3BlbicpXG4gIGlmICghY29udHJhY3QpIHJldHVybiB7IGdhbGF4eSwgbWVzc2FnZTogJ05vIG9wZW4gY29udHJhY3QgaXMgcmVnaXN0ZXJlZCBmb3IgdGhpcyBhaXJsb2NrLicgfVxuICBpZiAoZ2FsYXh5LmNhcmdvLnJlZHVjZSgodG90YWwsIGNhcmdvKSA9PiB0b3RhbCArIGNhcmdvLnVuaXRzLCAwKSArIGNvbnRyYWN0LmNhcmdvLnVuaXRzID4gMTIpIHJldHVybiB7IGdhbGF4eSwgbWVzc2FnZTogJ0pvbW9uIGNhcmdvIGhvbGQgbGFja3MgY2FwYWNpdHkgZm9yIHRoaXMgY29udHJhY3QuJyB9XG4gIGNvbnN0IHNvdXJjZVNpdGUgPSBnYWxheHkuc2l0ZXNbZnJvbVNpdGVJZF1cbiAgaWYgKCFzb3VyY2VTaXRlIHx8IHNvdXJjZVNpdGUubWFya2V0LnN0b2NrW2NvbnRyYWN0LmNhcmdvLmtpbmRdIDwgY29udHJhY3QuY2FyZ28udW5pdHMpIHJldHVybiB7IGdhbGF4eSwgbWVzc2FnZTogJ1RoZSBzb3VyY2UgbWFya2V0IGNhbm5vdCBsb2FkIHRoYXQgY2FyZ28gdG9kYXkuJyB9XG4gIHNvdXJjZVNpdGUubWFya2V0LnN0b2NrW2NvbnRyYWN0LmNhcmdvLmtpbmRdIC09IGNvbnRyYWN0LmNhcmdvLnVuaXRzXG4gIGdhbGF4eS5jYXJnby5wdXNoKHsgLi4uY29udHJhY3QuY2FyZ28sIGNvbnRyYWN0SWQ6IGNvbnRyYWN0LmlkIH0pXG4gIGNvbnRyYWN0LnN0YXR1cyA9ICdhY3RpdmUnXG4gIGFwcGVuZEV2ZW50KGdhbGF4eSwgeyBpZDogYGV2ZW50OiR7Z2FsYXh5LnNlZWR9OmNvbnRyYWN0OiR7Y29udHJhY3QuaWR9OmFjY2VwdGVkYCwgYXQ6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZywgcm91dGVSZWNrb25pbmc6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZywga2luZDogJ3RyYWRlJywgc2l0ZUlkOiBmcm9tU2l0ZUlkLCBoZWFkbGluZTogJ0NvbnRyYWN0IGNhcmdvIGxvYWRlZCcsIGRldGFpbDogYCR7Y29udHJhY3QuY2FyZ28udW5pdHN9IHVuaXRzIG9mICR7Y29udHJhY3QuY2FyZ28ua2luZH0gYXJlIGJvdW5kIGZvciAke2dhbGF4eS5zaXRlc1t0b1NpdGVJZF0/Lm5hbWUgPz8gdG9TaXRlSWR9LmAgfSlcbiAgcmV0dXJuIHsgZ2FsYXh5LCBtZXNzYWdlOiBgTG9hZGVkICR7Y29udHJhY3QuY2FyZ28udW5pdHN9IHVuaXRzIG9mICR7Y29udHJhY3QuY2FyZ28ua2luZH0uYCB9XG59XG5leHBvcnQgY29uc3QgZGVsaXZlckdhbGF4eUNvbnRyYWN0cyA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBzaXRlSWQ6IHN0cmluZywgaGVybzogSGVybywgX2xlZ2FjeU5vdz86IG51bWJlcik6IHsgZ2FsYXh5OiBHYWxheHlTdGF0ZTsgbWVzc2FnZT86IHN0cmluZyB9ID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY2xvbmVHYWxheHkoc291cmNlKVxuICBjb25zdCBkZWxpdmVyYWJsZSA9IGdhbGF4eS5jb250cmFjdHMuZmlsdGVyKGNvbnRyYWN0ID0+IGNvbnRyYWN0LmRlc3RpbmF0aW9uU2l0ZUlkID09PSBzaXRlSWQgJiYgY29udHJhY3Quc3RhdHVzID09PSAnYWN0aXZlJyAmJiBjb250cmFjdC5kZWFkbGluZVJlY2tvbmluZyA+PSBnYWxheHkucm91dGVSZWNrb25pbmcpXG4gIGlmICghZGVsaXZlcmFibGUubGVuZ3RoKSByZXR1cm4geyBnYWxheHkgfVxuICBsZXQgZmVlID0gMFxuICBmb3IgKGNvbnN0IGNvbnRyYWN0IG9mIGRlbGl2ZXJhYmxlKSB7XG4gICAgY29udHJhY3Quc3RhdHVzID0gJ2NvbXBsZXRlZCdcbiAgICBmZWUgKz0gY29udHJhY3QuZmVlXG4gICAgZ2FsYXh5LmNhcmdvID0gZ2FsYXh5LmNhcmdvLmZpbHRlcihjYXJnbyA9PiBjYXJnby5jb250cmFjdElkICE9PSBjb250cmFjdC5pZClcbiAgICBnYWxheHkuc2l0ZXNbc2l0ZUlkXSEubWFya2V0LnN0b2NrW2NvbnRyYWN0LmNhcmdvLmtpbmRdID0gY2xhbXAoZ2FsYXh5LnNpdGVzW3NpdGVJZF0hLm1hcmtldC5zdG9ja1tjb250cmFjdC5jYXJnby5raW5kXSArIGNvbnRyYWN0LmNhcmdvLnVuaXRzKVxuICB9XG4gIGhlcm8uZ29sZCArPSBmZWVcbiAgYXBwZW5kRXZlbnQoZ2FsYXh5LCB7IGlkOiBgZXZlbnQ6JHtnYWxheHkuc2VlZH06Y29udHJhY3Q6JHtzaXRlSWR9OiR7Z2FsYXh5LnJvdXRlUmVja29uaW5nfToke2dhbGF4eS5nZW5lcmFsTWFuaWZlc3QubmV4dFNlcXVlbmNlfTpkZWxpdmVyZWRgLCBhdDogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCByb3V0ZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nLCBraW5kOiAndHJhZGUnLCBzaXRlSWQsIGhlYWRsaW5lOiAnQ29udHJhY3QgZGVsaXZlcmVkJywgZGV0YWlsOiBgJHtkZWxpdmVyYWJsZS5sZW5ndGh9IGRlbGl2ZXJ5JHtkZWxpdmVyYWJsZS5sZW5ndGggPT09IDEgPyAnJyA6ICdpZXMnfSBwYWlkICR7ZmVlfSBjcmVkaXRzLmAgfSlcbiAgcmV0dXJuIHsgZ2FsYXh5LCBtZXNzYWdlOiBgRGVsaXZlcnkgY29tcGxldGU6ICR7ZmVlfSBjcmVkaXRzLmAgfVxufVxuZXhwb3J0IGNvbnN0IGFiYW5kb25HYWxheHlDYXJnbyA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBsaW5rSWQ6IHN0cmluZywgY2h1bms6IG51bWJlciwgX2xlZ2FjeU5vdz86IG51bWJlcik6IEdhbGF4eVN0YXRlID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY2xvbmVHYWxheHkoc291cmNlKVxuICBjb25zdCBjYXJnbyA9IGdhbGF4eS5jYXJnby5maWx0ZXIoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5jb250cmFjdElkKVxuICBpZiAoIWNhcmdvLmxlbmd0aCkgcmV0dXJuIGdhbGF4eVxuICBnYWxheHkuY2FyZ28gPSBnYWxheHkuY2FyZ28uZmlsdGVyKGNhbmRpZGF0ZSA9PiAhY2FuZGlkYXRlLmNvbnRyYWN0SWQpXG4gIGZvciAoY29uc3QgY29udHJhY3Qgb2YgZ2FsYXh5LmNvbnRyYWN0cykgaWYgKGNhcmdvLnNvbWUoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5jb250cmFjdElkID09PSBjb250cmFjdC5pZCkpIGNvbnRyYWN0LnN0YXR1cyA9ICdmYWlsZWQnXG4gIGdhbGF4eS5yb3V0ZUNhY2hlcy5wdXNoKHsgaWQ6IGBjYWNoZToke2xpbmtJZH06JHtjaHVua306JHtnYWxheHkucm91dGVSZWNrb25pbmd9OiR7Z2FsYXh5LmdlbmVyYWxNYW5pZmVzdC5uZXh0U2VxdWVuY2V9YCwgbGlua0lkLCBjaHVuaywgY2FyZ28sIHBhY2thZ2VzOiBbXSwgcmVjb3ZlcmVkOiBmYWxzZSB9KVxuICBhcHBlbmRFdmVudChnYWxheHksIHsgaWQ6IGBldmVudDoke2dhbGF4eS5zZWVkfTpjYWNoZToke2xpbmtJZH06JHtnYWxheHkucm91dGVSZWNrb25pbmd9OiR7Z2FsYXh5LmdlbmVyYWxNYW5pZmVzdC5uZXh0U2VxdWVuY2V9YCwgYXQ6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZywgcm91dGVSZWNrb25pbmc6IGdhbGF4eS5yb3V0ZVJlY2tvbmluZywga2luZDogJ2xvc3MnLCBoZWFkbGluZTogJ0NvbnRyYWN0IGNhcmdvIGFiYW5kb25lZCcsIGRldGFpbDogJ0EgcmVjb3ZlcmFibGUgcm91dGUgY2FjaGUgbWFya3MgdGhlIGxhc3Qga25vd24gcG9zaXRpb24gb2YgdGhlIGNhcmdvLicgfSlcbiAgcmV0dXJuIGdhbGF4eVxufVxuZXhwb3J0IGNvbnN0IHJlY292ZXJHYWxheHlSb3V0ZUNhY2hlcyA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBsaW5rSWQ6IHN0cmluZyk6IHsgZ2FsYXh5OiBHYWxheHlTdGF0ZTsgbWVzc2FnZTogc3RyaW5nIH0gPT4ge1xuICBjb25zdCBnYWxheHkgPSBjbG9uZUdhbGF4eShzb3VyY2UpXG4gIGNvbnN0IGNhY2hlcyA9IGdhbGF4eS5yb3V0ZUNhY2hlcy5maWx0ZXIoY2FjaGUgPT4gY2FjaGUubGlua0lkID09PSBsaW5rSWQgJiYgIWNhY2hlLnJlY292ZXJlZCAmJiBjYWNoZS5jYXJnby5sZW5ndGgpXG4gIGNvbnN0IGNhcmdvID0gY2FjaGVzLmZsYXRNYXAoY2FjaGUgPT4gY2FjaGUuY2FyZ28pXG4gIGNvbnN0IGNhcGFjaXR5ID0gMTIgLSBnYWxheHkuY2FyZ28ucmVkdWNlKCh0b3RhbCwgZW50cnkpID0+IHRvdGFsICsgZW50cnkudW5pdHMsIDApXG4gIGNvbnN0IHJlY292ZXJlZCA9IGNhcmdvLnJlZHVjZSgodG90YWwsIGVudHJ5KSA9PiB0b3RhbCArIGVudHJ5LnVuaXRzLCAwKVxuICBpZiAoIWNhY2hlcy5sZW5ndGgpIHJldHVybiB7IGdhbGF4eSwgbWVzc2FnZTogJ05vIHJlY292ZXJhYmxlIGNhcmdvIGNhY2hlIGlzIHJlY29yZGVkIG9uIHRoaXMgcm91dGUuJyB9XG4gIGlmIChyZWNvdmVyZWQgPiBjYXBhY2l0eSkgcmV0dXJuIHsgZ2FsYXh5LCBtZXNzYWdlOiAnVm95YWdlciBjYXJnbyBob2xkIGxhY2tzIHJvb20gZm9yIHRoZSByZWNvdmVyZWQgY2FjaGUuJyB9XG4gIGdhbGF4eS5jYXJnby5wdXNoKC4uLmNhcmdvLm1hcChlbnRyeSA9PiAoeyAuLi5lbnRyeSwgY29udHJhY3RJZDogdW5kZWZpbmVkIH0pKSlcbiAgY2FjaGVzLmZvckVhY2goY2FjaGUgPT4geyBjYWNoZS5yZWNvdmVyZWQgPSB0cnVlIH0pXG4gIHJldHVybiB7IGdhbGF4eSwgbWVzc2FnZTogYFJlY292ZXJlZCAke3JlY292ZXJlZH0gY2FyZ28gdW5pdHMuIFRoZSBmYWlsZWQgY29udHJhY3RzIHJlbWFpbiBjbG9zZWQuYCB9XG59XG5leHBvcnQgY29uc3QgZ2FsYXh5U25hcHNob3QgPSAoZ2FsYXh5OiBHYWxheHlTdGF0ZSwgc2l0ZUlkVmFsdWU6IHN0cmluZyk6IFJ1blN0YXRlIHwgdW5kZWZpbmVkID0+IGdhbGF4eS5zaXRlU25hcHNob3RzW3NpdGVJZFZhbHVlXSA/IHN0cnVjdHVyZWRDbG9uZShnYWxheHkuc2l0ZVNuYXBzaG90c1tzaXRlSWRWYWx1ZV0ucnVuKSA6IHVuZGVmaW5lZFxuZXhwb3J0IGNvbnN0IGF2YWlsYWJsZUdhbGF4eVNpdGVzID0gKGdhbGF4eTogR2FsYXh5U3RhdGUpOiBHYWxheHlTaXRlW10gPT4gT2JqZWN0LnZhbHVlcyhnYWxheHkuc2l0ZXMpLmZpbHRlcihzaXRlID0+IHNpdGUuZGlzY292ZXJlZCkuc29ydCgobGVmdCwgcmlnaHQpID0+IGxlZnQuaWQubG9jYWxlQ29tcGFyZShyaWdodC5pZCkpXG5leHBvcnQgY29uc3QgZ2FsYXh5Q2hyb25pY2xlID0gKGdhbGF4eTogR2FsYXh5U3RhdGUpOiByZWFkb25seSBHYWxheHlFdmVudFtdID0+IGdhbGF4eS5ldmVudHNcblxuY29uc3QgaXNSZWNvcmQgPSAodmFsdWU6IHVua25vd24pOiB2YWx1ZSBpcyBSZWNvcmQ8c3RyaW5nLCB1bmtub3duPiA9PiB0eXBlb2YgdmFsdWUgPT09ICdvYmplY3QnICYmIHZhbHVlICE9PSBudWxsICYmICFBcnJheS5pc0FycmF5KHZhbHVlKVxuZXhwb3J0IGNvbnN0IG1pZ3JhdGVHYWxheHkgPSAodmFsdWU6IHVua25vd24pOiBHYWxheHlTdGF0ZSB8IHVuZGVmaW5lZCA9PiB7XG4gIGlmICghaXNSZWNvcmQodmFsdWUpIHx8ICh2YWx1ZS52ZXJzaW9uICE9PSAxICYmIHZhbHVlLnZlcnNpb24gIT09IDIpIHx8IHR5cGVvZiB2YWx1ZS5zZWVkICE9PSAnbnVtYmVyJyB8fCAhQXJyYXkuaXNBcnJheSh2YWx1ZS5zZWN0b3JzKSB8fCAhaXNSZWNvcmQodmFsdWUuc2l0ZXMpIHx8ICFBcnJheS5pc0FycmF5KHZhbHVlLmNvdXJpZXJzKSB8fCAhQXJyYXkuaXNBcnJheSh2YWx1ZS5mYWN0aW9ucykgfHwgIUFycmF5LmlzQXJyYXkodmFsdWUuZXZlbnRzKSB8fCAhaXNSZWNvcmQodmFsdWUuc2l0ZVNuYXBzaG90cykpIHJldHVybiB1bmRlZmluZWRcbiAgdHJ5IHtcbiAgICBjb25zdCBsZWdhY3kgPSBzdHJ1Y3R1cmVkQ2xvbmUodmFsdWUpIGFzIHVua25vd24gYXMgR2FsYXh5U3RhdGVcbiAgICBjb25zdCBsZWdhY3lEYXkgPSB0eXBlb2YgbGVnYWN5LnNlY3RvckRheSA9PT0gJ251bWJlcicgJiYgTnVtYmVyLmlzRmluaXRlKGxlZ2FjeS5zZWN0b3JEYXkpID8gbGVnYWN5LnNlY3RvckRheSA6IDBcbiAgICBjb25zdCByb3V0ZVJlY2tvbmluZyA9IE51bWJlci5pc0ludGVnZXIobGVnYWN5LnJvdXRlUmVja29uaW5nKSAmJiBsZWdhY3kucm91dGVSZWNrb25pbmcgPj0gMCA/IGxlZ2FjeS5yb3V0ZVJlY2tvbmluZyA6IHJvdXRlUmVja29uaW5nRnJvbUxlZ2FjeURheShsZWdhY3lEYXkpXG4gICAgY29uc3QgZ2FsYXh5OiBHYWxheHlTdGF0ZSA9IHtcbiAgICAgIC4uLmxlZ2FjeSxcbiAgICAgIHZlcnNpb246IDIsXG4gICAgICByb3V0ZVJlY2tvbmluZyxcbiAgICAgIGxhc3RXb3JsZFRpY2s6IE51bWJlci5pc0ludGVnZXIobGVnYWN5Lmxhc3RXb3JsZFRpY2spICYmIGxlZ2FjeS5sYXN0V29ybGRUaWNrID49IDAgPyBNYXRoLm1pbihsZWdhY3kubGFzdFdvcmxkVGljaywgcm91dGVXb3JsZFRpY2tGb3Iocm91dGVSZWNrb25pbmcpKSA6IHJvdXRlV29ybGRUaWNrRm9yKHJvdXRlUmVja29uaW5nKSxcbiAgICAgIHNlY3RvckRheTogc2VjdG9yRGF5RnJvbVJvdXRlUmVja29uaW5nKHJvdXRlUmVja29uaW5nKSxcbiAgICAgIHNoYXJlZFN0YXNoOiBBcnJheS5pc0FycmF5KCh2YWx1ZSBhcyBSZWNvcmQ8c3RyaW5nLCB1bmtub3duPikuc2hhcmVkU3Rhc2gpID8gWy4uLmxlZ2FjeS5zaGFyZWRTdGFzaF0gOiBbXVxuICAgIH1cbiAgICBnYWxheHkuY2FyZ28gPz89IFtdXG4gICAgZ2FsYXh5LmNvbnRyYWN0cyA/Pz0gW11cbiAgICBnYWxheHkuc2VhbGVkUGFja2FnZUNvbnRyYWN0cyA/Pz0gW11cbiAgICBnYWxheHkuc2VhbGVkUGFja2FnZXMgPz89IFtdXG4gICAgZ2FsYXh5LmdlbmVyYWxNYW5pZmVzdCA/Pz0geyB2ZXJzaW9uOiAxLCBuZXh0U2VxdWVuY2U6IDAsIGVudHJpZXM6IFtdIH1cbiAgICBnYWxheHkucm91dGVDYWNoZXMgPz89IFtdXG4gICAgZm9yIChjb25zdCBjYWNoZSBvZiBnYWxheHkucm91dGVDYWNoZXMpIGNhY2hlLnBhY2thZ2VzID8/PSBbXVxuICAgIGZvciAoY29uc3QgY291cmllciBvZiBnYWxheHkuY291cmllcnMpIGlmIChjb3VyaWVyLnJpdmFsSWQgPT09IHVuZGVmaW5lZCkgZGVsZXRlIGNvdXJpZXIucml2YWxJZFxuICAgIGZvciAoY29uc3Qgc2VjdG9yIG9mIGdhbGF4eS5zZWN0b3JzKSB7XG4gICAgICBpZiAoT2JqZWN0LmlzKHNlY3Rvci54LCAtMCkpIHNlY3Rvci54ID0gMFxuICAgICAgaWYgKE9iamVjdC5pcyhzZWN0b3IueSwgLTApKSBzZWN0b3IueSA9IDBcbiAgICB9XG4gICAgZm9yIChjb25zdCBjb250cmFjdCBvZiBnYWxheHkuY29udHJhY3RzKSB7XG4gICAgICBjb250cmFjdC5kZWFkbGluZVJlY2tvbmluZyA/Pz0gcm91dGVSZWNrb25pbmdGcm9tTGVnYWN5RGF5KGNvbnRyYWN0LmRlYWRsaW5lRGF5KVxuICAgICAgY29udHJhY3QuZGVhZGxpbmVEYXkgPSBzZWN0b3JEYXlGcm9tUm91dGVSZWNrb25pbmcoY29udHJhY3QuZGVhZGxpbmVSZWNrb25pbmcpXG4gICAgfVxuICAgIGZvciAoY29uc3QgY29udHJhY3Qgb2YgZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHMpIHtcbiAgICAgIGNvbnRyYWN0LnZlcnNpb24gPSAyXG4gICAgICBjb250cmFjdC5vZmZlcmVkQXRSb3V0ZVJlY2tvbmluZyA/Pz0gcm91dGVSZWNrb25pbmdGcm9tTGVnYWN5RGF5KGNvbnRyYWN0Lm9mZmVyZWRBdFNlY3RvckRheSlcbiAgICAgIGNvbnRyYWN0LmFjY2VwdGVkQXRSb3V0ZVJlY2tvbmluZyA/Pz0gY29udHJhY3QuYWNjZXB0ZWRBdFNlY3RvckRheSA9PT0gdW5kZWZpbmVkID8gdW5kZWZpbmVkIDogcm91dGVSZWNrb25pbmdGcm9tTGVnYWN5RGF5KGNvbnRyYWN0LmFjY2VwdGVkQXRTZWN0b3JEYXkpXG4gICAgICBjb250cmFjdC5kZXN0aW5hdGlvblJlYWNoZWRBdFJvdXRlUmVja29uaW5nID8/PSBjb250cmFjdC5kZXN0aW5hdGlvblJlYWNoZWRBdFNlY3RvckRheSA9PT0gdW5kZWZpbmVkID8gdW5kZWZpbmVkIDogcm91dGVSZWNrb25pbmdGcm9tTGVnYWN5RGF5KGNvbnRyYWN0LmRlc3RpbmF0aW9uUmVhY2hlZEF0U2VjdG9yRGF5KVxuICAgICAgY29udHJhY3QucmVzb2x2ZWRBdFJvdXRlUmVja29uaW5nID8/PSBjb250cmFjdC5yZXNvbHZlZEF0U2VjdG9yRGF5ID09PSB1bmRlZmluZWQgPyB1bmRlZmluZWQgOiByb3V0ZVJlY2tvbmluZ0Zyb21MZWdhY3lEYXkoY29udHJhY3QucmVzb2x2ZWRBdFNlY3RvckRheSlcbiAgICAgIGNvbnRyYWN0LnRlcm1zLmRlYWRsaW5lUmVja29uaW5nID8/PSByb3V0ZVJlY2tvbmluZ0Zyb21MZWdhY3lEYXkoY29udHJhY3QudGVybXMuZGVhZGxpbmVEYXkpXG4gICAgICBjb250cmFjdC50ZXJtcy5kZWFkbGluZURheSA9IHNlY3RvckRheUZyb21Sb3V0ZVJlY2tvbmluZyhjb250cmFjdC50ZXJtcy5kZWFkbGluZVJlY2tvbmluZylcbiAgICB9XG4gICAgZm9yIChjb25zdCBwYWNrYWdlUmVjb3JkIG9mIGdhbGF4eS5zZWFsZWRQYWNrYWdlcykgcGFja2FnZVJlY29yZC52ZXJzaW9uID0gMlxuICAgIGdhbGF4eS5ldmVudHMgPSBnYWxheHkuZXZlbnRzLm1hcChldmVudCA9PiB7XG4gICAgICBjb25zdCBleGlzdGluZ1JlY2tvbmluZyA9IGV2ZW50LnJvdXRlUmVja29uaW5nXG4gICAgICBjb25zdCBldmVudFJlY2tvbmluZyA9IHR5cGVvZiBleGlzdGluZ1JlY2tvbmluZyA9PT0gJ251bWJlcicgJiYgTnVtYmVyLmlzSW50ZWdlcihleGlzdGluZ1JlY2tvbmluZykgJiYgZXhpc3RpbmdSZWNrb25pbmcgPj0gMCA/IGV4aXN0aW5nUmVja29uaW5nIDogcm91dGVSZWNrb25pbmdcbiAgICAgIHJldHVybiB7IC4uLmV2ZW50LCBhdDogZXZlbnRSZWNrb25pbmcsIHJvdXRlUmVja29uaW5nOiBldmVudFJlY2tvbmluZyB9XG4gICAgfSlcbiAgICBmb3IgKGNvbnN0IHNpdGUgb2YgT2JqZWN0LnZhbHVlcyhnYWxheHkuc2l0ZXMpKSB7XG4gICAgICBzaXRlLnN1cHBsaWVzID8/PSA0NVxuICAgICAgc2l0ZS5zYWx2YWdlID8/PSA0NVxuICAgICAgc2l0ZS5tYXJrZXQgPz89IHsgc3RvY2s6IHsgcHJvdmlzaW9uczogMTIsIGNvbXBvbmVudHM6IDEyLCBzYWx2YWdlOiAxMiwgYmlvc2FtcGxlczogMTIgfSwgZGVtYW5kOiB7IHByb3Zpc2lvbnM6IDEyLCBjb21wb25lbnRzOiAxMiwgc2FsdmFnZTogMTIsIGJpb3NhbXBsZXM6IDEyIH0sIHByaWNlczogeyBwcm92aXNpb25zOiAxMCwgY29tcG9uZW50czogMTAsIHNhbHZhZ2U6IDEwLCBiaW9zYW1wbGVzOiAxMCB9IH1cbiAgICAgIGlmIChPYmplY3QuaXMoc2l0ZS54LCAtMCkpIHNpdGUueCA9IDBcbiAgICAgIGlmIChPYmplY3QuaXMoc2l0ZS55LCAtMCkpIHNpdGUueSA9IDBcbiAgICAgIHNpdGUubGFzdENoYW5nZWRBdCA9IE51bWJlci5pc0ludGVnZXIoc2l0ZS5sYXN0Q2hhbmdlZEF0KSAmJiBzaXRlLmxhc3RDaGFuZ2VkQXQgPj0gMCAmJiBzaXRlLmxhc3RDaGFuZ2VkQXQgPD0gcm91dGVSZWNrb25pbmcgPyBzaXRlLmxhc3RDaGFuZ2VkQXQgOiByb3V0ZVJlY2tvbmluZ1xuICAgIH1cbiAgICBmb3IgKGNvbnN0IHNuYXBzaG90IG9mIE9iamVjdC52YWx1ZXMoZ2FsYXh5LnNpdGVTbmFwc2hvdHMpKSBzbmFwc2hvdC5zYXZlZEF0ID0gTnVtYmVyLmlzSW50ZWdlcihzbmFwc2hvdC5zYXZlZEF0KSAmJiBzbmFwc2hvdC5zYXZlZEF0ID49IDAgJiYgc25hcHNob3Quc2F2ZWRBdCA8PSByb3V0ZVJlY2tvbmluZyA/IHNuYXBzaG90LnNhdmVkQXQgOiByb3V0ZVJlY2tvbmluZ1xuICAgIG5vcm1hbGl6ZUdlbmVyYWxNYW5pZmVzdChnYWxheHkpXG4gICAgaWYgKCFnYWxheHkuc2l0ZXNbZ2FsYXh5LmFjdGl2ZVNpdGVJZF0gfHwgIWdhbGF4eS5jb3VyaWVycy5zb21lKGNvdXJpZXIgPT4gY291cmllci5pZCA9PT0gZ2FsYXh5LmFjdGl2ZUNvdXJpZXJJZCkpIHJldHVybiB1bmRlZmluZWRcbiAgICByZXR1cm4gY2xvbmVHYWxheHkoZ2FsYXh5KVxuICB9IGNhdGNoIHsgcmV0dXJuIHVuZGVmaW5lZCB9XG59XG4iXSwibWFwcGluZ3MiOiJBQUFBLFNBQVNBLFNBQVMsUUFBUSxZQUFZO0FBQ3RDLFNBQVNDLE1BQU0sUUFBUSxRQUFRO0FBRS9CLFNBQVNDLHFCQUFxQixFQUFFQyx3QkFBd0IsUUFBUSxZQUFZO0FBQzVFLFNBQVNDLDRCQUE0QixFQUFFQyxxQ0FBcUMsUUFBUSxtQkFBbUI7QUFDdkcsU0FBU0MsK0JBQStCLEVBQUVDLGdDQUFnQyxFQUFFQywyQkFBMkIsRUFBRUMsaUJBQWlCLEVBQUVDLDJCQUEyQixRQUFRLG1CQUFtQjtBQUVsTCxPQUFPLE1BQU1DLGtCQUFrQixHQUFHLEVBQUU7QUFDcEMsT0FBTyxNQUFNQyxnQkFBZ0IsR0FBRyxFQUFFO0FBQ2xDO0FBQ0EsT0FBTyxNQUFNQyxjQUFjLEdBQUcsRUFBRSxHQUFHLEVBQUUsR0FBRyxJQUFJO0FBQzVDO0FBQ0EsT0FBTyxNQUFNQyxjQUFjLEdBQUcsQ0FBQyxHQUFHRCxjQUFjO0FBQ2hEO0FB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