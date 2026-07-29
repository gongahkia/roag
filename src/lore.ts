import { biomeName } from './content'
import { deliveryEndingFor } from './engine/alignment'
import { streamSeed } from './rng'
import type { Alignment, Biome, LegacyRecord, RunState } from './types'

export const TYPEWRITER_INTERVAL = 28

export type LoreVignette = 'opening' | 'succession' | 'ending'
export interface LoreScene { title: string; pages: string[]; vignette: LoreVignette }
export interface StoryState { scene: LoreScene; page: number; pageStartedAt: number; complete?: boolean }
export interface LoadingState {
  phase: 'fade' | 'loading'
  startedAt: number
  kind: 'trailhead' | 'biome'
  fromBiome?: Biome
  toBiome?: Biome
}

const pick = <T>(seed: number, scope: string, values: readonly T[]): T => values[streamSeed(seed, 'generation', scope) % values.length]
const characters = (value: string): string[] => Array.from(value)
const elderNames = ['Ame', 'Kaya', 'Mori', 'Sumi', 'Tama', 'Ume']
const chiefNames = ['Hana', 'Kiri', 'Nagi', 'Sayo', 'Toki', 'Yuki']
export const villageElderName = (seed: number): string => pick(seed, 'lore:elder', elderNames)
export const shrineChiefName = (seed: number): string => pick(seed, 'lore:chief', chiefNames)

export const openingLore = (seed: number, courierName: string): LoreScene => {
  const elder = villageElderName(seed)
  const chief = shrineChiefName(seed)
  return {
    title: 'MURA NO MICHI', vignette: 'opening', pages: [
      `${elder} (village elder): ${courierName}, take this warning to ${chief} (shrine chief).\nThe coast has armed.`,
      `${elder}: Carry it through the old roads.\nIf ${chief} reads it, the villages can answer.`
    ]
  }
}

export const successionLore = (record: LegacyRecord, successorSeed: number): LoreScene => {
  const elder = villageElderName(successorSeed)
  const chief = shrineChiefName(successorSeed)
  return {
    title: 'KAKO NO MICHI', vignette: 'succession', pages: [
      `${elder} (village elder): ${record.heirName} did not reach ${chief}.\nThe warning died in ${biomeName[record.biome]}.`,
      `${elder}: That generation ends. The coast will not wait.\nAnother courier must carry the warning.`
    ]
  }
}

export const endingLore = (state: RunState, completedAreas: readonly Biome[], alignment: Readonly<Record<Alignment, number>> = state.alignment ?? { kami: 0, villagePact: 0 }): LoreScene => {
  const elder = villageElderName(state.seed)
  const chief = shrineChiefName(state.seed)
  const route = completedAreas.length ? completedAreas.map(area => biomeName[area]).join(', ') : 'the old road'
  const ending = deliveryEndingFor(alignment)
  const result = ending === 'both'
    ? `${chief} (shrine chief): The kami have heard.\n${elder}: The villages will stand together. The coast holds.`
    : ending === 'kami'
      ? `${chief} (shrine chief): The rite is set.\nThe kami guide the watchfires on the coast.`
      : ending === 'villagePact'
        ? `${elder} (village elder): The pact is carried.\nThe coastal villages gather their guards.`
        : `${chief} (shrine chief): The warning is read.\nThe villages have time to choose their answer.`
  return {
    title: 'SAIGO NO MICHI', vignette: 'ending', pages: [
      `${state.hero.name}: The warning from the inland villages.\n${chief}: I have it.`,
      `${elder}: The courier crossed ${route}.\n${chief}: The road has done its work.`,
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
