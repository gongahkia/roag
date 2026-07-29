import type { Hero } from '../types'

const has = (hero: Hero, skill: string): boolean => hero.skills.includes(skill)
export const agilityMoveDistance = (hero: Hero): number => 1 + (has(hero, 'agi1') ? 1 : 0) + (has(hero, 'agi5') ? 1 : 0)
export const agilityReachBonus = (hero: Hero): number => has(hero, 'agi2') ? 1 : 0
export const agilityEvasion = (hero: Hero): number => has(hero, 'agi4') ? 3 : 0
export const agilityTelegraphAvoidance = (hero: Hero): number => (has(hero, 'agi3') ? 20 : 0) + (has(hero, 'agi6') ? 35 : 0)
