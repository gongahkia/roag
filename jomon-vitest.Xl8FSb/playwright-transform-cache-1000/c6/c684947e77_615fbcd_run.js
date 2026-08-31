// b6188f3dbb1af2d9544608b6f07f58225276bfee
import { biomeName } from '../content';
import { generateAreaFloor } from '../world';
import { refreshFov } from './visibility';
import { hydrateEncyclopediaLegacy } from './encyclopedia';
import { createRunTelemetry } from '../telemetry';
import { recordSafePosition } from './buildcraft';
import { cloneCampaignCycle, DEFAULT_AREA_ORDER, initialCampaignCycle, campaignOrderForSeed } from './campaign';
import { applyAreaArcState, areaArcStateFor } from '../escalation';
import { emptySocialReputation } from '../social-contract';
import { cloneCompanions } from './companions';
import { isCompanionActor, synchronizePartyActors } from './party';
const routeOffsetFor = linkId => [...linkId].reduce((total, char) => total + char.charCodeAt(0), 0);
const routeChunkKey = chunk => String(chunk);
const cloneActor = (actor, x = actor.x, y = actor.y) => ({
  ...structuredClone(actor),
  x,
  y
});
const cloneItem = (item, x = item.x, y = item.y) => ({
  ...structuredClone(item),
  x,
  y
});
const cloneProp = (prop, x = prop.x, y = prop.y) => {
  var _prop$effectCells;
  return {
    ...structuredClone(prop),
    x,
    y,
    effectCells: (_prop$effectCells = prop.effectCells) === null || _prop$effectCells === void 0 ? void 0 : _prop$effectCells.map(cell => ({
      x: cell.x - prop.x + x,
      y: cell.y - prop.y + y
    }))
  };
};
const routeY = (travel, chunkWidth, chunk, localX, center) => center + Math.round(Math.sin((chunk * chunkWidth + localX + routeOffsetFor(travel.linkId)) / 7) * 3);
const routeTerrain = (travel, chunkWidth, height, chunk) => {
  const width = chunkWidth;
  const center = Math.floor(height / 2);
  const tiles = Array.from({
    length: width * height
  }, () => ({
    kind: 'wall',
    explored: false,
    visible: false
  }));
  const carve = (x, y, radius = 2) => {
    for (let dy = -radius; dy <= radius; dy++) for (let dx = -1; dx <= 1; dx++) {
      const px = x + dx;
      const py = y + dy;
      if (px > 0 && px < width - 1 && py > 0 && py < height - 1) tiles[py * width + px].kind = 'floor';
    }
  };
  for (let x = 2; x < width - 2; x++) {
    const y = routeY(travel, chunkWidth, chunk, x, center);
    carve(x, y);
    if ((chunk * width + x) % 13 === 0) for (let branch = 1; branch < 7; branch++) carve(x, y + ((chunk * width + x) % 26 ? branch : -branch), 1);
    const situation = travel.situations[chunk];
    if (situation === 'hazard' && (chunk * width + x) % 19 === 0) tiles[y * width + x].kind = 'gas';
    if (situation === 'ecology' && (chunk * width + x) % 23 === 0) tiles[y * width + x].kind = 'bramble';
  }
  tiles[center * width + 2].kind = 'floor';
  return tiles;
};
const routeActors = (travel, chunkWidth, height, chunk, patrolTemplate) => {
  const x = Math.min(chunkWidth - 5, Math.floor(chunkWidth * .62));
  const y = routeY(travel, chunkWidth, chunk, x, Math.floor(height / 2));
  if (travel.situations[chunk] === 'patrol' && patrolTemplate) return [{
    ...cloneActor(patrolTemplate, x, y),
    id: `route-patrol:${travel.linkId}:${chunk}`,
    health: patrolTemplate.maxHealth,
    maxHealth: patrolTemplate.maxHealth
  }];
  if (travel.situations[chunk] === 'trader') return [{
    id: `route-trader:${travel.linkId}:${chunk}`,
    role: 'merchant',
    kind: 'merchant',
    name: 'route trader',
    x,
    y,
    health: 1,
    maxHealth: 1,
    attack: 0,
    defense: 0,
    speed: 0,
    energy: 0,
    glyph: '$',
    color: '#f4d26a',
    hostile: false
  }];
  return [];
};
const restoredChunk = (travel, chunkWidth, height, chunk, patrolTemplate) => {
  var _travel$chunks, _memory$items, _memory$props;
  const tiles = routeTerrain(travel, chunkWidth, height, chunk);
  const memory = (_travel$chunks = travel.chunks) === null || _travel$chunks === void 0 ? void 0 : _travel$chunks[routeChunkKey(chunk)];
  for (const change of (_memory$terrain = memory === null || memory === void 0 ? void 0 : memory.terrain) !== null && _memory$terrain !== void 0 ? _memory$terrain : []) {
    var _memory$terrain;
    const tile = tiles[change.index];
    if (tile) {
      tile.kind = change.kind;
      tile.elevation = change.elevation;
      tile.flow = change.flow ? structuredClone(change.flow) : undefined;
      tile.explored = false;
      tile.visible = false;
    }
  }
  return {
    tiles,
    actors: memory ? memory.actors.map(actor => cloneActor(actor)) : routeActors(travel, chunkWidth, height, chunk, patrolTemplate),
    items: ((_memory$items = memory === null || memory === void 0 ? void 0 : memory.items) !== null && _memory$items !== void 0 ? _memory$items : []).map(item => cloneItem(item)),
    props: ((_memory$props = memory === null || memory === void 0 ? void 0 : memory.props) !== null && _memory$props !== void 0 ? _memory$props : []).map(prop => cloneProp(prop))
  };
};
const rememberedTerrain = (current, baseline) => {
  if (current.kind === baseline.kind && current.elevation === baseline.elevation && JSON.stringify(current.flow) === JSON.stringify(baseline.flow)) return undefined;
  return {
    index: 0,
    kind: current.kind,
    ...(current.elevation === undefined ? {} : {
      elevation: current.elevation
    }),
    ...(current.flow === undefined ? {} : {
      flow: structuredClone(current.flow)
    })
  };
};
const rememberTransitWindow = state => {
  var _travel$chunks2;
  const travel = state.travel;
  if (!travel) return undefined;
  const residentCount = Math.min(3, travel.chunkCount - travel.residentStart);
  const chunkWidth = Math.floor(state.floor.width / residentCount);
  const chunks = {
    ...((_travel$chunks2 = travel.chunks) !== null && _travel$chunks2 !== void 0 ? _travel$chunks2 : {})
  };
  for (let localChunk = 0; localChunk < residentCount; localChunk++) {
    const chunk = travel.residentStart + localChunk;
    const baseline = routeTerrain(travel, chunkWidth, state.floor.height, chunk);
    const offset = localChunk * chunkWidth;
    const terrain = baseline.flatMap((tile, index) => {
      const serviceExit = localChunk === residentCount - 1 && index === routeY(travel, chunkWidth, chunk, chunkWidth - 3, Math.floor(state.floor.height / 2)) * chunkWidth + chunkWidth - 3;
      if (serviceExit) return [];
      const memory = rememberedTerrain(state.floor.tiles[Math.floor(index / chunkWidth) * state.floor.width + offset + index % chunkWidth], tile);
      return memory ? [{
        ...memory,
        index
      }] : [];
    });
    const inChunk = x => x >= offset && x < offset + chunkWidth;
    chunks[routeChunkKey(chunk)] = {
      terrain,
      actors: state.floor.actors.filter(actor => !isCompanionActor(actor) && inChunk(actor.x)).map(actor => cloneActor(actor, actor.x - offset, actor.y)),
      items: state.floor.items.filter(item => inChunk(item.x)).map(item => cloneItem(item, item.x - offset, item.y)),
      props: state.floor.props.filter(prop => inChunk(prop.x)).map(prop => cloneProp(prop, prop.x - offset, prop.y))
    };
  }
  return {
    ...travel,
    chunks
  };
};
const originStats = {
  mineborn: {
    strength: 3,
    agility: 1,
    vitality: 3,
    intellect: 1
  },
  mosswalker: {
    strength: 1,
    agility: 3,
    vitality: 3,
    intellect: 1
  },
  cavernSeeker: {
    strength: 1,
    agility: 2,
    vitality: 2,
    intellect: 3
  },
  tidebound: {
    strength: 2,
    agility: 3,
    vitality: 1,
    intellect: 2
  }
};
const starterKit = (origin, calling) => {
  const kit = calling === 'trailguard' ? {
    bombs: 4,
    ropes: 4,
    inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'],
    equipment: {
      mainHand: 'whip',
      offHand: 'buckler'
    }
  } : calling === 'pathmaker' ? {
    bombs: 6,
    ropes: 6,
    inventory: ['tonic', 'rock', 'bombPack', 'bombPack', 'ropeBundle', 'ropeBundle', 'ember', 'mapScroll'],
    equipment: {
      mainHand: 'whip'
    }
  } : {
    bombs: 4,
    ropes: 4,
    inventory: ['tonic', 'focusTonic', 'rock', 'bombPack', 'ropeBundle', 'ember', 'sight'],
    equipment: {
      mainHand: 'whip'
    }
  };
  return origin === 'tidebound' ? {
    ...kit,
    inventory: [...kit.inventory, 'tideSpear'],
    equipment: {
      ...kit.equipment,
      mainHand: 'tideSpear'
    }
  } : kit;
};
export const newHero = (build = {}) => {
  var _build$origin, _build$calling, _build$name, _build$deathMode;
  const origin = (_build$origin = build.origin) !== null && _build$origin !== void 0 ? _build$origin : 'mineborn';
  const calling = (_build$calling = build.calling) !== null && _build$calling !== void 0 ? _build$calling : 'trailguard';
  const kit = Object.keys(build).length ? starterKit(origin, calling) : {
    bombs: 4,
    ropes: 4,
    inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'],
    equipment: {
      mainHand: 'whip'
    }
  };
  return {
    name: ((_build$name = build.name) === null || _build$name === void 0 ? void 0 : _build$name.trim()) || 'Existing Courier',
    origin,
    calling,
    deathMode: (_build$deathMode = build.deathMode) !== null && _build$deathMode !== void 0 ? _build$deathMode : 'checkpoint',
    x: 0,
    y: 0,
    health: 22,
    maxHealth: 22,
    focus: 8,
    maxFocus: 8,
    gold: 0,
    bombs: kit.bombs,
    ropes: kit.ropes,
    keys: 0,
    xp: 0,
    level: 1,
    stats: {
      ...originStats[origin]
    },
    skills: [],
    inventory: kit.inventory,
    equipment: kit.equipment,
    conditions: [],
    cooldowns: {},
    trailcrafts: {},
    traversalTools: [],
    relics: [],
    relicCharges: {},
    boons: {},
    boonEvolutions: {},
    safePositions: []
  };
};
export function newRun(seed = Math.floor(Math.random() * 0x7fffffff), area = 'mine', areaFloor = 0, inheritedHero, rescuedNpcs = [], legacyRecords = [], areaOrder = DEFAULT_AREA_ORDER, cycle = initialCampaignCycle(), companions = [], companionDeathMode = 'injury') {
  var _hero$boons$windfall, _hero$boons;
  const routePosition = Math.max(0, areaOrder.indexOf(area));
  const floor = generateAreaFloor(seed, area, areaFloor, routePosition, cycle);
  const areaArc = areaArcStateFor(seed, area);
  applyAreaArcState(floor, areaArc);
  const hero = inheritedHero ? structuredClone(inheritedHero) : newHero();
  if (inheritedHero && areaFloor === 0 && routePosition > 0) hero.gold = Math.min(500, hero.gold + ((_hero$boons$windfall = (_hero$boons = hero.boons) === null || _hero$boons === void 0 ? void 0 : _hero$boons.windfall) !== null && _hero$boons$windfall !== void 0 ? _hero$boons$windfall : 0) * 20);
  hero.x = floor.start.x;
  hero.y = floor.start.y;
  const state = {
    version: 5,
    seed,
    floor,
    hero,
    messages: [`Landing file confirms ${biomeName[area]}.`, 'The mission archive lists H for help.'],
    status: 'playing',
    turn: 0,
    area,
    areaFloor,
    areaArc,
    areaOrder: [...areaOrder],
    ...(inheritedHero ? {
      replayHero: structuredClone(inheritedHero)
    } : {}),
    rescuedNpcs: rescuedNpcs.map(npc => ({
      ...npc
    })),
    companions: cloneCompanions(companions),
    companionDeathMode,
    lineageEvents: [],
    alignment: {
      kami: 0,
      villagePact: 0
    },
    reputation: emptySocialReputation(),
    campaignCycle: cloneCampaignCycle(cycle)
  };
  synchronizePartyActors(state, 'spawn');
  hydrateEncyclopediaLegacy(state, legacyRecords);
  state.telemetry = createRunTelemetry(state);
  refreshFov(state);
  recordSafePosition(state);
  return state;
}
export function newTransitRun(seed, area, inheritedHero, travel, rescuedNpcs = [], legacyRecords = [], areaOrder = DEFAULT_AREA_ORDER, cycle = initialCampaignCycle(), companions = [], companionDeathMode = 'injury') {
  var _travel$routeCacheChu;
  const state = newRun(seed, area, 0, inheritedHero, rescuedNpcs, legacyRecords, areaOrder, cycle, companions, companionDeathMode);
  const floor = state.floor;
  const patrolTemplate = structuredClone(floor.actors.find(actor => actor.hostile));
  const chunkWidth = floor.width;
  const residentCount = Math.min(3, travel.chunkCount - travel.residentStart);
  floor.width *= residentCount;
  const center = Math.floor(floor.height / 2);
  floor.tiles = Array.from({
    length: floor.width * floor.height
  }, () => ({
    kind: 'wall',
    explored: false,
    visible: false
  }));
  floor.actors = [];
  floor.items = [];
  floor.props = [];
  for (let localChunk = 0; localChunk < residentCount; localChunk++) {
    const chunk = travel.residentStart + localChunk;
    const restored = restoredChunk(travel, chunkWidth, floor.height, chunk, patrolTemplate);
    const offset = localChunk * chunkWidth;
    for (let y = 0; y < floor.height; y++) for (let x = 0; x < chunkWidth; x++) floor.tiles[y * floor.width + offset + x] = restored.tiles[y * chunkWidth + x];
    floor.actors.push(...restored.actors.map(actor => cloneActor(actor, actor.x + offset, actor.y)));
    floor.items.push(...restored.items.map(item => cloneItem(item, item.x + offset, item.y)));
    floor.props.push(...restored.props.map(prop => cloneProp(prop, prop.x + offset, prop.y)));
  }
  const start = {
    x: 2,
    y: center
  };
  const lastChunk = travel.residentStart + residentCount - 1;
  const exit = {
    x: floor.width - 3,
    y: routeY(travel, chunkWidth, lastChunk, chunkWidth - 3, center)
  };
  floor.tiles[exit.y * floor.width + exit.x].kind = 'exit';
  floor.start = start;
  floor.exit = exit;
  floor.layoutId = 'voyager-link-corridor';
  floor.encounters = [];
  floor.milestones = [];
  floor.sideSpaces = [];
  floor.secretRooms = [];
  floor.secretRoutes = [];
  floor.ecology = [];
  const cacheChunk = (_travel$routeCacheChu = travel.routeCacheChunks) === null || _travel$routeCacheChu === void 0 ? void 0 : _travel$routeCacheChu.find(chunk => chunk >= travel.residentStart && chunk < travel.residentStart + residentCount);
  if (cacheChunk !== undefined) {
    const localChunk = cacheChunk - travel.residentStart;
    const x = localChunk * chunkWidth + Math.floor(chunkWidth * .62);
    floor.routeCache = {
      linkId: travel.linkId,
      x,
      y: routeY(travel, chunkWidth, cacheChunk, Math.floor(chunkWidth * .62), center)
    };
  }
  floor.guardianDefeated = true;
  floor.objective = {
    id: `link:${travel.linkId}`,
    kind: 'recoverSupplies',
    status: 'complete',
    label: 'Reach the far airlock'
  };
  state.travel = travel;
  state.hero.x = start.x;
  state.hero.y = start.y;
  state.messages = [`Link ${travel.linkId} streams through ${travel.chunkCount} physical partitions.`, `Resident chunks ${travel.residentStart + 1}-${travel.residentStart + residentCount}; route situations: ${travel.situations.slice(travel.residentStart, travel.residentStart + residentCount).join(', ')}.`];
  synchronizePartyActors(state, 'spawn');
  refreshFov(state);
  return state;
}
export function advanceTransitWindow(state) {
  var _state$area;
  const travel = state.travel;
  if (!travel || travel.chunkCount <= 3) return false;
  const chunkWidth = Math.floor(state.floor.width / Math.min(3, travel.chunkCount - travel.residentStart));
  const shiftForward = state.hero.x >= chunkWidth * 2 && travel.residentStart + 3 < travel.chunkCount;
  const shiftBackward = state.hero.x < chunkWidth && travel.residentStart > 0;
  if (!shiftForward && !shiftBackward) return false;
  const residentStart = travel.residentStart + (shiftForward ? 1 : -1);
  const remembered = rememberTransitWindow(state);
  if (!remembered) return false;
  const nextTravel = {
    ...remembered,
    residentStart,
    activeChunk: Math.max(0, Math.min(travel.chunkCount - 1, residentStart + 1))
  };
  const next = newTransitRun(state.seed, (_state$area = state.area) !== null && _state$area !== void 0 ? _state$area : state.floor.biome, state.hero, nextTravel, state.rescuedNpcs, [], state.areaOrder, state.campaignCycle, state.companions, state.companionDeathMode);
  state.floor = next.floor;
  state.travel = nextTravel;
  state.hero.x += shiftForward ? -chunkWidth : chunkWidth;
  state.hero.y = Math.max(1, Math.min(state.floor.height - 2, state.hero.y));
  synchronizePartyActors(state, 'spawn');
  refreshFov(state);
  return true;
}
export const newSeededCampaignRun = (seed, inheritedHero, rescuedNpcs = [], legacyRecords = [], cycle = initialCampaignCycle(), companions = [], companionDeathMode = 'injury') => {
  const areaOrder = campaignOrderForSeed(seed);
  return newRun(seed, areaOrder[0], 0, inheritedHero, rescuedNpcs, legacyRecords, areaOrder, cycle, companions, companionDeathMode);
};
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJiaW9tZU5hbWUiLCJnZW5lcmF0ZUFyZWFGbG9vciIsInJlZnJlc2hGb3YiLCJoeWRyYXRlRW5jeWNsb3BlZGlhTGVnYWN5IiwiY3JlYXRlUnVuVGVsZW1ldHJ5IiwicmVjb3JkU2FmZVBvc2l0aW9uIiwiY2xvbmVDYW1wYWlnbkN5Y2xlIiwiREVGQVVMVF9BUkVBX09SREVSIiwiaW5pdGlhbENhbXBhaWduQ3ljbGUiLCJjYW1wYWlnbk9yZGVyRm9yU2VlZCIsImFwcGx5QXJlYUFyY1N0YXRlIiwiYXJlYUFyY1N0YXRlRm9yIiwiZW1wdHlTb2NpYWxSZXB1dGF0aW9uIiwiY2xvbmVDb21wYW5pb25zIiwiaXNDb21wYW5pb25BY3RvciIsInN5bmNocm9uaXplUGFydHlBY3RvcnMiLCJyb3V0ZU9mZnNldEZvciIsImxpbmtJZCIsInJlZHVjZSIsInRvdGFsIiwiY2hhciIsImNoYXJDb2RlQXQiLCJyb3V0ZUNodW5rS2V5IiwiY2h1bmsiLCJTdHJpbmciLCJjbG9uZUFjdG9yIiwiYWN0b3IiLCJ4IiwieSIsInN0cnVjdHVyZWRDbG9uZSIsImNsb25lSXRlbSIsIml0ZW0iLCJjbG9uZVByb3AiLCJwcm9wIiwiX3Byb3AkZWZmZWN0Q2VsbHMiLCJlZmZlY3RDZWxscyIsIm1hcCIsImNlbGwiLCJyb3V0ZVkiLCJ0cmF2ZWwiLCJjaHVua1dpZHRoIiwibG9jYWxYIiwiY2VudGVyIiwiTWF0aCIsInJvdW5kIiwic2luIiwicm91dGVUZXJyYWluIiwiaGVpZ2h0Iiwid2lkdGgiLCJmbG9vciIsInRpbGVzIiwiQXJyYXkiLCJmcm9tIiwibGVuZ3RoIiwia2luZCIsImV4cGxvcmVkIiwidmlzaWJsZSIsImNhcnZlIiwicmFkaXVzIiwiZHkiLCJkeCIsInB4IiwicHkiLCJicmFuY2giLCJzaXR1YXRpb24iLCJzaXR1YXRpb25zIiwicm91dGVBY3RvcnMiLCJwYXRyb2xUZW1wbGF0ZSIsIm1pbiIsImlkIiwiaGVhbHRoIiwibWF4SGVhbHRoIiwicm9sZSIsIm5hbWUiLCJhdHRhY2siLCJkZWZlbnNlIiwic3BlZWQiLCJlbmVyZ3kiLCJnbHlwaCIsImNvbG9yIiwiaG9zdGlsZSIsInJlc3RvcmVkQ2h1bmsiLCJfdHJhdmVsJGNodW5rcyIsIl9tZW1vcnkkaXRlbXMiLCJfbWVtb3J5JHByb3BzIiwibWVtb3J5IiwiY2h1bmtzIiwiY2hhbmdlIiwiX21lbW9yeSR0ZXJyYWluIiwidGVycmFpbiIsInRpbGUiLCJpbmRleCIsImVsZXZhdGlvbiIsImZsb3ciLCJ1bmRlZmluZWQiLCJhY3RvcnMiLCJpdGVtcyIsInByb3BzIiwicmVtZW1iZXJlZFRlcnJhaW4iLCJjdXJyZW50IiwiYmFzZWxpbmUiLCJKU09OIiwic3RyaW5naWZ5IiwicmVtZW1iZXJUcmFuc2l0V2luZG93Iiwic3RhdGUiLCJfdHJhdmVsJGNodW5rczIiLCJyZXNpZGVudENvdW50IiwiY2h1bmtDb3VudCIsInJlc2lkZW50U3RhcnQiLCJsb2NhbENodW5rIiwib2Zmc2V0IiwiZmxhdE1hcCIsInNlcnZpY2VFeGl0IiwiaW5DaHVuayIsImZpbHRlciIsIm9yaWdpblN0YXRzIiwibWluZWJvcm4iLCJzdHJlbmd0aCIsImFnaWxpdHkiLCJ2aXRhbGl0eSIsImludGVsbGVjdCIsIm1vc3N3YWxrZXIiLCJjYXZlcm5TZWVrZXIiLCJ0aWRlYm91bmQiLCJzdGFydGVyS2l0Iiwib3JpZ2luIiwiY2FsbGluZyIsImtpdCIsImJvbWJzIiwicm9wZXMiLCJpbnZlbnRvcnkiLCJlcXVpcG1lbnQiLCJtYWluSGFuZCIsIm9mZkhhbmQiLCJuZXdIZXJvIiwiYnVpbGQiLCJfYnVpbGQkb3JpZ2luIiwiX2J1aWxkJGNhbGxpbmciLCJfYnVpbGQkbmFtZSIsIl9idWlsZCRkZWF0aE1vZGUiLCJPYmplY3QiLCJrZXlzIiwidHJpbSIsImRlYXRoTW9kZSIsImZvY3VzIiwibWF4Rm9jdXMiLCJnb2xkIiwieHAiLCJsZXZlbCIsInN0YXRzIiwic2tpbGxzIiwiY29uZGl0aW9ucyIsImNvb2xkb3ducyIsInRyYWlsY3JhZnRzIiwidHJhdmVyc2FsVG9vbHMiLCJyZWxpY3MiLCJyZWxpY0NoYXJnZXMiLCJib29ucyIsImJvb25Fdm9sdXRpb25zIiwic2FmZVBvc2l0aW9ucyIsIm5ld1J1biIsInNlZWQiLCJyYW5kb20iLCJhcmVhIiwiYXJlYUZsb29yIiwiaW5oZXJpdGVkSGVybyIsInJlc2N1ZWROcGNzIiwibGVnYWN5UmVjb3JkcyIsImFyZWFPcmRlciIsImN5Y2xlIiwiY29tcGFuaW9ucyIsImNvbXBhbmlvbkRlYXRoTW9kZSIsIl9oZXJvJGJvb25zJHdpbmRmYWxsIiwiX2hlcm8kYm9vbnMiLCJyb3V0ZVBvc2l0aW9uIiwibWF4IiwiaW5kZXhPZiIsImFyZWFBcmMiLCJoZXJvIiwid2luZGZhbGwiLCJzdGFydCIsInZlcnNpb24iLCJtZXNzYWdlcyIsInN0YXR1cyIsInR1cm4iLCJyZXBsYXlIZXJvIiwibnBjIiwibGluZWFnZUV2ZW50cyIsImFsaWdubWVudCIsImthbWkiLCJ2aWxsYWdlUGFjdCIsInJlcHV0YXRpb24iLCJjYW1wYWlnbkN5Y2xlIiwidGVsZW1ldHJ5IiwibmV3VHJhbnNpdFJ1biIsIl90cmF2ZWwkcm91dGVDYWNoZUNodSIsImZpbmQiLCJyZXN0b3JlZCIsInB1c2giLCJsYXN0Q2h1bmsiLCJleGl0IiwibGF5b3V0SWQiLCJlbmNvdW50ZXJzIiwibWlsZXN0b25lcyIsInNpZGVTcGFjZXMiLCJzZWNyZXRSb29tcyIsInNlY3JldFJvdXRlcyIsImVjb2xvZ3kiLCJjYWNoZUNodW5rIiwicm91dGVDYWNoZUNodW5rcyIsInJvdXRlQ2FjaGUiLCJndWFyZGlhbkRlZmVhdGVkIiwib2JqZWN0aXZlIiwibGFiZWwiLCJzbGljZSIsImpvaW4iLCJhZHZhbmNlVHJhbnNpdFdpbmRvdyIsIl9zdGF0ZSRhcmVhIiwic2hpZnRGb3J3YXJkIiwic2hpZnRCYWNrd2FyZCIsInJlbWVtYmVyZWQiLCJuZXh0VHJhdmVsIiwiYWN0aXZlQ2h1bmsiLCJuZXh0IiwiYmlvbWUiLCJuZXdTZWVkZWRDYW1wYWlnblJ1biJdLCJzb3VyY2VzIjpbInJ1bi50cyJdLCJzb3VyY2VzQ29udGVudCI6WyJpbXBvcnQgeyBiaW9tZU5hbWUgfSBmcm9tICcuLi9jb250ZW50J1xuaW1wb3J0IHsgZ2VuZXJhdGVBcmVhRmxvb3IgfSBmcm9tICcuLi93b3JsZCdcbmltcG9ydCB0eXBlIHsgQWN0b3IsIEJpb21lLCBDYW1wYWlnbkN5Y2xlLCBDb21wYW5pb24sIENvbXBhbmlvbkRlYXRoTW9kZSwgQ291cmllckNhbGxpbmcsIENvdXJpZXJPcmlnaW4sIERlYXRoTW9kZSwgR3JvdW5kSXRlbSwgSGVybywgTGVnYWN5UmVjb3JkLCBQcm9wLCBSZXNjdWVkTnBjLCBSdW5TdGF0ZSwgVGlsZSwgVHJhbnNpdENodW5rTWVtb3J5IH0gZnJvbSAnLi4vdHlwZXMnXG5pbXBvcnQgeyByZWZyZXNoRm92IH0gZnJvbSAnLi92aXNpYmlsaXR5J1xuaW1wb3J0IHsgaHlkcmF0ZUVuY3ljbG9wZWRpYUxlZ2FjeSB9IGZyb20gJy4vZW5jeWNsb3BlZGlhJ1xuaW1wb3J0IHsgY3JlYXRlUnVuVGVsZW1ldHJ5IH0gZnJvbSAnLi4vdGVsZW1ldHJ5J1xuaW1wb3J0IHsgcmVjb3JkU2FmZVBvc2l0aW9uIH0gZnJvbSAnLi9idWlsZGNyYWZ0J1xuaW1wb3J0IHsgY2xvbmVDYW1wYWlnbkN5Y2xlLCBERUZBVUxUX0FSRUFfT1JERVIsIGluaXRpYWxDYW1wYWlnbkN5Y2xlLCBjYW1wYWlnbk9yZGVyRm9yU2VlZCB9IGZyb20gJy4vY2FtcGFpZ24nXG5pbXBvcnQgeyBhcHBseUFyZWFBcmNTdGF0ZSwgYXJlYUFyY1N0YXRlRm9yIH0gZnJvbSAnLi4vZXNjYWxhdGlvbidcbmltcG9ydCB7IGVtcHR5U29jaWFsUmVwdXRhdGlvbiB9IGZyb20gJy4uL3NvY2lhbC1jb250cmFjdCdcbmltcG9ydCB7IGNsb25lQ29tcGFuaW9ucyB9IGZyb20gJy4vY29tcGFuaW9ucydcbmltcG9ydCB7IGlzQ29tcGFuaW9uQWN0b3IsIHN5bmNocm9uaXplUGFydHlBY3RvcnMgfSBmcm9tICcuL3BhcnR5J1xuXG5leHBvcnQgaW50ZXJmYWNlIENvdXJpZXJCdWlsZCB7IG5hbWU6IHN0cmluZzsgb3JpZ2luOiBDb3VyaWVyT3JpZ2luOyBjYWxsaW5nOiBDb3VyaWVyQ2FsbGluZzsgZGVhdGhNb2RlOiBEZWF0aE1vZGUgfVxuXG5jb25zdCByb3V0ZU9mZnNldEZvciA9IChsaW5rSWQ6IHN0cmluZyk6IG51bWJlciA9PiBbLi4ubGlua0lkXS5yZWR1Y2UoKHRvdGFsLCBjaGFyKSA9PiB0b3RhbCArIGNoYXIuY2hhckNvZGVBdCgwKSwgMClcbmNvbnN0IHJvdXRlQ2h1bmtLZXkgPSAoY2h1bms6IG51bWJlcik6IHN0cmluZyA9PiBTdHJpbmcoY2h1bmspXG5jb25zdCBjbG9uZUFjdG9yID0gKGFjdG9yOiBBY3RvciwgeCA9IGFjdG9yLngsIHkgPSBhY3Rvci55KTogQWN0b3IgPT4gKHsgLi4uc3RydWN0dXJlZENsb25lKGFjdG9yKSwgeCwgeSB9KVxuY29uc3QgY2xvbmVJdGVtID0gKGl0ZW06IEdyb3VuZEl0ZW0sIHggPSBpdGVtLngsIHkgPSBpdGVtLnkpOiBHcm91bmRJdGVtID0+ICh7IC4uLnN0cnVjdHVyZWRDbG9uZShpdGVtKSwgeCwgeSB9KVxuY29uc3QgY2xvbmVQcm9wID0gKHByb3A6IFByb3AsIHggPSBwcm9wLngsIHkgPSBwcm9wLnkpOiBQcm9wID0+ICh7IC4uLnN0cnVjdHVyZWRDbG9uZShwcm9wKSwgeCwgeSwgZWZmZWN0Q2VsbHM6IHByb3AuZWZmZWN0Q2VsbHM/Lm1hcChjZWxsID0+ICh7IHg6IGNlbGwueCAtIHByb3AueCArIHgsIHk6IGNlbGwueSAtIHByb3AueSArIHkgfSkpIH0pXG5cbmNvbnN0IHJvdXRlWSA9ICh0cmF2ZWw6IE5vbk51bGxhYmxlPFJ1blN0YXRlWyd0cmF2ZWwnXT4sIGNodW5rV2lkdGg6IG51bWJlciwgY2h1bms6IG51bWJlciwgbG9jYWxYOiBudW1iZXIsIGNlbnRlcjogbnVtYmVyKTogbnVtYmVyID0+IGNlbnRlciArIE1hdGgucm91bmQoTWF0aC5zaW4oKGNodW5rICogY2h1bmtXaWR0aCArIGxvY2FsWCArIHJvdXRlT2Zmc2V0Rm9yKHRyYXZlbC5saW5rSWQpKSAvIDcpICogMylcblxuY29uc3Qgcm91dGVUZXJyYWluID0gKHRyYXZlbDogTm9uTnVsbGFibGU8UnVuU3RhdGVbJ3RyYXZlbCddPiwgY2h1bmtXaWR0aDogbnVtYmVyLCBoZWlnaHQ6IG51bWJlciwgY2h1bms6IG51bWJlcik6IFRpbGVbXSA9PiB7XG4gIGNvbnN0IHdpZHRoID0gY2h1bmtXaWR0aFxuICBjb25zdCBjZW50ZXIgPSBNYXRoLmZsb29yKGhlaWdodCAvIDIpXG4gIGNvbnN0IHRpbGVzOiBUaWxlW10gPSBBcnJheS5mcm9tKHsgbGVuZ3RoOiB3aWR0aCAqIGhlaWdodCB9LCAoKSA9PiAoeyBraW5kOiAnd2FsbCcsIGV4cGxvcmVkOiBmYWxzZSwgdmlzaWJsZTogZmFsc2UgfSkpXG4gIGNvbnN0IGNhcnZlID0gKHg6IG51bWJlciwgeTogbnVtYmVyLCByYWRpdXMgPSAyKSA9PiB7XG4gICAgZm9yIChsZXQgZHkgPSAtcmFkaXVzOyBkeSA8PSByYWRpdXM7IGR5KyspIGZvciAobGV0IGR4ID0gLTE7IGR4IDw9IDE7IGR4KyspIHtcbiAgICAgIGNvbnN0IHB4ID0geCArIGR4XG4gICAgICBjb25zdCBweSA9IHkgKyBkeVxuICAgICAgaWYgKHB4ID4gMCAmJiBweCA8IHdpZHRoIC0gMSAmJiBweSA+IDAgJiYgcHkgPCBoZWlnaHQgLSAxKSB0aWxlc1tweSAqIHdpZHRoICsgcHhdIS5raW5kID0gJ2Zsb29yJ1xuICAgIH1cbiAgfVxuICBmb3IgKGxldCB4ID0gMjsgeCA8IHdpZHRoIC0gMjsgeCsrKSB7XG4gICAgY29uc3QgeSA9IHJvdXRlWSh0cmF2ZWwsIGNodW5rV2lkdGgsIGNodW5rLCB4LCBjZW50ZXIpXG4gICAgY2FydmUoeCwgeSlcbiAgICBpZiAoKGNodW5rICogd2lkdGggKyB4KSAlIDEzID09PSAwKSBmb3IgKGxldCBicmFuY2ggPSAxOyBicmFuY2ggPCA3OyBicmFuY2grKykgY2FydmUoeCwgeSArICgoY2h1bmsgKiB3aWR0aCArIHgpICUgMjYgPyBicmFuY2ggOiAtYnJhbmNoKSwgMSlcbiAgICBjb25zdCBzaXR1YXRpb24gPSB0cmF2ZWwuc2l0dWF0aW9uc1tjaHVua11cbiAgICBpZiAoc2l0dWF0aW9uID09PSAnaGF6YXJkJyAmJiAoY2h1bmsgKiB3aWR0aCArIHgpICUgMTkgPT09IDApIHRpbGVzW3kgKiB3aWR0aCArIHhdIS5raW5kID0gJ2dhcydcbiAgICBpZiAoc2l0dWF0aW9uID09PSAnZWNvbG9neScgJiYgKGNodW5rICogd2lkdGggKyB4KSAlIDIzID09PSAwKSB0aWxlc1t5ICogd2lkdGggKyB4XSEua2luZCA9ICdicmFtYmxlJ1xuICB9XG4gIHRpbGVzW2NlbnRlciAqIHdpZHRoICsgMl0hLmtpbmQgPSAnZmxvb3InXG4gIHJldHVybiB0aWxlc1xufVxuXG5jb25zdCByb3V0ZUFjdG9ycyA9ICh0cmF2ZWw6IE5vbk51bGxhYmxlPFJ1blN0YXRlWyd0cmF2ZWwnXT4sIGNodW5rV2lkdGg6IG51bWJlciwgaGVpZ2h0OiBudW1iZXIsIGNodW5rOiBudW1iZXIsIHBhdHJvbFRlbXBsYXRlOiBBY3RvciB8IHVuZGVmaW5lZCk6IEFjdG9yW10gPT4ge1xuICBjb25zdCB4ID0gTWF0aC5taW4oY2h1bmtXaWR0aCAtIDUsIE1hdGguZmxvb3IoY2h1bmtXaWR0aCAqIC42MikpXG4gIGNvbnN0IHkgPSByb3V0ZVkodHJhdmVsLCBjaHVua1dpZHRoLCBjaHVuaywgeCwgTWF0aC5mbG9vcihoZWlnaHQgLyAyKSlcbiAgaWYgKHRyYXZlbC5zaXR1YXRpb25zW2NodW5rXSA9PT0gJ3BhdHJvbCcgJiYgcGF0cm9sVGVtcGxhdGUpIHJldHVybiBbeyAuLi5jbG9uZUFjdG9yKHBhdHJvbFRlbXBsYXRlLCB4LCB5KSwgaWQ6IGByb3V0ZS1wYXRyb2w6JHt0cmF2ZWwubGlua0lkfToke2NodW5rfWAsIGhlYWx0aDogcGF0cm9sVGVtcGxhdGUubWF4SGVhbHRoLCBtYXhIZWFsdGg6IHBhdHJvbFRlbXBsYXRlLm1heEhlYWx0aCB9XVxuICBpZiAodHJhdmVsLnNpdHVhdGlvbnNbY2h1bmtdID09PSAndHJhZGVyJykgcmV0dXJuIFt7IGlkOiBgcm91dGUtdHJhZGVyOiR7dHJhdmVsLmxpbmtJZH06JHtjaHVua31gLCByb2xlOiAnbWVyY2hhbnQnLCBraW5kOiAnbWVyY2hhbnQnLCBuYW1lOiAncm91dGUgdHJhZGVyJywgeCwgeSwgaGVhbHRoOiAxLCBtYXhIZWFsdGg6IDEsIGF0dGFjazogMCwgZGVmZW5zZTogMCwgc3BlZWQ6IDAsIGVuZXJneTogMCwgZ2x5cGg6ICckJywgY29sb3I6ICcjZjRkMjZhJywgaG9zdGlsZTogZmFsc2UgfV1cbiAgcmV0dXJuIFtdXG59XG5cbmNvbnN0IHJlc3RvcmVkQ2h1bmsgPSAodHJhdmVsOiBOb25OdWxsYWJsZTxSdW5TdGF0ZVsndHJhdmVsJ10+LCBjaHVua1dpZHRoOiBudW1iZXIsIGhlaWdodDogbnVtYmVyLCBjaHVuazogbnVtYmVyLCBwYXRyb2xUZW1wbGF0ZTogQWN0b3IgfCB1bmRlZmluZWQpOiB7IHRpbGVzOiBUaWxlW107IGFjdG9yczogQWN0b3JbXTsgaXRlbXM6IEdyb3VuZEl0ZW1bXTsgcHJvcHM6IFByb3BbXSB9ID0+IHtcbiAgY29uc3QgdGlsZXMgPSByb3V0ZVRlcnJhaW4odHJhdmVsLCBjaHVua1dpZHRoLCBoZWlnaHQsIGNodW5rKVxuICBjb25zdCBtZW1vcnkgPSB0cmF2ZWwuY2h1bmtzPy5bcm91dGVDaHVua0tleShjaHVuayldXG4gIGZvciAoY29uc3QgY2hhbmdlIG9mIG1lbW9yeT8udGVycmFpbiA/PyBbXSkge1xuICAgIGNvbnN0IHRpbGUgPSB0aWxlc1tjaGFuZ2UuaW5kZXhdXG4gICAgaWYgKHRpbGUpIHtcbiAgICAgIHRpbGUua2luZCA9IGNoYW5nZS5raW5kXG4gICAgICB0aWxlLmVsZXZhdGlvbiA9IGNoYW5nZS5lbGV2YXRpb25cbiAgICAgIHRpbGUuZmxvdyA9IGNoYW5nZS5mbG93ID8gc3RydWN0dXJlZENsb25lKGNoYW5nZS5mbG93KSA6IHVuZGVmaW5lZFxuICAgICAgdGlsZS5leHBsb3JlZCA9IGZhbHNlXG4gICAgICB0aWxlLnZpc2libGUgPSBmYWxzZVxuICAgIH1cbiAgfVxuICByZXR1cm4ge1xuICAgIHRpbGVzLFxuICAgIGFjdG9yczogbWVtb3J5ID8gbWVtb3J5LmFjdG9ycy5tYXAoYWN0b3IgPT4gY2xvbmVBY3RvcihhY3RvcikpIDogcm91dGVBY3RvcnModHJhdmVsLCBjaHVua1dpZHRoLCBoZWlnaHQsIGNodW5rLCBwYXRyb2xUZW1wbGF0ZSksXG4gICAgaXRlbXM6IChtZW1vcnk/Lml0ZW1zID8/IFtdKS5tYXAoaXRlbSA9PiBjbG9uZUl0ZW0oaXRlbSkpLFxuICAgIHByb3BzOiAobWVtb3J5Py5wcm9wcyA/PyBbXSkubWFwKHByb3AgPT4gY2xvbmVQcm9wKHByb3ApKVxuICB9XG59XG5cbmNvbnN0IHJlbWVtYmVyZWRUZXJyYWluID0gKGN1cnJlbnQ6IFRpbGUsIGJhc2VsaW5lOiBUaWxlKTogVHJhbnNpdENodW5rTWVtb3J5Wyd0ZXJyYWluJ11bbnVtYmVyXSB8IHVuZGVmaW5lZCA9PiB7XG4gIGlmIChjdXJyZW50LmtpbmQgPT09IGJhc2VsaW5lLmtpbmQgJiYgY3VycmVudC5lbGV2YXRpb24gPT09IGJhc2VsaW5lLmVsZXZhdGlvbiAmJiBKU09OLnN0cmluZ2lmeShjdXJyZW50LmZsb3cpID09PSBKU09OLnN0cmluZ2lmeShiYXNlbGluZS5mbG93KSkgcmV0dXJuIHVuZGVmaW5lZFxuICByZXR1cm4geyBpbmRleDogMCwga2luZDogY3VycmVudC5raW5kLCAuLi4oY3VycmVudC5lbGV2YXRpb24gPT09IHVuZGVmaW5lZCA/IHt9IDogeyBlbGV2YXRpb246IGN1cnJlbnQuZWxldmF0aW9uIH0pLCAuLi4oY3VycmVudC5mbG93ID09PSB1bmRlZmluZWQgPyB7fSA6IHsgZmxvdzogc3RydWN0dXJlZENsb25lKGN1cnJlbnQuZmxvdykgfSkgfVxufVxuXG5jb25zdCByZW1lbWJlclRyYW5zaXRXaW5kb3cgPSAoc3RhdGU6IFJ1blN0YXRlKTogTm9uTnVsbGFibGU8UnVuU3RhdGVbJ3RyYXZlbCddPiB8IHVuZGVmaW5lZCA9PiB7XG4gIGNvbnN0IHRyYXZlbCA9IHN0YXRlLnRyYXZlbFxuICBpZiAoIXRyYXZlbCkgcmV0dXJuIHVuZGVmaW5lZFxuICBjb25zdCByZXNpZGVudENvdW50ID0gTWF0aC5taW4oMywgdHJhdmVsLmNodW5rQ291bnQgLSB0cmF2ZWwucmVzaWRlbnRTdGFydClcbiAgY29uc3QgY2h1bmtXaWR0aCA9IE1hdGguZmxvb3Ioc3RhdGUuZmxvb3Iud2lkdGggLyByZXNpZGVudENvdW50KVxuICBjb25zdCBjaHVua3MgPSB7IC4uLih0cmF2ZWwuY2h1bmtzID8/IHt9KSB9XG4gIGZvciAobGV0IGxvY2FsQ2h1bmsgPSAwOyBsb2NhbENodW5rIDwgcmVzaWRlbnRDb3VudDsgbG9jYWxDaHVuaysrKSB7XG4gICAgY29uc3QgY2h1bmsgPSB0cmF2ZWwucmVzaWRlbnRTdGFydCArIGxvY2FsQ2h1bmtcbiAgICBjb25zdCBiYXNlbGluZSA9IHJvdXRlVGVycmFpbih0cmF2ZWwsIGNodW5rV2lkdGgsIHN0YXRlLmZsb29yLmhlaWdodCwgY2h1bmspXG4gICAgY29uc3Qgb2Zmc2V0ID0gbG9jYWxDaHVuayAqIGNodW5rV2lkdGhcbiAgICBjb25zdCB0ZXJyYWluID0gYmFzZWxpbmUuZmxhdE1hcCgodGlsZSwgaW5kZXgpID0+IHtcbiAgICAgIGNvbnN0IHNlcnZpY2VFeGl0ID0gbG9jYWxDaHVuayA9PT0gcmVzaWRlbnRDb3VudCAtIDEgJiYgaW5kZXggPT09IHJvdXRlWSh0cmF2ZWwsIGNodW5rV2lkdGgsIGNodW5rLCBjaHVua1dpZHRoIC0gMywgTWF0aC5mbG9vcihzdGF0ZS5mbG9vci5oZWlnaHQgLyAyKSkgKiBjaHVua1dpZHRoICsgY2h1bmtXaWR0aCAtIDNcbiAgICAgIGlmIChzZXJ2aWNlRXhpdCkgcmV0dXJuIFtdXG4gICAgICBjb25zdCBtZW1vcnkgPSByZW1lbWJlcmVkVGVycmFpbihzdGF0ZS5mbG9vci50aWxlc1tNYXRoLmZsb29yKGluZGV4IC8gY2h1bmtXaWR0aCkgKiBzdGF0ZS5mbG9vci53aWR0aCArIG9mZnNldCArIGluZGV4ICUgY2h1bmtXaWR0aF0hLCB0aWxlKVxuICAgICAgcmV0dXJuIG1lbW9yeSA/IFt7IC4uLm1lbW9yeSwgaW5kZXggfV0gOiBbXVxuICAgIH0pXG4gICAgY29uc3QgaW5DaHVuayA9ICh4OiBudW1iZXIpID0+IHggPj0gb2Zmc2V0ICYmIHggPCBvZmZzZXQgKyBjaHVua1dpZHRoXG4gICAgY2h1bmtzW3JvdXRlQ2h1bmtLZXkoY2h1bmspXSA9IHtcbiAgICAgIHRlcnJhaW4sXG4gICAgICBhY3RvcnM6IHN0YXRlLmZsb29yLmFjdG9ycy5maWx0ZXIoYWN0b3IgPT4gIWlzQ29tcGFuaW9uQWN0b3IoYWN0b3IpICYmIGluQ2h1bmsoYWN0b3IueCkpLm1hcChhY3RvciA9PiBjbG9uZUFjdG9yKGFjdG9yLCBhY3Rvci54IC0gb2Zmc2V0LCBhY3Rvci55KSksXG4gICAgICBpdGVtczogc3RhdGUuZmxvb3IuaXRlbXMuZmlsdGVyKGl0ZW0gPT4gaW5DaHVuayhpdGVtLngpKS5tYXAoaXRlbSA9PiBjbG9uZUl0ZW0oaXRlbSwgaXRlbS54IC0gb2Zmc2V0LCBpdGVtLnkpKSxcbiAgICAgIHByb3BzOiBzdGF0ZS5mbG9vci5wcm9wcy5maWx0ZXIocHJvcCA9PiBpbkNodW5rKHByb3AueCkpLm1hcChwcm9wID0+IGNsb25lUHJvcChwcm9wLCBwcm9wLnggLSBvZmZzZXQsIHByb3AueSkpXG4gICAgfVxuICB9XG4gIHJldHVybiB7IC4uLnRyYXZlbCwgY2h1bmtzIH1cbn1cblxuY29uc3Qgb3JpZ2luU3RhdHM6IFJlY29yZDxDb3VyaWVyT3JpZ2luLCBIZXJvWydzdGF0cyddPiA9IHtcbiAgbWluZWJvcm46IHsgc3RyZW5ndGg6IDMsIGFnaWxpdHk6IDEsIHZpdGFsaXR5OiAzLCBpbnRlbGxlY3Q6IDEgfSxcbiAgbW9zc3dhbGtlcjogeyBzdHJlbmd0aDogMSwgYWdpbGl0eTogMywgdml0YWxpdHk6IDMsIGludGVsbGVjdDogMSB9LFxuICBjYXZlcm5TZWVrZXI6IHsgc3RyZW5ndGg6IDEsIGFnaWxpdHk6IDIsIHZpdGFsaXR5OiAyLCBpbnRlbGxlY3Q6IDMgfSxcbiAgdGlkZWJvdW5kOiB7IHN0cmVuZ3RoOiAyLCBhZ2lsaXR5OiAzLCB2aXRhbGl0eTogMSwgaW50ZWxsZWN0OiAyIH1cbn1cblxuY29uc3Qgc3RhcnRlcktpdCA9IChvcmlnaW46IENvdXJpZXJPcmlnaW4sIGNhbGxpbmc6IENvdXJpZXJDYWxsaW5nKTogUGljazxIZXJvLCAnYm9tYnMnIHwgJ3JvcGVzJyB8ICdpbnZlbnRvcnknIHwgJ2VxdWlwbWVudCc+ID0+IHtcbiAgY29uc3Qga2l0ID0gY2FsbGluZyA9PT0gJ3RyYWlsZ3VhcmQnXG4gICAgPyB7IGJvbWJzOiA0LCByb3BlczogNCwgaW52ZW50b3J5OiBbJ3RvbmljJywgJ3JvY2snLCAnYm9tYlBhY2snLCAncm9wZUJ1bmRsZScsICdlbWJlciddLCBlcXVpcG1lbnQ6IHsgbWFpbkhhbmQ6ICd3aGlwJywgb2ZmSGFuZDogJ2J1Y2tsZXInIH0gfVxuICAgIDogY2FsbGluZyA9PT0gJ3BhdGhtYWtlcidcbiAgICAgID8geyBib21iczogNiwgcm9wZXM6IDYsIGludmVudG9yeTogWyd0b25pYycsICdyb2NrJywgJ2JvbWJQYWNrJywgJ2JvbWJQYWNrJywgJ3JvcGVCdW5kbGUnLCAncm9wZUJ1bmRsZScsICdlbWJlcicsICdtYXBTY3JvbGwnXSwgZXF1aXBtZW50OiB7IG1haW5IYW5kOiAnd2hpcCcgfSB9XG4gICAgICA6IHsgYm9tYnM6IDQsIHJvcGVzOiA0LCBpbnZlbnRvcnk6IFsndG9uaWMnLCAnZm9jdXNUb25pYycsICdyb2NrJywgJ2JvbWJQYWNrJywgJ3JvcGVCdW5kbGUnLCAnZW1iZXInLCAnc2lnaHQnXSwgZXF1aXBtZW50OiB7IG1haW5IYW5kOiAnd2hpcCcgfSB9XG4gIHJldHVybiBvcmlnaW4gPT09ICd0aWRlYm91bmQnID8geyAuLi5raXQsIGludmVudG9yeTogWy4uLmtpdC5pbnZlbnRvcnksICd0aWRlU3BlYXInXSwgZXF1aXBtZW50OiB7IC4uLmtpdC5lcXVpcG1lbnQsIG1haW5IYW5kOiAndGlkZVNwZWFyJyB9IH0gOiBraXRcbn1cblxuZXhwb3J0IGNvbnN0IG5ld0hlcm8gPSAoYnVpbGQ6IFBhcnRpYWw8Q291cmllckJ1aWxkPiA9IHt9KTogSGVybyA9PiB7XG4gIGNvbnN0IG9yaWdpbiA9IGJ1aWxkLm9yaWdpbiA/PyAnbWluZWJvcm4nXG4gIGNvbnN0IGNhbGxpbmcgPSBidWlsZC5jYWxsaW5nID8/ICd0cmFpbGd1YXJkJ1xuICBjb25zdCBraXQgPSBPYmplY3Qua2V5cyhidWlsZCkubGVuZ3RoID8gc3RhcnRlcktpdChvcmlnaW4sIGNhbGxpbmcpIDogeyBib21iczogNCwgcm9wZXM6IDQsIGludmVudG9yeTogWyd0b25pYycsICdyb2NrJywgJ2JvbWJQYWNrJywgJ3JvcGVCdW5kbGUnLCAnZW1iZXInXSwgZXF1aXBtZW50OiB7IG1haW5IYW5kOiAnd2hpcCcgfSB9XG4gIHJldHVybiB7XG4gICAgbmFtZTogYnVpbGQubmFtZT8udHJpbSgpIHx8ICdFeGlzdGluZyBDb3VyaWVyJywgb3JpZ2luLCBjYWxsaW5nLCBkZWF0aE1vZGU6IGJ1aWxkLmRlYXRoTW9kZSA/PyAnY2hlY2twb2ludCcsXG4gICAgeDogMCwgeTogMCwgaGVhbHRoOiAyMiwgbWF4SGVhbHRoOiAyMiwgZm9jdXM6IDgsIG1heEZvY3VzOiA4LCBnb2xkOiAwLCBib21iczoga2l0LmJvbWJzLCByb3Blczoga2l0LnJvcGVzLCBrZXlzOiAwLCB4cDogMCwgbGV2ZWw6IDEsXG4gICAgc3RhdHM6IHsgLi4ub3JpZ2luU3RhdHNbb3JpZ2luXSB9LCBza2lsbHM6IFtdLCBpbnZlbnRvcnk6IGtpdC5pbnZlbnRvcnksIGVxdWlwbWVudDoga2l0LmVxdWlwbWVudCwgY29uZGl0aW9uczogW10sIGNvb2xkb3duczoge30sIHRyYWlsY3JhZnRzOiB7fSwgdHJhdmVyc2FsVG9vbHM6IFtdLCByZWxpY3M6IFtdLCByZWxpY0NoYXJnZXM6IHt9LCBib29uczoge30sIGJvb25Fdm9sdXRpb25zOiB7fSwgc2FmZVBvc2l0aW9uczogW11cbiAgfVxufVxuXG5leHBvcnQgZnVuY3Rpb24gbmV3UnVuKHNlZWQgPSBNYXRoLmZsb29yKE1hdGgucmFuZG9tKCkgKiAweDdmZmZmZmZmKSwgYXJlYTogQmlvbWUgPSAnbWluZScsIGFyZWFGbG9vciA9IDAsIGluaGVyaXRlZEhlcm8/OiBIZXJvLCByZXNjdWVkTnBjczogcmVhZG9ubHkgUmVzY3VlZE5wY1tdID0gW10sIGxlZ2FjeVJlY29yZHM6IHJlYWRvbmx5IExlZ2FjeVJlY29yZFtdID0gW10sIGFyZWFPcmRlcjogcmVhZG9ubHkgQmlvbWVbXSA9IERFRkFVTFRfQVJFQV9PUkRFUiwgY3ljbGU6IENhbXBhaWduQ3ljbGUgPSBpbml0aWFsQ2FtcGFpZ25DeWNsZSgpLCBjb21wYW5pb25zOiByZWFkb25seSBDb21wYW5pb25bXSA9IFtdLCBjb21wYW5pb25EZWF0aE1vZGU6IENvbXBhbmlvbkRlYXRoTW9kZSA9ICdpbmp1cnknKTogUnVuU3RhdGUge1xuICBjb25zdCByb3V0ZVBvc2l0aW9uID0gTWF0aC5tYXgoMCwgYXJlYU9yZGVyLmluZGV4T2YoYXJlYSkpXG4gIGNvbnN0IGZsb29yID0gZ2VuZXJhdGVBcmVhRmxvb3Ioc2VlZCwgYXJlYSwgYXJlYUZsb29yLCByb3V0ZVBvc2l0aW9uLCBjeWNsZSlcbiAgY29uc3QgYXJlYUFyYyA9IGFyZWFBcmNTdGF0ZUZvcihzZWVkLCBhcmVhKVxuICBhcHBseUFyZWFBcmNTdGF0ZShmbG9vciwgYXJlYUFyYylcbiAgY29uc3QgaGVybyA9IGluaGVyaXRlZEhlcm8gPyBzdHJ1Y3R1cmVkQ2xvbmUoaW5oZXJpdGVkSGVybykgOiBuZXdIZXJvKClcbiAgaWYgKGluaGVyaXRlZEhlcm8gJiYgYXJlYUZsb29yID09PSAwICYmIHJvdXRlUG9zaXRpb24gPiAwKSBoZXJvLmdvbGQgPSBNYXRoLm1pbig1MDAsIGhlcm8uZ29sZCArIChoZXJvLmJvb25zPy53aW5kZmFsbCA/PyAwKSAqIDIwKVxuICBoZXJvLnggPSBmbG9vci5zdGFydC54XG4gIGhlcm8ueSA9IGZsb29yLnN0YXJ0LnlcbiAgY29uc3Qgc3RhdGU6IFJ1blN0YXRlID0geyB2ZXJzaW9uOiA1LCBzZWVkLCBmbG9vciwgaGVybywgbWVzc2FnZXM6IFtgTGFuZGluZyBmaWxlIGNvbmZpcm1zICR7YmlvbWVOYW1lW2FyZWFdfS5gLCAnVGhlIG1pc3Npb24gYXJjaGl2ZSBsaXN0cyBIIGZvciBoZWxwLiddLCBzdGF0dXM6ICdwbGF5aW5nJywgdHVybjogMCwgYXJlYSwgYXJlYUZsb29yLCBhcmVhQXJjLCBhcmVhT3JkZXI6IFsuLi5hcmVhT3JkZXJdLCAuLi4oaW5oZXJpdGVkSGVybyA/IHsgcmVwbGF5SGVybzogc3RydWN0dXJlZENsb25lKGluaGVyaXRlZEhlcm8pIH0gOiB7fSksIHJlc2N1ZWROcGNzOiByZXNjdWVkTnBjcy5tYXAobnBjID0+ICh7IC4uLm5wYyB9KSksIGNvbXBhbmlvbnM6IGNsb25lQ29tcGFuaW9ucyhjb21wYW5pb25zKSwgY29tcGFuaW9uRGVhdGhNb2RlLCBsaW5lYWdlRXZlbnRzOiBbXSwgYWxpZ25tZW50OiB7IGthbWk6IDAsIHZpbGxhZ2VQYWN0OiAwIH0sIHJlcHV0YXRpb246IGVtcHR5U29jaWFsUmVwdXRhdGlvbigpLCBjYW1wYWlnbkN5Y2xlOiBjbG9uZUNhbXBhaWduQ3ljbGUoY3ljbGUpIH1cbiAgc3luY2hyb25pemVQYXJ0eUFjdG9ycyhzdGF0ZSwgJ3NwYXduJylcbiAgaHlkcmF0ZUVuY3ljbG9wZWRpYUxlZ2FjeShzdGF0ZSwgbGVnYWN5UmVjb3JkcylcbiAgc3RhdGUudGVsZW1ldHJ5ID0gY3JlYXRlUnVuVGVsZW1ldHJ5KHN0YXRlKVxuICByZWZyZXNoRm92KHN0YXRlKVxuICByZWNvcmRTYWZlUG9zaXRpb24oc3RhdGUpXG4gIHJldHVybiBzdGF0ZVxufVxuXG5leHBvcnQgZnVuY3Rpb24gbmV3VHJhbnNpdFJ1bihzZWVkOiBudW1iZXIsIGFyZWE6IEJpb21lLCBpbmhlcml0ZWRIZXJvOiBIZXJvLCB0cmF2ZWw6IE5vbk51bGxhYmxlPFJ1blN0YXRlWyd0cmF2ZWwnXT4sIHJlc2N1ZWROcGNzOiByZWFkb25seSBSZXNjdWVkTnBjW10gPSBbXSwgbGVnYWN5UmVjb3JkczogcmVhZG9ubHkgTGVnYWN5UmVjb3JkW10gPSBbXSwgYXJlYU9yZGVyOiByZWFkb25seSBCaW9tZVtdID0gREVGQVVMVF9BUkVBX09SREVSLCBjeWNsZTogQ2FtcGFpZ25DeWNsZSA9IGluaXRpYWxDYW1wYWlnbkN5Y2xlKCksIGNvbXBhbmlvbnM6IHJlYWRvbmx5IENvbXBhbmlvbltdID0gW10sIGNvbXBhbmlvbkRlYXRoTW9kZTogQ29tcGFuaW9uRGVhdGhNb2RlID0gJ2luanVyeScpOiBSdW5TdGF0ZSB7XG4gIGNvbnN0IHN0YXRlID0gbmV3UnVuKHNlZWQsIGFyZWEsIDAsIGluaGVyaXRlZEhlcm8sIHJlc2N1ZWROcGNzLCBsZWdhY3lSZWNvcmRzLCBhcmVhT3JkZXIsIGN5Y2xlLCBjb21wYW5pb25zLCBjb21wYW5pb25EZWF0aE1vZGUpXG4gIGNvbnN0IGZsb29yID0gc3RhdGUuZmxvb3JcbiAgY29uc3QgcGF0cm9sVGVtcGxhdGUgPSBzdHJ1Y3R1cmVkQ2xvbmUoZmxvb3IuYWN0b3JzLmZpbmQoYWN0b3IgPT4gYWN0b3IuaG9zdGlsZSkpXG4gIGNvbnN0IGNodW5rV2lkdGggPSBmbG9vci53aWR0aFxuICBjb25zdCByZXNpZGVudENvdW50ID0gTWF0aC5taW4oMywgdHJhdmVsLmNodW5rQ291bnQgLSB0cmF2ZWwucmVzaWRlbnRTdGFydClcbiAgZmxvb3Iud2lkdGggKj0gcmVzaWRlbnRDb3VudFxuICBjb25zdCBjZW50ZXIgPSBNYXRoLmZsb29yKGZsb29yLmhlaWdodCAvIDIpXG4gIGZsb29yLnRpbGVzID0gQXJyYXkuZnJvbSh7IGxlbmd0aDogZmxvb3Iud2lkdGggKiBmbG9vci5oZWlnaHQgfSwgKCkgPT4gKHsga2luZDogJ3dhbGwnIGFzIGNvbnN0LCBleHBsb3JlZDogZmFsc2UsIHZpc2libGU6IGZhbHNlIH0pKVxuICBmbG9vci5hY3RvcnMgPSBbXVxuICBmbG9vci5pdGVtcyA9IFtdXG4gIGZsb29yLnByb3BzID0gW11cbiAgZm9yIChsZXQgbG9jYWxDaHVuayA9IDA7IGxvY2FsQ2h1bmsgPCByZXNpZGVudENvdW50OyBsb2NhbENodW5rKyspIHtcbiAgICBjb25zdCBjaHVuayA9IHRyYXZlbC5yZXNpZGVudFN0YXJ0ICsgbG9jYWxDaHVua1xuICAgIGNvbnN0IHJlc3RvcmVkID0gcmVzdG9yZWRDaHVuayh0cmF2ZWwsIGNodW5rV2lkdGgsIGZsb29yLmhlaWdodCwgY2h1bmssIHBhdHJvbFRlbXBsYXRlKVxuICAgIGNvbnN0IG9mZnNldCA9IGxvY2FsQ2h1bmsgKiBjaHVua1dpZHRoXG4gICAgZm9yIChsZXQgeSA9IDA7IHkgPCBmbG9vci5oZWlnaHQ7IHkrKykgZm9yIChsZXQgeCA9IDA7IHggPCBjaHVua1dpZHRoOyB4KyspIGZsb29yLnRpbGVzW3kgKiBmbG9vci53aWR0aCArIG9mZnNldCArIHhdID0gcmVzdG9yZWQudGlsZXNbeSAqIGNodW5rV2lkdGggKyB4XSFcbiAgICBmbG9vci5hY3RvcnMucHVzaCguLi5yZXN0b3JlZC5hY3RvcnMubWFwKGFjdG9yID0+IGNsb25lQWN0b3IoYWN0b3IsIGFjdG9yLnggKyBvZmZzZXQsIGFjdG9yLnkpKSlcbiAgICBmbG9vci5pdGVtcy5wdXNoKC4uLnJlc3RvcmVkLml0ZW1zLm1hcChpdGVtID0+IGNsb25lSXRlbShpdGVtLCBpdGVtLnggKyBvZmZzZXQsIGl0ZW0ueSkpKVxuICAgIGZsb29yLnByb3BzLnB1c2goLi4ucmVzdG9yZWQucHJvcHMubWFwKHByb3AgPT4gY2xvbmVQcm9wKHByb3AsIHByb3AueCArIG9mZnNldCwgcHJvcC55KSkpXG4gIH1cbiAgY29uc3Qgc3RhcnQgPSB7IHg6IDIsIHk6IGNlbnRlciB9XG4gIGNvbnN0IGxhc3RDaHVuayA9IHRyYXZlbC5yZXNpZGVudFN0YXJ0ICsgcmVzaWRlbnRDb3VudCAtIDFcbiAgY29uc3QgZXhpdCA9IHsgeDogZmxvb3Iud2lkdGggLSAzLCB5OiByb3V0ZVkodHJhdmVsLCBjaHVua1dpZHRoLCBsYXN0Q2h1bmssIGNodW5rV2lkdGggLSAzLCBjZW50ZXIpIH1cbiAgZmxvb3IudGlsZXNbZXhpdC55ICogZmxvb3Iud2lkdGggKyBleGl0LnhdIS5raW5kID0gJ2V4aXQnXG4gIGZsb29yLnN0YXJ0ID0gc3RhcnRcbiAgZmxvb3IuZXhpdCA9IGV4aXRcbiAgZmxvb3IubGF5b3V0SWQgPSAndm95YWdlci1saW5rLWNvcnJpZG9yJ1xuICBmbG9vci5lbmNvdW50ZXJzID0gW11cbiAgZmxvb3IubWlsZXN0b25lcyA9IFtdXG4gIGZsb29yLnNpZGVTcGFjZXMgPSBbXVxuICBmbG9vci5zZWNyZXRSb29tcyA9IFtdXG4gIGZsb29yLnNlY3JldFJvdXRlcyA9IFtdXG4gIGZsb29yLmVjb2xvZ3kgPSBbXVxuICBjb25zdCBjYWNoZUNodW5rID0gdHJhdmVsLnJvdXRlQ2FjaGVDaHVua3M/LmZpbmQoY2h1bmsgPT4gY2h1bmsgPj0gdHJhdmVsLnJlc2lkZW50U3RhcnQgJiYgY2h1bmsgPCB0cmF2ZWwucmVzaWRlbnRTdGFydCArIHJlc2lkZW50Q291bnQpXG4gIGlmIChjYWNoZUNodW5rICE9PSB1bmRlZmluZWQpIHtcbiAgICBjb25zdCBsb2NhbENodW5rID0gY2FjaGVDaHVuayAtIHRyYXZlbC5yZXNpZGVudFN0YXJ0XG4gICAgY29uc3QgeCA9IGxvY2FsQ2h1bmsgKiBjaHVua1dpZHRoICsgTWF0aC5mbG9vcihjaHVua1dpZHRoICogLjYyKVxuICAgIGZsb29yLnJvdXRlQ2FjaGUgPSB7IGxpbmtJZDogdHJhdmVsLmxpbmtJZCwgeCwgeTogcm91dGVZKHRyYXZlbCwgY2h1bmtXaWR0aCwgY2FjaGVDaHVuaywgTWF0aC5mbG9vcihjaHVua1dpZHRoICogLjYyKSwgY2VudGVyKSB9XG4gIH1cbiAgZmxvb3IuZ3VhcmRpYW5EZWZlYXRlZCA9IHRydWVcbiAgZmxvb3Iub2JqZWN0aXZlID0geyBpZDogYGxpbms6JHt0cmF2ZWwubGlua0lkfWAsIGtpbmQ6ICdyZWNvdmVyU3VwcGxpZXMnLCBzdGF0dXM6ICdjb21wbGV0ZScsIGxhYmVsOiAnUmVhY2ggdGhlIGZhciBhaXJsb2NrJyB9XG4gIHN0YXRlLnRyYXZlbCA9IHRyYXZlbFxuICBzdGF0ZS5oZXJvLnggPSBzdGFydC54XG4gIHN0YXRlLmhlcm8ueSA9IHN0YXJ0LnlcbiAgc3RhdGUubWVzc2FnZXMgPSBbYExpbmsgJHt0cmF2ZWwubGlua0lkfSBzdHJlYW1zIHRocm91Z2ggJHt0cmF2ZWwuY2h1bmtDb3VudH0gcGh5c2ljYWwgcGFydGl0aW9ucy5gLCBgUmVzaWRlbnQgY2h1bmtzICR7dHJhdmVsLnJlc2lkZW50U3RhcnQgKyAxfS0ke3RyYXZlbC5yZXNpZGVudFN0YXJ0ICsgcmVzaWRlbnRDb3VudH07IHJvdXRlIHNpdHVhdGlvbnM6ICR7dHJhdmVsLnNpdHVhdGlvbnMuc2xpY2UodHJhdmVsLnJlc2lkZW50U3RhcnQsIHRyYXZlbC5yZXNpZGVudFN0YXJ0ICsgcmVzaWRlbnRDb3VudCkuam9pbignLCAnKX0uYF1cbiAgc3luY2hyb25pemVQYXJ0eUFjdG9ycyhzdGF0ZSwgJ3NwYXduJylcbiAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgcmV0dXJuIHN0YXRlXG59XG5cbmV4cG9ydCBmdW5jdGlvbiBhZHZhbmNlVHJhbnNpdFdpbmRvdyhzdGF0ZTogUnVuU3RhdGUpOiBib29sZWFuIHtcbiAgY29uc3QgdHJhdmVsID0gc3RhdGUudHJhdmVsXG4gIGlmICghdHJhdmVsIHx8IHRyYXZlbC5jaHVua0NvdW50IDw9IDMpIHJldHVybiBmYWxzZVxuICBjb25zdCBjaHVua1dpZHRoID0gTWF0aC5mbG9vcihzdGF0ZS5mbG9vci53aWR0aCAvIE1hdGgubWluKDMsIHRyYXZlbC5jaHVua0NvdW50IC0gdHJhdmVsLnJlc2lkZW50U3RhcnQpKVxuICBjb25zdCBzaGlmdEZvcndhcmQgPSBzdGF0ZS5oZXJvLnggPj0gY2h1bmtXaWR0aCAqIDIgJiYgdHJhdmVsLnJlc2lkZW50U3RhcnQgKyAzIDwgdHJhdmVsLmNodW5rQ291bnRcbiAgY29uc3Qgc2hpZnRCYWNrd2FyZCA9IHN0YXRlLmhlcm8ueCA8IGNodW5rV2lkdGggJiYgdHJhdmVsLnJlc2lkZW50U3RhcnQgPiAwXG4gIGlmICghc2hpZnRGb3J3YXJkICYmICFzaGlmdEJhY2t3YXJkKSByZXR1cm4gZmFsc2VcbiAgY29uc3QgcmVzaWRlbnRTdGFydCA9IHRyYXZlbC5yZXNpZGVudFN0YXJ0ICsgKHNoaWZ0Rm9yd2FyZCA/IDEgOiAtMSlcbiAgY29uc3QgcmVtZW1iZXJlZCA9IHJlbWVtYmVyVHJhbnNpdFdpbmRvdyhzdGF0ZSlcbiAgaWYgKCFyZW1lbWJlcmVkKSByZXR1cm4gZmFsc2VcbiAgY29uc3QgbmV4dFRyYXZlbCA9IHsgLi4ucmVtZW1iZXJlZCwgcmVzaWRlbnRTdGFydCwgYWN0aXZlQ2h1bms6IE1hdGgubWF4KDAsIE1hdGgubWluKHRyYXZlbC5jaHVua0NvdW50IC0gMSwgcmVzaWRlbnRTdGFydCArIDEpKSB9XG4gIGNvbnN0IG5leHQgPSBuZXdUcmFuc2l0UnVuKHN0YXRlLnNlZWQsIHN0YXRlLmFyZWEgPz8gc3RhdGUuZmxvb3IuYmlvbWUsIHN0YXRlLmhlcm8sIG5leHRUcmF2ZWwsIHN0YXRlLnJlc2N1ZWROcGNzLCBbXSwgc3RhdGUuYXJlYU9yZGVyLCBzdGF0ZS5jYW1wYWlnbkN5Y2xlLCBzdGF0ZS5jb21wYW5pb25zLCBzdGF0ZS5jb21wYW5pb25EZWF0aE1vZGUpXG4gIHN0YXRlLmZsb29yID0gbmV4dC5mbG9vclxuICBzdGF0ZS50cmF2ZWwgPSBuZXh0VHJhdmVsXG4gIHN0YXRlLmhlcm8ueCArPSBzaGlmdEZvcndhcmQgPyAtY2h1bmtXaWR0aCA6IGNodW5rV2lkdGhcbiAgc3RhdGUuaGVyby55ID0gTWF0aC5tYXgoMSwgTWF0aC5taW4oc3RhdGUuZmxvb3IuaGVpZ2h0IC0gMiwgc3RhdGUuaGVyby55KSlcbiAgc3luY2hyb25pemVQYXJ0eUFjdG9ycyhzdGF0ZSwgJ3NwYXduJylcbiAgcmVmcmVzaEZvdihzdGF0ZSlcbiAgcmV0dXJuIHRydWVcbn1cblxuZXhwb3J0IGNvbnN0IG5ld1NlZWRlZENhbXBhaWduUnVuID0gKHNlZWQ6IG51bWJlciwgaW5oZXJpdGVkSGVybz86IEhlcm8sIHJlc2N1ZWROcGNzOiByZWFkb25seSBSZXNjdWVkTnBjW10gPSBbXSwgbGVnYWN5UmVjb3JkczogcmVhZG9ubHkgTGVnYWN5UmVjb3JkW10gPSBbXSwgY3ljbGU6IENhbXBhaWduQ3ljbGUgPSBpbml0aWFsQ2FtcGFpZ25DeWNsZSgpLCBjb21wYW5pb25zOiByZWFkb25seSBDb21wYW5pb25bXSA9IFtdLCBjb21wYW5pb25EZWF0aE1vZGU6IENvbXBhbmlvbkRlYXRoTW9kZSA9ICdpbmp1cnknKTogUnVuU3RhdGUgPT4ge1xuICBjb25zdCBhcmVhT3JkZXIgPSBjYW1wYWlnbk9yZGVyRm9yU2VlZChzZWVkKVxuICByZXR1cm4gbmV3UnVuKHNlZWQsIGFyZWFPcmRlclswXSwgMCwgaW5oZXJpdGVkSGVybywgcmVzY3VlZE5wY3MsIGxlZ2FjeVJlY29yZHMsIGFyZWFPcmRlciwgY3ljbGUsIGNvbXBhbmlvbnMsIGNvbXBhbmlvbkRlYXRoTW9kZSlcbn1cbiJdLCJtYXBwaW5ncyI6IkFBQUEsU0FBU0EsU0FBUyxRQUFRLFlBQVk7QUFDdEMsU0FBU0MsaUJBQWlCLFFBQVEsVUFBVTtBQUU1QyxTQUFTQyxVQUFVLFFBQVEsY0FBYztBQUN6QyxTQUFTQyx5QkFBeUIsUUFBUSxnQkFBZ0I7QUFDMUQsU0FBU0Msa0JBQWtCLFFBQVEsY0FBYztBQUNqRCxTQUFTQyxrQkFBa0IsUUFBUSxjQUFjO0FBQ2pELFNBQVNDLGtCQUFrQixFQUFFQyxrQkFBa0IsRUFBRUMsb0JBQW9CLEVBQUVDLG9CQUFvQixRQUFRLFlBQVk7QUFDL0csU0FBU0MsaUJBQWlCLEVBQUVDLGVBQWUsUUFBUSxlQUFlO0FBQ2xFLFNBQVNDLHFCQUFxQixRQUFRLG9CQUFvQjtB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