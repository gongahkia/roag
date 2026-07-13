import { ITEM } from '../content'
import type { EquipmentEffect, EquipmentEffectKind, EquipmentTrigger } from '../effects'
import type { Hero, ItemId } from '../types'
import { evaluateModifiers, queryTags, type ModifierEvaluation, type TagModifier, type TagQuery } from './tags'

export interface EquipmentEffectContext extends TagQuery { trigger?: EquipmentTrigger; actionId?: string }
export interface EquipmentEffectEvaluation extends ModifierEvaluation { effects: string[] }

const equippedItems = (hero: Hero): ItemId[] => [...new Set(Object.values(hero.equipment).filter((item): item is ItemId => Boolean(item)))].sort()
export const equippedEffects = (hero: Hero): Array<{ itemId: ItemId; effect: EquipmentEffect }> => equippedItems(hero).flatMap(itemId => {
  const item = ITEM[itemId]
  if (!item) throw new Error(`unknown equipped item: ${itemId}`)
  return (item.effects ?? []).map(effect => ({ itemId, effect }))
})
export const evaluateEquipmentEffects = (hero: Hero, kind: EquipmentEffectKind, context: EquipmentEffectContext = {}, base: Readonly<Record<string, number>> = {}): EquipmentEffectEvaluation => {
  const tags = queryTags({ items: [...equippedItems(hero), ...(context.items ?? [])], skills: context.skills, scripts: context.scripts, terrain: context.terrain, actors: context.actors, tags: context.tags })
  const modifiers: TagModifier[] = equippedEffects(hero).filter(({ effect }) => effect.kind === kind && (kind !== 'triggered' || effect.trigger === context.trigger) && (kind !== 'action' || effect.actionId === context.actionId)).map(({ itemId, effect }) => ({ id: `${itemId}:${effect.id}`, requires: effect.requires, excludes: effect.excludes, add: effect.add, multiply: effect.multiply }))
  const evaluation = evaluateModifiers(tags, modifiers, base)
  return { ...evaluation, effects: evaluation.applied }
}
