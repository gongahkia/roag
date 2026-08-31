// 2f8fcba75c7ef015eab82154c01d9fc283171769
import { ITEM } from './content';
const define = definition => definition;
export const PROP_DEFINITIONS = [define({
  id: 'mine.oreVein',
  biome: 'mine',
  name: 'ore vein',
  description: 'A dense seam with a brittle mineral sheen.',
  glyph: 'O',
  color: '#d2b56f',
  terrain: ['floor', 'rail', 'support'],
  tags: ['salvage', 'force'],
  hooks: ['operate', 'bomb', 'force', 'throw'],
  activationReward: 'rock',
  effectReward: 'rock'
}), define({
  id: 'mine.lanternPost',
  biome: 'mine',
  name: 'lantern post',
  description: 'A guttering lamp marks a worked passage.',
  glyph: 'i',
  color: '#ffe18a',
  terrain: ['floor', 'rail'],
  tags: ['light', 'fire', 'hazard'],
  hooks: ['operate', 'fire', 'water', 'hazard'],
  activationReward: 'ember',
  effectReward: 'rock'
}), define({
  id: 'mine.brokenCart',
  biome: 'mine',
  name: 'broken cart',
  description: 'A splintered cart blocks a worked rail.',
  glyph: 'C',
  color: '#c9a06e',
  terrain: ['rail'],
  tags: ['route', 'force', 'salvage'],
  hooks: ['operate', 'bomb', 'force', 'throw'],
  activationReward: 'ropeBundle',
  effectReward: 'rock'
}), define({
  id: 'mine.warningMarker',
  biome: 'mine',
  name: 'warning marker',
  description: 'A painted stake warns of unstable ground.',
  glyph: '!',
  color: '#f0b56a',
  terrain: ['floor', 'rail', 'support'],
  tags: ['warning', 'hazard'],
  hooks: ['operate', 'fire', 'throw', 'hazard'],
  activationReward: 'mapScroll',
  effectReward: 'rock'
}), define({
  id: 'mine.skullMarker',
  biome: 'mine',
  name: 'skull marker',
  description: 'A miner’s warning stares from the dust.',
  glyph: 'x',
  color: '#ddd5c2',
  terrain: ['floor', 'rail'],
  tags: ['warning', 'hazard'],
  hooks: ['operate', 'bomb', 'throw', 'hazard'],
  activationReward: 'tonic',
  effectReward: 'rock'
}), define({
  id: 'mine.discardedParcel',
  biome: 'mine',
  name: 'discarded parcel',
  description: 'A sealed bundle lies under a film of ash.',
  glyph: '?',
  color: '#d9c27e',
  terrain: ['floor', 'support'],
  tags: ['cache', 'salvage'],
  hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'],
  activationReward: 'ropeBundle',
  effectReward: 'key'
}), define({
  id: 'wilds.mushrooms',
  biome: 'wilds',
  name: 'mushrooms',
  description: 'A bright cluster grows through the leaf litter.',
  glyph: 'm',
  color: '#b6df8a',
  terrain: ['floor', 'web'],
  tags: ['growth', 'root', 'fire'],
  hooks: ['operate', 'fire', 'water', 'root', 'throw'],
  activationReward: 'tonic',
  effectReward: 'focusTonic'
}), define({
  id: 'wilds.danglingCharm',
  biome: 'wilds',
  name: 'dangling charm',
  description: 'A weathered charm twists from a low branch.',
  glyph: 'o',
  color: '#ca9fe4',
  terrain: ['floor', 'web'],
  tags: ['ritual', 'root'],
  hooks: ['operate', 'fire', 'root', 'throw'],
  activationReward: 'wardScript',
  effectReward: 'root'
}), define({
  id: 'wilds.birdNest',
  biome: 'wilds',
  name: 'bird nest',
  description: 'A woven nest rustles above the path.',
  glyph: 'n',
  color: '#d8bc82',
  terrain: ['floor', 'web'],
  tags: ['growth', 'warning'],
  hooks: ['operate', 'fire', 'throw', 'hazard'],
  activationReward: 'tonic',
  effectReward: 'ropeBundle'
}), define({
  id: 'wilds.rootShrine',
  biome: 'wilds',
  name: 'root shrine',
  description: 'Roots curl around an offering stone.',
  glyph: '+',
  color: '#86c064',
  terrain: ['floor', 'web'],
  tags: ['ritual', 'root', 'growth'],
  hooks: ['operate', 'fire', 'water', 'root', 'force'],
  activationReward: 'root',
  effectReward: 'mend'
}), define({
  id: 'wilds.lostParcel',
  biome: 'wilds',
  name: 'lost parcel',
  description: 'A courier’s bundle is caught beneath ferns.',
  glyph: '?',
  color: '#dfcc91',
  terrain: ['floor', 'web'],
  tags: ['cache', 'route'],
  hooks: ['operate', 'fire', 'throw', 'hazard'],
  activationReward: 'ropeBundle',
  effectReward: 'tonic'
}), define({
  id: 'wilds.rootArch',
  biome: 'wilds',
  name: 'root arch',
  description: 'A living arch frames an overgrown trail.',
  glyph: 'A',
  color: '#71a66d',
  terrain: ['floor', 'web'],
  tags: ['route', 'growth', 'root'],
  hooks: ['operate', 'fire', 'root', 'force', 'throw'],
  activationReward: 'machete',
  effectReward: 'root'
}), define({
  id: 'caverns.crystalCluster',
  biome: 'caverns',
  name: 'crystal cluster',
  description: 'Facets catch every trace of cave light.',
  glyph: '*',
  color: '#8ce5f2',
  terrain: ['floor', 'darkness'],
  tags: ['salvage', 'force', 'light'],
  hooks: ['operate', 'bomb', 'force', 'throw', 'hazard'],
  activationReward: 'sight',
  effectReward: 'rock'
}), define({
  id: 'caverns.glowingFungus',
  biome: 'caverns',
  name: 'glowing fungus',
  description: 'Blue fungus spills a cold local glow.',
  glyph: 'f',
  color: '#8fd6c2',
  terrain: ['floor', 'darkness'],
  tags: ['light', 'growth', 'water'],
  hooks: ['operate', 'fire', 'water', 'root', 'throw'],
  activationReward: 'focusTonic',
  effectReward: 'tonic'
}), define({
  id: 'caverns.barnacledShrine',
  biome: 'caverns',
  name: 'barnacled shrine',
  description: 'A salt-crusted shrine waits beside the tide.',
  glyph: '+',
  color: '#9cc9ce',
  terrain: ['floor', 'water', 'darkness'],
  tags: ['ritual', 'water'],
  hooks: ['operate', 'fire', 'water', 'force', 'hazard'],
  activationReward: 'waterScript',
  effectReward: 'wardScript'
}), define({
  id: 'caverns.brokenBoat',
  biome: 'caverns',
  name: 'broken boat',
  description: 'A half-sunk skiff is tangled in cave reeds.',
  glyph: 'b',
  color: '#c8a879',
  terrain: ['floor', 'water'],
  tags: ['route', 'water', 'salvage'],
  hooks: ['operate', 'bomb', 'water', 'force', 'throw'],
  activationReward: 'ropeBundle',
  effectReward: 'rock'
}), define({
  id: 'caverns.eelTunnel',
  biome: 'caverns',
  name: 'eel tunnel',
  description: 'A narrow black tunnel exhales brine.',
  glyph: 'e',
  color: '#91bd9f',
  terrain: ['floor', 'water', 'darkness'],
  tags: ['route', 'water', 'hazard'],
  hooks: ['operate', 'bomb', 'fire', 'force', 'hazard'],
  activationReward: 'pull',
  effectReward: 'waterScript'
}), define({
  id: 'caverns.sealedParcel',
  biome: 'caverns',
  name: 'sealed parcel',
  description: 'Wax seals survive beneath a crust of salt.',
  glyph: '?',
  color: '#e5d6a4',
  terrain: ['floor', 'darkness'],
  tags: ['cache', 'water'],
  hooks: ['operate', 'bomb', 'water', 'throw', 'hazard'],
  activationReward: 'key',
  effectReward: 'focusTonic'
}), define({
  id: 'ruins.brokenStatue',
  biome: 'ruins',
  name: 'broken statue',
  description: 'A stone guardian has fallen across old mosaic.',
  glyph: 'S',
  color: '#b9b0c1',
  terrain: ['floor', 'dart'],
  tags: ['route', 'salvage', 'force'],
  hooks: ['operate', 'bomb', 'force', 'throw', 'hazard'],
  activationReward: 'rock',
  effectReward: 'tonic'
}), define({
  id: 'ruins.ritualBrazier',
  biome: 'ruins',
  name: 'ritual brazier',
  description: 'Cold ash waits in a ring of warding marks.',
  glyph: 'B',
  color: '#e59b64',
  terrain: ['floor', 'altar'],
  tags: ['ritual', 'fire', 'hazard'],
  hooks: ['operate', 'fire', 'water', 'force', 'hazard', 'ward', 'gate'],
  activationReward: 'ember',
  effectReward: 'wardScript'
}), define({
  id: 'ruins.glyphTablet',
  biome: 'ruins',
  name: 'glyph tablet',
  description: 'A carved tablet records a warning in stone.',
  glyph: 'T',
  color: '#c6bad6',
  terrain: ['floor', 'dart'],
  tags: ['warning', 'ritual'],
  hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'],
  activationReward: 'mapScroll',
  effectReward: 'sight'
}), define({
  id: 'ruins.collapsedArch',
  biome: 'ruins',
  name: 'collapsed arch',
  description: 'A cracked arch leans over the passage.',
  glyph: 'A',
  color: '#a89fae',
  terrain: ['floor', 'dart'],
  tags: ['route', 'salvage', 'force'],
  hooks: ['operate', 'bomb', 'force', 'throw', 'hazard'],
  activationReward: 'pickaxe',
  effectReward: 'rock'
}), define({
  id: 'ruins.sealedCache',
  biome: 'ruins',
  name: 'sealed cache',
  description: 'Bronze clasps hold a cache shut.',
  glyph: '?',
  color: '#d8b363',
  terrain: ['floor', 'altar'],
  tags: ['cache', 'ritual'],
  hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'],
  activationReward: 'key',
  effectReward: 'sunseal'
}), define({
  id: 'ruins.monolith',
  biome: 'ruins',
  name: 'monolith',
  description: 'A black monolith hums with a warded pulse.',
  glyph: 'M',
  color: '#d2a4e8',
  terrain: ['floor', 'altar', 'dart'],
  tags: ['ritual', 'force', 'hazard'],
  hooks: ['operate', 'bomb', 'fire', 'force', 'hazard', 'ward', 'gate'],
  activationReward: 'ward',
  effectReward: 'gate'
}), define({
  id: 'furnace.bellows',
  biome: 'furnace',
  name: 'bellows',
  description: 'A soot-black bellows coughs heat into the passage.',
  glyph: 'B',
  color: '#e27b55',
  terrain: ['floor', 'smoke'],
  tags: ['fire', 'smoke', 'hazard'],
  hooks: ['operate', 'fire', 'water', 'force', 'hazard'],
  activationReward: 'sootFilter',
  effectReward: 'firecracker'
}), define({
  id: 'furnace.liftConsole',
  biome: 'furnace',
  name: 'lift console',
  description: 'A chain console controls a nearby freight lift.',
  glyph: 'L',
  color: '#e9c47e',
  terrain: ['floor', 'lift'],
  tags: ['lift', 'route', 'force'],
  hooks: ['operate', 'force', 'throw', 'hazard'],
  activationReward: 'liftKey',
  effectReward: 'chainGuard'
}), define({
  id: 'furnace.breakwall',
  biome: 'furnace',
  name: 'breakwall rig',
  description: 'A scored wall waits for a controlled breach.',
  glyph: '#',
  color: '#bc8266',
  terrain: ['floor', 'smoke'],
  tags: ['route', 'force', 'hazard'],
  hooks: ['operate', 'bomb', 'fire', 'force', 'throw'],
  activationReward: 'boreGel',
  effectReward: 'breachCharge'
}), define({
  id: 'furnace.cinderCache',
  biome: 'furnace',
  name: 'cinder cache',
  description: 'A heat-sealed cache crackles beneath the ash.',
  glyph: '?',
  color: '#ef9a62',
  terrain: ['floor', 'smoke'],
  tags: ['cache', 'fire', 'salvage'],
  hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'],
  activationReward: 'cinderTonic',
  effectReward: 'cinderHammer'
}), define({
  id: 'furnace.smokeStack',
  biome: 'furnace',
  name: 'smoke stack',
  description: 'A cracked stack spills smoke into an old route.',
  glyph: 'S',
  color: '#9ca1ad',
  terrain: ['floor', 'smoke'],
  tags: ['smoke', 'route', 'hazard'],
  hooks: ['operate', 'fire', 'water', 'force', 'hazard'],
  activationReward: 'smokeMask',
  effectReward: 'sootFilter'
}), define({
  id: 'furnace.forgeIdol',
  biome: 'furnace',
  name: 'forge idol',
  description: 'A brass idol gathers heat around a broken anvil.',
  glyph: '+',
  color: '#ffd070',
  terrain: ['floor', 'lift'],
  tags: ['ritual', 'fire', 'force'],
  hooks: ['operate', 'bomb', 'fire', 'water', 'force', 'hazard'],
  activationReward: 'smokeKnife',
  effectReward: 'bellowsShield'
}), define({
  id: 'floodedRuins.anchorPost',
  biome: 'floodedRuins',
  name: 'anchor post',
  description: 'A bronze post can hold a route against the tide.',
  glyph: 'A',
  color: '#79c3cc',
  terrain: ['floor', 'anchor', 'current'],
  tags: ['anchor', 'route', 'water'],
  hooks: ['operate', 'water', 'force', 'throw', 'hazard'],
  activationReward: 'anchorSpool',
  effectReward: 'anchorBuckler'
}), define({
  id: 'floodedRuins.floodgate',
  biome: 'floodedRuins',
  name: 'floodgate',
  description: 'A crusted gate divides two old water channels.',
  glyph: 'G',
  color: '#73b8c4',
  terrain: ['floor', 'current', 'anchor'],
  tags: ['water', 'current', 'route'],
  hooks: ['operate', 'water', 'force', 'bomb', 'hazard'],
  activationReward: 'currentRune',
  effectReward: 'currentOrb'
}), define({
  id: 'floodedRuins.sunkenCache',
  biome: 'floodedRuins',
  name: 'sunken cache',
  description: 'A lacquered cache rests under clear black water.',
  glyph: '?',
  color: '#a1dfe2',
  terrain: ['floor', 'water', 'current'],
  tags: ['cache', 'water', 'salvage'],
  hooks: ['operate', 'water', 'bomb', 'throw', 'hazard'],
  activationReward: 'floodSalt',
  effectReward: 'anchorBlade'
}), define({
  id: 'floodedRuins.tideShrine',
  biome: 'floodedRuins',
  name: 'tide shrine',
  description: 'A shrine’s bowl fills and drains with the current.',
  glyph: '+',
  color: '#a4e7e9',
  terrain: ['floor', 'water', 'anchor'],
  tags: ['ritual', 'water', 'anchor'],
  hooks: ['operate', 'water', 'force', 'ward', 'hazard'],
  activationReward: 'tideCutter',
  effectReward: 'wingfoil'
}), define({
  id: 'floodedRuins.currentBell',
  biome: 'floodedRuins',
  name: 'current bell',
  description: 'A submerged bell hums whenever the water shifts.',
  glyph: 'b',
  color: '#b2e5e5',
  terrain: ['floor', 'current', 'anchor'],
  tags: ['current', 'warning', 'water'],
  hooks: ['operate', 'water', 'force', 'throw', 'hazard'],
  activationReward: 'salvageKit',
  effectReward: 'currentRune'
}), define({
  id: 'floodedRuins.mossBridge',
  biome: 'floodedRuins',
  name: 'moss bridge',
  description: 'A rope bridge sags between tide-worn columns.',
  glyph: '=',
  color: '#82b6a1',
  terrain: ['floor', 'anchor', 'water'],
  tags: ['route', 'growth', 'water'],
  hooks: ['operate', 'fire', 'water', 'force', 'throw', 'hazard'],
  activationReward: 'wingfoil',
  effectReward: 'anchorSpool'
}), define({
  id: 'cliffs.ropeAnchor',
  biome: 'cliffs',
  name: 'rope anchor',
  description: 'A weathered iron ring marks a vertical route.',
  glyph: '⚓',
  color: '#d8b66f',
  terrain: ['floor', 'ledge', 'rope'],
  tags: ['route', 'climb', 'wind'],
  hooks: ['operate', 'force', 'wind', 'throw'],
  activationReward: 'cliffSpool',
  effectReward: 'grappleLine'
}), define({
  id: 'cliffs.windVane',
  biome: 'cliffs',
  name: 'wind vane',
  description: 'A bent vane sings with the crosswind.',
  glyph: 'V',
  color: '#a8c7ff',
  terrain: ['floor', 'ledge'],
  tags: ['warning', 'wind', 'force'],
  hooks: ['operate', 'wind', 'force', 'hazard'],
  activationReward: 'skyMap',
  effectReward: 'thunderJar'
}), define({
  id: 'cliffs.nestCache',
  biome: 'cliffs',
  name: 'nest cache',
  description: 'A bundle is wedged beneath a high nest.',
  glyph: '?',
  color: '#d8d1b7',
  terrain: ['floor', 'ledge'],
  tags: ['cache', 'climb', 'salvage'],
  hooks: ['operate', 'wind', 'throw', 'hazard'],
  activationReward: 'cliffSpool',
  effectReward: 'windhook'
}), define({
  id: 'cliffs.skyShrine',
  biome: 'cliffs',
  name: 'sky shrine',
  description: 'Stone ribbons point toward the open sky.',
  glyph: '+',
  color: '#c4e5f2',
  terrain: ['floor', 'ledge'],
  tags: ['ritual', 'wind', 'climb'],
  hooks: ['operate', 'wind', 'force', 'ward', 'hazard'],
  activationReward: 'gust',
  effectReward: 'galeMantle'
}), define({
  id: 'cliffs.crackedLedge',
  biome: 'cliffs',
  name: 'cracked ledge',
  description: 'A ledge flexes over a deep drop.',
  glyph: '=',
  color: '#8398b4',
  terrain: ['floor', 'ledge'],
  tags: ['route', 'climb', 'hazard'],
  hooks: ['operate', 'force', 'throw', 'hazard'],
  activationReward: 'ropeBundle',
  effectReward: 'thunderJar'
}), define({
  id: 'cliffs.signalFire',
  biome: 'cliffs',
  name: 'signal fire',
  description: 'Cold charcoal waits in a storm-bent brazier.',
  glyph: 'i',
  color: '#c9e7f7',
  terrain: ['floor', 'ledge'],
  tags: ['light', 'wind', 'fire'],
  hooks: ['operate', 'fire', 'wind', 'hazard'],
  activationReward: 'ember',
  effectReward: 'skyMap'
}), define({
  id: 'burial.cairnGate',
  biome: 'burial',
  name: 'cairn gate',
  description: 'Stacked stones seal a narrow barrow path.',
  glyph: 'A',
  color: '#b9aa94',
  terrain: ['floor', 'cairn'],
  tags: ['route', 'grave', 'force'],
  hooks: ['operate', 'force', 'bomb', 'spirit'],
  activationReward: 'tombKey',
  effectReward: 'graveSickle'
}), define({
  id: 'burial.funeralLantern',
  biome: 'burial',
  name: 'funeral lantern',
  description: 'A violet flame burns without oil.',
  glyph: 'i',
  color: '#c9a6db',
  terrain: ['floor', 'graveSoil', 'spiritPath'],
  tags: ['light', 'spirit', 'ritual'],
  hooks: ['operate', 'spirit', 'ward', 'hazard'],
  activationReward: 'ancestorToken',
  effectReward: 'mourningBell'
}), define({
  id: 'burial.ossuaryCache',
  biome: 'burial',
  name: 'ossuary cache',
  description: 'Bone clasps protect a sealed offering.',
  glyph: '?',
  color: '#d9d3c5',
  terrain: ['floor', 'ossuary'],
  tags: ['cache', 'grave', 'salvage'],
  hooks: ['operate', 'spirit', 'bomb', 'throw'],
  activationReward: 'graveSalt',
  effectReward: 'tombKey'
}), define({
  id: 'burial.graveBloom',
  biome: 'burial',
  name: 'grave bloom',
  description: 'Pale flowers thread through old soil.',
  glyph: '*',
  color: '#d8c1e7',
  terrain: ['floor', 'graveSoil'],
  tags: ['growth', 'grave', 'spirit'],
  hooks: ['operate', 'spirit', 'root', 'hazard'],
  activationReward: 'graveSalt',
  effectReward: 'ancestorToken'
}), define({
  id: 'burial.ancestorStone',
  biome: 'burial',
  name: 'ancestor stone',
  description: 'A carved name waits beneath soft lichen.',
  glyph: 'S',
  color: '#bba9d0',
  terrain: ['floor', 'cairn', 'spiritPath'],
  tags: ['ritual', 'spirit', 'grave'],
  hooks: ['operate', 'spirit', 'ward', 'force'],
  activationReward: 'mourningBell',
  effectReward: 'ward'
}), define({
  id: 'burial.sealedTomb',
  biome: 'burial',
  name: 'sealed tomb',
  description: 'Bronze seals bind a tomb door shut.',
  glyph: 'T',
  color: '#cba96f',
  terrain: ['floor', 'ossuary'],
  tags: ['cache', 'grave', 'hazard'],
  hooks: ['operate', 'bomb', 'spirit', 'throw'],
  activationReward: 'tombKey',
  effectReward: 'graveSickle'
}), define({
  id: 'saltFlats.mirageCairn',
  biome: 'saltFlats',
  name: 'mirage cairn',
  description: 'A salt cairn shifts whenever you look away.',
  glyph: '△',
  color: '#f1df9a',
  terrain: ['floor', 'saltMirror'],
  tags: ['salt', 'mirror', 'warning'],
  hooks: ['operate', 'force', 'throw', 'hazard'],
  activationReward: 'mapScroll',
  effectReward: 'focusTonic'
}), define({
  id: 'saltFlats.sunMirror',
  biome: 'saltFlats',
  name: 'sun mirror',
  description: 'A polished plate throws hard light across the flats.',
  glyph: '◇',
  color: '#fff1ae',
  terrain: ['floor', 'saltMirror'],
  tags: ['salt', 'mirror', 'light'],
  hooks: ['operate', 'fire', 'force', 'hazard'],
  activationReward: 'sight',
  effectReward: 'sunblade'
}), define({
  id: 'saltFlats.brineWell',
  biome: 'saltFlats',
  name: 'brine well',
  description: 'Black brine wells beneath a crust of white salt.',
  glyph: '≈',
  color: '#76bbc4',
  terrain: ['floor', 'brine'],
  tags: ['brine', 'salt', 'hazard'],
  hooks: ['operate', 'water', 'force', 'hazard'],
  activationReward: 'focusTonic',
  effectReward: 'tonic'
}), define({
  id: 'saltFlats.caravanHusk',
  biome: 'saltFlats',
  name: 'caravan husk',
  description: 'A stripped courier cart points toward a false horizon.',
  glyph: 'C',
  color: '#c7a76f',
  terrain: ['floor', 'crumble'],
  tags: ['route', 'salt', 'salvage'],
  hooks: ['operate', 'bomb', 'force', 'throw'],
  activationReward: 'ropeBundle',
  effectReward: 'bridgeKit'
}), define({
  id: 'saltFlats.glassMarker',
  biome: 'saltFlats',
  name: 'glass marker',
  description: 'A sun-bleached sign lists routes that no longer exist.',
  glyph: '!',
  color: '#d5eff0',
  terrain: ['floor', 'saltMirror'],
  tags: ['warning', 'mirror', 'salt'],
  hooks: ['operate', 'fire', 'throw', 'hazard'],
  activationReward: 'mapScroll',
  effectReward: 'blink'
}), define({
  id: 'saltFlats.whiteCache',
  biome: 'saltFlats',
  name: 'white cache',
  description: 'A waxed parcel lies buried in hard salt.',
  glyph: '?',
  color: '#f3ebc2',
  terrain: ['floor', 'saltMirror'],
  tags: ['cache', 'salt', 'salvage'],
  hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'],
  activationReward: 'tonic',
  effectReward: 'fireJar'
}), define({
  id: 'frostReliquary.duelBell',
  biome: 'frostReliquary',
  name: 'wind shelter',
  description: 'A low snow wall breaks the whiteout and marks a safe rest.',
  glyph: '∩',
  color: '#d8f1ff',
  terrain: ['floor'],
  tags: ['frost', 'route', 'warning'],
  hooks: ['operate', 'force', 'ward', 'hazard'],
  activationReward: 'ward',
  effectReward: 'focusTonic'
}), define({
  id: 'frostReliquary.rimeSarcophagus',
  biome: 'frostReliquary',
  name: 'trail marker',
  description: 'Blue pennants point from the shore toward the next shelter.',
  glyph: '!',
  color: '#b9dbea',
  terrain: ['floor', 'ice'],
  tags: ['frost', 'ice', 'warning'],
  hooks: ['operate', 'bomb', 'force', 'throw'],
  activationReward: 'mapScroll',
  effectReward: 'sight'
}), define({
  id: 'frostReliquary.iceForge',
  biome: 'frostReliquary',
  name: 'ice bridge',
  description: 'A lashed bridge spans a crack where the lake ice has pulled apart.',
  glyph: '=',
  color: '#aee6f4',
  terrain: ['floor', 'ice'],
  tags: ['frost', 'ice', 'route'],
  hooks: ['operate', 'fire', 'force', 'hazard'],
  activationReward: 'bridgeKit',
  effectReward: 'portableWinch'
}), define({
  id: 'frostReliquary.frozenCache',
  biome: 'frostReliquary',
  name: 'broken sled',
  description: 'A splintered sled offers a risky cache beside the pressure crack.',
  glyph: 's',
  color: '#e2f6ff',
  terrain: ['floor', 'frostRime'],
  tags: ['cache', 'frost', 'ice'],
  hooks: ['operate', 'fire', 'bomb', 'throw'],
  activationReward: 'tonic',
  effectReward: 'grappleLine'
}), define({
  id: 'frostReliquary.reliquaryWard',
  biome: 'frostReliquary',
  name: 'reliquary ward',
  description: 'A cold wardstone preserves a narrow dueling ground.',
  glyph: '◇',
  color: '#d2c7ff',
  terrain: ['floor', 'ice'],
  tags: ['frost', 'duel', 'ward'],
  hooks: ['operate', 'ward', 'force', 'hazard'],
  activationReward: 'wardScript',
  effectReward: 'mend'
}), define({
  id: 'frostReliquary.thawValve',
  biome: 'frostReliquary',
  name: 'thaw valve',
  description: 'A brass valve can flood the reliquary with freezing melt.',
  glyph: 'V',
  color: '#d9bf83',
  terrain: ['floor', 'frostRime'],
  tags: ['frost', 'ice', 'route'],
  hooks: ['operate', 'fire', 'water', 'force', 'hazard'],
  activationReward: 'portableWinch',
  effectReward: 'bridgeKit'
})];
export const PROP_IDS = PROP_DEFINITIONS.map(definition => definition.id);
const definitions = new Map(PROP_DEFINITIONS.map(definition => [definition.id, definition]));
export const propDefinition = id => {
  const definition = definitions.get(id);
  if (!definition) throw new Error(`missing prop definition: ${id}`);
  return definition;
};
export const propDefinitionsFor = biome => PROP_DEFINITIONS.filter(definition => definition.biome === biome);
export const propAt = (props, x, y) => props.find(prop => prop.x === x && prop.y === y && prop.state !== 'destroyed');
export const isBlockingProp = prop => (prop === null || prop === void 0 ? void 0 : prop.kind) === 'mine.brokenCart' && prop.state !== 'destroyed' || (prop === null || prop === void 0 ? void 0 : prop.kind) === 'wilds.rootArch' && prop.state !== 'activated' && prop.state !== 'destroyed' || (prop === null || prop === void 0 ? void 0 : prop.kind) === 'caverns.brokenBoat' && prop.state !== 'activated' && prop.state !== 'destroyed' || (prop === null || prop === void 0 ? void 0 : prop.kind) === 'caverns.eelTunnel' && prop.state === 'activated' || (prop === null || prop === void 0 ? void 0 : prop.kind) === 'ruins.collapsedArch' && prop.state !== 'activated' && prop.state !== 'destroyed';
export const isSightBlockingProp = prop => (prop === null || prop === void 0 ? void 0 : prop.kind) === 'caverns.crystalCluster' && prop.state !== 'activated' && prop.state !== 'destroyed' || (prop === null || prop === void 0 ? void 0 : prop.kind) === 'ruins.brokenStatue' && prop.state === 'activated';
export const isLineBlockingProp = prop => isBlockingProp(prop) || isSightBlockingProp(prop);
export const linePropBlocker = prop => isBlockingProp(prop) ? 'cart' : (prop === null || prop === void 0 ? void 0 : prop.kind) === 'caverns.crystalCluster' && isSightBlockingProp(prop) ? 'crystal' : isSightBlockingProp(prop) ? 'cover' : undefined;
export const propEffects = hooks => hooks.filter(hook => hook !== 'operate');
export const validatePropDefinitions = () => {
  const errors = [];
  if (PROP_DEFINITIONS.length !== 60) errors.push(`expected 60 prop definitions, found ${PROP_DEFINITIONS.length}`);
  const ids = new Set();
  for (const definition of PROP_DEFINITIONS) {
    if (ids.has(definition.id)) errors.push(`duplicate prop definition: ${definition.id}`);
    ids.add(definition.id);
    if (!definition.tags.length) errors.push(`missing tags: ${definition.id}`);
    if (!definition.hooks.includes('operate') || !propEffects(definition.hooks).length) errors.push(`incomplete hooks: ${definition.id}`);
    if (!ITEM[definition.activationReward] || !ITEM[definition.effectReward]) errors.push(`unknown reward: ${definition.id}`);
  }
  for (const biome of ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']) if (propDefinitionsFor(biome).length !== 6) errors.push(`expected 6 ${biome} props`);
  return errors;
};
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJJVEVNIiwiZGVmaW5lIiwiZGVmaW5pdGlvbiIsIlBST1BfREVGSU5JVElPTlMiLCJpZCIsImJpb21lIiwibmFtZSIsImRlc2NyaXB0aW9uIiwiZ2x5cGgiLCJjb2xvciIsInRlcnJhaW4iLCJ0YWdzIiwiaG9va3MiLCJhY3RpdmF0aW9uUmV3YXJkIiwiZWZmZWN0UmV3YXJkIiwiUFJPUF9JRFMiLCJtYXAiLCJkZWZpbml0aW9ucyIsIk1hcCIsInByb3BEZWZpbml0aW9uIiwiZ2V0IiwiRXJyb3IiLCJwcm9wRGVmaW5pdGlvbnNGb3IiLCJmaWx0ZXIiLCJwcm9wQXQiLCJwcm9wcyIsIngiLCJ5IiwiZmluZCIsInByb3AiLCJzdGF0ZSIsImlzQmxvY2tpbmdQcm9wIiwia2luZCIsImlzU2lnaHRCbG9ja2luZ1Byb3AiLCJpc0xpbmVCbG9ja2luZ1Byb3AiLCJsaW5lUHJvcEJsb2NrZXIiLCJ1bmRlZmluZWQiLCJwcm9wRWZmZWN0cyIsImhvb2siLCJ2YWxpZGF0ZVByb3BEZWZpbml0aW9ucyIsImVycm9ycyIsImxlbmd0aCIsInB1c2giLCJpZHMiLCJTZXQiLCJoYXMiLCJhZGQiLCJpbmNsdWRlcyJdLCJzb3VyY2VzIjpbInByb3BzLnRzIl0sInNvdXJjZXNDb250ZW50IjpbImltcG9ydCB7IElURU0gfSBmcm9tICcuL2NvbnRlbnQnXG5pbXBvcnQgdHlwZSB7IEJpb21lLCBJdGVtSWQsIFByb3AsIFByb3BFZmZlY3RLaW5kLCBQcm9wSG9vaywgUHJvcElkLCBQcm9wVGFnLCBUaWxlS2luZCB9IGZyb20gJy4vdHlwZXMnXG5cbmV4cG9ydCBpbnRlcmZhY2UgUHJvcERlZmluaXRpb24ge1xuICBpZDogUHJvcElkXG4gIGJpb21lOiBCaW9tZVxuICBuYW1lOiBzdHJpbmdcbiAgZGVzY3JpcHRpb246IHN0cmluZ1xuICBnbHlwaDogc3RyaW5nXG4gIGNvbG9yOiBzdHJpbmdcbiAgdGVycmFpbjogcmVhZG9ubHkgVGlsZUtpbmRbXVxuICB0YWdzOiByZWFkb25seSBQcm9wVGFnW11cbiAgaG9va3M6IHJlYWRvbmx5IFByb3BIb29rW11cbiAgYWN0aXZhdGlvblJld2FyZDogSXRlbUlkXG4gIGVmZmVjdFJld2FyZDogSXRlbUlkXG59XG5cbmNvbnN0IGRlZmluZSA9IChkZWZpbml0aW9uOiBQcm9wRGVmaW5pdGlvbik6IFByb3BEZWZpbml0aW9uID0+IGRlZmluaXRpb25cblxuZXhwb3J0IGNvbnN0IFBST1BfREVGSU5JVElPTlMgPSBbXG4gIGRlZmluZSh7IGlkOiAnbWluZS5vcmVWZWluJywgYmlvbWU6ICdtaW5lJywgbmFtZTogJ29yZSB2ZWluJywgZGVzY3JpcHRpb246ICdBIGRlbnNlIHNlYW0gd2l0aCBhIGJyaXR0bGUgbWluZXJhbCBzaGVlbi4nLCBnbHlwaDogJ08nLCBjb2xvcjogJyNkMmI1NmYnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3JhaWwnLCAnc3VwcG9ydCddLCB0YWdzOiBbJ3NhbHZhZ2UnLCAnZm9yY2UnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdib21iJywgJ2ZvcmNlJywgJ3Rocm93J10sIGFjdGl2YXRpb25SZXdhcmQ6ICdyb2NrJywgZWZmZWN0UmV3YXJkOiAncm9jaycgfSksXG4gIGRlZmluZSh7IGlkOiAnbWluZS5sYW50ZXJuUG9zdCcsIGJpb21lOiAnbWluZScsIG5hbWU6ICdsYW50ZXJuIHBvc3QnLCBkZXNjcmlwdGlvbjogJ0EgZ3V0dGVyaW5nIGxhbXAgbWFya3MgYSB3b3JrZWQgcGFzc2FnZS4nLCBnbHlwaDogJ2knLCBjb2xvcjogJyNmZmUxOGEnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3JhaWwnXSwgdGFnczogWydsaWdodCcsICdmaXJlJywgJ2hhemFyZCddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAnd2F0ZXInLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdlbWJlcicsIGVmZmVjdFJld2FyZDogJ3JvY2snIH0pLFxuICBkZWZpbmUoeyBpZDogJ21pbmUuYnJva2VuQ2FydCcsIGJpb21lOiAnbWluZScsIG5hbWU6ICdicm9rZW4gY2FydCcsIGRlc2NyaXB0aW9uOiAnQSBzcGxpbnRlcmVkIGNhcnQgYmxvY2tzIGEgd29ya2VkIHJhaWwuJywgZ2x5cGg6ICdDJywgY29sb3I6ICcjYzlhMDZlJywgdGVycmFpbjogWydyYWlsJ10sIHRhZ3M6IFsncm91dGUnLCAnZm9yY2UnLCAnc2FsdmFnZSddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAnZm9yY2UnLCAndGhyb3cnXSwgYWN0aXZhdGlvblJld2FyZDogJ3JvcGVCdW5kbGUnLCBlZmZlY3RSZXdhcmQ6ICdyb2NrJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdtaW5lLndhcm5pbmdNYXJrZXInLCBiaW9tZTogJ21pbmUnLCBuYW1lOiAnd2FybmluZyBtYXJrZXInLCBkZXNjcmlwdGlvbjogJ0EgcGFpbnRlZCBzdGFrZSB3YXJucyBvZiB1bnN0YWJsZSBncm91bmQuJywgZ2x5cGg6ICchJywgY29sb3I6ICcjZjBiNTZhJywgdGVycmFpbjogWydmbG9vcicsICdyYWlsJywgJ3N1cHBvcnQnXSwgdGFnczogWyd3YXJuaW5nJywgJ2hhemFyZCddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdtYXBTY3JvbGwnLCBlZmZlY3RSZXdhcmQ6ICdyb2NrJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdtaW5lLnNrdWxsTWFya2VyJywgYmlvbWU6ICdtaW5lJywgbmFtZTogJ3NrdWxsIG1hcmtlcicsIGRlc2NyaXB0aW9uOiAnQSBtaW5lcuKAmXMgd2FybmluZyBzdGFyZXMgZnJvbSB0aGUgZHVzdC4nLCBnbHlwaDogJ3gnLCBjb2xvcjogJyNkZGQ1YzInLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3JhaWwnXSwgdGFnczogWyd3YXJuaW5nJywgJ2hhemFyZCddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICd0b25pYycsIGVmZmVjdFJld2FyZDogJ3JvY2snIH0pLFxuICBkZWZpbmUoeyBpZDogJ21pbmUuZGlzY2FyZGVkUGFyY2VsJywgYmlvbWU6ICdtaW5lJywgbmFtZTogJ2Rpc2NhcmRlZCBwYXJjZWwnLCBkZXNjcmlwdGlvbjogJ0Egc2VhbGVkIGJ1bmRsZSBsaWVzIHVuZGVyIGEgZmlsbSBvZiBhc2guJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjZDljMjdlJywgdGVycmFpbjogWydmbG9vcicsICdzdXBwb3J0J10sIHRhZ3M6IFsnY2FjaGUnLCAnc2FsdmFnZSddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAnZmlyZScsICd0aHJvdycsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ3JvcGVCdW5kbGUnLCBlZmZlY3RSZXdhcmQ6ICdrZXknIH0pLFxuICBkZWZpbmUoeyBpZDogJ3dpbGRzLm11c2hyb29tcycsIGJpb21lOiAnd2lsZHMnLCBuYW1lOiAnbXVzaHJvb21zJywgZGVzY3JpcHRpb246ICdBIGJyaWdodCBjbHVzdGVyIGdyb3dzIHRocm91Z2ggdGhlIGxlYWYgbGl0dGVyLicsIGdseXBoOiAnbScsIGNvbG9yOiAnI2I2ZGY4YScsIHRlcnJhaW46IFsnZmxvb3InLCAnd2ViJ10sIHRhZ3M6IFsnZ3Jvd3RoJywgJ3Jvb3QnLCAnZmlyZSddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAnd2F0ZXInLCAncm9vdCcsICd0aHJvdyddLCBhY3RpdmF0aW9uUmV3YXJkOiAndG9uaWMnLCBlZmZlY3RSZXdhcmQ6ICdmb2N1c1RvbmljJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICd3aWxkcy5kYW5nbGluZ0NoYXJtJywgYmlvbWU6ICd3aWxkcycsIG5hbWU6ICdkYW5nbGluZyBjaGFybScsIGRlc2NyaXB0aW9uOiAnQSB3ZWF0aGVyZWQgY2hhcm0gdHdpc3RzIGZyb20gYSBsb3cgYnJhbmNoLicsIGdseXBoOiAnbycsIGNvbG9yOiAnI2NhOWZlNCcsIHRlcnJhaW46IFsnZmxvb3InLCAnd2ViJ10sIHRhZ3M6IFsncml0dWFsJywgJ3Jvb3QnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdmaXJlJywgJ3Jvb3QnLCAndGhyb3cnXSwgYWN0aXZhdGlvblJld2FyZDogJ3dhcmRTY3JpcHQnLCBlZmZlY3RSZXdhcmQ6ICdyb290JyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICd3aWxkcy5iaXJkTmVzdCcsIGJpb21lOiAnd2lsZHMnLCBuYW1lOiAnYmlyZCBuZXN0JywgZGVzY3JpcHRpb246ICdBIHdvdmVuIG5lc3QgcnVzdGxlcyBhYm92ZSB0aGUgcGF0aC4nLCBnbHlwaDogJ24nLCBjb2xvcjogJyNkOGJjODInLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3dlYiddLCB0YWdzOiBbJ2dyb3d0aCcsICd3YXJuaW5nJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnZmlyZScsICd0aHJvdycsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ3RvbmljJywgZWZmZWN0UmV3YXJkOiAncm9wZUJ1bmRsZScgfSksXG4gIGRlZmluZSh7IGlkOiAnd2lsZHMucm9vdFNocmluZScsIGJpb21lOiAnd2lsZHMnLCBuYW1lOiAncm9vdCBzaHJpbmUnLCBkZXNjcmlwdGlvbjogJ1Jvb3RzIGN1cmwgYXJvdW5kIGFuIG9mZmVyaW5nIHN0b25lLicsIGdseXBoOiAnKycsIGNvbG9yOiAnIzg2YzA2NCcsIHRlcnJhaW46IFsnZmxvb3InLCAnd2ViJ10sIHRhZ3M6IFsncml0dWFsJywgJ3Jvb3QnLCAnZ3Jvd3RoJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnZmlyZScsICd3YXRlcicsICdyb290JywgJ2ZvcmNlJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdyb290JywgZWZmZWN0UmV3YXJkOiAnbWVuZCcgfSksXG4gIGRlZmluZSh7IGlkOiAnd2lsZHMubG9zdFBhcmNlbCcsIGJpb21lOiAnd2lsZHMnLCBuYW1lOiAnbG9zdCBwYXJjZWwnLCBkZXNjcmlwdGlvbjogJ0EgY291cmllcuKAmXMgYnVuZGxlIGlzIGNhdWdodCBiZW5lYXRoIGZlcm5zLicsIGdseXBoOiAnPycsIGNvbG9yOiAnI2RmY2M5MScsIHRlcnJhaW46IFsnZmxvb3InLCAnd2ViJ10sIHRhZ3M6IFsnY2FjaGUnLCAncm91dGUnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdmaXJlJywgJ3Rocm93JywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAncm9wZUJ1bmRsZScsIGVmZmVjdFJld2FyZDogJ3RvbmljJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICd3aWxkcy5yb290QXJjaCcsIGJpb21lOiAnd2lsZHMnLCBuYW1lOiAncm9vdCBhcmNoJywgZGVzY3JpcHRpb246ICdBIGxpdmluZyBhcmNoIGZyYW1lcyBhbiBvdmVyZ3Jvd24gdHJhaWwuJywgZ2x5cGg6ICdBJywgY29sb3I6ICcjNzFhNjZkJywgdGVycmFpbjogWydmbG9vcicsICd3ZWInXSwgdGFnczogWydyb3V0ZScsICdncm93dGgnLCAncm9vdCddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAncm9vdCcsICdmb3JjZScsICd0aHJvdyddLCBhY3RpdmF0aW9uUmV3YXJkOiAnbWFjaGV0ZScsIGVmZmVjdFJld2FyZDogJ3Jvb3QnIH0pLFxuICBkZWZpbmUoeyBpZDogJ2NhdmVybnMuY3J5c3RhbENsdXN0ZXInLCBiaW9tZTogJ2NhdmVybnMnLCBuYW1lOiAnY3J5c3RhbCBjbHVzdGVyJywgZGVzY3JpcHRpb246ICdGYWNldHMgY2F0Y2ggZXZlcnkgdHJhY2Ugb2YgY2F2ZSBsaWdodC4nLCBnbHlwaDogJyonLCBjb2xvcjogJyM4Y2U1ZjInLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2RhcmtuZXNzJ10sIHRhZ3M6IFsnc2FsdmFnZScsICdmb3JjZScsICdsaWdodCddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAnZm9yY2UnLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdzaWdodCcsIGVmZmVjdFJld2FyZDogJ3JvY2snIH0pLFxuICBkZWZpbmUoeyBpZDogJ2NhdmVybnMuZ2xvd2luZ0Z1bmd1cycsIGJpb21lOiAnY2F2ZXJucycsIG5hbWU6ICdnbG93aW5nIGZ1bmd1cycsIGRlc2NyaXB0aW9uOiAnQmx1ZSBmdW5ndXMgc3BpbGxzIGEgY29sZCBsb2NhbCBnbG93LicsIGdseXBoOiAnZicsIGNvbG9yOiAnIzhmZDZjMicsIHRlcnJhaW46IFsnZmxvb3InLCAnZGFya25lc3MnXSwgdGFnczogWydsaWdodCcsICdncm93dGgnLCAnd2F0ZXInXSwgaG9va3M6IFsnb3BlcmF0ZScsICdmaXJlJywgJ3dhdGVyJywgJ3Jvb3QnLCAndGhyb3cnXSwgYWN0aXZhdGlvblJld2FyZDogJ2ZvY3VzVG9uaWMnLCBlZmZlY3RSZXdhcmQ6ICd0b25pYycgfSksXG4gIGRlZmluZSh7IGlkOiAnY2F2ZXJucy5iYXJuYWNsZWRTaHJpbmUnLCBiaW9tZTogJ2NhdmVybnMnLCBuYW1lOiAnYmFybmFjbGVkIHNocmluZScsIGRlc2NyaXB0aW9uOiAnQSBzYWx0LWNydXN0ZWQgc2hyaW5lIHdhaXRzIGJlc2lkZSB0aGUgdGlkZS4nLCBnbHlwaDogJysnLCBjb2xvcjogJyM5Y2M5Y2UnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3dhdGVyJywgJ2RhcmtuZXNzJ10sIHRhZ3M6IFsncml0dWFsJywgJ3dhdGVyJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnZmlyZScsICd3YXRlcicsICdmb3JjZScsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ3dhdGVyU2NyaXB0JywgZWZmZWN0UmV3YXJkOiAnd2FyZFNjcmlwdCcgfSksXG4gIGRlZmluZSh7IGlkOiAnY2F2ZXJucy5icm9rZW5Cb2F0JywgYmlvbWU6ICdjYXZlcm5zJywgbmFtZTogJ2Jyb2tlbiBib2F0JywgZGVzY3JpcHRpb246ICdBIGhhbGYtc3VuayBza2lmZiBpcyB0YW5nbGVkIGluIGNhdmUgcmVlZHMuJywgZ2x5cGg6ICdiJywgY29sb3I6ICcjYzhhODc5JywgdGVycmFpbjogWydmbG9vcicsICd3YXRlciddLCB0YWdzOiBbJ3JvdXRlJywgJ3dhdGVyJywgJ3NhbHZhZ2UnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdib21iJywgJ3dhdGVyJywgJ2ZvcmNlJywgJ3Rocm93J10sIGFjdGl2YXRpb25SZXdhcmQ6ICdyb3BlQnVuZGxlJywgZWZmZWN0UmV3YXJkOiAncm9jaycgfSksXG4gIGRlZmluZSh7IGlkOiAnY2F2ZXJucy5lZWxUdW5uZWwnLCBiaW9tZTogJ2NhdmVybnMnLCBuYW1lOiAnZWVsIHR1bm5lbCcsIGRlc2NyaXB0aW9uOiAnQSBuYXJyb3cgYmxhY2sgdHVubmVsIGV4aGFsZXMgYnJpbmUuJywgZ2x5cGg6ICdlJywgY29sb3I6ICcjOTFiZDlmJywgdGVycmFpbjogWydmbG9vcicsICd3YXRlcicsICdkYXJrbmVzcyddLCB0YWdzOiBbJ3JvdXRlJywgJ3dhdGVyJywgJ2hhemFyZCddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAnZmlyZScsICdmb3JjZScsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ3B1bGwnLCBlZmZlY3RSZXdhcmQ6ICd3YXRlclNjcmlwdCcgfSksXG4gIGRlZmluZSh7IGlkOiAnY2F2ZXJucy5zZWFsZWRQYXJjZWwnLCBiaW9tZTogJ2NhdmVybnMnLCBuYW1lOiAnc2VhbGVkIHBhcmNlbCcsIGRlc2NyaXB0aW9uOiAnV2F4IHNlYWxzIHN1cnZpdmUgYmVuZWF0aCBhIGNydXN0IG9mIHNhbHQuJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjZTVkNmE0JywgdGVycmFpbjogWydmbG9vcicsICdkYXJrbmVzcyddLCB0YWdzOiBbJ2NhY2hlJywgJ3dhdGVyJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnYm9tYicsICd3YXRlcicsICd0aHJvdycsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ2tleScsIGVmZmVjdFJld2FyZDogJ2ZvY3VzVG9uaWMnIH0pLFxuICBkZWZpbmUoeyBpZDogJ3J1aW5zLmJyb2tlblN0YXR1ZScsIGJpb21lOiAncnVpbnMnLCBuYW1lOiAnYnJva2VuIHN0YXR1ZScsIGRlc2NyaXB0aW9uOiAnQSBzdG9uZSBndWFyZGlhbiBoYXMgZmFsbGVuIGFjcm9zcyBvbGQgbW9zYWljLicsIGdseXBoOiAnUycsIGNvbG9yOiAnI2I5YjBjMScsIHRlcnJhaW46IFsnZmxvb3InLCAnZGFydCddLCB0YWdzOiBbJ3JvdXRlJywgJ3NhbHZhZ2UnLCAnZm9yY2UnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdib21iJywgJ2ZvcmNlJywgJ3Rocm93JywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAncm9jaycsIGVmZmVjdFJld2FyZDogJ3RvbmljJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdydWlucy5yaXR1YWxCcmF6aWVyJywgYmlvbWU6ICdydWlucycsIG5hbWU6ICdyaXR1YWwgYnJhemllcicsIGRlc2NyaXB0aW9uOiAnQ29sZCBhc2ggd2FpdHMgaW4gYSByaW5nIG9mIHdhcmRpbmcgbWFya3MuJywgZ2x5cGg6ICdCJywgY29sb3I6ICcjZTU5YjY0JywgdGVycmFpbjogWydmbG9vcicsICdhbHRhciddLCB0YWdzOiBbJ3JpdHVhbCcsICdmaXJlJywgJ2hhemFyZCddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAnd2F0ZXInLCAnZm9yY2UnLCAnaGF6YXJkJywgJ3dhcmQnLCAnZ2F0ZSddLCBhY3RpdmF0aW9uUmV3YXJkOiAnZW1iZXInLCBlZmZlY3RSZXdhcmQ6ICd3YXJkU2NyaXB0JyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdydWlucy5nbHlwaFRhYmxldCcsIGJpb21lOiAncnVpbnMnLCBuYW1lOiAnZ2x5cGggdGFibGV0JywgZGVzY3JpcHRpb246ICdBIGNhcnZlZCB0YWJsZXQgcmVjb3JkcyBhIHdhcm5pbmcgaW4gc3RvbmUuJywgZ2x5cGg6ICdUJywgY29sb3I6ICcjYzZiYWQ2JywgdGVycmFpbjogWydmbG9vcicsICdkYXJ0J10sIHRhZ3M6IFsnd2FybmluZycsICdyaXR1YWwnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdib21iJywgJ2ZpcmUnLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdtYXBTY3JvbGwnLCBlZmZlY3RSZXdhcmQ6ICdzaWdodCcgfSksXG4gIGRlZmluZSh7IGlkOiAncnVpbnMuY29sbGFwc2VkQXJjaCcsIGJpb21lOiAncnVpbnMnLCBuYW1lOiAnY29sbGFwc2VkIGFyY2gnLCBkZXNjcmlwdGlvbjogJ0EgY3JhY2tlZCBhcmNoIGxlYW5zIG92ZXIgdGhlIHBhc3NhZ2UuJywgZ2x5cGg6ICdBJywgY29sb3I6ICcjYTg5ZmFlJywgdGVycmFpbjogWydmbG9vcicsICdkYXJ0J10sIHRhZ3M6IFsncm91dGUnLCAnc2FsdmFnZScsICdmb3JjZSddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAnZm9yY2UnLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdwaWNrYXhlJywgZWZmZWN0UmV3YXJkOiAncm9jaycgfSksXG4gIGRlZmluZSh7IGlkOiAncnVpbnMuc2VhbGVkQ2FjaGUnLCBiaW9tZTogJ3J1aW5zJywgbmFtZTogJ3NlYWxlZCBjYWNoZScsIGRlc2NyaXB0aW9uOiAnQnJvbnplIGNsYXNwcyBob2xkIGEgY2FjaGUgc2h1dC4nLCBnbHlwaDogJz8nLCBjb2xvcjogJyNkOGIzNjMnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2FsdGFyJ10sIHRhZ3M6IFsnY2FjaGUnLCAncml0dWFsJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnYm9tYicsICdmaXJlJywgJ3Rocm93JywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAna2V5JywgZWZmZWN0UmV3YXJkOiAnc3Vuc2VhbCcgfSksXG4gIGRlZmluZSh7IGlkOiAncnVpbnMubW9ub2xpdGgnLCBiaW9tZTogJ3J1aW5zJywgbmFtZTogJ21vbm9saXRoJywgZGVzY3JpcHRpb246ICdBIGJsYWNrIG1vbm9saXRoIGh1bXMgd2l0aCBhIHdhcmRlZCBwdWxzZS4nLCBnbHlwaDogJ00nLCBjb2xvcjogJyNkMmE0ZTgnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2FsdGFyJywgJ2RhcnQnXSwgdGFnczogWydyaXR1YWwnLCAnZm9yY2UnLCAnaGF6YXJkJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnYm9tYicsICdmaXJlJywgJ2ZvcmNlJywgJ2hhemFyZCcsICd3YXJkJywgJ2dhdGUnXSwgYWN0aXZhdGlvblJld2FyZDogJ3dhcmQnLCBlZmZlY3RSZXdhcmQ6ICdnYXRlJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmdXJuYWNlLmJlbGxvd3MnLCBiaW9tZTogJ2Z1cm5hY2UnLCBuYW1lOiAnYmVsbG93cycsIGRlc2NyaXB0aW9uOiAnQSBzb290LWJsYWNrIGJlbGxvd3MgY291Z2hzIGhlYXQgaW50byB0aGUgcGFzc2FnZS4nLCBnbHlwaDogJ0InLCBjb2xvcjogJyNlMjdiNTUnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3Ntb2tlJ10sIHRhZ3M6IFsnZmlyZScsICdzbW9rZScsICdoYXphcmQnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdmaXJlJywgJ3dhdGVyJywgJ2ZvcmNlJywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAnc29vdEZpbHRlcicsIGVmZmVjdFJld2FyZDogJ2ZpcmVjcmFja2VyJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmdXJuYWNlLmxpZnRDb25zb2xlJywgYmlvbWU6ICdmdXJuYWNlJywgbmFtZTogJ2xpZnQgY29uc29sZScsIGRlc2NyaXB0aW9uOiAnQSBjaGFpbiBjb25zb2xlIGNvbnRyb2xzIGEgbmVhcmJ5IGZyZWlnaHQgbGlmdC4nLCBnbHlwaDogJ0wnLCBjb2xvcjogJyNlOWM0N2UnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2xpZnQnXSwgdGFnczogWydsaWZ0JywgJ3JvdXRlJywgJ2ZvcmNlJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnZm9yY2UnLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdsaWZ0S2V5JywgZWZmZWN0UmV3YXJkOiAnY2hhaW5HdWFyZCcgfSksXG4gIGRlZmluZSh7IGlkOiAnZnVybmFjZS5icmVha3dhbGwnLCBiaW9tZTogJ2Z1cm5hY2UnLCBuYW1lOiAnYnJlYWt3YWxsIHJpZycsIGRlc2NyaXB0aW9uOiAnQSBzY29yZWQgd2FsbCB3YWl0cyBmb3IgYSBjb250cm9sbGVkIGJyZWFjaC4nLCBnbHlwaDogJyMnLCBjb2xvcjogJyNiYzgyNjYnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3Ntb2tlJ10sIHRhZ3M6IFsncm91dGUnLCAnZm9yY2UnLCAnaGF6YXJkJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnYm9tYicsICdmaXJlJywgJ2ZvcmNlJywgJ3Rocm93J10sIGFjdGl2YXRpb25SZXdhcmQ6ICdib3JlR2VsJywgZWZmZWN0UmV3YXJkOiAnYnJlYWNoQ2hhcmdlJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmdXJuYWNlLmNpbmRlckNhY2hlJywgYmlvbWU6ICdmdXJuYWNlJywgbmFtZTogJ2NpbmRlciBjYWNoZScsIGRlc2NyaXB0aW9uOiAnQSBoZWF0LXNlYWxlZCBjYWNoZSBjcmFja2xlcyBiZW5lYXRoIHRoZSBhc2guJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjZWY5YTYyJywgdGVycmFpbjogWydmbG9vcicsICdzbW9rZSddLCB0YWdzOiBbJ2NhY2hlJywgJ2ZpcmUnLCAnc2FsdmFnZSddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAnZmlyZScsICd0aHJvdycsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ2NpbmRlclRvbmljJywgZWZmZWN0UmV3YXJkOiAnY2luZGVySGFtbWVyJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmdXJuYWNlLnNtb2tlU3RhY2snLCBiaW9tZTogJ2Z1cm5hY2UnLCBuYW1lOiAnc21va2Ugc3RhY2snLCBkZXNjcmlwdGlvbjogJ0EgY3JhY2tlZCBzdGFjayBzcGlsbHMgc21va2UgaW50byBhbiBvbGQgcm91dGUuJywgZ2x5cGg6ICdTJywgY29sb3I6ICcjOWNhMWFkJywgdGVycmFpbjogWydmbG9vcicsICdzbW9rZSddLCB0YWdzOiBbJ3Ntb2tlJywgJ3JvdXRlJywgJ2hhemFyZCddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAnd2F0ZXInLCAnZm9yY2UnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdzbW9rZU1hc2snLCBlZmZlY3RSZXdhcmQ6ICdzb290RmlsdGVyJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmdXJuYWNlLmZvcmdlSWRvbCcsIGJpb21lOiAnZnVybmFjZScsIG5hbWU6ICdmb3JnZSBpZG9sJywgZGVzY3JpcHRpb246ICdBIGJyYXNzIGlkb2wgZ2F0aGVycyBoZWF0IGFyb3VuZCBhIGJyb2tlbiBhbnZpbC4nLCBnbHlwaDogJysnLCBjb2xvcjogJyNmZmQwNzAnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2xpZnQnXSwgdGFnczogWydyaXR1YWwnLCAnZmlyZScsICdmb3JjZSddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAnZmlyZScsICd3YXRlcicsICdmb3JjZScsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ3Ntb2tlS25pZmUnLCBlZmZlY3RSZXdhcmQ6ICdiZWxsb3dzU2hpZWxkJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmbG9vZGVkUnVpbnMuYW5jaG9yUG9zdCcsIGJpb21lOiAnZmxvb2RlZFJ1aW5zJywgbmFtZTogJ2FuY2hvciBwb3N0JywgZGVzY3JpcHRpb246ICdBIGJyb256ZSBwb3N0IGNhbiBob2xkIGEgcm91dGUgYWdhaW5zdCB0aGUgdGlkZS4nLCBnbHlwaDogJ0EnLCBjb2xvcjogJyM3OWMzY2MnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2FuY2hvcicsICdjdXJyZW50J10sIHRhZ3M6IFsnYW5jaG9yJywgJ3JvdXRlJywgJ3dhdGVyJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnd2F0ZXInLCAnZm9yY2UnLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdhbmNob3JTcG9vbCcsIGVmZmVjdFJld2FyZDogJ2FuY2hvckJ1Y2tsZXInIH0pLFxuICBkZWZpbmUoeyBpZDogJ2Zsb29kZWRSdWlucy5mbG9vZGdhdGUnLCBiaW9tZTogJ2Zsb29kZWRSdWlucycsIG5hbWU6ICdmbG9vZGdhdGUnLCBkZXNjcmlwdGlvbjogJ0EgY3J1c3RlZCBnYXRlIGRpdmlkZXMgdHdvIG9sZCB3YXRlciBjaGFubmVscy4nLCBnbHlwaDogJ0cnLCBjb2xvcjogJyM3M2I4YzQnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2N1cnJlbnQnLCAnYW5jaG9yJ10sIHRhZ3M6IFsnd2F0ZXInLCAnY3VycmVudCcsICdyb3V0ZSddLCBob29rczogWydvcGVyYXRlJywgJ3dhdGVyJywgJ2ZvcmNlJywgJ2JvbWInLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdjdXJyZW50UnVuZScsIGVmZmVjdFJld2FyZDogJ2N1cnJlbnRPcmInIH0pLFxuICBkZWZpbmUoeyBpZDogJ2Zsb29kZWRSdWlucy5zdW5rZW5DYWNoZScsIGJpb21lOiAnZmxvb2RlZFJ1aW5zJywgbmFtZTogJ3N1bmtlbiBjYWNoZScsIGRlc2NyaXB0aW9uOiAnQSBsYWNxdWVyZWQgY2FjaGUgcmVzdHMgdW5kZXIgY2xlYXIgYmxhY2sgd2F0ZXIuJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjYTFkZmUyJywgdGVycmFpbjogWydmbG9vcicsICd3YXRlcicsICdjdXJyZW50J10sIHRhZ3M6IFsnY2FjaGUnLCAnd2F0ZXInLCAnc2FsdmFnZSddLCBob29rczogWydvcGVyYXRlJywgJ3dhdGVyJywgJ2JvbWInLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdmbG9vZFNhbHQnLCBlZmZlY3RSZXdhcmQ6ICdhbmNob3JCbGFkZScgfSksXG4gIGRlZmluZSh7IGlkOiAnZmxvb2RlZFJ1aW5zLnRpZGVTaHJpbmUnLCBiaW9tZTogJ2Zsb29kZWRSdWlucycsIG5hbWU6ICd0aWRlIHNocmluZScsIGRlc2NyaXB0aW9uOiAnQSBzaHJpbmXigJlzIGJvd2wgZmlsbHMgYW5kIGRyYWlucyB3aXRoIHRoZSBjdXJyZW50LicsIGdseXBoOiAnKycsIGNvbG9yOiAnI2E0ZTdlOScsIHRlcnJhaW46IFsnZmxvb3InLCAnd2F0ZXInLCAnYW5jaG9yJ10sIHRhZ3M6IFsncml0dWFsJywgJ3dhdGVyJywgJ2FuY2hvciddLCBob29rczogWydvcGVyYXRlJywgJ3dhdGVyJywgJ2ZvcmNlJywgJ3dhcmQnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICd0aWRlQ3V0dGVyJywgZWZmZWN0UmV3YXJkOiAnd2luZ2ZvaWwnIH0pLFxuICBkZWZpbmUoeyBpZDogJ2Zsb29kZWRSdWlucy5jdXJyZW50QmVsbCcsIGJpb21lOiAnZmxvb2RlZFJ1aW5zJywgbmFtZTogJ2N1cnJlbnQgYmVsbCcsIGRlc2NyaXB0aW9uOiAnQSBzdWJtZXJnZWQgYmVsbCBodW1zIHdoZW5ldmVyIHRoZSB3YXRlciBzaGlmdHMuJywgZ2x5cGg6ICdiJywgY29sb3I6ICcjYjJlNWU1JywgdGVycmFpbjogWydmbG9vcicsICdjdXJyZW50JywgJ2FuY2hvciddLCB0YWdzOiBbJ2N1cnJlbnQnLCAnd2FybmluZycsICd3YXRlciddLCBob29rczogWydvcGVyYXRlJywgJ3dhdGVyJywgJ2ZvcmNlJywgJ3Rocm93JywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAnc2FsdmFnZUtpdCcsIGVmZmVjdFJld2FyZDogJ2N1cnJlbnRSdW5lJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmbG9vZGVkUnVpbnMubW9zc0JyaWRnZScsIGJpb21lOiAnZmxvb2RlZFJ1aW5zJywgbmFtZTogJ21vc3MgYnJpZGdlJywgZGVzY3JpcHRpb246ICdBIHJvcGUgYnJpZGdlIHNhZ3MgYmV0d2VlbiB0aWRlLXdvcm4gY29sdW1ucy4nLCBnbHlwaDogJz0nLCBjb2xvcjogJyM4MmI2YTEnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2FuY2hvcicsICd3YXRlciddLCB0YWdzOiBbJ3JvdXRlJywgJ2dyb3d0aCcsICd3YXRlciddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAnd2F0ZXInLCAnZm9yY2UnLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICd3aW5nZm9pbCcsIGVmZmVjdFJld2FyZDogJ2FuY2hvclNwb29sJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdjbGlmZnMucm9wZUFuY2hvcicsIGJpb21lOiAnY2xpZmZzJywgbmFtZTogJ3JvcGUgYW5jaG9yJywgZGVzY3JpcHRpb246ICdBIHdlYXRoZXJlZCBpcm9uIHJpbmcgbWFya3MgYSB2ZXJ0aWNhbCByb3V0ZS4nLCBnbHlwaDogJ+KakycsIGNvbG9yOiAnI2Q4YjY2ZicsIHRlcnJhaW46IFsnZmxvb3InLCAnbGVkZ2UnLCAncm9wZSddLCB0YWdzOiBbJ3JvdXRlJywgJ2NsaW1iJywgJ3dpbmQnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdmb3JjZScsICd3aW5kJywgJ3Rocm93J10sIGFjdGl2YXRpb25SZXdhcmQ6ICdjbGlmZlNwb29sJywgZWZmZWN0UmV3YXJkOiAnZ3JhcHBsZUxpbmUnIH0pLFxuICBkZWZpbmUoeyBpZDogJ2NsaWZmcy53aW5kVmFuZScsIGJpb21lOiAnY2xpZmZzJywgbmFtZTogJ3dpbmQgdmFuZScsIGRlc2NyaXB0aW9uOiAnQSBiZW50IHZhbmUgc2luZ3Mgd2l0aCB0aGUgY3Jvc3N3aW5kLicsIGdseXBoOiAnVicsIGNvbG9yOiAnI2E4YzdmZicsIHRlcnJhaW46IFsnZmxvb3InLCAnbGVkZ2UnXSwgdGFnczogWyd3YXJuaW5nJywgJ3dpbmQnLCAnZm9yY2UnXSwgaG9va3M6IFsnb3BlcmF0ZScsICd3aW5kJywgJ2ZvcmNlJywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAnc2t5TWFwJywgZWZmZWN0UmV3YXJkOiAndGh1bmRlckphcicgfSksXG4gIGRlZmluZSh7IGlkOiAnY2xpZmZzLm5lc3RDYWNoZScsIGJpb21lOiAnY2xpZmZzJywgbmFtZTogJ25lc3QgY2FjaGUnLCBkZXNjcmlwdGlvbjogJ0EgYnVuZGxlIGlzIHdlZGdlZCBiZW5lYXRoIGEgaGlnaCBuZXN0LicsIGdseXBoOiAnPycsIGNvbG9yOiAnI2Q4ZDFiNycsIHRlcnJhaW46IFsnZmxvb3InLCAnbGVkZ2UnXSwgdGFnczogWydjYWNoZScsICdjbGltYicsICdzYWx2YWdlJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnd2luZCcsICd0aHJvdycsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ2NsaWZmU3Bvb2wnLCBlZmZlY3RSZXdhcmQ6ICd3aW5kaG9vaycgfSksXG4gIGRlZmluZSh7IGlkOiAnY2xpZmZzLnNreVNocmluZScsIGJpb21lOiAnY2xpZmZzJywgbmFtZTogJ3NreSBzaHJpbmUnLCBkZXNjcmlwdGlvbjogJ1N0b25lIHJpYmJvbnMgcG9pbnQgdG93YXJkIHRoZSBvcGVuIHNreS4nLCBnbHlwaDogJysnLCBjb2xvcjogJyNjNGU1ZjInLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2xlZGdlJ10sIHRhZ3M6IFsncml0dWFsJywgJ3dpbmQnLCAnY2xpbWInXSwgaG9va3M6IFsnb3BlcmF0ZScsICd3aW5kJywgJ2ZvcmNlJywgJ3dhcmQnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdndXN0JywgZWZmZWN0UmV3YXJkOiAnZ2FsZU1hbnRsZScgfSksXG4gIGRlZmluZSh7IGlkOiAnY2xpZmZzLmNyYWNrZWRMZWRnZScsIGJpb21lOiAnY2xpZmZzJywgbmFtZTogJ2NyYWNrZWQgbGVkZ2UnLCBkZXNjcmlwdGlvbjogJ0EgbGVkZ2UgZmxleGVzIG92ZXIgYSBkZWVwIGRyb3AuJywgZ2x5cGg6ICc9JywgY29sb3I6ICcjODM5OGI0JywgdGVycmFpbjogWydmbG9vcicsICdsZWRnZSddLCB0YWdzOiBbJ3JvdXRlJywgJ2NsaW1iJywgJ2hhemFyZCddLCBob29rczogWydvcGVyYXRlJywgJ2ZvcmNlJywgJ3Rocm93JywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAncm9wZUJ1bmRsZScsIGVmZmVjdFJld2FyZDogJ3RodW5kZXJKYXInIH0pLFxuICBkZWZpbmUoeyBpZDogJ2NsaWZmcy5zaWduYWxGaXJlJywgYmlvbWU6ICdjbGlmZnMnLCBuYW1lOiAnc2lnbmFsIGZpcmUnLCBkZXNjcmlwdGlvbjogJ0NvbGQgY2hhcmNvYWwgd2FpdHMgaW4gYSBzdG9ybS1iZW50IGJyYXppZXIuJywgZ2x5cGg6ICdpJywgY29sb3I6ICcjYzllN2Y3JywgdGVycmFpbjogWydmbG9vcicsICdsZWRnZSddLCB0YWdzOiBbJ2xpZ2h0JywgJ3dpbmQnLCAnZmlyZSddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAnd2luZCcsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ2VtYmVyJywgZWZmZWN0UmV3YXJkOiAnc2t5TWFwJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdidXJpYWwuY2Fpcm5HYXRlJywgYmlvbWU6ICdidXJpYWwnLCBuYW1lOiAnY2Fpcm4gZ2F0ZScsIGRlc2NyaXB0aW9uOiAnU3RhY2tlZCBzdG9uZXMgc2VhbCBhIG5hcnJvdyBiYXJyb3cgcGF0aC4nLCBnbHlwaDogJ0EnLCBjb2xvcjogJyNiOWFhOTQnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2NhaXJuJ10sIHRhZ3M6IFsncm91dGUnLCAnZ3JhdmUnLCAnZm9yY2UnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdmb3JjZScsICdib21iJywgJ3NwaXJpdCddLCBhY3RpdmF0aW9uUmV3YXJkOiAndG9tYktleScsIGVmZmVjdFJld2FyZDogJ2dyYXZlU2lja2xlJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdidXJpYWwuZnVuZXJhbExhbnRlcm4nLCBiaW9tZTogJ2J1cmlhbCcsIG5hbWU6ICdmdW5lcmFsIGxhbnRlcm4nLCBkZXNjcmlwdGlvbjogJ0EgdmlvbGV0IGZsYW1lIGJ1cm5zIHdpdGhvdXQgb2lsLicsIGdseXBoOiAnaScsIGNvbG9yOiAnI2M5YTZkYicsIHRlcnJhaW46IFsnZmxvb3InLCAnZ3JhdmVTb2lsJywgJ3NwaXJpdFBhdGgnXSwgdGFnczogWydsaWdodCcsICdzcGlyaXQnLCAncml0dWFsJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnc3Bpcml0JywgJ3dhcmQnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdhbmNlc3RvclRva2VuJywgZWZmZWN0UmV3YXJkOiAnbW91cm5pbmdCZWxsJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdidXJpYWwub3NzdWFyeUNhY2hlJywgYmlvbWU6ICdidXJpYWwnLCBuYW1lOiAnb3NzdWFyeSBjYWNoZScsIGRlc2NyaXB0aW9uOiAnQm9uZSBjbGFzcHMgcHJvdGVjdCBhIHNlYWxlZCBvZmZlcmluZy4nLCBnbHlwaDogJz8nLCBjb2xvcjogJyNkOWQzYzUnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ29zc3VhcnknXSwgdGFnczogWydjYWNoZScsICdncmF2ZScsICdzYWx2YWdlJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnc3Bpcml0JywgJ2JvbWInLCAndGhyb3cnXSwgYWN0aXZhdGlvblJld2FyZDogJ2dyYXZlU2FsdCcsIGVmZmVjdFJld2FyZDogJ3RvbWJLZXknIH0pLFxuICBkZWZpbmUoeyBpZDogJ2J1cmlhbC5ncmF2ZUJsb29tJywgYmlvbWU6ICdidXJpYWwnLCBuYW1lOiAnZ3JhdmUgYmxvb20nLCBkZXNjcmlwdGlvbjogJ1BhbGUgZmxvd2VycyB0aHJlYWQgdGhyb3VnaCBvbGQgc29pbC4nLCBnbHlwaDogJyonLCBjb2xvcjogJyNkOGMxZTcnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2dyYXZlU29pbCddLCB0YWdzOiBbJ2dyb3d0aCcsICdncmF2ZScsICdzcGlyaXQnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdzcGlyaXQnLCAncm9vdCcsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ2dyYXZlU2FsdCcsIGVmZmVjdFJld2FyZDogJ2FuY2VzdG9yVG9rZW4nIH0pLFxuICBkZWZpbmUoeyBpZDogJ2J1cmlhbC5hbmNlc3RvclN0b25lJywgYmlvbWU6ICdidXJpYWwnLCBuYW1lOiAnYW5jZXN0b3Igc3RvbmUnLCBkZXNjcmlwdGlvbjogJ0EgY2FydmVkIG5hbWUgd2FpdHMgYmVuZWF0aCBzb2Z0IGxpY2hlbi4nLCBnbHlwaDogJ1MnLCBjb2xvcjogJyNiYmE5ZDAnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2NhaXJuJywgJ3NwaXJpdFBhdGgnXSwgdGFnczogWydyaXR1YWwnLCAnc3Bpcml0JywgJ2dyYXZlJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnc3Bpcml0JywgJ3dhcmQnLCAnZm9yY2UnXSwgYWN0aXZhdGlvblJld2FyZDogJ21vdXJuaW5nQmVsbCcsIGVmZmVjdFJld2FyZDogJ3dhcmQnIH0pLFxuICBkZWZpbmUoeyBpZDogJ2J1cmlhbC5zZWFsZWRUb21iJywgYmlvbWU6ICdidXJpYWwnLCBuYW1lOiAnc2VhbGVkIHRvbWInLCBkZXNjcmlwdGlvbjogJ0Jyb256ZSBzZWFscyBiaW5kIGEgdG9tYiBkb29yIHNodXQuJywgZ2x5cGg6ICdUJywgY29sb3I6ICcjY2JhOTZmJywgdGVycmFpbjogWydmbG9vcicsICdvc3N1YXJ5J10sIHRhZ3M6IFsnY2FjaGUnLCAnZ3JhdmUnLCAnaGF6YXJkJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnYm9tYicsICdzcGlyaXQnLCAndGhyb3cnXSwgYWN0aXZhdGlvblJld2FyZDogJ3RvbWJLZXknLCBlZmZlY3RSZXdhcmQ6ICdncmF2ZVNpY2tsZScgfSksXG4gIGRlZmluZSh7IGlkOiAnc2FsdEZsYXRzLm1pcmFnZUNhaXJuJywgYmlvbWU6ICdzYWx0RmxhdHMnLCBuYW1lOiAnbWlyYWdlIGNhaXJuJywgZGVzY3JpcHRpb246ICdBIHNhbHQgY2Fpcm4gc2hpZnRzIHdoZW5ldmVyIHlvdSBsb29rIGF3YXkuJywgZ2x5cGg6ICfilrMnLCBjb2xvcjogJyNmMWRmOWEnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3NhbHRNaXJyb3InXSwgdGFnczogWydzYWx0JywgJ21pcnJvcicsICd3YXJuaW5nJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnZm9yY2UnLCAndGhyb3cnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdtYXBTY3JvbGwnLCBlZmZlY3RSZXdhcmQ6ICdmb2N1c1RvbmljJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdzYWx0RmxhdHMuc3VuTWlycm9yJywgYmlvbWU6ICdzYWx0RmxhdHMnLCBuYW1lOiAnc3VuIG1pcnJvcicsIGRlc2NyaXB0aW9uOiAnQSBwb2xpc2hlZCBwbGF0ZSB0aHJvd3MgaGFyZCBsaWdodCBhY3Jvc3MgdGhlIGZsYXRzLicsIGdseXBoOiAn4peHJywgY29sb3I6ICcjZmZmMWFlJywgdGVycmFpbjogWydmbG9vcicsICdzYWx0TWlycm9yJ10sIHRhZ3M6IFsnc2FsdCcsICdtaXJyb3InLCAnbGlnaHQnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdmaXJlJywgJ2ZvcmNlJywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAnc2lnaHQnLCBlZmZlY3RSZXdhcmQ6ICdzdW5ibGFkZScgfSksXG4gIGRlZmluZSh7IGlkOiAnc2FsdEZsYXRzLmJyaW5lV2VsbCcsIGJpb21lOiAnc2FsdEZsYXRzJywgbmFtZTogJ2JyaW5lIHdlbGwnLCBkZXNjcmlwdGlvbjogJ0JsYWNrIGJyaW5lIHdlbGxzIGJlbmVhdGggYSBjcnVzdCBvZiB3aGl0ZSBzYWx0LicsIGdseXBoOiAn4omIJywgY29sb3I6ICcjNzZiYmM0JywgdGVycmFpbjogWydmbG9vcicsICdicmluZSddLCB0YWdzOiBbJ2JyaW5lJywgJ3NhbHQnLCAnaGF6YXJkJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnd2F0ZXInLCAnZm9yY2UnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdmb2N1c1RvbmljJywgZWZmZWN0UmV3YXJkOiAndG9uaWMnIH0pLFxuICBkZWZpbmUoeyBpZDogJ3NhbHRGbGF0cy5jYXJhdmFuSHVzaycsIGJpb21lOiAnc2FsdEZsYXRzJywgbmFtZTogJ2NhcmF2YW4gaHVzaycsIGRlc2NyaXB0aW9uOiAnQSBzdHJpcHBlZCBjb3VyaWVyIGNhcnQgcG9pbnRzIHRvd2FyZCBhIGZhbHNlIGhvcml6b24uJywgZ2x5cGg6ICdDJywgY29sb3I6ICcjYzdhNzZmJywgdGVycmFpbjogWydmbG9vcicsICdjcnVtYmxlJ10sIHRhZ3M6IFsncm91dGUnLCAnc2FsdCcsICdzYWx2YWdlJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnYm9tYicsICdmb3JjZScsICd0aHJvdyddLCBhY3RpdmF0aW9uUmV3YXJkOiAncm9wZUJ1bmRsZScsIGVmZmVjdFJld2FyZDogJ2JyaWRnZUtpdCcgfSksXG4gIGRlZmluZSh7IGlkOiAnc2FsdEZsYXRzLmdsYXNzTWFya2VyJywgYmlvbWU6ICdzYWx0RmxhdHMnLCBuYW1lOiAnZ2xhc3MgbWFya2VyJywgZGVzY3JpcHRpb246ICdBIHN1bi1ibGVhY2hlZCBzaWduIGxpc3RzIHJvdXRlcyB0aGF0IG5vIGxvbmdlciBleGlzdC4nLCBnbHlwaDogJyEnLCBjb2xvcjogJyNkNWVmZjAnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ3NhbHRNaXJyb3InXSwgdGFnczogWyd3YXJuaW5nJywgJ21pcnJvcicsICdzYWx0J10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnZmlyZScsICd0aHJvdycsICdoYXphcmQnXSwgYWN0aXZhdGlvblJld2FyZDogJ21hcFNjcm9sbCcsIGVmZmVjdFJld2FyZDogJ2JsaW5rJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdzYWx0RmxhdHMud2hpdGVDYWNoZScsIGJpb21lOiAnc2FsdEZsYXRzJywgbmFtZTogJ3doaXRlIGNhY2hlJywgZGVzY3JpcHRpb246ICdBIHdheGVkIHBhcmNlbCBsaWVzIGJ1cmllZCBpbiBoYXJkIHNhbHQuJywgZ2x5cGg6ICc/JywgY29sb3I6ICcjZjNlYmMyJywgdGVycmFpbjogWydmbG9vcicsICdzYWx0TWlycm9yJ10sIHRhZ3M6IFsnY2FjaGUnLCAnc2FsdCcsICdzYWx2YWdlJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnYm9tYicsICdmaXJlJywgJ3Rocm93JywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAndG9uaWMnLCBlZmZlY3RSZXdhcmQ6ICdmaXJlSmFyJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmcm9zdFJlbGlxdWFyeS5kdWVsQmVsbCcsIGJpb21lOiAnZnJvc3RSZWxpcXVhcnknLCBuYW1lOiAnd2luZCBzaGVsdGVyJywgZGVzY3JpcHRpb246ICdBIGxvdyBzbm93IHdhbGwgYnJlYWtzIHRoZSB3aGl0ZW91dCBhbmQgbWFya3MgYSBzYWZlIHJlc3QuJywgZ2x5cGg6ICfiiKknLCBjb2xvcjogJyNkOGYxZmYnLCB0ZXJyYWluOiBbJ2Zsb29yJ10sIHRhZ3M6IFsnZnJvc3QnLCAncm91dGUnLCAnd2FybmluZyddLCBob29rczogWydvcGVyYXRlJywgJ2ZvcmNlJywgJ3dhcmQnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICd3YXJkJywgZWZmZWN0UmV3YXJkOiAnZm9jdXNUb25pYycgfSksXG4gIGRlZmluZSh7IGlkOiAnZnJvc3RSZWxpcXVhcnkucmltZVNhcmNvcGhhZ3VzJywgYmlvbWU6ICdmcm9zdFJlbGlxdWFyeScsIG5hbWU6ICd0cmFpbCBtYXJrZXInLCBkZXNjcmlwdGlvbjogJ0JsdWUgcGVubmFudHMgcG9pbnQgZnJvbSB0aGUgc2hvcmUgdG93YXJkIHRoZSBuZXh0IHNoZWx0ZXIuJywgZ2x5cGg6ICchJywgY29sb3I6ICcjYjlkYmVhJywgdGVycmFpbjogWydmbG9vcicsICdpY2UnXSwgdGFnczogWydmcm9zdCcsICdpY2UnLCAnd2FybmluZyddLCBob29rczogWydvcGVyYXRlJywgJ2JvbWInLCAnZm9yY2UnLCAndGhyb3cnXSwgYWN0aXZhdGlvblJld2FyZDogJ21hcFNjcm9sbCcsIGVmZmVjdFJld2FyZDogJ3NpZ2h0JyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmcm9zdFJlbGlxdWFyeS5pY2VGb3JnZScsIGJpb21lOiAnZnJvc3RSZWxpcXVhcnknLCBuYW1lOiAnaWNlIGJyaWRnZScsIGRlc2NyaXB0aW9uOiAnQSBsYXNoZWQgYnJpZGdlIHNwYW5zIGEgY3JhY2sgd2hlcmUgdGhlIGxha2UgaWNlIGhhcyBwdWxsZWQgYXBhcnQuJywgZ2x5cGg6ICc9JywgY29sb3I6ICcjYWVlNmY0JywgdGVycmFpbjogWydmbG9vcicsICdpY2UnXSwgdGFnczogWydmcm9zdCcsICdpY2UnLCAncm91dGUnXSwgaG9va3M6IFsnb3BlcmF0ZScsICdmaXJlJywgJ2ZvcmNlJywgJ2hhemFyZCddLCBhY3RpdmF0aW9uUmV3YXJkOiAnYnJpZGdlS2l0JywgZWZmZWN0UmV3YXJkOiAncG9ydGFibGVXaW5jaCcgfSksXG4gIGRlZmluZSh7IGlkOiAnZnJvc3RSZWxpcXVhcnkuZnJvemVuQ2FjaGUnLCBiaW9tZTogJ2Zyb3N0UmVsaXF1YXJ5JywgbmFtZTogJ2Jyb2tlbiBzbGVkJywgZGVzY3JpcHRpb246ICdBIHNwbGludGVyZWQgc2xlZCBvZmZlcnMgYSByaXNreSBjYWNoZSBiZXNpZGUgdGhlIHByZXNzdXJlIGNyYWNrLicsIGdseXBoOiAncycsIGNvbG9yOiAnI2UyZjZmZicsIHRlcnJhaW46IFsnZmxvb3InLCAnZnJvc3RSaW1lJ10sIHRhZ3M6IFsnY2FjaGUnLCAnZnJvc3QnLCAnaWNlJ10sIGhvb2tzOiBbJ29wZXJhdGUnLCAnZmlyZScsICdib21iJywgJ3Rocm93J10sIGFjdGl2YXRpb25SZXdhcmQ6ICd0b25pYycsIGVmZmVjdFJld2FyZDogJ2dyYXBwbGVMaW5lJyB9KSxcbiAgZGVmaW5lKHsgaWQ6ICdmcm9zdFJlbGlxdWFyeS5yZWxpcXVhcnlXYXJkJywgYmlvbWU6ICdmcm9zdFJlbGlxdWFyeScsIG5hbWU6ICdyZWxpcXVhcnkgd2FyZCcsIGRlc2NyaXB0aW9uOiAnQSBjb2xkIHdhcmRzdG9uZSBwcmVzZXJ2ZXMgYSBuYXJyb3cgZHVlbGluZyBncm91bmQuJywgZ2x5cGg6ICfil4cnLCBjb2xvcjogJyNkMmM3ZmYnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2ljZSddLCB0YWdzOiBbJ2Zyb3N0JywgJ2R1ZWwnLCAnd2FyZCddLCBob29rczogWydvcGVyYXRlJywgJ3dhcmQnLCAnZm9yY2UnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICd3YXJkU2NyaXB0JywgZWZmZWN0UmV3YXJkOiAnbWVuZCcgfSksXG4gIGRlZmluZSh7IGlkOiAnZnJvc3RSZWxpcXVhcnkudGhhd1ZhbHZlJywgYmlvbWU6ICdmcm9zdFJlbGlxdWFyeScsIG5hbWU6ICd0aGF3IHZhbHZlJywgZGVzY3JpcHRpb246ICdBIGJyYXNzIHZhbHZlIGNhbiBmbG9vZCB0aGUgcmVsaXF1YXJ5IHdpdGggZnJlZXppbmcgbWVsdC4nLCBnbHlwaDogJ1YnLCBjb2xvcjogJyNkOWJmODMnLCB0ZXJyYWluOiBbJ2Zsb29yJywgJ2Zyb3N0UmltZSddLCB0YWdzOiBbJ2Zyb3N0JywgJ2ljZScsICdyb3V0ZSddLCBob29rczogWydvcGVyYXRlJywgJ2ZpcmUnLCAnd2F0ZXInLCAnZm9yY2UnLCAnaGF6YXJkJ10sIGFjdGl2YXRpb25SZXdhcmQ6ICdwb3J0YWJsZVdpbmNoJywgZWZmZWN0UmV3YXJkOiAnYnJpZGdlS2l0JyB9KVxuXSBhcyBjb25zdCBzYXRpc2ZpZXMgcmVhZG9ubHkgUHJvcERlZmluaXRpb25bXVxuXG5leHBvcnQgY29uc3QgUFJPUF9JRFMgPSBQUk9QX0RFRklOSVRJT05TLm1hcChkZWZpbml0aW9uID0+IGRlZmluaXRpb24uaWQpIGFzIHJlYWRvbmx5IFByb3BJZFtdXG5jb25zdCBkZWZpbml0aW9ucyA9IG5ldyBNYXA8UHJvcElkLCBQcm9wRGVmaW5pdGlvbj4oUFJPUF9ERUZJTklUSU9OUy5tYXAoZGVmaW5pdGlvbiA9PiBbZGVmaW5pdGlvbi5pZCwgZGVmaW5pdGlvbl0pKVxuXG5leHBvcnQgY29uc3QgcHJvcERlZmluaXRpb24gPSAoaWQ6IFByb3BJZCk6IFByb3BEZWZpbml0aW9uID0+IHtcbiAgY29uc3QgZGVmaW5pdGlvbiA9IGRlZmluaXRpb25zLmdldChpZClcbiAgaWYgKCFkZWZpbml0aW9uKSB0aHJvdyBuZXcgRXJyb3IoYG1pc3NpbmcgcHJvcCBkZWZpbml0aW9uOiAke2lkfWApXG4gIHJldHVybiBkZWZpbml0aW9uXG59XG5cbmV4cG9ydCBjb25zdCBwcm9wRGVmaW5pdGlvbnNGb3IgPSAoYmlvbWU6IEJpb21lKTogcmVhZG9ubHkgUHJvcERlZmluaXRpb25bXSA9PiBQUk9QX0RFRklOSVRJT05TLmZpbHRlcihkZWZpbml0aW9uID0+IGRlZmluaXRpb24uYmlvbWUgPT09IGJpb21lKVxuZXhwb3J0IGNvbnN0IHByb3BBdCA9IChwcm9wczogcmVhZG9ubHkgUHJvcFtdLCB4OiBudW1iZXIsIHk6IG51bWJlcik6IFByb3AgfCB1bmRlZmluZWQgPT4gcHJvcHMuZmluZChwcm9wID0+IHByb3AueCA9PT0geCAmJiBwcm9wLnkgPT09IHkgJiYgcHJvcC5zdGF0ZSAhPT0gJ2Rlc3Ryb3llZCcpXG5leHBvcnQgY29uc3QgaXNCbG9ja2luZ1Byb3AgPSAocHJvcDogUHJvcCB8IHVuZGVmaW5lZCk6IGJvb2xlYW4gPT4gKHByb3A/LmtpbmQgPT09ICdtaW5lLmJyb2tlbkNhcnQnICYmIHByb3Auc3RhdGUgIT09ICdkZXN0cm95ZWQnKSB8fCAocHJvcD8ua2luZCA9PT0gJ3dpbGRzLnJvb3RBcmNoJyAmJiBwcm9wLnN0YXRlICE9PSAnYWN0aXZhdGVkJyAmJiBwcm9wLnN0YXRlICE9PSAnZGVzdHJveWVkJykgfHwgKHByb3A/LmtpbmQgPT09ICdjYXZlcm5zLmJyb2tlbkJvYXQnICYmIHByb3Auc3RhdGUgIT09ICdhY3RpdmF0ZWQnICYmIHByb3Auc3RhdGUgIT09ICdkZXN0cm95ZWQnKSB8fCAocHJvcD8ua2luZCA9PT0gJ2NhdmVybnMuZWVsVHVubmVsJyAmJiBwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJykgfHwgKHByb3A/LmtpbmQgPT09ICdydWlucy5jb2xsYXBzZWRBcmNoJyAmJiBwcm9wLnN0YXRlICE9PSAnYWN0aXZhdGVkJyAmJiBwcm9wLnN0YXRlICE9PSAnZGVzdHJveWVkJylcbmV4cG9ydCBjb25zdCBpc1NpZ2h0QmxvY2tpbmdQcm9wID0gKHByb3A6IFByb3AgfCB1bmRlZmluZWQpOiBib29sZWFuID0+IChwcm9wPy5raW5kID09PSAnY2F2ZXJucy5jcnlzdGFsQ2x1c3RlcicgJiYgcHJvcC5zdGF0ZSAhPT0gJ2FjdGl2YXRlZCcgJiYgcHJvcC5zdGF0ZSAhPT0gJ2Rlc3Ryb3llZCcpIHx8IChwcm9wPy5raW5kID09PSAncnVpbnMuYnJva2VuU3RhdHVlJyAmJiBwcm9wLnN0YXRlID09PSAnYWN0aXZhdGVkJylcbmV4cG9ydCBjb25zdCBpc0xpbmVCbG9ja2luZ1Byb3AgPSAocHJvcDogUHJvcCB8IHVuZGVmaW5lZCk6IGJvb2xlYW4gPT4gaXNCbG9ja2luZ1Byb3AocHJvcCkgfHwgaXNTaWdodEJsb2NraW5nUHJvcChwcm9wKVxuZXhwb3J0IGNvbnN0IGxpbmVQcm9wQmxvY2tlciA9IChwcm9wOiBQcm9wIHwgdW5kZWZpbmVkKTogJ2NhcnQnIHwgJ2NyeXN0YWwnIHwgJ2NvdmVyJyB8IHVuZGVmaW5lZCA9PiBpc0Jsb2NraW5nUHJvcChwcm9wKSA/ICdjYXJ0JyA6IHByb3A/LmtpbmQgPT09ICdjYXZlcm5zLmNyeXN0YWxDbHVzdGVyJyAmJiBpc1NpZ2h0QmxvY2tpbmdQcm9wKHByb3ApID8gJ2NyeXN0YWwnIDogaXNTaWdodEJsb2NraW5nUHJvcChwcm9wKSA/ICdjb3ZlcicgOiB1bmRlZmluZWRcbmV4cG9ydCBjb25zdCBwcm9wRWZmZWN0cyA9IChob29rczogcmVhZG9ubHkgUHJvcEhvb2tbXSk6IFByb3BFZmZlY3RLaW5kW10gPT4gaG9va3MuZmlsdGVyKChob29rKTogaG9vayBpcyBQcm9wRWZmZWN0S2luZCA9PiBob29rICE9PSAnb3BlcmF0ZScpXG5cbmV4cG9ydCBjb25zdCB2YWxpZGF0ZVByb3BEZWZpbml0aW9ucyA9ICgpOiBzdHJpbmdbXSA9PiB7XG4gIGNvbnN0IGVycm9yczogc3RyaW5nW10gPSBbXVxuICBpZiAoUFJPUF9ERUZJTklUSU9OUy5sZW5ndGggIT09IDYwKSBlcnJvcnMucHVzaChgZXhwZWN0ZWQgNjAgcHJvcCBkZWZpbml0aW9ucywgZm91bmQgJHtQUk9QX0RFRklOSVRJT05TLmxlbmd0aH1gKVxuICBjb25zdCBpZHMgPSBuZXcgU2V0PHN0cmluZz4oKVxuICBmb3IgKGNvbnN0IGRlZmluaXRpb24gb2YgUFJPUF9ERUZJTklUSU9OUykge1xuICAgIGlmIChpZHMuaGFzKGRlZmluaXRpb24uaWQpKSBlcnJvcnMucHVzaChgZHVwbGljYXRlIHByb3AgZGVmaW5pdGlvbjogJHtkZWZpbml0aW9uLmlkfWApXG4gICAgaWRzLmFkZChkZWZpbml0aW9uLmlkKVxuICAgIGlmICghZGVmaW5pdGlvbi50YWdzLmxlbmd0aCkgZXJyb3JzLnB1c2goYG1pc3NpbmcgdGFnczogJHtkZWZpbml0aW9uLmlkfWApXG4gICAgaWYgKCFkZWZpbml0aW9uLmhvb2tzLmluY2x1ZGVzKCdvcGVyYXRlJykgfHwgIXByb3BFZmZlY3RzKGRlZmluaXRpb24uaG9va3MpLmxlbmd0aCkgZXJyb3JzLnB1c2goYGluY29tcGxldGUgaG9va3M6ICR7ZGVmaW5pdGlvbi5pZH1gKVxuICAgIGlmICghSVRFTVtkZWZpbml0aW9uLmFjdGl2YXRpb25SZXdhcmRdIHx8ICFJVEVNW2RlZmluaXRpb24uZWZmZWN0UmV3YXJkXSkgZXJyb3JzLnB1c2goYHVua25vd24gcmV3YXJkOiAke2RlZmluaXRpb24uaWR9YClcbiAgfVxuICBmb3IgKGNvbnN0IGJpb21lIG9mIFsnbWluZScsICd3aWxkcycsICdjYXZlcm5zJywgJ3J1aW5zJywgJ2Z1cm5hY2UnLCAnZmxvb2RlZFJ1aW5zJywgJ2NsaWZmcycsICdidXJpYWwnLCAnc2FsdEZsYXRzJywgJ2Zyb3N0UmVsaXF1YXJ5J10gYXMgY29uc3QpIGlmIChwcm9wRGVmaW5pdGlvbnNGb3IoYmlvbWUpLmxlbmd0aCAhPT0gNikgZXJyb3JzLnB1c2goYGV4cGVjdGVkIDYgJHtiaW9tZX0gcHJvcHNgKVxuICByZXR1cm4gZXJyb3JzXG59XG4iXSwibWFwcGluZ3MiOiJBQUFBLFNBQVNBLElBQUksUUFBUSxXQUFXO0FBaUJoQyxNQUFNQyxNQUFNLEdBQUlDLFVBQTBCLElBQXFCQSxVQUFVO0FBRXpFLE9BQU8sTUFBTUMsZ0JBQWdCLEdBQUcsQ0FDOUJGLE1BQU0sQ0FBQztFQUFFRyxFQUFFLEVBQUUsY0FBYztFQUFFQyxLQUFLLEVBQUUsTUFB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QUFFLFNBQVM7RUFBRUMsT0FBTyxFQUFFLENBQUMsT0FBTyxFQUFFLFNBQVMsQ0FBQztFQUFFQyxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsT0FBTyxFQUFFLFFBQVEsQ0FBQztFQUFFQyxLQUFLLEVBQUUsQ0FBQyxTQUFTLEVBQUUsTUFBTSxFQUFFLFFBQVEsRUFBRSxPQUFPLENBQUM7RUFBRUMsZ0JBQWdCLEVBQUUsU0FBUztFQUFFQyxZQUFZLEVBQUU7QUFBYyxDQUFDLENBQUMsRUFDdlViLE1BQU0sQ0FBQztFQUFFRyxFQUFFLEVBQUUsdUJBQXVCO0VBQUVDLEtBQUssRUFBRSxXQUFXO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLFdBQVcsRUFBRSw2Q0FBNkM7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsT0FBTyxFQUFFLENBQUMsT0FBTyxFQUFFLFlBQVksQ0FBQztFQUFFQyxJQUFJLEVBQUUsQ0FBQyxNQUFNLEVBQUUsUUFBUSxFQUFFLFNBQVMsQ0FBQztFQUFFQyxLQUFLLEVBQUUsQ0FBQyxTQUFTLEVBQUUsT0FBTyxFQUFFLE9BQU8sRUFBRSxRQUFRLENBQUM7RUFBRUMsZ0JBQWdCLEVBQUUsV0FBVztFQUFFQyxZQUFZLEVBQUU7QUFBYSxDQUFDLENBQUMsRUFDN1ZiLE1BQU0sQ0FBQztFQUFFRyxFQUFFLEVBQUUscUJBQXFCO0VBQUVDLEtBQUssRUFBRSxXQUFXO0VBQUVDLElBQUksRUFBRSxZQUFZO0VBQUVDLFdBQVcsRUFBRSxzREFBc0Q7RUFBRUMsS0FBSyxFQUFFLEdBQUc7RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsT0FBTyxFQUFFLENBQUMsT0FBTyxFQUFFLFlBQVksQ0FBQztFQUFFQyxJQUFJLEVBQUUsQ0FBQyxNQUFNLEVBQUUsUUFBUSxFQUFFLE9BQU8sQ0FBQztFQUFFQyxLQUFLLEVBQUUsQ0FBQyxTQUFTLEVBQUUsTUFBTSxFQUFFLE9BQU8sRUFBRSxRQUFRLENBQUM7RUFBRUMsZ0JBQWdCLEVBQUUsT0FBTztFQUFFQyxZ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