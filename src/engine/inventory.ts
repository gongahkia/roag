import { ITEM, biomeName } from '../content'
import { DIRECTIONS, floorPoint, type Direction, type GroundItem, type Modal, type RunState } from '../types'
import { actorAt, generateAreaFloor, getTile, isPassable } from '../world'
import { applyAreaArcState, areaArcStateFor, recordAreaArcPhase } from '../escalation'
import { advance, explode, resolveDefeatedActors } from './combat'
import { resolveLineEffect } from './line-effect'
import { addCondition, modifyIncomingDamage } from './conditions'
import { hasCondition } from './conditions'
import { gateForRun } from './gates'
import { gainXp } from './progression'
import { recordRescue } from './rescue'
import { tend } from './alignment'
import { completeObjective } from '../objectives'
import { consume, distance, event, log, turnRng, type ActionResult } from './shared'
import { refreshFov } from './visibility'
import { synchronizePartyActors } from './party'
import { evaluateEquipmentEffects } from './equipment'
import { vitalityRecovery, vitalityRescueRecovery } from './vitality'
import { scriptCastProfile } from './scripts'
import { castEmber } from './ember'
import { castVerdant, isVerdantSpell } from './verdant'
import { castAstral, isAstralSpell } from './astral'
import { announceSynergies, resolveSynergies } from './synergies'
import { contextualReward, merchantStock } from './rewards'
import { grantGold, purchaseBlocker, restoreBombs, restoreRopes, spendGold } from './economy'
import { anchorBoatWithRope, applyPropEffects, operateProp, releaseCartWithRope, secureCollapsedArchWithRope } from './props'
import { trailcraftTags } from './trailcraft'
import { acquireOptionalTraversalTool, boonRank, openMilestone, toolFor } from './buildcraft'
import { recordGeneratedOptionalContent, recordOptionalContent, recordSecretValue, recordTelemetryCount } from '../telemetry'
import { consumeRelicSpell } from './relics'
import { armRelicMove, armRelicWaterCrossing } from './relics'
import { openEncounter } from './encounters'
import { revealSecretClues } from './secret-discovery'
import { claimSecretReward, secretResolutionMessage } from '../secrets'
import { takeSecretShortcut } from './shortcuts'

const resolveSecretPickup = (state: RunState, item: GroundItem): void => {
  if (!item.secretId) return
  const claim = claimSecretReward(state, item.secretId)
  if (!claim || claim === 'already-resolved') return
  recordSecretValue(state, claim.resolution.rewardValue)
  recordOptionalContent(state, 'used', `secret-resolution:${claim.room.id}:${claim.resolution.rewardKind}`)
  recordTelemetryCount(state, 'eventOutcomes', `secret-reward:${claim.resolution.rewardKind}`)
  recordTelemetryCount(state, 'eventOutcomes', `secret-risk:${claim.resolution.riskKind}`)
  log(state, secretResolutionMessage(claim.room))
}

export function pickUp(state: RunState): ActionResult {
  const item = state.floor.items.find(current => current.x === state.hero.x && current.y === state.hero.y)
  if (!item) { log(state, 'Nothing here to take.'); return [] }
  if (item.secretId && state.floor.secretRooms?.find(room => room.id === item.secretId)?.resolution) {
    state.floor.items = state.floor.items.filter(current => current !== item)
    log(state, 'This secret cache has already been claimed.')
    return []
  }
  if (item.tool) {
    const acquired = acquireOptionalTraversalTool(state, item.tool)
    state.floor.items = state.floor.items.filter(current => current !== item)
    resolveSecretPickup(state, item)
    log(state, acquired.result === 'bound' ? `You bind the ${toolFor(item.tool).name}.` : acquired.result === 'duplicate' ? `You already carry the ${toolFor(item.tool).name}; leave its duplicate behind.` : `You replace ${toolFor(acquired.replaced!).name} with the ${toolFor(item.tool).name}.`)
    return advance(state, [event('pickup')])
  }
  if (item.id === 'gold') { const gained = grantGold(state, item.count); state.floor.items = state.floor.items.filter(current => current !== item); resolveSecretPickup(state, item); log(state, `You recover ${gained} cash.`); return advance(state, [event('pickup')]) }
  if (item.id === 'key') { state.hero.keys += item.count; state.floor.items = state.floor.items.filter(current => current !== item); resolveSecretPickup(state, item); log(state, 'You take a carved key.'); return advance(state, [event('pickup')]) }
  if (state.hero.inventory.length >= 12) { log(state, 'Your pack is full.'); return [] }
  state.hero.inventory.push(item.id)
  state.floor.items = state.floor.items.filter(current => current !== item)
  resolveSecretPickup(state, item)
  log(state, `You take ${ITEM[item.id].name}.`)
  return advance(state, [event('pickup')])
}

export function operate(state: RunState): ActionResult {
  const milestone = openMilestone(state)
  if (milestone) return milestone
  const encounter = openEncounter(state)
  if (encounter) return encounter
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  const friend = state.floor.actors.find(actor => !actor.hostile && distance(actor, state.hero) <= 1)
  const altar = tile?.kind === 'altar' ? tile : friend && getTile(state.floor, friend.x, friend.y)?.kind === 'altar' ? getTile(state.floor, friend.x, friend.y) : undefined
  const container = nearbyContainer(state)
  if (container) {
    container.tile.kind = 'floor'
    const loot = contextualReward(state, 'container')
    grantGold(state, (container.kind === 'chest' ? 60 : 18) + boonRank(state, 'barterThread') * 10)
    if (state.hero.inventory.length < 12) state.hero.inventory.push(loot)
    else state.floor.items.push({ id: loot, x: container.x, y: container.y, count: 1 })
    log(state, `You open the ${container.kind} and find ${ITEM[loot].name}.`)
    if (completeObjective(state, 'recoverSupplies')) log(state, 'Objective complete: trail cache secured.')
    return advance(state, [event('pickup')])
  }
  if (tile?.kind === 'rescue' || friend?.name === 'stranded traveler' || friend?.name === 'lost scout') {
    const knownRescue = state.rescuedNpcs?.some(npc => npc.id === `rescue:${state.area ?? state.floor.biome}:${state.floor.index}:${friend?.id ?? `${state.hero.x},${state.hero.y}`}`) ?? false
    const npc = recordRescue(state, friend)
    state.hero.maxHealth += 2
    state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 8 + vitalityRescueRecovery(state.hero))
    grantGold(state, 35)
    state.floor.actors = state.floor.actors.filter(actor => actor !== friend)
    const eventTile = friend ? getTile(state.floor, friend.x, friend.y) : tile
    if (eventTile?.kind === 'rescue' || eventTile?.kind === 'altar') eventTile.kind = 'floor'
    log(state, `${npc.name} reaches the village outpost.`)
    if (completeObjective(state, 'rescueScout')) log(state, 'Objective complete: traveler aided.')
    if (!knownRescue) tend(state, 'villagePact')
    return advance(state, [event('rescue')])
  }
  if (altar?.kind === 'altar') {
    if (state.hero.gold < 75) { log(state, 'The shrine asks for 75 cash.'); return [] }
    spendGold(state, 75)
    const reward = contextualReward(state, 'altar')
    if (state.hero.inventory.length < 12) state.hero.inventory.push(reward)
    else state.floor.items.push({ id: reward, x: state.hero.x, y: state.hero.y, count: 1 })
    gainXp(state, 35)
    log(state, `The shrine grants insight and ${ITEM[reward].name}.`)
    if (completeObjective(state, 'invokeAltar')) log(state, 'Objective complete: shrine offering made.')
    return advance(state, [event('spell')])
  }
  if (nearbyGate(state)) {
    const lock = nearbyLockedDoor(state)
    if (lock && state.hero.keys > 0) {
      state.hero.keys--
      lock.kind = 'floor'
      log(state, 'You unlock the sealed door.')
      return advance(state, [event('gateResolved')])
    }
    const gate = gateForRun(state)
    if (!gate) { log(state, 'This is the final route. Cross the area to complete the delivery.'); return [] }
    state.modal = { kind: 'gate', gateId: gate.id }
    log(state, gate.npcOffering)
    return [event('menu')]
  }
  if (friend?.role === 'merchant') { state.modal = { kind: 'shop', merchantId: friend.id }; return [event('menu')] }
  const shortcut = takeSecretShortcut(state)
  if (shortcut) return shortcut
  const propOperation = operateProp(state)
  if (propOperation) {
    revealSecretClues(state, 'prop')
    return propOperation.events.length ? advance(state, propOperation.events) : []
  }
  if (revealSecretClues(state, 'prop').length) return [event('menu')]
  log(state, 'Nothing answers.')
  return []
}

export function descend(state: RunState): ActionResult {
  const tile = getTile(state.floor, state.hero.x, state.hero.y)
  if (tile?.kind !== 'exit') { log(state, 'You are not at the exit.'); return [] }
  if (state.floor.objective.status !== 'complete') { log(state, `Objective incomplete: ${state.floor.objective.label}.`); return [] }
  if (!state.floor.guardianDefeated) { log(state, 'A guardian still seals the route.'); return [] }
  const areaFloor = state.areaFloor ?? state.floor.index % 4
  const biome = state.area ?? state.floor.biome
  const areaArc = state.areaArc?.biome === biome ? state.areaArc : areaArcStateFor(state.seed, biome)
  if (state.floor.escalation) recordAreaArcPhase(areaArc, state.floor.escalation.phase)
  state.areaArc = areaArc
  if (areaFloor === 3) { state.modal = undefined; log(state, `${biomeName[state.area ?? state.floor.biome]} is crossed. Return to the village outpost.`); return [event('areaComplete')] }
  const nextAreaFloor = areaFloor + 1
  const routePosition = Math.max(0, (state.areaOrder ?? []).indexOf(biome))
  state.floor = generateAreaFloor(state.seed, biome, nextAreaFloor, routePosition)
  applyAreaArcState(state.floor, areaArc)
  recordGeneratedOptionalContent(state)
  state.areaFloor = nextAreaFloor
  state.hero.x = state.floor.start.x
  state.hero.y = state.floor.start.y
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + 4 + vitalityRecovery(state.hero))
  state.hero.focus = state.hero.maxFocus
  state.hero.oaths = (state.hero.oaths ?? []).flatMap(oath => oath.remainingFloors <= 1 ? [] : [{ ...oath, remainingFloors: oath.remainingFloors - 1 }])
  synchronizePartyActors(state, 'floorTransition')
  state.modal = { kind: 'trailcraft' }
  log(state, `You continue through ${biomeName[state.floor.biome]}.`)
  log(state, 'Trail cleared: choose a trailcraft.')
  refreshFov(state)
  return [event('floor')]
}

export function inventoryChoice(state: RunState, modal: Extract<Modal, { kind: 'inventory' }>, command: string): ActionResult {
  const index = Number(command) - 1
  if (!Number.isInteger(index) || index < 0 || index >= state.hero.inventory.length) return []
  const id = state.hero.inventory[index]
  state.modal = undefined
  if (modal.mode === 'use') return useItem(state, id, index)
  if (modal.mode === 'drop') {
    state.hero.inventory.splice(index, 1)
    state.floor.items.push({ id, x: state.hero.x, y: state.hero.y, count: 1, visibleInFog: true })
    log(state, `You drop ${ITEM[id].name}.`)
    return advance(state, [event('pickup')])
  }
  if (modal.mode === 'throw') { state.modal = { kind: 'target', action: 'throw', item: id }; return [event('menu')] }
  return equip(state, id, index)
}

export function useRope(state: RunState): ActionResult {
  const climb = state.floor.climbLinks?.find(link => (link.lower.x === state.hero.x && link.lower.y === state.hero.y) || (link.upper.x === state.hero.x && link.upper.y === state.hero.y))
  if (climb?.anchored) {
    const destination = climb.lower.x === state.hero.x && climb.lower.y === state.hero.y ? climb.upper : climb.lower
    state.hero.x = destination.x
    state.hero.y = destination.y
    state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + boonRank(state, 'galeThread'))
    for (let radius = 0; radius < boonRank(state, 'highPath'); radius++) for (const tile of state.floor.tiles) if (!tile.explored) { tile.explored = true; break }
    refreshFov(state)
    log(state, 'You climb the secured vertical rope.')
    return advance(state, [event('traverse'), event('rope')])
  }
  if (climb) {
    const free = boonRank(state, 'ropewright') >= 2 && state.turn % 2 === 0
    if (!free && state.hero.ropes < 1) { log(state, 'No ropes remain.'); return [] }
    if (!free) state.hero.ropes--
    climb.anchored = true
    log(state, free ? 'Ropewright binds the vertical route without spending reserve rope.' : 'You secure a vertical rope between the cliff tiers.')
    return advance(state, [event('rope')])
  }
  if (state.hero.ropes < 1) { log(state, 'No ropes remain.'); return [] }
  const tile = getTile(state.floor, state.hero.x, state.hero.y)!
  if (tile.kind === 'pit') tile.kind = 'rope'
  else {
    const below = getTile(state.floor, state.hero.x, state.hero.y + 1)
    if (below?.kind === 'pit') below.kind = 'rope'
    else {
      const cartEvents = releaseCartWithRope(state)
      if (cartEvents !== undefined) {
        if (!cartEvents.length) return []
        state.hero.ropes--
        log(state, 'You rig the cart with a rope.')
        return advance(state, [event('rope'), ...cartEvents])
      }
      const boatEvents = anchorBoatWithRope(state)
      if (boatEvents !== undefined) {
        if (!boatEvents.length) return []
        state.hero.ropes--
        log(state, 'You anchor the boat with a rope.')
        return advance(state, [event('rope'), ...boatEvents])
      }
      const archEvents = secureCollapsedArchWithRope(state)
      if (archEvents !== undefined) {
        if (!archEvents.length) return []
        state.hero.ropes--
        log(state, 'You brace the collapsed arch with a rope.')
        return advance(state, [event('rope'), ...archEvents])
      }
      log(state, 'There is nowhere to anchor a rope.')
      return []
    }
  }
  state.hero.ropes--
  log(state, 'You secure a rope.')
  return advance(state, [event('rope')])
}

export function castFirstSpell(state: RunState): ActionResult {
  if (state.hero.oaths?.some(oath => oath.id === 'noCharms')) { log(state, 'Your active oath forbids charms.'); return [] }
  const id = state.hero.inventory.find(item => ITEM[item].use === 'spell')
  if (!id) { log(state, 'You know no ready charm.'); return [] }
  state.modal = { kind: 'target', action: 'spell', item: id }
  return [event('menu')]
}

export function quickCast(state: RunState, direction: Direction): ActionResult {
  const id = state.hero.inventory.find(item => ITEM[item].use === 'spell')
  if (!id) { log(state, 'You know no ready charm.'); return [] }
  return castSpell(state, id, direction)
}

export function bomb(state: RunState, direction: Direction): ActionResult {
  if (state.hero.oaths?.some(oath => oath.id === 'noBombs')) { log(state, 'Your active oath forbids bombs.'); return [] }
  if (state.hero.bombs < 1) { log(state, 'No bombs remain.'); return [] }
  state.hero.bombs--
  const delta = DIRECTIONS[direction]
  log(state, 'You place a bomb.')
  const lastMatch = state.hero.health * 4 <= state.hero.maxHealth ? boonRank(state, 'lastMatch') * 2 : 0
  explode(state, state.hero.x + delta.x * 2, state.hero.y + delta.y * 2, 12 + lastMatch, ['bomb'], 'your bomb', 1 + Math.min(1, boonRank(state, 'spareFuse')))
  return advance(state, [event('boom')])
}

const drillableTerrain = new Set(['wall', 'rubble', 'bramble', 'boulder', 'breakwall'])
const glidableTerrain = new Set(['pit', 'water', 'deepWater', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'smoke', 'crumble', 'boulder', 'bramble', 'rubble', 'current'])
const grappleTerrain = new Set(['pit', 'water', 'deepWater', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'smoke', 'crumble', 'current'])
const grappleBlockers = new Set(['wall', 'rubble', 'bramble', 'boulder', 'breakwall', 'crate', 'chest', 'lockedDoor'])
const bridgeTerrain = new Set(['pit', 'water', 'deepWater', 'current'])
const dashTerrain = new Set(['smoke', 'gas', 'fireVent', 'current'])
const winchTerrain = new Set(['boulder', 'rubble', 'crate'])

export function drill(state: RunState, id: string, direction: Exclude<Direction, 'wait'>): ActionResult {
  const index = state.hero.inventory.indexOf(id)
  const delta = DIRECTIONS[direction]
  const tile = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)
  if (index < 0 || !tile || !drillableTerrain.has(tile.kind)) { log(state, 'The auger needs blocked ground.'); return [] }
  tile.kind = 'floor'
  consume(state, index)
  refreshFov(state)
  log(state, 'The auger opens a narrow passage.')
  return advance(state, [event('traverse'), event('pickup')])
}

export function glide(state: RunState, id: string, direction: Exclude<Direction, 'wait'>): ActionResult {
  const index = state.hero.inventory.indexOf(id)
  const delta = DIRECTIONS[direction]
  const middle = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)
  const landing = { x: state.hero.x + delta.x * 2, y: state.hero.y + delta.y * 2 }
  if (index < 0 || !middle || !glidableTerrain.has(middle.kind) || !isPassable(state.floor, landing.x, landing.y)) { log(state, 'The glider needs a clear landing beyond hazardous ground.'); return [] }
  state.hero.x = landing.x
  state.hero.y = landing.y
  if (middle.kind === 'water' || middle.kind === 'current') armRelicWaterCrossing(state)
  armRelicMove(state)
  consume(state, index)
  refreshFov(state)
  log(state, 'You ride the reed glider across the hazard.')
  return advance(state, [event('traverse'), event('move')])
}

export function grapple(state: RunState, id: string, direction: Exclude<Direction, 'wait'>): ActionResult {
  const index = state.hero.inventory.indexOf(id)
  const delta = DIRECTIONS[direction]
  const route = [3, 2].map(range => {
    const crossing = Array.from({ length: range - 1 }, (_, offset) => getTile(state.floor, state.hero.x + delta.x * (offset + 1), state.hero.y + delta.y * (offset + 1)))
    const landing = { x: state.hero.x + delta.x * range, y: state.hero.y + delta.y * range }
    return { crossing, landing }
  }).find(candidate => candidate.crossing.every(tile => tile && !grappleBlockers.has(tile.kind)) && candidate.crossing.some(tile => tile && grappleTerrain.has(tile.kind)) && isPassable(state.floor, candidate.landing.x, candidate.landing.y))
  if (index < 0 || !route) { log(state, 'The grappling line needs a clear landing beyond a gap or hazard.'); return [] }
  state.hero.x = route.landing.x
  state.hero.y = route.landing.y
  if (route.crossing.some(tile => tile?.kind === 'water' || tile?.kind === 'current')) armRelicWaterCrossing(state)
  armRelicMove(state)
  consume(state, index)
  refreshFov(state)
  log(state, 'The grappling line carries you over the break.')
  return advance(state, [event('traverse'), event('move')])
}

export function bridge(state: RunState, id: string, direction: Exclude<Direction, 'wait'>): ActionResult {
  const index = state.hero.inventory.indexOf(id)
  const delta = DIRECTIONS[direction]
  const tile = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)
  if (index < 0 || !tile || !bridgeTerrain.has(tile.kind)) { log(state, 'The bridge needs an adjacent pit, water, current, or deep water.'); return [] }
  tile.kind = 'rope'
  delete tile.flow
  consume(state, index)
  refreshFov(state)
  log(state, 'The bridge locks into a permanent crossing.')
  return advance(state, [event('traverse'), event('rope')])
}

export function dash(state: RunState, id: string, direction: Exclude<Direction, 'wait'>): ActionResult {
  const index = state.hero.inventory.indexOf(id)
  const delta = DIRECTIONS[direction]
  const middle = getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)
  const landing = { x: state.hero.x + delta.x * 2, y: state.hero.y + delta.y * 2 }
  if (index < 0 || !middle || !dashTerrain.has(middle.kind) || !isPassable(state.floor, landing.x, landing.y)) { log(state, 'The steam jetpack needs smoke, gas, fire, or current before a clear landing.'); return [] }
  state.hero.x = landing.x
  state.hero.y = landing.y
  if (middle.kind === 'current') armRelicWaterCrossing(state)
  armRelicMove(state)
  consume(state, index)
  refreshFov(state)
  log(state, 'Steam carries you through the hazard.')
  return advance(state, [event('traverse'), event('move')])
}

export function winch(state: RunState, id: string, direction: Exclude<Direction, 'wait'>): ActionResult {
  const index = state.hero.inventory.indexOf(id)
  const delta = DIRECTIONS[direction]
  const target = { x: state.hero.x + delta.x, y: state.hero.y + delta.y }
  const tile = getTile(state.floor, target.x, target.y)
  if (index < 0 || !tile || !winchTerrain.has(tile.kind)) { log(state, 'The winch needs an adjacent boulder, rubble, or crate.'); return [] }
  tile.kind = 'floor'
  state.hero.x = target.x
  state.hero.y = target.y
  armRelicMove(state)
  consume(state, index)
  refreshFov(state)
  log(state, 'The winch clears the obstruction and reels you forward.')
  return advance(state, [event('traverse'), event('move')])
}

export function throwItem(state: RunState, id: string, direction: Direction): ActionResult {
  const index = state.hero.inventory.indexOf(id)
  if (index === -1) return []
  state.hero.inventory.splice(index, 1)
  const delta = DIRECTIONS[direction]
  const destination = { x: state.hero.x + delta.x * 5, y: state.hero.y + delta.y * 5 }
  const cells = resolveLineEffect(state.floor, state.hero, destination).cells
  const point = cells.at(-1) ?? { x: state.hero.x, y: state.hero.y }
  const target = actorAt(state.floor, point.x, point.y)
  if (target?.hostile) { target.health -= modifyIncomingDamage(target, 3 + state.hero.stats.strength + boonRank(state, 'emberFletching') + boonRank(state, 'thunderVessel') + boonRank(state, 'tetheredThunder')); if (boonRank(state, 'thunderVessel')) addCondition(target, { kind: 'marked', duration: 2, potency: boonRank(state, 'thunderVessel') }); log(state, `${ITEM[id].name} hits ${target.name}.`) }
  if (id === 'fireJar') explode(state, point.x, point.y, 5, ['bomb', 'fire'])
  else {
    state.floor.items.push({ id, x: point.x, y: point.y, count: 1, visibleInFog: true })
    applyPropEffects(state, [point], ['throw'])
  }
  resolveDefeatedActors(state)
  return advance(state, [event(id === 'fireJar' ? 'boom' : 'hit')])
}

export function castSpell(state: RunState, id: string, direction: Direction): ActionResult {
  if (state.hero.oaths?.some(oath => oath.id === 'noCharms')) { log(state, 'Your active oath forbids charms.'); return [] }
  const item = ITEM[id]
  const profile = scriptCastProfile(state.hero, id)
  const circuitReady = Boolean(state.hero.relicCharges?.ashCircuit)
  const currentTerrain = getTile(state.floor, state.hero.x, state.hero.y)?.kind
  const focusCost = Math.max(1, profile.focusCost - Number(circuitReady) - boonRank(state, 'frozenFocus') * Number(hasCondition(state.hero, 'shielded')) - boonRank(state, 'sunsetCircuit') * Number(currentTerrain === 'saltMirror'))
  if (state.hero.focus < focusCost) { log(state, 'You lack focus.'); return [] }
  const circuit = circuitReady && consumeRelicSpell(state)
  state.hero.focus -= focusCost
  const geometry = resolveSynergies({ scripts: [id], skills: state.hero.skills }, { range: profile.range })
  const delta = DIRECTIONS[direction]
  const point = { x: state.hero.x + delta.x * Math.max(1, Math.floor(geometry.values.range ?? profile.range)), y: state.hero.y + delta.y * Math.max(1, Math.floor(geometry.values.range ?? profile.range)) }
  const tile = getTile(state.floor, point.x, point.y)
  const impact = resolveSynergies({ scripts: [id], terrain: tile ? [tile.kind] : [] })
  if (item.spell === 'ember') castEmber(state, point, impact.values.damage ?? 0)
  if (isVerdantSpell(item.spell)) castVerdant(state, item.spell, point)
  if (isAstralSpell(item.spell)) castAstral(state, item.spell, point)
  announceSynergies(state, geometry)
  announceSynergies(state, impact)
  const effect = evaluateEquipmentEffects(state.hero, 'triggered', { trigger: 'spell', scripts: [id] })
  const tags = trailcraftTags(state.hero).filter(id => id === 'barkBinding' || id === 'spiritThread')
  const trailcraft = resolveSynergies({ tags })
  announceSynergies(state, trailcraft)
  state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + (effect.values.focus ?? 0) + (trailcraft.values.focus ?? 0))
  if (circuit) { state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 2); log(state, 'Ash Circuit completes the traversal-to-Charm relay.') }
  resolveDefeatedActors(state)
  refreshFov(state)
  log(state, `${item.name} takes effect.`)
  return advance(state, [event('spell')])
}

export function shopChoice(state: RunState, command: string): ActionResult {
  const id = merchantStock(state)[Number(command) - 1]
  if (!id) return []
  const item = ITEM[id]
  if (state.hero.gold < item.value) { log(state, 'Not enough cash.'); return [event('menu')] }
  const blocker = purchaseBlocker(state.hero, id)
  if (blocker) { log(state, blocker); return [event('menu')] }
  if (state.hero.inventory.length >= 12) { log(state, 'Your pack is full.'); return [event('menu')] }
  spendGold(state, item.value)
  recordTelemetryCount(state, 'purchases', id)
  state.hero.inventory.push(id)
  log(state, `You buy ${item.name}.`)
  return advance(state, [event('pickup')])
}

const recoverUsedItem = (state: RunState, id: string): void => {
  const chance = Math.min(90, boonRank(state, 'salvager') * 25 + boonRank(state, 'deepPockets') * 35)
  if (!chance || state.hero.inventory.length >= 12 || !turnRng(state, 'loot', `recover:${id}`).chance(chance)) return
  state.hero.inventory.push(id)
  log(state, `${ITEM[id].name} is recovered by your build.`)
}

function useItem(state: RunState, id: string, inventoryIndex: number): ActionResult {
  const item = ITEM[id]
  if (item.slot) return equip(state, id, inventoryIndex)
  if (item.use === 'heal') { if (state.hero.oaths?.some(oath => oath.id === 'noHealing')) { log(state, 'Your active oath forbids healing.'); return [] }; state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + Math.max(1, 10 + vitalityRecovery(state.hero) - boonRank(state, 'quietPocket') - boonRank(state, 'hardLesson')) + boonRank(state, 'stormRations') * 2); if (boonRank(state, 'quietPocket')) state.hero.conditions = []; consume(state, inventoryIndex); recoverUsedItem(state, id); log(state, 'Warmth returns to your limbs.'); return advance(state, [event('spell')]) }
  if (item.use === 'focus') { state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + 8); consume(state, inventoryIndex); recoverUsedItem(state, id); log(state, 'Your mind sharpens.'); return advance(state, [event('spell')]) }
  if (item.use === 'map') { for (const tile of state.floor.tiles) tile.explored = true; consume(state, inventoryIndex); log(state, 'The floor map unfolds in your mind.'); return advance(state, [event('spell')]) }
  if (item.use === 'teleport') {
    const choices = state.floor.tiles.flatMap((tile, i) => tile.kind === 'floor' && tile.explored ? [floorPoint(state.floor, i)] : [])
    if (choices.length) { const target = turnRng(state, 'combat', 'blink').pick(choices); state.hero.x = target.x; state.hero.y = target.y }
    consume(state, inventoryIndex); refreshFov(state); log(state, 'Space folds.'); return advance(state, [event('spell')])
  }
  if (item.use === 'drill') { state.modal = { kind: 'target', action: 'drill', item: id }; return [event('menu')] }
  if (item.use === 'glide') { state.modal = { kind: 'target', action: 'glide', item: id }; return [event('menu')] }
  if (item.use === 'grapple') { state.modal = { kind: 'target', action: 'grapple', item: id }; return [event('menu')] }
  if (item.use === 'bridge') { state.modal = { kind: 'target', action: 'bridge', item: id }; return [event('menu')] }
  if (item.use === 'dash') { state.modal = { kind: 'target', action: 'dash', item: id }; return [event('menu')] }
  if (item.use === 'winch') { state.modal = { kind: 'target', action: 'winch', item: id }; return [event('menu')] }
  if (item.use === 'bomb') { const restored = restoreBombs(state.hero, 3 + boonRank(state, 'spareFuse')); if (!restored) { log(state, 'Your bomb reserve is full.'); return [] }; consume(state, inventoryIndex); recoverUsedItem(state, id); log(state, `You gain ${restored} bombs.`); return advance(state, [event('pickup')]) }
  if (item.use === 'rope') { const restored = restoreRopes(state.hero, 3); if (!restored) { log(state, 'Your rope reserve is full.'); return [] }; consume(state, inventoryIndex); recoverUsedItem(state, id); log(state, `You gain ${restored} ropes.`); return advance(state, [event('pickup')]) }
  if (item.use === 'key') { state.hero.keys++; consume(state, inventoryIndex); return advance(state, [event('pickup')]) }
  if (item.use === 'spell') { state.modal = { kind: 'target', action: 'spell', item: id }; return [event('menu')] }
  log(state, 'That cannot be used here.')
  return []
}

function equip(state: RunState, id: string, index: number): ActionResult {
  const item = ITEM[id]
  if (!item.slot) { log(state, 'That cannot be equipped.'); return [] }
  const previous = state.hero.equipment[item.slot]
  state.hero.inventory.splice(index, 1)
  if (previous) { state.hero.inventory.push(previous); state.hero.lastUnequipped = previous }
  state.hero.equipment[item.slot] = id
  log(state, `You equip ${item.name}.`)
  return advance(state, [event('pickup')])
}

export function swap(state: RunState): ActionResult {
  const id = state.hero.lastUnequipped
  if (!id || state.hero.inventory.length >= 12) { log(state, 'No item is ready to swap.'); return [] }
  state.hero.inventory.push(id)
  state.hero.lastUnequipped = undefined
  log(state, 'You stow your last unequipped item.')
  return advance(state, [event('pickup')])
}

const nearbyContainer = (state: RunState): { tile: NonNullable<ReturnType<typeof getTile>>; kind: 'crate' | 'chest'; x: number; y: number } | undefined => {
  for (const delta of Object.values(DIRECTIONS)) {
    const x = state.hero.x + delta.x
    const y = state.hero.y + delta.y
    const tile = getTile(state.floor, x, y)
    if (tile?.kind === 'crate' || tile?.kind === 'chest') return { tile, kind: tile.kind, x, y }
  }
  return undefined
}

const nearbyGate = (state: RunState): boolean => Object.values(DIRECTIONS).some(delta => getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)?.kind === 'lockedDoor')
const nearbyLockedDoor = (state: RunState): NonNullable<ReturnType<typeof getTile>> | undefined => Object.values(DIRECTIONS).map(delta => getTile(state.floor, state.hero.x + delta.x, state.hero.y + delta.y)).find(tile => tile?.kind === 'lockedDoor')
