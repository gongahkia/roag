import { MONSTERS, SKILLS } from '../content'
import type { RunState } from '../types'
import { rngFor } from '../rng'
import { log } from './shared'

export const skillChoices = (state: RunState) => {
  const owned = new Set(state.hero.skills)
  const eligible = SKILLS.filter(skill => !owned.has(skill.id) && skill.prerequisites.every(prerequisite => owned.has(prerequisite))).sort((a, b) => a.id < b.id ? -1 : a.id > b.id ? 1 : 0)
  const rng = rngFor(state.seed, 'progression', 'offers', state.hero.level, [...owned].sort().join(','))
  const continuations = rng.shuffle(eligible.filter(skill => skill.prerequisites.length > 0))
  const offers = continuations.length ? [continuations[0]] : []
  for (const skill of rng.shuffle([...eligible])) if (offers.length < 3 && !offers.includes(skill)) offers.push(skill)
  return offers
}

export function chooseSkill(state: RunState, command: string): boolean {
  const choice = skillChoices(state)[Number(command) - 1]
  if (!choice) return false
  state.hero.skills.push(choice.id)
  state.hero.stats[choice.stat]++
  if (choice.stat === 'vitality') { state.hero.maxHealth += 2; state.hero.health += 2 }
  if (choice.stat === 'intellect') { state.hero.maxFocus += 2; state.hero.focus += 2 }
  state.modal = undefined
  log(state, `You learn ${choice.name}.`)
  return true
}

export function gainXp(state: RunState, amount: number): void {
  state.hero.xp += amount
  if (state.hero.xp < state.hero.level * 35) return
  state.hero.level++
  state.hero.maxHealth += 1
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 4)
  state.modal = { kind: 'skills', source: 'level' }
  log(state, `Level ${state.hero.level}: choose a discipline.`)
}

export const monsterXp = (kind: string) => MONSTERS.find(monster => monster.id === kind)?.xp ?? 10
