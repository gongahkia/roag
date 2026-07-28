import type { Biome, GuardianPhase, MonsterRole, TileKind } from '../types'

export interface BossPhaseContract { terrain: TileKind; telegraph: string; counterplay: string }
export interface BossContract { id: string; arena: string; phases: Record<Exclude<GuardianPhase, 'opening'>, BossPhaseContract>; adds: MonsterRole[]; reward: 'relic' }

const contracts: Record<Biome, BossContract> = {
  mine: { id: 'mine:obsidian-warden', arena: 'rail quarry', phases: { pressure: { terrain: 'rail', telegraph: 'foreman-cavein', counterplay: 'leave the marked shelf' }, cataclysm: { terrain: 'crumble', telegraph: 'guardian-slam', counterplay: 'keep an escape lane' } }, adds: ['guard', 'pursuer'], reward: 'relic' },
  wilds: { id: 'wilds:heartwood', arena: 'root clearing', phases: { pressure: { terrain: 'bramble', telegraph: 'heartwood-charge', counterplay: 'cut a clear lane' }, cataclysm: { terrain: 'water', telegraph: 'guardian-slam', counterplay: 'keep dry footing' } }, adds: ['ambusher', 'controller'], reward: 'relic' },
  caverns: { id: 'caverns:geode-wyrm', arena: 'fissure chamber', phases: { pressure: { terrain: 'gas', telegraph: 'geode-fissure', counterplay: 'move clear of the fissure' }, cataclysm: { terrain: 'lava', telegraph: 'guardian-slam', counterplay: 'preserve a cool route' } }, adds: ['artillery', 'controller'], reward: 'relic' },
  ruins: { id: 'ruins:stone-keeper', arena: 'ritual precinct', phases: { pressure: { terrain: 'dart', telegraph: 'regent-decree', counterplay: 'move before the decree lands' }, cataclysm: { terrain: 'darkness', telegraph: 'regent-judgment', counterplay: 'keep a lit escape route' } }, adds: ['support', 'controller'], reward: 'relic' },
  furnace: { id: 'furnace:kiln-heart', arena: 'kiln terrace', phases: { pressure: { terrain: 'smoke', telegraph: 'enemy-fire', counterplay: 'take the lift lane' }, cataclysm: { terrain: 'fireVent', telegraph: 'guardian-slam', counterplay: 'leave the vent line' } }, adds: ['guard', 'artillery'], reward: 'relic' },
  floodedRuins: { id: 'flooded:drowned-regent', arena: 'tide basin', phases: { pressure: { terrain: 'current', telegraph: 'enemy-pull', counterplay: 'brace at an anchor' }, cataclysm: { terrain: 'deepWater', telegraph: 'guardian-slam', counterplay: 'keep a dry crossing' } }, adds: ['guard', 'controller'], reward: 'relic' },
  cliffs: { id: 'cliffs:sky-warden', arena: 'wind shelf', phases: { pressure: { terrain: 'ledge', telegraph: 'enemy-shot', counterplay: 'hold the rope route' }, cataclysm: { terrain: 'smoke', telegraph: 'guardian-slam', counterplay: 'wait out the gust' } }, adds: ['skirmisher', 'pursuer'], reward: 'relic' },
  burial: { id: 'burial:barrow-king', arena: 'ancestor field', phases: { pressure: { terrain: 'spiritPath', telegraph: 'enemy-ritual', counterplay: 'leave the procession path' }, cataclysm: { terrain: 'graveSoil', telegraph: 'guardian-slam', counterplay: 'keep an open grave lane' } }, adds: ['support', 'pursuer'], reward: 'relic' },
  saltFlats: { id: 'salt:sovreign', arena: 'mirror basin', phases: { pressure: { terrain: 'saltMirror', telegraph: 'enemy-pull', counterplay: 'break the mirror line' }, cataclysm: { terrain: 'brine', telegraph: 'guardian-slam', counterplay: 'leave the brine edge' } }, adds: ['skirmisher', 'artillery'], reward: 'relic' },
  frostReliquary: { id: 'frost:reliquary-warden', arena: 'ice reliquary', phases: { pressure: { terrain: 'ice', telegraph: 'enemy-ward', counterplay: 'break the ward line' }, cataclysm: { terrain: 'frostRime', telegraph: 'guardian-slam', counterplay: 'keep a thawed route' } }, adds: ['guard', 'support'], reward: 'relic' }
}

const guardianBiome: Record<string, Biome> = { foreman: 'mine', heartwood: 'wilds', geode: 'caverns', regent: 'ruins', kilnheart: 'furnace', drownedRegent: 'floodedRuins', skyWarden: 'cliffs', barrowKing: 'burial', saltSovereign: 'saltFlats', reliquaryWarden: 'frostReliquary' }

export const bossContractFor = (biome: Biome): BossContract => contracts[biome]
export const bossContractForGuardian = (kind: string): BossContract | undefined => guardianBiome[kind] ? contracts[guardianBiome[kind]] : undefined
