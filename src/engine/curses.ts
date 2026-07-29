import type { RunState } from '../types'
import { log } from './shared'

export const markCurseDamaged = (state: RunState): void => {
  const curse = state.hero.curse
  if (!curse || curse.failed) return
  curse.failed = true
  if (curse.lethal) {
    state.hero.health = 0
    state.status = 'dead'
    state.modal = undefined
    log(state, `${curse.name} claims the courier.`)
    return
  }
  state.hero.maxHealth = Math.max(6, state.hero.maxHealth - 3)
  state.hero.health = Math.min(state.hero.health, Math.max(1, Math.floor(state.hero.maxHealth / 2)))
  log(state, `${curse.name} breaks: maximum health is reduced and you are left at half health.`)
  state.hero.curse = undefined
}

export const settleCurseAfterEncounter = (state: RunState): void => {
  const curse = state.hero.curse
  if (!curse || curse.failed) return
  curse.remainingEncounters--
  if (curse.remainingEncounters > 0) {
    log(state, `${curse.name}: ${curse.remainingEncounters} encounter${curse.remainingEncounters === 1 ? '' : 's'} remain.`)
    return
  }
  log(state, `${curse.name} releases its reward.`)
  state.hero.curse = undefined
}
