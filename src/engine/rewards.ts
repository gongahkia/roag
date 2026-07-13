import { ITEM, shopStock } from '../content'
import type { Biome, Hero, ItemId, ObjectiveKind, RunState } from '../types'
import { hasTags, queryTags } from './tags'

export type RewardSource = 'container' | 'merchant' | 'altar'
type Capability = 'bomb' | 'light' | 'mobility' | 'rope' | 'script'
interface RewardRule { source: RewardSource; item: ItemId; biome?: Biome; objective?: ObjectiveKind; requires?: readonly string[]; missing?: readonly Capability[] }

const rules: readonly RewardRule[] = [
  { source: 'container', biome: 'wilds', objective: 'rescueScout', missing: ['rope'], item: 'ropeBundle' },
  { source: 'container', biome: 'caverns', missing: ['light'], item: 'lantern' },
  { source: 'container', objective: 'recoverSupplies', missing: ['bomb'], item: 'bombPack' },
  { source: 'container', requires: ['arcane'], item: 'focusTonic' },
  { source: 'merchant', objective: 'defeatGuardian', missing: ['bomb'], item: 'bombPack' },
  { source: 'merchant', biome: 'wilds', missing: ['mobility'], item: 'boots' },
  { source: 'merchant', biome: 'caverns', missing: ['light'], item: 'lantern' },
  { source: 'merchant', requires: ['strength'], item: 'tonic' },
  { source: 'altar', biome: 'ruins', objective: 'invokeAltar', requires: ['arcane'], item: 'gate' },
  { source: 'altar', missing: ['script'], item: 'sight' },
  { source: 'altar', requires: ['strength'], item: 'focusTonic' }
]

const defaults: Record<RewardSource, readonly ItemId[]> = {
  container: ['tonic', 'focusTonic', 'bombPack', 'ropeBundle', 'rock', 'mapScroll', 'ward'],
  merchant: ['tonic'], altar: ['focusTonic']
}

const ownedItems = (hero: Hero): ItemId[] => [...new Set([...hero.inventory, ...Object.values(hero.equipment).filter((item): item is ItemId => Boolean(item))])].filter(id => Boolean(ITEM[id]))
export const rewardTags = (state: RunState): string[] => {
  const items = ownedItems(state.hero)
  return queryTags({ items, skills: state.hero.skills, scripts: items.filter(id => ITEM[id].use === 'spell') })
}
export const missingCapabilities = (state: RunState): Capability[] => {
  const tags = new Set(rewardTags(state))
  const items = ownedItems(state.hero)
  return [
    ...(state.hero.bombs > 0 || items.includes('bombPack') ? [] : ['bomb' as const]),
    ...(items.includes('lantern') || tags.has('vision') ? [] : ['light' as const]),
    ...(items.some(item => ['boots', 'featherboots', 'blinkRune'].includes(item)) || tags.has('movement') ? [] : ['mobility' as const]),
    ...(state.hero.ropes > 0 || items.includes('ropeBundle') ? [] : ['rope' as const]),
    ...(tags.has('script') ? [] : ['script' as const])
  ]
}

export const contextualReward = (state: RunState, source: RewardSource): ItemId => {
  const biome = state.area ?? state.floor.biome
  const tags = rewardTags(state)
  const missing = missingCapabilities(state)
  const rule = rules.find(candidate => candidate.source === source && (!candidate.biome || candidate.biome === biome) && (!candidate.objective || candidate.objective === state.floor.objective.kind) && (!candidate.requires || hasTags(tags, candidate.requires)) && (!candidate.missing || candidate.missing.every(capability => missing.includes(capability))))
  return rule?.item ?? defaults[source][0]
}

export const merchantStock = (state: RunState): ItemId[] => {
  const featured = contextualReward(state, 'merchant')
  return [...new Set([featured, ...shopStock(state.area ?? state.floor.biome)])]
}
