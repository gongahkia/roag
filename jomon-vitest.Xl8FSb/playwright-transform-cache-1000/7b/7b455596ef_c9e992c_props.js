// 9e858079a44de637c929dd100adcf403df74ee23
import { DIRECTIONS, floorPoint } from '../types';
import { isBlockingProp, propAt, propDefinition } from '../props';
import { actorAt, getTile, hasPassablePath, isPassable, preservesExitPath, spawnMonster } from '../world';
import { damageHero, explode, resolveDefeatedActors } from './combat';
import { addCondition, modifyIncomingDamage } from './conditions';
import { event, log } from './shared';
const pointKey = point => `${point.x},${point.y}`;
const mineProp = prop => prop.biome === 'mine' && prop.kind.startsWith('mine.');
const wildsProp = prop => prop.biome === 'wilds' && prop.kind.startsWith('wilds.');
const cavernProp = prop => prop.biome === 'caverns' && prop.kind.startsWith('caverns.');
const ruinsProp = prop => prop.biome === 'ruins' && prop.kind.startsWith('ruins.');
const furnaceProp = prop => prop.biome === 'furnace' && prop.kind.startsWith('furnace.');
const floodedProp = prop => prop.biome === 'floodedRuins' && prop.kind.startsWith('floodedRuins.');
const cardinal = point => Math.abs(point.x) + Math.abs(point.y) === 1;
const hazardKinds = new Set(['spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'gas', 'lava', 'pit']);
const reward = (state, prop, id, count = 1) => {
  state.floor.items.push({
    id,
    x: prop.x,
    y: prop.y,
    count,
    visibleInFog: true
  });
};
const nearbyPoints = (point, radius = 1) => {
  const points = [];
  for (let y = point.y - radius; y <= point.y + radius; y++) for (let x = point.x - radius; x <= point.x + radius; x++) if (x !== point.x || y !== point.y) points.push({
    x,
    y
  });
  return points.sort((first, second) => Math.max(Math.abs(first.x - point.x), Math.abs(first.y - point.y)) - Math.max(Math.abs(second.x - point.x), Math.abs(second.y - point.y)) || first.y - second.y || first.x - second.x);
};
const revealLocal = (state, point, radius = 3) => {
  let revealed = 0;
  for (const candidate of nearbyPoints(point, radius)) {
    const tile = getTile(state.floor, candidate.x, candidate.y);
    if (!tile || tile.explored) continue;
    tile.explored = true;
    revealed++;
  }
  return revealed;
};
const clearBramble = (state, point) => {
  const candidate = nearbyPoints(point, 2).find(current => {
    var _getTile;
    return ((_getTile = getTile(state.floor, current.x, current.y)) === null || _getTile === void 0 ? void 0 : _getTile.kind) === 'bramble';
  });
  if (!candidate) return false;
  getTile(state.floor, candidate.x, candidate.y).kind = 'floor';
  return true;
};
const growBramble = (state, point) => {
  const grown = [];
  for (const candidate of nearbyPoints(point)) {
    const tile = getTile(state.floor, candidate.x, candidate.y);
    if (!tile || tile.kind !== 'floor' || actorAt(state.floor, candidate.x, candidate.y) || state.hero.x === candidate.x && state.hero.y === candidate.y) continue;
    if (!preservesExitPath(state.floor, state.floor.start, candidate, 'bramble')) continue;
    tile.kind = 'bramble';
    grown.push(candidate);
    if (grown.length === 2) break;
  }
  return grown;
};
const clearEffectCells = (state, prop) => {
  for (const point of (_prop$effectCells = prop.effectCells) !== null && _prop$effectCells !== void 0 ? _prop$effectCells : []) {
    var _prop$effectCells;
    const tile = getTile(state.floor, point.x, point.y);
    if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'bramble') tile.kind = 'floor';
    if (prop.kind === 'caverns.barnacledShrine' && (tile === null || tile === void 0 ? void 0 : tile.kind) === 'water') tile.kind = 'floor';
  }
  prop.effectCells = undefined;
};
const floodBrine = (state, point) => {
  const flooded = [];
  for (const candidate of nearbyPoints(point)) {
    const tile = getTile(state.floor, candidate.x, candidate.y);
    if (!tile || tile.kind !== 'floor' || actorAt(state.floor, candidate.x, candidate.y) || state.hero.x === candidate.x && state.hero.y === candidate.y) continue;
    tile.kind = 'water';
    flooded.push(candidate);
    if (flooded.length === 2) break;
  }
  return flooded;
};
const disturbEelTunnel = (state, prop) => {
  const candidate = nearbyPoints(prop, 3).find(point => Math.max(Math.abs(point.x - state.hero.x), Math.abs(point.y - state.hero.y)) > 1 && isPassable(state.floor, point.x, point.y) && !actorAt(state.floor, point.x, point.y));
  if (!candidate) return false;
  state.floor.actors.push(spawnMonster('fumeeel', candidate, `${prop.id}:eel`));
  return true;
};
const destroyCrystal = (state, prop, source) => {
  prop.state = 'destroyed';
  const tile = getTile(state.floor, prop.x, prop.y);
  if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor' && preservesExitPath(state.floor, state.floor.start, prop, 'rubble')) tile.kind = 'rubble';
  reward(state, prop, 'rock', 2);
  log(state, source === 'pickaxe' ? 'You mine crystal shards and leave safe rubble.' : 'The blast shatters the crystal into safe rubble.');
};
const consumeCavernCacheKey = state => {
  if (state.hero.keys > 0) {
    state.hero.keys--;
    return 'key';
  }
  if (state.hero.bombs > 0) {
    state.hero.bombs--;
    return 'bomb';
  }
  if (state.hero.equipment.charm === 'sunseal') {
    state.hero.equipment.charm = undefined;
    return 'sunseal';
  }
  const ward = state.hero.inventory.indexOf('wardScript');
  if (ward >= 0) {
    state.hero.inventory.splice(ward, 1);
    return 'wardScript';
  }
  return undefined;
};
const canSealEelTunnel = (state, tunnel) => {
  const previous = tunnel.state;
  tunnel.state = 'activated';
  const safe = hasPassablePath(state.floor, state.hero, state.floor.exit) && hasPassablePath(state.floor, state.floor.start, state.floor.exit);
  tunnel.state = previous;
  return safe;
};
const armMonolith = (state, prop, source) => {
  prop.state = 'activated';
  prop.effectCells = [{
    x: prop.x,
    y: prop.y
  }, ...nearbyPoints(prop, 2)].map(point => ({
    ...point
  }));
  prop.expiresAt = state.turn + 4;
  addCondition(state.hero, {
    kind: 'marked',
    duration: 3,
    potency: 1
  });
  log(state, `${source} arms the monolith's unstable local ward for four turns.`);
};
const consumeRuinsCacheKey = state => consumeCavernCacheKey(state);
const disturbNest = (state, prop) => {
  const candidate = nearbyPoints(prop, 3).find(point => Math.max(Math.abs(point.x - state.hero.x), Math.abs(point.y - state.hero.y)) > 1 && isPassable(state.floor, point.x, point.y) && !actorAt(state.floor, point.x, point.y));
  if (!candidate) return false;
  state.floor.actors.push(spawnMonster('startledBirds', candidate, `${prop.id}:flock`));
  return true;
};
const consumeWildsCharm = state => {
  const id = state.hero.inventory.find(item => item === 'root' || item === 'mend' || item === 'wardScript');
  if (!id) return undefined;
  state.hero.inventory.splice(state.hero.inventory.indexOf(id), 1);
  return id;
};
const nearbyProp = state => {
  const points = [{
    x: state.hero.x,
    y: state.hero.y
  }, ...Object.values(DIRECTIONS).filter(delta => delta.x || delta.y).map(delta => ({
    x: state.hero.x + delta.x,
    y: state.hero.y + delta.y
  }))];
  return points.map(point => propAt(state.floor.props, point.x, point.y)).find(prop => Boolean(prop));
};
const inspect = (state, prop, followup) => {
  prop.state = 'inspected';
  const definition = propDefinition(prop.kind);
  log(state, `You examine the ${definition.name}: ${definition.description} ${followup}`);
  return {
    kind: 'examined',
    events: []
  };
};
const cartCandidates = (state, cart, previous) => {
  const choices = [{
    x: 0,
    y: -1
  }, {
    x: 1,
    y: 0
  }, {
    x: 0,
    y: 1
  }, {
    x: -1,
    y: 0
  }].map(delta => ({
    x: cart.x + delta.x,
    y: cart.y + delta.y
  })).filter(point => {
    var _getTile2;
    return ((_getTile2 = getTile(state.floor, point.x, point.y)) === null || _getTile2 === void 0 ? void 0 : _getTile2.kind) === 'rail';
  }).filter(point => !previous || point.x !== previous.x || point.y !== previous.y).filter(point => !isBlockingProp(propAt(state.floor.props, point.x, point.y)));
  return choices;
};
const triggerCartHazards = (state, cart) => {
  const events = [];
  for (const delta of Object.values(DIRECTIONS)) {
    const tile = getTile(state.floor, cart.x + delta.x, cart.y + delta.y);
    if ((tile === null || tile === void 0 ? void 0 : tile.kind) !== 'dart' && (tile === null || tile === void 0 ? void 0 : tile.kind) !== 'fireVent') continue;
    const hazard = tile.kind;
    tile.kind = 'floor';
    log(state, `The cart triggers the ${hazard === 'dart' ? 'dart' : 'fire'} trap.`);
    events.push(event('danger'));
  }
  return events;
};
const moveCart = (state, cart, first) => {
  var _getTile3;
  const events = [];
  if (!cardinal(first) || ((_getTile3 = getTile(state.floor, cart.x + first.x, cart.y + first.y)) === null || _getTile3 === void 0 ? void 0 : _getTile3.kind) !== 'rail') {
    log(state, 'The cart has no rail in that direction.');
    return [];
  }
  let direction = {
    ...first
  };
  let previous = {
    x: cart.x,
    y: cart.y
  };
  while (true) {
    var _getTile4;
    const next = {
      x: cart.x + direction.x,
      y: cart.y + direction.y
    };
    if (((_getTile4 = getTile(state.floor, next.x, next.y)) === null || _getTile4 === void 0 ? void 0 : _getTile4.kind) !== 'rail' || isBlockingProp(propAt(state.floor.props, next.x, next.y))) break;
    const actor = actorAt(state.floor, next.x, next.y);
    if (actor) {
      if (!actor.hostile) {
        log(state, `The cart is blocked by ${actor.name}.`);
        break;
      }
      actor.health -= modifyIncomingDamage(actor, 8);
      log(state, `The cart crushes ${actor.name}.`);
      events.push(event('hit'));
      break;
    }
    if (state.hero.x === next.x && state.hero.y === next.y) {
      events.push(...damageHero(state, 8, 'The rail cart', true));
      break;
    }
    cart.x = next.x;
    cart.y = next.y;
    if (!events.some(entry => entry.type === 'move')) events.push(event('move'));
    events.push(...triggerCartHazards(state, cart));
    const options = cartCandidates(state, cart, previous);
    const straight = options.find(point => point.x - cart.x === direction.x && point.y - cart.y === direction.y);
    if (straight) {
      previous = {
        x: cart.x,
        y: cart.y
      };
      continue;
    }
    if (options.length !== 1) break;
    direction = {
      x: options[0].x - cart.x,
      y: options[0].y - cart.y
    };
    previous = {
      x: cart.x,
      y: cart.y
    };
  }
  resolveDefeatedActors(state);
  log(state, 'The cart grinds to a halt.');
  return events;
};
const revealWarnings = (state, prop, skull) => {
  const points = state.floor.tiles.flatMap((tile, index) => hazardKinds.has(tile.kind) ? [{
    ...floorPoint(state.floor, index),
    priority: 0
  }] : []);
  if (skull) points.push(...state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => ({
    x: actor.x,
    y: actor.y,
    priority: 1
  })));
  const warnings = points.filter(point => Math.max(Math.abs(point.x - prop.x), Math.abs(point.y - prop.y)) <= 5).sort((first, second) => first.priority - second.priority || Math.max(Math.abs(first.x - prop.x), Math.abs(first.y - prop.y)) - Math.max(Math.abs(second.x - prop.x), Math.abs(second.y - prop.y)) || first.y - second.y || first.x - second.x).slice(0, 3);
  for (const point of warnings) getTile(state.floor, point.x, point.y).explored = true;
  return warnings.length;
};
const operateMineProp = (state, prop) => {
  if (prop.kind === 'mine.oreVein') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Equip a pickaxe, then press C to mine it.');
    if (prop.state !== 'inspected') {
      log(state, 'The vein has already been worked.');
      return {
        kind: 'examined',
        events: []
      };
    }
    if (state.hero.equipment.mainHand !== 'pickaxe') {
      log(state, 'A pickaxe is required to work this vein.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'destroyed';
    const tile = getTile(state.floor, prop.x, prop.y);
    if (tile) tile.kind = 'rubble';
    reward(state, prop, 'rock', 2);
    log(state, 'You chip ore free and leave a mound of rubble.');
    return {
      kind: 'activated',
      events: [event('pickup'), event('boom')]
    };
  }
  if (prop.kind === 'mine.lanternPost') {
    if (prop.state === 'dormant') return inspect(state, prop, 'An Ember Charm or Fire Jar can relight it.');
    log(state, prop.state === 'activated' ? 'The lantern post burns with a steady local glow.' : 'The lantern needs flame, not a hand.');
    return {
      kind: 'examined',
      events: []
    };
  }
  if (prop.kind === 'mine.brokenCart') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Stand beside it on a rail, then press C to push it.');
    const direction = {
      x: Math.sign(prop.x - state.hero.x),
      y: Math.sign(prop.y - state.hero.y)
    };
    if (!cardinal(direction)) {
      log(state, 'Stand on a cardinal side of the cart to push it.');
      return {
        kind: 'examined',
        events: []
      };
    }
    const events = moveCart(state, prop, direction);
    if (!events.length) return {
      kind: 'examined',
      events
    };
    prop.state = 'activated';
    return {
      kind: 'moved',
      events
    };
  }
  if (prop.kind === 'mine.warningMarker' || prop.kind === 'mine.skullMarker') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to trace its local warning.');
    if (prop.state === 'activated') {
      log(state, 'The warning has already been traced.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    const count = revealWarnings(state, prop, prop.kind === 'mine.skullMarker');
    log(state, count ? `The marker exposes ${count} nearby danger${count === 1 ? '' : 's'}.` : 'The marker points to no nearby danger.');
    return {
      kind: 'activated',
      events: [event('danger')]
    };
  }
  if (prop.kind === 'mine.discardedParcel') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to recover it, or leave it untouched.');
    if (prop.state === 'activated') {
      log(state, 'The parcel has already been recovered.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    reward(state, prop, propDefinition(prop.kind).activationReward);
    log(state, 'You recover the parcel before its charge can flare.');
    return {
      kind: 'activated',
      events: [event('pickup')]
    };
  }
  return undefined;
};
const operateWildsProp = (state, prop) => {
  if (prop.kind === 'wilds.mushrooms') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Harvest them with C, burn them for flame, or crush them to release spores.');
    if (prop.state === 'activated') {
      log(state, 'The mushroom patch has already been harvested.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    reward(state, prop, 'tonic');
    log(state, 'You harvest a vital tonic and leave the spores undisturbed.');
    return {
      kind: 'activated',
      events: [event('pickup')]
    };
  }
  if (prop.kind === 'wilds.danglingCharm') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to take its ward, or use a Brush Blade, Root, or fire to cut the nearby growth.');
    if (prop.state === 'activated') {
      log(state, 'The branch hangs bare.');
      return {
        kind: 'examined',
        events: []
      };
    }
    if (state.hero.equipment.mainHand === 'machete') {
      prop.state = 'destroyed';
      const cleared = clearBramble(state, prop);
      log(state, cleared ? 'You cut the charm loose and clear a bramble choke point.' : 'You cut the charm loose before its curse can take hold.');
      return {
        kind: 'activated',
        events: [event('move')]
      };
    }
    prop.state = 'activated';
    reward(state, prop, 'wardScript');
    log(state, 'You take the charm as a ward and leave the roots untouched.');
    return {
      kind: 'activated',
      events: [event('pickup')]
    };
  }
  if (prop.kind === 'wilds.birdNest') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to loot it, or throw or burn it to startle the flock.');
    if (prop.state === 'activated') {
      log(state, 'The nest is empty.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    reward(state, prop, 'tonic');
    log(state, 'You take a tonic without disturbing the nest.');
    return {
      kind: 'activated',
      events: [event('pickup')]
    };
  }
  if (prop.kind === 'wilds.rootShrine') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Offer a Root, Mending, or Ward Charm with C to raise a short-lived thorn screen.');
    if (prop.state === 'activated') {
      log(state, 'The shrine has spent its roots.');
      return {
        kind: 'examined',
        events: []
      };
    }
    const charm = consumeWildsCharm(state);
    if (!charm) {
      log(state, 'The shrine answers only Root, Mending, or Ward Charms.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    const grown = growBramble(state, prop);
    prop.effectCells = grown.map(point => ({
      ...point
    }));
    prop.expiresAt = state.turn + 4;
    if (charm === 'mend') state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 6);
    if (charm === 'wardScript') addCondition(state.hero, {
      kind: 'shielded',
      duration: 4,
      potency: 1
    });
    log(state, `The ${charm === 'wardScript' ? 'ward' : charm} charm raises ${grown.length || 'a'} thorn screen beside the shrine.`);
    return {
      kind: 'activated',
      events: [event('spell')]
    };
  }
  if (prop.kind === 'wilds.lostParcel') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to recover rope and cut a nearby bramble route, or leave the parcel for later.');
    if (prop.state === 'activated') {
      log(state, 'The lost parcel has already been recovered.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    reward(state, prop, 'ropeBundle');
    const cleared = clearBramble(state, prop);
    log(state, cleared ? 'You recover rope and uncover a clear alternate trail.' : 'You recover rope from the lost parcel.');
    return {
      kind: 'activated',
      events: [event('pickup')]
    };
  }
  if (prop.kind === 'wilds.rootArch') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Use a Brush Blade with C, Root to open a living detour, or burn through the arch.');
    if (prop.state === 'activated') {
      log(state, 'The roots hold open a clear passage.');
      return {
        kind: 'examined',
        events: []
      };
    }
    if (state.hero.equipment.mainHand !== 'machete') {
      log(state, 'A Brush Blade, Root Charm, or flame can open the living arch.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    log(state, 'You cut a bounded passage through the living roots.');
    return {
      kind: 'activated',
      events: [event('move')]
    };
  }
  return undefined;
};
const operateCavernProp = (state, prop) => {
  if (prop.kind === 'caverns.crystalCluster') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Mine it with an Obsidian Axe, blast it for shards, or use force to refract line effects.');
    if (prop.state === 'activated') {
      log(state, 'The crystal refracts sight and line effects through its opened facets.');
      return {
        kind: 'examined',
        events: []
      };
    }
    if (state.hero.equipment.mainHand !== 'pickaxe') {
      log(state, 'An Obsidian Axe is required to mine the crystal safely.');
      return {
        kind: 'examined',
        events: []
      };
    }
    destroyCrystal(state, prop, 'pickaxe');
    return {
      kind: 'activated',
      events: [event('pickup'), event('boom')]
    };
  }
  if (prop.kind === 'caverns.glowingFungus') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Harvest it with C to lose its light, wet it to brighten the cave, or burn it to release spores.');
    if (prop.state === 'activated') {
      log(state, 'The soaked fungus spills a broad, cold local glow.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'destroyed';
    reward(state, prop, 'focusTonic');
    log(state, 'You harvest the fungus for focus and extinguish its local glow.');
    return {
      kind: 'activated',
      events: [event('pickup')]
    };
  }
  if (prop.kind === 'caverns.barnacledShrine') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Offer a Vital or Focus Tonic with C for a temporary brine ward, or wet the shrine directly.');
    if (prop.state === 'activated') {
      log(state, 'The brine ward still shimmers around the shrine.');
      return {
        kind: 'examined',
        events: []
      };
    }
    const offering = state.hero.inventory.find(item => item === 'tonic' || item === 'focusTonic');
    if (!offering) {
      log(state, 'The shrine asks for a Vital or Focus Tonic.');
      return {
        kind: 'examined',
        events: []
      };
    }
    state.hero.inventory.splice(state.hero.inventory.indexOf(offering), 1);
    prop.state = 'activated';
    const flooded = floodBrine(state, prop);
    prop.effectCells = flooded.map(point => ({
      ...point
    }));
    prop.expiresAt = state.turn + 4;
    addCondition(state.hero, {
      kind: 'shielded',
      duration: 4,
      potency: 1
    });
    log(state, `The brine offering raises a ward and floods ${flooded.length || 'a'} nearby channel.`);
    return {
      kind: 'activated',
      events: [event('spell')]
    };
  }
  if (prop.kind === 'caverns.brokenBoat') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press R beside the boat to spend a rope and anchor this water crossing.');
    if (prop.state === 'activated') {
      log(state, 'The anchored boat holds a safe crossing.');
      return {
        kind: 'examined',
        events: []
      };
    }
    log(state, 'The boat is ready for a rope anchor.');
    return {
      kind: 'examined',
      events: []
    };
  }
  if (prop.kind === 'caverns.eelTunnel') {
    if (prop.state === 'dormant') return inspect(state, prop, 'The open tunnel is a shortcut and a fume-eel origin. Press C again with a bomb to seal it; force reopens it.');
    if (prop.state === 'activated') {
      log(state, 'The eel tunnel is sealed; force can reopen the shortcut.');
      return {
        kind: 'examined',
        events: []
      };
    }
    if (state.hero.bombs < 1) {
      log(state, 'A bomb is required to seal the eel tunnel.');
      return {
        kind: 'examined',
        events: []
      };
    }
    if (!canSealEelTunnel(state, prop)) {
      log(state, 'Sealing this tunnel would close the required trail.');
      return {
        kind: 'examined',
        events: []
      };
    }
    state.hero.bombs--;
    prop.state = 'activated';
    log(state, 'You seal the eel tunnel and close its shortcut.');
    return {
      kind: 'activated',
      events: [event('boom')]
    };
  }
  if (prop.kind === 'caverns.sealedParcel') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to spend a key, bomb, Sunstone Seal, or Ward Charm and open the cache.');
    if (prop.state === 'activated') {
      log(state, 'The sealed parcel has already been opened.');
      return {
        kind: 'examined',
        events: []
      };
    }
    const key = consumeCavernCacheKey(state);
    if (!key) {
      log(state, 'The wax seal resists: bring a key, bomb, Sunstone Seal, or Ward Charm.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    reward(state, prop, 'focusTonic');
    log(state, `You spend ${key === 'wardScript' ? 'a Ward Charm' : key === 'sunseal' ? 'the Sunstone Seal' : `a ${key}`} to open the parcel.`);
    return {
      kind: 'activated',
      events: [event('pickup')]
    };
  }
  return undefined;
};
const operateRuinsProp = (state, prop) => {
  if (prop.kind === 'ruins.brokenStatue') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C to topple it into sight-blocking cover, use force to shove it, or blast it into safe rubble.');
    if (prop.state === 'activated') {
      log(state, 'The toppled statue still blocks line effects as cover.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    log(state, 'You topple the statue into a line-blocking cover position.');
    return {
      kind: 'activated',
      events: [event('move')]
    };
  }
  if (prop.kind === 'ruins.ritualBrazier') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Fuel it with an Ember Charm using C, quench it with water, or target it with Ward or Gate; each spell exposes a local cost.');
    if (prop.state === 'activated') {
      log(state, 'The ritual brazier burns with a warded local flame.');
      return {
        kind: 'examined',
        events: []
      };
    }
    const ember = state.hero.inventory.indexOf('ember');
    if (ember < 0) {
      log(state, 'An Ember Charm is required to fuel the brazier.');
      return {
        kind: 'examined',
        events: []
      };
    }
    state.hero.inventory.splice(ember, 1);
    prop.state = 'activated';
    const tile = getTile(state.floor, prop.x, prop.y);
    if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor') tile.kind = 'fireVent';
    log(state, 'You fuel the brazier; its flame is now a visible local hazard.');
    return {
      kind: 'activated',
      events: [event('spell')]
    };
  }
  if (prop.kind === 'ruins.glyphTablet') {
    var _state$floor$telegrap, _state$floor$puzzleId;
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to read nearby telegraph timing and the floor puzzle rule.');
    if (prop.state === 'activated') {
      log(state, 'The tablet has already yielded its tactical warning.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    const threats = ((_state$floor$telegrap = state.floor.telegraphs) !== null && _state$floor$telegrap !== void 0 ? _state$floor$telegrap : []).filter(telegraph => {
      var _telegraph$cells$, _telegraph$cells$2;
      return Math.max(Math.abs(((_telegraph$cells$ = telegraph.cells[0]) === null || _telegraph$cells$ === void 0 ? void 0 : _telegraph$cells$.x) - prop.x), Math.abs(((_telegraph$cells$2 = telegraph.cells[0]) === null || _telegraph$cells$2 === void 0 ? void 0 : _telegraph$cells$2.y) - prop.y)) <= 6;
    });
    const timing = threats.length ? threats.map(telegraph => `${telegraph.actionId} in ${Math.max(0, telegraph.resolveTurn - state.turn)}`).join(', ') : 'no active nearby telegraphs';
    const puzzle = ((_state$floor$puzzleId = state.floor.puzzleIds) === null || _state$floor$puzzleId === void 0 ? void 0 : _state$floor$puzzleId.join(', ')) || 'no floor puzzle marker';
    log(state, `Glyph tablet: ${timing}; puzzle rule: ${puzzle}.`);
    return {
      kind: 'activated',
      events: [event('danger')]
    };
  }
  if (prop.kind === 'ruins.collapsedArch') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Use an Obsidian Axe with C, a bomb, or a rope brace to open this blocked route.');
    if (prop.state === 'activated') {
      log(state, 'The collapsed arch is braced open.');
      return {
        kind: 'examined',
        events: []
      };
    }
    if (state.hero.equipment.mainHand !== 'pickaxe') {
      log(state, 'An Obsidian Axe, bomb, or rope can open the collapsed arch.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    log(state, 'You cut a stable passage through the collapsed arch.');
    return {
      kind: 'activated',
      events: [event('boom')]
    };
  }
  if (prop.kind === 'ruins.sealedCache') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to spend a key, bomb, Sunstone Seal, or Ward Charm on this visible lock.');
    if (prop.state === 'activated') {
      log(state, 'The sealed cache has already been opened.');
      return {
        kind: 'examined',
        events: []
      };
    }
    const key = consumeRuinsCacheKey(state);
    if (!key) {
      log(state, 'The cache lock needs a key, bomb, Sunstone Seal, or Ward Charm.');
      return {
        kind: 'examined',
        events: []
      };
    }
    prop.state = 'activated';
    reward(state, prop, 'sunseal');
    log(state, `You spend ${key === 'wardScript' ? 'a Ward Charm' : key === 'sunseal' ? 'the Sunstone Seal' : `a ${key}`} and claim the sealed cache.`);
    return {
      kind: 'activated',
      events: [event('pickup')]
    };
  }
  if (prop.kind === 'ruins.monolith') {
    if (prop.state === 'dormant') return inspect(state, prop, 'Spend 2 focus with C to arm a four-turn ward that absorbs one nearby telegraph but marks you.');
    if (prop.state === 'activated') {
      log(state, 'The monolith is armed; its ward will absorb one local telegraph at a cost.');
      return {
        kind: 'examined',
        events: []
      };
    }
    if (state.hero.focus < 2) {
      log(state, 'The monolith requires 2 focus to invoke.');
      return {
        kind: 'examined',
        events: []
      };
    }
    state.hero.focus -= 2;
    armMonolith(state, prop, 'Focus');
    return {
      kind: 'activated',
      events: [event('spell'), event('danger')]
    };
  }
  return undefined;
};
export const operateProp = state => {
  var _prop$hooks;
  const prop = nearbyProp(state);
  if (!prop || !((_prop$hooks = prop.hooks) !== null && _prop$hooks !== void 0 && _prop$hooks.includes('operate'))) return undefined;
  if (mineProp(prop)) return operateMineProp(state, prop);
  if (wildsProp(prop)) return operateWildsProp(state, prop);
  if (cavernProp(prop)) return operateCavernProp(state, prop);
  if (ruinsProp(prop)) return operateRuinsProp(state, prop);
  const definition = propDefinition(prop.kind);
  if (prop.state === 'dormant') return inspect(state, prop, 'Press C again to activate it.');
  if (prop.state === 'activated') {
    log(state, `The ${definition.name} has already been activated.`);
    return {
      kind: 'examined',
      events: []
    };
  }
  if (prop.state !== 'inspected') return undefined;
  prop.state = 'activated';
  reward(state, prop, definition.activationReward);
  log(state, `You study the ${definition.name} and recover ${definition.activationReward}.`);
  return {
    kind: 'activated',
    events: [event('pickup')]
  };
};
export const moveCartByForce = (state, point, pull) => {
  const cart = propAt(state.floor.props, point.x, point.y);
  if ((cart === null || cart === void 0 ? void 0 : cart.kind) !== 'mine.brokenCart' || cart.state === 'destroyed') return undefined;
  const away = {
    x: Math.sign(cart.x - state.hero.x),
    y: Math.sign(cart.y - state.hero.y)
  };
  const direction = pull ? {
    x: -away.x,
    y: -away.y
  } : away;
  if (!cardinal(direction)) {
    log(state, 'The cart cannot find a rail-aligned force path.');
    return [];
  }
  const events = moveCart(state, cart, direction);
  if (events.length) cart.state = 'activated';
  return events;
};
export const releaseCartWithRope = state => {
  const cart = Object.values(DIRECTIONS).filter(cardinal).map(delta => propAt(state.floor.props, state.hero.x + delta.x, state.hero.y + delta.y)).find(prop => (prop === null || prop === void 0 ? void 0 : prop.kind) === 'mine.brokenCart' && prop.state !== 'destroyed');
  if (!cart) return undefined;
  if (cart.state === 'dormant') {
    log(state, 'Examine the cart before rigging it with a rope.');
    return [];
  }
  const direction = {
    x: Math.sign(cart.x - state.hero.x),
    y: Math.sign(cart.y - state.hero.y)
  };
  const events = moveCart(state, cart, direction);
  if (events.length) cart.state = 'activated';
  return events;
};
export const anchorBoatWithRope = state => {
  const boat = Object.values(DIRECTIONS).filter(cardinal).map(delta => propAt(state.floor.props, state.hero.x + delta.x, state.hero.y + delta.y)).find(prop => (prop === null || prop === void 0 ? void 0 : prop.kind) === 'caverns.brokenBoat' && prop.state !== 'destroyed');
  if (!boat) return undefined;
  if (boat.state === 'dormant') {
    log(state, 'Examine the boat before anchoring it.');
    return [];
  }
  if (boat.state === 'activated') {
    log(state, 'The boat is already anchored.');
    return [];
  }
  boat.state = 'activated';
  log(state, 'The rope draws the boat into a stable crossing.');
  return [event('move')];
};
export const secureCollapsedArchWithRope = state => {
  const arch = Object.values(DIRECTIONS).filter(cardinal).map(delta => propAt(state.floor.props, state.hero.x + delta.x, state.hero.y + delta.y)).find(prop => (prop === null || prop === void 0 ? void 0 : prop.kind) === 'ruins.collapsedArch' && prop.state !== 'destroyed');
  if (!arch) return undefined;
  if (arch.state === 'dormant') {
    log(state, 'Examine the collapsed arch before bracing it.');
    return [];
  }
  if (arch.state === 'activated') {
    log(state, 'The collapsed arch is already braced.');
    return [];
  }
  arch.state = 'activated';
  log(state, 'The rope braces a safe route through the collapsed arch.');
  return [event('move')];
};
const destroyProp = (state, prop, effect) => {
  const definition = propDefinition(prop.kind);
  prop.state = 'destroyed';
  reward(state, prop, definition.effectReward);
  log(state, `The ${effect} breaks the ${definition.name}; it leaves ${definition.effectReward}.`);
};
export const expirePropEffects = state => {
  for (const prop of state.floor.props) {
    if (prop.expiresAt === undefined || prop.expiresAt > state.turn) continue;
    clearEffectCells(state, prop);
    prop.expiresAt = undefined;
    if (prop.kind === 'wilds.rootShrine') log(state, 'The shrine\'s thorn screen withers away.');
    if (prop.kind === 'caverns.barnacledShrine') log(state, 'The brine channels drain and the ward recedes.');
    if (prop.kind === 'ruins.monolith') log(state, 'The monolith\'s unstable ward expires without a telegraph.');
    if (prop.kind === 'wilds.birdNest') {
      state.floor.actors = state.floor.actors.filter(actor => actor.id !== `${prop.id}:flock`);
      log(state, 'The startled birds scatter back into the canopy.');
    }
  }
};
const applyWildsEffect = (state, prop, effect) => {
  if (prop.kind === 'wilds.mushrooms') {
    const tile = getTile(state.floor, prop.x, prop.y);
    prop.state = effect === 'water' ? 'activated' : 'destroyed';
    if (effect === 'water') {
      const revealed = revealLocal(state, prop);
      log(state, `The wet mushrooms glow and reveal ${revealed} nearby tiles.`);
    } else if (effect === 'fire') {
      if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor' || (tile === null || tile === void 0 ? void 0 : tile.kind) === 'web') tile.kind = 'fireVent';
      log(state, 'The mushrooms flare into a visible fire patch.');
    } else {
      if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor' || (tile === null || tile === void 0 ? void 0 : tile.kind) === 'web') tile.kind = 'gas';
      log(state, 'The mushrooms burst and release a visible spore cloud.');
    }
    return true;
  }
  if (prop.kind === 'wilds.danglingCharm') {
    prop.state = 'destroyed';
    const cleared = clearBramble(state, prop);
    log(state, cleared ? 'The severed charm pulls a bramble choke point apart.' : 'The dangling charm unravels into harmless roots.');
    return true;
  }
  if (prop.kind === 'wilds.birdNest') {
    if (prop.state !== 'activated') {
      prop.state = 'activated';
      const disturbed = disturbNest(state, prop);
      if (disturbed) prop.expiresAt = state.turn + 4;
      log(state, disturbed ? 'The disturbed nest draws startled birds into the path.' : 'The startled flock scatters beyond the trail.');
    }
    return true;
  }
  if (prop.kind === 'wilds.rootShrine') {
    if (effect === 'root') {
      prop.state = 'activated';
      const grown = growBramble(state, prop);
      prop.effectCells = grown.map(point => ({
        ...point
      }));
      prop.expiresAt = state.turn + 4;
      log(state, `The shrine sends up ${grown.length || 'a'} thorn screen.`);
    } else {
      clearEffectCells(state, prop);
      prop.state = 'destroyed';
      log(state, 'The shrine\'s roots char and fall away.');
    }
    return true;
  }
  if (prop.kind === 'wilds.lostParcel') {
    prop.state = 'destroyed';
    const cleared = clearBramble(state, prop);
    log(state, cleared ? 'The parcel bursts open and clears a bramble route.' : 'The lost parcel bursts into scattered trail gear.');
    return true;
  }
  if (prop.kind === 'wilds.rootArch') {
    prop.state = effect === 'fire' ? 'destroyed' : 'activated';
    log(state, effect === 'fire' ? 'Flame burns a passage through the root arch.' : 'The living roots fold aside into a detour.');
    return true;
  }
  return false;
};
const applyCavernEffect = (state, prop, effect) => {
  if (prop.kind === 'caverns.crystalCluster') {
    if (effect === 'force') {
      prop.state = 'activated';
      log(state, 'Force angles the crystal facets and refracts line effects through them.');
    } else destroyCrystal(state, prop, 'blast');
    return true;
  }
  if (prop.kind === 'caverns.glowingFungus') {
    const tile = getTile(state.floor, prop.x, prop.y);
    if (effect === 'water') {
      prop.state = 'activated';
      const revealed = revealLocal(state, prop, 4);
      log(state, `The soaked fungus brightens and reveals ${revealed} nearby tiles.`);
    } else if (effect === 'fire') {
      prop.state = 'destroyed';
      if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor' || (tile === null || tile === void 0 ? void 0 : tile.kind) === 'darkness') tile.kind = 'fireVent';
      log(state, 'The fungus burns into a visible fire patch.');
    } else {
      prop.state = 'destroyed';
      if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor' || (tile === null || tile === void 0 ? void 0 : tile.kind) === 'darkness') tile.kind = 'gas';
      log(state, 'The fungus bursts into a visible spore cloud.');
    }
    return true;
  }
  if (prop.kind === 'caverns.barnacledShrine') {
    if (effect === 'water') {
      prop.state = 'activated';
      const flooded = floodBrine(state, prop);
      prop.effectCells = flooded.map(point => ({
        ...point
      }));
      prop.expiresAt = state.turn + 4;
      addCondition(state.hero, {
        kind: 'shielded',
        duration: 4,
        potency: 1
      });
      log(state, 'The shrine answers the tide with a brief brine ward.');
    } else {
      clearEffectCells(state, prop);
      prop.state = 'destroyed';
      log(state, 'The barnacled shrine cracks and its brine ward fails.');
    }
    return true;
  }
  if (prop.kind === 'caverns.brokenBoat') {
    if (effect === 'water' || effect === 'force') {
      prop.state = 'activated';
      log(state, 'The current settles the boat into a stable water crossing.');
    } else {
      prop.state = 'destroyed';
      log(state, 'The boat breaks apart, leaving the water route open but unanchored.');
    }
    return true;
  }
  if (prop.kind === 'caverns.eelTunnel') {
    if (effect === 'force') {
      if (prop.state !== 'activated' && !canSealEelTunnel(state, prop)) {
        log(state, 'The tunnel resists a seal that would close the required trail.');
        return true;
      }
      prop.state = prop.state === 'activated' ? 'dormant' : 'activated';
      log(state, prop.state === 'activated' ? 'Force seals the eel tunnel.' : 'Force pulls the eel tunnel open as a shortcut.');
    } else if (effect === 'fire' && prop.state !== 'activated') {
      log(state, disturbEelTunnel(state, prop) ? 'Flame draws a fume eel from the open tunnel.' : 'The open tunnel hisses, but no eel reaches the path.');
    } else {
      if (canSealEelTunnel(state, prop)) {
        prop.state = 'activated';
        log(state, 'The eel tunnel seals shut.');
      } else log(state, 'The tunnel cannot seal without closing the required trail.');
    }
    return true;
  }
  if (prop.kind === 'caverns.sealedParcel') {
    prop.state = 'activated';
    reward(state, prop, 'focusTonic');
    log(state, 'The impact cracks the wax seal and opens the parcel cache.');
    return true;
  }
  return false;
};
const applyRuinsEffect = (state, prop, effect) => {
  if (prop.kind === 'ruins.brokenStatue') {
    if (effect === 'force') {
      prop.state = 'activated';
      log(state, 'Force topples the statue into line-blocking cover.');
    } else {
      prop.state = 'destroyed';
      const tile = getTile(state.floor, prop.x, prop.y);
      if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor' && preservesExitPath(state.floor, state.floor.start, prop, 'rubble')) tile.kind = 'rubble';
      reward(state, prop, 'rock');
      log(state, 'The statue breaks into safe rubble and stone shards.');
    }
    return true;
  }
  if (prop.kind === 'ruins.ritualBrazier') {
    const tile = getTile(state.floor, prop.x, prop.y);
    if (effect === 'water') {
      prop.state = 'dormant';
      if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'fireVent') tile.kind = 'floor';
      log(state, 'Water extinguishes the brazier and removes its fire hazard.');
    } else if (effect === 'ward') {
      prop.state = 'activated';
      addCondition(state.hero, {
        kind: 'shielded',
        duration: 3,
        potency: 1
      });
      addCondition(state.hero, {
        kind: 'marked',
        duration: 2,
        potency: 1
      });
      log(state, 'Ward binds the brazier: gain a brief shield, but ritual marks expose the cost.');
    } else if (effect === 'gate') {
      prop.state = 'activated';
      addCondition(state.hero, {
        kind: 'marked',
        duration: 3,
        potency: 1
      });
      log(state, 'Gate wakes the brazier; the exit opens, but the ritual marks you.');
    } else {
      prop.state = 'activated';
      if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor') tile.kind = 'fireVent';
      log(state, 'The brazier catches and creates a visible local fire hazard.');
    }
    return true;
  }
  if (prop.kind === 'ruins.glyphTablet') {
    prop.state = 'destroyed';
    log(state, 'The tablet fractures after its glyphs discharge.');
    return true;
  }
  if (prop.kind === 'ruins.collapsedArch') {
    prop.state = effect === 'bomb' ? 'destroyed' : 'activated';
    const tile = getTile(state.floor, prop.x, prop.y);
    if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'rubble') tile.kind = 'floor';
    log(state, effect === 'bomb' ? 'The blast clears the collapsed arch route.' : 'Force opens a route through the collapsed arch.');
    return true;
  }
  if (prop.kind === 'ruins.sealedCache') {
    prop.state = 'activated';
    reward(state, prop, 'sunseal');
    log(state, 'The impact breaks the cache lock and reveals a Sunstone Seal.');
    return true;
  }
  if (prop.kind === 'ruins.monolith') {
    if (effect === 'ward') {
      armMonolith(state, prop, 'Ward');
      addCondition(state.hero, {
        kind: 'shielded',
        duration: 3,
        potency: 1
      });
    } else if (effect === 'gate') armMonolith(state, prop, 'Gate');else {
      prop.state = 'destroyed';
      prop.effectCells = undefined;
      prop.expiresAt = undefined;
      log(state, 'The monolith shatters before it can answer another ritual.');
    }
    return true;
  }
  return false;
};
const applyFurnaceEffect = (state, prop, effect) => {
  const tile = getTile(state.floor, prop.x, prop.y);
  if (prop.kind === 'furnace.bellows' || prop.kind === 'furnace.smokeStack') {
    prop.state = effect === 'water' ? 'dormant' : 'activated';
    if (tile && effect !== 'water') tile.kind = effect === 'fire' ? 'fireVent' : 'smoke';
    if (tile && effect === 'water') tile.kind = 'floor';
    log(state, effect === 'water' ? 'Water settles the furnace smoke.' : 'The furnace stack changes the local air.');
    return true;
  }
  if (prop.kind === 'furnace.liftConsole') {
    prop.state = 'activated';
    for (const point of nearbyPoints(prop)) {
      var _getTile5;
      if (((_getTile5 = getTile(state.floor, point.x, point.y)) === null || _getTile5 === void 0 ? void 0 : _getTile5.kind) === 'floor') {
        getTile(state.floor, point.x, point.y).kind = 'lift';
        break;
      }
    }
    log(state, 'The console raises a nearby lift route.');
    return true;
  }
  if (prop.kind === 'furnace.breakwall') {
    prop.state = effect === 'bomb' ? 'destroyed' : 'activated';
    if (tile) tile.kind = 'floor';
    log(state, 'The scored wall opens into a hot alternate route.');
    return true;
  }
  if (prop.kind === 'furnace.forgeIdol') {
    prop.state = 'activated';
    if (effect === 'fire') addCondition(state.hero, {
      kind: 'burning',
      duration: 2,
      potency: 1
    });else addCondition(state.hero, {
      kind: 'shielded',
      duration: 2,
      potency: 1
    });
    log(state, 'The forge idol answers with power and a cost.');
    return true;
  }
  return false;
};
const applyFloodedEffect = (state, prop, effect) => {
  const tile = getTile(state.floor, prop.x, prop.y);
  if (prop.kind === 'floodedRuins.anchorPost' || prop.kind === 'floodedRuins.currentBell') {
    prop.state = 'activated';
    for (const point of nearbyPoints(prop, 2)) {
      var _getTile6;
      if (((_getTile6 = getTile(state.floor, point.x, point.y)) === null || _getTile6 === void 0 ? void 0 : _getTile6.kind) === 'current') {
        getTile(state.floor, point.x, point.y).kind = effect === 'water' ? 'water' : 'anchor';
        break;
      }
    }
    log(state, 'The anchor changes the current around the route.');
    return true;
  }
  if (prop.kind === 'floodedRuins.floodgate') {
    prop.state = 'activated';
    if (tile) tile.kind = effect === 'bomb' ? 'floor' : 'current';
    log(state, effect === 'bomb' ? 'The floodgate breaks into a dry route.' : 'The floodgate sends a current through the chamber.');
    return true;
  }
  if (prop.kind === 'floodedRuins.tideShrine') {
    prop.state = 'activated';
    addCondition(state.hero, {
      kind: 'shielded',
      duration: 3,
      potency: 1
    });
    log(state, 'The tide shrine gives a brief brine ward.');
    return true;
  }
  return false;
};
export const resolveMonolithTelegraphs = (state, telegraphs) => {
  var _monolith$effectCells;
  const monolith = state.floor.props.find(prop => prop.kind === 'ruins.monolith' && prop.state === 'activated' && prop.expiresAt !== undefined && prop.expiresAt > state.turn);
  if (!monolith) return [...telegraphs];
  const field = new Set(((_monolith$effectCells = monolith.effectCells) !== null && _monolith$effectCells !== void 0 ? _monolith$effectCells : []).map(pointKey));
  const absorbed = telegraphs.filter(telegraph => telegraph.cells.some(point => field.has(pointKey(point))));
  if (!absorbed.length) return [...telegraphs];
  monolith.state = 'destroyed';
  monolith.effectCells = undefined;
  monolith.expiresAt = undefined;
  log(state, `The monolith absorbs ${absorbed.length} nearby telegraph${absorbed.length === 1 ? '' : 's'} and cracks apart.`);
  return telegraphs.filter(telegraph => !absorbed.includes(telegraph));
};
export const applyPropEffects = (state, points, effects) => {
  const targets = new Set(points.map(pointKey));
  const changed = [];
  for (const prop of state.floor.props) {
    if (prop.state === 'destroyed' || !targets.has(pointKey(prop))) continue;
    const effect = effects.find(candidate => {
      var _prop$hooks2;
      return (_prop$hooks2 = prop.hooks) === null || _prop$hooks2 === void 0 ? void 0 : _prop$hooks2.includes(candidate);
    });
    if (!effect) continue;
    if (wildsProp(prop) && applyWildsEffect(state, prop, effect)) {
      changed.push(prop.id);
      continue;
    }
    if (cavernProp(prop) && applyCavernEffect(state, prop, effect)) {
      changed.push(prop.id);
      continue;
    }
    if (ruinsProp(prop) && applyRuinsEffect(state, prop, effect)) {
      changed.push(prop.id);
      continue;
    }
    if (furnaceProp(prop) && applyFurnaceEffect(state, prop, effect)) {
      changed.push(prop.id);
      continue;
    }
    if (floodedProp(prop) && applyFloodedEffect(state, prop, effect)) {
      changed.push(prop.id);
      continue;
    }
    if (prop.kind === 'mine.lanternPost') {
      if (effect === 'fire') {
        prop.state = 'activated';
        log(state, 'The lantern post catches and spills local light.');
        changed.push(prop.id);
        continue;
      }
      if (effect === 'water' || effect === 'hazard') {
        prop.state = 'dormant';
        log(state, 'The lantern post gutters out.');
        changed.push(prop.id);
        continue;
      }
    }
    if (prop.kind === 'mine.oreVein' && effect === 'bomb') {
      prop.state = 'destroyed';
      const tile = getTile(state.floor, prop.x, prop.y);
      if (tile) tile.kind = 'floor';
      reward(state, prop, 'rock', 2);
      log(state, 'The blast clears the ore vein and scatters rock.');
      changed.push(prop.id);
      continue;
    }
    if (prop.kind === 'mine.brokenCart' && effect === 'force') continue;
    if (prop.kind === 'mine.discardedParcel') {
      prop.state = 'destroyed';
      log(state, 'The abandoned parcel bursts into a noisy blast.');
      changed.push(prop.id);
      explode(state, prop.x, prop.y, 5, ['bomb'], 'the discarded parcel');
      continue;
    }
    destroyProp(state, prop, effect);
    changed.push(prop.id);
  }
  return changed;
};
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJESVJFQ1RJT05TIiwiZmxvb3JQb2ludCIsImlzQmxvY2tpbmdQcm9wIiwicHJvcEF0IiwicHJvcERlZmluaXRpb24iLCJhY3RvckF0IiwiZ2V0VGlsZSIsImhhc1Bhc3NhYmxlUGF0aCIsImlzUGFzc2FibGUiLCJwcmVzZXJ2ZXNFeGl0UGF0aCIsInNwYXduTW9uc3RlciIsImRhbWFnZUhlcm8iLCJleHBsb2RlIiwicmVzb2x2ZURlZmVhdGVkQWN0b3JzIiwiYWRkQ29uZGl0aW9uIiwibW9kaWZ5SW5jb21pbmdEYW1hZ2UiLCJldmVudCIsImxvZyIsInBvaW50S2V5IiwicG9pbnQiLCJ4IiwieSIsIm1pbmVQcm9wIiwicHJvcCIsImJpb21lIiwia2luZCIsInN0YXJ0c1dpdGgiLCJ3aWxkc1Byb3AiLCJjYXZlcm5Qcm9wIiwicnVpbnNQcm9wIiwiZnVybmFjZVByb3AiLCJmbG9vZGVkUHJvcCIsImNhcmRpbmFsIiwiTWF0aCIsImFicyIsImhhemFyZEtpbmRzIiwiU2V0IiwicmV3YXJkIiwic3RhdGUiLCJpZCIsImNvdW50IiwiZmxvb3IiLCJpdGVtcyIsInB1c2giLCJ2aXNpYmxlSW5Gb2ciLCJuZWFyYnlQb2ludHMiLCJyYWRpdXMiLCJwb2ludHMiLCJzb3J0IiwiZmlyc3QiLCJzZWNvbmQiLCJtYXgiLCJyZXZlYWxMb2NhbCIsInJldmVhbGVkIiwiY2FuZGlkYXRlIiwidGlsZSIsImV4cGxvcmVkIiwiY2xlYXJCcmFtYmxlIiwiZmluZCIsImN1cnJlbnQiLCJfZ2V0VGlsZSIsImdyb3dCcmFtYmxlIiwiZ3Jvd24iLCJoZXJvIiwic3RhcnQiLCJsZW5ndGgiLCJjbGVhckVmZmVjdENlbGxzIiwiX3Byb3AkZWZmZWN0Q2VsbHMiLCJlZmZlY3RDZWxscyIsInVuZGVmaW5lZCIsImZsb29kQnJpbmUiLCJmbG9vZGVkIiwiZGlzdHVyYkVlbFR1bm5lbCIsImFjdG9ycyIsImRlc3Ryb3lDcnlzdGFsIiwic291cmNlIiwiY29uc3VtZUNhdmVybkNhY2hlS2V5Iiwia2V5cyIsImJvbWJzIiwiZXF1aXBtZW50IiwiY2hhcm0iLCJ3YXJkIiwiaW52ZW50b3J5IiwiaW5kZXhPZiIsInNwbGljZSIsImNhblNlYWxFZWxUdW5uZWwiLCJ0dW5uZWwiLCJwcmV2aW91cyIsInNhZmUiLCJleGl0IiwiYXJtTW9ub2xpdGgiLCJtYXAiLCJleHBpcmVzQXQiLCJ0dXJuIiwiZHVyYXRpb24iLCJwb3RlbmN5IiwiY29uc3VtZVJ1aW5zQ2FjaGVLZXkiLCJkaXN0dXJiTmVzdCIsImNvbnN1bWVXaWxkc0NoYXJtIiwiaXRlbSIsIm5lYXJieVByb3AiLCJPYmplY3QiLCJ2YWx1ZXMiLCJmaWx0ZXIiLCJkZWx0YSIsInByb3BzIiwiQm9vbGVhbiIsImluc3BlY3QiLCJmb2xsb3d1cCIsImRlZmluaXRpb24iLCJuYW1lIiwiZGVzY3JpcHRpb24iLCJldmVudHMiLCJjYXJ0Q2FuZGlkYXRlcyIsImNhcnQiLCJjaG9pY2VzIiwiX2dldFRpbGUyIiwidHJpZ2dlckNhcnRIYXphcmRzIiwiaGF6YXJkIiwibW92ZUNhcnQiLCJfZ2V0VGlsZTMiLCJkaXJlY3Rpb24iLCJfZ2V0VGlsZTQiLCJuZXh0IiwiYWN0b3IiLCJob3N0aWxlIiwiaGVhbHRoIiwic29tZSIsImVudHJ5IiwidHlwZSIsIm9wdGlvbnMiLCJzdHJhaWdodCIsInJldmVhbFdhcm5pbmdzIiwic2t1bGwiLCJ0aWxlcyIsImZsYXRNYXAiLCJpbmRleCIsImhhcyIsInByaW9yaXR5Iiwid2FybmluZ3MiLCJzbGljZSIsIm9wZXJhdGVNaW5lUHJvcCIsIm1haW5IYW5kIiwic2lnbiIsImFjdGl2YXRpb25SZXdhcmQiLCJvcGVyYXRlV2lsZHNQcm9wIiwiY2xlYXJlZCIsIm1pbiIsIm1heEhlYWx0aCIsIm9wZXJhdGVDYXZlcm5Qcm9wIiwib2ZmZXJpbmciLCJrZXkiLCJvcGVyYXRlUnVpbnNQcm9wIiwiZW1iZXIiLCJfc3RhdGUkZmxvb3IkdGVsZWdyYXAiLCJfc3RhdGUkZmxvb3IkcHV6emxlSWQiLCJ0aHJlYXRzIiwidGVsZWdyYXBocyIsInRlbGVncmFwaCIsIl90ZWxlZ3JhcGgkY2VsbHMkIiwiX3RlbGVncmFwaCRjZWxscyQyIiwiY2VsbHMiLCJ0aW1pbmciLCJhY3Rpb25JZCIsInJlc29sdmVUdXJuIiwiam9pbiIsInB1enpsZSIsInB1enpsZUlkcyIsImZvY3VzIiwib3BlcmF0ZVByb3AiLCJfcHJvcCRob29rcyIsImhvb2tzIiwiaW5jbHVkZXMiLCJtb3ZlQ2FydEJ5Rm9yY2UiLCJwdWxsIiwiYXdheSIsInJlbGVhc2VDYXJ0V2l0aFJvcGUiLCJhbmNob3JCb2F0V2l0aFJvcGUiLCJib2F0Iiwic2VjdXJlQ29sbGFwc2VkQXJjaFdpdGhSb3BlIiwiYXJjaCIsImRlc3Ryb3lQcm9wIiwiZWZmZWN0IiwiZWZmZWN0UmV3YXJkIiwiZXhwaXJlUHJvcEVmZmVjdHMiLCJhcHBseVdpbGRzRWZmZWN0IiwiZGlzdHVyYmVkIiwiYXBwbHlDYXZlcm5FZmZlY3QiLCJhcHBseVJ1aW5zRWZmZWN0IiwiYXBwbHlGdXJuYWNlRWZmZWN0IiwiX2dldFRpbGU1IiwiYXBwbHlGbG9vZGVkRWZmZWN0IiwiX2dldFRpbGU2IiwicmVzb2x2ZU1vbm9saXRoVGVsZWdyYXBocyIsIl9tb25vbGl0aCRlZmZlY3RDZWxscyIsIm1vbm9saXRoIiwiZmllbGQiLCJhYnNvcmJlZCIsImFwcGx5UHJvcEVmZmVjdHMiLCJlZmZlY3RzIiwidGFyZ2V0cyIsImNoYW5nZWQiLCJfcHJvcCRob29rczIiXSwic291cmNlcyI6WyJwcm9wcy50cyJdLCJzb3VyY2VzQ29udGVudCI6WyJpbXBvcnQgeyBESVJFQ1RJT05TLCBmbG9vclBvaW50LCB0eXBlIFBvaW50LCB0eXBlIFByb3AsIHR5cGUgUHJvcEVmZmVjdEtpbmQsIHR5cGUgUnVuU3RhdGUsIHR5cGUgVGVsZWdyYXBoIH0gZnJvbSAnLi4vdHlwZXMnXG5pbXBvcnQgeyBpc0Jsb2NraW5nUHJvcCwgcHJvcEF0LCBwcm9wRGVmaW5pdGlvbiB9IGZyb20gJy4uL3Byb3BzJ1xuaW1wb3J0IHsgYWN0b3JBdCwgZ2V0VGlsZSwgaGFzUGFzc2FibGVQYXRoLCBpc1Bhc3NhYmxlLCBwcmVzZXJ2ZXNFeGl0UGF0aCwgc3Bhd25Nb25zdGVyIH0gZnJvbSAnLi4vd29ybGQnXG5pbXBvcnQgeyBkYW1hZ2VIZXJvLCBleHBsb2RlLCByZXNvbHZlRGVmZWF0ZWRBY3RvcnMgfSBmcm9tICcuL2NvbWJhdCdcbmltcG9ydCB7IGFkZENvbmRpdGlvbiwgbW9kaWZ5SW5jb21pbmdEYW1hZ2UgfSBmcm9tICcuL2NvbmRpdGlvbnMnXG5pbXBvcnQgeyBldmVudCwgbG9nLCB0eXBlIEFjdGlvblJlc3VsdCB9IGZyb20gJy4vc2hhcmVkJ1xuXG5jb25zdCBwb2ludEtleSA9IChwb2ludDogUG9pbnQpOiBzdHJpbmcgPT4gYCR7cG9pbnQueH0sJHtwb2ludC55fWBcbmNvbnN0IG1pbmVQcm9wID0gKHByb3A6IFByb3ApOiBib29sZWFuID0+IHByb3AuYmlvbWUgPT09ICdtaW5lJyAmJiBwcm9wLmtpbmQuc3RhcnRzV2l0aCgnbWluZS4nKVxuY29uc3Qgd2lsZHNQcm9wID0gKHByb3A6IFByb3ApOiBib29sZWFuID0+IHByb3AuYmlvbWUgPT09ICd3aWxkcycgJiYgcHJvcC5raW5kLnN0YXJ0c1dpdGgoJ3dpbGRzLicpXG5jb25zdCBjYXZlcm5Qcm9wID0gKHByb3A6IFByb3ApOiBib29sZWFuID0+IHByb3AuYmlvbWUgPT09ICdjYXZlcm5zJyAmJiBwcm9wLmtpbmQuc3RhcnRzV2l0aCgnY2F2ZXJucy4nKVxuY29uc3QgcnVpbnNQcm9wID0gKHByb3A6IFByb3ApOiBib29sZWFuID0+IHByb3AuYmlvbWUgPT09ICdydWlucycgJiYgcHJvcC5raW5kLnN0YXJ0c1dpdGgoJ3J1aW5zLicpXG5jb25zdCBmdXJuYWNlUHJvcCA9IChwcm9wOiBQcm9wKTogYm9vbGVhbiA9PiBwcm9wLmJpb21lID09PSAnZnVybmFjZScgJiYgcHJvcC5raW5kLnN0YXJ0c1dpdGgoJ2Z1cm5hY2UuJylcbmNvbnN0IGZsb29kZWRQcm9wID0gKHByb3A6IFByb3ApOiBib29sZWFuID0+IHByb3AuYmlvbWUgPT09ICdmbG9vZGVkUnVpbnMnICYmIHByb3Aua2luZC5zdGFydHNXaXRoKCdmbG9vZGVkUnVpbnMuJylcbmNvbnN0IGNhcmRpbmFsID0gKHBvaW50OiBQb2ludCk6IGJvb2xlYW4gPT4gTWF0aC5hYnMocG9pbnQueCkgKyBNYXRoLmFicyhwb2ludC55KSA9PT0gMVxuY29uc3QgaGF6YXJkS2luZHMgPSBuZXcgU2V0KFsnc3Bpa2VzJywgJ2RhcnQnLCAnZmlyZVZlbnQnLCAnY3J1bWJsZScsICdib3VsZGVyJywgJ2dhcycsICdsYXZhJywgJ3BpdCddKVxuXG5jb25zdCByZXdhcmQgPSAoc3RhdGU6IFJ1blN0YXRlLCBwcm9wOiBQcm9wLCBpZDogc3RyaW5nLCBjb3VudCA9IDEpOiB2b2lkID0+IHtcbiAgc3RhdGUuZmxvb3IuaXRlbXMucHVzaCh7IGlkLCB4OiBwcm9wLngsIHk6IHByb3AueSwgY291bnQsIHZpc2libGVJbkZvZzogdHJ1ZSB9KVxufVxuXG5jb25zdCBuZWFyYnlQb2ludHMgPSAocG9pbnQ6IFBvaW50LCByYWRpdXMgPSAxKTogUG9pbnRbXSA9PiB7XG4gIGNvbnN0IHBvaW50czogUG9pbnRbXSA9IFtdXG4gIGZvciAobGV0IHkgPSBwb2ludC55IC0gcmFkaXVzOyB5IDw9IHBvaW50LnkgKyByYWRpdXM7IHkrKykgZm9yIChsZXQgeCA9IHBvaW50LnggLSByYWRpdXM7IHggPD0gcG9pbnQueCArIHJhZGl1czsgeCsrKSBpZiAoeCAhPT0gcG9pbnQueCB8fCB5ICE9PSBwb2ludC55KSBwb2ludHMucHVzaCh7IHgsIHkgfSlcbiAgcmV0dXJuIHBvaW50cy5zb3J0KChmaXJzdCwgc2Vjb25kKSA9PiBNYXRoLm1heChNYXRoLmFicyhmaXJzdC54IC0gcG9pbnQueCksIE1hdGguYWJzKGZpcnN0LnkgLSBwb2ludC55KSkgLSBNYXRoLm1heChNYXRoLmFicyhzZWNvbmQueCAtIHBvaW50LngpLCBNYXRoLmFicyhzZWNvbmQueSAtIHBvaW50LnkpKSB8fCBmaXJzdC55IC0gc2Vjb25kLnkgfHwgZmlyc3QueCAtIHNlY29uZC54KVxufVxuXG5jb25zdCByZXZlYWxMb2NhbCA9IChzdGF0ZTogUnVuU3RhdGUsIHBvaW50OiBQb2ludCwgcmFkaXVzID0gMyk6IG51bWJlciA9PiB7XG4gIGxldCByZXZlYWxlZCA9IDBcbiAgZm9yIChjb25zdCBjYW5kaWRhdGUgb2YgbmVhcmJ5UG9pbnRzKHBvaW50LCByYWRpdXMpKSB7XG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIGNhbmRpZGF0ZS54LCBjYW5kaWRhdGUueSlcbiAgICBpZiAoIXRpbGUgfHwgdGlsZS5leHBsb3JlZCkgY29udGludWVcbiAgICB0aWxlLmV4cGxvcmVkID0gdHJ1ZVxuICAgIHJldmVhbGVkKytcbiAgfVxuICByZXR1cm4gcmV2ZWFsZWRcbn1cblxuY29uc3QgY2xlYXJCcmFtYmxlID0gKHN0YXRlOiBSdW5TdGF0ZSwgcG9pbnQ6IFBvaW50KTogYm9vbGVhbiA9PiB7XG4gIGNvbnN0IGNhbmRpZGF0ZSA9IG5lYXJieVBvaW50cyhwb2ludCwgMikuZmluZChjdXJyZW50ID0+IGdldFRpbGUoc3RhdGUuZmxvb3IsIGN1cnJlbnQueCwgY3VycmVudC55KT8ua2luZCA9PT0gJ2JyYW1ibGUnKVxuICBpZiAoIWNhbmRpZGF0ZSkgcmV0dXJuIGZhbHNlXG4gIGdldFRpbGUoc3RhdGUuZmxvb3IsIGNhbmRpZGF0ZS54LCBjYW5kaWRhdGUueSkhLmtpbmQgPSAnZmxvb3InXG4gIHJldHVybiB0cnVlXG59XG5cbmNvbnN0IGdyb3dCcmFtYmxlID0gKHN0YXRlOiBSdW5TdGF0ZSwgcG9pbnQ6IFBvaW50KTogUG9pbnRbXSA9PiB7XG4gIGNvbnN0IGdyb3duOiBQb2ludFtdID0gW11cbiAgZm9yIChjb25zdCBjYW5kaWRhdGUgb2YgbmVhcmJ5UG9pbnRzKHBvaW50KSkge1xuICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBjYW5kaWRhdGUueCwgY2FuZGlkYXRlLnkpXG4gICAgaWYgKCF0aWxlIHx8IHRpbGUua2luZCAhPT0gJ2Zsb29yJyB8fCBhY3RvckF0KHN0YXRlLmZsb29yLCBjYW5kaWRhdGUueCwgY2FuZGlkYXRlLnkpIHx8IChzdGF0ZS5oZXJvLnggPT09IGNhbmRpZGF0ZS54ICYmIHN0YXRlLmhlcm8ueSA9PT0gY2FuZGlkYXRlLnkpKSBjb250aW51ZVxuICAgIGlmICghcHJlc2VydmVzRXhpdFBhdGgoc3RhdGUuZmxvb3IsIHN0YXRlLmZsb29yLnN0YXJ0LCBjYW5kaWRhdGUsICdicmFtYmxlJykpIGNvbnRpbnVlXG4gICAgdGlsZS5raW5kID0gJ2JyYW1ibGUnXG4gICAgZ3Jvd24ucHVzaChjYW5kaWRhdGUpXG4gICAgaWYgKGdyb3duLmxlbmd0aCA9PT0gMikgYnJlYWtcbiAgfVxuICByZXR1cm4gZ3Jvd25cbn1cblxuY29uc3QgY2xlYXJFZmZlY3RDZWxscyA9IChzdGF0ZTogUnVuU3RhdGUsIHByb3A6IFByb3ApOiB2b2lkID0+IHtcbiAgZm9yIChjb25zdCBwb2ludCBvZiBwcm9wLmVmZmVjdENlbGxzID8/IFtdKSB7XG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHBvaW50LngsIHBvaW50LnkpXG4gICAgaWYgKHRpbGU/LmtpbmQgPT09ICdicmFtYmxlJykgdGlsZS5raW5kID0gJ2Zsb29yJ1xuICAgIGlmIChwcm9wLmtpbmQgPT09ICdjYXZlcm5zLmJhcm5hY2xlZFNocmluZScgJiYgdGlsZT8ua2luZCA9PT0gJ3dhdGVyJykgdGlsZS5raW5kID0gJ2Zsb29yJ1xuICB9XG4gIHByb3AuZWZmZWN0Q2VsbHMgPSB1bmRlZmluZWRcbn1cblxuY29uc3QgZmxvb2RCcmluZSA9IChzdGF0ZTogUnVuU3RhdGUsIHBvaW50OiBQb2ludCk6IFBvaW50W10gPT4ge1xuICBjb25zdCBmbG9vZGVkOiBQb2ludFtdID0gW11cbiAgZm9yIChjb25zdCBjYW5kaWRhdGUgb2YgbmVhcmJ5UG9pbnRzKHBvaW50KSkge1xuICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBjYW5kaWRhdGUueCwgY2FuZGlkYXRlLnkpXG4gICAgaWYgKCF0aWxlIHx8IHRpbGUua2luZCAhPT0gJ2Zsb29yJyB8fCBhY3RvckF0KHN0YXRlLmZsb29yLCBjYW5kaWRhdGUueCwgY2FuZGlkYXRlLnkpIHx8IChzdGF0ZS5oZXJvLnggPT09IGNhbmRpZGF0ZS54ICYmIHN0YXRlLmhlcm8ueSA9PT0gY2FuZGlkYXRlLnkpKSBjb250aW51ZVxuICAgIHRpbGUua2luZCA9ICd3YXRlcidcbiAgICBmbG9vZGVkLnB1c2goY2FuZGlkYXRlKVxuICAgIGlmIChmbG9vZGVkLmxlbmd0aCA9PT0gMikgYnJlYWtcbiAgfVxuICByZXR1cm4gZmxvb2RlZFxufVxuXG5jb25zdCBkaXN0dXJiRWVsVHVubmVsID0gKHN0YXRlOiBSdW5TdGF0ZSwgcHJvcDogUHJvcCk6IGJvb2xlYW4gPT4ge1xuICBjb25zdCBjYW5kaWRhdGUgPSBuZWFyYnlQb2ludHMocHJvcCwgMykuZmluZChwb2ludCA9PiBNYXRoLm1heChNYXRoLmFicyhwb2ludC54IC0gc3RhdGUuaGVyby54KSwgTWF0aC5hYnMocG9pbnQueSAtIHN0YXRlLmhlcm8ueSkpID4gMSAmJiBpc1Bhc3NhYmxlKHN0YXRlLmZsb29yLCBwb2ludC54LCBwb2ludC55KSAmJiAhYWN0b3JBdChzdGF0ZS5mbG9vciwgcG9pbnQueCwgcG9pbnQueSkpXG4gIGlmICghY2FuZGlkYXRlKSByZXR1cm4gZmFsc2VcbiAgc3RhdGUuZmxvb3IuYWN0b3JzLnB1c2goc3Bhd25Nb25zdGVyKCdmdW1lZWVsJywgY2FuZGlkYXRlLCBgJHtwcm9wLmlkfTplZWxgKSlcbiAgcmV0dXJuIHRydWVcbn1cblxuY29uc3QgZGVzdHJveUNyeXN0YWwgPSAoc3RhdGU6IFJ1blN0YXRlLCBwcm9wOiBQcm9wLCBzb3VyY2U6ICdwaWNrYXhlJyB8ICdibGFzdCcpOiB2b2lkID0+IHtcbiAgcHJvcC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBwcm9wLngsIHByb3AueSlcbiAgaWYgKHRpbGU/LmtpbmQgPT09ICdmbG9vcicgJiYgcHJlc2VydmVzRXhpdFBhdGgoc3RhdGUuZmxvb3IsIHN0YXRlLmZsb29yLnN0YXJ0LCBwcm9wLCAncnViYmxlJykpIHRpbGUua2luZCA9ICdydWJibGUnXG4gIHJld2FyZChzdGF0ZSwgcHJvcCwgJ3JvY2snLCAyKVxuICBsb2coc3RhdGUsIHNvdXJjZSA9PT0gJ3BpY2theGUnID8gJ1lvdSBtaW5lIGNyeXN0YWwgc2hhcmRzIGFuZCBsZWF2ZSBzYWZlIHJ1YmJsZS4nIDogJ1RoZSBibGFzdCBzaGF0dGVycyB0aGUgY3J5c3RhbCBpbnRvIHNhZmUgcnViYmxlLicpXG59XG5cbmNvbnN0IGNvbnN1bWVDYXZlcm5DYWNoZUtleSA9IChzdGF0ZTogUnVuU3RhdGUpOiAna2V5JyB8ICdib21iJyB8ICdzdW5zZWFsJyB8ICd3YXJkU2NyaXB0JyB8IHVuZGVmaW5lZCA9PiB7XG4gIGlmIChzdGF0ZS5oZXJvLmtleXMgPiAwKSB7IHN0YXRlLmhlcm8ua2V5cy0tOyByZXR1cm4gJ2tleScgfVxuICBpZiAoc3RhdGUuaGVyby5ib21icyA+IDApIHsgc3RhdGUuaGVyby5ib21icy0tOyByZXR1cm4gJ2JvbWInIH1cbiAgaWYgKHN0YXRlLmhlcm8uZXF1aXBtZW50LmNoYXJtID09PSAnc3Vuc2VhbCcpIHsgc3RhdGUuaGVyby5lcXVpcG1lbnQuY2hhcm0gPSB1bmRlZmluZWQ7IHJldHVybiAnc3Vuc2VhbCcgfVxuICBjb25zdCB3YXJkID0gc3RhdGUuaGVyby5pbnZlbnRvcnkuaW5kZXhPZignd2FyZFNjcmlwdCcpXG4gIGlmICh3YXJkID49IDApIHsgc3RhdGUuaGVyby5pbnZlbnRvcnkuc3BsaWNlKHdhcmQsIDEpOyByZXR1cm4gJ3dhcmRTY3JpcHQnIH1cbiAgcmV0dXJuIHVuZGVmaW5lZFxufVxuXG5jb25zdCBjYW5TZWFsRWVsVHVubmVsID0gKHN0YXRlOiBSdW5TdGF0ZSwgdHVubmVsOiBQcm9wKTogYm9vbGVhbiA9PiB7XG4gIGNvbnN0IHByZXZpb3VzID0gdHVubmVsLnN0YXRlXG4gIHR1bm5lbC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gIGNvbnN0IHNhZmUgPSBoYXNQYXNzYWJsZVBhdGgoc3RhdGUuZmxvb3IsIHN0YXRlLmhlcm8sIHN0YXRlLmZsb29yLmV4aXQpICYmIGhhc1Bhc3NhYmxlUGF0aChzdGF0ZS5mbG9vciwgc3RhdGUuZmxvb3Iuc3RhcnQsIHN0YXRlLmZsb29yLmV4aXQpXG4gIHR1bm5lbC5zdGF0ZSA9IHByZXZpb3VzXG4gIHJldHVybiBzYWZlXG59XG5cbmNvbnN0IGFybU1vbm9saXRoID0gKHN0YXRlOiBSdW5TdGF0ZSwgcHJvcDogUHJvcCwgc291cmNlOiBzdHJpbmcpOiB2b2lkID0+IHtcbiAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gIHByb3AuZWZmZWN0Q2VsbHMgPSBbeyB4OiBwcm9wLngsIHk6IHByb3AueSB9LCAuLi5uZWFyYnlQb2ludHMocHJvcCwgMildLm1hcChwb2ludCA9PiAoeyAuLi5wb2ludCB9KSlcbiAgcHJvcC5leHBpcmVzQXQgPSBzdGF0ZS50dXJuICsgNFxuICBhZGRDb25kaXRpb24oc3RhdGUuaGVybywgeyBraW5kOiAnbWFya2VkJywgZHVyYXRpb246IDMsIHBvdGVuY3k6IDEgfSlcbiAgbG9nKHN0YXRlLCBgJHtzb3VyY2V9IGFybXMgdGhlIG1vbm9saXRoJ3MgdW5zdGFibGUgbG9jYWwgd2FyZCBmb3IgZm91ciB0dXJucy5gKVxufVxuXG5jb25zdCBjb25zdW1lUnVpbnNDYWNoZUtleSA9IChzdGF0ZTogUnVuU3RhdGUpOiAna2V5JyB8ICdib21iJyB8ICdzdW5zZWFsJyB8ICd3YXJkU2NyaXB0JyB8IHVuZGVmaW5lZCA9PiBjb25zdW1lQ2F2ZXJuQ2FjaGVLZXkoc3RhdGUpXG5cbmNvbnN0IGRpc3R1cmJOZXN0ID0gKHN0YXRlOiBSdW5TdGF0ZSwgcHJvcDogUHJvcCk6IGJvb2xlYW4gPT4ge1xuICBjb25zdCBjYW5kaWRhdGUgPSBuZWFyYnlQb2ludHMocHJvcCwgMykuZmluZChwb2ludCA9PiBNYXRoLm1heChNYXRoLmFicyhwb2ludC54IC0gc3RhdGUuaGVyby54KSwgTWF0aC5hYnMocG9pbnQueSAtIHN0YXRlLmhlcm8ueSkpID4gMSAmJiBpc1Bhc3NhYmxlKHN0YXRlLmZsb29yLCBwb2ludC54LCBwb2ludC55KSAmJiAhYWN0b3JBdChzdGF0ZS5mbG9vciwgcG9pbnQueCwgcG9pbnQueSkpXG4gIGlmICghY2FuZGlkYXRlKSByZXR1cm4gZmFsc2VcbiAgc3RhdGUuZmxvb3IuYWN0b3JzLnB1c2goc3Bhd25Nb25zdGVyKCdzdGFydGxlZEJpcmRzJywgY2FuZGlkYXRlLCBgJHtwcm9wLmlkfTpmbG9ja2ApKVxuICByZXR1cm4gdHJ1ZVxufVxuXG5jb25zdCBjb25zdW1lV2lsZHNDaGFybSA9IChzdGF0ZTogUnVuU3RhdGUpOiAncm9vdCcgfCAnbWVuZCcgfCAnd2FyZFNjcmlwdCcgfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBpZCA9IHN0YXRlLmhlcm8uaW52ZW50b3J5LmZpbmQoaXRlbSA9PiBpdGVtID09PSAncm9vdCcgfHwgaXRlbSA9PT0gJ21lbmQnIHx8IGl0ZW0gPT09ICd3YXJkU2NyaXB0JykgYXMgJ3Jvb3QnIHwgJ21lbmQnIHwgJ3dhcmRTY3JpcHQnIHwgdW5kZWZpbmVkXG4gIGlmICghaWQpIHJldHVybiB1bmRlZmluZWRcbiAgc3RhdGUuaGVyby5pbnZlbnRvcnkuc3BsaWNlKHN0YXRlLmhlcm8uaW52ZW50b3J5LmluZGV4T2YoaWQpLCAxKVxuICByZXR1cm4gaWRcbn1cblxuY29uc3QgbmVhcmJ5UHJvcCA9IChzdGF0ZTogUnVuU3RhdGUpOiBQcm9wIHwgdW5kZWZpbmVkID0+IHtcbiAgY29uc3QgcG9pbnRzID0gW3sgeDogc3RhdGUuaGVyby54LCB5OiBzdGF0ZS5oZXJvLnkgfSwgLi4uT2JqZWN0LnZhbHVlcyhESVJFQ1RJT05TKS5maWx0ZXIoZGVsdGEgPT4gZGVsdGEueCB8fCBkZWx0YS55KS5tYXAoZGVsdGEgPT4gKHsgeDogc3RhdGUuaGVyby54ICsgZGVsdGEueCwgeTogc3RhdGUuaGVyby55ICsgZGVsdGEueSB9KSldXG4gIHJldHVybiBwb2ludHMubWFwKHBvaW50ID0+IHByb3BBdChzdGF0ZS5mbG9vci5wcm9wcywgcG9pbnQueCwgcG9pbnQueSkpLmZpbmQoKHByb3ApOiBwcm9wIGlzIFByb3AgPT4gQm9vbGVhbihwcm9wKSlcbn1cblxuY29uc3QgaW5zcGVjdCA9IChzdGF0ZTogUnVuU3RhdGUsIHByb3A6IFByb3AsIGZvbGxvd3VwOiBzdHJpbmcpOiBQcm9wT3BlcmF0aW9uID0+IHtcbiAgcHJvcC5zdGF0ZSA9ICdpbnNwZWN0ZWQnXG4gIGNvbnN0IGRlZmluaXRpb24gPSBwcm9wRGVmaW5pdGlvbihwcm9wLmtpbmQpXG4gIGxvZyhzdGF0ZSwgYFlvdSBleGFtaW5lIHRoZSAke2RlZmluaXRpb24ubmFtZX06ICR7ZGVmaW5pdGlvbi5kZXNjcmlwdGlvbn0gJHtmb2xsb3d1cH1gKVxuICByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH1cbn1cblxuY29uc3QgY2FydENhbmRpZGF0ZXMgPSAoc3RhdGU6IFJ1blN0YXRlLCBjYXJ0OiBQcm9wLCBwcmV2aW91czogUG9pbnQgfCB1bmRlZmluZWQpOiBQb2ludFtdID0+IHtcbiAgY29uc3QgY2hvaWNlcyA9IFt7IHg6IDAsIHk6IC0xIH0sIHsgeDogMSwgeTogMCB9LCB7IHg6IDAsIHk6IDEgfSwgeyB4OiAtMSwgeTogMCB9XVxuICAgIC5tYXAoZGVsdGEgPT4gKHsgeDogY2FydC54ICsgZGVsdGEueCwgeTogY2FydC55ICsgZGVsdGEueSB9KSlcbiAgICAuZmlsdGVyKHBvaW50ID0+IGdldFRpbGUoc3RhdGUuZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kID09PSAncmFpbCcpXG4gICAgLmZpbHRlcihwb2ludCA9PiAhcHJldmlvdXMgfHwgcG9pbnQueCAhPT0gcHJldmlvdXMueCB8fCBwb2ludC55ICE9PSBwcmV2aW91cy55KVxuICAgIC5maWx0ZXIocG9pbnQgPT4gIWlzQmxvY2tpbmdQcm9wKHByb3BBdChzdGF0ZS5mbG9vci5wcm9wcywgcG9pbnQueCwgcG9pbnQueSkpKVxuICByZXR1cm4gY2hvaWNlc1xufVxuXG5jb25zdCB0cmlnZ2VyQ2FydEhhemFyZHMgPSAoc3RhdGU6IFJ1blN0YXRlLCBjYXJ0OiBQcm9wKTogQWN0aW9uUmVzdWx0ID0+IHtcbiAgY29uc3QgZXZlbnRzOiBBY3Rpb25SZXN1bHQgPSBbXVxuICBmb3IgKGNvbnN0IGRlbHRhIG9mIE9iamVjdC52YWx1ZXMoRElSRUNUSU9OUykpIHtcbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShzdGF0ZS5mbG9vciwgY2FydC54ICsgZGVsdGEueCwgY2FydC55ICsgZGVsdGEueSlcbiAgICBpZiAodGlsZT8ua2luZCAhPT0gJ2RhcnQnICYmIHRpbGU/LmtpbmQgIT09ICdmaXJlVmVudCcpIGNvbnRpbnVlXG4gICAgY29uc3QgaGF6YXJkID0gdGlsZS5raW5kXG4gICAgdGlsZS5raW5kID0gJ2Zsb29yJ1xuICAgIGxvZyhzdGF0ZSwgYFRoZSBjYXJ0IHRyaWdnZXJzIHRoZSAke2hhemFyZCA9PT0gJ2RhcnQnID8gJ2RhcnQnIDogJ2ZpcmUnfSB0cmFwLmApXG4gICAgZXZlbnRzLnB1c2goZXZlbnQoJ2RhbmdlcicpKVxuICB9XG4gIHJldHVybiBldmVudHNcbn1cblxuY29uc3QgbW92ZUNhcnQgPSAoc3RhdGU6IFJ1blN0YXRlLCBjYXJ0OiBQcm9wLCBmaXJzdDogUG9pbnQpOiBBY3Rpb25SZXN1bHQgPT4ge1xuICBjb25zdCBldmVudHM6IEFjdGlvblJlc3VsdCA9IFtdXG4gIGlmICghY2FyZGluYWwoZmlyc3QpIHx8IGdldFRpbGUoc3RhdGUuZmxvb3IsIGNhcnQueCArIGZpcnN0LngsIGNhcnQueSArIGZpcnN0LnkpPy5raW5kICE9PSAncmFpbCcpIHsgbG9nKHN0YXRlLCAnVGhlIGNhcnQgaGFzIG5vIHJhaWwgaW4gdGhhdCBkaXJlY3Rpb24uJyk7IHJldHVybiBbXSB9XG4gIGxldCBkaXJlY3Rpb24gPSB7IC4uLmZpcnN0IH1cbiAgbGV0IHByZXZpb3VzID0geyB4OiBjYXJ0LngsIHk6IGNhcnQueSB9XG4gIHdoaWxlICh0cnVlKSB7XG4gICAgY29uc3QgbmV4dCA9IHsgeDogY2FydC54ICsgZGlyZWN0aW9uLngsIHk6IGNhcnQueSArIGRpcmVjdGlvbi55IH1cbiAgICBpZiAoZ2V0VGlsZShzdGF0ZS5mbG9vciwgbmV4dC54LCBuZXh0LnkpPy5raW5kICE9PSAncmFpbCcgfHwgaXNCbG9ja2luZ1Byb3AocHJvcEF0KHN0YXRlLmZsb29yLnByb3BzLCBuZXh0LngsIG5leHQueSkpKSBicmVha1xuICAgIGNvbnN0IGFjdG9yID0gYWN0b3JBdChzdGF0ZS5mbG9vciwgbmV4dC54LCBuZXh0LnkpXG4gICAgaWYgKGFjdG9yKSB7XG4gICAgICBpZiAoIWFjdG9yLmhvc3RpbGUpIHtcbiAgICAgICAgbG9nKHN0YXRlLCBgVGhlIGNhcnQgaXMgYmxvY2tlZCBieSAke2FjdG9yLm5hbWV9LmApXG4gICAgICAgIGJyZWFrXG4gICAgICB9XG4gICAgICBhY3Rvci5oZWFsdGggLT0gbW9kaWZ5SW5jb21pbmdEYW1hZ2UoYWN0b3IsIDgpXG4gICAgICBsb2coc3RhdGUsIGBUaGUgY2FydCBjcnVzaGVzICR7YWN0b3IubmFtZX0uYClcbiAgICAgIGV2ZW50cy5wdXNoKGV2ZW50KCdoaXQnKSlcbiAgICAgIGJyZWFrXG4gICAgfVxuICAgIGlmIChzdGF0ZS5oZXJvLnggPT09IG5leHQueCAmJiBzdGF0ZS5oZXJvLnkgPT09IG5leHQueSkge1xuICAgICAgZXZlbnRzLnB1c2goLi4uZGFtYWdlSGVybyhzdGF0ZSwgOCwgJ1RoZSByYWlsIGNhcnQnLCB0cnVlKSlcbiAgICAgIGJyZWFrXG4gICAgfVxuICAgIGNhcnQueCA9IG5leHQueFxuICAgIGNhcnQueSA9IG5leHQueVxuICAgIGlmICghZXZlbnRzLnNvbWUoZW50cnkgPT4gZW50cnkudHlwZSA9PT0gJ21vdmUnKSkgZXZlbnRzLnB1c2goZXZlbnQoJ21vdmUnKSlcbiAgICBldmVudHMucHVzaCguLi50cmlnZ2VyQ2FydEhhemFyZHMoc3RhdGUsIGNhcnQpKVxuICAgIGNvbnN0IG9wdGlvbnMgPSBjYXJ0Q2FuZGlkYXRlcyhzdGF0ZSwgY2FydCwgcHJldmlvdXMpXG4gICAgY29uc3Qgc3RyYWlnaHQgPSBvcHRpb25zLmZpbmQocG9pbnQgPT4gcG9pbnQueCAtIGNhcnQueCA9PT0gZGlyZWN0aW9uLnggJiYgcG9pbnQueSAtIGNhcnQueSA9PT0gZGlyZWN0aW9uLnkpXG4gICAgaWYgKHN0cmFpZ2h0KSB7XG4gICAgICBwcmV2aW91cyA9IHsgeDogY2FydC54LCB5OiBjYXJ0LnkgfVxuICAgICAgY29udGludWVcbiAgICB9XG4gICAgaWYgKG9wdGlvbnMubGVuZ3RoICE9PSAxKSBicmVha1xuICAgIGRpcmVjdGlvbiA9IHsgeDogb3B0aW9uc1swXS54IC0gY2FydC54LCB5OiBvcHRpb25zWzBdLnkgLSBjYXJ0LnkgfVxuICAgIHByZXZpb3VzID0geyB4OiBjYXJ0LngsIHk6IGNhcnQueSB9XG4gIH1cbiAgcmVzb2x2ZURlZmVhdGVkQWN0b3JzKHN0YXRlKVxuICBsb2coc3RhdGUsICdUaGUgY2FydCBncmluZHMgdG8gYSBoYWx0LicpXG4gIHJldHVybiBldmVudHNcbn1cblxuY29uc3QgcmV2ZWFsV2FybmluZ3MgPSAoc3RhdGU6IFJ1blN0YXRlLCBwcm9wOiBQcm9wLCBza3VsbDogYm9vbGVhbik6IG51bWJlciA9PiB7XG4gIGNvbnN0IHBvaW50cyA9IHN0YXRlLmZsb29yLnRpbGVzLmZsYXRNYXAoKHRpbGUsIGluZGV4KSA9PiBoYXphcmRLaW5kcy5oYXModGlsZS5raW5kKSA/IFt7IC4uLmZsb29yUG9pbnQoc3RhdGUuZmxvb3IsIGluZGV4KSwgcHJpb3JpdHk6IDAgfV0gOiBbXSlcbiAgaWYgKHNrdWxsKSBwb2ludHMucHVzaCguLi5zdGF0ZS5mbG9vci5hY3RvcnMuZmlsdGVyKGFjdG9yID0+IGFjdG9yLmhvc3RpbGUgJiYgYWN0b3IuaGVhbHRoID4gMCkubWFwKGFjdG9yID0+ICh7IHg6IGFjdG9yLngsIHk6IGFjdG9yLnksIHByaW9yaXR5OiAxIH0pKSlcbiAgY29uc3Qgd2FybmluZ3MgPSBwb2ludHNcbiAgICAuZmlsdGVyKHBvaW50ID0+IE1hdGgubWF4KE1hdGguYWJzKHBvaW50LnggLSBwcm9wLngpLCBNYXRoLmFicyhwb2ludC55IC0gcHJvcC55KSkgPD0gNSlcbiAgICAuc29ydCgoZmlyc3QsIHNlY29uZCkgPT4gZmlyc3QucHJpb3JpdHkgLSBzZWNvbmQucHJpb3JpdHkgfHwgTWF0aC5tYXgoTWF0aC5hYnMoZmlyc3QueCAtIHByb3AueCksIE1hdGguYWJzKGZpcnN0LnkgLSBwcm9wLnkpKSAtIE1hdGgubWF4KE1hdGguYWJzKHNlY29uZC54IC0gcHJvcC54KSwgTWF0aC5hYnMoc2Vjb25kLnkgLSBwcm9wLnkpKSB8fCBmaXJzdC55IC0gc2Vjb25kLnkgfHwgZmlyc3QueCAtIHNlY29uZC54KVxuICAgIC5zbGljZSgwLCAzKVxuICBmb3IgKGNvbnN0IHBvaW50IG9mIHdhcm5pbmdzKSBnZXRUaWxlKHN0YXRlLmZsb29yLCBwb2ludC54LCBwb2ludC55KSEuZXhwbG9yZWQgPSB0cnVlXG4gIHJldHVybiB3YXJuaW5ncy5sZW5ndGhcbn1cblxuY29uc3Qgb3BlcmF0ZU1pbmVQcm9wID0gKHN0YXRlOiBSdW5TdGF0ZSwgcHJvcDogUHJvcCk6IFByb3BPcGVyYXRpb24gfCB1bmRlZmluZWQgPT4ge1xuICBpZiAocHJvcC5raW5kID09PSAnbWluZS5vcmVWZWluJykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnRXF1aXAgYSBwaWNrYXhlLCB0aGVuIHByZXNzIEMgdG8gbWluZSBpdC4nKVxuICAgIGlmIChwcm9wLnN0YXRlICE9PSAnaW5zcGVjdGVkJykgeyBsb2coc3RhdGUsICdUaGUgdmVpbiBoYXMgYWxyZWFkeSBiZWVuIHdvcmtlZC4nKTsgcmV0dXJuIHsga2luZDogJ2V4YW1pbmVkJywgZXZlbnRzOiBbXSB9IH1cbiAgICBpZiAoc3RhdGUuaGVyby5lcXVpcG1lbnQubWFpbkhhbmQgIT09ICdwaWNrYXhlJykgeyBsb2coc3RhdGUsICdBIHBpY2theGUgaXMgcmVxdWlyZWQgdG8gd29yayB0aGlzIHZlaW4uJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgcHJvcC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHByb3AueCwgcHJvcC55KVxuICAgIGlmICh0aWxlKSB0aWxlLmtpbmQgPSAncnViYmxlJ1xuICAgIHJld2FyZChzdGF0ZSwgcHJvcCwgJ3JvY2snLCAyKVxuICAgIGxvZyhzdGF0ZSwgJ1lvdSBjaGlwIG9yZSBmcmVlIGFuZCBsZWF2ZSBhIG1vdW5kIG9mIHJ1YmJsZS4nKVxuICAgIHJldHVybiB7IGtpbmQ6ICdhY3RpdmF0ZWQnLCBldmVudHM6IFtldmVudCgncGlja3VwJyksIGV2ZW50KCdib29tJyldIH1cbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAnbWluZS5sYW50ZXJuUG9zdCcpIHtcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSByZXR1cm4gaW5zcGVjdChzdGF0ZSwgcHJvcCwgJ0FuIEVtYmVyIENoYXJtIG9yIEZpcmUgSmFyIGNhbiByZWxpZ2h0IGl0LicpXG4gICAgbG9nKHN0YXRlLCBwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJyA/ICdUaGUgbGFudGVybiBwb3N0IGJ1cm5zIHdpdGggYSBzdGVhZHkgbG9jYWwgZ2xvdy4nIDogJ1RoZSBsYW50ZXJuIG5lZWRzIGZsYW1lLCBub3QgYSBoYW5kLicpXG4gICAgcmV0dXJuIHsga2luZDogJ2V4YW1pbmVkJywgZXZlbnRzOiBbXSB9XG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ21pbmUuYnJva2VuQ2FydCcpIHtcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSByZXR1cm4gaW5zcGVjdChzdGF0ZSwgcHJvcCwgJ1N0YW5kIGJlc2lkZSBpdCBvbiBhIHJhaWwsIHRoZW4gcHJlc3MgQyB0byBwdXNoIGl0LicpXG4gICAgY29uc3QgZGlyZWN0aW9uID0geyB4OiBNYXRoLnNpZ24ocHJvcC54IC0gc3RhdGUuaGVyby54KSwgeTogTWF0aC5zaWduKHByb3AueSAtIHN0YXRlLmhlcm8ueSkgfVxuICAgIGlmICghY2FyZGluYWwoZGlyZWN0aW9uKSkgeyBsb2coc3RhdGUsICdTdGFuZCBvbiBhIGNhcmRpbmFsIHNpZGUgb2YgdGhlIGNhcnQgdG8gcHVzaCBpdC4nKTsgcmV0dXJuIHsga2luZDogJ2V4YW1pbmVkJywgZXZlbnRzOiBbXSB9IH1cbiAgICBjb25zdCBldmVudHMgPSBtb3ZlQ2FydChzdGF0ZSwgcHJvcCwgZGlyZWN0aW9uKVxuICAgIGlmICghZXZlbnRzLmxlbmd0aCkgcmV0dXJuIHsga2luZDogJ2V4YW1pbmVkJywgZXZlbnRzIH1cbiAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICByZXR1cm4geyBraW5kOiAnbW92ZWQnLCBldmVudHMgfVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdtaW5lLndhcm5pbmdNYXJrZXInIHx8IHByb3Aua2luZCA9PT0gJ21pbmUuc2t1bGxNYXJrZXInKSB7XG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdkb3JtYW50JykgcmV0dXJuIGluc3BlY3Qoc3RhdGUsIHByb3AsICdQcmVzcyBDIGFnYWluIHRvIHRyYWNlIGl0cyBsb2NhbCB3YXJuaW5nLicpXG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7IGxvZyhzdGF0ZSwgJ1RoZSB3YXJuaW5nIGhhcyBhbHJlYWR5IGJlZW4gdHJhY2VkLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIGNvbnN0IGNvdW50ID0gcmV2ZWFsV2FybmluZ3Moc3RhdGUsIHByb3AsIHByb3Aua2luZCA9PT0gJ21pbmUuc2t1bGxNYXJrZXInKVxuICAgIGxvZyhzdGF0ZSwgY291bnQgPyBgVGhlIG1hcmtlciBleHBvc2VzICR7Y291bnR9IG5lYXJieSBkYW5nZXIke2NvdW50ID09PSAxID8gJycgOiAncyd9LmAgOiAnVGhlIG1hcmtlciBwb2ludHMgdG8gbm8gbmVhcmJ5IGRhbmdlci4nKVxuICAgIHJldHVybiB7IGtpbmQ6ICdhY3RpdmF0ZWQnLCBldmVudHM6IFtldmVudCgnZGFuZ2VyJyldIH1cbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAnbWluZS5kaXNjYXJkZWRQYXJjZWwnKSB7XG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdkb3JtYW50JykgcmV0dXJuIGluc3BlY3Qoc3RhdGUsIHByb3AsICdQcmVzcyBDIGFnYWluIHRvIHJlY292ZXIgaXQsIG9yIGxlYXZlIGl0IHVudG91Y2hlZC4nKVxuICAgIGlmIChwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgeyBsb2coc3RhdGUsICdUaGUgcGFyY2VsIGhhcyBhbHJlYWR5IGJlZW4gcmVjb3ZlcmVkLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIHJld2FyZChzdGF0ZSwgcHJvcCwgcHJvcERlZmluaXRpb24ocHJvcC5raW5kKS5hY3RpdmF0aW9uUmV3YXJkKVxuICAgIGxvZyhzdGF0ZSwgJ1lvdSByZWNvdmVyIHRoZSBwYXJjZWwgYmVmb3JlIGl0cyBjaGFyZ2UgY2FuIGZsYXJlLicpXG4gICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdwaWNrdXAnKV0gfVxuICB9XG4gIHJldHVybiB1bmRlZmluZWRcbn1cblxuY29uc3Qgb3BlcmF0ZVdpbGRzUHJvcCA9IChzdGF0ZTogUnVuU3RhdGUsIHByb3A6IFByb3ApOiBQcm9wT3BlcmF0aW9uIHwgdW5kZWZpbmVkID0+IHtcbiAgaWYgKHByb3Aua2luZCA9PT0gJ3dpbGRzLm11c2hyb29tcycpIHtcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSByZXR1cm4gaW5zcGVjdChzdGF0ZSwgcHJvcCwgJ0hhcnZlc3QgdGhlbSB3aXRoIEMsIGJ1cm4gdGhlbSBmb3IgZmxhbWUsIG9yIGNydXNoIHRoZW0gdG8gcmVsZWFzZSBzcG9yZXMuJylcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2FjdGl2YXRlZCcpIHsgbG9nKHN0YXRlLCAnVGhlIG11c2hyb29tIHBhdGNoIGhhcyBhbHJlYWR5IGJlZW4gaGFydmVzdGVkLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIHJld2FyZChzdGF0ZSwgcHJvcCwgJ3RvbmljJylcbiAgICBsb2coc3RhdGUsICdZb3UgaGFydmVzdCBhIHZpdGFsIHRvbmljIGFuZCBsZWF2ZSB0aGUgc3BvcmVzIHVuZGlzdHVyYmVkLicpXG4gICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdwaWNrdXAnKV0gfVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICd3aWxkcy5kYW5nbGluZ0NoYXJtJykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnUHJlc3MgQyBhZ2FpbiB0byB0YWtlIGl0cyB3YXJkLCBvciB1c2UgYSBCcnVzaCBCbGFkZSwgUm9vdCwgb3IgZmlyZSB0byBjdXQgdGhlIG5lYXJieSBncm93dGguJylcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2FjdGl2YXRlZCcpIHsgbG9nKHN0YXRlLCAnVGhlIGJyYW5jaCBoYW5ncyBiYXJlLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIGlmIChzdGF0ZS5oZXJvLmVxdWlwbWVudC5tYWluSGFuZCA9PT0gJ21hY2hldGUnKSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2Rlc3Ryb3llZCdcbiAgICAgIGNvbnN0IGNsZWFyZWQgPSBjbGVhckJyYW1ibGUoc3RhdGUsIHByb3ApXG4gICAgICBsb2coc3RhdGUsIGNsZWFyZWQgPyAnWW91IGN1dCB0aGUgY2hhcm0gbG9vc2UgYW5kIGNsZWFyIGEgYnJhbWJsZSBjaG9rZSBwb2ludC4nIDogJ1lvdSBjdXQgdGhlIGNoYXJtIGxvb3NlIGJlZm9yZSBpdHMgY3Vyc2UgY2FuIHRha2UgaG9sZC4nKVxuICAgICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdtb3ZlJyldIH1cbiAgICB9XG4gICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgcmV3YXJkKHN0YXRlLCBwcm9wLCAnd2FyZFNjcmlwdCcpXG4gICAgbG9nKHN0YXRlLCAnWW91IHRha2UgdGhlIGNoYXJtIGFzIGEgd2FyZCBhbmQgbGVhdmUgdGhlIHJvb3RzIHVudG91Y2hlZC4nKVxuICAgIHJldHVybiB7IGtpbmQ6ICdhY3RpdmF0ZWQnLCBldmVudHM6IFtldmVudCgncGlja3VwJyldIH1cbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAnd2lsZHMuYmlyZE5lc3QnKSB7XG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdkb3JtYW50JykgcmV0dXJuIGluc3BlY3Qoc3RhdGUsIHByb3AsICdQcmVzcyBDIGFnYWluIHRvIGxvb3QgaXQsIG9yIHRocm93IG9yIGJ1cm4gaXQgdG8gc3RhcnRsZSB0aGUgZmxvY2suJylcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2FjdGl2YXRlZCcpIHsgbG9nKHN0YXRlLCAnVGhlIG5lc3QgaXMgZW1wdHkuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgcmV3YXJkKHN0YXRlLCBwcm9wLCAndG9uaWMnKVxuICAgIGxvZyhzdGF0ZSwgJ1lvdSB0YWtlIGEgdG9uaWMgd2l0aG91dCBkaXN0dXJiaW5nIHRoZSBuZXN0LicpXG4gICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdwaWNrdXAnKV0gfVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICd3aWxkcy5yb290U2hyaW5lJykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnT2ZmZXIgYSBSb290LCBNZW5kaW5nLCBvciBXYXJkIENoYXJtIHdpdGggQyB0byByYWlzZSBhIHNob3J0LWxpdmVkIHRob3JuIHNjcmVlbi4nKVxuICAgIGlmIChwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgeyBsb2coc3RhdGUsICdUaGUgc2hyaW5lIGhhcyBzcGVudCBpdHMgcm9vdHMuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgY29uc3QgY2hhcm0gPSBjb25zdW1lV2lsZHNDaGFybShzdGF0ZSlcbiAgICBpZiAoIWNoYXJtKSB7IGxvZyhzdGF0ZSwgJ1RoZSBzaHJpbmUgYW5zd2VycyBvbmx5IFJvb3QsIE1lbmRpbmcsIG9yIFdhcmQgQ2hhcm1zLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIGNvbnN0IGdyb3duID0gZ3Jvd0JyYW1ibGUoc3RhdGUsIHByb3ApXG4gICAgcHJvcC5lZmZlY3RDZWxscyA9IGdyb3duLm1hcChwb2ludCA9PiAoeyAuLi5wb2ludCB9KSlcbiAgICBwcm9wLmV4cGlyZXNBdCA9IHN0YXRlLnR1cm4gKyA0XG4gICAgaWYgKGNoYXJtID09PSAnbWVuZCcpIHN0YXRlLmhlcm8uaGVhbHRoID0gTWF0aC5taW4oc3RhdGUuaGVyby5tYXhIZWFsdGgsIHN0YXRlLmhlcm8uaGVhbHRoICsgNilcbiAgICBpZiAoY2hhcm0gPT09ICd3YXJkU2NyaXB0JykgYWRkQ29uZGl0aW9uKHN0YXRlLmhlcm8sIHsga2luZDogJ3NoaWVsZGVkJywgZHVyYXRpb246IDQsIHBvdGVuY3k6IDEgfSlcbiAgICBsb2coc3RhdGUsIGBUaGUgJHtjaGFybSA9PT0gJ3dhcmRTY3JpcHQnID8gJ3dhcmQnIDogY2hhcm19IGNoYXJtIHJhaXNlcyAke2dyb3duLmxlbmd0aCB8fCAnYSd9IHRob3JuIHNjcmVlbiBiZXNpZGUgdGhlIHNocmluZS5gKVxuICAgIHJldHVybiB7IGtpbmQ6ICdhY3RpdmF0ZWQnLCBldmVudHM6IFtldmVudCgnc3BlbGwnKV0gfVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICd3aWxkcy5sb3N0UGFyY2VsJykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnUHJlc3MgQyBhZ2FpbiB0byByZWNvdmVyIHJvcGUgYW5kIGN1dCBhIG5lYXJieSBicmFtYmxlIHJvdXRlLCBvciBsZWF2ZSB0aGUgcGFyY2VsIGZvciBsYXRlci4nKVxuICAgIGlmIChwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgeyBsb2coc3RhdGUsICdUaGUgbG9zdCBwYXJjZWwgaGFzIGFscmVhZHkgYmVlbiByZWNvdmVyZWQuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgcmV3YXJkKHN0YXRlLCBwcm9wLCAncm9wZUJ1bmRsZScpXG4gICAgY29uc3QgY2xlYXJlZCA9IGNsZWFyQnJhbWJsZShzdGF0ZSwgcHJvcClcbiAgICBsb2coc3RhdGUsIGNsZWFyZWQgPyAnWW91IHJlY292ZXIgcm9wZSBhbmQgdW5jb3ZlciBhIGNsZWFyIGFsdGVybmF0ZSB0cmFpbC4nIDogJ1lvdSByZWNvdmVyIHJvcGUgZnJvbSB0aGUgbG9zdCBwYXJjZWwuJylcbiAgICByZXR1cm4geyBraW5kOiAnYWN0aXZhdGVkJywgZXZlbnRzOiBbZXZlbnQoJ3BpY2t1cCcpXSB9XG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ3dpbGRzLnJvb3RBcmNoJykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnVXNlIGEgQnJ1c2ggQmxhZGUgd2l0aCBDLCBSb290IHRvIG9wZW4gYSBsaXZpbmcgZGV0b3VyLCBvciBidXJuIHRocm91Z2ggdGhlIGFyY2guJylcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2FjdGl2YXRlZCcpIHsgbG9nKHN0YXRlLCAnVGhlIHJvb3RzIGhvbGQgb3BlbiBhIGNsZWFyIHBhc3NhZ2UuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgaWYgKHN0YXRlLmhlcm8uZXF1aXBtZW50Lm1haW5IYW5kICE9PSAnbWFjaGV0ZScpIHsgbG9nKHN0YXRlLCAnQSBCcnVzaCBCbGFkZSwgUm9vdCBDaGFybSwgb3IgZmxhbWUgY2FuIG9wZW4gdGhlIGxpdmluZyBhcmNoLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIGxvZyhzdGF0ZSwgJ1lvdSBjdXQgYSBib3VuZGVkIHBhc3NhZ2UgdGhyb3VnaCB0aGUgbGl2aW5nIHJvb3RzLicpXG4gICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdtb3ZlJyldIH1cbiAgfVxuICByZXR1cm4gdW5kZWZpbmVkXG59XG5cbmNvbnN0IG9wZXJhdGVDYXZlcm5Qcm9wID0gKHN0YXRlOiBSdW5TdGF0ZSwgcHJvcDogUHJvcCk6IFByb3BPcGVyYXRpb24gfCB1bmRlZmluZWQgPT4ge1xuICBpZiAocHJvcC5raW5kID09PSAnY2F2ZXJucy5jcnlzdGFsQ2x1c3RlcicpIHtcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSByZXR1cm4gaW5zcGVjdChzdGF0ZSwgcHJvcCwgJ01pbmUgaXQgd2l0aCBhbiBPYnNpZGlhbiBBeGUsIGJsYXN0IGl0IGZvciBzaGFyZHMsIG9yIHVzZSBmb3JjZSB0byByZWZyYWN0IGxpbmUgZWZmZWN0cy4nKVxuICAgIGlmIChwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgeyBsb2coc3RhdGUsICdUaGUgY3J5c3RhbCByZWZyYWN0cyBzaWdodCBhbmQgbGluZSBlZmZlY3RzIHRocm91Z2ggaXRzIG9wZW5lZCBmYWNldHMuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgaWYgKHN0YXRlLmhlcm8uZXF1aXBtZW50Lm1haW5IYW5kICE9PSAncGlja2F4ZScpIHsgbG9nKHN0YXRlLCAnQW4gT2JzaWRpYW4gQXhlIGlzIHJlcXVpcmVkIHRvIG1pbmUgdGhlIGNyeXN0YWwgc2FmZWx5LicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIGRlc3Ryb3lDcnlzdGFsKHN0YXRlLCBwcm9wLCAncGlja2F4ZScpXG4gICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdwaWNrdXAnKSwgZXZlbnQoJ2Jvb20nKV0gfVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdjYXZlcm5zLmdsb3dpbmdGdW5ndXMnKSB7XG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdkb3JtYW50JykgcmV0dXJuIGluc3BlY3Qoc3RhdGUsIHByb3AsICdIYXJ2ZXN0IGl0IHdpdGggQyB0byBsb3NlIGl0cyBsaWdodCwgd2V0IGl0IHRvIGJyaWdodGVuIHRoZSBjYXZlLCBvciBidXJuIGl0IHRvIHJlbGVhc2Ugc3BvcmVzLicpXG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7IGxvZyhzdGF0ZSwgJ1RoZSBzb2FrZWQgZnVuZ3VzIHNwaWxscyBhIGJyb2FkLCBjb2xkIGxvY2FsIGdsb3cuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgcHJvcC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gICAgcmV3YXJkKHN0YXRlLCBwcm9wLCAnZm9jdXNUb25pYycpXG4gICAgbG9nKHN0YXRlLCAnWW91IGhhcnZlc3QgdGhlIGZ1bmd1cyBmb3IgZm9jdXMgYW5kIGV4dGluZ3Vpc2ggaXRzIGxvY2FsIGdsb3cuJylcbiAgICByZXR1cm4geyBraW5kOiAnYWN0aXZhdGVkJywgZXZlbnRzOiBbZXZlbnQoJ3BpY2t1cCcpXSB9XG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ2NhdmVybnMuYmFybmFjbGVkU2hyaW5lJykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnT2ZmZXIgYSBWaXRhbCBvciBGb2N1cyBUb25pYyB3aXRoIEMgZm9yIGEgdGVtcG9yYXJ5IGJyaW5lIHdhcmQsIG9yIHdldCB0aGUgc2hyaW5lIGRpcmVjdGx5LicpXG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7IGxvZyhzdGF0ZSwgJ1RoZSBicmluZSB3YXJkIHN0aWxsIHNoaW1tZXJzIGFyb3VuZCB0aGUgc2hyaW5lLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIGNvbnN0IG9mZmVyaW5nID0gc3RhdGUuaGVyby5pbnZlbnRvcnkuZmluZChpdGVtID0+IGl0ZW0gPT09ICd0b25pYycgfHwgaXRlbSA9PT0gJ2ZvY3VzVG9uaWMnKVxuICAgIGlmICghb2ZmZXJpbmcpIHsgbG9nKHN0YXRlLCAnVGhlIHNocmluZSBhc2tzIGZvciBhIFZpdGFsIG9yIEZvY3VzIFRvbmljLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHN0YXRlLmhlcm8uaW52ZW50b3J5LnNwbGljZShzdGF0ZS5oZXJvLmludmVudG9yeS5pbmRleE9mKG9mZmVyaW5nKSwgMSlcbiAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICBjb25zdCBmbG9vZGVkID0gZmxvb2RCcmluZShzdGF0ZSwgcHJvcClcbiAgICBwcm9wLmVmZmVjdENlbGxzID0gZmxvb2RlZC5tYXAocG9pbnQgPT4gKHsgLi4ucG9pbnQgfSkpXG4gICAgcHJvcC5leHBpcmVzQXQgPSBzdGF0ZS50dXJuICsgNFxuICAgIGFkZENvbmRpdGlvbihzdGF0ZS5oZXJvLCB7IGtpbmQ6ICdzaGllbGRlZCcsIGR1cmF0aW9uOiA0LCBwb3RlbmN5OiAxIH0pXG4gICAgbG9nKHN0YXRlLCBgVGhlIGJyaW5lIG9mZmVyaW5nIHJhaXNlcyBhIHdhcmQgYW5kIGZsb29kcyAke2Zsb29kZWQubGVuZ3RoIHx8ICdhJ30gbmVhcmJ5IGNoYW5uZWwuYClcbiAgICByZXR1cm4geyBraW5kOiAnYWN0aXZhdGVkJywgZXZlbnRzOiBbZXZlbnQoJ3NwZWxsJyldIH1cbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAnY2F2ZXJucy5icm9rZW5Cb2F0Jykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnUHJlc3MgUiBiZXNpZGUgdGhlIGJvYXQgdG8gc3BlbmQgYSByb3BlIGFuZCBhbmNob3IgdGhpcyB3YXRlciBjcm9zc2luZy4nKVxuICAgIGlmIChwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgeyBsb2coc3RhdGUsICdUaGUgYW5jaG9yZWQgYm9hdCBob2xkcyBhIHNhZmUgY3Jvc3NpbmcuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgbG9nKHN0YXRlLCAnVGhlIGJvYXQgaXMgcmVhZHkgZm9yIGEgcm9wZSBhbmNob3IuJylcbiAgICByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH1cbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAnY2F2ZXJucy5lZWxUdW5uZWwnKSB7XG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdkb3JtYW50JykgcmV0dXJuIGluc3BlY3Qoc3RhdGUsIHByb3AsICdUaGUgb3BlbiB0dW5uZWwgaXMgYSBzaG9ydGN1dCBhbmQgYSBmdW1lLWVlbCBvcmlnaW4uIFByZXNzIEMgYWdhaW4gd2l0aCBhIGJvbWIgdG8gc2VhbCBpdDsgZm9yY2UgcmVvcGVucyBpdC4nKVxuICAgIGlmIChwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgeyBsb2coc3RhdGUsICdUaGUgZWVsIHR1bm5lbCBpcyBzZWFsZWQ7IGZvcmNlIGNhbiByZW9wZW4gdGhlIHNob3J0Y3V0LicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIGlmIChzdGF0ZS5oZXJvLmJvbWJzIDwgMSkgeyBsb2coc3RhdGUsICdBIGJvbWIgaXMgcmVxdWlyZWQgdG8gc2VhbCB0aGUgZWVsIHR1bm5lbC4nKTsgcmV0dXJuIHsga2luZDogJ2V4YW1pbmVkJywgZXZlbnRzOiBbXSB9IH1cbiAgICBpZiAoIWNhblNlYWxFZWxUdW5uZWwoc3RhdGUsIHByb3ApKSB7IGxvZyhzdGF0ZSwgJ1NlYWxpbmcgdGhpcyB0dW5uZWwgd291bGQgY2xvc2UgdGhlIHJlcXVpcmVkIHRyYWlsLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHN0YXRlLmhlcm8uYm9tYnMtLVxuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIGxvZyhzdGF0ZSwgJ1lvdSBzZWFsIHRoZSBlZWwgdHVubmVsIGFuZCBjbG9zZSBpdHMgc2hvcnRjdXQuJylcbiAgICByZXR1cm4geyBraW5kOiAnYWN0aXZhdGVkJywgZXZlbnRzOiBbZXZlbnQoJ2Jvb20nKV0gfVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdjYXZlcm5zLnNlYWxlZFBhcmNlbCcpIHtcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSByZXR1cm4gaW5zcGVjdChzdGF0ZSwgcHJvcCwgJ1ByZXNzIEMgYWdhaW4gdG8gc3BlbmQgYSBrZXksIGJvbWIsIFN1bnN0b25lIFNlYWwsIG9yIFdhcmQgQ2hhcm0gYW5kIG9wZW4gdGhlIGNhY2hlLicpXG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7IGxvZyhzdGF0ZSwgJ1RoZSBzZWFsZWQgcGFyY2VsIGhhcyBhbHJlYWR5IGJlZW4gb3BlbmVkLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIGNvbnN0IGtleSA9IGNvbnN1bWVDYXZlcm5DYWNoZUtleShzdGF0ZSlcbiAgICBpZiAoIWtleSkgeyBsb2coc3RhdGUsICdUaGUgd2F4IHNlYWwgcmVzaXN0czogYnJpbmcgYSBrZXksIGJvbWIsIFN1bnN0b25lIFNlYWwsIG9yIFdhcmQgQ2hhcm0uJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgcmV3YXJkKHN0YXRlLCBwcm9wLCAnZm9jdXNUb25pYycpXG4gICAgbG9nKHN0YXRlLCBgWW91IHNwZW5kICR7a2V5ID09PSAnd2FyZFNjcmlwdCcgPyAnYSBXYXJkIENoYXJtJyA6IGtleSA9PT0gJ3N1bnNlYWwnID8gJ3RoZSBTdW5zdG9uZSBTZWFsJyA6IGBhICR7a2V5fWB9IHRvIG9wZW4gdGhlIHBhcmNlbC5gKVxuICAgIHJldHVybiB7IGtpbmQ6ICdhY3RpdmF0ZWQnLCBldmVudHM6IFtldmVudCgncGlja3VwJyldIH1cbiAgfVxuICByZXR1cm4gdW5kZWZpbmVkXG59XG5cbmNvbnN0IG9wZXJhdGVSdWluc1Byb3AgPSAoc3RhdGU6IFJ1blN0YXRlLCBwcm9wOiBQcm9wKTogUHJvcE9wZXJhdGlvbiB8IHVuZGVmaW5lZCA9PiB7XG4gIGlmIChwcm9wLmtpbmQgPT09ICdydWlucy5icm9rZW5TdGF0dWUnKSB7XG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdkb3JtYW50JykgcmV0dXJuIGluc3BlY3Qoc3RhdGUsIHByb3AsICdQcmVzcyBDIHRvIHRvcHBsZSBpdCBpbnRvIHNpZ2h0LWJsb2NraW5nIGNvdmVyLCB1c2UgZm9yY2UgdG8gc2hvdmUgaXQsIG9yIGJsYXN0IGl0IGludG8gc2FmZSBydWJibGUuJylcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2FjdGl2YXRlZCcpIHsgbG9nKHN0YXRlLCAnVGhlIHRvcHBsZWQgc3RhdHVlIHN0aWxsIGJsb2NrcyBsaW5lIGVmZmVjdHMgYXMgY292ZXIuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgbG9nKHN0YXRlLCAnWW91IHRvcHBsZSB0aGUgc3RhdHVlIGludG8gYSBsaW5lLWJsb2NraW5nIGNvdmVyIHBvc2l0aW9uLicpXG4gICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdtb3ZlJyldIH1cbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAncnVpbnMucml0dWFsQnJhemllcicpIHtcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSByZXR1cm4gaW5zcGVjdChzdGF0ZSwgcHJvcCwgJ0Z1ZWwgaXQgd2l0aCBhbiBFbWJlciBDaGFybSB1c2luZyBDLCBxdWVuY2ggaXQgd2l0aCB3YXRlciwgb3IgdGFyZ2V0IGl0IHdpdGggV2FyZCBvciBHYXRlOyBlYWNoIHNwZWxsIGV4cG9zZXMgYSBsb2NhbCBjb3N0LicpXG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7IGxvZyhzdGF0ZSwgJ1RoZSByaXR1YWwgYnJhemllciBidXJucyB3aXRoIGEgd2FyZGVkIGxvY2FsIGZsYW1lLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIGNvbnN0IGVtYmVyID0gc3RhdGUuaGVyby5pbnZlbnRvcnkuaW5kZXhPZignZW1iZXInKVxuICAgIGlmIChlbWJlciA8IDApIHsgbG9nKHN0YXRlLCAnQW4gRW1iZXIgQ2hhcm0gaXMgcmVxdWlyZWQgdG8gZnVlbCB0aGUgYnJhemllci4nKTsgcmV0dXJuIHsga2luZDogJ2V4YW1pbmVkJywgZXZlbnRzOiBbXSB9IH1cbiAgICBzdGF0ZS5oZXJvLmludmVudG9yeS5zcGxpY2UoZW1iZXIsIDEpXG4gICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHByb3AueCwgcHJvcC55KVxuICAgIGlmICh0aWxlPy5raW5kID09PSAnZmxvb3InKSB0aWxlLmtpbmQgPSAnZmlyZVZlbnQnXG4gICAgbG9nKHN0YXRlLCAnWW91IGZ1ZWwgdGhlIGJyYXppZXI7IGl0cyBmbGFtZSBpcyBub3cgYSB2aXNpYmxlIGxvY2FsIGhhemFyZC4nKVxuICAgIHJldHVybiB7IGtpbmQ6ICdhY3RpdmF0ZWQnLCBldmVudHM6IFtldmVudCgnc3BlbGwnKV0gfVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdydWlucy5nbHlwaFRhYmxldCcpIHtcbiAgICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSByZXR1cm4gaW5zcGVjdChzdGF0ZSwgcHJvcCwgJ1ByZXNzIEMgYWdhaW4gdG8gcmVhZCBuZWFyYnkgdGVsZWdyYXBoIHRpbWluZyBhbmQgdGhlIGZsb29yIHB1enpsZSBydWxlLicpXG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7IGxvZyhzdGF0ZSwgJ1RoZSB0YWJsZXQgaGFzIGFscmVhZHkgeWllbGRlZCBpdHMgdGFjdGljYWwgd2FybmluZy4nKTsgcmV0dXJuIHsga2luZDogJ2V4YW1pbmVkJywgZXZlbnRzOiBbXSB9IH1cbiAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICBjb25zdCB0aHJlYXRzID0gKHN0YXRlLmZsb29yLnRlbGVncmFwaHMgPz8gW10pLmZpbHRlcih0ZWxlZ3JhcGggPT4gTWF0aC5tYXgoTWF0aC5hYnModGVsZWdyYXBoLmNlbGxzWzBdPy54IC0gcHJvcC54KSwgTWF0aC5hYnModGVsZWdyYXBoLmNlbGxzWzBdPy55IC0gcHJvcC55KSkgPD0gNilcbiAgICBjb25zdCB0aW1pbmcgPSB0aHJlYXRzLmxlbmd0aCA/IHRocmVhdHMubWFwKHRlbGVncmFwaCA9PiBgJHt0ZWxlZ3JhcGguYWN0aW9uSWR9IGluICR7TWF0aC5tYXgoMCwgdGVsZWdyYXBoLnJlc29sdmVUdXJuIC0gc3RhdGUudHVybil9YCkuam9pbignLCAnKSA6ICdubyBhY3RpdmUgbmVhcmJ5IHRlbGVncmFwaHMnXG4gICAgY29uc3QgcHV6emxlID0gc3RhdGUuZmxvb3IucHV6emxlSWRzPy5qb2luKCcsICcpIHx8ICdubyBmbG9vciBwdXp6bGUgbWFya2VyJ1xuICAgIGxvZyhzdGF0ZSwgYEdseXBoIHRhYmxldDogJHt0aW1pbmd9OyBwdXp6bGUgcnVsZTogJHtwdXp6bGV9LmApXG4gICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdkYW5nZXInKV0gfVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdydWlucy5jb2xsYXBzZWRBcmNoJykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnVXNlIGFuIE9ic2lkaWFuIEF4ZSB3aXRoIEMsIGEgYm9tYiwgb3IgYSByb3BlIGJyYWNlIHRvIG9wZW4gdGhpcyBibG9ja2VkIHJvdXRlLicpXG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7IGxvZyhzdGF0ZSwgJ1RoZSBjb2xsYXBzZWQgYXJjaCBpcyBicmFjZWQgb3Blbi4nKTsgcmV0dXJuIHsga2luZDogJ2V4YW1pbmVkJywgZXZlbnRzOiBbXSB9IH1cbiAgICBpZiAoc3RhdGUuaGVyby5lcXVpcG1lbnQubWFpbkhhbmQgIT09ICdwaWNrYXhlJykgeyBsb2coc3RhdGUsICdBbiBPYnNpZGlhbiBBeGUsIGJvbWIsIG9yIHJvcGUgY2FuIG9wZW4gdGhlIGNvbGxhcHNlZCBhcmNoLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIGxvZyhzdGF0ZSwgJ1lvdSBjdXQgYSBzdGFibGUgcGFzc2FnZSB0aHJvdWdoIHRoZSBjb2xsYXBzZWQgYXJjaC4nKVxuICAgIHJldHVybiB7IGtpbmQ6ICdhY3RpdmF0ZWQnLCBldmVudHM6IFtldmVudCgnYm9vbScpXSB9XG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ3J1aW5zLnNlYWxlZENhY2hlJykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZG9ybWFudCcpIHJldHVybiBpbnNwZWN0KHN0YXRlLCBwcm9wLCAnUHJlc3MgQyBhZ2FpbiB0byBzcGVuZCBhIGtleSwgYm9tYiwgU3Vuc3RvbmUgU2VhbCwgb3IgV2FyZCBDaGFybSBvbiB0aGlzIHZpc2libGUgbG9jay4nKVxuICAgIGlmIChwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgeyBsb2coc3RhdGUsICdUaGUgc2VhbGVkIGNhY2hlIGhhcyBhbHJlYWR5IGJlZW4gb3BlbmVkLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIGNvbnN0IGtleSA9IGNvbnN1bWVSdWluc0NhY2hlS2V5KHN0YXRlKVxuICAgIGlmICgha2V5KSB7IGxvZyhzdGF0ZSwgJ1RoZSBjYWNoZSBsb2NrIG5lZWRzIGEga2V5LCBib21iLCBTdW5zdG9uZSBTZWFsLCBvciBXYXJkIENoYXJtLicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIHJld2FyZChzdGF0ZSwgcHJvcCwgJ3N1bnNlYWwnKVxuICAgIGxvZyhzdGF0ZSwgYFlvdSBzcGVuZCAke2tleSA9PT0gJ3dhcmRTY3JpcHQnID8gJ2EgV2FyZCBDaGFybScgOiBrZXkgPT09ICdzdW5zZWFsJyA/ICd0aGUgU3Vuc3RvbmUgU2VhbCcgOiBgYSAke2tleX1gfSBhbmQgY2xhaW0gdGhlIHNlYWxlZCBjYWNoZS5gKVxuICAgIHJldHVybiB7IGtpbmQ6ICdhY3RpdmF0ZWQnLCBldmVudHM6IFtldmVudCgncGlja3VwJyldIH1cbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAncnVpbnMubW9ub2xpdGgnKSB7XG4gICAgaWYgKHByb3Auc3RhdGUgPT09ICdkb3JtYW50JykgcmV0dXJuIGluc3BlY3Qoc3RhdGUsIHByb3AsICdTcGVuZCAyIGZvY3VzIHdpdGggQyB0byBhcm0gYSBmb3VyLXR1cm4gd2FyZCB0aGF0IGFic29yYnMgb25lIG5lYXJieSB0ZWxlZ3JhcGggYnV0IG1hcmtzIHlvdS4nKVxuICAgIGlmIChwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgeyBsb2coc3RhdGUsICdUaGUgbW9ub2xpdGggaXMgYXJtZWQ7IGl0cyB3YXJkIHdpbGwgYWJzb3JiIG9uZSBsb2NhbCB0ZWxlZ3JhcGggYXQgYSBjb3N0LicpOyByZXR1cm4geyBraW5kOiAnZXhhbWluZWQnLCBldmVudHM6IFtdIH0gfVxuICAgIGlmIChzdGF0ZS5oZXJvLmZvY3VzIDwgMikgeyBsb2coc3RhdGUsICdUaGUgbW9ub2xpdGggcmVxdWlyZXMgMiBmb2N1cyB0byBpbnZva2UuJyk7IHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfSB9XG4gICAgc3RhdGUuaGVyby5mb2N1cyAtPSAyXG4gICAgYXJtTW9ub2xpdGgoc3RhdGUsIHByb3AsICdGb2N1cycpXG4gICAgcmV0dXJuIHsga2luZDogJ2FjdGl2YXRlZCcsIGV2ZW50czogW2V2ZW50KCdzcGVsbCcpLCBldmVudCgnZGFuZ2VyJyldIH1cbiAgfVxuICByZXR1cm4gdW5kZWZpbmVkXG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgUHJvcE9wZXJhdGlvbiB7IGtpbmQ6ICdleGFtaW5lZCcgfCAnYWN0aXZhdGVkJyB8ICdtb3ZlZCc7IGV2ZW50czogQWN0aW9uUmVzdWx0IH1cblxuZXhwb3J0IGNvbnN0IG9wZXJhdGVQcm9wID0gKHN0YXRlOiBSdW5TdGF0ZSk6IFByb3BPcGVyYXRpb24gfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBwcm9wID0gbmVhcmJ5UHJvcChzdGF0ZSlcbiAgaWYgKCFwcm9wIHx8ICFwcm9wLmhvb2tzPy5pbmNsdWRlcygnb3BlcmF0ZScpKSByZXR1cm4gdW5kZWZpbmVkXG4gIGlmIChtaW5lUHJvcChwcm9wKSkgcmV0dXJuIG9wZXJhdGVNaW5lUHJvcChzdGF0ZSwgcHJvcClcbiAgaWYgKHdpbGRzUHJvcChwcm9wKSkgcmV0dXJuIG9wZXJhdGVXaWxkc1Byb3Aoc3RhdGUsIHByb3ApXG4gIGlmIChjYXZlcm5Qcm9wKHByb3ApKSByZXR1cm4gb3BlcmF0ZUNhdmVyblByb3Aoc3RhdGUsIHByb3ApXG4gIGlmIChydWluc1Byb3AocHJvcCkpIHJldHVybiBvcGVyYXRlUnVpbnNQcm9wKHN0YXRlLCBwcm9wKVxuICBjb25zdCBkZWZpbml0aW9uID0gcHJvcERlZmluaXRpb24ocHJvcC5raW5kKVxuICBpZiAocHJvcC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSByZXR1cm4gaW5zcGVjdChzdGF0ZSwgcHJvcCwgJ1ByZXNzIEMgYWdhaW4gdG8gYWN0aXZhdGUgaXQuJylcbiAgaWYgKHByb3Auc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7XG4gICAgbG9nKHN0YXRlLCBgVGhlICR7ZGVmaW5pdGlvbi5uYW1lfSBoYXMgYWxyZWFkeSBiZWVuIGFjdGl2YXRlZC5gKVxuICAgIHJldHVybiB7IGtpbmQ6ICdleGFtaW5lZCcsIGV2ZW50czogW10gfVxuICB9XG4gIGlmIChwcm9wLnN0YXRlICE9PSAnaW5zcGVjdGVkJykgcmV0dXJuIHVuZGVmaW5lZFxuICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgcmV3YXJkKHN0YXRlLCBwcm9wLCBkZWZpbml0aW9uLmFjdGl2YXRpb25SZXdhcmQpXG4gIGxvZyhzdGF0ZSwgYFlvdSBzdHVkeSB0aGUgJHtkZWZpbml0aW9uLm5hbWV9IGFuZCByZWNvdmVyICR7ZGVmaW5pdGlvbi5hY3RpdmF0aW9uUmV3YXJkfS5gKVxuICByZXR1cm4geyBraW5kOiAnYWN0aXZhdGVkJywgZXZlbnRzOiBbZXZlbnQoJ3BpY2t1cCcpXSB9XG59XG5cbmV4cG9ydCBjb25zdCBtb3ZlQ2FydEJ5Rm9yY2UgPSAoc3RhdGU6IFJ1blN0YXRlLCBwb2ludDogUG9pbnQsIHB1bGw6IGJvb2xlYW4pOiBBY3Rpb25SZXN1bHQgfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBjYXJ0ID0gcHJvcEF0KHN0YXRlLmZsb29yLnByb3BzLCBwb2ludC54LCBwb2ludC55KVxuICBpZiAoY2FydD8ua2luZCAhPT0gJ21pbmUuYnJva2VuQ2FydCcgfHwgY2FydC5zdGF0ZSA9PT0gJ2Rlc3Ryb3llZCcpIHJldHVybiB1bmRlZmluZWRcbiAgY29uc3QgYXdheSA9IHsgeDogTWF0aC5zaWduKGNhcnQueCAtIHN0YXRlLmhlcm8ueCksIHk6IE1hdGguc2lnbihjYXJ0LnkgLSBzdGF0ZS5oZXJvLnkpIH1cbiAgY29uc3QgZGlyZWN0aW9uID0gcHVsbCA/IHsgeDogLWF3YXkueCwgeTogLWF3YXkueSB9IDogYXdheVxuICBpZiAoIWNhcmRpbmFsKGRpcmVjdGlvbikpIHsgbG9nKHN0YXRlLCAnVGhlIGNhcnQgY2Fubm90IGZpbmQgYSByYWlsLWFsaWduZWQgZm9yY2UgcGF0aC4nKTsgcmV0dXJuIFtdIH1cbiAgY29uc3QgZXZlbnRzID0gbW92ZUNhcnQoc3RhdGUsIGNhcnQsIGRpcmVjdGlvbilcbiAgaWYgKGV2ZW50cy5sZW5ndGgpIGNhcnQuc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICByZXR1cm4gZXZlbnRzXG59XG5cbmV4cG9ydCBjb25zdCByZWxlYXNlQ2FydFdpdGhSb3BlID0gKHN0YXRlOiBSdW5TdGF0ZSk6IEFjdGlvblJlc3VsdCB8IHVuZGVmaW5lZCA9PiB7XG4gIGNvbnN0IGNhcnQgPSBPYmplY3QudmFsdWVzKERJUkVDVElPTlMpXG4gICAgLmZpbHRlcihjYXJkaW5hbClcbiAgICAubWFwKGRlbHRhID0+IHByb3BBdChzdGF0ZS5mbG9vci5wcm9wcywgc3RhdGUuaGVyby54ICsgZGVsdGEueCwgc3RhdGUuaGVyby55ICsgZGVsdGEueSkpXG4gICAgLmZpbmQoKHByb3ApOiBwcm9wIGlzIFByb3AgPT4gcHJvcD8ua2luZCA9PT0gJ21pbmUuYnJva2VuQ2FydCcgJiYgcHJvcC5zdGF0ZSAhPT0gJ2Rlc3Ryb3llZCcpXG4gIGlmICghY2FydCkgcmV0dXJuIHVuZGVmaW5lZFxuICBpZiAoY2FydC5zdGF0ZSA9PT0gJ2Rvcm1hbnQnKSB7IGxvZyhzdGF0ZSwgJ0V4YW1pbmUgdGhlIGNhcnQgYmVmb3JlIHJpZ2dpbmcgaXQgd2l0aCBhIHJvcGUuJyk7IHJldHVybiBbXSB9XG4gIGNvbnN0IGRpcmVjdGlvbiA9IHsgeDogTWF0aC5zaWduKGNhcnQueCAtIHN0YXRlLmhlcm8ueCksIHk6IE1hdGguc2lnbihjYXJ0LnkgLSBzdGF0ZS5oZXJvLnkpIH1cbiAgY29uc3QgZXZlbnRzID0gbW92ZUNhcnQoc3RhdGUsIGNhcnQsIGRpcmVjdGlvbilcbiAgaWYgKGV2ZW50cy5sZW5ndGgpIGNhcnQuc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICByZXR1cm4gZXZlbnRzXG59XG5cbmV4cG9ydCBjb25zdCBhbmNob3JCb2F0V2l0aFJvcGUgPSAoc3RhdGU6IFJ1blN0YXRlKTogQWN0aW9uUmVzdWx0IHwgdW5kZWZpbmVkID0+IHtcbiAgY29uc3QgYm9hdCA9IE9iamVjdC52YWx1ZXMoRElSRUNUSU9OUylcbiAgICAuZmlsdGVyKGNhcmRpbmFsKVxuICAgIC5tYXAoZGVsdGEgPT4gcHJvcEF0KHN0YXRlLmZsb29yLnByb3BzLCBzdGF0ZS5oZXJvLnggKyBkZWx0YS54LCBzdGF0ZS5oZXJvLnkgKyBkZWx0YS55KSlcbiAgICAuZmluZCgocHJvcCk6IHByb3AgaXMgUHJvcCA9PiBwcm9wPy5raW5kID09PSAnY2F2ZXJucy5icm9rZW5Cb2F0JyAmJiBwcm9wLnN0YXRlICE9PSAnZGVzdHJveWVkJylcbiAgaWYgKCFib2F0KSByZXR1cm4gdW5kZWZpbmVkXG4gIGlmIChib2F0LnN0YXRlID09PSAnZG9ybWFudCcpIHsgbG9nKHN0YXRlLCAnRXhhbWluZSB0aGUgYm9hdCBiZWZvcmUgYW5jaG9yaW5nIGl0LicpOyByZXR1cm4gW10gfVxuICBpZiAoYm9hdC5zdGF0ZSA9PT0gJ2FjdGl2YXRlZCcpIHsgbG9nKHN0YXRlLCAnVGhlIGJvYXQgaXMgYWxyZWFkeSBhbmNob3JlZC4nKTsgcmV0dXJuIFtdIH1cbiAgYm9hdC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gIGxvZyhzdGF0ZSwgJ1RoZSByb3BlIGRyYXdzIHRoZSBib2F0IGludG8gYSBzdGFibGUgY3Jvc3NpbmcuJylcbiAgcmV0dXJuIFtldmVudCgnbW92ZScpXVxufVxuXG5leHBvcnQgY29uc3Qgc2VjdXJlQ29sbGFwc2VkQXJjaFdpdGhSb3BlID0gKHN0YXRlOiBSdW5TdGF0ZSk6IEFjdGlvblJlc3VsdCB8IHVuZGVmaW5lZCA9PiB7XG4gIGNvbnN0IGFyY2ggPSBPYmplY3QudmFsdWVzKERJUkVDVElPTlMpXG4gICAgLmZpbHRlcihjYXJkaW5hbClcbiAgICAubWFwKGRlbHRhID0+IHByb3BBdChzdGF0ZS5mbG9vci5wcm9wcywgc3RhdGUuaGVyby54ICsgZGVsdGEueCwgc3RhdGUuaGVyby55ICsgZGVsdGEueSkpXG4gICAgLmZpbmQoKHByb3ApOiBwcm9wIGlzIFByb3AgPT4gcHJvcD8ua2luZCA9PT0gJ3J1aW5zLmNvbGxhcHNlZEFyY2gnICYmIHByb3Auc3RhdGUgIT09ICdkZXN0cm95ZWQnKVxuICBpZiAoIWFyY2gpIHJldHVybiB1bmRlZmluZWRcbiAgaWYgKGFyY2guc3RhdGUgPT09ICdkb3JtYW50JykgeyBsb2coc3RhdGUsICdFeGFtaW5lIHRoZSBjb2xsYXBzZWQgYXJjaCBiZWZvcmUgYnJhY2luZyBpdC4nKTsgcmV0dXJuIFtdIH1cbiAgaWYgKGFyY2guc3RhdGUgPT09ICdhY3RpdmF0ZWQnKSB7IGxvZyhzdGF0ZSwgJ1RoZSBjb2xsYXBzZWQgYXJjaCBpcyBhbHJlYWR5IGJyYWNlZC4nKTsgcmV0dXJuIFtdIH1cbiAgYXJjaC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gIGxvZyhzdGF0ZSwgJ1RoZSByb3BlIGJyYWNlcyBhIHNhZmUgcm91dGUgdGhyb3VnaCB0aGUgY29sbGFwc2VkIGFyY2guJylcbiAgcmV0dXJuIFtldmVudCgnbW92ZScpXVxufVxuXG5jb25zdCBkZXN0cm95UHJvcCA9IChzdGF0ZTogUnVuU3RhdGUsIHByb3A6IFByb3AsIGVmZmVjdDogUHJvcEVmZmVjdEtpbmQpOiB2b2lkID0+IHtcbiAgY29uc3QgZGVmaW5pdGlvbiA9IHByb3BEZWZpbml0aW9uKHByb3Aua2luZClcbiAgcHJvcC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gIHJld2FyZChzdGF0ZSwgcHJvcCwgZGVmaW5pdGlvbi5lZmZlY3RSZXdhcmQpXG4gIGxvZyhzdGF0ZSwgYFRoZSAke2VmZmVjdH0gYnJlYWtzIHRoZSAke2RlZmluaXRpb24ubmFtZX07IGl0IGxlYXZlcyAke2RlZmluaXRpb24uZWZmZWN0UmV3YXJkfS5gKVxufVxuXG5leHBvcnQgY29uc3QgZXhwaXJlUHJvcEVmZmVjdHMgPSAoc3RhdGU6IFJ1blN0YXRlKTogdm9pZCA9PiB7XG4gIGZvciAoY29uc3QgcHJvcCBvZiBzdGF0ZS5mbG9vci5wcm9wcykge1xuICAgIGlmIChwcm9wLmV4cGlyZXNBdCA9PT0gdW5kZWZpbmVkIHx8IHByb3AuZXhwaXJlc0F0ID4gc3RhdGUudHVybikgY29udGludWVcbiAgICBjbGVhckVmZmVjdENlbGxzKHN0YXRlLCBwcm9wKVxuICAgIHByb3AuZXhwaXJlc0F0ID0gdW5kZWZpbmVkXG4gICAgaWYgKHByb3Aua2luZCA9PT0gJ3dpbGRzLnJvb3RTaHJpbmUnKSBsb2coc3RhdGUsICdUaGUgc2hyaW5lXFwncyB0aG9ybiBzY3JlZW4gd2l0aGVycyBhd2F5LicpXG4gICAgaWYgKHByb3Aua2luZCA9PT0gJ2NhdmVybnMuYmFybmFjbGVkU2hyaW5lJykgbG9nKHN0YXRlLCAnVGhlIGJyaW5lIGNoYW5uZWxzIGRyYWluIGFuZCB0aGUgd2FyZCByZWNlZGVzLicpXG4gICAgaWYgKHByb3Aua2luZCA9PT0gJ3J1aW5zLm1vbm9saXRoJykgbG9nKHN0YXRlLCAnVGhlIG1vbm9saXRoXFwncyB1bnN0YWJsZSB3YXJkIGV4cGlyZXMgd2l0aG91dCBhIHRlbGVncmFwaC4nKVxuICAgIGlmIChwcm9wLmtpbmQgPT09ICd3aWxkcy5iaXJkTmVzdCcpIHtcbiAgICAgIHN0YXRlLmZsb29yLmFjdG9ycyA9IHN0YXRlLmZsb29yLmFjdG9ycy5maWx0ZXIoYWN0b3IgPT4gYWN0b3IuaWQgIT09IGAke3Byb3AuaWR9OmZsb2NrYClcbiAgICAgIGxvZyhzdGF0ZSwgJ1RoZSBzdGFydGxlZCBiaXJkcyBzY2F0dGVyIGJhY2sgaW50byB0aGUgY2Fub3B5LicpXG4gICAgfVxuICB9XG59XG5cbmNvbnN0IGFwcGx5V2lsZHNFZmZlY3QgPSAoc3RhdGU6IFJ1blN0YXRlLCBwcm9wOiBQcm9wLCBlZmZlY3Q6IFByb3BFZmZlY3RLaW5kKTogYm9vbGVhbiA9PiB7XG4gIGlmIChwcm9wLmtpbmQgPT09ICd3aWxkcy5tdXNocm9vbXMnKSB7XG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHByb3AueCwgcHJvcC55KVxuICAgIHByb3Auc3RhdGUgPSBlZmZlY3QgPT09ICd3YXRlcicgPyAnYWN0aXZhdGVkJyA6ICdkZXN0cm95ZWQnXG4gICAgaWYgKGVmZmVjdCA9PT0gJ3dhdGVyJykgeyBjb25zdCByZXZlYWxlZCA9IHJldmVhbExvY2FsKHN0YXRlLCBwcm9wKTsgbG9nKHN0YXRlLCBgVGhlIHdldCBtdXNocm9vbXMgZ2xvdyBhbmQgcmV2ZWFsICR7cmV2ZWFsZWR9IG5lYXJieSB0aWxlcy5gKSB9XG4gICAgZWxzZSBpZiAoZWZmZWN0ID09PSAnZmlyZScpIHsgaWYgKHRpbGU/LmtpbmQgPT09ICdmbG9vcicgfHwgdGlsZT8ua2luZCA9PT0gJ3dlYicpIHRpbGUua2luZCA9ICdmaXJlVmVudCc7IGxvZyhzdGF0ZSwgJ1RoZSBtdXNocm9vbXMgZmxhcmUgaW50byBhIHZpc2libGUgZmlyZSBwYXRjaC4nKSB9XG4gICAgZWxzZSB7IGlmICh0aWxlPy5raW5kID09PSAnZmxvb3InIHx8IHRpbGU/LmtpbmQgPT09ICd3ZWInKSB0aWxlLmtpbmQgPSAnZ2FzJzsgbG9nKHN0YXRlLCAnVGhlIG11c2hyb29tcyBidXJzdCBhbmQgcmVsZWFzZSBhIHZpc2libGUgc3BvcmUgY2xvdWQuJykgfVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ3dpbGRzLmRhbmdsaW5nQ2hhcm0nKSB7XG4gICAgcHJvcC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gICAgY29uc3QgY2xlYXJlZCA9IGNsZWFyQnJhbWJsZShzdGF0ZSwgcHJvcClcbiAgICBsb2coc3RhdGUsIGNsZWFyZWQgPyAnVGhlIHNldmVyZWQgY2hhcm0gcHVsbHMgYSBicmFtYmxlIGNob2tlIHBvaW50IGFwYXJ0LicgOiAnVGhlIGRhbmdsaW5nIGNoYXJtIHVucmF2ZWxzIGludG8gaGFybWxlc3Mgcm9vdHMuJylcbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICd3aWxkcy5iaXJkTmVzdCcpIHtcbiAgICBpZiAocHJvcC5zdGF0ZSAhPT0gJ2FjdGl2YXRlZCcpIHtcbiAgICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgICAgY29uc3QgZGlzdHVyYmVkID0gZGlzdHVyYk5lc3Qoc3RhdGUsIHByb3ApXG4gICAgICBpZiAoZGlzdHVyYmVkKSBwcm9wLmV4cGlyZXNBdCA9IHN0YXRlLnR1cm4gKyA0XG4gICAgICBsb2coc3RhdGUsIGRpc3R1cmJlZCA/ICdUaGUgZGlzdHVyYmVkIG5lc3QgZHJhd3Mgc3RhcnRsZWQgYmlyZHMgaW50byB0aGUgcGF0aC4nIDogJ1RoZSBzdGFydGxlZCBmbG9jayBzY2F0dGVycyBiZXlvbmQgdGhlIHRyYWlsLicpXG4gICAgfVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ3dpbGRzLnJvb3RTaHJpbmUnKSB7XG4gICAgaWYgKGVmZmVjdCA9PT0gJ3Jvb3QnKSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICAgIGNvbnN0IGdyb3duID0gZ3Jvd0JyYW1ibGUoc3RhdGUsIHByb3ApXG4gICAgICBwcm9wLmVmZmVjdENlbGxzID0gZ3Jvd24ubWFwKHBvaW50ID0+ICh7IC4uLnBvaW50IH0pKVxuICAgICAgcHJvcC5leHBpcmVzQXQgPSBzdGF0ZS50dXJuICsgNFxuICAgICAgbG9nKHN0YXRlLCBgVGhlIHNocmluZSBzZW5kcyB1cCAke2dyb3duLmxlbmd0aCB8fCAnYSd9IHRob3JuIHNjcmVlbi5gKVxuICAgIH0gZWxzZSB7XG4gICAgICBjbGVhckVmZmVjdENlbGxzKHN0YXRlLCBwcm9wKVxuICAgICAgcHJvcC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gICAgICBsb2coc3RhdGUsICdUaGUgc2hyaW5lXFwncyByb290cyBjaGFyIGFuZCBmYWxsIGF3YXkuJylcbiAgICB9XG4gICAgcmV0dXJuIHRydWVcbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAnd2lsZHMubG9zdFBhcmNlbCcpIHtcbiAgICBwcm9wLnN0YXRlID0gJ2Rlc3Ryb3llZCdcbiAgICBjb25zdCBjbGVhcmVkID0gY2xlYXJCcmFtYmxlKHN0YXRlLCBwcm9wKVxuICAgIGxvZyhzdGF0ZSwgY2xlYXJlZCA/ICdUaGUgcGFyY2VsIGJ1cnN0cyBvcGVuIGFuZCBjbGVhcnMgYSBicmFtYmxlIHJvdXRlLicgOiAnVGhlIGxvc3QgcGFyY2VsIGJ1cnN0cyBpbnRvIHNjYXR0ZXJlZCB0cmFpbCBnZWFyLicpXG4gICAgcmV0dXJuIHRydWVcbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAnd2lsZHMucm9vdEFyY2gnKSB7XG4gICAgcHJvcC5zdGF0ZSA9IGVmZmVjdCA9PT0gJ2ZpcmUnID8gJ2Rlc3Ryb3llZCcgOiAnYWN0aXZhdGVkJ1xuICAgIGxvZyhzdGF0ZSwgZWZmZWN0ID09PSAnZmlyZScgPyAnRmxhbWUgYnVybnMgYSBwYXNzYWdlIHRocm91Z2ggdGhlIHJvb3QgYXJjaC4nIDogJ1RoZSBsaXZpbmcgcm9vdHMgZm9sZCBhc2lkZSBpbnRvIGEgZGV0b3VyLicpXG4gICAgcmV0dXJuIHRydWVcbiAgfVxuICByZXR1cm4gZmFsc2Vcbn1cblxuY29uc3QgYXBwbHlDYXZlcm5FZmZlY3QgPSAoc3RhdGU6IFJ1blN0YXRlLCBwcm9wOiBQcm9wLCBlZmZlY3Q6IFByb3BFZmZlY3RLaW5kKTogYm9vbGVhbiA9PiB7XG4gIGlmIChwcm9wLmtpbmQgPT09ICdjYXZlcm5zLmNyeXN0YWxDbHVzdGVyJykge1xuICAgIGlmIChlZmZlY3QgPT09ICdmb3JjZScpIHtcbiAgICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgICAgbG9nKHN0YXRlLCAnRm9yY2UgYW5nbGVzIHRoZSBjcnlzdGFsIGZhY2V0cyBhbmQgcmVmcmFjdHMgbGluZSBlZmZlY3RzIHRocm91Z2ggdGhlbS4nKVxuICAgIH0gZWxzZSBkZXN0cm95Q3J5c3RhbChzdGF0ZSwgcHJvcCwgJ2JsYXN0JylcbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdjYXZlcm5zLmdsb3dpbmdGdW5ndXMnKSB7XG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHByb3AueCwgcHJvcC55KVxuICAgIGlmIChlZmZlY3QgPT09ICd3YXRlcicpIHtcbiAgICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgICAgY29uc3QgcmV2ZWFsZWQgPSByZXZlYWxMb2NhbChzdGF0ZSwgcHJvcCwgNClcbiAgICAgIGxvZyhzdGF0ZSwgYFRoZSBzb2FrZWQgZnVuZ3VzIGJyaWdodGVucyBhbmQgcmV2ZWFscyAke3JldmVhbGVkfSBuZWFyYnkgdGlsZXMuYClcbiAgICB9IGVsc2UgaWYgKGVmZmVjdCA9PT0gJ2ZpcmUnKSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2Rlc3Ryb3llZCdcbiAgICAgIGlmICh0aWxlPy5raW5kID09PSAnZmxvb3InIHx8IHRpbGU/LmtpbmQgPT09ICdkYXJrbmVzcycpIHRpbGUua2luZCA9ICdmaXJlVmVudCdcbiAgICAgIGxvZyhzdGF0ZSwgJ1RoZSBmdW5ndXMgYnVybnMgaW50byBhIHZpc2libGUgZmlyZSBwYXRjaC4nKVxuICAgIH0gZWxzZSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2Rlc3Ryb3llZCdcbiAgICAgIGlmICh0aWxlPy5raW5kID09PSAnZmxvb3InIHx8IHRpbGU/LmtpbmQgPT09ICdkYXJrbmVzcycpIHRpbGUua2luZCA9ICdnYXMnXG4gICAgICBsb2coc3RhdGUsICdUaGUgZnVuZ3VzIGJ1cnN0cyBpbnRvIGEgdmlzaWJsZSBzcG9yZSBjbG91ZC4nKVxuICAgIH1cbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdjYXZlcm5zLmJhcm5hY2xlZFNocmluZScpIHtcbiAgICBpZiAoZWZmZWN0ID09PSAnd2F0ZXInKSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICAgIGNvbnN0IGZsb29kZWQgPSBmbG9vZEJyaW5lKHN0YXRlLCBwcm9wKVxuICAgICAgcHJvcC5lZmZlY3RDZWxscyA9IGZsb29kZWQubWFwKHBvaW50ID0+ICh7IC4uLnBvaW50IH0pKVxuICAgICAgcHJvcC5leHBpcmVzQXQgPSBzdGF0ZS50dXJuICsgNFxuICAgICAgYWRkQ29uZGl0aW9uKHN0YXRlLmhlcm8sIHsga2luZDogJ3NoaWVsZGVkJywgZHVyYXRpb246IDQsIHBvdGVuY3k6IDEgfSlcbiAgICAgIGxvZyhzdGF0ZSwgJ1RoZSBzaHJpbmUgYW5zd2VycyB0aGUgdGlkZSB3aXRoIGEgYnJpZWYgYnJpbmUgd2FyZC4nKVxuICAgIH0gZWxzZSB7XG4gICAgICBjbGVhckVmZmVjdENlbGxzKHN0YXRlLCBwcm9wKVxuICAgICAgcHJvcC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gICAgICBsb2coc3RhdGUsICdUaGUgYmFybmFjbGVkIHNocmluZSBjcmFja3MgYW5kIGl0cyBicmluZSB3YXJkIGZhaWxzLicpXG4gICAgfVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ2NhdmVybnMuYnJva2VuQm9hdCcpIHtcbiAgICBpZiAoZWZmZWN0ID09PSAnd2F0ZXInIHx8IGVmZmVjdCA9PT0gJ2ZvcmNlJykge1xuICAgICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgICBsb2coc3RhdGUsICdUaGUgY3VycmVudCBzZXR0bGVzIHRoZSBib2F0IGludG8gYSBzdGFibGUgd2F0ZXIgY3Jvc3NpbmcuJylcbiAgICB9IGVsc2Uge1xuICAgICAgcHJvcC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gICAgICBsb2coc3RhdGUsICdUaGUgYm9hdCBicmVha3MgYXBhcnQsIGxlYXZpbmcgdGhlIHdhdGVyIHJvdXRlIG9wZW4gYnV0IHVuYW5jaG9yZWQuJylcbiAgICB9XG4gICAgcmV0dXJuIHRydWVcbiAgfVxuICBpZiAocHJvcC5raW5kID09PSAnY2F2ZXJucy5lZWxUdW5uZWwnKSB7XG4gICAgaWYgKGVmZmVjdCA9PT0gJ2ZvcmNlJykge1xuICAgICAgaWYgKHByb3Auc3RhdGUgIT09ICdhY3RpdmF0ZWQnICYmICFjYW5TZWFsRWVsVHVubmVsKHN0YXRlLCBwcm9wKSkgeyBsb2coc3RhdGUsICdUaGUgdHVubmVsIHJlc2lzdHMgYSBzZWFsIHRoYXQgd291bGQgY2xvc2UgdGhlIHJlcXVpcmVkIHRyYWlsLicpOyByZXR1cm4gdHJ1ZSB9XG4gICAgICBwcm9wLnN0YXRlID0gcHJvcC5zdGF0ZSA9PT0gJ2FjdGl2YXRlZCcgPyAnZG9ybWFudCcgOiAnYWN0aXZhdGVkJ1xuICAgICAgbG9nKHN0YXRlLCBwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJyA/ICdGb3JjZSBzZWFscyB0aGUgZWVsIHR1bm5lbC4nIDogJ0ZvcmNlIHB1bGxzIHRoZSBlZWwgdHVubmVsIG9wZW4gYXMgYSBzaG9ydGN1dC4nKVxuICAgIH0gZWxzZSBpZiAoZWZmZWN0ID09PSAnZmlyZScgJiYgcHJvcC5zdGF0ZSAhPT0gJ2FjdGl2YXRlZCcpIHtcbiAgICAgIGxvZyhzdGF0ZSwgZGlzdHVyYkVlbFR1bm5lbChzdGF0ZSwgcHJvcCkgPyAnRmxhbWUgZHJhd3MgYSBmdW1lIGVlbCBmcm9tIHRoZSBvcGVuIHR1bm5lbC4nIDogJ1RoZSBvcGVuIHR1bm5lbCBoaXNzZXMsIGJ1dCBubyBlZWwgcmVhY2hlcyB0aGUgcGF0aC4nKVxuICAgIH0gZWxzZSB7XG4gICAgICBpZiAoY2FuU2VhbEVlbFR1bm5lbChzdGF0ZSwgcHJvcCkpIHsgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnOyBsb2coc3RhdGUsICdUaGUgZWVsIHR1bm5lbCBzZWFscyBzaHV0LicpIH1cbiAgICAgIGVsc2UgbG9nKHN0YXRlLCAnVGhlIHR1bm5lbCBjYW5ub3Qgc2VhbCB3aXRob3V0IGNsb3NpbmcgdGhlIHJlcXVpcmVkIHRyYWlsLicpXG4gICAgfVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ2NhdmVybnMuc2VhbGVkUGFyY2VsJykge1xuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIHJld2FyZChzdGF0ZSwgcHJvcCwgJ2ZvY3VzVG9uaWMnKVxuICAgIGxvZyhzdGF0ZSwgJ1RoZSBpbXBhY3QgY3JhY2tzIHRoZSB3YXggc2VhbCBhbmQgb3BlbnMgdGhlIHBhcmNlbCBjYWNoZS4nKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgcmV0dXJuIGZhbHNlXG59XG5cbmNvbnN0IGFwcGx5UnVpbnNFZmZlY3QgPSAoc3RhdGU6IFJ1blN0YXRlLCBwcm9wOiBQcm9wLCBlZmZlY3Q6IFByb3BFZmZlY3RLaW5kKTogYm9vbGVhbiA9PiB7XG4gIGlmIChwcm9wLmtpbmQgPT09ICdydWlucy5icm9rZW5TdGF0dWUnKSB7XG4gICAgaWYgKGVmZmVjdCA9PT0gJ2ZvcmNlJykge1xuICAgICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgICBsb2coc3RhdGUsICdGb3JjZSB0b3BwbGVzIHRoZSBzdGF0dWUgaW50byBsaW5lLWJsb2NraW5nIGNvdmVyLicpXG4gICAgfSBlbHNlIHtcbiAgICAgIHByb3Auc3RhdGUgPSAnZGVzdHJveWVkJ1xuICAgICAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHByb3AueCwgcHJvcC55KVxuICAgICAgaWYgKHRpbGU/LmtpbmQgPT09ICdmbG9vcicgJiYgcHJlc2VydmVzRXhpdFBhdGgoc3RhdGUuZmxvb3IsIHN0YXRlLmZsb29yLnN0YXJ0LCBwcm9wLCAncnViYmxlJykpIHRpbGUua2luZCA9ICdydWJibGUnXG4gICAgICByZXdhcmQoc3RhdGUsIHByb3AsICdyb2NrJylcbiAgICAgIGxvZyhzdGF0ZSwgJ1RoZSBzdGF0dWUgYnJlYWtzIGludG8gc2FmZSBydWJibGUgYW5kIHN0b25lIHNoYXJkcy4nKVxuICAgIH1cbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdydWlucy5yaXR1YWxCcmF6aWVyJykge1xuICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBwcm9wLngsIHByb3AueSlcbiAgICBpZiAoZWZmZWN0ID09PSAnd2F0ZXInKSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2Rvcm1hbnQnXG4gICAgICBpZiAodGlsZT8ua2luZCA9PT0gJ2ZpcmVWZW50JykgdGlsZS5raW5kID0gJ2Zsb29yJ1xuICAgICAgbG9nKHN0YXRlLCAnV2F0ZXIgZXh0aW5ndWlzaGVzIHRoZSBicmF6aWVyIGFuZCByZW1vdmVzIGl0cyBmaXJlIGhhemFyZC4nKVxuICAgIH0gZWxzZSBpZiAoZWZmZWN0ID09PSAnd2FyZCcpIHtcbiAgICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgICAgYWRkQ29uZGl0aW9uKHN0YXRlLmhlcm8sIHsga2luZDogJ3NoaWVsZGVkJywgZHVyYXRpb246IDMsIHBvdGVuY3k6IDEgfSlcbiAgICAgIGFkZENvbmRpdGlvbihzdGF0ZS5oZXJvLCB7IGtpbmQ6ICdtYXJrZWQnLCBkdXJhdGlvbjogMiwgcG90ZW5jeTogMSB9KVxuICAgICAgbG9nKHN0YXRlLCAnV2FyZCBiaW5kcyB0aGUgYnJhemllcjogZ2FpbiBhIGJyaWVmIHNoaWVsZCwgYnV0IHJpdHVhbCBtYXJrcyBleHBvc2UgdGhlIGNvc3QuJylcbiAgICB9IGVsc2UgaWYgKGVmZmVjdCA9PT0gJ2dhdGUnKSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICAgIGFkZENvbmRpdGlvbihzdGF0ZS5oZXJvLCB7IGtpbmQ6ICdtYXJrZWQnLCBkdXJhdGlvbjogMywgcG90ZW5jeTogMSB9KVxuICAgICAgbG9nKHN0YXRlLCAnR2F0ZSB3YWtlcyB0aGUgYnJhemllcjsgdGhlIGV4aXQgb3BlbnMsIGJ1dCB0aGUgcml0dWFsIG1hcmtzIHlvdS4nKVxuICAgIH0gZWxzZSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICAgIGlmICh0aWxlPy5raW5kID09PSAnZmxvb3InKSB0aWxlLmtpbmQgPSAnZmlyZVZlbnQnXG4gICAgICBsb2coc3RhdGUsICdUaGUgYnJhemllciBjYXRjaGVzIGFuZCBjcmVhdGVzIGEgdmlzaWJsZSBsb2NhbCBmaXJlIGhhemFyZC4nKVxuICAgIH1cbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdydWlucy5nbHlwaFRhYmxldCcpIHtcbiAgICBwcm9wLnN0YXRlID0gJ2Rlc3Ryb3llZCdcbiAgICBsb2coc3RhdGUsICdUaGUgdGFibGV0IGZyYWN0dXJlcyBhZnRlciBpdHMgZ2x5cGhzIGRpc2NoYXJnZS4nKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ3J1aW5zLmNvbGxhcHNlZEFyY2gnKSB7XG4gICAgcHJvcC5zdGF0ZSA9IGVmZmVjdCA9PT0gJ2JvbWInID8gJ2Rlc3Ryb3llZCcgOiAnYWN0aXZhdGVkJ1xuICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBwcm9wLngsIHByb3AueSlcbiAgICBpZiAodGlsZT8ua2luZCA9PT0gJ3J1YmJsZScpIHRpbGUua2luZCA9ICdmbG9vcidcbiAgICBsb2coc3RhdGUsIGVmZmVjdCA9PT0gJ2JvbWInID8gJ1RoZSBibGFzdCBjbGVhcnMgdGhlIGNvbGxhcHNlZCBhcmNoIHJvdXRlLicgOiAnRm9yY2Ugb3BlbnMgYSByb3V0ZSB0aHJvdWdoIHRoZSBjb2xsYXBzZWQgYXJjaC4nKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ3J1aW5zLnNlYWxlZENhY2hlJykge1xuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIHJld2FyZChzdGF0ZSwgcHJvcCwgJ3N1bnNlYWwnKVxuICAgIGxvZyhzdGF0ZSwgJ1RoZSBpbXBhY3QgYnJlYWtzIHRoZSBjYWNoZSBsb2NrIGFuZCByZXZlYWxzIGEgU3Vuc3RvbmUgU2VhbC4nKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ3J1aW5zLm1vbm9saXRoJykge1xuICAgIGlmIChlZmZlY3QgPT09ICd3YXJkJykge1xuICAgICAgYXJtTW9ub2xpdGgoc3RhdGUsIHByb3AsICdXYXJkJylcbiAgICAgIGFkZENvbmRpdGlvbihzdGF0ZS5oZXJvLCB7IGtpbmQ6ICdzaGllbGRlZCcsIGR1cmF0aW9uOiAzLCBwb3RlbmN5OiAxIH0pXG4gICAgfSBlbHNlIGlmIChlZmZlY3QgPT09ICdnYXRlJykgYXJtTW9ub2xpdGgoc3RhdGUsIHByb3AsICdHYXRlJylcbiAgICBlbHNlIHtcbiAgICAgIHByb3Auc3RhdGUgPSAnZGVzdHJveWVkJ1xuICAgICAgcHJvcC5lZmZlY3RDZWxscyA9IHVuZGVmaW5lZFxuICAgICAgcHJvcC5leHBpcmVzQXQgPSB1bmRlZmluZWRcbiAgICAgIGxvZyhzdGF0ZSwgJ1RoZSBtb25vbGl0aCBzaGF0dGVycyBiZWZvcmUgaXQgY2FuIGFuc3dlciBhbm90aGVyIHJpdHVhbC4nKVxuICAgIH1cbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIHJldHVybiBmYWxzZVxufVxuXG5jb25zdCBhcHBseUZ1cm5hY2VFZmZlY3QgPSAoc3RhdGU6IFJ1blN0YXRlLCBwcm9wOiBQcm9wLCBlZmZlY3Q6IFByb3BFZmZlY3RLaW5kKTogYm9vbGVhbiA9PiB7XG4gIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBwcm9wLngsIHByb3AueSlcbiAgaWYgKHByb3Aua2luZCA9PT0gJ2Z1cm5hY2UuYmVsbG93cycgfHwgcHJvcC5raW5kID09PSAnZnVybmFjZS5zbW9rZVN0YWNrJykge1xuICAgIHByb3Auc3RhdGUgPSBlZmZlY3QgPT09ICd3YXRlcicgPyAnZG9ybWFudCcgOiAnYWN0aXZhdGVkJ1xuICAgIGlmICh0aWxlICYmIGVmZmVjdCAhPT0gJ3dhdGVyJykgdGlsZS5raW5kID0gZWZmZWN0ID09PSAnZmlyZScgPyAnZmlyZVZlbnQnIDogJ3Ntb2tlJ1xuICAgIGlmICh0aWxlICYmIGVmZmVjdCA9PT0gJ3dhdGVyJykgdGlsZS5raW5kID0gJ2Zsb29yJ1xuICAgIGxvZyhzdGF0ZSwgZWZmZWN0ID09PSAnd2F0ZXInID8gJ1dhdGVyIHNldHRsZXMgdGhlIGZ1cm5hY2Ugc21va2UuJyA6ICdUaGUgZnVybmFjZSBzdGFjayBjaGFuZ2VzIHRoZSBsb2NhbCBhaXIuJylcbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdmdXJuYWNlLmxpZnRDb25zb2xlJykge1xuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIGZvciAoY29uc3QgcG9pbnQgb2YgbmVhcmJ5UG9pbnRzKHByb3ApKSBpZiAoZ2V0VGlsZShzdGF0ZS5mbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpIHsgZ2V0VGlsZShzdGF0ZS5mbG9vciwgcG9pbnQueCwgcG9pbnQueSkhLmtpbmQgPSAnbGlmdCc7IGJyZWFrIH1cbiAgICBsb2coc3RhdGUsICdUaGUgY29uc29sZSByYWlzZXMgYSBuZWFyYnkgbGlmdCByb3V0ZS4nKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ2Z1cm5hY2UuYnJlYWt3YWxsJykge1xuICAgIHByb3Auc3RhdGUgPSBlZmZlY3QgPT09ICdib21iJyA/ICdkZXN0cm95ZWQnIDogJ2FjdGl2YXRlZCdcbiAgICBpZiAodGlsZSkgdGlsZS5raW5kID0gJ2Zsb29yJ1xuICAgIGxvZyhzdGF0ZSwgJ1RoZSBzY29yZWQgd2FsbCBvcGVucyBpbnRvIGEgaG90IGFsdGVybmF0ZSByb3V0ZS4nKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ2Z1cm5hY2UuZm9yZ2VJZG9sJykge1xuICAgIHByb3Auc3RhdGUgPSAnYWN0aXZhdGVkJ1xuICAgIGlmIChlZmZlY3QgPT09ICdmaXJlJykgYWRkQ29uZGl0aW9uKHN0YXRlLmhlcm8sIHsga2luZDogJ2J1cm5pbmcnLCBkdXJhdGlvbjogMiwgcG90ZW5jeTogMSB9KVxuICAgIGVsc2UgYWRkQ29uZGl0aW9uKHN0YXRlLmhlcm8sIHsga2luZDogJ3NoaWVsZGVkJywgZHVyYXRpb246IDIsIHBvdGVuY3k6IDEgfSlcbiAgICBsb2coc3RhdGUsICdUaGUgZm9yZ2UgaWRvbCBhbnN3ZXJzIHdpdGggcG93ZXIgYW5kIGEgY29zdC4nKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgcmV0dXJuIGZhbHNlXG59XG5cbmNvbnN0IGFwcGx5Rmxvb2RlZEVmZmVjdCA9IChzdGF0ZTogUnVuU3RhdGUsIHByb3A6IFByb3AsIGVmZmVjdDogUHJvcEVmZmVjdEtpbmQpOiBib29sZWFuID0+IHtcbiAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHByb3AueCwgcHJvcC55KVxuICBpZiAocHJvcC5raW5kID09PSAnZmxvb2RlZFJ1aW5zLmFuY2hvclBvc3QnIHx8IHByb3Aua2luZCA9PT0gJ2Zsb29kZWRSdWlucy5jdXJyZW50QmVsbCcpIHtcbiAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICBmb3IgKGNvbnN0IHBvaW50IG9mIG5lYXJieVBvaW50cyhwcm9wLCAyKSkgaWYgKGdldFRpbGUoc3RhdGUuZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kID09PSAnY3VycmVudCcpIHsgZ2V0VGlsZShzdGF0ZS5mbG9vciwgcG9pbnQueCwgcG9pbnQueSkhLmtpbmQgPSBlZmZlY3QgPT09ICd3YXRlcicgPyAnd2F0ZXInIDogJ2FuY2hvcic7IGJyZWFrIH1cbiAgICBsb2coc3RhdGUsICdUaGUgYW5jaG9yIGNoYW5nZXMgdGhlIGN1cnJlbnQgYXJvdW5kIHRoZSByb3V0ZS4nKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgaWYgKHByb3Aua2luZCA9PT0gJ2Zsb29kZWRSdWlucy5mbG9vZGdhdGUnKSB7XG4gICAgcHJvcC5zdGF0ZSA9ICdhY3RpdmF0ZWQnXG4gICAgaWYgKHRpbGUpIHRpbGUua2luZCA9IGVmZmVjdCA9PT0gJ2JvbWInID8gJ2Zsb29yJyA6ICdjdXJyZW50J1xuICAgIGxvZyhzdGF0ZSwgZWZmZWN0ID09PSAnYm9tYicgPyAnVGhlIGZsb29kZ2F0ZSBicmVha3MgaW50byBhIGRyeSByb3V0ZS4nIDogJ1RoZSBmbG9vZGdhdGUgc2VuZHMgYSBjdXJyZW50IHRocm91Z2ggdGhlIGNoYW1iZXIuJylcbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIGlmIChwcm9wLmtpbmQgPT09ICdmbG9vZGVkUnVpbnMudGlkZVNocmluZScpIHtcbiAgICBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCdcbiAgICBhZGRDb25kaXRpb24oc3RhdGUuaGVybywgeyBraW5kOiAnc2hpZWxkZWQnLCBkdXJhdGlvbjogMywgcG90ZW5jeTogMSB9KVxuICAgIGxvZyhzdGF0ZSwgJ1RoZSB0aWRlIHNocmluZSBnaXZlcyBhIGJyaWVmIGJyaW5lIHdhcmQuJylcbiAgICByZXR1cm4gdHJ1ZVxuICB9XG4gIHJldHVybiBmYWxzZVxufVxuXG5leHBvcnQgY29uc3QgcmVzb2x2ZU1vbm9saXRoVGVsZWdyYXBocyA9IChzdGF0ZTogUnVuU3RhdGUsIHRlbGVncmFwaHM6IHJlYWRvbmx5IFRlbGVncmFwaFtdKTogVGVsZWdyYXBoW10gPT4ge1xuICBjb25zdCBtb25vbGl0aCA9IHN0YXRlLmZsb29yLnByb3BzLmZpbmQocHJvcCA9PiBwcm9wLmtpbmQgPT09ICdydWlucy5tb25vbGl0aCcgJiYgcHJvcC5zdGF0ZSA9PT0gJ2FjdGl2YXRlZCcgJiYgcHJvcC5leHBpcmVzQXQgIT09IHVuZGVmaW5lZCAmJiBwcm9wLmV4cGlyZXNBdCA+IHN0YXRlLnR1cm4pXG4gIGlmICghbW9ub2xpdGgpIHJldHVybiBbLi4udGVsZWdyYXBoc11cbiAgY29uc3QgZmllbGQgPSBuZXcgU2V0KChtb25vbGl0aC5lZmZlY3RDZWxscyA/PyBbXSkubWFwKHBvaW50S2V5KSlcbiAgY29uc3QgYWJzb3JiZWQgPSB0ZWxlZ3JhcGhzLmZpbHRlcih0ZWxlZ3JhcGggPT4gdGVsZWdyYXBoLmNlbGxzLnNvbWUocG9pbnQgPT4gZmllbGQuaGFzKHBvaW50S2V5KHBvaW50KSkpKVxuICBpZiAoIWFic29yYmVkLmxlbmd0aCkgcmV0dXJuIFsuLi50ZWxlZ3JhcGhzXVxuICBtb25vbGl0aC5zdGF0ZSA9ICdkZXN0cm95ZWQnXG4gIG1vbm9saXRoLmVmZmVjdENlbGxzID0gdW5kZWZpbmVkXG4gIG1vbm9saXRoLmV4cGlyZXNBdCA9IHVuZGVmaW5lZFxuICBsb2coc3RhdGUsIGBUaGUgbW9ub2xpdGggYWJzb3JicyAke2Fic29yYmVkLmxlbmd0aH0gbmVhcmJ5IHRlbGVncmFwaCR7YWJzb3JiZWQubGVuZ3RoID09PSAxID8gJycgOiAncyd9IGFuZCBjcmFja3MgYXBhcnQuYClcbiAgcmV0dXJuIHRlbGVncmFwaHMuZmlsdGVyKHRlbGVncmFwaCA9PiAhYWJzb3JiZWQuaW5jbHVkZXModGVsZWdyYXBoKSlcbn1cblxuZXhwb3J0IGNvbnN0IGFwcGx5UHJvcEVmZmVjdHMgPSAoc3RhdGU6IFJ1blN0YXRlLCBwb2ludHM6IHJlYWRvbmx5IFBvaW50W10sIGVmZmVjdHM6IHJlYWRvbmx5IFByb3BFZmZlY3RLaW5kW10pOiBzdHJpbmdbXSA9PiB7XG4gIGNvbnN0IHRhcmdldHMgPSBuZXcgU2V0KHBvaW50cy5tYXAocG9pbnRLZXkpKVxuICBjb25zdCBjaGFuZ2VkOiBzdHJpbmdbXSA9IFtdXG4gIGZvciAoY29uc3QgcHJvcCBvZiBzdGF0ZS5mbG9vci5wcm9wcykge1xuICAgIGlmIChwcm9wLnN0YXRlID09PSAnZGVzdHJveWVkJyB8fCAhdGFyZ2V0cy5oYXMocG9pbnRLZXkocHJvcCkpKSBjb250aW51ZVxuICAgIGNvbnN0IGVmZmVjdCA9IGVmZmVjdHMuZmluZChjYW5kaWRhdGUgPT4gcHJvcC5ob29rcz8uaW5jbHVkZXMoY2FuZGlkYXRlKSlcbiAgICBpZiAoIWVmZmVjdCkgY29udGludWVcbiAgICBpZiAod2lsZHNQcm9wKHByb3ApICYmIGFwcGx5V2lsZHNFZmZlY3Qoc3RhdGUsIHByb3AsIGVmZmVjdCkpIHsgY2hhbmdlZC5wdXNoKHByb3AuaWQpOyBjb250aW51ZSB9XG4gICAgaWYgKGNhdmVyblByb3AocHJvcCkgJiYgYXBwbHlDYXZlcm5FZmZlY3Qoc3RhdGUsIHByb3AsIGVmZmVjdCkpIHsgY2hhbmdlZC5wdXNoKHByb3AuaWQpOyBjb250aW51ZSB9XG4gICAgaWYgKHJ1aW5zUHJvcChwcm9wKSAmJiBhcHBseVJ1aW5zRWZmZWN0KHN0YXRlLCBwcm9wLCBlZmZlY3QpKSB7IGNoYW5nZWQucHVzaChwcm9wLmlkKTsgY29udGludWUgfVxuICAgIGlmIChmdXJuYWNlUHJvcChwcm9wKSAmJiBhcHBseUZ1cm5hY2VFZmZlY3Qoc3RhdGUsIHByb3AsIGVmZmVjdCkpIHsgY2hhbmdlZC5wdXNoKHByb3AuaWQpOyBjb250aW51ZSB9XG4gICAgaWYgKGZsb29kZWRQcm9wKHByb3ApICYmIGFwcGx5Rmxvb2RlZEVmZmVjdChzdGF0ZSwgcHJvcCwgZWZmZWN0KSkgeyBjaGFuZ2VkLnB1c2gocHJvcC5pZCk7IGNvbnRpbnVlIH1cbiAgICBpZiAocHJvcC5raW5kID09PSAnbWluZS5sYW50ZXJuUG9zdCcpIHtcbiAgICAgIGlmIChlZmZlY3QgPT09ICdmaXJlJykgeyBwcm9wLnN0YXRlID0gJ2FjdGl2YXRlZCc7IGxvZyhzdGF0ZSwgJ1RoZSBsYW50ZXJuIHBvc3QgY2F0Y2hlcyBhbmQgc3BpbGxzIGxvY2FsIGxpZ2h0LicpOyBjaGFuZ2VkLnB1c2gocHJvcC5pZCk7IGNvbnRpbnVlIH1cbiAgICAgIGlmIChlZmZlY3QgPT09ICd3YXRlcicgfHwgZWZmZWN0ID09PSAnaGF6YXJkJykgeyBwcm9wLnN0YXRlID0gJ2Rvcm1hbnQnOyBsb2coc3RhdGUsICdUaGUgbGFudGVybiBwb3N0IGd1dHRlcnMgb3V0LicpOyBjaGFuZ2VkLnB1c2gocHJvcC5pZCk7IGNvbnRpbnVlIH1cbiAgICB9XG4gICAgaWYgKHByb3Aua2luZCA9PT0gJ21pbmUub3JlVmVpbicgJiYgZWZmZWN0ID09PSAnYm9tYicpIHtcbiAgICAgIHByb3Auc3RhdGUgPSAnZGVzdHJveWVkJ1xuICAgICAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHByb3AueCwgcHJvcC55KVxuICAgICAgaWYgKHRpbGUpIHRpbGUua2luZCA9ICdmbG9vcidcbiAgICAgIHJld2FyZChzdGF0ZSwgcHJvcCwgJ3JvY2snLCAyKVxuICAgICAgbG9nKHN0YXRlLCAnVGhlIGJsYXN0IGNsZWFycyB0aGUgb3JlIHZlaW4gYW5kIHNjYXR0ZXJzIHJvY2suJyk7XG4gICAgICBjaGFuZ2VkLnB1c2gocHJvcC5pZClcbiAgICAgIGNvbnRpbnVlXG4gICAgfVxuICAgIGlmIChwcm9wLmtpbmQgPT09ICdtaW5lLmJyb2tlbkNhcnQnICYmIGVmZmVjdCA9PT0gJ2ZvcmNlJykgY29udGludWVcbiAgICBpZiAocHJvcC5raW5kID09PSAnbWluZS5kaXNjYXJkZWRQYXJjZWwnKSB7XG4gICAgICBwcm9wLnN0YXRlID0gJ2Rlc3Ryb3llZCdcbiAgICAgIGxvZyhzdGF0ZSwgJ1RoZSBhYmFuZG9uZWQgcGFyY2VsIGJ1cnN0cyBpbnRvIGEgbm9pc3kgYmxhc3QuJylcbiAgICAgIGNoYW5nZWQucHVzaChwcm9wLmlkKVxuICAgICAgZXhwbG9kZShzdGF0ZSwgcHJvcC54LCBwcm9wLnksIDUsIFsnYm9tYiddLCAndGhlIGRpc2NhcmRlZCBwYXJjZWwnKVxuICAgICAgY29udGludWVcbiAgICB9XG4gICAgZGVzdHJveVByb3Aoc3RhdGUsIHByb3AsIGVmZmVjdClcbiAgICBjaGFuZ2VkLnB1c2gocHJvcC5pZClcbiAgfVxuICByZXR1cm4gY2hhbmdlZFxufVxuIl0sIm1hcHBpbmdzIjoiQUFBQSxTQUFTQSxVQUFVLEVBQUVDLFVBQVUsUUFBbUYsVUFBVTtBQUM1SCxTQUFTQyxjQUFjLEVBQUVDLE1BQU0sRUFBRUMsY0FBYyxRQUFRLFVBQVU7QUFDakUsU0FBU0MsT0FBTyxFQUFFQyxPQUFPLEVBQUVDLGVBQWUsRUFBRUMsVUFBVSxFQUFFQyxpQkFBaUIsRUFBRUMsWUFBWSxRQUFRLFVBQVU7QUFDekcsU0FBU0MsVUFBVSxFQUFFQyxPQUFPLEVBQUVDLHFCQUFxQixRQUFRLFVBQVU7QUFDckUsU0FBU0MsWUFBWSxFQUFFQyxvQkFBb0IsUUFBUSxjQUFjO0FBQ2pFLFNBQVNDLEtBQUssRUFBRUMsR0FBRyxRQUEyQixVQUFVO0FBRXhELE1BQU1DLFFBQVEsR0FBSUMsS0FBWSxJQUFhLEdBQUdBLEtBQUssQ0FBQ0MsQ0FBQyxJQUFJRCxLQUFLLENBQUNFLENBQUMsRUFBRTtBQUNsRSxNQUFNQyxRQUFRLEdBQUlDLElBQVUsSUFBY0EsSUFBSSxDQUFDQyxLQUFLLEtBQUssTUFBTSxJQUFJRCxJQUFJLENBQUNFLElBQUksQ0FBQ0MsVUFBVSxDQUFDLE9BQU8sQ0FBQztBQUNoRyxNQUFNQyxTQUFTLEdBQUlKLElBQVUsSUFBY0EsSUFBSSxDQUFDQyxLQUFLLEtB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