import { ITEMS, MONSTERS, biomeForFloor, monsterById, monsterRoleFor, terrainAffinityFor } from './content'
import { rngFor, streamSeed, type Rng } from './rng'
import { FLOOR_COUNT, MAP_HEIGHT, MAP_WIDTH, type Actor, type Biome, type DifficultyContext, type Direction, type Floor, type FloorEncounter, type Point, type Prop, type Tile, type TileKind, type TraversalToolId, floorIndex, floorPoint, inFloorBounds } from './types'
import { objectiveForFloor } from './objectives'
import { gateForArea, validateAreaGate } from './area-gates'
import { puzzleTemplatesFor, validateFloorPuzzles, validatePuzzleTemplates } from './puzzles'
import { isBlockingProp, PROP_IDS, propAt, propDefinition, propDefinitionsFor, validatePropDefinitions } from './props'
import { generateRouteContract, validateRouteContract, type RouteContract, type RouteEdgeMode, type RouteNodeKind } from './route-contract'
import { compileRouteContract, macroConnectorPoints, macroRecipeFor, validateMacroRealization, type MacroRecipeDebug } from './macro-recipe'
import { selectPlacement, type PlacementContext, type PlacementContract, type PlacementDebug } from './placement-contract'
import { definitionForEncounter, encounterPlansFor, membersForEncounter } from './encounter-director'
import { ecologyEventFor, ecologyProfileFor } from './ecology'
import { escalationFor } from './escalation'
import { socialContractFor } from './social-contract'

const tile = (kind: Tile['kind']): Tile => ({ kind, explored: false, visible: false })
const pointKey = (point: Point) => `${point.x},${point.y}`
const passable = (kind: Tile['kind']) => !['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest', 'deepWater', 'breakwall', 'cliffWall'].includes(kind)
const propDefinitionErrors = validatePropDefinitions()
const macroDebugs = new WeakMap<Floor, MacroRecipeDebug>()
const macroPilots = new WeakMap<Floor, boolean>()
const placementDebugs = new WeakMap<Floor, PlacementDebug[]>()
const routeContractDebugs = new WeakMap<Floor, RouteContract>()
const tacticalEncounterDebugs = new WeakMap<Floor, readonly NonNullable<Actor['encounter']>[]>()

const indexOf = (floor: Floor, x: number, y: number): number => floorIndex(floor, x, y)
const pointAt = (floor: Floor, index: number): Point => floorPoint(floor, index)
const inBounds = (floor: Floor, x: number, y: number): boolean => inFloorBounds(floor, x, y)
export const getTile = (floor: Floor, x: number, y: number): Tile | undefined => inBounds(floor, x, y) ? floor.tiles[indexOf(floor, x, y)] : undefined
export const actorAt = (floor: Floor, x: number, y: number): Actor | undefined => floor.actors.find(actor => actor.x === x && actor.y === y && actor.health > 0)
export const isPassable = (floor: Floor, x: number, y: number): boolean => {
  const target = getTile(floor, x, y)
  return Boolean(target && passable(target.kind) && target.kind !== 'lockedDoor' && !actorAt(floor, x, y) && !isBlockingProp(propAt(floor.props, x, y)))
}

const toolDirections = [[0, -1], [1, 0], [0, 1], [-1, 0]] as const
const toolHazards = new Set<TileKind>(['pit', 'water', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder', 'bramble', 'rubble', 'brine', 'frostRime'])
const drillableToolTerrain = new Set<TileKind>(['wall', 'rubble', 'bramble', 'boulder'])
export const primaryToolUseAvailable = (floor: Floor): boolean => {
  const offer = floor.rewardOffers?.find(candidate => candidate.milestoneId === 'waycache')
  if (offer?.kind !== 'waycache') return false
  const tool = offer.choices[0]?.id as TraversalToolId | undefined
  if (!tool) return false
  for (const index of reachableFloorIndexes(floor)) {
    const origin = pointAt(floor, index)
    if (!isPassable(floor, origin.x, origin.y)) continue
    for (const [dx, dy] of toolDirections) {
      const first = { x: origin.x + dx, y: origin.y + dy }
      const second = { x: origin.x + dx * 2, y: origin.y + dy * 2 }
      const firstTile = getTile(floor, first.x, first.y)
      if (tool === 'stoneWedge' && firstTile && drillableToolTerrain.has(firstTile.kind)) return true
      if (tool === 'reedwing' && firstTile && toolHazards.has(firstTile.kind) && isPassable(floor, second.x, second.y)) return true
      if (tool === 'cordAnchor' && isPassable(floor, second.x, second.y)) return true
      if (tool === 'ashwayRites' && firstTile && toolHazards.has(firstTile.kind) && firstTile.kind !== 'boulder') return true
    }
  }
  return false
}

const pathOffsets = [[-1, -1], [0, -1], [1, -1], [-1, 0], [1, 0], [-1, 1], [0, 1], [1, 1]] as const
export interface TraversalOptions { target?: Point; ignoreBlockingProps?: boolean }
export interface TraversalResult { reachable: Set<number>; path?: Point[]; blockers: string[] }
const traversalBlocker = (floor: Floor, point: Point, options: TraversalOptions): string | undefined => {
  const current = getTile(floor, point.x, point.y)
  if (!current) return 'bounds'
  if (!passable(current.kind) || current.kind === 'lockedDoor') return `terrain:${current.kind}`
  const prop = propAt(floor.props, point.x, point.y)
  if (!options.ignoreBlockingProps && prop && isBlockingProp(prop)) return `prop:${prop.id}`
  return undefined
}
const isPathPassable = (floor: Floor, point: Point, ignoreBlockingProps = false): boolean => !traversalBlocker(floor, point, { ignoreBlockingProps })

export const traverseFloor = (floor: Floor, start = floor.start, options: TraversalOptions = {}): TraversalResult => {
  if (traversalBlocker(floor, start, options)) return { reachable: new Set(), blockers: [`start:${traversalBlocker(floor, start, options)}`] }
  const initial = indexOf(floor, start.x, start.y)
  const seen = new Set<number>([initial])
  const queue = [initial]
  const previous = new Map<number, number | undefined>([[initial, undefined]])
  const blockers = new Set<string>()
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const current = queue[cursor]
    const point = pointAt(floor, current)
    if (options.target && point.x === options.target.x && point.y === options.target.y) {
      const path: Point[] = []
      for (let index: number | undefined = current; index !== undefined; index = previous.get(index)) path.push(pointAt(floor, index))
      return { reachable: seen, path: path.reverse(), blockers: [...blockers].sort() }
    }
    for (const [x, y] of pathOffsets) {
      const next = { x: point.x + x, y: point.y + y }
      if (!inBounds(floor, next.x, next.y)) continue
      const blocker = traversalBlocker(floor, next, options)
      if (blocker) { blockers.add(`${next.x},${next.y}:${blocker}`); continue }
      const nextIndex = indexOf(floor, next.x, next.y)
      if (!seen.has(nextIndex)) { seen.add(nextIndex); previous.set(nextIndex, current); queue.push(nextIndex) }
    }
  }
  return { reachable: seen, blockers: [...blockers].sort() }
}

export const reachableFloorIndexes = (floor: Floor, start = floor.start, ignoreBlockingProps = false): Set<number> => traverseFloor(floor, start, { ignoreBlockingProps }).reachable
export const hasPassableTerrainPath = (floor: Floor, start: Point, destination: Point): boolean => Boolean(traverseFloor(floor, start, { target: destination, ignoreBlockingProps: true }).path)
export const hasPassablePath = (floor: Floor, start: Point, destination: Point): boolean => Boolean(traverseFloor(floor, start, { target: destination }).path)
export const macroRecipeDebug = (floor: Floor): MacroRecipeDebug | undefined => macroDebugs.get(floor)
export const placementDebug = (floor: Floor): readonly PlacementDebug[] => placementDebugs.get(floor) ?? []
export const routeContractDebug = (floor: Floor): RouteContract | undefined => routeContractDebugs.get(floor)
export const escalationDebug = (floor: Floor): Floor['escalation'] => floor.escalation
export const tacticalEncounterDebug = (floor: Floor): readonly NonNullable<Actor['encounter']>[] => tacticalEncounterDebugs.get(floor) ?? []
export const validateMacroRecipe = (floor: Floor): string[] => {
  const debug = macroDebugs.get(floor)
  return !debug || !macroPilots.get(floor) ? [] : validateMacroRealization(debug, point => Boolean(getTile(floor, point.x, point.y) && isPathPassable(floor, point, false)))
}

export const preservesExitPath = (floor: Floor, start: Point, point: Point, kind: Tile['kind']): boolean => {
  const target = getTile(floor, point.x, point.y)
  if (!target) return false
  const previous = target.kind
  target.kind = kind
  const preserves = hasPassableTerrainPath(floor, start, floor.exit)
  target.kind = previous
  return preserves
}

export const preservesAdjacentExitAccess = (floor: Floor, point: Point, kind: Tile['kind']): boolean => {
  const target = getTile(floor, point.x, point.y)
  if (!target) return false
  const previous = target.kind
  target.kind = kind
  const adjacent = [[0, -1], [1, 0], [0, 1], [-1, 0]]
    .map(([x, y]) => ({ x: point.x + x, y: point.y + y }))
    .filter(candidate => {
      const tile = getTile(floor, candidate.x, candidate.y)
      return Boolean(tile && passable(tile.kind) && tile.kind !== 'lockedDoor')
    })
  const preserves = adjacent.length > 0 && adjacent.every(candidate => hasPassableTerrainPath(floor, candidate, floor.exit))
  target.kind = previous
  return preserves
}

export const difficultyFor = (routePosition: number, areaFloor: number): DifficultyContext => {
  const threat = Math.max(0, Math.min(15, routePosition * 4 + areaFloor))
  return { routePosition, threat, healthMultiplier: 1 + threat * 0.06, attackBonus: Math.floor(threat / 2), defenseBonus: Math.floor(threat / 5), eliteChance: Math.min(35, 4 + threat * 2), guardianPattern: threat >= 12 ? 3 : threat >= 8 ? 2 : threat >= 4 ? 1 : 0 }
}

const assertGenerationPhase = (floor: Floor, campaignSeed: number, contract: ReturnType<typeof generateRouteContract>, phase: string): void => {
  const targets = [floor.exit, ...objectiveTargets(floor)]
  for (const target of targets) {
    const trace = traverseFloor(floor, floor.start, { target })
    const reachable = target.x === floor.exit.x && target.y === floor.exit.y ? Boolean(trace.path) : canReachObjectiveWithProps(floor, target)
    if (reachable) continue
    const node = contract.nodes.find(candidate => candidate.kind === (target.x === floor.exit.x && target.y === floor.exit.y ? (floor.index % 4 === 3 ? 'boss' : 'exit') : 'objective'))
    throw new Error(`generation failure seed=${campaignSeed} biome=${floor.biome} recipe=${floor.layoutId} phase=${phase} contractNode=${node?.id ?? 'unknown'} target=${target.x},${target.y} reached=${trace.reachable.size} blockers=${trace.blockers.join('|')}`)
  }
}

export function generateFloor(runSeed: number, index: number, difficulty = difficultyFor(Math.floor(index / 4), index % 4)): Floor {
  const seed = streamSeed(runSeed, 'generation', index)
  const biome = biomeForFloor(index)
  const areaFloor = index % 4
  const escalation = escalationFor(runSeed, biome, areaFloor)
  const layoutRng = rngFor(runSeed, 'generation', index, 'layout')
  const layoutId = layoutFor(runSeed, biome, areaFloor)
  const routeContract = generateRouteContract({ campaignSeed: runSeed, floorIndex: index, biome, areaFloor, recipeId: layoutId, escalationVariant: `${escalation.arcId}:${escalation.phase}` })
  const routeValidation = validateRouteContract(routeContract)
  if (!routeValidation.valid) throw new Error(`invalid route contract ${routeContract.id}: ${routeValidation.errors.join('; ')}`)
  const { width, height } = dimensionsFor(biome)
  const macro = compileRouteContract(routeContract, { width, height })
  if (!macro.valid) throw new Error(`invalid macro recipe ${macro.recipeId}: ${macro.diagnostics.join('; ')}`)
  const placements: PlacementRuntime = { macro, pilot: macroRecipeFor(routeContract).pilot, diagnostics: [] }
  const objective = objectiveForFloor(index)
  const floor: Floor = {
    index,
    biome,
    seed,
    width,
    height,
    layoutId,
    tiles: Array.from({ length: width * height }, () => tile('wall')),
    actors: [],
    items: [],
    props: [],
    encounters: [],
    start: { x: 2, y: 2 },
    exit: { x: width - 3, y: height - 3 },
    guardianDefeated: areaFloor !== 3,
    objective: { ...objective, label: `${objective.label} — ${areaFloor === 3 ? escalation.payoff : escalation.promise}` },
    milestones: [],
    rewardOffers: routeContract.rewardOffers,
    escalation,
    telegraphs: [],
    difficulty
  }
  const rooms = carveRouteContractLayout(floor, routeContract, macro, layoutRng)
  const reservedMacroCells = macroRecipeFor(routeContract).pilot ? new Set(macroConnectorPoints(macro).map(point => indexOf(floor, point.x, point.y))) : new Set<number>()
  floor.start = center(rooms[0])
  floor.exit = center(rooms[rooms.length - 1])
  setKind(floor, floor.exit.x, floor.exit.y, 'exit')
  decorateBiome(floor, rngFor(runSeed, 'generation', index, 'terrain', escalation.arcId), rooms)
  placePuzzleTemplate(floor, rngFor(runSeed, 'generation', index, 'puzzle'), rooms)
  imprintEscalationLandmark(floor, rooms)
  restoreMacroConnectors(floor, macro, reservedMacroCells)
  restoreMacroNodeTransit(floor, macro)
  imprintRuinsRitualCenter(floor, macro)
  placeEvents(floor, rooms, placements)
  placeDoorsAndLocks(floor, rngFor(runSeed, 'gates', index), rooms)
  openMandatoryLocks(floor)
  placeContainers(floor, rngFor(runSeed, 'loot', index, 'containers'), rooms, reservedMacroCells)
  restoreMacroConnectors(floor, macro, reservedMacroCells)
  imprintCavernTideRoute(floor, macro)
  imprintWildsPressureRoute(floor, macro)
  imprintRuinsWardRoute(floor, macro)
  imprintFurnaceFiringRoute(floor, macro)
  imprintCavernSetpieceContext(floor, macro)
  repairMandatoryPath(floor)
  assertGenerationPhase(floor, runSeed, routeContract, 'geometry')
  placeActors(floor, rngFor(runSeed, 'generation', index, 'actors'), placements)
  assertGenerationPhase(floor, runSeed, routeContract, 'actors')
  placeEcology(floor, placements)
  assertGenerationPhase(floor, runSeed, routeContract, 'ecology')
  placeItems(floor, rngFor(runSeed, 'loot', index, 'items'), rooms, placements)
  assertGenerationPhase(floor, runSeed, routeContract, 'loot')
  placeProps(floor, reachableIndexes(floor), reservedMacroCells, placements)
  assertGenerationPhase(floor, runSeed, routeContract, 'props')
  placeMilestones(floor, placements)
  assertGenerationPhase(floor, runSeed, routeContract, 'milestones')
  placeEncounters(floor, rngFor(runSeed, 'generation', index, 'encounters'), placements)
  assertGenerationPhase(floor, runSeed, routeContract, 'encounters')
  macroDebugs.set(floor, macro)
  macroPilots.set(floor, macroRecipeFor(routeContract).pilot)
  placementDebugs.set(floor, placements.diagnostics)
  routeContractDebugs.set(floor, routeContract)
  const macroErrors = validateMacroRecipe(floor)
  if (macroErrors.length) throw new Error(`invalid macro recipe ${macro.recipeId}: ${macroErrors.join('; ')}`)
  const validation = validateGeneration(floor)
  if (!validation.valid) throw new Error(`invalid generated floor ${index}/${layoutId}: ${validation.errors.join('; ')}`)
  return floor
}

export const areaFloorIndex = (biome: Floor['biome'], areaFloor: number): number => (['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'] as const).indexOf(biome) * 4 + areaFloor
export const generateAreaFloor = (runSeed: number, biome: Floor['biome'], areaFloor: number, routePosition = 0): Floor => {
  if (!Number.isInteger(areaFloor) || areaFloor < 0 || areaFloor > 3) throw new Error(`invalid area floor: ${areaFloor}`)
  return generateFloor(runSeed, areaFloorIndex(biome, areaFloor), difficultyFor(routePosition, areaFloor))
}

interface Room { x: number; y: number; w: number; h: number }
const center = (room: Room): Point => ({ x: room.x + Math.floor(room.w / 2), y: room.y + Math.floor(room.h / 2) })
const cardinalOffsets = [[0, -1], [1, 0], [0, 1], [-1, 0]] as const
const mineHazards = new Set<Tile['kind']>(['spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'gas', 'lava', 'pit'])

const dimensions: Record<Biome, { width: number; height: number }> = {
  mine: { width: MAP_WIDTH, height: MAP_HEIGHT }, wilds: { width: 72, height: 48 }, caverns: { width: 56, height: 44 }, ruins: { width: 64, height: 48 }, furnace: { width: 56, height: 40 }, floodedRuins: { width: 72, height: 48 }, cliffs: { width: 56, height: 52 }, burial: { width: 80, height: 56 }, saltFlats: { width: 72, height: 44 }, frostReliquary: { width: 64, height: 48 }
}
const layoutVariants: Record<Biome, readonly string[]> = {
  mine: ['rail-spine', 'branching-drifts', 'collapse-loop'], wilds: ['river-clearings', 'root-maze', 'wetland-causeways'], caverns: ['tide-chambers', 'sinkhole-galleries', 'fault-tunnels'], ruins: ['circular-precinct', 'broken-processional-loop', 'courtyard-lattice'], furnace: ['stepped-kiln-chain', 'smoke-choked-service-route', 'lift-and-ash-loop'], floodedRuins: ['braided-islands', 'drowned-causeway', 'floodgate-basin'], cliffs: ['escarpment-terraces', 'ravine-switchbacks', 'wind-shelves'], burial: ['rolling-mounds', 'ringed-necropolis', 'scattered-grave-field'], saltFlats: ['salt-basin', 'brine-fractures', 'caravan-road'], frostReliquary: ['glacial-basin', 'frozen-lake', 'reliquary-escarpment']
}
const dimensionsFor = (biome: Biome) => dimensions[biome]
export const layoutFor = (runSeed: number, biome: Biome, areaFloor: number): string => {
  const deck = rngFor(runSeed, 'generation', areaFloorIndex(biome, 0), 'layout-deck').shuffle([...layoutVariants[biome]])
  return areaFloor < deck.length ? deck[areaFloor] : `${deck[(areaFloor + runSeed) % deck.length]}-remix`
}

const escalationLandmarkTerrain: Record<Biome, readonly TileKind[]> = {
  mine: ['support', 'rail'], wilds: ['web', 'water'], caverns: ['darkness', 'water'], ruins: ['altar', 'dart'], furnace: ['lift', 'smoke'], floodedRuins: ['anchor', 'water'], cliffs: ['rope', 'ledge'], burial: ['cairn', 'graveSoil'], saltFlats: ['saltMirror', 'brine'], frostReliquary: ['ice', 'frostRime']
}
const imprintEscalationLandmark = (floor: Floor, rooms: readonly Room[]): void => {
  const room = rooms[1] ?? rooms[0]
  if (!room) return
  const target = center(room)
  const variant = Math.min(escalationLandmarkTerrain[floor.biome].length - 1, Math.floor((floor.escalation?.encounterOffset ?? 0) / 2))
  setKind(floor, target.x, target.y, escalationLandmarkTerrain[floor.biome][variant])
}

const hasNearbyTile = (floor: Floor, point: Point, radius: number, kinds: ReadonlySet<Tile['kind']>): boolean => {
  for (let y = point.y - radius; y <= point.y + radius; y++) for (let x = point.x - radius; x <= point.x + radius; x++) if (kinds.has(getTile(floor, x, y)?.kind ?? 'wall')) return true
  return false
}
const hasAdjacentTile = (floor: Floor, point: Point, kinds: ReadonlySet<Tile['kind']>): boolean => {
  for (let y = point.y - 1; y <= point.y + 1; y++) for (let x = point.x - 1; x <= point.x + 1; x++) if ((x !== point.x || y !== point.y) && kinds.has(getTile(floor, x, y)?.kind ?? 'wall')) return true
  return false
}

const hasMinePropContext = (floor: Floor, kind: Prop['kind'], point: Point): boolean => {
  if (!kind.startsWith('mine.')) return true
  const workedPassage = new Set<Tile['kind']>(['rail', 'support'])
  if (kind === 'mine.oreVein') return hasNearbyTile(floor, point, 1, new Set<Tile['kind']>(['support', 'rubble', 'boulder']))
  if (kind === 'mine.lanternPost' || kind === 'mine.discardedParcel') return hasNearbyTile(floor, point, 1, workedPassage)
  if (kind === 'mine.brokenCart') return cardinalOffsets.some(([x, y]) => getTile(floor, point.x + x, point.y + y)?.kind === 'rail')
  if (kind === 'mine.warningMarker') return hasNearbyTile(floor, point, 5, mineHazards)
  if (kind === 'mine.skullMarker') return hasNearbyTile(floor, point, 5, mineHazards) || floor.actors.some(actor => actor.hostile && Math.max(Math.abs(actor.x - point.x), Math.abs(actor.y - point.y)) <= 5)
  return true
}

const hasCavernPropContext = (floor: Floor, kind: Prop['kind'], point: Point): boolean => {
  if (!kind.startsWith('caverns.')) return true
  const nearWater = hasAdjacentTile(floor, point, new Set<Tile['kind']>(['water']))
  const nearDarkness = hasAdjacentTile(floor, point, new Set<Tile['kind']>(['darkness']))
  if (kind === 'caverns.crystalCluster' || kind === 'caverns.glowingFungus') return nearDarkness
  if (kind === 'caverns.barnacledShrine' || kind === 'caverns.brokenBoat' || kind === 'caverns.eelTunnel') return nearWater
  if (kind === 'caverns.sealedParcel') return nearWater || nearDarkness
  return true
}

const hasPropContext = (floor: Floor, kind: Prop['kind'], point: Point): boolean => hasMinePropContext(floor, kind, point) && hasCavernPropContext(floor, kind, point)

const carveRect = (floor: Floor, room: Room, kind: Tile['kind'] = 'floor'): void => {
  for (let y = room.y; y < room.y + room.h; y++) for (let x = room.x; x < room.x + room.w; x++) setKind(floor, x, y, kind)
}
const wallBand = (floor: Floor, vertical: boolean, at: number, gap: number, span = 2): void => {
  const limit = vertical ? floor.height - 1 : floor.width - 1
  for (let offset = 1; offset < limit; offset++) {
    if (Math.abs(offset - gap) <= span) continue
    setKind(floor, vertical ? at : offset, vertical ? offset : at, 'wall')
  }
}
const landmarkRooms = (floor: Floor): Room[] => {
  const w = Math.max(7, Math.floor(floor.width / 8))
  const h = Math.max(6, Math.floor(floor.height / 7))
  const rooms = [
    { x: 3, y: Math.max(3, Math.floor(floor.height * .18)), w, h },
    { x: Math.floor(floor.width * .27), y: Math.floor(floor.height * .58), w, h },
    { x: Math.floor(floor.width * .52), y: Math.floor(floor.height * .23), w, h },
    { x: floor.width - w - 4, y: Math.floor(floor.height * .62), w, h }
  ]
  rooms.forEach(room => carveRect(floor, room))
  return rooms
}

const carveMineRouteContract = (floor: Floor, rng: Rng): Room[] => {
  const variant = floor.layoutId.replace('-remix', '')
  const mainY = variant === 'collapse-loop' ? 13 : rng.int(12, 15)
  const upperY = variant === 'branching-drifts' ? 3 : 5
  const lowerY = variant === 'rail-spine' ? floor.height - 10 : floor.height - 9
  const rooms: Room[] = [
    { x: 2, y: mainY, w: 6, h: 6 },
    { x: 10, y: mainY, w: 7, h: 6 },
    { x: 20, y: mainY, w: 6, h: 6 },
    { x: 28, y: upperY, w: 7, h: 6 },
    { x: 28, y: lowerY, w: 7, h: 6 },
    { x: 37, y: lowerY, w: 5, h: 5 },
    { x: 36, y: mainY, w: 6, h: 6 },
    { x: 43, y: mainY + 1, w: 3, h: 4 }
  ]
  rooms.forEach(room => carveRect(floor, room))
  const [start, landmark, fork, safeRoute, riskRoute, reward, objective, exit] = rooms.map(center)
  const connect = (from: Point, to: Point, verticalFirst = false) => {
    if (verticalFirst) { carveV(floor, from.y, to.y, from.x); carveH(floor, from.x, to.x, to.y) }
    else { carveH(floor, from.x, to.x, from.y); carveV(floor, from.y, to.y, to.x) }
  }
  connect(start, landmark)
  connect(landmark, fork)
  connect(fork, safeRoute, variant === 'branching-drifts')
  connect(fork, riskRoute, true)
  connect(safeRoute, objective, true)
  connect(riskRoute, objective)
  connect(riskRoute, reward, variant === 'collapse-loop')
  connect(reward, objective, true)
  connect(objective, exit)
  return rooms
}

const carveBiomeLayout = (floor: Floor, rng: Rng): Room[] => {
  if (floor.biome === 'mine') {
    return carveMineRouteContract(floor, rng)
  }
  carveRect(floor, { x: 1, y: 1, w: floor.width - 2, h: floor.height - 2 })
  const rooms = landmarkRooms(floor)
  const variant = floor.layoutId.replace('-remix', '')
  if (floor.biome === 'wilds') {
    if (variant === 'root-maze') for (let x = 11; x < floor.width - 8; x += 10) wallBand(floor, true, x, rng.int(4, floor.height - 5), 2)
    if (variant === 'wetland-causeways') for (let y = 9; y < floor.height - 7; y += 9) wallBand(floor, false, y, rng.int(5, floor.width - 6), 3)
  } else if (floor.biome === 'caverns') {
    for (let x = 10; x < floor.width - 8; x += 11) wallBand(floor, true, x, rng.int(5, floor.height - 6), variant === 'fault-tunnels' ? 1 : 3)
    if (variant === 'sinkhole-galleries') for (let y = 8; y < floor.height - 6; y += 10) wallBand(floor, false, y, rng.int(5, floor.width - 6), 2)
  } else if (floor.biome === 'ruins') {
    for (let x = 9; x < floor.width - 6; x += 9) wallBand(floor, true, x, rng.int(4, floor.height - 5), 2)
    if (variant !== 'breached-precinct') for (let y = 8; y < floor.height - 6; y += 10) wallBand(floor, false, y, rng.int(5, floor.width - 6), 2)
  } else if (floor.biome === 'furnace') {
    for (let y = 7; y < floor.height - 5; y += 7) wallBand(floor, false, y, rng.int(5, floor.width - 6), variant === 'kiln-terraces' ? 2 : 4)
  } else if (floor.biome === 'floodedRuins') {
    for (let x = 12; x < floor.width - 8; x += 14) wallBand(floor, true, x, rng.int(5, floor.height - 6), 4)
  } else if (floor.biome === 'cliffs') {
    for (let y = 8; y < floor.height - 6; y += 9) wallBand(floor, false, y, rng.int(5, floor.width - 6), variant === 'ravine-switchbacks' ? 1 : 3)
  } else if (floor.biome === 'burial') {
    if (variant === 'ringed-necropolis') for (let x = 14; x < floor.width - 10; x += 18) wallBand(floor, true, x, rng.int(6, floor.height - 7), 5)
  } else if (floor.biome === 'saltFlats') {
    if (variant === 'brine-fractures') for (let x = 12; x < floor.width - 8; x += 12) wallBand(floor, true, x, rng.int(5, floor.height - 6), 4)
  } else if (floor.biome === 'frostReliquary') {
    for (let y = 9; y < floor.height - 7; y += 10) wallBand(floor, false, y, rng.int(5, floor.width - 6), variant === 'reliquary-escarpment' ? 2 : 5)
  }
  rooms.forEach(room => carveRect(floor, room))
  connectRooms(floor, rooms)
  imprintBiomeLandmarks(floor, rng, rooms)
  return rooms
}

const carveLegacyLayoutFromRouteContract = (floor: Floor, contract: ReturnType<typeof generateRouteContract>, rng: Rng): Room[] => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`)
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`)
  return carveBiomeLayout(floor, rng)
}

const carveRouteContractLayout = (floor: Floor, contract: ReturnType<typeof generateRouteContract>, macro: MacroRecipeDebug, rng: Rng): Room[] => {
  if (!macroRecipeFor(contract).pilot) return carveLegacyLayoutFromRouteContract(floor, contract, rng)
  if (floor.biome === 'wilds') return carveWildsRouteContractLayout(floor, contract, macro, rng)
  if (floor.biome === 'ruins') return carveRuinsRouteContractLayout(floor, contract, macro, rng)
  if (floor.biome === 'furnace') return carveFurnaceRouteContractLayout(floor, contract, macro, rng)
  const toRoom = (node: MacroRecipeDebug['nodes'][number]): Room => ({ x: node.footprint.x, y: node.footprint.y, w: node.footprint.width, h: node.footprint.height })
  const rooms = macro.nodes.map(toRoom)
  for (const room of rooms) carveRect(floor, room)
  for (const point of macroConnectorPoints(macro)) setKind(floor, point.x, point.y, 'floor')
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]))
  const ordered: RouteNodeKind[] = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit']
  return ordered.map(kind => byKind.get(kind)).filter((room): room is Room => Boolean(room))
}

const carveWildsRouteContractLayout = (floor: Floor, contract: ReturnType<typeof generateRouteContract>, macro: MacroRecipeDebug, rng: Rng): Room[] => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`)
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`)
  carveRect(floor, { x: 1, y: 1, w: floor.width - 2, h: floor.height - 2 })
  const variant = floor.layoutId.replace('-remix', '')
  if (variant === 'root-maze') for (let x = 11; x < floor.width - 8; x += 10) wallBand(floor, true, x, rng.int(4, floor.height - 5), 2)
  if (variant === 'wetland-causeways') for (let y = 9; y < floor.height - 7; y += 9) wallBand(floor, false, y, rng.int(5, floor.width - 6), 3)
  const toRoom = (node: MacroRecipeDebug['nodes'][number]): Room => ({ x: node.footprint.x, y: node.footprint.y, w: node.footprint.width, h: node.footprint.height })
  const rooms = macro.nodes.map(toRoom)
  rooms.forEach(room => carveRect(floor, room))
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'))
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]))
  const ordered: RouteNodeKind[] = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit']
  return ordered.map(kind => byKind.get(kind)).filter((room): room is Room => Boolean(room))
}

const carveRuinsRing = (floor: Floor, center: Point, halfWidth: number, halfHeight: number, gates: readonly Point[]): void => {
  const gateKeys = new Set(gates.map(pointKey))
  for (let x = center.x - halfWidth; x <= center.x + halfWidth; x++) for (const y of [center.y - halfHeight, center.y + halfHeight]) if (!gateKeys.has(pointKey({ x, y }))) setKind(floor, x, y, 'wall')
  for (let y = center.y - halfHeight + 1; y < center.y + halfHeight; y++) for (const x of [center.x - halfWidth, center.x + halfWidth]) if (!gateKeys.has(pointKey({ x, y }))) setKind(floor, x, y, 'wall')
}

const carveRuinsRouteContractLayout = (floor: Floor, contract: ReturnType<typeof generateRouteContract>, macro: MacroRecipeDebug, _rng: Rng): Room[] => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`)
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`)
  carveRect(floor, { x: 1, y: 1, w: floor.width - 2, h: floor.height - 2 })
  const toRoom = (node: MacroRecipeDebug['nodes'][number]): Room => ({ x: node.footprint.x, y: node.footprint.y, w: node.footprint.width, h: node.footprint.height })
  const ritualNode = macro.nodes.find(node => node.kind === 'objective')
  if (!ritualNode) throw new Error(`missing Ruins ritual node: ${macro.recipeId}`)
  const ritual = center(toRoom(ritualNode))
  const variant = floor.layoutId.replace('-remix', '')
  if (variant === 'circular-precinct') carveRuinsRing(floor, ritual, 9, 7, [{ x: ritual.x, y: ritual.y - 7 }, { x: ritual.x + 9, y: ritual.y }, { x: ritual.x, y: ritual.y + 7 }, { x: ritual.x - 9, y: ritual.y }])
  if (variant === 'broken-processional-loop') {
    carveRuinsRing(floor, ritual, 8, 6, [{ x: ritual.x, y: ritual.y - 6 }, { x: ritual.x - 8, y: ritual.y }])
    wallBand(floor, true, ritual.x - 14, ritual.y + 8, 4)
  }
  if (variant === 'courtyard-lattice') {
    wallBand(floor, true, ritual.x - 11, ritual.y - 8, 4)
    wallBand(floor, true, ritual.x + 11, ritual.y + 8, 4)
    wallBand(floor, false, ritual.y, ritual.x, 5)
  }
  const rooms = macro.nodes.map(toRoom)
  rooms.forEach(room => carveRect(floor, room))
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'))
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]))
  const ordered: RouteNodeKind[] = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit']
  return ordered.map(kind => byKind.get(kind)).filter((room): room is Room => Boolean(room))
}

const carveFurnaceTerraces = (floor: Floor, variant: string): void => {
  const levels = [7, 14, 21, 28, 34]
  const gates = variant === 'stepped-kiln-chain' ? [10, 43, 10, 43, 10]
    : variant === 'smoke-choked-service-route' ? [17, 35, 17, 35, 17]
      : [12, 42, 12, 42, 27]
  levels.forEach((y, index) => wallBand(floor, false, y, gates[index], 2))
  if (variant === 'smoke-choked-service-route') wallBand(floor, true, 28, 5, 3)
  if (variant === 'lift-and-ash-loop') wallBand(floor, true, 28, 33, 4)
}

const carveFurnaceRouteContractLayout = (floor: Floor, contract: ReturnType<typeof generateRouteContract>, macro: MacroRecipeDebug, _rng: Rng): Room[] => {
  if (contract.biome !== floor.biome) throw new Error(`route contract ${contract.id} biome does not match floor biome`)
  if (contract.recipeId !== floor.layoutId) throw new Error(`route contract ${contract.id} recipe does not match floor layout`)
  carveRect(floor, { x: 1, y: 1, w: floor.width - 2, h: floor.height - 2 })
  carveFurnaceTerraces(floor, floor.layoutId.replace('-remix', ''))
  const toRoom = (node: MacroRecipeDebug['nodes'][number]): Room => ({ x: node.footprint.x, y: node.footprint.y, w: node.footprint.width, h: node.footprint.height })
  const rooms = macro.nodes.map(toRoom)
  rooms.forEach(room => carveRect(floor, room))
  macroConnectorPoints(macro).forEach(point => setKind(floor, point.x, point.y, 'floor'))
  const byKind = new Map(macro.nodes.map(node => [node.kind, toRoom(node)]))
  const ordered: RouteNodeKind[] = ['start', 'landmark', 'fork', 'optionalReward', 'objective', floor.index % 4 === 3 ? 'boss' : 'exit']
  return ordered.map(kind => byKind.get(kind)).filter((room): room is Room => Boolean(room))
}

const restoreMacroConnectors = (floor: Floor, macro: MacroRecipeDebug, reserved: ReadonlySet<number>): void => {
  if (!reserved.size) return
  for (const point of macroConnectorPoints(macro)) if (reserved.has(indexOf(floor, point.x, point.y))) setKind(floor, point.x, point.y, 'floor')
}

const restoreMacroNodeTransit = (floor: Floor, macro: MacroRecipeDebug): void => {
  for (const node of macro.nodes) {
    const center = { x: node.footprint.x + Math.floor(node.footprint.width / 2), y: node.footprint.y + Math.floor(node.footprint.height / 2) }
    const endpoints = macro.edges.flatMap(edge => [edge.from, edge.to]).filter(point => point.x >= node.footprint.x && point.x < node.footprint.x + node.footprint.width && point.y >= node.footprint.y && point.y < node.footprint.y + node.footprint.height)
    const points = floor.biome === 'caverns'
      ? Array.from({ length: node.footprint.width * node.footprint.height }, (_, index) => ({ x: node.footprint.x + index % node.footprint.width, y: node.footprint.y + Math.floor(index / node.footprint.width) }))
      : endpoints.flatMap(endpoint => {
        const route: Point[] = []
        for (let x = Math.min(center.x, endpoint.x); x <= Math.max(center.x, endpoint.x); x++) route.push({ x, y: center.y })
        for (let y = Math.min(center.y, endpoint.y); y <= Math.max(center.y, endpoint.y); y++) route.push({ x: endpoint.x, y })
        return route
      })
    for (const point of points) if (point.x !== floor.exit.x || point.y !== floor.exit.y) setKind(floor, point.x, point.y, 'floor')
  }
}

const imprintRuinsRitualCenter = (floor: Floor, macro: MacroRecipeDebug): void => {
  if (floor.biome !== 'ruins') return
  const node = macro.nodes.find(candidate => candidate.kind === 'objective')
  if (!node) throw new Error(`missing Ruins ritual center: ${macro.recipeId}`)
  const point = { x: node.footprint.x + Math.floor(node.footprint.width / 2), y: node.footprint.y + Math.floor(node.footprint.height / 2) }
  for (const [x, y] of cardinalOffsets) setKind(floor, point.x + x, point.y + y, 'floor')
  setKind(floor, point.x, point.y, 'altar')
}

const imprintCavernTideRoute = (floor: Floor, macro: MacroRecipeDebug): void => {
  if (floor.biome !== 'caverns') return
  const edge = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'))
  if (!edge) throw new Error(`missing Caverns tide route: ${macro.recipeId}`)
  const cells = edge.cells.slice(3, -3).filter(point => getTile(floor, point.x, point.y)?.kind === 'floor')
  const start = Math.max(0, Math.floor(cells.length / 2) - 3)
  const route = cells.slice(start, start + 7)
  if (route.length < 3) throw new Error(`short Caverns tide route: ${macro.recipeId}`)
  for (let index = 0; index < route.length; index++) {
    const point = route[index]
    const tile = getTile(floor, point.x, point.y)
    if (!tile) continue
    if (index % 2 === 0) setKind(floor, point.x, point.y, 'water')
    else {
      tile.kind = 'current'
      const next = route[index + 1] ?? route[index - 1]
      tile.flow = { direction: flowDirection(next.x - point.x, next.y - point.y) }
    }
  }
}

const imprintWildsPressureRoute = (floor: Floor, macro: MacroRecipeDebug): void => {
  if (floor.biome !== 'wilds') return
  const edge = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'))
  if (!edge) throw new Error(`missing Wilds pressure route: ${macro.recipeId}`)
  const cells = edge.cells.slice(3, -3).filter(point => getTile(floor, point.x, point.y)?.kind === 'floor')
  const start = Math.max(0, Math.floor(cells.length / 2) - 3)
  const route = cells.slice(start, start + 7)
  if (route.length < 3) throw new Error(`short Wilds pressure route: ${macro.recipeId}`)
  for (let index = 0; index < route.length; index++) setKind(floor, route[index].x, route[index].y, index % 2 === 0 ? 'water' : 'web')
}

const imprintRuinsWardRoute = (floor: Floor, macro: MacroRecipeDebug): void => {
  if (floor.biome !== 'ruins') return
  const edge = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'))
  if (!edge) throw new Error(`missing Ruins ward route: ${macro.recipeId}`)
  const cells = edge.cells.slice(3, -3).filter(point => getTile(floor, point.x, point.y)?.kind === 'floor')
  const start = Math.max(0, Math.floor(cells.length / 2) - 3)
  const route = cells.slice(start, start + 7)
  if (route.length < 3) throw new Error(`short Ruins ward route: ${macro.recipeId}`)
  for (let index = 0; index < route.length; index++) if (index % 2 === 0) setKind(floor, route[index].x, route[index].y, 'dart')
}

const imprintFurnaceFiringRoute = (floor: Floor, macro: MacroRecipeDebug): void => {
  if (floor.biome !== 'furnace') return
  const safe = macro.edges.find(candidate => candidate.modes.includes('safe'))
  const costly = macro.edges.find(candidate => candidate.modes.includes('costly') && candidate.modes.includes('optional'))
  if (!safe || !costly) throw new Error(`missing Furnace route pair: ${macro.recipeId}`)
  const transit = (cells: readonly Point[]) => cells.slice(3, -3).filter(point => getTile(floor, point.x, point.y)?.kind === 'floor')
  const liftLane = transit(safe.cells)
  const firingLane = transit(costly.cells)
  if (liftLane.length < 3 || firingLane.length < 4) throw new Error(`short Furnace terrace route: ${macro.recipeId}`)
  for (let index = 0; index < liftLane.length; index++) {
    const point = liftLane[index]
    const tile = getTile(floor, point.x, point.y)!
    tile.elevation = 1
    if (index % 3 === 1) tile.kind = 'lift'
  }
  for (let index = 0; index < firingLane.length; index++) {
    const point = firingLane[index]
    const tile = getTile(floor, point.x, point.y)!
    tile.elevation = 0
    if (index % 3 === 0) tile.kind = 'smoke'
    else if (index % 3 === 1) tile.kind = 'fireVent'
  }
  const lower = firingLane.find(point => getTile(floor, point.x, point.y)?.kind === 'smoke')!
  const upper = liftLane.find(point => getTile(floor, point.x, point.y)?.kind === 'lift')!
  floor.climbLinks = [{ id: `lift:${floor.index}:${lower.x}:${lower.y}:${upper.x}:${upper.y}`, lower, upper, anchored: true }]
}

const imprintCavernSetpieceContext = (floor: Floor, macro: MacroRecipeDebug): void => {
  if (floor.biome !== 'caverns') return
  const node = macro.nodes.find(candidate => candidate.kind === 'optionalReward')
  if (!node) throw new Error(`missing Caverns optional chamber: ${macro.recipeId}`)
  const candidates = Array.from({ length: Math.max(0, node.footprint.width - 2) * Math.max(0, node.footprint.height - 2) }, (_, index) => ({ x: node.footprint.x + 1 + index % (node.footprint.width - 2), y: node.footprint.y + 1 + Math.floor(index / (node.footprint.width - 2)) }))
  const point = candidates.find(candidate => getTile(floor, candidate.x, candidate.y)?.kind === 'floor')
  if (!point) throw new Error(`missing Caverns setpiece floor: ${macro.recipeId}`)
  const nearby = cardinalOffsets.map(([x, y]) => ({ x: point.x + x, y: point.y + y })).filter(candidate => getTile(floor, candidate.x, candidate.y)?.kind === 'floor')
  const darkness = nearby[0]
  if (!darkness) throw new Error(`missing Caverns setpiece darkness: ${macro.recipeId}`)
  const water = nearby[1]
  const pool = nearby[2]
  setKind(floor, darkness.x, darkness.y, 'darkness')
  if (water) setKind(floor, water.x, water.y, 'water')
  if (pool) setKind(floor, pool.x, pool.y, 'deepWater')
  const fungus = propDefinition('caverns.glowingFungus')
  floor.props.push({ id: `prop:${floor.index}:caverns.glowingFungus:${point.x}:${point.y}`, kind: fungus.id, x: point.x, y: point.y, biome: floor.biome, state: 'dormant', tags: [...fungus.tags], hooks: [...fungus.hooks] })
}

const imprintBiomeLandmarks = (floor: Floor, rng: Rng, rooms: readonly Room[]): void => {
  const paint = (x: number, y: number, kind: Tile['kind']) => { if (getTile(floor, x, y)?.kind === 'floor') setKind(floor, x, y, kind) }
  if (floor.biome === 'burial') {
    for (const room of rooms.slice(1)) {
      const origin = center(room)
      for (let y = -3; y <= 3; y++) for (let x = -3; x <= 3; x++) {
        const distance = Math.abs(x) + Math.abs(y)
        if (distance === 3) paint(origin.x + x, origin.y + y, 'cairn')
        else if (distance < 3 && rng.chance(55)) paint(origin.x + x, origin.y + y, 'graveSoil')
      }
    }
  } else if (floor.biome === 'saltFlats') {
    for (let x = 7; x < floor.width - 5; x += 11) for (let y = 2; y < floor.height - 2; y++) if ((y + x) % 7 !== 0) paint(x, y, floor.layoutId.includes('caravan') ? 'saltMirror' : 'brine')
  } else if (floor.biome === 'frostReliquary') {
    const lake = rooms[1] ?? rooms[0]
    for (let y = lake.y + 1; y < lake.y + lake.h - 1; y++) for (let x = lake.x + 1; x < lake.x + lake.w - 1; x++) paint(x, y, floor.layoutId.includes('frozen-lake') ? 'ice' : 'frostRime')
  } else if (floor.biome === 'cliffs') {
    for (let y = 5; y < floor.height - 4; y += 9) for (let x = 2; x < floor.width - 2; x++) if (x % 8 !== 0) paint(x, y, 'ledge')
  } else if (floor.biome === 'furnace') {
    for (let y = 4; y < floor.height - 3; y += 7) for (let x = 3; x < floor.width - 3; x++) if (x % 9 !== 0) paint(x, y, floor.layoutId.includes('kiln') ? 'lift' : 'smoke')
  } else if (floor.biome === 'ruins') {
    for (const room of rooms.slice(1, -1)) {
      const origin = center(room)
      for (const [x, y] of cardinalOffsets) paint(origin.x + x * 2, origin.y + y * 2, 'dart')
    }
  }
}

function connectRooms(floor: Floor, rooms: Room[]): void {
  for (let i = 1; i < rooms.length; i++) {
    const from = center(rooms[i - 1])
    const to = center(rooms[i])
    if (i % 2) { carveH(floor, from.x, to.x, from.y); carveV(floor, from.y, to.y, to.x) }
    else { carveV(floor, from.y, to.y, from.x); carveH(floor, from.x, to.x, to.y) }
  }
}

interface PlacementRuntime { macro: MacroRecipeDebug; pilot: boolean; diagnostics: PlacementDebug[] }
const nativeActorTerrain: Record<Biome, readonly TileKind[]> = {
  mine: ['rail', 'support'], wilds: ['water', 'web'], caverns: ['water', 'current', 'darkness'], ruins: ['altar', 'dart'], furnace: ['smoke', 'lift'], floodedRuins: ['current', 'anchor'], cliffs: ['ledge', 'rope'], burial: ['graveSoil', 'spiritPath'], saltFlats: ['saltMirror', 'brine'], frostReliquary: ['ice', 'frostRime']
}
const placementContext = (floor: Floor, runtime: PlacementRuntime, eligible: (point: Point) => boolean = () => true): PlacementContext => {
  const nodeKinds = new Map<string, RouteNodeKind[]>()
  const edgeModes = new Map<string, RouteEdgeMode[]>()
  if (runtime.pilot) {
    for (const node of runtime.macro.nodes) for (let y = node.footprint.y; y < node.footprint.y + node.footprint.height; y++) for (let x = node.footprint.x; x < node.footprint.x + node.footprint.width; x++) {
      const key = pointKey({ x, y })
      nodeKinds.set(key, [...(nodeKinds.get(key) ?? []), node.kind])
    }
    for (const edge of runtime.macro.edges) for (const point of edge.cells) {
      const key = pointKey(point)
      edgeModes.set(key, [...(edgeModes.get(key) ?? []), ...edge.modes])
    }
  }
  const adjacent = (point: Point): Point[] => cardinalOffsets.map(([x, y]) => ({ x: point.x + x, y: point.y + y })).filter(point => inBounds(floor, point.x, point.y))
  const blocked = (point: Point): boolean => !eligible(point) || floor.props.some(prop => prop.x === point.x && prop.y === point.y) || floor.actors.some(actor => actor.health > 0 && actor.x === point.x && actor.y === point.y) || floor.items.some(item => item.x === point.x && item.y === point.y) || floor.milestones.some(milestone => milestone.x === point.x && milestone.y === point.y) || floor.ecology?.some(ecology => ecology.target.x === point.x && ecology.target.y === point.y) === true
  return {
    points: floor.tiles.map((_, index) => pointAt(floor, index)),
    terrainAt: point => getTile(floor, point.x, point.y)?.kind,
    passableAt: point => Boolean(getTile(floor, point.x, point.y) && isPathPassable(floor, point, true)),
    blockedAt: blocked,
    distanceFromStart: point => distance(point, floor.start),
    visibleAt: point => Boolean(getTile(floor, point.x, point.y)?.visible),
    coveredAt: point => adjacent(point).some(candidate => !passable(getTile(floor, candidate.x, candidate.y)?.kind ?? 'wall') || isBlockingProp(propAt(floor.props, candidate.x, candidate.y))),
    chokepointAt: point => adjacent(point).filter(candidate => isPathPassable(floor, candidate, false)).length <= 2,
    adjacentTerrainAt: point => adjacent(point).map(candidate => getTile(floor, candidate.x, candidate.y)?.kind).filter((kind): kind is TileKind => Boolean(kind)),
    nodeKindsAt: point => nodeKinds.get(pointKey(point)) ?? [],
    edgeModesAt: point => edgeModes.get(pointKey(point)) ?? []
  }
}
const choosePlacement = (floor: Floor, runtime: PlacementRuntime, contract: PlacementContract, eligible?: (point: Point) => boolean): Point | undefined => {
  const reachable = reachableFloorIndexes(floor)
  const selection = selectPlacement(contract, placementContext(floor, runtime, point => reachable.has(indexOf(floor, point.x, point.y)) && (eligible?.(point) ?? true)))
  runtime.diagnostics.push(selection.debug)
  return selection.point
}

function placeProps(floor: Floor, reachable: ReadonlySet<number>, reserved: ReadonlySet<number>, runtime: PlacementRuntime): void {
  const links: Partial<Record<Prop['kind'], Prop['kind']>> = {
    'mine.lanternPost': 'mine.warningMarker',
    'mine.brokenCart': 'mine.discardedParcel',
    'wilds.rootShrine': 'wilds.lostParcel',
    'caverns.barnacledShrine': 'caverns.sealedParcel',
    'ruins.ritualBrazier': 'ruins.sealedCache'
  }
  const companions = new Set(Object.values(links))
  const definitions = [...propDefinitionsFor(floor.biome)].sort((left, right) => Number(companions.has(left.id)) - Number(companions.has(right.id)))
  const anchors = new Map<Prop['kind'], Point>()
  const occupied = new Set<number>([
    indexOf(floor, floor.start.x, floor.start.y),
    indexOf(floor, floor.exit.x, floor.exit.y),
    ...objectiveTargets(floor).map(point => indexOf(floor, point.x, point.y)),
    ...floor.actors.map(actor => indexOf(floor, actor.x, actor.y)),
    ...floor.items.map(item => indexOf(floor, item.x, item.y))
  ])
  for (const definition of definitions) {
    const anchorId = Object.entries(links).find(([, companion]) => companion === definition.id)?.[0] as Prop['kind'] | undefined
    const anchor = anchorId ? anchors.get(anchorId) : undefined
    if (companions.has(definition.id) && !anchor) {
      runtime.diagnostics.push({ id: `prop:${definition.id}`, ranked: 0, usedFallback: false, diagnostics: [`placement prop:${definition.id}: linked setpiece anchor is unavailable`], requirements: { terrain: [...definition.terrain] } })
      continue
    }
    const tags = new Set(definition.tags)
    const requirements: PlacementContract = {
      id: `prop:${definition.id}`,
      requirements: {
        terrain: [...definition.terrain],
        minDistance: tags.has('cache') ? 8 : 5,
        ...(runtime.pilot && tags.has('route') ? { edgeModes: ['main'] as RouteEdgeMode[] } : {}),
        ...(runtime.pilot && tags.has('cache') ? { nodeKinds: ['optionalReward'] as RouteNodeKind[] } : {}),
        ...(anchor ? { near: anchor, nearDistance: 10 } : {})
      }
    }
    const point = choosePlacement(floor, runtime, requirements, candidate => reachable.has(indexOf(floor, candidate.x, candidate.y)) && !reserved.has(indexOf(floor, candidate.x, candidate.y)) && !occupied.has(indexOf(floor, candidate.x, candidate.y)) && hasPropContext(floor, definition.id, candidate))
    if (!point) continue
    const prop: Prop = {
      id: `prop:${floor.index}:${definition.id}:${point.x}:${point.y}`,
      kind: definition.id,
      x: point.x,
      y: point.y,
      biome: floor.biome,
      state: 'dormant',
      tags: [...definition.tags],
      hooks: [...definition.hooks]
    }
    if (isBlockingProp(prop)) {
      floor.props.push(prop)
      const keepsExitReachable = hasPassablePath(floor, floor.start, floor.exit)
      const keepsObjectiveReachable = objectiveTargets(floor).some(target => canReachObjectiveWithProps(floor, target))
      floor.props.pop()
      if (!keepsExitReachable || !keepsObjectiveReachable) {
        const debug = runtime.diagnostics.at(-1)
        if (debug) { delete debug.selected; debug.diagnostics.push(`placement prop:${definition.id}: rejected because it blocks mandatory pathing`) }
        continue
      }
    }
    floor.props.push(prop)
    occupied.add(indexOf(floor, point.x, point.y))
    if (links[definition.id]) anchors.set(definition.id, point)
  }
}

function placeMilestones(floor: Floor, runtime: PlacementRuntime): void {
  const specs = [
    { id: 'waycache', kind: 'waycache' as const, rewardKey: 'waycache' as const, primary: { nodeKinds: ['landmark'] as RouteNodeKind[], minDistance: 5 }, legacy: { minDistance: 5, maxDistance: 18 } },
    { id: 'boon-teach', kind: 'boon' as const, rewardKey: 'boon-teach' as const, primary: { nodeKinds: ['fork'] as RouteNodeKind[], minDistance: 7 }, legacy: { minDistance: 7, maxDistance: 24 } },
    { id: 'boon-test', kind: 'boon' as const, rewardKey: 'boon-test' as const, primary: { edgeModes: ['costly'] as RouteEdgeMode[], routeCost: 'costly' as const, minDistance: 9 }, legacy: { minDistance: 10, chokepoint: false } },
    { id: 'boon-payoff', kind: 'boon' as const, rewardKey: 'boon-payoff' as const, primary: { nodeKinds: ['optionalReward'] as RouteNodeKind[], minDistance: 10 }, legacy: { minDistance: 12 } },
    { id: 'augment', kind: 'augment' as const, primary: { nodeKinds: ['objective'] as RouteNodeKind[], minDistance: 12 }, legacy: { minDistance: 14 } }
  ]
  floor.milestones = []
  for (const spec of specs) {
    const contract: PlacementContract = { id: `milestone:${spec.id}`, requirements: runtime.pilot ? spec.primary : spec.legacy, ...(runtime.pilot ? { fallback: spec.legacy } : {}) }
    const point = choosePlacement(floor, runtime, contract, candidate => candidate.x !== floor.exit.x || candidate.y !== floor.exit.y)
    if (!point) throw new Error(`failed placement ${contract.id}: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
    floor.milestones.push({ id: `milestone:${floor.index}:${spec.id}:${point.x}:${point.y}`, kind: spec.kind, ...(spec.rewardKey ? { rewardKey: spec.rewardKey } : {}), ...point, discovered: false, claimed: false })
  }
}

function placeEncounters(floor: Floor, rng: Rng, runtime: PlacementRuntime): void {
  const contract: PlacementContract = runtime.pilot
    ? { id: 'encounter:guarded-shrine', requirements: { edgeModes: ['costly'], routeCost: 'costly', minDistance: 8, cover: true }, fallback: { minDistance: 8, terrain: ['floor'] } }
    : { id: 'encounter:guarded-shrine', requirements: { minDistance: 8, terrain: ['floor'] } }
  const point = choosePlacement(floor, runtime, contract, candidate => candidate.x !== floor.exit.x || candidate.y !== floor.exit.y)
  if (!point) return
  const aligned: Record<Biome, readonly FloorEncounter['kind'][]> = {
    mine: ['minePact', 'mineKami'], wilds: ['wildsPact', 'wildsKami'], caverns: ['cavernsPact', 'cavernsKami'], ruins: ['ruinsPact', 'ruinsKami'], furnace: ['furnacePact', 'furnaceKami'], floodedRuins: ['floodedPact', 'floodedKami'], cliffs: ['cliffsPact', 'cliffsKami'], burial: ['burialPact', 'burialKami'], saltFlats: ['saltPact', 'saltKami'], frostReliquary: ['frostPact', 'frostKami']
  }
  const kinds: FloorEncounter['kind'][] = floor.biome === 'cliffs' ? ['stormCache', 'windTrial', 'cursedObject', ...aligned.cliffs]
    : floor.biome === 'burial' ? ['ancestorDebt', 'tombAuction', 'cursedObject', ...aligned.burial]
      : floor.biome === 'saltFlats' ? ['sunTribute', 'mirageMarket', 'brineOath', 'glassTrial', 'whiteRoad', 'saltCache', ...aligned.saltFlats]
        : floor.biome === 'frostReliquary' ? ['iceDuel', 'winterTithe', 'rimeContract', 'frostCache', 'whiteout', 'reliquaryTrial', ...aligned.frostReliquary]
          : ['wayfarer', 'bloodBargain', 'shiftingChamber', 'oathwell', 'cursedObject', ...aligned[floor.biome]]
  const social = socialContractFor({ seed: floor.seed, floorIndex: floor.index, biome: floor.biome, recipeId: floor.layoutId, arcId: floor.escalation?.arcId })
  floor.encounters = [{ id: `encounter:${floor.index}:${point.x}:${point.y}`, kind: social ? 'wayfarer' : rng.pick(kinds), ...point, state: 'dormant', ...(social ? { social } : {}) }]
}

const carveH = (floor: Floor, from: number, to: number, y: number) => { for (let x = Math.min(from, to); x <= Math.max(from, to); x++) setKind(floor, x, y, 'floor') }
const carveV = (floor: Floor, from: number, to: number, x: number) => { for (let y = Math.min(from, to); y <= Math.max(from, to); y++) setKind(floor, x, y, 'floor') }
const setKind = (floor: Floor, x: number, y: number, kind: Tile['kind']) => {
  if (!inBounds(floor, x, y)) return
  const tile = floor.tiles[indexOf(floor, x, y)]
  tile.kind = kind
  if (kind !== 'current') delete tile.flow
}
const safeFloor = (floor: Floor): Point[] => floor.tiles.flatMap((current, index) => current.kind === 'floor' ? [pointAt(floor, index)] : []).filter(point => distance(point, floor.start) > 5 && distance(point, floor.exit) > 3)
const terrainCount = (floor: Floor, count: number): number => Math.max(count, Math.round(count * floor.tiles.length / (MAP_WIDTH * MAP_HEIGHT)))
const railH = (floor: Floor, from: number, to: number, y: number) => { for (let x = Math.min(from, to); x <= Math.max(from, to); x++) if (getTile(floor, x, y)?.kind === 'floor') setKind(floor, x, y, 'rail') }
const railV = (floor: Floor, from: number, to: number, x: number) => { for (let y = Math.min(from, to); y <= Math.max(from, to); y++) if (getTile(floor, x, y)?.kind === 'floor') setKind(floor, x, y, 'rail') }

function decorateBiome(floor: Floor, rng: Rng, rooms: Room[]): void {
  if (floor.biome === 'mine') decorateMine(floor, rng, rooms)
  if (floor.biome === 'wilds') decorateWilds(floor, rng)
  if (floor.biome === 'caverns') decorateCaverns(floor, rng)
  if (floor.biome === 'ruins') decorateRuins(floor, rng, rooms)
  if (floor.biome === 'furnace') decorateFurnace(floor, rng)
  if (floor.biome === 'floodedRuins') decorateFloodedRuins(floor, rng)
  if (floor.biome === 'cliffs') decorateCliffs(floor, rng)
  if (floor.biome === 'burial') decorateBurial(floor, rng)
  if (floor.biome === 'saltFlats') decorateSaltFlats(floor, rng)
  if (floor.biome === 'frostReliquary') decorateFrostReliquary(floor, rng)
  if (floor.index % 4 === 3) {
    const chamber = rooms[rooms.length - 1]
    for (let y = chamber.y; y < chamber.y + chamber.h; y++) for (let x = chamber.x; x < chamber.x + chamber.w; x++) setKind(floor, x, y, 'floor')
    setKind(floor, floor.exit.x, floor.exit.y, 'exit')
  }
}

function placePuzzleTemplate(floor: Floor, rng: Rng, rooms: Room[]): void {
  const templates = puzzleTemplatesFor(floor.biome)
  if (!templates.length) return
  const template = rng.pick(templates)
  const room = rng.pick(rooms.slice(1, -1).length ? rooms.slice(1, -1) : rooms)
  const point = center(room)
  for (const placement of template.placements) {
    const tile = getTile(floor, point.x + placement.dx, point.y + placement.dy)
    if (tile && tile.kind !== 'wall' && tile.kind !== 'exit') { tile.kind = placement.kind; if (placement.kind !== 'current') delete tile.flow }
  }
  floor.puzzleIds = [...(floor.puzzleIds ?? []), template.id]
}

function decorateMine(floor: Floor, rng: Rng, rooms: Room[]): void {
  for (let i = 1; i < rooms.length; i++) {
    const from = center(rooms[i - 1])
    const to = center(rooms[i])
    if (i % 2) { railH(floor, from.x, to.x, from.y); railV(floor, from.y, to.y, to.x) }
    else { railV(floor, from.y, to.y, from.x); railH(floor, from.x, to.x, to.y) }
  }
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number, byRail = false) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    if (byRail) {
      const adjacent = candidates.filter(point => [[0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => getTile(floor, point.x + x, point.y + y)?.kind === 'rail'))
      if (adjacent.length) candidates = adjacent
    }
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      candidates = candidates.filter(candidate => candidate.x !== point.x || candidate.y !== point.y)
    }
  }
  paint('support', 8, true)
  paint('crumble', 12)
  paint('rubble', 6)
  paint('boulder', 5)
}

function decorateWilds(floor: Floor, rng: Rng): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) if (rng.chance(45) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  paint('water', 9, true)
  if (floor.layoutId.includes('river-clearings') || floor.layoutId.includes('wetland')) carveFlowChannel(floor, rng, false)
  paint('bramble', 8, true)
  paint('web', 10)
}

function decorateCaverns(floor: Floor, rng: Rng): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) if (rng.chance(40) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  carveFlowChannel(floor, rng, false)
  paint('water', 11, true)
  paint('deepWater', 7, true)
  paint('crumble', 6)
  paint('darkness', 11, true)
}

function decorateRuins(floor: Floor, rng: Rng, rooms: Room[]): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      candidates = safe()
    }
  }
  paint('dart', 12)
  paint('crumble', 10)
  paint('boulder', 5)
  const ritualRoom = rooms.length > 2 ? rooms[rooms.length - 2] : undefined
  if (!ritualRoom) return
  const altar = center(ritualRoom)
  for (let y = altar.y - 1; y <= altar.y + 1; y++) for (let x = altar.x - 1; x <= altar.x + 1; x++) if (getTile(floor, x, y)?.kind !== 'wall') setKind(floor, x, y, 'floor')
  setKind(floor, altar.x, altar.y, 'altar')
}

function decorateFurnace(floor: Floor, rng: Rng): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of cardinalOffsets) if (rng.chance(35) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  paint('smoke', 14, true)
  paint('lift', 7)
  paint('breakwall', 8)
  paint('fireVent', 9)
}

function decorateFloodedRuins(floor: Floor, rng: Rng): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of cardinalOffsets) if (rng.chance(35) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  carveFlowChannel(floor, rng, floor.layoutId.includes('floodgate'))
  paint('anchor', 7)
  paint('deepWater', 9, true)
  paint('water', 8, true)
}

const carveFlowChannel = (floor: Floor, rng: Rng, hazardous: boolean): void => {
  const vertical = rng.chance(55)
  const start = vertical ? rng.int(4, floor.width - 5) : rng.int(4, floor.height - 5)
  const direction: Exclude<Direction, 'wait'> = vertical ? 's' : 'e'
  const length = vertical ? floor.height - 3 : floor.width - 3
  let bend = start
  let last: Point | undefined
  for (let step = 1; step < length; step++) {
    if (step % 8 === 0 && rng.chance(55)) bend += rng.int(-1, 1)
    const x = vertical ? bend : step
    const y = vertical ? step : bend
    if (!inBounds(floor, x, y) || x < 2 || y < 2 || x >= floor.width - 2 || y >= floor.height - 2) continue
    const current = getTile(floor, x, y)
    if (!current || current.kind === 'exit' || (x === floor.start.x && y === floor.start.y)) continue
    if (last) {
      const previous = getTile(floor, last.x, last.y)
      if (previous?.flow) previous.flow.direction = flowDirection(x - last.x, y - last.y)
    }
    current.kind = 'current'
    current.flow = { direction, ...(hazardous && step > length - 7 ? { hazard: 'undertow' as const } : {}) }
    last = { x, y }
    const bank = vertical ? { x: x + 1, y } : { x, y: y + 1 }
    if (getTile(floor, bank.x, bank.y)?.kind === 'floor' && rng.chance(45)) setKind(floor, bank.x, bank.y, 'water')
  }
  if (hazardous && last) {
    const delta = ({ n: { x: 0, y: -1 }, ne: { x: 1, y: -1 }, e: { x: 1, y: 0 }, se: { x: 1, y: 1 }, s: { x: 0, y: 1 }, sw: { x: -1, y: 1 }, w: { x: -1, y: 0 }, nw: { x: -1, y: -1 } } as const)[getTile(floor, last.x, last.y)?.flow?.direction ?? direction]
    const outlet = getTile(floor, last.x + delta.x, last.y + delta.y)
    if (outlet && outlet.kind !== 'exit') outlet.kind = 'deepWater'
  }
}

const flowDirection = (x: number, y: number): Exclude<Direction, 'wait'> => x < 0 ? y < 0 ? 'nw' : y > 0 ? 'sw' : 'w' : x > 0 ? y < 0 ? 'ne' : y > 0 ? 'se' : 'e' : y < 0 ? 'n' : 's'

function decorateCliffs(floor: Floor, rng: Rng): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      candidates = safe()
    }
  }
  paint('ledge', 12)
  paint('cliffWall', 10)
  paint('rope', 4)
  const lower = safe()
  if (!lower.length) return
  const from = rng.pick(lower)
  const far = lower.filter(point => distance(point, from) > 8)
  const to = rng.pick(far.length ? far : lower)
  getTile(floor, from.x, from.y)!.elevation = 0
  getTile(floor, to.x, to.y)!.elevation = 1
  floor.climbLinks = [{ id: `climb:${floor.index}:${from.x}:${from.y}:${to.x}:${to.y}`, lower: from, upper: to, anchored: false }]
}

function decorateBurial(floor: Floor, rng: Rng): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of cardinalOffsets) if (rng.chance(35) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  paint('graveSoil', 13, true)
  paint('cairn', 8)
  paint('ossuary', 7)
  paint('spiritPath', 10, true)
}

function decorateSaltFlats(floor: Floor, rng: Rng): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of cardinalOffsets) if (rng.chance(35) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  paint('saltMirror', 14, true)
  paint('brine', 8, true)
  paint('crumble', 6)
}

function decorateFrostReliquary(floor: Floor, rng: Rng): void {
  const safe = () => safeFloor(floor)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    count = terrainCount(floor, count)
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of cardinalOffsets) if (rng.chance(35) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  paint('ice', 15, true)
  paint('frostRime', 9, true)
  paint('boulder', 4)
}

function placeEvents(floor: Floor, rooms: Room[], runtime: PlacementRuntime): void {
  const eventRoom = rooms[floor.biome === 'ruins' && rooms.length > 2 ? 1 : Math.max(1, Math.floor(rooms.length / 2))]
  const nodeKinds: RouteNodeKind[] = floor.biome === 'ruins' ? floor.index % 4 === 0 ? ['landmark'] : floor.index % 4 === 1 ? ['fork'] : floor.index % 4 === 2 ? ['optionalReward'] : ['landmark'] : floor.index % 4 === 0 ? ['landmark'] : floor.index % 4 === 1 ? ['fork'] : ['objective']
  const node = runtime.pilot ? runtime.macro.nodes.find(candidate => nodeKinds.includes(candidate.kind)) : undefined
  const fallback = node ? { x: node.footprint.x + Math.floor(node.footprint.width / 2), y: node.footprint.y + Math.floor(node.footprint.height / 2) } : center(eventRoom)
  const point = choosePlacement(floor, runtime, { id: `event:${floor.index}`, requirements: runtime.pilot ? { nodeKinds, minDistance: 5, near: fallback, nearDistance: 0 } : { near: fallback, nearDistance: 3 }, ...(runtime.pilot ? { fallback: { near: fallback, nearDistance: 3 } } : {}) })
  if (!point) throw new Error(`failed placement event:${floor.index}: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
  const kind: Tile['kind'] = floor.index % 4 === 0 ? 'shop' : floor.index % 4 === 1 ? 'rescue' : floor.index % 4 === 2 ? 'altar' : 'shop'
  setKind(floor, point.x, point.y, kind)
  if (kind === 'shop') floor.actors.push(friendly('merchant', `${floor.biome} trader`, point, '$', '#f4d26a'))
  if (kind === 'rescue') floor.actors.push(friendly('ally', 'stranded traveler', point, '&', '#8ae0b3'))
  if (kind === 'altar') floor.actors.push(friendly('ally', 'shrine keeper', point, '_', '#d6a8eb'))
}

function placeDoorsAndLocks(floor: Floor, rng: Rng, rooms: Room[]): void {
  let placedRuinsLock = false
  for (const room of rooms.slice(1, -1)) {
    const point = center(room)
    const door = { x: Math.max(1, point.x - Math.floor(room.w / 2)), y: point.y }
    if (getTile(floor, door.x, door.y)?.kind === 'floor') {
      const locked = floor.biome === 'ruins' ? !placedRuinsLock || rng.chance(65) : rng.chance(25)
      setKind(floor, door.x, door.y, locked ? 'lockedDoor' : 'door')
      if (locked) placedRuinsLock = true
    }
  }
}

const routeThroughLocks = (floor: Floor): Point[] | undefined => {
  const start = { ...floor.start }
  const queue = [start]
  const previous = new Map<string, Point | undefined>([[pointKey(start), undefined]])
  const traversable = (kind: Tile['kind']) => kind !== 'wall' && kind !== 'cliffWall'
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const point = queue[cursor]
    if (point.x === floor.exit.x && point.y === floor.exit.y) {
      const path: Point[] = []
      for (let current: Point | undefined = point; current; current = previous.get(pointKey(current))) path.push(current)
      return path.reverse()
    }
    for (const [x, y] of cardinalOffsets) {
      const next = { x: point.x + x, y: point.y + y }
      const key = pointKey(next)
      if (previous.has(key) || !traversable(getTile(floor, next.x, next.y)?.kind ?? 'wall')) continue
      previous.set(key, point)
      queue.push(next)
    }
  }
  return undefined
}

const openMandatoryLocks = (floor: Floor): void => {
  if (hasPassablePath(floor, floor.start, floor.exit)) return
  const route = routeThroughLocks(floor)
  if (!route) return
  for (const point of route) {
    const tile = getTile(floor, point.x, point.y)
    if (tile?.kind === 'lockedDoor') tile.kind = 'door'
  }
}

const repairMandatoryPath = (floor: Floor): void => {
  if (hasPassablePath(floor, floor.start, floor.exit)) return
  openMandatoryLocks(floor)
  if (hasPassablePath(floor, floor.start, floor.exit)) return
  const trace = traverseFloor(floor, floor.start, { target: floor.exit })
  throw new Error(`bounded route repair refused to flatten terrain: blockers=${trace.blockers.join('|')}`)
}

function placeContainers(floor: Floor, rng: Rng, rooms: Room[], reserved: ReadonlySet<number> = new Set()): void {
  for (let i = 0; i < 4; i++) {
    const kind: Tile['kind'] = i === 3 ? 'chest' : 'crate'
    for (let attempt = 0; attempt < 80; attempt++) {
      const point = freeRoomPoint(floor, rng, rooms, reserved)
      const target = getTile(floor, point.x, point.y)
      const exits = cardinalOffsets.filter(([x, y]) => passable(getTile(floor, point.x + x, point.y + y)?.kind ?? 'wall')).length
      if (!target || target.kind !== 'floor' || exits < 2) continue
      target.kind = kind
      break
    }
  }
}

function placeActors(floor: Floor, rng: Rng, runtime: PlacementRuntime): void {
  const definitions = MONSTERS.filter(monster => monster.biome === floor.biome)
  const regular = definitions.filter(monster => monster.ai !== 'guardian' && monster.spawn !== 'triggered')
  const areaFloor = floor.index % 4
  const routePosition = floor.difficulty?.routePosition ?? 0
  const directed: NonNullable<Actor['encounter']>[] = []
  if (areaFloor !== 3) for (const [groupIndex, plan] of encounterPlansFor({ biome: floor.biome, areaFloor, routePosition, pilot: runtime.pilot, arcOffset: floor.escalation?.encounterOffset }, nativeActorTerrain[floor.biome]).entries()) {
    const id = `tactical:${floor.index}:${groupIndex}:${plan.archetype}`
    let leader: Point | undefined
    for (let member = 0; member < membersForEncounter(areaFloor, routePosition); member++) {
      if (member > 0 && !leader) break
      const definition = definitionForEncounter(regular, plan, member, nativeActorTerrain[floor.biome])
      if (!definition) continue
      const affinity = plan.archetype === 'nativeTerrainPack' && member === 0 ? terrainAffinityFor(definition).filter(kind => nativeActorTerrain[floor.biome].includes(kind)) : undefined
      const requirements = member === 0 ? affinity?.length ? { ...plan.requirements, terrain: affinity } : plan.requirements : { minDistance: 7, chokepoint: false, near: leader!, nearDistance: 5 }
      const fallback = member === 0 ? affinity?.length ? { ...plan.fallback, terrain: affinity } : plan.fallback : { minDistance: 7, chokepoint: false, near: leader!, nearDistance: 8 }
      const contract: PlacementContract = { id: `actor:${id}:${member}:${definition.id}`, requirements, fallback }
      const point = choosePlacement(floor, runtime, contract, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y))
      if (!point) continue
      const encounter = { id, archetype: plan.archetype, leader: member === 0, answer: plan.answer }
      const actor = spawnMonster(definition.id, point, `${definition.id}-${groupIndex}-${member}`, floor.difficulty)
      actor.encounter = encounter
      if (member === 0 && (rng.chance(floor.difficulty?.eliteChance ?? 0) || (floor.biome === 'frostReliquary' && areaFloor >= 1))) {
        actor.maxHealth = Math.round(actor.maxHealth * 1.25)
        actor.health = actor.maxHealth
        actor.attack += 2
        actor.status = [...(actor.status ?? []), 'elite']
      }
      floor.actors.push(actor)
      directed.push(encounter)
      leader ??= point
    }
  }
  if (floor.biome === 'mine' && areaFloor !== 3 && !floor.actors.some(actor => actor.hostile && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))) {
    const railguard = definitions.find(definition => definition.id === 'railguard')
    const terrain = railguard ? terrainAffinityFor(railguard).filter(kind => nativeActorTerrain.mine.includes(kind)) : []
    if (!railguard || !terrain.length) throw new Error('Mine rail patrol lacks terrain affinity')
    const point = choosePlacement(floor, runtime, { id: `actor:tactical:${floor.index}:rail-patrol:railguard`, requirements: { terrain, minDistance: 8 } }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y))
    if (!point) throw new Error(`failed placement Mine rail patrol: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
    const encounter = { id: `tactical:${floor.index}:rail-patrol`, archetype: 'nativeTerrainPack' as const, leader: true, answer: 'leave the enemy\'s native terrain' }
    const actor = spawnMonster(railguard.id, point, `${railguard.id}-rail-patrol`, floor.difficulty)
    actor.encounter = encounter
    floor.actors.push(actor)
    directed.push(encounter)
  }
  if (floor.biome === 'caverns' && areaFloor !== 3 && !floor.actors.some(actor => actor.hostile && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))) {
    const tideEel = definitions.find(definition => definition.id === 'fumeeel')
    const terrain = tideEel ? terrainAffinityFor(tideEel).filter(kind => nativeActorTerrain.caverns.includes(kind)) : []
    if (!tideEel || !terrain.length) throw new Error('Caverns tide patrol lacks terrain affinity')
    const point = choosePlacement(floor, runtime, { id: `actor:tactical:${floor.index}:tide-patrol:fumeeel`, requirements: { terrain, minDistance: 8 } }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y))
    if (!point) throw new Error(`failed placement Caverns tide patrol: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
    const encounter = { id: `tactical:${floor.index}:tide-patrol`, archetype: 'nativeTerrainPack' as const, leader: true, answer: 'leave the enemy\'s native terrain' }
    const actor = spawnMonster(tideEel.id, point, `${tideEel.id}-tide-patrol`, floor.difficulty)
    actor.encounter = encounter
    floor.actors.push(actor)
    directed.push(encounter)
  }
  if (floor.biome === 'wilds' && areaFloor !== 3 && !floor.actors.some(actor => actor.hostile && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))) {
    const webweaver = definitions.find(definition => definition.id === 'webweaver')
    const terrain = webweaver ? terrainAffinityFor(webweaver).filter(kind => nativeActorTerrain.wilds.includes(kind)) : []
    if (!webweaver || !terrain.length) throw new Error('Wilds web ambush lacks terrain affinity')
    const point = choosePlacement(floor, runtime, { id: `actor:tactical:${floor.index}:web-ambush:webweaver`, requirements: { terrain, minDistance: 8 } }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y))
    if (!point) throw new Error(`failed placement Wilds web ambush: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
    const encounter = { id: `tactical:${floor.index}:web-ambush`, archetype: 'nativeTerrainPack' as const, leader: true, answer: 'leave the enemy\'s native terrain' }
    const actor = spawnMonster(webweaver.id, point, `${webweaver.id}-web-ambush`, floor.difficulty)
    actor.encounter = encounter
    floor.actors.push(actor)
    directed.push(encounter)
  }
  if (floor.biome === 'ruins' && areaFloor !== 3 && !floor.actors.some(actor => actor.kind === 'wardacolyte')) {
    const acolyte = definitions.find(definition => definition.id === 'wardacolyte')
    const terrain = acolyte ? terrainAffinityFor(acolyte).filter(kind => nativeActorTerrain.ruins.includes(kind)) : []
    if (!acolyte || !terrain.length) throw new Error('Ruins ward post lacks terrain affinity')
    const point = choosePlacement(floor, runtime, { id: `actor:tactical:${floor.index}:ward-post:wardacolyte`, requirements: { terrain, minDistance: 8 }, fallback: { terrain: ['floor'], minDistance: 8 } }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y))
    if (!point) throw new Error(`failed placement Ruins ward post: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
    const encounter = { id: `tactical:${floor.index}:ward-post`, archetype: 'objectiveDefense' as const, leader: true, answer: 'break line of sight and interrupt the ward' }
    const actor = spawnMonster(acolyte.id, point, `${acolyte.id}-ward-post`, floor.difficulty)
    actor.encounter = encounter
    floor.actors.push(actor)
    directed.push(encounter)
  }
  if (floor.biome === 'furnace' && areaFloor !== 3 && !floor.actors.some(actor => actor.kind === 'cinderling' && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))) {
    const cinderling = definitions.find(definition => definition.id === 'cinderling')
    const terrain = cinderling ? terrainAffinityFor(cinderling).filter(kind => kind === 'fireVent') : []
    if (!cinderling || !terrain.length) throw new Error('Furnace firing post lacks heat affinity')
    const point = choosePlacement(floor, runtime, { id: `actor:tactical:${floor.index}:firing-post:cinderling`, requirements: { terrain, minDistance: 8 }, fallback: { terrain, minDistance: 5 } }, candidate => (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y) && (candidate.x !== floor.start.x || candidate.y !== floor.start.y))
    if (!point) throw new Error(`failed placement Furnace firing post: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
    const encounter = { id: `tactical:${floor.index}:firing-post`, archetype: 'objectiveDefense' as const, leader: true, answer: 'take the lift lane and quench the firing vent' }
    const actor = spawnMonster(cinderling.id, point, `${cinderling.id}-firing-post`, floor.difficulty)
    actor.encounter = encounter
    floor.actors.push(actor)
    directed.push(encounter)
  }
  if (floor.index % 4 === 3) {
    const guardian = definitions.find(monster => monster.ai === 'guardian')!
    const actor = spawnMonster(guardian.id, floor.exit, `${guardian.id}-99`, floor.difficulty)
    actor.status = [...(actor.status ?? []), `arc:${floor.escalation?.arcId ?? 'legacy'}:resolved`]
    floor.actors.push(actor)
  }
  tacticalEncounterDebugs.set(floor, directed)
}

function placeEcology(floor: Floor, runtime: PlacementRuntime): void {
  const profile = ecologyProfileFor(floor.biome)
  const pressureRoute = (floor.biome === 'caverns' || floor.biome === 'wilds' || floor.biome === 'ruins' || floor.biome === 'furnace') && runtime.pilot
  const contract: PlacementContract = pressureRoute
    ? { id: `ecology:${profile.kind}`, requirements: { terrain: floor.biome === 'ruins' ? ['dart'] : floor.biome === 'furnace' ? ['smoke'] : ['water'], edgeModes: ['costly', 'optional'], optional: true, minDistance: 7 }, fallback: { terrain: floor.biome === 'ruins' ? ['dart'] : floor.biome === 'furnace' ? ['smoke'] : ['water'], minDistance: 7 } }
    : { id: `ecology:${profile.kind}`, requirements: { terrain: profile.terrain, minDistance: 7, chokepoint: false }, fallback: { terrain: ['floor'], minDistance: 7, chokepoint: false } }
  const point = choosePlacement(floor, runtime, contract, candidate => (candidate.x !== floor.start.x || candidate.y !== floor.start.y) && (candidate.x !== floor.exit.x || candidate.y !== floor.exit.y))
  if (!point) throw new Error(`failed placement ${contract.id}: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
  const source = floor.actors.find(actor => actor.hostile && (actor.combatRole === 'guard' || actor.combatRole === 'pursuer'))
  const node = runtime.pilot ? runtime.macro.nodes.find(candidate => point.x >= candidate.footprint.x && point.x < candidate.footprint.x + candidate.footprint.width && point.y >= candidate.footprint.y && point.y < candidate.footprint.y + candidate.footprint.height)?.nodeId : undefined
  const route = runtime.pilot ? runtime.macro.edges.find(edge => edge.cells.some(cell => cell.x === point.x && cell.y === point.y))?.modes[0] : undefined
  const ecology = ecologyEventFor(floor, point, source?.id ?? `${floor.biome}:${profile.kind}`, node, route)
  if (floor.escalation) {
    ecology.warning = `${floor.escalation.ecology}: ${ecology.warning}`
    ecology.responses = [`${floor.escalation.phase}: ${floor.escalation.promise}`, ...ecology.responses]
    if (floor.escalation.phase === 'climax') { ecology.startsAt = Math.max(2, ecology.startsAt - 1); ecology.duration++ }
  }
  floor.ecology = [ecology]
}

function placeItems(floor: Floor, rng: Rng, _rooms: Room[], runtime: PlacementRuntime): void {
  const valueCap = 105 + (floor.difficulty?.threat ?? 0) * 14
  const eligible = ITEMS.filter(item => item.findable !== false && item.value <= valueCap && (!item.slot || rng.chance(30 + (floor.difficulty?.routePosition ?? 0) * 8)))
  const loot = eligible.length ? eligible : ITEMS.filter(item => item.findable !== false && (!item.slot || rng.chance(30)))
  const count = 10 + floor.index % 4 * 2 + Math.floor((floor.difficulty?.routePosition ?? 0) / 2)
  for (let i = 0; i < count; i++) {
    const contract: PlacementContract = runtime.pilot && i > 0
      ? { id: `loot:${i}`, requirements: { nodeKinds: ['optionalReward'], minDistance: 7 }, fallback: { terrain: ['floor'], minDistance: 4 } }
      : { id: `loot:${i}`, requirements: { terrain: ['floor'], minDistance: 4 } }
    const point = choosePlacement(floor, runtime, contract, candidate => candidate.x !== floor.exit.x || candidate.y !== floor.exit.y)
    if (!point) throw new Error(`failed placement ${contract.id}: ${runtime.diagnostics.at(-1)?.diagnostics.join('; ')}`)
    const id = i === 0 && floor.index % 4 === 0 ? 'key' : rng.pick(loot).id
    floor.items.push({ id, x: point.x, y: point.y, count: 1 })
  }
}

function freeRoomPoint(floor: Floor, rng: Rng, rooms: Room[], reserved: ReadonlySet<number> = new Set()): Point {
  for (let tries = 0; tries < 200; tries++) {
    const room = rng.pick(rooms)
    const point = { x: rng.int(room.x + 1, room.x + room.w - 2), y: rng.int(room.y + 1, room.y + room.h - 2) }
    const current = getTile(floor, point.x, point.y)
    if (current?.kind === 'floor' && !reserved.has(indexOf(floor, point.x, point.y)) && !actorAt(floor, point.x, point.y) && !floor.items.some(item => item.x === point.x && item.y === point.y) && distance(point, floor.start) > 4) return point
  }
  return { ...floor.start }
}

export const spawnMonster = (kind: string, point: Point, id: string, difficulty?: DifficultyContext): Actor => {
  const definition = monsterById(kind)
  if (!definition) throw new Error(`unknown monster: ${kind}`)
  const healthMultiplier = definition.ai === 'guardian' ? 1 + (difficulty?.threat ?? 0) * 0.08 : difficulty?.healthMultiplier ?? 1
  const maxHealth = Math.max(1, Math.round(definition.health * healthMultiplier))
  return { id, role: definition.ai === 'guardian' ? 'guardian' : 'monster', kind: definition.id, name: definition.name, x: point.x, y: point.y, health: maxHealth, maxHealth, attack: definition.attack + (difficulty?.attackBonus ?? 0), defense: definition.defense + (difficulty?.defenseBonus ?? 0), speed: definition.speed, energy: 0, glyph: definition.glyph, color: definition.color, hostile: true, ai: definition.ai, combatRole: monsterRoleFor(definition), tags: [...(definition.tags ?? [])], terrainAffinity: terrainAffinityFor(definition), conditions: [], ...(definition.ai === 'guardian' ? { guardianPhase: 'opening' as const, status: difficulty?.guardianPattern ? [`pattern:${difficulty.guardianPattern}`] : [] } : {}) }
}

function friendly(role: 'merchant' | 'ally', name: string, point: Point, glyph: string, color: string): Actor {
  return { id: `${role}-${pointKey(point)}`, role, kind: role, name, x: point.x, y: point.y, health: 99, maxHealth: 99, attack: 0, defense: 99, speed: 0, energy: 0, glyph, color, hostile: false, conditions: [] }
}

const distance = (a: Point, b: Point): number => Math.abs(a.x - b.x) + Math.abs(a.y - b.y)

export interface GenerationValidation { valid: boolean; errors: string[] }

const reachableIndexes = (floor: Floor): Set<number> => reachableFloorIndexes(floor)

const objectiveTargets = (floor: Floor): Point[] => {
  if (floor.objective.kind === 'recoverSupplies') return floor.tiles.flatMap((tile, i) => tile.kind === 'crate' || tile.kind === 'chest' ? [pointAt(floor, i)] : [])
  if (floor.objective.kind === 'rescueScout') return floor.tiles.flatMap((tile, i) => tile.kind === 'rescue' ? [pointAt(floor, i)] : [])
  if (floor.objective.kind === 'invokeAltar') return floor.tiles.flatMap((tile, i) => tile.kind === 'altar' ? [pointAt(floor, i)] : [])
  return floor.actors.filter(actor => actor.role === 'guardian').map(actor => ({ x: actor.x, y: actor.y }))
}

const canReachObjectiveWithProps = (floor: Floor, target: Point): boolean => [[0, 0], [0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => hasPassablePath(floor, floor.start, { x: target.x + x, y: target.y + y }))

export const validateGeneration = (floor: Floor): GenerationValidation => {
  const errors: string[] = []
  errors.push(...validatePuzzleTemplates(), ...validateFloorPuzzles(floor), ...propDefinitionErrors)
  if (puzzleTemplatesFor(floor.biome).length && !(floor.puzzleIds?.length)) errors.push('missing puzzle template')
  if (floor.tiles.length !== floor.width * floor.height || floor.width < MAP_WIDTH || floor.height < MAP_HEIGHT || floor.index < 0 || floor.index >= FLOOR_COUNT) errors.push('invalid floor dimensions')
  if (!floor.layoutId || !layoutVariants[floor.biome].some(layout => floor.layoutId === layout || floor.layoutId === `${layout}-remix`)) errors.push('invalid layout id')
  if (!getTile(floor, floor.start.x, floor.start.y) || !passable(getTile(floor, floor.start.x, floor.start.y)!.kind)) errors.push('invalid start placement')
  if (getTile(floor, floor.exit.x, floor.exit.y)?.kind !== 'exit') errors.push('invalid exit placement')
  for (let index = 0; index < floor.tiles.length; index++) {
    const tile = floor.tiles[index]
    if (!tile.flow) continue
    const point = pointAt(floor, index)
    const delta = ({ n: { x: 0, y: -1 }, ne: { x: 1, y: -1 }, e: { x: 1, y: 0 }, se: { x: 1, y: 1 }, s: { x: 0, y: 1 }, sw: { x: -1, y: 1 }, w: { x: -1, y: 0 }, nw: { x: -1, y: -1 } } as const)[tile.flow.direction]
    const downstream = getTile(floor, point.x + delta.x, point.y + delta.y)
    if (tile.kind !== 'current' || !downstream) errors.push(`invalid flow at ${pointKey(point)}`)
    if (tile.flow.hazard && downstream?.kind !== 'current' && downstream?.kind !== 'deepWater' && downstream?.kind !== 'brine') errors.push(`unmarked flow outlet at ${pointKey(point)}`)
  }
  const reachable = reachableIndexes(floor)
  if (!hasPassablePath(floor, floor.start, floor.exit)) errors.push('exit unreachable')
  const targets = objectiveTargets(floor)
  if (!targets.length || !targets.some(target => canReachObjectiveWithProps(floor, target))) errors.push(`objective unreachable: ${floor.objective.kind}`)
  if (floor.milestones.length < 5 || floor.milestones.length > 6) errors.push('invalid milestone count')
  const rewardOffers = floor.rewardOffers ?? []
  const expectedRewardOffers: Array<[string, string]> = [['waycache', 'waycache'], ['boon-teach', 'boon'], ['boon-test', 'boon'], ['boon-payoff', 'boon']]
  for (const [milestoneId, kind] of expectedRewardOffers) {
    const offer = rewardOffers.find(candidate => candidate.milestoneId === milestoneId)
    if (!offer || offer.kind !== kind || offer.choices.length !== 3 || new Set(offer.choices.map(choice => choice.role)).size !== 3) errors.push(`invalid reward offer: ${milestoneId}`)
    if (!floor.milestones.some(milestone => milestone.rewardKey === milestoneId && milestone.kind === kind)) errors.push(`missing reward milestone: ${milestoneId}`)
  }
  if (!primaryToolUseAvailable(floor)) errors.push('unusable primary Waycache tool')
  if ((floor.encounters?.length ?? 0) !== 1) errors.push('invalid encounter count')
  const milestoneLocations = new Set<string>()
  for (const milestone of floor.milestones) {
    const key = pointKey(milestone)
    const tile = getTile(floor, milestone.x, milestone.y)
    if (!milestone.id || !['waycache', 'boon', 'augment', 'relic'].includes(milestone.kind) || !tile || !passable(tile.kind) || (tile.kind === 'exit' && milestone.kind !== 'relic') || !hasPassablePath(floor, floor.start, milestone)) errors.push(`unreachable milestone: ${milestone.id}`)
    if (milestoneLocations.has(key)) errors.push(`overlapping milestone: ${key}`)
    milestoneLocations.add(key)
  }
  for (const encounter of floor.encounters ?? []) {
    const tile = getTile(floor, encounter.x, encounter.y)
    if (!encounter.id || !['wayfarer', 'bloodBargain', 'shiftingChamber', 'stormCache', 'ancestorDebt', 'cursedObject', 'oathwell', 'windTrial', 'tombAuction', 'sunTribute', 'mirageMarket', 'brineOath', 'glassTrial', 'whiteRoad', 'saltCache', 'iceDuel', 'winterTithe', 'rimeContract', 'frostCache', 'whiteout', 'reliquaryTrial', 'minePact', 'mineKami', 'wildsPact', 'wildsKami', 'cavernsPact', 'cavernsKami', 'ruinsPact', 'ruinsKami', 'furnacePact', 'furnaceKami', 'floodedPact', 'floodedKami', 'cliffsPact', 'cliffsKami', 'burialPact', 'burialKami', 'saltPact', 'saltKami', 'frostPact', 'frostKami'].includes(encounter.kind) || !tile || !passable(tile.kind) || tile.kind === 'exit' || !hasPassablePath(floor, floor.start, encounter)) errors.push(`unreachable encounter: ${encounter.id}`)
  }
  for (const ecology of floor.ecology ?? []) {
    const tile = getTile(floor, ecology.target.x, ecology.target.y)
    if (!ecology.id || !['tide', 'wind', 'smoke', 'collapse', 'fire', 'migration', 'nesting', 'visibility'].includes(ecology.kind) || !tile || !Number.isInteger(ecology.startsAt) || ecology.startsAt < 1 || !Number.isInteger(ecology.duration) || ecology.duration < 1 || !ecology.warning || !ecology.responses.length || !ecology.cleanup || !['waiting', 'active', 'resolved'].includes(ecology.state)) { errors.push(`invalid ecology event: ${ecology.id}`); continue }
    const previousKind = tile.kind
    const previousFlow = tile.flow ? { ...tile.flow } : undefined
    tile.kind = ecology.effect
    if (ecology.effectFlow) tile.flow = { ...ecology.effectFlow }
    else delete tile.flow
    const solvable = hasPassablePath(floor, floor.start, floor.exit) && objectiveTargets(floor).some(target => canReachObjectiveWithProps(floor, target))
    tile.kind = previousKind
    if (previousFlow) tile.flow = previousFlow
    else delete tile.flow
    if (!solvable) errors.push(`ecology blocks mandatory path: ${ecology.id}`)
  }
  const placements = [...floor.actors.map(actor => ({ ...actor, type: 'actor' as const })), ...floor.items.map(item => ({ ...item, type: 'item' as const }))]
  const occupied = new Set<string>()
  for (const placement of placements) {
    const tile = getTile(floor, placement.x, placement.y)
    if (!tile || !passable(tile.kind) || tile.kind === 'lockedDoor') errors.push(`illegal ${placement.type} placement`)
    const key = pointKey(placement)
    if (occupied.has(key)) errors.push(`overlapping ${placement.type} placement`)
    occupied.add(key)
  }
  if (!floor.props.length) errors.push('missing props')
  const propIds = new Set<string>()
  const propLocations = new Set<string>()
  for (const prop of floor.props) {
    if (propIds.has(prop.id)) errors.push(`duplicate prop id: ${prop.id}`)
    propIds.add(prop.id)
    if (!PROP_IDS.includes(prop.kind)) { errors.push(`unknown prop: ${prop.kind}`); continue }
    const definition = propDefinition(prop.kind)
    const tile = getTile(floor, prop.x, prop.y)
    const location = pointKey(prop)
    if (propLocations.has(location)) errors.push(`overlapping prop placement: ${location}`)
    propLocations.add(location)
    if (prop.biome !== floor.biome || definition.biome !== floor.biome) errors.push(`invalid prop biome: ${prop.id}`)
    if (!tile || !passable(tile.kind) || tile.kind === 'lockedDoor' || !definition.terrain.includes(tile.kind)) errors.push(`illegal prop placement: ${prop.id}`)
    if (!hasPropContext(floor, prop.kind, prop)) errors.push(`invalid prop context: ${prop.id}`)
    else if (isBlockingProp(prop)) {
      const reachableSide = [[0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => hasPassablePath(floor, floor.start, { x: prop.x + x, y: prop.y + y }))
      if (!reachableSide) errors.push(`unreachable prop: ${prop.id}`)
    } else if (!reachable.has(indexOf(floor, prop.x, prop.y))) errors.push(`unreachable prop: ${prop.id}`)
    if (!['dormant', 'inspected', 'activated', 'destroyed'].includes(prop.state)) errors.push(`invalid prop state: ${prop.id}`)
    if (!prop.tags.length || !prop.hooks?.length || !prop.hooks.includes('operate')) errors.push(`invalid prop hooks: ${prop.id}`)
  }
  for (const error of validateAreaGate(gateForArea(floor.biome))) errors.push(`impossible gate: ${error}`)
  return { valid: errors.length === 0, errors }
}

export const validateFloor = (floor: Floor): boolean => validateGeneration(floor).valid
