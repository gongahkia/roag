import { ITEM, MONSTERS, SKILLS } from '../content'
import { terrainTags } from './terrain'
import type { Actor, ItemId, TileKind } from '../types'

export interface TagQuery { items?: readonly ItemId[]; skills?: readonly string[]; scripts?: readonly ItemId[]; terrain?: readonly TileKind[]; actors?: readonly Actor[]; tags?: readonly string[] }
export interface TagModifier { id: string; requires?: readonly string[]; excludes?: readonly string[]; add?: Readonly<Record<string, number>>; multiply?: Readonly<Record<string, number>> }
export interface ModifierEvaluation { tags: string[]; values: Record<string, number>; applied: string[] }

const canonical = (tags: readonly string[]): string[] => [...new Set(tags)].sort()
const requireItem = (id: ItemId) => {
  const item = ITEM[id]
  if (!item) throw new Error(`unknown item tag source: ${id}`)
  return item
}

export const itemTags = (id: ItemId): string[] => {
  const item = requireItem(id)
  return canonical(['item', ...(item.slot ? ['equipment'] : []), ...(item.weapon ? ['weapon', ...item.weapon.tags] : []), ...(item.tags ?? []), ...(item.use === 'spell' ? ['script', 'arcane', item.spell ?? item.id] : [])])
}
export const skillTags = (id: string): string[] => {
  const skill = SKILLS.find(current => current.id === id)
  if (!skill) throw new Error(`unknown skill tag source: ${id}`)
  return canonical(['skill', ...skill.tags])
}
export const scriptTags = (id: ItemId): string[] => {
  const item = requireItem(id)
  if (item.use !== 'spell') throw new Error(`non-script tag source: ${id}`)
  return itemTags(id)
}
export const actorTags = (actor: Actor): string[] => canonical(['actor', actor.role, actor.kind, actor.hostile ? 'hostile' : 'friendly', ...(MONSTERS.find(monster => monster.id === actor.kind)?.tags ?? []), ...(actor.status ?? [])])
export const queryTags = (query: TagQuery): string[] => canonical([
  ...(query.items ?? []).flatMap(itemTags), ...(query.skills ?? []).flatMap(skillTags), ...(query.scripts ?? []).flatMap(scriptTags), ...(query.terrain ?? []).flatMap(terrainTags), ...(query.actors ?? []).flatMap(actorTags), ...(query.tags ?? [])
])
export const hasTags = (query: TagQuery | readonly string[], required: readonly string[]): boolean => {
  const tags = new Set(Array.isArray(query) ? query as readonly string[] : queryTags(query as TagQuery))
  return required.every(tag => tags.has(tag))
}

const applyValues = (values: Record<string, number>, change: Readonly<Record<string, number>> | undefined, operation: 'add' | 'multiply'): void => {
  for (const [key, amount] of Object.entries(change ?? {})) {
    if (!Number.isFinite(amount)) throw new Error(`invalid modifier value: ${key}`)
    values[key] = operation === 'add' ? (values[key] ?? 0) + amount : (values[key] ?? 0) * amount
  }
}

export const evaluateModifiers = (query: TagQuery | readonly string[], modifiers: readonly TagModifier[], base: Readonly<Record<string, number>> = {}): ModifierEvaluation => {
  const tags = Array.isArray(query) ? canonical(query as readonly string[]) : queryTags(query as TagQuery)
  const values = { ...base }
  for (const [key, value] of Object.entries(values)) if (!Number.isFinite(value)) throw new Error(`invalid base modifier value: ${key}`)
  const ids = new Set<string>()
  for (const modifier of modifiers) {
    if (!modifier.id || ids.has(modifier.id)) throw new Error(`invalid modifier id: ${modifier.id}`)
    ids.add(modifier.id)
  }
  const applied: string[] = []
  for (const modifier of [...modifiers].sort((a, b) => a.id < b.id ? -1 : a.id > b.id ? 1 : 0)) {
    if (!hasTags(tags, modifier.requires ?? []) || (modifier.excludes ?? []).some(tag => tags.includes(tag))) continue
    applyValues(values, modifier.add, 'add')
    applyValues(values, modifier.multiply, 'multiply')
    applied.push(modifier.id)
  }
  return { tags, values, applied }
}
