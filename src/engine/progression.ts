import { MONSTERS, SKILLS } from '../content'
import type { RunState, StatName } from '../types'
import { log } from './shared'

export const skillChoices = (state: RunState) => (['strength', 'agility', 'vitality', 'intellect'] as StatName[]).map(stat => SKILLS.find(skill => skill.stat === stat && !state.hero.skills.includes(skill.id))).filter((skill): skill is typeof SKILLS[number] => Boolean(skill))

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
  state.modal = { kind: 'skills' }
  log(state, `Level ${state.hero.level}: choose a discipline.`)
}

export const monsterXp = (kind: string) => MONSTERS.find(monster => monster.id === kind)?.xp ?? 10
