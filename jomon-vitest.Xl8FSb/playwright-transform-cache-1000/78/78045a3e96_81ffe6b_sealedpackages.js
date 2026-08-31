// b785248d34ee16fd745282504624889f4892495e
import { KESTREL_CALIBRATION_CASE, sealedPackageDefinition } from '../package-content';
import { appendGeneralManifest } from './manifest';
import { ROUTE_RECKONING_NEAR_EXPIRY_UNITS, ROUTE_RECKONING_UNITS_PER_CYCLE, sectorDayFromRouteReckoning } from './route-reckoning';
const PACKAGE_HOLD_CAPACITY = 12;
const copyGalaxy = source => structuredClone(source);
const packageForContract = (galaxy, contract) => contract.packageId ? galaxy.sealedPackages.find(candidate => candidate.id === contract.packageId) : undefined;
const contractForPackage = (galaxy, packageId) => galaxy.sealedPackageContracts.find(contract => contract.packageId === packageId);
const packageCapacityUsed = galaxy => galaxy.sealedPackages.reduce((total, packageRecord) => {
  var _contractForPackage$t, _contractForPackage;
  if (packageRecord.custody === 'routeCache' || packageRecord.custody === 'recipient' || packageRecord.custody === 'abandoned') return total;
  return total + ((_contractForPackage$t = (_contractForPackage = contractForPackage(galaxy, packageRecord.id)) === null || _contractForPackage === void 0 ? void 0 : _contractForPackage.terms.holdUnits) !== null && _contractForPackage$t !== void 0 ? _contractForPackage$t : 0);
}, 0);
const manifestEntry = (galaxy, kind, detail, options = {}) => appendGeneralManifest(galaxy, {
  ...options,
  kind,
  detail,
  source: kind === 'contractExpired' || kind === 'contractNearingExpiry' ? 'contract' : 'custody'
});
const custodyLabel = custody => custody === 'atJomon' ? 'Jomon custody' : custody === 'assignedToCourier' ? 'assigned courier custody' : custody === 'routeCache' ? 'route-cache custody' : custody === 'recipient' ? 'recipient custody' : 'abandoned custody';
const packageTerminal = status => status === 'declined' || status === 'completed' || status === 'failed' || status === 'expired';
const routeCacheIdFor = (galaxy, linkId, chunk) => `sealed-cache:${galaxy.seed}:${linkId}:${chunk}:${galaxy.generalManifest.nextSequence}`;

/** Generates the one authored M1 offer without changing legacy cargo contracts. */
export const addKestrelSealedPackageOffer = source => {
  const galaxy = copyGalaxy(source);
  const destination = galaxy.sites[galaxy.activeSiteId];
  if (!destination) return galaxy;
  const definition = KESTREL_CALIBRATION_CASE;
  const id = `sealed-contract:${galaxy.seed}:${definition.id}:${destination.id}`;
  if (galaxy.sealedPackageContracts.some(contract => contract.id === id)) return galaxy;
  const contract = {
    version: 2,
    id,
    definitionId: definition.id,
    status: 'offered',
    offeredAtSectorDay: galaxy.sectorDay,
    offeredAtRouteReckoning: galaxy.routeReckoning,
    terms: {
      ...structuredClone(definition.terms),
      destinationSiteId: destination.id,
      destinationLabel: destination.name,
      deadlineDay: galaxy.sectorDay + 4,
      deadlineReckoning: galaxy.routeReckoning + 4 * ROUTE_RECKONING_UNITS_PER_CYCLE
    }
  };
  galaxy.sealedPackageContracts.push(contract);
  manifestEntry(galaxy, 'contractOffered', `${definition.title} offered for ${destination.name}.`, {
    contractId: contract.id
  });
  return galaxy;
};
export const offeredSealedPackageContract = galaxy => galaxy.sealedPackageContracts.find(contract => contract.status === 'offered');
export const sealedPackageForContract = (galaxy, contractId) => {
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  return contract ? packageForContract(galaxy, contract) : undefined;
};
export const sealedPackageExteriorForContract = (galaxy, contractId) => {
  var _packageRecord$exteri, _sealedPackageDefinit;
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined;
  return (_packageRecord$exteri = packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.exterior) !== null && _packageRecord$exteri !== void 0 ? _packageRecord$exteri : contract ? (_sealedPackageDefinit = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit === void 0 ? void 0 : _sealedPackageDefinit.exterior : undefined;
};
export const sealedPackageHoldUsed = galaxy => galaxy.cargo.reduce((total, cargo) => total + cargo.units, 0) + packageCapacityUsed(galaxy);
export const acceptSealedPackageContract = (source, contractId, courierId) => {
  const galaxy = copyGalaxy(source);
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  const courier = galaxy.couriers.find(candidate => candidate.id === courierId);
  if (!contract || contract.status !== 'offered') return {
    galaxy,
    changed: false,
    message: 'That sealed-package offer is no longer available.'
  };
  if (!courier || courier.status !== 'available') return {
    galaxy,
    changed: false,
    message: 'Assign an available courier before accepting this package.'
  };
  if (sealedPackageHoldUsed(galaxy) + contract.terms.holdUnits > PACKAGE_HOLD_CAPACITY) return {
    galaxy,
    changed: false,
    message: 'Jomon custody hold lacks capacity for this package.'
  };
  const definition = sealedPackageDefinition(contract.definitionId);
  if (!definition) return {
    galaxy,
    changed: false,
    message: 'The package definition cannot be read.'
  };
  const packageId = `sealed-package:${galaxy.seed}:${contract.id}`;
  const packageRecord = {
    version: 2,
    id: packageId,
    contractId: contract.id,
    definitionId: definition.id,
    hiddenContentsId: definition.hiddenContentsId,
    exterior: structuredClone(definition.exterior),
    sealState: 'intact',
    custody: 'assignedToCourier',
    assignedCourierId: courier.id
  };
  contract.status = 'accepted';
  contract.packageId = packageId;
  contract.assignedCourierId = courier.id;
  contract.acceptedAtSectorDay = galaxy.sectorDay;
  contract.acceptedAtRouteReckoning = galaxy.routeReckoning;
  galaxy.sealedPackages.push(packageRecord);
  manifestEntry(galaxy, 'contractAccepted', `${definition.title} accepted under seal.`, {
    contractId: contract.id,
    packageId,
    courierId: courier.id
  });
  manifestEntry(galaxy, 'custodyTransferred', `${definition.title} transferred from Jomon custody to ${courier.name}.`, {
    contractId: contract.id,
    packageId,
    courierId: courier.id
  });
  return {
    galaxy,
    changed: true,
    message: `${definition.title} assigned to ${courier.name}.`
  };
};
export const declineSealedPackageContract = (source, contractId) => {
  var _sealedPackageDefinit2, _sealedPackageDefinit3;
  const galaxy = copyGalaxy(source);
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  if (!contract || contract.status !== 'offered') return {
    galaxy,
    changed: false,
    message: 'That sealed-package offer cannot be declined now.'
  };
  contract.status = 'declined';
  contract.resolvedAtSectorDay = galaxy.sectorDay;
  contract.resolvedAtRouteReckoning = galaxy.routeReckoning;
  manifestEntry(galaxy, 'contractDeclined', `Declined ${(_sealedPackageDefinit2 = (_sealedPackageDefinit3 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit3 === void 0 ? void 0 : _sealedPackageDefinit3.title) !== null && _sealedPackageDefinit2 !== void 0 ? _sealedPackageDefinit2 : contract.id}.`, {
    contractId: contract.id
  });
  return {
    galaxy,
    changed: true,
    message: 'Offer declined. The decision is recorded in the General Manifest.'
  };
};
export const inspectSealedPackage = (source, contractId) => {
  var _sealedPackageDefinit6, _sealedPackageDefinit7;
  const galaxy = copyGalaxy(source);
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined;
  if (!contract || packageTerminal(contract.status)) return {
    galaxy,
    changed: false,
    message: 'No inspectable sealed package is in custody.'
  };
  if (!packageRecord && contract.status === 'offered') {
    var _sealedPackageDefinit4, _sealedPackageDefinit5;
    manifestEntry(galaxy, 'packageInspected', `Exterior inspection recorded for offered ${(_sealedPackageDefinit4 = (_sealedPackageDefinit5 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit5 === void 0 ? void 0 : _sealedPackageDefinit5.title) !== null && _sealedPackageDefinit4 !== void 0 ? _sealedPackageDefinit4 : contract.id}; contents remain sealed.`, {
      contractId: contract.id
    });
    return {
      galaxy,
      changed: true,
      message: 'Exterior inspection recorded. Acceptance has not transferred custody.'
    };
  }
  if (!packageRecord) return {
    galaxy,
    changed: false,
    message: 'No inspectable sealed package is in custody.'
  };
  const sequence = manifestEntry(galaxy, 'packageInspected', `Exterior inspection recorded for ${(_sealedPackageDefinit6 = (_sealedPackageDefinit7 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit7 === void 0 ? void 0 : _sealedPackageDefinit7.title) !== null && _sealedPackageDefinit6 !== void 0 ? _sealedPackageDefinit6 : packageRecord.id}; contents remain sealed.`, {
    contractId: contract.id,
    packageId: packageRecord.id,
    courierId: packageRecord.assignedCourierId
  });
  packageRecord.inspectedAtSequence = sequence;
  return {
    galaxy,
    changed: true,
    message: 'Exterior inspection recorded. Contents remain unknown.'
  };
};
export const violateSealedPackageSeal = (source, contractId) => {
  const galaxy = copyGalaxy(source);
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined;
  const definition = contract ? sealedPackageDefinition(contract.definitionId) : undefined;
  if (!contract || !packageRecord || !definition || contract.status !== 'accepted') return {
    galaxy,
    changed: false,
    message: 'This contract cannot be opened.'
  };
  if (packageRecord.sealState !== 'intact') return {
    galaxy,
    changed: false,
    message: 'The seal has already been violated.'
  };
  if (packageRecord.custody === 'routeCache' || packageRecord.custody === 'recipient' || packageRecord.custody === 'abandoned') return {
    galaxy,
    changed: false,
    message: 'The package is not available to open.'
  };
  packageRecord.sealState = 'opened';
  packageRecord.revealedContents = structuredClone(definition.contents);
  const sequence = manifestEntry(galaxy, 'sealViolated', `${definition.title} opened: ${definition.contents.knowledge}`, {
    contractId: contract.id,
    packageId: packageRecord.id,
    courierId: packageRecord.assignedCourierId
  });
  packageRecord.openedAtSequence = sequence;
  return {
    galaxy,
    changed: true,
    message: `Seal violated. ${definition.contents.danger}`
  };
};

/** Marks a physical visit; it intentionally performs no delivery. */
export const markSealedPackageDestinationReached = (source, siteId) => {
  const galaxy = copyGalaxy(source);
  for (const contract of galaxy.sealedPackageContracts) {
    if (contract.status === 'accepted' && contract.terms.destinationSiteId === siteId) {
      var _contract$destination, _contract$destination2;
      (_contract$destination = contract.destinationReachedAtSectorDay) !== null && _contract$destination !== void 0 ? _contract$destination : contract.destinationReachedAtSectorDay = galaxy.sectorDay;
      (_contract$destination2 = contract.destinationReachedAtRouteReckoning) !== null && _contract$destination2 !== void 0 ? _contract$destination2 : contract.destinationReachedAtRouteReckoning = galaxy.routeReckoning;
    }
  }
  return galaxy;
};
export const deliverSealedPackage = (source, contractId, hero) => {
  var _sealedPackageDefinit8, _sealedPackageDefinit9;
  const galaxy = copyGalaxy(source);
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined;
  if (!contract || !packageRecord || contract.status !== 'accepted') return {
    galaxy,
    changed: false,
    message: 'No active sealed package can be delivered.'
  };
  if (contract.terms.destinationSiteId !== galaxy.activeSiteId || contract.destinationReachedAtSectorDay === undefined) return {
    galaxy,
    changed: false,
    message: 'The recipient will only accept this package after a physical Kestrel landing.'
  };
  if (contract.terms.deadlineReckoning < galaxy.routeReckoning) return expireSealedPackageContracts(galaxy, galaxy.routeReckoning);
  const intact = packageRecord.sealState === 'intact';
  const payment = intact ? contract.terms.payment : Math.max(0, contract.terms.payment - contract.terms.collateral);
  packageRecord.custody = 'recipient';
  packageRecord.routeCacheId = undefined;
  contract.status = 'completed';
  contract.resolvedAtSectorDay = galaxy.sectorDay;
  contract.resolvedAtRouteReckoning = galaxy.routeReckoning;
  hero.gold += payment;
  manifestEntry(galaxy, 'custodyTransferred', `${(_sealedPackageDefinit8 = (_sealedPackageDefinit9 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit9 === void 0 ? void 0 : _sealedPackageDefinit9.title) !== null && _sealedPackageDefinit8 !== void 0 ? _sealedPackageDefinit8 : packageRecord.id} transferred to ${contract.terms.recipient}.`, {
    contractId: contract.id,
    packageId: packageRecord.id,
    courierId: packageRecord.assignedCourierId
  });
  manifestEntry(galaxy, 'deliveryCompleted', `${intact ? 'Intact' : 'Tampered'} delivery settled for ${payment} credits (${intact ? contract.terms.intactSettlement : contract.terms.tamperedSettlement}).`, {
    contractId: contract.id,
    packageId: packageRecord.id,
    courierId: packageRecord.assignedCourierId
  });
  return {
    galaxy,
    changed: true,
    message: `Delivery complete: ${payment} credits (${intact ? 'intact' : 'tampered'} settlement).`
  };
};
export const refuseSealedPackage = (source, contractId) => {
  var _sealedPackageDefinit0, _sealedPackageDefinit1;
  const galaxy = copyGalaxy(source);
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined;
  if (!contract || !packageRecord || contract.status !== 'accepted') return {
    galaxy,
    changed: false,
    message: 'No active sealed package can be refused.'
  };
  packageRecord.custody = 'abandoned';
  packageRecord.routeCacheId = undefined;
  contract.status = 'failed';
  contract.resolvedAtSectorDay = galaxy.sectorDay;
  contract.resolvedAtRouteReckoning = galaxy.routeReckoning;
  manifestEntry(galaxy, 'deliveryFailed', `Delivery refused for ${(_sealedPackageDefinit0 = (_sealedPackageDefinit1 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit1 === void 0 ? void 0 : _sealedPackageDefinit1.title) !== null && _sealedPackageDefinit0 !== void 0 ? _sealedPackageDefinit0 : packageRecord.id}.`, {
    contractId: contract.id,
    packageId: packageRecord.id,
    courierId: packageRecord.assignedCourierId
  });
  return {
    galaxy,
    changed: true,
    message: 'Package refused. The contract is closed and the custody failure is recorded.'
  };
};
export const abandonSealedPackage = (source, contractId) => {
  var _sealedPackageDefinit10, _sealedPackageDefinit11, _sealedPackageDefinit12, _sealedPackageDefinit13;
  const galaxy = copyGalaxy(source);
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId);
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined;
  if (!contract || !packageRecord || contract.status !== 'accepted') return {
    galaxy,
    changed: false,
    message: 'No active sealed package can be abandoned.'
  };
  packageRecord.custody = 'abandoned';
  packageRecord.routeCacheId = undefined;
  contract.status = 'failed';
  contract.resolvedAtSectorDay = galaxy.sectorDay;
  contract.resolvedAtRouteReckoning = galaxy.routeReckoning;
  manifestEntry(galaxy, 'custodyTransferred', `${(_sealedPackageDefinit10 = (_sealedPackageDefinit11 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit11 === void 0 ? void 0 : _sealedPackageDefinit11.title) !== null && _sealedPackageDefinit10 !== void 0 ? _sealedPackageDefinit10 : packageRecord.id} abandoned from ${custodyLabel('assignedToCourier')}.`, {
    contractId: contract.id,
    packageId: packageRecord.id,
    courierId: packageRecord.assignedCourierId
  });
  manifestEntry(galaxy, 'deliveryFailed', `Abandonment closed ${(_sealedPackageDefinit12 = (_sealedPackageDefinit13 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit13 === void 0 ? void 0 : _sealedPackageDefinit13.title) !== null && _sealedPackageDefinit12 !== void 0 ? _sealedPackageDefinit12 : packageRecord.id}.`, {
    contractId: contract.id,
    packageId: packageRecord.id,
    courierId: packageRecord.assignedCourierId
  });
  return {
    galaxy,
    changed: true,
    message: 'Package abandoned. This is a recorded custody failure, not a delivery.'
  };
};
export const applySealedPackageDeadlineTransitions = galaxy => {
  let changed = 0;
  for (const contract of galaxy.sealedPackageContracts) {
    var _sealedPackageDefinit16, _sealedPackageDefinit17, _sealedPackageDefinit18, _sealedPackageDefinit19;
    if (contract.status !== 'offered' && contract.status !== 'accepted') continue;
    const packageRecord = packageForContract(galaxy, contract);
    if (galaxy.routeReckoning === contract.terms.deadlineReckoning - ROUTE_RECKONING_NEAR_EXPIRY_UNITS && contract.nearingExpiryNotifiedAtRouteReckoning === undefined) {
      var _sealedPackageDefinit14, _sealedPackageDefinit15;
      contract.nearingExpiryNotifiedAtRouteReckoning = galaxy.routeReckoning;
      manifestEntry(galaxy, 'contractNearingExpiry', `${(_sealedPackageDefinit14 = (_sealedPackageDefinit15 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit15 === void 0 ? void 0 : _sealedPackageDefinit15.title) !== null && _sealedPackageDefinit14 !== void 0 ? _sealedPackageDefinit14 : contract.id} has 240 Route Reckoning marks remaining.`, {
        contractId: contract.id,
        packageId: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.id,
        courierId: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.assignedCourierId
      });
      changed++;
    }
    if (contract.terms.deadlineReckoning >= galaxy.routeReckoning) continue;
    contract.status = 'expired';
    contract.resolvedAtRouteReckoning = galaxy.routeReckoning;
    contract.resolvedAtSectorDay = sectorDayFromRouteReckoning(galaxy.routeReckoning);
    manifestEntry(galaxy, 'contractExpired', `${(_sealedPackageDefinit16 = (_sealedPackageDefinit17 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit17 === void 0 ? void 0 : _sealedPackageDefinit17.title) !== null && _sealedPackageDefinit16 !== void 0 ? _sealedPackageDefinit16 : contract.id} expired at Route Reckoning ${galaxy.routeReckoning}.`, {
      contractId: contract.id,
      packageId: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.id,
      courierId: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.assignedCourierId
    });
    manifestEntry(galaxy, 'deliveryFailed', `Deadline failure recorded for ${(_sealedPackageDefinit18 = (_sealedPackageDefinit19 = sealedPackageDefinition(contract.definitionId)) === null || _sealedPackageDefinit19 === void 0 ? void 0 : _sealedPackageDefinit19.title) !== null && _sealedPackageDefinit18 !== void 0 ? _sealedPackageDefinit18 : contract.id}.`, {
      contractId: contract.id,
      packageId: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.id,
      courierId: packageRecord === null || packageRecord === void 0 ? void 0 : packageRecord.assignedCourierId
    });
    changed++;
  }
  return changed;
};
export const expireSealedPackageContracts = (source, routeReckoning = source.routeReckoning) => {
  const galaxy = copyGalaxy(source);
  galaxy.routeReckoning = Math.max(galaxy.routeReckoning, Math.floor(routeReckoning));
  galaxy.sectorDay = sectorDayFromRouteReckoning(galaxy.routeReckoning);
  const changed = applySealedPackageDeadlineTransitions(galaxy);
  const expired = galaxy.sealedPackageContracts.filter(contract => contract.status === 'expired').length - source.sealedPackageContracts.filter(contract => contract.status === 'expired').length;
  return {
    galaxy,
    changed: changed > 0,
    message: expired ? `${expired} sealed-package contract${expired === 1 ? '' : 's'} expired.` : 'No sealed-package deadline has expired.'
  };
};
export const loseSealedPackagesForCourier = (source, courierId, linkId, chunk) => {
  const galaxy = copyGalaxy(source);
  const packages = galaxy.sealedPackages.filter(packageRecord => packageRecord.custody === 'assignedToCourier' && packageRecord.assignedCourierId === courierId);
  if (!packages.length) return galaxy;
  let cache = galaxy.routeCaches.find(candidate => candidate.linkId === linkId && candidate.chunk === chunk && !candidate.recovered);
  if (!cache) {
    cache = {
      id: routeCacheIdFor(galaxy, linkId, chunk),
      linkId,
      chunk,
      cargo: [],
      packages: [],
      recovered: false
    };
    galaxy.routeCaches.push(cache);
  }
  for (const packageRecord of packages) {
    var _sealedPackageDefinit20, _sealedPackageDefinit21, _sealedPackageDefinit22, _sealedPackageDefinit23;
    const contract = contractForPackage(galaxy, packageRecord.id);
    packageRecord.custody = 'routeCache';
    packageRecord.routeCacheId = cache.id;
    if (!cache.packages.includes(packageRecord.id)) cache.packages.push(packageRecord.id);
    if ((contract === null || contract === void 0 ? void 0 : contract.status) === 'accepted') {
      contract.status = 'failed';
      contract.resolvedAtSectorDay = galaxy.sectorDay;
      contract.resolvedAtRouteReckoning = galaxy.routeReckoning;
    }
    manifestEntry(galaxy, 'packageLost', `${(_sealedPackageDefinit20 = (_sealedPackageDefinit21 = sealedPackageDefinition(packageRecord.definitionId)) === null || _sealedPackageDefinit21 === void 0 ? void 0 : _sealedPackageDefinit21.title) !== null && _sealedPackageDefinit20 !== void 0 ? _sealedPackageDefinit20 : packageRecord.id} lost with ${courierId}; cache ${cache.id} marks the last custody record.`, {
      contractId: contract === null || contract === void 0 ? void 0 : contract.id,
      packageId: packageRecord.id,
      courierId,
      routeCacheId: cache.id
    });
    manifestEntry(galaxy, 'deliveryFailed', `Courier loss closed ${(_sealedPackageDefinit22 = (_sealedPackageDefinit23 = sealedPackageDefinition(packageRecord.definitionId)) === null || _sealedPackageDefinit23 === void 0 ? void 0 : _sealedPackageDefinit23.title) !== null && _sealedPackageDefinit22 !== void 0 ? _sealedPackageDefinit22 : packageRecord.id}.`, {
      contractId: contract === null || contract === void 0 ? void 0 : contract.id,
      packageId: packageRecord.id,
      courierId,
      routeCacheId: cache.id
    });
  }
  return galaxy;
};
export const recoverSealedPackageRouteCaches = (source, linkId) => {
  const galaxy = copyGalaxy(source);
  const caches = galaxy.routeCaches.filter(cache => cache.linkId === linkId && !cache.recovered && cache.packages.length);
  if (!caches.length) return {
    galaxy,
    changed: false,
    message: 'No sealed-package cache is recorded on this route.'
  };
  let recovered = 0;
  for (const cache of caches) {
    for (const packageId of cache.packages) {
      var _sealedPackageDefinit24, _sealedPackageDefinit25, _sealedPackageDefinit26, _sealedPackageDefinit27;
      const packageRecord = galaxy.sealedPackages.find(candidate => candidate.id === packageId);
      if (!packageRecord || packageRecord.custody !== 'routeCache') continue;
      const contract = contractForPackage(galaxy, packageRecord.id);
      packageRecord.custody = 'atJomon';
      packageRecord.routeCacheId = undefined;
      manifestEntry(galaxy, 'custodyTransferred', `${(_sealedPackageDefinit24 = (_sealedPackageDefinit25 = sealedPackageDefinition(packageRecord.definitionId)) === null || _sealedPackageDefinit25 === void 0 ? void 0 : _sealedPackageDefinit25.title) !== null && _sealedPackageDefinit24 !== void 0 ? _sealedPackageDefinit24 : packageRecord.id} recovered into Jomon custody.`, {
        contractId: contract === null || contract === void 0 ? void 0 : contract.id,
        packageId: packageRecord.id,
        routeCacheId: cache.id
      });
      manifestEntry(galaxy, 'packageRecovered', `${(_sealedPackageDefinit26 = (_sealedPackageDefinit27 = sealedPackageDefinition(packageRecord.definitionId)) === null || _sealedPackageDefinit27 === void 0 ? void 0 : _sealedPackageDefinit27.title) !== null && _sealedPackageDefinit26 !== void 0 ? _sealedPackageDefinit26 : packageRecord.id} recovered from ${cache.id}; the failed contract remains closed.`, {
        contractId: contract === null || contract === void 0 ? void 0 : contract.id,
        packageId: packageRecord.id,
        routeCacheId: cache.id
      });
      recovered++;
    }
    if (!cache.cargo.length) cache.recovered = true;
  }
  return {
    galaxy,
    changed: recovered > 0,
    message: recovered ? `Recovered ${recovered} sealed package${recovered === 1 ? '' : 's'} to Jomon custody. Failed contracts remain closed.` : 'No sealed package could be recovered from this cache.'
  };
};
export const sealedPackageCustodyLabel = custodyLabel;
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJLRVNUUkVMX0NBTElCUkFUSU9OX0NBU0UiLCJzZWFsZWRQYWNrYWdlRGVmaW5pdGlvbiIsImFwcGVuZEdlbmVyYWxNYW5pZmVzdCIsIlJPVVRFX1JFQ0tPTklOR19ORUFSX0VYUElSWV9VTklUUyIsIlJPVVRFX1JFQ0tPTklOR19VTklUU19QRVJfQ1lDTEUiLCJzZWN0b3JEYXlGcm9tUm91dGVSZWNrb25pbmciLCJQQUNLQUdFX0hPTERfQ0FQQUNJVFkiLCJjb3B5R2FsYXh5Iiwic291cmNlIiwic3RydWN0dXJlZENsb25lIiwicGFja2FnZUZvckNvbnRyYWN0IiwiZ2FsYXh5IiwiY29udHJhY3QiLCJwYWNrYWdlSWQiLCJzZWFsZWRQYWNrYWdlcyIsImZpbmQiLCJjYW5kaWRhdGUiLCJpZCIsInVuZGVmaW5lZCIsImNvbnRyYWN0Rm9yUGFja2FnZSIsInNlYWxlZFBhY2thZ2VDb250cmFjdHMiLCJwYWNrYWdlQ2FwYWNpdHlVc2VkIiwicmVkdWNlIiwidG90YWwiLCJwYWNrYWdlUmVjb3JkIiwiX2NvbnRyYWN0Rm9yUGFja2FnZSR0IiwiX2NvbnRyYWN0Rm9yUGFja2FnZSIsImN1c3RvZHkiLCJ0ZXJtcyIsImhvbGRVbml0cyIsIm1hbmlmZXN0RW50cnkiLCJraW5kIiwiZGV0YWlsIiwib3B0aW9ucyIsImN1c3RvZHlMYWJlbCIsInBhY2thZ2VUZXJtaW5hbCIsInN0YXR1cyIsInJvdXRlQ2FjaGVJZEZvciIsImxpbmtJZCIsImNodW5rIiwic2VlZCIsImdlbmVyYWxNYW5pZmVzdCIsIm5leHRTZXF1ZW5jZSIsImFkZEtlc3RyZWxTZWFsZWRQYWNrYWdlT2ZmZXIiLCJkZXN0aW5hdGlvbiIsInNpdGVzIiwiYWN0aXZlU2l0ZUlkIiwiZGVmaW5pdGlvbiIsInNvbWUiLCJ2ZXJzaW9uIiwiZGVmaW5pdGlvbklkIiwib2ZmZXJlZEF0U2VjdG9yRGF5Iiwic2VjdG9yRGF5Iiwib2ZmZXJlZEF0Um91dGVSZWNrb25pbmciLCJyb3V0ZVJlY2tvbmluZyIsImRlc3RpbmF0aW9uU2l0ZUlkIiwiZGVzdGluYXRpb25MYWJlbCIsIm5hbWUiLCJkZWFkbGluZURheSIsImRlYWRsaW5lUmVja29uaW5nIiwicHVzaCIsInRpdGxlIiwiY29udHJhY3RJZCIsIm9mZmVyZWRTZWFsZWRQYWNrYWdlQ29udHJhY3QiLCJzZWFsZWRQYWNrYWdlRm9yQ29udHJhY3QiLCJzZWFsZWRQYWNrYWdlRXh0ZXJpb3JGb3JDb250cmFjdCIsIl9wYWNrYWdlUmVjb3JkJGV4dGVyaSIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdCIsImV4dGVyaW9yIiwic2VhbGVkUGFja2FnZUhvbGRVc2VkIiwiY2FyZ28iLCJ1bml0cyIsImFjY2VwdFNlYWxlZFBhY2thZ2VDb250cmFjdCIsImNvdXJpZXJJZCIsImNvdXJpZXIiLCJjb3VyaWVycyIsImNoYW5nZWQiLCJtZXNzYWdlIiwiaGlkZGVuQ29udGVudHNJZCIsInNlYWxTdGF0ZSIsImFzc2lnbmVkQ291cmllcklkIiwiYWNjZXB0ZWRBdFNlY3RvckRheSIsImFjY2VwdGVkQXRSb3V0ZVJlY2tvbmluZyIsImRlY2xpbmVTZWFsZWRQYWNrYWdlQ29udHJhY3QiLCJfc2VhbGVkUGFja2FnZURlZmluaXQyIiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0MyIsInJlc29sdmVkQXRTZWN0b3JEYXkiLCJyZXNvbHZlZEF0Um91dGVSZWNrb25pbmciLCJpbnNwZWN0U2VhbGVkUGFja2FnZSIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDYiLCJfc2VhbGVkUGFja2FnZURlZmluaXQ3IiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0NCIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDUiLCJzZXF1ZW5jZSIsImluc3BlY3RlZEF0U2VxdWVuY2UiLCJ2aW9sYXRlU2VhbGVkUGFja2FnZVNlYWwiLCJyZXZlYWxlZENvbnRlbnRzIiwiY29udGVudHMiLCJrbm93bGVkZ2UiLCJvcGVuZWRBdFNlcXVlbmNlIiwiZGFuZ2VyIiwibWFya1NlYWxlZFBhY2thZ2VEZXN0aW5hdGlvblJlYWNoZWQiLCJzaXRlSWQiLCJfY29udHJhY3QkZGVzdGluYXRpb24iLCJfY29udHJhY3QkZGVzdGluYXRpb24yIiwiZGVzdGluYXRpb25SZWFjaGVkQXRTZWN0b3JEYXkiLCJkZXN0aW5hdGlvblJlYWNoZWRBdFJvdXRlUmVja29uaW5nIiwiZGVsaXZlclNlYWxlZFBhY2thZ2UiLCJoZXJvIiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0OCIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDkiLCJleHBpcmVTZWFsZWRQYWNrYWdlQ29udHJhY3RzIiwiaW50YWN0IiwicGF5bWVudCIsIk1hdGgiLCJtYXgiLCJjb2xsYXRlcmFsIiwicm91dGVDYWNoZUlkIiwiZ29sZCIsInJlY2lwaWVudCIsImludGFjdFNldHRsZW1lbnQiLCJ0YW1wZXJlZFNldHRsZW1lbnQiLCJyZWZ1c2VTZWFsZWRQYWNrYWdlIiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0MCIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDEiLCJhYmFuZG9uU2VhbGVkUGFja2FnZSIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDEwIiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0MTEiLCJfc2VhbGVkUGFja2FnZURlZmluaXQxMiIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDEzIiwiYXBwbHlTZWFsZWRQYWNrYWdlRGVhZGxpbmVUcmFuc2l0aW9ucyIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDE2IiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0MTciLCJfc2VhbGVkUGFja2FnZURlZmluaXQxOCIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDE5IiwibmVhcmluZ0V4cGlyeU5vdGlmaWVkQXRSb3V0ZVJlY2tvbmluZyIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDE0IiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0MTUiLCJmbG9vciIsImV4cGlyZWQiLCJmaWx0ZXIiLCJsZW5ndGgiLCJsb3NlU2VhbGVkUGFja2FnZXNGb3JDb3VyaWVyIiwicGFja2FnZXMiLCJjYWNoZSIsInJvdXRlQ2FjaGVzIiwicmVjb3ZlcmVkIiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0MjAiLCJfc2VhbGVkUGFja2FnZURlZmluaXQyMSIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDIyIiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0MjMiLCJpbmNsdWRlcyIsInJlY292ZXJTZWFsZWRQYWNrYWdlUm91dGVDYWNoZXMiLCJjYWNoZXMiLCJfc2VhbGVkUGFja2FnZURlZmluaXQyNCIsIl9zZWFsZWRQYWNrYWdlRGVmaW5pdDI1IiwiX3NlYWxlZFBhY2thZ2VEZWZpbml0MjYiLCJfc2VhbGVkUGFja2FnZURlZmluaXQyNyIsInNlYWxlZFBhY2thZ2VDdXN0b2R5TGFiZWwiXSwic291cmNlcyI6WyJzZWFsZWQtcGFja2FnZXMudHMiXSwic291cmNlc0NvbnRlbnQiOlsiaW1wb3J0IHsgS0VTVFJFTF9DQUxJQlJBVElPTl9DQVNFLCBzZWFsZWRQYWNrYWdlRGVmaW5pdGlvbiB9IGZyb20gJy4uL3BhY2thZ2UtY29udGVudCdcbmltcG9ydCB0eXBlIHsgR2FsYXh5U3RhdGUsIEhlcm8sIFBhY2thZ2VDdXN0b2R5U3RhdGUsIFNlYWxlZFBhY2thZ2UsIFNlYWxlZFBhY2thZ2VDb250cmFjdCB9IGZyb20gJy4uL3R5cGVzJ1xuaW1wb3J0IHsgYXBwZW5kR2VuZXJhbE1hbmlmZXN0LCB0eXBlIE1hbmlmZXN0UmVmZXJlbmNlcyB9IGZyb20gJy4vbWFuaWZlc3QnXG5pbXBvcnQgeyBST1VURV9SRUNLT05JTkdfTkVBUl9FWFBJUllfVU5JVFMsIFJPVVRFX1JFQ0tPTklOR19VTklUU19QRVJfQ1lDTEUsIHNlY3RvckRheUZyb21Sb3V0ZVJlY2tvbmluZyB9IGZyb20gJy4vcm91dGUtcmVja29uaW5nJ1xuXG5jb25zdCBQQUNLQUdFX0hPTERfQ0FQQUNJVFkgPSAxMlxuXG50eXBlIFBhY2thZ2VNdXRhdGlvbiA9IHsgZ2FsYXh5OiBHYWxheHlTdGF0ZTsgY2hhbmdlZDogYm9vbGVhbjsgbWVzc2FnZTogc3RyaW5nIH1cbmNvbnN0IGNvcHlHYWxheHkgPSAoc291cmNlOiBHYWxheHlTdGF0ZSk6IEdhbGF4eVN0YXRlID0+IHN0cnVjdHVyZWRDbG9uZShzb3VyY2UpXG5jb25zdCBwYWNrYWdlRm9yQ29udHJhY3QgPSAoZ2FsYXh5OiBHYWxheHlTdGF0ZSwgY29udHJhY3Q6IFNlYWxlZFBhY2thZ2VDb250cmFjdCk6IFNlYWxlZFBhY2thZ2UgfCB1bmRlZmluZWQgPT4gY29udHJhY3QucGFja2FnZUlkID8gZ2FsYXh5LnNlYWxlZFBhY2thZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gY29udHJhY3QucGFja2FnZUlkKSA6IHVuZGVmaW5lZFxuY29uc3QgY29udHJhY3RGb3JQYWNrYWdlID0gKGdhbGF4eTogR2FsYXh5U3RhdGUsIHBhY2thZ2VJZDogc3RyaW5nKTogU2VhbGVkUGFja2FnZUNvbnRyYWN0IHwgdW5kZWZpbmVkID0+IGdhbGF4eS5zZWFsZWRQYWNrYWdlQ29udHJhY3RzLmZpbmQoY29udHJhY3QgPT4gY29udHJhY3QucGFja2FnZUlkID09PSBwYWNrYWdlSWQpXG5jb25zdCBwYWNrYWdlQ2FwYWNpdHlVc2VkID0gKGdhbGF4eTogR2FsYXh5U3RhdGUpOiBudW1iZXIgPT4gZ2FsYXh5LnNlYWxlZFBhY2thZ2VzLnJlZHVjZSgodG90YWwsIHBhY2thZ2VSZWNvcmQpID0+IHtcbiAgaWYgKHBhY2thZ2VSZWNvcmQuY3VzdG9keSA9PT0gJ3JvdXRlQ2FjaGUnIHx8IHBhY2thZ2VSZWNvcmQuY3VzdG9keSA9PT0gJ3JlY2lwaWVudCcgfHwgcGFja2FnZVJlY29yZC5jdXN0b2R5ID09PSAnYWJhbmRvbmVkJykgcmV0dXJuIHRvdGFsXG4gIHJldHVybiB0b3RhbCArIChjb250cmFjdEZvclBhY2thZ2UoZ2FsYXh5LCBwYWNrYWdlUmVjb3JkLmlkKT8udGVybXMuaG9sZFVuaXRzID8/IDApXG59LCAwKVxuXG5jb25zdCBtYW5pZmVzdEVudHJ5ID0gKGdhbGF4eTogR2FsYXh5U3RhdGUsIGtpbmQ6IFBhcmFtZXRlcnM8dHlwZW9mIGFwcGVuZEdlbmVyYWxNYW5pZmVzdD5bMV1bJ2tpbmQnXSwgZGV0YWlsOiBzdHJpbmcsIG9wdGlvbnM6IE1hbmlmZXN0UmVmZXJlbmNlcyA9IHt9KTogbnVtYmVyID0+IGFwcGVuZEdlbmVyYWxNYW5pZmVzdChnYWxheHksIHsgLi4ub3B0aW9ucywga2luZCwgZGV0YWlsLCBzb3VyY2U6IGtpbmQgPT09ICdjb250cmFjdEV4cGlyZWQnIHx8IGtpbmQgPT09ICdjb250cmFjdE5lYXJpbmdFeHBpcnknID8gJ2NvbnRyYWN0JyA6ICdjdXN0b2R5JyB9KVxuXG5jb25zdCBjdXN0b2R5TGFiZWwgPSAoY3VzdG9keTogUGFja2FnZUN1c3RvZHlTdGF0ZSk6IHN0cmluZyA9PiBjdXN0b2R5ID09PSAnYXRKb21vbicgPyAnSm9tb24gY3VzdG9keScgOiBjdXN0b2R5ID09PSAnYXNzaWduZWRUb0NvdXJpZXInID8gJ2Fzc2lnbmVkIGNvdXJpZXIgY3VzdG9keScgOiBjdXN0b2R5ID09PSAncm91dGVDYWNoZScgPyAncm91dGUtY2FjaGUgY3VzdG9keScgOiBjdXN0b2R5ID09PSAncmVjaXBpZW50JyA/ICdyZWNpcGllbnQgY3VzdG9keScgOiAnYWJhbmRvbmVkIGN1c3RvZHknXG5jb25zdCBwYWNrYWdlVGVybWluYWwgPSAoc3RhdHVzOiBTZWFsZWRQYWNrYWdlQ29udHJhY3RbJ3N0YXR1cyddKTogYm9vbGVhbiA9PiBzdGF0dXMgPT09ICdkZWNsaW5lZCcgfHwgc3RhdHVzID09PSAnY29tcGxldGVkJyB8fCBzdGF0dXMgPT09ICdmYWlsZWQnIHx8IHN0YXR1cyA9PT0gJ2V4cGlyZWQnXG5jb25zdCByb3V0ZUNhY2hlSWRGb3IgPSAoZ2FsYXh5OiBHYWxheHlTdGF0ZSwgbGlua0lkOiBzdHJpbmcsIGNodW5rOiBudW1iZXIpOiBzdHJpbmcgPT4gYHNlYWxlZC1jYWNoZToke2dhbGF4eS5zZWVkfToke2xpbmtJZH06JHtjaHVua306JHtnYWxheHkuZ2VuZXJhbE1hbmlmZXN0Lm5leHRTZXF1ZW5jZX1gXG5cbi8qKiBHZW5lcmF0ZXMgdGhlIG9uZSBhdXRob3JlZCBNMSBvZmZlciB3aXRob3V0IGNoYW5naW5nIGxlZ2FjeSBjYXJnbyBjb250cmFjdHMuICovXG5leHBvcnQgY29uc3QgYWRkS2VzdHJlbFNlYWxlZFBhY2thZ2VPZmZlciA9IChzb3VyY2U6IEdhbGF4eVN0YXRlKTogR2FsYXh5U3RhdGUgPT4ge1xuICBjb25zdCBnYWxheHkgPSBjb3B5R2FsYXh5KHNvdXJjZSlcbiAgY29uc3QgZGVzdGluYXRpb24gPSBnYWxheHkuc2l0ZXNbZ2FsYXh5LmFjdGl2ZVNpdGVJZF1cbiAgaWYgKCFkZXN0aW5hdGlvbikgcmV0dXJuIGdhbGF4eVxuICBjb25zdCBkZWZpbml0aW9uID0gS0VTVFJFTF9DQUxJQlJBVElPTl9DQVNFXG4gIGNvbnN0IGlkID0gYHNlYWxlZC1jb250cmFjdDoke2dhbGF4eS5zZWVkfToke2RlZmluaXRpb24uaWR9OiR7ZGVzdGluYXRpb24uaWR9YFxuICBpZiAoZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHMuc29tZShjb250cmFjdCA9PiBjb250cmFjdC5pZCA9PT0gaWQpKSByZXR1cm4gZ2FsYXh5XG4gIGNvbnN0IGNvbnRyYWN0OiBTZWFsZWRQYWNrYWdlQ29udHJhY3QgPSB7XG4gICAgdmVyc2lvbjogMixcbiAgICBpZCxcbiAgICBkZWZpbml0aW9uSWQ6IGRlZmluaXRpb24uaWQsXG4gICAgc3RhdHVzOiAnb2ZmZXJlZCcsXG4gICAgb2ZmZXJlZEF0U2VjdG9yRGF5OiBnYWxheHkuc2VjdG9yRGF5LFxuICAgIG9mZmVyZWRBdFJvdXRlUmVja29uaW5nOiBnYWxheHkucm91dGVSZWNrb25pbmcsXG4gICAgdGVybXM6IHtcbiAgICAgIC4uLnN0cnVjdHVyZWRDbG9uZShkZWZpbml0aW9uLnRlcm1zKSxcbiAgICAgIGRlc3RpbmF0aW9uU2l0ZUlkOiBkZXN0aW5hdGlvbi5pZCxcbiAgICAgIGRlc3RpbmF0aW9uTGFiZWw6IGRlc3RpbmF0aW9uLm5hbWUsXG4gICAgICBkZWFkbGluZURheTogZ2FsYXh5LnNlY3RvckRheSArIDQsXG4gICAgICBkZWFkbGluZVJlY2tvbmluZzogZ2FsYXh5LnJvdXRlUmVja29uaW5nICsgNCAqIFJPVVRFX1JFQ0tPTklOR19VTklUU19QRVJfQ1lDTEVcbiAgICB9XG4gIH1cbiAgZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHMucHVzaChjb250cmFjdClcbiAgbWFuaWZlc3RFbnRyeShnYWxheHksICdjb250cmFjdE9mZmVyZWQnLCBgJHtkZWZpbml0aW9uLnRpdGxlfSBvZmZlcmVkIGZvciAke2Rlc3RpbmF0aW9uLm5hbWV9LmAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQgfSlcbiAgcmV0dXJuIGdhbGF4eVxufVxuXG5leHBvcnQgY29uc3Qgb2ZmZXJlZFNlYWxlZFBhY2thZ2VDb250cmFjdCA9IChnYWxheHk6IEdhbGF4eVN0YXRlKTogU2VhbGVkUGFja2FnZUNvbnRyYWN0IHwgdW5kZWZpbmVkID0+IGdhbGF4eS5zZWFsZWRQYWNrYWdlQ29udHJhY3RzLmZpbmQoY29udHJhY3QgPT4gY29udHJhY3Quc3RhdHVzID09PSAnb2ZmZXJlZCcpXG5leHBvcnQgY29uc3Qgc2VhbGVkUGFja2FnZUZvckNvbnRyYWN0ID0gKGdhbGF4eTogR2FsYXh5U3RhdGUsIGNvbnRyYWN0SWQ6IHN0cmluZyk6IFNlYWxlZFBhY2thZ2UgfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBjb250cmFjdCA9IGdhbGF4eS5zZWFsZWRQYWNrYWdlQ29udHJhY3RzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gY29udHJhY3RJZClcbiAgcmV0dXJuIGNvbnRyYWN0ID8gcGFja2FnZUZvckNvbnRyYWN0KGdhbGF4eSwgY29udHJhY3QpIDogdW5kZWZpbmVkXG59XG5leHBvcnQgY29uc3Qgc2VhbGVkUGFja2FnZUV4dGVyaW9yRm9yQ29udHJhY3QgPSAoZ2FsYXh5OiBHYWxheHlTdGF0ZSwgY29udHJhY3RJZDogc3RyaW5nKSA9PiB7XG4gIGNvbnN0IGNvbnRyYWN0ID0gZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmlkID09PSBjb250cmFjdElkKVxuICBjb25zdCBwYWNrYWdlUmVjb3JkID0gY29udHJhY3QgPyBwYWNrYWdlRm9yQ29udHJhY3QoZ2FsYXh5LCBjb250cmFjdCkgOiB1bmRlZmluZWRcbiAgcmV0dXJuIHBhY2thZ2VSZWNvcmQ/LmV4dGVyaW9yID8/IChjb250cmFjdCA/IHNlYWxlZFBhY2thZ2VEZWZpbml0aW9uKGNvbnRyYWN0LmRlZmluaXRpb25JZCk/LmV4dGVyaW9yIDogdW5kZWZpbmVkKVxufVxuZXhwb3J0IGNvbnN0IHNlYWxlZFBhY2thZ2VIb2xkVXNlZCA9IChnYWxheHk6IEdhbGF4eVN0YXRlKTogbnVtYmVyID0+IGdhbGF4eS5jYXJnby5yZWR1Y2UoKHRvdGFsLCBjYXJnbykgPT4gdG90YWwgKyBjYXJnby51bml0cywgMCkgKyBwYWNrYWdlQ2FwYWNpdHlVc2VkKGdhbGF4eSlcblxuZXhwb3J0IGNvbnN0IGFjY2VwdFNlYWxlZFBhY2thZ2VDb250cmFjdCA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBjb250cmFjdElkOiBzdHJpbmcsIGNvdXJpZXJJZDogc3RyaW5nKTogUGFja2FnZU11dGF0aW9uID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY29weUdhbGF4eShzb3VyY2UpXG4gIGNvbnN0IGNvbnRyYWN0ID0gZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmlkID09PSBjb250cmFjdElkKVxuICBjb25zdCBjb3VyaWVyID0gZ2FsYXh5LmNvdXJpZXJzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gY291cmllcklkKVxuICBpZiAoIWNvbnRyYWN0IHx8IGNvbnRyYWN0LnN0YXR1cyAhPT0gJ29mZmVyZWQnKSByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IGZhbHNlLCBtZXNzYWdlOiAnVGhhdCBzZWFsZWQtcGFja2FnZSBvZmZlciBpcyBubyBsb25nZXIgYXZhaWxhYmxlLicgfVxuICBpZiAoIWNvdXJpZXIgfHwgY291cmllci5zdGF0dXMgIT09ICdhdmFpbGFibGUnKSByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IGZhbHNlLCBtZXNzYWdlOiAnQXNzaWduIGFuIGF2YWlsYWJsZSBjb3VyaWVyIGJlZm9yZSBhY2NlcHRpbmcgdGhpcyBwYWNrYWdlLicgfVxuICBpZiAoc2VhbGVkUGFja2FnZUhvbGRVc2VkKGdhbGF4eSkgKyBjb250cmFjdC50ZXJtcy5ob2xkVW5pdHMgPiBQQUNLQUdFX0hPTERfQ0FQQUNJVFkpIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogZmFsc2UsIG1lc3NhZ2U6ICdKb21vbiBjdXN0b2R5IGhvbGQgbGFja3MgY2FwYWNpdHkgZm9yIHRoaXMgcGFja2FnZS4nIH1cbiAgY29uc3QgZGVmaW5pdGlvbiA9IHNlYWxlZFBhY2thZ2VEZWZpbml0aW9uKGNvbnRyYWN0LmRlZmluaXRpb25JZClcbiAgaWYgKCFkZWZpbml0aW9uKSByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IGZhbHNlLCBtZXNzYWdlOiAnVGhlIHBhY2thZ2UgZGVmaW5pdGlvbiBjYW5ub3QgYmUgcmVhZC4nIH1cbiAgY29uc3QgcGFja2FnZUlkID0gYHNlYWxlZC1wYWNrYWdlOiR7Z2FsYXh5LnNlZWR9OiR7Y29udHJhY3QuaWR9YFxuICBjb25zdCBwYWNrYWdlUmVjb3JkOiBTZWFsZWRQYWNrYWdlID0ge1xuICAgIHZlcnNpb246IDIsXG4gICAgaWQ6IHBhY2thZ2VJZCxcbiAgICBjb250cmFjdElkOiBjb250cmFjdC5pZCxcbiAgICBkZWZpbml0aW9uSWQ6IGRlZmluaXRpb24uaWQsXG4gICAgaGlkZGVuQ29udGVudHNJZDogZGVmaW5pdGlvbi5oaWRkZW5Db250ZW50c0lkLFxuICAgIGV4dGVyaW9yOiBzdHJ1Y3R1cmVkQ2xvbmUoZGVmaW5pdGlvbi5leHRlcmlvciksXG4gICAgc2VhbFN0YXRlOiAnaW50YWN0JyxcbiAgICBjdXN0b2R5OiAnYXNzaWduZWRUb0NvdXJpZXInLFxuICAgIGFzc2lnbmVkQ291cmllcklkOiBjb3VyaWVyLmlkXG4gIH1cbiAgY29udHJhY3Quc3RhdHVzID0gJ2FjY2VwdGVkJ1xuICBjb250cmFjdC5wYWNrYWdlSWQgPSBwYWNrYWdlSWRcbiAgY29udHJhY3QuYXNzaWduZWRDb3VyaWVySWQgPSBjb3VyaWVyLmlkXG4gIGNvbnRyYWN0LmFjY2VwdGVkQXRTZWN0b3JEYXkgPSBnYWxheHkuc2VjdG9yRGF5XG4gIGNvbnRyYWN0LmFjY2VwdGVkQXRSb3V0ZVJlY2tvbmluZyA9IGdhbGF4eS5yb3V0ZVJlY2tvbmluZ1xuICBnYWxheHkuc2VhbGVkUGFja2FnZXMucHVzaChwYWNrYWdlUmVjb3JkKVxuICBtYW5pZmVzdEVudHJ5KGdhbGF4eSwgJ2NvbnRyYWN0QWNjZXB0ZWQnLCBgJHtkZWZpbml0aW9uLnRpdGxlfSBhY2NlcHRlZCB1bmRlciBzZWFsLmAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQsIHBhY2thZ2VJZCwgY291cmllcklkOiBjb3VyaWVyLmlkIH0pXG4gIG1hbmlmZXN0RW50cnkoZ2FsYXh5LCAnY3VzdG9keVRyYW5zZmVycmVkJywgYCR7ZGVmaW5pdGlvbi50aXRsZX0gdHJhbnNmZXJyZWQgZnJvbSBKb21vbiBjdXN0b2R5IHRvICR7Y291cmllci5uYW1lfS5gLCB7IGNvbnRyYWN0SWQ6IGNvbnRyYWN0LmlkLCBwYWNrYWdlSWQsIGNvdXJpZXJJZDogY291cmllci5pZCB9KVxuICByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IHRydWUsIG1lc3NhZ2U6IGAke2RlZmluaXRpb24udGl0bGV9IGFzc2lnbmVkIHRvICR7Y291cmllci5uYW1lfS5gIH1cbn1cblxuZXhwb3J0IGNvbnN0IGRlY2xpbmVTZWFsZWRQYWNrYWdlQ29udHJhY3QgPSAoc291cmNlOiBHYWxheHlTdGF0ZSwgY29udHJhY3RJZDogc3RyaW5nKTogUGFja2FnZU11dGF0aW9uID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY29weUdhbGF4eShzb3VyY2UpXG4gIGNvbnN0IGNvbnRyYWN0ID0gZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmlkID09PSBjb250cmFjdElkKVxuICBpZiAoIWNvbnRyYWN0IHx8IGNvbnRyYWN0LnN0YXR1cyAhPT0gJ29mZmVyZWQnKSByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IGZhbHNlLCBtZXNzYWdlOiAnVGhhdCBzZWFsZWQtcGFja2FnZSBvZmZlciBjYW5ub3QgYmUgZGVjbGluZWQgbm93LicgfVxuICBjb250cmFjdC5zdGF0dXMgPSAnZGVjbGluZWQnXG4gIGNvbnRyYWN0LnJlc29sdmVkQXRTZWN0b3JEYXkgPSBnYWxheHkuc2VjdG9yRGF5XG4gIGNvbnRyYWN0LnJlc29sdmVkQXRSb3V0ZVJlY2tvbmluZyA9IGdhbGF4eS5yb3V0ZVJlY2tvbmluZ1xuICBtYW5pZmVzdEVudHJ5KGdhbGF4eSwgJ2NvbnRyYWN0RGVjbGluZWQnLCBgRGVjbGluZWQgJHtzZWFsZWRQYWNrYWdlRGVmaW5pdGlvbihjb250cmFjdC5kZWZpbml0aW9uSWQpPy50aXRsZSA/PyBjb250cmFjdC5pZH0uYCwgeyBjb250cmFjdElkOiBjb250cmFjdC5pZCB9KVxuICByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IHRydWUsIG1lc3NhZ2U6ICdPZmZlciBkZWNsaW5lZC4gVGhlIGRlY2lzaW9uIGlzIHJlY29yZGVkIGluIHRoZSBHZW5lcmFsIE1hbmlmZXN0LicgfVxufVxuXG5leHBvcnQgY29uc3QgaW5zcGVjdFNlYWxlZFBhY2thZ2UgPSAoc291cmNlOiBHYWxheHlTdGF0ZSwgY29udHJhY3RJZDogc3RyaW5nKTogUGFja2FnZU11dGF0aW9uID0+IHtcbiAgY29uc3QgZ2FsYXh5ID0gY29weUdhbGF4eShzb3VyY2UpXG4gIGNvbnN0IGNvbnRyYWN0ID0gZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmlkID09PSBjb250cmFjdElkKVxuICBjb25zdCBwYWNrYWdlUmVjb3JkID0gY29udHJhY3QgPyBwYWNrYWdlRm9yQ29udHJhY3QoZ2FsYXh5LCBjb250cmFjdCkgOiB1bmRlZmluZWRcbiAgaWYgKCFjb250cmFjdCB8fCBwYWNrYWdlVGVybWluYWwoY29udHJhY3Quc3RhdHVzKSkgcmV0dXJuIHsgZ2FsYXh5LCBjaGFuZ2VkOiBmYWxzZSwgbWVzc2FnZTogJ05vIGluc3BlY3RhYmxlIHNlYWxlZCBwYWNrYWdlIGlzIGluIGN1c3RvZHkuJyB9XG4gIGlmICghcGFja2FnZVJlY29yZCAmJiBjb250cmFjdC5zdGF0dXMgPT09ICdvZmZlcmVkJykge1xuICAgIG1hbmlmZXN0RW50cnkoZ2FsYXh5LCAncGFja2FnZUluc3BlY3RlZCcsIGBFeHRlcmlvciBpbnNwZWN0aW9uIHJlY29yZGVkIGZvciBvZmZlcmVkICR7c2VhbGVkUGFja2FnZURlZmluaXRpb24oY29udHJhY3QuZGVmaW5pdGlvbklkKT8udGl0bGUgPz8gY29udHJhY3QuaWR9OyBjb250ZW50cyByZW1haW4gc2VhbGVkLmAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQgfSlcbiAgICByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IHRydWUsIG1lc3NhZ2U6ICdFeHRlcmlvciBpbnNwZWN0aW9uIHJlY29yZGVkLiBBY2NlcHRhbmNlIGhhcyBub3QgdHJhbnNmZXJyZWQgY3VzdG9keS4nIH1cbiAgfVxuICBpZiAoIXBhY2thZ2VSZWNvcmQpIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogZmFsc2UsIG1lc3NhZ2U6ICdObyBpbnNwZWN0YWJsZSBzZWFsZWQgcGFja2FnZSBpcyBpbiBjdXN0b2R5LicgfVxuICBjb25zdCBzZXF1ZW5jZSA9IG1hbmlmZXN0RW50cnkoZ2FsYXh5LCAncGFja2FnZUluc3BlY3RlZCcsIGBFeHRlcmlvciBpbnNwZWN0aW9uIHJlY29yZGVkIGZvciAke3NlYWxlZFBhY2thZ2VEZWZpbml0aW9uKGNvbnRyYWN0LmRlZmluaXRpb25JZCk/LnRpdGxlID8/IHBhY2thZ2VSZWNvcmQuaWR9OyBjb250ZW50cyByZW1haW4gc2VhbGVkLmAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQsIHBhY2thZ2VJZDogcGFja2FnZVJlY29yZC5pZCwgY291cmllcklkOiBwYWNrYWdlUmVjb3JkLmFzc2lnbmVkQ291cmllcklkIH0pXG4gIHBhY2thZ2VSZWNvcmQuaW5zcGVjdGVkQXRTZXF1ZW5jZSA9IHNlcXVlbmNlXG4gIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogdHJ1ZSwgbWVzc2FnZTogJ0V4dGVyaW9yIGluc3BlY3Rpb24gcmVjb3JkZWQuIENvbnRlbnRzIHJlbWFpbiB1bmtub3duLicgfVxufVxuXG5leHBvcnQgY29uc3QgdmlvbGF0ZVNlYWxlZFBhY2thZ2VTZWFsID0gKHNvdXJjZTogR2FsYXh5U3RhdGUsIGNvbnRyYWN0SWQ6IHN0cmluZyk6IFBhY2thZ2VNdXRhdGlvbiA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNvcHlHYWxheHkoc291cmNlKVxuICBjb25zdCBjb250cmFjdCA9IGdhbGF4eS5zZWFsZWRQYWNrYWdlQ29udHJhY3RzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gY29udHJhY3RJZClcbiAgY29uc3QgcGFja2FnZVJlY29yZCA9IGNvbnRyYWN0ID8gcGFja2FnZUZvckNvbnRyYWN0KGdhbGF4eSwgY29udHJhY3QpIDogdW5kZWZpbmVkXG4gIGNvbnN0IGRlZmluaXRpb24gPSBjb250cmFjdCA/IHNlYWxlZFBhY2thZ2VEZWZpbml0aW9uKGNvbnRyYWN0LmRlZmluaXRpb25JZCkgOiB1bmRlZmluZWRcbiAgaWYgKCFjb250cmFjdCB8fCAhcGFja2FnZVJlY29yZCB8fCAhZGVmaW5pdGlvbiB8fCBjb250cmFjdC5zdGF0dXMgIT09ICdhY2NlcHRlZCcpIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogZmFsc2UsIG1lc3NhZ2U6ICdUaGlzIGNvbnRyYWN0IGNhbm5vdCBiZSBvcGVuZWQuJyB9XG4gIGlmIChwYWNrYWdlUmVjb3JkLnNlYWxTdGF0ZSAhPT0gJ2ludGFjdCcpIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogZmFsc2UsIG1lc3NhZ2U6ICdUaGUgc2VhbCBoYXMgYWxyZWFkeSBiZWVuIHZpb2xhdGVkLicgfVxuICBpZiAocGFja2FnZVJlY29yZC5jdXN0b2R5ID09PSAncm91dGVDYWNoZScgfHwgcGFja2FnZVJlY29yZC5jdXN0b2R5ID09PSAncmVjaXBpZW50JyB8fCBwYWNrYWdlUmVjb3JkLmN1c3RvZHkgPT09ICdhYmFuZG9uZWQnKSByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IGZhbHNlLCBtZXNzYWdlOiAnVGhlIHBhY2thZ2UgaXMgbm90IGF2YWlsYWJsZSB0byBvcGVuLicgfVxuICBwYWNrYWdlUmVjb3JkLnNlYWxTdGF0ZSA9ICdvcGVuZWQnXG4gIHBhY2thZ2VSZWNvcmQucmV2ZWFsZWRDb250ZW50cyA9IHN0cnVjdHVyZWRDbG9uZShkZWZpbml0aW9uLmNvbnRlbnRzKVxuICBjb25zdCBzZXF1ZW5jZSA9IG1hbmlmZXN0RW50cnkoZ2FsYXh5LCAnc2VhbFZpb2xhdGVkJywgYCR7ZGVmaW5pdGlvbi50aXRsZX0gb3BlbmVkOiAke2RlZmluaXRpb24uY29udGVudHMua25vd2xlZGdlfWAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQsIHBhY2thZ2VJZDogcGFja2FnZVJlY29yZC5pZCwgY291cmllcklkOiBwYWNrYWdlUmVjb3JkLmFzc2lnbmVkQ291cmllcklkIH0pXG4gIHBhY2thZ2VSZWNvcmQub3BlbmVkQXRTZXF1ZW5jZSA9IHNlcXVlbmNlXG4gIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogdHJ1ZSwgbWVzc2FnZTogYFNlYWwgdmlvbGF0ZWQuICR7ZGVmaW5pdGlvbi5jb250ZW50cy5kYW5nZXJ9YCB9XG59XG5cbi8qKiBNYXJrcyBhIHBoeXNpY2FsIHZpc2l0OyBpdCBpbnRlbnRpb25hbGx5IHBlcmZvcm1zIG5vIGRlbGl2ZXJ5LiAqL1xuZXhwb3J0IGNvbnN0IG1hcmtTZWFsZWRQYWNrYWdlRGVzdGluYXRpb25SZWFjaGVkID0gKHNvdXJjZTogR2FsYXh5U3RhdGUsIHNpdGVJZDogc3RyaW5nKTogR2FsYXh5U3RhdGUgPT4ge1xuICBjb25zdCBnYWxheHkgPSBjb3B5R2FsYXh5KHNvdXJjZSlcbiAgZm9yIChjb25zdCBjb250cmFjdCBvZiBnYWxheHkuc2VhbGVkUGFja2FnZUNvbnRyYWN0cykge1xuICAgIGlmIChjb250cmFjdC5zdGF0dXMgPT09ICdhY2NlcHRlZCcgJiYgY29udHJhY3QudGVybXMuZGVzdGluYXRpb25TaXRlSWQgPT09IHNpdGVJZCkge1xuICAgICAgY29udHJhY3QuZGVzdGluYXRpb25SZWFjaGVkQXRTZWN0b3JEYXkgPz89IGdhbGF4eS5zZWN0b3JEYXlcbiAgICAgIGNvbnRyYWN0LmRlc3RpbmF0aW9uUmVhY2hlZEF0Um91dGVSZWNrb25pbmcgPz89IGdhbGF4eS5yb3V0ZVJlY2tvbmluZ1xuICAgIH1cbiAgfVxuICByZXR1cm4gZ2FsYXh5XG59XG5cbmV4cG9ydCBjb25zdCBkZWxpdmVyU2VhbGVkUGFja2FnZSA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBjb250cmFjdElkOiBzdHJpbmcsIGhlcm86IEhlcm8pOiBQYWNrYWdlTXV0YXRpb24gPT4ge1xuICBjb25zdCBnYWxheHkgPSBjb3B5R2FsYXh5KHNvdXJjZSlcbiAgY29uc3QgY29udHJhY3QgPSBnYWxheHkuc2VhbGVkUGFja2FnZUNvbnRyYWN0cy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUuaWQgPT09IGNvbnRyYWN0SWQpXG4gIGNvbnN0IHBhY2thZ2VSZWNvcmQgPSBjb250cmFjdCA/IHBhY2thZ2VGb3JDb250cmFjdChnYWxheHksIGNvbnRyYWN0KSA6IHVuZGVmaW5lZFxuICBpZiAoIWNvbnRyYWN0IHx8ICFwYWNrYWdlUmVjb3JkIHx8IGNvbnRyYWN0LnN0YXR1cyAhPT0gJ2FjY2VwdGVkJykgcmV0dXJuIHsgZ2FsYXh5LCBjaGFuZ2VkOiBmYWxzZSwgbWVzc2FnZTogJ05vIGFjdGl2ZSBzZWFsZWQgcGFja2FnZSBjYW4gYmUgZGVsaXZlcmVkLicgfVxuICBpZiAoY29udHJhY3QudGVybXMuZGVzdGluYXRpb25TaXRlSWQgIT09IGdhbGF4eS5hY3RpdmVTaXRlSWQgfHwgY29udHJhY3QuZGVzdGluYXRpb25SZWFjaGVkQXRTZWN0b3JEYXkgPT09IHVuZGVmaW5lZCkgcmV0dXJuIHsgZ2FsYXh5LCBjaGFuZ2VkOiBmYWxzZSwgbWVzc2FnZTogJ1RoZSByZWNpcGllbnQgd2lsbCBvbmx5IGFjY2VwdCB0aGlzIHBhY2thZ2UgYWZ0ZXIgYSBwaHlzaWNhbCBLZXN0cmVsIGxhbmRpbmcuJyB9XG4gIGlmIChjb250cmFjdC50ZXJtcy5kZWFkbGluZVJlY2tvbmluZyA8IGdhbGF4eS5yb3V0ZVJlY2tvbmluZykgcmV0dXJuIGV4cGlyZVNlYWxlZFBhY2thZ2VDb250cmFjdHMoZ2FsYXh5LCBnYWxheHkucm91dGVSZWNrb25pbmcpXG4gIGNvbnN0IGludGFjdCA9IHBhY2thZ2VSZWNvcmQuc2VhbFN0YXRlID09PSAnaW50YWN0J1xuICBjb25zdCBwYXltZW50ID0gaW50YWN0ID8gY29udHJhY3QudGVybXMucGF5bWVudCA6IE1hdGgubWF4KDAsIGNvbnRyYWN0LnRlcm1zLnBheW1lbnQgLSBjb250cmFjdC50ZXJtcy5jb2xsYXRlcmFsKVxuICBwYWNrYWdlUmVjb3JkLmN1c3RvZHkgPSAncmVjaXBpZW50J1xuICBwYWNrYWdlUmVjb3JkLnJvdXRlQ2FjaGVJZCA9IHVuZGVmaW5lZFxuICBjb250cmFjdC5zdGF0dXMgPSAnY29tcGxldGVkJ1xuICBjb250cmFjdC5yZXNvbHZlZEF0U2VjdG9yRGF5ID0gZ2FsYXh5LnNlY3RvckRheVxuICBjb250cmFjdC5yZXNvbHZlZEF0Um91dGVSZWNrb25pbmcgPSBnYWxheHkucm91dGVSZWNrb25pbmdcbiAgaGVyby5nb2xkICs9IHBheW1lbnRcbiAgbWFuaWZlc3RFbnRyeShnYWxheHksICdjdXN0b2R5VHJhbnNmZXJyZWQnLCBgJHtzZWFsZWRQYWNrYWdlRGVmaW5pdGlvbihjb250cmFjdC5kZWZpbml0aW9uSWQpPy50aXRsZSA/PyBwYWNrYWdlUmVjb3JkLmlkfSB0cmFuc2ZlcnJlZCB0byAke2NvbnRyYWN0LnRlcm1zLnJlY2lwaWVudH0uYCwgeyBjb250cmFjdElkOiBjb250cmFjdC5pZCwgcGFja2FnZUlkOiBwYWNrYWdlUmVjb3JkLmlkLCBjb3VyaWVySWQ6IHBhY2thZ2VSZWNvcmQuYXNzaWduZWRDb3VyaWVySWQgfSlcbiAgbWFuaWZlc3RFbnRyeShnYWxheHksICdkZWxpdmVyeUNvbXBsZXRlZCcsIGAke2ludGFjdCA/ICdJbnRhY3QnIDogJ1RhbXBlcmVkJ30gZGVsaXZlcnkgc2V0dGxlZCBmb3IgJHtwYXltZW50fSBjcmVkaXRzICgke2ludGFjdCA/IGNvbnRyYWN0LnRlcm1zLmludGFjdFNldHRsZW1lbnQgOiBjb250cmFjdC50ZXJtcy50YW1wZXJlZFNldHRsZW1lbnR9KS5gLCB7IGNvbnRyYWN0SWQ6IGNvbnRyYWN0LmlkLCBwYWNrYWdlSWQ6IHBhY2thZ2VSZWNvcmQuaWQsIGNvdXJpZXJJZDogcGFja2FnZVJlY29yZC5hc3NpZ25lZENvdXJpZXJJZCB9KVxuICByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IHRydWUsIG1lc3NhZ2U6IGBEZWxpdmVyeSBjb21wbGV0ZTogJHtwYXltZW50fSBjcmVkaXRzICgke2ludGFjdCA/ICdpbnRhY3QnIDogJ3RhbXBlcmVkJ30gc2V0dGxlbWVudCkuYCB9XG59XG5cbmV4cG9ydCBjb25zdCByZWZ1c2VTZWFsZWRQYWNrYWdlID0gKHNvdXJjZTogR2FsYXh5U3RhdGUsIGNvbnRyYWN0SWQ6IHN0cmluZyk6IFBhY2thZ2VNdXRhdGlvbiA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNvcHlHYWxheHkoc291cmNlKVxuICBjb25zdCBjb250cmFjdCA9IGdhbGF4eS5zZWFsZWRQYWNrYWdlQ29udHJhY3RzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gY29udHJhY3RJZClcbiAgY29uc3QgcGFja2FnZVJlY29yZCA9IGNvbnRyYWN0ID8gcGFja2FnZUZvckNvbnRyYWN0KGdhbGF4eSwgY29udHJhY3QpIDogdW5kZWZpbmVkXG4gIGlmICghY29udHJhY3QgfHwgIXBhY2thZ2VSZWNvcmQgfHwgY29udHJhY3Quc3RhdHVzICE9PSAnYWNjZXB0ZWQnKSByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IGZhbHNlLCBtZXNzYWdlOiAnTm8gYWN0aXZlIHNlYWxlZCBwYWNrYWdlIGNhbiBiZSByZWZ1c2VkLicgfVxuICBwYWNrYWdlUmVjb3JkLmN1c3RvZHkgPSAnYWJhbmRvbmVkJ1xuICBwYWNrYWdlUmVjb3JkLnJvdXRlQ2FjaGVJZCA9IHVuZGVmaW5lZFxuICBjb250cmFjdC5zdGF0dXMgPSAnZmFpbGVkJ1xuICBjb250cmFjdC5yZXNvbHZlZEF0U2VjdG9yRGF5ID0gZ2FsYXh5LnNlY3RvckRheVxuICBjb250cmFjdC5yZXNvbHZlZEF0Um91dGVSZWNrb25pbmcgPSBnYWxheHkucm91dGVSZWNrb25pbmdcbiAgbWFuaWZlc3RFbnRyeShnYWxheHksICdkZWxpdmVyeUZhaWxlZCcsIGBEZWxpdmVyeSByZWZ1c2VkIGZvciAke3NlYWxlZFBhY2thZ2VEZWZpbml0aW9uKGNvbnRyYWN0LmRlZmluaXRpb25JZCk/LnRpdGxlID8/IHBhY2thZ2VSZWNvcmQuaWR9LmAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQsIHBhY2thZ2VJZDogcGFja2FnZVJlY29yZC5pZCwgY291cmllcklkOiBwYWNrYWdlUmVjb3JkLmFzc2lnbmVkQ291cmllcklkIH0pXG4gIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogdHJ1ZSwgbWVzc2FnZTogJ1BhY2thZ2UgcmVmdXNlZC4gVGhlIGNvbnRyYWN0IGlzIGNsb3NlZCBhbmQgdGhlIGN1c3RvZHkgZmFpbHVyZSBpcyByZWNvcmRlZC4nIH1cbn1cblxuZXhwb3J0IGNvbnN0IGFiYW5kb25TZWFsZWRQYWNrYWdlID0gKHNvdXJjZTogR2FsYXh5U3RhdGUsIGNvbnRyYWN0SWQ6IHN0cmluZyk6IFBhY2thZ2VNdXRhdGlvbiA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNvcHlHYWxheHkoc291cmNlKVxuICBjb25zdCBjb250cmFjdCA9IGdhbGF4eS5zZWFsZWRQYWNrYWdlQ29udHJhY3RzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gY29udHJhY3RJZClcbiAgY29uc3QgcGFja2FnZVJlY29yZCA9IGNvbnRyYWN0ID8gcGFja2FnZUZvckNvbnRyYWN0KGdhbGF4eSwgY29udHJhY3QpIDogdW5kZWZpbmVkXG4gIGlmICghY29udHJhY3QgfHwgIXBhY2thZ2VSZWNvcmQgfHwgY29udHJhY3Quc3RhdHVzICE9PSAnYWNjZXB0ZWQnKSByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IGZhbHNlLCBtZXNzYWdlOiAnTm8gYWN0aXZlIHNlYWxlZCBwYWNrYWdlIGNhbiBiZSBhYmFuZG9uZWQuJyB9XG4gIHBhY2thZ2VSZWNvcmQuY3VzdG9keSA9ICdhYmFuZG9uZWQnXG4gIHBhY2thZ2VSZWNvcmQucm91dGVDYWNoZUlkID0gdW5kZWZpbmVkXG4gIGNvbnRyYWN0LnN0YXR1cyA9ICdmYWlsZWQnXG4gIGNvbnRyYWN0LnJlc29sdmVkQXRTZWN0b3JEYXkgPSBnYWxheHkuc2VjdG9yRGF5XG4gIGNvbnRyYWN0LnJlc29sdmVkQXRSb3V0ZVJlY2tvbmluZyA9IGdhbGF4eS5yb3V0ZVJlY2tvbmluZ1xuICBtYW5pZmVzdEVudHJ5KGdhbGF4eSwgJ2N1c3RvZHlUcmFuc2ZlcnJlZCcsIGAke3NlYWxlZFBhY2thZ2VEZWZpbml0aW9uKGNvbnRyYWN0LmRlZmluaXRpb25JZCk/LnRpdGxlID8/IHBhY2thZ2VSZWNvcmQuaWR9IGFiYW5kb25lZCBmcm9tICR7Y3VzdG9keUxhYmVsKCdhc3NpZ25lZFRvQ291cmllcicpfS5gLCB7IGNvbnRyYWN0SWQ6IGNvbnRyYWN0LmlkLCBwYWNrYWdlSWQ6IHBhY2thZ2VSZWNvcmQuaWQsIGNvdXJpZXJJZDogcGFja2FnZVJlY29yZC5hc3NpZ25lZENvdXJpZXJJZCB9KVxuICBtYW5pZmVzdEVudHJ5KGdhbGF4eSwgJ2RlbGl2ZXJ5RmFpbGVkJywgYEFiYW5kb25tZW50IGNsb3NlZCAke3NlYWxlZFBhY2thZ2VEZWZpbml0aW9uKGNvbnRyYWN0LmRlZmluaXRpb25JZCk/LnRpdGxlID8/IHBhY2thZ2VSZWNvcmQuaWR9LmAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQsIHBhY2thZ2VJZDogcGFja2FnZVJlY29yZC5pZCwgY291cmllcklkOiBwYWNrYWdlUmVjb3JkLmFzc2lnbmVkQ291cmllcklkIH0pXG4gIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogdHJ1ZSwgbWVzc2FnZTogJ1BhY2thZ2UgYWJhbmRvbmVkLiBUaGlzIGlzIGEgcmVjb3JkZWQgY3VzdG9keSBmYWlsdXJlLCBub3QgYSBkZWxpdmVyeS4nIH1cbn1cblxuZXhwb3J0IGNvbnN0IGFwcGx5U2VhbGVkUGFja2FnZURlYWRsaW5lVHJhbnNpdGlvbnMgPSAoZ2FsYXh5OiBHYWxheHlTdGF0ZSk6IG51bWJlciA9PiB7XG4gIGxldCBjaGFuZ2VkID0gMFxuICBmb3IgKGNvbnN0IGNvbnRyYWN0IG9mIGdhbGF4eS5zZWFsZWRQYWNrYWdlQ29udHJhY3RzKSB7XG4gICAgaWYgKGNvbnRyYWN0LnN0YXR1cyAhPT0gJ29mZmVyZWQnICYmIGNvbnRyYWN0LnN0YXR1cyAhPT0gJ2FjY2VwdGVkJykgY29udGludWVcbiAgICBjb25zdCBwYWNrYWdlUmVjb3JkID0gcGFja2FnZUZvckNvbnRyYWN0KGdhbGF4eSwgY29udHJhY3QpXG4gICAgaWYgKGdhbGF4eS5yb3V0ZVJlY2tvbmluZyA9PT0gY29udHJhY3QudGVybXMuZGVhZGxpbmVSZWNrb25pbmcgLSBST1VURV9SRUNLT05JTkdfTkVBUl9FWFBJUllfVU5JVFMgJiYgY29udHJhY3QubmVhcmluZ0V4cGlyeU5vdGlmaWVkQXRSb3V0ZVJlY2tvbmluZyA9PT0gdW5kZWZpbmVkKSB7XG4gICAgICBjb250cmFjdC5uZWFyaW5nRXhwaXJ5Tm90aWZpZWRBdFJvdXRlUmVja29uaW5nID0gZ2FsYXh5LnJvdXRlUmVja29uaW5nXG4gICAgICBtYW5pZmVzdEVudHJ5KGdhbGF4eSwgJ2NvbnRyYWN0TmVhcmluZ0V4cGlyeScsIGAke3NlYWxlZFBhY2thZ2VEZWZpbml0aW9uKGNvbnRyYWN0LmRlZmluaXRpb25JZCk/LnRpdGxlID8/IGNvbnRyYWN0LmlkfSBoYXMgMjQwIFJvdXRlIFJlY2tvbmluZyBtYXJrcyByZW1haW5pbmcuYCwgeyBjb250cmFjdElkOiBjb250cmFjdC5pZCwgcGFja2FnZUlkOiBwYWNrYWdlUmVjb3JkPy5pZCwgY291cmllcklkOiBwYWNrYWdlUmVjb3JkPy5hc3NpZ25lZENvdXJpZXJJZCB9KVxuICAgICAgY2hhbmdlZCsrXG4gICAgfVxuICAgIGlmIChjb250cmFjdC50ZXJtcy5kZWFkbGluZVJlY2tvbmluZyA+PSBnYWxheHkucm91dGVSZWNrb25pbmcpIGNvbnRpbnVlXG4gICAgY29udHJhY3Quc3RhdHVzID0gJ2V4cGlyZWQnXG4gICAgY29udHJhY3QucmVzb2x2ZWRBdFJvdXRlUmVja29uaW5nID0gZ2FsYXh5LnJvdXRlUmVja29uaW5nXG4gICAgY29udHJhY3QucmVzb2x2ZWRBdFNlY3RvckRheSA9IHNlY3RvckRheUZyb21Sb3V0ZVJlY2tvbmluZyhnYWxheHkucm91dGVSZWNrb25pbmcpXG4gICAgbWFuaWZlc3RFbnRyeShnYWxheHksICdjb250cmFjdEV4cGlyZWQnLCBgJHtzZWFsZWRQYWNrYWdlRGVmaW5pdGlvbihjb250cmFjdC5kZWZpbml0aW9uSWQpPy50aXRsZSA/PyBjb250cmFjdC5pZH0gZXhwaXJlZCBhdCBSb3V0ZSBSZWNrb25pbmcgJHtnYWxheHkucm91dGVSZWNrb25pbmd9LmAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQsIHBhY2thZ2VJZDogcGFja2FnZVJlY29yZD8uaWQsIGNvdXJpZXJJZDogcGFja2FnZVJlY29yZD8uYXNzaWduZWRDb3VyaWVySWQgfSlcbiAgICBtYW5pZmVzdEVudHJ5KGdhbGF4eSwgJ2RlbGl2ZXJ5RmFpbGVkJywgYERlYWRsaW5lIGZhaWx1cmUgcmVjb3JkZWQgZm9yICR7c2VhbGVkUGFja2FnZURlZmluaXRpb24oY29udHJhY3QuZGVmaW5pdGlvbklkKT8udGl0bGUgPz8gY29udHJhY3QuaWR9LmAsIHsgY29udHJhY3RJZDogY29udHJhY3QuaWQsIHBhY2thZ2VJZDogcGFja2FnZVJlY29yZD8uaWQsIGNvdXJpZXJJZDogcGFja2FnZVJlY29yZD8uYXNzaWduZWRDb3VyaWVySWQgfSlcbiAgICBjaGFuZ2VkKytcbiAgfVxuICByZXR1cm4gY2hhbmdlZFxufVxuXG5leHBvcnQgY29uc3QgZXhwaXJlU2VhbGVkUGFja2FnZUNvbnRyYWN0cyA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCByb3V0ZVJlY2tvbmluZyA9IHNvdXJjZS5yb3V0ZVJlY2tvbmluZyk6IFBhY2thZ2VNdXRhdGlvbiA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNvcHlHYWxheHkoc291cmNlKVxuICBnYWxheHkucm91dGVSZWNrb25pbmcgPSBNYXRoLm1heChnYWxheHkucm91dGVSZWNrb25pbmcsIE1hdGguZmxvb3Iocm91dGVSZWNrb25pbmcpKVxuICBnYWxheHkuc2VjdG9yRGF5ID0gc2VjdG9yRGF5RnJvbVJvdXRlUmVja29uaW5nKGdhbGF4eS5yb3V0ZVJlY2tvbmluZylcbiAgY29uc3QgY2hhbmdlZCA9IGFwcGx5U2VhbGVkUGFja2FnZURlYWRsaW5lVHJhbnNpdGlvbnMoZ2FsYXh5KVxuICBjb25zdCBleHBpcmVkID0gZ2FsYXh5LnNlYWxlZFBhY2thZ2VDb250cmFjdHMuZmlsdGVyKGNvbnRyYWN0ID0+IGNvbnRyYWN0LnN0YXR1cyA9PT0gJ2V4cGlyZWQnKS5sZW5ndGggLSBzb3VyY2Uuc2VhbGVkUGFja2FnZUNvbnRyYWN0cy5maWx0ZXIoY29udHJhY3QgPT4gY29udHJhY3Quc3RhdHVzID09PSAnZXhwaXJlZCcpLmxlbmd0aFxuICByZXR1cm4geyBnYWxheHksIGNoYW5nZWQ6IGNoYW5nZWQgPiAwLCBtZXNzYWdlOiBleHBpcmVkID8gYCR7ZXhwaXJlZH0gc2VhbGVkLXBhY2thZ2UgY29udHJhY3Qke2V4cGlyZWQgPT09IDEgPyAnJyA6ICdzJ30gZXhwaXJlZC5gIDogJ05vIHNlYWxlZC1wYWNrYWdlIGRlYWRsaW5lIGhhcyBleHBpcmVkLicgfVxufVxuXG5leHBvcnQgY29uc3QgbG9zZVNlYWxlZFBhY2thZ2VzRm9yQ291cmllciA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBjb3VyaWVySWQ6IHN0cmluZywgbGlua0lkOiBzdHJpbmcsIGNodW5rOiBudW1iZXIpOiBHYWxheHlTdGF0ZSA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNvcHlHYWxheHkoc291cmNlKVxuICBjb25zdCBwYWNrYWdlcyA9IGdhbGF4eS5zZWFsZWRQYWNrYWdlcy5maWx0ZXIocGFja2FnZVJlY29yZCA9PiBwYWNrYWdlUmVjb3JkLmN1c3RvZHkgPT09ICdhc3NpZ25lZFRvQ291cmllcicgJiYgcGFja2FnZVJlY29yZC5hc3NpZ25lZENvdXJpZXJJZCA9PT0gY291cmllcklkKVxuICBpZiAoIXBhY2thZ2VzLmxlbmd0aCkgcmV0dXJuIGdhbGF4eVxuICBsZXQgY2FjaGUgPSBnYWxheHkucm91dGVDYWNoZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmxpbmtJZCA9PT0gbGlua0lkICYmIGNhbmRpZGF0ZS5jaHVuayA9PT0gY2h1bmsgJiYgIWNhbmRpZGF0ZS5yZWNvdmVyZWQpXG4gIGlmICghY2FjaGUpIHtcbiAgICBjYWNoZSA9IHsgaWQ6IHJvdXRlQ2FjaGVJZEZvcihnYWxheHksIGxpbmtJZCwgY2h1bmspLCBsaW5rSWQsIGNodW5rLCBjYXJnbzogW10sIHBhY2thZ2VzOiBbXSwgcmVjb3ZlcmVkOiBmYWxzZSB9XG4gICAgZ2FsYXh5LnJvdXRlQ2FjaGVzLnB1c2goY2FjaGUpXG4gIH1cbiAgZm9yIChjb25zdCBwYWNrYWdlUmVjb3JkIG9mIHBhY2thZ2VzKSB7XG4gICAgY29uc3QgY29udHJhY3QgPSBjb250cmFjdEZvclBhY2thZ2UoZ2FsYXh5LCBwYWNrYWdlUmVjb3JkLmlkKVxuICAgIHBhY2thZ2VSZWNvcmQuY3VzdG9keSA9ICdyb3V0ZUNhY2hlJ1xuICAgIHBhY2thZ2VSZWNvcmQucm91dGVDYWNoZUlkID0gY2FjaGUuaWRcbiAgICBpZiAoIWNhY2hlLnBhY2thZ2VzLmluY2x1ZGVzKHBhY2thZ2VSZWNvcmQuaWQpKSBjYWNoZS5wYWNrYWdlcy5wdXNoKHBhY2thZ2VSZWNvcmQuaWQpXG4gICAgaWYgKGNvbnRyYWN0Py5zdGF0dXMgPT09ICdhY2NlcHRlZCcpIHtcbiAgICAgIGNvbnRyYWN0LnN0YXR1cyA9ICdmYWlsZWQnXG4gICAgICBjb250cmFjdC5yZXNvbHZlZEF0U2VjdG9yRGF5ID0gZ2FsYXh5LnNlY3RvckRheVxuICAgICAgY29udHJhY3QucmVzb2x2ZWRBdFJvdXRlUmVja29uaW5nID0gZ2FsYXh5LnJvdXRlUmVja29uaW5nXG4gICAgfVxuICAgIG1hbmlmZXN0RW50cnkoZ2FsYXh5LCAncGFja2FnZUxvc3QnLCBgJHtzZWFsZWRQYWNrYWdlRGVmaW5pdGlvbihwYWNrYWdlUmVjb3JkLmRlZmluaXRpb25JZCk/LnRpdGxlID8/IHBhY2thZ2VSZWNvcmQuaWR9IGxvc3Qgd2l0aCAke2NvdXJpZXJJZH07IGNhY2hlICR7Y2FjaGUuaWR9IG1hcmtzIHRoZSBsYXN0IGN1c3RvZHkgcmVjb3JkLmAsIHsgY29udHJhY3RJZDogY29udHJhY3Q/LmlkLCBwYWNrYWdlSWQ6IHBhY2thZ2VSZWNvcmQuaWQsIGNvdXJpZXJJZCwgcm91dGVDYWNoZUlkOiBjYWNoZS5pZCB9KVxuICAgIG1hbmlmZXN0RW50cnkoZ2FsYXh5LCAnZGVsaXZlcnlGYWlsZWQnLCBgQ291cmllciBsb3NzIGNsb3NlZCAke3NlYWxlZFBhY2thZ2VEZWZpbml0aW9uKHBhY2thZ2VSZWNvcmQuZGVmaW5pdGlvbklkKT8udGl0bGUgPz8gcGFja2FnZVJlY29yZC5pZH0uYCwgeyBjb250cmFjdElkOiBjb250cmFjdD8uaWQsIHBhY2thZ2VJZDogcGFja2FnZVJlY29yZC5pZCwgY291cmllcklkLCByb3V0ZUNhY2hlSWQ6IGNhY2hlLmlkIH0pXG4gIH1cbiAgcmV0dXJuIGdhbGF4eVxufVxuXG5leHBvcnQgY29uc3QgcmVjb3ZlclNlYWxlZFBhY2thZ2VSb3V0ZUNhY2hlcyA9IChzb3VyY2U6IEdhbGF4eVN0YXRlLCBsaW5rSWQ6IHN0cmluZyk6IFBhY2thZ2VNdXRhdGlvbiA9PiB7XG4gIGNvbnN0IGdhbGF4eSA9IGNvcHlHYWxheHkoc291cmNlKVxuICBjb25zdCBjYWNoZXMgPSBnYWxheHkucm91dGVDYWNoZXMuZmlsdGVyKGNhY2hlID0+IGNhY2hlLmxpbmtJZCA9PT0gbGlua0lkICYmICFjYWNoZS5yZWNvdmVyZWQgJiYgY2FjaGUucGFja2FnZXMubGVuZ3RoKVxuICBpZiAoIWNhY2hlcy5sZW5ndGgpIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogZmFsc2UsIG1lc3NhZ2U6ICdObyBzZWFsZWQtcGFja2FnZSBjYWNoZSBpcyByZWNvcmRlZCBvbiB0aGlzIHJvdXRlLicgfVxuICBsZXQgcmVjb3ZlcmVkID0gMFxuICBmb3IgKGNvbnN0IGNhY2hlIG9mIGNhY2hlcykge1xuICAgIGZvciAoY29uc3QgcGFja2FnZUlkIG9mIGNhY2hlLnBhY2thZ2VzKSB7XG4gICAgICBjb25zdCBwYWNrYWdlUmVjb3JkID0gZ2FsYXh5LnNlYWxlZFBhY2thZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gcGFja2FnZUlkKVxuICAgICAgaWYgKCFwYWNrYWdlUmVjb3JkIHx8IHBhY2thZ2VSZWNvcmQuY3VzdG9keSAhPT0gJ3JvdXRlQ2FjaGUnKSBjb250aW51ZVxuICAgICAgY29uc3QgY29udHJhY3QgPSBjb250cmFjdEZvclBhY2thZ2UoZ2FsYXh5LCBwYWNrYWdlUmVjb3JkLmlkKVxuICAgICAgcGFja2FnZVJlY29yZC5jdXN0b2R5ID0gJ2F0Sm9tb24nXG4gICAgICBwYWNrYWdlUmVjb3JkLnJvdXRlQ2FjaGVJZCA9IHVuZGVmaW5lZFxuICAgICAgbWFuaWZlc3RFbnRyeShnYWxheHksICdjdXN0b2R5VHJhbnNmZXJyZWQnLCBgJHtzZWFsZWRQYWNrYWdlRGVmaW5pdGlvbihwYWNrYWdlUmVjb3JkLmRlZmluaXRpb25JZCk/LnRpdGxlID8/IHBhY2thZ2VSZWNvcmQuaWR9IHJlY292ZXJlZCBpbnRvIEpvbW9uIGN1c3RvZHkuYCwgeyBjb250cmFjdElkOiBjb250cmFjdD8uaWQsIHBhY2thZ2VJZDogcGFja2FnZVJlY29yZC5pZCwgcm91dGVDYWNoZUlkOiBjYWNoZS5pZCB9KVxuICAgICAgbWFuaWZlc3RFbnRyeShnYWxheHksICdwYWNrYWdlUmVjb3ZlcmVkJywgYCR7c2VhbGVkUGFja2FnZURlZmluaXRpb24ocGFja2FnZVJlY29yZC5kZWZpbml0aW9uSWQpPy50aXRsZSA/PyBwYWNrYWdlUmVjb3JkLmlkfSByZWNvdmVyZWQgZnJvbSAke2NhY2hlLmlkfTsgdGhlIGZhaWxlZCBjb250cmFjdCByZW1haW5zIGNsb3NlZC5gLCB7IGNvbnRyYWN0SWQ6IGNvbnRyYWN0Py5pZCwgcGFja2FnZUlkOiBwYWNrYWdlUmVjb3JkLmlkLCByb3V0ZUNhY2hlSWQ6IGNhY2hlLmlkIH0pXG4gICAgICByZWNvdmVyZWQrK1xuICAgIH1cbiAgICBpZiAoIWNhY2hlLmNhcmdvLmxlbmd0aCkgY2FjaGUucmVjb3ZlcmVkID0gdHJ1ZVxuICB9XG4gIHJldHVybiB7IGdhbGF4eSwgY2hhbmdlZDogcmVjb3ZlcmVkID4gMCwgbWVzc2FnZTogcmVjb3ZlcmVkID8gYFJlY292ZXJlZCAke3JlY292ZXJlZH0gc2VhbGVkIHBhY2thZ2Uke3JlY292ZXJlZCA9PT0gMSA/ICcnIDogJ3MnfSB0byBKb21vbiBjdXN0b2R5LiBGYWlsZWQgY29udHJhY3RzIHJlbWFpbiBjbG9zZWQuYCA6ICdObyBzZWFsZWQgcGFja2FnZSBjb3VsZCBiZSByZWNvdmVyZWQgZnJvbSB0aGlzIGNhY2hlLicgfVxufVxuXG5leHBvcnQg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