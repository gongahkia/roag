import type { Biome, ItemId, Point } from '../types'

export interface GateCost { gold: number; items: ItemId[] }
export interface GateAlternative { label: string; tags: string[] }
export interface GateDestination { biome: Biome; floor: number; point: Point }
export interface AreaGate { id: string; biome: Biome; npcOffering: string; tagAlternatives: GateAlternative[]; cost: GateCost; unlockedDestination: GateDestination }

export const AREA_GATES: Record<Biome, AreaGate> = {
  mine: { id: 'mine-shaft', biome: 'mine', npcOffering: 'The foreman offers a reinforced shaft key.', tagAlternatives: [{ label: 'key', tags: ['key'] }, { label: 'breach', tags: ['rubble', 'bomb'] }], cost: { gold: 20, items: [] }, unlockedDestination: { biome: 'mine', floor: 3, point: { x: 45, y: 32 } } },
  wilds: { id: 'wilds-thicket', biome: 'wilds', npcOffering: 'The ranger offers a thorn-cutting route.', tagAlternatives: [{ label: 'machete', tags: ['cleave'] }, { label: 'fire', tags: ['fire'] }], cost: { gold: 35, items: [] }, unlockedDestination: { biome: 'wilds', floor: 3, point: { x: 45, y: 32 } } },
  caverns: { id: 'caverns-fissure', biome: 'caverns', npcOffering: 'The surveyor offers a crystal bridge route.', tagAlternatives: [{ label: 'rope', tags: ['rope'] }, { label: 'stone', tags: ['rubble', 'piercing'] }], cost: { gold: 50, items: ['ropeBundle'] }, unlockedDestination: { biome: 'caverns', floor: 3, point: { x: 45, y: 32 } } },
  ruins: { id: 'ruins-seal', biome: 'ruins', npcOffering: 'The archivist offers a sun-seal passage.', tagAlternatives: [{ label: 'ward', tags: ['ward'] }, { label: 'script', tags: ['script', 'arcane'] }], cost: { gold: 75, items: [] }, unlockedDestination: { biome: 'ruins', floor: 3, point: { x: 45, y: 32 } } }
}

export const gateForArea = (biome: Biome): AreaGate => AREA_GATES[biome]
