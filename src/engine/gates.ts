import { DIRECTIONS, type Biome, type ItemId, type LineageEvent, type Point, type RescuedNpc, type RunState } from '../types'
import { biomeName } from '../content'
import { getTile } from '../world'
import { hasAstralGateAccess } from './intellect'
import { spendGold } from './economy'

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

export interface GateResolution { resolved: boolean; destination?: Biome; sacrificedNpc?: RescuedNpc; lineageEvent?: LineageEvent; message: string }

const hasFireTag = (state: RunState): boolean => state.hero.inventory.some(item => item === 'fireJar' || item === 'ember')
const hasNpcOffering = (state: RunState): boolean => Boolean(state.rescuedNpcs?.length)
const hasGateTag = (state: RunState, tag: string): boolean => {
  const items = [...state.hero.inventory, ...Object.values(state.hero.equipment).filter((item): item is string => Boolean(item))]
  if (tag === 'fire') return hasFireTag(state)
  if (tag === 'light') return items.includes('lantern') || state.hero.inventory.includes('sight')
  if (tag === 'rope') return state.hero.ropes > 0 || items.includes('ropeBundle')
  if (tag === 'mobility') return items.some(item => ['blinkRune', 'boots', 'featherboots'].includes(item)) || state.hero.skills.some(skill => skill.startsWith('agi'))
  if (tag === 'ward') return items.some(item => ['ward', 'wardScript'].includes(item))
  if (tag === 'astral') return state.hero.skills.some(skill => skill.startsWith('astral')) || hasAstralGateAccess(state.hero)
  if (tag === 'relic') return items.includes('sunseal')
  if (tag === 'script' || tag === 'arcane') return items.some(item => ['ember', 'mend', 'sight', 'root', 'waterScript', 'lull', 'blink', 'gust', 'pull', 'wardScript', 'gate'].includes(item))
  if (tag === 'rubble') return items.includes('pickaxe') || state.hero.bombs > 0
  if (tag === 'piercing') return items.some(item => ['spear', 'pickaxe'].includes(item))
  return false
}
const openNearbyGate = (state: RunState): void => {
  for (const delta of Object.values(DIRECTIONS)) {
    const tile = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)
    if (tile?.kind === 'lockedDoor') tile.kind = 'floor'
  }
}

export const resolveAreaGate = (state: RunState, gate: AreaGate, choice: number): GateResolution => {
  const alternative = gate.tagAlternatives[choice]
  if (!alternative) return { resolved: false, message: 'Invalid gate alternative.' }
  const cost = alternative.cost ?? gate.cost
  if (state.hero.gold < cost.gold) return { resolved: false, message: 'Insufficient beads for this passage.' }
  if (!cost.items.every(item => state.hero.inventory.includes(item))) return { resolved: false, message: 'Required gate item is missing.' }
  if (alternative.kind === 'npc' && !hasNpcOffering(state)) return { resolved: false, message: 'A rescued NPC is required.' }
  if (alternative.kind === 'tag' && !alternative.tags.every(tag => hasGateTag(state, tag))) return { resolved: false, message: `Required tags missing: ${alternative.tags.join(' + ')}.` }
  if (alternative.kind === 'bomb' && state.hero.bombs < 1) return { resolved: false, message: 'A bomb is required.' }
  spendGold(state, cost.gold)
  for (const item of cost.items) state.hero.inventory.splice(state.hero.inventory.indexOf(item), 1)
  if (alternative.kind === 'bomb') state.hero.bombs--
  const sacrificedNpc = alternative.kind === 'npc' ? state.rescuedNpcs!.shift()! : undefined
  const lineageEvent = sacrificedNpc ? { id: `sacrifice:${gate.id}:${sacrificedNpc.id}`, kind: 'npcSacrifice' as const, npcId: sacrificedNpc.id, npcName: sacrificedNpc.name, biome: state.area ?? state.floor.biome, floor: state.areaFloor ?? state.floor.index % 4, gateId: gate.id, seed: state.seed } : undefined
  if (lineageEvent && !(state.lineageEvents ?? []).some(event => event.id === lineageEvent.id)) state.lineageEvents = [...(state.lineageEvents ?? []), lineageEvent].slice(-12)
  openNearbyGate(state)
  state.gateDestination = gate.unlockedDestination.biome
  return { resolved: true, destination: gate.unlockedDestination.biome, sacrificedNpc, lineageEvent, message: `${biomeName[gate.unlockedDestination.biome]} trail opened.` }
}

export const gateModalLines = (gate: AreaGate, choice?: number, confirming = false): string[] => {
  const selected = choice === undefined ? undefined : gate.tagAlternatives[choice]
  const choiceCost = selected?.cost ?? gate.cost
  const cost = `${choiceCost.gold} beads${choiceCost.items.length ? ` + ${choiceCost.items.join(', ')}` : ''}`
  const destination = `${biomeName[gate.unlockedDestination.biome]} stage ${gate.unlockedDestination.floor + 1}`
  const requirement = (option: GateAlternative): string => option.kind === 'npc' ? 'leave one companion behind' : option.tags.join(' + ')
  if (!selected) return [...gate.tagAlternatives.map((option, index) => `${index + 1}. ${option.label}: ${requirement(option)}`), `FINAL: pay ${cost}; open ${destination}.`, 'number chooses · Esc cancels']
  return [`CHOICE: ${selected.label} (${requirement(selected)})`, `FINAL: pay ${cost}; open ${destination}.`, confirming ? 'ENTER confirms this final passage choice.' : 'ENTER reviews confirmation · number changes choice']
}
