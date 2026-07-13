import { ITEMS, MONSTERS, biomeForFloor, type MonsterDefinition } from './content'
import { rngFor, streamSeed, type Rng } from './rng'
import { FLOOR_COUNT, MAP_HEIGHT, MAP_WIDTH, type Actor, type Floor, type Point, type Tile, indexOf, inBounds } from './types'
import { objectiveForFloor } from './objectives'

const tile = (kind: Tile['kind']): Tile => ({ kind, explored: false, visible: false })
const pointKey = (point: Point) => `${point.x},${point.y}`
const passable = (kind: Tile['kind']) => !['wall', 'lava', 'pit', 'crate', 'chest'].includes(kind)

export const getTile = (floor: Floor, x: number, y: number): Tile | undefined => inBounds(x, y) ? floor.tiles[indexOf(x, y)] : undefined
export const actorAt = (floor: Floor, x: number, y: number): Actor | undefined => floor.actors.find(actor => actor.x === x && actor.y === y && actor.health > 0)
export const isPassable = (floor: Floor, x: number, y: number): boolean => {
  const target = getTile(floor, x, y)
  return Boolean(target && passable(target.kind) && target.kind !== 'lockedDoor' && !actorAt(floor, x, y))
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
  placeEvents(floor, rooms)
  placeDoorsAndLocks(floor, rngFor(runSeed, 'gates', index), rooms)
  placeContainers(floor, rngFor(runSeed, 'loot', index, 'containers'), rooms)
  placeActors(floor, rngFor(runSeed, 'generation', index, 'actors'), rooms)
  placeItems(floor, rngFor(runSeed, 'loot', index, 'items'), rooms)
  ensureReachable(floor)
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

const carveH = (floor: Floor, from: number, to: number, y: number) => { for (let x = Math.min(from, to); x <= Math.max(from, to); x++) setKind(floor, x, y, 'floor') }
const carveV = (floor: Floor, from: number, to: number, x: number) => { for (let y = Math.min(from, to); y <= Math.max(from, to); y++) setKind(floor, x, y, 'floor') }
const setKind = (floor: Floor, x: number, y: number, kind: Tile['kind']) => { if (inBounds(x, y)) floor.tiles[indexOf(x, y)].kind = kind }

function decorateBiome(floor: Floor, rng: Rng, rooms: Room[]): void {
  const candidates = floor.tiles.flatMap((current, i) => current.kind === 'floor' ? [{ x: i % MAP_WIDTH, y: Math.floor(i / MAP_WIDTH) }] : [])
  const safe = candidates.filter(point => distance(point, floor.start) > 5 && distance(point, floor.exit) > 3)
  const paint = (kind: Tile['kind'], count: number) => {
    for (let i = 0; i < count; i++) {
      const point = rng.pick(safe)
      if (!actorAt(floor, point.x, point.y)) setKind(floor, point.x, point.y, kind)
    }
  }
  if (floor.biome === 'mine') { paint('crumble', 12); paint('boulder', 5); paint('spikes', 5) }
  if (floor.biome === 'wilds') { paint('water', 20); paint('web', 9); paint('spikes', 4) }
  if (floor.biome === 'caverns') { paint('lava', 16); paint('fireVent', 10); paint('gas', 8) }
  if (floor.biome === 'ruins') { paint('dart', 10); paint('spikes', 8); paint('crumble', 8); paint('boulder', 6) }
  if (floor.index % 4 === 3) {
    const chamber = rooms[rooms.length - 1]
    for (let y = chamber.y; y < chamber.y + chamber.h; y++) for (let x = chamber.x; x < chamber.x + chamber.w; x++) setKind(floor, x, y, 'floor')
    setKind(floor, floor.exit.x, floor.exit.y, 'exit')
  }
}

function placeEvents(floor: Floor, rooms: Room[]): void {
  const eventRoom = rooms[Math.max(1, Math.floor(rooms.length / 2))]
  const point = center(eventRoom)
  const kind: Tile['kind'] = floor.index % 4 === 0 ? 'shop' : floor.index % 4 === 1 ? 'rescue' : floor.index % 4 === 2 ? 'altar' : 'shop'
  setKind(floor, point.x, point.y, kind)
  if (kind === 'shop') floor.actors.push(friendly('merchant', `${floor.biome} trader`, point, '$', '#f4d26a'))
  if (kind === 'rescue') floor.actors.push(friendly('ally', 'lost scout', point, '&', '#8ae0b3'))
  if (kind === 'altar') floor.actors.push(friendly('ally', 'shrine keeper', point, '_', '#d6a8eb'))
}

function placeDoorsAndLocks(floor: Floor, rng: Rng, rooms: Room[]): void {
  for (const room of rooms.slice(1, -1)) {
    const point = center(room)
    const door = { x: Math.max(1, point.x - Math.floor(room.w / 2)), y: point.y }
    if (getTile(floor, door.x, door.y)?.kind === 'floor') setKind(floor, door.x, door.y, rng.chance(25) ? 'lockedDoor' : 'door')
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
    if (current && passable(current.kind) && current.kind !== 'exit' && !actorAt(floor, point.x, point.y) && !floor.items.some(item => item.x === point.x && item.y === point.y) && distance(point, floor.start) > 4) return point
  }
  return { ...floor.start }
}

function monster(definition: MonsterDefinition, point: Point, i: number): Actor {
  return { id: `${definition.id}-${i}`, role: definition.ai === 'guardian' ? 'guardian' : 'monster', kind: definition.id, name: definition.name, x: point.x, y: point.y, health: definition.health, maxHealth: definition.health, attack: definition.attack, defense: definition.defense, speed: definition.speed, energy: 0, glyph: definition.glyph, color: definition.color, hostile: true, ai: definition.ai, conditions: [], ...(definition.ai === 'guardian' ? { guardianPhase: 'opening' as const } : {}) }
}

function friendly(role: 'merchant' | 'ally', name: string, point: Point, glyph: string, color: string): Actor {
  return { id: `${role}-${pointKey(point)}`, role, kind: role, name, x: point.x, y: point.y, health: 99, maxHealth: 99, attack: 0, defense: 99, speed: 0, energy: 0, glyph, color, hostile: false, conditions: [] }
}

function ensureReachable(floor: Floor): void {
  const seen = new Set<string>([pointKey(floor.start)])
  const queue = [{ ...floor.start }]
  while (queue.length) {
    const current = queue.shift()!
    for (const [dx, dy] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) {
      const next = { x: current.x + dx, y: current.y + dy }
      if (!inBounds(next.x, next.y) || seen.has(pointKey(next))) continue
      const currentTile = getTile(floor, next.x, next.y)!
      if (!passable(currentTile.kind) || currentTile.kind === 'lockedDoor') continue
      seen.add(pointKey(next))
      queue.push(next)
    }
  }
  if (!seen.has(pointKey(floor.exit))) {
    carveH(floor, floor.start.x, floor.exit.x, floor.start.y)
    carveV(floor, floor.start.y, floor.exit.y, floor.exit.x)
    setKind(floor, floor.exit.x, floor.exit.y, 'exit')
  }
}

const distance = (a: Point, b: Point): number => Math.abs(a.x - b.x) + Math.abs(a.y - b.y)

export const validateFloor = (floor: Floor): boolean => floor.tiles.length === MAP_WIDTH * MAP_HEIGHT && floor.index >= 0 && floor.index < FLOOR_COUNT && getTile(floor, floor.start.x, floor.start.y)?.kind !== 'wall' && getTile(floor, floor.exit.x, floor.exit.y)?.kind === 'exit'
