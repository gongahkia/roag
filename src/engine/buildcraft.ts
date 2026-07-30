import { DIRECTIONS, type Alignment, type Biome, type BoonId, type FloorMilestone, type RunState, type TraversalToolId } from '../types'
import { rngFor } from '../rng'
import { rewardOfferFor } from '../reward-contract'
import { getTile, isPassable } from '../world'
import { advance } from './combat'
import { event, log, type ActionResult } from './shared'
import { refreshFov } from './visibility'
import { recordTelemetryCount } from '../telemetry'
import { armRelicTraversal, consumeRelicTool, relicAlignment, relicChoices, relicFor } from './relics'
import { tend } from './alignment'

export interface TraversalTool { id: TraversalToolId; name: string; glyph: string; cooldown: number; text: string; overdrive: string }
export type BoonFamily = 'traversal' | 'combat' | 'recovery' | 'scouting' | 'spellcraft' | 'economy' | 'terrain' | 'consumable'
export interface Boon { id: BoonId; name: string; glyph: string; text: string; family: BoonFamily; biomes?: readonly Biome[]; suppresses?: readonly BoonFamily[]; rare?: boolean }

export const TOOLS: readonly TraversalTool[] = [
  { id: 'stoneWedge', name: 'Stone Wedge', glyph: 'W', cooldown: 6, text: 'Breach one adjacent blocker.', overdrive: 'Breach a three-tile wedge, then retire.' },
  { id: 'reedwing', name: 'Reedwing', glyph: '^', cooldown: 5, text: 'Cross one hazardous tile to a clear landing.', overdrive: 'Cross up to two hazards, then retire.' },
  { id: 'cordAnchor', name: 'Cord Anchor', glyph: '⌁', cooldown: 5, text: 'Pull two tiles to a clear landing.', overdrive: 'Pull four tiles, then retire.' },
  { id: 'ashwayRites', name: 'Ashway Rites', glyph: '≈', cooldown: 8, text: 'Make a two-tile temporary safe path.', overdrive: 'Make a three-tile path, then retire.' },
  { id: 'antlerPrybar', name: 'Antler Prybar', glyph: '⌐', cooldown: 4, text: 'Shift one adjacent boulder or breakwall forward.', overdrive: 'Shift once with a louder exposed footing risk, then retire.' },
  { id: 'stoneAdze', name: 'Stone Adze', glyph: '⌟', cooldown: 5, text: 'Cut one adjacent crate or crumble route.', overdrive: 'Cut once, then retire.' },
  { id: 'resinFireBasket', name: 'Resin Fire Basket', glyph: '♨', cooldown: 6, text: 'Burn adjacent bramble or web into smoke.', overdrive: 'Burn once with stronger smoke, then retire.' },
  { id: 'woodenLeverRoller', name: 'Wooden Lever and Roller', glyph: '↔', cooldown: 5, text: 'Push one adjacent movable prop forward.', overdrive: 'Push once, then retire.' }
]

export const BOONS: readonly Boon[] = [
  { id: 'scoutEye', name: 'Scout Eye', glyph: '◉', family: 'scouting', text: 'Reveal all remaining milestone directions.' },
  { id: 'timeKnot', name: 'Time Knot', glyph: '⟲', family: 'recovery', text: 'Gain one position-only safe-step rewind.' },
  { id: 'coolAsh', name: 'Cool Ash', glyph: '•', family: 'traversal', text: 'Tools recover 1 turn faster per stack.' },
  { id: 'wayfinderCord', name: 'Wayfinder Cord', glyph: '⌇', family: 'traversal', text: 'Tool use reveals nearby terrain.' },
  { id: 'rootedResolve', name: 'Rooted Resolve', glyph: '✦', family: 'recovery', text: 'Tool use restores 1 HP per stack.' },
  { id: 'trailRations', name: 'Trail Rations', glyph: '+', family: 'recovery', text: 'Each milestone restores 2 HP per stack.' },
  { id: 'emberFletching', name: 'Ember Fletching', glyph: '*', family: 'combat', text: 'Thrown attacks deal +1 damage per stack.' },
  { id: 'cordTempo', name: 'Cord Tempo', glyph: '/', family: 'combat', text: 'Tool use restores 1 focus per stack.' },
  { id: 'watchfulStep', name: 'Watchful Step', glyph: '!', family: 'scouting', text: 'Newly discovered milestones reveal nearby threats.' },
  { id: 'mapMoss', name: 'Map Moss', glyph: '·', family: 'scouting', text: 'Exploration reveals 1 extra tile radius per stack.' },
  { id: 'quietTide', name: 'Quiet Tide', glyph: '~', family: 'spellcraft', text: 'Charms cost 1 less focus per stack.' },
  { id: 'spiritKindling', name: 'Spirit Kindling', glyph: '?', family: 'spellcraft', text: 'Milestones restore 1 focus per stack.' },
  { id: 'cacheSense', name: 'Cache Sense', glyph: '$', family: 'economy', text: 'Each milestone yields 8 cash per stack.' },
  { id: 'barterThread', name: 'Barter Thread', glyph: '¤', family: 'economy', text: 'Containers yield +10 cash per stack.' },
  { id: 'stoneMemory', name: 'Stone Memory', glyph: '#', family: 'traversal', text: 'Stone Wedge also clears nearby rubble per stack.' },
  { id: 'reedMemory', name: 'Reed Memory', glyph: '≋', family: 'traversal', text: 'Reedwing crosses one extra hazard per stack.' },
  { id: 'lastLight', name: 'Last Light', glyph: 'i', family: 'recovery', text: 'At 25% HP, tool use restores 2 HP per stack.' },
  { id: 'parcelMark', name: 'Parcel Mark', glyph: '□', family: 'economy', text: 'Unclaimed milestones show distance after one stack.' }
  ,{ id: 'breachTempo', name: 'Breach Tempo', glyph: '↯', family: 'traversal', text: 'Breaking terrain restores 1 focus per stack.' }
  ,{ id: 'updraftStep', name: 'Updraft Step', glyph: '↑', family: 'traversal', text: 'Lifts and glides travel 1 extra tile per stack.' }
  ,{ id: 'anchorHabit', name: 'Anchor Habit', glyph: '⚓', family: 'traversal', text: 'Ropes and anchors restore 1 HP per stack.' }
  ,{ id: 'smokeWalker', name: 'Smoke Walker', glyph: '≈', family: 'terrain', text: 'Smoke deals 1 less damage and reveals nearby ground.' }
  ,{ id: 'currentSense', name: 'Current Sense', glyph: '≋', family: 'terrain', text: 'Current crossings reveal a route and grant 1 focus.' }
  ,{ id: 'wallSong', name: 'Wall Song', glyph: '♫', family: 'terrain', text: 'Breach tools clear one additional adjacent blocker per stack.' }
  ,{ id: 'ashDividend', name: 'Ash Dividend', glyph: '¤', family: 'economy', text: 'Destroyed terrain and props yield 4 cash per stack.' }
  ,{ id: 'salvager', name: 'Salvager', glyph: '⛏', family: 'economy', text: 'First used consumable each floor has a 25% return chance per stack.' }
  ,{ id: 'lastMatch', name: 'Last Match', glyph: '†', family: 'consumable', text: 'At 25% HP, bombs and fire items gain +2 damage per stack.' }
  ,{ id: 'spareFuse', name: 'Spare Fuse', glyph: '!', family: 'consumable', text: 'Bomb packs restore one extra bomb per stack.' }
  ,{ id: 'quietPocket', name: 'Quiet Pocket', glyph: '◌', family: 'consumable', text: 'Tonics also remove one condition per stack.' }
  ,{ id: 'scavengerMap', name: 'Scavenger Map', glyph: '⌖', family: 'scouting', text: 'Containers reveal a nearby item or milestone per stack.' }
  ,{ id: 'dangerInstinct', name: 'Danger Instinct', glyph: '!', family: 'scouting', text: 'Entering a new hazard reveals adjacent enemies per stack.' }
  ,{ id: 'slagSkin', name: 'Slag Skin', glyph: '◒', family: 'recovery', text: 'Fire and smoke damage are reduced by 1 per stack.' }
  ,{ id: 'tideSkin', name: 'Tide Skin', glyph: '◓', family: 'recovery', text: 'Water and current damage are reduced by 1 per stack.' }
  ,{ id: 'pressureSeal', name: 'Pressure Seal', glyph: '◈', family: 'recovery', text: 'Gain 1 shield after using a traversal tool per stack.' }
  ,{ id: 'bloodCompass', name: 'Blood Compass', glyph: '✥', family: 'combat', text: 'Killing an enemy reveals 2 tiles per stack.' }
  ,{ id: 'hookLine', name: 'Hook Line', glyph: '⌇', family: 'combat', text: 'Reach weapons deal +1 damage per stack after movement.' }
  ,{ id: 'guardRattle', name: 'Guard Rattle', glyph: ')', family: 'combat', text: 'Off-hand gear grants +1 guard after a hit per stack.' }
  ,{ id: 'tideEdge', name: 'Tide Edge', glyph: '/', family: 'combat', text: 'Water/current weapon hits pull targets 1 tile per stack.' }
  ,{ id: 'cinderEdge', name: 'Cinder Edge', glyph: '/', family: 'combat', text: 'Fire/hammer weapon hits ignite gas or smoke per stack.' }
  ,{ id: 'echoCache', name: 'Echo Cache', glyph: '$', family: 'economy', text: 'Each distinct Boon family grants 5 cash at milestones.' }
  ,{ id: 'openCircuit', name: 'Open Circuit', glyph: '⌁', family: 'spellcraft', text: 'Casting after traversal restores 1 focus per stack.' }
  ,{ id: 'blinkDebt', name: 'Blink Debt', glyph: '?', family: 'spellcraft', text: 'Blink and pull gain 1 range per stack.' }
  ,{ id: 'rootBattery', name: 'Root Battery', glyph: '♣', family: 'spellcraft', text: 'Root/ward effects grant 1 shield or focus per stack.' }
  ,{ id: 'scrapPrayer', name: 'Scrap Prayer', glyph: '☼', family: 'terrain', text: 'Activated props restore 1 HP and focus per stack.' }
  ,{ id: 'softLanding', name: 'Soft Landing', glyph: '∨', family: 'traversal', text: 'Hazard crossings reduce incoming damage by 1 per stack.' }
  ,{ id: 'hardLesson', name: 'Hard Lesson', glyph: '∆', family: 'combat', text: 'Taking hazard damage gives +1 melee damage next attack per stack.' }
  ,{ id: 'sealedBreath', name: 'Sealed Breath', glyph: '◍', family: 'recovery', text: 'Smoke and gas no longer reduce sight per stack.' }
  ,{ id: 'relayStep', name: 'Relay Step', glyph: '›', family: 'traversal', text: 'Alternating move and tool use lowers tool cooldown by 1 per stack.' }
  ,{ id: 'borrowedTime', name: 'Borrowed Time', glyph: '⌛', family: 'recovery', text: 'Time Knot gains one extra safe position per stack.' }
  ,{ id: 'furnaceHeart', name: 'Furnace Heart', glyph: '♥', family: 'terrain', text: 'Fire actions grant +1 damage and +1 focus per stack.', rare: true }
  ,{ id: 'drownedOath', name: 'Drowned Oath', glyph: '♆', family: 'terrain', text: 'Water/current actions heal 1 HP and pull enemies per stack.', rare: true }
  ,{ id: 'blackLedger', name: 'Black Ledger', glyph: '§', family: 'economy', text: 'Gain 12 cash whenever you claim a milestone per stack.', rare: true }
  ,{ id: 'glassNerve', name: 'Glass Nerve', glyph: '◇', family: 'combat', text: 'Your first attack each turn gains +3 damage per stack.', rare: true }
  ,{ id: 'wayEater', name: 'Way Eater', glyph: '⌘', family: 'traversal', text: 'Clearing terrain permanently lowers all tool cooldowns by 1 per stack.', rare: true }
  ,{ id: 'deepPockets', name: 'Deep Pockets', glyph: '▣', family: 'consumable', text: 'Used items have a 35% chance per stack to return after combat.', rare: true },
  { id: 'galeThread', name: 'Gale Thread', glyph: '≈', family: 'traversal', text: 'Each climb restores 1 focus per stack.' },
  { id: 'ropewright', name: 'Ropewright', glyph: '⌁', family: 'traversal', text: 'At rank 2+, every second vertical rope costs no reserve rope.' },
  { id: 'updraftCadence', name: 'Updraft Cadence', glyph: '↑', family: 'traversal', text: 'Ledge movement grants 1 focus per stack.' },
  { id: 'skyhookReprisal', name: 'Skyhook Reprisal', glyph: 'J', family: 'combat', text: 'After climbing, your next attack gains +2 damage per stack.' },
  { id: 'thunderVessel', name: 'Thunder Vessel', glyph: 'ϟ', family: 'combat', text: 'Thrown damage gains +1 and marks targets per stack.' },
  { id: 'eyrieHoard', name: 'Eyrie Hoard', glyph: '$', family: 'economy', text: 'Caches and chests yield 12 cash per stack.' },
  { id: 'windScribe', name: 'Wind Scribe', glyph: '⌇', family: 'spellcraft', text: 'Wind and force charms gain 1 range per stack.' },
  { id: 'highPath', name: 'High Path', glyph: '⌖', family: 'scouting', text: 'Climbs reveal 2 tiles per stack around the destination.' },
  { id: 'graveLedger', name: 'Grave Ledger', glyph: '§', family: 'economy', text: 'Enemy kills grant 3 cash per stack.' },
  { id: 'ancestorLantern', name: 'Ancestor Lantern', glyph: 'i', family: 'recovery', text: 'Enemy kills restore 1 focus per stack.' },
  { id: 'boneOrchard', name: 'Bone Orchard', glyph: '✦', family: 'recovery', text: 'Enemy kills restore 1 HP per stack.' },
  { id: 'cairnPact', name: 'Cairn Pact', glyph: '▲', family: 'terrain', text: 'Oath and body payments grant 18 cash per stack.', suppresses: ['consumable'], rare: true },
  { id: 'mournersBell', name: 'Mourner’s Bell', glyph: 'o', family: 'spellcraft', text: 'Spirit events restore 1 focus per stack.' },
  { id: 'ossuaryWard', name: 'Ossuary Ward', glyph: '□', family: 'recovery', text: 'At each encounter resolution, gain 1 shield per stack.' },
  { id: 'funeralExchange', name: 'Funeral Exchange', glyph: '¤', family: 'economy', text: 'Discarded items yield 10 cash per stack.' },
  { id: 'lastRites', name: 'Last Rites', glyph: '†', family: 'combat', text: 'At 25% HP, attacks gain +2 damage per stack.' },
  { id: 'stormwake', name: 'Stormwake Engine', glyph: 'ϟ', family: 'combat', text: 'Traversal actions charge +1 attack damage per stack; suppresses recovery triggers.', suppresses: ['recovery'], rare: true },
  { id: 'burialCurrent', name: 'Burial Current', glyph: '≈', family: 'terrain', text: 'Current, spirit, and grave terrain restore 1 HP per stack.' },
  { id: 'echoDividend', name: 'Echo Dividend', glyph: '$', family: 'economy', text: 'Every resolved encounter grants 10 cash per stack.' },
  { id: 'tetheredThunder', name: 'Tethered Thunder', glyph: '⌁', family: 'combat', text: 'Rope and climb actions arm +1 thrown damage per stack.' },
  { id: 'riftLedger', name: 'Rift Ledger', glyph: '§', family: 'economy', text: 'Elite kills grant 15 extra cash per stack.' },
  { id: 'saltedAncestor', name: 'Salted Ancestor', glyph: 'i', family: 'spellcraft', text: 'A charm cast after a kill refunds 1 focus per stack.' },
  { id: 'windfall', name: 'Windfall', glyph: '↑', family: 'economy', text: 'Each route transition grants 20 cash per stack.' },
  { id: 'gravewind', name: 'Gravewind', glyph: '◌', family: 'terrain', text: 'Marked enemies take +1 damage per stack.' },
  { id: 'cliffsideCairn', name: 'Cliffside Cairn', glyph: '▲', family: 'scouting', text: 'Revealed milestones restore 1 focus per stack.' },
  { id: 'spiritSail', name: 'Spirit Sail', glyph: '⌇', family: 'traversal', text: 'Each climb or current crossing gains +1 movement range per stack.' },
  { id: 'descentEngine', name: 'Descent Engine', glyph: '↯', family: 'combat', text: 'Damage after a terrain crossing gains +1 per stack.' },
  { id: 'altarCompound', name: 'Altar Compound', glyph: '+', family: 'spellcraft', text: 'Altar and oath rewards add 1 boon evolution rank per stack at 3+.' },
  { id: 'stormRations', name: 'Storm Rations', glyph: '+', family: 'consumable', text: 'Using a tonic after moving restores 2 extra HP per stack.' },
  { id: 'heirloomCircuit', name: 'Heirloom Circuit', glyph: '◇', family: 'recovery', text: 'Guardian kills restore 4 HP and focus per stack.' },
  { id: 'bridgeOfNames', name: 'Bridge of Names', glyph: '=', family: 'traversal', text: 'Each active oath grants 1 armor per stack.' },
  { id: 'cursedInvestment', name: 'Cursed Investment', glyph: '☠', family: 'economy', text: 'While cursed, all cash gains are doubled per stack.', suppresses: ['consumable'], rare: true },
  { id: 'sunstep', name: 'Sunstep', glyph: '☼', family: 'traversal', biomes: ['saltFlats'], text: 'Salt mirrors restore 1 focus per stack and arm a Prism Relay.' },
  { id: 'brineWard', name: 'Brine Ward', glyph: '≈', family: 'recovery', biomes: ['saltFlats'], text: 'Brine damage is reduced by 1 per stack.' },
  { id: 'mirrorHunt', name: 'Mirror Hunt', glyph: '◇', family: 'combat', biomes: ['saltFlats'], text: 'Attacks from salt mirrors gain +1 damage per stack.' },
  { id: 'glassEdge', name: 'Glass Edge', glyph: '◈', family: 'combat', biomes: ['saltFlats'], text: 'Marked targets take +1 damage per stack.' },
  { id: 'saltLedger', name: 'Salt Ledger', glyph: '§', family: 'economy', biomes: ['saltFlats'], text: 'Milestones grant 6 extra cash per stack.' },
  { id: 'duneRation', name: 'Dune Ration', glyph: '+', family: 'recovery', biomes: ['saltFlats'], text: 'Milestones restore 1 extra HP per stack.' },
  { id: 'mirageMap', name: 'Mirage Map', glyph: '⌖', family: 'scouting', biomes: ['saltFlats'], text: 'Salt mirrors reveal 1 nearby unexplored tile per stack.' },
  { id: 'whiteRoad', name: 'White Road', glyph: '›', family: 'terrain', biomes: ['saltFlats'], text: 'Brine crossings grant 1 shield per stack.' },
  { id: 'sunsetCircuit', name: 'Sunset Circuit', glyph: 'ϟ', family: 'spellcraft', biomes: ['saltFlats'], text: 'Casting after a salt mirror restores 1 focus per stack.' },
  { id: 'heatDebt', name: 'Heat Debt', glyph: '☠', family: 'economy', biomes: ['saltFlats'], text: 'Gain 14 cash per hostile kill per stack; suppresses recovery triggers.', suppresses: ['recovery'], rare: true },
  { id: 'coldRead', name: 'Cold Read', glyph: '❄', family: 'scouting', biomes: ['frostReliquary'], text: 'Ice crossings restore 1 focus and reveal nearby threats per stack.' },
  { id: 'rimeGuard', name: 'Rime Guard', glyph: '□', family: 'recovery', biomes: ['frostReliquary'], text: 'Ice crossings grant 1 shield per stack.' },
  { id: 'duelistOath', name: 'Duelist Oath', glyph: '⚔', family: 'combat', biomes: ['frostReliquary'], text: 'Elite and guardian targets take +2 damage per stack.' },
  { id: 'shatterMark', name: 'Shatter Mark', glyph: '✦', family: 'combat', biomes: ['frostReliquary'], text: 'Slowed or marked targets take +1 damage per stack.' },
  { id: 'winterRations', name: 'Winter Rations', glyph: '+', family: 'recovery', biomes: ['frostReliquary'], text: 'Milestones restore 1 HP and focus per stack.' },
  { id: 'iceLedger', name: 'Ice Ledger', glyph: '§', family: 'economy', biomes: ['frostReliquary'], text: 'Elite kills grant 10 extra cash per stack.' },
  { id: 'frozenFocus', name: 'Frozen Focus', glyph: '◌', family: 'spellcraft', biomes: ['frostReliquary'], text: 'A charm cast while shielded costs 1 less focus per stack.' },
  { id: 'thawStep', name: 'Thaw Step', glyph: '∨', family: 'terrain', biomes: ['frostReliquary'], text: 'Frost rime damage is reduced by 1 per stack.' },
  { id: 'reliquaryEcho', name: 'Reliquary Echo', glyph: 'o', family: 'terrain', biomes: ['frostReliquary'], text: 'Resolving an encounter grants 1 shield per stack.' },
  { id: 'lastWinter', name: 'Last Winter', glyph: '†', family: 'combat', biomes: ['frostReliquary'], text: 'At 25% HP, attacks gain +3 damage per stack; suppresses consumable recovery.', suppresses: ['consumable'], rare: true }
]

const toolById = Object.fromEntries(TOOLS.map(tool => [tool.id, tool])) as Record<TraversalToolId, TraversalTool>
const boonById = Object.fromEntries(BOONS.map(boon => [boon.id, boon])) as Record<string, Boon>
const KAMI_BOONS = new Set<BoonId>(['timeKnot', 'rootedResolve', 'watchfulStep', 'mapMoss', 'quietTide', 'spiritKindling', 'lastLight', 'wallSong', 'wardMemory', 'echoDividend', 'sunstep', 'mirageMap', 'whiteRoad', 'sunsetCircuit', 'coldRead', 'rimeGuard', 'duelistOath', 'shatterMark', 'frozenFocus', 'reliquaryEcho', 'lastWinter'])
export const boonAlignment = (id: BoonId): Alignment => KAMI_BOONS.has(id) ? 'kami' : 'villagePact'
const drillable = new Set(['wall', 'rubble', 'bramble', 'boulder'])
const hazardous = new Set(['pit', 'water', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder', 'bramble', 'rubble', 'brine', 'frostRime'])

export const toolFor = (id: TraversalToolId): TraversalTool => toolById[id]
export const boonFor = (id: BoonId): Boon => boonById[id]
export const boonRank = (state: RunState, id: BoonId): number => {
  const boon = boonById[id]
  if (!boon) return 0
  const suppressed = Object.keys(state.hero.boons ?? {}).some(ownerId => {
    const owner = boonById[ownerId]
    return ownerId !== id && (state.hero.boons?.[ownerId] ?? 0) > 0 && owner?.suppresses?.includes(boon.family)
  })
  return suppressed ? 0 : (state.hero.boons?.[id] ?? 0) + (state.hero.boonEvolutions?.[id] ?? 0)
}
export const hasBoon = (state: RunState, id: BoonId): boolean => boonRank(state, id) > 0
export const toolCooldown = (state: RunState, id: TraversalToolId): number => state.hero.cooldowns?.[`tool:${id}`] ?? 0

export const toolChoices = (state: RunState, milestone: FloorMilestone): TraversalTool[] => {
  const offer = rewardOfferFor(state.floor, milestone.rewardKey)
  if (offer?.kind === 'waycache') {
    const choices = offer.choices.map(choice => toolById[choice.id]).filter((choice): choice is TraversalTool => Boolean(choice))
    if (choices.length === 3 && new Set(choices.map(choice => choice.id)).size === 3) return choices
  }
  return rngFor(state.seed, 'progression', state.floor.index, milestone.id, 'tools').shuffle([...TOOLS]).slice(0, 3)
}
export const boonChoices = (state: RunState, milestone: FloorMilestone): Boon[] => {
  const offer = rewardOfferFor(state.floor, milestone.rewardKey)
  if (offer?.kind === 'boon') {
    const choices = offer.choices.map(choice => boonById[choice.id]).filter((choice): choice is Boon => Boolean(choice))
    if (choices.length === 3 && new Set(choices.map(choice => choice.id)).size === 3) return choices
  }
  const owned = new Set(Object.keys(state.hero.boons ?? {}))
  const families = new Set([...owned].map(id => boonById[id]?.family).filter(Boolean))
  const shuffled = rngFor(state.seed, 'progression', state.floor.index, milestone.id, 'boons').shuffle([...BOONS])
  const ranked = shuffled.sort((a, b) => Number(owned.has(b.id)) - Number(owned.has(a.id)) || Number(families.has(b.family)) - Number(families.has(a.family)))
  const local = ranked.filter(boon => boon.biomes?.includes(state.floor.biome))
  const global = ranked.filter(boon => !boon.biomes?.includes(state.floor.biome))
  return [...local.slice(0, 2), ...global].slice(0, 3)
}

const milestoneAtReach = (state: RunState): FloorMilestone | undefined => state.floor.milestones.find(milestone => !milestone.claimed && (milestone.kind !== 'augment' || Object.values(state.hero.boons ?? {}).some(rank => (rank ?? 0) > 0)) && Math.max(Math.abs(milestone.x - state.hero.x), Math.abs(milestone.y - state.hero.y)) <= 1)

export function openMilestone(state: RunState): ActionResult | undefined {
  const milestone = milestoneAtReach(state)
  if (!milestone) return undefined
  milestone.discovered = true
  state.modal = milestone.kind === 'waycache' ? { kind: 'tool', milestoneId: milestone.id } : milestone.kind === 'augment' ? { kind: 'augment', milestoneId: milestone.id } : milestone.kind === 'relic' ? { kind: 'relic', milestoneId: milestone.id } : { kind: 'boon', milestoneId: milestone.id }
  log(state, milestone.kind === 'waycache' ? 'Waycache found: choose a ritual tool.' : milestone.kind === 'augment' ? 'Build-up moment: evolve, reforge, or transmute a Boon.' : milestone.kind === 'relic' ? 'Guardian echo found: bind one active relic.' : 'Boon site found: choose one mark.')
  return [event('menu')]
}

const milestone = (state: RunState, id: string): FloorMilestone | undefined => state.floor.milestones.find(current => current.id === id && !current.claimed)
const claim = (state: RunState, current: FloorMilestone): void => {
  current.claimed = true
  const health = boonRank(state, 'trailRations') * 2 + boonRank(state, 'duneRation') + boonRank(state, 'winterRations')
  const focus = boonRank(state, 'spiritKindling') + boonRank(state, 'winterRations')
  const cash = boonRank(state, 'cacheSense') * 8
    + boonRank(state, 'blackLedger') * 12
    + boonRank(state, 'saltLedger') * 6
    + boonRank(state, 'echoCache') * new Set(Object.keys(state.hero.boons ?? {}).map(id => boonById[id]?.family).filter(Boolean)).size * 5
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + health)
  state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + focus)
  state.hero.gold += cash
}

export function chooseBoon(state: RunState, milestoneId: string, command: string): boolean {
  const current = milestone(state, milestoneId)
  if (!current) return false
  const choice = boonChoices(state, current)[Number(command) - 1]
  if (!choice) return false
  state.hero.boons ??= {}
  state.hero.boons[choice.id] = (state.hero.boons[choice.id] ?? 0) + 1
  recordTelemetryCount(state, 'boonPicks', choice.id)
  claim(state, current)
  state.modal = undefined
  log(state, `Boon: ${choice.name} · rank ${boonRank(state, choice.id)}.`)
  tend(state, boonAlignment(choice.id))
  if (hasBoon(state, 'scoutEye')) revealMilestones(state)
  return true
}

const ownedBoonIds = (state: RunState): BoonId[] => Object.keys(state.hero.boons ?? {}).filter(id => (state.hero.boons?.[id] ?? 0) > 0 && boonById[id]).sort()
const rareChoices = (state: RunState, current: FloorMilestone, selected: BoonId): Boon[] => rngFor(state.seed, 'progression', state.floor.index, current.id, 'transmute', selected).shuffle(BOONS.filter(boon => boon.rare)).slice(0, 3)
const reforgeChoices = (state: RunState, current: FloorMilestone, selected: BoonId): Boon[] => {
  const source = boonFor(selected)
  const candidates = BOONS.filter(boon => boon.id !== selected && (boon.family === source.family || boon.rare))
  return rngFor(state.seed, 'progression', state.floor.index, current.id, 'reforge', selected).shuffle(candidates).slice(0, 3)
}

export function augmentChoices(state: RunState, milestoneId: string, mode: 'evolve' | 'reforge' | 'transmute'): Boon[] {
  const current = milestone(state, milestoneId)
  if (!current) return []
  const selected = state.modal?.kind === 'augment' ? state.modal.selected?.[0] : undefined
  if (!selected) return ownedBoonIds(state).map(boonFor)
  if (mode === 'reforge') return reforgeChoices(state, current, selected)
  if (mode === 'transmute') return rareChoices(state, current, selected)
  return []
}

export function chooseAugment(state: RunState, milestoneId: string, command: string): boolean {
  const current = milestone(state, milestoneId)
  const modal = state.modal?.kind === 'augment' ? state.modal : undefined
  if (!current || !modal) return false
  const numeric = Number(command) - 1
  if (!modal.mode) {
    const mode = (['evolve', 'reforge', 'transmute'] as const)[numeric]
    if (!mode) return false
    state.modal = { ...modal, mode }
    log(state, `${mode[0].toUpperCase()}${mode.slice(1)}: choose an owned Boon.`)
    return true
  }
  const selected = modal.selected?.[0]
  if (!selected) {
    const owned = ownedBoonIds(state)
    const choice = owned[numeric]
    if (!choice) return false
    if (modal.mode === 'evolve') {
      state.hero.boonEvolutions ??= {}
      state.hero.boonEvolutions[choice] = (state.hero.boonEvolutions[choice] ?? 0) + 1
      recordTelemetryCount(state, 'boonAugments', `evolve:${choice}`)
      claim(state, current)
      state.modal = undefined
      log(state, `${boonFor(choice).name} evolves to tier ${state.hero.boonEvolutions[choice]}. Its engine strengthens.`)
      return true
    }
    state.modal = { ...modal, selected: [choice] }
    log(state, `Choose a ${modal.mode} result for ${boonFor(choice).name}.`)
    return true
  }
  const choices = modal.mode === 'reforge' ? reforgeChoices(state, current, selected) : rareChoices(state, current, selected)
  const choice = choices[numeric]
  if (!choice) return false
  const prior = state.hero.boons?.[selected] ?? 0
  if (!prior) return false
  state.hero.boons![selected] = Math.max(0, prior - 1)
  if (state.hero.boons![selected] === 0) delete state.hero.boons![selected]
  delete state.hero.boonEvolutions?.[selected]
  state.hero.boons![choice.id] = (state.hero.boons![choice.id] ?? 0) + (modal.mode === 'reforge' ? 1 : Math.max(1, prior))
  recordTelemetryCount(state, 'boonAugments', `${modal.mode}:${selected}:${choice.id}`)
  claim(state, current)
  state.modal = undefined
  log(state, modal.mode === 'reforge' ? `${boonFor(selected).name} reforges into ${choice.name}.` : `${boonFor(selected).name} transmutes into ${choice.name}.`)
  tend(state, boonAlignment(choice.id))
  return true
}

export function chooseTool(state: RunState, milestoneId: string, command: string): boolean {
  const current = milestone(state, milestoneId)
  if (!current) return false
  const tools = state.hero.traversalTools ?? []
  const modal = state.modal?.kind === 'tool' ? state.modal : undefined
  if (!modal) return false
  if (tools.length >= 2 && modal.replace === undefined) {
    const slot = Number(command) - 1
    if (slot < 0 || slot >= tools.length) return false
    state.modal = { ...modal, replace: slot }
    log(state, `Replace ${toolFor(tools[slot]).name}; choose a Waycache tool.`)
    return true
  }
  const choice = toolChoices(state, current)[Number(command) - 1]
  if (!choice) return false
  if (modal.replace === undefined) tools.push(choice.id)
  else tools[modal.replace] = choice.id
  state.hero.traversalTools = tools
  claim(state, current)
  state.modal = undefined
  log(state, `You bind ${choice.name}.`)
  return true
}

export function chooseRelic(state: RunState, milestoneId: string, command: string): boolean {
  const current = milestone(state, milestoneId)
  const modal = state.modal?.kind === 'relic' ? state.modal : undefined
  if (!current || !modal) return false
  const relics = state.hero.relics ??= []
  if (relics.length >= 3 && modal.replace === undefined) {
    const slot = Number(command) - 1
    if (slot < 0 || slot >= relics.length) return false
    state.modal = { ...modal, replace: slot }
    log(state, `Replace ${relicFor(relics[slot]).name}; choose a guardian relic.`)
    return true
  }
  const choice = relicChoices(state, current)[Number(command) - 1]
  if (!choice) return false
  if (modal.replace === undefined) relics.push(choice.id)
  else relics[modal.replace] = choice.id
  state.hero.relicCharges ??= {}
  for (const id of relics) if (id !== choice.id) delete state.hero.relicCharges[id]
  recordTelemetryCount(state, 'relicPicks', choice.id)
  claim(state, current)
  state.modal = undefined
  log(state, `Relic bound: ${choice.name}.`)
  tend(state, relicAlignment(choice.id))
  return true
}

export function openTools(state: RunState): ActionResult {
  const tools = state.hero.traversalTools ?? []
  if (!tools.length) { log(state, 'No ritual traversal tool is bound. Find a Waycache.'); return [] }
  state.modal = { kind: 'tools' }
  return [event('menu')]
}

export function chooseToolUse(state: RunState, command: string): boolean {
  const tool = state.hero.traversalTools?.[Number(command) - 1]
  if (!tool) return false
  const cooldown = toolCooldown(state, tool)
  if (cooldown) { log(state, `${toolFor(tool).name} recovers in ${cooldown} turn${cooldown === 1 ? '' : 's'}.`); return false }
  state.modal = { kind: 'target', action: tool, tool }
  return true
}

const setCooldown = (state: RunState, tool: TraversalToolId) => { (state.hero.cooldowns ??= {})[`tool:${tool}`] = Math.max(1, toolFor(tool).cooldown - boonRank(state, 'coolAsh')) }
const point = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, distance: number) => ({ x: state.hero.x + DIRECTIONS[direction].x * distance, y: state.hero.y + DIRECTIONS[direction].y * distance })
const passableLanding = (state: RunState, target: { x: number; y: number }) => isPassable(state.floor, target.x, target.y)

export function useTool(state: RunState, tool: TraversalToolId, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive = false): ActionResult {
  if (!(state.hero.traversalTools ?? []).includes(tool)) { log(state, 'That ritual tool is no longer bound.'); return [] }
  if (toolCooldown(state, tool)) { log(state, `${toolFor(tool).name} is still recovering.`); return [] }
  const result = tool === 'stoneWedge' ? useStoneWedge(state, direction, overdrive) : tool === 'reedwing' ? useReedwing(state, direction, overdrive) : tool === 'ashwayRites' ? useAshway(state, direction, overdrive) : tool === 'antlerPrybar' ? useAntlerPrybar(state, direction) : tool === 'stoneAdze' ? useStoneAdze(state, direction) : tool === 'resinFireBasket' ? useResinFireBasket(state, direction, overdrive) : tool === 'cordAnchor' ? useCordAnchor(state, direction, overdrive) : useWoodenLeverRoller(state, direction)
  if (!result) return []
  const healing = boonRank(state, 'rootedResolve') + (state.hero.health * 4 <= state.hero.maxHealth ? boonRank(state, 'lastLight') * 2 : 0)
  if (healing) state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + healing)
  const focus = boonRank(state, 'cordTempo')
  if (focus) state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + focus)
  if (consumeRelicTool(state)) { state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 3); log(state, 'Tide Fetter restores 3 HP after the crossing.') }
  armRelicTraversal(state)
  if (boonRank(state, 'pressureSeal')) state.hero.conditions = [...(state.hero.conditions ?? []), { kind: 'shielded', duration: 2, potency: boonRank(state, 'pressureSeal') }]
  if (boonRank(state, 'wayfinderCord')) revealNearbyMilestones(state, 2 + boonRank(state, 'wayfinderCord'))
  if (overdrive) {
    state.hero.traversalTools = (state.hero.traversalTools ?? []).filter(current => current !== tool)
    log(state, `${toolFor(tool).name} burns out after its overdrive.`)
  } else {
    setCooldown(state, tool)
    const cooldown = toolCooldown(state, tool)
    const relay = boonRank(state, 'relayStep')
    if (relay && cooldown) state.hero.cooldowns![`tool:${tool}`] = Math.max(1, cooldown - relay)
  }
  refreshFov(state)
  return advance(state, [event('spell')])
}

const useStoneWedge = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const target = point(state, direction, 1)
  const width = overdrive ? 1 : boonRank(state, 'stoneMemory') + boonRank(state, 'wallSong')
  const offsets = Array.from({ length: width * 2 + 1 }, (_, index) => index - width).map(offset => [DIRECTIONS[direction].y * offset, -DIRECTIONS[direction].x * offset])
  const cleared = offsets.map(([x, y]) => getTile(state.floor, target.x + x, target.y + y)).filter((tile): tile is NonNullable<ReturnType<typeof getTile>> => Boolean(tile && drillable.has(tile.kind)))
  if (!cleared.length) { log(state, 'Stone Wedge needs blocked ground.'); return false }
  cleared.forEach(tile => { tile.kind = 'floor' })
  log(state, overdrive ? 'The wedge opens a broad passage.' : 'The wedge opens a narrow passage.')
  return true
}

const useReedwing = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const length = (overdrive ? 3 : 2) + boonRank(state, 'reedMemory') + boonRank(state, 'updraftStep')
  const landing = point(state, direction, length)
  const crossed = Array.from({ length: length - 1 }, (_, index) => getTile(state.floor, point(state, direction, index + 1).x, point(state, direction, index + 1).y))
  if (!crossed.some(tile => tile && hazardous.has(tile.kind)) || !passableLanding(state, landing)) { log(state, 'Reedwing needs hazardous ground and a clear landing.'); return false }
  state.hero.x = landing.x
  state.hero.y = landing.y
  log(state, 'Reedwing carries you across the hazard.')
  return true
}

const useCordAnchor = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const landing = point(state, direction, overdrive ? 4 : 2)
  if (!passableLanding(state, landing)) { log(state, 'Cord Anchor needs a clear landing.'); return false }
  state.hero.x = landing.x
  state.hero.y = landing.y
  log(state, 'The cord draws you across the gap.')
  return true
}

const useAshway = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const length = overdrive ? 3 : 2
  const cells = Array.from({ length }, (_, index) => point(state, direction, index + 1))
  const targets = cells.map(cell => ({ cell, tile: getTile(state.floor, cell.x, cell.y) })).filter((entry): entry is { cell: { x: number; y: number }; tile: NonNullable<ReturnType<typeof getTile>> } => Boolean(entry.tile && hazardous.has(entry.tile.kind) && entry.tile.kind !== 'boulder'))
  if (!targets.length) { log(state, 'Ashway Rites need hazardous ground to bind.'); return false }
  state.floor.transientTerrain ??= []
  targets.forEach(({ cell, tile }) => {
    const existing = state.floor.transientTerrain!.find(current => current.x === cell.x && current.y === cell.y)
    if (!existing) state.floor.transientTerrain!.push({ ...cell, original: tile.kind, ...(tile.flow ? { originalFlow: { ...tile.flow } } : {}), expiresAt: state.turn + 6 })
    tile.kind = 'floor'
    delete tile.flow
  })
  log(state, 'Warm ash settles into a temporary route.')
  return true
}

const useAntlerPrybar = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>): boolean => {
  const target = point(state, direction, 1)
  const destination = point(state, direction, 2)
  const tile = getTile(state.floor, target.x, target.y)
  const landing = getTile(state.floor, destination.x, destination.y)
  const protectedDestination = destination.x === state.floor.start.x && destination.y === state.floor.start.y || destination.x === state.floor.exit.x && destination.y === state.floor.exit.y || state.floor.actors.some(actor => actor.health > 0 && actor.x === destination.x && actor.y === destination.y) || state.floor.milestones.some(milestone => !milestone.claimed && milestone.x === destination.x && milestone.y === destination.y)
  if (!tile || !['boulder', 'breakwall'].includes(tile.kind)) { log(state, 'Antler Prybar needs an adjacent boulder or breakwall.'); return false }
  if (!landing || landing.kind !== 'floor' || protectedDestination || state.floor.props.some(prop => prop.state !== 'destroyed' && prop.x === destination.x && prop.y === destination.y)) { log(state, 'Antler Prybar needs an empty legal destination beyond the target.'); return false }
  landing.kind = tile.kind
  tile.kind = 'floor'
  state.hero.conditions = [...(state.hero.conditions ?? []), { kind: 'marked', duration: 2, potency: 1 }]
  log(state, 'The Antler Prybar shifts the barrier; the noise leaves you exposed.')
  return true
}

const useStoneAdze = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>): boolean => {
  const target = point(state, direction, 1)
  const tile = getTile(state.floor, target.x, target.y)
  if (!tile || !['crate', 'crumble'].includes(tile.kind)) { log(state, 'Stone Adze cuts only an adjacent wooden barrier or weakened route.'); return false }
  tile.kind = 'floor'
  log(state, 'The Stone Adze cuts a narrow route through the weakened barrier.')
  return true
}

const useResinFireBasket = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const target = point(state, direction, 1)
  const tile = getTile(state.floor, target.x, target.y)
  if (!tile || !['bramble', 'web'].includes(tile.kind) || state.floor.actors.some(actor => actor.health > 0 && actor.x === target.x && actor.y === target.y) || target.x === state.floor.exit.x && target.y === state.floor.exit.y) { log(state, 'Resin Fire Basket needs an unoccupied adjacent bramble or web.'); return false }
  tile.kind = 'smoke'
  state.hero.conditions = [...(state.hero.conditions ?? []), { kind: 'burning', duration: overdrive ? 3 : 2, potency: 1 }]
  log(state, overdrive ? 'The resin flares wide; smoke and heat cling to you.' : 'The resin burns the growth into a smoking, risky crossing.')
  return true
}

const useWoodenLeverRoller = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>): boolean => {
  const target = point(state, direction, 1)
  const destination = point(state, direction, 2)
  const prop = state.floor.props.find(candidate => candidate.x === target.x && candidate.y === target.y && candidate.state !== 'destroyed' && ['mine.brokenCart', 'caverns.brokenBoat', 'ruins.collapsedArch'].includes(candidate.kind))
  const tile = getTile(state.floor, destination.x, destination.y)
  const protectedDestination = destination.x === state.floor.start.x && destination.y === state.floor.start.y || destination.x === state.floor.exit.x && destination.y === state.floor.exit.y || state.floor.actors.some(actor => actor.health > 0 && actor.x === destination.x && actor.y === destination.y) || state.floor.milestones.some(milestone => !milestone.claimed && milestone.x === destination.x && milestone.y === destination.y) || state.floor.props.some(candidate => candidate.state !== 'destroyed' && candidate.x === destination.x && candidate.y === destination.y)
  if (!prop) { log(state, 'Wooden Lever and Roller needs an adjacent movable prop.'); return false }
  if (!tile || tile.kind !== 'floor' || protectedDestination) { log(state, 'Wooden Lever and Roller needs an empty legal destination.'); return false }
  prop.x = destination.x
  prop.y = destination.y
  log(state, 'The lever rolls the prop forward, changing the route.')
  return true
}

export function expireAshways(state: RunState): void {
  const pending = state.floor.transientTerrain ?? []
  const active = pending.filter(current => {
    if (current.expiresAt > state.turn || (state.hero.x === current.x && state.hero.y === current.y)) return true
    const tile = getTile(state.floor, current.x, current.y)
    if (tile?.kind === 'floor') { tile.kind = current.original; if (current.originalFlow) tile.flow = { ...current.originalFlow } }
    return false
  })
  state.floor.transientTerrain = active.length ? active : undefined
}

export function revealMilestones(state: RunState): void {
  for (const current of state.floor.milestones) if (!current.claimed) current.discovered = true
}

export function revealNearbyMilestones(state: RunState, radius: number): void {
  for (const current of state.floor.milestones) if (!current.claimed && Math.max(Math.abs(current.x - state.hero.x), Math.abs(current.y - state.hero.y)) <= radius) current.discovered = true
}

export function useTimeKnot(state: RunState): ActionResult {
  const rank = boonRank(state, 'timeKnot')
  const positions = state.hero.safePositions ?? []
  if (!rank) { log(state, 'No Time Knot is bound.'); return [] }
  const current = `${state.hero.x},${state.hero.y}`
  const target = [...positions].reverse().find(point => `${point.x},${point.y}` !== current && isPassable(state.floor, point.x, point.y))
  if (!target) { log(state, 'Time Knot has no earlier safe position.'); return [] }
  state.hero.x = target.x
  state.hero.y = target.y
  const base = state.hero.boons?.timeKnot ?? 0
  if (base > 0) {
    state.hero.boons!.timeKnot = base - 1
    if (state.hero.boons!.timeKnot === 0) delete state.hero.boons!.timeKnot
  } else if (state.hero.boonEvolutions?.timeKnot) state.hero.boonEvolutions.timeKnot--
  refreshFov(state)
  log(state, 'Time Knot returns you to an earlier safe position. The world does not rewind.')
  return [event('spell')]
}

export function recordSafePosition(state: RunState): void {
  if (!isPassable(state.floor, state.hero.x, state.hero.y)) return
  const positions = state.hero.safePositions ??= []
  const last = positions.at(-1)
  if (!last || last.x !== state.hero.x || last.y !== state.hero.y) positions.push({ x: state.hero.x, y: state.hero.y })
  if (positions.length > 12 + boonRank(state, 'borrowedTime')) positions.shift()
}
