import { biomeName } from '../content'
import { generateAreaFloor } from '../world'
import type { Actor, Biome, CampaignCycle, Companion, CompanionDeathMode, CourierCalling, CourierOrigin, DeathMode, GroundItem, Hero, LegacyRecord, Prop, RescuedNpc, RunState, Tile, TransitChunkMemory } from '../types'
import { refreshFov } from './visibility'
import { hydrateEncyclopediaLegacy } from './encyclopedia'
import { createRunTelemetry } from '../telemetry'
import { recordSafePosition } from './buildcraft'
import { cloneCampaignCycle, DEFAULT_AREA_ORDER, initialCampaignCycle, campaignOrderForSeed } from './campaign'
import { applyAreaArcState, areaArcStateFor } from '../escalation'
import { emptySocialReputation } from '../social-contract'
import { cloneCompanions } from './companions'
import { isCompanionActor, synchronizePartyActors } from './party'

export interface CourierBuild { name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode }

const routeOffsetFor = (linkId: string): number => [...linkId].reduce((total, char) => total + char.charCodeAt(0), 0)
const routeChunkKey = (chunk: number): string => String(chunk)
const cloneActor = (actor: Actor, x = actor.x, y = actor.y): Actor => ({ ...structuredClone(actor), x, y })
const cloneItem = (item: GroundItem, x = item.x, y = item.y): GroundItem => ({ ...structuredClone(item), x, y })
const cloneProp = (prop: Prop, x = prop.x, y = prop.y): Prop => ({ ...structuredClone(prop), x, y, effectCells: prop.effectCells?.map(cell => ({ x: cell.x - prop.x + x, y: cell.y - prop.y + y })) })

const routeY = (travel: NonNullable<RunState['travel']>, chunkWidth: number, chunk: number, localX: number, center: number): number => center + Math.round(Math.sin((chunk * chunkWidth + localX + routeOffsetFor(travel.linkId)) / 7) * 3)

const routeTerrain = (travel: NonNullable<RunState['travel']>, chunkWidth: number, height: number, chunk: number): Tile[] => {
  const width = chunkWidth
  const center = Math.floor(height / 2)
  const tiles = Array.from({ length: width * height }, () => ({ kind: 'wall' as const, explored: false, visible: false }))
  const carve = (x: number, y: number, radius = 2) => {
    for (let dy = -radius; dy <= radius; dy++) for (let dx = -1; dx <= 1; dx++) {
      const px = x + dx
      const py = y + dy
      if (px > 0 && px < width - 1 && py > 0 && py < height - 1) tiles[py * width + px]!.kind = 'floor'
    }
  }
  for (let x = 2; x < width - 2; x++) {
    const y = routeY(travel, chunkWidth, chunk, x, center)
    carve(x, y)
    if ((chunk * width + x) % 13 === 0) for (let branch = 1; branch < 7; branch++) carve(x, y + ((chunk * width + x) % 26 ? branch : -branch), 1)
    const situation = travel.situations[chunk]
    if (situation === 'hazard' && (chunk * width + x) % 19 === 0) tiles[y * width + x]!.kind = 'gas'
    if (situation === 'ecology' && (chunk * width + x) % 23 === 0) tiles[y * width + x]!.kind = 'bramble'
  }
  tiles[center * width + 2]!.kind = 'floor'
  tiles[routeY(travel, chunkWidth, chunk, width - 3, center) * width + width - 3]!.kind = 'exit'
  return tiles
}

const routeActors = (travel: NonNullable<RunState['travel']>, chunkWidth: number, height: number, chunk: number, patrolTemplate: Actor | undefined): Actor[] => {
  const x = Math.min(chunkWidth - 5, Math.floor(chunkWidth * .62))
  const y = routeY(travel, chunkWidth, chunk, x, Math.floor(height / 2))
  if (travel.situations[chunk] === 'patrol' && patrolTemplate) return [cloneActor(patrolTemplate, x, y)]
  if (travel.situations[chunk] === 'trader') return [{ id: `route-trader:${travel.linkId}:${chunk}`, role: 'merchant', kind: 'merchant', name: 'route trader', x, y, health: 1, maxHealth: 1, attack: 0, defense: 0, speed: 0, energy: 0, glyph: '$', color: '#f4d26a', hostile: false }]
  return []
}

const restoredChunk = (travel: NonNullable<RunState['travel']>, chunkWidth: number, height: number, chunk: number, patrolTemplate: Actor | undefined): { tiles: Tile[]; actors: Actor[]; items: GroundItem[]; props: Prop[] } => {
  const tiles = routeTerrain(travel, chunkWidth, height, chunk)
  const memory = travel.chunks?.[routeChunkKey(chunk)]
  for (const change of memory?.terrain ?? []) {
    const tile = tiles[change.index]
    if (tile) Object.assign(tile, structuredClone(change), { explored: false, visible: false })
  }
  return {
    tiles,
    actors: memory ? memory.actors.map(actor => cloneActor(actor)) : routeActors(travel, chunkWidth, height, chunk, patrolTemplate).map(actor => ({ ...actor, id: actor.id.includes(':') ? actor.id : `route-patrol:${travel.linkId}:${chunk}` })),
    items: (memory?.items ?? []).map(item => cloneItem(item)),
    props: (memory?.props ?? []).map(prop => cloneProp(prop))
  }
}

const rememberedTerrain = (current: Tile, baseline: Tile): TransitChunkMemory['terrain'][number] | undefined => {
  if (current.kind === baseline.kind && current.elevation === baseline.elevation && JSON.stringify(current.flow) === JSON.stringify(baseline.flow)) return undefined
  return { index: 0, kind: current.kind, ...(current.elevation === undefined ? {} : { elevation: current.elevation }), ...(current.flow === undefined ? {} : { flow: structuredClone(current.flow) }) }
}

const rememberTransitWindow = (state: RunState): NonNullable<RunState['travel']> | undefined => {
  const travel = state.travel
  if (!travel) return undefined
  const residentCount = Math.min(3, travel.chunkCount - travel.residentStart)
  const chunkWidth = Math.floor(state.floor.width / residentCount)
  const chunks = { ...(travel.chunks ?? {}) }
  for (let localChunk = 0; localChunk < residentCount; localChunk++) {
    const chunk = travel.residentStart + localChunk
    const baseline = routeTerrain(travel, chunkWidth, state.floor.height, chunk)
    const offset = localChunk * chunkWidth
    const terrain = baseline.flatMap((tile, index) => {
      const memory = rememberedTerrain(state.floor.tiles[Math.floor(index / chunkWidth) * state.floor.width + offset + index % chunkWidth]!, tile)
      return memory ? [{ ...memory, index }] : []
    })
    const inChunk = (x: number) => x >= offset && x < offset + chunkWidth
    chunks[routeChunkKey(chunk)] = {
      terrain,
      actors: state.floor.actors.filter(actor => !isCompanionActor(actor) && inChunk(actor.x)).map(actor => cloneActor(actor, actor.x - offset, actor.y)),
      items: state.floor.items.filter(item => inChunk(item.x)).map(item => cloneItem(item, item.x - offset, item.y)),
      props: state.floor.props.filter(prop => inChunk(prop.x)).map(prop => cloneProp(prop, prop.x - offset, prop.y))
    }
  }
  return { ...travel, chunks }
}

const originStats: Record<CourierOrigin, Hero['stats']> = {
  mineborn: { strength: 3, agility: 1, vitality: 3, intellect: 1 },
  mosswalker: { strength: 1, agility: 3, vitality: 3, intellect: 1 },
  cavernSeeker: { strength: 1, agility: 2, vitality: 2, intellect: 3 },
  tidebound: { strength: 2, agility: 3, vitality: 1, intellect: 2 }
}

const starterKit = (origin: CourierOrigin, calling: CourierCalling): Pick<Hero, 'bombs' | 'ropes' | 'inventory' | 'equipment'> => {
  const kit = calling === 'trailguard'
    ? { bombs: 4, ropes: 4, inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'], equipment: { mainHand: 'whip', offHand: 'buckler' } }
    : calling === 'pathmaker'
      ? { bombs: 6, ropes: 6, inventory: ['tonic', 'rock', 'bombPack', 'bombPack', 'ropeBundle', 'ropeBundle', 'ember', 'mapScroll'], equipment: { mainHand: 'whip' } }
      : { bombs: 4, ropes: 4, inventory: ['tonic', 'focusTonic', 'rock', 'bombPack', 'ropeBundle', 'ember', 'sight'], equipment: { mainHand: 'whip' } }
  return origin === 'tidebound' ? { ...kit, inventory: [...kit.inventory, 'tideSpear'], equipment: { ...kit.equipment, mainHand: 'tideSpear' } } : kit
}

export const newHero = (build: Partial<CourierBuild> = {}): Hero => {
  const origin = build.origin ?? 'mineborn'
  const calling = build.calling ?? 'trailguard'
  const kit = Object.keys(build).length ? starterKit(origin, calling) : { bombs: 4, ropes: 4, inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'], equipment: { mainHand: 'whip' } }
  return {
    name: build.name?.trim() || 'Existing Courier', origin, calling, deathMode: build.deathMode ?? 'checkpoint',
    x: 0, y: 0, health: 22, maxHealth: 22, focus: 8, maxFocus: 8, gold: 0, bombs: kit.bombs, ropes: kit.ropes, keys: 0, xp: 0, level: 1,
    stats: { ...originStats[origin] }, skills: [], inventory: kit.inventory, equipment: kit.equipment, conditions: [], cooldowns: {}, trailcrafts: {}, traversalTools: [], relics: [], relicCharges: {}, boons: {}, boonEvolutions: {}, safePositions: []
  }
}

export function newRun(seed = Math.floor(Math.random() * 0x7fffffff), area: Biome = 'mine', areaFloor = 0, inheritedHero?: Hero, rescuedNpcs: readonly RescuedNpc[] = [], legacyRecords: readonly LegacyRecord[] = [], areaOrder: readonly Biome[] = DEFAULT_AREA_ORDER, cycle: CampaignCycle = initialCampaignCycle(), companions: readonly Companion[] = [], companionDeathMode: CompanionDeathMode = 'injury'): RunState {
  const routePosition = Math.max(0, areaOrder.indexOf(area))
  const floor = generateAreaFloor(seed, area, areaFloor, routePosition, cycle)
  const areaArc = areaArcStateFor(seed, area)
  applyAreaArcState(floor, areaArc)
  const hero = inheritedHero ? structuredClone(inheritedHero) : newHero()
  if (inheritedHero && areaFloor === 0 && routePosition > 0) hero.gold = Math.min(500, hero.gold + (hero.boons?.windfall ?? 0) * 20)
  hero.x = floor.start.x
  hero.y = floor.start.y
  const state: RunState = { version: 5, seed, floor, hero, messages: [`Landing file confirms ${biomeName[area]}.`, 'The mission archive lists H for help.'], status: 'playing', turn: 0, area, areaFloor, areaArc, areaOrder: [...areaOrder], ...(inheritedHero ? { replayHero: structuredClone(inheritedHero) } : {}), rescuedNpcs: rescuedNpcs.map(npc => ({ ...npc })), companions: cloneCompanions(companions), companionDeathMode, lineageEvents: [], alignment: { kami: 0, villagePact: 0 }, reputation: emptySocialReputation(), campaignCycle: cloneCampaignCycle(cycle) }
  synchronizePartyActors(state, 'spawn')
  hydrateEncyclopediaLegacy(state, legacyRecords)
  state.telemetry = createRunTelemetry(state)
  refreshFov(state)
  recordSafePosition(state)
  return state
}

export function newTransitRun(seed: number, area: Biome, inheritedHero: Hero, travel: NonNullable<RunState['travel']>, rescuedNpcs: readonly RescuedNpc[] = [], legacyRecords: readonly LegacyRecord[] = [], areaOrder: readonly Biome[] = DEFAULT_AREA_ORDER, cycle: CampaignCycle = initialCampaignCycle(), companions: readonly Companion[] = [], companionDeathMode: CompanionDeathMode = 'injury'): RunState {
  const state = newRun(seed, area, 0, inheritedHero, rescuedNpcs, legacyRecords, areaOrder, cycle, companions, companionDeathMode)
  const floor = state.floor
  const patrolTemplate = structuredClone(floor.actors.find(actor => actor.hostile))
  const chunkWidth = floor.width
  const residentCount = Math.min(3, travel.chunkCount - travel.residentStart)
  floor.width *= residentCount
  const center = Math.floor(floor.height / 2)
  floor.tiles = Array.from({ length: floor.width * floor.height }, () => ({ kind: 'wall' as const, explored: false, visible: false }))
  floor.actors = []
  floor.items = []
  floor.props = []
  for (let localChunk = 0; localChunk < residentCount; localChunk++) {
    const chunk = travel.residentStart + localChunk
    const restored = restoredChunk(travel, chunkWidth, floor.height, chunk, patrolTemplate)
    const offset = localChunk * chunkWidth
    for (let y = 0; y < floor.height; y++) for (let x = 0; x < chunkWidth; x++) floor.tiles[y * floor.width + offset + x] = restored.tiles[y * chunkWidth + x]!
    floor.actors.push(...restored.actors.map(actor => cloneActor(actor, actor.x + offset, actor.y)))
    floor.items.push(...restored.items.map(item => cloneItem(item, item.x + offset, item.y)))
    floor.props.push(...restored.props.map(prop => cloneProp(prop, prop.x + offset, prop.y)))
  }
  const start = { x: 2, y: center }
  const lastChunk = travel.residentStart + residentCount - 1
  const exit = { x: floor.width - 3, y: routeY(travel, chunkWidth, lastChunk, chunkWidth - 3, center) }
  floor.start = start
  floor.exit = exit
  floor.layoutId = 'voyager-link-corridor'
  floor.encounters = []
  floor.milestones = []
  floor.sideSpaces = []
  floor.secretRooms = []
  floor.secretRoutes = []
  floor.ecology = []
  const cacheChunk = travel.routeCacheChunks?.find(chunk => chunk >= travel.residentStart && chunk < travel.residentStart + residentCount)
  if (cacheChunk !== undefined) {
    const localChunk = cacheChunk - travel.residentStart
    const x = localChunk * chunkWidth + Math.floor(chunkWidth * .62)
    floor.routeCache = { linkId: travel.linkId, x, y: routeY(travel, chunkWidth, cacheChunk, Math.floor(chunkWidth * .62), center) }
  }
  floor.guardianDefeated = true
  floor.objective = { id: `link:${travel.linkId}`, kind: 'recoverSupplies', status: 'complete', label: 'Reach the far airlock' }
  state.travel = travel
  state.hero.x = start.x
  state.hero.y = start.y
  state.messages = [`Link ${travel.linkId} streams through ${travel.chunkCount} physical partitions.`, `Resident chunks ${travel.residentStart + 1}-${travel.residentStart + residentCount}; route situations: ${travel.situations.slice(travel.residentStart, travel.residentStart + residentCount).join(', ')}.`]
  synchronizePartyActors(state, 'spawn')
  refreshFov(state)
  return state
}

export function advanceTransitWindow(state: RunState): boolean {
  const travel = state.travel
  if (!travel || travel.chunkCount <= 3) return false
  const chunkWidth = Math.floor(state.floor.width / Math.min(3, travel.chunkCount - travel.residentStart))
  const shiftForward = state.hero.x >= chunkWidth * 2 && travel.residentStart + 3 < travel.chunkCount
  const shiftBackward = state.hero.x < chunkWidth && travel.residentStart > 0
  if (!shiftForward && !shiftBackward) return false
  const residentStart = travel.residentStart + (shiftForward ? 1 : -1)
  const remembered = rememberTransitWindow(state)
  if (!remembered) return false
  const nextTravel = { ...remembered, residentStart, activeChunk: Math.max(0, Math.min(travel.chunkCount - 1, residentStart + 1)) }
  const next = newTransitRun(state.seed, state.area ?? state.floor.biome, state.hero, nextTravel, state.rescuedNpcs, [], state.areaOrder, state.campaignCycle, state.companions, state.companionDeathMode)
  state.floor = next.floor
  state.travel = nextTravel
  state.hero.x += shiftForward ? -chunkWidth : chunkWidth
  state.hero.y = Math.max(1, Math.min(state.floor.height - 2, state.hero.y))
  refreshFov(state)
  return true
}

export const newSeededCampaignRun = (seed: number, inheritedHero?: Hero, rescuedNpcs: readonly RescuedNpc[] = [], legacyRecords: readonly LegacyRecord[] = [], cycle: CampaignCycle = initialCampaignCycle(), companions: readonly Companion[] = [], companionDeathMode: CompanionDeathMode = 'injury'): RunState => {
  const areaOrder = campaignOrderForSeed(seed)
  return newRun(seed, areaOrder[0], 0, inheritedHero, rescuedNpcs, legacyRecords, areaOrder, cycle, companions, companionDeathMode)
}
