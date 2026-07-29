import type { Hero } from '../types'

const has = (hero: Hero, skill: string): boolean => hero.skills.includes(skill)
export const strengthMeleeBonus = (hero: Hero): number => (has(hero, 'str1') ? 1 : 0) + (has(hero, 'str2') ? 1 : 0) + (has(hero, 'str6') ? 2 : 0)
export const strengthGuard = (hero: Hero): number => has(hero, 'str4') ? 2 : 0
export const canBreakRubble = (hero: Hero): boolean => has(hero, 'str3')
export const canKnockback = (hero: Hero): boolean => has(hero, 'str5')
