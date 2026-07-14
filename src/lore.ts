import { biomeName } from './content'
import { heirNameFor } from './engine/hub'
import { streamSeed } from './rng'
import { mineSeason } from './season'
import type { LegacyRecord } from './types'

export const TYPEWRITER_INTERVAL = 28

export interface LoreScene { title: string; pages: string[] }
export interface StoryState { scene: LoreScene; page: number; pageStartedAt: number; complete?: boolean }
export interface LoadingState { phase: 'fade' | 'loading'; startedAt: number }

const pick = <T>(seed: number, scope: string, values: readonly T[]): T => values[streamSeed(seed, 'generation', scope) % values.length]
const characters = (value: string): string[] => Array.from(value)

export const openingLore = (heirSeed: number): LoreScene => {
  const season = mineSeason(heirSeed)
  const courier = heirNameFor(heirSeed)
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
  return { title: 'VILLAGE TRAILHEAD', pages: [`${opening}\n${season.scene}`, `${courier} takes the courier\'s mark.\n${charge}`] }
}

export const successionLore = (record: LegacyRecord, successorSeed: number): LoreScene => {
  const formerSeason = mineSeason(record.seed)
  const nextSeason = mineSeason(successorSeed)
  const successor = heirNameFor(successorSeed)
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
    title: 'THREADS OF THE TRAIL',
    pages: [
      `${record.heirName} fell in ${biomeName[record.biome]}, trail ${record.floor + 1}.\nThe sealed parcel slipped into the dark.`,
      `${passage}\n${record.heirName}\'s path reaches ${successor}.`,
      `${successor} answers at the village trail.\n${inheritance}`
    ]
  }
}

export const createStory = (scene: LoreScene, now: number): StoryState => ({ scene, page: 0, pageStartedAt: now })
export const storyPage = (story: StoryState): string => story.scene.pages[story.page] ?? ''
export const storyProgress = (story: StoryState, now: number): number => story.complete ? characters(storyPage(story)).length : Math.min(characters(storyPage(story)).length, Math.floor(Math.max(0, now - story.pageStartedAt) / TYPEWRITER_INTERVAL))
export const storyText = (story: StoryState, now: number): string => characters(storyPage(story)).slice(0, storyProgress(story, now)).join('')
export const isStoryPageComplete = (story: StoryState, now: number): boolean => storyProgress(story, now) === characters(storyPage(story)).length

export const advanceStory = (story: StoryState, now: number): { story?: StoryState; finished: boolean } => {
  if (!isStoryPageComplete(story, now)) return { story: { ...story, complete: true }, finished: false }
  if (story.page + 1 < story.scene.pages.length) return { story: { ...story, page: story.page + 1, pageStartedAt: now, complete: false }, finished: false }
  return { finished: true }
}
