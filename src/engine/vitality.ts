import type { Hero } from '../types'

const has = (hero: Hero, skill: string): boolean => hero.skills.includes(skill)
export const vitalityRecovery = (hero: Hero): number => (has(hero, 'vit2') ? 1 : 0) + (has(hero, 'vit4') ? 3 : 0)
export const vitalityShield = (hero: Hero): number => has(hero, 'vit3') ? 1 : 0
export const vitalityHazardReduction = (hero: Hero): number => has(hero, 'vit5') ? 2 : 0
export const vitalityRescueRecovery = (hero: Hero): number => has(hero, 'vit6') ? 6 : 0
