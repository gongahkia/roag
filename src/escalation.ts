import { rngFor } from './rng'
import type { AreaArcState, Biome, ExpeditionPhase, Floor, FloorEscalation } from './types'

export const EXPEDITION_PHASES = ['survey', 'pressure', 'counterroute', 'climax'] as const satisfies readonly ExpeditionPhase[]

interface ArcTemplate { id: string; promise: string; landmark: string; encounter: string; ecology: string; payoff: string }

const arcs: Record<Biome, readonly ArcTemplate[]> = {
  mine: [{ id: 'braced-shaft', promise: 'trace the braced shaft', landmark: 'lamp-strung shaft', encounter: 'foreman cordon', ecology: 'support-line collapse', payoff: 'open the supported escape' }, { id: 'black-vein', promise: 'follow the black-vein survey', landmark: 'ore-marked drift', encounter: 'claim-jumper patrol', ecology: 'gas-pocket rupture', payoff: 'break the vein seal' }],
  wilds: [{ id: 'rootwater', promise: 'read the rootwater trail', landmark: 'rootbound ford', encounter: 'canopy hunting pack', ecology: 'nesting surge', payoff: 'drain the drowned hollow' }, { id: 'thornsong', promise: 'follow the thornsong', landmark: 'singing briar arch', encounter: 'bramble skirmishers', ecology: 'webbed migration', payoff: 'silence the thorn chorus' }],
  caverns: [{ id: 'tide-bell', promise: 'follow the tide bell', landmark: 'echoing tide chamber', encounter: 'eel-stalker screen', ecology: 'vent-fire pulse', payoff: 'ring the chamber clear' }, { id: 'deep-lens', promise: 'align the deep lens', landmark: 'crystal lens', encounter: 'fault-tunnel ambush', ecology: 'darkness bloom', payoff: 'focus the cavern light' }],
  ruins: [{ id: 'ward-chain', promise: 'map the broken ward chain', landmark: 'warded colonnade', encounter: 'sentinel cordon', ecology: 'ritual-dust veil', payoff: 'restore the final ward' }, { id: 'sunken-court', promise: 'cross the sunken court', landmark: 'breached tribunal', encounter: 'sightline patrol', ecology: 'dart-gallery flare', payoff: 'unseal the court gate' }],
  furnace: [{ id: 'kiln-heart', promise: 'climb the cold kiln terrace', landmark: 'cold terrace lift', encounter: 'active kiln line', ecology: 'smoke emergency', payoff: 'quench the kiln heart' }, { id: 'slag-route', promise: 'chart the ash gully', landmark: 'slag spillway', encounter: 'terrace pursuers', ecology: 'lift-chain surge', payoff: 'open the kiln gate' }],
  floodedRuins: [{ id: 'floodgate', promise: 'find the old floodgate', landmark: 'anchored sluice', encounter: 'current-rider pack', ecology: 'tide pull', payoff: 'raise the floodgate' }, { id: 'drowned-archive', promise: 'recover the drowned archive', landmark: 'archive island', encounter: 'reef ambush', ecology: 'undertow surge', payoff: 'surface the archive' }],
  cliffs: [{ id: 'windline', promise: 'hold the windline', landmark: 'anchor shelf', encounter: 'ledge hunter screen', ecology: 'crosswind burst', payoff: 'secure the high traverse' }, { id: 'ravine-call', promise: 'answer the ravine call', landmark: 'rope bridge', encounter: 'switchback patrol', ecology: 'gust-front shift', payoff: 'bind the ravine route' }],
  burial: [{ id: 'ancestor-road', promise: 'read the ancestor road', landmark: 'procession cairn', encounter: 'grave guardian line', ecology: 'spirit migration', payoff: 'settle the procession' }, { id: 'mound-oath', promise: 'keep the mound oath', landmark: 'oathstone ring', encounter: 'ossuary ambush', ecology: 'grave-light drift', payoff: 'close the oath circle' }],
  saltFlats: [{ id: 'white-road', promise: 'follow the white road', landmark: 'mirror caravan', encounter: 'brine stalker screen', ecology: 'salt-haze roll', payoff: 'mark the caravan crossing' }, { id: 'brine-lens', promise: 'align the brine lens', landmark: 'fractured salt lens', encounter: 'mirror skirmishers', ecology: 'brine mirage', payoff: 'clear the reflected route' }],
  frostReliquary: [{ id: 'winter-seal', promise: 'trace the winter seal', landmark: 'rime reliquary', encounter: 'frost hunter patrol', ecology: 'rime nesting', payoff: 'break the winter seal' }, { id: 'glacier-vow', promise: 'honor the glacier vow', landmark: 'frozen vowstone', encounter: 'ice-duel guard', ecology: 'whiteout shift', payoff: 'wake the glacier path' }]
}

const phaseTopology: Record<ExpeditionPhase, string> = { survey: 'oriented approach', pressure: 'contested fork', counterroute: 'reversed route', climax: 'resolved arena' }
const selectedArc = (seed: number, biome: Biome): { arc: ArcTemplate; index: number } => {
  const index = rngFor(seed, 'generation', 'expedition-arc', biome).int(0, arcs[biome].length - 1)
  return { arc: arcs[biome][index], index }
}

export const escalationFor = (seed: number, biome: Biome, areaFloor: number): FloorEscalation => {
  if (!Number.isInteger(areaFloor) || areaFloor < 0 || areaFloor >= EXPEDITION_PHASES.length) throw new Error(`invalid escalation floor: ${areaFloor}`)
  const { arc, index } = selectedArc(seed, biome)
  const phase = EXPEDITION_PHASES[areaFloor]
  return { arcId: arc.id, phase, topology: phaseTopology[phase], landmark: arc.landmark, encounter: arc.encounter, ecology: arc.ecology, promise: arc.promise, payoff: arc.payoff, encounterOffset: index * 2, carried: [] }
}

export const areaArcStateFor = (seed: number, biome: Biome): AreaArcState => ({ biome, arcId: selectedArc(seed, biome).arc.id, clearedPhases: [] })
export const recordAreaArcPhase = (arc: AreaArcState, phase: ExpeditionPhase): void => { if (!arc.clearedPhases.includes(phase)) arc.clearedPhases.push(phase) }
export const applyAreaArcState = (floor: Floor, arc: AreaArcState): void => {
  const escalation = floor.escalation
  if (!escalation || escalation.arcId !== arc.arcId || floor.biome !== arc.biome) return
  escalation.carried = EXPEDITION_PHASES.filter(phase => arc.clearedPhases.includes(phase))
  if (!escalation.carried.length) return
  const history = escalation.carried.join(' → ')
  floor.objective.label = `${floor.objective.label} [carried: ${history}]`
  for (const ecology of floor.ecology ?? []) ecology.responses = [`Use the ${history} route.`, ...ecology.responses]
}
