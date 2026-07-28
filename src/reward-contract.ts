import { rngFor } from './rng'
import type { Biome, BoonId, BoonRewardChoice, Floor, RewardContext, RewardMilestoneId, RewardOffer, TileKind, ToolRewardChoice, TraversalToolId } from './types'

export interface RewardContractInput { campaignSeed: number; floorIndex: number; biome: Biome; recipeId: string; escalationVariant: string }
interface RewardTemplate { terrain: TileKind; problem: string; payoff: string; tools: readonly TraversalToolId[]; safe: readonly BoonId[]; risky: readonly BoonId[]; sidegrade: readonly BoonId[] }

const templates: Record<Biome, RewardTemplate> = {
  mine: { terrain: 'rubble', problem: 'blocked mine passages', payoff: 'open a shorter return route', tools: ['stoneWedge', 'ashwayRites', 'cordAnchor'], safe: ['stoneMemory', 'wallSong', 'breachTempo'], risky: ['hardLesson', 'emberFletching'], sidegrade: ['cacheSense', 'scoutEye'] },
  wilds: { terrain: 'bramble', problem: 'bramble-choked crossings', payoff: 'claim the wetland payoff', tools: ['reedwing', 'ashwayRites', 'cordAnchor'], safe: ['softLanding', 'wayfinderCord', 'rootedResolve'], risky: ['dangerInstinct', 'emberFletching'], sidegrade: ['mapMoss', 'trailRations'] },
  caverns: { terrain: 'water', problem: 'tide-cut cave paths', payoff: 'reach the deep chamber safely', tools: ['reedwing', 'cordAnchor', 'ashwayRites'], safe: ['tideSkin', 'sealedBreath', 'wayfinderCord'], risky: ['openCircuit', 'bloodCompass'], sidegrade: ['quietTide', 'scoutEye'] },
  ruins: { terrain: 'dart', problem: 'warded sightlines', payoff: 'break into the optional precinct', tools: ['stoneWedge', 'cordAnchor', 'ashwayRites'], safe: ['wallSong', 'pressureSeal', 'rootBattery'], risky: ['hookLine', 'glassNerve'], sidegrade: ['spiritKindling', 'cacheSense'] },
  furnace: { terrain: 'smoke', problem: 'smoke-choked lift lanes', payoff: 'take the kiln shortcut', tools: ['reedwing', 'ashwayRites', 'cordAnchor'], safe: ['smokeWalker', 'slagSkin', 'updraftStep'], risky: ['cinderEdge', 'hardLesson'], sidegrade: ['ashDividend', 'scavengerMap'] },
  floodedRuins: { terrain: 'current', problem: 'current-cut crossings', payoff: 'hold the anchored payoff route', tools: ['reedwing', 'cordAnchor', 'ashwayRites'], safe: ['currentSense', 'tideSkin', 'anchorHabit'], risky: ['tideEdge', 'descentEngine'], sidegrade: ['salvager', 'scavengerMap'] },
  cliffs: { terrain: 'ledge', problem: 'exposed ledge crossings', payoff: 'secure the high shelf', tools: ['cordAnchor', 'reedwing', 'ashwayRites'], safe: ['galeThread', 'updraftCadence', 'softLanding'], risky: ['skyhookReprisal', 'thunderVessel'], sidegrade: ['highPath', 'eyrieHoard'] },
  burial: { terrain: 'spiritPath', problem: 'procession-bound grave paths', payoff: 'reach the cairn side route', tools: ['cordAnchor', 'ashwayRites', 'reedwing'], safe: ['burialCurrent', 'bridgeOfNames', 'ossuaryWard'], risky: ['lastRites', 'descentEngine'], sidegrade: ['graveLedger', 'ancestorLantern'] },
  saltFlats: { terrain: 'brine', problem: 'brine and mirror crossings', payoff: 'cross the white-road detour', tools: ['reedwing', 'ashwayRites', 'cordAnchor'], safe: ['sunstep', 'brineWard', 'whiteRoad'], risky: ['mirrorHunt', 'glassEdge'], sidegrade: ['saltLedger', 'mirageMap'] },
  frostReliquary: { terrain: 'ice', problem: 'ice-bound reliquary paths', payoff: 'claim the frozen chamber', tools: ['reedwing', 'cordAnchor', 'ashwayRites'], safe: ['coldRead', 'rimeGuard', 'thawStep'], risky: ['duelistOath', 'shatterMark'], sidegrade: ['winterRations', 'iceLedger'] }
}

const context = (template: RewardTemplate, role: RewardContext['role'], route: RewardContext['route'], biomeFit: RewardContext['biomeFit']): Omit<RewardContext, 'id'> => ({ role, problem: template.problem, terrain: template.terrain, route, payoff: template.payoff, biomeFit })
const boonChoice = (id: BoonId, template: RewardTemplate, role: RewardContext['role'], route: RewardContext['route'], biomeFit: RewardContext['biomeFit']): BoonRewardChoice => ({ id, ...context(template, role, route, biomeFit) })
const toolChoice = (id: TraversalToolId, template: RewardTemplate, role: RewardContext['role'], route: RewardContext['route'], biomeFit: RewardContext['biomeFit']): ToolRewardChoice => ({ id, ...context(template, role, route, biomeFit) })

export const rewardOffersFor = (input: RewardContractInput): RewardOffer[] => {
  const template = templates[input.biome]
  const rng = rngFor(input.campaignSeed, 'generation', input.floorIndex, 'reward-offers', input.biome, input.recipeId, input.escalationVariant)
  const boonOffer = (milestoneId: 'boon-teach' | 'boon-test' | 'boon-payoff', route: RewardContext['route']): RewardOffer => ({
    id: `reward:${input.floorIndex}:${milestoneId}`,
    milestoneId,
    kind: 'boon',
    choices: [boonChoice(rng.pick(template.safe), template, 'safe', route, 'local'), boonChoice(rng.pick(template.risky), template, 'risky', route, 'local'), boonChoice(rng.pick(template.sidegrade), template, 'sidegrade', route, 'global')]
  })
  return [
    { id: `reward:${input.floorIndex}:waycache`, milestoneId: 'waycache', kind: 'waycache', choices: [toolChoice(template.tools[0], template, 'safe', 'safe', 'local'), toolChoice(template.tools[1], template, 'risky', 'costly', 'local'), toolChoice(template.tools[2], template, 'sidegrade', 'optional', 'global')] },
    boonOffer('boon-teach', 'safe'), boonOffer('boon-test', 'costly'), boonOffer('boon-payoff', 'optional')
  ]
}

export const rewardOfferFor = (floor: Pick<Floor, 'rewardOffers'>, milestoneId: RewardMilestoneId | undefined): RewardOffer | undefined => milestoneId ? floor.rewardOffers?.find(offer => offer.milestoneId === milestoneId) : undefined
