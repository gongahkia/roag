import { biomeName, MONSTERS } from '../content'
import type { Actor, EncyclopediaSection, EncyclopediaState, LegacyRecord, RunState } from '../types'
import { AREA_GATES } from './gates'
import { actionById } from './actions'
import { actorTags, itemTags, scriptTags, skillTags } from './tags'
import { terrainTags } from './terrain'
import { getTile } from '../world'

export const ENCYCLOPEDIA_SECTIONS: readonly EncyclopediaSection[] = ['enemies', 'telegraphs', 'tags', 'gates', 'legacy']
const copyLegacy = (records: readonly LegacyRecord[]): LegacyRecord[] => records.slice(-12).map(record => ({ ...record }))
const add = (entries: string[], value: string): string[] => entries.includes(value) ? entries : [...entries, value].sort()
const addMany = (entries: string[], values: readonly string[]): string[] => values.reduce((current, value) => add(current, value), entries)
const displayTag = (tag: string): string => tag === 'script' ? 'charm' : tag

export const encyclopedia = (state: RunState): EncyclopediaState => state.encyclopedia ??= { enemies: [], telegraphs: [], tags: [], gates: [], legacyRecords: [] }
export const hydrateEncyclopediaLegacy = (state: RunState, records: readonly LegacyRecord[]): void => { encyclopedia(state).legacyRecords = copyLegacy(records) }

export const recordEnemy = (state: RunState, actor: Actor): void => {
  const book = encyclopedia(state)
  book.enemies = add(book.enemies, actor.kind)
  book.tags = addMany(book.tags, actorTags(actor))
}

export const recordTelegraph = (state: RunState, actionId: string): void => {
  const book = encyclopedia(state)
  book.telegraphs = add(book.telegraphs, actionId)
  const action = actionById(actionId)
  if (action) book.tags = addMany(book.tags, action.tags)
}

export const recordGate = (state: RunState, gateId: string): void => {
  const gate = Object.values(AREA_GATES).find(current => current.id === gateId)
  const book = encyclopedia(state)
  book.gates = add(book.gates, gateId)
  if (gate) book.tags = addMany(book.tags, gate.tagAlternatives.flatMap(option => option.tags))
}

export const observeEncyclopedia = (state: RunState): void => {
  recordGate(state, AREA_GATES[state.area ?? state.floor.biome].id)
  const book = encyclopedia(state)
  for (const actor of state.floor.actors) if (actor.hostile && getTile(state.floor, actor.x, actor.y)?.visible) recordEnemy(state, actor)
  for (const tile of state.floor.tiles) if (tile.visible) book.tags = addMany(book.tags, terrainTags(tile.kind))
  for (const item of state.hero.inventory) book.tags = addMany(book.tags, item === 'ember' || item === 'mend' || item === 'sight' || item === 'root' || item === 'waterScript' || item === 'lull' || item === 'blink' || item === 'gust' || item === 'pull' || item === 'wardScript' || item === 'gate' ? scriptTags(item) : itemTags(item))
  for (const skill of state.hero.skills) book.tags = addMany(book.tags, skillTags(skill))
}

export const encyclopediaEntries = (state: RunState, section: EncyclopediaSection): string[] => {
  const book = encyclopedia(state)
  if (section === 'enemies') return book.enemies.map(id => { const monster = MONSTERS.find(current => current.id === id); return monster ? `${monster.name} — ${(monster.tags ?? [monster.biome]).join(', ')}` : id })
  if (section === 'telegraphs') return book.telegraphs.map(id => { const action = actionById(id); return action ? `${action.name} — ${action.tags.join(', ')}` : id })
  if (section === 'tags') return book.tags.map(tag => `#${displayTag(tag)}`)
  if (section === 'gates') return book.gates.map(id => { const gate = Object.values(AREA_GATES).find(current => current.id === id); return gate ? `${biomeName[gate.biome]} → ${biomeName[gate.unlockedDestination.biome]}: ${gate.tagAlternatives.map(option => option.label).join(' / ')}` : id })
  return book.legacyRecords.map(record => `${record.heirName} fell in ${biomeName[record.biome]} ${record.floor + 1}`)
}
