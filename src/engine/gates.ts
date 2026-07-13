import { DIRECTIONS, type Biome, type ItemId, type Point, type RunState } from '../types'
import { getTile } from '../world'

export interface GateCost { gold: number; items: ItemId[] }
export interface GateAlternative { label: string; kind: 'npc' | 'tag' | 'bomb'; tags: string[] }
export interface GateDestination { biome: Biome; floor: number; point: Point }
export interface AreaGate { id: string; biome: Biome; npcOffering: string; tagAlternatives: GateAlternative[]; cost: GateCost; unlockedDestination: GateDestination }

export const AREA_GATES: Record<Biome, AreaGate> = {
  mine: { id: 'mine-wilds-pass', biome: 'mine', npcOffering: 'A lost ranger offers the Verdant Wilds route.', tagAlternatives: [{ label: 'ranger offering', kind: 'npc', tags: ['npc'] }, { label: 'burn the thicket', kind: 'tag', tags: ['fire'] }, { label: 'blast a breach', kind: 'bomb', tags: ['bomb'] }], cost: { gold: 0, items: [] }, unlockedDestination: { biome: 'wilds', floor: 0, point: { x: 2, y: 2 } } },
  wilds: { id: 'wilds-caverns-pass', biome: 'wilds', npcOffering: 'A river guide offers a Glass Caverns crossing.', tagAlternatives: [{ label: 'guide offering', kind: 'npc', tags: ['npc'] }, { label: 'light and rope', kind: 'tag', tags: ['light', 'rope'] }, { label: 'mobility route', kind: 'tag', tags: ['mobility'] }], cost: { gold: 0, items: [] }, unlockedDestination: { biome: 'caverns', floor: 0, point: { x: 2, y: 2 } } },
  caverns: { id: 'caverns-ruins-pass', biome: 'caverns', npcOffering: 'A surveyor offers an Ashen Ruins lift.', tagAlternatives: [{ label: 'rope descent', kind: 'tag', tags: ['rope'] }, { label: 'crystal breach', kind: 'tag', tags: ['rubble', 'piercing'] }], cost: { gold: 50, items: ['ropeBundle'] }, unlockedDestination: { biome: 'ruins', floor: 0, point: { x: 2, y: 2 } } },
  ruins: { id: 'ruins-seal', biome: 'ruins', npcOffering: 'The archivist offers a sun-seal passage.', tagAlternatives: [{ label: 'ward', kind: 'tag', tags: ['ward'] }, { label: 'script', kind: 'tag', tags: ['script', 'arcane'] }], cost: { gold: 75, items: [] }, unlockedDestination: { biome: 'ruins', floor: 3, point: { x: 45, y: 32 } } }
}

export const gateForArea = (biome: Biome): AreaGate => AREA_GATES[biome]

export interface GateResolution { resolved: boolean; destination?: Biome; message: string }

const hasFireTag = (state: RunState): boolean => state.hero.inventory.some(item => item === 'fireJar' || item === 'ember')
const hasNpcOffering = (state: RunState): boolean => state.floor.actors.some(actor => !actor.hostile && actor.role === 'ally')
const hasGateTag = (state: RunState, tag: string): boolean => {
  const items = [...state.hero.inventory, ...Object.values(state.hero.equipment).filter((item): item is string => Boolean(item))]
  if (tag === 'fire') return hasFireTag(state)
  if (tag === 'light') return items.includes('lantern') || state.hero.inventory.includes('sight')
  if (tag === 'rope') return state.hero.ropes > 0 || items.includes('ropeBundle')
  if (tag === 'mobility') return items.some(item => ['blinkRune', 'boots', 'featherboots'].includes(item)) || state.hero.skills.some(skill => skill.startsWith('agi'))
  if (tag === 'ward') return items.some(item => ['ward', 'wardScript'].includes(item))
  if (tag === 'script' || tag === 'arcane') return items.some(item => ['ember', 'mend', 'sight', 'gust', 'wardScript', 'gate'].includes(item))
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
  if (state.hero.gold < gate.cost.gold) return { resolved: false, message: 'Insufficient gold for this gate.' }
  if (!gate.cost.items.every(item => state.hero.inventory.includes(item))) return { resolved: false, message: 'Required gate item is missing.' }
  if (alternative.kind === 'npc' && !hasNpcOffering(state)) return { resolved: false, message: 'An allied NPC offering is required.' }
  if (alternative.kind === 'tag' && !alternative.tags.every(tag => hasGateTag(state, tag))) return { resolved: false, message: `Required tags missing: ${alternative.tags.join(' + ')}.` }
  if (alternative.kind === 'bomb' && state.hero.bombs < 1) return { resolved: false, message: 'A bomb is required.' }
  state.hero.gold -= gate.cost.gold
  for (const item of gate.cost.items) state.hero.inventory.splice(state.hero.inventory.indexOf(item), 1)
  if (alternative.kind === 'bomb') state.hero.bombs--
  openNearbyGate(state)
  state.gateDestination = gate.unlockedDestination.biome
  return { resolved: true, destination: gate.unlockedDestination.biome, message: `${gate.unlockedDestination.biome} route unlocked.` }
}

export const gateModalLines = (gate: AreaGate, choice?: number, confirming = false): string[] => {
  const selected = choice === undefined ? undefined : gate.tagAlternatives[choice]
  const cost = `${gate.cost.gold} gold${gate.cost.items.length ? ` + ${gate.cost.items.join(', ')}` : ''}`
  const destination = `${gate.unlockedDestination.biome} floor ${gate.unlockedDestination.floor + 1}`
  if (!selected) return [...gate.tagAlternatives.map((option, index) => `${index + 1}. ${option.label}: ${option.tags.join(' + ')}`), `IRREVOCABLE: pay ${cost}; unlock ${destination}.`, 'number chooses · Esc cancels']
  return [`CHOICE: ${selected.label} (${selected.tags.join(' + ')})`, `IRREVOCABLE: pay ${cost}; unlock ${destination}.`, confirming ? 'ENTER confirms this irreversible gate choice.' : 'ENTER reviews confirmation · number changes choice']
}
