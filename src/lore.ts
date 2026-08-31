import { biomeName } from './content'
import { deliveryEndingFor } from './engine/alignment'
import { streamSeed } from './rng'
import type { Alignment, Biome, LegacyRecord, RunState } from './types'

export const TYPEWRITER_INTERVAL = 28

export type LoreVignette = 'opening' | 'succession' | 'ending'
export interface LoreScene { title: string; pages: string[]; vignette: LoreVignette }
export interface StoryState { scene: LoreScene; page: number; pageStartedAt: number; complete?: boolean }
export interface LoadingState {
  startedAt: number
  kind: 'trailhead' | 'biome'
  fromBiome?: Biome
  toBiome?: Biome
}
export interface TransitState {
  fromBiome: Biome
  toBiome?: Biome
  startedAt: number
  fromLabel?: string
  toLabel?: string
}

const pick = <T>(seed: number, scope: string, values: readonly T[]): T => values[streamSeed(seed, 'generation', scope) % values.length]
const characters = (value: string): string[] => Array.from(value)
const commanderNames = ['Ari', 'Cato', 'Iris', 'Mara', 'Niko', 'Sable']
const navigatorNames = ['Aster', 'Dara', 'Juno', 'Keen', 'Mika', 'Sol']
export const villageElderName = (seed: number): string => pick(seed, 'lore:commander', commanderNames)
export const shrineChiefName = (seed: number): string => pick(seed, 'lore:navigator', navigatorNames)

export const openingLore = (seed: number, courierName: string): LoreScene => {
  const elder = villageElderName(seed)
  const chief = shrineChiefName(seed)
  return {
    title: 'JOMON VOYAGER // LANDING BRIEF', vignette: 'opening', pages: [
      `${elder} (mission commander): ${courierName}, your landing team is cleared.\nNew Edo is still beyond the route.`,
      `${chief} (navigation): Survey each colony, recover its people, and return with a route the Voyager can trust.`
    ]
  }
}

export const successionLore = (record: LegacyRecord, successorSeed: number): LoreScene => {
  const elder = villageElderName(successorSeed)
  const chief = shrineChiefName(successorSeed)
  return {
    title: 'JOMON VOYAGER // RELIEF BRIEF', vignette: 'succession', pages: [
      `${elder} (mission commander): ${record.heirName}'s survey ended on ${biomeName[record.biome]}.\nTheir record is incomplete.`,
      `${chief} (navigation): The carrier cannot wait in orbit.\nAnother specialist must take the landing file.`
    ]
  }
}

export const endingLore = (state: RunState, completedAreas: readonly Biome[], alignment: Readonly<Record<Alignment, number>> = state.alignment ?? { kami: 0, villagePact: 0 }): LoreScene => {
  const elder = villageElderName(state.seed)
  const chief = shrineChiefName(state.seed)
  const route = completedAreas.length ? completedAreas.map(area => biomeName[area]).join(', ') : 'the outer route'
  const ending = deliveryEndingFor(alignment)
  const result = ending === 'both'
    ? `${chief} (navigation): The route is sound.\n${elder}: New Edo receives both the data and the people who made it home.`
    : ending === 'kami'
      ? `${chief} (navigation): The idealist record changes our approach.\nNew Edo will meet the unknown with open hands.`
      : ending === 'villagePact'
        ? `${elder} (mission commander): The practical route holds.\nNew Edo will have what it needs to survive.`
        : `${chief} (navigation): The survey is complete.\nNew Edo has time to choose its next horizon.`
  return {
    title: 'JOMON VOYAGER // NEW EDO', vignette: 'ending', pages: [
      `${state.hero.name}: Landing reports delivered.\n${chief}: New Edo has the route.`,
      `${elder}: The Jomon Voyager crossed ${route}.\n${chief}: The carrier has done its work.`,
      result
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
