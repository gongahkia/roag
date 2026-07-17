import { ITEMS, MONSTERS, biomeForFloor, type MonsterDefinition } from './content'
import { rngFor, streamSeed, type Rng } from './rng'
import { FLOOR_COUNT, MAP_HEIGHT, MAP_WIDTH, type Actor, type Floor, type Point, type Prop, type Tile, indexOf, inBounds } from './types'
import { objectiveForFloor } from './objectives'
import { gateForArea, validateAreaGate } from './engine/gates'
import { puzzleTemplatesFor, validateFloorPuzzles, validatePuzzleTemplates } from './puzzles'
import { PROP_IDS, propDefinition, propDefinitionsFor, validatePropDefinitions } from './props'

const tile = (kind: Tile['kind']): Tile => ({ kind, explored: false, visible: false })
const pointKey = (point: Point) => `${point.x},${point.y}`
const passable = (kind: Tile['kind']) => !['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest'].includes(kind)
const propDefinitionErrors = validatePropDefinitions()

export const getTile = (floor: Floor, x: number, y: number): Tile | undefined => inBounds(x, y) ? floor.tiles[indexOf(x, y)] : undefined
export const actorAt = (floor: Floor, x: number, y: number): Actor | undefined => floor.actors.find(actor => actor.x === x && actor.y === y && actor.health > 0)
export const isPassable = (floor: Floor, x: number, y: number): boolean => {
  const target = getTile(floor, x, y)
  return Boolean(target && passable(target.kind) && target.kind !== 'lockedDoor' && !actorAt(floor, x, y))
}

export const hasPassableTerrainPath = (floor: Floor, start: Point, destination: Point): boolean => {
  const queue = [{ ...start }]
  const seen = new Set<string>()
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const point = queue[cursor]
    const key = pointKey(point)
    if (seen.has(key)) continue
    const current = getTile(floor, point.x, point.y)
    if (!current || !passable(current.kind) || current.kind === 'lockedDoor') continue
    if (point.x === destination.x && point.y === destination.y) return true
    seen.add(key)
    for (const [x, y] of [[-1, -1], [0, -1], [1, -1], [-1, 0], [1, 0], [-1, 1], [0, 1], [1, 1]]) queue.push({ x: point.x + x, y: point.y + y })
  }
  return false
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

export function generateFloor(runSeed: number, index: number): Floor {
  const seed = streamSeed(runSeed, 'generation', index)
  const layoutRng = rngFor(runSeed, 'generation', index, 'layout')
  const biome = biomeForFloor(index)
  const floor: Floor = {
    index,
    biome,
    seed,
    tiles: Array.from({ length: MAP_WIDTH * MAP_HEIGHT }, () => tile('wall')),
    actors: [],
    items: [],
    props: [],
    start: { x: 2, y: 2 },
    exit: { x: MAP_WIDTH - 3, y: MAP_HEIGHT - 3 },
    guardianDefeated: index % 4 !== 3,
    objective: objectiveForFloor(index),
    telegraphs: []
  }
  const rooms = carveRooms(floor, layoutRng)
  connectRooms(floor, rooms)
  floor.start = center(rooms[0])
  floor.exit = center(rooms[rooms.length - 1])
  setKind(floor, floor.exit.x, floor.exit.y, 'exit')
  decorateBiome(floor, rngFor(runSeed, 'generation', index, 'terrain'), rooms)
  placePuzzleTemplate(floor, rngFor(runSeed, 'generation', index, 'puzzle'), rooms)
  placeEvents(floor, rooms)
  placeDoorsAndLocks(floor, rngFor(runSeed, 'gates', index), rooms)
  placeContainers(floor, rngFor(runSeed, 'loot', index, 'containers'), rooms)
  placeActors(floor, rngFor(runSeed, 'generation', index, 'actors'), rooms)
  placeItems(floor, rngFor(runSeed, 'loot', index, 'items'), rooms)
  placeProps(floor, rngFor(runSeed, 'props', index, 'placement'), ensureReachable(floor))
  const validation = validateGeneration(floor)
  if (!validation.valid) throw new Error(`invalid generated floor ${index}: ${validation.errors.join('; ')}`)
  return floor
}

export const areaFloorIndex = (biome: Floor['biome'], areaFloor: number): number => (['mine', 'wilds', 'caverns', 'ruins'] as const).indexOf(biome) * 4 + areaFloor
export const generateAreaFloor = (runSeed: number, biome: Floor['biome'], areaFloor: number): Floor => {
  if (!Number.isInteger(areaFloor) || areaFloor < 0 || areaFloor > 3) throw new Error(`invalid area floor: ${areaFloor}`)
  return generateFloor(runSeed, areaFloorIndex(biome, areaFloor))
}

interface Room { x: number; y: number; w: number; h: number }
const center = (room: Room): Point => ({ x: room.x + Math.floor(room.w / 2), y: room.y + Math.floor(room.h / 2) })
const overlaps = (a: Room, b: Room) => a.x - 2 < b.x + b.w && a.x + a.w + 2 > b.x && a.y - 2 < b.y + b.h && a.y + a.h + 2 > b.y

function carveRooms(floor: Floor, rng: Rng): Room[] {
  const rooms: Room[] = []
  for (let attempt = 0; attempt < 160 && rooms.length < 12; attempt++) {
    const room: Room = { x: rng.int(2, MAP_WIDTH - 11), y: rng.int(2, MAP_HEIGHT - 9), w: rng.int(5, 10), h: rng.int(4, 7) }
    if (rooms.some(other => overlaps(room, other))) continue
    rooms.push(room)
    for (let y = room.y; y < room.y + room.h; y++) for (let x = room.x; x < room.x + room.w; x++) setKind(floor, x, y, 'floor')
  }
  if (rooms.length < 2) throw new Error('failed to generate rooms')
  return rooms.sort((a, b) => a.x - b.x || a.y - b.y)
}

function connectRooms(floor: Floor, rooms: Room[]): void {
  for (let i = 1; i < rooms.length; i++) {
    const from = center(rooms[i - 1])
    const to = center(rooms[i])
    if (i % 2) { carveH(floor, from.x, to.x, from.y); carveV(floor, from.y, to.y, to.x) }
    else { carveV(floor, from.y, to.y, from.x); carveH(floor, from.x, to.x, to.y) }
  }
}

function placeProps(floor: Floor, rng: Rng, reachable: ReadonlySet<number>): void {
  const definitions = rng.shuffle([...propDefinitionsFor(floor.biome)])
  const occupied = new Set<number>([
    indexOf(floor.start.x, floor.start.y),
    indexOf(floor.exit.x, floor.exit.y),
    ...objectiveTargets(floor).map(point => indexOf(point.x, point.y)),
    ...floor.actors.map(actor => indexOf(actor.x, actor.y)),
    ...floor.items.map(item => indexOf(item.x, item.y))
  ])
  for (const definition of definitions) {
    const candidates: number[] = []
    for (let index = 0; index < floor.tiles.length; index++) {
      const tile = floor.tiles[index]
      if (definition.terrain.includes(tile.kind) && passable(tile.kind) && reachable.has(index) && !occupied.has(index)) candidates.push(index)
    }
    if (!candidates.length) continue
    const placement = rng.pick(candidates)
    const point = { x: placement % MAP_WIDTH, y: Math.floor(placement / MAP_WIDTH) }
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
    floor.props.push(prop)
    occupied.add(placement)
    break
  }
}

const carveH = (floor: Floor, from: number, to: number, y: number) => { for (let x = Math.min(from, to); x <= Math.max(from, to); x++) setKind(floor, x, y, 'floor') }
const carveV = (floor: Floor, from: number, to: number, x: number) => { for (let y = Math.min(from, to); y <= Math.max(from, to); y++) setKind(floor, x, y, 'floor') }
const setKind = (floor: Floor, x: number, y: number, kind: Tile['kind']) => { if (inBounds(x, y)) floor.tiles[indexOf(x, y)].kind = kind }
const railH = (floor: Floor, from: number, to: number, y: number) => { for (let x = Math.min(from, to); x <= Math.max(from, to); x++) if (getTile(floor, x, y)?.kind === 'floor') setKind(floor, x, y, 'rail') }
const railV = (floor: Floor, from: number, to: number, x: number) => { for (let y = Math.min(from, to); y <= Math.max(from, to); y++) if (getTile(floor, x, y)?.kind === 'floor') setKind(floor, x, y, 'rail') }

function decorateBiome(floor: Floor, rng: Rng, rooms: Room[]): void {
  if (floor.biome === 'mine') decorateMine(floor, rng, rooms)
  if (floor.biome === 'wilds') decorateWilds(floor, rng)
  if (floor.biome === 'caverns') decorateCaverns(floor, rng)
  if (floor.biome === 'ruins') decorateRuins(floor, rng, rooms)
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
    if (tile && tile.kind !== 'wall' && tile.kind !== 'exit') tile.kind = placement.kind
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
  const safe = () => floor.tiles.flatMap((current, i) => current.kind === 'floor' ? [{ x: i % MAP_WIDTH, y: Math.floor(i / MAP_WIDTH) }] : []).filter(point => distance(point, floor.start) > 5 && distance(point, floor.exit) > 3)
  const paint = (kind: Tile['kind'], count: number, byRail = false) => {
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
  const safe = () => floor.tiles.flatMap((current, i) => current.kind === 'floor' ? [{ x: i % MAP_WIDTH, y: Math.floor(i / MAP_WIDTH) }] : []).filter(point => distance(point, floor.start) > 5 && distance(point, floor.exit) > 3)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) if (rng.chance(45) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  paint('water', 9, true)
  paint('bramble', 8, true)
  paint('web', 10)
}

function decorateCaverns(floor: Floor, rng: Rng): void {
  const safe = () => floor.tiles.flatMap((current, i) => current.kind === 'floor' ? [{ x: i % MAP_WIDTH, y: Math.floor(i / MAP_WIDTH) }] : []).filter(point => distance(point, floor.start) > 5 && distance(point, floor.exit) > 3)
  const paint = (kind: Tile['kind'], count: number, clustered = false) => {
    let candidates = safe()
    for (let i = 0; i < count && candidates.length; i++) {
      const point = rng.pick(candidates)
      setKind(floor, point.x, point.y, kind)
      if (clustered) for (const [x, y] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) if (rng.chance(40) && getTile(floor, point.x + x, point.y + y)?.kind === 'floor') setKind(floor, point.x + x, point.y + y, kind)
      candidates = safe()
    }
  }
  paint('lava', 8, true)
  paint('gas', 7, true)
  paint('fireVent', 10)
  paint('darkness', 11, true)
}

function decorateRuins(floor: Floor, rng: Rng, rooms: Room[]): void {
  const safe = () => floor.tiles.flatMap((current, i) => current.kind === 'floor' ? [{ x: i % MAP_WIDTH, y: Math.floor(i / MAP_WIDTH) }] : []).filter(point => distance(point, floor.start) > 5 && distance(point, floor.exit) > 3)
  const paint = (kind: Tile['kind'], count: number) => {
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

function placeEvents(floor: Floor, rooms: Room[]): void {
  const eventRoom = rooms[Math.max(1, Math.floor(rooms.length / 2))]
  const point = center(eventRoom)
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

function placeContainers(floor: Floor, rng: Rng, rooms: Room[]): void {
  for (let i = 0; i < 4; i++) {
    const point = freeRoomPoint(floor, rng, rooms)
    setKind(floor, point.x, point.y, i === 3 ? 'chest' : 'crate')
  }
}

function placeActors(floor: Floor, rng: Rng, rooms: Room[]): void {
  const definitions = MONSTERS.filter(monster => monster.biome === floor.biome)
  const regular = definitions.filter(monster => monster.ai !== 'guardian')
  const count = 8 + floor.index % 4 * 2
  for (let i = 0; i < count; i++) {
    const point = freeRoomPoint(floor, rng, rooms.slice(1))
    floor.actors.push(monster(rng.pick(regular), point, i))
  }
  if (floor.index % 4 === 3) {
    const guardian = definitions.find(monster => monster.ai === 'guardian')!
    floor.actors.push(monster(guardian, floor.exit, 99))
  }
}

function placeItems(floor: Floor, rng: Rng, rooms: Room[]): void {
  const loot = ITEMS.filter(item => !item.slot || rng.chance(30))
  const count = 10 + floor.index % 4 * 2
  for (let i = 0; i < count; i++) {
    const point = freeRoomPoint(floor, rng, rooms)
    const id = i === 0 && floor.index % 4 === 0 ? 'key' : rng.pick(loot).id
    floor.items.push({ id, x: point.x, y: point.y, count: 1 })
  }
}

function freeRoomPoint(floor: Floor, rng: Rng, rooms: Room[]): Point {
  for (let tries = 0; tries < 200; tries++) {
    const room = rng.pick(rooms)
    const point = { x: rng.int(room.x + 1, room.x + room.w - 2), y: rng.int(room.y + 1, room.y + room.h - 2) }
    const current = getTile(floor, point.x, point.y)
    if (current?.kind === 'floor' && !actorAt(floor, point.x, point.y) && !floor.items.some(item => item.x === point.x && item.y === point.y) && distance(point, floor.start) > 4) return point
  }
  return { ...floor.start }
}

function monster(definition: MonsterDefinition, point: Point, i: number): Actor {
  return { id: `${definition.id}-${i}`, role: definition.ai === 'guardian' ? 'guardian' : 'monster', kind: definition.id, name: definition.name, x: point.x, y: point.y, health: definition.health, maxHealth: definition.health, attack: definition.attack, defense: definition.defense, speed: definition.speed, energy: 0, glyph: definition.glyph, color: definition.color, hostile: true, ai: definition.ai, conditions: [], ...(definition.ai === 'guardian' ? { guardianPhase: 'opening' as const } : {}) }
}

function friendly(role: 'merchant' | 'ally', name: string, point: Point, glyph: string, color: string): Actor {
  return { id: `${role}-${pointKey(point)}`, role, kind: role, name, x: point.x, y: point.y, health: 99, maxHealth: 99, attack: 0, defense: 99, speed: 0, energy: 0, glyph, color, hostile: false, conditions: [] }
}

function ensureReachable(floor: Floor): Set<number> {
  const objectives = objectiveTargets(floor).map(point => ({ point, kind: getTile(floor, point.x, point.y)!.kind }))
  let reachable = reachableIndexes(floor)
  if (!reachable.has(indexOf(floor.exit.x, floor.exit.y))) {
    carveH(floor, floor.start.x, floor.exit.x, floor.start.y)
    carveV(floor, floor.start.y, floor.exit.y, floor.exit.x)
    setKind(floor, floor.exit.x, floor.exit.y, 'exit')
    reachable = reachableIndexes(floor)
  }
  for (const objective of objectives) setKind(floor, objective.point.x, objective.point.y, objective.kind)
  const target = objectiveTargets(floor).find(point => !canReachObjective(reachable, point))
  if (!target) return reachable
  const targetTile = getTile(floor, target.x, target.y)!
  const originalKind = targetTile.kind
  const access = passable(originalKind) ? target : { x: target.x + 1, y: target.y }
  carveH(floor, floor.start.x, access.x, floor.start.y)
  carveV(floor, floor.start.y, access.y, access.x)
  for (const [x, y] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) if (inBounds(target.x + x, target.y + y)) setKind(floor, target.x + x, target.y + y, 'floor')
  targetTile.kind = originalKind
  setKind(floor, floor.exit.x, floor.exit.y, 'exit')
  return reachableIndexes(floor)
}

const distance = (a: Point, b: Point): number => Math.abs(a.x - b.x) + Math.abs(a.y - b.y)

export interface GenerationValidation { valid: boolean; errors: string[] }

const reachableIndexes = (floor: Floor): Set<number> => {
  const seen = new Set<number>()
  const queue = [indexOf(floor.start.x, floor.start.y)]
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const index = queue[cursor]
    if (seen.has(index)) continue
    const x = index % MAP_WIDTH
    const y = Math.floor(index / MAP_WIDTH)
    const tile = floor.tiles[index]
    if (!tile || !passable(tile.kind) || tile.kind === 'lockedDoor') continue
    seen.add(index)
    for (const [dx, dy] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) if (inBounds(x + dx, y + dy)) queue.push(indexOf(x + dx, y + dy))
  }
  return seen
}

const objectiveTargets = (floor: Floor): Point[] => {
  if (floor.objective.kind === 'recoverSupplies') return floor.tiles.flatMap((tile, i) => tile.kind === 'crate' || tile.kind === 'chest' ? [{ x: i % MAP_WIDTH, y: Math.floor(i / MAP_WIDTH) }] : [])
  if (floor.objective.kind === 'rescueScout') return floor.tiles.flatMap((tile, i) => tile.kind === 'rescue' ? [{ x: i % MAP_WIDTH, y: Math.floor(i / MAP_WIDTH) }] : [])
  if (floor.objective.kind === 'invokeAltar') return floor.tiles.flatMap((tile, i) => tile.kind === 'altar' ? [{ x: i % MAP_WIDTH, y: Math.floor(i / MAP_WIDTH) }] : [])
  return floor.actors.filter(actor => actor.role === 'guardian').map(actor => ({ x: actor.x, y: actor.y }))
}

const canReachObjective = (reachable: ReadonlySet<number>, target: Point): boolean => [[0, 0], [0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => inBounds(target.x + x, target.y + y) && reachable.has(indexOf(target.x + x, target.y + y)))

export const validateGeneration = (floor: Floor): GenerationValidation => {
  const errors: string[] = []
  errors.push(...validatePuzzleTemplates(), ...validateFloorPuzzles(floor), ...propDefinitionErrors)
  if (puzzleTemplatesFor(floor.biome).length && !(floor.puzzleIds?.length)) errors.push('missing puzzle template')
  if (floor.tiles.length !== MAP_WIDTH * MAP_HEIGHT || floor.index < 0 || floor.index >= FLOOR_COUNT) errors.push('invalid floor dimensions')
  if (!getTile(floor, floor.start.x, floor.start.y) || !passable(getTile(floor, floor.start.x, floor.start.y)!.kind)) errors.push('invalid start placement')
  if (getTile(floor, floor.exit.x, floor.exit.y)?.kind !== 'exit') errors.push('invalid exit placement')
  const reachable = reachableIndexes(floor)
  if (!reachable.has(indexOf(floor.exit.x, floor.exit.y))) errors.push('exit unreachable')
  const targets = objectiveTargets(floor)
  if (!targets.length || !targets.some(target => canReachObjective(reachable, target))) errors.push(`objective unreachable: ${floor.objective.kind}`)
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
    if (!tile || !passable(tile.kind) || tile.kind === 'lockedDoor') errors.push(`illegal prop placement: ${prop.id}`)
    else if (!reachable.has(indexOf(prop.x, prop.y))) errors.push(`unreachable prop: ${prop.id}`)
    if (!['dormant', 'activated', 'destroyed'].includes(prop.state)) errors.push(`invalid prop state: ${prop.id}`)
    if (!prop.tags.length || !prop.hooks?.length || !prop.hooks.includes('operate')) errors.push(`invalid prop hooks: ${prop.id}`)
  }
  for (const error of validateAreaGate(gateForArea(floor.biome))) errors.push(`impossible gate: ${error}`)
  return { valid: errors.length === 0, errors }
}

export const validateFloor = (floor: Floor): boolean => validateGeneration(floor).valid
