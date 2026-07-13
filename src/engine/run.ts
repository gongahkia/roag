import { biomeName } from '../content'
import { generateAreaFloor } from '../world'
import type { Biome, Hero, LegacyRecord, RescuedNpc, RunState } from '../types'
import { refreshFov } from './visibility'
import { hydrateEncyclopediaLegacy } from './encyclopedia'

export const newHero = (): Hero => ({
  x: 0, y: 0, health: 22, maxHealth: 22, focus: 8, maxFocus: 8, gold: 0, bombs: 4, ropes: 4, keys: 0, xp: 0, level: 1,
  stats: { strength: 2, agility: 2, vitality: 2, intellect: 2 }, skills: [], inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'], equipment: { mainHand: 'whip' }, conditions: [], cooldowns: {}
})

export function newRun(seed = Math.floor(Math.random() * 0x7fffffff), area: Biome = 'mine', areaFloor = 0, inheritedHero?: Hero, rescuedNpcs: readonly RescuedNpc[] = [], legacyRecords: readonly LegacyRecord[] = []): RunState {
  const floor = generateAreaFloor(seed, area, areaFloor)
  const hero = inheritedHero ? structuredClone(inheritedHero) : newHero()
  hero.x = floor.start.x
  hero.y = floor.start.y
  const state: RunState = { version: 2, seed, floor, hero, messages: [`You enter ${biomeName[area]} with the sealed parcel.`, 'H opens help.'], status: 'playing', turn: 0, area, areaFloor, rescuedNpcs: rescuedNpcs.map(npc => ({ ...npc })), lineageEvents: [] }
  hydrateEncyclopediaLegacy(state, legacyRecords)
  refreshFov(state)
  return state
}
