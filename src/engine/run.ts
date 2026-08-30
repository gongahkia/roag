import { biomeName } from '../content'
import { generateAreaFloor } from '../world'
import type { Biome, CampaignCycle, Companion, CompanionDeathMode, CourierCalling, CourierOrigin, DeathMode, Hero, LegacyRecord, RescuedNpc, RunState } from '../types'
import { refreshFov } from './visibility'
import { hydrateEncyclopediaLegacy } from './encyclopedia'
import { createRunTelemetry } from '../telemetry'
import { recordSafePosition } from './buildcraft'
import { cloneCampaignCycle, DEFAULT_AREA_ORDER, initialCampaignCycle, campaignOrderForSeed } from './campaign'
import { applyAreaArcState, areaArcStateFor } from '../escalation'
import { emptySocialReputation } from '../social-contract'
import { cloneCompanions } from './companions'
import { synchronizePartyActors } from './party'

export interface CourierBuild { name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode }

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
  floor.tiles = Array.from({ length: floor.width * floor.height }, () => ({ kind: 'wall' as const, explored: false, visible: false }))
  const center = Math.floor(floor.height / 2)
  const carve = (x: number, y: number, radius = 2) => {
    for (let dy = -radius; dy <= radius; dy++) for (let dx = -1; dx <= 1; dx++) {
      const px = x + dx
      const py = y + dy
      if (px > 0 && px < floor.width - 1 && py > 0 && py < floor.height - 1) floor.tiles[py * floor.width + px]!.kind = 'floor'
    }
  }
  const routeOffset = [...travel.linkId].reduce((total, char) => total + char.charCodeAt(0), 0)
  for (let x = 2; x < floor.width - 2; x++) {
    const localChunk = Math.min(residentCount - 1, Math.floor(x / chunkWidth))
    const chunk = travel.residentStart + localChunk
    const y = center + Math.round(Math.sin((x + routeOffset + travel.residentStart * chunkWidth) / 7) * 3)
    carve(x, y)
    if (x % 13 === 0) for (let branch = 1; branch < 7; branch++) carve(x, y + (x % 26 ? branch : -branch), 1)
    const situation = travel.situations[chunk]
    if (situation === 'hazard' && x % 19 === 0) floor.tiles[y * floor.width + x]!.kind = 'gas'
    if (situation === 'ecology' && x % 23 === 0) floor.tiles[y * floor.width + x]!.kind = 'bramble'
  }
  const start = { x: 2, y: center }
  const exit = { x: floor.width - 3, y: center + Math.round(Math.sin((floor.width - 3 + routeOffset + travel.residentStart * chunkWidth) / 7) * 3) }
  floor.tiles[start.y * floor.width + start.x]!.kind = 'floor'
  floor.tiles[exit.y * floor.width + exit.x]!.kind = 'exit'
  floor.start = start
  floor.exit = exit
  floor.layoutId = 'voyager-link-corridor'
  floor.actors = []
  floor.items = []
  floor.props = []
  floor.encounters = []
  floor.milestones = []
  floor.sideSpaces = []
  floor.secretRooms = []
  floor.secretRoutes = []
  floor.ecology = []
  for (let localChunk = 0; localChunk < residentCount; localChunk++) {
    const chunk = travel.residentStart + localChunk
    const x = Math.min(floor.width - 5, localChunk * chunkWidth + Math.floor(chunkWidth * .62))
    const y = center + Math.round(Math.sin((x + routeOffset) / 7) * 3)
    if (travel.situations[chunk] === 'patrol' && patrolTemplate) floor.actors.push({ ...patrolTemplate, id: `route-patrol:${travel.linkId}:${chunk}`, x, y, health: patrolTemplate.maxHealth, maxHealth: patrolTemplate.maxHealth })
    if (travel.situations[chunk] === 'trader') floor.actors.push({ id: `route-trader:${travel.linkId}:${chunk}`, role: 'merchant', kind: 'merchant', name: 'route trader', x, y, health: 1, maxHealth: 1, attack: 0, defense: 0, speed: 0, energy: 0, glyph: '$', color: '#f4d26a', hostile: false })
  }
  floor.guardianDefeated = true
  floor.objective = { id: `link:${travel.linkId}`, kind: 'recoverSupplies', status: 'complete', label: 'Reach the far airlock' }
  state.travel = travel
  state.hero.x = start.x
  state.hero.y = start.y
  state.messages = [`Link ${travel.linkId} streams through ${travel.chunkCount} physical partitions.`, `Resident chunks ${travel.residentStart + 1}-${travel.residentStart + residentCount}; route situations: ${travel.situations.slice(travel.residentStart, travel.residentStart + residentCount).join(', ')}.`]
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
  const nextTravel = { ...travel, residentStart, activeChunk: Math.max(0, Math.min(travel.chunkCount - 1, residentStart + 1)) }
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
