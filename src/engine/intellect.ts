import type { Hero } from '../types'

const has = (hero: Hero, skill: string): boolean => hero.skills.includes(skill)
export const intellectFocusDiscount = (hero: Hero): number => has(hero, 'int1') ? 1 : 0
export const intellectFocusRecovery = (hero: Hero): number => (has(hero, 'int2') ? 1 : 0) + (has(hero, 'int6') ? 1 : 0)
export const intellectScriptRange = (hero: Hero): number => 1 + (has(hero, 'int3') ? 1 : 0) + (has(hero, 'int5') ? 1 : 0)
export const intellectWardBonus = (hero: Hero): number => has(hero, 'int4') ? 2 : 0
export const hasAstralGateAccess = (hero: Hero): boolean => has(hero, 'int6')
