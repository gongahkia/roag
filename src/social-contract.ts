import { rngFor } from './rng'
import type { Biome, SocialContract, SocialDisposition, SocialFaction, SocialOffer, SocialReputation, SocialRole } from './types'

export interface SocialContractInput { seed: number; floorIndex: number; biome: Biome; recipeId: string; arcId?: string }

const roles: readonly SocialRole[] = ['trader', 'strandedExplorer', 'rival', 'caretaker', 'ritualist', 'territorialGroup']
const goals: Record<SocialRole, string> = {
  trader: 'move surplus along a safe route', strandedExplorer: 'reach the next marked refuge', rival: 'claim the faster route', caretaker: 'keep a local crossing intact', ritualist: 'protect a charged landmark', territorialGroup: 'control the costly approach'
}
const offerFor = (role: SocialRole): SocialOffer => role === 'trader' || role === 'strandedExplorer' ? 'supplyCache' : role === 'rival' || role === 'territorialGroup' ? 'shortcut' : 'routeReveal'
const factionFor = (role: SocialRole): SocialFaction => role === 'ritualist' || role === 'caretaker' ? 'kami' : 'trailfolk'

export const emptySocialReputation = (): SocialReputation => ({ trailfolk: 0, kami: 0 })
export const socialDispositionFor = (reputation: SocialReputation | undefined, faction: SocialFaction): SocialDisposition => (reputation?.[faction] ?? 0) >= 2 ? 'allied' : (reputation?.[faction] ?? 0) <= -2 ? 'hostile' : 'neutral'
export const adjustSocialReputation = (reputation: SocialReputation | undefined, faction: SocialFaction, amount: number): SocialReputation => ({ ...emptySocialReputation(), ...reputation, [faction]: (reputation?.[faction] ?? 0) + amount })

export const socialContractFor = (input: SocialContractInput): SocialContract | undefined => {
  const rng = rngFor(input.seed, 'generation', input.floorIndex, 'social-contract', input.biome, input.recipeId, input.arcId ?? 'legacy')
  if (rng.int(4) === 0) return undefined
  const role = rng.pick(roles)
  const faction = factionFor(role)
  return { id: `social:${input.floorIndex}:${role}`, faction, role, goal: goals[role], visibility: rng.int(3) === 0 ? 'rumored' : 'visible', offer: offerFor(role), consequence: role === 'rival' || role === 'territorialGroup' ? 'hostility' : role === 'trader' || role === 'strandedExplorer' ? 'alliance' : 'routeChange', disposition: 'neutral' }
}
