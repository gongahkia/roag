import { biomeName } from '../content'
import { generateAreaFloor } from '../world'
import type { Biome, CourierCalling, CourierOrigin, DeathMode, Hero, LegacyRecord, RescuedNpc, RunState } from '../types'
import { refreshFov } from './visibility'
import { hydrateEncyclopediaLegacy } from './encyclopedia'
import { createRunTelemetry } from '../telemetry'
import { recordSafePosition } from './buildcraft'
import { DEFAULT_AREA_ORDER, campaignOrderForSeed } from './campaign'

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

export function newRun(seed = Math.floor(Math.random() * 0x7fffffff), area: Biome = 'mine', areaFloor = 0, inheritedHero?: Hero, rescuedNpcs: readonly RescuedNpc[] = [], legacyRecords: readonly LegacyRecord[] = [], areaOrder: readonly Biome[] = DEFAULT_AREA_ORDER): RunState {
  const routePosition = Math.max(0, areaOrder.indexOf(area))
  const floor = generateAreaFloor(seed, area, areaFloor, routePosition)
  const hero = inheritedHero ? structuredClone(inheritedHero) : newHero()
  if (inheritedHero && areaFloor === 0 && routePosition > 0) hero.gold = Math.min(500, hero.gold + (hero.boons?.windfall ?? 0) * 20)
  hero.x = floor.start.x
  hero.y = floor.start.y
  const state: RunState = { version: 5, seed, floor, hero, messages: [`A route marker names ${biomeName[area]}.`, 'The lodge ledger lists H for help.'], status: 'playing', turn: 0, area, areaFloor, areaOrder: [...areaOrder], rescuedNpcs: rescuedNpcs.map(npc => ({ ...npc })), lineageEvents: [], alignment: { kami: 0, villagePact: 0 } }
  hydrateEncyclopediaLegacy(state, legacyRecords)
  state.telemetry = createRunTelemetry(state)
  refreshFov(state)
  recordSafePosition(state)
  return state
}

export const newSeededCampaignRun = (seed: number, inheritedHero?: Hero, rescuedNpcs: readonly RescuedNpc[] = [], legacyRecords: readonly LegacyRecord[] = []): RunState => {
  const areaOrder = campaignOrderForSeed(seed)
  return newRun(seed, areaOrder[0], 0, inheritedHero, rescuedNpcs, legacyRecords, areaOrder)
}
