import { DIRECTIONS, type Biome, type LineageEvent, type RescuedNpc, type RunState } from '../types'
import { AREA_GATES, gateForArea, validateAreaGate, type AreaGate, type GateAlternative, type GateCost, type GateDestination } from '../area-gates'
import { biomeName } from '../content'
import { getTile } from '../world'
import { hasAstralGateAccess } from './intellect'
import { spendGold } from './economy'

export { AREA_GATES, gateForArea, validateAreaGate }
export type { AreaGate, GateAlternative, GateCost, GateDestination }

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
  if (tag === 'astral') return hasAstralGateAccess(state.hero)
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
  if (state.hero.gold < cost.gold) return { resolved: false, message: 'Insufficient cash for this passage.' }
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
  const cost = `${choiceCost.gold} cash${choiceCost.items.length ? ` + ${choiceCost.items.join(', ')}` : ''}`
  const destination = `${biomeName[gate.unlockedDestination.biome]} stage ${gate.unlockedDestination.floor + 1}`
  const requirement = (option: GateAlternative): string => option.kind === 'npc' ? 'leave one companion behind' : option.tags.join(' + ')
  if (!selected) return [...gate.tagAlternatives.map((option, index) => `${index + 1}. ${option.label}: ${requirement(option)}`), `FINAL: pay ${cost}; open ${destination}.`, 'number chooses · Esc cancels']
  return [`CHOICE: ${selected.label} (${requirement(selected)})`, `FINAL: pay ${cost}; open ${destination}.`, confirming ? 'ENTER confirms this final passage choice.' : 'ENTER reviews confirmation · number changes choice']
}
