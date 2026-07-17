import { biomeName } from '../content'
import { generateAreaFloor } from '../world'
import type { Biome, CourierCalling, CourierOrigin, DeathMode, Hero, LegacyRecord, RescuedNpc, RunState } from '../types'
import { refreshFov } from './visibility'
import { hydrateEncyclopediaLegacy } from './encyclopedia'
import { createRunTelemetry } from '../telemetry'

export interface CourierBuild { name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode }

const originStats: Record<CourierOrigin, Hero['stats']> = {
  mineborn: { strength: 3, agility: 1, vitality: 3, intellect: 1 },
  mosswalker: { strength: 1, agility: 3, vitality: 3, intellect: 1 },
  cavernSeeker: { strength: 1, agility: 2, vitality: 2, intellect: 3 }
}

const starterKit = (calling: CourierCalling): Pick<Hero, 'bombs' | 'ropes' | 'inventory' | 'equipment'> => {
  if (calling === 'trailguard') return { bombs: 4, ropes: 4, inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'], equipment: { mainHand: 'whip', offHand: 'buckler' } }
  if (calling === 'pathmaker') return { bombs: 6, ropes: 6, inventory: ['tonic', 'rock', 'bombPack', 'bombPack', 'ropeBundle', 'ropeBundle', 'ember', 'mapScroll'], equipment: { mainHand: 'whip' } }
  return { bombs: 4, ropes: 4, inventory: ['tonic', 'focusTonic', 'rock', 'bombPack', 'ropeBundle', 'ember', 'sight'], equipment: { mainHand: 'whip' } }
}

export const newHero = (build: Partial<CourierBuild> = {}): Hero => {
  const origin = build.origin ?? 'mineborn'
  const calling = build.calling ?? 'trailguard'
  const kit = Object.keys(build).length ? starterKit(calling) : { bombs: 4, ropes: 4, inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'], equipment: { mainHand: 'whip' } }
  return {
    name: build.name?.trim() || 'Existing Courier', origin, calling, deathMode: build.deathMode ?? 'checkpoint',
    x: 0, y: 0, health: 22, maxHealth: 22, focus: 8, maxFocus: 8, gold: 0, bombs: kit.bombs, ropes: kit.ropes, keys: 0, xp: 0, level: 1,
    stats: { ...originStats[origin] }, skills: [], inventory: kit.inventory, equipment: kit.equipment, conditions: [], cooldowns: {}
  }
}

export function newRun(seed = Math.floor(Math.random() * 0x7fffffff), area: Biome = 'mine', areaFloor = 0, inheritedHero?: Hero, rescuedNpcs: readonly RescuedNpc[] = [], legacyRecords: readonly LegacyRecord[] = []): RunState {
  const floor = generateAreaFloor(seed, area, areaFloor)
  const hero = inheritedHero ? structuredClone(inheritedHero) : newHero()
  hero.x = floor.start.x
  hero.y = floor.start.y
  const state: RunState = { version: 3, seed, floor, hero, messages: [`You enter ${biomeName[area]} with the sealed parcel.`, 'H opens help.'], status: 'playing', turn: 0, area, areaFloor, rescuedNpcs: rescuedNpcs.map(npc => ({ ...npc })), lineageEvents: [] }
  hydrateEncyclopediaLegacy(state, legacyRecords)
  state.telemetry = createRunTelemetry(state)
  refreshFov(state)
  return state
}
