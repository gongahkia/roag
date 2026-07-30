import { DIRECTIONS, type Actor, type Companion, type CompanionActionCategory, type Point, type RunState, type TileKind } from '../types'
import { getTile, isPassable } from '../world'
import { addCondition } from './conditions'
import { assessCompanionAbility, companionAbilityTarget } from './companion-abilities'
import { isLegalCompanionRoleAction } from './companion-roles'
import { activeCompanionRoster, isCompanionActor } from './party'
import { applyCompanionTerrainMutation, companionTerrainMutationAssessment, type CompanionTerrainAction } from './companion-traversal'
import { log } from './shared'

export type AutonomousCompanionAction = CompanionActionCategory | 'follow' | 'wait'
export interface AutonomousCompanionCommand { companionId: string; action: AutonomousCompanionAction; rationale: string; target?: Point; result?: string }
export interface CompanionRoleActionExecution { executed: boolean; target?: Point; targetLabel?: string; result: string }

const hazards = new Set<TileKind>(['spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder', 'bramble', 'rubble', 'water', 'deepWater', 'lava', 'pit', 'brine', 'frostRime'])
const hazardDistance = (left: Point, right: Point): number => Math.max(Math.abs(left.x - right.x), Math.abs(left.y - right.y))
const pointCompare = (left: Point, right: Point): number => left.y - right.y || left.x - right.x
const companionIdForActor = (actor: Actor): string | undefined => actor.status?.find(status => status.startsWith('companion:'))?.slice('companion:'.length)
const visible = (state: RunState, point: Point): boolean => getTile(state.floor, point.x, point.y)?.visible === true
const cooled = (companion: Companion, action: CompanionActionCategory): boolean => (companion.abilityState.cooldowns[action] ?? 0) > 0
const usable = (companion: Companion, action: CompanionActionCategory): boolean => isLegalCompanionRoleAction(companion.role, action) && !cooled(companion, action)
const visibleHostiles = (state: RunState): Actor[] => state.floor.actors.filter(actor => actor.hostile && actor.health > 0 && visible(state, actor)).sort((left, right) => hazardDistance(state.hero, left) - hazardDistance(state.hero, right) || left.id.localeCompare(right.id))
const nearby = (state: RunState, point: Point, range: number): boolean => hazardDistance(state.hero, point) <= range

export const autonomousCompanionDecision = (state: RunState, companion: Companion, actor: Actor): AutonomousCompanionCommand => {
  if (companion.controlMode !== 'autonomous') return { companionId: companion.id, action: 'wait', rationale: 'direct control is active' }
  const hostiles = visibleHostiles(state)
  const ready = (action: CompanionActionCategory, target = companionAbilityTarget(state, companion, actor, action)): boolean => Boolean(target && usable(companion, action) && assessCompanionAbility(state, companion, actor, action, target).ready)
  if (companion.role === 'guard') {
    const threat = hostiles.find(hostile => nearby(state, hostile, 2))
    const protect = companionAbilityTarget(state, companion, actor, 'protect')
    const intercept = companionAbilityTarget(state, companion, actor, 'intercept')
    if (threat && state.hero.health * 2 <= state.hero.maxHealth && ready('protect', protect)) return { companionId: companion.id, action: 'protect', rationale: 'the courier is wounded under visible pressure', target: protect }
    if (threat && ready('intercept', intercept)) return { companionId: companion.id, action: 'intercept', rationale: 'a visible hostile threatens the courier', target: intercept }
  }
  if (companion.role === 'scout') {
    const sign = companionAbilityTarget(state, companion, actor, 'observe')
    const hazard = companionAbilityTarget(state, companion, actor, 'mark')
    if (sign && ready('observe', sign)) return { companionId: companion.id, action: 'observe', rationale: 'a visible trail sign is unreviewed', target: sign }
    if (hazard && ready('mark', hazard)) return { companionId: companion.id, action: 'mark', rationale: 'a visible hazard needs marking', target: hazard }
  }
  if (companion.role === 'pathmaker') {
    const traverse = companionAbilityTarget(state, companion, actor, 'traverse')
    const terrain = companionAbilityTarget(state, companion, actor, 'stabilizeTerrain')
    if (traverse && ready('traverse', traverse)) return { companionId: companion.id, action: 'traverse', rationale: 'the courier is crossing visible hazardous terrain', target: traverse }
    if (terrain && ready('stabilizeTerrain', terrain)) return { companionId: companion.id, action: 'stabilizeTerrain', rationale: 'a visible route blocker is near the courier', target: terrain }
  }
  if (companion.role === 'ritualist') {
    const ritual = companionAbilityTarget(state, companion, actor, 'ward')
    const stabilizableHazard = companionAbilityTarget(state, companion, actor, 'stabilizeHazard')
    if (ritual && ready('ward', ritual)) return { companionId: companion.id, action: 'ward', rationale: 'an explored ritual marker can shield the courier', target: ritual }
    if (stabilizableHazard && ready('stabilizeHazard', stabilizableHazard)) return { companionId: companion.id, action: 'stabilizeHazard', rationale: 'a visible hazard threatens the route', target: stabilizableHazard }
  }
  return followDecision(state, companion, actor, hostiles)
}

const safeTile = (state: RunState, point: Point): boolean => {
  const tile = getTile(state.floor, point.x, point.y)
  return Boolean(tile && !hazards.has(tile.kind) && !(point.x === state.floor.exit.x && point.y === state.floor.exit.y) && isPassable(state.floor, point.x, point.y))
}
const followSlots = (state: RunState): Point[] => {
  const points: Point[] = []
  for (let y = state.hero.y - 2; y <= state.hero.y + 2; y++) for (let x = state.hero.x - 2; x <= state.hero.x + 2; x++) if (Math.max(Math.abs(x - state.hero.x), Math.abs(y - state.hero.y)) === 2) points.push({ x, y })
  return points.filter(point => safeTile(state, point)).sort(pointCompare)
}
const followDecision = (state: RunState, companion: Companion, actor: Actor, hostiles: readonly Actor[]): AutonomousCompanionCommand => {
  const pressure = hostiles.some(hostile => hazardDistance(actor, hostile) <= 1)
  const distance = hazardDistance(actor, state.hero)
  if (!pressure && distance <= 2) return { companionId: companion.id, action: 'wait', rationale: 'formation is clear' }
  const slots = followSlots(state)
  const target = slots.sort((left, right) => hazardDistance(actor, left) - hazardDistance(actor, right) || pointCompare(left, right))[0]
  if (!target) return { companionId: companion.id, action: 'wait', rationale: 'no safe follow position is visible' }
  const steps = Object.values(DIRECTIONS).filter(direction => direction.x || direction.y).map(direction => ({ x: actor.x + direction.x, y: actor.y + direction.y })).filter(point => safeTile(state, point))
  const step = steps.sort((left, right) => {
    const leftPressure = pressure ? Math.min(...hostiles.map(hostile => hazardDistance(left, hostile))) : 0
    const rightPressure = pressure ? Math.min(...hostiles.map(hostile => hazardDistance(right, hostile))) : 0
    return (pressure ? rightPressure - leftPressure : 0) || hazardDistance(left, target) - hazardDistance(right, target) || pointCompare(left, right)
  })[0]
  if (!step) return { companionId: companion.id, action: 'wait', rationale: 'the follow path is blocked' }
  return { companionId: companion.id, action: 'follow', rationale: pressure ? 'disengaging from visible pressure' : 'returning to the courier formation', target: step }
}

export const tickCompanionCooldowns = (companions: readonly Companion[]): void => {
  for (const companion of companions) for (const [action, turns] of Object.entries(companion.abilityState.cooldowns)) {
    if (turns <= 1) delete companion.abilityState.cooldowns[action]
    else companion.abilityState.cooldowns[action] = turns - 1
  }
}
export const executeCompanionRoleAction = (state: RunState, companion: Companion, actor: Actor, action: CompanionActionCategory, target: Point | undefined, rationale: string): CompanionRoleActionExecution => {
  if (!isLegalCompanionRoleAction(companion.role, action)) throw new Error(`illegal companion action ${action} for ${companion.role}`)
  const assessment = assessCompanionAbility(state, companion, actor, action, target)
  if (!assessment.ready) { log(state, `${companion.name} cannot ${action}: ${assessment.reason.replaceAll('-', ' ')}.`); return { executed: false, target: assessment.target, targetLabel: assessment.targetLabel, result: assessment.reason } }
  if (action === 'stabilizeTerrain' || action === 'stabilizeHazard') applyCompanionTerrainMutation(state, companionTerrainMutationAssessment(state, action as CompanionTerrainAction, assessment.target!))
  companion.abilityState.cooldowns[action] = 2
  if (action === 'intercept') actor.status = [...(actor.status ?? []).filter(status => !status.startsWith('intercept:')), `intercept:${state.turn}`]
  if (action === 'protect' || action === 'ward' || action === 'traverse') addCondition(state.hero, { kind: 'shielded', duration: 1, potency: 1 })
  if (action === 'observe') state.floor.milestones.filter(milestone => milestone.x === assessment.target!.x && milestone.y === assessment.target!.y).forEach(milestone => { milestone.discovered = true })
  if (action === 'mark') actor.status = [...(actor.status ?? []).filter(status => !status.startsWith('hazard-mark:')), `hazard-mark:${assessment.target!.x},${assessment.target!.y}`]
  log(state, `${companion.name} ${action} ${assessment.targetLabel}: ${assessment.effect} (${rationale}).`)
  return { executed: true, target: assessment.target, targetLabel: assessment.targetLabel, result: assessment.effect }
}
const execute = (state: RunState, companion: Companion, actor: Actor, command: AutonomousCompanionCommand): CompanionRoleActionExecution => {
  if (command.action === 'follow') { if (command.target) { actor.x = command.target.x; actor.y = command.target.y }; return { executed: true, target: command.target, result: 'formation adjusted' } }
  if (command.action === 'wait') return { executed: true, result: 'waited' }
  return executeCompanionRoleAction(state, companion, actor, command.action, command.target, command.rationale)
}

export const resolveAutonomousCompanions = (state: RunState): AutonomousCompanionCommand[] => {
  const companions = state.companions ?? []
  tickCompanionCooldowns(companions)
  const commands: AutonomousCompanionCommand[] = []
  for (const roster of activeCompanionRoster(companions)) {
    const companion = companions.find(candidate => candidate.id === roster.id)
    const actor = state.floor.actors.find(candidate => isCompanionActor(candidate) && companionIdForActor(candidate) === roster.id)
    if (!companion || !actor || companion.controlMode !== 'autonomous') continue
    const command = autonomousCompanionDecision(state, companion, actor)
    const execution = execute(state, companion, actor, command)
    if (execution.executed) { command.target = execution.target ?? command.target; command.result = execution.result; commands.push(command) }
  }
  return commands
}
