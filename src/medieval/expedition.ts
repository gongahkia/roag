/**
 * The first expedition is deliberately one authored place and one authored
 * threat. This module owns neither world time nor persistence; it only makes
 * the compact state transitions that the world reducer persists.
 */
export const EXPEDITION_STATE_VERSION = 1 as const

export const EXPEDITION_LOADOUTS = [
  { id: 'spear-and-buckler', label: 'Spear + buckler' },
  { id: 'smoke-and-hook', label: 'Smoke cord + hook' },
  { id: 'lantern-and-mallet', label: 'Lantern + mallet' }
] as const
export type ExpeditionLoadoutId = typeof EXPEDITION_LOADOUTS[number]['id']
export type ExpeditionLoadoutChoice = ExpeditionLoadoutId | 'unprepared'

export const EXPEDITION_SUPPORTS = [
  { id: 'quiet-scout', label: 'Quiet scout' },
  { id: 'field-dresser', label: 'Field dresser' },
  { id: 'porter', label: 'Porter' }
] as const
export type ExpeditionSupportId = typeof EXPEDITION_SUPPORTS[number]['id']
export type ExpeditionSupportChoice = ExpeditionSupportId | 'unprepared'

export const HEARTHFORD_CONTACT = {
  id: 'contact:hearthford-mara-venn',
  name: 'Mara Venn',
  adult: true,
  role: 'weir keeper',
  problem: 'A seal cord for the mill race is stranded beyond the marsh hound.'
} as const

export const HEARTHFORD_MAP_ROWS = [
  '###########',
  '#....R..S.#',
  '#.#####...#',
  '#G.M..H...#',
  '#.#####...#',
  '#.........#',
  '###########'
] as const

export const HEARTHFORD_GANGPLANK = { column: 1, row: 3 } as const
export const HEARTHFORD_CONTACT_COORDINATE = { column: 3, row: 3 } as const
export const HEARTHFORD_REED_SCREEN = { column: 5, row: 1 } as const
export const HEARTHFORD_SEAL_CORD = { column: 8, row: 1 } as const
export const HEARTHFORD_HOUND_START = { column: 6, row: 3 } as const

export type ExpeditionCoordinate = { column: number; row: number }
export type ExpeditionLocation = 'jomon' | 'hearthford'
export type ExpeditionObjective = 'unmet' | 'accepted' | 'refused' | 'completed' | 'failed'
export type ExpeditionResource = 'none' | 'carried' | 'delivered' | 'lost'
export type ExpeditionInjury = 'clear' | 'hurt'
export type ExpeditionConsequence = 'none' | 'objective-refused' | 'seal-delivered' | 'escaped-injured' | 'courier-lost'
export type HearthfordThreatStatus = 'dormant' | 'engaged' | 'defeated' | 'evaded'
export type HearthfordThreatIntent = 'watching' | 'advancing' | 'pouncing' | 'recoiling' | 'gone'

export interface HearthfordThreatState {
  status: HearthfordThreatStatus
  health: number
  position: ExpeditionCoordinate
  intent: HearthfordThreatIntent
}

export interface ExpeditionState {
  version: typeof EXPEDITION_STATE_VERSION
  location: ExpeditionLocation
  coordinate: ExpeditionCoordinate
  loadout: ExpeditionLoadoutChoice
  support: ExpeditionSupportChoice
  supportSpent: boolean
  objective: ExpeditionObjective
  resource: ExpeditionResource
  injury: ExpeditionInjury
  noise: number
  startedAtWorldTime: number
  expeditionCount: number
  reedScreen: 'standing' | 'lowered'
  threat: HearthfordThreatState
  consequence: ExpeditionConsequence
}

export type HearthfordMoveDirection = 'north' | 'south' | 'west' | 'east'
export type HearthfordAction = 'attack' | 'brace' | 'lower-reed-screen' | 'evade' | 'retreat'

export type ExpeditionStateTransition =
  | { status: 'changed'; state: ExpeditionState; detail: string }
  | { status: 'blocked'; detail: string }
  | { status: 'courier-death'; state: ExpeditionState; detail: string }

export interface ExpeditionPressure {
  elapsed: number
  depth: number
  noise: number
  valuables: number
  total: number
  band: 'steady' | 'strained' | 'critical'
}

const sameCoordinate = (left: ExpeditionCoordinate, right: ExpeditionCoordinate): boolean => left.column === right.column && left.row === right.row
const distance = (left: ExpeditionCoordinate, right: ExpeditionCoordinate): number => Math.abs(left.column - right.column) + Math.abs(left.row - right.row)
const withinMap = (coordinate: ExpeditionCoordinate): boolean => coordinate.row >= 0
  && coordinate.row < HEARTHFORD_MAP_ROWS.length
  && coordinate.column >= 0
  && coordinate.column < HEARTHFORD_MAP_ROWS[0]!.length

export const isHearthfordWalkable = (coordinate: ExpeditionCoordinate): boolean => withinMap(coordinate)
  && HEARTHFORD_MAP_ROWS[coordinate.row]![coordinate.column] !== '#'

const clone = (state: ExpeditionState): ExpeditionState => structuredClone(state)
const isSafeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const isCoordinate = (value: unknown): value is ExpeditionCoordinate => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
  && Object.keys(value as Record<string, unknown>).sort().join(',') === 'column,row'
  && isSafeInteger((value as ExpeditionCoordinate).column) && isSafeInteger((value as ExpeditionCoordinate).row)
const isLoadout = (value: unknown): value is ExpeditionLoadoutChoice => value === 'unprepared' || EXPEDITION_LOADOUTS.some(item => item.id === value)
const isSupport = (value: unknown): value is ExpeditionSupportChoice => value === 'unprepared' || EXPEDITION_SUPPORTS.some(item => item.id === value)
const isObjective = (value: unknown): value is ExpeditionObjective => ['unmet', 'accepted', 'refused', 'completed', 'failed'].includes(value as string)
const isResource = (value: unknown): value is ExpeditionResource => ['none', 'carried', 'delivered', 'lost'].includes(value as string)
const isInjury = (value: unknown): value is ExpeditionInjury => value === 'clear' || value === 'hurt'
const isConsequence = (value: unknown): value is ExpeditionConsequence => ['none', 'objective-refused', 'seal-delivered', 'escaped-injured', 'courier-lost'].includes(value as string)
const isThreatStatus = (value: unknown): value is HearthfordThreatStatus => ['dormant', 'engaged', 'defeated', 'evaded'].includes(value as string)
const isThreatIntent = (value: unknown): value is HearthfordThreatIntent => ['watching', 'advancing', 'pouncing', 'recoiling', 'gone'].includes(value as string)

/** Exact-shape validation keeps the one saved expedition inspectable and bounded. */
export const isExpeditionState = (value: unknown): value is ExpeditionState => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return false
  const state = value as Record<string, unknown>
  const keys = Object.keys(state).sort()
  const expected = ['version', 'location', 'coordinate', 'loadout', 'support', 'supportSpent', 'objective', 'resource', 'injury', 'noise', 'startedAtWorldTime', 'expeditionCount', 'reedScreen', 'threat', 'consequence'].sort()
  if (keys.length !== expected.length || !keys.every((key, index) => key === expected[index])) return false
  const threat = state.threat as Record<string, unknown>
  return state.version === EXPEDITION_STATE_VERSION
    && (state.location === 'jomon' || state.location === 'hearthford')
    && isCoordinate(state.coordinate)
    && isHearthfordWalkable(state.coordinate)
    && isLoadout(state.loadout)
    && isSupport(state.support)
    && typeof state.supportSpent === 'boolean'
    && isObjective(state.objective)
    && isResource(state.resource)
    && isInjury(state.injury)
    && isSafeInteger(state.noise) && state.noise <= 20
    && isSafeInteger(state.startedAtWorldTime)
    && isSafeInteger(state.expeditionCount) && state.expeditionCount <= 99
    && (state.reedScreen === 'standing' || state.reedScreen === 'lowered')
    && Boolean(threat) && typeof threat === 'object' && !Array.isArray(threat)
    && Object.keys(threat).sort().join(',') === 'health,intent,position,status'
    && isThreatStatus(threat.status)
    && isSafeInteger(threat.health) && threat.health <= 2
    && isCoordinate(threat.position) && isHearthfordWalkable(threat.position)
    && isThreatIntent(threat.intent)
    && isConsequence(state.consequence)
}

export const createExpeditionState = (): ExpeditionState => ({
  version: EXPEDITION_STATE_VERSION,
  location: 'jomon',
  coordinate: { ...HEARTHFORD_GANGPLANK },
  loadout: 'unprepared',
  support: 'unprepared',
  supportSpent: false,
  objective: 'unmet',
  resource: 'none',
  injury: 'clear',
  noise: 0,
  startedAtWorldTime: 0,
  expeditionCount: 0,
  reedScreen: 'standing',
  threat: { status: 'dormant', health: 2, position: { ...HEARTHFORD_HOUND_START }, intent: 'watching' },
  consequence: 'none'
})

export const expeditionPressure = (state: ExpeditionState, worldTime: number): ExpeditionPressure => {
  const elapsed = state.location === 'hearthford' ? Math.max(0, worldTime - state.startedAtWorldTime) : 0
  const depth = state.location === 'hearthford' ? Math.max(0, state.coordinate.column - HEARTHFORD_GANGPLANK.column) : 0
  const valuables = state.resource === 'carried' ? 1 : 0
  const total = elapsed + depth + state.noise + valuables
  return { elapsed, depth, noise: state.noise, valuables, total, band: total >= 17 ? 'critical' : total >= 9 ? 'strained' : 'steady' }
}

export const selectExpeditionLoadout = (state: ExpeditionState, loadout: ExpeditionLoadoutId): ExpeditionState => {
  if (state.location !== 'jomon') throw new Error('loadout selection requires Jomon')
  if (!EXPEDITION_LOADOUTS.some(item => item.id === loadout)) throw new Error('unknown expedition loadout')
  return { ...clone(state), loadout }
}

export const selectExpeditionSupport = (state: ExpeditionState, support: ExpeditionSupportId): ExpeditionState => {
  if (state.location !== 'jomon') throw new Error('support selection requires Jomon')
  if (!EXPEDITION_SUPPORTS.some(item => item.id === support)) throw new Error('unknown expedition support')
  return { ...clone(state), support }
}

export const departForHearthford = (state: ExpeditionState, worldTime: number): ExpeditionState => {
  if (state.location !== 'jomon') throw new Error('courier is already away from Jomon')
  if (state.loadout === 'unprepared' || state.support === 'unprepared') throw new Error('loadout and support preparation are required')
  return {
    ...clone(state),
    location: 'hearthford',
    coordinate: { ...HEARTHFORD_GANGPLANK },
    supportSpent: false,
    injury: 'clear',
    noise: 0,
    startedAtWorldTime: worldTime,
    expeditionCount: state.expeditionCount + 1,
    reedScreen: state.objective === 'completed' ? state.reedScreen : 'standing',
    threat: state.objective === 'completed'
      ? clone(state).threat
      : { status: 'dormant', health: 2, position: { ...HEARTHFORD_HOUND_START }, intent: 'watching' },
    resource: state.objective === 'completed' ? 'delivered' : 'none'
  }
}

const nextCoordinate = (coordinate: ExpeditionCoordinate, direction: HearthfordMoveDirection): ExpeditionCoordinate => ({
  north: { column: coordinate.column, row: coordinate.row - 1 },
  south: { column: coordinate.column, row: coordinate.row + 1 },
  west: { column: coordinate.column - 1, row: coordinate.row },
  east: { column: coordinate.column + 1, row: coordinate.row }
}[direction])

const pressureNoise = (state: ExpeditionState): number => state.support === 'quiet-scout' ? state.noise : Math.min(20, state.noise + 1)
const houndProtectedByReeds = (state: ExpeditionState): boolean => state.reedScreen === 'lowered' && state.coordinate.row === HEARTHFORD_REED_SCREEN.row

const houndStep = (from: ExpeditionCoordinate, courier: ExpeditionCoordinate): ExpeditionCoordinate => {
  const candidates: ExpeditionCoordinate[] = []
  if (courier.row !== from.row) candidates.push({ column: from.column, row: from.row + Math.sign(courier.row - from.row) })
  if (courier.column !== from.column) candidates.push({ column: from.column + Math.sign(courier.column - from.column), row: from.row })
  return candidates.find(candidate => isHearthfordWalkable(candidate) && !sameCoordinate(candidate, courier)) ?? from
}

const afterHoundTurn = (state: ExpeditionState, braced: boolean): ExpeditionStateTransition => {
  if (state.threat.status !== 'engaged') return { status: 'changed', state, detail: 'The marsh is quiet.' }
  if (houndProtectedByReeds(state)) return { status: 'changed', state: { ...state, threat: { ...state.threat, intent: 'recoiling' } }, detail: 'The lowered reeds keep the hound below the path.' }
  if (distance(state.coordinate, state.threat.position) <= 1) {
    if (braced) return { status: 'changed', state: { ...state, threat: { ...state.threat, intent: 'recoiling' } }, detail: 'Your stance turns the hound aside.' }
    if (state.support === 'field-dresser' && !state.supportSpent) {
      return { status: 'changed', state: { ...state, supportSpent: true, threat: { ...state.threat, intent: 'recoiling' } }, detail: 'The field dresser binds the tearing bite before it becomes an injury.' }
    }
    if (state.injury === 'hurt') {
      return {
        status: 'courier-death',
        state: { ...state, location: 'jomon', coordinate: { ...HEARTHFORD_GANGPLANK }, resource: state.resource === 'carried' ? 'lost' : state.resource, objective: state.objective === 'accepted' ? 'failed' : state.objective, consequence: 'courier-lost', threat: { ...state.threat, intent: 'pouncing' } },
        detail: 'The marsh hound overwhelms the courier.'
      }
    }
    return { status: 'changed', state: { ...state, injury: 'hurt', threat: { ...state.threat, intent: 'pouncing' } }, detail: 'The marsh hound wounds the courier.' }
  }
  const position = houndStep(state.threat.position, state.coordinate)
  return { status: 'changed', state: { ...state, threat: { ...state.threat, position, intent: distance(position, state.coordinate) <= 1 ? 'pouncing' : 'advancing' } }, detail: 'The marsh hound closes through the reeds.' }
}

const continueThreatTurn = (state: ExpeditionState, braced = false): ExpeditionStateTransition => afterHoundTurn(state, braced)

export const moveThroughHearthford = (state: ExpeditionState, direction: HearthfordMoveDirection): ExpeditionStateTransition => {
  if (state.location !== 'hearthford') return { status: 'blocked', detail: 'The courier is aboard Jomon.' }
  const coordinate = nextCoordinate(state.coordinate, direction)
  if (!isHearthfordWalkable(coordinate)) return { status: 'blocked', detail: 'Reed walls and water block that step.' }
  if (state.threat.status === 'engaged' && sameCoordinate(coordinate, state.threat.position)) return { status: 'blocked', detail: 'The hound holds that ground.' }
  let next: ExpeditionState = { ...clone(state), coordinate, noise: pressureNoise(state) }
  if (next.objective === 'accepted' && next.threat.status === 'dormant' && coordinate.column >= HEARTHFORD_HOUND_START.column) {
    next = { ...next, threat: { ...next.threat, status: 'engaged', intent: 'advancing' } }
  }
  if (next.objective === 'accepted' && next.resource === 'none' && sameCoordinate(coordinate, HEARTHFORD_SEAL_CORD) && (next.threat.status === 'defeated' || next.threat.status === 'evaded')) next = { ...next, resource: 'carried' }
  const turn = continueThreatTurn(next)
  return turn.status === 'changed' ? { ...turn, detail: next.resource === 'carried' ? 'The useful mill seal cord is now carried.' : turn.detail } : turn
}

export const resolveHearthfordAction = (state: ExpeditionState, action: HearthfordAction): ExpeditionStateTransition => {
  if (state.location !== 'hearthford') return { status: 'blocked', detail: 'The courier is aboard Jomon.' }
  if (action === 'attack') {
    if (state.threat.status !== 'engaged' || distance(state.coordinate, state.threat.position) > 1) return { status: 'blocked', detail: 'No hound is within reach of the attack.' }
    const health = Math.max(0, state.threat.health - 1)
    if (health === 0) return { status: 'changed', state: { ...clone(state), threat: { ...state.threat, status: 'defeated', health, intent: 'gone' } }, detail: 'The marsh hound is driven away.' }
    return continueThreatTurn({ ...clone(state), threat: { ...state.threat, health } })
  }
  if (action === 'brace') {
    if (state.threat.status !== 'engaged') return { status: 'blocked', detail: 'There is no immediate threat to brace against.' }
    return continueThreatTurn(clone(state), true)
  }
  if (action === 'lower-reed-screen') {
    if (!sameCoordinate(state.coordinate, HEARTHFORD_REED_SCREEN)) return { status: 'blocked', detail: 'The reed screen is not here.' }
    if (state.reedScreen === 'lowered') return { status: 'blocked', detail: 'The reed screen is already lowered.' }
    return continueThreatTurn({ ...clone(state), reedScreen: 'lowered', noise: Math.max(0, state.noise - 3) })
  }
  if (action === 'evade') {
    const prepared = state.loadout === 'smoke-and-hook' || state.support === 'quiet-scout'
    if (state.threat.status !== 'engaged' || !sameCoordinate(state.coordinate, HEARTHFORD_SEAL_CORD) || state.resource !== 'carried' || state.reedScreen !== 'lowered' || !prepared || state.noise > 10) {
      return { status: 'blocked', detail: 'Evade needs the carried seal cord, lowered reeds, quiet footing, and smoke or a scout.' }
    }
    return { status: 'changed', state: { ...clone(state), threat: { ...state.threat, status: 'evaded', intent: 'gone' } }, detail: 'Smoke and the reed screen let the courier evade the hound.' }
  }
  if (state.threat.status !== 'engaged' || state.injury !== 'hurt') return { status: 'blocked', detail: 'Retreat is available only after an injury during the hound encounter.' }
  return { status: 'changed', state: { ...clone(state), location: 'jomon', coordinate: { ...HEARTHFORD_GANGPLANK }, resource: state.resource === 'carried' ? 'lost' : state.resource, objective: state.objective === 'accepted' ? 'failed' : state.objective, consequence: 'escaped-injured' }, detail: 'The injured courier escapes back to Jomon and loses the expedition material.' }
}

export const decideHearthfordObjective = (state: ExpeditionState, decision: 'accept' | 'refuse'): ExpeditionState => {
  if (state.location !== 'hearthford' || !sameCoordinate(state.coordinate, HEARTHFORD_CONTACT_COORDINATE)) throw new Error('Mara Venn must be met in person')
  if (state.objective !== 'unmet') throw new Error('Mara Venn has already received an answer')
  return decision === 'accept'
    ? { ...clone(state), objective: 'accepted' }
    : { ...clone(state), objective: 'refused', consequence: 'objective-refused' }
}

export const deliverHearthfordSealCord = (state: ExpeditionState): ExpeditionState => {
  if (state.location !== 'hearthford' || !sameCoordinate(state.coordinate, HEARTHFORD_CONTACT_COORDINATE)) throw new Error('seal cord delivery requires Mara Venn')
  if (state.objective !== 'accepted' || state.resource !== 'carried') throw new Error('Mara Venn is waiting for the seal cord')
  return { ...clone(state), objective: 'completed', resource: 'delivered', consequence: 'seal-delivered' }
}

export const returnToJomonFromHearthford = (state: ExpeditionState): ExpeditionStateTransition => {
  if (state.location !== 'hearthford' || !sameCoordinate(state.coordinate, HEARTHFORD_GANGPLANK)) return { status: 'blocked', detail: 'Return requires the Hearthford gangplank.' }
  if (state.objective === 'accepted' && state.resource === 'carried') return { status: 'blocked', detail: 'Mara Venn is waiting for the carried seal cord.' }
  return { status: 'changed', state: { ...clone(state), location: 'jomon', coordinate: { ...HEARTHFORD_GANGPLANK } }, detail: 'The courier returns physically to Jomon.' }
}

export const hearthfordGlyphAt = (state: ExpeditionState, coordinate: ExpeditionCoordinate): string => {
  if (sameCoordinate(state.coordinate, coordinate)) return '@'
  if (state.threat.status === 'engaged' && sameCoordinate(state.threat.position, coordinate)) return 'H'
  const glyph = HEARTHFORD_MAP_ROWS[coordinate.row]?.[coordinate.column]
  if (glyph === 'H') return '.'
  return glyph ?? ' '
}

