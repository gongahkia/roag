// f7bff0eb7dc9a98e5d5a20906320e4de84c1e740
import { DIRECTIONS } from '../types';
import { rngFor } from '../rng';
import { rewardOfferFor } from '../reward-contract';
import { getTile, isPassable } from '../world';
import { advance } from './combat';
import { event, log } from './shared';
import { refreshFov } from './visibility';
import { recordInteraction, recordTelemetryCount } from '../telemetry';
import { armRelicTraversal, consumeRelicTool, relicAlignment, relicChoices, relicFor } from './relics';
import { tend } from './alignment';
import { isTerrainMutationTool, terrainMutationAssessment } from './terrain-mutations';
export const TOOLS = [{
  id: 'stoneWedge',
  name: 'Stone Wedge',
  glyph: 'W',
  cooldown: 6,
  text: 'Breach one adjacent blocker.',
  overdrive: 'Breach a three-tile wedge, then retire.'
}, {
  id: 'reedwing',
  name: 'Reedwing',
  glyph: '^',
  cooldown: 5,
  text: 'Cross one hazardous tile to a clear landing.',
  overdrive: 'Cross up to two hazards, then retire.'
}, {
  id: 'cordAnchor',
  name: 'Cord Anchor',
  glyph: '⌁',
  cooldown: 5,
  text: 'Pull two tiles to a clear landing.',
  overdrive: 'Pull four tiles, then retire.'
}, {
  id: 'ashwayRites',
  name: 'Ashway Rites',
  glyph: '≈',
  cooldown: 8,
  text: 'Make a two-tile temporary safe path.',
  overdrive: 'Make a three-tile path, then retire.'
}, {
  id: 'antlerPrybar',
  name: 'Antler Prybar',
  glyph: '⌐',
  cooldown: 4,
  text: 'Shift one adjacent boulder or breakwall forward.',
  overdrive: 'Shift once with a louder exposed footing risk, then retire.'
}, {
  id: 'stoneAdze',
  name: 'Stone Adze',
  glyph: '⌟',
  cooldown: 5,
  text: 'Cut one adjacent crate or crumble route.',
  overdrive: 'Cut once, then retire.'
}, {
  id: 'resinFireBasket',
  name: 'Resin Fire Basket',
  glyph: '♨',
  cooldown: 6,
  text: 'Burn adjacent bramble or web into smoke.',
  overdrive: 'Burn once with stronger smoke, then retire.'
}, {
  id: 'woodenLeverRoller',
  name: 'Wooden Lever and Roller',
  glyph: '↔',
  cooldown: 5,
  text: 'Push one adjacent movable prop forward.',
  overdrive: 'Push once, then retire.'
}];
export const BOONS = [{
  id: 'scoutEye',
  name: 'Scout Eye',
  glyph: '◉',
  family: 'scouting',
  text: 'Reveal all remaining milestone directions.'
}, {
  id: 'timeKnot',
  name: 'Time Knot',
  glyph: '⟲',
  family: 'recovery',
  text: 'Gain one position-only safe-step rewind.'
}, {
  id: 'coolAsh',
  name: 'Cool Ash',
  glyph: '•',
  family: 'traversal',
  text: 'Tools recover 1 turn faster per stack.'
}, {
  id: 'wayfinderCord',
  name: 'Wayfinder Cord',
  glyph: '⌇',
  family: 'traversal',
  text: 'Tool use reveals nearby terrain.'
}, {
  id: 'rootedResolve',
  name: 'Rooted Resolve',
  glyph: '✦',
  family: 'recovery',
  text: 'Tool use restores 1 HP per stack.'
}, {
  id: 'trailRations',
  name: 'Trail Rations',
  glyph: '+',
  family: 'recovery',
  text: 'Each milestone restores 2 HP per stack.'
}, {
  id: 'emberFletching',
  name: 'Ember Fletching',
  glyph: '*',
  family: 'combat',
  text: 'Thrown attacks deal +1 damage per stack.'
}, {
  id: 'cordTempo',
  name: 'Cord Tempo',
  glyph: '/',
  family: 'combat',
  text: 'Tool use restores 1 focus per stack.'
}, {
  id: 'watchfulStep',
  name: 'Watchful Step',
  glyph: '!',
  family: 'scouting',
  text: 'Newly discovered milestones reveal nearby threats.'
}, {
  id: 'mapMoss',
  name: 'Map Moss',
  glyph: '·',
  family: 'scouting',
  text: 'Exploration reveals 1 extra tile radius per stack.'
}, {
  id: 'quietTide',
  name: 'Quiet Tide',
  glyph: '~',
  family: 'spellcraft',
  text: 'Charms cost 1 less focus per stack.'
}, {
  id: 'spiritKindling',
  name: 'Spirit Kindling',
  glyph: '?',
  family: 'spellcraft',
  text: 'Milestones restore 1 focus per stack.'
}, {
  id: 'cacheSense',
  name: 'Cache Sense',
  glyph: '$',
  family: 'economy',
  text: 'Each milestone yields 8 cash per stack.'
}, {
  id: 'barterThread',
  name: 'Barter Thread',
  glyph: '¤',
  family: 'economy',
  text: 'Containers yield +10 cash per stack.'
}, {
  id: 'stoneMemory',
  name: 'Stone Memory',
  glyph: '#',
  family: 'traversal',
  text: 'Stone Wedge also clears nearby rubble per stack.'
}, {
  id: 'reedMemory',
  name: 'Reed Memory',
  glyph: '≋',
  family: 'traversal',
  text: 'Reedwing crosses one extra hazard per stack.'
}, {
  id: 'lastLight',
  name: 'Last Light',
  glyph: 'i',
  family: 'recovery',
  text: 'At 25% HP, tool use restores 2 HP per stack.'
}, {
  id: 'parcelMark',
  name: 'Parcel Mark',
  glyph: '□',
  family: 'economy',
  text: 'Unclaimed milestones show distance after one stack.'
}, {
  id: 'breachTempo',
  name: 'Breach Tempo',
  glyph: '↯',
  family: 'traversal',
  text: 'Breaking terrain restores 1 focus per stack.'
}, {
  id: 'updraftStep',
  name: 'Updraft Step',
  glyph: '↑',
  family: 'traversal',
  text: 'Lifts and glides travel 1 extra tile per stack.'
}, {
  id: 'anchorHabit',
  name: 'Anchor Habit',
  glyph: '⚓',
  family: 'traversal',
  text: 'Ropes and anchors restore 1 HP per stack.'
}, {
  id: 'smokeWalker',
  name: 'Smoke Walker',
  glyph: '≈',
  family: 'terrain',
  text: 'Smoke deals 1 less damage and reveals nearby ground.'
}, {
  id: 'currentSense',
  name: 'Current Sense',
  glyph: '≋',
  family: 'terrain',
  text: 'Current crossings reveal a route and grant 1 focus.'
}, {
  id: 'wallSong',
  name: 'Wall Song',
  glyph: '♫',
  family: 'terrain',
  text: 'Breach tools clear one additional adjacent blocker per stack.'
}, {
  id: 'ashDividend',
  name: 'Ash Dividend',
  glyph: '¤',
  family: 'economy',
  text: 'Destroyed terrain and props yield 4 cash per stack.'
}, {
  id: 'salvager',
  name: 'Salvager',
  glyph: '⛏',
  family: 'economy',
  text: 'First used consumable each floor has a 25% return chance per stack.'
}, {
  id: 'lastMatch',
  name: 'Last Match',
  glyph: '†',
  family: 'consumable',
  text: 'At 25% HP, bombs and fire items gain +2 damage per stack.'
}, {
  id: 'spareFuse',
  name: 'Spare Fuse',
  glyph: '!',
  family: 'consumable',
  text: 'Bomb packs restore one extra bomb per stack.'
}, {
  id: 'quietPocket',
  name: 'Quiet Pocket',
  glyph: '◌',
  family: 'consumable',
  text: 'Tonics also remove one condition per stack.'
}, {
  id: 'scavengerMap',
  name: 'Scavenger Map',
  glyph: '⌖',
  family: 'scouting',
  text: 'Containers reveal a nearby item or milestone per stack.'
}, {
  id: 'dangerInstinct',
  name: 'Danger Instinct',
  glyph: '!',
  family: 'scouting',
  text: 'Entering a new hazard reveals adjacent enemies per stack.'
}, {
  id: 'slagSkin',
  name: 'Slag Skin',
  glyph: '◒',
  family: 'recovery',
  text: 'Fire and smoke damage are reduced by 1 per stack.'
}, {
  id: 'tideSkin',
  name: 'Tide Skin',
  glyph: '◓',
  family: 'recovery',
  text: 'Water and current damage are reduced by 1 per stack.'
}, {
  id: 'pressureSeal',
  name: 'Pressure Seal',
  glyph: '◈',
  family: 'recovery',
  text: 'Gain 1 shield after using a traversal tool per stack.'
}, {
  id: 'bloodCompass',
  name: 'Blood Compass',
  glyph: '✥',
  family: 'combat',
  text: 'Killing an enemy reveals 2 tiles per stack.'
}, {
  id: 'hookLine',
  name: 'Hook Line',
  glyph: '⌇',
  family: 'combat',
  text: 'Reach weapons deal +1 damage per stack after movement.'
}, {
  id: 'guardRattle',
  name: 'Guard Rattle',
  glyph: ')',
  family: 'combat',
  text: 'Off-hand gear grants +1 guard after a hit per stack.'
}, {
  id: 'tideEdge',
  name: 'Tide Edge',
  glyph: '/',
  family: 'combat',
  text: 'Water/current weapon hits pull targets 1 tile per stack.'
}, {
  id: 'cinderEdge',
  name: 'Cinder Edge',
  glyph: '/',
  family: 'combat',
  text: 'Fire/hammer weapon hits ignite gas or smoke per stack.'
}, {
  id: 'echoCache',
  name: 'Echo Cache',
  glyph: '$',
  family: 'economy',
  text: 'Each distinct Boon family grants 5 cash at milestones.'
}, {
  id: 'openCircuit',
  name: 'Open Circuit',
  glyph: '⌁',
  family: 'spellcraft',
  text: 'Casting after traversal restores 1 focus per stack.'
}, {
  id: 'blinkDebt',
  name: 'Blink Debt',
  glyph: '?',
  family: 'spellcraft',
  text: 'Blink and pull gain 1 range per stack.'
}, {
  id: 'rootBattery',
  name: 'Root Battery',
  glyph: '♣',
  family: 'spellcraft',
  text: 'Root/ward effects grant 1 shield or focus per stack.'
}, {
  id: 'scrapPrayer',
  name: 'Scrap Prayer',
  glyph: '☼',
  family: 'terrain',
  text: 'Activated props restore 1 HP and focus per stack.'
}, {
  id: 'softLanding',
  name: 'Soft Landing',
  glyph: '∨',
  family: 'traversal',
  text: 'Hazard crossings reduce incoming damage by 1 per stack.'
}, {
  id: 'hardLesson',
  name: 'Hard Lesson',
  glyph: '∆',
  family: 'combat',
  text: 'Taking hazard damage gives +1 melee damage next attack per stack.'
}, {
  id: 'sealedBreath',
  name: 'Sealed Breath',
  glyph: '◍',
  family: 'recovery',
  text: 'Smoke and gas no longer reduce sight per stack.'
}, {
  id: 'relayStep',
  name: 'Relay Step',
  glyph: '›',
  family: 'traversal',
  text: 'Alternating move and tool use lowers tool cooldown by 1 per stack.'
}, {
  id: 'borrowedTime',
  name: 'Borrowed Time',
  glyph: '⌛',
  family: 'recovery',
  text: 'Time Knot gains one extra safe position per stack.'
}, {
  id: 'furnaceHeart',
  name: 'Furnace Heart',
  glyph: '♥',
  family: 'terrain',
  text: 'Fire actions grant +1 damage and +1 focus per stack.',
  rare: true
}, {
  id: 'drownedOath',
  name: 'Drowned Oath',
  glyph: '♆',
  family: 'terrain',
  text: 'Water/current actions heal 1 HP and pull enemies per stack.',
  rare: true
}, {
  id: 'blackLedger',
  name: 'Black Ledger',
  glyph: '§',
  family: 'economy',
  text: 'Gain 12 cash whenever you claim a milestone per stack.',
  rare: true
}, {
  id: 'glassNerve',
  name: 'Glass Nerve',
  glyph: '◇',
  family: 'combat',
  text: 'Your first attack each turn gains +3 damage per stack.',
  rare: true
}, {
  id: 'wayEater',
  name: 'Way Eater',
  glyph: '⌘',
  family: 'traversal',
  text: 'Clearing terrain permanently lowers all tool cooldowns by 1 per stack.',
  rare: true
}, {
  id: 'deepPockets',
  name: 'Deep Pockets',
  glyph: '▣',
  family: 'consumable',
  text: 'Used items have a 35% chance per stack to return after combat.',
  rare: true
}, {
  id: 'galeThread',
  name: 'Gale Thread',
  glyph: '≈',
  family: 'traversal',
  text: 'Each climb restores 1 focus per stack.'
}, {
  id: 'ropewright',
  name: 'Ropewright',
  glyph: '⌁',
  family: 'traversal',
  text: 'At rank 2+, every second vertical rope costs no reserve rope.'
}, {
  id: 'updraftCadence',
  name: 'Updraft Cadence',
  glyph: '↑',
  family: 'traversal',
  text: 'Ledge movement grants 1 focus per stack.'
}, {
  id: 'skyhookReprisal',
  name: 'Skyhook Reprisal',
  glyph: 'J',
  family: 'combat',
  text: 'After climbing, your next attack gains +2 damage per stack.'
}, {
  id: 'thunderVessel',
  name: 'Thunder Vessel',
  glyph: 'ϟ',
  family: 'combat',
  text: 'Thrown damage gains +1 and marks targets per stack.'
}, {
  id: 'eyrieHoard',
  name: 'Eyrie Hoard',
  glyph: '$',
  family: 'economy',
  text: 'Caches and chests yield 12 cash per stack.'
}, {
  id: 'windScribe',
  name: 'Wind Scribe',
  glyph: '⌇',
  family: 'spellcraft',
  text: 'Wind and force charms gain 1 range per stack.'
}, {
  id: 'highPath',
  name: 'High Path',
  glyph: '⌖',
  family: 'scouting',
  text: 'Climbs reveal 2 tiles per stack around the destination.'
}, {
  id: 'graveLedger',
  name: 'Grave Ledger',
  glyph: '§',
  family: 'economy',
  text: 'Enemy kills grant 3 cash per stack.'
}, {
  id: 'ancestorLantern',
  name: 'Ancestor Lantern',
  glyph: 'i',
  family: 'recovery',
  text: 'Enemy kills restore 1 focus per stack.'
}, {
  id: 'boneOrchard',
  name: 'Bone Orchard',
  glyph: '✦',
  family: 'recovery',
  text: 'Enemy kills restore 1 HP per stack.'
}, {
  id: 'cairnPact',
  name: 'Cairn Pact',
  glyph: '▲',
  family: 'terrain',
  text: 'Oath and body payments grant 18 cash per stack.',
  suppresses: ['consumable'],
  rare: true
}, {
  id: 'mournersBell',
  name: 'Mourner’s Bell',
  glyph: 'o',
  family: 'spellcraft',
  text: 'Spirit events restore 1 focus per stack.'
}, {
  id: 'ossuaryWard',
  name: 'Ossuary Ward',
  glyph: '□',
  family: 'recovery',
  text: 'At each encounter resolution, gain 1 shield per stack.'
}, {
  id: 'funeralExchange',
  name: 'Funeral Exchange',
  glyph: '¤',
  family: 'economy',
  text: 'Discarded items yield 10 cash per stack.'
}, {
  id: 'lastRites',
  name: 'Last Rites',
  glyph: '†',
  family: 'combat',
  text: 'At 25% HP, attacks gain +2 damage per stack.'
}, {
  id: 'stormwake',
  name: 'Stormwake Engine',
  glyph: 'ϟ',
  family: 'combat',
  text: 'Traversal actions charge +1 attack damage per stack; suppresses recovery triggers.',
  suppresses: ['recovery'],
  rare: true
}, {
  id: 'burialCurrent',
  name: 'Burial Current',
  glyph: '≈',
  family: 'terrain',
  text: 'Current, spirit, and grave terrain restore 1 HP per stack.'
}, {
  id: 'echoDividend',
  name: 'Echo Dividend',
  glyph: '$',
  family: 'economy',
  text: 'Every resolved encounter grants 10 cash per stack.'
}, {
  id: 'tetheredThunder',
  name: 'Tethered Thunder',
  glyph: '⌁',
  family: 'combat',
  text: 'Rope and climb actions arm +1 thrown damage per stack.'
}, {
  id: 'riftLedger',
  name: 'Rift Ledger',
  glyph: '§',
  family: 'economy',
  text: 'Elite kills grant 15 extra cash per stack.'
}, {
  id: 'saltedAncestor',
  name: 'Salted Ancestor',
  glyph: 'i',
  family: 'spellcraft',
  text: 'A charm cast after a kill refunds 1 focus per stack.'
}, {
  id: 'windfall',
  name: 'Windfall',
  glyph: '↑',
  family: 'economy',
  text: 'Each route transition grants 20 cash per stack.'
}, {
  id: 'gravewind',
  name: 'Gravewind',
  glyph: '◌',
  family: 'terrain',
  text: 'Marked enemies take +1 damage per stack.'
}, {
  id: 'cliffsideCairn',
  name: 'Cliffside Cairn',
  glyph: '▲',
  family: 'scouting',
  text: 'Revealed milestones restore 1 focus per stack.'
}, {
  id: 'spiritSail',
  name: 'Spirit Sail',
  glyph: '⌇',
  family: 'traversal',
  text: 'Each climb or current crossing gains +1 movement range per stack.'
}, {
  id: 'descentEngine',
  name: 'Descent Engine',
  glyph: '↯',
  family: 'combat',
  text: 'Damage after a terrain crossing gains +1 per stack.'
}, {
  id: 'altarCompound',
  name: 'Altar Compound',
  glyph: '+',
  family: 'spellcraft',
  text: 'Altar and oath rewards add 1 boon evolution rank per stack at 3+.'
}, {
  id: 'stormRations',
  name: 'Storm Rations',
  glyph: '+',
  family: 'consumable',
  text: 'Using a tonic after moving restores 2 extra HP per stack.'
}, {
  id: 'heirloomCircuit',
  name: 'Heirloom Circuit',
  glyph: '◇',
  family: 'recovery',
  text: 'Guardian kills restore 4 HP and focus per stack.'
}, {
  id: 'bridgeOfNames',
  name: 'Bridge of Names',
  glyph: '=',
  family: 'traversal',
  text: 'Each active oath grants 1 armor per stack.'
}, {
  id: 'cursedInvestment',
  name: 'Cursed Investment',
  glyph: '☠',
  family: 'economy',
  text: 'While cursed, all cash gains are doubled per stack.',
  suppresses: ['consumable'],
  rare: true
}, {
  id: 'sunstep',
  name: 'Sunstep',
  glyph: '☼',
  family: 'traversal',
  biomes: ['saltFlats'],
  text: 'Salt mirrors restore 1 focus per stack and arm a Prism Relay.'
}, {
  id: 'brineWard',
  name: 'Brine Ward',
  glyph: '≈',
  family: 'recovery',
  biomes: ['saltFlats'],
  text: 'Brine damage is reduced by 1 per stack.'
}, {
  id: 'mirrorHunt',
  name: 'Mirror Hunt',
  glyph: '◇',
  family: 'combat',
  biomes: ['saltFlats'],
  text: 'Attacks from salt mirrors gain +1 damage per stack.'
}, {
  id: 'glassEdge',
  name: 'Glass Edge',
  glyph: '◈',
  family: 'combat',
  biomes: ['saltFlats'],
  text: 'Marked targets take +1 damage per stack.'
}, {
  id: 'saltLedger',
  name: 'Salt Ledger',
  glyph: '§',
  family: 'economy',
  biomes: ['saltFlats'],
  text: 'Milestones grant 6 extra cash per stack.'
}, {
  id: 'duneRation',
  name: 'Dune Ration',
  glyph: '+',
  family: 'recovery',
  biomes: ['saltFlats'],
  text: 'Milestones restore 1 extra HP per stack.'
}, {
  id: 'mirageMap',
  name: 'Mirage Map',
  glyph: '⌖',
  family: 'scouting',
  biomes: ['saltFlats'],
  text: 'Salt mirrors reveal 1 nearby unexplored tile per stack.'
}, {
  id: 'whiteRoad',
  name: 'White Road',
  glyph: '›',
  family: 'terrain',
  biomes: ['saltFlats'],
  text: 'Brine crossings grant 1 shield per stack.'
}, {
  id: 'sunsetCircuit',
  name: 'Sunset Circuit',
  glyph: 'ϟ',
  family: 'spellcraft',
  biomes: ['saltFlats'],
  text: 'Casting after a salt mirror restores 1 focus per stack.'
}, {
  id: 'heatDebt',
  name: 'Heat Debt',
  glyph: '☠',
  family: 'economy',
  biomes: ['saltFlats'],
  text: 'Gain 14 cash per hostile kill per stack; suppresses recovery triggers.',
  suppresses: ['recovery'],
  rare: true
}, {
  id: 'coldRead',
  name: 'Cold Read',
  glyph: '❄',
  family: 'scouting',
  biomes: ['frostReliquary'],
  text: 'Ice crossings restore 1 focus and reveal nearby threats per stack.'
}, {
  id: 'rimeGuard',
  name: 'Rime Guard',
  glyph: '□',
  family: 'recovery',
  biomes: ['frostReliquary'],
  text: 'Ice crossings grant 1 shield per stack.'
}, {
  id: 'duelistOath',
  name: 'Duelist Oath',
  glyph: '⚔',
  family: 'combat',
  biomes: ['frostReliquary'],
  text: 'Elite and guardian targets take +2 damage per stack.'
}, {
  id: 'shatterMark',
  name: 'Shatter Mark',
  glyph: '✦',
  family: 'combat',
  biomes: ['frostReliquary'],
  text: 'Slowed or marked targets take +1 damage per stack.'
}, {
  id: 'winterRations',
  name: 'Winter Rations',
  glyph: '+',
  family: 'recovery',
  biomes: ['frostReliquary'],
  text: 'Milestones restore 1 HP and focus per stack.'
}, {
  id: 'iceLedger',
  name: 'Ice Ledger',
  glyph: '§',
  family: 'economy',
  biomes: ['frostReliquary'],
  text: 'Elite kills grant 10 extra cash per stack.'
}, {
  id: 'frozenFocus',
  name: 'Frozen Focus',
  glyph: '◌',
  family: 'spellcraft',
  biomes: ['frostReliquary'],
  text: 'A charm cast while shielded costs 1 less focus per stack.'
}, {
  id: 'thawStep',
  name: 'Thaw Step',
  glyph: '∨',
  family: 'terrain',
  biomes: ['frostReliquary'],
  text: 'Frost rime damage is reduced by 1 per stack.'
}, {
  id: 'reliquaryEcho',
  name: 'Reliquary Echo',
  glyph: 'o',
  family: 'terrain',
  biomes: ['frostReliquary'],
  text: 'Resolving an encounter grants 1 shield per stack.'
}, {
  id: 'lastWinter',
  name: 'Last Winter',
  glyph: '†',
  family: 'combat',
  biomes: ['frostReliquary'],
  text: 'At 25% HP, attacks gain +3 damage per stack; suppresses consumable recovery.',
  suppresses: ['consumable'],
  rare: true
}];
const toolById = Object.fromEntries(TOOLS.map(tool => [tool.id, tool]));
const boonById = Object.fromEntries(BOONS.map(boon => [boon.id, boon]));
const KAMI_BOONS = new Set(['timeKnot', 'rootedResolve', 'watchfulStep', 'mapMoss', 'quietTide', 'spiritKindling', 'lastLight', 'wallSong', 'wardMemory', 'echoDividend', 'sunstep', 'mirageMap', 'whiteRoad', 'sunsetCircuit', 'coldRead', 'rimeGuard', 'duelistOath', 'shatterMark', 'frozenFocus', 'reliquaryEcho', 'lastWinter']);
export const boonAlignment = id => KAMI_BOONS.has(id) ? 'kami' : 'villagePact';
const drillable = new Set(['wall', 'rubble', 'bramble', 'boulder']);
const hazardous = new Set(['pit', 'water', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder', 'bramble', 'rubble', 'brine', 'frostRime']);
export const toolFor = id => toolById[id];
export const acquireOptionalTraversalTool = (state, tool) => {
  var _state$hero$traversal, _state$hero$cooldowns;
  const tools = [...((_state$hero$traversal = state.hero.traversalTools) !== null && _state$hero$traversal !== void 0 ? _state$hero$traversal : [])];
  if (tools.includes(tool)) return {
    result: 'duplicate'
  };
  if (tools.length < 2) {
    state.hero.traversalTools = [...tools, tool];
    return {
      result: 'bound'
    };
  }
  const replaced = tools.shift();
  state.hero.traversalTools = [...tools, tool];
  (_state$hero$cooldowns = state.hero.cooldowns) === null || _state$hero$cooldowns === void 0 || delete _state$hero$cooldowns[`tool:${replaced}`];
  return {
    result: 'replaced',
    replaced
  };
};
export const boonFor = id => boonById[id];
export const boonRank = (state, id) => {
  var _state$hero$boons, _state$hero$boons$id, _state$hero$boons3, _state$hero$boonEvolu, _state$hero$boonEvolu2;
  const boon = boonById[id];
  if (!boon) return 0;
  const suppressed = Object.keys((_state$hero$boons = state.hero.boons) !== null && _state$hero$boons !== void 0 ? _state$hero$boons : {}).some(ownerId => {
    var _state$hero$boons$own, _state$hero$boons2, _owner$suppresses;
    const owner = boonById[ownerId];
    return ownerId !== id && ((_state$hero$boons$own = (_state$hero$boons2 = state.hero.boons) === null || _state$hero$boons2 === void 0 ? void 0 : _state$hero$boons2[ownerId]) !== null && _state$hero$boons$own !== void 0 ? _state$hero$boons$own : 0) > 0 && (owner === null || owner === void 0 || (_owner$suppresses = owner.suppresses) === null || _owner$suppresses === void 0 ? void 0 : _owner$suppresses.includes(boon.family));
  });
  return suppressed ? 0 : ((_state$hero$boons$id = (_state$hero$boons3 = state.hero.boons) === null || _state$hero$boons3 === void 0 ? void 0 : _state$hero$boons3[id]) !== null && _state$hero$boons$id !== void 0 ? _state$hero$boons$id : 0) + ((_state$hero$boonEvolu = (_state$hero$boonEvolu2 = state.hero.boonEvolutions) === null || _state$hero$boonEvolu2 === void 0 ? void 0 : _state$hero$boonEvolu2[id]) !== null && _state$hero$boonEvolu !== void 0 ? _state$hero$boonEvolu : 0);
};
export const hasBoon = (state, id) => boonRank(state, id) > 0;
export const toolCooldown = (state, id) => {
  var _state$hero$cooldowns2, _state$hero$cooldowns3;
  return (_state$hero$cooldowns2 = (_state$hero$cooldowns3 = state.hero.cooldowns) === null || _state$hero$cooldowns3 === void 0 ? void 0 : _state$hero$cooldowns3[`tool:${id}`]) !== null && _state$hero$cooldowns2 !== void 0 ? _state$hero$cooldowns2 : 0;
};
export const toolChoices = (state, milestone) => {
  const offer = rewardOfferFor(state.floor, milestone.rewardKey);
  if ((offer === null || offer === void 0 ? void 0 : offer.kind) === 'waycache') {
    const choices = offer.choices.map(choice => toolById[choice.id]).filter(choice => Boolean(choice));
    if (choices.length === 3 && new Set(choices.map(choice => choice.id)).size === 3) return choices;
  }
  return rngFor(state.seed, 'progression', state.floor.index, milestone.id, 'tools').shuffle([...TOOLS]).slice(0, 3);
};
export const boonChoices = (state, milestone) => {
  var _state$hero$boons4;
  const offer = rewardOfferFor(state.floor, milestone.rewardKey);
  if ((offer === null || offer === void 0 ? void 0 : offer.kind) === 'boon') {
    const choices = offer.choices.map(choice => boonById[choice.id]).filter(choice => Boolean(choice));
    if (choices.length === 3 && new Set(choices.map(choice => choice.id)).size === 3) return choices;
  }
  const owned = new Set(Object.keys((_state$hero$boons4 = state.hero.boons) !== null && _state$hero$boons4 !== void 0 ? _state$hero$boons4 : {}));
  const families = new Set([...owned].map(id => {
    var _boonById$id;
    return (_boonById$id = boonById[id]) === null || _boonById$id === void 0 ? void 0 : _boonById$id.family;
  }).filter(Boolean));
  const shuffled = rngFor(state.seed, 'progression', state.floor.index, milestone.id, 'boons').shuffle([...BOONS]);
  const ranked = shuffled.sort((a, b) => Number(owned.has(b.id)) - Number(owned.has(a.id)) || Number(families.has(b.family)) - Number(families.has(a.family)));
  const local = ranked.filter(boon => {
    var _boon$biomes;
    return (_boon$biomes = boon.biomes) === null || _boon$biomes === void 0 ? void 0 : _boon$biomes.includes(state.floor.biome);
  });
  const global = ranked.filter(boon => {
    var _boon$biomes2;
    return !((_boon$biomes2 = boon.biomes) !== null && _boon$biomes2 !== void 0 && _boon$biomes2.includes(state.floor.biome));
  });
  return [...local.slice(0, 2), ...global].slice(0, 3);
};
const milestoneAtReach = state => state.floor.milestones.find(milestone => {
  var _state$hero$boons5;
  return !milestone.claimed && (milestone.kind !== 'augment' || Object.values((_state$hero$boons5 = state.hero.boons) !== null && _state$hero$boons5 !== void 0 ? _state$hero$boons5 : {}).some(rank => (rank !== null && rank !== void 0 ? rank : 0) > 0)) && Math.max(Math.abs(milestone.x - state.hero.x), Math.abs(milestone.y - state.hero.y)) <= 1;
});
export function openMilestone(state) {
  const milestone = milestoneAtReach(state);
  if (!milestone) return undefined;
  milestone.discovered = true;
  state.modal = milestone.kind === 'waycache' ? {
    kind: 'tool',
    milestoneId: milestone.id
  } : milestone.kind === 'augment' ? {
    kind: 'augment',
    milestoneId: milestone.id
  } : milestone.kind === 'relic' ? {
    kind: 'relic',
    milestoneId: milestone.id
  } : {
    kind: 'boon',
    milestoneId: milestone.id
  };
  log(state, milestone.kind === 'waycache' ? 'Waycache found: choose a ritual tool.' : milestone.kind === 'augment' ? 'Build-up moment: evolve, reforge, or transmute a Boon.' : milestone.kind === 'relic' ? 'Guardian echo found: bind one active relic.' : 'Boon site found: choose one mark.');
  return [event('menu')];
}
const milestone = (state, id) => state.floor.milestones.find(current => current.id === id && !current.claimed);
const claim = (state, current) => {
  var _state$hero$boons6;
  current.claimed = true;
  const health = boonRank(state, 'trailRations') * 2 + boonRank(state, 'duneRation') + boonRank(state, 'winterRations');
  const focus = boonRank(state, 'spiritKindling') + boonRank(state, 'winterRations');
  const cash = boonRank(state, 'cacheSense') * 8 + boonRank(state, 'blackLedger') * 12 + boonRank(state, 'saltLedger') * 6 + boonRank(state, 'echoCache') * new Set(Object.keys((_state$hero$boons6 = state.hero.boons) !== null && _state$hero$boons6 !== void 0 ? _state$hero$boons6 : {}).map(id => {
    var _boonById$id2;
    return (_boonById$id2 = boonById[id]) === null || _boonById$id2 === void 0 ? void 0 : _boonById$id2.family;
  }).filter(Boolean)).size * 5;
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + health);
  state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + focus);
  state.hero.gold += cash;
};
export function chooseBoon(state, milestoneId, command) {
  var _state$hero, _state$hero$boons7, _state$hero$boons$cho;
  const current = milestone(state, milestoneId);
  if (!current) return false;
  const choice = boonChoices(state, current)[Number(command) - 1];
  if (!choice) return false;
  (_state$hero$boons7 = (_state$hero = state.hero).boons) !== null && _state$hero$boons7 !== void 0 ? _state$hero$boons7 : _state$hero.boons = {};
  state.hero.boons[choice.id] = ((_state$hero$boons$cho = state.hero.boons[choice.id]) !== null && _state$hero$boons$cho !== void 0 ? _state$hero$boons$cho : 0) + 1;
  recordTelemetryCount(state, 'boonPicks', choice.id);
  claim(state, current);
  state.modal = undefined;
  log(state, `Boon: ${choice.name} · rank ${boonRank(state, choice.id)}.`);
  tend(state, boonAlignment(choice.id));
  if (hasBoon(state, 'scoutEye')) revealMilestones(state);
  return true;
}
const ownedBoonIds = state => {
  var _state$hero$boons8;
  return Object.keys((_state$hero$boons8 = state.hero.boons) !== null && _state$hero$boons8 !== void 0 ? _state$hero$boons8 : {}).filter(id => {
    var _state$hero$boons$id2, _state$hero$boons9;
    return ((_state$hero$boons$id2 = (_state$hero$boons9 = state.hero.boons) === null || _state$hero$boons9 === void 0 ? void 0 : _state$hero$boons9[id]) !== null && _state$hero$boons$id2 !== void 0 ? _state$hero$boons$id2 : 0) > 0 && boonById[id];
  }).sort();
};
const rareChoices = (state, current, selected) => rngFor(state.seed, 'progression', state.floor.index, current.id, 'transmute', selected).shuffle(BOONS.filter(boon => boon.rare)).slice(0, 3);
const reforgeChoices = (state, current, selected) => {
  const source = boonFor(selected);
  const candidates = BOONS.filter(boon => boon.id !== selected && (boon.family === source.family || boon.rare));
  return rngFor(state.seed, 'progression', state.floor.index, current.id, 'reforge', selected).shuffle(candidates).slice(0, 3);
};
export function augmentChoices(state, milestoneId, mode) {
  var _state$modal, _state$modal$selected;
  const current = milestone(state, milestoneId);
  if (!current) return [];
  const selected = ((_state$modal = state.modal) === null || _state$modal === void 0 ? void 0 : _state$modal.kind) === 'augment' ? (_state$modal$selected = state.modal.selected) === null || _state$modal$selected === void 0 ? void 0 : _state$modal$selected[0] : undefined;
  if (!selected) return ownedBoonIds(state).map(boonFor);
  if (mode === 'reforge') return reforgeChoices(state, current, selected);
  if (mode === 'transmute') return rareChoices(state, current, selected);
  return [];
}
export function chooseAugment(state, milestoneId, command) {
  var _state$modal2, _modal$selected, _state$hero$boons$sel, _state$hero$boons0, _state$hero$boonEvolu4, _choice$id;
  const current = milestone(state, milestoneId);
  const modal = ((_state$modal2 = state.modal) === null || _state$modal2 === void 0 ? void 0 : _state$modal2.kind) === 'augment' ? state.modal : undefined;
  if (!current || !modal) return false;
  const numeric = Number(command) - 1;
  if (!modal.mode) {
    const mode = ['evolve', 'reforge', 'transmute'][numeric];
    if (!mode) return false;
    state.modal = {
      ...modal,
      mode
    };
    log(state, `${mode[0].toUpperCase()}${mode.slice(1)}: choose an owned Boon.`);
    return true;
  }
  const selected = (_modal$selected = modal.selected) === null || _modal$selected === void 0 ? void 0 : _modal$selected[0];
  if (!selected) {
    const owned = ownedBoonIds(state);
    const choice = owned[numeric];
    if (!choice) return false;
    if (modal.mode === 'evolve') {
      var _state$hero2, _state$hero2$boonEvol, _state$hero$boonEvolu3;
      (_state$hero2$boonEvol = (_state$hero2 = state.hero).boonEvolutions) !== null && _state$hero2$boonEvol !== void 0 ? _state$hero2$boonEvol : _state$hero2.boonEvolutions = {};
      state.hero.boonEvolutions[choice] = ((_state$hero$boonEvolu3 = state.hero.boonEvolutions[choice]) !== null && _state$hero$boonEvolu3 !== void 0 ? _state$hero$boonEvolu3 : 0) + 1;
      recordTelemetryCount(state, 'boonAugments', `evolve:${choice}`);
      claim(state, current);
      state.modal = undefined;
      log(state, `${boonFor(choice).name} evolves to tier ${state.hero.boonEvolutions[choice]}. Its engine strengthens.`);
      return true;
    }
    state.modal = {
      ...modal,
      selected: [choice]
    };
    log(state, `Choose a ${modal.mode} result for ${boonFor(choice).name}.`);
    return true;
  }
  const choices = modal.mode === 'reforge' ? reforgeChoices(state, current, selected) : rareChoices(state, current, selected);
  const choice = choices[numeric];
  if (!choice) return false;
  const prior = (_state$hero$boons$sel = (_state$hero$boons0 = state.hero.boons) === null || _state$hero$boons0 === void 0 ? void 0 : _state$hero$boons0[selected]) !== null && _state$hero$boons$sel !== void 0 ? _state$hero$boons$sel : 0;
  if (!prior) return false;
  state.hero.boons[selected] = Math.max(0, prior - 1);
  if (state.hero.boons[selected] === 0) delete state.hero.boons[selected];
  (_state$hero$boonEvolu4 = state.hero.boonEvolutions) === null || _state$hero$boonEvolu4 === void 0 || delete _state$hero$boonEvolu4[selected];
  state.hero.boons[choice.id] = ((_choice$id = state.hero.boons[choice.id]) !== null && _choice$id !== void 0 ? _choice$id : 0) + (modal.mode === 'reforge' ? 1 : Math.max(1, prior));
  recordTelemetryCount(state, 'boonAugments', `${modal.mode}:${selected}:${choice.id}`);
  claim(state, current);
  state.modal = undefined;
  log(state, modal.mode === 'reforge' ? `${boonFor(selected).name} reforges into ${choice.name}.` : `${boonFor(selected).name} transmutes into ${choice.name}.`);
  tend(state, boonAlignment(choice.id));
  return true;
}
export function chooseTool(state, milestoneId, command) {
  var _state$hero$traversal2, _state$modal3;
  const current = milestone(state, milestoneId);
  if (!current) return false;
  const tools = (_state$hero$traversal2 = state.hero.traversalTools) !== null && _state$hero$traversal2 !== void 0 ? _state$hero$traversal2 : [];
  const modal = ((_state$modal3 = state.modal) === null || _state$modal3 === void 0 ? void 0 : _state$modal3.kind) === 'tool' ? state.modal : undefined;
  if (!modal) return false;
  if (tools.length >= 2 && modal.replace === undefined) {
    const slot = Number(command) - 1;
    if (slot < 0 || slot >= tools.length) return false;
    state.modal = {
      ...modal,
      replace: slot
    };
    log(state, `Replace ${toolFor(tools[slot]).name}; choose a Waycache tool.`);
    return true;
  }
  const choice = toolChoices(state, current)[Number(command) - 1];
  if (!choice) return false;
  if (modal.replace === undefined) tools.push(choice.id);else tools[modal.replace] = choice.id;
  state.hero.traversalTools = tools;
  claim(state, current);
  state.modal = undefined;
  log(state, `You bind ${choice.name}.`);
  return true;
}
export function chooseRelic(state, milestoneId, command) {
  var _state$modal4, _state$hero3, _state$hero3$relics, _state$hero4, _state$hero4$relicCha;
  const current = milestone(state, milestoneId);
  const modal = ((_state$modal4 = state.modal) === null || _state$modal4 === void 0 ? void 0 : _state$modal4.kind) === 'relic' ? state.modal : undefined;
  if (!current || !modal) return false;
  const relics = (_state$hero3$relics = (_state$hero3 = state.hero).relics) !== null && _state$hero3$relics !== void 0 ? _state$hero3$relics : _state$hero3.relics = [];
  if (relics.length >= 3 && modal.replace === undefined) {
    const slot = Number(command) - 1;
    if (slot < 0 || slot >= relics.length) return false;
    state.modal = {
      ...modal,
      replace: slot
    };
    log(state, `Replace ${relicFor(relics[slot]).name}; choose a guardian relic.`);
    return true;
  }
  const choice = relicChoices(state, current)[Number(command) - 1];
  if (!choice) return false;
  if (modal.replace === undefined) relics.push(choice.id);else relics[modal.replace] = choice.id;
  (_state$hero4$relicCha = (_state$hero4 = state.hero).relicCharges) !== null && _state$hero4$relicCha !== void 0 ? _state$hero4$relicCha : _state$hero4.relicCharges = {};
  for (const id of relics) if (id !== choice.id) delete state.hero.relicCharges[id];
  recordTelemetryCount(state, 'relicPicks', choice.id);
  claim(state, current);
  state.modal = undefined;
  log(state, `Relic bound: ${choice.name}.`);
  tend(state, relicAlignment(choice.id));
  return true;
}
export function openTools(state) {
  var _state$hero$traversal3;
  const tools = (_state$hero$traversal3 = state.hero.traversalTools) !== null && _state$hero$traversal3 !== void 0 ? _state$hero$traversal3 : [];
  if (!tools.length) {
    log(state, 'No ritual traversal tool is bound. Find a Waycache.');
    return [];
  }
  state.modal = {
    kind: 'tools'
  };
  return [event('menu')];
}
export function chooseToolUse(state, command) {
  var _state$hero$traversal4;
  const tool = (_state$hero$traversal4 = state.hero.traversalTools) === null || _state$hero$traversal4 === void 0 ? void 0 : _state$hero$traversal4[Number(command) - 1];
  if (!tool) return false;
  const cooldown = toolCooldown(state, tool);
  if (cooldown && !isTerrainMutationTool(tool)) {
    log(state, `${toolFor(tool).name} recovers in ${cooldown} turn${cooldown === 1 ? '' : 's'}.`);
    return false;
  }
  state.modal = {
    kind: 'target',
    action: tool,
    tool
  };
  return true;
}
const setCooldown = (state, tool) => {
  var _state$hero5, _state$hero5$cooldown;
  ((_state$hero5$cooldown = (_state$hero5 = state.hero).cooldowns) !== null && _state$hero5$cooldown !== void 0 ? _state$hero5$cooldown : _state$hero5.cooldowns = {})[`tool:${tool}`] = Math.max(1, toolFor(tool).cooldown - boonRank(state, 'coolAsh'));
};
const point = (state, direction, distance) => ({
  x: state.hero.x + DIRECTIONS[direction].x * distance,
  y: state.hero.y + DIRECTIONS[direction].y * distance
});
const passableLanding = (state, target) => isPassable(state.floor, target.x, target.y);
export function useTool(state, tool, direction, overdrive = false) {
  var _state$hero$traversal5, _state$hero$condition;
  const mutation = isTerrainMutationTool(tool) ? terrainMutationAssessment(state, tool, direction, overdrive) : undefined;
  const terrainRejectionMessage = reason => reason === 'unbound' ? 'That ritual tool is no longer bound.' : reason === 'cooldown' ? `${toolFor(tool).name} is still recovering.` : reason === 'target-unseen' ? 'That terrain target is not visible.' : reason === 'destination-unseen' ? 'That terrain destination is not visible.' : tool === 'stoneAdze' ? 'Stone Adze cuts only an adjacent wooden barrier or weakened route.' : tool === 'resinFireBasket' ? 'Resin Fire Basket needs an unoccupied adjacent bramble or web.' : tool === 'antlerPrybar' ? reason === 'destination-blocked' ? 'Antler Prybar needs an empty legal destination beyond the target.' : 'Antler Prybar needs an adjacent boulder or breakwall.' : reason === 'destination-blocked' ? 'Wooden Lever and Roller needs an empty legal destination.' : 'Wooden Lever and Roller needs an adjacent movable prop.';
  const terrainRejected = reason => {
    recordInteraction(state, 'rejectedInteractions', `terrain:${tool}:${reason}`);
    if (mutation !== null && mutation !== void 0 && mutation.routeBlocked) recordInteraction(state, 'routeFailures', `terrain:${tool}:mandatory-route`);
    log(state, `terrain:${tool}:attempted`);
    log(state, `terrain:${tool}:rejected:${reason}`);
    log(state, terrainRejectionMessage(reason));
    return [event('terrain', tool, 'attempted'), event('terrain', tool, `rejected:${reason}`)];
  };
  if (!((_state$hero$traversal5 = state.hero.traversalTools) !== null && _state$hero$traversal5 !== void 0 ? _state$hero$traversal5 : []).includes(tool)) {
    if (mutation) return terrainRejected('unbound');
    log(state, 'That ritual tool is no longer bound.');
    return [];
  }
  if (toolCooldown(state, tool)) {
    if (mutation) return terrainRejected('cooldown');
    log(state, `${toolFor(tool).name} is still recovering.`);
    return [];
  }
  if (mutation && !mutation.ready) return terrainRejected(mutation.reason);
  if (mutation) log(state, `terrain:${tool}:attempted`);
  const result = tool === 'stoneWedge' ? useStoneWedge(state, direction, overdrive) : tool === 'reedwing' ? useReedwing(state, direction, overdrive) : tool === 'ashwayRites' ? useAshway(state, direction, overdrive) : tool === 'antlerPrybar' ? useAntlerPrybar(state, direction) : tool === 'stoneAdze' ? useStoneAdze(state, direction) : tool === 'resinFireBasket' ? useResinFireBasket(state, direction, overdrive) : tool === 'cordAnchor' ? useCordAnchor(state, direction, overdrive) : useWoodenLeverRoller(state, direction);
  if (!result) return mutation ? terrainRejected('execution-failed') : [];
  recordInteraction(state, 'terrainToolUses', `${state.floor.biome}:${tool}`);
  const healing = boonRank(state, 'rootedResolve') + (state.hero.health * 4 <= state.hero.maxHealth ? boonRank(state, 'lastLight') * 2 : 0);
  if (healing) state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + healing);
  const focus = boonRank(state, 'cordTempo');
  if (focus) state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + focus);
  if (consumeRelicTool(state)) {
    state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 3);
    log(state, 'Tide Fetter restores 3 HP after the crossing.');
  }
  armRelicTraversal(state);
  if (boonRank(state, 'pressureSeal')) state.hero.conditions = [...((_state$hero$condition = state.hero.conditions) !== null && _state$hero$condition !== void 0 ? _state$hero$condition : []), {
    kind: 'shielded',
    duration: 2,
    potency: boonRank(state, 'pressureSeal')
  }];
  if (boonRank(state, 'wayfinderCord')) revealNearbyMilestones(state, 2 + boonRank(state, 'wayfinderCord'));
  if (overdrive) {
    var _state$hero$traversal6;
    state.hero.traversalTools = ((_state$hero$traversal6 = state.hero.traversalTools) !== null && _state$hero$traversal6 !== void 0 ? _state$hero$traversal6 : []).filter(current => current !== tool);
    log(state, `${toolFor(tool).name} burns out after its overdrive.`);
  } else {
    setCooldown(state, tool);
    const cooldown = toolCooldown(state, tool);
    const relay = boonRank(state, 'relayStep');
    if (relay && cooldown) state.hero.cooldowns[`tool:${tool}`] = Math.max(1, cooldown - relay);
  }
  if (mutation) {
    log(state, `terrain:${tool}:resolved:${mutation.reason}`);
    if (mutation.risk) log(state, `terrain:${tool}:hazard:${mutation.risk}`);
  }
  refreshFov(state);
  return advance(state, [event('spell'), ...(mutation ? [event('terrain', tool, 'attempted'), event('terrain', tool, 'resolved'), ...(mutation.risk ? [event('terrain', tool, `hazard:${mutation.risk}`)] : []), ...(overdrive ? [event('terrain', tool, 'retired')] : [])] : [])]);
}
const useStoneWedge = (state, direction, overdrive) => {
  const target = point(state, direction, 1);
  const width = overdrive ? 1 : boonRank(state, 'stoneMemory') + boonRank(state, 'wallSong');
  const offsets = Array.from({
    length: width * 2 + 1
  }, (_, index) => index - width).map(offset => [DIRECTIONS[direction].y * offset, -DIRECTIONS[direction].x * offset]);
  const cleared = offsets.map(([x, y]) => getTile(state.floor, target.x + x, target.y + y)).filter(tile => Boolean(tile && drillable.has(tile.kind)));
  if (!cleared.length) {
    log(state, 'Stone Wedge needs blocked ground.');
    return false;
  }
  cleared.forEach(tile => {
    tile.kind = 'floor';
  });
  log(state, overdrive ? 'The wedge opens a broad passage.' : 'The wedge opens a narrow passage.');
  return true;
};
const useReedwing = (state, direction, overdrive) => {
  const length = (overdrive ? 3 : 2) + boonRank(state, 'reedMemory') + boonRank(state, 'updraftStep');
  const landing = point(state, direction, length);
  const crossed = Array.from({
    length: length - 1
  }, (_, index) => getTile(state.floor, point(state, direction, index + 1).x, point(state, direction, index + 1).y));
  if (!crossed.some(tile => tile && hazardous.has(tile.kind)) || !passableLanding(state, landing)) {
    log(state, 'Reedwing needs hazardous ground and a clear landing.');
    return false;
  }
  state.hero.x = landing.x;
  state.hero.y = landing.y;
  log(state, 'Reedwing carries you across the hazard.');
  return true;
};
const useCordAnchor = (state, direction, overdrive) => {
  const landing = point(state, direction, overdrive ? 4 : 2);
  if (!passableLanding(state, landing)) {
    log(state, 'Cord Anchor needs a clear landing.');
    return false;
  }
  state.hero.x = landing.x;
  state.hero.y = landing.y;
  log(state, 'The cord draws you across the gap.');
  return true;
};
const useAshway = (state, direction, overdrive) => {
  var _state$floor, _state$floor$transien;
  const length = overdrive ? 3 : 2;
  const cells = Array.from({
    length
  }, (_, index) => point(state, direction, index + 1));
  const targets = cells.map(cell => ({
    cell,
    tile: getTile(state.floor, cell.x, cell.y)
  })).filter(entry => Boolean(entry.tile && hazardous.has(entry.tile.kind) && entry.tile.kind !== 'boulder'));
  if (!targets.length) {
    log(state, 'Ashway Rites need hazardous ground to bind.');
    return false;
  }
  (_state$floor$transien = (_state$floor = state.floor).transientTerrain) !== null && _state$floor$transien !== void 0 ? _state$floor$transien : _state$floor.transientTerrain = [];
  targets.forEach(({
    cell,
    tile
  }) => {
    const existing = state.floor.transientTerrain.find(current => current.x === cell.x && current.y === cell.y);
    if (!existing) state.floor.transientTerrain.push({
      ...cell,
      original: tile.kind,
      ...(tile.flow ? {
        originalFlow: {
          ...tile.flow
        }
      } : {}),
      expiresAt: state.turn + 6
    });
    tile.kind = 'floor';
    delete tile.flow;
  });
  log(state, 'Warm ash settles into a temporary route.');
  return true;
};
const useAntlerPrybar = (state, direction) => {
  var _state$hero$condition2;
  const target = point(state, direction, 1);
  const destination = point(state, direction, 2);
  const tile = getTile(state.floor, target.x, target.y);
  const landing = getTile(state.floor, destination.x, destination.y);
  const protectedDestination = destination.x === state.floor.start.x && destination.y === state.floor.start.y || destination.x === state.floor.exit.x && destination.y === state.floor.exit.y || state.floor.actors.some(actor => actor.health > 0 && actor.x === destination.x && actor.y === destination.y) || state.floor.milestones.some(milestone => !milestone.claimed && milestone.x === destination.x && milestone.y === destination.y);
  if (!tile || !['boulder', 'breakwall'].includes(tile.kind)) {
    log(state, 'Antler Prybar needs an adjacent boulder or breakwall.');
    return false;
  }
  if (!landing || landing.kind !== 'floor' || protectedDestination || state.floor.props.some(prop => prop.state !== 'destroyed' && prop.x === destination.x && prop.y === destination.y)) {
    log(state, 'Antler Prybar needs an empty legal destination beyond the target.');
    return false;
  }
  landing.kind = tile.kind;
  tile.kind = 'floor';
  state.hero.conditions = [...((_state$hero$condition2 = state.hero.conditions) !== null && _state$hero$condition2 !== void 0 ? _state$hero$condition2 : []), {
    kind: 'marked',
    duration: 2,
    potency: 1
  }];
  log(state, 'The Antler Prybar shifts the barrier; the noise leaves you exposed.');
  return true;
};
const useStoneAdze = (state, direction) => {
  const target = point(state, direction, 1);
  const tile = getTile(state.floor, target.x, target.y);
  if (!tile || !['crate', 'crumble'].includes(tile.kind)) {
    log(state, 'Stone Adze cuts only an adjacent wooden barrier or weakened route.');
    return false;
  }
  tile.kind = 'floor';
  log(state, 'The Stone Adze cuts a narrow route through the weakened barrier.');
  return true;
};
const useResinFireBasket = (state, direction, overdrive) => {
  var _state$hero$condition3;
  const target = point(state, direction, 1);
  const tile = getTile(state.floor, target.x, target.y);
  if (!tile || !['bramble', 'web'].includes(tile.kind) || state.floor.actors.some(actor => actor.health > 0 && actor.x === target.x && actor.y === target.y) || target.x === state.floor.exit.x && target.y === state.floor.exit.y) {
    log(state, 'Resin Fire Basket needs an unoccupied adjacent bramble or web.');
    return false;
  }
  tile.kind = 'smoke';
  state.hero.conditions = [...((_state$hero$condition3 = state.hero.conditions) !== null && _state$hero$condition3 !== void 0 ? _state$hero$condition3 : []), {
    kind: 'burning',
    duration: overdrive ? 3 : 2,
    potency: 1
  }];
  log(state, overdrive ? 'The resin flares wide; smoke and heat cling to you.' : 'The resin burns the growth into a smoking, risky crossing.');
  return true;
};
const useWoodenLeverRoller = (state, direction) => {
  const target = point(state, direction, 1);
  const destination = point(state, direction, 2);
  const prop = state.floor.props.find(candidate => candidate.x === target.x && candidate.y === target.y && candidate.state !== 'destroyed' && ['mine.brokenCart', 'caverns.brokenBoat', 'ruins.collapsedArch'].includes(candidate.kind));
  const tile = getTile(state.floor, destination.x, destination.y);
  const protectedDestination = destination.x === state.floor.start.x && destination.y === state.floor.start.y || destination.x === state.floor.exit.x && destination.y === state.floor.exit.y || state.floor.actors.some(actor => actor.health > 0 && actor.x === destination.x && actor.y === destination.y) || state.floor.milestones.some(milestone => !milestone.claimed && milestone.x === destination.x && milestone.y === destination.y) || state.floor.props.some(candidate => candidate.state !== 'destroyed' && candidate.x === destination.x && candidate.y === destination.y);
  if (!prop) {
    log(state, 'Wooden Lever and Roller needs an adjacent movable prop.');
    return false;
  }
  if (!tile || tile.kind !== 'floor' || protectedDestination) {
    log(state, 'Wooden Lever and Roller needs an empty legal destination.');
    return false;
  }
  prop.x = destination.x;
  prop.y = destination.y;
  log(state, 'The lever rolls the prop forward, changing the route.');
  return true;
};
export function expireAshways(state) {
  var _state$floor$transien2;
  const pending = (_state$floor$transien2 = state.floor.transientTerrain) !== null && _state$floor$transien2 !== void 0 ? _state$floor$transien2 : [];
  const active = pending.filter(current => {
    if (current.expiresAt > state.turn || state.hero.x === current.x && state.hero.y === current.y) return true;
    const tile = getTile(state.floor, current.x, current.y);
    if ((tile === null || tile === void 0 ? void 0 : tile.kind) === 'floor') {
      tile.kind = current.original;
      if (current.originalFlow) tile.flow = {
        ...current.originalFlow
      };
    }
    return false;
  });
  state.floor.transientTerrain = active.length ? active : undefined;
}
export function revealMilestones(state) {
  for (const current of state.floor.milestones) if (!current.claimed) current.discovered = true;
}
export function revealNearbyMilestones(state, radius) {
  for (const current of state.floor.milestones) if (!current.claimed && Math.max(Math.abs(current.x - state.hero.x), Math.abs(current.y - state.hero.y)) <= radius) current.discovered = true;
}
export function useTimeKnot(state) {
  var _state$hero$safePosit, _state$hero$boons$tim, _state$hero$boons1, _state$hero$boonEvolu5;
  const rank = boonRank(state, 'timeKnot');
  const positions = (_state$hero$safePosit = state.hero.safePositions) !== null && _state$hero$safePosit !== void 0 ? _state$hero$safePosit : [];
  if (!rank) {
    log(state, 'No Time Knot is bound.');
    return [];
  }
  const current = `${state.hero.x},${state.hero.y}`;
  const target = [...positions].reverse().find(point => `${point.x},${point.y}` !== current && isPassable(state.floor, point.x, point.y));
  if (!target) {
    log(state, 'Time Knot has no earlier safe position.');
    return [];
  }
  state.hero.x = target.x;
  state.hero.y = target.y;
  const base = (_state$hero$boons$tim = (_state$hero$boons1 = state.hero.boons) === null || _state$hero$boons1 === void 0 ? void 0 : _state$hero$boons1.timeKnot) !== null && _state$hero$boons$tim !== void 0 ? _state$hero$boons$tim : 0;
  if (base > 0) {
    state.hero.boons.timeKnot = base - 1;
    if (state.hero.boons.timeKnot === 0) delete state.hero.boons.timeKnot;
  } else if ((_state$hero$boonEvolu5 = state.hero.boonEvolutions) !== null && _state$hero$boonEvolu5 !== void 0 && _state$hero$boonEvolu5.timeKnot) state.hero.boonEvolutions.timeKnot--;
  refreshFov(state);
  log(state, 'Time Knot returns you to an earlier safe position. The world does not rewind.');
  return [event('spell')];
}
export function recordSafePosition(state) {
  var _state$hero6, _state$hero6$safePosi;
  if (!isPassable(state.floor, state.hero.x, state.hero.y)) return;
  const positions = (_state$hero6$safePosi = (_state$hero6 = state.hero).safePositions) !== null && _state$hero6$safePosi !== void 0 ? _state$hero6$safePosi : _state$hero6.safePositions = [];
  const last = positions.at(-1);
  if (!last || last.x !== state.hero.x || last.y !== state.hero.y) positions.push({
    x: state.hero.x,
    y: state.hero.y
  });
  if (positions.length > 12 + boonRank(state, 'borrowedTime')) positions.shift();
}
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJESVJFQ1RJT05TIiwicm5nRm9yIiwicmV3YXJkT2ZmZXJGb3IiLCJnZXRUaWxlIiwiaXNQYXNzYWJsZSIsImFkdmFuY2UiLCJldmVudCIsImxvZyIsInJlZnJlc2hGb3YiLCJyZWNvcmRJbnRlcmFjdGlvbiIsInJlY29yZFRlbGVtZXRyeUNvdW50IiwiYXJtUmVsaWNUcmF2ZXJzYWwiLCJjb25zdW1lUmVsaWNUb29sIiwicmVsaWNBbGlnbm1lbnQiLCJyZWxpY0Nob2ljZXMiLCJyZWxpY0ZvciIsInRlbmQiLCJpc1RlcnJhaW5NdXRhdGlvblRvb2wiLCJ0ZXJyYWluTXV0YXRpb25Bc3Nlc3NtZW50IiwiVE9PTFMiLCJpZCIsIm5hbWUiLCJnbHlwaCIsImNvb2xkb3duIiwidGV4dCIsIm92ZXJkcml2ZSIsIkJPT05TIiwiZmFtaWx5IiwicmFyZSIsInN1cHByZXNzZXMiLCJiaW9tZXMiLCJ0b29sQnlJZCIsIk9iamVjdCIsImZyb21FbnRyaWVzIiwibWFwIiwidG9vbCIsImJvb25CeUlkIiwiYm9vbiIsIktBTUlfQk9PTlMiLCJTZXQiLCJib29uQWxpZ25tZW50IiwiaGFzIiwiZHJpbGxhYmxlIiwiaGF6YXJkb3VzIiwidG9vbEZvciIsImFjcXVpcmVPcHRpb25hbFRyYXZlcnNhbFRvb2wiLCJzdGF0ZSIsIl9zdGF0ZSRoZXJvJHRyYXZlcnNhbCIsIl9zdGF0ZSRoZXJvJGNvb2xkb3ducyIsInRvb2xzIiwiaGVybyIsInRyYXZlcnNhbFRvb2xzIiwiaW5jbHVkZXMiLCJyZXN1bHQiLCJsZW5ndGgiLCJyZXBsYWNlZCIsInNoaWZ0IiwiY29vbGRvd25zIiwiYm9vbkZvciIsImJvb25SYW5rIiwiX3N0YXRlJGhlcm8kYm9vbnMiLCJfc3RhdGUkaGVybyRib29ucyRpZCIsIl9zdGF0ZSRoZXJvJGJvb25zMyIsIl9zdGF0ZSRoZXJvJGJvb25Fdm9sdSIsIl9zdGF0ZSRoZXJvJGJvb25Fdm9sdTIiLCJzdXBwcmVzc2VkIiwia2V5cyIsImJvb25zIiwic29tZSIsIm93bmVySWQiLCJfc3RhdGUkaGVybyRib29ucyRvd24iLCJfc3RhdGUkaGVybyRib29uczIiLCJfb3duZXIkc3VwcHJlc3NlcyIsIm93bmVyIiwiYm9vbkV2b2x1dGlvbnMiLCJoYXNCb29uIiwidG9vbENvb2xkb3duIiwiX3N0YXRlJGhlcm8kY29vbGRvd25zMiIsIl9zdGF0ZSRoZXJvJGNvb2xkb3duczMiLCJ0b29sQ2hvaWNlcyIsIm1pbGVzdG9uZSIsIm9mZmVyIiwiZmxvb3IiLCJyZXdhcmRLZXkiLCJraW5kIiwiY2hvaWNlcyIsImNob2ljZSIsImZpbHRlciIsIkJvb2xlYW4iLCJzaXplIiwic2VlZCIsImluZGV4Iiwic2h1ZmZsZSIsInNsaWNlIiwiYm9vbkNob2ljZXMiLCJfc3RhdGUkaGVybyRib29uczQiLCJvd25lZCIsImZhbWlsaWVzIiwiX2Jvb25CeUlkJGlkIiwic2h1ZmZsZWQiLCJyYW5rZWQiLCJzb3J0IiwiYSIsImIiLCJOdW1iZXIiLCJsb2NhbCIsIl9ib29uJGJpb21lcyIsImJpb21lIiwiZ2xvYmFsIiwiX2Jvb24kYmlvbWVzMiIsIm1pbGVzdG9uZUF0UmVhY2giLCJtaWxlc3RvbmVzIiwiZmluZCIsIl9zdGF0ZSRoZXJvJGJvb25zNSIsImNsYWltZWQiLCJ2YWx1ZXMiLCJyYW5rIiwiTWF0aCIsIm1heCIsImFicyIsIngiLCJ5Iiwib3Blbk1pbGVzdG9uZSIsInVuZGVmaW5lZCIsImRpc2NvdmVyZWQiLCJtb2RhbCIsIm1pbGVzdG9uZUlkIiwiY3VycmVudCIsImNsYWltIiwiX3N0YXRlJGhlcm8kYm9vbnM2IiwiaGVhbHRoIiwiZm9jdXMiLCJjYXNoIiwiX2Jvb25CeUlkJGlkMiIsIm1pbiIsIm1heEhlYWx0aCIsIm1heEZvY3VzIiwiZ29sZCIsImNob29zZUJvb24iLCJjb21tYW5kIiwiX3N0YXRlJGhlcm8iLCJfc3RhdGUkaGVybyRib29uczciLCJfc3RhdGUkaGVybyRib29ucyRjaG8iLCJyZXZlYWxNaWxlc3RvbmVzIiwib3duZWRCb29uSWRzIiwiX3N0YXRlJGhlcm8kYm9vbnM4IiwiX3N0YXRlJGhlcm8kYm9vbnMkaWQyIiwiX3N0YXRlJGhlcm8kYm9vbnM5IiwicmFyZUNob2ljZXMiLCJzZWxlY3RlZCIsInJlZm9yZ2VDaG9pY2VzIiwic291cmNlIiwiY2FuZGlkYXRlcyIsImF1Z21lbnRDaG9pY2VzIiwibW9kZSIsIl9zdGF0ZSRtb2RhbCIsIl9zdGF0ZSRtb2RhbCRzZWxlY3RlZCIsImNob29zZUF1Z21lbnQiLCJfc3RhdGUkbW9kYWwyIiwiX21vZGFsJHNlbGVjdGVkIiwiX3N0YXRlJGhlcm8kYm9vbnMkc2VsIiwiX3N0YXRlJGhlcm8kYm9vbnMwIiwiX3N0YXRlJGhlcm8kYm9vbkV2b2x1NCIsIl9jaG9pY2UkaWQiLCJudW1lcmljIiwidG9VcHBlckNhc2UiLCJfc3RhdGUkaGVybzIiLCJfc3RhdGUkaGVybzIkYm9vbkV2b2wiLCJfc3RhdGUkaGVybyRib29uRXZvbHUzIiwicHJpb3IiLCJjaG9vc2VUb29sIiwiX3N0YXRlJGhlcm8kdHJhdmVyc2FsMiIsIl9zdGF0ZSRtb2RhbDMiLCJyZXBsYWNlIiwic2xvdCIsInB1c2giLCJjaG9vc2VSZWxpYyIsIl9zdGF0ZSRtb2RhbDQiLCJfc3RhdGUkaGVybzMiLCJfc3RhdGUkaGVybzMkcmVsaWNzIiwiX3N0YXRlJGhlcm80IiwiX3N0YXRlJGhlcm80JHJlbGljQ2hhIiwicmVsaWNzIiwicmVsaWNDaGFyZ2VzIiwib3BlblRvb2xzIiwiX3N0YXRlJGhlcm8kdHJhdmVyc2FsMyIsImNob29zZVRvb2xVc2UiLCJfc3RhdGUkaGVybyR0cmF2ZXJzYWw0IiwiYWN0aW9uIiwic2V0Q29vbGRvd24iLCJfc3RhdGUkaGVybzUiLCJfc3RhdGUkaGVybzUkY29vbGRvd24iLCJwb2ludCIsImRpcmVjdGlvbiIsImRpc3RhbmNlIiwicGFzc2FibGVMYW5kaW5nIiwidGFyZ2V0IiwidXNlVG9vbCIsIl9zdGF0ZSRoZXJvJHRyYXZlcnNhbDUiLCJfc3RhdGUkaGVybyRjb25kaXRpb24iLCJtdXRhdGlvbiIsInRlcnJhaW5SZWplY3Rpb25NZXNzYWdlIiwicmVhc29uIiwidGVycmFpblJlamVjdGVkIiwicm91dGVCbG9ja2VkIiwicmVhZHkiLCJ1c2VTdG9uZVdlZGdlIiwidXNlUmVlZHdpbmciLCJ1c2VBc2h3YXkiLCJ1c2VBbnRsZXJQcnliYXIiLCJ1c2VTdG9uZUFkemUiLCJ1c2VSZXNpbkZpcmVCYXNrZXQiLCJ1c2VDb3JkQW5jaG9yIiwidXNlV29vZGVuTGV2ZXJSb2xsZXIiLCJoZWFsaW5nIiwiY29uZGl0aW9ucyIsImR1cmF0aW9uIiwicG90ZW5jeSIsInJldmVhbE5lYXJieU1pbGVzdG9uZXMiLCJfc3RhdGUkaGVybyR0cmF2ZXJzYWw2IiwicmVsYXkiLCJyaXNrIiwid2lkdGgiLCJvZmZzZXRzIiwiQXJyYXkiLCJmcm9tIiwiXyIsIm9mZnNldCIsImNsZWFyZWQiLCJ0aWxlIiwiZm9yRWFjaCIsImxhbmRpbmciLCJjcm9zc2VkIiwiX3N0YXRlJGZsb29yIiwiX3N0YXRlJGZsb29yJHRyYW5zaWVuIiwiY2VsbHMiLCJ0YXJnZXRzIiwiY2VsbCIsImVudHJ5IiwidHJhbnNpZW50VGVycmFpbiIsImV4aXN0aW5nIiwib3JpZ2luYWwiLCJmbG93Iiwib3JpZ2luYWxGbG93IiwiZXhwaXJlc0F0IiwidHVybiIsIl9zdGF0ZSRoZXJvJGNvbmRpdGlvbjIiLCJkZXN0aW5hdGlvbiIsInByb3RlY3RlZERlc3RpbmF0aW9uIiwic3RhcnQiLCJleGl0IiwiYWN0b3JzIiwiYWN0b3IiLCJwcm9wcyIsInByb3AiLCJfc3RhdGUkaGVybyRjb25kaXRpb24zIiwiY2FuZGlkYXRlIiwiZXhwaXJlQXNod2F5cyIsIl9zdGF0ZSRmbG9vciR0cmFuc2llbjIiLCJwZW5kaW5nIiwiYWN0aXZlIiwicmFkaXVzIiwidXNlVGltZUtub3QiLCJfc3RhdGUkaGVybyRzYWZlUG9zaXQiLCJfc3RhdGUkaGVybyRib29ucyR0aW0iLCJfc3RhdGUkaGVybyRib29uczEiLCJfc3RhdGUkaGVybyRib29uRXZvbHU1IiwicG9zaXRpb25zIiwic2FmZVBvc2l0aW9ucyIsInJldmVyc2UiLCJiYXNlIiwidGltZUtub3QiLCJyZWNvcmRTYWZlUG9zaXRpb24iLCJfc3RhdGUkaGVybzYiLCJfc3RhdGUkaGVybzYkc2FmZVBvc2kiLCJsYXN0IiwiYXQiXSwic291cmNlcyI6WyJidWlsZGNyYWZ0LnRzIl0sInNvdXJjZXNDb250ZW50IjpbImltcG9ydCB7IERJUkVDVElPTlMsIHR5cGUgQWxpZ25tZW50LCB0eXBlIEJpb21lLCB0eXBlIEJvb25JZCwgdHlwZSBGbG9vck1pbGVzdG9uZSwgdHlwZSBSdW5TdGF0ZSwgdHlwZSBUcmF2ZXJzYWxUb29sSWQgfSBmcm9tICcuLi90eXBlcydcbmltcG9ydCB7IHJuZ0ZvciB9IGZyb20gJy4uL3JuZydcbmltcG9ydCB7IHJld2FyZE9mZmVyRm9yIH0gZnJvbSAnLi4vcmV3YXJkLWNvbnRyYWN0J1xuaW1wb3J0IHsgZ2V0VGlsZSwgaXNQYXNzYWJsZSB9IGZyb20gJy4uL3dvcmxkJ1xuaW1wb3J0IHsgYWR2YW5jZSB9IGZyb20gJy4vY29tYmF0J1xuaW1wb3J0IHsgZXZlbnQsIGxvZywgdHlwZSBBY3Rpb25SZXN1bHQgfSBmcm9tICcuL3NoYXJlZCdcbmltcG9ydCB7IHJlZnJlc2hGb3YgfSBmcm9tICcuL3Zpc2liaWxpdHknXG5pbXBvcnQgeyByZWNvcmRJbnRlcmFjdGlvbiwgcmVjb3JkVGVsZW1ldHJ5Q291bnQgfSBmcm9tICcuLi90ZWxlbWV0cnknXG5pbXBvcnQgeyBhcm1SZWxpY1RyYXZlcnNhbCwgY29uc3VtZVJlbGljVG9vbCwgcmVsaWNBbGlnbm1lbnQsIHJlbGljQ2hvaWNlcywgcmVsaWNGb3IgfSBmcm9tICcuL3JlbGljcydcbmltcG9ydCB7IHRlbmQgfSBmcm9tICcuL2FsaWdubWVudCdcbmltcG9ydCB7IGlzVGVycmFpbk11dGF0aW9uVG9vbCwgdGVycmFpbk11dGF0aW9uQXNzZXNzbWVudCwgdHlwZSBUZXJyYWluTXV0YXRpb25SZWFzb24gfSBmcm9tICcuL3RlcnJhaW4tbXV0YXRpb25zJ1xuXG5leHBvcnQgaW50ZXJmYWNlIFRyYXZlcnNhbFRvb2wgeyBpZDogVHJhdmVyc2FsVG9vbElkOyBuYW1lOiBzdHJpbmc7IGdseXBoOiBzdHJpbmc7IGNvb2xkb3duOiBudW1iZXI7IHRleHQ6IHN0cmluZzsgb3ZlcmRyaXZlOiBzdHJpbmcgfVxuZXhwb3J0IHR5cGUgQm9vbkZhbWlseSA9ICd0cmF2ZXJzYWwnIHwgJ2NvbWJhdCcgfCAncmVjb3ZlcnknIHwgJ3Njb3V0aW5nJyB8ICdzcGVsbGNyYWZ0JyB8ICdlY29ub215JyB8ICd0ZXJyYWluJyB8ICdjb25zdW1hYmxlJ1xuZXhwb3J0IGludGVyZmFjZSBCb29uIHsgaWQ6IEJvb25JZDsgbmFtZTogc3RyaW5nOyBnbHlwaDogc3RyaW5nOyB0ZXh0OiBzdHJpbmc7IGZhbWlseTogQm9vbkZhbWlseTsgYmlvbWVzPzogcmVhZG9ubHkgQmlvbWVbXTsgc3VwcHJlc3Nlcz86IHJlYWRvbmx5IEJvb25GYW1pbHlbXTsgcmFyZT86IGJvb2xlYW4gfVxuXG5leHBvcnQgY29uc3QgVE9PTFM6IHJlYWRvbmx5IFRyYXZlcnNhbFRvb2xbXSA9IFtcbiAgeyBpZDogJ3N0b25lV2VkZ2UnLCBuYW1lOiAnU3RvbmUgV2VkZ2UnLCBnbHlwaDogJ1cnLCBjb29sZG93bjogNiwgdGV4dDogJ0JyZWFjaCBvbmUgYWRqYWNlbnQgYmxvY2tlci4nLCBvdmVyZHJpdmU6ICdCcmVhY2ggYSB0aHJlZS10aWxlIHdlZGdlLCB0aGVuIHJldGlyZS4nIH0sXG4gIHsgaWQ6ICdyZWVkd2luZycsIG5hbWU6ICdSZWVkd2luZycsIGdseXBoOiAnXicsIGNvb2xkb3duOiA1LCB0ZXh0OiAnQ3Jvc3Mgb25lIGhhemFyZG91cyB0aWxlIHRvIGEgY2xlYXIgbGFuZGluZy4nLCBvdmVyZHJpdmU6ICdDcm9zcyB1cCB0byB0d28gaGF6YXJkcywgdGhlbiByZXRpcmUuJyB9LFxuICB7IGlkOiAnY29yZEFuY2hvcicsIG5hbWU6ICdDb3JkIEFuY2hvcicsIGdseXBoOiAn4oyBJywgY29vbGRvd246IDUsIHRleHQ6ICdQdWxsIHR3byB0aWxlcyB0byBhIGNsZWFyIGxhbmRpbmcuJywgb3ZlcmRyaXZlOiAnUHVsbCBmb3VyIHRpbGVzLCB0aGVuIHJldGlyZS4nIH0sXG4gIHsgaWQ6ICdhc2h3YXlSaXRlcycsIG5hbWU6ICdBc2h3YXkgUml0ZXMnLCBnbHlwaDogJ+KJiCcsIGNvb2xkb3duOiA4LCB0ZXh0OiAnTWFrZSBhIHR3by10aWxlIHRlbXBvcmFyeSBzYWZlIHBhdGguJywgb3ZlcmRyaXZlOiAnTWFrZSBhIHRocmVlLXRpbGUgcGF0aCwgdGhlbiByZXRpcmUuJyB9LFxuICB7IGlkOiAnYW50bGVyUHJ5YmFyJywgbmFtZTogJ0FudGxlciBQcnliYXInLCBnbHlwaDogJ+KMkCcsIGNvb2xkb3duOiA0LCB0ZXh0OiAnU2hpZnQgb25lIGFkamFjZW50IGJvdWxkZXIgb3IgYnJlYWt3YWxsIGZvcndhcmQuJywgb3ZlcmRyaXZlOiAnU2hpZnQgb25jZSB3aXRoIGEgbG91ZGVyIGV4cG9zZWQgZm9vdGluZyByaXNrLCB0aGVuIHJldGlyZS4nIH0sXG4gIHsgaWQ6ICdzdG9uZUFkemUnLCBuYW1lOiAnU3RvbmUgQWR6ZScsIGdseXBoOiAn4oyfJywgY29vbGRvd246IDUsIHRleHQ6ICdDdXQgb25lIGFkamFjZW50IGNyYXRlIG9yIGNydW1ibGUgcm91dGUuJywgb3ZlcmRyaXZlOiAnQ3V0IG9uY2UsIHRoZW4gcmV0aXJlLicgfSxcbiAgeyBpZDogJ3Jlc2luRmlyZUJhc2tldCcsIG5hbWU6ICdSZXNpbiBGaXJlIEJhc2tldCcsIGdseXBoOiAn4pmoJywgY29vbGRvd246IDYsIHRleHQ6ICdCdXJuIGFkamFjZW50IGJyYW1ibGUgb3Igd2ViIGludG8gc21va2UuJywgb3ZlcmRyaXZlOiAnQnVybiBvbmNlIHdpdGggc3Ryb25nZXIgc21va2UsIHRoZW4gcmV0aXJlLicgfSxcbiAgeyBpZDogJ3dvb2RlbkxldmVyUm9sbGVyJywgbmFtZTogJ1dvb2RlbiBMZXZlciBhbmQgUm9sbGVyJywgZ2x5cGg6ICfihpQnLCBjb29sZG93bjogNSwgdGV4dDogJ1B1c2ggb25lIGFkamFjZW50IG1vdmFibGUgcHJvcCBmb3J3YXJkLicsIG92ZXJkcml2ZTogJ1B1c2ggb25jZSwgdGhlbiByZXRpcmUuJyB9XG5dXG5cbmV4cG9ydCBjb25zdCBCT09OUzogcmVhZG9ubHkgQm9vbltdID0gW1xuICB7IGlkOiAnc2NvdXRFeWUnLCBuYW1lOiAnU2NvdXQgRXllJywgZ2x5cGg6ICfil4knLCBmYW1pbHk6ICdzY291dGluZycsIHRleHQ6ICdSZXZlYWwgYWxsIHJlbWFpbmluZyBtaWxlc3RvbmUgZGlyZWN0aW9ucy4nIH0sXG4gIHsgaWQ6ICd0aW1lS25vdCcsIG5hbWU6ICdUaW1lIEtub3QnLCBnbHlwaDogJ+KfsicsIGZhbWlseTogJ3JlY292ZXJ5JywgdGV4dDogJ0dhaW4gb25lIHBvc2l0aW9uLW9ubHkgc2FmZS1zdGVwIHJld2luZC4nIH0sXG4gIHsgaWQ6ICdjb29sQXNoJywgbmFtZTogJ0Nvb2wgQXNoJywgZ2x5cGg6ICfigKInLCBmYW1pbHk6ICd0cmF2ZXJzYWwnLCB0ZXh0OiAnVG9vbHMgcmVjb3ZlciAxIHR1cm4gZmFzdGVyIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICd3YXlmaW5kZXJDb3JkJywgbmFtZTogJ1dheWZpbmRlciBDb3JkJywgZ2x5cGg6ICfijIcnLCBmYW1pbHk6ICd0cmF2ZXJzYWwnLCB0ZXh0OiAnVG9vbCB1c2UgcmV2ZWFscyBuZWFyYnkgdGVycmFpbi4nIH0sXG4gIHsgaWQ6ICdyb290ZWRSZXNvbHZlJywgbmFtZTogJ1Jvb3RlZCBSZXNvbHZlJywgZ2x5cGg6ICfinKYnLCBmYW1pbHk6ICdyZWNvdmVyeScsIHRleHQ6ICdUb29sIHVzZSByZXN0b3JlcyAxIEhQIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICd0cmFpbFJhdGlvbnMnLCBuYW1lOiAnVHJhaWwgUmF0aW9ucycsIGdseXBoOiAnKycsIGZhbWlseTogJ3JlY292ZXJ5JywgdGV4dDogJ0VhY2ggbWlsZXN0b25lIHJlc3RvcmVzIDIgSFAgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ2VtYmVyRmxldGNoaW5nJywgbmFtZTogJ0VtYmVyIEZsZXRjaGluZycsIGdseXBoOiAnKicsIGZhbWlseTogJ2NvbWJhdCcsIHRleHQ6ICdUaHJvd24gYXR0YWNrcyBkZWFsICsxIGRhbWFnZSBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnY29yZFRlbXBvJywgbmFtZTogJ0NvcmQgVGVtcG8nLCBnbHlwaDogJy8nLCBmYW1pbHk6ICdjb21iYXQnLCB0ZXh0OiAnVG9vbCB1c2UgcmVzdG9yZXMgMSBmb2N1cyBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnd2F0Y2hmdWxTdGVwJywgbmFtZTogJ1dhdGNoZnVsIFN0ZXAnLCBnbHlwaDogJyEnLCBmYW1pbHk6ICdzY291dGluZycsIHRleHQ6ICdOZXdseSBkaXNjb3ZlcmVkIG1pbGVzdG9uZXMgcmV2ZWFsIG5lYXJieSB0aHJlYXRzLicgfSxcbiAgeyBpZDogJ21hcE1vc3MnLCBuYW1lOiAnTWFwIE1vc3MnLCBnbHlwaDogJ8K3JywgZmFtaWx5OiAnc2NvdXRpbmcnLCB0ZXh0OiAnRXhwbG9yYXRpb24gcmV2ZWFscyAxIGV4dHJhIHRpbGUgcmFkaXVzIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdxdWlldFRpZGUnLCBuYW1lOiAnUXVpZXQgVGlkZScsIGdseXBoOiAnficsIGZhbWlseTogJ3NwZWxsY3JhZnQnLCB0ZXh0OiAnQ2hhcm1zIGNvc3QgMSBsZXNzIGZvY3VzIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdzcGlyaXRLaW5kbGluZycsIG5hbWU6ICdTcGlyaXQgS2luZGxpbmcnLCBnbHlwaDogJz8nLCBmYW1pbHk6ICdzcGVsbGNyYWZ0JywgdGV4dDogJ01pbGVzdG9uZXMgcmVzdG9yZSAxIGZvY3VzIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdjYWNoZVNlbnNlJywgbmFtZTogJ0NhY2hlIFNlbnNlJywgZ2x5cGg6ICckJywgZmFtaWx5OiAnZWNvbm9teScsIHRleHQ6ICdFYWNoIG1pbGVzdG9uZSB5aWVsZHMgOCBjYXNoIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdiYXJ0ZXJUaHJlYWQnLCBuYW1lOiAnQmFydGVyIFRocmVhZCcsIGdseXBoOiAnwqQnLCBmYW1pbHk6ICdlY29ub215JywgdGV4dDogJ0NvbnRhaW5lcnMgeWllbGQgKzEwIGNhc2ggcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3N0b25lTWVtb3J5JywgbmFtZTogJ1N0b25lIE1lbW9yeScsIGdseXBoOiAnIycsIGZhbWlseTogJ3RyYXZlcnNhbCcsIHRleHQ6ICdTdG9uZSBXZWRnZSBhbHNvIGNsZWFycyBuZWFyYnkgcnViYmxlIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdyZWVkTWVtb3J5JywgbmFtZTogJ1JlZWQgTWVtb3J5JywgZ2x5cGg6ICfiiYsnLCBmYW1pbHk6ICd0cmF2ZXJzYWwnLCB0ZXh0OiAnUmVlZHdpbmcgY3Jvc3NlcyBvbmUgZXh0cmEgaGF6YXJkIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdsYXN0TGlnaHQnLCBuYW1lOiAnTGFzdCBMaWdodCcsIGdseXBoOiAnaScsIGZhbWlseTogJ3JlY292ZXJ5JywgdGV4dDogJ0F0IDI1JSBIUCwgdG9vbCB1c2UgcmVzdG9yZXMgMiBIUCBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAncGFyY2VsTWFyaycsIG5hbWU6ICdQYXJjZWwgTWFyaycsIGdseXBoOiAn4pahJywgZmFtaWx5OiAnZWNvbm9teScsIHRleHQ6ICdVbmNsYWltZWQgbWlsZXN0b25lcyBzaG93IGRpc3RhbmNlIGFmdGVyIG9uZSBzdGFjay4nIH1cbiAgLHsgaWQ6ICdicmVhY2hUZW1wbycsIG5hbWU6ICdCcmVhY2ggVGVtcG8nLCBnbHlwaDogJ+KGrycsIGZhbWlseTogJ3RyYXZlcnNhbCcsIHRleHQ6ICdCcmVha2luZyB0ZXJyYWluIHJlc3RvcmVzIDEgZm9jdXMgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ3VwZHJhZnRTdGVwJywgbmFtZTogJ1VwZHJhZnQgU3RlcCcsIGdseXBoOiAn4oaRJywgZmFtaWx5OiAndHJhdmVyc2FsJywgdGV4dDogJ0xpZnRzIGFuZCBnbGlkZXMgdHJhdmVsIDEgZXh0cmEgdGlsZSBwZXIgc3RhY2suJyB9XG4gICx7IGlkOiAnYW5jaG9ySGFiaXQnLCBuYW1lOiAnQW5jaG9yIEhhYml0JywgZ2x5cGg6ICfimpMnLCBmYW1pbHk6ICd0cmF2ZXJzYWwnLCB0ZXh0OiAnUm9wZXMgYW5kIGFuY2hvcnMgcmVzdG9yZSAxIEhQIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICdzbW9rZVdhbGtlcicsIG5hbWU6ICdTbW9rZSBXYWxrZXInLCBnbHlwaDogJ+KJiCcsIGZhbWlseTogJ3RlcnJhaW4nLCB0ZXh0OiAnU21va2UgZGVhbHMgMSBsZXNzIGRhbWFnZSBhbmQgcmV2ZWFscyBuZWFyYnkgZ3JvdW5kLicgfVxuICAseyBpZDogJ2N1cnJlbnRTZW5zZScsIG5hbWU6ICdDdXJyZW50IFNlbnNlJywgZ2x5cGg6ICfiiYsnLCBmYW1pbHk6ICd0ZXJyYWluJywgdGV4dDogJ0N1cnJlbnQgY3Jvc3NpbmdzIHJldmVhbCBhIHJvdXRlIGFuZCBncmFudCAxIGZvY3VzLicgfVxuICAseyBpZDogJ3dhbGxTb25nJywgbmFtZTogJ1dhbGwgU29uZycsIGdseXBoOiAn4pmrJywgZmFtaWx5OiAndGVycmFpbicsIHRleHQ6ICdCcmVhY2ggdG9vbHMgY2xlYXIgb25lIGFkZGl0aW9uYWwgYWRqYWNlbnQgYmxvY2tlciBwZXIgc3RhY2suJyB9XG4gICx7IGlkOiAnYXNoRGl2aWRlbmQnLCBuYW1lOiAnQXNoIERpdmlkZW5kJywgZ2x5cGg6ICfCpCcsIGZhbWlseTogJ2Vjb25vbXknLCB0ZXh0OiAnRGVzdHJveWVkIHRlcnJhaW4gYW5kIHByb3BzIHlpZWxkIDQgY2FzaCBwZXIgc3RhY2suJyB9XG4gICx7IGlkOiAnc2FsdmFnZXInLCBuYW1lOiAnU2FsdmFnZXInLCBnbHlwaDogJ+KbjycsIGZhbWlseTogJ2Vjb25vbXknLCB0ZXh0OiAnRmlyc3QgdXNlZCBjb25zdW1hYmxlIGVhY2ggZmxvb3IgaGFzIGEgMjUlIHJldHVybiBjaGFuY2UgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ2xhc3RNYXRjaCcsIG5hbWU6ICdMYXN0IE1hdGNoJywgZ2x5cGg6ICfigKAnLCBmYW1pbHk6ICdjb25zdW1hYmxlJywgdGV4dDogJ0F0IDI1JSBIUCwgYm9tYnMgYW5kIGZpcmUgaXRlbXMgZ2FpbiArMiBkYW1hZ2UgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ3NwYXJlRnVzZScsIG5hbWU6ICdTcGFyZSBGdXNlJywgZ2x5cGg6ICchJywgZmFtaWx5OiAnY29uc3VtYWJsZScsIHRleHQ6ICdCb21iIHBhY2tzIHJlc3RvcmUgb25lIGV4dHJhIGJvbWIgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ3F1aWV0UG9ja2V0JywgbmFtZTogJ1F1aWV0IFBvY2tldCcsIGdseXBoOiAn4peMJywgZmFtaWx5OiAnY29uc3VtYWJsZScsIHRleHQ6ICdUb25pY3MgYWxzbyByZW1vdmUgb25lIGNvbmRpdGlvbiBwZXIgc3RhY2suJyB9XG4gICx7IGlkOiAnc2NhdmVuZ2VyTWFwJywgbmFtZTogJ1NjYXZlbmdlciBNYXAnLCBnbHlwaDogJ+KMlicsIGZhbWlseTogJ3Njb3V0aW5nJywgdGV4dDogJ0NvbnRhaW5lcnMgcmV2ZWFsIGEgbmVhcmJ5IGl0ZW0gb3IgbWlsZXN0b25lIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICdkYW5nZXJJbnN0aW5jdCcsIG5hbWU6ICdEYW5nZXIgSW5zdGluY3QnLCBnbHlwaDogJyEnLCBmYW1pbHk6ICdzY291dGluZycsIHRleHQ6ICdFbnRlcmluZyBhIG5ldyBoYXphcmQgcmV2ZWFscyBhZGphY2VudCBlbmVtaWVzIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICdzbGFnU2tpbicsIG5hbWU6ICdTbGFnIFNraW4nLCBnbHlwaDogJ+KXkicsIGZhbWlseTogJ3JlY292ZXJ5JywgdGV4dDogJ0ZpcmUgYW5kIHNtb2tlIGRhbWFnZSBhcmUgcmVkdWNlZCBieSAxIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICd0aWRlU2tpbicsIG5hbWU6ICdUaWRlIFNraW4nLCBnbHlwaDogJ+KXkycsIGZhbWlseTogJ3JlY292ZXJ5JywgdGV4dDogJ1dhdGVyIGFuZCBjdXJyZW50IGRhbWFnZSBhcmUgcmVkdWNlZCBieSAxIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICdwcmVzc3VyZVNlYWwnLCBuYW1lOiAnUHJlc3N1cmUgU2VhbCcsIGdseXBoOiAn4peIJywgZmFtaWx5OiAncmVjb3ZlcnknLCB0ZXh0OiAnR2FpbiAxIHNoaWVsZCBhZnRlciB1c2luZyBhIHRyYXZlcnNhbCB0b29sIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICdibG9vZENvbXBhc3MnLCBuYW1lOiAnQmxvb2QgQ29tcGFzcycsIGdseXBoOiAn4pylJywgZmFtaWx5OiAnY29tYmF0JywgdGV4dDogJ0tpbGxpbmcgYW4gZW5lbXkgcmV2ZWFscyAyIHRpbGVzIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICdob29rTGluZScsIG5hbWU6ICdIb29rIExpbmUnLCBnbHlwaDogJ+KMhycsIGZhbWlseTogJ2NvbWJhdCcsIHRleHQ6ICdSZWFjaCB3ZWFwb25zIGRlYWwgKzEgZGFtYWdlIHBlciBzdGFjayBhZnRlciBtb3ZlbWVudC4nIH1cbiAgLHsgaWQ6ICdndWFyZFJhdHRsZScsIG5hbWU6ICdHdWFyZCBSYXR0bGUnLCBnbHlwaDogJyknLCBmYW1pbHk6ICdjb21iYXQnLCB0ZXh0OiAnT2ZmLWhhbmQgZ2VhciBncmFudHMgKzEgZ3VhcmQgYWZ0ZXIgYSBoaXQgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ3RpZGVFZGdlJywgbmFtZTogJ1RpZGUgRWRnZScsIGdseXBoOiAnLycsIGZhbWlseTogJ2NvbWJhdCcsIHRleHQ6ICdXYXRlci9jdXJyZW50IHdlYXBvbiBoaXRzIHB1bGwgdGFyZ2V0cyAxIHRpbGUgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ2NpbmRlckVkZ2UnLCBuYW1lOiAnQ2luZGVyIEVkZ2UnLCBnbHlwaDogJy8nLCBmYW1pbHk6ICdjb21iYXQnLCB0ZXh0OiAnRmlyZS9oYW1tZXIgd2VhcG9uIGhpdHMgaWduaXRlIGdhcyBvciBzbW9rZSBwZXIgc3RhY2suJyB9XG4gICx7IGlkOiAnZWNob0NhY2hlJywgbmFtZTogJ0VjaG8gQ2FjaGUnLCBnbHlwaDogJyQnLCBmYW1pbHk6ICdlY29ub215JywgdGV4dDogJ0VhY2ggZGlzdGluY3QgQm9vbiBmYW1pbHkgZ3JhbnRzIDUgY2FzaCBhdCBtaWxlc3RvbmVzLicgfVxuICAseyBpZDogJ29wZW5DaXJjdWl0JywgbmFtZTogJ09wZW4gQ2lyY3VpdCcsIGdseXBoOiAn4oyBJywgZmFtaWx5OiAnc3BlbGxjcmFmdCcsIHRleHQ6ICdDYXN0aW5nIGFmdGVyIHRyYXZlcnNhbCByZXN0b3JlcyAxIGZvY3VzIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICdibGlua0RlYnQnLCBuYW1lOiAnQmxpbmsgRGVidCcsIGdseXBoOiAnPycsIGZhbWlseTogJ3NwZWxsY3JhZnQnLCB0ZXh0OiAnQmxpbmsgYW5kIHB1bGwgZ2FpbiAxIHJhbmdlIHBlciBzdGFjay4nIH1cbiAgLHsgaWQ6ICdyb290QmF0dGVyeScsIG5hbWU6ICdSb290IEJhdHRlcnknLCBnbHlwaDogJ+KZoycsIGZhbWlseTogJ3NwZWxsY3JhZnQnLCB0ZXh0OiAnUm9vdC93YXJkIGVmZmVjdHMgZ3JhbnQgMSBzaGllbGQgb3IgZm9jdXMgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ3NjcmFwUHJheWVyJywgbmFtZTogJ1NjcmFwIFByYXllcicsIGdseXBoOiAn4pi8JywgZmFtaWx5OiAndGVycmFpbicsIHRleHQ6ICdBY3RpdmF0ZWQgcHJvcHMgcmVzdG9yZSAxIEhQIGFuZCBmb2N1cyBwZXIgc3RhY2suJyB9XG4gICx7IGlkOiAnc29mdExhbmRpbmcnLCBuYW1lOiAnU29mdCBMYW5kaW5nJywgZ2x5cGg6ICfiiKgnLCBmYW1pbHk6ICd0cmF2ZXJzYWwnLCB0ZXh0OiAnSGF6YXJkIGNyb3NzaW5ncyByZWR1Y2UgaW5jb21pbmcgZGFtYWdlIGJ5IDEgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ2hhcmRMZXNzb24nLCBuYW1lOiAnSGFyZCBMZXNzb24nLCBnbHlwaDogJ+KIhicsIGZhbWlseTogJ2NvbWJhdCcsIHRleHQ6ICdUYWtpbmcgaGF6YXJkIGRhbWFnZSBnaXZlcyArMSBtZWxlZSBkYW1hZ2UgbmV4dCBhdHRhY2sgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ3NlYWxlZEJyZWF0aCcsIG5hbWU6ICdTZWFsZWQgQnJlYXRoJywgZ2x5cGg6ICfil40nLCBmYW1pbHk6ICdyZWNvdmVyeScsIHRleHQ6ICdTbW9rZSBhbmQgZ2FzIG5vIGxvbmdlciByZWR1Y2Ugc2lnaHQgcGVyIHN0YWNrLicgfVxuICAseyBpZDogJ3JlbGF5U3RlcCcsIG5hbWU6ICdSZWxheSBTdGVwJywgZ2x5cGg6ICfigLonLCBmYW1pbHk6ICd0cmF2ZXJzYWwnLCB0ZXh0OiAnQWx0ZXJuYXRpbmcgbW92ZSBhbmQgdG9vbCB1c2UgbG93ZXJzIHRvb2wgY29vbGRvd24gYnkgMSBwZXIgc3RhY2suJyB9XG4gICx7IGlkOiAnYm9ycm93ZWRUaW1lJywgbmFtZTogJ0JvcnJvd2VkIFRpbWUnLCBnbHlwaDogJ+KMmycsIGZhbWlseTogJ3JlY292ZXJ5JywgdGV4dDogJ1RpbWUgS25vdCBnYWlucyBvbmUgZXh0cmEgc2FmZSBwb3NpdGlvbiBwZXIgc3RhY2suJyB9XG4gICx7IGlkOiAnZnVybmFjZUhlYXJ0JywgbmFtZTogJ0Z1cm5hY2UgSGVhcnQnLCBnbHlwaDogJ+KZpScsIGZhbWlseTogJ3RlcnJhaW4nLCB0ZXh0OiAnRmlyZSBhY3Rpb25zIGdyYW50ICsxIGRhbWFnZSBhbmQgKzEgZm9jdXMgcGVyIHN0YWNrLicsIHJhcmU6IHRydWUgfVxuICAseyBpZDogJ2Ryb3duZWRPYXRoJywgbmFtZTogJ0Ryb3duZWQgT2F0aCcsIGdseXBoOiAn4pmGJywgZmFtaWx5OiAndGVycmFpbicsIHRleHQ6ICdXYXRlci9jdXJyZW50IGFjdGlvbnMgaGVhbCAxIEhQIGFuZCBwdWxsIGVuZW1pZXMgcGVyIHN0YWNrLicsIHJhcmU6IHRydWUgfVxuICAseyBpZDogJ2JsYWNrTGVkZ2VyJywgbmFtZTogJ0JsYWNrIExlZGdlcicsIGdseXBoOiAnwqcnLCBmYW1pbHk6ICdlY29ub215JywgdGV4dDogJ0dhaW4gMTIgY2FzaCB3aGVuZXZlciB5b3UgY2xhaW0gYSBtaWxlc3RvbmUgcGVyIHN0YWNrLicsIHJhcmU6IHRydWUgfVxuICAseyBpZDogJ2dsYXNzTmVydmUnLCBuYW1lOiAnR2xhc3MgTmVydmUnLCBnbHlwaDogJ+KXhycsIGZhbWlseTogJ2NvbWJhdCcsIHRleHQ6ICdZb3VyIGZpcnN0IGF0dGFjayBlYWNoIHR1cm4gZ2FpbnMgKzMgZGFtYWdlIHBlciBzdGFjay4nLCByYXJlOiB0cnVlIH1cbiAgLHsgaWQ6ICd3YXlFYXRlcicsIG5hbWU6ICdXYXkgRWF0ZXInLCBnbHlwaDogJ+KMmCcsIGZhbWlseTogJ3RyYXZlcnNhbCcsIHRleHQ6ICdDbGVhcmluZyB0ZXJyYWluIHBlcm1hbmVudGx5IGxvd2VycyBhbGwgdG9vbCBjb29sZG93bnMgYnkgMSBwZXIgc3RhY2suJywgcmFyZTogdHJ1ZSB9XG4gICx7IGlkOiAnZGVlcFBvY2tldHMnLCBuYW1lOiAnRGVlcCBQb2NrZXRzJywgZ2x5cGg6ICfilqMnLCBmYW1pbHk6ICdjb25zdW1hYmxlJywgdGV4dDogJ1VzZWQgaXRlbXMgaGF2ZSBhIDM1JSBjaGFuY2UgcGVyIHN0YWNrIHRvIHJldHVybiBhZnRlciBjb21iYXQuJywgcmFyZTogdHJ1ZSB9LFxuICB7IGlkOiAnZ2FsZVRocmVhZCcsIG5hbWU6ICdHYWxlIFRocmVhZCcsIGdseXBoOiAn4omIJywgZmFtaWx5OiAndHJhdmVyc2FsJywgdGV4dDogJ0VhY2ggY2xpbWIgcmVzdG9yZXMgMSBmb2N1cyBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAncm9wZXdyaWdodCcsIG5hbWU6ICdSb3Bld3JpZ2h0JywgZ2x5cGg6ICfijIEnLCBmYW1pbHk6ICd0cmF2ZXJzYWwnLCB0ZXh0OiAnQXQgcmFuayAyKywgZXZlcnkgc2Vjb25kIHZlcnRpY2FsIHJvcGUgY29zdHMgbm8gcmVzZXJ2ZSByb3BlLicgfSxcbiAgeyBpZDogJ3VwZHJhZnRDYWRlbmNlJywgbmFtZTogJ1VwZHJhZnQgQ2FkZW5jZScsIGdseXBoOiAn4oaRJywgZmFtaWx5OiAndHJhdmVyc2FsJywgdGV4dDogJ0xlZGdlIG1vdmVtZW50IGdyYW50cyAxIGZvY3VzIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdza3lob29rUmVwcmlzYWwnLCBuYW1lOiAnU2t5aG9vayBSZXByaXNhbCcsIGdseXBoOiAnSicsIGZhbWlseTogJ2NvbWJhdCcsIHRleHQ6ICdBZnRlciBjbGltYmluZywgeW91ciBuZXh0IGF0dGFjayBnYWlucyArMiBkYW1hZ2UgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3RodW5kZXJWZXNzZWwnLCBuYW1lOiAnVGh1bmRlciBWZXNzZWwnLCBnbHlwaDogJ8+fJywgZmFtaWx5OiAnY29tYmF0JywgdGV4dDogJ1Rocm93biBkYW1hZ2UgZ2FpbnMgKzEgYW5kIG1hcmtzIHRhcmdldHMgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ2V5cmllSG9hcmQnLCBuYW1lOiAnRXlyaWUgSG9hcmQnLCBnbHlwaDogJyQnLCBmYW1pbHk6ICdlY29ub215JywgdGV4dDogJ0NhY2hlcyBhbmQgY2hlc3RzIHlpZWxkIDEyIGNhc2ggcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3dpbmRTY3JpYmUnLCBuYW1lOiAnV2luZCBTY3JpYmUnLCBnbHlwaDogJ+KMhycsIGZhbWlseTogJ3NwZWxsY3JhZnQnLCB0ZXh0OiAnV2luZCBhbmQgZm9yY2UgY2hhcm1zIGdhaW4gMSByYW5nZSBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnaGlnaFBhdGgnLCBuYW1lOiAnSGlnaCBQYXRoJywgZ2x5cGg6ICfijJYnLCBmYW1pbHk6ICdzY291dGluZycsIHRleHQ6ICdDbGltYnMgcmV2ZWFsIDIgdGlsZXMgcGVyIHN0YWNrIGFyb3VuZCB0aGUgZGVzdGluYXRpb24uJyB9LFxuICB7IGlkOiAnZ3JhdmVMZWRnZXInLCBuYW1lOiAnR3JhdmUgTGVkZ2VyJywgZ2x5cGg6ICfCpycsIGZhbWlseTogJ2Vjb25vbXknLCB0ZXh0OiAnRW5lbXkga2lsbHMgZ3JhbnQgMyBjYXNoIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdhbmNlc3RvckxhbnRlcm4nLCBuYW1lOiAnQW5jZXN0b3IgTGFudGVybicsIGdseXBoOiAnaScsIGZhbWlseTogJ3JlY292ZXJ5JywgdGV4dDogJ0VuZW15IGtpbGxzIHJlc3RvcmUgMSBmb2N1cyBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnYm9uZU9yY2hhcmQnLCBuYW1lOiAnQm9uZSBPcmNoYXJkJywgZ2x5cGg6ICfinKYnLCBmYW1pbHk6ICdyZWNvdmVyeScsIHRleHQ6ICdFbmVteSBraWxscyByZXN0b3JlIDEgSFAgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ2NhaXJuUGFjdCcsIG5hbWU6ICdDYWlybiBQYWN0JywgZ2x5cGg6ICfilrInLCBmYW1pbHk6ICd0ZXJyYWluJywgdGV4dDogJ09hdGggYW5kIGJvZHkgcGF5bWVudHMgZ3JhbnQgMTggY2FzaCBwZXIgc3RhY2suJywgc3VwcHJlc3NlczogWydjb25zdW1hYmxlJ10sIHJhcmU6IHRydWUgfSxcbiAgeyBpZDogJ21vdXJuZXJzQmVsbCcsIG5hbWU6ICdNb3VybmVy4oCZcyBCZWxsJywgZ2x5cGg6ICdvJywgZmFtaWx5OiAnc3BlbGxjcmFmdCcsIHRleHQ6ICdTcGlyaXQgZXZlbnRzIHJlc3RvcmUgMSBmb2N1cyBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnb3NzdWFyeVdhcmQnLCBuYW1lOiAnT3NzdWFyeSBXYXJkJywgZ2x5cGg6ICfilqEnLCBmYW1pbHk6ICdyZWNvdmVyeScsIHRleHQ6ICdBdCBlYWNoIGVuY291bnRlciByZXNvbHV0aW9uLCBnYWluIDEgc2hpZWxkIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdmdW5lcmFsRXhjaGFuZ2UnLCBuYW1lOiAnRnVuZXJhbCBFeGNoYW5nZScsIGdseXBoOiAnwqQnLCBmYW1pbHk6ICdlY29ub215JywgdGV4dDogJ0Rpc2NhcmRlZCBpdGVtcyB5aWVsZCAxMCBjYXNoIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdsYXN0Uml0ZXMnLCBuYW1lOiAnTGFzdCBSaXRlcycsIGdseXBoOiAn4oCgJywgZmFtaWx5OiAnY29tYmF0JywgdGV4dDogJ0F0IDI1JSBIUCwgYXR0YWNrcyBnYWluICsyIGRhbWFnZSBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnc3Rvcm13YWtlJywgbmFtZTogJ1N0b3Jtd2FrZSBFbmdpbmUnLCBnbHlwaDogJ8+fJywgZmFtaWx5OiAnY29tYmF0JywgdGV4dDogJ1RyYXZlcnNhbCBhY3Rpb25zIGNoYXJnZSArMSBhdHRhY2sgZGFtYWdlIHBlciBzdGFjazsgc3VwcHJlc3NlcyByZWNvdmVyeSB0cmlnZ2Vycy4nLCBzdXBwcmVzc2VzOiBbJ3JlY292ZXJ5J10sIHJhcmU6IHRydWUgfSxcbiAgeyBpZDogJ2J1cmlhbEN1cnJlbnQnLCBuYW1lOiAnQnVyaWFsIEN1cnJlbnQnLCBnbHlwaDogJ+KJiCcsIGZhbWlseTogJ3RlcnJhaW4nLCB0ZXh0OiAnQ3VycmVudCwgc3Bpcml0LCBhbmQgZ3JhdmUgdGVycmFpbiByZXN0b3JlIDEgSFAgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ2VjaG9EaXZpZGVuZCcsIG5hbWU6ICdFY2hvIERpdmlkZW5kJywgZ2x5cGg6ICckJywgZmFtaWx5OiAnZWNvbm9teScsIHRleHQ6ICdFdmVyeSByZXNvbHZlZCBlbmNvdW50ZXIgZ3JhbnRzIDEwIGNhc2ggcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3RldGhlcmVkVGh1bmRlcicsIG5hbWU6ICdUZXRoZXJlZCBUaHVuZGVyJywgZ2x5cGg6ICfijIEnLCBmYW1pbHk6ICdjb21iYXQnLCB0ZXh0OiAnUm9wZSBhbmQgY2xpbWIgYWN0aW9ucyBhcm0gKzEgdGhyb3duIGRhbWFnZSBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAncmlmdExlZGdlcicsIG5hbWU6ICdSaWZ0IExlZGdlcicsIGdseXBoOiAnwqcnLCBmYW1pbHk6ICdlY29ub215JywgdGV4dDogJ0VsaXRlIGtpbGxzIGdyYW50IDE1IGV4dHJhIGNhc2ggcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3NhbHRlZEFuY2VzdG9yJywgbmFtZTogJ1NhbHRlZCBBbmNlc3RvcicsIGdseXBoOiAnaScsIGZhbWlseTogJ3NwZWxsY3JhZnQnLCB0ZXh0OiAnQSBjaGFybSBjYXN0IGFmdGVyIGEga2lsbCByZWZ1bmRzIDEgZm9jdXMgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3dpbmRmYWxsJywgbmFtZTogJ1dpbmRmYWxsJywgZ2x5cGg6ICfihpEnLCBmYW1pbHk6ICdlY29ub215JywgdGV4dDogJ0VhY2ggcm91dGUgdHJhbnNpdGlvbiBncmFudHMgMjAgY2FzaCBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnZ3JhdmV3aW5kJywgbmFtZTogJ0dyYXZld2luZCcsIGdseXBoOiAn4peMJywgZmFtaWx5OiAndGVycmFpbicsIHRleHQ6ICdNYXJrZWQgZW5lbWllcyB0YWtlICsxIGRhbWFnZSBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnY2xpZmZzaWRlQ2Fpcm4nLCBuYW1lOiAnQ2xpZmZzaWRlIENhaXJuJywgZ2x5cGg6ICfilrInLCBmYW1pbHk6ICdzY291dGluZycsIHRleHQ6ICdSZXZlYWxlZCBtaWxlc3RvbmVzIHJlc3RvcmUgMSBmb2N1cyBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnc3Bpcml0U2FpbCcsIG5hbWU6ICdTcGlyaXQgU2FpbCcsIGdseXBoOiAn4oyHJywgZmFtaWx5OiAndHJhdmVyc2FsJywgdGV4dDogJ0VhY2ggY2xpbWIgb3IgY3VycmVudCBjcm9zc2luZyBnYWlucyArMSBtb3ZlbWVudCByYW5nZSBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnZGVzY2VudEVuZ2luZScsIG5hbWU6ICdEZXNjZW50IEVuZ2luZScsIGdseXBoOiAn4oavJywgZmFtaWx5OiAnY29tYmF0JywgdGV4dDogJ0RhbWFnZSBhZnRlciBhIHRlcnJhaW4gY3Jvc3NpbmcgZ2FpbnMgKzEgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ2FsdGFyQ29tcG91bmQnLCBuYW1lOiAnQWx0YXIgQ29tcG91bmQnLCBnbHlwaDogJysnLCBmYW1pbHk6ICdzcGVsbGNyYWZ0JywgdGV4dDogJ0FsdGFyIGFuZCBvYXRoIHJld2FyZHMgYWRkIDEgYm9vbiBldm9sdXRpb24gcmFuayBwZXIgc3RhY2sgYXQgMysuJyB9LFxuICB7IGlkOiAnc3Rvcm1SYXRpb25zJywgbmFtZTogJ1N0b3JtIFJhdGlvbnMnLCBnbHlwaDogJysnLCBmYW1pbHk6ICdjb25zdW1hYmxlJywgdGV4dDogJ1VzaW5nIGEgdG9uaWMgYWZ0ZXIgbW92aW5nIHJlc3RvcmVzIDIgZXh0cmEgSFAgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ2hlaXJsb29tQ2lyY3VpdCcsIG5hbWU6ICdIZWlybG9vbSBDaXJjdWl0JywgZ2x5cGg6ICfil4cnLCBmYW1pbHk6ICdyZWNvdmVyeScsIHRleHQ6ICdHdWFyZGlhbiBraWxscyByZXN0b3JlIDQgSFAgYW5kIGZvY3VzIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdicmlkZ2VPZk5hbWVzJywgbmFtZTogJ0JyaWRnZSBvZiBOYW1lcycsIGdseXBoOiAnPScsIGZhbWlseTogJ3RyYXZlcnNhbCcsIHRleHQ6ICdFYWNoIGFjdGl2ZSBvYXRoIGdyYW50cyAxIGFybW9yIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdjdXJzZWRJbnZlc3RtZW50JywgbmFtZTogJ0N1cnNlZCBJbnZlc3RtZW50JywgZ2x5cGg6ICfimKAnLCBmYW1pbHk6ICdlY29ub215JywgdGV4dDogJ1doaWxlIGN1cnNlZCwgYWxsIGNhc2ggZ2FpbnMgYXJlIGRvdWJsZWQgcGVyIHN0YWNrLicsIHN1cHByZXNzZXM6IFsnY29uc3VtYWJsZSddLCByYXJlOiB0cnVlIH0sXG4gIHsgaWQ6ICdzdW5zdGVwJywgbmFtZTogJ1N1bnN0ZXAnLCBnbHlwaDogJ+KYvCcsIGZhbWlseTogJ3RyYXZlcnNhbCcsIGJpb21lczogWydzYWx0RmxhdHMnXSwgdGV4dDogJ1NhbHQgbWlycm9ycyByZXN0b3JlIDEgZm9jdXMgcGVyIHN0YWNrIGFuZCBhcm0gYSBQcmlzbSBSZWxheS4nIH0sXG4gIHsgaWQ6ICdicmluZVdhcmQnLCBuYW1lOiAnQnJpbmUgV2FyZCcsIGdseXBoOiAn4omIJywgZmFtaWx5OiAncmVjb3ZlcnknLCBiaW9tZXM6IFsnc2FsdEZsYXRzJ10sIHRleHQ6ICdCcmluZSBkYW1hZ2UgaXMgcmVkdWNlZCBieSAxIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdtaXJyb3JIdW50JywgbmFtZTogJ01pcnJvciBIdW50JywgZ2x5cGg6ICfil4cnLCBmYW1pbHk6ICdjb21iYXQnLCBiaW9tZXM6IFsnc2FsdEZsYXRzJ10sIHRleHQ6ICdBdHRhY2tzIGZyb20gc2FsdCBtaXJyb3JzIGdhaW4gKzEgZGFtYWdlIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdnbGFzc0VkZ2UnLCBuYW1lOiAnR2xhc3MgRWRnZScsIGdseXBoOiAn4peIJywgZmFtaWx5OiAnY29tYmF0JywgYmlvbWVzOiBbJ3NhbHRGbGF0cyddLCB0ZXh0OiAnTWFya2VkIHRhcmdldHMgdGFrZSArMSBkYW1hZ2UgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3NhbHRMZWRnZXInLCBuYW1lOiAnU2FsdCBMZWRnZXInLCBnbHlwaDogJ8KnJywgZmFtaWx5OiAnZWNvbm9teScsIGJpb21lczogWydzYWx0RmxhdHMnXSwgdGV4dDogJ01pbGVzdG9uZXMgZ3JhbnQgNiBleHRyYSBjYXNoIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdkdW5lUmF0aW9uJywgbmFtZTogJ0R1bmUgUmF0aW9uJywgZ2x5cGg6ICcrJywgZmFtaWx5OiAncmVjb3ZlcnknLCBiaW9tZXM6IFsnc2FsdEZsYXRzJ10sIHRleHQ6ICdNaWxlc3RvbmVzIHJlc3RvcmUgMSBleHRyYSBIUCBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnbWlyYWdlTWFwJywgbmFtZTogJ01pcmFnZSBNYXAnLCBnbHlwaDogJ+KMlicsIGZhbWlseTogJ3Njb3V0aW5nJywgYmlvbWVzOiBbJ3NhbHRGbGF0cyddLCB0ZXh0OiAnU2FsdCBtaXJyb3JzIHJldmVhbCAxIG5lYXJieSB1bmV4cGxvcmVkIHRpbGUgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3doaXRlUm9hZCcsIG5hbWU6ICdXaGl0ZSBSb2FkJywgZ2x5cGg6ICfigLonLCBmYW1pbHk6ICd0ZXJyYWluJywgYmlvbWVzOiBbJ3NhbHRGbGF0cyddLCB0ZXh0OiAnQnJpbmUgY3Jvc3NpbmdzIGdyYW50IDEgc2hpZWxkIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdzdW5zZXRDaXJjdWl0JywgbmFtZTogJ1N1bnNldCBDaXJjdWl0JywgZ2x5cGg6ICfPnycsIGZhbWlseTogJ3NwZWxsY3JhZnQnLCBiaW9tZXM6IFsnc2FsdEZsYXRzJ10sIHRleHQ6ICdDYXN0aW5nIGFmdGVyIGEgc2FsdCBtaXJyb3IgcmVzdG9yZXMgMSBmb2N1cyBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAnaGVhdERlYnQnLCBuYW1lOiAnSGVhdCBEZWJ0JywgZ2x5cGg6ICfimKAnLCBmYW1pbHk6ICdlY29ub215JywgYmlvbWVzOiBbJ3NhbHRGbGF0cyddLCB0ZXh0OiAnR2FpbiAxNCBjYXNoIHBlciBob3N0aWxlIGtpbGwgcGVyIHN0YWNrOyBzdXBwcmVzc2VzIHJlY292ZXJ5IHRyaWdnZXJzLicsIHN1cHByZXNzZXM6IFsncmVjb3ZlcnknXSwgcmFyZTogdHJ1ZSB9LFxuICB7IGlkOiAnY29sZFJlYWQnLCBuYW1lOiAnQ29sZCBSZWFkJywgZ2x5cGg6ICfinYQnLCBmYW1pbHk6ICdzY291dGluZycsIGJpb21lczogWydmcm9zdFJlbGlxdWFyeSddLCB0ZXh0OiAnSWNlIGNyb3NzaW5ncyByZXN0b3JlIDEgZm9jdXMgYW5kIHJldmVhbCBuZWFyYnkgdGhyZWF0cyBwZXIgc3RhY2suJyB9LFxuICB7IGlkOiAncmltZUd1YXJkJywgbmFtZTogJ1JpbWUgR3VhcmQnLCBnbHlwaDogJ+KWoScsIGZhbWlseTogJ3JlY292ZXJ5JywgYmlvbWVzOiBbJ2Zyb3N0UmVsaXF1YXJ5J10sIHRleHQ6ICdJY2UgY3Jvc3NpbmdzIGdyYW50IDEgc2hpZWxkIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdkdWVsaXN0T2F0aCcsIG5hbWU6ICdEdWVsaXN0IE9hdGgnLCBnbHlwaDogJ+KalCcsIGZhbWlseTogJ2NvbWJhdCcsIGJpb21lczogWydmcm9zdFJlbGlxdWFyeSddLCB0ZXh0OiAnRWxpdGUgYW5kIGd1YXJkaWFuIHRhcmdldHMgdGFrZSArMiBkYW1hZ2UgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3NoYXR0ZXJNYXJrJywgbmFtZTogJ1NoYXR0ZXIgTWFyaycsIGdseXBoOiAn4pymJywgZmFtaWx5OiAnY29tYmF0JywgYmlvbWVzOiBbJ2Zyb3N0UmVsaXF1YXJ5J10sIHRleHQ6ICdTbG93ZWQgb3IgbWFya2VkIHRhcmdldHMgdGFrZSArMSBkYW1hZ2UgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3dpbnRlclJhdGlvbnMnLCBuYW1lOiAnV2ludGVyIFJhdGlvbnMnLCBnbHlwaDogJysnLCBmYW1pbHk6ICdyZWNvdmVyeScsIGJpb21lczogWydmcm9zdFJlbGlxdWFyeSddLCB0ZXh0OiAnTWlsZXN0b25lcyByZXN0b3JlIDEgSFAgYW5kIGZvY3VzIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdpY2VMZWRnZXInLCBuYW1lOiAnSWNlIExlZGdlcicsIGdseXBoOiAnwqcnLCBmYW1pbHk6ICdlY29ub215JywgYmlvbWVzOiBbJ2Zyb3N0UmVsaXF1YXJ5J10sIHRleHQ6ICdFbGl0ZSBraWxscyBncmFudCAxMCBleHRyYSBjYXNoIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdmcm96ZW5Gb2N1cycsIG5hbWU6ICdGcm96ZW4gRm9jdXMnLCBnbHlwaDogJ+KXjCcsIGZhbWlseTogJ3NwZWxsY3JhZnQnLCBiaW9tZXM6IFsnZnJvc3RSZWxpcXVhcnknXSwgdGV4dDogJ0EgY2hhcm0gY2FzdCB3aGlsZSBzaGllbGRlZCBjb3N0cyAxIGxlc3MgZm9jdXMgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ3RoYXdTdGVwJywgbmFtZTogJ1RoYXcgU3RlcCcsIGdseXBoOiAn4oioJywgZmFtaWx5OiAndGVycmFpbicsIGJpb21lczogWydmcm9zdFJlbGlxdWFyeSddLCB0ZXh0OiAnRnJvc3QgcmltZSBkYW1hZ2UgaXMgcmVkdWNlZCBieSAxIHBlciBzdGFjay4nIH0sXG4gIHsgaWQ6ICdyZWxpcXVhcnlFY2hvJywgbmFtZTogJ1JlbGlxdWFyeSBFY2hvJywgZ2x5cGg6ICdvJywgZmFtaWx5OiAndGVycmFpbicsIGJpb21lczogWydmcm9zdFJlbGlxdWFyeSddLCB0ZXh0OiAnUmVzb2x2aW5nIGFuIGVuY291bnRlciBncmFudHMgMSBzaGllbGQgcGVyIHN0YWNrLicgfSxcbiAgeyBpZDogJ2xhc3RXaW50ZXInLCBuYW1lOiAnTGFzdCBXaW50ZXInLCBnbHlwaDogJ+KAoCcsIGZhbWlseTogJ2NvbWJhdCcsIGJpb21lczogWydmcm9zdFJlbGlxdWFyeSddLCB0ZXh0OiAnQXQgMjUlIEhQLCBhdHRhY2tzIGdhaW4gKzMgZGFtYWdlIHBlciBzdGFjazsgc3VwcHJlc3NlcyBjb25zdW1hYmxlIHJlY292ZXJ5LicsIHN1cHByZXNzZXM6IFsnY29uc3VtYWJsZSddLCByYXJlOiB0cnVlIH1cbl1cblxuY29uc3QgdG9vbEJ5SWQgPSBPYmplY3QuZnJvbUVudHJpZXMoVE9PTFMubWFwKHRvb2wgPT4gW3Rvb2wuaWQsIHRvb2xdKSkgYXMgUmVjb3JkPFRyYXZlcnNhbFRvb2xJZCwgVHJhdmVyc2FsVG9vbD5cbmNvbnN0IGJvb25CeUlkID0gT2JqZWN0LmZyb21FbnRyaWVzKEJPT05TLm1hcChib29uID0+IFtib29uLmlkLCBib29uXSkpIGFzIFJlY29yZDxzdHJpbmcsIEJvb24+XG5jb25zdCBLQU1JX0JPT05TID0gbmV3IFNldDxCb29uSWQ+KFsndGltZUtub3QnLCAncm9vdGVkUmVzb2x2ZScsICd3YXRjaGZ1bFN0ZXAnLCAnbWFwTW9zcycsICdxdWlldFRpZGUnLCAnc3Bpcml0S2luZGxpbmcnLCAnbGFzdExpZ2h0JywgJ3dhbGxTb25nJywgJ3dhcmRNZW1vcnknLCAnZWNob0RpdmlkZW5kJywgJ3N1bnN0ZXAnLCAnbWlyYWdlTWFwJywgJ3doaXRlUm9hZCcsICdzdW5zZXRDaXJjdWl0JywgJ2NvbGRSZWFkJywgJ3JpbWVHdWFyZCcsICdkdWVsaXN0T2F0aCcsICdzaGF0dGVyTWFyaycsICdmcm96ZW5Gb2N1cycsICdyZWxpcXVhcnlFY2hvJywgJ2xhc3RXaW50ZXInXSlcbmV4cG9ydCBjb25zdCBib29uQWxpZ25tZW50ID0gKGlkOiBCb29uSWQpOiBBbGlnbm1lbnQgPT4gS0FNSV9CT09OUy5oYXMoaWQpID8gJ2thbWknIDogJ3ZpbGxhZ2VQYWN0J1xuY29uc3QgZHJpbGxhYmxlID0gbmV3IFNldChbJ3dhbGwnLCAncnViYmxlJywgJ2JyYW1ibGUnLCAnYm91bGRlciddKVxuY29uc3QgaGF6YXJkb3VzID0gbmV3IFNldChbJ3BpdCcsICd3YXRlcicsICdsYXZhJywgJ3NwaWtlcycsICdkYXJ0JywgJ2ZpcmVWZW50JywgJ2dhcycsICdjcnVtYmxlJywgJ2JvdWxkZXInLCAnYnJhbWJsZScsICdydWJibGUnLCAnYnJpbmUnLCAnZnJvc3RSaW1lJ10pXG5cbmV4cG9ydCBjb25zdCB0b29sRm9yID0gKGlkOiBUcmF2ZXJzYWxUb29sSWQpOiBUcmF2ZXJzYWxUb29sID0+IHRvb2xCeUlkW2lkXVxuZXhwb3J0IGNvbnN0IGFjcXVpcmVPcHRpb25hbFRyYXZlcnNhbFRvb2wgPSAoc3RhdGU6IFJ1blN0YXRlLCB0b29sOiBUcmF2ZXJzYWxUb29sSWQpOiB7IHJlc3VsdDogJ2JvdW5kJyB8ICdkdXBsaWNhdGUnIHwgJ3JlcGxhY2VkJzsgcmVwbGFjZWQ/OiBUcmF2ZXJzYWxUb29sSWQgfSA9PiB7XG4gIGNvbnN0IHRvb2xzID0gWy4uLihzdGF0ZS5oZXJvLnRyYXZlcnNhbFRvb2xzID8/IFtdKV1cbiAgaWYgKHRvb2xzLmluY2x1ZGVzKHRvb2wpKSByZXR1cm4geyByZXN1bHQ6ICdkdXBsaWNhdGUnIH1cbiAgaWYgKHRvb2xzLmxlbmd0aCA8IDIpIHsgc3RhdGUuaGVyby50cmF2ZXJzYWxUb29scyA9IFsuLi50b29scywgdG9vbF07IHJldHVybiB7IHJlc3VsdDogJ2JvdW5kJyB9IH1cbiAgY29uc3QgcmVwbGFjZWQgPSB0b29scy5zaGlmdCgpIVxuICBzdGF0ZS5oZXJvLnRyYXZlcnNhbFRvb2xzID0gWy4uLnRvb2xzLCB0b29sXVxuICBkZWxldGUgc3RhdGUuaGVyby5jb29sZG93bnM/LltgdG9vbDoke3JlcGxhY2VkfWBdXG4gIHJldHVybiB7IHJlc3VsdDogJ3JlcGxhY2VkJywgcmVwbGFjZWQgfVxufVxuZXhwb3J0IGNvbnN0IGJvb25Gb3IgPSAoaWQ6IEJvb25JZCk6IEJvb24gPT4gYm9vbkJ5SWRbaWRdXG5leHBvcnQgY29uc3QgYm9vblJhbmsgPSAoc3RhdGU6IFJ1blN0YXRlLCBpZDogQm9vbklkKTogbnVtYmVyID0+IHtcbiAgY29uc3QgYm9vbiA9IGJvb25CeUlkW2lkXVxuICBpZiAoIWJvb24pIHJldHVybiAwXG4gIGNvbnN0IHN1cHByZXNzZWQgPSBPYmplY3Qua2V5cyhzdGF0ZS5oZXJvLmJvb25zID8/IHt9KS5zb21lKG93bmVySWQgPT4ge1xuICAgIGNvbnN0IG93bmVyID0gYm9vbkJ5SWRbb3duZXJJZF1cbiAgICByZXR1cm4gb3duZXJJZCAhPT0gaWQgJiYgKHN0YXRlLmhlcm8uYm9vbnM/Lltvd25lcklkXSA/PyAwKSA+IDAgJiYgb3duZXI/LnN1cHByZXNzZXM/LmluY2x1ZGVzKGJvb24uZmFtaWx5KVxuICB9KVxuICByZXR1cm4gc3VwcHJlc3NlZCA/IDAgOiAoc3RhdGUuaGVyby5ib29ucz8uW2lkXSA/PyAwKSArIChzdGF0ZS5oZXJvLmJvb25Fdm9sdXRpb25zPy5baWRdID8/IDApXG59XG5leHBvcnQgY29uc3QgaGFzQm9vbiA9IChzdGF0ZTogUnVuU3RhdGUsIGlkOiBCb29uSWQpOiBib29sZWFuID0+IGJvb25SYW5rKHN0YXRlLCBpZCkgPiAwXG5leHBvcnQgY29uc3QgdG9vbENvb2xkb3duID0gKHN0YXRlOiBSdW5TdGF0ZSwgaWQ6IFRyYXZlcnNhbFRvb2xJZCk6IG51bWJlciA9PiBzdGF0ZS5oZXJvLmNvb2xkb3ducz8uW2B0b29sOiR7aWR9YF0gPz8gMFxuXG5leHBvcnQgY29uc3QgdG9vbENob2ljZXMgPSAoc3RhdGU6IFJ1blN0YXRlLCBtaWxlc3RvbmU6IEZsb29yTWlsZXN0b25lKTogVHJhdmVyc2FsVG9vbFtdID0+IHtcbiAgY29uc3Qgb2ZmZXIgPSByZXdhcmRPZmZlckZvcihzdGF0ZS5mbG9vciwgbWlsZXN0b25lLnJld2FyZEtleSlcbiAgaWYgKG9mZmVyPy5raW5kID09PSAnd2F5Y2FjaGUnKSB7XG4gICAgY29uc3QgY2hvaWNlcyA9IG9mZmVyLmNob2ljZXMubWFwKGNob2ljZSA9PiB0b29sQnlJZFtjaG9pY2UuaWRdKS5maWx0ZXIoKGNob2ljZSk6IGNob2ljZSBpcyBUcmF2ZXJzYWxUb29sID0+IEJvb2xlYW4oY2hvaWNlKSlcbiAgICBpZiAoY2hvaWNlcy5sZW5ndGggPT09IDMgJiYgbmV3IFNldChjaG9pY2VzLm1hcChjaG9pY2UgPT4gY2hvaWNlLmlkKSkuc2l6ZSA9PT0gMykgcmV0dXJuIGNob2ljZXNcbiAgfVxuICByZXR1cm4gcm5nRm9yKHN0YXRlLnNlZWQsICdwcm9ncmVzc2lvbicsIHN0YXRlLmZsb29yLmluZGV4LCBtaWxlc3RvbmUuaWQsICd0b29scycpLnNodWZmbGUoWy4uLlRPT0xTXSkuc2xpY2UoMCwgMylcbn1cbmV4cG9ydCBjb25zdCBib29uQ2hvaWNlcyA9IChzdGF0ZTogUnVuU3RhdGUsIG1pbGVzdG9uZTogRmxvb3JNaWxlc3RvbmUpOiBCb29uW10gPT4ge1xuICBjb25zdCBvZmZlciA9IHJld2FyZE9mZmVyRm9yKHN0YXRlLmZsb29yLCBtaWxlc3RvbmUucmV3YXJkS2V5KVxuICBpZiAob2ZmZXI/LmtpbmQgPT09ICdib29uJykge1xuICAgIGNvbnN0IGNob2ljZXMgPSBvZmZlci5jaG9pY2VzLm1hcChjaG9pY2UgPT4gYm9vbkJ5SWRbY2hvaWNlLmlkXSkuZmlsdGVyKChjaG9pY2UpOiBjaG9pY2UgaXMgQm9vbiA9PiBCb29sZWFuKGNob2ljZSkpXG4gICAgaWYgKGNob2ljZXMubGVuZ3RoID09PSAzICYmIG5ldyBTZXQoY2hvaWNlcy5tYXAoY2hvaWNlID0+IGNob2ljZS5pZCkpLnNpemUgPT09IDMpIHJldHVybiBjaG9pY2VzXG4gIH1cbiAgY29uc3Qgb3duZWQgPSBuZXcgU2V0KE9iamVjdC5rZXlzKHN0YXRlLmhlcm8uYm9vbnMgPz8ge30pKVxuICBjb25zdCBmYW1pbGllcyA9IG5ldyBTZXQoWy4uLm93bmVkXS5tYXAoaWQgPT4gYm9vbkJ5SWRbaWRdPy5mYW1pbHkpLmZpbHRlcihCb29sZWFuKSlcbiAgY29uc3Qgc2h1ZmZsZWQgPSBybmdGb3Ioc3RhdGUuc2VlZCwgJ3Byb2dyZXNzaW9uJywgc3RhdGUuZmxvb3IuaW5kZXgsIG1pbGVzdG9uZS5pZCwgJ2Jvb25zJykuc2h1ZmZsZShbLi4uQk9PTlNdKVxuICBjb25zdCByYW5rZWQgPSBzaHVmZmxlZC5zb3J0KChhLCBiKSA9PiBOdW1iZXIob3duZWQuaGFzKGIuaWQpKSAtIE51bWJlcihvd25lZC5oYXMoYS5pZCkpIHx8IE51bWJlcihmYW1pbGllcy5oYXMoYi5mYW1pbHkpKSAtIE51bWJlcihmYW1pbGllcy5oYXMoYS5mYW1pbHkpKSlcbiAgY29uc3QgbG9jYWwgPSByYW5rZWQuZmlsdGVyKGJvb24gPT4gYm9vbi5iaW9tZXM/LmluY2x1ZGVzKHN0YXRlLmZsb29yLmJpb21lKSlcbiAgY29uc3QgZ2xvYmFsID0gcmFua2VkLmZpbHRlcihib29uID0+ICFib29uLmJpb21lcz8uaW5jbHVkZXMoc3RhdGUuZmxvb3IuYmlvbWUpKVxuICByZXR1cm4gWy4uLmxvY2FsLnNsaWNlKDAsIDIpLCAuLi5nbG9iYWxdLnNsaWNlKDAsIDMpXG59XG5cbmNvbnN0IG1pbGVzdG9uZUF0UmVhY2ggPSAoc3RhdGU6IFJ1blN0YXRlKTogRmxvb3JNaWxlc3RvbmUgfCB1bmRlZmluZWQgPT4gc3RhdGUuZmxvb3IubWlsZXN0b25lcy5maW5kKG1pbGVzdG9uZSA9PiAhbWlsZXN0b25lLmNsYWltZWQgJiYgKG1pbGVzdG9uZS5raW5kICE9PSAnYXVnbWVudCcgfHwgT2JqZWN0LnZhbHVlcyhzdGF0ZS5oZXJvLmJvb25zID8/IHt9KS5zb21lKHJhbmsgPT4gKHJhbmsgPz8gMCkgPiAwKSkgJiYgTWF0aC5tYXgoTWF0aC5hYnMobWlsZXN0b25lLnggLSBzdGF0ZS5oZXJvLngpLCBNYXRoLmFicyhtaWxlc3RvbmUueSAtIHN0YXRlLmhlcm8ueSkpIDw9IDEpXG5cbmV4cG9ydCBmdW5jdGlvbiBvcGVuTWlsZXN0b25lKHN0YXRlOiBSdW5TdGF0ZSk6IEFjdGlvblJlc3VsdCB8IHVuZGVmaW5lZCB7XG4gIGNvbnN0IG1pbGVzdG9uZSA9IG1pbGVzdG9uZUF0UmVhY2goc3RhdGUpXG4gIGlmICghbWlsZXN0b25lKSByZXR1cm4gdW5kZWZpbmVkXG4gIG1pbGVzdG9uZS5kaXNjb3ZlcmVkID0gdHJ1ZVxuICBzdGF0ZS5tb2RhbCA9IG1pbGVzdG9uZS5raW5kID09PSAnd2F5Y2FjaGUnID8geyBraW5kOiAndG9vbCcsIG1pbGVzdG9uZUlkOiBtaWxlc3RvbmUuaWQgfSA6IG1pbGVzdG9uZS5raW5kID09PSAnYXVnbWVudCcgPyB7IGtpbmQ6ICdhdWdtZW50JywgbWlsZXN0b25lSWQ6IG1pbGVzdG9uZS5pZCB9IDogbWlsZXN0b25lLmtpbmQgPT09ICdyZWxpYycgPyB7IGtpbmQ6ICdyZWxpYycsIG1pbGVzdG9uZUlkOiBtaWxlc3RvbmUuaWQgfSA6IHsga2luZDogJ2Jvb24nLCBtaWxlc3RvbmVJZDogbWlsZXN0b25lLmlkIH1cbiAgbG9nKHN0YXRlLCBtaWxlc3RvbmUua2luZCA9PT0gJ3dheWNhY2hlJyA/ICdXYXljYWNoZSBmb3VuZDogY2hvb3NlIGEgcml0dWFsIHRvb2wuJyA6IG1pbGVzdG9uZS5raW5kID09PSAnYXVnbWVudCcgPyAnQnVpbGQtdXAgbW9tZW50OiBldm9sdmUsIHJlZm9yZ2UsIG9yIHRyYW5zbXV0ZSBhIEJvb24uJyA6IG1pbGVzdG9uZS5raW5kID09PSAncmVsaWMnID8gJ0d1YXJkaWFuIGVjaG8gZm91bmQ6IGJpbmQgb25lIGFjdGl2ZSByZWxpYy4nIDogJ0Jvb24gc2l0ZSBmb3VuZDogY2hvb3NlIG9uZSBtYXJrLicpXG4gIHJldHVybiBbZXZlbnQoJ21lbnUnKV1cbn1cblxuY29uc3QgbWlsZXN0b25lID0gKHN0YXRlOiBSdW5TdGF0ZSwgaWQ6IHN0cmluZyk6IEZsb29yTWlsZXN0b25lIHwgdW5kZWZpbmVkID0+IHN0YXRlLmZsb29yLm1pbGVzdG9uZXMuZmluZChjdXJyZW50ID0+IGN1cnJlbnQuaWQgPT09IGlkICYmICFjdXJyZW50LmNsYWltZWQpXG5jb25zdCBjbGFpbSA9IChzdGF0ZTogUnVuU3RhdGUsIGN1cnJlbnQ6IEZsb29yTWlsZXN0b25lKTogdm9pZCA9PiB7XG4gIGN1cnJlbnQuY2xhaW1lZCA9IHRydWVcbiAgY29uc3QgaGVhbHRoID0gYm9vblJhbmsoc3RhdGUsICd0cmFpbFJhdGlvbnMnKSAqIDIgKyBib29uUmFuayhzdGF0ZSwgJ2R1bmVSYXRpb24nKSArIGJvb25SYW5rKHN0YXRlLCAnd2ludGVyUmF0aW9ucycpXG4gIGNvbnN0IGZvY3VzID0gYm9vblJhbmsoc3RhdGUsICdzcGlyaXRLaW5kbGluZycpICsgYm9vblJhbmsoc3RhdGUsICd3aW50ZXJSYXRpb25zJylcbiAgY29uc3QgY2FzaCA9IGJvb25SYW5rKHN0YXRlLCAnY2FjaGVTZW5zZScpICogOFxuICAgICsgYm9vblJhbmsoc3RhdGUsICdibGFja0xlZGdlcicpICogMTJcbiAgICArIGJvb25SYW5rKHN0YXRlLCAnc2FsdExlZGdlcicpICogNlxuICAgICsgYm9vblJhbmsoc3RhdGUsICdlY2hvQ2FjaGUnKSAqIG5ldyBTZXQoT2JqZWN0LmtleXMoc3RhdGUuaGVyby5ib29ucyA/PyB7fSkubWFwKGlkID0+IGJvb25CeUlkW2lkXT8uZmFtaWx5KS5maWx0ZXIoQm9vbGVhbikpLnNpemUgKiA1XG4gIHN0YXRlLmhlcm8uaGVhbHRoID0gTWF0aC5taW4oc3RhdGUuaGVyby5tYXhIZWFsdGgsIHN0YXRlLmhlcm8uaGVhbHRoICsgaGVhbHRoKVxuICBzdGF0ZS5oZXJvLmZvY3VzID0gTWF0aC5taW4oc3RhdGUuaGVyby5tYXhGb2N1cywgc3RhdGUuaGVyby5mb2N1cyArIGZvY3VzKVxuICBzdGF0ZS5oZXJvLmdvbGQgKz0gY2FzaFxufVxuXG5leHBvcnQgZnVuY3Rpb24gY2hvb3NlQm9vbihzdGF0ZTogUnVuU3RhdGUsIG1pbGVzdG9uZUlkOiBzdHJpbmcsIGNvbW1hbmQ6IHN0cmluZyk6IGJvb2xlYW4ge1xuICBjb25zdCBjdXJyZW50ID0gbWlsZXN0b25lKHN0YXRlLCBtaWxlc3RvbmVJZClcbiAgaWYgKCFjdXJyZW50KSByZXR1cm4gZmFsc2VcbiAgY29uc3QgY2hvaWNlID0gYm9vbkNob2ljZXMoc3RhdGUsIGN1cnJlbnQpW051bWJlcihjb21tYW5kKSAtIDFdXG4gIGlmICghY2hvaWNlKSByZXR1cm4gZmFsc2VcbiAgc3RhdGUuaGVyby5ib29ucyA/Pz0ge31cbiAgc3RhdGUuaGVyby5ib29uc1tjaG9pY2UuaWRdID0gKHN0YXRlLmhlcm8uYm9vbnNbY2hvaWNlLmlkXSA/PyAwKSArIDFcbiAgcmVjb3JkVGVsZW1ldHJ5Q291bnQoc3RhdGUsICdib29uUGlja3MnLCBjaG9pY2UuaWQpXG4gIGNsYWltKHN0YXRlLCBjdXJyZW50KVxuICBzdGF0ZS5tb2RhbCA9IHVuZGVmaW5lZFxuICBsb2coc3RhdGUsIGBCb29uOiAke2Nob2ljZS5uYW1lfSDCtyByYW5rICR7Ym9vblJhbmsoc3RhdGUsIGNob2ljZS5pZCl9LmApXG4gIHRlbmQoc3RhdGUsIGJvb25BbGlnbm1lbnQoY2hvaWNlLmlkKSlcbiAgaWYgKGhhc0Jvb24oc3RhdGUsICdzY291dEV5ZScpKSByZXZlYWxNaWxlc3RvbmVzKHN0YXRlKVxuICByZXR1cm4gdHJ1ZVxufVxuXG5jb25zdCBvd25lZEJvb25JZHMgPSAoc3RhdGU6IFJ1blN0YXRlKTogQm9vbklkW10gPT4gT2JqZWN0LmtleXMoc3RhdGUuaGVyby5ib29ucyA/PyB7fSkuZmlsdGVyKGlkID0+IChzdGF0ZS5oZXJvLmJvb25zPy5baWRdID8/IDApID4gMCAmJiBib29uQnlJZFtpZF0pLnNvcnQoKVxuY29uc3QgcmFyZUNob2ljZXMgPSAoc3RhdGU6IFJ1blN0YXRlLCBjdXJyZW50OiBGbG9vck1pbGVzdG9uZSwgc2VsZWN0ZWQ6IEJvb25JZCk6IEJvb25bXSA9PiBybmdGb3Ioc3RhdGUuc2VlZCwgJ3Byb2dyZXNzaW9uJywgc3RhdGUuZmxvb3IuaW5kZXgsIGN1cnJlbnQuaWQsICd0cmFuc211dGUnLCBzZWxlY3RlZCkuc2h1ZmZsZShCT09OUy5maWx0ZXIoYm9vbiA9PiBib29uLnJhcmUpKS5zbGljZSgwLCAzKVxuY29uc3QgcmVmb3JnZUNob2ljZXMgPSAoc3RhdGU6IFJ1blN0YXRlLCBjdXJyZW50OiBGbG9vck1pbGVzdG9uZSwgc2VsZWN0ZWQ6IEJvb25JZCk6IEJvb25bXSA9PiB7XG4gIGNvbnN0IHNvdXJjZSA9IGJvb25Gb3Ioc2VsZWN0ZWQpXG4gIGNvbnN0IGNhbmRpZGF0ZXMgPSBCT09OUy5maWx0ZXIoYm9vbiA9PiBib29uLmlkICE9PSBzZWxlY3RlZCAmJiAoYm9vbi5mYW1pbHkgPT09IHNvdXJjZS5mYW1pbHkgfHwgYm9vbi5yYXJlKSlcbiAgcmV0dXJuIHJuZ0ZvcihzdGF0ZS5zZWVkLCAncHJvZ3Jlc3Npb24nLCBzdGF0ZS5mbG9vci5pbmRleCwgY3VycmVudC5pZCwgJ3JlZm9yZ2UnLCBzZWxlY3RlZCkuc2h1ZmZsZShjYW5kaWRhdGVzKS5zbGljZSgwLCAzKVxufVxuXG5leHBvcnQgZnVuY3Rpb24gYXVnbWVudENob2ljZXMoc3RhdGU6IFJ1blN0YXRlLCBtaWxlc3RvbmVJZDogc3RyaW5nLCBtb2RlOiAnZXZvbHZlJyB8ICdyZWZvcmdlJyB8ICd0cmFuc211dGUnKTogQm9vbltdIHtcbiAgY29uc3QgY3VycmVudCA9IG1pbGVzdG9uZShzdGF0ZSwgbWlsZXN0b25lSWQpXG4gIGlmICghY3VycmVudCkgcmV0dXJuIFtdXG4gIGNvbnN0IHNlbGVjdGVkID0gc3RhdGUubW9kYWw/LmtpbmQgPT09ICdhdWdtZW50JyA/IHN0YXRlLm1vZGFsLnNlbGVjdGVkPy5bMF0gOiB1bmRlZmluZWRcbiAgaWYgKCFzZWxlY3RlZCkgcmV0dXJuIG93bmVkQm9vbklkcyhzdGF0ZSkubWFwKGJvb25Gb3IpXG4gIGlmIChtb2RlID09PSAncmVmb3JnZScpIHJldHVybiByZWZvcmdlQ2hvaWNlcyhzdGF0ZSwgY3VycmVudCwgc2VsZWN0ZWQpXG4gIGlmIChtb2RlID09PSAndHJhbnNtdXRlJykgcmV0dXJuIHJhcmVDaG9pY2VzKHN0YXRlLCBjdXJyZW50LCBzZWxlY3RlZClcbiAgcmV0dXJuIFtdXG59XG5cbmV4cG9ydCBmdW5jdGlvbiBjaG9vc2VBdWdtZW50KHN0YXRlOiBSdW5TdGF0ZSwgbWlsZXN0b25lSWQ6IHN0cmluZywgY29tbWFuZDogc3RyaW5nKTogYm9vbGVhbiB7XG4gIGNvbnN0IGN1cnJlbnQgPSBtaWxlc3RvbmUoc3RhdGUsIG1pbGVzdG9uZUlkKVxuICBjb25zdCBtb2RhbCA9IHN0YXRlLm1vZGFsPy5raW5kID09PSAnYXVnbWVudCcgPyBzdGF0ZS5tb2RhbCA6IHVuZGVmaW5lZFxuICBpZiAoIWN1cnJlbnQgfHwgIW1vZGFsKSByZXR1cm4gZmFsc2VcbiAgY29uc3QgbnVtZXJpYyA9IE51bWJlcihjb21tYW5kKSAtIDFcbiAgaWYgKCFtb2RhbC5tb2RlKSB7XG4gICAgY29uc3QgbW9kZSA9IChbJ2V2b2x2ZScsICdyZWZvcmdlJywgJ3RyYW5zbXV0ZSddIGFzIGNvbnN0KVtudW1lcmljXVxuICAgIGlmICghbW9kZSkgcmV0dXJuIGZhbHNlXG4gICAgc3RhdGUubW9kYWwgPSB7IC4uLm1vZGFsLCBtb2RlIH1cbiAgICBsb2coc3RhdGUsIGAke21vZGVbMF0udG9VcHBlckNhc2UoKX0ke21vZGUuc2xpY2UoMSl9OiBjaG9vc2UgYW4gb3duZWQgQm9vbi5gKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgY29uc3Qgc2VsZWN0ZWQgPSBtb2RhbC5zZWxlY3RlZD8uWzBdXG4gIGlmICghc2VsZWN0ZWQpIHtcbiAgICBjb25zdCBvd25lZCA9IG93bmVkQm9vbklkcyhzdGF0ZSlcbiAgICBjb25zdCBjaG9pY2UgPSBvd25lZFtudW1lcmljXVxuICAgIGlmICghY2hvaWNlKSByZXR1cm4gZmFsc2VcbiAgICBpZiAobW9kYWwubW9kZSA9PT0gJ2V2b2x2ZScpIHtcbiAgICAgIHN0YXRlLmhlcm8uYm9vbkV2b2x1dGlvbnMgPz89IHt9XG4gICAgICBzdGF0ZS5oZXJvLmJvb25Fdm9sdXRpb25zW2Nob2ljZV0gPSAoc3RhdGUuaGVyby5ib29uRXZvbHV0aW9uc1tjaG9pY2VdID8/IDApICsgMVxuICAgICAgcmVjb3JkVGVsZW1ldHJ5Q291bnQoc3RhdGUsICdib29uQXVnbWVudHMnLCBgZXZvbHZlOiR7Y2hvaWNlfWApXG4gICAgICBjbGFpbShzdGF0ZSwgY3VycmVudClcbiAgICAgIHN0YXRlLm1vZGFsID0gdW5kZWZpbmVkXG4gICAgICBsb2coc3RhdGUsIGAke2Jvb25Gb3IoY2hvaWNlKS5uYW1lfSBldm9sdmVzIHRvIHRpZXIgJHtzdGF0ZS5oZXJvLmJvb25Fdm9sdXRpb25zW2Nob2ljZV19LiBJdHMgZW5naW5lIHN0cmVuZ3RoZW5zLmApXG4gICAgICByZXR1cm4gdHJ1ZVxuICAgIH1cbiAgICBzdGF0ZS5tb2RhbCA9IHsgLi4ubW9kYWwsIHNlbGVjdGVkOiBbY2hvaWNlXSB9XG4gICAgbG9nKHN0YXRlLCBgQ2hvb3NlIGEgJHttb2RhbC5tb2RlfSByZXN1bHQgZm9yICR7Ym9vbkZvcihjaG9pY2UpLm5hbWV9LmApXG4gICAgcmV0dXJuIHRydWVcbiAgfVxuICBjb25zdCBjaG9pY2VzID0gbW9kYWwubW9kZSA9PT0gJ3JlZm9yZ2UnID8gcmVmb3JnZUNob2ljZXMoc3RhdGUsIGN1cnJlbnQsIHNlbGVjdGVkKSA6IHJhcmVDaG9pY2VzKHN0YXRlLCBjdXJyZW50LCBzZWxlY3RlZClcbiAgY29uc3QgY2hvaWNlID0gY2hvaWNlc1tudW1lcmljXVxuICBpZiAoIWNob2ljZSkgcmV0dXJuIGZhbHNlXG4gIGNvbnN0IHByaW9yID0gc3RhdGUuaGVyby5ib29ucz8uW3NlbGVjdGVkXSA/PyAwXG4gIGlmICghcHJpb3IpIHJldHVybiBmYWxzZVxuICBzdGF0ZS5oZXJvLmJvb25zIVtzZWxlY3RlZF0gPSBNYXRoLm1heCgwLCBwcmlvciAtIDEpXG4gIGlmIChzdGF0ZS5oZXJvLmJvb25zIVtzZWxlY3RlZF0gPT09IDApIGRlbGV0ZSBzdGF0ZS5oZXJvLmJvb25zIVtzZWxlY3RlZF1cbiAgZGVsZXRlIHN0YXRlLmhlcm8uYm9vbkV2b2x1dGlvbnM/LltzZWxlY3RlZF1cbiAgc3RhdGUuaGVyby5ib29ucyFbY2hvaWNlLmlkXSA9IChzdGF0ZS5oZXJvLmJvb25zIVtjaG9pY2UuaWRdID8/IDApICsgKG1vZGFsLm1vZGUgPT09ICdyZWZvcmdlJyA/IDEgOiBNYXRoLm1heCgxLCBwcmlvcikpXG4gIHJlY29yZFRlbGVtZXRyeUNvdW50KHN0YXRlLCAnYm9vbkF1Z21lbnRzJywgYCR7bW9kYWwubW9kZX06JHtzZWxlY3RlZH06JHtjaG9pY2UuaWR9YClcbiAgY2xhaW0oc3RhdGUsIGN1cnJlbnQpXG4gIHN0YXRlLm1vZGFsID0gdW5kZWZpbmVkXG4gIGxvZyhzdGF0ZSwgbW9kYWwubW9kZSA9PT0gJ3JlZm9yZ2UnID8gYCR7Ym9vbkZvcihzZWxlY3RlZCkubmFtZX0gcmVmb3JnZXMgaW50byAke2Nob2ljZS5uYW1lfS5gIDogYCR7Ym9vbkZvcihzZWxlY3RlZCkubmFtZX0gdHJhbnNtdXRlcyBpbnRvICR7Y2hvaWNlLm5hbWV9LmApXG4gIHRlbmQoc3RhdGUsIGJvb25BbGlnbm1lbnQoY2hvaWNlLmlkKSlcbiAgcmV0dXJuIHRydWVcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIGNob29zZVRvb2woc3RhdGU6IFJ1blN0YXRlLCBtaWxlc3RvbmVJZDogc3RyaW5nLCBjb21tYW5kOiBzdHJpbmcpOiBib29sZWFuIHtcbiAgY29uc3QgY3VycmVudCA9IG1pbGVzdG9uZShzdGF0ZSwgbWlsZXN0b25lSWQpXG4gIGlmICghY3VycmVudCkgcmV0dXJuIGZhbHNlXG4gIGNvbnN0IHRvb2xzID0gc3RhdGUuaGVyby50cmF2ZXJzYWxUb29scyA/PyBbXVxuICBjb25zdCBtb2RhbCA9IHN0YXRlLm1vZGFsPy5raW5kID09PSAndG9vbCcgPyBzdGF0ZS5tb2RhbCA6IHVuZGVmaW5lZFxuICBpZiAoIW1vZGFsKSByZXR1cm4gZmFsc2VcbiAgaWYgKHRvb2xzLmxlbmd0aCA+PSAyICYmIG1vZGFsLnJlcGxhY2UgPT09IHVuZGVmaW5lZCkge1xuICAgIGNvbnN0IHNsb3QgPSBOdW1iZXIoY29tbWFuZCkgLSAxXG4gICAgaWYgKHNsb3QgPCAwIHx8IHNsb3QgPj0gdG9vbHMubGVuZ3RoKSByZXR1cm4gZmFsc2VcbiAgICBzdGF0ZS5tb2RhbCA9IHsgLi4ubW9kYWwsIHJlcGxhY2U6IHNsb3QgfVxuICAgIGxvZyhzdGF0ZSwgYFJlcGxhY2UgJHt0b29sRm9yKHRvb2xzW3Nsb3RdKS5uYW1lfTsgY2hvb3NlIGEgV2F5Y2FjaGUgdG9vbC5gKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgY29uc3QgY2hvaWNlID0gdG9vbENob2ljZXMoc3RhdGUsIGN1cnJlbnQpW051bWJlcihjb21tYW5kKSAtIDFdXG4gIGlmICghY2hvaWNlKSByZXR1cm4gZmFsc2VcbiAgaWYgKG1vZGFsLnJlcGxhY2UgPT09IHVuZGVmaW5lZCkgdG9vbHMucHVzaChjaG9pY2UuaWQpXG4gIGVsc2UgdG9vbHNbbW9kYWwucmVwbGFjZV0gPSBjaG9pY2UuaWRcbiAgc3RhdGUuaGVyby50cmF2ZXJzYWxUb29scyA9IHRvb2xzXG4gIGNsYWltKHN0YXRlLCBjdXJyZW50KVxuICBzdGF0ZS5tb2RhbCA9IHVuZGVmaW5lZFxuICBsb2coc3RhdGUsIGBZb3UgYmluZCAke2Nob2ljZS5uYW1lfS5gKVxuICByZXR1cm4gdHJ1ZVxufVxuXG5leHBvcnQgZnVuY3Rpb24gY2hvb3NlUmVsaWMoc3RhdGU6IFJ1blN0YXRlLCBtaWxlc3RvbmVJZDogc3RyaW5nLCBjb21tYW5kOiBzdHJpbmcpOiBib29sZWFuIHtcbiAgY29uc3QgY3VycmVudCA9IG1pbGVzdG9uZShzdGF0ZSwgbWlsZXN0b25lSWQpXG4gIGNvbnN0IG1vZGFsID0gc3RhdGUubW9kYWw/LmtpbmQgPT09ICdyZWxpYycgPyBzdGF0ZS5tb2RhbCA6IHVuZGVmaW5lZFxuICBpZiAoIWN1cnJlbnQgfHwgIW1vZGFsKSByZXR1cm4gZmFsc2VcbiAgY29uc3QgcmVsaWNzID0gc3RhdGUuaGVyby5yZWxpY3MgPz89IFtdXG4gIGlmIChyZWxpY3MubGVuZ3RoID49IDMgJiYgbW9kYWwucmVwbGFjZSA9PT0gdW5kZWZpbmVkKSB7XG4gICAgY29uc3Qgc2xvdCA9IE51bWJlcihjb21tYW5kKSAtIDFcbiAgICBpZiAoc2xvdCA8IDAgfHwgc2xvdCA+PSByZWxpY3MubGVuZ3RoKSByZXR1cm4gZmFsc2VcbiAgICBzdGF0ZS5tb2RhbCA9IHsgLi4ubW9kYWwsIHJlcGxhY2U6IHNsb3QgfVxuICAgIGxvZyhzdGF0ZSwgYFJlcGxhY2UgJHtyZWxpY0ZvcihyZWxpY3Nbc2xvdF0pLm5hbWV9OyBjaG9vc2UgYSBndWFyZGlhbiByZWxpYy5gKVxuICAgIHJldHVybiB0cnVlXG4gIH1cbiAgY29uc3QgY2hvaWNlID0gcmVsaWNDaG9pY2VzKHN0YXRlLCBjdXJyZW50KVtOdW1iZXIoY29tbWFuZCkgLSAxXVxuICBpZiAoIWNob2ljZSkgcmV0dXJuIGZhbHNlXG4gIGlmIChtb2RhbC5yZXBsYWNlID09PSB1bmRlZmluZWQpIHJlbGljcy5wdXNoKGNob2ljZS5pZClcbiAgZWxzZSByZWxpY3NbbW9kYWwucmVwbGFjZV0gPSBjaG9pY2UuaWRcbiAgc3RhdGUuaGVyby5yZWxpY0NoYXJnZXMgPz89IHt9XG4gIGZvciAoY29uc3QgaWQgb2YgcmVsaWNzKSBpZiAoaWQgIT09IGNob2ljZS5pZCkgZGVsZXRlIHN0YXRlLmhlcm8ucmVsaWNDaGFyZ2VzW2lkXVxuICByZWNvcmRUZWxlbWV0cnlDb3VudChzdGF0ZSwgJ3JlbGljUGlja3MnLCBjaG9pY2UuaWQpXG4gIGNsYWltKHN0YXRlLCBjdXJyZW50KVxuICBzdGF0ZS5tb2RhbCA9IHVuZGVmaW5lZFxuICBsb2coc3RhdGUsIGBSZWxpYyBib3VuZDogJHtjaG9pY2UubmFtZX0uYClcbiAgdGVuZChzdGF0ZSwgcmVsaWNBbGlnbm1lbnQoY2hvaWNlLmlkKSlcbiAgcmV0dXJuIHRydWVcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIG9wZW5Ub29scyhzdGF0ZTogUnVuU3RhdGUpOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCB0b29scyA9IHN0YXRlLmhlcm8udHJhdmVyc2FsVG9vbHMgPz8gW11cbiAgaWYgKCF0b29scy5sZW5ndGgpIHsgbG9nKHN0YXRlLCAnTm8gcml0dWFsIHRyYXZlcnNhbCB0b29sIGlzIGJvdW5kLiBGaW5kIGEgV2F5Y2FjaGUuJyk7IHJldHVybiBbXSB9XG4gIHN0YXRlLm1vZGFsID0geyBraW5kOiAndG9vbHMnIH1cbiAgcmV0dXJuIFtldmVudCgnbWVudScpXVxufVxuXG5leHBvcnQgZnVuY3Rpb24gY2hvb3NlVG9vbFVzZShzdGF0ZTogUnVuU3RhdGUsIGNvbW1hbmQ6IHN0cmluZyk6IGJvb2xlYW4ge1xuICBjb25zdCB0b29sID0gc3RhdGUuaGVyby50cmF2ZXJzYWxUb29scz8uW051bWJlcihjb21tYW5kKSAtIDFdXG4gIGlmICghdG9vbCkgcmV0dXJuIGZhbHNlXG4gIGNvbnN0IGNvb2xkb3duID0gdG9vbENvb2xkb3duKHN0YXRlLCB0b29sKVxuICBpZiAoY29vbGRvd24gJiYgIWlzVGVycmFpbk11dGF0aW9uVG9vbCh0b29sKSkgeyBsb2coc3RhdGUsIGAke3Rvb2xGb3IodG9vbCkubmFtZX0gcmVjb3ZlcnMgaW4gJHtjb29sZG93bn0gdHVybiR7Y29vbGRvd24gPT09IDEgPyAnJyA6ICdzJ30uYCk7IHJldHVybiBmYWxzZSB9XG4gIHN0YXRlLm1vZGFsID0geyBraW5kOiAndGFyZ2V0JywgYWN0aW9uOiB0b29sLCB0b29sIH1cbiAgcmV0dXJuIHRydWVcbn1cblxuY29uc3Qgc2V0Q29vbGRvd24gPSAoc3RhdGU6IFJ1blN0YXRlLCB0b29sOiBUcmF2ZXJzYWxUb29sSWQpID0+IHsgKHN0YXRlLmhlcm8uY29vbGRvd25zID8/PSB7fSlbYHRvb2w6JHt0b29sfWBdID0gTWF0aC5tYXgoMSwgdG9vbEZvcih0b29sKS5jb29sZG93biAtIGJvb25SYW5rKHN0YXRlLCAnY29vbEFzaCcpKSB9XG5jb25zdCBwb2ludCA9IChzdGF0ZTogUnVuU3RhdGUsIGRpcmVjdGlvbjogRXhjbHVkZTxrZXlvZiB0eXBlb2YgRElSRUNUSU9OUywgJ3dhaXQnPiwgZGlzdGFuY2U6IG51bWJlcikgPT4gKHsgeDogc3RhdGUuaGVyby54ICsgRElSRUNUSU9OU1tkaXJlY3Rpb25dLnggKiBkaXN0YW5jZSwgeTogc3RhdGUuaGVyby55ICsgRElSRUNUSU9OU1tkaXJlY3Rpb25dLnkgKiBkaXN0YW5jZSB9KVxuY29uc3QgcGFzc2FibGVMYW5kaW5nID0gKHN0YXRlOiBSdW5TdGF0ZSwgdGFyZ2V0OiB7IHg6IG51bWJlcjsgeTogbnVtYmVyIH0pID0+IGlzUGFzc2FibGUoc3RhdGUuZmxvb3IsIHRhcmdldC54LCB0YXJnZXQueSlcblxuZXhwb3J0IGZ1bmN0aW9uIHVzZVRvb2woc3RhdGU6IFJ1blN0YXRlLCB0b29sOiBUcmF2ZXJzYWxUb29sSWQsIGRpcmVjdGlvbjogRXhjbHVkZTxrZXlvZiB0eXBlb2YgRElSRUNUSU9OUywgJ3dhaXQnPiwgb3ZlcmRyaXZlID0gZmFsc2UpOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCBtdXRhdGlvbiA9IGlzVGVycmFpbk11dGF0aW9uVG9vbCh0b29sKSA/IHRlcnJhaW5NdXRhdGlvbkFzc2Vzc21lbnQoc3RhdGUsIHRvb2wsIGRpcmVjdGlvbiwgb3ZlcmRyaXZlKSA6IHVuZGVmaW5lZFxuICBjb25zdCB0ZXJyYWluUmVqZWN0aW9uTWVzc2FnZSA9IChyZWFzb246IHN0cmluZyk6IHN0cmluZyA9PiByZWFzb24gPT09ICd1bmJvdW5kJyA/ICdUaGF0IHJpdHVhbCB0b29sIGlzIG5vIGxvbmdlciBib3VuZC4nIDogcmVhc29uID09PSAnY29vbGRvd24nID8gYCR7dG9vbEZvcih0b29sKS5uYW1lfSBpcyBzdGlsbCByZWNvdmVyaW5nLmAgOiByZWFzb24gPT09ICd0YXJnZXQtdW5zZWVuJyA/ICdUaGF0IHRlcnJhaW4gdGFyZ2V0IGlzIG5vdCB2aXNpYmxlLicgOiByZWFzb24gPT09ICdkZXN0aW5hdGlvbi11bnNlZW4nID8gJ1RoYXQgdGVycmFpbiBkZXN0aW5hdGlvbiBpcyBub3QgdmlzaWJsZS4nIDogdG9vbCA9PT0gJ3N0b25lQWR6ZScgPyAnU3RvbmUgQWR6ZSBjdXRzIG9ubHkgYW4gYWRqYWNlbnQgd29vZGVuIGJhcnJpZXIgb3Igd2Vha2VuZWQgcm91dGUuJyA6IHRvb2wgPT09ICdyZXNpbkZpcmVCYXNrZXQnID8gJ1Jlc2luIEZpcmUgQmFza2V0IG5lZWRzIGFuIHVub2NjdXBpZWQgYWRqYWNlbnQgYnJhbWJsZSBvciB3ZWIuJyA6IHRvb2wgPT09ICdhbnRsZXJQcnliYXInID8gcmVhc29uID09PSAnZGVzdGluYXRpb24tYmxvY2tlZCcgPyAnQW50bGVyIFByeWJhciBuZWVkcyBhbiBlbXB0eSBsZWdhbCBkZXN0aW5hdGlvbiBiZXlvbmQgdGhlIHRhcmdldC4nIDogJ0FudGxlciBQcnliYXIgbmVlZHMgYW4gYWRqYWNlbnQgYm91bGRlciBvciBicmVha3dhbGwuJyA6IHJlYXNvbiA9PT0gJ2Rlc3RpbmF0aW9uLWJsb2NrZWQnID8gJ1dvb2RlbiBMZXZlciBhbmQgUm9sbGVyIG5lZWRzIGFuIGVtcHR5IGxlZ2FsIGRlc3RpbmF0aW9uLicgOiAnV29vZGVuIExldmVyIGFuZCBSb2xsZXIgbmVlZHMgYW4gYWRqYWNlbnQgbW92YWJsZSBwcm9wLidcbiAgY29uc3QgdGVycmFpblJlamVjdGVkID0gKHJlYXNvbjogVGVycmFpbk11dGF0aW9uUmVhc29uKTogQWN0aW9uUmVzdWx0ID0+IHsgcmVjb3JkSW50ZXJhY3Rpb24oc3RhdGUsICdyZWplY3RlZEludGVyYWN0aW9ucycsIGB0ZXJyYWluOiR7dG9vbH06JHtyZWFzb259YCk7IGlmIChtdXRhdGlvbj8ucm91dGVCbG9ja2VkKSByZWNvcmRJbnRlcmFjdGlvbihzdGF0ZSwgJ3JvdXRlRmFpbHVyZXMnLCBgdGVycmFpbjoke3Rvb2x9Om1hbmRhdG9yeS1yb3V0ZWApOyBsb2coc3RhdGUsIGB0ZXJyYWluOiR7dG9vbH06YXR0ZW1wdGVkYCk7IGxvZyhzdGF0ZSwgYHRlcnJhaW46JHt0b29sfTpyZWplY3RlZDoke3JlYXNvbn1gKTsgbG9nKHN0YXRlLCB0ZXJyYWluUmVqZWN0aW9uTWVzc2FnZShyZWFzb24pKTsgcmV0dXJuIFtldmVudCgndGVycmFpbicsIHRvb2wsICdhdHRlbXB0ZWQnKSwgZXZlbnQoJ3RlcnJhaW4nLCB0b29sLCBgcmVqZWN0ZWQ6JHtyZWFzb259YCldIH1cbiAgaWYgKCEoc3RhdGUuaGVyby50cmF2ZXJzYWxUb29scyA/PyBbXSkuaW5jbHVkZXModG9vbCkpIHsgaWYgKG11dGF0aW9uKSByZXR1cm4gdGVycmFpblJlamVjdGVkKCd1bmJvdW5kJyk7IGxvZyhzdGF0ZSwgJ1RoYXQgcml0dWFsIHRvb2wgaXMgbm8gbG9uZ2VyIGJvdW5kLicpOyByZXR1cm4gW10gfVxuICBpZiAodG9vbENvb2xkb3duKHN0YXRlLCB0b29sKSkgeyBpZiAobXV0YXRpb24pIHJldHVybiB0ZXJyYWluUmVqZWN0ZWQoJ2Nvb2xkb3duJyk7IGxvZyhzdGF0ZSwgYCR7dG9vbEZvcih0b29sKS5uYW1lfSBpcyBzdGlsbCByZWNvdmVyaW5nLmApOyByZXR1cm4gW10gfVxuICBpZiAobXV0YXRpb24gJiYgIW11dGF0aW9uLnJlYWR5KSByZXR1cm4gdGVycmFpblJlamVjdGVkKG11dGF0aW9uLnJlYXNvbilcbiAgaWYgKG11dGF0aW9uKSBsb2coc3RhdGUsIGB0ZXJyYWluOiR7dG9vbH06YXR0ZW1wdGVkYClcbiAgY29uc3QgcmVzdWx0ID0gdG9vbCA9PT0gJ3N0b25lV2VkZ2UnID8gdXNlU3RvbmVXZWRnZShzdGF0ZSwgZGlyZWN0aW9uLCBvdmVyZHJpdmUpIDogdG9vbCA9PT0gJ3JlZWR3aW5nJyA/IHVzZVJlZWR3aW5nKHN0YXRlLCBkaXJlY3Rpb24sIG92ZXJkcml2ZSkgOiB0b29sID09PSAnYXNod2F5Uml0ZXMnID8gdXNlQXNod2F5KHN0YXRlLCBkaXJlY3Rpb24sIG92ZXJkcml2ZSkgOiB0b29sID09PSAnYW50bGVyUHJ5YmFyJyA/IHVzZUFudGxlclByeWJhcihzdGF0ZSwgZGlyZWN0aW9uKSA6IHRvb2wgPT09ICdzdG9uZUFkemUnID8gdXNlU3RvbmVBZHplKHN0YXRlLCBkaXJlY3Rpb24pIDogdG9vbCA9PT0gJ3Jlc2luRmlyZUJhc2tldCcgPyB1c2VSZXNpbkZpcmVCYXNrZXQoc3RhdGUsIGRpcmVjdGlvbiwgb3ZlcmRyaXZlKSA6IHRvb2wgPT09ICdjb3JkQW5jaG9yJyA/IHVzZUNvcmRBbmNob3Ioc3RhdGUsIGRpcmVjdGlvbiwgb3ZlcmRyaXZlKSA6IHVzZVdvb2RlbkxldmVyUm9sbGVyKHN0YXRlLCBkaXJlY3Rpb24pXG4gIGlmICghcmVzdWx0KSByZXR1cm4gbXV0YXRpb24gPyB0ZXJyYWluUmVqZWN0ZWQoJ2V4ZWN1dGlvbi1mYWlsZWQnKSA6IFtdXG4gIHJlY29yZEludGVyYWN0aW9uKHN0YXRlLCAndGVycmFpblRvb2xVc2VzJywgYCR7c3RhdGUuZmxvb3IuYmlvbWV9OiR7dG9vbH1gKVxuICBjb25zdCBoZWFsaW5nID0gYm9vblJhbmsoc3RhdGUsICdyb290ZWRSZXNvbHZlJykgKyAoc3RhdGUuaGVyby5oZWFsdGggKiA0IDw9IHN0YXRlLmhlcm8ubWF4SGVhbHRoID8gYm9vblJhbmsoc3RhdGUsICdsYXN0TGlnaHQnKSAqIDIgOiAwKVxuICBpZiAoaGVhbGluZykgc3RhdGUuaGVyby5oZWFsdGggPSBNYXRoLm1pbihzdGF0ZS5oZXJvLm1heEhlYWx0aCwgc3RhdGUuaGVyby5oZWFsdGggKyBoZWFsaW5nKVxuICBjb25zdCBmb2N1cyA9IGJvb25SYW5rKHN0YXRlLCAnY29yZFRlbXBvJylcbiAgaWYgKGZvY3VzKSBzdGF0ZS5oZXJvLmZvY3VzID0gTWF0aC5taW4oc3RhdGUuaGVyby5tYXhGb2N1cywgc3RhdGUuaGVyby5mb2N1cyArIGZvY3VzKVxuICBpZiAoY29uc3VtZVJlbGljVG9vbChzdGF0ZSkpIHsgc3RhdGUuaGVyby5oZWFsdGggPSBNYXRoLm1pbihzdGF0ZS5oZXJvLm1heEhlYWx0aCwgc3RhdGUuaGVyby5oZWFsdGggKyAzKTsgbG9nKHN0YXRlLCAnVGlkZSBGZXR0ZXIgcmVzdG9yZXMgMyBIUCBhZnRlciB0aGUgY3Jvc3NpbmcuJykgfVxuICBhcm1SZWxpY1RyYXZlcnNhbChzdGF0ZSlcbiAgaWYgKGJvb25SYW5rKHN0YXRlLCAncHJlc3N1cmVTZWFsJykpIHN0YXRlLmhlcm8uY29uZGl0aW9ucyA9IFsuLi4oc3RhdGUuaGVyby5jb25kaXRpb25zID8/IFtdKSwgeyBraW5kOiAnc2hpZWxkZWQnLCBkdXJhdGlvbjogMiwgcG90ZW5jeTogYm9vblJhbmsoc3RhdGUsICdwcmVzc3VyZVNlYWwnKSB9XVxuICBpZiAoYm9vblJhbmsoc3RhdGUsICd3YXlmaW5kZXJDb3JkJykpIHJldmVhbE5lYXJieU1pbGVzdG9uZXMoc3RhdGUsIDIgKyBib29uUmFuayhzdGF0ZSwgJ3dheWZpbmRlckNvcmQnKSlcbiAgaWYgKG92ZXJkcml2ZSkge1xuICAgIHN0YXRlLmhlcm8udHJhdmVyc2FsVG9vbHMgPSAoc3RhdGUuaGVyby50cmF2ZXJzYWxUb29scyA/PyBbXSkuZmlsdGVyKGN1cnJlbnQgPT4gY3VycmVudCAhPT0gdG9vbClcbiAgICBsb2coc3RhdGUsIGAke3Rvb2xGb3IodG9vbCkubmFtZX0gYnVybnMgb3V0IGFmdGVyIGl0cyBvdmVyZHJpdmUuYClcbiAgfSBlbHNlIHtcbiAgICBzZXRDb29sZG93bihzdGF0ZSwgdG9vbClcbiAgICBjb25zdCBjb29sZG93biA9IHRvb2xDb29sZG93bihzdGF0ZSwgdG9vbClcbiAgICBjb25zdCByZWxheSA9IGJvb25SYW5rKHN0YXRlLCAncmVsYXlTdGVwJylcbiAgICBpZiAocmVsYXkgJiYgY29vbGRvd24pIHN0YXRlLmhlcm8uY29vbGRvd25zIVtgdG9vbDoke3Rvb2x9YF0gPSBNYXRoLm1heCgxLCBjb29sZG93biAtIHJlbGF5KVxuICB9XG4gIGlmIChtdXRhdGlvbikge1xuICAgIGxvZyhzdGF0ZSwgYHRlcnJhaW46JHt0b29sfTpyZXNvbHZlZDoke211dGF0aW9uLnJlYXNvbn1gKVxuICAgIGlmIChtdXRhdGlvbi5yaXNrKSBsb2coc3RhdGUsIGB0ZXJyYWluOiR7dG9vbH06aGF6YXJkOiR7bXV0YXRpb24ucmlza31gKVxuICB9XG4gIHJlZnJlc2hGb3Yoc3RhdGUpXG4gIHJldHVybiBhZHZhbmNlKHN0YXRlLCBbZXZlbnQoJ3NwZWxsJyksIC4uLihtdXRhdGlvbiA/IFtldmVudCgndGVycmFpbicsIHRvb2wsICdhdHRlbXB0ZWQnKSwgZXZlbnQoJ3RlcnJhaW4nLCB0b29sLCAncmVzb2x2ZWQnKSwgLi4uKG11dGF0aW9uLnJpc2sgPyBbZXZlbnQoJ3RlcnJhaW4nLCB0b29sLCBgaGF6YXJkOiR7bXV0YXRpb24ucmlza31gKV0gOiBbXSksIC4uLihvdmVyZHJpdmUgPyBbZXZlbnQoJ3RlcnJhaW4nLCB0b29sLCAncmV0aXJlZCcpXSA6IFtdKV0gOiBbXSldKVxufVxuXG5jb25zdCB1c2VTdG9uZVdlZGdlID0gKHN0YXRlOiBSdW5TdGF0ZSwgZGlyZWN0aW9uOiBFeGNsdWRlPGtleW9mIHR5cGVvZiBESVJFQ1RJT05TLCAnd2FpdCc+LCBvdmVyZHJpdmU6IGJvb2xlYW4pOiBib29sZWFuID0+IHtcbiAgY29uc3QgdGFyZ2V0ID0gcG9pbnQoc3RhdGUsIGRpcmVjdGlvbiwgMSlcbiAgY29uc3Qgd2lkdGggPSBvdmVyZHJpdmUgPyAxIDogYm9vblJhbmsoc3RhdGUsICdzdG9uZU1lbW9yeScpICsgYm9vblJhbmsoc3RhdGUsICd3YWxsU29uZycpXG4gIGNvbnN0IG9mZnNldHMgPSBBcnJheS5mcm9tKHsgbGVuZ3RoOiB3aWR0aCAqIDIgKyAxIH0sIChfLCBpbmRleCkgPT4gaW5kZXggLSB3aWR0aCkubWFwKG9mZnNldCA9PiBbRElSRUNUSU9OU1tkaXJlY3Rpb25dLnkgKiBvZmZzZXQsIC1ESVJFQ1RJT05TW2RpcmVjdGlvbl0ueCAqIG9mZnNldF0pXG4gIGNvbnN0IGNsZWFyZWQgPSBvZmZzZXRzLm1hcCgoW3gsIHldKSA9PiBnZXRUaWxlKHN0YXRlLmZsb29yLCB0YXJnZXQueCArIHgsIHRhcmdldC55ICsgeSkpLmZpbHRlcigodGlsZSk6IHRpbGUgaXMgTm9uTnVsbGFibGU8UmV0dXJuVHlwZTx0eXBlb2YgZ2V0VGlsZT4+ID0+IEJvb2xlYW4odGlsZSAmJiBkcmlsbGFibGUuaGFzKHRpbGUua2luZCkpKVxuICBpZiAoIWNsZWFyZWQubGVuZ3RoKSB7IGxvZyhzdGF0ZSwgJ1N0b25lIFdlZGdlIG5lZWRzIGJsb2NrZWQgZ3JvdW5kLicpOyByZXR1cm4gZmFsc2UgfVxuICBjbGVhcmVkLmZvckVhY2godGlsZSA9PiB7IHRpbGUua2luZCA9ICdmbG9vcicgfSlcbiAgbG9nKHN0YXRlLCBvdmVyZHJpdmUgPyAnVGhlIHdlZGdlIG9wZW5zIGEgYnJvYWQgcGFzc2FnZS4nIDogJ1RoZSB3ZWRnZSBvcGVucyBhIG5hcnJvdyBwYXNzYWdlLicpXG4gIHJldHVybiB0cnVlXG59XG5cbmNvbnN0IHVzZVJlZWR3aW5nID0gKHN0YXRlOiBSdW5TdGF0ZSwgZGlyZWN0aW9uOiBFeGNsdWRlPGtleW9mIHR5cGVvZiBESVJFQ1RJT05TLCAnd2FpdCc+LCBvdmVyZHJpdmU6IGJvb2xlYW4pOiBib29sZWFuID0+IHtcbiAgY29uc3QgbGVuZ3RoID0gKG92ZXJkcml2ZSA/IDMgOiAyKSArIGJvb25SYW5rKHN0YXRlLCAncmVlZE1lbW9yeScpICsgYm9vblJhbmsoc3RhdGUsICd1cGRyYWZ0U3RlcCcpXG4gIGNvbnN0IGxhbmRpbmcgPSBwb2ludChzdGF0ZSwgZGlyZWN0aW9uLCBsZW5ndGgpXG4gIGNvbnN0IGNyb3NzZWQgPSBBcnJheS5mcm9tKHsgbGVuZ3RoOiBsZW5ndGggLSAxIH0sIChfLCBpbmRleCkgPT4gZ2V0VGlsZShzdGF0ZS5mbG9vciwgcG9pbnQoc3RhdGUsIGRpcmVjdGlvbiwgaW5kZXggKyAxKS54LCBwb2ludChzdGF0ZSwgZGlyZWN0aW9uLCBpbmRleCArIDEpLnkpKVxuICBpZiAoIWNyb3NzZWQuc29tZSh0aWxlID0+IHRpbGUgJiYgaGF6YXJkb3VzLmhhcyh0aWxlLmtpbmQpKSB8fCAhcGFzc2FibGVMYW5kaW5nKHN0YXRlLCBsYW5kaW5nKSkgeyBsb2coc3RhdGUsICdSZWVkd2luZyBuZWVkcyBoYXphcmRvdXMgZ3JvdW5kIGFuZCBhIGNsZWFyIGxhbmRpbmcuJyk7IHJldHVybiBmYWxzZSB9XG4gIHN0YXRlLmhlcm8ueCA9IGxhbmRpbmcueFxuICBzdGF0ZS5oZXJvLnkgPSBsYW5kaW5nLnlcbiAgbG9nKHN0YXRlLCAnUmVlZHdpbmcgY2FycmllcyB5b3UgYWNyb3NzIHRoZSBoYXphcmQuJylcbiAgcmV0dXJuIHRydWVcbn1cblxuY29uc3QgdXNlQ29yZEFuY2hvciA9IChzdGF0ZTogUnVuU3RhdGUsIGRpcmVjdGlvbjogRXhjbHVkZTxrZXlvZiB0eXBlb2YgRElSRUNUSU9OUywgJ3dhaXQnPiwgb3ZlcmRyaXZlOiBib29sZWFuKTogYm9vbGVhbiA9PiB7XG4gIGNvbnN0IGxhbmRpbmcgPSBwb2ludChzdGF0ZSwgZGlyZWN0aW9uLCBvdmVyZHJpdmUgPyA0IDogMilcbiAgaWYgKCFwYXNzYWJsZUxhbmRpbmcoc3RhdGUsIGxhbmRpbmcpKSB7IGxvZyhzdGF0ZSwgJ0NvcmQgQW5jaG9yIG5lZWRzIGEgY2xlYXIgbGFuZGluZy4nKTsgcmV0dXJuIGZhbHNlIH1cbiAgc3RhdGUuaGVyby54ID0gbGFuZGluZy54XG4gIHN0YXRlLmhlcm8ueSA9IGxhbmRpbmcueVxuICBsb2coc3RhdGUsICdUaGUgY29yZCBkcmF3cyB5b3UgYWNyb3NzIHRoZSBnYXAuJylcbiAgcmV0dXJuIHRydWVcbn1cblxuY29uc3QgdXNlQXNod2F5ID0gKHN0YXRlOiBSdW5TdGF0ZSwgZGlyZWN0aW9uOiBFeGNsdWRlPGtleW9mIHR5cGVvZiBESVJFQ1RJT05TLCAnd2FpdCc+LCBvdmVyZHJpdmU6IGJvb2xlYW4pOiBib29sZWFuID0+IHtcbiAgY29uc3QgbGVuZ3RoID0gb3ZlcmRyaXZlID8gMyA6IDJcbiAgY29uc3QgY2VsbHMgPSBBcnJheS5mcm9tKHsgbGVuZ3RoIH0sIChfLCBpbmRleCkgPT4gcG9pbnQoc3RhdGUsIGRpcmVjdGlvbiwgaW5kZXggKyAxKSlcbiAgY29uc3QgdGFyZ2V0cyA9IGNlbGxzLm1hcChjZWxsID0+ICh7IGNlbGwsIHRpbGU6IGdldFRpbGUoc3RhdGUuZmxvb3IsIGNlbGwueCwgY2VsbC55KSB9KSkuZmlsdGVyKChlbnRyeSk6IGVudHJ5IGlzIHsgY2VsbDogeyB4OiBudW1iZXI7IHk6IG51bWJlciB9OyB0aWxlOiBOb25OdWxsYWJsZTxSZXR1cm5UeXBlPHR5cGVvZiBnZXRUaWxlPj4gfSA9PiBCb29sZWFuKGVudHJ5LnRpbGUgJiYgaGF6YXJkb3VzLmhhcyhlbnRyeS50aWxlLmtpbmQpICYmIGVudHJ5LnRpbGUua2luZCAhPT0gJ2JvdWxkZXInKSlcbiAgaWYgKCF0YXJnZXRzLmxlbmd0aCkgeyBsb2coc3RhdGUsICdBc2h3YXkgUml0ZXMgbmVlZCBoYXphcmRvdXMgZ3JvdW5kIHRvIGJpbmQuJyk7IHJldHVybiBmYWxzZSB9XG4gIHN0YXRlLmZsb29yLnRyYW5zaWVudFRlcnJhaW4gPz89IFtdXG4gIHRhcmdldHMuZm9yRWFjaCgoeyBjZWxsLCB0aWxlIH0pID0+IHtcbiAgICBjb25zdCBleGlzdGluZyA9IHN0YXRlLmZsb29yLnRyYW5zaWVudFRlcnJhaW4hLmZpbmQoY3VycmVudCA9PiBjdXJyZW50LnggPT09IGNlbGwueCAmJiBjdXJyZW50LnkgPT09IGNlbGwueSlcbiAgICBpZiAoIWV4aXN0aW5nKSBzdGF0ZS5mbG9vci50cmFuc2llbnRUZXJyYWluIS5wdXNoKHsgLi4uY2VsbCwgb3JpZ2luYWw6IHRpbGUua2luZCwgLi4uKHRpbGUuZmxvdyA/IHsgb3JpZ2luYWxGbG93OiB7IC4uLnRpbGUuZmxvdyB9IH0gOiB7fSksIGV4cGlyZXNBdDogc3RhdGUudHVybiArIDYgfSlcbiAgICB0aWxlLmtpbmQgPSAnZmxvb3InXG4gICAgZGVsZXRlIHRpbGUuZmxvd1xuICB9KVxuICBsb2coc3RhdGUsICdXYXJtIGFzaCBzZXR0bGVzIGludG8gYSB0ZW1wb3Jhcnkgcm91dGUuJylcbiAgcmV0dXJuIHRydWVcbn1cblxuY29uc3QgdXNlQW50bGVyUHJ5YmFyID0gKHN0YXRlOiBSdW5TdGF0ZSwgZGlyZWN0aW9uOiBFeGNsdWRlPGtleW9mIHR5cGVvZiBESVJFQ1RJT05TLCAnd2FpdCc+KTogYm9vbGVhbiA9PiB7XG4gIGNvbnN0IHRhcmdldCA9IHBvaW50KHN0YXRlLCBkaXJlY3Rpb24sIDEpXG4gIGNvbnN0IGRlc3RpbmF0aW9uID0gcG9pbnQoc3RhdGUsIGRpcmVjdGlvbiwgMilcbiAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIHRhcmdldC54LCB0YXJnZXQueSlcbiAgY29uc3QgbGFuZGluZyA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIGRlc3RpbmF0aW9uLngsIGRlc3RpbmF0aW9uLnkpXG4gIGNvbnN0IHByb3RlY3RlZERlc3RpbmF0aW9uID0gZGVzdGluYXRpb24ueCA9PT0gc3RhdGUuZmxvb3Iuc3RhcnQueCAmJiBkZXN0aW5hdGlvbi55ID09PSBzdGF0ZS5mbG9vci5zdGFydC55IHx8IGRlc3RpbmF0aW9uLnggPT09IHN0YXRlLmZsb29yLmV4aXQueCAmJiBkZXN0aW5hdGlvbi55ID09PSBzdGF0ZS5mbG9vci5leGl0LnkgfHwgc3RhdGUuZmxvb3IuYWN0b3JzLnNvbWUoYWN0b3IgPT4gYWN0b3IuaGVhbHRoID4gMCAmJiBhY3Rvci54ID09PSBkZXN0aW5hdGlvbi54ICYmIGFjdG9yLnkgPT09IGRlc3RpbmF0aW9uLnkpIHx8IHN0YXRlLmZsb29yLm1pbGVzdG9uZXMuc29tZShtaWxlc3RvbmUgPT4gIW1pbGVzdG9uZS5jbGFpbWVkICYmIG1pbGVzdG9uZS54ID09PSBkZXN0aW5hdGlvbi54ICYmIG1pbGVzdG9uZS55ID09PSBkZXN0aW5hdGlvbi55KVxuICBpZiAoIXRpbGUgfHwgIVsnYm91bGRlcicsICdicmVha3dhbGwnXS5pbmNsdWRlcyh0aWxlLmtpbmQpKSB7IGxvZyhzdGF0ZSwgJ0FudGxlciBQcnliYXIgbmVlZHMgYW4gYWRqYWNlbnQgYm91bGRlciBvciBicmVha3dhbGwuJyk7IHJldHVybiBmYWxzZSB9XG4gIGlmICghbGFuZGluZyB8fCBsYW5kaW5nLmtpbmQgIT09ICdmbG9vcicgfHwgcHJvdGVjdGVkRGVzdGluYXRpb24gfHwgc3RhdGUuZmxvb3IucHJvcHMuc29tZShwcm9wID0+IHByb3Auc3RhdGUgIT09ICdkZXN0cm95ZWQnICYmIHByb3AueCA9PT0gZGVzdGluYXRpb24ueCAmJiBwcm9wLnkgPT09IGRlc3RpbmF0aW9uLnkpKSB7IGxvZyhzdGF0ZSwgJ0FudGxlciBQcnliYXIgbmVlZHMgYW4gZW1wdHkgbGVnYWwgZGVzdGluYXRpb24gYmV5b25kIHRoZSB0YXJnZXQuJyk7IHJldHVybiBmYWxzZSB9XG4gIGxhbmRpbmcua2luZCA9IHRpbGUua2luZFxuICB0aWxlLmtpbmQgPSAnZmxvb3InXG4gIHN0YXRlLmhlcm8uY29uZGl0aW9ucyA9IFsuLi4oc3RhdGUuaGVyby5jb25kaXRpb25zID8/IFtdKSwgeyBraW5kOiAnbWFya2VkJywgZHVyYXRpb246IDIsIHBvdGVuY3k6IDEgfV1cbiAgbG9nKHN0YXRlLCAnVGhlIEFudGxlciBQcnliYXIgc2hpZnRzIHRoZSBiYXJyaWVyOyB0aGUgbm9pc2UgbGVhdmVzIHlvdSBleHBvc2VkLicpXG4gIHJldHVybiB0cnVlXG59XG5cbmNvbnN0IHVzZVN0b25lQWR6ZSA9IChzdGF0ZTogUnVuU3RhdGUsIGRpcmVjdGlvbjogRXhjbHVkZTxrZXlvZiB0eXBlb2YgRElSRUNUSU9OUywgJ3dhaXQnPik6IGJvb2xlYW4gPT4ge1xuICBjb25zdCB0YXJnZXQgPSBwb2ludChzdGF0ZSwgZGlyZWN0aW9uLCAxKVxuICBjb25zdCB0aWxlID0gZ2V0VGlsZShzdGF0ZS5mbG9vciwgdGFyZ2V0LngsIHRhcmdldC55KVxuICBpZiAoIXRpbGUgfHwgIVsnY3JhdGUnLCAnY3J1bWJsZSddLmluY2x1ZGVzKHRpbGUua2luZCkpIHsgbG9nKHN0YXRlLCAnU3RvbmUgQWR6ZSBjdXRzIG9ubHkgYW4gYWRqYWNlbnQgd29vZGVuIGJhcnJpZXIgb3Igd2Vha2VuZWQgcm91dGUuJyk7IHJldHVybiBmYWxzZSB9XG4gIHRpbGUua2luZCA9ICdmbG9vcidcbiAgbG9nKHN0YXRlLCAnVGhlIFN0b25lIEFkemUgY3V0cyBhIG5hcnJvdyByb3V0ZSB0aHJvdWdoIHRoZSB3ZWFrZW5lZCBiYXJyaWVyLicpXG4gIHJldHVybiB0cnVlXG59XG5cbmNvbnN0IHVzZVJlc2luRmlyZUJhc2tldCA9IChzdGF0ZTogUnVuU3RhdGUsIGRpcmVjdGlvbjogRXhjbHVkZTxrZXlvZiB0eXBlb2YgRElSRUNUSU9OUywgJ3dhaXQnPiwgb3ZlcmRyaXZlOiBib29sZWFuKTogYm9vbGVhbiA9PiB7XG4gIGNvbnN0IHRhcmdldCA9IHBvaW50KHN0YXRlLCBkaXJlY3Rpb24sIDEpXG4gIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCB0YXJnZXQueCwgdGFyZ2V0LnkpXG4gIGlmICghdGlsZSB8fCAhWydicmFtYmxlJywgJ3dlYiddLmluY2x1ZGVzKHRpbGUua2luZCkgfHwgc3RhdGUuZmxvb3IuYWN0b3JzLnNvbWUoYWN0b3IgPT4gYWN0b3IuaGVhbHRoID4gMCAmJiBhY3Rvci54ID09PSB0YXJnZXQueCAmJiBhY3Rvci55ID09PSB0YXJnZXQueSkgfHwgdGFyZ2V0LnggPT09IHN0YXRlLmZsb29yLmV4aXQueCAmJiB0YXJnZXQueSA9PT0gc3RhdGUuZmxvb3IuZXhpdC55KSB7IGxvZyhzdGF0ZSwgJ1Jlc2luIEZpcmUgQmFza2V0IG5lZWRzIGFuIHVub2NjdXBpZWQgYWRqYWNlbnQgYnJhbWJsZSBvciB3ZWIuJyk7IHJldHVybiBmYWxzZSB9XG4gIHRpbGUua2luZCA9ICdzbW9rZSdcbiAgc3RhdGUuaGVyby5jb25kaXRpb25zID0gWy4uLihzdGF0ZS5oZXJvLmNvbmRpdGlvbnMgPz8gW10pLCB7IGtpbmQ6ICdidXJuaW5nJywgZHVyYXRpb246IG92ZXJkcml2ZSA/IDMgOiAyLCBwb3RlbmN5OiAxIH1dXG4gIGxvZyhzdGF0ZSwgb3ZlcmRyaXZlID8gJ1RoZSByZXNpbiBmbGFyZXMgd2lkZTsgc21va2UgYW5kIGhlYXQgY2xpbmcgdG8geW91LicgOiAnVGhlIHJlc2luIGJ1cm5zIHRoZSBncm93dGggaW50byBhIHNtb2tpbmcsIHJpc2t5IGNyb3NzaW5nLicpXG4gIHJldHVybiB0cnVlXG59XG5cbmNvbnN0IHVzZVdvb2RlbkxldmVyUm9sbGVyID0gKHN0YXRlOiBSdW5TdGF0ZSwgZGlyZWN0aW9uOiBFeGNsdWRlPGtleW9mIHR5cGVvZiBESVJFQ1RJT05TLCAnd2FpdCc+KTogYm9vbGVhbiA9PiB7XG4gIGNvbnN0IHRhcmdldCA9IHBvaW50KHN0YXRlLCBkaXJlY3Rpb24sIDEpXG4gIGNvbnN0IGRlc3RpbmF0aW9uID0gcG9pbnQoc3RhdGUsIGRpcmVjdGlvbiwgMilcbiAgY29uc3QgcHJvcCA9IHN0YXRlLmZsb29yLnByb3BzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS54ID09PSB0YXJnZXQueCAmJiBjYW5kaWRhdGUueSA9PT0gdGFyZ2V0LnkgJiYgY2FuZGlkYXRlLnN0YXRlICE9PSAnZGVzdHJveWVkJyAmJiBbJ21pbmUuYnJva2VuQ2FydCcsICdjYXZlcm5zLmJyb2tlbkJvYXQnLCAncnVpbnMuY29sbGFwc2VkQXJjaCddLmluY2x1ZGVzKGNhbmRpZGF0ZS5raW5kKSlcbiAgY29uc3QgdGlsZSA9IGdldFRpbGUoc3RhdGUuZmxvb3IsIGRlc3RpbmF0aW9uLngsIGRlc3RpbmF0aW9uLnkpXG4gIGNvbnN0IHByb3RlY3RlZERlc3RpbmF0aW9uID0gZGVzdGluYXRpb24ueCA9PT0gc3RhdGUuZmxvb3Iuc3RhcnQueCAmJiBkZXN0aW5hdGlvbi55ID09PSBzdGF0ZS5mbG9vci5zdGFydC55IHx8IGRlc3RpbmF0aW9uLnggPT09IHN0YXRlLmZsb29yLmV4aXQueCAmJiBkZXN0aW5hdGlvbi55ID09PSBzdGF0ZS5mbG9vci5leGl0LnkgfHwgc3RhdGUuZmxvb3IuYWN0b3JzLnNvbWUoYWN0b3IgPT4gYWN0b3IuaGVhbHRoID4gMCAmJiBhY3Rvci54ID09PSBkZXN0aW5hdGlvbi54ICYmIGFjdG9yLnkgPT09IGRlc3RpbmF0aW9uLnkpIHx8IHN0YXRlLmZsb29yLm1pbGVzdG9uZXMuc29tZShtaWxlc3RvbmUgPT4gIW1pbGVzdG9uZS5jbGFpbWVkICYmIG1pbGVzdG9uZS54ID09PSBkZXN0aW5hdGlvbi54ICYmIG1pbGVzdG9uZS55ID09PSBkZXN0aW5hdGlvbi55KSB8fCBzdGF0ZS5mbG9vci5wcm9wcy5zb21lKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUuc3RhdGUgIT09ICdkZXN0cm95ZWQnICYmIGNhbmRpZGF0ZS54ID09PSBkZXN0aW5hdGlvbi54ICYmIGNhbmRpZGF0ZS55ID09PSBkZXN0aW5hdGlvbi55KVxuICBpZiAoIXByb3ApIHsgbG9nKHN0YXRlLCAnV29vZGVuIExldmVyIGFuZCBSb2xsZXIgbmVlZHMgYW4gYWRqYWNlbnQgbW92YWJsZSBwcm9wLicpOyByZXR1cm4gZmFsc2UgfVxuICBpZiAoIXRpbGUgfHwgdGlsZS5raW5kICE9PSAnZmxvb3InIHx8IHByb3RlY3RlZERlc3RpbmF0aW9uKSB7IGxvZyhzdGF0ZSwgJ1dvb2RlbiBMZXZlciBhbmQgUm9sbGVyIG5lZWRzIGFuIGVtcHR5IGxlZ2FsIGRlc3RpbmF0aW9uLicpOyByZXR1cm4gZmFsc2UgfVxuICBwcm9wLnggPSBkZXN0aW5hdGlvbi54XG4gIHByb3AueSA9IGRlc3RpbmF0aW9uLnlcbiAgbG9nKHN0YXRlLCAnVGhlIGxldmVyIHJvbGxzIHRoZSBwcm9wIGZvcndhcmQsIGNoYW5naW5nIHRoZSByb3V0ZS4nKVxuICByZXR1cm4gdHJ1ZVxufVxuXG5leHBvcnQgZnVuY3Rpb24gZXhwaXJlQXNod2F5cyhzdGF0ZTogUnVuU3RhdGUpOiB2b2lkIHtcbiAgY29uc3QgcGVuZGluZyA9IHN0YXRlLmZsb29yLnRyYW5zaWVudFRlcnJhaW4gPz8gW11cbiAgY29uc3QgYWN0aXZlID0gcGVuZGluZy5maWx0ZXIoY3VycmVudCA9PiB7XG4gICAgaWYgKGN1cnJlbnQuZXhwaXJlc0F0ID4gc3RhdGUudHVybiB8fCAoc3RhdGUuaGVyby54ID09PSBjdXJyZW50LnggJiYgc3RhdGUuaGVyby55ID09PSBjdXJyZW50LnkpKSByZXR1cm4gdHJ1ZVxuICAgIGNvbnN0IHRpbGUgPSBnZXRUaWxlKHN0YXRlLmZsb29yLCBjdXJyZW50LngsIGN1cnJlbnQueSlcbiAgICBpZiAodGlsZT8ua2luZCA9PT0gJ2Zsb29yJykgeyB0aWxlLmtpbmQgPSBjdXJyZW50Lm9yaWdpbmFsOyBpZiAoY3VycmVudC5vcmlnaW5hbEZsb3cpIHRpbGUuZmxvdyA9IHsgLi4uY3VycmVudC5vcmlnaW5hbEZsb3cgfSB9XG4gICAgcmV0dXJuIGZhbHNlXG4gIH0pXG4gIHN0YXRlLmZsb29yLnRyYW5zaWVudFRlcnJhaW4gPSBhY3RpdmUubGVuZ3RoID8gYWN0aXZlIDogdW5kZWZpbmVkXG59XG5cbmV4cG9ydCBmdW5jdGlvbiByZXZlYWxNaWxlc3RvbmVzKHN0YXRlOiBSdW5TdGF0ZSk6IHZvaWQge1xuICBmb3IgKGNvbnN0IGN1cnJlbnQgb2Ygc3RhdGUuZmxvb3IubWlsZXN0b25lcykgaWYgKCFjdXJyZW50LmNsYWltZWQpIGN1cnJlbnQuZGlzY292ZXJlZCA9IHRydWVcbn1cblxuZXhwb3J0IGZ1bmN0aW9uIHJldmVhbE5lYXJieU1pbGVzdG9uZXMoc3RhdGU6IFJ1blN0YXRlLCByYWRpdXM6IG51bWJlcik6IHZvaWQge1xuICBmb3IgKGNvbnN0IGN1cnJlbnQgb2Ygc3RhdGUuZmxvb3IubWlsZXN0b25lcykgaWYgKCFjdXJyZW50LmNsYWltZWQgJiYgTWF0aC5tYXgoTWF0aC5hYnMoY3VycmVudC54IC0gc3RhdGUuaGVyby54KSwgTWF0aC5hYnMoY3VycmVudC55IC0gc3RhdGUuaGVyby55KSkgPD0gcmFkaXVzKSBjdXJyZW50LmRpc2NvdmVyZWQgPSB0cnVlXG59XG5cbmV4cG9ydCBmdW5jdGlvbiB1c2VUaW1lS25vdChzdGF0ZTogUnVuU3RhdGUpOiBBY3Rpb25SZXN1bHQge1xuICBjb25zdCByYW5rID0gYm9vblJhbmsoc3RhdGUsICd0aW1lS25vdCcpXG4gIGNvbnN0IHBvc2l0aW9ucyA9IHN0YXRlLmhlcm8uc2FmZVBvc2l0aW9ucyA/PyBbXVxuICBpZiAoIXJhbmspIHsgbG9nKHN0YXRlLCAnTm8gVGltZSBLbm90IGlzIGJvdW5kLicpOyByZXR1cm4gW10gfVxuICBjb25zdCBjdXJyZW50ID0gYCR7c3RhdGUuaGVyby54fSwke3N0YXRlLmhlcm8ueX1gXG4gIGNvbnN0IHRhcmdldCA9IFsuLi5wb3NpdGlvbnNdLnJldmVyc2UoKS5maW5kKHBvaW50ID0+IGAke3BvaW50Lnh9LCR7cG9pbnQueX1gICE9PSBjdXJyZW50ICYmIGlzUGFzc2FibGUoc3RhdGUuZmxvb3IsIHBvaW50LngsIHBvaW50LnkpKVxuICBpZiAoIXRhcmdldCkgeyBsb2coc3RhdGUsICdUaW1lIEtub3QgaGFzIG5vIGVhcmxpZXIgc2FmZSBwb3NpdGlvbi4nKTsgcmV0dXJuIFtdIH1cbiAgc3RhdGUuaGVyby54ID0gdGFyZ2V0LnhcbiAgc3RhdGUuaGVyby55ID0gdGFyZ2V0LnlcbiAgY29uc3QgYmFzZSA9IHN0YXRlLmhlcm8uYm9vbnM/LnRpbWVLbm90ID8/IDBcbiAgaWYgKGJhc2UgPiAwKSB7XG4gICAgc3RhdGUuaGVyby5ib29ucyEudGltZUtub3QgPSBiYXNlIC0gMVxuICAgIGlmIChzdGF0ZS5oZXJvLmJvb25zIS50aW1lS25vdCA9PT0gMCkgZGVsZXRlIHN0YXRlLmhlcm8uYm9vbnMhLnRpbWVLbm90XG4gIH0gZWxzZSBpZiAoc3RhdGUuaGVyby5ib29uRXZvbHV0aW9ucz8udGltZUtub3QpIHN0YXRlLmhlcm8uYm9vbkV2b2x1dGlvbnMudGltZUtub3QtLVxuICByZWZyZXNoRm92KHN0YXRlKVxuICBsb2coc3RhdGUsICdUaW1lIEtub3QgcmV0dXJucyB5b3UgdG8gYW4gZWFybGllciBzYWZlIHBvc2l0aW9uLiBUaGUgd29ybGQgZG9lcyBub3QgcmV3aW5kLicpXG4gIHJldHVybiBbZXZlbnQoJ3NwZWxsJyldXG59XG5cbmV4cG9ydCBmdW5jdGlvbiByZWNvcmRTYWZlUG9zaXRpb24oc3RhdGU6IFJ1blN0YXRlKTogdm9pZCB7XG4gIGlmICghaXNQYXNzYWJsZShzdGF0ZS5mbG9vciwgc3RhdGUuaGVyby54LCBzdGF0ZS5oZXJvLnkpKSByZXR1cm5cbiAgY29uc3QgcG9zaXRpb25zID0gc3RhdGUuaGVyby5zYWZlUG9zaXRpb25zID8/PSBbXVxuICBjb25zdCBsYXN0ID0gcG9zaXRpb25zLmF0KC0xKVxuICBpZiAoIWxhc3QgfHwgbGFzdC54ICE9PSBzdGF0ZS5oZXJvLnggfHwgbGFzdC55ICE9PSBzdGF0ZS5oZXJvLnkpIHBvc2l0aW9ucy5wdXNoKHsgeDogc3RhdGUuaGVyby54LCB5OiBzdGF0ZS5oZXJvLnkgfSlcbiAgaWYgKHBvc2l0aW9ucy5sZW5ndGggPiAxMiArIGJvb25SYW5rKHN0YXRlLCAnYm9ycm93ZWRUaW1lJykpIHBvc2l0aW9ucy5zaGlmdCgpXG59XG4iXSwibWFwcGluZ3MiOiJBQUFBLFNBQVNBLFVBQVUsUUFBMkcsVUFBVTtBQUN4SSxTQUFTQyxNQUFNLFFBQVEsUUFBUTtBQUMvQixTQUFTQyxjQUFjLFFBQVEsb0JBQW9CO0FBQ25ELFNBQVNDLE9BQU8sRUFBRUMsVUFBVSxRQUFRLFVBQVU7QUFDOUMsU0FBU0MsT0FBTyxRQUFRLFVBQVU7QUFDbEMsU0FBU0MsS0FBSyxFQUFFQyxH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RSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxXQUFXO0VBQUVILElBQUksRUFBRTtBQUErQyxDQUFDLEVBQ2xJO0VBQUVKLEVBQUUsRUFBRSxhQUFhO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxXQUFXO0VBQUVILElBQUksRUFBRTtBQUFrRCxDQUFDLEVBQ3JJO0VBQUVKLEVBQUUsRUFBRSxhQUFhO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxXQUFXO0VBQUVILElBQUksRUFBRTtBQUE0QyxDQUFDLEVBQy9IO0VBQUVKLEVBQUUsRUFBRSxhQUFhO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxTQUFTO0VBQUVILElBQUksRUFBRTtBQUF1RCxDQUFDLEVBQ3hJO0VBQUVKLEVBQUUsRUFBRSxjQUFjO0VBQUVDLElBQUksRUFBRSxlQUFlO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxTQUFTO0VBQUVILElBQUksRUFBRTtBQUFzRCxDQUFDLEVBQ3pJO0VBQUVKLEVBQUUsRUFBRSxVQUFVO0VBQUVDLElBQUksRUFBRSxXQUFXO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxTQUFTO0VBQUVILElBQUksRUFBRTtBQUFnRSxDQUFDLEVBQzNJO0VBQUVKLEVBQUUsRUFBRSxhQUFhO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxTQUFTO0VBQUVILElBQUksRUFBRTtBQUFzRCxDQUFDLEVBQ3ZJO0VBQUVKLEVBQUUsRUFBRSxVQUFVO0VBQUVDLElBQUksRUFBRSxVQUFVO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxTQUFTO0VBQUVILElBQUksRUFBRTtBQUFzRSxDQUFDLEVBQ2hKO0VBQUVKLEVBQUUsRUFBRSxXQUFXO0VBQUVDLElBQUksRUFBRSxZQUFZO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxZQUFZO0VBQUVILElBQUksRUFBRTtBQUE0RCxDQUFDLEVBQzVJO0VBQUVKLEVBQUUsRUFBRSxXQUFXO0VBQUVDLElBQUksRUFBRSxZQUFZO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxZQUFZO0VBQUVILElBQUksRUFBRTtBQUErQyxDQUFDLEVBQy9IO0VBQUVKLEVBQUUsRUFBRSxhQUFhO0VBQUVDLElBQUksRUFBRSxjQUFjO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxZQUFZO0VBQUVILElBQUksRUFBRTtBQUE4QyxDQUFDLEVBQ2xJO0VBQUVKLEVBQUUsRUFBRSxjQUFjO0VBQUVDLElBQUksRUFBRSxlQUFlO0VBQUVDLEtBQUssRUFBRSxHQUFHO0VBQUVLLE1BQU0sRUFBRSxVQUFVO0VBQUVILElBQUksRUFBRTtBQUEwRCxDQUFDLEVBQzlJO0VBQUVKLEVBQUUsRUFBRSxnQkFBZ0I7RUFBRUMsSUFBSSxFQUFFLGlCQUFpQjtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsVUFBVTtFQUFFSCxJQUFJLEVBQUU7QUFBNEQsQ0FBQyxFQUNwSjtFQUFFSixFQUFFLEVBQUUsVUFBVTtFQUFFQyxJQUFJLEVBQUUsV0FBVztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsVUFBVTtFQUFFSCxJQUFJLEVBQUU7QUFBb0QsQ0FBQyxFQUNoSTtFQUFFSixFQUFFLEVBQUUsVUFBVTtFQUFFQyxJQUFJLEVBQUUsV0FBVztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsVUFBVTtFQUFFSCxJQUFJLEVBQUU7QUFBdUQsQ0FBQyxFQUNuSTtFQUFFSixFQUFFLEVBQUUsY0FBYztFQUFFQyxJQUFJLEVBQUUsZUFBZTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsVUFBVTtFQUFFSCxJQUFJLEVBQUU7QUFBd0QsQ0FBQyxFQUM1STtFQUFFSixFQUFFLEVBQUUsY0FBYztFQUFFQyxJQUFJLEVBQUUsZUFBZTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsUUFBUTtFQUFFSCxJQUFJLEVBQUU7QUFBOEMsQ0FBQyxFQUNoSTtFQUFFSixFQUFFLEVBQUUsVUFBVTtFQUFFQyxJQUFJLEVBQUUsV0FBVztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsUUFBUTtFQUFFSCxJQUFJLEVBQUU7QUFBeUQsQ0FBQyxFQUNuSTtFQUFFSixFQUFFLEVBQUUsYUFBYTtFQUFFQyxJQUFJLEVBQUUsY0FBYztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsUUFBUTtFQUFFSCxJQUFJLEVBQUU7QUFBdUQsQ0FBQyxFQUN2STtFQUFFSixFQUFFLEVBQUUsVUFBVTtFQUFFQyxJQUFJLEVBQUUsV0FBVztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsUUFBUTtFQUFFSCxJQUFJLEVBQUU7QUFBMkQsQ0FBQyxFQUNySTtFQUFFSixFQUFFLEVBQUUsWUFBWTtFQUFFQyxJQUFJLEVBQUUsYUFBYTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsUUFBUTtFQUFFSCxJQUFJLEVBQUU7QUFBeUQsQ0FBQyxFQUN2STtFQUFFSixFQUFFLEVBQUUsV0FBVztFQUFFQyxJQUFJLEVBQUUsWUFBWTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsU0FBUztFQUFFSCxJQUFJLEVBQUU7QUFBeUQsQ0FBQyxFQUN0STtFQUFFSixFQUFFLEVBQUUsYUFBYTtFQUFFQyxJQUFJLEVBQUUsY0FBYztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsWUFBWTtFQUFFSCxJQUFJLEVBQUU7QUFBc0QsQ0FBQyxFQUMxSTtFQUFFSixFQUFFLEVBQUUsV0FBVztFQUFFQyxJQUFJLEVBQUUsWUFBWTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsWUFBWTtFQUFFSCxJQUFJLEVBQUU7QUFBeUMsQ0FBQyxFQUN6SDtFQUFFSixFQUFFLEVBQUUsYUFBYTtFQUFFQyxJQUFJLEVBQUUsY0FBYztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsWUFBWTtFQUFFSCxJQUFJLEVBQUU7QUFBdUQsQ0FBQyxFQUMzSTtFQUFFSixFQUFFLEVBQUUsYUFBYTtFQUFFQyxJQUFJLEVBQUUsY0FBYztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsU0FBUztFQUFFSCxJQUFJLEVBQUU7QUFBb0QsQ0FBQyxFQUNySTtFQUFFSixFQUFFLEVBQUUsYUFBYTtFQUFFQyxJQUFJLEVBQUUsY0FBYztFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsV0FBVztFQUFFSCxJQUFJLEVBQUU7QUFBMEQsQ0FBQyxFQUM3STtFQUFFSixFQUFFLEVBQUUsWUFBWTtFQUFFQyxJQUFJLEVBQUUsYUFBYTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsUUFBUTtFQUFFSCxJQUFJLEVBQUU7QUFBb0UsQ0FBQyxFQUNsSjtFQUFFSixFQUFFLEVBQUUsY0FBYztFQUFFQyxJQUFJLEVBQUUsZUFBZTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsVUFBVTtFQUFFSCxJQUFJLEVBQUU7QUFBa0QsQ0FBQyxFQUN0STtFQUFFSixFQUFFLEVBQUUsV0FBVztFQUFFQyxJQUFJLEVBQUUsWUFBWTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsV0FBVztFQUFFSCxJQUFJLEVBQUU7QUFBcUUsQ0FBQyxFQUNwSjtFQUFFSixFQUFFLEVBQUUsY0FBYztFQUFFQyxJQUFJLEVBQUUsZUFBZTtFQUFFQyxLQUFLLEVBQUUsR0FBRztFQUFFSyxNQUFNLEVBQUUsVUFBVTtFQUFFSCxJQUFJLEVBQUU7QUFBcUQsQ0FBQyxFQUN6STtFQUFFSixFQUFFLEVBQUUsY0FBYztFQUFF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