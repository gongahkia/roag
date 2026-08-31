// 05e98dc888de59fb326fd8222ed289d73a0a4443
import { validateEquipmentEffects } from './effects';
import { validateItemPrice } from './engine/economy';
export const CONTENT_TAGS = ['strength', 'agility', 'vitality', 'intellect', 'mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary', 'rail', 'telegraph', 'cover', 'explosive', 'root', 'water', 'web', 'mobility', 'snare', 'fire', 'gas', 'light', 'displacement', 'darkness', 'counterplay', 'ward', 'dart', 'lock', 'ritual', 'cordmark', 'reedstep', 'smoke', 'lift', 'anchor', 'current', 'breakwall', 'flow', 'heat', 'guard', 'hook', 'blade', 'hammer', 'tide', 'salvage', 'force', 'grapple', 'bridge', 'dash', 'winch', 'wind', 'climb', 'grave', 'spirit', 'echo', 'curse', 'salt', 'mirror', 'brine', 'frost', 'ice', 'duel', 'ambush', 'guardian'];
export const ITEMS = [{
  id: 'whip',
  name: 'Courier Cord',
  glyph: '/',
  color: '#e7c680',
  slot: 'mainHand',
  weapon: {
    damage: 4,
    reach: 2,
    shape: 'line',
    cooldown: 0,
    tags: ['flexible', 'reach']
  },
  value: 45,
  effects: [{
    id: 'surveying-strike',
    kind: 'action',
    actionId: 'player-strike',
    requires: ['reach'],
    add: {
      damage: 1
    }
  }]
}, {
  id: 'machete',
  name: 'Brush Blade',
  glyph: '/',
  color: '#b8d6a0',
  slot: 'mainHand',
  weapon: {
    damage: 6,
    reach: 1,
    shape: 'adjacent',
    cooldown: 1,
    tags: ['cleave', 'wilds']
  },
  value: 75
}, {
  id: 'pickaxe',
  name: 'Obsidian Axe',
  glyph: 'T',
  color: '#c7c4ba',
  slot: 'mainHand',
  weapon: {
    damage: 7,
    reach: 1,
    shape: 'cross',
    cooldown: 2,
    tags: ['rubble', 'piercing']
  },
  value: 110
}, {
  id: 'spear',
  name: 'Cave Spear',
  glyph: '/',
  color: '#d0ae78',
  slot: 'mainHand',
  weapon: {
    damage: 8,
    reach: 2,
    shape: 'line',
    cooldown: 1,
    tags: ['piercing', 'reach']
  },
  value: 140,
  throwable: true
}, {
  id: 'tideSpear',
  name: 'Tide Spear',
  glyph: '/',
  color: '#76d8df',
  slot: 'mainHand',
  weapon: {
    damage: 7,
    reach: 2,
    shape: 'line',
    cooldown: 1,
    tags: ['water']
  },
  value: 145
}, {
  id: 'sunblade',
  name: 'Sunstone Blade',
  glyph: '/',
  color: '#ffe181',
  slot: 'mainHand',
  weapon: {
    damage: 11,
    reach: 2,
    shape: 'cone',
    cooldown: 2,
    tags: ['radiant', 'cleave']
  },
  value: 260
}, {
  id: 'cinderHammer',
  name: 'Cinder Hammer',
  glyph: 'T',
  color: '#f08d5b',
  slot: 'mainHand',
  weapon: {
    damage: 10,
    reach: 1,
    shape: 'cross',
    cooldown: 2,
    tags: ['hammer', 'fire', 'breakwall']
  },
  value: 250,
  tags: ['furnace']
}, {
  id: 'smokeKnife',
  name: 'Soot Knife',
  glyph: '/',
  color: '#9ca1ad',
  slot: 'mainHand',
  weapon: {
    damage: 7,
    reach: 1,
    shape: 'adjacent',
    cooldown: 0,
    tags: ['blade', 'smoke', 'quick']
  },
  value: 180,
  tags: ['furnace']
}, {
  id: 'liftHook',
  name: 'Lift Hook',
  glyph: 'J',
  color: '#e9c47e',
  slot: 'mainHand',
  weapon: {
    damage: 8,
    reach: 2,
    shape: 'line',
    cooldown: 1,
    tags: ['hook', 'lift', 'reach']
  },
  value: 210,
  tags: ['furnace']
}, {
  id: 'anchorBlade',
  name: 'Anchor Blade',
  glyph: '/',
  color: '#78c4ce',
  slot: 'mainHand',
  weapon: {
    damage: 9,
    reach: 1,
    shape: 'cone',
    cooldown: 1,
    tags: ['blade', 'anchor', 'water']
  },
  value: 240,
  tags: ['floodedRuins']
}, {
  id: 'tideCutter',
  name: 'Tide Cutter',
  glyph: '/',
  color: '#89d8df',
  slot: 'mainHand',
  weapon: {
    damage: 8,
    reach: 2,
    shape: 'line',
    cooldown: 1,
    tags: ['blade', 'current', 'tide']
  },
  value: 230,
  tags: ['floodedRuins']
}, {
  id: 'buckler',
  name: 'Woven Guard',
  glyph: ')',
  color: '#bbc6cc',
  slot: 'offHand',
  defense: 2,
  value: 80,
  effects: [{
    id: 'guarded',
    kind: 'passive',
    add: {
      defense: 1
    }
  }]
}, {
  id: 'bellowsShield',
  name: 'Bellows Shield',
  glyph: ')',
  color: '#d9875d',
  slot: 'offHand',
  defense: 3,
  value: 190,
  tags: ['furnace', 'fire', 'guard']
}, {
  id: 'chainGuard',
  name: 'Chain Guard',
  glyph: ')',
  color: '#bdc4ca',
  slot: 'offHand',
  defense: 2,
  value: 175,
  tags: ['furnace', 'hook', 'lift']
}, {
  id: 'smokeMask',
  name: 'Soot Mask',
  glyph: '[',
  color: '#a3a8b3',
  slot: 'offHand',
  defense: 1,
  value: 165,
  tags: ['furnace', 'smoke']
}, {
  id: 'anchorBuckler',
  name: 'Anchor Buckler',
  glyph: ')',
  color: '#75bbc7',
  slot: 'offHand',
  defense: 3,
  value: 210,
  tags: ['floodedRuins', 'anchor', 'guard']
}, {
  id: 'currentOrb',
  name: 'Current Orb',
  glyph: 'o',
  color: '#9be5e9',
  slot: 'offHand',
  defense: 1,
  value: 185,
  tags: ['floodedRuins', 'current', 'flow']
}, {
  id: 'lantern',
  name: 'Resin Lamp',
  glyph: 'i',
  color: '#ffe18a',
  slot: 'offHand',
  defense: 1,
  value: 95,
  use: 'torch'
}, {
  id: 'cap',
  name: 'Bark Cap',
  glyph: '[',
  color: '#d3b05c',
  slot: 'head',
  defense: 1,
  value: 55
}, {
  id: 'mask',
  name: 'Moss Mask',
  glyph: '[',
  color: '#71a66d',
  slot: 'head',
  defense: 2,
  value: 120
}, {
  id: 'coat',
  name: 'Bark-fiber Coat',
  glyph: '[',
  color: '#ad8056',
  slot: 'body',
  defense: 2,
  value: 100
}, {
  id: 'mail',
  name: 'Stone Bead Coat',
  glyph: '[',
  color: '#8bb7d1',
  slot: 'body',
  defense: 4,
  value: 230
}, {
  id: 'boots',
  name: 'Trail Boots',
  glyph: ';',
  color: '#c28b5d',
  slot: 'boots',
  defense: 1,
  value: 70
}, {
  id: 'featherboots',
  name: 'Feather Boots',
  glyph: ';',
  color: '#e7e9f0',
  slot: 'boots',
  defense: 2,
  value: 180
}, {
  id: 'ward',
  name: 'Spirit Charm',
  glyph: 'o',
  color: '#ca9fe4',
  slot: 'charm',
  defense: 2,
  value: 160,
  effects: [{
    id: 'arcane-return',
    kind: 'triggered',
    trigger: 'spell',
    requires: ['arcane'],
    add: {
      focus: 1
    }
  }]
}, {
  id: 'sunseal',
  name: 'Sunstone Seal',
  glyph: 'o',
  color: '#ffe181',
  slot: 'charm',
  defense: 3,
  value: 280
}, {
  id: 'cordmarkTalisman',
  name: 'Cordmark Talisman',
  glyph: 'o',
  color: '#e4885d',
  slot: 'charm',
  defense: 1,
  value: 175,
  findable: false,
  tags: ['ritual', 'cordmark']
}, {
  id: 'reedstepBoots',
  name: 'Reedstep Boots',
  glyph: ';',
  color: '#c7ad70',
  slot: 'boots',
  defense: 2,
  value: 155,
  findable: false,
  tags: ['mobility', 'reedstep']
}, {
  id: 'tonic',
  name: 'Vital Tonic',
  glyph: '!',
  color: '#eb6571',
  value: 35,
  use: 'heal',
  throwable: true
}, {
  id: 'focusTonic',
  name: 'Focus Tonic',
  glyph: '!',
  color: '#7fa8e8',
  value: 50,
  use: 'focus',
  throwable: true
}, {
  id: 'mapScroll',
  name: 'Trail Map',
  glyph: '?',
  color: '#e6d2a6',
  value: 65,
  use: 'map'
}, {
  id: 'blinkRune',
  name: 'Swift-foot Charm',
  glyph: '?',
  color: '#bda8eb',
  value: 90,
  use: 'teleport'
}, {
  id: 'bombPack',
  name: 'Fire-ash Bundle',
  glyph: '*',
  color: '#ea8e64',
  value: 80,
  use: 'bomb'
}, {
  id: 'ropeBundle',
  name: 'Rope Bundle',
  glyph: '~',
  color: '#dab272',
  value: 55,
  use: 'rope'
}, {
  id: 'auger',
  name: 'Obsidian Auger',
  glyph: '%',
  color: '#c7c4ba',
  value: 100,
  use: 'drill',
  findable: false,
  tags: ['mine', 'mobility']
}, {
  id: 'reedGlider',
  name: 'Reed Glider',
  glyph: '^',
  color: '#d8bc82',
  value: 95,
  use: 'glide',
  findable: false,
  tags: ['wilds', 'mobility']
}, {
  id: 'grappleLine',
  name: 'Grappling Line',
  glyph: '⌁',
  color: '#d8b66f',
  value: 105,
  use: 'grapple',
  findable: false,
  tags: ['mobility', 'grapple', 'hook']
}, {
  id: 'bridgeKit',
  name: 'Deployable Bridge',
  glyph: '=',
  color: '#caa56d',
  value: 85,
  use: 'bridge',
  findable: false,
  tags: ['mobility', 'bridge', 'water']
}, {
  id: 'steamJetpack',
  name: 'Steam Jetpack',
  glyph: '↑',
  color: '#ee9364',
  value: 125,
  use: 'dash',
  findable: false,
  tags: ['mobility', 'dash', 'smoke', 'heat']
}, {
  id: 'portableWinch',
  name: 'Portable Winch',
  glyph: 'W',
  color: '#c6c8cc',
  value: 115,
  use: 'winch',
  findable: false,
  tags: ['mobility', 'winch', 'force']
}, {
  id: 'key',
  name: 'Carved Key',
  glyph: '?',
  color: '#d7c268',
  value: 40,
  use: 'key'
}, {
  id: 'rock',
  name: 'Throwing Stone',
  glyph: '*',
  color: '#9da5a9',
  value: 5,
  throwable: true
}, {
  id: 'fireJar',
  name: 'Fire Jar',
  glyph: '!',
  color: '#ff874f',
  value: 95,
  throwable: true
}, {
  id: 'cinderTonic',
  name: 'Cinder Tonic',
  glyph: '!',
  color: '#ef795a',
  value: 65,
  use: 'heal',
  tags: ['furnace', 'heat']
}, {
  id: 'sootFilter',
  name: 'Soot Filter',
  glyph: '!',
  color: '#a3a8b3',
  value: 55,
  use: 'focus',
  tags: ['furnace', 'smoke']
}, {
  id: 'breachCharge',
  name: 'Breach Charge',
  glyph: '*',
  color: '#f09c63',
  value: 100,
  use: 'bomb',
  tags: ['furnace', 'breakwall']
}, {
  id: 'liftKey',
  name: 'Lift Key',
  glyph: '?',
  color: '#e9c47e',
  value: 75,
  use: 'teleport',
  tags: ['furnace', 'lift']
}, {
  id: 'anchorSpool',
  name: 'Anchor Spool',
  glyph: '~',
  color: '#74c1cc',
  value: 80,
  use: 'rope',
  tags: ['floodedRuins', 'anchor']
}, {
  id: 'boreGel',
  name: 'Bore Gel',
  glyph: '%',
  color: '#d6ae78',
  value: 110,
  use: 'drill',
  tags: ['furnace', 'breakwall']
}, {
  id: 'wingfoil',
  name: 'Wingfoil',
  glyph: '^',
  color: '#b2dde0',
  value: 105,
  use: 'glide',
  tags: ['floodedRuins', 'flow']
}, {
  id: 'floodSalt',
  name: 'Flood Salt',
  glyph: '!',
  color: '#8ed8df',
  value: 60,
  use: 'heal',
  tags: ['floodedRuins', 'water']
}, {
  id: 'currentRune',
  name: 'Current Rune',
  glyph: '?',
  color: '#a1e5eb',
  value: 95,
  use: 'teleport',
  tags: ['floodedRuins', 'current']
}, {
  id: 'firecracker',
  name: 'Firecracker',
  glyph: '*',
  color: '#f1b568',
  value: 70,
  use: 'bomb',
  tags: ['furnace', 'fire']
}, {
  id: 'salvageKit',
  name: 'Salvage Kit',
  glyph: '!',
  color: '#c6d6d8',
  value: 60,
  use: 'focus',
  tags: ['floodedRuins', 'salvage']
}, {
  id: 'ember',
  name: 'Ember Charm',
  glyph: '?',
  color: '#ff9c63',
  value: 120,
  use: 'spell',
  spell: 'ember'
}, {
  id: 'mend',
  name: 'Mending Charm',
  glyph: '?',
  color: '#91e0b1',
  value: 110,
  use: 'spell',
  spell: 'mend'
}, {
  id: 'sight',
  name: 'Sight Charm',
  glyph: '?',
  color: '#9dd7e4',
  value: 105,
  use: 'spell',
  spell: 'sight'
}, {
  id: 'root',
  name: 'Root Charm',
  glyph: '?',
  color: '#6dad62',
  value: 115,
  use: 'spell',
  spell: 'root'
}, {
  id: 'waterScript',
  name: 'Tide Charm',
  glyph: '?',
  color: '#7bcfe0',
  value: 120,
  use: 'spell',
  spell: 'water'
}, {
  id: 'lull',
  name: 'Lull Charm',
  glyph: '?',
  color: '#b6df8a',
  value: 135,
  use: 'spell',
  spell: 'lull'
}, {
  id: 'blink',
  name: 'Blink Charm',
  glyph: '?',
  color: '#bda8eb',
  value: 135,
  use: 'spell',
  spell: 'blink'
}, {
  id: 'pull',
  name: 'Pull Charm',
  glyph: '?',
  color: '#d2b1ed',
  value: 125,
  use: 'spell',
  spell: 'pull'
}, {
  id: 'gust',
  name: 'Gust Charm',
  glyph: '?',
  color: '#c1b8f4',
  value: 115,
  use: 'spell',
  spell: 'gust'
}, {
  id: 'wardScript',
  name: 'Ward Charm',
  glyph: '?',
  color: '#ecb7e3',
  value: 130,
  use: 'spell',
  spell: 'ward'
}, {
  id: 'gate',
  name: 'Gate Charm',
  glyph: '?',
  color: '#f1db78',
  value: 160,
  use: 'spell',
  spell: 'gate'
}, {
  id: 'windhook',
  name: 'Windhook',
  glyph: 'J',
  color: '#b9dcf4',
  slot: 'mainHand',
  weapon: {
    damage: 9,
    reach: 2,
    shape: 'line',
    cooldown: 1,
    tags: ['hook', 'wind', 'climb']
  },
  value: 220,
  tags: ['cliffs', 'wind', 'climb']
}, {
  id: 'galeMantle',
  name: 'Gale Mantle',
  glyph: '[',
  color: '#d8edf9',
  slot: 'body',
  defense: 2,
  value: 200,
  tags: ['cliffs', 'wind', 'mobility']
}, {
  id: 'cliffSpool',
  name: 'Cliff Spool',
  glyph: '~',
  color: '#d8b66f',
  value: 85,
  use: 'rope',
  tags: ['cliffs', 'climb']
}, {
  id: 'thunderJar',
  name: 'Thunder Jar',
  glyph: '!',
  color: '#a8c7ff',
  value: 100,
  throwable: true,
  tags: ['cliffs', 'wind', 'force']
}, {
  id: 'skyMap',
  name: 'Sky Map',
  glyph: '?',
  color: '#c4e5f2',
  value: 75,
  use: 'map',
  tags: ['cliffs', 'wind']
}, {
  id: 'graveSickle',
  name: 'Grave Sickle',
  glyph: '/',
  color: '#c6b7d4',
  slot: 'mainHand',
  weapon: {
    damage: 9,
    reach: 1,
    shape: 'cone',
    cooldown: 1,
    tags: ['blade', 'grave', 'spirit']
  },
  value: 220,
  tags: ['burial', 'grave', 'spirit']
}, {
  id: 'mourningBell',
  name: 'Mourning Bell',
  glyph: 'o',
  color: '#d3bce7',
  slot: 'charm',
  defense: 2,
  value: 205,
  tags: ['burial', 'spirit', 'echo']
}, {
  id: 'graveSalt',
  name: 'Grave Salt',
  glyph: '!',
  color: '#d9d3c5',
  value: 65,
  use: 'heal',
  tags: ['burial', 'grave']
}, {
  id: 'ancestorToken',
  name: 'Ancestor Token',
  glyph: '?',
  color: '#d9b9e3',
  value: 95,
  use: 'focus',
  tags: ['burial', 'spirit']
}, {
  id: 'tombKey',
  name: 'Tomb Key',
  glyph: '?',
  color: '#bfaa70',
  value: 80,
  use: 'key',
  tags: ['burial', 'grave']
}, {
  id: 'cursedMirror',
  name: 'Cursed Mirror',
  glyph: '☠',
  color: '#d9a3c6',
  value: 240,
  findable: false,
  tags: ['curse']
}, {
  id: 'graveFleece',
  name: 'Grave Fleece',
  glyph: '☠',
  color: '#9d8baf',
  value: 180,
  findable: false,
  tags: ['curse']
}, {
  id: 'stormIdol',
  name: 'Storm Idol',
  glyph: '☠',
  color: '#9ebfec',
  value: 180,
  findable: false,
  tags: ['curse']
}, {
  id: 'oathShard',
  name: 'Oath Shard',
  glyph: '☠',
  color: '#e7c680',
  value: 200,
  findable: false,
  tags: ['curse']
}];
const voyagerTerms = [['Courier', 'Field'], ['Obsidian', 'Alloy'], ['Cinder', 'Thermal'], ['Soot', 'Carbon'], ['Tide', 'Flux'], ['Sunstone', 'Solar'], ['Spirit', 'Signal'], ['Charm', 'Module'], ['Talisman', 'Module'], ['Tonic', 'Gel'], ['Trail', 'Survey'], ['Map', 'Survey Chart'], ['Rope', 'Line'], ['Fire-ash', 'Breach'], ['Fire', 'Thermal'], ['Stone', 'Hull'], ['Moss', 'Biofilter'], ['Bark', 'Composite'], ['Resin', 'Signal'], ['Feather', 'Micrograv'], ['Reed', 'Glide'], ['Wind', 'Vector'], ['Gale', 'Vector'], ['Sky', 'Star'], ['Grave', 'Memorial'], ['Ancestor', 'Archive'], ['Tomb', 'Archive'], ['Mourning', 'Archive'], ['Cursed', 'Corrupted'], ['Oath', 'Protocol']];
const voyagerName = name => voyagerTerms.reduce((current, [source, replacement]) => current.replaceAll(source, replacement), name);
ITEMS.forEach(item => {
  item.name = voyagerName(item.name);
});
export const ITEM = Object.fromEntries(ITEMS.map(item => [item.id, item]));
export const itemById = id => ITEM[id];
export const isItemId = id => typeof id === 'string' && itemById(id) !== undefined;
export const SCRIPTS = [{
  itemId: 'ember',
  id: 'ember',
  school: 'ember',
  tags: ['fire', 'damage'],
  focusCost: 3,
  shape: 'line',
  range: 1,
  upgrades: ['potency', 'range']
}, {
  itemId: 'mend',
  id: 'mend',
  school: 'verdant',
  tags: ['healing'],
  focusCost: 3,
  shape: 'adjacent',
  range: 1,
  upgrades: ['potency', 'focusCost']
}, {
  itemId: 'root',
  id: 'root',
  school: 'verdant',
  tags: ['root', 'control'],
  focusCost: 3,
  shape: 'line',
  range: 2,
  upgrades: ['potency', 'range']
}, {
  itemId: 'waterScript',
  id: 'water',
  school: 'verdant',
  tags: ['water', 'terrain'],
  focusCost: 3,
  shape: 'line',
  range: 2,
  upgrades: ['range', 'potency']
}, {
  itemId: 'lull',
  id: 'lull',
  school: 'verdant',
  tags: ['creature', 'control'],
  focusCost: 4,
  shape: 'line',
  range: 1,
  upgrades: ['potency', 'range']
}, {
  itemId: 'sight',
  id: 'sight',
  school: 'astral',
  tags: ['vision'],
  focusCost: 3,
  shape: 'burst',
  range: 1,
  upgrades: ['range', 'focusCost']
}, {
  itemId: 'blink',
  id: 'blink',
  school: 'astral',
  tags: ['teleport', 'movement'],
  focusCost: 3,
  shape: 'line',
  range: 3,
  upgrades: ['range', 'focusCost']
}, {
  itemId: 'gust',
  id: 'gust',
  school: 'astral',
  tags: ['force', 'movement'],
  focusCost: 3,
  shape: 'line',
  range: 1,
  upgrades: ['potency', 'range']
}, {
  itemId: 'pull',
  id: 'pull',
  school: 'astral',
  tags: ['force', 'control'],
  focusCost: 3,
  shape: 'line',
  range: 2,
  upgrades: ['potency', 'range']
}, {
  itemId: 'wardScript',
  id: 'ward',
  school: 'astral',
  tags: ['ward'],
  focusCost: 3,
  shape: 'adjacent',
  range: 1,
  upgrades: ['potency', 'focusCost']
}, {
  itemId: 'gate',
  id: 'gate',
  school: 'astral',
  tags: ['teleport'],
  focusCost: 3,
  shape: 'line',
  range: 1,
  upgrades: ['range', 'focusCost']
}];
export const SCRIPT_BY_ITEM = Object.fromEntries(SCRIPTS.map(script => [script.itemId, script]));
const colonyMonsterPrefix = {
  mine: 'Kestrel',
  wilds: 'Verdant',
  caverns: 'Pelagos',
  ruins: 'Orison',
  furnace: 'Helion',
  floodedRuins: 'Nerida',
  cliffs: 'Aerie',
  burial: 'Memorial',
  saltFlats: 'Halcyon',
  frostReliquary: 'Borealis'
};
export const MONSTERS = [{
  id: 'rat',
  name: 'Field Rat',
  glyph: 'r',
  color: '#b8a598',
  health: 5,
  attack: 2,
  defense: 8,
  speed: 110,
  ai: 'chase',
  xp: 6,
  biome: 'mine'
}, {
  id: 'mole',
  name: 'Burrowing Mole',
  glyph: 'm',
  color: '#9298a2',
  health: 9,
  attack: 4,
  defense: 10,
  speed: 90,
  ai: 'chase',
  xp: 10,
  biome: 'mine'
}, {
  id: 'sapper',
  name: 'Fire-ash Thrower',
  glyph: 's',
  color: '#d6a263',
  health: 8,
  attack: 5,
  defense: 9,
  speed: 100,
  ai: 'ranged',
  xp: 14,
  biome: 'mine'
}, {
  id: 'beetle',
  name: 'Obsidian Beetle',
  glyph: 'b',
  color: '#d6c16d',
  health: 13,
  attack: 5,
  defense: 13,
  speed: 75,
  ai: 'chase',
  xp: 18,
  biome: 'mine'
}, {
  id: 'driller',
  name: 'Stone Breaker',
  glyph: 'd',
  color: '#dfb77a',
  health: 11,
  attack: 6,
  defense: 12,
  speed: 100,
  ai: 'ranged',
  xp: 20,
  biome: 'mine'
}, {
  id: 'railguard',
  name: 'Trail Guard',
  glyph: 'g',
  color: '#cad1dc',
  health: 12,
  attack: 6,
  defense: 12,
  speed: 100,
  ai: 'chase',
  xp: 22,
  biome: 'mine',
  tags: ['mine', 'rail']
}, {
  id: 'fusewarden',
  name: 'Fire Keeper',
  glyph: 'f',
  color: '#f0a35e',
  health: 10,
  attack: 7,
  defense: 10,
  speed: 95,
  ai: 'ranged',
  xp: 24,
  biome: 'mine',
  tags: ['mine', 'telegraph', 'cover', 'explosive']
}, {
  id: 'foreman',
  name: 'The Obsidian Warden',
  glyph: 'F',
  color: '#ffe080',
  health: 42,
  attack: 8,
  defense: 14,
  speed: 105,
  ai: 'guardian',
  xp: 70,
  biome: 'mine'
}, {
  id: 'thornling',
  name: 'Thornling',
  glyph: 't',
  color: '#86c064',
  health: 8,
  attack: 4,
  defense: 10,
  speed: 105,
  ai: 'chase',
  xp: 11,
  biome: 'wilds'
}, {
  id: 'boar',
  name: 'Moss Boar',
  glyph: 'b',
  color: '#a77d58',
  health: 15,
  attack: 7,
  defense: 11,
  speed: 115,
  ai: 'chase',
  xp: 19,
  biome: 'wilds'
}, {
  id: 'spitter',
  name: 'Vine Spitter',
  glyph: 'v',
  color: '#67ba7b',
  health: 10,
  attack: 6,
  defense: 9,
  speed: 90,
  ai: 'ranged',
  xp: 16,
  biome: 'wilds'
}, {
  id: 'wisp',
  name: 'Marsh Wisp',
  glyph: 'w',
  color: '#9be6bc',
  health: 7,
  attack: 6,
  defense: 12,
  speed: 130,
  ai: 'wander',
  xp: 21,
  biome: 'wilds'
}, {
  id: 'frog',
  name: 'Canopy Frog',
  glyph: 'f',
  color: '#a9d666',
  health: 11,
  attack: 6,
  defense: 11,
  speed: 120,
  ai: 'chase',
  xp: 23,
  biome: 'wilds'
}, {
  id: 'vinebinder',
  name: 'Vine Binder',
  glyph: 'V',
  color: '#5d9f67',
  health: 12,
  attack: 5,
  defense: 11,
  speed: 95,
  ai: 'ranged',
  xp: 24,
  biome: 'wilds',
  tags: ['wilds', 'root', 'telegraph']
}, {
  id: 'marshskater',
  name: 'Marsh Skater',
  glyph: 'k',
  color: '#72b9b1',
  health: 10,
  attack: 6,
  defense: 12,
  speed: 100,
  ai: 'chase',
  xp: 25,
  biome: 'wilds',
  tags: ['wilds', 'water', 'mobility']
}, {
  id: 'webweaver',
  name: 'Web Weaver',
  glyph: 'W',
  color: '#d5dce4',
  health: 11,
  attack: 5,
  defense: 12,
  speed: 90,
  ai: 'ranged',
  xp: 26,
  biome: 'wilds',
  tags: ['wilds', 'web', 'snare', 'telegraph']
}, {
  id: 'startledBirds',
  name: 'Startled Birds',
  glyph: 'b',
  color: '#d8bc82',
  health: 4,
  attack: 3,
  defense: 8,
  speed: 125,
  ai: 'chase',
  xp: 8,
  biome: 'wilds',
  tags: ['wilds', 'mobility'],
  spawn: 'triggered'
}, {
  id: 'heartwood',
  name: 'Heartwood Stag',
  glyph: 'H',
  color: '#d1e281',
  health: 52,
  attack: 10,
  defense: 14,
  speed: 110,
  ai: 'guardian',
  xp: 90,
  biome: 'wilds'
}, {
  id: 'crawler',
  name: 'Crystal Crawler',
  glyph: 'c',
  color: '#7bcfe0',
  health: 14,
  attack: 7,
  defense: 13,
  speed: 95,
  ai: 'chase',
  xp: 23,
  biome: 'caverns'
}, {
  id: 'magma',
  name: 'Shore Newt',
  glyph: 'n',
  color: '#73b6c1',
  health: 12,
  attack: 8,
  defense: 11,
  speed: 105,
  ai: 'chase',
  xp: 25,
  biome: 'caverns',
  tags: ['caverns', 'water', 'mobility']
}, {
  id: 'echo',
  name: 'Echo Bat',
  glyph: 'e',
  color: '#ba9ddd',
  health: 9,
  attack: 7,
  defense: 12,
  speed: 140,
  ai: 'wander',
  xp: 26,
  biome: 'caverns',
  tags: ['caverns', 'darkness', 'echo']
}, {
  id: 'seer',
  name: 'Grotto Seer',
  glyph: 's',
  color: '#ba8ae7',
  health: 13,
  attack: 9,
  defense: 12,
  speed: 95,
  ai: 'ranged',
  xp: 30,
  biome: 'caverns',
  tags: ['caverns', 'darkness', 'telegraph']
}, {
  id: 'slug',
  name: 'Tide Slug',
  glyph: 'u',
  color: '#a8c5cf',
  health: 19,
  attack: 8,
  defense: 15,
  speed: 65,
  ai: 'chase',
  xp: 31,
  biome: 'caverns',
  tags: ['caverns', 'water', 'cover']
}, {
  id: 'cinderimp',
  name: 'Brine Skulker',
  glyph: 'i',
  color: '#8ed9de',
  health: 10,
  attack: 7,
  defense: 10,
  speed: 100,
  ai: 'ranged',
  xp: 28,
  biome: 'caverns',
  tags: ['caverns', 'water', 'telegraph']
}, {
  id: 'fumeeel',
  name: 'Tide Eel',
  glyph: 'u',
  color: '#8fc59a',
  health: 13,
  attack: 7,
  defense: 12,
  speed: 100,
  ai: 'chase',
  xp: 29,
  biome: 'caverns',
  tags: ['caverns', 'water', 'tide', 'mobility']
}, {
  id: 'gloomseer',
  name: 'Gloom Seer',
  glyph: 'G',
  color: '#7c6d9f',
  health: 12,
  attack: 8,
  defense: 13,
  speed: 95,
  ai: 'ranged',
  xp: 32,
  biome: 'caverns',
  tags: ['caverns', 'darkness', 'light', 'counterplay']
}, {
  id: 'crystalpuller',
  name: 'Undertow Puller',
  glyph: 'p',
  color: '#9ecce3',
  health: 14,
  attack: 7,
  defense: 13,
  speed: 90,
  ai: 'ranged',
  xp: 33,
  biome: 'caverns',
  tags: ['caverns', 'water', 'displacement', 'telegraph']
}, {
  id: 'geode',
  name: 'Tidemaw',
  glyph: 'G',
  color: '#8ce5f2',
  health: 62,
  attack: 12,
  defense: 16,
  speed: 100,
  ai: 'guardian',
  xp: 115,
  biome: 'caverns',
  tags: ['caverns', 'water', 'current']
}, {
  id: 'scarab',
  name: 'Ash Scarab',
  glyph: 's',
  color: '#d8b363',
  health: 16,
  attack: 8,
  defense: 15,
  speed: 95,
  ai: 'chase',
  xp: 32,
  biome: 'ruins'
}, {
  id: 'sentinel',
  name: 'Stone Sentinel',
  glyph: 'S',
  color: '#9da5aa',
  health: 23,
  attack: 10,
  defense: 17,
  speed: 75,
  ai: 'chase',
  xp: 40,
  biome: 'ruins'
}, {
  id: 'oracle',
  name: 'Dust Oracle',
  glyph: 'o',
  color: '#e9c489',
  health: 15,
  attack: 11,
  defense: 13,
  speed: 100,
  ai: 'ranged',
  xp: 45,
  biome: 'ruins'
}, {
  id: 'shade',
  name: 'Vault Shade',
  glyph: 'h',
  color: '#c1a5ed',
  health: 14,
  attack: 10,
  defense: 16,
  speed: 125,
  ai: 'wander',
  xp: 48,
  biome: 'ruins'
}, {
  id: 'cultist',
  name: 'Ash Cultist',
  glyph: 'c',
  color: '#df9a7c',
  health: 18,
  attack: 11,
  defense: 14,
  speed: 100,
  ai: 'ranged',
  xp: 51,
  biome: 'ruins'
}, {
  id: 'wardacolyte',
  name: 'Ward Acolyte',
  glyph: 'a',
  color: '#c29ce6',
  health: 15,
  attack: 8,
  defense: 13,
  speed: 90,
  ai: 'ranged',
  xp: 37,
  biome: 'ruins',
  tags: ['ruins', 'ward', 'ritual']
}, {
  id: 'dartadept',
  name: 'Dart Adept',
  glyph: 'd',
  color: '#d8b576',
  health: 13,
  attack: 9,
  defense: 12,
  speed: 100,
  ai: 'ranged',
  xp: 38,
  biome: 'ruins',
  tags: ['ruins', 'dart', 'telegraph']
}, {
  id: 'lockkeeper',
  name: 'Lock Keeper',
  glyph: 'k',
  color: '#c4b488',
  health: 18,
  attack: 9,
  defense: 15,
  speed: 80,
  ai: 'chase',
  xp: 42,
  biome: 'ruins',
  tags: ['ruins', 'lock', 'counterplay']
}, {
  id: 'ritualist',
  name: 'Ash Ritualist',
  glyph: 'r',
  color: '#d88ea4',
  health: 14,
  attack: 10,
  defense: 13,
  speed: 95,
  ai: 'ranged',
  xp: 44,
  biome: 'ruins',
  tags: ['ruins', 'ritual', 'telegraph']
}, {
  id: 'regent',
  name: 'The Stone Keeper',
  glyph: 'R',
  color: '#ffdb75',
  health: 84,
  attack: 15,
  defense: 19,
  speed: 110,
  ai: 'guardian',
  xp: 180,
  biome: 'ruins'
}, {
  id: 'cinderling',
  name: 'Cinderling',
  glyph: 'c',
  color: '#f08d5b',
  health: 18,
  attack: 12,
  defense: 16,
  speed: 110,
  ai: 'chase',
  xp: 48,
  biome: 'furnace',
  tags: ['furnace', 'fire', 'heat']
}, {
  id: 'smokeskulk',
  name: 'Smoke Skulk',
  glyph: 's',
  color: '#9ca1ad',
  health: 15,
  attack: 11,
  defense: 17,
  speed: 130,
  ai: 'wander',
  xp: 50,
  biome: 'furnace',
  tags: ['furnace', 'smoke', 'mobility']
}, {
  id: 'liftwarden',
  name: 'Lift Warden',
  glyph: 'l',
  color: '#e9c47e',
  health: 24,
  attack: 12,
  defense: 19,
  speed: 85,
  ai: 'chase',
  xp: 58,
  biome: 'furnace',
  tags: ['furnace', 'lift', 'counterplay']
}, {
  id: 'slagcaster',
  name: 'Slag Caster',
  glyph: 's',
  color: '#e26e4c',
  health: 17,
  attack: 13,
  defense: 16,
  speed: 95,
  ai: 'ranged',
  xp: 60,
  biome: 'furnace',
  tags: ['furnace', 'fire', 'telegraph']
}, {
  id: 'breakmaw',
  name: 'Break Maw',
  glyph: 'b',
  color: '#bd8567',
  health: 29,
  attack: 13,
  defense: 20,
  speed: 80,
  ai: 'chase',
  xp: 65,
  biome: 'furnace',
  tags: ['furnace', 'breakwall', 'force']
}, {
  id: 'ashoracle',
  name: 'Ash Oracle',
  glyph: 'a',
  color: '#e6b4a2',
  health: 18,
  attack: 14,
  defense: 17,
  speed: 95,
  ai: 'ranged',
  xp: 66,
  biome: 'furnace',
  tags: ['furnace', 'smoke', 'telegraph']
}, {
  id: 'kilnheart',
  name: 'The Kiln Heart',
  glyph: 'K',
  color: '#ffd070',
  health: 108,
  attack: 17,
  defense: 22,
  speed: 105,
  ai: 'guardian',
  xp: 240,
  biome: 'furnace',
  tags: ['furnace', 'fire', 'breakwall']
}, {
  id: 'tidewraith',
  name: 'Tide Wraith',
  glyph: 't',
  color: '#85d9df',
  health: 20,
  attack: 14,
  defense: 18,
  speed: 120,
  ai: 'wander',
  xp: 65,
  biome: 'floodedRuins',
  tags: ['floodedRuins', 'water', 'current']
}, {
  id: 'anchorcrab',
  name: 'Anchor Crab',
  glyph: 'a',
  color: '#83bdc6',
  health: 31,
  attack: 13,
  defense: 23,
  speed: 75,
  ai: 'chase',
  xp: 72,
  biome: 'floodedRuins',
  tags: ['floodedRuins', 'anchor', 'guard']
}, {
  id: 'siltseer',
  name: 'Silt Seer',
  glyph: 's',
  color: '#c5d8d4',
  health: 19,
  attack: 15,
  defense: 18,
  speed: 95,
  ai: 'ranged',
  xp: 74,
  biome: 'floodedRuins',
  tags: ['floodedRuins', 'current', 'telegraph']
}, {
  id: 'drownblade',
  name: 'Drownblade',
  glyph: 'd',
  color: '#71b7c3',
  health: 24,
  attack: 15,
  defense: 20,
  speed: 110,
  ai: 'chase',
  xp: 78,
  biome: 'floodedRuins',
  tags: ['floodedRuins', 'blade', 'water']
}, {
  id: 'coralguard',
  name: 'Coral Guard',
  glyph: 'c',
  color: '#d69b92',
  health: 33,
  attack: 14,
  defense: 24,
  speed: 70,
  ai: 'chase',
  xp: 82,
  biome: 'floodedRuins',
  tags: ['floodedRuins', 'anchor', 'counterplay']
}, {
  id: 'currentcaller',
  name: 'Current Caller',
  glyph: 'c',
  color: '#9ae5ea',
  health: 20,
  attack: 16,
  defense: 18,
  speed: 100,
  ai: 'ranged',
  xp: 84,
  biome: 'floodedRuins',
  tags: ['floodedRuins', 'current', 'displacement']
}, {
  id: 'drownedRegent',
  name: 'The Drowned Regent',
  glyph: 'D',
  color: '#b3edf0',
  health: 132,
  attack: 20,
  defense: 25,
  speed: 100,
  ai: 'guardian',
  xp: 320,
  biome: 'floodedRuins',
  tags: ['floodedRuins', 'water', 'anchor']
}, {
  id: 'cliffkite',
  name: 'Cliff Kite',
  glyph: 'k',
  color: '#b9dcf4',
  health: 18,
  attack: 10,
  defense: 15,
  speed: 135,
  ai: 'wander',
  xp: 55,
  biome: 'cliffs',
  tags: ['cliffs', 'wind', 'mobility']
}, {
  id: 'ropeRaider',
  name: 'Rope Raider',
  glyph: 'r',
  color: '#d8b66f',
  health: 23,
  attack: 12,
  defense: 17,
  speed: 110,
  ai: 'chase',
  xp: 62,
  biome: 'cliffs',
  tags: ['cliffs', 'climb', 'hook']
}, {
  id: 'galeSeer',
  name: 'Gale Seer',
  glyph: 'g',
  color: '#a8c7ff',
  health: 17,
  attack: 13,
  defense: 15,
  speed: 105,
  ai: 'ranged',
  xp: 67,
  biome: 'cliffs',
  tags: ['cliffs', 'wind', 'telegraph']
}, {
  id: 'ledgeStalker',
  name: 'Ledge Stalker',
  glyph: 'l',
  color: '#8398b4',
  health: 25,
  attack: 11,
  defense: 19,
  speed: 95,
  ai: 'chase',
  xp: 66,
  biome: 'cliffs',
  tags: ['cliffs', 'climb', 'counterplay']
}, {
  id: 'stormCrow',
  name: 'Storm Crow',
  glyph: 'c',
  color: '#778ec3',
  health: 15,
  attack: 14,
  defense: 14,
  speed: 145,
  ai: 'ranged',
  xp: 70,
  biome: 'cliffs',
  tags: ['cliffs', 'wind', 'force'],
  terrainAffinity: ['ledge']
}, {
  id: 'cragMoth',
  name: 'Crag Moth',
  glyph: 'm',
  color: '#ccd7eb',
  health: 16,
  attack: 12,
  defense: 15,
  speed: 140,
  ai: 'wander',
  xp: 58,
  biome: 'cliffs',
  tags: ['cliffs', 'wind', 'light']
}, {
  id: 'screeHound',
  name: 'Scree Hound',
  glyph: 'h',
  color: '#8b99aa',
  health: 27,
  attack: 13,
  defense: 18,
  speed: 110,
  ai: 'chase',
  xp: 72,
  biome: 'cliffs',
  tags: ['cliffs', 'force', 'counterplay']
}, {
  id: 'wireSinger',
  name: 'Wire Singer',
  glyph: 'w',
  color: '#adc9ed',
  health: 18,
  attack: 14,
  defense: 16,
  speed: 100,
  ai: 'ranged',
  xp: 74,
  biome: 'cliffs',
  tags: ['cliffs', 'wind', 'telegraph']
}, {
  id: 'skyWarden',
  name: 'Sky Warden',
  glyph: 'S',
  color: '#ecf5ff',
  health: 118,
  attack: 18,
  defense: 23,
  speed: 115,
  ai: 'guardian',
  xp: 280,
  biome: 'cliffs',
  tags: ['cliffs', 'wind', 'climb']
}, {
  id: 'graveMite',
  name: 'Grave Mite',
  glyph: 'm',
  color: '#a99aad',
  health: 20,
  attack: 11,
  defense: 17,
  speed: 105,
  ai: 'chase',
  xp: 60,
  biome: 'burial',
  tags: ['burial', 'grave']
}, {
  id: 'ossuaryGuard',
  name: 'Ossuary Guard',
  glyph: 'o',
  color: '#d9d3c5',
  health: 32,
  attack: 13,
  defense: 22,
  speed: 75,
  ai: 'chase',
  xp: 74,
  biome: 'burial',
  tags: ['burial', 'grave', 'guard']
}, {
  id: 'mourner',
  name: 'Mourner',
  glyph: 'm',
  color: '#c9a6db',
  health: 19,
  attack: 14,
  defense: 17,
  speed: 95,
  ai: 'ranged',
  xp: 76,
  biome: 'burial',
  tags: ['burial', 'spirit', 'telegraph']
}, {
  id: 'ancestorEcho',
  name: 'Ancestor Echo',
  glyph: 'e',
  color: '#cbbde7',
  health: 17,
  attack: 15,
  defense: 16,
  speed: 130,
  ai: 'wander',
  xp: 78,
  biome: 'burial',
  tags: ['burial', 'spirit', 'echo']
}, {
  id: 'tombWarden',
  name: 'Tomb Warden',
  glyph: 't',
  color: '#9d8b76',
  health: 29,
  attack: 14,
  defense: 21,
  speed: 85,
  ai: 'chase',
  xp: 80,
  biome: 'burial',
  tags: ['burial', 'grave', 'counterplay'],
  terrainAffinity: ['graveSoil']
}, {
  id: 'graveWisp',
  name: 'Grave Wisp',
  glyph: 'w',
  color: '#e0c9f0',
  health: 14,
  attack: 15,
  defense: 15,
  speed: 145,
  ai: 'wander',
  xp: 67,
  biome: 'burial',
  tags: ['burial', 'spirit', 'light'],
  terrainAffinity: ['spiritPath']
}, {
  id: 'barrowHound',
  name: 'Barrow Hound',
  glyph: 'h',
  color: '#8f796d',
  health: 28,
  attack: 15,
  defense: 19,
  speed: 105,
  ai: 'chase',
  xp: 82,
  biome: 'burial',
  tags: ['burial', 'grave', 'force']
}, {
  id: 'lamenter',
  name: 'Lamenter',
  glyph: 'l',
  color: '#d6b4e5',
  health: 20,
  attack: 16,
  defense: 17,
  speed: 95,
  ai: 'ranged',
  xp: 84,
  biome: 'burial',
  tags: ['burial', 'spirit', 'telegraph']
}, {
  id: 'barrowKing',
  name: 'The Barrow King',
  glyph: 'B',
  color: '#f0d5a0',
  health: 128,
  attack: 19,
  defense: 25,
  speed: 100,
  ai: 'guardian',
  xp: 310,
  biome: 'burial',
  tags: ['burial', 'grave', 'spirit']
}, {
  id: 'saltRaider',
  name: 'Salt Raider',
  glyph: 'r',
  color: '#f0d889',
  health: 23,
  attack: 15,
  defense: 18,
  speed: 135,
  ai: 'chase',
  xp: 76,
  biome: 'saltFlats',
  tags: ['saltFlats', 'salt', 'mobility']
}, {
  id: 'mirageSkirmisher',
  name: 'Mirage Skirmisher',
  glyph: 'm',
  color: '#c5e7ee',
  health: 18,
  attack: 16,
  defense: 16,
  speed: 125,
  ai: 'ranged',
  xp: 80,
  biome: 'saltFlats',
  tags: ['saltFlats', 'mirror', 'displacement', 'telegraph']
}, {
  id: 'brineStalker',
  name: 'Brine Stalker',
  glyph: 'b',
  color: '#76bac0',
  health: 26,
  attack: 14,
  defense: 20,
  speed: 105,
  ai: 'wander',
  xp: 78,
  biome: 'saltFlats',
  tags: ['saltFlats', 'brine', 'ambush']
}, {
  id: 'glassCutter',
  name: 'Glass Cutter',
  glyph: 'g',
  color: '#f4f1c9',
  health: 20,
  attack: 16,
  defense: 17,
  speed: 115,
  ai: 'ranged',
  xp: 83,
  biome: 'saltFlats',
  tags: ['saltFlats', 'mirror', 'dart', 'telegraph']
}, {
  id: 'saltSovereign',
  name: 'The Salt Sovereign',
  glyph: 'S',
  color: '#fff2b0',
  health: 138,
  attack: 21,
  defense: 26,
  speed: 110,
  ai: 'guardian',
  xp: 330,
  biome: 'saltFlats',
  tags: ['saltFlats', 'salt', 'mirror', 'guardian']
}, {
  id: 'rimeDuelist',
  name: 'Rime Duelist',
  glyph: 'd',
  color: '#b8dcf2',
  health: 29,
  attack: 17,
  defense: 22,
  speed: 105,
  ai: 'chase',
  xp: 88,
  biome: 'frostReliquary',
  tags: ['frostReliquary', 'frost', 'duel']
}, {
  id: 'iceSentinel',
  name: 'Ice Sentinel',
  glyph: 'i',
  color: '#d5f1ff',
  health: 34,
  attack: 15,
  defense: 25,
  speed: 75,
  ai: 'chase',
  xp: 90,
  biome: 'frostReliquary',
  tags: ['frostReliquary', 'ice', 'guard', 'duel']
}, {
  id: 'whiteoutOracle',
  name: 'Whiteout Oracle',
  glyph: 'o',
  color: '#e8f6ff',
  health: 21,
  attack: 17,
  defense: 18,
  speed: 100,
  ai: 'ranged',
  xp: 92,
  biome: 'frostReliquary',
  tags: ['frostReliquary', 'frost', 'telegraph', 'ritual']
}, {
  id: 'shardHound',
  name: 'Shard Hound',
  glyph: 'h',
  color: '#9dc7e2',
  health: 27,
  attack: 16,
  defense: 19,
  speed: 125,
  ai: 'chase',
  xp: 86,
  biome: 'frostReliquary',
  tags: ['frostReliquary', 'ice', 'mobility']
}, {
  id: 'reliquaryWarden',
  name: 'The Reliquary Warden',
  glyph: 'R',
  color: '#f0fbff',
  health: 146,
  attack: 22,
  defense: 28,
  speed: 95,
  ai: 'guardian',
  xp: 350,
  biome: 'frostReliquary',
  tags: ['frostReliquary', 'frost', 'ice', 'guardian', 'duel']
}];
MONSTERS.forEach(monster => {
  monster.name = `${colonyMonsterPrefix[monster.biome]} ${voyagerName(monster.name)}`;
});
export const MONSTER = Object.fromEntries(MONSTERS.map(monster => [monster.id, monster]));
export const monsterById = id => MONSTER[id];
export const isMonsterId = id => typeof id === 'string' && monsterById(id) !== undefined;
const tagged = (definition, ...tags) => tags.some(tag => {
  var _definition$tags;
  return (_definition$tags = definition.tags) === null || _definition$tags === void 0 ? void 0 : _definition$tags.includes(tag);
});
export const monsterRoleFor = definition => {
  var _definition$role;
  return (_definition$role = definition.role) !== null && _definition$role !== void 0 ? _definition$role : definition.ai === 'guardian' ? 'apex' : tagged(definition, 'ward') ? 'support' : tagged(definition, 'root', 'web', 'displacement', 'dart', 'ritual', 'lock') ? 'controller' : tagged(definition, 'ambush') ? 'ambusher' : tagged(definition, 'guard') ? 'guard' : definition.ai === 'ranged' ? 'artillery' : definition.ai === 'wander' ? 'scavenger' : definition.speed >= 120 ? 'skirmisher' : 'pursuer';
};
const terrainByTag = [['rail', ['rail']], ['water', ['water', 'current', 'deepWater']], ['web', ['web']], ['dart', ['dart']], ['ward', ['altar']], ['current', ['current', 'deepWater']], ['gas', ['gas', 'smoke']], ['smoke', ['smoke', 'gas']], ['fire', ['fireVent', 'lava']], ['heat', ['fireVent', 'lava']], ['climb', ['ledge']], ['grave', ['graveSoil', 'spiritPath']], ['spirit', ['spiritPath']], ['salt', ['saltMirror', 'brine']], ['mirror', ['saltMirror']], ['brine', ['brine']], ['frost', ['frostRime', 'ice']], ['ice', ['ice']], ['anchor', ['anchor']], ['lift', ['lift']]];
export const terrainAffinityFor = definition => definition.terrainAffinity ? [...definition.terrainAffinity] : Array.from(new Set(terrainByTag.filter(([tag]) => tagged(definition, tag)).flatMap(([, terrain]) => terrain)));
export const SKILLS = [...['Iron Grip', 'Cleave', 'Breaker', 'Counter', 'Unstoppable', 'Titan'].map((name, i) => ({
  id: `str${i + 1}`,
  name,
  stat: 'strength',
  level: i + 1,
  text: ['Strength +1, melee damage +1', 'Strength +1, melee damage +1', 'Strength +1, break rubble', 'Strength +1, guard 2 damage', 'Strength +1, melee knockback', 'Strength +1, melee damage +2'][i],
  tags: ['strength'],
  prerequisites: i ? [`str${i}`] : []
})), ...['Quick Step', 'Sure Aim', 'Skirmisher', 'Evasion', 'Fleet', 'Ghostwalk'].map((name, i) => ({
  id: `agi${i + 1}`,
  name,
  stat: 'agility',
  level: i + 1,
  text: ['Agility +1, move +1 floor tile', 'Agility +1, melee reach +1', 'Agility +1, evade telegraphs 20%', 'Agility +1, dodge +3', 'Agility +1, move +1 floor tile', 'Agility +1, evade telegraphs +35%'][i],
  tags: ['agility'],
  prerequisites: i ? [`agi${i}`] : []
})), ...['Hardy', 'Forager', 'Stalwart', 'Recovery', 'Ironblood', 'Last Stand'].map((name, i) => ({
  id: `vit${i + 1}`,
  name,
  stat: 'vitality',
  level: i + 1,
  text: ['Vitality +1, maximum health +2', 'Vitality +1, recovery +1', 'Vitality +1, shield 1 damage', 'Vitality +1, recovery +3', 'Vitality +1, hazards -2 damage', 'Vitality +1, rescue recovery +6'][i],
  tags: ['vitality'],
  prerequisites: i ? [`vit${i}`] : []
})), ...['Spark', 'Insight', 'Field Theorist', 'Star Reader', 'Signal Walker', 'Routefinder'].map((name, i) => ({
  id: `int${i + 1}`,
  name,
  stat: 'intellect',
  level: i + 1,
  text: ['Intellect +1, modules cost 1 less', 'Intellect +1, focus recovery +1', 'Intellect +1, module range +1', 'Intellect +1, shields absorb 2', 'Intellect +1, module range +1', 'Intellect +1, focus recovery +1, star routes'][i],
  tags: ['intellect'],
  prerequisites: i ? [`int${i}`] : []
}))];
const SKILL = Object.fromEntries(SKILLS.map(skill => [skill.id, skill]));
export const isSkillId = id => typeof id === 'string' && SKILL[id] !== undefined;
export const biomeForFloor = index => ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'][Math.floor(index / 4)];
export const biomeName = {
  mine: 'Kestrel Colony',
  wilds: 'Verdant Colony',
  caverns: 'Pelagos Colony',
  ruins: 'Orison Colony',
  furnace: 'Helion Colony',
  floodedRuins: 'Nerida Colony',
  cliffs: 'Aerie Colony',
  burial: 'Memorial Colony',
  saltFlats: 'Halcyon Colony',
  frostReliquary: 'Borealis Colony'
};
export const SHOP_STOCK = {
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
};
const idPattern = /^[a-z][a-zA-Z0-9]*$/;
const validateIds = (label, definitions) => {
  const ids = new Set();
  for (const definition of definitions) {
    if (!idPattern.test(definition.id)) throw new Error(`invalid ${label} id: ${definition.id}`);
    if (ids.has(definition.id)) throw new Error(`duplicate ${label} id: ${definition.id}`);
    ids.add(definition.id);
  }
};
const validateTags = (label, id, tags, knownTags) => {
  for (const tag of tags !== null && tags !== void 0 ? tags : []) if (!knownTags.has(tag)) throw new Error(`invalid ${label} tag on ${id}: ${tag}`);
};
const validatePrerequisites = skills => {
  const skillIds = new Set(skills.map(skill => skill.id));
  const visiting = new Set();
  const visited = new Set();
  const visit = skill => {
    if (visited.has(skill.id)) return;
    if (visiting.has(skill.id)) throw new Error(`cyclic skill prerequisite: ${skill.id}`);
    visiting.add(skill.id);
    for (const prerequisite of skill.prerequisites) {
      if (!skillIds.has(prerequisite)) throw new Error(`unknown skill prerequisite on ${skill.id}: ${prerequisite}`);
      if (prerequisite === skill.id) throw new Error(`self-referencing skill prerequisite: ${skill.id}`);
      visit(skills.find(candidate => candidate.id === prerequisite));
    }
    visiting.delete(skill.id);
    visited.add(skill.id);
  };
  for (const skill of skills) visit(skill);
};
const validateScripts = (scripts, items) => {
  const ids = new Set();
  const itemIds = new Set();
  const validShapes = ['adjacent', 'line', 'cone', 'burst', 'cross'];
  for (const script of scripts) {
    if (!script.id || ids.has(script.id) || itemIds.has(script.itemId)) throw new Error(`invalid script: ${script.id}`);
    ids.add(script.id);
    itemIds.add(script.itemId);
    const item = items.find(current => current.id === script.itemId);
    if (!item || item.use !== 'spell' || item.spell !== script.id) throw new Error(`invalid script item: ${script.itemId}`);
    if (!['ember', 'verdant', 'astral'].includes(script.school) || !script.tags.length || !Number.isInteger(script.focusCost) || script.focusCost < 1 || !validShapes.includes(script.shape) || !Number.isInteger(script.range) || script.range < 1 || new Set(script.upgrades).size !== script.upgrades.length || !script.upgrades.every(upgrade => ['focusCost', 'range', 'potency'].includes(upgrade))) throw new Error(`invalid script definition: ${script.id}`);
  }
  if (scripts.length !== items.filter(item => item.use === 'spell').length) throw new Error('missing script definition');
};
export const validateContent = registry => {
  validateIds('item', registry.items);
  validateIds('monster', registry.monsters);
  validateIds('skill', registry.skills);
  const tags = new Set(registry.tags);
  for (const item of registry.items) {
    validateItemPrice(item.value);
    validateTags('item', item.id, item.tags, tags);
    validateEquipmentEffects(item.effects, item.id);
    if (item.use === 'spell' && !item.spell) throw new Error(`spell item missing spell id: ${item.id}`);
    if (item.use !== 'spell' && item.spell) throw new Error(`non-spell item has spell id: ${item.id}`);
  }
  validateScripts(registry.scripts, registry.items);
  for (const monster of registry.monsters) validateTags('monster', monster.id, monster.tags, tags);
  for (const skill of registry.skills) validateTags('skill', skill.id, skill.tags, tags);
  const itemIds = new Set(registry.items.map(item => item.id));
  for (const [biome, stock] of Object.entries(registry.shopStock)) for (const id of stock) if (!itemIds.has(id)) throw new Error(`unknown shop item in ${biome}: ${id}`);
  validatePrerequisites(registry.skills);
};
export const CONTENT = {
  items: ITEMS,
  monsters: MONSTERS,
  skills: SKILLS,
  scripts: SCRIPTS,
  tags: CONTENT_TAGS,
  shopStock: SHOP_STOCK
};
validateContent(CONTENT);
export const shopStock = biome => SHOP_STOCK[biome];
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJ2YWxpZGF0ZUVxdWlwbWVudEVmZmVjdHMiLCJ2YWxpZGF0ZUl0ZW1QcmljZSIsIkNPTlRFTlRfVEFHUyIsIklURU1TIiwiaWQiLCJuYW1lIiwiZ2x5cGgiLCJjb2xvciIsInNsb3QiLCJ3ZWFwb24iLCJkYW1hZ2UiLCJyZWFjaCIsInNoYXBlIiwiY29vbGRvd24iLCJ0YWdzIiwidmFsdWUiLCJlZmZlY3RzIiwia2luZCIsImFjdGlvbklkIiwicmVxdWlyZXMiLCJhZGQiLCJ0aHJvd2FibGUiLCJkZWZlbnNlIiwidXNlIiwidHJpZ2dlciIsImZvY3VzIiwiZmluZGFibGUiLCJzcGVsbCIsInZveWFnZXJUZXJtcyIsInZveWFnZXJOYW1lIiwicmVkdWNlIiwiY3VycmVudCIsInNvdXJjZSIsInJlcGxhY2VtZW50IiwicmVwbGFjZUFsbCIsImZvckVhY2giLCJpdGVtIiwiSVRFTSIsIk9iamVjdCIsImZyb21FbnRyaWVzIiwibWFwIiwiaXRlbUJ5SWQiLCJpc0l0ZW1JZCIsInVuZGVmaW5lZCIsIlNDUklQVFMiLCJpdGVtSWQiLCJzY2hvb2wiLCJmb2N1c0Nvc3QiLCJyYW5nZSIsInVwZ3JhZGVzIiwiU0NSSVBUX0JZX0lURU0iLCJzY3JpcHQiLCJjb2xvbnlNb25zdGVyUHJlZml4IiwibWluZSIsIndpbGRzIiwiY2F2ZXJucyIsInJ1aW5zIiwiZnVybmFjZSIsImZsb29kZWRSdWlucyIsImNsaWZmcyIsImJ1cmlhbCIsInNhbHRGbGF0cyIsImZyb3N0UmVsaXF1YXJ5IiwiTU9OU1RFUlMiLCJoZWFsdGgiLCJhdHRhY2siLCJzcGVlZCIsImFpIiwieHAiLCJiaW9tZSIsInNwYXduIiwidGVycmFpbkFmZmluaXR5IiwibW9uc3RlciIsIk1PTlNURVIiLCJtb25zdGVyQnlJZCIsImlzTW9uc3RlcklkIiwidGFnZ2VkIiwiZGVmaW5pdGlvbiIsInNvbWUiLCJ0YWciLCJfZGVmaW5pdGlvbiR0YWdzIiwiaW5jbHVkZXMiLCJtb25zdGVyUm9sZUZvciIsIl9kZWZpbml0aW9uJHJvbGUiLCJyb2xlIiwidGVycmFpbkJ5VGFnIiwidGVycmFpbkFmZmluaXR5Rm9yIiwiQXJyYXkiLCJmcm9tIiwiU2V0IiwiZmlsdGVyIiwiZmxhdE1hcCIsInRlcnJhaW4iLCJTS0lMTFMiLCJpIiwic3RhdCIsImxldmVsIiwidGV4dCIsInByZXJlcXVpc2l0ZXMiLCJTS0lMTCIsInNraWxsIiwiaXNTa2lsbElkIiwiYmlvbWVGb3JGbG9vciIsImluZGV4IiwiTWF0aCIsImZsb29yIiwiYmlvbWVOYW1lIiwiU0hPUF9TVE9DSyIsImlkUGF0dGVybiIsInZhbGlkYXRlSWRzIiwibGFiZWwiLCJkZWZpbml0aW9ucyIsImlkcyIsInRlc3QiLCJFcnJvciIsImhhcyIsInZhbGlkYXRlVGFncyIsImtub3duVGFncyIsInZhbGlkYXRlUHJlcmVxdWlzaXRlcyIsInNraWxscyIsInNraWxsSWRzIiwidmlzaXRpbmciLCJ2aXNpdGVkIiwidmlzaXQiLCJwcmVyZXF1aXNpdGUiLCJmaW5kIiwiY2FuZGlkYXRlIiwiZGVsZXRlIiwidmFsaWRhdGVTY3JpcHRzIiwic2NyaXB0cyIsIml0ZW1zIiwiaXRlbUlkcyIsInZhbGlkU2hhcGVzIiwibGVuZ3RoIiwiTnVtYmVyIiwiaXNJbnRlZ2VyIiwic2l6ZSIsImV2ZXJ5IiwidXBncmFkZSIsInZhbGlkYXRlQ29udGVudCIsInJlZ2lzdHJ5IiwibW9uc3RlcnMiLCJzdG9jayIsImVudHJpZXMiLCJzaG9wU3RvY2siLCJDT05URU5UIl0sInNvdXJjZXMiOlsiY29udGVudC50cyJdLCJzb3VyY2VzQ29udGVudCI6WyJpbXBvcnQgdHlwZSB7IEJpb21lLCBFcXVpcG1lbnRTbG90LCBJdGVtSWQsIE1vbnN0ZXJSb2xlLCBTdGF0TmFtZSwgVGlsZUtpbmQgfSBmcm9tICcuL3R5cGVzJ1xuaW1wb3J0IHR5cGUgeyBBY3Rpb25TaGFwZSB9IGZyb20gJy4vZW5naW5lL2FjdGlvbnMnXG5pbXBvcnQgeyB2YWxpZGF0ZUVxdWlwbWVudEVmZmVjdHMsIHR5cGUgRXF1aXBtZW50RWZmZWN0IH0gZnJvbSAnLi9lZmZlY3RzJ1xuaW1wb3J0IHsgdmFsaWRhdGVJdGVtUHJpY2UgfSBmcm9tICcuL2VuZ2luZS9lY29ub215J1xuXG5leHBvcnQgaW50ZXJmYWNlIFdlYXBvblByb2ZpbGUgeyBkYW1hZ2U6IG51bWJlcjsgcmVhY2g6IG51bWJlcjsgc2hhcGU6IEFjdGlvblNoYXBlOyBjb29sZG93bjogbnVtYmVyOyB0YWdzOiBzdHJpbmdbXSB9XG5leHBvcnQgdHlwZSBTY3JpcHRTY2hvb2wgPSAnZW1iZXInIHwgJ3ZlcmRhbnQnIHwgJ2FzdHJhbCdcbmV4cG9ydCB0eXBlIFNjcmlwdFVwZ3JhZGVIb29rID0gJ2ZvY3VzQ29zdCcgfCAncmFuZ2UnIHwgJ3BvdGVuY3knXG5leHBvcnQgaW50ZXJmYWNlIFNjcmlwdERlZmluaXRpb24geyBpdGVtSWQ6IEl0ZW1JZDsgaWQ6IHN0cmluZzsgc2Nob29sOiBTY3JpcHRTY2hvb2w7IHRhZ3M6IHN0cmluZ1tdOyBmb2N1c0Nvc3Q6IG51bWJlcjsgc2hhcGU6IEFjdGlvblNoYXBlOyByYW5nZTogbnVtYmVyOyB1cGdyYWRlczogU2NyaXB0VXBncmFkZUhvb2tbXSB9XG5cbmV4cG9ydCBpbnRlcmZhY2UgSXRlbURlZmluaXRpb24ge1xuICBpZDogSXRlbUlkXG4gIG5hbWU6IHN0cmluZ1xuICBnbHlwaDogc3RyaW5nXG4gIGNvbG9yOiBzdHJpbmdcbiAgc2xvdD86IEVxdWlwbWVudFNsb3RcbiAgd2VhcG9uPzogV2VhcG9uUHJvZmlsZVxuICBkZWZlbnNlPzogbnVtYmVyXG4gIHZhbHVlOiBudW1iZXJcbiAgdXNlPzogJ2hlYWwnIHwgJ2ZvY3VzJyB8ICdtYXAnIHwgJ3RlbGVwb3J0JyB8ICdib21iJyB8ICdyb3BlJyB8ICdrZXknIHwgJ3RvcmNoJyB8ICdkcmlsbCcgfCAnZ2xpZGUnIHwgJ2dyYXBwbGUnIHwgJ2JyaWRnZScgfCAnZGFzaCcgfCAnd2luY2gnIHwgJ3NwZWxsJ1xuICBzcGVsbD86IHN0cmluZ1xuICB0aHJvd2FibGU/OiBib29sZWFuXG4gIGZpbmRhYmxlPzogYm9vbGVhblxuICB0YWdzPzogc3RyaW5nW11cbiAgZWZmZWN0cz86IHJlYWRvbmx5IEVxdWlwbWVudEVmZmVjdFtdXG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgTW9uc3RlckRlZmluaXRpb24geyBpZDogc3RyaW5nOyBuYW1lOiBzdHJpbmc7IGdseXBoOiBzdHJpbmc7IGNvbG9yOiBzdHJpbmc7IGhlYWx0aDogbnVtYmVyOyBhdHRhY2s6IG51bWJlcjsgZGVmZW5zZTogbnVtYmVyOyBzcGVlZDogbnVtYmVyOyBhaTogJ2NoYXNlJyB8ICdyYW5nZWQnIHwgJ3dhbmRlcicgfCAnZ3VhcmRpYW4nOyB4cDogbnVtYmVyOyBiaW9tZTogQmlvbWU7IHJvbGU/OiBNb25zdGVyUm9sZTsgdGFncz86IHN0cmluZ1tdOyB0ZXJyYWluQWZmaW5pdHk/OiBUaWxlS2luZFtdOyBzcGF3bj86ICdhbWJpZW50JyB8ICd0cmlnZ2VyZWQnIH1cbmV4cG9ydCBpbnRlcmZhY2UgU2tpbGxEZWZpbml0aW9uIHsgaWQ6IHN0cmluZzsgbmFtZTogc3RyaW5nOyBzdGF0OiBTdGF0TmFtZTsgbGV2ZWw6IG51bWJlcjsgdGV4dDogc3RyaW5nOyB0YWdzOiBzdHJpbmdbXTsgcHJlcmVxdWlzaXRlczogc3RyaW5nW10gfVxuZXhwb3J0IGludGVyZmFjZSBDb250ZW50UmVnaXN0cnkgeyBpdGVtczogcmVhZG9ubHkgSXRlbURlZmluaXRpb25bXTsgbW9uc3RlcnM6IHJlYWRvbmx5IE1vbnN0ZXJEZWZpbml0aW9uW107IHNraWxsczogcmVhZG9ubHkgU2tpbGxEZWZpbml0aW9uW107IHNjcmlwdHM6IHJlYWRvbmx5IFNjcmlwdERlZmluaXRpb25bXTsgdGFnczogcmVhZG9ubHkgc3RyaW5nW107IHNob3BTdG9jazogUmVhZG9ubHk8UmVjb3JkPEJpb21lLCByZWFkb25seSBJdGVtSWRbXT4+IH1cblxuZXhwb3J0IGNvbnN0IENPTlRFTlRfVEFHUyA9IFsnc3RyZW5ndGgnLCAnYWdpbGl0eScsICd2aXRhbGl0eScsICdpbnRlbGxlY3QnLCAnbWluZScsICd3aWxkcycsICdjYXZlcm5zJywgJ3J1aW5zJywgJ2Z1cm5hY2UnLCAnZmxvb2RlZFJ1aW5zJywgJ2NsaWZmcycsICdidXJpYWwnLCAnc2FsdEZsYXRzJywgJ2Zyb3N0UmVsaXF1YXJ5JywgJ3JhaWwnLCAndGVsZWdyYXBoJywgJ2NvdmVyJywgJ2V4cGxvc2l2ZScsICdyb290JywgJ3dhdGVyJywgJ3dlYicsICdtb2JpbGl0eScsICdzbmFyZScsICdmaXJlJywgJ2dhcycsICdsaWdodCcsICdkaXNwbGFjZW1lbnQnLCAnZGFya25lc3MnLCAnY291bnRlcnBsYXknLCAnd2FyZCcsICdkYXJ0JywgJ2xvY2snLCAncml0dWFsJywgJ2NvcmRtYXJrJywgJ3JlZWRzdGVwJywgJ3Ntb2tlJywgJ2xpZnQnLCAnYW5jaG9yJywgJ2N1cnJlbnQnLCAnYnJlYWt3YWxsJywgJ2Zsb3cnLCAnaGVhdCcsICdndWFyZCcsICdob29rJywgJ2JsYWRlJywgJ2hhbW1lcicsICd0aWRlJywgJ3NhbHZhZ2UnLCAnZm9yY2UnLCAnZ3JhcHBsZScsICdicmlkZ2UnLCAnZGFzaCcsICd3aW5jaCcsICd3aW5kJywgJ2NsaW1iJywgJ2dyYXZlJywgJ3NwaXJpdCcsICdlY2hvJywgJ2N1cnNlJywgJ3NhbHQnLCAnbWlycm9yJywgJ2JyaW5lJywgJ2Zyb3N0JywgJ2ljZScsICdkdWVsJywgJ2FtYnVzaCcsICdndWFyZGlhbiddIGFzIGNvbnN0XG5cbmV4cG9ydCBjb25zdCBJVEVNUzogSXRlbURlZmluaXRpb25bXSA9IFtcbiAgeyBpZDogJ3doaXAnLCBuYW1lOiAnQ291cmllciBDb3JkJywgZ2x5cGg6ICcvJywgY29sb3I6ICcjZTdjNjgwJywgc2xvdDogJ21haW5IYW5kJywgd2VhcG9uOiB7IGRhbWFnZTogNCwgcmVhY2g6IDIsIHNoYXBlOiAnbGluZScsIGNvb2xkb3duOiAwLCB0YWdzOiBbJ2ZsZXhpYmxlJywgJ3JlYWNoJ10gfSwgdmFsdWU6IDQ1LCBlZmZlY3RzOiBbeyBpZDogJ3N1cnZleWluZy1zdHJpa2UnLCBraW5kOiAnYWN0aW9uJywgYWN0aW9uSWQ6ICdwbGF5ZXItc3RyaWtlJywgcmVxdWlyZXM6IFsncmVhY2gnXSwgYWRkOiB7IGRhbWFnZTogMSB9IH1dIH0sXG4gIHsgaWQ6ICdtYWNoZXRlJywgbmFtZTogJ0JydXNoIEJsYWRlJywgZ2x5cGg6ICcvJywgY29sb3I6ICcjYjhkNmEwJywgc2xvdDogJ21haW5IYW5kJywgd2VhcG9uOiB7IGRhbWFnZTogNiwgcmVhY2g6IDEsIHNoYXBlOiAnYWRqYWNlbnQnLCBjb29sZG93bjogMSwgdGFnczogWydjbGVhdmUnLCAnd2lsZHMnXSB9LCB2YWx1ZTogNzUgfSxcbiAgeyBpZDogJ3BpY2theGUnLCBuYW1lOiAnT2JzaWRpYW4gQXhlJywgZ2x5cGg6ICdUJywgY29sb3I6ICcjYzdjNGJhJywgc2xvdDogJ21haW5IYW5kJywgd2VhcG9uOiB7IGRhbWFnZTogNywgcmVhY2g6IDEsIHNoYXBlOiAnY3Jvc3MnLCBjb29sZG93bjogMiwgdGFnczogWydydWJibGUnLCAncGllcmNpbmcnXSB9LCB2YWx1ZTogMTEwIH0sXG4gIHsgaWQ6ICdzcGVhcicsIG5hbWU6ICdDYXZlIFNwZWFyJywgZ2x5cGg6ICcvJywgY29sb3I6ICcjZDBhZTc4Jywgc2xvdDogJ21haW5IYW5kJywgd2VhcG9uOiB7IGRhbWFnZTogOCwgcmVhY2g6IDIsIHNoYXBlOiAnbGluZScsIGNvb2xkb3duOiAxLCB0YWdzOiBbJ3BpZXJjaW5nJywgJ3JlYWNoJ10gfSwgdmFsdWU6IDE0MCwgdGhyb3dhYmxlOiB0cnVlIH0sXG4gIHsgaWQ6ICd0aWRlU3BlYXInLCBuYW1lOiAnVGlkZSBTcGVhcicsIGdseXBoOiAnLycsIGNvbG9yOiAnIzc2ZDhkZicsIHNsb3Q6ICdtYWluSGFuZCcsIHdlYXBvbjogeyBkYW1hZ2U6IDcsIHJlYWNoOiAyLCBzaGFwZTogJ2xpbmUnLCBjb29sZG93bjogMSwgdGFnczogWyd3YXRlciddIH0sIHZhbHVlOiAxNDUgfSxcbiAgeyBpZDogJ3N1bmJsYWRlJywgbmFtZTogJ1N1bnN0b25lIEJsYWRlJywgZ2x5cGg6ICcvJywgY29sb3I6ICcjZmZlMTgxJywgc2xvdDogJ21haW5IYW5kJywgd2VhcG9uOiB7IGRhbWFnZTogMTEsIHJlYWNoOiAyLCBzaGFwZTogJ2NvbmUnLCBjb29sZG93bjogMiwgdGFnczogWydyYWRpYW50JywgJ2NsZWF2ZSddIH0sIHZhbHVlOiAyNjAgfSxcbiAgeyBpZDogJ2NpbmRlckhhbW1lcicsIG5hbWU6ICdDaW5kZXIgSGFtbWVyJywgZ2x5cGg6ICdUJywgY29sb3I6ICcjZjA4ZDViJywgc2xvdDogJ21haW5IYW5kJywgd2VhcG9uOiB7IGRhbWFnZTogMTAsIHJlYWNoOiAxLCBzaGFwZTogJ2Nyb3NzJywgY29vbGRvd246IDIsIHRhZ3M6IFsnaGFtbWVyJywgJ2ZpcmUnLCAnYnJlYWt3YWxsJ10gfSwgdmFsdWU6IDI1MCwgdGFnczogWydmdXJuYWNlJ10gfSxcbiAgeyBpZDogJ3Ntb2tlS25pZmUnLCBuYW1lOiAnU29vdCBLbmlmZScsIGdseXBoOiAnLycsIGNvbG9yOiAnIzljYTFhZCcsIHNsb3Q6ICdtYWluSGFuZCcsIHdlYXBvbjogeyBkYW1hZ2U6IDcsIHJlYWNoOiAxLCBzaGFwZTogJ2FkamFjZW50JywgY29vbGRvd246IDAsIHRhZ3M6IFsnYmxhZGUnLCAnc21va2UnLCAncXVpY2snXSB9LCB2YWx1ZTogMTgwLCB0YWdzOiBbJ2Z1cm5hY2UnXSB9LFxuICB7IGlkOiAnbGlmdEhvb2snLCBuYW1lOiAnTGlmdCBIb29rJywgZ2x5cGg6ICdKJywgY29sb3I6ICcjZTljNDdlJywgc2xvdDogJ21haW5IYW5kJywgd2VhcG9uOiB7IGRhbWFnZTogOCwgcmVhY2g6IDIsIHNoYXBlOiAnbGluZScsIGNvb2xkb3duOiAxLCB0YWdzOiBbJ2hvb2snLCAnbGlmdCcsICdyZWFjaCddIH0sIHZhbHVlOiAyMTAsIHRhZ3M6IFsnZnVybmFjZSddIH0sXG4gIHsgaWQ6ICdhbmNob3JCbGFkZScsIG5hbWU6ICdBbmNob3IgQmxhZGUnLCBnbHlwaDogJy8nLCBjb2xvcjogJyM3OGM0Y2UnLCBzbG90OiAnbWFpbkhhbmQnLCB3ZWFwb246IHsgZGFtYWdlOiA5LCByZWFjaDogMSwgc2hhcGU6ICdjb25lJywgY29vbGRvd246IDEsIHRhZ3M6IFsnYmxhZGUnLCAnYW5jaG9yJywgJ3dhdGVyJ10gfSwgdmFsdWU6IDI0MCwgdGFnczogWydmbG9vZGVkUnVpbnMnXSB9LFxuICB7IGlkOiAndGlkZUN1dHRlcicsIG5hbWU6ICdUaWRlIEN1dHRlcicsIGdseXBoOiAnLycsIGNvbG9yOiAnIzg5ZDhkZicsIHNsb3Q6ICdtYWluSGFuZCcsIHdlYXBvbjogeyBkYW1hZ2U6IDgsIHJlYWNoOiAyLCBzaGFwZTogJ2xpbmUnLCBjb29sZG93bjogMSwgdGFnczogWydibGFkZScsICdjdXJyZW50JywgJ3RpZGUnXSB9LCB2YWx1ZTogMjMwLCB0YWdzOiBbJ2Zsb29kZWRSdWlucyddIH0sXG4gIHsgaWQ6ICdidWNrbGVyJywgbmFtZTogJ1dvdmVuIEd1YXJkJywgZ2x5cGg6ICcpJywgY29sb3I6ICcjYmJjNmNjJywgc2xvdDogJ29mZkhhbmQnLCBkZWZlbnNlOiAyLCB2YWx1ZTogODAsIGVmZmVjdHM6IFt7IGlkOiAnZ3VhcmRlZCcsIGtpbmQ6ICdwYXNzaXZlJywgYWRkOiB7IGRlZmVuc2U6IDEgfSB9XSB9LFxuICB7IGlkOiAnYmVsbG93c1NoaWVsZCcsIG5hbWU6ICdCZWxsb3dzIFNoaWVsZCcsIGdseXBoOiAnKScsIGNvbG9yOiAnI2Q5ODc1ZCcsIHNsb3Q6ICdvZmZIYW5kJywgZGVmZW5zZTogMywgdmFsdWU6IDE5MCwgdGFnczogWydmdXJuYWNlJywgJ2ZpcmUnLCAnZ3VhcmQnXSB9LFxuICB7IGlkOiAnY2hhaW5HdWFyZCcsIG5hbWU6ICdDaGFpbiBHdWFyZCcsIGdseXBoOiAnKScsIGNvbG9yOiAnI2JkYzRjYScsIHNsb3Q6ICdvZmZIYW5kJywgZGVmZW5zZTogMiwgdmFsdWU6IDE3NSwgdGFnczogWydmdXJuYWNlJywgJ2hvb2snLCAnbGlmdCddIH0sXG4gIHsgaWQ6ICdzbW9rZU1hc2snLCBuYW1lOiAnU29vdCBNYXNrJywgZ2x5cGg6ICdbJywgY29sb3I6ICcjYTNhOGIzJywgc2xvdDogJ29mZkhhbmQnLCBkZWZlbnNlOiAxLCB2YWx1ZTogMTY1LCB0YWdzOiBbJ2Z1cm5hY2UnLCAnc21va2UnXSB9LFxuICB7IGlkOiAnYW5jaG9yQnVja2xlcicsIG5hbWU6ICdBbmNob3IgQnVja2xlcicsIGdseXBoOiAnKScsIGNvbG9yOiAnIzc1YmJjNycsIHNsb3Q6ICdvZmZIYW5kJywgZGVmZW5zZTogMywgdmFsdWU6IDIxMCwgdGFnczogWydmbG9vZGVkUnVpbnMnLCAnYW5jaG9yJywgJ2d1YXJkJ10gfSxcbiAgeyBpZDogJ2N1cnJlbnRPcmInLCBuYW1lOiAnQ3VycmVudCBPcmInLCBnbHlwaDogJ28nLCBjb2xvcjogJyM5YmU1ZTknLCBzbG90OiAnb2ZmSGFuZCcsIGRlZmVuc2U6IDEsIHZhbHVlOiAxODUsIHRhZ3M6IFsnZmxvb2RlZFJ1aW5zJywgJ2N1cnJlbnQnLCAnZmxvdyddIH0sXG4gIHsgaWQ6ICdsYW50ZXJuJywgbmFtZTogJ1Jlc2luIExhbXAnLCBnbHlwaDogJ2knLCBjb2xvcjogJyNmZmUxOGEnLCBzbG90OiAnb2ZmSGFuZCcsIGRlZmVuc2U6IDEsIHZhbHVlOiA5NSwgdXNlOiAndG9yY2gnIH0sXG4gIHsgaWQ6ICdjYXAnLCBuYW1lOiAnQmFyayBDYXAnLCBnbHlwaDogJ1snLCBjb2xvcjogJyNkM2IwNWMnLCBzbG90OiAnaGVhZCcsIGRlZmVuc2U6IDEsIHZhbHVlOiA1NSB9LFxuICB7IGlkOiAnbWFzaycsIG5hbWU6ICdNb3NzIE1hc2snLCBnbHlwaDogJ1snLCBjb2xvcjogJyM3MWE2NmQnLCBzbG90OiAnaGVhZCcsIGRlZmVuc2U6IDIsIHZhbHVlOiAxMjAgfSxcbiAgeyBpZDogJ2NvYXQnLCBuYW1lOiAnQmFyay1maWJlciBDb2F0JywgZ2x5cGg6ICdbJywgY29sb3I6ICcjYWQ4MDU2Jywgc2xvdDogJ2JvZHknLCBkZWZlbnNlOiAyLCB2YWx1ZTogMTAwIH0sXG4gIHsgaWQ6ICdtYWlsJywgbmFtZTogJ1N0b25lIEJlYWQgQ29hdCcsIGdseXBoOiAnWycsIGNvbG9yOiAnIzhiYjdkMScsIHNsb3Q6ICdib2R5JywgZGVmZW5zZTogNCwgdmFsdWU6IDIzMCB9LFxuICB7IGlkOiAnYm9vdHMnLCBuYW1lOiAnVHJhaWwgQm9vdHMnLCBnbHlwaDogJzsnLCBjb2xvcjogJyNjMjhiNWQnLCBzbG90OiAnYm9vdHMnLCBkZWZlbnNlOiAxLCB2YWx1ZTogNzAgfSxcbiAgeyBpZDogJ2ZlYXRoZXJib290cycsIG5hbWU6ICdGZWF0aGVyIEJvb3RzJywgZ2x5cGg6ICc7JywgY29sb3I6ICcjZTdlOWYwJywgc2xvdDogJ2Jvb3RzJywgZGVmZW5zZTogMiwgdmFsdWU6IDE4MCB9LFxuICB7IGlkOiAnd2FyZCcsIG5hbWU6ICdTcGlyaXQgQ2hhcm0nLCBnbHlwaDogJ28nLCBjb2xvcjogJyNjYTlmZTQnLCBzbG90OiAnY2hhcm0nLCBkZWZlbnNlOiAyLCB2YWx1ZTogMTYwLCBlZmZlY3RzOiBbeyBpZDogJ2FyY2FuZS1yZXR1cm4nLCBraW5kOiAndHJpZ2dlcmVkJywgdHJpZ2dlcjogJ3NwZWxsJywgcmVxdWlyZXM6IFsnYXJjYW5lJ10sIGFkZDogeyBmb2N1czogMSB9IH1dIH0sXG4gIHsgaWQ6ICdzdW5zZWFsJywgbmFtZTogJ1N1bnN0b25lIFNlYWwnLCBnbHlwaDogJ28nLCBjb2xvcjogJyNmZmUxODEnLCBzbG90OiAnY2hhcm0nLCBkZWZlbnNlOiAzLCB2YWx1ZTogMjgwIH0sXG4gIHsgaWQ6ICdjb3JkbWFya1RhbGlzbWFuJywgbmFtZTogJ0NvcmRtYXJrIFRhbGlzbWFuJywgZ2x5cGg6ICdvJywgY29sb3I6ICcjZTQ4ODVkJywgc2xvdDogJ2NoYXJtJywgZGVmZW5zZTogMSwgdmFsdWU6IDE3NSwgZmluZGFibGU6IGZhbHNlLCB0YWdzOiBbJ3JpdHVhbCcsICdjb3JkbWFyayddIH0sXG4gIHsgaWQ6ICdyZWVkc3RlcEJvb3RzJywgbmFtZTogJ1JlZWRzdGVwIEJvb3RzJywgZ2x5cGg6ICc7JywgY29sb3I6ICcjYzdhZDcwJywgc2xvdDogJ2Jvb3RzJywgZGVmZW5zZTogMiwgdmFsdWU6IDE1NSwgZmluZGFibGU6IGZhbHNlLCB0YWdzOiBbJ21vYmlsaXR5JywgJ3JlZWRzdGVwJ10gfSxcbiAgeyBpZDogJ3RvbmljJywgbmFtZTogJ1ZpdGFsIFRvbmljJywgZ2x5cGg6ICchJywgY29sb3I6ICcjZWI2NTcxJywgdmFsdWU6IDM1LCB1c2U6ICdoZWFsJywgdGhyb3dhYmxlOiB0cnVlIH0sXG4gIHsgaWQ6ICdmb2N1c1RvbmljJywgbmFtZTogJ0ZvY3VzIFRvbmljJywgZ2x5cGg6ICchJywgY29sb3I6ICcjN2ZhOGU4JywgdmFsdWU6IDUwLCB1c2U6ICdmb2N1cycsIHRocm93YWJsZTogdHJ1ZSB9LFxuICB7IGlkOiAnbWFwU2Nyb2xsJywgbmFtZTogJ1RyYWlsIE1hcCcsIGdseXBoOiAnPycsIGNvbG9yOiAnI2U2ZDJhNicsIHZhbHVlOiA2NSwgdXNlOiAnbWFwJyB9LFxuICB7IGlkOiAnYmxpbmtSdW5lJywgbmFtZTogJ1N3aWZ0LWZvb3QgQ2hhcm0nLCBnbHlwaDogJz8nLCBjb2xvcjogJyNiZGE4ZWInLCB2YWx1ZTogOTAsIHVzZTogJ3RlbGVwb3J0JyB9LFxuICB7IGlkOiAnYm9tYlBhY2snLCBuYW1lOiAnRmlyZS1hc2ggQnVuZGxlJywgZ2x5cGg6ICcqJywgY29sb3I6ICcjZWE4ZTY0JywgdmFsdWU6IDgwLCB1c2U6ICdib21iJyB9LFxuICB7IGlkOiAncm9wZUJ1bmRsZScsIG5hbWU6ICdSb3BlIEJ1bmRsZScsIGdseXBoOiAnficsIGNvbG9yOiAnI2RhYjI3MicsIHZhbHVlOiA1NSwgdXNlOiAncm9wZScgfSxcbiAgeyBpZDogJ2F1Z2VyJywgbmFtZTogJ09ic2lkaWFuIEF1Z2VyJywgZ2x5cGg6ICclJywgY29sb3I6ICcjYzdjNGJhJywgdmFsdWU6IDEwMCwgdXNlOiAnZHJpbGwnLCBmaW5kYWJsZTogZmFsc2UsIHRhZ3M6IFsnbWluZScsICdtb2JpbGl0eSddIH0sXG4gIHsgaWQ6ICdyZWVkR2xpZGVyJywgbmFtZTogJ1JlZWQgR2xpZGVyJywgZ2x5cGg6ICdeJywgY29sb3I6ICcjZDhiYzgyJywgdmFsdWU6IDk1LCB1c2U6ICdnbGlkZScsIGZpbmRhYmxlOiBmYWxzZSwgdGFnczogWyd3aWxkcycsICdtb2JpbGl0eSddIH0sXG4gIHsgaWQ6ICdncmFwcGxlTGluZScsIG5hbWU6ICdHcmFwcGxpbmcgTGluZScsIGdseXBoOiAn4oyBJywgY29sb3I6ICcjZDhiNjZmJywgdmFsdWU6IDEwNSwgdXNlOiAnZ3JhcHBsZScsIGZpbmRhYmxlOiBmYWxzZSwgdGFnczogWydtb2JpbGl0eScsICdncmFwcGxlJywgJ2hvb2snXSB9LFxuICB7IGlkOiAnYnJpZGdlS2l0JywgbmFtZTogJ0RlcGxveWFibGUgQnJpZGdlJywgZ2x5cGg6ICc9JywgY29sb3I6ICcjY2FhNTZkJywgdmFsdWU6IDg1LCB1c2U6ICdicmlkZ2UnLCBmaW5kYWJsZTogZmFsc2UsIHRhZ3M6IFsnbW9iaWxpdHknLCAnYnJpZGdlJywgJ3dhdGVyJ10gfSxcbiAgeyBpZDogJ3N0ZWFtSmV0cGFjaycsIG5hbWU6ICdTdGVhbSBKZXRwYWNrJywgZ2x5cGg6ICfihpEnLCBjb2xvcjogJyNlZTkzNjQnLCB2YWx1ZTogMTI1LCB1c2U6ICdkYXNoJywgZmluZGFibGU6IGZhbHNlLCB0YWdzOiBbJ21vYmlsaXR5JywgJ2Rhc2gnLCAnc21va2UnLCAnaGVhdCddIH0sXG4gIHsgaWQ6ICdwb3J0YWJsZVdpbmNoJywgbmFtZTogJ1BvcnRhYmxlIFdpbmNoJywgZ2x5cGg6ICdXJywgY29sb3I6ICcjYzZjOGNjJywgdmFsdWU6IDExNSwgdXNlOiAnd2luY2gnLCBmaW5kYWJsZTogZmFsc2UsIHRhZ3M6IFsnbW9iaWxpdHknLCAnd2luY2gnLCAnZm9yY2UnXSB9LFxuICB7IGlkOiAna2V5JywgbmFtZTogJ0NhcnZlZCBLZXknLCBnbHlwaDogJz8nLCBjb2xvcjogJyNkN2MyNjgnLCB2YWx1ZTogNDAsIHVzZTogJ2tleScgfSxcbiAgeyBpZDogJ3JvY2snLCBuYW1lOiAnVGhyb3dpbmcgU3RvbmUnLCBnbHlwaDogJyonLCBjb2xvcjogJyM5ZGE1YTknLCB2YWx1ZTogNSwgdGhyb3dhYmxlOiB0cnVlIH0sXG4gIHsgaWQ6ICdmaXJlSmFyJywgbmFtZTogJ0ZpcmUgSmFyJywgZ2x5cGg6ICchJywgY29sb3I6ICcjZmY4NzRmJywgdmFsdWU6IDk1LCB0aHJvd2FibGU6IHRydWUgfSxcbiAgeyBpZDogJ2NpbmRlclRvbmljJywgbmFtZTogJ0NpbmRlciBUb25pYycsIGdseXBoOiAnIScsIGNvbG9yOiAnI2VmNzk1YScsIHZhbHVlOiA2NSwgdXNlOiAnaGVhbCcsIHRhZ3M6IFsnZnVybmFjZScsICdoZWF0J10gfSxcbiAgeyBpZDogJ3Nvb3RGaWx0ZXInLCBuYW1lOiAnU29vdCBGaWx0ZXInLCBnbHlwaDogJyEnLCBjb2xvcjogJyNhM2E4YjMnLCB2YWx1ZTogNTUsIHVzZTogJ2ZvY3VzJywgdGFnczogWydmdXJuYWNlJywgJ3Ntb2tlJ10gfSxcbiAgeyBpZDogJ2JyZWFjaENoYXJnZScsIG5hbWU6ICdCcmVhY2ggQ2hhcmdlJywgZ2x5cGg6ICcqJywgY29sb3I6ICcjZjA5YzYzJywgdmFsdWU6IDEwMCwgdXNlOiAnYm9tYicsIHRhZ3M6IFsnZnVybmFjZScsICdicmVha3dhbGwnXSB9LFxuICB7IGlkOiAnbGlmdEtleScsIG5hbWU6ICdMaWZ0IEtleScsIGdseXBoOiAnPycsIGNvbG9yOiAnI2U5YzQ3ZScsIHZhbHVlOiA3NSwgdXNlOiAndGVsZXBvcnQnLCB0YWdzOiBbJ2Z1cm5hY2UnLCAnbGlmdCddIH0sXG4gIHsgaWQ6ICdhbmNob3JTcG9vbCcsIG5hbWU6ICdBbmNob3IgU3Bvb2wnLCBnbHlwaDogJ34nLCBjb2xvcjogJyM3NGMxY2MnLCB2YWx1ZTogODAsIHVzZTogJ3JvcGUnLCB0YWdzOiBbJ2Zsb29kZWRSdWlucycsICdhbmNob3InXSB9LFxuICB7IGlkOiAnYm9yZUdlbCcsIG5hbWU6ICdCb3JlIEdlbCcsIGdseXBoOiAnJScsIGNvbG9yOiAnI2Q2YWU3OCcsIHZhbHVlOiAxMTAsIHVzZTogJ2RyaWxsJywgdGFnczogWydmdXJuYWNlJywgJ2JyZWFrd2FsbCddIH0sXG4gIHsgaWQ6ICd3aW5nZm9pbCcsIG5hbWU6ICdXaW5nZm9pbCcsIGdseXBoOiAnXicsIGNvbG9yOiAnI2IyZGRlMCcsIHZhbHVlOiAxMDUsIHVzZTogJ2dsaWRlJywgdGFnczogWydmbG9vZGVkUnVpbnMnLCAnZmxvdyddIH0sXG4gIHsgaWQ6ICdmbG9vZFNhbHQnLCBuYW1lOiAnRmxvb2QgU2FsdCcsIGdseXBoOiAnIScsIGNvbG9yOiAnIzhlZDhkZicsIHZhbHVlOiA2MCwgdXNlOiAnaGVhbCcsIHRhZ3M6IFsnZmxvb2RlZFJ1aW5zJywgJ3dhdGVyJ10gfSxcbiAgeyBpZDogJ2N1cnJlbnRSdW5lJywgbmFtZTogJ0N1cnJlbnQgUnVuZScsIGdseXBoOiAnPycsIGNvbG9yOiAnI2ExZTVlYicsIHZhbHVlOiA5NSwgdXNlOiAndGVsZXBvcnQnLCB0YWdzOiBbJ2Zsb29kZWRSdWlucycsICdjdXJyZW50J10gfSxcbiAgeyBpZDogJ2ZpcmVjcmFja2VyJywgbmFtZTogJ0ZpcmVjcmFja2VyJywgZ2x5cGg6ICcqJywgY29sb3I6ICcjZjFiNTY4JywgdmFsdWU6IDcwLCB1c2U6ICdib21iJywgdGFnczogWydmdXJuYWNlJywgJ2ZpcmUnXSB9LFxuICB7IGlkOiAnc2FsdmFnZUtpdCcsIG5hbWU6ICdTYWx2YWdlIEtpdCcsIGdseXBoOiAnIScsIGNvbG9yOiAnI2M2ZDZkOCcsIHZhbHVlOiA2MCwgdXNlOiAnZm9jdXMnLCB0YWdzOiBbJ2Zsb29kZWRSdWlucycsICdzYWx2YWdlJ10gfSxcbiAgeyBpZDogJ2VtYmVyJywgbmFtZTogJ0VtYmVyIENoYXJtJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjZmY5YzYzJywgdmFsdWU6IDEyMCwgdXNlOiAnc3BlbGwnLCBzcGVsbDogJ2VtYmVyJyB9LFxuICB7IGlkOiAnbWVuZCcsIG5hbWU6ICdNZW5kaW5nIENoYXJtJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjOTFlMGIxJywgdmFsdWU6IDExMCwgdXNlOiAnc3BlbGwnLCBzcGVsbDogJ21lbmQnIH0sXG4gIHsgaWQ6ICdzaWdodCcsIG5hbWU6ICdTaWdodCBDaGFybScsIGdseXBoOiAnPycsIGNvbG9yOiAnIzlkZDdlNCcsIHZhbHVlOiAxMDUsIHVzZTogJ3NwZWxsJywgc3BlbGw6ICdzaWdodCcgfSxcbiAgeyBpZDogJ3Jvb3QnLCBuYW1lOiAnUm9vdCBDaGFybScsIGdseXBoOiAnPycsIGNvbG9yOiAnIzZkYWQ2MicsIHZhbHVlOiAxMTUsIHVzZTogJ3NwZWxsJywgc3BlbGw6ICdyb290JyB9LFxuICB7IGlkOiAnd2F0ZXJTY3JpcHQnLCBuYW1lOiAnVGlkZSBDaGFybScsIGdseXBoOiAnPycsIGNvbG9yOiAnIzdiY2ZlMCcsIHZhbHVlOiAxMjAsIHVzZTogJ3NwZWxsJywgc3BlbGw6ICd3YXRlcicgfSxcbiAgeyBpZDogJ2x1bGwnLCBuYW1lOiAnTHVsbCBDaGFybScsIGdseXBoOiAnPycsIGNvbG9yOiAnI2I2ZGY4YScsIHZhbHVlOiAxMzUsIHVzZTogJ3NwZWxsJywgc3BlbGw6ICdsdWxsJyB9LFxuICB7IGlkOiAnYmxpbmsnLCBuYW1lOiAnQmxpbmsgQ2hhcm0nLCBnbHlwaDogJz8nLCBjb2xvcjogJyNiZGE4ZWInLCB2YWx1ZTogMTM1LCB1c2U6ICdzcGVsbCcsIHNwZWxsOiAnYmxpbmsnIH0sXG4gIHsgaWQ6ICdwdWxsJywgbmFtZTogJ1B1bGwgQ2hhcm0nLCBnbHlwaDogJz8nLCBjb2xvcjogJyNkMmIxZWQnLCB2YWx1ZTogMTI1LCB1c2U6ICdzcGVsbCcsIHNwZWxsOiAncHVsbCcgfSxcbiAgeyBpZDogJ2d1c3QnLCBuYW1lOiAnR3VzdCBDaGFybScsIGdseXBoOiAnPycsIGNvbG9yOiAnI2MxYjhmNCcsIHZhbHVlOiAxMTUsIHVzZTogJ3NwZWxsJywgc3BlbGw6ICdndXN0JyB9LFxuICB7IGlkOiAnd2FyZFNjcmlwdCcsIG5hbWU6ICdXYXJkIENoYXJtJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjZWNiN2UzJywgdmFsdWU6IDEzMCwgdXNlOiAnc3BlbGwnLCBzcGVsbDogJ3dhcmQnIH0sXG4gIHsgaWQ6ICdnYXRlJywgbmFtZTogJ0dhdGUgQ2hhcm0nLCBnbHlwaDogJz8nLCBjb2xvcjogJyNmMWRiNzgnLCB2YWx1ZTogMTYwLCB1c2U6ICdzcGVsbCcsIHNwZWxsOiAnZ2F0ZScgfSxcbiAgeyBpZDogJ3dpbmRob29rJywgbmFtZTogJ1dpbmRob29rJywgZ2x5cGg6ICdKJywgY29sb3I6ICcjYjlkY2Y0Jywgc2xvdDogJ21haW5IYW5kJywgd2VhcG9uOiB7IGRhbWFnZTogOSwgcmVhY2g6IDIsIHNoYXBlOiAnbGluZScsIGNvb2xkb3duOiAxLCB0YWdzOiBbJ2hvb2snLCAnd2luZCcsICdjbGltYiddIH0sIHZhbHVlOiAyMjAsIHRhZ3M6IFsnY2xpZmZzJywgJ3dpbmQnLCAnY2xpbWInXSB9LFxuICB7IGlkOiAnZ2FsZU1hbnRsZScsIG5hbWU6ICdHYWxlIE1hbnRsZScsIGdseXBoOiAnWycsIGNvbG9yOiAnI2Q4ZWRmOScsIHNsb3Q6ICdib2R5JywgZGVmZW5zZTogMiwgdmFsdWU6IDIwMCwgdGFnczogWydjbGlmZnMnLCAnd2luZCcsICdtb2JpbGl0eSddIH0sXG4gIHsgaWQ6ICdjbGlmZlNwb29sJywgbmFtZTogJ0NsaWZmIFNwb29sJywgZ2x5cGg6ICd+JywgY29sb3I6ICcjZDhiNjZmJywgdmFsdWU6IDg1LCB1c2U6ICdyb3BlJywgdGFnczogWydjbGlmZnMnLCAnY2xpbWInXSB9LFxuICB7IGlkOiAndGh1bmRlckphcicsIG5hbWU6ICdUaHVuZGVyIEphcicsIGdseXBoOiAnIScsIGNvbG9yOiAnI2E4YzdmZicsIHZhbHVlOiAxMDAsIHRocm93YWJsZTogdHJ1ZSwgdGFnczogWydjbGlmZnMnLCAnd2luZCcsICdmb3JjZSddIH0sXG4gIHsgaWQ6ICdza3lNYXAnLCBuYW1lOiAnU2t5IE1hcCcsIGdseXBoOiAnPycsIGNvbG9yOiAnI2M0ZTVmMicsIHZhbHVlOiA3NSwgdXNlOiAnbWFwJywgdGFnczogWydjbGlmZnMnLCAnd2luZCddIH0sXG4gIHsgaWQ6ICdncmF2ZVNpY2tsZScsIG5hbWU6ICdHcmF2ZSBTaWNrbGUnLCBnbHlwaDogJy8nLCBjb2xvcjogJyNjNmI3ZDQnLCBzbG90OiAnbWFpbkhhbmQnLCB3ZWFwb246IHsgZGFtYWdlOiA5LCByZWFjaDogMSwgc2hhcGU6ICdjb25lJywgY29vbGRvd246IDEsIHRhZ3M6IFsnYmxhZGUnLCAnZ3JhdmUnLCAnc3Bpcml0J10gfSwgdmFsdWU6IDIyMCwgdGFnczogWydidXJpYWwnLCAnZ3JhdmUnLCAnc3Bpcml0J10gfSxcbiAgeyBpZDogJ21vdXJuaW5nQmVsbCcsIG5hbWU6ICdNb3VybmluZyBCZWxsJywgZ2x5cGg6ICdvJywgY29sb3I6ICcjZDNiY2U3Jywgc2xvdDogJ2NoYXJtJywgZGVmZW5zZTogMiwgdmFsdWU6IDIwNSwgdGFnczogWydidXJpYWwnLCAnc3Bpcml0JywgJ2VjaG8nXSB9LFxuICB7IGlkOiAnZ3JhdmVTYWx0JywgbmFtZTogJ0dyYXZlIFNhbHQnLCBnbHlwaDogJyEnLCBjb2xvcjogJyNkOWQzYzUnLCB2YWx1ZTogNjUsIHVzZTogJ2hlYWwnLCB0YWdzOiBbJ2J1cmlhbCcsICdncmF2ZSddIH0sXG4gIHsgaWQ6ICdhbmNlc3RvclRva2VuJywgbmFtZTogJ0FuY2VzdG9yIFRva2VuJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjZDliOWUzJywgdmFsdWU6IDk1LCB1c2U6ICdmb2N1cycsIHRhZ3M6IFsnYnVyaWFsJywgJ3NwaXJpdCddIH0sXG4gIHsgaWQ6ICd0b21iS2V5JywgbmFtZTogJ1RvbWIgS2V5JywgZ2x5cGg6ICc/JywgY29sb3I6ICcjYmZhYTcwJywgdmFsdWU6IDgwLCB1c2U6ICdrZXknLCB0YWdzOiBbJ2J1cmlhbCcsICdncmF2ZSddIH0sXG4gIHsgaWQ6ICdjdXJzZWRNaXJyb3InLCBuYW1lOiAnQ3Vyc2VkIE1pcnJvcicsIGdseXBoOiAn4pigJywgY29sb3I6ICcjZDlhM2M2JywgdmFsdWU6IDI0MCwgZmluZGFibGU6IGZhbHNlLCB0YWdzOiBbJ2N1cnNlJ10gfSxcbiAgeyBpZDogJ2dyYXZlRmxlZWNlJywgbmFtZTogJ0dyYXZlIEZsZWVjZScsIGdseXBoOiAn4pigJywgY29sb3I6ICcjOWQ4YmFmJywgdmFsdWU6IDE4MCwgZmluZGFibGU6IGZhbHNlLCB0YWdzOiBbJ2N1cnNlJ10gfSxcbiAgeyBpZDogJ3N0b3JtSWRvbCcsIG5hbWU6ICdTdG9ybSBJZG9sJywgZ2x5cGg6ICfimKAnLCBjb2xvcjogJyM5ZWJmZWMnLCB2YWx1ZTogMTgwLCBmaW5kYWJsZTogZmFsc2UsIHRhZ3M6IFsnY3Vyc2UnXSB9LFxuICB7IGlkOiAnb2F0aFNoYXJkJywgbmFtZTogJ09hdGggU2hhcmQnLCBnbHlwaDogJ+KYoCcsIGNvbG9yOiAnI2U3YzY4MCcsIHZhbHVlOiAyMDAsIGZpbmRhYmxlOiBmYWxzZSwgdGFnczogWydjdXJzZSddIH1cbl1cblxuY29uc3Qgdm95YWdlclRlcm1zOiBSZWFkb25seUFycmF5PHJlYWRvbmx5IFtzdHJpbmcsIHN0cmluZ10+ID0gW1xuICBbJ0NvdXJpZXInLCAnRmllbGQnXSwgWydPYnNpZGlhbicsICdBbGxveSddLCBbJ0NpbmRlcicsICdUaGVybWFsJ10sIFsnU29vdCcsICdDYXJib24nXSwgWydUaWRlJywgJ0ZsdXgnXSwgWydTdW5zdG9uZScsICdTb2xhciddLCBbJ1NwaXJpdCcsICdTaWduYWwnXSwgWydDaGFybScsICdNb2R1bGUnXSwgWydUYWxpc21hbicsICdNb2R1bGUnXSwgWydUb25pYycsICdHZWwnXSwgWydUcmFpbCcsICdTdXJ2ZXknXSwgWydNYXAnLCAnU3VydmV5IENoYXJ0J10sIFsnUm9wZScsICdMaW5lJ10sIFsnRmlyZS1hc2gnLCAnQnJlYWNoJ10sIFsnRmlyZScsICdUaGVybWFsJ10sIFsnU3RvbmUnLCAnSHVsbCddLCBbJ01vc3MnLCAnQmlvZmlsdGVyJ10sIFsnQmFyaycsICdDb21wb3NpdGUnXSwgWydSZXNpbicsICdTaWduYWwnXSwgWydGZWF0aGVyJywgJ01pY3JvZ3JhdiddLCBbJ1JlZWQnLCAnR2xpZGUnXSwgWydXaW5kJywgJ1ZlY3RvciddLCBbJ0dhbGUnLCAnVmVjdG9yJ10sIFsnU2t5JywgJ1N0YXInXSwgWydHcmF2ZScsICdNZW1vcmlhbCddLCBbJ0FuY2VzdG9yJywgJ0FyY2hpdmUnXSwgWydUb21iJywgJ0FyY2hpdmUnXSwgWydNb3VybmluZycsICdBcmNoaXZlJ10sIFsnQ3Vyc2VkJywgJ0NvcnJ1cHRlZCddLCBbJ09hdGgnLCAnUHJvdG9jb2wnXVxuXVxuY29uc3Qgdm95YWdlck5hbWUgPSAobmFtZTogc3RyaW5nKTogc3RyaW5nID0+IHZveWFnZXJUZXJtcy5yZWR1Y2UoKGN1cnJlbnQsIFtzb3VyY2UsIHJlcGxhY2VtZW50XSkgPT4gY3VycmVudC5yZXBsYWNlQWxsKHNvdXJjZSwgcmVwbGFjZW1lbnQpLCBuYW1lKVxuSVRFTVMuZm9yRWFjaChpdGVtID0+IHsgaXRlbS5uYW1lID0gdm95YWdlck5hbWUoaXRlbS5uYW1lKSB9KVxuXG5leHBvcnQgY29uc3QgSVRFTSA9IE9iamVjdC5mcm9tRW50cmllcyhJVEVNUy5tYXAoaXRlbSA9PiBbaXRlbS5pZCwgaXRlbV0pKSBhcyBSZWNvcmQ8c3RyaW5nLCBJdGVtRGVmaW5pdGlvbj5cbmV4cG9ydCBjb25zdCBpdGVtQnlJZCA9IChpZDogc3RyaW5nKTogSXRlbURlZmluaXRpb24gfCB1bmRlZmluZWQgPT4gSVRFTVtpZF1cbmV4cG9ydCBjb25zdCBpc0l0ZW1JZCA9IChpZDogdW5rbm93bik6IGlkIGlzIEl0ZW1JZCA9PiB0eXBlb2YgaWQgPT09ICdzdHJpbmcnICYmIGl0ZW1CeUlkKGlkKSAhPT0gdW5kZWZpbmVkXG5leHBvcnQgY29uc3QgU0NSSVBUUzogU2NyaXB0RGVmaW5pdGlvbltdID0gW1xuICB7IGl0ZW1JZDogJ2VtYmVyJywgaWQ6ICdlbWJlcicsIHNjaG9vbDogJ2VtYmVyJywgdGFnczogWydmaXJlJywgJ2RhbWFnZSddLCBmb2N1c0Nvc3Q6IDMsIHNoYXBlOiAnbGluZScsIHJhbmdlOiAxLCB1cGdyYWRlczogWydwb3RlbmN5JywgJ3JhbmdlJ10gfSxcbiAgeyBpdGVtSWQ6ICdtZW5kJywgaWQ6ICdtZW5kJywgc2Nob29sOiAndmVyZGFudCcsIHRhZ3M6IFsnaGVhbGluZyddLCBmb2N1c0Nvc3Q6IDMsIHNoYXBlOiAnYWRqYWNlbnQnLCByYW5nZTogMSwgdXBncmFkZXM6IFsncG90ZW5jeScsICdmb2N1c0Nvc3QnXSB9LFxuICB7IGl0ZW1JZDogJ3Jvb3QnLCBpZDogJ3Jvb3QnLCBzY2hvb2w6ICd2ZXJkYW50JywgdGFnczogWydyb290JywgJ2NvbnRyb2wnXSwgZm9jdXNDb3N0OiAzLCBzaGFwZTogJ2xpbmUnLCByYW5nZTogMiwgdXBncmFkZXM6IFsncG90ZW5jeScsICdyYW5nZSddIH0sXG4gIHsgaXRlbUlkOiAnd2F0ZXJTY3JpcHQnLCBpZDogJ3dhdGVyJywgc2Nob29sOiAndmVyZGFudCcsIHRhZ3M6IFsnd2F0ZXInLCAndGVycmFpbiddLCBmb2N1c0Nvc3Q6IDMsIHNoYXBlOiAnbGluZScsIHJhbmdlOiAyLCB1cGdyYWRlczogWydyYW5nZScsICdwb3RlbmN5J10gfSxcbiAgeyBpdGVtSWQ6ICdsdWxsJywgaWQ6ICdsdWxsJywgc2Nob29sOiAndmVyZGFudCcsIHRhZ3M6IFsnY3JlYXR1cmUnLCAnY29udHJvbCddLCBmb2N1c0Nvc3Q6IDQsIHNoYXBlOiAnbGluZScsIHJhbmdlOiAxLCB1cGdyYWRlczogWydwb3RlbmN5JywgJ3JhbmdlJ10gfSxcbiAgeyBpdGVtSWQ6ICdzaWdodCcsIGlkOiAnc2lnaHQnLCBzY2hvb2w6ICdhc3RyYWwnLCB0YWdzOiBbJ3Zpc2lvbiddLCBmb2N1c0Nvc3Q6IDMsIHNoYXBlOiAnYnVyc3QnLCByYW5nZTogMSwgdXBncmFkZXM6IFsncmFuZ2UnLCAnZm9jdXNDb3N0J10gfSxcbiAgeyBpdGVtSWQ6ICdibGluaycsIGlkOiAnYmxpbmsnLCBzY2hvb2w6ICdhc3RyYWwnLCB0YWdzOiBbJ3RlbGVwb3J0JywgJ21vdmVtZW50J10sIGZvY3VzQ29zdDogMywgc2hhcGU6ICdsaW5lJywgcmFuZ2U6IDMsIHVwZ3JhZGVzOiBbJ3JhbmdlJywgJ2ZvY3VzQ29zdCddIH0sXG4gIHsgaXRlbUlkOiAnZ3VzdCcsIGlkOiAnZ3VzdCcsIHNjaG9vbDogJ2FzdHJhbCcsIHRhZ3M6IFsnZm9yY2UnLCAnbW92ZW1lbnQnXSwgZm9jdXNDb3N0OiAzLCBzaGFwZTogJ2xpbmUnLCByYW5nZTogMSwgdXBncmFkZXM6IFsncG90ZW5jeScsICdyYW5nZSddIH0sXG4gIHsgaXRlbUlkOiAncHVsbCcsIGlkOiAncHVsbCcsIHNjaG9vbDogJ2FzdHJhbCcsIHRhZ3M6IFsnZm9yY2UnLCAnY29udHJvbCddLCBmb2N1c0Nvc3Q6IDMsIHNoYXBlOiAnbGluZScsIHJhbmdlOiAyLCB1cGdyYWRlczogWydwb3RlbmN5JywgJ3JhbmdlJ10gfSxcbiAgeyBpdGVtSWQ6ICd3YXJkU2NyaXB0JywgaWQ6ICd3YXJkJywgc2Nob29sOiAnYXN0cmFsJywgdGFnczogWyd3YXJkJ10sIGZvY3VzQ29zdDogMywgc2hhcGU6ICdhZGphY2VudCcsIHJhbmdlOiAxLCB1cGdyYWRlczogWydwb3RlbmN5JywgJ2ZvY3VzQ29zdCddIH0sXG4gIHsgaXRlbUlkOiAnZ2F0ZScsIGlkOiAnZ2F0ZScsIHNjaG9vbDogJ2FzdHJhbCcsIHRhZ3M6IFsndGVsZXBvcnQnXSwgZm9jdXNDb3N0OiAzLCBzaGFwZTogJ2xpbmUnLCByYW5nZTogMSwgdXBncmFkZXM6IFsncmFuZ2UnLCAnZm9jdXNDb3N0J10gfVxuXVxuZXhwb3J0IGNvbnN0IFNDUklQVF9CWV9JVEVNID0gT2JqZWN0LmZyb21FbnRyaWVzKFNDUklQVFMubWFwKHNjcmlwdCA9PiBbc2NyaXB0Lml0ZW1JZCwgc2NyaXB0XSkpIGFzIFJlY29yZDxzdHJpbmcsIFNjcmlwdERlZmluaXRpb24+XG5cbmNvbnN0IGNvbG9ueU1vbnN0ZXJQcmVmaXg6IFJlY29yZDxCaW9tZSwgc3RyaW5nPiA9IHsgbWluZTogJ0tlc3RyZWwnLCB3aWxkczogJ1ZlcmRhbnQnLCBjYXZlcm5zOiAnUGVsYWdvcycsIHJ1aW5zOiAnT3Jpc29uJywgZnVybmFjZTogJ0hlbGlvbicsIGZsb29kZWRSdWluczogJ05lcmlkYScsIGNsaWZmczogJ0FlcmllJywgYnVyaWFsOiAnTWVtb3JpYWwnLCBzYWx0RmxhdHM6ICdIYWxjeW9uJywgZnJvc3RSZWxpcXVhcnk6ICdCb3JlYWxpcycgfVxuXG5leHBvcnQgY29uc3QgTU9OU1RFUlM6IE1vbnN0ZXJEZWZpbml0aW9uW10gPSBbXG4gIHsgaWQ6ICdyYXQnLCBuYW1lOiAnRmllbGQgUmF0JywgZ2x5cGg6ICdyJywgY29sb3I6ICcjYjhhNTk4JywgaGVhbHRoOiA1LCBhdHRhY2s6IDIsIGRlZmVuc2U6IDgsIHNwZWVkOiAxMTAsIGFpOiAnY2hhc2UnLCB4cDogNiwgYmlvbWU6ICdtaW5lJyB9LFxuICB7IGlkOiAnbW9sZScsIG5hbWU6ICdCdXJyb3dpbmcgTW9sZScsIGdseXBoOiAnbScsIGNvbG9yOiAnIzkyOThhMicsIGhlYWx0aDogOSwgYXR0YWNrOiA0LCBkZWZlbnNlOiAxMCwgc3BlZWQ6IDkwLCBhaTogJ2NoYXNlJywgeHA6IDEwLCBiaW9tZTogJ21pbmUnIH0sXG4gIHsgaWQ6ICdzYXBwZXInLCBuYW1lOiAnRmlyZS1hc2ggVGhyb3dlcicsIGdseXBoOiAncycsIGNvbG9yOiAnI2Q2YTI2MycsIGhlYWx0aDogOCwgYXR0YWNrOiA1LCBkZWZlbnNlOiA5LCBzcGVlZDogMTAwLCBhaTogJ3JhbmdlZCcsIHhwOiAxNCwgYmlvbWU6ICdtaW5lJyB9LFxuICB7IGlkOiAnYmVldGxlJywgbmFtZTogJ09ic2lkaWFuIEJlZXRsZScsIGdseXBoOiAnYicsIGNvbG9yOiAnI2Q2YzE2ZCcsIGhlYWx0aDogMTMsIGF0dGFjazogNSwgZGVmZW5zZTogMTMsIHNwZWVkOiA3NSwgYWk6ICdjaGFzZScsIHhwOiAxOCwgYmlvbWU6ICdtaW5lJyB9LFxuICB7IGlkOiAnZHJpbGxlcicsIG5hbWU6ICdTdG9uZSBCcmVha2VyJywgZ2x5cGg6ICdkJywgY29sb3I6ICcjZGZiNzdhJywgaGVhbHRoOiAxMSwgYXR0YWNrOiA2LCBkZWZlbnNlOiAxMiwgc3BlZWQ6IDEwMCwgYWk6ICdyYW5nZWQnLCB4cDogMjAsIGJpb21lOiAnbWluZScgfSxcbiAgeyBpZDogJ3JhaWxndWFyZCcsIG5hbWU6ICdUcmFpbCBHdWFyZCcsIGdseXBoOiAnZycsIGNvbG9yOiAnI2NhZDFkYycsIGhlYWx0aDogMTIsIGF0dGFjazogNiwgZGVmZW5zZTogMTIsIHNwZWVkOiAxMDAsIGFpOiAnY2hhc2UnLCB4cDogMjIsIGJpb21lOiAnbWluZScsIHRhZ3M6IFsnbWluZScsICdyYWlsJ10gfSxcbiAgeyBpZDogJ2Z1c2V3YXJkZW4nLCBuYW1lOiAnRmlyZSBLZWVwZXInLCBnbHlwaDogJ2YnLCBjb2xvcjogJyNmMGEzNWUnLCBoZWFsdGg6IDEwLCBhdHRhY2s6IDcsIGRlZmVuc2U6IDEwLCBzcGVlZDogOTUsIGFpOiAncmFuZ2VkJywgeHA6IDI0LCBiaW9tZTogJ21pbmUnLCB0YWdzOiBbJ21pbmUnLCAndGVsZWdyYXBoJywgJ2NvdmVyJywgJ2V4cGxvc2l2ZSddIH0sXG4gIHsgaWQ6ICdmb3JlbWFuJywgbmFtZTogJ1RoZSBPYnNpZGlhbiBXYXJkZW4nLCBnbHlwaDogJ0YnLCBjb2xvcjogJyNmZmUwODAnLCBoZWFsdGg6IDQyLCBhdHRhY2s6IDgsIGRlZmVuc2U6IDE0LCBzcGVlZDogMTA1LCBhaTogJ2d1YXJkaWFuJywgeHA6IDcwLCBiaW9tZTogJ21pbmUnIH0sXG4gIHsgaWQ6ICd0aG9ybmxpbmcnLCBuYW1lOiAnVGhvcm5saW5nJywgZ2x5cGg6ICd0JywgY29sb3I6ICcjODZjMDY0JywgaGVhbHRoOiA4LCBhdHRhY2s6IDQsIGRlZmVuc2U6IDEwLCBzcGVlZDogMTA1LCBhaTogJ2NoYXNlJywgeHA6IDExLCBiaW9tZTogJ3dpbGRzJyB9LFxuICB7IGlkOiAnYm9hcicsIG5hbWU6ICdNb3NzIEJvYXInLCBnbHlwaDogJ2InLCBjb2xvcjogJyNhNzdkNTgnLCBoZWFsdGg6IDE1LCBhdHRhY2s6IDcsIGRlZmVuc2U6IDExLCBzcGVlZDogMTE1LCBhaTogJ2NoYXNlJywgeHA6IDE5LCBiaW9tZTogJ3dpbGRzJyB9LFxuICB7IGlkOiAnc3BpdHRlcicsIG5hbWU6ICdWaW5lIFNwaXR0ZXInLCBnbHlwaDogJ3YnLCBjb2xvcjogJyM2N2JhN2InLCBoZWFsdGg6IDEwLCBhdHRhY2s6IDYsIGRlZmVuc2U6IDksIHNwZWVkOiA5MCwgYWk6ICdyYW5nZWQnLCB4cDogMTYsIGJpb21lOiAnd2lsZHMnIH0sXG4gIHsgaWQ6ICd3aXNwJywgbmFtZTogJ01hcnNoIFdpc3AnLCBnbHlwaDogJ3cnLCBjb2xvcjogJyM5YmU2YmMnLCBoZWFsdGg6IDcsIGF0dGFjazogNiwgZGVmZW5zZTogMTIsIHNwZWVkOiAxMzAsIGFpOiAnd2FuZGVyJywgeHA6IDIxLCBiaW9tZTogJ3dpbGRzJyB9LFxuICB7IGlkOiAnZnJvZycsIG5hbWU6ICdDYW5vcHkgRnJvZycsIGdseXBoOiAnZicsIGNvbG9yOiAnI2E5ZDY2NicsIGhlYWx0aDogMTEsIGF0dGFjazogNiwgZGVmZW5zZTogMTEsIHNwZWVkOiAxMjAsIGFpOiAnY2hhc2UnLCB4cDogMjMsIGJpb21lOiAnd2lsZHMnIH0sXG4gIHsgaWQ6ICd2aW5lYmluZGVyJywgbmFtZTogJ1ZpbmUgQmluZGVyJywgZ2x5cGg6ICdWJywgY29sb3I6ICcjNWQ5ZjY3JywgaGVhbHRoOiAxMiwgYXR0YWNrOiA1LCBkZWZlbnNlOiAxMSwgc3BlZWQ6IDk1LCBhaTogJ3JhbmdlZCcsIHhwOiAyNCwgYmlvbWU6ICd3aWxkcycsIHRhZ3M6IFsnd2lsZHMnLCAncm9vdCcsICd0ZWxlZ3JhcGgnXSB9LFxuICB7IGlkOiAnbWFyc2hza2F0ZXInLCBuYW1lOiAnTWFyc2ggU2thdGVyJywgZ2x5cGg6ICdrJywgY29sb3I6ICcjNzJiOWIxJywgaGVhbHRoOiAxMCwgYXR0YWNrOiA2LCBkZWZlbnNlOiAxMiwgc3BlZWQ6IDEwMCwgYWk6ICdjaGFzZScsIHhwOiAyNSwgYmlvbWU6ICd3aWxkcycsIHRhZ3M6IFsnd2lsZHMnLCAnd2F0ZXInLCAnbW9iaWxpdHknXSB9LFxuICB7IGlkOiAnd2Vid2VhdmVyJywgbmFtZTogJ1dlYiBXZWF2ZXInLCBnbHlwaDogJ1cnLCBjb2xvcjogJyNkNWRjZTQnLCBoZWFsdGg6IDExLCBhdHRhY2s6IDUsIGRlZmVuc2U6IDEyLCBzcGVlZDogOTAsIGFpOiAncmFuZ2VkJywgeHA6IDI2LCBiaW9tZTogJ3dpbGRzJywgdGFnczogWyd3aWxkcycsICd3ZWInLCAnc25hcmUnLCAndGVsZWdyYXBoJ10gfSxcbiAgeyBpZDogJ3N0YXJ0bGVkQmlyZHMnLCBuYW1lOiAnU3RhcnRsZWQgQmlyZHMnLCBnbHlwaDogJ2InLCBjb2xvcjogJyNkOGJjODInLCBoZWFsdGg6IDQsIGF0dGFjazogMywgZGVmZW5zZTogOCwgc3BlZWQ6IDEyNSwgYWk6ICdjaGFzZScsIHhwOiA4LCBiaW9tZTogJ3dpbGRzJywgdGFnczogWyd3aWxkcycsICdtb2JpbGl0eSddLCBzcGF3bjogJ3RyaWdnZXJlZCcgfSxcbiAgeyBpZDogJ2hlYXJ0d29vZCcsIG5hbWU6ICdIZWFydHdvb2QgU3RhZycsIGdseXBoOiAnSCcsIGNvbG9yOiAnI2QxZTI4MScsIGhlYWx0aDogNTIsIGF0dGFjazogMTAsIGRlZmVuc2U6IDE0LCBzcGVlZDogMTEwLCBhaTogJ2d1YXJkaWFuJywgeHA6IDkwLCBiaW9tZTogJ3dpbGRzJyB9LFxuICB7IGlkOiAnY3Jhd2xlcicsIG5hbWU6ICdDcnlzdGFsIENyYXdsZXInLCBnbHlwaDogJ2MnLCBjb2xvcjogJyM3YmNmZTAnLCBoZWFsdGg6IDE0LCBhdHRhY2s6IDcsIGRlZmVuc2U6IDEzLCBzcGVlZDogOTUsIGFpOiAnY2hhc2UnLCB4cDogMjMsIGJpb21lOiAnY2F2ZXJucycgfSxcbiAgeyBpZDogJ21hZ21hJywgbmFtZTogJ1Nob3JlIE5ld3QnLCBnbHlwaDogJ24nLCBjb2xvcjogJyM3M2I2YzEnLCBoZWFsdGg6IDEyLCBhdHRhY2s6IDgsIGRlZmVuc2U6IDExLCBzcGVlZDogMTA1LCBhaTogJ2NoYXNlJywgeHA6IDI1LCBiaW9tZTogJ2NhdmVybnMnLCB0YWdzOiBbJ2NhdmVybnMnLCAnd2F0ZXInLCAnbW9iaWxpdHknXSB9LFxuICB7IGlkOiAnZWNobycsIG5hbWU6ICdFY2hvIEJhdCcsIGdseXBoOiAnZScsIGNvbG9yOiAnI2JhOWRkZCcsIGhlYWx0aDogOSwgYXR0YWNrOiA3LCBkZWZlbnNlOiAxMiwgc3BlZWQ6IDE0MCwgYWk6ICd3YW5kZXInLCB4cDogMjYsIGJpb21lOiAnY2F2ZXJucycsIHRhZ3M6IFsnY2F2ZXJucycsICdkYXJrbmVzcycsICdlY2hvJ10gfSxcbiAgeyBpZDogJ3NlZXInLCBuYW1lOiAnR3JvdHRvIFNlZXInLCBnbHlwaDogJ3MnLCBjb2xvcjogJyNiYThhZTcnLCBoZWFsdGg6IDEzLCBhdHRhY2s6IDksIGRlZmVuc2U6IDEyLCBzcGVlZDogOTUsIGFpOiAncmFuZ2VkJywgeHA6IDMwLCBiaW9tZTogJ2NhdmVybnMnLCB0YWdzOiBbJ2NhdmVybnMnLCAnZGFya25lc3MnLCAndGVsZWdyYXBoJ10gfSxcbiAgeyBpZDogJ3NsdWcnLCBuYW1lOiAnVGlkZSBTbHVnJywgZ2x5cGg6ICd1JywgY29sb3I6ICcjYThjNWNmJywgaGVhbHRoOiAxOSwgYXR0YWNrOiA4LCBkZWZlbnNlOiAxNSwgc3BlZWQ6IDY1LCBhaTogJ2NoYXNlJywgeHA6IDMxLCBiaW9tZTogJ2NhdmVybnMnLCB0YWdzOiBbJ2NhdmVybnMnLCAnd2F0ZXInLCAnY292ZXInXSB9LFxuICB7IGlkOiAnY2luZGVyaW1wJywgbmFtZTogJ0JyaW5lIFNrdWxrZXInLCBnbHlwaDogJ2knLCBjb2xvcjogJyM4ZWQ5ZGUnLCBoZWFsdGg6IDEwLCBhdHRhY2s6IDcsIGRlZmVuc2U6IDEwLCBzcGVlZDogMTAwLCBhaTogJ3JhbmdlZCcsIHhwOiAyOCwgYmlvbWU6ICdjYXZlcm5zJywgdGFnczogWydjYXZlcm5zJywgJ3dhdGVyJywgJ3RlbGVncmFwaCddIH0sXG4gIHsgaWQ6ICdmdW1lZWVsJywgbmFtZTogJ1RpZGUgRWVsJywgZ2x5cGg6ICd1JywgY29sb3I6ICcjOGZjNTlhJywgaGVhbHRoOiAxMywgYXR0YWNrOiA3LCBkZWZlbnNlOiAxMiwgc3BlZWQ6IDEwMCwgYWk6ICdjaGFzZScsIHhwOiAyOSwgYmlvbWU6ICdjYXZlcm5zJywgdGFnczogWydjYXZlcm5zJywgJ3dhdGVyJywgJ3RpZGUnLCAnbW9iaWxpdHknXSB9LFxuICB7IGlkOiAnZ2xvb21zZWVyJywgbmFtZTogJ0dsb29tIFNlZXInLCBnbHlwaDogJ0cnLCBjb2xvcjogJyM3YzZkOWYnLCBoZWFsdGg6IDEyLCBhdHRhY2s6IDgsIGRlZmVuc2U6IDEzLCBzcGVlZDogOTUsIGFpOiAncmFuZ2VkJywgeHA6IDMyLCBiaW9tZTogJ2NhdmVybnMnLCB0YWdzOiBbJ2NhdmVybnMnLCAnZGFya25lc3MnLCAnbGlnaHQnLCAnY291bnRlcnBsYXknXSB9LFxuICB7IGlkOiAnY3J5c3RhbHB1bGxlcicsIG5hbWU6ICdVbmRlcnRvdyBQdWxsZXInLCBnbHlwaDogJ3AnLCBjb2xvcjogJyM5ZWNjZTMnLCBoZWFsdGg6IDE0LCBhdHRhY2s6IDcsIGRlZmVuc2U6IDEzLCBzcGVlZDogOTAsIGFpOiAncmFuZ2VkJywgeHA6IDMzLCBiaW9tZTogJ2NhdmVybnMnLCB0YWdzOiBbJ2NhdmVybnMnLCAnd2F0ZXInLCAnZGlzcGxhY2VtZW50JywgJ3RlbGVncmFwaCddIH0sXG4gIHsgaWQ6ICdnZW9kZScsIG5hbWU6ICdUaWRlbWF3JywgZ2x5cGg6ICdHJywgY29sb3I6ICcjOGNlNWYyJywgaGVhbHRoOiA2MiwgYXR0YWNrOiAxMiwgZGVmZW5zZTogMTYsIHNwZWVkOiAxMDAsIGFpOiAnZ3VhcmRpYW4nLCB4cDogMTE1LCBiaW9tZTogJ2NhdmVybnMnLCB0YWdzOiBbJ2NhdmVybnMnLCAnd2F0ZXInLCAnY3VycmVudCddIH0sXG4gIHsgaWQ6ICdzY2FyYWInLCBuYW1lOiAnQXNoIFNjYXJhYicsIGdseXBoOiAncycsIGNvbG9yOiAnI2Q4YjM2MycsIGhlYWx0aDogMTYsIGF0dGFjazogOCwgZGVmZW5zZTogMTUsIHNwZWVkOiA5NSwgYWk6ICdjaGFzZScsIHhwOiAzMiwgYmlvbWU6ICdydWlucycgfSxcbiAgeyBpZDogJ3NlbnRpbmVsJywgbmFtZTogJ1N0b25lIFNlbnRpbmVsJywgZ2x5cGg6ICdTJywgY29sb3I6ICcjOWRhNWFhJywgaGVhbHRoOiAyMywgYXR0YWNrOiAxMCwgZGVmZW5zZTogMTcsIHNwZWVkOiA3NSwgYWk6ICdjaGFzZScsIHhwOiA0MCwgYmlvbWU6ICdydWlucycgfSxcbiAgeyBpZDogJ29yYWNsZScsIG5hbWU6ICdEdXN0IE9yYWNsZScsIGdseXBoOiAnbycsIGNvbG9yOiAnI2U5YzQ4OScsIGhlYWx0aDogMTUsIGF0dGFjazogMTEsIGRlZmVuc2U6IDEzLCBzcGVlZDogMTAwLCBhaTogJ3JhbmdlZCcsIHhwOiA0NSwgYmlvbWU6ICdydWlucycgfSxcbiAgeyBpZDogJ3NoYWRlJywgbmFtZTogJ1ZhdWx0IFNoYWRlJywgZ2x5cGg6ICdoJywgY29sb3I6ICcjYzFhNWVkJywgaGVhbHRoOiAxNCwgYXR0YWNrOiAxMCwgZGVmZW5zZTogMTYsIHNwZWVkOiAxMjUsIGFpOiAnd2FuZGVyJywgeHA6IDQ4LCBiaW9tZTogJ3J1aW5zJyB9LFxuICB7IGlkOiAnY3VsdGlzdCcsIG5hbWU6ICdBc2ggQ3VsdGlzdCcsIGdseXBoOiAnYycsIGNvbG9yOiAnI2RmOWE3YycsIGhlYWx0aDogMTgsIGF0dGFjazogMTEsIGRlZmVuc2U6IDE0LCBzcGVlZDogMTAwLCBhaTogJ3JhbmdlZCcsIHhwOiA1MSwgYmlvbWU6ICdydWlucycgfSxcbiAgeyBpZDogJ3dhcmRhY29seXRlJywgbmFtZTogJ1dhcmQgQWNvbHl0ZScsIGdseXBoOiAnYScsIGNvbG9yOiAnI2MyOWNlNicsIGhlYWx0aDogMTUsIGF0dGFjazogOCwgZGVmZW5zZTogMTMsIHNwZWVkOiA5MCwgYWk6ICdyYW5nZWQnLCB4cDogMzcsIGJpb21lOiAncnVpbnMnLCB0YWdzOiBbJ3J1aW5zJywgJ3dhcmQnLCAncml0dWFsJ10gfSxcbiAgeyBpZDogJ2RhcnRhZGVwdCcsIG5hbWU6ICdEYXJ0IEFkZXB0JywgZ2x5cGg6ICdkJywgY29sb3I6ICcjZDhiNTc2JywgaGVhbHRoOiAxMywgYXR0YWNrOiA5LCBkZWZlbnNlOiAxMiwgc3BlZWQ6IDEwMCwgYWk6ICdyYW5nZWQnLCB4cDogMzgsIGJpb21lOiAncnVpbnMnLCB0YWdzOiBbJ3J1aW5zJywgJ2RhcnQnLCAndGVsZWdyYXBoJ10gfSxcbiAgeyBpZDogJ2xvY2trZWVwZXInLCBuYW1lOiAnTG9jayBLZWVwZXInLCBnbHlwaDogJ2snLCBjb2xvcjogJyNjNGI0ODgnLCBoZWFsdGg6IDE4LCBhdHRhY2s6IDksIGRlZmVuc2U6IDE1LCBzcGVlZDogODAsIGFpOiAnY2hhc2UnLCB4cDogNDIsIGJpb21lOiAncnVpbnMnLCB0YWdzOiBbJ3J1aW5zJywgJ2xvY2snLCAnY291bnRlcnBsYXknXSB9LFxuICB7IGlkOiAncml0dWFsaXN0JywgbmFtZTogJ0FzaCBSaXR1YWxpc3QnLCBnbHlwaDogJ3InLCBjb2xvcjogJyNkODhlYTQnLCBoZWFsdGg6IDE0LCBhdHRhY2s6IDEwLCBkZWZlbnNlOiAxMywgc3BlZWQ6IDk1LCBhaTogJ3JhbmdlZCcsIHhwOiA0NCwgYmlvbWU6ICdydWlucycsIHRhZ3M6IFsncnVpbnMnLCAncml0dWFsJywgJ3RlbGVncmFwaCddIH0sXG4gIHsgaWQ6ICdyZWdlbnQnLCBuYW1lOiAnVGhlIFN0b25lIEtlZXBlcicsIGdseXBoOiAnUicsIGNvbG9yOiAnI2ZmZGI3NScsIGhlYWx0aDogODQsIGF0dGFjazogMTUsIGRlZmVuc2U6IDE5LCBzcGVlZDogMTEwLCBhaTogJ2d1YXJkaWFuJywgeHA6IDE4MCwgYmlvbWU6ICdydWlucycgfSxcbiAgeyBpZDogJ2NpbmRlcmxpbmcnLCBuYW1lOiAnQ2luZGVybGluZycsIGdseXBoOiAnYycsIGNvbG9yOiAnI2YwOGQ1YicsIGhlYWx0aDogMTgsIGF0dGFjazogMTIsIGRlZmVuc2U6IDE2LCBzcGVlZDogMTEwLCBhaTogJ2NoYXNlJywgeHA6IDQ4LCBiaW9tZTogJ2Z1cm5hY2UnLCB0YWdzOiBbJ2Z1cm5hY2UnLCAnZmlyZScsICdoZWF0J10gfSxcbiAgeyBpZDogJ3Ntb2tlc2t1bGsnLCBuYW1lOiAnU21va2UgU2t1bGsnLCBnbHlwaDogJ3MnLCBjb2xvcjogJyM5Y2ExYWQnLCBoZWFsdGg6IDE1LCBhdHRhY2s6IDExLCBkZWZlbnNlOiAxNywgc3BlZWQ6IDEzMCwgYWk6ICd3YW5kZXInLCB4cDogNTAsIGJpb21lOiAnZnVybmFjZScsIHRhZ3M6IFsnZnVybmFjZScsICdzbW9rZScsICdtb2JpbGl0eSddIH0sXG4gIHsgaWQ6ICdsaWZ0d2FyZGVuJywgbmFtZTogJ0xpZnQgV2FyZGVuJywgZ2x5cGg6ICdsJywgY29sb3I6ICcjZTljNDdlJywgaGVhbHRoOiAyNCwgYXR0YWNrOiAxMiwgZGVmZW5zZTogMTksIHNwZWVkOiA4NSwgYWk6ICdjaGFzZScsIHhwOiA1OCwgYmlvbWU6ICdmdXJuYWNlJywgdGFnczogWydmdXJuYWNlJywgJ2xpZnQnLCAnY291bnRlcnBsYXknXSB9LFxuICB7IGlkOiAnc2xhZ2Nhc3RlcicsIG5hbWU6ICdTbGFnIENhc3RlcicsIGdseXBoOiAncycsIGNvbG9yOiAnI2UyNmU0YycsIGhlYWx0aDogMTcsIGF0dGFjazogMTMsIGRlZmVuc2U6IDE2LCBzcGVlZDogOTUsIGFpOiAncmFuZ2VkJywgeHA6IDYwLCBiaW9tZTogJ2Z1cm5hY2UnLCB0YWdzOiBbJ2Z1cm5hY2UnLCAnZmlyZScsICd0ZWxlZ3JhcGgnXSB9LFxuICB7IGlkOiAnYnJlYWttYXcnLCBuYW1lOiAnQnJlYWsgTWF3JywgZ2x5cGg6ICdiJywgY29sb3I6ICcjYmQ4NTY3JywgaGVhbHRoOiAyOSwgYXR0YWNrOiAxMywgZGVmZW5zZTogMjAsIHNwZWVkOiA4MCwgYWk6ICdjaGFzZScsIHhwOiA2NSwgYmlvbWU6ICdmdXJuYWNlJywgdGFnczogWydmdXJuYWNlJywgJ2JyZWFrd2FsbCcsICdmb3JjZSddIH0sXG4gIHsgaWQ6ICdhc2hvcmFjbGUnLCBuYW1lOiAnQXNoIE9yYWNsZScsIGdseXBoOiAnYScsIGNvbG9yOiAnI2U2YjRhMicsIGhlYWx0aDogMTgsIGF0dGFjazogMTQsIGRlZmVuc2U6IDE3LCBzcGVlZDogOTUsIGFpOiAncmFuZ2VkJywgeHA6IDY2LCBiaW9tZTogJ2Z1cm5hY2UnLCB0YWdzOiBbJ2Z1cm5hY2UnLCAnc21va2UnLCAndGVsZWdyYXBoJ10gfSxcbiAgeyBpZDogJ2tpbG5oZWFydCcsIG5hbWU6ICdUaGUgS2lsbiBIZWFydCcsIGdseXBoOiAnSycsIGNvbG9yOiAnI2ZmZDA3MCcsIGhlYWx0aDogMTA4LCBhdHRhY2s6IDE3LCBkZWZlbnNlOiAyMiwgc3BlZWQ6IDEwNSwgYWk6ICdndWFyZGlhbicsIHhwOiAyNDAsIGJpb21lOiAnZnVybmFjZScsIHRhZ3M6IFsnZnVybmFjZScsICdmaXJlJywgJ2JyZWFrd2FsbCddIH0sXG4gIHsgaWQ6ICd0aWRld3JhaXRoJywgbmFtZTogJ1RpZGUgV3JhaXRoJywgZ2x5cGg6ICd0JywgY29sb3I6ICcjODVkOWRmJywgaGVhbHRoOiAyMCwgYXR0YWNrOiAxNCwgZGVmZW5zZTogMTgsIHNwZWVkOiAxMjAsIGFpOiAnd2FuZGVyJywgeHA6IDY1LCBiaW9tZTogJ2Zsb29kZWRSdWlucycsIHRhZ3M6IFsnZmxvb2RlZFJ1aW5zJywgJ3dhdGVyJywgJ2N1cnJlbnQnXSB9LFxuICB7IGlkOiAnYW5jaG9yY3JhYicsIG5hbWU6ICdBbmNob3IgQ3JhYicsIGdseXBoOiAnYScsIGNvbG9yOiAnIzgzYmRjNicsIGhlYWx0aDogMzEsIGF0dGFjazogMTMsIGRlZmVuc2U6IDIzLCBzcGVlZDogNzUsIGFpOiAnY2hhc2UnLCB4cDogNzIsIGJpb21lOiAnZmxvb2RlZFJ1aW5zJywgdGFnczogWydmbG9vZGVkUnVpbnMnLCAnYW5jaG9yJywgJ2d1YXJkJ10gfSxcbiAgeyBpZDogJ3NpbHRzZWVyJywgbmFtZTogJ1NpbHQgU2VlcicsIGdseXBoOiAncycsIGNvbG9yOiAnI2M1ZDhkNCcsIGhlYWx0aDogMTksIGF0dGFjazogMTUsIGRlZmVuc2U6IDE4LCBzcGVlZDogOTUsIGFpOiAncmFuZ2VkJywgeHA6IDc0LCBiaW9tZTogJ2Zsb29kZWRSdWlucycsIHRhZ3M6IFsnZmxvb2RlZFJ1aW5zJywgJ2N1cnJlbnQnLCAndGVsZWdyYXBoJ10gfSxcbiAgeyBpZDogJ2Ryb3duYmxhZGUnLCBuYW1lOiAnRHJvd25ibGFkZScsIGdseXBoOiAnZCcsIGNvbG9yOiAnIzcxYjdjMycsIGhlYWx0aDogMjQsIGF0dGFjazogMTUsIGRlZmVuc2U6IDIwLCBzcGVlZDogMTEwLCBhaTogJ2NoYXNlJywgeHA6IDc4LCBiaW9tZTogJ2Zsb29kZWRSdWlucycsIHRhZ3M6IFsnZmxvb2RlZFJ1aW5zJywgJ2JsYWRlJywgJ3dhdGVyJ10gfSxcbiAgeyBpZDogJ2NvcmFsZ3VhcmQnLCBuYW1lOiAnQ29yYWwgR3VhcmQnLCBnbHlwaDogJ2MnLCBjb2xvcjogJyNkNjliOTInLCBoZWFsdGg6IDMzLCBhdHRhY2s6IDE0LCBkZWZlbnNlOiAyNCwgc3BlZWQ6IDcwLCBhaTogJ2NoYXNlJywgeHA6IDgyLCBiaW9tZTogJ2Zsb29kZWRSdWlucycsIHRhZ3M6IFsnZmxvb2RlZFJ1aW5zJywgJ2FuY2hvcicsICdjb3VudGVycGxheSddIH0sXG4gIHsgaWQ6ICdjdXJyZW50Y2FsbGVyJywgbmFtZTogJ0N1cnJlbnQgQ2FsbGVyJywgZ2x5cGg6ICdjJywgY29sb3I6ICcjOWFlNWVhJywgaGVhbHRoOiAyMCwgYXR0YWNrOiAxNiwgZGVmZW5zZTogMTgsIHNwZWVkOiAxMDAsIGFpOiAncmFuZ2VkJywgeHA6IDg0LCBiaW9tZTogJ2Zsb29kZWRSdWlucycsIHRhZ3M6IFsnZmxvb2RlZFJ1aW5zJywgJ2N1cnJlbnQnLCAnZGlzcGxhY2VtZW50J10gfSxcbiAgeyBpZDogJ2Ryb3duZWRSZWdlbnQnLCBuYW1lOiAnVGhlIERyb3duZWQgUmVnZW50JywgZ2x5cGg6ICdEJywgY29sb3I6ICcjYjNlZGYwJywgaGVhbHRoOiAxMzIsIGF0dGFjazogMjAsIGRlZmVuc2U6IDI1LCBzcGVlZDogMTAwLCBhaTogJ2d1YXJkaWFuJywgeHA6IDMyMCwgYmlvbWU6ICdmbG9vZGVkUnVpbnMnLCB0YWdzOiBbJ2Zsb29kZWRSdWlucycsICd3YXRlcicsICdhbmNob3InXSB9LFxuICB7IGlkOiAnY2xpZmZraXRlJywgbmFtZTogJ0NsaWZmIEtpdGUnLCBnbHlwaDogJ2snLCBjb2xvcjogJyNiOWRjZjQnLCBoZWFsdGg6IDE4LCBhdHRhY2s6IDEwLCBkZWZlbnNlOiAxNSwgc3BlZWQ6IDEzNSwgYWk6ICd3YW5kZXInLCB4cDogNTUsIGJpb21lOiAnY2xpZmZzJywgdGFnczogWydjbGlmZnMnLCAnd2luZCcsICdtb2JpbGl0eSddIH0sXG4gIHsgaWQ6ICdyb3BlUmFpZGVyJywgbmFtZTogJ1JvcGUgUmFpZGVyJywgZ2x5cGg6ICdyJywgY29sb3I6ICcjZDhiNjZmJywgaGVhbHRoOiAyMywgYXR0YWNrOiAxMiwgZGVmZW5zZTogMTcsIHNwZWVkOiAxMTAsIGFpOiAnY2hhc2UnLCB4cDogNjIsIGJpb21lOiAnY2xpZmZzJywgdGFnczogWydjbGlmZnMnLCAnY2xpbWInLCAnaG9vayddIH0sXG4gIHsgaWQ6ICdnYWxlU2VlcicsIG5hbWU6ICdHYWxlIFNlZXInLCBnbHlwaDogJ2cnLCBjb2xvcjogJyNhOGM3ZmYnLCBoZWFsdGg6IDE3LCBhdHRhY2s6IDEzLCBkZWZlbnNlOiAxNSwgc3BlZWQ6IDEwNSwgYWk6ICdyYW5nZWQnLCB4cDogNjcsIGJpb21lOiAnY2xpZmZzJywgdGFnczogWydjbGlmZnMnLCAnd2luZCcsICd0ZWxlZ3JhcGgnXSB9LFxuICB7IGlkOiAnbGVkZ2VTdGFsa2VyJywgbmFtZTogJ0xlZGdlIFN0YWxrZXInLCBnbHlwaDogJ2wnLCBjb2xvcjogJyM4Mzk4YjQnLCBoZWFsdGg6IDI1LCBhdHRhY2s6IDExLCBkZWZlbnNlOiAxOSwgc3BlZWQ6IDk1LCBhaTogJ2NoYXNlJywgeHA6IDY2LCBiaW9tZTogJ2NsaWZmcycsIHRhZ3M6IFsnY2xpZmZzJywgJ2NsaW1iJywgJ2NvdW50ZXJwbGF5J10gfSxcbiAgeyBpZDogJ3N0b3JtQ3JvdycsIG5hbWU6ICdTdG9ybSBDcm93JywgZ2x5cGg6ICdjJywgY29sb3I6ICcjNzc4ZWMzJywgaGVhbHRoOiAxNSwgYXR0YWNrOiAxNCwgZGVmZW5zZTogMTQsIHNwZWVkOiAxNDUsIGFpOiAncmFuZ2VkJywgeHA6IDcwLCBiaW9tZTogJ2NsaWZmcycsIHRhZ3M6IFsnY2xpZmZzJywgJ3dpbmQnLCAnZm9yY2UnXSwgdGVycmFpbkFmZmluaXR5OiBbJ2xlZGdlJ10gfSxcbiAgeyBpZDogJ2NyYWdNb3RoJywgbmFtZTogJ0NyYWcgTW90aCcsIGdseXBoOiAnbScsIGNvbG9yOiAnI2NjZDdlYicsIGhlYWx0aDogMTYsIGF0dGFjazogMTIsIGRlZmVuc2U6IDE1LCBzcGVlZDogMTQwLCBhaTogJ3dhbmRlcicsIHhwOiA1OCwgYmlvbWU6ICdjbGlmZnMnLCB0YWdzOiBbJ2NsaWZmcycsICd3aW5kJywgJ2xpZ2h0J10gfSxcbiAgeyBpZDogJ3NjcmVlSG91bmQnLCBuYW1lOiAnU2NyZWUgSG91bmQnLCBnbHlwaDogJ2gnLCBjb2xvcjogJyM4Yjk5YWEnLCBoZWFsdGg6IDI3LCBhdHRhY2s6IDEzLCBkZWZlbnNlOiAxOCwgc3BlZWQ6IDExMCwgYWk6ICdjaGFzZScsIHhwOiA3MiwgYmlvbWU6ICdjbGlmZnMnLCB0YWdzOiBbJ2NsaWZmcycsICdmb3JjZScsICdjb3VudGVycGxheSddIH0sXG4gIHsgaWQ6ICd3aXJlU2luZ2VyJywgbmFtZTogJ1dpcmUgU2luZ2VyJywgZ2x5cGg6ICd3JywgY29sb3I6ICcjYWRjOWVkJywgaGVhbHRoOiAxOCwgYXR0YWNrOiAxNCwgZGVmZW5zZTogMTYsIHNwZWVkOiAxMDAsIGFpOiAncmFuZ2VkJywgeHA6IDc0LCBiaW9tZTogJ2NsaWZmcycsIHRhZ3M6IFsnY2xpZmZzJywgJ3dpbmQnLCAndGVsZWdyYXBoJ10gfSxcbiAgeyBpZDogJ3NreVdhcmRlbicsIG5hbWU6ICdTa3kgV2FyZGVuJywgZ2x5cGg6ICdTJywgY29sb3I6ICcjZWNmNWZmJywgaGVhbHRoOiAxMTgsIGF0dGFjazogMTgsIGRlZmVuc2U6IDIzLCBzcGVlZDogMTE1LCBhaTogJ2d1YXJkaWFuJywgeHA6IDI4MCwgYmlvbWU6ICdjbGlmZnMnLCB0YWdzOiBbJ2NsaWZmcycsICd3aW5kJywgJ2NsaW1iJ10gfSxcbiAgeyBpZDogJ2dyYXZlTWl0ZScsIG5hbWU6ICdHcmF2ZSBNaXRlJywgZ2x5cGg6ICdtJywgY29sb3I6ICcjYTk5YWFkJywgaGVhbHRoOiAyMCwgYXR0YWNrOiAxMSwgZGVmZW5zZTogMTcsIHNwZWVkOiAxMDUsIGFpOiAnY2hhc2UnLCB4cDogNjAsIGJpb21lOiAnYnVyaWFsJywgdGFnczogWydidXJpYWwnLCAnZ3JhdmUnXSB9LFxuICB7IGlkOiAnb3NzdWFyeUd1YXJkJywgbmFtZTogJ09zc3VhcnkgR3VhcmQnLCBnbHlwaDogJ28nLCBjb2xvcjogJyNkOWQzYzUnLCBoZWFsdGg6IDMyLCBhdHRhY2s6IDEzLCBkZWZlbnNlOiAyMiwgc3BlZWQ6IDc1LCBhaTogJ2NoYXNlJywgeHA6IDc0LCBiaW9tZTogJ2J1cmlhbCcsIHRhZ3M6IFsnYnVyaWFsJywgJ2dyYXZlJywgJ2d1YXJkJ10gfSxcbiAgeyBpZDogJ21vdXJuZXInLCBuYW1lOiAnTW91cm5lcicsIGdseXBoOiAnbScsIGNvbG9yOiAnI2M5YTZkYicsIGhlYWx0aDogMTksIGF0dGFjazogMTQsIGRlZmVuc2U6IDE3LCBzcGVlZDogOTUsIGFpOiAncmFuZ2VkJywgeHA6IDc2LCBiaW9tZTogJ2J1cmlhbCcsIHRhZ3M6IFsnYnVyaWFsJywgJ3NwaXJpdCcsICd0ZWxlZ3JhcGgnXSB9LFxuICB7IGlkOiAnYW5jZXN0b3JFY2hvJywgbmFtZTogJ0FuY2VzdG9yIEVjaG8nLCBnbHlwaDogJ2UnLCBjb2xvcjogJyNjYmJkZTcnLCBoZWFsdGg6IDE3LCBhdHRhY2s6IDE1LCBkZWZlbnNlOiAxNiwgc3BlZWQ6IDEzMCwgYWk6ICd3YW5kZXInLCB4cDogNzgsIGJpb21lOiAnYnVyaWFsJywgdGFnczogWydidXJpYWwnLCAnc3Bpcml0JywgJ2VjaG8nXSB9LFxuICB7IGlkOiAndG9tYldhcmRlbicsIG5hbWU6ICdUb21iIFdhcmRlbicsIGdseXBoOiAndCcsIGNvbG9yOiAnIzlkOGI3NicsIGhlYWx0aDogMjksIGF0dGFjazogMTQsIGRlZmVuc2U6IDIxLCBzcGVlZDogODUsIGFpOiAnY2hhc2UnLCB4cDogODAsIGJpb21lOiAnYnVyaWFsJywgdGFnczogWydidXJpYWwnLCAnZ3JhdmUnLCAnY291bnRlcnBsYXknXSwgdGVycmFpbkFmZmluaXR5OiBbJ2dyYXZlU29pbCddIH0sXG4gIHsgaWQ6ICdncmF2ZVdpc3AnLCBuYW1lOiAnR3JhdmUgV2lzcCcsIGdseXBoOiAndycsIGNvbG9yOiAnI2UwYzlmMCcsIGhlYWx0aDogMTQsIGF0dGFjazogMTUsIGRlZmVuc2U6IDE1LCBzcGVlZDogMTQ1LCBhaTogJ3dhbmRlcicsIHhwOiA2NywgYmlvbWU6ICdidXJpYWwnLCB0YWdzOiBbJ2J1cmlhbCcsICdzcGlyaXQnLCAnbGlnaHQnXSwgdGVycmFpbkFmZmluaXR5OiBbJ3NwaXJpdFBhdGgnXSB9LFxuICB7IGlkOiAnYmFycm93SG91bmQnLCBuYW1lOiAnQmFycm93IEhvdW5kJywgZ2x5cGg6ICdoJywgY29sb3I6ICcjOGY3OTZkJywgaGVhbHRoOiAyOCwgYXR0YWNrOiAxNSwgZGVmZW5zZTogMTksIHNwZWVkOiAxMDUsIGFpOiAnY2hhc2UnLCB4cDogODIsIGJpb21lOiAnYnVyaWFsJywgdGFnczogWydidXJpYWwnLCAnZ3JhdmUnLCAnZm9yY2UnXSB9LFxuICB7IGlkOiAnbGFtZW50ZXInLCBuYW1lOiAnTGFtZW50ZXInLCBnbHlwaDogJ2wnLCBjb2xvcjogJyNkNmI0ZTUnLCBoZWFsdGg6IDIwLCBhdHRhY2s6IDE2LCBkZWZlbnNlOiAxNywgc3BlZWQ6IDk1LCBhaTogJ3JhbmdlZCcsIHhwOiA4NCwgYmlvbWU6ICdidXJpYWwnLCB0YWdzOiBbJ2J1cmlhbCcsICdzcGlyaXQnLCAndGVsZWdyYXBoJ10gfSxcbiAgeyBpZDogJ2JhcnJvd0tpbmcnLCBuYW1lOiAnVGhlIEJhcnJvdyBLaW5nJywgZ2x5cGg6ICdCJywgY29sb3I6ICcjZjBkNWEwJywgaGVhbHRoOiAxMjgsIGF0dGFjazogMTksIGRlZmVuc2U6IDI1LCBzcGVlZDogMTAwLCBhaTogJ2d1YXJkaWFuJywgeHA6IDMxMCwgYmlvbWU6ICdidXJpYWwnLCB0YWdzOiBbJ2J1cmlhbCcsICdncmF2ZScsICdzcGlyaXQnXSB9LFxuICB7IGlkOiAnc2FsdFJhaWRlcicsIG5hbWU6ICdTYWx0IFJhaWRlcicsIGdseXBoOiAncicsIGNvbG9yOiAnI2YwZDg4OScsIGhlYWx0aDogMjMsIGF0dGFjazogMTUsIGRlZmVuc2U6IDE4LCBzcGVlZDogMTM1LCBhaTogJ2NoYXNlJywgeHA6IDc2LCBiaW9tZTogJ3NhbHRGbGF0cycsIHRhZ3M6IFsnc2FsdEZsYXRzJywgJ3NhbHQnLCAnbW9iaWxpdHknXSB9LFxuICB7IGlkOiAnbWlyYWdlU2tpcm1pc2hlcicsIG5hbWU6ICdNaXJhZ2UgU2tpcm1pc2hlcicsIGdseXBoOiAnbScsIGNvbG9yOiAnI2M1ZTdlZScsIGhlYWx0aDogMTgsIGF0dGFjazogMTYsIGRlZmVuc2U6IDE2LCBzcGVlZDogMTI1LCBhaTogJ3JhbmdlZCcsIHhwOiA4MCwgYmlvbWU6ICdzYWx0RmxhdHMnLCB0YWdzOiBbJ3NhbHRGbGF0cycsICdtaXJyb3InLCAnZGlzcGxhY2VtZW50JywgJ3RlbGVncmFwaCddIH0sXG4gIHsgaWQ6ICdicmluZVN0YWxrZXInLCBuYW1lOiAnQnJpbmUgU3RhbGtlcicsIGdseXBoOiAnYicsIGNvbG9yOiAnIzc2YmFjMCcsIGhlYWx0aDogMjYsIGF0dGFjazogMTQsIGRlZmVuc2U6IDIwLCBzcGVlZDogMTA1LCBhaTogJ3dhbmRlcicsIHhwOiA3OCwgYmlvbWU6ICdzYWx0RmxhdHMnLCB0YWdzOiBbJ3NhbHRGbGF0cycsICdicmluZScsICdhbWJ1c2gnXSB9LFxuICB7IGlkOiAnZ2xhc3NDdXR0ZXInLCBuYW1lOiAnR2xhc3MgQ3V0dGVyJywgZ2x5cGg6ICdnJywgY29sb3I6ICcjZjRmMWM5JywgaGVhbHRoOiAyMCwgYXR0YWNrOiAxNiwgZGVmZW5zZTogMTcsIHNwZWVkOiAxMTUsIGFpOiAncmFuZ2VkJywgeHA6IDgzLCBiaW9tZTogJ3NhbHRGbGF0cycsIHRhZ3M6IFsnc2FsdEZsYXRzJywgJ21pcnJvcicsICdkYXJ0JywgJ3RlbGVncmFwaCddIH0sXG4gIHsgaWQ6ICdzYWx0U292ZXJlaWduJywgbmFtZTogJ1RoZSBTYWx0IFNvdmVyZWlnbicsIGdseXBoOiAnUycsIGNvbG9yOiAnI2ZmZjJiMCcsIGhlYWx0aDogMTM4LCBhdHRhY2s6IDIxLCBkZWZlbnNlOiAyNiwgc3BlZWQ6IDExMCwgYWk6ICdndWFyZGlhbicsIHhwOiAzMzAsIGJpb21lOiAnc2FsdEZsYXRzJywgdGFnczogWydzYWx0RmxhdHMnLCAnc2FsdCcsICdtaXJyb3InLCAnZ3VhcmRpYW4nXSB9LFxuICB7IGlkOiAncmltZUR1ZWxpc3QnLCBuYW1lOiAnUmltZSBEdWVsaXN0JywgZ2x5cGg6ICdkJywgY29sb3I6ICcjYjhkY2YyJywgaGVhbHRoOiAyOSwgYXR0YWNrOiAxNywgZGVmZW5zZTogMjIsIHNwZWVkOiAxMDUsIGFpOiAnY2hhc2UnLCB4cDogODgsIGJpb21lOiAnZnJvc3RSZWxpcXVhcnknLCB0YWdzOiBbJ2Zyb3N0UmVsaXF1YXJ5JywgJ2Zyb3N0JywgJ2R1ZWwnXSB9LFxuICB7IGlkOiAnaWNlU2VudGluZWwnLCBuYW1lOiAnSWNlIFNlbnRpbmVsJywgZ2x5cGg6ICdpJywgY29sb3I6ICcjZDVmMWZmJywgaGVhbHRoOiAzNCwgYXR0YWNrOiAxNSwgZGVmZW5zZTogMjUsIHNwZWVkOiA3NSwgYWk6ICdjaGFzZScsIHhwOiA5MCwgYmlvbWU6ICdmcm9zdFJlbGlxdWFyeScsIHRhZ3M6IFsnZnJvc3RSZWxpcXVhcnknLCAnaWNlJywgJ2d1YXJkJywgJ2R1ZWwnXSB9LFxuICB7IGlkOiAnd2hpdGVvdXRPcmFjbGUnLCBuYW1lOiAnV2hpdGVvdXQgT3JhY2xlJywgZ2x5cGg6ICdvJywgY29sb3I6ICcjZThmNmZmJywgaGVhbHRoOiAyMSwgYXR0YWNrOiAxNywgZGVmZW5zZTogMTgsIHNwZWVkOiAxMDAsIGFpOiAncmFuZ2VkJywgeHA6IDkyLCBiaW9tZTogJ2Zyb3N0UmVsaXF1YXJ5JywgdGFnczogWydmcm9zdFJlbGlxdWFyeScsICdmcm9zdCcsICd0ZWxlZ3JhcGgnLCAncml0dWFsJ10gfSxcbiAgeyBpZDogJ3NoYXJkSG91bmQnLCBuYW1lOiAnU2hhcmQgSG91bmQnLCBnbHlwaDogJ2gnLCBjb2xvcjogJyM5ZGM3ZTInLCBoZWFsdGg6IDI3LCBhdHRhY2s6IDE2LCBkZWZlbnNlOiAxOSwgc3BlZWQ6IDEyNSwgYWk6ICdjaGFzZScsIHhwOiA4NiwgYmlvbWU6ICdmcm9zdFJlbGlxdWFyeScsIHRhZ3M6IFsnZnJvc3RSZWxpcXVhcnknLCAnaWNlJywgJ21vYmlsaXR5J10gfSxcbiAgeyBpZDogJ3JlbGlxdWFyeVdhcmRlbicsIG5hbWU6ICdUaGUgUmVsaXF1YXJ5IFdhcmRlbicsIGdseXBoOiAnUicsIGNvbG9yOiAnI2YwZmJmZicsIGhlYWx0aDogMTQ2LCBhdHRhY2s6IDIyLCBkZWZlbnNlOiAyOCwgc3BlZWQ6IDk1LCBhaTogJ2d1YXJkaWFuJywgeHA6IDM1MCwgYmlvbWU6ICdmcm9zdFJlbGlxdWFyeScsIHRhZ3M6IFsnZnJvc3RSZWxpcXVhcnknLCAnZnJvc3QnLCAnaWNlJywgJ2d1YXJkaWFuJywgJ2R1ZWwnXSB9XG5dXG5NT05TVEVSUy5mb3JFYWNoKG1vbnN0ZXIgPT4geyBtb25zdGVyLm5hbWUgPSBgJHtjb2xvbnlNb25zdGVyUHJlZml4W21vbnN0ZXIuYmlvbWVdfSAke3ZveWFnZXJOYW1lKG1vbnN0ZXIubmFtZSl9YCB9KVxuZXhwb3J0IGNvbnN0IE1PTlNURVIgPSBPYmplY3QuZnJvbUVudHJpZXMoTU9OU1RFUlMubWFwKG1vbnN0ZXIgPT4gW21vbnN0ZXIuaWQsIG1vbnN0ZXJdKSkgYXMgUmVjb3JkPHN0cmluZywgTW9uc3RlckRlZmluaXRpb24+XG5leHBvcnQgY29uc3QgbW9uc3RlckJ5SWQgPSAoaWQ6IHN0cmluZyk6IE1vbnN0ZXJEZWZpbml0aW9uIHwgdW5kZWZpbmVkID0+IE1PTlNURVJbaWRdXG5leHBvcnQgY29uc3QgaXNNb25zdGVySWQgPSAoaWQ6IHVua25vd24pOiBpZCBpcyBzdHJpbmcgPT4gdHlwZW9mIGlkID09PSAnc3RyaW5nJyAmJiBtb25zdGVyQnlJZChpZCkgIT09IHVuZGVmaW5lZFxuY29uc3QgdGFnZ2VkID0gKGRlZmluaXRpb246IE1vbnN0ZXJEZWZpbml0aW9uLCAuLi50YWdzOiBzdHJpbmdbXSk6IGJvb2xlYW4gPT4gdGFncy5zb21lKHRhZyA9PiBkZWZpbml0aW9uLnRhZ3M/LmluY2x1ZGVzKHRhZykpXG5leHBvcnQgY29uc3QgbW9uc3RlclJvbGVGb3IgPSAoZGVmaW5pdGlvbjogTW9uc3RlckRlZmluaXRpb24pOiBNb25zdGVyUm9sZSA9PiBkZWZpbml0aW9uLnJvbGVcbiAgPz8gKGRlZmluaXRpb24uYWkgPT09ICdndWFyZGlhbicgPyAnYXBleCdcbiAgICA6IHRhZ2dlZChkZWZpbml0aW9uLCAnd2FyZCcpID8gJ3N1cHBvcnQnXG4gICAgICA6IHRhZ2dlZChkZWZpbml0aW9uLCAncm9vdCcsICd3ZWInLCAnZGlzcGxhY2VtZW50JywgJ2RhcnQnLCAncml0dWFsJywgJ2xvY2snKSA/ICdjb250cm9sbGVyJ1xuICAgICAgICA6IHRhZ2dlZChkZWZpbml0aW9uLCAnYW1idXNoJykgPyAnYW1idXNoZXInXG4gICAgICAgICAgOiB0YWdnZWQoZGVmaW5pdGlvbiwgJ2d1YXJkJykgPyAnZ3VhcmQnXG4gICAgICAgICAgICA6IGRlZmluaXRpb24uYWkgPT09ICdyYW5nZWQnID8gJ2FydGlsbGVyeSdcbiAgICAgICAgICAgICAgOiBkZWZpbml0aW9uLmFpID09PSAnd2FuZGVyJyA/ICdzY2F2ZW5nZXInXG4gICAgICAgICAgICAgICAgOiBkZWZpbml0aW9uLnNwZWVkID49IDEyMCA/ICdza2lybWlzaGVyJ1xuICAgICAgICAgICAgICAgICAgOiAncHVyc3VlcicpXG5jb25zdCB0ZXJyYWluQnlUYWc6IEFycmF5PFtzdHJpbmcsIFRpbGVLaW5kW11dPiA9IFtcbiAgWydyYWlsJywgWydyYWlsJ11dLCBbJ3dhdGVyJywgWyd3YXRlcicsICdjdXJyZW50JywgJ2RlZXBXYXRlciddXSwgWyd3ZWInLCBbJ3dlYiddXSwgWydkYXJ0JywgWydkYXJ0J11dLCBbJ3dhcmQnLCBbJ2FsdGFyJ11dLCBbJ2N1cnJlbnQnLCBbJ2N1cnJlbnQnLCAnZGVlcFdhdGVyJ11dLCBbJ2dhcycsIFsnZ2FzJywgJ3Ntb2tlJ11dLCBbJ3Ntb2tlJywgWydzbW9rZScsICdnYXMnXV0sIFsnZmlyZScsIFsnZmlyZVZlbnQnLCAnbGF2YSddXSwgWydoZWF0JywgWydmaXJlVmVudCcsICdsYXZhJ11dLCBbJ2NsaW1iJywgWydsZWRnZSddXSwgWydncmF2ZScsIFsnZ3JhdmVTb2lsJywgJ3NwaXJpdFBhdGgnXV0sIFsnc3Bpcml0JywgWydzcGlyaXRQYXRoJ11dLCBbJ3NhbHQnLCBbJ3NhbHRNaXJyb3InLCAnYnJpbmUnXV0sIFsnbWlycm9yJywgWydzYWx0TWlycm9yJ11dLCBbJ2JyaW5lJywgWydicmluZSddXSwgWydmcm9zdCcsIFsnZnJvc3RSaW1lJywgJ2ljZSddXSwgWydpY2UnLCBbJ2ljZSddXSwgWydhbmNob3InLCBbJ2FuY2hvciddXSwgWydsaWZ0JywgWydsaWZ0J11dXG5dXG5leHBvcnQgY29uc3QgdGVycmFpbkFmZmluaXR5Rm9yID0gKGRlZmluaXRpb246IE1vbnN0ZXJEZWZpbml0aW9uKTogVGlsZUtpbmRbXSA9PiBkZWZpbml0aW9uLnRlcnJhaW5BZmZpbml0eSA/IFsuLi5kZWZpbml0aW9uLnRlcnJhaW5BZmZpbml0eV0gOiBBcnJheS5mcm9tKG5ldyBTZXQodGVycmFpbkJ5VGFnLmZpbHRlcigoW3RhZ10pID0+IHRhZ2dlZChkZWZpbml0aW9uLCB0YWcpKS5mbGF0TWFwKChbLCB0ZXJyYWluXSkgPT4gdGVycmFpbikpKVxuXG5leHBvcnQgY29uc3QgU0tJTExTOiBTa2lsbERlZmluaXRpb25bXSA9IFtcbiAgLi4uKFsnSXJvbiBHcmlwJywgJ0NsZWF2ZScsICdCcmVha2VyJywgJ0NvdW50ZXInLCAnVW5zdG9wcGFibGUnLCAnVGl0YW4nXSBhcyBjb25zdCkubWFwKChuYW1lLCBpKSA9PiAoeyBpZDogYHN0ciR7aSArIDF9YCwgbmFtZSwgc3RhdDogJ3N0cmVuZ3RoJyBhcyBTdGF0TmFtZSwgbGV2ZWw6IGkgKyAxLCB0ZXh0OiBbJ1N0cmVuZ3RoICsxLCBtZWxlZSBkYW1hZ2UgKzEnLCAnU3RyZW5ndGggKzEsIG1lbGVlIGRhbWFnZSArMScsICdTdHJlbmd0aCArMSwgYnJlYWsgcnViYmxlJywgJ1N0cmVuZ3RoICsxLCBndWFyZCAyIGRhbWFnZScsICdTdHJlbmd0aCArMSwgbWVsZWUga25vY2tiYWNrJywgJ1N0cmVuZ3RoICsxLCBtZWxlZSBkYW1hZ2UgKzInXVtpXSwgdGFnczogWydzdHJlbmd0aCddLCBwcmVyZXF1aXNpdGVzOiBpID8gW2BzdHIke2l9YF0gOiBbXSB9KSksXG4gIC4uLihbJ1F1aWNrIFN0ZXAnLCAnU3VyZSBBaW0nLCAnU2tpcm1pc2hlcicsICdFdmFzaW9uJywgJ0ZsZWV0JywgJ0dob3N0d2FsayddIGFzIGNvbnN0KS5tYXAoKG5hbWUsIGkpID0+ICh7IGlkOiBgYWdpJHtpICsgMX1gLCBuYW1lLCBzdGF0OiAnYWdpbGl0eScgYXMgU3RhdE5hbWUsIGxldmVsOiBpICsgMSwgdGV4dDogWydBZ2lsaXR5ICsxLCBtb3ZlICsxIGZsb29yIHRpbGUnLCAnQWdpbGl0eSArMSwgbWVsZWUgcmVhY2ggKzEnLCAnQWdpbGl0eSArMSwgZXZhZGUgdGVsZWdyYXBocyAyMCUnLCAnQWdpbGl0eSArMSwgZG9kZ2UgKzMnLCAnQWdpbGl0eSArMSwgbW92ZSArMSBmbG9vciB0aWxlJywgJ0FnaWxpdHkgKzEsIGV2YWRlIHRlbGVncmFwaHMgKzM1JSddW2ldLCB0YWdzOiBbJ2FnaWxpdHknXSwgcHJlcmVxdWlzaXRlczogaSA/IFtgYWdpJHtpfWBdIDogW10gfSkpLFxuICAuLi4oWydIYXJkeScsICdGb3JhZ2VyJywgJ1N0YWx3YXJ0JywgJ1JlY292ZXJ5JywgJ0lyb25ibG9vZCcsICdMYXN0IFN0YW5kJ10gYXMgY29uc3QpLm1hcCgobmFtZSwgaSkgPT4gKHsgaWQ6IGB2aXQke2kgKzF9YCwgbmFtZSwgc3RhdDogJ3ZpdGFsaXR5JyBhcyBTdGF0TmFtZSwgbGV2ZWw6IGkgKyAxLCB0ZXh0OiBbJ1ZpdGFsaXR5ICsxLCBtYXhpbXVtIGhlYWx0aCArMicsICdWaXRhbGl0eSArMSwgcmVjb3ZlcnkgKzEnLCAnVml0YWxpdHkgKzEsIHNoaWVsZCAxIGRhbWFnZScsICdWaXRhbGl0eSArMSwgcmVjb3ZlcnkgKzMnLCAnVml0YWxpdHkgKzEsIGhhemFyZHMgLTIgZGFtYWdlJywgJ1ZpdGFsaXR5ICsxLCByZXNjdWUgcmVjb3ZlcnkgKzYnXVtpXSwgdGFnczogWyd2aXRhbGl0eSddLCBwcmVyZXF1aXNpdGVzOiBpID8gW2B2aXQke2l9YF0gOiBbXSB9KSksXG4gIC4uLihbJ1NwYXJrJywgJ0luc2lnaHQnLCAnRmllbGQgVGhlb3Jpc3QnLCAnU3RhciBSZWFkZXInLCAnU2lnbmFsIFdhbGtlcicsICdSb3V0ZWZpbmRlciddIGFzIGNvbnN0KS5tYXAoKG5hbWUsIGkpID0+ICh7IGlkOiBgaW50JHtpICsgMX1gLCBuYW1lLCBzdGF0OiAnaW50ZWxsZWN0JyBhcyBTdGF0TmFtZSwgbGV2ZWw6IGkgKyAxLCB0ZXh0OiBbJ0ludGVsbGVjdCArMSwgbW9kdWxlcyBjb3N0IDEgbGVzcycsICdJbnRlbGxlY3QgKzEsIGZvY3VzIHJlY292ZXJ5ICsxJywgJ0ludGVsbGVjdCArMSwgbW9kdWxlIHJhbmdlICsxJywgJ0ludGVsbGVjdCArMSwgc2hpZWxkcyBhYnNvcmIgMicsICdJbnRlbGxlY3QgKzEsIG1vZHVsZSByYW5nZSArMScsICdJbnRlbGxlY3QgKzEsIGZvY3VzIHJlY292ZXJ5ICsxLCBzdGFyIHJvdXRlcyddW2ldLCB0YWdzOiBbJ2ludGVsbGVjdCddLCBwcmVyZXF1aXNpdGVzOiBpID8gW2BpbnQke2l9YF0gOiBbXSB9KSlcbl1cbmNvbnN0IFNLSUxMID0gT2JqZWN0LmZyb21FbnRyaWVzKFNLSUxMUy5tYXAoc2tpbGwgPT4gW3NraWxsLmlkLCBza2lsbF0pKSBhcyBSZWNvcmQ8c3RyaW5nLCBTa2lsbERlZmluaXRpb24+XG5leHBvcnQgY29uc3QgaXNTa2lsbElkID0gKGlkOiB1bmtub3duKTogaWQgaXMgc3RyaW5nID0+IHR5cGVvZiBpZCA9PT0gJ3N0cmluZycgJiYgU0tJTExbaWRdICE9PSB1bmRlZmluZWRcblxuZXhwb3J0IGNvbnN0IGJpb21lRm9yRmxvb3IgPSAoaW5kZXg6IG51bWJlcik6IEJpb21lID0+IChbJ21pbmUnLCAnd2lsZHMnLCAnY2F2ZXJucycsICdydWlucycsICdmdXJuYWNlJywgJ2Zsb29kZWRSdWlucycsICdjbGlmZnMnLCAnYnVyaWFsJywgJ3NhbHRGbGF0cycsICdmcm9zdFJlbGlxdWFyeSddIGFzIGNvbnN0KVtNYXRoLmZsb29yKGluZGV4IC8gNCldXG5leHBvcnQgY29uc3QgYmlvbWVOYW1lOiBSZWNvcmQ8QmlvbWUsIHN0cmluZz4gPSB7IG1pbmU6ICdLZXN0cmVsIENvbG9ueScsIHdpbGRzOiAnVmVyZGFudCBDb2xvbnknLCBjYXZlcm5zOiAnUGVsYWdvcyBDb2xvbnknLCBydWluczogJ09yaXNvbiBDb2xvbnknLCBmdXJuYWNlOiAnSGVsaW9uIENvbG9ueScsIGZsb29kZWRSdWluczogJ05lcmlkYSBDb2xvbnknLCBjbGlmZnM6ICdBZXJpZSBDb2xvbnknLCBidXJpYWw6ICdNZW1vcmlhbCBDb2xvbnknLCBzYWx0RmxhdHM6ICdIYWxjeW9uIENvbG9ueScsIGZyb3N0UmVsaXF1YXJ5OiAnQm9yZWFsaXMgQ29sb255JyB9XG5leHBvcnQgY29uc3QgU0hPUF9TVE9DSzogUmVjb3JkPEJpb21lLCBJdGVtSWRbXT4gPSB7XG4gIG1pbmU6IFsndG9uaWMnLCAnYm9tYlBhY2snLCAncm9wZUJ1bmRsZScsICdhdWdlcicsICdncmFwcGxlTGluZScsICdwb3J0YWJsZVdpbmNoJywgJ3BpY2theGUnLCAnY2FwJywgJ2tleSddLFxuICB3aWxkczogWyd0b25pYycsICdtYWNoZXRlJywgJ2ZvY3VzVG9uaWMnLCAncm9vdCcsICd3YXRlclNjcmlwdCcsICdsdWxsJywgJ2Jvb3RzJywgJ2ZpcmVKYXInLCAnbWFwU2Nyb2xsJywgJ3JlZWRHbGlkZXInLCAnZ3JhcHBsZUxpbmUnLCAnYnJpZGdlS2l0JywgJ2NvcmRtYXJrVGFsaXNtYW4nLCAncmVlZHN0ZXBCb290cyddLFxuICBjYXZlcm5zOiBbJ2ZvY3VzVG9uaWMnLCAnbGFudGVybicsICdzcGVhcicsICdlbWJlcicsICdtZW5kJywgJ3NpZ2h0JywgJ2JsaW5rJywgJ3B1bGwnLCAnYmxpbmtSdW5lJywgJ3JlZWRHbGlkZXInXSxcbiAgcnVpbnM6IFsnbWFpbCcsICd3YXJkJywgJ3N1bmJsYWRlJywgJ2dhdGUnLCAnd2FyZFNjcmlwdCcsICdibGluaycsICdwdWxsJywgJ2tleSddLFxuICBmdXJuYWNlOiBbJ2NpbmRlclRvbmljJywgJ3Nvb3RGaWx0ZXInLCAnYnJlYWNoQ2hhcmdlJywgJ2JvcmVHZWwnLCAnbGlmdEtleScsICdzdGVhbUpldHBhY2snLCAncG9ydGFibGVXaW5jaCcsICdjaW5kZXJIYW1tZXInLCAnc21va2VLbmlmZScsICdsaWZ0SG9vaycsICdiZWxsb3dzU2hpZWxkJywgJ2NoYWluR3VhcmQnLCAnc21va2VNYXNrJ10sXG4gIGZsb29kZWRSdWluczogWydmbG9vZFNhbHQnLCAnYW5jaG9yU3Bvb2wnLCAnd2luZ2ZvaWwnLCAnY3VycmVudFJ1bmUnLCAnYnJpZGdlS2l0JywgJ2dyYXBwbGVMaW5lJywgJ3NhbHZhZ2VLaXQnLCAnYW5jaG9yQmxhZGUnLCAndGlkZUN1dHRlcicsICdhbmNob3JCdWNrbGVyJywgJ2N1cnJlbnRPcmInXSxcbiAgY2xpZmZzOiBbJ2NsaWZmU3Bvb2wnLCAnd2luZGhvb2snLCAnZ2FsZU1hbnRsZScsICd0aHVuZGVySmFyJywgJ3NreU1hcCcsICdncmFwcGxlTGluZScsICdyZWVkR2xpZGVyJ10sXG4gIGJ1cmlhbDogWydncmF2ZVNhbHQnLCAnYW5jZXN0b3JUb2tlbicsICd0b21iS2V5JywgJ2dyYXZlU2lja2xlJywgJ21vdXJuaW5nQmVsbCcsICd3YXJkJywgJ21lbmQnXSxcbiAgc2FsdEZsYXRzOiBbJ2ZvY3VzVG9uaWMnLCAndG9uaWMnLCAnZmlyZUphcicsICdibGluaycsICdwdWxsJywgJ21hcFNjcm9sbCcsICdicmlkZ2VLaXQnLCAncmVlZEdsaWRlcicsICdzdW5ibGFkZSddLFxuICBmcm9zdFJlbGlxdWFyeTogWydmb2N1c1RvbmljJywgJ3RvbmljJywgJ3dhcmQnLCAnbWVuZCcsICdzaWdodCcsICdibGluaycsICdncmFwcGxlTGluZScsICdwb3J0YWJsZVdpbmNoJywgJ21haWwnXVxufVxuXG5jb25zdCBpZFBhdHRlcm4gPSAvXlthLXpdW2EtekEtWjAtOV0qJC9cbmNvbnN0IHZhbGlkYXRlSWRzID0gKGxhYmVsOiBzdHJpbmcsIGRlZmluaXRpb25zOiBSZWFkb25seUFycmF5PHsgaWQ6IHN0cmluZyB9Pik6IHZvaWQgPT4ge1xuICBjb25zdCBpZHMgPSBuZXcgU2V0PHN0cmluZz4oKVxuICBmb3IgKGNvbnN0IGRlZmluaXRpb24gb2YgZGVmaW5pdGlvbnMpIHtcbiAgICBpZiAoIWlkUGF0dGVybi50ZXN0KGRlZmluaXRpb24uaWQpKSB0aHJvdyBuZXcgRXJyb3IoYGludmFsaWQgJHtsYWJlbH0gaWQ6ICR7ZGVmaW5pdGlvbi5pZH1gKVxuICAgIGlmIChpZHMuaGFzKGRlZmluaXRpb24uaWQpKSB0aHJvdyBuZXcgRXJyb3IoYGR1cGxpY2F0ZSAke2xhYmVsfSBpZDogJHtkZWZpbml0aW9uLmlkfWApXG4gICAgaWRzLmFkZChkZWZpbml0aW9uLmlkKVxuICB9XG59XG5cbmNvbnN0IHZhbGlkYXRlVGFncyA9IChsYWJlbDogc3RyaW5nLCBpZDogc3RyaW5nLCB0YWdzOiByZWFkb25seSBzdHJpbmdbXSB8IHVuZGVmaW5lZCwga25vd25UYWdzOiBSZWFkb25seVNldDxzdHJpbmc+KTogdm9pZCA9PiB7XG4gIGZvciAoY29uc3QgdGFnIG9mIHRhZ3MgPz8gW10pIGlmICgha25vd25UYWdzLmhhcyh0YWcpKSB0aHJvdyBuZXcgRXJyb3IoYGludmFsaWQgJHtsYWJlbH0gdGFnIG9uICR7aWR9OiAke3RhZ31gKVxufVxuXG5jb25zdCB2YWxpZGF0ZVByZXJlcXVpc2l0ZXMgPSAoc2tpbGxzOiByZWFkb25seSBTa2lsbERlZmluaXRpb25bXSk6IHZvaWQgPT4ge1xuICBjb25zdCBza2lsbElkcyA9IG5ldyBTZXQoc2tpbGxzLm1hcChza2lsbCA9PiBza2lsbC5pZCkpXG4gIGNvbnN0IHZpc2l0aW5nID0gbmV3IFNldDxzdHJpbmc+KClcbiAgY29uc3QgdmlzaXRlZCA9IG5ldyBTZXQ8c3RyaW5nPigpXG4gIGNvbnN0IHZpc2l0ID0gKHNraWxsOiBTa2lsbERlZmluaXRpb24pOiB2b2lkID0+IHtcbiAgICBpZiAodmlzaXRlZC5oYXMoc2tpbGwuaWQpKSByZXR1cm5cbiAgICBpZiAodmlzaXRpbmcuaGFzKHNraWxsLmlkKSkgdGhyb3cgbmV3IEVycm9yKGBjeWNsaWMgc2tpbGwgcHJlcmVxdWlzaXRlOiAke3NraWxsLmlkfWApXG4gICAgdmlzaXRpbmcuYWRkKHNraWxsLmlkKVxuICAgIGZvciAoY29uc3QgcHJlcmVxdWlzaXRlIG9mIHNraWxsLnByZXJlcXVpc2l0ZXMpIHtcbiAgICAgIGlmICghc2tpbGxJZHMuaGFzKHByZXJlcXVpc2l0ZSkpIHRocm93IG5ldyBFcnJvcihgdW5rbm93biBza2lsbCBwcmVyZXF1aXNpdGUgb24gJHtza2lsbC5pZH06ICR7cHJlcmVxdWlzaXRlfWApXG4gICAgICBpZiAocHJlcmVxdWlzaXRlID09PSBza2lsbC5pZCkgdGhyb3cgbmV3IEVycm9yKGBzZWxmLXJlZmVyZW5jaW5nIHNraWxsIHByZXJlcXVpc2l0ZTogJHtza2lsbC5pZH1gKVxuICAgICAgdmlzaXQoc2tpbGxzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gcHJlcmVxdWlzaXRlKSEpXG4gICAgfVxuICAgIHZpc2l0aW5nLmRlbGV0ZShza2lsbC5pZClcbiAgICB2aXNpdGVkLmFkZChza2lsbC5pZClcbiAgfVxuICBmb3IgKGNvbnN0IHNraWxsIG9mIHNraWxscykgdmlzaXQoc2tpbGwpXG59XG5jb25zdCB2YWxpZGF0ZVNjcmlwdHMgPSAoc2NyaXB0czogcmVhZG9ubHkgU2NyaXB0RGVmaW5pdGlvbltdLCBpdGVtczogcmVhZG9ubHkgSXRlbURlZmluaXRpb25bXSk6IHZvaWQgPT4ge1xuICBjb25zdCBpZHMgPSBuZXcgU2V0PHN0cmluZz4oKVxuICBjb25zdCBpdGVtSWRzID0gbmV3IFNldDxzdHJpbmc+KClcbiAgY29uc3QgdmFsaWRTaGFwZXM6IEFjdGlvblNoYXBlW10gPSBbJ2FkamFjZW50JywgJ2xpbmUnLCAnY29uZScsICdidXJzdCcsICdjcm9zcyddXG4gIGZvciAoY29uc3Qgc2NyaXB0IG9mIHNjcmlwdHMpIHtcbiAgICBpZiAoIXNjcmlwdC5pZCB8fCBpZHMuaGFzKHNjcmlwdC5pZCkgfHwgaXRlbUlkcy5oYXMoc2NyaXB0Lml0ZW1JZCkpIHRocm93IG5ldyBFcnJvcihgaW52YWxpZCBzY3JpcHQ6ICR7c2NyaXB0LmlkfWApXG4gICAgaWRzLmFkZChzY3JpcHQuaWQpXG4gICAgaXRlbUlkcy5hZGQoc2NyaXB0Lml0ZW1JZClcbiAgICBjb25zdCBpdGVtID0gaXRlbXMuZmluZChjdXJyZW50ID0+IGN1cnJlbnQuaWQgPT09IHNjcmlwdC5pdGVtSWQpXG4gICAgaWYgKCFpdGVtIHx8IGl0ZW0udXNlICE9PSAnc3BlbGwnIHx8IGl0ZW0uc3BlbGwgIT09IHNjcmlwdC5pZCkgdGhyb3cgbmV3IEVycm9yKGBpbnZhbGlkIHNjcmlwdCBpdGVtOiAke3NjcmlwdC5pdGVtSWR9YClcbiAgICBpZiAoIVsnZW1iZXInLCAndmVyZGFudCcsICdhc3RyYWwnXS5pbmNsdWRlcyhzY3JpcHQuc2Nob29sKSB8fCAhc2NyaXB0LnRhZ3MubGVuZ3RoIHx8ICFOdW1iZXIuaXNJbnRlZ2VyKHNjcmlwdC5mb2N1c0Nvc3QpIHx8IHNjcmlwdC5mb2N1c0Nvc3QgPCAxIHx8ICF2YWxpZFNoYXBlcy5pbmNsdWRlcyhzY3JpcHQuc2hhcGUpIHx8ICFOdW1iZXIuaXNJbnRlZ2VyKHNjcmlwdC5yYW5nZSkgfHwgc2NyaXB0LnJhbmdlIDwgMSB8fCBuZXcgU2V0KHNjcmlwdC51cGdyYWRlcykuc2l6ZSAhPT0gc2NyaXB0LnVwZ3JhZGVzLmxlbmd0aCB8fCAhc2NyaXB0LnVwZ3JhZGVzLmV2ZXJ5KHVwZ3JhZGUgPT4gWydmb2N1c0Nvc3QnLCAncmFuZ2UnLCAncG90ZW5jeSddLmluY2x1ZGVzKHVwZ3JhZGUpKSkgdGhyb3cgbmV3IEVycm9yKGBpbnZhbGlkIHNjcmlwdCBkZWZpbml0aW9uOiAke3NjcmlwdC5pZH1gKVxuICB9XG4gIGlmIChzY3JpcHRzLmxlbmd0aCAhPT0gaXRlbXMuZmlsdGVyKGl0ZW0gPT4gaXRlbS51c2UgPT09ICdzcGVsbCcpLmxlbmd0aCkgdGhyb3cgbmV3IEVycm9yKCdtaXNzaW5nIHNjcmlwdCBkZWZpbml0aW9uJylcbn1cblxuZXhwb3J0IGNvbnN0IHZhbGlkYXRlQ29udGVudCA9IChyZWdpc3RyeTogQ29udGVudFJlZ2lzdHJ5KTogdm9pZCA9PiB7XG4gIHZhbGlkYXRlSWRzKCdpdGVtJywgcmVnaXN0cnkuaXRlbXMpXG4gIHZhbGlkYXRlSWRzKCdtb25zdGVyJywgcmVnaXN0cnkubW9uc3RlcnMpXG4gIHZhbGlkYXRlSWRzKCdza2lsbCcsIHJlZ2lzdHJ5LnNraWxscylcbiAgY29uc3QgdGFncyA9IG5ldyBTZXQocmVnaXN0cnkudGFncylcbiAgZm9yIChjb25zdCBpdGVtIG9mIHJlZ2lzdHJ5Lml0ZW1zKSB7XG4gICAgdmFsaWRhdGVJdGVtUHJpY2UoaXRlbS52YWx1ZSlcbiAgICB2YWxpZGF0ZVRhZ3MoJ2l0ZW0nLCBpdGVtLmlkLCBpdGVtLnRhZ3MsIHRhZ3MpXG4gICAgdmFsaWRhdGVFcXVpcG1lbnRFZmZlY3RzKGl0ZW0uZWZmZWN0cywgaXRlbS5pZClcbiAgICBpZiAoaXRlbS51c2UgPT09ICdzcGVsbCcgJiYgIWl0ZW0uc3BlbGwpIHRocm93IG5ldyBFcnJvcihgc3BlbGwgaXRlbSBtaXNzaW5nIHNwZWxsIGlkOiAke2l0ZW0uaWR9YClcbiAgICBpZiAoaXRlbS51c2UgIT09ICdzcGVsbCcgJiYgaXRlbS5zcGVsbCkgdGhyb3cgbmV3IEVycm9yKGBub24tc3BlbGwgaXRlbSBoYXMgc3BlbGwgaWQ6ICR7aXRlbS5pZH1gKVxuICB9XG4gIHZhbGlkYXRlU2NyaXB0cyhyZWdpc3RyeS5zY3JpcHRzLCByZWdpc3RyeS5pdGVtcylcbiAgZm9yIChjb25zdCBtb25zdGVyIG9mIHJlZ2lzdHJ5Lm1vbnN0ZXJzKSB2YWxpZGF0ZVRhZ3MoJ21vbnN0ZXInLCBtb25zdGVyLmlkLCBtb25zdGVyLnRhZ3MsIHRhZ3MpXG4gIGZvciAoY29uc3Qgc2tpbGwgb2YgcmVnaXN0cnkuc2tpbGxzKSB2YWxpZGF0ZVRhZ3MoJ3NraWxsJywgc2tpbGwuaWQsIHNraWxsLnRhZ3MsIHRhZ3MpXG4gIGNvbnN0IGl0ZW1JZHMgPSBuZXcgU2V0KHJlZ2lzdHJ5Lml0ZW1zLm1hcChpdGVtID0+IGl0ZW0uaWQpKVxuICBmb3IgKGNvbnN0IFtiaW9tZSwgc3RvY2tdIG9mIE9iamVjdC5lbnRyaWVzKHJlZ2lzdHJ5LnNob3BTdG9jaykpIGZvciAoY29uc3QgaWQgb2Ygc3RvY2spIGlmICghaXRlbUlkcy5oYXMoaWQpKSB0aHJvdyBuZXcgRXJyb3IoYHVua25vd24gc2hvcCBpdGVtIGluICR7YmlvbWV9OiAke2lkfWApXG4gIHZhbGlkYXRlUHJlcmVxdWlzaXRlcyhyZWdpc3RyeS5za2lsbHMpXG59XG5cbmV4cG9ydCBjb25zdCBDT05URU5UOiBDb250ZW50UmVnaXN0cnkgPSB7IGl0ZW1zOiBJVEVNUywgbW9uc3RlcnM6IE1PTlNURVJTLCBza2lsbHM6IFNLSUxMUywgc2NyaXB0czogU0NSSVBUUywgdGFnczogQ09OVEVOVF9UQUdTLCBzaG9wU3RvY2s6IFNIT1BfU1RPQ0sgfVxudmFsaWRhdGVDb250ZW50KENPTlRFTlQpXG5cbmV4cG9ydCBjb25zdCBzaG9wU3RvY2sgPSAoYmlvbWU6IEJpb21lKTogSXRlbUlkW10gPT4gU0hPUF9TVE9DS1tiaW9tZV1cbiJdLCJtYXBwaW5ncyI6IkFBRUEsU0FBU0Esd0JBQXdCLFFBQThCLFdBQVc7QUFDMUUsU0FBU0MsaUJBQWlCLFFBQVEsa0JBQWtCO0FBNEJwRCxPQUFPLE1BQU1DLFlBQVksR0FBRyxDQUFDLFVBQVUsRUFBRSxTQUFTLEVBQUUsVUFBVSxFQUFFLFdBQVcsRUFBRSxNQUFNLEVBQUUsT0FBTyxFQUFFLFNBQVMsRUFBRSxPQUFPLEVBQUUsU0FBUyxFQUFFLGNBQWMsRUFBRSxRQUFRLEVBQUUsUUFBUSxFQUFFLFdBQVcsRUFBRSxnQkFBZ0IsRUFBRSxNQUFNLEVBQUUsV0FBVyxFQUFFLE9BQU8sRUFBRSxXQUFXLEVBQUUsTUFBTSxFQUFFLE9BQU8sRUFBRSxLQUFLLEVBQUUsVUFBVSxFQUFFLE9BQU8sRUFBRSxNQUFNLEVBQUUsS0FBSyxFQUFFLE9BQU8sRUFBRSxjQUFjLEVBQUUsVUFBVSxFQUFFLGFBQWEsRUFBRSxNQUFNLEVBQUUsTUFBTSxFQUFFLE1BQU0sRUFBRSxRQUFRLEVBQUUsVUFBVSxFQUFFLFVBQVUsRUFBRSxPQUFPLEVBQUUsTUFBTSxFQUFFLFFBQVEsRUFBRSxTQUFTLEVBQUUsV0FBVyxFQUFFLE1BQU0sRUFBRSxNQUFNLEVBQUUsT0FBTyxFQUFFLE1BQU0sRUFBRSxPQUFPLEVBQUUsUUFBUSxFQUFFLE1BQU0sRUFBRSxTQUFTLEVBQUUsT0FBTyxFQUFFLFNBQVMsRUFBRSxRQUFRLEVBQUUsTUFBTSxFQUFFLE9BQU8sRUFBRSxNQUFNLEVBQUUsT0FBTyxFQUFFLE9BQU8sRUFBRSxRQUFRLEVBQUUsTUFBTSxFQUFFLE9BQU8sRUFBRSxNQUFNLEVBQUUsUUFB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VUFBVTtFQUFFQyxNQUFNLEVBQUU7SUFBRUMsTUFBTSxFQUFFLEVBQUU7SUFBRUMsS0FBSyxFQUFFLENBQUM7SUFBRUMsS0FBSyxFQUFFLE1BQU07SUFBRUMsUUFBUSxFQUFFLENBQUM7SUFBRUMsSUFBSSxFQUFFLENBQUMsU0FBUyxFQUFFLFFBQVE7RUFBRSxDQUFDO0VBQUVDLEtBQUssRUFBRTtBQUFJLENBQUMsRUFDak07RUFBRVgsRUFBRSxFQUFFLGNBQWM7RUFBRUMsSUFBSSxFQUFFLGVBQWU7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsSUFBSSxFQUFFLFVBQVU7RUFBRUMsTUFBTSxFQUFFO0lBQUVDLE1BQU0sRUFBRSxFQUFFO0lBQUVDLEtBQUssRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxPQUFPO0lBQUVDLFFBQVEsRUFBRSxDQUFDO0lBQUVDLElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxNQUFNLEVBQUUsV0FBVztFQUFFLENBQUM7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUQsSUFBSSxFQUFFLENBQUMsU0FBUztBQUFFLENBQUMsRUFDbE87RUFBRVYsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsSUFBSSxFQUFFLFVBQVU7RUFBRUMsTUFBTSxFQUFFO0lBQUVDLE1BQU0sRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxVQUFVO0lBQUVDLFFBQVEsRUFBRSxDQUFDO0lBQUVDLElBQUksRUFBRSxDQUFDLE9BQU8sRUFBRSxPQUFPLEVBQUUsT0FBTztFQUFFLENBQUM7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUQsSUFBSSxFQUFFLENBQUMsU0FBUztBQUFFLENBQUMsRUFDM047RUFBRVYsRUFBRSxFQUFFLFVBQVU7RUFBRUMsSUFBSSxFQUFFLFdBQVc7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsSUFBSSxFQUFFLFVBQVU7RUFBRUMsTUFBTSxFQUFFO0lBQUVDLE1BQU0sRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxNQUFNO0lBQUVDLFFBQVEsRUFBRSxDQUFDO0lBQUVDLElBQUksRUFBRSxDQUFDLE1BQU0sRUFBRSxNQUFNLEVBQUUsT0FBTztFQUFFLENBQUM7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUQsSUFBSSxFQUFFLENBQUMsU0FBUztBQUFFLENBQUMsRUFDbE47RUFBRVYsRUFBRSxFQUFFLGFBQWE7RUFBRUMsSUFBSSxFQUFFLGNBQWM7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsSUFBSSxFQUFFLFVBQVU7RUFBRUMsTUFBTSxFQUFFO0lBQUVDLE1BQU0sRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxNQUFNO0lBQUVDLFFBQVEsRUFBRSxDQUFDO0lBQUVDLElBQUksRUFBRSxDQUFDLE9BQU8sRUFBRSxRQUFRLEVBQUUsT0FBTztFQUFFLENBQUM7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUQsSUFBSSxFQUFFLENBQUMsY0FBYztBQUFFLENBQUMsRUFDaE87RUFBRVYsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsSUFBSSxFQUFFLFVBQVU7RUFBRUMsTUFBTSxFQUFFO0lBQUVDLE1BQU0sRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxNQUFNO0lBQUVDLFFBQVEsRUFBRSxDQUFDO0lBQUVDLElBQUksRUFBRSxDQUFDLE9BQU8sRUFBRSxTQUFTLEVBQUUsTUFBTTtFQUFFLENBQUM7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUQsSUFBSSxFQUFFLENBQUMsY0FBYztBQUFFLENBQUMsRUFD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RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQU8sQ0FBQyxFQUM1RztFQUFFdkIsRUFBRSxFQUFFLE9BQU87RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQVEsQ0FBQyxFQUM1RztFQUFFdkIsRUFBRSxFQUFFLE1BQU07RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQU8sQ0FBQyxFQUN6RztFQUFFdkIsRUFBRSxFQUFFLGFBQWE7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQVEsQ0FBQyxFQUNqSDtFQUFFdkIsRUFBRSxFQUFFLE1BQU07RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQU8sQ0FBQyxFQUN6RztFQUFFdkIsRUFBRSxFQUFFLE9BQU87RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQVEsQ0FBQyxFQUM1RztFQUFFdkIsRUFBRSxFQUFFLE1BQU07RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQU8sQ0FBQyxFQUN6RztFQUFFdkIsRUFBRSxFQUFFLE1BQU07RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQU8sQ0FBQyxFQUN6RztFQUFFdkIsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQU8sQ0FBQyxFQUMvRztFQUFFdkIsRUFBRSxFQUFFLE1BQU07RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRVEsS0FBSyxFQUFFLEdBQUc7RUFBRVEsR0FBRyxFQUFFLE9BQU87RUFBRUksS0FBSyxFQUFFO0FBQU8sQ0FBQyxFQUN6RztFQUFFdkIsRUFBRSxFQUFFLFVBQVU7RUFBRUMsSUFBSSxFQUFFLFVBQVU7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsSUFBSSxFQUFFLFVBQVU7RUFBRUMsTUFBTSxFQUFFO0lBQUVDLE1BQU0sRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxDQUFDO0lBQUVDLEtBQUssRUFBRSxNQUFNO0lBQUVDLFFBQVEsRUFBRSxDQUFDO0lBQUVDLElBQUksRUFBRSxDQUFDLE1BQU0sRUFBRSxNQUFNLEVBQUUsT0FBTztFQUFFLENBQUM7RUFBRUMsS0FBSyxF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QUVDLEtBQUssRUFBRSxTQUFTO0VBQUVRLEtBQUssRUFBRSxFQUFFO0VBQUVRLEdBQUcsRUFBRSxLQUFLO0VBQUVULElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxPQUFPO0FBQUUsQ0FBQyxFQUNuSDtFQUFFVixFQUFFLEVBQUUsY0FBYztFQUFFQyxJQUFJLEVBQUUsZUFBZTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFUSxLQUFLLEVBQUUsR0FBRztFQUFFVyxRQUFRLEVBQUUsS0FBSztFQUFFWixJQUFJLEVBQUUsQ0FBQyxPQUFPO0FBQUUsQ0FBQyxFQUN6SDtFQUFFVixFQUFFLEVBQUUsYUFBYTtFQUFFQyxJQUFJLEVBQUUsY0FBYztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFUSxLQUFLLEVBQUUsR0FBRztFQUFFVyxRQUFRLEVBQUUsS0FBSztFQUFFWixJQUFJLEVBQUUsQ0FBQyxPQUFPO0FBQUUsQ0FBQyxFQUN2SDtFQUFFVixFQUFFLEVBQUUsV0FBVztFQUFFQyxJQUFJLEVBQUUsWUFBWTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFUSxLQUFLLEVBQUUsR0FBRztFQUFFVyxRQUFRLEVBQUUsS0FBSztFQUFFWixJQUFJLEVBQUUsQ0FBQyxPQUFPO0FBQUUsQ0FBQyxFQUNuSDtFQUFFVixFQUFFLEVBQUUsV0FBVztFQUFFQyxJQUFJLEVBQUUsWUFBWTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFUSxLQUFLLEVBQUUsR0FBRztFQUFFVyxRQUFRLEVBQUUsS0FBSztFQUFFWixJQUFJLEVBQUUsQ0FBQyxPQUFPO0FBQUUsQ0FBQyxDQUNwSDtBQUVELE1BQU1jLFlBQXNELEdBQUcsQ0FDN0QsQ0FBQyxTQUFTLEVBQUUsT0FBTyxDQUFDLEVBQUUsQ0FBQyxVQUFVLEVBQUUsT0FBTyxDQUFDLEVBQUUsQ0FBQyxRQUFRLEVBQUUsU0FBUyxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsTUFBTSxDQUFDLEVBQUUsQ0FBQyxVQUFVLEVBQUUsT0FBTyxDQUFDLEVBQUUsQ0FBQyxRQUFRLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxPQUFPLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxVQUFVLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxPQUFPLEVBQUUsS0FBSyxDQUFDLEVBQUUsQ0FBQyxPQUFPLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxLQUFLLEVBQUUsY0FBYyxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsTUFBTSxDQUFDLEVBQUUsQ0FBQyxVQUFVLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsU0FBUyxDQUFDLEVBQUUsQ0FBQyxPQUFPLEVBQUUsTUFBTSxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsV0FBVyxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsV0FBVyxDQUFDLEVBQUUsQ0FBQyxPQUFPLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxTQUFTLEVBQUUsV0FBVyxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsT0FBTyxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsUUFBUSxDQUFDLEVBQUUsQ0FBQyxLQUFLLEVBQUUsTUFBTSxDQUFDLEVBQUUsQ0FBQyxPQUFPLEVBQUUsVUFBVSxDQUFDLEVBQUUsQ0FBQyxVQUFVLEVBQUUsU0FBUyxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsU0FBUyxDQUFDLEVBQUUsQ0FBQyxVQUFVLEVBQUUsU0FBUyxDQUFDLEVBQUUsQ0FBQyxRQUFRLEVBQUUsV0FBVyxDQUFDLEVBQUUsQ0FBQyxNQUFNLEVBQUUsVUFBVSxDQUFDLENBQzNvQjtBQUNELE1BQU1DLFdBQVcsR0FBSXhCLElBQVksSUFBYXVCLFlBQVksQ0FBQ0UsTUFBTSxDQUFDLENBQUNDLE9BQU8sRUFBRSxDQUFDQyxNQUFNLEVBQUVDLFdBQVcsQ0FBQyxLQUFLRixPQUFPLENBQUNHLFVBQVUsQ0FBQ0YsTUFBTSxFQUFFQyxXQUFXLENBQUMsRUFBRTVCLElBQUksQ0FBQztBQUNwSkYsS0FBSyxDQUFDZ0MsT0FBTyxDQUFDQyxJQUFJLElBQUk7RUFBRUEsSUFBSSxDQUFDL0IsSUFBSSxHQUFHd0IsV0FBVyxDQUFDTyxJQUFJLENBQUMvQixJQUFJLENBQUM7QUFBQyxDQUFDLENBQUM7QUFFN0QsT0FBTyxNQUFNZ0MsSUFBSSxHQUFHQyxNQUFNLENBQUNDLFdBQVcsQ0FBQ3BDLEtBQUssQ0FBQ3FDLEdBQUcsQ0FBQ0osSUFBSSxJQUFJLENBQUNBLElBQUksQ0FBQ2hDLEVBQUUsRUFBRWdDLElBQUksQ0FBQyxDQUFDLENBQW1DO0FBQzVHLE9BQU8sTUFBTUssUUFBUSxHQUFJckMsRUFBVSxJQUFpQ2lDLElBQUksQ0FBQ2pDLEVBQUUsQ0FBQztBQUM1RSxPQUFPLE1BQU1zQyxRQUFRLEdBQUl0QyxFQUFXLElBQW1CLE9BQU9BLEVBQUUsS0FBSyxRQUFRLElBQUlxQyxRQUFRLENBQUNyQyxFQUFFLENBQUMsS0FBS3VDLFNBQVM7QUFDM0csT0FBTyxNQUFNQyxPQUEyQixHQUFHLENBQ3pDO0VBQUVDLE1BQU0sRUFBRSxPQUFPO0VBQUV6QyxFQUFFLEVBQUUsT0FBTztFQUFFMEMsTUFBTSxFQUFFLE9BQU87RUFBRWhDLElBQUksRUFBRSxDQUFDLE1BQU0sRUFBRSxRQUFRLENBQUM7RUFBRWlDLFNBQVMsRUFBRSxDQUFDO0VBQUVuQyxLQUFLLEVBQUUsTUFBTTtFQUFFb0MsS0FBSyxFQUFFLENBQUM7RUFBRUMsUUFBUSxFQUFFLENBQUMsU0FBUyxFQUFFLE9BQU87QUFBRSxDQUFDLEVBQ2xKO0VBQUVKLE1BQU0sRUFBRSxNQUFNO0VBQUV6QyxFQUFFLEVBQUUsTUFBTTtFQUFFMEMsTUFBTSxFQUFFLFNBQVM7RUFBRWhDLElBQUksRUFBRSxDQUFDLFNBQVMsQ0FBQztFQUFFaUMsU0FBUyxFQUFFLENBQUM7RUFBRW5DLEtBQUssRUFBRSxVQUFVO0VBQUVvQyxLQUFLLEVBQUUsQ0FBQztFQUFFQyxRQUFRLEVBQUUsQ0FBQyxTQUFTLEVBQUUsV0FBVztBQUFFLENBQUMsRUFDbko7RUFBRUosTUFBTSxFQUFFLE1BQU07RUFBRXpDLEVBQUUsRUFBRSxNQUFNO0VBQUUwQyxNQUFNLEVBQUUsU0FBUztFQUFFaEMsSUFBSSxFQUFFLENBQUMsTUFBTSxFQUFFLFNBQVMsQ0FBQztFQUFFaUMsU0FBUyxFQUFFLENBQUM7RUFBRW5DLEtBQUssRUFBRSxNQUFNO0VBQUVvQyxLQUFLLEVBQUUsQ0FBQztFQUFFQyxRQUFRLEVBQUUsQ0FBQyxTQUFTLEVBQUUsT0FBTztBQUFFLENBQUMsRUFDbko7RUFBRUosTUFBTSxFQUFFLGFBQWE7RUFBRXpDLEVBQUUsRUFBRSxPQUFPO0VBQUUwQyxNQUFNLEVBQUUsU0FBUztFQUFFaEMsSUFBSSxFQUFFLENBQUMsT0FBTyxFQUFFLFNBQVMsQ0FBQztFQUFFaUMsU0FBUyxFQUFFLENBQUM7RUFBRW5DLEtBQUssRUFBRSxNQUFNO0VBQUVvQyxLQUFLLEVBQUUsQ0FBQztFQUFFQyxRQUFRLEVBQUUsQ0FBQyxPQUFPLEVBQUUsU0FBUztBQUFFLENBQUMsRUFDNUo7RUFBRUosTUFBTSxFQUFFLE1BQU07RUFBRXpDLEVBQUUsRUFBRSxNQUFNO0VBQUUwQyxNQUFNLEVBQUUsU0FBUztFQUFFaEMsSUFBSSxFQUFFLENBQUMsVUFBVSxFQUFFLFNBQVMsQ0FBQztFQUFFaUMsU0FBUyxFQUFFLENBQUM7RUFBRW5DLEtBQUssRUFBRSxNQUFNO0VBQUVvQyxLQUFLLEVBQUUsQ0FBQztFQUFFQyxRQUFRLEVBQUUsQ0FBQyxTQUFTLEVBQUUsT0FBTztBQUFFLENBQUMsRUFDdko7RUFBRUosTUFBTSxFQUFFLE9BQU87RUFBRXpDLEVBQUUsRUFBRSxPQUFPO0VBQUUwQyxNQUFNLEVBQUUsUUFBUTtFQUFFaEMsSUFBSSxFQUFFLENBQUMsUUFBUSxDQUFDO0VBQUVpQyxTQUFTLEVBQUUsQ0FBQztFQUFFbkMsS0FBSyxFQUFFLE9BQU87RUFBRW9DLEtBQUssRUFBRSxDQUFDO0VBQUVDLFFBQVEsRUFBRSxDQUFDLE9BQU8sRUFBRSxXQUFXO0FBQUUsQ0FBQyxFQUM5STtFQUFFSixNQUFNLEVBQUUsT0FBTztFQUFFekMsRUFBRSxFQUFFLE9BQU87RUFBRTBDLE1BQU0sRUFBRSxRQUFRO0VBQUVoQyxJQUFJLEVBQUUsQ0FBQyxVQUFVLEVBQUUsVUFBVSxDQUFDO0VBQUVpQyxTQUFTLEVBQUUsQ0FBQztFQUFFbkMsS0FBSyxFQUFFLE1BQU07RUFBRW9DLEtBQUssRUFBRSxDQUFDO0VBQUVDLFFBQVEsRUFBRSxDQUFDLE9BQU8sRUFBRSxXQUFXO0FBQUUsQ0FBQyxFQUMzSjtFQUFFSixNQUFNLEVBQUUsTUFBTTtFQUFFekMsRUFBRSxFQUFFLE1BQU07RUFBRTBDLE1BQU0sRUFBRSxRQUFRO0VBQUVoQyxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsVUFBVSxDQUFDO0VBQUVpQyxTQUFTLEVBQUUsQ0FBQztFQUFFbkMsS0FBSyxFQUFFLE1BQU07RUFBRW9DLEtBQUssRUFBRSxDQUFDO0VBQUVDLFFBQVEsRUFBRSxDQUFDLFNBQVMsRUFBRSxPQUFPO0FBQUUsQ0FBQyxFQUNwSjtFQUFFSixNQUFNLEVBQUUsTUFBTTtFQUFFekMsRUFBRSxFQUFFLE1BQU07RUFBRTBDLE1BQU0sRUFBRSxRQUFRO0VBQUVoQyxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsU0FBUyxDQUFDO0VBQUVpQyxTQUFTLEVBQUUsQ0FBQztFQUFFbkMsS0FBSyxFQUFFLE1BQU07RUFBRW9DLEtBQUssRUFBRSxDQUFDO0VBQUVDLFFBQVEsRUFBRSxDQUFDLFNBQVMsRUFBRSxPQUFPO0FBQUUsQ0FBQyxFQUNuSjtFQUFFSixNQUFNLEVBQUUsWUFBWTtFQUFFekMsRUFBRSxFQUFFLE1BQU07RUFBRTBDLE1BQU0sRUFBRSxRQUFRO0VBQUVoQyxJQUFJLEVBQUUsQ0FBQyxNQUFNLENBQUM7RUFBRWlDLFNBQVMsRUFBRSxDQUFDO0VBQUVuQyxLQUFLLEVBQUUsVUFBVTtFQUFFb0MsS0FBSyxFQUFFLENBQUM7RUFBRUMsUUFBUSxFQUFFLENBQUMsU0FBUyxFQUFFLFdBQVc7QUFBRSxDQUFDLEVBQ3JKO0VBQUVKLE1BQU0sRUFBRSxNQUFNO0VBQUV6QyxFQUFFLEVBQUUsTUFBTTtFQUFFMEMsTUFBTSxFQUFFLFFBQVE7RUFBRWhDLElBQUksRUFBRSxDQUFDLFVBQVUsQ0FBQztFQUFFaUMsU0FBUyxFQUFFLENBQUM7RUFBRW5DLEtBQUssRUFBRSxNQUFNO0VBQUVvQyxLQUFLLEVBQUUsQ0FBQztFQUFFQyxRQUFRLEVBQUUsQ0FBQyxPQUFPLEVBQUUsV0FBVztBQUFFLENBQUMsQ0FDOUk7QUFDRCxPQUFPLE1BQU1DLGNBQWMsR0FBR1osTUFBTSxDQUFDQyxXQUFXLENBQUNLLE9BQU8sQ0FBQ0osR0FBRyxDQUFDVyxNQUFNLElBQUksQ0FBQ0EsTUFBTSxDQUFDTixNQUFNLEVBQUVNLE1BQU0sQ0FBQyxDQUFDLENBQXFDO0FBRXBJLE1BQU1DLG1CQUEwQyxHQUFHO0VBQUVDLElBQUksRUFBRSxTQUFTO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUVDLE9BQU8sRUFBRSxTQUFTO0VBQUVDLEtBQUssRUFBRSxRQUFRO0VBQUVDLE9BQU8sRUFBRSxRQUFRO0VBQUVDLFlBQVksRUFBRSxRQUFRO0VBQUVDLE1BQU0sRUFBRSxPQUFPO0VBQUVDLE1BQU0sRUFBRSxVQUFVO0VBQUVDLFNBQVMsRUFBRSxTQUFTO0VBQUVDLGNBQWMsRUFBRTtBQUFXLENBQUM7QUFFL1AsT0FBTyxNQUFNQyxRQUE2QixHQUFHLENBQzNDO0VBQUUzRCxFQUFFLEVBQUUsS0FBSztFQUFFQyxJQUFJLEVBQUUsV0FBVztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLENBQUM7RUFBRUMsTUFBTSxFQUFFLENBQUM7RUFBRTNDLE9BQU8sRUFBRSxDQUFDO0VBQUU0QyxLQUFLLEVBQUUsR0FBRztFQUFFQyxFQUFFLEVBQUUsT0FBTztFQUFFQyxFQUFFLEVBQUUsQ0FBQztFQUFFQyxLQUFLLEVBQUU7QUFBTyxDQUFDLEVBQy9JO0VBQUVqRSxFQUFFLEVBQUUsTUFBTTtFQUFFQyxJQUFJLEVBQUUsZ0JBQWdCO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsQ0FBQztFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxFQUFFO0VBQUVDLEVBQUUsRUFBRSxPQUFPO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFPLENBQUMsRUFDdEo7RUFBRWpFLEVBQUUsRUFBRSxRQUFRO0VBQUVDLElBQUksRUFBRSxrQkFBa0I7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxDQUFDO0VBQUVDLE1BQU0sRUFBRSxDQUFDO0VBQUUzQyxPQUFPLEVBQUUsQ0FBQztFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFO0FBQU8sQ0FBQyxFQUMzSjtFQUFFakUsRUFBRSxFQUFFLFFBQVE7RUFBRUMsSUFBSSxFQUFFLGlCQUFpQjtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLENBQUM7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsRUFBRTtFQUFFQyxFQUFFLEVBQUUsT0FBTztFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUU7QUFBTyxDQUFDLEVBQzFKO0VBQUVqRSxFQUFFLEVBQUUsU0FBUztFQUFFQyxJQUFJLEVBQUUsZUFBZTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLENBQUM7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsR0FBRztFQUFFQyxFQUFFLEVBQUUsUUFBUTtFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUU7QUFBTyxDQUFDLEVBQzNKO0VBQUVqRSxFQUFFLEVBQUUsV0FBVztFQUFFQyxJQUFJLEVBQUUsYUFBYTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLENBQUM7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsR0FBRztFQUFFQyxFQUFFLEVBQUUsT0FBTztFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUUsTUFBTTtFQUFFdkQsSUFBSSxFQUFFLENBQUMsTUFBTSxFQUFFLE1BQU07QUFBRSxDQUFDLEVBQ2xMO0VBQUVWLEVBQUUsRUFBRSxZQUFZO0VBQUVDLElBQUksRUFBRSxhQUFhO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxFQUFFO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxNQUFNO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxNQUFNLEVBQUUsV0FBVyxFQUFFLE9BQU8sRUFBRSxXQUFXO0FBQUUsQ0FBQyxFQUM5TTtFQUFFVixFQUFFLEVBQUUsU0FBUztFQUFFQyxJQUFJLEVBQUUscUJBQXFCO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxVQUFVO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFPLENBQUMsRUFDbks7RUFBRWpFLEVBQUUsRUFBRSxXQUFXO0VBQUVDLElBQUksRUFBRSxXQUFXO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsQ0FBQztFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxPQUFPO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDeEo7RUFBRWpFLEVBQUUsRUFBRSxNQUFNO0VBQUVDLElBQUksRUFBRSxXQUFXO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxPQUFPO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDcEo7RUFBRWpFLEVBQUUsRUFBRSxTQUFTO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLENBQUM7RUFBRTRDLEtBQUssRUFBRSxFQUFFO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDeko7RUFBRWpFLEVBQUUsRUFBRSxNQUFNO0VBQUVDLElBQUksRUFBRSxZQUFZO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsQ0FBQztFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDcko7RUFBRWpFLEVBQUUsRUFBRSxNQUFNO0VBQUVDLElBQUksRUFBRSxhQUFhO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxPQUFPO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDdEo7RUFBRWpFLEVBQUUsRUFBRSxZQUFZO0VBQUVDLElBQUksRUFBRSxhQUFhO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxFQUFFO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxPQUFPO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsTUFBTSxFQUFFLFdBQVc7QUFBRSxDQUFDLEVBQ2xNO0VBQUVWLEVBQUUsRUFBRSxhQUFhO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxPQUFPO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxPQUFPO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsT0FBTyxFQUFFLFVBQVU7QUFBRSxDQUFDLEVBQ3BNO0VBQUVWLEVBQUUsRUFBRSxXQUFXO0VBQUVDLElBQUksRUFBRSxZQUFZO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxFQUFFO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxPQUFPO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsS0FBSyxFQUFFLE9BQU8sRUFBRSxXQUFXO0FBQUUsQ0FBQyxFQUN4TTtFQUFFVixFQUFFLEVBQUUsZUFBZTtFQUFFQyxJQUFJLEVBQUUsZ0JBQWdCO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsQ0FBQztFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLENBQUM7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxPQUFPO0VBQUVDLEVBQUUsRUFBRSxDQUFDO0VBQUVDLEtBQUssRUFBRSxPQUFPO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsVUFBVSxDQUFDO0VBQUV3RCxLQUFLLEVBQUU7QUFBWSxDQUFDLEVBQ2hOO0VBQUVsRSxFQUFFLEVBQUUsV0FBVztFQUFFQyxJQUFJLEVBQUUsZ0JBQWdCO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsRUFBRTtFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxVQUFVO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDbEs7RUFBRWpFLEVBQUUsRUFBRSxTQUFTO0VBQUVDLElBQUksRUFBRSxpQkFBaUI7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxDQUFDO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEVBQUU7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFO0FBQVUsQ0FBQyxFQUM5SjtFQUFFakUsRUFBRSxFQUFFLE9BQU87RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxDQUFDO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxPQUFPLEVBQUUsVUFBVTtBQUFFLENBQUMsRUFDaE07RUFBRVYsRUFBRSxFQUFFLE1BQU07RUFBRUMsSUFBSSxFQUFFLFVBQVU7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxDQUFDO0VBQUVDLE1BQU0sRUFBRSxDQUFDO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxVQUFVLEVBQUUsTUFBTTtBQUFFLENBQUMsRUFDNUw7RUFBRVYsRUFBRSxFQUFFLE1BQU07RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxDQUFDO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEVBQUU7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxVQUFVLEVBQUUsV0FBVztBQUFFLENBQUMsRUFDcE07RUFBRVYsRUFBRSxFQUFFLE1BQU07RUFBRUMsSUFBSSxFQUFFLFdBQVc7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxDQUFDO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEVBQUU7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxPQUFPLEVBQUUsT0FBTztBQUFFLENBQUMsRUFDMUw7RUFBRVYsRUFBRSxFQUFFLFdBQVc7RUFBRUMsSUFBSSxFQUFFLGVBQWU7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxDQUFDO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxPQUFPLEVBQUUsV0FBVztBQUFFLENBQUMsRUFDek07RUFBRVYsRUFBRSxFQUFFLFNBQVM7RUFB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QyxNQUFNLEVBQUUsRUFBRTtFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDMUo7RUFBRWpFLEVBQUUsRUFBRSxPQUFPO0VBQUVDLElBQUksRUFBRSxhQUFhO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsRUFBRTtFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDeko7RUFBRWpFLEVBQUUsRUFBRSxTQUFTO0VBQUVDLElBQUksRUFBRSxhQUFhO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsRUFBRTtFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRTtBQUFRLENBQUMsRUFDM0o7RUFBRWpFLEVBQUUsRUFBRSxhQUFhO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxFQUFFO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxPQUFPO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsTUFBTSxFQUFFLFFBQVE7QUFBRSxDQUFDLEVBQ2pNO0VBQUVWLEVBQUUsRUFBRSxXQUFXO0VBQUVDLElBQUksRUFBRSxZQUFZO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxPQUFPO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsTUFBTSxFQUFFLFdBQVc7QUFBRSxDQUFDLEVBQ2pNO0VBQUVWLEVBQUUsRUFBRSxZQUFZO0VBQUVDLElBQUksRUFBRSxhQUFhO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsQ0FBQztFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxFQUFFO0VBQUVDLEVBQUUsRUFBRSxPQUFPO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxPQUFPO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsTUFBTSxFQUFFLGFBQWE7QUFBRSxDQUFDLEVBQ25NO0VBQUVWLEVBQUUsRUFBRSxXQUFXO0VBQUVDLElBQUksRUFBRSxlQUFlO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsRUFBRTtFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxFQUFFO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxPQUFPO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsUUFBUSxFQUFFLFdBQVc7QUFBRSxDQUFDLEVBQ3RNO0VBQUVWLEVBQUUsRUFBRSxRQUFRO0VBQUVDLElBQUksRUFBRSxrQkFBa0I7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFVBQVU7RUFBRUMsRUFBRSxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFO0FBQVEsQ0FBQyxFQUNsSztFQUFFakUsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxNQUFNLEVBQUUsTUFBTTtBQUFFLENBQUMsRUFDak07RUFBRVYsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxPQUFPLEVBQUUsVUFBVTtBQUFFLENBQUMsRUFDeE07RUFBRVYsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEVBQUU7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxNQUFNLEVBQUUsYUFBYTtBQUFFLENBQUMsRUFDeE07RUFBRVYsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEVBQUU7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxNQUFNLEVBQUUsV0FBVztBQUFFLENBQUMsRUFDdk07RUFBRVYsRUFBRSxFQUFFLFVBQVU7RUFBRUMsSUFBSSxFQUFFLFdBQVc7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEVBQUU7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxXQUFXLEVBQUUsT0FBTztBQUFFLENBQUMsRUFDbk07RUFBRVYsRUFBRSxFQUFFLFdBQVc7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEVBQUU7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXZELElBQUksRUFBRSxDQUFDLFNBQVMsRUFBRSxPQUFPLEVBQUUsV0FBVztBQUFFLENBQUMsRUFDdE07RUFBRVYsRUFBRSxFQUFFLFdBQVc7RUFBRUMsSUFBSSxFQUFFLGdCQUFnQjtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEdBQUc7RUFBRUMsTUFBTSxFQUFFLEVBQUU7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsR0FBRztFQUFFQyxFQUFFLEVBQUUsVUFBVTtFQUFFQyxFQUFFLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFdkQsSUFBSSxFQUFFLENBQUMsU0FBUyxFQUFFLE1BQU0sRUFBRSxXQUFXO0FBQUUsQ0FBQyxFQUM5TTtFQUFFVixFQUFFLEVBQUUsWUFBWTtFQUFFQyxJQUFJLEVBQUUsYUFBYTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLEVBQUU7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsR0FBRztFQUFFQyxFQUFFLEVBQUUsUUFBUTtFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUUsY0FBYztFQUFFdkQsSUFBSSxFQUFFLENBQUMsY0FBYyxFQUFFLE9BQU8sRUFBRSxTQUFTO0FBQUUsQ0FBQyxFQUNqTjtFQUFFVixFQUFFLEVBQUUsWUFBWTtFQUFFQyxJQUFJLEVBQUUsYUFBYTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLEVBQUU7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsRUFBRTtFQUFFQyxFQUFFLEVBQUUsT0FBTztFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUUsY0FBYztFQUFFdkQsSUFBSSxFQUFFLENBQUMsY0FBYyxFQUFFLFFBQVEsRUFBRSxPQUFPO0FBQUUsQ0FBQyxFQUM5TTtFQUFFVixFQUFFLEVBQUUsVUFBVTtFQUFFQyxJQUFJLEVBQUUsV0FBVztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLEVBQUU7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsRUFBRTtFQUFFQyxFQUFFLEVBQUUsUUFBUTtFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUUsY0FBYztFQUFFdkQsSUFBSSxFQUFFLENBQUMsY0FBYyxFQUFFLFNBQVMsRUFBRSxXQUFXO0FBQUUsQ0FBQyxFQUNoTjtFQUFFVixFQUFFLEVBQUUsWUFBWTtFQUFFQyxJQUFJLEVBQUUsWUFBWTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLEVBQUU7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsR0FBRztFQUFFQyxFQUFFLEVBQUUsT0FBTztFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUUsY0FBYztFQUFFdkQsSUFBSSxFQUFFLENBQUMsY0FBYyxFQUFFLE9BQU8sRUFBRSxPQUFPO0FBQUUsQ0FBQyxFQUM3TTtFQUFFVixFQUFFLEVBQUUsWUFBWTtFQUFFQyxJQUFJLEVBQUUsYUFBYTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLEVBQUU7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsRUFBRTtFQUFFQyxFQUFFLEVBQUUsT0FBTztFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUUsY0FBYztFQUFFdkQsSUFBSSxFQUFFLENBQUMsY0FBYyxFQUFFLFFBQVEsRUFBRSxhQUFhO0FBQUUsQ0FBQyxFQUNwTjtFQUFFVixFQUFFLEVBQUUsZUFBZTtFQUFFQyxJQUFJLEVBQUUsZ0JBQWdCO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEtBQUssRUFBRSxTQUFTO0VBQUV5RCxNQUFNLEVBQUUsRUFBRTtFQUFFQyxNQUFNLEVBQUUsRUFBRTtFQUFFM0MsT0FBTyxFQUFFLEVBQUU7RUFBRTRDLEtBQUssRUFBRSxHQUFHO0VBQUVDLEVBQUUsRUFBRSxRQUFRO0VBQUVDLEVBQUUsRUFBRSxFQUFFO0VBQUVDLEtBQUssRUFBRSxjQUFjO0VBQUV2RCxJQUFJLEVBQUUsQ0FBQyxjQUFjLEVBQUUsU0FBUyxFQUFFLGNBQWM7QUFBRSxDQUFDLEVBQzlOO0VBQUVWLEVBQUUsRUFBRSxlQUFlO0VBQUVDLElBQUksRUFBRSxvQkFBb0I7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxHQUFHO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFVBQVU7RUFBRUMsRUFBRSxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLGNBQWM7RUFBRXZELElBQUksRUFBRSxDQUFDLGNBQWMsRUFBRSxPQUFPLEVBQUUsUUFBUTtBQUFFLENBQUMsRUFDOU47RUFBRVYsRUFBRSxFQUFFLFdBQVc7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxNQUFNLEVBQUUsVUFBVTtBQUFFLENBQUMsRUFDbk07RUFBRVYsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxPQUFPLEVBQUUsTUFBTTtBQUFFLENBQUMsRUFDak07RUFBRVYsRUFBRSxFQUFFLFVBQVU7RUFBRUMsSUFBSSxFQUFFLFdBQVc7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxNQUFNLEVBQUUsV0FBVztBQUFFLENBQUMsRUFDbE07RUFBRVYsRUFBRSxFQUFFLGNBQWM7RUFBRUMsSUFBSSxFQUFFLGVBQWU7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEVBQUU7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxPQUFPLEVBQUUsYUFBYTtBQUFFLENBQUMsRUFDM007RUFBRVYsRUFBRSxFQUFFLFdBQVc7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxNQUFNLEVBQUUsT0FBTyxDQUFDO0VBQUV5RCxlQUFlLEVBQUUsQ0FBQyxPQUFPO0FBQUUsQ0FBQyxFQUM1TjtFQUFFbkUsRUFBRSxFQUFFLFVBQVU7RUFBRUMsSUFBSSxFQUFFLFdBQVc7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxNQUFNLEVBQUUsT0FBTztBQUFFLENBQUMsRUFDOUw7RUFBRVYsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxPQUFPLEVBQUUsYUFBYTtBQUFFLENBQUMsRUFDeE07RUFBRVYsRUFBRSxFQUFFLFlBQVk7RUFBRUMsSUFBSSxFQUFFLGFBQWE7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFFBQVE7RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxNQUFNLEVBQUUsV0FBVztBQUFFLENBQUMsRUFDdE07RUFBRVYsRUFBRSxFQUFFLFdBQVc7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxHQUFHO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLFVBQVU7RUFBRUMsRUFBRSxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxNQUFNLEVBQUUsT0FBTztBQUFFLENBQUMsRUFDcE07RUFBRVYsRUFBRSxFQUFFLFdBQVc7RUFBRUMsSUFBSSxFQUFFLFlBQVk7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRXlELE1BQU0sRUFBRSxFQUFFO0VBQUVDLE1BQU0sRUFBRSxFQUFFO0VBQUUzQyxPQUFPLEVBQUUsRUFBRTtFQUFFNEMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsRUFBRSxFQUFFLE9BQU87RUFBRUMsRUFBRSxFQUFFLEVBQUU7RUFBRUMsS0FBSyxFQUFFLFFBQVE7RUFBRXZELElBQUksRUFBRSxDQUFDLFFBQVEsRUFBRSxPQUFPO0FBQUUsQ0FBQyxFQUN2TDtFQUFFVixFQUFFLEVBQUUsY0FBYztFQUFFQyxJQUFJLEVBQUUsZUFBZTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLEVBQUU7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsRUFBRTtFQUFFQyxFQUFFLEVBQUUsT0FBTztFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUUsUUFBUTtFQUFFdkQsSUFBSSxFQUFFLENBQUMsUUFBUSxFQUFFLE9BQU8sRUFBRSxPQUFPO0FBQUUsQ0FBQyxFQUNyTTtFQUFFVixFQUFFLEVBQUUsU0FBUztFQUFFQyxJQUFJLEVBQUUsU0FBUztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztFQUFFeUQsTUFBTSxFQUFFLEVBQUU7RUFBRUMsTUFBTSxFQUFFLEVBQUU7RUFBRTNDLE9BQU8sRUFBRSxFQUFFO0VBQUU0QyxLQUFLLEVBQUUsRUFBRTtFQUFFQyxFQUFFLEVBQUUsUUFBUTtFQUFFQyxFQUFFLEVBQUUsRUFBRTtFQUFFQyxLQUFLLEVBQUUsUUFBUTtFQUFFdkQsSUFBSSxFQUFFLENBQUMsUUFBUSxFQUFFLFFBQVEsRUFBRSxXQUFXO0FBQUUsQ0FBQyxFQUNoTTtFQUFFVixFQUFFLEVBQUUsY0FBYztFQUFFQyxJQUFJLEVBQUUsZUFBZTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFQyxLQUFLLEVBQUUsU0FBUztF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