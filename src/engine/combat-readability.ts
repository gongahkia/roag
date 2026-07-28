import { ACTIONS } from './actions'

export interface CombatReadabilityScenario { actionId: string; actor: string; terrain: string; seed: number; response: string; forecast: string; escape: string }

export const combatReadabilityScenarios: readonly CombatReadabilityScenario[] = [
  { actionId: 'enemy-shot', actor: 'fusewarden', terrain: 'cover', seed: 7, response: 'break line of sight', forecast: 'major telegraph', escape: 'cover' },
  { actionId: 'enemy-root', actor: 'vinebinder', terrain: 'bramble', seed: 11, response: 'move before root resolves', forecast: 'minor control telegraph', escape: 'clear adjacent tile' },
  { actionId: 'enemy-web', actor: 'webweaver', terrain: 'web', seed: 13, response: 'leave the marked tile', forecast: 'minor terrain telegraph', escape: 'clear route' },
  { actionId: 'enemy-fire', actor: 'cinderimp', terrain: 'gas', seed: 17, response: 'leave the fire line', forecast: 'minor terrain telegraph', escape: 'side route' },
  { actionId: 'enemy-pull', actor: 'crystalpuller', terrain: 'cover', seed: 19, response: 'break line of sight', forecast: 'minor control telegraph', escape: 'anchor route' },
  { actionId: 'enemy-ward', actor: 'wardacolyte', terrain: 'objective', seed: 23, response: 'interrupt support', forecast: 'shield forecast', escape: 'focus the support' },
  { actionId: 'enemy-lock', actor: 'lockkeeper', terrain: 'door', seed: 29, response: 'keep a key or alternate route', forecast: 'route-control forecast', escape: 'side route' },
  { actionId: 'enemy-dart', actor: 'dartadept', terrain: 'dart', seed: 31, response: 'leave the dart line', forecast: 'minor terrain telegraph', escape: 'side route' },
  { actionId: 'enemy-ritual', actor: 'ritualist', terrain: 'cover', seed: 37, response: 'move before marking resolves', forecast: 'minor control telegraph', escape: 'cover' },
  { actionId: 'foreman-cavein', actor: 'foreman', terrain: 'rail', seed: 41, response: 'leave the marked shelf', forecast: 'major terrain telegraph', escape: 'rail branch' },
  { actionId: 'heartwood-charge', actor: 'heartwood', terrain: 'bramble', seed: 43, response: 'keep a clear lane', forecast: 'major movement telegraph', escape: 'cleared trail' },
  { actionId: 'geode-fissure', actor: 'geode', terrain: 'gas', seed: 47, response: 'leave the fissure line', forecast: 'major terrain telegraph', escape: 'cool route' },
  { actionId: 'regent-ward', actor: 'regent', terrain: 'altar', seed: 53, response: 'break the ward', forecast: 'shield forecast', escape: 'focus the keeper' },
  { actionId: 'regent-decree', actor: 'regent', terrain: 'dart', seed: 59, response: 'move before decree resolves', forecast: 'major terrain telegraph', escape: 'side route' },
  { actionId: 'regent-judgment', actor: 'regent', terrain: 'darkness', seed: 61, response: 'keep a lit escape route', forecast: 'major terrain telegraph', escape: 'lit route' },
  { actionId: 'guardian-slam', actor: 'foreman', terrain: 'crumble', seed: 67, response: 'leave the cross', forecast: 'major area telegraph', escape: 'open lane' }
]

const specialActionIds = ACTIONS.filter(action => action.id.startsWith('enemy-') && !['enemy-strike', 'enemy-approach', 'enemy-reposition'].includes(action.id) || action.tags.includes('guardian')).map(action => action.id)

export const validateCombatReadability = (scenarios = combatReadabilityScenarios): string[] => {
  const byAction = new Map(scenarios.map(scenario => [scenario.actionId, scenario]))
  const errors = specialActionIds.filter(id => !byAction.has(id)).map(id => `action=${id} terrain=missing seed=missing counterplay=missing`)
  for (const scenario of scenarios) if (!scenario.actor || !scenario.terrain || !Number.isInteger(scenario.seed) || !scenario.response || !scenario.forecast || !scenario.escape) errors.push(`action=${scenario.actionId} actor=${scenario.actor || 'missing'} terrain=${scenario.terrain || 'missing'} seed=${scenario.seed} counterplay=${scenario.response || 'missing'}`)
  return errors
}
