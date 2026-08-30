import type { AutoplayMode, AutoplayPolicy } from './types'

export const autoplayModes: readonly AutoplayMode[] = ['off', 'visible', 'omniscient']
export const autoplayPolicies: readonly AutoplayPolicy[] = ['survival', 'clear', 'explore', 'legacy']
export const nextAutoplayMode = (mode: AutoplayMode): AutoplayMode => autoplayModes[(autoplayModes.indexOf(mode) + 1) % autoplayModes.length]
export const nextAutoplayPolicy = (policy: AutoplayPolicy): AutoplayPolicy => autoplayPolicies[(autoplayPolicies.indexOf(policy) + 1) % autoplayPolicies.length]
export const autoplayModeLabel = (mode: AutoplayMode): string => mode === 'visible' ? 'VISIBLE' : mode === 'omniscient' ? 'FULL MAP' : 'OFF'
export const autoplayPolicyLabel = (policy: AutoplayPolicy): string => policy === 'clear' ? 'CLEAR RATE' : policy === 'explore' ? 'EXPLORE' : policy === 'legacy' ? 'LEGACY' : 'SURVIVAL'
