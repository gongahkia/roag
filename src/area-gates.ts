import type { Biome, ItemId, Point } from './types'

export interface GateCost { gold: number; items: ItemId[] }
export interface GateAlternative { label: string; kind: 'npc' | 'tag' | 'bomb'; tags: string[]; cost?: GateCost }
export interface GateDestination { biome: Biome; floor: number; point: Point }
export interface AreaGate { id: string; biome: Biome; npcOffering: string; tagAlternatives: GateAlternative[]; cost: GateCost; unlockedDestination: GateDestination }

export const AREA_GATES: Record<Biome, AreaGate> = {
  mine: { id: 'mine-wilds-pass', biome: 'mine', npcOffering: 'A companion can hold the trail to Cedar Wilds.', tagAlternatives: [{ label: 'leave a companion to guide', kind: 'npc', tags: ['npc'], cost: { gold: 0, items: [] } }, { label: 'burn the thicket', kind: 'tag', tags: ['fire'], cost: { gold: 20, items: [] } }, { label: 'clear a breach', kind: 'bomb', tags: ['bomb'], cost: { gold: 8, items: [] } }], cost: { gold: 20, items: [] }, unlockedDestination: { biome: 'wilds', floor: 0, point: { x: 2, y: 2 } } },
  wilds: { id: 'wilds-caverns-pass', biome: 'wilds', npcOffering: 'A companion can hold the Sea Caves crossing.', tagAlternatives: [{ label: 'leave a companion to guide', kind: 'npc', tags: ['npc'], cost: { gold: 0, items: [] } }, { label: 'light and rope', kind: 'tag', tags: ['light', 'rope'], cost: { gold: 25, items: [] } }, { label: 'find a safe crossing', kind: 'tag', tags: ['mobility'], cost: { gold: 15, items: [] } }], cost: { gold: 25, items: [] }, unlockedDestination: { biome: 'caverns', floor: 0, point: { x: 2, y: 2 } } },
  caverns: { id: 'caverns-ruins-pass', biome: 'caverns', npcOffering: 'A companion can hold the Stone Circle path.', tagAlternatives: [{ label: 'leave a companion to guide', kind: 'npc', tags: ['npc'], cost: { gold: 0, items: [] } }, { label: 'ward and sky charm', kind: 'tag', tags: ['ward', 'astral'], cost: { gold: 40, items: [] } }, { label: 'sunstone seal', kind: 'tag', tags: ['relic'], cost: { gold: 0, items: ['sunseal'] } }], cost: { gold: 40, items: [] }, unlockedDestination: { biome: 'ruins', floor: 0, point: { x: 2, y: 2 } } },
  ruins: { id: 'ruins-seal', biome: 'ruins', npcOffering: 'The distant keeper offers the final passage.', tagAlternatives: [{ label: 'spirit charm', kind: 'tag', tags: ['ward'] }, { label: 'ritual charm', kind: 'tag', tags: ['script', 'arcane'] }], cost: { gold: 75, items: [] }, unlockedDestination: { biome: 'ruins', floor: 3, point: { x: 45, y: 32 } } }
}

export const gateForArea = (biome: Biome): AreaGate => AREA_GATES[biome]
const GATE_TAGS = new Set(['fire', 'light', 'rope', 'mobility', 'ward', 'astral', 'relic', 'script', 'arcane', 'rubble', 'piercing'])
export const validateAreaGate = (gate: AreaGate): string[] => {
  const errors: string[] = []
  if (!gate.id || !gate.npcOffering) errors.push('missing gate identity')
  if (!gate.tagAlternatives.length) errors.push('no gate alternatives')
  if (!gate.tagAlternatives.some(alternative => alternative.kind === 'npc' || alternative.kind === 'bomb' || alternative.tags.every(tag => GATE_TAGS.has(tag)))) errors.push('no possible gate alternative')
  if (!gate.unlockedDestination || gate.unlockedDestination.floor < 0 || !Number.isInteger(gate.unlockedDestination.floor)) errors.push('invalid gate destination')
  for (const alternative of gate.tagAlternatives) if (alternative.kind === 'tag' && !alternative.tags.every(tag => GATE_TAGS.has(tag))) errors.push(`unknown gate tag: ${alternative.tags.join(' + ')}`)
  return errors
}
