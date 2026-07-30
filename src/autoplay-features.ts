import { MAP_WIDTH, type AutoplayMode, type RunState, type TileKind } from './types'

export const POLICY_FEATURE_VERSION = 1 as const
export const POLICY_FEATURE_HISTORY_LIMIT = 8 as const
export const POLICY_FEATURE_INPUTS = ['fov', 'knownTerrain', 'visibleEntities', 'visibleItems', 'courierKit', 'history'] as const
export type PolicyFeatureMode = Exclude<AutoplayMode, 'off'>
export interface PolicyFeatureHistoryEntry { turn: number; command: string; reason: string; events: string[]; resourceDelta: { health: number; focus: number; gold: number; bombs: number; ropes: number; keys: number } }
export interface PolicyFeatureVector<M extends PolicyFeatureMode = PolicyFeatureMode> { version: typeof POLICY_FEATURE_VERSION; informationMode: M; fov: Array<{ x: number; y: number; kind: TileKind }>; knownTerrain: Array<{ x: number; y: number; kind: TileKind }>; visibleEntities: Array<{ id: string; role: string; hostile: boolean; x: number; y: number; health: number }>; visibleItems: Array<{ id: string; x: number; y: number; count: number }>; courierKit: { health: number; maxHealth: number; focus: number; maxFocus: number; gold: number; bombs: number; ropes: number; keys: number; inventory: string[]; equipment: Record<string, string>; cooldowns: Record<string, number>; traversalTools: string[] }; history: PolicyFeatureHistoryEntry[] }
export type VisiblePolicyFeatureVector = PolicyFeatureVector<'visible'>
export type OmniscientPolicyFeatureVector = PolicyFeatureVector<'omniscient'>

const visibleAt = (state: RunState, x: number, y: number): boolean => state.floor.tiles[y * MAP_WIDTH + x]?.visible === true
const orderedTiles = (state: RunState, include: (index: number) => boolean) => state.floor.tiles.flatMap((tile, index) => include(index) ? [{ x: index % MAP_WIDTH, y: Math.floor(index / MAP_WIDTH), kind: tile.kind }] : [])
const orderedEntities = (state: RunState, include: (x: number, y: number) => boolean) => state.floor.actors.filter(actor => actor.health > 0 && include(actor.x, actor.y)).map(actor => ({ id: actor.id, role: actor.role, hostile: actor.hostile, x: actor.x, y: actor.y, health: actor.health })).sort((left, right) => left.id.localeCompare(right.id))
const orderedItems = (state: RunState, include: (x: number, y: number) => boolean) => state.floor.items.filter(item => include(item.x, item.y)).map(item => ({ id: item.id, x: item.x, y: item.y, count: item.count })).sort((left, right) => left.y - right.y || left.x - right.x || left.id.localeCompare(right.id))

export const appendPolicyFeatureHistory = (history: readonly PolicyFeatureHistoryEntry[], entry: PolicyFeatureHistoryEntry): PolicyFeatureHistoryEntry[] => [...history, { ...entry, events: [...entry.events], resourceDelta: { ...entry.resourceDelta } }].slice(-POLICY_FEATURE_HISTORY_LIMIT)

export const encodePolicyFeatures = <M extends PolicyFeatureMode>(state: RunState, informationMode: M, history: readonly PolicyFeatureHistoryEntry[] = []): PolicyFeatureVector<M> => {
  const omniscient = informationMode === 'omniscient'
  const visible = (x: number, y: number): boolean => omniscient || visibleAt(state, x, y)
  const known = (index: number): boolean => omniscient || state.floor.tiles[index]!.explored
  return {
    version: POLICY_FEATURE_VERSION,
    informationMode,
    fov: orderedTiles(state, index => omniscient || state.floor.tiles[index]!.visible),
    knownTerrain: orderedTiles(state, known),
    visibleEntities: orderedEntities(state, visible),
    visibleItems: orderedItems(state, visible),
    courierKit: { health: state.hero.health, maxHealth: state.hero.maxHealth, focus: state.hero.focus, maxFocus: state.hero.maxFocus, gold: state.hero.gold, bombs: state.hero.bombs, ropes: state.hero.ropes, keys: state.hero.keys, inventory: [...state.hero.inventory], equipment: { ...state.hero.equipment }, cooldowns: { ...(state.hero.cooldowns ?? {}) }, traversalTools: [...(state.hero.traversalTools ?? [])] },
    history: history.slice(-POLICY_FEATURE_HISTORY_LIMIT).map(entry => ({ ...entry, events: [...entry.events], resourceDelta: { ...entry.resourceDelta } }))
  }
}

export const visiblePolicyFeatures = (features: PolicyFeatureVector): VisiblePolicyFeatureVector => {
  if (features.informationMode !== 'visible') throw new Error(`visible policy cannot consume ${features.informationMode} features`)
  return features as VisiblePolicyFeatureVector
}
