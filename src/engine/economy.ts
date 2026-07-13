import type { Hero, ItemId, RunState } from '../types'

export const MAX_GOLD = 500
export const MIN_ITEM_PRICE = 5
export const MAX_ITEM_PRICE = 300
export const MAX_BOMBS = 8
export const MAX_ROPES = 8

const carryCaps: Partial<Record<ItemId, number>> = { tonic: 3, focusTonic: 3, bombPack: 1, ropeBundle: 1 }

export const validateItemPrice = (value: number): void => {
  if (!Number.isInteger(value) || value < MIN_ITEM_PRICE || value > MAX_ITEM_PRICE) throw new Error(`invalid item price: ${value}`)
}
export const grantGold = (state: RunState, amount: number): number => {
  if (!Number.isInteger(amount) || amount < 0) throw new Error(`invalid gold grant: ${amount}`)
  state.hero.gold = Math.max(0, Math.min(MAX_GOLD, state.hero.gold))
  const gained = Math.min(amount, MAX_GOLD - state.hero.gold)
  state.hero.gold += gained
  return gained
}
export const spendGold = (state: RunState, amount: number): boolean => {
  if (!Number.isInteger(amount) || amount < 0) throw new Error(`invalid gold cost: ${amount}`)
  if (state.hero.gold < amount) return false
  state.hero.gold -= amount
  return true
}
export const purchaseBlocker = (hero: Hero, id: ItemId): string | undefined => {
  const cap = carryCaps[id]
  if (cap !== undefined && hero.inventory.filter(item => item === id).length >= cap) return `${id} reserve is full.`
  if (id === 'bombPack' && hero.bombs >= MAX_BOMBS - 2) return 'Bomb reserve is full.'
  if (id === 'ropeBundle' && hero.ropes >= MAX_ROPES - 2) return 'Rope reserve is full.'
  return undefined
}
export const restoreBombs = (hero: Hero, amount: number): number => {
  const restored = Math.max(0, Math.min(amount, MAX_BOMBS - hero.bombs))
  hero.bombs += restored
  return restored
}
export const restoreRopes = (hero: Hero, amount: number): number => {
  const restored = Math.max(0, Math.min(amount, MAX_ROPES - hero.ropes))
  hero.ropes += restored
  return restored
}
