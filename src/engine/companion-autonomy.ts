import { DIRECTIONS, type Actor, type Companion, type CompanionActionCategory, type Point, type RunState, type TileKind } from '../types'
import { getTile, isPassable } from '../world'
import { addCondition } from './conditions'
import { isLegalCompanionRoleAction } from './companion-roles'
import { activeCompanionRoster, isCompanionActor } from './party'
import { log } from './shared'

export type AutonomousCompanionAction = CompanionActionCategory | 'follow' | 'wait'
export interface AutonomousCompanionCommand { companionId: string; action: AutonomousCompanionAction; rationale: string; target?: Point }

const hazards = new Set<TileKind>(['spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder', 'bramble', 'rubble', 'water', 'deepWater', 'lava', 'pit', 'brine', 'frostRime'])
const terrainBlockers = new Set<TileKind>(['rubble', 'bramble', 'boulder'])
const hazardDistance = (left: Point, right: Point): number => Math.max(Math.abs(left.x - right.x), Math.abs(left.y - right.y))
const pointCompare = (left: Point, right: Point): number => left.y - right.y || left.x - right.x
const companionIdForActor = (actor: Actor): string | undefined => actor.status?.find(status => status.startsWith('companion:'))?.slice('companion:'.length)
const visible = (state: RunState, point: Point): boolean => getTile(state.floor, point.x, point.y)?.visible === true
const visibleHazards = (state: RunState): Point[] => state.floor.tiles.flatMap((tile, index) => tile.visible && hazards.has(tile.kind) ? [{ x: index % state.floor.width, y: Math.floor(index / state.floor.width) }] : [])
const orderedByHero = (state: RunState, points: readonly Point[]): Point[] => [...points].sort((left, right) => hazardDistance(state.hero, left) - hazardDistance(state.hero, right) || pointCompare(left, right))
const cooled = (companion: Companion, action: CompanionActionCategory): boolean => (companion.abilityState.cooldowns[action] ?? 0) > 0
const usable = (companion: Companion, action: CompanionActionCategory): boolean => isLegalCompanionRoleAction(companion.role, action) && !cooled(companion, action)
const visibleHostiles = (state: RunState): Actor[] => state.floor.actors.filter(actor => actor.hostile && actor.health > 0 && visible(state, actor)).sort((left, right) => hazardDistance(state.hero, left) - hazardDistance(state.hero, right) || left.id.localeCompare(right.id))
const visibleSigns = (state: RunState): Point[] => orderedByHero(state, [
  ...state.floor.props.filter(prop => prop.state !== 'destroyed' && visible(state, prop)).map(prop => ({ x: prop.x, y: prop.y })),
  ...state.floor.milestones.filter(milestone => !milestone.claimed && visible(state, milestone)).map(milestone => ({ x: milestone.x, y: milestone.y }))
])
const visibleHazardsByHero = (state: RunState): Point[] => orderedByHero(state, visibleHazards(state))
const nearby = (state: RunState, point: Point, range: number): boolean => hazardDistance(state.hero, point) <= range

export const autonomousCompanionDecision = (state: RunState, companion: Companion, actor: Actor): AutonomousCompanionCommand => {
  if (companion.controlMode !== 'autonomous') return { companionId: companion.id, action: 'wait', rationale: 'direct control is active' }
  const hostiles = visibleHostiles(state)
  const hazard = visibleHazardsByHero(state)[0]
  if (companion.role === 'guard') {
    const threat = hostiles.find(hostile => nearby(state, hostile, 2))
    if (threat && state.hero.health * 2 <= state.hero.maxHealth && usable(companion, 'protect')) return { companionId: companion.id, action: 'protect', rationale: 'the courier is wounded under visible pressure', target: { x: threat.x, y: threat.y } }
    if (threat && usable(companion, 'intercept')) return { companionId: companion.id, action: 'intercept', rationale: 'a visible hostile threatens the courier', target: { x: threat.x, y: threat.y } }
  }
  if (companion.role === 'scout') {
    const sign = visibleSigns(state)[0]
    if (sign && usable(companion, 'observe')) return { companionId: companion.id, action: 'observe', rationale: 'a visible trail sign is unreviewed', target: sign }
    if (hazard && usable(companion, 'mark')) return { companionId: companion.id, action: 'mark', rationale: 'a visible hazard needs marking', target: hazard }
  }
  if (companion.role === 'pathmaker') {
    const heroTile = getTile(state.floor, state.hero.x, state.hero.y)
    if (heroTile?.visible && hazards.has(heroTile.kind) && usable(companion, 'traverse')) return { companionId: companion.id, action: 'traverse', rationale: 'the courier is crossing visible hazardous terrain', target: { x: state.hero.x, y: state.hero.y } }
    const terrain = visibleHazardsByHero(state).find(point => terrainBlockers.has(getTile(state.floor, point.x, point.y)!.kind) && nearby(state, point, 2))
    if (terrain && usable(companion, 'stabilizeTerrain')) return { companionId: companion.id, action: 'stabilizeTerrain', rationale: 'a visible route blocker is near the courier', target: terrain }
  }
  if (companion.role === 'ritualist') {
    const ritual = state.floor.tiles.flatMap((tile, index) => tile.kind === 'altar' && tile.visible && tile.explored ? [{ x: index % state.floor.width, y: Math.floor(index / state.floor.width) }] : [])[0]
    if (ritual && usable(companion, 'ward')) return { companionId: companion.id, action: 'ward', rationale: 'an explored ritual marker can shield the courier', target: ritual }
    if (hazard && nearby(state, hazard, 2) && usable(companion, 'stabilizeHazard')) return { companionId: companion.id, action: 'stabilizeHazard', rationale: 'a visible hazard threatens the route', target: hazard }
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
export const executeCompanionRoleAction = (state: RunState, companion: Companion, actor: Actor, action: CompanionActionCategory, target: Point | undefined, rationale: string): void => {
  if (!isLegalCompanionRoleAction(companion.role, action)) throw new Error(`illegal companion action ${action} for ${companion.role}`)
  companion.abilityState.cooldowns[action] = 2
  if (action === 'intercept') actor.status = [...(actor.status ?? []).filter(status => !status.startsWith('intercept:')), `intercept:${state.turn}`]
  if (action === 'protect' || action === 'intercept' || action === 'ward') addCondition(state.hero, { kind: 'shielded', duration: 1, potency: 1 })
  if ((action === 'stabilizeTerrain' || action === 'stabilizeHazard') && target) {
    const tile = getTile(state.floor, target.x, target.y)
    if (tile?.visible && hazards.has(tile.kind)) tile.kind = 'floor'
  }
  log(state, `${companion.name}: ${rationale}.`)
}
const execute = (state: RunState, companion: Companion, actor: Actor, command: AutonomousCompanionCommand): void => {
  if (command.action === 'follow') { if (command.target) { actor.x = command.target.x; actor.y = command.target.y }; return }
  if (command.action === 'wait') return
  executeCompanionRoleAction(state, companion, actor, command.action, command.target, command.rationale)
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
    execute(state, companion, actor, command)
    commands.push(command)
  }
  return commands
}
