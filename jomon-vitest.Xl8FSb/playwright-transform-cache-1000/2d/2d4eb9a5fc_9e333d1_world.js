// b819428af270a3b59a9be0b0a0a265a4f230f27d
import { ITEMS, MONSTERS, biomeForFloor, monsterById, monsterRoleFor, terrainAffinityFor } from './content';
import { rngFor, streamSeed } from './rng';
import { FLOOR_COUNT, MAP_HEIGHT, MAP_WIDTH, floorIndex, floorPoint, inFloorBounds } from './types';
import { objectiveForFloor } from './objectives';
import { gateForArea, validateAreaGate } from './area-gates';
import { puzzleTemplatesFor, validateFloorPuzzles, validatePuzzleTemplates } from './puzzles';
import { isBlockingProp, PROP_IDS, propAt, propDefinition, propDefinitionsFor, validatePropDefinitions } from './props';
import { generateRouteContract, validateRouteContract } from './route-contract';
import { compileRouteContract, macroConnectorPoints, macroRecipeFor, validateMacroRealization } from './macro-recipe';
import { selectPlacement } from './placement-contract';
import { definitionForEncounter, encounterPlansFor, membersForEncounter } from './encounter-director';
import { ecologyEventFor, ecologyProfileFor } from './ecology';
import { escalationFor } from './escalation';
import { socialContractFor } from './social-contract';
import { optionalTerrainToolFor } from './traversal-tool-distribution';
import { placeSecretMetadata } from './secrets';
import { resolveCampaignDifficulty } from './campaign-difficulty';
import { initialCampaignCycle } from './engine/campaign';
const tile = kind => ({
  kind,
  explored: false,
  visible: false
});
const pointKey = point => `${point.x},${point.y}`;
const passable = kind => !['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest', 'deepWater', 'breakwall', 'cliffWall'].includes(kind);
const propDefinitionErrors = validatePropDefinitions();
const macroDebugs = new WeakMap();
const macroPilots = new WeakMap();
const placementDebugs = new WeakMap();
const routeContractDebugs = new WeakMap();
const tacticalEncounterDebugs = new WeakMap();
const indexOf = (floor, x, y) => floorIndex(floor, x, y);
const pointAt = (floor, index) => floorPoint(floor, index);
const inBounds = (floor, x, y) => inFloorBounds(floor, x, y);
export const getTile = (floor, x, y) => inBounds(floor, x, y) ? floor.tiles[indexOf(floor, x, y)] : undefined;
export const actorAt = (floor, x, y) => floor.actors.find(actor => actor.x === x && actor.y === y && actor.health > 0);
export const isPassable = (floor, x, y) => {
  const target = getTile(floor, x, y);
  return Boolean(target && passable(target.kind) && target.kind !== 'lockedDoor' && !actorAt(floor, x, y) && !isBlockingProp(propAt(floor.props, x, y)));
};
const toolDirections = [[0, -1], [1, 0], [0, 1], [-1, 0]];
const toolHazards = new Set(['pit', 'water', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder', 'bramble', 'rubble', 'brine', 'frostRime']);
const drillableToolTerrain = new Set(['wall', 'rubble', 'bramble', 'boulder']);
export const primaryToolUseAvailable = (floor, reachable = reachableFloorIndexes(floor)) => {
  var _floor$rewardOffers, _offer$choices$;
  const offer = (_floor$rewardOffers = floor.rewardOffers) === null || _floor$rewardOffers === void 0 ? void 0 : _floor$rewardOffers.find(candidate => candidate.milestoneId === 'waycache');
  if ((offer === null || offer === void 0 ? void 0 : offer.kind) !== 'waycache') return false;
  const tool = (_offer$choices$ = offer.choices[0]) === null || _offer$choices$ === void 0 ? void 0 : _offer$choices$.id;
  if (!tool) return false;
  for (const index of reachable) {
    const origin = pointAt(floor, index);
    if (!isPassable(floor, origin.x, origin.y)) continue;
    for (const [dx, dy] of toolDirections) {
      const first = {
        x: origin.x + dx,
        y: origin.y + dy
      };
      const second = {
        x: origin.x + dx * 2,
        y: origin.y + dy * 2
      };
      const firstTile = getTile(floor, first.x, first.y);
      if (tool === 'stoneWedge' && firstTile && drillableToolTerrain.has(firstTile.kind)) return true;
      if (tool === 'reedwing' && firstTile && toolHazards.has(firstTile.kind) && isPassable(floor, second.x, second.y)) return true;
      if (tool === 'cordAnchor' && isPassable(floor, second.x, second.y)) return true;
      if (tool === 'ashwayRites' && firstTile && toolHazards.has(firstTile.kind) && firstTile.kind !== 'boulder') return true;
    }
  }
  return false;
};
const pathOffsets = [[-1, -1], [0, -1], [1, -1], [-1, 0], [1, 0], [-1, 1], [0, 1], [1, 1]];
const traversalBlocker = (floor, point, options) => {
  const current = getTile(floor, point.x, point.y);
  if (!current) return 'bounds';
  if (!passable(current.kind) || current.kind === 'lockedDoor') return `terrain:${current.kind}`;
  const prop = propAt(floor.props, point.x, point.y);
  if (!options.ignoreBlockingProps && prop && isBlockingProp(prop)) return `prop:${prop.id}`;
  return undefined;
};
const isPathPassable = (floor, point, ignoreBlockingProps = false) => !traversalBlocker(floor, point, {
  ignoreBlockingProps
});
export const traverseFloor = (floor, start = floor.start, options = {}) => {
  var _options$collectBlock;
  const startBlocker = traversalBlocker(floor, start, options);
  if (startBlocker) return {
    reachable: new Set(),
    blockers: [`start:${startBlocker}`]
  };
  const collectBlockers = (_options$collectBlock = options.collectBlockers) !== null && _options$collectBlock !== void 0 ? _options$collectBlock : true;
  const propsByIndex = options.ignoreBlockingProps ? undefined : new Map();
  if (propsByIndex) for (const prop of floor.props) {
    const index = indexOf(floor, prop.x, prop.y);
    if (prop.state !== 'destroyed' && !propsByIndex.has(index)) propsByIndex.set(index, prop);
  }
  const initial = indexOf(floor, start.x, start.y);
  const seen = new Set([initial]);
  const queue = [initial];
  const previous = options.target ? new Map([[initial, undefined]]) : undefined;
  const blockers = collectBlockers ? new Set() : undefined;
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const current = queue[cursor];
    const point = pointAt(floor, current);
    if (options.target && point.x === options.target.x && point.y === options.target.y) {
      const path = [];
      for (let index = current; index !== undefined; index = previous.get(index)) path.push(pointAt(floor, index));
      return {
        reachable: seen,
        path: path.reverse(),
        blockers: blockers ? [...blockers].sort() : []
      };
    }
    for (const [x, y] of pathOffsets) {
      const nextX = point.x + x;
      const nextY = point.y + y;
      if (!inBounds(floor, nextX, nextY)) continue;
      const nextIndex = indexOf(floor, nextX, nextY);
      const nextTile = floor.tiles[nextIndex];
      const blocker = !passable(nextTile.kind) || nextTile.kind === 'lockedDoor' ? `terrain:${nextTile.kind}` : !options.ignoreBlockingProps && isBlockingProp(propsByIndex === null || propsByIndex === void 0 ? void 0 : propsByIndex.get(nextIndex)) ? `prop:${propsByIndex.get(nextIndex).id}` : undefined;
      if (blocker) {
        blockers === null || blockers === void 0 || blockers.add(`${nextX},${nextY}:${blocker}`);
        continue;
      }
      if (!seen.has(nextIndex)) {
        seen.add(nextIndex);
        previous === null || previous === void 0 || previous.set(nextIndex, current);
        queue.push(nextIndex);
      }
    }
  }
  return {
    reachable: seen,
    blockers: blockers ? [...blockers].sort() : []
  };
};
export const reachableFloorIndexes = (floor, start = floor.start, ignoreBlockingProps = false) => traverseFloor(floor, start, {
  ignoreBlockingProps,
  collectBlockers: false
}).reachable;
export const hasPassableTerrainPath = (floor, start, destination) => Boolean(traverseFloor(floor, start, {
  target: destination,
  ignoreBlockingProps: true,
  collectBlockers: false
}).path);
export const hasPassablePath = (floor, start, destination) => Boolean(traverseFloor(floor, start, {
  target: destination,
  collectBlockers: false
}).path);
export const macroRecipeDebug = floor => macroDebugs.get(floor);
export const placementDebug = floor => {
  var _placementDebugs$get;
  return (_placementDebugs$get = placementDebugs.get(floor)) !== null && _placementDebugs$get !== void 0 ? _placementDebugs$get : [];
};
export const routeContractDebug = floor => routeContractDebugs.get(floor);
export const escalationDebug = floor => floor.escalation;
export const tacticalEncounterDebug = floor => {
  var _tacticalEncounterDeb;
  return (_tacticalEncounterDeb = tacticalEncounterDebugs.get(floor)) !== null && _tacticalEncounterDeb !== void 0 ? _tacticalEncounterDeb : [];
};
export const validateMacroRecipe = floor => {
  const debug = macroDebugs.get(floor);
  return !debug || !macroPilots.get(floor) ? [] : validateMacroRealization(debug, point => Boolean(getTile(floor, point.x, point.y) && isPathPassable(floor, point, false)));
};
export const preservesExitPath = (floor, start, point, kind) => {
  const target = getTile(floor, point.x, point.y);
  if (!target) return false;
  const previous = target.kind;
  target.kind = kind;
  const preserves = hasPassableTerrainPath(floor, start, floor.exit);
  target.kind = previous;
  return preserves;
};
export const preservesAdjacentExitAccess = (floor, point, kind) => {
  const target = getTile(floor, point.x, point.y);
  if (!target) return false;
  const previous = target.kind;
  target.kind = kind;
  const adjacent = [[0, -1], [1, 0], [0, 1], [-1, 0]].map(([x, y]) => ({
    x: point.x + x,
    y: point.y + y
  })).filter(candidate => {
    const tile = getTile(floor, candidate.x, candidate.y);
    return Boolean(tile && passable(tile.kind) && tile.kind !== 'lockedDoor');
  });
  const preserves = adjacent.length > 0 && adjacent.every(candidate => hasPassableTerrainPath(floor, candidate, floor.exit));
  target.kind = previous;
  return preserves;
};
export const difficultyFor = (routePosition, areaFloor) => {
  const threat = Math.max(0, Math.min(15, routePosition * 4 + areaFloor));
  return {
    routePosition,
    threat,
    healthMultiplier: 1 + threat * 0.06,
    attackBonus: Math.floor(threat / 2),
    defenseBonus: Math.floor(threat / 5),
    eliteChance: Math.min(35, 4 + threat * 2),
    guardianPattern: threat >= 12 ? 3 : threat >= 8 ? 2 : threat >= 4 ? 1 : 0
  };
};
const assertGenerationPhase = (floor, campaignSeed, contract, phase) => {
  const targets = [floor.exit, ...objectiveTargets(floor)];
  const reachableIndexes = reachableFloorIndexes(floor);
  const reachable = point => inBounds(floor, point.x, point.y) && reachableIndexes.has(indexOf(floor, point.x, point.y));
  for (const target of targets) {
    var _node$id;
    const targetReachable = target.x === floor.exit.x && target.y === floor.exit.y ? reachable(target) : [[0, 0], [0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => reachable({
      x: target.x + x,
      y: target.y + y
    }));
    if (targetReachable) continue;
    const node = contract.nodes.find(candidate => candidate.kind === (target.x === floor.exit.x && target.y === floor.exit.y ? floor.index % 4 === 3 ? 'boss' : 'exit' : 'objective'));
    const trace = traverseFloor(floor);
    throw new Error(`generation failure seed=${campaignSeed} biome=${floor.biome} recipe=${floor.layoutId} phase=${phase} contractNode=${(_node$id = node === null || node === void 0 ? void 0 : node.id) !== null && _node$id !== void 0 ? _node$id : 'unknown'} target=${target.x},${target.y} reached=${trace.reachable.size} blockers=${trace.blockers.join('|')}`);
  }
};
export function generateFloor(runSeed, index, difficulty = difficultyFor(Math.floor(index / 4), index % 4)) {
  const seed = streamSeed(runSeed, 'generation', index);
  const biome = biomeForFloor(index);
  const areaFloor = index % 4;
  const escalation = escalationFor(runSeed, biome, areaFloor);
  const layoutRng = rngFor(runSeed, 'generation', index, 'layout');
  const layoutId = layoutFor(runSeed, biome, areaFloor);
  const routeContract = generateRouteContract({
    campaignSeed: runSeed,
    floorIndex: index,
    biome,
    areaFloor,
    recipeId: layoutId,
    escalationVariant: `${escalation.arcId}:${escalation.phase}`
  });
  const routeValidation = validateRouteContract(routeContract);
  if (!routeValidation.valid) throw new Error(`invalid route contract ${routeContract.id}: ${routeValidation.errors.join('; ')}`);
  const {
    width,
    height
  } = dimensionsFor(biome);
  const macro = compileRouteContract(routeContract, {
    width,
    height
  });
  if (!macro.valid) throw new Error(`invalid macro recipe ${macro.recipeId}: ${macro.diagnostics.join('; ')}`);
  const placements = {
    macro,
    pilot: macroRecipeFor(routeContract).pilot,
    diagnostics: []
  };
  const objective = objectiveForFloor(index);
  const floor = {
    index,
    biome,
    seed,
    width,
    height,
    layoutId,
    tiles: Array.from({
      length: width * height
    }, () => tile('wall')),
    actors: [],
    items: [],
    props: [],
    encounters: [],
    start: {
      x: 2,
      y: 2
    },
    exit: {
      x: width - 3,
      y: height - 3
    },
    guardianDefeated: areaFloor !== 3,
    objective: {
      ...objective,
      label: `${objective.label} — ${areaFloor === 3 ? escalation.payoff : escalation.promise}`
    },
    milestones: [],
    rewardOffers: routeContract.rewardOffers,
    escalation,
    telegraphs: [],
    difficulty
  };
  const rooms = carveRouteContractLayout(floor, routeContract, macro, layoutRng);
  const reservedMacroCells = macroRecipeFor(routeContract).pilot ? new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y))) : new Set();
  floor.start = center(rooms[0]);
  floor.exit = center(rooms[rooms.length - 1]);
  setKind(floor, floor.exit.x, floor.exit.y, 'exit');
  decorateBiome(floor, rngFor(runSeed, 'generation', index, 'terrain', escalation.arcId), rooms);
  placePuzzleTemplate(floor, rngFor(runSeed, 'generation', index, 'puzzle'), rooms);
  imprintEscalationLandmark(floor, rooms);
  restoreMacroConnectors(floor, macro, reservedMacroCells);
  restoreMacroNodeTransit(floor, macro);
  imprintMineRailServiceRoute(floor, macro);
  imprintRuinsRitualCenter(floor, macro);
  placeEvents(floor, rooms, placements);
  placeDoorsAndLocks(floor, rngFor(runSeed, 'gates', index), rooms);
  openMandatoryLocks(floor);
  placeContainers(floor, rngFor(runSeed, 'loot', index, 'containers'), rooms, reservedMacroCells);
  restoreMacroConnectors(floor, macro, reservedMacroCells);
  placements.reachable = undefined;
  imprintCavernTideRoute(floor, macro);
  imprintWildsPressureRoute(floor, macro);
  imprintRuinsWardRoute(floor, macro);
  imprintFurnaceFiringRoute(floor, macro);
  imprintFloodedCurrentNetwork(floor, macro);
  imprintCliffHeightGraph(floor, macro);
  imprintBurialRitualRoutes(floor, macro);
  imprintSaltRouteContract(floor, macro);
  imprintFrostRouteContract(floor, macro);
  imprintCavernSetpieceContext(floor, macro);
  repairMandatoryPath(floor);
  imprintMineBreachRooms(floor, rngFor(runSeed, 'generation', index, 'mine-breach-rooms'));
  imprintWildsCaves(floor, rngFor(runSeed, 'generation', index, 'wilds-caves'));
  imprintCavernHiddenChambers(floor, rngFor(runSeed, 'generation', index, 'cavern-hidden-chambers'));
  imprintRuinsHiddenChambers(floor, macro, rngFor(runSeed, 'generation', index, 'ritual-hidden-chambers'));
  imprintFurnaceServiceSpaces(floor, macro, rngFor(runSeed, 'generation', index, 'furnace-service-spaces'));
  imprintFloodedWhirlpools(floor, macro, rngFor(runSeed, 'generation', index, 'flooded-whirlpools'));
  imprintCliffAlcoves(floor, macro, rngFor(runSeed, 'generation', index, 'cliff-alcoves'));
  imprintBurialCrypts(floor, macro, rngFor(runSeed, 'generation', index, 'burial-crypts'));
  imprintSaltMirages(floor, macro, rngFor(runSeed, 'generation', index, 'salt-mirages'));
  imprintFrostCaves(floor, macro, rngFor(runSeed, 'generation', index, 'frost-caves'));
  placeSecretMetadata(floor);
  assertGenerationPhase(floor, runSeed, routeContract, 'geometry');
  placeActors(floor, rngFor(runSeed, 'generation', index, 'actors'), placements);
  assertGenerationPhase(floor, runSeed, routeContract, 'actors');
  placeEcology(floor, placements);
  assertGenerationPhase(floor, runSeed, routeContract, 'ecology');
  placeItems(floor, rngFor(runSeed, 'loot', index, 'items'), rooms, placements);
  assertGenerationPhase(floor, runSeed, routeContract, 'loot');
  placeProps(floor, reservedMacroCells, placements);
  assertGenerationPhase(floor, runSeed, routeContract, 'props');
  placeMilestones(floor, placements);
  assertGenerationPhase(floor, runSeed, routeContract, 'milestones');
  placeEncounters(floor, rngFor(runSeed, 'generation', index, 'encounters'), placements);
  assertGenerationPhase(floor, runSeed, routeContract, 'encounters');
  macroDebugs.set(floor, macro);
  macroPilots.set(floor, macroRecipeFor(routeContract).pilot);
  placementDebugs.set(floor, placements.diagnostics);
  routeContractDebugs.set(floor, routeContract);
  const macroErrors = validateMacroRecipe(floor);
  if (macroErrors.length) throw new Error(`invalid macro recipe ${macro.recipeId}: ${macroErrors.join('; ')}`);
  const validation = validateGeneration(floor);
  if (!validation.valid) throw new Error(`invalid generated floor ${index}/${layoutId}: ${validation.errors.join('; ')}`);
  return floor;
}
export const areaFloorIndex = (biome, areaFloor) => ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'].indexOf(biome) * 4 + areaFloor;
export const generateAreaFloor = (runSeed, biome, areaFloor, routePosition = 0, cycle = initialCampaignCycle()) => {
  if (!Number.isInteger(areaFloor) || areaFloor < 0 || areaFloor > 3) throw new Error(`invalid area floor: ${areaFloor}`);
  return generateFloor(runSeed, areaFloorIndex(biome, areaFloor), resolveCampaignDifficulty(difficultyFor(routePosition, areaFloor), cycle));
};
const center = room => ({
  x: room.x + Math.floor(room.w / 2),
  y: room.y + Math.floor(room.h / 2)
});
const cardinalOffsets = [[0, -1], [1, 0], [0, 1], [-1, 0]];
const mineHazards = new Set(['spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'gas', 'lava', 'pit']);
const dimensions = {
  mine: {
    width: MAP_WIDTH,
    height: MAP_HEIGHT
  },
  wilds: {
    width: 72,
    height: 48
  },
  caverns: {
    width: 56,
    height: 44
  },
  ruins: {
    width: 64,
    height: 48
  },
  furnace: {
    width: 56,
    height: 40
  },
  floodedRuins: {
    width: 72,
    height: 48
  },
  cliffs: {
    width: 56,
    height: 52
  },
  burial: {
    width: 80,
    height: 56
  },
  saltFlats: {
    width: 72,
    height: 44
  },
  frostReliquary: {
    width: 64,
    height: 48
  }
};
const layoutVariants = {
  mine: ['rail-spine', 'branching-drifts', 'collapse-loop'],
  wilds: ['river-clearings', 'root-maze', 'wetland-causeways'],
  caverns: ['tide-chambers', 'sinkhole-galleries', 'fault-tunnels'],
  ruins: ['circular-precinct', 'broken-processional-loop', 'courtyard-lattice'],
  furnace: ['stepped-kiln-chain', 'smoke-choked-service-route', 'lift-and-ash-loop'],
  floodedRuins: ['braided-current-delta', 'anchor-gated-ruin', 'island-hop-network'],
  cliffs: ['switchback-face', 'ravine-bridge-loop', 'anchor-chain'],
  burial: ['stone-circle-center', 'mound-procession', 'cemetery-settlement-edge', 'ossuary-hollow', 'ancestor-path-loop'],
  saltFlats: ['crust-island-chain', 'brine-maze', 'caravan-causeway', 'mirror-basin-loop', 'salt-ridge-refuge'],
  frostReliquary: ['frozen-lake-crossing', 'ridge-hollow-loop', 'pressure-crack-maze', 'shore-reliquary-route', 'storm-refuge-chain']
};
const dimensionsFor = biome => dimensions[biome];
export const layoutFor = (runSeed, biome, areaFloor) => {
  const deck = rngFor(runSeed, 'generation', areaFloorIndex(biome, 0), 'layout-deck').shuffle([...layoutVariants[biome]]);
  return areaFloor < deck.length ? deck[areaFloor] : `${deck[(areaFloor + runSeed) % deck.length]}-remix`;
};
const escalationLandmarkTerrain = {
  mine: ['support', 'rail'],
  wilds: ['web', 'water'],
  caverns: ['darkness', 'water'],
  ruins: ['altar', 'dart'],
  furnace: ['lift', 'smoke'],
  floodedRuins: ['anchor', 'water'],
  cliffs: ['rope', 'ledge'],
  burial: ['cairn', 'graveSoil'],
  saltFlats: ['saltMirror', 'brine'],
  frostReliquary: ['ice', 'frostRime']
};
const imprintEscalationLandmark = (floor, rooms) => {
  var _rooms$, _floor$escalation$enc, _floor$escalation;
  const room = (_rooms$ = rooms[1]) !== null && _rooms$ !== void 0 ? _rooms$ : rooms[0];
  if (!room) return;
  const target = center(room);
  const variant = Math.min(escalationLandmarkTerrain[floor.biome].length - 1, Math.floor(((_floor$escalation$enc = (_floor$escalation = floor.escalation) === null || _floor$escalation === void 0 ? void 0 : _floor$escalation.encounterOffset) !== null && _floor$escalation$enc !== void 0 ? _floor$escalation$enc : 0) / 2));
  setKind(floor, target.x, target.y, escalationLandmarkTerrain[floor.biome][variant]);
};
const hasNearbyTile = (floor, point, radius, kinds) => {
  for (let y = point.y - radius; y <= point.y + radius; y++) for (let x = point.x - radius; x <= point.x + radius; x++) {
    var _getTile$kind, _getTile;
    if (kinds.has((_getTile$kind = (_getTile = getTile(floor, x, y)) === null || _getTile === void 0 ? void 0 : _getTile.kind) !== null && _getTile$kind !== void 0 ? _getTile$kind : 'wall')) return true;
  }
  return false;
};
const hasAdjacentTile = (floor, point, kinds) => {
  for (let y = point.y - 1; y <= point.y + 1; y++) for (let x = point.x - 1; x <= point.x + 1; x++) {
    var _getTile$kind2, _getTile2;
    if ((x !== point.x || y !== point.y) && kinds.has((_getTile$kind2 = (_getTile2 = getTile(floor, x, y)) === null || _getTile2 === void 0 ? void 0 : _getTile2.kind) !== null && _getTile$kind2 !== void 0 ? _getTile$kind2 : 'wall')) return true;
  }
  return false;
};
const hasMinePropContext = (floor, kind, point) => {
  if (!kind.startsWith('mine.')) return true;
  const workedPassage = new Set(['rail', 'support']);
  if (kind === 'mine.oreVein') return hasNearbyTile(floor, point, 1, new Set(['support', 'rubble', 'boulder']));
  if (kind === 'mine.lanternPost' || kind === 'mine.discardedParcel') return hasNearbyTile(floor, point, 1, workedPassage);
  if (kind === 'mine.brokenCart') return cardinalOffsets.some(([x, y]) => {
    var _getTile3;
    return ((_getTile3 = getTile(floor, point.x + x, point.y + y)) === null || _getTile3 === void 0 ? void 0 : _getTile3.kind) === 'rail';
  });
  if (kind === 'mine.warningMarker') return hasNearbyTile(floor, point, 5, mineHazards);
  if (kind === 'mine.skullMarker') return hasNearbyTile(floor, point, 5, mineHazards) || floor.actors.some(actor => actor.hostile && Math.max(Math.abs(actor.x - point.x), Math.abs(actor.y - point.y)) <= 5);
  return true;
};
const hasCavernPropContext = (floor, kind, point) => {
  if (!kind.startsWith('caverns.')) return true;
  const nearWater = hasAdjacentTile(floor, point, new Set(['water']));
  const nearDarkness = hasAdjacentTile(floor, point, new Set(['darkness']));
  if (kind === 'caverns.crystalCluster' || kind === 'caverns.glowingFungus') return nearDarkness;
  if (kind === 'caverns.barnacledShrine' || kind === 'caverns.brokenBoat' || kind === 'caverns.eelTunnel') return nearWater;
  if (kind === 'caverns.sealedParcel') return nearWater || nearDarkness;
  return true;
};
const hasPropContext = (floor, kind, point) => hasMinePropContext(floor, kind, point) && hasCavernPropContext(floor, kind, point);
const carveRect = (floor, room, kind = 'floor') => {
  for (let y = room.y; y < room.y + room.h; y++) for (let x = room.x; x < room.x + room.w; x++) setKind(floor, x, y, kind);
};
const wallBand = (floor, vertical, at, gap, span = 2) => {
  const limit = vertical ? floor.height - 1 : floor.width - 1;
  for (let offset = 1; offset < limit; offset++) {
    if (Math.abs(offset - gap) <= span) continue;
    setKind(floor, vertical ? at : offset, vertical ? offset : at, 'wall');
  }
};
const landmarkRooms = floor => {
  const w = Math.max(7, Math.floor(floor.width / 8));
  const h = Math.max(6, Math.floor(floor.height / 7));
  const rooms = [{
    x: 3,
    y: Math.max(3, Math.floor(floor.height * .18)),
    w,
    h
  }, {
    x: Math.floor(floor.width * .27),
    y: Math.floor(floor.height * .58),
    w,
    h
  }, {
    x: Math.floor(floor.width * .52),
    y: Math.floor(floor.height * .23),
    w,
    h
  }, {
    x: floor.width - w - 4,
    y: Math.floor(floor.height * .62),
    w,
    h
  }];
  rooms.forEach(room => carveRect(floor, room));
  return rooms;
};
const carveMineRouteContract = (floor, rng) => {
  const variant = floor.layoutId.replace('-remix', '');
  const mainY = variant === 'collapse-loop' ? 13 : rng.int(12, 15);
  const upperY = variant === 'branching-drifts' ? 3 : 5;
  const lowerY = variant === 'rail-spine' ? floor.height - 10 : floor.height - 9;
  const rooms = [{
    x: 2,
    y: mainY,
    w: 6,
    h: 6
  }, {
    x: 10,
    y: mainY,
    w: 7,
    h: 6
  }, {
    x: 20,
    y: mainY,
    w: 6,
    h: 6
  }, {
    x: 28,
    y: upperY,
    w: 7,
    h: 6
  }, {
    x: 28,
    y: lowerY,
    w: 7,
    h: 6
  }, {
    x: 37,
    y: lowerY,
    w: 5,
    h: 5
  }, {
    x: 36,
    y: mainY,
    w: 6,
    h: 6
  }, {
    x: 43,
    y: mainY + 1,
    w: 3,
    h: 4
  }];
  rooms.forEach(room => carveRect(floor, room));
  const [start, landmark, fork, safeRoute, riskRoute, reward, objective, exit] = rooms.map(center);
  const connect = (from, to, verticalFirst = false) => {
    if (verticalFirst) {
      carveV(floor, from.y, to.y, from.x);
      carveH(floor, from.x, to.x, to.y);
    } else {
      carveH(floor, from.x, to.x, from.y);
      carveV(floor, from.y, to.y, to.x);
    }
  };
  connect(start, landmark);
  connect(landmark, fork);
  connect(fork, safeRoute, variant === 'branching-drifts');
  connect(fork, riskRoute, true);
  connect(safeRoute, objective, true);
  connect(riskRoute, objective);
  connect(riskRoute, reward, variant === 'collapse-loop');
  connect(reward, objective, true);
  connect(objective, exit);
  return rooms;
};
const carveBiomeLayout = (floor, rng) => {
  if (floor.biome === 'mine') {
    return carveMineRouteContract(floor, rng);
  }
  carveRect(floor, {
    x: 1,
    y: 1,
    w: floor.width - 2,
    h: floor.height - 2
  });
  const rooms = landmarkRooms(floor);
  const variant = floor.layoutId.replace('-remix', '');
  if (floor.biome === 'wilds') {
    if (variant === 'root-maze') for (let x = 11; x < floor.width - 8; x += 10) wallBand(floor, true, x, rng.int(4, floor.height - 5), 2);
    if (variant === 'wetland-causeways') for (let y = 9; y < floor.height - 7; y += 9) wallBand(floor, false, y, rng.int(5, floor.width - 6), 3);
  } else if (floor.biome === 'caverns') {
    for (let x = 10; x < floor.width - 8; x += 11) wallBand(floor, true, x, rng.int(5, floor.height - 6), variant === 'fault-tunnels' ? 1 : 3);
    if (variant === 'sinkhole-galleries') for (let y = 8; y < floor.height - 6; y += 10) wallBand(floor, false, y, rng.int(5, floor.width - 6), 2);
  } else if (floor.biome === 'ruins') {
    for (let x = 9; x < floor.width - 6; x += 9) wallBand(floor, true, x, rng.int(4, floor.height - 5), 2);
    if (variant !== 'breached-precinct') for (let y = 8; y < floor.height - 6; y += 10) wallBand(floor, false, y, rng.int(5, floor.width - 6), 2);
  } else if (floor.biome === 'furnace') {
    for (let y = 7; y < floor.height - 5; y += 7) wallBand(floor, false, y, rng.int(5, floor.width - 6), variant === 'kiln-terraces' ? 2 : 4);
  } else if (floor.biome === 'floodedRuins') {
    for (let x = 12; x < floor.width - 8; x += 14) wallBand(floor, true, x, rng.int(5, floor.height - 6), 4);
  } else if (floor.biome === 'cliffs') {
    for (let y = 8; y < floor.height - 6; y += 9) wallBand(floor, false, y, rng.int(5, floor.width - 6), variant === 'ravine-switchbacks' ? 1 : 3);
  } else if (floor.biome === 'burial') {
    if (variant === 'ringed-necropolis') for (let x = 14; x < floor.width - 10; x += 18) wallBand(floor, true, x, rng.int(6, floor.height - 7), 5);
  } else if (floor.biome === 'saltFlats') {
    if (variant === 'brine-fractures') for (let x = 12; x < floor.width - 8; x += 12) wallBand(floor, true, x, rng.int(5, floor.height - 6), 4);
  } else if (floor.biome === 'frostReliquary') {
    for (let y = 9; y < floor.height - 7; y += 10) wallBand(floor, false, y, rng.int(5, floor.width - 6), variant === 'reliquary-escarpment' ? 2 : 5);
  }
  rooms.forEach(room => carveRect(floor, room));
  connectRooms(floor, rooms);
  imprintBiomeLandmarks(floor, rng, rooms);
  return rooms;
};
const carveLegacyLayoutFromRouteContract = (floor, contract, rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  return carveBiomeLayout(floor, rng);
};
const carveRouteContractLayout = (floor, contract, macro, rng) => {
  if (!macroRecipeFor(contract).pilot) return carveLegacyLayoutFromRouteContract(floor, contract, rng);
  if (floor.biome === 'wilds') return carveWildsRouteContractLayout(floor, contract, macro, rng);
  if (floor.biome === 'caverns') return carveCavernsRouteContractLayout(floor, contract, macro, rng);
  if (floor.biome === 'ruins') return carveRuinsRouteContractLayout(floor, contract, macro, rng);
  if (floor.biome === 'furnace') return carveFurnaceRouteContractLayout(floor, contract, macro, rng);
  if (floor.biome === 'floodedRuins') return carveFloodedRouteContractLayout(floor, contract, macro, rng);
  if (floor.biome === 'cliffs') return carveCliffsRouteContractLayout(floor, contract, macro, rng);
  if (floor.biome === 'burial') return carveBurialRouteContractLayout(floor, contract, macro, rng);
  if (floor.biome === 'saltFlats') return carveSaltFlatsRouteContractLayout(floor, contract, macro, rng);
  if (floor.biome === 'frostReliquary') return carveFrostReliquaryRouteContractLayout(floor, contract, macro, rng);
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  for (const room of rooms) carveRect(floor, room);
  for (const point of macroConnectorPoints(macro)) setKind(floor, point.x, point.y, 'floor');
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const carveWildsRouteContractLayout = (floor, contract, macro, rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  const carveClearing = room => {
    const center = {
      x: room.x + Math.floor(room.w / 2),
      y: room.y + Math.floor(room.h / 2)
    };
    const radiusX = room.w / 2 + 2;
    const radiusY = room.h / 2 + 2;
    for (let y = room.y - 2; y < room.y + room.h + 2; y++) for (let x = room.x - 2; x < room.x + room.w + 2; x++) {
      const distance = ((x - center.x) / radiusX) ** 2 + ((y - center.y) / radiusY) ** 2;
      if (distance < 1 + (rng.chance(22) ? .18 : -.08)) setKind(floor, x, y, 'floor');
    }
    carveRect(floor, room);
  };
  const carveTrail = point => {
    setKind(floor, point.x, point.y, 'floor');
    for (const [x, y] of cardinalOffsets) if (rng.chance(62)) setKind(floor, point.x + x, point.y + y, 'floor');
    if (rng.chance(28)) for (const [x, y] of [[-1, -1], [1, -1], [-1, 1], [1, 1]]) setKind(floor, point.x + x, point.y + y, 'floor');
  };
  rooms.forEach(carveClearing);
  macroConnectorPoints(macro).forEach(carveTrail);
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const carveCavernsRouteContractLayout = (floor, contract, macro, rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  const carveChamber = (room, large = false) => {
    const center = {
      x: room.x + Math.floor(room.w / 2),
      y: room.y + Math.floor(room.h / 2)
    };
    const radiusX = room.w / 2 + (large ? 5 : 2);
    const radiusY = room.h / 2 + (large ? 4 : 2);
    for (let y = Math.floor(center.y - radiusY); y <= Math.ceil(center.y + radiusY); y++) for (let x = Math.floor(center.x - radiusX); x <= Math.ceil(center.x + radiusX); x++) {
      const distance = ((x - center.x) / radiusX) ** 2 + ((y - center.y) / radiusY) ** 2;
      if (distance < 1 + (rng.chance(18) ? .12 : -.06)) setKind(floor, x, y, 'floor');
    }
    carveRect(floor, room);
  };
  rooms.forEach((room, index) => {
    const node = macro.nodes[index];
    carveChamber(room, node.kind === 'optionalReward' || node.kind === 'landmark' && floor.index % 4 > 1);
  });
  for (const point of macroConnectorPoints(macro)) {
    setKind(floor, point.x, point.y, 'floor');
    for (const [x, y] of cardinalOffsets) if (rng.chance(82)) setKind(floor, point.x + x, point.y + y, 'floor');
  }
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const carveRuinsRing = (floor, center, halfWidth, halfHeight, gates) => {
  const gateKeys = new Set(gates.map(pointKey));
  for (let x = center.x - halfWidth; x <= center.x + halfWidth; x++) for (const y of [center.y - halfHeight, center.y + halfHeight]) if (!gateKeys.has(pointKey({
    x,
    y
  }))) setKind(floor, x, y, 'wall');
  for (let y = center.y - halfHeight + 1; y < center.y + halfHeight; y++) for (const x of [center.x - halfWidth, center.x + halfWidth]) if (!gateKeys.has(pointKey({
    x,
    y
  }))) setKind(floor, x, y, 'wall');
};
const carveRitualArc = (floor, center, radiusX, radiusY, gapAngle) => {
  const ring = [];
  for (let y = center.y - radiusY - 1; y <= center.y + radiusY + 1; y++) for (let x = center.x - radiusX - 1; x <= center.x + radiusX + 1; x++) {
    const dx = (x - center.x) / radiusX;
    const dy = (y - center.y) / radiusY;
    const distance = dx * dx + dy * dy;
    const angle = Math.atan2(dy, dx);
    if (distance < .8 || distance > 1.22 || Math.abs(angle - gapAngle) < .22 || Math.abs(Math.abs(angle) - Math.PI) < .16) continue;
    setKind(floor, x, y, 'wall');
    ring.push({
      x,
      y
    });
  }
  return ring;
};
const carveRitualAnnex = (floor, center) => {
  const points = [];
  for (let offset = 0; offset < 7; offset++) for (const point of [{
    x: center.x + 9 + offset,
    y: center.y - 3 + offset
  }, {
    x: center.x + 9 + offset,
    y: center.y + 3 - offset
  }, {
    x: center.x + 15,
    y: center.y - 3 + offset
  }]) {
    setKind(floor, point.x, point.y, 'wall');
    points.push(point);
  }
  return points;
};
const carveRuinsRouteContractLayout = (floor, contract, macro, _rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  carveRect(floor, {
    x: 1,
    y: 1,
    w: floor.width - 2,
    h: floor.height - 2
  });
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const ritualNode = macro.nodes.find(node => node.kind === 'objective');
  if (!ritualNode) throw new Error(`missing Ruins ritual node: ${macro.recipeId}`);
  const ritual = center(toRoom(ritualNode));
  const approachNode = macro.nodes.find(node => node.kind === 'landmark');
  const approach = approachNode ? center(toRoom(approachNode)) : ritual;
  const outerRing = carveRitualArc(floor, ritual, 13, 9, -Math.PI / 2);
  const innerRing = carveRitualArc(floor, ritual, 6, 4, 0);
  const annex = carveRitualAnnex(floor, ritual);
  floor.ritualLayout = {
    approach,
    center: ritual,
    outerRing,
    innerRing,
    annex
  };
  const variant = floor.layoutId.replace('-remix', '');
  if (variant === 'circular-precinct') carveRuinsRing(floor, ritual, 9, 7, [{
    x: ritual.x,
    y: ritual.y - 7
  }, {
    x: ritual.x + 9,
    y: ritual.y
  }, {
    x: ritual.x,
    y: ritual.y + 7
  }, {
    x: ritual.x - 9,
    y: ritual.y
  }]);
  if (variant === 'broken-processional-loop') {
    carveRuinsRing(floor, ritual, 8, 6, [{
      x: ritual.x,
      y: ritual.y - 6
    }, {
      x: ritual.x - 8,
      y: ritual.y
    }]);
    wallBand(floor, true, ritual.x - 14, ritual.y + 8, 4);
  }
  if (variant === 'courtyard-lattice') {
    wallBand(floor, true, ritual.x - 11, ritual.y - 8, 4);
    wallBand(floor, true, ritual.x + 11, ritual.y + 8, 4);
    wallBand(floor, false, ritual.y, ritual.x, 5);
  }
  const rooms = macro.nodes.map(toRoom);
  rooms.forEach(room => carveRect(floor, room));
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'));
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const carveFurnaceTerraces = (floor, variant) => {
  const levels = [7, 14, 21, 28, 34];
  const gates = variant === 'stepped-kiln-chain' ? [10, 43, 10, 43, 10] : variant === 'smoke-choked-service-route' ? [17, 35, 17, 35, 17] : [12, 42, 12, 42, 27];
  levels.forEach((y, index) => wallBand(floor, false, y, gates[index], 2));
  if (variant === 'smoke-choked-service-route') wallBand(floor, true, 28, 5, 3);
  if (variant === 'lift-and-ash-loop') wallBand(floor, true, 28, 33, 4);
};
const carveFurnaceRouteContractLayout = (floor, contract, macro, _rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  carveRect(floor, {
    x: 1,
    y: 1,
    w: floor.width - 2,
    h: floor.height - 2
  });
  carveFurnaceTerraces(floor, floor.layoutId.replace('-remix', ''));
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  rooms.forEach(room => carveRect(floor, room));
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'));
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const carveFloodedChannels = (floor, variant) => {
  if (variant === 'braided-current-delta') {
    wallBand(floor, true, 20, 10, 3);
    wallBand(floor, true, 45, 35, 3);
  } else if (variant === 'anchor-gated-ruin') {
    wallBand(floor, false, 15, 18, 3);
    wallBand(floor, false, 31, 53, 3);
  } else {
    wallBand(floor, true, 24, 11, 4);
    wallBand(floor, false, 24, 49, 4);
  }
};
const carveFloodedRouteContractLayout = (floor, contract, macro, _rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  carveRect(floor, {
    x: 1,
    y: 1,
    w: floor.width - 2,
    h: floor.height - 2
  });
  carveFloodedChannels(floor, floor.layoutId.replace('-remix', ''));
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  rooms.forEach(room => carveRect(floor, room));
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'));
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const cliffBand = (floor, vertical, at, gap, span = 2) => {
  const limit = vertical ? floor.height - 1 : floor.width - 1;
  for (let offset = 1; offset < limit; offset++) if (Math.abs(offset - gap) > span) for (let depth = -1; depth <= 1; depth++) setKind(floor, vertical ? at + depth + (offset % 5 === 0 ? 1 : 0) : offset, vertical ? offset : at + depth + (offset % 5 === 0 ? 1 : 0), 'cliffWall');
};
const cliffRidge = (floor, points, width = 1) => {
  for (let index = 1; index < points.length; index++) {
    const from = points[index - 1];
    const to = points[index];
    const steps = Math.max(Math.abs(to.x - from.x), Math.abs(to.y - from.y));
    for (let step = 0; step <= steps; step++) {
      const x = Math.round(from.x + (to.x - from.x) * step / steps);
      const y = Math.round(from.y + (to.y - from.y) * step / steps);
      for (let lateral = -width; lateral <= width; lateral++) setKind(floor, x + lateral, y, 'cliffWall');
    }
  }
};
const carveCliffContours = (floor, variant) => {
  const areaFloor = floor.index % 4;
  if (variant === 'switchback-face') {
    const terraces = [[9, 12], [18, 43], [28, 12], [38, 43], [47, 26]].slice(0, 3 + areaFloor);
    terraces.forEach(([y, gap]) => cliffBand(floor, false, y, gap));
  } else if (variant === 'ravine-bridge-loop') {
    cliffRidge(floor, [{
      x: 13,
      y: 2
    }, {
      x: 18,
      y: 13
    }, {
      x: 15,
      y: 25
    }, {
      x: 21,
      y: 38
    }, {
      x: 17,
      y: floor.height - 3
    }], 2);
    cliffRidge(floor, [{
      x: 42,
      y: 2
    }, {
      x: 36,
      y: 14
    }, {
      x: 40,
      y: 27
    }, {
      x: 34,
      y: 39
    }, {
      x: 39,
      y: floor.height - 3
    }], 2);
    for (const [y, gap] of [[15, 28], [29, 25], [41, 31]].slice(0, areaFloor)) cliffBand(floor, false, y, gap, 3);
  } else {
    const spines = [[{
      x: 7,
      y: 7
    }, {
      x: 20,
      y: 11
    }, {
      x: 16,
      y: 22
    }, {
      x: 29,
      y: 27
    }], [{
      x: 47,
      y: 9
    }, {
      x: 34,
      y: 16
    }, {
      x: 40,
      y: 29
    }, {
      x: 28,
      y: 38
    }], [{
      x: 8,
      y: 38
    }, {
      x: 19,
      y: 34
    }, {
      x: 26,
      y: 44
    }, {
      x: 39,
      y: 47
    }], [{
      x: 27,
      y: 3
    }, {
      x: 31,
      y: 15
    }, {
      x: 25,
      y: 27
    }, {
      x: 31,
      y: 42
    }]];
    spines.slice(0, 2 + areaFloor).forEach(points => cliffRidge(floor, points, 2));
  }
};
const carveCliffsRouteContractLayout = (floor, contract, macro, _rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  carveRect(floor, {
    x: 1,
    y: 1,
    w: floor.width - 2,
    h: floor.height - 2
  });
  carveCliffContours(floor, floor.layoutId.replace('-remix', ''));
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  rooms.forEach(room => carveRect(floor, room));
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'));
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const paintBurialMound = (floor, center, radiusX, radiusY) => {
  for (let y = center.y - radiusY; y <= center.y + radiusY; y++) for (let x = center.x - radiusX; x <= center.x + radiusX; x++) {
    const dx = (x - center.x) / radiusX;
    const dy = (y - center.y) / radiusY;
    const distance = dx * dx + dy * dy + ((x * 11 + y * 7) % 5 - 2) * .07;
    if (distance >= .73 && distance <= 1.13) setKind(floor, x, y, 'cairn');else if (distance < .46 && (x * 3 + y) % 4 === 0) setKind(floor, x, y, 'graveSoil');
  }
};
const paintBurialProcession = (floor, points) => {
  for (let index = 1; index < points.length; index++) {
    const from = points[index - 1];
    const to = points[index];
    const steps = Math.max(Math.abs(to.x - from.x), Math.abs(to.y - from.y));
    for (let step = 0; step <= steps; step++) setKind(floor, Math.round(from.x + (to.x - from.x) * step / steps), Math.round(from.y + (to.y - from.y) * step / steps), 'spiritPath');
  }
};
const carveBurialLandscape = (floor, variant) => {
  const center = {
    x: Math.floor(floor.width / 2),
    y: Math.floor(floor.height / 2)
  };
  if (variant === 'stone-circle-center') {
    ;
    [{
      x: center.x - 18,
      y: center.y - 8
    }, {
      x: center.x + 3,
      y: center.y - 10
    }, {
      x: center.x + 19,
      y: center.y + 4
    }, {
      x: center.x - 6,
      y: center.y + 10
    }].forEach((point, index) => paintBurialMound(floor, point, 7 + index % 2, 4 + index % 3));
    paintBurialProcession(floor, [{
      x: 4,
      y: center.y + 6
    }, {
      x: center.x - 18,
      y: center.y + 3
    }, {
      x: center.x - 3,
      y: center.y - 3
    }, {
      x: center.x + 18,
      y: center.y + 5
    }, {
      x: floor.width - 5,
      y: center.y - 2
    }]);
  } else if (variant === 'mound-procession') {
    ;
    [{
      x: 18,
      y: 9
    }, {
      x: 47,
      y: 14
    }, {
      x: 26,
      y: 25
    }, {
      x: 59,
      y: 32
    }, {
      x: 20,
      y: 43
    }, {
      x: 49,
      y: 48
    }].forEach((point, index) => paintBurialMound(floor, point, 5 + index % 3, 3 + index % 2));
    paintBurialProcession(floor, [{
      x: center.x - 4,
      y: 3
    }, {
      x: center.x + 7,
      y: 14
    }, {
      x: center.x - 9,
      y: 25
    }, {
      x: center.x + 8,
      y: 38
    }, {
      x: center.x - 5,
      y: floor.height - 4
    }]);
  } else if (variant === 'cemetery-settlement-edge') {
    for (let cluster = 0; cluster < 7; cluster++) {
      const origin = {
        x: 9 + cluster * 17 % (floor.width - 18),
        y: 8 + cluster * 11 % (floor.height - 16)
      };
      paintBurialMound(floor, origin, 4 + cluster % 3, 3 + (cluster + 1) % 2);
    }
    paintBurialProcession(floor, [{
      x: 3,
      y: center.y - 7
    }, {
      x: 19,
      y: center.y - 2
    }, {
      x: 37,
      y: center.y + 4
    }, {
      x: 58,
      y: center.y - 3
    }, {
      x: floor.width - 4,
      y: center.y + 5
    }]);
  } else if (variant === 'ossuary-hollow') {
    ;
    [{
      x: center.x - 18,
      y: center.y - 8
    }, {
      x: center.x + 17,
      y: center.y - 6
    }, {
      x: center.x - 11,
      y: center.y + 11
    }, {
      x: center.x + 13,
      y: center.y + 10
    }].forEach((point, index) => paintBurialMound(floor, point, 7, 4 + index % 2));
    for (let y = center.y - 7; y <= center.y + 7; y++) for (let x = center.x - 13; x <= center.x + 13; x++) if (Math.abs(x - center.x) + Math.abs(y - center.y) > 10 && (x * 5 + y) % 4 === 0) setKind(floor, x, y, 'ossuary');
    paintBurialProcession(floor, [{
      x: 4,
      y: center.y + 5
    }, {
      x: center.x - 15,
      y: center.y - 2
    }, {
      x: center.x,
      y: center.y + 3
    }, {
      x: center.x + 17,
      y: center.y - 4
    }, {
      x: floor.width - 5,
      y: center.y + 2
    }]);
  } else {
    ;
    [{
      x: center.x - 19,
      y: center.y - 10
    }, {
      x: center.x + 17,
      y: center.y - 11
    }, {
      x: center.x + 20,
      y: center.y + 9
    }, {
      x: center.x - 15,
      y: center.y + 11
    }, {
      x: center.x,
      y: center.y
    }].forEach((point, index) => paintBurialMound(floor, point, 6 + index % 2, 4 + (index + 1) % 2));
    paintBurialProcession(floor, [{
      x: center.x - 22,
      y: center.y - 10
    }, {
      x: center.x - 4,
      y: center.y - 14
    }, {
      x: center.x + 20,
      y: center.y - 7
    }, {
      x: center.x + 16,
      y: center.y + 11
    }, {
      x: center.x - 15,
      y: center.y + 13
    }, {
      x: center.x - 22,
      y: center.y - 2
    }]);
  }
};
const carveBurialRouteContractLayout = (floor, contract, macro, _rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  carveRect(floor, {
    x: 1,
    y: 1,
    w: floor.width - 2,
    h: floor.height - 2
  });
  carveBurialLandscape(floor, floor.layoutId.replace('-remix', ''));
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  rooms.forEach(room => carveRect(floor, room));
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'));
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const paintSaltLine = (floor, from, to, kind) => {
  if (from.x === to.x) for (let y = Math.min(from.y, to.y); y <= Math.max(from.y, to.y); y++) setKind(floor, from.x, y, kind);else for (let x = Math.min(from.x, to.x); x <= Math.max(from.x, to.x); x++) setKind(floor, x, from.y, kind);
};
const paintSaltPatch = (floor, left, top, width, height, kind) => {
  for (let y = top; y < top + height; y++) for (let x = left; x < left + width; x++) setKind(floor, x, y, kind);
};
const carveSaltLandscape = (floor, variant) => {
  const center = {
    x: Math.floor(floor.width / 2),
    y: Math.floor(floor.height / 2)
  };
  if (variant === 'crust-island-chain') {
    for (let x = 14, island = 0; x < floor.width - 10; x += 15, island++) {
      paintSaltLine(floor, {
        x,
        y: 2
      }, {
        x,
        y: floor.height - 3
      }, 'brine');
      const gap = island % 2 ? 12 : floor.height - 13;
      paintSaltPatch(floor, x - 1, gap - 2, 3, 5, 'floor');
    }
  } else if (variant === 'brine-maze') {
    for (let x = 12; x < floor.width - 8; x += 14) paintSaltLine(floor, {
      x,
      y: 4
    }, {
      x,
      y: floor.height - 5
    }, 'brine');
    for (let y = 10; y < floor.height - 7; y += 12) paintSaltLine(floor, {
      x: 5,
      y
    }, {
      x: floor.width - 6,
      y
    }, 'brine');
    paintSaltPatch(floor, center.x - 5, center.y - 3, 11, 7, 'floor');
  } else if (variant === 'caravan-causeway') {
    paintSaltLine(floor, {
      x: 3,
      y: center.y
    }, {
      x: floor.width - 4,
      y: center.y
    }, 'saltMirror');
    paintSaltLine(floor, {
      x: 3,
      y: center.y - 5
    }, {
      x: floor.width - 4,
      y: center.y - 5
    }, 'brine');
    paintSaltLine(floor, {
      x: 3,
      y: center.y + 5
    }, {
      x: floor.width - 4,
      y: center.y + 5
    }, 'brine');
  } else if (variant === 'mirror-basin-loop') {
    const left = center.x - 19;
    const right = center.x + 19;
    const top = center.y - 12;
    const bottom = center.y + 12;
    paintSaltLine(floor, {
      x: left,
      y: top
    }, {
      x: right,
      y: top
    }, 'saltMirror');
    paintSaltLine(floor, {
      x: right,
      y: top
    }, {
      x: right,
      y: bottom
    }, 'saltMirror');
    paintSaltLine(floor, {
      x: right,
      y: bottom
    }, {
      x: left,
      y: bottom
    }, 'saltMirror');
    paintSaltLine(floor, {
      x: left,
      y: bottom
    }, {
      x: left,
      y: top
    }, 'saltMirror');
    paintSaltPatch(floor, center.x - 10, center.y - 6, 21, 13, 'brine');
  } else {
    for (let y = 8; y < floor.height - 7; y += 10) {
      paintSaltLine(floor, {
        x: 5,
        y
      }, {
        x: floor.width - 6,
        y
      }, 'crumble');
      paintSaltPatch(floor, center.x - 5, y - 1, 11, 3, 'floor');
    }
    paintSaltPatch(floor, center.x - 12, center.y - 6, 25, 13, 'saltMirror');
  }
};
const carveSaltFlatsRouteContractLayout = (floor, contract, macro, _rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  carveRect(floor, {
    x: 1,
    y: 1,
    w: floor.width - 2,
    h: floor.height - 2
  });
  carveSaltLandscape(floor, floor.layoutId.replace('-remix', ''));
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  rooms.forEach(room => carveRect(floor, room));
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'));
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const paintFrostShelf = (floor, center, radiusX, radiusY, kind) => {
  for (let y = center.y - radiusY; y <= center.y + radiusY; y++) for (let x = center.x - radiusX; x <= center.x + radiusX; x++) if ((x - center.x) ** 2 / radiusX ** 2 + (y - center.y) ** 2 / radiusY ** 2 + ((x * 5 + y * 7) % 5 - 2) * .08 < 1) setKind(floor, x, y, kind);
};
const paintFrostCrack = (floor, points) => {
  for (let index = 1; index < points.length; index++) {
    const from = points[index - 1];
    const to = points[index];
    const steps = Math.max(Math.abs(to.x - from.x), Math.abs(to.y - from.y));
    for (let step = 0; step <= steps; step++) setKind(floor, Math.round(from.x + (to.x - from.x) * step / steps), Math.round(from.y + (to.y - from.y) * step / steps), 'frostRime');
  }
};
const carveFrostLandscape = (floor, variant) => {
  const center = {
    x: Math.floor(floor.width / 2),
    y: Math.floor(floor.height / 2)
  };
  if (variant === 'frozen-lake-crossing') {
    paintFrostShelf(floor, {
      x: center.x - 12,
      y: center.y - 2
    }, 19, 8, 'ice');
    paintFrostShelf(floor, {
      x: center.x + 14,
      y: center.y + 3
    }, 18, 9, 'ice');
    paintFrostCrack(floor, [{
      x: 4,
      y: center.y - 8
    }, {
      x: 20,
      y: center.y - 5
    }, {
      x: 37,
      y: center.y - 9
    }, {
      x: floor.width - 5,
      y: center.y - 6
    }]);
    paintFrostCrack(floor, [{
      x: 5,
      y: center.y + 8
    }, {
      x: 24,
      y: center.y + 5
    }, {
      x: 43,
      y: center.y + 9
    }, {
      x: floor.width - 5,
      y: center.y + 6
    }]);
  } else if (variant === 'ridge-hollow-loop') {
    paintFrostCrack(floor, [{
      x: center.x - 20,
      y: center.y - 10
    }, {
      x: center.x - 3,
      y: center.y - 14
    }, {
      x: center.x + 20,
      y: center.y - 8
    }, {
      x: center.x + 16,
      y: center.y + 12
    }, {
      x: center.x - 17,
      y: center.y + 13
    }, {
      x: center.x - 22,
      y: center.y - 2
    }]);
    paintFrostShelf(floor, center, 13, 8, 'ice');
  } else if (variant === 'pressure-crack-maze') {
    for (let x = 10; x < floor.width - 7; x += 13) paintFrostCrack(floor, [{
      x,
      y: 4
    }, {
      x: x + 3,
      y: 16
    }, {
      x: x - 2,
      y: 28
    }, {
      x: x + 2,
      y: floor.height - 5
    }]);
    [{
      x: 14,
      y: 10
    }, {
      x: 33,
      y: 20
    }, {
      x: 49,
      y: 34
    }].forEach(point => paintFrostShelf(floor, point, 10, 5, 'ice'));
  } else if (variant === 'shore-reliquary-route') {
    paintFrostShelf(floor, {
      x: center.x - 5,
      y: 14
    }, 9, 12, 'ice');
    paintFrostShelf(floor, {
      x: center.x + 7,
      y: 34
    }, 10, 12, 'ice');
    paintFrostCrack(floor, [{
      x: center.x - 10,
      y: 3
    }, {
      x: center.x - 4,
      y: 16
    }, {
      x: center.x - 9,
      y: 29
    }, {
      x: center.x - 3,
      y: floor.height - 4
    }]);
    paintFrostCrack(floor, [{
      x: center.x + 10,
      y: 3
    }, {
      x: center.x + 4,
      y: 18
    }, {
      x: center.x + 9,
      y: 33
    }, {
      x: center.x + 3,
      y: floor.height - 4
    }]);
  } else {
    ;
    [{
      x: 12,
      y: center.y - 4
    }, {
      x: 27,
      y: center.y + 5
    }, {
      x: 43,
      y: center.y - 3
    }, {
      x: 55,
      y: center.y + 5
    }].forEach((point, index) => paintFrostShelf(floor, point, 6 + index % 2, 5, 'ice'));
    paintFrostCrack(floor, [{
      x: 8,
      y: 4
    }, {
      x: 17,
      y: 16
    }, {
      x: 13,
      y: 30
    }, {
      x: 22,
      y: floor.height - 5
    }]);
    paintFrostCrack(floor, [{
      x: 42,
      y: 4
    }, {
      x: 50,
      y: 16
    }, {
      x: 45,
      y: 30
    }, {
      x: 54,
      y: floor.height - 5
    }]);
  }
};
const carveFrostReliquaryRouteContractLayout = (floor, contract, macro, _rng) => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`);
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`);
  carveRect(floor, {
    x: 1,
    y: 1,
    w: floor.width - 2,
    h: floor.height - 2
  });
  carveFrostLandscape(floor, floor.layoutId.replace('-remix', ''));
  const toRoom = node => ({
    x: node.footprint.x,
    y: node.footprint.y,
    w: node.footprint.width,
    h: node.footprint.height
  });
  const rooms = macro.nodes.map(toRoom);
  rooms.forEach(room => carveRect(floor, room));
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'));
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]));
  const ordered = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit'];
  return ordered.map(kind => byKind.get(kind)).filter(room => Boolean(room));
};
const restoreMacroConnectors = (floor, macro, reserved) => {
  if (!reserved.size) return;
  for (const point of macroConnectorPoints(macro)) if (reserved.has(indexOf(floor, point.x, point.y))) setKind(floor, point.x, point.y, 'floor');
};
const restoreMacroNodeTransit = (floor, macro) => {
  for (const node of macro.nodes) {
    const center = {
      x: node.footprint.x + Math.floor(node.footprint.width / 2),
      y: node.footprint.y + Math.floor(node.footprint.height / 2)
    };
    const endpoints = macro.edges.flatMap(edge => [edge.from, edge.to]).filter(point => point.x >= node.footprint.x && point.x < node.footprint.x + node.footprint.width && point.y >= node.footprint.y && point.y < node.footprint.y + node.footprint.height);
    const points = floor.biome === 'caverns' ? Array.from({
      length: node.footprint.width * node.footprint.height
    }, (_, index) => ({
      x: node.footprint.x + index % node.footprint.width,
      y: node.footprint.y + Math.floor(index / node.footprint.width)
    })) : endpoints.flatMap(endpoint => {
      const route = [];
      for (let x = Math.min(center.x, endpoint.x); x <= Math.max(center.x, endpoint.x); x++) route.push({
        x,
        y: center.y
      });
      for (let y = Math.min(center.y, endpoint.y); y <= Math.max(center.y, endpoint.y); y++) route.push({
        x: endpoint.x,
        y
      });
      return route;
    });
    for (const point of points) if (point.x !== floor.exit.x || point.y !== floor.exit.y) setKind(floor, point.x, point.y, 'floor');
  }
};
const imprintMineRailServiceRoute = (floor, macro) => {
  if (floor.biome !== 'mine') return;
  const landmark = macro.nodes.find(node => node.kind === 'landmark');
  if (!landmark || landmark.footprint.width < 4) throw new Error(`missing Mine rail service route: ${macro.recipeId}`);
  const y = landmark.footprint.y + Math.floor(landmark.footprint.height / 2);
  for (let x = landmark.footprint.x + 1; x < landmark.footprint.x + landmark.footprint.width - 1; x++) setKind(floor, x, y, 'rail');
};
const mineBreachDirections = [{
  x: 0,
  y: -1,
  cross: {
    x: 1,
    y: 0
  }
}, {
  x: 1,
  y: 0,
  cross: {
    x: 0,
    y: 1
  }
}, {
  x: 0,
  y: 1,
  cross: {
    x: 1,
    y: 0
  }
}, {
  x: -1,
  y: 0,
  cross: {
    x: 0,
    y: 1
  }
}];
const breachChamber = (floor, approach, direction, depth, width) => {
  const entry = {
    x: approach.x + direction.x,
    y: approach.y + direction.y
  };
  const radius = Math.floor(width / 2);
  const chamber = [];
  for (let forward = 2; forward < depth + 2; forward++) for (let lateral = -radius; lateral <= radius; lateral++) {
    var _getTile4;
    const point = {
      x: approach.x + direction.x * forward + direction.cross.x * lateral,
      y: approach.y + direction.y * forward + direction.cross.y * lateral
    };
    if (!getTile(floor, point.x, point.y) || ((_getTile4 = getTile(floor, point.x, point.y)) === null || _getTile4 === void 0 ? void 0 : _getTile4.kind) !== 'wall') return undefined;
    chamber.push(point);
  }
  const chamberIndexes = new Set(chamber.map(point => indexOf(floor, point.x, point.y)));
  for (const point of chamber) for (const [x, y] of pathOffsets) {
    const neighbor = {
      x: point.x + x,
      y: point.y + y
    };
    if (neighbor.x === entry.x && neighbor.y === entry.y) continue;
    const tile = getTile(floor, neighbor.x, neighbor.y);
    if (!tile || !chamberIndexes.has(indexOf(floor, neighbor.x, neighbor.y)) && tile.kind !== 'wall') return undefined;
  }
  return chamber;
};
const imprintMineBreachRooms = (floor, rng) => {
  if (floor.biome !== 'mine') return;
  const areaFloor = floor.index % 4;
  const desired = areaFloor + 1;
  const reachable = [...reachableFloorIndexes(floor)].map(index => pointAt(floor, index));
  const candidates = rng.shuffle(reachable.flatMap(approach => mineBreachDirections.map(direction => ({
    approach,
    direction
  }))));
  const reserved = new Set();
  const sideSpaces = [];
  for (const candidate of candidates) {
    var _getTile5;
    if (sideSpaces.length === desired) break;
    const entry = {
      x: candidate.approach.x + candidate.direction.x,
      y: candidate.approach.y + candidate.direction.y
    };
    if (((_getTile5 = getTile(floor, entry.x, entry.y)) === null || _getTile5 === void 0 ? void 0 : _getTile5.kind) !== 'wall' || reserved.has(indexOf(floor, entry.x, entry.y))) continue;
    const chamber = breachChamber(floor, candidate.approach, candidate.direction, 2 + Math.floor((areaFloor + sideSpaces.length) / 2), 3 + (areaFloor > 1 ? 2 : 0));
    if (!chamber || chamber.some(point => reserved.has(indexOf(floor, point.x, point.y)))) continue;
    const rewardPoint = chamber[Math.floor(chamber.length / 2)];
    const reward = {
      id: sideSpaces.length % 2 ? 'ropeBundle' : 'bombPack',
      x: rewardPoint.x,
      y: rewardPoint.y,
      count: 1,
      visibleInFog: true
    };
    const transition = sideSpaces.length === desired - 1 && areaFloor === 1 ? {
      kind: 'floorSkip',
      targetBiome: floor.biome,
      targetFloor: 3
    } : undefined;
    getTile(floor, entry.x, entry.y).kind = 'breakwall';
    chamber.forEach(point => {
      getTile(floor, point.x, point.y).kind = 'floor';
      reserved.add(indexOf(floor, point.x, point.y));
    });
    reserved.add(indexOf(floor, entry.x, entry.y));
    floor.items.push(reward);
    sideSpaces.push({
      id: `mine-breach:${floor.seed}:${sideSpaces.length}`,
      kind: 'mine-breach-room',
      approach: {
        ...candidate.approach
      },
      entry,
      chamber,
      reward,
      ...(transition ? {
        rareTransition: transition
      } : {})
    });
  }
  if (sideSpaces.length !== desired) throw new Error(`failed Mine breach-room generation: expected ${desired}, found ${sideSpaces.length}`);
  floor.sideSpaces = sideSpaces;
};
const imprintWildsCaves = (floor, rng) => {
  if (floor.biome !== 'wilds') return;
  const desired = 1 + Math.floor(floor.index % 4 / 2);
  const reachable = [...reachableFloorIndexes(floor)].map(index => pointAt(floor, index));
  const candidates = rng.shuffle(reachable.flatMap(approach => mineBreachDirections.map(direction => ({
    approach,
    direction
  }))));
  const caves = [];
  const reserved = new Set();
  for (const candidate of candidates) {
    var _getTile6;
    if (caves.length === desired) break;
    const entry = {
      x: candidate.approach.x + candidate.direction.x,
      y: candidate.approach.y + candidate.direction.y
    };
    if (((_getTile6 = getTile(floor, entry.x, entry.y)) === null || _getTile6 === void 0 ? void 0 : _getTile6.kind) !== 'wall' || reserved.has(indexOf(floor, entry.x, entry.y))) continue;
    const chamber = breachChamber(floor, candidate.approach, candidate.direction, 1, 3);
    if (!chamber || chamber.some(point => reserved.has(indexOf(floor, point.x, point.y)))) continue;
    const rewardPoint = chamber[1];
    const reward = {
      id: caves.length % 2 ? 'ropeBundle' : 'focusTonic',
      x: rewardPoint.x,
      y: rewardPoint.y,
      count: 1,
      visibleInFog: true
    };
    getTile(floor, entry.x, entry.y).kind = 'breakwall';
    reserved.add(indexOf(floor, entry.x, entry.y));
    chamber.forEach(point => {
      getTile(floor, point.x, point.y).kind = 'floor';
      reserved.add(indexOf(floor, point.x, point.y));
    });
    floor.items.push(reward);
    caves.push({
      id: `wilds-cave:${floor.seed}:${caves.length}`,
      kind: 'wilds-cave',
      approach: {
        ...candidate.approach
      },
      entry,
      chamber,
      reward
    });
  }
  if (caves.length !== desired) throw new Error(`failed Wilds cave generation: expected ${desired}, found ${caves.length}`);
  floor.sideSpaces = caves;
};
const imprintCavernHiddenChambers = (floor, rng) => {
  if (floor.biome !== 'caverns') return;
  const desired = 1 + Math.floor(floor.index % 4 / 2);
  const reachable = [...reachableFloorIndexes(floor)].map(index => pointAt(floor, index));
  const candidates = rng.shuffle(reachable.flatMap(approach => mineBreachDirections.map(direction => ({
    approach,
    direction
  }))));
  const chambers = [];
  const reserved = new Set();
  for (const candidate of candidates) {
    var _getTile7, _getTile8;
    if (chambers.length === desired) break;
    const entry = {
      x: candidate.approach.x + candidate.direction.x,
      y: candidate.approach.y + candidate.direction.y
    };
    if (((_getTile7 = getTile(floor, candidate.approach.x, candidate.approach.y)) === null || _getTile7 === void 0 ? void 0 : _getTile7.kind) !== 'floor' || ((_getTile8 = getTile(floor, entry.x, entry.y)) === null || _getTile8 === void 0 ? void 0 : _getTile8.kind) !== 'wall' || reserved.has(indexOf(floor, entry.x, entry.y))) continue;
    const chamber = breachChamber(floor, candidate.approach, candidate.direction, 2, 3);
    if (!chamber || chamber.some(point => reserved.has(indexOf(floor, point.x, point.y)))) continue;
    const rewardPoint = chamber[Math.floor(chamber.length / 2)];
    const reward = {
      id: chambers.length % 2 ? 'waterScript' : 'ropeBundle',
      x: rewardPoint.x,
      y: rewardPoint.y,
      count: 1,
      visibleInFog: true
    };
    const waterHint = {
      ...candidate.approach
    };
    const hintTile = getTile(floor, waterHint.x, waterHint.y);
    hintTile.kind = 'current';
    hintTile.flow = {
      direction: flowDirection(entry.x - waterHint.x, entry.y - waterHint.y)
    };
    getTile(floor, entry.x, entry.y).kind = 'breakwall';
    reserved.add(indexOf(floor, entry.x, entry.y));
    chamber.forEach(point => {
      getTile(floor, point.x, point.y).kind = 'floor';
      reserved.add(indexOf(floor, point.x, point.y));
    });
    floor.items.push(reward);
    chambers.push({
      id: `cavern-hidden:${floor.seed}:${chambers.length}`,
      kind: 'cavern-hidden-chamber',
      approach: {
        ...candidate.approach
      },
      entry,
      chamber,
      reward,
      waterHint
    });
  }
  if (chambers.length !== desired) throw new Error(`failed Cavern hidden-chamber generation: expected ${desired}, found ${chambers.length}`);
  floor.sideSpaces = chambers;
};
const imprintRuinsHiddenChambers = (floor, macro, rng) => {
  if (floor.biome !== 'ruins') return;
  const desired = 1 + Math.floor(floor.index % 4 / 2);
  const reachable = [...reachableFloorIndexes(floor)].map(index => pointAt(floor, index));
  const candidates = rng.shuffle(reachable.flatMap(approach => mineBreachDirections.map(direction => ({
    approach,
    direction
  }))));
  const chambers = [];
  const reserved = new Set();
  const macroCells = new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y)));
  for (const candidate of candidates) {
    if (chambers.length === desired) break;
    const entry = {
      x: candidate.approach.x + candidate.direction.x,
      y: candidate.approach.y + candidate.direction.y
    };
    const chamber = [];
    for (let forward = 2; forward < 4; forward++) for (let lateral = -1; lateral <= 1; lateral++) chamber.push({
      x: candidate.approach.x + candidate.direction.x * forward + candidate.direction.cross.x * lateral,
      y: candidate.approach.y + candidate.direction.y * forward + candidate.direction.cross.y * lateral
    });
    const chamberIndexes = new Set(chamber.map(point => indexOf(floor, point.x, point.y)));
    const barrier = chamber.flatMap(point => pathOffsets.map(([x, y]) => ({
      x: point.x + x,
      y: point.y + y
    }))).filter(point => !chamberIndexes.has(indexOf(floor, point.x, point.y)) && (point.x !== entry.x || point.y !== entry.y)).filter((point, index, points) => points.findIndex(other => other.x === point.x && other.y === point.y) === index);
    const changed = [entry, ...chamber, ...barrier];
    if (changed.some(point => {
      var _getTile9;
      return !getTile(floor, point.x, point.y) || ((_getTile9 = getTile(floor, point.x, point.y)) === null || _getTile9 === void 0 ? void 0 : _getTile9.kind) !== 'floor' || reserved.has(indexOf(floor, point.x, point.y)) || macroCells.has(indexOf(floor, point.x, point.y));
    })) continue;
    const before = changed.map(point => ({
      point,
      kind: getTile(floor, point.x, point.y).kind
    }));
    barrier.forEach(point => setKind(floor, point.x, point.y, 'wall'));
    setKind(floor, entry.x, entry.y, 'breakwall');
    if (!hasPassablePath(floor, floor.start, floor.exit)) {
      before.forEach(({
        point,
        kind
      }) => setKind(floor, point.x, point.y, kind));
      continue;
    }
    const rewardPoint = chamber[Math.floor(chamber.length / 2)];
    const reward = {
      id: chambers.length % 2 ? 'sunseal' : 'wardScript',
      x: rewardPoint.x,
      y: rewardPoint.y,
      count: 1,
      visibleInFog: true
    };
    changed.forEach(point => reserved.add(indexOf(floor, point.x, point.y)));
    floor.items.push(reward);
    chambers.push({
      id: `ritual-hidden:${floor.seed}:${chambers.length}`,
      kind: 'ritual-hidden-chamber',
      approach: {
        ...candidate.approach
      },
      entry,
      chamber,
      reward
    });
  }
  if (chambers.length !== desired) throw new Error(`failed ritual hidden-chamber generation: expected ${desired}, found ${chambers.length}`);
  floor.sideSpaces = chambers;
};
const imprintFurnaceServiceSpaces = (floor, macro, rng) => {
  if (floor.biome !== 'furnace') return;
  const desired = 1 + Math.floor(floor.index % 4 / 2);
  const reachable = [...reachableFloorIndexes(floor)].map(index => pointAt(floor, index));
  const candidates = rng.shuffle(reachable.flatMap(approach => mineBreachDirections.map(direction => ({
    approach,
    direction
  }))));
  const spaces = [];
  const reserved = new Set();
  const macroCells = new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y)));
  for (const candidate of candidates) {
    if (spaces.length === desired) break;
    const entry = {
      x: candidate.approach.x + candidate.direction.x,
      y: candidate.approach.y + candidate.direction.y
    };
    const chamber = [];
    for (let forward = 2; forward < 4; forward++) for (let lateral = -1; lateral <= 1; lateral++) chamber.push({
      x: candidate.approach.x + candidate.direction.x * forward + candidate.direction.cross.x * lateral,
      y: candidate.approach.y + candidate.direction.y * forward + candidate.direction.cross.y * lateral
    });
    const chamberIndexes = new Set(chamber.map(point => indexOf(floor, point.x, point.y)));
    const barrier = chamber.flatMap(point => pathOffsets.map(([x, y]) => ({
      x: point.x + x,
      y: point.y + y
    }))).filter(point => !chamberIndexes.has(indexOf(floor, point.x, point.y)) && (point.x !== entry.x || point.y !== entry.y)).filter((point, index, points) => points.findIndex(other => other.x === point.x && other.y === point.y) === index);
    const changed = [entry, ...chamber, ...barrier];
    if (changed.some(point => {
      var _getTile0;
      return !getTile(floor, point.x, point.y) || ((_getTile0 = getTile(floor, point.x, point.y)) === null || _getTile0 === void 0 ? void 0 : _getTile0.kind) !== 'floor' || reserved.has(indexOf(floor, point.x, point.y)) || macroCells.has(indexOf(floor, point.x, point.y));
    })) continue;
    const before = changed.map(point => ({
      point,
      kind: getTile(floor, point.x, point.y).kind
    }));
    barrier.forEach(point => setKind(floor, point.x, point.y, 'wall'));
    setKind(floor, entry.x, entry.y, 'breakwall');
    if (!hasPassablePath(floor, floor.start, floor.exit)) {
      before.forEach(({
        point,
        kind
      }) => setKind(floor, point.x, point.y, kind));
      continue;
    }
    const rewardPoint = chamber[Math.floor(chamber.length / 2)];
    const reward = {
      id: spaces.length % 2 ? 'breachCharge' : 'sootFilter',
      x: rewardPoint.x,
      y: rewardPoint.y,
      count: 1,
      visibleInFog: true
    };
    changed.forEach(point => reserved.add(indexOf(floor, point.x, point.y)));
    floor.items.push(reward);
    spaces.push({
      id: `furnace-service:${floor.seed}:${spaces.length}`,
      kind: 'furnace-service-space',
      approach: {
        ...candidate.approach
      },
      entry,
      chamber,
      reward
    });
  }
  if (spaces.length !== desired) throw new Error(`failed Furnace service-space generation: expected ${desired}, found ${spaces.length}`);
  floor.sideSpaces = spaces;
};
const imprintRuinsRitualCenter = (floor, macro) => {
  if (floor.biome !== 'ruins') return;
  const node = macro.nodes.find(candidate => candidate.kind === 'objective');
  if (!node) throw new Error(`missing Ruins ritual center: ${macro.recipeId}`);
  const point = {
    x: node.footprint.x + Math.floor(node.footprint.width / 2),
    y: node.footprint.y + Math.floor(node.footprint.height / 2)
  };
  for (const [x, y] of cardinalOffsets) setKind(floor, point.x + x, point.y + y, 'floor');
  setKind(floor, point.x, point.y, 'altar');
};
const imprintCavernTideRoute = (floor, macro) => {
  if (floor.biome !== 'caverns') return;
  const edge = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  if (!edge) throw new Error(`missing Caverns tide route: ${macro.recipeId}`);
  const cells = edge.cells.slice(3, -3).filter(point => {
    var _getTile1;
    return ((_getTile1 = getTile(floor, point.x, point.y)) === null || _getTile1 === void 0 ? void 0 : _getTile1.kind) === 'floor';
  });
  const length = Math.min(cells.length, 5 + floor.index % 4 * 2);
  const start = Math.max(0, Math.floor((cells.length - length) / 2));
  const route = cells.slice(start, start + length);
  if (route.length < 3) throw new Error(`short Caverns tide route: ${macro.recipeId}`);
  for (let index = 0; index < route.length; index++) {
    const point = route[index];
    const tile = getTile(floor, point.x, point.y);
    if (!tile) continue;
    if (index % 2 === 0) setKind(floor, point.x, point.y, 'water');else {
      var _route;
      tile.kind = 'current';
      const next = (_route = route[index + 1]) !== null && _route !== void 0 ? _route : route[index - 1];
      tile.flow = {
        direction: flowDirection(next.x - point.x, next.y - point.y)
      };
    }
  }
  const flood = floor.index % 4;
  const cavernCells = macro.nodes.flatMap(node => Array.from({
    length: Math.max(0, (node.footprint.width - 2) * (node.footprint.height - 2))
  }, (_, index) => ({
    x: node.footprint.x + 1 + index % (node.footprint.width - 2),
    y: node.footprint.y + 1 + Math.floor(index / (node.footprint.width - 2))
  }))).filter(point => {
    var _getTile10;
    return ((_getTile10 = getTile(floor, point.x, point.y)) === null || _getTile10 === void 0 ? void 0 : _getTile10.kind) === 'floor';
  });
  for (let index = 0; index < flood * 8 && cavernCells.length; index++) {
    const point = cavernCells[index % cavernCells.length];
    const tile = getTile(floor, point.x, point.y);
    tile.kind = 'current';
    tile.flow = {
      direction: index % 2 ? 'e' : 'w'
    };
  }
};
const imprintWildsPressureRoute = (floor, macro) => {
  var _safe$cells$slice$fil;
  if (floor.biome !== 'wilds') return;
  const edge = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  if (!edge) throw new Error(`missing Wilds pressure route: ${macro.recipeId}`);
  const cells = edge.cells.slice(3, -3).filter(point => {
    var _getTile11;
    return ((_getTile11 = getTile(floor, point.x, point.y)) === null || _getTile11 === void 0 ? void 0 : _getTile11.kind) === 'floor';
  });
  const start = Math.max(0, Math.floor(cells.length / 2) - 3);
  const route = cells.slice(start, start + 7);
  if (route.length < 3) throw new Error(`short Wilds pressure route: ${macro.recipeId}`);
  for (let index = 0; index < route.length; index++) setKind(floor, route[index].x, route[index].y, index % 2 === 0 ? 'water' : 'web');
  const safe = macro.edges.find(candidate => candidate.modes.includes('safe'));
  const safeRoute = (_safe$cells$slice$fil = safe === null || safe === void 0 ? void 0 : safe.cells.slice(3, -3).filter(point => {
    var _getTile12;
    return ((_getTile12 = getTile(floor, point.x, point.y)) === null || _getTile12 === void 0 ? void 0 : _getTile12.kind) === 'floor';
  })) !== null && _safe$cells$slice$fil !== void 0 ? _safe$cells$slice$fil : [];
  const lower = safeRoute[1];
  const upper = route.at(-2);
  if (!lower || !upper) throw new Error(`missing Wilds climb route: ${macro.recipeId}`);
  getTile(floor, lower.x, lower.y).elevation = 0;
  getTile(floor, upper.x, upper.y).elevation = 1;
  floor.climbLinks = [{
    id: `wilds-climb:${floor.index}:${lower.x}:${lower.y}:${upper.x}:${upper.y}`,
    lower,
    upper,
    anchored: false
  }];
};
const imprintRuinsWardRoute = (floor, macro) => {
  if (floor.biome !== 'ruins') return;
  const edge = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  if (!edge) throw new Error(`missing Ruins ward route: ${macro.recipeId}`);
  const cells = edge.cells.slice(3, -3).filter(point => {
    var _getTile13;
    return ((_getTile13 = getTile(floor, point.x, point.y)) === null || _getTile13 === void 0 ? void 0 : _getTile13.kind) === 'floor';
  });
  const start = Math.max(0, Math.floor(cells.length / 2) - 3);
  const route = cells.slice(start, start + 7);
  if (route.length < 3) throw new Error(`short Ruins ward route: ${macro.recipeId}`);
  for (let index = 0; index < route.length; index++) if (index % 2 === 0) setKind(floor, route[index].x, route[index].y, 'dart');
};
const imprintFurnaceFiringRoute = (floor, macro) => {
  if (floor.biome !== 'furnace') return;
  const safe = macro.edges.find(candidate => candidate.modes.includes('safe'));
  const costly = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  if (!safe || !costly) throw new Error(`missing Furnace route pair: ${macro.recipeId}`);
  const transit = cells => cells.slice(3, -3).filter(point => {
    var _getTile14;
    return ((_getTile14 = getTile(floor, point.x, point.y)) === null || _getTile14 === void 0 ? void 0 : _getTile14.kind) === 'floor';
  });
  const liftLane = transit(safe.cells);
  const firingLane = transit(costly.cells);
  if (liftLane.length < 3 || firingLane.length < 4) throw new Error(`short Furnace terrace route: ${macro.recipeId}`);
  for (let index = 0; index < liftLane.length; index++) {
    const point = liftLane[index];
    const tile = getTile(floor, point.x, point.y);
    tile.elevation = 1;
    if (index % 3 === 1) tile.kind = 'lift';
  }
  for (let index = 0; index < firingLane.length; index++) {
    const point = firingLane[index];
    const tile = getTile(floor, point.x, point.y);
    tile.elevation = 0;
    if (index % 3 === 0) tile.kind = 'smoke';else if (index % 3 === 1) tile.kind = 'fireVent';
  }
  const lower = firingLane.find(point => {
    var _getTile15;
    return ((_getTile15 = getTile(floor, point.x, point.y)) === null || _getTile15 === void 0 ? void 0 : _getTile15.kind) === 'smoke';
  });
  const upper = liftLane.find(point => {
    var _getTile16;
    return ((_getTile16 = getTile(floor, point.x, point.y)) === null || _getTile16 === void 0 ? void 0 : _getTile16.kind) === 'lift';
  });
  floor.climbLinks = [{
    id: `lift:${floor.index}:${lower.x}:${lower.y}:${upper.x}:${upper.y}`,
    lower,
    upper,
    anchored: true
  }];
  const heatNetwork = [...firingLane];
  for (let index = 0; index < floor.index % 4 * 4; index++) {
    const anchor = firingLane[index % firingLane.length];
    const point = cardinalOffsets.map(([x, y]) => ({
      x: anchor.x + x,
      y: anchor.y + y
    })).find(candidate => {
      var _getTile17;
      return ((_getTile17 = getTile(floor, candidate.x, candidate.y)) === null || _getTile17 === void 0 ? void 0 : _getTile17.kind) === 'floor' && !heatNetwork.some(existing => existing.x === candidate.x && existing.y === candidate.y);
    });
    if (!point) continue;
    getTile(floor, point.x, point.y).kind = index % 2 ? 'smoke' : 'fireVent';
    heatNetwork.push(point);
  }
  const kiln = macro.nodes.find(node => node.kind === 'objective');
  if (!kiln) throw new Error(`missing Furnace kiln core: ${macro.recipeId}`);
  floor.furnaceLayout = {
    kiln: {
      x: kiln.footprint.x + Math.floor(kiln.footprint.width / 2),
      y: kiln.footprint.y + Math.floor(kiln.footprint.height / 2)
    },
    heatNetwork,
    liftLane: [...liftLane]
  };
};
const imprintFloodedCurrentNetwork = (floor, macro) => {
  var _getTile20;
  if (floor.biome !== 'floodedRuins') return;
  const safe = macro.edges.find(candidate => candidate.modes.includes('safe'));
  const costly = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  const refuge = macro.nodes.find(candidate => candidate.kind === 'optionalReward');
  if (!safe || !costly || !refuge) throw new Error(`missing Flooded route network: ${macro.recipeId}`);
  const transit = cells => cells.filter(point => {
    var _getTile18;
    return ((_getTile18 = getTile(floor, point.x, point.y)) === null || _getTile18 === void 0 ? void 0 : _getTile18.kind) === 'floor';
  });
  const refugeRoute = transit(safe.cells);
  const currentRoute = transit(costly.cells);
  if (refugeRoute.length < 3 || currentRoute.length < 4) throw new Error(`short Flooded current network: ${macro.recipeId}`);
  for (let index = 0; index < refugeRoute.length; index++) setKind(floor, refugeRoute[index].x, refugeRoute[index].y, index % 4 === 1 ? 'anchor' : 'floor');
  for (let index = 0; index < currentRoute.length - 1; index++) {
    const point = currentRoute[index];
    const next = currentRoute[index + 1];
    const tile = getTile(floor, point.x, point.y);
    tile.kind = 'current';
    tile.flow = {
      direction: flowDirection(next.x - point.x, next.y - point.y),
      ...(index >= currentRoute.length - 3 ? {
        hazard: 'undertow'
      } : {})
    };
  }
  const outlet = currentRoute[currentRoute.length - 1];
  const bank = cardinalOffsets.map(([x, y]) => ({
    x: outlet.x + x,
    y: outlet.y + y
  })).find(point => {
    var _getTile19;
    return ((_getTile19 = getTile(floor, point.x, point.y)) === null || _getTile19 === void 0 ? void 0 : _getTile19.kind) === 'floor';
  });
  const next = bank !== null && bank !== void 0 ? bank : currentRoute[currentRoute.length - 2];
  const outletTile = getTile(floor, outlet.x, outlet.y);
  outletTile.kind = 'current';
  outletTile.flow = {
    direction: flowDirection(next.x - outlet.x, next.y - outlet.y)
  };
  if (bank) setKind(floor, bank.x, bank.y, 'anchor');
  const island = {
    x: refuge.footprint.x + Math.floor(refuge.footprint.width / 2),
    y: refuge.footprint.y + Math.floor(refuge.footprint.height / 2)
  };
  if (((_getTile20 = getTile(floor, island.x, island.y)) === null || _getTile20 === void 0 ? void 0 : _getTile20.kind) === 'floor') setKind(floor, island.x, island.y, 'anchor');
};
const imprintFloodedWhirlpools = (floor, macro, rng) => {
  if (floor.biome !== 'floodedRuins') return;
  const desired = 1 + floor.index % 4;
  const radius = floor.index % 4 > 1 ? 2 : 1;
  const macroCells = new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y)));
  const centers = rng.shuffle(floor.tiles.flatMap((tile, index) => tile.kind === 'floor' ? [pointAt(floor, index)] : []));
  const whirlpools = [];
  for (const center of centers) {
    var _getTile21, _getTile22;
    if (whirlpools.length === desired) break;
    const cells = [];
    for (let y = center.y - radius; y <= center.y + radius; y++) for (let x = center.x - radius; x <= center.x + radius; x++) if (x !== center.x || y !== center.y) cells.push({
      x,
      y
    });
    const anchor = {
      x: center.x + radius + 1,
      y: center.y
    };
    if (((_getTile21 = getTile(floor, center.x, center.y)) === null || _getTile21 === void 0 ? void 0 : _getTile21.kind) !== 'floor' || ((_getTile22 = getTile(floor, anchor.x, anchor.y)) === null || _getTile22 === void 0 ? void 0 : _getTile22.kind) !== 'floor' || [center, anchor, ...cells].some(point => {
      var _getTile23;
      return !getTile(floor, point.x, point.y) || ((_getTile23 = getTile(floor, point.x, point.y)) === null || _getTile23 === void 0 ? void 0 : _getTile23.kind) !== 'floor' || macroCells.has(indexOf(floor, point.x, point.y)) || whirlpools.some(whirlpool => whirlpool.cells.some(cell => cell.x === point.x && cell.y === point.y) || whirlpool.center.x === point.x && whirlpool.center.y === point.y);
    })) continue;
    setKind(floor, center.x, center.y, 'deepWater');
    for (const point of cells) {
      const tile = getTile(floor, point.x, point.y);
      tile.kind = 'current';
      tile.flow = {
        direction: flowDirection(center.x - point.x, center.y - point.y),
        hazard: 'undertow'
      };
    }
    setKind(floor, anchor.x, anchor.y, 'anchor');
    whirlpools.push({
      id: `whirlpool:${floor.seed}:${whirlpools.length}`,
      center,
      radius,
      cells,
      anchor
    });
  }
  if (whirlpools.length !== desired) throw new Error(`failed Flooded whirlpool generation: expected ${desired}, found ${whirlpools.length}`);
  floor.whirlpools = whirlpools;
};
const imprintCliffHeightGraph = (floor, macro) => {
  if (floor.biome !== 'cliffs') return;
  const areaFloor = floor.index % 4;
  const safe = macro.edges.find(candidate => candidate.modes.includes('safe'));
  const costly = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  if (!safe || !costly) throw new Error(`missing Cliffs height routes: ${macro.recipeId}`);
  const transit = cells => cells.slice(3, -3).filter(point => {
    var _getTile24;
    return ((_getTile24 = getTile(floor, point.x, point.y)) === null || _getTile24 === void 0 ? void 0 : _getTile24.kind) === 'floor';
  });
  const highRoute = transit(safe.cells);
  const lowRoute = transit(costly.cells);
  if (highRoute.length < 4 || lowRoute.length < 4) throw new Error(`short Cliffs height routes: ${macro.recipeId}`);
  const occupied = new Set([...highRoute, ...lowRoute].map(point => indexOf(floor, point.x, point.y)));
  const midLedges = macro.edges.flatMap(edge => transit(edge.cells)).filter(point => !occupied.has(indexOf(floor, point.x, point.y)));
  if (midLedges.length < 4) throw new Error(`short Cliffs mid ledges: ${macro.recipeId}`);
  for (let index = 0; index < highRoute.length; index++) {
    const point = highRoute[index];
    const tile = getTile(floor, point.x, point.y);
    tile.elevation = 2;
    tile.kind = index % 3 === 1 ? 'rope' : 'floor';
  }
  for (const point of lowRoute) {
    const tile = getTile(floor, point.x, point.y);
    tile.elevation = 0;
    tile.kind = 'ledge';
  }
  for (let index = 0; index < midLedges.length; index++) {
    const tile = getTile(floor, midLedges[index].x, midLedges[index].y);
    tile.elevation = 1;
    if (index % 4 === 0) tile.kind = 'rope';
  }
  const overlook = macro.nodes.find(candidate => candidate.kind === 'optionalReward');
  const perch = overlook && Array.from({
    length: overlook.footprint.width * overlook.footprint.height
  }, (_, index) => ({
    x: overlook.footprint.x + index % overlook.footprint.width,
    y: overlook.footprint.y + Math.floor(index / overlook.footprint.width)
  })).find(point => {
    var _getTile25;
    return ((_getTile25 = getTile(floor, point.x, point.y)) === null || _getTile25 === void 0 ? void 0 : _getTile25.kind) === 'floor';
  });
  if (!perch) throw new Error(`missing Cliffs exposed overlook: ${macro.recipeId}`);
  const perchTile = getTile(floor, perch.x, perch.y);
  perchTile.elevation = 2;
  perchTile.kind = 'ledge';
  const nearest = (lower, upper, used) => lower.flatMap(from => upper.map(to => ({
    lower: from,
    upper: to
  }))).filter(link => !used.has(indexOf(floor, link.lower.x, link.lower.y)) && !used.has(indexOf(floor, link.upper.x, link.upper.y))).sort((left, right) => distance(left.lower, left.upper) - distance(right.lower, right.upper))[0];
  const pools = [[lowRoute, midLedges], [midLedges, highRoute], [lowRoute, highRoute]];
  const links = [];
  const used = new Set();
  for (let index = 0; index < 2 + areaFloor; index++) {
    const pair = pools[index % pools.length];
    const link = nearest(pair[0], pair[1], used);
    if (!link) throw new Error(`short Cliffs climb network: ${macro.recipeId}`);
    links.push(link);
    used.add(indexOf(floor, link.lower.x, link.lower.y));
    used.add(indexOf(floor, link.upper.x, link.upper.y));
  }
  floor.climbLinks = links.map(({
    lower,
    upper
  }, index) => ({
    id: `cliff:${floor.index}:${index}:${lower.x}:${lower.y}:${upper.x}:${upper.y}`,
    lower,
    upper,
    anchored: false
  }));
  const windCorridors = [];
  for (let index = 0; index < areaFloor; index++) {
    const route = lowRoute;
    const start = Math.min(route.length - 3, 1 + index * 3);
    const corridor = route.slice(start, start + 3);
    if (corridor.length < 2) continue;
    for (let step = 0; step < corridor.length - 1; step++) {
      const point = corridor[step];
      const next = corridor[step + 1];
      const tile = getTile(floor, point.x, point.y);
      tile.kind = 'ledge';
      tile.flow = {
        direction: flowDirection(next.x - point.x, next.y - point.y),
        hazard: 'squall'
      };
    }
    const tieOff = getTile(floor, corridor.at(-1).x, corridor.at(-1).y);
    tieOff.kind = 'rope';
    delete tieOff.flow;
    windCorridors.push(corridor);
  }
  const shelteredPockets = floor.tiles.flatMap((tile, index) => tile.kind === 'floor' && cardinalOffsets.some(([x, y]) => {
    var _getTile26;
    return ((_getTile26 = getTile(floor, index % floor.width + x, Math.floor(index / floor.width) + y)) === null || _getTile26 === void 0 ? void 0 : _getTile26.kind) === 'cliffWall';
  }) ? [pointAt(floor, index)] : []).slice(0, 1 + areaFloor);
  floor.cliffLayout = {
    lowRoute: [...lowRoute],
    midLedges,
    highRidge: [...highRoute],
    windCorridors,
    shelteredPockets
  };
};
const imprintCliffAlcoves = (floor, macro, rng) => {
  if (floor.biome !== 'cliffs') return;
  const desired = 1 + floor.index % 4;
  const macroCells = new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y)));
  const reserved = new Set();
  const alcoves = [];
  const reachable = [...reachableFloorIndexes(floor)].map(index => pointAt(floor, index));
  for (const candidate of rng.shuffle(reachable.flatMap(approach => mineBreachDirections.map(direction => ({
    approach,
    direction
  }))))) {
    var _getTile27;
    if (alcoves.length === desired) break;
    const entry = {
      x: candidate.approach.x + candidate.direction.x,
      y: candidate.approach.y + candidate.direction.y
    };
    const chamber = [];
    for (let forward = 2; forward < 4; forward++) for (let lateral = -1; lateral <= 1; lateral++) chamber.push({
      x: candidate.approach.x + candidate.direction.x * forward + candidate.direction.cross.x * lateral,
      y: candidate.approach.y + candidate.direction.y * forward + candidate.direction.cross.y * lateral
    });
    const changed = [entry, ...chamber];
    if (((_getTile27 = getTile(floor, entry.x, entry.y)) === null || _getTile27 === void 0 ? void 0 : _getTile27.kind) !== 'cliffWall' || changed.some(point => {
      var _getTile28;
      return !getTile(floor, point.x, point.y) || ((_getTile28 = getTile(floor, point.x, point.y)) === null || _getTile28 === void 0 ? void 0 : _getTile28.kind) !== 'cliffWall' || macroCells.has(indexOf(floor, point.x, point.y)) || reserved.has(indexOf(floor, point.x, point.y));
    })) continue;
    const elevation = alcoves.length % 2 ? 1 : 2;
    const rewardPoint = chamber[Math.floor(chamber.length / 2)];
    const reward = {
      id: alcoves.length % 2 ? 'cliffSpool' : 'skyMap',
      x: rewardPoint.x,
      y: rewardPoint.y,
      count: 1,
      visibleInFog: true
    };
    const entryTile = getTile(floor, entry.x, entry.y);
    entryTile.kind = 'rope';
    entryTile.elevation = elevation;
    chamber.forEach(point => {
      const tile = getTile(floor, point.x, point.y);
      tile.kind = 'ledge';
      tile.elevation = elevation;
      reserved.add(indexOf(floor, point.x, point.y));
    });
    reserved.add(indexOf(floor, entry.x, entry.y));
    floor.items.push(reward);
    alcoves.push({
      id: `cliff-alcove:${floor.seed}:${alcoves.length}`,
      kind: 'cliff-alcove',
      approach: {
        ...candidate.approach
      },
      entry,
      chamber,
      reward,
      elevation
    });
  }
  if (alcoves.length !== desired) throw new Error(`failed Cliffs alcove generation: expected ${desired}, found ${alcoves.length}`);
  floor.sideSpaces = alcoves;
};
const imprintBurialCrypts = (floor, macro, rng) => {
  if (floor.biome !== 'burial') return;
  const areaFloor = floor.index % 4;
  const desired = 1 + areaFloor;
  const macroCells = new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y)));
  const reserved = new Set();
  const crypts = [];
  const reachable = [...reachableFloorIndexes(floor)].map(index => pointAt(floor, index));
  for (const candidate of rng.shuffle(reachable.flatMap(approach => mineBreachDirections.map(direction => ({
    approach,
    direction
  }))))) {
    if (crypts.length === desired) break;
    const depth = 3 + areaFloor;
    const width = 3 + (crypts.length + areaFloor) % 2 * 2;
    const radius = Math.floor(width / 2);
    const entry = {
      x: candidate.approach.x + candidate.direction.x,
      y: candidate.approach.y + candidate.direction.y
    };
    const chamber = [];
    for (let forward = 2; forward < depth + 2; forward++) for (let lateral = -radius; lateral <= radius; lateral++) chamber.push({
      x: candidate.approach.x + candidate.direction.x * forward + candidate.direction.cross.x * lateral,
      y: candidate.approach.y + candidate.direction.y * forward + candidate.direction.cross.y * lateral
    });
    const rejoin = {
      x: candidate.approach.x + candidate.direction.x * (depth + 2),
      y: candidate.approach.y + candidate.direction.y * (depth + 2)
    };
    const rejoinApproach = {
      x: candidate.approach.x + candidate.direction.x * (depth + 3),
      y: candidate.approach.y + candidate.direction.y * (depth + 3)
    };
    const chamberIndexes = new Set(chamber.map(point => indexOf(floor, point.x, point.y)));
    const barrier = chamber.flatMap(point => pathOffsets.map(([x, y]) => ({
      x: point.x + x,
      y: point.y + y
    }))).filter(point => !chamberIndexes.has(indexOf(floor, point.x, point.y)) && (point.x !== entry.x || point.y !== entry.y) && (point.x !== rejoin.x || point.y !== rejoin.y)).filter((point, index, points) => points.findIndex(other => other.x === point.x && other.y === point.y) === index);
    const changed = [entry, rejoin, ...chamber, ...barrier];
    const protectedPoints = [candidate.approach, rejoinApproach, ...changed];
    if (protectedPoints.some(point => {
      var _getTile29;
      return !getTile(floor, point.x, point.y) || ((_getTile29 = getTile(floor, point.x, point.y)) === null || _getTile29 === void 0 ? void 0 : _getTile29.kind) !== 'floor' || macroCells.has(indexOf(floor, point.x, point.y)) || reserved.has(indexOf(floor, point.x, point.y));
    })) continue;
    const before = changed.map(point => ({
      point,
      kind: getTile(floor, point.x, point.y).kind
    }));
    barrier.forEach(point => setKind(floor, point.x, point.y, 'wall'));
    setKind(floor, entry.x, entry.y, 'breakwall');
    setKind(floor, rejoin.x, rejoin.y, 'breakwall');
    if (!hasPassablePath(floor, floor.start, floor.exit)) {
      before.forEach(({
        point,
        kind
      }) => setKind(floor, point.x, point.y, kind));
      continue;
    }
    chamber.forEach((point, index) => setKind(floor, point.x, point.y, index % 7 === 0 ? 'ossuary' : index % 5 === 0 ? 'spiritPath' : index % 3 === 0 ? 'graveSoil' : 'floor'));
    const rewardPoint = chamber[Math.floor(chamber.length / 2)];
    const reward = {
      id: crypts.length % 3 === 0 ? 'tombKey' : crypts.length % 3 === 1 ? 'wardScript' : 'ropeBundle',
      x: rewardPoint.x,
      y: rewardPoint.y,
      count: 1,
      visibleInFog: true
    };
    protectedPoints.forEach(point => reserved.add(indexOf(floor, point.x, point.y)));
    floor.items.push(reward);
    crypts.push({
      id: `burial-crypt:${floor.seed}:${crypts.length}`,
      kind: 'burial-crypt',
      approach: {
        ...candidate.approach
      },
      entry,
      approaches: [{
        ...candidate.approach
      }, rejoinApproach],
      entries: [entry, rejoin],
      chamber,
      reward,
      depth
    });
  }
  if (crypts.length !== desired) throw new Error(`failed Burial crypt generation: expected ${desired}, found ${crypts.length}`);
  const safe = macro.edges.find(edge => edge.modes.includes('safe'));
  const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'));
  if (!safe || !costly) throw new Error(`missing Burial processions: ${macro.recipeId}`);
  const spiritRoute = safe.cells.filter(point => {
    var _getTile30;
    return ((_getTile30 = getTile(floor, point.x, point.y)) === null || _getTile30 === void 0 ? void 0 : _getTile30.kind) === 'spiritPath';
  });
  const graveRoute = costly.cells.filter(point => {
    var _getTile31;
    return ((_getTile31 = getTile(floor, point.x, point.y)) === null || _getTile31 === void 0 ? void 0 : _getTile31.kind) === 'graveSoil';
  });
  if (spiritRoute.length < 3 || graveRoute.length < 3) throw new Error(`short Burial processions: ${macro.recipeId}`);
  const processions = Array.from({
    length: desired
  }, (_, index) => graveRoute.slice(Math.min(graveRoute.length - 3, index * 3), Math.min(graveRoute.length, index * 3 + 3))).filter(route => route.length === 3);
  if (processions.length !== desired) throw new Error(`short Burial procession network: ${macro.recipeId}`);
  const shelters = spiritRoute.filter((_, index) => index % Math.max(1, Math.floor(spiritRoute.length / desired)) === 0).slice(0, desired);
  floor.sideSpaces = crypts;
  floor.burialLayout = {
    mounds: floor.tiles.flatMap((tile, index) => tile.kind === 'cairn' ? [pointAt(floor, index)] : []).slice(0, 24 + areaFloor * 8),
    processions,
    shelters
  };
};
const imprintSaltMirages = (floor, macro, rng) => {
  if (floor.biome !== 'saltFlats') return;
  const desired = 1 + floor.index % 4;
  const macroCells = new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y)));
  const mirages = [];
  const candidates = rng.shuffle(floor.tiles.flatMap((tile, index) => tile.kind === 'floor' ? [pointAt(floor, index)] : []));
  for (const marker of candidates) {
    if (mirages.length === desired) break;
    const cells = [{
      x: marker.x,
      y: marker.y
    }, {
      x: marker.x + 1,
      y: marker.y
    }, {
      x: marker.x,
      y: marker.y + 1
    }, {
      x: marker.x + 1,
      y: marker.y + 1
    }];
    if (distance(marker, floor.start) < 8 || cells.some(point => {
      var _getTile32;
      return ((_getTile32 = getTile(floor, point.x, point.y)) === null || _getTile32 === void 0 ? void 0 : _getTile32.kind) !== 'floor' || macroCells.has(indexOf(floor, point.x, point.y)) || mirages.some(mirage => mirage.cells.some(cell => cell.x === point.x && cell.y === point.y));
    })) continue;
    cells.forEach(point => setKind(floor, point.x, point.y, 'saltMirror'));
    mirages.push({
      id: `salt-mirage:${floor.seed}:${mirages.length}`,
      marker,
      cells,
      revealed: false
    });
  }
  if (mirages.length !== desired) throw new Error(`failed Salt mirage generation: expected ${desired}, found ${mirages.length}`);
  floor.saltMirages = mirages;
};
const imprintFrostCaves = (floor, macro, rng) => {
  if (floor.biome !== 'frostReliquary') return;
  const desired = 1 + floor.index % 4;
  const macroCells = new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y)));
  const caves = [];
  const reserved = new Set();
  const reachable = [...reachableFloorIndexes(floor)].map(index => pointAt(floor, index));
  for (const candidate of rng.shuffle(reachable.flatMap(approach => mineBreachDirections.map(direction => ({
    approach,
    direction
  }))))) {
    if (caves.length === desired) break;
    const depth = 3 + floor.index % 4;
    const chamber = [];
    for (let forward = 2; forward < depth + 2; forward++) for (let lateral = -1; lateral <= 1; lateral++) chamber.push({
      x: candidate.approach.x + candidate.direction.x * forward + candidate.direction.cross.x * lateral,
      y: candidate.approach.y + candidate.direction.y * forward + candidate.direction.cross.y * lateral
    });
    const entry = {
      x: candidate.approach.x + candidate.direction.x,
      y: candidate.approach.y + candidate.direction.y
    };
    const chamberIndexes = new Set(chamber.map(point => indexOf(floor, point.x, point.y)));
    const barrier = chamber.flatMap(point => pathOffsets.map(([x, y]) => ({
      x: point.x + x,
      y: point.y + y
    }))).filter(point => !chamberIndexes.has(indexOf(floor, point.x, point.y)) && (point.x !== entry.x || point.y !== entry.y)).filter((point, index, points) => points.findIndex(other => other.x === point.x && other.y === point.y) === index);
    const changed = [entry, ...chamber, ...barrier];
    if (changed.some(point => {
      var _getTile33;
      return !getTile(floor, point.x, point.y) || ((_getTile33 = getTile(floor, point.x, point.y)) === null || _getTile33 === void 0 ? void 0 : _getTile33.kind) !== 'floor' || macroCells.has(indexOf(floor, point.x, point.y)) || reserved.has(indexOf(floor, point.x, point.y)) || floor.props.some(prop => prop.x === point.x && prop.y === point.y);
    })) continue;
    const before = changed.map(point => ({
      point,
      kind: getTile(floor, point.x, point.y).kind
    }));
    barrier.forEach(point => setKind(floor, point.x, point.y, 'wall'));
    setKind(floor, entry.x, entry.y, 'breakwall');
    if (!hasPassablePath(floor, floor.start, floor.exit)) {
      before.forEach(({
        point,
        kind
      }) => setKind(floor, point.x, point.y, kind));
      continue;
    }
    chamber.forEach((point, index) => setKind(floor, point.x, point.y, index % 5 === 0 ? 'frostRime' : index % 3 === 0 ? 'ice' : 'floor'));
    const rewardPoint = chamber[Math.floor(chamber.length / 2)];
    const reward = {
      id: caves.length % 2 ? 'grappleLine' : 'wardScript',
      x: rewardPoint.x,
      y: rewardPoint.y,
      count: 1,
      visibleInFog: true
    };
    changed.forEach(point => reserved.add(indexOf(floor, point.x, point.y)));
    floor.items.push(reward);
    caves.push({
      id: `frost-cave:${floor.seed}:${caves.length}`,
      kind: 'frost-cave',
      approach: {
        ...candidate.approach
      },
      entry,
      chamber,
      reward,
      depth
    });
  }
  if (caves.length !== desired) throw new Error(`failed Frost cave generation: expected ${desired}, found ${caves.length}`);
  const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'));
  const safe = macro.edges.find(edge => edge.modes.includes('safe'));
  if (!costly || !safe) throw new Error(`missing Frost layout: ${macro.recipeId}`);
  floor.sideSpaces = caves;
  floor.frostLayout = {
    shelves: floor.tiles.flatMap((tile, index) => tile.kind === 'ice' ? [pointAt(floor, index)] : []).slice(0, 20 + floor.index % 4 * 8),
    cracks: [costly.cells.filter(point => {
      var _getTile34;
      return ((_getTile34 = getTile(floor, point.x, point.y)) === null || _getTile34 === void 0 ? void 0 : _getTile34.kind) === 'frostRime';
    })],
    shelters: safe.cells.filter(point => {
      var _getTile35;
      return ((_getTile35 = getTile(floor, point.x, point.y)) === null || _getTile35 === void 0 ? void 0 : _getTile35.kind) === 'floor';
    }).slice(0, desired)
  };
};
const imprintBurialRitualRoutes = (floor, macro) => {
  if (floor.biome !== 'burial') return;
  const safe = macro.edges.find(candidate => candidate.modes.includes('safe'));
  const costly = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  const offering = macro.nodes.find(candidate => candidate.kind === 'optionalReward');
  if (!safe || !costly || !offering) throw new Error(`missing Burial ritual routes: ${macro.recipeId}`);
  const transit = cells => cells.slice(3, -3).filter(point => {
    var _getTile36;
    return ((_getTile36 = getTile(floor, point.x, point.y)) === null || _getTile36 === void 0 ? void 0 : _getTile36.kind) === 'floor';
  });
  const spiritRoute = transit(safe.cells);
  const graveRoute = transit(costly.cells);
  if (spiritRoute.length < 4 || graveRoute.length < 4) throw new Error(`short Burial ritual route: ${macro.recipeId}`);
  for (const point of spiritRoute) setKind(floor, point.x, point.y, 'spiritPath');
  for (const point of graveRoute) setKind(floor, point.x, point.y, 'graveSoil');
  const cache = Array.from({
    length: offering.footprint.width * offering.footprint.height
  }, (_, index) => ({
    x: offering.footprint.x + index % offering.footprint.width,
    y: offering.footprint.y + Math.floor(index / offering.footprint.width)
  })).find(point => {
    var _getTile37;
    return ((_getTile37 = getTile(floor, point.x, point.y)) === null || _getTile37 === void 0 ? void 0 : _getTile37.kind) === 'floor';
  });
  if (!cache) throw new Error(`missing Burial offering hollow: ${macro.recipeId}`);
  setKind(floor, cache.x, cache.y, 'ossuary');
};
const imprintSaltRouteContract = (floor, macro) => {
  if (floor.biome !== 'saltFlats') return;
  const safe = macro.edges.find(candidate => candidate.modes.includes('safe'));
  const costly = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  const refugeNode = macro.nodes.find(candidate => candidate.kind === 'optionalReward');
  if (!safe || !costly || !refugeNode) throw new Error(`missing Salt route choices: ${macro.recipeId}`);
  const transit = cells => cells.slice(3, -3).filter(point => {
    var _getTile38;
    return ((_getTile38 = getTile(floor, point.x, point.y)) === null || _getTile38 === void 0 ? void 0 : _getTile38.kind) === 'floor';
  });
  const stableRoute = transit(safe.cells);
  const brineRoute = transit(costly.cells);
  if (stableRoute.length < 4 || brineRoute.length < 4) throw new Error(`short Salt route: ${macro.recipeId}`);
  for (const point of stableRoute) setKind(floor, point.x, point.y, 'floor');
  for (const point of brineRoute) setKind(floor, point.x, point.y, 'brine');
  const horizon = brineRoute[Math.floor(brineRoute.length / 2)];
  setKind(floor, horizon.x, horizon.y, 'saltMirror');
  const refuge = {
    x: refugeNode.footprint.x + Math.floor(refugeNode.footprint.width / 2),
    y: refugeNode.footprint.y + Math.floor(refugeNode.footprint.height / 2)
  };
  setKind(floor, refuge.x, refuge.y, 'floor');
  for (const [x, y] of cardinalOffsets) {
    var _getTile39;
    if (((_getTile39 = getTile(floor, refuge.x + x, refuge.y + y)) === null || _getTile39 === void 0 ? void 0 : _getTile39.kind) === 'floor') setKind(floor, refuge.x + x, refuge.y + y, 'saltMirror');
  }
  const husk = propDefinition('saltFlats.caravanHusk');
  floor.props.push({
    id: `prop:${floor.index}:saltFlats.caravanHusk:${refuge.x}:${refuge.y}`,
    kind: husk.id,
    x: refuge.x,
    y: refuge.y,
    biome: floor.biome,
    state: 'dormant',
    tags: [...husk.tags],
    hooks: [...husk.hooks]
  });
  const markerPoint = stableRoute[Math.floor(stableRoute.length / 2)];
  const marker = propDefinition('saltFlats.glassMarker');
  floor.props.push({
    id: `prop:${floor.index}:saltFlats.glassMarker:${markerPoint.x}:${markerPoint.y}`,
    kind: marker.id,
    x: markerPoint.x,
    y: markerPoint.y,
    biome: floor.biome,
    state: 'dormant',
    tags: [...marker.tags],
    hooks: [...marker.hooks]
  });
};
const imprintFrostRouteContract = (floor, macro) => {
  if (floor.biome !== 'frostReliquary') return;
  const safe = macro.edges.find(candidate => candidate.modes.includes('safe'));
  const costly = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'));
  const refugeNode = macro.nodes.find(candidate => candidate.kind === 'optionalReward');
  if (!safe || !costly || !refugeNode) throw new Error(`missing Frost route choices: ${macro.recipeId}`);
  const transit = cells => cells.slice(3, -3).filter(point => {
    var _getTile40;
    return ((_getTile40 = getTile(floor, point.x, point.y)) === null || _getTile40 === void 0 ? void 0 : _getTile40.kind) === 'floor';
  });
  const shoreRoute = transit(safe.cells);
  const iceRoute = transit(costly.cells);
  if (shoreRoute.length < 4 || iceRoute.length < 4) throw new Error(`short Frost route: ${macro.recipeId}`);
  for (const point of shoreRoute) setKind(floor, point.x, point.y, 'floor');
  for (const point of iceRoute) setKind(floor, point.x, point.y, 'ice');
  const pressure = iceRoute[Math.floor(iceRoute.length / 2)];
  setKind(floor, pressure.x, pressure.y, 'frostRime');
  const refuge = {
    x: refugeNode.footprint.x + Math.floor(refugeNode.footprint.width / 2),
    y: refugeNode.footprint.y + Math.floor(refugeNode.footprint.height / 2)
  };
  setKind(floor, refuge.x, refuge.y, 'floor');
  for (const [x, y] of cardinalOffsets) {
    var _getTile41;
    if (((_getTile41 = getTile(floor, refuge.x + x, refuge.y + y)) === null || _getTile41 === void 0 ? void 0 : _getTile41.kind) !== 'wall') setKind(floor, refuge.x + x, refuge.y + y, 'floor');
  }
  const place = (kind, point) => {
    const definition = propDefinition(kind);
    floor.props.push({
      id: `prop:${floor.index}:${kind}:${point.x}:${point.y}`,
      kind,
      x: point.x,
      y: point.y,
      biome: floor.biome,
      state: 'dormant',
      tags: [...definition.tags],
      hooks: [...definition.hooks]
    });
  };
  place('frostReliquary.duelBell', refuge);
  place('frostReliquary.rimeSarcophagus', shoreRoute[Math.floor(shoreRoute.length / 2)]);
  place('frostReliquary.iceForge', iceRoute[1]);
  place('frostReliquary.frozenCache', pressure);
};
const imprintCavernSetpieceContext = (floor, macro) => {
  if (floor.biome !== 'caverns') return;
  const node = macro.nodes.find(candidate => candidate.kind === 'optionalReward');
  if (!node) throw new Error(`missing Caverns optional chamber: ${macro.recipeId}`);
  const candidates = Array.from({
    length: Math.max(0, node.footprint.width - 2) * Math.max(0, node.footprint.height - 2)
  }, (_, index) => ({
    x: node.footprint.x + 1 + index % (node.footprint.width - 2),
    y: node.footprint.y + 1 + Math.floor(index / (node.footprint.width - 2))
  }));
  const point = candidates.find(candidate => {
    var _getTile42;
    return ((_getTile42 = getTile(floor, candidate.x, candidate.y)) === null || _getTile42 === void 0 ? void 0 : _getTile42.kind) === 'floor';
  });
  if (!point) throw new Error(`missing Caverns setpiece floor: ${macro.recipeId}`);
  const nearby = cardinalOffsets.map(([x, y]) => ({
    x: point.x + x,
    y: point.y + y
  })).filter(candidate => {
    var _getTile43;
    return ((_getTile43 = getTile(floor, candidate.x, candidate.y)) === null || _getTile43 === void 0 ? void 0 : _getTile43.kind) === 'floor';
  });
  const darkness = nearby[0];
  if (!darkness) throw new Error(`missing Caverns setpiece darkness: ${macro.recipeId}`);
  const water = nearby[1];
  const pool = nearby[2];
  setKind(floor, darkness.x, darkness.y, 'darkness');
  if (water) setKind(floor, water.x, water.y, 'water');
  if (pool) setKind(floor, pool.x, pool.y, 'deepWater');
  const fungus = propDefinition('caverns.glowingFungus');
  floor.props.push({
    id: `prop:${floor.index}:caverns.glowingFungus:${point.x}:${point.y}`,
    kind: fungus.id,
    x: point.x,
    y: point.y,
    biome: floor.biome,
    state: 'dormant',
    tags: [...fungus.tags],
    hooks: [...fungus.hooks]
  });
};
const imprintBiomeLandmarks = (floor, rng, rooms) => {
  const paint = (x, y, kind) => {
    var _getTile44;
    if (((_getTile44 = getTile(floor, x, y)) === null || _getTile44 === void 0 ? void 0 : _getTile44.kind) === 'floor') setKind(floor, x, y, kind);
  };
  if (floor.biome === 'burial') {
    for (const room of rooms.slice(1)) {
      const origin = center(room);
      for (let y = -3; y <= 3; y++) for (let x = -3; x <= 3; x++) {
        const distance = Math.abs(x) + Math.abs(y);
        if (distance === 3) paint(origin.x + x, origin.y + y, 'cairn');else if (distance < 3 && rng.chance(55)) paint(origin.x + x, origin.y + y, 'graveSoil');
      }
    }
  } else if (floor.biome === 'saltFlats') {
    for (let x = 7; x < floor.width - 5; x += 11) for (let y = 2; y < floor.height - 2; y++) if ((y + x) % 7 !== 0) paint(x, y, floor.layoutId.includes('caravan') ? 'saltMirror' : 'brine');
  } else if (floor.biome === 'frostReliquary') {
    var _rooms$2;
    const lake = (_rooms$2 = rooms[1]) !== null && _rooms$2 !== void 0 ? _rooms$2 : rooms[0];
    for (let y = lake.y + 1; y < lake.y + lake.h - 1; y++) for (let x = lake.x + 1; x < lake.x + lake.w - 1; x++) paint(x, y, floor.layoutId.includes('frozen-lake') ? 'ice' : 'frostRime');
  } else if (floor.biome === 'cliffs') {
    for (let y = 5; y < floor.height - 4; y += 9) for (let x = 2; x < floor.width - 2; x++) if (x % 8 !== 0) paint(x, y, 'ledge');
  } else if (floor.biome === 'furnace') {
    for (let y = 4; y < floor.height - 3; y += 7) for (let x = 3; x < floor.width - 3; x++) if (x % 9 !== 0) paint(x, y, floor.layoutId.includes('kiln') ? 'lift' : 'smoke');
  } else if (floor.biome === 'ruins') {
    for (const room of rooms.slice(1, -1)) {
      const origin = center(room);
      for (const [x, y] of cardinalOffsets) paint(origin.x + x * 2, origin.y + y * 2, 'dart');
    }
  }
};
function connectRooms(floor, rooms) {
  for (let i = 1; i < rooms.length; i++) {
    const from = center(rooms[i - 1]);
    const to = center(rooms[i]);
    if (i % 2) {
      carveH(floor, from.x, to.x, from.y);
      carveV(floor, from.y, to.y, to.x);
    } else {
      carveV(floor, from.y, to.y, from.x);
      carveH(floor, from.x, to.x, to.y);
    }
  }
}
const nativeActorTerrain = {
  mine: ['rail', 'support'],
  wilds: ['water', 'web'],
  caverns: ['water', 'current', 'darkness'],
  ruins: ['altar', 'dart'],
  furnace: ['smoke', 'lift'],
  floodedRuins: ['current', 'anchor'],
  cliffs: ['ledge', 'rope'],
  burial: ['graveSoil', 'spiritPath'],
  saltFlats: ['saltMirror', 'brine'],
  frostReliquary: ['ice', 'frostRime']
};
const placementContext = (floor, runtime, eligible = () => true) => {
  const nodeKinds = new Map();
  const edgeModes = new Map();
  if (runtime.pilot) {
    for (const node of runtime.macro.nodes) for (let y = node.footprint.y; y < node.footprint.y + node.footprint.height; y++) for (let x = node.footprint.x; x < node.footprint.x + node.footprint.width; x++) {
      var _nodeKinds$get;
      const key = pointKey({
        x,
        y
      });
      nodeKinds.set(key, [...((_nodeKinds$get = nodeKinds.get(key)) !== null && _nodeKinds$get !== void 0 ? _nodeKinds$get : []), node.kind]);
    }
    for (const edge of runtime.macro.edges) for (const point of edge.cells) {
      var _edgeModes$get;
      const key = pointKey(point);
      edgeModes.set(key, [...((_edgeModes$get = edgeModes.get(key)) !== null && _edgeModes$get !== void 0 ? _edgeModes$get : []), ...edge.modes]);
    }
  }
  const adjacent = point => cardinalOffsets.map(([x, y]) => ({
    x: point.x + x,
    y: point.y + y
  })).filter(point => inBounds(floor, point.x, point.y));
  const blocked = point => {
    var _floor$ecology;
    return !eligible(point) || floor.props.some(prop => prop.x === point.x && prop.y === point.y) || floor.actors.some(actor => actor.health > 0 && actor.x === point.x && actor.y === point.y) || floor.items.some(item => item.x === point.x && item.y === point.y) || floor.milestones.some(milestone => milestone.x === point.x && milestone.y === point.y) || ((_floor$ecology = floor.ecology) === null || _floor$ecology === void 0 ? void 0 : _floor$ecology.some(ecology => ecology.target.x === point.x && ecology.target.y === point.y)) === true;
  };
  return {
    points: floor.tiles.map((_, index) => pointAt(floor, index)),
    terrainAt: point => {
      var _getTile45;
      return (_getTile45 = getTile(floor, point.x, point.y)) === null || _getTile45 === void 0 ? void 0 : _getTile45.kind;
    },
    passableAt: point => Boolean(getTile(floor, point.x, point.y) && isPathPassable(floor, point, true)),
    blockedAt: blocked,
    distanceFromStart: point => distance(point, floor.start),
    visibleAt: point => {
      var _getTile46;
      return Boolean((_getTile46 = getTile(floor, point.x, point.y)) === null || _getTile46 === void 0 ? void 0 : _getTile46.visible);
    },
    coveredAt: point => adjacent(point).some(candidate => {
      var _getTile$kind3, _getTile47;
      return !passable((_getTile$kind3 = (_getTile47 = getTile(floor, candidate.x, candidate.y)) === null || _getTile47 === void 0 ? void 0 : _getTile47.kind) !== null && _getTile$kind3 !== void 0 ? _getTile$kind3 : 'wall') || isBlockingProp(propAt(floor.props, candidate.x, candidate.y));
    }),
    chokepointAt: point => adjacent(point).filter(candidate => isPathPassable(floor, candidate, false)).length <= 2,
    adjacentTerrainAt: point => adjacent(point).map(candidate => {
      var _getTile48;
      return (_getTile48 = getTile(floor, candidate.x, candidate.y)) === null || _getTile48 === void 0 ? void 0 : _getTile48.kind;
    }).filter(kind => Boolean(kind)),
    nodeKindsAt: point => {
      var _nodeKinds$get2;
      return (_nodeKinds$get2 = nodeKinds.get(pointKey(point))) !== null && _nodeKinds$get2 !== void 0 ? _nodeKinds$get2 : [];
    },
    edgeModesAt: point => {
      var _edgeModes$get2;
      return (_edgeModes$get2 = edgeModes.get(pointKey(point))) !== null && _edgeModes$get2 !== void 0 ? _edgeModes$get2 : [];
    }
  };
};
const placementReachability = (floor, runtime) => {
  var _runtime$reachable;
  return (_runtime$reachable = runtime.reachable) !== null && _runtime$reachable !== void 0 ? _runtime$reachable : runtime.reachable = reachableFloorIndexes(floor);
};
const choosePlacement = (floor, runtime, contract, eligible) => {
  const reachable = placementReachability(floor, runtime);
  const selection = selectPlacement(contract, placementContext(floor, runtime, point => {
    var _eligible;
    return reachable.has(indexOf(floor, point.x, point.y)) && ((_eligible = eligible === null || eligible === void 0 ? void 0 : eligible(point)) !== null && _eligible !== void 0 ? _eligible : true);
  }));
  runtime.diagnostics.push(selection.debug);
  return selection.point;
};
function placeProps(floor, reserved, runtime) {
  const links = {
    'mine.lanternPost': 'mine.warningMarker',
    'mine.brokenCart': 'mine.discardedParcel',
    'wilds.rootShrine': 'wilds.lostParcel',
    'caverns.barnacledShrine': 'caverns.sealedParcel',
    'ruins.ritualBrazier': 'ruins.sealedCache'
  };
  const companions = new Set(Object.values(links));
  const definitions = [...propDefinitionsFor(floor.biome)].sort((left, right) => Number(companions.has(left.id)) - Number(companions.has(right.id)));
  let reachable = placementReachability(floor, runtime);
  const anchors = new Map();
  const occupied = new Set([indexOf(floor, floor.start.x, floor.start.y), indexOf(floor, floor.exit.x, floor.exit.y), ...objectiveTargets(floor).map(point => indexOf(floor, point.x, point.y)), ...floor.actors.map(actor => indexOf(floor, actor.x, actor.y)), ...floor.items.map(item => indexOf(floor, item.x, item.y)), ...floor.props.map(prop => indexOf(floor, prop.x, prop.y))]);
  for (const definition of definitions) {
    var _Object$entries$find;
    const anchorId = (_Object$entries$find = Object.entries(links).find(([, companion]) => companion === definition.id)) === null || _Object$entries$find === void 0 ? void 0 : _Object$entries$find[0];
    const anchor = anchorId ? anchors.get(anchorId) : undefined;
    if (companions.has(definition.id) && !anchor) {
      runtime.diagnostics.push({
        id: `prop:${definition.id}`,
        ranked: 0,
        usedFallback: false,
        diagnostics: [`placement prop:${definition.id}: linked setpiece anchor is unavailable`],
        requirements: {
          terrain: [...definition.terrain]
        }
      });
      continue;
    }
    const tags = new Set(definition.tags);
    const cart = definition.id === 'mine.brokenCart';
    const requirements = {
      id: `prop:${definition.id}`,
      requirements: {
        terrain: [...definition.terrain],
        minDistance: tags.has('cache') ? 8 : 5,
        ...(runtime.pilot && tags.has('route') ? cart ? {
          nodeKinds: ['landmark']
        } : {
          edgeModes: ['main']
        } : {}),
        ...(runtime.pilot && tags.has('cache') ? {
          nodeKinds: ['optionalReward']
        } : {}),
        ...(anchor ? {
          near: anchor,
          nearDistance: 10
        } : {})
      }
    };
    const point = choosePlacement(floor, runtime, requirements, candidate => reachable.has(indexOf(floor, candidate.x, candidate.y)) && !reserved.has(indexOf(floor, candidate.x, candidate.y)) && !occupied.has(indexOf(floor, candidate.x, candidate.y)) && hasPropContext(floor, definition.id, candidate));
    if (!point) continue;
    const prop = {
      id: `prop:${floor.index}:${definition.id}:${point.x}:${point.y}`,
      kind: definition.id,
      x: point.x,
      y: point.y,
      biome: floor.biome,
      state: 'dormant',
      tags: [...definition.tags],
      hooks: [...definition.hooks]
    };
    if (isBlockingProp(prop)) {
      floor.props.push(prop);
      const keepsExitReachable = hasPassablePath(floor, floor.start, floor.exit);
      const keepsObjectiveReachable = objectiveTargets(floor).some(target => canReachObjectiveWithProps(floor, target));
      floor.props.pop();
      if (!keepsExitReachable || !keepsObjectiveReachable) {
        const debug = runtime.diagnostics.at(-1);
        if (debug) {
          delete debug.selected;
          debug.diagnostics.push(`placement prop:${definition.id}: rejected because it blocks mandatory pathing`);
        }
        continue;
      }
    }
    floor.props.push(prop);
    occupied.add(indexOf(floor, point.x, point.y));
    if (isBlockingProp(prop)) {
      runtime.reachable = undefined;
      reachable = placementReachability(floor, runtime);
    }
    if (links[definition.id]) anchors.set(definition.id, point);
  }
}
function placeMilestones(floor, runtime) {
  const specs = [{
    id: 'waycache',
    kind: 'waycache',
    rewardKey: 'waycache',
    primary: {
      nodeKinds: ['landmark'],
      minDistance: 5
    },
    legacy: {
      minDistance: 5,
      maxDistance: 18
    }
  }, {
    id: 'boon-teach',
    kind: 'boon',
    rewardKey: 'boon-teach',
    primary: {
      nodeKinds: ['fork'],
      minDistance: 7
    },
    legacy: {
      minDistance: 7,
      maxDistance: 24
    }
  }, {
    id: 'boon-test',
    kind: 'boon',
    rewardKey: 'boon-test',
    primary: {
      edgeModes: ['costly'],
      routeCost: 'costly',
      minDistance: 9
    },
    legacy: {
      minDistance: 10,
      chokepoint: false
    }
  }, {
    id: 'boon-payoff',
    kind: 'boon',
    rewardKey: 'boon-payoff',
    primary: {
      nodeKinds: ['optionalReward'],
      minDistance: 10
    },
    legacy: {
      minDistance: 12
    }
  }, {
    id: 'augment',
    kind: 'augment',
    primary: {
      nodeKinds: ['objective'],
      minDistance: 12
    },
    legacy: {
      minDistance: 14
    }
  }];
  floor.milestones = [];
  for (const spec of specs) {
    var _runtime$diagnostics$;
    const contract = {
      id: `milestone:${spec.id}`,
      requirements: runtime.pilot ? spec.primary : spec.legacy,
      ...(runtime.pilot ? {
        fallback: spec.legacy
      } : {})
    };
    const point = choosePlacement(floor, runtime, contract, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (floor.biome !== 'cliffs' || spec.kind !== 'augment' || floor.tiles.every((tile, index) => tile.kind !== 'altar' || Math.max(Math.abs(candidate.x - index % floor.width), Math.abs(candidate.y - Math.floor(index / floor.width))) > 2)));
    if (!point) throw new Error(`failed placement ${contract.id}: ${(_runtime$diagnostics$ = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$ === void 0 ? void 0 : _runtime$diagnostics$.diagnostics.join('; ')}`);
    floor.milestones.push({
      id: `milestone:${floor.index}:${spec.id}:${point.x}:${point.y}`,
      kind: spec.kind,
      ...(spec.rewardKey ? {
        rewardKey: spec.rewardKey
      } : {}),
      ...point,
      discovered: false,
      claimed: false
    });
  }
}
function placeEncounters(floor, rng, runtime) {
  var _floor$escalation2;
  const contract = runtime.pilot ? {
    id: 'encounter:guarded-shrine',
    requirements: {
      edgeModes: ['costly'],
      routeCost: 'costly',
      minDistance: 8,
      cover: true
    },
    fallback: {
      minDistance: 8,
      terrain: ['floor']
    }
  } : {
    id: 'encounter:guarded-shrine',
    requirements: {
      minDistance: 8,
      terrain: ['floor']
    }
  };
  const point = choosePlacement(floor, runtime, contract, candidate => candidate.x !== floor.exit.x || candidate.y !== floor.exit.y);
  if (!point) return;
  const aligned = {
    mine: ['minePact', 'mineKami'],
    wilds: ['wildsPact', 'wildsKami'],
    caverns: ['cavernsPact', 'cavernsKami'],
    ruins: ['ruinsPact', 'ruinsKami'],
    furnace: ['furnacePact', 'furnaceKami'],
    floodedRuins: ['floodedPact', 'floodedKami'],
    cliffs: ['cliffsPact', 'cliffsKami'],
    burial: ['burialPact', 'burialKami'],
    saltFlats: ['saltPact', 'saltKami'],
    frostReliquary: ['frostPact', 'frostKami']
  };
  const kinds = floor.biome === 'cliffs' ? ['stormCache', 'windTrial', 'cursedObject', ...aligned.cliffs] : floor.biome === 'burial' ? ['ancestorDebt', 'tombAuction', 'cursedObject', ...aligned.burial] : floor.biome === 'saltFlats' ? ['sunTribute', 'mirageMarket', 'brineOath', 'glassTrial', 'whiteRoad', 'saltCache', ...aligned.saltFlats] : floor.biome === 'frostReliquary' ? ['iceDuel', 'winterTithe', 'rimeContract', 'frostCache', 'whiteout', 'reliquaryTrial', ...aligned.frostReliquary] : ['wayfarer', 'bloodBargain', 'shiftingChamber', 'oathwell', 'cursedObject', ...aligned[floor.biome]];
  const social = socialContractFor({
    seed: floor.seed,
    floorIndex: floor.index,
    biome: floor.biome,
    recipeId: floor.layoutId,
    arcId: (_floor$escalation2 = floor.escalation) === null || _floor$escalation2 === void 0 ? void 0 : _floor$escalation2.arcId
  });
  const toolOffer = optionalTerrainToolFor({
    seed: floor.seed,
    floorIndex: floor.index,
    biome: floor.biome,
    recipeId: floor.layoutId,
    source: social ? 'social' : 'encounter'
  });
  floor.encounters = [{
    id: `encounter:${floor.index}:${point.x}:${point.y}`,
    kind: social ? 'wayfarer' : rng.pick(kinds),
    ...point,
    state: 'dormant',
    ...(social ? {
      social
    } : {}),
    ...(toolOffer ? {
      toolOffer
    } : {})
  }];
}
const carveH = (floor, from, to, y) => {
  for (let x = Math.min(from, to); x <= Math.max(from, to); x++) setKind(floor, x, y, 'floor');
};
const carveV = (floor, from, to, x) => {
  for (let y = Math.min(from, to); y <= Math.max(from, to); y++) setKind(floor, x, y, 'floor');
};
const setKind = (floor, x, y, kind) => {
  if (!inBounds(floor, x, y)) return;
  const tile = floor.tiles[indexOf(floor, x, y)];
  tile.kind = kind;
  if (kind !== 'current') delete tile.flow;
};
const safeFloor = floor => floor.tiles.flatMap((current, index) => current.kind === 'floor' ? [pointAt(floor, index)] : []).filter(point => distance(point, floor.start) > 5 && distance(point, floor.exit) > 3);
const terrainCount = (floor, count) => Math.max(count, Math.round(count * floor.tiles.length / (MAP_WIDTH * MAP_HEIGHT)));
const railH = (floor, from, to, y) => {
  for (let x = Math.min(from, to); x <= Math.max(from, to); x++) {
    var _getTile49;
    if (((_getTile49 = getTile(floor, x, y)) === null || _getTile49 === void 0 ? void 0 : _getTile49.kind) === 'floor') setKind(floor, x, y, 'rail');
  }
};
const railV = (floor, from, to, x) => {
  for (let y = Math.min(from, to); y <= Math.max(from, to); y++) {
    var _getTile50;
    if (((_getTile50 = getTile(floor, x, y)) === null || _getTile50 === void 0 ? void 0 : _getTile50.kind) === 'floor') setKind(floor, x, y, 'rail');
  }
};
function decorateBiome(floor, rng, rooms) {
  if (floor.biome === 'mine') decorateMine(floor, rng, rooms);
  if (floor.biome === 'wilds') decorateWilds(floor, rng);
  if (floor.biome === 'caverns') decorateCaverns(floor, rng);
  if (floor.biome === 'ruins') decorateRuins(floor, rng, rooms);
  if (floor.biome === 'furnace') decorateFurnace(floor, rng);
  if (floor.biome === 'floodedRuins') decorateFloodedRuins(floor, rng);
  if (floor.biome === 'cliffs') decorateCliffs(floor, rng);
  if (floor.biome === 'burial') decorateBurial(floor, rng);
  if (floor.biome === 'saltFlats') decorateSaltFlats(floor, rng);
  if (floor.biome === 'frostReliquary') decorateFrostReliquary(floor, rng);
  if (floor.index % 4 === 3) {
    const chamber = rooms[rooms.length - 1];
    for (let y = chamber.y; y < chamber.y + chamber.h; y++) for (let x = chamber.x; x < chamber.x + chamber.w; x++) setKind(floor, x, y, 'floor');
    setKind(floor, floor.exit.x, floor.exit.y, 'exit');
  }
}
function placePuzzleTemplate(floor, rng, rooms) {
  var _floor$puzzleIds;
  const templates = puzzleTemplatesFor(floor.biome);
  if (!templates.length) return;
  const template = rng.pick(templates);
  const room = rng.pick(rooms.slice(1, -1).length ? rooms.slice(1, -1) : rooms);
  const point = center(room);
  for (const placement of template.placements) {
    const tile = getTile(floor, point.x + placement.dx, point.y + placement.dy);
    if (tile && tile.kind !== 'wall' && tile.kind !== 'exit') {
      tile.kind = placement.kind;
      if (placement.kind !== 'current') delete tile.flow;
    }
  }
  floor.puzzleIds = [...((_floor$puzzleIds = floor.puzzleIds) !== null && _floor$puzzleIds !== void 0 ? _floor$puzzleIds : []), template.id];
}
function decorateMine(floor, rng, rooms) {
  for (let i = 1; i < rooms.length; i++) {
    const from = center(rooms[i - 1]);
    const to = center(rooms[i]);
    if (i % 2) {
      railH(floor, from.x, to.x, from.y);
      railV(floor, from.y, to.y, to.x);
    } else {
      railV(floor, from.y, to.y, from.x);
      railH(floor, from.x, to.x, to.y);
    }
  }
  const safe = () => safeFloor(floor);
  const paint = (kind, count, byRail = false) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    if (byRail) {
      const adjacent = candidates.filter(point => [[0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => {
        var _getTile51;
        return ((_getTile51 = getTile(floor, point.x + x, point.y + y)) === null || _getTile51 === void 0 ? void 0 : _getTile51.kind) === 'rail';
      }));
      if (adjacent.length) candidates = adjacent;
    }
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      candidates = candidates.filter(candidate => candidate.x !== point.x || candidate.y !== point.y);
    }
  };
  paint('support', 8, true);
  paint('crumble', 12);
  paint('rubble', 6);
  paint('boulder', 5);
}
function decorateWilds(floor, rng) {
  const safe = () => safeFloor(floor);
  const paint = (kind, count, clustered = false) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      if (clustered) for (const [x, y] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) {
        var _getTile52;
        if (rng.chance(45) && ((_getTile52 = getTile(floor, point.x + x, point.y + y)) === null || _getTile52 === void 0 ? void 0 : _getTile52.kind) === 'floor') setKind(floor, point.x + x, point.y + y, kind);
      }
      candidates = safe();
    }
  };
  paint('water', 9, true);
  if (floor.layoutId.includes('river-clearings') || floor.layoutId.includes('wetland')) carveFlowChannel(floor, rng, false);
  paint('bramble', 12, true);
  paint('boulder', 7, true);
  paint('web', 10);
}
function decorateCaverns(floor, rng) {
  const safe = () => safeFloor(floor);
  const paint = (kind, count, clustered = false) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      if (clustered) for (const [x, y] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) {
        var _getTile53;
        if (rng.chance(40) && ((_getTile53 = getTile(floor, point.x + x, point.y + y)) === null || _getTile53 === void 0 ? void 0 : _getTile53.kind) === 'floor') setKind(floor, point.x + x, point.y + y, kind);
      }
      candidates = safe();
    }
  };
  carveFlowChannel(floor, rng, false);
  paint('water', 11, true);
  paint('deepWater', 7, true);
  paint('crumble', 6);
  paint('darkness', 11, true);
}
function decorateRuins(floor, rng, rooms) {
  const safe = () => safeFloor(floor);
  const paint = (kind, count) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      candidates = safe();
    }
  };
  paint('dart', 12);
  paint('crumble', 10);
  paint('boulder', 5);
  const ritualRoom = rooms.length > 2 ? rooms[rooms.length - 2] : undefined;
  if (!ritualRoom) return;
  const altar = center(ritualRoom);
  for (let y = altar.y - 1; y <= altar.y + 1; y++) for (let x = altar.x - 1; x <= altar.x + 1; x++) {
    var _getTile54;
    if (((_getTile54 = getTile(floor, x, y)) === null || _getTile54 === void 0 ? void 0 : _getTile54.kind) !== 'wall') setKind(floor, x, y, 'floor');
  }
  setKind(floor, altar.x, altar.y, 'altar');
}
function decorateFurnace(floor, rng) {
  const safe = () => safeFloor(floor);
  const paint = (kind, count, clustered = false) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      if (clustered) for (const [x, y] of cardinalOffsets) {
        var _getTile55;
        if (rng.chance(35) && ((_getTile55 = getTile(floor, point.x + x, point.y + y)) === null || _getTile55 === void 0 ? void 0 : _getTile55.kind) === 'floor') setKind(floor, point.x + x, point.y + y, kind);
      }
      candidates = safe();
    }
  };
  paint('smoke', 14, true);
  paint('lift', 7);
  paint('breakwall', 8);
  paint('fireVent', 9);
}
function decorateFloodedRuins(floor, rng) {
  const safe = () => safeFloor(floor);
  const paint = (kind, count, clustered = false) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      if (clustered) for (const [x, y] of cardinalOffsets) {
        var _getTile56;
        if (rng.chance(35) && ((_getTile56 = getTile(floor, point.x + x, point.y + y)) === null || _getTile56 === void 0 ? void 0 : _getTile56.kind) === 'floor') setKind(floor, point.x + x, point.y + y, kind);
      }
      candidates = safe();
    }
  };
  if (!['braided-current-delta', 'anchor-gated-ruin', 'island-hop-network'].some(recipe => floor.layoutId.includes(recipe))) carveFlowChannel(floor, rng, floor.layoutId.includes('floodgate'));
  paint('anchor', 7);
  paint('deepWater', 9, true);
  paint('water', 8, true);
}
const carveFlowChannel = (floor, rng, hazardous) => {
  const vertical = rng.chance(55);
  const start = vertical ? rng.int(4, floor.width - 5) : rng.int(4, floor.height - 5);
  const direction = vertical ? 's' : 'e';
  const length = vertical ? floor.height - 3 : floor.width - 3;
  let bend = start;
  let last;
  for (let step = 1; step < length; step++) {
    var _getTile57;
    if (step % 8 === 0 && rng.chance(55)) bend += rng.int(-1, 1);
    const x = vertical ? bend : step;
    const y = vertical ? step : bend;
    if (!inBounds(floor, x, y) || x < 2 || y < 2 || x >= floor.width - 2 || y >= floor.height - 2) continue;
    const current = getTile(floor, x, y);
    if (!current || current.kind === 'exit' || x === floor.start.x && y === floor.start.y) continue;
    if (last) {
      const previous = getTile(floor, last.x, last.y);
      if (previous !== null && previous !== void 0 && previous.flow) previous.flow.direction = flowDirection(x - last.x, y - last.y);
    }
    current.kind = 'current';
    current.flow = {
      direction,
      ...(hazardous && step > length - 7 ? {
        hazard: 'undertow'
      } : {})
    };
    last = {
      x,
      y
    };
    const bank = vertical ? {
      x: x + 1,
      y
    } : {
      x,
      y: y + 1
    };
    if (((_getTile57 = getTile(floor, bank.x, bank.y)) === null || _getTile57 === void 0 ? void 0 : _getTile57.kind) === 'floor' && rng.chance(45)) setKind(floor, bank.x, bank.y, 'water');
  }
  if (hazardous && last) {
    var _getTile$flow$directi, _getTile58;
    const delta = {
      n: {
        x: 0,
        y: -1
      },
      ne: {
        x: 1,
        y: -1
      },
      e: {
        x: 1,
        y: 0
      },
      se: {
        x: 1,
        y: 1
      },
      s: {
        x: 0,
        y: 1
      },
      sw: {
        x: -1,
        y: 1
      },
      w: {
        x: -1,
        y: 0
      },
      nw: {
        x: -1,
        y: -1
      }
    }[(_getTile$flow$directi = (_getTile58 = getTile(floor, last.x, last.y)) === null || _getTile58 === void 0 || (_getTile58 = _getTile58.flow) === null || _getTile58 === void 0 ? void 0 : _getTile58.direction) !== null && _getTile$flow$directi !== void 0 ? _getTile$flow$directi : direction];
    const outlet = getTile(floor, last.x + delta.x, last.y + delta.y);
    if (outlet && outlet.kind !== 'exit') outlet.kind = 'deepWater';
  }
};
const flowDirection = (x, y) => x < 0 ? y < 0 ? 'nw' : y > 0 ? 'sw' : 'w' : x > 0 ? y < 0 ? 'ne' : y > 0 ? 'se' : 'e' : y < 0 ? 'n' : 's';
function decorateCliffs(floor, rng) {
  if (['switchback-face', 'ravine-bridge-loop', 'anchor-chain'].some(variant => floor.layoutId === variant || floor.layoutId === `${variant}-remix`)) return;
  const safe = () => safeFloor(floor);
  const paint = (kind, count) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      candidates = safe();
    }
  };
  paint('ledge', 12);
  paint('cliffWall', 10);
  paint('rope', 4);
  const lower = safe();
  if (!lower.length) return;
  const from = rng.pick(lower);
  const far = lower.filter(point => distance(point, from) > 8);
  const to = rng.pick(far.length ? far : lower);
  getTile(floor, from.x, from.y).elevation = 0;
  getTile(floor, to.x, to.y).elevation = 1;
  floor.climbLinks = [{
    id: `climb:${floor.index}:${from.x}:${from.y}:${to.x}:${to.y}`,
    lower: from,
    upper: to,
    anchored: false
  }];
}
function decorateBurial(floor, rng) {
  if (['stone-circle-center', 'mound-procession', 'cemetery-settlement-edge', 'ossuary-hollow', 'ancestor-path-loop'].some(variant => floor.layoutId === variant || floor.layoutId === `${variant}-remix`)) return;
  const safe = () => safeFloor(floor);
  const paint = (kind, count, clustered = false) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      if (clustered) for (const [x, y] of cardinalOffsets) {
        var _getTile59;
        if (rng.chance(35) && ((_getTile59 = getTile(floor, point.x + x, point.y + y)) === null || _getTile59 === void 0 ? void 0 : _getTile59.kind) === 'floor') setKind(floor, point.x + x, point.y + y, kind);
      }
      candidates = safe();
    }
  };
  paint('graveSoil', 13, true);
  paint('cairn', 8);
  paint('ossuary', 7);
  paint('spiritPath', 10, true);
}
function decorateSaltFlats(floor, rng) {
  if (['crust-island-chain', 'brine-maze', 'caravan-causeway', 'mirror-basin-loop', 'salt-ridge-refuge'].some(variant => floor.layoutId === variant || floor.layoutId === `${variant}-remix`)) return;
  const safe = () => safeFloor(floor);
  const paint = (kind, count, clustered = false) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      if (clustered) for (const [x, y] of cardinalOffsets) {
        var _getTile60;
        if (rng.chance(35) && ((_getTile60 = getTile(floor, point.x + x, point.y + y)) === null || _getTile60 === void 0 ? void 0 : _getTile60.kind) === 'floor') setKind(floor, point.x + x, point.y + y, kind);
      }
      candidates = safe();
    }
  };
  paint('saltMirror', 14, true);
  paint('brine', 8, true);
  paint('crumble', 6);
}
function decorateFrostReliquary(floor, rng) {
  if (['frozen-lake-crossing', 'ridge-hollow-loop', 'pressure-crack-maze', 'shore-reliquary-route', 'storm-refuge-chain'].some(variant => floor.layoutId === variant || floor.layoutId === `${variant}-remix`)) return;
  const safe = () => safeFloor(floor);
  const paint = (kind, count, clustered = false) => {
    count = terrainCount(floor, count);
    let candidates = safe();
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates);
      setKind(floor, point.x, point.y, kind);
      if (clustered) for (const [x, y] of cardinalOffsets) {
        var _getTile61;
        if (rng.chance(35) && ((_getTile61 = getTile(floor, point.x + x, point.y + y)) === null || _getTile61 === void 0 ? void 0 : _getTile61.kind) === 'floor') setKind(floor, point.x + x, point.y + y, kind);
      }
      candidates = safe();
    }
  };
  paint('ice', 15, true);
  paint('frostRime', 9, true);
  paint('boulder', 4);
}
function placeEvents(floor, rooms, runtime) {
  var _runtime$diagnostics$2;
  const eventRoom = rooms[floor.biome === 'ruins' && rooms.length > 2 ? 1 : Math.max(1, Math.floor(rooms.length / 2))];
  const nodeKinds = floor.biome === 'ruins' ? floor.index % 4 === 0 ? ['landmark'] : floor.index % 4 === 1 ? ['fork'] : floor.index % 4 === 2 ? ['optionalReward'] : ['landmark'] : floor.index % 4 === 0 ? ['landmark'] : floor.index % 4 === 1 ? ['fork'] : ['objective'];
  const node = runtime.pilot ? runtime.macro.nodes.find(candidate => nodeKinds.includes(candidate.kind)) : undefined;
  const fallback = node ? {
    x: node.footprint.x + Math.floor(node.footprint.width / 2),
    y: node.footprint.y + Math.floor(node.footprint.height / 2)
  } : center(eventRoom);
  const point = choosePlacement(floor, runtime, {
    id: `event:${floor.index}`,
    requirements: runtime.pilot ? {
      nodeKinds,
      minDistance: 5,
      near: fallback,
      nearDistance: 0
    } : {
      near: fallback,
      nearDistance: 3
    },
    ...(runtime.pilot ? {
      fallback: {
        near: fallback,
        nearDistance: 3
      }
    } : {})
  });
  if (!point) throw new Error(`failed placement event:${floor.index}: ${(_runtime$diagnostics$2 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$2 === void 0 ? void 0 : _runtime$diagnostics$2.diagnostics.join('; ')}`);
  const kind = floor.index % 4 === 0 ? 'shop' : floor.index % 4 === 1 ? 'rescue' : floor.index % 4 === 2 ? 'altar' : 'shop';
  setKind(floor, point.x, point.y, kind);
  if (kind === 'shop') floor.actors.push(friendly('merchant', `${floor.biome} trader`, point, '$', '#f4d26a'));
  if (kind === 'rescue') floor.actors.push(friendly('ally', 'stranded traveler', point, '&', '#8ae0b3'));
  if (kind === 'altar') floor.actors.push(friendly('ally', 'shrine keeper', point, '_', '#d6a8eb'));
}
function placeDoorsAndLocks(floor, rng, rooms) {
  let placedRuinsLock = false;
  for (const room of rooms.slice(1, -1)) {
    var _getTile62;
    const point = center(room);
    const door = {
      x: Math.max(1, point.x - Math.floor(room.w / 2)),
      y: point.y
    };
    if (((_getTile62 = getTile(floor, door.x, door.y)) === null || _getTile62 === void 0 ? void 0 : _getTile62.kind) === 'floor') {
      const locked = floor.biome === 'ruins' ? !placedRuinsLock || rng.chance(65) : rng.chance(25);
      setKind(floor, door.x, door.y, locked ? 'lockedDoor' : 'door');
      if (locked) placedRuinsLock = true;
    }
  }
}
const routeThroughLocks = floor => {
  const start = {
    ...floor.start
  };
  const queue = [start];
  const previous = new Map([[pointKey(start), undefined]]);
  const traversable = kind => kind !== 'wall' && kind !== 'cliffWall';
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const point = queue[cursor];
    if (point.x === floor.exit.x && point.y === floor.exit.y) {
      const path = [];
      for (let current = point; current; current = previous.get(pointKey(current))) path.push(current);
      return path.reverse();
    }
    for (const [x, y] of cardinalOffsets) {
      var _getTile$kind4, _getTile63;
      const next = {
        x: point.x + x,
        y: point.y + y
      };
      const key = pointKey(next);
      if (previous.has(key) || !traversable((_getTile$kind4 = (_getTile63 = getTile(floor, next.x, next.y)) === null || _getTile63 === void 0 ? void 0 : _getTile63.kind) !== null && _getTile$kind4 !== void 0 ? _getTile$kind4 : 'wall')) continue;
      previous.set(key, point);
      queue.push(next);
    }
  }
  return undefined;
};
const openMandatoryLocks = floor => {
  if (hasPassablePath(floor, floor.start, floor.exit)) return;
  const route = routeThroughLocks(floor);
  if (!route) return;
  for (const point of route) {
    const tile = getTile(floor, point.x, point.y);
    if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'lockedDoor') tile.kind = 'door';
  }
};
const repairMandatoryPath = floor => {
  if (hasPassablePath(floor, floor.start, floor.exit)) return;
  openMandatoryLocks(floor);
  if (hasPassablePath(floor, floor.start, floor.exit)) return;
  const trace = traverseFloor(floor, floor.start, {
    target: floor.exit
  });
  throw new Error(`bounded route repair refused to flatten terrain: blockers=${trace.blockers.join('|')}`);
};
function placeContainers(floor, rng, rooms, reserved = new Set()) {
  for (let i = 0; i < 4; i++) {
    const kind = i === 3 ? 'chest' : 'crate';
    for (let attempt = 0; attempt < 80; attempt++) {
      const point = freeRoomPoint(floor, rng, rooms, reserved);
      const target = getTile(floor, point.x, point.y);
      const exits = cardinalOffsets.filter(([x, y]) => {
        var _getTile$kind5, _getTile64;
        return passable((_getTile$kind5 = (_getTile64 = getTile(floor, point.x + x, point.y + y)) === null || _getTile64 === void 0 ? void 0 : _getTile64.kind) !== null && _getTile$kind5 !== void 0 ? _getTile$kind5 : 'wall');
      }).length;
      if (!target || target.kind !== 'floor' || exits < 2) continue;
      target.kind = kind;
      break;
    }
  }
}
function placeActors(floor, rng, runtime) {
  var _floor$difficulty$rou, _floor$difficulty;
  const definitions = MONSTERS.filter(monster => monster.biome === floor.biome);
  const regular = definitions.filter(monster => monster.ai !== 'guardian' && monster.spawn !== 'triggered');
  const areaFloor = floor.index % 4;
  const routePosition = (_floor$difficulty$rou = (_floor$difficulty = floor.difficulty) === null || _floor$difficulty === void 0 ? void 0 : _floor$difficulty.routePosition) !== null && _floor$difficulty$rou !== void 0 ? _floor$difficulty$rou : 0;
  const directed = [];
  if (areaFloor !== 3) for (const [groupIndex, plan] of encounterPlansFor({
    biome: floor.biome,
    areaFloor,
    routePosition,
    pilot: runtime.pilot,
    arcOffset: (_floor$escalation3 = floor.escalation) === null || _floor$escalation3 === void 0 ? void 0 : _floor$escalation3.encounterOffset
  }, nativeActorTerrain[floor.biome]).entries()) {
    var _floor$escalation3;
    const id = `tactical:${floor.index}:${groupIndex}:${plan.archetype}`;
    let leader;
    for (let member = 0; member < membersForEncounter(areaFloor, routePosition); member++) {
      var _floor$difficulty$eli, _floor$difficulty2;
      if (member > 0 && !leader) break;
      const definition = definitionForEncounter(regular, plan, member, nativeActorTerrain[floor.biome]);
      if (!definition) continue;
      const affinity = plan.archetype === 'nativeTerrainPack' && member === 0 ? terrainAffinityFor(definition).filter(kind => nativeActorTerrain[floor.biome].includes(kind)) : undefined;
      const requirements = member === 0 ? affinity !== null && affinity !== void 0 && affinity.length ? {
        ...plan.requirements,
        terrain: affinity
      } : plan.requirements : {
        minDistance: 7,
        chokepoint: false,
        near: leader,
        nearDistance: 5
      };
      const fallback = member === 0 ? affinity !== null && affinity !== void 0 && affinity.length ? {
        ...plan.fallback,
        terrain: affinity
      } : plan.fallback : {
        minDistance: 7,
        chokepoint: false,
        near: leader,
        nearDistance: 8
      };
      const contract = {
        id: `actor:${id}:${member}:${definition.id}`,
        requirements,
        fallback
      };
      const point = choosePlacement(floor, runtime, contract, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
      if (!point) continue;
      const encounter = {
        id,
        archetype: plan.archetype,
        leader: member === 0,
        answer: plan.answer
      };
      const actor = spawnMonster(definition.id, point, `${definition.id}-${groupIndex}-${member}`, floor.difficulty);
      actor.encounter = encounter;
      if (member === 0 && (rng.chance((_floor$difficulty$eli = (_floor$difficulty2 = floor.difficulty) === null || _floor$difficulty2 === void 0 ? void 0 : _floor$difficulty2.eliteChance) !== null && _floor$difficulty$eli !== void 0 ? _floor$difficulty$eli : 0) || floor.biome === 'frostReliquary' && areaFloor >= 1)) {
        var _actor$status;
        actor.maxHealth = Math.round(actor.maxHealth * 1.25);
        actor.health = actor.maxHealth;
        actor.attack += 2;
        actor.status = [...((_actor$status = actor.status) !== null && _actor$status !== void 0 ? _actor$status : []), 'elite'];
      }
      floor.actors.push(actor);
      directed.push(encounter);
      leader !== null && leader !== void 0 ? leader : leader = point;
    }
  }
  if (floor.biome === 'mine' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit, _getTile$kind6, _getTile65;
    return actor.hostile && ((_actor$terrainAffinit = actor.terrainAffinity) === null || _actor$terrainAffinit === void 0 ? void 0 : _actor$terrainAffinit.includes((_getTile$kind6 = (_getTile65 = getTile(floor, actor.x, actor.y)) === null || _getTile65 === void 0 ? void 0 : _getTile65.kind) !== null && _getTile$kind6 !== void 0 ? _getTile$kind6 : 'wall'));
  })) {
    var _runtime$diagnostics$3;
    const railguard = definitions.find(definition => definition.id === 'railguard');
    const terrain = railguard ? terrainAffinityFor(railguard).filter(kind => nativeActorTerrain.mine.includes(kind)) : [];
    if (!railguard || !terrain.length) throw new Error('Mine rail patrol lacks terrain affinity');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:rail-patrol:railguard`,
      requirements: {
        terrain,
        minDistance: 8
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Mine rail patrol: ${(_runtime$diagnostics$3 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$3 === void 0 ? void 0 : _runtime$diagnostics$3.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:rail-patrol`,
      archetype: 'nativeTerrainPack',
      leader: true,
      answer: 'leave the enemy\'s native terrain'
    };
    const actor = spawnMonster(railguard.id, point, `${railguard.id}-rail-patrol`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'caverns' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit2, _getTile$kind7, _getTile66;
    return actor.hostile && ((_actor$terrainAffinit2 = actor.terrainAffinity) === null || _actor$terrainAffinit2 === void 0 ? void 0 : _actor$terrainAffinit2.includes((_getTile$kind7 = (_getTile66 = getTile(floor, actor.x, actor.y)) === null || _getTile66 === void 0 ? void 0 : _getTile66.kind) !== null && _getTile$kind7 !== void 0 ? _getTile$kind7 : 'wall'));
  })) {
    var _runtime$diagnostics$4;
    const tideEel = definitions.find(definition => definition.id === 'fumeeel');
    const terrain = tideEel ? terrainAffinityFor(tideEel).filter(kind => nativeActorTerrain.caverns.includes(kind)) : [];
    if (!tideEel || !terrain.length) throw new Error('Caverns tide patrol lacks terrain affinity');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:tide-patrol:fumeeel`,
      requirements: {
        terrain,
        minDistance: 8
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Caverns tide patrol: ${(_runtime$diagnostics$4 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$4 === void 0 ? void 0 : _runtime$diagnostics$4.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:tide-patrol`,
      archetype: 'nativeTerrainPack',
      leader: true,
      answer: 'leave the enemy\'s native terrain'
    };
    const actor = spawnMonster(tideEel.id, point, `${tideEel.id}-tide-patrol`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'wilds' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit3, _getTile$kind8, _getTile67;
    return actor.hostile && ((_actor$terrainAffinit3 = actor.terrainAffinity) === null || _actor$terrainAffinit3 === void 0 ? void 0 : _actor$terrainAffinit3.includes((_getTile$kind8 = (_getTile67 = getTile(floor, actor.x, actor.y)) === null || _getTile67 === void 0 ? void 0 : _getTile67.kind) !== null && _getTile$kind8 !== void 0 ? _getTile$kind8 : 'wall'));
  })) {
    var _runtime$diagnostics$5;
    const webweaver = definitions.find(definition => definition.id === 'webweaver');
    const terrain = webweaver ? terrainAffinityFor(webweaver).filter(kind => nativeActorTerrain.wilds.includes(kind)) : [];
    if (!webweaver || !terrain.length) throw new Error('Wilds web ambush lacks terrain affinity');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:web-ambush:webweaver`,
      requirements: {
        terrain,
        minDistance: 8
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Wilds web ambush: ${(_runtime$diagnostics$5 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$5 === void 0 ? void 0 : _runtime$diagnostics$5.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:web-ambush`,
      archetype: 'nativeTerrainPack',
      leader: true,
      answer: 'leave the enemy\'s native terrain'
    };
    const actor = spawnMonster(webweaver.id, point, `${webweaver.id}-web-ambush`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'ruins' && areaFloor !== 3 && !floor.actors.some(actor => actor.kind === 'wardacolyte')) {
    var _runtime$diagnostics$6;
    const acolyte = definitions.find(definition => definition.id === 'wardacolyte');
    const terrain = acolyte ? terrainAffinityFor(acolyte).filter(kind => nativeActorTerrain.ruins.includes(kind)) : [];
    if (!acolyte || !terrain.length) throw new Error('Ruins ward post lacks terrain affinity');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:ward-post:wardacolyte`,
      requirements: {
        terrain,
        minDistance: 8
      },
      fallback: {
        terrain: ['floor'],
        minDistance: 8
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Ruins ward post: ${(_runtime$diagnostics$6 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$6 === void 0 ? void 0 : _runtime$diagnostics$6.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:ward-post`,
      archetype: 'objectiveDefense',
      leader: true,
      answer: 'break line of sight and interrupt the ward'
    };
    const actor = spawnMonster(acolyte.id, point, `${acolyte.id}-ward-post`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'furnace' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit4, _getTile$kind9, _getTile68;
    return actor.kind === 'cinderling' && ((_actor$terrainAffinit4 = actor.terrainAffinity) === null || _actor$terrainAffinit4 === void 0 ? void 0 : _actor$terrainAffinit4.includes((_getTile$kind9 = (_getTile68 = getTile(floor, actor.x, actor.y)) === null || _getTile68 === void 0 ? void 0 : _getTile68.kind) !== null && _getTile$kind9 !== void 0 ? _getTile$kind9 : 'wall'));
  })) {
    var _runtime$diagnostics$7;
    const cinderling = definitions.find(definition => definition.id === 'cinderling');
    const terrain = cinderling ? terrainAffinityFor(cinderling).filter(kind => kind === 'fireVent') : [];
    if (!cinderling || !terrain.length) throw new Error('Furnace firing post lacks heat affinity');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:firing-post:cinderling`,
      requirements: {
        terrain,
        minDistance: 8
      },
      fallback: {
        terrain,
        minDistance: 5
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Furnace firing post: ${(_runtime$diagnostics$7 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$7 === void 0 ? void 0 : _runtime$diagnostics$7.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:firing-post`,
      archetype: 'objectiveDefense',
      leader: true,
      answer: 'take the lift lane and quench the firing vent'
    };
    const actor = spawnMonster(cinderling.id, point, `${cinderling.id}-firing-post`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'floodedRuins' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit5, _getTile$kind0, _getTile69;
    return actor.kind === 'tidewraith' && ((_actor$terrainAffinit5 = actor.terrainAffinity) === null || _actor$terrainAffinit5 === void 0 ? void 0 : _actor$terrainAffinit5.includes((_getTile$kind0 = (_getTile69 = getTile(floor, actor.x, actor.y)) === null || _getTile69 === void 0 ? void 0 : _getTile69.kind) !== null && _getTile$kind0 !== void 0 ? _getTile$kind0 : 'wall'));
  })) {
    var _runtime$diagnostics$8;
    const tidewraith = definitions.find(definition => definition.id === 'tidewraith');
    const terrain = tidewraith ? terrainAffinityFor(tidewraith).filter(kind => kind === 'current') : [];
    if (!tidewraith || !terrain.length) throw new Error('Flooded current patrol lacks flow affinity');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:current-patrol:tidewraith`,
      requirements: {
        terrain,
        minDistance: 8
      },
      fallback: {
        terrain,
        minDistance: 5
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Flooded current patrol: ${(_runtime$diagnostics$8 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$8 === void 0 ? void 0 : _runtime$diagnostics$8.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:current-patrol`,
      archetype: 'nativeTerrainPack',
      leader: true,
      answer: 'hold the anchor and leave the current'
    };
    const actor = spawnMonster(tidewraith.id, point, `${tidewraith.id}-current-patrol`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'cliffs' && areaFloor === 0 && !floor.actors.some(actor => {
    var _getTile70;
    return actor.kind === 'stormCrow' && ((_getTile70 = getTile(floor, actor.x, actor.y)) === null || _getTile70 === void 0 ? void 0 : _getTile70.kind) === 'ledge';
  })) {
    var _runtime$diagnostics$9;
    const stormCrow = definitions.find(definition => definition.id === 'stormCrow');
    if (!stormCrow) throw new Error('Cliffs wind patrol lacks storm crow');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:wind-patrol:stormCrow`,
      requirements: {
        terrain: ['ledge'],
        nodeKinds: ['optionalReward'],
        minDistance: 8,
        cover: false
      },
      fallback: {
        terrain: ['ledge'],
        minDistance: 5
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Cliffs wind patrol: ${(_runtime$diagnostics$9 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$9 === void 0 ? void 0 : _runtime$diagnostics$9.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:wind-patrol`,
      archetype: 'artilleryCover',
      leader: true,
      answer: 'break the sightline or take the anchored high route'
    };
    const actor = spawnMonster(stormCrow.id, point, `${stormCrow.id}-wind-patrol`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'burial' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit6, _getTile$kind1, _getTile71;
    return actor.kind === 'tombWarden' && ((_actor$terrainAffinit6 = actor.terrainAffinity) === null || _actor$terrainAffinit6 === void 0 ? void 0 : _actor$terrainAffinit6.includes((_getTile$kind1 = (_getTile71 = getTile(floor, actor.x, actor.y)) === null || _getTile71 === void 0 ? void 0 : _getTile71.kind) !== null && _getTile$kind1 !== void 0 ? _getTile$kind1 : 'wall'));
  })) {
    var _runtime$diagnostics$0;
    const tombWarden = definitions.find(definition => definition.id === 'tombWarden');
    if (!tombWarden) throw new Error('Burial procession lacks tomb warden');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:procession-warden:tombWarden`,
      requirements: {
        terrain: ['graveSoil'],
        edgeModes: ['costly', 'optional'],
        optional: true,
        minDistance: 8
      },
      fallback: {
        terrain: ['graveSoil'],
        minDistance: 5
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Burial procession warden: ${(_runtime$diagnostics$0 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$0 === void 0 ? void 0 : _runtime$diagnostics$0.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:procession-warden`,
      archetype: 'nativeTerrainPack',
      leader: true,
      answer: 'leave the grave lane or follow the lit spirit path'
    };
    const actor = spawnMonster(tombWarden.id, point, `${tombWarden.id}-procession-warden`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'saltFlats' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit7, _getTile$kind10, _getTile72;
    return actor.kind === 'mirageSkirmisher' && ((_actor$terrainAffinit7 = actor.terrainAffinity) === null || _actor$terrainAffinit7 === void 0 ? void 0 : _actor$terrainAffinit7.includes((_getTile$kind10 = (_getTile72 = getTile(floor, actor.x, actor.y)) === null || _getTile72 === void 0 ? void 0 : _getTile72.kind) !== null && _getTile$kind10 !== void 0 ? _getTile$kind10 : 'wall'));
  })) {
    var _runtime$diagnostics$1;
    const mirageSkirmisher = definitions.find(definition => definition.id === 'mirageSkirmisher');
    if (!mirageSkirmisher) throw new Error('Salt horizon lacks mirage skirmisher');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:mirror-horizon:mirageSkirmisher`,
      requirements: {
        terrain: ['saltMirror'],
        nodeKinds: ['optionalReward'],
        minDistance: 8,
        cover: false
      },
      fallback: {
        terrain: ['saltMirror'],
        minDistance: 5
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Salt mirror horizon: ${(_runtime$diagnostics$1 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$1 === void 0 ? void 0 : _runtime$diagnostics$1.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:mirror-horizon`,
      archetype: 'artilleryCover',
      leader: true,
      answer: 'break the mirror sightline or reach the caravan refuge'
    };
    const actor = spawnMonster(mirageSkirmisher.id, point, `${mirageSkirmisher.id}-mirror-horizon`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'frostReliquary' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit8, _getTile$kind11, _getTile73;
    return actor.kind === 'shardHound' && ((_actor$terrainAffinit8 = actor.terrainAffinity) === null || _actor$terrainAffinit8 === void 0 ? void 0 : _actor$terrainAffinit8.includes((_getTile$kind11 = (_getTile73 = getTile(floor, actor.x, actor.y)) === null || _getTile73 === void 0 ? void 0 : _getTile73.kind) !== null && _getTile$kind11 !== void 0 ? _getTile$kind11 : 'wall'));
  })) {
    var _runtime$diagnostics$10;
    const shardHound = definitions.find(definition => definition.id === 'shardHound');
    if (!shardHound) throw new Error('Frost Basin lacks shard hound');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:ice-pursuit:shardHound`,
      requirements: {
        terrain: ['ice'],
        edgeModes: ['costly', 'optional'],
        optional: true,
        minDistance: 8,
        cover: false
      },
      fallback: {
        terrain: ['ice'],
        minDistance: 5
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Frost ice pursuit: ${(_runtime$diagnostics$10 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$10 === void 0 ? void 0 : _runtime$diagnostics$10.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:ice-pursuit`,
      archetype: 'pursuitLane',
      leader: true,
      answer: 'leave the ice lane or reach the marked wind shelter'
    };
    const actor = spawnMonster(shardHound.id, point, `${shardHound.id}-ice-pursuit`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.biome === 'frostReliquary' && areaFloor !== 3 && !floor.actors.some(actor => {
    var _actor$terrainAffinit9, _getTile$kind12, _getTile74;
    return actor.kind === 'whiteoutOracle' && ((_actor$terrainAffinit9 = actor.terrainAffinity) === null || _actor$terrainAffinit9 === void 0 ? void 0 : _actor$terrainAffinit9.includes((_getTile$kind12 = (_getTile74 = getTile(floor, actor.x, actor.y)) === null || _getTile74 === void 0 ? void 0 : _getTile74.kind) !== null && _getTile$kind12 !== void 0 ? _getTile$kind12 : 'wall'));
  })) {
    var _runtime$diagnostics$11;
    const whiteoutOracle = definitions.find(definition => definition.id === 'whiteoutOracle');
    if (!whiteoutOracle) throw new Error('Frost Basin lacks whiteout oracle');
    const point = choosePlacement(floor, runtime, {
      id: `actor:tactical:${floor.index}:whiteout-watch:whiteoutOracle`,
      requirements: {
        terrain: ['ice'],
        edgeModes: ['costly', 'optional'],
        optional: true,
        minDistance: 8,
        cover: false
      },
      fallback: {
        terrain: ['ice'],
        minDistance: 5
      }
    }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y));
    if (!point) throw new Error(`failed placement Frost whiteout watch: ${(_runtime$diagnostics$11 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$11 === void 0 ? void 0 : _runtime$diagnostics$11.diagnostics.join('; ')}`);
    const encounter = {
      id: `tactical:${floor.index}:whiteout-watch`,
      archetype: 'artilleryCover',
      leader: true,
      answer: 'follow the marked shore and break the oracle sightline'
    };
    const actor = spawnMonster(whiteoutOracle.id, point, `${whiteoutOracle.id}-whiteout-watch`, floor.difficulty);
    actor.encounter = encounter;
    floor.actors.push(actor);
    directed.push(encounter);
  }
  if (floor.index % 4 === 3) {
    var _actor$status2, _floor$escalation$arc, _floor$escalation4;
    const guardian = definitions.find(monster => monster.ai === 'guardian');
    const actor = spawnMonster(guardian.id, floor.exit, `${guardian.id}-99`, floor.difficulty);
    actor.status = [...((_actor$status2 = actor.status) !== null && _actor$status2 !== void 0 ? _actor$status2 : []), `arc:${(_floor$escalation$arc = (_floor$escalation4 = floor.escalation) === null || _floor$escalation4 === void 0 ? void 0 : _floor$escalation4.arcId) !== null && _floor$escalation$arc !== void 0 ? _floor$escalation$arc : 'legacy'}:resolved`];
    floor.actors.push(actor);
  }
  tacticalEncounterDebugs.set(floor, directed);
}
function placeEcology(floor, runtime) {
  var _runtime$diagnostics$12, _runtime$macro$nodes$, _runtime$macro$edges$, _source$id;
  const profile = ecologyProfileFor(floor.biome);
  const pressureRoute = (floor.biome === 'caverns' || floor.biome === 'wilds' || floor.biome === 'ruins' || floor.biome === 'furnace' || floor.biome === 'floodedRuins' || floor.biome === 'cliffs' || floor.biome === 'burial' || floor.biome === 'saltFlats' || floor.biome === 'frostReliquary') && runtime.pilot;
  const contract = pressureRoute ? {
    id: `ecology:${profile.kind}`,
    requirements: {
      terrain: floor.biome === 'ruins' ? ['dart'] : floor.biome === 'furnace' ? ['smoke'] : floor.biome === 'floodedRuins' ? ['current'] : floor.biome === 'cliffs' ? ['ledge'] : floor.biome === 'burial' ? ['graveSoil'] : floor.biome === 'saltFlats' ? ['saltMirror'] : floor.biome === 'frostReliquary' ? ['ice'] : ['water'],
      edgeModes: ['costly', 'optional'],
      optional: true,
      minDistance: 7
    },
    fallback: {
      terrain: floor.biome === 'ruins' ? ['dart'] : floor.biome === 'furnace' ? ['smoke'] : floor.biome === 'floodedRuins' ? ['current'] : floor.biome === 'cliffs' ? ['ledge'] : floor.biome === 'burial' ? ['graveSoil'] : floor.biome === 'saltFlats' ? ['saltMirror'] : floor.biome === 'frostReliquary' ? ['ice'] : ['water'],
      minDistance: 7
    }
  } : {
    id: `ecology:${profile.kind}`,
    requirements: {
      terrain: profile.terrain,
      minDistance: 7,
      chokepoint: false
    },
    fallback: {
      terrain: ['floor'],
      minDistance: 7,
      chokepoint: false
    }
  };
  const point = choosePlacement(floor, runtime, contract, candidate => (candidate.x !== floor.start.x || candidate.y !== floor.start.y) && (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y));
  if (!point) throw new Error(`failed placement ${contract.id}: ${(_runtime$diagnostics$12 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$12 === void 0 ? void 0 : _runtime$diagnostics$12.diagnostics.join('; ')}`);
  const source = floor.actors.find(actor => actor.hostile && (actor.combatRole === 'guard' || actor.combatRole === 'pursuer'));
  const node = runtime.pilot ? (_runtime$macro$nodes$ = runtime.macro.nodes.find(candidate => point.x >= candidate.footprint.x && point.x < candidate.footprint.x + candidate.footprint.width && point.y >= candidate.footprint.y && point.y < candidate.footprint.y + candidate.footprint.height)) === null || _runtime$macro$nodes$ === void 0 ? void 0 : _runtime$macro$nodes$.nodeId : undefined;
  const route = runtime.pilot ? (_runtime$macro$edges$ = runtime.macro.edges.find(edge => edge.cells.some(cell => cell.x === point.x && cell.y === point.y))) === null || _runtime$macro$edges$ === void 0 ? void 0 : _runtime$macro$edges$.modes[0] : undefined;
  const ecology = ecologyEventFor(floor, point, (_source$id = source === null || source === void 0 ? void 0 : source.id) !== null && _source$id !== void 0 ? _source$id : `${floor.biome}:${profile.kind}`, node, route);
  if (floor.biome === 'cliffs' && runtime.pilot) {
    var _getTile75;
    const squall = [{
      direction: 'n',
      x: 0,
      y: -1
    }, {
      direction: 'e',
      x: 1,
      y: 0
    }, {
      direction: 's',
      x: 0,
      y: 1
    }, {
      direction: 'w',
      x: -1,
      y: 0
    }].find(delta => {
      var _target$kind;
      const target = getTile(floor, point.x + delta.x, point.y + delta.y);
      return passable((_target$kind = target === null || target === void 0 ? void 0 : target.kind) !== null && _target$kind !== void 0 ? _target$kind : 'wall') && !(target !== null && target !== void 0 && target.flow);
    });
    if (!squall) throw new Error(`missing Cliffs squall landing: ${floor.layoutId}`);
    const tieOff = getTile(floor, point.x + squall.x, point.y + squall.y);
    tieOff.kind = 'rope';
    tieOff.elevation = (_getTile75 = getTile(floor, point.x, point.y)) === null || _getTile75 === void 0 ? void 0 : _getTile75.elevation;
    delete tieOff.flow;
    ecology.effectFlow = {
      direction: squall.direction,
      hazard: 'squall'
    };
  }
  if (floor.escalation) {
    ecology.warning = `${floor.escalation.ecology}: ${ecology.warning}`;
    ecology.responses = [`${floor.escalation.phase}: ${floor.escalation.promise}`, ...ecology.responses];
    if (floor.escalation.phase === 'climax') {
      ecology.startsAt = Math.max(2, ecology.startsAt - 1);
      ecology.duration++;
    }
  }
  const ecologies = [ecology];
  if (floor.biome === 'burial') for (const [index, procession] of ((_floor$burialLayout$p = (_floor$burialLayout = floor.burialLayout) === null || _floor$burialLayout === void 0 ? void 0 : _floor$burialLayout.processions) !== null && _floor$burialLayout$p !== void 0 ? _floor$burialLayout$p : []).slice(1).entries()) {
    var _floor$burialLayout$p, _floor$burialLayout, _source$id2;
    const target = procession.find(candidate => candidate.x !== point.x || candidate.y !== point.y);
    if (!target) continue;
    const processionEcology = ecologyEventFor(floor, target, (_source$id2 = source === null || source === void 0 ? void 0 : source.id) !== null && _source$id2 !== void 0 ? _source$id2 : `${floor.biome}:${profile.kind}`, node, 'costly');
    processionEcology.startsAt = ecology.startsAt + (index + 1) * 2;
    processionEcology.duration = ecology.duration;
    processionEcology.warning = ecology.warning;
    processionEcology.responses = [...ecology.responses];
    ecologies.push(processionEcology);
  }
  floor.ecology = ecologies;
}
function placeItems(floor, rng, _rooms, runtime) {
  var _floor$difficulty$thr, _floor$difficulty3, _floor$difficulty$rou3, _floor$difficulty5, _floor$difficulty$rew, _floor$difficulty6;
  const valueCap = 105 + ((_floor$difficulty$thr = (_floor$difficulty3 = floor.difficulty) === null || _floor$difficulty3 === void 0 ? void 0 : _floor$difficulty3.threat) !== null && _floor$difficulty$thr !== void 0 ? _floor$difficulty$thr : 0) * 14;
  const eligible = ITEMS.filter(item => {
    var _floor$difficulty$rou2, _floor$difficulty4;
    return item.findable !== false && item.value <= valueCap && (!item.slot || rng.chance(30 + ((_floor$difficulty$rou2 = (_floor$difficulty4 = floor.difficulty) === null || _floor$difficulty4 === void 0 ? void 0 : _floor$difficulty4.routePosition) !== null && _floor$difficulty$rou2 !== void 0 ? _floor$difficulty$rou2 : 0) * 8));
  });
  const loot = eligible.length ? eligible : ITEMS.filter(item => item.findable !== false && (!item.slot || rng.chance(30)));
  const count = Math.round((10 + floor.index % 4 * 2 + Math.floor(((_floor$difficulty$rou3 = (_floor$difficulty5 = floor.difficulty) === null || _floor$difficulty5 === void 0 ? void 0 : _floor$difficulty5.routePosition) !== null && _floor$difficulty$rou3 !== void 0 ? _floor$difficulty$rou3 : 0) / 2)) * ((_floor$difficulty$rew = (_floor$difficulty6 = floor.difficulty) === null || _floor$difficulty6 === void 0 ? void 0 : _floor$difficulty6.rewardMultiplier) !== null && _floor$difficulty$rew !== void 0 ? _floor$difficulty$rew : 1));
  const tool = optionalTerrainToolFor({
    seed: floor.seed,
    floorIndex: floor.index,
    biome: floor.biome,
    recipeId: floor.layoutId,
    source: 'loot'
  });
  for (let i = 0; i < count; i++) {
    var _runtime$diagnostics$13;
    const contract = runtime.pilot && i > 0 ? {
      id: `loot:${i}`,
      requirements: {
        nodeKinds: ['optionalReward'],
        minDistance: 7
      },
      fallback: {
        terrain: ['floor'],
        minDistance: 4
      }
    } : {
      id: `loot:${i}`,
      requirements: {
        terrain: ['floor'],
        minDistance: 4
      }
    };
    const point = choosePlacement(floor, runtime, contract, candidate => candidate.x !== floor.exit.x || candidate.y !== floor.exit.y);
    if (!point) throw new Error(`failed placement ${contract.id}: ${(_runtime$diagnostics$13 = runtime.diagnostics.at(-1)) === null || _runtime$diagnostics$13 === void 0 ? void 0 : _runtime$diagnostics$13.diagnostics.join('; ')}`);
    const id = i === 0 && floor.index % 4 === 0 ? 'key' : tool && i === count - 1 ? 'rock' : rng.pick(loot).id;
    floor.items.push({
      id,
      x: point.x,
      y: point.y,
      count: 1,
      ...(tool && i === count - 1 ? {
        tool
      } : {})
    });
  }
}
function freeRoomPoint(floor, rng, rooms, reserved = new Set()) {
  for (let tries = 0; tries < 200; tries++) {
    const room = rng.pick(rooms);
    const point = {
      x: rng.int(room.x + 1, room.x + room.w - 2),
      y: rng.int(room.y + 1, room.y + room.h - 2)
    };
    const current = getTile(floor, point.x, point.y);
    if ((current === null || current === void 0 ? void 0 : current.kind) === 'floor' && !reserved.has(indexOf(floor, point.x, point.y)) && !actorAt(floor, point.x, point.y) && !floor.items.some(item => item.x === point.x && item.y === point.y) && distance(point, floor.start) > 4) return point;
  }
  return {
    ...floor.start
  };
}
export const spawnMonster = (kind, point, id, difficulty) => {
  var _difficulty$threat, _difficulty$healthMul, _difficulty$attackBon, _difficulty$defenseBo, _definition$tags;
  const definition = monsterById(kind);
  if (!definition) throw new Error(`unknown monster: ${kind}`);
  const healthMultiplier = definition.ai === 'guardian' ? 1 + ((_difficulty$threat = difficulty === null || difficulty === void 0 ? void 0 : difficulty.threat) !== null && _difficulty$threat !== void 0 ? _difficulty$threat : 0) * 0.08 : (_difficulty$healthMul = difficulty === null || difficulty === void 0 ? void 0 : difficulty.healthMultiplier) !== null && _difficulty$healthMul !== void 0 ? _difficulty$healthMul : 1;
  const maxHealth = Math.max(1, Math.round(definition.health * healthMultiplier));
  return {
    id,
    role: definition.ai === 'guardian' ? 'guardian' : 'monster',
    kind: definition.id,
    name: definition.name,
    x: point.x,
    y: point.y,
    health: maxHealth,
    maxHealth,
    attack: definition.attack + ((_difficulty$attackBon = difficulty === null || difficulty === void 0 ? void 0 : difficulty.attackBonus) !== null && _difficulty$attackBon !== void 0 ? _difficulty$attackBon : 0),
    defense: definition.defense + ((_difficulty$defenseBo = difficulty === null || difficulty === void 0 ? void 0 : difficulty.defenseBonus) !== null && _difficulty$defenseBo !== void 0 ? _difficulty$defenseBo : 0),
    speed: definition.speed,
    energy: 0,
    glyph: definition.glyph,
    color: definition.color,
    hostile: true,
    ai: definition.ai,
    combatRole: monsterRoleFor(definition),
    tags: [...((_definition$tags = definition.tags) !== null && _definition$tags !== void 0 ? _definition$tags : [])],
    terrainAffinity: terrainAffinityFor(definition),
    conditions: [],
    ...(definition.ai === 'guardian' ? {
      guardianPhase: 'opening',
      status: difficulty !== null && difficulty !== void 0 && difficulty.guardianPattern ? [`pattern:${difficulty.guardianPattern}`] : []
    } : {})
  };
};
function friendly(role, name, point, glyph, color) {
  return {
    id: `${role}-${pointKey(point)}`,
    role,
    kind: role,
    name,
    x: point.x,
    y: point.y,
    health: 99,
    maxHealth: 99,
    attack: 0,
    defense: 99,
    speed: 0,
    energy: 0,
    glyph,
    color,
    hostile: false,
    conditions: []
  };
}
const distance = (a, b) => Math.abs(a.x - b.x) + Math.abs(a.y - b.y);
const reachableIndexes = floor => reachableFloorIndexes(floor);
const objectiveTargets = floor => {
  if (floor.objective.kind === 'recoverSupplies') return floor.tiles.flatMap((tile, i) => tile.kind === 'crate' || tile.kind === 'chest' ? [pointAt(floor, i)] : []);
  if (floor.objective.kind === 'rescueScout') return floor.tiles.flatMap((tile, i) => tile.kind === 'rescue' ? [pointAt(floor, i)] : []);
  if (floor.objective.kind === 'invokeAltar') return floor.tiles.flatMap((tile, i) => tile.kind === 'altar' ? [pointAt(floor, i)] : []);
  return floor.actors.filter(actor => actor.role === 'guardian').map(actor => ({
    x: actor.x,
    y: actor.y
  }));
};
const canReachObjectiveWithProps = (floor, target) => [[0, 0], [0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => hasPassablePath(floor, floor.start, {
  x: target.x + x,
  y: target.y + y
}));
const containsReachable = (floor, reachable, point) => inBounds(floor, point.x, point.y) && reachable.has(indexOf(floor, point.x, point.y));
const reachesObjective = (floor, reachable, targets) => targets.some(target => [[0, 0], [0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => containsReachable(floor, reachable, {
  x: target.x + x,
  y: target.y + y
})));
export const hasMandatoryCompletionRoute = floor => {
  const reachable = reachableIndexes(floor);
  return containsReachable(floor, reachable, floor.exit) && reachesObjective(floor, reachable, objectiveTargets(floor));
};
export const validateSecretRoutes = floor => {
  var _floor$secretRooms, _floor$secretRoutes, _floor$sideSpaces;
  const errors = [];
  const rooms = (_floor$secretRooms = floor.secretRooms) !== null && _floor$secretRooms !== void 0 ? _floor$secretRooms : [];
  const routes = (_floor$secretRoutes = floor.secretRoutes) !== null && _floor$secretRoutes !== void 0 ? _floor$secretRoutes : [];
  const spaces = (_floor$sideSpaces = floor.sideSpaces) !== null && _floor$sideSpaces !== void 0 ? _floor$sideSpaces : [];
  const roomIds = new Set();
  for (const room of rooms) {
    if (roomIds.has(room.id)) errors.push(`duplicate secret room: ${room.id}`);
    roomIds.add(room.id);
    const source = spaces.find(space => space.id === room.sourceId);
    if (!source || room.id !== `secret-room:${source.id}` || !room.safeFallback || room.version !== 1 || !room.discoveryClue || !room.entries.length || !room.chamber.length) errors.push(`invalid secret room: ${room.id}`);
    if ([room.approach, ...room.entries, ...room.chamber].some(point => !inBounds(floor, point.x, point.y))) errors.push(`out-of-bounds secret room: ${room.id}`);
    if (!floor.items.some(item => item.secretId === room.id)) errors.push(`untagged secret reward: ${room.id}`);
  }
  for (const space of spaces) if (!rooms.some(room => room.sourceId === space.id)) errors.push(`untagged secret content: ${space.id}`);
  const routeIds = new Set();
  for (const route of routes) {
    var _floor$sideSpaces2;
    if (routeIds.has(route.id)) errors.push(`duplicate secret route: ${route.id}`);
    routeIds.add(route.id);
    const room = rooms.find(candidate => candidate.id === route.roomId);
    if (!room || route.version !== 1 || !route.safeFallback || !route.discoveryClue || !inBounds(floor, route.from.x, route.from.y) || !inBounds(floor, route.entry.x, route.entry.y)) errors.push(`invalid secret route: ${route.id}`);
    if (room && (route.from.x !== room.approach.x || route.from.y !== room.approach.y || route.entryCondition !== room.entryCondition || route.discoveryClue !== room.discoveryClue || route.accessMethod !== room.accessMethod || route.risk !== room.risk)) errors.push(`mismatched secret route: ${route.id}`);
    if (route.kind === 'rare-transition' && (!route.destination || route.destination.biome !== floor.biome || route.destination.floor < 0 || route.destination.floor > 3 || route.direction !== 'one-way' && route.direction !== 'two-way' || route.arrival !== 'floor-start' || (route.direction === 'one-way' ? route.returnSemantics !== 'no-return' : route.returnSemantics !== 'return-link'))) errors.push(`invalid secret transition: ${route.id}`);
    if (route.kind === 'concealed-passage' && (!room || !room.entries.some(point => point.x === route.entry.x && point.y === route.entry.y))) errors.push(`unmatched secret route: ${route.id}`);
    if (route.kind === 'rare-transition' && (!room || !((_floor$sideSpaces2 = floor.sideSpaces) !== null && _floor$sideSpaces2 !== void 0 && _floor$sideSpaces2.some(space => {
      var _space$rareTransition, _route$destination, _space$rareTransition2, _route$destination2;
      return space.kind === 'mine-breach-room' && space.id === room.sourceId && ((_space$rareTransition = space.rareTransition) === null || _space$rareTransition === void 0 ? void 0 : _space$rareTransition.targetBiome) === ((_route$destination = route.destination) === null || _route$destination === void 0 ? void 0 : _route$destination.biome) && ((_space$rareTransition2 = space.rareTransition) === null || _space$rareTransition2 === void 0 ? void 0 : _space$rareTransition2.targetFloor) === ((_route$destination2 = route.destination) === null || _route$destination2 === void 0 ? void 0 : _route$destination2.floor);
    })))) errors.push(`unmatched secret transition: ${route.id}`);
  }
  for (const room of rooms) for (const [index, entry] of room.entries.entries()) if (!routes.some(route => route.id === `secret-route:${room.id}:access:${index}` && route.kind === 'concealed-passage' && route.entry.x === entry.x && route.entry.y === entry.y)) errors.push(`missing secret route: ${room.id}:${index}`);
  for (const space of spaces) if (space.kind === 'mine-breach-room' && space.rareTransition && !routes.some(route => {
    var _route$destination3, _space$rareTransition3, _route$destination4, _space$rareTransition4;
    return route.id === `secret-route:secret-room:${space.id}:transition` && route.kind === 'rare-transition' && ((_route$destination3 = route.destination) === null || _route$destination3 === void 0 ? void 0 : _route$destination3.biome) === ((_space$rareTransition3 = space.rareTransition) === null || _space$rareTransition3 === void 0 ? void 0 : _space$rareTransition3.targetBiome) && ((_route$destination4 = route.destination) === null || _route$destination4 === void 0 ? void 0 : _route$destination4.floor) === ((_space$rareTransition4 = space.rareTransition) === null || _space$rareTransition4 === void 0 ? void 0 : _space$rareTransition4.targetFloor);
  })) errors.push(`missing secret transition: ${space.id}`);
  const secretCells = rooms.flatMap(room => [...room.entries, ...room.chamber]);
  if (!secretCells.length) return errors;
  const sealed = new Map();
  for (const point of secretCells) {
    const current = getTile(floor, point.x, point.y);
    if (!current) continue;
    const index = indexOf(floor, point.x, point.y);
    if (!sealed.has(index)) sealed.set(index, {
      kind: current.kind,
      ...(current.flow ? {
        flow: {
          ...current.flow
        }
      } : {})
    });
    current.kind = 'wall';
    delete current.flow;
  }
  if (!hasMandatoryCompletionRoute(floor)) errors.push('secret dependency on campaign completion');
  for (const [index, original] of sealed) {
    const current = floor.tiles[index];
    current.kind = original.kind;
    if (original.flow) current.flow = original.flow;else delete current.flow;
  }
  return errors;
};
export const validateShortcutLandings = floor => {
  var _floor$secretRoutes2;
  return ((_floor$secretRoutes2 = floor.secretRoutes) !== null && _floor$secretRoutes2 !== void 0 ? _floor$secretRoutes2 : []).flatMap(route => {
    if (route.kind !== 'rare-transition' || !route.destination || route.destination.biome !== floor.biome || route.destination.floor < 0 || route.destination.floor > 3) return [];
    try {
      const landing = generateAreaFloor(floor.seed, route.destination.biome, route.destination.floor);
      return isPassable(landing, landing.start.x, landing.start.y) && hasMandatoryCompletionRoute(landing) ? [] : [`unsafe shortcut landing: ${route.id}`];
    } catch {
      return [`unsafe shortcut landing: ${route.id}`];
    }
  });
};
export const validateGeneration = floor => {
  var _floor$puzzleIds2, _getTile76, _floor$rewardOffers2, _floor$encounters$len, _floor$encounters;
  const errors = [];
  errors.push(...validatePuzzleTemplates(), ...validateFloorPuzzles(floor), ...propDefinitionErrors);
  if (puzzleTemplatesFor(floor.biome).length && !((_floor$puzzleIds2 = floor.puzzleIds) !== null && _floor$puzzleIds2 !== void 0 && _floor$puzzleIds2.length)) errors.push('missing puzzle template');
  if (floor.tiles.length !== floor.width * floor.height || floor.width < MAP_WIDTH || floor.height < MAP_HEIGHT || floor.index < 0 || floor.index >= FLOOR_COUNT) errors.push('invalid floor dimensions');
  if (!floor.layoutId || !layoutVariants[floor.biome].some(layout => floor.layoutId === layout || floor.layoutId === `${layout}-remix`)) errors.push('invalid layout id');
  if (!getTile(floor, floor.start.x, floor.start.y) || !passable(getTile(floor, floor.start.x, floor.start.y).kind)) errors.push('invalid start placement');
  if (((_getTile76 = getTile(floor, floor.exit.x, floor.exit.y)) === null || _getTile76 === void 0 ? void 0 : _getTile76.kind) !== 'exit') errors.push('invalid exit placement');
  for (let index = 0; index < floor.tiles.length; index++) {
    const tile = floor.tiles[index];
    if (!tile.flow) continue;
    const point = pointAt(floor, index);
    const delta = {
      n: {
        x: 0,
        y: -1
      },
      ne: {
        x: 1,
        y: -1
      },
      e: {
        x: 1,
        y: 0
      },
      se: {
        x: 1,
        y: 1
      },
      s: {
        x: 0,
        y: 1
      },
      sw: {
        x: -1,
        y: 1
      },
      w: {
        x: -1,
        y: 0
      },
      nw: {
        x: -1,
        y: -1
      }
    }[tile.flow.direction];
    const downstream = getTile(floor, point.x + delta.x, point.y + delta.y);
    const squall = tile.kind === 'ledge' && tile.flow.hazard === 'squall';
    if (tile.kind !== 'current' && !squall || !downstream) errors.push(`invalid flow at ${pointKey(point)}`);
    if (tile.flow.hazard && !squall && (downstream === null || downstream === void 0 ? void 0 : downstream.kind) !== 'current' && (downstream === null || downstream === void 0 ? void 0 : downstream.kind) !== 'deepWater' && (downstream === null || downstream === void 0 ? void 0 : downstream.kind) !== 'brine') errors.push(`unmarked flow outlet at ${pointKey(point)}`);
  }
  const reachable = reachableIndexes(floor);
  if (!containsReachable(floor, reachable, floor.exit)) errors.push('exit unreachable');
  const targets = objectiveTargets(floor);
  if (!targets.length || !reachesObjective(floor, reachable, targets)) errors.push(`objective unreachable: ${floor.objective.kind}`);
  if (floor.milestones.length < 5 || floor.milestones.length > 6) errors.push('invalid milestone count');
  const rewardOffers = (_floor$rewardOffers2 = floor.rewardOffers) !== null && _floor$rewardOffers2 !== void 0 ? _floor$rewardOffers2 : [];
  const expectedRewardOffers = [['waycache', 'waycache'], ['boon-teach', 'boon'], ['boon-test', 'boon'], ['boon-payoff', 'boon']];
  for (const [milestoneId, kind] of expectedRewardOffers) {
    const offer = rewardOffers.find(candidate => candidate.milestoneId === milestoneId);
    if (!offer || offer.kind !== kind || offer.choices.length !== 3 || new Set(offer.choices.map(choice => choice.role)).size !== 3) errors.push(`invalid reward offer: ${milestoneId}`);
    if (!floor.milestones.some(milestone => milestone.rewardKey === milestoneId && milestone.kind === kind)) errors.push(`missing reward milestone: ${milestoneId}`);
  }
  if (!primaryToolUseAvailable(floor, reachable)) errors.push('unusable primary Waycache tool');
  if (((_floor$encounters$len = (_floor$encounters = floor.encounters) === null || _floor$encounters === void 0 ? void 0 : _floor$encounters.length) !== null && _floor$encounters$len !== void 0 ? _floor$encounters$len : 0) !== 1) errors.push('invalid encounter count');
  const milestoneLocations = new Set();
  for (const milestone of floor.milestones) {
    const key = pointKey(milestone);
    const tile = getTile(floor, milestone.x, milestone.y);
    if (!milestone.id || !['waycache', 'boon', 'augment', 'relic'].includes(milestone.kind) || !tile || !passable(tile.kind) || tile.kind === 'exit' && milestone.kind !== 'relic' || !containsReachable(floor, reachable, milestone)) errors.push(`unreachable milestone: ${milestone.id}`);
    if (milestoneLocations.has(key)) errors.push(`overlapping milestone: ${key}`);
    milestoneLocations.add(key);
  }
  for (const encounter of (_floor$encounters2 = floor.encounters) !== null && _floor$encounters2 !== void 0 ? _floor$encounters2 : []) {
    var _floor$encounters2;
    const tile = getTile(floor, encounter.x, encounter.y);
    if (!encounter.id || !['wayfarer', 'bloodBargain', 'shiftingChamber', 'stormCache', 'ancestorDebt', 'cursedObject', 'oathwell', 'windTrial', 'tombAuction', 'sunTribute', 'mirageMarket', 'brineOath', 'glassTrial', 'whiteRoad', 'saltCache', 'iceDuel', 'winterTithe', 'rimeContract', 'frostCache', 'whiteout', 'reliquaryTrial', 'minePact', 'mineKami', 'wildsPact', 'wildsKami', 'cavernsPact', 'cavernsKami', 'ruinsPact', 'ruinsKami', 'furnacePact', 'furnaceKami', 'floodedPact', 'floodedKami', 'cliffsPact', 'cliffsKami', 'burialPact', 'burialKami', 'saltPact', 'saltKami', 'frostPact', 'frostKami'].includes(encounter.kind) || !tile || !passable(tile.kind) || tile.kind === 'exit' || !containsReachable(floor, reachable, encounter)) errors.push(`unreachable encounter: ${encounter.id}`);
  }
  for (const ecology of (_floor$ecology2 = floor.ecology) !== null && _floor$ecology2 !== void 0 ? _floor$ecology2 : []) {
    var _floor$ecology2;
    const tile = getTile(floor, ecology.target.x, ecology.target.y);
    if (!ecology.id || !['tide', 'wind', 'smoke', 'collapse', 'fire', 'migration', 'nesting', 'visibility'].includes(ecology.kind) || !tile || !Number.isInteger(ecology.startsAt) || ecology.startsAt < 1 || !Number.isInteger(ecology.duration) || ecology.duration < 1 || !ecology.warning || !ecology.responses.length || !ecology.cleanup || !['waiting', 'active', 'resolved'].includes(ecology.state)) {
      errors.push(`invalid ecology event: ${ecology.id}`);
      continue;
    }
    const previousKind = tile.kind;
    const previousFlow = tile.flow ? {
      ...tile.flow
    } : undefined;
    tile.kind = ecology.effect;
    if (ecology.effectFlow) tile.flow = {
      ...ecology.effectFlow
    };else delete tile.flow;
    const afterEcology = reachableIndexes(floor);
    const solvable = containsReachable(floor, afterEcology, floor.exit) && reachesObjective(floor, afterEcology, objectiveTargets(floor));
    tile.kind = previousKind;
    if (previousFlow) tile.flow = previousFlow;else delete tile.flow;
    if (!solvable) errors.push(`ecology blocks mandatory path: ${ecology.id}`);
  }
  const placements = [...floor.actors.map(actor => ({
    ...actor,
    type: 'actor'
  })), ...floor.items.map(item => ({
    ...item,
    type: 'item'
  }))];
  const occupied = new Set();
  for (const placement of placements) {
    const tile = getTile(floor, placement.x, placement.y);
    if (!tile || !passable(tile.kind) || tile.kind === 'lockedDoor') errors.push(`illegal ${placement.type} placement`);
    const key = pointKey(placement);
    if (occupied.has(key)) errors.push(`overlapping ${placement.type} placement`);
    occupied.add(key);
  }
  if (!floor.props.length) errors.push('missing props');
  const propIds = new Set();
  const propLocations = new Set();
  for (const prop of floor.props) {
    var _prop$hooks;
    if (propIds.has(prop.id)) errors.push(`duplicate prop id: ${prop.id}`);
    propIds.add(prop.id);
    if (!PROP_IDS.includes(prop.kind)) {
      errors.push(`unknown prop: ${prop.kind}`);
      continue;
    }
    const definition = propDefinition(prop.kind);
    const tile = getTile(floor, prop.x, prop.y);
    const location = pointKey(prop);
    if (propLocations.has(location)) errors.push(`overlapping prop placement: ${location}`);
    propLocations.add(location);
    if (prop.biome !== floor.biome || definition.biome !== floor.biome) errors.push(`invalid prop biome: ${prop.id}`);
    if (!tile || !passable(tile.kind) || tile.kind === 'lockedDoor' || !definition.terrain.includes(tile.kind)) errors.push(`illegal prop placement: ${prop.id}`);
    if (!hasPropContext(floor, prop.kind, prop)) errors.push(`invalid prop context: ${prop.id}`);else if (isBlockingProp(prop)) {
      const reachableSide = [[0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => containsReachable(floor, reachable, {
        x: prop.x + x,
        y: prop.y + y
      }));
      if (!reachableSide) errors.push(`unreachable prop: ${prop.id}`);
    } else if (!reachable.has(indexOf(floor, prop.x, prop.y))) errors.push(`unreachable prop: ${prop.id}`);
    if (!['dormant', 'inspected', 'activated', 'destroyed'].includes(prop.state)) errors.push(`invalid prop state: ${prop.id}`);
    if (!prop.tags.length || !((_prop$hooks = prop.hooks) !== null && _prop$hooks !== void 0 && _prop$hooks.length) || !prop.hooks.includes('operate')) errors.push(`invalid prop hooks: ${prop.id}`);
  }
  errors.push(...validateSecretRoutes(floor), ...validateShortcutLandings(floor));
  for (const error of validateAreaGate(gateForArea(floor.biome))) errors.push(`impossible gate: ${error}`);
  return {
    valid: errors.length === 0,
    errors
  };
};
const routeNodeForFailure = (floor, invariant) => {
  var _route$nodes$find$id, _route$nodes$find;
  const route = routeContractDebug(floor);
  const kind = invariant.includes('secret') || invariant.includes('shortcut') ? 'optionalReward' : invariant.includes('objective') ? 'objective' : invariant.includes('exit') ? 'exit' : invariant.includes('start') ? 'start' : 'fork';
  return (_route$nodes$find$id = route === null || route === void 0 || (_route$nodes$find = route.nodes.find(node => node.kind === kind)) === null || _route$nodes$find === void 0 ? void 0 : _route$nodes$find.id) !== null && _route$nodes$find$id !== void 0 ? _route$nodes$find$id : `floor:${floor.index % 4}:${kind}`;
};
export const generationValidationFailures = (floor, validation = validateGeneration(floor)) => validation.errors.map(invariant => ({
  seed: floor.seed,
  biome: floor.biome,
  floor: floor.index % 4,
  routeNode: routeNodeForFailure(floor, invariant),
  invariant
}));
export const validateFloor = floor => validateGeneration(floor).valid;
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJJVEVNUyIsIk1PTlNURVJTIiwiYmlvbWVGb3JGbG9vciIsIm1vbnN0ZXJCeUlkIiwibW9uc3RlclJvbGVGb3IiLCJ0ZXJyYWluQWZmaW5pdHlGb3IiLCJybmdGb3IiLCJzdHJlYW1TZWVkIiwiRkxPT1JfQ09VTlQiLCJNQVBfSEVJR0hUIiwiTUFQX1dJRFRIIiwiZmxvb3JJbmRleCIsImZsb29yUG9pbnQiLCJpbkZsb29yQm91bmRzIiwib2JqZWN0aXZlRm9yRmxvb3IiLCJnYXRlRm9yQXJlYSIsInZhbGlkYXRlQXJlYUdhdGUiLCJwdXp6bGVUZW1wbGF0ZXNGb3IiLCJ2YWxpZGF0ZUZsb29yUHV6emxlcyIsInZhbGlkYXRlUHV6emxlVGVtcGxhdGVzIiwiaXNCbG9ja2luZ1Byb3AiLCJQUk9QX0lEUyIsInByb3BBdCIsInByb3BEZWZpbml0aW9uIiwicHJvcERlZmluaXRpb25zRm9yIiwidmFsaWRhdGVQcm9wRGVmaW5pdGlvbnMiLCJnZW5lcmF0ZVJvdXRlQ29udHJhY3QiLCJ2YWxpZGF0ZVJvdXRlQ29udHJhY3QiLCJjb21waWxlUm91dGVDb250cmFjdCIsIm1hY3JvQ29ubmVjdG9yUG9pbnRzIiwibWFjcm9SZWNpcGVGb3IiLCJ2YWxpZGF0ZU1hY3JvUmVhbGl6YXRpb24iLCJzZWxlY3RQbGFjZW1lbnQiLCJkZWZpbml0aW9uRm9yRW5jb3VudGVyIiwiZW5jb3VudGVyUGxhbnNGb3IiLCJtZW1iZXJzRm9yRW5jb3VudGVyIiwiZWNvbG9neUV2ZW50Rm9yIiwiZWNvbG9neVByb2ZpbGVGb3IiLCJlc2NhbGF0aW9uRm9yIiwic29jaWFsQ29udHJhY3RGb3IiLCJvcHRpb25hbFRlcnJhaW5Ub29sRm9yIiwicGxhY2VTZWNyZXRNZXRhZGF0YSIsInJlc29sdmVDYW1wYWlnbkRpZmZpY3VsdHkiLCJpbml0aWFsQ2FtcGFpZ25DeWNsZSIsInRpbGUiLCJraW5kIiwiZXhwbG9yZWQiLCJ2aXNpYmxlIiwicG9pbnRLZXkiLCJwb2ludCIsIngiLCJ5IiwicGFzc2FibGUiLCJpbmNsdWRlcyIsInByb3BEZWZpbml0aW9uRXJyb3JzIiwibWFjcm9EZWJ1Z3MiLCJXZWFrTWFwIiwibWFjcm9QaWxvdHMiLCJwbGFjZW1lbnREZWJ1Z3MiLCJyb3V0ZUNvbnRyYWN0RGVidWdzIiwidGFjdGljYWxFbmNvdW50ZXJEZWJ1Z3MiLCJpbmRleE9mIiwiZmxvb3IiLCJwb2ludEF0IiwiaW5kZXgiLCJpbkJvdW5kcyIsImdldFRpbGUiLCJ0aWxlcyIsInVuZGVmaW5lZCIsImFjdG9yQXQiLCJhY3RvcnMiLCJmaW5kIiwiYWN0b3IiLCJoZWFsdGgiLCJpc1Bhc3NhYmxlIiwidGFyZ2V0IiwiQm9vbGVhbiIsInByb3BzIiwidG9vbERpcmVjdGlvbnMiLCJ0b29sSGF6YXJkcyIsIlNldCIsImRyaWxsYWJsZVRvb2xUZXJyYWluIiwicHJpbWFyeVRvb2xVc2VBdmFpbGFibGUiLCJyZWFjaGFibGUiLCJyZWFjaGFibGVGbG9vckluZGV4ZXMiLCJfZmxvb3IkcmV3YXJkT2ZmZXJzIiwiX29mZmVyJGNob2ljZXMkIiwib2ZmZXIiLCJyZXdhcmRPZmZlcnMiLCJjYW5kaWRhdGUiLCJtaWxlc3RvbmVJZCIsInRvb2wiLCJjaG9pY2VzIiwiaWQiLCJvcmlnaW4iLCJkeCIsImR5IiwiZmlyc3QiLCJzZWNvbmQiLCJmaXJzdFRpbGUiLCJoYXMiLCJwYXRoT2Zmc2V0cyIsInRyYXZlcnNhbEJsb2NrZXIiLCJvcHRpb25zIiwiY3VycmVudCIsInByb3AiLCJpZ25vcmVCbG9ja2luZ1Byb3BzIiwiaXNQYXRoUGFzc2FibGUiLCJ0cmF2ZXJzZUZsb29yIiwic3RhcnQiLCJfb3B0aW9ucyRjb2xsZWN0QmxvY2siLCJzdGFydEJsb2NrZXIiLCJibG9ja2VycyIsImNvbGxlY3RCbG9ja2VycyIsInByb3BzQnlJbmRleCIsIk1hcCIsInN0YXRlIiwic2V0IiwiaW5pdGlhbCIsInNlZW4iLCJxdWV1ZSIsInByZXZpb3VzIiwiY3Vyc29yIiwibGVuZ3RoIiwicGF0aCIsImdldCIsInB1c2giLCJyZXZlcnNlIiwic29ydCIsIm5leHRYIiwibmV4dFkiLCJuZXh0SW5kZXgiLCJuZXh0VGlsZSIsImJsb2NrZXIiLCJhZGQiLCJoYXNQYXNzYWJsZVRlcnJhaW5QYXRoIiwiZGVzdGluYXRpb24iLCJoYXNQYXNzYWJsZVBhdGgiLCJtYWNyb1JlY2lwZURlYnVnIiwicGxhY2VtZW50RGVidWciLCJfcGxhY2VtZW50RGVidWdzJGdldCIsInJvdXRlQ29udHJhY3REZWJ1ZyIsImVzY2FsYXRpb25EZWJ1ZyIsImVzY2FsYXRpb24iLCJ0YWN0aWNhbEVuY291bnRlckRlYnVnIiwiX3RhY3RpY2FsRW5jb3VudGVyRGViIiwidmFsaWRhdGVNYWNyb1JlY2lwZSIsImRlYnVnIiwicHJlc2VydmVzRXhpdFBhdGgiLCJwcmVzZXJ2ZXMiLCJleGl0IiwicHJlc2VydmVzQWRqYWNlbnRFeGl0QWNjZXNzIiwiYWRqYWNlbnQiLCJtYXAiLCJmaWx0ZXIiLCJldmVyeSIsImRpZmZpY3VsdHlGb3IiLCJyb3V0ZVBvc2l0aW9uIiwiYXJlYUZsb29yIiwidGhyZWF0IiwiTWF0aCIsIm1heCIsIm1pbiIsImhlYWx0aE11bHRpcGxpZXIiLCJhdHRhY2tCb251cyIsImRlZmVuc2VCb251cyIsImVsaXRlQ2hhbmNlIiwiZ3VhcmRpYW5QYXR0ZXJuIiwiYXNzZXJ0R2VuZXJhdGlvblBoYXNlIiwiY2FtcGFpZ25TZWVkIiwiY29udHJhY3QiLCJwaGFzZSIsInRhcmdldHMiLCJvYmplY3RpdmVUYXJnZXRzIiwicmVhY2hhYmxlSW5kZXhlcyIsIl9ub2RlJGlkIiwidGFyZ2V0UmVhY2hhYmxlIiwic29tZSIsIm5vZGUiLCJub2RlcyIsInRyYWNlIiwiRXJyb3IiLCJiaW9tZSIsImxheW91dElkIiwic2l6ZSIsImpvaW4iLCJnZW5lcmF0ZUZsb29yIiwicnVuU2VlZCIsImRpZmZpY3VsdHkiLCJzZWVkIiwibGF5b3V0Um5nIiwibGF5b3V0Rm9yIiwicm91dGVDb250cmFjdCIsInJlY2lwZUlkIiwiZXNjYWxhdGlvblZhcmlhbnQiLCJhcmNJZCIsInJvdXRlVmFsaWRhdGlvbiIsInZhbGlkIiwiZXJyb3JzIiwid2lkdGgiLCJoZWlnaHQiLCJkaW1lbnNpb25zRm9yIiwibWFjcm8iLCJkaWFnbm9zdGljcyIsInBsYWNlbWVudHMiLCJwaWxvdCIsIm9iamVjdGl2ZSIsIkFycmF5IiwiZnJvbSIsIml0ZW1zIiwiZW5jb3VudGVycyIsImd1YXJkaWFuRGVmZWF0ZWQiLCJsYWJlbCIsInBheW9mZiIsInByb21pc2UiLCJtaWxlc3RvbmVzIiwidGVsZWdyYXBocyIsInJvb21zIiwiY2FydmVSb3V0ZUNvbnRyYWN0TGF5b3V0IiwicmVzZXJ2ZWRNYWNyb0NlbGxzIiwiY2VudGVyIiwic2V0S2luZCIsImRlY29yYXRlQmlvbWUiLCJwbGFjZVB1enpsZVRlbXBsYXRlIiwiaW1wcmludEVzY2FsYXRpb25MYW5kbWFyayIsInJlc3RvcmVNYWNyb0Nvbm5lY3RvcnMiLCJyZXN0b3JlTWFjcm9Ob2RlVHJhbnNpdCIsImltcHJpbnRNaW5lUmFpbFNlcnZpY2VSb3V0ZSIsImltcHJpbnRSdWluc1JpdHVhbENlbnRlciIsInBsYWNlRXZlbnRzIiwicGxhY2VEb29yc0FuZExvY2tzIiwib3Blbk1hbmRhdG9yeUxvY2tzIiwicGxhY2VDb250YWluZXJzIiwiaW1wcmludENhdmVyblRpZGVSb3V0ZSIsImltcHJpbnRXaWxkc1ByZXNzdXJlUm91dGUiLCJpbXByaW50UnVpbnNXYXJkUm91dGUiLCJpbXByaW50RnVybmFjZUZpcmluZ1JvdXRlIiwiaW1wcmludEZsb29kZWRDdXJyZW50TmV0d29yayIsImltcHJpbnRDbGlmZkhlaWdodEdyYXBoIiwiaW1wcmludEJ1cmlhbFJpdHVhbFJvdXRlcyIsImltcHJpbnRTYWx0Um91dGVDb250cmFjdCIsImltcHJpbnRGcm9zdFJvdXRlQ29udHJhY3QiLCJpbXByaW50Q2F2ZXJuU2V0cGllY2VDb250ZXh0IiwicmVwYWlyTWFuZGF0b3J5UGF0aCIsImltcHJpbnRNaW5lQnJlYWNoUm9vbXMiLCJpbXByaW50V2lsZHNDYXZlcyIsImltcHJpbnRDYXZlcm5IaWRkZW5DaGFtYmVycyIsImltcHJpbnRSdWluc0hpZGRlbkNoYW1iZXJzIiwiaW1wcmludEZ1cm5hY2VTZXJ2aWNlU3BhY2VzIiwiaW1wcmludEZsb29kZWRXaGlybHBvb2xzIiwiaW1wcmludENsaWZmQWxjb3ZlcyIsImltcHJpbnRCdXJpYWxDcnlwdHMiLCJpbXByaW50U2FsdE1pcmFnZXMiLCJpbXByaW50RnJvc3RDYXZlcyIsInBsYWNlQWN0b3JzIiwicGxhY2VFY29sb2d5IiwicGxhY2VJdGVtcyIsInBsYWNlUHJvcHMiLCJwbGFjZU1pbGVzdG9uZXMiLCJwbGFjZUVuY291bnRlcnMiLCJtYWNyb0Vycm9ycyIsInZhbGlkYXRpb24iLCJ2YWxpZGF0ZUdlbmVyYXRpb24iLCJhcmVhRmxvb3JJbmRleCIsImdlbmVyYXRlQXJlYUZsb29yIiwiY3ljbGUiLCJOdW1iZXIiLCJpc0ludGVnZXIiLCJyb29tIiwidyIsImgiLCJjYXJkaW5hbE9mZnNldHMiLCJtaW5lSGF6YXJkcyIsImRpbWVuc2lvbnMiLCJtaW5lIiwid2lsZHMiLCJjYXZlcm5zIiwicnVpbnMiLCJmdXJuYWNlIiwiZmxvb2RlZFJ1aW5zIiwiY2xpZmZzIiwiYnVyaWFsIiwic2FsdEZsYXRzIiwiZnJvc3RSZWxpcXVhcnkiLCJsYXlvdXRWYXJpYW50cyIsImRlY2siLCJzaHVmZmxlIiwiZXNjYWxhdGlvbkxhbmRtYXJrVGVycmFpbiIsIl9yb29tcyQiLCJfZmxvb3IkZXNjYWxhdGlvbiRlbmMiLCJfZmxvb3IkZXNjYWxhdGlvbiIsInZhcmlhbnQiLCJlbmNvdW50ZXJPZmZzZXQiLCJoYXNOZWFyYnlUaWxlIiwicmFkaXVzIiwia2luZHMiLCJfZ2V0VGlsZSRraW5kIiwiX2dldFRpbGUiLCJoYXNBZGphY2VudFRpbGUiLCJfZ2V0VGlsZSRraW5kMiIsIl9nZXRUaWxlMiIsImhhc01pbmVQcm9wQ29udGV4dCIsInN0YXJ0c1dpdGgiLCJ3b3JrZWRQYXNzYWdlIiwiX2dldFRpbGUzIiwiaG9zdGlsZSIsImFicyIsImhhc0NhdmVyblByb3BDb250ZXh0IiwibmVhcldhdGVyIiwibmVhckRhcmtuZXNzIiwiaGFzUHJvcENvbnRleHQiLCJjYXJ2ZVJlY3QiLCJ3YWxsQmFuZCIsInZlcnRpY2FsIiwiYXQiLCJnYXAiLCJzcGFuIiwibGltaXQiLCJvZmZzZXQiLCJsYW5kbWFya1Jvb21zIiwiZm9yRWFjaCIsImNhcnZlTWluZVJvdXRlQ29udHJhY3QiLCJybmciLCJyZXBsYWNlIiwibWFpblkiLCJpbnQiLCJ1cHBlclkiLCJsb3dlclkiLCJsYW5kbWFyayIsImZvcmsiLCJzYWZlUm91dGUiLCJyaXNrUm91dGUiLCJyZXdhcmQiLCJjb25uZWN0IiwidG8iLCJ2ZXJ0aWNhbEZpcnN0IiwiY2FydmVWIiwiY2FydmVIIiwiY2FydmVCaW9tZUxheW91dCIsImNvbm5lY3RSb29tcyIsImltcHJpbnRCaW9tZUxhbmRtYXJrcyIsImNhcnZlTGVnYWN5TGF5b3V0RnJvbVJvdXRlQ29udHJhY3QiLCJjYXJ2ZVdpbGRzUm91dGVDb250cmFjdExheW91dCIsImNhcnZlQ2F2ZXJuc1JvdXRlQ29udHJhY3RMYXlvdXQiLCJjYXJ2ZVJ1aW5zUm91dGVDb250cmFjdExheW91dCIsImNhcnZlRnVybmFjZVJvdXRlQ29udHJhY3RMYXlvdXQiLCJjYXJ2ZUZsb29kZWRSb3V0ZUNvbnRyYWN0TGF5b3V0IiwiY2FydmVDbGlmZnNSb3V0ZUNvbnRyYWN0TGF5b3V0IiwiY2FydmVCdXJpYWxSb3V0ZUNvbnRyYWN0TGF5b3V0IiwiY2FydmVTYWx0RmxhdHNSb3V0ZUNvbnRyYWN0TGF5b3V0IiwiY2FydmVGcm9zdFJlbGlxdWFyeVJvdXRlQ29udHJhY3RMYXlvdXQiLCJ0b1Jvb20iLCJmb290cHJpbnQiLCJieUtpbmQiLCJvcmRlcmVkIiwiY2FydmVDbGVhcmluZyIsInJhZGl1c1giLCJyYWRpdXNZIiwiZGlzdGFuY2UiLCJjaGFuY2UiLCJjYXJ2ZVRyYWlsIiwiY2FydmVDaGFtYmVyIiwibGFyZ2UiLCJjZWlsIiwiY2FydmVSdWluc1JpbmciLCJoYWxmV2lkdGgiLCJoYWxmSGVpZ2h0IiwiZ2F0ZXMiLCJnYXRlS2V5cyIsImNhcnZlUml0dWFsQXJjIiwiZ2FwQW5nbGUiLCJyaW5nIiwiYW5nbGUiLCJhdGFuMiIsIlBJIiwiY2FydmVSaXR1YWxBbm5leCIsInBvaW50cyIsIl9ybmciLCJyaXR1YWxOb2RlIiwicml0dWFsIiwiYXBwcm9hY2hOb2RlIiwiYXBwcm9hY2giLCJvdXRlclJpbmciLCJpbm5lclJpbmciLCJhbm5leCIsInJpdHVhbExheW91dCIsImNhcnZlRnVybmFjZVRlcnJhY2VzIiwibGV2ZWxzIiwiY2FydmVGbG9vZGVkQ2hhbm5lbHMiLCJjbGlmZkJhbmQiLCJkZXB0aCIsImNsaWZmUmlkZ2UiLCJzdGVwcyIsInN0ZXAiLCJyb3VuZCIsImxhdGVyYWwiLCJjYXJ2ZUNsaWZmQ29udG91cnMiLCJ0ZXJyYWNlcyIsInNsaWNlIiwic3BpbmVzIiwicGFpbnRCdXJpYWxNb3VuZCIsInBhaW50QnVyaWFsUHJvY2Vzc2lvbiIsImNhcnZlQnVyaWFsTGFuZHNjYXBlIiwiY2x1c3RlciIsInBhaW50U2FsdExpbmUiLCJwYWludFNhbHRQYXRjaCIsImxlZnQiLCJ0b3AiLCJjYXJ2ZVNhbHRMYW5kc2NhcGUiLCJpc2xhbmQiLCJyaWdodCIsImJvdHRvbSIsInBhaW50RnJvc3RTaGVsZiIsInBhaW50RnJvc3RDcmFjayIsImNhcnZlRnJvc3RMYW5kc2NhcGUiLCJyZXNlcnZlZCIsImVuZHBvaW50cyIsImVkZ2VzIiwiZmxhdE1hcCIsImVkZ2UiLCJfIiwiZW5kcG9pbnQiLCJyb3V0ZSIsIm1pbmVCcmVhY2hEaXJlY3Rpb25zIiwiY3Jvc3MiLCJicmVhY2hDaGFtYmVyIiwiZGlyZWN0aW9uIiwiZW50cnkiLCJjaGFtYmVyIiwiZm9yd2FyZCIsIl9nZXRUaWxlNCIsImNoYW1iZXJJbmRleGVzIiwibmVpZ2hib3IiLCJkZXNpcmVkIiwiY2FuZGlkYXRlcyIsInNpZGVTcGFjZXMiLCJfZ2V0VGlsZTUiLCJyZXdhcmRQb2ludCIsImNvdW50IiwidmlzaWJsZUluRm9nIiwidHJhbnNpdGlvbiIsInRhcmdldEJpb21lIiwidGFyZ2V0Rmxvb3IiLCJyYXJlVHJhbnNpdGlvbiIsImNhdmVzIiwiX2dldFRpbGU2IiwiY2hhbWJlcnMiLCJfZ2V0VGlsZTciLCJfZ2V0VGlsZTgiLCJ3YXRlckhpbnQiLCJoaW50VGlsZSIsImZsb3ciLCJmbG93RGlyZWN0aW9uIiwibWFjcm9DZWxscyIsImJhcnJpZXIiLCJmaW5kSW5kZXgiLCJvdGhlciIsImNoYW5nZWQiLCJfZ2V0VGlsZTkiLCJiZWZvcmUiLCJzcGFjZXMiLCJfZ2V0VGlsZTAiLCJtb2RlcyIsImNlbGxzIiwiX2dldFRpbGUxIiwiX3JvdXRlIiwibmV4dCIsImZsb29kIiwiY2F2ZXJuQ2VsbHMiLCJfZ2V0VGlsZTEwIiwiX3NhZmUkY2VsbHMkc2xpY2UkZmlsIiwiX2dldFRpbGUxMSIsInNhZmUiLCJfZ2V0VGlsZTEyIiwibG93ZXIiLCJ1cHBlciIsImVsZXZhdGlvbiIsImNsaW1iTGlua3MiLCJhbmNob3JlZCIsIl9nZXRUaWxlMTMiLCJjb3N0bHkiLCJ0cmFuc2l0IiwiX2dldFRpbGUxNCIsImxpZnRMYW5lIiwiZmlyaW5nTGFuZSIsIl9nZXRUaWxlMTUiLCJfZ2V0VGlsZTE2IiwiaGVhdE5ldHdvcmsiLCJhbmNob3IiLCJfZ2V0VGlsZTE3IiwiZXhpc3RpbmciLCJraWxuIiwiZnVybmFjZUxheW91dCIsIl9nZXRUaWxlMjAiLCJyZWZ1Z2UiLCJfZ2V0VGlsZTE4IiwicmVmdWdlUm91dGUiLCJjdXJyZW50Um91dGUiLCJoYXphcmQiLCJvdXRsZXQiLCJiYW5rIiwiX2dldFRpbGUxOSIsIm91dGxldFRpbGUiLCJjZW50ZXJzIiwid2hpcmxwb29scyIsIl9nZXRUaWxlMjEiLCJfZ2V0VGlsZTIyIiwiX2dldFRpbGUyMyIsIndoaXJscG9vbCIsImNlbGwiLCJfZ2V0VGlsZTI0IiwiaGlnaFJvdXRlIiwibG93Um91dGUiLCJvY2N1cGllZCIsIm1pZExlZGdlcyIsIm92ZXJsb29rIiwicGVyY2giLCJfZ2V0VGlsZTI1IiwicGVyY2hUaWxlIiwibmVhcmVzdCIsInVzZWQiLCJsaW5rIiwicG9vbHMiLCJsaW5rcyIsInBhaXIiLCJ3aW5kQ29ycmlkb3JzIiwiY29ycmlkb3IiLCJ0aWVPZmYiLCJzaGVsdGVyZWRQb2NrZXRzIiwiX2dldFRpbGUyNiIsImNsaWZmTGF5b3V0IiwiaGlnaFJpZGdlIiwiYWxjb3ZlcyIsIl9nZXRUaWxlMjciLCJfZ2V0VGlsZTI4IiwiZW50cnlUaWxlIiwiY3J5cHRzIiwicmVqb2luIiwicmVqb2luQXBwcm9hY2giLCJwcm90ZWN0ZWRQb2ludHMiLCJfZ2V0VGlsZTI5IiwiYXBwcm9hY2hlcyIsImVudHJpZXMiLCJzcGlyaXRSb3V0ZSIsIl9nZXRUaWxlMzAiLCJncmF2ZVJvdXRlIiwiX2dldFRpbGUzMSIsInByb2Nlc3Npb25zIiwic2hlbHRlcnMiLCJidXJpYWxMYXlvdXQiLCJtb3VuZHMiLCJtaXJhZ2VzIiwibWFya2VyIiwiX2dldFRpbGUzMiIsIm1pcmFnZSIsInJldmVhbGVkIiwic2FsdE1pcmFnZXMiLCJfZ2V0VGlsZTMzIiwiZnJvc3RMYXlvdXQiLCJzaGVsdmVzIiwiY3JhY2tzIiwiX2dldFRpbGUzNCIsIl9nZXRUaWxlMzUiLCJvZmZlcmluZyIsIl9nZXRUaWxlMzYiLCJjYWNoZSIsIl9nZXRUaWxlMzciLCJyZWZ1Z2VOb2RlIiwiX2dldFRpbGUzOCIsInN0YWJsZVJvdXRlIiwiYnJpbmVSb3V0ZSIsImhvcml6b24iLCJfZ2V0VGlsZTM5IiwiaHVzayIsInRhZ3MiLCJob29rcyIsIm1hcmtlclBvaW50IiwiX2dldFRpbGU0MCIsInNob3JlUm91dGUiLCJpY2VSb3V0ZSIsInByZXNzdXJlIiwiX2dldFRpbGU0MSIsInBsYWNlIiwiZGVmaW5pdGlvbiIsIl9nZXRUaWxlNDIiLCJuZWFyYnkiLCJfZ2V0VGlsZTQzIiwiZGFya25lc3MiLCJ3YXRlciIsInBvb2wiLCJmdW5ndXMiLCJwYWludCIsIl9nZXRUaWxlNDQiLCJfcm9vbXMkMiIsImxha2UiLCJpIiwibmF0aXZlQWN0b3JUZXJyYWluIiwicGxhY2VtZW50Q29udGV4dCIsInJ1bnRpbWUiLCJlbGlnaWJsZSIsIm5vZGVLaW5kcyIsImVkZ2VNb2RlcyIsIl9ub2RlS2luZHMkZ2V0Iiwia2V5IiwiX2VkZ2VNb2RlcyRnZXQiLCJibG9ja2VkIiwiX2Zsb29yJGVjb2xvZ3kiLCJpdGVtIiwibWlsZXN0b25lIiwiZWNvbG9neSIsInRlcnJhaW5BdCIsIl9nZXRUaWxlNDUiLCJwYXNzYWJsZUF0IiwiYmxvY2tlZEF0IiwiZGlzdGFuY2VGcm9tU3RhcnQiLCJ2aXNpYmxlQXQiLCJfZ2V0VGlsZTQ2IiwiY292ZXJlZEF0IiwiX2dldFRpbGUka2luZDMiLCJfZ2V0VGlsZTQ3IiwiY2hva2Vwb2ludEF0IiwiYWRqYWNlbnRUZXJyYWluQXQiLCJfZ2V0VGlsZTQ4Iiwibm9kZUtpbmRzQXQiLCJfbm9kZUtpbmRzJGdldDIiLCJlZGdlTW9kZXNBdCIsIl9lZGdlTW9kZXMkZ2V0MiIsInBsYWNlbWVudFJlYWNoYWJpbGl0eSIsIl9ydW50aW1lJHJlYWNoYWJsZSIsImNob29zZVBsYWNlbWVudCIsInNlbGVjdGlvbiIsIl9lbGlnaWJsZSIsImNvbXBhbmlvbnMiLCJPYmplY3QiLCJ2YWx1ZXMiLCJkZWZpbml0aW9ucyIsImFuY2hvcnMiLCJfT2JqZWN0JGVudHJpZXMkZmluZCIsImFuY2hvcklkIiwiY29tcGFuaW9uIiwicmFua2VkIiwidXNlZEZhbGxiYWNrIiwicmVxdWlyZW1lbnRzIiwidGVycmFpbiIsImNhcnQiLCJtaW5EaXN0YW5jZSIsIm5lYXIiLCJuZWFyRGlzdGFuY2UiLCJrZWVwc0V4aXRSZWFjaGFibGUiLCJrZWVwc09iamVjdGl2ZVJlYWNoYWJsZSIsImNhblJlYWNoT2JqZWN0aXZlV2l0aFByb3BzIiwicG9wIiwic2VsZWN0ZWQiLCJzcGVjcyIsInJld2FyZEtleSIsInByaW1hcnkiLCJsZWdhY3kiLCJtYXhEaXN0YW5jZSIsInJvdXRlQ29zdCIsImNob2tlcG9pbnQiLCJzcGVjIiwiX3J1bnRpbWUkZGlhZ25vc3RpY3MkIiwiZmFsbGJhY2siLCJkaXNjb3ZlcmVkIiwiY2xhaW1lZCIsIl9mbG9vciRlc2NhbGF0aW9uMiIsImNvdmVyIiwiYWxpZ25lZCIsInNvY2lhbCIsInRvb2xPZmZlciIsInNvdXJjZSIsInBpY2siLCJzYWZlRmxvb3IiLCJ0ZXJyYWluQ291bnQiLCJyYWlsSCIsIl9nZXRUaWxlNDkiLCJyYWlsViIsIl9nZXRUaWxlNTAiLCJkZWNvcmF0ZU1pbmUiLCJkZWNvcmF0ZVdpbGRzIiwiZGVjb3JhdGVDYXZlcm5zIiwiZGVjb3JhdGVSdWlucyIsImRlY29yYXRlRnVybmFjZSIsImRlY29yYXRlRmxvb2RlZFJ1aW5zIiwiZGVjb3JhdGVDbGlmZnMiLCJkZWNvcmF0ZUJ1cmlhbCIsImRlY29yYXRlU2FsdEZsYXRzIiwiZGVjb3JhdGVGcm9zdFJlbGlxdWFyeSIsIl9mbG9vciRwdXp6bGVJZHMiLCJ0ZW1wbGF0ZXMiLCJ0ZW1wbGF0ZSIsInBsYWNlbWVudCIsInB1enpsZUlkcyIsImJ5UmFpbCIsIl9nZXRUaWxlNTEiLCJjbHVzdGVyZWQiLCJfZ2V0VGlsZTUyIiwiY2FydmVGbG93Q2hhbm5lbCIsIl9nZXRUaWxlNTMiLCJyaXR1YWxSb29tIiwiYWx0YXIiLCJfZ2V0VGlsZTU0IiwiX2dldFRpbGU1NSIsIl9nZXRUaWxlNTYiLCJyZWNpcGUiLCJoYXphcmRvdXMiLCJiZW5kIiwibGFzdCIsIl9nZXRUaWxlNTciLCJfZ2V0VGlsZSRmbG93JGRpcmVjdGkiLCJfZ2V0VGlsZTU4IiwiZGVsdGEiLCJuIiwibmUiLCJlIiwic2UiLCJzIiwic3ciLCJudyIsImZhciIsIl9nZXRUaWxlNTkiLCJfZ2V0VGlsZTYwIiwiX2dldFRpbGU2MSIsIl9ydW50aW1lJGRpYWdub3N0aWNzJDIiLCJldmVudFJvb20iLCJmcmllbmRseSIsInBsYWNlZFJ1aW5zTG9jayIsIl9nZXRUaWxlNjIiLCJkb29yIiwibG9ja2VkIiwicm91dGVUaHJvdWdoTG9ja3MiLCJ0cmF2ZXJzYWJsZSIsIl9nZXRUaWxlJGtpbmQ0IiwiX2dldFRpbGU2MyIsImF0dGVtcHQiLCJmcmVlUm9vbVBvaW50IiwiZXhpdHMiLCJfZ2V0VGlsZSRraW5kNSIsIl9nZXRUaWxlNjQiLCJfZmxvb3IkZGlmZmljdWx0eSRyb3UiLCJfZmxvb3IkZGlmZmljdWx0eSIsIm1vbnN0ZXIiLCJyZWd1bGFyIiwiYWkiLCJzcGF3biIsImRpcmVjdGVkIiwiZ3JvdXBJbmRleCIsInBsYW4iLCJhcmNPZmZzZXQiLCJfZmxvb3IkZXNjYWxhdGlvbjMiLCJhcmNoZXR5cGUiLCJsZWFkZXIiLCJtZW1iZXIiLCJfZmxvb3IkZGlmZmljdWx0eSRlbGkiLCJfZmxvb3IkZGlmZmljdWx0eTIiLCJhZmZpbml0eSIsImVuY291bnRlciIsImFuc3dlciIsInNwYXduTW9uc3RlciIsIl9hY3RvciRzdGF0dXMiLCJtYXhIZWFsdGgiLCJhdHRhY2siLCJzdGF0dXMiLCJfYWN0b3IkdGVycmFpbkFmZmluaXQiLCJfZ2V0VGlsZSRraW5kNiIsIl9nZXRUaWxlNjUiLCJ0ZXJyYWluQWZmaW5pdHkiLCJfcnVudGltZSRkaWFnbm9zdGljcyQzIiwicmFpbGd1YXJkIiwiX2FjdG9yJHRlcnJhaW5BZmZpbml0MiIsIl9nZXRUaWxlJGtpbmQ3IiwiX2dldFRpbGU2NiIsIl9ydW50aW1lJGRpYWdub3N0aWNzJDQiLCJ0aWRlRWVsIiwiX2FjdG9yJHRlcnJhaW5BZmZpbml0MyIsIl9nZXRUaWxlJGtpbmQ4IiwiX2dldFRpbGU2NyIsIl9ydW50aW1lJGRpYWdub3N0aWNzJDUiLCJ3ZWJ3ZWF2ZXIiLCJfcnVudGltZSRkaWFnbm9zdGljcyQ2IiwiYWNvbHl0ZSIsIl9hY3RvciR0ZXJyYWluQWZmaW5pdDQiLCJfZ2V0VGlsZSRraW5kOSIsIl9nZXRUaWxlNjgiLCJfcnVudGltZSRkaWFnbm9zdGljcyQ3IiwiY2luZGVybGluZyIsIl9hY3RvciR0ZXJyYWluQWZmaW5pdDUiLCJfZ2V0VGlsZSRraW5kMCIsIl9nZXRUaWxlNjkiLCJfcnVudGltZSRkaWFnbm9zdGljcyQ4IiwidGlkZXdyYWl0aCIsIl9nZXRUaWxlNzAiLCJfcnVudGltZSRkaWFnbm9zdGljcyQ5Iiwic3Rvcm1Dcm93IiwiX2FjdG9yJHRlcnJhaW5BZmZpbml0NiIsIl9nZXRUaWxlJGtpbmQxIiwiX2dldFRpbGU3MSIsIl9ydW50aW1lJGRpYWdub3N0aWNzJDAiLCJ0b21iV2FyZGVuIiwib3B0aW9uYWwiLCJfYWN0b3IkdGVycmFpbkFmZmluaXQ3IiwiX2dldFRpbGUka2luZDEwIiwiX2dldFRpbGU3MiIsIl9ydW50aW1lJGRpYWdub3N0aWNzJDEiLCJtaXJhZ2VTa2lybWlzaGVyIiwiX2FjdG9yJHRlcnJhaW5BZmZpbml0OCIsIl9nZXRUaWxlJGtpbmQxMSIsIl9nZXRUaWxlNzMiLCJfcnVudGltZSRkaWFnbm9zdGljcyQxMCIsInNoYXJkSG91bmQiLCJfYWN0b3IkdGVycmFpbkFmZmluaXQ5IiwiX2dldFRpbGUka2luZDEyIiwiX2dldFRpbGU3NCIsIl9ydW50aW1lJGRpYWdub3N0aWNzJDExIiwid2hpdGVvdXRPcmFjbGUiLCJfYWN0b3Ikc3RhdHVzMiIsIl9mbG9vciRlc2NhbGF0aW9uJGFyYyIsIl9mbG9vciRlc2NhbGF0aW9uNCIsImd1YXJkaWFuIiwiX3J1bnRpbWUkZGlhZ25vc3RpY3MkMTIiLCJfcnVudGltZSRtYWNybyRub2RlcyQiLCJfcnVudGltZSRtYWNybyRlZGdlcyQiLCJfc291cmNlJGlkIiwicHJvZmlsZSIsInByZXNzdXJlUm91dGUiLCJjb21iYXRSb2xlIiwibm9kZUlkIiwiX2dldFRpbGU3NSIsInNxdWFsbCIsIl90YXJnZXQka2luZCIsImVmZmVjdEZsb3ciLCJ3YXJuaW5nIiwicmVzcG9uc2VzIiwic3RhcnRzQXQiLCJkdXJhdGlvbiIsImVjb2xvZ2llcyIsInByb2Nlc3Npb24iLCJfZmxvb3IkYnVyaWFsTGF5b3V0JHAiLCJfZmxvb3IkYnVyaWFsTGF5b3V0IiwiX3NvdXJjZSRpZDIiLCJwcm9jZXNzaW9uRWNvbG9neSIsIl9yb29tcyIsIl9mbG9vciRkaWZmaWN1bHR5JHRociIsIl9mbG9vciRkaWZmaWN1bHR5MyIsIl9mbG9vciRkaWZmaWN1bHR5JHJvdTMiLCJfZmxvb3IkZGlmZmljdWx0eTUiLCJfZmxvb3IkZGlmZmljdWx0eSRyZXciLCJfZmxvb3IkZGlmZmljdWx0eTYiLCJ2YWx1ZUNhcCIsIl9mbG9vciRkaWZmaWN1bHR5JHJvdTIiLCJfZmxvb3IkZGlmZmljdWx0eTQiLCJmaW5kYWJsZSIsInZhbHVlIiwic2xvdCIsImxvb3QiLCJyZXdhcmRNdWx0aXBsaWVyIiwiX3J1bnRpbWUkZGlhZ25vc3RpY3MkMTMiLCJ0cmllcyIsIl9kaWZmaWN1bHR5JHRocmVhdCIsIl9kaWZmaWN1bHR5JGhlYWx0aE11bCIsIl9kaWZmaWN1bHR5JGF0dGFja0JvbiIsIl9kaWZmaWN1bHR5JGRlZmVuc2VCbyIsIl9kZWZpbml0aW9uJHRhZ3MiLCJyb2xlIiwibmFtZSIsImRlZmVuc2UiLCJzcGVlZCIsImVuZXJneSIsImdseXBoIiwiY29sb3IiLCJjb25kaXRpb25zIiwiZ3VhcmRpYW5QaGFzZSIsImEiLCJiIiwiY29udGFpbnNSZWFjaGFibGUiLCJyZWFjaGVzT2JqZWN0aXZlIiwiaGFzTWFuZGF0b3J5Q29tcGxldGlvblJvdXRlIiwidmFsaWRhdGVTZWNyZXRSb3V0ZXMiLCJfZmxvb3Ikc2VjcmV0Um9vbXMiLCJfZmxvb3Ikc2VjcmV0Um91dGVzIiwiX2Zsb29yJHNpZGVTcGFjZXMiLCJzZWNyZXRSb29tcyIsInJvdXRlcyIsInNlY3JldFJvdXRlcyIsInJvb21JZHMiLCJzcGFjZSIsInNvdXJjZUlkIiwic2FmZUZhbGxiYWNrIiwidmVyc2lvbiIsImRpc2NvdmVyeUNsdWUiLCJzZWNyZXRJZCIsInJvdXRlSWRzIiwiX2Zsb29yJHNpZGVTcGFjZXMyIiwicm9vbUlkIiwiZW50cnlDb25kaXRpb24iLCJhY2Nlc3NNZXRob2QiLCJyaXNrIiwiYXJyaXZhbCIsInJldHVyblNlbWFudGljcyIsIl9zcGFjZSRyYXJlVHJhbnNpdGlvbiIsIl9yb3V0ZSRkZXN0aW5hdGlvbiIsIl9zcGFjZSRyYXJlVHJhbnNpdGlvbjIiLCJfcm91dGUkZGVzdGluYXRpb24yIiwiX3JvdXRlJGRlc3RpbmF0aW9uMyIsIl9zcGFjZSRyYXJlVHJhbnNpdGlvbjMiLCJfcm91dGUkZGVzdGluYXRpb240IiwiX3NwYWNlJHJhcmVUcmFuc2l0aW9uNCIsInNlY3JldENlbGxzIiwic2VhbGVkIiwib3JpZ2luYWwiLCJ2YWxpZGF0ZVNob3J0Y3V0TGFuZGluZ3MiLCJfZmxvb3Ikc2VjcmV0Um91dGVzMiIsImxhbmRpbmciLCJfZmxvb3IkcHV6emxlSWRzMiIsIl9nZXRUaWxlNzYiLCJfZmxvb3IkcmV3YXJkT2ZmZXJzMiIsIl9mbG9vciRlbmNvdW50ZXJzJGxlbiIsIl9mbG9vciRlbmNvdW50ZXJzIiwibGF5b3V0IiwiZG93bnN0cmVhbSIsImV4cGVjdGVkUmV3YXJkT2ZmZXJzIiwiY2hvaWNlIiwibWlsZXN0b25lTG9jYXRpb25zIiwiX2Zsb29yJGVuY291bnRlcnMyIiwiX2Zsb29yJGVjb2xvZ3kyIiwiY2xlYW51cCIsInByZXZpb3VzS2luZCIsInByZXZpb3VzRmxvdyIsImVmZmVjdCIsImFmdGVyRWNvbG9neSIsInNvbHZhYmxlIiwidHlwZSIsInByb3BJZHMiLCJwcm9wTG9jYXRpb25zIiwiX3Byb3AkaG9va3MiLCJsb2NhdGlvbiIsInJlYWNoYWJsZVNpZGUiLCJlcnJvciIsInJvdXRlTm9kZUZvckZhaWx1cmUiLCJpbnZhcmlhbnQiLCJfcm91dGUkbm9kZXMkZmluZCRpZCIsIl9yb3V0ZSRub2RlcyRmaW5kIiwiZ2VuZXJhdGlvblZhbGlkYXRpb25GYWlsdXJlcyIsInJvdXRlTm9kZSIsInZhbGlkYXRlRmxvb3IiXSwic291cmNlcyI6WyJ3b3JsZC50cyJdLCJzb3VyY2VzQ29udGVudCI6WyJpbXBvcnQgeyBJVEVNUywgTU9OU1RFUlMsIGJpb21lRm9yRmxvb3IsIG1vbnN0ZXJCeUlkLCBtb25zdGVyUm9sZUZvciwgdGVycmFpbkFmZmluaXR5Rm9yIH0gZnJvbSAnLi9jb250ZW50J1xuaW1wb3J0IHsgcm5nRm9yLCBzdHJlYW1TZWVkLCB0eXBlIFJuZyB9IGZyb20gJy4vcm5nJ1xuaW1wb3J0IHsgRkxPT1JfQ09VTlQsIE1BUF9IRUlHSFQsIE1BUF9XSURUSCwgdHlwZSBBY3RvciwgdHlwZSBCaW9tZSwgdHlwZSBCdXJpYWxDcnlwdCwgdHlwZSBCdXJpYWxMYXlvdXQsIHR5cGUgQ2FtcGFpZ25DeWNsZSwgdHlwZSBDYXZlcm5IaWRkZW5DaGFtYmVyLCB0eXBlIENsaWZmQWxjb3ZlLCB0eXBlIENsaWZmTGF5b3V0LCB0eXBlIERpZmZpY3VsdHlDb250ZXh0LCB0eXBlIERpcmVjdGlvbiwgdHlwZSBGbG9vciwgdHlwZSBGbG9vckVuY291bnRlciwgdHlwZSBGcm9zdENhdmUsIHR5cGUgRnJvc3RMYXlvdXQsIHR5cGUgRnVybmFjZUxheW91dCwgdHlwZSBGdXJuYWNlU2VydmljZVNwYWNlLCB0eXBlIE1pbmVCcmVhY2hSb29tLCB0eXBlIFBvaW50LCB0eXBlIFByb3AsIHR5cGUgUml0dWFsSGlkZGVuQ2hhbWJlciwgdHlwZSBSaXR1YWxMYXlvdXQsIHR5cGUgU2FsdE1pcmFnZSwgdHlwZSBUaWxlLCB0eXBlIFRpbGVLaW5kLCB0eXBlIFRyYXZlcnNhbFRvb2xJZCwgdHlwZSBXaGlybHBvb2wsIHR5cGUgV2lsZHNDYXZlLCBmbG9vckluZGV4LCBmbG9vclBvaW50LCBpbkZsb29yQm91bmRzIH0gZnJvbSAnLi90eXBlcydcbmltcG9ydCB7IG9iamVjdGl2ZUZvckZsb29yIH0gZnJvbSAnLi9vYmplY3RpdmVzJ1xuaW1wb3J0IHsgZ2F0ZUZvckFyZWEsIHZhbGlkYXRlQXJlYUdhdGUgfSBmcm9tICcuL2FyZWEtZ2F0ZXMnXG5pbXBvcnQgeyBwdXp6bGVUZW1wbGF0ZXNGb3IsIHZhbGlkYXRlRmxvb3JQdXp6bGVzLCB2YWxpZGF0ZVB1enpsZVRlbXBsYXRlcyB9IGZyb20gJy4vcHV6emxlcydcbmltcG9ydCB7IGlzQmxvY2tpbmdQcm9wLCBQUk9QX0lEUywgcHJvcEF0LCBwcm9wRGVmaW5pdGlvbiwgcHJvcERlZmluaXRpb25zRm9yLCB2YWxpZGF0ZVByb3BEZWZpbml0aW9ucyB9IGZyb20gJy4vcHJvcHMnXG5pbXBvcnQgeyBnZW5lcmF0ZVJvdXRlQ29udHJhY3QsIHZhbGlkYXRlUm91dGVDb250cmFjdCwgdHlwZSBSb3V0ZUNvbnRyYWN0LCB0eXBlIFJvdXRlRWRnZU1vZGUsIHR5cGUgUm91dGVOb2RlS2luZCB9IGZyb20gJy4vcm91dGUtY29udHJhY3QnXG5pbXBvcnQgeyBjb21waWxlUm91dGVDb250cmFjdCwgbWFjcm9Db25uZWN0b3JQb2ludHMsIG1hY3JvUmVjaXBlRm9yLCB2YWxpZGF0ZU1hY3JvUmVhbGl6YXRpb24sIHR5cGUgTWFjcm9SZWNpcGVEZWJ1ZyB9IGZyb20gJy4vbWFjcm8tcmVjaXBlJ1xuaW1wb3J0IHsgc2VsZWN0UGxhY2VtZW50LCB0eXBlIFBsYWNlbWVudENvbnRleHQsIHR5cGUgUGxhY2VtZW50Q29udHJhY3QsIHR5cGUgUGxhY2VtZW50RGVidWcgfSBmcm9tICcuL3BsYWNlbWVudC1jb250cmFjdCdcbmltcG9ydCB7IGRlZmluaXRpb25Gb3JFbmNvdW50ZXIsIGVuY291bnRlclBsYW5zRm9yLCBtZW1iZXJzRm9yRW5jb3VudGVyIH0gZnJvbSAnLi9lbmNvdW50ZXItZGlyZWN0b3InXG5pbXBvcnQgeyBlY29sb2d5RXZlbnRGb3IsIGVjb2xvZ3lQcm9maWxlRm9yIH0gZnJvbSAnLi9lY29sb2d5J1xuaW1wb3J0IHsgZXNjYWxhdGlvbkZvciB9IGZyb20gJy4vZXNjYWxhdGlvbidcbmltcG9ydCB7IHNvY2lhbENvbnRyYWN0Rm9yIH0gZnJvbSAnLi9zb2NpYWwtY29udHJhY3QnXG5pbXBvcnQgeyBvcHRpb25hbFRlcnJhaW5Ub29sRm9yIH0gZnJvbSAnLi90cmF2ZXJzYWwtdG9vbC1kaXN0cmlidXRpb24nXG5pbXBvcnQgeyBwbGFjZVNlY3JldE1ldGFkYXRhIH0gZnJvbSAnLi9zZWNyZXRzJ1xuaW1wb3J0IHsgcmVzb2x2ZUNhbXBhaWduRGlmZmljdWx0eSB9IGZyb20gJy4vY2FtcGFpZ24tZGlmZmljdWx0eSdcbmltcG9ydCB7IGluaXRpYWxDYW1wYWlnbkN5Y2xlIH0gZnJvbSAnLi9lbmdpbmUvY2FtcGFpZ24nXG5cbmNvbnN0IHRpbGUgPSAoa2luZDogVGlsZVsna2luZCddKTogVGlsZSA9PiAoeyBraW5kLCBleHBsb3JlZDogZmFsc2UsIHZpc2libGU6IGZhbHNlIH0pXG5jb25zdCBwb2ludEtleSA9IChwb2ludDogUG9pbnQpID0+IGAke3BvaW50Lnh9LCR7cG9pbnQueX1gXG5jb25zdCBwYXNzYWJsZSA9IChraW5kOiBUaWxlWydraW5kJ10pID0+ICFbJ3dhbGwnLCAnbGF2YScsICdwaXQnLCAncnViYmxlJywgJ2JyYW1ibGUnLCAnY3JhdGUnLCAnY2hlc3QnLCAnZGVlcFdhdGVyJywgJ2JyZWFrd2FsbCcsICdjbGlmZldhbGwnXS5pbmNsdWRlcyhraW5kKVxuY29uc3QgcHJvcERlZmluaXRpb25FcnJvcnMgPSB2YWxpZGF0ZVByb3BEZWZpbml0aW9ucygpXG5jb25zdCBtYWNyb0RlYnVncyA9IG5ldyBXZWFrTWFwPEZsb29yLCBNYWNyb1JlY2lwZURlYnVnPigpXG5jb25zdCBtYWNyb1BpbG90cyA9IG5ldyBXZWFrTWFwPEZsb29yLCBib29sZWFuPigpXG5jb25zdCBwbGFjZW1lbnREZWJ1Z3MgPSBuZXcgV2Vha01hcDxGbG9vciwgUGxhY2VtZW50RGVidWdbXT4oKVxuY29uc3Qgcm91dGVDb250cmFjdERlYnVncyA9IG5ldyBXZWFrTWFwPEZsb29yLCBSb3V0ZUNvbnRyYWN0PigpXG5jb25zdCB0YWN0aWNhbEVuY291bnRlckRlYnVncyA9IG5ldyBXZWFrTWFwPEZsb29yLCByZWFkb25seSBOb25OdWxsYWJsZTxBY3RvclsnZW5jb3VudGVyJ10+W10+KClcblxuY29uc3QgaW5kZXhPZiA9IChmbG9vcjogRmxvb3IsIHg6IG51bWJlciwgeTogbnVtYmVyKTogbnVtYmVyID0+IGZsb29ySW5kZXgoZmxvb3IsIHgsIHkpXG5jb25zdCBwb2ludEF0ID0gKGZsb29yOiBGbG9vciwgaW5kZXg6IG51bWJlcik6IFBvaW50ID0+IGZsb29yUG9pbnQoZmxvb3IsIGluZGV4KVxuY29uc3QgaW5Cb3VuZHMgPSAoZmxvb3I6IEZsb29yLCB4OiBudW1iZXIsIHk6IG51bWJlcik6IGJvb2xlYW4gPT4gaW5GbG9vckJvdW5kcyhmbG9vciwgeCwgeSlcbmV4cG9ydCBjb25zdCBnZXRUaWxlID0gKGZsb29yOiBGbG9vciwgeDogbnVtYmVyLCB5OiBudW1iZXIpOiBUaWxlIHwgdW5kZWZpbmVkID0+IGluQm91bmRzKGZsb29yLCB4LCB5KSA/IGZsb29yLnRpbGVzW2luZGV4T2YoZmxvb3IsIHgsIHkpXSA6IHVuZGVmaW5lZFxuZXhwb3J0IGNvbnN0IGFjdG9yQXQgPSAoZmxvb3I6IEZsb29yLCB4OiBudW1iZXIsIHk6IG51bWJlcik6IEFjdG9yIHwgdW5kZWZpbmVkID0+IGZsb29yLmFjdG9ycy5maW5kKGFjdG9yID0+IGFjdG9yLnggPT09IHggJiYgYWN0b3IueSA9PT0geSAmJiBhY3Rvci5oZWFsdGggPiAwKVxuZXhwb3J0IGNvbnN0IGlzUGFzc2FibGUgPSAoZmxvb3I6IEZsb29yLCB4OiBudW1iZXIsIHk6IG51bWJlcik6IGJvb2xlYW4gPT4ge1xuICBjb25zdCB0YXJnZXQgPSBnZXRUaWxlKGZsb29yLCB4LCB5KVxuICByZXR1cm4gQm9vbGVhbih0YXJnZXQgJiYgcGFzc2FibGUodGFyZ2V0LmtpbmQpICYmIHRhcmdldC5raW5kICE9PSAnbG9ja2VkRG9vcicgJiYgIWFjdG9yQXQoZmxvb3IsIHgsIHkpICYmICFpc0Jsb2NraW5nUHJvcChwcm9wQXQoZmxvb3IucHJvcHMsIHgsIHkpKSlcbn1cblxuY29uc3QgdG9vbERpcmVjdGlvbnMgPSBbWzAsIC0xXSwgWzEsIDBdLCBbMCwgMV0sIFstMSwgMF1dIGFzIGNvbnN0XG5jb25zdCB0b29sSGF6YXJkcyA9IG5ldyBTZXQ8VGlsZUtpbmQ+KFsncGl0JywgJ3dhdGVyJywgJ2xhdmEnLCAnc3Bpa2VzJywgJ2RhcnQnLCAnZmlyZVZlbnQnLCAnZ2FzJywgJ2NydW1ibGUnLCAnYm91bGRlcicsICdicmFtYmxlJywgJ3J1YmJsZScsICdicmluZScsICdmcm9zdFJpbWUnXSlcbmNvbnN0IGRyaWxsYWJsZVRvb2xUZXJyYWluID0gbmV3IFNldDxUaWxlS2luZD4oWyd3YWxsJywgJ3J1YmJsZScsICdicmFtYmxlJywgJ2JvdWxkZXInXSlcbmV4cG9ydCBjb25zdCBwcmltYXJ5VG9vbFVzZUF2YWlsYWJsZSA9IChmbG9vcjogRmxvb3IsIHJlYWNoYWJsZSA9IHJlYWNoYWJsZUZsb29ySW5kZXhlcyhmbG9vcikpOiBib29sZWFuID0+IHtcbiAgY29uc3Qgb2ZmZXIgPSBmbG9vci5yZXdhcmRPZmZlcnM/LmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5taWxlc3RvbmVJZCA9PT0gJ3dheWNhY2hlJylcbiAgaWYgKG9mZmVyPy5raW5kICE9PSAnd2F5Y2FjaGUnKSByZXR1cm4gZmFsc2VcbiAgY29uc3QgdG9vbCA9IG9mZmVyLmNob2ljZXNbMF0/LmlkIGFzIFRyYXZlcnNhbFRvb2xJZCB8IHVuZGVmaW5lZFxuICBpZiAoIXRvb2wpIHJldHVybiBmYWxzZVxuICBmb3IgKGNvbnN0IGluZGV4IG9mIHJlYWNoYWJsZSkge1xuICAgIGNvbnN0IG9yaWdpbiA9IHBvaW50QXQoZmxvb3IsIGluZGV4KVxuICAgIGlmICghaXNQYXNzYWJsZShmbG9vciwgb3JpZ2luLngsIG9yaWdpbi55KSkgY29udGludWVcbiAgICBmb3IgKGNvbnN0IFtkeCwgZHldIG9mIHRvb2xEaXJlY3Rpb25zKSB7XG4gICAgICBjb25zdCBmaXJzdCA9IHsgeDogb3JpZ2luLnggKyBkeCwgeTogb3JpZ2luLnkgKyBkeSB9XG4gICAgICBjb25zdCBzZWNvbmQgPSB7IHg6IG9yaWdpbi54ICsgZHggKiAyLCB5OiBvcmlnaW4ueSArIGR5ICogMiB9XG4gICAgICBjb25zdCBmaXJzdFRpbGUgPSBnZXRUaWxlKGZsb29yLCBmaXJzdC54LCBmaXJzdC55KVxuICAgICAgaWYgKHRvb2wgPT09ICdzdG9uZVdlZGdlJyAmJiBmaXJzdFRpbGUgJiYgZHJpbGxhYmxlVG9vbFRlcnJhaW4uaGFzKGZpcnN0VGlsZS5raW5kKSkgcmV0dXJuIHRydWVcbiAgICAgIGlmICh0b29sID09PSAncmVlZHdpbmcnICYmIGZpcnN0VGlsZSAmJiB0b29sSGF6YXJkcy5oYXMoZmlyc3RUaWxlLmtpbmQpICYmIGlzUGFzc2FibGUoZmxvb3IsIHNlY29uZC54LCBzZWNvbmQueSkpIHJldHVybiB0cnVlXG4gICAgICBpZiAodG9vbCA9PT0gJ2NvcmRBbmNob3InICYmIGlzUGFzc2FibGUoZmxvb3IsIHNlY29uZC54LCBzZWNvbmQueSkpIHJldHVybiB0cnVlXG4gICAgICBpZiAodG9vbCA9PT0gJ2FzaHdheVJpdGVzJyAmJiBmaXJzdFRpbGUgJiYgdG9vbEhhemFyZHMuaGFzKGZpcnN0VGlsZS5raW5kKSAmJiBmaXJzdFRpbGUua2luZCAhPT0gJ2JvdWxkZXInKSByZXR1cm4gdHJ1ZVxuICAgIH1cbiAgfVxuICByZXR1cm4gZmFsc2Vcbn1cblxuY29uc3QgcGF0aE9mZnNldHMgPSBbWy0xLCAtMV0sIFswLCAtMV0sIFsxLCAtMV0sIFstMSwgMF0sIFsxLCAwXSwgWy0xLCAxXSwgWzAsIDFdLCBbMSwgMV1dIGFzIGNvbnN0XG5leHBvcnQgaW50ZXJmYWNlIFRyYXZlcnNhbE9wdGlvbnMgeyB0YXJnZXQ/OiBQb2ludDsgaWdub3JlQmxvY2tpbmdQcm9wcz86IGJvb2xlYW47IGNvbGxlY3RCbG9ja2Vycz86IGJvb2xlYW4gfVxuZXhwb3J0IGludGVyZmFjZSBUcmF2ZXJzYWxSZXN1bHQgeyByZWFjaGFibGU6IFNldDxudW1iZXI+OyBwYXRoPzogUG9pbnRbXTsgYmxvY2tlcnM6IHN0cmluZ1tdIH1cbmNvbnN0IHRyYXZlcnNhbEJsb2NrZXIgPSAoZmxvb3I6IEZsb29yLCBwb2ludDogUG9pbnQsIG9wdGlvbnM6IFRyYXZlcnNhbE9wdGlvbnMpOiBzdHJpbmcgfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBjdXJyZW50ID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSlcbiAgaWYgKCFjdXJyZW50KSByZXR1cm4gJ2JvdW5kcydcbiAgaWYgKCFwYXNzYWJsZShjdXJyZW50LmtpbmQpIHx8IGN1cnJlbnQua2luZCA9PT0gJ2xvY2tlZERvb3InKSByZXR1cm4gYHRlcnJhaW46JHtjdXJyZW50LmtpbmR9YFxuICBjb25zdCBwcm9wID0gcHJvcEF0KGZsb29yLnByb3BzLCBwb2ludC54LCBwb2ludC55KVxuICBpZiAoIW9wdGlvbnMuaWdub3JlQmxvY2tpbmdQcm9wcyAmJiBwcm9wICYmIGlzQmxvY2tpbmdQcm9wKHByb3ApKSByZXR1cm4gYHByb3A6JHtwcm9wLmlkfWBcbiAgcmV0dXJuIHVuZGVmaW5lZFxufVxuY29uc3QgaXNQYXRoUGFzc2FibGUgPSAoZmxvb3I6IEZsb29yLCBwb2ludDogUG9pbnQsIGlnbm9yZUJsb2NraW5nUHJvcHMgPSBmYWxzZSk6IGJvb2xlYW4gPT4gIXRyYXZlcnNhbEJsb2NrZXIoZmxvb3IsIHBvaW50LCB7IGlnbm9yZUJsb2NraW5nUHJvcHMgfSlcblxuZXhwb3J0IGNvbnN0IHRyYXZlcnNlRmxvb3IgPSAoZmxvb3I6IEZsb29yLCBzdGFydCA9IGZsb29yLnN0YXJ0LCBvcHRpb25zOiBUcmF2ZXJzYWxPcHRpb25zID0ge30pOiBUcmF2ZXJzYWxSZXN1bHQgPT4ge1xuICBjb25zdCBzdGFydEJsb2NrZXIgPSB0cmF2ZXJzYWxCbG9ja2VyKGZsb29yLCBzdGFydCwgb3B0aW9ucylcbiAgaWYgKHN0YXJ0QmxvY2tlcikgcmV0dXJuIHsgcmVhY2hhYmxlOiBuZXcgU2V0KCksIGJsb2NrZXJzOiBbYHN0YXJ0OiR7c3RhcnRCbG9ja2VyfWBdIH1cbiAgY29uc3QgY29sbGVjdEJsb2NrZXJzID0gb3B0aW9ucy5jb2xsZWN0QmxvY2tlcnMgPz8gdHJ1ZVxuICBjb25zdCBwcm9wc0J5SW5kZXggPSBvcHRpb25zLmlnbm9yZUJsb2NraW5nUHJvcHMgPyB1bmRlZmluZWQgOiBuZXcgTWFwPG51bWJlciwgUHJvcD4oKVxuICBpZiAocHJvcHNCeUluZGV4KSBmb3IgKGNvbnN0IHByb3Agb2YgZmxvb3IucHJvcHMpIHtcbiAgICBjb25zdCBpbmRleCA9IGluZGV4T2YoZmxvb3IsIHByb3AueCwgcHJvcC55KVxuICAgIGlmIChwcm9wLnN0YXRlICE9PSAnZGVzdHJveWVkJyAmJiAhcHJvcHNCeUluZGV4LmhhcyhpbmRleCkpIHByb3BzQnlJbmRleC5zZXQoaW5kZXgsIHByb3ApXG4gIH1cbiAgY29uc3QgaW5pdGlhbCA9IGluZGV4T2YoZmxvb3IsIHN0YXJ0LngsIHN0YXJ0LnkpXG4gIGNvbnN0IHNlZW4gPSBuZXcgU2V0PG51bWJlcj4oW2luaXRpYWxdKVxuICBjb25zdCBxdWV1ZSA9IFtpbml0aWFsXVxuICBjb25zdCBwcmV2aW91cyA9IG9wdGlvbnMudGFyZ2V0ID8gbmV3IE1hcDxudW1iZXIsIG51bWJlciB8IHVuZGVmaW5lZD4oW1tpbml0aWFsLCB1bmRlZmluZWRdXSkgOiB1bmRlZmluZWRcbiAgY29uc3QgYmxvY2tlcnMgPSBjb2xsZWN0QmxvY2tlcnMgPyBuZXcgU2V0PHN0cmluZz4oKSA6IHVuZGVmaW5lZFxuICBmb3IgKGxldCBjdXJzb3IgPSAwOyBjdXJzb3IgPCBxdWV1ZS5sZW5ndGg7IGN1cnNvcisrKSB7XG4gICAgY29uc3QgY3VycmVudCA9IHF1ZXVlW2N1cnNvcl0hXG4gICAgY29uc3QgcG9pbnQgPSBwb2ludEF0KGZsb29yLCBjdXJyZW50KVxuICAgIGlmIChvcHRpb25zLnRhcmdldCAmJiBwb2ludC54ID09PSBvcHRpb25zLnRhcmdldC54ICYmIHBvaW50LnkgPT09IG9wdGlvbnMudGFyZ2V0LnkpIHtcbiAgICAgIGNvbnN0IHBhdGg6IFBvaW50W10gPSBbXVxuICAgICAgZm9yIChsZXQgaW5kZXg6IG51bWJlciB8IHVuZGVmaW5lZCA9IGN1cnJlbnQ7IGluZGV4ICE9PSB1bmRlZmluZWQ7IGluZGV4ID0gcHJldmlvdXMhLmdldChpbmRleCkpIHBhdGgucHVzaChwb2ludEF0KGZsb29yLCBpbmRleCkpXG4gICAgICByZXR1cm4geyByZWFjaGFibGU6IHNlZW4sIHBhdGg6IHBhdGgucmV2ZXJzZSgpLCBibG9ja2VyczogYmxvY2tlcnMgPyBbLi4uYmxvY2tlcnNdLnNvcnQoKSA6IFtdIH1cbiAgICB9XG4gICAgZm9yIChjb25zdCBbeCwgeV0gb2YgcGF0aE9mZnNldHMpIHtcbiAgICAgIGNvbnN0IG5leHRYID0gcG9pbnQueCArIHhcbiAgICAgIGNvbnN0IG5leHRZID0gcG9pbnQueSArIHlcbiAgICAgIGlmICghaW5Cb3VuZHMoZmxvb3IsIG5leHRYLCBuZXh0WSkpIGNvbnRpbnVlXG4gICAgICBjb25zdCBuZXh0SW5kZXggPSBpbmRleE9mKGZsb29yLCBuZXh0WCwgbmV4dFkpXG4gICAgICBjb25zdCBuZXh0VGlsZSA9IGZsb29yLnRpbGVzW25leHRJbmRleF0hXG4gICAgICBjb25zdCBibG9ja2VyID0gIXBhc3NhYmxlKG5leHRUaWxlLmtpbmQpIHx8IG5leHRUaWxlLmtpbmQgPT09ICdsb2NrZWREb29yJ1xuICAgICAgICA/IGB0ZXJyYWluOiR7bmV4dFRpbGUua2luZH1gXG4gICAgICAgIDogIW9wdGlvbnMuaWdub3JlQmxvY2tpbmdQcm9wcyAmJiBpc0Jsb2NraW5nUHJvcChwcm9wc0J5SW5kZXg/LmdldChuZXh0SW5kZXgpKVxuICAgICAgICAgID8gYHByb3A6JHtwcm9wc0J5SW5kZXghLmdldChuZXh0SW5kZXgpIS5pZH1gXG4gICAgICAgICAgOiB1bmRlZmluZWRcbiAgICAgIGlmIChibG9ja2VyKSB7IGJsb2NrZXJzPy5hZGQoYCR7bmV4dFh9LCR7bmV4dFl9OiR7YmxvY2tlcn1gKTsgY29udGludWUgfVxuICAgICAgaWYgKCFzZWVuLmhhcyhuZXh0SW5kZXgpKSB7IHNlZW4uYWRkKG5leHRJbmRleCk7IHByZXZpb3VzPy5zZXQobmV4dEluZGV4LCBjdXJyZW50KTsgcXVldWUucHVzaChuZXh0SW5kZXgpIH1cbiAgICB9XG4gIH1cbiAgcmV0dXJuIHsgcmVhY2hhYmxlOiBzZWVuLCBibG9ja2VyczogYmxvY2tlcnMgPyBbLi4uYmxvY2tlcnNdLnNvcnQoKSA6IFtdIH1cbn1cblxuZXhwb3J0IGNvbnN0IHJlYWNoYWJsZUZsb29ySW5kZXhlcyA9IChmbG9vcjogRmxvb3IsIHN0YXJ0ID0gZmxvb3Iuc3RhcnQsIGlnbm9yZUJsb2NraW5nUHJvcHMgPSBmYWxzZSk6IFNldDxudW1iZXI+ID0+IHRyYXZlcnNlRmxvb3IoZmxvb3IsIHN0YXJ0LCB7IGlnbm9yZUJsb2NraW5nUHJvcHMsIGNvbGxlY3RCbG9ja2VyczogZmFsc2UgfSkucmVhY2hhYmxlXG5leHBvcnQgY29uc3QgaGFzUGFzc2FibGVUZXJyYWluUGF0aCA9IChmbG9vcjogRmxvb3IsIHN0YXJ0OiBQb2ludCwgZGVzdGluYXRpb246IFBvaW50KTogYm9vbGVhbiA9PiBCb29sZWFuKHRyYXZlcnNlRmxvb3IoZmxvb3IsIHN0YXJ0LCB7IHRhcmdldDogZGVzdGluYXRpb24sIGlnbm9yZUJsb2NraW5nUHJvcHM6IHRydWUsIGNvbGxlY3RCbG9ja2VyczogZmFsc2UgfSkucGF0aClcbmV4cG9ydCBjb25zdCBoYXNQYXNzYWJsZVBhdGggPSAoZmxvb3I6IEZsb29yLCBzdGFydDogUG9pbnQsIGRlc3RpbmF0aW9uOiBQb2ludCk6IGJvb2xlYW4gPT4gQm9vbGVhbih0cmF2ZXJzZUZsb29yKGZsb29yLCBzdGFydCwgeyB0YXJnZXQ6IGRlc3RpbmF0aW9uLCBjb2xsZWN0QmxvY2tlcnM6IGZhbHNlIH0pLnBhdGgpXG5leHBvcnQgY29uc3QgbWFjcm9SZWNpcGVEZWJ1ZyA9IChmbG9vcjogRmxvb3IpOiBNYWNyb1JlY2lwZURlYnVnIHwgdW5kZWZpbmVkID0+IG1hY3JvRGVidWdzLmdldChmbG9vcilcbmV4cG9ydCBjb25zdCBwbGFjZW1lbnREZWJ1ZyA9IChmbG9vcjogRmxvb3IpOiByZWFkb25seSBQbGFjZW1lbnREZWJ1Z1tdID0+IHBsYWNlbWVudERlYnVncy5nZXQoZmxvb3IpID8/IFtdXG5leHBvcnQgY29uc3Qgcm91dGVDb250cmFjdERlYnVnID0gKGZsb29yOiBGbG9vcik6IFJvdXRlQ29udHJhY3QgfCB1bmRlZmluZWQgPT4gcm91dGVDb250cmFjdERlYnVncy5nZXQoZmxvb3IpXG5leHBvcnQgY29uc3QgZXNjYWxhdGlvbkRlYnVnID0gKGZsb29yOiBGbG9vcik6IEZsb29yWydlc2NhbGF0aW9uJ10gPT4gZmxvb3IuZXNjYWxhdGlvblxuZXhwb3J0IGNvbnN0IHRhY3RpY2FsRW5jb3VudGVyRGVidWcgPSAoZmxvb3I6IEZsb29yKTogcmVhZG9ubHkgTm9uTnVsbGFibGU8QWN0b3JbJ2VuY291bnRlciddPltdID0+IHRhY3RpY2FsRW5jb3VudGVyRGVidWdzLmdldChmbG9vcikgPz8gW11cbmV4cG9ydCBjb25zdCB2YWxpZGF0ZU1hY3JvUmVjaXBlID0gKGZsb29yOiBGbG9vcik6IHN0cmluZ1tdID0+IHtcbiAgY29uc3QgZGVidWcgPSBtYWNyb0RlYnVncy5nZXQoZmxvb3IpXG4gIHJldHVybiAhZGVidWcgfHwgIW1hY3JvUGlsb3RzLmdldChmbG9vcikgPyBbXSA6IHZhbGlkYXRlTWFjcm9SZWFsaXphdGlvbihkZWJ1ZywgcG9pbnQgPT4gQm9vbGVhbihnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KSAmJiBpc1BhdGhQYXNzYWJsZShmbG9vciwgcG9pbnQsIGZhbHNlKSkpXG59XG5cbmV4cG9ydCBjb25zdCBwcmVzZXJ2ZXNFeGl0UGF0aCA9IChmbG9vcjogRmxvb3IsIHN0YXJ0OiBQb2ludCwgcG9pbnQ6IFBvaW50LCBraW5kOiBUaWxlWydraW5kJ10pOiBib29sZWFuID0+IHtcbiAgY29uc3QgdGFyZ2V0ID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSlcbiAgaWYgKCF0YXJnZXQpIHJldHVybiBmYWxzZVxuICBjb25zdCBwcmV2aW91cyA9IHRhcmdldC5raW5kXG4gIHRhcmdldC5raW5kID0ga2luZFxuICBjb25zdCBwcmVzZXJ2ZXMgPSBoYXNQYXNzYWJsZVRlcnJhaW5QYXRoKGZsb29yLCBzdGFydCwgZmxvb3IuZXhpdClcbiAgdGFyZ2V0LmtpbmQgPSBwcmV2aW91c1xuICByZXR1cm4gcHJlc2VydmVzXG59XG5cbmV4cG9ydCBjb25zdCBwcmVzZXJ2ZXNBZGphY2VudEV4aXRBY2Nlc3MgPSAoZmxvb3I6IEZsb29yLCBwb2ludDogUG9pbnQsIGtpbmQ6IFRpbGVbJ2tpbmQnXSk6IGJvb2xlYW4gPT4ge1xuICBjb25zdCB0YXJnZXQgPSBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KVxuICBpZiAoIXRhcmdldCkgcmV0dXJuIGZhbHNlXG4gIGNvbnN0IHByZXZpb3VzID0gdGFyZ2V0LmtpbmRcbiAgdGFyZ2V0LmtpbmQgPSBraW5kXG4gIGNvbnN0IGFkamFjZW50ID0gW1swLCAtMV0sIFsxLCAwXSwgWzAsIDFdLCBbLTEsIDBdXVxuICAgIC5tYXAoKFt4LCB5XSkgPT4gKHsgeDogcG9pbnQueCArIHgsIHk6IHBvaW50LnkgKyB5IH0pKVxuICAgIC5maWx0ZXIoY2FuZGlkYXRlID0+IHtcbiAgICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKGZsb29yLCBjYW5kaWRhdGUueCwgY2FuZGlkYXRlLnkpXG4gICAgICByZXR1cm4gQm9vbGVhbih0aWxlICYmIHBhc3NhYmxlKHRpbGUua2luZCkgJiYgdGlsZS5raW5kICE9PSAnbG9ja2VkRG9vcicpXG4gICAgfSlcbiAgY29uc3QgcHJlc2VydmVzID0gYWRqYWNlbnQubGVuZ3RoID4gMCAmJiBhZGphY2VudC5ldmVyeShjYW5kaWRhdGUgPT4gaGFzUGFzc2FibGVUZXJyYWluUGF0aChmbG9vciwgY2FuZGlkYXRlLCBmbG9vci5leGl0KSlcbiAgdGFyZ2V0LmtpbmQgPSBwcmV2aW91c1xuICByZXR1cm4gcHJlc2VydmVzXG59XG5cbmV4cG9ydCBjb25zdCBkaWZmaWN1bHR5Rm9yID0gKHJvdXRlUG9zaXRpb246IG51bWJlciwgYXJlYUZsb29yOiBudW1iZXIpOiBEaWZmaWN1bHR5Q29udGV4dCA9PiB7XG4gIGNvbnN0IHRocmVhdCA9IE1hdGgubWF4KDAsIE1hdGgubWluKDE1LCByb3V0ZVBvc2l0aW9uICogNCArIGFyZWFGbG9vcikpXG4gIHJldHVybiB7IHJvdXRlUG9zaXRpb24sIHRocmVhdCwgaGVhbHRoTXVsdGlwbGllcjogMSArIHRocmVhdCAqIDAuMDYsIGF0dGFja0JvbnVzOiBNYXRoLmZsb29yKHRocmVhdCAvIDIpLCBkZWZlbnNlQm9udXM6IE1hdGguZmxvb3IodGhyZWF0IC8gNSksIGVsaXRlQ2hhbmNlOiBNYXRoLm1pbigzNSwgNCArIHRocmVhdCAqIDIpLCBndWFyZGlhblBhdHRlcm46IHRocmVhdCA+PSAxMiA/IDMgOiB0aHJlYXQgPj0gOCA/IDIgOiB0aHJlYXQgPj0gNCA/IDEgOiAwIH1cbn1cblxuY29uc3QgYXNzZXJ0R2VuZXJhdGlvblBoYXNlID0gKGZsb29yOiBGbG9vciwgY2FtcGFpZ25TZWVkOiBudW1iZXIsIGNvbnRyYWN0OiBSZXR1cm5UeXBlPHR5cGVvZiBnZW5lcmF0ZVJvdXRlQ29udHJhY3Q+LCBwaGFzZTogc3RyaW5nKTogdm9pZCA9PiB7XG4gIGNvbnN0IHRhcmdldHMgPSBbZmxvb3IuZXhpdCwgLi4ub2JqZWN0aXZlVGFyZ2V0cyhmbG9vcildXG4gIGNvbnN0IHJlYWNoYWJsZUluZGV4ZXMgPSByZWFjaGFibGVGbG9vckluZGV4ZXMoZmxvb3IpXG4gIGNvbnN0IHJlYWNoYWJsZSA9IChwb2ludDogUG9pbnQpOiBib29sZWFuID0+IGluQm91bmRzKGZsb29yLCBwb2ludC54LCBwb2ludC55KSAmJiByZWFjaGFibGVJbmRleGVzLmhhcyhpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSlcbiAgZm9yIChjb25zdCB0YXJnZXQgb2YgdGFyZ2V0cykge1xuICAgIGNvbnN0IHRhcmdldFJlYWNoYWJsZSA9IHRhcmdldC54ID09PSBmbG9vci5leGl0LnggJiYgdGFyZ2V0LnkgPT09IGZsb29yLmV4aXQueVxuICAgICAgPyByZWFjaGFibGUodGFyZ2V0KVxuICAgICAgOiBbWzAsIDBdLCBbMCwgLTFdLCBbMSwgMF0sIFswLCAxXSwgWy0xLCAwXV0uc29tZSgoW3gsIHldKSA9PiByZWFjaGFibGUoeyB4OiB0YXJnZXQueCArIHgsIHk6IHRhcmdldC55ICsgeSB9KSlcbiAgICBpZiAodGFyZ2V0UmVhY2hhYmxlKSBjb250aW51ZVxuICAgIGNvbnN0IG5vZGUgPSBjb250cmFjdC5ub2Rlcy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUua2luZCA9PT0gKHRhcmdldC54ID09PSBmbG9vci5leGl0LnggJiYgdGFyZ2V0LnkgPT09IGZsb29yLmV4aXQueSA/IChmbG9vci5pbmRleCAlIDQgPT09IDMgPyAnYm9zcycgOiAnZXhpdCcpIDogJ29iamVjdGl2ZScpKVxuICAgIGNvbnN0IHRyYWNlID0gdHJhdmVyc2VGbG9vcihmbG9vcilcbiAgICB0aHJvdyBuZXcgRXJyb3IoYGdlbmVyYXRpb24gZmFpbHVyZSBzZWVkPSR7Y2FtcGFpZ25TZWVkfSBiaW9tZT0ke2Zsb29yLmJpb21lfSByZWNpcGU9JHtmbG9vci5sYXlvdXRJZH0gcGhhc2U9JHtwaGFzZX0gY29udHJhY3ROb2RlPSR7bm9kZT8uaWQgPz8gJ3Vua25vd24nfSB0YXJnZXQ9JHt0YXJnZXQueH0sJHt0YXJnZXQueX0gcmVhY2hlZD0ke3RyYWNlLnJlYWNoYWJsZS5zaXplfSBibG9ja2Vycz0ke3RyYWNlLmJsb2NrZXJzLmpvaW4oJ3wnKX1gKVxuICB9XG59XG5cbmV4cG9ydCBmdW5jdGlvbiBnZW5lcmF0ZUZsb29yKHJ1blNlZWQ6IG51bWJlciwgaW5kZXg6IG51bWJlciwgZGlmZmljdWx0eSA9IGRpZmZpY3VsdHlGb3IoTWF0aC5mbG9vcihpbmRleCAvIDQpLCBpbmRleCAlIDQpKTogRmxvb3Ige1xuICBjb25zdCBzZWVkID0gc3RyZWFtU2VlZChydW5TZWVkLCAnZ2VuZXJhdGlvbicsIGluZGV4KVxuICBjb25zdCBiaW9tZSA9IGJpb21lRm9yRmxvb3IoaW5kZXgpXG4gIGNvbnN0IGFyZWFGbG9vciA9IGluZGV4ICUgNFxuICBjb25zdCBlc2NhbGF0aW9uID0gZXNjYWxhdGlvbkZvcihydW5TZWVkLCBiaW9tZSwgYXJlYUZsb29yKVxuICBjb25zdCBsYXlvdXRSbmcgPSBybmdGb3IocnVuU2VlZCwgJ2dlbmVyYXRpb24nLCBpbmRleCwgJ2xheW91dCcpXG4gIGNvbnN0IGxheW91dElkID0gbGF5b3V0Rm9yKHJ1blNlZWQsIGJpb21lLCBhcmVhRmxvb3IpXG4gIGNvbnN0IHJvdXRlQ29udHJhY3QgPSBnZW5lcmF0ZVJvdXRlQ29udHJhY3QoeyBjYW1wYWlnblNlZWQ6IHJ1blNlZWQsIGZsb29ySW5kZXg6IGluZGV4LCBiaW9tZSwgYXJlYUZsb29yLCByZWNpcGVJZDogbGF5b3V0SWQsIGVzY2FsYXRpb25WYXJpYW50OiBgJHtlc2NhbGF0aW9uLmFyY0lkfToke2VzY2FsYXRpb24ucGhhc2V9YCB9KVxuICBjb25zdCByb3V0ZVZhbGlkYXRpb24gPSB2YWxpZGF0ZVJvdXRlQ29udHJhY3Qocm91dGVDb250cmFjdClcbiAgaWYgKCFyb3V0ZVZhbGlkYXRpb24udmFsaWQpIHRocm93IG5ldyBFcnJvcihgaW52YWxpZCByb3V0ZSBjb250cmFjdCAke3JvdXRlQ29udHJhY3QuaWR9OiAke3JvdXRlVmFsaWRhdGlvbi5lcnJvcnMuam9pbignOyAnKX1gKVxuICBjb25zdCB7IHdpZHRoLCBoZWlnaHQgfSA9IGRpbWVuc2lvbnNGb3IoYmlvbWUpXG4gIGNvbnN0IG1hY3JvID0gY29tcGlsZVJvdXRlQ29udHJhY3Qocm91dGVDb250cmFjdCwgeyB3aWR0aCwgaGVpZ2h0IH0pXG4gIGlmICghbWFjcm8udmFsaWQpIHRocm93IG5ldyBFcnJvcihgaW52YWxpZCBtYWNybyByZWNpcGUgJHttYWNyby5yZWNpcGVJZH06ICR7bWFjcm8uZGlhZ25vc3RpY3Muam9pbignOyAnKX1gKVxuICBjb25zdCBwbGFjZW1lbnRzOiBQbGFjZW1lbnRSdW50aW1lID0geyBtYWNybywgcGlsb3Q6IG1hY3JvUmVjaXBlRm9yKHJvdXRlQ29udHJhY3QpLnBpbG90LCBkaWFnbm9zdGljczogW10gfVxuICBjb25zdCBvYmplY3RpdmUgPSBvYmplY3RpdmVGb3JGbG9vcihpbmRleClcbiAgY29uc3QgZmxvb3I6IEZsb29yID0ge1xuICAgIGluZGV4LFxuICAgIGJpb21lLFxuICAgIHNlZWQsXG4gICAgd2lkdGgsXG4gICAgaGVpZ2h0LFxuICAgIGxheW91dElkLFxuICAgIHRpbGVzOiBBcnJheS5mcm9tKHsgbGVuZ3RoOiB3aWR0aCAqIGhlaWdodCB9LCAoKSA9PiB0aWxlKCd3YWxsJykpLFxuICAgIGFjdG9yczogW10sXG4gICAgaXRlbXM6IFtdLFxuICAgIHByb3BzOiBbXSxcbiAgICBlbmNvdW50ZXJzOiBbXSxcbiAgICBzdGFydDogeyB4OiAyLCB5OiAyIH0sXG4gICAgZXhpdDogeyB4OiB3aWR0aCAtIDMsIHk6IGhlaWdodCAtIDMgfSxcbiAgICBndWFyZGlhbkRlZmVhdGVkOiBhcmVhRmxvb3IgIT09IDMsXG4gICAgb2JqZWN0aXZlOiB7IC4uLm9iamVjdGl2ZSwgbGFiZWw6IGAke29iamVjdGl2ZS5sYWJlbH0g4oCUICR7YXJlYUZsb29yID09PSAzID8gZXNjYWxhdGlvbi5wYXlvZmYgOiBlc2NhbGF0aW9uLnByb21pc2V9YCB9LFxuICAgIG1pbGVzdG9uZXM6IFtdLFxuICAgIHJld2FyZE9mZmVyczogcm91dGVDb250cmFjdC5yZXdhcmRPZmZlcnMsXG4gICAgZXNjYWxhdGlvbixcbiAgICB0ZWxlZ3JhcGhzOiBbXSxcbiAgICBkaWZmaWN1bHR5XG4gIH1cbiAgY29uc3Qgcm9vbXMgPSBjYXJ2ZVJvdXRlQ29udHJhY3RMYXlvdXQoZmxvb3IsIHJvdXRlQ29udHJhY3QsIG1hY3JvLCBsYXlvdXRSbmcpXG4gIGNvbnN0IHJlc2VydmVkTWFjcm9DZWxscyA9IG1hY3JvUmVjaXBlRm9yKHJvdXRlQ29udHJhY3QpLnBpbG90ID8gbmV3IFNldChtYWNyb0Nvbm5lY3RvclBvaW50cyhtYWNybykubWFwKHBvaW50ID0+IGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSkgOiBuZXcgU2V0PG51bWJlcj4oKVxuICBmbG9vci5zdGFydCA9IGNlbnRlcihyb29tc1swXSlcbiAgZmxvb3IuZXhpdCA9IGNlbnRlcihyb29tc1tyb29tcy5sZW5ndGggLSAxXSlcbiAgc2V0S2luZChmbG9vciwgZmxvb3IuZXhpdC54LCBmbG9vci5leGl0LnksICdleGl0JylcbiAgZGVjb3JhdGVCaW9tZShmbG9vciwgcm5nRm9yKHJ1blNlZWQsICdnZW5lcmF0aW9uJywgaW5kZXgsICd0ZXJyYWluJywgZXNjYWxhdGlvbi5hcmNJZCksIHJvb21zKVxuICBwbGFjZVB1enpsZVRlbXBsYXRlKGZsb29yLCBybmdGb3IocnVuU2VlZCwgJ2dlbmVyYXRpb24nLCBpbmRleCwgJ3B1enpsZScpLCByb29tcylcbiAgaW1wcmludEVzY2FsYXRpb25MYW5kbWFyayhmbG9vciwgcm9vbXMpXG4gIHJlc3RvcmVNYWNyb0Nvbm5lY3RvcnMoZmxvb3IsIG1hY3JvLCByZXNlcnZlZE1hY3JvQ2VsbHMpXG4gIHJlc3RvcmVNYWNyb05vZGVUcmFuc2l0KGZsb29yLCBtYWNybylcbiAgaW1wcmludE1pbmVSYWlsU2VydmljZVJvdXRlKGZsb29yLCBtYWNybylcbiAgaW1wcmludFJ1aW5zUml0dWFsQ2VudGVyKGZsb29yLCBtYWNybylcbiAgcGxhY2VFdmVudHMoZmxvb3IsIHJvb21zLCBwbGFjZW1lbnRzKVxuICBwbGFjZURvb3JzQW5kTG9ja3MoZmxvb3IsIHJuZ0ZvcihydW5TZWVkLCAnZ2F0ZXMnLCBpbmRleCksIHJvb21zKVxuICBvcGVuTWFuZGF0b3J5TG9ja3MoZmxvb3IpXG4gIHBsYWNlQ29udGFpbmVycyhmbG9vciwgcm5nRm9yKHJ1blNlZWQsICdsb290JywgaW5kZXgsICdjb250YWluZXJzJyksIHJvb21zLCByZXNlcnZlZE1hY3JvQ2VsbHMpXG4gIHJlc3RvcmVNYWNyb0Nvbm5lY3RvcnMoZmxvb3IsIG1hY3JvLCByZXNlcnZlZE1hY3JvQ2VsbHMpXG4gIHBsYWNlbWVudHMucmVhY2hhYmxlID0gdW5kZWZpbmVkXG4gIGltcHJpbnRDYXZlcm5UaWRlUm91dGUoZmxvb3IsIG1hY3JvKVxuICBpbXByaW50V2lsZHNQcmVzc3VyZVJvdXRlKGZsb29yLCBtYWNybylcbiAgaW1wcmludFJ1aW5zV2FyZFJvdXRlKGZsb29yLCBtYWNybylcbiAgaW1wcmludEZ1cm5hY2VGaXJpbmdSb3V0ZShmbG9vciwgbWFjcm8pXG4gIGltcHJpbnRGbG9vZGVkQ3VycmVudE5ldHdvcmsoZmxvb3IsIG1hY3JvKVxuICBpbXByaW50Q2xpZmZIZWlnaHRHcmFwaChmbG9vciwgbWFjcm8pXG4gIGltcHJpbnRCdXJpYWxSaXR1YWxSb3V0ZXMoZmxvb3IsIG1hY3JvKVxuICBpbXByaW50U2FsdFJvdXRlQ29udHJhY3QoZmxvb3IsIG1hY3JvKVxuICBpbXByaW50RnJvc3RSb3V0ZUNvbnRyYWN0KGZsb29yLCBtYWNybylcbiAgaW1wcmludENhdmVyblNldHBpZWNlQ29udGV4dChmbG9vciwgbWFjcm8pXG4gIHJlcGFpck1hbmRhdG9yeVBhdGgoZmxvb3IpXG4gIGltcHJpbnRNaW5lQnJlYWNoUm9vbXMoZmxvb3IsIHJuZ0ZvcihydW5TZWVkLCAnZ2VuZXJhdGlvbicsIGluZGV4LCAnbWluZS1icmVhY2gtcm9vbXMnKSlcbiAgaW1wcmludFdpbGRzQ2F2ZXMoZmxvb3IsIHJuZ0ZvcihydW5TZWVkLCAnZ2VuZXJhdGlvbicsIGluZGV4LCAnd2lsZHMtY2F2ZXMnKSlcbiAgaW1wcmludENhdmVybkhpZGRlbkNoYW1iZXJzKGZsb29yLCBybmdGb3IocnVuU2VlZCwgJ2dlbmVyYXRpb24nLCBpbmRleCwgJ2NhdmVybi1oaWRkZW4tY2hhbWJlcnMnKSlcbiAgaW1wcmludFJ1aW5zSGlkZGVuQ2hhbWJlcnMoZmxvb3IsIG1hY3JvLCBybmdGb3IocnVuU2VlZCwgJ2dlbmVyYXRpb24nLCBpbmRleCwgJ3JpdHVhbC1oaWRkZW4tY2hhbWJlcnMnKSlcbiAgaW1wcmludEZ1cm5hY2VTZXJ2aWNlU3BhY2VzKGZsb29yLCBtYWNybywgcm5nRm9yKHJ1blNlZWQsICdnZW5lcmF0aW9uJywgaW5kZXgsICdmdXJuYWNlLXNlcnZpY2Utc3BhY2VzJykpXG4gIGltcHJpbnRGbG9vZGVkV2hpcmxwb29scyhmbG9vciwgbWFjcm8sIHJuZ0ZvcihydW5TZWVkLCAnZ2VuZXJhdGlvbicsIGluZGV4LCAnZmxvb2RlZC13aGlybHBvb2xzJykpXG4gIGltcHJpbnRDbGlmZkFsY292ZXMoZmxvb3IsIG1hY3JvLCBybmdGb3IocnVuU2VlZCwgJ2dlbmVyYXRpb24nLCBpbmRleCwgJ2NsaWZmLWFsY292ZXMnKSlcbiAgaW1wcmludEJ1cmlhbENyeXB0cyhmbG9vciwgbWFjcm8sIHJuZ0ZvcihydW5TZWVkLCAnZ2VuZXJhdGlvbicsIGluZGV4LCAnYnVyaWFsLWNyeXB0cycpKVxuICBpbXByaW50U2FsdE1pcmFnZXMoZmxvb3IsIG1hY3JvLCBybmdGb3IocnVuU2VlZCwgJ2dlbmVyYXRpb24nLCBpbmRleCwgJ3NhbHQtbWlyYWdlcycpKVxuICBpbXByaW50RnJvc3RDYXZlcyhmbG9vciwgbWFjcm8sIHJuZ0ZvcihydW5TZWVkLCAnZ2VuZXJhdGlvbicsIGluZGV4LCAnZnJvc3QtY2F2ZXMnKSlcbiAgcGxhY2VTZWNyZXRNZXRhZGF0YShmbG9vcilcbiAgYXNzZXJ0R2VuZXJhdGlvblBoYXNlKGZsb29yLCBydW5TZWVkLCByb3V0ZUNvbnRyYWN0LCAnZ2VvbWV0cnknKVxuICBwbGFjZUFjdG9ycyhmbG9vciwgcm5nRm9yKHJ1blNlZWQsICdnZW5lcmF0aW9uJywgaW5kZXgsICdhY3RvcnMnKSwgcGxhY2VtZW50cylcbiAgYXNzZXJ0R2VuZXJhdGlvblBoYXNlKGZsb29yLCBydW5TZWVkLCByb3V0ZUNvbnRyYWN0LCAnYWN0b3JzJylcbiAgcGxhY2VFY29sb2d5KGZsb29yLCBwbGFjZW1lbnRzKVxuICBhc3NlcnRHZW5lcmF0aW9uUGhhc2UoZmxvb3IsIHJ1blNlZWQsIHJvdXRlQ29udHJhY3QsICdlY29sb2d5JylcbiAgcGxhY2VJdGVtcyhmbG9vciwgcm5nRm9yKHJ1blNlZWQsICdsb290JywgaW5kZXgsICdpdGVtcycpLCByb29tcywgcGxhY2VtZW50cylcbiAgYXNzZXJ0R2VuZXJhdGlvblBoYXNlKGZsb29yLCBydW5TZWVkLCByb3V0ZUNvbnRyYWN0LCAnbG9vdCcpXG4gIHBsYWNlUHJvcHMoZmxvb3IsIHJlc2VydmVkTWFjcm9DZWxscywgcGxhY2VtZW50cylcbiAgYXNzZXJ0R2VuZXJhdGlvblBoYXNlKGZsb29yLCBydW5TZWVkLCByb3V0ZUNvbnRyYWN0LCAncHJvcHMnKVxuICBwbGFjZU1pbGVzdG9uZXMoZmxvb3IsIHBsYWNlbWVudHMpXG4gIGFzc2VydEdlbmVyYXRpb25QaGFzZShmbG9vciwgcnVuU2VlZCwgcm91dGVDb250cmFjdCwgJ21pbGVzdG9uZXMnKVxuICBwbGFjZUVuY291bnRlcnMoZmxvb3IsIHJuZ0ZvcihydW5TZWVkLCAnZ2VuZXJhdGlvbicsIGluZGV4LCAnZW5jb3VudGVycycpLCBwbGFjZW1lbnRzKVxuICBhc3NlcnRHZW5lcmF0aW9uUGhhc2UoZmxvb3IsIHJ1blNlZWQsIHJvdXRlQ29udHJhY3QsICdlbmNvdW50ZXJzJylcbiAgbWFjcm9EZWJ1Z3Muc2V0KGZsb29yLCBtYWNybylcbiAgbWFjcm9QaWxvdHMuc2V0KGZsb29yLCBtYWNyb1JlY2lwZUZvcihyb3V0ZUNvbnRyYWN0KS5waWxvdClcbiAgcGxhY2VtZW50RGVidWdzLnNldChmbG9vciwgcGxhY2VtZW50cy5kaWFnbm9zdGljcylcbiAgcm91dGVDb250cmFjdERlYnVncy5zZXQoZmxvb3IsIHJvdXRlQ29udHJhY3QpXG4gIGNvbnN0IG1hY3JvRXJyb3JzID0gdmFsaWRhdGVNYWNyb1JlY2lwZShmbG9vcilcbiAgaWYgKG1hY3JvRXJyb3JzLmxlbmd0aCkgdGhyb3cgbmV3IEVycm9yKGBpbnZhbGlkIG1hY3JvIHJlY2lwZSAke21hY3JvLnJlY2lwZUlkfTogJHttYWNyb0Vycm9ycy5qb2luKCc7ICcpfWApXG4gIGNvbnN0IHZhbGlkYXRpb24gPSB2YWxpZGF0ZUdlbmVyYXRpb24oZmxvb3IpXG4gIGlmICghdmFsaWRhdGlvbi52YWxpZCkgdGhyb3cgbmV3IEVycm9yKGBpbnZhbGlkIGdlbmVyYXRlZCBmbG9vciAke2luZGV4fS8ke2xheW91dElkfTogJHt2YWxpZGF0aW9uLmVycm9ycy5qb2luKCc7ICcpfWApXG4gIHJldHVybiBmbG9vclxufVxuXG5leHBvcnQgY29uc3QgYXJlYUZsb29ySW5kZXggPSAoYmlvbWU6IEZsb29yWydiaW9tZSddLCBhcmVhRmxvb3I6IG51bWJlcik6IG51bWJlciA9PiAoWydtaW5lJywgJ3dpbGRzJywgJ2NhdmVybnMnLCAncnVpbnMnLCAnZnVybmFjZScsICdmbG9vZGVkUnVpbnMnLCAnY2xpZmZzJywgJ2J1cmlhbCcsICdzYWx0RmxhdHMnLCAnZnJvc3RSZWxpcXVhcnknXSBhcyBjb25zdCkuaW5kZXhPZihiaW9tZSkgKiA0ICsgYXJlYUZsb29yXG5leHBvcnQgY29uc3QgZ2VuZXJhdGVBcmVhRmxvb3IgPSAocnVuU2VlZDogbnVtYmVyLCBiaW9tZTogRmxvb3JbJ2Jpb21lJ10sIGFyZWFGbG9vcjogbnVtYmVyLCByb3V0ZVBvc2l0aW9uID0gMCwgY3ljbGU6IENhbXBhaWduQ3ljbGUgPSBpbml0aWFsQ2FtcGFpZ25DeWNsZSgpKTogRmxvb3IgPT4ge1xuICBpZiAoIU51bWJlci5pc0ludGVnZXIoYXJlYUZsb29yKSB8fCBhcmVhRmxvb3IgPCAwIHx8IGFyZWFGbG9vciA+IDMpIHRocm93IG5ldyBFcnJvcihgaW52YWxpZCBhcmVhIGZsb29yOiAke2FyZWFGbG9vcn1gKVxuICByZXR1cm4gZ2VuZXJhdGVGbG9vcihydW5TZWVkLCBhcmVhRmxvb3JJbmRleChiaW9tZSwgYXJlYUZsb29yKSwgcmVzb2x2ZUNhbXBhaWduRGlmZmljdWx0eShkaWZmaWN1bHR5Rm9yKHJvdXRlUG9zaXRpb24sIGFyZWFGbG9vciksIGN5Y2xlKSlcbn1cblxuaW50ZXJmYWNlIFJvb20geyB4OiBudW1iZXI7IHk6IG51bWJlcjsgdzogbnVtYmVyOyBoOiBudW1iZXIgfVxuY29uc3QgY2VudGVyID0gKHJvb206IFJvb20pOiBQb2ludCA9PiAoeyB4OiByb29tLnggKyBNYXRoLmZsb29yKHJvb20udyAvIDIpLCB5OiByb29tLnkgKyBNYXRoLmZsb29yKHJvb20uaCAvIDIpIH0pXG5jb25zdCBjYXJkaW5hbE9mZnNldHMgPSBbWzAsIC0xXSwgWzEsIDBdLCBbMCwgMV0sIFstMSwgMF1dIGFzIGNvbnN0XG5jb25zdCBtaW5lSGF6YXJkcyA9IG5ldyBTZXQ8VGlsZVsna2luZCddPihbJ3NwaWtlcycsICdkYXJ0JywgJ2ZpcmVWZW50JywgJ2NydW1ibGUnLCAnYm91bGRlcicsICdnYXMnLCAnbGF2YScsICdwaXQnXSlcblxuY29uc3QgZGltZW5zaW9uczogUmVjb3JkPEJpb21lLCB7IHdpZHRoOiBudW1iZXI7IGhlaWdodDogbnVtYmVyIH0+ID0ge1xuICBtaW5lOiB7IHdpZHRoOiBNQVBfV0lEVEgsIGhlaWdodDogTUFQX0hFSUdIVCB9LCB3aWxkczogeyB3aWR0aDogNzIsIGhlaWdodDogNDggfSwgY2F2ZXJuczogeyB3aWR0aDogNTYsIGhlaWdodDogNDQgfSwgcnVpbnM6IHsgd2lkdGg6IDY0LCBoZWlnaHQ6IDQ4IH0sIGZ1cm5hY2U6IHsgd2lkdGg6IDU2LCBoZWlnaHQ6IDQwIH0sIGZsb29kZWRSdWluczogeyB3aWR0aDogNzIsIGhlaWdodDogNDggfSwgY2xpZmZzOiB7IHdpZHRoOiA1NiwgaGVpZ2h0OiA1MiB9LCBidXJpYWw6IHsgd2lkdGg6IDgwLCBoZWlnaHQ6IDU2IH0sIHNhbHRGbGF0czogeyB3aWR0aDogNzIsIGhlaWdodDogNDQgfSwgZnJvc3RSZWxpcXVhcnk6IHsgd2lkdGg6IDY0LCBoZWlnaHQ6IDQ4IH1cbn1cbmNvbnN0IGxheW91dFZhcmlhbnRzOiBSZWNvcmQ8QmlvbWUsIHJlYWRvbmx5IHN0cmluZ1tdPiA9IHtcbiAgbWluZTogWydyYWlsLXNwaW5lJywgJ2JyYW5jaGluZy1kcmlmdHMnLCAnY29sbGFwc2UtbG9vcCddLCB3aWxkczogWydyaXZlci1jbGVhcmluZ3MnLCAncm9vdC1tYXplJywgJ3dldGxhbmQtY2F1c2V3YXlzJ10sIGNhdmVybnM6IFsndGlkZS1jaGFtYmVycycsICdzaW5raG9sZS1nYWxsZXJpZXMnLCAnZmF1bHQtdHVubmVscyddLCBydWluczogWydjaXJjdWxhci1wcmVjaW5jdCcsICdicm9rZW4tcHJvY2Vzc2lvbmFsLWxvb3AnLCAnY291cnR5YXJkLWxhdHRpY2UnXSwgZnVybmFjZTogWydzdGVwcGVkLWtpbG4tY2hhaW4nLCAnc21va2UtY2hva2VkLXNlcnZpY2Utcm91dGUnLCAnbGlmdC1hbmQtYXNoLWxvb3AnXSwgZmxvb2RlZFJ1aW5zOiBbJ2JyYWlkZWQtY3VycmVudC1kZWx0YScsICdhbmNob3ItZ2F0ZWQtcnVpbicsICdpc2xhbmQtaG9wLW5ldHdvcmsnXSwgY2xpZmZzOiBbJ3N3aXRjaGJhY2stZmFjZScsICdyYXZpbmUtYnJpZGdlLWxvb3AnLCAnYW5jaG9yLWNoYWluJ10sIGJ1cmlhbDogWydzdG9uZS1jaXJjbGUtY2VudGVyJywgJ21vdW5kLXByb2Nlc3Npb24nLCAnY2VtZXRlcnktc2V0dGxlbWVudC1lZGdlJywgJ29zc3VhcnktaG9sbG93JywgJ2FuY2VzdG9yLXBhdGgtbG9vcCddLCBzYWx0RmxhdHM6IFsnY3J1c3QtaXNsYW5kLWNoYWluJywgJ2JyaW5lLW1hemUnLCAnY2FyYXZhbi1jYXVzZXdheScsICdtaXJyb3ItYmFzaW4tbG9vcCcsICdzYWx0LXJpZGdlLXJlZnVnZSddLCBmcm9zdFJlbGlxdWFyeTogWydmcm96ZW4tbGFrZS1jcm9zc2luZycsICdyaWRnZS1ob2xsb3ctbG9vcCcsICdwcmVzc3VyZS1jcmFjay1tYXplJywgJ3Nob3JlLXJlbGlxdWFyeS1yb3V0ZScsICdzdG9ybS1yZWZ1Z2UtY2hhaW4nXVxufVxuY29uc3QgZGltZW5zaW9uc0ZvciA9IChiaW9tZTogQmlvbWUpID0+IGRpbWVuc2lvbnNbYmlvbWVdXG5leHBvcnQgY29uc3QgbGF5b3V0Rm9yID0gKHJ1blNlZWQ6IG51bWJlciwgYmlvbWU6IEJpb21lLCBhcmVhRmxvb3I6IG51bWJlcik6IHN0cmluZyA9PiB7XG4gIGNvbnN0IGRlY2sgPSBybmdGb3IocnVuU2VlZCwgJ2dlbmVyYXRpb24nLCBhcmVhRmxvb3JJbmRleChiaW9tZSwgMCksICdsYXlvdXQtZGVjaycpLnNodWZmbGUoWy4uLmxheW91dFZhcmlhbnRzW2Jpb21lXV0pXG4gIHJldHVybiBhcmVhRmxvb3IgPCBkZWNrLmxlbmd0aCA/IGRlY2tbYXJlYUZsb29yXSA6IGAke2RlY2tbKGFyZWFGbG9vciArIHJ1blNlZWQpICUgZGVjay5sZW5ndGhdfS1yZW1peGBcbn1cblxuY29uc3QgZXNjYWxhdGlvbkxhbmRtYXJrVGVycmFpbjogUmVjb3JkPEJpb21lLCByZWFkb25seSBUaWxlS2luZFtdPiA9IHtcbiAgbWluZTogWydzdXBwb3J0JywgJ3JhaWwnXSwgd2lsZHM6IFsnd2ViJywgJ3dhdGVyJ10sIGNhdmVybnM6IFsnZGFya25lc3MnLCAnd2F0ZXInXSwgcnVpbnM6IFsnYWx0YXInLCAnZGFydCddLCBmdXJuYWNlOiBbJ2xpZnQnLCAnc21va2UnXSwgZmxvb2RlZFJ1aW5zOiBbJ2FuY2hvcicsICd3YXRlciddLCBjbGlmZnM6IFsncm9wZScsICdsZWRnZSddLCBidXJpYWw6IFsnY2Fpcm4nLCAnZ3JhdmVTb2lsJ10sIHNhbHRGbGF0czogWydzYWx0TWlycm9yJywgJ2JyaW5lJ10sIGZyb3N0UmVsaXF1YXJ5OiBbJ2ljZScsICdmcm9zdFJpbWUnXVxufVxuY29uc3QgaW1wcmludEVzY2FsYXRpb25MYW5kbWFyayA9IChmbG9vcjogRmxvb3IsIHJvb21zOiByZWFkb25seSBSb29tW10pOiB2b2lkID0+IHtcbiAgY29uc3Qgcm9vbSA9IHJvb21zWzFdID8/IHJvb21zWzBdXG4gIGlmICghcm9vbSkgcmV0dXJuXG4gIGNvbnN0IHRhcmdldCA9IGNlbnRlcihyb29tKVxuICBjb25zdCB2YXJpYW50ID0gTWF0aC5taW4oZXNjYWxhdGlvbkxhbmRtYXJrVGVycmFpbltmbG9vci5iaW9tZV0ubGVuZ3RoIC0gMSwgTWF0aC5mbG9vcigoZmxvb3IuZXNjYWxhdGlvbj8uZW5jb3VudGVyT2Zmc2V0ID8/IDApIC8gMikpXG4gIHNldEtpbmQoZmxvb3IsIHRhcmdldC54LCB0YXJnZXQueSwgZXNjYWxhdGlvbkxhbmRtYXJrVGVycmFpbltmbG9vci5iaW9tZV1bdmFyaWFudF0pXG59XG5cbmNvbnN0IGhhc05lYXJieVRpbGUgPSAoZmxvb3I6IEZsb29yLCBwb2ludDogUG9pbnQsIHJhZGl1czogbnVtYmVyLCBraW5kczogUmVhZG9ubHlTZXQ8VGlsZVsna2luZCddPik6IGJvb2xlYW4gPT4ge1xuICBmb3IgKGxldCB5ID0gcG9pbnQueSAtIHJhZGl1czsgeSA8PSBwb2ludC55ICsgcmFkaXVzOyB5KyspIGZvciAobGV0IHggPSBwb2ludC54IC0gcmFkaXVzOyB4IDw9IHBvaW50LnggKyByYWRpdXM7IHgrKykgaWYgKGtpbmRzLmhhcyhnZXRUaWxlKGZsb29yLCB4LCB5KT8ua2luZCA/PyAnd2FsbCcpKSByZXR1cm4gdHJ1ZVxuICByZXR1cm4gZmFsc2Vcbn1cbmNvbnN0IGhhc0FkamFjZW50VGlsZSA9IChmbG9vcjogRmxvb3IsIHBvaW50OiBQb2ludCwga2luZHM6IFJlYWRvbmx5U2V0PFRpbGVbJ2tpbmQnXT4pOiBib29sZWFuID0+IHtcbiAgZm9yIChsZXQgeSA9IHBvaW50LnkgLSAxOyB5IDw9IHBvaW50LnkgKyAxOyB5KyspIGZvciAobGV0IHggPSBwb2ludC54IC0gMTsgeCA8PSBwb2ludC54ICsgMTsgeCsrKSBpZiAoKHggIT09IHBvaW50LnggfHwgeSAhPT0gcG9pbnQueSkgJiYga2luZHMuaGFzKGdldFRpbGUoZmxvb3IsIHgsIHkpPy5raW5kID8/ICd3YWxsJykpIHJldHVybiB0cnVlXG4gIHJldHVybiBmYWxzZVxufVxuXG5jb25zdCBoYXNNaW5lUHJvcENvbnRleHQgPSAoZmxvb3I6IEZsb29yLCBraW5kOiBQcm9wWydraW5kJ10sIHBvaW50OiBQb2ludCk6IGJvb2xlYW4gPT4ge1xuICBpZiAoIWtpbmQuc3RhcnRzV2l0aCgnbWluZS4nKSkgcmV0dXJuIHRydWVcbiAgY29uc3Qgd29ya2VkUGFzc2FnZSA9IG5ldyBTZXQ8VGlsZVsna2luZCddPihbJ3JhaWwnLCAnc3VwcG9ydCddKVxuICBpZiAoa2luZCA9PT0gJ21pbmUub3JlVmVpbicpIHJldHVybiBoYXNOZWFyYnlUaWxlKGZsb29yLCBwb2ludCwgMSwgbmV3IFNldDxUaWxlWydraW5kJ10+KFsnc3VwcG9ydCcsICdydWJibGUnLCAnYm91bGRlciddKSlcbiAgaWYgKGtpbmQgPT09ICdtaW5lLmxhbnRlcm5Qb3N0JyB8fCBraW5kID09PSAnbWluZS5kaXNjYXJkZWRQYXJjZWwnKSByZXR1cm4gaGFzTmVhcmJ5VGlsZShmbG9vciwgcG9pbnQsIDEsIHdvcmtlZFBhc3NhZ2UpXG4gIGlmIChraW5kID09PSAnbWluZS5icm9rZW5DYXJ0JykgcmV0dXJuIGNhcmRpbmFsT2Zmc2V0cy5zb21lKChbeCwgeV0pID0+IGdldFRpbGUoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSk/LmtpbmQgPT09ICdyYWlsJylcbiAgaWYgKGtpbmQgPT09ICdtaW5lLndhcm5pbmdNYXJrZXInKSByZXR1cm4gaGFzTmVhcmJ5VGlsZShmbG9vciwgcG9pbnQsIDUsIG1pbmVIYXphcmRzKVxuICBpZiAoa2luZCA9PT0gJ21pbmUuc2t1bGxNYXJrZXInKSByZXR1cm4gaGFzTmVhcmJ5VGlsZShmbG9vciwgcG9pbnQsIDUsIG1pbmVIYXphcmRzKSB8fCBmbG9vci5hY3RvcnMuc29tZShhY3RvciA9PiBhY3Rvci5ob3N0aWxlICYmIE1hdGgubWF4KE1hdGguYWJzKGFjdG9yLnggLSBwb2ludC54KSwgTWF0aC5hYnMoYWN0b3IueSAtIHBvaW50LnkpKSA8PSA1KVxuICByZXR1cm4gdHJ1ZVxufVxuXG5jb25zdCBoYXNDYXZlcm5Qcm9wQ29udGV4dCA9IChmbG9vcjogRmxvb3IsIGtpbmQ6IFByb3BbJ2tpbmQnXSwgcG9pbnQ6IFBvaW50KTogYm9vbGVhbiA9PiB7XG4gIGlmICgha2luZC5zdGFydHNXaXRoKCdjYXZlcm5zLicpKSByZXR1cm4gdHJ1ZVxuICBjb25zdCBuZWFyV2F0ZXIgPSBoYXNBZGphY2VudFRpbGUoZmxvb3IsIHBvaW50LCBuZXcgU2V0PFRpbGVbJ2tpbmQnXT4oWyd3YXRlciddKSlcbiAgY29uc3QgbmVhckRhcmtuZXNzID0gaGFzQWRqYWNlbnRUaWxlKGZsb29yLCBwb2ludCwgbmV3IFNldDxUaWxlWydraW5kJ10+KFsnZGFya25lc3MnXSkpXG4gIGlmIChraW5kID09PSAnY2F2ZXJucy5jcnlzdGFsQ2x1c3RlcicgfHwga2luZCA9PT0gJ2NhdmVybnMuZ2xvd2luZ0Z1bmd1cycpIHJldHVybiBuZWFyRGFya25lc3NcbiAgaWYgKGtpbmQgPT09ICdjYXZlcm5zLmJhcm5hY2xlZFNocmluZScgfHwga2luZCA9PT0gJ2NhdmVybnMuYnJva2VuQm9hdCcgfHwga2luZCA9PT0gJ2NhdmVybnMuZWVsVHVubmVsJykgcmV0dXJuIG5lYXJXYXRlclxuICBpZiAoa2luZCA9PT0gJ2NhdmVybnMuc2VhbGVkUGFyY2VsJykgcmV0dXJuIG5lYXJXYXRlciB8fCBuZWFyRGFya25lc3NcbiAgcmV0dXJuIHRydWVcbn1cblxuY29uc3QgaGFzUHJvcENvbnRleHQgPSAoZmxvb3I6IEZsb29yLCBraW5kOiBQcm9wWydraW5kJ10sIHBvaW50OiBQb2ludCk6IGJvb2xlYW4gPT4gaGFzTWluZVByb3BDb250ZXh0KGZsb29yLCBraW5kLCBwb2ludCkgJiYgaGFzQ2F2ZXJuUHJvcENvbnRleHQoZmxvb3IsIGtpbmQsIHBvaW50KVxuXG5jb25zdCBjYXJ2ZVJlY3QgPSAoZmxvb3I6IEZsb29yLCByb29tOiBSb29tLCBraW5kOiBUaWxlWydraW5kJ10gPSAnZmxvb3InKTogdm9pZCA9PiB7XG4gIGZvciAobGV0IHkgPSByb29tLnk7IHkgPCByb29tLnkgKyByb29tLmg7IHkrKykgZm9yIChsZXQgeCA9IHJvb20ueDsgeCA8IHJvb20ueCArIHJvb20udzsgeCsrKSBzZXRLaW5kKGZsb29yLCB4LCB5LCBraW5kKVxufVxuY29uc3Qgd2FsbEJhbmQgPSAoZmxvb3I6IEZsb29yLCB2ZXJ0aWNhbDogYm9vbGVhbiwgYXQ6IG51bWJlciwgZ2FwOiBudW1iZXIsIHNwYW4gPSAyKTogdm9pZCA9PiB7XG4gIGNvbnN0IGxpbWl0ID0gdmVydGljYWwgPyBmbG9vci5oZWlnaHQgLSAxIDogZmxvb3Iud2lkdGggLSAxXG4gIGZvciAobGV0IG9mZnNldCA9IDE7IG9mZnNldCA8IGxpbWl0OyBvZmZzZXQrKykge1xuICAgIGlmIChNYXRoLmFicyhvZmZzZXQgLSBnYXApIDw9IHNwYW4pIGNvbnRpbnVlXG4gICAgc2V0S2luZChmbG9vciwgdmVydGljYWwgPyBhdCA6IG9mZnNldCwgdmVydGljYWwgPyBvZmZzZXQgOiBhdCwgJ3dhbGwnKVxuICB9XG59XG5jb25zdCBsYW5kbWFya1Jvb21zID0gKGZsb29yOiBGbG9vcik6IFJvb21bXSA9PiB7XG4gIGNvbnN0IHcgPSBNYXRoLm1heCg3LCBNYXRoLmZsb29yKGZsb29yLndpZHRoIC8gOCkpXG4gIGNvbnN0IGggPSBNYXRoLm1heCg2LCBNYXRoLmZsb29yKGZsb29yLmhlaWdodCAvIDcpKVxuICBjb25zdCByb29tcyA9IFtcbiAgICB7IHg6IDMsIHk6IE1hdGgubWF4KDMsIE1hdGguZmxvb3IoZmxvb3IuaGVpZ2h0ICogLjE4KSksIHcsIGggfSxcbiAgICB7IHg6IE1hdGguZmxvb3IoZmxvb3Iud2lkdGggKiAuMjcpLCB5OiBNYXRoLmZsb29yKGZsb29yLmhlaWdodCAqIC41OCksIHcsIGggfSxcbiAgICB7IHg6IE1hdGguZmxvb3IoZmxvb3Iud2lkdGggKiAuNTIpLCB5OiBNYXRoLmZsb29yKGZsb29yLmhlaWdodCAqIC4yMyksIHcsIGggfSxcbiAgICB7IHg6IGZsb29yLndpZHRoIC0gdyAtIDQsIHk6IE1hdGguZmxvb3IoZmxvb3IuaGVpZ2h0ICogLjYyKSwgdywgaCB9XG4gIF1cbiAgcm9vbXMuZm9yRWFjaChyb29tID0+IGNhcnZlUmVjdChmbG9vciwgcm9vbSkpXG4gIHJldHVybiByb29tc1xufVxuXG5jb25zdCBjYXJ2ZU1pbmVSb3V0ZUNvbnRyYWN0ID0gKGZsb29yOiBGbG9vciwgcm5nOiBSbmcpOiBSb29tW10gPT4ge1xuICBjb25zdCB2YXJpYW50ID0gZmxvb3IubGF5b3V0SWQucmVwbGFjZSgnLXJlbWl4JywgJycpXG4gIGNvbnN0IG1haW5ZID0gdmFyaWFudCA9PT0gJ2NvbGxhcHNlLWxvb3AnID8gMTMgOiBybmcuaW50KDEyLCAxNSlcbiAgY29uc3QgdXBwZXJZID0gdmFyaWFudCA9PT0gJ2JyYW5jaGluZy1kcmlmdHMnID8gMyA6IDVcbiAgY29uc3QgbG93ZXJZID0gdmFyaWFudCA9PT0gJ3JhaWwtc3BpbmUnID8gZmxvb3IuaGVpZ2h0IC0gMTAgOiBmbG9vci5oZWlnaHQgLSA5XG4gIGNvbnN0IHJvb21zOiBSb29tW10gPSBbXG4gICAgeyB4OiAyLCB5OiBtYWluWSwgdzogNiwgaDogNiB9LFxuICAgIHsgeDogMTAsIHk6IG1haW5ZLCB3OiA3LCBoOiA2IH0sXG4gICAgeyB4OiAyMCwgeTogbWFpblksIHc6IDYsIGg6IDYgfSxcbiAgICB7IHg6IDI4LCB5OiB1cHBlclksIHc6IDcsIGg6IDYgfSxcbiAgICB7IHg6IDI4LCB5OiBsb3dlclksIHc6IDcsIGg6IDYgfSxcbiAgICB7IHg6IDM3LCB5OiBsb3dlclksIHc6IDUsIGg6IDUgfSxcbiAgICB7IHg6IDM2LCB5OiBtYWluWSwgdzogNiwgaDogNiB9LFxuICAgIHsgeDogNDMsIHk6IG1haW5ZICsgMSwgdzogMywgaDogNCB9XG4gIF1cbiAgcm9vbXMuZm9yRWFjaChyb29tID0+IGNhcnZlUmVjdChmbG9vciwgcm9vbSkpXG4gIGNvbnN0IFtzdGFydCwgbGFuZG1hcmssIGZvcmssIHNhZmVSb3V0ZSwgcmlza1JvdXRlLCByZXdhcmQsIG9iamVjdGl2ZSwgZXhpdF0gPSByb29tcy5tYXAoY2VudGVyKVxuICBjb25zdCBjb25uZWN0ID0gKGZyb206IFBvaW50LCB0bzogUG9pbnQsIHZlcnRpY2FsRmlyc3QgPSBmYWxzZSkgPT4ge1xuICAgIGlmICh2ZXJ0aWNhbEZpcnN0KSB7IGNhcnZlVihmbG9vciwgZnJvbS55LCB0by55LCBmcm9tLngpOyBjYXJ2ZUgoZmxvb3IsIGZyb20ueCwgdG8ueCwgdG8ueSkgfVxuICAgIGVsc2UgeyBjYXJ2ZUgoZmxvb3IsIGZyb20ueCwgdG8ueCwgZnJvbS55KTsgY2FydmVWKGZsb29yLCBmcm9tLnksIHRvLnksIHRvLngpIH1cbiAgfVxuICBjb25uZWN0KHN0YXJ0LCBsYW5kbWFyaylcbiAgY29ubmVjdChsYW5kbWFyaywgZm9yaylcbiAgY29ubmVjdChmb3JrLCBzYWZlUm91dGUsIHZhcmlhbnQgPT09ICdicmFuY2hpbmctZHJpZnRzJylcbiAgY29ubmVjdChmb3JrLCByaXNrUm91dGUsIHRydWUpXG4gIGNvbm5lY3Qoc2FmZVJvdXRlLCBvYmplY3RpdmUsIHRydWUpXG4gIGNvbm5lY3Qocmlza1JvdXRlLCBvYmplY3RpdmUpXG4gIGNvbm5lY3Qocmlza1JvdXRlLCByZXdhcmQsIHZhcmlhbnQgPT09ICdjb2xsYXBzZS1sb29wJylcbiAgY29ubmVjdChyZXdhcmQsIG9iamVjdGl2ZSwgdHJ1ZSlcbiAgY29ubmVjdChvYmplY3RpdmUsIGV4aXQpXG4gIHJldHVybiByb29tc1xufVxuXG5jb25zdCBjYXJ2ZUJpb21lTGF5b3V0ID0gKGZsb29yOiBGbG9vciwgcm5nOiBSbmcpOiBSb29tW10gPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdtaW5lJykge1xuICAgIHJldHVybiBjYXJ2ZU1pbmVSb3V0ZUNvbnRyYWN0KGZsb29yLCBybmcpXG4gIH1cbiAgY2FydmVSZWN0KGZsb29yLCB7IHg6IDEsIHk6IDEsIHc6IGZsb29yLndpZHRoIC0gMiwgaDogZmxvb3IuaGVpZ2h0IC0gMiB9KVxuICBjb25zdCByb29tcyA9IGxhbmRtYXJrUm9vbXMoZmxvb3IpXG4gIGNvbnN0IHZhcmlhbnQgPSBmbG9vci5sYXlvdXRJZC5yZXBsYWNlKCctcmVtaXgnLCAnJylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnd2lsZHMnKSB7XG4gICAgaWYgKHZhcmlhbnQgPT09ICdyb290LW1hemUnKSBmb3IgKGxldCB4ID0gMTE7IHggPCBmbG9vci53aWR0aCAtIDg7IHggKz0gMTApIHdhbGxCYW5kKGZsb29yLCB0cnVlLCB4LCBybmcuaW50KDQsIGZsb29yLmhlaWdodCAtIDUpLCAyKVxuICAgIGlmICh2YXJpYW50ID09PSAnd2V0bGFuZC1jYXVzZXdheXMnKSBmb3IgKGxldCB5ID0gOTsgeSA8IGZsb29yLmhlaWdodCAtIDc7IHkgKz0gOSkgd2FsbEJhbmQoZmxvb3IsIGZhbHNlLCB5LCBybmcuaW50KDUsIGZsb29yLndpZHRoIC0gNiksIDMpXG4gIH0gZWxzZSBpZiAoZmxvb3IuYmlvbWUgPT09ICdjYXZlcm5zJykge1xuICAgIGZvciAobGV0IHggPSAxMDsgeCA8IGZsb29yLndpZHRoIC0gODsgeCArPSAxMSkgd2FsbEJhbmQoZmxvb3IsIHRydWUsIHgsIHJuZy5pbnQoNSwgZmxvb3IuaGVpZ2h0IC0gNiksIHZhcmlhbnQgPT09ICdmYXVsdC10dW5uZWxzJyA/IDEgOiAzKVxuICAgIGlmICh2YXJpYW50ID09PSAnc2lua2hvbGUtZ2FsbGVyaWVzJykgZm9yIChsZXQgeSA9IDg7IHkgPCBmbG9vci5oZWlnaHQgLSA2OyB5ICs9IDEwKSB3YWxsQmFuZChmbG9vciwgZmFsc2UsIHksIHJuZy5pbnQoNSwgZmxvb3Iud2lkdGggLSA2KSwgMilcbiAgfSBlbHNlIGlmIChmbG9vci5iaW9tZSA9PT0gJ3J1aW5zJykge1xuICAgIGZvciAobGV0IHggPSA5OyB4IDwgZmxvb3Iud2lkdGggLSA2OyB4ICs9IDkpIHdhbGxCYW5kKGZsb29yLCB0cnVlLCB4LCBybmcuaW50KDQsIGZsb29yLmhlaWdodCAtIDUpLCAyKVxuICAgIGlmICh2YXJpYW50ICE9PSAnYnJlYWNoZWQtcHJlY2luY3QnKSBmb3IgKGxldCB5ID0gODsgeSA8IGZsb29yLmhlaWdodCAtIDY7IHkgKz0gMTApIHdhbGxCYW5kKGZsb29yLCBmYWxzZSwgeSwgcm5nLmludCg1LCBmbG9vci53aWR0aCAtIDYpLCAyKVxuICB9IGVsc2UgaWYgKGZsb29yLmJpb21lID09PSAnZnVybmFjZScpIHtcbiAgICBmb3IgKGxldCB5ID0gNzsgeSA8IGZsb29yLmhlaWdodCAtIDU7IHkgKz0gNykgd2FsbEJhbmQoZmxvb3IsIGZhbHNlLCB5LCBybmcuaW50KDUsIGZsb29yLndpZHRoIC0gNiksIHZhcmlhbnQgPT09ICdraWxuLXRlcnJhY2VzJyA/IDIgOiA0KVxuICB9IGVsc2UgaWYgKGZsb29yLmJpb21lID09PSAnZmxvb2RlZFJ1aW5zJykge1xuICAgIGZvciAobGV0IHggPSAxMjsgeCA8IGZsb29yLndpZHRoIC0gODsgeCArPSAxNCkgd2FsbEJhbmQoZmxvb3IsIHRydWUsIHgsIHJuZy5pbnQoNSwgZmxvb3IuaGVpZ2h0IC0gNiksIDQpXG4gIH0gZWxzZSBpZiAoZmxvb3IuYmlvbWUgPT09ICdjbGlmZnMnKSB7XG4gICAgZm9yIChsZXQgeSA9IDg7IHkgPCBmbG9vci5oZWlnaHQgLSA2OyB5ICs9IDkpIHdhbGxCYW5kKGZsb29yLCBmYWxzZSwgeSwgcm5nLmludCg1LCBmbG9vci53aWR0aCAtIDYpLCB2YXJpYW50ID09PSAncmF2aW5lLXN3aXRjaGJhY2tzJyA/IDEgOiAzKVxuICB9IGVsc2UgaWYgKGZsb29yLmJpb21lID09PSAnYnVyaWFsJykge1xuICAgIGlmICh2YXJpYW50ID09PSAncmluZ2VkLW5lY3JvcG9saXMnKSBmb3IgKGxldCB4ID0gMTQ7IHggPCBmbG9vci53aWR0aCAtIDEwOyB4ICs9IDE4KSB3YWxsQmFuZChmbG9vciwgdHJ1ZSwgeCwgcm5nLmludCg2LCBmbG9vci5oZWlnaHQgLSA3KSwgNSlcbiAgfSBlbHNlIGlmIChmbG9vci5iaW9tZSA9PT0gJ3NhbHRGbGF0cycpIHtcbiAgICBpZiAodmFyaWFudCA9PT0gJ2JyaW5lLWZyYWN0dXJlcycpIGZvciAobGV0IHggPSAxMjsgeCA8IGZsb29yLndpZHRoIC0gODsgeCArPSAxMikgd2FsbEJhbmQoZmxvb3IsIHRydWUsIHgsIHJuZy5pbnQoNSwgZmxvb3IuaGVpZ2h0IC0gNiksIDQpXG4gIH0gZWxzZSBpZiAoZmxvb3IuYmlvbWUgPT09ICdmcm9zdFJlbGlxdWFyeScpIHtcbiAgICBmb3IgKGxldCB5ID0gOTsgeSA8IGZsb29yLmhlaWdodCAtIDc7IHkgKz0gMTApIHdhbGxCYW5kKGZsb29yLCBmYWxzZSwgeSwgcm5nLmludCg1LCBmbG9vci53aWR0aCAtIDYpLCB2YXJpYW50ID09PSAncmVsaXF1YXJ5LWVzY2FycG1lbnQnID8gMiA6IDUpXG4gIH1cbiAgcm9vbXMuZm9yRWFjaChyb29tID0+IGNhcnZlUmVjdChmbG9vciwgcm9vbSkpXG4gIGNvbm5lY3RSb29tcyhmbG9vciwgcm9vbXMpXG4gIGltcHJpbnRCaW9tZUxhbmRtYXJrcyhmbG9vciwgcm5nLCByb29tcylcbiAgcmV0dXJuIHJvb21zXG59XG5cbmNvbnN0IGNhcnZlTGVnYWN5TGF5b3V0RnJvbVJvdXRlQ29udHJhY3QgPSAoZmxvb3I6IEZsb29yLCBjb250cmFjdDogUmV0dXJuVHlwZTx0eXBlb2YgZ2VuZXJhdGVSb3V0ZUNvbnRyYWN0Piwgcm5nOiBSbmcpOiBSb29tW10gPT4ge1xuICBpZiAoY29udHJhY3QuYmlvbWUgIT09IGZsb29yLmJpb21lKSB0aHJvdyBuZXcgRXJyb3IoYHJvdXRlIGNvbnRyYWN0ICR7Y29udHJhY3QuaWR9IGJpb21lIGRvZXMgbm90IG1hdGNoIGZsb29yIGJpb21lYClcbiAgaWYgKGNvbnRyYWN0LnJlY2lwZUlkICE9PSBmbG9vci5sYXlvdXRJZCkgdGhyb3cgbmV3IEVycm9yKGByb3V0ZSBjb250cmFjdCAke2NvbnRyYWN0LmlkfSByZWNpcGUgZG9lcyBub3QgbWF0Y2ggZmxvb3IgbGF5b3V0YClcbiAgcmV0dXJuIGNhcnZlQmlvbWVMYXlvdXQoZmxvb3IsIHJuZylcbn1cblxuY29uc3QgY2FydmVSb3V0ZUNvbnRyYWN0TGF5b3V0ID0gKGZsb29yOiBGbG9vciwgY29udHJhY3Q6IFJldHVyblR5cGU8dHlwZW9mIGdlbmVyYXRlUm91dGVDb250cmFjdD4sIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBybmc6IFJuZyk6IFJvb21bXSA9PiB7XG4gIGlmICghbWFjcm9SZWNpcGVGb3IoY29udHJhY3QpLnBpbG90KSByZXR1cm4gY2FydmVMZWdhY3lMYXlvdXRGcm9tUm91dGVDb250cmFjdChmbG9vciwgY29udHJhY3QsIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnd2lsZHMnKSByZXR1cm4gY2FydmVXaWxkc1JvdXRlQ29udHJhY3RMYXlvdXQoZmxvb3IsIGNvbnRyYWN0LCBtYWNybywgcm5nKVxuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdjYXZlcm5zJykgcmV0dXJuIGNhcnZlQ2F2ZXJuc1JvdXRlQ29udHJhY3RMYXlvdXQoZmxvb3IsIGNvbnRyYWN0LCBtYWNybywgcm5nKVxuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdydWlucycpIHJldHVybiBjYXJ2ZVJ1aW5zUm91dGVDb250cmFjdExheW91dChmbG9vciwgY29udHJhY3QsIG1hY3JvLCBybmcpXG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2Z1cm5hY2UnKSByZXR1cm4gY2FydmVGdXJuYWNlUm91dGVDb250cmFjdExheW91dChmbG9vciwgY29udHJhY3QsIG1hY3JvLCBybmcpXG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2Zsb29kZWRSdWlucycpIHJldHVybiBjYXJ2ZUZsb29kZWRSb3V0ZUNvbnRyYWN0TGF5b3V0KGZsb29yLCBjb250cmFjdCwgbWFjcm8sIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnY2xpZmZzJykgcmV0dXJuIGNhcnZlQ2xpZmZzUm91dGVDb250cmFjdExheW91dChmbG9vciwgY29udHJhY3QsIG1hY3JvLCBybmcpXG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2J1cmlhbCcpIHJldHVybiBjYXJ2ZUJ1cmlhbFJvdXRlQ29udHJhY3RMYXlvdXQoZmxvb3IsIGNvbnRyYWN0LCBtYWNybywgcm5nKVxuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdzYWx0RmxhdHMnKSByZXR1cm4gY2FydmVTYWx0RmxhdHNSb3V0ZUNvbnRyYWN0TGF5b3V0KGZsb29yLCBjb250cmFjdCwgbWFjcm8sIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnZnJvc3RSZWxpcXVhcnknKSByZXR1cm4gY2FydmVGcm9zdFJlbGlxdWFyeVJvdXRlQ29udHJhY3RMYXlvdXQoZmxvb3IsIGNvbnRyYWN0LCBtYWNybywgcm5nKVxuICBjb25zdCB0b1Jvb20gPSAobm9kZTogTWFjcm9SZWNpcGVEZWJ1Z1snbm9kZXMnXVtudW1iZXJdKTogUm9vbSA9PiAoeyB4OiBub2RlLmZvb3RwcmludC54LCB5OiBub2RlLmZvb3RwcmludC55LCB3OiBub2RlLmZvb3RwcmludC53aWR0aCwgaDogbm9kZS5mb290cHJpbnQuaGVpZ2h0IH0pXG4gIGNvbnN0IHJvb21zID0gbWFjcm8ubm9kZXMubWFwKHRvUm9vbSlcbiAgZm9yIChjb25zdCByb29tIG9mIHJvb21zKSBjYXJ2ZVJlY3QoZmxvb3IsIHJvb20pXG4gIGZvciAoY29uc3QgcG9pbnQgb2YgbWFjcm9Db25uZWN0b3JQb2ludHMobWFjcm8pKSBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCAnZmxvb3InKVxuICBjb25zdCBieUtpbmQgPSBuZXcgTWFwKG1hY3JvLm5vZGVzLm1hcChub2RlID0+IFtub2RlLmtpbmQsIHRvUm9vbShub2RlKV0pKVxuICBjb25zdCBvcmRlcmVkOiBSb3V0ZU5vZGVLaW5kW10gPSBbJ3N0YXJ0JywgJ2xhbmRtYXJrJywgJ2ZvcmsnLCAnb3B0aW9uYWxSZXdhcmQnLCAnb2JqZWN0aXZlJywgZmxvb3IuaW5kZXggJSA0ID09PSAzID8gJ2Jvc3MnIDogJ2V4aXQnXVxuICByZXR1cm4gb3JkZXJlZC5tYXAoa2luZCA9PiBieUtpbmQuZ2V0KGtpbmQpKS5maWx0ZXIoKHJvb20pOiByb29tIGlzIFJvb20gPT4gQm9vbGVhbihyb29tKSlcbn1cblxuY29uc3QgY2FydmVXaWxkc1JvdXRlQ29udHJhY3RMYXlvdXQgPSAoZmxvb3I6IEZsb29yLCBjb250cmFjdDogUmV0dXJuVHlwZTx0eXBlb2YgZ2VuZXJhdGVSb3V0ZUNvbnRyYWN0PiwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcsIHJuZzogUm5nKTogUm9vbVtdID0+IHtcbiAgaWYgKGNvbnRyYWN0LmJpb21lICE9PSBmbG9vci5iaW9tZSkgdGhyb3cgbmV3IEVycm9yKGByb3V0ZSBjb250cmFjdCAke2NvbnRyYWN0LmlkfSBiaW9tZSBkb2VzIG5vdCBtYXRjaCBmbG9vciBiaW9tZWApXG4gIGlmIChjb250cmFjdC5yZWNpcGVJZCAhPT0gZmxvb3IubGF5b3V0SWQpIHRocm93IG5ldyBFcnJvcihgcm91dGUgY29udHJhY3QgJHtjb250cmFjdC5pZH0gcmVjaXBlIGRvZXMgbm90IG1hdGNoIGZsb29yIGxheW91dGApXG4gIGNvbnN0IHRvUm9vbSA9IChub2RlOiBNYWNyb1JlY2lwZURlYnVnWydub2RlcyddW251bWJlcl0pOiBSb29tID0+ICh7IHg6IG5vZGUuZm9vdHByaW50LngsIHk6IG5vZGUuZm9vdHByaW50LnksIHc6IG5vZGUuZm9vdHByaW50LndpZHRoLCBoOiBub2RlLmZvb3RwcmludC5oZWlnaHQgfSlcbiAgY29uc3Qgcm9vbXMgPSBtYWNyby5ub2Rlcy5tYXAodG9Sb29tKVxuICBjb25zdCBjYXJ2ZUNsZWFyaW5nID0gKHJvb206IFJvb20pID0+IHtcbiAgICBjb25zdCBjZW50ZXIgPSB7IHg6IHJvb20ueCArIE1hdGguZmxvb3Iocm9vbS53IC8gMiksIHk6IHJvb20ueSArIE1hdGguZmxvb3Iocm9vbS5oIC8gMikgfVxuICAgIGNvbnN0IHJhZGl1c1ggPSByb29tLncgLyAyICsgMlxuICAgIGNvbnN0IHJhZGl1c1kgPSByb29tLmggLyAyICsgMlxuICAgIGZvciAobGV0IHkgPSByb29tLnkgLSAyOyB5IDwgcm9vbS55ICsgcm9vbS5oICsgMjsgeSsrKSBmb3IgKGxldCB4ID0gcm9vbS54IC0gMjsgeCA8IHJvb20ueCArIHJvb20udyArIDI7IHgrKykge1xuICAgICAgY29uc3QgZGlzdGFuY2UgPSAoKHggLSBjZW50ZXIueCkgLyByYWRpdXNYKSAqKiAyICsgKCh5IC0gY2VudGVyLnkpIC8gcmFkaXVzWSkgKiogMlxuICAgICAgaWYgKGRpc3RhbmNlIDwgMSArIChybmcuY2hhbmNlKDIyKSA/IC4xOCA6IC0uMDgpKSBzZXRLaW5kKGZsb29yLCB4LCB5LCAnZmxvb3InKVxuICAgIH1cbiAgICBjYXJ2ZVJlY3QoZmxvb3IsIHJvb20pXG4gIH1cbiAgY29uc3QgY2FydmVUcmFpbCA9IChwb2ludDogUG9pbnQpID0+IHtcbiAgICBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCAnZmxvb3InKVxuICAgIGZvciAoY29uc3QgW3gsIHldIG9mIGNhcmRpbmFsT2Zmc2V0cykgaWYgKHJuZy5jaGFuY2UoNjIpKSBzZXRLaW5kKGZsb29yLCBwb2ludC54ICsgeCwgcG9pbnQueSArIHksICdmbG9vcicpXG4gICAgaWYgKHJuZy5jaGFuY2UoMjgpKSBmb3IgKGNvbnN0IFt4LCB5XSBvZiBbWy0xLCAtMV0sIFsxLCAtMV0sIFstMSwgMV0sIFsxLCAxXV0pIHNldEtpbmQoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSwgJ2Zsb29yJylcbiAgfVxuICByb29tcy5mb3JFYWNoKGNhcnZlQ2xlYXJpbmcpXG4gIG1hY3JvQ29ubmVjdG9yUG9pbnRzKG1hY3JvKS5mb3JFYWNoKGNhcnZlVHJhaWwpXG4gIGNvbnN0IGJ5S2luZCA9IG5ldyBNYXAobWFjcm8ubm9kZXMubWFwKG5vZGUgPT4gW25vZGUua2luZCwgdG9Sb29tKG5vZGUpXSkpXG4gIGNvbnN0IG9yZGVyZWQ6IFJvdXRlTm9kZUtpbmRbXSA9IFsnc3RhcnQnLCAnbGFuZG1hcmsnLCAnZm9yaycsICdvcHRpb25hbFJld2FyZCcsICdvYmplY3RpdmUnLCBmbG9vci5pbmRleCAlIDQgPT09IDMgPyAnYm9zcycgOiAnZXhpdCddXG4gIHJldHVybiBvcmRlcmVkLm1hcChraW5kID0+IGJ5S2luZC5nZXQoa2luZCkpLmZpbHRlcigocm9vbSk6IHJvb20gaXMgUm9vbSA9PiBCb29sZWFuKHJvb20pKVxufVxuXG5jb25zdCBjYXJ2ZUNhdmVybnNSb3V0ZUNvbnRyYWN0TGF5b3V0ID0gKGZsb29yOiBGbG9vciwgY29udHJhY3Q6IFJldHVyblR5cGU8dHlwZW9mIGdlbmVyYXRlUm91dGVDb250cmFjdD4sIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBybmc6IFJuZyk6IFJvb21bXSA9PiB7XG4gIGlmIChjb250cmFjdC5iaW9tZSAhPT0gZmxvb3IuYmlvbWUpIHRocm93IG5ldyBFcnJvcihgcm91dGUgY29udHJhY3QgJHtjb250cmFjdC5pZH0gYmlvbWUgZG9lcyBub3QgbWF0Y2ggZmxvb3IgYmlvbWVgKVxuICBpZiAoY29udHJhY3QucmVjaXBlSWQgIT09IGZsb29yLmxheW91dElkKSB0aHJvdyBuZXcgRXJyb3IoYHJvdXRlIGNvbnRyYWN0ICR7Y29udHJhY3QuaWR9IHJlY2lwZSBkb2VzIG5vdCBtYXRjaCBmbG9vciBsYXlvdXRgKVxuICBjb25zdCB0b1Jvb20gPSAobm9kZTogTWFjcm9SZWNpcGVEZWJ1Z1snbm9kZXMnXVtudW1iZXJdKTogUm9vbSA9PiAoeyB4OiBub2RlLmZvb3RwcmludC54LCB5OiBub2RlLmZvb3RwcmludC55LCB3OiBub2RlLmZvb3RwcmludC53aWR0aCwgaDogbm9kZS5mb290cHJpbnQuaGVpZ2h0IH0pXG4gIGNvbnN0IHJvb21zID0gbWFjcm8ubm9kZXMubWFwKHRvUm9vbSlcbiAgY29uc3QgY2FydmVDaGFtYmVyID0gKHJvb206IFJvb20sIGxhcmdlID0gZmFsc2UpID0+IHtcbiAgICBjb25zdCBjZW50ZXIgPSB7IHg6IHJvb20ueCArIE1hdGguZmxvb3Iocm9vbS53IC8gMiksIHk6IHJvb20ueSArIE1hdGguZmxvb3Iocm9vbS5oIC8gMikgfVxuICAgIGNvbnN0IHJhZGl1c1ggPSByb29tLncgLyAyICsgKGxhcmdlID8gNSA6IDIpXG4gICAgY29uc3QgcmFkaXVzWSA9IHJvb20uaCAvIDIgKyAobGFyZ2UgPyA0IDogMilcbiAgICBmb3IgKGxldCB5ID0gTWF0aC5mbG9vcihjZW50ZXIueSAtIHJhZGl1c1kpOyB5IDw9IE1hdGguY2VpbChjZW50ZXIueSArIHJhZGl1c1kpOyB5KyspIGZvciAobGV0IHggPSBNYXRoLmZsb29yKGNlbnRlci54IC0gcmFkaXVzWCk7IHggPD0gTWF0aC5jZWlsKGNlbnRlci54ICsgcmFkaXVzWCk7IHgrKykge1xuICAgICAgY29uc3QgZGlzdGFuY2UgPSAoKHggLSBjZW50ZXIueCkgLyByYWRpdXNYKSAqKiAyICsgKCh5IC0gY2VudGVyLnkpIC8gcmFkaXVzWSkgKiogMlxuICAgICAgaWYgKGRpc3RhbmNlIDwgMSArIChybmcuY2hhbmNlKDE4KSA/IC4xMiA6IC0uMDYpKSBzZXRLaW5kKGZsb29yLCB4LCB5LCAnZmxvb3InKVxuICAgIH1cbiAgICBjYXJ2ZVJlY3QoZmxvb3IsIHJvb20pXG4gIH1cbiAgcm9vbXMuZm9yRWFjaCgocm9vbSwgaW5kZXgpID0+IHtcbiAgICBjb25zdCBub2RlID0gbWFjcm8ubm9kZXNbaW5kZXhdIVxuICAgIGNhcnZlQ2hhbWJlcihyb29tLCBub2RlLmtpbmQgPT09ICdvcHRpb25hbFJld2FyZCcgfHwgbm9kZS5raW5kID09PSAnbGFuZG1hcmsnICYmIGZsb29yLmluZGV4ICUgNCA+IDEpXG4gIH0pXG4gIGZvciAoY29uc3QgcG9pbnQgb2YgbWFjcm9Db25uZWN0b3JQb2ludHMobWFjcm8pKSB7XG4gICAgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2Zsb29yJylcbiAgICBmb3IgKGNvbnN0IFt4LCB5XSBvZiBjYXJkaW5hbE9mZnNldHMpIGlmIChybmcuY2hhbmNlKDgyKSkgc2V0S2luZChmbG9vciwgcG9pbnQueCArIHgsIHBvaW50LnkgKyB5LCAnZmxvb3InKVxuICB9XG4gIGNvbnN0IGJ5S2luZCA9IG5ldyBNYXAobWFjcm8ubm9kZXMubWFwKG5vZGUgPT4gW25vZGUua2luZCwgdG9Sb29tKG5vZGUpXSkpXG4gIGNvbnN0IG9yZGVyZWQ6IFJvdXRlTm9kZUtpbmRbXSA9IFsnc3RhcnQnLCAnbGFuZG1hcmsnLCAnZm9yaycsICdvcHRpb25hbFJld2FyZCcsICdvYmplY3RpdmUnLCBmbG9vci5pbmRleCAlIDQgPT09IDMgPyAnYm9zcycgOiAnZXhpdCddXG4gIHJldHVybiBvcmRlcmVkLm1hcChraW5kID0+IGJ5S2luZC5nZXQoa2luZCkpLmZpbHRlcigocm9vbSk6IHJvb20gaXMgUm9vbSA9PiBCb29sZWFuKHJvb20pKVxufVxuXG5jb25zdCBjYXJ2ZVJ1aW5zUmluZyA9IChmbG9vcjogRmxvb3IsIGNlbnRlcjogUG9pbnQsIGhhbGZXaWR0aDogbnVtYmVyLCBoYWxmSGVpZ2h0OiBudW1iZXIsIGdhdGVzOiByZWFkb25seSBQb2ludFtdKTogdm9pZCA9PiB7XG4gIGNvbnN0IGdhdGVLZXlzID0gbmV3IFNldChnYXRlcy5tYXAocG9pbnRLZXkpKVxuICBmb3IgKGxldCB4ID0gY2VudGVyLnggLSBoYWxmV2lkdGg7IHggPD0gY2VudGVyLnggKyBoYWxmV2lkdGg7IHgrKykgZm9yIChjb25zdCB5IG9mIFtjZW50ZXIueSAtIGhhbGZIZWlnaHQsIGNlbnRlci55ICsgaGFsZkhlaWdodF0pIGlmICghZ2F0ZUtleXMuaGFzKHBvaW50S2V5KHsgeCwgeSB9KSkpIHNldEtpbmQoZmxvb3IsIHgsIHksICd3YWxsJylcbiAgZm9yIChsZXQgeSA9IGNlbnRlci55IC0gaGFsZkhlaWdodCArIDE7IHkgPCBjZW50ZXIueSArIGhhbGZIZWlnaHQ7IHkrKykgZm9yIChjb25zdCB4IG9mIFtjZW50ZXIueCAtIGhhbGZXaWR0aCwgY2VudGVyLnggKyBoYWxmV2lkdGhdKSBpZiAoIWdhdGVLZXlzLmhhcyhwb2ludEtleSh7IHgsIHkgfSkpKSBzZXRLaW5kKGZsb29yLCB4LCB5LCAnd2FsbCcpXG59XG5cbmNvbnN0IGNhcnZlUml0dWFsQXJjID0gKGZsb29yOiBGbG9vciwgY2VudGVyOiBQb2ludCwgcmFkaXVzWDogbnVtYmVyLCByYWRpdXNZOiBudW1iZXIsIGdhcEFuZ2xlOiBudW1iZXIpOiBQb2ludFtdID0+IHtcbiAgY29uc3QgcmluZzogUG9pbnRbXSA9IFtdXG4gIGZvciAobGV0IHkgPSBjZW50ZXIueSAtIHJhZGl1c1kgLSAxOyB5IDw9IGNlbnRlci55ICsgcmFkaXVzWSArIDE7IHkrKykgZm9yIChsZXQgeCA9IGNlbnRlci54IC0gcmFkaXVzWCAtIDE7IHggPD0gY2VudGVyLnggKyByYWRpdXNYICsgMTsgeCsrKSB7XG4gICAgY29uc3QgZHggPSAoeCAtIGNlbnRlci54KSAvIHJhZGl1c1hcbiAgICBjb25zdCBkeSA9ICh5IC0gY2VudGVyLnkpIC8gcmFkaXVzWVxuICAgIGNvbnN0IGRpc3RhbmNlID0gZHggKiBkeCArIGR5ICogZHlcbiAgICBjb25zdCBhbmdsZSA9IE1hdGguYXRhbjIoZHksIGR4KVxuICAgIGlmIChkaXN0YW5jZSA8IC44IHx8IGRpc3RhbmNlID4gMS4yMiB8fCBNYXRoLmFicyhhbmdsZSAtIGdhcEFuZ2xlKSA8IC4yMiB8fCBNYXRoLmFicyhNYXRoLmFicyhhbmdsZSkgLSBNYXRoLlBJKSA8IC4xNikgY29udGludWVcbiAgICBzZXRLaW5kKGZsb29yLCB4LCB5LCAnd2FsbCcpXG4gICAgcmluZy5wdXNoKHsgeCwgeSB9KVxuICB9XG4gIHJldHVybiByaW5nXG59XG5cbmNvbnN0IGNhcnZlUml0dWFsQW5uZXggPSAoZmxvb3I6IEZsb29yLCBjZW50ZXI6IFBvaW50KTogUG9pbnRbXSA9PiB7XG4gIGNvbnN0IHBvaW50czogUG9pbnRbXSA9IFtdXG4gIGZvciAobGV0IG9mZnNldCA9IDA7IG9mZnNldCA8IDc7IG9mZnNldCsrKSBmb3IgKGNvbnN0IHBvaW50IG9mIFt7IHg6IGNlbnRlci54ICsgOSArIG9mZnNldCwgeTogY2VudGVyLnkgLSAzICsgb2Zmc2V0IH0sIHsgeDogY2VudGVyLnggKyA5ICsgb2Zmc2V0LCB5OiBjZW50ZXIueSArIDMgLSBvZmZzZXQgfSwgeyB4OiBjZW50ZXIueCArIDE1LCB5OiBjZW50ZXIueSAtIDMgKyBvZmZzZXQgfV0pIHtcbiAgICBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCAnd2FsbCcpXG4gICAgcG9pbnRzLnB1c2gocG9pbnQpXG4gIH1cbiAgcmV0dXJuIHBvaW50c1xufVxuXG5jb25zdCBjYXJ2ZVJ1aW5zUm91dGVDb250cmFjdExheW91dCA9IChmbG9vcjogRmxvb3IsIGNvbnRyYWN0OiBSZXR1cm5UeXBlPHR5cGVvZiBnZW5lcmF0ZVJvdXRlQ29udHJhY3Q+LCBtYWNybzogTWFjcm9SZWNpcGVEZWJ1ZywgX3JuZzogUm5nKTogUm9vbVtdID0+IHtcbiAgaWYgKGNvbnRyYWN0LmJpb21lICE9PSBmbG9vci5iaW9tZSkgdGhyb3cgbmV3IEVycm9yKGByb3V0ZSBjb250cmFjdCAke2NvbnRyYWN0LmlkfSBiaW9tZSBkb2VzIG5vdCBtYXRjaCBmbG9vciBiaW9tZWApXG4gIGlmIChjb250cmFjdC5yZWNpcGVJZCAhPT0gZmxvb3IubGF5b3V0SWQpIHRocm93IG5ldyBFcnJvcihgcm91dGUgY29udHJhY3QgJHtjb250cmFjdC5pZH0gcmVjaXBlIGRvZXMgbm90IG1hdGNoIGZsb29yIGxheW91dGApXG4gIGNhcnZlUmVjdChmbG9vciwgeyB4OiAxLCB5OiAxLCB3OiBmbG9vci53aWR0aCAtIDIsIGg6IGZsb29yLmhlaWdodCAtIDIgfSlcbiAgY29uc3QgdG9Sb29tID0gKG5vZGU6IE1hY3JvUmVjaXBlRGVidWdbJ25vZGVzJ11bbnVtYmVyXSk6IFJvb20gPT4gKHsgeDogbm9kZS5mb290cHJpbnQueCwgeTogbm9kZS5mb290cHJpbnQueSwgdzogbm9kZS5mb290cHJpbnQud2lkdGgsIGg6IG5vZGUuZm9vdHByaW50LmhlaWdodCB9KVxuICBjb25zdCByaXR1YWxOb2RlID0gbWFjcm8ubm9kZXMuZmluZChub2RlID0+IG5vZGUua2luZCA9PT0gJ29iamVjdGl2ZScpXG4gIGlmICghcml0dWFsTm9kZSkgdGhyb3cgbmV3IEVycm9yKGBtaXNzaW5nIFJ1aW5zIHJpdHVhbCBub2RlOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGNvbnN0IHJpdHVhbCA9IGNlbnRlcih0b1Jvb20ocml0dWFsTm9kZSkpXG4gIGNvbnN0IGFwcHJvYWNoTm9kZSA9IG1hY3JvLm5vZGVzLmZpbmQobm9kZSA9PiBub2RlLmtpbmQgPT09ICdsYW5kbWFyaycpXG4gIGNvbnN0IGFwcHJvYWNoID0gYXBwcm9hY2hOb2RlID8gY2VudGVyKHRvUm9vbShhcHByb2FjaE5vZGUpKSA6IHJpdHVhbFxuICBjb25zdCBvdXRlclJpbmcgPSBjYXJ2ZVJpdHVhbEFyYyhmbG9vciwgcml0dWFsLCAxMywgOSwgLU1hdGguUEkgLyAyKVxuICBjb25zdCBpbm5lclJpbmcgPSBjYXJ2ZVJpdHVhbEFyYyhmbG9vciwgcml0dWFsLCA2LCA0LCAwKVxuICBjb25zdCBhbm5leCA9IGNhcnZlUml0dWFsQW5uZXgoZmxvb3IsIHJpdHVhbClcbiAgZmxvb3Iucml0dWFsTGF5b3V0ID0geyBhcHByb2FjaCwgY2VudGVyOiByaXR1YWwsIG91dGVyUmluZywgaW5uZXJSaW5nLCBhbm5leCB9IHNhdGlzZmllcyBSaXR1YWxMYXlvdXRcbiAgY29uc3QgdmFyaWFudCA9IGZsb29yLmxheW91dElkLnJlcGxhY2UoJy1yZW1peCcsICcnKVxuICBpZiAodmFyaWFudCA9PT0gJ2NpcmN1bGFyLXByZWNpbmN0JykgY2FydmVSdWluc1JpbmcoZmxvb3IsIHJpdHVhbCwgOSwgNywgW3sgeDogcml0dWFsLngsIHk6IHJpdHVhbC55IC0gNyB9LCB7IHg6IHJpdHVhbC54ICsgOSwgeTogcml0dWFsLnkgfSwgeyB4OiByaXR1YWwueCwgeTogcml0dWFsLnkgKyA3IH0sIHsgeDogcml0dWFsLnggLSA5LCB5OiByaXR1YWwueSB9XSlcbiAgaWYgKHZhcmlhbnQgPT09ICdicm9rZW4tcHJvY2Vzc2lvbmFsLWxvb3AnKSB7XG4gICAgY2FydmVSdWluc1JpbmcoZmxvb3IsIHJpdHVhbCwgOCwgNiwgW3sgeDogcml0dWFsLngsIHk6IHJpdHVhbC55IC0gNiB9LCB7IHg6IHJpdHVhbC54IC0gOCwgeTogcml0dWFsLnkgfV0pXG4gICAgd2FsbEJhbmQoZmxvb3IsIHRydWUsIHJpdHVhbC54IC0gMTQsIHJpdHVhbC55ICsgOCwgNClcbiAgfVxuICBpZiAodmFyaWFudCA9PT0gJ2NvdXJ0eWFyZC1sYXR0aWNlJykge1xuICAgIHdhbGxCYW5kKGZsb29yLCB0cnVlLCByaXR1YWwueCAtIDExLCByaXR1YWwueSAtIDgsIDQpXG4gICAgd2FsbEJhbmQoZmxvb3IsIHRydWUsIHJpdHVhbC54ICsgMTEsIHJpdHVhbC55ICsgOCwgNClcbiAgICB3YWxsQmFuZChmbG9vciwgZmFsc2UsIHJpdHVhbC55LCByaXR1YWwueCwgNSlcbiAgfVxuICBjb25zdCByb29tcyA9IG1hY3JvLm5vZGVzLm1hcCh0b1Jvb20pXG4gIHJvb21zLmZvckVhY2gocm9vbSA9PiBjYXJ2ZVJlY3QoZmxvb3IsIHJvb20pKVxuICBtYWNyb0Nvbm5lY3RvclBvaW50cyhtYWNybykuZm9yRWFjaChwb2ludCA9PiBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCAnZmxvb3InKSlcbiAgY29uc3QgYnlLaW5kID0gbmV3IE1hcChtYWNyby5ub2Rlcy5tYXAobm9kZSA9PiBbbm9kZS5raW5kLCB0b1Jvb20obm9kZSldKSlcbiAgY29uc3Qgb3JkZXJlZDogUm91dGVOb2RlS2luZFtdID0gWydzdGFydCcsICdsYW5kbWFyaycsICdmb3JrJywgJ29wdGlvbmFsUmV3YXJkJywgJ29iamVjdGl2ZScsIGZsb29yLmluZGV4ICUgNCA9PT0gMyA/ICdib3NzJyA6ICdleGl0J11cbiAgcmV0dXJuIG9yZGVyZWQubWFwKGtpbmQgPT4gYnlLaW5kLmdldChraW5kKSkuZmlsdGVyKChyb29tKTogcm9vbSBpcyBSb29tID0+IEJvb2xlYW4ocm9vbSkpXG59XG5cbmNvbnN0IGNhcnZlRnVybmFjZVRlcnJhY2VzID0gKGZsb29yOiBGbG9vciwgdmFyaWFudDogc3RyaW5nKTogdm9pZCA9PiB7XG4gIGNvbnN0IGxldmVscyA9IFs3LCAxNCwgMjEsIDI4LCAzNF1cbiAgY29uc3QgZ2F0ZXMgPSB2YXJpYW50ID09PSAnc3RlcHBlZC1raWxuLWNoYWluJyA/IFsxMCwgNDMsIDEwLCA0MywgMTBdXG4gICAgOiB2YXJpYW50ID09PSAnc21va2UtY2hva2VkLXNlcnZpY2Utcm91dGUnID8gWzE3LCAzNSwgMTcsIDM1LCAxN11cbiAgICAgIDogWzEyLCA0MiwgMTIsIDQyLCAyN11cbiAgbGV2ZWxzLmZvckVhY2goKHksIGluZGV4KSA9PiB3YWxsQmFuZChmbG9vciwgZmFsc2UsIHksIGdhdGVzW2luZGV4XSwgMikpXG4gIGlmICh2YXJpYW50ID09PSAnc21va2UtY2hva2VkLXNlcnZpY2Utcm91dGUnKSB3YWxsQmFuZChmbG9vciwgdHJ1ZSwgMjgsIDUsIDMpXG4gIGlmICh2YXJpYW50ID09PSAnbGlmdC1hbmQtYXNoLWxvb3AnKSB3YWxsQmFuZChmbG9vciwgdHJ1ZSwgMjgsIDMzLCA0KVxufVxuXG5jb25zdCBjYXJ2ZUZ1cm5hY2VSb3V0ZUNvbnRyYWN0TGF5b3V0ID0gKGZsb29yOiBGbG9vciwgY29udHJhY3Q6IFJldHVyblR5cGU8dHlwZW9mIGdlbmVyYXRlUm91dGVDb250cmFjdD4sIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBfcm5nOiBSbmcpOiBSb29tW10gPT4ge1xuICBpZiAoY29udHJhY3QuYmlvbWUgIT09IGZsb29yLmJpb21lKSB0aHJvdyBuZXcgRXJyb3IoYHJvdXRlIGNvbnRyYWN0ICR7Y29udHJhY3QuaWR9IGJpb21lIGRvZXMgbm90IG1hdGNoIGZsb29yIGJpb21lYClcbiAgaWYgKGNvbnRyYWN0LnJlY2lwZUlkICE9PSBmbG9vci5sYXlvdXRJZCkgdGhyb3cgbmV3IEVycm9yKGByb3V0ZSBjb250cmFjdCAke2NvbnRyYWN0LmlkfSByZWNpcGUgZG9lcyBub3QgbWF0Y2ggZmxvb3IgbGF5b3V0YClcbiAgY2FydmVSZWN0KGZsb29yLCB7IHg6IDEsIHk6IDEsIHc6IGZsb29yLndpZHRoIC0gMiwgaDogZmxvb3IuaGVpZ2h0IC0gMiB9KVxuICBjYXJ2ZUZ1cm5hY2VUZXJyYWNlcyhmbG9vciwgZmxvb3IubGF5b3V0SWQucmVwbGFjZSgnLXJlbWl4JywgJycpKVxuICBjb25zdCB0b1Jvb20gPSAobm9kZTogTWFjcm9SZWNpcGVEZWJ1Z1snbm9kZXMnXVtudW1iZXJdKTogUm9vbSA9PiAoeyB4OiBub2RlLmZvb3RwcmludC54LCB5OiBub2RlLmZvb3RwcmludC55LCB3OiBub2RlLmZvb3RwcmludC53aWR0aCwgaDogbm9kZS5mb290cHJpbnQuaGVpZ2h0IH0pXG4gIGNvbnN0IHJvb21zID0gbWFjcm8ubm9kZXMubWFwKHRvUm9vbSlcbiAgcm9vbXMuZm9yRWFjaChyb29tID0+IGNhcnZlUmVjdChmbG9vciwgcm9vbSkpXG4gIG1hY3JvQ29ubmVjdG9yUG9pbnRzKG1hY3JvKS5mb3JFYWNoKHBvaW50ID0+IHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksICdmbG9vcicpKVxuICBjb25zdCBieUtpbmQgPSBuZXcgTWFwKG1hY3JvLm5vZGVzLm1hcChub2RlID0+IFtub2RlLmtpbmQsIHRvUm9vbShub2RlKV0pKVxuICBjb25zdCBvcmRlcmVkOiBSb3V0ZU5vZGVLaW5kW10gPSBbJ3N0YXJ0JywgJ2xhbmRtYXJrJywgJ2ZvcmsnLCAnb3B0aW9uYWxSZXdhcmQnLCAnb2JqZWN0aXZlJywgZmxvb3IuaW5kZXggJSA0ID09PSAzID8gJ2Jvc3MnIDogJ2V4aXQnXVxuICByZXR1cm4gb3JkZXJlZC5tYXAoa2luZCA9PiBieUtpbmQuZ2V0KGtpbmQpKS5maWx0ZXIoKHJvb20pOiByb29tIGlzIFJvb20gPT4gQm9vbGVhbihyb29tKSlcbn1cblxuY29uc3QgY2FydmVGbG9vZGVkQ2hhbm5lbHMgPSAoZmxvb3I6IEZsb29yLCB2YXJpYW50OiBzdHJpbmcpOiB2b2lkID0+IHtcbiAgaWYgKHZhcmlhbnQgPT09ICdicmFpZGVkLWN1cnJlbnQtZGVsdGEnKSB7XG4gICAgd2FsbEJhbmQoZmxvb3IsIHRydWUsIDIwLCAxMCwgMylcbiAgICB3YWxsQmFuZChmbG9vciwgdHJ1ZSwgNDUsIDM1LCAzKVxuICB9IGVsc2UgaWYgKHZhcmlhbnQgPT09ICdhbmNob3ItZ2F0ZWQtcnVpbicpIHtcbiAgICB3YWxsQmFuZChmbG9vciwgZmFsc2UsIDE1LCAxOCwgMylcbiAgICB3YWxsQmFuZChmbG9vciwgZmFsc2UsIDMxLCA1MywgMylcbiAgfSBlbHNlIHtcbiAgICB3YWxsQmFuZChmbG9vciwgdHJ1ZSwgMjQsIDExLCA0KVxuICAgIHdhbGxCYW5kKGZsb29yLCBmYWxzZSwgMjQsIDQ5LCA0KVxuICB9XG59XG5cbmNvbnN0IGNhcnZlRmxvb2RlZFJvdXRlQ29udHJhY3RMYXlvdXQgPSAoZmxvb3I6IEZsb29yLCBjb250cmFjdDogUmV0dXJuVHlwZTx0eXBlb2YgZ2VuZXJhdGVSb3V0ZUNvbnRyYWN0PiwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcsIF9ybmc6IFJuZyk6IFJvb21bXSA9PiB7XG4gIGlmIChjb250cmFjdC5iaW9tZSAhPT0gZmxvb3IuYmlvbWUpIHRocm93IG5ldyBFcnJvcihgcm91dGUgY29udHJhY3QgJHtjb250cmFjdC5pZH0gYmlvbWUgZG9lcyBub3QgbWF0Y2ggZmxvb3IgYmlvbWVgKVxuICBpZiAoY29udHJhY3QucmVjaXBlSWQgIT09IGZsb29yLmxheW91dElkKSB0aHJvdyBuZXcgRXJyb3IoYHJvdXRlIGNvbnRyYWN0ICR7Y29udHJhY3QuaWR9IHJlY2lwZSBkb2VzIG5vdCBtYXRjaCBmbG9vciBsYXlvdXRgKVxuICBjYXJ2ZVJlY3QoZmxvb3IsIHsgeDogMSwgeTogMSwgdzogZmxvb3Iud2lkdGggLSAyLCBoOiBmbG9vci5oZWlnaHQgLSAyIH0pXG4gIGNhcnZlRmxvb2RlZENoYW5uZWxzKGZsb29yLCBmbG9vci5sYXlvdXRJZC5yZXBsYWNlKCctcmVtaXgnLCAnJykpXG4gIGNvbnN0IHRvUm9vbSA9IChub2RlOiBNYWNyb1JlY2lwZURlYnVnWydub2RlcyddW251bWJlcl0pOiBSb29tID0+ICh7IHg6IG5vZGUuZm9vdHByaW50LngsIHk6IG5vZGUuZm9vdHByaW50LnksIHc6IG5vZGUuZm9vdHByaW50LndpZHRoLCBoOiBub2RlLmZvb3RwcmludC5oZWlnaHQgfSlcbiAgY29uc3Qgcm9vbXMgPSBtYWNyby5ub2Rlcy5tYXAodG9Sb29tKVxuICByb29tcy5mb3JFYWNoKHJvb20gPT4gY2FydmVSZWN0KGZsb29yLCByb29tKSlcbiAgbWFjcm9Db25uZWN0b3JQb2ludHMobWFjcm8pLmZvckVhY2gocG9pbnQgPT4gc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2Zsb29yJykpXG4gIGNvbnN0IGJ5S2luZCA9IG5ldyBNYXAobWFjcm8ubm9kZXMubWFwKG5vZGUgPT4gW25vZGUua2luZCwgdG9Sb29tKG5vZGUpXSkpXG4gIGNvbnN0IG9yZGVyZWQ6IFJvdXRlTm9kZUtpbmRbXSA9IFsnc3RhcnQnLCAnbGFuZG1hcmsnLCAnZm9yaycsICdvcHRpb25hbFJld2FyZCcsICdvYmplY3RpdmUnLCBmbG9vci5pbmRleCAlIDQgPT09IDMgPyAnYm9zcycgOiAnZXhpdCddXG4gIHJldHVybiBvcmRlcmVkLm1hcChraW5kID0+IGJ5S2luZC5nZXQoa2luZCkpLmZpbHRlcigocm9vbSk6IHJvb20gaXMgUm9vbSA9PiBCb29sZWFuKHJvb20pKVxufVxuXG5jb25zdCBjbGlmZkJhbmQgPSAoZmxvb3I6IEZsb29yLCB2ZXJ0aWNhbDogYm9vbGVhbiwgYXQ6IG51bWJlciwgZ2FwOiBudW1iZXIsIHNwYW4gPSAyKTogdm9pZCA9PiB7XG4gIGNvbnN0IGxpbWl0ID0gdmVydGljYWwgPyBmbG9vci5oZWlnaHQgLSAxIDogZmxvb3Iud2lkdGggLSAxXG4gIGZvciAobGV0IG9mZnNldCA9IDE7IG9mZnNldCA8IGxpbWl0OyBvZmZzZXQrKykgaWYgKE1hdGguYWJzKG9mZnNldCAtIGdhcCkgPiBzcGFuKSBmb3IgKGxldCBkZXB0aCA9IC0xOyBkZXB0aCA8PSAxOyBkZXB0aCsrKSBzZXRLaW5kKGZsb29yLCB2ZXJ0aWNhbCA/IGF0ICsgZGVwdGggKyAob2Zmc2V0ICUgNSA9PT0gMCA/IDEgOiAwKSA6IG9mZnNldCwgdmVydGljYWwgPyBvZmZzZXQgOiBhdCArIGRlcHRoICsgKG9mZnNldCAlIDUgPT09IDAgPyAxIDogMCksICdjbGlmZldhbGwnKVxufVxuXG5jb25zdCBjbGlmZlJpZGdlID0gKGZsb29yOiBGbG9vciwgcG9pbnRzOiByZWFkb25seSBQb2ludFtdLCB3aWR0aCA9IDEpOiB2b2lkID0+IHtcbiAgZm9yIChsZXQgaW5kZXggPSAxOyBpbmRleCA8IHBvaW50cy5sZW5ndGg7IGluZGV4KyspIHtcbiAgICBjb25zdCBmcm9tID0gcG9pbnRzW2luZGV4IC0gMV0hXG4gICAgY29uc3QgdG8gPSBwb2ludHNbaW5kZXhdIVxuICAgIGNvbnN0IHN0ZXBzID0gTWF0aC5tYXgoTWF0aC5hYnModG8ueCAtIGZyb20ueCksIE1hdGguYWJzKHRvLnkgLSBmcm9tLnkpKVxuICAgIGZvciAobGV0IHN0ZXAgPSAwOyBzdGVwIDw9IHN0ZXBzOyBzdGVwKyspIHtcbiAgICAgIGNvbnN0IHggPSBNYXRoLnJvdW5kKGZyb20ueCArICh0by54IC0gZnJvbS54KSAqIHN0ZXAgLyBzdGVwcylcbiAgICAgIGNvbnN0IHkgPSBNYXRoLnJvdW5kKGZyb20ueSArICh0by55IC0gZnJvbS55KSAqIHN0ZXAgLyBzdGVwcylcbiAgICAgIGZvciAobGV0IGxhdGVyYWwgPSAtd2lkdGg7IGxhdGVyYWwgPD0gd2lkdGg7IGxhdGVyYWwrKykgc2V0S2luZChmbG9vciwgeCArIGxhdGVyYWwsIHksICdjbGlmZldhbGwnKVxuICAgIH1cbiAgfVxufVxuXG5jb25zdCBjYXJ2ZUNsaWZmQ29udG91cnMgPSAoZmxvb3I6IEZsb29yLCB2YXJpYW50OiBzdHJpbmcpOiB2b2lkID0+IHtcbiAgY29uc3QgYXJlYUZsb29yID0gZmxvb3IuaW5kZXggJSA0XG4gIGlmICh2YXJpYW50ID09PSAnc3dpdGNoYmFjay1mYWNlJykge1xuICAgIGNvbnN0IHRlcnJhY2VzID0gW1s5LCAxMl0sIFsxOCwgNDNdLCBbMjgsIDEyXSwgWzM4LCA0M10sIFs0NywgMjZdXS5zbGljZSgwLCAzICsgYXJlYUZsb29yKVxuICAgIHRlcnJhY2VzLmZvckVhY2goKFt5LCBnYXBdKSA9PiBjbGlmZkJhbmQoZmxvb3IsIGZhbHNlLCB5LCBnYXApKVxuICB9IGVsc2UgaWYgKHZhcmlhbnQgPT09ICdyYXZpbmUtYnJpZGdlLWxvb3AnKSB7XG4gICAgY2xpZmZSaWRnZShmbG9vciwgW3sgeDogMTMsIHk6IDIgfSwgeyB4OiAxOCwgeTogMTMgfSwgeyB4OiAxNSwgeTogMjUgfSwgeyB4OiAyMSwgeTogMzggfSwgeyB4OiAxNywgeTogZmxvb3IuaGVpZ2h0IC0gMyB9XSwgMilcbiAgICBjbGlmZlJpZGdlKGZsb29yLCBbeyB4OiA0MiwgeTogMiB9LCB7IHg6IDM2LCB5OiAxNCB9LCB7IHg6IDQwLCB5OiAyNyB9LCB7IHg6IDM0LCB5OiAzOSB9LCB7IHg6IDM5LCB5OiBmbG9vci5oZWlnaHQgLSAzIH1dLCAyKVxuICAgIGZvciAoY29uc3QgW3ksIGdhcF0gb2YgW1sxNSwgMjhdLCBbMjksIDI1XSwgWzQxLCAzMV1dLnNsaWNlKDAsIGFyZWFGbG9vcikpIGNsaWZmQmFuZChmbG9vciwgZmFsc2UsIHksIGdhcCwgMylcbiAgfSBlbHNlIHtcbiAgICBjb25zdCBzcGluZXMgPSBbXG4gICAgICBbeyB4OiA3LCB5OiA3IH0sIHsgeDogMjAsIHk6IDExIH0sIHsgeDogMTYsIHk6IDIyIH0sIHsgeDogMjksIHk6IDI3IH1dLFxuICAgICAgW3sgeDogNDcsIHk6IDkgfSwgeyB4OiAzNCwgeTogMTYgfSwgeyB4OiA0MCwgeTogMjkgfSwgeyB4OiAyOCwgeTogMzggfV0sXG4gICAgICBbeyB4OiA4LCB5OiAzOCB9LCB7IHg6IDE5LCB5OiAzNCB9LCB7IHg6IDI2LCB5OiA0NCB9LCB7IHg6IDM5LCB5OiA0NyB9XSxcbiAgICAgIFt7IHg6IDI3LCB5OiAzIH0sIHsgeDogMzEsIHk6IDE1IH0sIHsgeDogMjUsIHk6IDI3IH0sIHsgeDogMzEsIHk6IDQyIH1dXG4gICAgXVxuICAgIHNwaW5lcy5zbGljZSgwLCAyICsgYXJlYUZsb29yKS5mb3JFYWNoKHBvaW50cyA9PiBjbGlmZlJpZGdlKGZsb29yLCBwb2ludHMsIDIpKVxuICB9XG59XG5cbmNvbnN0IGNhcnZlQ2xpZmZzUm91dGVDb250cmFjdExheW91dCA9IChmbG9vcjogRmxvb3IsIGNvbnRyYWN0OiBSZXR1cm5UeXBlPHR5cGVvZiBnZW5lcmF0ZVJvdXRlQ29udHJhY3Q+LCBtYWNybzogTWFjcm9SZWNpcGVEZWJ1ZywgX3JuZzogUm5nKTogUm9vbVtdID0+IHtcbiAgaWYgKGNvbnRyYWN0LmJpb21lICE9PSBmbG9vci5iaW9tZSkgdGhyb3cgbmV3IEVycm9yKGByb3V0ZSBjb250cmFjdCAke2NvbnRyYWN0LmlkfSBiaW9tZSBkb2VzIG5vdCBtYXRjaCBmbG9vciBiaW9tZWApXG4gIGlmIChjb250cmFjdC5yZWNpcGVJZCAhPT0gZmxvb3IubGF5b3V0SWQpIHRocm93IG5ldyBFcnJvcihgcm91dGUgY29udHJhY3QgJHtjb250cmFjdC5pZH0gcmVjaXBlIGRvZXMgbm90IG1hdGNoIGZsb29yIGxheW91dGApXG4gIGNhcnZlUmVjdChmbG9vciwgeyB4OiAxLCB5OiAxLCB3OiBmbG9vci53aWR0aCAtIDIsIGg6IGZsb29yLmhlaWdodCAtIDIgfSlcbiAgY2FydmVDbGlmZkNvbnRvdXJzKGZsb29yLCBmbG9vci5sYXlvdXRJZC5yZXBsYWNlKCctcmVtaXgnLCAnJykpXG4gIGNvbnN0IHRvUm9vbSA9IChub2RlOiBNYWNyb1JlY2lwZURlYnVnWydub2RlcyddW251bWJlcl0pOiBSb29tID0+ICh7IHg6IG5vZGUuZm9vdHByaW50LngsIHk6IG5vZGUuZm9vdHByaW50LnksIHc6IG5vZGUuZm9vdHByaW50LndpZHRoLCBoOiBub2RlLmZvb3RwcmludC5oZWlnaHQgfSlcbiAgY29uc3Qgcm9vbXMgPSBtYWNyby5ub2Rlcy5tYXAodG9Sb29tKVxuICByb29tcy5mb3JFYWNoKHJvb20gPT4gY2FydmVSZWN0KGZsb29yLCByb29tKSlcbiAgbWFjcm9Db25uZWN0b3JQb2ludHMobWFjcm8pLmZvckVhY2gocG9pbnQgPT4gc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2Zsb29yJykpXG4gIGNvbnN0IGJ5S2luZCA9IG5ldyBNYXAobWFjcm8ubm9kZXMubWFwKG5vZGUgPT4gW25vZGUua2luZCwgdG9Sb29tKG5vZGUpXSkpXG4gIGNvbnN0IG9yZGVyZWQ6IFJvdXRlTm9kZUtpbmRbXSA9IFsnc3RhcnQnLCAnbGFuZG1hcmsnLCAnZm9yaycsICdvcHRpb25hbFJld2FyZCcsICdvYmplY3RpdmUnLCBmbG9vci5pbmRleCAlIDQgPT09IDMgPyAnYm9zcycgOiAnZXhpdCddXG4gIHJldHVybiBvcmRlcmVkLm1hcChraW5kID0+IGJ5S2luZC5nZXQoa2luZCkpLmZpbHRlcigocm9vbSk6IHJvb20gaXMgUm9vbSA9PiBCb29sZWFuKHJvb20pKVxufVxuXG5jb25zdCBwYWludEJ1cmlhbE1vdW5kID0gKGZsb29yOiBGbG9vciwgY2VudGVyOiBQb2ludCwgcmFkaXVzWDogbnVtYmVyLCByYWRpdXNZOiBudW1iZXIpOiB2b2lkID0+IHtcbiAgZm9yIChsZXQgeSA9IGNlbnRlci55IC0gcmFkaXVzWTsgeSA8PSBjZW50ZXIueSArIHJhZGl1c1k7IHkrKykgZm9yIChsZXQgeCA9IGNlbnRlci54IC0gcmFkaXVzWDsgeCA8PSBjZW50ZXIueCArIHJhZGl1c1g7IHgrKykge1xuICAgIGNvbnN0IGR4ID0gKHggLSBjZW50ZXIueCkgLyByYWRpdXNYXG4gICAgY29uc3QgZHkgPSAoeSAtIGNlbnRlci55KSAvIHJhZGl1c1lcbiAgICBjb25zdCBkaXN0YW5jZSA9IGR4ICogZHggKyBkeSAqIGR5ICsgKCh4ICogMTEgKyB5ICogNykgJSA1IC0gMikgKiAuMDdcbiAgICBpZiAoZGlzdGFuY2UgPj0gLjczICYmIGRpc3RhbmNlIDw9IDEuMTMpIHNldEtpbmQoZmxvb3IsIHgsIHksICdjYWlybicpXG4gICAgZWxzZSBpZiAoZGlzdGFuY2UgPCAuNDYgJiYgKHggKiAzICsgeSkgJSA0ID09PSAwKSBzZXRLaW5kKGZsb29yLCB4LCB5LCAnZ3JhdmVTb2lsJylcbiAgfVxufVxuXG5jb25zdCBwYWludEJ1cmlhbFByb2Nlc3Npb24gPSAoZmxvb3I6IEZsb29yLCBwb2ludHM6IHJlYWRvbmx5IFBvaW50W10pOiB2b2lkID0+IHtcbiAgZm9yIChsZXQgaW5kZXggPSAxOyBpbmRleCA8IHBvaW50cy5sZW5ndGg7IGluZGV4KyspIHtcbiAgICBjb25zdCBmcm9tID0gcG9pbnRzW2luZGV4IC0gMV0hXG4gICAgY29uc3QgdG8gPSBwb2ludHNbaW5kZXhdIVxuICAgIGNvbnN0IHN0ZXBzID0gTWF0aC5tYXgoTWF0aC5hYnModG8ueCAtIGZyb20ueCksIE1hdGguYWJzKHRvLnkgLSBmcm9tLnkpKVxuICAgIGZvciAobGV0IHN0ZXAgPSAwOyBzdGVwIDw9IHN0ZXBzOyBzdGVwKyspIHNldEtpbmQoZmxvb3IsIE1hdGgucm91bmQoZnJvbS54ICsgKHRvLnggLSBmcm9tLngpICogc3RlcCAvIHN0ZXBzKSwgTWF0aC5yb3VuZChmcm9tLnkgKyAodG8ueSAtIGZyb20ueSkgKiBzdGVwIC8gc3RlcHMpLCAnc3Bpcml0UGF0aCcpXG4gIH1cbn1cblxuY29uc3QgY2FydmVCdXJpYWxMYW5kc2NhcGUgPSAoZmxvb3I6IEZsb29yLCB2YXJpYW50OiBzdHJpbmcpOiB2b2lkID0+IHtcbiAgY29uc3QgY2VudGVyID0geyB4OiBNYXRoLmZsb29yKGZsb29yLndpZHRoIC8gMiksIHk6IE1hdGguZmxvb3IoZmxvb3IuaGVpZ2h0IC8gMikgfVxuICBpZiAodmFyaWFudCA9PT0gJ3N0b25lLWNpcmNsZS1jZW50ZXInKSB7XG4gICAgO1t7IHg6IGNlbnRlci54IC0gMTgsIHk6IGNlbnRlci55IC0gOCB9LCB7IHg6IGNlbnRlci54ICsgMywgeTogY2VudGVyLnkgLSAxMCB9LCB7IHg6IGNlbnRlci54ICsgMTksIHk6IGNlbnRlci55ICsgNCB9LCB7IHg6IGNlbnRlci54IC0gNiwgeTogY2VudGVyLnkgKyAxMCB9XS5mb3JFYWNoKChwb2ludCwgaW5kZXgpID0+IHBhaW50QnVyaWFsTW91bmQoZmxvb3IsIHBvaW50LCA3ICsgaW5kZXggJSAyLCA0ICsgaW5kZXggJSAzKSlcbiAgICBwYWludEJ1cmlhbFByb2Nlc3Npb24oZmxvb3IsIFt7IHg6IDQsIHk6IGNlbnRlci55ICsgNiB9LCB7IHg6IGNlbnRlci54IC0gMTgsIHk6IGNlbnRlci55ICsgMyB9LCB7IHg6IGNlbnRlci54IC0gMywgeTogY2VudGVyLnkgLSAzIH0sIHsgeDogY2VudGVyLnggKyAxOCwgeTogY2VudGVyLnkgKyA1IH0sIHsgeDogZmxvb3Iud2lkdGggLSA1LCB5OiBjZW50ZXIueSAtIDIgfV0pXG4gIH0gZWxzZSBpZiAodmFyaWFudCA9PT0gJ21vdW5kLXByb2Nlc3Npb24nKSB7XG4gICAgO1t7IHg6IDE4LCB5OiA5IH0sIHsgeDogNDcsIHk6IDE0IH0sIHsgeDogMjYsIHk6IDI1IH0sIHsgeDogNTksIHk6IDMyIH0sIHsgeDogMjAsIHk6IDQzIH0sIHsgeDogNDksIHk6IDQ4IH1dLmZvckVhY2goKHBvaW50LCBpbmRleCkgPT4gcGFpbnRCdXJpYWxNb3VuZChmbG9vciwgcG9pbnQsIDUgKyBpbmRleCAlIDMsIDMgKyBpbmRleCAlIDIpKVxuICAgIHBhaW50QnVyaWFsUHJvY2Vzc2lvbihmbG9vciwgW3sgeDogY2VudGVyLnggLSA0LCB5OiAzIH0sIHsgeDogY2VudGVyLnggKyA3LCB5OiAxNCB9LCB7IHg6IGNlbnRlci54IC0gOSwgeTogMjUgfSwgeyB4OiBjZW50ZXIueCArIDgsIHk6IDM4IH0sIHsgeDogY2VudGVyLnggLSA1LCB5OiBmbG9vci5oZWlnaHQgLSA0IH1dKVxuICB9IGVsc2UgaWYgKHZhcmlhbnQgPT09ICdjZW1ldGVyeS1zZXR0bGVtZW50LWVkZ2UnKSB7XG4gICAgZm9yIChsZXQgY2x1c3RlciA9IDA7IGNsdXN0ZXIgPCA3OyBjbHVzdGVyKyspIHtcbiAgICAgIGNvbnN0IG9yaWdpbiA9IHsgeDogOSArIChjbHVzdGVyICogMTcpICUgKGZsb29yLndpZHRoIC0gMTgpLCB5OiA4ICsgKGNsdXN0ZXIgKiAxMSkgJSAoZmxvb3IuaGVpZ2h0IC0gMTYpIH1cbiAgICAgIHBhaW50QnVyaWFsTW91bmQoZmxvb3IsIG9yaWdpbiwgNCArIGNsdXN0ZXIgJSAzLCAzICsgKGNsdXN0ZXIgKyAxKSAlIDIpXG4gICAgfVxuICAgIHBhaW50QnVyaWFsUHJvY2Vzc2lvbihmbG9vciwgW3sgeDogMywgeTogY2VudGVyLnkgLSA3IH0sIHsgeDogMTksIHk6IGNlbnRlci55IC0gMiB9LCB7IHg6IDM3LCB5OiBjZW50ZXIueSArIDQgfSwgeyB4OiA1OCwgeTogY2VudGVyLnkgLSAzIH0sIHsgeDogZmxvb3Iud2lkdGggLSA0LCB5OiBjZW50ZXIueSArIDUgfV0pXG4gIH0gZWxzZSBpZiAodmFyaWFudCA9PT0gJ29zc3VhcnktaG9sbG93Jykge1xuICAgIDtbeyB4OiBjZW50ZXIueCAtIDE4LCB5OiBjZW50ZXIueSAtIDggfSwgeyB4OiBjZW50ZXIueCArIDE3LCB5OiBjZW50ZXIueSAtIDYgfSwgeyB4OiBjZW50ZXIueCAtIDExLCB5OiBjZW50ZXIueSArIDExIH0sIHsgeDogY2VudGVyLnggKyAxMywgeTogY2VudGVyLnkgKyAxMCB9XS5mb3JFYWNoKChwb2ludCwgaW5kZXgpID0+IHBhaW50QnVyaWFsTW91bmQoZmxvb3IsIHBvaW50LCA3LCA0ICsgaW5kZXggJSAyKSlcbiAgICBmb3IgKGxldCB5ID0gY2VudGVyLnkgLSA3OyB5IDw9IGNlbnRlci55ICsgNzsgeSsrKSBmb3IgKGxldCB4ID0gY2VudGVyLnggLSAxMzsgeCA8PSBjZW50ZXIueCArIDEzOyB4KyspIGlmICgoTWF0aC5hYnMoeCAtIGNlbnRlci54KSArIE1hdGguYWJzKHkgLSBjZW50ZXIueSkgPiAxMCkgJiYgKHggKiA1ICsgeSkgJSA0ID09PSAwKSBzZXRLaW5kKGZsb29yLCB4LCB5LCAnb3NzdWFyeScpXG4gICAgcGFpbnRCdXJpYWxQcm9jZXNzaW9uKGZsb29yLCBbeyB4OiA0LCB5OiBjZW50ZXIueSArIDUgfSwgeyB4OiBjZW50ZXIueCAtIDE1LCB5OiBjZW50ZXIueSAtIDIgfSwgeyB4OiBjZW50ZXIueCwgeTogY2VudGVyLnkgKyAzIH0sIHsgeDogY2VudGVyLnggKyAxNywgeTogY2VudGVyLnkgLSA0IH0sIHsgeDogZmxvb3Iud2lkdGggLSA1LCB5OiBjZW50ZXIueSArIDIgfV0pXG4gIH0gZWxzZSB7XG4gICAgO1t7IHg6IGNlbnRlci54IC0gMTksIHk6IGNlbnRlci55IC0gMTAgfSwgeyB4OiBjZW50ZXIueCArIDE3LCB5OiBjZW50ZXIueSAtIDExIH0sIHsgeDogY2VudGVyLnggKyAyMCwgeTogY2VudGVyLnkgKyA5IH0sIHsgeDogY2VudGVyLnggLSAxNSwgeTogY2VudGVyLnkgKyAxMSB9LCB7IHg6IGNlbnRlci54LCB5OiBjZW50ZXIueSB9XS5mb3JFYWNoKChwb2ludCwgaW5kZXgpID0+IHBhaW50QnVyaWFsTW91bmQoZmxvb3IsIHBvaW50LCA2ICsgaW5kZXggJSAyLCA0ICsgKGluZGV4ICsgMSkgJSAyKSlcbiAgICBwYWludEJ1cmlhbFByb2Nlc3Npb24oZmxvb3IsIFt7IHg6IGNlbnRlci54IC0gMjIsIHk6IGNlbnRlci55IC0gMTAgfSwgeyB4OiBjZW50ZXIueCAtIDQsIHk6IGNlbnRlci55IC0gMTQgfSwgeyB4OiBjZW50ZXIueCArIDIwLCB5OiBjZW50ZXIueSAtIDcgfSwgeyB4OiBjZW50ZXIueCArIDE2LCB5OiBjZW50ZXIueSArIDExIH0sIHsgeDogY2VudGVyLnggLSAxNSwgeTogY2VudGVyLnkgKyAxMyB9LCB7IHg6IGNlbnRlci54IC0gMjIsIHk6IGNlbnRlci55IC0gMiB9XSlcbiAgfVxufVxuXG5jb25zdCBjYXJ2ZUJ1cmlhbFJvdXRlQ29udHJhY3RMYXlvdXQgPSAoZmxvb3I6IEZsb29yLCBjb250cmFjdDogUmV0dXJuVHlwZTx0eXBlb2YgZ2VuZXJhdGVSb3V0ZUNvbnRyYWN0PiwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcsIF9ybmc6IFJuZyk6IFJvb21bXSA9PiB7XG4gIGlmIChjb250cmFjdC5iaW9tZSAhPT0gZmxvb3IuYmlvbWUpIHRocm93IG5ldyBFcnJvcihgcm91dGUgY29udHJhY3QgJHtjb250cmFjdC5pZH0gYmlvbWUgZG9lcyBub3QgbWF0Y2ggZmxvb3IgYmlvbWVgKVxuICBpZiAoY29udHJhY3QucmVjaXBlSWQgIT09IGZsb29yLmxheW91dElkKSB0aHJvdyBuZXcgRXJyb3IoYHJvdXRlIGNvbnRyYWN0ICR7Y29udHJhY3QuaWR9IHJlY2lwZSBkb2VzIG5vdCBtYXRjaCBmbG9vciBsYXlvdXRgKVxuICBjYXJ2ZVJlY3QoZmxvb3IsIHsgeDogMSwgeTogMSwgdzogZmxvb3Iud2lkdGggLSAyLCBoOiBmbG9vci5oZWlnaHQgLSAyIH0pXG4gIGNhcnZlQnVyaWFsTGFuZHNjYXBlKGZsb29yLCBmbG9vci5sYXlvdXRJZC5yZXBsYWNlKCctcmVtaXgnLCAnJykpXG4gIGNvbnN0IHRvUm9vbSA9IChub2RlOiBNYWNyb1JlY2lwZURlYnVnWydub2RlcyddW251bWJlcl0pOiBSb29tID0+ICh7IHg6IG5vZGUuZm9vdHByaW50LngsIHk6IG5vZGUuZm9vdHByaW50LnksIHc6IG5vZGUuZm9vdHByaW50LndpZHRoLCBoOiBub2RlLmZvb3RwcmludC5oZWlnaHQgfSlcbiAgY29uc3Qgcm9vbXMgPSBtYWNyby5ub2Rlcy5tYXAodG9Sb29tKVxuICByb29tcy5mb3JFYWNoKHJvb20gPT4gY2FydmVSZWN0KGZsb29yLCByb29tKSlcbiAgbWFjcm9Db25uZWN0b3JQb2ludHMobWFjcm8pLmZvckVhY2gocG9pbnQgPT4gc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2Zsb29yJykpXG4gIGNvbnN0IGJ5S2luZCA9IG5ldyBNYXAobWFjcm8ubm9kZXMubWFwKG5vZGUgPT4gW25vZGUua2luZCwgdG9Sb29tKG5vZGUpXSkpXG4gIGNvbnN0IG9yZGVyZWQ6IFJvdXRlTm9kZUtpbmRbXSA9IFsnc3RhcnQnLCAnbGFuZG1hcmsnLCAnZm9yaycsICdvcHRpb25hbFJld2FyZCcsICdvYmplY3RpdmUnLCBmbG9vci5pbmRleCAlIDQgPT09IDMgPyAnYm9zcycgOiAnZXhpdCddXG4gIHJldHVybiBvcmRlcmVkLm1hcChraW5kID0+IGJ5S2luZC5nZXQoa2luZCkpLmZpbHRlcigocm9vbSk6IHJvb20gaXMgUm9vbSA9PiBCb29sZWFuKHJvb20pKVxufVxuXG5jb25zdCBwYWludFNhbHRMaW5lID0gKGZsb29yOiBGbG9vciwgZnJvbTogUG9pbnQsIHRvOiBQb2ludCwga2luZDogVGlsZVsna2luZCddKTogdm9pZCA9PiB7XG4gIGlmIChmcm9tLnggPT09IHRvLngpIGZvciAobGV0IHkgPSBNYXRoLm1pbihmcm9tLnksIHRvLnkpOyB5IDw9IE1hdGgubWF4KGZyb20ueSwgdG8ueSk7IHkrKykgc2V0S2luZChmbG9vciwgZnJvbS54LCB5LCBraW5kKVxuICBlbHNlIGZvciAobGV0IHggPSBNYXRoLm1pbihmcm9tLngsIHRvLngpOyB4IDw9IE1hdGgubWF4KGZyb20ueCwgdG8ueCk7IHgrKykgc2V0S2luZChmbG9vciwgeCwgZnJvbS55LCBraW5kKVxufVxuXG5jb25zdCBwYWludFNhbHRQYXRjaCA9IChmbG9vcjogRmxvb3IsIGxlZnQ6IG51bWJlciwgdG9wOiBudW1iZXIsIHdpZHRoOiBudW1iZXIsIGhlaWdodDogbnVtYmVyLCBraW5kOiBUaWxlWydraW5kJ10pOiB2b2lkID0+IHtcbiAgZm9yIChsZXQgeSA9IHRvcDsgeSA8IHRvcCArIGhlaWdodDsgeSsrKSBmb3IgKGxldCB4ID0gbGVmdDsgeCA8IGxlZnQgKyB3aWR0aDsgeCsrKSBzZXRLaW5kKGZsb29yLCB4LCB5LCBraW5kKVxufVxuXG5jb25zdCBjYXJ2ZVNhbHRMYW5kc2NhcGUgPSAoZmxvb3I6IEZsb29yLCB2YXJpYW50OiBzdHJpbmcpOiB2b2lkID0+IHtcbiAgY29uc3QgY2VudGVyID0geyB4OiBNYXRoLmZsb29yKGZsb29yLndpZHRoIC8gMiksIHk6IE1hdGguZmxvb3IoZmxvb3IuaGVpZ2h0IC8gMikgfVxuICBpZiAodmFyaWFudCA9PT0gJ2NydXN0LWlzbGFuZC1jaGFpbicpIHtcbiAgICBmb3IgKGxldCB4ID0gMTQsIGlzbGFuZCA9IDA7IHggPCBmbG9vci53aWR0aCAtIDEwOyB4ICs9IDE1LCBpc2xhbmQrKykge1xuICAgICAgcGFpbnRTYWx0TGluZShmbG9vciwgeyB4LCB5OiAyIH0sIHsgeCwgeTogZmxvb3IuaGVpZ2h0IC0gMyB9LCAnYnJpbmUnKVxuICAgICAgY29uc3QgZ2FwID0gaXNsYW5kICUgMiA/IDEyIDogZmxvb3IuaGVpZ2h0IC0gMTNcbiAgICAgIHBhaW50U2FsdFBhdGNoKGZsb29yLCB4IC0gMSwgZ2FwIC0gMiwgMywgNSwgJ2Zsb29yJylcbiAgICB9XG4gIH0gZWxzZSBpZiAodmFyaWFudCA9PT0gJ2JyaW5lLW1hemUnKSB7XG4gICAgZm9yIChsZXQgeCA9IDEyOyB4IDwgZmxvb3Iud2lkdGggLSA4OyB4ICs9IDE0KSBwYWludFNhbHRMaW5lKGZsb29yLCB7IHgsIHk6IDQgfSwgeyB4LCB5OiBmbG9vci5oZWlnaHQgLSA1IH0sICdicmluZScpXG4gICAgZm9yIChsZXQgeSA9IDEwOyB5IDwgZmxvb3IuaGVpZ2h0IC0gNzsgeSArPSAxMikgcGFpbnRTYWx0TGluZShmbG9vciwgeyB4OiA1LCB5IH0sIHsgeDogZmxvb3Iud2lkdGggLSA2LCB5IH0sICdicmluZScpXG4gICAgcGFpbnRTYWx0UGF0Y2goZmxvb3IsIGNlbnRlci54IC0gNSwgY2VudGVyLnkgLSAzLCAxMSwgNywgJ2Zsb29yJylcbiAgfSBlbHNlIGlmICh2YXJpYW50ID09PSAnY2FyYXZhbi1jYXVzZXdheScpIHtcbiAgICBwYWludFNhbHRMaW5lKGZsb29yLCB7IHg6IDMsIHk6IGNlbnRlci55IH0sIHsgeDogZmxvb3Iud2lkdGggLSA0LCB5OiBjZW50ZXIueSB9LCAnc2FsdE1pcnJvcicpXG4gICAgcGFpbnRTYWx0TGluZShmbG9vciwgeyB4OiAzLCB5OiBjZW50ZXIueSAtIDUgfSwgeyB4OiBmbG9vci53aWR0aCAtIDQsIHk6IGNlbnRlci55IC0gNSB9LCAnYnJpbmUnKVxuICAgIHBhaW50U2FsdExpbmUoZmxvb3IsIHsgeDogMywgeTogY2VudGVyLnkgKyA1IH0sIHsgeDogZmxvb3Iud2lkdGggLSA0LCB5OiBjZW50ZXIueSArIDUgfSwgJ2JyaW5lJylcbiAgfSBlbHNlIGlmICh2YXJpYW50ID09PSAnbWlycm9yLWJhc2luLWxvb3AnKSB7XG4gICAgY29uc3QgbGVmdCA9IGNlbnRlci54IC0gMTlcbiAgICBjb25zdCByaWdodCA9IGNlbnRlci54ICsgMTlcbiAgICBjb25zdCB0b3AgPSBjZW50ZXIueSAtIDEyXG4gICAgY29uc3QgYm90dG9tID0gY2VudGVyLnkgKyAxMlxuICAgIHBhaW50U2FsdExpbmUoZmxvb3IsIHsgeDogbGVmdCwgeTogdG9wIH0sIHsgeDogcmlnaHQsIHk6IHRvcCB9LCAnc2FsdE1pcnJvcicpXG4gICAgcGFpbnRTYWx0TGluZShmbG9vciwgeyB4OiByaWdodCwgeTogdG9wIH0sIHsgeDogcmlnaHQsIHk6IGJvdHRvbSB9LCAnc2FsdE1pcnJvcicpXG4gICAgcGFpbnRTYWx0TGluZShmbG9vciwgeyB4OiByaWdodCwgeTogYm90dG9tIH0sIHsgeDogbGVmdCwgeTogYm90dG9tIH0sICdzYWx0TWlycm9yJylcbiAgICBwYWludFNhbHRMaW5lKGZsb29yLCB7IHg6IGxlZnQsIHk6IGJvdHRvbSB9LCB7IHg6IGxlZnQsIHk6IHRvcCB9LCAnc2FsdE1pcnJvcicpXG4gICAgcGFpbnRTYWx0UGF0Y2goZmxvb3IsIGNlbnRlci54IC0gMTAsIGNlbnRlci55IC0gNiwgMjEsIDEzLCAnYnJpbmUnKVxuICB9IGVsc2Uge1xuICAgIGZvciAobGV0IHkgPSA4OyB5IDwgZmxvb3IuaGVpZ2h0IC0gNzsgeSArPSAxMCkge1xuICAgICAgcGFpbnRTYWx0TGluZShmbG9vciwgeyB4OiA1LCB5IH0sIHsgeDogZmxvb3Iud2lkdGggLSA2LCB5IH0sICdjcnVtYmxlJylcbiAgICAgIHBhaW50U2FsdFBhdGNoKGZsb29yLCBjZW50ZXIueCAtIDUsIHkgLSAxLCAxMSwgMywgJ2Zsb29yJylcbiAgICB9XG4gICAgcGFpbnRTYWx0UGF0Y2goZmxvb3IsIGNlbnRlci54IC0gMTIsIGNlbnRlci55IC0gNiwgMjUsIDEzLCAnc2FsdE1pcnJvcicpXG4gIH1cbn1cblxuY29uc3QgY2FydmVTYWx0RmxhdHNSb3V0ZUNvbnRyYWN0TGF5b3V0ID0gKGZsb29yOiBGbG9vciwgY29udHJhY3Q6IFJldHVyblR5cGU8dHlwZW9mIGdlbmVyYXRlUm91dGVDb250cmFjdD4sIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBfcm5nOiBSbmcpOiBSb29tW10gPT4ge1xuICBpZiAoY29udHJhY3QuYmlvbWUgIT09IGZsb29yLmJpb21lKSB0aHJvdyBuZXcgRXJyb3IoYHJvdXRlIGNvbnRyYWN0ICR7Y29udHJhY3QuaWR9IGJpb21lIGRvZXMgbm90IG1hdGNoIGZsb29yIGJpb21lYClcbiAgaWYgKGNvbnRyYWN0LnJlY2lwZUlkICE9PSBmbG9vci5sYXlvdXRJZCkgdGhyb3cgbmV3IEVycm9yKGByb3V0ZSBjb250cmFjdCAke2NvbnRyYWN0LmlkfSByZWNpcGUgZG9lcyBub3QgbWF0Y2ggZmxvb3IgbGF5b3V0YClcbiAgY2FydmVSZWN0KGZsb29yLCB7IHg6IDEsIHk6IDEsIHc6IGZsb29yLndpZHRoIC0gMiwgaDogZmxvb3IuaGVpZ2h0IC0gMiB9KVxuICBjYXJ2ZVNhbHRMYW5kc2NhcGUoZmxvb3IsIGZsb29yLmxheW91dElkLnJlcGxhY2UoJy1yZW1peCcsICcnKSlcbiAgY29uc3QgdG9Sb29tID0gKG5vZGU6IE1hY3JvUmVjaXBlRGVidWdbJ25vZGVzJ11bbnVtYmVyXSk6IFJvb20gPT4gKHsgeDogbm9kZS5mb290cHJpbnQueCwgeTogbm9kZS5mb290cHJpbnQueSwgdzogbm9kZS5mb290cHJpbnQud2lkdGgsIGg6IG5vZGUuZm9vdHByaW50LmhlaWdodCB9KVxuICBjb25zdCByb29tcyA9IG1hY3JvLm5vZGVzLm1hcCh0b1Jvb20pXG4gIHJvb21zLmZvckVhY2gocm9vbSA9PiBjYXJ2ZVJlY3QoZmxvb3IsIHJvb20pKVxuICBtYWNyb0Nvbm5lY3RvclBvaW50cyhtYWNybykuZm9yRWFjaChwb2ludCA9PiBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCAnZmxvb3InKSlcbiAgY29uc3QgYnlLaW5kID0gbmV3IE1hcChtYWNyby5ub2Rlcy5tYXAobm9kZSA9PiBbbm9kZS5raW5kLCB0b1Jvb20obm9kZSldKSlcbiAgY29uc3Qgb3JkZXJlZDogUm91dGVOb2RlS2luZFtdID0gWydzdGFydCcsICdsYW5kbWFyaycsICdmb3JrJywgJ29wdGlvbmFsUmV3YXJkJywgJ29iamVjdGl2ZScsIGZsb29yLmluZGV4ICUgNCA9PT0gMyA/ICdib3NzJyA6ICdleGl0J11cbiAgcmV0dXJuIG9yZGVyZWQubWFwKGtpbmQgPT4gYnlLaW5kLmdldChraW5kKSkuZmlsdGVyKChyb29tKTogcm9vbSBpcyBSb29tID0+IEJvb2xlYW4ocm9vbSkpXG59XG5cbmNvbnN0IHBhaW50RnJvc3RTaGVsZiA9IChmbG9vcjogRmxvb3IsIGNlbnRlcjogUG9pbnQsIHJhZGl1c1g6IG51bWJlciwgcmFkaXVzWTogbnVtYmVyLCBraW5kOiBUaWxlWydraW5kJ10pOiB2b2lkID0+IHtcbiAgZm9yIChsZXQgeSA9IGNlbnRlci55IC0gcmFkaXVzWTsgeSA8PSBjZW50ZXIueSArIHJhZGl1c1k7IHkrKykgZm9yIChsZXQgeCA9IGNlbnRlci54IC0gcmFkaXVzWDsgeCA8PSBjZW50ZXIueCArIHJhZGl1c1g7IHgrKykgaWYgKCh4IC0gY2VudGVyLngpICoqIDIgLyByYWRpdXNYICoqIDIgKyAoeSAtIGNlbnRlci55KSAqKiAyIC8gcmFkaXVzWSAqKiAyICsgKCh4ICogNSArIHkgKiA3KSAlIDUgLSAyKSAqIC4wOCA8IDEpIHNldEtpbmQoZmxvb3IsIHgsIHksIGtpbmQpXG59XG5cbmNvbnN0IHBhaW50RnJvc3RDcmFjayA9IChmbG9vcjogRmxvb3IsIHBvaW50czogcmVhZG9ubHkgUG9pbnRbXSk6IHZvaWQgPT4ge1xuICBmb3IgKGxldCBpbmRleCA9IDE7IGluZGV4IDwgcG9pbnRzLmxlbmd0aDsgaW5kZXgrKykge1xuICAgIGNvbnN0IGZyb20gPSBwb2ludHNbaW5kZXggLSAxXSFcbiAgICBjb25zdCB0byA9IHBvaW50c1tpbmRleF0hXG4gICAgY29uc3Qgc3RlcHMgPSBNYXRoLm1heChNYXRoLmFicyh0by54IC0gZnJvbS54KSwgTWF0aC5hYnModG8ueSAtIGZyb20ueSkpXG4gICAgZm9yIChsZXQgc3RlcCA9IDA7IHN0ZXAgPD0gc3RlcHM7IHN0ZXArKykgc2V0S2luZChmbG9vciwgTWF0aC5yb3VuZChmcm9tLnggKyAodG8ueCAtIGZyb20ueCkgKiBzdGVwIC8gc3RlcHMpLCBNYXRoLnJvdW5kKGZyb20ueSArICh0by55IC0gZnJvbS55KSAqIHN0ZXAgLyBzdGVwcyksICdmcm9zdFJpbWUnKVxuICB9XG59XG5cbmNvbnN0IGNhcnZlRnJvc3RMYW5kc2NhcGUgPSAoZmxvb3I6IEZsb29yLCB2YXJpYW50OiBzdHJpbmcpOiB2b2lkID0+IHtcbiAgY29uc3QgY2VudGVyID0geyB4OiBNYXRoLmZsb29yKGZsb29yLndpZHRoIC8gMiksIHk6IE1hdGguZmxvb3IoZmxvb3IuaGVpZ2h0IC8gMikgfVxuICBpZiAodmFyaWFudCA9PT0gJ2Zyb3plbi1sYWtlLWNyb3NzaW5nJykge1xuICAgIHBhaW50RnJvc3RTaGVsZihmbG9vciwgeyB4OiBjZW50ZXIueCAtIDEyLCB5OiBjZW50ZXIueSAtIDIgfSwgMTksIDgsICdpY2UnKVxuICAgIHBhaW50RnJvc3RTaGVsZihmbG9vciwgeyB4OiBjZW50ZXIueCArIDE0LCB5OiBjZW50ZXIueSArIDMgfSwgMTgsIDksICdpY2UnKVxuICAgIHBhaW50RnJvc3RDcmFjayhmbG9vciwgW3sgeDogNCwgeTogY2VudGVyLnkgLSA4IH0sIHsgeDogMjAsIHk6IGNlbnRlci55IC0gNSB9LCB7IHg6IDM3LCB5OiBjZW50ZXIueSAtIDkgfSwgeyB4OiBmbG9vci53aWR0aCAtIDUsIHk6IGNlbnRlci55IC0gNiB9XSlcbiAgICBwYWludEZyb3N0Q3JhY2soZmxvb3IsIFt7IHg6IDUsIHk6IGNlbnRlci55ICsgOCB9LCB7IHg6IDI0LCB5OiBjZW50ZXIueSArIDUgfSwgeyB4OiA0MywgeTogY2VudGVyLnkgKyA5IH0sIHsgeDogZmxvb3Iud2lkdGggLSA1LCB5OiBjZW50ZXIueSArIDYgfV0pXG4gIH0gZWxzZSBpZiAodmFyaWFudCA9PT0gJ3JpZGdlLWhvbGxvdy1sb29wJykge1xuICAgIHBhaW50RnJvc3RDcmFjayhmbG9vciwgW3sgeDogY2VudGVyLnggLSAyMCwgeTogY2VudGVyLnkgLSAxMCB9LCB7IHg6IGNlbnRlci54IC0gMywgeTogY2VudGVyLnkgLSAxNCB9LCB7IHg6IGNlbnRlci54ICsgMjAsIHk6IGNlbnRlci55IC0gOCB9LCB7IHg6IGNlbnRlci54ICsgMTYsIHk6IGNlbnRlci55ICsgMTIgfSwgeyB4OiBjZW50ZXIueCAtIDE3LCB5OiBjZW50ZXIueSArIDEzIH0sIHsgeDogY2VudGVyLnggLSAyMiwgeTogY2VudGVyLnkgLSAyIH1dKVxuICAgIHBhaW50RnJvc3RTaGVsZihmbG9vciwgY2VudGVyLCAxMywgOCwgJ2ljZScpXG4gIH0gZWxzZSBpZiAodmFyaWFudCA9PT0gJ3ByZXNzdXJlLWNyYWNrLW1hemUnKSB7XG4gICAgZm9yIChsZXQgeCA9IDEwOyB4IDwgZmxvb3Iud2lkdGggLSA3OyB4ICs9IDEzKSBwYWludEZyb3N0Q3JhY2soZmxvb3IsIFt7IHgsIHk6IDQgfSwgeyB4OiB4ICsgMywgeTogMTYgfSwgeyB4OiB4IC0gMiwgeTogMjggfSwgeyB4OiB4ICsgMiwgeTogZmxvb3IuaGVpZ2h0IC0gNSB9XSlcbiAgICA7W3sgeDogMTQsIHk6IDEwIH0sIHsgeDogMzMsIHk6IDIwIH0sIHsgeDogNDksIHk6IDM0IH1dLmZvckVhY2gocG9pbnQgPT4gcGFpbnRGcm9zdFNoZWxmKGZsb29yLCBwb2ludCwgMTAsIDUsICdpY2UnKSlcbiAgfSBlbHNlIGlmICh2YXJpYW50ID09PSAnc2hvcmUtcmVsaXF1YXJ5LXJvdXRlJykge1xuICAgIHBhaW50RnJvc3RTaGVsZihmbG9vciwgeyB4OiBjZW50ZXIueCAtIDUsIHk6IDE0IH0sIDksIDEyLCAnaWNlJylcbiAgICBwYWludEZyb3N0U2hlbGYoZmxvb3IsIHsgeDogY2VudGVyLnggKyA3LCB5OiAzNCB9LCAxMCwgMTIsICdpY2UnKVxuICAgIHBhaW50RnJvc3RDcmFjayhmbG9vciwgW3sgeDogY2VudGVyLnggLSAxMCwgeTogMyB9LCB7IHg6IGNlbnRlci54IC0gNCwgeTogMTYgfSwgeyB4OiBjZW50ZXIueCAtIDksIHk6IDI5IH0sIHsgeDogY2VudGVyLnggLSAzLCB5OiBmbG9vci5oZWlnaHQgLSA0IH1dKVxuICAgIHBhaW50RnJvc3RDcmFjayhmbG9vciwgW3sgeDogY2VudGVyLnggKyAxMCwgeTogMyB9LCB7IHg6IGNlbnRlci54ICsgNCwgeTogMTggfSwgeyB4OiBjZW50ZXIueCArIDksIHk6IDMzIH0sIHsgeDogY2VudGVyLnggKyAzLCB5OiBmbG9vci5oZWlnaHQgLSA0IH1dKVxuICB9IGVsc2Uge1xuICAgIDtbeyB4OiAxMiwgeTogY2VudGVyLnkgLSA0IH0sIHsgeDogMjcsIHk6IGNlbnRlci55ICsgNSB9LCB7IHg6IDQzLCB5OiBjZW50ZXIueSAtIDMgfSwgeyB4OiA1NSwgeTogY2VudGVyLnkgKyA1IH1dLmZvckVhY2goKHBvaW50LCBpbmRleCkgPT4gcGFpbnRGcm9zdFNoZWxmKGZsb29yLCBwb2ludCwgNiArIGluZGV4ICUgMiwgNSwgJ2ljZScpKVxuICAgIHBhaW50RnJvc3RDcmFjayhmbG9vciwgW3sgeDogOCwgeTogNCB9LCB7IHg6IDE3LCB5OiAxNiB9LCB7IHg6IDEzLCB5OiAzMCB9LCB7IHg6IDIyLCB5OiBmbG9vci5oZWlnaHQgLSA1IH1dKVxuICAgIHBhaW50RnJvc3RDcmFjayhmbG9vciwgW3sgeDogNDIsIHk6IDQgfSwgeyB4OiA1MCwgeTogMTYgfSwgeyB4OiA0NSwgeTogMzAgfSwgeyB4OiA1NCwgeTogZmxvb3IuaGVpZ2h0IC0gNSB9XSlcbiAgfVxufVxuXG5jb25zdCBjYXJ2ZUZyb3N0UmVsaXF1YXJ5Um91dGVDb250cmFjdExheW91dCA9IChmbG9vcjogRmxvb3IsIGNvbnRyYWN0OiBSZXR1cm5UeXBlPHR5cGVvZiBnZW5lcmF0ZVJvdXRlQ29udHJhY3Q+LCBtYWNybzogTWFjcm9SZWNpcGVEZWJ1ZywgX3JuZzogUm5nKTogUm9vbVtdID0+IHtcbiAgaWYgKGNvbnRyYWN0LmJpb21lICE9PSBmbG9vci5iaW9tZSkgdGhyb3cgbmV3IEVycm9yKGByb3V0ZSBjb250cmFjdCAke2NvbnRyYWN0LmlkfSBiaW9tZSBkb2VzIG5vdCBtYXRjaCBmbG9vciBiaW9tZWApXG4gIGlmIChjb250cmFjdC5yZWNpcGVJZCAhPT0gZmxvb3IubGF5b3V0SWQpIHRocm93IG5ldyBFcnJvcihgcm91dGUgY29udHJhY3QgJHtjb250cmFjdC5pZH0gcmVjaXBlIGRvZXMgbm90IG1hdGNoIGZsb29yIGxheW91dGApXG4gIGNhcnZlUmVjdChmbG9vciwgeyB4OiAxLCB5OiAxLCB3OiBmbG9vci53aWR0aCAtIDIsIGg6IGZsb29yLmhlaWdodCAtIDIgfSlcbiAgY2FydmVGcm9zdExhbmRzY2FwZShmbG9vciwgZmxvb3IubGF5b3V0SWQucmVwbGFjZSgnLXJlbWl4JywgJycpKVxuICBjb25zdCB0b1Jvb20gPSAobm9kZTogTWFjcm9SZWNpcGVEZWJ1Z1snbm9kZXMnXVtudW1iZXJdKTogUm9vbSA9PiAoeyB4OiBub2RlLmZvb3RwcmludC54LCB5OiBub2RlLmZvb3RwcmludC55LCB3OiBub2RlLmZvb3RwcmludC53aWR0aCwgaDogbm9kZS5mb290cHJpbnQuaGVpZ2h0IH0pXG4gIGNvbnN0IHJvb21zID0gbWFjcm8ubm9kZXMubWFwKHRvUm9vbSlcbiAgcm9vbXMuZm9yRWFjaChyb29tID0+IGNhcnZlUmVjdChmbG9vciwgcm9vbSkpXG4gIG1hY3JvQ29ubmVjdG9yUG9pbnRzKG1hY3JvKS5mb3JFYWNoKHBvaW50ID0+IHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksICdmbG9vcicpKVxuICBjb25zdCBieUtpbmQgPSBuZXcgTWFwKG1hY3JvLm5vZGVzLm1hcChub2RlID0+IFtub2RlLmtpbmQsIHRvUm9vbShub2RlKV0pKVxuICBjb25zdCBvcmRlcmVkOiBSb3V0ZU5vZGVLaW5kW10gPSBbJ3N0YXJ0JywgJ2xhbmRtYXJrJywgJ2ZvcmsnLCAnb3B0aW9uYWxSZXdhcmQnLCAnb2JqZWN0aXZlJywgZmxvb3IuaW5kZXggJSA0ID09PSAzID8gJ2Jvc3MnIDogJ2V4aXQnXVxuICByZXR1cm4gb3JkZXJlZC5tYXAoa2luZCA9PiBieUtpbmQuZ2V0KGtpbmQpKS5maWx0ZXIoKHJvb20pOiByb29tIGlzIFJvb20gPT4gQm9vbGVhbihyb29tKSlcbn1cblxuY29uc3QgcmVzdG9yZU1hY3JvQ29ubmVjdG9ycyA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCByZXNlcnZlZDogUmVhZG9ubHlTZXQ8bnVtYmVyPik6IHZvaWQgPT4ge1xuICBpZiAoIXJlc2VydmVkLnNpemUpIHJldHVyblxuICBmb3IgKGNvbnN0IHBvaW50IG9mIG1hY3JvQ29ubmVjdG9yUG9pbnRzKG1hY3JvKSkgaWYgKHJlc2VydmVkLmhhcyhpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkpIHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksICdmbG9vcicpXG59XG5cbmNvbnN0IHJlc3RvcmVNYWNyb05vZGVUcmFuc2l0ID0gKGZsb29yOiBGbG9vciwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcpOiB2b2lkID0+IHtcbiAgZm9yIChjb25zdCBub2RlIG9mIG1hY3JvLm5vZGVzKSB7XG4gICAgY29uc3QgY2VudGVyID0geyB4OiBub2RlLmZvb3RwcmludC54ICsgTWF0aC5mbG9vcihub2RlLmZvb3RwcmludC53aWR0aCAvIDIpLCB5OiBub2RlLmZvb3RwcmludC55ICsgTWF0aC5mbG9vcihub2RlLmZvb3RwcmludC5oZWlnaHQgLyAyKSB9XG4gICAgY29uc3QgZW5kcG9pbnRzID0gbWFjcm8uZWRnZXMuZmxhdE1hcChlZGdlID0+IFtlZGdlLmZyb20sIGVkZ2UudG9dKS5maWx0ZXIocG9pbnQgPT4gcG9pbnQueCA+PSBub2RlLmZvb3RwcmludC54ICYmIHBvaW50LnggPCBub2RlLmZvb3RwcmludC54ICsgbm9kZS5mb290cHJpbnQud2lkdGggJiYgcG9pbnQueSA+PSBub2RlLmZvb3RwcmludC55ICYmIHBvaW50LnkgPCBub2RlLmZvb3RwcmludC55ICsgbm9kZS5mb290cHJpbnQuaGVpZ2h0KVxuICAgIGNvbnN0IHBvaW50cyA9IGZsb29yLmJpb21lID09PSAnY2F2ZXJucydcbiAgICAgID8gQXJyYXkuZnJvbSh7IGxlbmd0aDogbm9kZS5mb290cHJpbnQud2lkdGggKiBub2RlLmZvb3RwcmludC5oZWlnaHQgfSwgKF8sIGluZGV4KSA9PiAoeyB4OiBub2RlLmZvb3RwcmludC54ICsgaW5kZXggJSBub2RlLmZvb3RwcmludC53aWR0aCwgeTogbm9kZS5mb290cHJpbnQueSArIE1hdGguZmxvb3IoaW5kZXggLyBub2RlLmZvb3RwcmludC53aWR0aCkgfSkpXG4gICAgICA6IGVuZHBvaW50cy5mbGF0TWFwKGVuZHBvaW50ID0+IHtcbiAgICAgICAgY29uc3Qgcm91dGU6IFBvaW50W10gPSBbXVxuICAgICAgICBmb3IgKGxldCB4ID0gTWF0aC5taW4oY2VudGVyLngsIGVuZHBvaW50LngpOyB4IDw9IE1hdGgubWF4KGNlbnRlci54LCBlbmRwb2ludC54KTsgeCsrKSByb3V0ZS5wdXNoKHsgeCwgeTogY2VudGVyLnkgfSlcbiAgICAgICAgZm9yIChsZXQgeSA9IE1hdGgubWluKGNlbnRlci55LCBlbmRwb2ludC55KTsgeSA8PSBNYXRoLm1heChjZW50ZXIueSwgZW5kcG9pbnQueSk7IHkrKykgcm91dGUucHVzaCh7IHg6IGVuZHBvaW50LngsIHkgfSlcbiAgICAgICAgcmV0dXJuIHJvdXRlXG4gICAgICB9KVxuICAgIGZvciAoY29uc3QgcG9pbnQgb2YgcG9pbnRzKSBpZiAocG9pbnQueCAhPT0gZmxvb3IuZXhpdC54IHx8IHBvaW50LnkgIT09IGZsb29yLmV4aXQueSkgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2Zsb29yJylcbiAgfVxufVxuXG5jb25zdCBpbXByaW50TWluZVJhaWxTZXJ2aWNlUm91dGUgPSAoZmxvb3I6IEZsb29yLCBtYWNybzogTWFjcm9SZWNpcGVEZWJ1Zyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdtaW5lJykgcmV0dXJuXG4gIGNvbnN0IGxhbmRtYXJrID0gbWFjcm8ubm9kZXMuZmluZChub2RlID0+IG5vZGUua2luZCA9PT0gJ2xhbmRtYXJrJylcbiAgaWYgKCFsYW5kbWFyayB8fCBsYW5kbWFyay5mb290cHJpbnQud2lkdGggPCA0KSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgTWluZSByYWlsIHNlcnZpY2Ugcm91dGU6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgY29uc3QgeSA9IGxhbmRtYXJrLmZvb3RwcmludC55ICsgTWF0aC5mbG9vcihsYW5kbWFyay5mb290cHJpbnQuaGVpZ2h0IC8gMilcbiAgZm9yIChsZXQgeCA9IGxhbmRtYXJrLmZvb3RwcmludC54ICsgMTsgeCA8IGxhbmRtYXJrLmZvb3RwcmludC54ICsgbGFuZG1hcmsuZm9vdHByaW50LndpZHRoIC0gMTsgeCsrKSBzZXRLaW5kKGZsb29yLCB4LCB5LCAncmFpbCcpXG59XG5cbmNvbnN0IG1pbmVCcmVhY2hEaXJlY3Rpb25zID0gW1xuICB7IHg6IDAsIHk6IC0xLCBjcm9zczogeyB4OiAxLCB5OiAwIH0gfSxcbiAgeyB4OiAxLCB5OiAwLCBjcm9zczogeyB4OiAwLCB5OiAxIH0gfSxcbiAgeyB4OiAwLCB5OiAxLCBjcm9zczogeyB4OiAxLCB5OiAwIH0gfSxcbiAgeyB4OiAtMSwgeTogMCwgY3Jvc3M6IHsgeDogMCwgeTogMSB9IH1cbl0gYXMgY29uc3RcblxuY29uc3QgYnJlYWNoQ2hhbWJlciA9IChmbG9vcjogRmxvb3IsIGFwcHJvYWNoOiBQb2ludCwgZGlyZWN0aW9uOiB0eXBlb2YgbWluZUJyZWFjaERpcmVjdGlvbnNbbnVtYmVyXSwgZGVwdGg6IG51bWJlciwgd2lkdGg6IG51bWJlcik6IFBvaW50W10gfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBlbnRyeSA9IHsgeDogYXBwcm9hY2gueCArIGRpcmVjdGlvbi54LCB5OiBhcHByb2FjaC55ICsgZGlyZWN0aW9uLnkgfVxuICBjb25zdCByYWRpdXMgPSBNYXRoLmZsb29yKHdpZHRoIC8gMilcbiAgY29uc3QgY2hhbWJlcjogUG9pbnRbXSA9IFtdXG4gIGZvciAobGV0IGZvcndhcmQgPSAyOyBmb3J3YXJkIDwgZGVwdGggKyAyOyBmb3J3YXJkKyspIGZvciAobGV0IGxhdGVyYWwgPSAtcmFkaXVzOyBsYXRlcmFsIDw9IHJhZGl1czsgbGF0ZXJhbCsrKSB7XG4gICAgY29uc3QgcG9pbnQgPSB7IHg6IGFwcHJvYWNoLnggKyBkaXJlY3Rpb24ueCAqIGZvcndhcmQgKyBkaXJlY3Rpb24uY3Jvc3MueCAqIGxhdGVyYWwsIHk6IGFwcHJvYWNoLnkgKyBkaXJlY3Rpb24ueSAqIGZvcndhcmQgKyBkaXJlY3Rpb24uY3Jvc3MueSAqIGxhdGVyYWwgfVxuICAgIGlmICghZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkgfHwgZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgIT09ICd3YWxsJykgcmV0dXJuIHVuZGVmaW5lZFxuICAgIGNoYW1iZXIucHVzaChwb2ludClcbiAgfVxuICBjb25zdCBjaGFtYmVySW5kZXhlcyA9IG5ldyBTZXQoY2hhbWJlci5tYXAocG9pbnQgPT4gaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKVxuICBmb3IgKGNvbnN0IHBvaW50IG9mIGNoYW1iZXIpIGZvciAoY29uc3QgW3gsIHldIG9mIHBhdGhPZmZzZXRzKSB7XG4gICAgY29uc3QgbmVpZ2hib3IgPSB7IHg6IHBvaW50LnggKyB4LCB5OiBwb2ludC55ICsgeSB9XG4gICAgaWYgKG5laWdoYm9yLnggPT09IGVudHJ5LnggJiYgbmVpZ2hib3IueSA9PT0gZW50cnkueSkgY29udGludWVcbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgbmVpZ2hib3IueCwgbmVpZ2hib3IueSlcbiAgICBpZiAoIXRpbGUgfHwgKCFjaGFtYmVySW5kZXhlcy5oYXMoaW5kZXhPZihmbG9vciwgbmVpZ2hib3IueCwgbmVpZ2hib3IueSkpICYmIHRpbGUua2luZCAhPT0gJ3dhbGwnKSkgcmV0dXJuIHVuZGVmaW5lZFxuICB9XG4gIHJldHVybiBjaGFtYmVyXG59XG5cbmNvbnN0IGltcHJpbnRNaW5lQnJlYWNoUm9vbXMgPSAoZmxvb3I6IEZsb29yLCBybmc6IFJuZyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdtaW5lJykgcmV0dXJuXG4gIGNvbnN0IGFyZWFGbG9vciA9IGZsb29yLmluZGV4ICUgNFxuICBjb25zdCBkZXNpcmVkID0gYXJlYUZsb29yICsgMVxuICBjb25zdCByZWFjaGFibGUgPSBbLi4ucmVhY2hhYmxlRmxvb3JJbmRleGVzKGZsb29yKV0ubWFwKGluZGV4ID0+IHBvaW50QXQoZmxvb3IsIGluZGV4KSlcbiAgY29uc3QgY2FuZGlkYXRlcyA9IHJuZy5zaHVmZmxlKHJlYWNoYWJsZS5mbGF0TWFwKGFwcHJvYWNoID0+IG1pbmVCcmVhY2hEaXJlY3Rpb25zLm1hcChkaXJlY3Rpb24gPT4gKHsgYXBwcm9hY2gsIGRpcmVjdGlvbiB9KSkpKVxuICBjb25zdCByZXNlcnZlZCA9IG5ldyBTZXQ8bnVtYmVyPigpXG4gIGNvbnN0IHNpZGVTcGFjZXM6IE1pbmVCcmVhY2hSb29tW10gPSBbXVxuICBmb3IgKGNvbnN0IGNhbmRpZGF0ZSBvZiBjYW5kaWRhdGVzKSB7XG4gICAgaWYgKHNpZGVTcGFjZXMubGVuZ3RoID09PSBkZXNpcmVkKSBicmVha1xuICAgIGNvbnN0IGVudHJ5ID0geyB4OiBjYW5kaWRhdGUuYXBwcm9hY2gueCArIGNhbmRpZGF0ZS5kaXJlY3Rpb24ueCwgeTogY2FuZGlkYXRlLmFwcHJvYWNoLnkgKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnkgfVxuICAgIGlmIChnZXRUaWxlKGZsb29yLCBlbnRyeS54LCBlbnRyeS55KT8ua2luZCAhPT0gJ3dhbGwnIHx8IHJlc2VydmVkLmhhcyhpbmRleE9mKGZsb29yLCBlbnRyeS54LCBlbnRyeS55KSkpIGNvbnRpbnVlXG4gICAgY29uc3QgY2hhbWJlciA9IGJyZWFjaENoYW1iZXIoZmxvb3IsIGNhbmRpZGF0ZS5hcHByb2FjaCwgY2FuZGlkYXRlLmRpcmVjdGlvbiwgMiArIE1hdGguZmxvb3IoKGFyZWFGbG9vciArIHNpZGVTcGFjZXMubGVuZ3RoKSAvIDIpLCAzICsgKGFyZWFGbG9vciA+IDEgPyAyIDogMCkpXG4gICAgaWYgKCFjaGFtYmVyIHx8IGNoYW1iZXIuc29tZShwb2ludCA9PiByZXNlcnZlZC5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKSkgY29udGludWVcbiAgICBjb25zdCByZXdhcmRQb2ludCA9IGNoYW1iZXJbTWF0aC5mbG9vcihjaGFtYmVyLmxlbmd0aCAvIDIpXSFcbiAgICBjb25zdCByZXdhcmQgPSB7IGlkOiBzaWRlU3BhY2VzLmxlbmd0aCAlIDIgPyAncm9wZUJ1bmRsZScgOiAnYm9tYlBhY2snLCB4OiByZXdhcmRQb2ludC54LCB5OiByZXdhcmRQb2ludC55LCBjb3VudDogMSwgdmlzaWJsZUluRm9nOiB0cnVlIH1cbiAgICBjb25zdCB0cmFuc2l0aW9uID0gc2lkZVNwYWNlcy5sZW5ndGggPT09IGRlc2lyZWQgLSAxICYmIGFyZWFGbG9vciA9PT0gMSA/IHsga2luZDogJ2Zsb29yU2tpcCcgYXMgY29uc3QsIHRhcmdldEJpb21lOiBmbG9vci5iaW9tZSwgdGFyZ2V0Rmxvb3I6IDMgfSA6IHVuZGVmaW5lZFxuICAgIGdldFRpbGUoZmxvb3IsIGVudHJ5LngsIGVudHJ5LnkpIS5raW5kID0gJ2JyZWFrd2FsbCdcbiAgICBjaGFtYmVyLmZvckVhY2gocG9pbnQgPT4geyBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KSEua2luZCA9ICdmbG9vcic7IHJlc2VydmVkLmFkZChpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkgfSlcbiAgICByZXNlcnZlZC5hZGQoaW5kZXhPZihmbG9vciwgZW50cnkueCwgZW50cnkueSkpXG4gICAgZmxvb3IuaXRlbXMucHVzaChyZXdhcmQpXG4gICAgc2lkZVNwYWNlcy5wdXNoKHsgaWQ6IGBtaW5lLWJyZWFjaDoke2Zsb29yLnNlZWR9OiR7c2lkZVNwYWNlcy5sZW5ndGh9YCwga2luZDogJ21pbmUtYnJlYWNoLXJvb20nLCBhcHByb2FjaDogeyAuLi5jYW5kaWRhdGUuYXBwcm9hY2ggfSwgZW50cnksIGNoYW1iZXIsIHJld2FyZCwgLi4uKHRyYW5zaXRpb24gPyB7IHJhcmVUcmFuc2l0aW9uOiB0cmFuc2l0aW9uIH0gOiB7fSkgfSlcbiAgfVxuICBpZiAoc2lkZVNwYWNlcy5sZW5ndGggIT09IGRlc2lyZWQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIE1pbmUgYnJlYWNoLXJvb20gZ2VuZXJhdGlvbjogZXhwZWN0ZWQgJHtkZXNpcmVkfSwgZm91bmQgJHtzaWRlU3BhY2VzLmxlbmd0aH1gKVxuICBmbG9vci5zaWRlU3BhY2VzID0gc2lkZVNwYWNlc1xufVxuXG5jb25zdCBpbXByaW50V2lsZHNDYXZlcyA9IChmbG9vcjogRmxvb3IsIHJuZzogUm5nKTogdm9pZCA9PiB7XG4gIGlmIChmbG9vci5iaW9tZSAhPT0gJ3dpbGRzJykgcmV0dXJuXG4gIGNvbnN0IGRlc2lyZWQgPSAxICsgTWF0aC5mbG9vcigoZmxvb3IuaW5kZXggJSA0KSAvIDIpXG4gIGNvbnN0IHJlYWNoYWJsZSA9IFsuLi5yZWFjaGFibGVGbG9vckluZGV4ZXMoZmxvb3IpXS5tYXAoaW5kZXggPT4gcG9pbnRBdChmbG9vciwgaW5kZXgpKVxuICBjb25zdCBjYW5kaWRhdGVzID0gcm5nLnNodWZmbGUocmVhY2hhYmxlLmZsYXRNYXAoYXBwcm9hY2ggPT4gbWluZUJyZWFjaERpcmVjdGlvbnMubWFwKGRpcmVjdGlvbiA9PiAoeyBhcHByb2FjaCwgZGlyZWN0aW9uIH0pKSkpXG4gIGNvbnN0IGNhdmVzOiBXaWxkc0NhdmVbXSA9IFtdXG4gIGNvbnN0IHJlc2VydmVkID0gbmV3IFNldDxudW1iZXI+KClcbiAgZm9yIChjb25zdCBjYW5kaWRhdGUgb2YgY2FuZGlkYXRlcykge1xuICAgIGlmIChjYXZlcy5sZW5ndGggPT09IGRlc2lyZWQpIGJyZWFrXG4gICAgY29uc3QgZW50cnkgPSB7IHg6IGNhbmRpZGF0ZS5hcHByb2FjaC54ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi54LCB5OiBjYW5kaWRhdGUuYXBwcm9hY2gueSArIGNhbmRpZGF0ZS5kaXJlY3Rpb24ueSB9XG4gICAgaWYgKGdldFRpbGUoZmxvb3IsIGVudHJ5LngsIGVudHJ5LnkpPy5raW5kICE9PSAnd2FsbCcgfHwgcmVzZXJ2ZWQuaGFzKGluZGV4T2YoZmxvb3IsIGVudHJ5LngsIGVudHJ5LnkpKSkgY29udGludWVcbiAgICBjb25zdCBjaGFtYmVyID0gYnJlYWNoQ2hhbWJlcihmbG9vciwgY2FuZGlkYXRlLmFwcHJvYWNoLCBjYW5kaWRhdGUuZGlyZWN0aW9uLCAxLCAzKVxuICAgIGlmICghY2hhbWJlciB8fCBjaGFtYmVyLnNvbWUocG9pbnQgPT4gcmVzZXJ2ZWQuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSkpIGNvbnRpbnVlXG4gICAgY29uc3QgcmV3YXJkUG9pbnQgPSBjaGFtYmVyWzFdIVxuICAgIGNvbnN0IHJld2FyZCA9IHsgaWQ6IGNhdmVzLmxlbmd0aCAlIDIgPyAncm9wZUJ1bmRsZScgOiAnZm9jdXNUb25pYycsIHg6IHJld2FyZFBvaW50LngsIHk6IHJld2FyZFBvaW50LnksIGNvdW50OiAxLCB2aXNpYmxlSW5Gb2c6IHRydWUgfVxuICAgIGdldFRpbGUoZmxvb3IsIGVudHJ5LngsIGVudHJ5LnkpIS5raW5kID0gJ2JyZWFrd2FsbCdcbiAgICByZXNlcnZlZC5hZGQoaW5kZXhPZihmbG9vciwgZW50cnkueCwgZW50cnkueSkpXG4gICAgY2hhbWJlci5mb3JFYWNoKHBvaW50ID0+IHsgZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhLmtpbmQgPSAnZmxvb3InOyByZXNlcnZlZC5hZGQoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpIH0pXG4gICAgZmxvb3IuaXRlbXMucHVzaChyZXdhcmQpXG4gICAgY2F2ZXMucHVzaCh7IGlkOiBgd2lsZHMtY2F2ZToke2Zsb29yLnNlZWR9OiR7Y2F2ZXMubGVuZ3RofWAsIGtpbmQ6ICd3aWxkcy1jYXZlJywgYXBwcm9hY2g6IHsgLi4uY2FuZGlkYXRlLmFwcHJvYWNoIH0sIGVudHJ5LCBjaGFtYmVyLCByZXdhcmQgfSlcbiAgfVxuICBpZiAoY2F2ZXMubGVuZ3RoICE9PSBkZXNpcmVkKSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCBXaWxkcyBjYXZlIGdlbmVyYXRpb246IGV4cGVjdGVkICR7ZGVzaXJlZH0sIGZvdW5kICR7Y2F2ZXMubGVuZ3RofWApXG4gIGZsb29yLnNpZGVTcGFjZXMgPSBjYXZlc1xufVxuXG5jb25zdCBpbXByaW50Q2F2ZXJuSGlkZGVuQ2hhbWJlcnMgPSAoZmxvb3I6IEZsb29yLCBybmc6IFJuZyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdjYXZlcm5zJykgcmV0dXJuXG4gIGNvbnN0IGRlc2lyZWQgPSAxICsgTWF0aC5mbG9vcigoZmxvb3IuaW5kZXggJSA0KSAvIDIpXG4gIGNvbnN0IHJlYWNoYWJsZSA9IFsuLi5yZWFjaGFibGVGbG9vckluZGV4ZXMoZmxvb3IpXS5tYXAoaW5kZXggPT4gcG9pbnRBdChmbG9vciwgaW5kZXgpKVxuICBjb25zdCBjYW5kaWRhdGVzID0gcm5nLnNodWZmbGUocmVhY2hhYmxlLmZsYXRNYXAoYXBwcm9hY2ggPT4gbWluZUJyZWFjaERpcmVjdGlvbnMubWFwKGRpcmVjdGlvbiA9PiAoeyBhcHByb2FjaCwgZGlyZWN0aW9uIH0pKSkpXG4gIGNvbnN0IGNoYW1iZXJzOiBDYXZlcm5IaWRkZW5DaGFtYmVyW10gPSBbXVxuICBjb25zdCByZXNlcnZlZCA9IG5ldyBTZXQ8bnVtYmVyPigpXG4gIGZvciAoY29uc3QgY2FuZGlkYXRlIG9mIGNhbmRpZGF0ZXMpIHtcbiAgICBpZiAoY2hhbWJlcnMubGVuZ3RoID09PSBkZXNpcmVkKSBicmVha1xuICAgIGNvbnN0IGVudHJ5ID0geyB4OiBjYW5kaWRhdGUuYXBwcm9hY2gueCArIGNhbmRpZGF0ZS5kaXJlY3Rpb24ueCwgeTogY2FuZGlkYXRlLmFwcHJvYWNoLnkgKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnkgfVxuICAgIGlmIChnZXRUaWxlKGZsb29yLCBjYW5kaWRhdGUuYXBwcm9hY2gueCwgY2FuZGlkYXRlLmFwcHJvYWNoLnkpPy5raW5kICE9PSAnZmxvb3InIHx8IGdldFRpbGUoZmxvb3IsIGVudHJ5LngsIGVudHJ5LnkpPy5raW5kICE9PSAnd2FsbCcgfHwgcmVzZXJ2ZWQuaGFzKGluZGV4T2YoZmxvb3IsIGVudHJ5LngsIGVudHJ5LnkpKSkgY29udGludWVcbiAgICBjb25zdCBjaGFtYmVyID0gYnJlYWNoQ2hhbWJlcihmbG9vciwgY2FuZGlkYXRlLmFwcHJvYWNoLCBjYW5kaWRhdGUuZGlyZWN0aW9uLCAyLCAzKVxuICAgIGlmICghY2hhbWJlciB8fCBjaGFtYmVyLnNvbWUocG9pbnQgPT4gcmVzZXJ2ZWQuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSkpIGNvbnRpbnVlXG4gICAgY29uc3QgcmV3YXJkUG9pbnQgPSBjaGFtYmVyW01hdGguZmxvb3IoY2hhbWJlci5sZW5ndGggLyAyKV0hXG4gICAgY29uc3QgcmV3YXJkID0geyBpZDogY2hhbWJlcnMubGVuZ3RoICUgMiA/ICd3YXRlclNjcmlwdCcgOiAncm9wZUJ1bmRsZScsIHg6IHJld2FyZFBvaW50LngsIHk6IHJld2FyZFBvaW50LnksIGNvdW50OiAxLCB2aXNpYmxlSW5Gb2c6IHRydWUgfVxuICAgIGNvbnN0IHdhdGVySGludCA9IHsgLi4uY2FuZGlkYXRlLmFwcHJvYWNoIH1cbiAgICBjb25zdCBoaW50VGlsZSA9IGdldFRpbGUoZmxvb3IsIHdhdGVySGludC54LCB3YXRlckhpbnQueSkhXG4gICAgaGludFRpbGUua2luZCA9ICdjdXJyZW50J1xuICAgIGhpbnRUaWxlLmZsb3cgPSB7IGRpcmVjdGlvbjogZmxvd0RpcmVjdGlvbihlbnRyeS54IC0gd2F0ZXJIaW50LngsIGVudHJ5LnkgLSB3YXRlckhpbnQueSkgfVxuICAgIGdldFRpbGUoZmxvb3IsIGVudHJ5LngsIGVudHJ5LnkpIS5raW5kID0gJ2JyZWFrd2FsbCdcbiAgICByZXNlcnZlZC5hZGQoaW5kZXhPZihmbG9vciwgZW50cnkueCwgZW50cnkueSkpXG4gICAgY2hhbWJlci5mb3JFYWNoKHBvaW50ID0+IHsgZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhLmtpbmQgPSAnZmxvb3InOyByZXNlcnZlZC5hZGQoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpIH0pXG4gICAgZmxvb3IuaXRlbXMucHVzaChyZXdhcmQpXG4gICAgY2hhbWJlcnMucHVzaCh7IGlkOiBgY2F2ZXJuLWhpZGRlbjoke2Zsb29yLnNlZWR9OiR7Y2hhbWJlcnMubGVuZ3RofWAsIGtpbmQ6ICdjYXZlcm4taGlkZGVuLWNoYW1iZXInLCBhcHByb2FjaDogeyAuLi5jYW5kaWRhdGUuYXBwcm9hY2ggfSwgZW50cnksIGNoYW1iZXIsIHJld2FyZCwgd2F0ZXJIaW50IH0pXG4gIH1cbiAgaWYgKGNoYW1iZXJzLmxlbmd0aCAhPT0gZGVzaXJlZCkgdGhyb3cgbmV3IEVycm9yKGBmYWlsZWQgQ2F2ZXJuIGhpZGRlbi1jaGFtYmVyIGdlbmVyYXRpb246IGV4cGVjdGVkICR7ZGVzaXJlZH0sIGZvdW5kICR7Y2hhbWJlcnMubGVuZ3RofWApXG4gIGZsb29yLnNpZGVTcGFjZXMgPSBjaGFtYmVyc1xufVxuXG5jb25zdCBpbXByaW50UnVpbnNIaWRkZW5DaGFtYmVycyA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBybmc6IFJuZyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdydWlucycpIHJldHVyblxuICBjb25zdCBkZXNpcmVkID0gMSArIE1hdGguZmxvb3IoKGZsb29yLmluZGV4ICUgNCkgLyAyKVxuICBjb25zdCByZWFjaGFibGUgPSBbLi4ucmVhY2hhYmxlRmxvb3JJbmRleGVzKGZsb29yKV0ubWFwKGluZGV4ID0+IHBvaW50QXQoZmxvb3IsIGluZGV4KSlcbiAgY29uc3QgY2FuZGlkYXRlcyA9IHJuZy5zaHVmZmxlKHJlYWNoYWJsZS5mbGF0TWFwKGFwcHJvYWNoID0+IG1pbmVCcmVhY2hEaXJlY3Rpb25zLm1hcChkaXJlY3Rpb24gPT4gKHsgYXBwcm9hY2gsIGRpcmVjdGlvbiB9KSkpKVxuICBjb25zdCBjaGFtYmVyczogUml0dWFsSGlkZGVuQ2hhbWJlcltdID0gW11cbiAgY29uc3QgcmVzZXJ2ZWQgPSBuZXcgU2V0PG51bWJlcj4oKVxuICBjb25zdCBtYWNyb0NlbGxzID0gbmV3IFNldChtYWNyb0Nvbm5lY3RvclBvaW50cyhtYWNybykubWFwKHBvaW50ID0+IGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSlcbiAgZm9yIChjb25zdCBjYW5kaWRhdGUgb2YgY2FuZGlkYXRlcykge1xuICAgIGlmIChjaGFtYmVycy5sZW5ndGggPT09IGRlc2lyZWQpIGJyZWFrXG4gICAgY29uc3QgZW50cnkgPSB7IHg6IGNhbmRpZGF0ZS5hcHByb2FjaC54ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi54LCB5OiBjYW5kaWRhdGUuYXBwcm9hY2gueSArIGNhbmRpZGF0ZS5kaXJlY3Rpb24ueSB9XG4gICAgY29uc3QgY2hhbWJlciA9IFtdIGFzIFBvaW50W11cbiAgICBmb3IgKGxldCBmb3J3YXJkID0gMjsgZm9yd2FyZCA8IDQ7IGZvcndhcmQrKykgZm9yIChsZXQgbGF0ZXJhbCA9IC0xOyBsYXRlcmFsIDw9IDE7IGxhdGVyYWwrKykgY2hhbWJlci5wdXNoKHsgeDogY2FuZGlkYXRlLmFwcHJvYWNoLnggKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnggKiBmb3J3YXJkICsgY2FuZGlkYXRlLmRpcmVjdGlvbi5jcm9zcy54ICogbGF0ZXJhbCwgeTogY2FuZGlkYXRlLmFwcHJvYWNoLnkgKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnkgKiBmb3J3YXJkICsgY2FuZGlkYXRlLmRpcmVjdGlvbi5jcm9zcy55ICogbGF0ZXJhbCB9KVxuICAgIGNvbnN0IGNoYW1iZXJJbmRleGVzID0gbmV3IFNldChjaGFtYmVyLm1hcChwb2ludCA9PiBpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkpXG4gICAgY29uc3QgYmFycmllciA9IGNoYW1iZXIuZmxhdE1hcChwb2ludCA9PiBwYXRoT2Zmc2V0cy5tYXAoKFt4LCB5XSkgPT4gKHsgeDogcG9pbnQueCArIHgsIHk6IHBvaW50LnkgKyB5IH0pKSkuZmlsdGVyKHBvaW50ID0+ICFjaGFtYmVySW5kZXhlcy5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpICYmIChwb2ludC54ICE9PSBlbnRyeS54IHx8IHBvaW50LnkgIT09IGVudHJ5LnkpKS5maWx0ZXIoKHBvaW50LCBpbmRleCwgcG9pbnRzKSA9PiBwb2ludHMuZmluZEluZGV4KG90aGVyID0+IG90aGVyLnggPT09IHBvaW50LnggJiYgb3RoZXIueSA9PT0gcG9pbnQueSkgPT09IGluZGV4KVxuICAgIGNvbnN0IGNoYW5nZWQgPSBbZW50cnksIC4uLmNoYW1iZXIsIC4uLmJhcnJpZXJdXG4gICAgaWYgKGNoYW5nZWQuc29tZShwb2ludCA9PiAhZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkgfHwgZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgIT09ICdmbG9vcicgfHwgcmVzZXJ2ZWQuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSB8fCBtYWNyb0NlbGxzLmhhcyhpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkpKSBjb250aW51ZVxuICAgIGNvbnN0IGJlZm9yZSA9IGNoYW5nZWQubWFwKHBvaW50ID0+ICh7IHBvaW50LCBraW5kOiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KSEua2luZCB9KSlcbiAgICBiYXJyaWVyLmZvckVhY2gocG9pbnQgPT4gc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ3dhbGwnKSlcbiAgICBzZXRLaW5kKGZsb29yLCBlbnRyeS54LCBlbnRyeS55LCAnYnJlYWt3YWxsJylcbiAgICBpZiAoIWhhc1Bhc3NhYmxlUGF0aChmbG9vciwgZmxvb3Iuc3RhcnQsIGZsb29yLmV4aXQpKSB7XG4gICAgICBiZWZvcmUuZm9yRWFjaCgoeyBwb2ludCwga2luZCB9KSA9PiBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCBraW5kKSlcbiAgICAgIGNvbnRpbnVlXG4gICAgfVxuICAgIGNvbnN0IHJld2FyZFBvaW50ID0gY2hhbWJlcltNYXRoLmZsb29yKGNoYW1iZXIubGVuZ3RoIC8gMildIVxuICAgIGNvbnN0IHJld2FyZCA9IHsgaWQ6IGNoYW1iZXJzLmxlbmd0aCAlIDIgPyAnc3Vuc2VhbCcgOiAnd2FyZFNjcmlwdCcsIHg6IHJld2FyZFBvaW50LngsIHk6IHJld2FyZFBvaW50LnksIGNvdW50OiAxLCB2aXNpYmxlSW5Gb2c6IHRydWUgfVxuICAgIGNoYW5nZWQuZm9yRWFjaChwb2ludCA9PiByZXNlcnZlZC5hZGQoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKVxuICAgIGZsb29yLml0ZW1zLnB1c2gocmV3YXJkKVxuICAgIGNoYW1iZXJzLnB1c2goeyBpZDogYHJpdHVhbC1oaWRkZW46JHtmbG9vci5zZWVkfToke2NoYW1iZXJzLmxlbmd0aH1gLCBraW5kOiAncml0dWFsLWhpZGRlbi1jaGFtYmVyJywgYXBwcm9hY2g6IHsgLi4uY2FuZGlkYXRlLmFwcHJvYWNoIH0sIGVudHJ5LCBjaGFtYmVyLCByZXdhcmQgfSlcbiAgfVxuICBpZiAoY2hhbWJlcnMubGVuZ3RoICE9PSBkZXNpcmVkKSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCByaXR1YWwgaGlkZGVuLWNoYW1iZXIgZ2VuZXJhdGlvbjogZXhwZWN0ZWQgJHtkZXNpcmVkfSwgZm91bmQgJHtjaGFtYmVycy5sZW5ndGh9YClcbiAgZmxvb3Iuc2lkZVNwYWNlcyA9IGNoYW1iZXJzXG59XG5cbmNvbnN0IGltcHJpbnRGdXJuYWNlU2VydmljZVNwYWNlcyA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBybmc6IFJuZyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdmdXJuYWNlJykgcmV0dXJuXG4gIGNvbnN0IGRlc2lyZWQgPSAxICsgTWF0aC5mbG9vcigoZmxvb3IuaW5kZXggJSA0KSAvIDIpXG4gIGNvbnN0IHJlYWNoYWJsZSA9IFsuLi5yZWFjaGFibGVGbG9vckluZGV4ZXMoZmxvb3IpXS5tYXAoaW5kZXggPT4gcG9pbnRBdChmbG9vciwgaW5kZXgpKVxuICBjb25zdCBjYW5kaWRhdGVzID0gcm5nLnNodWZmbGUocmVhY2hhYmxlLmZsYXRNYXAoYXBwcm9hY2ggPT4gbWluZUJyZWFjaERpcmVjdGlvbnMubWFwKGRpcmVjdGlvbiA9PiAoeyBhcHByb2FjaCwgZGlyZWN0aW9uIH0pKSkpXG4gIGNvbnN0IHNwYWNlczogRnVybmFjZVNlcnZpY2VTcGFjZVtdID0gW11cbiAgY29uc3QgcmVzZXJ2ZWQgPSBuZXcgU2V0PG51bWJlcj4oKVxuICBjb25zdCBtYWNyb0NlbGxzID0gbmV3IFNldChtYWNyb0Nvbm5lY3RvclBvaW50cyhtYWNybykubWFwKHBvaW50ID0+IGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSlcbiAgZm9yIChjb25zdCBjYW5kaWRhdGUgb2YgY2FuZGlkYXRlcykge1xuICAgIGlmIChzcGFjZXMubGVuZ3RoID09PSBkZXNpcmVkKSBicmVha1xuICAgIGNvbnN0IGVudHJ5ID0geyB4OiBjYW5kaWRhdGUuYXBwcm9hY2gueCArIGNhbmRpZGF0ZS5kaXJlY3Rpb24ueCwgeTogY2FuZGlkYXRlLmFwcHJvYWNoLnkgKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnkgfVxuICAgIGNvbnN0IGNoYW1iZXIgPSBbXSBhcyBQb2ludFtdXG4gICAgZm9yIChsZXQgZm9yd2FyZCA9IDI7IGZvcndhcmQgPCA0OyBmb3J3YXJkKyspIGZvciAobGV0IGxhdGVyYWwgPSAtMTsgbGF0ZXJhbCA8PSAxOyBsYXRlcmFsKyspIGNoYW1iZXIucHVzaCh7IHg6IGNhbmRpZGF0ZS5hcHByb2FjaC54ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi54ICogZm9yd2FyZCArIGNhbmRpZGF0ZS5kaXJlY3Rpb24uY3Jvc3MueCAqIGxhdGVyYWwsIHk6IGNhbmRpZGF0ZS5hcHByb2FjaC55ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi55ICogZm9yd2FyZCArIGNhbmRpZGF0ZS5kaXJlY3Rpb24uY3Jvc3MueSAqIGxhdGVyYWwgfSlcbiAgICBjb25zdCBjaGFtYmVySW5kZXhlcyA9IG5ldyBTZXQoY2hhbWJlci5tYXAocG9pbnQgPT4gaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKVxuICAgIGNvbnN0IGJhcnJpZXIgPSBjaGFtYmVyLmZsYXRNYXAocG9pbnQgPT4gcGF0aE9mZnNldHMubWFwKChbeCwgeV0pID0+ICh7IHg6IHBvaW50LnggKyB4LCB5OiBwb2ludC55ICsgeSB9KSkpLmZpbHRlcihwb2ludCA9PiAhY2hhbWJlckluZGV4ZXMuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSAmJiAocG9pbnQueCAhPT0gZW50cnkueCB8fCBwb2ludC55ICE9PSBlbnRyeS55KSkuZmlsdGVyKChwb2ludCwgaW5kZXgsIHBvaW50cykgPT4gcG9pbnRzLmZpbmRJbmRleChvdGhlciA9PiBvdGhlci54ID09PSBwb2ludC54ICYmIG90aGVyLnkgPT09IHBvaW50LnkpID09PSBpbmRleClcbiAgICBjb25zdCBjaGFuZ2VkID0gW2VudHJ5LCAuLi5jaGFtYmVyLCAuLi5iYXJyaWVyXVxuICAgIGlmIChjaGFuZ2VkLnNvbWUocG9pbnQgPT4gIWdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpIHx8IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kICE9PSAnZmxvb3InIHx8IHJlc2VydmVkLmhhcyhpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkgfHwgbWFjcm9DZWxscy5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKSkgY29udGludWVcbiAgICBjb25zdCBiZWZvcmUgPSBjaGFuZ2VkLm1hcChwb2ludCA9PiAoeyBwb2ludCwga2luZDogZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhLmtpbmQgfSkpXG4gICAgYmFycmllci5mb3JFYWNoKHBvaW50ID0+IHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksICd3YWxsJykpXG4gICAgc2V0S2luZChmbG9vciwgZW50cnkueCwgZW50cnkueSwgJ2JyZWFrd2FsbCcpXG4gICAgaWYgKCFoYXNQYXNzYWJsZVBhdGgoZmxvb3IsIGZsb29yLnN0YXJ0LCBmbG9vci5leGl0KSkgeyBiZWZvcmUuZm9yRWFjaCgoeyBwb2ludCwga2luZCB9KSA9PiBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCBraW5kKSk7IGNvbnRpbnVlIH1cbiAgICBjb25zdCByZXdhcmRQb2ludCA9IGNoYW1iZXJbTWF0aC5mbG9vcihjaGFtYmVyLmxlbmd0aCAvIDIpXSFcbiAgICBjb25zdCByZXdhcmQgPSB7IGlkOiBzcGFjZXMubGVuZ3RoICUgMiA/ICdicmVhY2hDaGFyZ2UnIDogJ3Nvb3RGaWx0ZXInLCB4OiByZXdhcmRQb2ludC54LCB5OiByZXdhcmRQb2ludC55LCBjb3VudDogMSwgdmlzaWJsZUluRm9nOiB0cnVlIH1cbiAgICBjaGFuZ2VkLmZvckVhY2gocG9pbnQgPT4gcmVzZXJ2ZWQuYWRkKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSlcbiAgICBmbG9vci5pdGVtcy5wdXNoKHJld2FyZClcbiAgICBzcGFjZXMucHVzaCh7IGlkOiBgZnVybmFjZS1zZXJ2aWNlOiR7Zmxvb3Iuc2VlZH06JHtzcGFjZXMubGVuZ3RofWAsIGtpbmQ6ICdmdXJuYWNlLXNlcnZpY2Utc3BhY2UnLCBhcHByb2FjaDogeyAuLi5jYW5kaWRhdGUuYXBwcm9hY2ggfSwgZW50cnksIGNoYW1iZXIsIHJld2FyZCB9KVxuICB9XG4gIGlmIChzcGFjZXMubGVuZ3RoICE9PSBkZXNpcmVkKSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCBGdXJuYWNlIHNlcnZpY2Utc3BhY2UgZ2VuZXJhdGlvbjogZXhwZWN0ZWQgJHtkZXNpcmVkfSwgZm91bmQgJHtzcGFjZXMubGVuZ3RofWApXG4gIGZsb29yLnNpZGVTcGFjZXMgPSBzcGFjZXNcbn1cblxuY29uc3QgaW1wcmludFJ1aW5zUml0dWFsQ2VudGVyID0gKGZsb29yOiBGbG9vciwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcpOiB2b2lkID0+IHtcbiAgaWYgKGZsb29yLmJpb21lICE9PSAncnVpbnMnKSByZXR1cm5cbiAgY29uc3Qgbm9kZSA9IG1hY3JvLm5vZGVzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5raW5kID09PSAnb2JqZWN0aXZlJylcbiAgaWYgKCFub2RlKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgUnVpbnMgcml0dWFsIGNlbnRlcjogJHttYWNyby5yZWNpcGVJZH1gKVxuICBjb25zdCBwb2ludCA9IHsgeDogbm9kZS5mb290cHJpbnQueCArIE1hdGguZmxvb3Iobm9kZS5mb290cHJpbnQud2lkdGggLyAyKSwgeTogbm9kZS5mb290cHJpbnQueSArIE1hdGguZmxvb3Iobm9kZS5mb290cHJpbnQuaGVpZ2h0IC8gMikgfVxuICBmb3IgKGNvbnN0IFt4LCB5XSBvZiBjYXJkaW5hbE9mZnNldHMpIHNldEtpbmQoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSwgJ2Zsb29yJylcbiAgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2FsdGFyJylcbn1cblxuY29uc3QgaW1wcmludENhdmVyblRpZGVSb3V0ZSA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnKTogdm9pZCA9PiB7XG4gIGlmIChmbG9vci5iaW9tZSAhPT0gJ2NhdmVybnMnKSByZXR1cm5cbiAgY29uc3QgZWRnZSA9IG1hY3JvLmVkZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnY29zdGx5JykgJiYgY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdvcHRpb25hbCcpKVxuICBpZiAoIWVkZ2UpIHRocm93IG5ldyBFcnJvcihgbWlzc2luZyBDYXZlcm5zIHRpZGUgcm91dGU6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgY29uc3QgY2VsbHMgPSBlZGdlLmNlbGxzLnNsaWNlKDMsIC0zKS5maWx0ZXIocG9pbnQgPT4gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpXG4gIGNvbnN0IGxlbmd0aCA9IE1hdGgubWluKGNlbGxzLmxlbmd0aCwgNSArIChmbG9vci5pbmRleCAlIDQpICogMilcbiAgY29uc3Qgc3RhcnQgPSBNYXRoLm1heCgwLCBNYXRoLmZsb29yKChjZWxscy5sZW5ndGggLSBsZW5ndGgpIC8gMikpXG4gIGNvbnN0IHJvdXRlID0gY2VsbHMuc2xpY2Uoc3RhcnQsIHN0YXJ0ICsgbGVuZ3RoKVxuICBpZiAocm91dGUubGVuZ3RoIDwgMykgdGhyb3cgbmV3IEVycm9yKGBzaG9ydCBDYXZlcm5zIHRpZGUgcm91dGU6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgZm9yIChsZXQgaW5kZXggPSAwOyBpbmRleCA8IHJvdXRlLmxlbmd0aDsgaW5kZXgrKykge1xuICAgIGNvbnN0IHBvaW50ID0gcm91dGVbaW5kZXhdXG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpXG4gICAgaWYgKCF0aWxlKSBjb250aW51ZVxuICAgIGlmIChpbmRleCAlIDIgPT09IDApIHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksICd3YXRlcicpXG4gICAgZWxzZSB7XG4gICAgICB0aWxlLmtpbmQgPSAnY3VycmVudCdcbiAgICAgIGNvbnN0IG5leHQgPSByb3V0ZVtpbmRleCArIDFdID8/IHJvdXRlW2luZGV4IC0gMV1cbiAgICAgIHRpbGUuZmxvdyA9IHsgZGlyZWN0aW9uOiBmbG93RGlyZWN0aW9uKG5leHQueCAtIHBvaW50LngsIG5leHQueSAtIHBvaW50LnkpIH1cbiAgICB9XG4gIH1cbiAgY29uc3QgZmxvb2QgPSBmbG9vci5pbmRleCAlIDRcbiAgY29uc3QgY2F2ZXJuQ2VsbHMgPSBtYWNyby5ub2Rlcy5mbGF0TWFwKG5vZGUgPT4gQXJyYXkuZnJvbSh7IGxlbmd0aDogTWF0aC5tYXgoMCwgKG5vZGUuZm9vdHByaW50LndpZHRoIC0gMikgKiAobm9kZS5mb290cHJpbnQuaGVpZ2h0IC0gMikpIH0sIChfLCBpbmRleCkgPT4gKHsgeDogbm9kZS5mb290cHJpbnQueCArIDEgKyBpbmRleCAlIChub2RlLmZvb3RwcmludC53aWR0aCAtIDIpLCB5OiBub2RlLmZvb3RwcmludC55ICsgMSArIE1hdGguZmxvb3IoaW5kZXggLyAobm9kZS5mb290cHJpbnQud2lkdGggLSAyKSkgfSkpKS5maWx0ZXIocG9pbnQgPT4gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpXG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCBmbG9vZCAqIDggJiYgY2F2ZXJuQ2VsbHMubGVuZ3RoOyBpbmRleCsrKSB7XG4gICAgY29uc3QgcG9pbnQgPSBjYXZlcm5DZWxsc1tpbmRleCAlIGNhdmVybkNlbGxzLmxlbmd0aF0hXG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpIVxuICAgIHRpbGUua2luZCA9ICdjdXJyZW50J1xuICAgIHRpbGUuZmxvdyA9IHsgZGlyZWN0aW9uOiBpbmRleCAlIDIgPyAnZScgOiAndycgfVxuICB9XG59XG5cbmNvbnN0IGltcHJpbnRXaWxkc1ByZXNzdXJlUm91dGUgPSAoZmxvb3I6IEZsb29yLCBtYWNybzogTWFjcm9SZWNpcGVEZWJ1Zyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICd3aWxkcycpIHJldHVyblxuICBjb25zdCBlZGdlID0gbWFjcm8uZWRnZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdjb3N0bHknKSAmJiBjYW5kaWRhdGUubW9kZXMuaW5jbHVkZXMoJ29wdGlvbmFsJykpXG4gIGlmICghZWRnZSkgdGhyb3cgbmV3IEVycm9yKGBtaXNzaW5nIFdpbGRzIHByZXNzdXJlIHJvdXRlOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGNvbnN0IGNlbGxzID0gZWRnZS5jZWxscy5zbGljZSgzLCAtMykuZmlsdGVyKHBvaW50ID0+IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kID09PSAnZmxvb3InKVxuICBjb25zdCBzdGFydCA9IE1hdGgubWF4KDAsIE1hdGguZmxvb3IoY2VsbHMubGVuZ3RoIC8gMikgLSAzKVxuICBjb25zdCByb3V0ZSA9IGNlbGxzLnNsaWNlKHN0YXJ0LCBzdGFydCArIDcpXG4gIGlmIChyb3V0ZS5sZW5ndGggPCAzKSB0aHJvdyBuZXcgRXJyb3IoYHNob3J0IFdpbGRzIHByZXNzdXJlIHJvdXRlOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCByb3V0ZS5sZW5ndGg7IGluZGV4KyspIHNldEtpbmQoZmxvb3IsIHJvdXRlW2luZGV4XS54LCByb3V0ZVtpbmRleF0ueSwgaW5kZXggJSAyID09PSAwID8gJ3dhdGVyJyA6ICd3ZWInKVxuICBjb25zdCBzYWZlID0gbWFjcm8uZWRnZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdzYWZlJykpXG4gIGNvbnN0IHNhZmVSb3V0ZSA9IHNhZmU/LmNlbGxzLnNsaWNlKDMsIC0zKS5maWx0ZXIocG9pbnQgPT4gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpID8/IFtdXG4gIGNvbnN0IGxvd2VyID0gc2FmZVJvdXRlWzFdXG4gIGNvbnN0IHVwcGVyID0gcm91dGUuYXQoLTIpXG4gIGlmICghbG93ZXIgfHwgIXVwcGVyKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgV2lsZHMgY2xpbWIgcm91dGU6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgZ2V0VGlsZShmbG9vciwgbG93ZXIueCwgbG93ZXIueSkhLmVsZXZhdGlvbiA9IDBcbiAgZ2V0VGlsZShmbG9vciwgdXBwZXIueCwgdXBwZXIueSkhLmVsZXZhdGlvbiA9IDFcbiAgZmxvb3IuY2xpbWJMaW5rcyA9IFt7IGlkOiBgd2lsZHMtY2xpbWI6JHtmbG9vci5pbmRleH06JHtsb3dlci54fToke2xvd2VyLnl9OiR7dXBwZXIueH06JHt1cHBlci55fWAsIGxvd2VyLCB1cHBlciwgYW5jaG9yZWQ6IGZhbHNlIH1dXG59XG5cbmNvbnN0IGltcHJpbnRSdWluc1dhcmRSb3V0ZSA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnKTogdm9pZCA9PiB7XG4gIGlmIChmbG9vci5iaW9tZSAhPT0gJ3J1aW5zJykgcmV0dXJuXG4gIGNvbnN0IGVkZ2UgPSBtYWNyby5lZGdlcy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUubW9kZXMuaW5jbHVkZXMoJ2Nvc3RseScpICYmIGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnb3B0aW9uYWwnKSlcbiAgaWYgKCFlZGdlKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgUnVpbnMgd2FyZCByb3V0ZTogJHttYWNyby5yZWNpcGVJZH1gKVxuICBjb25zdCBjZWxscyA9IGVkZ2UuY2VsbHMuc2xpY2UoMywgLTMpLmZpbHRlcihwb2ludCA9PiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KT8ua2luZCA9PT0gJ2Zsb29yJylcbiAgY29uc3Qgc3RhcnQgPSBNYXRoLm1heCgwLCBNYXRoLmZsb29yKGNlbGxzLmxlbmd0aCAvIDIpIC0gMylcbiAgY29uc3Qgcm91dGUgPSBjZWxscy5zbGljZShzdGFydCwgc3RhcnQgKyA3KVxuICBpZiAocm91dGUubGVuZ3RoIDwgMykgdGhyb3cgbmV3IEVycm9yKGBzaG9ydCBSdWlucyB3YXJkIHJvdXRlOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCByb3V0ZS5sZW5ndGg7IGluZGV4KyspIGlmIChpbmRleCAlIDIgPT09IDApIHNldEtpbmQoZmxvb3IsIHJvdXRlW2luZGV4XS54LCByb3V0ZVtpbmRleF0ueSwgJ2RhcnQnKVxufVxuXG5jb25zdCBpbXByaW50RnVybmFjZUZpcmluZ1JvdXRlID0gKGZsb29yOiBGbG9vciwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcpOiB2b2lkID0+IHtcbiAgaWYgKGZsb29yLmJpb21lICE9PSAnZnVybmFjZScpIHJldHVyblxuICBjb25zdCBzYWZlID0gbWFjcm8uZWRnZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdzYWZlJykpXG4gIGNvbnN0IGNvc3RseSA9IG1hY3JvLmVkZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnY29zdGx5JykgJiYgY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdvcHRpb25hbCcpKVxuICBpZiAoIXNhZmUgfHwgIWNvc3RseSkgdGhyb3cgbmV3IEVycm9yKGBtaXNzaW5nIEZ1cm5hY2Ugcm91dGUgcGFpcjogJHttYWNyby5yZWNpcGVJZH1gKVxuICBjb25zdCB0cmFuc2l0ID0gKGNlbGxzOiByZWFkb25seSBQb2ludFtdKSA9PiBjZWxscy5zbGljZSgzLCAtMykuZmlsdGVyKHBvaW50ID0+IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kID09PSAnZmxvb3InKVxuICBjb25zdCBsaWZ0TGFuZSA9IHRyYW5zaXQoc2FmZS5jZWxscylcbiAgY29uc3QgZmlyaW5nTGFuZSA9IHRyYW5zaXQoY29zdGx5LmNlbGxzKVxuICBpZiAobGlmdExhbmUubGVuZ3RoIDwgMyB8fCBmaXJpbmdMYW5lLmxlbmd0aCA8IDQpIHRocm93IG5ldyBFcnJvcihgc2hvcnQgRnVybmFjZSB0ZXJyYWNlIHJvdXRlOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCBsaWZ0TGFuZS5sZW5ndGg7IGluZGV4KyspIHtcbiAgICBjb25zdCBwb2ludCA9IGxpZnRMYW5lW2luZGV4XVxuICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KSFcbiAgICB0aWxlLmVsZXZhdGlvbiA9IDFcbiAgICBpZiAoaW5kZXggJSAzID09PSAxKSB0aWxlLmtpbmQgPSAnbGlmdCdcbiAgfVxuICBmb3IgKGxldCBpbmRleCA9IDA7IGluZGV4IDwgZmlyaW5nTGFuZS5sZW5ndGg7IGluZGV4KyspIHtcbiAgICBjb25zdCBwb2ludCA9IGZpcmluZ0xhbmVbaW5kZXhdXG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpIVxuICAgIHRpbGUuZWxldmF0aW9uID0gMFxuICAgIGlmIChpbmRleCAlIDMgPT09IDApIHRpbGUua2luZCA9ICdzbW9rZSdcbiAgICBlbHNlIGlmIChpbmRleCAlIDMgPT09IDEpIHRpbGUua2luZCA9ICdmaXJlVmVudCdcbiAgfVxuICBjb25zdCBsb3dlciA9IGZpcmluZ0xhbmUuZmluZChwb2ludCA9PiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KT8ua2luZCA9PT0gJ3Ntb2tlJykhXG4gIGNvbnN0IHVwcGVyID0gbGlmdExhbmUuZmluZChwb2ludCA9PiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KT8ua2luZCA9PT0gJ2xpZnQnKSFcbiAgZmxvb3IuY2xpbWJMaW5rcyA9IFt7IGlkOiBgbGlmdDoke2Zsb29yLmluZGV4fToke2xvd2VyLnh9OiR7bG93ZXIueX06JHt1cHBlci54fToke3VwcGVyLnl9YCwgbG93ZXIsIHVwcGVyLCBhbmNob3JlZDogdHJ1ZSB9XVxuICBjb25zdCBoZWF0TmV0d29yayA9IFsuLi5maXJpbmdMYW5lXVxuICBmb3IgKGxldCBpbmRleCA9IDA7IGluZGV4IDwgKGZsb29yLmluZGV4ICUgNCkgKiA0OyBpbmRleCsrKSB7XG4gICAgY29uc3QgYW5jaG9yID0gZmlyaW5nTGFuZVtpbmRleCAlIGZpcmluZ0xhbmUubGVuZ3RoXSFcbiAgICBjb25zdCBwb2ludCA9IGNhcmRpbmFsT2Zmc2V0cy5tYXAoKFt4LCB5XSkgPT4gKHsgeDogYW5jaG9yLnggKyB4LCB5OiBhbmNob3IueSArIHkgfSkpLmZpbmQoY2FuZGlkYXRlID0+IGdldFRpbGUoZmxvb3IsIGNhbmRpZGF0ZS54LCBjYW5kaWRhdGUueSk/LmtpbmQgPT09ICdmbG9vcicgJiYgIWhlYXROZXR3b3JrLnNvbWUoZXhpc3RpbmcgPT4gZXhpc3RpbmcueCA9PT0gY2FuZGlkYXRlLnggJiYgZXhpc3RpbmcueSA9PT0gY2FuZGlkYXRlLnkpKVxuICAgIGlmICghcG9pbnQpIGNvbnRpbnVlXG4gICAgZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhLmtpbmQgPSBpbmRleCAlIDIgPyAnc21va2UnIDogJ2ZpcmVWZW50J1xuICAgIGhlYXROZXR3b3JrLnB1c2gocG9pbnQpXG4gIH1cbiAgY29uc3Qga2lsbiA9IG1hY3JvLm5vZGVzLmZpbmQobm9kZSA9PiBub2RlLmtpbmQgPT09ICdvYmplY3RpdmUnKVxuICBpZiAoIWtpbG4pIHRocm93IG5ldyBFcnJvcihgbWlzc2luZyBGdXJuYWNlIGtpbG4gY29yZTogJHttYWNyby5yZWNpcGVJZH1gKVxuICBmbG9vci5mdXJuYWNlTGF5b3V0ID0geyBraWxuOiB7IHg6IGtpbG4uZm9vdHByaW50LnggKyBNYXRoLmZsb29yKGtpbG4uZm9vdHByaW50LndpZHRoIC8gMiksIHk6IGtpbG4uZm9vdHByaW50LnkgKyBNYXRoLmZsb29yKGtpbG4uZm9vdHByaW50LmhlaWdodCAvIDIpIH0sIGhlYXROZXR3b3JrLCBsaWZ0TGFuZTogWy4uLmxpZnRMYW5lXSB9IHNhdGlzZmllcyBGdXJuYWNlTGF5b3V0XG59XG5cbmNvbnN0IGltcHJpbnRGbG9vZGVkQ3VycmVudE5ldHdvcmsgPSAoZmxvb3I6IEZsb29yLCBtYWNybzogTWFjcm9SZWNpcGVEZWJ1Zyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdmbG9vZGVkUnVpbnMnKSByZXR1cm5cbiAgY29uc3Qgc2FmZSA9IG1hY3JvLmVkZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnc2FmZScpKVxuICBjb25zdCBjb3N0bHkgPSBtYWNyby5lZGdlcy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUubW9kZXMuaW5jbHVkZXMoJ2Nvc3RseScpICYmIGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnb3B0aW9uYWwnKSlcbiAgY29uc3QgcmVmdWdlID0gbWFjcm8ubm9kZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmtpbmQgPT09ICdvcHRpb25hbFJld2FyZCcpXG4gIGlmICghc2FmZSB8fCAhY29zdGx5IHx8ICFyZWZ1Z2UpIHRocm93IG5ldyBFcnJvcihgbWlzc2luZyBGbG9vZGVkIHJvdXRlIG5ldHdvcms6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgY29uc3QgdHJhbnNpdCA9IChjZWxsczogcmVhZG9ubHkgUG9pbnRbXSkgPT4gY2VsbHMuZmlsdGVyKHBvaW50ID0+IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kID09PSAnZmxvb3InKVxuICBjb25zdCByZWZ1Z2VSb3V0ZSA9IHRyYW5zaXQoc2FmZS5jZWxscylcbiAgY29uc3QgY3VycmVudFJvdXRlID0gdHJhbnNpdChjb3N0bHkuY2VsbHMpXG4gIGlmIChyZWZ1Z2VSb3V0ZS5sZW5ndGggPCAzIHx8IGN1cnJlbnRSb3V0ZS5sZW5ndGggPCA0KSB0aHJvdyBuZXcgRXJyb3IoYHNob3J0IEZsb29kZWQgY3VycmVudCBuZXR3b3JrOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCByZWZ1Z2VSb3V0ZS5sZW5ndGg7IGluZGV4KyspIHNldEtpbmQoZmxvb3IsIHJlZnVnZVJvdXRlW2luZGV4XS54LCByZWZ1Z2VSb3V0ZVtpbmRleF0ueSwgaW5kZXggJSA0ID09PSAxID8gJ2FuY2hvcicgOiAnZmxvb3InKVxuICBmb3IgKGxldCBpbmRleCA9IDA7IGluZGV4IDwgY3VycmVudFJvdXRlLmxlbmd0aCAtIDE7IGluZGV4KyspIHtcbiAgICBjb25zdCBwb2ludCA9IGN1cnJlbnRSb3V0ZVtpbmRleF1cbiAgICBjb25zdCBuZXh0ID0gY3VycmVudFJvdXRlW2luZGV4ICsgMV1cbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhXG4gICAgdGlsZS5raW5kID0gJ2N1cnJlbnQnXG4gICAgdGlsZS5mbG93ID0geyBkaXJlY3Rpb246IGZsb3dEaXJlY3Rpb24obmV4dC54IC0gcG9pbnQueCwgbmV4dC55IC0gcG9pbnQueSksIC4uLihpbmRleCA+PSBjdXJyZW50Um91dGUubGVuZ3RoIC0gMyA/IHsgaGF6YXJkOiAndW5kZXJ0b3cnIGFzIGNvbnN0IH0gOiB7fSkgfVxuICB9XG4gIGNvbnN0IG91dGxldCA9IGN1cnJlbnRSb3V0ZVtjdXJyZW50Um91dGUubGVuZ3RoIC0gMV1cbiAgY29uc3QgYmFuayA9IGNhcmRpbmFsT2Zmc2V0cy5tYXAoKFt4LCB5XSkgPT4gKHsgeDogb3V0bGV0LnggKyB4LCB5OiBvdXRsZXQueSArIHkgfSkpLmZpbmQocG9pbnQgPT4gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpXG4gIGNvbnN0IG5leHQgPSBiYW5rID8/IGN1cnJlbnRSb3V0ZVtjdXJyZW50Um91dGUubGVuZ3RoIC0gMl1cbiAgY29uc3Qgb3V0bGV0VGlsZSA9IGdldFRpbGUoZmxvb3IsIG91dGxldC54LCBvdXRsZXQueSkhXG4gIG91dGxldFRpbGUua2luZCA9ICdjdXJyZW50J1xuICBvdXRsZXRUaWxlLmZsb3cgPSB7IGRpcmVjdGlvbjogZmxvd0RpcmVjdGlvbihuZXh0LnggLSBvdXRsZXQueCwgbmV4dC55IC0gb3V0bGV0LnkpIH1cbiAgaWYgKGJhbmspIHNldEtpbmQoZmxvb3IsIGJhbmsueCwgYmFuay55LCAnYW5jaG9yJylcbiAgY29uc3QgaXNsYW5kID0geyB4OiByZWZ1Z2UuZm9vdHByaW50LnggKyBNYXRoLmZsb29yKHJlZnVnZS5mb290cHJpbnQud2lkdGggLyAyKSwgeTogcmVmdWdlLmZvb3RwcmludC55ICsgTWF0aC5mbG9vcihyZWZ1Z2UuZm9vdHByaW50LmhlaWdodCAvIDIpIH1cbiAgaWYgKGdldFRpbGUoZmxvb3IsIGlzbGFuZC54LCBpc2xhbmQueSk/LmtpbmQgPT09ICdmbG9vcicpIHNldEtpbmQoZmxvb3IsIGlzbGFuZC54LCBpc2xhbmQueSwgJ2FuY2hvcicpXG59XG5cbmNvbnN0IGltcHJpbnRGbG9vZGVkV2hpcmxwb29scyA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBybmc6IFJuZyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdmbG9vZGVkUnVpbnMnKSByZXR1cm5cbiAgY29uc3QgZGVzaXJlZCA9IDEgKyBmbG9vci5pbmRleCAlIDRcbiAgY29uc3QgcmFkaXVzID0gZmxvb3IuaW5kZXggJSA0ID4gMSA/IDIgOiAxXG4gIGNvbnN0IG1hY3JvQ2VsbHMgPSBuZXcgU2V0KG1hY3JvQ29ubmVjdG9yUG9pbnRzKG1hY3JvKS5tYXAocG9pbnQgPT4gaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKVxuICBjb25zdCBjZW50ZXJzID0gcm5nLnNodWZmbGUoZmxvb3IudGlsZXMuZmxhdE1hcCgodGlsZSwgaW5kZXgpID0+IHRpbGUua2luZCA9PT0gJ2Zsb29yJyA/IFtwb2ludEF0KGZsb29yLCBpbmRleCldIDogW10pKVxuICBjb25zdCB3aGlybHBvb2xzOiBXaGlybHBvb2xbXSA9IFtdXG4gIGZvciAoY29uc3QgY2VudGVyIG9mIGNlbnRlcnMpIHtcbiAgICBpZiAod2hpcmxwb29scy5sZW5ndGggPT09IGRlc2lyZWQpIGJyZWFrXG4gICAgY29uc3QgY2VsbHMgPSBbXSBhcyBQb2ludFtdXG4gICAgZm9yIChsZXQgeSA9IGNlbnRlci55IC0gcmFkaXVzOyB5IDw9IGNlbnRlci55ICsgcmFkaXVzOyB5KyspIGZvciAobGV0IHggPSBjZW50ZXIueCAtIHJhZGl1czsgeCA8PSBjZW50ZXIueCArIHJhZGl1czsgeCsrKSBpZiAoeCAhPT0gY2VudGVyLnggfHwgeSAhPT0gY2VudGVyLnkpIGNlbGxzLnB1c2goeyB4LCB5IH0pXG4gICAgY29uc3QgYW5jaG9yID0geyB4OiBjZW50ZXIueCArIHJhZGl1cyArIDEsIHk6IGNlbnRlci55IH1cbiAgICBpZiAoZ2V0VGlsZShmbG9vciwgY2VudGVyLngsIGNlbnRlci55KT8ua2luZCAhPT0gJ2Zsb29yJyB8fCBnZXRUaWxlKGZsb29yLCBhbmNob3IueCwgYW5jaG9yLnkpPy5raW5kICE9PSAnZmxvb3InIHx8IFtjZW50ZXIsIGFuY2hvciwgLi4uY2VsbHNdLnNvbWUocG9pbnQgPT4gIWdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpIHx8IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kICE9PSAnZmxvb3InIHx8IG1hY3JvQ2VsbHMuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSB8fCB3aGlybHBvb2xzLnNvbWUod2hpcmxwb29sID0+IHdoaXJscG9vbC5jZWxscy5zb21lKGNlbGwgPT4gY2VsbC54ID09PSBwb2ludC54ICYmIGNlbGwueSA9PT0gcG9pbnQueSkgfHwgd2hpcmxwb29sLmNlbnRlci54ID09PSBwb2ludC54ICYmIHdoaXJscG9vbC5jZW50ZXIueSA9PT0gcG9pbnQueSkpKSBjb250aW51ZVxuICAgIHNldEtpbmQoZmxvb3IsIGNlbnRlci54LCBjZW50ZXIueSwgJ2RlZXBXYXRlcicpXG4gICAgZm9yIChjb25zdCBwb2ludCBvZiBjZWxscykge1xuICAgICAgY29uc3QgdGlsZSA9IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpIVxuICAgICAgdGlsZS5raW5kID0gJ2N1cnJlbnQnXG4gICAgICB0aWxlLmZsb3cgPSB7IGRpcmVjdGlvbjogZmxvd0RpcmVjdGlvbihjZW50ZXIueCAtIHBvaW50LngsIGNlbnRlci55IC0gcG9pbnQueSksIGhhemFyZDogJ3VuZGVydG93JyB9XG4gICAgfVxuICAgIHNldEtpbmQoZmxvb3IsIGFuY2hvci54LCBhbmNob3IueSwgJ2FuY2hvcicpXG4gICAgd2hpcmxwb29scy5wdXNoKHsgaWQ6IGB3aGlybHBvb2w6JHtmbG9vci5zZWVkfToke3doaXJscG9vbHMubGVuZ3RofWAsIGNlbnRlciwgcmFkaXVzLCBjZWxscywgYW5jaG9yIH0pXG4gIH1cbiAgaWYgKHdoaXJscG9vbHMubGVuZ3RoICE9PSBkZXNpcmVkKSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCBGbG9vZGVkIHdoaXJscG9vbCBnZW5lcmF0aW9uOiBleHBlY3RlZCAke2Rlc2lyZWR9LCBmb3VuZCAke3doaXJscG9vbHMubGVuZ3RofWApXG4gIGZsb29yLndoaXJscG9vbHMgPSB3aGlybHBvb2xzXG59XG5cbmNvbnN0IGltcHJpbnRDbGlmZkhlaWdodEdyYXBoID0gKGZsb29yOiBGbG9vciwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcpOiB2b2lkID0+IHtcbiAgaWYgKGZsb29yLmJpb21lICE9PSAnY2xpZmZzJykgcmV0dXJuXG4gIGNvbnN0IGFyZWFGbG9vciA9IGZsb29yLmluZGV4ICUgNFxuICBjb25zdCBzYWZlID0gbWFjcm8uZWRnZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdzYWZlJykpXG4gIGNvbnN0IGNvc3RseSA9IG1hY3JvLmVkZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnY29zdGx5JykgJiYgY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdvcHRpb25hbCcpKVxuICBpZiAoIXNhZmUgfHwgIWNvc3RseSkgdGhyb3cgbmV3IEVycm9yKGBtaXNzaW5nIENsaWZmcyBoZWlnaHQgcm91dGVzOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGNvbnN0IHRyYW5zaXQgPSAoY2VsbHM6IHJlYWRvbmx5IFBvaW50W10pID0+IGNlbGxzLnNsaWNlKDMsIC0zKS5maWx0ZXIocG9pbnQgPT4gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpXG4gIGNvbnN0IGhpZ2hSb3V0ZSA9IHRyYW5zaXQoc2FmZS5jZWxscylcbiAgY29uc3QgbG93Um91dGUgPSB0cmFuc2l0KGNvc3RseS5jZWxscylcbiAgaWYgKGhpZ2hSb3V0ZS5sZW5ndGggPCA0IHx8IGxvd1JvdXRlLmxlbmd0aCA8IDQpIHRocm93IG5ldyBFcnJvcihgc2hvcnQgQ2xpZmZzIGhlaWdodCByb3V0ZXM6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgY29uc3Qgb2NjdXBpZWQgPSBuZXcgU2V0KFsuLi5oaWdoUm91dGUsIC4uLmxvd1JvdXRlXS5tYXAocG9pbnQgPT4gaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKVxuICBjb25zdCBtaWRMZWRnZXMgPSBtYWNyby5lZGdlcy5mbGF0TWFwKGVkZ2UgPT4gdHJhbnNpdChlZGdlLmNlbGxzKSkuZmlsdGVyKHBvaW50ID0+ICFvY2N1cGllZC5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKVxuICBpZiAobWlkTGVkZ2VzLmxlbmd0aCA8IDQpIHRocm93IG5ldyBFcnJvcihgc2hvcnQgQ2xpZmZzIG1pZCBsZWRnZXM6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgZm9yIChsZXQgaW5kZXggPSAwOyBpbmRleCA8IGhpZ2hSb3V0ZS5sZW5ndGg7IGluZGV4KyspIHtcbiAgICBjb25zdCBwb2ludCA9IGhpZ2hSb3V0ZVtpbmRleF1cbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhXG4gICAgdGlsZS5lbGV2YXRpb24gPSAyXG4gICAgdGlsZS5raW5kID0gaW5kZXggJSAzID09PSAxID8gJ3JvcGUnIDogJ2Zsb29yJ1xuICB9XG4gIGZvciAoY29uc3QgcG9pbnQgb2YgbG93Um91dGUpIHtcbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhXG4gICAgdGlsZS5lbGV2YXRpb24gPSAwXG4gICAgdGlsZS5raW5kID0gJ2xlZGdlJ1xuICB9XG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCBtaWRMZWRnZXMubGVuZ3RoOyBpbmRleCsrKSB7XG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoZmxvb3IsIG1pZExlZGdlc1tpbmRleF0hLngsIG1pZExlZGdlc1tpbmRleF0hLnkpIVxuICAgIHRpbGUuZWxldmF0aW9uID0gMVxuICAgIGlmIChpbmRleCAlIDQgPT09IDApIHRpbGUua2luZCA9ICdyb3BlJ1xuICB9XG4gIGNvbnN0IG92ZXJsb29rID0gbWFjcm8ubm9kZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmtpbmQgPT09ICdvcHRpb25hbFJld2FyZCcpXG4gIGNvbnN0IHBlcmNoID0gb3Zlcmxvb2sgJiYgQXJyYXkuZnJvbSh7IGxlbmd0aDogb3Zlcmxvb2suZm9vdHByaW50LndpZHRoICogb3Zlcmxvb2suZm9vdHByaW50LmhlaWdodCB9LCAoXywgaW5kZXgpID0+ICh7IHg6IG92ZXJsb29rLmZvb3RwcmludC54ICsgaW5kZXggJSBvdmVybG9vay5mb290cHJpbnQud2lkdGgsIHk6IG92ZXJsb29rLmZvb3RwcmludC55ICsgTWF0aC5mbG9vcihpbmRleCAvIG92ZXJsb29rLmZvb3RwcmludC53aWR0aCkgfSkpLmZpbmQocG9pbnQgPT4gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpXG4gIGlmICghcGVyY2gpIHRocm93IG5ldyBFcnJvcihgbWlzc2luZyBDbGlmZnMgZXhwb3NlZCBvdmVybG9vazogJHttYWNyby5yZWNpcGVJZH1gKVxuICBjb25zdCBwZXJjaFRpbGUgPSBnZXRUaWxlKGZsb29yLCBwZXJjaC54LCBwZXJjaC55KSFcbiAgcGVyY2hUaWxlLmVsZXZhdGlvbiA9IDJcbiAgcGVyY2hUaWxlLmtpbmQgPSAnbGVkZ2UnXG4gIGNvbnN0IG5lYXJlc3QgPSAobG93ZXI6IHJlYWRvbmx5IFBvaW50W10sIHVwcGVyOiByZWFkb25seSBQb2ludFtdLCB1c2VkOiBTZXQ8bnVtYmVyPik6IHsgbG93ZXI6IFBvaW50OyB1cHBlcjogUG9pbnQgfSB8IHVuZGVmaW5lZCA9PiBsb3dlci5mbGF0TWFwKGZyb20gPT4gdXBwZXIubWFwKHRvID0+ICh7IGxvd2VyOiBmcm9tLCB1cHBlcjogdG8gfSkpKS5maWx0ZXIobGluayA9PiAhdXNlZC5oYXMoaW5kZXhPZihmbG9vciwgbGluay5sb3dlci54LCBsaW5rLmxvd2VyLnkpKSAmJiAhdXNlZC5oYXMoaW5kZXhPZihmbG9vciwgbGluay51cHBlci54LCBsaW5rLnVwcGVyLnkpKSkuc29ydCgobGVmdCwgcmlnaHQpID0+IGRpc3RhbmNlKGxlZnQubG93ZXIsIGxlZnQudXBwZXIpIC0gZGlzdGFuY2UocmlnaHQubG93ZXIsIHJpZ2h0LnVwcGVyKSlbMF1cbiAgY29uc3QgcG9vbHMgPSBbW2xvd1JvdXRlLCBtaWRMZWRnZXNdLCBbbWlkTGVkZ2VzLCBoaWdoUm91dGVdLCBbbG93Um91dGUsIGhpZ2hSb3V0ZV1dIGFzIGNvbnN0XG4gIGNvbnN0IGxpbmtzID0gW10gYXMgQXJyYXk8eyBsb3dlcjogUG9pbnQ7IHVwcGVyOiBQb2ludCB9PlxuICBjb25zdCB1c2VkID0gbmV3IFNldDxudW1iZXI+KClcbiAgZm9yIChsZXQgaW5kZXggPSAwOyBpbmRleCA8IDIgKyBhcmVhRmxvb3I7IGluZGV4KyspIHtcbiAgICBjb25zdCBwYWlyID0gcG9vbHNbaW5kZXggJSBwb29scy5sZW5ndGhdXG4gICAgY29uc3QgbGluayA9IG5lYXJlc3QocGFpclswXSwgcGFpclsxXSwgdXNlZClcbiAgICBpZiAoIWxpbmspIHRocm93IG5ldyBFcnJvcihgc2hvcnQgQ2xpZmZzIGNsaW1iIG5ldHdvcms6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgICBsaW5rcy5wdXNoKGxpbmspXG4gICAgdXNlZC5hZGQoaW5kZXhPZihmbG9vciwgbGluay5sb3dlci54LCBsaW5rLmxvd2VyLnkpKVxuICAgIHVzZWQuYWRkKGluZGV4T2YoZmxvb3IsIGxpbmsudXBwZXIueCwgbGluay51cHBlci55KSlcbiAgfVxuICBmbG9vci5jbGltYkxpbmtzID0gbGlua3MubWFwKCh7IGxvd2VyLCB1cHBlciB9LCBpbmRleCkgPT4gKHsgaWQ6IGBjbGlmZjoke2Zsb29yLmluZGV4fToke2luZGV4fToke2xvd2VyLnh9OiR7bG93ZXIueX06JHt1cHBlci54fToke3VwcGVyLnl9YCwgbG93ZXIsIHVwcGVyLCBhbmNob3JlZDogZmFsc2UgfSkpXG4gIGNvbnN0IHdpbmRDb3JyaWRvcnM6IFBvaW50W11bXSA9IFtdXG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCBhcmVhRmxvb3I7IGluZGV4KyspIHtcbiAgICBjb25zdCByb3V0ZSA9IGxvd1JvdXRlXG4gICAgY29uc3Qgc3RhcnQgPSBNYXRoLm1pbihyb3V0ZS5sZW5ndGggLSAzLCAxICsgaW5kZXggKiAzKVxuICAgIGNvbnN0IGNvcnJpZG9yID0gcm91dGUuc2xpY2Uoc3RhcnQsIHN0YXJ0ICsgMylcbiAgICBpZiAoY29ycmlkb3IubGVuZ3RoIDwgMikgY29udGludWVcbiAgICBmb3IgKGxldCBzdGVwID0gMDsgc3RlcCA8IGNvcnJpZG9yLmxlbmd0aCAtIDE7IHN0ZXArKykge1xuICAgICAgY29uc3QgcG9pbnQgPSBjb3JyaWRvcltzdGVwXSFcbiAgICAgIGNvbnN0IG5leHQgPSBjb3JyaWRvcltzdGVwICsgMV0hXG4gICAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhXG4gICAgICB0aWxlLmtpbmQgPSAnbGVkZ2UnXG4gICAgICB0aWxlLmZsb3cgPSB7IGRpcmVjdGlvbjogZmxvd0RpcmVjdGlvbihuZXh0LnggLSBwb2ludC54LCBuZXh0LnkgLSBwb2ludC55KSwgaGF6YXJkOiAnc3F1YWxsJyB9XG4gICAgfVxuICAgIGNvbnN0IHRpZU9mZiA9IGdldFRpbGUoZmxvb3IsIGNvcnJpZG9yLmF0KC0xKSEueCwgY29ycmlkb3IuYXQoLTEpIS55KSFcbiAgICB0aWVPZmYua2luZCA9ICdyb3BlJ1xuICAgIGRlbGV0ZSB0aWVPZmYuZmxvd1xuICAgIHdpbmRDb3JyaWRvcnMucHVzaChjb3JyaWRvcilcbiAgfVxuICBjb25zdCBzaGVsdGVyZWRQb2NrZXRzID0gZmxvb3IudGlsZXMuZmxhdE1hcCgodGlsZSwgaW5kZXgpID0+IHRpbGUua2luZCA9PT0gJ2Zsb29yJyAmJiBjYXJkaW5hbE9mZnNldHMuc29tZSgoW3gsIHldKSA9PiBnZXRUaWxlKGZsb29yLCBpbmRleCAlIGZsb29yLndpZHRoICsgeCwgTWF0aC5mbG9vcihpbmRleCAvIGZsb29yLndpZHRoKSArIHkpPy5raW5kID09PSAnY2xpZmZXYWxsJykgPyBbcG9pbnRBdChmbG9vciwgaW5kZXgpXSA6IFtdKS5zbGljZSgwLCAxICsgYXJlYUZsb29yKVxuICBmbG9vci5jbGlmZkxheW91dCA9IHsgbG93Um91dGU6IFsuLi5sb3dSb3V0ZV0sIG1pZExlZGdlcywgaGlnaFJpZGdlOiBbLi4uaGlnaFJvdXRlXSwgd2luZENvcnJpZG9ycywgc2hlbHRlcmVkUG9ja2V0cyB9IHNhdGlzZmllcyBDbGlmZkxheW91dFxufVxuXG5jb25zdCBpbXByaW50Q2xpZmZBbGNvdmVzID0gKGZsb29yOiBGbG9vciwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcsIHJuZzogUm5nKTogdm9pZCA9PiB7XG4gIGlmIChmbG9vci5iaW9tZSAhPT0gJ2NsaWZmcycpIHJldHVyblxuICBjb25zdCBkZXNpcmVkID0gMSArIGZsb29yLmluZGV4ICUgNFxuICBjb25zdCBtYWNyb0NlbGxzID0gbmV3IFNldChtYWNyb0Nvbm5lY3RvclBvaW50cyhtYWNybykubWFwKHBvaW50ID0+IGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSlcbiAgY29uc3QgcmVzZXJ2ZWQgPSBuZXcgU2V0PG51bWJlcj4oKVxuICBjb25zdCBhbGNvdmVzOiBDbGlmZkFsY292ZVtdID0gW11cbiAgY29uc3QgcmVhY2hhYmxlID0gWy4uLnJlYWNoYWJsZUZsb29ySW5kZXhlcyhmbG9vcildLm1hcChpbmRleCA9PiBwb2ludEF0KGZsb29yLCBpbmRleCkpXG4gIGZvciAoY29uc3QgY2FuZGlkYXRlIG9mIHJuZy5zaHVmZmxlKHJlYWNoYWJsZS5mbGF0TWFwKGFwcHJvYWNoID0+IG1pbmVCcmVhY2hEaXJlY3Rpb25zLm1hcChkaXJlY3Rpb24gPT4gKHsgYXBwcm9hY2gsIGRpcmVjdGlvbiB9KSkpKSkge1xuICAgIGlmIChhbGNvdmVzLmxlbmd0aCA9PT0gZGVzaXJlZCkgYnJlYWtcbiAgICBjb25zdCBlbnRyeSA9IHsgeDogY2FuZGlkYXRlLmFwcHJvYWNoLnggKyBjYW5kaWRhdGUuZGlyZWN0aW9uLngsIHk6IGNhbmRpZGF0ZS5hcHByb2FjaC55ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi55IH1cbiAgICBjb25zdCBjaGFtYmVyID0gW10gYXMgUG9pbnRbXVxuICAgIGZvciAobGV0IGZvcndhcmQgPSAyOyBmb3J3YXJkIDwgNDsgZm9yd2FyZCsrKSBmb3IgKGxldCBsYXRlcmFsID0gLTE7IGxhdGVyYWwgPD0gMTsgbGF0ZXJhbCsrKSBjaGFtYmVyLnB1c2goeyB4OiBjYW5kaWRhdGUuYXBwcm9hY2gueCArIGNhbmRpZGF0ZS5kaXJlY3Rpb24ueCAqIGZvcndhcmQgKyBjYW5kaWRhdGUuZGlyZWN0aW9uLmNyb3NzLnggKiBsYXRlcmFsLCB5OiBjYW5kaWRhdGUuYXBwcm9hY2gueSArIGNhbmRpZGF0ZS5kaXJlY3Rpb24ueSAqIGZvcndhcmQgKyBjYW5kaWRhdGUuZGlyZWN0aW9uLmNyb3NzLnkgKiBsYXRlcmFsIH0pXG4gICAgY29uc3QgY2hhbmdlZCA9IFtlbnRyeSwgLi4uY2hhbWJlcl1cbiAgICBpZiAoZ2V0VGlsZShmbG9vciwgZW50cnkueCwgZW50cnkueSk/LmtpbmQgIT09ICdjbGlmZldhbGwnIHx8IGNoYW5nZWQuc29tZShwb2ludCA9PiAhZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkgfHwgZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgIT09ICdjbGlmZldhbGwnIHx8IG1hY3JvQ2VsbHMuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSB8fCByZXNlcnZlZC5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKSkgY29udGludWVcbiAgICBjb25zdCBlbGV2YXRpb24gPSBhbGNvdmVzLmxlbmd0aCAlIDIgPyAxIDogMlxuICAgIGNvbnN0IHJld2FyZFBvaW50ID0gY2hhbWJlcltNYXRoLmZsb29yKGNoYW1iZXIubGVuZ3RoIC8gMildIVxuICAgIGNvbnN0IHJld2FyZCA9IHsgaWQ6IGFsY292ZXMubGVuZ3RoICUgMiA/ICdjbGlmZlNwb29sJyA6ICdza3lNYXAnLCB4OiByZXdhcmRQb2ludC54LCB5OiByZXdhcmRQb2ludC55LCBjb3VudDogMSwgdmlzaWJsZUluRm9nOiB0cnVlIH1cbiAgICBjb25zdCBlbnRyeVRpbGUgPSBnZXRUaWxlKGZsb29yLCBlbnRyeS54LCBlbnRyeS55KSFcbiAgICBlbnRyeVRpbGUua2luZCA9ICdyb3BlJ1xuICAgIGVudHJ5VGlsZS5lbGV2YXRpb24gPSBlbGV2YXRpb25cbiAgICBjaGFtYmVyLmZvckVhY2gocG9pbnQgPT4geyBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkhOyB0aWxlLmtpbmQgPSAnbGVkZ2UnOyB0aWxlLmVsZXZhdGlvbiA9IGVsZXZhdGlvbjsgcmVzZXJ2ZWQuYWRkKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSB9KVxuICAgIHJlc2VydmVkLmFkZChpbmRleE9mKGZsb29yLCBlbnRyeS54LCBlbnRyeS55KSlcbiAgICBmbG9vci5pdGVtcy5wdXNoKHJld2FyZClcbiAgICBhbGNvdmVzLnB1c2goeyBpZDogYGNsaWZmLWFsY292ZToke2Zsb29yLnNlZWR9OiR7YWxjb3Zlcy5sZW5ndGh9YCwga2luZDogJ2NsaWZmLWFsY292ZScsIGFwcHJvYWNoOiB7IC4uLmNhbmRpZGF0ZS5hcHByb2FjaCB9LCBlbnRyeSwgY2hhbWJlciwgcmV3YXJkLCBlbGV2YXRpb24gfSlcbiAgfVxuICBpZiAoYWxjb3Zlcy5sZW5ndGggIT09IGRlc2lyZWQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIENsaWZmcyBhbGNvdmUgZ2VuZXJhdGlvbjogZXhwZWN0ZWQgJHtkZXNpcmVkfSwgZm91bmQgJHthbGNvdmVzLmxlbmd0aH1gKVxuICBmbG9vci5zaWRlU3BhY2VzID0gYWxjb3Zlc1xufVxuXG5jb25zdCBpbXByaW50QnVyaWFsQ3J5cHRzID0gKGZsb29yOiBGbG9vciwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcsIHJuZzogUm5nKTogdm9pZCA9PiB7XG4gIGlmIChmbG9vci5iaW9tZSAhPT0gJ2J1cmlhbCcpIHJldHVyblxuICBjb25zdCBhcmVhRmxvb3IgPSBmbG9vci5pbmRleCAlIDRcbiAgY29uc3QgZGVzaXJlZCA9IDEgKyBhcmVhRmxvb3JcbiAgY29uc3QgbWFjcm9DZWxscyA9IG5ldyBTZXQobWFjcm9Db25uZWN0b3JQb2ludHMobWFjcm8pLm1hcChwb2ludCA9PiBpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkpXG4gIGNvbnN0IHJlc2VydmVkID0gbmV3IFNldDxudW1iZXI+KClcbiAgY29uc3QgY3J5cHRzOiBCdXJpYWxDcnlwdFtdID0gW11cbiAgY29uc3QgcmVhY2hhYmxlID0gWy4uLnJlYWNoYWJsZUZsb29ySW5kZXhlcyhmbG9vcildLm1hcChpbmRleCA9PiBwb2ludEF0KGZsb29yLCBpbmRleCkpXG4gIGZvciAoY29uc3QgY2FuZGlkYXRlIG9mIHJuZy5zaHVmZmxlKHJlYWNoYWJsZS5mbGF0TWFwKGFwcHJvYWNoID0+IG1pbmVCcmVhY2hEaXJlY3Rpb25zLm1hcChkaXJlY3Rpb24gPT4gKHsgYXBwcm9hY2gsIGRpcmVjdGlvbiB9KSkpKSkge1xuICAgIGlmIChjcnlwdHMubGVuZ3RoID09PSBkZXNpcmVkKSBicmVha1xuICAgIGNvbnN0IGRlcHRoID0gMyArIGFyZWFGbG9vclxuICAgIGNvbnN0IHdpZHRoID0gMyArIChjcnlwdHMubGVuZ3RoICsgYXJlYUZsb29yKSAlIDIgKiAyXG4gICAgY29uc3QgcmFkaXVzID0gTWF0aC5mbG9vcih3aWR0aCAvIDIpXG4gICAgY29uc3QgZW50cnkgPSB7IHg6IGNhbmRpZGF0ZS5hcHByb2FjaC54ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi54LCB5OiBjYW5kaWRhdGUuYXBwcm9hY2gueSArIGNhbmRpZGF0ZS5kaXJlY3Rpb24ueSB9XG4gICAgY29uc3QgY2hhbWJlciA9IFtdIGFzIFBvaW50W11cbiAgICBmb3IgKGxldCBmb3J3YXJkID0gMjsgZm9yd2FyZCA8IGRlcHRoICsgMjsgZm9yd2FyZCsrKSBmb3IgKGxldCBsYXRlcmFsID0gLXJhZGl1czsgbGF0ZXJhbCA8PSByYWRpdXM7IGxhdGVyYWwrKykgY2hhbWJlci5wdXNoKHsgeDogY2FuZGlkYXRlLmFwcHJvYWNoLnggKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnggKiBmb3J3YXJkICsgY2FuZGlkYXRlLmRpcmVjdGlvbi5jcm9zcy54ICogbGF0ZXJhbCwgeTogY2FuZGlkYXRlLmFwcHJvYWNoLnkgKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnkgKiBmb3J3YXJkICsgY2FuZGlkYXRlLmRpcmVjdGlvbi5jcm9zcy55ICogbGF0ZXJhbCB9KVxuICAgIGNvbnN0IHJlam9pbiA9IHsgeDogY2FuZGlkYXRlLmFwcHJvYWNoLnggKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnggKiAoZGVwdGggKyAyKSwgeTogY2FuZGlkYXRlLmFwcHJvYWNoLnkgKyBjYW5kaWRhdGUuZGlyZWN0aW9uLnkgKiAoZGVwdGggKyAyKSB9XG4gICAgY29uc3QgcmVqb2luQXBwcm9hY2ggPSB7IHg6IGNhbmRpZGF0ZS5hcHByb2FjaC54ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi54ICogKGRlcHRoICsgMyksIHk6IGNhbmRpZGF0ZS5hcHByb2FjaC55ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi55ICogKGRlcHRoICsgMykgfVxuICAgIGNvbnN0IGNoYW1iZXJJbmRleGVzID0gbmV3IFNldChjaGFtYmVyLm1hcChwb2ludCA9PiBpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkpXG4gICAgY29uc3QgYmFycmllciA9IGNoYW1iZXIuZmxhdE1hcChwb2ludCA9PiBwYXRoT2Zmc2V0cy5tYXAoKFt4LCB5XSkgPT4gKHsgeDogcG9pbnQueCArIHgsIHk6IHBvaW50LnkgKyB5IH0pKSkuZmlsdGVyKHBvaW50ID0+ICFjaGFtYmVySW5kZXhlcy5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpICYmIChwb2ludC54ICE9PSBlbnRyeS54IHx8IHBvaW50LnkgIT09IGVudHJ5LnkpICYmIChwb2ludC54ICE9PSByZWpvaW4ueCB8fCBwb2ludC55ICE9PSByZWpvaW4ueSkpLmZpbHRlcigocG9pbnQsIGluZGV4LCBwb2ludHMpID0+IHBvaW50cy5maW5kSW5kZXgob3RoZXIgPT4gb3RoZXIueCA9PT0gcG9pbnQueCAmJiBvdGhlci55ID09PSBwb2ludC55KSA9PT0gaW5kZXgpXG4gICAgY29uc3QgY2hhbmdlZCA9IFtlbnRyeSwgcmVqb2luLCAuLi5jaGFtYmVyLCAuLi5iYXJyaWVyXVxuICAgIGNvbnN0IHByb3RlY3RlZFBvaW50cyA9IFtjYW5kaWRhdGUuYXBwcm9hY2gsIHJlam9pbkFwcHJvYWNoLCAuLi5jaGFuZ2VkXVxuICAgIGlmIChwcm90ZWN0ZWRQb2ludHMuc29tZShwb2ludCA9PiAhZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkgfHwgZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgIT09ICdmbG9vcicgfHwgbWFjcm9DZWxscy5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpIHx8IHJlc2VydmVkLmhhcyhpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkpKSBjb250aW51ZVxuICAgIGNvbnN0IGJlZm9yZSA9IGNoYW5nZWQubWFwKHBvaW50ID0+ICh7IHBvaW50LCBraW5kOiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KSEua2luZCB9KSlcbiAgICBiYXJyaWVyLmZvckVhY2gocG9pbnQgPT4gc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ3dhbGwnKSlcbiAgICBzZXRLaW5kKGZsb29yLCBlbnRyeS54LCBlbnRyeS55LCAnYnJlYWt3YWxsJylcbiAgICBzZXRLaW5kKGZsb29yLCByZWpvaW4ueCwgcmVqb2luLnksICdicmVha3dhbGwnKVxuICAgIGlmICghaGFzUGFzc2FibGVQYXRoKGZsb29yLCBmbG9vci5zdGFydCwgZmxvb3IuZXhpdCkpIHsgYmVmb3JlLmZvckVhY2goKHsgcG9pbnQsIGtpbmQgfSkgPT4gc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwga2luZCkpOyBjb250aW51ZSB9XG4gICAgY2hhbWJlci5mb3JFYWNoKChwb2ludCwgaW5kZXgpID0+IHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksIGluZGV4ICUgNyA9PT0gMCA/ICdvc3N1YXJ5JyA6IGluZGV4ICUgNSA9PT0gMCA/ICdzcGlyaXRQYXRoJyA6IGluZGV4ICUgMyA9PT0gMCA/ICdncmF2ZVNvaWwnIDogJ2Zsb29yJykpXG4gICAgY29uc3QgcmV3YXJkUG9pbnQgPSBjaGFtYmVyW01hdGguZmxvb3IoY2hhbWJlci5sZW5ndGggLyAyKV0hXG4gICAgY29uc3QgcmV3YXJkID0geyBpZDogY3J5cHRzLmxlbmd0aCAlIDMgPT09IDAgPyAndG9tYktleScgOiBjcnlwdHMubGVuZ3RoICUgMyA9PT0gMSA/ICd3YXJkU2NyaXB0JyA6ICdyb3BlQnVuZGxlJywgeDogcmV3YXJkUG9pbnQueCwgeTogcmV3YXJkUG9pbnQueSwgY291bnQ6IDEsIHZpc2libGVJbkZvZzogdHJ1ZSB9XG4gICAgcHJvdGVjdGVkUG9pbnRzLmZvckVhY2gocG9pbnQgPT4gcmVzZXJ2ZWQuYWRkKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSlcbiAgICBmbG9vci5pdGVtcy5wdXNoKHJld2FyZClcbiAgICBjcnlwdHMucHVzaCh7IGlkOiBgYnVyaWFsLWNyeXB0OiR7Zmxvb3Iuc2VlZH06JHtjcnlwdHMubGVuZ3RofWAsIGtpbmQ6ICdidXJpYWwtY3J5cHQnLCBhcHByb2FjaDogeyAuLi5jYW5kaWRhdGUuYXBwcm9hY2ggfSwgZW50cnksIGFwcHJvYWNoZXM6IFt7IC4uLmNhbmRpZGF0ZS5hcHByb2FjaCB9LCByZWpvaW5BcHByb2FjaF0sIGVudHJpZXM6IFtlbnRyeSwgcmVqb2luXSwgY2hhbWJlciwgcmV3YXJkLCBkZXB0aCB9KVxuICB9XG4gIGlmIChjcnlwdHMubGVuZ3RoICE9PSBkZXNpcmVkKSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCBCdXJpYWwgY3J5cHQgZ2VuZXJhdGlvbjogZXhwZWN0ZWQgJHtkZXNpcmVkfSwgZm91bmQgJHtjcnlwdHMubGVuZ3RofWApXG4gIGNvbnN0IHNhZmUgPSBtYWNyby5lZGdlcy5maW5kKGVkZ2UgPT4gZWRnZS5tb2Rlcy5pbmNsdWRlcygnc2FmZScpKVxuICBjb25zdCBjb3N0bHkgPSBtYWNyby5lZGdlcy5maW5kKGVkZ2UgPT4gZWRnZS5tb2Rlcy5pbmNsdWRlcygnY29zdGx5JykgJiYgZWRnZS5tb2Rlcy5pbmNsdWRlcygnb3B0aW9uYWwnKSlcbiAgaWYgKCFzYWZlIHx8ICFjb3N0bHkpIHRocm93IG5ldyBFcnJvcihgbWlzc2luZyBCdXJpYWwgcHJvY2Vzc2lvbnM6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgY29uc3Qgc3Bpcml0Um91dGUgPSBzYWZlLmNlbGxzLmZpbHRlcihwb2ludCA9PiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KT8ua2luZCA9PT0gJ3NwaXJpdFBhdGgnKVxuICBjb25zdCBncmF2ZVJvdXRlID0gY29zdGx5LmNlbGxzLmZpbHRlcihwb2ludCA9PiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KT8ua2luZCA9PT0gJ2dyYXZlU29pbCcpXG4gIGlmIChzcGlyaXRSb3V0ZS5sZW5ndGggPCAzIHx8IGdyYXZlUm91dGUubGVuZ3RoIDwgMykgdGhyb3cgbmV3IEVycm9yKGBzaG9ydCBCdXJpYWwgcHJvY2Vzc2lvbnM6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgY29uc3QgcHJvY2Vzc2lvbnMgPSBBcnJheS5mcm9tKHsgbGVuZ3RoOiBkZXNpcmVkIH0sIChfLCBpbmRleCkgPT4gZ3JhdmVSb3V0ZS5zbGljZShNYXRoLm1pbihncmF2ZVJvdXRlLmxlbmd0aCAtIDMsIGluZGV4ICogMyksIE1hdGgubWluKGdyYXZlUm91dGUubGVuZ3RoLCBpbmRleCAqIDMgKyAzKSkpLmZpbHRlcihyb3V0ZSA9PiByb3V0ZS5sZW5ndGggPT09IDMpXG4gIGlmIChwcm9jZXNzaW9ucy5sZW5ndGggIT09IGRlc2lyZWQpIHRocm93IG5ldyBFcnJvcihgc2hvcnQgQnVyaWFsIHByb2Nlc3Npb24gbmV0d29yazogJHttYWNyby5yZWNpcGVJZH1gKVxuICBjb25zdCBzaGVsdGVycyA9IHNwaXJpdFJvdXRlLmZpbHRlcigoXywgaW5kZXgpID0+IGluZGV4ICUgTWF0aC5tYXgoMSwgTWF0aC5mbG9vcihzcGlyaXRSb3V0ZS5sZW5ndGggLyBkZXNpcmVkKSkgPT09IDApLnNsaWNlKDAsIGRlc2lyZWQpXG4gIGZsb29yLnNpZGVTcGFjZXMgPSBjcnlwdHNcbiAgZmxvb3IuYnVyaWFsTGF5b3V0ID0geyBtb3VuZHM6IGZsb29yLnRpbGVzLmZsYXRNYXAoKHRpbGUsIGluZGV4KSA9PiB0aWxlLmtpbmQgPT09ICdjYWlybicgPyBbcG9pbnRBdChmbG9vciwgaW5kZXgpXSA6IFtdKS5zbGljZSgwLCAyNCArIGFyZWFGbG9vciAqIDgpLCBwcm9jZXNzaW9ucywgc2hlbHRlcnMgfSBzYXRpc2ZpZXMgQnVyaWFsTGF5b3V0XG59XG5cbmNvbnN0IGltcHJpbnRTYWx0TWlyYWdlcyA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBybmc6IFJuZyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdzYWx0RmxhdHMnKSByZXR1cm5cbiAgY29uc3QgZGVzaXJlZCA9IDEgKyBmbG9vci5pbmRleCAlIDRcbiAgY29uc3QgbWFjcm9DZWxscyA9IG5ldyBTZXQobWFjcm9Db25uZWN0b3JQb2ludHMobWFjcm8pLm1hcChwb2ludCA9PiBpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkpXG4gIGNvbnN0IG1pcmFnZXM6IFNhbHRNaXJhZ2VbXSA9IFtdXG4gIGNvbnN0IGNhbmRpZGF0ZXMgPSBybmcuc2h1ZmZsZShmbG9vci50aWxlcy5mbGF0TWFwKCh0aWxlLCBpbmRleCkgPT4gdGlsZS5raW5kID09PSAnZmxvb3InID8gW3BvaW50QXQoZmxvb3IsIGluZGV4KV0gOiBbXSkpXG4gIGZvciAoY29uc3QgbWFya2VyIG9mIGNhbmRpZGF0ZXMpIHtcbiAgICBpZiAobWlyYWdlcy5sZW5ndGggPT09IGRlc2lyZWQpIGJyZWFrXG4gICAgY29uc3QgY2VsbHMgPSBbeyB4OiBtYXJrZXIueCwgeTogbWFya2VyLnkgfSwgeyB4OiBtYXJrZXIueCArIDEsIHk6IG1hcmtlci55IH0sIHsgeDogbWFya2VyLngsIHk6IG1hcmtlci55ICsgMSB9LCB7IHg6IG1hcmtlci54ICsgMSwgeTogbWFya2VyLnkgKyAxIH1dXG4gICAgaWYgKGRpc3RhbmNlKG1hcmtlciwgZmxvb3Iuc3RhcnQpIDwgOCB8fCBjZWxscy5zb21lKHBvaW50ID0+IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kICE9PSAnZmxvb3InIHx8IG1hY3JvQ2VsbHMuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSB8fCBtaXJhZ2VzLnNvbWUobWlyYWdlID0+IG1pcmFnZS5jZWxscy5zb21lKGNlbGwgPT4gY2VsbC54ID09PSBwb2ludC54ICYmIGNlbGwueSA9PT0gcG9pbnQueSkpKSkgY29udGludWVcbiAgICBjZWxscy5mb3JFYWNoKHBvaW50ID0+IHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksICdzYWx0TWlycm9yJykpXG4gICAgbWlyYWdlcy5wdXNoKHsgaWQ6IGBzYWx0LW1pcmFnZToke2Zsb29yLnNlZWR9OiR7bWlyYWdlcy5sZW5ndGh9YCwgbWFya2VyLCBjZWxscywgcmV2ZWFsZWQ6IGZhbHNlIH0pXG4gIH1cbiAgaWYgKG1pcmFnZXMubGVuZ3RoICE9PSBkZXNpcmVkKSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCBTYWx0IG1pcmFnZSBnZW5lcmF0aW9uOiBleHBlY3RlZCAke2Rlc2lyZWR9LCBmb3VuZCAke21pcmFnZXMubGVuZ3RofWApXG4gIGZsb29yLnNhbHRNaXJhZ2VzID0gbWlyYWdlc1xufVxuXG5jb25zdCBpbXByaW50RnJvc3RDYXZlcyA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnLCBybmc6IFJuZyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdmcm9zdFJlbGlxdWFyeScpIHJldHVyblxuICBjb25zdCBkZXNpcmVkID0gMSArIGZsb29yLmluZGV4ICUgNFxuICBjb25zdCBtYWNyb0NlbGxzID0gbmV3IFNldChtYWNyb0Nvbm5lY3RvclBvaW50cyhtYWNybykubWFwKHBvaW50ID0+IGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSlcbiAgY29uc3QgY2F2ZXM6IEZyb3N0Q2F2ZVtdID0gW11cbiAgY29uc3QgcmVzZXJ2ZWQgPSBuZXcgU2V0PG51bWJlcj4oKVxuICBjb25zdCByZWFjaGFibGUgPSBbLi4ucmVhY2hhYmxlRmxvb3JJbmRleGVzKGZsb29yKV0ubWFwKGluZGV4ID0+IHBvaW50QXQoZmxvb3IsIGluZGV4KSlcbiAgZm9yIChjb25zdCBjYW5kaWRhdGUgb2Ygcm5nLnNodWZmbGUocmVhY2hhYmxlLmZsYXRNYXAoYXBwcm9hY2ggPT4gbWluZUJyZWFjaERpcmVjdGlvbnMubWFwKGRpcmVjdGlvbiA9PiAoeyBhcHByb2FjaCwgZGlyZWN0aW9uIH0pKSkpKSB7XG4gICAgaWYgKGNhdmVzLmxlbmd0aCA9PT0gZGVzaXJlZCkgYnJlYWtcbiAgICBjb25zdCBkZXB0aCA9IDMgKyBmbG9vci5pbmRleCAlIDRcbiAgICBjb25zdCBjaGFtYmVyID0gW10gYXMgUG9pbnRbXVxuICAgIGZvciAobGV0IGZvcndhcmQgPSAyOyBmb3J3YXJkIDwgZGVwdGggKyAyOyBmb3J3YXJkKyspIGZvciAobGV0IGxhdGVyYWwgPSAtMTsgbGF0ZXJhbCA8PSAxOyBsYXRlcmFsKyspIGNoYW1iZXIucHVzaCh7IHg6IGNhbmRpZGF0ZS5hcHByb2FjaC54ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi54ICogZm9yd2FyZCArIGNhbmRpZGF0ZS5kaXJlY3Rpb24uY3Jvc3MueCAqIGxhdGVyYWwsIHk6IGNhbmRpZGF0ZS5hcHByb2FjaC55ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi55ICogZm9yd2FyZCArIGNhbmRpZGF0ZS5kaXJlY3Rpb24uY3Jvc3MueSAqIGxhdGVyYWwgfSlcbiAgICBjb25zdCBlbnRyeSA9IHsgeDogY2FuZGlkYXRlLmFwcHJvYWNoLnggKyBjYW5kaWRhdGUuZGlyZWN0aW9uLngsIHk6IGNhbmRpZGF0ZS5hcHByb2FjaC55ICsgY2FuZGlkYXRlLmRpcmVjdGlvbi55IH1cbiAgICBjb25zdCBjaGFtYmVySW5kZXhlcyA9IG5ldyBTZXQoY2hhbWJlci5tYXAocG9pbnQgPT4gaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKVxuICAgIGNvbnN0IGJhcnJpZXIgPSBjaGFtYmVyLmZsYXRNYXAocG9pbnQgPT4gcGF0aE9mZnNldHMubWFwKChbeCwgeV0pID0+ICh7IHg6IHBvaW50LnggKyB4LCB5OiBwb2ludC55ICsgeSB9KSkpLmZpbHRlcihwb2ludCA9PiAhY2hhbWJlckluZGV4ZXMuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSAmJiAocG9pbnQueCAhPT0gZW50cnkueCB8fCBwb2ludC55ICE9PSBlbnRyeS55KSkuZmlsdGVyKChwb2ludCwgaW5kZXgsIHBvaW50cykgPT4gcG9pbnRzLmZpbmRJbmRleChvdGhlciA9PiBvdGhlci54ID09PSBwb2ludC54ICYmIG90aGVyLnkgPT09IHBvaW50LnkpID09PSBpbmRleClcbiAgICBjb25zdCBjaGFuZ2VkID0gW2VudHJ5LCAuLi5jaGFtYmVyLCAuLi5iYXJyaWVyXVxuICAgIGlmIChjaGFuZ2VkLnNvbWUocG9pbnQgPT4gIWdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpIHx8IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kICE9PSAnZmxvb3InIHx8IG1hY3JvQ2VsbHMuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSB8fCByZXNlcnZlZC5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpIHx8IGZsb29yLnByb3BzLnNvbWUocHJvcCA9PiBwcm9wLnggPT09IHBvaW50LnggJiYgcHJvcC55ID09PSBwb2ludC55KSkpIGNvbnRpbnVlXG4gICAgY29uc3QgYmVmb3JlID0gY2hhbmdlZC5tYXAocG9pbnQgPT4gKHsgcG9pbnQsIGtpbmQ6IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpIS5raW5kIH0pKVxuICAgIGJhcnJpZXIuZm9yRWFjaChwb2ludCA9PiBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCAnd2FsbCcpKVxuICAgIHNldEtpbmQoZmxvb3IsIGVudHJ5LngsIGVudHJ5LnksICdicmVha3dhbGwnKVxuICAgIGlmICghaGFzUGFzc2FibGVQYXRoKGZsb29yLCBmbG9vci5zdGFydCwgZmxvb3IuZXhpdCkpIHsgYmVmb3JlLmZvckVhY2goKHsgcG9pbnQsIGtpbmQgfSkgPT4gc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwga2luZCkpOyBjb250aW51ZSB9XG4gICAgY2hhbWJlci5mb3JFYWNoKChwb2ludCwgaW5kZXgpID0+IHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksIGluZGV4ICUgNSA9PT0gMCA/ICdmcm9zdFJpbWUnIDogaW5kZXggJSAzID09PSAwID8gJ2ljZScgOiAnZmxvb3InKSlcbiAgICBjb25zdCByZXdhcmRQb2ludCA9IGNoYW1iZXJbTWF0aC5mbG9vcihjaGFtYmVyLmxlbmd0aCAvIDIpXSFcbiAgICBjb25zdCByZXdhcmQgPSB7IGlkOiBjYXZlcy5sZW5ndGggJSAyID8gJ2dyYXBwbGVMaW5lJyA6ICd3YXJkU2NyaXB0JywgeDogcmV3YXJkUG9pbnQueCwgeTogcmV3YXJkUG9pbnQueSwgY291bnQ6IDEsIHZpc2libGVJbkZvZzogdHJ1ZSB9XG4gICAgY2hhbmdlZC5mb3JFYWNoKHBvaW50ID0+IHJlc2VydmVkLmFkZChpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSkpXG4gICAgZmxvb3IuaXRlbXMucHVzaChyZXdhcmQpXG4gICAgY2F2ZXMucHVzaCh7IGlkOiBgZnJvc3QtY2F2ZToke2Zsb29yLnNlZWR9OiR7Y2F2ZXMubGVuZ3RofWAsIGtpbmQ6ICdmcm9zdC1jYXZlJywgYXBwcm9hY2g6IHsgLi4uY2FuZGlkYXRlLmFwcHJvYWNoIH0sIGVudHJ5LCBjaGFtYmVyLCByZXdhcmQsIGRlcHRoIH0pXG4gIH1cbiAgaWYgKGNhdmVzLmxlbmd0aCAhPT0gZGVzaXJlZCkgdGhyb3cgbmV3IEVycm9yKGBmYWlsZWQgRnJvc3QgY2F2ZSBnZW5lcmF0aW9uOiBleHBlY3RlZCAke2Rlc2lyZWR9LCBmb3VuZCAke2NhdmVzLmxlbmd0aH1gKVxuICBjb25zdCBjb3N0bHkgPSBtYWNyby5lZGdlcy5maW5kKGVkZ2UgPT4gZWRnZS5tb2Rlcy5pbmNsdWRlcygnY29zdGx5JykgJiYgZWRnZS5tb2Rlcy5pbmNsdWRlcygnb3B0aW9uYWwnKSlcbiAgY29uc3Qgc2FmZSA9IG1hY3JvLmVkZ2VzLmZpbmQoZWRnZSA9PiBlZGdlLm1vZGVzLmluY2x1ZGVzKCdzYWZlJykpXG4gIGlmICghY29zdGx5IHx8ICFzYWZlKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgRnJvc3QgbGF5b3V0OiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGZsb29yLnNpZGVTcGFjZXMgPSBjYXZlc1xuICBmbG9vci5mcm9zdExheW91dCA9IHsgc2hlbHZlczogZmxvb3IudGlsZXMuZmxhdE1hcCgodGlsZSwgaW5kZXgpID0+IHRpbGUua2luZCA9PT0gJ2ljZScgPyBbcG9pbnRBdChmbG9vciwgaW5kZXgpXSA6IFtdKS5zbGljZSgwLCAyMCArIGZsb29yLmluZGV4ICUgNCAqIDgpLCBjcmFja3M6IFtjb3N0bHkuY2VsbHMuZmlsdGVyKHBvaW50ID0+IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kID09PSAnZnJvc3RSaW1lJyldLCBzaGVsdGVyczogc2FmZS5jZWxscy5maWx0ZXIocG9pbnQgPT4gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpLnNsaWNlKDAsIGRlc2lyZWQpIH0gc2F0aXNmaWVzIEZyb3N0TGF5b3V0XG59XG5cbmNvbnN0IGltcHJpbnRCdXJpYWxSaXR1YWxSb3V0ZXMgPSAoZmxvb3I6IEZsb29yLCBtYWNybzogTWFjcm9SZWNpcGVEZWJ1Zyk6IHZvaWQgPT4ge1xuICBpZiAoZmxvb3IuYmlvbWUgIT09ICdidXJpYWwnKSByZXR1cm5cbiAgY29uc3Qgc2FmZSA9IG1hY3JvLmVkZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnc2FmZScpKVxuICBjb25zdCBjb3N0bHkgPSBtYWNyby5lZGdlcy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUubW9kZXMuaW5jbHVkZXMoJ2Nvc3RseScpICYmIGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnb3B0aW9uYWwnKSlcbiAgY29uc3Qgb2ZmZXJpbmcgPSBtYWNyby5ub2Rlcy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUua2luZCA9PT0gJ29wdGlvbmFsUmV3YXJkJylcbiAgaWYgKCFzYWZlIHx8ICFjb3N0bHkgfHwgIW9mZmVyaW5nKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgQnVyaWFsIHJpdHVhbCByb3V0ZXM6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgY29uc3QgdHJhbnNpdCA9IChjZWxsczogcmVhZG9ubHkgUG9pbnRbXSkgPT4gY2VsbHMuc2xpY2UoMywgLTMpLmZpbHRlcihwb2ludCA9PiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KT8ua2luZCA9PT0gJ2Zsb29yJylcbiAgY29uc3Qgc3Bpcml0Um91dGUgPSB0cmFuc2l0KHNhZmUuY2VsbHMpXG4gIGNvbnN0IGdyYXZlUm91dGUgPSB0cmFuc2l0KGNvc3RseS5jZWxscylcbiAgaWYgKHNwaXJpdFJvdXRlLmxlbmd0aCA8IDQgfHwgZ3JhdmVSb3V0ZS5sZW5ndGggPCA0KSB0aHJvdyBuZXcgRXJyb3IoYHNob3J0IEJ1cmlhbCByaXR1YWwgcm91dGU6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgZm9yIChjb25zdCBwb2ludCBvZiBzcGlyaXRSb3V0ZSkgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ3NwaXJpdFBhdGgnKVxuICBmb3IgKGNvbnN0IHBvaW50IG9mIGdyYXZlUm91dGUpIHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksICdncmF2ZVNvaWwnKVxuICBjb25zdCBjYWNoZSA9IEFycmF5LmZyb20oeyBsZW5ndGg6IG9mZmVyaW5nLmZvb3RwcmludC53aWR0aCAqIG9mZmVyaW5nLmZvb3RwcmludC5oZWlnaHQgfSwgKF8sIGluZGV4KSA9PiAoeyB4OiBvZmZlcmluZy5mb290cHJpbnQueCArIGluZGV4ICUgb2ZmZXJpbmcuZm9vdHByaW50LndpZHRoLCB5OiBvZmZlcmluZy5mb290cHJpbnQueSArIE1hdGguZmxvb3IoaW5kZXggLyBvZmZlcmluZy5mb290cHJpbnQud2lkdGgpIH0pKS5maW5kKHBvaW50ID0+IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kID09PSAnZmxvb3InKVxuICBpZiAoIWNhY2hlKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgQnVyaWFsIG9mZmVyaW5nIGhvbGxvdzogJHttYWNyby5yZWNpcGVJZH1gKVxuICBzZXRLaW5kKGZsb29yLCBjYWNoZS54LCBjYWNoZS55LCAnb3NzdWFyeScpXG59XG5cbmNvbnN0IGltcHJpbnRTYWx0Um91dGVDb250cmFjdCA9IChmbG9vcjogRmxvb3IsIG1hY3JvOiBNYWNyb1JlY2lwZURlYnVnKTogdm9pZCA9PiB7XG4gIGlmIChmbG9vci5iaW9tZSAhPT0gJ3NhbHRGbGF0cycpIHJldHVyblxuICBjb25zdCBzYWZlID0gbWFjcm8uZWRnZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdzYWZlJykpXG4gIGNvbnN0IGNvc3RseSA9IG1hY3JvLmVkZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnY29zdGx5JykgJiYgY2FuZGlkYXRlLm1vZGVzLmluY2x1ZGVzKCdvcHRpb25hbCcpKVxuICBjb25zdCByZWZ1Z2VOb2RlID0gbWFjcm8ubm9kZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmtpbmQgPT09ICdvcHRpb25hbFJld2FyZCcpXG4gIGlmICghc2FmZSB8fCAhY29zdGx5IHx8ICFyZWZ1Z2VOb2RlKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgU2FsdCByb3V0ZSBjaG9pY2VzOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGNvbnN0IHRyYW5zaXQgPSAoY2VsbHM6IHJlYWRvbmx5IFBvaW50W10pID0+IGNlbGxzLnNsaWNlKDMsIC0zKS5maWx0ZXIocG9pbnQgPT4gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmtpbmQgPT09ICdmbG9vcicpXG4gIGNvbnN0IHN0YWJsZVJvdXRlID0gdHJhbnNpdChzYWZlLmNlbGxzKVxuICBjb25zdCBicmluZVJvdXRlID0gdHJhbnNpdChjb3N0bHkuY2VsbHMpXG4gIGlmIChzdGFibGVSb3V0ZS5sZW5ndGggPCA0IHx8IGJyaW5lUm91dGUubGVuZ3RoIDwgNCkgdGhyb3cgbmV3IEVycm9yKGBzaG9ydCBTYWx0IHJvdXRlOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGZvciAoY29uc3QgcG9pbnQgb2Ygc3RhYmxlUm91dGUpIHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksICdmbG9vcicpXG4gIGZvciAoY29uc3QgcG9pbnQgb2YgYnJpbmVSb3V0ZSkgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2JyaW5lJylcbiAgY29uc3QgaG9yaXpvbiA9IGJyaW5lUm91dGVbTWF0aC5mbG9vcihicmluZVJvdXRlLmxlbmd0aCAvIDIpXVxuICBzZXRLaW5kKGZsb29yLCBob3Jpem9uLngsIGhvcml6b24ueSwgJ3NhbHRNaXJyb3InKVxuICBjb25zdCByZWZ1Z2UgPSB7IHg6IHJlZnVnZU5vZGUuZm9vdHByaW50LnggKyBNYXRoLmZsb29yKHJlZnVnZU5vZGUuZm9vdHByaW50LndpZHRoIC8gMiksIHk6IHJlZnVnZU5vZGUuZm9vdHByaW50LnkgKyBNYXRoLmZsb29yKHJlZnVnZU5vZGUuZm9vdHByaW50LmhlaWdodCAvIDIpIH1cbiAgc2V0S2luZChmbG9vciwgcmVmdWdlLngsIHJlZnVnZS55LCAnZmxvb3InKVxuICBmb3IgKGNvbnN0IFt4LCB5XSBvZiBjYXJkaW5hbE9mZnNldHMpIGlmIChnZXRUaWxlKGZsb29yLCByZWZ1Z2UueCArIHgsIHJlZnVnZS55ICsgeSk/LmtpbmQgPT09ICdmbG9vcicpIHNldEtpbmQoZmxvb3IsIHJlZnVnZS54ICsgeCwgcmVmdWdlLnkgKyB5LCAnc2FsdE1pcnJvcicpXG4gIGNvbnN0IGh1c2sgPSBwcm9wRGVmaW5pdGlvbignc2FsdEZsYXRzLmNhcmF2YW5IdXNrJylcbiAgZmxvb3IucHJvcHMucHVzaCh7IGlkOiBgcHJvcDoke2Zsb29yLmluZGV4fTpzYWx0RmxhdHMuY2FyYXZhbkh1c2s6JHtyZWZ1Z2UueH06JHtyZWZ1Z2UueX1gLCBraW5kOiBodXNrLmlkLCB4OiByZWZ1Z2UueCwgeTogcmVmdWdlLnksIGJpb21lOiBmbG9vci5iaW9tZSwgc3RhdGU6ICdkb3JtYW50JywgdGFnczogWy4uLmh1c2sudGFnc10sIGhvb2tzOiBbLi4uaHVzay5ob29rc10gfSlcbiAgY29uc3QgbWFya2VyUG9pbnQgPSBzdGFibGVSb3V0ZVtNYXRoLmZsb29yKHN0YWJsZVJvdXRlLmxlbmd0aCAvIDIpXVxuICBjb25zdCBtYXJrZXIgPSBwcm9wRGVmaW5pdGlvbignc2FsdEZsYXRzLmdsYXNzTWFya2VyJylcbiAgZmxvb3IucHJvcHMucHVzaCh7IGlkOiBgcHJvcDoke2Zsb29yLmluZGV4fTpzYWx0RmxhdHMuZ2xhc3NNYXJrZXI6JHttYXJrZXJQb2ludC54fToke21hcmtlclBvaW50Lnl9YCwga2luZDogbWFya2VyLmlkLCB4OiBtYXJrZXJQb2ludC54LCB5OiBtYXJrZXJQb2ludC55LCBiaW9tZTogZmxvb3IuYmlvbWUsIHN0YXRlOiAnZG9ybWFudCcsIHRhZ3M6IFsuLi5tYXJrZXIudGFnc10sIGhvb2tzOiBbLi4ubWFya2VyLmhvb2tzXSB9KVxufVxuXG5jb25zdCBpbXByaW50RnJvc3RSb3V0ZUNvbnRyYWN0ID0gKGZsb29yOiBGbG9vciwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcpOiB2b2lkID0+IHtcbiAgaWYgKGZsb29yLmJpb21lICE9PSAnZnJvc3RSZWxpcXVhcnknKSByZXR1cm5cbiAgY29uc3Qgc2FmZSA9IG1hY3JvLmVkZ2VzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnc2FmZScpKVxuICBjb25zdCBjb3N0bHkgPSBtYWNyby5lZGdlcy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUubW9kZXMuaW5jbHVkZXMoJ2Nvc3RseScpICYmIGNhbmRpZGF0ZS5tb2Rlcy5pbmNsdWRlcygnb3B0aW9uYWwnKSlcbiAgY29uc3QgcmVmdWdlTm9kZSA9IG1hY3JvLm5vZGVzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5raW5kID09PSAnb3B0aW9uYWxSZXdhcmQnKVxuICBpZiAoIXNhZmUgfHwgIWNvc3RseSB8fCAhcmVmdWdlTm9kZSkgdGhyb3cgbmV3IEVycm9yKGBtaXNzaW5nIEZyb3N0IHJvdXRlIGNob2ljZXM6ICR7bWFjcm8ucmVjaXBlSWR9YClcbiAgY29uc3QgdHJhbnNpdCA9IChjZWxsczogcmVhZG9ubHkgUG9pbnRbXSkgPT4gY2VsbHMuc2xpY2UoMywgLTMpLmZpbHRlcihwb2ludCA9PiBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KT8ua2luZCA9PT0gJ2Zsb29yJylcbiAgY29uc3Qgc2hvcmVSb3V0ZSA9IHRyYW5zaXQoc2FmZS5jZWxscylcbiAgY29uc3QgaWNlUm91dGUgPSB0cmFuc2l0KGNvc3RseS5jZWxscylcbiAgaWYgKHNob3JlUm91dGUubGVuZ3RoIDwgNCB8fCBpY2VSb3V0ZS5sZW5ndGggPCA0KSB0aHJvdyBuZXcgRXJyb3IoYHNob3J0IEZyb3N0IHJvdXRlOiAke21hY3JvLnJlY2lwZUlkfWApXG4gIGZvciAoY29uc3QgcG9pbnQgb2Ygc2hvcmVSb3V0ZSkgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2Zsb29yJylcbiAgZm9yIChjb25zdCBwb2ludCBvZiBpY2VSb3V0ZSkgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwgJ2ljZScpXG4gIGNvbnN0IHByZXNzdXJlID0gaWNlUm91dGVbTWF0aC5mbG9vcihpY2VSb3V0ZS5sZW5ndGggLyAyKV1cbiAgc2V0S2luZChmbG9vciwgcHJlc3N1cmUueCwgcHJlc3N1cmUueSwgJ2Zyb3N0UmltZScpXG4gIGNvbnN0IHJlZnVnZSA9IHsgeDogcmVmdWdlTm9kZS5mb290cHJpbnQueCArIE1hdGguZmxvb3IocmVmdWdlTm9kZS5mb290cHJpbnQud2lkdGggLyAyKSwgeTogcmVmdWdlTm9kZS5mb290cHJpbnQueSArIE1hdGguZmxvb3IocmVmdWdlTm9kZS5mb290cHJpbnQuaGVpZ2h0IC8gMikgfVxuICBzZXRLaW5kKGZsb29yLCByZWZ1Z2UueCwgcmVmdWdlLnksICdmbG9vcicpXG4gIGZvciAoY29uc3QgW3gsIHldIG9mIGNhcmRpbmFsT2Zmc2V0cykgaWYgKGdldFRpbGUoZmxvb3IsIHJlZnVnZS54ICsgeCwgcmVmdWdlLnkgKyB5KT8ua2luZCAhPT0gJ3dhbGwnKSBzZXRLaW5kKGZsb29yLCByZWZ1Z2UueCArIHgsIHJlZnVnZS55ICsgeSwgJ2Zsb29yJylcbiAgY29uc3QgcGxhY2UgPSAoa2luZDogUHJvcFsna2luZCddLCBwb2ludDogUG9pbnQpID0+IHtcbiAgICBjb25zdCBkZWZpbml0aW9uID0gcHJvcERlZmluaXRpb24oa2luZClcbiAgICBmbG9vci5wcm9wcy5wdXNoKHsgaWQ6IGBwcm9wOiR7Zmxvb3IuaW5kZXh9OiR7a2luZH06JHtwb2ludC54fToke3BvaW50Lnl9YCwga2luZCwgeDogcG9pbnQueCwgeTogcG9pbnQueSwgYmlvbWU6IGZsb29yLmJpb21lLCBzdGF0ZTogJ2Rvcm1hbnQnLCB0YWdzOiBbLi4uZGVmaW5pdGlvbi50YWdzXSwgaG9va3M6IFsuLi5kZWZpbml0aW9uLmhvb2tzXSB9KVxuICB9XG4gIHBsYWNlKCdmcm9zdFJlbGlxdWFyeS5kdWVsQmVsbCcsIHJlZnVnZSlcbiAgcGxhY2UoJ2Zyb3N0UmVsaXF1YXJ5LnJpbWVTYXJjb3BoYWd1cycsIHNob3JlUm91dGVbTWF0aC5mbG9vcihzaG9yZVJvdXRlLmxlbmd0aCAvIDIpXSlcbiAgcGxhY2UoJ2Zyb3N0UmVsaXF1YXJ5LmljZUZvcmdlJywgaWNlUm91dGVbMV0pXG4gIHBsYWNlKCdmcm9zdFJlbGlxdWFyeS5mcm96ZW5DYWNoZScsIHByZXNzdXJlKVxufVxuXG5jb25zdCBpbXByaW50Q2F2ZXJuU2V0cGllY2VDb250ZXh0ID0gKGZsb29yOiBGbG9vciwgbWFjcm86IE1hY3JvUmVjaXBlRGVidWcpOiB2b2lkID0+IHtcbiAgaWYgKGZsb29yLmJpb21lICE9PSAnY2F2ZXJucycpIHJldHVyblxuICBjb25zdCBub2RlID0gbWFjcm8ubm9kZXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmtpbmQgPT09ICdvcHRpb25hbFJld2FyZCcpXG4gIGlmICghbm9kZSkgdGhyb3cgbmV3IEVycm9yKGBtaXNzaW5nIENhdmVybnMgb3B0aW9uYWwgY2hhbWJlcjogJHttYWNyby5yZWNpcGVJZH1gKVxuICBjb25zdCBjYW5kaWRhdGVzID0gQXJyYXkuZnJvbSh7IGxlbmd0aDogTWF0aC5tYXgoMCwgbm9kZS5mb290cHJpbnQud2lkdGggLSAyKSAqIE1hdGgubWF4KDAsIG5vZGUuZm9vdHByaW50LmhlaWdodCAtIDIpIH0sIChfLCBpbmRleCkgPT4gKHsgeDogbm9kZS5mb290cHJpbnQueCArIDEgKyBpbmRleCAlIChub2RlLmZvb3RwcmludC53aWR0aCAtIDIpLCB5OiBub2RlLmZvb3RwcmludC55ICsgMSArIE1hdGguZmxvb3IoaW5kZXggLyAobm9kZS5mb290cHJpbnQud2lkdGggLSAyKSkgfSkpXG4gIGNvbnN0IHBvaW50ID0gY2FuZGlkYXRlcy5maW5kKGNhbmRpZGF0ZSA9PiBnZXRUaWxlKGZsb29yLCBjYW5kaWRhdGUueCwgY2FuZGlkYXRlLnkpPy5raW5kID09PSAnZmxvb3InKVxuICBpZiAoIXBvaW50KSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgQ2F2ZXJucyBzZXRwaWVjZSBmbG9vcjogJHttYWNyby5yZWNpcGVJZH1gKVxuICBjb25zdCBuZWFyYnkgPSBjYXJkaW5hbE9mZnNldHMubWFwKChbeCwgeV0pID0+ICh7IHg6IHBvaW50LnggKyB4LCB5OiBwb2ludC55ICsgeSB9KSkuZmlsdGVyKGNhbmRpZGF0ZSA9PiBnZXRUaWxlKGZsb29yLCBjYW5kaWRhdGUueCwgY2FuZGlkYXRlLnkpPy5raW5kID09PSAnZmxvb3InKVxuICBjb25zdCBkYXJrbmVzcyA9IG5lYXJieVswXVxuICBpZiAoIWRhcmtuZXNzKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgQ2F2ZXJucyBzZXRwaWVjZSBkYXJrbmVzczogJHttYWNyby5yZWNpcGVJZH1gKVxuICBjb25zdCB3YXRlciA9IG5lYXJieVsxXVxuICBjb25zdCBwb29sID0gbmVhcmJ5WzJdXG4gIHNldEtpbmQoZmxvb3IsIGRhcmtuZXNzLngsIGRhcmtuZXNzLnksICdkYXJrbmVzcycpXG4gIGlmICh3YXRlcikgc2V0S2luZChmbG9vciwgd2F0ZXIueCwgd2F0ZXIueSwgJ3dhdGVyJylcbiAgaWYgKHBvb2wpIHNldEtpbmQoZmxvb3IsIHBvb2wueCwgcG9vbC55LCAnZGVlcFdhdGVyJylcbiAgY29uc3QgZnVuZ3VzID0gcHJvcERlZmluaXRpb24oJ2NhdmVybnMuZ2xvd2luZ0Z1bmd1cycpXG4gIGZsb29yLnByb3BzLnB1c2goeyBpZDogYHByb3A6JHtmbG9vci5pbmRleH06Y2F2ZXJucy5nbG93aW5nRnVuZ3VzOiR7cG9pbnQueH06JHtwb2ludC55fWAsIGtpbmQ6IGZ1bmd1cy5pZCwgeDogcG9pbnQueCwgeTogcG9pbnQueSwgYmlvbWU6IGZsb29yLmJpb21lLCBzdGF0ZTogJ2Rvcm1hbnQnLCB0YWdzOiBbLi4uZnVuZ3VzLnRhZ3NdLCBob29rczogWy4uLmZ1bmd1cy5ob29rc10gfSlcbn1cblxuY29uc3QgaW1wcmludEJpb21lTGFuZG1hcmtzID0gKGZsb29yOiBGbG9vciwgcm5nOiBSbmcsIHJvb21zOiByZWFkb25seSBSb29tW10pOiB2b2lkID0+IHtcbiAgY29uc3QgcGFpbnQgPSAoeDogbnVtYmVyLCB5OiBudW1iZXIsIGtpbmQ6IFRpbGVbJ2tpbmQnXSkgPT4geyBpZiAoZ2V0VGlsZShmbG9vciwgeCwgeSk/LmtpbmQgPT09ICdmbG9vcicpIHNldEtpbmQoZmxvb3IsIHgsIHksIGtpbmQpIH1cbiAgaWYgKGZsb29yLmJpb21lID09PSAnYnVyaWFsJykge1xuICAgIGZvciAoY29uc3Qgcm9vbSBvZiByb29tcy5zbGljZSgxKSkge1xuICAgICAgY29uc3Qgb3JpZ2luID0gY2VudGVyKHJvb20pXG4gICAgICBmb3IgKGxldCB5ID0gLTM7IHkgPD0gMzsgeSsrKSBmb3IgKGxldCB4ID0gLTM7IHggPD0gMzsgeCsrKSB7XG4gICAgICAgIGNvbnN0IGRpc3RhbmNlID0gTWF0aC5hYnMoeCkgKyBNYXRoLmFicyh5KVxuICAgICAgICBpZiAoZGlzdGFuY2UgPT09IDMpIHBhaW50KG9yaWdpbi54ICsgeCwgb3JpZ2luLnkgKyB5LCAnY2Fpcm4nKVxuICAgICAgICBlbHNlIGlmIChkaXN0YW5jZSA8IDMgJiYgcm5nLmNoYW5jZSg1NSkpIHBhaW50KG9yaWdpbi54ICsgeCwgb3JpZ2luLnkgKyB5LCAnZ3JhdmVTb2lsJylcbiAgICAgIH1cbiAgICB9XG4gIH0gZWxzZSBpZiAoZmxvb3IuYmlvbWUgPT09ICdzYWx0RmxhdHMnKSB7XG4gICAgZm9yIChsZXQgeCA9IDc7IHggPCBmbG9vci53aWR0aCAtIDU7IHggKz0gMTEpIGZvciAobGV0IHkgPSAyOyB5IDwgZmxvb3IuaGVpZ2h0IC0gMjsgeSsrKSBpZiAoKHkgKyB4KSAlIDcgIT09IDApIHBhaW50KHgsIHksIGZsb29yLmxheW91dElkLmluY2x1ZGVzKCdjYXJhdmFuJykgPyAnc2FsdE1pcnJvcicgOiAnYnJpbmUnKVxuICB9IGVsc2UgaWYgKGZsb29yLmJpb21lID09PSAnZnJvc3RSZWxpcXVhcnknKSB7XG4gICAgY29uc3QgbGFrZSA9IHJvb21zWzFdID8/IHJvb21zWzBdXG4gICAgZm9yIChsZXQgeSA9IGxha2UueSArIDE7IHkgPCBsYWtlLnkgKyBsYWtlLmggLSAxOyB5KyspIGZvciAobGV0IHggPSBsYWtlLnggKyAxOyB4IDwgbGFrZS54ICsgbGFrZS53IC0gMTsgeCsrKSBwYWludCh4LCB5LCBmbG9vci5sYXlvdXRJZC5pbmNsdWRlcygnZnJvemVuLWxha2UnKSA/ICdpY2UnIDogJ2Zyb3N0UmltZScpXG4gIH0gZWxzZSBpZiAoZmxvb3IuYmlvbWUgPT09ICdjbGlmZnMnKSB7XG4gICAgZm9yIChsZXQgeSA9IDU7IHkgPCBmbG9vci5oZWlnaHQgLSA0OyB5ICs9IDkpIGZvciAobGV0IHggPSAyOyB4IDwgZmxvb3Iud2lkdGggLSAyOyB4KyspIGlmICh4ICUgOCAhPT0gMCkgcGFpbnQoeCwgeSwgJ2xlZGdlJylcbiAgfSBlbHNlIGlmIChmbG9vci5iaW9tZSA9PT0gJ2Z1cm5hY2UnKSB7XG4gICAgZm9yIChsZXQgeSA9IDQ7IHkgPCBmbG9vci5oZWlnaHQgLSAzOyB5ICs9IDcpIGZvciAobGV0IHggPSAzOyB4IDwgZmxvb3Iud2lkdGggLSAzOyB4KyspIGlmICh4ICUgOSAhPT0gMCkgcGFpbnQoeCwgeSwgZmxvb3IubGF5b3V0SWQuaW5jbHVkZXMoJ2tpbG4nKSA/ICdsaWZ0JyA6ICdzbW9rZScpXG4gIH0gZWxzZSBpZiAoZmxvb3IuYmlvbWUgPT09ICdydWlucycpIHtcbiAgICBmb3IgKGNvbnN0IHJvb20gb2Ygcm9vbXMuc2xpY2UoMSwgLTEpKSB7XG4gICAgICBjb25zdCBvcmlnaW4gPSBjZW50ZXIocm9vbSlcbiAgICAgIGZvciAoY29uc3QgW3gsIHldIG9mIGNhcmRpbmFsT2Zmc2V0cykgcGFpbnQob3JpZ2luLnggKyB4ICogMiwgb3JpZ2luLnkgKyB5ICogMiwgJ2RhcnQnKVxuICAgIH1cbiAgfVxufVxuXG5mdW5jdGlvbiBjb25uZWN0Um9vbXMoZmxvb3I6IEZsb29yLCByb29tczogUm9vbVtdKTogdm9pZCB7XG4gIGZvciAobGV0IGkgPSAxOyBpIDwgcm9vbXMubGVuZ3RoOyBpKyspIHtcbiAgICBjb25zdCBmcm9tID0gY2VudGVyKHJvb21zW2kgLSAxXSlcbiAgICBjb25zdCB0byA9IGNlbnRlcihyb29tc1tpXSlcbiAgICBpZiAoaSAlIDIpIHsgY2FydmVIKGZsb29yLCBmcm9tLngsIHRvLngsIGZyb20ueSk7IGNhcnZlVihmbG9vciwgZnJvbS55LCB0by55LCB0by54KSB9XG4gICAgZWxzZSB7IGNhcnZlVihmbG9vciwgZnJvbS55LCB0by55LCBmcm9tLngpOyBjYXJ2ZUgoZmxvb3IsIGZyb20ueCwgdG8ueCwgdG8ueSkgfVxuICB9XG59XG5cbmludGVyZmFjZSBQbGFjZW1lbnRSdW50aW1lIHsgbWFjcm86IE1hY3JvUmVjaXBlRGVidWc7IHBpbG90OiBib29sZWFuOyBkaWFnbm9zdGljczogUGxhY2VtZW50RGVidWdbXTsgcmVhY2hhYmxlPzogU2V0PG51bWJlcj4gfVxuY29uc3QgbmF0aXZlQWN0b3JUZXJyYWluOiBSZWNvcmQ8QmlvbWUsIHJlYWRvbmx5IFRpbGVLaW5kW10+ID0ge1xuICBtaW5lOiBbJ3JhaWwnLCAnc3VwcG9ydCddLCB3aWxkczogWyd3YXRlcicsICd3ZWInXSwgY2F2ZXJuczogWyd3YXRlcicsICdjdXJyZW50JywgJ2RhcmtuZXNzJ10sIHJ1aW5zOiBbJ2FsdGFyJywgJ2RhcnQnXSwgZnVybmFjZTogWydzbW9rZScsICdsaWZ0J10sIGZsb29kZWRSdWluczogWydjdXJyZW50JywgJ2FuY2hvciddLCBjbGlmZnM6IFsnbGVkZ2UnLCAncm9wZSddLCBidXJpYWw6IFsnZ3JhdmVTb2lsJywgJ3NwaXJpdFBhdGgnXSwgc2FsdEZsYXRzOiBbJ3NhbHRNaXJyb3InLCAnYnJpbmUnXSwgZnJvc3RSZWxpcXVhcnk6IFsnaWNlJywgJ2Zyb3N0UmltZSddXG59XG5jb25zdCBwbGFjZW1lbnRDb250ZXh0ID0gKGZsb29yOiBGbG9vciwgcnVudGltZTogUGxhY2VtZW50UnVudGltZSwgZWxpZ2libGU6IChwb2ludDogUG9pbnQpID0+IGJvb2xlYW4gPSAoKSA9PiB0cnVlKTogUGxhY2VtZW50Q29udGV4dCA9PiB7XG4gIGNvbnN0IG5vZGVLaW5kcyA9IG5ldyBNYXA8c3RyaW5nLCBSb3V0ZU5vZGVLaW5kW10+KClcbiAgY29uc3QgZWRnZU1vZGVzID0gbmV3IE1hcDxzdHJpbmcsIFJvdXRlRWRnZU1vZGVbXT4oKVxuICBpZiAocnVudGltZS5waWxvdCkge1xuICAgIGZvciAoY29uc3Qgbm9kZSBvZiBydW50aW1lLm1hY3JvLm5vZGVzKSBmb3IgKGxldCB5ID0gbm9kZS5mb290cHJpbnQueTsgeSA8IG5vZGUuZm9vdHByaW50LnkgKyBub2RlLmZvb3RwcmludC5oZWlnaHQ7IHkrKykgZm9yIChsZXQgeCA9IG5vZGUuZm9vdHByaW50Lng7IHggPCBub2RlLmZvb3RwcmludC54ICsgbm9kZS5mb290cHJpbnQud2lkdGg7IHgrKykge1xuICAgICAgY29uc3Qga2V5ID0gcG9pbnRLZXkoeyB4LCB5IH0pXG4gICAgICBub2RlS2luZHMuc2V0KGtleSwgWy4uLihub2RlS2luZHMuZ2V0KGtleSkgPz8gW10pLCBub2RlLmtpbmRdKVxuICAgIH1cbiAgICBmb3IgKGNvbnN0IGVkZ2Ugb2YgcnVudGltZS5tYWNyby5lZGdlcykgZm9yIChjb25zdCBwb2ludCBvZiBlZGdlLmNlbGxzKSB7XG4gICAgICBjb25zdCBrZXkgPSBwb2ludEtleShwb2ludClcbiAgICAgIGVkZ2VNb2Rlcy5zZXQoa2V5LCBbLi4uKGVkZ2VNb2Rlcy5nZXQoa2V5KSA/PyBbXSksIC4uLmVkZ2UubW9kZXNdKVxuICAgIH1cbiAgfVxuICBjb25zdCBhZGphY2VudCA9IChwb2ludDogUG9pbnQpOiBQb2ludFtdID0+IGNhcmRpbmFsT2Zmc2V0cy5tYXAoKFt4LCB5XSkgPT4gKHsgeDogcG9pbnQueCArIHgsIHk6IHBvaW50LnkgKyB5IH0pKS5maWx0ZXIocG9pbnQgPT4gaW5Cb3VuZHMoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKVxuICBjb25zdCBibG9ja2VkID0gKHBvaW50OiBQb2ludCk6IGJvb2xlYW4gPT4gIWVsaWdpYmxlKHBvaW50KSB8fCBmbG9vci5wcm9wcy5zb21lKHByb3AgPT4gcHJvcC54ID09PSBwb2ludC54ICYmIHByb3AueSA9PT0gcG9pbnQueSkgfHwgZmxvb3IuYWN0b3JzLnNvbWUoYWN0b3IgPT4gYWN0b3IuaGVhbHRoID4gMCAmJiBhY3Rvci54ID09PSBwb2ludC54ICYmIGFjdG9yLnkgPT09IHBvaW50LnkpIHx8IGZsb29yLml0ZW1zLnNvbWUoaXRlbSA9PiBpdGVtLnggPT09IHBvaW50LnggJiYgaXRlbS55ID09PSBwb2ludC55KSB8fCBmbG9vci5taWxlc3RvbmVzLnNvbWUobWlsZXN0b25lID0+IG1pbGVzdG9uZS54ID09PSBwb2ludC54ICYmIG1pbGVzdG9uZS55ID09PSBwb2ludC55KSB8fCBmbG9vci5lY29sb2d5Py5zb21lKGVjb2xvZ3kgPT4gZWNvbG9neS50YXJnZXQueCA9PT0gcG9pbnQueCAmJiBlY29sb2d5LnRhcmdldC55ID09PSBwb2ludC55KSA9PT0gdHJ1ZVxuICByZXR1cm4ge1xuICAgIHBvaW50czogZmxvb3IudGlsZXMubWFwKChfLCBpbmRleCkgPT4gcG9pbnRBdChmbG9vciwgaW5kZXgpKSxcbiAgICB0ZXJyYWluQXQ6IHBvaW50ID0+IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpPy5raW5kLFxuICAgIHBhc3NhYmxlQXQ6IHBvaW50ID0+IEJvb2xlYW4oZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSkgJiYgaXNQYXRoUGFzc2FibGUoZmxvb3IsIHBvaW50LCB0cnVlKSksXG4gICAgYmxvY2tlZEF0OiBibG9ja2VkLFxuICAgIGRpc3RhbmNlRnJvbVN0YXJ0OiBwb2ludCA9PiBkaXN0YW5jZShwb2ludCwgZmxvb3Iuc3RhcnQpLFxuICAgIHZpc2libGVBdDogcG9pbnQgPT4gQm9vbGVhbihnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KT8udmlzaWJsZSksXG4gICAgY292ZXJlZEF0OiBwb2ludCA9PiBhZGphY2VudChwb2ludCkuc29tZShjYW5kaWRhdGUgPT4gIXBhc3NhYmxlKGdldFRpbGUoZmxvb3IsIGNhbmRpZGF0ZS54LCBjYW5kaWRhdGUueSk/LmtpbmQgPz8gJ3dhbGwnKSB8fCBpc0Jsb2NraW5nUHJvcChwcm9wQXQoZmxvb3IucHJvcHMsIGNhbmRpZGF0ZS54LCBjYW5kaWRhdGUueSkpKSxcbiAgICBjaG9rZXBvaW50QXQ6IHBvaW50ID0+IGFkamFjZW50KHBvaW50KS5maWx0ZXIoY2FuZGlkYXRlID0+IGlzUGF0aFBhc3NhYmxlKGZsb29yLCBjYW5kaWRhdGUsIGZhbHNlKSkubGVuZ3RoIDw9IDIsXG4gICAgYWRqYWNlbnRUZXJyYWluQXQ6IHBvaW50ID0+IGFkamFjZW50KHBvaW50KS5tYXAoY2FuZGlkYXRlID0+IGdldFRpbGUoZmxvb3IsIGNhbmRpZGF0ZS54LCBjYW5kaWRhdGUueSk/LmtpbmQpLmZpbHRlcigoa2luZCk6IGtpbmQgaXMgVGlsZUtpbmQgPT4gQm9vbGVhbihraW5kKSksXG4gICAgbm9kZUtpbmRzQXQ6IHBvaW50ID0+IG5vZGVLaW5kcy5nZXQocG9pbnRLZXkocG9pbnQpKSA/PyBbXSxcbiAgICBlZGdlTW9kZXNBdDogcG9pbnQgPT4gZWRnZU1vZGVzLmdldChwb2ludEtleShwb2ludCkpID8/IFtdXG4gIH1cbn1cbmNvbnN0IHBsYWNlbWVudFJlYWNoYWJpbGl0eSA9IChmbG9vcjogRmxvb3IsIHJ1bnRpbWU6IFBsYWNlbWVudFJ1bnRpbWUpOiBTZXQ8bnVtYmVyPiA9PiBydW50aW1lLnJlYWNoYWJsZSA/Pz0gcmVhY2hhYmxlRmxvb3JJbmRleGVzKGZsb29yKVxuY29uc3QgY2hvb3NlUGxhY2VtZW50ID0gKGZsb29yOiBGbG9vciwgcnVudGltZTogUGxhY2VtZW50UnVudGltZSwgY29udHJhY3Q6IFBsYWNlbWVudENvbnRyYWN0LCBlbGlnaWJsZT86IChwb2ludDogUG9pbnQpID0+IGJvb2xlYW4pOiBQb2ludCB8IHVuZGVmaW5lZCA9PiB7XG4gIGNvbnN0IHJlYWNoYWJsZSA9IHBsYWNlbWVudFJlYWNoYWJpbGl0eShmbG9vciwgcnVudGltZSlcbiAgY29uc3Qgc2VsZWN0aW9uID0gc2VsZWN0UGxhY2VtZW50KGNvbnRyYWN0LCBwbGFjZW1lbnRDb250ZXh0KGZsb29yLCBydW50aW1lLCBwb2ludCA9PiByZWFjaGFibGUuaGFzKGluZGV4T2YoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKSAmJiAoZWxpZ2libGU/Lihwb2ludCkgPz8gdHJ1ZSkpKVxuICBydW50aW1lLmRpYWdub3N0aWNzLnB1c2goc2VsZWN0aW9uLmRlYnVnKVxuICByZXR1cm4gc2VsZWN0aW9uLnBvaW50XG59XG5cbmZ1bmN0aW9uIHBsYWNlUHJvcHMoZmxvb3I6IEZsb29yLCByZXNlcnZlZDogUmVhZG9ubHlTZXQ8bnVtYmVyPiwgcnVudGltZTogUGxhY2VtZW50UnVudGltZSk6IHZvaWQge1xuICBjb25zdCBsaW5rczogUGFydGlhbDxSZWNvcmQ8UHJvcFsna2luZCddLCBQcm9wWydraW5kJ10+PiA9IHtcbiAgICAnbWluZS5sYW50ZXJuUG9zdCc6ICdtaW5lLndhcm5pbmdNYXJrZXInLFxuICAgICdtaW5lLmJyb2tlbkNhcnQnOiAnbWluZS5kaXNjYXJkZWRQYXJjZWwnLFxuICAgICd3aWxkcy5yb290U2hyaW5lJzogJ3dpbGRzLmxvc3RQYXJjZWwnLFxuICAgICdjYXZlcm5zLmJhcm5hY2xlZFNocmluZSc6ICdjYXZlcm5zLnNlYWxlZFBhcmNlbCcsXG4gICAgJ3J1aW5zLnJpdHVhbEJyYXppZXInOiAncnVpbnMuc2VhbGVkQ2FjaGUnXG4gIH1cbiAgY29uc3QgY29tcGFuaW9ucyA9IG5ldyBTZXQoT2JqZWN0LnZhbHVlcyhsaW5rcykpXG4gIGNvbnN0IGRlZmluaXRpb25zID0gWy4uLnByb3BEZWZpbml0aW9uc0ZvcihmbG9vci5iaW9tZSldLnNvcnQoKGxlZnQsIHJpZ2h0KSA9PiBOdW1iZXIoY29tcGFuaW9ucy5oYXMobGVmdC5pZCkpIC0gTnVtYmVyKGNvbXBhbmlvbnMuaGFzKHJpZ2h0LmlkKSkpXG4gIGxldCByZWFjaGFibGUgPSBwbGFjZW1lbnRSZWFjaGFiaWxpdHkoZmxvb3IsIHJ1bnRpbWUpXG4gIGNvbnN0IGFuY2hvcnMgPSBuZXcgTWFwPFByb3BbJ2tpbmQnXSwgUG9pbnQ+KClcbiAgY29uc3Qgb2NjdXBpZWQgPSBuZXcgU2V0PG51bWJlcj4oW1xuICAgIGluZGV4T2YoZmxvb3IsIGZsb29yLnN0YXJ0LngsIGZsb29yLnN0YXJ0LnkpLFxuICAgIGluZGV4T2YoZmxvb3IsIGZsb29yLmV4aXQueCwgZmxvb3IuZXhpdC55KSxcbiAgICAuLi5vYmplY3RpdmVUYXJnZXRzKGZsb29yKS5tYXAocG9pbnQgPT4gaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpLFxuICAgIC4uLmZsb29yLmFjdG9ycy5tYXAoYWN0b3IgPT4gaW5kZXhPZihmbG9vciwgYWN0b3IueCwgYWN0b3IueSkpLFxuICAgIC4uLmZsb29yLml0ZW1zLm1hcChpdGVtID0+IGluZGV4T2YoZmxvb3IsIGl0ZW0ueCwgaXRlbS55KSksXG4gICAgLi4uZmxvb3IucHJvcHMubWFwKHByb3AgPT4gaW5kZXhPZihmbG9vciwgcHJvcC54LCBwcm9wLnkpKVxuICBdKVxuICBmb3IgKGNvbnN0IGRlZmluaXRpb24gb2YgZGVmaW5pdGlvbnMpIHtcbiAgICBjb25zdCBhbmNob3JJZCA9IE9iamVjdC5lbnRyaWVzKGxpbmtzKS5maW5kKChbLCBjb21wYW5pb25dKSA9PiBjb21wYW5pb24gPT09IGRlZmluaXRpb24uaWQpPy5bMF0gYXMgUHJvcFsna2luZCddIHwgdW5kZWZpbmVkXG4gICAgY29uc3QgYW5jaG9yID0gYW5jaG9ySWQgPyBhbmNob3JzLmdldChhbmNob3JJZCkgOiB1bmRlZmluZWRcbiAgICBpZiAoY29tcGFuaW9ucy5oYXMoZGVmaW5pdGlvbi5pZCkgJiYgIWFuY2hvcikge1xuICAgICAgcnVudGltZS5kaWFnbm9zdGljcy5wdXNoKHsgaWQ6IGBwcm9wOiR7ZGVmaW5pdGlvbi5pZH1gLCByYW5rZWQ6IDAsIHVzZWRGYWxsYmFjazogZmFsc2UsIGRpYWdub3N0aWNzOiBbYHBsYWNlbWVudCBwcm9wOiR7ZGVmaW5pdGlvbi5pZH06IGxpbmtlZCBzZXRwaWVjZSBhbmNob3IgaXMgdW5hdmFpbGFibGVgXSwgcmVxdWlyZW1lbnRzOiB7IHRlcnJhaW46IFsuLi5kZWZpbml0aW9uLnRlcnJhaW5dIH0gfSlcbiAgICAgIGNvbnRpbnVlXG4gICAgfVxuICAgIGNvbnN0IHRhZ3MgPSBuZXcgU2V0KGRlZmluaXRpb24udGFncylcbiAgICBjb25zdCBjYXJ0ID0gZGVmaW5pdGlvbi5pZCA9PT0gJ21pbmUuYnJva2VuQ2FydCdcbiAgICBjb25zdCByZXF1aXJlbWVudHM6IFBsYWNlbWVudENvbnRyYWN0ID0ge1xuICAgICAgaWQ6IGBwcm9wOiR7ZGVmaW5pdGlvbi5pZH1gLFxuICAgICAgcmVxdWlyZW1lbnRzOiB7XG4gICAgICAgIHRlcnJhaW46IFsuLi5kZWZpbml0aW9uLnRlcnJhaW5dLFxuICAgICAgICBtaW5EaXN0YW5jZTogdGFncy5oYXMoJ2NhY2hlJykgPyA4IDogNSxcbiAgICAgICAgLi4uKHJ1bnRpbWUucGlsb3QgJiYgdGFncy5oYXMoJ3JvdXRlJykgPyBjYXJ0ID8geyBub2RlS2luZHM6IFsnbGFuZG1hcmsnXSBhcyBSb3V0ZU5vZGVLaW5kW10gfSA6IHsgZWRnZU1vZGVzOiBbJ21haW4nXSBhcyBSb3V0ZUVkZ2VNb2RlW10gfSA6IHt9KSxcbiAgICAgICAgLi4uKHJ1bnRpbWUucGlsb3QgJiYgdGFncy5oYXMoJ2NhY2hlJykgPyB7IG5vZGVLaW5kczogWydvcHRpb25hbFJld2FyZCddIGFzIFJvdXRlTm9kZUtpbmRbXSB9IDoge30pLFxuICAgICAgICAuLi4oYW5jaG9yID8geyBuZWFyOiBhbmNob3IsIG5lYXJEaXN0YW5jZTogMTAgfSA6IHt9KVxuICAgICAgfVxuICAgIH1cbiAgICBjb25zdCBwb2ludCA9IGNob29zZVBsYWNlbWVudChmbG9vciwgcnVudGltZSwgcmVxdWlyZW1lbnRzLCBjYW5kaWRhdGUgPT4gcmVhY2hhYmxlLmhhcyhpbmRleE9mKGZsb29yLCBjYW5kaWRhdGUueCwgY2FuZGlkYXRlLnkpKSAmJiAhcmVzZXJ2ZWQuaGFzKGluZGV4T2YoZmxvb3IsIGNhbmRpZGF0ZS54LCBjYW5kaWRhdGUueSkpICYmICFvY2N1cGllZC5oYXMoaW5kZXhPZihmbG9vciwgY2FuZGlkYXRlLngsIGNhbmRpZGF0ZS55KSkgJiYgaGFzUHJvcENvbnRleHQoZmxvb3IsIGRlZmluaXRpb24uaWQsIGNhbmRpZGF0ZSkpXG4gICAgaWYgKCFwb2ludCkgY29udGludWVcbiAgICBjb25zdCBwcm9wOiBQcm9wID0ge1xuICAgICAgaWQ6IGBwcm9wOiR7Zmxvb3IuaW5kZXh9OiR7ZGVmaW5pdGlvbi5pZH06JHtwb2ludC54fToke3BvaW50Lnl9YCxcbiAgICAgIGtpbmQ6IGRlZmluaXRpb24uaWQsXG4gICAgICB4OiBwb2ludC54LFxuICAgICAgeTogcG9pbnQueSxcbiAgICAgIGJpb21lOiBmbG9vci5iaW9tZSxcbiAgICAgIHN0YXRlOiAnZG9ybWFudCcsXG4gICAgICB0YWdzOiBbLi4uZGVmaW5pdGlvbi50YWdzXSxcbiAgICAgIGhvb2tzOiBbLi4uZGVmaW5pdGlvbi5ob29rc11cbiAgICB9XG4gICAgaWYgKGlzQmxvY2tpbmdQcm9wKHByb3ApKSB7XG4gICAgICBmbG9vci5wcm9wcy5wdXNoKHByb3ApXG4gICAgICBjb25zdCBrZWVwc0V4aXRSZWFjaGFibGUgPSBoYXNQYXNzYWJsZVBhdGgoZmxvb3IsIGZsb29yLnN0YXJ0LCBmbG9vci5leGl0KVxuICAgICAgY29uc3Qga2VlcHNPYmplY3RpdmVSZWFjaGFibGUgPSBvYmplY3RpdmVUYXJnZXRzKGZsb29yKS5zb21lKHRhcmdldCA9PiBjYW5SZWFjaE9iamVjdGl2ZVdpdGhQcm9wcyhmbG9vciwgdGFyZ2V0KSlcbiAgICAgIGZsb29yLnByb3BzLnBvcCgpXG4gICAgICBpZiAoIWtlZXBzRXhpdFJlYWNoYWJsZSB8fCAha2VlcHNPYmplY3RpdmVSZWFjaGFibGUpIHtcbiAgICAgICAgY29uc3QgZGVidWcgPSBydW50aW1lLmRpYWdub3N0aWNzLmF0KC0xKVxuICAgICAgICBpZiAoZGVidWcpIHsgZGVsZXRlIGRlYnVnLnNlbGVjdGVkOyBkZWJ1Zy5kaWFnbm9zdGljcy5wdXNoKGBwbGFjZW1lbnQgcHJvcDoke2RlZmluaXRpb24uaWR9OiByZWplY3RlZCBiZWNhdXNlIGl0IGJsb2NrcyBtYW5kYXRvcnkgcGF0aGluZ2ApIH1cbiAgICAgICAgY29udGludWVcbiAgICAgIH1cbiAgICB9XG4gICAgZmxvb3IucHJvcHMucHVzaChwcm9wKVxuICAgIG9jY3VwaWVkLmFkZChpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSlcbiAgICBpZiAoaXNCbG9ja2luZ1Byb3AocHJvcCkpIHtcbiAgICAgIHJ1bnRpbWUucmVhY2hhYmxlID0gdW5kZWZpbmVkXG4gICAgICByZWFjaGFibGUgPSBwbGFjZW1lbnRSZWFjaGFiaWxpdHkoZmxvb3IsIHJ1bnRpbWUpXG4gICAgfVxuICAgIGlmIChsaW5rc1tkZWZpbml0aW9uLmlkXSkgYW5jaG9ycy5zZXQoZGVmaW5pdGlvbi5pZCwgcG9pbnQpXG4gIH1cbn1cblxuZnVuY3Rpb24gcGxhY2VNaWxlc3RvbmVzKGZsb29yOiBGbG9vciwgcnVudGltZTogUGxhY2VtZW50UnVudGltZSk6IHZvaWQge1xuICBjb25zdCBzcGVjcyA9IFtcbiAgICB7IGlkOiAnd2F5Y2FjaGUnLCBraW5kOiAnd2F5Y2FjaGUnIGFzIGNvbnN0LCByZXdhcmRLZXk6ICd3YXljYWNoZScgYXMgY29uc3QsIHByaW1hcnk6IHsgbm9kZUtpbmRzOiBbJ2xhbmRtYXJrJ10gYXMgUm91dGVOb2RlS2luZFtdLCBtaW5EaXN0YW5jZTogNSB9LCBsZWdhY3k6IHsgbWluRGlzdGFuY2U6IDUsIG1heERpc3RhbmNlOiAxOCB9IH0sXG4gICAgeyBpZDogJ2Jvb24tdGVhY2gnLCBraW5kOiAnYm9vbicgYXMgY29uc3QsIHJld2FyZEtleTogJ2Jvb24tdGVhY2gnIGFzIGNvbnN0LCBwcmltYXJ5OiB7IG5vZGVLaW5kczogWydmb3JrJ10gYXMgUm91dGVOb2RlS2luZFtdLCBtaW5EaXN0YW5jZTogNyB9LCBsZWdhY3k6IHsgbWluRGlzdGFuY2U6IDcsIG1heERpc3RhbmNlOiAyNCB9IH0sXG4gICAgeyBpZDogJ2Jvb24tdGVzdCcsIGtpbmQ6ICdib29uJyBhcyBjb25zdCwgcmV3YXJkS2V5OiAnYm9vbi10ZXN0JyBhcyBjb25zdCwgcHJpbWFyeTogeyBlZGdlTW9kZXM6IFsnY29zdGx5J10gYXMgUm91dGVFZGdlTW9kZVtdLCByb3V0ZUNvc3Q6ICdjb3N0bHknIGFzIGNvbnN0LCBtaW5EaXN0YW5jZTogOSB9LCBsZWdhY3k6IHsgbWluRGlzdGFuY2U6IDEwLCBjaG9rZXBvaW50OiBmYWxzZSB9IH0sXG4gICAgeyBpZDogJ2Jvb24tcGF5b2ZmJywga2luZDogJ2Jvb24nIGFzIGNvbnN0LCByZXdhcmRLZXk6ICdib29uLXBheW9mZicgYXMgY29uc3QsIHByaW1hcnk6IHsgbm9kZUtpbmRzOiBbJ29wdGlvbmFsUmV3YXJkJ10gYXMgUm91dGVOb2RlS2luZFtdLCBtaW5EaXN0YW5jZTogMTAgfSwgbGVnYWN5OiB7IG1pbkRpc3RhbmNlOiAxMiB9IH0sXG4gICAgeyBpZDogJ2F1Z21lbnQnLCBraW5kOiAnYXVnbWVudCcgYXMgY29uc3QsIHByaW1hcnk6IHsgbm9kZUtpbmRzOiBbJ29iamVjdGl2ZSddIGFzIFJvdXRlTm9kZUtpbmRbXSwgbWluRGlzdGFuY2U6IDEyIH0sIGxlZ2FjeTogeyBtaW5EaXN0YW5jZTogMTQgfSB9XG4gIF1cbiAgZmxvb3IubWlsZXN0b25lcyA9IFtdXG4gIGZvciAoY29uc3Qgc3BlYyBvZiBzcGVjcykge1xuICAgIGNvbnN0IGNvbnRyYWN0OiBQbGFjZW1lbnRDb250cmFjdCA9IHsgaWQ6IGBtaWxlc3RvbmU6JHtzcGVjLmlkfWAsIHJlcXVpcmVtZW50czogcnVudGltZS5waWxvdCA/IHNwZWMucHJpbWFyeSA6IHNwZWMubGVnYWN5LCAuLi4ocnVudGltZS5waWxvdCA/IHsgZmFsbGJhY2s6IHNwZWMubGVnYWN5IH0gOiB7fSkgfVxuICAgIGNvbnN0IHBvaW50ID0gY2hvb3NlUGxhY2VtZW50KGZsb29yLCBydW50aW1lLCBjb250cmFjdCwgY2FuZGlkYXRlID0+IChjYW5kaWRhdGUueCAhPT0gZmxvb3IuZXhpdC54IHx8IGNhbmRpZGF0ZS55ICE9PSBmbG9vci5leGl0LnkpICYmIChmbG9vci5iaW9tZSAhPT0gJ2NsaWZmcycgfHwgc3BlYy5raW5kICE9PSAnYXVnbWVudCcgfHwgZmxvb3IudGlsZXMuZXZlcnkoKHRpbGUsIGluZGV4KSA9PiB0aWxlLmtpbmQgIT09ICdhbHRhcicgfHwgTWF0aC5tYXgoTWF0aC5hYnMoY2FuZGlkYXRlLnggLSBpbmRleCAlIGZsb29yLndpZHRoKSwgTWF0aC5hYnMoY2FuZGlkYXRlLnkgLSBNYXRoLmZsb29yKGluZGV4IC8gZmxvb3Iud2lkdGgpKSkgPiAyKSkpXG4gICAgaWYgKCFwb2ludCkgdGhyb3cgbmV3IEVycm9yKGBmYWlsZWQgcGxhY2VtZW50ICR7Y29udHJhY3QuaWR9OiAke3J1bnRpbWUuZGlhZ25vc3RpY3MuYXQoLTEpPy5kaWFnbm9zdGljcy5qb2luKCc7ICcpfWApXG4gICAgZmxvb3IubWlsZXN0b25lcy5wdXNoKHsgaWQ6IGBtaWxlc3RvbmU6JHtmbG9vci5pbmRleH06JHtzcGVjLmlkfToke3BvaW50Lnh9OiR7cG9pbnQueX1gLCBraW5kOiBzcGVjLmtpbmQsIC4uLihzcGVjLnJld2FyZEtleSA/IHsgcmV3YXJkS2V5OiBzcGVjLnJld2FyZEtleSB9IDoge30pLCAuLi5wb2ludCwgZGlzY292ZXJlZDogZmFsc2UsIGNsYWltZWQ6IGZhbHNlIH0pXG4gIH1cbn1cblxuZnVuY3Rpb24gcGxhY2VFbmNvdW50ZXJzKGZsb29yOiBGbG9vciwgcm5nOiBSbmcsIHJ1bnRpbWU6IFBsYWNlbWVudFJ1bnRpbWUpOiB2b2lkIHtcbiAgY29uc3QgY29udHJhY3Q6IFBsYWNlbWVudENvbnRyYWN0ID0gcnVudGltZS5waWxvdFxuICAgID8geyBpZDogJ2VuY291bnRlcjpndWFyZGVkLXNocmluZScsIHJlcXVpcmVtZW50czogeyBlZGdlTW9kZXM6IFsnY29zdGx5J10sIHJvdXRlQ29zdDogJ2Nvc3RseScsIG1pbkRpc3RhbmNlOiA4LCBjb3ZlcjogdHJ1ZSB9LCBmYWxsYmFjazogeyBtaW5EaXN0YW5jZTogOCwgdGVycmFpbjogWydmbG9vciddIH0gfVxuICAgIDogeyBpZDogJ2VuY291bnRlcjpndWFyZGVkLXNocmluZScsIHJlcXVpcmVtZW50czogeyBtaW5EaXN0YW5jZTogOCwgdGVycmFpbjogWydmbG9vciddIH0gfVxuICBjb25zdCBwb2ludCA9IGNob29zZVBsYWNlbWVudChmbG9vciwgcnVudGltZSwgY29udHJhY3QsIGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUueCAhPT0gZmxvb3IuZXhpdC54IHx8IGNhbmRpZGF0ZS55ICE9PSBmbG9vci5leGl0LnkpXG4gIGlmICghcG9pbnQpIHJldHVyblxuICBjb25zdCBhbGlnbmVkOiBSZWNvcmQ8QmlvbWUsIHJlYWRvbmx5IEZsb29yRW5jb3VudGVyWydraW5kJ11bXT4gPSB7XG4gICAgbWluZTogWydtaW5lUGFjdCcsICdtaW5lS2FtaSddLCB3aWxkczogWyd3aWxkc1BhY3QnLCAnd2lsZHNLYW1pJ10sIGNhdmVybnM6IFsnY2F2ZXJuc1BhY3QnLCAnY2F2ZXJuc0thbWknXSwgcnVpbnM6IFsncnVpbnNQYWN0JywgJ3J1aW5zS2FtaSddLCBmdXJuYWNlOiBbJ2Z1cm5hY2VQYWN0JywgJ2Z1cm5hY2VLYW1pJ10sIGZsb29kZWRSdWluczogWydmbG9vZGVkUGFjdCcsICdmbG9vZGVkS2FtaSddLCBjbGlmZnM6IFsnY2xpZmZzUGFjdCcsICdjbGlmZnNLYW1pJ10sIGJ1cmlhbDogWydidXJpYWxQYWN0JywgJ2J1cmlhbEthbWknXSwgc2FsdEZsYXRzOiBbJ3NhbHRQYWN0JywgJ3NhbHRLYW1pJ10sIGZyb3N0UmVsaXF1YXJ5OiBbJ2Zyb3N0UGFjdCcsICdmcm9zdEthbWknXVxuICB9XG4gIGNvbnN0IGtpbmRzOiBGbG9vckVuY291bnRlclsna2luZCddW10gPSBmbG9vci5iaW9tZSA9PT0gJ2NsaWZmcycgPyBbJ3N0b3JtQ2FjaGUnLCAnd2luZFRyaWFsJywgJ2N1cnNlZE9iamVjdCcsIC4uLmFsaWduZWQuY2xpZmZzXVxuICAgIDogZmxvb3IuYmlvbWUgPT09ICdidXJpYWwnID8gWydhbmNlc3RvckRlYnQnLCAndG9tYkF1Y3Rpb24nLCAnY3Vyc2VkT2JqZWN0JywgLi4uYWxpZ25lZC5idXJpYWxdXG4gICAgICA6IGZsb29yLmJpb21lID09PSAnc2FsdEZsYXRzJyA/IFsnc3VuVHJpYnV0ZScsICdtaXJhZ2VNYXJrZXQnLCAnYnJpbmVPYXRoJywgJ2dsYXNzVHJpYWwnLCAnd2hpdGVSb2FkJywgJ3NhbHRDYWNoZScsIC4uLmFsaWduZWQuc2FsdEZsYXRzXVxuICAgICAgICA6IGZsb29yLmJpb21lID09PSAnZnJvc3RSZWxpcXVhcnknID8gWydpY2VEdWVsJywgJ3dpbnRlclRpdGhlJywgJ3JpbWVDb250cmFjdCcsICdmcm9zdENhY2hlJywgJ3doaXRlb3V0JywgJ3JlbGlxdWFyeVRyaWFsJywgLi4uYWxpZ25lZC5mcm9zdFJlbGlxdWFyeV1cbiAgICAgICAgICA6IFsnd2F5ZmFyZXInLCAnYmxvb2RCYXJnYWluJywgJ3NoaWZ0aW5nQ2hhbWJlcicsICdvYXRod2VsbCcsICdjdXJzZWRPYmplY3QnLCAuLi5hbGlnbmVkW2Zsb29yLmJpb21lXV1cbiAgY29uc3Qgc29jaWFsID0gc29jaWFsQ29udHJhY3RGb3IoeyBzZWVkOiBmbG9vci5zZWVkLCBmbG9vckluZGV4OiBmbG9vci5pbmRleCwgYmlvbWU6IGZsb29yLmJpb21lLCByZWNpcGVJZDogZmxvb3IubGF5b3V0SWQsIGFyY0lkOiBmbG9vci5lc2NhbGF0aW9uPy5hcmNJZCB9KVxuICBjb25zdCB0b29sT2ZmZXIgPSBvcHRpb25hbFRlcnJhaW5Ub29sRm9yKHsgc2VlZDogZmxvb3Iuc2VlZCwgZmxvb3JJbmRleDogZmxvb3IuaW5kZXgsIGJpb21lOiBmbG9vci5iaW9tZSwgcmVjaXBlSWQ6IGZsb29yLmxheW91dElkLCBzb3VyY2U6IHNvY2lhbCA/ICdzb2NpYWwnIDogJ2VuY291bnRlcicgfSlcbiAgZmxvb3IuZW5jb3VudGVycyA9IFt7IGlkOiBgZW5jb3VudGVyOiR7Zmxvb3IuaW5kZXh9OiR7cG9pbnQueH06JHtwb2ludC55fWAsIGtpbmQ6IHNvY2lhbCA/ICd3YXlmYXJlcicgOiBybmcucGljayhraW5kcyksIC4uLnBvaW50LCBzdGF0ZTogJ2Rvcm1hbnQnLCAuLi4oc29jaWFsID8geyBzb2NpYWwgfSA6IHt9KSwgLi4uKHRvb2xPZmZlciA/IHsgdG9vbE9mZmVyIH0gOiB7fSkgfV1cbn1cblxuY29uc3QgY2FydmVIID0gKGZsb29yOiBGbG9vciwgZnJvbTogbnVtYmVyLCB0bzogbnVtYmVyLCB5OiBudW1iZXIpID0+IHsgZm9yIChsZXQgeCA9IE1hdGgubWluKGZyb20sIHRvKTsgeCA8PSBNYXRoLm1heChmcm9tLCB0byk7IHgrKykgc2V0S2luZChmbG9vciwgeCwgeSwgJ2Zsb29yJykgfVxuY29uc3QgY2FydmVWID0gKGZsb29yOiBGbG9vciwgZnJvbTogbnVtYmVyLCB0bzogbnVtYmVyLCB4OiBudW1iZXIpID0+IHsgZm9yIChsZXQgeSA9IE1hdGgubWluKGZyb20sIHRvKTsgeSA8PSBNYXRoLm1heChmcm9tLCB0byk7IHkrKykgc2V0S2luZChmbG9vciwgeCwgeSwgJ2Zsb29yJykgfVxuY29uc3Qgc2V0S2luZCA9IChmbG9vcjogRmxvb3IsIHg6IG51bWJlciwgeTogbnVtYmVyLCBraW5kOiBUaWxlWydraW5kJ10pID0+IHtcbiAgaWYgKCFpbkJvdW5kcyhmbG9vciwgeCwgeSkpIHJldHVyblxuICBjb25zdCB0aWxlID0gZmxvb3IudGlsZXNbaW5kZXhPZihmbG9vciwgeCwgeSldXG4gIHRpbGUua2luZCA9IGtpbmRcbiAgaWYgKGtpbmQgIT09ICdjdXJyZW50JykgZGVsZXRlIHRpbGUuZmxvd1xufVxuY29uc3Qgc2FmZUZsb29yID0gKGZsb29yOiBGbG9vcik6IFBvaW50W10gPT4gZmxvb3IudGlsZXMuZmxhdE1hcCgoY3VycmVudCwgaW5kZXgpID0+IGN1cnJlbnQua2luZCA9PT0gJ2Zsb29yJyA/IFtwb2ludEF0KGZsb29yLCBpbmRleCldIDogW10pLmZpbHRlcihwb2ludCA9PiBkaXN0YW5jZShwb2ludCwgZmxvb3Iuc3RhcnQpID4gNSAmJiBkaXN0YW5jZShwb2ludCwgZmxvb3IuZXhpdCkgPiAzKVxuY29uc3QgdGVycmFpbkNvdW50ID0gKGZsb29yOiBGbG9vciwgY291bnQ6IG51bWJlcik6IG51bWJlciA9PiBNYXRoLm1heChjb3VudCwgTWF0aC5yb3VuZChjb3VudCAqIGZsb29yLnRpbGVzLmxlbmd0aCAvIChNQVBfV0lEVEggKiBNQVBfSEVJR0hUKSkpXG5jb25zdCByYWlsSCA9IChmbG9vcjogRmxvb3IsIGZyb206IG51bWJlciwgdG86IG51bWJlciwgeTogbnVtYmVyKSA9PiB7IGZvciAobGV0IHggPSBNYXRoLm1pbihmcm9tLCB0byk7IHggPD0gTWF0aC5tYXgoZnJvbSwgdG8pOyB4KyspIGlmIChnZXRUaWxlKGZsb29yLCB4LCB5KT8ua2luZCA9PT0gJ2Zsb29yJykgc2V0S2luZChmbG9vciwgeCwgeSwgJ3JhaWwnKSB9XG5jb25zdCByYWlsViA9IChmbG9vcjogRmxvb3IsIGZyb206IG51bWJlciwgdG86IG51bWJlciwgeDogbnVtYmVyKSA9PiB7IGZvciAobGV0IHkgPSBNYXRoLm1pbihmcm9tLCB0byk7IHkgPD0gTWF0aC5tYXgoZnJvbSwgdG8pOyB5KyspIGlmIChnZXRUaWxlKGZsb29yLCB4LCB5KT8ua2luZCA9PT0gJ2Zsb29yJykgc2V0S2luZChmbG9vciwgeCwgeSwgJ3JhaWwnKSB9XG5cbmZ1bmN0aW9uIGRlY29yYXRlQmlvbWUoZmxvb3I6IEZsb29yLCBybmc6IFJuZywgcm9vbXM6IFJvb21bXSk6IHZvaWQge1xuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdtaW5lJykgZGVjb3JhdGVNaW5lKGZsb29yLCBybmcsIHJvb21zKVxuICBpZiAoZmxvb3IuYmlvbWUgPT09ICd3aWxkcycpIGRlY29yYXRlV2lsZHMoZmxvb3IsIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnY2F2ZXJucycpIGRlY29yYXRlQ2F2ZXJucyhmbG9vciwgcm5nKVxuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdydWlucycpIGRlY29yYXRlUnVpbnMoZmxvb3IsIHJuZywgcm9vbXMpXG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2Z1cm5hY2UnKSBkZWNvcmF0ZUZ1cm5hY2UoZmxvb3IsIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnZmxvb2RlZFJ1aW5zJykgZGVjb3JhdGVGbG9vZGVkUnVpbnMoZmxvb3IsIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnY2xpZmZzJykgZGVjb3JhdGVDbGlmZnMoZmxvb3IsIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnYnVyaWFsJykgZGVjb3JhdGVCdXJpYWwoZmxvb3IsIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnc2FsdEZsYXRzJykgZGVjb3JhdGVTYWx0RmxhdHMoZmxvb3IsIHJuZylcbiAgaWYgKGZsb29yLmJpb21lID09PSAnZnJvc3RSZWxpcXVhcnknKSBkZWNvcmF0ZUZyb3N0UmVsaXF1YXJ5KGZsb29yLCBybmcpXG4gIGlmIChmbG9vci5pbmRleCAlIDQgPT09IDMpIHtcbiAgICBjb25zdCBjaGFtYmVyID0gcm9vbXNbcm9vbXMubGVuZ3RoIC0gMV1cbiAgICBmb3IgKGxldCB5ID0gY2hhbWJlci55OyB5IDwgY2hhbWJlci55ICsgY2hhbWJlci5oOyB5KyspIGZvciAobGV0IHggPSBjaGFtYmVyLng7IHggPCBjaGFtYmVyLnggKyBjaGFtYmVyLnc7IHgrKykgc2V0S2luZChmbG9vciwgeCwgeSwgJ2Zsb29yJylcbiAgICBzZXRLaW5kKGZsb29yLCBmbG9vci5leGl0LngsIGZsb29yLmV4aXQueSwgJ2V4aXQnKVxuICB9XG59XG5cbmZ1bmN0aW9uIHBsYWNlUHV6emxlVGVtcGxhdGUoZmxvb3I6IEZsb29yLCBybmc6IFJuZywgcm9vbXM6IFJvb21bXSk6IHZvaWQge1xuICBjb25zdCB0ZW1wbGF0ZXMgPSBwdXp6bGVUZW1wbGF0ZXNGb3IoZmxvb3IuYmlvbWUpXG4gIGlmICghdGVtcGxhdGVzLmxlbmd0aCkgcmV0dXJuXG4gIGNvbnN0IHRlbXBsYXRlID0gcm5nLnBpY2sodGVtcGxhdGVzKVxuICBjb25zdCByb29tID0gcm5nLnBpY2socm9vbXMuc2xpY2UoMSwgLTEpLmxlbmd0aCA/IHJvb21zLnNsaWNlKDEsIC0xKSA6IHJvb21zKVxuICBjb25zdCBwb2ludCA9IGNlbnRlcihyb29tKVxuICBmb3IgKGNvbnN0IHBsYWNlbWVudCBvZiB0ZW1wbGF0ZS5wbGFjZW1lbnRzKSB7XG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoZmxvb3IsIHBvaW50LnggKyBwbGFjZW1lbnQuZHgsIHBvaW50LnkgKyBwbGFjZW1lbnQuZHkpXG4gICAgaWYgKHRpbGUgJiYgdGlsZS5raW5kICE9PSAnd2FsbCcgJiYgdGlsZS5raW5kICE9PSAnZXhpdCcpIHsgdGlsZS5raW5kID0gcGxhY2VtZW50LmtpbmQ7IGlmIChwbGFjZW1lbnQua2luZCAhPT0gJ2N1cnJlbnQnKSBkZWxldGUgdGlsZS5mbG93IH1cbiAgfVxuICBmbG9vci5wdXp6bGVJZHMgPSBbLi4uKGZsb29yLnB1enpsZUlkcyA/PyBbXSksIHRlbXBsYXRlLmlkXVxufVxuXG5mdW5jdGlvbiBkZWNvcmF0ZU1pbmUoZmxvb3I6IEZsb29yLCBybmc6IFJuZywgcm9vbXM6IFJvb21bXSk6IHZvaWQge1xuICBmb3IgKGxldCBpID0gMTsgaSA8IHJvb21zLmxlbmd0aDsgaSsrKSB7XG4gICAgY29uc3QgZnJvbSA9IGNlbnRlcihyb29tc1tpIC0gMV0pXG4gICAgY29uc3QgdG8gPSBjZW50ZXIocm9vbXNbaV0pXG4gICAgaWYgKGkgJSAyKSB7IHJhaWxIKGZsb29yLCBmcm9tLngsIHRvLngsIGZyb20ueSk7IHJhaWxWKGZsb29yLCBmcm9tLnksIHRvLnksIHRvLngpIH1cbiAgICBlbHNlIHsgcmFpbFYoZmxvb3IsIGZyb20ueSwgdG8ueSwgZnJvbS54KTsgcmFpbEgoZmxvb3IsIGZyb20ueCwgdG8ueCwgdG8ueSkgfVxuICB9XG4gIGNvbnN0IHNhZmUgPSAoKSA9PiBzYWZlRmxvb3IoZmxvb3IpXG4gIGNvbnN0IHBhaW50ID0gKGtpbmQ6IFRpbGVbJ2tpbmQnXSwgY291bnQ6IG51bWJlciwgYnlSYWlsID0gZmFsc2UpID0+IHtcbiAgICBjb3VudCA9IHRlcnJhaW5Db3VudChmbG9vciwgY291bnQpXG4gICAgbGV0IGNhbmRpZGF0ZXMgPSBzYWZlKClcbiAgICBpZiAoYnlSYWlsKSB7XG4gICAgICBjb25zdCBhZGphY2VudCA9IGNhbmRpZGF0ZXMuZmlsdGVyKHBvaW50ID0+IFtbMCwgLTFdLCBbMSwgMF0sIFswLCAxXSwgWy0xLCAwXV0uc29tZSgoW3gsIHldKSA9PiBnZXRUaWxlKGZsb29yLCBwb2ludC54ICsgeCwgcG9pbnQueSArIHkpPy5raW5kID09PSAncmFpbCcpKVxuICAgICAgaWYgKGFkamFjZW50Lmxlbmd0aCkgY2FuZGlkYXRlcyA9IGFkamFjZW50XG4gICAgfVxuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgY291bnQgJiYgY2FuZGlkYXRlcy5sZW5ndGg7IGkrKykge1xuICAgICAgY29uc3QgcG9pbnQgPSBybmcucGljayhjYW5kaWRhdGVzKVxuICAgICAgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwga2luZClcbiAgICAgIGNhbmRpZGF0ZXMgPSBjYW5kaWRhdGVzLmZpbHRlcihjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLnggIT09IHBvaW50LnggfHwgY2FuZGlkYXRlLnkgIT09IHBvaW50LnkpXG4gICAgfVxuICB9XG4gIHBhaW50KCdzdXBwb3J0JywgOCwgdHJ1ZSlcbiAgcGFpbnQoJ2NydW1ibGUnLCAxMilcbiAgcGFpbnQoJ3J1YmJsZScsIDYpXG4gIHBhaW50KCdib3VsZGVyJywgNSlcbn1cblxuZnVuY3Rpb24gZGVjb3JhdGVXaWxkcyhmbG9vcjogRmxvb3IsIHJuZzogUm5nKTogdm9pZCB7XG4gIGNvbnN0IHNhZmUgPSAoKSA9PiBzYWZlRmxvb3IoZmxvb3IpXG4gIGNvbnN0IHBhaW50ID0gKGtpbmQ6IFRpbGVbJ2tpbmQnXSwgY291bnQ6IG51bWJlciwgY2x1c3RlcmVkID0gZmFsc2UpID0+IHtcbiAgICBjb3VudCA9IHRlcnJhaW5Db3VudChmbG9vciwgY291bnQpXG4gICAgbGV0IGNhbmRpZGF0ZXMgPSBzYWZlKClcbiAgICBmb3IgKGxldCBpID0gMDsgaSA8IGNvdW50ICYmIGNhbmRpZGF0ZXMubGVuZ3RoOyBpKyspIHtcbiAgICAgIGNvbnN0IHBvaW50ID0gcm5nLnBpY2soY2FuZGlkYXRlcylcbiAgICAgIHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksIGtpbmQpXG4gICAgICBpZiAoY2x1c3RlcmVkKSBmb3IgKGNvbnN0IFt4LCB5XSBvZiBbWzAsIC0xXSwgWzEsIDBdLCBbMCwgMV0sIFstMSwgMF1dKSBpZiAocm5nLmNoYW5jZSg0NSkgJiYgZ2V0VGlsZShmbG9vciwgcG9pbnQueCArIHgsIHBvaW50LnkgKyB5KT8ua2luZCA9PT0gJ2Zsb29yJykgc2V0S2luZChmbG9vciwgcG9pbnQueCArIHgsIHBvaW50LnkgKyB5LCBraW5kKVxuICAgICAgY2FuZGlkYXRlcyA9IHNhZmUoKVxuICAgIH1cbiAgfVxuICBwYWludCgnd2F0ZXInLCA5LCB0cnVlKVxuICBpZiAoZmxvb3IubGF5b3V0SWQuaW5jbHVkZXMoJ3JpdmVyLWNsZWFyaW5ncycpIHx8IGZsb29yLmxheW91dElkLmluY2x1ZGVzKCd3ZXRsYW5kJykpIGNhcnZlRmxvd0NoYW5uZWwoZmxvb3IsIHJuZywgZmFsc2UpXG4gIHBhaW50KCdicmFtYmxlJywgMTIsIHRydWUpXG4gIHBhaW50KCdib3VsZGVyJywgNywgdHJ1ZSlcbiAgcGFpbnQoJ3dlYicsIDEwKVxufVxuXG5mdW5jdGlvbiBkZWNvcmF0ZUNhdmVybnMoZmxvb3I6IEZsb29yLCBybmc6IFJuZyk6IHZvaWQge1xuICBjb25zdCBzYWZlID0gKCkgPT4gc2FmZUZsb29yKGZsb29yKVxuICBjb25zdCBwYWludCA9IChraW5kOiBUaWxlWydraW5kJ10sIGNvdW50OiBudW1iZXIsIGNsdXN0ZXJlZCA9IGZhbHNlKSA9PiB7XG4gICAgY291bnQgPSB0ZXJyYWluQ291bnQoZmxvb3IsIGNvdW50KVxuICAgIGxldCBjYW5kaWRhdGVzID0gc2FmZSgpXG4gICAgZm9yIChsZXQgaSA9IDA7IGkgPCBjb3VudCAmJiBjYW5kaWRhdGVzLmxlbmd0aDsgaSsrKSB7XG4gICAgICBjb25zdCBwb2ludCA9IHJuZy5waWNrKGNhbmRpZGF0ZXMpXG4gICAgICBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCBraW5kKVxuICAgICAgaWYgKGNsdXN0ZXJlZCkgZm9yIChjb25zdCBbeCwgeV0gb2YgW1swLCAtMV0sIFsxLCAwXSwgWzAsIDFdLCBbLTEsIDBdXSkgaWYgKHJuZy5jaGFuY2UoNDApICYmIGdldFRpbGUoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSk/LmtpbmQgPT09ICdmbG9vcicpIHNldEtpbmQoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSwga2luZClcbiAgICAgIGNhbmRpZGF0ZXMgPSBzYWZlKClcbiAgICB9XG4gIH1cbiAgY2FydmVGbG93Q2hhbm5lbChmbG9vciwgcm5nLCBmYWxzZSlcbiAgcGFpbnQoJ3dhdGVyJywgMTEsIHRydWUpXG4gIHBhaW50KCdkZWVwV2F0ZXInLCA3LCB0cnVlKVxuICBwYWludCgnY3J1bWJsZScsIDYpXG4gIHBhaW50KCdkYXJrbmVzcycsIDExLCB0cnVlKVxufVxuXG5mdW5jdGlvbiBkZWNvcmF0ZVJ1aW5zKGZsb29yOiBGbG9vciwgcm5nOiBSbmcsIHJvb21zOiBSb29tW10pOiB2b2lkIHtcbiAgY29uc3Qgc2FmZSA9ICgpID0+IHNhZmVGbG9vcihmbG9vcilcbiAgY29uc3QgcGFpbnQgPSAoa2luZDogVGlsZVsna2luZCddLCBjb3VudDogbnVtYmVyKSA9PiB7XG4gICAgY291bnQgPSB0ZXJyYWluQ291bnQoZmxvb3IsIGNvdW50KVxuICAgIGxldCBjYW5kaWRhdGVzID0gc2FmZSgpXG4gICAgZm9yIChsZXQgaSA9IDA7IGkgPCBjb3VudCAmJiBjYW5kaWRhdGVzLmxlbmd0aDsgaSsrKSB7XG4gICAgICBjb25zdCBwb2ludCA9IHJuZy5waWNrKGNhbmRpZGF0ZXMpXG4gICAgICBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCBraW5kKVxuICAgICAgY2FuZGlkYXRlcyA9IHNhZmUoKVxuICAgIH1cbiAgfVxuICBwYWludCgnZGFydCcsIDEyKVxuICBwYWludCgnY3J1bWJsZScsIDEwKVxuICBwYWludCgnYm91bGRlcicsIDUpXG4gIGNvbnN0IHJpdHVhbFJvb20gPSByb29tcy5sZW5ndGggPiAyID8gcm9vbXNbcm9vbXMubGVuZ3RoIC0gMl0gOiB1bmRlZmluZWRcbiAgaWYgKCFyaXR1YWxSb29tKSByZXR1cm5cbiAgY29uc3QgYWx0YXIgPSBjZW50ZXIocml0dWFsUm9vbSlcbiAgZm9yIChsZXQgeSA9IGFsdGFyLnkgLSAxOyB5IDw9IGFsdGFyLnkgKyAxOyB5KyspIGZvciAobGV0IHggPSBhbHRhci54IC0gMTsgeCA8PSBhbHRhci54ICsgMTsgeCsrKSBpZiAoZ2V0VGlsZShmbG9vciwgeCwgeSk/LmtpbmQgIT09ICd3YWxsJykgc2V0S2luZChmbG9vciwgeCwgeSwgJ2Zsb29yJylcbiAgc2V0S2luZChmbG9vciwgYWx0YXIueCwgYWx0YXIueSwgJ2FsdGFyJylcbn1cblxuZnVuY3Rpb24gZGVjb3JhdGVGdXJuYWNlKGZsb29yOiBGbG9vciwgcm5nOiBSbmcpOiB2b2lkIHtcbiAgY29uc3Qgc2FmZSA9ICgpID0+IHNhZmVGbG9vcihmbG9vcilcbiAgY29uc3QgcGFpbnQgPSAoa2luZDogVGlsZVsna2luZCddLCBjb3VudDogbnVtYmVyLCBjbHVzdGVyZWQgPSBmYWxzZSkgPT4ge1xuICAgIGNvdW50ID0gdGVycmFpbkNvdW50KGZsb29yLCBjb3VudClcbiAgICBsZXQgY2FuZGlkYXRlcyA9IHNhZmUoKVxuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgY291bnQgJiYgY2FuZGlkYXRlcy5sZW5ndGg7IGkrKykge1xuICAgICAgY29uc3QgcG9pbnQgPSBybmcucGljayhjYW5kaWRhdGVzKVxuICAgICAgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwga2luZClcbiAgICAgIGlmIChjbHVzdGVyZWQpIGZvciAoY29uc3QgW3gsIHldIG9mIGNhcmRpbmFsT2Zmc2V0cykgaWYgKHJuZy5jaGFuY2UoMzUpICYmIGdldFRpbGUoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSk/LmtpbmQgPT09ICdmbG9vcicpIHNldEtpbmQoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSwga2luZClcbiAgICAgIGNhbmRpZGF0ZXMgPSBzYWZlKClcbiAgICB9XG4gIH1cbiAgcGFpbnQoJ3Ntb2tlJywgMTQsIHRydWUpXG4gIHBhaW50KCdsaWZ0JywgNylcbiAgcGFpbnQoJ2JyZWFrd2FsbCcsIDgpXG4gIHBhaW50KCdmaXJlVmVudCcsIDkpXG59XG5cbmZ1bmN0aW9uIGRlY29yYXRlRmxvb2RlZFJ1aW5zKGZsb29yOiBGbG9vciwgcm5nOiBSbmcpOiB2b2lkIHtcbiAgY29uc3Qgc2FmZSA9ICgpID0+IHNhZmVGbG9vcihmbG9vcilcbiAgY29uc3QgcGFpbnQgPSAoa2luZDogVGlsZVsna2luZCddLCBjb3VudDogbnVtYmVyLCBjbHVzdGVyZWQgPSBmYWxzZSkgPT4ge1xuICAgIGNvdW50ID0gdGVycmFpbkNvdW50KGZsb29yLCBjb3VudClcbiAgICBsZXQgY2FuZGlkYXRlcyA9IHNhZmUoKVxuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgY291bnQgJiYgY2FuZGlkYXRlcy5sZW5ndGg7IGkrKykge1xuICAgICAgY29uc3QgcG9pbnQgPSBybmcucGljayhjYW5kaWRhdGVzKVxuICAgICAgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwga2luZClcbiAgICAgIGlmIChjbHVzdGVyZWQpIGZvciAoY29uc3QgW3gsIHldIG9mIGNhcmRpbmFsT2Zmc2V0cykgaWYgKHJuZy5jaGFuY2UoMzUpICYmIGdldFRpbGUoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSk/LmtpbmQgPT09ICdmbG9vcicpIHNldEtpbmQoZmxvb3IsIHBvaW50LnggKyB4LCBwb2ludC55ICsgeSwga2luZClcbiAgICAgIGNhbmRpZGF0ZXMgPSBzYWZlKClcbiAgICB9XG4gIH1cbiAgaWYgKCFbJ2JyYWlkZWQtY3VycmVudC1kZWx0YScsICdhbmNob3ItZ2F0ZWQtcnVpbicsICdpc2xhbmQtaG9wLW5ldHdvcmsnXS5zb21lKHJlY2lwZSA9PiBmbG9vci5sYXlvdXRJZC5pbmNsdWRlcyhyZWNpcGUpKSkgY2FydmVGbG93Q2hhbm5lbChmbG9vciwgcm5nLCBmbG9vci5sYXlvdXRJZC5pbmNsdWRlcygnZmxvb2RnYXRlJykpXG4gIHBhaW50KCdhbmNob3InLCA3KVxuICBwYWludCgnZGVlcFdhdGVyJywgOSwgdHJ1ZSlcbiAgcGFpbnQoJ3dhdGVyJywgOCwgdHJ1ZSlcbn1cblxuY29uc3QgY2FydmVGbG93Q2hhbm5lbCA9IChmbG9vcjogRmxvb3IsIHJuZzogUm5nLCBoYXphcmRvdXM6IGJvb2xlYW4pOiB2b2lkID0+IHtcbiAgY29uc3QgdmVydGljYWwgPSBybmcuY2hhbmNlKDU1KVxuICBjb25zdCBzdGFydCA9IHZlcnRpY2FsID8gcm5nLmludCg0LCBmbG9vci53aWR0aCAtIDUpIDogcm5nLmludCg0LCBmbG9vci5oZWlnaHQgLSA1KVxuICBjb25zdCBkaXJlY3Rpb246IEV4Y2x1ZGU8RGlyZWN0aW9uLCAnd2FpdCc+ID0gdmVydGljYWwgPyAncycgOiAnZSdcbiAgY29uc3QgbGVuZ3RoID0gdmVydGljYWwgPyBmbG9vci5oZWlnaHQgLSAzIDogZmxvb3Iud2lkdGggLSAzXG4gIGxldCBiZW5kID0gc3RhcnRcbiAgbGV0IGxhc3Q6IFBvaW50IHwgdW5kZWZpbmVkXG4gIGZvciAobGV0IHN0ZXAgPSAxOyBzdGVwIDwgbGVuZ3RoOyBzdGVwKyspIHtcbiAgICBpZiAoc3RlcCAlIDggPT09IDAgJiYgcm5nLmNoYW5jZSg1NSkpIGJlbmQgKz0gcm5nLmludCgtMSwgMSlcbiAgICBjb25zdCB4ID0gdmVydGljYWwgPyBiZW5kIDogc3RlcFxuICAgIGNvbnN0IHkgPSB2ZXJ0aWNhbCA/IHN0ZXAgOiBiZW5kXG4gICAgaWYgKCFpbkJvdW5kcyhmbG9vciwgeCwgeSkgfHwgeCA8IDIgfHwgeSA8IDIgfHwgeCA+PSBmbG9vci53aWR0aCAtIDIgfHwgeSA+PSBmbG9vci5oZWlnaHQgLSAyKSBjb250aW51ZVxuICAgIGNvbnN0IGN1cnJlbnQgPSBnZXRUaWxlKGZsb29yLCB4LCB5KVxuICAgIGlmICghY3VycmVudCB8fCBjdXJyZW50LmtpbmQgPT09ICdleGl0JyB8fCAoeCA9PT0gZmxvb3Iuc3RhcnQueCAmJiB5ID09PSBmbG9vci5zdGFydC55KSkgY29udGludWVcbiAgICBpZiAobGFzdCkge1xuICAgICAgY29uc3QgcHJldmlvdXMgPSBnZXRUaWxlKGZsb29yLCBsYXN0LngsIGxhc3QueSlcbiAgICAgIGlmIChwcmV2aW91cz8uZmxvdykgcHJldmlvdXMuZmxvdy5kaXJlY3Rpb24gPSBmbG93RGlyZWN0aW9uKHggLSBsYXN0LngsIHkgLSBsYXN0LnkpXG4gICAgfVxuICAgIGN1cnJlbnQua2luZCA9ICdjdXJyZW50J1xuICAgIGN1cnJlbnQuZmxvdyA9IHsgZGlyZWN0aW9uLCAuLi4oaGF6YXJkb3VzICYmIHN0ZXAgPiBsZW5ndGggLSA3ID8geyBoYXphcmQ6ICd1bmRlcnRvdycgYXMgY29uc3QgfSA6IHt9KSB9XG4gICAgbGFzdCA9IHsgeCwgeSB9XG4gICAgY29uc3QgYmFuayA9IHZlcnRpY2FsID8geyB4OiB4ICsgMSwgeSB9IDogeyB4LCB5OiB5ICsgMSB9XG4gICAgaWYgKGdldFRpbGUoZmxvb3IsIGJhbmsueCwgYmFuay55KT8ua2luZCA9PT0gJ2Zsb29yJyAmJiBybmcuY2hhbmNlKDQ1KSkgc2V0S2luZChmbG9vciwgYmFuay54LCBiYW5rLnksICd3YXRlcicpXG4gIH1cbiAgaWYgKGhhemFyZG91cyAmJiBsYXN0KSB7XG4gICAgY29uc3QgZGVsdGEgPSAoeyBuOiB7IHg6IDAsIHk6IC0xIH0sIG5lOiB7IHg6IDEsIHk6IC0xIH0sIGU6IHsgeDogMSwgeTogMCB9LCBzZTogeyB4OiAxLCB5OiAxIH0sIHM6IHsgeDogMCwgeTogMSB9LCBzdzogeyB4OiAtMSwgeTogMSB9LCB3OiB7IHg6IC0xLCB5OiAwIH0sIG53OiB7IHg6IC0xLCB5OiAtMSB9IH0gYXMgY29uc3QpW2dldFRpbGUoZmxvb3IsIGxhc3QueCwgbGFzdC55KT8uZmxvdz8uZGlyZWN0aW9uID8/IGRpcmVjdGlvbl1cbiAgICBjb25zdCBvdXRsZXQgPSBnZXRUaWxlKGZsb29yLCBsYXN0LnggKyBkZWx0YS54LCBsYXN0LnkgKyBkZWx0YS55KVxuICAgIGlmIChvdXRsZXQgJiYgb3V0bGV0LmtpbmQgIT09ICdleGl0Jykgb3V0bGV0LmtpbmQgPSAnZGVlcFdhdGVyJ1xuICB9XG59XG5cbmNvbnN0IGZsb3dEaXJlY3Rpb24gPSAoeDogbnVtYmVyLCB5OiBudW1iZXIpOiBFeGNsdWRlPERpcmVjdGlvbiwgJ3dhaXQnPiA9PiB4IDwgMCA/IHkgPCAwID8gJ253JyA6IHkgPiAwID8gJ3N3JyA6ICd3JyA6IHggPiAwID8geSA8IDAgPyAnbmUnIDogeSA+IDAgPyAnc2UnIDogJ2UnIDogeSA8IDAgPyAnbicgOiAncydcblxuZnVuY3Rpb24gZGVjb3JhdGVDbGlmZnMoZmxvb3I6IEZsb29yLCBybmc6IFJuZyk6IHZvaWQge1xuICBpZiAoWydzd2l0Y2hiYWNrLWZhY2UnLCAncmF2aW5lLWJyaWRnZS1sb29wJywgJ2FuY2hvci1jaGFpbiddLnNvbWUodmFyaWFudCA9PiBmbG9vci5sYXlvdXRJZCA9PT0gdmFyaWFudCB8fCBmbG9vci5sYXlvdXRJZCA9PT0gYCR7dmFyaWFudH0tcmVtaXhgKSkgcmV0dXJuXG4gIGNvbnN0IHNhZmUgPSAoKSA9PiBzYWZlRmxvb3IoZmxvb3IpXG4gIGNvbnN0IHBhaW50ID0gKGtpbmQ6IFRpbGVbJ2tpbmQnXSwgY291bnQ6IG51bWJlcikgPT4ge1xuICAgIGNvdW50ID0gdGVycmFpbkNvdW50KGZsb29yLCBjb3VudClcbiAgICBsZXQgY2FuZGlkYXRlcyA9IHNhZmUoKVxuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgY291bnQgJiYgY2FuZGlkYXRlcy5sZW5ndGg7IGkrKykge1xuICAgICAgY29uc3QgcG9pbnQgPSBybmcucGljayhjYW5kaWRhdGVzKVxuICAgICAgc2V0S2luZChmbG9vciwgcG9pbnQueCwgcG9pbnQueSwga2luZClcbiAgICAgIGNhbmRpZGF0ZXMgPSBzYWZlKClcbiAgICB9XG4gIH1cbiAgcGFpbnQoJ2xlZGdlJywgMTIpXG4gIHBhaW50KCdjbGlmZldhbGwnLCAxMClcbiAgcGFpbnQoJ3JvcGUnLCA0KVxuICBjb25zdCBsb3dlciA9IHNhZmUoKVxuICBpZiAoIWxvd2VyLmxlbmd0aCkgcmV0dXJuXG4gIGNvbnN0IGZyb20gPSBybmcucGljayhsb3dlcilcbiAgY29uc3QgZmFyID0gbG93ZXIuZmlsdGVyKHBvaW50ID0+IGRpc3RhbmNlKHBvaW50LCBmcm9tKSA+IDgpXG4gIGNvbnN0IHRvID0gcm5nLnBpY2soZmFyLmxlbmd0aCA/IGZhciA6IGxvd2VyKVxuICBnZXRUaWxlKGZsb29yLCBmcm9tLngsIGZyb20ueSkhLmVsZXZhdGlvbiA9IDBcbiAgZ2V0VGlsZShmbG9vciwgdG8ueCwgdG8ueSkhLmVsZXZhdGlvbiA9IDFcbiAgZmxvb3IuY2xpbWJMaW5rcyA9IFt7IGlkOiBgY2xpbWI6JHtmbG9vci5pbmRleH06JHtmcm9tLnh9OiR7ZnJvbS55fToke3RvLnh9OiR7dG8ueX1gLCBsb3dlcjogZnJvbSwgdXBwZXI6IHRvLCBhbmNob3JlZDogZmFsc2UgfV1cbn1cblxuZnVuY3Rpb24gZGVjb3JhdGVCdXJpYWwoZmxvb3I6IEZsb29yLCBybmc6IFJuZyk6IHZvaWQge1xuICBpZiAoWydzdG9uZS1jaXJjbGUtY2VudGVyJywgJ21vdW5kLXByb2Nlc3Npb24nLCAnY2VtZXRlcnktc2V0dGxlbWVudC1lZGdlJywgJ29zc3VhcnktaG9sbG93JywgJ2FuY2VzdG9yLXBhdGgtbG9vcCddLnNvbWUodmFyaWFudCA9PiBmbG9vci5sYXlvdXRJZCA9PT0gdmFyaWFudCB8fCBmbG9vci5sYXlvdXRJZCA9PT0gYCR7dmFyaWFudH0tcmVtaXhgKSkgcmV0dXJuXG4gIGNvbnN0IHNhZmUgPSAoKSA9PiBzYWZlRmxvb3IoZmxvb3IpXG4gIGNvbnN0IHBhaW50ID0gKGtpbmQ6IFRpbGVbJ2tpbmQnXSwgY291bnQ6IG51bWJlciwgY2x1c3RlcmVkID0gZmFsc2UpID0+IHtcbiAgICBjb3VudCA9IHRlcnJhaW5Db3VudChmbG9vciwgY291bnQpXG4gICAgbGV0IGNhbmRpZGF0ZXMgPSBzYWZlKClcbiAgICBmb3IgKGxldCBpID0gMDsgaSA8IGNvdW50ICYmIGNhbmRpZGF0ZXMubGVuZ3RoOyBpKyspIHtcbiAgICAgIGNvbnN0IHBvaW50ID0gcm5nLnBpY2soY2FuZGlkYXRlcylcbiAgICAgIHNldEtpbmQoZmxvb3IsIHBvaW50LngsIHBvaW50LnksIGtpbmQpXG4gICAgICBpZiAoY2x1c3RlcmVkKSBmb3IgKGNvbnN0IFt4LCB5XSBvZiBjYXJkaW5hbE9mZnNldHMpIGlmIChybmcuY2hhbmNlKDM1KSAmJiBnZXRUaWxlKGZsb29yLCBwb2ludC54ICsgeCwgcG9pbnQueSArIHkpPy5raW5kID09PSAnZmxvb3InKSBzZXRLaW5kKGZsb29yLCBwb2ludC54ICsgeCwgcG9pbnQueSArIHksIGtpbmQpXG4gICAgICBjYW5kaWRhdGVzID0gc2FmZSgpXG4gICAgfVxuICB9XG4gIHBhaW50KCdncmF2ZVNvaWwnLCAxMywgdHJ1ZSlcbiAgcGFpbnQoJ2NhaXJuJywgOClcbiAgcGFpbnQoJ29zc3VhcnknLCA3KVxuICBwYWludCgnc3Bpcml0UGF0aCcsIDEwLCB0cnVlKVxufVxuXG5mdW5jdGlvbiBkZWNvcmF0ZVNhbHRGbGF0cyhmbG9vcjogRmxvb3IsIHJuZzogUm5nKTogdm9pZCB7XG4gIGlmIChbJ2NydXN0LWlzbGFuZC1jaGFpbicsICdicmluZS1tYXplJywgJ2NhcmF2YW4tY2F1c2V3YXknLCAnbWlycm9yLWJhc2luLWxvb3AnLCAnc2FsdC1yaWRnZS1yZWZ1Z2UnXS5zb21lKHZhcmlhbnQgPT4gZmxvb3IubGF5b3V0SWQgPT09IHZhcmlhbnQgfHwgZmxvb3IubGF5b3V0SWQgPT09IGAke3ZhcmlhbnR9LXJlbWl4YCkpIHJldHVyblxuICBjb25zdCBzYWZlID0gKCkgPT4gc2FmZUZsb29yKGZsb29yKVxuICBjb25zdCBwYWludCA9IChraW5kOiBUaWxlWydraW5kJ10sIGNvdW50OiBudW1iZXIsIGNsdXN0ZXJlZCA9IGZhbHNlKSA9PiB7XG4gICAgY291bnQgPSB0ZXJyYWluQ291bnQoZmxvb3IsIGNvdW50KVxuICAgIGxldCBjYW5kaWRhdGVzID0gc2FmZSgpXG4gICAgZm9yIChsZXQgaSA9IDA7IGkgPCBjb3VudCAmJiBjYW5kaWRhdGVzLmxlbmd0aDsgaSsrKSB7XG4gICAgICBjb25zdCBwb2ludCA9IHJuZy5waWNrKGNhbmRpZGF0ZXMpXG4gICAgICBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCBraW5kKVxuICAgICAgaWYgKGNsdXN0ZXJlZCkgZm9yIChjb25zdCBbeCwgeV0gb2YgY2FyZGluYWxPZmZzZXRzKSBpZiAocm5nLmNoYW5jZSgzNSkgJiYgZ2V0VGlsZShmbG9vciwgcG9pbnQueCArIHgsIHBvaW50LnkgKyB5KT8ua2luZCA9PT0gJ2Zsb29yJykgc2V0S2luZChmbG9vciwgcG9pbnQueCArIHgsIHBvaW50LnkgKyB5LCBraW5kKVxuICAgICAgY2FuZGlkYXRlcyA9IHNhZmUoKVxuICAgIH1cbiAgfVxuICBwYWludCgnc2FsdE1pcnJvcicsIDE0LCB0cnVlKVxuICBwYWludCgnYnJpbmUnLCA4LCB0cnVlKVxuICBwYWludCgnY3J1bWJsZScsIDYpXG59XG5cbmZ1bmN0aW9uIGRlY29yYXRlRnJvc3RSZWxpcXVhcnkoZmxvb3I6IEZsb29yLCBybmc6IFJuZyk6IHZvaWQge1xuICBpZiAoWydmcm96ZW4tbGFrZS1jcm9zc2luZycsICdyaWRnZS1ob2xsb3ctbG9vcCcsICdwcmVzc3VyZS1jcmFjay1tYXplJywgJ3Nob3JlLXJlbGlxdWFyeS1yb3V0ZScsICdzdG9ybS1yZWZ1Z2UtY2hhaW4nXS5zb21lKHZhcmlhbnQgPT4gZmxvb3IubGF5b3V0SWQgPT09IHZhcmlhbnQgfHwgZmxvb3IubGF5b3V0SWQgPT09IGAke3ZhcmlhbnR9LXJlbWl4YCkpIHJldHVyblxuICBjb25zdCBzYWZlID0gKCkgPT4gc2FmZUZsb29yKGZsb29yKVxuICBjb25zdCBwYWludCA9IChraW5kOiBUaWxlWydraW5kJ10sIGNvdW50OiBudW1iZXIsIGNsdXN0ZXJlZCA9IGZhbHNlKSA9PiB7XG4gICAgY291bnQgPSB0ZXJyYWluQ291bnQoZmxvb3IsIGNvdW50KVxuICAgIGxldCBjYW5kaWRhdGVzID0gc2FmZSgpXG4gICAgZm9yIChsZXQgaSA9IDA7IGkgPCBjb3VudCAmJiBjYW5kaWRhdGVzLmxlbmd0aDsgaSsrKSB7XG4gICAgICBjb25zdCBwb2ludCA9IHJuZy5waWNrKGNhbmRpZGF0ZXMpXG4gICAgICBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCBraW5kKVxuICAgICAgaWYgKGNsdXN0ZXJlZCkgZm9yIChjb25zdCBbeCwgeV0gb2YgY2FyZGluYWxPZmZzZXRzKSBpZiAocm5nLmNoYW5jZSgzNSkgJiYgZ2V0VGlsZShmbG9vciwgcG9pbnQueCArIHgsIHBvaW50LnkgKyB5KT8ua2luZCA9PT0gJ2Zsb29yJykgc2V0S2luZChmbG9vciwgcG9pbnQueCArIHgsIHBvaW50LnkgKyB5LCBraW5kKVxuICAgICAgY2FuZGlkYXRlcyA9IHNhZmUoKVxuICAgIH1cbiAgfVxuICBwYWludCgnaWNlJywgMTUsIHRydWUpXG4gIHBhaW50KCdmcm9zdFJpbWUnLCA5LCB0cnVlKVxuICBwYWludCgnYm91bGRlcicsIDQpXG59XG5cbmZ1bmN0aW9uIHBsYWNlRXZlbnRzKGZsb29yOiBGbG9vciwgcm9vbXM6IFJvb21bXSwgcnVudGltZTogUGxhY2VtZW50UnVudGltZSk6IHZvaWQge1xuICBjb25zdCBldmVudFJvb20gPSByb29tc1tmbG9vci5iaW9tZSA9PT0gJ3J1aW5zJyAmJiByb29tcy5sZW5ndGggPiAyID8gMSA6IE1hdGgubWF4KDEsIE1hdGguZmxvb3Iocm9vbXMubGVuZ3RoIC8gMikpXVxuICBjb25zdCBub2RlS2luZHM6IFJvdXRlTm9kZUtpbmRbXSA9IGZsb29yLmJpb21lID09PSAncnVpbnMnID8gZmxvb3IuaW5kZXggJSA0ID09PSAwID8gWydsYW5kbWFyayddIDogZmxvb3IuaW5kZXggJSA0ID09PSAxID8gWydmb3JrJ10gOiBmbG9vci5pbmRleCAlIDQgPT09IDIgPyBbJ29wdGlvbmFsUmV3YXJkJ10gOiBbJ2xhbmRtYXJrJ10gOiBmbG9vci5pbmRleCAlIDQgPT09IDAgPyBbJ2xhbmRtYXJrJ10gOiBmbG9vci5pbmRleCAlIDQgPT09IDEgPyBbJ2ZvcmsnXSA6IFsnb2JqZWN0aXZlJ11cbiAgY29uc3Qgbm9kZSA9IHJ1bnRpbWUucGlsb3QgPyBydW50aW1lLm1hY3JvLm5vZGVzLmZpbmQoY2FuZGlkYXRlID0+IG5vZGVLaW5kcy5pbmNsdWRlcyhjYW5kaWRhdGUua2luZCkpIDogdW5kZWZpbmVkXG4gIGNvbnN0IGZhbGxiYWNrID0gbm9kZSA/IHsgeDogbm9kZS5mb290cHJpbnQueCArIE1hdGguZmxvb3Iobm9kZS5mb290cHJpbnQud2lkdGggLyAyKSwgeTogbm9kZS5mb290cHJpbnQueSArIE1hdGguZmxvb3Iobm9kZS5mb290cHJpbnQuaGVpZ2h0IC8gMikgfSA6IGNlbnRlcihldmVudFJvb20pXG4gIGNvbnN0IHBvaW50ID0gY2hvb3NlUGxhY2VtZW50KGZsb29yLCBydW50aW1lLCB7IGlkOiBgZXZlbnQ6JHtmbG9vci5pbmRleH1gLCByZXF1aXJlbWVudHM6IHJ1bnRpbWUucGlsb3QgPyB7IG5vZGVLaW5kcywgbWluRGlzdGFuY2U6IDUsIG5lYXI6IGZhbGxiYWNrLCBuZWFyRGlzdGFuY2U6IDAgfSA6IHsgbmVhcjogZmFsbGJhY2ssIG5lYXJEaXN0YW5jZTogMyB9LCAuLi4ocnVudGltZS5waWxvdCA/IHsgZmFsbGJhY2s6IHsgbmVhcjogZmFsbGJhY2ssIG5lYXJEaXN0YW5jZTogMyB9IH0gOiB7fSkgfSlcbiAgaWYgKCFwb2ludCkgdGhyb3cgbmV3IEVycm9yKGBmYWlsZWQgcGxhY2VtZW50IGV2ZW50OiR7Zmxvb3IuaW5kZXh9OiAke3J1bnRpbWUuZGlhZ25vc3RpY3MuYXQoLTEpPy5kaWFnbm9zdGljcy5qb2luKCc7ICcpfWApXG4gIGNvbnN0IGtpbmQ6IFRpbGVbJ2tpbmQnXSA9IGZsb29yLmluZGV4ICUgNCA9PT0gMCA/ICdzaG9wJyA6IGZsb29yLmluZGV4ICUgNCA9PT0gMSA/ICdyZXNjdWUnIDogZmxvb3IuaW5kZXggJSA0ID09PSAyID8gJ2FsdGFyJyA6ICdzaG9wJ1xuICBzZXRLaW5kKGZsb29yLCBwb2ludC54LCBwb2ludC55LCBraW5kKVxuICBpZiAoa2luZCA9PT0gJ3Nob3AnKSBmbG9vci5hY3RvcnMucHVzaChmcmllbmRseSgnbWVyY2hhbnQnLCBgJHtmbG9vci5iaW9tZX0gdHJhZGVyYCwgcG9pbnQsICckJywgJyNmNGQyNmEnKSlcbiAgaWYgKGtpbmQgPT09ICdyZXNjdWUnKSBmbG9vci5hY3RvcnMucHVzaChmcmllbmRseSgnYWxseScsICdzdHJhbmRlZCB0cmF2ZWxlcicsIHBvaW50LCAnJicsICcjOGFlMGIzJykpXG4gIGlmIChraW5kID09PSAnYWx0YXInKSBmbG9vci5hY3RvcnMucHVzaChmcmllbmRseSgnYWxseScsICdzaHJpbmUga2VlcGVyJywgcG9pbnQsICdfJywgJyNkNmE4ZWInKSlcbn1cblxuZnVuY3Rpb24gcGxhY2VEb29yc0FuZExvY2tzKGZsb29yOiBGbG9vciwgcm5nOiBSbmcsIHJvb21zOiBSb29tW10pOiB2b2lkIHtcbiAgbGV0IHBsYWNlZFJ1aW5zTG9jayA9IGZhbHNlXG4gIGZvciAoY29uc3Qgcm9vbSBvZiByb29tcy5zbGljZSgxLCAtMSkpIHtcbiAgICBjb25zdCBwb2ludCA9IGNlbnRlcihyb29tKVxuICAgIGNvbnN0IGRvb3IgPSB7IHg6IE1hdGgubWF4KDEsIHBvaW50LnggLSBNYXRoLmZsb29yKHJvb20udyAvIDIpKSwgeTogcG9pbnQueSB9XG4gICAgaWYgKGdldFRpbGUoZmxvb3IsIGRvb3IueCwgZG9vci55KT8ua2luZCA9PT0gJ2Zsb29yJykge1xuICAgICAgY29uc3QgbG9ja2VkID0gZmxvb3IuYmlvbWUgPT09ICdydWlucycgPyAhcGxhY2VkUnVpbnNMb2NrIHx8IHJuZy5jaGFuY2UoNjUpIDogcm5nLmNoYW5jZSgyNSlcbiAgICAgIHNldEtpbmQoZmxvb3IsIGRvb3IueCwgZG9vci55LCBsb2NrZWQgPyAnbG9ja2VkRG9vcicgOiAnZG9vcicpXG4gICAgICBpZiAobG9ja2VkKSBwbGFjZWRSdWluc0xvY2sgPSB0cnVlXG4gICAgfVxuICB9XG59XG5cbmNvbnN0IHJvdXRlVGhyb3VnaExvY2tzID0gKGZsb29yOiBGbG9vcik6IFBvaW50W10gfCB1bmRlZmluZWQgPT4ge1xuICBjb25zdCBzdGFydCA9IHsgLi4uZmxvb3Iuc3RhcnQgfVxuICBjb25zdCBxdWV1ZSA9IFtzdGFydF1cbiAgY29uc3QgcHJldmlvdXMgPSBuZXcgTWFwPHN0cmluZywgUG9pbnQgfCB1bmRlZmluZWQ+KFtbcG9pbnRLZXkoc3RhcnQpLCB1bmRlZmluZWRdXSlcbiAgY29uc3QgdHJhdmVyc2FibGUgPSAoa2luZDogVGlsZVsna2luZCddKSA9PiBraW5kICE9PSAnd2FsbCcgJiYga2luZCAhPT0gJ2NsaWZmV2FsbCdcbiAgZm9yIChsZXQgY3Vyc29yID0gMDsgY3Vyc29yIDwgcXVldWUubGVuZ3RoOyBjdXJzb3IrKykge1xuICAgIGNvbnN0IHBvaW50ID0gcXVldWVbY3Vyc29yXVxuICAgIGlmIChwb2ludC54ID09PSBmbG9vci5leGl0LnggJiYgcG9pbnQueSA9PT0gZmxvb3IuZXhpdC55KSB7XG4gICAgICBjb25zdCBwYXRoOiBQb2ludFtdID0gW11cbiAgICAgIGZvciAobGV0IGN1cnJlbnQ6IFBvaW50IHwgdW5kZWZpbmVkID0gcG9pbnQ7IGN1cnJlbnQ7IGN1cnJlbnQgPSBwcmV2aW91cy5nZXQocG9pbnRLZXkoY3VycmVudCkpKSBwYXRoLnB1c2goY3VycmVudClcbiAgICAgIHJldHVybiBwYXRoLnJldmVyc2UoKVxuICAgIH1cbiAgICBmb3IgKGNvbnN0IFt4LCB5XSBvZiBjYXJkaW5hbE9mZnNldHMpIHtcbiAgICAgIGNvbnN0IG5leHQgPSB7IHg6IHBvaW50LnggKyB4LCB5OiBwb2ludC55ICsgeSB9XG4gICAgICBjb25zdCBrZXkgPSBwb2ludEtleShuZXh0KVxuICAgICAgaWYgKHByZXZpb3VzLmhhcyhrZXkpIHx8ICF0cmF2ZXJzYWJsZShnZXRUaWxlKGZsb29yLCBuZXh0LngsIG5leHQueSk/LmtpbmQgPz8gJ3dhbGwnKSkgY29udGludWVcbiAgICAgIHByZXZpb3VzLnNldChrZXksIHBvaW50KVxuICAgICAgcXVldWUucHVzaChuZXh0KVxuICAgIH1cbiAgfVxuICByZXR1cm4gdW5kZWZpbmVkXG59XG5cbmNvbnN0IG9wZW5NYW5kYXRvcnlMb2NrcyA9IChmbG9vcjogRmxvb3IpOiB2b2lkID0+IHtcbiAgaWYgKGhhc1Bhc3NhYmxlUGF0aChmbG9vciwgZmxvb3Iuc3RhcnQsIGZsb29yLmV4aXQpKSByZXR1cm5cbiAgY29uc3Qgcm91dGUgPSByb3V0ZVRocm91Z2hMb2NrcyhmbG9vcilcbiAgaWYgKCFyb3V0ZSkgcmV0dXJuXG4gIGZvciAoY29uc3QgcG9pbnQgb2Ygcm91dGUpIHtcbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSlcbiAgICBpZiAodGlsZT8ua2luZCA9PT0gJ2xvY2tlZERvb3InKSB0aWxlLmtpbmQgPSAnZG9vcidcbiAgfVxufVxuXG5jb25zdCByZXBhaXJNYW5kYXRvcnlQYXRoID0gKGZsb29yOiBGbG9vcik6IHZvaWQgPT4ge1xuICBpZiAoaGFzUGFzc2FibGVQYXRoKGZsb29yLCBmbG9vci5zdGFydCwgZmxvb3IuZXhpdCkpIHJldHVyblxuICBvcGVuTWFuZGF0b3J5TG9ja3MoZmxvb3IpXG4gIGlmIChoYXNQYXNzYWJsZVBhdGgoZmxvb3IsIGZsb29yLnN0YXJ0LCBmbG9vci5leGl0KSkgcmV0dXJuXG4gIGNvbnN0IHRyYWNlID0gdHJhdmVyc2VGbG9vcihmbG9vciwgZmxvb3Iuc3RhcnQsIHsgdGFyZ2V0OiBmbG9vci5leGl0IH0pXG4gIHRocm93IG5ldyBFcnJvcihgYm91bmRlZCByb3V0ZSByZXBhaXIgcmVmdXNlZCB0byBmbGF0dGVuIHRlcnJhaW46IGJsb2NrZXJzPSR7dHJhY2UuYmxvY2tlcnMuam9pbignfCcpfWApXG59XG5cbmZ1bmN0aW9uIHBsYWNlQ29udGFpbmVycyhmbG9vcjogRmxvb3IsIHJuZzogUm5nLCByb29tczogUm9vbVtdLCByZXNlcnZlZDogUmVhZG9ubHlTZXQ8bnVtYmVyPiA9IG5ldyBTZXQoKSk6IHZvaWQge1xuICBmb3IgKGxldCBpID0gMDsgaSA8IDQ7IGkrKykge1xuICAgIGNvbnN0IGtpbmQ6IFRpbGVbJ2tpbmQnXSA9IGkgPT09IDMgPyAnY2hlc3QnIDogJ2NyYXRlJ1xuICAgIGZvciAobGV0IGF0dGVtcHQgPSAwOyBhdHRlbXB0IDwgODA7IGF0dGVtcHQrKykge1xuICAgICAgY29uc3QgcG9pbnQgPSBmcmVlUm9vbVBvaW50KGZsb29yLCBybmcsIHJvb21zLCByZXNlcnZlZClcbiAgICAgIGNvbnN0IHRhcmdldCA9IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpXG4gICAgICBjb25zdCBleGl0cyA9IGNhcmRpbmFsT2Zmc2V0cy5maWx0ZXIoKFt4LCB5XSkgPT4gcGFzc2FibGUoZ2V0VGlsZShmbG9vciwgcG9pbnQueCArIHgsIHBvaW50LnkgKyB5KT8ua2luZCA/PyAnd2FsbCcpKS5sZW5ndGhcbiAgICAgIGlmICghdGFyZ2V0IHx8IHRhcmdldC5raW5kICE9PSAnZmxvb3InIHx8IGV4aXRzIDwgMikgY29udGludWVcbiAgICAgIHRhcmdldC5raW5kID0ga2luZFxuICAgICAgYnJlYWtcbiAgICB9XG4gIH1cbn1cblxuZnVuY3Rpb24gcGxhY2VBY3RvcnMoZmxvb3I6IEZsb29yLCBybmc6IFJuZywgcnVudGltZTogUGxhY2VtZW50UnVudGltZSk6IHZvaWQge1xuICBjb25zdCBkZWZpbml0aW9ucyA9IE1PTlNURVJTLmZpbHRlcihtb25zdGVyID0+IG1vbnN0ZXIuYmlvbWUgPT09IGZsb29yLmJpb21lKVxuICBjb25zdCByZWd1bGFyID0gZGVmaW5pdGlvbnMuZmlsdGVyKG1vbnN0ZXIgPT4gbW9uc3Rlci5haSAhPT0gJ2d1YXJkaWFuJyAmJiBtb25zdGVyLnNwYXduICE9PSAndHJpZ2dlcmVkJylcbiAgY29uc3QgYXJlYUZsb29yID0gZmxvb3IuaW5kZXggJSA0XG4gIGNvbnN0IHJvdXRlUG9zaXRpb24gPSBmbG9vci5kaWZmaWN1bHR5Py5yb3V0ZVBvc2l0aW9uID8/IDBcbiAgY29uc3QgZGlyZWN0ZWQ6IE5vbk51bGxhYmxlPEFjdG9yWydlbmNvdW50ZXInXT5bXSA9IFtdXG4gIGlmIChhcmVhRmxvb3IgIT09IDMpIGZvciAoY29uc3QgW2dyb3VwSW5kZXgsIHBsYW5dIG9mIGVuY291bnRlclBsYW5zRm9yKHsgYmlvbWU6IGZsb29yLmJpb21lLCBhcmVhRmxvb3IsIHJvdXRlUG9zaXRpb24sIHBpbG90OiBydW50aW1lLnBpbG90LCBhcmNPZmZzZXQ6IGZsb29yLmVzY2FsYXRpb24/LmVuY291bnRlck9mZnNldCB9LCBuYXRpdmVBY3RvclRlcnJhaW5bZmxvb3IuYmlvbWVdKS5lbnRyaWVzKCkpIHtcbiAgICBjb25zdCBpZCA9IGB0YWN0aWNhbDoke2Zsb29yLmluZGV4fToke2dyb3VwSW5kZXh9OiR7cGxhbi5hcmNoZXR5cGV9YFxuICAgIGxldCBsZWFkZXI6IFBvaW50IHwgdW5kZWZpbmVkXG4gICAgZm9yIChsZXQgbWVtYmVyID0gMDsgbWVtYmVyIDwgbWVtYmVyc0ZvckVuY291bnRlcihhcmVhRmxvb3IsIHJvdXRlUG9zaXRpb24pOyBtZW1iZXIrKykge1xuICAgICAgaWYgKG1lbWJlciA+IDAgJiYgIWxlYWRlcikgYnJlYWtcbiAgICAgIGNvbnN0IGRlZmluaXRpb24gPSBkZWZpbml0aW9uRm9yRW5jb3VudGVyKHJlZ3VsYXIsIHBsYW4sIG1lbWJlciwgbmF0aXZlQWN0b3JUZXJyYWluW2Zsb29yLmJpb21lXSlcbiAgICAgIGlmICghZGVmaW5pdGlvbikgY29udGludWVcbiAgICAgIGNvbnN0IGFmZmluaXR5ID0gcGxhbi5hcmNoZXR5cGUgPT09ICduYXRpdmVUZXJyYWluUGFjaycgJiYgbWVtYmVyID09PSAwID8gdGVycmFpbkFmZmluaXR5Rm9yKGRlZmluaXRpb24pLmZpbHRlcihraW5kID0+IG5hdGl2ZUFjdG9yVGVycmFpbltmbG9vci5iaW9tZV0uaW5jbHVkZXMoa2luZCkpIDogdW5kZWZpbmVkXG4gICAgICBjb25zdCByZXF1aXJlbWVudHMgPSBtZW1iZXIgPT09IDAgPyBhZmZpbml0eT8ubGVuZ3RoID8geyAuLi5wbGFuLnJlcXVpcmVtZW50cywgdGVycmFpbjogYWZmaW5pdHkgfSA6IHBsYW4ucmVxdWlyZW1lbnRzIDogeyBtaW5EaXN0YW5jZTogNywgY2hva2Vwb2ludDogZmFsc2UsIG5lYXI6IGxlYWRlciEsIG5lYXJEaXN0YW5jZTogNSB9XG4gICAgICBjb25zdCBmYWxsYmFjayA9IG1lbWJlciA9PT0gMCA/IGFmZmluaXR5Py5sZW5ndGggPyB7IC4uLnBsYW4uZmFsbGJhY2ssIHRlcnJhaW46IGFmZmluaXR5IH0gOiBwbGFuLmZhbGxiYWNrIDogeyBtaW5EaXN0YW5jZTogNywgY2hva2Vwb2ludDogZmFsc2UsIG5lYXI6IGxlYWRlciEsIG5lYXJEaXN0YW5jZTogOCB9XG4gICAgICBjb25zdCBjb250cmFjdDogUGxhY2VtZW50Q29udHJhY3QgPSB7IGlkOiBgYWN0b3I6JHtpZH06JHttZW1iZXJ9OiR7ZGVmaW5pdGlvbi5pZH1gLCByZXF1aXJlbWVudHMsIGZhbGxiYWNrIH1cbiAgICAgIGNvbnN0IHBvaW50ID0gY2hvb3NlUGxhY2VtZW50KGZsb29yLCBydW50aW1lLCBjb250cmFjdCwgY2FuZGlkYXRlID0+IChjYW5kaWRhdGUueCAhPT0gZmxvb3IuZXhpdC54IHx8IGNhbmRpZGF0ZS55ICE9PSBmbG9vci5leGl0LnkpICYmIChjYW5kaWRhdGUueCAhPT0gZmxvb3Iuc3RhcnQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3Iuc3RhcnQueSkpXG4gICAgICBpZiAoIXBvaW50KSBjb250aW51ZVxuICAgICAgY29uc3QgZW5jb3VudGVyID0geyBpZCwgYXJjaGV0eXBlOiBwbGFuLmFyY2hldHlwZSwgbGVhZGVyOiBtZW1iZXIgPT09IDAsIGFuc3dlcjogcGxhbi5hbnN3ZXIgfVxuICAgICAgY29uc3QgYWN0b3IgPSBzcGF3bk1vbnN0ZXIoZGVmaW5pdGlvbi5pZCwgcG9pbnQsIGAke2RlZmluaXRpb24uaWR9LSR7Z3JvdXBJbmRleH0tJHttZW1iZXJ9YCwgZmxvb3IuZGlmZmljdWx0eSlcbiAgICAgIGFjdG9yLmVuY291bnRlciA9IGVuY291bnRlclxuICAgICAgaWYgKG1lbWJlciA9PT0gMCAmJiAocm5nLmNoYW5jZShmbG9vci5kaWZmaWN1bHR5Py5lbGl0ZUNoYW5jZSA/PyAwKSB8fCAoZmxvb3IuYmlvbWUgPT09ICdmcm9zdFJlbGlxdWFyeScgJiYgYXJlYUZsb29yID49IDEpKSkge1xuICAgICAgICBhY3Rvci5tYXhIZWFsdGggPSBNYXRoLnJvdW5kKGFjdG9yLm1heEhlYWx0aCAqIDEuMjUpXG4gICAgICAgIGFjdG9yLmhlYWx0aCA9IGFjdG9yLm1heEhlYWx0aFxuICAgICAgICBhY3Rvci5hdHRhY2sgKz0gMlxuICAgICAgICBhY3Rvci5zdGF0dXMgPSBbLi4uKGFjdG9yLnN0YXR1cyA/PyBbXSksICdlbGl0ZSddXG4gICAgICB9XG4gICAgICBmbG9vci5hY3RvcnMucHVzaChhY3RvcilcbiAgICAgIGRpcmVjdGVkLnB1c2goZW5jb3VudGVyKVxuICAgICAgbGVhZGVyID8/PSBwb2ludFxuICAgIH1cbiAgfVxuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdtaW5lJyAmJiBhcmVhRmxvb3IgIT09IDMgJiYgIWZsb29yLmFjdG9ycy5zb21lKGFjdG9yID0+IGFjdG9yLmhvc3RpbGUgJiYgYWN0b3IudGVycmFpbkFmZmluaXR5Py5pbmNsdWRlcyhnZXRUaWxlKGZsb29yLCBhY3Rvci54LCBhY3Rvci55KT8ua2luZCA/PyAnd2FsbCcpKSkge1xuICAgIGNvbnN0IHJhaWxndWFyZCA9IGRlZmluaXRpb25zLmZpbmQoZGVmaW5pdGlvbiA9PiBkZWZpbml0aW9uLmlkID09PSAncmFpbGd1YXJkJylcbiAgICBjb25zdCB0ZXJyYWluID0gcmFpbGd1YXJkID8gdGVycmFpbkFmZmluaXR5Rm9yKHJhaWxndWFyZCkuZmlsdGVyKGtpbmQgPT4gbmF0aXZlQWN0b3JUZXJyYWluLm1pbmUuaW5jbHVkZXMoa2luZCkpIDogW11cbiAgICBpZiAoIXJhaWxndWFyZCB8fCAhdGVycmFpbi5sZW5ndGgpIHRocm93IG5ldyBFcnJvcignTWluZSByYWlsIHBhdHJvbCBsYWNrcyB0ZXJyYWluIGFmZmluaXR5JylcbiAgICBjb25zdCBwb2ludCA9IGNob29zZVBsYWNlbWVudChmbG9vciwgcnVudGltZSwgeyBpZDogYGFjdG9yOnRhY3RpY2FsOiR7Zmxvb3IuaW5kZXh9OnJhaWwtcGF0cm9sOnJhaWxndWFyZGAsIHJlcXVpcmVtZW50czogeyB0ZXJyYWluLCBtaW5EaXN0YW5jZTogOCB9IH0sIGNhbmRpZGF0ZSA9PiAoY2FuZGlkYXRlLnggIT09IGZsb29yLmV4aXQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3IuZXhpdC55KSAmJiAoY2FuZGlkYXRlLnggIT09IGZsb29yLnN0YXJ0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLnN0YXJ0LnkpKVxuICAgIGlmICghcG9pbnQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIHBsYWNlbWVudCBNaW5lIHJhaWwgcGF0cm9sOiAke3J1bnRpbWUuZGlhZ25vc3RpY3MuYXQoLTEpPy5kaWFnbm9zdGljcy5qb2luKCc7ICcpfWApXG4gICAgY29uc3QgZW5jb3VudGVyID0geyBpZDogYHRhY3RpY2FsOiR7Zmxvb3IuaW5kZXh9OnJhaWwtcGF0cm9sYCwgYXJjaGV0eXBlOiAnbmF0aXZlVGVycmFpblBhY2snIGFzIGNvbnN0LCBsZWFkZXI6IHRydWUsIGFuc3dlcjogJ2xlYXZlIHRoZSBlbmVteVxcJ3MgbmF0aXZlIHRlcnJhaW4nIH1cbiAgICBjb25zdCBhY3RvciA9IHNwYXduTW9uc3RlcihyYWlsZ3VhcmQuaWQsIHBvaW50LCBgJHtyYWlsZ3VhcmQuaWR9LXJhaWwtcGF0cm9sYCwgZmxvb3IuZGlmZmljdWx0eSlcbiAgICBhY3Rvci5lbmNvdW50ZXIgPSBlbmNvdW50ZXJcbiAgICBmbG9vci5hY3RvcnMucHVzaChhY3RvcilcbiAgICBkaXJlY3RlZC5wdXNoKGVuY291bnRlcilcbiAgfVxuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdjYXZlcm5zJyAmJiBhcmVhRmxvb3IgIT09IDMgJiYgIWZsb29yLmFjdG9ycy5zb21lKGFjdG9yID0+IGFjdG9yLmhvc3RpbGUgJiYgYWN0b3IudGVycmFpbkFmZmluaXR5Py5pbmNsdWRlcyhnZXRUaWxlKGZsb29yLCBhY3Rvci54LCBhY3Rvci55KT8ua2luZCA/PyAnd2FsbCcpKSkge1xuICAgIGNvbnN0IHRpZGVFZWwgPSBkZWZpbml0aW9ucy5maW5kKGRlZmluaXRpb24gPT4gZGVmaW5pdGlvbi5pZCA9PT0gJ2Z1bWVlZWwnKVxuICAgIGNvbnN0IHRlcnJhaW4gPSB0aWRlRWVsID8gdGVycmFpbkFmZmluaXR5Rm9yKHRpZGVFZWwpLmZpbHRlcihraW5kID0+IG5hdGl2ZUFjdG9yVGVycmFpbi5jYXZlcm5zLmluY2x1ZGVzKGtpbmQpKSA6IFtdXG4gICAgaWYgKCF0aWRlRWVsIHx8ICF0ZXJyYWluLmxlbmd0aCkgdGhyb3cgbmV3IEVycm9yKCdDYXZlcm5zIHRpZGUgcGF0cm9sIGxhY2tzIHRlcnJhaW4gYWZmaW5pdHknKVxuICAgIGNvbnN0IHBvaW50ID0gY2hvb3NlUGxhY2VtZW50KGZsb29yLCBydW50aW1lLCB7IGlkOiBgYWN0b3I6dGFjdGljYWw6JHtmbG9vci5pbmRleH06dGlkZS1wYXRyb2w6ZnVtZWVlbGAsIHJlcXVpcmVtZW50czogeyB0ZXJyYWluLCBtaW5EaXN0YW5jZTogOCB9IH0sIGNhbmRpZGF0ZSA9PiAoY2FuZGlkYXRlLnggIT09IGZsb29yLmV4aXQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3IuZXhpdC55KSAmJiAoY2FuZGlkYXRlLnggIT09IGZsb29yLnN0YXJ0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLnN0YXJ0LnkpKVxuICAgIGlmICghcG9pbnQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIHBsYWNlbWVudCBDYXZlcm5zIHRpZGUgcGF0cm9sOiAke3J1bnRpbWUuZGlhZ25vc3RpY3MuYXQoLTEpPy5kaWFnbm9zdGljcy5qb2luKCc7ICcpfWApXG4gICAgY29uc3QgZW5jb3VudGVyID0geyBpZDogYHRhY3RpY2FsOiR7Zmxvb3IuaW5kZXh9OnRpZGUtcGF0cm9sYCwgYXJjaGV0eXBlOiAnbmF0aXZlVGVycmFpblBhY2snIGFzIGNvbnN0LCBsZWFkZXI6IHRydWUsIGFuc3dlcjogJ2xlYXZlIHRoZSBlbmVteVxcJ3MgbmF0aXZlIHRlcnJhaW4nIH1cbiAgICBjb25zdCBhY3RvciA9IHNwYXduTW9uc3Rlcih0aWRlRWVsLmlkLCBwb2ludCwgYCR7dGlkZUVlbC5pZH0tdGlkZS1wYXRyb2xgLCBmbG9vci5kaWZmaWN1bHR5KVxuICAgIGFjdG9yLmVuY291bnRlciA9IGVuY291bnRlclxuICAgIGZsb29yLmFjdG9ycy5wdXNoKGFjdG9yKVxuICAgIGRpcmVjdGVkLnB1c2goZW5jb3VudGVyKVxuICB9XG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ3dpbGRzJyAmJiBhcmVhRmxvb3IgIT09IDMgJiYgIWZsb29yLmFjdG9ycy5zb21lKGFjdG9yID0+IGFjdG9yLmhvc3RpbGUgJiYgYWN0b3IudGVycmFpbkFmZmluaXR5Py5pbmNsdWRlcyhnZXRUaWxlKGZsb29yLCBhY3Rvci54LCBhY3Rvci55KT8ua2luZCA/PyAnd2FsbCcpKSkge1xuICAgIGNvbnN0IHdlYndlYXZlciA9IGRlZmluaXRpb25zLmZpbmQoZGVmaW5pdGlvbiA9PiBkZWZpbml0aW9uLmlkID09PSAnd2Vid2VhdmVyJylcbiAgICBjb25zdCB0ZXJyYWluID0gd2Vid2VhdmVyID8gdGVycmFpbkFmZmluaXR5Rm9yKHdlYndlYXZlcikuZmlsdGVyKGtpbmQgPT4gbmF0aXZlQWN0b3JUZXJyYWluLndpbGRzLmluY2x1ZGVzKGtpbmQpKSA6IFtdXG4gICAgaWYgKCF3ZWJ3ZWF2ZXIgfHwgIXRlcnJhaW4ubGVuZ3RoKSB0aHJvdyBuZXcgRXJyb3IoJ1dpbGRzIHdlYiBhbWJ1c2ggbGFja3MgdGVycmFpbiBhZmZpbml0eScpXG4gICAgY29uc3QgcG9pbnQgPSBjaG9vc2VQbGFjZW1lbnQoZmxvb3IsIHJ1bnRpbWUsIHsgaWQ6IGBhY3Rvcjp0YWN0aWNhbDoke2Zsb29yLmluZGV4fTp3ZWItYW1idXNoOndlYndlYXZlcmAsIHJlcXVpcmVtZW50czogeyB0ZXJyYWluLCBtaW5EaXN0YW5jZTogOCB9IH0sIGNhbmRpZGF0ZSA9PiAoY2FuZGlkYXRlLnggIT09IGZsb29yLmV4aXQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3IuZXhpdC55KSAmJiAoY2FuZGlkYXRlLnggIT09IGZsb29yLnN0YXJ0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLnN0YXJ0LnkpKVxuICAgIGlmICghcG9pbnQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIHBsYWNlbWVudCBXaWxkcyB3ZWIgYW1idXNoOiAke3J1bnRpbWUuZGlhZ25vc3RpY3MuYXQoLTEpPy5kaWFnbm9zdGljcy5qb2luKCc7ICcpfWApXG4gICAgY29uc3QgZW5jb3VudGVyID0geyBpZDogYHRhY3RpY2FsOiR7Zmxvb3IuaW5kZXh9OndlYi1hbWJ1c2hgLCBhcmNoZXR5cGU6ICduYXRpdmVUZXJyYWluUGFjaycgYXMgY29uc3QsIGxlYWRlcjogdHJ1ZSwgYW5zd2VyOiAnbGVhdmUgdGhlIGVuZW15XFwncyBuYXRpdmUgdGVycmFpbicgfVxuICAgIGNvbnN0IGFjdG9yID0gc3Bhd25Nb25zdGVyKHdlYndlYXZlci5pZCwgcG9pbnQsIGAke3dlYndlYXZlci5pZH0td2ViLWFtYnVzaGAsIGZsb29yLmRpZmZpY3VsdHkpXG4gICAgYWN0b3IuZW5jb3VudGVyID0gZW5jb3VudGVyXG4gICAgZmxvb3IuYWN0b3JzLnB1c2goYWN0b3IpXG4gICAgZGlyZWN0ZWQucHVzaChlbmNvdW50ZXIpXG4gIH1cbiAgaWYgKGZsb29yLmJpb21lID09PSAncnVpbnMnICYmIGFyZWFGbG9vciAhPT0gMyAmJiAhZmxvb3IuYWN0b3JzLnNvbWUoYWN0b3IgPT4gYWN0b3Iua2luZCA9PT0gJ3dhcmRhY29seXRlJykpIHtcbiAgICBjb25zdCBhY29seXRlID0gZGVmaW5pdGlvbnMuZmluZChkZWZpbml0aW9uID0+IGRlZmluaXRpb24uaWQgPT09ICd3YXJkYWNvbHl0ZScpXG4gICAgY29uc3QgdGVycmFpbiA9IGFjb2x5dGUgPyB0ZXJyYWluQWZmaW5pdHlGb3IoYWNvbHl0ZSkuZmlsdGVyKGtpbmQgPT4gbmF0aXZlQWN0b3JUZXJyYWluLnJ1aW5zLmluY2x1ZGVzKGtpbmQpKSA6IFtdXG4gICAgaWYgKCFhY29seXRlIHx8ICF0ZXJyYWluLmxlbmd0aCkgdGhyb3cgbmV3IEVycm9yKCdSdWlucyB3YXJkIHBvc3QgbGFja3MgdGVycmFpbiBhZmZpbml0eScpXG4gICAgY29uc3QgcG9pbnQgPSBjaG9vc2VQbGFjZW1lbnQoZmxvb3IsIHJ1bnRpbWUsIHsgaWQ6IGBhY3Rvcjp0YWN0aWNhbDoke2Zsb29yLmluZGV4fTp3YXJkLXBvc3Q6d2FyZGFjb2x5dGVgLCByZXF1aXJlbWVudHM6IHsgdGVycmFpbiwgbWluRGlzdGFuY2U6IDggfSwgZmFsbGJhY2s6IHsgdGVycmFpbjogWydmbG9vciddLCBtaW5EaXN0YW5jZTogOCB9IH0sIGNhbmRpZGF0ZSA9PiAoY2FuZGlkYXRlLnggIT09IGZsb29yLmV4aXQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3IuZXhpdC55KSAmJiAoY2FuZGlkYXRlLnggIT09IGZsb29yLnN0YXJ0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLnN0YXJ0LnkpKVxuICAgIGlmICghcG9pbnQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIHBsYWNlbWVudCBSdWlucyB3YXJkIHBvc3Q6ICR7cnVudGltZS5kaWFnbm9zdGljcy5hdCgtMSk/LmRpYWdub3N0aWNzLmpvaW4oJzsgJyl9YClcbiAgICBjb25zdCBlbmNvdW50ZXIgPSB7IGlkOiBgdGFjdGljYWw6JHtmbG9vci5pbmRleH06d2FyZC1wb3N0YCwgYXJjaGV0eXBlOiAnb2JqZWN0aXZlRGVmZW5zZScgYXMgY29uc3QsIGxlYWRlcjogdHJ1ZSwgYW5zd2VyOiAnYnJlYWsgbGluZSBvZiBzaWdodCBhbmQgaW50ZXJydXB0IHRoZSB3YXJkJyB9XG4gICAgY29uc3QgYWN0b3IgPSBzcGF3bk1vbnN0ZXIoYWNvbHl0ZS5pZCwgcG9pbnQsIGAke2Fjb2x5dGUuaWR9LXdhcmQtcG9zdGAsIGZsb29yLmRpZmZpY3VsdHkpXG4gICAgYWN0b3IuZW5jb3VudGVyID0gZW5jb3VudGVyXG4gICAgZmxvb3IuYWN0b3JzLnB1c2goYWN0b3IpXG4gICAgZGlyZWN0ZWQucHVzaChlbmNvdW50ZXIpXG4gIH1cbiAgaWYgKGZsb29yLmJpb21lID09PSAnZnVybmFjZScgJiYgYXJlYUZsb29yICE9PSAzICYmICFmbG9vci5hY3RvcnMuc29tZShhY3RvciA9PiBhY3Rvci5raW5kID09PSAnY2luZGVybGluZycgJiYgYWN0b3IudGVycmFpbkFmZmluaXR5Py5pbmNsdWRlcyhnZXRUaWxlKGZsb29yLCBhY3Rvci54LCBhY3Rvci55KT8ua2luZCA/PyAnd2FsbCcpKSkge1xuICAgIGNvbnN0IGNpbmRlcmxpbmcgPSBkZWZpbml0aW9ucy5maW5kKGRlZmluaXRpb24gPT4gZGVmaW5pdGlvbi5pZCA9PT0gJ2NpbmRlcmxpbmcnKVxuICAgIGNvbnN0IHRlcnJhaW4gPSBjaW5kZXJsaW5nID8gdGVycmFpbkFmZmluaXR5Rm9yKGNpbmRlcmxpbmcpLmZpbHRlcihraW5kID0+IGtpbmQgPT09ICdmaXJlVmVudCcpIDogW11cbiAgICBpZiAoIWNpbmRlcmxpbmcgfHwgIXRlcnJhaW4ubGVuZ3RoKSB0aHJvdyBuZXcgRXJyb3IoJ0Z1cm5hY2UgZmlyaW5nIHBvc3QgbGFja3MgaGVhdCBhZmZpbml0eScpXG4gICAgY29uc3QgcG9pbnQgPSBjaG9vc2VQbGFjZW1lbnQoZmxvb3IsIHJ1bnRpbWUsIHsgaWQ6IGBhY3Rvcjp0YWN0aWNhbDoke2Zsb29yLmluZGV4fTpmaXJpbmctcG9zdDpjaW5kZXJsaW5nYCwgcmVxdWlyZW1lbnRzOiB7IHRlcnJhaW4sIG1pbkRpc3RhbmNlOiA4IH0sIGZhbGxiYWNrOiB7IHRlcnJhaW4sIG1pbkRpc3RhbmNlOiA1IH0gfSwgY2FuZGlkYXRlID0+IChjYW5kaWRhdGUueCAhPT0gZmxvb3IuZXhpdC54IHx8IGNhbmRpZGF0ZS55ICE9PSBmbG9vci5leGl0LnkpICYmIChjYW5kaWRhdGUueCAhPT0gZmxvb3Iuc3RhcnQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3Iuc3RhcnQueSkpXG4gICAgaWYgKCFwb2ludCkgdGhyb3cgbmV3IEVycm9yKGBmYWlsZWQgcGxhY2VtZW50IEZ1cm5hY2UgZmlyaW5nIHBvc3Q6ICR7cnVudGltZS5kaWFnbm9zdGljcy5hdCgtMSk/LmRpYWdub3N0aWNzLmpvaW4oJzsgJyl9YClcbiAgICBjb25zdCBlbmNvdW50ZXIgPSB7IGlkOiBgdGFjdGljYWw6JHtmbG9vci5pbmRleH06ZmlyaW5nLXBvc3RgLCBhcmNoZXR5cGU6ICdvYmplY3RpdmVEZWZlbnNlJyBhcyBjb25zdCwgbGVhZGVyOiB0cnVlLCBhbnN3ZXI6ICd0YWtlIHRoZSBsaWZ0IGxhbmUgYW5kIHF1ZW5jaCB0aGUgZmlyaW5nIHZlbnQnIH1cbiAgICBjb25zdCBhY3RvciA9IHNwYXduTW9uc3RlcihjaW5kZXJsaW5nLmlkLCBwb2ludCwgYCR7Y2luZGVybGluZy5pZH0tZmlyaW5nLXBvc3RgLCBmbG9vci5kaWZmaWN1bHR5KVxuICAgIGFjdG9yLmVuY291bnRlciA9IGVuY291bnRlclxuICAgIGZsb29yLmFjdG9ycy5wdXNoKGFjdG9yKVxuICAgIGRpcmVjdGVkLnB1c2goZW5jb3VudGVyKVxuICB9XG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2Zsb29kZWRSdWlucycgJiYgYXJlYUZsb29yICE9PSAzICYmICFmbG9vci5hY3RvcnMuc29tZShhY3RvciA9PiBhY3Rvci5raW5kID09PSAndGlkZXdyYWl0aCcgJiYgYWN0b3IudGVycmFpbkFmZmluaXR5Py5pbmNsdWRlcyhnZXRUaWxlKGZsb29yLCBhY3Rvci54LCBhY3Rvci55KT8ua2luZCA/PyAnd2FsbCcpKSkge1xuICAgIGNvbnN0IHRpZGV3cmFpdGggPSBkZWZpbml0aW9ucy5maW5kKGRlZmluaXRpb24gPT4gZGVmaW5pdGlvbi5pZCA9PT0gJ3RpZGV3cmFpdGgnKVxuICAgIGNvbnN0IHRlcnJhaW4gPSB0aWRld3JhaXRoID8gdGVycmFpbkFmZmluaXR5Rm9yKHRpZGV3cmFpdGgpLmZpbHRlcihraW5kID0+IGtpbmQgPT09ICdjdXJyZW50JykgOiBbXVxuICAgIGlmICghdGlkZXdyYWl0aCB8fCAhdGVycmFpbi5sZW5ndGgpIHRocm93IG5ldyBFcnJvcignRmxvb2RlZCBjdXJyZW50IHBhdHJvbCBsYWNrcyBmbG93IGFmZmluaXR5JylcbiAgICBjb25zdCBwb2ludCA9IGNob29zZVBsYWNlbWVudChmbG9vciwgcnVudGltZSwgeyBpZDogYGFjdG9yOnRhY3RpY2FsOiR7Zmxvb3IuaW5kZXh9OmN1cnJlbnQtcGF0cm9sOnRpZGV3cmFpdGhgLCByZXF1aXJlbWVudHM6IHsgdGVycmFpbiwgbWluRGlzdGFuY2U6IDggfSwgZmFsbGJhY2s6IHsgdGVycmFpbiwgbWluRGlzdGFuY2U6IDUgfSB9LCBjYW5kaWRhdGUgPT4gKGNhbmRpZGF0ZS54ICE9PSBmbG9vci5leGl0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLmV4aXQueSkgJiYgKGNhbmRpZGF0ZS54ICE9PSBmbG9vci5zdGFydC54IHx8IGNhbmRpZGF0ZS55ICE9PSBmbG9vci5zdGFydC55KSlcbiAgICBpZiAoIXBvaW50KSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCBwbGFjZW1lbnQgRmxvb2RlZCBjdXJyZW50IHBhdHJvbDogJHtydW50aW1lLmRpYWdub3N0aWNzLmF0KC0xKT8uZGlhZ25vc3RpY3Muam9pbignOyAnKX1gKVxuICAgIGNvbnN0IGVuY291bnRlciA9IHsgaWQ6IGB0YWN0aWNhbDoke2Zsb29yLmluZGV4fTpjdXJyZW50LXBhdHJvbGAsIGFyY2hldHlwZTogJ25hdGl2ZVRlcnJhaW5QYWNrJyBhcyBjb25zdCwgbGVhZGVyOiB0cnVlLCBhbnN3ZXI6ICdob2xkIHRoZSBhbmNob3IgYW5kIGxlYXZlIHRoZSBjdXJyZW50JyB9XG4gICAgY29uc3QgYWN0b3IgPSBzcGF3bk1vbnN0ZXIodGlkZXdyYWl0aC5pZCwgcG9pbnQsIGAke3RpZGV3cmFpdGguaWR9LWN1cnJlbnQtcGF0cm9sYCwgZmxvb3IuZGlmZmljdWx0eSlcbiAgICBhY3Rvci5lbmNvdW50ZXIgPSBlbmNvdW50ZXJcbiAgICBmbG9vci5hY3RvcnMucHVzaChhY3RvcilcbiAgICBkaXJlY3RlZC5wdXNoKGVuY291bnRlcilcbiAgfVxuICBpZiAoZmxvb3IuYmlvbWUgPT09ICdjbGlmZnMnICYmIGFyZWFGbG9vciA9PT0gMCAmJiAhZmxvb3IuYWN0b3JzLnNvbWUoYWN0b3IgPT4gYWN0b3Iua2luZCA9PT0gJ3N0b3JtQ3JvdycgJiYgZ2V0VGlsZShmbG9vciwgYWN0b3IueCwgYWN0b3IueSk/LmtpbmQgPT09ICdsZWRnZScpKSB7XG4gICAgY29uc3Qgc3Rvcm1Dcm93ID0gZGVmaW5pdGlvbnMuZmluZChkZWZpbml0aW9uID0+IGRlZmluaXRpb24uaWQgPT09ICdzdG9ybUNyb3cnKVxuICAgIGlmICghc3Rvcm1Dcm93KSB0aHJvdyBuZXcgRXJyb3IoJ0NsaWZmcyB3aW5kIHBhdHJvbCBsYWNrcyBzdG9ybSBjcm93JylcbiAgICBjb25zdCBwb2ludCA9IGNob29zZVBsYWNlbWVudChmbG9vciwgcnVudGltZSwgeyBpZDogYGFjdG9yOnRhY3RpY2FsOiR7Zmxvb3IuaW5kZXh9OndpbmQtcGF0cm9sOnN0b3JtQ3Jvd2AsIHJlcXVpcmVtZW50czogeyB0ZXJyYWluOiBbJ2xlZGdlJ10sIG5vZGVLaW5kczogWydvcHRpb25hbFJld2FyZCddLCBtaW5EaXN0YW5jZTogOCwgY292ZXI6IGZhbHNlIH0sIGZhbGxiYWNrOiB7IHRlcnJhaW46IFsnbGVkZ2UnXSwgbWluRGlzdGFuY2U6IDUgfSB9LCBjYW5kaWRhdGUgPT4gKGNhbmRpZGF0ZS54ICE9PSBmbG9vci5leGl0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLmV4aXQueSkgJiYgKGNhbmRpZGF0ZS54ICE9PSBmbG9vci5zdGFydC54IHx8IGNhbmRpZGF0ZS55ICE9PSBmbG9vci5zdGFydC55KSlcbiAgICBpZiAoIXBvaW50KSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCBwbGFjZW1lbnQgQ2xpZmZzIHdpbmQgcGF0cm9sOiAke3J1bnRpbWUuZGlhZ25vc3RpY3MuYXQoLTEpPy5kaWFnbm9zdGljcy5qb2luKCc7ICcpfWApXG4gICAgY29uc3QgZW5jb3VudGVyID0geyBpZDogYHRhY3RpY2FsOiR7Zmxvb3IuaW5kZXh9OndpbmQtcGF0cm9sYCwgYXJjaGV0eXBlOiAnYXJ0aWxsZXJ5Q292ZXInIGFzIGNvbnN0LCBsZWFkZXI6IHRydWUsIGFuc3dlcjogJ2JyZWFrIHRoZSBzaWdodGxpbmUgb3IgdGFrZSB0aGUgYW5jaG9yZWQgaGlnaCByb3V0ZScgfVxuICAgIGNvbnN0IGFjdG9yID0gc3Bhd25Nb25zdGVyKHN0b3JtQ3Jvdy5pZCwgcG9pbnQsIGAke3N0b3JtQ3Jvdy5pZH0td2luZC1wYXRyb2xgLCBmbG9vci5kaWZmaWN1bHR5KVxuICAgIGFjdG9yLmVuY291bnRlciA9IGVuY291bnRlclxuICAgIGZsb29yLmFjdG9ycy5wdXNoKGFjdG9yKVxuICAgIGRpcmVjdGVkLnB1c2goZW5jb3VudGVyKVxuICB9XG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2J1cmlhbCcgJiYgYXJlYUZsb29yICE9PSAzICYmICFmbG9vci5hY3RvcnMuc29tZShhY3RvciA9PiBhY3Rvci5raW5kID09PSAndG9tYldhcmRlbicgJiYgYWN0b3IudGVycmFpbkFmZmluaXR5Py5pbmNsdWRlcyhnZXRUaWxlKGZsb29yLCBhY3Rvci54LCBhY3Rvci55KT8ua2luZCA/PyAnd2FsbCcpKSkge1xuICAgIGNvbnN0IHRvbWJXYXJkZW4gPSBkZWZpbml0aW9ucy5maW5kKGRlZmluaXRpb24gPT4gZGVmaW5pdGlvbi5pZCA9PT0gJ3RvbWJXYXJkZW4nKVxuICAgIGlmICghdG9tYldhcmRlbikgdGhyb3cgbmV3IEVycm9yKCdCdXJpYWwgcHJvY2Vzc2lvbiBsYWNrcyB0b21iIHdhcmRlbicpXG4gICAgY29uc3QgcG9pbnQgPSBjaG9vc2VQbGFjZW1lbnQoZmxvb3IsIHJ1bnRpbWUsIHsgaWQ6IGBhY3Rvcjp0YWN0aWNhbDoke2Zsb29yLmluZGV4fTpwcm9jZXNzaW9uLXdhcmRlbjp0b21iV2FyZGVuYCwgcmVxdWlyZW1lbnRzOiB7IHRlcnJhaW46IFsnZ3JhdmVTb2lsJ10sIGVkZ2VNb2RlczogWydjb3N0bHknLCAnb3B0aW9uYWwnXSwgb3B0aW9uYWw6IHRydWUsIG1pbkRpc3RhbmNlOiA4IH0sIGZhbGxiYWNrOiB7IHRlcnJhaW46IFsnZ3JhdmVTb2lsJ10sIG1pbkRpc3RhbmNlOiA1IH0gfSwgY2FuZGlkYXRlID0+IChjYW5kaWRhdGUueCAhPT0gZmxvb3IuZXhpdC54IHx8IGNhbmRpZGF0ZS55ICE9PSBmbG9vci5leGl0LnkpICYmIChjYW5kaWRhdGUueCAhPT0gZmxvb3Iuc3RhcnQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3Iuc3RhcnQueSkpXG4gICAgaWYgKCFwb2ludCkgdGhyb3cgbmV3IEVycm9yKGBmYWlsZWQgcGxhY2VtZW50IEJ1cmlhbCBwcm9jZXNzaW9uIHdhcmRlbjogJHtydW50aW1lLmRpYWdub3N0aWNzLmF0KC0xKT8uZGlhZ25vc3RpY3Muam9pbignOyAnKX1gKVxuICAgIGNvbnN0IGVuY291bnRlciA9IHsgaWQ6IGB0YWN0aWNhbDoke2Zsb29yLmluZGV4fTpwcm9jZXNzaW9uLXdhcmRlbmAsIGFyY2hldHlwZTogJ25hdGl2ZVRlcnJhaW5QYWNrJyBhcyBjb25zdCwgbGVhZGVyOiB0cnVlLCBhbnN3ZXI6ICdsZWF2ZSB0aGUgZ3JhdmUgbGFuZSBvciBmb2xsb3cgdGhlIGxpdCBzcGlyaXQgcGF0aCcgfVxuICAgIGNvbnN0IGFjdG9yID0gc3Bhd25Nb25zdGVyKHRvbWJXYXJkZW4uaWQsIHBvaW50LCBgJHt0b21iV2FyZGVuLmlkfS1wcm9jZXNzaW9uLXdhcmRlbmAsIGZsb29yLmRpZmZpY3VsdHkpXG4gICAgYWN0b3IuZW5jb3VudGVyID0gZW5jb3VudGVyXG4gICAgZmxvb3IuYWN0b3JzLnB1c2goYWN0b3IpXG4gICAgZGlyZWN0ZWQucHVzaChlbmNvdW50ZXIpXG4gIH1cbiAgaWYgKGZsb29yLmJpb21lID09PSAnc2FsdEZsYXRzJyAmJiBhcmVhRmxvb3IgIT09IDMgJiYgIWZsb29yLmFjdG9ycy5zb21lKGFjdG9yID0+IGFjdG9yLmtpbmQgPT09ICdtaXJhZ2VTa2lybWlzaGVyJyAmJiBhY3Rvci50ZXJyYWluQWZmaW5pdHk/LmluY2x1ZGVzKGdldFRpbGUoZmxvb3IsIGFjdG9yLngsIGFjdG9yLnkpPy5raW5kID8/ICd3YWxsJykpKSB7XG4gICAgY29uc3QgbWlyYWdlU2tpcm1pc2hlciA9IGRlZmluaXRpb25zLmZpbmQoZGVmaW5pdGlvbiA9PiBkZWZpbml0aW9uLmlkID09PSAnbWlyYWdlU2tpcm1pc2hlcicpXG4gICAgaWYgKCFtaXJhZ2VTa2lybWlzaGVyKSB0aHJvdyBuZXcgRXJyb3IoJ1NhbHQgaG9yaXpvbiBsYWNrcyBtaXJhZ2Ugc2tpcm1pc2hlcicpXG4gICAgY29uc3QgcG9pbnQgPSBjaG9vc2VQbGFjZW1lbnQoZmxvb3IsIHJ1bnRpbWUsIHsgaWQ6IGBhY3Rvcjp0YWN0aWNhbDoke2Zsb29yLmluZGV4fTptaXJyb3ItaG9yaXpvbjptaXJhZ2VTa2lybWlzaGVyYCwgcmVxdWlyZW1lbnRzOiB7IHRlcnJhaW46IFsnc2FsdE1pcnJvciddLCBub2RlS2luZHM6IFsnb3B0aW9uYWxSZXdhcmQnXSwgbWluRGlzdGFuY2U6IDgsIGNvdmVyOiBmYWxzZSB9LCBmYWxsYmFjazogeyB0ZXJyYWluOiBbJ3NhbHRNaXJyb3InXSwgbWluRGlzdGFuY2U6IDUgfSB9LCBjYW5kaWRhdGUgPT4gKGNhbmRpZGF0ZS54ICE9PSBmbG9vci5leGl0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLmV4aXQueSkgJiYgKGNhbmRpZGF0ZS54ICE9PSBmbG9vci5zdGFydC54IHx8IGNhbmRpZGF0ZS55ICE9PSBmbG9vci5zdGFydC55KSlcbiAgICBpZiAoIXBvaW50KSB0aHJvdyBuZXcgRXJyb3IoYGZhaWxlZCBwbGFjZW1lbnQgU2FsdCBtaXJyb3IgaG9yaXpvbjogJHtydW50aW1lLmRpYWdub3N0aWNzLmF0KC0xKT8uZGlhZ25vc3RpY3Muam9pbignOyAnKX1gKVxuICAgIGNvbnN0IGVuY291bnRlciA9IHsgaWQ6IGB0YWN0aWNhbDoke2Zsb29yLmluZGV4fTptaXJyb3ItaG9yaXpvbmAsIGFyY2hldHlwZTogJ2FydGlsbGVyeUNvdmVyJyBhcyBjb25zdCwgbGVhZGVyOiB0cnVlLCBhbnN3ZXI6ICdicmVhayB0aGUgbWlycm9yIHNpZ2h0bGluZSBvciByZWFjaCB0aGUgY2FyYXZhbiByZWZ1Z2UnIH1cbiAgICBjb25zdCBhY3RvciA9IHNwYXduTW9uc3RlcihtaXJhZ2VTa2lybWlzaGVyLmlkLCBwb2ludCwgYCR7bWlyYWdlU2tpcm1pc2hlci5pZH0tbWlycm9yLWhvcml6b25gLCBmbG9vci5kaWZmaWN1bHR5KVxuICAgIGFjdG9yLmVuY291bnRlciA9IGVuY291bnRlclxuICAgIGZsb29yLmFjdG9ycy5wdXNoKGFjdG9yKVxuICAgIGRpcmVjdGVkLnB1c2goZW5jb3VudGVyKVxuICB9XG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2Zyb3N0UmVsaXF1YXJ5JyAmJiBhcmVhRmxvb3IgIT09IDMgJiYgIWZsb29yLmFjdG9ycy5zb21lKGFjdG9yID0+IGFjdG9yLmtpbmQgPT09ICdzaGFyZEhvdW5kJyAmJiBhY3Rvci50ZXJyYWluQWZmaW5pdHk/LmluY2x1ZGVzKGdldFRpbGUoZmxvb3IsIGFjdG9yLngsIGFjdG9yLnkpPy5raW5kID8/ICd3YWxsJykpKSB7XG4gICAgY29uc3Qgc2hhcmRIb3VuZCA9IGRlZmluaXRpb25zLmZpbmQoZGVmaW5pdGlvbiA9PiBkZWZpbml0aW9uLmlkID09PSAnc2hhcmRIb3VuZCcpXG4gICAgaWYgKCFzaGFyZEhvdW5kKSB0aHJvdyBuZXcgRXJyb3IoJ0Zyb3N0IEJhc2luIGxhY2tzIHNoYXJkIGhvdW5kJylcbiAgICBjb25zdCBwb2ludCA9IGNob29zZVBsYWNlbWVudChmbG9vciwgcnVudGltZSwgeyBpZDogYGFjdG9yOnRhY3RpY2FsOiR7Zmxvb3IuaW5kZXh9OmljZS1wdXJzdWl0OnNoYXJkSG91bmRgLCByZXF1aXJlbWVudHM6IHsgdGVycmFpbjogWydpY2UnXSwgZWRnZU1vZGVzOiBbJ2Nvc3RseScsICdvcHRpb25hbCddLCBvcHRpb25hbDogdHJ1ZSwgbWluRGlzdGFuY2U6IDgsIGNvdmVyOiBmYWxzZSB9LCBmYWxsYmFjazogeyB0ZXJyYWluOiBbJ2ljZSddLCBtaW5EaXN0YW5jZTogNSB9IH0sIGNhbmRpZGF0ZSA9PiAoY2FuZGlkYXRlLnggIT09IGZsb29yLmV4aXQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3IuZXhpdC55KSAmJiAoY2FuZGlkYXRlLnggIT09IGZsb29yLnN0YXJ0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLnN0YXJ0LnkpKVxuICAgIGlmICghcG9pbnQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIHBsYWNlbWVudCBGcm9zdCBpY2UgcHVyc3VpdDogJHtydW50aW1lLmRpYWdub3N0aWNzLmF0KC0xKT8uZGlhZ25vc3RpY3Muam9pbignOyAnKX1gKVxuICAgIGNvbnN0IGVuY291bnRlciA9IHsgaWQ6IGB0YWN0aWNhbDoke2Zsb29yLmluZGV4fTppY2UtcHVyc3VpdGAsIGFyY2hldHlwZTogJ3B1cnN1aXRMYW5lJyBhcyBjb25zdCwgbGVhZGVyOiB0cnVlLCBhbnN3ZXI6ICdsZWF2ZSB0aGUgaWNlIGxhbmUgb3IgcmVhY2ggdGhlIG1hcmtlZCB3aW5kIHNoZWx0ZXInIH1cbiAgICBjb25zdCBhY3RvciA9IHNwYXduTW9uc3RlcihzaGFyZEhvdW5kLmlkLCBwb2ludCwgYCR7c2hhcmRIb3VuZC5pZH0taWNlLXB1cnN1aXRgLCBmbG9vci5kaWZmaWN1bHR5KVxuICAgIGFjdG9yLmVuY291bnRlciA9IGVuY291bnRlclxuICAgIGZsb29yLmFjdG9ycy5wdXNoKGFjdG9yKVxuICAgIGRpcmVjdGVkLnB1c2goZW5jb3VudGVyKVxuICB9XG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2Zyb3N0UmVsaXF1YXJ5JyAmJiBhcmVhRmxvb3IgIT09IDMgJiYgIWZsb29yLmFjdG9ycy5zb21lKGFjdG9yID0+IGFjdG9yLmtpbmQgPT09ICd3aGl0ZW91dE9yYWNsZScgJiYgYWN0b3IudGVycmFpbkFmZmluaXR5Py5pbmNsdWRlcyhnZXRUaWxlKGZsb29yLCBhY3Rvci54LCBhY3Rvci55KT8ua2luZCA/PyAnd2FsbCcpKSkge1xuICAgIGNvbnN0IHdoaXRlb3V0T3JhY2xlID0gZGVmaW5pdGlvbnMuZmluZChkZWZpbml0aW9uID0+IGRlZmluaXRpb24uaWQgPT09ICd3aGl0ZW91dE9yYWNsZScpXG4gICAgaWYgKCF3aGl0ZW91dE9yYWNsZSkgdGhyb3cgbmV3IEVycm9yKCdGcm9zdCBCYXNpbiBsYWNrcyB3aGl0ZW91dCBvcmFjbGUnKVxuICAgIGNvbnN0IHBvaW50ID0gY2hvb3NlUGxhY2VtZW50KGZsb29yLCBydW50aW1lLCB7IGlkOiBgYWN0b3I6dGFjdGljYWw6JHtmbG9vci5pbmRleH06d2hpdGVvdXQtd2F0Y2g6d2hpdGVvdXRPcmFjbGVgLCByZXF1aXJlbWVudHM6IHsgdGVycmFpbjogWydpY2UnXSwgZWRnZU1vZGVzOiBbJ2Nvc3RseScsICdvcHRpb25hbCddLCBvcHRpb25hbDogdHJ1ZSwgbWluRGlzdGFuY2U6IDgsIGNvdmVyOiBmYWxzZSB9LCBmYWxsYmFjazogeyB0ZXJyYWluOiBbJ2ljZSddLCBtaW5EaXN0YW5jZTogNSB9IH0sIGNhbmRpZGF0ZSA9PiAoY2FuZGlkYXRlLnggIT09IGZsb29yLmV4aXQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3IuZXhpdC55KSAmJiAoY2FuZGlkYXRlLnggIT09IGZsb29yLnN0YXJ0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLnN0YXJ0LnkpKVxuICAgIGlmICghcG9pbnQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIHBsYWNlbWVudCBGcm9zdCB3aGl0ZW91dCB3YXRjaDogJHtydW50aW1lLmRpYWdub3N0aWNzLmF0KC0xKT8uZGlhZ25vc3RpY3Muam9pbignOyAnKX1gKVxuICAgIGNvbnN0IGVuY291bnRlciA9IHsgaWQ6IGB0YWN0aWNhbDoke2Zsb29yLmluZGV4fTp3aGl0ZW91dC13YXRjaGAsIGFyY2hldHlwZTogJ2FydGlsbGVyeUNvdmVyJyBhcyBjb25zdCwgbGVhZGVyOiB0cnVlLCBhbnN3ZXI6ICdmb2xsb3cgdGhlIG1hcmtlZCBzaG9yZSBhbmQgYnJlYWsgdGhlIG9yYWNsZSBzaWdodGxpbmUnIH1cbiAgICBjb25zdCBhY3RvciA9IHNwYXduTW9uc3Rlcih3aGl0ZW91dE9yYWNsZS5pZCwgcG9pbnQsIGAke3doaXRlb3V0T3JhY2xlLmlkfS13aGl0ZW91dC13YXRjaGAsIGZsb29yLmRpZmZpY3VsdHkpXG4gICAgYWN0b3IuZW5jb3VudGVyID0gZW5jb3VudGVyXG4gICAgZmxvb3IuYWN0b3JzLnB1c2goYWN0b3IpXG4gICAgZGlyZWN0ZWQucHVzaChlbmNvdW50ZXIpXG4gIH1cbiAgaWYgKGZsb29yLmluZGV4ICUgNCA9PT0gMykge1xuICAgIGNvbnN0IGd1YXJkaWFuID0gZGVmaW5pdGlvbnMuZmluZChtb25zdGVyID0+IG1vbnN0ZXIuYWkgPT09ICdndWFyZGlhbicpIVxuICAgIGNvbnN0IGFjdG9yID0gc3Bhd25Nb25zdGVyKGd1YXJkaWFuLmlkLCBmbG9vci5leGl0LCBgJHtndWFyZGlhbi5pZH0tOTlgLCBmbG9vci5kaWZmaWN1bHR5KVxuICAgIGFjdG9yLnN0YXR1cyA9IFsuLi4oYWN0b3Iuc3RhdHVzID8/IFtdKSwgYGFyYzoke2Zsb29yLmVzY2FsYXRpb24/LmFyY0lkID8/ICdsZWdhY3knfTpyZXNvbHZlZGBdXG4gICAgZmxvb3IuYWN0b3JzLnB1c2goYWN0b3IpXG4gIH1cbiAgdGFjdGljYWxFbmNvdW50ZXJEZWJ1Z3Muc2V0KGZsb29yLCBkaXJlY3RlZClcbn1cblxuZnVuY3Rpb24gcGxhY2VFY29sb2d5KGZsb29yOiBGbG9vciwgcnVudGltZTogUGxhY2VtZW50UnVudGltZSk6IHZvaWQge1xuICBjb25zdCBwcm9maWxlID0gZWNvbG9neVByb2ZpbGVGb3IoZmxvb3IuYmlvbWUpXG4gIGNvbnN0IHByZXNzdXJlUm91dGUgPSAoZmxvb3IuYmlvbWUgPT09ICdjYXZlcm5zJyB8fCBmbG9vci5iaW9tZSA9PT0gJ3dpbGRzJyB8fCBmbG9vci5iaW9tZSA9PT0gJ3J1aW5zJyB8fCBmbG9vci5iaW9tZSA9PT0gJ2Z1cm5hY2UnIHx8IGZsb29yLmJpb21lID09PSAnZmxvb2RlZFJ1aW5zJyB8fCBmbG9vci5iaW9tZSA9PT0gJ2NsaWZmcycgfHwgZmxvb3IuYmlvbWUgPT09ICdidXJpYWwnIHx8IGZsb29yLmJpb21lID09PSAnc2FsdEZsYXRzJyB8fCBmbG9vci5iaW9tZSA9PT0gJ2Zyb3N0UmVsaXF1YXJ5JykgJiYgcnVudGltZS5waWxvdFxuICBjb25zdCBjb250cmFjdDogUGxhY2VtZW50Q29udHJhY3QgPSBwcmVzc3VyZVJvdXRlXG4gICAgPyB7IGlkOiBgZWNvbG9neToke3Byb2ZpbGUua2luZH1gLCByZXF1aXJlbWVudHM6IHsgdGVycmFpbjogZmxvb3IuYmlvbWUgPT09ICdydWlucycgPyBbJ2RhcnQnXSA6IGZsb29yLmJpb21lID09PSAnZnVybmFjZScgPyBbJ3Ntb2tlJ10gOiBmbG9vci5iaW9tZSA9PT0gJ2Zsb29kZWRSdWlucycgPyBbJ2N1cnJlbnQnXSA6IGZsb29yLmJpb21lID09PSAnY2xpZmZzJyA/IFsnbGVkZ2UnXSA6IGZsb29yLmJpb21lID09PSAnYnVyaWFsJyA/IFsnZ3JhdmVTb2lsJ10gOiBmbG9vci5iaW9tZSA9PT0gJ3NhbHRGbGF0cycgPyBbJ3NhbHRNaXJyb3InXSA6IGZsb29yLmJpb21lID09PSAnZnJvc3RSZWxpcXVhcnknID8gWydpY2UnXSA6IFsnd2F0ZXInXSwgZWRnZU1vZGVzOiBbJ2Nvc3RseScsICdvcHRpb25hbCddLCBvcHRpb25hbDogdHJ1ZSwgbWluRGlzdGFuY2U6IDcgfSwgZmFsbGJhY2s6IHsgdGVycmFpbjogZmxvb3IuYmlvbWUgPT09ICdydWlucycgPyBbJ2RhcnQnXSA6IGZsb29yLmJpb21lID09PSAnZnVybmFjZScgPyBbJ3Ntb2tlJ10gOiBmbG9vci5iaW9tZSA9PT0gJ2Zsb29kZWRSdWlucycgPyBbJ2N1cnJlbnQnXSA6IGZsb29yLmJpb21lID09PSAnY2xpZmZzJyA/IFsnbGVkZ2UnXSA6IGZsb29yLmJpb21lID09PSAnYnVyaWFsJyA/IFsnZ3JhdmVTb2lsJ10gOiBmbG9vci5iaW9tZSA9PT0gJ3NhbHRGbGF0cycgPyBbJ3NhbHRNaXJyb3InXSA6IGZsb29yLmJpb21lID09PSAnZnJvc3RSZWxpcXVhcnknID8gWydpY2UnXSA6IFsnd2F0ZXInXSwgbWluRGlzdGFuY2U6IDcgfSB9XG4gICAgOiB7IGlkOiBgZWNvbG9neToke3Byb2ZpbGUua2luZH1gLCByZXF1aXJlbWVudHM6IHsgdGVycmFpbjogcHJvZmlsZS50ZXJyYWluLCBtaW5EaXN0YW5jZTogNywgY2hva2Vwb2ludDogZmFsc2UgfSwgZmFsbGJhY2s6IHsgdGVycmFpbjogWydmbG9vciddLCBtaW5EaXN0YW5jZTogNywgY2hva2Vwb2ludDogZmFsc2UgfSB9XG4gIGNvbnN0IHBvaW50ID0gY2hvb3NlUGxhY2VtZW50KGZsb29yLCBydW50aW1lLCBjb250cmFjdCwgY2FuZGlkYXRlID0+IChjYW5kaWRhdGUueCAhPT0gZmxvb3Iuc3RhcnQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3Iuc3RhcnQueSkgJiYgKGNhbmRpZGF0ZS54ICE9PSBmbG9vci5leGl0LnggfHwgY2FuZGlkYXRlLnkgIT09IGZsb29yLmV4aXQueSkpXG4gIGlmICghcG9pbnQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIHBsYWNlbWVudCAke2NvbnRyYWN0LmlkfTogJHtydW50aW1lLmRpYWdub3N0aWNzLmF0KC0xKT8uZGlhZ25vc3RpY3Muam9pbignOyAnKX1gKVxuICBjb25zdCBzb3VyY2UgPSBmbG9vci5hY3RvcnMuZmluZChhY3RvciA9PiBhY3Rvci5ob3N0aWxlICYmIChhY3Rvci5jb21iYXRSb2xlID09PSAnZ3VhcmQnIHx8IGFjdG9yLmNvbWJhdFJvbGUgPT09ICdwdXJzdWVyJykpXG4gIGNvbnN0IG5vZGUgPSBydW50aW1lLnBpbG90ID8gcnVudGltZS5tYWNyby5ub2Rlcy5maW5kKGNhbmRpZGF0ZSA9PiBwb2ludC54ID49IGNhbmRpZGF0ZS5mb290cHJpbnQueCAmJiBwb2ludC54IDwgY2FuZGlkYXRlLmZvb3RwcmludC54ICsgY2FuZGlkYXRlLmZvb3RwcmludC53aWR0aCAmJiBwb2ludC55ID49IGNhbmRpZGF0ZS5mb290cHJpbnQueSAmJiBwb2ludC55IDwgY2FuZGlkYXRlLmZvb3RwcmludC55ICsgY2FuZGlkYXRlLmZvb3RwcmludC5oZWlnaHQpPy5ub2RlSWQgOiB1bmRlZmluZWRcbiAgY29uc3Qgcm91dGUgPSBydW50aW1lLnBpbG90ID8gcnVudGltZS5tYWNyby5lZGdlcy5maW5kKGVkZ2UgPT4gZWRnZS5jZWxscy5zb21lKGNlbGwgPT4gY2VsbC54ID09PSBwb2ludC54ICYmIGNlbGwueSA9PT0gcG9pbnQueSkpPy5tb2Rlc1swXSA6IHVuZGVmaW5lZFxuICBjb25zdCBlY29sb2d5ID0gZWNvbG9neUV2ZW50Rm9yKGZsb29yLCBwb2ludCwgc291cmNlPy5pZCA/PyBgJHtmbG9vci5iaW9tZX06JHtwcm9maWxlLmtpbmR9YCwgbm9kZSwgcm91dGUpXG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2NsaWZmcycgJiYgcnVudGltZS5waWxvdCkge1xuICAgIGNvbnN0IHNxdWFsbCA9IFt7IGRpcmVjdGlvbjogJ24nIGFzIGNvbnN0LCB4OiAwLCB5OiAtMSB9LCB7IGRpcmVjdGlvbjogJ2UnIGFzIGNvbnN0LCB4OiAxLCB5OiAwIH0sIHsgZGlyZWN0aW9uOiAncycgYXMgY29uc3QsIHg6IDAsIHk6IDEgfSwgeyBkaXJlY3Rpb246ICd3JyBhcyBjb25zdCwgeDogLTEsIHk6IDAgfV0uZmluZChkZWx0YSA9PiB7XG4gICAgICBjb25zdCB0YXJnZXQgPSBnZXRUaWxlKGZsb29yLCBwb2ludC54ICsgZGVsdGEueCwgcG9pbnQueSArIGRlbHRhLnkpXG4gICAgICByZXR1cm4gcGFzc2FibGUodGFyZ2V0Py5raW5kID8/ICd3YWxsJykgJiYgIXRhcmdldD8uZmxvd1xuICAgIH0pXG4gICAgaWYgKCFzcXVhbGwpIHRocm93IG5ldyBFcnJvcihgbWlzc2luZyBDbGlmZnMgc3F1YWxsIGxhbmRpbmc6ICR7Zmxvb3IubGF5b3V0SWR9YClcbiAgICBjb25zdCB0aWVPZmYgPSBnZXRUaWxlKGZsb29yLCBwb2ludC54ICsgc3F1YWxsLngsIHBvaW50LnkgKyBzcXVhbGwueSkhXG4gICAgdGllT2ZmLmtpbmQgPSAncm9wZSdcbiAgICB0aWVPZmYuZWxldmF0aW9uID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCwgcG9pbnQueSk/LmVsZXZhdGlvblxuICAgIGRlbGV0ZSB0aWVPZmYuZmxvd1xuICAgIGVjb2xvZ3kuZWZmZWN0RmxvdyA9IHsgZGlyZWN0aW9uOiBzcXVhbGwuZGlyZWN0aW9uLCBoYXphcmQ6ICdzcXVhbGwnIH1cbiAgfVxuICBpZiAoZmxvb3IuZXNjYWxhdGlvbikge1xuICAgIGVjb2xvZ3kud2FybmluZyA9IGAke2Zsb29yLmVzY2FsYXRpb24uZWNvbG9neX06ICR7ZWNvbG9neS53YXJuaW5nfWBcbiAgICBlY29sb2d5LnJlc3BvbnNlcyA9IFtgJHtmbG9vci5lc2NhbGF0aW9uLnBoYXNlfTogJHtmbG9vci5lc2NhbGF0aW9uLnByb21pc2V9YCwgLi4uZWNvbG9neS5yZXNwb25zZXNdXG4gICAgaWYgKGZsb29yLmVzY2FsYXRpb24ucGhhc2UgPT09ICdjbGltYXgnKSB7IGVjb2xvZ3kuc3RhcnRzQXQgPSBNYXRoLm1heCgyLCBlY29sb2d5LnN0YXJ0c0F0IC0gMSk7IGVjb2xvZ3kuZHVyYXRpb24rKyB9XG4gIH1cbiAgY29uc3QgZWNvbG9naWVzID0gW2Vjb2xvZ3ldXG4gIGlmIChmbG9vci5iaW9tZSA9PT0gJ2J1cmlhbCcpIGZvciAoY29uc3QgW2luZGV4LCBwcm9jZXNzaW9uXSBvZiAoZmxvb3IuYnVyaWFsTGF5b3V0Py5wcm9jZXNzaW9ucyA/PyBbXSkuc2xpY2UoMSkuZW50cmllcygpKSB7XG4gICAgY29uc3QgdGFyZ2V0ID0gcHJvY2Vzc2lvbi5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUueCAhPT0gcG9pbnQueCB8fCBjYW5kaWRhdGUueSAhPT0gcG9pbnQueSlcbiAgICBpZiAoIXRhcmdldCkgY29udGludWVcbiAgICBjb25zdCBwcm9jZXNzaW9uRWNvbG9neSA9IGVjb2xvZ3lFdmVudEZvcihmbG9vciwgdGFyZ2V0LCBzb3VyY2U/LmlkID8/IGAke2Zsb29yLmJpb21lfToke3Byb2ZpbGUua2luZH1gLCBub2RlLCAnY29zdGx5JylcbiAgICBwcm9jZXNzaW9uRWNvbG9neS5zdGFydHNBdCA9IGVjb2xvZ3kuc3RhcnRzQXQgKyAoaW5kZXggKyAxKSAqIDJcbiAgICBwcm9jZXNzaW9uRWNvbG9neS5kdXJhdGlvbiA9IGVjb2xvZ3kuZHVyYXRpb25cbiAgICBwcm9jZXNzaW9uRWNvbG9neS53YXJuaW5nID0gZWNvbG9neS53YXJuaW5nXG4gICAgcHJvY2Vzc2lvbkVjb2xvZ3kucmVzcG9uc2VzID0gWy4uLmVjb2xvZ3kucmVzcG9uc2VzXVxuICAgIGVjb2xvZ2llcy5wdXNoKHByb2Nlc3Npb25FY29sb2d5KVxuICB9XG4gIGZsb29yLmVjb2xvZ3kgPSBlY29sb2dpZXNcbn1cblxuZnVuY3Rpb24gcGxhY2VJdGVtcyhmbG9vcjogRmxvb3IsIHJuZzogUm5nLCBfcm9vbXM6IFJvb21bXSwgcnVudGltZTogUGxhY2VtZW50UnVudGltZSk6IHZvaWQge1xuICBjb25zdCB2YWx1ZUNhcCA9IDEwNSArIChmbG9vci5kaWZmaWN1bHR5Py50aHJlYXQgPz8gMCkgKiAxNFxuICBjb25zdCBlbGlnaWJsZSA9IElURU1TLmZpbHRlcihpdGVtID0+IGl0ZW0uZmluZGFibGUgIT09IGZhbHNlICYmIGl0ZW0udmFsdWUgPD0gdmFsdWVDYXAgJiYgKCFpdGVtLnNsb3QgfHwgcm5nLmNoYW5jZSgzMCArIChmbG9vci5kaWZmaWN1bHR5Py5yb3V0ZVBvc2l0aW9uID8/IDApICogOCkpKVxuICBjb25zdCBsb290ID0gZWxpZ2libGUubGVuZ3RoID8gZWxpZ2libGUgOiBJVEVNUy5maWx0ZXIoaXRlbSA9PiBpdGVtLmZpbmRhYmxlICE9PSBmYWxzZSAmJiAoIWl0ZW0uc2xvdCB8fCBybmcuY2hhbmNlKDMwKSkpXG4gIGNvbnN0IGNvdW50ID0gTWF0aC5yb3VuZCgoMTAgKyBmbG9vci5pbmRleCAlIDQgKiAyICsgTWF0aC5mbG9vcigoZmxvb3IuZGlmZmljdWx0eT8ucm91dGVQb3NpdGlvbiA/PyAwKSAvIDIpKSAqIChmbG9vci5kaWZmaWN1bHR5Py5yZXdhcmRNdWx0aXBsaWVyID8/IDEpKVxuICBjb25zdCB0b29sID0gb3B0aW9uYWxUZXJyYWluVG9vbEZvcih7IHNlZWQ6IGZsb29yLnNlZWQsIGZsb29ySW5kZXg6IGZsb29yLmluZGV4LCBiaW9tZTogZmxvb3IuYmlvbWUsIHJlY2lwZUlkOiBmbG9vci5sYXlvdXRJZCwgc291cmNlOiAnbG9vdCcgfSlcbiAgZm9yIChsZXQgaSA9IDA7IGkgPCBjb3VudDsgaSsrKSB7XG4gICAgY29uc3QgY29udHJhY3Q6IFBsYWNlbWVudENvbnRyYWN0ID0gcnVudGltZS5waWxvdCAmJiBpID4gMFxuICAgICAgPyB7IGlkOiBgbG9vdDoke2l9YCwgcmVxdWlyZW1lbnRzOiB7IG5vZGVLaW5kczogWydvcHRpb25hbFJld2FyZCddLCBtaW5EaXN0YW5jZTogNyB9LCBmYWxsYmFjazogeyB0ZXJyYWluOiBbJ2Zsb29yJ10sIG1pbkRpc3RhbmNlOiA0IH0gfVxuICAgICAgOiB7IGlkOiBgbG9vdDoke2l9YCwgcmVxdWlyZW1lbnRzOiB7IHRlcnJhaW46IFsnZmxvb3InXSwgbWluRGlzdGFuY2U6IDQgfSB9XG4gICAgY29uc3QgcG9pbnQgPSBjaG9vc2VQbGFjZW1lbnQoZmxvb3IsIHJ1bnRpbWUsIGNvbnRyYWN0LCBjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLnggIT09IGZsb29yLmV4aXQueCB8fCBjYW5kaWRhdGUueSAhPT0gZmxvb3IuZXhpdC55KVxuICAgIGlmICghcG9pbnQpIHRocm93IG5ldyBFcnJvcihgZmFpbGVkIHBsYWNlbWVudCAke2NvbnRyYWN0LmlkfTogJHtydW50aW1lLmRpYWdub3N0aWNzLmF0KC0xKT8uZGlhZ25vc3RpY3Muam9pbignOyAnKX1gKVxuICAgIGNvbnN0IGlkID0gaSA9PT0gMCAmJiBmbG9vci5pbmRleCAlIDQgPT09IDAgPyAna2V5JyA6IHRvb2wgJiYgaSA9PT0gY291bnQgLSAxID8gJ3JvY2snIDogcm5nLnBpY2sobG9vdCkuaWRcbiAgICBmbG9vci5pdGVtcy5wdXNoKHsgaWQsIHg6IHBvaW50LngsIHk6IHBvaW50LnksIGNvdW50OiAxLCAuLi4odG9vbCAmJiBpID09PSBjb3VudCAtIDEgPyB7IHRvb2wgfSA6IHt9KSB9KVxuICB9XG59XG5cbmZ1bmN0aW9uIGZyZWVSb29tUG9pbnQoZmxvb3I6IEZsb29yLCBybmc6IFJuZywgcm9vbXM6IFJvb21bXSwgcmVzZXJ2ZWQ6IFJlYWRvbmx5U2V0PG51bWJlcj4gPSBuZXcgU2V0KCkpOiBQb2ludCB7XG4gIGZvciAobGV0IHRyaWVzID0gMDsgdHJpZXMgPCAyMDA7IHRyaWVzKyspIHtcbiAgICBjb25zdCByb29tID0gcm5nLnBpY2socm9vbXMpXG4gICAgY29uc3QgcG9pbnQgPSB7IHg6IHJuZy5pbnQocm9vbS54ICsgMSwgcm9vbS54ICsgcm9vbS53IC0gMiksIHk6IHJuZy5pbnQocm9vbS55ICsgMSwgcm9vbS55ICsgcm9vbS5oIC0gMikgfVxuICAgIGNvbnN0IGN1cnJlbnQgPSBnZXRUaWxlKGZsb29yLCBwb2ludC54LCBwb2ludC55KVxuICAgIGlmIChjdXJyZW50Py5raW5kID09PSAnZmxvb3InICYmICFyZXNlcnZlZC5oYXMoaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpICYmICFhY3RvckF0KGZsb29yLCBwb2ludC54LCBwb2ludC55KSAmJiAhZmxvb3IuaXRlbXMuc29tZShpdGVtID0+IGl0ZW0ueCA9PT0gcG9pbnQueCAmJiBpdGVtLnkgPT09IHBvaW50LnkpICYmIGRpc3RhbmNlKHBvaW50LCBmbG9vci5zdGFydCkgPiA0KSByZXR1cm4gcG9pbnRcbiAgfVxuICByZXR1cm4geyAuLi5mbG9vci5zdGFydCB9XG59XG5cbmV4cG9ydCBjb25zdCBzcGF3bk1vbnN0ZXIgPSAoa2luZDogc3RyaW5nLCBwb2ludDogUG9pbnQsIGlkOiBzdHJpbmcsIGRpZmZpY3VsdHk/OiBEaWZmaWN1bHR5Q29udGV4dCk6IEFjdG9yID0+IHtcbiAgY29uc3QgZGVmaW5pdGlvbiA9IG1vbnN0ZXJCeUlkKGtpbmQpXG4gIGlmICghZGVmaW5pdGlvbikgdGhyb3cgbmV3IEVycm9yKGB1bmtub3duIG1vbnN0ZXI6ICR7a2luZH1gKVxuICBjb25zdCBoZWFsdGhNdWx0aXBsaWVyID0gZGVmaW5pdGlvbi5haSA9PT0gJ2d1YXJkaWFuJyA/IDEgKyAoZGlmZmljdWx0eT8udGhyZWF0ID8/IDApICogMC4wOCA6IGRpZmZpY3VsdHk/LmhlYWx0aE11bHRpcGxpZXIgPz8gMVxuICBjb25zdCBtYXhIZWFsdGggPSBNYXRoLm1heCgxLCBNYXRoLnJvdW5kKGRlZmluaXRpb24uaGVhbHRoICogaGVhbHRoTXVsdGlwbGllcikpXG4gIHJldHVybiB7IGlkLCByb2xlOiBkZWZpbml0aW9uLmFpID09PSAnZ3VhcmRpYW4nID8gJ2d1YXJkaWFuJyA6ICdtb25zdGVyJywga2luZDogZGVmaW5pdGlvbi5pZCwgbmFtZTogZGVmaW5pdGlvbi5uYW1lLCB4OiBwb2ludC54LCB5OiBwb2ludC55LCBoZWFsdGg6IG1heEhlYWx0aCwgbWF4SGVhbHRoLCBhdHRhY2s6IGRlZmluaXRpb24uYXR0YWNrICsgKGRpZmZpY3VsdHk/LmF0dGFja0JvbnVzID8/IDApLCBkZWZlbnNlOiBkZWZpbml0aW9uLmRlZmVuc2UgKyAoZGlmZmljdWx0eT8uZGVmZW5zZUJvbnVzID8/IDApLCBzcGVlZDogZGVmaW5pdGlvbi5zcGVlZCwgZW5lcmd5OiAwLCBnbHlwaDogZGVmaW5pdGlvbi5nbHlwaCwgY29sb3I6IGRlZmluaXRpb24uY29sb3IsIGhvc3RpbGU6IHRydWUsIGFpOiBkZWZpbml0aW9uLmFpLCBjb21iYXRSb2xlOiBtb25zdGVyUm9sZUZvcihkZWZpbml0aW9uKSwgdGFnczogWy4uLihkZWZpbml0aW9uLnRhZ3MgPz8gW10pXSwgdGVycmFpbkFmZmluaXR5OiB0ZXJyYWluQWZmaW5pdHlGb3IoZGVmaW5pdGlvbiksIGNvbmRpdGlvbnM6IFtdLCAuLi4oZGVmaW5pdGlvbi5haSA9PT0gJ2d1YXJkaWFuJyA/IHsgZ3VhcmRpYW5QaGFzZTogJ29wZW5pbmcnIGFzIGNvbnN0LCBzdGF0dXM6IGRpZmZpY3VsdHk/Lmd1YXJkaWFuUGF0dGVybiA/IFtgcGF0dGVybjoke2RpZmZpY3VsdHkuZ3VhcmRpYW5QYXR0ZXJufWBdIDogW10gfSA6IHt9KSB9XG59XG5cbmZ1bmN0aW9uIGZyaWVuZGx5KHJvbGU6ICdtZXJjaGFudCcgfCAnYWxseScsIG5hbWU6IHN0cmluZywgcG9pbnQ6IFBvaW50LCBnbHlwaDogc3RyaW5nLCBjb2xvcjogc3RyaW5nKTogQWN0b3Ige1xuICByZXR1cm4geyBpZDogYCR7cm9sZX0tJHtwb2ludEtleShwb2ludCl9YCwgcm9sZSwga2luZDogcm9sZSwgbmFtZSwgeDogcG9pbnQueCwgeTogcG9pbnQueSwgaGVhbHRoOiA5OSwgbWF4SGVhbHRoOiA5OSwgYXR0YWNrOiAwLCBkZWZlbnNlOiA5OSwgc3BlZWQ6IDAsIGVuZXJneTogMCwgZ2x5cGgsIGNvbG9yLCBob3N0aWxlOiBmYWxzZSwgY29uZGl0aW9uczogW10gfVxufVxuXG5jb25zdCBkaXN0YW5jZSA9IChhOiBQb2ludCwgYjogUG9pbnQpOiBudW1iZXIgPT4gTWF0aC5hYnMoYS54IC0gYi54KSArIE1hdGguYWJzKGEueSAtIGIueSlcblxuZXhwb3J0IGludGVyZmFjZSBHZW5lcmF0aW9uVmFsaWRhdGlvbiB7IHZhbGlkOiBib29sZWFuOyBlcnJvcnM6IHN0cmluZ1tdIH1cblxuY29uc3QgcmVhY2hhYmxlSW5kZXhlcyA9IChmbG9vcjogRmxvb3IpOiBTZXQ8bnVtYmVyPiA9PiByZWFjaGFibGVGbG9vckluZGV4ZXMoZmxvb3IpXG5cbmNvbnN0IG9iamVjdGl2ZVRhcmdldHMgPSAoZmxvb3I6IEZsb29yKTogUG9pbnRbXSA9PiB7XG4gIGlmIChmbG9vci5vYmplY3RpdmUua2luZCA9PT0gJ3JlY292ZXJTdXBwbGllcycpIHJldHVybiBmbG9vci50aWxlcy5mbGF0TWFwKCh0aWxlLCBpKSA9PiB0aWxlLmtpbmQgPT09ICdjcmF0ZScgfHwgdGlsZS5raW5kID09PSAnY2hlc3QnID8gW3BvaW50QXQoZmxvb3IsIGkpXSA6IFtdKVxuICBpZiAoZmxvb3Iub2JqZWN0aXZlLmtpbmQgPT09ICdyZXNjdWVTY291dCcpIHJldHVybiBmbG9vci50aWxlcy5mbGF0TWFwKCh0aWxlLCBpKSA9PiB0aWxlLmtpbmQgPT09ICdyZXNjdWUnID8gW3BvaW50QXQoZmxvb3IsIGkpXSA6IFtdKVxuICBpZiAoZmxvb3Iub2JqZWN0aXZlLmtpbmQgPT09ICdpbnZva2VBbHRhcicpIHJldHVybiBmbG9vci50aWxlcy5mbGF0TWFwKCh0aWxlLCBpKSA9PiB0aWxlLmtpbmQgPT09ICdhbHRhcicgPyBbcG9pbnRBdChmbG9vciwgaSldIDogW10pXG4gIHJldHVybiBmbG9vci5hY3RvcnMuZmlsdGVyKGFjdG9yID0+IGFjdG9yLnJvbGUgPT09ICdndWFyZGlhbicpLm1hcChhY3RvciA9PiAoeyB4OiBhY3Rvci54LCB5OiBhY3Rvci55IH0pKVxufVxuXG5jb25zdCBjYW5SZWFjaE9iamVjdGl2ZVdpdGhQcm9wcyA9IChmbG9vcjogRmxvb3IsIHRhcmdldDogUG9pbnQpOiBib29sZWFuID0+IFtbMCwgMF0sIFswLCAtMV0sIFsxLCAwXSwgWzAsIDFdLCBbLTEsIDBdXS5zb21lKChbeCwgeV0pID0+IGhhc1Bhc3NhYmxlUGF0aChmbG9vciwgZmxvb3Iuc3RhcnQsIHsgeDogdGFyZ2V0LnggKyB4LCB5OiB0YXJnZXQueSArIHkgfSkpXG5jb25zdCBjb250YWluc1JlYWNoYWJsZSA9IChmbG9vcjogRmxvb3IsIHJlYWNoYWJsZTogUmVhZG9ubHlTZXQ8bnVtYmVyPiwgcG9pbnQ6IFBvaW50KTogYm9vbGVhbiA9PiBpbkJvdW5kcyhmbG9vciwgcG9pbnQueCwgcG9pbnQueSkgJiYgcmVhY2hhYmxlLmhhcyhpbmRleE9mKGZsb29yLCBwb2ludC54LCBwb2ludC55KSlcbmNvbnN0IHJlYWNoZXNPYmplY3RpdmUgPSAoZmxvb3I6IEZsb29yLCByZWFjaGFibGU6IFJlYWRvbmx5U2V0PG51bWJlcj4sIHRhcmdldHM6IHJlYWRvbmx5IFBvaW50W10pOiBib29sZWFuID0+IHRhcmdldHMuc29tZSh0YXJnZXQgPT4gW1swLCAwXSwgWzAsIC0xXSwgWzEsIDBdLCBbMCwgMV0sIFstMSwgMF1dLnNvbWUoKFt4LCB5XSkgPT4gY29udGFpbnNSZWFjaGFibGUoZmxvb3IsIHJlYWNoYWJsZSwgeyB4OiB0YXJnZXQueCArIHgsIHk6IHRhcmdldC55ICsgeSB9KSkpXG5leHBvcnQgY29uc3QgaGFzTWFuZGF0b3J5Q29tcGxldGlvblJvdXRlID0gKGZsb29yOiBGbG9vcik6IGJvb2xlYW4gPT4ge1xuICBjb25zdCByZWFjaGFibGUgPSByZWFjaGFibGVJbmRleGVzKGZsb29yKVxuICByZXR1cm4gY29udGFpbnNSZWFjaGFibGUoZmxvb3IsIHJlYWNoYWJsZSwgZmxvb3IuZXhpdCkgJiYgcmVhY2hlc09iamVjdGl2ZShmbG9vciwgcmVhY2hhYmxlLCBvYmplY3RpdmVUYXJnZXRzKGZsb29yKSlcbn1cblxuZXhwb3J0IGNvbnN0IHZhbGlkYXRlU2VjcmV0Um91dGVzID0gKGZsb29yOiBGbG9vcik6IHN0cmluZ1tdID0+IHtcbiAgY29uc3QgZXJyb3JzOiBzdHJpbmdbXSA9IFtdXG4gIGNvbnN0IHJvb21zID0gZmxvb3Iuc2VjcmV0Um9vbXMgPz8gW11cbiAgY29uc3Qgcm91dGVzID0gZmxvb3Iuc2VjcmV0Um91dGVzID8/IFtdXG4gIGNvbnN0IHNwYWNlcyA9IGZsb29yLnNpZGVTcGFjZXMgPz8gW11cbiAgY29uc3Qgcm9vbUlkcyA9IG5ldyBTZXQ8c3RyaW5nPigpXG4gIGZvciAoY29uc3Qgcm9vbSBvZiByb29tcykge1xuICAgIGlmIChyb29tSWRzLmhhcyhyb29tLmlkKSkgZXJyb3JzLnB1c2goYGR1cGxpY2F0ZSBzZWNyZXQgcm9vbTogJHtyb29tLmlkfWApXG4gICAgcm9vbUlkcy5hZGQocm9vbS5pZClcbiAgICBjb25zdCBzb3VyY2UgPSBzcGFjZXMuZmluZChzcGFjZSA9PiBzcGFjZS5pZCA9PT0gcm9vbS5zb3VyY2VJZClcbiAgICBpZiAoIXNvdXJjZSB8fCByb29tLmlkICE9PSBgc2VjcmV0LXJvb206JHtzb3VyY2UuaWR9YCB8fCAhcm9vbS5zYWZlRmFsbGJhY2sgfHwgcm9vbS52ZXJzaW9uICE9PSAxIHx8ICFyb29tLmRpc2NvdmVyeUNsdWUgfHwgIXJvb20uZW50cmllcy5sZW5ndGggfHwgIXJvb20uY2hhbWJlci5sZW5ndGgpIGVycm9ycy5wdXNoKGBpbnZhbGlkIHNlY3JldCByb29tOiAke3Jvb20uaWR9YClcbiAgICBpZiAoW3Jvb20uYXBwcm9hY2gsIC4uLnJvb20uZW50cmllcywgLi4ucm9vbS5jaGFtYmVyXS5zb21lKHBvaW50ID0+ICFpbkJvdW5kcyhmbG9vciwgcG9pbnQueCwgcG9pbnQueSkpKSBlcnJvcnMucHVzaChgb3V0LW9mLWJvdW5kcyBzZWNyZXQgcm9vbTogJHtyb29tLmlkfWApXG4gICAgaWYgKCFmbG9vci5pdGVtcy5zb21lKGl0ZW0gPT4gaXRlbS5zZWNyZXRJZCA9PT0gcm9vbS5pZCkpIGVycm9ycy5wdXNoKGB1bnRhZ2dlZCBzZWNyZXQgcmV3YXJkOiAke3Jvb20uaWR9YClcbiAgfVxuICBmb3IgKGNvbnN0IHNwYWNlIG9mIHNwYWNlcykgaWYgKCFyb29tcy5zb21lKHJvb20gPT4gcm9vbS5zb3VyY2VJZCA9PT0gc3BhY2UuaWQpKSBlcnJvcnMucHVzaChgdW50YWdnZWQgc2VjcmV0IGNvbnRlbnQ6ICR7c3BhY2UuaWR9YClcbiAgY29uc3Qgcm91dGVJZHMgPSBuZXcgU2V0PHN0cmluZz4oKVxuICBmb3IgKGNvbnN0IHJvdXRlIG9mIHJvdXRlcykge1xuICAgIGlmIChyb3V0ZUlkcy5oYXMocm91dGUuaWQpKSBlcnJvcnMucHVzaChgZHVwbGljYXRlIHNlY3JldCByb3V0ZTogJHtyb3V0ZS5pZH1gKVxuICAgIHJvdXRlSWRzLmFkZChyb3V0ZS5pZClcbiAgICBjb25zdCByb29tID0gcm9vbXMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLmlkID09PSByb3V0ZS5yb29tSWQpXG4gICAgaWYgKCFyb29tIHx8IHJvdXRlLnZlcnNpb24gIT09IDEgfHwgIXJvdXRlLnNhZmVGYWxsYmFjayB8fCAhcm91dGUuZGlzY292ZXJ5Q2x1ZSB8fCAhaW5Cb3VuZHMoZmxvb3IsIHJvdXRlLmZyb20ueCwgcm91dGUuZnJvbS55KSB8fCAhaW5Cb3VuZHMoZmxvb3IsIHJvdXRlLmVudHJ5LngsIHJvdXRlLmVudHJ5LnkpKSBlcnJvcnMucHVzaChgaW52YWxpZCBzZWNyZXQgcm91dGU6ICR7cm91dGUuaWR9YClcbiAgICBpZiAocm9vbSAmJiAocm91dGUuZnJvbS54ICE9PSByb29tLmFwcHJvYWNoLnggfHwgcm91dGUuZnJvbS55ICE9PSByb29tLmFwcHJvYWNoLnkgfHwgcm91dGUuZW50cnlDb25kaXRpb24gIT09IHJvb20uZW50cnlDb25kaXRpb24gfHwgcm91dGUuZGlzY292ZXJ5Q2x1ZSAhPT0gcm9vbS5kaXNjb3ZlcnlDbHVlIHx8IHJvdXRlLmFjY2Vzc01ldGhvZCAhPT0gcm9vbS5hY2Nlc3NNZXRob2QgfHwgcm91dGUucmlzayAhPT0gcm9vbS5yaXNrKSkgZXJyb3JzLnB1c2goYG1pc21hdGNoZWQgc2VjcmV0IHJvdXRlOiAke3JvdXRlLmlkfWApXG4gICAgaWYgKHJvdXRlLmtpbmQgPT09ICdyYXJlLXRyYW5zaXRpb24nICYmICghcm91dGUuZGVzdGluYXRpb24gfHwgcm91dGUuZGVzdGluYXRpb24uYmlvbWUgIT09IGZsb29yLmJpb21lIHx8IHJvdXRlLmRlc3RpbmF0aW9uLmZsb29yIDwgMCB8fCByb3V0ZS5kZXN0aW5hdGlvbi5mbG9vciA+IDMgfHwgcm91dGUuZGlyZWN0aW9uICE9PSAnb25lLXdheScgJiYgcm91dGUuZGlyZWN0aW9uICE9PSAndHdvLXdheScgfHwgcm91dGUuYXJyaXZhbCAhPT0gJ2Zsb29yLXN0YXJ0JyB8fCAocm91dGUuZGlyZWN0aW9uID09PSAnb25lLXdheScgPyByb3V0ZS5yZXR1cm5TZW1hbnRpY3MgIT09ICduby1yZXR1cm4nIDogcm91dGUucmV0dXJuU2VtYW50aWNzICE9PSAncmV0dXJuLWxpbmsnKSkpIGVycm9ycy5wdXNoKGBpbnZhbGlkIHNlY3JldCB0cmFuc2l0aW9uOiAke3JvdXRlLmlkfWApXG4gICAgaWYgKHJvdXRlLmtpbmQgPT09ICdjb25jZWFsZWQtcGFzc2FnZScgJiYgKCFyb29tIHx8ICFyb29tLmVudHJpZXMuc29tZShwb2ludCA9PiBwb2ludC54ID09PSByb3V0ZS5lbnRyeS54ICYmIHBvaW50LnkgPT09IHJvdXRlLmVudHJ5LnkpKSkgZXJyb3JzLnB1c2goYHVubWF0Y2hlZCBzZWNyZXQgcm91dGU6ICR7cm91dGUuaWR9YClcbiAgICBpZiAocm91dGUua2luZCA9PT0gJ3JhcmUtdHJhbnNpdGlvbicgJiYgKCFyb29tIHx8ICFmbG9vci5zaWRlU3BhY2VzPy5zb21lKHNwYWNlID0+IHNwYWNlLmtpbmQgPT09ICdtaW5lLWJyZWFjaC1yb29tJyAmJiBzcGFjZS5pZCA9PT0gcm9vbS5zb3VyY2VJZCAmJiBzcGFjZS5yYXJlVHJhbnNpdGlvbj8udGFyZ2V0QmlvbWUgPT09IHJvdXRlLmRlc3RpbmF0aW9uPy5iaW9tZSAmJiBzcGFjZS5yYXJlVHJhbnNpdGlvbj8udGFyZ2V0Rmxvb3IgPT09IHJvdXRlLmRlc3RpbmF0aW9uPy5mbG9vcikpKSBlcnJvcnMucHVzaChgdW5tYXRjaGVkIHNlY3JldCB0cmFuc2l0aW9uOiAke3JvdXRlLmlkfWApXG4gIH1cbiAgZm9yIChjb25zdCByb29tIG9mIHJvb21zKSBmb3IgKGNvbnN0IFtpbmRleCwgZW50cnldIG9mIHJvb20uZW50cmllcy5lbnRyaWVzKCkpIGlmICghcm91dGVzLnNvbWUocm91dGUgPT4gcm91dGUuaWQgPT09IGBzZWNyZXQtcm91dGU6JHtyb29tLmlkfTphY2Nlc3M6JHtpbmRleH1gICYmIHJvdXRlLmtpbmQgPT09ICdjb25jZWFsZWQtcGFzc2FnZScgJiYgcm91dGUuZW50cnkueCA9PT0gZW50cnkueCAmJiByb3V0ZS5lbnRyeS55ID09PSBlbnRyeS55KSkgZXJyb3JzLnB1c2goYG1pc3Npbmcgc2VjcmV0IHJvdXRlOiAke3Jvb20uaWR9OiR7aW5kZXh9YClcbiAgZm9yIChjb25zdCBzcGFjZSBvZiBzcGFjZXMpIGlmIChzcGFjZS5raW5kID09PSAnbWluZS1icmVhY2gtcm9vbScgJiYgc3BhY2UucmFyZVRyYW5zaXRpb24gJiYgIXJvdXRlcy5zb21lKHJvdXRlID0+IHJvdXRlLmlkID09PSBgc2VjcmV0LXJvdXRlOnNlY3JldC1yb29tOiR7c3BhY2UuaWR9OnRyYW5zaXRpb25gICYmIHJvdXRlLmtpbmQgPT09ICdyYXJlLXRyYW5zaXRpb24nICYmIHJvdXRlLmRlc3RpbmF0aW9uPy5iaW9tZSA9PT0gc3BhY2UucmFyZVRyYW5zaXRpb24/LnRhcmdldEJpb21lICYmIHJvdXRlLmRlc3RpbmF0aW9uPy5mbG9vciA9PT0gc3BhY2UucmFyZVRyYW5zaXRpb24/LnRhcmdldEZsb29yKSkgZXJyb3JzLnB1c2goYG1pc3Npbmcgc2VjcmV0IHRyYW5zaXRpb246ICR7c3BhY2UuaWR9YClcbiAgY29uc3Qgc2VjcmV0Q2VsbHMgPSByb29tcy5mbGF0TWFwKHJvb20gPT4gWy4uLnJvb20uZW50cmllcywgLi4ucm9vbS5jaGFtYmVyXSlcbiAgaWYgKCFzZWNyZXRDZWxscy5sZW5ndGgpIHJldHVybiBlcnJvcnNcbiAgY29uc3Qgc2VhbGVkID0gbmV3IE1hcDxudW1iZXIsIHsga2luZDogVGlsZUtpbmQ7IGZsb3c/OiBUaWxlWydmbG93J10gfT4oKVxuICBmb3IgKGNvbnN0IHBvaW50IG9mIHNlY3JldENlbGxzKSB7XG4gICAgY29uc3QgY3VycmVudCA9IGdldFRpbGUoZmxvb3IsIHBvaW50LngsIHBvaW50LnkpXG4gICAgaWYgKCFjdXJyZW50KSBjb250aW51ZVxuICAgIGNvbnN0IGluZGV4ID0gaW5kZXhPZihmbG9vciwgcG9pbnQueCwgcG9pbnQueSlcbiAgICBpZiAoIXNlYWxlZC5oYXMoaW5kZXgpKSBzZWFsZWQuc2V0KGluZGV4LCB7IGtpbmQ6IGN1cnJlbnQua2luZCwgLi4uKGN1cnJlbnQuZmxvdyA/IHsgZmxvdzogeyAuLi5jdXJyZW50LmZsb3cgfSB9IDoge30pIH0pXG4gICAgY3VycmVudC5raW5kID0gJ3dhbGwnXG4gICAgZGVsZXRlIGN1cnJlbnQuZmxvd1xuICB9XG4gIGlmICghaGFzTWFuZGF0b3J5Q29tcGxldGlvblJvdXRlKGZsb29yKSkgZXJyb3JzLnB1c2goJ3NlY3JldCBkZXBlbmRlbmN5IG9uIGNhbXBhaWduIGNvbXBsZXRpb24nKVxuICBmb3IgKGNvbnN0IFtpbmRleCwgb3JpZ2luYWxdIG9mIHNlYWxlZCkge1xuICAgIGNvbnN0IGN1cnJlbnQgPSBmbG9vci50aWxlc1tpbmRleF0hXG4gICAgY3VycmVudC5raW5kID0gb3JpZ2luYWwua2luZFxuICAgIGlmIChvcmlnaW5hbC5mbG93KSBjdXJyZW50LmZsb3cgPSBvcmlnaW5hbC5mbG93XG4gICAgZWxzZSBkZWxldGUgY3VycmVudC5mbG93XG4gIH1cbiAgcmV0dXJuIGVycm9yc1xufVxuXG5leHBvcnQgY29uc3QgdmFsaWRhdGVTaG9ydGN1dExhbmRpbmdzID0gKGZsb29yOiBGbG9vcik6IHN0cmluZ1tdID0+IChmbG9vci5zZWNyZXRSb3V0ZXMgPz8gW10pLmZsYXRNYXAocm91dGUgPT4ge1xuICBpZiAocm91dGUua2luZCAhPT0gJ3JhcmUtdHJhbnNpdGlvbicgfHwgIXJvdXRlLmRlc3RpbmF0aW9uIHx8IHJvdXRlLmRlc3RpbmF0aW9uLmJpb21lICE9PSBmbG9vci5iaW9tZSB8fCByb3V0ZS5kZXN0aW5hdGlvbi5mbG9vciA8IDAgfHwgcm91dGUuZGVzdGluYXRpb24uZmxvb3IgPiAzKSByZXR1cm4gW11cbiAgdHJ5IHtcbiAgICBjb25zdCBsYW5kaW5nID0gZ2VuZXJhdGVBcmVhRmxvb3IoZmxvb3Iuc2VlZCwgcm91dGUuZGVzdGluYXRpb24uYmlvbWUsIHJvdXRlLmRlc3RpbmF0aW9uLmZsb29yKVxuICAgIHJldHVybiBpc1Bhc3NhYmxlKGxhbmRpbmcsIGxhbmRpbmcuc3RhcnQueCwgbGFuZGluZy5zdGFydC55KSAmJiBoYXNNYW5kYXRvcnlDb21wbGV0aW9uUm91dGUobGFuZGluZykgPyBbXSA6IFtgdW5zYWZlIHNob3J0Y3V0IGxhbmRpbmc6ICR7cm91dGUuaWR9YF1cbiAgfSBjYXRjaCB7IHJldHVybiBbYHVuc2FmZSBzaG9ydGN1dCBsYW5kaW5nOiAke3JvdXRlLmlkfWBdIH1cbn0pXG5cbmV4cG9ydCBjb25zdCB2YWxpZGF0ZUdlbmVyYXRpb24gPSAoZmxvb3I6IEZsb29yKTogR2VuZXJhdGlvblZhbGlkYXRpb24gPT4ge1xuICBjb25zdCBlcnJvcnM6IHN0cmluZ1tdID0gW11cbiAgZXJyb3JzLnB1c2goLi4udmFsaWRhdGVQdXp6bGVUZW1wbGF0ZXMoKSwgLi4udmFsaWRhdGVGbG9vclB1enpsZXMoZmxvb3IpLCAuLi5wcm9wRGVmaW5pdGlvbkVycm9ycylcbiAgaWYgKHB1enpsZVRlbXBsYXRlc0ZvcihmbG9vci5iaW9tZSkubGVuZ3RoICYmICEoZmxvb3IucHV6emxlSWRzPy5sZW5ndGgpKSBlcnJvcnMucHVzaCgnbWlzc2luZyBwdXp6bGUgdGVtcGxhdGUnKVxuICBpZiAoZmxvb3IudGlsZXMubGVuZ3RoICE9PSBmbG9vci53aWR0aCAqIGZsb29yLmhlaWdodCB8fCBmbG9vci53aWR0aCA8IE1BUF9XSURUSCB8fCBmbG9vci5oZWlnaHQgPCBNQVBfSEVJR0hUIHx8IGZsb29yLmluZGV4IDwgMCB8fCBmbG9vci5pbmRleCA+PSBGTE9PUl9DT1VOVCkgZXJyb3JzLnB1c2goJ2ludmFsaWQgZmxvb3IgZGltZW5zaW9ucycpXG4gIGlmICghZmxvb3IubGF5b3V0SWQgfHwgIWxheW91dFZhcmlhbnRzW2Zsb29yLmJpb21lXS5zb21lKGxheW91dCA9PiBmbG9vci5sYXlvdXRJZCA9PT0gbGF5b3V0IHx8IGZsb29yLmxheW91dElkID09PSBgJHtsYXlvdXR9LXJlbWl4YCkpIGVycm9ycy5wdXNoKCdpbnZhbGlkIGxheW91dCBpZCcpXG4gIGlmICghZ2V0VGlsZShmbG9vciwgZmxvb3Iuc3RhcnQueCwgZmxvb3Iuc3RhcnQueSkgfHwgIXBhc3NhYmxlKGdldFRpbGUoZmxvb3IsIGZsb29yLnN0YXJ0LngsIGZsb29yLnN0YXJ0LnkpIS5raW5kKSkgZXJyb3JzLnB1c2goJ2ludmFsaWQgc3RhcnQgcGxhY2VtZW50JylcbiAgaWYgKGdldFRpbGUoZmxvb3IsIGZsb29yLmV4aXQueCwgZmxvb3IuZXhpdC55KT8ua2luZCAhPT0gJ2V4aXQnKSBlcnJvcnMucHVzaCgnaW52YWxpZCBleGl0IHBsYWNlbWVudCcpXG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCBmbG9vci50aWxlcy5sZW5ndGg7IGluZGV4KyspIHtcbiAgICBjb25zdCB0aWxlID0gZmxvb3IudGlsZXNbaW5kZXhdXG4gICAgaWYgKCF0aWxlLmZsb3cpIGNvbnRpbnVlXG4gICAgY29uc3QgcG9pbnQgPSBwb2ludEF0KGZsb29yLCBpbmRleClcbiAgICBjb25zdCBkZWx0YSA9ICh7IG46IHsgeDogMCwgeTogLTEgfSwgbmU6IHsgeDogMSwgeTogLTEgfSwgZTogeyB4OiAxLCB5OiAwIH0sIHNlOiB7IHg6IDEsIHk6IDEgfSwgczogeyB4OiAwLCB5OiAxIH0sIHN3OiB7IHg6IC0xLCB5OiAxIH0sIHc6IHsgeDogLTEsIHk6IDAgfSwgbnc6IHsgeDogLTEsIHk6IC0xIH0gfSBhcyBjb25zdClbdGlsZS5mbG93LmRpcmVjdGlvbl1cbiAgICBjb25zdCBkb3duc3RyZWFtID0gZ2V0VGlsZShmbG9vciwgcG9pbnQueCArIGRlbHRhLngsIHBvaW50LnkgKyBkZWx0YS55KVxuICAgIGNvbnN0IHNxdWFsbCA9IHRpbGUua2luZCA9PT0gJ2xlZGdlJyAmJiB0aWxlLmZsb3cuaGF6YXJkID09PSAnc3F1YWxsJ1xuICAgIGlmICgodGlsZS5raW5kICE9PSAnY3VycmVudCcgJiYgIXNxdWFsbCkgfHwgIWRvd25zdHJlYW0pIGVycm9ycy5wdXNoKGBpbnZhbGlkIGZsb3cgYXQgJHtwb2ludEtleShwb2ludCl9YClcbiAgICBpZiAodGlsZS5mbG93LmhhemFyZCAmJiAhc3F1YWxsICYmIGRvd25zdHJlYW0/LmtpbmQgIT09ICdjdXJyZW50JyAmJiBkb3duc3RyZWFtPy5raW5kICE9PSAnZGVlcFdhdGVyJyAmJiBkb3duc3RyZWFtPy5raW5kICE9PSAnYnJpbmUnKSBlcnJvcnMucHVzaChgdW5tYXJrZWQgZmxvdyBvdXRsZXQgYXQgJHtwb2ludEtleShwb2ludCl9YClcbiAgfVxuICBjb25zdCByZWFjaGFibGUgPSByZWFjaGFibGVJbmRleGVzKGZsb29yKVxuICBpZiAoIWNvbnRhaW5zUmVhY2hhYmxlKGZsb29yLCByZWFjaGFibGUsIGZsb29yLmV4aXQpKSBlcnJvcnMucHVzaCgnZXhpdCB1bnJlYWNoYWJsZScpXG4gIGNvbnN0IHRhcmdldHMgPSBvYmplY3RpdmVUYXJnZXRzKGZsb29yKVxuICBpZiAoIXRhcmdldHMubGVuZ3RoIHx8ICFyZWFjaGVzT2JqZWN0aXZlKGZsb29yLCByZWFjaGFibGUsIHRhcmdldHMpKSBlcnJvcnMucHVzaChgb2JqZWN0aXZlIHVucmVhY2hhYmxlOiAke2Zsb29yLm9iamVjdGl2ZS5raW5kfWApXG4gIGlmIChmbG9vci5taWxlc3RvbmVzLmxlbmd0aCA8IDUgfHwgZmxvb3IubWlsZXN0b25lcy5sZW5ndGggPiA2KSBlcnJvcnMucHVzaCgnaW52YWxpZCBtaWxlc3RvbmUgY291bnQnKVxuICBjb25zdCByZXdhcmRPZmZlcnMgPSBmbG9vci5yZXdhcmRPZmZlcnMgPz8gW11cbiAgY29uc3QgZXhwZWN0ZWRSZXdhcmRPZmZlcnM6IEFycmF5PFtzdHJpbmcsIHN0cmluZ10+ID0gW1snd2F5Y2FjaGUnLCAnd2F5Y2FjaGUnXSwgWydib29uLXRlYWNoJywgJ2Jvb24nXSwgWydib29uLXRlc3QnLCAnYm9vbiddLCBbJ2Jvb24tcGF5b2ZmJywgJ2Jvb24nXV1cbiAgZm9yIChjb25zdCBbbWlsZXN0b25lSWQsIGtpbmRdIG9mIGV4cGVjdGVkUmV3YXJkT2ZmZXJzKSB7XG4gICAgY29uc3Qgb2ZmZXIgPSByZXdhcmRPZmZlcnMuZmluZChjYW5kaWRhdGUgPT4gY2FuZGlkYXRlLm1pbGVzdG9uZUlkID09PSBtaWxlc3RvbmVJZClcbiAgICBpZiAoIW9mZmVyIHx8IG9mZmVyLmtpbmQgIT09IGtpbmQgfHwgb2ZmZXIuY2hvaWNlcy5sZW5ndGggIT09IDMgfHwgbmV3IFNldChvZmZlci5jaG9pY2VzLm1hcChjaG9pY2UgPT4gY2hvaWNlLnJvbGUpKS5zaXplICE9PSAzKSBlcnJvcnMucHVzaChgaW52YWxpZCByZXdhcmQgb2ZmZXI6ICR7bWlsZXN0b25lSWR9YClcbiAgICBpZiAoIWZsb29yLm1pbGVzdG9uZXMuc29tZShtaWxlc3RvbmUgPT4gbWlsZXN0b25lLnJld2FyZEtleSA9PT0gbWlsZXN0b25lSWQgJiYgbWlsZXN0b25lLmtpbmQgPT09IGtpbmQpKSBlcnJvcnMucHVzaChgbWlzc2luZyByZXdhcmQgbWlsZXN0b25lOiAke21pbGVzdG9uZUlkfWApXG4gIH1cbiAgaWYgKCFwcmltYXJ5VG9vbFVzZUF2YWlsYWJsZShmbG9vciwgcmVhY2hhYmxlKSkgZXJyb3JzLnB1c2goJ3VudXNhYmxlIHByaW1hcnkgV2F5Y2FjaGUgdG9vbCcpXG4gIGlmICgoZmxvb3IuZW5jb3VudGVycz8ubGVuZ3RoID8/IDApICE9PSAxKSBlcnJvcnMucHVzaCgnaW52YWxpZCBlbmNvdW50ZXIgY291bnQnKVxuICBjb25zdCBtaWxlc3RvbmVMb2NhdGlvbnMgPSBuZXcgU2V0PHN0cmluZz4oKVxuICBmb3IgKGNvbnN0IG1pbGVzdG9uZSBvZiBmbG9vci5taWxlc3RvbmVzKSB7XG4gICAgY29uc3Qga2V5ID0gcG9pbnRLZXkobWlsZXN0b25lKVxuICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKGZsb29yLCBtaWxlc3RvbmUueCwgbWlsZXN0b25lLnkpXG4gICAgaWYgKCFtaWxlc3RvbmUuaWQgfHwgIVsnd2F5Y2FjaGUnLCAnYm9vbicsICdhdWdtZW50JywgJ3JlbGljJ10uaW5jbHVkZXMobWlsZXN0b25lLmtpbmQpIHx8ICF0aWxlIHx8ICFwYXNzYWJsZSh0aWxlLmtpbmQpIHx8ICh0aWxlLmtpbmQgPT09ICdleGl0JyAmJiBtaWxlc3RvbmUua2luZCAhPT0gJ3JlbGljJykgfHwgIWNvbnRhaW5zUmVhY2hhYmxlKGZsb29yLCByZWFjaGFibGUsIG1pbGVzdG9uZSkpIGVycm9ycy5wdXNoKGB1bnJlYWNoYWJsZSBtaWxlc3RvbmU6ICR7bWlsZXN0b25lLmlkfWApXG4gICAgaWYgKG1pbGVzdG9uZUxvY2F0aW9ucy5oYXMoa2V5KSkgZXJyb3JzLnB1c2goYG92ZXJsYXBwaW5nIG1pbGVzdG9uZTogJHtrZXl9YClcbiAgICBtaWxlc3RvbmVMb2NhdGlvbnMuYWRkKGtleSlcbiAgfVxuICBmb3IgKGNvbnN0IGVuY291bnRlciBvZiBmbG9vci5lbmNvdW50ZXJzID8/IFtdKSB7XG4gICAgY29uc3QgdGlsZSA9IGdldFRpbGUoZmxvb3IsIGVuY291bnRlci54LCBlbmNvdW50ZXIueSlcbiAgICBpZiAoIWVuY291bnRlci5pZCB8fCAhWyd3YXlmYXJlcicsICdibG9vZEJhcmdhaW4nLCAnc2hpZnRpbmdDaGFtYmVyJywgJ3N0b3JtQ2FjaGUnLCAnYW5jZXN0b3JEZWJ0JywgJ2N1cnNlZE9iamVjdCcsICdvYXRod2VsbCcsICd3aW5kVHJpYWwnLCAndG9tYkF1Y3Rpb24nLCAnc3VuVHJpYnV0ZScsICdtaXJhZ2VNYXJrZXQnLCAnYnJpbmVPYXRoJywgJ2dsYXNzVHJpYWwnLCAnd2hpdGVSb2FkJywgJ3NhbHRDYWNoZScsICdpY2VEdWVsJywgJ3dpbnRlclRpdGhlJywgJ3JpbWVDb250cmFjdCcsICdmcm9zdENhY2hlJywgJ3doaXRlb3V0JywgJ3JlbGlxdWFyeVRyaWFsJywgJ21pbmVQYWN0JywgJ21pbmVLYW1pJywgJ3dpbGRzUGFjdCcsICd3aWxkc0thbWknLCAnY2F2ZXJuc1BhY3QnLCAnY2F2ZXJuc0thbWknLCAncnVpbnNQYWN0JywgJ3J1aW5zS2FtaScsICdmdXJuYWNlUGFjdCcsICdmdXJuYWNlS2FtaScsICdmbG9vZGVkUGFjdCcsICdmbG9vZGVkS2FtaScsICdjbGlmZnNQYWN0JywgJ2NsaWZmc0thbWknLCAnYnVyaWFsUGFjdCcsICdidXJpYWxLYW1pJywgJ3NhbHRQYWN0JywgJ3NhbHRLYW1pJywgJ2Zyb3N0UGFjdCcsICdmcm9zdEthbWknXS5pbmNsdWRlcyhlbmNvdW50ZXIua2luZCkgfHwgIXRpbGUgfHwgIXBhc3NhYmxlKHRpbGUua2luZCkgfHwgdGlsZS5raW5kID09PSAnZXhpdCcgfHwgIWNvbnRhaW5zUmVhY2hhYmxlKGZsb29yLCByZWFjaGFibGUsIGVuY291bnRlcikpIGVycm9ycy5wdXNoKGB1bnJlYWNoYWJsZSBlbmNvdW50ZXI6ICR7ZW5jb3VudGVyLmlkfWApXG4gIH1cbiAgZm9yIChjb25zdCBlY29sb2d5IG9mIGZsb29yLmVjb2xvZ3kgPz8gW10pIHtcbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgZWNvbG9neS50YXJnZXQueCwgZWNvbG9neS50YXJnZXQueSlcbiAgICBpZiAoIWVjb2xvZ3kuaWQgfHwgIVsndGlkZScsICd3aW5kJywgJ3Ntb2tlJywgJ2NvbGxhcHNlJywgJ2ZpcmUnLCAnbWlncmF0aW9uJywgJ25lc3RpbmcnLCAndmlzaWJpbGl0eSddLmluY2x1ZGVzKGVjb2xvZ3kua2luZCkgfHwgIXRpbGUgfHwgIU51bWJlci5pc0ludGVnZXIoZWNvbG9neS5zdGFydHNBdCkgfHwgZWNvbG9neS5zdGFydHNBdCA8IDEgfHwgIU51bWJlci5pc0ludGVnZXIoZWNvbG9neS5kdXJhdGlvbikgfHwgZWNvbG9neS5kdXJhdGlvbiA8IDEgfHwgIWVjb2xvZ3kud2FybmluZyB8fCAhZWNvbG9neS5yZXNwb25zZXMubGVuZ3RoIHx8ICFlY29sb2d5LmNsZWFudXAgfHwgIVsnd2FpdGluZycsICdhY3RpdmUnLCAncmVzb2x2ZWQnXS5pbmNsdWRlcyhlY29sb2d5LnN0YXRlKSkgeyBlcnJvcnMucHVzaChgaW52YWxpZCBlY29sb2d5IGV2ZW50OiAke2Vjb2xvZ3kuaWR9YCk7IGNvbnRpbnVlIH1cbiAgICBjb25zdCBwcmV2aW91c0tpbmQgPSB0aWxlLmtpbmRcbiAgICBjb25zdCBwcmV2aW91c0Zsb3cgPSB0aWxlLmZsb3cgPyB7IC4uLnRpbGUuZmxvdyB9IDogdW5kZWZpbmVkXG4gICAgdGlsZS5raW5kID0gZWNvbG9neS5lZmZlY3RcbiAgICBpZiAoZWNvbG9neS5lZmZlY3RGbG93KSB0aWxlLmZsb3cgPSB7IC4uLmVjb2xvZ3kuZWZmZWN0RmxvdyB9XG4gICAgZWxzZSBkZWxldGUgdGlsZS5mbG93XG4gICAgY29uc3QgYWZ0ZXJFY29sb2d5ID0gcmVhY2hhYmxlSW5kZXhlcyhmbG9vcilcbiAgICBjb25zdCBzb2x2YWJsZSA9IGNvbnRhaW5zUmVhY2hhYmxlKGZsb29yLCBhZnRlckVjb2xvZ3ksIGZsb29yLmV4aXQpICYmIHJlYWNoZXNPYmplY3RpdmUoZmxvb3IsIGFmdGVyRWNvbG9neSwgb2JqZWN0aXZlVGFyZ2V0cyhmbG9vcikpXG4gICAgdGlsZS5raW5kID0gcHJldmlvdXNLaW5kXG4gICAgaWYgKHByZXZpb3VzRmxvdykgdGlsZS5mbG93ID0gcHJldmlvdXNGbG93XG4gICAgZWxzZSBkZWxldGUgdGlsZS5mbG93XG4gICAgaWYgKCFzb2x2YWJsZSkgZXJyb3JzLnB1c2goYGVjb2xvZ3kgYmxvY2tzIG1hbmRhdG9yeSBwYXRoOiAke2Vjb2xvZ3kuaWR9YClcbiAgfVxuICBjb25zdCBwbGFjZW1lbnRzID0gWy4uLmZsb29yLmFjdG9ycy5tYXAoYWN0b3IgPT4gKHsgLi4uYWN0b3IsIHR5cGU6ICdhY3RvcicgYXMgY29uc3QgfSkpLCAuLi5mbG9vci5pdGVtcy5tYXAoaXRlbSA9PiAoeyAuLi5pdGVtLCB0eXBlOiAnaXRlbScgYXMgY29uc3QgfSkpXVxuICBjb25zdCBvY2N1cGllZCA9IG5ldyBTZXQ8c3RyaW5nPigpXG4gIGZvciAoY29uc3QgcGxhY2VtZW50IG9mIHBsYWNlbWVudHMpIHtcbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgcGxhY2VtZW50LngsIHBsYWNlbWVudC55KVxuICAgIGlmICghdGlsZSB8fCAhcGFzc2FibGUodGlsZS5raW5kKSB8fCB0aWxlLmtpbmQgPT09ICdsb2NrZWREb29yJykgZXJyb3JzLnB1c2goYGlsbGVnYWwgJHtwbGFjZW1lbnQudHlwZX0gcGxhY2VtZW50YClcbiAgICBjb25zdCBrZXkgPSBwb2ludEtleShwbGFjZW1lbnQpXG4gICAgaWYgKG9jY3VwaWVkLmhhcyhrZXkpKSBlcnJvcnMucHVzaChgb3ZlcmxhcHBpbmcgJHtwbGFjZW1lbnQudHlwZX0gcGxhY2VtZW50YClcbiAgICBvY2N1cGllZC5hZGQoa2V5KVxuICB9XG4gIGlmICghZmxvb3IucHJvcHMubGVuZ3RoKSBlcnJvcnMucHVzaCgnbWlzc2luZyBwcm9wcycpXG4gIGNvbnN0IHByb3BJZHMgPSBuZXcgU2V0PHN0cmluZz4oKVxuICBjb25zdCBwcm9wTG9jYXRpb25zID0gbmV3IFNldDxzdHJpbmc+KClcbiAgZm9yIChjb25zdCBwcm9wIG9mIGZsb29yLnByb3BzKSB7XG4gICAgaWYgKHByb3BJZHMuaGFzKHByb3AuaWQpKSBlcnJvcnMucHVzaChgZHVwbGljYXRlIHByb3AgaWQ6ICR7cHJvcC5pZH1gKVxuICAgIHByb3BJZHMuYWRkKHByb3AuaWQpXG4gICAgaWYgKCFQUk9QX0lEUy5pbmNsdWRlcyhwcm9wLmtpbmQpKSB7IGVycm9ycy5wdXNoKGB1bmtub3duIHByb3A6ICR7cHJvcC5raW5kfWApOyBjb250aW51ZSB9XG4gICAgY29uc3QgZGVmaW5pdGlvbiA9IHByb3BEZWZpbml0aW9uKHByb3Aua2luZClcbiAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShmbG9vciwgcHJvcC54LCBwcm9wLnkpXG4gICAgY29uc3QgbG9jYXRpb24gPSBwb2ludEtleShwcm9wKVxuICAgIGlmIChwcm9wTG9jYXRpb25zLmhhcyhsb2NhdGlvbikpIGVycm9ycy5wdXNoKGBvdmVybGFwcGluZyBwcm9wIHBsYWNlbWVudDogJHtsb2NhdGlvbn1gKVxuICAgIHByb3BMb2NhdGlvbnMuYWRkKGxvY2F0aW9uKVxuICAgIGlmIChwcm9wLmJpb21lICE9PSBmbG9vci5iaW9tZSB8fCBkZWZpbml0aW9uLmJpb21lICE9PSBmbG9vci5iaW9tZSkgZXJyb3JzLnB1c2goYGludmFsaWQgcHJvcCBiaW9tZTogJHtwcm9wLmlkfWApXG4gICAgaWYgKCF0aWxlIHx8ICFwYXNzYWJsZSh0aWxlLmtpbmQpIHx8IHRpbGUua2luZCA9PT0gJ2xvY2tlZERvb3InIHx8ICFkZWZpbml0aW9uLnRlcnJhaW4uaW5jbHVkZXModGlsZS5raW5kKSkgZXJyb3JzLnB1c2goYGlsbGVnYWwgcHJvcCBwbGFjZW1lbnQ6ICR7cHJvcC5pZH1gKVxuICAgIGlmICghaGFzUHJvcENvbnRleHQoZmxvb3IsIHByb3Aua2luZCwgcHJvcCkpIGVycm9ycy5wdXNoKGBpbnZhbGlkIHByb3AgY29udGV4dDogJHtwcm9wLmlkfWApXG4gICAgZWxzZSBpZiAoaXNCbG9ja2luZ1Byb3AocHJvcCkpIHtcbiAgICAgIGNvbnN0IHJlYWNoYWJsZVNpZGUgPSBbWzAsIC0xXSwgWzEsIDBdLCBbMCwgMV0sIFstMSwgMF1dLnNvbWUoKFt4LCB5XSkgPT4gY29udGFpbnNSZWFjaGFibGUoZmxvb3IsIHJlYWNoYWJsZSwgeyB4OiBwcm9wLnggKyB4LCB5OiBwcm9wLnkgKyB5IH0pKVxuICAgICAgaWYgKCFyZWFjaGFibGVTaWRlKSBlcnJvcnMucHVzaChgdW5yZWFjaGFibGUgcHJvcDogJHtwcm9wLmlkfWApXG4gICAgfSBlbHNlIGlmICghcmVhY2hhYmxlLmhhcyhpbmRleE9mKGZsb29yLCBwcm9wLngsIHByb3AueSkpKSBlcnJvcnMucHVzaChgdW5yZWFjaGFibGUgcHJvcDogJHtwcm9wLmlkfWApXG4gICAgaWYgKCFbJ2Rvcm1hbnQnLCAnaW5zcGVjdGVkJywgJ2FjdGl2YXRlZCcsICdkZXN0cm95ZWQnXS5pbmNsdWRlcyhwcm9wLnN0YXRlKSkgZXJyb3JzLnB1c2goYGludmFsaWQgcHJvcCBzdGF0ZTogJHtwcm9wLmlkfWApXG4gICAgaWYgKCFwcm9wLnRhZ3MubGVuZ3RoIHx8ICFwcm9wLmhvb2tzPy5sZW5ndGggfHwgIXByb3AuaG9va3MuaW5jbHVkZXMoJ29wZXJhdGUnKSkgZXJyb3JzLnB1c2goYGludmFsaWQgcHJvcCBob29rczogJHtwcm9wLmlkfWApXG4gIH1cbiAgZXJyb3JzLnB1c2goLi4udmFsaWRhdGVTZWNyZXRSb3V0ZXMoZmxvb3IpLCAuLi52YWxpZGF0ZVNob3J0Y3V0TGFuZGluZ3MoZmxvb3IpKVxuICBmb3IgKGNvbnN0IGVycm9yIG9mIHZhbGlkYXRlQXJlYUdhdGUoZ2F0ZUZvckFyZWEoZmxvb3IuYmlvbWUpKSkgZXJyb3JzLnB1c2goYGltcG9zc2libGUgZ2F0ZTogJHtlcnJvcn1gKVxuICByZXR1cm4geyB2YWxpZDogZXJyb3JzLmxlbmd0aCA9PT0gMCwgZXJyb3JzIH1cbn1cblxuZXhwb3J0IGludGVyZmFjZSBHZW5lcmF0aW9uVmFsaWRhdGlvbkZhaWx1cmUgeyBzZWVkOiBudW1iZXI7IGJpb21lOiBGbG9vclsnYmlvbWUnXTsgZmxvb3I6IG51bWJlcjsgcm91dGVOb2RlOiBzdHJpbmc7IGludmFyaWFudDogc3RyaW5nIH1cbmNvbnN0IHJvdXRlTm9kZUZvckZhaWx1cmUgPSAoZmxvb3I6IEZsb29yLCBpbnZhcmlhbnQ6IHN0cmluZyk6IHN0cmluZyA9PiB7XG4gIGNvbnN0IHJvdXRlID0gcm91dGVDb250cmFjdERlYnVnKGZsb29yKVxuICBjb25zdCBraW5kID0gaW52YXJpYW50LmluY2x1ZGVzKCdzZWNyZXQnKSB8fCBpbnZhcmlhbnQuaW5jbHVkZXMoJ3Nob3J0Y3V0JykgPyAnb3B0aW9uYWxSZXdhcmQnIDogaW52YXJpYW50LmluY2x1ZGVzKCdvYmplY3RpdmUnKSA/ICdvYmplY3RpdmUnIDogaW52YXJpYW50LmluY2x1ZGVzKCdleGl0JykgPyAnZXhpdCcgOiBpbnZhcmlhbnQuaW5jbHVkZXMoJ3N0YXJ0JykgPyAnc3RhcnQnIDogJ2ZvcmsnXG4gIHJldHVybiByb3V0ZT8ubm9kZXMuZmluZChub2RlID0+IG5vZGUua2luZCA9PT0ga2luZCk/LmlkID8/IGBmbG9vcjoke2Zsb29yLmluZGV4ICUgNH06JHtraW5kfWBcbn1cbmV4cG9ydCBjb25zdCBnZW5lcmF0aW9uVmFsaWRhdGlvbkZhaWx1cmVzID0gKGZsb29yOiBGbG9vciwgdmFsaWRhdGlvbiA9IHZhbGlkYXRlR2VuZXJhdGlvbihmbG9vcikpOiBHZW5lcmF0aW9uVmFsaWRhdGlvbkZhaWx1cmVbXSA9PiB2YWxpZGF0aW9uLmVycm9ycy5tYXAoaW52YXJpYW50ID0+ICh7IHNlZWQ6IGZsb29yLnNlZWQsIGJpb21lOiBmbG9vci5iaW9tZSwgZmxvb3I6IGZsb29yLmluZGV4ICUgNCwgcm91dGVOb2RlOiByb3V0ZU5vZGVGb3JGYWlsdXJlKGZsb29yLCBpbnZhcmlhbnQpLCBpbnZhcmlhbnQgfSkpXG5cbmV4cG9ydCBjb25zdCB2YWxpZGF0ZUZsb29yID0gKGZsb29yOiBGbG9vcik6IGJvb2xlYW4gPT4gdmFsaWRhdGVHZW5lcmF0aW9uKGZsb29yKS52YWxpZFxuIl0sIm1hcHBpbmdzIjoiQUFBQSxTQUFTQSxLQUFLLEVBQUVDLFFBQVEsRUFBRUMsYUFBYSxFQUFFQyxXQUFXLEVBQUVDLGNBQWMsRUFBRUMsa0JBQWtCLFFBQVEsV0FBVztBQUMzRyxTQUFTQyxNQUFNLEVBQUVDLFVBQVUsUUFBa0IsT0FBTztBQUNwRCxTQUFTQyxXQUFXLEVBQUVDLFVBQVUsRUFBRUMsU0FBUyxFQUFvZUMsVUFBVSxFQUFFQyxVQUFVLEVBQUVDLGFBQWEsUUFBUSxTQUFTO0FBQ3JrQixTQUFTQyxpQkFBaUIsUUFBUSxjQUFjO0FBQ2hELFNBQVND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Q0FBQ3pVLEtBQUssRUFBRSxDQUFDO01BQUVaLENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUVXLEtBQUssQ0FBQzBJLE1BQU0sR0FBRztJQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQztJQUM3SCtMLFVBQVUsQ0FBQ3pVLEtBQUssRUFBRSxDQUFDO01BQUVaLENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUVXLEtBQUssQ0FBQzBJLE1BQU0sR0FBRztJQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQztJQUM3SCxLQUFLLE1BQU0sQ0FBQ3JKLENBQUMsRUFBRXdRLEdBQUcsQ0FBQyxJQUFJLENBQUMsQ0FBQyxFQUFFLEVBQUUsRUFBRSxDQUFDLEVBQUUsQ0FBQyxFQUFFLEVBQUUsRUFBRSxDQUFDLEVBQUUsQ0FBQyxFQUFFLEVBQUUsRUFBRSxDQUFDLENBQUMsQ0FBQ21GLEtBQUssQ0FBQyxDQUFDLEVBQUVoUCxTQUFTLENBQUMsRUFBRXVPLFNBQVMsQ0FBQ3ZVLEtBQUssRUFBRSxLQUFLLEVBQUVYLENBQUMsRUFBRXdRLEdBQUcsRUFBRSxDQUFDLENBQUM7RUFDL0csQ0FBQyxNQUFNO0lBQ0wsTUFBTW9GLE1BQU0sR0FBRyxDQUNiLENBQUM7TUFBRTdWLENBQUMsRUFBRSxDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsQ0FBQyxFQUN0RSxDQUFDO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsQ0FBQyxFQUN2RSxDQUFDO01BQUVELENBQUMsRUFBRSxDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsQ0FBQyxFQUN2RSxDQUFDO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsQ0FBQyxDQUN4RTtJQUNENFYsTUFBTSxDQUFDRCxLQUFLLENBQUMsQ0FBQyxFQUFFLENBQUMsR0FBR2hQLFNBQVMsQ0FBQyxDQUFDa0ssT0FBTyxDQUFDd0QsTUFBTSxJQUFJZSxVQUFVLENBQUN6VSxLQUFLLEVBQUUwVCxNQUFNLEVBQUUsQ0FBQyxDQUFDLENBQUM7RUFDaEY7QUFDRixDQUFDO0FBRUQsTUFBTTdCLDhCQUE4QixHQUFHQSxDQUFDN1IsS0FBWSxFQUFFNEcsUUFBa0QsRUFBRWdDLEtBQXVCLEVBQUUrSyxJQUFTLEtBQWE7RUFDdkosSUFBSS9NLFFBQVEsQ0FBQ1ksS0FBSyxLQUFLeEgsS0FBSyxDQUFDd0gsS0FBSyxFQUFFLE1BQU0sSUFBSUQsS0FBSyxDQUFDLGtCQUFrQlgsUUFBUSxDQUFDN0UsRUFBRSxtQ0FBbUMsQ0FBQztFQUNySCxJQUFJNkUsUUFBUSxDQUFDdUIsUUFBUSxLQUFLbkksS0FBSyxDQUFDeUgsUUFBUSxFQUFFLE1BQU0sSUFBSUYsS0FBSyxDQUFDLGtCQUFrQlgsUUFBUSxDQUFDN0UsRUFBRSxxQ0FBcUMsQ0FBQztFQUM3SDBOLFNBQVMsQ0FBQ3pQLEtBQUssRUFBRTtJQUFFWixDQUFDLEVBQUUsQ0FBQztJQUFFQyxDQUFDLEVBQUUsQ0FBQztJQUFFME4sQ0FBQyxFQUFFL00sS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUM7SUFBRXVFLENBQUMsRUFBRWhOLEtBQUssQ0FBQzBJLE1BQU0sR0FBRztFQUFFLENBQUMsQ0FBQztFQUN6RW9NLGtCQUFrQixDQUFDOVUsS0FBSyxFQUFFQSxLQUFLLENBQUN5SCxRQUFRLENBQUM0SSxPQUFPLENBQUMsUUFBUSxFQUFFLEVBQUUsQ0FBQyxDQUFDO0VBQy9ELE1BQU00QixNQUFNLEdBQUk3SyxJQUF1QyxLQUFZO0lBQUVoSSxDQUFDLEVBQUVnSSxJQUFJLENBQUM4SyxTQUFTLENBQUM5UyxDQUFDO0lBQUVDLENBQUMsRUFBRStILElBQUksQ0FBQzhLLFNBQVMsQ0FBQzdTLENBQUM7SUFBRTBOLENBQUMsRUFBRTNGLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3pKLEtBQUs7SUFBRXVFLENBQUMsRUFBRTVGLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3hKO0VBQU8sQ0FBQyxDQUFDO0VBQ25LLE1BQU1pQixLQUFLLEdBQUdmLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzFCLEdBQUcsQ0FBQ3NNLE1BQU0sQ0FBQztFQUNyQ3RJLEtBQUssQ0FBQ3VHLE9BQU8sQ0FBQ3BELElBQUksSUFBSTJDLFNBQVMsQ0FBQ3pQLEtBQUssRUFBRThNLElBQUksQ0FBQyxDQUFDO0VBQzdDL08sb0JBQW9CLENBQUM2SyxLQUFLLENBQUMsQ0FBQ3NILE9BQU8sQ0FBQy9RLEtBQUssSUFBSTRLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFLE9BQU8sQ0FBQyxDQUFDO0VBQ3ZGLE1BQU04UyxNQUFNLEdBQUcsSUFBSTlPLEdBQUcsQ0FBQ3VGLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzFCLEdBQUcsQ0FBQ3lCLElBQUksSUFBSSxDQUFDQSxJQUFJLENBQUNySSxJQUFJLEVBQUVrVCxNQUFNLENBQUM3SyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDMUUsTUFBTWdMLE9BQXdCLEdBQUcsQ0FBQyxPQUFPLEVBQUUsVUFBVSxFQUFFLE1BQU0sRUFBRSxnQkFBZ0IsRUFBRSxXQUFXLEVBQUVwUyxLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLE1BQU0sR0FBRyxNQUFNLENBQUM7RUFDdEksT0FBT2tTLE9BQU8sQ0FBQ3pNLEdBQUcsQ0FBQzVHLElBQUksSUFBSW9ULE1BQU0sQ0FBQ3BPLEdBQUcsQ0FBQ2hGLElBQUksQ0FBQyxDQUFDLENBQUM2RyxNQUFNLENBQUVrSCxJQUFJLElBQW1CaE0sT0FBTyxDQUFDZ00sSUFBSSxDQUFDLENBQUM7QUFDNUYsQ0FBQztBQUVELE1BQU1vSSxnQkFBZ0IsR0FBR0EsQ0FBQ2xWLEtBQVksRUFBRThKLE1BQWEsRUFBRXdJLE9BQWUsRUFBRUMsT0FBZSxLQUFXO0VBQ2hHLEtBQUssSUFBSWxULENBQUMsR0FBR3lLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBR2tULE9BQU8sRUFBRWxULENBQUMsSUFBSXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBR2tULE9BQU8sRUFBRWxULENBQUMsRUFBRSxFQUFFLEtBQUssSUFBSUQsQ0FBQyxHQUFHMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHa1QsT0FBTyxFQUFFbFQsQ0FBQyxJQUFJMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHa1QsT0FBTyxFQUFFbFQsQ0FBQyxFQUFFLEVBQUU7SUFDNUgsTUFBTTZDLEVBQUUsR0FBRyxDQUFDN0MsQ0FBQyxHQUFHMEssTUFBTSxDQUFDMUssQ0FBQyxJQUFJa1QsT0FBTztJQUNuQyxNQUFNcFEsRUFBRSxHQUFHLENBQUM3QyxDQUFDLEdBQUd5SyxNQUFNLENBQUN6SyxDQUFDLElBQUlrVCxPQUFPO0lBQ25DLE1BQU1DLFFBQVEsR0FBR3ZRLEVBQUUsR0FBR0EsRUFBRSxHQUFHQyxFQUFFLEdBQUdBLEVBQUUsR0FBRyxDQUFDLENBQUM5QyxDQUFDLEdBQUcsRUFBRSxHQUFHQyxDQUFDLEdBQUcsQ0FBQyxJQUFJLENBQUMsR0FBRyxDQUFDLElBQUksR0FBRztJQUNyRSxJQUFJbVQsUUFBUSxJQUFJLEdBQUcsSUFBSUEsUUFBUSxJQUFJLElBQUksRUFBRXpJLE9BQU8sQ0FBQy9KLEtBQUssRUFBRVosQ0FBQyxFQUFFQyxDQUFDLEVBQUUsT0FBTyxDQUFDLE1BQ2pFLElBQUltVCxRQUFRLEdBQUcsR0FBRyxJQUFJLENBQUNwVCxDQUFDLEdBQUcsQ0FBQyxHQUFHQyxDQUFDLElBQUksQ0FBQyxLQUFLLENBQUMsRUFBRTBLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRVosQ0FBQyxFQUFFQyxDQUFDLEVBQUUsV0FBVyxDQUFDO0VBQ3JGO0FBQ0YsQ0FBQztBQUVELE1BQU04VixxQkFBcUIsR0FBR0EsQ0FBQ25WLEtBQVksRUFBRTBULE1BQXdCLEtBQVc7RUFDOUUsS0FBSyxJQUFJeFQsS0FBSyxHQUFHLENBQUMsRUFBRUEsS0FBSyxHQUFHd1QsTUFBTSxDQUFDN1AsTUFBTSxFQUFFM0QsS0FBSyxFQUFFLEVBQUU7SUFDbEQsTUFBTWdKLElBQUksR0FBR3dLLE1BQU0sQ0FBQ3hULEtBQUssR0FBRyxDQUFDLENBQUU7SUFDL0IsTUFBTThRLEVBQUUsR0FBRzBDLE1BQU0sQ0FBQ3hULEtBQUssQ0FBRTtJQUN6QixNQUFNd1UsS0FBSyxHQUFHeE8sSUFBSSxDQUFDQyxHQUFHLENBQUNELElBQUksQ0FBQ2tKLEdBQUcsQ0FBQzRCLEVBQUUsQ0FBQzVSLENBQUMsR0FBRzhKLElBQUksQ0FBQzlKLENBQUMsQ0FBQyxFQUFFOEcsSUFBSSxDQUFDa0osR0FBRyxDQUFDNEIsRUFBRSxDQUFDM1IsQ0FBQyxHQUFHNkosSUFBSSxDQUFDN0osQ0FBQyxDQUFDLENBQUM7SUFDeEUsS0FBSyxJQUFJc1YsSUFBSSxHQUFHLENBQUMsRUFBRUEsSUFBSSxJQUFJRCxLQUFLLEVBQUVDLElBQUksRUFBRSxFQUFFNUssT0FBTyxDQUFDL0osS0FBSyxFQUFFa0csSUFBSSxDQUFDME8sS0FBSyxDQUFDMUwsSUFBSSxDQUFDOUosQ0FBQyxHQUFHLENBQUM0UixFQUFFLENBQUM1UixDQUFDLEdBQUc4SixJQUFJLENBQUM5SixDQUFDLElBQUl1VixJQUFJLEdBQUdELEtBQUssQ0FBQyxFQUFFeE8sSUFBSSxDQUFDME8sS0FBSyxDQUFDMUwsSUFBSSxDQUFDN0osQ0FBQyxHQUFHLENBQUMyUixFQUFFLENBQUMzUixDQUFDLEdBQUc2SixJQUFJLENBQUM3SixDQUFDLElBQUlzVixJQUFJLEdBQUdELEtBQUssQ0FBQyxFQUFFLFlBQVksQ0FBQztFQUNsTDtBQUNGLENBQUM7QUFFRCxNQUFNVSxvQkFBb0IsR0FBR0EsQ0FBQ3BWLEtBQVksRUFBRXFPLE9BQWUsS0FBVztFQUNwRSxNQUFNdkUsTUFBTSxHQUFHO0lBQUUxSyxDQUFDLEVBQUU4RyxJQUFJLENBQUNsRyxLQUFLLENBQUNBLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDLENBQUM7SUFBRXBKLENBQUMsRUFBRTZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ0EsS0FBSyxDQUFDMEksTUFBTSxHQUFHLENBQUM7RUFBRSxDQUFDO0VBQ2xGLElBQUkyRixPQUFPLEtBQUsscUJBQXFCLEVBQUU7SUFDckM7SUFBQyxDQUFDO01BQUVqUCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsQ0FBQztNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRyxDQUFDLENBQUMsQ0FBQzZRLE9BQU8sQ0FBQyxDQUFDL1EsS0FBSyxFQUFFZSxLQUFLLEtBQUtnVixnQkFBZ0IsQ0FBQ2xWLEtBQUssRUFBRWIsS0FBSyxFQUFFLENBQUMsR0FBR2UsS0FBSyxHQUFHLENBQUMsRUFBRSxDQUFDLEdBQUdBLEtBQUssR0FBRyxDQUFDLENBQUMsQ0FBQztJQUNyUGlWLHFCQUFxQixDQUFDblYsS0FBSyxFQUFFLENBQUM7TUFBRVosQ0FBQyxFQUFFLENBQUM7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsQ0FBQztNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRVksS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUM7TUFBRXBKLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsQ0FBQyxDQUFDO0VBQ3hOLENBQUMsTUFBTSxJQUFJZ1AsT0FBTyxLQUFLLGtCQUFrQixFQUFFO0lBQ3pDO0lBQUMsQ0FBQztNQUFFalAsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLENBQUMsQ0FBQzZRLE9BQU8sQ0FBQyxDQUFDL1EsS0FBSyxFQUFFZSxLQUFLLEtBQUtnVixnQkFBZ0IsQ0FBQ2xWLEtBQUssRUFBRWIsS0FBSyxFQUFFLENBQUMsR0FBR2UsS0FBSyxHQUFHLENBQUMsRUFBRSxDQUFDLEdBQUdBLEtBQUssR0FBRyxDQUFDLENBQUMsQ0FBQztJQUNwTWlWLHFCQUFxQixDQUFDblYsS0FBSyxFQUFFLENBQUM7TUFBRVosQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsQ0FBQztNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRVcsS0FBSyxDQUFDMEksTUFBTSxHQUFHO0lBQUUsQ0FBQyxDQUFDLENBQUM7RUFDekwsQ0FBQyxNQUFNLElBQUkyRixPQUFPLEtBQUssMEJBQTBCLEVBQUU7SUFDakQsS0FBSyxJQUFJZ0gsT0FBTyxHQUFHLENBQUMsRUFBRUEsT0FBTyxHQUFHLENBQUMsRUFBRUEsT0FBTyxFQUFFLEVBQUU7TUFDNUMsTUFBTXJULE1BQU0sR0FBRztRQUFFNUMsQ0FBQyxFQUFFLENBQUMsR0FBSWlXLE9BQU8sR0FBRyxFQUFFLElBQUtyVixLQUFLLENBQUN5SSxLQUFLLEdBQUcsRUFBRSxDQUFDO1FBQUVwSixDQUFDLEVBQUUsQ0FBQyxHQUFJZ1csT0FBTyxHQUFHLEVBQUUsSUFBS3JWLEtBQUssQ0FBQzBJLE1BQU0sR0FBRyxFQUFFO01BQUUsQ0FBQztNQUMxR3dNLGdCQUFnQixDQUFDbFYsS0FBSyxFQUFFZ0MsTUFBTSxFQUFFLENBQUMsR0FBR3FULE9BQU8sR0FBRyxDQUFDLEVBQUUsQ0FBQyxHQUFHLENBQUNBLE9BQU8sR0FBRyxDQUFDLElBQUksQ0FBQyxDQUFDO0lBQ3pFO0lBQ0FGLHFCQUFxQixDQUFDblYsS0FBSyxFQUFFLENBQUM7TUFBRVosQ0FBQyxFQUFFLENBQUM7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRVksS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUM7TUFBRXBKLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsQ0FBQyxDQUFDO0VBQ3hMLENBQUMsTUFBTSxJQUFJZ1AsT0FBTyxLQUFLLGdCQUFnQixFQUFFO0lBQ3ZDO0lBQUMsQ0FBQztNQUFFalAsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUcsQ0FBQyxDQUFDLENBQUM2USxPQUFPLENBQUMsQ0FBQy9RLEtBQUssRUFBRWUsS0FBSyxLQUFLZ1YsZ0JBQWdCLENBQUNsVixLQUFLLEVBQUViLEtBQUssRUFBRSxDQUFDLEVBQUUsQ0FBQyxHQUFHZSxLQUFLLEdBQUcsQ0FBQyxDQUFDLENBQUM7SUFDM08sS0FBSyxJQUFJYixDQUFDLEdBQUd5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLElBQUl5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEVBQUUsRUFBRSxLQUFLLElBQUlELENBQUMsR0FBRzBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFLEVBQUVBLENBQUMsSUFBSTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFLEVBQUVBLENBQUMsRUFBRSxFQUFFLElBQUs4RyxJQUFJLENBQUNrSixHQUFHLENBQUNoUSxDQUFDLEdBQUcwSyxNQUFNLENBQUMxSyxDQUFDLENBQUMsR0FBRzhHLElBQUksQ0FBQ2tKLEdBQUcsQ0FBQy9QLENBQUMsR0FBR3lLLE1BQU0sQ0FBQ3pLLENBQUMsQ0FBQyxHQUFHLEVBQUUsSUFBSyxDQUFDRCxDQUFDLEdBQUcsQ0FBQyxHQUFHQyxDQUFDLElBQUksQ0FBQyxLQUFLLENBQUMsRUFBRTBLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRVosQ0FBQyxFQUFFQyxDQUFDLEVBQUUsU0FBUyxDQUFDO0lBQzVOOFYscUJBQXFCLENBQUNuVixLQUFLLEVBQUUsQ0FBQztNQUFFWixDQUFDLEVBQUUsQ0FBQztNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUM7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUVZLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDO01BQUVwSixDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLENBQUMsQ0FBQztFQUNwTixDQUFDLE1BQU07SUFDTDtJQUFDLENBQUM7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUM7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDeks7SUFBRSxDQUFDLENBQUMsQ0FBQzZRLE9BQU8sQ0FBQyxDQUFDL1EsS0FBSyxFQUFFZSxLQUFLLEtBQUtnVixnQkFBZ0IsQ0FBQ2xWLEtBQUssRUFBRWIsS0FBSyxFQUFFLENBQUMsR0FBR2UsS0FBSyxHQUFHLENBQUMsRUFBRSxDQUFDLEdBQUcsQ0FBQ0EsS0FBSyxHQUFHLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQztJQUM1UmlWLHFCQUFxQixDQUFDblYsS0FBSyxFQUFFLENBQUM7TUFBRVosQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLENBQUMsQ0FBQztFQUM3UTtBQUNGLENBQUM7QUFFRCxNQUFNeVMsOEJBQThCLEdBQUdBLENBQUM5UixLQUFZLEVBQUU0RyxRQUFrRCxFQUFFZ0MsS0FBdUIsRUFBRStLLElBQVMsS0FBYTtFQUN2SixJQUFJL00sUUFBUSxDQUFDWSxLQUFLLEtBQUt4SCxLQUFLLENBQUN3SCxLQUFLLEVBQUUsTUFBTSxJQUFJRCxLQUFLLENBQUMsa0JBQWtCWCxRQUFRLENBQUM3RSxFQUFFLG1DQUFtQyxDQUFDO0VBQ3JILElBQUk2RSxRQUFRLENBQUN1QixRQUFRLEtBQUtuSSxLQUFLLENBQUN5SCxRQUFRLEVBQUUsTUFBTSxJQUFJRixLQUFLLENBQUMsa0JBQWtCWCxRQUFRLENBQUM3RSxFQUFFLHFDQUFxQyxDQUFDO0VBQzdIME4sU0FBUyxDQUFDelAsS0FBSyxFQUFFO0lBQUVaLENBQUMsRUFBRSxDQUFDO0lBQUVDLENBQUMsRUFBRSxDQUFDO0lBQUUwTixDQUFDLEVBQUUvTSxLQUFLLENBQUN5SSxLQUFLLEdBQUcsQ0FBQztJQUFFdUUsQ0FBQyxFQUFFaE4sS0FBSyxDQUFDMEksTUFBTSxHQUFHO0VBQUUsQ0FBQyxDQUFDO0VBQ3pFME0sb0JBQW9CLENBQUNwVixLQUFLLEVBQUVBLEtBQUssQ0FBQ3lILFFBQVEsQ0FBQzRJLE9BQU8sQ0FBQyxRQUFRLEVBQUUsRUFBRSxDQUFDLENBQUM7RUFDakUsTUFBTTRCLE1BQU0sR0FBSTdLLElBQXVDLEtBQVk7SUFBRWhJLENBQUMsRUFBRWdJLElBQUksQ0FBQzhLLFNBQVMsQ0FBQzlTLENBQUM7SUFBRUMsQ0FBQyxFQUFFK0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDN1MsQ0FBQztJQUFFME4sQ0FBQyxFQUFFM0YsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSztJQUFFdUUsQ0FBQyxFQUFFNUYsSUFBSSxDQUFDOEssU0FBUyxDQUFDeEo7RUFBTyxDQUFDLENBQUM7RUFDbkssTUFBTWlCLEtBQUssR0FBR2YsS0FBSyxDQUFDdkIsS0FBSyxDQUFDMUIsR0FBRyxDQUFDc00sTUFBTSxDQUFDO0VBQ3JDdEksS0FBSyxDQUFDdUcsT0FBTyxDQUFDcEQsSUFBSSxJQUFJMkMsU0FBUyxDQUFDelAsS0FBSyxFQUFFOE0sSUFBSSxDQUFDLENBQUM7RUFDN0MvTyxvQkFBb0IsQ0FBQzZLLEtBQUssQ0FBQyxDQUFDc0gsT0FBTyxDQUFDL1EsS0FBSyxJQUFJNEssT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUUsT0FBTyxDQUFDLENBQUM7RUFDdkYsTUFBTThTLE1BQU0sR0FBRyxJQUFJOU8sR0FBRyxDQUFDdUYsS0FBSyxDQUFDdkIsS0FBSyxDQUFDMUIsR0FBRyxDQUFDeUIsSUFBSSxJQUFJLENBQUNBLElBQUksQ0FBQ3JJLElBQUksRUFBRWtULE1BQU0sQ0FBQzdLLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUMxRSxNQUFNZ0wsT0FBd0IsR0FBRyxDQUFDLE9BQU8sRUFBRSxVQUFVLEVBQUUsTUFBTSxFQUFFLGdCQUFnQixFQUFFLFdBQVcsRUFBRXBTLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEdBQUcsTUFBTSxHQUFHLE1BQU0sQ0FBQztFQUN0SSxPQUFPa1MsT0FBTyxDQUFDek0sR0FBRyxDQUFDNUcsSUFBSSxJQUFJb1QsTUFBTSxDQUFDcE8sR0FBRyxDQUFDaEYsSUFBSSxDQUFDLENBQUMsQ0FBQzZHLE1BQU0sQ0FBRWtILElBQUksSUFBbUJoTSxPQUFPLENBQUNnTSxJQUFJLENBQUMsQ0FBQztBQUM1RixDQUFDO0FBRUQsTUFBTXdJLGFBQWEsR0FBR0EsQ0FBQ3RWLEtBQVksRUFBRWtKLElBQVcsRUFBRThILEVBQVMsRUFBRWpTLElBQWtCLEtBQVc7RUFDeEYsSUFBSW1LLElBQUksQ0FBQzlKLENBQUMsS0FBSzRSLEVBQUUsQ0FBQzVSLENBQUMsRUFBRSxLQUFLLElBQUlDLENBQUMsR0FBRzZHLElBQUksQ0FBQ0UsR0FBRyxDQUFDOEMsSUFBSSxDQUFDN0osQ0FBQyxFQUFFMlIsRUFBRSxDQUFDM1IsQ0FBQyxDQUFDLEVBQUVBLENBQUMsSUFBSTZHLElBQUksQ0FBQ0MsR0FBRyxDQUFDK0MsSUFBSSxDQUFDN0osQ0FBQyxFQUFFMlIsRUFBRSxDQUFDM1IsQ0FBQyxDQUFDLEVBQUVBLENBQUMsRUFBRSxFQUFFMEssT0FBTyxDQUFDL0osS0FBSyxFQUFFa0osSUFBSSxDQUFDOUosQ0FBQyxFQUFFQyxDQUFDLEVBQUVOLElBQUksQ0FBQyxNQUN0SCxLQUFLLElBQUlLLENBQUMsR0FBRzhHLElBQUksQ0FBQ0UsR0FBRyxDQUFDOEMsSUFBSSxDQUFDOUosQ0FBQyxFQUFFNFIsRUFBRSxDQUFDNVIsQ0FBQyxDQUFDLEVBQUVBLENBQUMsSUFBSThHLElBQUksQ0FBQ0MsR0FBRyxDQUFDK0MsSUFBSSxDQUFDOUosQ0FBQyxFQUFFNFIsRUFBRSxDQUFDNVIsQ0FBQyxDQUFDLEVBQUVBLENBQUMsRUFBRSxFQUFFMkssT0FBTyxDQUFDL0osS0FBSyxFQUFFWixDQUFDLEVBQUU4SixJQUFJLENBQUM3SixDQUFDLEVBQUVOLElBQUksQ0FBQztBQUM3RyxDQUFDO0FBRUQsTUFBTXdXLGNBQWMsR0FBR0EsQ0FBQ3ZWLEtBQVksRUFBRXdWLElBQVksRUFBRUMsR0FBVyxFQUFFaE4sS0FBYSxFQUFFQyxNQUFjLEVBQUUzSixJQUFrQixLQUFXO0VBQzNILEtBQUssSUFBSU0sQ0FBQyxHQUFHb1csR0FBRyxFQUFFcFcsQ0FBQyxHQUFHb1csR0FBRyxHQUFHL00sTUFBTSxFQUFFckosQ0FBQyxFQUFFLEVBQUUsS0FBSyxJQUFJRCxDQUFDLEdBQUdvVyxJQUFJLEVBQUVwVyxDQUFDLEdBQUdvVyxJQUFJLEdBQUcvTSxLQUFLLEVBQUVySixDQUFDLEVBQUUsRUFBRTJLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRVosQ0FBQyxFQUFFQyxDQUFDLEVBQUVOLElBQUksQ0FBQztBQUMvRyxDQUFDO0FBRUQsTUFBTTJXLGtCQUFrQixHQUFHQSxDQUFDMVYsS0FBWSxFQUFFcU8sT0FBZSxLQUFXO0VBQ2xFLE1BQU12RSxNQUFNLEdBQUc7SUFBRTFLLENBQUMsRUFBRThHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ0EsS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUMsQ0FBQztJQUFFcEosQ0FBQyxFQUFFNkcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDQSxLQUFLLENBQUMwSSxNQUFNLEdBQUcsQ0FBQztFQUFFLENBQUM7RUFDbEYsSUFBSTJGLE9BQU8sS0FBSyxvQkFBb0IsRUFBRTtJQUNwQyxLQUFLLElBQUlqUCxDQUFDLEdBQUcsRUFBRSxFQUFFdVcsTUFBTSxHQUFHLENBQUMsRUFBRXZXLENBQUMsR0FBR1ksS0FBSyxDQUFDeUksS0FBSyxHQUFHLEVBQUUsRUFBRXJKLENBQUMsSUFBSSxFQUFFLEVBQUV1VyxNQUFNLEVBQUUsRUFBRTtNQUNwRUwsYUFBYSxDQUFDdFYsS0FBSyxFQUFFO1FBQUVaLENBQUM7UUFBRUMsQ0FBQyxFQUFFO01BQUUsQ0FBQyxFQUFFO1FBQUVELENBQUM7UUFBRUMsQ0FBQyxFQUFFVyxLQUFLLENBQUMwSSxNQUFNLEdBQUc7TUFBRSxDQUFDLEVBQUUsT0FBTyxDQUFDO01BQ3RFLE1BQU1tSCxHQUFHLEdBQUc4RixNQUFNLEdBQUcsQ0FBQyxHQUFHLEVBQUUsR0FBRzNWLEtBQUssQ0FBQzBJLE1BQU0sR0FBRyxFQUFFO01BQy9DNk0sY0FBYyxDQUFDdlYsS0FBSyxFQUFFWixDQUFDLEdBQUcsQ0FBQyxFQUFFeVEsR0FBRyxHQUFHLENBQUMsRUFBRSxDQUFDLEVBQUUsQ0FBQyxFQUFFLE9BQU8sQ0FBQztJQUN0RDtFQUNGLENBQUMsTUFBTSxJQUFJeEIsT0FBTyxLQUFLLFlBQVksRUFBRTtJQUNuQyxLQUFLLElBQUlqUCxDQUFDLEdBQUcsRUFBRSxFQUFFQSxDQUFDLEdBQUdZLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDLEVBQUVySixDQUFDLElBQUksRUFBRSxFQUFFa1csYUFBYSxDQUFDdFYsS0FBSyxFQUFFO01BQUVaLENBQUM7TUFBRUMsQ0FBQyxFQUFFO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUM7TUFBRUMsQ0FBQyxFQUFFVyxLQUFLLENBQUMwSSxNQUFNLEdBQUc7SUFBRSxDQUFDLEVBQUUsT0FBTyxDQUFDO0lBQ3JILEtBQUssSUFBSXJKLENBQUMsR0FBRyxFQUFFLEVBQUVBLENBQUMsR0FBR1csS0FBSyxDQUFDMEksTUFBTSxHQUFHLENBQUMsRUFBRXJKLENBQUMsSUFBSSxFQUFFLEVBQUVpVyxhQUFhLENBQUN0VixLQUFLLEVBQUU7TUFBRVosQ0FBQyxFQUFFLENBQUM7TUFBRUM7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFWSxLQUFLLENBQUN5SSxLQUFLLEdBQUcsQ0FBQztNQUFFcEo7SUFBRSxDQUFDLEVBQUUsT0FBTyxDQUFDO0lBQ3JIa1csY0FBYyxDQUFDdlYsS0FBSyxFQUFFOEosTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUMsRUFBRTBLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRyxDQUFDLEVBQUUsRUFBRSxFQUFFLENBQUMsRUFBRSxPQUFPLENBQUM7RUFDbkUsQ0FBQyxNQUFNLElBQUlnUCxPQUFPLEtBQUssa0JBQWtCLEVBQUU7SUFDekNpSCxhQUFhLENBQUN0VixLQUFLLEVBQUU7TUFBRVosQ0FBQyxFQUFFLENBQUM7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDeks7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFWSxLQUFLLENBQUN5SSxLQUFLLEdBQUcsQ0FBQztNQUFFcEosQ0FBQyxFQUFFeUssTUFBTSxDQUFDeks7SUFBRSxDQUFDLEVBQUUsWUFBWSxDQUFDO0lBQzlGaVcsYUFBYSxDQUFDdFYsS0FBSyxFQUFFO01BQUVaLENBQUMsRUFBRSxDQUFDO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUVZLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDO01BQUVwSixDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUUsT0FBTyxDQUFDO0lBQ2pHaVcsYUFBYSxDQUFDdFYsS0FBSyxFQUFFO01BQUVaLENBQUMsRUFBRSxDQUFDO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUVZLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDO01BQUVwSixDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUUsT0FBTyxDQUFDO0VBQ25HLENBQUMsTUFBTSxJQUFJZ1AsT0FBTyxLQUFLLG1CQUFtQixFQUFFO0lBQzFDLE1BQU1tSCxJQUFJLEdBQUcxTCxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtJQUMxQixNQUFNd1csS0FBSyxHQUFHOUwsTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7SUFDM0IsTUFBTXFXLEdBQUcsR0FBRzNMLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRyxFQUFFO0lBQ3pCLE1BQU13VyxNQUFNLEdBQUcvTCxNQUFNLENBQUN6SyxDQUFDLEdBQUcsRUFBRTtJQUM1QmlXLGFBQWEsQ0FBQ3RWLEtBQUssRUFBRTtNQUFFWixDQUFDLEVBQUVvVyxJQUFJO01BQUVuVyxDQUFDLEVBQUVvVztJQUFJLENBQUMsRUFBRTtNQUFFclcsQ0FBQyxFQUFFd1csS0FBSztNQUFFdlcsQ0FBQyxFQUFFb1c7SUFBSSxDQUFDLEVBQUUsWUFBWSxDQUFDO0lBQzdFSCxhQUFhLENBQUN0VixLQUFLLEVBQUU7TUFBRVosQ0FBQyxFQUFFd1csS0FBSztNQUFFdlcsQ0FBQyxFQUFFb1c7SUFBSSxDQUFDLEVBQUU7TUFBRXJXLENBQUMsRUFBRXdXLEtBQUs7TUFBRXZXLENBQUMsRUFBRXdXO0lBQU8sQ0FBQyxFQUFFLFlBQVksQ0FBQztJQUNqRlAsYUFBYSxDQUFDdFYsS0FBSyxFQUFFO01BQUVaLENBQUMsRUFBRXdXLEtBQUs7TUFBRXZXLENBQUMsRUFBRXdXO0lBQU8sQ0FBQyxFQUFFO01BQUV6VyxDQUFDLEVBQUVvVyxJQUFJO01BQUVuVyxDQUFDLEVBQUV3VztJQUFPLENBQUMsRUFBRSxZQUFZLENBQUM7SUFDbkZQLGFBQWEsQ0FBQ3RWLEtBQUssRUFBRTtNQUFFWixDQUFDLEVBQUVvVyxJQUFJO01BQUVuVyxDQUFDLEVBQUV3VztJQUFPLENBQUMsRUFBRTtNQUFFelcsQ0FBQyxFQUFFb1csSUFBSTtNQUFFblcsQ0FBQyxFQUFFb1c7SUFBSSxDQUFDLEVBQUUsWUFBWSxDQUFDO0lBQy9FRixjQUFjLENBQUN2VixLQUFLLEVBQUU4SixNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRSxFQUFFMEssTUFBTSxDQUFDekssQ0FBQyxHQUFHLENBQUMsRUFBRSxFQUFFLEVBQUUsRUFBRSxFQUFFLE9BQU8sQ0FBQztFQUNyRSxDQUFDLE1BQU07SUFDTCxLQUFLLElBQUlBLENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBR1csS0FBSyxDQUFDMEksTUFBTSxHQUFHLENBQUMsRUFBRXJKLENBQUMsSUFBSSxFQUFFLEVBQUU7TUFDN0NpVyxhQUFhLENBQUN0VixLQUFLLEVBQUU7UUFBRVosQ0FBQyxFQUFFLENBQUM7UUFBRUM7TUFBRSxDQUFDLEVBQUU7UUFBRUQsQ0FBQyxFQUFFWSxLQUFLLENBQUN5SSxLQUFLLEdBQUcsQ0FBQztRQUFFcEo7TUFBRSxDQUFDLEVBQUUsU0FBUyxDQUFDO01BQ3ZFa1csY0FBYyxDQUFDdlYsS0FBSyxFQUFFOEosTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUMsRUFBRUMsQ0FBQyxHQUFHLENBQUMsRUFBRSxFQUFFLEVBQUUsQ0FBQyxFQUFFLE9BQU8sQ0FBQztJQUM1RDtJQUNBa1csY0FBYyxDQUFDdlYsS0FBSyxFQUFFOEosTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUUsRUFBRTBLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRyxDQUFDLEVBQUUsRUFBRSxFQUFFLEVBQUUsRUFBRSxZQUFZLENBQUM7RUFDMUU7QUFDRixDQUFDO0FBRUQsTUFBTTBTLGlDQUFpQyxHQUFHQSxDQUFDL1IsS0FBWSxFQUFFNEcsUUFBa0QsRUFBRWdDLEtBQXVCLEVBQUUrSyxJQUFTLEtBQWE7RUFDMUosSUFBSS9NLFFBQVEsQ0FBQ1ksS0FBSyxLQUFLeEgsS0FBSyxDQUFDd0gsS0FBSyxFQUFFLE1BQU0sSUFBSUQsS0FBSyxDQUFDLGtCQUFrQlgsUUFBUSxDQUFDN0UsRUFBRSxtQ0FBbUMsQ0FBQztFQUNySCxJQUFJNkUsUUFBUSxDQUFDdUIsUUFBUSxLQUFLbkksS0FBSyxDQUFDeUgsUUFBUSxFQUFFLE1BQU0sSUFBSUYsS0FBSyxDQUFDLGtCQUFrQlgsUUFBUSxDQUFDN0UsRUFBRSxxQ0FBcUMsQ0FBQztFQUM3SDBOLFNBQVMsQ0FBQ3pQLEtBQUssRUFBRTtJQUFFWixDQUFDLEVBQUUsQ0FBQztJQUFFQyxDQUFDLEVBQUUsQ0FBQztJQUFFME4sQ0FBQyxFQUFFL00sS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUM7SUFBRXVFLENBQUMsRUFBRWhOLEtBQUssQ0FBQzBJLE1BQU0sR0FBRztFQUFFLENBQUMsQ0FBQztFQUN6RWdOLGtCQUFrQixDQUFDMVYsS0FBSyxFQUFFQSxLQUFLLENBQUN5SCxRQUFRLENBQUM0SSxPQUFPLENBQUMsUUFBUSxFQUFFLEVBQUUsQ0FBQyxDQUFDO0VBQy9ELE1BQU00QixNQUFNLEdBQUk3SyxJQUF1QyxLQUFZO0lBQUVoSSxDQUFDLEVBQUVnSSxJQUFJLENBQUM4SyxTQUFTLENBQUM5UyxDQUFDO0lBQUVDLENBQUMsRUFBRStILElBQUksQ0FBQzhLLFNBQVMsQ0FBQzdTLENBQUM7SUFBRTBOLENBQUMsRUFBRTNGLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3pKLEtBQUs7SUFBRXVFLENBQUMsRUFBRTVGLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3hKO0VBQU8sQ0FBQyxDQUFDO0VBQ25LLE1BQU1pQixLQUFLLEdBQUdmLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzFCLEdBQUcsQ0FBQ3NNLE1BQU0sQ0FBQztFQUNyQ3RJLEtBQUssQ0FBQ3VHLE9BQU8sQ0FBQ3BELElBQUksSUFBSTJDLFNBQVMsQ0FBQ3pQLEtBQUssRUFBRThNLElBQUksQ0FBQyxDQUFDO0VBQzdDL08sb0JBQW9CLENBQUM2SyxLQUFLLENBQUMsQ0FBQ3NILE9BQU8sQ0FBQy9RLEtBQUssSUFBSTRLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFLE9BQU8sQ0FBQyxDQUFDO0VBQ3ZGLE1BQU04UyxNQUFNLEdBQUcsSUFBSTlPLEdBQUcsQ0FBQ3VGLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzFCLEdBQUcsQ0FBQ3lCLElBQUksSUFBSSxDQUFDQSxJQUFJLENBQUNySSxJQUFJLEVBQUVrVCxNQUFNLENBQUM3SyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDMUUsTUFBTWdMLE9BQXdCLEdBQUcsQ0FBQyxPQUFPLEVBQUUsVUFBVSxFQUFFLE1BQU0sRUFBRSxnQkFBZ0IsRUFBRSxXQUFXLEVBQUVwUyxLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLE1BQU0sR0FBRyxNQUFNLENBQUM7RUFDdEksT0FBT2tTLE9BQU8sQ0FBQ3pNLEdBQUcsQ0FBQzVHLElBQUksSUFBSW9ULE1BQU0sQ0FBQ3BPLEdBQUcsQ0FBQ2hGLElBQUksQ0FBQyxDQUFDLENBQUM2RyxNQUFNLENBQUVrSCxJQUFJLElBQW1CaE0sT0FBTyxDQUFDZ00sSUFBSSxDQUFDLENBQUM7QUFDNUYsQ0FBQztBQUVELE1BQU1nSixlQUFlLEdBQUdBLENBQUM5VixLQUFZLEVBQUU4SixNQUFhLEVBQUV3SSxPQUFlLEVBQUVDLE9BQWUsRUFBRXhULElBQWtCLEtBQVc7RUFDbkgsS0FBSyxJQUFJTSxDQUFDLEdBQUd5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUdrVCxPQUFPLEVBQUVsVCxDQUFDLElBQUl5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUdrVCxPQUFPLEVBQUVsVCxDQUFDLEVBQUUsRUFBRSxLQUFLLElBQUlELENBQUMsR0FBRzBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBR2tULE9BQU8sRUFBRWxULENBQUMsSUFBSTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBR2tULE9BQU8sRUFBRWxULENBQUMsRUFBRSxFQUFFLElBQUksQ0FBQ0EsQ0FBQyxHQUFHMEssTUFBTSxDQUFDMUssQ0FBQyxLQUFLLENBQUMsR0FBR2tULE9BQU8sSUFBSSxDQUFDLEdBQUcsQ0FBQ2pULENBQUMsR0FBR3lLLE1BQU0sQ0FBQ3pLLENBQUMsS0FBSyxDQUFDLEdBQUdrVCxPQUFPLElBQUksQ0FBQyxHQUFHLENBQUMsQ0FBQ25ULENBQUMsR0FBRyxDQUFDLEdBQUdDLENBQUMsR0FBRyxDQUFDLElBQUksQ0FBQyxHQUFHLENBQUMsSUFBSSxHQUFHLEdBQUcsQ0FBQyxFQUFFMEssT0FBTyxDQUFDL0osS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsRUFBRU4sSUFBSSxDQUFDO0FBQzdRLENBQUM7QUFFRCxNQUFNZ1gsZUFBZSxHQUFHQSxDQUFDL1YsS0FBWSxFQUFFMFQsTUFBd0IsS0FBVztFQUN4RSxLQUFLLElBQUl4VCxLQUFLLEdBQUcsQ0FBQyxFQUFFQSxLQUFLLEdBQUd3VCxNQUFNLENBQUM3UCxNQUFNLEVBQUUzRCxLQUFLLEVBQUUsRUFBRTtJQUNsRCxNQUFNZ0osSUFBSSxHQUFHd0ssTUFBTSxDQUFDeFQsS0FBSyxHQUFHLENBQUMsQ0FBRTtJQUMvQixNQUFNOFEsRUFBRSxHQUFHMEMsTUFBTSxDQUFDeFQsS0FBSyxDQUFFO0lBQ3pCLE1BQU13VSxLQUFLLEdBQUd4TyxJQUFJLENBQUNDLEdBQUcsQ0FBQ0QsSUFBSSxDQUFDa0osR0FBRyxDQUFDNEIsRUFBRSxDQUFDNVIsQ0FBQyxHQUFHOEosSUFBSSxDQUFDOUosQ0FBQyxDQUFDLEVBQUU4RyxJQUFJLENBQUNrSixHQUFHLENBQUM0QixFQUFFLENBQUMzUixDQUFDLEdBQUc2SixJQUFJLENBQUM3SixDQUFDLENBQUMsQ0FBQztJQUN4RSxLQUFLLElBQUlzVixJQUFJLEdBQUcsQ0FBQyxFQUFFQSxJQUFJLElBQUlELEtBQUssRUFBRUMsSUFBSSxFQUFFLEVBQUU1SyxPQUFPLENBQUMvSixLQUFLLEVBQUVrRyxJQUFJLENBQUMwTyxLQUFLLENBQUMxTCxJQUFJLENBQUM5SixDQUFDLEdBQUcsQ0FBQzRSLEVBQUUsQ0FBQzVSLENBQUMsR0FBRzhKLElBQUksQ0FBQzlKLENBQUMsSUFBSXVWLElBQUksR0FBR0QsS0FBSyxDQUFDLEVBQUV4TyxJQUFJLENBQUMwTyxLQUFLLENBQUMxTCxJQUFJLENBQUM3SixDQUFDLEdBQUcsQ0FBQzJSLEVBQUUsQ0FBQzNSLENBQUMsR0FBRzZKLElBQUksQ0FBQzdKLENBQUMsSUFBSXNWLElBQUksR0FBR0QsS0FBSyxDQUFDLEVBQUUsV0FBVyxDQUFDO0VBQ2pMO0FBQ0YsQ0FBQztBQUVELE1BQU1zQixtQkFBbUIsR0FBR0EsQ0FBQ2hXLEtBQVksRUFBRXFPLE9BQWUsS0FBVztFQUNuRSxNQUFNdkUsTUFBTSxHQUFHO0lBQUUxSyxDQUFDLEVBQUU4RyxJQUFJLENBQUNsRyxLQUFLLENBQUNBLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDLENBQUM7SUFBRXBKLENBQUMsRUFBRTZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ0EsS0FBSyxDQUFDMEksTUFBTSxHQUFHLENBQUM7RUFBRSxDQUFDO0VBQ2xGLElBQUkyRixPQUFPLEtBQUssc0JBQXNCLEVBQUU7SUFDdEN5SCxlQUFlLENBQUM5VixLQUFLLEVBQUU7TUFBRVosQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFLEVBQUUsRUFBRSxDQUFDLEVBQUUsS0FBSyxDQUFDO0lBQzNFeVcsZUFBZSxDQUFDOVYsS0FBSyxFQUFFO01BQUVaLENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRSxFQUFFLEVBQUUsQ0FBQyxFQUFFLEtBQUssQ0FBQztJQUMzRTBXLGVBQWUsQ0FBQy9WLEtBQUssRUFBRSxDQUFDO01BQUVaLENBQUMsRUFBRSxDQUFDO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRVksS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUM7TUFBRXBKLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsQ0FBQyxDQUFDO0lBQ3BKMFcsZUFBZSxDQUFDL1YsS0FBSyxFQUFFLENBQUM7TUFBRVosQ0FBQyxFQUFFLENBQUM7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFWSxLQUFLLENBQUN5SSxLQUFLLEdBQUcsQ0FBQztNQUFFcEosQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxDQUFDLENBQUM7RUFDdEosQ0FBQyxNQUFNLElBQUlnUCxPQUFPLEtBQUssbUJBQW1CLEVBQUU7SUFDMUMwSCxlQUFlLENBQUMvVixLQUFLLEVBQUUsQ0FBQztNQUFFWixDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsQ0FBQyxDQUFDO0lBQ3JReVcsZUFBZSxDQUFDOVYsS0FBSyxFQUFFOEosTUFBTSxFQUFFLEVBQUUsRUFBRSxDQUFDLEVBQUUsS0FBSyxDQUFDO0VBQzlDLENBQUMsTUFBTSxJQUFJdUUsT0FBTyxLQUFLLHFCQUFxQixFQUFFO0lBQzVDLEtBQUssSUFBSWpQLENBQUMsR0FBRyxFQUFFLEVBQUVBLENBQUMsR0FBR1ksS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUMsRUFBRXJKLENBQUMsSUFBSSxFQUFFLEVBQUUyVyxlQUFlLENBQUMvVixLQUFLLEVBQUUsQ0FBQztNQUFFWixDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUVBLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUVBLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUVBLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRVcsS0FBSyxDQUFDMEksTUFBTSxHQUFHO0lBQUUsQ0FBQyxDQUFDLENBQUM7SUFDaEssQ0FBQztNQUFFdEosQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLENBQUMsQ0FBQzZRLE9BQU8sQ0FBQy9RLEtBQUssSUFBSTJXLGVBQWUsQ0FBQzlWLEtBQUssRUFBRWIsS0FBSyxFQUFFLEVBQUUsRUFBRSxDQUFDLEVBQUUsS0FBSyxDQUFDLENBQUM7RUFDdkgsQ0FBQyxNQUFNLElBQUlrUCxPQUFPLEtBQUssdUJBQXVCLEVBQUU7SUFDOUN5SCxlQUFlLENBQUM5VixLQUFLLEVBQUU7TUFBRVosQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFLENBQUMsRUFBRSxFQUFFLEVBQUUsS0FBSyxDQUFDO0lBQ2hFeVcsZUFBZSxDQUFDOVYsS0FBSyxFQUFFO01BQUVaLENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRSxFQUFFLEVBQUUsRUFBRSxFQUFFLEtBQUssQ0FBQztJQUNqRTBXLGVBQWUsQ0FBQy9WLEtBQUssRUFBRSxDQUFDO01BQUVaLENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsQ0FBQztNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRVcsS0FBSyxDQUFDMEksTUFBTSxHQUFHO0lBQUUsQ0FBQyxDQUFDLENBQUM7SUFDdEpxTixlQUFlLENBQUMvVixLQUFLLEVBQUUsQ0FBQztNQUFFWixDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFMEssTUFBTSxDQUFDMUssQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRTBLLE1BQU0sQ0FBQzFLLENBQUMsR0FBRyxDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUcsQ0FBQztNQUFFQyxDQUFDLEVBQUVXLEtBQUssQ0FBQzBJLE1BQU0sR0FBRztJQUFFLENBQUMsQ0FBQyxDQUFDO0VBQ3hKLENBQUMsTUFBTTtJQUNMO0lBQUMsQ0FBQztNQUFFdEosQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRXlLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBRztJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SyxDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHO0lBQUUsQ0FBQyxDQUFDLENBQUM2USxPQUFPLENBQUMsQ0FBQy9RLEtBQUssRUFBRWUsS0FBSyxLQUFLNFYsZUFBZSxDQUFDOVYsS0FBSyxFQUFFYixLQUFLLEVBQUUsQ0FBQyxHQUFHZSxLQUFLLEdBQUcsQ0FBQyxFQUFFLENBQUMsRUFBRSxLQUFLLENBQUMsQ0FBQztJQUNuTTZWLGVBQWUsQ0FBQy9WLEtBQUssRUFBRSxDQUFDO01BQUVaLENBQUMsRUFBRSxDQUFDO01BQUVDLENBQUMsRUFBRTtJQUFFLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRyxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRVcsS0FBSyxDQUFDMEksTUFBTSxHQUFHO0lBQUUsQ0FBQyxDQUFDLENBQUM7SUFDNUdxTixlQUFlLENBQUMvVixLQUFLLEVBQUUsQ0FBQztNQUFFWixDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUU7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFLEVBQUU7TUFBRUMsQ0FBQyxFQUFFO0lBQUcsQ0FBQyxFQUFFO01BQUVELENBQUMsRUFBRSxFQUFFO01BQUVDLENBQUMsRUFBRTtJQUFHLENBQUMsRUFBRTtNQUFFRCxDQUFDLEVBQUUsRUFBRTtNQUFFQyxDQUFDLEVBQUVXLEtBQUssQ0FBQzBJLE1BQU0sR0FBRztJQUFFLENBQUMsQ0FBQyxDQUFDO0VBQy9HO0FBQ0YsQ0FBQztBQUVELE1BQU1zSixzQ0FBc0MsR0FBR0EsQ0FBQ2hTLEtBQVksRUFBRTRHLFFBQWtELEVBQUVnQyxLQUF1QixFQUFFK0ssSUFBUyxLQUFhO0VBQy9KLElBQUkvTSxRQUFRLENBQUNZLEtBQUssS0FBS3hILEtBQUssQ0FBQ3dILEtBQUssRUFBRSxNQUFNLElBQUlELEtBQUssQ0FBQyxrQkFBa0JYLFFBQVEsQ0FBQzdFLEVBQUUsbUNBQW1DLENBQUM7RUFDckgsSUFBSTZFLFFBQVEsQ0FBQ3VCLFFBQVEsS0FBS25JLEtBQUssQ0FBQ3lILFFBQVEsRUFBRSxNQUFNLElBQUlGLEtBQUssQ0FBQyxrQkFBa0JYLFFBQVEsQ0FBQzdFLEVBQUUscUNBQXFDLENBQUM7RUFDN0gwTixTQUFTLENBQUN6UCxLQUFLLEVBQUU7SUFBRVosQ0FBQyxFQUFFLENBQUM7SUFBRUMsQ0FBQyxFQUFFLENBQUM7SUFBRTBOLENBQUMsRUFBRS9NLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDO0lBQUV1RSxDQUFDLEVBQUVoTixLQUFLLENBQUMwSSxNQUFNLEdBQUc7RUFBRSxDQUFDLENBQUM7RUFDekVzTixtQkFBbUIsQ0FBQ2hXLEtBQUssRUFBRUEsS0FBSyxDQUFDeUgsUUFBUSxDQUFDNEksT0FBTyxDQUFDLFFBQVEsRUFBRSxFQUFFLENBQUMsQ0FBQztFQUNoRSxNQUFNNEIsTUFBTSxHQUFJN0ssSUFBdUMsS0FBWTtJQUFFaEksQ0FBQyxFQUFFZ0ksSUFBSSxDQUFDOEssU0FBUyxDQUFDOVMsQ0FBQztJQUFFQyxDQUFDLEVBQUUrSCxJQUFJLENBQUM4SyxTQUFTLENBQUM3UyxDQUFDO0lBQUUwTixDQUFDLEVBQUUzRixJQUFJLENBQUM4SyxTQUFTLENBQUN6SixLQUFLO0lBQUV1RSxDQUFDLEVBQUU1RixJQUFJLENBQUM4SyxTQUFTLENBQUN4SjtFQUFPLENBQUMsQ0FBQztFQUNuSyxNQUFNaUIsS0FBSyxHQUFHZixLQUFLLENBQUN2QixLQUFLLENBQUMxQixHQUFHLENBQUNzTSxNQUFNLENBQUM7RUFDckN0SSxLQUFLLENBQUN1RyxPQUFPLENBQUNwRCxJQUFJLElBQUkyQyxTQUFTLENBQUN6UCxLQUFLLEVBQUU4TSxJQUFJLENBQUMsQ0FBQztFQUM3Qy9PLG9CQUFvQixDQUFDNkssS0FBSyxDQUFDLENBQUNzSCxPQUFPLENBQUMvUSxLQUFLLElBQUk0SyxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRSxPQUFPLENBQUMsQ0FBQztFQUN2RixNQUFNOFMsTUFBTSxHQUFHLElBQUk5TyxHQUFHLENBQUN1RixLQUFLLENBQUN2QixLQUFLLENBQUMxQixHQUFHLENBQUN5QixJQUFJLElBQUksQ0FBQ0EsSUFBSSxDQUFDckksSUFBSSxFQUFFa1QsTUFBTSxDQUFDN0ssSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDO0VBQzFFLE1BQU1nTCxPQUF3QixHQUFHLENBQUMsT0FBTyxFQUFFLFVBQVUsRUFBRSxNQUFNLEVBQUUsZ0JBQWdCLEVBQUUsV0FBVyxFQUFFcFMsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsR0FBRyxNQUFNLEdBQUcsTUFBTSxDQUFDO0VBQ3RJLE9BQU9rUyxPQUFPLENBQUN6TSxHQUFHLENBQUM1RyxJQUFJLElBQUlvVCxNQUFNLENBQUNwTyxHQUFHLENBQUNoRixJQUFJLENBQUMsQ0FBQyxDQUFDNkcsTUFBTSxDQUFFa0gsSUFBSSxJQUFtQmhNLE9BQU8sQ0FBQ2dNLElBQUksQ0FBQyxDQUFDO0FBQzVGLENBQUM7QUFFRCxNQUFNM0Msc0JBQXNCLEdBQUdBLENBQUNuSyxLQUFZLEVBQUU0SSxLQUF1QixFQUFFcU4sUUFBNkIsS0FBVztFQUM3RyxJQUFJLENBQUNBLFFBQVEsQ0FBQ3ZPLElBQUksRUFBRTtFQUNwQixLQUFLLE1BQU12SSxLQUFLLElBQUlwQixvQkFBb0IsQ0FBQzZLLEtBQUssQ0FBQyxFQUFFLElBQUlxTixRQUFRLENBQUMzVCxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsRUFBRTBLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFLE9BQU8sQ0FBQztBQUNoSixDQUFDO0FBRUQsTUFBTStLLHVCQUF1QixHQUFHQSxDQUFDcEssS0FBWSxFQUFFNEksS0FBdUIsS0FBVztFQUMvRSxLQUFLLE1BQU14QixJQUFJLElBQUl3QixLQUFLLENBQUN2QixLQUFLLEVBQUU7SUFDOUIsTUFBTXlDLE1BQU0sR0FBRztNQUFFMUssQ0FBQyxFQUFFZ0ksSUFBSSxDQUFDOEssU0FBUyxDQUFDOVMsQ0FBQyxHQUFHOEcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDb0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSyxHQUFHLENBQUMsQ0FBQztNQUFFcEosQ0FBQyxFQUFFK0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDN1MsQ0FBQyxHQUFHNkcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDb0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDeEosTUFBTSxHQUFHLENBQUM7SUFBRSxDQUFDO0lBQzFJLE1BQU13TixTQUFTLEdBQUd0TixLQUFLLENBQUN1TixLQUFLLENBQUNDLE9BQU8sQ0FBQ0MsSUFBSSxJQUFJLENBQUNBLElBQUksQ0FBQ25OLElBQUksRUFBRW1OLElBQUksQ0FBQ3JGLEVBQUUsQ0FBQyxDQUFDLENBQUNwTCxNQUFNLENBQUN6RyxLQUFLLElBQUlBLEtBQUssQ0FBQ0MsQ0FBQyxJQUFJZ0ksSUFBSSxDQUFDOEssU0FBUyxDQUFDOVMsQ0FBQyxJQUFJRCxLQUFLLENBQUNDLENBQUMsR0FBR2dJLElBQUksQ0FBQzhLLFNBQVMsQ0FBQzlTLENBQUMsR0FBR2dJLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3pKLEtBQUssSUFBSXRKLEtBQUssQ0FBQ0UsQ0FBQyxJQUFJK0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDN1MsQ0FBQyxJQUFJRixLQUFLLENBQUNFLENBQUMsR0FBRytILElBQUksQ0FBQzhLLFNBQVMsQ0FBQzdTLENBQUMsR0FBRytILElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3hKLE1BQU0sQ0FBQztJQUMxUCxNQUFNZ0wsTUFBTSxHQUFHMVQsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFNBQVMsR0FDcEN5QixLQUFLLENBQUNDLElBQUksQ0FBQztNQUFFckYsTUFBTSxFQUFFdUQsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSyxHQUFHckIsSUFBSSxDQUFDOEssU0FBUyxDQUFDeEo7SUFBTyxDQUFDLEVBQUUsQ0FBQzROLENBQUMsRUFBRXBXLEtBQUssTUFBTTtNQUFFZCxDQUFDLEVBQUVnSSxJQUFJLENBQUM4SyxTQUFTLENBQUM5UyxDQUFDLEdBQUdjLEtBQUssR0FBR2tILElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3pKLEtBQUs7TUFBRXBKLENBQUMsRUFBRStILElBQUksQ0FBQzhLLFNBQVMsQ0FBQzdTLENBQUMsR0FBRzZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ0UsS0FBSyxHQUFHa0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSztJQUFFLENBQUMsQ0FBQyxDQUFDLEdBQzVNeU4sU0FBUyxDQUFDRSxPQUFPLENBQUNHLFFBQVEsSUFBSTtNQUM5QixNQUFNQyxLQUFjLEdBQUcsRUFBRTtNQUN6QixLQUFLLElBQUlwWCxDQUFDLEdBQUc4RyxJQUFJLENBQUNFLEdBQUcsQ0FBQzBELE1BQU0sQ0FBQzFLLENBQUMsRUFBRW1YLFFBQVEsQ0FBQ25YLENBQUMsQ0FBQyxFQUFFQSxDQUFDLElBQUk4RyxJQUFJLENBQUNDLEdBQUcsQ0FBQzJELE1BQU0sQ0FBQzFLLENBQUMsRUFBRW1YLFFBQVEsQ0FBQ25YLENBQUMsQ0FBQyxFQUFFQSxDQUFDLEVBQUUsRUFBRW9YLEtBQUssQ0FBQ3hTLElBQUksQ0FBQztRQUFFNUUsQ0FBQztRQUFFQyxDQUFDLEVBQUV5SyxNQUFNLENBQUN6SztNQUFFLENBQUMsQ0FBQztNQUNySCxLQUFLLElBQUlBLENBQUMsR0FBRzZHLElBQUksQ0FBQ0UsR0FBRyxDQUFDMEQsTUFBTSxDQUFDekssQ0FBQyxFQUFFa1gsUUFBUSxDQUFDbFgsQ0FBQyxDQUFDLEVBQUVBLENBQUMsSUFBSTZHLElBQUksQ0FBQ0MsR0FBRyxDQUFDMkQsTUFBTSxDQUFDekssQ0FBQyxFQUFFa1gsUUFBUSxDQUFDbFgsQ0FBQyxDQUFDLEVBQUVBLENBQUMsRUFBRSxFQUFFbVgsS0FBSyxDQUFDeFMsSUFBSSxDQUFDO1FBQUU1RSxDQUFDLEVBQUVtWCxRQUFRLENBQUNuWCxDQUFDO1FBQUVDO01BQUUsQ0FBQyxDQUFDO01BQ3ZILE9BQU9tWCxLQUFLO0lBQ2QsQ0FBQyxDQUFDO0lBQ0osS0FBSyxNQUFNclgsS0FBSyxJQUFJdVUsTUFBTSxFQUFFLElBQUl2VSxLQUFLLENBQUNDLENBQUMsS0FBS1ksS0FBSyxDQUFDd0YsSUFBSSxDQUFDcEcsQ0FBQyxJQUFJRCxLQUFLLENBQUNFLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxFQUFFMEssT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUUsT0FBTyxDQUFDO0VBQ2pJO0FBQ0YsQ0FBQztBQUVELE1BQU1nTCwyQkFBMkIsR0FBR0EsQ0FBQ3JLLEtBQVksRUFBRTRJLEtBQXVCLEtBQVc7RUFDbkYsSUFBSTVJLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxNQUFNLEVBQUU7RUFDNUIsTUFBTWtKLFFBQVEsR0FBRzlILEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzVHLElBQUksQ0FBQzJHLElBQUksSUFBSUEsSUFBSSxDQUFDckksSUFBSSxLQUFLLFVBQVUsQ0FBQztFQUNuRSxJQUFJLENBQUMyUixRQUFRLElBQUlBLFFBQVEsQ0FBQ3dCLFNBQVMsQ0FBQ3pKLEtBQUssR0FBRyxDQUFDLEVBQUUsTUFBTSxJQUFJbEIsS0FBSyxDQUFDLG9DQUFvQ3FCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDcEgsTUFBTTlJLENBQUMsR0FBR3FSLFFBQVEsQ0FBQ3dCLFNBQVMsQ0FBQzdTLENBQUMsR0FBRzZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQzBRLFFBQVEsQ0FBQ3dCLFNBQVMsQ0FBQ3hKLE1BQU0sR0FBRyxDQUFDLENBQUM7RUFDMUUsS0FBSyxJQUFJdEosQ0FBQyxHQUFHc1IsUUFBUSxDQUFDd0IsU0FBUyxDQUFDOVMsQ0FBQyxHQUFHLENBQUMsRUFBRUEsQ0FBQyxHQUFHc1IsUUFBUSxDQUFDd0IsU0FBUyxDQUFDOVMsQ0FBQyxHQUFHc1IsUUFBUSxDQUFDd0IsU0FBUyxDQUFDekosS0FBSyxHQUFHLENBQUMsRUFBRXJKLENBQUMsRUFBRSxFQUFFMkssT0FBTyxDQUFDL0osS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsRUFBRSxNQUFNLENBQUM7QUFDbkksQ0FBQztBQUVELE1BQU1vWCxvQkFBb0IsR0FBRyxDQUMzQjtFQUFFclgsQ0FBQyxFQUFFLENBQUM7RUFBRUMsQ0FBQyxFQUFFLENBQUMsQ0FBQztFQUFFcVgsS0FBSyxFQUFFO0lBQUV0WCxDQUFDLEVBQUUsQ0FBQztJQUFFQyxDQUFDLEVBQUU7RUFBRTtBQUFFLENBQUMsRUFDdEM7RUFBRUQsQ0FBQyxFQUFFLENBQUM7RUFBRUMsQ0FBQyxFQUFFLENBQUM7RUFBRXFYLEtBQUssRUFBRTtJQUFFdFgsQ0FBQyxFQUFFLENBQUM7SUFBRUMsQ0FBQyxFQUFFO0VBQUU7QUFBRSxDQUFDLEVBQ3JDO0VBQUVELENBQUMsRUFBRSxDQUFDO0VBQUVDLENBQUMsRUFBRSxDQUFDO0VBQUVxWCxLQUFLLEVBQUU7SUFBRXRYLENBQUMsRUFBRSxDQUFDO0lBQUVDLENBQUMsRUFBRTtFQUFFO0FBQUUsQ0FBQyxFQUNyQztFQUFFRCxDQUFDLEVBQUUsQ0FBQyxDQUFDO0VBQUVDLENBQUMsRUFBRSxDQUFDO0VBQUVxWCxLQUFLLEVBQUU7SUFBRXRYLENBQUMsRUFBRSxDQUFDO0lBQUVDLENBQUMsRUFBRTtFQUFFO0FBQUUsQ0FBQyxDQUM5QjtBQUVWLE1BQU1zWCxhQUFhLEdBQUdBLENBQUMzVyxLQUFZLEVBQUUrVCxRQUFlLEVBQUU2QyxTQUE4QyxFQUFFcEMsS0FBYSxFQUFFL0wsS0FBYSxLQUEwQjtFQUMxSixNQUFNb08sS0FBSyxHQUFHO0lBQUV6WCxDQUFDLEVBQUUyVSxRQUFRLENBQUMzVSxDQUFDLEdBQUd3WCxTQUFTLENBQUN4WCxDQUFDO0lBQUVDLENBQUMsRUFBRTBVLFFBQVEsQ0FBQzFVLENBQUMsR0FBR3VYLFNBQVMsQ0FBQ3ZYO0VBQUUsQ0FBQztFQUMxRSxNQUFNbVAsTUFBTSxHQUFHdEksSUFBSSxDQUFDbEcsS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUMsQ0FBQztFQUNwQyxNQUFNcU8sT0FBZ0IsR0FBRyxFQUFFO0VBQzNCLEtBQUssSUFBSUMsT0FBTyxHQUFHLENBQUMsRUFBRUEsT0FBTyxHQUFHdkMsS0FBSyxHQUFHLENBQUMsRUFBRXVDLE9BQU8sRUFBRSxFQUFFLEtBQUssSUFBSWxDLE9BQU8sR0FBRyxDQUFDckcsTUFBTSxFQUFFcUcsT0FBTyxJQUFJckcsTUFBTSxFQUFFcUcsT0FBTyxFQUFFLEVBQUU7SUFBQSxJQUFBbUMsU0FBQTtJQUM5RyxNQUFNN1gsS0FBSyxHQUFHO01BQUVDLENBQUMsRUFBRTJVLFFBQVEsQ0FBQzNVLENBQUMsR0FBR3dYLFNBQVMsQ0FBQ3hYLENBQUMsR0FBRzJYLE9BQU8sR0FBR0gsU0FBUyxDQUFDRixLQUFLLENBQUN0WCxDQUFDLEdBQUd5VixPQUFPO01BQUV4VixDQUFDLEVBQUUwVSxRQUFRLENBQUMxVSxDQUFDLEdBQUd1WCxTQUFTLENBQUN2WCxDQUFDLEdBQUcwWCxPQUFPLEdBQUdILFNBQVMsQ0FBQ0YsS0FBSyxDQUFDclgsQ0FBQyxHQUFHd1Y7SUFBUSxDQUFDO0lBQzFKLElBQUksQ0FBQ3pVLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsSUFBSSxFQUFBMlgsU0FBQSxHQUFBNVcsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBMlgsU0FBQSx1QkFBaENBLFNBQUEsQ0FBa0NqWSxJQUFJLE1BQUssTUFBTSxFQUFFLE9BQU91QixTQUFTO0lBQzVHd1csT0FBTyxDQUFDOVMsSUFBSSxDQUFDN0UsS0FBSyxDQUFDO0VBQ3JCO0VBQ0EsTUFBTThYLGNBQWMsR0FBRyxJQUFJL1YsR0FBRyxDQUFDNFYsT0FBTyxDQUFDblIsR0FBRyxDQUFDeEcsS0FBSyxJQUFJWSxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUN0RixLQUFLLE1BQU1GLEtBQUssSUFBSTJYLE9BQU8sRUFBRSxLQUFLLE1BQU0sQ0FBQzFYLENBQUMsRUFBRUMsQ0FBQyxDQUFDLElBQUlrRCxXQUFXLEVBQUU7SUFDN0QsTUFBTTJVLFFBQVEsR0FBRztNQUFFOVgsQ0FBQyxFQUFFRCxLQUFLLENBQUNDLENBQUMsR0FBR0EsQ0FBQztNQUFFQyxDQUFDLEVBQUVGLEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQTtJQUFFLENBQUM7SUFDbkQsSUFBSTZYLFFBQVEsQ0FBQzlYLENBQUMsS0FBS3lYLEtBQUssQ0FBQ3pYLENBQUMsSUFBSThYLFFBQVEsQ0FBQzdYLENBQUMsS0FBS3dYLEtBQUssQ0FBQ3hYLENBQUMsRUFBRTtJQUN0RCxNQUFNUCxJQUFJLEdBQUdzQixPQUFPLENBQUNKLEtBQUssRUFBRWtYLFFBQVEsQ0FBQzlYLENBQUMsRUFBRThYLFFBQVEsQ0FBQzdYLENBQUMsQ0FBQztJQUNuRCxJQUFJLENBQUNQLElBQUksSUFBSyxDQUFDbVksY0FBYyxDQUFDM1UsR0FBRyxDQUFDdkMsT0FBTyxDQUFDQyxLQUFLLEVBQUVrWCxRQUFRLENBQUM5WCxDQUFDLEVBQUU4WCxRQUFRLENBQUM3WCxDQUFDLENBQUMsQ0FBQyxJQUFJUCxJQUFJLENBQUNDLElBQUksS0FBSyxNQUFPLEVBQUUsT0FBT3VCLFNBQVM7RUFDdEg7RUFDQSxPQUFPd1csT0FBTztBQUNoQixDQUFDO0FBRUQsTUFBTXhMLHNCQUFzQixHQUFHQSxDQUFDdEwsS0FBWSxFQUFFb1EsR0FBUSxLQUFXO0VBQy9ELElBQUlwUSxLQUFLLENBQUN3SCxLQUFLLEtBQUssTUFBTSxFQUFFO0VBQzVCLE1BQU14QixTQUFTLEdBQUdoRyxLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDO0VBQ2pDLE1BQU1pWCxPQUFPLEdBQUduUixTQUFTLEdBQUcsQ0FBQztFQUM3QixNQUFNM0UsU0FBUyxHQUFHLENBQUMsR0FBR0MscUJBQXFCLENBQUN0QixLQUFLLENBQUMsQ0FBQyxDQUFDMkYsR0FBRyxDQUFDekYsS0FBSyxJQUFJRCxPQUFPLENBQUNELEtBQUssRUFBRUUsS0FBSyxDQUFDLENBQUM7RUFDdkYsTUFBTWtYLFVBQVUsR0FBR2hILEdBQUcsQ0FBQ3BDLE9BQU8sQ0FBQzNNLFNBQVMsQ0FBQytVLE9BQU8sQ0FBQ3JDLFFBQVEsSUFBSTBDLG9CQUFvQixDQUFDOVEsR0FBRyxDQUFDaVIsU0FBUyxLQUFLO0lBQUU3QyxRQUFRO0lBQUU2QztFQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUMvSCxNQUFNWCxRQUFRLEdBQUcsSUFBSS9VLEdBQUcsQ0FBUyxDQUFDO0VBQ2xDLE1BQU1tVyxVQUE0QixHQUFHLEVBQUU7RUFDdkMsS0FBSyxNQUFNMVYsU0FBUyxJQUFJeVYsVUFBVSxFQUFFO0lBQUEsSUFBQUUsU0FBQTtJQUNsQyxJQUFJRCxVQUFVLENBQUN4VCxNQUFNLEtBQUtzVCxPQUFPLEVBQUU7SUFDbkMsTUFBTU4sS0FBSyxHQUFHO01BQUV6WCxDQUFDLEVBQUV1QyxTQUFTLENBQUNvUyxRQUFRLENBQUMzVSxDQUFDLEdBQUd1QyxTQUFTLENBQUNpVixTQUFTLENBQUN4WCxDQUFDO01BQUVDLENBQUMsRUFBRXNDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzFVLENBQUMsR0FBR3NDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3ZYO0lBQUUsQ0FBQztJQUNsSCxJQUFJLEVBQUFpWSxTQUFBLEdBQUFsWCxPQUFPLENBQUNKLEtBQUssRUFBRTZXLEtBQUssQ0FBQ3pYLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsQ0FBQyxjQUFBaVksU0FBQSx1QkFBaENBLFNBQUEsQ0FBa0N2WSxJQUFJLE1BQUssTUFBTSxJQUFJa1gsUUFBUSxDQUFDM1QsR0FBRyxDQUFDdkMsT0FBTyxDQUFDQyxLQUFLLEVBQUU2VyxLQUFLLENBQUN6WCxDQUFDLEVBQUV5WCxLQUFLLENBQUN4WCxDQUFDLENBQUMsQ0FBQyxFQUFFO0lBQ3pHLE1BQU15WCxPQUFPLEdBQUdILGFBQWEsQ0FBQzNXLEtBQUssRUFBRTJCLFNBQVMsQ0FBQ29TLFFBQVEsRUFBRXBTLFNBQVMsQ0FBQ2lWLFNBQVMsRUFBRSxDQUFDLEdBQUcxUSxJQUFJLENBQUNsRyxLQUFLLENBQUMsQ0FBQ2dHLFNBQVMsR0FBR3FSLFVBQVUsQ0FBQ3hULE1BQU0sSUFBSSxDQUFDLENBQUMsRUFBRSxDQUFDLElBQUltQyxTQUFTLEdBQUcsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQztJQUMvSixJQUFJLENBQUM4USxPQUFPLElBQUlBLE9BQU8sQ0FBQzNQLElBQUksQ0FBQ2hJLEtBQUssSUFBSThXLFFBQVEsQ0FBQzNULEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUU7SUFDdkYsTUFBTWtZLFdBQVcsR0FBR1QsT0FBTyxDQUFDNVEsSUFBSSxDQUFDbEcsS0FBSyxDQUFDOFcsT0FBTyxDQUFDalQsTUFBTSxHQUFHLENBQUMsQ0FBQyxDQUFFO0lBQzVELE1BQU1pTixNQUFNLEdBQUc7TUFBRS9PLEVBQUUsRUFBRXNWLFVBQVUsQ0FBQ3hULE1BQU0sR0FBRyxDQUFDLEdBQUcsWUFBWSxHQUFHLFVBQVU7TUFBRXpFLENBQUMsRUFBRW1ZLFdBQVcsQ0FBQ25ZLENBQUM7TUFBRUMsQ0FBQyxFQUFFa1ksV0FBVyxDQUFDbFksQ0FBQztNQUFFbVksS0FBSyxFQUFFLENBQUM7TUFBRUMsWUFBWSxFQUFFO0lBQUssQ0FBQztJQUMxSSxNQUFNQyxVQUFVLEdBQUdMLFVBQVUsQ0FBQ3hULE1BQU0sS0FBS3NULE9BQU8sR0FBRyxDQUFDLElBQUluUixTQUFTLEtBQUssQ0FBQyxHQUFHO01BQUVqSCxJQUFJLEVBQUUsV0FBb0I7TUFBRTRZLFdBQVcsRUFBRTNYLEtBQUssQ0FBQ3dILEtBQUs7TUFBRW9RLFdBQVcsRUFBRTtJQUFFLENBQUMsR0FBR3RYLFNBQVM7SUFDOUpGLE9BQU8sQ0FBQ0osS0FBSyxFQUFFNlcsS0FBSyxDQUFDelgsQ0FBQyxFQUFFeVgsS0FBSyxDQUFDeFgsQ0FBQyxDQUFDLENBQUVOLElBQUksR0FBRyxXQUFXO0lBQ3BEK1gsT0FBTyxDQUFDNUcsT0FBTyxDQUFDL1EsS0FBSyxJQUFJO01BQUVpQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUVOLElBQUksR0FBRyxPQUFPO01BQUVrWCxRQUFRLENBQUN6UixHQUFHLENBQUN6RSxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUM7SUFBQyxDQUFDLENBQUM7SUFDOUg0VyxRQUFRLENBQUN6UixHQUFHLENBQUN6RSxPQUFPLENBQUNDLEtBQUssRUFBRTZXLEtBQUssQ0FBQ3pYLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsQ0FBQyxDQUFDO0lBQzlDVyxLQUFLLENBQUNtSixLQUFLLENBQUNuRixJQUFJLENBQUM4TSxNQUFNLENBQUM7SUFDeEJ1RyxVQUFVLENBQUNyVCxJQUFJLENBQUM7TUFBRWpDLEVBQUUsRUFBRSxlQUFlL0IsS0FBSyxDQUFDK0gsSUFBSSxJQUFJc1AsVUFBVSxDQUFDeFQsTUFBTSxFQUFFO01BQUU5RSxJQUFJLEVBQUUsa0JBQWtCO01BQUVnVixRQUFRLEVBQUU7UUFBRSxHQUFHcFMsU0FBUyxDQUFDb1M7TUFBUyxDQUFDO01BQUU4QyxLQUFLO01BQUVDLE9BQU87TUFBRWhHLE1BQU07TUFBRSxJQUFJNEcsVUFBVSxHQUFHO1FBQUVHLGNBQWMsRUFBRUg7TUFBVyxDQUFDLEdBQUcsQ0FBQyxDQUFDO0lBQUUsQ0FBQyxDQUFDO0VBQ3pOO0VBQ0EsSUFBSUwsVUFBVSxDQUFDeFQsTUFBTSxLQUFLc1QsT0FBTyxFQUFFLE1BQU0sSUFBSTVQLEtBQUssQ0FBQyxnREFBZ0Q0UCxPQUFPLFdBQVdFLFVBQVUsQ0FBQ3hULE1BQU0sRUFBRSxDQUFDO0VBQ3pJN0QsS0FBSyxDQUFDcVgsVUFBVSxHQUFHQSxVQUFVO0FBQy9CLENBQUM7QUFFRCxNQUFNOUwsaUJBQWlCLEdBQUdBLENBQUN2TCxLQUFZLEVBQUVvUSxHQUFRLEtBQVc7RUFDMUQsSUFBSXBRLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxPQUFPLEVBQUU7RUFDN0IsTUFBTTJQLE9BQU8sR0FBRyxDQUFDLEdBQUdqUixJQUFJLENBQUNsRyxLQUFLLENBQUVBLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUMsR0FBSSxDQUFDLENBQUM7RUFDckQsTUFBTW1CLFNBQVMsR0FBRyxDQUFDLEdBQUdDLHFCQUFxQixDQUFDdEIsS0FBSyxDQUFDLENBQUMsQ0FBQzJGLEdBQUcsQ0FBQ3pGLEtBQUssSUFBSUQsT0FBTyxDQUFDRCxLQUFLLEVBQUVFLEtBQUssQ0FBQyxDQUFDO0VBQ3ZGLE1BQU1rWCxVQUFVLEdBQUdoSCxHQUFHLENBQUNwQyxPQUFPLENBQUMzTSxTQUFTLENBQUMrVSxPQUFPLENBQUNyQyxRQUFRLElBQUkwQyxvQkFBb0IsQ0FBQzlRLEdBQUcsQ0FBQ2lSLFNBQVMsS0FBSztJQUFFN0MsUUFBUTtJQUFFNkM7RUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDL0gsTUFBTWtCLEtBQWtCLEdBQUcsRUFBRTtFQUM3QixNQUFNN0IsUUFBUSxHQUFHLElBQUkvVSxHQUFHLENBQVMsQ0FBQztFQUNsQyxLQUFLLE1BQU1TLFNBQVMsSUFBSXlWLFVBQVUsRUFBRTtJQUFBLElBQUFXLFNBQUE7SUFDbEMsSUFBSUQsS0FBSyxDQUFDalUsTUFBTSxLQUFLc1QsT0FBTyxFQUFFO0lBQzlCLE1BQU1OLEtBQUssR0FBRztNQUFFelgsQ0FBQyxFQUFFdUMsU0FBUyxDQUFDb1MsUUFBUSxDQUFDM1UsQ0FBQyxHQUFHdUMsU0FBUyxDQUFDaVYsU0FBUyxDQUFDeFgsQ0FBQztNQUFFQyxDQUFDLEVBQUVzQyxTQUFTLENBQUNvUyxRQUFRLENBQUMxVSxDQUFDLEdBQUdzQyxTQUFTLENBQUNpVixTQUFTLENBQUN2WDtJQUFFLENBQUM7SUFDbEgsSUFBSSxFQUFBMFksU0FBQSxHQUFBM1gsT0FBTyxDQUFDSixLQUFLLEVBQUU2VyxLQUFLLENBQUN6WCxDQUFDLEVBQUV5WCxLQUFLLENBQUN4WCxDQUFDLENBQUMsY0FBQTBZLFNBQUEsdUJBQWhDQSxTQUFBLENBQWtDaFosSUFBSSxNQUFLLE1BQU0sSUFBSWtYLFFBQVEsQ0FBQzNULEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFNlcsS0FBSyxDQUFDelgsQ0FBQyxFQUFFeVgsS0FBSyxDQUFDeFgsQ0FBQyxDQUFDLENBQUMsRUFBRTtJQUN6RyxNQUFNeVgsT0FBTyxHQUFHSCxhQUFhLENBQUMzVyxLQUFLLEVBQUUyQixTQUFTLENBQUNvUyxRQUFRLEVBQUVwUyxTQUFTLENBQUNpVixTQUFTLEVBQUUsQ0FBQyxFQUFFLENBQUMsQ0FBQztJQUNuRixJQUFJLENBQUNFLE9BQU8sSUFBSUEsT0FBTyxDQUFDM1AsSUFBSSxDQUFDaEksS0FBSyxJQUFJOFcsUUFBUSxDQUFDM1QsR0FBRyxDQUFDdkMsT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRTtJQUN2RixNQUFNa1ksV0FBVyxHQUFHVCxPQUFPLENBQUMsQ0FBQyxDQUFFO0lBQy9CLE1BQU1oRyxNQUFNLEdBQUc7TUFBRS9PLEVBQUUsRUFBRStWLEtBQUssQ0FBQ2pVLE1BQU0sR0FBRyxDQUFDLEdBQUcsWUFBWSxHQUFHLFlBQVk7TUFBRXpFLENBQUMsRUFBRW1ZLFdBQVcsQ0FBQ25ZLENBQUM7TUFBRUMsQ0FBQyxFQUFFa1ksV0FBVyxDQUFDbFksQ0FBQztNQUFFbVksS0FBSyxFQUFFLENBQUM7TUFBRUMsWUFBWSxFQUFFO0lBQUssQ0FBQztJQUN2SXJYLE9BQU8sQ0FBQ0osS0FBSyxFQUFFNlcsS0FBSyxDQUFDelgsQ0FBQyxFQUFFeVgsS0FBSyxDQUFDeFgsQ0FBQyxDQUFDLENBQUVOLElBQUksR0FBRyxXQUFXO0lBQ3BEa1gsUUFBUSxDQUFDelIsR0FBRyxDQUFDekUsT0FBTyxDQUFDQyxLQUFLLEVBQUU2VyxLQUFLLENBQUN6WCxDQUFDLEVBQUV5WCxLQUFLLENBQUN4WCxDQUFDLENBQUMsQ0FBQztJQUM5Q3lYLE9BQU8sQ0FBQzVHLE9BQU8sQ0FBQy9RLEtBQUssSUFBSTtNQUFFaUIsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFFTixJQUFJLEdBQUcsT0FBTztNQUFFa1gsUUFBUSxDQUFDelIsR0FBRyxDQUFDekUsT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDO0lBQUMsQ0FBQyxDQUFDO0lBQzlIVyxLQUFLLENBQUNtSixLQUFLLENBQUNuRixJQUFJLENBQUM4TSxNQUFNLENBQUM7SUFDeEJnSCxLQUFLLENBQUM5VCxJQUFJLENBQUM7TUFBRWpDLEVBQUUsRUFBRSxjQUFjL0IsS0FBSyxDQUFDK0gsSUFBSSxJQUFJK1AsS0FBSyxDQUFDalUsTUFBTSxFQUFFO01BQUU5RSxJQUFJLEVBQUUsWUFBWTtNQUFFZ1YsUUFBUSxFQUFFO1FBQUUsR0FBR3BTLFNBQVMsQ0FBQ29TO01BQVMsQ0FBQztNQUFFOEMsS0FBSztNQUFFQyxPQUFPO01BQUVoRztJQUFPLENBQUMsQ0FBQztFQUNqSjtFQUNBLElBQUlnSCxLQUFLLENBQUNqVSxNQUFNLEtBQUtzVCxPQUFPLEVBQUUsTUFBTSxJQUFJNVAsS0FBSyxDQUFDLDBDQUEwQzRQLE9BQU8sV0FBV1csS0FBSyxDQUFDalUsTUFBTSxFQUFFLENBQUM7RUFDekg3RCxLQUFLLENBQUNxWCxVQUFVLEdBQUdTLEtBQUs7QUFDMUIsQ0FBQztBQUVELE1BQU10TSwyQkFBMkIsR0FBR0EsQ0FBQ3hMLEtBQVksRUFBRW9RLEdBQVEsS0FBVztFQUNwRSxJQUFJcFEsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFNBQVMsRUFBRTtFQUMvQixNQUFNMlAsT0FBTyxHQUFHLENBQUMsR0FBR2pSLElBQUksQ0FBQ2xHLEtBQUssQ0FBRUEsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxHQUFJLENBQUMsQ0FBQztFQUNyRCxNQUFNbUIsU0FBUyxHQUFHLENBQUMsR0FBR0MscUJBQXFCLENBQUN0QixLQUFLLENBQUMsQ0FBQyxDQUFDMkYsR0FBRyxDQUFDekYsS0FBSyxJQUFJRCxPQUFPLENBQUNELEtBQUssRUFBRUUsS0FBSyxDQUFDLENBQUM7RUFDdkYsTUFBTWtYLFVBQVUsR0FBR2hILEdBQUcsQ0FBQ3BDLE9BQU8sQ0FBQzNNLFNBQVMsQ0FBQytVLE9BQU8sQ0FBQ3JDLFFBQVEsSUFBSTBDLG9CQUFvQixDQUFDOVEsR0FBRyxDQUFDaVIsU0FBUyxLQUFLO0lBQUU3QyxRQUFRO0lBQUU2QztFQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUMvSCxNQUFNb0IsUUFBK0IsR0FBRyxFQUFFO0VBQzFDLE1BQU0vQixRQUFRLEdBQUcsSUFBSS9VLEdBQUcsQ0FBUyxDQUFDO0VBQ2xDLEtBQUssTUFBTVMsU0FBUyxJQUFJeVYsVUFBVSxFQUFFO0lBQUEsSUFBQWEsU0FBQSxFQUFBQyxTQUFBO0lBQ2xDLElBQUlGLFFBQVEsQ0FBQ25VLE1BQU0sS0FBS3NULE9BQU8sRUFBRTtJQUNqQyxNQUFNTixLQUFLLEdBQUc7TUFBRXpYLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzNVLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3hYLENBQUM7TUFBRUMsQ0FBQyxFQUFFc0MsU0FBUyxDQUFDb1MsUUFBUSxDQUFDMVUsQ0FBQyxHQUFHc0MsU0FBUyxDQUFDaVYsU0FBUyxDQUFDdlg7SUFBRSxDQUFDO0lBQ2xILElBQUksRUFBQTRZLFNBQUEsR0FBQTdYLE9BQU8sQ0FBQ0osS0FBSyxFQUFFMkIsU0FBUyxDQUFDb1MsUUFBUSxDQUFDM1UsQ0FBQyxFQUFFdUMsU0FBUyxDQUFDb1MsUUFBUSxDQUFDMVUsQ0FBQyxDQUFDLGNBQUE0WSxTQUFBLHVCQUExREEsU0FBQSxDQUE0RGxaLElBQUksTUFBSyxPQUFPLElBQUksRUFBQW1aLFNBQUEsR0FBQTlYLE9BQU8sQ0FBQ0osS0FBSyxFQUFFNlcsS0FBSyxDQUFDelgsQ0FBQyxFQUFFeVgsS0FBSyxDQUFDeFgsQ0FBQyxDQUFDLGNBQUE2WSxTQUFBLHVCQUFoQ0EsU0FBQSxDQUFrQ25aLElBQUksTUFBSyxNQUFNLElBQUlrWCxRQUFRLENBQUMzVCxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRTZXLEtBQUssQ0FBQ3pYLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsQ0FBQyxDQUFDLEVBQUU7SUFDekwsTUFBTXlYLE9BQU8sR0FBR0gsYUFBYSxDQUFDM1csS0FBSyxFQUFFMkIsU0FBUyxDQUFDb1MsUUFBUSxFQUFFcFMsU0FBUyxDQUFDaVYsU0FBUyxFQUFFLENBQUMsRUFBRSxDQUFDLENBQUM7SUFDbkYsSUFBSSxDQUFDRSxPQUFPLElBQUlBLE9BQU8sQ0FBQzNQLElBQUksQ0FBQ2hJLEtBQUssSUFBSThXLFFBQVEsQ0FBQzNULEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUU7SUFDdkYsTUFBTWtZLFdBQVcsR0FBR1QsT0FBTyxDQUFDNVEsSUFBSSxDQUFDbEcsS0FBSyxDQUFDOFcsT0FBTyxDQUFDalQsTUFBTSxHQUFHLENBQUMsQ0FBQyxDQUFFO0lBQzVELE1BQU1pTixNQUFNLEdBQUc7TUFBRS9PLEVBQUUsRUFBRWlXLFFBQVEsQ0FBQ25VLE1BQU0sR0FBRyxDQUFDLEdBQUcsYUFBYSxHQUFHLFlBQVk7TUFBRXpFLENBQUMsRUFBRW1ZLFdBQVcsQ0FBQ25ZLENBQUM7TUFBRUMsQ0FBQyxFQUFFa1ksV0FBVyxDQUFDbFksQ0FBQztNQUFFbVksS0FBSyxFQUFFLENBQUM7TUFBRUMsWUFBWSxFQUFFO0lBQUssQ0FBQztJQUMzSSxNQUFNVSxTQUFTLEdBQUc7TUFBRSxHQUFHeFcsU0FBUyxDQUFDb1M7SUFBUyxDQUFDO0lBQzNDLE1BQU1xRSxRQUFRLEdBQUdoWSxPQUFPLENBQUNKLEtBQUssRUFBRW1ZLFNBQVMsQ0FBQy9ZLENBQUMsRUFBRStZLFNBQVMsQ0FBQzlZLENBQUMsQ0FBRTtJQUMxRCtZLFFBQVEsQ0FBQ3JaLElBQUksR0FBRyxTQUFTO0lBQ3pCcVosUUFBUSxDQUFDQyxJQUFJLEdBQUc7TUFBRXpCLFNBQVMsRUFBRTBCLGFBQWEsQ0FBQ3pCLEtBQUssQ0FBQ3pYLENBQUMsR0FBRytZLFNBQVMsQ0FBQy9ZLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsR0FBRzhZLFNBQVMsQ0FBQzlZLENBQUM7SUFBRSxDQUFDO0lBQzFGZSxPQUFPLENBQUNKLEtBQUssRUFBRTZXLEtBQUssQ0FBQ3pYLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsQ0FBQyxDQUFFTixJQUFJLEdBQUcsV0FBVztJQUNwRGtYLFFBQVEsQ0FBQ3pSLEdBQUcsQ0FBQ3pFLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFNlcsS0FBSyxDQUFDelgsQ0FBQyxFQUFFeVgsS0FBSyxDQUFDeFgsQ0FBQyxDQUFDLENBQUM7SUFDOUN5WCxPQUFPLENBQUM1RyxPQUFPLENBQUMvUSxLQUFLLElBQUk7TUFBRWlCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBRU4sSUFBSSxHQUFHLE9BQU87TUFBRWtYLFFBQVEsQ0FBQ3pSLEdBQUcsQ0FBQ3pFLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQztJQUFDLENBQUMsQ0FBQztJQUM5SFcsS0FBSyxDQUFDbUosS0FBSyxDQUFDbkYsSUFBSSxDQUFDOE0sTUFBTSxDQUFDO0lBQ3hCa0gsUUFBUSxDQUFDaFUsSUFBSSxDQUFDO01BQUVqQyxFQUFFLEVBQUUsaUJBQWlCL0IsS0FBSyxDQUFDK0gsSUFBSSxJQUFJaVEsUUFBUSxDQUFDblUsTUFBTSxFQUFFO01BQUU5RSxJQUFJLEVBQUUsdUJBQXVCO01BQUVnVixRQUFRLEVBQUU7UUFBRSxHQUFHcFMsU0FBUyxDQUFDb1M7TUFBUyxDQUFDO01BQUU4QyxLQUFLO01BQUVDLE9BQU87TUFBRWhHLE1BQU07TUFBRXFIO0lBQVUsQ0FBQyxDQUFDO0VBQ2hMO0VBQ0EsSUFBSUgsUUFBUSxDQUFDblUsTUFBTSxLQUFLc1QsT0FBTyxFQUFFLE1BQU0sSUFBSTVQLEtBQUssQ0FBQyxxREFBcUQ0UCxPQUFPLFdBQVdhLFFBQVEsQ0FBQ25VLE1BQU0sRUFBRSxDQUFDO0VBQzFJN0QsS0FBSyxDQUFDcVgsVUFBVSxHQUFHVyxRQUFRO0FBQzdCLENBQUM7QUFFRCxNQUFNdk0sMEJBQTBCLEdBQUdBLENBQUN6TCxLQUFZLEVBQUU0SSxLQUF1QixFQUFFd0gsR0FBUSxLQUFXO0VBQzVGLElBQUlwUSxLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxFQUFFO0VBQzdCLE1BQU0yUCxPQUFPLEdBQUcsQ0FBQyxHQUFHalIsSUFBSSxDQUFDbEcsS0FBSyxDQUFFQSxLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLEdBQUksQ0FBQyxDQUFDO0VBQ3JELE1BQU1tQixTQUFTLEdBQUcsQ0FBQyxHQUFHQyxxQkFBcUIsQ0FBQ3RCLEtBQUssQ0FBQyxDQUFDLENBQUMyRixHQUFHLENBQUN6RixLQUFLLElBQUlELE9BQU8sQ0FBQ0QsS0FBSyxFQUFFRSxLQUFLLENBQUMsQ0FBQztFQUN2RixNQUFNa1gsVUFBVSxHQUFHaEgsR0FBRyxDQUFDcEMsT0FBTyxDQUFDM00sU0FBUyxDQUFDK1UsT0FBTyxDQUFDckMsUUFBUSxJQUFJMEMsb0JBQW9CLENBQUM5USxHQUFHLENBQUNpUixTQUFTLEtBQUs7SUFBRTdDLFFBQVE7SUFBRTZDO0VBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDO0VBQy9ILE1BQU1vQixRQUErQixHQUFHLEVBQUU7RUFDMUMsTUFBTS9CLFFBQVEsR0FBRyxJQUFJL1UsR0FBRyxDQUFTLENBQUM7RUFDbEMsTUFBTXFYLFVBQVUsR0FBRyxJQUFJclgsR0FBRyxDQUFDbkQsb0JBQW9CLENBQUM2SyxLQUFLLENBQUMsQ0FBQ2pELEdBQUcsQ0FBQ3hHLEtBQUssSUFBSVksT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDdEcsS0FBSyxNQUFNc0MsU0FBUyxJQUFJeVYsVUFBVSxFQUFFO0lBQ2xDLElBQUlZLFFBQVEsQ0FBQ25VLE1BQU0sS0FBS3NULE9BQU8sRUFBRTtJQUNqQyxNQUFNTixLQUFLLEdBQUc7TUFBRXpYLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzNVLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3hYLENBQUM7TUFBRUMsQ0FBQyxFQUFFc0MsU0FBUyxDQUFDb1MsUUFBUSxDQUFDMVUsQ0FBQyxHQUFHc0MsU0FBUyxDQUFDaVYsU0FBUyxDQUFDdlg7SUFBRSxDQUFDO0lBQ2xILE1BQU15WCxPQUFPLEdBQUcsRUFBYTtJQUM3QixLQUFLLElBQUlDLE9BQU8sR0FBRyxDQUFDLEVBQUVBLE9BQU8sR0FBRyxDQUFDLEVBQUVBLE9BQU8sRUFBRSxFQUFFLEtBQUssSUFBSWxDLE9BQU8sR0FBRyxDQUFDLENBQUMsRUFBRUEsT0FBTyxJQUFJLENBQUMsRUFBRUEsT0FBTyxFQUFFLEVBQUVpQyxPQUFPLENBQUM5UyxJQUFJLENBQUM7TUFBRTVFLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzNVLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3hYLENBQUMsR0FBRzJYLE9BQU8sR0FBR3BWLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ0YsS0FBSyxDQUFDdFgsQ0FBQyxHQUFHeVYsT0FBTztNQUFFeFYsQ0FBQyxFQUFFc0MsU0FBUyxDQUFDb1MsUUFBUSxDQUFDMVUsQ0FBQyxHQUFHc0MsU0FBUyxDQUFDaVYsU0FBUyxDQUFDdlgsQ0FBQyxHQUFHMFgsT0FBTyxHQUFHcFYsU0FBUyxDQUFDaVYsU0FBUyxDQUFDRixLQUFLLENBQUNyWCxDQUFDLEdBQUd3VjtJQUFRLENBQUMsQ0FBQztJQUNwVCxNQUFNb0MsY0FBYyxHQUFHLElBQUkvVixHQUFHLENBQUM0VixPQUFPLENBQUNuUixHQUFHLENBQUN4RyxLQUFLLElBQUlZLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxDQUFDO0lBQ3RGLE1BQU1tWixPQUFPLEdBQUcxQixPQUFPLENBQUNWLE9BQU8sQ0FBQ2pYLEtBQUssSUFBSW9ELFdBQVcsQ0FBQ29ELEdBQUcsQ0FBQyxDQUFDLENBQUN2RyxDQUFDLEVBQUVDLENBQUMsQ0FBQyxNQUFNO01BQUVELENBQUMsRUFBRUQsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUM7TUFBRUMsQ0FBQyxFQUFFRixLQUFLLENBQUNFLENBQUMsR0FBR0E7SUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUN1RyxNQUFNLENBQUN6RyxLQUFLLElBQUksQ0FBQzhYLGNBQWMsQ0FBQzNVLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxLQUFLRixLQUFLLENBQUNDLENBQUMsS0FBS3lYLEtBQUssQ0FBQ3pYLENBQUMsSUFBSUQsS0FBSyxDQUFDRSxDQUFDLEtBQUt3WCxLQUFLLENBQUN4WCxDQUFDLENBQUMsQ0FBQyxDQUFDdUcsTUFBTSxDQUFDLENBQUN6RyxLQUFLLEVBQUVlLEtBQUssRUFBRXdULE1BQU0sS0FBS0EsTUFBTSxDQUFDK0UsU0FBUyxDQUFDQyxLQUFLLElBQUlBLEtBQUssQ0FBQ3RaLENBQUMsS0FBS0QsS0FBSyxDQUFDQyxDQUFDLElBQUlzWixLQUFLLENBQUNyWixDQUFDLEtBQUtGLEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLEtBQUthLEtBQUssQ0FBQztJQUNwVixNQUFNeVksT0FBTyxHQUFHLENBQUM5QixLQUFLLEVBQUUsR0FBR0MsT0FBTyxFQUFFLEdBQUcwQixPQUFPLENBQUM7SUFDL0MsSUFBSUcsT0FBTyxDQUFDeFIsSUFBSSxDQUFDaEksS0FBSztNQUFBLElBQUF5WixTQUFBO01BQUEsT0FBSSxDQUFDeFksT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxJQUFJLEVBQUF1WixTQUFBLEdBQUF4WSxPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUF1WixTQUFBLHVCQUFoQ0EsU0FBQSxDQUFrQzdaLElBQUksTUFBSyxPQUFPLElBQUlrWCxRQUFRLENBQUMzVCxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsSUFBSWtaLFVBQVUsQ0FBQ2pXLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQztJQUFBLEVBQUMsRUFBRTtJQUMxTixNQUFNd1osTUFBTSxHQUFHRixPQUFPLENBQUNoVCxHQUFHLENBQUN4RyxLQUFLLEtBQUs7TUFBRUEsS0FBSztNQUFFSixJQUFJLEVBQUVxQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUVOO0lBQUssQ0FBQyxDQUFDLENBQUM7SUFDOUZ5WixPQUFPLENBQUN0SSxPQUFPLENBQUMvUSxLQUFLLElBQUk0SyxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRSxNQUFNLENBQUMsQ0FBQztJQUNsRTBLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRTZXLEtBQUssQ0FBQ3pYLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsRUFBRSxXQUFXLENBQUM7SUFDN0MsSUFBSSxDQUFDc0YsZUFBZSxDQUFDM0UsS0FBSyxFQUFFQSxLQUFLLENBQUMrQyxLQUFLLEVBQUUvQyxLQUFLLENBQUN3RixJQUFJLENBQUMsRUFBRTtNQUNwRHFULE1BQU0sQ0FBQzNJLE9BQU8sQ0FBQyxDQUFDO1FBQUUvUSxLQUFLO1FBQUVKO01BQUssQ0FBQyxLQUFLZ0wsT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUVOLElBQUksQ0FBQyxDQUFDO01BQzNFO0lBQ0Y7SUFDQSxNQUFNd1ksV0FBVyxHQUFHVCxPQUFPLENBQUM1USxJQUFJLENBQUNsRyxLQUFLLENBQUM4VyxPQUFPLENBQUNqVCxNQUFNLEdBQUcsQ0FBQyxDQUFDLENBQUU7SUFDNUQsTUFBTWlOLE1BQU0sR0FBRztNQUFFL08sRUFBRSxFQUFFaVcsUUFBUSxDQUFDblUsTUFBTSxHQUFHLENBQUMsR0FBRyxTQUFTLEdBQUcsWUFBWTtNQUFFekUsQ0FBQyxFQUFFbVksV0FBVyxDQUFDblksQ0FBQztNQUFFQyxDQUFDLEVBQUVrWSxXQUFXLENBQUNsWSxDQUFDO01BQUVtWSxLQUFLLEVBQUUsQ0FBQztNQUFFQyxZQUFZLEVBQUU7SUFBSyxDQUFDO0lBQ3ZJa0IsT0FBTyxDQUFDekksT0FBTyxDQUFDL1EsS0FBSyxJQUFJOFcsUUFBUSxDQUFDelIsR0FBRyxDQUFDekUsT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUM7SUFDeEVXLEtBQUssQ0FBQ21KLEtBQUssQ0FBQ25GLElBQUksQ0FBQzhNLE1BQU0sQ0FBQztJQUN4QmtILFFBQVEsQ0FBQ2hVLElBQUksQ0FBQztNQUFFakMsRUFBRSxFQUFFLGlCQUFpQi9CLEtBQUssQ0FBQytILElBQUksSUFBSWlRLFFBQVEsQ0FBQ25VLE1BQU0sRUFBRTtNQUFFOUUsSUFBSSxFQUFFLHVCQUF1QjtNQUFFZ1YsUUFBUSxFQUFFO1FBQUUsR0FBR3BTLFNBQVMsQ0FBQ29TO01BQVMsQ0FBQztNQUFFOEMsS0FBSztNQUFFQyxPQUFPO01BQUVoRztJQUFPLENBQUMsQ0FBQztFQUNySztFQUNBLElBQUlrSCxRQUFRLENBQUNuVSxNQUFNLEtBQUtzVCxPQUFPLEVBQUUsTUFBTSxJQUFJNVAsS0FBSyxDQUFDLHFEQUFxRDRQLE9BQU8sV0FBV2EsUUFBUSxDQUFDblUsTUFBTSxFQUFFLENBQUM7RUFDMUk3RCxLQUFLLENBQUNxWCxVQUFVLEdBQUdXLFFBQVE7QUFDN0IsQ0FBQztBQUVELE1BQU10TSwyQkFBMkIsR0FBR0EsQ0FBQzFMLEtBQVksRUFBRTRJLEtBQXVCLEVBQUV3SCxHQUFRLEtBQVc7RUFDN0YsSUFBSXBRLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxTQUFTLEVBQUU7RUFDL0IsTUFBTTJQLE9BQU8sR0FBRyxDQUFDLEdBQUdqUixJQUFJLENBQUNsRyxLQUFLLENBQUVBLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUMsR0FBSSxDQUFDLENBQUM7RUFDckQsTUFBTW1CLFNBQVMsR0FBRyxDQUFDLEdBQUdDLHFCQUFxQixDQUFDdEIsS0FBSyxDQUFDLENBQUMsQ0FBQzJGLEdBQUcsQ0FBQ3pGLEtBQUssSUFBSUQsT0FBTyxDQUFDRCxLQUFLLEVBQUVFLEtBQUssQ0FBQyxDQUFDO0VBQ3ZGLE1BQU1rWCxVQUFVLEdBQUdoSCxHQUFHLENBQUNwQyxPQUFPLENBQUMzTSxTQUFTLENBQUMrVSxPQUFPLENBQUNyQyxRQUFRLElBQUkwQyxvQkFBb0IsQ0FBQzlRLEdBQUcsQ0FBQ2lSLFNBQVMsS0FBSztJQUFFN0MsUUFBUTtJQUFFNkM7RUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDL0gsTUFBTWtDLE1BQTZCLEdBQUcsRUFBRTtFQUN4QyxNQUFNN0MsUUFBUSxHQUFHLElBQUkvVSxHQUFHLENBQVMsQ0FBQztFQUNsQyxNQUFNcVgsVUFBVSxHQUFHLElBQUlyWCxHQUFHLENBQUNuRCxvQkFBb0IsQ0FBQzZLLEtBQUssQ0FBQyxDQUFDakQsR0FBRyxDQUFDeEcsS0FBSyxJQUFJWSxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUN0RyxLQUFLLE1BQU1zQyxTQUFTLElBQUl5VixVQUFVLEVBQUU7SUFDbEMsSUFBSTBCLE1BQU0sQ0FBQ2pWLE1BQU0sS0FBS3NULE9BQU8sRUFBRTtJQUMvQixNQUFNTixLQUFLLEdBQUc7TUFBRXpYLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzNVLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3hYLENBQUM7TUFBRUMsQ0FBQyxFQUFFc0MsU0FBUyxDQUFDb1MsUUFBUSxDQUFDMVUsQ0FBQyxHQUFHc0MsU0FBUyxDQUFDaVYsU0FBUyxDQUFDdlg7SUFBRSxDQUFDO0lBQ2xILE1BQU15WCxPQUFPLEdBQUcsRUFBYTtJQUM3QixLQUFLLElBQUlDLE9BQU8sR0FBRyxDQUFDLEVBQUVBLE9BQU8sR0FBRyxDQUFDLEVBQUVBLE9BQU8sRUFBRSxFQUFFLEtBQUssSUFBSWxDLE9BQU8sR0FBRyxDQUFDLENBQUMsRUFBRUEsT0FBTyxJQUFJLENBQUMsRUFBRUEsT0FBTyxFQUFFLEVBQUVpQyxPQUFPLENBQUM5UyxJQUFJLENBQUM7TUFBRTVFLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzNVLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3hYLENBQUMsR0FBRzJYLE9BQU8sR0FBR3BWLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ0YsS0FBSyxDQUFDdFgsQ0FBQyxHQUFHeVYsT0FBTztNQUFFeFYsQ0FBQyxFQUFFc0MsU0FBUyxDQUFDb1MsUUFBUSxDQUFDMVUsQ0FBQyxHQUFHc0MsU0FBUyxDQUFDaVYsU0FBUyxDQUFDdlgsQ0FBQyxHQUFHMFgsT0FBTyxHQUFHcFYsU0FBUyxDQUFDaVYsU0FBUyxDQUFDRixLQUFLLENBQUNyWCxDQUFDLEdBQUd3VjtJQUFRLENBQUMsQ0FBQztJQUNwVCxNQUFNb0MsY0FBYyxHQUFHLElBQUkvVixHQUFHLENBQUM0VixPQUFPLENBQUNuUixHQUFHLENBQUN4RyxLQUFLLElBQUlZLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxDQUFDO0lBQ3RGLE1BQU1tWixPQUFPLEdBQUcxQixPQUFPLENBQUNWLE9BQU8sQ0FBQ2pYLEtBQUssSUFBSW9ELFdBQVcsQ0FBQ29ELEdBQUcsQ0FBQyxDQUFDLENBQUN2RyxDQUFDLEVBQUVDLENBQUMsQ0FBQyxNQUFNO01BQUVELENBQUMsRUFBRUQsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUM7TUFBRUMsQ0FBQyxFQUFFRixLQUFLLENBQUNFLENBQUMsR0FBR0E7SUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUN1RyxNQUFNLENBQUN6RyxLQUFLLElBQUksQ0FBQzhYLGNBQWMsQ0FBQzNVLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxLQUFLRixLQUFLLENBQUNDLENBQUMsS0FBS3lYLEtBQUssQ0FBQ3pYLENBQUMsSUFBSUQsS0FBSyxDQUFDRSxDQUFDLEtBQUt3WCxLQUFLLENBQUN4WCxDQUFDLENBQUMsQ0FBQyxDQUFDdUcsTUFBTSxDQUFDLENBQUN6RyxLQUFLLEVBQUVlLEtBQUssRUFBRXdULE1BQU0sS0FBS0EsTUFBTSxDQUFDK0UsU0FBUyxDQUFDQyxLQUFLLElBQUlBLEtBQUssQ0FBQ3RaLENBQUMsS0FBS0QsS0FBSyxDQUFDQyxDQUFDLElBQUlzWixLQUFLLENBQUNyWixDQUFDLEtBQUtGLEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLEtBQUthLEtBQUssQ0FBQztJQUNwVixNQUFNeVksT0FBTyxHQUFHLENBQUM5QixLQUFLLEVBQUUsR0FBR0MsT0FBTyxFQUFFLEdBQUcwQixPQUFPLENBQUM7SUFDL0MsSUFBSUcsT0FBTyxDQUFDeFIsSUFBSSxDQUFDaEksS0FBSztNQUFBLElBQUE0WixTQUFBO01BQUEsT0FBSSxDQUFDM1ksT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxJQUFJLEVBQUEwWixTQUFBLEdBQUEzWSxPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUEwWixTQUFBLHVCQUFoQ0EsU0FBQSxDQUFrQ2hhLElBQUksTUFBSyxPQUFPLElBQUlrWCxRQUFRLENBQUMzVCxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsSUFBSWtaLFVBQVUsQ0FBQ2pXLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQztJQUFBLEVBQUMsRUFBRTtJQUMxTixNQUFNd1osTUFBTSxHQUFHRixPQUFPLENBQUNoVCxHQUFHLENBQUN4RyxLQUFLLEtBQUs7TUFBRUEsS0FBSztNQUFFSixJQUFJLEVBQUVxQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUVOO0lBQUssQ0FBQyxDQUFDLENBQUM7SUFDOUZ5WixPQUFPLENBQUN0SSxPQUFPLENBQUMvUSxLQUFLLElBQUk0SyxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRSxNQUFNLENBQUMsQ0FBQztJQUNsRTBLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRTZXLEtBQUssQ0FBQ3pYLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsRUFBRSxXQUFXLENBQUM7SUFDN0MsSUFBSSxDQUFDc0YsZUFBZSxDQUFDM0UsS0FBSyxFQUFFQSxLQUFLLENBQUMrQyxLQUFLLEVBQUUvQyxLQUFLLENBQUN3RixJQUFJLENBQUMsRUFBRTtNQUFFcVQsTUFBTSxDQUFDM0ksT0FBTyxDQUFDLENBQUM7UUFBRS9RLEtBQUs7UUFBRUo7TUFBSyxDQUFDLEtBQUtnTCxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRU4sSUFBSSxDQUFDLENBQUM7TUFBRTtJQUFTO0lBQzlJLE1BQU13WSxXQUFXLEdBQUdULE9BQU8sQ0FBQzVRLElBQUksQ0FBQ2xHLEtBQUssQ0FBQzhXLE9BQU8sQ0FBQ2pULE1BQU0sR0FBRyxDQUFDLENBQUMsQ0FBRTtJQUM1RCxNQUFNaU4sTUFBTSxHQUFHO01BQUUvTyxFQUFFLEVBQUUrVyxNQUFNLENBQUNqVixNQUFNLEdBQUcsQ0FBQyxHQUFHLGNBQWMsR0FBRyxZQUFZO01BQUV6RSxDQUFDLEVBQUVtWSxXQUFXLENBQUNuWSxDQUFDO01BQUVDLENBQUMsRUFBRWtZLFdBQVcsQ0FBQ2xZLENBQUM7TUFBRW1ZLEtBQUssRUFBRSxDQUFDO01BQUVDLFlBQVksRUFBRTtJQUFLLENBQUM7SUFDMUlrQixPQUFPLENBQUN6SSxPQUFPLENBQUMvUSxLQUFLLElBQUk4VyxRQUFRLENBQUN6UixHQUFHLENBQUN6RSxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsQ0FBQztJQUN4RVcsS0FBSyxDQUFDbUosS0FBSyxDQUFDbkYsSUFBSSxDQUFDOE0sTUFBTSxDQUFDO0lBQ3hCZ0ksTUFBTSxDQUFDOVUsSUFBSSxDQUFDO01BQUVqQyxFQUFFLEVBQUUsbUJBQW1CL0IsS0FBSyxDQUFDK0gsSUFBSSxJQUFJK1EsTUFBTSxDQUFDalYsTUFBTSxFQUFFO01BQUU5RSxJQUFJLEVBQUUsdUJBQXVCO01BQUVnVixRQUFRLEVBQUU7UUFBRSxHQUFHcFMsU0FBUyxDQUFDb1M7TUFBUyxDQUFDO01BQUU4QyxLQUFLO01BQUVDLE9BQU87TUFBRWhHO0lBQU8sQ0FBQyxDQUFDO0VBQ25LO0VBQ0EsSUFBSWdJLE1BQU0sQ0FBQ2pWLE1BQU0sS0FBS3NULE9BQU8sRUFBRSxNQUFNLElBQUk1UCxLQUFLLENBQUMscURBQXFENFAsT0FBTyxXQUFXMkIsTUFBTSxDQUFDalYsTUFBTSxFQUFFLENBQUM7RUFDdEk3RCxLQUFLLENBQUNxWCxVQUFVLEdBQUd5QixNQUFNO0FBQzNCLENBQUM7QUFFRCxNQUFNeE8sd0JBQXdCLEdBQUdBLENBQUN0SyxLQUFZLEVBQUU0SSxLQUF1QixLQUFXO0VBQ2hGLElBQUk1SSxLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxFQUFFO0VBQzdCLE1BQU1KLElBQUksR0FBR3dCLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzVHLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDNUMsSUFBSSxLQUFLLFdBQVcsQ0FBQztFQUMxRSxJQUFJLENBQUNxSSxJQUFJLEVBQUUsTUFBTSxJQUFJRyxLQUFLLENBQUMsZ0NBQWdDcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUM1RSxNQUFNaEosS0FBSyxHQUFHO0lBQUVDLENBQUMsRUFBRWdJLElBQUksQ0FBQzhLLFNBQVMsQ0FBQzlTLENBQUMsR0FBRzhHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ29ILElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3pKLEtBQUssR0FBRyxDQUFDLENBQUM7SUFBRXBKLENBQUMsRUFBRStILElBQUksQ0FBQzhLLFNBQVMsQ0FBQzdTLENBQUMsR0FBRzZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ29ILElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3hKLE1BQU0sR0FBRyxDQUFDO0VBQUUsQ0FBQztFQUN6SSxLQUFLLE1BQU0sQ0FBQ3RKLENBQUMsRUFBRUMsQ0FBQyxDQUFDLElBQUk0TixlQUFlLEVBQUVsRCxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxHQUFHQSxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQSxDQUFDLEVBQUUsT0FBTyxDQUFDO0VBQ3ZGMEssT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUUsT0FBTyxDQUFDO0FBQzNDLENBQUM7QUFFRCxNQUFNc0wsc0JBQXNCLEdBQUdBLENBQUMzSyxLQUFZLEVBQUU0SSxLQUF1QixLQUFXO0VBQzlFLElBQUk1SSxLQUFLLENBQUN3SCxLQUFLLEtBQUssU0FBUyxFQUFFO0VBQy9CLE1BQU02TyxJQUFJLEdBQUd6TixLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxRQUFRLENBQUMsSUFBSW9DLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxVQUFVLENBQUMsQ0FBQztFQUN0SCxJQUFJLENBQUM4VyxJQUFJLEVBQUUsTUFBTSxJQUFJOU8sS0FBSyxDQUFDLCtCQUErQnFCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDM0UsTUFBTThRLEtBQUssR0FBRzVDLElBQUksQ0FBQzRDLEtBQUssQ0FBQ2pFLEtBQUssQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQ3BQLE1BQU0sQ0FBQ3pHLEtBQUs7SUFBQSxJQUFBK1osU0FBQTtJQUFBLE9BQUksRUFBQUEsU0FBQSxHQUFBOVksT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBNlosU0FBQSx1QkFBaENBLFNBQUEsQ0FBa0NuYSxJQUFJLE1BQUssT0FBTztFQUFBLEVBQUM7RUFDekcsTUFBTThFLE1BQU0sR0FBR3FDLElBQUksQ0FBQ0UsR0FBRyxDQUFDNlMsS0FBSyxDQUFDcFYsTUFBTSxFQUFFLENBQUMsR0FBSTdELEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUMsR0FBSSxDQUFDLENBQUM7RUFDaEUsTUFBTTZDLEtBQUssR0FBR21ELElBQUksQ0FBQ0MsR0FBRyxDQUFDLENBQUMsRUFBRUQsSUFBSSxDQUFDbEcsS0FBSyxDQUFDLENBQUNpWixLQUFLLENBQUNwVixNQUFNLEdBQUdBLE1BQU0sSUFBSSxDQUFDLENBQUMsQ0FBQztFQUNsRSxNQUFNMlMsS0FBSyxHQUFHeUMsS0FBSyxDQUFDakUsS0FBSyxDQUFDalMsS0FBSyxFQUFFQSxLQUFLLEdBQUdjLE1BQU0sQ0FBQztFQUNoRCxJQUFJMlMsS0FBSyxDQUFDM1MsTUFBTSxHQUFHLENBQUMsRUFBRSxNQUFNLElBQUkwRCxLQUFLLENBQUMsNkJBQTZCcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUNwRixLQUFLLElBQUlqSSxLQUFLLEdBQUcsQ0FBQyxFQUFFQSxLQUFLLEdBQUdzVyxLQUFLLENBQUMzUyxNQUFNLEVBQUUzRCxLQUFLLEVBQUUsRUFBRTtJQUNqRCxNQUFNZixLQUFLLEdBQUdxWCxLQUFLLENBQUN0VyxLQUFLLENBQUM7SUFDMUIsTUFBTXBCLElBQUksR0FBR3NCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUM7SUFDN0MsSUFBSSxDQUFDUCxJQUFJLEVBQUU7SUFDWCxJQUFJb0IsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEVBQUU2SixPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRSxPQUFPLENBQUMsTUFDekQ7TUFBQSxJQUFBOFosTUFBQTtNQUNIcmEsSUFBSSxDQUFDQyxJQUFJLEdBQUcsU0FBUztNQUNyQixNQUFNcWEsSUFBSSxJQUFBRCxNQUFBLEdBQUczQyxLQUFLLENBQUN0VyxLQUFLLEdBQUcsQ0FBQyxDQUFDLGNBQUFpWixNQUFBLGNBQUFBLE1BQUEsR0FBSTNDLEtBQUssQ0FBQ3RXLEtBQUssR0FBRyxDQUFDLENBQUM7TUFDakRwQixJQUFJLENBQUN1WixJQUFJLEdBQUc7UUFBRXpCLFNBQVMsRUFBRTBCLGFBQWEsQ0FBQ2MsSUFBSSxDQUFDaGEsQ0FBQyxHQUFHRCxLQUFLLENBQUNDLENBQUMsRUFBRWdhLElBQUksQ0FBQy9aLENBQUMsR0FBR0YsS0FBSyxDQUFDRSxDQUFDO01BQUUsQ0FBQztJQUM5RTtFQUNGO0VBQ0EsTUFBTWdhLEtBQUssR0FBR3JaLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUM7RUFDN0IsTUFBTW9aLFdBQVcsR0FBRzFRLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQytPLE9BQU8sQ0FBQ2hQLElBQUksSUFBSTZCLEtBQUssQ0FBQ0MsSUFBSSxDQUFDO0lBQUVyRixNQUFNLEVBQUVxQyxJQUFJLENBQUNDLEdBQUcsQ0FBQyxDQUFDLEVBQUUsQ0FBQ2lCLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3pKLEtBQUssR0FBRyxDQUFDLEtBQUtyQixJQUFJLENBQUM4SyxTQUFTLENBQUN4SixNQUFNLEdBQUcsQ0FBQyxDQUFDO0VBQUUsQ0FBQyxFQUFFLENBQUM0TixDQUFDLEVBQUVwVyxLQUFLLE1BQU07SUFBRWQsQ0FBQyxFQUFFZ0ksSUFBSSxDQUFDOEssU0FBUyxDQUFDOVMsQ0FBQyxHQUFHLENBQUMsR0FBR2MsS0FBSyxJQUFJa0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSyxHQUFHLENBQUMsQ0FBQztJQUFFcEosQ0FBQyxFQUFFK0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDN1MsQ0FBQyxHQUFHLENBQUMsR0FBRzZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ0UsS0FBSyxJQUFJa0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSyxHQUFHLENBQUMsQ0FBQztFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQzdDLE1BQU0sQ0FBQ3pHLEtBQUs7SUFBQSxJQUFBb2EsVUFBQTtJQUFBLE9BQUksRUFBQUEsVUFBQSxHQUFBblosT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBa2EsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0N4YSxJQUFJLE1BQUssT0FBTztFQUFBLEVBQUM7RUFDOVcsS0FBSyxJQUFJbUIsS0FBSyxHQUFHLENBQUMsRUFBRUEsS0FBSyxHQUFHbVosS0FBSyxHQUFHLENBQUMsSUFBSUMsV0FBVyxDQUFDelYsTUFBTSxFQUFFM0QsS0FBSyxFQUFFLEVBQUU7SUFDcEUsTUFBTWYsS0FBSyxHQUFHbWEsV0FBVyxDQUFDcFosS0FBSyxHQUFHb1osV0FBVyxDQUFDelYsTUFBTSxDQUFFO0lBQ3RELE1BQU0vRSxJQUFJLEdBQUdzQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFFO0lBQzlDUCxJQUFJLENBQUNDLElBQUksR0FBRyxTQUFTO0lBQ3JCRCxJQUFJLENBQUN1WixJQUFJLEdBQUc7TUFBRXpCLFNBQVMsRUFBRTFXLEtBQUssR0FBRyxDQUFDLEdBQUcsR0FBRyxHQUFHO0lBQUksQ0FBQztFQUNsRDtBQUNGLENBQUM7QUFFRCxNQUFNMEsseUJBQXlCLEdBQUdBLENBQUM1SyxLQUFZLEVBQUU0SSxLQUF1QixLQUFXO0VBQUEsSUFBQTRRLHFCQUFBO0VBQ2pGLElBQUl4WixLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxFQUFFO0VBQzdCLE1BQU02TyxJQUFJLEdBQUd6TixLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxRQUFRLENBQUMsSUFBSW9DLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxVQUFVLENBQUMsQ0FBQztFQUN0SCxJQUFJLENBQUM4VyxJQUFJLEVBQUUsTUFBTSxJQUFJOU8sS0FBSyxDQUFDLGlDQUFpQ3FCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDN0UsTUFBTThRLEtBQUssR0FBRzVDLElBQUksQ0FBQzRDLEtBQUssQ0FBQ2pFLEtBQUssQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQ3BQLE1BQU0sQ0FBQ3pHLEtBQUs7SUFBQSxJQUFBc2EsVUFBQTtJQUFBLE9BQUksRUFBQUEsVUFBQSxHQUFBclosT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBb2EsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0MxYSxJQUFJLE1BQUssT0FBTztFQUFBLEVBQUM7RUFDekcsTUFBTWdFLEtBQUssR0FBR21ELElBQUksQ0FBQ0MsR0FBRyxDQUFDLENBQUMsRUFBRUQsSUFBSSxDQUFDbEcsS0FBSyxDQUFDaVosS0FBSyxDQUFDcFYsTUFBTSxHQUFHLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQztFQUMzRCxNQUFNMlMsS0FBSyxHQUFHeUMsS0FBSyxDQUFDakUsS0FBSyxDQUFDalMsS0FBSyxFQUFFQSxLQUFLLEdBQUcsQ0FBQyxDQUFDO0VBQzNDLElBQUl5VCxLQUFLLENBQUMzUyxNQUFNLEdBQUcsQ0FBQyxFQUFFLE1BQU0sSUFBSTBELEtBQUssQ0FBQywrQkFBK0JxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ3RGLEtBQUssSUFBSWpJLEtBQUssR0FBRyxDQUFDLEVBQUVBLEtBQUssR0FBR3NXLEtBQUssQ0FBQzNTLE1BQU0sRUFBRTNELEtBQUssRUFBRSxFQUFFNkosT0FBTyxDQUFDL0osS0FBSyxFQUFFd1csS0FBSyxDQUFDdFcsS0FBSyxDQUFDLENBQUNkLENBQUMsRUFBRW9YLEtBQUssQ0FBQ3RXLEtBQUssQ0FBQyxDQUFDYixDQUFDLEVBQUVhLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLE9BQU8sR0FBRyxLQUFLLENBQUM7RUFDcEksTUFBTXdaLElBQUksR0FBRzlRLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLE1BQU0sQ0FBQyxDQUFDO0VBQzVFLE1BQU1xUixTQUFTLElBQUE0SSxxQkFBQSxHQUFHRSxJQUFJLGFBQUpBLElBQUksdUJBQUpBLElBQUksQ0FBRVQsS0FBSyxDQUFDakUsS0FBSyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDcFAsTUFBTSxDQUFDekcsS0FBSztJQUFBLElBQUF3YSxVQUFBO0lBQUEsT0FBSSxFQUFBQSxVQUFBLEdBQUF2WixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUFzYSxVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQzVhLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQyxjQUFBeWEscUJBQUEsY0FBQUEscUJBQUEsR0FBSSxFQUFFO0VBQ3BILE1BQU1JLEtBQUssR0FBR2hKLFNBQVMsQ0FBQyxDQUFDLENBQUM7RUFDMUIsTUFBTWlKLEtBQUssR0FBR3JELEtBQUssQ0FBQzVHLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUMxQixJQUFJLENBQUNnSyxLQUFLLElBQUksQ0FBQ0MsS0FBSyxFQUFFLE1BQU0sSUFBSXRTLEtBQUssQ0FBQyw4QkFBOEJxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ3JGL0gsT0FBTyxDQUFDSixLQUFLLEVBQUU0WixLQUFLLENBQUN4YSxDQUFDLEVBQUV3YSxLQUFLLENBQUN2YSxDQUFDLENBQUMsQ0FBRXlhLFNBQVMsR0FBRyxDQUFDO0VBQy9DMVosT0FBTyxDQUFDSixLQUFLLEVBQUU2WixLQUFLLENBQUN6YSxDQUFDLEVBQUV5YSxLQUFLLENBQUN4YSxDQUFDLENBQUMsQ0FBRXlhLFNBQVMsR0FBRyxDQUFDO0VBQy9DOVosS0FBSyxDQUFDK1osVUFBVSxHQUFHLENBQUM7SUFBRWhZLEVBQUUsRUFBRSxlQUFlL0IsS0FBSyxDQUFDRSxLQUFLLElBQUkwWixLQUFLLENBQUN4YSxDQUFDLElBQUl3YSxLQUFLLENBQUN2YSxDQUFDLElBQUl3YSxLQUFLLENBQUN6YSxDQUFDLElBQUl5YSxLQUFLLENBQUN4YSxDQUFDLEVBQUU7SUFBRXVhLEtBQUs7SUFBRUMsS0FBSztJQUFFRyxRQUFRLEVBQUU7RUFBTSxDQUFDLENBQUM7QUFDdEksQ0FBQztBQUVELE1BQU1uUCxxQkFBcUIsR0FBR0EsQ0FBQzdLLEtBQVksRUFBRTRJLEtBQXVCLEtBQVc7RUFDN0UsSUFBSTVJLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxPQUFPLEVBQUU7RUFDN0IsTUFBTTZPLElBQUksR0FBR3pOLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLFFBQVEsQ0FBQyxJQUFJb0MsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLFVBQVUsQ0FBQyxDQUFDO0VBQ3RILElBQUksQ0FBQzhXLElBQUksRUFBRSxNQUFNLElBQUk5TyxLQUFLLENBQUMsNkJBQTZCcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUN6RSxNQUFNOFEsS0FBSyxHQUFHNUMsSUFBSSxDQUFDNEMsS0FBSyxDQUFDakUsS0FBSyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDcFAsTUFBTSxDQUFDekcsS0FBSztJQUFBLElBQUE4YSxVQUFBO0lBQUEsT0FBSSxFQUFBQSxVQUFBLEdBQUE3WixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUE0YSxVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ2xiLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQztFQUN6RyxNQUFNZ0UsS0FBSyxHQUFHbUQsSUFBSSxDQUFDQyxHQUFHLENBQUMsQ0FBQyxFQUFFRCxJQUFJLENBQUNsRyxLQUFLLENBQUNpWixLQUFLLENBQUNwVixNQUFNLEdBQUcsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDO0VBQzNELE1BQU0yUyxLQUFLLEdBQUd5QyxLQUFLLENBQUNqRSxLQUFLLENBQUNqUyxLQUFLLEVBQUVBLEtBQUssR0FBRyxDQUFDLENBQUM7RUFDM0MsSUFBSXlULEtBQUssQ0FBQzNTLE1BQU0sR0FBRyxDQUFDLEVBQUUsTUFBTSxJQUFJMEQsS0FBSyxDQUFDLDJCQUEyQnFCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDbEYsS0FBSyxJQUFJakksS0FBSyxHQUFHLENBQUMsRUFBRUEsS0FBSyxHQUFHc1csS0FBSyxDQUFDM1MsTUFBTSxFQUFFM0QsS0FBSyxFQUFFLEVBQUUsSUFBSUEsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEVBQUU2SixPQUFPLENBQUMvSixLQUFLLEVBQUV3VyxLQUFLLENBQUN0VyxLQUFLLENBQUMsQ0FBQ2QsQ0FBQyxFQUFFb1gsS0FBSyxDQUFDdFcsS0FBSyxDQUFDLENBQUNiLENBQUMsRUFBRSxNQUFNLENBQUM7QUFDaEksQ0FBQztBQUVELE1BQU15TCx5QkFBeUIsR0FBR0EsQ0FBQzlLLEtBQVksRUFBRTRJLEtBQXVCLEtBQVc7RUFDakYsSUFBSTVJLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxTQUFTLEVBQUU7RUFDL0IsTUFBTWtTLElBQUksR0FBRzlRLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLE1BQU0sQ0FBQyxDQUFDO0VBQzVFLE1BQU0yYSxNQUFNLEdBQUd0UixLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxRQUFRLENBQUMsSUFBSW9DLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxVQUFVLENBQUMsQ0FBQztFQUN4SCxJQUFJLENBQUNtYSxJQUFJLElBQUksQ0FBQ1EsTUFBTSxFQUFFLE1BQU0sSUFBSTNTLEtBQUssQ0FBQywrQkFBK0JxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ3RGLE1BQU1nUyxPQUFPLEdBQUlsQixLQUF1QixJQUFLQSxLQUFLLENBQUNqRSxLQUFLLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUNwUCxNQUFNLENBQUN6RyxLQUFLO0lBQUEsSUFBQWliLFVBQUE7SUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQWhhLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQSthLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDcmIsSUFBSSxNQUFLLE9BQU87RUFBQSxFQUFDO0VBQ25JLE1BQU1zYixRQUFRLEdBQUdGLE9BQU8sQ0FBQ1QsSUFBSSxDQUFDVCxLQUFLLENBQUM7RUFDcEMsTUFBTXFCLFVBQVUsR0FBR0gsT0FBTyxDQUFDRCxNQUFNLENBQUNqQixLQUFLLENBQUM7RUFDeEMsSUFBSW9CLFFBQVEsQ0FBQ3hXLE1BQU0sR0FBRyxDQUFDLElBQUl5VyxVQUFVLENBQUN6VyxNQUFNLEdBQUcsQ0FBQyxFQUFFLE1BQU0sSUFBSTBELEtBQUssQ0FBQyxnQ0FBZ0NxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ25ILEtBQUssSUFBSWpJLEtBQUssR0FBRyxDQUFDLEVBQUVBLEtBQUssR0FBR21hLFFBQVEsQ0FBQ3hXLE1BQU0sRUFBRTNELEtBQUssRUFBRSxFQUFFO0lBQ3BELE1BQU1mLEtBQUssR0FBR2tiLFFBQVEsQ0FBQ25hLEtBQUssQ0FBQztJQUM3QixNQUFNcEIsSUFBSSxHQUFHc0IsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBRTtJQUM5Q1AsSUFBSSxDQUFDZ2IsU0FBUyxHQUFHLENBQUM7SUFDbEIsSUFBSTVaLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxFQUFFcEIsSUFBSSxDQUFDQyxJQUFJLEdBQUcsTUFBTTtFQUN6QztFQUNBLEtBQUssSUFBSW1CLEtBQUssR0FBRyxDQUFDLEVBQUVBLEtBQUssR0FBR29hLFVBQVUsQ0FBQ3pXLE1BQU0sRUFBRTNELEtBQUssRUFBRSxFQUFFO0lBQ3RELE1BQU1mLEtBQUssR0FBR21iLFVBQVUsQ0FBQ3BhLEtBQUssQ0FBQztJQUMvQixNQUFNcEIsSUFBSSxHQUFHc0IsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBRTtJQUM5Q1AsSUFBSSxDQUFDZ2IsU0FBUyxHQUFHLENBQUM7SUFDbEIsSUFBSTVaLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxFQUFFcEIsSUFBSSxDQUFDQyxJQUFJLEdBQUcsT0FBTyxNQUNuQyxJQUFJbUIsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEVBQUVwQixJQUFJLENBQUNDLElBQUksR0FBRyxVQUFVO0VBQ2xEO0VBQ0EsTUFBTTZhLEtBQUssR0FBR1UsVUFBVSxDQUFDN1osSUFBSSxDQUFDdEIsS0FBSztJQUFBLElBQUFvYixVQUFBO0lBQUEsT0FBSSxFQUFBQSxVQUFBLEdBQUFuYSxPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUFrYixVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ3hiLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBRTtFQUMzRixNQUFNOGEsS0FBSyxHQUFHUSxRQUFRLENBQUM1WixJQUFJLENBQUN0QixLQUFLO0lBQUEsSUFBQXFiLFVBQUE7SUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQXBhLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQW1iLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDemIsSUFBSSxNQUFLLE1BQU07RUFBQSxFQUFFO0VBQ3hGaUIsS0FBSyxDQUFDK1osVUFBVSxHQUFHLENBQUM7SUFBRWhZLEVBQUUsRUFBRSxRQUFRL0IsS0FBSyxDQUFDRSxLQUFLLElBQUkwWixLQUFLLENBQUN4YSxDQUFDLElBQUl3YSxLQUFLLENBQUN2YSxDQUFDLElBQUl3YSxLQUFLLENBQUN6YSxDQUFDLElBQUl5YSxLQUFLLENBQUN4YSxDQUFDLEVBQUU7SUFBRXVhLEtBQUs7SUFBRUMsS0FBSztJQUFFRyxRQUFRLEVBQUU7RUFBSyxDQUFDLENBQUM7RUFDNUgsTUFBTVMsV0FBVyxHQUFHLENBQUMsR0FBR0gsVUFBVSxDQUFDO0VBQ25DLEtBQUssSUFBSXBhLEtBQUssR0FBRyxDQUFDLEVBQUVBLEtBQUssR0FBSUYsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxHQUFJLENBQUMsRUFBRUEsS0FBSyxFQUFFLEVBQUU7SUFDMUQsTUFBTXdhLE1BQU0sR0FBR0osVUFBVSxDQUFDcGEsS0FBSyxHQUFHb2EsVUFBVSxDQUFDelcsTUFBTSxDQUFFO0lBQ3JELE1BQU0xRSxLQUFLLEdBQUc4TixlQUFlLENBQUN0SCxHQUFHLENBQUMsQ0FBQyxDQUFDdkcsQ0FBQyxFQUFFQyxDQUFDLENBQUMsTUFBTTtNQUFFRCxDQUFDLEVBQUVzYixNQUFNLENBQUN0YixDQUFDLEdBQUdBLENBQUM7TUFBRUMsQ0FBQyxFQUFFcWIsTUFBTSxDQUFDcmIsQ0FBQyxHQUFHQTtJQUFFLENBQUMsQ0FBQyxDQUFDLENBQUNvQixJQUFJLENBQUNrQixTQUFTO01BQUEsSUFBQWdaLFVBQUE7TUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQXZhLE9BQU8sQ0FBQ0osS0FBSyxFQUFFMkIsU0FBUyxDQUFDdkMsQ0FBQyxFQUFFdUMsU0FBUyxDQUFDdEMsQ0FBQyxDQUFDLGNBQUFzYixVQUFBLHVCQUF4Q0EsVUFBQSxDQUEwQzViLElBQUksTUFBSyxPQUFPLElBQUksQ0FBQzBiLFdBQVcsQ0FBQ3RULElBQUksQ0FBQ3lULFFBQVEsSUFBSUEsUUFBUSxDQUFDeGIsQ0FBQyxLQUFLdUMsU0FBUyxDQUFDdkMsQ0FBQyxJQUFJd2IsUUFBUSxDQUFDdmIsQ0FBQyxLQUFLc0MsU0FBUyxDQUFDdEMsQ0FBQyxDQUFDO0lBQUEsRUFBQztJQUM5UCxJQUFJLENBQUNGLEtBQUssRUFBRTtJQUNaaUIsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFFTixJQUFJLEdBQUdtQixLQUFLLEdBQUcsQ0FBQyxHQUFHLE9BQU8sR0FBRyxVQUFVO0lBQ3pFdWEsV0FBVyxDQUFDelcsSUFBSSxDQUFDN0UsS0FBSyxDQUFDO0VBQ3pCO0VBQ0EsTUFBTTBiLElBQUksR0FBR2pTLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzVHLElBQUksQ0FBQzJHLElBQUksSUFBSUEsSUFBSSxDQUFDckksSUFBSSxLQUFLLFdBQVcsQ0FBQztFQUNoRSxJQUFJLENBQUM4YixJQUFJLEVBQUUsTUFBTSxJQUFJdFQsS0FBSyxDQUFDLDhCQUE4QnFCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDMUVuSSxLQUFLLENBQUM4YSxhQUFhLEdBQUc7SUFBRUQsSUFBSSxFQUFFO01BQUV6YixDQUFDLEVBQUV5YixJQUFJLENBQUMzSSxTQUFTLENBQUM5UyxDQUFDLEdBQUc4RyxJQUFJLENBQUNsRyxLQUFLLENBQUM2YSxJQUFJLENBQUMzSSxTQUFTLENBQUN6SixLQUFLLEdBQUcsQ0FBQyxDQUFDO01BQUVwSixDQUFDLEVBQUV3YixJQUFJLENBQUMzSSxTQUFTLENBQUM3UyxDQUFDLEdBQUc2RyxJQUFJLENBQUNsRyxLQUFLLENBQUM2YSxJQUFJLENBQUMzSSxTQUFTLENBQUN4SixNQUFNLEdBQUcsQ0FBQztJQUFFLENBQUM7SUFBRStSLFdBQVc7SUFBRUosUUFBUSxFQUFFLENBQUMsR0FBR0EsUUFBUTtFQUFFLENBQXlCO0FBQzNOLENBQUM7QUFFRCxNQUFNdFAsNEJBQTRCLEdBQUdBLENBQUMvSyxLQUFZLEVBQUU0SSxLQUF1QixLQUFXO0VBQUEsSUFBQW1TLFVBQUE7RUFDcEYsSUFBSS9hLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxjQUFjLEVBQUU7RUFDcEMsTUFBTWtTLElBQUksR0FBRzlRLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLE1BQU0sQ0FBQyxDQUFDO0VBQzVFLE1BQU0yYSxNQUFNLEdBQUd0UixLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxRQUFRLENBQUMsSUFBSW9DLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxVQUFVLENBQUMsQ0FBQztFQUN4SCxNQUFNeWIsTUFBTSxHQUFHcFMsS0FBSyxDQUFDdkIsS0FBSyxDQUFDNUcsSUFBSSxDQUFDa0IsU0FBUyxJQUFJQSxTQUFTLENBQUM1QyxJQUFJLEtBQUssZ0JBQWdCLENBQUM7RUFDakYsSUFBSSxDQUFDMmEsSUFBSSxJQUFJLENBQUNRLE1BQU0sSUFBSSxDQUFDYyxNQUFNLEVBQUUsTUFBTSxJQUFJelQsS0FBSyxDQUFDLGtDQUFrQ3FCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDcEcsTUFBTWdTLE9BQU8sR0FBSWxCLEtBQXVCLElBQUtBLEtBQUssQ0FBQ3JULE1BQU0sQ0FBQ3pHLEtBQUs7SUFBQSxJQUFBOGIsVUFBQTtJQUFBLE9BQUksRUFBQUEsVUFBQSxHQUFBN2EsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBNGIsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0NsYyxJQUFJLE1BQUssT0FBTztFQUFBLEVBQUM7RUFDdEgsTUFBTW1jLFdBQVcsR0FBR2YsT0FBTyxDQUFDVCxJQUFJLENBQUNULEtBQUssQ0FBQztFQUN2QyxNQUFNa0MsWUFBWSxHQUFHaEIsT0FBTyxDQUFDRCxNQUFNLENBQUNqQixLQUFLLENBQUM7RUFDMUMsSUFBSWlDLFdBQVcsQ0FBQ3JYLE1BQU0sR0FBRyxDQUFDLElBQUlzWCxZQUFZLENBQUN0WCxNQUFNLEdBQUcsQ0FBQyxFQUFFLE1BQU0sSUFBSTBELEtBQUssQ0FBQyxrQ0FBa0NxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQzFILEtBQUssSUFBSWpJLEtBQUssR0FBRyxDQUFDLEVBQUVBLEtBQUssR0FBR2diLFdBQVcsQ0FBQ3JYLE1BQU0sRUFBRTNELEtBQUssRUFBRSxFQUFFNkosT0FBTyxDQUFDL0osS0FBSyxFQUFFa2IsV0FBVyxDQUFDaGIsS0FBSyxDQUFDLENBQUNkLENBQUMsRUFBRThiLFdBQVcsQ0FBQ2hiLEtBQUssQ0FBQyxDQUFDYixDQUFDLEVBQUVhLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLFFBQVEsR0FBRyxPQUFPLENBQUM7RUFDekosS0FBSyxJQUFJQSxLQUFLLEdBQUcsQ0FBQyxFQUFFQSxLQUFLLEdBQUdpYixZQUFZLENBQUN0WCxNQUFNLEdBQUcsQ0FBQyxFQUFFM0QsS0FBSyxFQUFFLEVBQUU7SUFDNUQsTUFBTWYsS0FBSyxHQUFHZ2MsWUFBWSxDQUFDamIsS0FBSyxDQUFDO0lBQ2pDLE1BQU1rWixJQUFJLEdBQUcrQixZQUFZLENBQUNqYixLQUFLLEdBQUcsQ0FBQyxDQUFDO0lBQ3BDLE1BQU1wQixJQUFJLEdBQUdzQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFFO0lBQzlDUCxJQUFJLENBQUNDLElBQUksR0FBRyxTQUFTO0lBQ3JCRCxJQUFJLENBQUN1WixJQUFJLEdBQUc7TUFBRXpCLFNBQVMsRUFBRTBCLGFBQWEsQ0FBQ2MsSUFBSSxDQUFDaGEsQ0FBQyxHQUFHRCxLQUFLLENBQUNDLENBQUMsRUFBRWdhLElBQUksQ0FBQy9aLENBQUMsR0FBR0YsS0FBSyxDQUFDRSxDQUFDLENBQUM7TUFBRSxJQUFJYSxLQUFLLElBQUlpYixZQUFZLENBQUN0WCxNQUFNLEdBQUcsQ0FBQyxHQUFHO1FBQUV1WCxNQUFNLEVBQUU7TUFBb0IsQ0FBQyxHQUFHLENBQUMsQ0FBQztJQUFFLENBQUM7RUFDNUo7RUFDQSxNQUFNQyxNQUFNLEdBQUdGLFlBQVksQ0FBQ0EsWUFBWSxDQUFDdFgsTUFBTSxHQUFHLENBQUMsQ0FBQztFQUNwRCxNQUFNeVgsSUFBSSxHQUFHck8sZUFBZSxDQUFDdEgsR0FBRyxDQUFDLENBQUMsQ0FBQ3ZHLENBQUMsRUFBRUMsQ0FBQyxDQUFDLE1BQU07SUFBRUQsQ0FBQyxFQUFFaWMsTUFBTSxDQUFDamMsQ0FBQyxHQUFHQSxDQUFDO0lBQUVDLENBQUMsRUFBRWdjLE1BQU0sQ0FBQ2hjLENBQUMsR0FBR0E7RUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDb0IsSUFBSSxDQUFDdEIsS0FBSztJQUFBLElBQUFvYyxVQUFBO0lBQUEsT0FBSSxFQUFBQSxVQUFBLEdBQUFuYixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUFrYyxVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ3hjLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQztFQUN0SixNQUFNcWEsSUFBSSxHQUFHa0MsSUFBSSxhQUFKQSxJQUFJLGNBQUpBLElBQUksR0FBSUgsWUFBWSxDQUFDQSxZQUFZLENBQUN0WCxNQUFNLEdBQUcsQ0FBQyxDQUFDO0VBQzFELE1BQU0yWCxVQUFVLEdBQUdwYixPQUFPLENBQUNKLEtBQUssRUFBRXFiLE1BQU0sQ0FBQ2pjLENBQUMsRUFBRWljLE1BQU0sQ0FBQ2hjLENBQUMsQ0FBRTtFQUN0RG1jLFVBQVUsQ0FBQ3pjLElBQUksR0FBRyxTQUFTO0VBQzNCeWMsVUFBVSxDQUFDbkQsSUFBSSxHQUFHO0lBQUV6QixTQUFTLEVBQUUwQixhQUFhLENBQUNjLElBQUksQ0FBQ2hhLENBQUMsR0FBR2ljLE1BQU0sQ0FBQ2pjLENBQUMsRUFBRWdhLElBQUksQ0FBQy9aLENBQUMsR0FBR2djLE1BQU0sQ0FBQ2hjLENBQUM7RUFBRSxDQUFDO0VBQ3BGLElBQUlpYyxJQUFJLEVBQUV2UixPQUFPLENBQUMvSixLQUFLLEVBQUVzYixJQUFJLENBQUNsYyxDQUFDLEVBQUVrYyxJQUFJLENBQUNqYyxDQUFDLEVBQUUsUUFBUSxDQUFDO0VBQ2xELE1BQU1zVyxNQUFNLEdBQUc7SUFBRXZXLENBQUMsRUFBRTRiLE1BQU0sQ0FBQzlJLFNBQVMsQ0FBQzlTLENBQUMsR0FBRzhHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ2diLE1BQU0sQ0FBQzlJLFNBQVMsQ0FBQ3pKLEtBQUssR0FBRyxDQUFDLENBQUM7SUFBRXBKLENBQUMsRUFBRTJiLE1BQU0sQ0FBQzlJLFNBQVMsQ0FBQzdTLENBQUMsR0FBRzZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ2diLE1BQU0sQ0FBQzlJLFNBQVMsQ0FBQ3hKLE1BQU0sR0FBRyxDQUFDO0VBQUUsQ0FBQztFQUNsSixJQUFJLEVBQUFxUyxVQUFBLEdBQUEzYSxPQUFPLENBQUNKLEtBQUssRUFBRTJWLE1BQU0sQ0FBQ3ZXLENBQUMsRUFBRXVXLE1BQU0sQ0FBQ3RXLENBQUMsQ0FBQyxjQUFBMGIsVUFBQSx1QkFBbENBLFVBQUEsQ0FBb0NoYyxJQUFJLE1BQUssT0FBTyxFQUFFZ0wsT0FBTyxDQUFDL0osS0FBSyxFQUFFMlYsTUFBTSxDQUFDdlcsQ0FBQyxFQUFFdVcsTUFBTSxDQUFDdFcsQ0FBQyxFQUFFLFFBQVEsQ0FBQztBQUN4RyxDQUFDO0FBRUQsTUFBTXNNLHdCQUF3QixHQUFHQSxDQUFDM0wsS0FBWSxFQUFFNEksS0FBdUIsRUFBRXdILEdBQVEsS0FBVztFQUMxRixJQUFJcFEsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLGNBQWMsRUFBRTtFQUNwQyxNQUFNMlAsT0FBTyxHQUFHLENBQUMsR0FBR25YLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUM7RUFDbkMsTUFBTXNPLE1BQU0sR0FBR3hPLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUM7RUFDMUMsTUFBTXFZLFVBQVUsR0FBRyxJQUFJclgsR0FBRyxDQUFDbkQsb0JBQW9CLENBQUM2SyxLQUFLLENBQUMsQ0FBQ2pELEdBQUcsQ0FBQ3hHLEtBQUssSUFBSVksT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDdEcsTUFBTW9jLE9BQU8sR0FBR3JMLEdBQUcsQ0FBQ3BDLE9BQU8sQ0FBQ2hPLEtBQUssQ0FBQ0ssS0FBSyxDQUFDK1YsT0FBTyxDQUFDLENBQUN0WCxJQUFJLEVBQUVvQixLQUFLLEtBQUtwQixJQUFJLENBQUNDLElBQUksS0FBSyxPQUFPLEdBQUcsQ0FBQ2tCLE9BQU8sQ0FBQ0QsS0FBSyxFQUFFRSxLQUFLLENBQUMsQ0FBQyxHQUFHLEVBQUUsQ0FBQyxDQUFDO0VBQ3ZILE1BQU13YixVQUF1QixHQUFHLEVBQUU7RUFDbEMsS0FBSyxNQUFNNVIsTUFBTSxJQUFJMlIsT0FBTyxFQUFFO0lBQUEsSUFBQUUsVUFBQSxFQUFBQyxVQUFBO0lBQzVCLElBQUlGLFVBQVUsQ0FBQzdYLE1BQU0sS0FBS3NULE9BQU8sRUFBRTtJQUNuQyxNQUFNOEIsS0FBSyxHQUFHLEVBQWE7SUFDM0IsS0FBSyxJQUFJNVosQ0FBQyxHQUFHeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHbVAsTUFBTSxFQUFFblAsQ0FBQyxJQUFJeUssTUFBTSxDQUFDekssQ0FBQyxHQUFHbVAsTUFBTSxFQUFFblAsQ0FBQyxFQUFFLEVBQUUsS0FBSyxJQUFJRCxDQUFDLEdBQUcwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUdvUCxNQUFNLEVBQUVwUCxDQUFDLElBQUkwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUdvUCxNQUFNLEVBQUVwUCxDQUFDLEVBQUUsRUFBRSxJQUFJQSxDQUFDLEtBQUswSyxNQUFNLENBQUMxSyxDQUFDLElBQUlDLENBQUMsS0FBS3lLLE1BQU0sQ0FBQ3pLLENBQUMsRUFBRTRaLEtBQUssQ0FBQ2pWLElBQUksQ0FBQztNQUFFNUUsQ0FBQztNQUFFQztJQUFFLENBQUMsQ0FBQztJQUNwTCxNQUFNcWIsTUFBTSxHQUFHO01BQUV0YixDQUFDLEVBQUUwSyxNQUFNLENBQUMxSyxDQUFDLEdBQUdvUCxNQUFNLEdBQUcsQ0FBQztNQUFFblAsQ0FBQyxFQUFFeUssTUFBTSxDQUFDeks7SUFBRSxDQUFDO0lBQ3hELElBQUksRUFBQXNjLFVBQUEsR0FBQXZiLE9BQU8sQ0FBQ0osS0FBSyxFQUFFOEosTUFBTSxDQUFDMUssQ0FBQyxFQUFFMEssTUFBTSxDQUFDekssQ0FBQyxDQUFDLGNBQUFzYyxVQUFBLHVCQUFsQ0EsVUFBQSxDQUFvQzVjLElBQUksTUFBSyxPQUFPLElBQUksRUFBQTZjLFVBQUEsR0FBQXhiLE9BQU8sQ0FBQ0osS0FBSyxFQUFFMGEsTUFBTSxDQUFDdGIsQ0FBQyxFQUFFc2IsTUFBTSxDQUFDcmIsQ0FBQyxDQUFDLGNBQUF1YyxVQUFBLHVCQUFsQ0EsVUFBQSxDQUFvQzdjLElBQUksTUFBSyxPQUFPLElBQUksQ0FBQytLLE1BQU0sRUFBRTRRLE1BQU0sRUFBRSxHQUFHekIsS0FBSyxDQUFDLENBQUM5UixJQUFJLENBQUNoSSxLQUFLO01BQUEsSUFBQTBjLFVBQUE7TUFBQSxPQUFJLENBQUN6YixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLElBQUksRUFBQXdjLFVBQUEsR0FBQXpiLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQXdjLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDOWMsSUFBSSxNQUFLLE9BQU8sSUFBSXdaLFVBQVUsQ0FBQ2pXLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxJQUFJcWMsVUFBVSxDQUFDdlUsSUFBSSxDQUFDMlUsU0FBUyxJQUFJQSxTQUFTLENBQUM3QyxLQUFLLENBQUM5UixJQUFJLENBQUM0VSxJQUFJLElBQUlBLElBQUksQ0FBQzNjLENBQUMsS0FBS0QsS0FBSyxDQUFDQyxDQUFDLElBQUkyYyxJQUFJLENBQUMxYyxDQUFDLEtBQUtGLEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLElBQUl5YyxTQUFTLENBQUNoUyxNQUFNLENBQUMxSyxDQUFDLEtBQUtELEtBQUssQ0FBQ0MsQ0FBQyxJQUFJMGMsU0FBUyxDQUFDaFMsTUFBTSxDQUFDekssQ0FBQyxLQUFLRixLQUFLLENBQUNFLENBQUMsQ0FBQztJQUFBLEVBQUMsRUFBRTtJQUN2ZDBLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRThKLE1BQU0sQ0FBQzFLLENBQUMsRUFBRTBLLE1BQU0sQ0FBQ3pLLENBQUMsRUFBRSxXQUFXLENBQUM7SUFDL0MsS0FBSyxNQUFNRixLQUFLLElBQUk4WixLQUFLLEVBQUU7TUFDekIsTUFBTW5hLElBQUksR0FBR3NCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUU7TUFDOUNQLElBQUksQ0FBQ0MsSUFBSSxHQUFHLFNBQVM7TUFDckJELElBQUksQ0FBQ3VaLElBQUksR0FBRztRQUFFekIsU0FBUyxFQUFFMEIsYUFBYSxDQUFDeE8sTUFBTSxDQUFDMUssQ0FBQyxHQUFHRCxLQUFLLENBQUNDLENBQUMsRUFBRTBLLE1BQU0sQ0FBQ3pLLENBQUMsR0FBR0YsS0FBSyxDQUFDRSxDQUFDLENBQUM7UUFBRStiLE1BQU0sRUFBRTtNQUFXLENBQUM7SUFDdEc7SUFDQXJSLE9BQU8sQ0FBQy9KLEtBQUssRUFBRTBhLE1BQU0sQ0FBQ3RiLENBQUMsRUFBRXNiLE1BQU0sQ0FBQ3JiLENBQUMsRUFBRSxRQUFRLENBQUM7SUFDNUNxYyxVQUFVLENBQUMxWCxJQUFJLENBQUM7TUFBRWpDLEVBQUUsRUFBRSxhQUFhL0IsS0FBSyxDQUFDK0gsSUFBSSxJQUFJMlQsVUFBVSxDQUFDN1gsTUFBTSxFQUFFO01BQUVpRyxNQUFNO01BQUUwRSxNQUFNO01BQUV5SyxLQUFLO01BQUV5QjtJQUFPLENBQUMsQ0FBQztFQUN4RztFQUNBLElBQUlnQixVQUFVLENBQUM3WCxNQUFNLEtBQUtzVCxPQUFPLEVBQUUsTUFBTSxJQUFJNVAsS0FBSyxDQUFDLGlEQUFpRDRQLE9BQU8sV0FBV3VFLFVBQVUsQ0FBQzdYLE1BQU0sRUFBRSxDQUFDO0VBQzFJN0QsS0FBSyxDQUFDMGIsVUFBVSxHQUFHQSxVQUFVO0FBQy9CLENBQUM7QUFFRCxNQUFNMVEsdUJBQXVCLEdBQUdBLENBQUNoTCxLQUFZLEVBQUU0SSxLQUF1QixLQUFXO0VBQy9FLElBQUk1SSxLQUFLLENBQUN3SCxLQUFLLEtBQUssUUFBUSxFQUFFO0VBQzlCLE1BQU14QixTQUFTLEdBQUdoRyxLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDO0VBQ2pDLE1BQU13WixJQUFJLEdBQUc5USxLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxNQUFNLENBQUMsQ0FBQztFQUM1RSxNQUFNMmEsTUFBTSxHQUFHdFIsS0FBSyxDQUFDdU4sS0FBSyxDQUFDMVYsSUFBSSxDQUFDa0IsU0FBUyxJQUFJQSxTQUFTLENBQUNxWCxLQUFLLENBQUN6WixRQUFRLENBQUMsUUFBUSxDQUFDLElBQUlvQyxTQUFTLENBQUNxWCxLQUFLLENBQUN6WixRQUFRLENBQUMsVUFBVSxDQUFDLENBQUM7RUFDeEgsSUFBSSxDQUFDbWEsSUFBSSxJQUFJLENBQUNRLE1BQU0sRUFBRSxNQUFNLElBQUkzUyxLQUFLLENBQUMsaUNBQWlDcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUN4RixNQUFNZ1MsT0FBTyxHQUFJbEIsS0FBdUIsSUFBS0EsS0FBSyxDQUFDakUsS0FBSyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDcFAsTUFBTSxDQUFDekcsS0FBSztJQUFBLElBQUE2YyxVQUFBO0lBQUEsT0FBSSxFQUFBQSxVQUFBLEdBQUE1YixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUEyYyxVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ2pkLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQztFQUNuSSxNQUFNa2QsU0FBUyxHQUFHOUIsT0FBTyxDQUFDVCxJQUFJLENBQUNULEtBQUssQ0FBQztFQUNyQyxNQUFNaUQsUUFBUSxHQUFHL0IsT0FBTyxDQUFDRCxNQUFNLENBQUNqQixLQUFLLENBQUM7RUFDdEMsSUFBSWdELFNBQVMsQ0FBQ3BZLE1BQU0sR0FBRyxDQUFDLElBQUlxWSxRQUFRLENBQUNyWSxNQUFNLEdBQUcsQ0FBQyxFQUFFLE1BQU0sSUFBSTBELEtBQUssQ0FBQywrQkFBK0JxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ2pILE1BQU1nVSxRQUFRLEdBQUcsSUFBSWpiLEdBQUcsQ0FBQyxDQUFDLEdBQUcrYSxTQUFTLEVBQUUsR0FBR0MsUUFBUSxDQUFDLENBQUN2VyxHQUFHLENBQUN4RyxLQUFLLElBQUlZLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxDQUFDO0VBQ3BHLE1BQU0rYyxTQUFTLEdBQUd4VCxLQUFLLENBQUN1TixLQUFLLENBQUNDLE9BQU8sQ0FBQ0MsSUFBSSxJQUFJOEQsT0FBTyxDQUFDOUQsSUFBSSxDQUFDNEMsS0FBSyxDQUFDLENBQUMsQ0FBQ3JULE1BQU0sQ0FBQ3pHLEtBQUssSUFBSSxDQUFDZ2QsUUFBUSxDQUFDN1osR0FBRyxDQUFDdkMsT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDbkksSUFBSStjLFNBQVMsQ0FBQ3ZZLE1BQU0sR0FBRyxDQUFDLEVBQUUsTUFBTSxJQUFJMEQsS0FBSyxDQUFDLDRCQUE0QnFCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDdkYsS0FBSyxJQUFJakksS0FBSyxHQUFHLENBQUMsRUFBRUEsS0FBSyxHQUFHK2IsU0FBUyxDQUFDcFksTUFBTSxFQUFFM0QsS0FBSyxFQUFFLEVBQUU7SUFDckQsTUFBTWYsS0FBSyxHQUFHOGMsU0FBUyxDQUFDL2IsS0FBSyxDQUFDO0lBQzlCLE1BQU1wQixJQUFJLEdBQUdzQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFFO0lBQzlDUCxJQUFJLENBQUNnYixTQUFTLEdBQUcsQ0FBQztJQUNsQmhiLElBQUksQ0FBQ0MsSUFBSSxHQUFHbUIsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEdBQUcsTUFBTSxHQUFHLE9BQU87RUFDaEQ7RUFDQSxLQUFLLE1BQU1mLEtBQUssSUFBSStjLFFBQVEsRUFBRTtJQUM1QixNQUFNcGQsSUFBSSxHQUFHc0IsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBRTtJQUM5Q1AsSUFBSSxDQUFDZ2IsU0FBUyxHQUFHLENBQUM7SUFDbEJoYixJQUFJLENBQUNDLElBQUksR0FBRyxPQUFPO0VBQ3JCO0VBQ0EsS0FBSyxJQUFJbUIsS0FBSyxHQUFHLENBQUMsRUFBRUEsS0FBSyxHQUFHa2MsU0FBUyxDQUFDdlksTUFBTSxFQUFFM0QsS0FBSyxFQUFFLEVBQUU7SUFDckQsTUFBTXBCLElBQUksR0FBR3NCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFb2MsU0FBUyxDQUFDbGMsS0FBSyxDQUFDLENBQUVkLENBQUMsRUFBRWdkLFNBQVMsQ0FBQ2xjLEtBQUssQ0FBQyxDQUFFYixDQUFDLENBQUU7SUFDdEVQLElBQUksQ0FBQ2diLFNBQVMsR0FBRyxDQUFDO0lBQ2xCLElBQUk1WixLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsRUFBRXBCLElBQUksQ0FBQ0MsSUFBSSxHQUFHLE1BQU07RUFDekM7RUFDQSxNQUFNc2QsUUFBUSxHQUFHelQsS0FBSyxDQUFDdkIsS0FBSyxDQUFDNUcsSUFBSSxDQUFDa0IsU0FBUyxJQUFJQSxTQUFTLENBQUM1QyxJQUFJLEtBQUssZ0JBQWdCLENBQUM7RUFDbkYsTUFBTXVkLEtBQUssR0FBR0QsUUFBUSxJQUFJcFQsS0FBSyxDQUFDQyxJQUFJLENBQUM7SUFBRXJGLE1BQU0sRUFBRXdZLFFBQVEsQ0FBQ25LLFNBQVMsQ0FBQ3pKLEtBQUssR0FBRzRULFFBQVEsQ0FBQ25LLFNBQVMsQ0FBQ3hKO0VBQU8sQ0FBQyxFQUFFLENBQUM0TixDQUFDLEVBQUVwVyxLQUFLLE1BQU07SUFBRWQsQ0FBQyxFQUFFaWQsUUFBUSxDQUFDbkssU0FBUyxDQUFDOVMsQ0FBQyxHQUFHYyxLQUFLLEdBQUdtYyxRQUFRLENBQUNuSyxTQUFTLENBQUN6SixLQUFLO0lBQUVwSixDQUFDLEVBQUVnZCxRQUFRLENBQUNuSyxTQUFTLENBQUM3UyxDQUFDLEdBQUc2RyxJQUFJLENBQUNsRyxLQUFLLENBQUNFLEtBQUssR0FBR21jLFFBQVEsQ0FBQ25LLFNBQVMsQ0FBQ3pKLEtBQUs7RUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDaEksSUFBSSxDQUFDdEIsS0FBSztJQUFBLElBQUFvZCxVQUFBO0lBQUEsT0FBSSxFQUFBQSxVQUFBLEdBQUFuYyxPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUFrZCxVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ3hkLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQztFQUNoVSxJQUFJLENBQUN1ZCxLQUFLLEVBQUUsTUFBTSxJQUFJL1UsS0FBSyxDQUFDLG9DQUFvQ3FCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDakYsTUFBTXFVLFNBQVMsR0FBR3BjLE9BQU8sQ0FBQ0osS0FBSyxFQUFFc2MsS0FBSyxDQUFDbGQsQ0FBQyxFQUFFa2QsS0FBSyxDQUFDamQsQ0FBQyxDQUFFO0VBQ25EbWQsU0FBUyxDQUFDMUMsU0FBUyxHQUFHLENBQUM7RUFDdkIwQyxTQUFTLENBQUN6ZCxJQUFJLEdBQUcsT0FBTztFQUN4QixNQUFNMGQsT0FBTyxHQUFHQSxDQUFDN0MsS0FBdUIsRUFBRUMsS0FBdUIsRUFBRTZDLElBQWlCLEtBQWlEOUMsS0FBSyxDQUFDeEQsT0FBTyxDQUFDbE4sSUFBSSxJQUFJMlEsS0FBSyxDQUFDbFUsR0FBRyxDQUFDcUwsRUFBRSxLQUFLO0lBQUU0SSxLQUFLLEVBQUUxUSxJQUFJO0lBQUUyUSxLQUFLLEVBQUU3STtFQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQ3BMLE1BQU0sQ0FBQytXLElBQUksSUFBSSxDQUFDRCxJQUFJLENBQUNwYSxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRTJjLElBQUksQ0FBQy9DLEtBQUssQ0FBQ3hhLENBQUMsRUFBRXVkLElBQUksQ0FBQy9DLEtBQUssQ0FBQ3ZhLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQ3FkLElBQUksQ0FBQ3BhLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFMmMsSUFBSSxDQUFDOUMsS0FBSyxDQUFDemEsQ0FBQyxFQUFFdWQsSUFBSSxDQUFDOUMsS0FBSyxDQUFDeGEsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDNkUsSUFBSSxDQUFDLENBQUNzUixJQUFJLEVBQUVJLEtBQUssS0FBS3BELFFBQVEsQ0FBQ2dELElBQUksQ0FBQ29FLEtBQUssRUFBRXBFLElBQUksQ0FBQ3FFLEtBQUssQ0FBQyxHQUFHckgsUUFBUSxDQUFDb0QsS0FBSyxDQUFDZ0UsS0FBSyxFQUFFaEUsS0FBSyxDQUFDaUUsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDeGEsTUFBTStDLEtBQUssR0FBRyxDQUFDLENBQUNWLFFBQVEsRUFBRUUsU0FBUyxDQUFDLEVBQUUsQ0FBQ0EsU0FBUyxFQUFFSCxTQUFTLENBQUMsRUFBRSxDQUFDQyxRQUFRLEVBQUVELFNBQVMsQ0FBQyxDQUFVO0VBQzdGLE1BQU1ZLEtBQUssR0FBRyxFQUEyQztFQUN6RCxNQUFNSCxJQUFJLEdBQUcsSUFBSXhiLEdBQUcsQ0FBUyxDQUFDO0VBQzlCLEtBQUssSUFBSWhCLEtBQUssR0FBRyxDQUFDLEVBQUVBLEtBQUssR0FBRyxDQUFDLEdBQUc4RixTQUFTLEVBQUU5RixLQUFLLEVBQUUsRUFBRTtJQUNsRCxNQUFNNGMsSUFBSSxHQUFHRixLQUFLLENBQUMxYyxLQUFLLEdBQUcwYyxLQUFLLENBQUMvWSxNQUFNLENBQUM7SUFDeEMsTUFBTThZLElBQUksR0FBR0YsT0FBTyxDQUFDSyxJQUFJLENBQUMsQ0FBQyxDQUFDLEVBQUVBLElBQUksQ0FBQyxDQUFDLENBQUMsRUFBRUosSUFBSSxDQUFDO0lBQzVDLElBQUksQ0FBQ0MsSUFBSSxFQUFFLE1BQU0sSUFBSXBWLEtBQUssQ0FBQywrQkFBK0JxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0lBQzNFMFUsS0FBSyxDQUFDN1ksSUFBSSxDQUFDMlksSUFBSSxDQUFDO0lBQ2hCRCxJQUFJLENBQUNsWSxHQUFHLENBQUN6RSxPQUFPLENBQUNDLEtBQUssRUFBRTJjLElBQUksQ0FBQy9DLEtBQUssQ0FBQ3hhLENBQUMsRUFBRXVkLElBQUksQ0FBQy9DLEtBQUssQ0FBQ3ZhLENBQUMsQ0FBQyxDQUFDO0lBQ3BEcWQsSUFBSSxDQUFDbFksR0FBRyxDQUFDekUsT0FBTyxDQUFDQyxLQUFLLEVBQUUyYyxJQUFJLENBQUM5QyxLQUFLLENBQUN6YSxDQUFDLEVBQUV1ZCxJQUFJLENBQUM5QyxLQUFLLENBQUN4YSxDQUFDLENBQUMsQ0FBQztFQUN0RDtFQUNBVyxLQUFLLENBQUMrWixVQUFVLEdBQUc4QyxLQUFLLENBQUNsWCxHQUFHLENBQUMsQ0FBQztJQUFFaVUsS0FBSztJQUFFQztFQUFNLENBQUMsRUFBRTNaLEtBQUssTUFBTTtJQUFFNkIsRUFBRSxFQUFFLFNBQVMvQixLQUFLLENBQUNFLEtBQUssSUFBSUEsS0FBSyxJQUFJMFosS0FBSyxDQUFDeGEsQ0FBQyxJQUFJd2EsS0FBSyxDQUFDdmEsQ0FBQyxJQUFJd2EsS0FBSyxDQUFDemEsQ0FBQyxJQUFJeWEsS0FBSyxDQUFDeGEsQ0FBQyxFQUFFO0lBQUV1YSxLQUFLO0lBQUVDLEtBQUs7SUFBRUcsUUFBUSxFQUFFO0VBQU0sQ0FBQyxDQUFDLENBQUM7RUFDL0ssTUFBTStDLGFBQXdCLEdBQUcsRUFBRTtFQUNuQyxLQUFLLElBQUk3YyxLQUFLLEdBQUcsQ0FBQyxFQUFFQSxLQUFLLEdBQUc4RixTQUFTLEVBQUU5RixLQUFLLEVBQUUsRUFBRTtJQUM5QyxNQUFNc1csS0FBSyxHQUFHMEYsUUFBUTtJQUN0QixNQUFNblosS0FBSyxHQUFHbUQsSUFBSSxDQUFDRSxHQUFHLENBQUNvUSxLQUFLLENBQUMzUyxNQUFNLEdBQUcsQ0FBQyxFQUFFLENBQUMsR0FBRzNELEtBQUssR0FBRyxDQUFDLENBQUM7SUFDdkQsTUFBTThjLFFBQVEsR0FBR3hHLEtBQUssQ0FBQ3hCLEtBQUssQ0FBQ2pTLEtBQUssRUFBRUEsS0FBSyxHQUFHLENBQUMsQ0FBQztJQUM5QyxJQUFJaWEsUUFBUSxDQUFDblosTUFBTSxHQUFHLENBQUMsRUFBRTtJQUN6QixLQUFLLElBQUk4USxJQUFJLEdBQUcsQ0FBQyxFQUFFQSxJQUFJLEdBQUdxSSxRQUFRLENBQUNuWixNQUFNLEdBQUcsQ0FBQyxFQUFFOFEsSUFBSSxFQUFFLEVBQUU7TUFDckQsTUFBTXhWLEtBQUssR0FBRzZkLFFBQVEsQ0FBQ3JJLElBQUksQ0FBRTtNQUM3QixNQUFNeUUsSUFBSSxHQUFHNEQsUUFBUSxDQUFDckksSUFBSSxHQUFHLENBQUMsQ0FBRTtNQUNoQyxNQUFNN1YsSUFBSSxHQUFHc0IsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBRTtNQUM5Q1AsSUFBSSxDQUFDQyxJQUFJLEdBQUcsT0FBTztNQUNuQkQsSUFBSSxDQUFDdVosSUFBSSxHQUFHO1FBQUV6QixTQUFTLEVBQUUwQixhQUFhLENBQUNjLElBQUksQ0FBQ2hhLENBQUMsR0FBR0QsS0FBSyxDQUFDQyxDQUFDLEVBQUVnYSxJQUFJLENBQUMvWixDQUFDLEdBQUdGLEtBQUssQ0FBQ0UsQ0FBQyxDQUFDO1FBQUUrYixNQUFNLEVBQUU7TUFBUyxDQUFDO0lBQ2hHO0lBQ0EsTUFBTTZCLE1BQU0sR0FBRzdjLE9BQU8sQ0FBQ0osS0FBSyxFQUFFZ2QsUUFBUSxDQUFDcE4sRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUV4USxDQUFDLEVBQUU0ZCxRQUFRLENBQUNwTixFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBRXZRLENBQUMsQ0FBRTtJQUN0RTRkLE1BQU0sQ0FBQ2xlLElBQUksR0FBRyxNQUFNO0lBQ3BCLE9BQU9rZSxNQUFNLENBQUM1RSxJQUFJO0lBQ2xCMEUsYUFBYSxDQUFDL1ksSUFBSSxDQUFDZ1osUUFBUSxDQUFDO0VBQzlCO0VBQ0EsTUFBTUUsZ0JBQWdCLEdBQUdsZCxLQUFLLENBQUNLLEtBQUssQ0FBQytWLE9BQU8sQ0FBQyxDQUFDdFgsSUFBSSxFQUFFb0IsS0FBSyxLQUFLcEIsSUFBSSxDQUFDQyxJQUFJLEtBQUssT0FBTyxJQUFJa08sZUFBZSxDQUFDOUYsSUFBSSxDQUFDLENBQUMsQ0FBQy9ILENBQUMsRUFBRUMsQ0FBQyxDQUFDO0lBQUEsSUFBQThkLFVBQUE7SUFBQSxPQUFLLEVBQUFBLFVBQUEsR0FBQS9jLE9BQU8sQ0FBQ0osS0FBSyxFQUFFRSxLQUFLLEdBQUdGLEtBQUssQ0FBQ3lJLEtBQUssR0FBR3JKLENBQUMsRUFBRThHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ0UsS0FBSyxHQUFHRixLQUFLLENBQUN5SSxLQUFLLENBQUMsR0FBR3BKLENBQUMsQ0FBQyxjQUFBOGQsVUFBQSx1QkFBNUVBLFVBQUEsQ0FBOEVwZSxJQUFJLE1BQUssV0FBVztFQUFBLEVBQUMsR0FBRyxDQUFDa0IsT0FBTyxDQUFDRCxLQUFLLEVBQUVFLEtBQUssQ0FBQyxDQUFDLEdBQUcsRUFBRSxDQUFDLENBQUM4VSxLQUFLLENBQUMsQ0FBQyxFQUFFLENBQUMsR0FBR2hQLFNBQVMsQ0FBQztFQUNuUmhHLEtBQUssQ0FBQ29kLFdBQVcsR0FBRztJQUFFbEIsUUFBUSxFQUFFLENBQUMsR0FBR0EsUUFBUSxDQUFDO0lBQUVFLFNBQVM7SUFBRWlCLFNBQVMsRUFBRSxDQUFDLEdBQUdwQixTQUFTLENBQUM7SUFBRWMsYUFBYTtJQUFFRztFQUFpQixDQUF1QjtBQUM5SSxDQUFDO0FBRUQsTUFBTXRSLG1CQUFtQixHQUFHQSxDQUFDNUwsS0FBWSxFQUFFNEksS0FBdUIsRUFBRXdILEdBQVEsS0FBVztFQUNyRixJQUFJcFEsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFFBQVEsRUFBRTtFQUM5QixNQUFNMlAsT0FBTyxHQUFHLENBQUMsR0FBR25YLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUM7RUFDbkMsTUFBTXFZLFVBQVUsR0FBRyxJQUFJclgsR0FBRyxDQUFDbkQsb0JBQW9CLENBQUM2SyxLQUFLLENBQUMsQ0FBQ2pELEdBQUcsQ0FBQ3hHLEtBQUssSUFBSVksT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDdEcsTUFBTTRXLFFBQVEsR0FBRyxJQUFJL1UsR0FBRyxDQUFTLENBQUM7RUFDbEMsTUFBTW9jLE9BQXNCLEdBQUcsRUFBRTtFQUNqQyxNQUFNamMsU0FBUyxHQUFHLENBQUMsR0FBR0MscUJBQXFCLENBQUN0QixLQUFLLENBQUMsQ0FBQyxDQUFDMkYsR0FBRyxDQUFDekYsS0FBSyxJQUFJRCxPQUFPLENBQUNELEtBQUssRUFBRUUsS0FBSyxDQUFDLENBQUM7RUFDdkYsS0FBSyxNQUFNeUIsU0FBUyxJQUFJeU8sR0FBRyxDQUFDcEMsT0FBTyxDQUFDM00sU0FBUyxDQUFDK1UsT0FBTyxDQUFDckMsUUFBUSxJQUFJMEMsb0JBQW9CLENBQUM5USxHQUFHLENBQUNpUixTQUFTLEtBQUs7SUFBRTdDLFFBQVE7SUFBRTZDO0VBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUU7SUFBQSxJQUFBMkcsVUFBQTtJQUNwSSxJQUFJRCxPQUFPLENBQUN6WixNQUFNLEtBQUtzVCxPQUFPLEVBQUU7SUFDaEMsTUFBTU4sS0FBSyxHQUFHO01BQUV6WCxDQUFDLEVBQUV1QyxTQUFTLENBQUNvUyxRQUFRLENBQUMzVSxDQUFDLEdBQUd1QyxTQUFTLENBQUNpVixTQUFTLENBQUN4WCxDQUFDO01BQUVDLENBQUMsRUFBRXNDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzFVLENBQUMsR0FBR3NDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3ZYO0lBQUUsQ0FBQztJQUNsSCxNQUFNeVgsT0FBTyxHQUFHLEVBQWE7SUFDN0IsS0FBSyxJQUFJQyxPQUFPLEdBQUcsQ0FBQyxFQUFFQSxPQUFPLEdBQUcsQ0FBQyxFQUFFQSxPQUFPLEVBQUUsRUFBRSxLQUFLLElBQUlsQyxPQUFPLEdBQUcsQ0FBQyxDQUFDLEVBQUVBLE9BQU8sSUFBSSxDQUFDLEVBQUVBLE9BQU8sRUFBRSxFQUFFaUMsT0FBTyxDQUFDOVMsSUFBSSxDQUFDO01BQUU1RSxDQUFDLEVBQUV1QyxTQUFTLENBQUNvUyxRQUFRLENBQUMzVSxDQUFDLEdBQUd1QyxTQUFTLENBQUNpVixTQUFTLENBQUN4WCxDQUFDLEdBQUcyWCxPQUFPLEdBQUdwVixTQUFTLENBQUNpVixTQUFTLENBQUNGLEtBQUssQ0FBQ3RYLENBQUMsR0FBR3lWLE9BQU87TUFBRXhWLENBQUMsRUFBRXNDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzFVLENBQUMsR0FBR3NDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3ZYLENBQUMsR0FBRzBYLE9BQU8sR0FBR3BWLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ0YsS0FBSyxDQUFDclgsQ0FBQyxHQUFHd1Y7SUFBUSxDQUFDLENBQUM7SUFDcFQsTUFBTThELE9BQU8sR0FBRyxDQUFDOUIsS0FBSyxFQUFFLEdBQUdDLE9BQU8sQ0FBQztJQUNuQyxJQUFJLEVBQUF5RyxVQUFBLEdBQUFuZCxPQUFPLENBQUNKLEtBQUssRUFBRTZXLEtBQUssQ0FBQ3pYLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsQ0FBQyxjQUFBa2UsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0N4ZSxJQUFJLE1BQUssV0FBVyxJQUFJNFosT0FBTyxDQUFDeFIsSUFBSSxDQUFDaEksS0FBSztNQUFBLElBQUFxZSxVQUFBO01BQUEsT0FBSSxDQUFDcGQsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxJQUFJLEVBQUFtZSxVQUFBLEdBQUFwZCxPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUFtZSxVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ3plLElBQUksTUFBSyxXQUFXLElBQUl3WixVQUFVLENBQUNqVyxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsSUFBSTRXLFFBQVEsQ0FBQzNULEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQztJQUFBLEVBQUMsRUFBRTtJQUN4UixNQUFNeWEsU0FBUyxHQUFHd0QsT0FBTyxDQUFDelosTUFBTSxHQUFHLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQztJQUM1QyxNQUFNMFQsV0FBVyxHQUFHVCxPQUFPLENBQUM1USxJQUFJLENBQUNsRyxLQUFLLENBQUM4VyxPQUFPLENBQUNqVCxNQUFNLEdBQUcsQ0FBQyxDQUFDLENBQUU7SUFDNUQsTUFBTWlOLE1BQU0sR0FBRztNQUFFL08sRUFBRSxFQUFFdWIsT0FBTyxDQUFDelosTUFBTSxHQUFHLENBQUMsR0FBRyxZQUFZLEdBQUcsUUFBUTtNQUFFekUsQ0FBQyxFQUFFbVksV0FBVyxDQUFDblksQ0FBQztNQUFFQyxDQUFDLEVBQUVrWSxXQUFXLENBQUNsWSxDQUFDO01BQUVtWSxLQUFLLEVBQUUsQ0FBQztNQUFFQyxZQUFZLEVBQUU7SUFBSyxDQUFDO0lBQ3JJLE1BQU1nRyxTQUFTLEdBQUdyZCxPQUFPLENBQUNKLEtBQUssRUFBRTZXLEtBQUssQ0FBQ3pYLENBQUMsRUFBRXlYLEtBQUssQ0FBQ3hYLENBQUMsQ0FBRTtJQUNuRG9lLFNBQVMsQ0FBQzFlLElBQUksR0FBRyxNQUFNO0lBQ3ZCMGUsU0FBUyxDQUFDM0QsU0FBUyxHQUFHQSxTQUFTO0lBQy9CaEQsT0FBTyxDQUFDNUcsT0FBTyxDQUFDL1EsS0FBSyxJQUFJO01BQUUsTUFBTUwsSUFBSSxHQUFHc0IsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBRTtNQUFFUCxJQUFJLENBQUNDLElBQUksR0FBRyxPQUFPO01BQUVELElBQUksQ0FBQ2diLFNBQVMsR0FBR0EsU0FBUztNQUFFN0QsUUFBUSxDQUFDelIsR0FBRyxDQUFDekUsT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDO0lBQUMsQ0FBQyxDQUFDO0lBQzdLNFcsUUFBUSxDQUFDelIsR0FBRyxDQUFDekUsT0FBTyxDQUFDQyxLQUFLLEVBQUU2VyxLQUFLLENBQUN6WCxDQUFDLEVBQUV5WCxLQUFLLENBQUN4WCxDQUFDLENBQUMsQ0FBQztJQUM5Q1csS0FBSyxDQUFDbUosS0FBSyxDQUFDbkYsSUFBSSxDQUFDOE0sTUFBTSxDQUFDO0lBQ3hCd00sT0FBTyxDQUFDdFosSUFBSSxDQUFDO01BQUVqQyxFQUFFLEVBQUUsZ0JBQWdCL0IsS0FBSyxDQUFDK0gsSUFBSSxJQUFJdVYsT0FBTyxDQUFDelosTUFBTSxFQUFFO01BQUU5RSxJQUFJLEVBQUUsY0FBYztNQUFFZ1YsUUFBUSxFQUFFO1FBQUUsR0FBR3BTLFNBQVMsQ0FBQ29TO01BQVMsQ0FBQztNQUFFOEMsS0FBSztNQUFFQyxPQUFPO01BQUVoRyxNQUFNO01BQUVnSjtJQUFVLENBQUMsQ0FBQztFQUNwSztFQUNBLElBQUl3RCxPQUFPLENBQUN6WixNQUFNLEtBQUtzVCxPQUFPLEVBQUUsTUFBTSxJQUFJNVAsS0FBSyxDQUFDLDZDQUE2QzRQLE9BQU8sV0FBV21HLE9BQU8sQ0FBQ3paLE1BQU0sRUFBRSxDQUFDO0VBQ2hJN0QsS0FBSyxDQUFDcVgsVUFBVSxHQUFHaUcsT0FBTztBQUM1QixDQUFDO0FBRUQsTUFBTXpSLG1CQUFtQixHQUFHQSxDQUFDN0wsS0FBWSxFQUFFNEksS0FBdUIsRUFBRXdILEdBQVEsS0FBVztFQUNyRixJQUFJcFEsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFFBQVEsRUFBRTtFQUM5QixNQUFNeEIsU0FBUyxHQUFHaEcsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQztFQUNqQyxNQUFNaVgsT0FBTyxHQUFHLENBQUMsR0FBR25SLFNBQVM7RUFDN0IsTUFBTXVTLFVBQVUsR0FBRyxJQUFJclgsR0FBRyxDQUFDbkQsb0JBQW9CLENBQUM2SyxLQUFLLENBQUMsQ0FBQ2pELEdBQUcsQ0FBQ3hHLEtBQUssSUFBSVksT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDdEcsTUFBTTRXLFFBQVEsR0FBRyxJQUFJL1UsR0FBRyxDQUFTLENBQUM7RUFDbEMsTUFBTXdjLE1BQXFCLEdBQUcsRUFBRTtFQUNoQyxNQUFNcmMsU0FBUyxHQUFHLENBQUMsR0FBR0MscUJBQXFCLENBQUN0QixLQUFLLENBQUMsQ0FBQyxDQUFDMkYsR0FBRyxDQUFDekYsS0FBSyxJQUFJRCxPQUFPLENBQUNELEtBQUssRUFBRUUsS0FBSyxDQUFDLENBQUM7RUFDdkYsS0FBSyxNQUFNeUIsU0FBUyxJQUFJeU8sR0FBRyxDQUFDcEMsT0FBTyxDQUFDM00sU0FBUyxDQUFDK1UsT0FBTyxDQUFDckMsUUFBUSxJQUFJMEMsb0JBQW9CLENBQUM5USxHQUFHLENBQUNpUixTQUFTLEtBQUs7SUFBRTdDLFFBQVE7SUFBRTZDO0VBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUU7SUFDcEksSUFBSThHLE1BQU0sQ0FBQzdaLE1BQU0sS0FBS3NULE9BQU8sRUFBRTtJQUMvQixNQUFNM0MsS0FBSyxHQUFHLENBQUMsR0FBR3hPLFNBQVM7SUFDM0IsTUFBTXlDLEtBQUssR0FBRyxDQUFDLEdBQUcsQ0FBQ2lWLE1BQU0sQ0FBQzdaLE1BQU0sR0FBR21DLFNBQVMsSUFBSSxDQUFDLEdBQUcsQ0FBQztJQUNyRCxNQUFNd0ksTUFBTSxHQUFHdEksSUFBSSxDQUFDbEcsS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUMsQ0FBQztJQUNwQyxNQUFNb08sS0FBSyxHQUFHO01BQUV6WCxDQUFDLEVBQUV1QyxTQUFTLENBQUNvUyxRQUFRLENBQUMzVSxDQUFDLEdBQUd1QyxTQUFTLENBQUNpVixTQUFTLENBQUN4WCxDQUFDO01BQUVDLENBQUMsRUFBRXNDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzFVLENBQUMsR0FBR3NDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3ZYO0lBQUUsQ0FBQztJQUNsSCxNQUFNeVgsT0FBTyxHQUFHLEVBQWE7SUFDN0IsS0FBSyxJQUFJQyxPQUFPLEdBQUcsQ0FBQyxFQUFFQSxPQUFPLEdBQUd2QyxLQUFLLEdBQUcsQ0FBQyxFQUFFdUMsT0FBTyxFQUFFLEVBQUUsS0FBSyxJQUFJbEMsT0FBTyxHQUFHLENBQUNyRyxNQUFNLEVBQUVxRyxPQUFPLElBQUlyRyxNQUFNLEVBQUVxRyxPQUFPLEVBQUUsRUFBRWlDLE9BQU8sQ0FBQzlTLElBQUksQ0FBQztNQUFFNUUsQ0FBQyxFQUFFdUMsU0FBUyxDQUFDb1MsUUFBUSxDQUFDM1UsQ0FBQyxHQUFHdUMsU0FBUyxDQUFDaVYsU0FBUyxDQUFDeFgsQ0FBQyxHQUFHMlgsT0FBTyxHQUFHcFYsU0FBUyxDQUFDaVYsU0FBUyxDQUFDRixLQUFLLENBQUN0WCxDQUFDLEdBQUd5VixPQUFPO01BQUV4VixDQUFDLEVBQUVzQyxTQUFTLENBQUNvUyxRQUFRLENBQUMxVSxDQUFDLEdBQUdzQyxTQUFTLENBQUNpVixTQUFTLENBQUN2WCxDQUFDLEdBQUcwWCxPQUFPLEdBQUdwVixTQUFTLENBQUNpVixTQUFTLENBQUNGLEtBQUssQ0FBQ3JYLENBQUMsR0FBR3dWO0lBQVEsQ0FBQyxDQUFDO0lBQ3RVLE1BQU04SSxNQUFNLEdBQUc7TUFBRXZlLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzNVLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3hYLENBQUMsSUFBSW9WLEtBQUssR0FBRyxDQUFDLENBQUM7TUFBRW5WLENBQUMsRUFBRXNDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzFVLENBQUMsR0FBR3NDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3ZYLENBQUMsSUFBSW1WLEtBQUssR0FBRyxDQUFDO0lBQUUsQ0FBQztJQUMvSSxNQUFNb0osY0FBYyxHQUFHO01BQUV4ZSxDQUFDLEVBQUV1QyxTQUFTLENBQUNvUyxRQUFRLENBQUMzVSxDQUFDLEdBQUd1QyxTQUFTLENBQUNpVixTQUFTLENBQUN4WCxDQUFDLElBQUlvVixLQUFLLEdBQUcsQ0FBQyxDQUFDO01BQUVuVixDQUFDLEVBQUVzQyxTQUFTLENBQUNvUyxRQUFRLENBQUMxVSxDQUFDLEdBQUdzQyxTQUFTLENBQUNpVixTQUFTLENBQUN2WCxDQUFDLElBQUltVixLQUFLLEdBQUcsQ0FBQztJQUFFLENBQUM7SUFDdkosTUFBTXlDLGNBQWMsR0FBRyxJQUFJL1YsR0FBRyxDQUFDNFYsT0FBTyxDQUFDblIsR0FBRyxDQUFDeEcsS0FBSyxJQUFJWSxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsQ0FBQztJQUN0RixNQUFNbVosT0FBTyxHQUFHMUIsT0FBTyxDQUFDVixPQUFPLENBQUNqWCxLQUFLLElBQUlvRCxXQUFXLENBQUNvRCxHQUFHLENBQUMsQ0FBQyxDQUFDdkcsQ0FBQyxFQUFFQyxDQUFDLENBQUMsTUFBTTtNQUFFRCxDQUFDLEVBQUVELEtBQUssQ0FBQ0MsQ0FBQyxHQUFHQSxDQUFDO01BQUVDLENBQUMsRUFBRUYsS0FBSyxDQUFDRSxDQUFDLEdBQUdBO0lBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDdUcsTUFBTSxDQUFDekcsS0FBSyxJQUFJLENBQUM4WCxjQUFjLENBQUMzVSxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsS0FBS0YsS0FBSyxDQUFDQyxDQUFDLEtBQUt5WCxLQUFLLENBQUN6WCxDQUFDLElBQUlELEtBQUssQ0FBQ0UsQ0FBQyxLQUFLd1gsS0FBSyxDQUFDeFgsQ0FBQyxDQUFDLEtBQUtGLEtBQUssQ0FBQ0MsQ0FBQyxLQUFLdWUsTUFBTSxDQUFDdmUsQ0FBQyxJQUFJRCxLQUFLLENBQUNFLENBQUMsS0FBS3NlLE1BQU0sQ0FBQ3RlLENBQUMsQ0FBQyxDQUFDLENBQUN1RyxNQUFNLENBQUMsQ0FBQ3pHLEtBQUssRUFBRWUsS0FBSyxFQUFFd1QsTUFBTSxLQUFLQSxNQUFNLENBQUMrRSxTQUFTLENBQUNDLEtBQUssSUFBSUEsS0FBSyxDQUFDdFosQ0FBQyxLQUFLRCxLQUFLLENBQUNDLENBQUMsSUFBSXNaLEtBQUssQ0FBQ3JaLENBQUMsS0FBS0YsS0FBSyxDQUFDRSxDQUFDLENBQUMsS0FBS2EsS0FBSyxDQUFDO0lBQ3RZLE1BQU15WSxPQUFPLEdBQUcsQ0FBQzlCLEtBQUssRUFBRThHLE1BQU0sRUFBRSxHQUFHN0csT0FBTyxFQUFFLEdBQUcwQixPQUFPLENBQUM7SUFDdkQsTUFBTXFGLGVBQWUsR0FBRyxDQUFDbGMsU0FBUyxDQUFDb1MsUUFBUSxFQUFFNkosY0FBYyxFQUFFLEdBQUdqRixPQUFPLENBQUM7SUFDeEUsSUFBSWtGLGVBQWUsQ0FBQzFXLElBQUksQ0FBQ2hJLEtBQUs7TUFBQSxJQUFBMmUsVUFBQTtNQUFBLE9BQUksQ0FBQzFkLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsSUFBSSxFQUFBeWUsVUFBQSxHQUFBMWQsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBeWUsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0MvZSxJQUFJLE1BQUssT0FBTyxJQUFJd1osVUFBVSxDQUFDalcsR0FBRyxDQUFDdkMsT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLElBQUk0VyxRQUFRLENBQUMzVCxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUM7SUFBQSxFQUFDLEVBQUU7SUFDbE8sTUFBTXdaLE1BQU0sR0FBR0YsT0FBTyxDQUFDaFQsR0FBRyxDQUFDeEcsS0FBSyxLQUFLO01BQUVBLEtBQUs7TUFBRUosSUFBSSxFQUFFcUIsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFFTjtJQUFLLENBQUMsQ0FBQyxDQUFDO0lBQzlGeVosT0FBTyxDQUFDdEksT0FBTyxDQUFDL1EsS0FBSyxJQUFJNEssT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUUsTUFBTSxDQUFDLENBQUM7SUFDbEUwSyxPQUFPLENBQUMvSixLQUFLLEVBQUU2VyxLQUFLLENBQUN6WCxDQUFDLEVBQUV5WCxLQUFLLENBQUN4WCxDQUFDLEVBQUUsV0FBVyxDQUFDO0lBQzdDMEssT0FBTyxDQUFDL0osS0FBSyxFQUFFMmQsTUFBTSxDQUFDdmUsQ0FBQyxFQUFFdWUsTUFBTSxDQUFDdGUsQ0FBQyxFQUFFLFdBQVcsQ0FBQztJQUMvQyxJQUFJLENBQUNzRixlQUFlLENBQUMzRSxLQUFLLEVBQUVBLEtBQUssQ0FBQytDLEtBQUssRUFBRS9DLEtBQUssQ0FBQ3dGLElBQUksQ0FBQyxFQUFFO01BQUVxVCxNQUFNLENBQUMzSSxPQUFPLENBQUMsQ0FBQztRQUFFL1EsS0FBSztRQUFFSjtNQUFLLENBQUMsS0FBS2dMLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFTixJQUFJLENBQUMsQ0FBQztNQUFFO0lBQVM7SUFDOUkrWCxPQUFPLENBQUM1RyxPQUFPLENBQUMsQ0FBQy9RLEtBQUssRUFBRWUsS0FBSyxLQUFLNkosT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUVhLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLFNBQVMsR0FBR0EsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEdBQUcsWUFBWSxHQUFHQSxLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsR0FBRyxXQUFXLEdBQUcsT0FBTyxDQUFDLENBQUM7SUFDM0ssTUFBTXFYLFdBQVcsR0FBR1QsT0FBTyxDQUFDNVEsSUFBSSxDQUFDbEcsS0FBSyxDQUFDOFcsT0FBTyxDQUFDalQsTUFBTSxHQUFHLENBQUMsQ0FBQyxDQUFFO0lBQzVELE1BQU1pTixNQUFNLEdBQUc7TUFBRS9PLEVBQUUsRUFBRTJiLE1BQU0sQ0FBQzdaLE1BQU0sR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLFNBQVMsR0FBRzZaLE1BQU0sQ0FBQzdaLE1BQU0sR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLFlBQVksR0FBRyxZQUFZO01BQUV6RSxDQUFDLEVBQUVtWSxXQUFXLENBQUNuWSxDQUFDO01BQUVDLENBQUMsRUFBRWtZLFdBQVcsQ0FBQ2xZLENBQUM7TUFBRW1ZLEtBQUssRUFBRSxDQUFDO01BQUVDLFlBQVksRUFBRTtJQUFLLENBQUM7SUFDcExvRyxlQUFlLENBQUMzTixPQUFPLENBQUMvUSxLQUFLLElBQUk4VyxRQUFRLENBQUN6UixHQUFHLENBQUN6RSxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsQ0FBQztJQUNoRlcsS0FBSyxDQUFDbUosS0FBSyxDQUFDbkYsSUFBSSxDQUFDOE0sTUFBTSxDQUFDO0lBQ3hCNE0sTUFBTSxDQUFDMVosSUFBSSxDQUFDO01BQUVqQyxFQUFFLEVBQUUsZ0JBQWdCL0IsS0FBSyxDQUFDK0gsSUFBSSxJQUFJMlYsTUFBTSxDQUFDN1osTUFBTSxFQUFFO01BQUU5RSxJQUFJLEVBQUUsY0FBYztNQUFFZ1YsUUFBUSxFQUFFO1FBQUUsR0FBR3BTLFNBQVMsQ0FBQ29TO01BQVMsQ0FBQztNQUFFOEMsS0FBSztNQUFFa0gsVUFBVSxFQUFFLENBQUM7UUFBRSxHQUFHcGMsU0FBUyxDQUFDb1M7TUFBUyxDQUFDLEVBQUU2SixjQUFjLENBQUM7TUFBRUksT0FBTyxFQUFFLENBQUNuSCxLQUFLLEVBQUU4RyxNQUFNLENBQUM7TUFBRTdHLE9BQU87TUFBRWhHLE1BQU07TUFBRTBEO0lBQU0sQ0FBQyxDQUFDO0VBQ2pQO0VBQ0EsSUFBSWtKLE1BQU0sQ0FBQzdaLE1BQU0sS0FBS3NULE9BQU8sRUFBRSxNQUFNLElBQUk1UCxLQUFLLENBQUMsNENBQTRDNFAsT0FBTyxXQUFXdUcsTUFBTSxDQUFDN1osTUFBTSxFQUFFLENBQUM7RUFDN0gsTUFBTTZWLElBQUksR0FBRzlRLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQzRWLElBQUksSUFBSUEsSUFBSSxDQUFDMkMsS0FBSyxDQUFDelosUUFBUSxDQUFDLE1BQU0sQ0FBQyxDQUFDO0VBQ2xFLE1BQU0yYSxNQUFNLEdBQUd0UixLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUM0VixJQUFJLElBQUlBLElBQUksQ0FBQzJDLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxRQUFRLENBQUMsSUFBSThXLElBQUksQ0FBQzJDLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxVQUFVLENBQUMsQ0FBQztFQUN6RyxJQUFJLENBQUNtYSxJQUFJLElBQUksQ0FBQ1EsTUFBTSxFQUFFLE1BQU0sSUFBSTNTLEtBQUssQ0FBQywrQkFBK0JxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ3RGLE1BQU04VixXQUFXLEdBQUd2RSxJQUFJLENBQUNULEtBQUssQ0FBQ3JULE1BQU0sQ0FBQ3pHLEtBQUs7SUFBQSxJQUFBK2UsVUFBQTtJQUFBLE9BQUksRUFBQUEsVUFBQSxHQUFBOWQsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBNmUsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0NuZixJQUFJLE1BQUssWUFBWTtFQUFBLEVBQUM7RUFDdkcsTUFBTW9mLFVBQVUsR0FBR2pFLE1BQU0sQ0FBQ2pCLEtBQUssQ0FBQ3JULE1BQU0sQ0FBQ3pHLEtBQUs7SUFBQSxJQUFBaWYsVUFBQTtJQUFBLE9BQUksRUFBQUEsVUFBQSxHQUFBaGUsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBK2UsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0NyZixJQUFJLE1BQUssV0FBVztFQUFBLEVBQUM7RUFDdkcsSUFBSWtmLFdBQVcsQ0FBQ3BhLE1BQU0sR0FBRyxDQUFDLElBQUlzYSxVQUFVLENBQUN0YSxNQUFNLEdBQUcsQ0FBQyxFQUFFLE1BQU0sSUFBSTBELEtBQUssQ0FBQyw2QkFBNkJxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ25ILE1BQU1rVyxXQUFXLEdBQUdwVixLQUFLLENBQUNDLElBQUksQ0FBQztJQUFFckYsTUFBTSxFQUFFc1Q7RUFBUSxDQUFDLEVBQUUsQ0FBQ2IsQ0FBQyxFQUFFcFcsS0FBSyxLQUFLaWUsVUFBVSxDQUFDbkosS0FBSyxDQUFDOU8sSUFBSSxDQUFDRSxHQUFHLENBQUMrWCxVQUFVLENBQUN0YSxNQUFNLEdBQUcsQ0FBQyxFQUFFM0QsS0FBSyxHQUFHLENBQUMsQ0FBQyxFQUFFZ0csSUFBSSxDQUFDRSxHQUFHLENBQUMrWCxVQUFVLENBQUN0YSxNQUFNLEVBQUUzRCxLQUFLLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQzBGLE1BQU0sQ0FBQzRRLEtBQUssSUFBSUEsS0FBSyxDQUFDM1MsTUFBTSxLQUFLLENBQUMsQ0FBQztFQUMvTSxJQUFJd2EsV0FBVyxDQUFDeGEsTUFBTSxLQUFLc1QsT0FBTyxFQUFFLE1BQU0sSUFBSTVQLEtBQUssQ0FBQyxvQ0FBb0NxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ3pHLE1BQU1tVyxRQUFRLEdBQUdMLFdBQVcsQ0FBQ3JZLE1BQU0sQ0FBQyxDQUFDMFEsQ0FBQyxFQUFFcFcsS0FBSyxLQUFLQSxLQUFLLEdBQUdnRyxJQUFJLENBQUNDLEdBQUcsQ0FBQyxDQUFDLEVBQUVELElBQUksQ0FBQ2xHLEtBQUssQ0FBQ2llLFdBQVcsQ0FBQ3BhLE1BQU0sR0FBR3NULE9BQU8sQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUNuQyxLQUFLLENBQUMsQ0FBQyxFQUFFbUMsT0FBTyxDQUFDO0VBQ3hJblgsS0FBSyxDQUFDcVgsVUFBVSxHQUFHcUcsTUFBTTtFQUN6QjFkLEtBQUssQ0FBQ3VlLFlBQVksR0FBRztJQUFFQyxNQUFNLEVBQUV4ZSxLQUFLLENBQUNLLEtBQUssQ0FBQytWLE9BQU8sQ0FBQyxDQUFDdFgsSUFBSSxFQUFFb0IsS0FBSyxLQUFLcEIsSUFBSSxDQUFDQyxJQUFJLEtBQUssT0FBTyxHQUFHLENBQUNrQixPQUFPLENBQUNELEtBQUssRUFBRUUsS0FBSyxDQUFDLENBQUMsR0FBRyxFQUFFLENBQUMsQ0FBQzhVLEtBQUssQ0FBQyxDQUFDLEVBQUUsRUFBRSxHQUFHaFAsU0FBUyxHQUFHLENBQUMsQ0FBQztJQUFFcVksV0FBVztJQUFFQztFQUFTLENBQXdCO0FBQ3hNLENBQUM7QUFFRCxNQUFNeFMsa0JBQWtCLEdBQUdBLENBQUM5TCxLQUFZLEVBQUU0SSxLQUF1QixFQUFFd0gsR0FBUSxLQUFXO0VBQ3BGLElBQUlwUSxLQUFLLENBQUN3SCxLQUFLLEtBQUssV0FBVyxFQUFFO0VBQ2pDLE1BQU0yUCxPQUFPLEdBQUcsQ0FBQyxHQUFHblgsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQztFQUNuQyxNQUFNcVksVUFBVSxHQUFHLElBQUlyWCxHQUFHLENBQUNuRCxvQkFBb0IsQ0FBQzZLLEtBQUssQ0FBQyxDQUFDakQsR0FBRyxDQUFDeEcsS0FBSyxJQUFJWSxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUN0RyxNQUFNb2YsT0FBcUIsR0FBRyxFQUFFO0VBQ2hDLE1BQU1ySCxVQUFVLEdBQUdoSCxHQUFHLENBQUNwQyxPQUFPLENBQUNoTyxLQUFLLENBQUNLLEtBQUssQ0FBQytWLE9BQU8sQ0FBQyxDQUFDdFgsSUFBSSxFQUFFb0IsS0FBSyxLQUFLcEIsSUFBSSxDQUFDQyxJQUFJLEtBQUssT0FBTyxHQUFHLENBQUNrQixPQUFPLENBQUNELEtBQUssRUFBRUUsS0FBSyxDQUFDLENBQUMsR0FBRyxFQUFFLENBQUMsQ0FBQztFQUMxSCxLQUFLLE1BQU13ZSxNQUFNLElBQUl0SCxVQUFVLEVBQUU7SUFDL0IsSUFBSXFILE9BQU8sQ0FBQzVhLE1BQU0sS0FBS3NULE9BQU8sRUFBRTtJQUNoQyxNQUFNOEIsS0FBSyxHQUFHLENBQUM7TUFBRTdaLENBQUMsRUFBRXNmLE1BQU0sQ0FBQ3RmLENBQUM7TUFBRUMsQ0FBQyxFQUFFcWYsTUFBTSxDQUFDcmY7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFc2YsTUFBTSxDQUFDdGYsQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFcWYsTUFBTSxDQUFDcmY7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFc2YsTUFBTSxDQUFDdGYsQ0FBQztNQUFFQyxDQUFDLEVBQUVxZixNQUFNLENBQUNyZixDQUFDLEdBQUc7SUFBRSxDQUFDLEVBQUU7TUFBRUQsQ0FBQyxFQUFFc2YsTUFBTSxDQUFDdGYsQ0FBQyxHQUFHLENBQUM7TUFBRUMsQ0FBQyxFQUFFcWYsTUFBTSxDQUFDcmYsQ0FBQyxHQUFHO0lBQUUsQ0FBQyxDQUFDO0lBQ3RKLElBQUltVCxRQUFRLENBQUNrTSxNQUFNLEVBQUUxZSxLQUFLLENBQUMrQyxLQUFLLENBQUMsR0FBRyxDQUFDLElBQUlrVyxLQUFLLENBQUM5UixJQUFJLENBQUNoSSxLQUFLO01BQUEsSUFBQXdmLFVBQUE7TUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQXZlLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQXNmLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDNWYsSUFBSSxNQUFLLE9BQU8sSUFBSXdaLFVBQVUsQ0FBQ2pXLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxJQUFJb2YsT0FBTyxDQUFDdFgsSUFBSSxDQUFDeVgsTUFBTSxJQUFJQSxNQUFNLENBQUMzRixLQUFLLENBQUM5UixJQUFJLENBQUM0VSxJQUFJLElBQUlBLElBQUksQ0FBQzNjLENBQUMsS0FBS0QsS0FBSyxDQUFDQyxDQUFDLElBQUkyYyxJQUFJLENBQUMxYyxDQUFDLEtBQUtGLEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUM7SUFBQSxFQUFDLEVBQUU7SUFDclE0WixLQUFLLENBQUMvSSxPQUFPLENBQUMvUSxLQUFLLElBQUk0SyxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRSxZQUFZLENBQUMsQ0FBQztJQUN0RW9mLE9BQU8sQ0FBQ3phLElBQUksQ0FBQztNQUFFakMsRUFBRSxFQUFFLGVBQWUvQixLQUFLLENBQUMrSCxJQUFJLElBQUkwVyxPQUFPLENBQUM1YSxNQUFNLEVBQUU7TUFBRTZhLE1BQU07TUFBRXpGLEtBQUs7TUFBRTRGLFFBQVEsRUFBRTtJQUFNLENBQUMsQ0FBQztFQUNyRztFQUNBLElBQUlKLE9BQU8sQ0FBQzVhLE1BQU0sS0FBS3NULE9BQU8sRUFBRSxNQUFNLElBQUk1UCxLQUFLLENBQUMsMkNBQTJDNFAsT0FBTyxXQUFXc0gsT0FBTyxDQUFDNWEsTUFBTSxFQUFFLENBQUM7RUFDOUg3RCxLQUFLLENBQUM4ZSxXQUFXLEdBQUdMLE9BQU87QUFDN0IsQ0FBQztBQUVELE1BQU0xUyxpQkFBaUIsR0FBR0EsQ0FBQy9MLEtBQVksRUFBRTRJLEtBQXVCLEVBQUV3SCxHQUFRLEtBQVc7RUFDbkYsSUFBSXBRLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxnQkFBZ0IsRUFBRTtFQUN0QyxNQUFNMlAsT0FBTyxHQUFHLENBQUMsR0FBR25YLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUM7RUFDbkMsTUFBTXFZLFVBQVUsR0FBRyxJQUFJclgsR0FBRyxDQUFDbkQsb0JBQW9CLENBQUM2SyxLQUFLLENBQUMsQ0FBQ2pELEdBQUcsQ0FBQ3hHLEtBQUssSUFBSVksT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDdEcsTUFBTXlZLEtBQWtCLEdBQUcsRUFBRTtFQUM3QixNQUFNN0IsUUFBUSxHQUFHLElBQUkvVSxHQUFHLENBQVMsQ0FBQztFQUNsQyxNQUFNRyxTQUFTLEdBQUcsQ0FBQyxHQUFHQyxxQkFBcUIsQ0FBQ3RCLEtBQUssQ0FBQyxDQUFDLENBQUMyRixHQUFHLENBQUN6RixLQUFLLElBQUlELE9BQU8sQ0FBQ0QsS0FBSyxFQUFFRSxLQUFLLENBQUMsQ0FBQztFQUN2RixLQUFLLE1BQU15QixTQUFTLElBQUl5TyxHQUFHLENBQUNwQyxPQUFPLENBQUMzTSxTQUFTLENBQUMrVSxPQUFPLENBQUNyQyxRQUFRLElBQUkwQyxvQkFBb0IsQ0FBQzlRLEdBQUcsQ0FBQ2lSLFNBQVMsS0FBSztJQUFFN0MsUUFBUTtJQUFFNkM7RUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRTtJQUNwSSxJQUFJa0IsS0FBSyxDQUFDalUsTUFBTSxLQUFLc1QsT0FBTyxFQUFFO0lBQzlCLE1BQU0zQyxLQUFLLEdBQUcsQ0FBQyxHQUFHeFUsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQztJQUNqQyxNQUFNNFcsT0FBTyxHQUFHLEVBQWE7SUFDN0IsS0FBSyxJQUFJQyxPQUFPLEdBQUcsQ0FBQyxFQUFFQSxPQUFPLEdBQUd2QyxLQUFLLEdBQUcsQ0FBQyxFQUFFdUMsT0FBTyxFQUFFLEVBQUUsS0FBSyxJQUFJbEMsT0FBTyxHQUFHLENBQUMsQ0FBQyxFQUFFQSxPQUFPLElBQUksQ0FBQyxFQUFFQSxPQUFPLEVBQUUsRUFBRWlDLE9BQU8sQ0FBQzlTLElBQUksQ0FBQztNQUFFNUUsQ0FBQyxFQUFFdUMsU0FBUyxDQUFDb1MsUUFBUSxDQUFDM1UsQ0FBQyxHQUFHdUMsU0FBUyxDQUFDaVYsU0FBUyxDQUFDeFgsQ0FBQyxHQUFHMlgsT0FBTyxHQUFHcFYsU0FBUyxDQUFDaVYsU0FBUyxDQUFDRixLQUFLLENBQUN0WCxDQUFDLEdBQUd5VixPQUFPO01BQUV4VixDQUFDLEVBQUVzQyxTQUFTLENBQUNvUyxRQUFRLENBQUMxVSxDQUFDLEdBQUdzQyxTQUFTLENBQUNpVixTQUFTLENBQUN2WCxDQUFDLEdBQUcwWCxPQUFPLEdBQUdwVixTQUFTLENBQUNpVixTQUFTLENBQUNGLEtBQUssQ0FBQ3JYLENBQUMsR0FBR3dWO0lBQVEsQ0FBQyxDQUFDO0lBQzVULE1BQU1nQyxLQUFLLEdBQUc7TUFBRXpYLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ29TLFFBQVEsQ0FBQzNVLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ2lWLFNBQVMsQ0FBQ3hYLENBQUM7TUFBRUMsQ0FBQyxFQUFFc0MsU0FBUyxDQUFDb1MsUUFBUSxDQUFDMVUsQ0FBQyxHQUFHc0MsU0FBUyxDQUFDaVYsU0FBUyxDQUFDdlg7SUFBRSxDQUFDO0lBQ2xILE1BQU00WCxjQUFjLEdBQUcsSUFBSS9WLEdBQUcsQ0FBQzRWLE9BQU8sQ0FBQ25SLEdBQUcsQ0FBQ3hHLEtBQUssSUFBSVksT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLENBQUM7SUFDdEYsTUFBTW1aLE9BQU8sR0FBRzFCLE9BQU8sQ0FBQ1YsT0FBTyxDQUFDalgsS0FBSyxJQUFJb0QsV0FBVyxDQUFDb0QsR0FBRyxDQUFDLENBQUMsQ0FBQ3ZHLENBQUMsRUFBRUMsQ0FBQyxDQUFDLE1BQU07TUFBRUQsQ0FBQyxFQUFFRCxLQUFLLENBQUNDLENBQUMsR0FBR0EsQ0FBQztNQUFFQyxDQUFDLEVBQUVGLEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQTtJQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQ3VHLE1BQU0sQ0FBQ3pHLEtBQUssSUFBSSxDQUFDOFgsY0FBYyxDQUFDM1UsR0FBRyxDQUFDdkMsT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLEtBQUtGLEtBQUssQ0FBQ0MsQ0FBQyxLQUFLeVgsS0FBSyxDQUFDelgsQ0FBQyxJQUFJRCxLQUFLLENBQUNFLENBQUMsS0FBS3dYLEtBQUssQ0FBQ3hYLENBQUMsQ0FBQyxDQUFDLENBQUN1RyxNQUFNLENBQUMsQ0FBQ3pHLEtBQUssRUFBRWUsS0FBSyxFQUFFd1QsTUFBTSxLQUFLQSxNQUFNLENBQUMrRSxTQUFTLENBQUNDLEtBQUssSUFBSUEsS0FBSyxDQUFDdFosQ0FBQyxLQUFLRCxLQUFLLENBQUNDLENBQUMsSUFBSXNaLEtBQUssQ0FBQ3JaLENBQUMsS0FBS0YsS0FBSyxDQUFDRSxDQUFDLENBQUMsS0FBS2EsS0FBSyxDQUFDO0lBQ3BWLE1BQU15WSxPQUFPLEdBQUcsQ0FBQzlCLEtBQUssRUFBRSxHQUFHQyxPQUFPLEVBQUUsR0FBRzBCLE9BQU8sQ0FBQztJQUMvQyxJQUFJRyxPQUFPLENBQUN4UixJQUFJLENBQUNoSSxLQUFLO01BQUEsSUFBQTRmLFVBQUE7TUFBQSxPQUFJLENBQUMzZSxPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLElBQUksRUFBQTBmLFVBQUEsR0FBQTNlLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQTBmLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDaGdCLElBQUksTUFBSyxPQUFPLElBQUl3WixVQUFVLENBQUNqVyxHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUMsSUFBSTRXLFFBQVEsQ0FBQzNULEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxJQUFJVyxLQUFLLENBQUNlLEtBQUssQ0FBQ29HLElBQUksQ0FBQ3hFLElBQUksSUFBSUEsSUFBSSxDQUFDdkQsQ0FBQyxLQUFLRCxLQUFLLENBQUNDLENBQUMsSUFBSXVELElBQUksQ0FBQ3RELENBQUMsS0FBS0YsS0FBSyxDQUFDRSxDQUFDLENBQUM7SUFBQSxFQUFDLEVBQUU7SUFDaFMsTUFBTXdaLE1BQU0sR0FBR0YsT0FBTyxDQUFDaFQsR0FBRyxDQUFDeEcsS0FBSyxLQUFLO01BQUVBLEtBQUs7TUFBRUosSUFBSSxFQUFFcUIsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFFTjtJQUFLLENBQUMsQ0FBQyxDQUFDO0lBQzlGeVosT0FBTyxDQUFDdEksT0FBTyxDQUFDL1EsS0FBSyxJQUFJNEssT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUUsTUFBTSxDQUFDLENBQUM7SUFDbEUwSyxPQUFPLENBQUMvSixLQUFLLEVBQUU2VyxLQUFLLENBQUN6WCxDQUFDLEVBQUV5WCxLQUFLLENBQUN4WCxDQUFDLEVBQUUsV0FBVyxDQUFDO0lBQzdDLElBQUksQ0FBQ3NGLGVBQWUsQ0FBQzNFLEtBQUssRUFBRUEsS0FBSyxDQUFDK0MsS0FBSyxFQUFFL0MsS0FBSyxDQUFDd0YsSUFBSSxDQUFDLEVBQUU7TUFBRXFULE1BQU0sQ0FBQzNJLE9BQU8sQ0FBQyxDQUFDO1FBQUUvUSxLQUFLO1FBQUVKO01BQUssQ0FBQyxLQUFLZ0wsT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUVOLElBQUksQ0FBQyxDQUFDO01BQUU7SUFBUztJQUM5SStYLE9BQU8sQ0FBQzVHLE9BQU8sQ0FBQyxDQUFDL1EsS0FBSyxFQUFFZSxLQUFLLEtBQUs2SixPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRWEsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEdBQUcsV0FBVyxHQUFHQSxLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsR0FBRyxLQUFLLEdBQUcsT0FBTyxDQUFDLENBQUM7SUFDdEksTUFBTXFYLFdBQVcsR0FBR1QsT0FBTyxDQUFDNVEsSUFBSSxDQUFDbEcsS0FBSyxDQUFDOFcsT0FBTyxDQUFDalQsTUFBTSxHQUFHLENBQUMsQ0FBQyxDQUFFO0lBQzVELE1BQU1pTixNQUFNLEdBQUc7TUFBRS9PLEVBQUUsRUFBRStWLEtBQUssQ0FBQ2pVLE1BQU0sR0FBRyxDQUFDLEdBQUcsYUFBYSxHQUFHLFlBQVk7TUFBRXpFLENBQUMsRUFBRW1ZLFdBQVcsQ0FBQ25ZLENBQUM7TUFBRUMsQ0FBQyxFQUFFa1ksV0FBVyxDQUFDbFksQ0FBQztNQUFFbVksS0FBSyxFQUFFLENBQUM7TUFBRUMsWUFBWSxFQUFFO0lBQUssQ0FBQztJQUN4SWtCLE9BQU8sQ0FBQ3pJLE9BQU8sQ0FBQy9RLEtBQUssSUFBSThXLFFBQVEsQ0FBQ3pSLEdBQUcsQ0FBQ3pFLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxDQUFDO0lBQ3hFVyxLQUFLLENBQUNtSixLQUFLLENBQUNuRixJQUFJLENBQUM4TSxNQUFNLENBQUM7SUFDeEJnSCxLQUFLLENBQUM5VCxJQUFJLENBQUM7TUFBRWpDLEVBQUUsRUFBRSxjQUFjL0IsS0FBSyxDQUFDK0gsSUFBSSxJQUFJK1AsS0FBSyxDQUFDalUsTUFBTSxFQUFFO01BQUU5RSxJQUFJLEVBQUUsWUFBWTtNQUFFZ1YsUUFBUSxFQUFFO1FBQUUsR0FBR3BTLFNBQVMsQ0FBQ29TO01BQVMsQ0FBQztNQUFFOEMsS0FBSztNQUFFQyxPQUFPO01BQUVoRyxNQUFNO01BQUUwRDtJQUFNLENBQUMsQ0FBQztFQUN4SjtFQUNBLElBQUlzRCxLQUFLLENBQUNqVSxNQUFNLEtBQUtzVCxPQUFPLEVBQUUsTUFBTSxJQUFJNVAsS0FBSyxDQUFDLDBDQUEwQzRQLE9BQU8sV0FBV1csS0FBSyxDQUFDalUsTUFBTSxFQUFFLENBQUM7RUFDekgsTUFBTXFXLE1BQU0sR0FBR3RSLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQzRWLElBQUksSUFBSUEsSUFBSSxDQUFDMkMsS0FBSyxDQUFDelosUUFBUSxDQUFDLFFBQVEsQ0FBQyxJQUFJOFcsSUFBSSxDQUFDMkMsS0FBSyxDQUFDelosUUFBUSxDQUFDLFVBQVUsQ0FBQyxDQUFDO0VBQ3pHLE1BQU1tYSxJQUFJLEdBQUc5USxLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUM0VixJQUFJLElBQUlBLElBQUksQ0FBQzJDLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxNQUFNLENBQUMsQ0FBQztFQUNsRSxJQUFJLENBQUMyYSxNQUFNLElBQUksQ0FBQ1IsSUFBSSxFQUFFLE1BQU0sSUFBSW5TLEtBQUssQ0FBQyx5QkFBeUJxQixLQUFLLENBQUNULFFBQVEsRUFBRSxDQUFDO0VBQ2hGbkksS0FBSyxDQUFDcVgsVUFBVSxHQUFHUyxLQUFLO0VBQ3hCOVgsS0FBSyxDQUFDZ2YsV0FBVyxHQUFHO0lBQUVDLE9BQU8sRUFBRWpmLEtBQUssQ0FBQ0ssS0FBSyxDQUFDK1YsT0FBTyxDQUFDLENBQUN0WCxJQUFJLEVBQUVvQixLQUFLLEtBQUtwQixJQUFJLENBQUNDLElBQUksS0FBSyxLQUFLLEdBQUcsQ0FBQ2tCLE9BQU8sQ0FBQ0QsS0FBSyxFQUFFRSxLQUFLLENBQUMsQ0FBQyxHQUFHLEVBQUUsQ0FBQyxDQUFDOFUsS0FBSyxDQUFDLENBQUMsRUFBRSxFQUFFLEdBQUdoVixLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDO0lBQUVnZixNQUFNLEVBQUUsQ0FBQ2hGLE1BQU0sQ0FBQ2pCLEtBQUssQ0FBQ3JULE1BQU0sQ0FBQ3pHLEtBQUs7TUFBQSxJQUFBZ2dCLFVBQUE7TUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQS9lLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQThmLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDcGdCLElBQUksTUFBSyxXQUFXO0lBQUEsRUFBQyxDQUFDO0lBQUV1ZixRQUFRLEVBQUU1RSxJQUFJLENBQUNULEtBQUssQ0FBQ3JULE1BQU0sQ0FBQ3pHLEtBQUs7TUFBQSxJQUFBaWdCLFVBQUE7TUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQWhmLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQStmLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDcmdCLElBQUksTUFBSyxPQUFPO0lBQUEsRUFBQyxDQUFDaVcsS0FBSyxDQUFDLENBQUMsRUFBRW1DLE9BQU87RUFBRSxDQUF1QjtBQUNoWSxDQUFDO0FBRUQsTUFBTWxNLHlCQUF5QixHQUFHQSxDQUFDakwsS0FBWSxFQUFFNEksS0FBdUIsS0FBVztFQUNqRixJQUFJNUksS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFFBQVEsRUFBRTtFQUM5QixNQUFNa1MsSUFBSSxHQUFHOVEsS0FBSyxDQUFDdU4sS0FBSyxDQUFDMVYsSUFBSSxDQUFDa0IsU0FBUyxJQUFJQSxTQUFTLENBQUNxWCxLQUFLLENBQUN6WixRQUFRLENBQUMsTUFBTSxDQUFDLENBQUM7RUFDNUUsTUFBTTJhLE1BQU0sR0FBR3RSLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLFFBQVEsQ0FBQyxJQUFJb0MsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLFVBQVUsQ0FBQyxDQUFDO0VBQ3hILE1BQU04ZixRQUFRLEdBQUd6VyxLQUFLLENBQUN2QixLQUFLLENBQUM1RyxJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQzVDLElBQUksS0FBSyxnQkFBZ0IsQ0FBQztFQUNuRixJQUFJLENBQUMyYSxJQUFJLElBQUksQ0FBQ1EsTUFBTSxJQUFJLENBQUNtRixRQUFRLEVBQUUsTUFBTSxJQUFJOVgsS0FBSyxDQUFDLGlDQUFpQ3FCLEtBQUssQ0FBQ1QsUUFBUSxFQUFFLENBQUM7RUFDckcsTUFBTWdTLE9BQU8sR0FBSWxCLEtBQXVCLElBQUtBLEtBQUssQ0FBQ2pFLEtBQUssQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQ3BQLE1BQU0sQ0FBQ3pHLEtBQUs7SUFBQSxJQUFBbWdCLFVBQUE7SUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQWxmLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQWlnQixVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ3ZnQixJQUFJLE1BQUssT0FBTztFQUFBLEVBQUM7RUFDbkksTUFBTWtmLFdBQVcsR0FBRzlELE9BQU8sQ0FBQ1QsSUFBSSxDQUFDVCxLQUFLLENBQUM7RUFDdkMsTUFBTWtGLFVBQVUsR0FBR2hFLE9BQU8sQ0FBQ0QsTUFBTSxDQUFDakIsS0FBSyxDQUFDO0VBQ3hDLElBQUlnRixXQUFXLENBQUNwYSxNQUFNLEdBQUcsQ0FBQyxJQUFJc2EsVUFBVSxDQUFDdGEsTUFBTSxHQUFHLENBQUMsRUFBRSxNQUFNLElBQUkwRCxLQUFLLENBQUMsOEJBQThCcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUNwSCxLQUFLLE1BQU1oSixLQUFLLElBQUk4ZSxXQUFXLEVBQUVsVSxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRSxZQUFZLENBQUM7RUFDL0UsS0FBSyxNQUFNRixLQUFLLElBQUlnZixVQUFVLEVBQUVwVSxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRSxXQUFXLENBQUM7RUFDN0UsTUFBTWtnQixLQUFLLEdBQUd0VyxLQUFLLENBQUNDLElBQUksQ0FBQztJQUFFckYsTUFBTSxFQUFFd2IsUUFBUSxDQUFDbk4sU0FBUyxDQUFDekosS0FBSyxHQUFHNFcsUUFBUSxDQUFDbk4sU0FBUyxDQUFDeEo7RUFBTyxDQUFDLEVBQUUsQ0FBQzROLENBQUMsRUFBRXBXLEtBQUssTUFBTTtJQUFFZCxDQUFDLEVBQUVpZ0IsUUFBUSxDQUFDbk4sU0FBUyxDQUFDOVMsQ0FBQyxHQUFHYyxLQUFLLEdBQUdtZixRQUFRLENBQUNuTixTQUFTLENBQUN6SixLQUFLO0lBQUVwSixDQUFDLEVBQUVnZ0IsUUFBUSxDQUFDbk4sU0FBUyxDQUFDN1MsQ0FBQyxHQUFHNkcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDRSxLQUFLLEdBQUdtZixRQUFRLENBQUNuTixTQUFTLENBQUN6SixLQUFLO0VBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQ2hJLElBQUksQ0FBQ3RCLEtBQUs7SUFBQSxJQUFBcWdCLFVBQUE7SUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQXBmLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQW1nQixVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ3pnQixJQUFJLE1BQUssT0FBTztFQUFBLEVBQUM7RUFDcFQsSUFBSSxDQUFDd2dCLEtBQUssRUFBRSxNQUFNLElBQUloWSxLQUFLLENBQUMsbUNBQW1DcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUNoRjRCLE9BQU8sQ0FBQy9KLEtBQUssRUFBRXVmLEtBQUssQ0FBQ25nQixDQUFDLEVBQUVtZ0IsS0FBSyxDQUFDbGdCLENBQUMsRUFBRSxTQUFTLENBQUM7QUFDN0MsQ0FBQztBQUVELE1BQU02TCx3QkFBd0IsR0FBR0EsQ0FBQ2xMLEtBQVksRUFBRTRJLEtBQXVCLEtBQVc7RUFDaEYsSUFBSTVJLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxXQUFXLEVBQUU7RUFDakMsTUFBTWtTLElBQUksR0FBRzlRLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLE1BQU0sQ0FBQyxDQUFDO0VBQzVFLE1BQU0yYSxNQUFNLEdBQUd0UixLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxRQUFRLENBQUMsSUFBSW9DLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxVQUFVLENBQUMsQ0FBQztFQUN4SCxNQUFNa2dCLFVBQVUsR0FBRzdXLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzVHLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDNUMsSUFBSSxLQUFLLGdCQUFnQixDQUFDO0VBQ3JGLElBQUksQ0FBQzJhLElBQUksSUFBSSxDQUFDUSxNQUFNLElBQUksQ0FBQ3VGLFVBQVUsRUFBRSxNQUFNLElBQUlsWSxLQUFLLENBQUMsK0JBQStCcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUNyRyxNQUFNZ1MsT0FBTyxHQUFJbEIsS0FBdUIsSUFBS0EsS0FBSyxDQUFDakUsS0FBSyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDcFAsTUFBTSxDQUFDekcsS0FBSztJQUFBLElBQUF1Z0IsVUFBQTtJQUFBLE9BQUksRUFBQUEsVUFBQSxHQUFBdGYsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBcWdCLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDM2dCLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQztFQUNuSSxNQUFNNGdCLFdBQVcsR0FBR3hGLE9BQU8sQ0FBQ1QsSUFBSSxDQUFDVCxLQUFLLENBQUM7RUFDdkMsTUFBTTJHLFVBQVUsR0FBR3pGLE9BQU8sQ0FBQ0QsTUFBTSxDQUFDakIsS0FBSyxDQUFDO0VBQ3hDLElBQUkwRyxXQUFXLENBQUM5YixNQUFNLEdBQUcsQ0FBQyxJQUFJK2IsVUFBVSxDQUFDL2IsTUFBTSxHQUFHLENBQUMsRUFBRSxNQUFNLElBQUkwRCxLQUFLLENBQUMscUJBQXFCcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUMzRyxLQUFLLE1BQU1oSixLQUFLLElBQUl3Z0IsV0FBVyxFQUFFNVYsT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUUsT0FBTyxDQUFDO0VBQzFFLEtBQUssTUFBTUYsS0FBSyxJQUFJeWdCLFVBQVUsRUFBRTdWLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFLE9BQU8sQ0FBQztFQUN6RSxNQUFNd2dCLE9BQU8sR0FBR0QsVUFBVSxDQUFDMVosSUFBSSxDQUFDbEcsS0FBSyxDQUFDNGYsVUFBVSxDQUFDL2IsTUFBTSxHQUFHLENBQUMsQ0FBQyxDQUFDO0VBQzdEa0csT0FBTyxDQUFDL0osS0FBSyxFQUFFNmYsT0FBTyxDQUFDemdCLENBQUMsRUFBRXlnQixPQUFPLENBQUN4Z0IsQ0FBQyxFQUFFLFlBQVksQ0FBQztFQUNsRCxNQUFNMmIsTUFBTSxHQUFHO0lBQUU1YixDQUFDLEVBQUVxZ0IsVUFBVSxDQUFDdk4sU0FBUyxDQUFDOVMsQ0FBQyxHQUFHOEcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDeWYsVUFBVSxDQUFDdk4sU0FBUyxDQUFDekosS0FBSyxHQUFHLENBQUMsQ0FBQztJQUFFcEosQ0FBQyxFQUFFb2dCLFVBQVUsQ0FBQ3ZOLFNBQVMsQ0FBQzdTLENBQUMsR0FBRzZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ3lmLFVBQVUsQ0FBQ3ZOLFNBQVMsQ0FBQ3hKLE1BQU0sR0FBRyxDQUFDO0VBQUUsQ0FBQztFQUNsS3FCLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWdiLE1BQU0sQ0FBQzViLENBQUMsRUFBRTRiLE1BQU0sQ0FBQzNiLENBQUMsRUFBRSxPQUFPLENBQUM7RUFDM0MsS0FBSyxNQUFNLENBQUNELENBQUMsRUFBRUMsQ0FBQyxDQUFDLElBQUk0TixlQUFlO0lBQUEsSUFBQTZTLFVBQUE7SUFBRSxJQUFJLEVBQUFBLFVBQUEsR0FBQTFmLE9BQU8sQ0FBQ0osS0FBSyxFQUFFZ2IsTUFBTSxDQUFDNWIsQ0FBQyxHQUFHQSxDQUFDLEVBQUU0YixNQUFNLENBQUMzYixDQUFDLEdBQUdBLENBQUMsQ0FBQyxjQUFBeWdCLFVBQUEsdUJBQTFDQSxVQUFBLENBQTRDL2dCLElBQUksTUFBSyxPQUFPLEVBQUVnTCxPQUFPLENBQUMvSixLQUFLLEVBQUVnYixNQUFNLENBQUM1YixDQUFDLEdBQUdBLENBQUMsRUFBRTRiLE1BQU0sQ0FBQzNiLENBQUMsR0FBR0EsQ0FBQyxFQUFFLFlBQVksQ0FBQztFQUFBO0VBQ2hLLE1BQU0wZ0IsSUFBSSxHQUFHdGlCLGNBQWMsQ0FBQyx1QkFBdUIsQ0FBQztFQUNwRHVDLEtBQUssQ0FBQ2UsS0FBSyxDQUFDaUQsSUFBSSxDQUFDO0lBQUVqQyxFQUFFLEVBQUUsUUFBUS9CLEtBQUssQ0FBQ0UsS0FBSywwQkFBMEI4YSxNQUFNLENBQUM1YixDQUFDLElBQUk0YixNQUFNLENBQUMzYixDQUFDLEVBQUU7SUFBRU4sSUFBSSxFQUFFZ2hCLElBQUksQ0FBQ2hlLEVBQUU7SUFBRTNDLENBQUMsRUFBRTRiLE1BQU0sQ0FBQzViLENBQUM7SUFBRUMsQ0FBQyxFQUFFMmIsTUFBTSxDQUFDM2IsQ0FBQztJQUFFbUksS0FBSyxFQUFFeEgsS0FBSyxDQUFDd0gsS0FBSztJQUFFbEUsS0FBSyxFQUFFLFNBQVM7SUFBRTBjLElBQUksRUFBRSxDQUFDLEdBQUdELElBQUksQ0FBQ0MsSUFBSSxDQUFDO0lBQUVDLEtBQUssRUFBRSxDQUFDLEdBQUdGLElBQUksQ0FBQ0UsS0FBSztFQUFFLENBQUMsQ0FBQztFQUMxTixNQUFNQyxXQUFXLEdBQUdQLFdBQVcsQ0FBQ3paLElBQUksQ0FBQ2xHLEtBQUssQ0FBQzJmLFdBQVcsQ0FBQzliLE1BQU0sR0FBRyxDQUFDLENBQUMsQ0FBQztFQUNuRSxNQUFNNmEsTUFBTSxHQUFHamhCLGNBQWMsQ0FBQyx1QkFBdUIsQ0FBQztFQUN0RHVDLEtBQUssQ0FBQ2UsS0FBSyxDQUFDaUQsSUFBSSxDQUFDO0lBQUVqQyxFQUFFLEVBQUUsUUFBUS9CLEtBQUssQ0FBQ0UsS0FBSywwQkFBMEJnZ0IsV0FBVyxDQUFDOWdCLENBQUMsSUFBSThnQixXQUFXLENBQUM3Z0IsQ0FBQyxFQUFFO0lBQUVOLElBQUksRUFBRTJmLE1BQU0sQ0FBQzNjLEVBQUU7SUFBRTNDLENBQUMsRUFBRThnQixXQUFXLENBQUM5Z0IsQ0FBQztJQUFFQyxDQUFDLEVBQUU2Z0IsV0FBVyxDQUFDN2dCLENBQUM7SUFBRW1JLEtBQUssRUFBRXhILEtBQUssQ0FBQ3dILEtBQUs7SUFBRWxFLEtBQUssRUFBRSxTQUFTO0lBQUUwYyxJQUFJLEVBQUUsQ0FBQyxHQUFHdEIsTUFBTSxDQUFDc0IsSUFBSSxDQUFDO0lBQUVDLEtBQUssRUFBRSxDQUFDLEdBQUd2QixNQUFNLENBQUN1QixLQUFLO0VBQUUsQ0FBQyxDQUFDO0FBQ3RQLENBQUM7QUFFRCxNQUFNOVUseUJBQXlCLEdBQUdBLENBQUNuTCxLQUFZLEVBQUU0SSxLQUF1QixLQUFXO0VBQ2pGLElBQUk1SSxLQUFLLENBQUN3SCxLQUFLLEtBQUssZ0JBQWdCLEVBQUU7RUFDdEMsTUFBTWtTLElBQUksR0FBRzlRLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDcVgsS0FBSyxDQUFDelosUUFBUSxDQUFDLE1BQU0sQ0FBQyxDQUFDO0VBQzVFLE1BQU0yYSxNQUFNLEdBQUd0UixLQUFLLENBQUN1TixLQUFLLENBQUMxVixJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxRQUFRLENBQUMsSUFBSW9DLFNBQVMsQ0FBQ3FYLEtBQUssQ0FBQ3paLFFBQVEsQ0FBQyxVQUFVLENBQUMsQ0FBQztFQUN4SCxNQUFNa2dCLFVBQVUsR0FBRzdXLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzVHLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDNUMsSUFBSSxLQUFLLGdCQUFnQixDQUFDO0VBQ3JGLElBQUksQ0FBQzJhLElBQUksSUFBSSxDQUFDUSxNQUFNLElBQUksQ0FBQ3VGLFVBQVUsRUFBRSxNQUFNLElBQUlsWSxLQUFLLENBQUMsZ0NBQWdDcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUN0RyxNQUFNZ1MsT0FBTyxHQUFJbEIsS0FBdUIsSUFBS0EsS0FBSyxDQUFDakUsS0FBSyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDcFAsTUFBTSxDQUFDekcsS0FBSztJQUFBLElBQUFnaEIsVUFBQTtJQUFBLE9BQUksRUFBQUEsVUFBQSxHQUFBL2YsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxjQUFBOGdCLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDcGhCLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQztFQUNuSSxNQUFNcWhCLFVBQVUsR0FBR2pHLE9BQU8sQ0FBQ1QsSUFBSSxDQUFDVCxLQUFLLENBQUM7RUFDdEMsTUFBTW9ILFFBQVEsR0FBR2xHLE9BQU8sQ0FBQ0QsTUFBTSxDQUFDakIsS0FBSyxDQUFDO0VBQ3RDLElBQUltSCxVQUFVLENBQUN2YyxNQUFNLEdBQUcsQ0FBQyxJQUFJd2MsUUFBUSxDQUFDeGMsTUFBTSxHQUFHLENBQUMsRUFBRSxNQUFNLElBQUkwRCxLQUFLLENBQUMsc0JBQXNCcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUN6RyxLQUFLLE1BQU1oSixLQUFLLElBQUlpaEIsVUFBVSxFQUFFclcsT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUUsT0FBTyxDQUFDO0VBQ3pFLEtBQUssTUFBTUYsS0FBSyxJQUFJa2hCLFFBQVEsRUFBRXRXLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFLEtBQUssQ0FBQztFQUNyRSxNQUFNaWhCLFFBQVEsR0FBR0QsUUFBUSxDQUFDbmEsSUFBSSxDQUFDbEcsS0FBSyxDQUFDcWdCLFFBQVEsQ0FBQ3hjLE1BQU0sR0FBRyxDQUFDLENBQUMsQ0FBQztFQUMxRGtHLE9BQU8sQ0FBQy9KLEtBQUssRUFBRXNnQixRQUFRLENBQUNsaEIsQ0FBQyxFQUFFa2hCLFFBQVEsQ0FBQ2poQixDQUFDLEVBQUUsV0FBVyxDQUFDO0VBQ25ELE1BQU0yYixNQUFNLEdBQUc7SUFBRTViLENBQUMsRUFBRXFnQixVQUFVLENBQUN2TixTQUFTLENBQUM5UyxDQUFDLEdBQUc4RyxJQUFJLENBQUNsRyxLQUFLLENBQUN5ZixVQUFVLENBQUN2TixTQUFTLENBQUN6SixLQUFLLEdBQUcsQ0FBQyxDQUFDO0lBQUVwSixDQUFDLEVBQUVvZ0IsVUFBVSxDQUFDdk4sU0FBUyxDQUFDN1MsQ0FBQyxHQUFHNkcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDeWYsVUFBVSxDQUFDdk4sU0FBUyxDQUFDeEosTUFBTSxHQUFHLENBQUM7RUFBRSxDQUFDO0VBQ2xLcUIsT0FBTyxDQUFDL0osS0FBSyxFQUFFZ2IsTUFBTSxDQUFDNWIsQ0FBQyxFQUFFNGIsTUFBTSxDQUFDM2IsQ0FBQyxFQUFFLE9BQU8sQ0FBQztFQUMzQyxLQUFLLE1BQU0sQ0FBQ0QsQ0FBQyxFQUFFQyxDQUFDLENBQUMsSUFBSTROLGVBQWU7SUFBQSxJQUFBc1QsVUFBQTtJQUFFLElBQUksRUFBQUEsVUFBQSxHQUFBbmdCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFZ2IsTUFBTSxDQUFDNWIsQ0FBQyxHQUFHQSxDQUFDLEVBQUU0YixNQUFNLENBQUMzYixDQUFDLEdBQUdBLENBQUMsQ0FBQyxjQUFBa2hCLFVBQUEsdUJBQTFDQSxVQUFBLENBQTRDeGhCLElBQUksTUFBSyxNQUFNLEVBQUVnTCxPQUFPLENBQUMvSixLQUFLLEVBQUVnYixNQUFNLENBQUM1YixDQUFDLEdBQUdBLENBQUMsRUFBRTRiLE1BQU0sQ0FBQzNiLENBQUMsR0FBR0EsQ0FBQyxFQUFFLE9BQU8sQ0FBQztFQUFBO0VBQzFKLE1BQU1taEIsS0FBSyxHQUFHQSxDQUFDemhCLElBQWtCLEVBQUVJLEtBQVksS0FBSztJQUNsRCxNQUFNc2hCLFVBQVUsR0FBR2hqQixjQUFjLENBQUNzQixJQUFJLENBQUM7SUFDdkNpQixLQUFLLENBQUNlLEtBQUssQ0FBQ2lELElBQUksQ0FBQztNQUFFakMsRUFBRSxFQUFFLFFBQVEvQixLQUFLLENBQUNFLEtBQUssSUFBSW5CLElBQUksSUFBSUksS0FBSyxDQUFDQyxDQUFDLElBQUlELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFO01BQUVOLElBQUk7TUFBRUssQ0FBQyxFQUFFRCxLQUFLLENBQUNDLENBQUM7TUFBRUMsQ0FBQyxFQUFFRixLQUFLLENBQUNFLENBQUM7TUFBRW1JLEtBQUssRUFBRXhILEtBQUssQ0FBQ3dILEtBQUs7TUFBRWxFLEtBQUssRUFBRSxTQUFTO01BQUUwYyxJQUFJLEVBQUUsQ0FBQyxHQUFHUyxVQUFVLENBQUNULElBQUksQ0FBQztNQUFFQyxLQUFLLEVBQUUsQ0FBQyxHQUFHUSxVQUFVLENBQUNSLEtBQUs7SUFBRSxDQUFDLENBQUM7RUFDN00sQ0FBQztFQUNETyxLQUFLLENBQUMseUJBQXlCLEVBQUV4RixNQUFNLENBQUM7RUFDeEN3RixLQUFLLENBQUMsZ0NBQWdDLEVBQUVKLFVBQVUsQ0FBQ2xhLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ29nQixVQUFVLENBQUN2YyxNQUFNLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUN0RjJjLEtBQUssQ0FBQyx5QkFBeUIsRUFBRUgsUUFBUSxDQUFDLENBQUMsQ0FBQyxDQUFDO0VBQzdDRyxLQUFLLENBQUMsNEJBQTRCLEVBQUVGLFFBQVEsQ0FBQztBQUMvQyxDQUFDO0FBRUQsTUFBTWxWLDRCQUE0QixHQUFHQSxDQUFDcEwsS0FBWSxFQUFFNEksS0FBdUIsS0FBVztFQUNwRixJQUFJNUksS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFNBQVMsRUFBRTtFQUMvQixNQUFNSixJQUFJLEdBQUd3QixLQUFLLENBQUN2QixLQUFLLENBQUM1RyxJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQzVDLElBQUksS0FBSyxnQkFBZ0IsQ0FBQztFQUMvRSxJQUFJLENBQUNxSSxJQUFJLEVBQUUsTUFBTSxJQUFJRyxLQUFLLENBQUMscUNBQXFDcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUNqRixNQUFNaVAsVUFBVSxHQUFHbk8sS0FBSyxDQUFDQyxJQUFJLENBQUM7SUFBRXJGLE1BQU0sRUFBRXFDLElBQUksQ0FBQ0MsR0FBRyxDQUFDLENBQUMsRUFBRWlCLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3pKLEtBQUssR0FBRyxDQUFDLENBQUMsR0FBR3ZDLElBQUksQ0FBQ0MsR0FBRyxDQUFDLENBQUMsRUFBRWlCLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3hKLE1BQU0sR0FBRyxDQUFDO0VBQUUsQ0FBQyxFQUFFLENBQUM0TixDQUFDLEVBQUVwVyxLQUFLLE1BQU07SUFBRWQsQ0FBQyxFQUFFZ0ksSUFBSSxDQUFDOEssU0FBUyxDQUFDOVMsQ0FBQyxHQUFHLENBQUMsR0FBR2MsS0FBSyxJQUFJa0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSyxHQUFHLENBQUMsQ0FBQztJQUFFcEosQ0FBQyxFQUFFK0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDN1MsQ0FBQyxHQUFHLENBQUMsR0FBRzZHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQ0UsS0FBSyxJQUFJa0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSyxHQUFHLENBQUMsQ0FBQztFQUFFLENBQUMsQ0FBQyxDQUFDO0VBQ3JSLE1BQU10SixLQUFLLEdBQUdpWSxVQUFVLENBQUMzVyxJQUFJLENBQUNrQixTQUFTO0lBQUEsSUFBQStlLFVBQUE7SUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQXRnQixPQUFPLENBQUNKLEtBQUssRUFBRTJCLFNBQVMsQ0FBQ3ZDLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ3RDLENBQUMsQ0FBQyxjQUFBcWhCLFVBQUEsdUJBQXhDQSxVQUFBLENBQTBDM2hCLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQztFQUN0RyxJQUFJLENBQUNJLEtBQUssRUFBRSxNQUFNLElBQUlvSSxLQUFLLENBQUMsbUNBQW1DcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUNoRixNQUFNd1ksTUFBTSxHQUFHMVQsZUFBZSxDQUFDdEgsR0FBRyxDQUFDLENBQUMsQ0FBQ3ZHLENBQUMsRUFBRUMsQ0FBQyxDQUFDLE1BQU07SUFBRUQsQ0FBQyxFQUFFRCxLQUFLLENBQUNDLENBQUMsR0FBR0EsQ0FBQztJQUFFQyxDQUFDLEVBQUVGLEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQTtFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUN1RyxNQUFNLENBQUNqRSxTQUFTO0lBQUEsSUFBQWlmLFVBQUE7SUFBQSxPQUFJLEVBQUFBLFVBQUEsR0FBQXhnQixPQUFPLENBQUNKLEtBQUssRUFBRTJCLFNBQVMsQ0FBQ3ZDLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ3RDLENBQUMsQ0FBQyxjQUFBdWhCLFVBQUEsdUJBQXhDQSxVQUFBLENBQTBDN2hCLElBQUksTUFBSyxPQUFPO0VBQUEsRUFBQztFQUNwSyxNQUFNOGhCLFFBQVEsR0FBR0YsTUFBTSxDQUFDLENBQUMsQ0FBQztFQUMxQixJQUFJLENBQUNFLFFBQVEsRUFBRSxNQUFNLElBQUl0WixLQUFLLENBQUMsc0NBQXNDcUIsS0FBSyxDQUFDVCxRQUFRLEVBQUUsQ0FBQztFQUN0RixNQUFNMlksS0FBSyxHQUFHSCxNQUFNLENBQUMsQ0FBQyxDQUFDO0VBQ3ZCLE1BQU1JLElBQUksR0FBR0osTUFBTSxDQUFDLENBQUMsQ0FBQztFQUN0QjVXLE9BQU8sQ0FBQy9KLEtBQUssRUFBRTZnQixRQUFRLENBQUN6aEIsQ0FBQyxFQUFFeWhCLFFBQVEsQ0FBQ3hoQixDQUFDLEVBQUUsVUFBVSxDQUFDO0VBQ2xELElBQUl5aEIsS0FBSyxFQUFFL1csT0FBTyxDQUFDL0osS0FBSyxFQUFFOGdCLEtBQUssQ0FBQzFoQixDQUFDLEVBQUUwaEIsS0FBSyxDQUFDemhCLENBQUMsRUFBRSxPQUFPLENBQUM7RUFDcEQsSUFBSTBoQixJQUFJLEVBQUVoWCxPQUFPLENBQUMvSixLQUFLLEVBQUUrZ0IsSUFBSSxDQUFDM2hCLENBQUMsRUFBRTJoQixJQUFJLENBQUMxaEIsQ0FBQyxFQUFFLFdBQVcsQ0FBQztFQUNyRCxNQUFNMmhCLE1BQU0sR0FBR3ZqQixjQUFjLENBQUMsdUJBQXVCLENBQUM7RUFDdER1QyxLQUFLLENBQUNlLEtBQUssQ0FBQ2lELElBQUksQ0FBQztJQUFFakMsRUFBRSxFQUFFLFFBQVEvQixLQUFLLENBQUNFLEtBQUssMEJBQTBCZixLQUFLLENBQUNDLENBQUMsSUFBSUQsS0FBSyxDQUFDRSxDQUFDLEVBQUU7SUFBRU4sSUFBSSxFQUFFaWlCLE1BQU0sQ0FBQ2pmLEVBQUU7SUFBRTNDLENBQUMsRUFBRUQsS0FBSyxDQUFDQyxDQUFDO0lBQUVDLENBQUMsRUFBRUYsS0FBSyxDQUFDRSxDQUFDO0lBQUVtSSxLQUFLLEVBQUV4SCxLQUFLLENBQUN3SCxLQUFLO0lBQUVsRSxLQUFLLEVBQUUsU0FBUztJQUFFMGMsSUFBSSxFQUFFLENBQUMsR0FBR2dCLE1BQU0sQ0FBQ2hCLElBQUksQ0FBQztJQUFFQyxLQUFLLEVBQUUsQ0FBQyxHQUFHZSxNQUFNLENBQUNmLEtBQUs7RUFBRSxDQUFDLENBQUM7QUFDOU4sQ0FBQztBQUVELE1BQU0zTyxxQkFBcUIsR0FBR0EsQ0FBQ3RSLEtBQVksRUFBRW9RLEdBQVEsRUFBRXpHLEtBQXNCLEtBQVc7RUFDdEYsTUFBTXNYLEtBQUssR0FBR0EsQ0FBQzdoQixDQUFTLEVBQUVDLENBQVMsRUFBRU4sSUFBa0IsS0FBSztJQUFBLElBQUFtaUIsVUFBQTtJQUFFLElBQUksRUFBQUEsVUFBQSxHQUFBOWdCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsQ0FBQyxjQUFBNmhCLFVBQUEsdUJBQXBCQSxVQUFBLENBQXNCbmlCLElBQUksTUFBSyxPQUFPLEVBQUVnTCxPQUFPLENBQUMvSixLQUFLLEVBQUVaLENBQUMsRUFBRUMsQ0FBQyxFQUFFTixJQUFJLENBQUM7RUFBQyxDQUFDO0VBQ3RJLElBQUlpQixLQUFLLENBQUN3SCxLQUFLLEtBQUssUUFBUSxFQUFFO0lBQzVCLEtBQUssTUFBTXNGLElBQUksSUFBSW5ELEtBQUssQ0FBQ3FMLEtBQUssQ0FBQyxDQUFDLENBQUMsRUFBRTtNQUNqQyxNQUFNaFQsTUFBTSxHQUFHOEgsTUFBTSxDQUFDZ0QsSUFBSSxDQUFDO01BQzNCLEtBQUssSUFBSXpOLENBQUMsR0FBRyxDQUFDLENBQUMsRUFBRUEsQ0FBQyxJQUFJLENBQUMsRUFBRUEsQ0FBQyxFQUFFLEVBQUUsS0FBSyxJQUFJRCxDQUFDLEdBQUcsQ0FBQyxDQUFDLEVBQUVBLENBQUMsSUFBSSxDQUFDLEVBQUVBLENBQUMsRUFBRSxFQUFFO1FBQzFELE1BQU1vVCxRQUFRLEdBQUd0TSxJQUFJLENBQUNrSixHQUFHLENBQUNoUSxDQUFDLENBQUMsR0FBRzhHLElBQUksQ0FBQ2tKLEdBQUcsQ0FBQy9QLENBQUMsQ0FBQztRQUMxQyxJQUFJbVQsUUFBUSxLQUFLLENBQUMsRUFBRXlPLEtBQUssQ0FBQ2pmLE1BQU0sQ0FBQzVDLENBQUMsR0FBR0EsQ0FBQyxFQUFFNEMsTUFBTSxDQUFDM0MsQ0FBQyxHQUFHQSxDQUFDLEVBQUUsT0FBTyxDQUFDLE1BQ3pELElBQUltVCxRQUFRLEdBQUcsQ0FBQyxJQUFJcEMsR0FBRyxDQUFDcUMsTUFBTSxDQUFDLEVBQUUsQ0FBQyxFQUFFd08sS0FBSyxDQUFDamYsTUFBTSxDQUFDNUMsQ0FBQyxHQUFHQSxDQUFDLEVBQUU0QyxNQUFNLENBQUMzQyxDQUFDLEdBQUdBLENBQUMsRUFBRSxXQUFXLENBQUM7TUFDekY7SUFDRjtFQUNGLENBQUMsTUFBTSxJQUFJVyxLQUFLLENBQUN3SCxLQUFLLEtBQUssV0FBVyxFQUFFO0lBQ3RDLEtBQUssSUFBSXBJLENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBR1ksS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUMsRUFBRXJKLENBQUMsSUFBSSxFQUFFLEVBQUUsS0FBSyxJQUFJQyxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUdXLEtBQUssQ0FBQzBJLE1BQU0sR0FBRyxDQUFDLEVBQUVySixDQUFDLEVBQUUsRUFBRSxJQUFJLENBQUNBLENBQUMsR0FBR0QsQ0FBQyxJQUFJLENBQUMsS0FBSyxDQUFDLEVBQUU2aEIsS0FBSyxDQUFDN2hCLENBQUMsRUFBRUMsQ0FBQyxFQUFFVyxLQUFLLENBQUN5SCxRQUFRLENBQUNsSSxRQUFRLENBQUMsU0FBUyxDQUFDLEdBQUcsWUFBWSxHQUFHLE9BQU8sQ0FBQztFQUMxTCxDQUFDLE1BQU0sSUFBSVMsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLGdCQUFnQixFQUFFO0lBQUEsSUFBQTJaLFFBQUE7SUFDM0MsTUFBTUMsSUFBSSxJQUFBRCxRQUFBLEdBQUd4WCxLQUFLLENBQUMsQ0FBQyxDQUFDLGNBQUF3WCxRQUFBLGNBQUFBLFFBQUEsR0FBSXhYLEtBQUssQ0FBQyxDQUFDLENBQUM7SUFDakMsS0FBSyxJQUFJdEssQ0FBQyxHQUFHK2hCLElBQUksQ0FBQy9oQixDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUcraEIsSUFBSSxDQUFDL2hCLENBQUMsR0FBRytoQixJQUFJLENBQUNwVSxDQUFDLEdBQUcsQ0FBQyxFQUFFM04sQ0FBQyxFQUFFLEVBQUUsS0FBSyxJQUFJRCxDQUFDLEdBQUdnaUIsSUFBSSxDQUFDaGlCLENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBR2dpQixJQUFJLENBQUNoaUIsQ0FBQyxHQUFHZ2lCLElBQUksQ0FBQ3JVLENBQUMsR0FBRyxDQUFDLEVBQUUzTixDQUFDLEVBQUUsRUFBRTZoQixLQUFLLENBQUM3aEIsQ0FBQyxFQUFFQyxDQUFDLEVBQUVXLEtBQUssQ0FBQ3lILFFBQVEsQ0FBQ2xJLFFBQVEsQ0FBQyxhQUFhLENBQUMsR0FBRyxLQUFLLEdBQUcsV0FBVyxDQUFDO0VBQ3pMLENBQUMsTUFBTSxJQUFJUyxLQUFLLENBQUN3SCxLQUFLLEtBQUssUUFBUSxFQUFFO0lBQ25DLEtBQUssSUFBSW5JLENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBR1csS0FBSyxDQUFDMEksTUFBTSxHQUFHLENBQUMsRUFBRXJKLENBQUMsSUFBSSxDQUFDLEVBQUUsS0FBSyxJQUFJRCxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUdZLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDLEVBQUVySixDQUFDLEVBQUUsRUFBRSxJQUFJQSxDQUFDLEdBQUcsQ0FBQyxLQUFLLENBQUMsRUFBRTZoQixLQUFLLENBQUM3aEIsQ0FBQyxFQUFFQyxDQUFDLEVBQUUsT0FBTyxDQUFDO0VBQy9ILENBQUMsTUFBTSxJQUFJVyxLQUFLLENBQUN3SCxLQUFLLEtBQUssU0FBUyxFQUFFO0lBQ3BDLEtBQUssSUFBSW5JLENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBR1csS0FBSyxDQUFDMEksTUFBTSxHQUFHLENBQUMsRUFBRXJKLENBQUMsSUFBSSxDQUFDLEVBQUUsS0FBSyxJQUFJRCxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUdZLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDLEVBQUVySixDQUFDLEVBQUUsRUFBRSxJQUFJQSxDQUFDLEdBQUcsQ0FBQyxLQUFLLENBQUMsRUFBRTZoQixLQUFLLENBQUM3aEIsQ0FBQyxFQUFFQyxDQUFDLEVBQUVXLEtBQUssQ0FBQ3lILFFBQVEsQ0FBQ2xJLFFBQVEsQ0FBQyxNQUFNLENBQUMsR0FBRyxNQUFNLEdBQUcsT0FBTyxDQUFDO0VBQzFLLENBQUMsTUFBTSxJQUFJUyxLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxFQUFFO0lBQ2xDLEtBQUssTUFBTXNGLElBQUksSUFBSW5ELEtBQUssQ0FBQ3FMLEtBQUssQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsRUFBRTtNQUNyQyxNQUFNaFQsTUFBTSxHQUFHOEgsTUFBTSxDQUFDZ0QsSUFBSSxDQUFDO01BQzNCLEtBQUssTUFBTSxDQUFDMU4sQ0FBQyxFQUFFQyxDQUFDLENBQUMsSUFBSTROLGVBQWUsRUFBRWdVLEtBQUssQ0FBQ2pmLE1BQU0sQ0FBQzVDLENBQUMsR0FBR0EsQ0FBQyxHQUFHLENBQUMsRUFBRTRDLE1BQU0sQ0FBQzNDLENBQUMsR0FBR0EsQ0FBQyxHQUFHLENBQUMsRUFBRSxNQUFNLENBQUM7SUFDekY7RUFDRjtBQUNGLENBQUM7QUFFRCxTQUFTZ1MsWUFBWUEsQ0FBQ3JSLEtBQVksRUFBRTJKLEtBQWEsRUFBUTtFQUN2RCxLQUFLLElBQUkwWCxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUcxWCxLQUFLLENBQUM5RixNQUFNLEVBQUV3ZCxDQUFDLEVBQUUsRUFBRTtJQUNyQyxNQUFNblksSUFBSSxHQUFHWSxNQUFNLENBQUNILEtBQUssQ0FBQzBYLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQztJQUNqQyxNQUFNclEsRUFBRSxHQUFHbEgsTUFBTSxDQUFDSCxLQUFLLENBQUMwWCxDQUFDLENBQUMsQ0FBQztJQUMzQixJQUFJQSxDQUFDLEdBQUcsQ0FBQyxFQUFFO01BQUVsUSxNQUFNLENBQUNuUixLQUFLLEVBQUVrSixJQUFJLENBQUM5SixDQUFDLEVBQUU0UixFQUFFLENBQUM1UixDQUFDLEVBQUU4SixJQUFJLENBQUM3SixDQUFDLENBQUM7TUFBRTZSLE1BQU0sQ0FBQ2xSLEtBQUssRUFBRWtKLElBQUksQ0FBQzdKLENBQUMsRUFBRTJSLEVBQUUsQ0FBQzNSLENBQUMsRUFBRTJSLEVBQUUsQ0FBQzVSLENBQUMsQ0FBQztJQUFDLENBQUMsTUFDaEY7TUFBRThSLE1BQU0sQ0FBQ2xSLEtBQUssRUFBRWtKLElBQUksQ0FBQzdKLENBQUMsRUFBRTJSLEVBQUUsQ0FBQzNSLENBQUMsRUFBRTZKLElBQUksQ0FBQzlKLENBQUMsQ0FBQztNQUFFK1IsTUFBTSxDQUFDblIsS0FBSyxFQUFFa0osSUFBSSxDQUFDOUosQ0FBQyxFQUFFNFIsRUFBRSxDQUFDNVIsQ0FBQyxFQUFFNFIsRUFBRSxDQUFDM1IsQ0FBQyxDQUFDO0lBQUM7RUFDaEY7QUFDRjtBQUdBLE1BQU1paUIsa0JBQXNELEdBQUc7RUFDN0RsVSxJQUFJLEVBQUUsQ0FBQyxNQUFNLEVBQUUsU0FBUyxDQUFDO0VBQUVDLEtBQUssRUFBRSxDQUFDLE9BQU8sRUFBRSxLQUFLLENBQUM7RUFBRUMsT0FBTyxFQUFFLENBQUMsT0FBTyxFQUFFLFNBQVMsRUFBRSxVQUFVLENBQUM7RUFBRUMsS0FBSyxFQUFFLENBQUMsT0FBTyxFQUFFLE1BQU0sQ0FBQztFQUFFQyxPQUFPLEVBQUUsQ0FBQyxPQUFPLEVBQUUsTUFBTSxDQUFDO0VBQUVDLFlBQVksRUFBRSxDQUFDLFNBQVMsRUFBRSxRQUFRLENBQUM7RUFBRUMsTUFBTSxFQUFFLENBQUMsT0FBTyxFQUFFLE1BQU0sQ0FBQztFQUFFQyxNQUFNLEVBQUUsQ0FBQyxXQUFXLEVBQUUsWUFBWSxDQUFDO0VBQUVDLFNBQVMsRUFBRSxDQUFDLFlBQVksRUFBRSxPQUFPLENBQUM7RUFBRUMsY0FBYyxFQUFFLENBQUMsS0FBSyxFQUFFLFdBQVc7QUFDblUsQ0FBQztBQUNELE1BQU0wVCxnQkFBZ0IsR0FBR0EsQ0FBQ3ZoQixLQUFZLEVBQUV3aEIsT0FBeUIsRUFBRUMsUUFBbUMsR0FBR0EsQ0FBQSxLQUFNLElBQUksS0FBdUI7RUFDeEksTUFBTUMsU0FBUyxHQUFHLElBQUlyZSxHQUFHLENBQTBCLENBQUM7RUFDcEQsTUFBTXNlLFNBQVMsR0FBRyxJQUFJdGUsR0FBRyxDQUEwQixDQUFDO0VBQ3BELElBQUltZSxPQUFPLENBQUN6WSxLQUFLLEVBQUU7SUFDakIsS0FBSyxNQUFNM0IsSUFBSSxJQUFJb2EsT0FBTyxDQUFDNVksS0FBSyxDQUFDdkIsS0FBSyxFQUFFLEtBQUssSUFBSWhJLENBQUMsR0FBRytILElBQUksQ0FBQzhLLFNBQVMsQ0FBQzdTLENBQUMsRUFBRUEsQ0FBQyxHQUFHK0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDN1MsQ0FBQyxHQUFHK0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDeEosTUFBTSxFQUFFckosQ0FBQyxFQUFFLEVBQUUsS0FBSyxJQUFJRCxDQUFDLEdBQUdnSSxJQUFJLENBQUM4SyxTQUFTLENBQUM5UyxDQUFDLEVBQUVBLENBQUMsR0FBR2dJLElBQUksQ0FBQzhLLFNBQVMsQ0FBQzlTLENBQUMsR0FBR2dJLElBQUksQ0FBQzhLLFNBQVMsQ0FBQ3pKLEtBQUssRUFBRXJKLENBQUMsRUFBRSxFQUFFO01BQUEsSUFBQXdpQixjQUFBO01BQ3pNLE1BQU1DLEdBQUcsR0FBRzNpQixRQUFRLENBQUM7UUFBRUUsQ0FBQztRQUFFQztNQUFFLENBQUMsQ0FBQztNQUM5QnFpQixTQUFTLENBQUNuZSxHQUFHLENBQUNzZSxHQUFHLEVBQUUsQ0FBQyxLQUFBRCxjQUFBLEdBQUlGLFNBQVMsQ0FBQzNkLEdBQUcsQ0FBQzhkLEdBQUcsQ0FBQyxjQUFBRCxjQUFBLGNBQUFBLGNBQUEsR0FBSSxFQUFFLENBQUMsRUFBRXhhLElBQUksQ0FBQ3JJLElBQUksQ0FBQyxDQUFDO0lBQ2hFO0lBQ0EsS0FBSyxNQUFNc1gsSUFBSSxJQUFJbUwsT0FBTyxDQUFDNVksS0FBSyxDQUFDdU4sS0FBSyxFQUFFLEtBQUssTUFBTWhYLEtBQUssSUFBSWtYLElBQUksQ0FBQzRDLEtBQUssRUFBRTtNQUFBLElBQUE2SSxjQUFBO01BQ3RFLE1BQU1ELEdBQUcsR0FBRzNpQixRQUFRLENBQUNDLEtBQUssQ0FBQztNQUMzQndpQixTQUFTLENBQUNwZSxHQUFHLENBQUNzZSxHQUFHLEVBQUUsQ0FBQyxLQUFBQyxjQUFBLEdBQUlILFNBQVMsQ0FBQzVkLEdBQUcsQ0FBQzhkLEdBQUcsQ0FBQyxjQUFBQyxjQUFBLGNBQUFBLGNBQUEsR0FBSSxFQUFFLENBQUMsRUFBRSxHQUFHekwsSUFBSSxDQUFDMkMsS0FBSyxDQUFDLENBQUM7SUFDcEU7RUFDRjtFQUNBLE1BQU10VCxRQUFRLEdBQUl2RyxLQUFZLElBQWM4TixlQUFlLENBQUN0SCxHQUFHLENBQUMsQ0FBQyxDQUFDdkcsQ0FBQyxFQUFFQyxDQUFDLENBQUMsTUFBTTtJQUFFRCxDQUFDLEVBQUVELEtBQUssQ0FBQ0MsQ0FBQyxHQUFHQSxDQUFDO0lBQUVDLENBQUMsRUFBRUYsS0FBSyxDQUFDRSxDQUFDLEdBQUdBO0VBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQ3VHLE1BQU0sQ0FBQ3pHLEtBQUssSUFBSWdCLFFBQVEsQ0FBQ0gsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQztFQUNwSyxNQUFNMGlCLE9BQU8sR0FBSTVpQixLQUFZO0lBQUEsSUFBQTZpQixjQUFBO0lBQUEsT0FBYyxDQUFDUCxRQUFRLENBQUN0aUIsS0FBSyxDQUFDLElBQUlhLEtBQUssQ0FBQ2UsS0FBSyxDQUFDb0csSUFBSSxDQUFDeEUsSUFBSSxJQUFJQSxJQUFJLENBQUN2RCxDQUFDLEtBQUtELEtBQUssQ0FBQ0MsQ0FBQyxJQUFJdUQsSUFBSSxDQUFDdEQsQ0FBQyxLQUFLRixLQUFLLENBQUNFLENBQUMsQ0FBQyxJQUFJVyxLQUFLLENBQUNRLE1BQU0sQ0FBQzJHLElBQUksQ0FBQ3pHLEtBQUssSUFBSUEsS0FBSyxDQUFDQyxNQUFNLEdBQUcsQ0FBQyxJQUFJRCxLQUFLLENBQUN0QixDQUFDLEtBQUtELEtBQUssQ0FBQ0MsQ0FBQyxJQUFJc0IsS0FBSyxDQUFDckIsQ0FBQyxLQUFLRixLQUFLLENBQUNFLENBQUMsQ0FBQyxJQUFJVyxLQUFLLENBQUNtSixLQUFLLENBQUNoQyxJQUFJLENBQUM4YSxJQUFJLElBQUlBLElBQUksQ0FBQzdpQixDQUFDLEtBQUtELEtBQUssQ0FBQ0MsQ0FBQyxJQUFJNmlCLElBQUksQ0FBQzVpQixDQUFDLEtBQUtGLEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLElBQUlXLEtBQUssQ0FBQ3lKLFVBQVUsQ0FBQ3RDLElBQUksQ0FBQythLFNBQVMsSUFBSUEsU0FBUyxDQUFDOWlCLENBQUMsS0FBS0QsS0FBSyxDQUFDQyxDQUFDLElBQUk4aUIsU0FBUyxDQUFDN2lCLENBQUMsS0FBS0YsS0FBSyxDQUFDRSxDQUFDLENBQUMsSUFBSSxFQUFBMmlCLGNBQUEsR0FBQWhpQixLQUFLLENBQUNtaUIsT0FBTyxjQUFBSCxjQUFBLHVCQUFiQSxjQUFBLENBQWU3YSxJQUFJLENBQUNnYixPQUFPLElBQUlBLE9BQU8sQ0FBQ3RoQixNQUFNLENBQUN6QixDQUFDLEtBQUtELEtBQUssQ0FBQ0MsQ0FBQyxJQUFJK2lCLE9BQU8sQ0FBQ3RoQixNQUFNLENBQUN4QixDQUFDLEtBQUtGLEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLE1BQUssSUFBSTtFQUFBO0VBQ3hlLE9BQU87SUFDTHFVLE1BQU0sRUFBRTFULEtBQUssQ0FBQ0ssS0FBSyxDQUFDc0YsR0FBRyxDQUFDLENBQUMyUSxDQUFDLEVBQUVwVyxLQUFLLEtBQUtELE9BQU8sQ0FBQ0QsS0FBSyxFQUFFRSxLQUFLLENBQUMsQ0FBQztJQUM1RGtpQixTQUFTLEVBQUVqakIsS0FBSztNQUFBLElBQUFrakIsVUFBQTtNQUFBLFFBQUFBLFVBQUEsR0FBSWppQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUFnakIsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0N0akIsSUFBSTtJQUFBO0lBQzFEdWpCLFVBQVUsRUFBRW5qQixLQUFLLElBQUkyQixPQUFPLENBQUNWLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsSUFBSXdELGNBQWMsQ0FBQzdDLEtBQUssRUFBRWIsS0FBSyxFQUFFLElBQUksQ0FBQyxDQUFDO0lBQ3BHb2pCLFNBQVMsRUFBRVIsT0FBTztJQUNsQlMsaUJBQWlCLEVBQUVyakIsS0FBSyxJQUFJcVQsUUFBUSxDQUFDclQsS0FBSyxFQUFFYSxLQUFLLENBQUMrQyxLQUFLLENBQUM7SUFDeEQwZixTQUFTLEVBQUV0akIsS0FBSztNQUFBLElBQUF1akIsVUFBQTtNQUFBLE9BQUk1aEIsT0FBTyxFQUFBNGhCLFVBQUEsR0FBQ3RpQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLGNBQUFxakIsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0N6akIsT0FBTyxDQUFDO0lBQUE7SUFDdEUwakIsU0FBUyxFQUFFeGpCLEtBQUssSUFBSXVHLFFBQVEsQ0FBQ3ZHLEtBQUssQ0FBQyxDQUFDZ0ksSUFBSSxDQUFDeEYsU0FBUztNQUFBLElBQUFpaEIsY0FBQSxFQUFBQyxVQUFBO01BQUEsT0FBSSxDQUFDdmpCLFFBQVEsRUFBQXNqQixjQUFBLElBQUFDLFVBQUEsR0FBQ3ppQixPQUFPLENBQUNKLEtBQUssRUFBRTJCLFNBQVMsQ0FBQ3ZDLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ3RDLENBQUMsQ0FBQyxjQUFBd2pCLFVBQUEsdUJBQXhDQSxVQUFBLENBQTBDOWpCLElBQUksY0FBQTZqQixjQUFBLGNBQUFBLGNBQUEsR0FBSSxNQUFNLENBQUMsSUFBSXRsQixjQUFjLENBQUNFLE1BQU0sQ0FBQ3dDLEtBQUssQ0FBQ2UsS0FBSyxFQUFFWSxTQUFTLENBQUN2QyxDQUFDLEVBQUV1QyxTQUFTLENBQUN0QyxDQUFDLENBQUMsQ0FBQztJQUFBLEVBQUM7SUFDM0x5akIsWUFBWSxFQUFFM2pCLEtBQUssSUFBSXVHLFFBQVEsQ0FBQ3ZHLEtBQUssQ0FBQyxDQUFDeUcsTUFBTSxDQUFDakUsU0FBUyxJQUFJa0IsY0FBYyxDQUFDN0MsS0FBSyxFQUFFMkIsU0FBUyxFQUFFLEtBQUssQ0FBQyxDQUFDLENBQUNrQyxNQUFNLElBQUksQ0FBQztJQUMvR2tmLGlCQUFpQixFQUFFNWpCLEtBQUssSUFBSXVHLFFBQVEsQ0FBQ3ZHLEtBQUssQ0FBQyxDQUFDd0csR0FBRyxDQUFDaEUsU0FBUztNQUFBLElBQUFxaEIsVUFBQTtNQUFBLFFBQUFBLFVBQUEsR0FBSTVpQixPQUFPLENBQUNKLEtBQUssRUFBRTJCLFNBQVMsQ0FBQ3ZDLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ3RDLENBQUMsQ0FBQyxjQUFBMmpCLFVBQUEsdUJBQXhDQSxVQUFBLENBQTBDamtCLElBQUk7SUFBQSxFQUFDLENBQUM2RyxNQUFNLENBQUU3RyxJQUFJLElBQXVCK0IsT0FBTyxDQUFDL0IsSUFBSSxDQUFDLENBQUM7SUFDOUpra0IsV0FBVyxFQUFFOWpCLEtBQUs7TUFBQSxJQUFBK2pCLGVBQUE7TUFBQSxRQUFBQSxlQUFBLEdBQUl4QixTQUFTLENBQUMzZCxHQUFHLENBQUM3RSxRQUFRLENBQUNDLEtBQUssQ0FBQyxDQUFDLGNBQUErakIsZUFBQSxjQUFBQSxlQUFBLEdBQUksRUFBRTtJQUFBO0lBQzFEQyxXQUFXLEVBQUVoa0IsS0FBSztNQUFBLElBQUFpa0IsZUFBQTtNQUFBLFFBQUFBLGVBQUEsR0FBSXpCLFNBQVMsQ0FBQzVkLEdBQUcsQ0FBQzdFLFFBQVEsQ0FBQ0MsS0FBSyxDQUFDLENBQUMsY0FBQWlrQixlQUFBLGNBQUFBLGVBQUEsR0FBSSxFQUFFO0lBQUE7RUFDNUQsQ0FBQztBQUNILENBQUM7QUFDRCxNQUFNQyxxQkFBcUIsR0FBR0EsQ0FBQ3JqQixLQUFZLEVBQUV3aEIsT0FBeUI7RUFBQSxJQUFBOEIsa0JBQUE7RUFBQSxRQUFBQSxrQkFBQSxHQUFrQjlCLE9BQU8sQ0FBQ25nQixTQUFTLGNBQUFpaUIsa0JBQUEsY0FBQUEsa0JBQUEsR0FBakI5QixPQUFPLENBQUNuZ0IsU0FBUyxHQUFLQyxxQkFBcUIsQ0FBQ3RCLEtBQUssQ0FBQztBQUFBO0FBQzFJLE1BQU11akIsZUFBZSxHQUFHQSxDQUFDdmpCLEtBQVksRUFBRXdoQixPQUF5QixFQUFFNWEsUUFBMkIsRUFBRTZhLFFBQW9DLEtBQXdCO0VBQ3pKLE1BQU1wZ0IsU0FBUyxHQUFHZ2lCLHFCQUFxQixDQUFDcmpCLEtBQUssRUFBRXdoQixPQUFPLENBQUM7RUFDdkQsTUFBTWdDLFNBQVMsR0FBR3RsQixlQUFlLENBQUMwSSxRQUFRLEVBQUUyYSxnQkFBZ0IsQ0FBQ3ZoQixLQUFLLEVBQUV3aEIsT0FBTyxFQUFFcmlCLEtBQUs7SUFBQSxJQUFBc2tCLFNBQUE7SUFBQSxPQUFJcGlCLFNBQVMsQ0FBQ2lCLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxNQUFBb2tCLFNBQUEsR0FBS2hDLFFBQVEsYUFBUkEsUUFBUSx1QkFBUkEsUUFBUSxDQUFHdGlCLEtBQUssQ0FBQyxjQUFBc2tCLFNBQUEsY0FBQUEsU0FBQSxHQUFJLElBQUksQ0FBQztFQUFBLEVBQUMsQ0FBQztFQUN0S2pDLE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQzdFLElBQUksQ0FBQ3dmLFNBQVMsQ0FBQ25lLEtBQUssQ0FBQztFQUN6QyxPQUFPbWUsU0FBUyxDQUFDcmtCLEtBQUs7QUFDeEIsQ0FBQztBQUVELFNBQVNnTixVQUFVQSxDQUFDbk0sS0FBWSxFQUFFaVcsUUFBNkIsRUFBRXVMLE9BQXlCLEVBQVE7RUFDaEcsTUFBTTNFLEtBQWtELEdBQUc7SUFDekQsa0JBQWtCLEVBQUUsb0JBQW9CO0lBQ3hDLGlCQUFpQixFQUFFLHNCQUFzQjtJQUN6QyxrQkFBa0IsRUFBRSxrQkFBa0I7SUFDdEMseUJBQXlCLEVBQUUsc0JBQXNCO0lBQ2pELHFCQUFxQixFQUFFO0VBQ3pCLENBQUM7RUFDRCxNQUFNNkcsVUFBVSxHQUFHLElBQUl4aUIsR0FBRyxDQUFDeWlCLE1BQU0sQ0FBQ0MsTUFBTSxDQUFDL0csS0FBSyxDQUFDLENBQUM7RUFDaEQsTUFBTWdILFdBQVcsR0FBRyxDQUFDLEdBQUdubUIsa0JBQWtCLENBQUNzQyxLQUFLLENBQUN3SCxLQUFLLENBQUMsQ0FBQyxDQUFDdEQsSUFBSSxDQUFDLENBQUNzUixJQUFJLEVBQUVJLEtBQUssS0FBS2hKLE1BQU0sQ0FBQzhXLFVBQVUsQ0FBQ3BoQixHQUFHLENBQUNrVCxJQUFJLENBQUN6VCxFQUFFLENBQUMsQ0FBQyxHQUFHNkssTUFBTSxDQUFDOFcsVUFBVSxDQUFDcGhCLEdBQUcsQ0FBQ3NULEtBQUssQ0FBQzdULEVBQUUsQ0FBQyxDQUFDLENBQUM7RUFDbEosSUFBSVYsU0FBUyxHQUFHZ2lCLHFCQUFxQixDQUFDcmpCLEtBQUssRUFBRXdoQixPQUFPLENBQUM7RUFDckQsTUFBTXNDLE9BQU8sR0FBRyxJQUFJemdCLEdBQUcsQ0FBc0IsQ0FBQztFQUM5QyxNQUFNOFksUUFBUSxHQUFHLElBQUlqYixHQUFHLENBQVMsQ0FDL0JuQixPQUFPLENBQUNDLEtBQUssRUFBRUEsS0FBSyxDQUFDK0MsS0FBSyxDQUFDM0QsQ0FBQyxFQUFFWSxLQUFLLENBQUMrQyxLQUFLLENBQUMxRCxDQUFDLENBQUMsRUFDNUNVLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFQSxLQUFLLENBQUN3RixJQUFJLENBQUNwRyxDQUFDLEVBQUVZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ25HLENBQUMsQ0FBQyxFQUMxQyxHQUFHMEgsZ0JBQWdCLENBQUMvRyxLQUFLLENBQUMsQ0FBQzJGLEdBQUcsQ0FBQ3hHLEtBQUssSUFBSVksT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLEVBQ3pFLEdBQUdXLEtBQUssQ0FBQ1EsTUFBTSxDQUFDbUYsR0FBRyxDQUFDakYsS0FBSyxJQUFJWCxPQUFPLENBQUNDLEtBQUssRUFBRVUsS0FBSyxDQUFDdEIsQ0FBQyxFQUFFc0IsS0FBSyxDQUFDckIsQ0FBQyxDQUFDLENBQUMsRUFDOUQsR0FBR1csS0FBSyxDQUFDbUosS0FBSyxDQUFDeEQsR0FBRyxDQUFDc2MsSUFBSSxJQUFJbGlCLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFaWlCLElBQUksQ0FBQzdpQixDQUFDLEVBQUU2aUIsSUFBSSxDQUFDNWlCLENBQUMsQ0FBQyxDQUFDLEVBQzFELEdBQUdXLEtBQUssQ0FBQ2UsS0FBSyxDQUFDNEUsR0FBRyxDQUFDaEQsSUFBSSxJQUFJNUMsT0FBTyxDQUFDQyxLQUFLLEVBQUUyQyxJQUFJLENBQUN2RCxDQUFDLEVBQUV1RCxJQUFJLENBQUN0RCxDQUFDLENBQUMsQ0FBQyxDQUMzRCxDQUFDO0VBQ0YsS0FBSyxNQUFNb2hCLFVBQVUsSUFBSW9ELFdBQVcsRUFBRTtJQUFBLElBQUFFLG9CQUFBO0lBQ3BDLE1BQU1DLFFBQVEsSUFBQUQsb0JBQUEsR0FBR0osTUFBTSxDQUFDM0YsT0FBTyxDQUFDbkIsS0FBSyxDQUFDLENBQUNwYyxJQUFJLENBQUMsQ0FBQyxHQUFHd2pCLFNBQVMsQ0FBQyxLQUFLQSxTQUFTLEtBQUt4RCxVQUFVLENBQUMxZSxFQUFFLENBQUMsY0FBQWdpQixvQkFBQSx1QkFBMUVBLG9CQUFBLENBQTZFLENBQUMsQ0FBNkI7SUFDNUgsTUFBTXJKLE1BQU0sR0FBR3NKLFFBQVEsR0FBR0YsT0FBTyxDQUFDL2YsR0FBRyxDQUFDaWdCLFFBQVEsQ0FBQyxHQUFHMWpCLFNBQVM7SUFDM0QsSUFBSW9qQixVQUFVLENBQUNwaEIsR0FBRyxDQUFDbWUsVUFBVSxDQUFDMWUsRUFBRSxDQUFDLElBQUksQ0FBQzJZLE1BQU0sRUFBRTtNQUM1QzhHLE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQzdFLElBQUksQ0FBQztRQUFFakMsRUFBRSxFQUFFLFFBQVEwZSxVQUFVLENBQUMxZSxFQUFFLEVBQUU7UUFBRW1pQixNQUFNLEVBQUUsQ0FBQztRQUFFQyxZQUFZLEVBQUUsS0FBSztRQUFFdGIsV0FBVyxFQUFFLENBQUMsa0JBQWtCNFgsVUFBVSxDQUFDMWUsRUFBRSx5Q0FBeUMsQ0FBQztRQUFFcWlCLFlBQVksRUFBRTtVQUFFQyxPQUFPLEVBQUUsQ0FBQyxHQUFHNUQsVUFBVSxDQUFDNEQsT0FBTztRQUFFO01BQUUsQ0FBQyxDQUFDO01BQ3RPO0lBQ0Y7SUFDQSxNQUFNckUsSUFBSSxHQUFHLElBQUk5ZSxHQUFHLENBQUN1ZixVQUFVLENBQUNULElBQUksQ0FBQztJQUNyQyxNQUFNc0UsSUFBSSxHQUFHN0QsVUFBVSxDQUFDMWUsRUFBRSxLQUFLLGlCQUFpQjtJQUNoRCxNQUFNcWlCLFlBQStCLEdBQUc7TUFDdENyaUIsRUFBRSxFQUFFLFFBQVEwZSxVQUFVLENBQUMxZSxFQUFFLEVBQUU7TUFDM0JxaUIsWUFBWSxFQUFFO1FBQ1pDLE9BQU8sRUFBRSxDQUFDLEdBQUc1RCxVQUFVLENBQUM0RCxPQUFPLENBQUM7UUFDaENFLFdBQVcsRUFBRXZFLElBQUksQ0FBQzFkLEdBQUcsQ0FBQyxPQUFPLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQztRQUN0QyxJQUFJa2YsT0FBTyxDQUFDelksS0FBSyxJQUFJaVgsSUFBSSxDQUFDMWQsR0FBRyxDQUFDLE9BQU8sQ0FBQyxHQUFHZ2lCLElBQUksR0FBRztVQUFFNUMsU0FBUyxFQUFFLENBQUMsVUFBVTtRQUFxQixDQUFDLEdBQUc7VUFBRUMsU0FBUyxFQUFFLENBQUMsTUFBTTtRQUFxQixDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUM7UUFDakosSUFBSUgsT0FBTyxDQUFDelksS0FBSyxJQUFJaVgsSUFBSSxDQUFDMWQsR0FBRyxDQUFDLE9BQU8sQ0FBQyxHQUFHO1VBQUVvZixTQUFTLEVBQUUsQ0FBQyxnQkFBZ0I7UUFBcUIsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDO1FBQ25HLElBQUloSCxNQUFNLEdBQUc7VUFBRThKLElBQUksRUFBRTlKLE1BQU07VUFBRStKLFlBQVksRUFBRTtRQUFHLENBQUMsR0FBRyxDQUFDLENBQUM7TUFDdEQ7SUFDRixDQUFDO0lBQ0QsTUFBTXRsQixLQUFLLEdBQUdva0IsZUFBZSxDQUFDdmpCLEtBQUssRUFBRXdoQixPQUFPLEVBQUU0QyxZQUFZLEVBQUV6aUIsU0FBUyxJQUFJTixTQUFTLENBQUNpQixHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRTJCLFNBQVMsQ0FBQ3ZDLENBQUMsRUFBRXVDLFNBQVMsQ0FBQ3RDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQzRXLFFBQVEsQ0FBQzNULEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFMkIsU0FBUyxDQUFDdkMsQ0FBQyxFQUFFdUMsU0FBUyxDQUFDdEMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDOGMsUUFBUSxDQUFDN1osR0FBRyxDQUFDdkMsT0FBTyxDQUFDQyxLQUFLLEVBQUUyQixTQUFTLENBQUN2QyxDQUFDLEVBQUV1QyxTQUFTLENBQUN0QyxDQUFDLENBQUMsQ0FBQyxJQUFJbVEsY0FBYyxDQUFDeFAsS0FBSyxFQUFFeWdCLFVBQVUsQ0FBQzFlLEVBQUUsRUFBRUosU0FBUyxDQUFDLENBQUM7SUFDMVMsSUFBSSxDQUFDeEMsS0FBSyxFQUFFO0lBQ1osTUFBTXdELElBQVUsR0FBRztNQUNqQlosRUFBRSxFQUFFLFFBQVEvQixLQUFLLENBQUNFLEtBQUssSUFBSXVnQixVQUFVLENBQUMxZSxFQUFFLElBQUk1QyxLQUFLLENBQUNDLENBQUMsSUFBSUQsS0FBSyxDQUFDRSxDQUFDLEVBQUU7TUFDaEVOLElBQUksRUFBRTBoQixVQUFVLENBQUMxZSxFQUFFO01BQ25CM0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNDLENBQUM7TUFDVkMsQ0FBQyxFQUFFRixLQUFLLENBQUNFLENBQUM7TUFDVm1JLEtBQUssRUFBRXhILEtBQUssQ0FBQ3dILEtBQUs7TUFDbEJsRSxLQUFLLEVBQUUsU0FBUztNQUNoQjBjLElBQUksRUFBRSxDQUFDLEdBQUdTLFVBQVUsQ0FBQ1QsSUFBSSxDQUFDO01BQzFCQyxLQUFLLEVBQUUsQ0FBQyxHQUFHUSxVQUFVLENBQUNSLEtBQUs7SUFDN0IsQ0FBQztJQUNELElBQUkzaUIsY0FBYyxDQUFDcUYsSUFBSSxDQUFDLEVBQUU7TUFDeEIzQyxLQUFLLENBQUNlLEtBQUssQ0FBQ2lELElBQUksQ0FBQ3JCLElBQUksQ0FBQztNQUN0QixNQUFNK2hCLGtCQUFrQixHQUFHL2YsZUFBZSxDQUFDM0UsS0FBSyxFQUFFQSxLQUFLLENBQUMrQyxLQUFLLEVBQUUvQyxLQUFLLENBQUN3RixJQUFJLENBQUM7TUFDMUUsTUFBTW1mLHVCQUF1QixHQUFHNWQsZ0JBQWdCLENBQUMvRyxLQUFLLENBQUMsQ0FBQ21ILElBQUksQ0FBQ3RHLE1BQU0sSUFBSStqQiwwQkFBMEIsQ0FBQzVrQixLQUFLLEVBQUVhLE1BQU0sQ0FBQyxDQUFDO01BQ2pIYixLQUFLLENBQUNlLEtBQUssQ0FBQzhqQixHQUFHLENBQUMsQ0FBQztNQUNqQixJQUFJLENBQUNILGtCQUFrQixJQUFJLENBQUNDLHVCQUF1QixFQUFFO1FBQ25ELE1BQU10ZixLQUFLLEdBQUdtYyxPQUFPLENBQUMzWSxXQUFXLENBQUMrRyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUM7UUFDeEMsSUFBSXZLLEtBQUssRUFBRTtVQUFFLE9BQU9BLEtBQUssQ0FBQ3lmLFFBQVE7VUFBRXpmLEtBQUssQ0FBQ3dELFdBQVcsQ0FBQzdFLElBQUksQ0FBQyxrQkFBa0J5YyxVQUFVLENBQUMxZSxFQUFFLGdEQUFnRCxDQUFDO1FBQUM7UUFDNUk7TUFDRjtJQUNGO0lBQ0EvQixLQUFLLENBQUNlLEtBQUssQ0FBQ2lELElBQUksQ0FBQ3JCLElBQUksQ0FBQztJQUN0QndaLFFBQVEsQ0FBQzNYLEdBQUcsQ0FBQ3pFLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQztJQUM5QyxJQUFJL0IsY0FBYyxDQUFDcUYsSUFBSSxDQUFDLEVBQUU7TUFDeEI2ZSxPQUFPLENBQUNuZ0IsU0FBUyxHQUFHZixTQUFTO01BQzdCZSxTQUFTLEdBQUdnaUIscUJBQXFCLENBQUNyakIsS0FBSyxFQUFFd2hCLE9BQU8sQ0FBQztJQUNuRDtJQUNBLElBQUkzRSxLQUFLLENBQUM0RCxVQUFVLENBQUMxZSxFQUFFLENBQUMsRUFBRStoQixPQUFPLENBQUN2Z0IsR0FBRyxDQUFDa2QsVUFBVSxDQUFDMWUsRUFBRSxFQUFFNUMsS0FBSyxDQUFDO0VBQzdEO0FBQ0Y7QUFFQSxTQUFTaU4sZUFBZUEsQ0FBQ3BNLEtBQVksRUFBRXdoQixPQUF5QixFQUFRO0VBQ3RFLE1BQU11RCxLQUFLLEdBQUcsQ0FDWjtJQUFFaGpCLEVBQUUsRUFBRSxVQUFVO0lBQUVoRCxJQUFJLEVBQUUsVUFBbUI7SUFBRWltQixTQUFTLEVBQUUsVUFBbUI7SUFBRUMsT0FBTyxFQUFFO01BQUV2RCxTQUFTLEVBQUUsQ0FBQyxVQUFVLENBQW9CO01BQUU2QyxXQUFXLEVBQUU7SUFBRSxDQUFDO0lBQUVXLE1BQU0sRUFBRTtNQUFFWCxXQUFXLEVBQUUsQ0FBQztNQUFFWSxXQUFXLEVBQUU7SUFBRztFQUFFLENBQUMsRUFDbk07SUFBRXBqQixFQUFFLEVBQUUsWUFBWTtJQUFFaEQsSUFBSSxFQUFFLE1BQWU7SUFBRWltQixTQUFTLEVBQUUsWUFBcUI7SUFBRUMsT0FBTyxFQUFFO01BQUV2RCxTQUFTLEVBQUUsQ0FBQyxNQUFNLENBQW9CO01BQUU2QyxXQUFXLEVBQUU7SUFBRSxDQUFDO0lBQUVXLE1BQU0sRUFBRTtNQUFFWCxXQUFXLEVBQUUsQ0FBQztNQUFFWSxXQUFXLEVBQUU7SUFBRztFQUFFLENBQUMsRUFDL0w7SUFBRXBqQixFQUFFLEVBQUUsV0FBVztJQUFFaEQsSUFBSSxFQUFFLE1BQWU7SUFBRWltQixTQUFTLEVBQUUsV0FBb0I7SUFBRUMsT0FBTyxFQUFFO01BQUV0RCxTQUFTLEVBQUUsQ0FBQyxRQUFRLENBQW9CO01BQUV5RCxTQUFTLEVBQUUsUUFBaUI7TUFBRWIsV0FBVyxFQUFFO0lBQUUsQ0FBQztJQUFFVyxNQUFNLEVBQUU7TUFBRVgsV0FBVyxFQUFFLEVBQUU7TUFBRWMsVUFBVSxFQUFFO0lBQU07RUFBRSxDQUFDLEVBQ2hPO0lBQUV0akIsRUFBRSxFQUFFLGFBQWE7SUFBRWhELElBQUksRUFBRSxNQUFlO0lBQUVpbUIsU0FBUyxFQUFFLGFBQXNCO0lBQUVDLE9BQU8sRUFBRTtNQUFFdkQsU0FBUyxFQUFFLENBQUMsZ0JBQWdCLENBQW9CO01BQUU2QyxXQUFXLEVBQUU7SUFBRyxDQUFDO0lBQUVXLE1BQU0sRUFBRTtNQUFFWCxXQUFXLEVBQUU7SUFBRztFQUFFLENBQUMsRUFDNUw7SUFBRXhpQixFQUFFLEVBQUUsU0FBUztJQUFFaEQsSUFBSSxFQUFFLFNBQWtCO0lBQUVrbUIsT0FBTyxFQUFFO01BQUV2RCxTQUFTLEVBQUUsQ0FBQyxXQUFXLENBQW9CO01BQUU2QyxXQUFXLEVBQUU7SUFBRyxDQUFDO0lBQUVXLE1BQU0sRUFBRTtNQUFFWCxXQUFXLEVBQUU7SUFBRztFQUFFLENBQUMsQ0FDcEo7RUFDRHZrQixLQUFLLENBQUN5SixVQUFVLEdBQUcsRUFBRTtFQUNyQixLQUFLLE1BQU02YixJQUFJLElBQUlQLEtBQUssRUFBRTtJQUFBLElBQUFRLHFCQUFBO0lBQ3hCLE1BQU0zZSxRQUEyQixHQUFHO01BQUU3RSxFQUFFLEVBQUUsYUFBYXVqQixJQUFJLENBQUN2akIsRUFBRSxFQUFFO01BQUVxaUIsWUFBWSxFQUFFNUMsT0FBTyxDQUFDelksS0FBSyxHQUFHdWMsSUFBSSxDQUFDTCxPQUFPLEdBQUdLLElBQUksQ0FBQ0osTUFBTTtNQUFFLElBQUkxRCxPQUFPLENBQUN6WSxLQUFLLEdBQUc7UUFBRXljLFFBQVEsRUFBRUYsSUFBSSxDQUFDSjtNQUFPLENBQUMsR0FBRyxDQUFDLENBQUM7SUFBRSxDQUFDO0lBQ2pMLE1BQU0vbEIsS0FBSyxHQUFHb2tCLGVBQWUsQ0FBQ3ZqQixLQUFLLEVBQUV3aEIsT0FBTyxFQUFFNWEsUUFBUSxFQUFFakYsU0FBUyxJQUFJLENBQUNBLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDd0YsSUFBSSxDQUFDcEcsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUN3RixJQUFJLENBQUNuRyxDQUFDLE1BQU1XLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxRQUFRLElBQUk4ZCxJQUFJLENBQUN2bUIsSUFBSSxLQUFLLFNBQVMsSUFBSWlCLEtBQUssQ0FBQ0ssS0FBSyxDQUFDd0YsS0FBSyxDQUFDLENBQUMvRyxJQUFJLEVBQUVvQixLQUFLLEtBQUtwQixJQUFJLENBQUNDLElBQUksS0FBSyxPQUFPLElBQUltSCxJQUFJLENBQUNDLEdBQUcsQ0FBQ0QsSUFBSSxDQUFDa0osR0FBRyxDQUFDek4sU0FBUyxDQUFDdkMsQ0FBQyxHQUFHYyxLQUFLLEdBQUdGLEtBQUssQ0FBQ3lJLEtBQUssQ0FBQyxFQUFFdkMsSUFBSSxDQUFDa0osR0FBRyxDQUFDek4sU0FBUyxDQUFDdEMsQ0FBQyxHQUFHNkcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDRSxLQUFLLEdBQUdGLEtBQUssQ0FBQ3lJLEtBQUssQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDO0lBQ2hYLElBQUksQ0FBQ3RKLEtBQUssRUFBRSxNQUFNLElBQUlvSSxLQUFLLENBQUMsb0JBQW9CWCxRQUFRLENBQUM3RSxFQUFFLE1BQUF3akIscUJBQUEsR0FBSy9ELE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQytHLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxjQUFBMlYscUJBQUEsdUJBQTFCQSxxQkFBQSxDQUE0QjFjLFdBQVcsQ0FBQ2xCLElBQUksQ0FBQyxJQUFJLENBQUMsRUFBRSxDQUFDO0lBQ3JIM0gsS0FBSyxDQUFDeUosVUFBVSxDQUFDekYsSUFBSSxDQUFDO01BQUVqQyxFQUFFLEVBQUUsYUFBYS9CLEtBQUssQ0FBQ0UsS0FBSyxJQUFJb2xCLElBQUksQ0FBQ3ZqQixFQUFFLElBQUk1QyxLQUFLLENBQUNDLENBQUMsSUFBSUQsS0FBSyxDQUFDRSxDQUFDLEVBQUU7TUFBRU4sSUFBSSxFQUFFdW1CLElBQUksQ0FBQ3ZtQixJQUFJO01BQUUsSUFBSXVtQixJQUFJLENBQUNOLFNBQVMsR0FBRztRQUFFQSxTQUFTLEVBQUVNLElBQUksQ0FBQ047TUFBVSxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUM7TUFBRSxHQUFHN2xCLEtBQUs7TUFBRXNtQixVQUFVLEVBQUUsS0FBSztNQUFFQyxPQUFPLEVBQUU7SUFBTSxDQUFDLENBQUM7RUFDcE47QUFDRjtBQUVBLFNBQVNyWixlQUFlQSxDQUFDck0sS0FBWSxFQUFFb1EsR0FBUSxFQUFFb1IsT0FBeUIsRUFBUTtFQUFBLElBQUFtRSxrQkFBQTtFQUNoRixNQUFNL2UsUUFBMkIsR0FBRzRhLE9BQU8sQ0FBQ3pZLEtBQUssR0FDN0M7SUFBRWhILEVBQUUsRUFBRSwwQkFBMEI7SUFBRXFpQixZQUFZLEVBQUU7TUFBRXpDLFNBQVMsRUFBRSxDQUFDLFFBQVEsQ0FBQztNQUFFeUQsU0FBUyxFQUFFLFFBQVE7TUFBRWIsV0FBVyxFQUFFLENBQUM7TUFBRXFCLEtBQUssRUFBRTtJQUFLLENBQUM7SUFBRUosUUFBUSxFQUFFO01BQUVqQixXQUFXLEVBQUUsQ0FBQztNQUFFRixPQUFPLEVBQUUsQ0FBQyxPQUFPO0lBQUU7RUFBRSxDQUFDLEdBQy9LO0lBQUV0aUIsRUFBRSxFQUFFLDBCQUEwQjtJQUFFcWlCLFlBQVksRUFBRTtNQUFFRyxXQUFXLEVBQUUsQ0FBQztNQUFFRixPQUFPLEVBQUUsQ0FBQyxPQUFPO0lBQUU7RUFBRSxDQUFDO0VBQzVGLE1BQU1sbEIsS0FBSyxHQUFHb2tCLGVBQWUsQ0FBQ3ZqQixLQUFLLEVBQUV3aEIsT0FBTyxFQUFFNWEsUUFBUSxFQUFFakYsU0FBUyxJQUFJQSxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxDQUFDO0VBQ2xJLElBQUksQ0FBQ0YsS0FBSyxFQUFFO0VBQ1osTUFBTTBtQixPQUF5RCxHQUFHO0lBQ2hFelksSUFBSSxFQUFFLENBQUMsVUFBVSxFQUFFLFVBQVUsQ0FBQztJQUFFQyxLQUFLLEVBQUUsQ0FBQyxXQUFXLEVBQUUsV0FBVyxDQUFDO0lBQUVDLE9BQU8sRUFBRSxDQUFDLGFBQWEsRUFBRSxhQUFhLENBQUM7SUFBRUMsS0FBSyxFQUFFLENBQUMsV0FBVyxFQUFFLFdBQVcsQ0FBQztJQUFFQyxPQUFPLEVBQUUsQ0FBQyxhQUFhLEVBQUUsYUFBYSxDQUFDO0lBQUVDLFlBQVksRUFBRSxDQUFDLGFBQWEsRUFBRSxhQUFhLENBQUM7SUFBRUMsTUFBTSxFQUFFLENBQUMsWUFBWSxFQUFFLFlBQVksQ0FBQztJQUFFQyxNQUFNLEVBQUUsQ0FBQyxZQUFZLEVBQUUsWUFBWSxDQUFDO0lBQUVDLFNBQVMsRUFBRSxDQUFDLFVBQVUsRUFBRSxVQUFVLENBQUM7SUFBRUMsY0FBYyxFQUFFLENBQUMsV0FBVyxFQUFFLFdBQVc7RUFDbFksQ0FBQztFQUNELE1BQU1ZLEtBQStCLEdBQUd6TyxLQUFLLENBQUN3SCxLQUFLLEtBQUssUUFBUSxHQUFHLENBQUMsWUFBWSxFQUFFLFdBQVcsRUFBRSxjQUFjLEVBQUUsR0FBR3FlLE9BQU8sQ0FBQ25ZLE1BQU0sQ0FBQyxHQUM3SDFOLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxRQUFRLEdBQUcsQ0FBQyxjQUFjLEVBQUUsYUFBYSxFQUFFLGNBQWMsRUFBRSxHQUFHcWUsT0FBTyxDQUFDbFksTUFBTSxDQUFDLEdBQzNGM04sS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFdBQVcsR0FBRyxDQUFDLFlBQVksRUFBRSxjQUFjLEVBQUUsV0FBVyxFQUFFLFlBQVksRUFBRSxXQUFXLEVBQUUsV0FBVyxFQUFFLEdBQUdxZSxPQUFPLENBQUNqWSxTQUFTLENBQUMsR0FDckk1TixLQUFLLENBQUN3SCxLQUFLLEtBQUssZ0JBQWdCLEdBQUcsQ0FBQyxTQUFTLEVBQUUsYUFBYSxFQUFFLGNBQWMsRUFBRSxZQUFZLEVBQUUsVUFBVSxFQUFFLGdCQUFnQixFQUFFLEdBQUdxZSxPQUFPLENBQUNoWSxjQUFjLENBQUMsR0FDbEosQ0FBQyxVQUFVLEVBQUUsY0FBYyxFQUFFLGlCQUFpQixFQUFFLFVBQVUsRUFBRSxjQUFjLEVBQUUsR0FBR2dZLE9BQU8sQ0FBQzdsQixLQUFLLENBQUN3SCxLQUFLLENBQUMsQ0FBQztFQUM5RyxNQUFNc2UsTUFBTSxHQUFHcm5CLGlCQUFpQixDQUFDO0lBQUVzSixJQUFJLEVBQUUvSCxLQUFLLENBQUMrSCxJQUFJO0lBQUVsTCxVQUFVLEVBQUVtRCxLQUFLLENBQUNFLEtBQUs7SUFBRXNILEtBQUssRUFBRXhILEtBQUssQ0FBQ3dILEtBQUs7SUFBRVcsUUFBUSxFQUFFbkksS0FBSyxDQUFDeUgsUUFBUTtJQUFFWSxLQUFLLEdBQUFzZCxrQkFBQSxHQUFFM2xCLEtBQUssQ0FBQ2lGLFVBQVUsY0FBQTBnQixrQkFBQSx1QkFBaEJBLGtCQUFBLENBQWtCdGQ7RUFBTSxDQUFDLENBQUM7RUFDN0osTUFBTTBkLFNBQVMsR0FBR3JuQixzQkFBc0IsQ0FBQztJQUFFcUosSUFBSSxFQUFFL0gsS0FBSyxDQUFDK0gsSUFBSTtJQUFFbEwsVUFBVSxFQUFFbUQsS0FBSyxDQUFDRSxLQUFLO0lBQUVzSCxLQUFLLEVBQUV4SCxLQUFLLENBQUN3SCxLQUFLO0lBQUVXLFFBQVEsRUFBRW5JLEtBQUssQ0FBQ3lILFFBQVE7SUFBRXVlLE1BQU0sRUFBRUYsTUFBTSxHQUFHLFFBQVEsR0FBRztFQUFZLENBQUMsQ0FBQztFQUM5SzlsQixLQUFLLENBQUNvSixVQUFVLEdBQUcsQ0FBQztJQUFFckgsRUFBRSxFQUFFLGFBQWEvQixLQUFLLENBQUNFLEtBQUssSUFBSWYsS0FBSyxDQUFDQyxDQUFDLElBQUlELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFO0lBQUVOLElBQUksRUFBRSttQixNQUFNLEdBQUcsVUFBVSxHQUFHMVYsR0FBRyxDQUFDNlYsSUFBSSxDQUFDeFgsS0FBSyxDQUFDO0lBQUUsR0FBR3RQLEtBQUs7SUFBRW1FLEtBQUssRUFBRSxTQUFTO0lBQUUsSUFBSXdpQixNQUFNLEdBQUc7TUFBRUE7SUFBTyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUM7SUFBRSxJQUFJQyxTQUFTLEdBQUc7TUFBRUE7SUFBVSxDQUFDLEdBQUcsQ0FBQyxDQUFDO0VBQUUsQ0FBQyxDQUFDO0FBQzVOO0FBRUEsTUFBTTVVLE1BQU0sR0FBR0EsQ0FBQ25SLEtBQVksRUFBRWtKLElBQVksRUFBRThILEVBQVUsRUFBRTNSLENBQVMsS0FBSztFQUFFLEtBQUssSUFBSUQsQ0FBQyxHQUFHOEcsSUFBSSxDQUFDRSxHQUFHLENBQUM4QyxJQUFJLEVBQUU4SCxFQUFFLENBQUMsRUFBRTVSLENBQUMsSUFBSThHLElBQUksQ0FBQ0MsR0FBRyxDQUFDK0MsSUFBSSxFQUFFOEgsRUFBRSxDQUFDLEVBQUU1UixDQUFDLEVBQUUsRUFBRTJLLE9BQU8sQ0FBQy9KLEtBQUssRUFBRVosQ0FBQyxFQUFFQyxDQUFDLEVBQUUsT0FBTyxDQUFDO0FBQUMsQ0FBQztBQUN0SyxNQUFNNlIsTUFBTSxHQUFHQSxDQUFDbFIsS0FBWSxFQUFFa0osSUFBWSxFQUFFOEgsRUFBVSxFQUFFNVIsQ0FBUyxLQUFLO0VBQUUsS0FBSyxJQUFJQyxDQUFDLEdBQUc2RyxJQUFJLENBQUNFLEdBQUcsQ0FBQzhDLElBQUksRUFBRThILEVBQUUsQ0FBQyxFQUFFM1IsQ0FBQyxJQUFJNkcsSUFBSSxDQUFDQyxHQUFHLENBQUMrQyxJQUFJLEVBQUU4SCxFQUFFLENBQUMsRUFBRTNSLENBQUMsRUFBRSxFQUFFMEssT0FBTyxDQUFDL0osS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsRUFBRSxPQUFPLENBQUM7QUFBQyxDQUFDO0FBQ3RLLE1BQU0wSyxPQUFPLEdBQUdBLENBQUMvSixLQUFZLEVBQUVaLENBQVMsRUFBRUMsQ0FBUyxFQUFFTixJQUFrQixLQUFLO0VBQzFFLElBQUksQ0FBQ29CLFFBQVEsQ0FBQ0gsS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsQ0FBQyxFQUFFO0VBQzVCLE1BQU1QLElBQUksR0FBR2tCLEtBQUssQ0FBQ0ssS0FBSyxDQUFDTixPQUFPLENBQUNDLEtBQUssRUFBRVosQ0FBQyxFQUFFQyxDQUFDLENBQUMsQ0FBQztFQUM5Q1AsSUFBSSxDQUFDQyxJQUFJLEdBQUdBLElBQUk7RUFDaEIsSUFBSUEsSUFBSSxLQUFLLFNBQVMsRUFBRSxPQUFPRCxJQUFJLENBQUN1WixJQUFJO0FBQzFDLENBQUM7QUFDRCxNQUFNNk4sU0FBUyxHQUFJbG1CLEtBQVksSUFBY0EsS0FBSyxDQUFDSyxLQUFLLENBQUMrVixPQUFPLENBQUMsQ0FBQzFULE9BQU8sRUFBRXhDLEtBQUssS0FBS3dDLE9BQU8sQ0FBQzNELElBQUksS0FBSyxPQUFPLEdBQUcsQ0FBQ2tCLE9BQU8sQ0FBQ0QsS0FBSyxFQUFFRSxLQUFLLENBQUMsQ0FBQyxHQUFHLEVBQUUsQ0FBQyxDQUFDMEYsTUFBTSxDQUFDekcsS0FBSyxJQUFJcVQsUUFBUSxDQUFDclQsS0FBSyxFQUFFYSxLQUFLLENBQUMrQyxLQUFLLENBQUMsR0FBRyxDQUFDLElBQUl5UCxRQUFRLENBQUNyVCxLQUFLLEVBQUVhLEtBQUssQ0FBQ3dGLElBQUksQ0FBQyxHQUFHLENBQUMsQ0FBQztBQUNsTyxNQUFNMmdCLFlBQVksR0FBR0EsQ0FBQ25tQixLQUFZLEVBQUV3WCxLQUFhLEtBQWF0UixJQUFJLENBQUNDLEdBQUcsQ0FBQ3FSLEtBQUssRUFBRXRSLElBQUksQ0FBQzBPLEtBQUssQ0FBQzRDLEtBQUssR0FBR3hYLEtBQUssQ0FBQ0ssS0FBSyxDQUFDd0QsTUFBTSxJQUFJakgsU0FBUyxHQUFHRCxVQUFVLENBQUMsQ0FBQyxDQUFDO0FBQ2hKLE1BQU15cEIsS0FBSyxHQUFHQSxDQUFDcG1CLEtBQVksRUFBRWtKLElBQVksRUFBRThILEVBQVUsRUFBRTNSLENBQVMsS0FBSztFQUFFLEtBQUssSUFBSUQsQ0FBQyxHQUFHOEcsSUFBSSxDQUFDRSxHQUFHLENBQUM4QyxJQUFJLEVBQUU4SCxFQUFFLENBQUMsRUFBRTVSLENBQUMsSUFBSThHLElBQUksQ0FBQ0MsR0FBRyxDQUFDK0MsSUFBSSxFQUFFOEgsRUFBRSxDQUFDLEVBQUU1UixDQUFDLEVBQUU7SUFBQSxJQUFBaW5CLFVBQUE7SUFBRSxJQUFJLEVBQUFBLFVBQUEsR0FBQWptQixPQUFPLENBQUNKLEtBQUssRUFBRVosQ0FBQyxFQUFFQyxDQUFDLENBQUMsY0FBQWduQixVQUFBLHVCQUFwQkEsVUFBQSxDQUFzQnRuQixJQUFJLE1BQUssT0FBTyxFQUFFZ0wsT0FBTyxDQUFDL0osS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsRUFBRSxNQUFNLENBQUM7RUFBQTtBQUFDLENBQUM7QUFDaE4sTUFBTWluQixLQUFLLEdBQUdBLENBQUN0bUIsS0FBWSxFQUFFa0osSUFBWSxFQUFFOEgsRUFBVSxFQUFFNVIsQ0FBUyxLQUFLO0VBQUUsS0FBSyxJQUFJQyxDQUFDLEdBQUc2RyxJQUFJLENBQUNFLEdBQUcsQ0FBQzhDLElBQUksRUFBRThILEVBQUUsQ0FBQyxFQUFFM1IsQ0FBQyxJQUFJNkcsSUFBSSxDQUFDQyxHQUFHLENBQUMrQyxJQUFJLEVBQUU4SCxFQUFFLENBQUMsRUFBRTNSLENBQUMsRUFBRTtJQUFBLElBQUFrbkIsVUFBQTtJQUFFLElBQUksRUFBQUEsVUFBQSxHQUFBbm1CLE9BQU8sQ0FBQ0osS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsQ0FBQyxjQUFBa25CLFVBQUEsdUJBQXBCQSxVQUFBLENBQXNCeG5CLElBQUksTUFBSyxPQUFPLEVBQUVnTCxPQUFPLENBQUMvSixLQUFLLEVBQUVaLENBQUMsRUFBRUMsQ0FBQyxFQUFFLE1BQU0sQ0FBQztFQUFBO0FBQUMsQ0FBQztBQUVoTixTQUFTMkssYUFBYUEsQ0FBQ2hLLEtBQVksRUFBRW9RLEdBQVEsRUFBRXpHLEtBQWEsRUFBUTtFQUNsRSxJQUFJM0osS0FBSyxDQUFDd0gsS0FBSyxLQUFLLE1BQU0sRUFBRWdmLFlBQVksQ0FBQ3htQixLQUFLLEVBQUVvUSxHQUFHLEVBQUV6RyxLQUFLLENBQUM7RUFDM0QsSUFBSTNKLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxPQUFPLEVBQUVpZixhQUFhLENBQUN6bUIsS0FBSyxFQUFFb1EsR0FBRyxDQUFDO0VBQ3RELElBQUlwUSxLQUFLLENBQUN3SCxLQUFLLEtBQUssU0FBUyxFQUFFa2YsZUFBZSxDQUFDMW1CLEtBQUssRUFBRW9RLEdBQUcsQ0FBQztFQUMxRCxJQUFJcFEsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLE9BQU8sRUFBRW1mLGFBQWEsQ0FBQzNtQixLQUFLLEVBQUVvUSxHQUFHLEVBQUV6RyxLQUFLLENBQUM7RUFDN0QsSUFBSTNKLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxTQUFTLEVBQUVvZixlQUFlLENBQUM1bUIsS0FBSyxFQUFFb1EsR0FBRyxDQUFDO0VBQzFELElBQUlwUSxLQUFLLENBQUN3SCxLQUFLLEtBQUssY0FBYyxFQUFFcWYsb0JBQW9CLENBQUM3bUIsS0FBSyxFQUFFb1EsR0FBRyxDQUFDO0VBQ3BFLElBQUlwUSxLQUFLLENBQUN3SCxLQUFLLEtBQUssUUFBUSxFQUFFc2YsY0FBYyxDQUFDOW1CLEtBQUssRUFBRW9RLEdBQUcsQ0FBQztFQUN4RCxJQUFJcFEsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFFBQVEsRUFBRXVmLGNBQWMsQ0FBQy9tQixLQUFLLEVBQUVvUSxHQUFHLENBQUM7RUFDeEQsSUFBSXBRLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxXQUFXLEVBQUV3ZixpQkFBaUIsQ0FBQ2huQixLQUFLLEVBQUVvUSxHQUFHLENBQUM7RUFDOUQsSUFBSXBRLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxnQkFBZ0IsRUFBRXlmLHNCQUFzQixDQUFDam5CLEtBQUssRUFBRW9RLEdBQUcsQ0FBQztFQUN4RSxJQUFJcFEsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsRUFBRTtJQUN6QixNQUFNNFcsT0FBTyxHQUFHbk4sS0FBSyxDQUFDQSxLQUFLLENBQUM5RixNQUFNLEdBQUcsQ0FBQyxDQUFDO0lBQ3ZDLEtBQUssSUFBSXhFLENBQUMsR0FBR3lYLE9BQU8sQ0FBQ3pYLENBQUMsRUFBRUEsQ0FBQyxHQUFHeVgsT0FBTyxDQUFDelgsQ0FBQyxHQUFHeVgsT0FBTyxDQUFDOUosQ0FBQyxFQUFFM04sQ0FBQyxFQUFFLEVBQUUsS0FBSyxJQUFJRCxDQUFDLEdBQUcwWCxPQUFPLENBQUMxWCxDQUFDLEVBQUVBLENBQUMsR0FBRzBYLE9BQU8sQ0FBQzFYLENBQUMsR0FBRzBYLE9BQU8sQ0FBQy9KLENBQUMsRUFBRTNOLENBQUMsRUFBRSxFQUFFMkssT0FBTyxDQUFDL0osS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsRUFBRSxPQUFPLENBQUM7SUFDN0kwSyxPQUFPLENBQUMvSixLQUFLLEVBQUVBLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsRUFBRVksS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxFQUFFLE1BQU0sQ0FBQztFQUNwRDtBQUNGO0FBRUEsU0FBUzRLLG1CQUFtQkEsQ0FBQ2pLLEtBQVksRUFBRW9RLEdBQVEsRUFBRXpHLEtBQWEsRUFBUTtFQUFBLElBQUF1ZCxnQkFBQTtFQUN4RSxNQUFNQyxTQUFTLEdBQUdocUIsa0JBQWtCLENBQUM2QyxLQUFLLENBQUN3SCxLQUFLLENBQUM7RUFDakQsSUFBSSxDQUFDMmYsU0FBUyxDQUFDdGpCLE1BQU0sRUFBRTtFQUN2QixNQUFNdWpCLFFBQVEsR0FBR2hYLEdBQUcsQ0FBQzZWLElBQUksQ0FBQ2tCLFNBQVMsQ0FBQztFQUNwQyxNQUFNcmEsSUFBSSxHQUFHc0QsR0FBRyxDQUFDNlYsSUFBSSxDQUFDdGMsS0FBSyxDQUFDcUwsS0FBSyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDblIsTUFBTSxHQUFHOEYsS0FBSyxDQUFDcUwsS0FBSyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxHQUFHckwsS0FBSyxDQUFDO0VBQzdFLE1BQU14SyxLQUFLLEdBQUcySyxNQUFNLENBQUNnRCxJQUFJLENBQUM7RUFDMUIsS0FBSyxNQUFNdWEsU0FBUyxJQUFJRCxRQUFRLENBQUN0ZSxVQUFVLEVBQUU7SUFDM0MsTUFBTWhLLElBQUksR0FBR3NCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsR0FBR2lvQixTQUFTLENBQUNwbEIsRUFBRSxFQUFFOUMsS0FBSyxDQUFDRSxDQUFDLEdBQUdnb0IsU0FBUyxDQUFDbmxCLEVBQUUsQ0FBQztJQUMzRSxJQUFJcEQsSUFBSSxJQUFJQSxJQUFJLENBQUNDLElBQUksS0FBSyxNQUFNLElBQUlELElBQUksQ0FBQ0MsSUFBSSxLQUFLLE1BQU0sRUFBRTtNQUFFRCxJQUFJLENBQUNDLElBQUksR0FBR3NvQixTQUFTLENBQUN0b0IsSUFBSTtNQUFFLElBQUlzb0IsU0FBUyxDQUFDdG9CLElBQUksS0FBSyxTQUFTLEVBQUUsT0FBT0QsSUFBSSxDQUFDdVosSUFBSTtJQUFDO0VBQzdJO0VBQ0FyWSxLQUFLLENBQUNzbkIsU0FBUyxHQUFHLENBQUMsS0FBQUosZ0JBQUEsR0FBSWxuQixLQUFLLENBQUNzbkIsU0FBUyxjQUFBSixnQkFBQSxjQUFBQSxnQkFBQSxHQUFJLEVBQUUsQ0FBQyxFQUFFRSxRQUFRLENBQUNybEIsRUFBRSxDQUFDO0FBQzdEO0FBRUEsU0FBU3lrQixZQUFZQSxDQUFDeG1CLEtBQVksRUFBRW9RLEdBQVEsRUFBRXpHLEtBQWEsRUFBUTtFQUNqRSxLQUFLLElBQUkwWCxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUcxWCxLQUFLLENBQUM5RixNQUFNLEVBQUV3ZCxDQUFDLEVBQUUsRUFBRTtJQUNyQyxNQUFNblksSUFBSSxHQUFHWSxNQUFNLENBQUNILEtBQUssQ0FBQzBYLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQztJQUNqQyxNQUFNclEsRUFBRSxHQUFHbEgsTUFBTSxDQUFDSCxLQUFLLENBQUMwWCxDQUFDLENBQUMsQ0FBQztJQUMzQixJQUFJQSxDQUFDLEdBQUcsQ0FBQyxFQUFFO01BQUUrRSxLQUFLLENBQUNwbUIsS0FBSyxFQUFFa0osSUFBSSxDQUFDOUosQ0FBQyxFQUFFNFIsRUFBRSxDQUFDNVIsQ0FBQyxFQUFFOEosSUFBSSxDQUFDN0osQ0FBQyxDQUFDO01BQUVpbkIsS0FBSyxDQUFDdG1CLEtBQUssRUFBRWtKLElBQUksQ0FBQzdKLENBQUMsRUFBRTJSLEVBQUUsQ0FBQzNSLENBQUMsRUFBRTJSLEVBQUUsQ0FBQzVSLENBQUMsQ0FBQztJQUFDLENBQUMsTUFDOUU7TUFBRWtuQixLQUFLLENBQUN0bUIsS0FBSyxFQUFFa0osSUFBSSxDQUFDN0osQ0FBQyxFQUFFMlIsRUFBRSxDQUFDM1IsQ0FBQyxFQUFFNkosSUFBSSxDQUFDOUosQ0FBQyxDQUFDO01BQUVnbkIsS0FBSyxDQUFDcG1CLEtBQUssRUFBRWtKLElBQUksQ0FBQzlKLENBQUMsRUFBRTRSLEVBQUUsQ0FBQzVSLENBQUMsRUFBRTRSLEVBQUUsQ0FBQzNSLENBQUMsQ0FBQztJQUFDO0VBQzlFO0VBQ0EsTUFBTXFhLElBQUksR0FBR0EsQ0FBQSxLQUFNd00sU0FBUyxDQUFDbG1CLEtBQUssQ0FBQztFQUNuQyxNQUFNaWhCLEtBQUssR0FBR0EsQ0FBQ2xpQixJQUFrQixFQUFFeVksS0FBYSxFQUFFK1AsTUFBTSxHQUFHLEtBQUssS0FBSztJQUNuRS9QLEtBQUssR0FBRzJPLFlBQVksQ0FBQ25tQixLQUFLLEVBQUV3WCxLQUFLLENBQUM7SUFDbEMsSUFBSUosVUFBVSxHQUFHc0MsSUFBSSxDQUFDLENBQUM7SUFDdkIsSUFBSTZOLE1BQU0sRUFBRTtNQUNWLE1BQU03aEIsUUFBUSxHQUFHMFIsVUFBVSxDQUFDeFIsTUFBTSxDQUFDekcsS0FBSyxJQUFJLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUNnSSxJQUFJLENBQUMsQ0FBQyxDQUFDL0gsQ0FBQyxFQUFFQyxDQUFDLENBQUM7UUFBQSxJQUFBbW9CLFVBQUE7UUFBQSxPQUFLLEVBQUFBLFVBQUEsR0FBQXBuQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEdBQUdBLENBQUMsQ0FBQyxjQUFBbW9CLFVBQUEsdUJBQXhDQSxVQUFBLENBQTBDem9CLElBQUksTUFBSyxNQUFNO01BQUEsRUFBQyxDQUFDO01BQzNKLElBQUkyRyxRQUFRLENBQUM3QixNQUFNLEVBQUV1VCxVQUFVLEdBQUcxUixRQUFRO0lBQzVDO0lBQ0EsS0FBSyxJQUFJMmIsQ0FBQyxHQUFHLENBQUMsRUFBRUEsQ0FBQyxHQUFHN0osS0FBSyxJQUFJSixVQUFVLENBQUN2VCxNQUFNLEVBQUV3ZCxDQUFDLEVBQUUsRUFBRTtNQUNuRCxNQUFNbGlCLEtBQUssR0FBR2lSLEdBQUcsQ0FBQzZWLElBQUksQ0FBQzdPLFVBQVUsQ0FBQztNQUNsQ3JOLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFTixJQUFJLENBQUM7TUFDdENxWSxVQUFVLEdBQUdBLFVBQVUsQ0FBQ3hSLE1BQU0sQ0FBQ2pFLFNBQVMsSUFBSUEsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLRCxLQUFLLENBQUNDLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS0YsS0FBSyxDQUFDRSxDQUFDLENBQUM7SUFDakc7RUFDRixDQUFDO0VBQ0Q0aEIsS0FBSyxDQUFDLFNBQVMsRUFBRSxDQUFDLEVBQUUsSUFBSSxDQUFDO0VBQ3pCQSxLQUFLLENBQUMsU0FBUyxFQUFFLEVBQUUsQ0FBQztFQUNwQkEsS0FBSyxDQUFDLFFBQVEsRUFBRSxDQUFDLENBQUM7RUFDbEJBLEtBQUssQ0FBQyxTQUFTLEVBQUUsQ0FBQyxDQUFDO0FBQ3JCO0FBRUEsU0FBU3dGLGFBQWFBLENBQUN6bUIsS0FBWSxFQUFFb1EsR0FBUSxFQUFRO0VBQ25ELE1BQU1zSixJQUFJLEdBQUdBLENBQUEsS0FBTXdNLFNBQVMsQ0FBQ2xtQixLQUFLLENBQUM7RUFDbkMsTUFBTWloQixLQUFLLEdBQUdBLENBQUNsaUIsSUFBa0IsRUFBRXlZLEtBQWEsRUFBRWlRLFNBQVMsR0FBRyxLQUFLLEtBQUs7SUFDdEVqUSxLQUFLLEdBQUcyTyxZQUFZLENBQUNubUIsS0FBSyxFQUFFd1gsS0FBSyxDQUFDO0lBQ2xDLElBQUlKLFVBQVUsR0FBR3NDLElBQUksQ0FBQyxDQUFDO0lBQ3ZCLEtBQUssSUFBSTJILENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBRzdKLEtBQUssSUFBSUosVUFBVSxDQUFDdlQsTUFBTSxFQUFFd2QsQ0FBQyxFQUFFLEVBQUU7TUFDbkQsTUFBTWxpQixLQUFLLEdBQUdpUixHQUFHLENBQUM2VixJQUFJLENBQUM3TyxVQUFVLENBQUM7TUFDbENyTixPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRU4sSUFBSSxDQUFDO01BQ3RDLElBQUkwb0IsU0FBUyxFQUFFLEtBQUssTUFBTSxDQUFDcm9CLENBQUMsRUFBRUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUM7UUFBQSxJQUFBcW9CLFVBQUE7UUFBRSxJQUFJdFgsR0FBRyxDQUFDcUMsTUFBTSxDQUFDLEVBQUUsQ0FBQyxJQUFJLEVBQUFpVixVQUFBLEdBQUF0bkIsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxHQUFHQSxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQSxDQUFDLENBQUMsY0FBQXFvQixVQUFBLHVCQUF4Q0EsVUFBQSxDQUEwQzNvQixJQUFJLE1BQUssT0FBTyxFQUFFZ0wsT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsR0FBR0EsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsR0FBR0EsQ0FBQyxFQUFFTixJQUFJLENBQUM7TUFBQTtNQUN4TXFZLFVBQVUsR0FBR3NDLElBQUksQ0FBQyxDQUFDO0lBQ3JCO0VBQ0YsQ0FBQztFQUNEdUgsS0FBSyxDQUFDLE9BQU8sRUFBRSxDQUFDLEVBQUUsSUFBSSxDQUFDO0VBQ3ZCLElBQUlqaEIsS0FBSyxDQUFDeUgsUUFBUSxDQUFDbEksUUFBUSxDQUFDLGlCQUFpQixDQUFDLElBQUlTLEtBQUssQ0FBQ3lILFFBQVEsQ0FBQ2xJLFFBQVEsQ0FBQyxTQUFTLENBQUMsRUFBRW9vQixnQkFBZ0IsQ0FBQzNuQixLQUFLLEVBQUVvUSxHQUFHLEVBQUUsS0FBSyxDQUFDO0VBQ3pINlEsS0FBSyxDQUFDLFNBQVMsRUFBRSxFQUFFLEVBQUUsSUFBSSxDQUFDO0VBQzFCQSxLQUFLLENBQUMsU0FBUyxFQUFFLENBQUMsRUFBRSxJQUFJLENBQUM7RUFDekJBLEtBQUssQ0FBQyxLQUFLLEVBQUUsRUFBRSxDQUFDO0FBQ2xCO0FBRUEsU0FBU3lGLGVBQWVBLENBQUMxbUIsS0FBWSxFQUFFb1EsR0FBUSxFQUFRO0VBQ3JELE1BQU1zSixJQUFJLEdBQUdBLENBQUEsS0FBTXdNLFNBQVMsQ0FBQ2xtQixLQUFLLENBQUM7RUFDbkMsTUFBTWloQixLQUFLLEdBQUdBLENBQUNsaUIsSUFBa0IsRUFBRXlZLEtBQWEsRUFBRWlRLFNBQVMsR0FBRyxLQUFLLEtBQUs7SUFDdEVqUSxLQUFLLEdBQUcyTyxZQUFZLENBQUNubUIsS0FBSyxFQUFFd1gsS0FBSyxDQUFDO0lBQ2xDLElBQUlKLFVBQVUsR0FBR3NDLElBQUksQ0FBQyxDQUFDO0lBQ3ZCLEtBQUssSUFBSTJILENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBRzdKLEtBQUssSUFBSUosVUFBVSxDQUFDdlQsTUFBTSxFQUFFd2QsQ0FBQyxFQUFFLEVBQUU7TUFDbkQsTUFBTWxpQixLQUFLLEdBQUdpUixHQUFHLENBQUM2VixJQUFJLENBQUM3TyxVQUFVLENBQUM7TUFDbENyTixPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRU4sSUFBSSxDQUFDO01BQ3RDLElBQUkwb0IsU0FBUyxFQUFFLEtBQUssTUFBTSxDQUFDcm9CLENBQUMsRUFBRUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUM7UUFBQSxJQUFBdW9CLFVBQUE7UUFBRSxJQUFJeFgsR0FBRyxDQUFDcUMsTUFBTSxDQUFDLEVBQUUsQ0FBQyxJQUFJLEVBQUFtVixVQUFBLEdBQUF4bkIsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxHQUFHQSxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQSxDQUFDLENBQUMsY0FBQXVvQixVQUFBLHVCQUF4Q0EsVUFBQSxDQUEwQzdvQixJQUFJLE1BQUssT0FBTyxFQUFFZ0wsT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsR0FBR0EsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsR0FBR0EsQ0FBQyxFQUFFTixJQUFJLENBQUM7TUFBQTtNQUN4TXFZLFVBQVUsR0FBR3NDLElBQUksQ0FBQyxDQUFDO0lBQ3JCO0VBQ0YsQ0FBQztFQUNEaU8sZ0JBQWdCLENBQUMzbkIsS0FBSyxFQUFFb1EsR0FBRyxFQUFFLEtBQUssQ0FBQztFQUNuQzZRLEtBQUssQ0FBQyxPQUFPLEVBQUUsRUFBRSxFQUFFLElBQUksQ0FBQztFQUN4QkEsS0FBSyxDQUFDLFdBQVcsRUFBRSxDQUFDLEVBQUUsSUFBSSxDQUFDO0VBQzNCQSxLQUFLLENBQUMsU0FBUyxFQUFFLENBQUMsQ0FBQztFQUNuQkEsS0FBSyxDQUFDLFVBQVUsRUFBRSxFQUFFLEVBQUUsSUFBSSxDQUFDO0FBQzdCO0FBRUEsU0FBUzBGLGFBQWFBLENBQUMzbUIsS0FBWSxFQUFFb1EsR0FBUSxFQUFFekcsS0FBYSxFQUFRO0VBQ2xFLE1BQU0rUCxJQUFJLEdBQUdBLENBQUEsS0FBTXdNLFNBQVMsQ0FBQ2xtQixLQUFLLENBQUM7RUFDbkMsTUFBTWloQixLQUFLLEdBQUdBLENBQUNsaUIsSUFBa0IsRUFBRXlZLEtBQWEsS0FBSztJQUNuREEsS0FBSyxHQUFHMk8sWUFBWSxDQUFDbm1CLEtBQUssRUFBRXdYLEtBQUssQ0FBQztJQUNsQyxJQUFJSixVQUFVLEdBQUdzQyxJQUFJLENBQUMsQ0FBQztJQUN2QixLQUFLLElBQUkySCxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUc3SixLQUFLLElBQUlKLFVBQVUsQ0FBQ3ZULE1BQU0sRUFBRXdkLENBQUMsRUFBRSxFQUFFO01BQ25ELE1BQU1saUIsS0FBSyxHQUFHaVIsR0FBRyxDQUFDNlYsSUFBSSxDQUFDN08sVUFBVSxDQUFDO01BQ2xDck4sT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUVOLElBQUksQ0FBQztNQUN0Q3FZLFVBQVUsR0FBR3NDLElBQUksQ0FBQyxDQUFDO0lBQ3JCO0VBQ0YsQ0FBQztFQUNEdUgsS0FBSyxDQUFDLE1BQU0sRUFBRSxFQUFFLENBQUM7RUFDakJBLEtBQUssQ0FBQyxTQUFTLEVBQUUsRUFBRSxDQUFDO0VBQ3BCQSxLQUFLLENBQUMsU0FBUyxFQUFFLENBQUMsQ0FBQztFQUNuQixNQUFNNEcsVUFBVSxHQUFHbGUsS0FBSyxDQUFDOUYsTUFBTSxHQUFHLENBQUMsR0FBRzhGLEtBQUssQ0FBQ0EsS0FBSyxDQUFDOUYsTUFBTSxHQUFHLENBQUMsQ0FBQyxHQUFHdkQsU0FBUztFQUN6RSxJQUFJLENBQUN1bkIsVUFBVSxFQUFFO0VBQ2pCLE1BQU1DLEtBQUssR0FBR2hlLE1BQU0sQ0FBQytkLFVBQVUsQ0FBQztFQUNoQyxLQUFLLElBQUl4b0IsQ0FBQyxHQUFHeW9CLEtBQUssQ0FBQ3pvQixDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLElBQUl5b0IsS0FBSyxDQUFDem9CLENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsRUFBRSxFQUFFLEtBQUssSUFBSUQsQ0FBQyxHQUFHMG9CLEtBQUssQ0FBQzFvQixDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLElBQUkwb0IsS0FBSyxDQUFDMW9CLENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsRUFBRTtJQUFBLElBQUEyb0IsVUFBQTtJQUFFLElBQUksRUFBQUEsVUFBQSxHQUFBM25CLE9BQU8sQ0FBQ0osS0FBSyxFQUFFWixDQUFDLEVBQUVDLENBQUMsQ0FBQyxjQUFBMG9CLFVBQUEsdUJBQXBCQSxVQUFBLENBQXNCaHBCLElBQUksTUFBSyxNQUFNLEVBQUVnTCxPQUFPLENBQUMvSixLQUFLLEVBQUVaLENBQUMsRUFBRUMsQ0FBQyxFQUFFLE9BQU8sQ0FBQztFQUFBO0VBQzFLMEssT0FBTyxDQUFDL0osS0FBSyxFQUFFOG5CLEtBQUssQ0FBQzFvQixDQUFDLEVBQUUwb0IsS0FBSyxDQUFDem9CLENBQUMsRUFBRSxPQUFPLENBQUM7QUFDM0M7QUFFQSxTQUFTdW5CLGVBQWVBLENBQUM1bUIsS0FBWSxFQUFFb1EsR0FBUSxFQUFRO0VBQ3JELE1BQU1zSixJQUFJLEdBQUdBLENBQUEsS0FBTXdNLFNBQVMsQ0FBQ2xtQixLQUFLLENBQUM7RUFDbkMsTUFBTWloQixLQUFLLEdBQUdBLENBQUNsaUIsSUFBa0IsRUFBRXlZLEtBQWEsRUFBRWlRLFNBQVMsR0FBRyxLQUFLLEtBQUs7SUFDdEVqUSxLQUFLLEdBQUcyTyxZQUFZLENBQUNubUIsS0FBSyxFQUFFd1gsS0FBSyxDQUFDO0lBQ2xDLElBQUlKLFVBQVUsR0FBR3NDLElBQUksQ0FBQyxDQUFDO0lBQ3ZCLEtBQUssSUFBSTJILENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBRzdKLEtBQUssSUFBSUosVUFBVSxDQUFDdlQsTUFBTSxFQUFFd2QsQ0FBQyxFQUFFLEVBQUU7TUFDbkQsTUFBTWxpQixLQUFLLEdBQUdpUixHQUFHLENBQUM2VixJQUFJLENBQUM3TyxVQUFVLENBQUM7TUFDbENyTixPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsRUFBRU4sSUFBSSxDQUFDO01BQ3RDLElBQUkwb0IsU0FBUyxFQUFFLEtBQUssTUFBTSxDQUFDcm9CLENBQUMsRUFBRUMsQ0FBQyxDQUFDLElBQUk0TixlQUFlO1FBQUEsSUFBQSthLFVBQUE7UUFBRSxJQUFJNVgsR0FBRyxDQUFDcUMsTUFBTSxDQUFDLEVBQUUsQ0FBQyxJQUFJLEVBQUF1VixVQUFBLEdBQUE1bkIsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxHQUFHQSxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQSxDQUFDLENBQUMsY0FBQTJvQixVQUFBLHVCQUF4Q0EsVUFBQSxDQUEwQ2pwQixJQUFJLE1BQUssT0FBTyxFQUFFZ0wsT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsR0FBR0EsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsR0FBR0EsQ0FBQyxFQUFFTixJQUFJLENBQUM7TUFBQTtNQUNyTHFZLFVBQVUsR0FBR3NDLElBQUksQ0FBQyxDQUFDO0lBQ3JCO0VBQ0YsQ0FBQztFQUNEdUgsS0FBSyxDQUFDLE9BQU8sRUFBRSxFQUFFLEVBQUUsSUFBSSxDQUFDO0VBQ3hCQSxLQUFLLENBQUMsTUFBTSxFQUFFLENBQUMsQ0FBQztFQUNoQkEsS0FBSyxDQUFDLFdBQVcsRUFBRSxDQUFDLENBQUM7RUFDckJBLEtBQUssQ0FBQyxVQUFVLEVBQUUsQ0FBQyxDQUFDO0FBQ3RCO0FBRUEsU0FBUzRGLG9CQUFvQkEsQ0FBQzdtQixLQUFZLEVBQUVvUSxHQUFRLEVBQVE7RUFDMUQsTUFBTXNKLElBQUksR0FBR0EsQ0FBQSxLQUFNd00sU0FBUyxDQUFDbG1CLEtBQUssQ0FBQztFQUNuQyxNQUFNaWhCLEtBQUssR0FBR0EsQ0FBQ2xpQixJQUFrQixFQUFFeVksS0FBYSxFQUFFaVEsU0FBUyxHQUFHLEtBQUssS0FBSztJQUN0RWpRLEtBQUssR0FBRzJPLFlBQVksQ0FBQ25tQixLQUFLLEVBQUV3WCxLQUFLLENBQUM7SUFDbEMsSUFBSUosVUFBVSxHQUFHc0MsSUFBSSxDQUFDLENBQUM7SUFDdkIsS0FBSyxJQUFJMkgsQ0FBQyxHQUFHLENBQUMsRUFBRUEsQ0FBQyxHQUFHN0osS0FBSyxJQUFJSixVQUFVLENBQUN2VCxNQUFNLEVBQUV3ZCxDQUFDLEVBQUUsRUFBRTtNQUNuRCxNQUFNbGlCLEtBQUssR0FBR2lSLEdBQUcsQ0FBQzZWLElBQUksQ0FBQzdPLFVBQVUsQ0FBQztNQUNsQ3JOLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFTixJQUFJLENBQUM7TUFDdEMsSUFBSTBvQixTQUFTLEVBQUUsS0FBSyxNQUFNLENBQUNyb0IsQ0FBQyxFQUFFQyxDQUFDLENBQUMsSUFBSTROLGVBQWU7UUFBQSxJQUFBZ2IsVUFBQTtRQUFFLElBQUk3WCxHQUFHLENBQUNxQyxNQUFNLENBQUMsRUFBRSxDQUFDLElBQUksRUFBQXdWLFVBQUEsR0FBQTduQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEdBQUdBLENBQUMsQ0FBQyxjQUFBNG9CLFVBQUEsdUJBQXhDQSxVQUFBLENBQTBDbHBCLElBQUksTUFBSyxPQUFPLEVBQUVnTCxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxHQUFHQSxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQSxDQUFDLEVBQUVOLElBQUksQ0FBQztNQUFBO01BQ3JMcVksVUFBVSxHQUFHc0MsSUFBSSxDQUFDLENBQUM7SUFDckI7RUFDRixDQUFDO0VBQ0QsSUFBSSxDQUFDLENBQUMsdUJBQXVCLEVBQUUsbUJBQW1CLEVBQUUsb0JBQW9CLENBQUMsQ0FBQ3ZTLElBQUksQ0FBQytnQixNQUFNLElBQUlsb0IsS0FBSyxDQUFDeUgsUUFBUSxDQUFDbEksUUFBUSxDQUFDMm9CLE1BQU0sQ0FBQyxDQUFDLEVBQUVQLGdCQUFnQixDQUFDM25CLEtBQUssRUFBRW9RLEdBQUcsRUFBRXBRLEtBQUssQ0FBQ3lILFFBQVEsQ0FBQ2xJLFFBQVEsQ0FBQyxXQUFXLENBQUMsQ0FBQztFQUM3TDBoQixLQUFLLENBQUMsUUFBUSxFQUFFLENBQUMsQ0FBQztFQUNsQkEsS0FBSyxDQUFDLFdBQVcsRUFBRSxDQUFDLEVBQUUsSUFBSSxDQUFDO0VBQzNCQSxLQUFLLENBQUMsT0FBTyxFQUFFLENBQUMsRUFBRSxJQUFJLENBQUM7QUFDekI7QUFFQSxNQUFNMEcsZ0JBQWdCLEdBQUdBLENBQUMzbkIsS0FBWSxFQUFFb1EsR0FBUSxFQUFFK1gsU0FBa0IsS0FBVztFQUM3RSxNQUFNeFksUUFBUSxHQUFHUyxHQUFHLENBQUNxQyxNQUFNLENBQUMsRUFBRSxDQUFDO0VBQy9CLE1BQU0xUCxLQUFLLEdBQUc0TSxRQUFRLEdBQUdTLEdBQUcsQ0FBQ0csR0FBRyxDQUFDLENBQUMsRUFBRXZRLEtBQUssQ0FBQ3lJLEtBQUssR0FBRyxDQUFDLENBQUMsR0FBRzJILEdBQUcsQ0FBQ0csR0FBRyxDQUFDLENBQUMsRUFBRXZRLEtBQUssQ0FBQzBJLE1BQU0sR0FBRyxDQUFDLENBQUM7RUFDbkYsTUFBTWtPLFNBQXFDLEdBQUdqSCxRQUFRLEdBQUcsR0FBRyxHQUFHLEdBQUc7RUFDbEUsTUFBTTlMLE1BQU0sR0FBRzhMLFFBQVEsR0FBRzNQLEtBQUssQ0FBQzBJLE1BQU0sR0FBRyxDQUFDLEdBQUcxSSxLQUFLLENBQUN5SSxLQUFLLEdBQUcsQ0FBQztFQUM1RCxJQUFJMmYsSUFBSSxHQUFHcmxCLEtBQUs7RUFDaEIsSUFBSXNsQixJQUF1QjtFQUMzQixLQUFLLElBQUkxVCxJQUFJLEdBQUcsQ0FBQyxFQUFFQSxJQUFJLEdBQUc5USxNQUFNLEVBQUU4USxJQUFJLEVBQUUsRUFBRTtJQUFBLElBQUEyVCxVQUFBO0lBQ3hDLElBQUkzVCxJQUFJLEdBQUcsQ0FBQyxLQUFLLENBQUMsSUFBSXZFLEdBQUcsQ0FBQ3FDLE1BQU0sQ0FBQyxFQUFFLENBQUMsRUFBRTJWLElBQUksSUFBSWhZLEdBQUcsQ0FBQ0csR0FBRyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQztJQUM1RCxNQUFNblIsQ0FBQyxHQUFHdVEsUUFBUSxHQUFHeVksSUFBSSxHQUFHelQsSUFBSTtJQUNoQyxNQUFNdFYsQ0FBQyxHQUFHc1EsUUFBUSxHQUFHZ0YsSUFBSSxHQUFHeVQsSUFBSTtJQUNoQyxJQUFJLENBQUNqb0IsUUFBUSxDQUFDSCxLQUFLLEVBQUVaLENBQUMsRUFBRUMsQ0FBQyxDQUFDLElBQUlELENBQUMsR0FBRyxDQUFDLElBQUlDLENBQUMsR0FBRyxDQUFDLElBQUlELENBQUMsSUFBSVksS0FBSyxDQUFDeUksS0FBSyxHQUFHLENBQUMsSUFBSXBKLENBQUMsSUFBSVcsS0FBSyxDQUFDMEksTUFBTSxHQUFHLENBQUMsRUFBRTtJQUMvRixNQUFNaEcsT0FBTyxHQUFHdEMsT0FBTyxDQUFDSixLQUFLLEVBQUVaLENBQUMsRUFBRUMsQ0FBQyxDQUFDO0lBQ3BDLElBQUksQ0FBQ3FELE9BQU8sSUFBSUEsT0FBTyxDQUFDM0QsSUFBSSxLQUFLLE1BQU0sSUFBS0ssQ0FBQyxLQUFLWSxLQUFLLENBQUMrQyxLQUFLLENBQUMzRCxDQUFDLElBQUlDLENBQUMsS0FBS1csS0FBSyxDQUFDK0MsS0FBSyxDQUFDMUQsQ0FBRSxFQUFFO0lBQ3pGLElBQUlncEIsSUFBSSxFQUFFO01BQ1IsTUFBTTFrQixRQUFRLEdBQUd2RCxPQUFPLENBQUNKLEtBQUssRUFBRXFvQixJQUFJLENBQUNqcEIsQ0FBQyxFQUFFaXBCLElBQUksQ0FBQ2hwQixDQUFDLENBQUM7TUFDL0MsSUFBSXNFLFFBQVEsYUFBUkEsUUFBUSxlQUFSQSxRQUFRLENBQUUwVSxJQUFJLEVBQUUxVSxRQUFRLENBQUMwVSxJQUFJLENBQUN6QixTQUFTLEdBQUcwQixhQUFhLENBQUNsWixDQUFDLEdBQUdpcEIsSUFBSSxDQUFDanBCLENBQUMsRUFBRUMsQ0FBQyxHQUFHZ3BCLElBQUksQ0FBQ2hwQixDQUFDLENBQUM7SUFDckY7SUFDQXFELE9BQU8sQ0FBQzNELElBQUksR0FBRyxTQUFTO0lBQ3hCMkQsT0FBTyxDQUFDMlYsSUFBSSxHQUFHO01BQUV6QixTQUFTO01BQUUsSUFBSXVSLFNBQVMsSUFBSXhULElBQUksR0FBRzlRLE1BQU0sR0FBRyxDQUFDLEdBQUc7UUFBRXVYLE1BQU0sRUFBRTtNQUFvQixDQUFDLEdBQUcsQ0FBQyxDQUFDO0lBQUUsQ0FBQztJQUN4R2lOLElBQUksR0FBRztNQUFFanBCLENBQUM7TUFBRUM7SUFBRSxDQUFDO0lBQ2YsTUFBTWljLElBQUksR0FBRzNMLFFBQVEsR0FBRztNQUFFdlEsQ0FBQyxFQUFFQSxDQUFDLEdBQUcsQ0FBQztNQUFFQztJQUFFLENBQUMsR0FBRztNQUFFRCxDQUFDO01BQUVDLENBQUMsRUFBRUEsQ0FBQyxHQUFHO0lBQUUsQ0FBQztJQUN6RCxJQUFJLEVBQUFpcEIsVUFBQSxHQUFBbG9CLE9BQU8sQ0FBQ0osS0FBSyxFQUFFc2IsSUFBSSxDQUFDbGMsQ0FBQyxFQUFFa2MsSUFBSSxDQUFDamMsQ0FBQyxDQUFDLGNBQUFpcEIsVUFBQSx1QkFBOUJBLFVBQUEsQ0FBZ0N2cEIsSUFBSSxNQUFLLE9BQU8sSUFBSXFSLEdBQUcsQ0FBQ3FDLE1BQU0sQ0FBQyxFQUFFLENBQUMsRUFBRTFJLE9BQU8sQ0FBQy9KLEtBQUssRUFBRXNiLElBQUksQ0FBQ2xjLENBQUMsRUFBRWtjLElBQUksQ0FBQ2pjLENBQUMsRUFBRSxPQUFPLENBQUM7RUFDakg7RUFDQSxJQUFJOG9CLFNBQVMsSUFBSUUsSUFBSSxFQUFFO0lBQUEsSUFBQUUscUJBQUEsRUFBQUMsVUFBQTtJQUNyQixNQUFNQyxLQUFLLEdBQUk7TUFBRUMsQ0FBQyxFQUFFO1FBQUV0cEIsQ0FBQyxFQUFFLENBQUM7UUFBRUMsQ0FBQyxFQUFFLENBQUM7TUFBRSxDQUFDO01BQUVzcEIsRUFBRSxFQUFFO1FBQUV2cEIsQ0FBQyxFQUFFLENBQUM7UUFBRUMsQ0FBQyxFQUFFLENBQUM7TUFBRSxDQUFDO01BQUV1cEIsQ0FBQyxFQUFFO1FBQUV4cEIsQ0FBQyxFQUFFLENBQUM7UUFBRUMsQ0FBQyxFQUFFO01BQUUsQ0FBQztNQUFFd3BCLEVBQUUsRUFBRTtRQUFFenBCLENBQUMsRUFBRSxDQUFDO1FBQUVDLENBQUMsRUFBRTtNQUFFLENBQUM7TUFBRXlwQixDQUFDLEVBQUU7UUFBRTFwQixDQUFDLEVBQUUsQ0FBQztRQUFFQyxDQUFDLEVBQUU7TUFBRSxDQUFDO01BQUUwcEIsRUFBRSxFQUFFO1FBQUUzcEIsQ0FBQyxFQUFFLENBQUMsQ0FBQztRQUFFQyxDQUFDLEVBQUU7TUFBRSxDQUFDO01BQUUwTixDQUFDLEVBQUU7UUFBRTNOLENBQUMsRUFBRSxDQUFDLENBQUM7UUFBRUMsQ0FBQyxFQUFFO01BQUUsQ0FBQztNQUFFMnBCLEVBQUUsRUFBRTtRQUFFNXBCLENBQUMsRUFBRSxDQUFDLENBQUM7UUFBRUMsQ0FBQyxFQUFFLENBQUM7TUFBRTtJQUFFLENBQUMsRUFBQWtwQixxQkFBQSxJQUFBQyxVQUFBLEdBQVdwb0IsT0FBTyxDQUFDSixLQUFLLEVBQUVxb0IsSUFBSSxDQUFDanBCLENBQUMsRUFBRWlwQixJQUFJLENBQUNocEIsQ0FBQyxDQUFDLGNBQUFtcEIsVUFBQSxnQkFBQUEsVUFBQSxHQUE5QkEsVUFBQSxDQUFnQ25RLElBQUksY0FBQW1RLFVBQUEsdUJBQXBDQSxVQUFBLENBQXNDNVIsU0FBUyxjQUFBMlIscUJBQUEsY0FBQUEscUJBQUEsR0FBSTNSLFNBQVMsQ0FBQztJQUMzUCxNQUFNeUUsTUFBTSxHQUFHamIsT0FBTyxDQUFDSixLQUFLLEVBQUVxb0IsSUFBSSxDQUFDanBCLENBQUMsR0FBR3FwQixLQUFLLENBQUNycEIsQ0FBQyxFQUFFaXBCLElBQUksQ0FBQ2hwQixDQUFDLEdBQUdvcEIsS0FBSyxDQUFDcHBCLENBQUMsQ0FBQztJQUNqRSxJQUFJZ2MsTUFBTSxJQUFJQSxNQUFNLENBQUN0YyxJQUFJLEtBQUssTUFBTSxFQUFFc2MsTUFBTSxDQUFDdGMsSUFBSSxHQUFHLFdBQVc7RUFDakU7QUFDRixDQUFDO0FBRUQsTUFBTXVaLGFBQWEsR0FBR0EsQ0FBQ2xaLENBQVMsRUFBRUMsQ0FBUyxLQUFpQ0QsQ0FBQyxHQUFHLENBQUMsR0FBR0MsQ0FBQyxHQUFHLENBQUMsR0FBRyxJQUFJLEdBQUdBLENBQUMsR0FBRyxDQUFDLEdBQUcsSUFBSSxHQUFHLEdBQUcsR0FBR0QsQ0FBQyxHQUFHLENBQUMsR0FBR0MsQ0FBQyxHQUFHLENBQUMsR0FBRyxJQUFJLEdBQUdBLENBQUMsR0FBRyxDQUFDLEdBQUcsSUFBSSxHQUFHLEdBQUcsR0FBR0EsQ0FBQyxHQUFHLENBQUMsR0FBRyxHQUFHLEdBQUcsR0FBRztBQUVyTCxTQUFTeW5CLGNBQWNBLENBQUM5bUIsS0FBWSxFQUFFb1EsR0FBUSxFQUFRO0VBQ3BELElBQUksQ0FBQyxpQkFBaUIsRUFBRSxvQkFBb0IsRUFBRSxjQUFjLENBQUMsQ0FBQ2pKLElBQUksQ0FBQ2tILE9BQU8sSUFBSXJPLEtBQUssQ0FBQ3lILFFBQVEsS0FBSzRHLE9BQU8sSUFBSXJPLEtBQUssQ0FBQ3lILFFBQVEsS0FBSyxHQUFHNEcsT0FBTyxRQUFRLENBQUMsRUFBRTtFQUNwSixNQUFNcUwsSUFBSSxHQUFHQSxDQUFBLEtBQU13TSxTQUFTLENBQUNsbUIsS0FBSyxDQUFDO0VBQ25DLE1BQU1paEIsS0FBSyxHQUFHQSxDQUFDbGlCLElBQWtCLEVBQUV5WSxLQUFhLEtBQUs7SUFDbkRBLEtBQUssR0FBRzJPLFlBQVksQ0FBQ25tQixLQUFLLEVBQUV3WCxLQUFLLENBQUM7SUFDbEMsSUFBSUosVUFBVSxHQUFHc0MsSUFBSSxDQUFDLENBQUM7SUFDdkIsS0FBSyxJQUFJMkgsQ0FBQyxHQUFHLENBQUMsRUFBRUEsQ0FBQyxHQUFHN0osS0FBSyxJQUFJSixVQUFVLENBQUN2VCxNQUFNLEVBQUV3ZCxDQUFDLEVBQUUsRUFBRTtNQUNuRCxNQUFNbGlCLEtBQUssR0FBR2lSLEdBQUcsQ0FBQzZWLElBQUksQ0FBQzdPLFVBQVUsQ0FBQztNQUNsQ3JOLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFTixJQUFJLENBQUM7TUFDdENxWSxVQUFVLEdBQUdzQyxJQUFJLENBQUMsQ0FBQztJQUNyQjtFQUNGLENBQUM7RUFDRHVILEtBQUssQ0FBQyxPQUFPLEVBQUUsRUFBRSxDQUFDO0VBQ2xCQSxLQUFLLENBQUMsV0FBVyxFQUFFLEVBQUUsQ0FBQztFQUN0QkEsS0FBSyxDQUFDLE1BQU0sRUFBRSxDQUFDLENBQUM7RUFDaEIsTUFBTXJILEtBQUssR0FBR0YsSUFBSSxDQUFDLENBQUM7RUFDcEIsSUFBSSxDQUFDRSxLQUFLLENBQUMvVixNQUFNLEVBQUU7RUFDbkIsTUFBTXFGLElBQUksR0FBR2tILEdBQUcsQ0FBQzZWLElBQUksQ0FBQ3JNLEtBQUssQ0FBQztFQUM1QixNQUFNcVAsR0FBRyxHQUFHclAsS0FBSyxDQUFDaFUsTUFBTSxDQUFDekcsS0FBSyxJQUFJcVQsUUFBUSxDQUFDclQsS0FBSyxFQUFFK0osSUFBSSxDQUFDLEdBQUcsQ0FBQyxDQUFDO0VBQzVELE1BQU04SCxFQUFFLEdBQUdaLEdBQUcsQ0FBQzZWLElBQUksQ0FBQ2dELEdBQUcsQ0FBQ3BsQixNQUFNLEdBQUdvbEIsR0FBRyxHQUFHclAsS0FBSyxDQUFDO0VBQzdDeFosT0FBTyxDQUFDSixLQUFLLEVBQUVrSixJQUFJLENBQUM5SixDQUFDLEVBQUU4SixJQUFJLENBQUM3SixDQUFDLENBQUMsQ0FBRXlhLFNBQVMsR0FBRyxDQUFDO0VBQzdDMVosT0FBTyxDQUFDSixLQUFLLEVBQUVnUixFQUFFLENBQUM1UixDQUFDLEVBQUU0UixFQUFFLENBQUMzUixDQUFDLENBQUMsQ0FBRXlhLFNBQVMsR0FBRyxDQUFDO0VBQ3pDOVosS0FBSyxDQUFDK1osVUFBVSxHQUFHLENBQUM7SUFBRWhZLEVBQUUsRUFBRSxTQUFTL0IsS0FBSyxDQUFDRSxLQUFLLElBQUlnSixJQUFJLENBQUM5SixDQUFDLElBQUk4SixJQUFJLENBQUM3SixDQUFDLElBQUkyUixFQUFFLENBQUM1UixDQUFDLElBQUk0UixFQUFFLENBQUMzUixDQUFDLEVBQUU7SUFBRXVhLEtBQUssRUFBRTFRLElBQUk7SUFBRTJRLEtBQUssRUFBRTdJLEVBQUU7SUFBRWdKLFFBQVEsRUFBRTtFQUFNLENBQUMsQ0FBQztBQUNsSTtBQUVBLFNBQVMrTSxjQUFjQSxDQUFDL21CLEtBQVksRUFBRW9RLEdBQVEsRUFBUTtFQUNwRCxJQUFJLENBQUMscUJBQXFCLEVBQUUsa0JBQWtCLEVBQUUsMEJBQTBCLEVBQUUsZ0JBQWdCLEVBQUUsb0JBQW9CLENBQUMsQ0FBQ2pKLElBQUksQ0FBQ2tILE9BQU8sSUFBSXJPLEtBQUssQ0FBQ3lILFFBQVEsS0FBSzRHLE9BQU8sSUFBSXJPLEtBQUssQ0FBQ3lILFFBQVEsS0FBSyxHQUFHNEcsT0FBTyxRQUFRLENBQUMsRUFBRTtFQUMxTSxNQUFNcUwsSUFBSSxHQUFHQSxDQUFBLEtBQU13TSxTQUFTLENBQUNsbUIsS0FBSyxDQUFDO0VBQ25DLE1BQU1paEIsS0FBSyxHQUFHQSxDQUFDbGlCLElBQWtCLEVBQUV5WSxLQUFhLEVBQUVpUSxTQUFTLEdBQUcsS0FBSyxLQUFLO0lBQ3RFalEsS0FBSyxHQUFHMk8sWUFBWSxDQUFDbm1CLEtBQUssRUFBRXdYLEtBQUssQ0FBQztJQUNsQyxJQUFJSixVQUFVLEdBQUdzQyxJQUFJLENBQUMsQ0FBQztJQUN2QixLQUFLLElBQUkySCxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUc3SixLQUFLLElBQUlKLFVBQVUsQ0FBQ3ZULE1BQU0sRUFBRXdkLENBQUMsRUFBRSxFQUFFO01BQ25ELE1BQU1saUIsS0FBSyxHQUFHaVIsR0FBRyxDQUFDNlYsSUFBSSxDQUFDN08sVUFBVSxDQUFDO01BQ2xDck4sT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUVOLElBQUksQ0FBQztNQUN0QyxJQUFJMG9CLFNBQVMsRUFBRSxLQUFLLE1BQU0sQ0FBQ3JvQixDQUFDLEVBQUVDLENBQUMsQ0FBQyxJQUFJNE4sZUFBZTtRQUFBLElBQUFpYyxVQUFBO1FBQUUsSUFBSTlZLEdBQUcsQ0FBQ3FDLE1BQU0sQ0FBQyxFQUFFLENBQUMsSUFBSSxFQUFBeVcsVUFBQSxHQUFBOW9CLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsR0FBR0EsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsR0FBR0EsQ0FBQyxDQUFDLGNBQUE2cEIsVUFBQSx1QkFBeENBLFVBQUEsQ0FBMENucUIsSUFBSSxNQUFLLE9BQU8sRUFBRWdMLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEdBQUdBLENBQUMsRUFBRU4sSUFBSSxDQUFDO01BQUE7TUFDckxxWSxVQUFVLEdBQUdzQyxJQUFJLENBQUMsQ0FBQztJQUNyQjtFQUNGLENBQUM7RUFDRHVILEtBQUssQ0FBQyxXQUFXLEVBQUUsRUFBRSxFQUFFLElBQUksQ0FBQztFQUM1QkEsS0FBSyxDQUFDLE9BQU8sRUFBRSxDQUFDLENBQUM7RUFDakJBLEtBQUssQ0FBQyxTQUFTLEVBQUUsQ0FBQyxDQUFDO0VBQ25CQSxLQUFLLENBQUMsWUFBWSxFQUFFLEVBQUUsRUFBRSxJQUFJLENBQUM7QUFDL0I7QUFFQSxTQUFTK0YsaUJBQWlCQSxDQUFDaG5CLEtBQVksRUFBRW9RLEdBQVEsRUFBUTtFQUN2RCxJQUFJLENBQUMsb0JBQW9CLEVBQUUsWUFBWSxFQUFFLGtCQUFrQixFQUFFLG1CQUFtQixFQUFFLG1CQUFtQixDQUFDLENBQUNqSixJQUFJLENBQUNrSCxPQUFPLElBQUlyTyxLQUFLLENBQUN5SCxRQUFRLEtBQUs0RyxPQUFPLElBQUlyTyxLQUFLLENBQUN5SCxRQUFRLEtBQUssR0FBRzRHLE9BQU8sUUFBUSxDQUFDLEVBQUU7RUFDN0wsTUFBTXFMLElBQUksR0FBR0EsQ0FBQSxLQUFNd00sU0FBUyxDQUFDbG1CLEtBQUssQ0FBQztFQUNuQyxNQUFNaWhCLEtBQUssR0FBR0EsQ0FBQ2xpQixJQUFrQixFQUFFeVksS0FBYSxFQUFFaVEsU0FBUyxHQUFHLEtBQUssS0FBSztJQUN0RWpRLEtBQUssR0FBRzJPLFlBQVksQ0FBQ25tQixLQUFLLEVBQUV3WCxLQUFLLENBQUM7SUFDbEMsSUFBSUosVUFBVSxHQUFHc0MsSUFBSSxDQUFDLENBQUM7SUFDdkIsS0FBSyxJQUFJMkgsQ0FBQyxHQUFHLENBQUMsRUFBRUEsQ0FBQyxHQUFHN0osS0FBSyxJQUFJSixVQUFVLENBQUN2VCxNQUFNLEVBQUV3ZCxDQUFDLEVBQUUsRUFBRTtNQUNuRCxNQUFNbGlCLEtBQUssR0FBR2lSLEdBQUcsQ0FBQzZWLElBQUksQ0FBQzdPLFVBQVUsQ0FBQztNQUNsQ3JOLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFTixJQUFJLENBQUM7TUFDdEMsSUFBSTBvQixTQUFTLEVBQUUsS0FBSyxNQUFNLENBQUNyb0IsQ0FBQyxFQUFFQyxDQUFDLENBQUMsSUFBSTROLGVBQWU7UUFBQSxJQUFBa2MsVUFBQTtRQUFFLElBQUkvWSxHQUFHLENBQUNxQyxNQUFNLENBQUMsRUFBRSxDQUFDLElBQUksRUFBQTBXLFVBQUEsR0FBQS9vQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEdBQUdBLENBQUMsQ0FBQyxjQUFBOHBCLFVBQUEsdUJBQXhDQSxVQUFBLENBQTBDcHFCLElBQUksTUFBSyxPQUFPLEVBQUVnTCxPQUFPLENBQUMvSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxHQUFHQSxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxHQUFHQSxDQUFDLEVBQUVOLElBQUksQ0FBQztNQUFBO01BQ3JMcVksVUFBVSxHQUFHc0MsSUFBSSxDQUFDLENBQUM7SUFDckI7RUFDRixDQUFDO0VBQ0R1SCxLQUFLLENBQUMsWUFBWSxFQUFFLEVBQUUsRUFBRSxJQUFJLENBQUM7RUFDN0JBLEtBQUssQ0FBQyxPQUFPLEVBQUUsQ0FBQyxFQUFFLElBQUksQ0FBQztFQUN2QkEsS0FBSyxDQUFDLFNBQVMsRUFBRSxDQUFDLENBQUM7QUFDckI7QUFFQSxTQUFTZ0csc0JBQXNCQSxDQUFDam5CLEtBQVksRUFBRW9RLEdBQVEsRUFBUTtFQUM1RCxJQUFJLENBQUMsc0JBQXNCLEVBQUUsbUJBQW1CLEVBQUUscUJBQXFCLEVBQUUsdUJBQXVCLEVBQUUsb0JBQW9CLENBQUMsQ0FBQ2pKLElBQUksQ0FBQ2tILE9BQU8sSUFBSXJPLEtBQUssQ0FBQ3lILFFBQVEsS0FBSzRHLE9BQU8sSUFBSXJPLEtBQUssQ0FBQ3lILFFBQVEsS0FBSyxHQUFHNEcsT0FBTyxRQUFRLENBQUMsRUFBRTtFQUM5TSxNQUFNcUwsSUFBSSxHQUFHQSxDQUFBLEtBQU13TSxTQUFTLENBQUNsbUIsS0FBSyxDQUFDO0VBQ25DLE1BQU1paEIsS0FBSyxHQUFHQSxDQUFDbGlCLElBQWtCLEVBQUV5WSxLQUFhLEVBQUVpUSxTQUFTLEdBQUcsS0FBSyxLQUFLO0lBQ3RFalEsS0FBSyxHQUFHMk8sWUFBWSxDQUFDbm1CLEtBQUssRUFBRXdYLEtBQUssQ0FBQztJQUNsQyxJQUFJSixVQUFVLEdBQUdzQyxJQUFJLENBQUMsQ0FBQztJQUN2QixLQUFLLElBQUkySCxDQUFDLEdBQUcsQ0FBQyxFQUFFQSxDQUFDLEdBQUc3SixLQUFLLElBQUlKLFVBQVUsQ0FBQ3ZULE1BQU0sRUFBRXdkLENBQUMsRUFBRSxFQUFFO01BQ25ELE1BQU1saUIsS0FBSyxHQUFHaVIsR0FBRyxDQUFDNlYsSUFBSSxDQUFDN08sVUFBVSxDQUFDO01BQ2xDck4sT0FBTyxDQUFDL0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEVBQUVOLElBQUksQ0FBQztNQUN0QyxJQUFJMG9CLFNBQVMsRUFBRSxLQUFLLE1BQU0sQ0FBQ3JvQixDQUFDLEVBQUVDLENBQUMsQ0FBQyxJQUFJNE4sZUFBZTtRQUFBLElBQUFtYyxVQUFBO1FBQUUsSUFBSWhaLEdBQUcsQ0FBQ3FDLE1BQU0sQ0FBQyxFQUFFLENBQUMsSUFBSSxFQUFBMlcsVUFBQSxHQUFBaHBCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsR0FBR0EsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsR0FBR0EsQ0FBQyxDQUFDLGNBQUErcEIsVUFBQSx1QkFBeENBLFVBQUEsQ0FBMENycUIsSUFBSSxNQUFLLE9BQU8sRUFBRWdMLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEdBQUdBLENBQUMsRUFBRU4sSUFBSSxDQUFDO01BQUE7TUFDckxxWSxVQUFVLEdBQUdzQyxJQUFJLENBQUMsQ0FBQztJQUNyQjtFQUNGLENBQUM7RUFDRHVILEtBQUssQ0FBQyxLQUFLLEVBQUUsRUFBRSxFQUFFLElBQUksQ0FBQztFQUN0QkEsS0FBSyxDQUFDLFdBQVcsRUFBRSxDQUFDLEVBQUUsSUFBSSxDQUFDO0VBQzNCQSxLQUFLLENBQUMsU0FBUyxFQUFFLENBQUMsQ0FBQztBQUNyQjtBQUVBLFNBQVMxVyxXQUFXQSxDQUFDdkssS0FBWSxFQUFFMkosS0FBYSxFQUFFNlgsT0FBeUIsRUFBUTtFQUFBLElBQUE2SCxzQkFBQTtFQUNqRixNQUFNQyxTQUFTLEdBQUczZixLQUFLLENBQUMzSixLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxJQUFJbUMsS0FBSyxDQUFDOUYsTUFBTSxHQUFHLENBQUMsR0FBRyxDQUFDLEdBQUdxQyxJQUFJLENBQUNDLEdBQUcsQ0FBQyxDQUFDLEVBQUVELElBQUksQ0FBQ2xHLEtBQUssQ0FBQzJKLEtBQUssQ0FBQzlGLE1BQU0sR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDO0VBQ3BILE1BQU02ZCxTQUEwQixHQUFHMWhCLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxPQUFPLEdBQUd4SCxLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLENBQUMsVUFBVSxDQUFDLEdBQUdGLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEdBQUcsQ0FBQyxNQUFNLENBQUMsR0FBR0YsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsR0FBRyxDQUFDLGdCQUFnQixDQUFDLEdBQUcsQ0FBQyxVQUFVLENBQUMsR0FBR0YsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsR0FBRyxDQUFDLFVBQVUsQ0FBQyxHQUFHRixLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLENBQUMsTUFBTSxDQUFDLEdBQUcsQ0FBQyxXQUFXLENBQUM7RUFDMVIsTUFBTWtILElBQUksR0FBR29hLE9BQU8sQ0FBQ3pZLEtBQUssR0FBR3lZLE9BQU8sQ0FBQzVZLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzVHLElBQUksQ0FBQ2tCLFNBQVMsSUFBSStmLFNBQVMsQ0FBQ25pQixRQUFRLENBQUNvQyxTQUFTLENBQUM1QyxJQUFJLENBQUMsQ0FBQyxHQUFHdUIsU0FBUztFQUNsSCxNQUFNa2xCLFFBQVEsR0FBR3BlLElBQUksR0FBRztJQUFFaEksQ0FBQyxFQUFFZ0ksSUFBSSxDQUFDOEssU0FBUyxDQUFDOVMsQ0FBQyxHQUFHOEcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDb0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDekosS0FBSyxHQUFHLENBQUMsQ0FBQztJQUFFcEosQ0FBQyxFQUFFK0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDN1MsQ0FBQyxHQUFHNkcsSUFBSSxDQUFDbEcsS0FBSyxDQUFDb0gsSUFBSSxDQUFDOEssU0FBUyxDQUFDeEosTUFBTSxHQUFHLENBQUM7RUFBRSxDQUFDLEdBQUdvQixNQUFNLENBQUN3ZixTQUFTLENBQUM7RUFDdkssTUFBTW5xQixLQUFLLEdBQUdva0IsZUFBZSxDQUFDdmpCLEtBQUssRUFBRXdoQixPQUFPLEVBQUU7SUFBRXpmLEVBQUUsRUFBRSxTQUFTL0IsS0FBSyxDQUFDRSxLQUFLLEVBQUU7SUFBRWtrQixZQUFZLEVBQUU1QyxPQUFPLENBQUN6WSxLQUFLLEdBQUc7TUFBRTJZLFNBQVM7TUFBRTZDLFdBQVcsRUFBRSxDQUFDO01BQUVDLElBQUksRUFBRWdCLFFBQVE7TUFBRWYsWUFBWSxFQUFFO0lBQUUsQ0FBQyxHQUFHO01BQUVELElBQUksRUFBRWdCLFFBQVE7TUFBRWYsWUFBWSxFQUFFO0lBQUUsQ0FBQztJQUFFLElBQUlqRCxPQUFPLENBQUN6WSxLQUFLLEdBQUc7TUFBRXljLFFBQVEsRUFBRTtRQUFFaEIsSUFBSSxFQUFFZ0IsUUFBUTtRQUFFZixZQUFZLEVBQUU7TUFBRTtJQUFFLENBQUMsR0FBRyxDQUFDLENBQUM7RUFBRSxDQUFDLENBQUM7RUFDOVIsSUFBSSxDQUFDdGxCLEtBQUssRUFBRSxNQUFNLElBQUlvSSxLQUFLLENBQUMsMEJBQTBCdkgsS0FBSyxDQUFDRSxLQUFLLE1BQUFtcEIsc0JBQUEsR0FBSzdILE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQytHLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxjQUFBeVosc0JBQUEsdUJBQTFCQSxzQkFBQSxDQUE0QnhnQixXQUFXLENBQUNsQixJQUFJLENBQUMsSUFBSSxDQUFDLEVBQUUsQ0FBQztFQUMzSCxNQUFNNUksSUFBa0IsR0FBR2lCLEtBQUssQ0FBQ0UsS0FBSyxHQUFHLENBQUMsS0FBSyxDQUFDLEdBQUcsTUFBTSxHQUFHRixLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLFFBQVEsR0FBR0YsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsR0FBRyxPQUFPLEdBQUcsTUFBTTtFQUN2STZKLE9BQU8sQ0FBQy9KLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxFQUFFTixJQUFJLENBQUM7RUFDdEMsSUFBSUEsSUFBSSxLQUFLLE1BQU0sRUFBRWlCLEtBQUssQ0FBQ1EsTUFBTSxDQUFDd0QsSUFBSSxDQUFDdWxCLFFBQVEsQ0FBQyxVQUFVLEVBQUUsR0FBR3ZwQixLQUFLLENBQUN3SCxLQUFLLFNBQVMsRUFBRXJJLEtBQUssRUFBRSxHQUFHLEVBQUUsU0FBUyxDQUFDLENBQUM7RUFDNUcsSUFBSUosSUFBSSxLQUFLLFFBQVEsRUFBRWlCLEtBQUssQ0FBQ1EsTUFBTSxDQUFDd0QsSUFBSSxDQUFDdWxCLFFBQVEsQ0FBQyxNQUFNLEVBQUUsbUJBQW1CLEVBQUVwcUIsS0FBSyxFQUFFLEdBQUcsRUFBRSxTQUFTLENBQUMsQ0FBQztFQUN0RyxJQUFJSixJQUFJLEtBQUssT0FBTyxFQUFFaUIsS0FBSyxDQUFDUSxNQUFNLENBQUN3RCxJQUFJLENBQUN1bEIsUUFBUSxDQUFDLE1BQU0sRUFBRSxlQUFlLEVBQUVwcUIsS0FBSyxFQUFFLEdBQUcsRUFBRSxTQUFTLENBQUMsQ0FBQztBQUNuRztBQUVBLFNBQVNxTCxrQkFBa0JBLENBQUN4SyxLQUFZLEVBQUVvUSxHQUFRLEVBQUV6RyxLQUFhLEVBQVE7RUFDdkUsSUFBSTZmLGVBQWUsR0FBRyxLQUFLO0VBQzNCLEtBQUssTUFBTTFjLElBQUksSUFBSW5ELEtBQUssQ0FBQ3FMLEtBQUssQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsRUFBRTtJQUFBLElBQUF5VSxVQUFBO0lBQ3JDLE1BQU10cUIsS0FBSyxHQUFHMkssTUFBTSxDQUFDZ0QsSUFBSSxDQUFDO0lBQzFCLE1BQU00YyxJQUFJLEdBQUc7TUFBRXRxQixDQUFDLEVBQUU4RyxJQUFJLENBQUNDLEdBQUcsQ0FBQyxDQUFDLEVBQUVoSCxLQUFLLENBQUNDLENBQUMsR0FBRzhHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQzhNLElBQUksQ0FBQ0MsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDO01BQUUxTixDQUFDLEVBQUVGLEtBQUssQ0FBQ0U7SUFBRSxDQUFDO0lBQzdFLElBQUksRUFBQW9xQixVQUFBLEdBQUFycEIsT0FBTyxDQUFDSixLQUFLLEVBQUUwcEIsSUFBSSxDQUFDdHFCLENBQUMsRUFBRXNxQixJQUFJLENBQUNycUIsQ0FBQyxDQUFDLGNBQUFvcUIsVUFBQSx1QkFBOUJBLFVBQUEsQ0FBZ0MxcUIsSUFBSSxNQUFLLE9BQU8sRUFBRTtNQUNwRCxNQUFNNHFCLE1BQU0sR0FBRzNwQixLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxHQUFHLENBQUNnaUIsZUFBZSxJQUFJcFosR0FBRyxDQUFDcUMsTUFBTSxDQUFDLEVBQUUsQ0FBQyxHQUFHckMsR0FBRyxDQUFDcUMsTUFBTSxDQUFDLEVBQUUsQ0FBQztNQUM1RjFJLE9BQU8sQ0FBQy9KLEtBQUssRUFBRTBwQixJQUFJLENBQUN0cUIsQ0FBQyxFQUFFc3FCLElBQUksQ0FBQ3JxQixDQUFDLEVBQUVzcUIsTUFBTSxHQUFHLFlBQVksR0FBRyxNQUFNLENBQUM7TUFDOUQsSUFBSUEsTUFBTSxFQUFFSCxlQUFlLEdBQUcsSUFBSTtJQUNwQztFQUNGO0FBQ0Y7QUFFQSxNQUFNSSxpQkFBaUIsR0FBSTVwQixLQUFZLElBQTBCO0VBQy9ELE1BQU0rQyxLQUFLLEdBQUc7SUFBRSxHQUFHL0MsS0FBSyxDQUFDK0M7RUFBTSxDQUFDO0VBQ2hDLE1BQU1XLEtBQUssR0FBRyxDQUFDWCxLQUFLLENBQUM7RUFDckIsTUFBTVksUUFBUSxHQUFHLElBQUlOLEdBQUcsQ0FBNEIsQ0FBQyxDQUFDbkUsUUFBUSxDQUFDNkQsS0FBSyxDQUFDLEVBQUV6QyxTQUFTLENBQUMsQ0FBQyxDQUFDO0VBQ25GLE1BQU11cEIsV0FBVyxHQUFJOXFCLElBQWtCLElBQUtBLElBQUksS0FBSyxNQUFNLElBQUlBLElBQUksS0FBSyxXQUFXO0VBQ25GLEtBQUssSUFBSTZFLE1BQU0sR0FBRyxDQUFDLEVBQUVBLE1BQU0sR0FBR0YsS0FBSyxDQUFDRyxNQUFNLEVBQUVELE1BQU0sRUFBRSxFQUFFO0lBQ3BELE1BQU16RSxLQUFLLEdBQUd1RSxLQUFLLENBQUNFLE1BQU0sQ0FBQztJQUMzQixJQUFJekUsS0FBSyxDQUFDQyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSUQsS0FBSyxDQUFDRSxDQUFDLEtBQUtXLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ25HLENBQUMsRUFBRTtNQUN4RCxNQUFNeUUsSUFBYSxHQUFHLEVBQUU7TUFDeEIsS0FBSyxJQUFJcEIsT0FBMEIsR0FBR3ZELEtBQUssRUFBRXVELE9BQU8sRUFBRUEsT0FBTyxHQUFHaUIsUUFBUSxDQUFDSSxHQUFHLENBQUM3RSxRQUFRLENBQUN3RCxPQUFPLENBQUMsQ0FBQyxFQUFFb0IsSUFBSSxDQUFDRSxJQUFJLENBQUN0QixPQUFPLENBQUM7TUFDbkgsT0FBT29CLElBQUksQ0FBQ0csT0FBTyxDQUFDLENBQUM7SUFDdkI7SUFDQSxLQUFLLE1BQU0sQ0FBQzdFLENBQUMsRUFBRUMsQ0FBQyxDQUFDLElBQUk0TixlQUFlLEVBQUU7TUFBQSxJQUFBNmMsY0FBQSxFQUFBQyxVQUFBO01BQ3BDLE1BQU0zUSxJQUFJLEdBQUc7UUFBRWhhLENBQUMsRUFBRUQsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUM7UUFBRUMsQ0FBQyxFQUFFRixLQUFLLENBQUNFLENBQUMsR0FBR0E7TUFBRSxDQUFDO01BQy9DLE1BQU13aUIsR0FBRyxHQUFHM2lCLFFBQVEsQ0FBQ2thLElBQUksQ0FBQztNQUMxQixJQUFJelYsUUFBUSxDQUFDckIsR0FBRyxDQUFDdWYsR0FBRyxDQUFDLElBQUksQ0FBQ2dJLFdBQVcsRUFBQUMsY0FBQSxJQUFBQyxVQUFBLEdBQUMzcEIsT0FBTyxDQUFDSixLQUFLLEVBQUVvWixJQUFJLENBQUNoYSxDQUFDLEVBQUVnYSxJQUFJLENBQUMvWixDQUFDLENBQUMsY0FBQTBxQixVQUFBLHVCQUE5QkEsVUFBQSxDQUFnQ2hyQixJQUFJLGNBQUErcUIsY0FBQSxjQUFBQSxjQUFBLEdBQUksTUFBTSxDQUFDLEVBQUU7TUFDdkZubUIsUUFBUSxDQUFDSixHQUFHLENBQUNzZSxHQUFHLEVBQUUxaUIsS0FBSyxDQUFDO01BQ3hCdUUsS0FBSyxDQUFDTSxJQUFJLENBQUNvVixJQUFJLENBQUM7SUFDbEI7RUFDRjtFQUNBLE9BQU85WSxTQUFTO0FBQ2xCLENBQUM7QUFFRCxNQUFNbUssa0JBQWtCLEdBQUl6SyxLQUFZLElBQVc7RUFDakQsSUFBSTJFLGVBQWUsQ0FBQzNFLEtBQUssRUFBRUEsS0FBSyxDQUFDK0MsS0FBSyxFQUFFL0MsS0FBSyxDQUFDd0YsSUFBSSxDQUFDLEVBQUU7RUFDckQsTUFBTWdSLEtBQUssR0FBR29ULGlCQUFpQixDQUFDNXBCLEtBQUssQ0FBQztFQUN0QyxJQUFJLENBQUN3VyxLQUFLLEVBQUU7RUFDWixLQUFLLE1BQU1yWCxLQUFLLElBQUlxWCxLQUFLLEVBQUU7SUFDekIsTUFBTTFYLElBQUksR0FBR3NCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUM7SUFDN0MsSUFBSSxDQUFBUCxJQUFJLGFBQUpBLElBQUksdUJBQUpBLElBQUksQ0FBRUMsSUFBSSxNQUFLLFlBQVksRUFBRUQsSUFBSSxDQUFDQyxJQUFJLEdBQUcsTUFBTTtFQUNyRDtBQUNGLENBQUM7QUFFRCxNQUFNc00sbUJBQW1CLEdBQUlyTCxLQUFZLElBQVc7RUFDbEQsSUFBSTJFLGVBQWUsQ0FBQzNFLEtBQUssRUFBRUEsS0FBSyxDQUFDK0MsS0FBSyxFQUFFL0MsS0FBSyxDQUFDd0YsSUFBSSxDQUFDLEVBQUU7RUFDckRpRixrQkFBa0IsQ0FBQ3pLLEtBQUssQ0FBQztFQUN6QixJQUFJMkUsZUFBZSxDQUFDM0UsS0FBSyxFQUFFQSxLQUFLLENBQUMrQyxLQUFLLEVBQUUvQyxLQUFLLENBQUN3RixJQUFJLENBQUMsRUFBRTtFQUNyRCxNQUFNOEIsS0FBSyxHQUFHeEUsYUFBYSxDQUFDOUMsS0FBSyxFQUFFQSxLQUFLLENBQUMrQyxLQUFLLEVBQUU7SUFBRWxDLE1BQU0sRUFBRWIsS0FBSyxDQUFDd0Y7RUFBSyxDQUFDLENBQUM7RUFDdkUsTUFBTSxJQUFJK0IsS0FBSyxDQUFDLDZEQUE2REQsS0FBSyxDQUFDcEUsUUFBUSxDQUFDeUUsSUFBSSxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUM7QUFDMUcsQ0FBQztBQUVELFNBQVMrQyxlQUFlQSxDQUFDMUssS0FBWSxFQUFFb1EsR0FBUSxFQUFFekcsS0FBYSxFQUFFc00sUUFBNkIsR0FBRyxJQUFJL1UsR0FBRyxDQUFDLENBQUMsRUFBUTtFQUMvRyxLQUFLLElBQUltZ0IsQ0FBQyxHQUFHLENBQUMsRUFBRUEsQ0FBQyxHQUFHLENBQUMsRUFBRUEsQ0FBQyxFQUFFLEVBQUU7SUFDMUIsTUFBTXRpQixJQUFrQixHQUFHc2lCLENBQUMsS0FBSyxDQUFDLEdBQUcsT0FBTyxHQUFHLE9BQU87SUFDdEQsS0FBSyxJQUFJMkksT0FBTyxHQUFHLENBQUMsRUFBRUEsT0FBTyxHQUFHLEVBQUUsRUFBRUEsT0FBTyxFQUFFLEVBQUU7TUFDN0MsTUFBTTdxQixLQUFLLEdBQUc4cUIsYUFBYSxDQUFDanFCLEtBQUssRUFBRW9RLEdBQUcsRUFBRXpHLEtBQUssRUFBRXNNLFFBQVEsQ0FBQztNQUN4RCxNQUFNcFYsTUFBTSxHQUFHVCxPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDO01BQy9DLE1BQU02cUIsS0FBSyxHQUFHamQsZUFBZSxDQUFDckgsTUFBTSxDQUFDLENBQUMsQ0FBQ3hHLENBQUMsRUFBRUMsQ0FBQyxDQUFDO1FBQUEsSUFBQThxQixjQUFBLEVBQUFDLFVBQUE7UUFBQSxPQUFLOXFCLFFBQVEsRUFBQTZxQixjQUFBLElBQUFDLFVBQUEsR0FBQ2hxQixPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEdBQUdBLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEdBQUdBLENBQUMsQ0FBQyxjQUFBK3FCLFVBQUEsdUJBQXhDQSxVQUFBLENBQTBDcnJCLElBQUksY0FBQW9yQixjQUFBLGNBQUFBLGNBQUEsR0FBSSxNQUFNLENBQUM7TUFBQSxFQUFDLENBQUN0bUIsTUFBTTtNQUMzSCxJQUFJLENBQUNoRCxNQUFNLElBQUlBLE1BQU0sQ0FBQzlCLElBQUksS0FBSyxPQUFPLElBQUltckIsS0FBSyxHQUFHLENBQUMsRUFBRTtNQUNyRHJwQixNQUFNLENBQUM5QixJQUFJLEdBQUdBLElBQUk7TUFDbEI7SUFDRjtFQUNGO0FBQ0Y7QUFFQSxTQUFTaU4sV0FBV0EsQ0FBQ2hNLEtBQVksRUFBRW9RLEdBQVEsRUFBRW9SLE9BQXlCLEVBQVE7RUFBQSxJQUFBNkkscUJBQUEsRUFBQUMsaUJBQUE7RUFDNUUsTUFBTXpHLFdBQVcsR0FBRzFuQixRQUFRLENBQUN5SixNQUFNLENBQUMya0IsT0FBTyxJQUFJQSxPQUFPLENBQUMvaUIsS0FBSyxLQUFLeEgsS0FBSyxDQUFDd0gsS0FBSyxDQUFDO0VBQzdFLE1BQU1nakIsT0FBTyxHQUFHM0csV0FBVyxDQUFDamUsTUFBTSxDQUFDMmtCLE9BQU8sSUFBSUEsT0FBTyxDQUFDRSxFQUFFLEtBQUssVUFBVSxJQUFJRixPQUFPLENBQUNHLEtBQUssS0FBSyxXQUFXLENBQUM7RUFDekcsTUFBTTFrQixTQUFTLEdBQUdoRyxLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDO0VBQ2pDLE1BQU02RixhQUFhLElBQUFza0IscUJBQUEsSUFBQUMsaUJBQUEsR0FBR3RxQixLQUFLLENBQUM4SCxVQUFVLGNBQUF3aUIsaUJBQUEsdUJBQWhCQSxpQkFBQSxDQUFrQnZrQixhQUFhLGNBQUFza0IscUJBQUEsY0FBQUEscUJBQUEsR0FBSSxDQUFDO0VBQzFELE1BQU1NLFFBQTJDLEdBQUcsRUFBRTtFQUN0RCxJQUFJM2tCLFNBQVMsS0FBSyxDQUFDLEVBQUUsS0FBSyxNQUFNLENBQUM0a0IsVUFBVSxFQUFFQyxJQUFJLENBQUMsSUFBSXpzQixpQkFBaUIsQ0FBQztJQUFFb0osS0FBSyxFQUFFeEgsS0FBSyxDQUFDd0gsS0FBSztJQUFFeEIsU0FBUztJQUFFRCxhQUFhO0lBQUVnRCxLQUFLLEVBQUV5WSxPQUFPLENBQUN6WSxLQUFLO0lBQUUraEIsU0FBUyxHQUFBQyxrQkFBQSxHQUFFL3FCLEtBQUssQ0FBQ2lGLFVBQVUsY0FBQThsQixrQkFBQSx1QkFBaEJBLGtCQUFBLENBQWtCemM7RUFBZ0IsQ0FBQyxFQUFFZ1Qsa0JBQWtCLENBQUN0aEIsS0FBSyxDQUFDd0gsS0FBSyxDQUFDLENBQUMsQ0FBQ3dXLE9BQU8sQ0FBQyxDQUFDLEVBQUU7SUFBQSxJQUFBK00sa0JBQUE7SUFDeE8sTUFBTWhwQixFQUFFLEdBQUcsWUFBWS9CLEtBQUssQ0FBQ0UsS0FBSyxJQUFJMHFCLFVBQVUsSUFBSUMsSUFBSSxDQUFDRyxTQUFTLEVBQUU7SUFDcEUsSUFBSUMsTUFBeUI7SUFDN0IsS0FBSyxJQUFJQyxNQUFNLEdBQUcsQ0FBQyxFQUFFQSxNQUFNLEdBQUc3c0IsbUJBQW1CLENBQUMySCxTQUFTLEVBQUVELGFBQWEsQ0FBQyxFQUFFbWxCLE1BQU0sRUFBRSxFQUFFO01BQUEsSUFBQUMscUJBQUEsRUFBQUMsa0JBQUE7TUFDckYsSUFBSUYsTUFBTSxHQUFHLENBQUMsSUFBSSxDQUFDRCxNQUFNLEVBQUU7TUFDM0IsTUFBTXhLLFVBQVUsR0FBR3RpQixzQkFBc0IsQ0FBQ3FzQixPQUFPLEVBQUVLLElBQUksRUFBRUssTUFBTSxFQUFFNUosa0JBQWtCLENBQUN0aEIsS0FBSyxDQUFDd0gsS0FBSyxDQUFDLENBQUM7TUFDakcsSUFBSSxDQUFDaVosVUFBVSxFQUFFO01BQ2pCLE1BQU00SyxRQUFRLEdBQUdSLElBQUksQ0FBQ0csU0FBUyxLQUFLLG1CQUFtQixJQUFJRSxNQUFNLEtBQUssQ0FBQyxHQUFHM3VCLGtCQUFrQixDQUFDa2tCLFVBQVUsQ0FBQyxDQUFDN2EsTUFBTSxDQUFDN0csSUFBSSxJQUFJdWlCLGtCQUFrQixDQUFDdGhCLEtBQUssQ0FBQ3dILEtBQUssQ0FBQyxDQUFDakksUUFBUSxDQUFDUixJQUFJLENBQUMsQ0FBQyxHQUFHdUIsU0FBUztNQUNuTCxNQUFNOGpCLFlBQVksR0FBRzhHLE1BQU0sS0FBSyxDQUFDLEdBQUdHLFFBQVEsYUFBUkEsUUFBUSxlQUFSQSxRQUFRLENBQUV4bkIsTUFBTSxHQUFHO1FBQUUsR0FBR2duQixJQUFJLENBQUN6RyxZQUFZO1FBQUVDLE9BQU8sRUFBRWdIO01BQVMsQ0FBQyxHQUFHUixJQUFJLENBQUN6RyxZQUFZLEdBQUc7UUFBRUcsV0FBVyxFQUFFLENBQUM7UUFBRWMsVUFBVSxFQUFFLEtBQUs7UUFBRWIsSUFBSSxFQUFFeUcsTUFBTztRQUFFeEcsWUFBWSxFQUFFO01BQUUsQ0FBQztNQUM5TCxNQUFNZSxRQUFRLEdBQUcwRixNQUFNLEtBQUssQ0FBQyxHQUFHRyxRQUFRLGFBQVJBLFFBQVEsZUFBUkEsUUFBUSxDQUFFeG5CLE1BQU0sR0FBRztRQUFFLEdBQUdnbkIsSUFBSSxDQUFDckYsUUFBUTtRQUFFbkIsT0FBTyxFQUFFZ0g7TUFBUyxDQUFDLEdBQUdSLElBQUksQ0FBQ3JGLFFBQVEsR0FBRztRQUFFakIsV0FBVyxFQUFFLENBQUM7UUFBRWMsVUFBVSxFQUFFLEtBQUs7UUFBRWIsSUFBSSxFQUFFeUcsTUFBTztRQUFFeEcsWUFBWSxFQUFFO01BQUUsQ0FBQztNQUNsTCxNQUFNN2QsUUFBMkIsR0FBRztRQUFFN0UsRUFBRSxFQUFFLFNBQVNBLEVBQUUsSUFBSW1wQixNQUFNLElBQUl6SyxVQUFVLENBQUMxZSxFQUFFLEVBQUU7UUFBRXFpQixZQUFZO1FBQUVvQjtNQUFTLENBQUM7TUFDNUcsTUFBTXJtQixLQUFLLEdBQUdva0IsZUFBZSxDQUFDdmpCLEtBQUssRUFBRXdoQixPQUFPLEVBQUU1YSxRQUFRLEVBQUVqRixTQUFTLElBQUksQ0FBQ0EsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUN3RixJQUFJLENBQUNwRyxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ25HLENBQUMsTUFBTXNDLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDK0MsS0FBSyxDQUFDM0QsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUMrQyxLQUFLLENBQUMxRCxDQUFDLENBQUMsQ0FBQztNQUN4TSxJQUFJLENBQUNGLEtBQUssRUFBRTtNQUNaLE1BQU1tc0IsU0FBUyxHQUFHO1FBQUV2cEIsRUFBRTtRQUFFaXBCLFNBQVMsRUFBRUgsSUFBSSxDQUFDRyxTQUFTO1FBQUVDLE1BQU0sRUFBRUMsTUFBTSxLQUFLLENBQUM7UUFBRUssTUFBTSxFQUFFVixJQUFJLENBQUNVO01BQU8sQ0FBQztNQUM5RixNQUFNN3FCLEtBQUssR0FBRzhxQixZQUFZLENBQUMvSyxVQUFVLENBQUMxZSxFQUFFLEVBQUU1QyxLQUFLLEVBQUUsR0FBR3NoQixVQUFVLENBQUMxZSxFQUFFLElBQUk2b0IsVUFBVSxJQUFJTSxNQUFNLEVBQUUsRUFBRWxyQixLQUFLLENBQUM4SCxVQUFVLENBQUM7TUFDOUdwSCxLQUFLLENBQUM0cUIsU0FBUyxHQUFHQSxTQUFTO01BQzNCLElBQUlKLE1BQU0sS0FBSyxDQUFDLEtBQUs5YSxHQUFHLENBQUNxQyxNQUFNLEVBQUEwWSxxQkFBQSxJQUFBQyxrQkFBQSxHQUFDcHJCLEtBQUssQ0FBQzhILFVBQVUsY0FBQXNqQixrQkFBQSx1QkFBaEJBLGtCQUFBLENBQWtCNWtCLFdBQVcsY0FBQTJrQixxQkFBQSxjQUFBQSxxQkFBQSxHQUFJLENBQUMsQ0FBQyxJQUFLbnJCLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxnQkFBZ0IsSUFBSXhCLFNBQVMsSUFBSSxDQUFFLENBQUMsRUFBRTtRQUFBLElBQUF5bEIsYUFBQTtRQUM1SC9xQixLQUFLLENBQUNnckIsU0FBUyxHQUFHeGxCLElBQUksQ0FBQzBPLEtBQUssQ0FBQ2xVLEtBQUssQ0FBQ2dyQixTQUFTLEdBQUcsSUFBSSxDQUFDO1FBQ3BEaHJCLEtBQUssQ0FBQ0MsTUFBTSxHQUFHRCxLQUFLLENBQUNnckIsU0FBUztRQUM5QmhyQixLQUFLLENBQUNpckIsTUFBTSxJQUFJLENBQUM7UUFDakJqckIsS0FBSyxDQUFDa3JCLE1BQU0sR0FBRyxDQUFDLEtBQUFILGFBQUEsR0FBSS9xQixLQUFLLENBQUNrckIsTUFBTSxjQUFBSCxhQUFBLGNBQUFBLGFBQUEsR0FBSSxFQUFFLENBQUMsRUFBRSxPQUFPLENBQUM7TUFDbkQ7TUFDQXpyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztNQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztNQUN4QkwsTUFBTSxhQUFOQSxNQUFNLGNBQU5BLE1BQU0sR0FBTkEsTUFBTSxHQUFLOXJCLEtBQUs7SUFDbEI7RUFDRjtFQUNBLElBQUlhLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxNQUFNLElBQUl4QixTQUFTLEtBQUssQ0FBQyxJQUFJLENBQUNoRyxLQUFLLENBQUNRLE1BQU0sQ0FBQzJHLElBQUksQ0FBQ3pHLEtBQUs7SUFBQSxJQUFBbXJCLHFCQUFBLEVBQUFDLGNBQUEsRUFBQUMsVUFBQTtJQUFBLE9BQUlyckIsS0FBSyxDQUFDeU8sT0FBTyxNQUFBMGMscUJBQUEsR0FBSW5yQixLQUFLLENBQUNzckIsZUFBZSxjQUFBSCxxQkFBQSx1QkFBckJBLHFCQUFBLENBQXVCdHNCLFFBQVEsRUFBQXVzQixjQUFBLElBQUFDLFVBQUEsR0FBQzNyQixPQUFPLENBQUNKLEtBQUssRUFBRVUsS0FBSyxDQUFDdEIsQ0FBQyxFQUFFc0IsS0FBSyxDQUFDckIsQ0FBQyxDQUFDLGNBQUEwc0IsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0NodEIsSUFBSSxjQUFBK3NCLGNBQUEsY0FBQUEsY0FBQSxHQUFJLE1BQU0sQ0FBQztFQUFBLEVBQUMsRUFBRTtJQUFBLElBQUFHLHNCQUFBO0lBQ2hMLE1BQU1DLFNBQVMsR0FBR3JJLFdBQVcsQ0FBQ3BqQixJQUFJLENBQUNnZ0IsVUFBVSxJQUFJQSxVQUFVLENBQUMxZSxFQUFFLEtBQUssV0FBVyxDQUFDO0lBQy9FLE1BQU1zaUIsT0FBTyxHQUFHNkgsU0FBUyxHQUFHM3ZCLGtCQUFrQixDQUFDMnZCLFNBQVMsQ0FBQyxDQUFDdG1CLE1BQU0sQ0FBQzdHLElBQUksSUFBSXVpQixrQkFBa0IsQ0FBQ2xVLElBQUksQ0FBQzdOLFFBQVEsQ0FBQ1IsSUFBSSxDQUFDLENBQUMsR0FBRyxFQUFFO0lBQ3JILElBQUksQ0FBQ210QixTQUFTLElBQUksQ0FBQzdILE9BQU8sQ0FBQ3hnQixNQUFNLEVBQUUsTUFBTSxJQUFJMEQsS0FBSyxDQUFDLHlDQUF5QyxDQUFDO0lBQzdGLE1BQU1wSSxLQUFLLEdBQUdva0IsZUFBZSxDQUFDdmpCLEtBQUssRUFBRXdoQixPQUFPLEVBQUU7TUFBRXpmLEVBQUUsRUFBRSxrQkFBa0IvQixLQUFLLENBQUNFLEtBQUssd0JBQXdCO01BQUVra0IsWUFBWSxFQUFFO1FBQUVDLE9BQU87UUFBRUUsV0FBVyxFQUFFO01BQUU7SUFBRSxDQUFDLEVBQUU1aUIsU0FBUyxJQUFJLENBQUNBLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDd0YsSUFBSSxDQUFDcEcsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUN3RixJQUFJLENBQUNuRyxDQUFDLE1BQU1zQyxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQytDLEtBQUssQ0FBQzNELENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDK0MsS0FBSyxDQUFDMUQsQ0FBQyxDQUFDLENBQUM7SUFDeFMsSUFBSSxDQUFDRixLQUFLLEVBQUUsTUFBTSxJQUFJb0ksS0FBSyxDQUFDLHVDQUFBMGtCLHNCQUFBLEdBQXNDekssT0FBTyxDQUFDM1ksV0FBVyxDQUFDK0csRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLGNBQUFxYyxzQkFBQSx1QkFBMUJBLHNCQUFBLENBQTRCcGpCLFdBQVcsQ0FBQ2xCLElBQUksQ0FBQyxJQUFJLENBQUMsRUFBRSxDQUFDO0lBQ3ZILE1BQU0yakIsU0FBUyxHQUFHO01BQUV2cEIsRUFBRSxFQUFFLFlBQVkvQixLQUFLLENBQUNFLEtBQUssY0FBYztNQUFFOHFCLFNBQVMsRUFBRSxtQkFBNEI7TUFBRUMsTUFBTSxFQUFFLElBQUk7TUFBRU0sTUFBTSxFQUFFO0lBQW9DLENBQUM7SUFDbkssTUFBTTdxQixLQUFLLEdBQUc4cUIsWUFBWSxDQUFDVSxTQUFTLENBQUNucUIsRUFBRSxFQUFFNUMsS0FBSyxFQUFFLEdBQUcrc0IsU0FBUyxDQUFDbnFCLEVBQUUsY0FBYyxFQUFFL0IsS0FBSyxDQUFDOEgsVUFBVSxDQUFDO0lBQ2hHcEgsS0FBSyxDQUFDNHFCLFNBQVMsR0FBR0EsU0FBUztJQUMzQnRyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztJQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztFQUMxQjtFQUNBLElBQUl0ckIsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFNBQVMsSUFBSXhCLFNBQVMsS0FBSyxDQUFDLElBQUksQ0FBQ2hHLEtBQUssQ0FBQ1EsTUFBTSxDQUFDMkcsSUFBSSxDQUFDekcsS0FBSztJQUFBLElBQUF5ckIsc0JBQUEsRUFBQUMsY0FBQSxFQUFBQyxVQUFBO0lBQUEsT0FBSTNyQixLQUFLLENBQUN5TyxPQUFPLE1BQUFnZCxzQkFBQSxHQUFJenJCLEtBQUssQ0FBQ3NyQixlQUFlLGNBQUFHLHNCQUFBLHVCQUFyQkEsc0JBQUEsQ0FBdUI1c0IsUUFBUSxFQUFBNnNCLGNBQUEsSUFBQUMsVUFBQSxHQUFDanNCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFVSxLQUFLLENBQUN0QixDQUFDLEVBQUVzQixLQUFLLENBQUNyQixDQUFDLENBQUMsY0FBQWd0QixVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQ3R0QixJQUFJLGNBQUFxdEIsY0FBQSxjQUFBQSxjQUFBLEdBQUksTUFBTSxDQUFDO0VBQUEsRUFBQyxFQUFFO0lBQUEsSUFBQUUsc0JBQUE7SUFDbkwsTUFBTUMsT0FBTyxHQUFHMUksV0FBVyxDQUFDcGpCLElBQUksQ0FBQ2dnQixVQUFVLElBQUlBLFVBQVUsQ0FBQzFlLEVBQUUsS0FBSyxTQUFTLENBQUM7SUFDM0UsTUFBTXNpQixPQUFPLEdBQUdrSSxPQUFPLEdBQUdod0Isa0JBQWtCLENBQUNnd0IsT0FBTyxDQUFDLENBQUMzbUIsTUFBTSxDQUFDN0csSUFBSSxJQUFJdWlCLGtCQUFrQixDQUFDaFUsT0FBTyxDQUFDL04sUUFBUSxDQUFDUixJQUFJLENBQUMsQ0FBQyxHQUFHLEVBQUU7SUFDcEgsSUFBSSxDQUFDd3RCLE9BQU8sSUFBSSxDQUFDbEksT0FBTyxDQUFDeGdCLE1BQU0sRUFBRSxNQUFNLElBQUkwRCxLQUFLLENBQUMsNENBQTRDLENBQUM7SUFDOUYsTUFBTXBJLEtBQUssR0FBR29rQixlQUFlLENBQUN2akIsS0FBSyxFQUFFd2hCLE9BQU8sRUFBRTtNQUFFemYsRUFBRSxFQUFFLGtCQUFrQi9CLEtBQUssQ0FBQ0UsS0FBSyxzQkFBc0I7TUFBRWtrQixZQUFZLEVBQUU7UUFBRUMsT0FBTztRQUFFRSxXQUFXLEVBQUU7TUFBRTtJQUFFLENBQUMsRUFBRTVpQixTQUFTLElBQUksQ0FBQ0EsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUN3RixJQUFJLENBQUNwRyxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ25HLENBQUMsTUFBTXNDLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDK0MsS0FBSyxDQUFDM0QsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUMrQyxLQUFLLENBQUMxRCxDQUFDLENBQUMsQ0FBQztJQUN0UyxJQUFJLENBQUNGLEtBQUssRUFBRSxNQUFNLElBQUlvSSxLQUFLLENBQUMsMENBQUEra0Isc0JBQUEsR0FBeUM5SyxPQUFPLENBQUMzWSxXQUFXLENBQUMrRyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsY0FBQTBjLHNCQUFBLHVCQUExQkEsc0JBQUEsQ0FBNEJ6akIsV0FBVyxDQUFDbEIsSUFBSSxDQUFDLElBQUksQ0FBQyxFQUFFLENBQUM7SUFDMUgsTUFBTTJqQixTQUFTLEdBQUc7TUFBRXZwQixFQUFFLEVBQUUsWUFBWS9CLEtBQUssQ0FBQ0UsS0FBSyxjQUFjO01BQUU4cUIsU0FBUyxFQUFFLG1CQUE0QjtNQUFFQyxNQUFNLEVBQUUsSUFBSTtNQUFFTSxNQUFNLEVBQUU7SUFBb0MsQ0FBQztJQUNuSyxNQUFNN3FCLEtBQUssR0FBRzhxQixZQUFZLENBQUNlLE9BQU8sQ0FBQ3hxQixFQUFFLEVBQUU1QyxLQUFLLEVBQUUsR0FBR290QixPQUFPLENBQUN4cUIsRUFBRSxjQUFjLEVBQUUvQixLQUFLLENBQUM4SCxVQUFVLENBQUM7SUFDNUZwSCxLQUFLLENBQUM0cUIsU0FBUyxHQUFHQSxTQUFTO0lBQzNCdHJCLEtBQUssQ0FBQ1EsTUFBTSxDQUFDd0QsSUFBSSxDQUFDdEQsS0FBSyxDQUFDO0lBQ3hCaXFCLFFBQVEsQ0FBQzNtQixJQUFJLENBQUNzbkIsU0FBUyxDQUFDO0VBQzFCO0VBQ0EsSUFBSXRyQixLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxJQUFJeEIsU0FBUyxLQUFLLENBQUMsSUFBSSxDQUFDaEcsS0FBSyxDQUFDUSxNQUFNLENBQUMyRyxJQUFJLENBQUN6RyxLQUFLO0lBQUEsSUFBQThyQixzQkFBQSxFQUFBQyxjQUFBLEVBQUFDLFVBQUE7SUFBQSxPQUFJaHNCLEtBQUssQ0FBQ3lPLE9BQU8sTUFBQXFkLHNCQUFBLEdBQUk5ckIsS0FBSyxDQUFDc3JCLGVBQWUsY0FBQVEsc0JBQUEsdUJBQXJCQSxzQkFBQSxDQUF1Qmp0QixRQUFRLEVBQUFrdEIsY0FBQSxJQUFBQyxVQUFBLEdBQUN0c0IsT0FBTyxDQUFDSixLQUFLLEVBQUVVLEtBQUssQ0FBQ3RCLENBQUMsRUFBRXNCLEtBQUssQ0FBQ3JCLENBQUMsQ0FBQyxjQUFBcXRCLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDM3RCLElBQUksY0FBQTB0QixjQUFBLGNBQUFBLGNBQUEsR0FBSSxNQUFNLENBQUM7RUFBQSxFQUFDLEVBQUU7SUFBQSxJQUFBRSxzQkFBQTtJQUNqTCxNQUFNQyxTQUFTLEdBQUcvSSxXQUFXLENBQUNwakIsSUFBSSxDQUFDZ2dCLFVBQVUsSUFBSUEsVUFBVSxDQUFDMWUsRUFBRSxLQUFLLFdBQVcsQ0FBQztJQUMvRSxNQUFNc2lCLE9BQU8sR0FBR3VJLFNBQVMsR0FBR3J3QixrQkFBa0IsQ0FBQ3F3QixTQUFTLENBQUMsQ0FBQ2huQixNQUFNLENBQUM3RyxJQUFJLElBQUl1aUIsa0JBQWtCLENBQUNqVSxLQUFLLENBQUM5TixRQUFRLENBQUNSLElBQUksQ0FBQyxDQUFDLEdBQUcsRUFBRTtJQUN0SCxJQUFJLENBQUM2dEIsU0FBUyxJQUFJLENBQUN2SSxPQUFPLENBQUN4Z0IsTUFBTSxFQUFFLE1BQU0sSUFBSTBELEtBQUssQ0FBQyx5Q0FBeUMsQ0FBQztJQUM3RixNQUFNcEksS0FBSyxHQUFHb2tCLGVBQWUsQ0FBQ3ZqQixLQUFLLEVBQUV3aEIsT0FBTyxFQUFFO01BQUV6ZixFQUFFLEVBQUUsa0JBQWtCL0IsS0FBSyxDQUFDRSxLQUFLLHVCQUF1QjtNQUFFa2tCLFlBQVksRUFBRTtRQUFFQyxPQUFPO1FBQUVFLFdBQVcsRUFBRTtNQUFFO0lBQUUsQ0FBQyxFQUFFNWlCLFNBQVMsSUFBSSxDQUFDQSxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxNQUFNc0MsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUMrQyxLQUFLLENBQUMzRCxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQytDLEtBQUssQ0FBQzFELENBQUMsQ0FBQyxDQUFDO0lBQ3ZTLElBQUksQ0FBQ0YsS0FBSyxFQUFFLE1BQU0sSUFBSW9JLEtBQUssQ0FBQyx1Q0FBQW9sQixzQkFBQSxHQUFzQ25MLE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQytHLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxjQUFBK2Msc0JBQUEsdUJBQTFCQSxzQkFBQSxDQUE0QjlqQixXQUFXLENBQUNsQixJQUFJLENBQUMsSUFBSSxDQUFDLEVBQUUsQ0FBQztJQUN2SCxNQUFNMmpCLFNBQVMsR0FBRztNQUFFdnBCLEVBQUUsRUFBRSxZQUFZL0IsS0FBSyxDQUFDRSxLQUFLLGFBQWE7TUFBRThxQixTQUFTLEVBQUUsbUJBQTRCO01BQUVDLE1BQU0sRUFBRSxJQUFJO01BQUVNLE1BQU0sRUFBRTtJQUFvQyxDQUFDO0lBQ2xLLE1BQU03cUIsS0FBSyxHQUFHOHFCLFlBQVksQ0FBQ29CLFNBQVMsQ0FBQzdxQixFQUFFLEVBQUU1QyxLQUFLLEVBQUUsR0FBR3l0QixTQUFTLENBQUM3cUIsRUFBRSxhQUFhLEVBQUUvQixLQUFLLENBQUM4SCxVQUFVLENBQUM7SUFDL0ZwSCxLQUFLLENBQUM0cUIsU0FBUyxHQUFHQSxTQUFTO0lBQzNCdHJCLEtBQUssQ0FBQ1EsTUFBTSxDQUFDd0QsSUFBSSxDQUFDdEQsS0FBSyxDQUFDO0lBQ3hCaXFCLFFBQVEsQ0FBQzNtQixJQUFJLENBQUNzbkIsU0FBUyxDQUFDO0VBQzFCO0VBQ0EsSUFBSXRyQixLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxJQUFJeEIsU0FBUyxLQUFLLENBQUMsSUFBSSxDQUFDaEcsS0FBSyxDQUFDUSxNQUFNLENBQUMyRyxJQUFJLENBQUN6RyxLQUFLLElBQUlBLEtBQUssQ0FBQzNCLElBQUksS0FBSyxhQUFhLENBQUMsRUFBRTtJQUFBLElBQUE4dEIsc0JBQUE7SUFDM0csTUFBTUMsT0FBTyxHQUFHakosV0FBVyxDQUFDcGpCLElBQUksQ0FBQ2dnQixVQUFVLElBQUlBLFVBQVUsQ0FBQzFlLEVBQUUsS0FBSyxhQUFhLENBQUM7SUFDL0UsTUFBTXNpQixPQUFPLEdBQUd5SSxPQUFPLEdBQUd2d0Isa0JBQWtCLENBQUN1d0IsT0FBTyxDQUFDLENBQUNsbkIsTUFBTSxDQUFDN0csSUFBSSxJQUFJdWlCLGtCQUFrQixDQUFDL1QsS0FBSyxDQUFDaE8sUUFBUSxDQUFDUixJQUFJLENBQUMsQ0FBQyxHQUFHLEVBQUU7SUFDbEgsSUFBSSxDQUFDK3RCLE9BQU8sSUFBSSxDQUFDekksT0FBTyxDQUFDeGdCLE1BQU0sRUFBRSxNQUFNLElBQUkwRCxLQUFLLENBQUMsd0NBQXdDLENBQUM7SUFDMUYsTUFBTXBJLEtBQUssR0FBR29rQixlQUFlLENBQUN2akIsS0FBSyxFQUFFd2hCLE9BQU8sRUFBRTtNQUFFemYsRUFBRSxFQUFFLGtCQUFrQi9CLEtBQUssQ0FBQ0UsS0FBSyx3QkFBd0I7TUFBRWtrQixZQUFZLEVBQUU7UUFBRUMsT0FBTztRQUFFRSxXQUFXLEVBQUU7TUFBRSxDQUFDO01BQUVpQixRQUFRLEVBQUU7UUFBRW5CLE9BQU8sRUFBRSxDQUFDLE9BQU8sQ0FBQztRQUFFRSxXQUFXLEVBQUU7TUFBRTtJQUFFLENBQUMsRUFBRTVpQixTQUFTLElBQUksQ0FBQ0EsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUN3RixJQUFJLENBQUNwRyxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ25HLENBQUMsTUFBTXNDLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDK0MsS0FBSyxDQUFDM0QsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUMrQyxLQUFLLENBQUMxRCxDQUFDLENBQUMsQ0FBQztJQUMxVixJQUFJLENBQUNGLEtBQUssRUFBRSxNQUFNLElBQUlvSSxLQUFLLENBQUMsc0NBQUFzbEIsc0JBQUEsR0FBcUNyTCxPQUFPLENBQUMzWSxXQUFXLENBQUMrRyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsY0FBQWlkLHNCQUFBLHVCQUExQkEsc0JBQUEsQ0FBNEJoa0IsV0FBVyxDQUFDbEIsSUFBSSxDQUFDLElBQUksQ0FBQyxFQUFFLENBQUM7SUFDdEgsTUFBTTJqQixTQUFTLEdBQUc7TUFBRXZwQixFQUFFLEVBQUUsWUFBWS9CLEtBQUssQ0FBQ0UsS0FBSyxZQUFZO01BQUU4cUIsU0FBUyxFQUFFLGtCQUEyQjtNQUFFQyxNQUFNLEVBQUUsSUFBSTtNQUFFTSxNQUFNLEVBQUU7SUFBNkMsQ0FBQztJQUN6SyxNQUFNN3FCLEtBQUssR0FBRzhxQixZQUFZLENBQUNzQixPQUFPLENBQUMvcUIsRUFBRSxFQUFFNUMsS0FBSyxFQUFFLEdBQUcydEIsT0FBTyxDQUFDL3FCLEVBQUUsWUFBWSxFQUFFL0IsS0FBSyxDQUFDOEgsVUFBVSxDQUFDO0lBQzFGcEgsS0FBSyxDQUFDNHFCLFNBQVMsR0FBR0EsU0FBUztJQUMzQnRyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztJQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztFQUMxQjtFQUNBLElBQUl0ckIsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFNBQVMsSUFBSXhCLFNBQVMsS0FBSyxDQUFDLElBQUksQ0FBQ2hHLEtBQUssQ0FBQ1EsTUFBTSxDQUFDMkcsSUFBSSxDQUFDekcsS0FBSztJQUFBLElBQUFxc0Isc0JBQUEsRUFBQUMsY0FBQSxFQUFBQyxVQUFBO0lBQUEsT0FBSXZzQixLQUFLLENBQUMzQixJQUFJLEtBQUssWUFBWSxNQUFBZ3VCLHNCQUFBLEdBQUlyc0IsS0FBSyxDQUFDc3JCLGVBQWUsY0FBQWUsc0JBQUEsdUJBQXJCQSxzQkFBQSxDQUF1Qnh0QixRQUFRLEVBQUF5dEIsY0FBQSxJQUFBQyxVQUFBLEdBQUM3c0IsT0FBTyxDQUFDSixLQUFLLEVBQUVVLEtBQUssQ0FBQ3RCLENBQUMsRUFBRXNCLEtBQUssQ0FBQ3JCLENBQUMsQ0FBQyxjQUFBNHRCLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDbHVCLElBQUksY0FBQWl1QixjQUFBLGNBQUFBLGNBQUEsR0FBSSxNQUFNLENBQUM7RUFBQSxFQUFDLEVBQUU7SUFBQSxJQUFBRSxzQkFBQTtJQUNqTSxNQUFNQyxVQUFVLEdBQUd0SixXQUFXLENBQUNwakIsSUFBSSxDQUFDZ2dCLFVBQVUsSUFBSUEsVUFBVSxDQUFDMWUsRUFBRSxLQUFLLFlBQVksQ0FBQztJQUNqRixNQUFNc2lCLE9BQU8sR0FBRzhJLFVBQVUsR0FBRzV3QixrQkFBa0IsQ0FBQzR3QixVQUFVLENBQUMsQ0FBQ3ZuQixNQUFNLENBQUM3RyxJQUFJLElBQUlBLElBQUksS0FBSyxVQUFVLENBQUMsR0FBRyxFQUFFO0lBQ3BHLElBQUksQ0FBQ291QixVQUFVLElBQUksQ0FBQzlJLE9BQU8sQ0FBQ3hnQixNQUFNLEVBQUUsTUFBTSxJQUFJMEQsS0FBSyxDQUFDLHlDQUF5QyxDQUFDO0lBQzlGLE1BQU1wSSxLQUFLLEdBQUdva0IsZUFBZSxDQUFDdmpCLEtBQUssRUFBRXdoQixPQUFPLEVBQUU7TUFBRXpmLEVBQUUsRUFBRSxrQkFBa0IvQixLQUFLLENBQUNFLEtBQUsseUJBQXlCO01BQUVra0IsWUFBWSxFQUFFO1FBQUVDLE9BQU87UUFBRUUsV0FBVyxFQUFFO01BQUUsQ0FBQztNQUFFaUIsUUFBUSxFQUFFO1FBQUVuQixPQUFPO1FBQUVFLFdBQVcsRUFBRTtNQUFFO0lBQUUsQ0FBQyxFQUFFNWlCLFNBQVMsSUFBSSxDQUFDQSxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxNQUFNc0MsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUMrQyxLQUFLLENBQUMzRCxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQytDLEtBQUssQ0FBQzFELENBQUMsQ0FBQyxDQUFDO0lBQ2hWLElBQUksQ0FBQ0YsS0FBSyxFQUFFLE1BQU0sSUFBSW9JLEtBQUssQ0FBQywwQ0FBQTJsQixzQkFBQSxHQUF5QzFMLE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQytHLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxjQUFBc2Qsc0JBQUEsdUJBQTFCQSxzQkFBQSxDQUE0QnJrQixXQUFXLENBQUNsQixJQUFJLENBQUMsSUFBSSxDQUFDLEVBQUUsQ0FBQztJQUMxSCxNQUFNMmpCLFNBQVMsR0FBRztNQUFFdnBCLEVBQUUsRUFBRSxZQUFZL0IsS0FBSyxDQUFDRSxLQUFLLGNBQWM7TUFBRThxQixTQUFTLEVBQUUsa0JBQTJCO01BQUVDLE1BQU0sRUFBRSxJQUFJO01BQUVNLE1BQU0sRUFBRTtJQUFnRCxDQUFDO0lBQzlLLE1BQU03cUIsS0FBSyxHQUFHOHFCLFlBQVksQ0FBQzJCLFVBQVUsQ0FBQ3ByQixFQUFFLEVBQUU1QyxLQUFLLEVBQUUsR0FBR2d1QixVQUFVLENBQUNwckIsRUFBRSxjQUFjLEVBQUUvQixLQUFLLENBQUM4SCxVQUFVLENBQUM7SUFDbEdwSCxLQUFLLENBQUM0cUIsU0FBUyxHQUFHQSxTQUFTO0lBQzNCdHJCLEtBQUssQ0FBQ1EsTUFBTSxDQUFDd0QsSUFBSSxDQUFDdEQsS0FBSyxDQUFDO0lBQ3hCaXFCLFFBQVEsQ0FBQzNtQixJQUFJLENBQUNzbkIsU0FBUyxDQUFDO0VBQzFCO0VBQ0EsSUFBSXRyQixLQUFLLENBQUN3SCxLQUFLLEtBQUssY0FBYyxJQUFJeEIsU0FBUyxLQUFLLENBQUMsSUFBSSxDQUFDaEcsS0FBSyxDQUFDUSxNQUFNLENBQUMyRyxJQUFJLENBQUN6RyxLQUFLO0lBQUEsSUFBQTBzQixzQkFBQSxFQUFBQyxjQUFBLEVBQUFDLFVBQUE7SUFBQSxPQUFJNXNCLEtBQUssQ0FBQzNCLElBQUksS0FBSyxZQUFZLE1BQUFxdUIsc0JBQUEsR0FBSTFzQixLQUFLLENBQUNzckIsZUFBZSxjQUFBb0Isc0JBQUEsdUJBQXJCQSxzQkFBQSxDQUF1Qjd0QixRQUFRLEVBQUE4dEIsY0FBQSxJQUFBQyxVQUFBLEdBQUNsdEIsT0FBTyxDQUFDSixLQUFLLEVBQUVVLEtBQUssQ0FBQ3RCLENBQUMsRUFBRXNCLEtBQUssQ0FBQ3JCLENBQUMsQ0FBQyxjQUFBaXVCLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDdnVCLElBQUksY0FBQXN1QixjQUFBLGNBQUFBLGNBQUEsR0FBSSxNQUFNLENBQUM7RUFBQSxFQUFDLEVBQUU7SUFBQSxJQUFBRSxzQkFBQTtJQUN0TSxNQUFNQyxVQUFVLEdBQUczSixXQUFXLENBQUNwakIsSUFBSSxDQUFDZ2dCLFVBQVUsSUFBSUEsVUFBVSxDQUFDMWUsRUFBRSxLQUFLLFlBQVksQ0FBQztJQUNqRixNQUFNc2lCLE9BQU8sR0FBR21KLFVBQVUsR0FBR2p4QixrQkFBa0IsQ0FBQ2l4QixVQUFVLENBQUMsQ0FBQzVuQixNQUFNLENBQUM3RyxJQUFJLElBQUlBLElBQUksS0FBSyxTQUFTLENBQUMsR0FBRyxFQUFFO0lBQ25HLElBQUksQ0FBQ3l1QixVQUFVLElBQUksQ0FBQ25KLE9BQU8sQ0FBQ3hnQixNQUFNLEVBQUUsTUFBTSxJQUFJMEQsS0FBSyxDQUFDLDRDQUE0QyxDQUFDO0lBQ2pHLE1BQU1wSSxLQUFLLEdBQUdva0IsZUFBZSxDQUFDdmpCLEtBQUssRUFBRXdoQixPQUFPLEVBQUU7TUFBRXpmLEVBQUUsRUFBRSxrQkFBa0IvQixLQUFLLENBQUNFLEtBQUssNEJBQTRCO01BQUVra0IsWUFBWSxFQUFFO1FBQUVDLE9BQU87UUFBRUUsV0FBVyxFQUFFO01BQUUsQ0FBQztNQUFFaUIsUUFBUSxFQUFFO1FBQUVuQixPQUFPO1FBQUVFLFdBQVcsRUFBRTtNQUFFO0lBQUUsQ0FBQyxFQUFFNWlCLFNBQVMsSUFBSSxDQUFDQSxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxNQUFNc0MsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUMrQyxLQUFLLENBQUMzRCxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQytDLEtBQUssQ0FBQzFELENBQUMsQ0FBQyxDQUFDO0lBQ25WLElBQUksQ0FBQ0YsS0FBSyxFQUFFLE1BQU0sSUFBSW9JLEtBQUssQ0FBQyw2Q0FBQWdtQixzQkFBQSxHQUE0Qy9MLE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQytHLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxjQUFBMmQsc0JBQUEsdUJBQTFCQSxzQkFBQSxDQUE0QjFrQixXQUFXLENBQUNsQixJQUFJLENBQUMsSUFBSSxDQUFDLEVBQUUsQ0FBQztJQUM3SCxNQUFNMmpCLFNBQVMsR0FBRztNQUFFdnBCLEVBQUUsRUFBRSxZQUFZL0IsS0FBSyxDQUFDRSxLQUFLLGlCQUFpQjtNQUFFOHFCLFNBQVMsRUFBRSxtQkFBNEI7TUFBRUMsTUFBTSxFQUFFLElBQUk7TUFBRU0sTUFBTSxFQUFFO0lBQXdDLENBQUM7SUFDMUssTUFBTTdxQixLQUFLLEdBQUc4cUIsWUFBWSxDQUFDZ0MsVUFBVSxDQUFDenJCLEVBQUUsRUFBRTVDLEtBQUssRUFBRSxHQUFHcXVCLFVBQVUsQ0FBQ3pyQixFQUFFLGlCQUFpQixFQUFFL0IsS0FBSyxDQUFDOEgsVUFBVSxDQUFDO0lBQ3JHcEgsS0FBSyxDQUFDNHFCLFNBQVMsR0FBR0EsU0FBUztJQUMzQnRyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztJQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztFQUMxQjtFQUNBLElBQUl0ckIsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFFBQVEsSUFBSXhCLFNBQVMsS0FBSyxDQUFDLElBQUksQ0FBQ2hHLEtBQUssQ0FBQ1EsTUFBTSxDQUFDMkcsSUFBSSxDQUFDekcsS0FBSztJQUFBLElBQUErc0IsVUFBQTtJQUFBLE9BQUkvc0IsS0FBSyxDQUFDM0IsSUFBSSxLQUFLLFdBQVcsSUFBSSxFQUFBMHVCLFVBQUEsR0FBQXJ0QixPQUFPLENBQUNKLEtBQUssRUFBRVUsS0FBSyxDQUFDdEIsQ0FBQyxFQUFFc0IsS0FBSyxDQUFDckIsQ0FBQyxDQUFDLGNBQUFvdUIsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0MxdUIsSUFBSSxNQUFLLE9BQU87RUFBQSxFQUFDLEVBQUU7SUFBQSxJQUFBMnVCLHNCQUFBO0lBQ2hLLE1BQU1DLFNBQVMsR0FBRzlKLFdBQVcsQ0FBQ3BqQixJQUFJLENBQUNnZ0IsVUFBVSxJQUFJQSxVQUFVLENBQUMxZSxFQUFFLEtBQUssV0FBVyxDQUFDO0lBQy9FLElBQUksQ0FBQzRyQixTQUFTLEVBQUUsTUFBTSxJQUFJcG1CLEtBQUssQ0FBQyxxQ0FBcUMsQ0FBQztJQUN0RSxNQUFNcEksS0FBSyxHQUFHb2tCLGVBQWUsQ0FBQ3ZqQixLQUFLLEVBQUV3aEIsT0FBTyxFQUFFO01BQUV6ZixFQUFFLEVBQUUsa0JBQWtCL0IsS0FBSyxDQUFDRSxLQUFLLHdCQUF3QjtNQUFFa2tCLFlBQVksRUFBRTtRQUFFQyxPQUFPLEVBQUUsQ0FBQyxPQUFPLENBQUM7UUFBRTNDLFNBQVMsRUFBRSxDQUFDLGdCQUFnQixDQUFDO1FBQUU2QyxXQUFXLEVBQUUsQ0FBQztRQUFFcUIsS0FBSyxFQUFFO01BQU0sQ0FBQztNQUFFSixRQUFRLEVBQUU7UUFBRW5CLE9BQU8sRUFBRSxDQUFDLE9BQU8sQ0FBQztRQUFFRSxXQUFXLEVBQUU7TUFBRTtJQUFFLENBQUMsRUFBRTVpQixTQUFTLElBQUksQ0FBQ0EsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUN3RixJQUFJLENBQUNwRyxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ25HLENBQUMsTUFBTXNDLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDK0MsS0FBSyxDQUFDM0QsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUMrQyxLQUFLLENBQUMxRCxDQUFDLENBQUMsQ0FBQztJQUNsWixJQUFJLENBQUNGLEtBQUssRUFBRSxNQUFNLElBQUlvSSxLQUFLLENBQUMseUNBQUFtbUIsc0JBQUEsR0FBd0NsTSxPQUFPLENBQUMzWSxXQUFXLENBQUMrRyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsY0FBQThkLHNCQUFBLHVCQUExQkEsc0JBQUEsQ0FBNEI3a0IsV0FBVyxDQUFDbEIsSUFBSSxDQUFDLElBQUksQ0FBQyxFQUFFLENBQUM7SUFDekgsTUFBTTJqQixTQUFTLEdBQUc7TUFBRXZwQixFQUFFLEVBQUUsWUFBWS9CLEtBQUssQ0FBQ0UsS0FBSyxjQUFjO01BQUU4cUIsU0FBUyxFQUFFLGdCQUF5QjtNQUFFQyxNQUFNLEVBQUUsSUFBSTtNQUFFTSxNQUFNLEVBQUU7SUFBc0QsQ0FBQztJQUNsTCxNQUFNN3FCLEtBQUssR0FBRzhxQixZQUFZLENBQUNtQyxTQUFTLENBQUM1ckIsRUFBRSxFQUFFNUMsS0FBSyxFQUFFLEdBQUd3dUIsU0FBUyxDQUFDNXJCLEVBQUUsY0FBYyxFQUFFL0IsS0FBSyxDQUFDOEgsVUFBVSxDQUFDO0lBQ2hHcEgsS0FBSyxDQUFDNHFCLFNBQVMsR0FBR0EsU0FBUztJQUMzQnRyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztJQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztFQUMxQjtFQUNBLElBQUl0ckIsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFFBQVEsSUFBSXhCLFNBQVMsS0FBSyxDQUFDLElBQUksQ0FBQ2hHLEtBQUssQ0FBQ1EsTUFBTSxDQUFDMkcsSUFBSSxDQUFDekcsS0FBSztJQUFBLElBQUFrdEIsc0JBQUEsRUFBQUMsY0FBQSxFQUFBQyxVQUFBO0lBQUEsT0FBSXB0QixLQUFLLENBQUMzQixJQUFJLEtBQUssWUFBWSxNQUFBNnVCLHNCQUFBLEdBQUlsdEIsS0FBSyxDQUFDc3JCLGVBQWUsY0FBQTRCLHNCQUFBLHVCQUFyQkEsc0JBQUEsQ0FBdUJydUIsUUFBUSxFQUFBc3VCLGNBQUEsSUFBQUMsVUFBQSxHQUFDMXRCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFVSxLQUFLLENBQUN0QixDQUFDLEVBQUVzQixLQUFLLENBQUNyQixDQUFDLENBQUMsY0FBQXl1QixVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQy91QixJQUFJLGNBQUE4dUIsY0FBQSxjQUFBQSxjQUFBLEdBQUksTUFBTSxDQUFDO0VBQUEsRUFBQyxFQUFFO0lBQUEsSUFBQUUsc0JBQUE7SUFDaE0sTUFBTUMsVUFBVSxHQUFHbkssV0FBVyxDQUFDcGpCLElBQUksQ0FBQ2dnQixVQUFVLElBQUlBLFVBQVUsQ0FBQzFlLEVBQUUsS0FBSyxZQUFZLENBQUM7SUFDakYsSUFBSSxDQUFDaXNCLFVBQVUsRUFBRSxNQUFNLElBQUl6bUIsS0FBSyxDQUFDLHFDQUFxQyxDQUFDO0lBQ3ZFLE1BQU1wSSxLQUFLLEdBQUdva0IsZUFBZSxDQUFDdmpCLEtBQUssRUFBRXdoQixPQUFPLEVBQUU7TUFBRXpmLEVBQUUsRUFBRSxrQkFBa0IvQixLQUFLLENBQUNFLEtBQUssK0JBQStCO01BQUVra0IsWUFBWSxFQUFFO1FBQUVDLE9BQU8sRUFBRSxDQUFDLFdBQVcsQ0FBQztRQUFFMUMsU0FBUyxFQUFFLENBQUMsUUFBUSxFQUFFLFVBQVUsQ0FBQztRQUFFc00sUUFBUSxFQUFFLElBQUk7UUFBRTFKLFdBQVcsRUFBRTtNQUFFLENBQUM7TUFBRWlCLFFBQVEsRUFBRTtRQUFFbkIsT0FBTyxFQUFFLENBQUMsV0FBVyxDQUFDO1FBQUVFLFdBQVcsRUFBRTtNQUFFO0lBQUUsQ0FBQyxFQUFFNWlCLFNBQVMsSUFBSSxDQUFDQSxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxNQUFNc0MsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUMrQyxLQUFLLENBQUMzRCxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQytDLEtBQUssQ0FBQzFELENBQUMsQ0FBQyxDQUFDO0lBQ3ZhLElBQUksQ0FBQ0YsS0FBSyxFQUFFLE1BQU0sSUFBSW9JLEtBQUssQ0FBQywrQ0FBQXdtQixzQkFBQSxHQUE4Q3ZNLE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQytHLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxjQUFBbWUsc0JBQUEsdUJBQTFCQSxzQkFBQSxDQUE0QmxsQixXQUFXLENBQUNsQixJQUFJLENBQUMsSUFBSSxDQUFDLEVBQUUsQ0FBQztJQUMvSCxNQUFNMmpCLFNBQVMsR0FBRztNQUFFdnBCLEVBQUUsRUFBRSxZQUFZL0IsS0FBSyxDQUFDRSxLQUFLLG9CQUFvQjtNQUFFOHFCLFNBQVMsRUFBRSxtQkFBNEI7TUFBRUMsTUFBTSxFQUFFLElBQUk7TUFBRU0sTUFBTSxFQUFFO0lBQXFELENBQUM7SUFDMUwsTUFBTTdxQixLQUFLLEdBQUc4cUIsWUFBWSxDQUFDd0MsVUFBVSxDQUFDanNCLEVBQUUsRUFBRTVDLEtBQUssRUFBRSxHQUFHNnVCLFVBQVUsQ0FBQ2pzQixFQUFFLG9CQUFvQixFQUFFL0IsS0FBSyxDQUFDOEgsVUFBVSxDQUFDO0lBQ3hHcEgsS0FBSyxDQUFDNHFCLFNBQVMsR0FBR0EsU0FBUztJQUMzQnRyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztJQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztFQUMxQjtFQUNBLElBQUl0ckIsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFdBQVcsSUFBSXhCLFNBQVMsS0FBSyxDQUFDLElBQUksQ0FBQ2hHLEtBQUssQ0FBQ1EsTUFBTSxDQUFDMkcsSUFBSSxDQUFDekcsS0FBSztJQUFBLElBQUF3dEIsc0JBQUEsRUFBQUMsZUFBQSxFQUFBQyxVQUFBO0lBQUEsT0FBSTF0QixLQUFLLENBQUMzQixJQUFJLEtBQUssa0JBQWtCLE1BQUFtdkIsc0JBQUEsR0FBSXh0QixLQUFLLENBQUNzckIsZUFBZSxjQUFBa0Msc0JBQUEsdUJBQXJCQSxzQkFBQSxDQUF1QjN1QixRQUFRLEVBQUE0dUIsZUFBQSxJQUFBQyxVQUFBLEdBQUNodUIsT0FBTyxDQUFDSixLQUFLLEVBQUVVLEtBQUssQ0FBQ3RCLENBQUMsRUFBRXNCLEtBQUssQ0FBQ3JCLENBQUMsQ0FBQyxjQUFBK3VCLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDcnZCLElBQUksY0FBQW92QixlQUFBLGNBQUFBLGVBQUEsR0FBSSxNQUFNLENBQUM7RUFBQSxFQUFDLEVBQUU7SUFBQSxJQUFBRSxzQkFBQTtJQUN6TSxNQUFNQyxnQkFBZ0IsR0FBR3pLLFdBQVcsQ0FBQ3BqQixJQUFJLENBQUNnZ0IsVUFBVSxJQUFJQSxVQUFVLENBQUMxZSxFQUFFLEtBQUssa0JBQWtCLENBQUM7SUFDN0YsSUFBSSxDQUFDdXNCLGdCQUFnQixFQUFFLE1BQU0sSUFBSS9tQixLQUFLLENBQUMsc0NBQXNDLENBQUM7SUFDOUUsTUFBTXBJLEtBQUssR0FBR29rQixlQUFlLENBQUN2akIsS0FBSyxFQUFFd2hCLE9BQU8sRUFBRTtNQUFFemYsRUFBRSxFQUFFLGtCQUFrQi9CLEtBQUssQ0FBQ0UsS0FBSyxrQ0FBa0M7TUFBRWtrQixZQUFZLEVBQUU7UUFBRUMsT0FBTyxFQUFFLENBQUMsWUFBWSxDQUFDO1FBQUUzQyxTQUFTLEVBQUUsQ0FBQyxnQkFBZ0IsQ0FBQztRQUFFNkMsV0FBVyxFQUFFLENBQUM7UUFBRXFCLEtBQUssRUFBRTtNQUFNLENBQUM7TUFBRUosUUFBUSxFQUFFO1FBQUVuQixPQUFPLEVBQUUsQ0FBQyxZQUFZLENBQUM7UUFBRUUsV0FBVyxFQUFFO01BQUU7SUFBRSxDQUFDLEVBQUU1aUIsU0FBUyxJQUFJLENBQUNBLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDd0YsSUFBSSxDQUFDcEcsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUN3RixJQUFJLENBQUNuRyxDQUFDLE1BQU1zQyxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQytDLEtBQUssQ0FBQzNELENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDK0MsS0FBSyxDQUFDMUQsQ0FBQyxDQUFDLENBQUM7SUFDdGEsSUFBSSxDQUFDRixLQUFLLEVBQUUsTUFBTSxJQUFJb0ksS0FBSyxDQUFDLDBDQUFBOG1CLHNCQUFBLEdBQXlDN00sT0FBTyxDQUFDM1ksV0FBVyxDQUFDK0csRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLGNBQUF5ZSxzQkFBQSx1QkFBMUJBLHNCQUFBLENBQTRCeGxCLFdBQVcsQ0FBQ2xCLElBQUksQ0FBQyxJQUFJLENBQUMsRUFBRSxDQUFDO0lBQzFILE1BQU0yakIsU0FBUyxHQUFHO01BQUV2cEIsRUFBRSxFQUFFLFlBQVkvQixLQUFLLENBQUNFLEtBQUssaUJBQWlCO01BQUU4cUIsU0FBUyxFQUFFLGdCQUF5QjtNQUFFQyxNQUFNLEVBQUUsSUFBSTtNQUFFTSxNQUFNLEVBQUU7SUFBeUQsQ0FBQztJQUN4TCxNQUFNN3FCLEtBQUssR0FBRzhxQixZQUFZLENBQUM4QyxnQkFBZ0IsQ0FBQ3ZzQixFQUFFLEVBQUU1QyxLQUFLLEVBQUUsR0FBR212QixnQkFBZ0IsQ0FBQ3ZzQixFQUFFLGlCQUFpQixFQUFFL0IsS0FBSyxDQUFDOEgsVUFBVSxDQUFDO0lBQ2pIcEgsS0FBSyxDQUFDNHFCLFNBQVMsR0FBR0EsU0FBUztJQUMzQnRyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztJQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztFQUMxQjtFQUNBLElBQUl0ckIsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLGdCQUFnQixJQUFJeEIsU0FBUyxLQUFLLENBQUMsSUFBSSxDQUFDaEcsS0FBSyxDQUFDUSxNQUFNLENBQUMyRyxJQUFJLENBQUN6RyxLQUFLO0lBQUEsSUFBQTZ0QixzQkFBQSxFQUFBQyxlQUFBLEVBQUFDLFVBQUE7SUFBQSxPQUFJL3RCLEtBQUssQ0FBQzNCLElBQUksS0FBSyxZQUFZLE1BQUF3dkIsc0JBQUEsR0FBSTd0QixLQUFLLENBQUNzckIsZUFBZSxjQUFBdUMsc0JBQUEsdUJBQXJCQSxzQkFBQSxDQUF1Qmh2QixRQUFRLEVBQUFpdkIsZUFBQSxJQUFBQyxVQUFBLEdBQUNydUIsT0FBTyxDQUFDSixLQUFLLEVBQUVVLEtBQUssQ0FBQ3RCLENBQUMsRUFBRXNCLEtBQUssQ0FBQ3JCLENBQUMsQ0FBQyxjQUFBb3ZCLFVBQUEsdUJBQWhDQSxVQUFBLENBQWtDMXZCLElBQUksY0FBQXl2QixlQUFBLGNBQUFBLGVBQUEsR0FBSSxNQUFNLENBQUM7RUFBQSxFQUFDLEVBQUU7SUFBQSxJQUFBRSx1QkFBQTtJQUN4TSxNQUFNQyxVQUFVLEdBQUc5SyxXQUFXLENBQUNwakIsSUFBSSxDQUFDZ2dCLFVBQVUsSUFBSUEsVUFBVSxDQUFDMWUsRUFBRSxLQUFLLFlBQVksQ0FBQztJQUNqRixJQUFJLENBQUM0c0IsVUFBVSxFQUFFLE1BQU0sSUFBSXBuQixLQUFLLENBQUMsK0JBQStCLENBQUM7SUFDakUsTUFBTXBJLEtBQUssR0FBR29rQixlQUFlLENBQUN2akIsS0FBSyxFQUFFd2hCLE9BQU8sRUFBRTtNQUFFemYsRUFBRSxFQUFFLGtCQUFrQi9CLEtBQUssQ0FBQ0UsS0FBSyx5QkFBeUI7TUFBRWtrQixZQUFZLEVBQUU7UUFBRUMsT0FBTyxFQUFFLENBQUMsS0FBSyxDQUFDO1FBQUUxQyxTQUFTLEVBQUUsQ0FBQyxRQUFRLEVBQUUsVUFBVSxDQUFDO1FBQUVzTSxRQUFRLEVBQUUsSUFBSTtRQUFFMUosV0FBVyxFQUFFLENBQUM7UUFBRXFCLEtBQUssRUFBRTtNQUFNLENBQUM7TUFBRUosUUFBUSxFQUFFO1FBQUVuQixPQUFPLEVBQUUsQ0FBQyxLQUFLLENBQUM7UUFBRUUsV0FBVyxFQUFFO01BQUU7SUFBRSxDQUFDLEVBQUU1aUIsU0FBUyxJQUFJLENBQUNBLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDd0YsSUFBSSxDQUFDcEcsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUN3RixJQUFJLENBQUNuRyxDQUFDLE1BQU1zQyxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQytDLEtBQUssQ0FBQzNELENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDK0MsS0FBSyxDQUFDMUQsQ0FBQyxDQUFDLENBQUM7SUFDbmEsSUFBSSxDQUFDRixLQUFLLEVBQUUsTUFBTSxJQUFJb0ksS0FBSyxDQUFDLHdDQUFBbW5CLHVCQUFBLEdBQXVDbE4sT0FBTyxDQUFDM1ksV0FBVyxDQUFDK0csRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLGNBQUE4ZSx1QkFBQSx1QkFBMUJBLHVCQUFBLENBQTRCN2xCLFdBQVcsQ0FBQ2xCLElBQUksQ0FBQyxJQUFJLENBQUMsRUFBRSxDQUFDO0lBQ3hILE1BQU0yakIsU0FBUyxHQUFHO01BQUV2cEIsRUFBRSxFQUFFLFlBQVkvQixLQUFLLENBQUNFLEtBQUssY0FBYztNQUFFOHFCLFNBQVMsRUFBRSxhQUFzQjtNQUFFQyxNQUFNLEVBQUUsSUFBSTtNQUFFTSxNQUFNLEVBQUU7SUFBc0QsQ0FBQztJQUMvSyxNQUFNN3FCLEtBQUssR0FBRzhxQixZQUFZLENBQUNtRCxVQUFVLENBQUM1c0IsRUFBRSxFQUFFNUMsS0FBSyxFQUFFLEdBQUd3dkIsVUFBVSxDQUFDNXNCLEVBQUUsY0FBYyxFQUFFL0IsS0FBSyxDQUFDOEgsVUFBVSxDQUFDO0lBQ2xHcEgsS0FBSyxDQUFDNHFCLFNBQVMsR0FBR0EsU0FBUztJQUMzQnRyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztJQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztFQUMxQjtFQUNBLElBQUl0ckIsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLGdCQUFnQixJQUFJeEIsU0FBUyxLQUFLLENBQUMsSUFBSSxDQUFDaEcsS0FBSyxDQUFDUSxNQUFNLENBQUMyRyxJQUFJLENBQUN6RyxLQUFLO0lBQUEsSUFBQWt1QixzQkFBQSxFQUFBQyxlQUFBLEVBQUFDLFVBQUE7SUFBQSxPQUFJcHVCLEtBQUssQ0FBQzNCLElBQUksS0FBSyxnQkFBZ0IsTUFBQTZ2QixzQkFBQSxHQUFJbHVCLEtBQUssQ0FBQ3NyQixlQUFlLGNBQUE0QyxzQkFBQSx1QkFBckJBLHNCQUFBLENBQXVCcnZCLFFBQVEsRUFBQXN2QixlQUFBLElBQUFDLFVBQUEsR0FBQzF1QixPQUFPLENBQUNKLEtBQUssRUFBRVUsS0FBSyxDQUFDdEIsQ0FBQyxFQUFFc0IsS0FBSyxDQUFDckIsQ0FBQyxDQUFDLGNBQUF5dkIsVUFBQSx1QkFBaENBLFVBQUEsQ0FBa0MvdkIsSUFBSSxjQUFBOHZCLGVBQUEsY0FBQUEsZUFBQSxHQUFJLE1BQU0sQ0FBQztFQUFBLEVBQUMsRUFBRTtJQUFBLElBQUFFLHVCQUFBO0lBQzVNLE1BQU1DLGNBQWMsR0FBR25MLFdBQVcsQ0FBQ3BqQixJQUFJLENBQUNnZ0IsVUFBVSxJQUFJQSxVQUFVLENBQUMxZSxFQUFFLEtBQUssZ0JBQWdCLENBQUM7SUFDekYsSUFBSSxDQUFDaXRCLGNBQWMsRUFBRSxNQUFNLElBQUl6bkIsS0FBSyxDQUFDLG1DQUFtQyxDQUFDO0lBQ3pFLE1BQU1wSSxLQUFLLEdBQUdva0IsZUFBZSxDQUFDdmpCLEtBQUssRUFBRXdoQixPQUFPLEVBQUU7TUFBRXpmLEVBQUUsRUFBRSxrQkFBa0IvQixLQUFLLENBQUNFLEtBQUssZ0NBQWdDO01BQUVra0IsWUFBWSxFQUFFO1FBQUVDLE9BQU8sRUFBRSxDQUFDLEtBQUssQ0FBQztRQUFFMUMsU0FBUyxFQUFFLENBQUMsUUFBUSxFQUFFLFVBQVUsQ0FBQztRQUFFc00sUUFBUSxFQUFFLElBQUk7UUFBRTFKLFdBQVcsRUFBRSxDQUFDO1FBQUVxQixLQUFLLEVBQUU7TUFBTSxDQUFDO01BQUVKLFFBQVEsRUFBRTtRQUFFbkIsT0FBTyxFQUFFLENBQUMsS0FBSyxDQUFDO1FBQUVFLFdBQVcsRUFBRTtNQUFFO0lBQUUsQ0FBQyxFQUFFNWlCLFNBQVMsSUFBSSxDQUFDQSxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxNQUFNc0MsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLWSxLQUFLLENBQUMrQyxLQUFLLENBQUMzRCxDQUFDLElBQUl1QyxTQUFTLENBQUN0QyxDQUFDLEtBQUtXLEtBQUssQ0FBQytDLEtBQUssQ0FBQzFELENBQUMsQ0FBQyxDQUFDO0lBQzFhLElBQUksQ0FBQ0YsS0FBSyxFQUFFLE1BQU0sSUFBSW9JLEtBQUssQ0FBQywyQ0FBQXduQix1QkFBQSxHQUEwQ3ZOLE9BQU8sQ0FBQzNZLFdBQVcsQ0FBQytHLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxjQUFBbWYsdUJBQUEsdUJBQTFCQSx1QkFBQSxDQUE0QmxtQixXQUFXLENBQUNsQixJQUFJLENBQUMsSUFBSSxDQUFDLEVBQUUsQ0FBQztJQUMzSCxNQUFNMmpCLFNBQVMsR0FBRztNQUFFdnBCLEVBQUUsRUFBRSxZQUFZL0IsS0FBSyxDQUFDRSxLQUFLLGlCQUFpQjtNQUFFOHFCLFNBQVMsRUFBRSxnQkFBeUI7TUFBRUMsTUFBTSxFQUFFLElBQUk7TUFBRU0sTUFBTSxFQUFFO0lBQXlELENBQUM7SUFDeEwsTUFBTTdxQixLQUFLLEdBQUc4cUIsWUFBWSxDQUFDd0QsY0FBYyxDQUFDanRCLEVBQUUsRUFBRTVDLEtBQUssRUFBRSxHQUFHNnZCLGNBQWMsQ0FBQ2p0QixFQUFFLGlCQUFpQixFQUFFL0IsS0FBSyxDQUFDOEgsVUFBVSxDQUFDO0lBQzdHcEgsS0FBSyxDQUFDNHFCLFNBQVMsR0FBR0EsU0FBUztJQUMzQnRyQixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztJQUN4QmlxQixRQUFRLENBQUMzbUIsSUFBSSxDQUFDc25CLFNBQVMsQ0FBQztFQUMxQjtFQUNBLElBQUl0ckIsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxLQUFLLENBQUMsRUFBRTtJQUFBLElBQUErdUIsY0FBQSxFQUFBQyxxQkFBQSxFQUFBQyxrQkFBQTtJQUN6QixNQUFNQyxRQUFRLEdBQUd2TCxXQUFXLENBQUNwakIsSUFBSSxDQUFDOHBCLE9BQU8sSUFBSUEsT0FBTyxDQUFDRSxFQUFFLEtBQUssVUFBVSxDQUFFO0lBQ3hFLE1BQU0vcEIsS0FBSyxHQUFHOHFCLFlBQVksQ0FBQzRELFFBQVEsQ0FBQ3J0QixFQUFFLEVBQUUvQixLQUFLLENBQUN3RixJQUFJLEVBQUUsR0FBRzRwQixRQUFRLENBQUNydEIsRUFBRSxLQUFLLEVBQUUvQixLQUFLLENBQUM4SCxVQUFVLENBQUM7SUFDMUZwSCxLQUFLLENBQUNrckIsTUFBTSxHQUFHLENBQUMsS0FBQXFELGNBQUEsR0FBSXZ1QixLQUFLLENBQUNrckIsTUFBTSxjQUFBcUQsY0FBQSxjQUFBQSxjQUFBLEdBQUksRUFBRSxDQUFDLEVBQUUsUUFBQUMscUJBQUEsSUFBQUMsa0JBQUEsR0FBT252QixLQUFLLENBQUNpRixVQUFVLGNBQUFrcUIsa0JBQUEsdUJBQWhCQSxrQkFBQSxDQUFrQjltQixLQUFLLGNBQUE2bUIscUJBQUEsY0FBQUEscUJBQUEsR0FBSSxRQUFRLFdBQVcsQ0FBQztJQUMvRmx2QixLQUFLLENBQUNRLE1BQU0sQ0FBQ3dELElBQUksQ0FBQ3RELEtBQUssQ0FBQztFQUMxQjtFQUNBWix1QkFBdUIsQ0FBQ3lELEdBQUcsQ0FBQ3ZELEtBQUssRUFBRTJxQixRQUFRLENBQUM7QUFDOUM7QUFFQSxTQUFTMWUsWUFBWUEsQ0FBQ2pNLEtBQVksRUFBRXdoQixPQUF5QixFQUFRO0VBQUEsSUFBQTZOLHVCQUFBLEVBQUFDLHFCQUFBLEVBQUFDLHFCQUFBLEVBQUFDLFVBQUE7RUFDbkUsTUFBTUMsT0FBTyxHQUFHbHhCLGlCQUFpQixDQUFDeUIsS0FBSyxDQUFDd0gsS0FBSyxDQUFDO0VBQzlDLE1BQU1rb0IsYUFBYSxHQUFHLENBQUMxdkIsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFNBQVMsSUFBSXhILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxPQUFPLElBQUl4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxJQUFJeEgsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFNBQVMsSUFBSXhILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxjQUFjLElBQUl4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssUUFBUSxJQUFJeEgsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFFBQVEsSUFBSXhILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxXQUFXLElBQUl4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssZ0JBQWdCLEtBQUtnYSxPQUFPLENBQUN6WSxLQUFLO0VBQ2xULE1BQU1uQyxRQUEyQixHQUFHOG9CLGFBQWEsR0FDN0M7SUFBRTN0QixFQUFFLEVBQUUsV0FBVzB0QixPQUFPLENBQUMxd0IsSUFBSSxFQUFFO0lBQUVxbEIsWUFBWSxFQUFFO01BQUVDLE9BQU8sRUFBRXJrQixLQUFLLENBQUN3SCxLQUFLLEtBQUssT0FBTyxHQUFHLENBQUMsTUFBTSxDQUFDLEdBQUd4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssU0FBUyxHQUFHLENBQUMsT0FBTyxDQUFDLEdBQUd4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssY0FBYyxHQUFHLENBQUMsU0FBUyxDQUFDLEdBQUd4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssUUFBUSxHQUFHLENBQUMsT0FBTyxDQUFDLEdBQUd4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssUUFBUSxHQUFHLENBQUMsV0FBVyxDQUFDLEdBQUd4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssV0FBVyxHQUFHLENBQUMsWUFBWSxDQUFDLEdBQUd4SCxLQUFLLENBQUN3SCxLQUFLLEtBQUssZ0JBQWdCLEdBQUcsQ0FBQyxLQUFLLENBQUMsR0FBRyxDQUFDLE9BQU8sQ0FBQztNQUFFbWEsU0FBUyxFQUFFLENBQUMsUUFBUSxFQUFFLFVBQVUsQ0FBQztNQUFFc00sUUFBUSxFQUFFLElBQUk7TUFBRTFKLFdBQVcsRUFBRTtJQUFFLENBQUM7SUFBRWlCLFFBQVEsRUFBRTtNQUFFbkIsT0FBTyxFQUFFcmtCLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxPQUFPLEdBQUcsQ0FBQyxNQUFNLENBQUMsR0FBR3hILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxTQUFTLEdBQUcsQ0FBQyxPQUFPLENBQUMsR0FBR3hILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxjQUFjLEdBQUcsQ0FBQyxTQUFTLENBQUMsR0FBR3hILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxRQUFRLEdBQUcsQ0FBQyxPQUFPLENBQUMsR0FBR3hILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxRQUFRLEdBQUcsQ0FBQyxXQUFXLENBQUMsR0FBR3hILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxXQUFXLEdBQUcsQ0FBQyxZQUFZLENBQUMsR0FBR3hILEtBQUssQ0FBQ3dILEtBQUssS0FBSyxnQkFBZ0IsR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLENBQUMsT0FBTyxDQUFDO01BQUUrYyxXQUFXLEVBQUU7SUFBRTtFQUFFLENBQUMsR0FDaHhCO0lBQUV4aUIsRUFBRSxFQUFFLFdBQVcwdEIsT0FBTyxDQUFDMXdCLElBQUksRUFBRTtJQUFFcWxCLFlBQVksRUFBRTtNQUFFQyxPQUFPLEVBQUVvTCxPQUFPLENBQUNwTCxPQUFPO01BQUVFLFdBQVcsRUFBRSxDQUFDO01BQUVjLFVBQVUsRUFBRTtJQUFNLENBQUM7SUFBRUcsUUFBUSxFQUFFO01BQUVuQixPQUFPLEVBQUUsQ0FBQyxPQUFPLENBQUM7TUFBRUUsV0FBVyxFQUFFLENBQUM7TUFBRWMsVUFBVSxFQUFFO0lBQU07RUFBRSxDQUFDO0VBQ3pMLE1BQU1sbUIsS0FBSyxHQUFHb2tCLGVBQWUsQ0FBQ3ZqQixLQUFLLEVBQUV3aEIsT0FBTyxFQUFFNWEsUUFBUSxFQUFFakYsU0FBUyxJQUFJLENBQUNBLFNBQVMsQ0FBQ3ZDLENBQUMsS0FBS1ksS0FBSyxDQUFDK0MsS0FBSyxDQUFDM0QsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdEMsQ0FBQyxLQUFLVyxLQUFLLENBQUMrQyxLQUFLLENBQUMxRCxDQUFDLE1BQU1zQyxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxDQUFDLENBQUM7RUFDeE0sSUFBSSxDQUFDRixLQUFLLEVBQUUsTUFBTSxJQUFJb0ksS0FBSyxDQUFDLG9CQUFvQlgsUUFBUSxDQUFDN0UsRUFBRSxNQUFBc3RCLHVCQUFBLEdBQUs3TixPQUFPLENBQUMzWSxXQUFXLENBQUMrRyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsY0FBQXlmLHVCQUFBLHVCQUExQkEsdUJBQUEsQ0FBNEJ4bUIsV0FBVyxDQUFDbEIsSUFBSSxDQUFDLElBQUksQ0FBQyxFQUFFLENBQUM7RUFDckgsTUFBTXFlLE1BQU0sR0FBR2htQixLQUFLLENBQUNRLE1BQU0sQ0FBQ0MsSUFBSSxDQUFDQyxLQUFLLElBQUlBLEtBQUssQ0FBQ3lPLE9BQU8sS0FBS3pPLEtBQUssQ0FBQ2l2QixVQUFVLEtBQUssT0FBTyxJQUFJanZCLEtBQUssQ0FBQ2l2QixVQUFVLEtBQUssU0FBUyxDQUFDLENBQUM7RUFDNUgsTUFBTXZvQixJQUFJLEdBQUdvYSxPQUFPLENBQUN6WSxLQUFLLElBQUF1bUIscUJBQUEsR0FBRzlOLE9BQU8sQ0FBQzVZLEtBQUssQ0FBQ3ZCLEtBQUssQ0FBQzVHLElBQUksQ0FBQ2tCLFNBQVMsSUFBSXhDLEtBQUssQ0FBQ0MsQ0FBQyxJQUFJdUMsU0FBUyxDQUFDdVEsU0FBUyxDQUFDOVMsQ0FBQyxJQUFJRCxLQUFLLENBQUNDLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ3VRLFNBQVMsQ0FBQzlTLENBQUMsR0FBR3VDLFNBQVMsQ0FBQ3VRLFNBQVMsQ0FBQ3pKLEtBQUssSUFBSXRKLEtBQUssQ0FBQ0UsQ0FBQyxJQUFJc0MsU0FBUyxDQUFDdVEsU0FBUyxDQUFDN1MsQ0FBQyxJQUFJRixLQUFLLENBQUNFLENBQUMsR0FBR3NDLFNBQVMsQ0FBQ3VRLFNBQVMsQ0FBQzdTLENBQUMsR0FBR3NDLFNBQVMsQ0FBQ3VRLFNBQVMsQ0FBQ3hKLE1BQU0sQ0FBQyxjQUFBNG1CLHFCQUFBLHVCQUExT0EscUJBQUEsQ0FBNE9NLE1BQU0sR0FBR3R2QixTQUFTO0VBQzNSLE1BQU1rVyxLQUFLLEdBQUdnTCxPQUFPLENBQUN6WSxLQUFLLElBQUF3bUIscUJBQUEsR0FBRy9OLE9BQU8sQ0FBQzVZLEtBQUssQ0FBQ3VOLEtBQUssQ0FBQzFWLElBQUksQ0FBQzRWLElBQUksSUFBSUEsSUFBSSxDQUFDNEMsS0FBSyxDQUFDOVIsSUFBSSxDQUFDNFUsSUFBSSxJQUFJQSxJQUFJLENBQUMzYyxDQUFDLEtBQUtELEtBQUssQ0FBQ0MsQ0FBQyxJQUFJMmMsSUFBSSxDQUFDMWMsQ0FBQyxLQUFLRixLQUFLLENBQUNFLENBQUMsQ0FBQyxDQUFDLGNBQUFrd0IscUJBQUEsdUJBQW5HQSxxQkFBQSxDQUFxR3ZXLEtBQUssQ0FBQyxDQUFDLENBQUMsR0FBRzFZLFNBQVM7RUFDdkosTUFBTTZoQixPQUFPLEdBQUc3akIsZUFBZSxDQUFDMEIsS0FBSyxFQUFFYixLQUFLLEdBQUFxd0IsVUFBQSxHQUFFeEosTUFBTSxhQUFOQSxNQUFNLHVCQUFOQSxNQUFNLENBQUVqa0IsRUFBRSxjQUFBeXRCLFVBQUEsY0FBQUEsVUFBQSxHQUFJLEdBQUd4dkIsS0FBSyxDQUFDd0gsS0FBSyxJQUFJaW9CLE9BQU8sQ0FBQzF3QixJQUFJLEVBQUUsRUFBRXFJLElBQUksRUFBRW9QLEtBQUssQ0FBQztFQUMxRyxJQUFJeFcsS0FBSyxDQUFDd0gsS0FBSyxLQUFLLFFBQVEsSUFBSWdhLE9BQU8sQ0FBQ3pZLEtBQUssRUFBRTtJQUFBLElBQUE4bUIsVUFBQTtJQUM3QyxNQUFNQyxNQUFNLEdBQUcsQ0FBQztNQUFFbFosU0FBUyxFQUFFLEdBQVk7TUFBRXhYLENBQUMsRUFBRSxDQUFDO01BQUVDLENBQUMsRUFBRSxDQUFDO0lBQUUsQ0FBQyxFQUFFO01BQUV1WCxTQUFTLEVBQUUsR0FBWTtNQUFFeFgsQ0FBQyxFQUFFLENBQUM7TUFBRUMsQ0FBQyxFQUFFO0lBQUUsQ0FBQyxFQUFFO01BQUV1WCxTQUFTLEVBQUUsR0FBWTtNQUFFeFgsQ0FBQyxFQUFFLENBQUM7TUFBRUMsQ0FBQyxFQUFFO0lBQUUsQ0FBQyxFQUFFO01BQUV1WCxTQUFTLEVBQUUsR0FBWTtNQUFFeFgsQ0FBQyxFQUFFLENBQUMsQ0FBQztNQUFFQyxDQUFDLEVBQUU7SUFBRSxDQUFDLENBQUMsQ0FBQ29CLElBQUksQ0FBQ2dvQixLQUFLLElBQUk7TUFBQSxJQUFBc0gsWUFBQTtNQUNsTSxNQUFNbHZCLE1BQU0sR0FBR1QsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxHQUFHcXBCLEtBQUssQ0FBQ3JwQixDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxHQUFHb3BCLEtBQUssQ0FBQ3BwQixDQUFDLENBQUM7TUFDbkUsT0FBT0MsUUFBUSxFQUFBeXdCLFlBQUEsR0FBQ2x2QixNQUFNLGFBQU5BLE1BQU0sdUJBQU5BLE1BQU0sQ0FBRTlCLElBQUksY0FBQWd4QixZQUFBLGNBQUFBLFlBQUEsR0FBSSxNQUFNLENBQUMsSUFBSSxFQUFDbHZCLE1BQU0sYUFBTkEsTUFBTSxlQUFOQSxNQUFNLENBQUV3WCxJQUFJO0lBQzFELENBQUMsQ0FBQztJQUNGLElBQUksQ0FBQ3lYLE1BQU0sRUFBRSxNQUFNLElBQUl2b0IsS0FBSyxDQUFDLGtDQUFrQ3ZILEtBQUssQ0FBQ3lILFFBQVEsRUFBRSxDQUFDO0lBQ2hGLE1BQU13VixNQUFNLEdBQUc3YyxPQUFPLENBQUNKLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEdBQUcwd0IsTUFBTSxDQUFDMXdCLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLEdBQUd5d0IsTUFBTSxDQUFDendCLENBQUMsQ0FBRTtJQUN0RTRkLE1BQU0sQ0FBQ2xlLElBQUksR0FBRyxNQUFNO0lBQ3BCa2UsTUFBTSxDQUFDbkQsU0FBUyxJQUFBK1YsVUFBQSxHQUFHenZCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsY0FBQXd3QixVQUFBLHVCQUFoQ0EsVUFBQSxDQUFrQy9WLFNBQVM7SUFDOUQsT0FBT21ELE1BQU0sQ0FBQzVFLElBQUk7SUFDbEI4SixPQUFPLENBQUM2TixVQUFVLEdBQUc7TUFBRXBaLFNBQVMsRUFBRWtaLE1BQU0sQ0FBQ2xaLFNBQVM7TUFBRXdFLE1BQU0sRUFBRTtJQUFTLENBQUM7RUFDeEU7RUFDQSxJQUFJcGIsS0FBSyxDQUFDaUYsVUFBVSxFQUFFO0lBQ3BCa2QsT0FBTyxDQUFDOE4sT0FBTyxHQUFHLEdBQUdqd0IsS0FBSyxDQUFDaUYsVUFBVSxDQUFDa2QsT0FBTyxLQUFLQSxPQUFPLENBQUM4TixPQUFPLEVBQUU7SUFDbkU5TixPQUFPLENBQUMrTixTQUFTLEdBQUcsQ0FBQyxHQUFHbHdCLEtBQUssQ0FBQ2lGLFVBQVUsQ0FBQzRCLEtBQUssS0FBSzdHLEtBQUssQ0FBQ2lGLFVBQVUsQ0FBQ3VFLE9BQU8sRUFBRSxFQUFFLEdBQUcyWSxPQUFPLENBQUMrTixTQUFTLENBQUM7SUFDcEcsSUFBSWx3QixLQUFLLENBQUNpRixVQUFVLENBQUM0QixLQUFLLEtBQUssUUFBUSxFQUFFO01BQUVzYixPQUFPLENBQUNnTyxRQUFRLEdBQUdqcUIsSUFBSSxDQUFDQyxHQUFHLENBQUMsQ0FBQyxFQUFFZ2MsT0FBTyxDQUFDZ08sUUFBUSxHQUFHLENBQUMsQ0FBQztNQUFFaE8sT0FBTyxDQUFDaU8sUUFBUSxFQUFFO0lBQUM7RUFDdEg7RUFDQSxNQUFNQyxTQUFTLEdBQUcsQ0FBQ2xPLE9BQU8sQ0FBQztFQUMzQixJQUFJbmlCLEtBQUssQ0FBQ3dILEtBQUssS0FBSyxRQUFRLEVBQUUsS0FBSyxNQUFNLENBQUN0SCxLQUFLLEVBQUVvd0IsVUFBVSxDQUFDLElBQUksRUFBQUMscUJBQUEsSUFBQUMsbUJBQUEsR0FBQ3h3QixLQUFLLENBQUN1ZSxZQUFZLGNBQUFpUyxtQkFBQSx1QkFBbEJBLG1CQUFBLENBQW9CblMsV0FBVyxjQUFBa1MscUJBQUEsY0FBQUEscUJBQUEsR0FBSSxFQUFFLEVBQUV2YixLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUNnSixPQUFPLENBQUMsQ0FBQyxFQUFFO0lBQUEsSUFBQXVTLHFCQUFBLEVBQUFDLG1CQUFBLEVBQUFDLFdBQUE7SUFDMUgsTUFBTTV2QixNQUFNLEdBQUd5dkIsVUFBVSxDQUFDN3ZCLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDdkMsQ0FBQyxLQUFLRCxLQUFLLENBQUNDLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS0YsS0FBSyxDQUFDRSxDQUFDLENBQUM7SUFDL0YsSUFBSSxDQUFDd0IsTUFBTSxFQUFFO0lBQ2IsTUFBTTZ2QixpQkFBaUIsR0FBR3B5QixlQUFlLENBQUMwQixLQUFLLEVBQUVhLE1BQU0sR0FBQTR2QixXQUFBLEdBQUV6SyxNQUFNLGFBQU5BLE1BQU0sdUJBQU5BLE1BQU0sQ0FBRWprQixFQUFFLGNBQUEwdUIsV0FBQSxjQUFBQSxXQUFBLEdBQUksR0FBR3p3QixLQUFLLENBQUN3SCxLQUFLLElBQUlpb0IsT0FBTyxDQUFDMXdCLElBQUksRUFBRSxFQUFFcUksSUFBSSxFQUFFLFFBQVEsQ0FBQztJQUN4SHNwQixpQkFBaUIsQ0FBQ1AsUUFBUSxHQUFHaE8sT0FBTyxDQUFDZ08sUUFBUSxHQUFHLENBQUNqd0IsS0FBSyxHQUFHLENBQUMsSUFBSSxDQUFDO0lBQy9Ed3dCLGlCQUFpQixDQUFDTixRQUFRLEdBQUdqTyxPQUFPLENBQUNpTyxRQUFRO0lBQzdDTSxpQkFBaUIsQ0FBQ1QsT0FBTyxHQUFHOU4sT0FBTyxDQUFDOE4sT0FBTztJQUMzQ1MsaUJBQWlCLENBQUNSLFNBQVMsR0FBRyxDQUFDLEdBQUcvTixPQUFPLENBQUMrTixTQUFTLENBQUM7SUFDcERHLFNBQVMsQ0FBQ3JzQixJQUFJLENBQUMwc0IsaUJBQWlCLENBQUM7RUFDbkM7RUFDQTF3QixLQUFLLENBQUNtaUIsT0FBTyxHQUFHa08sU0FBUztBQUMzQjtBQUVBLFNBQVNua0IsVUFBVUEsQ0FBQ2xNLEtBQVksRUFBRW9RLEdBQVEsRUFBRXVnQixNQUFjLEVBQUVuUCxPQUF5QixFQUFRO0VBQUEsSUFBQW9QLHFCQUFBLEVBQUFDLGtCQUFBLEVBQUFDLHNCQUFBLEVBQUFDLGtCQUFBLEVBQUFDLHFCQUFBLEVBQUFDLGtCQUFBO0VBQzNGLE1BQU1DLFFBQVEsR0FBRyxHQUFHLEdBQUcsRUFBQU4scUJBQUEsSUFBQUMsa0JBQUEsR0FBQzd3QixLQUFLLENBQUM4SCxVQUFVLGNBQUErb0Isa0JBQUEsdUJBQWhCQSxrQkFBQSxDQUFrQjVxQixNQUFNLGNBQUEycUIscUJBQUEsY0FBQUEscUJBQUEsR0FBSSxDQUFDLElBQUksRUFBRTtFQUMzRCxNQUFNblAsUUFBUSxHQUFHdmxCLEtBQUssQ0FBQzBKLE1BQU0sQ0FBQ3FjLElBQUk7SUFBQSxJQUFBa1Asc0JBQUEsRUFBQUMsa0JBQUE7SUFBQSxPQUFJblAsSUFBSSxDQUFDb1AsUUFBUSxLQUFLLEtBQUssSUFBSXBQLElBQUksQ0FBQ3FQLEtBQUssSUFBSUosUUFBUSxLQUFLLENBQUNqUCxJQUFJLENBQUNzUCxJQUFJLElBQUluaEIsR0FBRyxDQUFDcUMsTUFBTSxDQUFDLEVBQUUsR0FBRyxFQUFBMGUsc0JBQUEsSUFBQUMsa0JBQUEsR0FBQ3B4QixLQUFLLENBQUM4SCxVQUFVLGNBQUFzcEIsa0JBQUEsdUJBQWhCQSxrQkFBQSxDQUFrQnJyQixhQUFhLGNBQUFvckIsc0JBQUEsY0FBQUEsc0JBQUEsR0FBSSxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUM7RUFBQSxFQUFDO0VBQ3ZLLE1BQU1LLElBQUksR0FBRy9QLFFBQVEsQ0FBQzVkLE1BQU0sR0FBRzRkLFFBQVEsR0FBR3ZsQixLQUFLLENBQUMwSixNQUFNLENBQUNxYyxJQUFJLElBQUlBLElBQUksQ0FBQ29QLFFBQVEsS0FBSyxLQUFLLEtBQUssQ0FBQ3BQLElBQUksQ0FBQ3NQLElBQUksSUFBSW5oQixHQUFHLENBQUNxQyxNQUFNLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQztFQUN6SCxNQUFNK0UsS0FBSyxHQUFHdFIsSUFBSSxDQUFDME8sS0FBSyxDQUFDLENBQUMsRUFBRSxHQUFHNVUsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxHQUFHLENBQUMsR0FBR2dHLElBQUksQ0FBQ2xHLEtBQUssQ0FBQyxFQUFBOHdCLHNCQUFBLElBQUFDLGtCQUFBLEdBQUMvd0IsS0FBSyxDQUFDOEgsVUFBVSxjQUFBaXBCLGtCQUFBLHVCQUFoQkEsa0JBQUEsQ0FBa0JockIsYUFBYSxjQUFBK3FCLHNCQUFBLGNBQUFBLHNCQUFBLEdBQUksQ0FBQyxJQUFJLENBQUMsQ0FBQyxNQUFBRSxxQkFBQSxJQUFBQyxrQkFBQSxHQUFLanhCLEtBQUssQ0FBQzhILFVBQVUsY0FBQW1wQixrQkFBQSx1QkFBaEJBLGtCQUFBLENBQWtCUSxnQkFBZ0IsY0FBQVQscUJBQUEsY0FBQUEscUJBQUEsR0FBSSxDQUFDLENBQUMsQ0FBQztFQUN6SixNQUFNbnZCLElBQUksR0FBR25ELHNCQUFzQixDQUFDO0lBQUVxSixJQUFJLEVBQUUvSCxLQUFLLENBQUMrSCxJQUFJO0lBQUVsTCxVQUFVLEVBQUVtRCxLQUFLLENBQUNFLEtBQUs7SUFBRXNILEtBQUssRUFBRXhILEtBQUssQ0FBQ3dILEtBQUs7SUFBRVcsUUFBUSxFQUFFbkksS0FBSyxDQUFDeUgsUUFBUTtJQUFFdWUsTUFBTSxFQUFFO0VBQU8sQ0FBQyxDQUFDO0VBQ2hKLEtBQUssSUFBSTNFLENBQUMsR0FBRyxDQUFDLEVBQUVBLENBQUMsR0FBRzdKLEtBQUssRUFBRTZKLENBQUMsRUFBRSxFQUFFO0lBQUEsSUFBQXFRLHVCQUFBO0lBQzlCLE1BQU05cUIsUUFBMkIsR0FBRzRhLE9BQU8sQ0FBQ3pZLEtBQUssSUFBSXNZLENBQUMsR0FBRyxDQUFDLEdBQ3REO01BQUV0ZixFQUFFLEVBQUUsUUFBUXNmLENBQUMsRUFBRTtNQUFFK0MsWUFBWSxFQUFFO1FBQUUxQyxTQUFTLEVBQUUsQ0FBQyxnQkFBZ0IsQ0FBQztRQUFFNkMsV0FBVyxFQUFFO01BQUUsQ0FBQztNQUFFaUIsUUFBUSxFQUFFO1FBQUVuQixPQUFPLEVBQUUsQ0FBQyxPQUFPLENBQUM7UUFBRUUsV0FBVyxFQUFFO01BQUU7SUFBRSxDQUFDLEdBQ3RJO01BQUV4aUIsRUFBRSxFQUFFLFFBQVFzZixDQUFDLEVBQUU7TUFBRStDLFlBQVksRUFBRTtRQUFFQyxPQUFPLEVBQUUsQ0FBQyxPQUFPLENBQUM7UUFBRUUsV0FBVyxFQUFFO01BQUU7SUFBRSxDQUFDO0lBQzdFLE1BQU1wbEIsS0FBSyxHQUFHb2tCLGVBQWUsQ0FBQ3ZqQixLQUFLLEVBQUV3aEIsT0FBTyxFQUFFNWEsUUFBUSxFQUFFakYsU0FBUyxJQUFJQSxTQUFTLENBQUN2QyxDQUFDLEtBQUtZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ3BHLENBQUMsSUFBSXVDLFNBQVMsQ0FBQ3RDLENBQUMsS0FBS1csS0FBSyxDQUFDd0YsSUFBSSxDQUFDbkcsQ0FBQyxDQUFDO0lBQ2xJLElBQUksQ0FBQ0YsS0FBSyxFQUFFLE1BQU0sSUFBSW9JLEtBQUssQ0FBQyxvQkFBb0JYLFFBQVEsQ0FBQzdFLEVBQUUsTUFBQTJ2Qix1QkFBQSxHQUFLbFEsT0FBTyxDQUFDM1ksV0FBVyxDQUFDK0csRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLGNBQUE4aEIsdUJBQUEsdUJBQTFCQSx1QkFBQSxDQUE0QjdvQixXQUFXLENBQUNsQixJQUFJLENBQUMsSUFBSSxDQUFDLEVBQUUsQ0FBQztJQUNySCxNQUFNNUYsRUFBRSxHQUFHc2YsQ0FBQyxLQUFLLENBQUMsSUFBSXJoQixLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLEtBQUssQ0FBQyxHQUFHLEtBQUssR0FBRzJCLElBQUksSUFBSXdmLENBQUMsS0FBSzdKLEtBQUssR0FBRyxDQUFDLEdBQUcsTUFBTSxHQUFHcEgsR0FBRyxDQUFDNlYsSUFBSSxDQUFDdUwsSUFBSSxDQUFDLENBQUN6dkIsRUFBRTtJQUMxRy9CLEtBQUssQ0FBQ21KLEtBQUssQ0FBQ25GLElBQUksQ0FBQztNQUFFakMsRUFBRTtNQUFFM0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNDLENBQUM7TUFBRUMsQ0FBQyxFQUFFRixLQUFLLENBQUNFLENBQUM7TUFBRW1ZLEtBQUssRUFBRSxDQUFDO01BQUUsSUFBSTNWLElBQUksSUFBSXdmLENBQUMsS0FBSzdKLEtBQUssR0FBRyxDQUFDLEdBQUc7UUFBRTNWO01BQUssQ0FBQyxHQUFHLENBQUMsQ0FBQztJQUFFLENBQUMsQ0FBQztFQUMxRztBQUNGO0FBRUEsU0FBU29vQixhQUFhQSxDQUFDanFCLEtBQVksRUFBRW9RLEdBQVEsRUFBRXpHLEtBQWEsRUFBRXNNLFFBQTZCLEdBQUcsSUFBSS9VLEdBQUcsQ0FBQyxDQUFDLEVBQVM7RUFDOUcsS0FBSyxJQUFJeXdCLEtBQUssR0FBRyxDQUFDLEVBQUVBLEtBQUssR0FBRyxHQUFHLEVBQUVBLEtBQUssRUFBRSxFQUFFO0lBQ3hDLE1BQU03a0IsSUFBSSxHQUFHc0QsR0FBRyxDQUFDNlYsSUFBSSxDQUFDdGMsS0FBSyxDQUFDO0lBQzVCLE1BQU14SyxLQUFLLEdBQUc7TUFBRUMsQ0FBQyxFQUFFZ1IsR0FBRyxDQUFDRyxHQUFHLENBQUN6RCxJQUFJLENBQUMxTixDQUFDLEdBQUcsQ0FBQyxFQUFFME4sSUFBSSxDQUFDMU4sQ0FBQyxHQUFHME4sSUFBSSxDQUFDQyxDQUFDLEdBQUcsQ0FBQyxDQUFDO01BQUUxTixDQUFDLEVBQUUrUSxHQUFHLENBQUNHLEdBQUcsQ0FBQ3pELElBQUksQ0FBQ3pOLENBQUMsR0FBRyxDQUFDLEVBQUV5TixJQUFJLENBQUN6TixDQUFDLEdBQUd5TixJQUFJLENBQUNFLENBQUMsR0FBRyxDQUFDO0lBQUUsQ0FBQztJQUMxRyxNQUFNdEssT0FBTyxHQUFHdEMsT0FBTyxDQUFDSixLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQztJQUNoRCxJQUFJLENBQUFxRCxPQUFPLGFBQVBBLE9BQU8sdUJBQVBBLE9BQU8sQ0FBRTNELElBQUksTUFBSyxPQUFPLElBQUksQ0FBQ2tYLFFBQVEsQ0FBQzNULEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUNrQixPQUFPLENBQUNQLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLElBQUksQ0FBQ1csS0FBSyxDQUFDbUosS0FBSyxDQUFDaEMsSUFBSSxDQUFDOGEsSUFBSSxJQUFJQSxJQUFJLENBQUM3aUIsQ0FBQyxLQUFLRCxLQUFLLENBQUNDLENBQUMsSUFBSTZpQixJQUFJLENBQUM1aUIsQ0FBQyxLQUFLRixLQUFLLENBQUNFLENBQUMsQ0FBQyxJQUFJbVQsUUFBUSxDQUFDclQsS0FBSyxFQUFFYSxLQUFLLENBQUMrQyxLQUFLLENBQUMsR0FBRyxDQUFDLEVBQUUsT0FBTzVELEtBQUs7RUFDaFA7RUFDQSxPQUFPO0lBQUUsR0FBR2EsS0FBSyxDQUFDK0M7RUFBTSxDQUFDO0FBQzNCO0FBRUEsT0FBTyxNQUFNeW9CLFlBQVksR0FBR0EsQ0FBQ3pzQixJQUFZLEVBQUVJLEtBQVksRUFBRTRDLEVBQVUsRUFBRStGLFVBQThCLEtBQVk7RUFBQSxJQUFBOHBCLGtCQUFBLEVBQUFDLHFCQUFBLEVBQUFDLHFCQUFBLEVBQUFDLHFCQUFBLEVBQUFDLGdCQUFBO0VBQzdHLE1BQU12UixVQUFVLEdBQUdwa0IsV0FBVyxDQUFDMEMsSUFBSSxDQUFDO0VBQ3BDLElBQUksQ0FBQzBoQixVQUFVLEVBQUUsTUFBTSxJQUFJbFosS0FBSyxDQUFDLG9CQUFvQnhJLElBQUksRUFBRSxDQUFDO0VBQzVELE1BQU1zSCxnQkFBZ0IsR0FBR29hLFVBQVUsQ0FBQ2dLLEVBQUUsS0FBSyxVQUFVLEdBQUcsQ0FBQyxHQUFHLEVBQUFtSCxrQkFBQSxHQUFDOXBCLFVBQVUsYUFBVkEsVUFBVSx1QkFBVkEsVUFBVSxDQUFFN0IsTUFBTSxjQUFBMnJCLGtCQUFBLGNBQUFBLGtCQUFBLEdBQUksQ0FBQyxJQUFJLElBQUksSUFBQUMscUJBQUEsR0FBRy9wQixVQUFVLGFBQVZBLFVBQVUsdUJBQVZBLFVBQVUsQ0FBRXpCLGdCQUFnQixjQUFBd3JCLHFCQUFBLGNBQUFBLHFCQUFBLEdBQUksQ0FBQztFQUNoSSxNQUFNbkcsU0FBUyxHQUFHeGxCLElBQUksQ0FBQ0MsR0FBRyxDQUFDLENBQUMsRUFBRUQsSUFBSSxDQUFDME8sS0FBSyxDQUFDNkwsVUFBVSxDQUFDOWYsTUFBTSxHQUFHMEYsZ0JBQWdCLENBQUMsQ0FBQztFQUMvRSxPQUFPO0lBQUV0RSxFQUFFO0lBQUVrd0IsSUFBSSxFQUFFeFIsVUFBVSxDQUFDZ0ssRUFBRSxLQUFLLFVBQVUsR0FBRyxVQUFVLEdBQUcsU0FBUztJQUFFMXJCLElBQUksRUFBRTBoQixVQUFVLENBQUMxZSxFQUFFO0lBQUVtd0IsSUFBSSxFQUFFelIsVUFBVSxDQUFDeVIsSUFBSTtJQUFFOXlCLENBQUMsRUFBRUQsS0FBSyxDQUFDQyxDQUFDO0lBQUVDLENBQUMsRUFBRUYsS0FBSyxDQUFDRSxDQUFDO0lBQUVzQixNQUFNLEVBQUUrcUIsU0FBUztJQUFFQSxTQUFTO0lBQUVDLE1BQU0sRUFBRWxMLFVBQVUsQ0FBQ2tMLE1BQU0sS0FBQW1HLHFCQUFBLEdBQUlocUIsVUFBVSxhQUFWQSxVQUFVLHVCQUFWQSxVQUFVLENBQUV4QixXQUFXLGNBQUF3ckIscUJBQUEsY0FBQUEscUJBQUEsR0FBSSxDQUFDLENBQUM7SUFBRUssT0FBTyxFQUFFMVIsVUFBVSxDQUFDMFIsT0FBTyxLQUFBSixxQkFBQSxHQUFJanFCLFVBQVUsYUFBVkEsVUFBVSx1QkFBVkEsVUFBVSxDQUFFdkIsWUFBWSxjQUFBd3JCLHFCQUFBLGNBQUFBLHFCQUFBLEdBQUksQ0FBQyxDQUFDO0lBQUVLLEtBQUssRUFBRTNSLFVBQVUsQ0FBQzJSLEtBQUs7SUFBRUMsTUFBTSxFQUFFLENBQUM7SUFBRUMsS0FBSyxFQUFFN1IsVUFBVSxDQUFDNlIsS0FBSztJQUFFQyxLQUFLLEVBQUU5UixVQUFVLENBQUM4UixLQUFLO0lBQUVwakIsT0FBTyxFQUFFLElBQUk7SUFBRXNiLEVBQUUsRUFBRWhLLFVBQVUsQ0FBQ2dLLEVBQUU7SUFBRWtGLFVBQVUsRUFBRXJ6QixjQUFjLENBQUNta0IsVUFBVSxDQUFDO0lBQUVULElBQUksRUFBRSxDQUFDLEtBQUFnUyxnQkFBQSxHQUFJdlIsVUFBVSxDQUFDVCxJQUFJLGNBQUFnUyxnQkFBQSxjQUFBQSxnQkFBQSxHQUFJLEVBQUUsQ0FBQyxDQUFDO0lBQUVoRyxlQUFlLEVBQUV6dkIsa0JBQWtCLENBQUNra0IsVUFBVSxDQUFDO0lBQUUrUixVQUFVLEVBQUUsRUFBRTtJQUFFLElBQUkvUixVQUFVLENBQUNnSyxFQUFFLEtBQUssVUFBVSxHQUFHO01BQUVnSSxhQUFhLEVBQUUsU0FBa0I7TUFBRTdHLE1BQU0sRUFBRTlqQixVQUFVLGFBQVZBLFVBQVUsZUFBVkEsVUFBVSxDQUFFckIsZUFBZSxHQUFHLENBQUMsV0FBV3FCLFVBQVUsQ0FBQ3JCLGVBQWUsRUFBRSxDQUFDLEdBQUc7SUFBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDO0VBQUUsQ0FBQztBQUNwdEIsQ0FBQztBQUVELFNBQVM4aUIsUUFBUUEsQ0FBQzBJLElBQXlCLEVBQUVDLElBQVksRUFBRS95QixLQUFZLEVBQUVtekIsS0FBYSxFQUFFQyxLQUFhLEVBQVM7RUFDNUcsT0FBTztJQUFFeHdCLEVBQUUsRUFBRSxHQUFHa3dCLElBQUksSUFBSS95QixRQUFRLENBQUNDLEtBQUssQ0FBQyxFQUFFO0lBQUU4eUIsSUFBSTtJQUFFbHpCLElBQUksRUFBRWt6QixJQUFJO0lBQUVDLElBQUk7SUFBRTl5QixDQUFDLEVBQUVELEtBQUssQ0FBQ0MsQ0FBQztJQUFFQyxDQUFDLEVBQUVGLEtBQUssQ0FBQ0UsQ0FBQztJQUFFc0IsTUFBTSxFQUFFLEVBQUU7SUFBRStxQixTQUFTLEVBQUUsRUFBRTtJQUFFQyxNQUFNLEVBQUUsQ0FBQztJQUFFd0csT0FBTyxFQUFFLEVBQUU7SUFBRUMsS0FBSyxFQUFFLENBQUM7SUFBRUMsTUFBTSxFQUFFLENBQUM7SUFBRUMsS0FBSztJQUFFQyxLQUFLO0lBQUVwakIsT0FBTyxFQUFFLEtBQUs7SUFBRXFqQixVQUFVLEVBQUU7RUFBRyxDQUFDO0FBQ25OO0FBRUEsTUFBTWhnQixRQUFRLEdBQUdBLENBQUNrZ0IsQ0FBUSxFQUFFQyxDQUFRLEtBQWF6c0IsSUFBSSxDQUFDa0osR0FBRyxDQUFDc2pCLENBQUMsQ0FBQ3R6QixDQUFDLEdBQUd1ekIsQ0FBQyxDQUFDdnpCLENBQUMsQ0FBQyxHQUFHOEcsSUFBSSxDQUFDa0osR0FBRyxDQUFDc2pCLENBQUMsQ0FBQ3J6QixDQUFDLEdBQUdzekIsQ0FBQyxDQUFDdHpCLENBQUMsQ0FBQztBQUkxRixNQUFNMkgsZ0JBQWdCLEdBQUloSCxLQUFZLElBQWtCc0IscUJBQXFCLENBQUN0QixLQUFLLENBQUM7QUFFcEYsTUFBTStHLGdCQUFnQixHQUFJL0csS0FBWSxJQUFjO0VBQ2xELElBQUlBLEtBQUssQ0FBQ2dKLFNBQVMsQ0FBQ2pLLElBQUksS0FBSyxpQkFBaUIsRUFBRSxPQUFPaUIsS0FBSyxDQUFDSyxLQUFLLENBQUMrVixPQUFPLENBQUMsQ0FBQ3RYLElBQUksRUFBRXVpQixDQUFDLEtBQUt2aUIsSUFBSSxDQUFDQyxJQUFJLEtBQUssT0FBTyxJQUFJRCxJQUFJLENBQUNDLElBQUksS0FBSyxPQUFPLEdBQUcsQ0FBQ2tCLE9BQU8sQ0FBQ0QsS0FBSyxFQUFFcWhCLENBQUMsQ0FBQyxDQUFDLEdBQUcsRUFBRSxDQUFDO0VBQ2xLLElBQUlyaEIsS0FBSyxDQUFDZ0osU0FBUyxDQUFDakssSUFBSSxLQUFLLGFBQWEsRUFBRSxPQUFPaUIsS0FBSyxDQUFDSyxLQUFLLENBQUMrVixPQUFPLENBQUMsQ0FBQ3RYLElBQUksRUFBRXVpQixDQUFDLEtBQUt2aUIsSUFBSSxDQUFDQyxJQUFJLEtBQUssUUFBUSxHQUFHLENBQUNrQixPQUFPLENBQUNELEtBQUssRUFBRXFoQixDQUFDLENBQUMsQ0FBQyxHQUFHLEVBQUUsQ0FBQztFQUN0SSxJQUFJcmhCLEtBQUssQ0FBQ2dKLFNBQVMsQ0FBQ2pLLElBQUksS0FBSyxhQUFhLEVBQUUsT0FBT2lCLEtBQUssQ0FBQ0ssS0FBSyxDQUFDK1YsT0FBTyxDQUFDLENBQUN0WCxJQUFJLEVBQUV1aUIsQ0FBQyxLQUFLdmlCLElBQUksQ0FBQ0MsSUFBSSxLQUFLLE9BQU8sR0FBRyxDQUFDa0IsT0FBTyxDQUFDRCxLQUFLLEVBQUVxaEIsQ0FBQyxDQUFDLENBQUMsR0FBRyxFQUFFLENBQUM7RUFDckksT0FBT3JoQixLQUFLLENBQUNRLE1BQU0sQ0FBQ29GLE1BQU0sQ0FBQ2xGLEtBQUssSUFBSUEsS0FBSyxDQUFDdXhCLElBQUksS0FBSyxVQUFVLENBQUMsQ0FBQ3RzQixHQUFHLENBQUNqRixLQUFLLEtBQUs7SUFBRXRCLENBQUMsRUFBRXNCLEtBQUssQ0FBQ3RCLENBQUM7SUFBRUMsQ0FBQyxFQUFFcUIsS0FBSyxDQUFDckI7RUFBRSxDQUFDLENBQUMsQ0FBQztBQUMzRyxDQUFDO0FBRUQsTUFBTXVsQiwwQkFBMEIsR0FBR0EsQ0FBQzVrQixLQUFZLEVBQUVhLE1BQWEsS0FBYyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDc0csSUFBSSxDQUFDLENBQUMsQ0FBQy9ILENBQUMsRUFBRUMsQ0FBQyxDQUFDLEtBQUtzRixlQUFlLENBQUMzRSxLQUFLLEVBQUVBLEtBQUssQ0FBQytDLEtBQUssRUFBRTtFQUFFM0QsQ0FBQyxFQUFFeUIsTUFBTSxDQUFDekIsQ0FBQyxHQUFHQSxDQUFDO0VBQUVDLENBQUMsRUFBRXdCLE1BQU0sQ0FBQ3hCLENBQUMsR0FBR0E7QUFBRSxDQUFDLENBQUMsQ0FBQztBQUNuTixNQUFNdXpCLGlCQUFpQixHQUFHQSxDQUFDNXlCLEtBQVksRUFBRXFCLFNBQThCLEVBQUVsQyxLQUFZLEtBQWNnQixRQUFRLENBQUNILEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLElBQUlnQyxTQUFTLENBQUNpQixHQUFHLENBQUN2QyxPQUFPLENBQUNDLEtBQUssRUFBRWIsS0FBSyxDQUFDQyxDQUFDLEVBQUVELEtBQUssQ0FBQ0UsQ0FBQyxDQUFDLENBQUM7QUFDdkwsTUFBTXd6QixnQkFBZ0IsR0FBR0EsQ0FBQzd5QixLQUFZLEVBQUVxQixTQUE4QixFQUFFeUYsT0FBeUIsS0FBY0EsT0FBTyxDQUFDSyxJQUFJLENBQUN0RyxNQUFNLElBQUksQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQ3NHLElBQUksQ0FBQyxDQUFDLENBQUMvSCxDQUFDLEVBQUVDLENBQUMsQ0FBQyxLQUFLdXpCLGlCQUFpQixDQUFDNXlCLEtBQUssRUFBRXFCLFNBQVMsRUFBRTtFQUFFakMsQ0FBQyxFQUFFeUIsTUFBTSxDQUFDekIsQ0FBQyxHQUFHQSxDQUFDO0VBQUVDLENBQUMsRUFBRXdCLE1BQU0sQ0FBQ3hCLENBQUMsR0FBR0E7QUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDO0FBQzdRLE9BQU8sTUFBTXl6QiwyQkFBMkIsR0FBSTl5QixLQUFZLElBQWM7RUFDcEUsTUFBTXFCLFNBQVMsR0FBRzJGLGdCQUFnQixDQUFDaEgsS0FBSyxDQUFDO0VBQ3pDLE9BQU80eUIsaUJBQWlCLENBQUM1eUIsS0FBSyxFQUFFcUIsU0FBUyxFQUFFckIsS0FBSyxDQUFDd0YsSUFBSSxDQUFDLElBQUlxdEIsZ0JBQWdCLENBQUM3eUIsS0FBSyxFQUFFcUIsU0FBUyxFQUFFMEYsZ0JBQWdCLENBQUMvRyxLQUFLLENBQUMsQ0FBQztBQUN2SCxDQUFDO0FBRUQsT0FBTyxNQUFNK3lCLG9CQUFvQixHQUFJL3lCLEtBQVksSUFBZTtFQUFBLElBQUFnekIsa0JBQUEsRUFBQUMsbUJBQUEsRUFBQUMsaUJBQUE7RUFDOUQsTUFBTTFxQixNQUFnQixHQUFHLEVBQUU7RUFDM0IsTUFBTW1CLEtBQUssSUFBQXFwQixrQkFBQSxHQUFHaHpCLEtBQUssQ0FBQ216QixXQUFXLGNBQUFILGtCQUFBLGNBQUFBLGtCQUFBLEdBQUksRUFBRTtFQUNyQyxNQUFNSSxNQUFNLElBQUFILG1CQUFBLEdBQUdqekIsS0FBSyxDQUFDcXpCLFlBQVksY0FBQUosbUJBQUEsY0FBQUEsbUJBQUEsR0FBSSxFQUFFO0VBQ3ZDLE1BQU1uYSxNQUFNLElBQUFvYSxpQkFBQSxHQUFHbHpCLEtBQUssQ0FBQ3FYLFVBQVUsY0FBQTZiLGlCQUFBLGNBQUFBLGlCQUFBLEdBQUksRUFBRTtFQUNyQyxNQUFNSSxPQUFPLEdBQUcsSUFBSXB5QixHQUFHLENBQVMsQ0FBQztFQUNqQyxLQUFLLE1BQU00TCxJQUFJLElBQUluRCxLQUFLLEVBQUU7SUFDeEIsSUFBSTJwQixPQUFPLENBQUNoeEIsR0FBRyxDQUFDd0ssSUFBSSxDQUFDL0ssRUFBRSxDQUFDLEVBQUV5RyxNQUFNLENBQUN4RSxJQUFJLENBQUMsMEJBQTBCOEksSUFBSSxDQUFDL0ssRUFBRSxFQUFFLENBQUM7SUFDMUV1eEIsT0FBTyxDQUFDOXVCLEdBQUcsQ0FBQ3NJLElBQUksQ0FBQy9LLEVBQUUsQ0FBQztJQUNwQixNQUFNaWtCLE1BQU0sR0FBR2xOLE1BQU0sQ0FBQ3JZLElBQUksQ0FBQzh5QixLQUFLLElBQUlBLEtBQUssQ0FBQ3h4QixFQUFFLEtBQUsrSyxJQUFJLENBQUMwbUIsUUFBUSxDQUFDO0lBQy9ELElBQUksQ0FBQ3hOLE1BQU0sSUFBSWxaLElBQUksQ0FBQy9LLEVBQUUsS0FBSyxlQUFlaWtCLE1BQU0sQ0FBQ2prQixFQUFFLEVBQUUsSUFBSSxDQUFDK0ssSUFBSSxDQUFDMm1CLFlBQVksSUFBSTNtQixJQUFJLENBQUM0bUIsT0FBTyxLQUFLLENBQUMsSUFBSSxDQUFDNW1CLElBQUksQ0FBQzZtQixhQUFhLElBQUksQ0FBQzdtQixJQUFJLENBQUNrUixPQUFPLENBQUNuYSxNQUFNLElBQUksQ0FBQ2lKLElBQUksQ0FBQ2dLLE9BQU8sQ0FBQ2pULE1BQU0sRUFBRTJFLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx3QkFBd0I4SSxJQUFJLENBQUMvSyxFQUFFLEVBQUUsQ0FBQztJQUN4TixJQUFJLENBQUMrSyxJQUFJLENBQUNpSCxRQUFRLEVBQUUsR0FBR2pILElBQUksQ0FBQ2tSLE9BQU8sRUFBRSxHQUFHbFIsSUFBSSxDQUFDZ0ssT0FBTyxDQUFDLENBQUMzUCxJQUFJLENBQUNoSSxLQUFLLElBQUksQ0FBQ2dCLFFBQVEsQ0FBQ0gsS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUMsQ0FBQyxFQUFFbUosTUFBTSxDQUFDeEUsSUFBSSxDQUFDLDhCQUE4QjhJLElBQUksQ0FBQy9LLEVBQUUsRUFBRSxDQUFDO0lBQzdKLElBQUksQ0FBQy9CLEtBQUssQ0FBQ21KLEtBQUssQ0FBQ2hDLElBQUksQ0FBQzhhLElBQUksSUFBSUEsSUFBSSxDQUFDMlIsUUFBUSxLQUFLOW1CLElBQUksQ0FBQy9LLEVBQUUsQ0FBQyxFQUFFeUcsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLDJCQUEyQjhJLElBQUksQ0FBQy9LLEVBQUUsRUFBRSxDQUFDO0VBQzdHO0VBQ0EsS0FBSyxNQUFNd3hCLEtBQUssSUFBSXphLE1BQU0sRUFBRSxJQUFJLENBQUNuUCxLQUFLLENBQUN4QyxJQUFJLENBQUMyRixJQUFJLElBQUlBLElBQUksQ0FBQzBtQixRQUFRLEtBQUtELEtBQUssQ0FBQ3h4QixFQUFFLENBQUMsRUFBRXlHLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyw0QkFBNEJ1dkIsS0FBSyxDQUFDeHhCLEVBQUUsRUFBRSxDQUFDO0VBQ3BJLE1BQU04eEIsUUFBUSxHQUFHLElBQUkzeUIsR0FBRyxDQUFTLENBQUM7RUFDbEMsS0FBSyxNQUFNc1YsS0FBSyxJQUFJNGMsTUFBTSxFQUFFO0lBQUEsSUFBQVUsa0JBQUE7SUFDMUIsSUFBSUQsUUFBUSxDQUFDdnhCLEdBQUcsQ0FBQ2tVLEtBQUssQ0FBQ3pVLEVBQUUsQ0FBQyxFQUFFeUcsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLDJCQUEyQndTLEtBQUssQ0FBQ3pVLEVBQUUsRUFBRSxDQUFDO0lBQzlFOHhCLFFBQVEsQ0FBQ3J2QixHQUFHLENBQUNnUyxLQUFLLENBQUN6VSxFQUFFLENBQUM7SUFDdEIsTUFBTStLLElBQUksR0FBR25ELEtBQUssQ0FBQ2xKLElBQUksQ0FBQ2tCLFNBQVMsSUFBSUEsU0FBUyxDQUFDSSxFQUFFLEtBQUt5VSxLQUFLLENBQUN1ZCxNQUFNLENBQUM7SUFDbkUsSUFBSSxDQUFDam5CLElBQUksSUFBSTBKLEtBQUssQ0FBQ2tkLE9BQU8sS0FBSyxDQUFDLElBQUksQ0FBQ2xkLEtBQUssQ0FBQ2lkLFlBQVksSUFBSSxDQUFDamQsS0FBSyxDQUFDbWQsYUFBYSxJQUFJLENBQUN4ekIsUUFBUSxDQUFDSCxLQUFLLEVBQUV3VyxLQUFLLENBQUN0TixJQUFJLENBQUM5SixDQUFDLEVBQUVvWCxLQUFLLENBQUN0TixJQUFJLENBQUM3SixDQUFDLENBQUMsSUFBSSxDQUFDYyxRQUFRLENBQUNILEtBQUssRUFBRXdXLEtBQUssQ0FBQ0ssS0FBSyxDQUFDelgsQ0FBQyxFQUFFb1gsS0FBSyxDQUFDSyxLQUFLLENBQUN4WCxDQUFDLENBQUMsRUFBRW1KLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx5QkFBeUJ3UyxLQUFLLENBQUN6VSxFQUFFLEVBQUUsQ0FBQztJQUNuTyxJQUFJK0ssSUFBSSxLQUFLMEosS0FBSyxDQUFDdE4sSUFBSSxDQUFDOUosQ0FBQyxLQUFLME4sSUFBSSxDQUFDaUgsUUFBUSxDQUFDM1UsQ0FBQyxJQUFJb1gsS0FBSyxDQUFDdE4sSUFBSSxDQUFDN0osQ0FBQyxLQUFLeU4sSUFBSSxDQUFDaUgsUUFBUSxDQUFDMVUsQ0FBQyxJQUFJbVgsS0FBSyxDQUFDd2QsY0FBYyxLQUFLbG5CLElBQUksQ0FBQ2tuQixjQUFjLElBQUl4ZCxLQUFLLENBQUNtZCxhQUFhLEtBQUs3bUIsSUFBSSxDQUFDNm1CLGFBQWEsSUFBSW5kLEtBQUssQ0FBQ3lkLFlBQVksS0FBS25uQixJQUFJLENBQUNtbkIsWUFBWSxJQUFJemQsS0FBSyxDQUFDMGQsSUFBSSxLQUFLcG5CLElBQUksQ0FBQ29uQixJQUFJLENBQUMsRUFBRTFyQixNQUFNLENBQUN4RSxJQUFJLENBQUMsNEJBQTRCd1MsS0FBSyxDQUFDelUsRUFBRSxFQUFFLENBQUM7SUFDN1MsSUFBSXlVLEtBQUssQ0FBQ3pYLElBQUksS0FBSyxpQkFBaUIsS0FBSyxDQUFDeVgsS0FBSyxDQUFDOVIsV0FBVyxJQUFJOFIsS0FBSyxDQUFDOVIsV0FBVyxDQUFDOEMsS0FBSyxLQUFLeEgsS0FBSyxDQUFDd0gsS0FBSyxJQUFJZ1AsS0FBSyxDQUFDOVIsV0FBVyxDQUFDMUUsS0FBSyxHQUFHLENBQUMsSUFBSXdXLEtBQUssQ0FBQzlSLFdBQVcsQ0FBQzFFLEtBQUssR0FBRyxDQUFDLElBQUl3VyxLQUFLLENBQUNJLFNBQVMsS0FBSyxTQUFTLElBQUlKLEtBQUssQ0FBQ0ksU0FBUyxLQUFLLFNBQVMsSUFBSUosS0FBSyxDQUFDMmQsT0FBTyxLQUFLLGFBQWEsS0FBSzNkLEtBQUssQ0FBQ0ksU0FBUyxLQUFLLFNBQVMsR0FBR0osS0FBSyxDQUFDNGQsZUFBZSxLQUFLLFdBQVcsR0FBRzVkLEtBQUssQ0FBQzRkLGVBQWUsS0FBSyxhQUFhLENBQUMsQ0FBQyxFQUFFNXJCLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyw4QkFBOEJ3UyxLQUFLLENBQUN6VSxFQUFFLEVBQUUsQ0FBQztJQUN0YixJQUFJeVUsS0FBSyxDQUFDelgsSUFBSSxLQUFLLG1CQUFtQixLQUFLLENBQUMrTixJQUFJLElBQUksQ0FBQ0EsSUFBSSxDQUFDa1IsT0FBTyxDQUFDN1csSUFBSSxDQUFDaEksS0FBSyxJQUFJQSxLQUFLLENBQUNDLENBQUMsS0FBS29YLEtBQUssQ0FBQ0ssS0FBSyxDQUFDelgsQ0FBQyxJQUFJRCxLQUFLLENBQUNFLENBQUMsS0FBS21YLEtBQUssQ0FBQ0ssS0FBSyxDQUFDeFgsQ0FBQyxDQUFDLENBQUMsRUFBRW1KLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQywyQkFBMkJ3UyxLQUFLLENBQUN6VSxFQUFFLEVBQUUsQ0FBQztJQUM1TCxJQUFJeVUsS0FBSyxDQUFDelgsSUFBSSxLQUFLLGlCQUFpQixLQUFLLENBQUMrTixJQUFJLElBQUksR0FBQWduQixrQkFBQSxHQUFDOXpCLEtBQUssQ0FBQ3FYLFVBQVUsY0FBQXljLGtCQUFBLGVBQWhCQSxrQkFBQSxDQUFrQjNzQixJQUFJLENBQUNvc0IsS0FBSztNQUFBLElBQUFjLHFCQUFBLEVBQUFDLGtCQUFBLEVBQUFDLHNCQUFBLEVBQUFDLG1CQUFBO01BQUEsT0FBSWpCLEtBQUssQ0FBQ3gwQixJQUFJLEtBQUssa0JBQWtCLElBQUl3MEIsS0FBSyxDQUFDeHhCLEVBQUUsS0FBSytLLElBQUksQ0FBQzBtQixRQUFRLElBQUksRUFBQWEscUJBQUEsR0FBQWQsS0FBSyxDQUFDMWIsY0FBYyxjQUFBd2MscUJBQUEsdUJBQXBCQSxxQkFBQSxDQUFzQjFjLFdBQVcsUUFBQTJjLGtCQUFBLEdBQUs5ZCxLQUFLLENBQUM5UixXQUFXLGNBQUE0dkIsa0JBQUEsdUJBQWpCQSxrQkFBQSxDQUFtQjlzQixLQUFLLEtBQUksRUFBQStzQixzQkFBQSxHQUFBaEIsS0FBSyxDQUFDMWIsY0FBYyxjQUFBMGMsc0JBQUEsdUJBQXBCQSxzQkFBQSxDQUFzQjNjLFdBQVcsUUFBQTRjLG1CQUFBLEdBQUtoZSxLQUFLLENBQUM5UixXQUFXLGNBQUE4dkIsbUJBQUEsdUJBQWpCQSxtQkFBQSxDQUFtQngwQixLQUFLO0lBQUEsRUFBQyxFQUFDLEVBQUV3SSxNQUFNLENBQUN4RSxJQUFJLENBQUMsZ0NBQWdDd1MsS0FBSyxDQUFDelUsRUFBRSxFQUFFLENBQUM7RUFDblY7RUFDQSxLQUFLLE1BQU0rSyxJQUFJLElBQUluRCxLQUFLLEVBQUUsS0FBSyxNQUFNLENBQUN6SixLQUFLLEVBQUUyVyxLQUFLLENBQUMsSUFBSS9KLElBQUksQ0FBQ2tSLE9BQU8sQ0FBQ0EsT0FBTyxDQUFDLENBQUMsRUFBRSxJQUFJLENBQUNvVixNQUFNLENBQUNqc0IsSUFBSSxDQUFDcVAsS0FBSyxJQUFJQSxLQUFLLENBQUN6VSxFQUFFLEtBQUssZ0JBQWdCK0ssSUFBSSxDQUFDL0ssRUFBRSxXQUFXN0IsS0FBSyxFQUFFLElBQUlzVyxLQUFLLENBQUN6WCxJQUFJLEtBQUssbUJBQW1CLElBQUl5WCxLQUFLLENBQUNLLEtBQUssQ0FBQ3pYLENBQUMsS0FBS3lYLEtBQUssQ0FBQ3pYLENBQUMsSUFBSW9YLEtBQUssQ0FBQ0ssS0FBSyxDQUFDeFgsQ0FBQyxLQUFLd1gsS0FBSyxDQUFDeFgsQ0FBQyxDQUFDLEVBQUVtSixNQUFNLENBQUN4RSxJQUFJLENBQUMseUJBQXlCOEksSUFBSSxDQUFDL0ssRUFBRSxJQUFJN0IsS0FBSyxFQUFFLENBQUM7RUFDMVQsS0FBSyxNQUFNcXpCLEtBQUssSUFBSXphLE1BQU0sRUFBRSxJQUFJeWEsS0FBSyxDQUFDeDBCLElBQUksS0FBSyxrQkFBa0IsSUFBSXcwQixLQUFLLENBQUMxYixjQUFjLElBQUksQ0FBQ3ViLE1BQU0sQ0FBQ2pzQixJQUFJLENBQUNxUCxLQUFLO0lBQUEsSUFBQWllLG1CQUFBLEVBQUFDLHNCQUFBLEVBQUFDLG1CQUFBLEVBQUFDLHNCQUFBO0lBQUEsT0FBSXBlLEtBQUssQ0FBQ3pVLEVBQUUsS0FBSyw0QkFBNEJ3eEIsS0FBSyxDQUFDeHhCLEVBQUUsYUFBYSxJQUFJeVUsS0FBSyxDQUFDelgsSUFBSSxLQUFLLGlCQUFpQixJQUFJLEVBQUEwMUIsbUJBQUEsR0FBQWplLEtBQUssQ0FBQzlSLFdBQVcsY0FBQSt2QixtQkFBQSx1QkFBakJBLG1CQUFBLENBQW1CanRCLEtBQUssUUFBQWt0QixzQkFBQSxHQUFLbkIsS0FBSyxDQUFDMWIsY0FBYyxjQUFBNmMsc0JBQUEsdUJBQXBCQSxzQkFBQSxDQUFzQi9jLFdBQVcsS0FBSSxFQUFBZ2QsbUJBQUEsR0FBQW5lLEtBQUssQ0FBQzlSLFdBQVcsY0FBQWl3QixtQkFBQSx1QkFBakJBLG1CQUFBLENBQW1CMzBCLEtBQUssUUFBQTQwQixzQkFBQSxHQUFLckIsS0FBSyxDQUFDMWIsY0FBYyxjQUFBK2Msc0JBQUEsdUJBQXBCQSxzQkFBQSxDQUFzQmhkLFdBQVc7RUFBQSxFQUFDLEVBQUVwUCxNQUFNLENBQUN4RSxJQUFJLENBQUMsOEJBQThCdXZCLEtBQUssQ0FBQ3h4QixFQUFFLEVBQUUsQ0FBQztFQUNqWixNQUFNOHlCLFdBQVcsR0FBR2xyQixLQUFLLENBQUN5TSxPQUFPLENBQUN0SixJQUFJLElBQUksQ0FBQyxHQUFHQSxJQUFJLENBQUNrUixPQUFPLEVBQUUsR0FBR2xSLElBQUksQ0FBQ2dLLE9BQU8sQ0FBQyxDQUFDO0VBQzdFLElBQUksQ0FBQytkLFdBQVcsQ0FBQ2h4QixNQUFNLEVBQUUsT0FBTzJFLE1BQU07RUFDdEMsTUFBTXNzQixNQUFNLEdBQUcsSUFBSXp4QixHQUFHLENBQWtELENBQUM7RUFDekUsS0FBSyxNQUFNbEUsS0FBSyxJQUFJMDFCLFdBQVcsRUFBRTtJQUMvQixNQUFNbnlCLE9BQU8sR0FBR3RDLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsRUFBRUQsS0FBSyxDQUFDRSxDQUFDLENBQUM7SUFDaEQsSUFBSSxDQUFDcUQsT0FBTyxFQUFFO0lBQ2QsTUFBTXhDLEtBQUssR0FBR0gsT0FBTyxDQUFDQyxLQUFLLEVBQUViLEtBQUssQ0FBQ0MsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsQ0FBQztJQUM5QyxJQUFJLENBQUN5MUIsTUFBTSxDQUFDeHlCLEdBQUcsQ0FBQ3BDLEtBQUssQ0FBQyxFQUFFNDBCLE1BQU0sQ0FBQ3Z4QixHQUFHLENBQUNyRCxLQUFLLEVBQUU7TUFBRW5CLElBQUksRUFBRTJELE9BQU8sQ0FBQzNELElBQUk7TUFBRSxJQUFJMkQsT0FBTyxDQUFDMlYsSUFBSSxHQUFHO1FBQUVBLElBQUksRUFBRTtVQUFFLEdBQUczVixPQUFPLENBQUMyVjtRQUFLO01BQUUsQ0FBQyxHQUFHLENBQUMsQ0FBQztJQUFFLENBQUMsQ0FBQztJQUN6SDNWLE9BQU8sQ0FBQzNELElBQUksR0FBRyxNQUFNO0lBQ3JCLE9BQU8yRCxPQUFPLENBQUMyVixJQUFJO0VBQ3JCO0VBQ0EsSUFBSSxDQUFDeWEsMkJBQTJCLENBQUM5eUIsS0FBSyxDQUFDLEVBQUV3SSxNQUFNLENBQUN4RSxJQUFJLENBQUMsMENBQTBDLENBQUM7RUFDaEcsS0FBSyxNQUFNLENBQUM5RCxLQUFLLEVBQUU2MEIsUUFBUSxDQUFDLElBQUlELE1BQU0sRUFBRTtJQUN0QyxNQUFNcHlCLE9BQU8sR0FBRzFDLEtBQUssQ0FBQ0ssS0FBSyxDQUFDSCxLQUFLLENBQUU7SUFDbkN3QyxPQUFPLENBQUMzRCxJQUFJLEdBQUdnMkIsUUFBUSxDQUFDaDJCLElBQUk7SUFDNUIsSUFBSWcyQixRQUFRLENBQUMxYyxJQUFJLEVBQUUzVixPQUFPLENBQUMyVixJQUFJLEdBQUcwYyxRQUFRLENBQUMxYyxJQUFJLE1BQzFDLE9BQU8zVixPQUFPLENBQUMyVixJQUFJO0VBQzFCO0VBQ0EsT0FBTzdQLE1BQU07QUFDZixDQUFDO0FBRUQsT0FBTyxNQUFNd3NCLHdCQUF3QixHQUFJaDFCLEtBQVk7RUFBQSxJQUFBaTFCLG9CQUFBO0VBQUEsT0FBZSxFQUFBQSxvQkFBQSxHQUFDajFCLEtBQUssQ0FBQ3F6QixZQUFZLGNBQUE0QixvQkFBQSxjQUFBQSxvQkFBQSxHQUFJLEVBQUUsRUFBRTdlLE9BQU8sQ0FBQ0ksS0FBSyxJQUFJO0lBQzlHLElBQUlBLEtBQUssQ0FBQ3pYLElBQUksS0FBSyxpQkFBaUIsSUFBSSxDQUFDeVgsS0FBSyxDQUFDOVIsV0FBVyxJQUFJOFIsS0FBSyxDQUFDOVIsV0FBVyxDQUFDOEMsS0FBSyxLQUFLeEgsS0FBSyxDQUFDd0gsS0FBSyxJQUFJZ1AsS0FBSyxDQUFDOVIsV0FBVyxDQUFDMUUsS0FBSyxHQUFHLENBQUMsSUFBSXdXLEtBQUssQ0FBQzlSLFdBQVcsQ0FBQzFFLEtBQUssR0FBRyxDQUFDLEVBQUUsT0FBTyxFQUFFO0lBQzlLLElBQUk7TUFDRixNQUFNazFCLE9BQU8sR0FBR3hvQixpQkFBaUIsQ0FBQzFNLEtBQUssQ0FBQytILElBQUksRUFBRXlPLEtBQUssQ0FBQzlSLFdBQVcsQ0FBQzhDLEtBQUssRUFBRWdQLEtBQUssQ0FBQzlSLFdBQVcsQ0FBQzFFLEtBQUssQ0FBQztNQUMvRixPQUFPWSxVQUFVLENBQUNzMEIsT0FBTyxFQUFFQSxPQUFPLENBQUNueUIsS0FBSyxDQUFDM0QsQ0FBQyxFQUFFODFCLE9BQU8sQ0FBQ255QixLQUFLLENBQUMxRCxDQUFDLENBQUMsSUFBSXl6QiwyQkFBMkIsQ0FBQ29DLE9BQU8sQ0FBQyxHQUFHLEVBQUUsR0FBRyxDQUFDLDRCQUE0QjFlLEtBQUssQ0FBQ3pVLEVBQUUsRUFBRSxDQUFDO0lBQ3RKLENBQUMsQ0FBQyxNQUFNO01BQUUsT0FBTyxDQUFDLDRCQUE0QnlVLEtBQUssQ0FBQ3pVLEVBQUUsRUFBRSxDQUFDO0lBQUM7RUFDNUQsQ0FBQyxDQUFDO0FBQUE7QUFFRixPQUFPLE1BQU15SyxrQkFBa0IsR0FBSXhNLEtBQVksSUFBMkI7RUFBQSxJQUFBbTFCLGlCQUFBLEVBQUFDLFVBQUEsRUFBQUMsb0JBQUEsRUFBQUMscUJBQUEsRUFBQUMsaUJBQUE7RUFDeEUsTUFBTS9zQixNQUFnQixHQUFHLEVBQUU7RUFDM0JBLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyxHQUFHM0csdUJBQXVCLENBQUMsQ0FBQyxFQUFFLEdBQUdELG9CQUFvQixDQUFDNEMsS0FBSyxDQUFDLEVBQUUsR0FBR1Isb0JBQW9CLENBQUM7RUFDbEcsSUFBSXJDLGtCQUFrQixDQUFDNkMsS0FBSyxDQUFDd0gsS0FBSyxDQUFDLENBQUMzRCxNQUFNLElBQUksR0FBQXN4QixpQkFBQSxHQUFFbjFCLEtBQUssQ0FBQ3NuQixTQUFTLGNBQUE2TixpQkFBQSxlQUFmQSxpQkFBQSxDQUFpQnR4QixNQUFNLENBQUMsRUFBRTJFLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx5QkFBeUIsQ0FBQztFQUNoSCxJQUFJaEUsS0FBSyxDQUFDSyxLQUFLLENBQUN3RCxNQUFNLEtBQUs3RCxLQUFLLENBQUN5SSxLQUFLLEdBQUd6SSxLQUFLLENBQUMwSSxNQUFNLElBQUkxSSxLQUFLLENBQUN5SSxLQUFLLEdBQUc3TCxTQUFTLElBQUlvRCxLQUFLLENBQUMwSSxNQUFNLEdBQUcvTCxVQUFVLElBQUlxRCxLQUFLLENBQUNFLEtBQUssR0FBRyxDQUFDLElBQUlGLEtBQUssQ0FBQ0UsS0FBSyxJQUFJeEQsV0FBVyxFQUFFOEwsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLDBCQUEwQixDQUFDO0VBQ3ZNLElBQUksQ0FBQ2hFLEtBQUssQ0FBQ3lILFFBQVEsSUFBSSxDQUFDcUcsY0FBYyxDQUFDOU4sS0FBSyxDQUFDd0gsS0FBSyxDQUFDLENBQUNMLElBQUksQ0FBQ3F1QixNQUFNLElBQUl4MUIsS0FBSyxDQUFDeUgsUUFBUSxLQUFLK3RCLE1BQU0sSUFBSXgxQixLQUFLLENBQUN5SCxRQUFRLEtBQUssR0FBRyt0QixNQUFNLFFBQVEsQ0FBQyxFQUFFaHRCLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyxtQkFBbUIsQ0FBQztFQUN2SyxJQUFJLENBQUM1RCxPQUFPLENBQUNKLEtBQUssRUFBRUEsS0FBSyxDQUFDK0MsS0FBSyxDQUFDM0QsQ0FBQyxFQUFFWSxLQUFLLENBQUMrQyxLQUFLLENBQUMxRCxDQUFDLENBQUMsSUFBSSxDQUFDQyxRQUFRLENBQUNjLE9BQU8sQ0FBQ0osS0FBSyxFQUFFQSxLQUFLLENBQUMrQyxLQUFLLENBQUMzRCxDQUFDLEVBQUVZLEtBQUssQ0FBQytDLEtBQUssQ0FBQzFELENBQUMsQ0FBQyxDQUFFTixJQUFJLENBQUMsRUFBRXlKLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx5QkFBeUIsQ0FBQztFQUMxSixJQUFJLEVBQUFveEIsVUFBQSxHQUFBaDFCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFQSxLQUFLLENBQUN3RixJQUFJLENBQUNwRyxDQUFDLEVBQUVZLEtBQUssQ0FBQ3dGLElBQUksQ0FBQ25HLENBQUMsQ0FBQyxjQUFBKzFCLFVBQUEsdUJBQTFDQSxVQUFBLENBQTRDcjJCLElBQUksTUFBSyxNQUFNLEVBQUV5SixNQUFNLENBQUN4RSxJQUFJLENBQUMsd0JBQXdCLENBQUM7RUFDdEcsS0FBSyxJQUFJOUQsS0FBSyxHQUFHLENBQUMsRUFBRUEsS0FBSyxHQUFHRixLQUFLLENBQUNLLEtBQUssQ0FBQ3dELE1BQU0sRUFBRTNELEtBQUssRUFBRSxFQUFFO0lBQ3ZELE1BQU1wQixJQUFJLEdBQUdrQixLQUFLLENBQUNLLEtBQUssQ0FBQ0gsS0FBSyxDQUFDO0lBQy9CLElBQUksQ0FBQ3BCLElBQUksQ0FBQ3VaLElBQUksRUFBRTtJQUNoQixNQUFNbFosS0FBSyxHQUFHYyxPQUFPLENBQUNELEtBQUssRUFBRUUsS0FBSyxDQUFDO0lBQ25DLE1BQU11b0IsS0FBSyxHQUFJO01BQUVDLENBQUMsRUFBRTtRQUFFdHBCLENBQUMsRUFBRSxDQUFDO1FBQUVDLENBQUMsRUFBRSxDQUFDO01BQUUsQ0FBQztNQUFFc3BCLEVBQUUsRUFBRTtRQUFFdnBCLENBQUMsRUFBRSxDQUFDO1FBQUVDLENBQUMsRUFBRSxDQUFDO01BQUUsQ0FBQztNQUFFdXBCLENBQUMsRUFBRTtRQUFFeHBCLENBQUMsRUFBRSxDQUFDO1FBQUVDLENBQUMsRUFBRTtNQUFFLENBQUM7TUFBRXdwQixFQUFFLEVBQUU7UUFBRXpwQixDQUFDLEVBQUUsQ0FBQztRQUFFQyxDQUFDLEVBQUU7TUFBRSxDQUFDO01BQUV5cEIsQ0FBQyxFQUFFO1FBQUUxcEIsQ0FBQyxFQUFFLENBQUM7UUFBRUMsQ0FBQyxFQUFFO01BQUUsQ0FBQztNQUFFMHBCLEVBQUUsRUFBRTtRQUFFM3BCLENBQUMsRUFBRSxDQUFDLENBQUM7UUFBRUMsQ0FBQyxFQUFFO01BQUUsQ0FBQztNQUFFME4sQ0FBQyxFQUFFO1FBQUUzTixDQUFDLEVBQUUsQ0FBQyxDQUFDO1FBQUVDLENBQUMsRUFBRTtNQUFFLENBQUM7TUFBRTJwQixFQUFFLEVBQUU7UUFBRTVwQixDQUFDLEVBQUUsQ0FBQyxDQUFDO1FBQUVDLENBQUMsRUFBRSxDQUFDO01BQUU7SUFBRSxDQUFDLENBQVdQLElBQUksQ0FBQ3VaLElBQUksQ0FBQ3pCLFNBQVMsQ0FBQztJQUNsTixNQUFNNmUsVUFBVSxHQUFHcjFCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFYixLQUFLLENBQUNDLENBQUMsR0FBR3FwQixLQUFLLENBQUNycEIsQ0FBQyxFQUFFRCxLQUFLLENBQUNFLENBQUMsR0FBR29wQixLQUFLLENBQUNwcEIsQ0FBQyxDQUFDO0lBQ3ZFLE1BQU15d0IsTUFBTSxHQUFHaHhCLElBQUksQ0FBQ0MsSUFBSSxLQUFLLE9BQU8sSUFBSUQsSUFBSSxDQUFDdVosSUFBSSxDQUFDK0MsTUFBTSxLQUFLLFFBQVE7SUFDckUsSUFBS3RjLElBQUksQ0FBQ0MsSUFBSSxLQUFLLFNBQVMsSUFBSSxDQUFDK3dCLE1BQU0sSUFBSyxDQUFDMkYsVUFBVSxFQUFFanRCLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyxtQkFBbUI5RSxRQUFRLENBQUNDLEtBQUssQ0FBQyxFQUFFLENBQUM7SUFDMUcsSUFBSUwsSUFBSSxDQUFDdVosSUFBSSxDQUFDK0MsTUFBTSxJQUFJLENBQUMwVSxNQUFNLElBQUksQ0FBQTJGLFVBQVUsYUFBVkEsVUFBVSx1QkFBVkEsVUFBVSxDQUFFMTJCLElBQUksTUFBSyxTQUFTLElBQUksQ0FBQTAyQixVQUFVLGFBQVZBLFVBQVUsdUJBQVZBLFVBQVUsQ0FBRTEyQixJQUFJLE1BQUssV0FBVyxJQUFJLENBQUEwMkIsVUFBVSxhQUFWQSxVQUFVLHVCQUFWQSxVQUFVLENBQUUxMkIsSUFBSSxNQUFLLE9BQU8sRUFBRXlKLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQywyQkFBMkI5RSxRQUFRLENBQUNDLEtBQUssQ0FBQyxFQUFFLENBQUM7RUFDbE07RUFDQSxNQUFNa0MsU0FBUyxHQUFHMkYsZ0JBQWdCLENBQUNoSCxLQUFLLENBQUM7RUFDekMsSUFBSSxDQUFDNHlCLGlCQUFpQixDQUFDNXlCLEtBQUssRUFBRXFCLFNBQVMsRUFBRXJCLEtBQUssQ0FBQ3dGLElBQUksQ0FBQyxFQUFFZ0QsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLGtCQUFrQixDQUFDO0VBQ3JGLE1BQU04QyxPQUFPLEdBQUdDLGdCQUFnQixDQUFDL0csS0FBSyxDQUFDO0VBQ3ZDLElBQUksQ0FBQzhHLE9BQU8sQ0FBQ2pELE1BQU0sSUFBSSxDQUFDZ3ZCLGdCQUFnQixDQUFDN3lCLEtBQUssRUFBRXFCLFNBQVMsRUFBRXlGLE9BQU8sQ0FBQyxFQUFFMEIsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLDBCQUEwQmhFLEtBQUssQ0FBQ2dKLFNBQVMsQ0FBQ2pLLElBQUksRUFBRSxDQUFDO0VBQ2xJLElBQUlpQixLQUFLLENBQUN5SixVQUFVLENBQUM1RixNQUFNLEdBQUcsQ0FBQyxJQUFJN0QsS0FBSyxDQUFDeUosVUFBVSxDQUFDNUYsTUFBTSxHQUFHLENBQUMsRUFBRTJFLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx5QkFBeUIsQ0FBQztFQUN0RyxNQUFNdEMsWUFBWSxJQUFBMnpCLG9CQUFBLEdBQUdyMUIsS0FBSyxDQUFDMEIsWUFBWSxjQUFBMnpCLG9CQUFBLGNBQUFBLG9CQUFBLEdBQUksRUFBRTtFQUM3QyxNQUFNSyxvQkFBNkMsR0FBRyxDQUFDLENBQUMsVUFBVSxFQUFFLFVBQVUsQ0FBQyxFQUFFLENBQUMsWUFBWSxFQUFFLE1BQU0sQ0FBQyxFQUFFLENBQUMsV0FBVyxFQUFFLE1BQU0sQ0FBQyxFQUFFLENBQUMsYUFBYSxFQUFFLE1BQU0sQ0FBQyxDQUFDO0VBQ3hKLEtBQUssTUFBTSxDQUFDOXpCLFdBQVcsRUFBRTdDLElBQUksQ0FBQyxJQUFJMjJCLG9CQUFvQixFQUFFO0lBQ3RELE1BQU1qMEIsS0FBSyxHQUFHQyxZQUFZLENBQUNqQixJQUFJLENBQUNrQixTQUFTLElBQUlBLFNBQVMsQ0FBQ0MsV0FBVyxLQUFLQSxXQUFXLENBQUM7SUFDbkYsSUFBSSxDQUFDSCxLQUFLLElBQUlBLEtBQUssQ0FBQzFDLElBQUksS0FBS0EsSUFBSSxJQUFJMEMsS0FBSyxDQUFDSyxPQUFPLENBQUMrQixNQUFNLEtBQUssQ0FBQyxJQUFJLElBQUkzQyxHQUFHLENBQUNPLEtBQUssQ0FBQ0ssT0FBTyxDQUFDNkQsR0FBRyxDQUFDZ3dCLE1BQU0sSUFBSUEsTUFBTSxDQUFDMUQsSUFBSSxDQUFDLENBQUMsQ0FBQ3ZxQixJQUFJLEtBQUssQ0FBQyxFQUFFYyxNQUFNLENBQUN4RSxJQUFJLENBQUMseUJBQXlCcEMsV0FBVyxFQUFFLENBQUM7SUFDcEwsSUFBSSxDQUFDNUIsS0FBSyxDQUFDeUosVUFBVSxDQUFDdEMsSUFBSSxDQUFDK2EsU0FBUyxJQUFJQSxTQUFTLENBQUM4QyxTQUFTLEtBQUtwakIsV0FBVyxJQUFJc2dCLFNBQVMsQ0FBQ25qQixJQUFJLEtBQUtBLElBQUksQ0FBQyxFQUFFeUosTUFBTSxDQUFDeEUsSUFBSSxDQUFDLDZCQUE2QnBDLFdBQVcsRUFBRSxDQUFDO0VBQ2xLO0VBQ0EsSUFBSSxDQUFDUix1QkFBdUIsQ0FBQ3BCLEtBQUssRUFBRXFCLFNBQVMsQ0FBQyxFQUFFbUgsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLGdDQUFnQyxDQUFDO0VBQzdGLElBQUksRUFBQXN4QixxQkFBQSxJQUFBQyxpQkFBQSxHQUFDdjFCLEtBQUssQ0FBQ29KLFVBQVUsY0FBQW1zQixpQkFBQSx1QkFBaEJBLGlCQUFBLENBQWtCMXhCLE1BQU0sY0FBQXl4QixxQkFBQSxjQUFBQSxxQkFBQSxHQUFJLENBQUMsTUFBTSxDQUFDLEVBQUU5c0IsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLHlCQUF5QixDQUFDO0VBQ2pGLE1BQU00eEIsa0JBQWtCLEdBQUcsSUFBSTEwQixHQUFHLENBQVMsQ0FBQztFQUM1QyxLQUFLLE1BQU1naEIsU0FBUyxJQUFJbGlCLEtBQUssQ0FBQ3lKLFVBQVUsRUFBRTtJQUN4QyxNQUFNb1ksR0FBRyxHQUFHM2lCLFFBQVEsQ0FBQ2dqQixTQUFTLENBQUM7SUFDL0IsTUFBTXBqQixJQUFJLEdBQUdzQixPQUFPLENBQUNKLEtBQUssRUFBRWtpQixTQUFTLENBQUM5aUIsQ0FBQyxFQUFFOGlCLFNBQVMsQ0FBQzdpQixDQUFDLENBQUM7SUFDckQsSUFBSSxDQUFDNmlCLFNBQVMsQ0FBQ25nQixFQUFFLElBQUksQ0FBQyxDQUFDLFVBQVUsRUFBRSxNQUFNLEVBQUUsU0FBUyxFQUFFLE9BQU8sQ0FBQyxDQUFDeEMsUUFBUSxDQUFDMmlCLFNBQVMsQ0FBQ25qQixJQUFJLENBQUMsSUFBSSxDQUFDRCxJQUFJLElBQUksQ0FBQ1EsUUFBUSxDQUFDUixJQUFJLENBQUNDLElBQUksQ0FBQyxJQUFLRCxJQUFJLENBQUNDLElBQUksS0FBSyxNQUFNLElBQUltakIsU0FBUyxDQUFDbmpCLElBQUksS0FBSyxPQUFRLElBQUksQ0FBQzZ6QixpQkFBaUIsQ0FBQzV5QixLQUFLLEVBQUVxQixTQUFTLEVBQUU2Z0IsU0FBUyxDQUFDLEVBQUUxWixNQUFNLENBQUN4RSxJQUFJLENBQUMsMEJBQTBCa2UsU0FBUyxDQUFDbmdCLEVBQUUsRUFBRSxDQUFDO0lBQzFSLElBQUk2ekIsa0JBQWtCLENBQUN0ekIsR0FBRyxDQUFDdWYsR0FBRyxDQUFDLEVBQUVyWixNQUFNLENBQUN4RSxJQUFJLENBQUMsMEJBQTBCNmQsR0FBRyxFQUFFLENBQUM7SUFDN0UrVCxrQkFBa0IsQ0FBQ3B4QixHQUFHLENBQUNxZCxHQUFHLENBQUM7RUFDN0I7RUFDQSxLQUFLLE1BQU15SixTQUFTLEtBQUF1SyxrQkFBQSxHQUFJNzFCLEtBQUssQ0FBQ29KLFVBQVUsY0FBQXlzQixrQkFBQSxjQUFBQSxrQkFBQSxHQUFJLEVBQUUsRUFBRTtJQUFBLElBQUFBLGtCQUFBO0lBQzlDLE1BQU0vMkIsSUFBSSxHQUFHc0IsT0FBTyxDQUFDSixLQUFLLEVBQUVzckIsU0FBUyxDQUFDbHNCLENBQUMsRUFBRWtzQixTQUFTLENBQUNqc0IsQ0FBQyxDQUFDO0lBQ3JELElBQUksQ0FBQ2lzQixTQUFTLENBQUN2cEIsRUFBRSxJQUFJLENBQUMsQ0FBQyxVQUFVLEVBQUUsY0FBYyxFQUFFLGlCQUFpQixFQUFFLFlBQVksRUFBRSxjQUFjLEVBQUUsY0FBYyxFQUFFLFVBQVUsRUFBRSxXQUFXLEVBQUUsYUFBYSxFQUFFLFlBQVksRUFBRSxjQUFjLEVBQUUsV0FBVyxFQUFFLFlBQVksRUFBRSxXQUFXLEVBQUUsV0FBVyxFQUFFLFNBQVMsRUFBRSxhQUFhLEVBQUUsY0FBYyxFQUFFLFlBQVksRUFBRSxVQUFVLEVBQUUsZ0JBQWdCLEVBQUUsVUFBVSxFQUFFLFVBQVUsRUFBRSxXQUFXLEVBQUUsV0FBVyxFQUFFLGFBQWEsRUFBRSxhQUFhLEVBQUUsV0FBVyxFQUFFLFdBQVcsRUFBRSxhQUFhLEVBQUUsYUFBYSxFQUFFLGFBQWEsRUFBRSxhQUFhLEVBQUUsWUFBWSxFQUFFLFlBQVksRUFBRSxZQUFZLEVBQUUsWUFBWSxFQUFFLFVBQVUsRUFBRSxVQUFVLEVBQUUsV0FBVyxFQUFFLFdBQVcsQ0FBQyxDQUFDeEMsUUFBUSxDQUFDK3JCLFNBQVMsQ0FBQ3ZzQixJQUFJLENBQUMsSUFBSSxDQUFDRCxJQUFJLElBQUksQ0FBQ1EsUUFBUSxDQUFDUixJQUFJLENBQUNDLElBQUksQ0FBQyxJQUFJRCxJQUFJLENBQUNDLElBQUksS0FBSyxNQUFNLElBQUksQ0FBQzZ6QixpQkFBaUIsQ0FBQzV5QixLQUFLLEVBQUVxQixTQUFTLEVBQUVpcUIsU0FBUyxDQUFDLEVBQUU5aUIsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLDBCQUEwQnNuQixTQUFTLENBQUN2cEIsRUFBRSxFQUFFLENBQUM7RUFDbHhCO0VBQ0EsS0FBSyxNQUFNb2dCLE9BQU8sS0FBQTJULGVBQUEsR0FBSTkxQixLQUFLLENBQUNtaUIsT0FBTyxjQUFBMlQsZUFBQSxjQUFBQSxlQUFBLEdBQUksRUFBRSxFQUFFO0lBQUEsSUFBQUEsZUFBQTtJQUN6QyxNQUFNaDNCLElBQUksR0FBR3NCLE9BQU8sQ0FBQ0osS0FBSyxFQUFFbWlCLE9BQU8sQ0FBQ3RoQixNQUFNLENBQUN6QixDQUFDLEVBQUUraUIsT0FBTyxDQUFDdGhCLE1BQU0sQ0FBQ3hCLENBQUMsQ0FBQztJQUMvRCxJQUFJLENBQUM4aUIsT0FBTyxDQUFDcGdCLEVBQUUsSUFBSSxDQUFDLENBQUMsTUFBTSxFQUFFLE1BQU0sRUFBRSxPQUFPLEVBQUUsVUFBVSxFQUFFLE1BQU0sRUFBRSxXQUFXLEVBQUUsU0FBUyxFQUFFLFlBQVksQ0FBQyxDQUFDeEMsUUFBUSxDQUFDNGlCLE9BQU8sQ0FBQ3BqQixJQUFJLENBQUMsSUFBSSxDQUFDRCxJQUFJLElBQUksQ0FBQzhOLE1BQU0sQ0FBQ0MsU0FBUyxDQUFDc1YsT0FBTyxDQUFDZ08sUUFBUSxDQUFDLElBQUloTyxPQUFPLENBQUNnTyxRQUFRLEdBQUcsQ0FBQyxJQUFJLENBQUN2akIsTUFBTSxDQUFDQyxTQUFTLENBQUNzVixPQUFPLENBQUNpTyxRQUFRLENBQUMsSUFBSWpPLE9BQU8sQ0FBQ2lPLFFBQVEsR0FBRyxDQUFDLElBQUksQ0FBQ2pPLE9BQU8sQ0FBQzhOLE9BQU8sSUFBSSxDQUFDOU4sT0FBTyxDQUFDK04sU0FBUyxDQUFDcnNCLE1BQU0sSUFBSSxDQUFDc2UsT0FBTyxDQUFDNFQsT0FBTyxJQUFJLENBQUMsQ0FBQyxTQUFTLEVBQUUsUUFBUSxFQUFFLFVBQVUsQ0FBQyxDQUFDeDJCLFFBQVEsQ0FBQzRpQixPQUFPLENBQUM3ZSxLQUFLLENBQUMsRUFBRTtNQUFFa0YsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLDBCQUEwQm1lLE9BQU8sQ0FBQ3BnQixFQUFFLEVBQUUsQ0FBQztNQUFFO0lBQVM7SUFDMWMsTUFBTWkwQixZQUFZLEdBQUdsM0IsSUFBSSxDQUFDQyxJQUFJO0lBQzlCLE1BQU1rM0IsWUFBWSxHQUFHbjNCLElBQUksQ0FBQ3VaLElBQUksR0FBRztNQUFFLEdBQUd2WixJQUFJLENBQUN1WjtJQUFLLENBQUMsR0FBRy9YLFNBQVM7SUFDN0R4QixJQUFJLENBQUNDLElBQUksR0FBR29qQixPQUFPLENBQUMrVCxNQUFNO0lBQzFCLElBQUkvVCxPQUFPLENBQUM2TixVQUFVLEVBQUVseEIsSUFBSSxDQUFDdVosSUFBSSxHQUFHO01BQUUsR0FBRzhKLE9BQU8sQ0FBQzZOO0lBQVcsQ0FBQyxNQUN4RCxPQUFPbHhCLElBQUksQ0FBQ3VaLElBQUk7SUFDckIsTUFBTThkLFlBQVksR0FBR252QixnQkFBZ0IsQ0FBQ2hILEtBQUssQ0FBQztJQUM1QyxNQUFNbzJCLFFBQVEsR0FBR3hELGlCQUFpQixDQUFDNXlCLEtBQUssRUFBRW0yQixZQUFZLEVBQUVuMkIsS0FBSyxDQUFDd0YsSUFBSSxDQUFDLElBQUlxdEIsZ0JBQWdCLENBQUM3eUIsS0FBSyxFQUFFbTJCLFlBQVksRUFBRXB2QixnQkFBZ0IsQ0FBQy9HLEtBQUssQ0FBQyxDQUFDO0lBQ3JJbEIsSUFBSSxDQUFDQyxJQUFJLEdBQUdpM0IsWUFBWTtJQUN4QixJQUFJQyxZQUFZLEVBQUVuM0IsSUFBSSxDQUFDdVosSUFBSSxHQUFHNGQsWUFBWSxNQUNyQyxPQUFPbjNCLElBQUksQ0FBQ3VaLElBQUk7SUFDckIsSUFBSSxDQUFDK2QsUUFBUSxFQUFFNXRCLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyxrQ0FBa0NtZSxPQUFPLENBQUNwZ0IsRUFBRSxFQUFFLENBQUM7RUFDNUU7RUFDQSxNQUFNK0csVUFBVSxHQUFHLENBQUMsR0FBRzlJLEtBQUssQ0FBQ1EsTUFBTSxDQUFDbUYsR0FBRyxDQUFDakYsS0FBSyxLQUFLO0lBQUUsR0FBR0EsS0FBSztJQUFFMjFCLElBQUksRUFBRTtFQUFpQixDQUFDLENBQUMsQ0FBQyxFQUFFLEdBQUdyMkIsS0FBSyxDQUFDbUosS0FBSyxDQUFDeEQsR0FBRyxDQUFDc2MsSUFBSSxLQUFLO0lBQUUsR0FBR0EsSUFBSTtJQUFFb1UsSUFBSSxFQUFFO0VBQWdCLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDM0osTUFBTWxhLFFBQVEsR0FBRyxJQUFJamIsR0FBRyxDQUFTLENBQUM7RUFDbEMsS0FBSyxNQUFNbW1CLFNBQVMsSUFBSXZlLFVBQVUsRUFBRTtJQUNsQyxNQUFNaEssSUFBSSxHQUFHc0IsT0FBTyxDQUFDSixLQUFLLEVBQUVxbkIsU0FBUyxDQUFDam9CLENBQUMsRUFBRWlvQixTQUFTLENBQUNob0IsQ0FBQyxDQUFDO0lBQ3JELElBQUksQ0FBQ1AsSUFBSSxJQUFJLENBQUNRLFFBQVEsQ0FBQ1IsSUFBSSxDQUFDQyxJQUFJLENBQUMsSUFBSUQsSUFBSSxDQUFDQyxJQUFJLEtBQUssWUFBWSxFQUFFeUosTUFBTSxDQUFDeEUsSUFBSSxDQUFDLFdBQVdxakIsU0FBUyxDQUFDZ1AsSUFBSSxZQUFZLENBQUM7SUFDbkgsTUFBTXhVLEdBQUcsR0FBRzNpQixRQUFRLENBQUNtb0IsU0FBUyxDQUFDO0lBQy9CLElBQUlsTCxRQUFRLENBQUM3WixHQUFHLENBQUN1ZixHQUFHLENBQUMsRUFBRXJaLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyxlQUFlcWpCLFNBQVMsQ0FBQ2dQLElBQUksWUFBWSxDQUFDO0lBQzdFbGEsUUFBUSxDQUFDM1gsR0FBRyxDQUFDcWQsR0FBRyxDQUFDO0VBQ25CO0VBQ0EsSUFBSSxDQUFDN2hCLEtBQUssQ0FBQ2UsS0FBSyxDQUFDOEMsTUFBTSxFQUFFMkUsTUFBTSxDQUFDeEUsSUFBSSxDQUFDLGVBQWUsQ0FBQztFQUNyRCxNQUFNc3lCLE9BQU8sR0FBRyxJQUFJcDFCLEdBQUcsQ0FBUyxDQUFDO0VBQ2pDLE1BQU1xMUIsYUFBYSxHQUFHLElBQUlyMUIsR0FBRyxDQUFTLENBQUM7RUFDdkMsS0FBSyxNQUFNeUIsSUFBSSxJQUFJM0MsS0FBSyxDQUFDZSxLQUFLLEVBQUU7SUFBQSxJQUFBeTFCLFdBQUE7SUFDOUIsSUFBSUYsT0FBTyxDQUFDaDBCLEdBQUcsQ0FBQ0ssSUFBSSxDQUFDWixFQUFFLENBQUMsRUFBRXlHLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyxzQkFBc0JyQixJQUFJLENBQUNaLEVBQUUsRUFBRSxDQUFDO0lBQ3RFdTBCLE9BQU8sQ0FBQzl4QixHQUFHLENBQUM3QixJQUFJLENBQUNaLEVBQUUsQ0FBQztJQUNwQixJQUFJLENBQUN4RSxRQUFRLENBQUNnQyxRQUFRLENBQUNvRCxJQUFJLENBQUM1RCxJQUFJLENBQUMsRUFBRTtNQUFFeUosTUFBTSxDQUFDeEUsSUFBSSxDQUFDLGlCQUFpQnJCLElBQUksQ0FBQzVELElBQUksRUFBRSxDQUFDO01BQUU7SUFBUztJQUN6RixNQUFNMGhCLFVBQVUsR0FBR2hqQixjQUFjLENBQUNrRixJQUFJLENBQUM1RCxJQUFJLENBQUM7SUFDNUMsTUFBTUQsSUFBSSxHQUFHc0IsT0FBTyxDQUFDSixLQUFLLEVBQUUyQyxJQUFJLENBQUN2RCxDQUFDLEVBQUV1RCxJQUFJLENBQUN0RCxDQUFDLENBQUM7SUFDM0MsTUFBTW8zQixRQUFRLEdBQUd2M0IsUUFBUSxDQUFDeUQsSUFBSSxDQUFDO0lBQy9CLElBQUk0ekIsYUFBYSxDQUFDajBCLEdBQUcsQ0FBQ20wQixRQUFRLENBQUMsRUFBRWp1QixNQUFNLENBQUN4RSxJQUFJLENBQUMsK0JBQStCeXlCLFFBQVEsRUFBRSxDQUFDO0lBQ3ZGRixhQUFhLENBQUMveEIsR0FBRyxDQUFDaXlCLFFBQVEsQ0FBQztJQUMzQixJQUFJOXpCLElBQUksQ0FBQzZFLEtBQUssS0FBS3hILEtBQUssQ0FBQ3dILEtBQUssSUFBSWlaLFVBQVUsQ0FBQ2paLEtBQUssS0FBS3hILEtBQUssQ0FBQ3dILEtBQUssRUFBRWdCLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx1QkFBdUJyQixJQUFJLENBQUNaLEVBQUUsRUFBRSxDQUFDO0lBQ2pILElBQUksQ0FBQ2pELElBQUksSUFBSSxDQUFDUSxRQUFRLENBQUNSLElBQUksQ0FBQ0MsSUFBSSxDQUFDLElBQUlELElBQUksQ0FBQ0MsSUFBSSxLQUFLLFlBQVksSUFBSSxDQUFDMGhCLFVBQVUsQ0FBQzRELE9BQU8sQ0FBQzlrQixRQUFRLENBQUNULElBQUksQ0FBQ0MsSUFBSSxDQUFDLEVBQUV5SixNQUFNLENBQUN4RSxJQUFJLENBQUMsMkJBQTJCckIsSUFBSSxDQUFDWixFQUFFLEVBQUUsQ0FBQztJQUM3SixJQUFJLENBQUN5TixjQUFjLENBQUN4UCxLQUFLLEVBQUUyQyxJQUFJLENBQUM1RCxJQUFJLEVBQUU0RCxJQUFJLENBQUMsRUFBRTZGLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx5QkFBeUJyQixJQUFJLENBQUNaLEVBQUUsRUFBRSxDQUFDLE1BQ3ZGLElBQUl6RSxjQUFjLENBQUNxRixJQUFJLENBQUMsRUFBRTtNQUM3QixNQUFNK3pCLGFBQWEsR0FBRyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDdnZCLElBQUksQ0FBQyxDQUFDLENBQUMvSCxDQUFDLEVBQUVDLENBQUMsQ0FBQyxLQUFLdXpCLGlCQUFpQixDQUFDNXlCLEtBQUssRUFBRXFCLFNBQVMsRUFBRTtRQUFFakMsQ0FBQyxFQUFFdUQsSUFBSSxDQUFDdkQsQ0FBQyxHQUFHQSxDQUFDO1FBQUVDLENBQUMsRUFBRXNELElBQUksQ0FBQ3RELENBQUMsR0FBR0E7TUFBRSxDQUFDLENBQUMsQ0FBQztNQUNoSixJQUFJLENBQUNxM0IsYUFBYSxFQUFFbHVCLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyxxQkFBcUJyQixJQUFJLENBQUNaLEVBQUUsRUFBRSxDQUFDO0lBQ2pFLENBQUMsTUFBTSxJQUFJLENBQUNWLFNBQVMsQ0FBQ2lCLEdBQUcsQ0FBQ3ZDLE9BQU8sQ0FBQ0MsS0FBSyxFQUFFMkMsSUFBSSxDQUFDdkQsQ0FBQyxFQUFFdUQsSUFBSSxDQUFDdEQsQ0FBQyxDQUFDLENBQUMsRUFBRW1KLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyxxQkFBcUJyQixJQUFJLENBQUNaLEVBQUUsRUFBRSxDQUFDO0lBQ3RHLElBQUksQ0FBQyxDQUFDLFNBQVMsRUFBRSxXQUFXLEVBQUUsV0FBVyxFQUFFLFdBQVcsQ0FBQyxDQUFDeEMsUUFBUSxDQUFDb0QsSUFBSSxDQUFDVyxLQUFLLENBQUMsRUFBRWtGLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx1QkFBdUJyQixJQUFJLENBQUNaLEVBQUUsRUFBRSxDQUFDO0lBQzNILElBQUksQ0FBQ1ksSUFBSSxDQUFDcWQsSUFBSSxDQUFDbmMsTUFBTSxJQUFJLEdBQUEyeUIsV0FBQSxHQUFDN3pCLElBQUksQ0FBQ3NkLEtBQUssY0FBQXVXLFdBQUEsZUFBVkEsV0FBQSxDQUFZM3lCLE1BQU0sS0FBSSxDQUFDbEIsSUFBSSxDQUFDc2QsS0FBSyxDQUFDMWdCLFFBQVEsQ0FBQyxTQUFTLENBQUMsRUFBRWlKLE1BQU0sQ0FBQ3hFLElBQUksQ0FBQyx1QkFBdUJyQixJQUFJLENBQUNaLEVBQUUsRUFBRSxDQUFDO0VBQ2hJO0VBQ0F5RyxNQUFNLENBQUN4RSxJQUFJLENBQUMsR0FBRyt1QixvQkFBb0IsQ0FBQy95QixLQUFLLENBQUMsRUFBRSxHQUFHZzFCLHdCQUF3QixDQUFDaDFCLEtBQUssQ0FBQyxDQUFDO0VBQy9FLEtBQUssTUFBTTIyQixLQUFLLElBQUl6NUIsZ0JBQWdCLENBQUNELFdBQVcsQ0FBQytDLEtBQUssQ0FBQ3dILEtBQUssQ0FBQyxDQUFDLEVBQUVnQixNQUFNLENBQUN4RSxJQUFJLENBQUMsb0JBQW9CMnlCLEtBQUssRUFBRSxDQUFDO0VBQ3hHLE9BQU87SUFBRXB1QixLQUFLLEVBQUVDLE1BQU0sQ0FBQzNFLE1BQU0sS0FBSyxDQUFDO0lBQUUyRTtFQUFPLENBQUM7QUFDL0MsQ0FBQztBQUdELE1BQU1vdUIsbUJBQW1CLEdBQUdBLENBQUM1MkIsS0FBWSxFQUFFNjJCLFNBQWlCLEtBQWE7RUFBQSxJQUFBQyxvQkFBQSxFQUFBQyxpQkFBQTtFQUN2RSxNQUFNdmdCLEtBQUssR0FBR3pSLGtCQUFrQixDQUFDL0UsS0FBSyxDQUFDO0VBQ3ZDLE1BQU1qQixJQUFJLEdBQUc4M0IsU0FBUyxDQUFDdDNCLFFBQVEsQ0FBQyxRQUFRLENBQUMsSUFBSXMzQixTQUFTLENBQUN0M0IsUUFBUSxDQUFDLFVBQVUsQ0FBQyxHQUFHLGdCQUFnQixHQUFHczNCLFNBQVMsQ0FBQ3QzQixRQUFRLENBQUMsV0FBVyxDQUFDLEdBQUcsV0FBVyxHQUFHczNCLFNBQVMsQ0FBQ3QzQixRQUFRLENBQUMsTUFBTSxDQUFDLEdBQUcsTUFBTSxHQUFHczNCLFNBQVMsQ0FBQ3QzQixRQUFRLENBQUMsT0FBTyxDQUFDLEdBQUcsT0FBTyxHQUFHLE1BQU07RUFDck8sUUFBQXUzQixvQkFBQSxHQUFPdGdCLEtBQUssYUFBTEEsS0FBSyxnQkFBQXVnQixpQkFBQSxHQUFMdmdCLEtBQUssQ0FBRW5QLEtBQUssQ0FBQzVHLElBQUksQ0FBQzJHLElBQUksSUFBSUEsSUFBSSxDQUFDckksSUFBSSxLQUFLQSxJQUFJLENBQUMsY0FBQWc0QixpQkFBQSx1QkFBN0NBLGlCQUFBLENBQStDaDFCLEVBQUUsY0FBQSswQixvQkFBQSxjQUFBQSxvQkFBQSxHQUFJLFNBQVM5MkIsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQyxJQUFJbkIsSUFBSSxFQUFFO0FBQ2hHLENBQUM7QUFDRCxPQUFPLE1BQU1pNEIsNEJBQTRCLEdBQUdBLENBQUNoM0IsS0FBWSxFQUFFdU0sVUFBVSxHQUFHQyxrQkFBa0IsQ0FBQ3hNLEtBQUssQ0FBQyxLQUFvQ3VNLFVBQVUsQ0FBQy9ELE1BQU0sQ0FBQzdDLEdBQUcsQ0FBQ2t4QixTQUFTLEtBQUs7RUFBRTl1QixJQUFJLEVBQUUvSCxLQUFLLENBQUMrSCxJQUFJO0VBQUVQLEtBQUssRUFBRXhILEtBQUssQ0FBQ3dILEtBQUs7RUFBRXhILEtBQUssRUFBRUEsS0FBSyxDQUFDRSxLQUFLLEdBQUcsQ0FBQztFQUFFKzJCLFNBQVMsRUFBRUwsbUJBQW1CLENBQUM1MkIsS0FBSyxFQUFFNjJCLFNBQVMsQ0FBQztFQUFFQTtBQUFVLENBQUMsQ0FBQyxDQUFDO0FBRXhTLE9BQU8sTUFBTUssYUFBYSxHQUFJbDNCLEtBQVksSUFBY3dNLGtCQUFrQixDQUFDeE0sS0FBSyxDQUFDLENBQUN1SSxLQUFLIiwiaWdub3JlTGlzdCI6W119