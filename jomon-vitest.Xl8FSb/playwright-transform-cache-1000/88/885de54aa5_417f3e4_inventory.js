// cb2465d13cf49655657af4aa83dc17a7fd3e9b06
import { ITEM, biomeName } from '../content';
import { DIRECTIONS, floorPoint } from '../types';
import { actorAt, generateAreaFloor, getTile, isPassable } from '../world';
import { applyAreaArcState, areaArcStateFor, recordAreaArcPhase } from '../escalation';
import { advance, explode, resolveDefeatedActors } from './combat';
import { resolveLineEffect } from './line-effect';
import { addCondition, modifyIncomingDamage } from './conditions';
import { hasCondition } from './conditions';
import { gateForRun } from './gates';
import { gainXp } from './progression';
import { recordRescue } from './rescue';
import { tend } from './alignment';
import { completeObjective } from '../objectives';
import { consume, distance, event, log, turnRng } from './shared';
import { refreshFov } from './visibility';
import { synchronizePartyActors } from './party';
import { progressCompanionRecovery } from './companions';
import { evaluateEquipmentEffects } from './equipment';
import { vitalityRecovery, vitalityRescueRecovery } from './vitality';
import { scriptCastProfile } from './scripts';
import { castEmber } from './ember';
import { castVerdant, isVerdantSpell } from './verdant';
import { castAstral, isAstralSpell } from './astral';
import { announceSynergies, resolveSynergies } from './synergies';
import { contextualReward, merchantStock } from './rewards';
import { grantGold, purchaseBlocker, restoreBombs, restoreRopes, spendGold } from './economy';
import { anchorBoatWithRope, applyPropEffects, operateProp, releaseCartWithRope, secureCollapsedArchWithRope } from './props';
import { trailcraftTags } from './trailcraft';
import { acquireOptionalTraversalTool, boonRank, openMilestone, toolFor } from './buildcraft';
import { recordGeneratedOptionalContent, recordOptionalContent, recordSecretValue, recordTelemetryCount } from '../telemetry';
import { consumeRelicSpell } from './relics';
import { armRelicMove, armRelicWaterCrossing } from './relics';
import { openEncounter } from './encounters';
import { revealSecretClues } from './secret-discovery';
import { claimSecretReward, secretResolutionMessage } from '../secrets';
import { takeSecretShortcut } from './shortcuts';
const resolveSecretPickup = (state, item) => {
  if (!item.secretId) return;
  const claim = claimSecretReward(state, item.secretId);
  if (!claim || claim === 'already-resolved') return;
  recordSecretValue(state, claim.resolution.rewardValue);
  recordOptionalContent(state, 'used', `secret-resolution:${claim.room.id}:${claim.resolution.rewardKind}`);
  recordTelemetryCount(state, 'eventOutcomes', `secret-reward:${claim.resolution.rewardKind}`);
  recordTelemetryCount(state, 'eventOutcomes', `secret-risk:${claim.resolution.riskKind}`);
  log(state, secretResolutionMessage(claim.room));
};
export function pickUp(state) {
  var _state$floor$secretRo;
  const item = state.floor.items.find(current => current.x === state.hero.x && current.y === state.hero.y);
  if (!item) {
    log(state, 'Nothing here to take.');
    return [];
  }
  if (item.secretId && (_state$floor$secretRo = state.floor.secretRooms) !== null && _state$floor$secretRo !== void 0 && (_state$floor$secretRo = _state$floor$secretRo.find(room => room.id === item.secretId)) !== null && _state$floor$secretRo !== void 0 && _state$floor$secretRo.resolution) {
    state.floor.items = state.floor.items.filter(current => current !== item);
    log(state, 'This secret cache has already been claimed.');
    return [];
  }
  if (item.tool) {
    const acquired = acquireOptionalTraversalTool(state, item.tool);
    state.floor.items = state.floor.items.filter(current => current !== item);
    resolveSecretPickup(state, item);
    log(state, acquired.result === 'bound' ? `You bind the ${toolFor(item.tool).name}.` : acquired.result === 'duplicate' ? `You already carry the ${toolFor(item.tool).name}; leave its duplicate behind.` : `You replace ${toolFor(acquired.replaced).name} with the ${toolFor(item.tool).name}.`);
    return advance(state, [event('pickup')]);
  }
  if (item.id === 'gold') {
    const gained = grantGold(state, item.count);
    state.floor.items = state.floor.items.filter(current => current !== item);
    resolveSecretPickup(state, item);
    log(state, `You recover ${gained} cash.`);
    return advance(state, [event('pickup')]);
  }
  if (item.id === 'key') {
    state.hero.keys += item.count;
    state.floor.items = state.floor.items.filter(current => current !== item);
    resolveSecretPickup(state, item);
    log(state, 'You take a carved key.');
    return advance(state, [event('pickup')]);
  }
  if (state.hero.inventory.length >= 12) {
    log(state, 'Your pack is full.');
    return [];
  }
  state.hero.inventory.push(item.id);
  state.floor.items = state.floor.items.filter(current => current !== item);
  resolveSecretPickup(state, item);
  log(state, `You take ${ITEM[item.id].name}.`);
  return advance(state, [event('pickup')]);
}
export function operate(state) {
  var _state$floor$airlocks, _getTile;
  const milestone = openMilestone(state);
  if (milestone) return milestone;
  const encounter = openEncounter(state);
  if (encounter) return encounter;
  const tile = getTile(state.floor, state.hero.x, state.hero.y);
  const routeCache = state.floor.routeCache;
  if (routeCache && Math.max(Math.abs(routeCache.x - state.hero.x), Math.abs(routeCache.y - state.hero.y)) <= 1) {
    log(state, 'A marked Voyager cargo cache is ready for recovery.');
    return [event('routeCache', routeCache.linkId)];
  }
  const airlock = (_state$floor$airlocks = state.floor.airlocks) === null || _state$floor$airlocks === void 0 ? void 0 : _state$floor$airlocks.find(candidate => Math.max(Math.abs(candidate.x - state.hero.x), Math.abs(candidate.y - state.hero.y)) <= 1);
  if (airlock) {
    var _airlock$destinationS;
    log(state, `${airlock.label} ready.`);
    return [event('routeDeparture', (_airlock$destinationS = airlock.destinationSiteId) !== null && _airlock$destinationS !== void 0 ? _airlock$destinationS : 'voyager')];
  }
  const friend = state.floor.actors.find(actor => !actor.hostile && distance(actor, state.hero) <= 1);
  const altar = (tile === null || tile === void 0 ? void 0 : tile.kind) === 'altar' ? tile : friend && ((_getTile = getTile(state.floor, friend.x, friend.y)) === null || _getTile === void 0 ? void 0 : _getTile.kind) === 'altar' ? getTile(state.floor, friend.x, friend.y) : undefined;
  const container = nearbyContainer(state);
  if (container) {
    container.tile.kind = 'floor';
    const loot = contextualReward(state, 'container');
    grantGold(state, (container.kind === 'chest' ? 60 : 18) + boonRank(state, 'barterThread') * 10);
    if (state.hero.inventory.length < 12) state.hero.inventory.push(loot);else state.floor.items.push({
      id: loot,
      x: container.x,
      y: container.y,
      count: 1
    });
    log(state, `You open the ${container.kind} and find ${ITEM[loot].name}.`);
    if (completeObjective(state, 'recoverSupplies')) log(state, 'Objective complete: trail cache secured.');
    return advance(state, [event('pickup')]);
  }
  if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'rescue' || (friend === null || friend === void 0 ? void 0 : friend.name) === 'stranded traveler' || (friend === null || friend === void 0 ? void 0 : friend.name) === 'lost scout') {
    var _state$rescuedNpcs$so, _state$rescuedNpcs;
    const knownRescue = (_state$rescuedNpcs$so = (_state$rescuedNpcs = state.rescuedNpcs) === null || _state$rescuedNpcs === void 0 ? void 0 : _state$rescuedNpcs.some(npc => {
      var _state$area, _friend$id;
      return npc.id === `rescue:${(_state$area = state.area) !== null && _state$area !== void 0 ? _state$area : state.floor.biome}:${state.floor.index}:${(_friend$id = friend === null || friend === void 0 ? void 0 : friend.id) !== null && _friend$id !== void 0 ? _friend$id : `${state.hero.x},${state.hero.y}`}`;
    })) !== null && _state$rescuedNpcs$so !== void 0 ? _state$rescuedNpcs$so : false;
    const npc = recordRescue(state, friend);
    state.hero.maxHealth += 2;
    state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 8 + vitalityRescueRecovery(state.hero));
    grantGold(state, 35);
    state.floor.actors = state.floor.actors.filter(actor => actor !== friend);
    const eventTile = friend ? getTile(state.floor, friend.x, friend.y) : tile;
    if ((eventTile === null || eventTile === void 0 ? void 0 : eventTile.kind) === 'rescue' || (eventTile === null || eventTile === void 0 ? void 0 : eventTile.kind) === 'altar') eventTile.kind = 'floor';
    log(state, `${npc.name} reaches the village outpost.`);
    if (completeObjective(state, 'rescueScout')) log(state, 'Objective complete: traveler aided.');
    if (!knownRescue) tend(state, 'villagePact');
    return advance(state, [event('rescue')]);
  }
  if ((altar === null || altar === void 0 ? void 0 : altar.kind) === 'altar') {
    if (state.hero.gold < 75) {
      log(state, 'The shrine asks for 75 cash.');
      return [];
    }
    spendGold(state, 75);
    const reward = contextualReward(state, 'altar');
    if (state.hero.inventory.length < 12) state.hero.inventory.push(reward);else state.floor.items.push({
      id: reward,
      x: state.hero.x,
      y: state.hero.y,
      count: 1
    });
    gainXp(state, 35);
    log(state, `The shrine grants insight and ${ITEM[reward].name}.`);
    if (completeObjective(state, 'invokeAltar')) log(state, 'Objective complete: shrine offering made.');
    return advance(state, [event('spell')]);
  }
  if (nearbyGate(state)) {
    const lock = nearbyLockedDoor(state);
    if (lock && state.hero.keys > 0) {
      state.hero.keys--;
      lock.kind = 'floor';
      log(state, 'You unlock the sealed door.');
      return advance(state, [event('gateResolved')]);
    }
    const gate = gateForRun(state);
    if (!gate) {
      log(state, 'This is the final route. Cross the area to complete the delivery.');
      return [];
    }
    state.modal = {
      kind: 'gate',
      gateId: gate.id
    };
    log(state, gate.npcOffering);
    return [event('menu')];
  }
  if ((friend === null || friend === void 0 ? void 0 : friend.role) === 'merchant') {
    state.modal = {
      kind: 'shop',
      merchantId: friend.id
    };
    return [event('menu')];
  }
  const shortcut = takeSecretShortcut(state);
  if (shortcut) return shortcut;
  const propOperation = operateProp(state);
  if (propOperation) {
    revealSecretClues(state, 'prop');
    return propOperation.events.length ? advance(state, propOperation.events) : [];
  }
  if (revealSecretClues(state, 'prop').length) return [event('menu')];
  log(state, 'Nothing answers.');
  return [];
}
export function descend(state) {
  var _state$areaFloor, _state$area2, _state$areaArc, _state$companions, _state$areaOrder, _state$hero$oaths;
  const tile = getTile(state.floor, state.hero.x, state.hero.y);
  if ((tile === null || tile === void 0 ? void 0 : tile.kind) !== 'exit') {
    log(state, 'You are not at the exit.');
    return [];
  }
  if (state.floor.objective.status !== 'complete') {
    log(state, `Objective incomplete: ${state.floor.objective.label}.`);
    return [];
  }
  if (!state.floor.guardianDefeated) {
    log(state, 'A guardian still seals the route.');
    return [];
  }
  if (state.travel) {
    if (state.travel.residentStart + Math.min(3, state.travel.chunkCount - state.travel.residentStart) < state.travel.chunkCount) {
      log(state, 'This is a service hatch, not the far airlock. Continue through the route.');
      return [];
    }
    state.modal = undefined;
    log(state, 'Far airlock reached. Preparing the landing file.');
    return [event('connectorComplete')];
  }
  const areaFloor = (_state$areaFloor = state.areaFloor) !== null && _state$areaFloor !== void 0 ? _state$areaFloor : state.floor.index % 4;
  const biome = (_state$area2 = state.area) !== null && _state$area2 !== void 0 ? _state$area2 : state.floor.biome;
  const areaArc = ((_state$areaArc = state.areaArc) === null || _state$areaArc === void 0 ? void 0 : _state$areaArc.biome) === biome ? state.areaArc : areaArcStateFor(state.seed, biome);
  if (state.floor.escalation) recordAreaArcPhase(areaArc, state.floor.escalation.phase);
  state.areaArc = areaArc;
  state.companions = progressCompanionRecovery((_state$companions = state.companions) !== null && _state$companions !== void 0 ? _state$companions : [], state.rescuedNpcs);
  for (const companion of state.companions.filter(companion => companion.injury === 'recovering' && companion.recoveryFloors === 0)) log(state, `${companion.name}'s medbay recovery is ready to conclude.`);
  if (areaFloor === 3) {
    var _state$area3;
    state.modal = undefined;
    log(state, `${biomeName[(_state$area3 = state.area) !== null && _state$area3 !== void 0 ? _state$area3 : state.floor.biome]} survey complete. Return to the Jomon Voyager.`);
    return [event('areaComplete')];
  }
  const nextAreaFloor = areaFloor + 1;
  const routePosition = Math.max(0, ((_state$areaOrder = state.areaOrder) !== null && _state$areaOrder !== void 0 ? _state$areaOrder : []).indexOf(biome));
  state.floor = generateAreaFloor(state.seed, biome, nextAreaFloor, routePosition, state.campaignCycle);
  applyAreaArcState(state.floor, areaArc);
  recordGeneratedOptionalContent(state);
  state.areaFloor = nextAreaFloor;
  state.hero.x = state.floor.start.x;
  state.hero.y = state.floor.start.y;
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 4 + vitalityRecovery(state.hero));
  state.hero.focus = state.hero.maxFocus;
  state.hero.oaths = ((_state$hero$oaths = state.hero.oaths) !== null && _state$hero$oaths !== void 0 ? _state$hero$oaths : []).flatMap(oath => oath.remainingFloors <= 1 ? [] : [{
    ...oath,
    remainingFloors: oath.remainingFloors - 1
  }]);
  synchronizePartyActors(state, 'floorTransition');
  state.modal = {
    kind: 'trailcraft'
  };
  log(state, `You continue through ${biomeName[state.floor.biome]}.`);
  log(state, 'Landing zone cleared: choose a field upgrade.');
  refreshFov(state);
  return [event('floor')];
}
export function inventoryChoice(state, modal, command) {
  const index = Number(command) - 1;
  if (!Number.isInteger(index) || index < 0 || index >= state.hero.inventory.length) return [];
  const id = state.hero.inventory[index];
  state.modal = undefined;
  if (modal.mode === 'use') return useItem(state, id, index);
  if (modal.mode === 'drop') {
    state.hero.inventory.splice(index, 1);
    state.floor.items.push({
      id,
      x: state.hero.x,
      y: state.hero.y,
      count: 1,
      visibleInFog: true
    });
    log(state, `You drop ${ITEM[id].name}.`);
    return advance(state, [event('pickup')]);
  }
  if (modal.mode === 'throw') {
    state.modal = {
      kind: 'target',
      action: 'throw',
      item: id
    };
    return [event('menu')];
  }
  return equip(state, id, index);
}
export function useRope(state) {
  var _state$floor$climbLin;
  const climb = (_state$floor$climbLin = state.floor.climbLinks) === null || _state$floor$climbLin === void 0 ? void 0 : _state$floor$climbLin.find(link => link.lower.x === state.hero.x && link.lower.y === state.hero.y || link.upper.x === state.hero.x && link.upper.y === state.hero.y);
  if (climb !== null && climb !== void 0 && climb.anchored) {
    const destination = climb.lower.x === state.hero.x && climb.lower.y === state.hero.y ? climb.upper : climb.lower;
    state.hero.x = destination.x;
    state.hero.y = destination.y;
    state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + boonRank(state, 'galeThread'));
    for (let radius = 0; radius < boonRank(state, 'highPath'); radius++) for (const tile of state.floor.tiles) if (!tile.explored) {
      tile.explored = true;
      break;
    }
    refreshFov(state);
    log(state, 'You climb the secured vertical rope.');
    return advance(state, [event('traverse'), event('rope')]);
  }
  if (climb) {
    const free = boonRank(state, 'ropewright') >= 2 && state.turn % 2 === 0;
    if (!free && state.hero.ropes < 1) {
      log(state, 'No ropes remain.');
      return [];
    }
    if (!free) state.hero.ropes--;
    climb.anchored = true;
    log(state, free ? 'Ropewright binds the vertical route without spending reserve rope.' : 'You secure a vertical rope between the cliff tiers.');
    return advance(state, [event('rope')]);
  }
  if (state.hero.ropes < 1) {
    log(state, 'No ropes remain.');
    return [];
  }
  const tile = getTile(state.floor, state.hero.x, state.hero.y);
  if (tile.kind === 'pit') tile.kind = 'rope';else {
    const below = getTile(state.floor, state.hero.x, state.hero.y + 1);
    if ((below === null || below === void 0 ? void 0 : below.kind) === 'pit') below.kind = 'rope';else {
      const cartEvents = releaseCartWithRope(state);
      if (cartEvents !== undefined) {
        if (!cartEvents.length) return [];
        state.hero.ropes--;
        log(state, 'You rig the cart with a rope.');
        return advance(state, [event('rope'), ...cartEvents]);
      }
      const boatEvents = anchorBoatWithRope(state);
      if (boatEvents !== undefined) {
        if (!boatEvents.length) return [];
        state.hero.ropes--;
        log(state, 'You anchor the boat with a rope.');
        return advance(state, [event('rope'), ...boatEvents]);
      }
      const archEvents = secureCollapsedArchWithRope(state);
      if (archEvents !== undefined) {
        if (!archEvents.length) return [];
        state.hero.ropes--;
        log(state, 'You brace the collapsed arch with a rope.');
        return advance(state, [event('rope'), ...archEvents]);
      }
      log(state, 'There is nowhere to anchor a rope.');
      return [];
    }
  }
  state.hero.ropes--;
  log(state, 'You secure a rope.');
  return advance(state, [event('rope')]);
}
export function castFirstSpell(state) {
  var _state$hero$oaths2;
  if ((_state$hero$oaths2 = state.hero.oaths) !== null && _state$hero$oaths2 !== void 0 && _state$hero$oaths2.some(oath => oath.id === 'noCharms')) {
    log(state, 'Your active oath forbids charms.');
    return [];
  }
  const id = state.hero.inventory.find(item => ITEM[item].use === 'spell');
  if (!id) {
    log(state, 'You know no ready charm.');
    return [];
  }
  state.modal = {
    kind: 'target',
    action: 'spell',
    item: id
  };
  return [event('menu')];
}
export function quickCast(state, direction) {
  const id = state.hero.inventory.find(item => ITEM[item].use === 'spell');
  if (!id) {
    log(state, 'You know no ready charm.');
    return [];
  }
  return castSpell(state, id, direction);
}
export function bomb(state, direction) {
  var _state$hero$oaths3;
  if ((_state$hero$oaths3 = state.hero.oaths) !== null && _state$hero$oaths3 !== void 0 && _state$hero$oaths3.some(oath => oath.id === 'noBombs')) {
    log(state, 'Your active oath forbids bombs.');
    return [];
  }
  if (state.hero.bombs < 1) {
    log(state, 'No bombs remain.');
    return [];
  }
  state.hero.bombs--;
  const delta = DIRECTIONS[direction];
  log(state, 'You place a bomb.');
  const lastMatch = state.hero.health * 4 <= state.hero.maxHealth ? boonRank(state, 'lastMatch') * 2 : 0;
  explode(state, state.hero.x + delta.x * 2, state.hero.y + delta.y * 2, 12 + lastMatch, ['bomb'], 'your bomb', 1 + Math.min(1, boonRank(state, 'spareFuse')));
  return advance(state, [event('boom')]);
}
const drillableTerrain = new Set(['wall', 'rubble', 'bramble', 'boulder', 'breakwall']);
const glidableTerrain = new Set(['pit', 'water', 'deepWater', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'smoke', 'crumble', 'boulder', 'bramble', 'rubble', 'current']);
const grappleTerrain = new Set(['pit', 'water', 'deepWater', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'smoke', 'crumble', 'current']);
const grappleBlockers = new Set(['wall', 'rubble', 'bramble', 'boulder', 'breakwall', 'crate', 'chest', 'lockedDoor']);
const bridgeTerrain = new Set(['pit', 'water', 'deepWater', 'current']);
const dashTerrain = new Set(['smoke', 'gas', 'fireVent', 'current']);
const winchTerrain = new Set(['boulder', 'rubble', 'crate']);
export function drill(state, id, direction) {
  const index = state.hero.inventory.indexOf(id);
  const delta = DIRECTIONS[direction];
  const tile = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y);
  if (index < 0 || !tile || !drillableTerrain.has(tile.kind)) {
    log(state, 'The auger needs blocked ground.');
    return [];
  }
  tile.kind = 'floor';
  consume(state, index);
  refreshFov(state);
  log(state, 'The auger opens a narrow passage.');
  return advance(state, [event('traverse'), event('pickup')]);
}
export function glide(state, id, direction) {
  const index = state.hero.inventory.indexOf(id);
  const delta = DIRECTIONS[direction];
  const middle = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y);
  const landing = {
    x: state.hero.x + delta.x * 2,
    y: state.hero.y + delta.y * 2
  };
  if (index < 0 || !middle || !glidableTerrain.has(middle.kind) || !isPassable(state.floor, landing.x, landing.y)) {
    log(state, 'The glider needs a clear landing beyond hazardous ground.');
    return [];
  }
  state.hero.x = landing.x;
  state.hero.y = landing.y;
  if (middle.kind === 'water' || middle.kind === 'current') armRelicWaterCrossing(state);
  armRelicMove(state);
  consume(state, index);
  refreshFov(state);
  log(state, 'You ride the reed glider across the hazard.');
  return advance(state, [event('traverse'), event('move')]);
}
export function grapple(state, id, direction) {
  const index = state.hero.inventory.indexOf(id);
  const delta = DIRECTIONS[direction];
  const route = [3, 2].map(range => {
    const crossing = Array.from({
      length: range - 1
    }, (_, offset) => getTile(state.floor, state.hero.x + delta.x * (offset + 1), state.hero.y + delta.y * (offset + 1)));
    const landing = {
      x: state.hero.x + delta.x * range,
      y: state.hero.y + delta.y * range
    };
    return {
      crossing,
      landing
    };
  }).find(candidate => candidate.crossing.every(tile => tile && !grappleBlockers.has(tile.kind)) && candidate.crossing.some(tile => tile && grappleTerrain.has(tile.kind)) && isPassable(state.floor, candidate.landing.x, candidate.landing.y));
  if (index < 0 || !route) {
    log(state, 'The grappling line needs a clear landing beyond a gap or hazard.');
    return [];
  }
  state.hero.x = route.landing.x;
  state.hero.y = route.landing.y;
  if (route.crossing.some(tile => (tile === null || tile === void 0 ? void 0 : tile.kind) === 'water' || (tile === null || tile === void 0 ? void 0 : tile.kind) === 'current')) armRelicWaterCrossing(state);
  armRelicMove(state);
  consume(state, index);
  refreshFov(state);
  log(state, 'The grappling line carries you over the break.');
  return advance(state, [event('traverse'), event('move')]);
}
export function bridge(state, id, direction) {
  const index = state.hero.inventory.indexOf(id);
  const delta = DIRECTIONS[direction];
  const tile = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y);
  if (index < 0 || !tile || !bridgeTerrain.has(tile.kind)) {
    log(state, 'The bridge needs an adjacent pit, water, current, or deep water.');
    return [];
  }
  tile.kind = 'rope';
  delete tile.flow;
  consume(state, index);
  refreshFov(state);
  log(state, 'The bridge locks into a permanent crossing.');
  return advance(state, [event('traverse'), event('rope')]);
}
export function dash(state, id, direction) {
  const index = state.hero.inventory.indexOf(id);
  const delta = DIRECTIONS[direction];
  const middle = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y);
  const landing = {
    x: state.hero.x + delta.x * 2,
    y: state.hero.y + delta.y * 2
  };
  if (index < 0 || !middle || !dashTerrain.has(middle.kind) || !isPassable(state.floor, landing.x, landing.y)) {
    log(state, 'The steam jetpack needs smoke, gas, fire, or current before a clear landing.');
    return [];
  }
  state.hero.x = landing.x;
  state.hero.y = landing.y;
  if (middle.kind === 'current') armRelicWaterCrossing(state);
  armRelicMove(state);
  consume(state, index);
  refreshFov(state);
  log(state, 'Steam carries you through the hazard.');
  return advance(state, [event('traverse'), event('move')]);
}
export function winch(state, id, direction) {
  const index = state.hero.inventory.indexOf(id);
  const delta = DIRECTIONS[direction];
  const target = {
    x: state.hero.x + delta.x,
    y: state.hero.y + delta.y
  };
  const tile = getTile(state.floor, target.x, target.y);
  if (index < 0 || !tile || !winchTerrain.has(tile.kind)) {
    log(state, 'The winch needs an adjacent boulder, rubble, or crate.');
    return [];
  }
  tile.kind = 'floor';
  state.hero.x = target.x;
  state.hero.y = target.y;
  armRelicMove(state);
  consume(state, index);
  refreshFov(state);
  log(state, 'The winch clears the obstruction and reels you forward.');
  return advance(state, [event('traverse'), event('move')]);
}
export function throwItem(state, id, direction) {
  var _cells$at;
  const index = state.hero.inventory.indexOf(id);
  if (index === -1) return [];
  state.hero.inventory.splice(index, 1);
  const delta = DIRECTIONS[direction];
  const destination = {
    x: state.hero.x + delta.x * 5,
    y: state.hero.y + delta.y * 5
  };
  const cells = resolveLineEffect(state.floor, state.hero, destination).cells;
  const point = (_cells$at = cells.at(-1)) !== null && _cells$at !== void 0 ? _cells$at : {
    x: state.hero.x,
    y: state.hero.y
  };
  const target = actorAt(state.floor, point.x, point.y);
  if (target !== null && target !== void 0 && target.hostile) {
    target.health -= modifyIncomingDamage(target, 3 + state.hero.stats.strength + boonRank(state, 'emberFletching') + boonRank(state, 'thunderVessel') + boonRank(state, 'tetheredThunder'));
    if (boonRank(state, 'thunderVessel')) addCondition(target, {
      kind: 'marked',
      duration: 2,
      potency: boonRank(state, 'thunderVessel')
    });
    log(state, `${ITEM[id].name} hits ${target.name}.`);
  }
  if (id === 'fireJar') explode(state, point.x, point.y, 5, ['bomb', 'fire']);else {
    state.floor.items.push({
      id,
      x: point.x,
      y: point.y,
      count: 1,
      visibleInFog: true
    });
    applyPropEffects(state, [point], ['throw']);
  }
  resolveDefeatedActors(state);
  return advance(state, [event(id === 'fireJar' ? 'boom' : 'hit')]);
}
export function castSpell(state, id, direction) {
  var _state$hero$oaths4, _state$hero$relicChar, _getTile2, _geometry$values$rang, _geometry$values$rang2, _impact$values$damage, _effect$values$focus, _trailcraft$values$fo;
  if ((_state$hero$oaths4 = state.hero.oaths) !== null && _state$hero$oaths4 !== void 0 && _state$hero$oaths4.some(oath => oath.id === 'noCharms')) {
    log(state, 'Your active oath forbids charms.');
    return [];
  }
  const item = ITEM[id];
  const profile = scriptCastProfile(state.hero, id);
  const circuitReady = Boolean((_state$hero$relicChar = state.hero.relicCharges) === null || _state$hero$relicChar === void 0 ? void 0 : _state$hero$relicChar.ashCircuit);
  const currentTerrain = (_getTile2 = getTile(state.floor, state.hero.x, state.hero.y)) === null || _getTile2 === void 0 ? void 0 : _getTile2.kind;
  const focusCost = Math.max(1, profile.focusCost - Number(circuitReady) - boonRank(state, 'frozenFocus') * Number(hasCondition(state.hero, 'shielded')) - boonRank(state, 'sunsetCircuit') * Number(currentTerrain === 'saltMirror'));
  if (state.hero.focus < focusCost) {
    log(state, 'You lack focus.');
    return [];
  }
  const circuit = circuitReady && consumeRelicSpell(state);
  state.hero.focus -= focusCost;
  const geometry = resolveSynergies({
    scripts: [id],
    skills: state.hero.skills
  }, {
    range: profile.range
  });
  const delta = DIRECTIONS[direction];
  const point = {
    x: state.hero.x + delta.x * Math.max(1, Math.floor((_geometry$values$rang = geometry.values.range) !== null && _geometry$values$rang !== void 0 ? _geometry$values$rang : profile.range)),
    y: state.hero.y + delta.y * Math.max(1, Math.floor((_geometry$values$rang2 = geometry.values.range) !== null && _geometry$values$rang2 !== void 0 ? _geometry$values$rang2 : profile.range))
  };
  const tile = getTile(state.floor, point.x, point.y);
  const impact = resolveSynergies({
    scripts: [id],
    terrain: tile ? [tile.kind] : []
  });
  if (item.spell === 'ember') castEmber(state, point, (_impact$values$damage = impact.values.damage) !== null && _impact$values$damage !== void 0 ? _impact$values$damage : 0);
  if (isVerdantSpell(item.spell)) castVerdant(state, item.spell, point);
  if (isAstralSpell(item.spell)) castAstral(state, item.spell, point);
  announceSynergies(state, geometry);
  announceSynergies(state, impact);
  const effect = evaluateEquipmentEffects(state.hero, 'triggered', {
    trigger: 'spell',
    scripts: [id]
  });
  const tags = trailcraftTags(state.hero).filter(id => id === 'barkBinding' || id === 'spiritThread');
  const trailcraft = resolveSynergies({
    tags
  });
  announceSynergies(state, trailcraft);
  state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + ((_effect$values$focus = effect.values.focus) !== null && _effect$values$focus !== void 0 ? _effect$values$focus : 0) + ((_trailcraft$values$fo = trailcraft.values.focus) !== null && _trailcraft$values$fo !== void 0 ? _trailcraft$values$fo : 0));
  if (circuit) {
    state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 2);
    log(state, 'Ash Circuit completes the traversal-to-Charm relay.');
  }
  resolveDefeatedActors(state);
  refreshFov(state);
  log(state, `${item.name} takes effect.`);
  return advance(state, [event('spell')]);
}
export function shopChoice(state, command) {
  const id = merchantStock(state)[Number(command) - 1];
  if (!id) return [];
  const item = ITEM[id];
  if (state.hero.gold < item.value) {
    log(state, 'Not enough cash.');
    return [event('menu')];
  }
  const blocker = purchaseBlocker(state.hero, id);
  if (blocker) {
    log(state, blocker);
    return [event('menu')];
  }
  if (state.hero.inventory.length >= 12) {
    log(state, 'Your pack is full.');
    return [event('menu')];
  }
  spendGold(state, item.value);
  recordTelemetryCount(state, 'purchases', id);
  state.hero.inventory.push(id);
  log(state, `You buy ${item.name}.`);
  return advance(state, [event('pickup')]);
}
const recoverUsedItem = (state, id) => {
  const chance = Math.min(90, boonRank(state, 'salvager') * 25 + boonRank(state, 'deepPockets') * 35);
  if (!chance || state.hero.inventory.length >= 12 || !turnRng(state, 'loot', `recover:${id}`).chance(chance)) return;
  state.hero.inventory.push(id);
  log(state, `${ITEM[id].name} is recovered by your build.`);
};
function useItem(state, id, inventoryIndex) {
  const item = ITEM[id];
  if (item.slot) return equip(state, id, inventoryIndex);
  if (item.use === 'heal') {
    var _state$hero$oaths5;
    if ((_state$hero$oaths5 = state.hero.oaths) !== null && _state$hero$oaths5 !== void 0 && _state$hero$oaths5.some(oath => oath.id === 'noHealing')) {
      log(state, 'Your active oath forbids healing.');
      return [];
    }
    ;
    state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + Math.max(1, 10 + vitalityRecovery(state.hero) - boonRank(state, 'quietPocket') - boonRank(state, 'hardLesson')) + boonRank(state, 'stormRations') * 2);
    if (boonRank(state, 'quietPocket')) state.hero.conditions = [];
    consume(state, inventoryIndex);
    recoverUsedItem(state, id);
    log(state, 'Warmth returns to your limbs.');
    return advance(state, [event('spell')]);
  }
  if (item.use === 'focus') {
    state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 8);
    consume(state, inventoryIndex);
    recoverUsedItem(state, id);
    log(state, 'Your mind sharpens.');
    return advance(state, [event('spell')]);
  }
  if (item.use === 'map') {
    for (const tile of state.floor.tiles) tile.explored = true;
    consume(state, inventoryIndex);
    log(state, 'The floor map unfolds in your mind.');
    return advance(state, [event('spell')]);
  }
  if (item.use === 'teleport') {
    const choices = state.floor.tiles.flatMap((tile, i) => tile.kind === 'floor' && tile.explored ? [floorPoint(state.floor, i)] : []);
    if (choices.length) {
      const target = turnRng(state, 'combat', 'blink').pick(choices);
      state.hero.x = target.x;
      state.hero.y = target.y;
    }
    consume(state, inventoryIndex);
    refreshFov(state);
    log(state, 'Space folds.');
    return advance(state, [event('spell')]);
  }
  if (item.use === 'drill') {
    state.modal = {
      kind: 'target',
      action: 'drill',
      item: id
    };
    return [event('menu')];
  }
  if (item.use === 'glide') {
    state.modal = {
      kind: 'target',
      action: 'glide',
      item: id
    };
    return [event('menu')];
  }
  if (item.use === 'grapple') {
    state.modal = {
      kind: 'target',
      action: 'grapple',
      item: id
    };
    return [event('menu')];
  }
  if (item.use === 'bridge') {
    state.modal = {
      kind: 'target',
      action: 'bridge',
      item: id
    };
    return [event('menu')];
  }
  if (item.use === 'dash') {
    state.modal = {
      kind: 'target',
      action: 'dash',
      item: id
    };
    return [event('menu')];
  }
  if (item.use === 'winch') {
    state.modal = {
      kind: 'target',
      action: 'winch',
      item: id
    };
    return [event('menu')];
  }
  if (item.use === 'bomb') {
    const restored = restoreBombs(state.hero, 3 + boonRank(state, 'spareFuse'));
    if (!restored) {
      log(state, 'Your bomb reserve is full.');
      return [];
    }
    ;
    consume(state, inventoryIndex);
    recoverUsedItem(state, id);
    log(state, `You gain ${restored} bombs.`);
    return advance(state, [event('pickup')]);
  }
  if (item.use === 'rope') {
    const restored = restoreRopes(state.hero, 3);
    if (!restored) {
      log(state, 'Your rope reserve is full.');
      return [];
    }
    ;
    consume(state, inventoryIndex);
    recoverUsedItem(state, id);
    log(state, `You gain ${restored} ropes.`);
    return advance(state, [event('pickup')]);
  }
  if (item.use === 'key') {
    state.hero.keys++;
    consume(state, inventoryIndex);
    return advance(state, [event('pickup')]);
  }
  if (item.use === 'spell') {
    state.modal = {
      kind: 'target',
      action: 'spell',
      item: id
    };
    return [event('menu')];
  }
  log(state, 'That cannot be used here.');
  return [];
}
function equip(state, id, index) {
  const item = ITEM[id];
  if (!item.slot) {
    log(state, 'That cannot be equipped.');
    return [];
  }
  const previous = state.hero.equipment[item.slot];
  state.hero.inventory.splice(index, 1);
  if (previous) {
    state.hero.inventory.push(previous);
    state.hero.lastUnequipped = previous;
  }
  state.hero.equipment[item.slot] = id;
  log(state, `You equip ${item.name}.`);
  return advance(state, [event('pickup')]);
}
export function swap(state) {
  const id = state.hero.lastUnequipped;
  if (!id || state.hero.inventory.length >= 12) {
    log(state, 'No item is ready to swap.');
    return [];
  }
  state.hero.inventory.push(id);
  state.hero.lastUnequipped = undefined;
  log(state, 'You stow your last unequipped item.');
  return advance(state, [event('pickup')]);
}
const nearbyContainer = state => {
  for (const delta of Object.values(DIRECTIONS)) {
    const x = state.hero.x + delta.x;
    const y = state.hero.y + delta.y;
    const tile = getTile(state.floor, x, y);
    if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'crate' || (tile === null || tile === void 0 ? void 0 : tile.kind) === 'chest') return {
      tile,
      kind: tile.kind,
      x,
      y
    };
  }
  return undefined;
};
const nearbyGate = state => Object.values(DIRECTIONS).some(delta => {
  var _getTile3;
  return ((_getTile3 = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)) === null || _getTile3 === void 0 ? void 0 : _getTile3.kind) === 'lockedDoor';
});
const nearbyLockedDoor = state => Object.values(DIRECTIONS).map(delta => getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)).find(tile => (tile === null || tile === void 0 ? void 0 : tile.kind) === 'lockedDoor');
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJJVEVNIiwiYmlvbWVOYW1lIiwiRElSRUNUSU9OUyIsImZsb29yUG9pbnQiLCJhY3RvckF0IiwiZ2VuZXJhdGVBcmVhRmxvb3IiLCJnZXRUaWxlIiwiaXNQYXNzYWJsZSIsImFwcGx5QXJlYUFyY1N0YXRlIiwiYXJlYUFyY1N0YXRlRm9yIiwicmVjb3JkQXJlYUFyY1BoYXNlIiwiYWR2YW5jZSIsImV4cGxvZGUiLCJyZXNvbHZlRGVmZWF0ZWRBY3RvcnMiLCJyZXNvbHZlTGluZUVmZmVjdCIsImFkZENvbmRpdGlvbiIsIm1vZGlmeUluY29taW5nRGFtYWdlIiwiaGFzQ29uZGl0aW9uIiwiZ2F0ZUZvclJ1biIsImdhaW5YcCIsInJlY29yZFJlc2N1ZSIsInRlbmQiLCJjb21wbGV0ZU9iamVjdGl2ZSIsImNvbnN1bWUiLCJkaXN0YW5jZSIsImV2ZW50IiwibG9nIiwidHVyblJuZyIsInJlZnJlc2hGb3YiLCJzeW5jaHJvbml6ZVBhcnR5QWN0b3JzIiwicHJvZ3Jlc3NDb21wYW5pb25SZWNvdmVyeSIsImV2YWx1YXRlRXF1aXBtZW50RWZmZWN0cyIsInZpdGFsaXR5UmVjb3ZlcnkiLCJ2aXRhbGl0eVJlc2N1ZVJlY292ZXJ5Iiwic2NyaXB0Q2FzdFByb2ZpbGUiLCJjYXN0RW1iZXIiLCJjYXN0VmVyZGFudCIsImlzVmVyZGFudFNwZWxsIiwiY2FzdEFzdHJhbCIsImlzQXN0cmFsU3BlbGwiLCJhbm5vdW5jZVN5bmVyZ2llcyIsInJlc29sdmVTeW5lcmdpZXMiLCJjb250ZXh0dWFsUmV3YXJkIiwibWVyY2hhbnRTdG9jayIsImdyYW50R29sZCIsInB1cmNoYXNlQmxvY2tlciIsInJlc3RvcmVCb21icyIsInJlc3RvcmVSb3BlcyIsInNwZW5kR29sZCIsImFuY2hvckJvYXRXaXRoUm9wZSIsImFwcGx5UHJvcEVmZmVjdHMiLCJvcGVyYXRlUHJvcCIsInJlbGVhc2VDYXJ0V2l0aFJvcGUiLCJzZWN1cmVDb2xsYXBzZWRBcmNoV2l0aFJvcGUiLCJ0cmFpbGNyYWZ0VGFncyIsImFjcXVpcmVPcHRpb25hbFRyYXZlcnNhbFRvb2wiLCJib29uUmFuayIsIm9wZW5NaWxlc3RvbmUiLCJ0b29sRm9yIiwicmVjb3JkR2VuZXJhdGVkT3B0aW9uYWxDb250ZW50IiwicmVjb3JkT3B0aW9uYWxDb250ZW50IiwicmVjb3JkU2VjcmV0VmFsdWUiLCJyZWNvcmRUZWxlbWV0cnlDb3VudCIsImNvbnN1bWVSZWxpY1NwZWxsIiwiYXJtUmVsaWNNb3ZlIiwiYXJtUmVsaWNXYXRlckNyb3NzaW5nIiwib3BlbkVuY291bnRlciIsInJldmVhbFNlY3JldENsdWVzIiwiY2xhaW1TZWNyZXRSZXdhcmQiLCJzZWNyZXRSZXNvbHV0aW9uTWVzc2FnZSIsInRha2VTZWNyZXRTaG9ydGN1dCIsInJlc29sdmVTZWNyZXRQaWNrdXAiLCJzdGF0ZSIsIml0ZW0iLCJzZWNyZXRJZCIsImNsYWltIiwicmVzb2x1dGlvbiIsInJld2FyZFZhbHVlIiwicm9vbSIsImlkIiwicmV3YXJkS2luZCIsInJpc2tLaW5kIiwicGlja1VwIiwiX3N0YXRlJGZsb29yJHNlY3JldFJvIiwiZmxvb3IiLCJpdGVtcyIsImZpbmQiLCJjdXJyZW50IiwieCIsImhlcm8iLCJ5Iiwic2VjcmV0Um9vbXMiLCJmaWx0ZXIiLCJ0b29sIiwiYWNxdWlyZWQiLCJyZXN1bHQiLCJuYW1lIiwicmVwbGFjZWQiLCJnYWluZWQiLCJjb3VudCIsImtleXMiLCJpbnZlbnRvcnkiLCJsZW5ndGgiLCJwdXNoIiwib3BlcmF0ZSIsIl9zdGF0ZSRmbG9vciRhaXJsb2NrcyIsIl9nZXRUaWxlIiwibWlsZXN0b25lIiwiZW5jb3VudGVyIiwidGlsZSIsInJvdXRlQ2FjaGUiLCJNYXRoIiwibWF4IiwiYWJzIiwibGlua0lkIiwiYWlybG9jayIsImFpcmxvY2tzIiwiY2FuZGlkYXRlIiwiX2FpcmxvY2skZGVzdGluYXRpb25TIiwibGFiZWwiLCJkZXN0aW5hdGlvblNpdGVJZCIsImZyaWVuZCIsImFjdG9ycyIsImFjdG9yIiwiaG9zdGlsZSIsImFsdGFyIiwia2luZCIsInVuZGVmaW5lZCIsImNvbnRhaW5lciIsIm5lYXJieUNvbnRhaW5lciIsImxvb3QiLCJfc3RhdGUkcmVzY3VlZE5wY3Mkc28iLCJfc3RhdGUkcmVzY3VlZE5wY3MiLCJrbm93blJlc2N1ZSIsInJlc2N1ZWROcGNzIiwic29tZSIsIm5wYyIsIl9zdGF0ZSRhcmVhIiwiX2ZyaWVuZCRpZCIsImFyZWEiLCJiaW9tZSIsImluZGV4IiwibWF4SGVhbHRoIiwiaGVhbHRoIiwibWluIiwiZXZlbnRUaWxlIiwiZ29sZCIsInJld2FyZCIsIm5lYXJieUdhdGUiLCJsb2NrIiwibmVhcmJ5TG9ja2VkRG9vciIsImdhdGUiLCJtb2RhbCIsImdhdGVJZCIsIm5wY09mZmVyaW5nIiwicm9sZSIsIm1lcmNoYW50SWQiLCJzaG9ydGN1dCIsInByb3BPcGVyYXRpb24iLCJldmVudHMiLCJkZXNjZW5kIiwiX3N0YXRlJGFyZWFGbG9vciIsIl9zdGF0ZSRhcmVhMiIsIl9zdGF0ZSRhcmVhQXJjIiwiX3N0YXRlJGNvbXBhbmlvbnMiLCJfc3RhdGUkYXJlYU9yZGVyIiwiX3N0YXRlJGhlcm8kb2F0aHMiLCJvYmplY3RpdmUiLCJzdGF0dXMiLCJndWFyZGlhbkRlZmVhdGVkIiwidHJhdmVsIiwicmVzaWRlbnRTdGFydCIsImNodW5rQ291bnQiLCJhcmVhRmxvb3IiLCJhcmVhQXJjIiwic2VlZCIsImVzY2FsYXRpb24iLCJwaGFzZSIsImNvbXBhbmlvbnMiLCJjb21wYW5pb24iLCJpbmp1cnkiLCJyZWNvdmVyeUZsb29ycyIsIl9zdGF0ZSRhcmVhMyIsIm5leHRBcmVhRmxvb3IiLCJyb3V0ZVBvc2l0aW9uIiwiYXJlYU9yZGVyIiwiaW5kZXhPZiIsImNhbXBhaWduQ3ljbGUiLCJzdGFydCIsImZvY3VzIiwibWF4Rm9jdXMiLCJvYXRocyIsImZsYXRNYXAiLCJvYXRoIiwicmVtYWluaW5nRmxvb3JzIiwiaW52ZW50b3J5Q2hvaWNlIiwiY29tbWFuZCIsIk51bWJlciIsImlzSW50ZWdlciIsIm1vZGUiLCJ1c2VJdGVtIiwic3BsaWNlIiwidmlzaWJsZUluRm9nIiwiYWN0aW9uIiwiZXF1aXAiLCJ1c2VSb3BlIiwiX3N0YXRlJGZsb29yJGNsaW1iTGluIiwiY2xpbWIiLCJjbGltYkxpbmtzIiwibGluayIsImxvd2VyIiwidXBwZXIiLCJhbmNob3JlZCIsImRlc3RpbmF0aW9uIiwicmFkaXVzIiwidGlsZXMiLCJleHBsb3JlZCIsImZyZWUiLCJ0dXJuIiwicm9wZXMiLCJiZWxvdyIsImNhcnRFdmVudHMiLCJib2F0RXZlbnRzIiwiYXJjaEV2ZW50cyIsImNhc3RGaXJzdFNwZWxsIiwiX3N0YXRlJGhlcm8kb2F0aHMyIiwidXNlIiwicXVpY2tDYXN0IiwiZGlyZWN0aW9uIiwiY2FzdFNwZWxsIiwiYm9tYiIsIl9zdGF0ZSRoZXJvJG9hdGhzMyIsImJvbWJzIiwiZGVsdGEiLCJsYXN0TWF0Y2giLCJkcmlsbGFibGVUZXJyYWluIiwiU2V0IiwiZ2xpZGFibGVUZXJyYWluIiwiZ3JhcHBsZVRlcnJhaW4iLCJncmFwcGxlQmxvY2tlcnMiLCJicmlkZ2VUZXJyYWluIiwiZGFzaFRlcnJhaW4iLCJ3aW5jaFRlcnJhaW4iLCJkcmlsbCIsImhhcyIsImdsaWRlIiwibWlkZGxlIiwibGFuZGluZyIsImdyYXBwbGUiLCJyb3V0ZSIsIm1hcCIsInJhbmdlIiwiY3Jvc3NpbmciLCJBcnJheSIsImZyb20iLCJfIiwib2Zmc2V0IiwiZXZlcnkiLCJicmlkZ2UiLCJmbG93IiwiZGFzaCIsIndpbmNoIiwidGFyZ2V0IiwidGhyb3dJdGVtIiwiX2NlbGxzJGF0IiwiY2VsbHMiLCJwb2ludCIsImF0Iiwic3RhdHMiLCJzdHJlbmd0aCIsImR1cmF0aW9uIiwicG90ZW5jeSIsIl9zdGF0ZSRoZXJvJG9hdGhzNCIsIl9zdGF0ZSRoZXJvJHJlbGljQ2hhciIsIl9nZXRUaWxlMiIsIl9nZW9tZXRyeSR2YWx1ZXMkcmFuZyIsIl9nZW9tZXRyeSR2YWx1ZXMkcmFuZzIiLCJfaW1wYWN0JHZhbHVlcyRkYW1hZ2UiLCJfZWZmZWN0JHZhbHVlcyRmb2N1cyIsIl90cmFpbGNyYWZ0JHZhbHVlcyRmbyIsInByb2ZpbGUiLCJjaXJjdWl0UmVhZHkiLCJCb29sZWFuIiwicmVsaWNDaGFyZ2VzIiwiYXNoQ2lyY3VpdCIsImN1cnJlbnRUZXJyYWluIiwiZm9jdXNDb3N0IiwiY2lyY3VpdCIsImdlb21ldHJ5Iiwic2NyaXB0cyIsInNraWxscyIsInZhbHVlcyIsImltcGFjdCIsInRlcnJhaW4iLCJzcGVsbCIsImRhbWFnZSIsImVmZmVjdCIsInRyaWdnZXIiLCJ0YWdzIiwidHJhaWxjcmFmdCIsInNob3BDaG9pY2UiLCJ2YWx1ZSIsImJsb2NrZXIiLCJyZWNvdmVyVXNlZEl0ZW0iLCJjaGFuY2UiLCJpbnZlbnRvcnlJbmRleCIsInNsb3QiLCJfc3RhdGUkaGVybyRvYXRoczUiLCJjb25kaXRpb25zIiwiY2hvaWNlcyIsImkiLCJwaWNrIiwicmVzdG9yZWQiLCJwcmV2aW91cyIsImVxdWlwbWVudCIsImxhc3RVbmVxdWlwcGVkIiwic3dhcCIsIk9iamVjdCIsIl9nZXRUaWxlMyJdLCJzb3VyY2VzIjpbImludmVudG9yeS50cyJdLCJzb3VyY2VzQ29udGVudCI6WyJpbXBvcnQgeyBJVEVNLCBiaW9tZU5hbWUgfSBmcm9tICcuLi9jb250ZW50J1xuaW1wb3J0IHsgRElSRUNUSU9OUywgZmxvb3JQb2ludCwgdHlwZSBEaXJlY3Rpb24sIHR5cGUgR3JvdW5kSXRlbSwgdHlwZSBNb2RhbCwgdHlwZSBSdW5TdGF0ZSB9IGZyb20gJy4uL3R5cGVzJ1xuaW1wb3J0IHsgYWN0b3JBdCwgZ2VuZXJhdGVBcmVhRmxvb3IsIGdldFRpbGUsIGlzUGFzc2FibGUgfSBmcm9tICcuLi93b3JsZCdcbmltcG9ydCB7IGFwcGx5QXJlYUFyY1N0YXRlLCBhcmVhQXJjU3RhdGVGb3IsIHJlY29yZEFyZWFBcmNQaGFzZSB9IGZyb20gJy4uL2VzY2FsYXRpb24nXG5pbXBvcnQgeyBhZHZhbmNlLCBleHBsb2RlLCByZXNvbHZlRGVmZWF0ZWRBY3RvcnMgfSBmcm9tICcuL2NvbWJhdCdcbmltcG9ydCB7IHJlc29sdmVMaW5lRWZmZWN0IH0gZnJvbSAnLi9saW5lLWVmZmVjdCdcbmltcG9ydCB7IGFkZENvbmRpdGlvbiwgbW9kaWZ5SW5jb21pbmdEYW1hZ2UgfSBmcm9tICcuL2NvbmRpdGlvbnMnXG5pbXBvcnQgeyBoYXNDb25kaXRpb24gfSBmcm9tICcuL2NvbmRpdGlvbnMnXG5pbXBvcnQgeyBnYXRlRm9yUnVuIH0gZnJvbSAnLi9nYXRlcydcbmltcG9ydCB7IGdhaW5YcCB9IGZyb20gJy4vcHJvZ3Jlc3Npb24nXG5pbXBvcnQgeyByZWNvcmRSZXNjdWUgfSBmcm9tICcuL3Jlc2N1ZSdcbmltcG9ydCB7IHRlbmQgfSBmcm9tICcuL2FsaWdubWVudCdcbmltcG9ydCB7IGNvbXBsZXRlT2JqZWN0aXZlIH0gZnJvbSAnLi4vb2JqZWN0aXZlcydcbmltcG9ydCB7IGNvbnN1bWUsIGRpc3RhbmNlLCBldmVudCwgbG9nLCB0dXJuUm5nLCB0eXBlIEFjdGlvblJlc3VsdCB9IGZyb20gJy4vc2hhcmVkJ1xuaW1wb3J0IHsgcmVmcmVzaEZvdiB9IGZyb20gJy4vdmlzaWJpbGl0eSdcbmltcG9ydCB7IHN5bmNocm9uaXplUGFydHlBY3RvcnMgfSBmcm9tICcuL3BhcnR5J1xuaW1wb3J0IHsgcHJvZ3Jlc3NDb21wYW5pb25SZWNvdmVyeSB9IGZyb20gJy4vY29tcGFuaW9ucydcbmltcG9ydCB7IGV2YWx1YXRlRXF1aXBtZW50RWZmZWN0cyB9IGZyb20gJy4vZXF1aXBtZW50J1xuaW1wb3J0IHsgdml0YWxpdHlSZWNvdmVyeSwgdml0YWxpdHlSZXNjdWVSZWNvdmVyeSB9IGZyb20gJy4vdml0YWxpdHknXG5pbXBvcnQgeyBzY3JpcHRDYXN0UHJvZmlsZSB9IGZyb20gJy4vc2NyaXB0cydcbmltcG9ydCB7IGNhc3RFbWJlciB9IGZyb20gJy4vZW1iZXInXG5pbXBvcnQgeyBjYXN0VmVyZGFudCwgaXNWZXJkYW50U3BlbGwgfSBmcm9tICcuL3ZlcmRhbnQnXG5pbXBvcnQgeyBjYXN0QXN0cmFsLCBpc0FzdHJhbFNwZWxsIH0gZnJvbSAnLi9hc3RyYWwnXG5pbXBvcnQgeyBhbm5vdW5jZVN5bmVyZ2llcywgcmVzb2x2ZVN5bmVyZ2llcyB9IGZyb20gJy4vc3luZXJnaWVzJ1xuaW1wb3J0IHsgY29udGV4dHVhbFJld2FyZCwgbWVyY2hhbnRTdG9jayB9IGZyb20gJy4vcmV3YXJkcydcbmltcG9ydCB7IGdyYW50R29sZCwgcHVyY2hhc2VCbG9ja2VyLCByZXN0b3JlQm9tYnMsIHJlc3RvcmVSb3Blcywgc3BlbmRHb2xkIH0gZnJvbSAnLi9lY29ub215J1xuaW1wb3J0IHsgYW5jaG9yQm9hdFdpdGhSb3BlLCBhcHBseVByb3BFZmZlY3RzLCBvcGVyYXRlUHJvcCwgcmVsZWFzZUNhcnRXaXRoUm9wZSwgc2VjdXJlQ29sbGFwc2VkQXJjaFdpdGhSb3BlIH0gZnJvbSAnLi9wcm9wcydcbmltcG9ydCB7IHRyYWlsY3JhZnRUYWdzIH0gZnJvbSAnLi90cmFpbGNyYWZ0J1xuaW1wb3J0IHsgYWNxdWlyZU9wdGlvbmFsVHJhdmVyc2FsVG9vbCwgYm9vblJhbmssIG9wZW5NaWxlc3RvbmUsIHRvb2xGb3IgfSBmcm9tICcuL2J1aWxkY3JhZnQnXG5pbXBvcnQgeyByZWNvcmRHZW5lcmF0ZWRPcHRpb25hbENvbnRlbnQsIHJlY29yZE9wdGlvbmFsQ29udGVudCwgcmVjb3JkU2VjcmV0VmFsdWUsIHJlY29yZFRlbGVtZXRyeUNvdW50IH0gZnJvbSAnLi4vdGVsZW1ldHJ5J1xuaW1wb3J0IHsgY29uc3VtZVJlbGljU3BlbGwgfSBmcm9tICcuL3JlbGljcydcbmltcG9ydCB7IGFybVJlbGljTW92ZSwgYXJtUmVsaWNXYXRlckNyb3NzaW5nIH0gZnJvbSAnLi9yZWxpY3MnXG5pbXBvcnQgeyBvcGVuRW5jb3VudGVyIH0gZnJvbSAnLi9lbmNvdW50ZXJzJ1xuaW1wb3J0IHsgcmV2ZWFsU2VjcmV0Q2x1ZXMgfSBmcm9tICcuL3NlY3JldC1kaXNjb3ZlcnknXG5pbXBvcnQgeyBjbGFpbVNlY3JldFJld2FyZCwgc2VjcmV0UmVzb2x1dGlvbk1lc3NhZ2UgfSBmcm9tICcuLi9zZWNyZXRzJ1xuaW1wb3J0IHsgdGFrZVNlY3JldFNob3J0Y3V0IH0gZnJvbSAnLi9zaG9ydGN1dHMnXG5cbmNvbnN0IHJlc29sdmVTZWNyZXRQaWNrdXAgPSAoc3RhdGU6IFJ1blN0YXRlLCBpdGVtOiBHcm91bmRJdGVtKTogdm9pZCA9PiB7XG4gIGlmICghaXRlbS5zZWNyZXRJZCkgcmV0dXJuXG4gIGNvbnN0IGNsYWltID0gY2xhaW1TZWNyZXRSZXdhcmQoc3RhdGUsIGl0ZW0uc2VjcmV0SWQpXG4gIGlmICghY2xhaW0gfHwgY2xhaW0gPT09ICdhbHJlYWR5LXJlc29sdmVkJykgcmV0dXJuXG4gIHJlY29yZFNlY3JldFZhbHVlKHN0YXRlLCBjbGFpbS5yZXNvbHV0aW9uLnJld2FyZFZhbHVlKVxuICByZWNvcmRPcHRpb25hbENvbnRlbnQoc3RhdGUsICd1c2VkJywgYHNlY3JldC1yZXNvbHV0aW9uOiR7Y2xhaW0ucm9vbS5pZH06JHtjbGFpbS5yZXNvbHV0aW9uLnJld2FyZEtpbmR9YClcbiAgcmVjb3JkVGVsZW1ldHJ5Q291bnQoc3RhdGUsICdldmVudE91dGNvbWVzJywgYHNlY3JldC1yZXdhcmQ6JHtjbGFpbS5yZXNvbHV0aW9uLnJld2FyZEtpbmR9YClcbiAgcmVjb3JkVGVsZW1ldHJ5Q291bnQoc3RhdGUsICdldmVudE91dGNvbWVzJywgYHNlY3JldC1yaXNrOiR7Y2xhaW0ucmVzb2x1dGlvbi5yaXNrS2luZH1gKVxuICBsb2coc3RhdGUsIHNlY3JldFJlc29sdXRpb25NZXNzYWdlKGNsYWltLnJvb20pKVxufVxuXG5leHBvcnQgZnVuY3Rpb24gcGlja1VwKHN0YXRlOiBSdW5TdGF0ZSk6IEFjdGlvblJlc3VsdCB7XG4gIGNvbnN0IGl0ZW0gPSBzdGF0ZS5mbG9vci5pdGVtcy5maW5kKGN1cnJlbnQgPT4gY3VycmVudC54ID09PSBzdGF0ZS5oZXJvLnggJiYgY3VycmVudC55ID09PSBzdGF0ZS5oZXJvLnkpXG4gIGlmICghaXRlbSkgeyBsb2coc3RhdGUsICdOb3RoaW5nIGhlcmUgdG8gdGFrZS4nKTsgcmV0dXJuIFtdIH1cbiAgaWYgKGl0ZW0uc2VjcmV0SWQgJiYgc3RhdGUuZmxvb3Iuc2VjcmV0Um9vbXM/LmZpbmQocm9vbSA9PiByb29tLmlkID09PSBpdGVtLnNlY3JldElkKT8ucmVzb2x1dGlvbikge1xuICAgIHN0YXRlLmZsb29yLml0ZW1zID0gc3RhdGUuZmxvb3IuaXRlbXMuZmlsdGVyKGN1cnJlbnQgPT4gY3VycmVudCAhPT0gaXRlbSlcbiAgICBsb2coc3RhdGUsICdUaGlzIHNlY3JldCBjYWNoZSBoYXMgYWxyZWFkeSBiZWVuIGNsYWltZWQuJylcbiAgICByZXR1cm4gW11cbiAgfVxuICBpZiAoaXRlbS50b29sKSB7XG4gICAgY29uc3QgYWNxdWlyZWQgPSBhY3F1aXJlT3B0aW9uYWxUcmF2ZXJzYWxUb29sKHN0YXRlLCBpdGVtLnRvb2wpXG4gICAgc3RhdGUuZmxvb3IuaXRlbXMgPSBzdGF0ZS5mbG9vci5pdGVtcy5maWx0ZXIoY3VycmVudCA9PiBjdXJyZW50ICE9PSBpdGVtKVxuICAgIHJlc29sdmVTZWNyZXRQaWNrdXAoc3RhdGUsIGl0ZW0pXG4gICAgbG9nKHN0YXRlLCBhY3F1aXJlZC5yZXN1bHQgPT09ICdib3VuZCcgPyBgWW91IGJpbmQgdGhlICR7dG9vbEZvcihpdGVtLnRvb2wpLm5hbWV9LmAgOiBhY3F1aXJlZC5yZXN1bHQgPT09ICdkdXBsaWNhdGUnID8gYFlvdSBhbHJlYWR5IGNhcnJ5IHRoZSAke3Rvb2xGb3IoaXRlbS50b29sKS5uYW1lfTsgbGVhdmUgaXRzIGR1cGxpY2F0ZSBiZWhpbmQuYCA6IGBZb3UgcmVwbGFjZSAke3Rvb2xGb3IoYWNxdWlyZWQucmVwbGFjZWQhKS5uYW1lfSB3aXRoIHRoZSAke3Rvb2xGb3IoaXRlbS50b29sKS5uYW1lfS5gKVxuICAgIHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSlcbiAgfVxuICBpZiAoaXRlbS5pZCA9PT0gJ2dvbGQnKSB7IGNvbnN0IGdhaW5lZCA9IGdyYW50R29sZChzdGF0ZSwgaXRlbS5jb3VudCk7IHN0YXRlLmZsb29yLml0ZW1zID0gc3RhdGUuZmxvb3IuaXRlbXMuZmlsdGVyKGN1cnJlbnQgPT4gY3VycmVudCAhPT0gaXRlbSk7IHJlc29sdmVTZWNyZXRQaWNrdXAoc3RhdGUsIGl0ZW0pOyBsb2coc3RhdGUsIGBZb3UgcmVjb3ZlciAke2dhaW5lZH0gY2FzaC5gKTsgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncGlja3VwJyldKSB9XG4gIGlmIChpdGVtLmlkID09PSAna2V5JykgeyBzdGF0ZS5oZXJvLmtleXMgKz0gaXRlbS5jb3VudDsgc3RhdGUuZmxvb3IuaXRlbXMgPSBzdGF0ZS5mbG9vci5pdGVtcy5maWx0ZXIoY3VycmVudCA9PiBjdXJyZW50ICE9PSBpdGVtKTsgcmVzb2x2ZVNlY3JldFBpY2t1cChzdGF0ZSwgaXRlbSk7IGxvZyhzdGF0ZSwgJ1lvdSB0YWtlIGEgY2FydmVkIGtleS4nKTsgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncGlja3VwJyldKSB9XG4gIGlmIChzdGF0ZS5oZXJvLmludmVudG9yeS5sZW5ndGggPj0gMTIpIHsgbG9nKHN0YXRlLCAnWW91ciBwYWNrIGlzIGZ1bGwuJyk7IHJldHVybiBbXSB9XG4gIHN0YXRlLmhlcm8uaW52ZW50b3J5LnB1c2goaXRlbS5pZClcbiAgc3RhdGUuZmxvb3IuaXRlbXMgPSBzdGF0ZS5mbG9vci5pdGVtcy5maWx0ZXIoY3VycmVudCA9PiBjdXJyZW50ICE9PSBpdGVtKVxuICByZXNvbHZlU2VjcmV0UGlja3VwKHN0YXRlLCBpdGVtKVxuICBsb2coc3RhdGUsIGBZb3UgdGFrZSAke0lURU1baXRlbS5pZF0ubmFtZX0uYClcbiAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncGlja3VwJyldKVxufVxuXG5leHBvcnQgZnVuY3Rpb24gb3BlcmF0ZShzdGF0ZTogUnVuU3RhdGUpOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBtaWxlc3RvbmUgPSBvcGVuTWlsZXN0b25lKHN0YXRlKVxuICBpZiAobWlsZXN0b25lKSByZXR1cm4gbWlsZXN0b25lXG4gIGNvbnN0IGVuY291bnRlciA9IG9wZW5FbmNvdW50ZXIoc3RhdGUpXG4gIGlmIChlbmNvdW50ZXIpIHJldHVybiBlbmNvdW50ZXJcbiAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHN0YXRlLmhlcm8ueCwgc3RhdGUuaGVyby55KVxuICBjb25zdCByb3V0ZUNhY2hlID0gc3RhdGUuZmxvb3Iucm91dGVDYWNoZVxuICBpZiAocm91dGVDYWNoZSAmJiBNYXRoLm1heChNYXRoLmFicyhyb3V0ZUNhY2hlLnggLSBzdGF0ZS5oZXJvLngpLCBNYXRoLmFicyhyb3V0ZUNhY2hlLnkgLSBzdGF0ZS5oZXJvLnkpKSA8PSAxKSB7IGxvZyhzdGF0ZSwgJ0EgbWFya2VkIFZveWFnZXIgY2FyZ28gY2FjaGUgaXMgcmVhZHkgZm9yIHJlY292ZXJ5LicpOyByZXR1cm4gW2V2ZW50KCdyb3V0ZUNhY2hlJywgcm91dGVDYWNoZS5saW5rSWQpXSB9XG4gIGNvbnN0IGFpcmxvY2sgPSBzdGF0ZS5mbG9vci5haXJsb2Nrcz8uZmluZChjYW5kaWRhdGUgPT4gTWF0aC5tYXgoTWF0aC5hYnMoY2FuZGlkYXRlLnggLSBzdGF0ZS5oZXJvLngpLCBNYXRoLmFicyhjYW5kaWRhdGUueSAtIHN0YXRlLmhlcm8ueSkpIDw9IDEpXG4gIGlmIChhaXJsb2NrKSB7IGxvZyhzdGF0ZSwgYCR7YWlybG9jay5sYWJlbH0gcmVhZHkuYCk7IHJldHVybiBbZXZlbnQoJ3JvdXRlRGVwYXJ0dXJlJywgYWlybG9jay5kZXN0aW5hdGlvblNpdGVJZCA/PyAndm95YWdlcicpXSB9XG4gIGNvbnN0IGZyaWVuZCA9IHN0YXRlLmZsb29yLmFjdG9ycy5maW5kKGFjdG9yID0+ICFhY3Rvci5ob3N0aWxlICYmIGRpc3RhbmNlKGFjdG9yLCBzdGF0ZS5oZXJvKSA8PSAxKVxuICBjb25zdCBhbHRhciA9IHRpbGU/LmtpbmQgPT09ICdhbHRhcicgPyB0aWxlIDogZnJpZW5kICYmIGdldFRpbGUoc3RhdGUuZmxvb3IsIGZyaWVuZC54LCBmcmllbmQueSk/LmtpbmQgPT09ICdhbHRhcicgPyBnZXRUaWxlKHN0YXRlLmZsb29yLCBmcmllbmQueCwgZnJpZW5kLnkpIDogdW5kZWZpbmVkXG4gIGNvbnN0IGNvbnRhaW5lciA9IG5lYXJieUNvbnRhaW5lcihzdGF0ZSlcbiAgaWYgKGNvbnRhaW5lcikge1xuICAgIGNvbnRhaW5lci50aWxlLmtpbmQgPSAnZmxvb3InXG4gICAgY29uc3QgbG9vdCA9IGNvbnRleHR1YWxSZXdhcmQoc3RhdGUsICdjb250YWluZXInKVxuICAgIGdyYW50R29sZChzdGF0ZSwgKGNvbnRhaW5lci5raW5kID09PSAnY2hlc3QnID8gNjAgOiAxOCkgKyBib29uUmFuayhzdGF0ZSwgJ2JhcnRlclRocmVhZCcpICogMTApXG4gICAgaWYgKHN0YXRlLmhlcm8uaW52ZW50b3J5Lmxlbmd0aCA8IDEyKSBzdGF0ZS5oZXJvLmludmVudG9yeS5wdXNoKGxvb3QpXG4gICAgZWxzZSBzdGF0ZS5mbG9vci5pdGVtcy5wdXNoKHsgaWQ6IGxvb3QsIHg6IGNvbnRhaW5lci54LCB5OiBjb250YWluZXIueSwgY291bnQ6IDEgfSlcbiAgICBsb2coc3RhdGUsIGBZb3Ugb3BlbiB0aGUgJHtjb250YWluZXIua2luZH0gYW5kIGZpbmQgJHtJVEVNW2xvb3RdLm5hbWV9LmApXG4gICAgaWYgKGNvbXBsZXRlT2JqZWN0aXZlKHN0YXRlLCAncmVjb3ZlclN1cHBsaWVzJykpIGxvZyhzdGF0ZSwgJ09iamVjdGl2ZSBjb21wbGV0ZTogdHJhaWwgY2FjaGUgc2VjdXJlZC4nKVxuICAgIHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSlcbiAgfVxuICBpZiAodGlsZT8ua2luZCA9PT0gJ3Jlc2N1ZScgfHwgZnJpZW5kPy5uYW1lID09PSAnc3RyYW5kZWQgdHJhdmVsZXInIHx8IGZyaWVuZD8ubmFtZSA9PT0gJ2xvc3Qgc2NvdXQnKSB7XG4gICAgY29uc3Qga25vd25SZXNjdWUgPSBzdGF0ZS5yZXNjdWVkTnBjcz8uc29tZShucGMgPT4gbnBjLmlkID09PSBgcmVzY3VlOiR7c3RhdGUuYXJlYSA/PyBzdGF0ZS5mbG9vci5iaW9tZX06JHtzdGF0ZS5mbG9vci5pbmRleH06JHtmcmllbmQ/LmlkID8/IGAke3N0YXRlLmhlcm8ueH0sJHtzdGF0ZS5oZXJvLnl9YH1gKSA/PyBmYWxzZVxuICAgIGNvbnN0IG5wYyA9IHJlY29yZFJlc2N1ZShzdGF0ZSwgZnJpZW5kKVxuICAgIHN0YXRlLmhlcm8ubWF4SGVhbHRoICs9IDJcbiAgICBzdGF0ZS5oZXJvLmhlYWx0aCA9IE1hdGgubWluKHN0YXRlLmhlcm8ubWF4SGVhbHRoLCBzdGF0ZS5oZXJvLmhlYWx0aCArIDggKyB2aXRhbGl0eVJlc2N1ZVJlY292ZXJ5KHN0YXRlLmhlcm8pKVxuICAgIGdyYW50R29sZChzdGF0ZSwgMzUpXG4gICAgc3RhdGUuZmxvb3IuYWN0b3JzID0gc3RhdGUuZmxvb3IuYWN0b3JzLmZpbHRlcihhY3RvciA9PiBhY3RvciAhPT0gZnJpZW5kKVxuICAgIGNvbnN0IGV2ZW50VGlsZSA9IGZyaWVuZCA/IGdldFRpbGUoc3RhdGUuZmxvb3IsIGZyaWVuZC54LCBmcmllbmQueSkgOiB0aWxlXG4gICAgaWYgKGV2ZW50VGlsZT8ua2luZCA9PT0gJ3Jlc2N1ZScgfHwgZXZlbnRUaWxlPy5raW5kID09PSAnYWx0YXInKSBldmVudFRpbGUua2luZCA9ICdmbG9vcidcbiAgICBsb2coc3RhdGUsIGAke25wYy5uYW1lfSByZWFjaGVzIHRoZSB2aWxsYWdlIG91dHBvc3QuYClcbiAgICBpZiAoY29tcGxldGVPYmplY3RpdmUoc3RhdGUsICdyZXNjdWVTY291dCcpKSBsb2coc3RhdGUsICdPYmplY3RpdmUgY29tcGxldGU6IHRyYXZlbGVyIGFpZGVkLicpXG4gICAgaWYgKCFrbm93blJlc2N1ZSkgdGVuZChzdGF0ZSwgJ3ZpbGxhZ2VQYWN0JylcbiAgICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdyZXNjdWUnKV0pXG4gIH1cbiAgaWYgKGFsdGFyPy5raW5kID09PSAnYWx0YXInKSB7XG4gICAgaWYgKHN0YXRlLmhlcm8uZ29sZCA8IDc1KSB7IGxvZyhzdGF0ZSwgJ1RoZSBzaHJpbmUgYXNrcyBmb3IgNzUgY2FzaC4nKTsgcmV0dXJuIFtdIH1cbiAgICBzcGVuZEdvbGQoc3RhdGUsIDc1KVxuICAgIGNvbnN0IHJld2FyZCA9IGNvbnRleHR1YWxSZXdhcmQoc3RhdGUsICdhbHRhcicpXG4gICAgaWYgKHN0YXRlLmhlcm8uaW52ZW50b3J5Lmxlbmd0aCA8IDEyKSBzdGF0ZS5oZXJvLmludmVudG9yeS5wdXNoKHJld2FyZClcbiAgICBlbHNlIHN0YXRlLmZsb29yLml0ZW1zLnB1c2goeyBpZDogcmV3YXJkLCB4OiBzdGF0ZS5oZXJvLngsIHk6IHN0YXRlLmhlcm8ueSwgY291bnQ6IDEgfSlcbiAgICBnYWluWHAoc3RhdGUsIDM1KVxuICAgIGxvZyhzdGF0ZSwgYFRoZSBzaHJpbmUgZ3JhbnRzIGluc2lnaHQgYW5kICR7SVRFTVtyZXdhcmRdLm5hbWV9LmApXG4gICAgaWYgKGNvbXBsZXRlT2JqZWN0aXZlKHN0YXRlLCAnaW52b2tlQWx0YXInKSkgbG9nKHN0YXRlLCAnT2JqZWN0aXZlIGNvbXBsZXRlOiBzaHJpbmUgb2ZmZXJpbmcgbWFkZS4nKVxuICAgIHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3NwZWxsJyldKVxuICB9XG4gIGlmIChuZWFyYnlHYXRlKHN0YXRlKSkge1xuICAgIGNvbnN0IGxvY2sgPSBuZWFyYnlMb2NrZWREb29yKHN0YXRlKVxuICAgIGlmIChsb2NrICYmIHN0YXRlLmhlcm8ua2V5cyA+IDApIHtcbiAgICAgIHN0YXRlLmhlcm8ua2V5cy0tXG4gICAgICBsb2NrLmtpbmQgPSAnZmxvb3InXG4gICAgICBsb2coc3RhdGUsICdZb3UgdW5sb2NrIHRoZSBzZWFsZWQgZG9vci4nKVxuICAgICAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgnZ2F0ZVJlc29sdmVkJyldKVxuICAgIH1cbiAgICBjb25zdCBnYXRlID0gZ2F0ZUZvclJ1bihzdGF0ZSlcbiAgICBpZiAoIWdhdGUpIHsgbG9nKHN0YXRlLCAnVGhpcyBpcyB0aGUgZmluYWwgcm91dGUuIENyb3NzIHRoZSBhcmVhIHRvIGNvbXBsZXRlIHRoZSBkZWxpdmVyeS4nKTsgcmV0dXJuIFtdIH1cbiAgICBzdGF0ZS5tb2RhbCA9IHsga2luZDogJ2dhdGUnLCBnYXRlSWQ6IGdhdGUuaWQgfVxuICAgIGxvZyhzdGF0ZSwgZ2F0ZS5ucGNPZmZlcmluZylcbiAgICByZXR1cm4gW2V2ZW50KCdtZW51JyldXG4gIH1cbiAgaWYgKGZyaWVuZD8ucm9sZSA9PT0gJ21lcmNoYW50JykgeyBzdGF0ZS5tb2RhbCA9IHsga2luZDogJ3Nob3AnLCBtZXJjaGFudElkOiBmcmllbmQuaWQgfTsgcmV0dXJuIFtldmVudCgnbWVudScpXSB9XG4gIGNvbnN0IHNob3J0Y3V0ID0gdGFrZVNlY3JldFNob3J0Y3V0KHN0YXRlKVxuICBpZiAoc2hvcnRjdXQpIHJldHVybiBzaG9ydGN1dFxuICBjb25zdCBwcm9wT3BlcmF0aW9uID0gb3BlcmF0ZVByb3Aoc3RhdGUpXG4gIGlmIChwcm9wT3BlcmF0aW9uKSB7XG4gICAgcmV2ZWFsU2VjcmV0Q2x1ZXMoc3RhdGUsICdwcm9wJylcbiAgICByZXR1cm4gcHJvcE9wZXJhdGlvbi5ldmVudHMubGVuZ3RoID8gYWR2YW5jZShzdGF0ZSwgcHJvcE9wZXJhdGlvbi5ldmVudHMpIDogW11cbiAgfVxuICBpZiAocmV2ZWFsU2VjcmV0Q2x1ZXMoc3RhdGUsICdwcm9wJykubGVuZ3RoKSByZXR1cm4gW2V2ZW50KCdtZW51JyldXG4gIGxvZyhzdGF0ZSwgJ05vdGhpbmcgYW5zd2Vycy4nKVxuICByZXR1cm4gW11cbn1cblxuZXhwb3J0IGZ1bmN0aW9uIGRlc2NlbmQoc3RhdGU6IFJ1blN0YXRlKTogQWN0aW9uUmVzdWx0IHtcbiAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHN0YXRlLmhlcm8ueCwgc3RhdGUuaGVyby55KVxuICBpZiAodGlsZT8ua2luZCAhPT0gJ2V4aXQnKSB7IGxvZyhzdGF0ZSwgJ1lvdSBhcmUgbm90IGF0IHRoZSBleGl0LicpOyByZXR1cm4gW10gfVxuICBpZiAoc3RhdGUuZmxvb3Iub2JqZWN0aXZlLnN0YXR1cyAhPT0gJ2NvbXBsZXRlJykgeyBsb2coc3RhdGUsIGBPYmplY3RpdmUgaW5jb21wbGV0ZTogJHtzdGF0ZS5mbG9vci5vYmplY3RpdmUubGFiZWx9LmApOyByZXR1cm4gW10gfVxuICBpZiAoIXN0YXRlLmZsb29yLmd1YXJkaWFuRGVmZWF0ZWQpIHsgbG9nKHN0YXRlLCAnQSBndWFyZGlhbiBzdGlsbCBzZWFscyB0aGUgcm91dGUuJyk7IHJldHVybiBbXSB9XG4gIGlmIChzdGF0ZS50cmF2ZWwpIHtcbiAgICBpZiAoc3RhdGUudHJhdmVsLnJlc2lkZW50U3RhcnQgKyBNYXRoLm1pbigzLCBzdGF0ZS50cmF2ZWwuY2h1bmtDb3VudCAtIHN0YXRlLnRyYXZlbC5yZXNpZGVudFN0YXJ0KSA8IHN0YXRlLnRyYXZlbC5jaHVua0NvdW50KSB7IGxvZyhzdGF0ZSwgJ1RoaXMgaXMgYSBzZXJ2aWNlIGhhdGNoLCBub3QgdGhlIGZhciBhaXJsb2NrLiBDb250aW51ZSB0aHJvdWdoIHRoZSByb3V0ZS4nKTsgcmV0dXJuIFtdIH1cbiAgICBzdGF0ZS5tb2RhbCA9IHVuZGVmaW5lZDsgbG9nKHN0YXRlLCAnRmFyIGFpcmxvY2sgcmVhY2hlZC4gUHJlcGFyaW5nIHRoZSBsYW5kaW5nIGZpbGUuJyk7IHJldHVybiBbZXZlbnQoJ2Nvbm5lY3RvckNvbXBsZXRlJyldXG4gIH1cbiAgY29uc3QgYXJlYUZsb29yID0gc3RhdGUuYXJlYUZsb29yID8/IHN0YXRlLmZsb29yLmluZGV4ICUgNFxuICBjb25zdCBiaW9tZSA9IHN0YXRlLmFyZWEgPz8gc3RhdGUuZmxvb3IuYmlvbWVcbiAgY29uc3QgYXJlYUFyYyA9IHN0YXRlLmFyZWFBcmM/LmJpb21lID09PSBiaW9tZSA/IHN0YXRlLmFyZWFBcmMgOiBhcmVhQXJjU3RhdGVGb3Ioc3RhdGUuc2VlZCwgYmlvbWUpXG4gIGlmIChzdGF0ZS5mbG9vci5lc2NhbGF0aW9uKSByZWNvcmRBcmVhQXJjUGhhc2UoYXJlYUFyYywgc3RhdGUuZmxvb3IuZXNjYWxhdGlvbi5waGFzZSlcbiAgc3RhdGUuYXJlYUFyYyA9IGFyZWFBcmNcbiAgc3RhdGUuY29tcGFuaW9ucyA9IHByb2dyZXNzQ29tcGFuaW9uUmVjb3Zlcnkoc3RhdGUuY29tcGFuaW9ucyA/PyBbXSwgc3RhdGUucmVzY3VlZE5wY3MpXG4gIGZvciAoY29uc3QgY29tcGFuaW9uIG9mIHN0YXRlLmNvbXBhbmlvbnMuZmlsdGVyKGNvbXBhbmlvbiA9PiBjb21wYW5pb24uaW5qdXJ5ID09PSAncmVjb3ZlcmluZycgJiYgY29tcGFuaW9uLnJlY292ZXJ5Rmxvb3JzID09PSAwKSkgbG9nKHN0YXRlLCBgJHtjb21wYW5pb24ubmFtZX0ncyBtZWRiYXkgcmVjb3ZlcnkgaXMgcmVhZHkgdG8gY29uY2x1ZGUuYClcbiAgaWYgKGFyZWFGbG9vciA9PT0gMykgeyBzdGF0ZS5tb2RhbCA9IHVuZGVmaW5lZDsgbG9nKHN0YXRlLCBgJHtiaW9tZU5hbWVbc3RhdGUuYXJlYSA/PyBzdGF0ZS5mbG9vci5iaW9tZV19IHN1cnZleSBjb21wbGV0ZS4gUmV0dXJuIHRvIHRoZSBKb21vbiBWb3lhZ2VyLmApOyByZXR1cm4gW2V2ZW50KCdhcmVhQ29tcGxldGUnKV0gfVxuICBjb25zdCBuZXh0QXJlYUZsb29yID0gYXJlYUZsb29yICsgMVxuICBjb25zdCByb3V0ZVBvc2l0aW9uID0gTWF0aC5tYXgoMCwgKHN0YXRlLmFyZWFPcmRlciA/PyBbXSkuaW5kZXhPZihiaW9tZSkpXG4gIHN0YXRlLmZsb29yID0gZ2VuZXJhdGVBcmVhRmxvb3Ioc3RhdGUuc2VlZCwgYmlvbWUsIG5leHRBcmVhRmxvb3IsIHJvdXRlUG9zaXRpb24sIHN0YXRlLmNhbXBhaWduQ3ljbGUpXG4gIGFwcGx5QXJlYUFyY1N0YXRlKHN0YXRlLmZsb29yLCBhcmVhQXJjKVxuICByZWNvcmRHZW5lcmF0ZWRPcHRpb25hbENvbnRlbnQoc3RhdGUpXG4gIHN0YXRlLmFyZWFGbG9vciA9IG5leHRBcmVhRmxvb3JcbiAgc3RhdGUuaGVyby54ID0gc3RhdGUuZmxvb3Iuc3RhcnQueFxuICBzdGF0ZS5oZXJvLnkgPSBzdGF0ZS5mbG9vci5zdGFydC55XG4gIHN0YXRlLmhlcm8uaGVhbHRoID0gTWF0aC5taW4oc3RhdGUuaGVyby5tYXhIZWFsdGgsIHN0YXRlLmhlcm8uaGVhbHRoICsgNCArIHZpdGFsaXR5UmVjb3Zlcnkoc3RhdGUuaGVybykpXG4gIHN0YXRlLmhlcm8uZm9jdXMgPSBzdGF0ZS5oZXJvLm1heEZvY3VzXG4gIHN0YXRlLmhlcm8ub2F0aHMgPSAoc3RhdGUuaGVyby5vYXRocyA/PyBbXSkuZmxhdE1hcChvYXRoID0+IG9hdGgucmVtYWluaW5nRmxvb3JzIDw9IDEgPyBbXSA6IFt7IC4uLm9hdGgsIHJlbWFpbmluZ0Zsb29yczogb2F0aC5yZW1haW5pbmdGbG9vcnMgLSAxIH1dKVxuICBzeW5jaHJvbml6ZVBhcnR5QWN0b3JzKHN0YXRlLCAnZmxvb3JUcmFuc2l0aW9uJylcbiAgc3RhdGUubW9kYWwgPSB7IGtpbmQ6ICd0cmFpbGNyYWZ0JyB9XG4gIGxvZyhzdGF0ZSwgYFlvdSBjb250aW51ZSB0aHJvdWdoICR7YmlvbWVOYW1lW3N0YXRlLmZsb29yLmJpb21lXX0uYClcbiAgbG9nKHN0YXRlLCAnTGFuZGluZyB6b25lIGNsZWFyZWQ6IGNob29zZSBhIGZpZWxkIHVwZ3JhZGUuJylcbiAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgcmV0dXJuIFtldmVudCgnZmxvb3InKV1cbn1cblxuZXhwb3J0IGZ1bmN0aW9uIGludmVudG9yeUNob2ljZShzdGF0ZTogUnVuU3RhdGUsIG1vZGFsOiBFeHRyYWN0PE1vZGFsLCB7IGtpbmQ6ICdpbnZlbnRvcnknIH0+LCBjb21tYW5kOiBzdHJpbmcpOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBpbmRleCA9IE51bWJlcihjb21tYW5kKSAtIDFcbiAgaWYgKCFOdW1iZXIuaXNJbnRlZ2VyKGluZGV4KSB8fCBpbmRleCA8IDAgfHwgaW5kZXggPj0gc3RhdGUuaGVyby5pbnZlbnRvcnkubGVuZ3RoKSByZXR1cm4gW11cbiAgY29uc3QgaWQgPSBzdGF0ZS5oZXJvLmludmVudG9yeVtpbmRleF1cbiAgc3RhdGUubW9kYWwgPSB1bmRlZmluZWRcbiAgaWYgKG1vZGFsLm1vZGUgPT09ICd1c2UnKSByZXR1cm4gdXNlSXRlbShzdGF0ZSwgaWQsIGluZGV4KVxuICBpZiAobW9kYWwubW9kZSA9PT0gJ2Ryb3AnKSB7XG4gICAgc3RhdGUuaGVyby5pbnZlbnRvcnkuc3BsaWNlKGluZGV4LCAxKVxuICAgIHN0YXRlLmZsb29yLml0ZW1zLnB1c2goeyBpZCwgeDogc3RhdGUuaGVyby54LCB5OiBzdGF0ZS5oZXJvLnksIGNvdW50OiAxLCB2aXNpYmxlSW5Gb2c6IHRydWUgfSlcbiAgICBsb2coc3RhdGUsIGBZb3UgZHJvcCAke0lURU1baWRdLm5hbWV9LmApXG4gICAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncGlja3VwJyldKVxuICB9XG4gIGlmIChtb2RhbC5tb2RlID09PSAndGhyb3cnKSB7IHN0YXRlLm1vZGFsID0geyBraW5kOiAndGFyZ2V0JywgYWN0aW9uOiAndGhyb3cnLCBpdGVtOiBpZCB9OyByZXR1cm4gW2V2ZW50KCdtZW51JyldIH1cbiAgcmV0dXJuIGVxdWlwKHN0YXRlLCBpZCwgaW5kZXgpXG59XG5cbmV4cG9ydCBmdW5jdGlvbiB1c2VSb3BlKHN0YXRlOiBSdW5TdGF0ZSk6IEFjdGlvblJlc3VsdCB7XG4gIGNvbnN0IGNsaW1iID0gc3RhdGUuZmxvb3IuY2xpbWJMaW5rcz8uZmluZChsaW5rID0+IChsaW5rLmxvd2VyLnggPT09IHN0YXRlLmhlcm8ueCAmJiBsaW5rLmxvd2VyLnkgPT09IHN0YXRlLmhlcm8ueSkgfHwgKGxpbmsudXBwZXIueCA9PT0gc3RhdGUuaGVyby54ICYmIGxpbmsudXBwZXIueSA9PT0gc3RhdGUuaGVyby55KSlcbiAgaWYgKGNsaW1iPy5hbmNob3JlZCkge1xuICAgIGNvbnN0IGRlc3RpbmF0aW9uID0gY2xpbWIubG93ZXIueCA9PT0gc3RhdGUuaGVyby54ICYmIGNsaW1iLmxvd2VyLnkgPT09IHN0YXRlLmhlcm8ueSA/IGNsaW1iLnVwcGVyIDogY2xpbWIubG93ZXJcbiAgICBzdGF0ZS5oZXJvLnggPSBkZXN0aW5hdGlvbi54XG4gICAgc3RhdGUuaGVyby55ID0gZGVzdGluYXRpb24ueVxuICAgIHN0YXRlLmhlcm8uZm9jdXMgPSBNYXRoLm1pbihzdGF0ZS5oZXJvLm1heEZvY3VzLCBzdGF0ZS5oZXJvLmZvY3VzICsgYm9vblJhbmsoc3RhdGUsICdnYWxlVGhyZWFkJykpXG4gICAgZm9yIChsZXQgcmFkaXVzID0gMDsgcmFkaXVzIDwgYm9vblJhbmsoc3RhdGUsICdoaWdoUGF0aCcpOyByYWRpdXMrKykgZm9yIChjb25zdCB0aWxlIG9mIHN0YXRlLmZsb29yLnRpbGVzKSBpZiAoIXRpbGUuZXhwbG9yZWQpIHsgdGlsZS5leHBsb3JlZCA9IHRydWU7IGJyZWFrIH1cbiAgICByZWZyZXNoRm92KHN0YXRlKVxuICAgIGxvZyhzdGF0ZSwgJ1lvdSBjbGltYiB0aGUgc2VjdXJlZCB2ZXJ0aWNhbCByb3BlLicpXG4gICAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgndHJhdmVyc2UnKSwgZXZlbnQoJ3JvcGUnKV0pXG4gIH1cbiAgaWYgKGNsaW1iKSB7XG4gICAgY29uc3QgZnJlZSA9IGJvb25SYW5rKHN0YXRlLCAncm9wZXdyaWdodCcpID49IDIgJiYgc3RhdGUudHVybiAlIDIgPT09IDBcbiAgICBpZiAoIWZyZWUgJiYgc3RhdGUuaGVyby5yb3BlcyA8IDEpIHsgbG9nKHN0YXRlLCAnTm8gcm9wZXMgcmVtYWluLicpOyByZXR1cm4gW10gfVxuICAgIGlmICghZnJlZSkgc3RhdGUuaGVyby5yb3Blcy0tXG4gICAgY2xpbWIuYW5jaG9yZWQgPSB0cnVlXG4gICAgbG9nKHN0YXRlLCBmcmVlID8gJ1JvcGV3cmlnaHQgYmluZHMgdGhlIHZlcnRpY2FsIHJvdXRlIHdpdGhvdXQgc3BlbmRpbmcgcmVzZXJ2ZSByb3BlLicgOiAnWW91IHNlY3VyZSBhIHZlcnRpY2FsIHJvcGUgYmV0d2VlbiB0aGUgY2xpZmYgdGllcnMuJylcbiAgICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdyb3BlJyldKVxuICB9XG4gIGlmIChzdGF0ZS5oZXJvLnJvcGVzIDwgMSkgeyBsb2coc3RhdGUsICdObyByb3BlcyByZW1haW4uJyk7IHJldHVybiBbXSB9XG4gIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBzdGF0ZS5oZXJvLngsIHN0YXRlLmhlcm8ueSkhXG4gIGlmICh0aWxlLmtpbmQgPT09ICdwaXQnKSB0aWxlLmtpbmQgPSAncm9wZSdcbiAgZWxzZSB7XG4gICAgY29uc3QgYmVsb3cgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBzdGF0ZS5oZXJvLngsIHN0YXRlLmhlcm8ueSArIDEpXG4gICAgaWYgKGJlbG93Py5raW5kID09PSAncGl0JykgYmVsb3cua2luZCA9ICdyb3BlJ1xuICAgIGVsc2Uge1xuICAgICAgY29uc3QgY2FydEV2ZW50cyA9IHJlbGVhc2VDYXJ0V2l0aFJvcGUoc3RhdGUpXG4gICAgICBpZiAoY2FydEV2ZW50cyAhPT0gdW5kZWZpbmVkKSB7XG4gICAgICAgIGlmICghY2FydEV2ZW50cy5sZW5ndGgpIHJldHVybiBbXVxuICAgICAgICBzdGF0ZS5oZXJvLnJvcGVzLS1cbiAgICAgICAgbG9nKHN0YXRlLCAnWW91IHJpZyB0aGUgY2FydCB3aXRoIGEgcm9wZS4nKVxuICAgICAgICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdyb3BlJyksIC4uLmNhcnRFdmVudHNdKVxuICAgICAgfVxuICAgICAgY29uc3QgYm9hdEV2ZW50cyA9IGFuY2hvckJvYXRXaXRoUm9wZShzdGF0ZSlcbiAgICAgIGlmIChib2F0RXZlbnRzICE9PSB1bmRlZmluZWQpIHtcbiAgICAgICAgaWYgKCFib2F0RXZlbnRzLmxlbmd0aCkgcmV0dXJuIFtdXG4gICAgICAgIHN0YXRlLmhlcm8ucm9wZXMtLVxuICAgICAgICBsb2coc3RhdGUsICdZb3UgYW5jaG9yIHRoZSBib2F0IHdpdGggYSByb3BlLicpXG4gICAgICAgIHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3JvcGUnKSwgLi4uYm9hdEV2ZW50c10pXG4gICAgICB9XG4gICAgICBjb25zdCBhcmNoRXZlbnRzID0gc2VjdXJlQ29sbGFwc2VkQXJjaFdpdGhSb3BlKHN0YXRlKVxuICAgICAgaWYgKGFyY2hFdmVudHMgIT09IHVuZGVmaW5lZCkge1xuICAgICAgICBpZiAoIWFyY2hFdmVudHMubGVuZ3RoKSByZXR1cm4gW11cbiAgICAgICAgc3RhdGUuaGVyby5yb3Blcy0tXG4gICAgICAgIGxvZyhzdGF0ZSwgJ1lvdSBicmFjZSB0aGUgY29sbGFwc2VkIGFyY2ggd2l0aCBhIHJvcGUuJylcbiAgICAgICAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncm9wZScpLCAuLi5hcmNoRXZlbnRzXSlcbiAgICAgIH1cbiAgICAgIGxvZyhzdGF0ZSwgJ1RoZXJlIGlzIG5vd2hlcmUgdG8gYW5jaG9yIGEgcm9wZS4nKVxuICAgICAgcmV0dXJuIFtdXG4gICAgfVxuICB9XG4gIHN0YXRlLmhlcm8ucm9wZXMtLVxuICBsb2coc3RhdGUsICdZb3Ugc2VjdXJlIGEgcm9wZS4nKVxuICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdyb3BlJyldKVxufVxuXG5leHBvcnQgZnVuY3Rpb24gY2FzdEZpcnN0U3BlbGwoc3RhdGU6IFJ1blN0YXRlKTogQWN0aW9uUmVzdWx0IHtcbiAgaWYgKHN0YXRlLmhlcm8ub2F0aHM/LnNvbWUob2F0aCA9PiBvYXRoLmlkID09PSAnbm9DaGFybXMnKSkgeyBsb2coc3RhdGUsICdZb3VyIGFjdGl2ZSBvYXRoIGZvcmJpZHMgY2hhcm1zLicpOyByZXR1cm4gW10gfVxuICBjb25zdCBpZCA9IHN0YXRlLmhlcm8uaW52ZW50b3J5LmZpbmQoaXRlbSA9PiBJVEVNW2l0ZW1dLnVzZSA9PT0gJ3NwZWxsJylcbiAgaWYgKCFpZCkgeyBsb2coc3RhdGUsICdZb3Uga25vdyBubyByZWFkeSBjaGFybS4nKTsgcmV0dXJuIFtdIH1cbiAgc3RhdGUubW9kYWwgPSB7IGtpbmQ6ICd0YXJnZXQnLCBhY3Rpb246ICdzcGVsbCcsIGl0ZW06IGlkIH1cbiAgcmV0dXJuIFtldmVudCgnbWVudScpXVxufVxuXG5leHBvcnQgZnVuY3Rpb24gcXVpY2tDYXN0KHN0YXRlOiBSdW5TdGF0ZSwgZGlyZWN0aW9uOiBEaXJlY3Rpb24pOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBpZCA9IHN0YXRlLmhlcm8uaW52ZW50b3J5LmZpbmQoaXRlbSA9PiBJVEVNW2l0ZW1dLnVzZSA9PT0gJ3NwZWxsJylcbiAgaWYgKCFpZCkgeyBsb2coc3RhdGUsICdZb3Uga25vdyBubyByZWFkeSBjaGFybS4nKTsgcmV0dXJuIFtdIH1cbiAgcmV0dXJuIGNhc3RTcGVsbChzdGF0ZSwgaWQsIGRpcmVjdGlvbilcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIGJvbWIoc3RhdGU6IFJ1blN0YXRlLCBkaXJlY3Rpb246IERpcmVjdGlvbik6IEFjdGlvblJlc3VsdCB7XG4gIGlmIChzdGF0ZS5oZXJvLm9hdGhzPy5zb21lKG9hdGggPT4gb2F0aC5pZCA9PT0gJ25vQm9tYnMnKSkgeyBsb2coc3RhdGUsICdZb3VyIGFjdGl2ZSBvYXRoIGZvcmJpZHMgYm9tYnMuJyk7IHJldHVybiBbXSB9XG4gIGlmIChzdGF0ZS5oZXJvLmJvbWJzIDwgMSkgeyBsb2coc3RhdGUsICdObyBib21icyByZW1haW4uJyk7IHJldHVybiBbXSB9XG4gIHN0YXRlLmhlcm8uYm9tYnMtLVxuICBjb25zdCBkZWx0YSA9IERJUkVDVElPTlNbZGlyZWN0aW9uXVxuICBsb2coc3RhdGUsICdZb3UgcGxhY2UgYSBib21iLicpXG4gIGNvbnN0IGxhc3RNYXRjaCA9IHN0YXRlLmhlcm8uaGVhbHRoICogNCA8PSBzdGF0ZS5oZXJvLm1heEhlYWx0aCA/IGJvb25SYW5rKHN0YXRlLCAnbGFzdE1hdGNoJykgKiAyIDogMFxuICBleHBsb2RlKHN0YXRlLCBzdGF0ZS5oZXJvLnggKyBkZWx0YS54ICogMiwgc3RhdGUuaGVyby55ICsgZGVsdGEueSAqIDIsIDEyICsgbGFzdE1hdGNoLCBbJ2JvbWInXSwgJ3lvdXIgYm9tYicsIDEgKyBNYXRoLm1pbigxLCBib29uUmFuayhzdGF0ZSwgJ3NwYXJlRnVzZScpKSlcbiAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgnYm9vbScpXSlcbn1cblxuY29uc3QgZHJpbGxhYmxlVGVycmFpbiA9IG5ldyBTZXQoWyd3YWxsJywgJ3J1YmJsZScsICdicmFtYmxlJywgJ2JvdWxkZXInLCAnYnJlYWt3YWxsJ10pXG5jb25zdCBnbGlkYWJsZVRlcnJhaW4gPSBuZXcgU2V0KFsncGl0JywgJ3dhdGVyJywgJ2RlZXBXYXRlcicsICdsYXZhJywgJ3NwaWtlcycsICdkYXJ0JywgJ2ZpcmVWZW50JywgJ2dhcycsICdzbW9rZScsICdjcnVtYmxlJywgJ2JvdWxkZXInLCAnYnJhbWJsZScsICdydWJibGUnLCAnY3VycmVudCddKVxuY29uc3QgZ3JhcHBsZVRlcnJhaW4gPSBuZXcgU2V0KFsncGl0JywgJ3dhdGVyJywgJ2RlZXBXYXRlcicsICdsYXZhJywgJ3NwaWtlcycsICdkYXJ0JywgJ2ZpcmVWZW50JywgJ2dhcycsICdzbW9rZScsICdjcnVtYmxlJywgJ2N1cnJlbnQnXSlcbmNvbnN0IGdyYXBwbGVCbG9ja2VycyA9IG5ldyBTZXQoWyd3YWxsJywgJ3J1YmJsZScsICdicmFtYmxlJywgJ2JvdWxkZXInLCAnYnJlYWt3YWxsJywgJ2NyYXRlJywgJ2NoZXN0JywgJ2xvY2tlZERvb3InXSlcbmNvbnN0IGJyaWRnZVRlcnJhaW4gPSBuZXcgU2V0KFsncGl0JywgJ3dhdGVyJywgJ2RlZXBXYXRlcicsICdjdXJyZW50J10pXG5jb25zdCBkYXNoVGVycmFpbiA9IG5ldyBTZXQoWydzbW9rZScsICdnYXMnLCAnZmlyZVZlbnQnLCAnY3VycmVudCddKVxuY29uc3Qgd2luY2hUZXJyYWluID0gbmV3IFNldChbJ2JvdWxkZXInLCAncnViYmxlJywgJ2NyYXRlJ10pXG5cbmV4cG9ydCBmdW5jdGlvbiBkcmlsbChzdGF0ZTogUnVuU3RhdGUsIGlkOiBzdHJpbmcsIGRpcmVjdGlvbjogRXhjbHVkZTxEaXJlY3Rpb24sICd3YWl0Jz4pOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBpbmRleCA9IHN0YXRlLmhlcm8uaW52ZW50b3J5LmluZGV4T2YoaWQpXG4gIGNvbnN0IGRlbHRhID0gRElSRUNUSU9OU1tkaXJlY3Rpb25dXG4gIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBzdGF0ZS5oZXJvLnggKyBkZWx0YS54LCBzdGF0ZS5oZXJvLnkgKyBkZWx0YS55KVxuICBpZiAoaW5kZXggPCAwIHx8ICF0aWxlIHx8ICFkcmlsbGFibGVUZXJyYWluLmhhcyh0aWxlLmtpbmQpKSB7IGxvZyhzdGF0ZSwgJ1RoZSBhdWdlciBuZWVkcyBibG9ja2VkIGdyb3VuZC4nKTsgcmV0dXJuIFtdIH1cbiAgdGlsZS5raW5kID0gJ2Zsb29yJ1xuICBjb25zdW1lKHN0YXRlLCBpbmRleClcbiAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgbG9nKHN0YXRlLCAnVGhlIGF1Z2VyIG9wZW5zIGEgbmFycm93IHBhc3NhZ2UuJylcbiAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgndHJhdmVyc2UnKSwgZXZlbnQoJ3BpY2t1cCcpXSlcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIGdsaWRlKHN0YXRlOiBSdW5TdGF0ZSwgaWQ6IHN0cmluZywgZGlyZWN0aW9uOiBFeGNsdWRlPERpcmVjdGlvbiwgJ3dhaXQnPik6IEFjdGlvblJlc3VsdCB7XG4gIGNvbnN0IGluZGV4ID0gc3RhdGUuaGVyby5pbnZlbnRvcnkuaW5kZXhPZihpZClcbiAgY29uc3QgZGVsdGEgPSBESVJFQ1RJT05TW2RpcmVjdGlvbl1cbiAgY29uc3QgbWlkZGxlID0gZ2V0VGlsZShzdGF0ZS5mbG9vciwgc3RhdGUuaGVyby54ICsgZGVsdGEueCwgc3RhdGUuaGVyby55ICsgZGVsdGEueSlcbiAgY29uc3QgbGFuZGluZyA9IHsgeDogc3RhdGUuaGVyby54ICsgZGVsdGEueCAqIDIsIHk6IHN0YXRlLmhlcm8ueSArIGRlbHRhLnkgKiAyIH1cbiAgaWYgKGluZGV4IDwgMCB8fCAhbWlkZGxlIHx8ICFnbGlkYWJsZVRlcnJhaW4uaGFzKG1pZGRsZS5raW5kKSB8fCAhaXNQYXNzYWJsZShzdGF0ZS5mbG9vciwgbGFuZGluZy54LCBsYW5kaW5nLnkpKSB7IGxvZyhzdGF0ZSwgJ1RoZSBnbGlkZXIgbmVlZHMgYSBjbGVhciBsYW5kaW5nIGJleW9uZCBoYXphcmRvdXMgZ3JvdW5kLicpOyByZXR1cm4gW10gfVxuICBzdGF0ZS5oZXJvLnggPSBsYW5kaW5nLnhcbiAgc3RhdGUuaGVyby55ID0gbGFuZGluZy55XG4gIGlmIChtaWRkbGUua2luZCA9PT0gJ3dhdGVyJyB8fCBtaWRkbGUua2luZCA9PT0gJ2N1cnJlbnQnKSBhcm1SZWxpY1dhdGVyQ3Jvc3Npbmcoc3RhdGUpXG4gIGFybVJlbGljTW92ZShzdGF0ZSlcbiAgY29uc3VtZShzdGF0ZSwgaW5kZXgpXG4gIHJlZnJlc2hGb3Yoc3RhdGUpXG4gIGxvZyhzdGF0ZSwgJ1lvdSByaWRlIHRoZSByZWVkIGdsaWRlciBhY3Jvc3MgdGhlIGhhemFyZC4nKVxuICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCd0cmF2ZXJzZScpLCBldmVudCgnbW92ZScpXSlcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIGdyYXBwbGUoc3RhdGU6IFJ1blN0YXRlLCBpZDogc3RyaW5nLCBkaXJlY3Rpb246IEV4Y2x1ZGU8RGlyZWN0aW9uLCAnd2FpdCc+KTogQWN0aW9uUmVzdWx0IHtcbiAgY29uc3QgaW5kZXggPSBzdGF0ZS5oZXJvLmludmVudG9yeS5pbmRleE9mKGlkKVxuICBjb25zdCBkZWx0YSA9IERJUkVDVElPTlNbZGlyZWN0aW9uXVxuICBjb25zdCByb3V0ZSA9IFszLCAyXS5tYXAocmFuZ2UgPT4ge1xuICAgIGNvbnN0IGNyb3NzaW5nID0gQXJyYXkuZnJvbSh7IGxlbmd0aDogcmFuZ2UgLSAxIH0sIChfLCBvZmZzZXQpID0+IGdldFRpbGUoc3RhdGUuZmxvb3IsIHN0YXRlLmhlcm8ueCArIGRlbHRhLnggKiAob2Zmc2V0ICsgMSksIHN0YXRlLmhlcm8ueSArIGRlbHRhLnkgKiAob2Zmc2V0ICsgMSkpKVxuICAgIGNvbnN0IGxhbmRpbmcgPSB7IHg6IHN0YXRlLmhlcm8ueCArIGRlbHRhLnggKiByYW5nZSwgeTogc3RhdGUuaGVyby55ICsgZGVsdGEueSAqIHJhbmdlIH1cbiAgICByZXR1cm4geyBjcm9zc2luZywgbGFuZGluZyB9XG4gIH0pLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5jcm9zc2luZy5ldmVyeSh0aWxlID0+IHRpbGUgJiYgIWdyYXBwbGVCbG9ja2Vycy5oYXModGlsZS5raW5kKSkgJiYgY2FuZGlkYXRlLmNyb3NzaW5nLnNvbWUodGlsZSA9PiB0aWxlICYmIGdyYXBwbGVUZXJyYWluLmhhcyh0aWxlLmtpbmQpKSAmJiBpc1Bhc3NhYmxlKHN0YXRlLmZsb29yLCBjYW5kaWRhdGUubGFuZGluZy54LCBjYW5kaWRhdGUubGFuZGluZy55KSlcbiAgaWYgKGluZGV4IDwgMCB8fCAhcm91dGUpIHsgbG9nKHN0YXRlLCAnVGhlIGdyYXBwbGluZyBsaW5lIG5lZWRzIGEgY2xlYXIgbGFuZGluZyBiZXlvbmQgYSBnYXAgb3IgaGF6YXJkLicpOyByZXR1cm4gW10gfVxuICBzdGF0ZS5oZXJvLnggPSByb3V0ZS5sYW5kaW5nLnhcbiAgc3RhdGUuaGVyby55ID0gcm91dGUubGFuZGluZy55XG4gIGlmIChyb3V0ZS5jcm9zc2luZy5zb21lKHRpbGUgPT4gdGlsZT8ua2luZCA9PT0gJ3dhdGVyJyB8fCB0aWxlPy5raW5kID09PSAnY3VycmVudCcpKSBhcm1SZWxpY1dhdGVyQ3Jvc3Npbmcoc3RhdGUpXG4gIGFybVJlbGljTW92ZShzdGF0ZSlcbiAgY29uc3VtZShzdGF0ZSwgaW5kZXgpXG4gIHJlZnJlc2hGb3Yoc3RhdGUpXG4gIGxvZyhzdGF0ZSwgJ1RoZSBncmFwcGxpbmcgbGluZSBjYXJyaWVzIHlvdSBvdmVyIHRoZSBicmVhay4nKVxuICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCd0cmF2ZXJzZScpLCBldmVudCgnbW92ZScpXSlcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIGJyaWRnZShzdGF0ZTogUnVuU3RhdGUsIGlkOiBzdHJpbmcsIGRpcmVjdGlvbjogRXhjbHVkZTxEaXJlY3Rpb24sICd3YWl0Jz4pOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBpbmRleCA9IHN0YXRlLmhlcm8uaW52ZW50b3J5LmluZGV4T2YoaWQpXG4gIGNvbnN0IGRlbHRhID0gRElSRUNUSU9OU1tkaXJlY3Rpb25dXG4gIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBzdGF0ZS5oZXJvLnggKyBkZWx0YS54LCBzdGF0ZS5oZXJvLnkgKyBkZWx0YS55KVxuICBpZiAoaW5kZXggPCAwIHx8ICF0aWxlIHx8ICFicmlkZ2VUZXJyYWluLmhhcyh0aWxlLmtpbmQpKSB7IGxvZyhzdGF0ZSwgJ1RoZSBicmlkZ2UgbmVlZHMgYW4gYWRqYWNlbnQgcGl0LCB3YXRlciwgY3VycmVudCwgb3IgZGVlcCB3YXRlci4nKTsgcmV0dXJuIFtdIH1cbiAgdGlsZS5raW5kID0gJ3JvcGUnXG4gIGRlbGV0ZSB0aWxlLmZsb3dcbiAgY29uc3VtZShzdGF0ZSwgaW5kZXgpXG4gIHJlZnJlc2hGb3Yoc3RhdGUpXG4gIGxvZyhzdGF0ZSwgJ1RoZSBicmlkZ2UgbG9ja3MgaW50byBhIHBlcm1hbmVudCBjcm9zc2luZy4nKVxuICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCd0cmF2ZXJzZScpLCBldmVudCgncm9wZScpXSlcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIGRhc2goc3RhdGU6IFJ1blN0YXRlLCBpZDogc3RyaW5nLCBkaXJlY3Rpb246IEV4Y2x1ZGU8RGlyZWN0aW9uLCAnd2FpdCc+KTogQWN0aW9uUmVzdWx0IHtcbiAgY29uc3QgaW5kZXggPSBzdGF0ZS5oZXJvLmludmVudG9yeS5pbmRleE9mKGlkKVxuICBjb25zdCBkZWx0YSA9IERJUkVDVElPTlNbZGlyZWN0aW9uXVxuICBjb25zdCBtaWRkbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBzdGF0ZS5oZXJvLnggKyBkZWx0YS54LCBzdGF0ZS5oZXJvLnkgKyBkZWx0YS55KVxuICBjb25zdCBsYW5kaW5nID0geyB4OiBzdGF0ZS5oZXJvLnggKyBkZWx0YS54ICogMiwgeTogc3RhdGUuaGVyby55ICsgZGVsdGEueSAqIDIgfVxuICBpZiAoaW5kZXggPCAwIHx8ICFtaWRkbGUgfHwgIWRhc2hUZXJyYWluLmhhcyhtaWRkbGUua2luZCkgfHwgIWlzUGFzc2FibGUoc3RhdGUuZmxvb3IsIGxhbmRpbmcueCwgbGFuZGluZy55KSkgeyBsb2coc3RhdGUsICdUaGUgc3RlYW0gamV0cGFjayBuZWVkcyBzbW9rZSwgZ2FzLCBmaXJlLCBvciBjdXJyZW50IGJlZm9yZSBhIGNsZWFyIGxhbmRpbmcuJyk7IHJldHVybiBbXSB9XG4gIHN0YXRlLmhlcm8ueCA9IGxhbmRpbmcueFxuICBzdGF0ZS5oZXJvLnkgPSBsYW5kaW5nLnlcbiAgaWYgKG1pZGRsZS5raW5kID09PSAnY3VycmVudCcpIGFybVJlbGljV2F0ZXJDcm9zc2luZyhzdGF0ZSlcbiAgYXJtUmVsaWNNb3ZlKHN0YXRlKVxuICBjb25zdW1lKHN0YXRlLCBpbmRleClcbiAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgbG9nKHN0YXRlLCAnU3RlYW0gY2FycmllcyB5b3UgdGhyb3VnaCB0aGUgaGF6YXJkLicpXG4gIHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3RyYXZlcnNlJyksIGV2ZW50KCdtb3ZlJyldKVxufVxuXG5leHBvcnQgZnVuY3Rpb24gd2luY2goc3RhdGU6IFJ1blN0YXRlLCBpZDogc3RyaW5nLCBkaXJlY3Rpb246IEV4Y2x1ZGU8RGlyZWN0aW9uLCAnd2FpdCc+KTogQWN0aW9uUmVzdWx0IHtcbiAgY29uc3QgaW5kZXggPSBzdGF0ZS5oZXJvLmludmVudG9yeS5pbmRleE9mKGlkKVxuICBjb25zdCBkZWx0YSA9IERJUkVDVElPTlNbZGlyZWN0aW9uXVxuICBjb25zdCB0YXJnZXQgPSB7IHg6IHN0YXRlLmhlcm8ueCArIGRlbHRhLngsIHk6IHN0YXRlLmhlcm8ueSArIGRlbHRhLnkgfVxuICBjb25zdCB0aWxlID0gZ2V0VGlsZShzdGF0ZS5mbG9vciwgdGFyZ2V0LngsIHRhcmdldC55KVxuICBpZiAoaW5kZXggPCAwIHx8ICF0aWxlIHx8ICF3aW5jaFRlcnJhaW4uaGFzKHRpbGUua2luZCkpIHsgbG9nKHN0YXRlLCAnVGhlIHdpbmNoIG5lZWRzIGFuIGFkamFjZW50IGJvdWxkZXIsIHJ1YmJsZSwgb3IgY3JhdGUuJyk7IHJldHVybiBbXSB9XG4gIHRpbGUua2luZCA9ICdmbG9vcidcbiAgc3RhdGUuaGVyby54ID0gdGFyZ2V0LnhcbiAgc3RhdGUuaGVyby55ID0gdGFyZ2V0LnlcbiAgYXJtUmVsaWNNb3ZlKHN0YXRlKVxuICBjb25zdW1lKHN0YXRlLCBpbmRleClcbiAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgbG9nKHN0YXRlLCAnVGhlIHdpbmNoIGNsZWFycyB0aGUgb2JzdHJ1Y3Rpb24gYW5kIHJlZWxzIHlvdSBmb3J3YXJkLicpXG4gIHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3RyYXZlcnNlJyksIGV2ZW50KCdtb3ZlJyldKVxufVxuXG5leHBvcnQgZnVuY3Rpb24gdGhyb3dJdGVtKHN0YXRlOiBSdW5TdGF0ZSwgaWQ6IHN0cmluZywgZGlyZWN0aW9uOiBEaXJlY3Rpb24pOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBpbmRleCA9IHN0YXRlLmhlcm8uaW52ZW50b3J5LmluZGV4T2YoaWQpXG4gIGlmIChpbmRleCA9PT0gLTEpIHJldHVybiBbXVxuICBzdGF0ZS5oZXJvLmludmVudG9yeS5zcGxpY2UoaW5kZXgsIDEpXG4gIGNvbnN0IGRlbHRhID0gRElSRUNUSU9OU1tkaXJlY3Rpb25dXG4gIGNvbnN0IGRlc3RpbmF0aW9uID0geyB4OiBzdGF0ZS5oZXJvLnggKyBkZWx0YS54ICogNSwgeTogc3RhdGUuaGVyby55ICsgZGVsdGEueSAqIDUgfVxuICBjb25zdCBjZWxscyA9IHJlc29sdmVMaW5lRWZmZWN0KHN0YXRlLmZsb29yLCBzdGF0ZS5oZXJvLCBkZXN0aW5hdGlvbikuY2VsbHNcbiAgY29uc3QgcG9pbnQgPSBjZWxscy5hdCgtMSkgPz8geyB4OiBzdGF0ZS5oZXJvLngsIHk6IHN0YXRlLmhlcm8ueSB9XG4gIGNvbnN0IHRhcmdldCA9IGFjdG9yQXQoc3RhdGUuZmxvb3IsIHBvaW50LngsIHBvaW50LnkpXG4gIGlmICh0YXJnZXQ/Lmhvc3RpbGUpIHsgdGFyZ2V0LmhlYWx0aCAtPSBtb2RpZnlJbmNvbWluZ0RhbWFnZSh0YXJnZXQsIDMgKyBzdGF0ZS5oZXJvLnN0YXRzLnN0cmVuZ3RoICsgYm9vblJhbmsoc3RhdGUsICdlbWJlckZsZXRjaGluZycpICsgYm9vblJhbmsoc3RhdGUsICd0aHVuZGVyVmVzc2VsJykgKyBib29uUmFuayhzdGF0ZSwgJ3RldGhlcmVkVGh1bmRlcicpKTsgaWYgKGJvb25SYW5rKHN0YXRlLCAndGh1bmRlclZlc3NlbCcpKSBhZGRDb25kaXRpb24odGFyZ2V0LCB7IGtpbmQ6ICdtYXJrZWQnLCBkdXJhdGlvbjogMiwgcG90ZW5jeTogYm9vblJhbmsoc3RhdGUsICd0aHVuZGVyVmVzc2VsJykgfSk7IGxvZyhzdGF0ZSwgYCR7SVRFTVtpZF0ubmFtZX0gaGl0cyAke3RhcmdldC5uYW1lfS5gKSB9XG4gIGlmIChpZCA9PT0gJ2ZpcmVKYXInKSBleHBsb2RlKHN0YXRlLCBwb2ludC54LCBwb2ludC55LCA1LCBbJ2JvbWInLCAnZmlyZSddKVxuICBlbHNlIHtcbiAgICBzdGF0ZS5mbG9vci5pdGVtcy5wdXNoKHsgaWQsIHg6IHBvaW50LngsIHk6IHBvaW50LnksIGNvdW50OiAxLCB2aXNpYmxlSW5Gb2c6IHRydWUgfSlcbiAgICBhcHBseVByb3BFZmZlY3RzKHN0YXRlLCBbcG9pbnRdLCBbJ3Rocm93J10pXG4gIH1cbiAgcmVzb2x2ZURlZmVhdGVkQWN0b3JzKHN0YXRlKVxuICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KGlkID09PSAnZmlyZUphcicgPyAnYm9vbScgOiAnaGl0JyldKVxufVxuXG5leHBvcnQgZnVuY3Rpb24gY2FzdFNwZWxsKHN0YXRlOiBSdW5TdGF0ZSwgaWQ6IHN0cmluZywgZGlyZWN0aW9uOiBEaXJlY3Rpb24pOiBBY3Rpb25SZXN1bHQge1xuICBpZiAoc3RhdGUuaGVyby5vYXRocz8uc29tZShvYXRoID0+IG9hdGguaWQgPT09ICdub0NoYXJtcycpKSB7IGxvZyhzdGF0ZSwgJ1lvdXIgYWN0aXZlIG9hdGggZm9yYmlkcyBjaGFybXMuJyk7IHJldHVybiBbXSB9XG4gIGNvbnN0IGl0ZW0gPSBJVEVNW2lkXVxuICBjb25zdCBwcm9maWxlID0gc2NyaXB0Q2FzdFByb2ZpbGUoc3RhdGUuaGVybywgaWQpXG4gIGNvbnN0IGNpcmN1aXRSZWFkeSA9IEJvb2xlYW4oc3RhdGUuaGVyby5yZWxpY0NoYXJnZXM/LmFzaENpcmN1aXQpXG4gIGNvbnN0IGN1cnJlbnRUZXJyYWluID0gZ2V0VGlsZShzdGF0ZS5mbG9vciwgc3RhdGUuaGVyby54LCBzdGF0ZS5oZXJvLnkpPy5raW5kXG4gIGNvbnN0IGZvY3VzQ29zdCA9IE1hdGgubWF4KDEsIHByb2ZpbGUuZm9jdXNDb3N0IC0gTnVtYmVyKGNpcmN1aXRSZWFkeSkgLSBib29uUmFuayhzdGF0ZSwgJ2Zyb3plbkZvY3VzJykgKiBOdW1iZXIoaGFzQ29uZGl0aW9uKHN0YXRlLmhlcm8sICdzaGllbGRlZCcpKSAtIGJvb25SYW5rKHN0YXRlLCAnc3Vuc2V0Q2lyY3VpdCcpICogTnVtYmVyKGN1cnJlbnRUZXJyYWluID09PSAnc2FsdE1pcnJvcicpKVxuICBpZiAoc3RhdGUuaGVyby5mb2N1cyA8IGZvY3VzQ29zdCkgeyBsb2coc3RhdGUsICdZb3UgbGFjayBmb2N1cy4nKTsgcmV0dXJuIFtdIH1cbiAgY29uc3QgY2lyY3VpdCA9IGNpcmN1aXRSZWFkeSAmJiBjb25zdW1lUmVsaWNTcGVsbChzdGF0ZSlcbiAgc3RhdGUuaGVyby5mb2N1cyAtPSBmb2N1c0Nvc3RcbiAgY29uc3QgZ2VvbWV0cnkgPSByZXNvbHZlU3luZXJnaWVzKHsgc2NyaXB0czogW2lkXSwgc2tpbGxzOiBzdGF0ZS5oZXJvLnNraWxscyB9LCB7IHJhbmdlOiBwcm9maWxlLnJhbmdlIH0pXG4gIGNvbnN0IGRlbHRhID0gRElSRUNUSU9OU1tkaXJlY3Rpb25dXG4gIGNvbnN0IHBvaW50ID0geyB4OiBzdGF0ZS5oZXJvLnggKyBkZWx0YS54ICogTWF0aC5tYXgoMSwgTWF0aC5mbG9vcihnZW9tZXRyeS52YWx1ZXMucmFuZ2UgPz8gcHJvZmlsZS5yYW5nZSkpLCB5OiBzdGF0ZS5oZXJvLnkgKyBkZWx0YS55ICogTWF0aC5tYXgoMSwgTWF0aC5mbG9vcihnZW9tZXRyeS52YWx1ZXMucmFuZ2UgPz8gcHJvZmlsZS5yYW5nZSkpIH1cbiAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHBvaW50LngsIHBvaW50LnkpXG4gIGNvbnN0IGltcGFjdCA9IHJlc29sdmVTeW5lcmdpZXMoeyBzY3JpcHRzOiBbaWRdLCB0ZXJyYWluOiB0aWxlID8gW3RpbGUua2luZF0gOiBbXSB9KVxuICBpZiAoaXRlbS5zcGVsbCA9PT0gJ2VtYmVyJykgY2FzdEVtYmVyKHN0YXRlLCBwb2ludCwgaW1wYWN0LnZhbHVlcy5kYW1hZ2UgPz8gMClcbiAgaWYgKGlzVmVyZGFudFNwZWxsKGl0ZW0uc3BlbGwpKSBjYXN0VmVyZGFudChzdGF0ZSwgaXRlbS5zcGVsbCwgcG9pbnQpXG4gIGlmIChpc0FzdHJhbFNwZWxsKGl0ZW0uc3BlbGwpKSBjYXN0QXN0cmFsKHN0YXRlLCBpdGVtLnNwZWxsLCBwb2ludClcbiAgYW5ub3VuY2VTeW5lcmdpZXMoc3RhdGUsIGdlb21ldHJ5KVxuICBhbm5vdW5jZVN5bmVyZ2llcyhzdGF0ZSwgaW1wYWN0KVxuICBjb25zdCBlZmZlY3QgPSBldmFsdWF0ZUVxdWlwbWVudEVmZmVjdHMoc3RhdGUuaGVybywgJ3RyaWdnZXJlZCcsIHsgdHJpZ2dlcjogJ3NwZWxsJywgc2NyaXB0czogW2lkXSB9KVxuICBjb25zdCB0YWdzID0gdHJhaWxjcmFmdFRhZ3Moc3RhdGUuaGVybykuZmlsdGVyKGlkID0+IGlkID09PSAnYmFya0JpbmRpbmcnIHx8IGlkID09PSAnc3Bpcml0VGhyZWFkJylcbiAgY29uc3QgdHJhaWxjcmFmdCA9IHJlc29sdmVTeW5lcmdpZXMoeyB0YWdzIH0pXG4gIGFubm91bmNlU3luZXJnaWVzKHN0YXRlLCB0cmFpbGNyYWZ0KVxuICBzdGF0ZS5oZXJvLmZvY3VzID0gTWF0aC5taW4oc3RhdGUuaGVyby5tYXhGb2N1cywgc3RhdGUuaGVyby5mb2N1cyArIChlZmZlY3QudmFsdWVzLmZvY3VzID8/IDApICsgKHRyYWlsY3JhZnQudmFsdWVzLmZvY3VzID8/IDApKVxuICBpZiAoY2lyY3VpdCkgeyBzdGF0ZS5oZXJvLmZvY3VzID0gTWF0aC5taW4oc3RhdGUuaGVyby5tYXhGb2N1cywgc3RhdGUuaGVyby5mb2N1cyArIDIpOyBsb2coc3RhdGUsICdBc2ggQ2lyY3VpdCBjb21wbGV0ZXMgdGhlIHRyYXZlcnNhbC10by1DaGFybSByZWxheS4nKSB9XG4gIHJlc29sdmVEZWZlYXRlZEFjdG9ycyhzdGF0ZSlcbiAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgbG9nKHN0YXRlLCBgJHtpdGVtLm5hbWV9IHRha2VzIGVmZmVjdC5gKVxuICByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdzcGVsbCcpXSlcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIHNob3BDaG9pY2Uoc3RhdGU6IFJ1blN0YXRlLCBjb21tYW5kOiBzdHJpbmcpOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBpZCA9IG1lcmNoYW50U3RvY2soc3RhdGUpW051bWJlcihjb21tYW5kKSAtIDFdXG4gIGlmICghaWQpIHJldHVybiBbXVxuICBjb25zdCBpdGVtID0gSVRFTVtpZF1cbiAgaWYgKHN0YXRlLmhlcm8uZ29sZCA8IGl0ZW0udmFsdWUpIHsgbG9nKHN0YXRlLCAnTm90IGVub3VnaCBjYXNoLicpOyByZXR1cm4gW2V2ZW50KCdtZW51JyldIH1cbiAgY29uc3QgYmxvY2tlciA9IHB1cmNoYXNlQmxvY2tlcihzdGF0ZS5oZXJvLCBpZClcbiAgaWYgKGJsb2NrZXIpIHsgbG9nKHN0YXRlLCBibG9ja2VyKTsgcmV0dXJuIFtldmVudCgnbWVudScpXSB9XG4gIGlmIChzdGF0ZS5oZXJvLmludmVudG9yeS5sZW5ndGggPj0gMTIpIHsgbG9nKHN0YXRlLCAnWW91ciBwYWNrIGlzIGZ1bGwuJyk7IHJldHVybiBbZXZlbnQoJ21lbnUnKV0gfVxuICBzcGVuZEdvbGQoc3RhdGUsIGl0ZW0udmFsdWUpXG4gIHJlY29yZFRlbGVtZXRyeUNvdW50KHN0YXRlLCAncHVyY2hhc2VzJywgaWQpXG4gIHN0YXRlLmhlcm8uaW52ZW50b3J5LnB1c2goaWQpXG4gIGxvZyhzdGF0ZSwgYFlvdSBidXkgJHtpdGVtLm5hbWV9LmApXG4gIHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3BpY2t1cCcpXSlcbn1cblxuY29uc3QgcmVjb3ZlclVzZWRJdGVtID0gKHN0YXRlOiBSdW5TdGF0ZSwgaWQ6IHN0cmluZyk6IHZvaWQgPT4ge1xuICBjb25zdCBjaGFuY2UgPSBNYXRoLm1pbig5MCwgYm9vblJhbmsoc3RhdGUsICdzYWx2YWdlcicpICogMjUgKyBib29uUmFuayhzdGF0ZSwgJ2RlZXBQb2NrZXRzJykgKiAzNSlcbiAgaWYgKCFjaGFuY2UgfHwgc3RhdGUuaGVyby5pbnZlbnRvcnkubGVuZ3RoID49IDEyIHx8ICF0dXJuUm5nKHN0YXRlLCAnbG9vdCcsIGByZWNvdmVyOiR7aWR9YCkuY2hhbmNlKGNoYW5jZSkpIHJldHVyblxuICBzdGF0ZS5oZXJvLmludmVudG9yeS5wdXNoKGlkKVxuICBsb2coc3RhdGUsIGAke0lURU1baWRdLm5hbWV9IGlzIHJlY292ZXJlZCBieSB5b3VyIGJ1aWxkLmApXG59XG5cbmZ1bmN0aW9uIHVzZUl0ZW0oc3RhdGU6IFJ1blN0YXRlLCBpZDogc3RyaW5nLCBpbnZlbnRvcnlJbmRleDogbnVtYmVyKTogQWN0aW9uUmVzdWx0IHtcbiAgY29uc3QgaXRlbSA9IElURU1baWRdXG4gIGlmIChpdGVtLnNsb3QpIHJldHVybiBlcXVpcChzdGF0ZSwgaWQsIGludmVudG9yeUluZGV4KVxuICBpZiAoaXRlbS51c2UgPT09ICdoZWFsJykgeyBpZiAoc3RhdGUuaGVyby5vYXRocz8uc29tZShvYXRoID0+IG9hdGguaWQgPT09ICdub0hlYWxpbmcnKSkgeyBsb2coc3RhdGUsICdZb3VyIGFjdGl2ZSBvYXRoIGZvcmJpZHMgaGVhbGluZy4nKTsgcmV0dXJuIFtdIH07IHN0YXRlLmhlcm8uaGVhbHRoID0gTWF0aC5taW4oc3RhdGUuaGVyby5tYXhIZWFsdGgsIHN0YXRlLmhlcm8uaGVhbHRoICsgTWF0aC5tYXgoMSwgMTAgKyB2aXRhbGl0eVJlY292ZXJ5KHN0YXRlLmhlcm8pIC0gYm9vblJhbmsoc3RhdGUsICdxdWlldFBvY2tldCcpIC0gYm9vblJhbmsoc3RhdGUsICdoYXJkTGVzc29uJykpICsgYm9vblJhbmsoc3RhdGUsICdzdG9ybVJhdGlvbnMnKSAqIDIpOyBpZiAoYm9vblJhbmsoc3RhdGUsICdxdWlldFBvY2tldCcpKSBzdGF0ZS5oZXJvLmNvbmRpdGlvbnMgPSBbXTsgY29uc3VtZShzdGF0ZSwgaW52ZW50b3J5SW5kZXgpOyByZWNvdmVyVXNlZEl0ZW0oc3RhdGUsIGlkKTsgbG9nKHN0YXRlLCAnV2FybXRoIHJldHVybnMgdG8geW91ciBsaW1icy4nKTsgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgnc3BlbGwnKV0pIH1cbiAgaWYgKGl0ZW0udXNlID09PSAnZm9jdXMnKSB7IHN0YXRlLmhlcm8uZm9jdXMgPSBNYXRoLm1pbihzdGF0ZS5oZXJvLm1heEZvY3VzLCBzdGF0ZS5oZXJvLmZvY3VzICsgOCk7IGNvbnN1bWUoc3RhdGUsIGludmVudG9yeUluZGV4KTsgcmVjb3ZlclVzZWRJdGVtKHN0YXRlLCBpZCk7IGxvZyhzdGF0ZSwgJ1lvdXIgbWluZCBzaGFycGVucy4nKTsgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgnc3BlbGwnKV0pIH1cbiAgaWYgKGl0ZW0udXNlID09PSAnbWFwJykgeyBmb3IgKGNvbnN0IHRpbGUgb2Ygc3RhdGUuZmxvb3IudGlsZXMpIHRpbGUuZXhwbG9yZWQgPSB0cnVlOyBjb25zdW1lKHN0YXRlLCBpbnZlbnRvcnlJbmRleCk7IGxvZyhzdGF0ZSwgJ1RoZSBmbG9vciBtYXAgdW5mb2xkcyBpbiB5b3VyIG1pbmQuJyk7IHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3NwZWxsJyldKSB9XG4gIGlmIChpdGVtLnVzZSA9PT0gJ3RlbGVwb3J0Jykge1xuICAgIGNvbnN0IGNob2ljZXMgPSBzdGF0ZS5mbG9vci50aWxlcy5mbGF0TWFwKCh0aWxlLCBpKSA9PiB0aWxlLmtpbmQgPT09ICdmbG9vcicgJiYgdGlsZS5leHBsb3JlZCA/IFtmbG9vclBvaW50KHN0YXRlLmZsb29yLCBpKV0gOiBbXSlcbiAgICBpZiAoY2hvaWNlcy5sZW5ndGgpIHsgY29uc3QgdGFyZ2V0ID0gdHVyblJuZyhzdGF0ZSwgJ2NvbWJhdCcsICdibGluaycpLnBpY2soY2hvaWNlcyk7IHN0YXRlLmhlcm8ueCA9IHRhcmdldC54OyBzdGF0ZS5oZXJvLnkgPSB0YXJnZXQueSB9XG4gICAgY29uc3VtZShzdGF0ZSwgaW52ZW50b3J5SW5kZXgpOyByZWZyZXNoRm92KHN0YXRlKTsgbG9nKHN0YXRlLCAnU3BhY2UgZm9sZHMuJyk7IHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3NwZWxsJyldKVxuICB9XG4gIGlmIChpdGVtLnVzZSA9PT0gJ2RyaWxsJykgeyBzdGF0ZS5tb2RhbCA9IHsga2luZDogJ3RhcmdldCcsIGFjdGlvbjogJ2RyaWxsJywgaXRlbTogaWQgfTsgcmV0dXJuIFtldmVudCgnbWVudScpXSB9XG4gIGlmIChpdGVtLnVzZSA9PT0gJ2dsaWRlJykgeyBzdGF0ZS5tb2RhbCA9IHsga2luZDogJ3RhcmdldCcsIGFjdGlvbjogJ2dsaWRlJywgaXRlbTogaWQgfTsgcmV0dXJuIFtldmVudCgnbWVudScpXSB9XG4gIGlmIChpdGVtLnVzZSA9PT0gJ2dyYXBwbGUnKSB7IHN0YXRlLm1vZGFsID0geyBraW5kOiAndGFyZ2V0JywgYWN0aW9uOiAnZ3JhcHBsZScsIGl0ZW06IGlkIH07IHJldHVybiBbZXZlbnQoJ21lbnUnKV0gfVxuICBpZiAoaXRlbS51c2UgPT09ICdicmlkZ2UnKSB7IHN0YXRlLm1vZGFsID0geyBraW5kOiAndGFyZ2V0JywgYWN0aW9uOiAnYnJpZGdlJywgaXRlbTogaWQgfTsgcmV0dXJuIFtldmVudCgnbWVudScpXSB9XG4gIGlmIChpdGVtLnVzZSA9PT0gJ2Rhc2gnKSB7IHN0YXRlLm1vZGFsID0geyBraW5kOiAndGFyZ2V0JywgYWN0aW9uOiAnZGFzaCcsIGl0ZW06IGlkIH07IHJldHVybiBbZXZlbnQoJ21lbnUnKV0gfVxuICBpZiAoaXRlbS51c2UgPT09ICd3aW5jaCcpIHsgc3RhdGUubW9kYWwgPSB7IGtpbmQ6ICd0YXJnZXQnLCBhY3Rpb246ICd3aW5jaCcsIGl0ZW06IGlkIH07IHJldHVybiBbZXZlbnQoJ21lbnUnKV0gfVxuICBpZiAoaXRlbS51c2UgPT09ICdib21iJykgeyBjb25zdCByZXN0b3JlZCA9IHJlc3RvcmVCb21icyhzdGF0ZS5oZXJvLCAzICsgYm9vblJhbmsoc3RhdGUsICdzcGFyZUZ1c2UnKSk7IGlmICghcmVzdG9yZWQpIHsgbG9nKHN0YXRlLCAnWW91ciBib21iIHJlc2VydmUgaXMgZnVsbC4nKTsgcmV0dXJuIFtdIH07IGNvbnN1bWUoc3RhdGUsIGludmVudG9yeUluZGV4KTsgcmVjb3ZlclVzZWRJdGVtKHN0YXRlLCBpZCk7IGxvZyhzdGF0ZSwgYFlvdSBnYWluICR7cmVzdG9yZWR9IGJvbWJzLmApOyByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdwaWNrdXAnKV0pIH1cbiAgaWYgKGl0ZW0udXNlID09PSAncm9wZScpIHsgY29uc3QgcmVzdG9yZWQgPSByZXN0b3JlUm9wZXMoc3RhdGUuaGVybywgMyk7IGlmICghcmVzdG9yZWQpIHsgbG9nKHN0YXRlLCAnWW91ciByb3BlIHJlc2VydmUgaXMgZnVsbC4nKTsgcmV0dXJuIFtdIH07IGNvbnN1bWUoc3RhdGUsIGludmVudG9yeUluZGV4KTsgcmVjb3ZlclVzZWRJdGVtKHN0YXRlLCBpZCk7IGxvZyhzdGF0ZSwgYFlvdSBnYWluICR7cmVzdG9yZWR9IHJvcGVzLmApOyByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdwaWNrdXAnKV0pIH1cbiAgaWYgKGl0ZW0udXNlID09PSAna2V5JykgeyBzdGF0ZS5oZXJvLmtleXMrKzsgY29uc3VtZShzdGF0ZSwgaW52ZW50b3J5SW5kZXgpOyByZXR1cm4gYWR2YW5jZShzdGF0ZSwgW2V2ZW50KCdwaWNrdXAnKV0pIH1cbiAgaWYgKGl0ZW0udXNlID09PSAnc3BlbGwnKSB7IHN0YXRlLm1vZGFsID0geyBraW5kOiAndGFyZ2V0JywgYWN0aW9uOiAnc3BlbGwnLCBpdGVtOiBpZCB9OyByZXR1cm4gW2V2ZW50KCdtZW51JyldIH1cbiAgbG9nKHN0YXRlLCAnVGhhdCBjYW5ub3QgYmUgdXNlZCBoZXJlLicpXG4gIHJldHVybiBbXVxufVxuXG5mdW5jdGlvbiBlcXVpcChzdGF0ZTogUnVuU3RhdGUsIGlkOiBzdHJpbmcsIGluZGV4OiBudW1iZXIpOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBpdGVtID0gSVRFTVtpZF1cbiAgaWYgKCFpdGVtLnNsb3QpIHsgbG9nKHN0YXRlLCAnVGhhdCBjYW5ub3QgYmUgZXF1aXBwZWQuJyk7IHJldHVybiBbXSB9XG4gIGNvbnN0IHByZXZpb3VzID0gc3RhdGUuaGVyby5lcXVpcG1lbnRbaXRlbS5zbG90XVxuICBzdGF0ZS5oZXJvLmludmVudG9yeS5zcGxpY2UoaW5kZXgsIDEpXG4gIGlmIChwcmV2aW91cykgeyBzdGF0ZS5oZXJvLmludmVudG9yeS5wdXNoKHByZXZpb3VzKTsgc3RhdGUuaGVyby5sYXN0VW5lcXVpcHBlZCA9IHByZXZpb3VzIH1cbiAgc3RhdGUuaGVyby5lcXVpcG1lbnRbaXRlbS5zbG90XSA9IGlkXG4gIGxvZyhzdGF0ZSwgYFlvdSBlcXVpcCAke2l0ZW0ubmFtZX0uYClcbiAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncGlja3VwJyldKVxufVxuXG5leHBvcnQgZnVuY3Rpb24gc3dhcChzdGF0ZTogUnVuU3RhdGUpOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBpZCA9IHN0YXRlLmhlcm8ubGFzdFVuZXF1aXBwZWRcbiAgaWYgKCFpZCB8fCBzdGF0ZS5oZXJvLmludmVudG9yeS5sZW5ndGggPj0gMTIpIHsgbG9nKHN0YXRlLCAnTm8gaXRlbSBpcyByZWFkeSB0byBzd2FwLicpOyByZXR1cm4gW10gfVxuICBzdGF0ZS5oZXJvLmludmVudG9yeS5wdXNoKGlkKVxuICBzdGF0ZS5oZXJvLmxhc3RVbmVxdWlwcGVkID0gdW5kZWZpbmVkXG4gIGxvZyhzdGF0ZSwgJ1lvdSBzdG93IHlvdXIgbGFzdCB1bmVxdWlwcGVkIGl0ZW0uJylcbiAgcmV0dXJuIGFkdmFuY2Uoc3RhdGUsIFtldmVudCgncGlja3VwJyldKVxufVxuXG5jb25zdCBuZWFyYnlDb250YWluZXIgPSAoc3RhdGU6IFJ1blN0YXRlKTogeyB0aWxlOiBOb25OdWxsYWJsZTxSZXR1cm5UeXBlPHR5cGVvZiBnZXRUaWxlPj47IGtpbmQ6ICdjcmF0ZScgfCAnY2hlc3QnOyB4OiBudW1iZXI7IHk6IG51bWJlciB9IHwgdW5kZWZpbmVkID0+IHtcbiAgZm9yIChjb25zdCBkZWx0YSBvZiBPYmplY3QudmFsdWVzKERJUkVDVElPTlMpKSB7XG4gICAgY29uc3QgeCA9IHN0YXRlLmhlcm8ueCArIGRlbHRhLnhcbiAgICBjb25zdCB5ID0gc3RhdGUuaGVyby55ICsgZGVsdGEueVxuICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCB4LCB5KVxuICAgIGlmICh0aWxlPy5raW5kID09PSAnY3JhdGUnIHx8IHRpbGU/LmtpbmQgPT09ICdjaGVzdCcpIHJldHVybiB7IHRpbGUsIGtpbmQ6IHRpbGUua2luZCwgeCwgeSB9XG4gIH1cbiAgcmV0dXJuIHVuZGVmaW5lZFxufVxuXG5jb25zdCBuZWFyYnlHYXRlID0gKHN0YXRlOiBSdW5TdGF0ZSk6IGJvb2xlYW4gPT4gT2JqZWN0LnZhbHVlcyhESVJFQ1RJT05TKS5zb21lKGRlbHRhID0+IGdldFRpbGUoc3RhdGUuZmxvb3IsIHN0YXRlLmhlcm8ueCArIGRlbHRhLngsIHN0YXRlLmhlcm8ueSArIGRlbHRhLnkpPy5raW5kID09PSAnbG9ja2VkRG9vcicpXG5jb25zdCBuZWFyYnlMb2NrZWREb29yID0gKHN0YXRlOiBSdW5TdGF0ZSk6IE5vbk51bGxhYmxlPFJldHVyblR5cGU8dHlwZW9mIGdldFRpbGU+PiB8IHVuZGVmaW5lZCA9PiBPYmplY3QudmFsdWVzKERJUkVDVElPTlMpLm1hcChkZWx0YSA9PiBnZXRUaWxlKHN0YXRlLmZsb29yLCBzdGF0ZS5oZXJvLnggKyBkZWx0YS54LCBzdGF0ZS5oZXJvLnkgKyBkZWx0YS55KSkuZmluZCh0aWxlID0+IHRpbGU/LmtpbmQgPT09ICdsb2NrZWREb29yJylcbiJdLCJtYXBwaW5ncyI6IkFBQUEsU0FBU0EsSUFBSSxFQUFFQyxTQUFTLFFBQVEsWUFBWTtBQUM1QyxTQUFTQyxVQUFVLEVBQUVDLFVBQVUsUUFBb0UsVUFBVTtBQUM3RyxTQUFTQyxPQUFPLEVBQUVDLGlCQUFpQixFQUFFQyxPQUFPLEVBQUVDLFVBQVUsUUFBUSxVQUFVO0FBQzFFLFNBQVNDLGlCQUFpQixFQUFFQyxlQUFlLEVBQUVDLGtCQUFrQixRQUFRLGVBQWU7QUFDdEYsU0FBU0MsT0FBTyxFQUFFQyxPQUFPLEVBQUVDLHFCQUFxQixRQUFRLFVBQVU7QUFDbEUsU0FBU0MsaUJBQWlCLFFBQVEsZUFBZTtBQUNqRCxTQUFTQyxZQUFZLEVBQUVDLG9CQUFvQixRQUFRLGNB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