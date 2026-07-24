import { streamSeed } from '../rng'
import { ITEM, shopStock } from '../content'
import type { Biome, Hero, HubState, ItemId } from '../types'
import { purchaseBlocker } from './economy'

export type HubAction = 'routes' | 'roster' | 'shop' | 'outfitter'
export interface HubOptions { hero?: Hero; biome?: Biome; notice?: string }
export interface HubView { courierName: string; state: HubState; hero?: Hero; stock?: ItemId[]; equipment?: ItemId[]; notice?: string }
export interface HubMutation { changed: boolean; message: string }

const packLimit = 12

export const createHubState = (seed: number): HubState => ({ season: streamSeed(seed, 'generation', 'hub-season') % 4, supplies: ['tonic', 'ropeBundle', 'rock'], rescued: [], unlockedAreas: ['mine'], completedAreas: [] })
export const hubStock = (biome: Biome): ItemId[] => [...new Set(['tonic', 'focusTonic', 'bombPack', 'ropeBundle', ...shopStock(biome)])]
export const hubEquipment = (hero: Hero): ItemId[] => [...new Set([...Object.values(hero.equipment).filter((id): id is ItemId => Boolean(id)), ...hero.inventory].filter(id => Boolean(ITEM[id]?.slot)))]
export const hubView = (courierName: string, state: HubState, options: HubOptions = {}): HubView => ({
  courierName,
  state,
  ...(options.hero ? { hero: options.hero, stock: hubStock(options.biome ?? 'mine'), equipment: hubEquipment(options.hero) } : {}),
  ...(options.notice ? { notice: options.notice } : {})
})

export const buyHubItem = (hero: Hero, id: ItemId): HubMutation => {
  const item = ITEM[id]
  if (!item) return { changed: false, message: 'That stock is unavailable.' }
  if (item.slot && Object.values(hero.equipment).includes(id)) return { changed: false, message: `${item.name} is already equipped.` }
  const blocker = purchaseBlocker(hero, id)
  if (blocker) return { changed: false, message: blocker }
  if (hero.inventory.length >= packLimit) return { changed: false, message: 'Your pack is full.' }
  if (hero.gold < item.value) return { changed: false, message: `Need ${item.value - hero.gold} more cash.` }
  hero.gold -= item.value
  hero.inventory.push(id)
  return { changed: true, message: `Bought ${item.name}.` }
}

export const equipHubItem = (hero: Hero, id: ItemId): HubMutation => {
  const item = ITEM[id]
  if (!item?.slot) return { changed: false, message: 'That cannot be equipped.' }
  if (hero.equipment[item.slot] === id) return { changed: false, message: `${item.name} is already equipped.` }
  const index = hero.inventory.indexOf(id)
  if (index < 0) return { changed: false, message: `${item.name} is not in your pack.` }
  const previous = hero.equipment[item.slot]
  hero.inventory.splice(index, 1)
  if (previous) { hero.inventory.push(previous); hero.lastUnequipped = previous }
  hero.equipment[item.slot] = id
  return { changed: true, message: `Equipped ${item.name}.` }
}
