import { DIRECTIONS, type Alignment, type Biome, type Companion, type LineageEvent, type RescuedNpc, type RunState } from '../types'
import { AREA_GATES, gateForArea, validateAreaGate, type AreaGate, type GateAlternative, type GateCost, type GateDestination } from '../area-gates'
import { biomeName } from '../content'
import { nextArea } from './campaign'
import { getTile } from '../world'
import { hasAstralGateAccess } from './intellect'
import { grantGold, spendGold } from './economy'
import { boonRank } from './buildcraft'
import { injureCompanion, loseCompanion } from './companions'
import { activeCompanionRoster, synchronizePartyActors } from './party'

export { AREA_GATES, gateForArea, validateAreaGate }
export type { AreaGate, GateAlternative, GateCost, GateDestination }

export type GateSacrificeConsequence = 'injury' | 'permanent'
export interface GateSacrificeCandidate { id: string; kind: 'companion' | 'legacyNpc'; name: string; role?: Companion['role']; rescueId: string }
export interface GateResolution { resolved: boolean; destination?: Biome; sacrificedNpc?: RescuedNpc; sacrificedCompanion?: Companion; consequence?: GateSacrificeConsequence; lineageEvent?: LineageEvent; alignment?: Alignment; message: string }

export const gateAlternativesForRun = (gate: AreaGate, surcharge = 0): GateAlternative[] => {
  const alternatives = [...gate.tagAlternatives]
  if (!alternatives.some(option => option.kind === 'body')) alternatives.push({ label: 'pay in breath and blood', kind: 'body', tags: ['body'], cost: { gold: 0, items: [] } })
  if (!alternatives.some(option => option.kind === 'oath')) alternatives.push({ label: 'take an oath burden', kind: 'oath', tags: ['oath'], cost: { gold: 0, items: [] } })
  return alternatives.map(option => option.cost ? { ...option, cost: { ...option.cost, gold: option.cost.gold + surcharge } } : option)
}

export const gateForRun = (state: Pick<RunState, 'area' | 'floor' | 'areaOrder'>): AreaGate | undefined => {
  const biome = state.area ?? state.floor.biome
  const destination = nextArea(biome, state.areaOrder)
  if (!destination) return undefined
  const gate = gateForArea(biome, destination)
  const surcharge = (state.floor.difficulty?.threat ?? 0) * 5
  return { ...gate, cost: { ...gate.cost, gold: gate.cost.gold + surcharge }, tagAlternatives: gateAlternativesForRun(gate, surcharge) }
}

const hasFireTag = (state: RunState): boolean => state.hero.inventory.some(item => item === 'fireJar' || item === 'ember')
export const gateSacrificeCandidates = (state: RunState): GateSacrificeCandidate[] => {
  const rescues = state.rescuedNpcs ?? []
  const companions = activeCompanionRoster(state.companions ?? []).filter(companion => rescues.some(rescue => rescue.id === companion.recruitment.rescueId))
  const represented = new Set((state.companions ?? []).map(companion => companion.recruitment.rescueId))
  const legacy = rescues.filter(rescue => !represented.has(rescue.id)).map(rescue => ({ id: `legacy:${rescue.id}`, kind: 'legacyNpc' as const, name: rescue.name, rescueId: rescue.id }))
  return [...companions.map(companion => ({ id: companion.id, kind: 'companion' as const, name: companion.name, role: companion.role, rescueId: companion.recruitment.rescueId })), ...legacy].sort((left, right) => left.name.localeCompare(right.name) || left.id.localeCompare(right.id))
}

export const gateSacrificeConsequence = (state: RunState, candidate: GateSacrificeCandidate): GateSacrificeConsequence => candidate.kind === 'legacyNpc' || state.companionDeathMode === 'permadeath' ? 'permanent' : 'injury'
const hasNpcOffering = (state: RunState): boolean => gateSacrificeCandidates(state).length > 0
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

const sacrificeEvent = (state: RunState, gate: AreaGate, npc: RescuedNpc): LineageEvent => ({ id: `sacrifice:${gate.id}:${npc.id}`, kind: 'npcSacrifice', npcId: npc.id, npcName: npc.name, biome: state.area ?? state.floor.biome, floor: state.floor.index, gateId: gate.id, seed: state.seed })

export const resolveAreaGate = (state: RunState, gate: AreaGate, choice: number, offeringId?: string): GateResolution => {
  const alternative = gate.tagAlternatives[choice]
  if (!alternative) return { resolved: false, message: 'Invalid gate alternative.' }
  const candidates = alternative.kind === 'npc' ? gateSacrificeCandidates(state) : []
  const candidate = alternative.kind === 'npc' ? candidates.find(current => current.id === offeringId) ?? (offeringId === undefined && candidates.length === 1 && candidates[0]!.kind === 'legacyNpc' ? candidates[0] : undefined) : undefined
  if (alternative.kind === 'npc' && !candidate) return { resolved: false, message: candidates.length ? 'Select an active companion or listed legacy NPC.' : 'No eligible companion can hold this passage.' }
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
  const sacrificedNpc = alternative.kind === 'npc' ? state.rescuedNpcs!.find(npc => npc.id === candidate!.rescueId) : undefined
  if (alternative.kind === 'npc' && !sacrificedNpc) return { resolved: false, message: 'That companion record is unavailable.' }
  const consequence = candidate ? gateSacrificeConsequence(state, candidate) : undefined
  let sacrificedCompanion: Companion | undefined
  let lineageEvent: LineageEvent | undefined
  if (candidate?.kind === 'companion') {
    const companion = state.companions?.find(current => current.id === candidate.id)
    if (!companion || companion.rosterStatus !== 'active' || companion.injury !== 'healthy' || companion.permanentlyLost) return { resolved: false, message: 'That companion is unavailable for sacrifice.' }
    if (consequence === 'permanent') loseCompanion(companion)
    else injureCompanion(companion)
    sacrificedCompanion = structuredClone(companion)
    synchronizePartyActors(state, 'removal')
  }
  if (candidate?.kind === 'legacyNpc') state.rescuedNpcs = state.rescuedNpcs!.filter(npc => npc.id !== candidate.rescueId)
  if (candidate?.kind === 'companion' && consequence === 'permanent') {
    state.rescuedNpcs = state.rescuedNpcs!.filter(npc => npc.id !== candidate.rescueId)
    lineageEvent = sacrificeEvent(state, gate, sacrificedNpc!)
    if (!state.lineageEvents?.some(event => event.id === lineageEvent!.id)) state.lineageEvents = [...(state.lineageEvents ?? []), lineageEvent]
  }
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
  const sacrificeMessage = candidate ? consequence === 'permanent' ? `${candidate.name} is permanently lost holding the passage.` : `${candidate.name} holds the passage and returns injured to the Lodge.` : undefined
  return { resolved: true, destination: gate.unlockedDestination.biome, ...(sacrificedNpc ? { sacrificedNpc } : {}), ...(sacrificedCompanion ? { sacrificedCompanion } : {}), ...(consequence ? { consequence } : {}), ...(lineageEvent ? { lineageEvent } : {}), alignment, message: sacrificeMessage ? `${sacrificeMessage} ${biomeName[gate.unlockedDestination.biome]} trail opened.` : `${biomeName[gate.unlockedDestination.biome]} trail opened.` }
}

export const gateModalLines = (gate: AreaGate, choice?: number, confirming = false, candidates: readonly GateSacrificeCandidate[] = [], offeringId?: string, consequence?: GateSacrificeConsequence): string[] => {
  const selected = choice === undefined ? undefined : gate.tagAlternatives[choice]
  const choiceCost = selected?.cost ?? gate.cost
  const cost = `${choiceCost.gold} cash${choiceCost.items.length ? ` + ${choiceCost.items.join(', ')}` : ''}`
  const destination = `${biomeName[gate.unlockedDestination.biome]} stage ${gate.unlockedDestination.floor + 1}`
  const requirement = (option: GateAlternative): string => option.kind === 'npc' ? 'select an active, healthy companion' : option.kind === 'body' ? 'lose 4 maximum HP; gain cash' : option.kind === 'oath' ? 'two-floor oath; gain cash' : option.tags.join(' + ')
  if (!selected) return [...gate.tagAlternatives.map((option, index) => `${index + 1}. ${option.label}: ${requirement(option)}`), `FINAL: pay ${cost}; open ${destination}.`, 'number chooses · Esc cancels']
  if (selected.kind === 'npc') {
    const offering = candidates.find(candidate => candidate.id === offeringId)
    if (!offering) return [`CHOICE: ${selected.label} (${requirement(selected)})`, candidates.length ? 'SELECT WHO HOLDS THE PASSAGE:' : 'NO ELIGIBLE ACTIVE COMPANION.', ...candidates.slice(0, 5).map((candidate, index) => `${index + 1}. ${candidate.name}${candidate.role ? ` · ${candidate.role.toUpperCase()}` : ' · LEGACY NPC'}`), 'number selects · Backspace changes route · Esc declines']
    const result = consequence === 'permanent' ? 'PERMANENT LOSS — cannot recover or return.' : 'INJURY — benched for Lodge recovery.'
    return [`SACRIFICE: ${offering.name}${offering.role ? ` · ${offering.role.toUpperCase()}` : ' · LEGACY NPC'}`, `CONSEQUENCE: ${result}`, `FINAL: ${offering.name} holds the passage to ${destination}.`, confirming ? 'ENTER confirms this companion sacrifice.' : 'ENTER reviews sacrifice confirmation · Backspace changes route', 'Esc declines and leaves the passage unchanged.']
  }
  return [`CHOICE: ${selected.label} (${requirement(selected)})`, `FINAL: pay ${cost}; open ${destination}.`, confirming ? 'ENTER confirms this final passage choice.' : 'ENTER reviews confirmation · number changes choice']
}
