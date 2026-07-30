import { ITEM } from '../content'
import { DIRECTIONS, type Actor, type Biome, type Direction, type ItemId, type RunState, type Telegraph } from '../types'
import { actorAt, getTile, isPassable, preservesAdjacentExitAccess, preservesExitPath } from '../world'
import { gainXp, monsterXp } from './progression'
import { event, distance, equipmentDefense, log, turnRng, type ActionResult } from './shared'
import { announceTelegraph, resolveTelegraphs } from './telegraphs'
import { refreshFov } from './visibility'
import { addCondition, conditionSpeed, hasCondition, modifyIncomingDamage, tickConditions } from './conditions'
import { resolveTerrainReactions, type TerrainTag } from './terrain'
import { actionCells } from './geometry'
import { planEnemyIntent } from './intents'
import { projectBolt } from './projectiles'
import { advanceGuardianPhase } from './guardians'
import { completeObjective } from '../objectives'
import { evaluateEquipmentEffects } from './equipment'
import { canBreakRubble, canKnockback, strengthGuard, strengthMeleeBonus } from './strength'
import { resolveDisplacement } from './displacement'
import { agilityEvasion, agilityMoveDistance, agilityReachBonus, agilityTelegraphAvoidance } from './agility'
import { vitalityHazardReduction, vitalityShield } from './vitality'
import { intellectFocusRecovery } from './intellect'
import { announceSynergies, resolveSynergies } from './synergies'
import { applyPropEffects, expirePropEffects, resolveMonolithTelegraphs } from './props'
import { trailcraftTags } from './trailcraft'
import { boonRank, expireAshways, recordSafePosition } from './buildcraft'
import { recordTelemetryCount, recordTelemetryKill } from '../telemetry'
import { armRelicIce, armRelicMirror, armRelicMove, armRelicWaterCrossing, consumeRelicPrismStrike, consumeRelicWinterGuard, markbreakerDamage, resolveKillRelics } from './relics'
import { markCurseDamaged } from './curses'
import { grantGold } from './economy'
import { advanceEcology } from '../ecology'
import { activeCompanionRoster, isCompanionActor } from './party'
import { resolveAutonomousCompanions, tickCompanionCooldowns } from './companion-autonomy'

export function moveHero(state: RunState, direction: Direction): ActionResult {
  const delta = DIRECTIONS[direction]
  if (direction === 'wait') return advance(state, [event('move')])
  if (hasCondition(state.hero, 'rooted')) { log(state, 'Roots hold you in place.'); return advance(state, [event('danger')]) }
  const windLimited = getTile(state.floor, state.hero.x, state.hero.y)?.flow?.hazard === 'squall'
  const x = state.hero.x + delta.x
  const y = state.hero.y + delta.y
  const weapon = state.hero.equipment.mainHand ? ITEM[state.hero.equipment.mainHand] : undefined
  const profile = weapon?.weapon ?? { damage: 2, reach: 1, shape: 'adjacent' as const, cooldown: 0, tags: ['unarmed'] }
  const modified = evaluateEquipmentEffects(state.hero, 'action', { actionId: 'player-strike' }, { damage: profile.damage, range: profile.reach + agilityReachBonus(state.hero), cooldown: profile.cooldown }).values
  const tags = [...trailcraftTags(state.hero).filter(id => id === 'flintTemper' || id === 'windKnot' || id === 'sunstride' || id === 'prismLedger' || id === 'rimeEdge' || id === 'iceNerve'), ...Object.keys(state.hero.boons ?? {}).filter(id => boonRank(state, id) > 0).map(id => `boon:${id}`)]
  const items = Object.values(state.hero.equipment).filter((id): id is string => Boolean(id))
  const synergy = resolveSynergies({ items, skills: state.hero.skills, tags }, { range: Math.max(1, Math.floor(modified.range ?? profile.reach)) })
  const targets = actionCells(profile.shape, state.hero, direction, Math.max(1, Math.floor(synergy.values.range ?? profile.reach) - (windLimited ? 1 : 0))).map(point => actorAt(state.floor, point.x, point.y)).filter((target): target is Actor => Boolean(target?.hostile))
  if (targets.length) { announceSynergies(state, synergy); return heroAttack(state, targets, weapon?.id, Math.max(1, Math.floor(synergy.values.damage ?? modified.damage ?? profile.damage)), Math.max(0, Math.floor(modified.cooldown ?? profile.cooldown))) }
  let tile = getTile(state.floor, x, y)
  if (!tile) return []
  if (tile.kind === 'door') { tile.kind = 'floor'; log(state, 'You open the door.'); return advance(state, [event('move')]) }
  if (tile.kind === 'lockedDoor') {
    if (state.hero.keys < 1) { log(state, 'A key is required.'); return [] }
    state.hero.keys--
    tile.kind = 'floor'
    log(state, 'You unlock the door.')
    return advance(state, [event('move')])
  }
  if (tile.kind === 'bramble' && weapon?.weapon?.tags.includes('cleave')) { tile.kind = 'floor'; state.hero.x = x; state.hero.y = y; log(state, 'You cut through the bramble.'); return advance(state, [event('move')]) }
  if (tile.kind === 'rubble' && canBreakRubble(state.hero)) { tile.kind = 'floor'; state.hero.x = x; state.hero.y = y; log(state, 'You break through the rubble.'); return advance(state, [event('boom'), event('move')]) }
  if (tile.kind === 'breakwall' && (weapon?.weapon?.tags.includes('breakwall') || weapon?.weapon?.tags.includes('hammer') || state.hero.bombs > 0)) { tile.kind = 'floor'; state.hero.x = x; state.hero.y = y; log(state, 'You open the scored breakwall.'); return advance(state, [event('boom'), event('move')]) }
  const occupant = actorAt(state.floor, x, y)
  if (occupant && isCompanionActor(occupant)) { log(state, `${occupant.name} occupies that tile.`); return [] }
  if (!isPassable(state.floor, x, y)) { log(state, 'The way is blocked.'); return [] }
  let destination = { x, y }
  for (let step = 1; ['floor', 'spiritPath', 'saltMirror', 'ice'].includes(tile.kind) && step < agilityMoveDistance(state.hero); step++) {
    const next = { x: destination.x + delta.x, y: destination.y + delta.y }
    const nextTile = getTile(state.floor, next.x, next.y)
    if (!nextTile || !['floor', 'spiritPath', 'saltMirror', 'ice'].includes(nextTile.kind) || !isPassable(state.floor, next.x, next.y)) break
    destination = next
    tile = nextTile
  }
  state.hero.x = destination.x
  state.hero.y = destination.y
  const events: ActionResult = [event('move')]
  for (const mirage of state.floor.saltMirages ?? []) if (!mirage.revealed && Math.max(Math.abs(state.hero.x - mirage.marker.x), Math.abs(state.hero.y - mirage.marker.y)) <= 3) {
    mirage.revealed = true
    mirage.cells.forEach(point => { const falseHorizon = getTile(state.floor, point.x, point.y); if (falseHorizon?.kind === 'saltMirror') falseHorizon.kind = 'brine' })
    log(state, 'The false horizon ripples away into a brine basin.')
  }
  if (tile.kind === 'spikes' || tile.kind === 'dart' || tile.kind === 'fireVent') events.push(...damageHero(state, tile.kind === 'spikes' ? 3 : 4, 'a trap', true))
  if (tile.kind === 'lava') events.push(...damageHero(state, 8, 'lava', true))
  if (tile.kind === 'gas') events.push(...damageHero(state, 2, 'poison gas', true))
  if (tile.kind === 'smoke') {
    const damage = Math.max(0, 1 - boonRank(state, 'smokeWalker') - boonRank(state, 'slagSkin'))
    if (damage) events.push(...damageHero(state, damage, 'choking smoke', true))
    if (boonRank(state, 'smokeWalker')) for (const nearby of state.floor.tiles) if (!nearby.explored) { nearby.explored = true; break }
  }
  if (tile.kind === 'saltMirror') {
    armRelicMirror(state)
    state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + boonRank(state, 'sunstep'))
    for (let i = 0; i < boonRank(state, 'mirageMap'); i++) {
      const unseen = state.floor.tiles.find(candidate => !candidate.explored)
      if (unseen) unseen.explored = true
    }
  }
  if (tile.kind === 'brine') {
    const damage = Math.max(0, 2 - boonRank(state, 'brineWard'))
    if (damage) events.push(...damageHero(state, damage, 'cutting brine', true))
    if (boonRank(state, 'whiteRoad')) state.hero.conditions = [...(state.hero.conditions ?? []), { kind: 'shielded', duration: 2, potency: boonRank(state, 'whiteRoad') }]
    if ((state.hero.trailcrafts?.brineGrit ?? 0) > 0) state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 1)
  }
  if (tile.kind === 'ice') {
    armRelicIce(state)
    const coldRead = boonRank(state, 'coldRead')
    state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + coldRead)
    if (coldRead) for (const actor of state.floor.actors.filter(actor => actor.hostile && Math.max(Math.abs(actor.x - state.hero.x), Math.abs(actor.y - state.hero.y)) <= 2)) getTile(state.floor, actor.x, actor.y)!.explored = true
    const rimeGuard = boonRank(state, 'rimeGuard') + ((state.hero.trailcrafts?.winterVow ?? 0) > 0 ? 1 : 0)
    if (rimeGuard) state.hero.conditions = [...(state.hero.conditions ?? []), { kind: 'shielded', duration: 2, potency: rimeGuard }]
  }
  if (tile.kind === 'frostRime') {
    const damage = Math.max(0, 2 - boonRank(state, 'thawStep'))
    if (damage) events.push(...damageHero(state, damage, 'biting rime', true))
    addCondition(state.hero, { kind: 'slowed', duration: 1, potency: 1 })
  }
  if (tile.kind === 'current') {
    armRelicWaterCrossing(state)
    state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + boonRank(state, 'currentSense'))
  }
  if (tile.kind === 'lift') { state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 1 + boonRank(state, 'updraftStep')); log(state, 'The lift raises your momentum.') }
  if (tile.kind === 'ledge') state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + boonRank(state, 'updraftCadence'))
  if (tile.kind === 'graveSoil' || tile.kind === 'spiritPath') state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + boonRank(state, 'burialCurrent'))
  if (tile.kind === 'graveSoil') log(state, 'The grave soil slows your pace.')
  if (tile.kind === 'spiritPath') state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 1)
  if (tile.kind === 'anchor') { state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + boonRank(state, 'anchorHabit')); log(state, 'The anchor steadies your route.') }
  if (tile.kind === 'crumble') {
    if (preservesAdjacentExitAccess(state.floor, destination, 'pit')) { tile.kind = 'pit'; log(state, 'The floor crumbles into a pit.'); events.push(event('danger')) }
    else { tile.kind = 'floor'; log(state, 'The floor holds before it can seal the trail.') }
  }
  if (tile.kind === 'boulder') { tile.kind = 'floor'; events.push(...damageHero(state, 6, 'a rolling boulder', true)) }
  if (['water', 'current'].includes(tile.kind)) armRelicWaterCrossing(state)
  armRelicMove(state)
  return advance(state, events)
}

export function advance(state: RunState, events: ActionResult): ActionResult {
  state.turn++
  advanceEcology(state, events)
  expirePropEffects(state)
  expireAshways(state)
  refreshFov(state)
  const directCompanions = activeCompanionRoster(state.companions ?? []).filter(companion => companion.controlMode === 'direct')
  if (directCompanions.length) {
    tickCompanionCooldowns(state.companions ?? [])
    state.modal = { kind: 'companionCommand', companionIds: directCompanions.map(companion => companion.id), index: 0 }
    log(state, `${directCompanions[0]!.name} awaits a command (1-2 action · direction move · Enter wait).`)
    return events
  }
  for (const command of resolveAutonomousCompanions(state)) events.push(event('companion', command.companionId, command.action))
  return resolveTurnAfterParty(state, events)
}

export const resolveTurnAfterParty = (state: RunState, events: ActionResult): ActionResult => {
  const resolvedTelegraphs = resolveMonolithTelegraphs(state, revalidateProjectileTelegraphs(state, resolveTelegraphs(state)))
  for (const telegraph of resolvedTelegraphs) {
    const propEffects = telegraph.actionId === 'enemy-fire' ? ['fire', 'hazard'] as const : telegraph.actionId === 'enemy-root' ? ['root', 'hazard'] as const : telegraph.actionId === 'enemy-pull' ? ['force', 'hazard'] as const : ['hazard'] as const
    applyPropEffects(state, telegraph.cells, propEffects)
    if (telegraph.actionId !== 'enemy-shot' && telegraph.actionId !== 'guardian-slam' && telegraph.actionId !== 'enemy-root' && telegraph.actionId !== 'enemy-web' && telegraph.actionId !== 'enemy-fire' && telegraph.actionId !== 'enemy-pull' && telegraph.actionId !== 'enemy-dart' && telegraph.actionId !== 'enemy-ritual' && telegraph.actionId !== 'foreman-cavein' && telegraph.actionId !== 'heartwood-charge' && telegraph.actionId !== 'geode-fissure' && telegraph.actionId !== 'regent-decree' && telegraph.actionId !== 'regent-judgment') continue
    const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)
    const hit = telegraph.cells.some(cell => cell.x === state.hero.x && cell.y === state.hero.y)
    const avoidance = agilityTelegraphAvoidance(state.hero)
    const dodged = Boolean(avoidance && turnRng(state, 'combat', `telegraph-dodge:${telegraph.id}`).chance(avoidance))
    if (telegraph.actionId === 'enemy-fire') {
      const tile = telegraph.cells[0] ? getTile(state.floor, telegraph.cells[0].x, telegraph.cells[0].y) : undefined
      if (tile?.kind === 'floor') tile.kind = 'fireVent'
      if (source?.hostile && hit && !dodged) events.push(...monsterAttack(state, source, 1))
      else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
      else log(state, 'The fire line scorches empty ground.')
      continue
    }
    if (telegraph.actionId === 'enemy-dart') {
      const tile = telegraph.cells[0] ? getTile(state.floor, telegraph.cells[0].x, telegraph.cells[0].y) : undefined
      if (tile?.kind === 'floor') tile.kind = 'dart'
      if (source?.hostile && hit && !dodged) events.push(...monsterAttack(state, source, 1))
      else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
      else log(state, 'The dart line arms on empty ground.')
      continue
    }
    if (telegraph.actionId === 'enemy-ritual') {
      if (source?.hostile && hit && !dodged) { addCondition(state.hero, { kind: 'marked', duration: 2, potency: 1 }); log(state, 'Ritual sigils mark you.') }
      else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
      else log(state, 'The ritual fades harmlessly.')
      continue
    }
    if (telegraph.actionId === 'foreman-cavein') {
      const tile = telegraph.cells[0] ? getTile(state.floor, telegraph.cells[0].x, telegraph.cells[0].y) : undefined
      if (tile?.kind === 'floor') tile.kind = 'crumble'
      if (source?.hostile && hit && !dodged) events.push(...monsterAttack(state, source, 2))
      else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
      else log(state, 'The cave-in shatters empty ground.')
      continue
    }
    if (telegraph.actionId === 'heartwood-charge') {
      const tile = telegraph.cells[0] ? getTile(state.floor, telegraph.cells[0].x, telegraph.cells[0].y) : undefined
      if (source?.hostile && hit && !dodged) { resolveDisplacement(state, source, state.hero, 'knockback'); log(state, 'The Heartwood Stag drives you through the thorns.') }
      else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
      else log(state, 'The charge tears through empty brush.')
      if (tile?.kind === 'floor' && telegraph.cells[0] && preservesExitPath(state.floor, state.hero, telegraph.cells[0], 'bramble')) tile.kind = 'bramble'
      continue
    }
    if (telegraph.actionId === 'geode-fissure') {
      for (const cell of telegraph.cells) {
        const tile = getTile(state.floor, cell.x, cell.y)
        if (tile?.kind === 'floor') tile.kind = 'fireVent'
      }
      if (source?.hostile && hit && !dodged) events.push(...monsterAttack(state, source, 2))
      else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
      else log(state, 'The fissure line seals the passage with flame.')
      continue
    }
    if (telegraph.actionId === 'regent-decree') {
      const tile = telegraph.cells[0] ? getTile(state.floor, telegraph.cells[0].x, telegraph.cells[0].y) : undefined
      if (tile?.kind === 'floor') tile.kind = 'dart'
      if (source?.hostile && hit && !dodged) { addCondition(state.hero, { kind: 'marked', duration: 2, potency: 2 }); log(state, 'Ash sigils mark you.') }
      else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
      else log(state, 'The ash decree marks empty ground.')
      continue
    }
    if (telegraph.actionId === 'regent-judgment') {
      for (const cell of telegraph.cells) {
        const tile = getTile(state.floor, cell.x, cell.y)
        if (tile?.kind === 'floor') tile.kind = 'fireVent'
      }
      if (source?.hostile && hit && !dodged) events.push(...monsterAttack(state, source, 3))
      else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
      else log(state, 'The final judgment burns an empty line.')
      continue
    }
    if (source?.hostile && hit && !dodged && telegraph.actionId === 'enemy-pull') { resolveDisplacement(state, source, state.hero, 'pull'); log(state, 'Crystal force drags you closer.') }
    else if (source?.hostile && hit && !dodged && telegraph.actionId === 'enemy-root') { addCondition(state.hero, { kind: 'rooted', duration: 2, potency: 1 }); log(state, 'Vines root you in place.') }
    else if (source?.hostile && hit && !dodged && telegraph.actionId === 'enemy-web') { const tile = getTile(state.floor, state.hero.x, state.hero.y); if (tile?.kind === 'floor') tile.kind = 'web'; addCondition(state.hero, { kind: 'slowed', duration: 2, potency: 1 }); log(state, 'Webs slow your escape.') }
    else if (source?.hostile && hit && !dodged) events.push(...monsterAttack(state, source, telegraph.actionId === 'guardian-slam' ? 2 : 1))
    else if (source?.hostile && hit) log(state, 'You evade the telegraphed attack.')
    else log(state, `${telegraph.actionId === 'guardian-slam' ? 'The slam' : telegraph.actionId === 'enemy-root' ? 'The roots' : telegraph.actionId === 'enemy-web' ? 'The web' : 'The bolt'} passes harmlessly.`)
  }
  if (resolvedTelegraphs.length) events.push(event('danger'))
  for (const actor of [...state.floor.actors]) {
    if (!actor.hostile || actor.health <= 0) continue
    const terrain = getTile(state.floor, actor.x, actor.y)?.kind
    const terrainMomentum = (actor.kind === 'railguard' || actor.kind === 'foreman') && terrain === 'rail' ? 50
      : actor.kind === 'marshskater' && terrain === 'water' ? 50
        : actor.kind === 'fumeeel' && terrain === 'gas' ? 50
          : (actor.kind === 'saltRaider' || actor.kind === 'mirageSkirmisher') && terrain === 'saltMirror' ? 45
            : actor.kind === 'shardHound' && terrain === 'ice' ? 40 : 0
    actor.energy += conditionSpeed(actor, actor.speed) + terrainMomentum
    while (actor.energy >= 100 && state.status === 'playing') {
      actor.energy -= 100
      events.push(...actorTurn(state, actor))
    }
  }
  resolveFlows(state, events)
  tickEnvironment(state, events)
  tickConditionEffects(state, events)
  for (const [id, cooldown] of Object.entries(state.hero.cooldowns ?? {})) {
    if (cooldown <= 1) delete state.hero.cooldowns![id]
    else state.hero.cooldowns![id] = cooldown - 1
  }
  refreshFov(state)
  recordSafePosition(state)
  return events
}

const resolveFlows = (state: RunState, events: ActionResult): void => {
  const push = (target: { x: number; y: number; health?: number; hostile?: boolean; name?: string }, isHero: boolean): void => {
    const tile = getTile(state.floor, target.x, target.y)
    const squall = tile?.kind === 'ledge' && tile.flow?.hazard === 'squall'
    if ((!squall && tile?.kind !== 'current') || !tile?.flow) return
    const braced = isHero && (state.hero.traversalTools?.includes('cordAnchor') ?? false) && Object.values(DIRECTIONS).some(delta => ['anchor', 'rope'].includes(getTile(state.floor, target.x + delta.x, target.y + delta.y)?.kind ?? 'wall'))
    if (braced) { log(state, 'Your cord holds at the rope anchor.'); return }
    const delta = DIRECTIONS[tile.flow.direction]
    const destination = { x: target.x + delta.x, y: target.y + delta.y }
    const downstream = getTile(state.floor, destination.x, destination.y)
    const blocked = !downstream || !isPassable(state.floor, destination.x, destination.y) || (!isHero && destination.x === state.hero.x && destination.y === state.hero.y)
    if (!blocked) {
      target.x = destination.x
      target.y = destination.y
      log(state, squall ? isHero ? 'The squall drives you along the ledge.' : `${target.name} is driven along the ledge.` : isHero ? 'The current carries you downstream.' : `${target.name} is carried downstream.`)
    }
    if (!squall && (tile.flow.hazard === 'undertow' || downstream?.kind === 'deepWater' || downstream?.kind === 'brine')) {
      if (isHero) events.push(...damageHero(state, Math.max(1, 4 - boonRank(state, 'tideSkin')), 'the undertow', true))
      else if (target.health !== undefined) target.health -= 3
      if (isHero) log(state, 'The marked undertow drags at your footing.')
    }
  }
  push(state.hero, true)
  for (const actor of state.floor.actors.filter(actor => actor.hostile && actor.health > 0)) push(actor, false)
  resolveDefeatedActors(state)
}

const revalidateProjectileTelegraphs = (state: RunState, telegraphs: Telegraph[]): Telegraph[] => telegraphs.flatMap(telegraph => {
  if (telegraph.actionId !== 'enemy-shot' || telegraph.collision?.by !== 'target') return [telegraph]
  const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)
  if (!source?.hostile || source.health <= 0) {
    log(state, 'The abandoned shot fizzles.')
    return []
  }
  const bolt = projectBolt(state.floor, source, telegraph.collision.point)
  if (bolt.collision?.by !== 'target') {
    log(state, `The shot is stopped by ${bolt.collision?.by ?? 'cover'}.`)
    return []
  }
  return [{ ...telegraph, cells: bolt.cells, collision: bolt.collision, cover: false }]
})

export function damageHero(state: RunState, amount: number, source: string, hazard = false): ActionResult {
  const winterGuard = consumeRelicWinterGuard(state) ? 3 : 0
  amount = Math.max(1, modifyIncomingDamage(state.hero, amount) - strengthGuard(state.hero) - vitalityShield(state.hero) - boonRank(state, 'bridgeOfNames') * (state.hero.oaths?.length ?? 0) - (hazard ? vitalityHazardReduction(state.hero) : 0) - winterGuard)
  if (winterGuard) log(state, 'Winter Seal absorbs part of the blow.')
  state.hero.health -= amount
  markCurseDamaged(state)
  log(state, `${source} harms you for ${amount}.`)
  if (state.hero.health > 0) return [event('hurt')]
  state.hero.health = 0
  state.status = 'dead'
  state.modal = undefined
  recordTelemetryCount(state, 'deathCauses', `${state.floor.biome}:${state.floor.layoutId}:${source}`)
  log(state, 'Your warning falls with you.')
  return [event('death')]
}

const interceptEnemyDamage = (state: RunState, source: Actor, damage: number): number => {
  const guard = state.floor.actors.filter(actor => isCompanionActor(actor) && actor.status?.includes(`intercept:${state.turn}`) && Math.max(Math.abs(actor.x - state.hero.x), Math.abs(actor.y - state.hero.y)) <= 1).sort((left, right) => left.id.localeCompare(right.id))[0]
  if (!guard) return damage
  const companionId = guard.status?.find(status => status.startsWith('companion:'))?.slice('companion:'.length)
  const companion = state.companions?.find(candidate => candidate.id === companionId && candidate.role === 'guard' && candidate.rosterStatus === 'active' && candidate.injury === 'healthy' && !candidate.permanentlyLost)
  if (!companion) return damage
  const transferred = Math.min(Math.ceil(damage / 2), guard.health)
  if (!transferred) return damage
  guard.health -= transferred
  guard.status = guard.status?.filter(status => !status.startsWith('intercept:'))
  const remaining = damage - transferred
  log(state, `${guard.name} intercepts ${source.name}: absorbs ${transferred}, courier takes ${remaining}.`)
  if (guard.health <= 0) {
    companion.injury = 'injured'
    companion.rosterStatus = 'benched'
    state.floor.actors = state.floor.actors.filter(actor => actor !== guard)
    log(state, `${guard.name} withdraws injured to the Lodge.`)
  }
  return remaining
}

export function explode(state: RunState, x: number, y: number, damage: number, tags: TerrainTag[] = ['bomb'], source = 'your bomb', radius = 1): void {
  const points = [] as Array<{ x: number; y: number }>
  for (let dy = -radius; dy <= radius; dy++) for (let dx = -radius; dx <= radius; dx++) {
    const tx = x + dx
    const ty = y + dy
    points.push({ x: tx, y: ty })
    const tile = getTile(state.floor, tx, ty)
    if (tile && tile.kind === 'wall' && tx > 0 && tx < state.floor.width - 1 && ty > 0 && ty < state.floor.height - 1) tile.kind = 'floor'
    const actor = actorAt(state.floor, tx, ty)
    if (actor?.hostile) actor.health -= modifyIncomingDamage(actor, damage)
    if (state.hero.x === tx && state.hero.y === ty) damageHero(state, Math.max(1, Math.floor(damage / 3)), source)
  }
  for (const effect of resolveTerrainReactions(state.floor, points, tags)) log(state, `Terrain reaction: ${effect.reaction}.`)
  applyPropEffects(state, points, tags.includes('bomb') ? tags.includes('fire') ? ['bomb', 'fire'] : ['bomb'] : tags.includes('fire') ? ['fire'] : [])
  resolveDefeatedActors(state)
  log(state, 'The blast tears through the stone.')
}

export function resolveDefeatedActors(state: RunState): void {
  for (const actor of state.floor.actors.filter(actor => actor.health <= 0)) {
    if (actor.hostile) recordTelemetryKill(state, actor.kind)
    log(state, `${actor.name} falls.`)
    dropLoot(state, actor)
    if (actor.role === 'guardian') {
      state.floor.guardianDefeated = true
      if (!state.floor.milestones.some(milestone => milestone.kind === 'relic')) state.floor.milestones.push({ id: `relic:${state.floor.index}:${actor.id}`, kind: 'relic', x: actor.x, y: actor.y, discovered: true, claimed: false })
      if (completeObjective(state, 'defeatGuardian')) log(state, 'Objective complete: guardian passed.')
      log(state, 'The way to the exit is open; a guardian echo remains.')
    }
    gainXp(state, monsterXp(actor.kind))
    if (actor.hostile) {
      const cash = boonRank(state, 'graveLedger') * 3 + boonRank(state, 'heatDebt') * 14 + (actor.status?.includes('elite') ? boonRank(state, 'riftLedger') * 15 + boonRank(state, 'iceLedger') * 10 : 0)
      if (cash) grantGold(state, cash)
      const healing = boonRank(state, 'boneOrchard') + (actor.role === 'guardian' ? boonRank(state, 'heirloomCircuit') * 4 : 0)
      if (healing) state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + healing)
      const focus = boonRank(state, 'ancestorLantern') + (actor.role === 'guardian' ? boonRank(state, 'heirloomCircuit') * 4 : 0)
      if (focus) state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + focus)
      resolveKillRelics(state)
    }
  }
  state.floor.actors = state.floor.actors.filter(actor => actor.health > 0)
}

function heroAttack(state: RunState, targets: Actor[], weaponId: string | undefined, baseDamage: number, cooldown: number): ActionResult {
  if (weaponId && (state.hero.cooldowns?.[weaponId] ?? 0) > 0) { log(state, 'Your weapon is recovering.'); return [] }
  for (const target of targets) {
    const rng = turnRng(state, 'combat', `hero:${target.id}`)
    if (rng.int(1, 20) + state.hero.stats.strength + state.hero.level < target.defense) { log(state, `Your attack misses ${target.name}.`); continue }
    const stormwake = boonRank(state, 'stormwake')
    const marked = target.conditions?.some(condition => condition.kind === 'marked') ? boonRank(state, 'gravewind') : 0
    const lowHealth = state.hero.health * 4 <= state.hero.maxHealth ? boonRank(state, 'lastRites') * 2 + boonRank(state, 'lastWinter') * 3 : 0
    const mirrorDamage = getTile(state.floor, state.hero.x, state.hero.y)?.kind === 'saltMirror' ? boonRank(state, 'mirrorHunt') : 0
    const markedDamage = target.conditions?.some(condition => condition.kind === 'marked') ? boonRank(state, 'glassEdge') : 0
    const frozenDamage = target.conditions?.some(condition => condition.kind === 'marked' || condition.kind === 'slowed') ? boonRank(state, 'shatterMark') : 0
    const duelDamage = target.role === 'guardian' || target.status?.includes('elite') ? boonRank(state, 'duelistOath') * 2 : 0
    const damage = modifyIncomingDamage(target, Math.max(1, baseDamage + markbreakerDamage(state, target) + consumeRelicPrismStrike(state, target) + state.hero.stats.strength + strengthMeleeBonus(state.hero) + stormwake + marked + lowHealth + mirrorDamage + markedDamage + frozenDamage + duelDamage + rng.int(0, 3) - Math.floor(target.defense / 8)))
    target.health -= damage
    log(state, `You strike ${target.name} for ${damage}.`)
    if (target.health > 0 && canKnockback(state.hero) && resolveDisplacement(state, state.hero, target, 'knockback').moved) addCondition(target, { kind: 'staggered', duration: 1, potency: 1 })
  }
  resolveDefeatedActors(state)
  const events = advance(state, [event('hit')])
  if (weaponId && cooldown) (state.hero.cooldowns ??= {})[weaponId] = cooldown
  return events
}

function actorTurn(state: RunState, actor: Actor): ActionResult {
  if (hasCondition(actor, 'staggered')) return []
  advanceGuardianPhase(state, actor)
  const intent = planEnemyIntent(state, actor)
  log(state, `${actor.name}: ${intent.action.name} (${intent.reason}).`)
  if (hasCondition(actor, 'rooted') && (intent.action.id === 'enemy-approach' || intent.action.id === 'enemy-reposition')) { log(state, `${actor.name} is rooted.`); return [] }
  if (intent.action.id === 'enemy-strike') return monsterAttack(state, actor)
  if (intent.action.id === 'enemy-shot') return announceProjectile(state, actor)
  if (intent.action.id === 'enemy-root') return announceWildsSnare(state, actor, 'enemy-root', `${actor.name} marks a rooting line.`)
  if (intent.action.id === 'enemy-web') return announceWildsSnare(state, actor, 'enemy-web', `${actor.name} marks a snare line.`)
  if (intent.action.id === 'enemy-fire') return announceCavernLine(state, actor, 'enemy-fire', `${actor.name} marks a fire line.`)
  if (intent.action.id === 'enemy-pull') return announceCavernLine(state, actor, 'enemy-pull', `${actor.name} marks a pull line.`)
  if (intent.action.id === 'enemy-ward') {
    const leader = actor.encounter ? state.floor.actors.find(other => {
      const encounter = other.encounter
      return encounter?.id === actor.encounter!.id && encounter.leader && other.health > 0
    }) : undefined
    const target = leader ?? actor
    addCondition(target, { kind: 'shielded', duration: 3, potency: 2 })
    log(state, leader && leader.id !== actor.id ? `${actor.name} wards ${leader.name}.` : `${actor.name} raises a shield.`)
    return [event('danger')]
  }
  if (intent.action.id === 'enemy-lock') return sealNearbyDoor(state, actor)
  if (intent.action.id === 'enemy-dart') return announceRuinsLine(state, actor, 'enemy-dart', `${actor.name} marks a dart line.`)
  if (intent.action.id === 'enemy-ritual') return announceRuinsLine(state, actor, 'enemy-ritual', `${actor.name} begins a marking ritual.`)
  if (intent.action.id === 'foreman-cavein') return announceForemanCavein(state, actor)
  if (intent.action.id === 'heartwood-charge') return announceHeartwoodCharge(state, actor)
  if (intent.action.id === 'geode-fissure') return announceGeodeFissure(state, actor)
  if (intent.action.id === 'regent-ward') { addCondition(actor, { kind: 'shielded', duration: 3, potency: 3 }); log(state, 'The Stone Keeper raises a spirit ward.'); return [event('danger')] }
  if (intent.action.id === 'regent-decree') return announceRegentDecree(state, actor)
  if (intent.action.id === 'regent-judgment') return announceRegentJudgment(state, actor)
  if (intent.action.id === 'guardian-slam') return announceGuardianSlam(state, actor)
  const candidates = Object.values(DIRECTIONS).filter(delta => delta.x || delta.y).map(delta => ({ x: actor.x + delta.x, y: actor.y + delta.y }))
  const valid = candidates.filter(point => isPassable(state.floor, point.x, point.y) && !(point.x === state.hero.x && point.y === state.hero.y))
  if (!valid.length) return []
  valid.sort((a, b) => intent.action.id === 'enemy-reposition' ? distance(b, state.hero) - distance(a, state.hero) : distance(a, state.hero) - distance(b, state.hero))
  const rng = turnRng(state, 'combat', `move:${actor.id}`)
  const next = actor.ai === 'wander' && rng.chance(45) ? rng.pick(valid) : valid[0]
  actor.x = next.x
  actor.y = next.y
  return []
}

function announceProjectile(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:shot`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  if (actor.kind === 'fusewarden') log(state, 'The Fuse Warden primes a blast line; find cover.')
  const bolt = projectBolt(state.floor, actor, state.hero)
  if (bolt.cover || !bolt.collision) { log(state, `${actor.name}'s shot is blocked by cover.`); return [] }
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'enemy-shot', cells: bolt.cells, danger: 'major', windup: 1, collision: { point: bolt.collision.point, by: bolt.collision.by }, cover: bolt.cover })
  return [event('danger')]
}

function announceWildsSnare(state: RunState, actor: Actor, actionId: 'enemy-root' | 'enemy-web', message: string): ActionResult {
  const id = `${actor.id}:${actionId}`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  log(state, message)
  announceTelegraph(state, { id, sourceId: actor.id, actionId, cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'minor', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function announceCavernLine(state: RunState, actor: Actor, actionId: 'enemy-fire' | 'enemy-pull', message: string): ActionResult {
  const id = `${actor.id}:${actionId}`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  log(state, message)
  announceTelegraph(state, { id, sourceId: actor.id, actionId, cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'minor', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function sealNearbyDoor(state: RunState, actor: Actor): ActionResult {
  for (const delta of Object.values(DIRECTIONS)) {
    const tile = getTile(state.floor, actor.x + delta.x, actor.y + delta.y)
    if (tile?.kind !== 'door') continue
    tile.kind = 'lockedDoor'
    log(state, 'The Lock Keeper seals a door.')
    return [event('danger')]
  }
  return []
}

function announceRuinsLine(state: RunState, actor: Actor, actionId: 'enemy-dart' | 'enemy-ritual', message: string): ActionResult {
  const id = `${actor.id}:${actionId}`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  log(state, message)
  announceTelegraph(state, { id, sourceId: actor.id, actionId, cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'minor', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function announceForemanCavein(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:cavein`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  log(state, 'The Obsidian Warden marks a cave-in; move clear.')
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'foreman-cavein', cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'major', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function announceHeartwoodCharge(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:charge`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  log(state, 'The Heartwood Stag lowers its antlers; move clear.')
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'heartwood-charge', cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'major', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function announceGeodeFissure(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:fissure`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  const cells = actionCells('line', actor, directionToward(actor, state.hero), Math.min(6, distance(actor, state.hero)))
  log(state, 'The Geode Wyrm marks a fissure line; move clear.')
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'geode-fissure', cells, danger: 'major', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function announceRegentDecree(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:decree`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  log(state, 'The Stone Keeper marks a decree; move clear.')
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'regent-decree', cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'major', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function announceRegentJudgment(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:judgment`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  const cells = actionCells('line', actor, directionToward(actor, state.hero), Math.min(6, distance(actor, state.hero)))
  log(state, 'The Stone Keeper marks a final judgment; move clear.')
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'regent-judgment', cells, danger: 'major', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function announceGuardianSlam(state: RunState, actor: Actor): ActionResult {
  const id = `${actor.id}:slam`
  if (state.floor.telegraphs?.some(telegraph => telegraph.id === id)) return []
  const direction = directionToward(actor, state.hero)
  const cells = actionCells('cross', actor, direction, 1)
  announceTelegraph(state, { id, sourceId: actor.id, actionId: 'guardian-slam', cells, danger: 'major', windup: 1, collision: { point: { ...state.hero }, by: 'target' }, cover: false })
  return [event('danger')]
}

function directionToward(from: { x: number; y: number }, to: { x: number; y: number }): Exclude<Direction, 'wait'> {
  const x = Math.sign(to.x - from.x)
  const y = Math.sign(to.y - from.y)
  if (x === 0 && y < 0) return 'n'
  if (x > 0 && y < 0) return 'ne'
  if (x > 0 && y === 0) return 'e'
  if (x > 0 && y > 0) return 'se'
  if (x === 0 && y > 0) return 's'
  if (x < 0 && y > 0) return 'sw'
  if (x < 0 && y === 0) return 'w'
  return 'nw'
}

function monsterAttack(state: RunState, actor: Actor, ranged = 0): ActionResult {
  const rng = turnRng(state, 'combat', `attack:${actor.id}`)
  const dodge = 10 + state.hero.stats.agility + agilityEvasion(state.hero) + equipmentDefense(state.hero)
  if (rng.int(1, 20) + actor.attack < dodge) { log(state, `${actor.name} misses.`); return [event('hurt')] }
  const damage = Math.max(1, actor.attack + ranged + rng.int(0, 3) - Math.floor(state.hero.stats.vitality / 2) - equipmentDefense(state.hero))
  return damageHero(state, interceptEnemyDamage(state, actor, damage), actor.name)
}

function tickEnvironment(state: RunState, events: ActionResult): void {
  for (const actor of state.floor.actors) {
    const tile = getTile(state.floor, actor.x, actor.y)
    if (!tile || !actor.hostile) continue
    if (tile.kind === 'lava') actor.health -= 4
    if (tile.kind === 'fireVent' && turnRng(state, 'combat', `vent:${actor.id}`).chance(25)) actor.health -= 3
  }
  resolveDefeatedActors(state)
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  if (tile?.kind === 'fireVent' && turnRng(state, 'combat', 'vent:hero').chance(20)) events.push(...damageHero(state, 3, 'a fire vent', true))
  if (state.turn % 8 === 0 && state.hero.focus < state.hero.maxFocus) state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 1 + intellectFocusRecovery(state.hero))
}

function tickConditionEffects(state: RunState, events: ActionResult): void {
  const heroTick = tickConditions(state.hero)
  if (heroTick.burningDamage) events.push(...damageHero(state, heroTick.burningDamage, 'burning'))
  for (const actor of state.floor.actors) {
    const tick = tickConditions(actor)
    if (tick.burningDamage) { actor.health -= tick.burningDamage; log(state, `${actor.name} burns for ${tick.burningDamage}.`) }
  }
  resolveDefeatedActors(state)
}

function dropLoot(state: RunState, actor: Actor): void {
  const rng = turnRng(state, 'loot', `drop:${actor.id}`)
  state.floor.items.push({ id: 'gold', x: actor.x, y: actor.y, count: actor.role === 'guardian' ? rng.int(130, 210) : rng.int(5, 18) })
  const tables: Record<Biome, ItemId[]> = {
    mine: ['rock', 'tonic', 'bombPack', 'key'], wilds: ['tonic', 'ropeBundle', 'machete', 'focusTonic', 'root', 'waterScript', 'lull'], caverns: ['focusTonic', 'ember', 'mend', 'sight', 'blink', 'pull', 'spear'], ruins: ['mapScroll', 'ward', 'wardScript', 'gate', 'blinkRune'], furnace: ['cinderTonic', 'sootFilter', 'breachCharge', 'boreGel', 'cinderHammer', 'smokeKnife'], floodedRuins: ['floodSalt', 'anchorSpool', 'wingfoil', 'currentRune', 'anchorBlade', 'tideCutter'], cliffs: ['cliffSpool', 'thunderJar', 'skyMap', 'windhook', 'galeMantle'], burial: ['graveSalt', 'ancestorToken', 'tombKey', 'graveSickle', 'mourningBell'], saltFlats: ['tonic', 'focusTonic', 'fireJar', 'blink', 'pull', 'mapScroll', 'bridgeKit', 'reedGlider'], frostReliquary: ['tonic', 'focusTonic', 'ward', 'mend', 'sight', 'blink', 'grappleLine', 'mail']
  }
  if (actor.role === 'guardian' || actor.status?.includes('elite') || rng.chance(28)) state.floor.items.push({ id: rng.pick(tables[state.floor.biome]), x: actor.x, y: actor.y, count: 1 })
}
