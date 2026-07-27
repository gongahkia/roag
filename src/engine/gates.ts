import { DIRECTIONS, type Alignment, type Biome, type LineageEvent, type RescuedNpc, type RunState } from '../types'
import { AREA_GATES, gateForArea, validateAreaGate, type AreaGate, type GateAlternative, type GateCost, type GateDestination } from '../area-gates'
import { biomeName } from '../content'
import { nextArea } from './campaign'
import { getTile } from '../world'
import { hasAstralGateAccess } from './intellect'
import { grantGold, spendGold } from './economy'
import { boonRank } from './buildcraft'

export { AREA_GATES, gateForArea, validateAreaGate }
export type { AreaGate, GateAlternative, GateCost, GateDestination }

export interface GateResolution { resolved: boolean; destination?: Biome; sacrificedNpc?: RescuedNpc; lineageEvent?: LineageEvent; alignment?: Alignment; message: string }

export const gateForRun = (state: Pick<RunState, 'area' | 'floor' | 'areaOrder'>): AreaGate | undefined => {
  const biome = state.area ?? state.floor.biome
  const destination = nextArea(biome, state.areaOrder)
  if (!destination) return undefined
  const gate = gateForArea(biome, destination)
  const surcharge = (state.floor.difficulty?.threat ?? 0) * 5
  const alternatives = [...gate.tagAlternatives]
  if (!alternatives.some(option => option.kind === 'body')) alternatives.push({ label: 'pay in breath and blood', kind: 'body', tags: ['body'], cost: { gold: 0, items: [] } })
  if (!alternatives.some(option => option.kind === 'oath')) alternatives.push({ label: 'take an oath burden', kind: 'oath', tags: ['oath'], cost: { gold: 0, items: [] } })
  return { ...gate, cost: { ...gate.cost, gold: gate.cost.gold + surcharge }, tagAlternatives: alternatives.map(option => option.cost ? { ...option, cost: { ...option.cost, gold: option.cost.gold + surcharge } } : option) }
}

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
  if (tag === 'lift') return items.some(item => ['liftKey', 'liftHook', 'chainGuard'].includes(item)) || state.hero.traversalTools?.includes('cordAnchor') === true
  if (tag === 'anchor') return items.some(item => ['anchorSpool', 'anchorBlade', 'anchorBuckler'].includes(item)) || state.hero.ropes > 0
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
  if (alternative.kind === 'body' && state.hero.maxHealth <= 8) return { resolved: false, message: 'You need more than 8 maximum health for this passage.' }
  if (alternative.kind === 'oath' && (state.hero.oaths?.length ?? 0) >= 3) return { resolved: false, message: 'Too many active oaths already bind this courier.' }
  if (alternative.kind === 'tag' && !alternative.tags.every(tag => hasGateTag(state, tag))) return { resolved: false, message: `Required tags missing: ${alternative.tags.join(' + ')}.` }
  if (alternative.kind === 'bomb' && state.hero.bombs < 1) return { resolved: false, message: 'A bomb is required.' }
  spendGold(state, cost.gold)
  for (const item of cost.items) state.hero.inventory.splice(state.hero.inventory.indexOf(item), 1)
  if (alternative.kind === 'bomb') state.hero.bombs--
  const sacrificedNpc = alternative.kind === 'npc' ? state.rescuedNpcs!.shift()! : undefined
  if (alternative.kind === 'body') {
    state.hero.maxHealth -= 4
    state.hero.health = Math.min(state.hero.health, state.hero.maxHealth)
    grantGold(state, 35 + boonRank(state, 'cairnPact') * 18)
  }
  if (alternative.kind === 'oath') {
    const id = (['noHealing', 'noBombs', 'noCharms'] as const)[(state.seed + state.floor.index + state.turn) % 3]
    state.hero.oaths = [...(state.hero.oaths ?? []), { id, remainingFloors: 2 }]
    grantGold(state, 35 + boonRank(state, 'cairnPact') * 18)
  }
  openNearbyGate(state)
  state.gateDestination = gate.unlockedDestination.biome
  const alignment: Alignment = alternative.kind === 'body' || alternative.kind === 'oath' || alternative.tags.some(tag => ['ward', 'astral', 'relic', 'script', 'arcane'].includes(tag)) ? 'kami' : 'villagePact'
  return { resolved: true, destination: gate.unlockedDestination.biome, sacrificedNpc, alignment, message: `${biomeName[gate.unlockedDestination.biome]} trail opened.` }
}

export const gateModalLines = (gate: AreaGate, choice?: number, confirming = false): string[] => {
  const selected = choice === undefined ? undefined : gate.tagAlternatives[choice]
  const choiceCost = selected?.cost ?? gate.cost
  const cost = `${choiceCost.gold} cash${choiceCost.items.length ? ` + ${choiceCost.items.join(', ')}` : ''}`
  const destination = `${biomeName[gate.unlockedDestination.biome]} stage ${gate.unlockedDestination.floor + 1}`
  const requirement = (option: GateAlternative): string => option.kind === 'npc' ? 'leave one companion behind for this run' : option.kind === 'body' ? 'lose 4 maximum HP; gain cash' : option.kind === 'oath' ? 'two-floor oath; gain cash' : option.tags.join(' + ')
  if (!selected) return [...gate.tagAlternatives.map((option, index) => `${index + 1}. ${option.label}: ${requirement(option)}`), `FINAL: pay ${cost}; open ${destination}.`, 'number chooses · Esc cancels']
  return [`CHOICE: ${selected.label} (${requirement(selected)})`, `FINAL: pay ${cost}; open ${destination}.`, confirming ? 'ENTER confirms this final passage choice.' : 'ENTER reviews confirmation · number changes choice']
}
