import type { ActionResult } from './engine/shared'
import type { AutoplayPartyOutcomes, Companion, RunState } from './types'

const active = (companion: Companion): boolean => companion.rosterStatus === 'active' && companion.injury === 'healthy' && !companion.permanentlyLost
export const autoplayActiveCompanionIds = (state: RunState): string[] => (state.companions ?? []).filter(active).map(companion => companion.id).sort()
export const autoplayDirectCompanionIds = (state: RunState): string[] => (state.companions ?? []).filter(companion => active(companion) && companion.controlMode === 'direct').map(companion => companion.id).sort()

export const createAutoplayPartyOutcomes = (state: RunState): AutoplayPartyOutcomes => {
  const activeCompanionIds = autoplayActiveCompanionIds(state)
  const controlMode = !activeCompanionIds.length ? 'none' : autoplayDirectCompanionIds(state).length ? 'direct' : 'autonomous'
  return { controlMode, activeCompanionIds, actions: {}, injuries: [], losses: [], intercepts: 0, traversalAssists: 0, blockedTurns: 0, finiteResourceConsents: 0, directModeRefused: false }
}

const add = (values: string[], id: string): void => { if (!values.includes(id)) values.push(id); values.sort() }
export const recordAutoplayPartyOutcome = (outcomes: AutoplayPartyOutcomes, before: RunState, after: RunState, events: ActionResult): void => {
  const previous = new Map((before.companions ?? []).map(companion => [companion.id, companion]))
  for (const companion of after.companions ?? []) {
    const prior = previous.get(companion.id)
    if (!prior) continue
    if (!prior.permanentlyLost && companion.permanentlyLost) add(outcomes.losses, companion.id)
    else if (prior.injury === 'healthy' && companion.injury !== 'healthy') add(outcomes.injuries, companion.id)
  }
  for (const event of events) {
    if (event.type !== 'companion' || !event.reason) continue
    const action = event.reason.split(':', 1)[0]!
    outcomes.actions[action] = (outcomes.actions[action] ?? 0) + 1
    if (action === 'intercept') outcomes.intercepts++
    if (action === 'traverse' || action === 'stabilizeTerrain' || action === 'stabilizeHazard') outcomes.traversalAssists++
    if (event.reason.includes('follow path is blocked') || event.reason.includes('no safe follow position')) outcomes.blockedTurns++
  }
}

export const autoplayPartyCandidateScore = (before: RunState, after: RunState): number => {
  const previous = new Map((before.companions ?? []).map(companion => [companion.id, companion]))
  let score = 0
  for (const companion of after.companions ?? []) {
    const prior = previous.get(companion.id)
    if (!prior) continue
    if (!prior.permanentlyLost && companion.permanentlyLost) { score -= 700; continue }
    if (prior.injury === 'healthy' && companion.injury !== 'healthy') score -= 300
    for (const [action, turns] of Object.entries(companion.abilityState.cooldowns)) if ((prior.abilityState.cooldowns[action] ?? 0) < turns) score += action === 'traverse' || action === 'stabilizeTerrain' || action === 'stabilizeHazard' ? 44 : 16
  }
  return score
}
