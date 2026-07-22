import { biomeName } from './content'
import { streamSeed } from './rng'
import { mineSeason } from './season'
import type { Biome, LegacyRecord, RunState } from './types'

export const TYPEWRITER_INTERVAL = 28

export interface AsciiAnimation { frames: string[]; frameMs: number }
export interface LoreScene { title: string; pages: string[]; animation?: AsciiAnimation }
export interface StoryState { scene: LoreScene; page: number; pageStartedAt: number; complete?: boolean }
export interface LoadingState { phase: 'fade' | 'loading'; startedAt: number }

export const trailAnimation: AsciiAnimation = { frameMs: 190, frames: [
  '      .      \n   .  /|\\  .  \n  /\\ / | \\ /\\ \n /  V  @  V  \\\n/___|_/ \\_|___\\\n    /___\\',
  '   .     .   \n    /|\\      \n  /\\ | \\ /\\ \n /  V @  V  \\\n/___|_/ \\_|___\\\n    /___\\',
  ' .      .     \n   /|\\  .    \n  /\\ | \\ /\\ \n /  V  @ V  \\\n/___|_/ \\_|___\\\n    /___\\'
] }

export const threadsAnimation: AsciiAnimation = { frameMs: 170, frames: [
  '  .-.-.      \n /  |  \\     \n|  / \\  |    \n| /   \\ |    \n|/  *  \\|    \n \\  |  /     \n  `-^-`',
  '  .-.-.      \n /  |  \\     \n|  / \\  |    \n|/     \\|    \n|\\  *  /|    \n \\  |  /     \n  `-^-`',
  '  .-.-.      \n /  |  \\     \n| /   \\ |    \n|/  *  \\|    \n|\\     /|    \n \\  |  /     \n  `-^-`'
] }

export const deliveryAnimation: AsciiAnimation = { frameMs: 190, frames: [
  "    .----.    \n  .'  __  '.  \n |  |__|  |  \n  '.______.'  \n     [ - ]",
  "    .----.    \n  .'  __  '.  \n |  |__|  |  \n  '.______.'  \n     [ = ]",
  "    .----.    \n  .'  __  '.  \n |  |__|  |  \n  '.______.'  \n     [ + ]"
] }

export const loadingAnimation: AsciiAnimation = { frameMs: 140, frames: [
  '  [=     ]  \n  /|  .  |\\ \n /_|_____|_\\\n    / \\',
  '  [==    ]  \n  /| . . |\\ \n /_|_____|_\\\n    / \\',
  '  [===   ]  \n  /|.   .|\\ \n /_|_____|_\\\n    / \\',
  '  [====  ]  \n  /| . . |\\ \n /_|_____|_\\\n    / \\',
  '  [===== ]  \n  /|  .  |\\ \n /_|_____|_\\\n    / \\'
] }

const pick = <T>(seed: number, scope: string, values: readonly T[]): T => values[streamSeed(seed, 'generation', scope) % values.length]
const characters = (value: string): string[] => Array.from(value)

export const openingLore = (heirSeed: number, courierName: string): LoreScene => {
  const season = mineSeason(heirSeed)
  const opening = pick(heirSeed, 'lore:opening', [
    `The ${season.name.toLowerCase()} opens over the village trail.`,
    `The village trail stirs beneath the ${season.name.toLowerCase()}.`,
    `Under the ${season.name.toLowerCase()}, the old path wakes again.`
  ])
  const charge = pick(heirSeed, 'lore:charge', [
    'The sealed parcel waits beyond the outpost.',
    'A sealed parcel waits for a steady hand.',
    'The outpost keeps a parcel for the next courier.'
  ])
  return { title: 'VILLAGE TRAILHEAD', animation: trailAnimation, pages: [`${opening}\n${season.scene}`, `${courierName} takes the courier\'s mark.\n${charge}`] }
}

export const successionLore = (record: LegacyRecord, successorSeed: number): LoreScene => {
  const formerSeason = mineSeason(record.seed)
  const nextSeason = mineSeason(successorSeed)
  const passage = pick(successorSeed, `lore:passage:${record.id}`, [
    `The ${formerSeason.name.toLowerCase()} gives way to ${nextSeason.name.toLowerCase()}.`,
    `The trail turns from ${formerSeason.name.toLowerCase()} into ${nextSeason.name.toLowerCase()}.`,
    `Seasons gather where the old road bends: ${formerSeason.name.toLowerCase()} to ${nextSeason.name.toLowerCase()}.`
  ])
  const inheritance = pick(successorSeed, `lore:inheritance:${record.id}`, [
    'The parcel passes on. The trail remembers.',
    'The road keeps the name, then calls another.',
    'No delivery ends while another hand answers.'
  ])
  return {
    title: 'THREADS OF THE TRAIL', animation: threadsAnimation,
    pages: [
      `${record.heirName} fell in ${biomeName[record.biome]}, trail ${record.floor + 1}.\nThe sealed parcel slipped into the dark.`,
      `${passage}\n${record.heirName}\'s path reaches a new courier.`,
      `A new courier answers at the village trail.\n${inheritance}`
    ]
  }
}

export const endingLore = (state: RunState, completedAreas: readonly Biome[]): LoreScene => {
  const arrival = pick(state.seed, 'lore:ending:arrival', [
    'Lanterns rise along the keeper\'s threshold.',
    'The outpost bells carry over the quiet trail.',
    'Rain settles on the road as the gate opens.'
  ])
  const remembrance = pick(state.seed, 'lore:ending:remembrance', [
    'The keeper records every hard-won mile.',
    'The village sets the parcel among its oldest promises.',
    'The road is marked safe for the couriers who follow.'
  ])
  const companions = state.rescuedNpcs?.map(npc => npc.name) ?? []
  const rescue = companions.length ? `${companions.join(', ')} return${companions.length === 1 ? 's' : ''} with the trail.` : 'The trail returns its silence to the village.'
  const route = completedAreas.length ? completedAreas.map(area => biomeName[area]).join(', ') : 'the old road'
  const kills = state.telemetry?.kills ?? 0
  return {
    title: 'THE LAST MILE', animation: deliveryAnimation,
    pages: [
      `${state.hero.name} brings the sealed parcel to its keeper.\n${arrival}`,
      `The trail marks return from ${route}.\n${rescue} ${kills} threats were turned aside.`,
      `${remembrance}\nThe delivery is complete.`
    ]
  }
}

export const createStory = (scene: LoreScene, now: number): StoryState => ({ scene, page: 0, pageStartedAt: now })
export const storyPage = (story: StoryState): string => story.scene.pages[story.page] ?? ''
export const storyProgress = (story: StoryState, now: number): number => story.complete ? characters(storyPage(story)).length : Math.min(characters(storyPage(story)).length, Math.floor(Math.max(0, now - story.pageStartedAt) / TYPEWRITER_INTERVAL))
export const storyText = (story: StoryState, now: number): string => characters(storyPage(story)).slice(0, storyProgress(story, now)).join('')
export const isStoryPageComplete = (story: StoryState, now: number): boolean => storyProgress(story, now) === characters(storyPage(story)).length
export const animationFrame = (animation: AsciiAnimation | undefined, now: number): string => !animation?.frames.length ? '' : animation.frames[Math.floor(now / animation.frameMs) % animation.frames.length]

export const advanceStory = (story: StoryState, now: number): { story?: StoryState; finished: boolean } => {
  if (!isStoryPageComplete(story, now)) return { story: { ...story, complete: true }, finished: false }
  if (story.page + 1 < story.scene.pages.length) return { story: { ...story, page: story.page + 1, pageStartedAt: now, complete: false }, finished: false }
  return { finished: true }
}
