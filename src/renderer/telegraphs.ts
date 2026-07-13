import type { Telegraph } from '../types'

export interface TelegraphPresentation { glyph: string; color: string; label: string }

export const presentTelegraph = (telegraph: Telegraph, turn: number, source: string): TelegraphPresentation => {
  const impact = Math.max(0, telegraph.resolveTurn - turn)
  const major = telegraph.danger === 'major'
  return { glyph: major ? '!' : ':', color: major ? '#ee6f78' : '#f4d26a', label: `T-${impact} ${major ? 'MAJ' : 'MIN'} ${source}` }
}
