import type { Biome, EquipmentSlot, ItemId, StatName } from './types'
import type { ActionShape } from './engine/actions'
import { validateEquipmentEffects, type EquipmentEffect } from './effects'

export interface WeaponProfile { damage: number; reach: number; shape: ActionShape; cooldown: number; tags: string[] }

export interface ItemDefinition {
  id: ItemId
  name: string
  glyph: string
  color: string
  slot?: EquipmentSlot
  weapon?: WeaponProfile
  defense?: number
  value: number
  use?: 'heal' | 'focus' | 'map' | 'teleport' | 'bomb' | 'rope' | 'key' | 'torch' | 'spell'
  spell?: string
  throwable?: boolean
  tags?: string[]
  effects?: readonly EquipmentEffect[]
}

export interface MonsterDefinition { id: string; name: string; glyph: string; color: string; health: number; attack: number; defense: number; speed: number; ai: 'chase' | 'ranged' | 'wander' | 'guardian'; xp: number; biome: Biome; tags?: string[] }
export interface SkillDefinition { id: string; name: string; stat: StatName; level: number; text: string; tags: string[]; prerequisites: string[] }
export interface ContentRegistry { items: readonly ItemDefinition[]; monsters: readonly MonsterDefinition[]; skills: readonly SkillDefinition[]; tags: readonly string[]; shopStock: Readonly<Record<Biome, readonly ItemId[]>> }

export const CONTENT_TAGS = ['strength', 'agility', 'vitality', 'intellect'] as const

export const ITEMS: ItemDefinition[] = [
  { id: 'whip', name: 'Surveyor Whip', glyph: '/', color: '#e7c680', slot: 'mainHand', weapon: { damage: 4, reach: 2, shape: 'line', cooldown: 0, tags: ['flexible', 'reach'] }, value: 45, effects: [{ id: 'surveying-strike', kind: 'action', actionId: 'player-strike', requires: ['reach'], add: { damage: 1 } }] },
  { id: 'machete', name: 'Brush Machete', glyph: '/', color: '#b8d6a0', slot: 'mainHand', weapon: { damage: 6, reach: 1, shape: 'adjacent', cooldown: 1, tags: ['cleave', 'wilds'] }, value: 75 },
  { id: 'pickaxe', name: 'Prospector Pick', glyph: 'T', color: '#c7c4ba', slot: 'mainHand', weapon: { damage: 7, reach: 1, shape: 'cross', cooldown: 2, tags: ['rubble', 'piercing'] }, value: 110 },
  { id: 'spear', name: 'Cave Spear', glyph: '/', color: '#d0ae78', slot: 'mainHand', weapon: { damage: 8, reach: 2, shape: 'line', cooldown: 1, tags: ['piercing', 'reach'] }, value: 140, throwable: true },
  { id: 'sunblade', name: 'Sun Blade', glyph: '/', color: '#ffe181', slot: 'mainHand', weapon: { damage: 11, reach: 2, shape: 'cone', cooldown: 2, tags: ['radiant', 'cleave'] }, value: 260 },
  { id: 'buckler', name: 'Tin Buckler', glyph: ')', color: '#bbc6cc', slot: 'offHand', defense: 2, value: 80, effects: [{ id: 'guarded', kind: 'passive', add: { defense: 1 } }] },
  { id: 'lantern', name: 'Glow Lantern', glyph: 'i', color: '#ffe18a', slot: 'offHand', defense: 1, value: 95, use: 'torch' },
  { id: 'cap', name: 'Miner Cap', glyph: '[', color: '#d3b05c', slot: 'head', defense: 1, value: 55 },
  { id: 'mask', name: 'Moss Mask', glyph: '[', color: '#71a66d', slot: 'head', defense: 2, value: 120 },
  { id: 'coat', name: 'Canvas Coat', glyph: '[', color: '#ad8056', slot: 'body', defense: 2, value: 100 },
  { id: 'mail', name: 'Crystal Mail', glyph: '[', color: '#8bb7d1', slot: 'body', defense: 4, value: 230 },
  { id: 'boots', name: 'Trail Boots', glyph: ';', color: '#c28b5d', slot: 'boots', defense: 1, value: 70 },
  { id: 'featherboots', name: 'Feather Boots', glyph: ';', color: '#e7e9f0', slot: 'boots', defense: 2, value: 180 },
  { id: 'ward', name: 'Ward Charm', glyph: 'o', color: '#ca9fe4', slot: 'charm', defense: 2, value: 160, effects: [{ id: 'arcane-return', kind: 'triggered', trigger: 'spell', requires: ['arcane'], add: { focus: 1 } }] },
  { id: 'sunseal', name: 'Sun Seal', glyph: 'o', color: '#ffe181', slot: 'charm', defense: 3, value: 280 },
  { id: 'tonic', name: 'Vital Tonic', glyph: '!', color: '#eb6571', value: 35, use: 'heal', throwable: true },
  { id: 'focusTonic', name: 'Focus Tonic', glyph: '!', color: '#7fa8e8', value: 50, use: 'focus', throwable: true },
  { id: 'mapScroll', name: 'Survey Scroll', glyph: '?', color: '#e6d2a6', value: 65, use: 'map' },
  { id: 'blinkRune', name: 'Blink Rune', glyph: '?', color: '#bda8eb', value: 90, use: 'teleport' },
  { id: 'bombPack', name: 'Bomb Bundle', glyph: '*', color: '#ea8e64', value: 80, use: 'bomb' },
  { id: 'ropeBundle', name: 'Rope Bundle', glyph: '~', color: '#dab272', value: 55, use: 'rope' },
  { id: 'key', name: 'Iron Key', glyph: '?', color: '#d7c268', value: 40, use: 'key' },
  { id: 'rock', name: 'Throwing Stone', glyph: '*', color: '#9da5a9', value: 5, throwable: true },
  { id: 'fireJar', name: 'Fire Jar', glyph: '!', color: '#ff874f', value: 95, throwable: true },
  { id: 'ember', name: 'Ember Script', glyph: '?', color: '#ff9c63', value: 120, use: 'spell', spell: 'ember' },
  { id: 'mend', name: 'Mending Script', glyph: '?', color: '#91e0b1', value: 110, use: 'spell', spell: 'mend' },
  { id: 'sight', name: 'Sight Script', glyph: '?', color: '#9dd7e4', value: 105, use: 'spell', spell: 'sight' },
  { id: 'gust', name: 'Gust Script', glyph: '?', color: '#c1b8f4', value: 115, use: 'spell', spell: 'gust' },
  { id: 'wardScript', name: 'Ward Script', glyph: '?', color: '#ecb7e3', value: 130, use: 'spell', spell: 'ward' },
  { id: 'gate', name: 'Gate Script', glyph: '?', color: '#f1db78', value: 160, use: 'spell', spell: 'gate' }
]

export const ITEM = Object.fromEntries(ITEMS.map(item => [item.id, item])) as Record<string, ItemDefinition>

export const MONSTERS: MonsterDefinition[] = [
  { id: 'rat', name: 'Tunnel Rat', glyph: 'r', color: '#b8a598', health: 5, attack: 2, defense: 8, speed: 110, ai: 'chase', xp: 6, biome: 'mine' },
  { id: 'mole', name: 'Iron Mole', glyph: 'm', color: '#9298a2', health: 9, attack: 4, defense: 10, speed: 90, ai: 'chase', xp: 10, biome: 'mine' },
  { id: 'sapper', name: 'Powder Sapper', glyph: 's', color: '#d6a263', health: 8, attack: 5, defense: 9, speed: 100, ai: 'ranged', xp: 14, biome: 'mine' },
  { id: 'beetle', name: 'Brass Beetle', glyph: 'b', color: '#d6c16d', health: 13, attack: 5, defense: 13, speed: 75, ai: 'chase', xp: 18, biome: 'mine' },
  { id: 'driller', name: 'Tunnel Driller', glyph: 'd', color: '#dfb77a', health: 11, attack: 6, defense: 12, speed: 100, ai: 'ranged', xp: 20, biome: 'mine' },
  { id: 'foreman', name: 'The Foreman', glyph: 'F', color: '#ffe080', health: 42, attack: 8, defense: 14, speed: 105, ai: 'guardian', xp: 70, biome: 'mine' },
  { id: 'thornling', name: 'Thornling', glyph: 't', color: '#86c064', health: 8, attack: 4, defense: 10, speed: 105, ai: 'chase', xp: 11, biome: 'wilds' },
  { id: 'boar', name: 'Moss Boar', glyph: 'b', color: '#a77d58', health: 15, attack: 7, defense: 11, speed: 115, ai: 'chase', xp: 19, biome: 'wilds' },
  { id: 'spitter', name: 'Vine Spitter', glyph: 'v', color: '#67ba7b', health: 10, attack: 6, defense: 9, speed: 90, ai: 'ranged', xp: 16, biome: 'wilds' },
  { id: 'wisp', name: 'Marsh Wisp', glyph: 'w', color: '#9be6bc', health: 7, attack: 6, defense: 12, speed: 130, ai: 'wander', xp: 21, biome: 'wilds' },
  { id: 'frog', name: 'Canopy Frog', glyph: 'f', color: '#a9d666', health: 11, attack: 6, defense: 11, speed: 120, ai: 'chase', xp: 23, biome: 'wilds' },
  { id: 'heartwood', name: 'Heartwood Stag', glyph: 'H', color: '#d1e281', health: 52, attack: 10, defense: 14, speed: 110, ai: 'guardian', xp: 90, biome: 'wilds' },
  { id: 'crawler', name: 'Crystal Crawler', glyph: 'c', color: '#7bcfe0', health: 14, attack: 7, defense: 13, speed: 95, ai: 'chase', xp: 23, biome: 'caverns' },
  { id: 'magma', name: 'Magma Newt', glyph: 'n', color: '#ef795a', health: 12, attack: 8, defense: 11, speed: 105, ai: 'chase', xp: 25, biome: 'caverns' },
  { id: 'echo', name: 'Echo Bat', glyph: 'e', color: '#ba9ddd', health: 9, attack: 7, defense: 12, speed: 140, ai: 'wander', xp: 26, biome: 'caverns' },
  { id: 'seer', name: 'Cave Seer', glyph: 's', color: '#ba8ae7', health: 13, attack: 9, defense: 12, speed: 95, ai: 'ranged', xp: 30, biome: 'caverns' },
  { id: 'slug', name: 'Salt Slug', glyph: 'u', color: '#a8c5cf', health: 19, attack: 8, defense: 15, speed: 65, ai: 'chase', xp: 31, biome: 'caverns' },
  { id: 'geode', name: 'Geode Wyrm', glyph: 'G', color: '#8ce5f2', health: 62, attack: 12, defense: 16, speed: 100, ai: 'guardian', xp: 115, biome: 'caverns' },
  { id: 'scarab', name: 'Ash Scarab', glyph: 's', color: '#d8b363', health: 16, attack: 8, defense: 15, speed: 95, ai: 'chase', xp: 32, biome: 'ruins' },
  { id: 'sentinel', name: 'Stone Sentinel', glyph: 'S', color: '#9da5aa', health: 23, attack: 10, defense: 17, speed: 75, ai: 'chase', xp: 40, biome: 'ruins' },
  { id: 'oracle', name: 'Dust Oracle', glyph: 'o', color: '#e9c489', health: 15, attack: 11, defense: 13, speed: 100, ai: 'ranged', xp: 45, biome: 'ruins' },
  { id: 'shade', name: 'Vault Shade', glyph: 'h', color: '#c1a5ed', health: 14, attack: 10, defense: 16, speed: 125, ai: 'wander', xp: 48, biome: 'ruins' },
  { id: 'cultist', name: 'Ash Cultist', glyph: 'c', color: '#df9a7c', health: 18, attack: 11, defense: 14, speed: 100, ai: 'ranged', xp: 51, biome: 'ruins' },
  { id: 'regent', name: 'The Ash Regent', glyph: 'R', color: '#ffdb75', health: 84, attack: 15, defense: 19, speed: 110, ai: 'guardian', xp: 180, biome: 'ruins' }
]

export const SKILLS: SkillDefinition[] = [
  ...(['Iron Grip', 'Cleave', 'Breaker', 'Counter', 'Unstoppable', 'Titan'] as const).map((name, i) => ({ id: `str${i + 1}`, name, stat: 'strength' as StatName, level: i + 1, text: ['Strength +1, melee damage +1', 'Strength +1, melee damage +1', 'Strength +1, break rubble', 'Strength +1, guard 2 damage', 'Strength +1, melee knockback', 'Strength +1, melee damage +2'][i], tags: ['strength'], prerequisites: i ? [`str${i}`] : [] })),
  ...(['Quick Step', 'Sure Aim', 'Skirmisher', 'Evasion', 'Fleet', 'Ghostwalk'] as const).map((name, i) => ({ id: `agi${i + 1}`, name, stat: 'agility' as StatName, level: i + 1, text: ['Agility +1, move +1 floor tile', 'Agility +1, melee reach +1', 'Agility +1, evade telegraphs 20%', 'Agility +1, dodge +3', 'Agility +1, move +1 floor tile', 'Agility +1, evade telegraphs +35%'][i], tags: ['agility'], prerequisites: i ? [`agi${i}`] : [] })),
  ...(['Hardy', 'Forager', 'Stalwart', 'Recovery', 'Ironblood', 'Last Stand'] as const).map((name, i) => ({ id: `vit${i +1}`, name, stat: 'vitality' as StatName, level: i + 1, text: ['Vitality +1, maximum health +2', 'Vitality +1, recovery +1', 'Vitality +1, shield 1 damage', 'Vitality +1, recovery +3', 'Vitality +1, hazards -2 damage', 'Vitality +1, rescue recovery +6'][i], tags: ['vitality'], prerequisites: i ? [`vit${i}`] : [] })),
  ...(['Spark', 'Insight', 'Conjure', 'Divine', 'Sorcery', 'Archmage'] as const).map((name, i) => ({ id: `int${i + 1}`, name, stat: 'intellect' as StatName, level: i + 1, text: ['Intellect +1, scripts cost 1 less', 'Intellect +1, focus recovery +1', 'Intellect +1, script range +1', 'Intellect +1, wards shield 2', 'Intellect +1, script range +1', 'Intellect +1, focus recovery +1, astral gates'][i], tags: ['intellect'], prerequisites: i ? [`int${i}`] : [] }))
]

export const biomeForFloor = (index: number): Biome => (['mine', 'wilds', 'caverns', 'ruins'] as const)[Math.floor(index / 4)]
export const biomeName: Record<Biome, string> = { mine: 'Shale Mine', wilds: 'Verdant Wilds', caverns: 'Glass Caverns', ruins: 'Ashen Ruins' }
export const SHOP_STOCK: Record<Biome, ItemId[]> = {
  mine: ['tonic', 'bombPack', 'ropeBundle', 'pickaxe', 'cap', 'key'],
  wilds: ['tonic', 'machete', 'focusTonic', 'boots', 'fireJar', 'mapScroll'],
  caverns: ['focusTonic', 'lantern', 'spear', 'ember', 'mend', 'blinkRune'],
  ruins: ['mail', 'ward', 'sunblade', 'gate', 'wardScript', 'key']
}

const idPattern = /^[a-z][a-zA-Z0-9]*$/
const validateIds = (label: string, definitions: ReadonlyArray<{ id: string }>): void => {
  const ids = new Set<string>()
  for (const definition of definitions) {
    if (!idPattern.test(definition.id)) throw new Error(`invalid ${label} id: ${definition.id}`)
    if (ids.has(definition.id)) throw new Error(`duplicate ${label} id: ${definition.id}`)
    ids.add(definition.id)
  }
}

const validateTags = (label: string, id: string, tags: readonly string[] | undefined, knownTags: ReadonlySet<string>): void => {
  for (const tag of tags ?? []) if (!knownTags.has(tag)) throw new Error(`invalid ${label} tag on ${id}: ${tag}`)
}

const validatePrerequisites = (skills: readonly SkillDefinition[]): void => {
  const skillIds = new Set(skills.map(skill => skill.id))
  const visiting = new Set<string>()
  const visited = new Set<string>()
  const visit = (skill: SkillDefinition): void => {
    if (visited.has(skill.id)) return
    if (visiting.has(skill.id)) throw new Error(`cyclic skill prerequisite: ${skill.id}`)
    visiting.add(skill.id)
    for (const prerequisite of skill.prerequisites) {
      if (!skillIds.has(prerequisite)) throw new Error(`unknown skill prerequisite on ${skill.id}: ${prerequisite}`)
      if (prerequisite === skill.id) throw new Error(`self-referencing skill prerequisite: ${skill.id}`)
      visit(skills.find(candidate => candidate.id === prerequisite)!)
    }
    visiting.delete(skill.id)
    visited.add(skill.id)
  }
  for (const skill of skills) visit(skill)
}

export const validateContent = (registry: ContentRegistry): void => {
  validateIds('item', registry.items)
  validateIds('monster', registry.monsters)
  validateIds('skill', registry.skills)
  const tags = new Set(registry.tags)
  for (const item of registry.items) {
    validateTags('item', item.id, item.tags, tags)
    validateEquipmentEffects(item.effects, item.id)
    if (item.use === 'spell' && !item.spell) throw new Error(`spell item missing spell id: ${item.id}`)
    if (item.use !== 'spell' && item.spell) throw new Error(`non-spell item has spell id: ${item.id}`)
  }
  for (const monster of registry.monsters) validateTags('monster', monster.id, monster.tags, tags)
  for (const skill of registry.skills) validateTags('skill', skill.id, skill.tags, tags)
  const itemIds = new Set(registry.items.map(item => item.id))
  for (const [biome, stock] of Object.entries(registry.shopStock)) for (const id of stock) if (!itemIds.has(id)) throw new Error(`unknown shop item in ${biome}: ${id}`)
  validatePrerequisites(registry.skills)
}

export const CONTENT: ContentRegistry = { items: ITEMS, monsters: MONSTERS, skills: SKILLS, tags: CONTENT_TAGS, shopStock: SHOP_STOCK }
validateContent(CONTENT)

export const shopStock = (biome: Biome): ItemId[] => SHOP_STOCK[biome]
