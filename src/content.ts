import type { Biome, EquipmentSlot, ItemId, MonsterRole, StatName, TileKind } from './types'
import type { ActionShape } from './engine/actions'
import { validateEquipmentEffects, type EquipmentEffect } from './effects'
import { validateItemPrice } from './engine/economy'

export interface WeaponProfile { damage: number; reach: number; shape: ActionShape; cooldown: number; tags: string[] }
export type ScriptSchool = 'ember' | 'verdant' | 'astral'
export type ScriptUpgradeHook = 'focusCost' | 'range' | 'potency'
export interface ScriptDefinition { itemId: ItemId; id: string; school: ScriptSchool; tags: string[]; focusCost: number; shape: ActionShape; range: number; upgrades: ScriptUpgradeHook[] }

export interface ItemDefinition {
  id: ItemId
  name: string
  glyph: string
  color: string
  slot?: EquipmentSlot
  weapon?: WeaponProfile
  defense?: number
  value: number
  use?: 'heal' | 'focus' | 'map' | 'teleport' | 'bomb' | 'rope' | 'key' | 'torch' | 'drill' | 'glide' | 'grapple' | 'bridge' | 'dash' | 'winch' | 'spell'
  spell?: string
  throwable?: boolean
  findable?: boolean
  tags?: string[]
  effects?: readonly EquipmentEffect[]
}

export interface MonsterDefinition { id: string; name: string; glyph: string; color: string; health: number; attack: number; defense: number; speed: number; ai: 'chase' | 'ranged' | 'wander' | 'guardian'; xp: number; biome: Biome; role?: MonsterRole; tags?: string[]; terrainAffinity?: TileKind[]; spawn?: 'ambient' | 'triggered' }
export interface SkillDefinition { id: string; name: string; stat: StatName; level: number; text: string; tags: string[]; prerequisites: string[] }
export interface ContentRegistry { items: readonly ItemDefinition[]; monsters: readonly MonsterDefinition[]; skills: readonly SkillDefinition[]; scripts: readonly ScriptDefinition[]; tags: readonly string[]; shopStock: Readonly<Record<Biome, readonly ItemId[]>> }

export const CONTENT_TAGS = ['strength', 'agility', 'vitality', 'intellect', 'mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary', 'rail', 'telegraph', 'cover', 'explosive', 'root', 'water', 'web', 'mobility', 'snare', 'fire', 'gas', 'light', 'displacement', 'darkness', 'counterplay', 'ward', 'dart', 'lock', 'ritual', 'cordmark', 'reedstep', 'smoke', 'lift', 'anchor', 'current', 'breakwall', 'flow', 'heat', 'guard', 'hook', 'blade', 'hammer', 'tide', 'salvage', 'force', 'grapple', 'bridge', 'dash', 'winch', 'wind', 'climb', 'grave', 'spirit', 'echo', 'curse', 'salt', 'mirror', 'brine', 'frost', 'ice', 'duel', 'ambush', 'guardian'] as const

export const ITEMS: ItemDefinition[] = [
  { id: 'whip', name: 'Courier Cord', glyph: '/', color: '#e7c680', slot: 'mainHand', weapon: { damage: 4, reach: 2, shape: 'line', cooldown: 0, tags: ['flexible', 'reach'] }, value: 45, effects: [{ id: 'surveying-strike', kind: 'action', actionId: 'player-strike', requires: ['reach'], add: { damage: 1 } }] },
  { id: 'machete', name: 'Brush Blade', glyph: '/', color: '#b8d6a0', slot: 'mainHand', weapon: { damage: 6, reach: 1, shape: 'adjacent', cooldown: 1, tags: ['cleave', 'wilds'] }, value: 75 },
  { id: 'pickaxe', name: 'Obsidian Axe', glyph: 'T', color: '#c7c4ba', slot: 'mainHand', weapon: { damage: 7, reach: 1, shape: 'cross', cooldown: 2, tags: ['rubble', 'piercing'] }, value: 110 },
  { id: 'spear', name: 'Cave Spear', glyph: '/', color: '#d0ae78', slot: 'mainHand', weapon: { damage: 8, reach: 2, shape: 'line', cooldown: 1, tags: ['piercing', 'reach'] }, value: 140, throwable: true },
  { id: 'tideSpear', name: 'Tide Spear', glyph: '/', color: '#76d8df', slot: 'mainHand', weapon: { damage: 7, reach: 2, shape: 'line', cooldown: 1, tags: ['water'] }, value: 145 },
  { id: 'sunblade', name: 'Sunstone Blade', glyph: '/', color: '#ffe181', slot: 'mainHand', weapon: { damage: 11, reach: 2, shape: 'cone', cooldown: 2, tags: ['radiant', 'cleave'] }, value: 260 },
  { id: 'cinderHammer', name: 'Cinder Hammer', glyph: 'T', color: '#f08d5b', slot: 'mainHand', weapon: { damage: 10, reach: 1, shape: 'cross', cooldown: 2, tags: ['hammer', 'fire', 'breakwall'] }, value: 250, tags: ['furnace'] },
  { id: 'smokeKnife', name: 'Soot Knife', glyph: '/', color: '#9ca1ad', slot: 'mainHand', weapon: { damage: 7, reach: 1, shape: 'adjacent', cooldown: 0, tags: ['blade', 'smoke', 'quick'] }, value: 180, tags: ['furnace'] },
  { id: 'liftHook', name: 'Lift Hook', glyph: 'J', color: '#e9c47e', slot: 'mainHand', weapon: { damage: 8, reach: 2, shape: 'line', cooldown: 1, tags: ['hook', 'lift', 'reach'] }, value: 210, tags: ['furnace'] },
  { id: 'anchorBlade', name: 'Anchor Blade', glyph: '/', color: '#78c4ce', slot: 'mainHand', weapon: { damage: 9, reach: 1, shape: 'cone', cooldown: 1, tags: ['blade', 'anchor', 'water'] }, value: 240, tags: ['floodedRuins'] },
  { id: 'tideCutter', name: 'Tide Cutter', glyph: '/', color: '#89d8df', slot: 'mainHand', weapon: { damage: 8, reach: 2, shape: 'line', cooldown: 1, tags: ['blade', 'current', 'tide'] }, value: 230, tags: ['floodedRuins'] },
  { id: 'buckler', name: 'Woven Guard', glyph: ')', color: '#bbc6cc', slot: 'offHand', defense: 2, value: 80, effects: [{ id: 'guarded', kind: 'passive', add: { defense: 1 } }] },
  { id: 'bellowsShield', name: 'Bellows Shield', glyph: ')', color: '#d9875d', slot: 'offHand', defense: 3, value: 190, tags: ['furnace', 'fire', 'guard'] },
  { id: 'chainGuard', name: 'Chain Guard', glyph: ')', color: '#bdc4ca', slot: 'offHand', defense: 2, value: 175, tags: ['furnace', 'hook', 'lift'] },
  { id: 'smokeMask', name: 'Soot Mask', glyph: '[', color: '#a3a8b3', slot: 'offHand', defense: 1, value: 165, tags: ['furnace', 'smoke'] },
  { id: 'anchorBuckler', name: 'Anchor Buckler', glyph: ')', color: '#75bbc7', slot: 'offHand', defense: 3, value: 210, tags: ['floodedRuins', 'anchor', 'guard'] },
  { id: 'currentOrb', name: 'Current Orb', glyph: 'o', color: '#9be5e9', slot: 'offHand', defense: 1, value: 185, tags: ['floodedRuins', 'current', 'flow'] },
  { id: 'lantern', name: 'Resin Lamp', glyph: 'i', color: '#ffe18a', slot: 'offHand', defense: 1, value: 95, use: 'torch' },
  { id: 'cap', name: 'Bark Cap', glyph: '[', color: '#d3b05c', slot: 'head', defense: 1, value: 55 },
  { id: 'mask', name: 'Moss Mask', glyph: '[', color: '#71a66d', slot: 'head', defense: 2, value: 120 },
  { id: 'coat', name: 'Bark-fiber Coat', glyph: '[', color: '#ad8056', slot: 'body', defense: 2, value: 100 },
  { id: 'mail', name: 'Stone Bead Coat', glyph: '[', color: '#8bb7d1', slot: 'body', defense: 4, value: 230 },
  { id: 'boots', name: 'Trail Boots', glyph: ';', color: '#c28b5d', slot: 'boots', defense: 1, value: 70 },
  { id: 'featherboots', name: 'Feather Boots', glyph: ';', color: '#e7e9f0', slot: 'boots', defense: 2, value: 180 },
  { id: 'ward', name: 'Spirit Charm', glyph: 'o', color: '#ca9fe4', slot: 'charm', defense: 2, value: 160, effects: [{ id: 'arcane-return', kind: 'triggered', trigger: 'spell', requires: ['arcane'], add: { focus: 1 } }] },
  { id: 'sunseal', name: 'Sunstone Seal', glyph: 'o', color: '#ffe181', slot: 'charm', defense: 3, value: 280 },
  { id: 'cordmarkTalisman', name: 'Cordmark Talisman', glyph: 'o', color: '#e4885d', slot: 'charm', defense: 1, value: 175, findable: false, tags: ['ritual', 'cordmark'] },
  { id: 'reedstepBoots', name: 'Reedstep Boots', glyph: ';', color: '#c7ad70', slot: 'boots', defense: 2, value: 155, findable: false, tags: ['mobility', 'reedstep'] },
  { id: 'tonic', name: 'Vital Tonic', glyph: '!', color: '#eb6571', value: 35, use: 'heal', throwable: true },
  { id: 'focusTonic', name: 'Focus Tonic', glyph: '!', color: '#7fa8e8', value: 50, use: 'focus', throwable: true },
  { id: 'mapScroll', name: 'Trail Map', glyph: '?', color: '#e6d2a6', value: 65, use: 'map' },
  { id: 'blinkRune', name: 'Swift-foot Charm', glyph: '?', color: '#bda8eb', value: 90, use: 'teleport' },
  { id: 'bombPack', name: 'Fire-ash Bundle', glyph: '*', color: '#ea8e64', value: 80, use: 'bomb' },
  { id: 'ropeBundle', name: 'Rope Bundle', glyph: '~', color: '#dab272', value: 55, use: 'rope' },
  { id: 'auger', name: 'Obsidian Auger', glyph: '%', color: '#c7c4ba', value: 100, use: 'drill', findable: false, tags: ['mine', 'mobility'] },
  { id: 'reedGlider', name: 'Reed Glider', glyph: '^', color: '#d8bc82', value: 95, use: 'glide', findable: false, tags: ['wilds', 'mobility'] },
  { id: 'grappleLine', name: 'Grappling Line', glyph: '⌁', color: '#d8b66f', value: 105, use: 'grapple', findable: false, tags: ['mobility', 'grapple', 'hook'] },
  { id: 'bridgeKit', name: 'Deployable Bridge', glyph: '=', color: '#caa56d', value: 85, use: 'bridge', findable: false, tags: ['mobility', 'bridge', 'water'] },
  { id: 'steamJetpack', name: 'Steam Jetpack', glyph: '↑', color: '#ee9364', value: 125, use: 'dash', findable: false, tags: ['mobility', 'dash', 'smoke', 'heat'] },
  { id: 'portableWinch', name: 'Portable Winch', glyph: 'W', color: '#c6c8cc', value: 115, use: 'winch', findable: false, tags: ['mobility', 'winch', 'force'] },
  { id: 'key', name: 'Carved Key', glyph: '?', color: '#d7c268', value: 40, use: 'key' },
  { id: 'rock', name: 'Throwing Stone', glyph: '*', color: '#9da5a9', value: 5, throwable: true },
  { id: 'fireJar', name: 'Fire Jar', glyph: '!', color: '#ff874f', value: 95, throwable: true },
  { id: 'cinderTonic', name: 'Cinder Tonic', glyph: '!', color: '#ef795a', value: 65, use: 'heal', tags: ['furnace', 'heat'] },
  { id: 'sootFilter', name: 'Soot Filter', glyph: '!', color: '#a3a8b3', value: 55, use: 'focus', tags: ['furnace', 'smoke'] },
  { id: 'breachCharge', name: 'Breach Charge', glyph: '*', color: '#f09c63', value: 100, use: 'bomb', tags: ['furnace', 'breakwall'] },
  { id: 'liftKey', name: 'Lift Key', glyph: '?', color: '#e9c47e', value: 75, use: 'teleport', tags: ['furnace', 'lift'] },
  { id: 'anchorSpool', name: 'Anchor Spool', glyph: '~', color: '#74c1cc', value: 80, use: 'rope', tags: ['floodedRuins', 'anchor'] },
  { id: 'boreGel', name: 'Bore Gel', glyph: '%', color: '#d6ae78', value: 110, use: 'drill', tags: ['furnace', 'breakwall'] },
  { id: 'wingfoil', name: 'Wingfoil', glyph: '^', color: '#b2dde0', value: 105, use: 'glide', tags: ['floodedRuins', 'flow'] },
  { id: 'floodSalt', name: 'Flood Salt', glyph: '!', color: '#8ed8df', value: 60, use: 'heal', tags: ['floodedRuins', 'water'] },
  { id: 'currentRune', name: 'Current Rune', glyph: '?', color: '#a1e5eb', value: 95, use: 'teleport', tags: ['floodedRuins', 'current'] },
  { id: 'firecracker', name: 'Firecracker', glyph: '*', color: '#f1b568', value: 70, use: 'bomb', tags: ['furnace', 'fire'] },
  { id: 'salvageKit', name: 'Salvage Kit', glyph: '!', color: '#c6d6d8', value: 60, use: 'focus', tags: ['floodedRuins', 'salvage'] },
  { id: 'ember', name: 'Ember Charm', glyph: '?', color: '#ff9c63', value: 120, use: 'spell', spell: 'ember' },
  { id: 'mend', name: 'Mending Charm', glyph: '?', color: '#91e0b1', value: 110, use: 'spell', spell: 'mend' },
  { id: 'sight', name: 'Sight Charm', glyph: '?', color: '#9dd7e4', value: 105, use: 'spell', spell: 'sight' },
  { id: 'root', name: 'Root Charm', glyph: '?', color: '#6dad62', value: 115, use: 'spell', spell: 'root' },
  { id: 'waterScript', name: 'Tide Charm', glyph: '?', color: '#7bcfe0', value: 120, use: 'spell', spell: 'water' },
  { id: 'lull', name: 'Lull Charm', glyph: '?', color: '#b6df8a', value: 135, use: 'spell', spell: 'lull' },
  { id: 'blink', name: 'Blink Charm', glyph: '?', color: '#bda8eb', value: 135, use: 'spell', spell: 'blink' },
  { id: 'pull', name: 'Pull Charm', glyph: '?', color: '#d2b1ed', value: 125, use: 'spell', spell: 'pull' },
  { id: 'gust', name: 'Gust Charm', glyph: '?', color: '#c1b8f4', value: 115, use: 'spell', spell: 'gust' },
  { id: 'wardScript', name: 'Ward Charm', glyph: '?', color: '#ecb7e3', value: 130, use: 'spell', spell: 'ward' },
  { id: 'gate', name: 'Gate Charm', glyph: '?', color: '#f1db78', value: 160, use: 'spell', spell: 'gate' },
  { id: 'windhook', name: 'Windhook', glyph: 'J', color: '#b9dcf4', slot: 'mainHand', weapon: { damage: 9, reach: 2, shape: 'line', cooldown: 1, tags: ['hook', 'wind', 'climb'] }, value: 220, tags: ['cliffs', 'wind', 'climb'] },
  { id: 'galeMantle', name: 'Gale Mantle', glyph: '[', color: '#d8edf9', slot: 'body', defense: 2, value: 200, tags: ['cliffs', 'wind', 'mobility'] },
  { id: 'cliffSpool', name: 'Cliff Spool', glyph: '~', color: '#d8b66f', value: 85, use: 'rope', tags: ['cliffs', 'climb'] },
  { id: 'thunderJar', name: 'Thunder Jar', glyph: '!', color: '#a8c7ff', value: 100, throwable: true, tags: ['cliffs', 'wind', 'force'] },
  { id: 'skyMap', name: 'Sky Map', glyph: '?', color: '#c4e5f2', value: 75, use: 'map', tags: ['cliffs', 'wind'] },
  { id: 'graveSickle', name: 'Grave Sickle', glyph: '/', color: '#c6b7d4', slot: 'mainHand', weapon: { damage: 9, reach: 1, shape: 'cone', cooldown: 1, tags: ['blade', 'grave', 'spirit'] }, value: 220, tags: ['burial', 'grave', 'spirit'] },
  { id: 'mourningBell', name: 'Mourning Bell', glyph: 'o', color: '#d3bce7', slot: 'charm', defense: 2, value: 205, tags: ['burial', 'spirit', 'echo'] },
  { id: 'graveSalt', name: 'Grave Salt', glyph: '!', color: '#d9d3c5', value: 65, use: 'heal', tags: ['burial', 'grave'] },
  { id: 'ancestorToken', name: 'Ancestor Token', glyph: '?', color: '#d9b9e3', value: 95, use: 'focus', tags: ['burial', 'spirit'] },
  { id: 'tombKey', name: 'Tomb Key', glyph: '?', color: '#bfaa70', value: 80, use: 'key', tags: ['burial', 'grave'] },
  { id: 'cursedMirror', name: 'Cursed Mirror', glyph: '☠', color: '#d9a3c6', value: 240, findable: false, tags: ['curse'] },
  { id: 'graveFleece', name: 'Grave Fleece', glyph: '☠', color: '#9d8baf', value: 180, findable: false, tags: ['curse'] },
  { id: 'stormIdol', name: 'Storm Idol', glyph: '☠', color: '#9ebfec', value: 180, findable: false, tags: ['curse'] },
  { id: 'oathShard', name: 'Oath Shard', glyph: '☠', color: '#e7c680', value: 200, findable: false, tags: ['curse'] }
]

export const ITEM = Object.fromEntries(ITEMS.map(item => [item.id, item])) as Record<string, ItemDefinition>
export const itemById = (id: string): ItemDefinition | undefined => ITEM[id]
export const isItemId = (id: unknown): id is ItemId => typeof id === 'string' && itemById(id) !== undefined
export const SCRIPTS: ScriptDefinition[] = [
  { itemId: 'ember', id: 'ember', school: 'ember', tags: ['fire', 'damage'], focusCost: 3, shape: 'line', range: 1, upgrades: ['potency', 'range'] },
  { itemId: 'mend', id: 'mend', school: 'verdant', tags: ['healing'], focusCost: 3, shape: 'adjacent', range: 1, upgrades: ['potency', 'focusCost'] },
  { itemId: 'root', id: 'root', school: 'verdant', tags: ['root', 'control'], focusCost: 3, shape: 'line', range: 2, upgrades: ['potency', 'range'] },
  { itemId: 'waterScript', id: 'water', school: 'verdant', tags: ['water', 'terrain'], focusCost: 3, shape: 'line', range: 2, upgrades: ['range', 'potency'] },
  { itemId: 'lull', id: 'lull', school: 'verdant', tags: ['creature', 'control'], focusCost: 4, shape: 'line', range: 1, upgrades: ['potency', 'range'] },
  { itemId: 'sight', id: 'sight', school: 'astral', tags: ['vision'], focusCost: 3, shape: 'burst', range: 1, upgrades: ['range', 'focusCost'] },
  { itemId: 'blink', id: 'blink', school: 'astral', tags: ['teleport', 'movement'], focusCost: 3, shape: 'line', range: 3, upgrades: ['range', 'focusCost'] },
  { itemId: 'gust', id: 'gust', school: 'astral', tags: ['force', 'movement'], focusCost: 3, shape: 'line', range: 1, upgrades: ['potency', 'range'] },
  { itemId: 'pull', id: 'pull', school: 'astral', tags: ['force', 'control'], focusCost: 3, shape: 'line', range: 2, upgrades: ['potency', 'range'] },
  { itemId: 'wardScript', id: 'ward', school: 'astral', tags: ['ward'], focusCost: 3, shape: 'adjacent', range: 1, upgrades: ['potency', 'focusCost'] },
  { itemId: 'gate', id: 'gate', school: 'astral', tags: ['teleport'], focusCost: 3, shape: 'line', range: 1, upgrades: ['range', 'focusCost'] }
]
export const SCRIPT_BY_ITEM = Object.fromEntries(SCRIPTS.map(script => [script.itemId, script])) as Record<string, ScriptDefinition>

export const MONSTERS: MonsterDefinition[] = [
  { id: 'rat', name: 'Field Rat', glyph: 'r', color: '#b8a598', health: 5, attack: 2, defense: 8, speed: 110, ai: 'chase', xp: 6, biome: 'mine' },
  { id: 'mole', name: 'Burrowing Mole', glyph: 'm', color: '#9298a2', health: 9, attack: 4, defense: 10, speed: 90, ai: 'chase', xp: 10, biome: 'mine' },
  { id: 'sapper', name: 'Fire-ash Thrower', glyph: 's', color: '#d6a263', health: 8, attack: 5, defense: 9, speed: 100, ai: 'ranged', xp: 14, biome: 'mine' },
  { id: 'beetle', name: 'Obsidian Beetle', glyph: 'b', color: '#d6c16d', health: 13, attack: 5, defense: 13, speed: 75, ai: 'chase', xp: 18, biome: 'mine' },
  { id: 'driller', name: 'Stone Breaker', glyph: 'd', color: '#dfb77a', health: 11, attack: 6, defense: 12, speed: 100, ai: 'ranged', xp: 20, biome: 'mine' },
  { id: 'railguard', name: 'Trail Guard', glyph: 'g', color: '#cad1dc', health: 12, attack: 6, defense: 12, speed: 100, ai: 'chase', xp: 22, biome: 'mine', tags: ['mine', 'rail'] },
  { id: 'fusewarden', name: 'Fire Keeper', glyph: 'f', color: '#f0a35e', health: 10, attack: 7, defense: 10, speed: 95, ai: 'ranged', xp: 24, biome: 'mine', tags: ['mine', 'telegraph', 'cover', 'explosive'] },
  { id: 'foreman', name: 'The Obsidian Warden', glyph: 'F', color: '#ffe080', health: 42, attack: 8, defense: 14, speed: 105, ai: 'guardian', xp: 70, biome: 'mine' },
  { id: 'thornling', name: 'Thornling', glyph: 't', color: '#86c064', health: 8, attack: 4, defense: 10, speed: 105, ai: 'chase', xp: 11, biome: 'wilds' },
  { id: 'boar', name: 'Moss Boar', glyph: 'b', color: '#a77d58', health: 15, attack: 7, defense: 11, speed: 115, ai: 'chase', xp: 19, biome: 'wilds' },
  { id: 'spitter', name: 'Vine Spitter', glyph: 'v', color: '#67ba7b', health: 10, attack: 6, defense: 9, speed: 90, ai: 'ranged', xp: 16, biome: 'wilds' },
  { id: 'wisp', name: 'Marsh Wisp', glyph: 'w', color: '#9be6bc', health: 7, attack: 6, defense: 12, speed: 130, ai: 'wander', xp: 21, biome: 'wilds' },
  { id: 'frog', name: 'Canopy Frog', glyph: 'f', color: '#a9d666', health: 11, attack: 6, defense: 11, speed: 120, ai: 'chase', xp: 23, biome: 'wilds' },
  { id: 'vinebinder', name: 'Vine Binder', glyph: 'V', color: '#5d9f67', health: 12, attack: 5, defense: 11, speed: 95, ai: 'ranged', xp: 24, biome: 'wilds', tags: ['wilds', 'root', 'telegraph'] },
  { id: 'marshskater', name: 'Marsh Skater', glyph: 'k', color: '#72b9b1', health: 10, attack: 6, defense: 12, speed: 100, ai: 'chase', xp: 25, biome: 'wilds', tags: ['wilds', 'water', 'mobility'] },
  { id: 'webweaver', name: 'Web Weaver', glyph: 'W', color: '#d5dce4', health: 11, attack: 5, defense: 12, speed: 90, ai: 'ranged', xp: 26, biome: 'wilds', tags: ['wilds', 'web', 'snare', 'telegraph'] },
  { id: 'startledBirds', name: 'Startled Birds', glyph: 'b', color: '#d8bc82', health: 4, attack: 3, defense: 8, speed: 125, ai: 'chase', xp: 8, biome: 'wilds', tags: ['wilds', 'mobility'], spawn: 'triggered' },
  { id: 'heartwood', name: 'Heartwood Stag', glyph: 'H', color: '#d1e281', health: 52, attack: 10, defense: 14, speed: 110, ai: 'guardian', xp: 90, biome: 'wilds' },
  { id: 'crawler', name: 'Crystal Crawler', glyph: 'c', color: '#7bcfe0', health: 14, attack: 7, defense: 13, speed: 95, ai: 'chase', xp: 23, biome: 'caverns' },
  { id: 'magma', name: 'Magma Newt', glyph: 'n', color: '#ef795a', health: 12, attack: 8, defense: 11, speed: 105, ai: 'chase', xp: 25, biome: 'caverns' },
  { id: 'echo', name: 'Echo Bat', glyph: 'e', color: '#ba9ddd', health: 9, attack: 7, defense: 12, speed: 140, ai: 'wander', xp: 26, biome: 'caverns' },
  { id: 'seer', name: 'Cave Seer', glyph: 's', color: '#ba8ae7', health: 13, attack: 9, defense: 12, speed: 95, ai: 'ranged', xp: 30, biome: 'caverns' },
  { id: 'slug', name: 'Salt Slug', glyph: 'u', color: '#a8c5cf', health: 19, attack: 8, defense: 15, speed: 65, ai: 'chase', xp: 31, biome: 'caverns' },
  { id: 'cinderimp', name: 'Cinder Imp', glyph: 'i', color: '#ee865d', health: 10, attack: 7, defense: 10, speed: 100, ai: 'ranged', xp: 28, biome: 'caverns', tags: ['caverns', 'fire', 'telegraph'] },
  { id: 'fumeeel', name: 'Fume Eel', glyph: 'u', color: '#8fc59a', health: 13, attack: 7, defense: 12, speed: 100, ai: 'chase', xp: 29, biome: 'caverns', tags: ['caverns', 'gas', 'mobility'] },
  { id: 'gloomseer', name: 'Gloom Seer', glyph: 'G', color: '#7c6d9f', health: 12, attack: 8, defense: 13, speed: 95, ai: 'ranged', xp: 32, biome: 'caverns', tags: ['caverns', 'darkness', 'light', 'counterplay'] },
  { id: 'crystalpuller', name: 'Crystal Puller', glyph: 'p', color: '#9ecce3', health: 14, attack: 7, defense: 13, speed: 90, ai: 'ranged', xp: 33, biome: 'caverns', tags: ['caverns', 'displacement', 'telegraph'] },
  { id: 'geode', name: 'Geode Wyrm', glyph: 'G', color: '#8ce5f2', health: 62, attack: 12, defense: 16, speed: 100, ai: 'guardian', xp: 115, biome: 'caverns' },
  { id: 'scarab', name: 'Ash Scarab', glyph: 's', color: '#d8b363', health: 16, attack: 8, defense: 15, speed: 95, ai: 'chase', xp: 32, biome: 'ruins' },
  { id: 'sentinel', name: 'Stone Sentinel', glyph: 'S', color: '#9da5aa', health: 23, attack: 10, defense: 17, speed: 75, ai: 'chase', xp: 40, biome: 'ruins' },
  { id: 'oracle', name: 'Dust Oracle', glyph: 'o', color: '#e9c489', health: 15, attack: 11, defense: 13, speed: 100, ai: 'ranged', xp: 45, biome: 'ruins' },
  { id: 'shade', name: 'Vault Shade', glyph: 'h', color: '#c1a5ed', health: 14, attack: 10, defense: 16, speed: 125, ai: 'wander', xp: 48, biome: 'ruins' },
  { id: 'cultist', name: 'Ash Cultist', glyph: 'c', color: '#df9a7c', health: 18, attack: 11, defense: 14, speed: 100, ai: 'ranged', xp: 51, biome: 'ruins' },
  { id: 'wardacolyte', name: 'Ward Acolyte', glyph: 'a', color: '#c29ce6', health: 15, attack: 8, defense: 13, speed: 90, ai: 'ranged', xp: 37, biome: 'ruins', tags: ['ruins', 'ward', 'ritual'] },
  { id: 'dartadept', name: 'Dart Adept', glyph: 'd', color: '#d8b576', health: 13, attack: 9, defense: 12, speed: 100, ai: 'ranged', xp: 38, biome: 'ruins', tags: ['ruins', 'dart', 'telegraph'] },
  { id: 'lockkeeper', name: 'Lock Keeper', glyph: 'k', color: '#c4b488', health: 18, attack: 9, defense: 15, speed: 80, ai: 'chase', xp: 42, biome: 'ruins', tags: ['ruins', 'lock', 'counterplay'] },
  { id: 'ritualist', name: 'Ash Ritualist', glyph: 'r', color: '#d88ea4', health: 14, attack: 10, defense: 13, speed: 95, ai: 'ranged', xp: 44, biome: 'ruins', tags: ['ruins', 'ritual', 'telegraph'] },
  { id: 'regent', name: 'The Stone Keeper', glyph: 'R', color: '#ffdb75', health: 84, attack: 15, defense: 19, speed: 110, ai: 'guardian', xp: 180, biome: 'ruins' },
  { id: 'cinderling', name: 'Cinderling', glyph: 'c', color: '#f08d5b', health: 18, attack: 12, defense: 16, speed: 110, ai: 'chase', xp: 48, biome: 'furnace', tags: ['furnace', 'fire', 'heat'] },
  { id: 'smokeskulk', name: 'Smoke Skulk', glyph: 's', color: '#9ca1ad', health: 15, attack: 11, defense: 17, speed: 130, ai: 'wander', xp: 50, biome: 'furnace', tags: ['furnace', 'smoke', 'mobility'] },
  { id: 'liftwarden', name: 'Lift Warden', glyph: 'l', color: '#e9c47e', health: 24, attack: 12, defense: 19, speed: 85, ai: 'chase', xp: 58, biome: 'furnace', tags: ['furnace', 'lift', 'counterplay'] },
  { id: 'slagcaster', name: 'Slag Caster', glyph: 's', color: '#e26e4c', health: 17, attack: 13, defense: 16, speed: 95, ai: 'ranged', xp: 60, biome: 'furnace', tags: ['furnace', 'fire', 'telegraph'] },
  { id: 'breakmaw', name: 'Break Maw', glyph: 'b', color: '#bd8567', health: 29, attack: 13, defense: 20, speed: 80, ai: 'chase', xp: 65, biome: 'furnace', tags: ['furnace', 'breakwall', 'force'] },
  { id: 'ashoracle', name: 'Ash Oracle', glyph: 'a', color: '#e6b4a2', health: 18, attack: 14, defense: 17, speed: 95, ai: 'ranged', xp: 66, biome: 'furnace', tags: ['furnace', 'smoke', 'telegraph'] },
  { id: 'kilnheart', name: 'The Kiln Heart', glyph: 'K', color: '#ffd070', health: 108, attack: 17, defense: 22, speed: 105, ai: 'guardian', xp: 240, biome: 'furnace', tags: ['furnace', 'fire', 'breakwall'] },
  { id: 'tidewraith', name: 'Tide Wraith', glyph: 't', color: '#85d9df', health: 20, attack: 14, defense: 18, speed: 120, ai: 'wander', xp: 65, biome: 'floodedRuins', tags: ['floodedRuins', 'water', 'current'] },
  { id: 'anchorcrab', name: 'Anchor Crab', glyph: 'a', color: '#83bdc6', health: 31, attack: 13, defense: 23, speed: 75, ai: 'chase', xp: 72, biome: 'floodedRuins', tags: ['floodedRuins', 'anchor', 'guard'] },
  { id: 'siltseer', name: 'Silt Seer', glyph: 's', color: '#c5d8d4', health: 19, attack: 15, defense: 18, speed: 95, ai: 'ranged', xp: 74, biome: 'floodedRuins', tags: ['floodedRuins', 'current', 'telegraph'] },
  { id: 'drownblade', name: 'Drownblade', glyph: 'd', color: '#71b7c3', health: 24, attack: 15, defense: 20, speed: 110, ai: 'chase', xp: 78, biome: 'floodedRuins', tags: ['floodedRuins', 'blade', 'water'] },
  { id: 'coralguard', name: 'Coral Guard', glyph: 'c', color: '#d69b92', health: 33, attack: 14, defense: 24, speed: 70, ai: 'chase', xp: 82, biome: 'floodedRuins', tags: ['floodedRuins', 'anchor', 'counterplay'] },
  { id: 'currentcaller', name: 'Current Caller', glyph: 'c', color: '#9ae5ea', health: 20, attack: 16, defense: 18, speed: 100, ai: 'ranged', xp: 84, biome: 'floodedRuins', tags: ['floodedRuins', 'current', 'displacement'] },
  { id: 'drownedRegent', name: 'The Drowned Regent', glyph: 'D', color: '#b3edf0', health: 132, attack: 20, defense: 25, speed: 100, ai: 'guardian', xp: 320, biome: 'floodedRuins', tags: ['floodedRuins', 'water', 'anchor'] },
  { id: 'cliffkite', name: 'Cliff Kite', glyph: 'k', color: '#b9dcf4', health: 18, attack: 10, defense: 15, speed: 135, ai: 'wander', xp: 55, biome: 'cliffs', tags: ['cliffs', 'wind', 'mobility'] },
  { id: 'ropeRaider', name: 'Rope Raider', glyph: 'r', color: '#d8b66f', health: 23, attack: 12, defense: 17, speed: 110, ai: 'chase', xp: 62, biome: 'cliffs', tags: ['cliffs', 'climb', 'hook'] },
  { id: 'galeSeer', name: 'Gale Seer', glyph: 'g', color: '#a8c7ff', health: 17, attack: 13, defense: 15, speed: 105, ai: 'ranged', xp: 67, biome: 'cliffs', tags: ['cliffs', 'wind', 'telegraph'] },
  { id: 'ledgeStalker', name: 'Ledge Stalker', glyph: 'l', color: '#8398b4', health: 25, attack: 11, defense: 19, speed: 95, ai: 'chase', xp: 66, biome: 'cliffs', tags: ['cliffs', 'climb', 'counterplay'] },
  { id: 'stormCrow', name: 'Storm Crow', glyph: 'c', color: '#778ec3', health: 15, attack: 14, defense: 14, speed: 145, ai: 'ranged', xp: 70, biome: 'cliffs', tags: ['cliffs', 'wind', 'force'] },
  { id: 'cragMoth', name: 'Crag Moth', glyph: 'm', color: '#ccd7eb', health: 16, attack: 12, defense: 15, speed: 140, ai: 'wander', xp: 58, biome: 'cliffs', tags: ['cliffs', 'wind', 'light'] },
  { id: 'screeHound', name: 'Scree Hound', glyph: 'h', color: '#8b99aa', health: 27, attack: 13, defense: 18, speed: 110, ai: 'chase', xp: 72, biome: 'cliffs', tags: ['cliffs', 'force', 'counterplay'] },
  { id: 'wireSinger', name: 'Wire Singer', glyph: 'w', color: '#adc9ed', health: 18, attack: 14, defense: 16, speed: 100, ai: 'ranged', xp: 74, biome: 'cliffs', tags: ['cliffs', 'wind', 'telegraph'] },
  { id: 'skyWarden', name: 'Sky Warden', glyph: 'S', color: '#ecf5ff', health: 118, attack: 18, defense: 23, speed: 115, ai: 'guardian', xp: 280, biome: 'cliffs', tags: ['cliffs', 'wind', 'climb'] },
  { id: 'graveMite', name: 'Grave Mite', glyph: 'm', color: '#a99aad', health: 20, attack: 11, defense: 17, speed: 105, ai: 'chase', xp: 60, biome: 'burial', tags: ['burial', 'grave'] },
  { id: 'ossuaryGuard', name: 'Ossuary Guard', glyph: 'o', color: '#d9d3c5', health: 32, attack: 13, defense: 22, speed: 75, ai: 'chase', xp: 74, biome: 'burial', tags: ['burial', 'grave', 'guard'] },
  { id: 'mourner', name: 'Mourner', glyph: 'm', color: '#c9a6db', health: 19, attack: 14, defense: 17, speed: 95, ai: 'ranged', xp: 76, biome: 'burial', tags: ['burial', 'spirit', 'telegraph'] },
  { id: 'ancestorEcho', name: 'Ancestor Echo', glyph: 'e', color: '#cbbde7', health: 17, attack: 15, defense: 16, speed: 130, ai: 'wander', xp: 78, biome: 'burial', tags: ['burial', 'spirit', 'echo'] },
  { id: 'tombWarden', name: 'Tomb Warden', glyph: 't', color: '#9d8b76', health: 29, attack: 14, defense: 21, speed: 85, ai: 'chase', xp: 80, biome: 'burial', tags: ['burial', 'grave', 'counterplay'] },
  { id: 'graveWisp', name: 'Grave Wisp', glyph: 'w', color: '#e0c9f0', health: 14, attack: 15, defense: 15, speed: 145, ai: 'wander', xp: 67, biome: 'burial', tags: ['burial', 'spirit', 'light'] },
  { id: 'barrowHound', name: 'Barrow Hound', glyph: 'h', color: '#8f796d', health: 28, attack: 15, defense: 19, speed: 105, ai: 'chase', xp: 82, biome: 'burial', tags: ['burial', 'grave', 'force'] },
  { id: 'lamenter', name: 'Lamenter', glyph: 'l', color: '#d6b4e5', health: 20, attack: 16, defense: 17, speed: 95, ai: 'ranged', xp: 84, biome: 'burial', tags: ['burial', 'spirit', 'telegraph'] },
  { id: 'barrowKing', name: 'The Barrow King', glyph: 'B', color: '#f0d5a0', health: 128, attack: 19, defense: 25, speed: 100, ai: 'guardian', xp: 310, biome: 'burial', tags: ['burial', 'grave', 'spirit'] },
  { id: 'saltRaider', name: 'Salt Raider', glyph: 'r', color: '#f0d889', health: 23, attack: 15, defense: 18, speed: 135, ai: 'chase', xp: 76, biome: 'saltFlats', tags: ['saltFlats', 'salt', 'mobility'] },
  { id: 'mirageSkirmisher', name: 'Mirage Skirmisher', glyph: 'm', color: '#c5e7ee', health: 18, attack: 16, defense: 16, speed: 125, ai: 'ranged', xp: 80, biome: 'saltFlats', tags: ['saltFlats', 'mirror', 'displacement', 'telegraph'] },
  { id: 'brineStalker', name: 'Brine Stalker', glyph: 'b', color: '#76bac0', health: 26, attack: 14, defense: 20, speed: 105, ai: 'wander', xp: 78, biome: 'saltFlats', tags: ['saltFlats', 'brine', 'ambush'] },
  { id: 'glassCutter', name: 'Glass Cutter', glyph: 'g', color: '#f4f1c9', health: 20, attack: 16, defense: 17, speed: 115, ai: 'ranged', xp: 83, biome: 'saltFlats', tags: ['saltFlats', 'mirror', 'dart', 'telegraph'] },
  { id: 'saltSovereign', name: 'The Salt Sovereign', glyph: 'S', color: '#fff2b0', health: 138, attack: 21, defense: 26, speed: 110, ai: 'guardian', xp: 330, biome: 'saltFlats', tags: ['saltFlats', 'salt', 'mirror', 'guardian'] },
  { id: 'rimeDuelist', name: 'Rime Duelist', glyph: 'd', color: '#b8dcf2', health: 29, attack: 17, defense: 22, speed: 105, ai: 'chase', xp: 88, biome: 'frostReliquary', tags: ['frostReliquary', 'frost', 'duel'] },
  { id: 'iceSentinel', name: 'Ice Sentinel', glyph: 'i', color: '#d5f1ff', health: 34, attack: 15, defense: 25, speed: 75, ai: 'chase', xp: 90, biome: 'frostReliquary', tags: ['frostReliquary', 'ice', 'guard', 'duel'] },
  { id: 'whiteoutOracle', name: 'Whiteout Oracle', glyph: 'o', color: '#e8f6ff', health: 21, attack: 17, defense: 18, speed: 100, ai: 'ranged', xp: 92, biome: 'frostReliquary', tags: ['frostReliquary', 'frost', 'telegraph', 'ritual'] },
  { id: 'shardHound', name: 'Shard Hound', glyph: 'h', color: '#9dc7e2', health: 27, attack: 16, defense: 19, speed: 125, ai: 'chase', xp: 86, biome: 'frostReliquary', tags: ['frostReliquary', 'ice', 'mobility'] },
  { id: 'reliquaryWarden', name: 'The Reliquary Warden', glyph: 'R', color: '#f0fbff', health: 146, attack: 22, defense: 28, speed: 95, ai: 'guardian', xp: 350, biome: 'frostReliquary', tags: ['frostReliquary', 'frost', 'ice', 'guardian', 'duel'] }
]
export const MONSTER = Object.fromEntries(MONSTERS.map(monster => [monster.id, monster])) as Record<string, MonsterDefinition>
export const monsterById = (id: string): MonsterDefinition | undefined => MONSTER[id]
export const isMonsterId = (id: unknown): id is string => typeof id === 'string' && monsterById(id) !== undefined
const tagged = (definition: MonsterDefinition, ...tags: string[]): boolean => tags.some(tag => definition.tags?.includes(tag))
export const monsterRoleFor = (definition: MonsterDefinition): MonsterRole => definition.role
  ?? (definition.ai === 'guardian' ? 'apex'
    : tagged(definition, 'ward') ? 'support'
      : tagged(definition, 'root', 'web', 'displacement', 'dart', 'ritual', 'lock') ? 'controller'
        : tagged(definition, 'ambush') ? 'ambusher'
          : tagged(definition, 'guard') ? 'guard'
            : definition.ai === 'ranged' ? 'artillery'
              : definition.ai === 'wander' ? 'scavenger'
                : definition.speed >= 120 ? 'skirmisher'
                  : 'pursuer')
const terrainByTag: Array<[string, TileKind[]]> = [
  ['rail', ['rail']], ['water', ['water', 'current', 'deepWater']], ['current', ['current', 'deepWater']], ['gas', ['gas', 'smoke']], ['smoke', ['smoke', 'gas']], ['fire', ['fireVent', 'lava']], ['heat', ['fireVent', 'lava']], ['climb', ['ledge']], ['grave', ['graveSoil', 'spiritPath']], ['spirit', ['spiritPath']], ['salt', ['saltMirror', 'brine']], ['mirror', ['saltMirror']], ['brine', ['brine']], ['frost', ['frostRime', 'ice']], ['ice', ['ice']], ['anchor', ['anchor']], ['lift', ['lift']]
]
export const terrainAffinityFor = (definition: MonsterDefinition): TileKind[] => definition.terrainAffinity ? [...definition.terrainAffinity] : Array.from(new Set(terrainByTag.filter(([tag]) => tagged(definition, tag)).flatMap(([, terrain]) => terrain)))

export const SKILLS: SkillDefinition[] = [
  ...(['Iron Grip', 'Cleave', 'Breaker', 'Counter', 'Unstoppable', 'Titan'] as const).map((name, i) => ({ id: `str${i + 1}`, name, stat: 'strength' as StatName, level: i + 1, text: ['Strength +1, melee damage +1', 'Strength +1, melee damage +1', 'Strength +1, break rubble', 'Strength +1, guard 2 damage', 'Strength +1, melee knockback', 'Strength +1, melee damage +2'][i], tags: ['strength'], prerequisites: i ? [`str${i}`] : [] })),
  ...(['Quick Step', 'Sure Aim', 'Skirmisher', 'Evasion', 'Fleet', 'Ghostwalk'] as const).map((name, i) => ({ id: `agi${i + 1}`, name, stat: 'agility' as StatName, level: i + 1, text: ['Agility +1, move +1 floor tile', 'Agility +1, melee reach +1', 'Agility +1, evade telegraphs 20%', 'Agility +1, dodge +3', 'Agility +1, move +1 floor tile', 'Agility +1, evade telegraphs +35%'][i], tags: ['agility'], prerequisites: i ? [`agi${i}`] : [] })),
  ...(['Hardy', 'Forager', 'Stalwart', 'Recovery', 'Ironblood', 'Last Stand'] as const).map((name, i) => ({ id: `vit${i +1}`, name, stat: 'vitality' as StatName, level: i + 1, text: ['Vitality +1, maximum health +2', 'Vitality +1, recovery +1', 'Vitality +1, shield 1 damage', 'Vitality +1, recovery +3', 'Vitality +1, hazards -2 damage', 'Vitality +1, rescue recovery +6'][i], tags: ['vitality'], prerequisites: i ? [`vit${i}`] : [] })),
  ...(['Spark', 'Insight', 'Ritualist', 'Sky Reader', 'Spirit Walker', 'Wayfinder'] as const).map((name, i) => ({ id: `int${i + 1}`, name, stat: 'intellect' as StatName, level: i + 1, text: ['Intellect +1, charms cost 1 less', 'Intellect +1, focus recovery +1', 'Intellect +1, charm range +1', 'Intellect +1, wards shield 2', 'Intellect +1, charm range +1', 'Intellect +1, focus recovery +1, sky paths'][i], tags: ['intellect'], prerequisites: i ? [`int${i}`] : [] }))
]
const SKILL = Object.fromEntries(SKILLS.map(skill => [skill.id, skill])) as Record<string, SkillDefinition>
export const isSkillId = (id: unknown): id is string => typeof id === 'string' && SKILL[id] !== undefined

export const biomeForFloor = (index: number): Biome => (['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'] as const)[Math.floor(index / 4)]
export const biomeName: Record<Biome, string> = { mine: 'Obsidian Mine', wilds: 'Cedar Wilds', caverns: 'Sea Caves', ruins: 'Stone Circle', furnace: 'Cinder Furnace', floodedRuins: 'Flooded Ruins', cliffs: 'Windcarved Cliffs', burial: 'Barrow Fields', saltFlats: 'Mirror Salt Flats', frostReliquary: 'Frost Reliquary' }
export const SHOP_STOCK: Record<Biome, ItemId[]> = {
  mine: ['tonic', 'bombPack', 'ropeBundle', 'auger', 'grappleLine', 'portableWinch', 'pickaxe', 'cap', 'key'],
  wilds: ['tonic', 'machete', 'focusTonic', 'root', 'waterScript', 'lull', 'boots', 'fireJar', 'mapScroll', 'reedGlider', 'grappleLine', 'bridgeKit', 'cordmarkTalisman', 'reedstepBoots'],
  caverns: ['focusTonic', 'lantern', 'spear', 'ember', 'mend', 'sight', 'blink', 'pull', 'blinkRune', 'reedGlider'],
  ruins: ['mail', 'ward', 'sunblade', 'gate', 'wardScript', 'blink', 'pull', 'key'],
  furnace: ['cinderTonic', 'sootFilter', 'breachCharge', 'boreGel', 'liftKey', 'steamJetpack', 'portableWinch', 'cinderHammer', 'smokeKnife', 'liftHook', 'bellowsShield', 'chainGuard', 'smokeMask'],
  floodedRuins: ['floodSalt', 'anchorSpool', 'wingfoil', 'currentRune', 'bridgeKit', 'grappleLine', 'salvageKit', 'anchorBlade', 'tideCutter', 'anchorBuckler', 'currentOrb'],
  cliffs: ['cliffSpool', 'windhook', 'galeMantle', 'thunderJar', 'skyMap', 'grappleLine', 'reedGlider'],
  burial: ['graveSalt', 'ancestorToken', 'tombKey', 'graveSickle', 'mourningBell', 'ward', 'mend'],
  saltFlats: ['focusTonic', 'tonic', 'fireJar', 'blink', 'pull', 'mapScroll', 'bridgeKit', 'reedGlider', 'sunblade'],
  frostReliquary: ['focusTonic', 'tonic', 'ward', 'mend', 'sight', 'blink', 'grappleLine', 'portableWinch', 'mail']
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
const validateScripts = (scripts: readonly ScriptDefinition[], items: readonly ItemDefinition[]): void => {
  const ids = new Set<string>()
  const itemIds = new Set<string>()
  const validShapes: ActionShape[] = ['adjacent', 'line', 'cone', 'burst', 'cross']
  for (const script of scripts) {
    if (!script.id || ids.has(script.id) || itemIds.has(script.itemId)) throw new Error(`invalid script: ${script.id}`)
    ids.add(script.id)
    itemIds.add(script.itemId)
    const item = items.find(current => current.id === script.itemId)
    if (!item || item.use !== 'spell' || item.spell !== script.id) throw new Error(`invalid script item: ${script.itemId}`)
    if (!['ember', 'verdant', 'astral'].includes(script.school) || !script.tags.length || !Number.isInteger(script.focusCost) || script.focusCost < 1 || !validShapes.includes(script.shape) || !Number.isInteger(script.range) || script.range < 1 || new Set(script.upgrades).size !== script.upgrades.length || !script.upgrades.every(upgrade => ['focusCost', 'range', 'potency'].includes(upgrade))) throw new Error(`invalid script definition: ${script.id}`)
  }
  if (scripts.length !== items.filter(item => item.use === 'spell').length) throw new Error('missing script definition')
}

export const validateContent = (registry: ContentRegistry): void => {
  validateIds('item', registry.items)
  validateIds('monster', registry.monsters)
  validateIds('skill', registry.skills)
  const tags = new Set(registry.tags)
  for (const item of registry.items) {
    validateItemPrice(item.value)
    validateTags('item', item.id, item.tags, tags)
    validateEquipmentEffects(item.effects, item.id)
    if (item.use === 'spell' && !item.spell) throw new Error(`spell item missing spell id: ${item.id}`)
    if (item.use !== 'spell' && item.spell) throw new Error(`non-spell item has spell id: ${item.id}`)
  }
  validateScripts(registry.scripts, registry.items)
  for (const monster of registry.monsters) validateTags('monster', monster.id, monster.tags, tags)
  for (const skill of registry.skills) validateTags('skill', skill.id, skill.tags, tags)
  const itemIds = new Set(registry.items.map(item => item.id))
  for (const [biome, stock] of Object.entries(registry.shopStock)) for (const id of stock) if (!itemIds.has(id)) throw new Error(`unknown shop item in ${biome}: ${id}`)
  validatePrerequisites(registry.skills)
}

export const CONTENT: ContentRegistry = { items: ITEMS, monsters: MONSTERS, skills: SKILLS, scripts: SCRIPTS, tags: CONTENT_TAGS, shopStock: SHOP_STOCK }
validateContent(CONTENT)

export const shopStock = (biome: Biome): ItemId[] => SHOP_STOCK[biome]
