import type { Alignment, DeliveryEnding, RunState } from '../types'
import { log } from './shared'

export const ALIGNMENT_THRESHOLD = 4
export const alignmentLine = (alignment: Alignment): string => alignment === 'kami' ? 'Your report favors idealism.' : 'Your report favors pragmatism.'
export const tend = (state: RunState, alignment: Alignment): void => {
  state.alignment ??= { kami: 0, villagePact: 0 }
  state.alignment[alignment]++
  log(state, alignmentLine(alignment))
}
export const deliveryEndingFor = (alignment: Readonly<Record<Alignment, number>>): DeliveryEnding => {
  if (alignment.kami >= ALIGNMENT_THRESHOLD && alignment.villagePact >= ALIGNMENT_THRESHOLD) return 'both'
  if (alignment.kami >= ALIGNMENT_THRESHOLD) return 'kami'
  if (alignment.villagePact >= ALIGNMENT_THRESHOLD) return 'villagePact'
  return 'plain'
}
