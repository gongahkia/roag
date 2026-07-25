import { DIRECTIONS, type BoonId, type FloorMilestone, type RunState, type TraversalToolId } from '../types'
import { rngFor } from '../rng'
import { getTile, isPassable } from '../world'
import { advance } from './combat'
import { event, log, type ActionResult } from './shared'
import { refreshFov } from './visibility'

export interface TraversalTool { id: TraversalToolId; name: string; glyph: string; cooldown: number; text: string; overdrive: string }
export interface Boon { id: BoonId; name: string; glyph: string; text: string; family: 'traversal' | 'combat' | 'recovery' | 'scouting' | 'spellcraft' | 'economy' }

export const TOOLS: readonly TraversalTool[] = [
  { id: 'stoneWedge', name: 'Stone Wedge', glyph: 'W', cooldown: 6, text: 'Breach one adjacent blocker.', overdrive: 'Breach a three-tile wedge, then retire.' },
  { id: 'reedwing', name: 'Reedwing', glyph: '^', cooldown: 5, text: 'Cross one hazardous tile to a clear landing.', overdrive: 'Cross up to two hazards, then retire.' },
  { id: 'cordAnchor', name: 'Cord Anchor', glyph: '⌁', cooldown: 5, text: 'Pull two tiles to a clear landing.', overdrive: 'Pull four tiles, then retire.' },
  { id: 'ashwayRites', name: 'Ashway Rites', glyph: '≈', cooldown: 8, text: 'Make a two-tile temporary safe path.', overdrive: 'Make a three-tile path, then retire.' }
]

export const BOONS: readonly Boon[] = [
  { id: 'scoutEye', name: 'Scout Eye', glyph: '◉', family: 'scouting', text: 'Reveal all remaining milestone directions.' },
  { id: 'timeKnot', name: 'Time Knot', glyph: '⟲', family: 'recovery', text: 'Gain one position-only safe-step rewind.' },
  { id: 'coolAsh', name: 'Cool Ash', glyph: '•', family: 'traversal', text: 'Tools recover 1 turn faster per stack.' },
  { id: 'wayfinderCord', name: 'Wayfinder Cord', glyph: '⌇', family: 'traversal', text: 'Tool use reveals nearby terrain.' },
  { id: 'rootedResolve', name: 'Rooted Resolve', glyph: '✦', family: 'recovery', text: 'Tool use restores 1 HP per stack.' },
  { id: 'trailRations', name: 'Trail Rations', glyph: '+', family: 'recovery', text: 'Each milestone restores 2 HP per stack.' },
  { id: 'emberFletching', name: 'Ember Fletching', glyph: '*', family: 'combat', text: 'Thrown attacks deal +1 damage per stack.' },
  { id: 'cordTempo', name: 'Cord Tempo', glyph: '/', family: 'combat', text: 'After a tool use, your next strike gains +1 damage per stack.' },
  { id: 'watchfulStep', name: 'Watchful Step', glyph: '!', family: 'scouting', text: 'Newly discovered milestones reveal nearby threats.' },
  { id: 'mapMoss', name: 'Map Moss', glyph: '·', family: 'scouting', text: 'Exploration reveals 1 extra tile radius per stack.' },
  { id: 'quietTide', name: 'Quiet Tide', glyph: '~', family: 'spellcraft', text: 'First charm each floor costs 1 less focus per stack.' },
  { id: 'spiritKindling', name: 'Spirit Kindling', glyph: '?', family: 'spellcraft', text: 'Milestones restore 1 focus per stack.' },
  { id: 'cacheSense', name: 'Cache Sense', glyph: '$', family: 'economy', text: 'Each milestone yields 8 cash per stack.' },
  { id: 'barterThread', name: 'Barter Thread', glyph: '¤', family: 'economy', text: 'Containers yield +10 cash per stack.' },
  { id: 'stoneMemory', name: 'Stone Memory', glyph: '#', family: 'traversal', text: 'Stone Wedge also clears nearby rubble per stack.' },
  { id: 'reedMemory', name: 'Reed Memory', glyph: '≋', family: 'traversal', text: 'Reedwing crosses one extra hazard per stack.' },
  { id: 'lastLight', name: 'Last Light', glyph: 'i', family: 'recovery', text: 'At 25% HP, gain 1 focus per stack once per floor.' },
  { id: 'parcelMark', name: 'Parcel Mark', glyph: '□', family: 'economy', text: 'Unclaimed milestones show distance after one stack.' }
]

const toolById = Object.fromEntries(TOOLS.map(tool => [tool.id, tool])) as Record<TraversalToolId, TraversalTool>
const boonById = Object.fromEntries(BOONS.map(boon => [boon.id, boon])) as Record<string, Boon>
const drillable = new Set(['wall', 'rubble', 'bramble', 'boulder'])
const hazardous = new Set(['pit', 'water', 'lava', 'spikes', 'dart', 'fireVent', 'gas', 'crumble', 'boulder', 'bramble', 'rubble'])

export const toolFor = (id: TraversalToolId): TraversalTool => toolById[id]
export const boonFor = (id: BoonId): Boon => boonById[id]
export const boonRank = (state: RunState, id: BoonId): number => state.hero.boons?.[id] ?? 0
export const hasBoon = (state: RunState, id: BoonId): boolean => boonRank(state, id) > 0
export const toolCooldown = (state: RunState, id: TraversalToolId): number => state.hero.cooldowns?.[`tool:${id}`] ?? 0

export const toolChoices = (state: RunState, milestone: FloorMilestone): TraversalTool[] => rngFor(state.seed, 'progression', state.floor.index, milestone.id, 'tools').shuffle([...TOOLS]).slice(0, 3)
export const boonChoices = (state: RunState, milestone: FloorMilestone): Boon[] => rngFor(state.seed, 'progression', state.floor.index, milestone.id, 'boons').shuffle([...BOONS]).slice(0, 3)

const milestoneAtReach = (state: RunState): FloorMilestone | undefined => state.floor.milestones.find(milestone => !milestone.claimed && Math.max(Math.abs(milestone.x - state.hero.x), Math.abs(milestone.y - state.hero.y)) <= 1)

export function openMilestone(state: RunState): ActionResult | undefined {
  const milestone = milestoneAtReach(state)
  if (!milestone) return undefined
  milestone.discovered = true
  state.modal = milestone.kind === 'waycache' ? { kind: 'tool', milestoneId: milestone.id } : { kind: 'boon', milestoneId: milestone.id }
  log(state, milestone.kind === 'waycache' ? 'Waycache found: choose a ritual tool.' : 'Boon site found: choose one mark.')
  return [event('menu')]
}

const milestone = (state: RunState, id: string): FloorMilestone | undefined => state.floor.milestones.find(current => current.id === id && !current.claimed)
const claim = (state: RunState, current: FloorMilestone): void => {
  current.claimed = true
  const health = boonRank(state, 'trailRations') * 2
  const focus = boonRank(state, 'spiritKindling')
  const cash = boonRank(state, 'cacheSense') * 8
  state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + health)
  state.hero.focus = Math.min(state.hero.maxFocus, state.hero.focus + focus)
  state.hero.gold += cash
}

export function chooseBoon(state: RunState, milestoneId: string, command: string): boolean {
  const current = milestone(state, milestoneId)
  if (!current) return false
  const choice = boonChoices(state, current)[Number(command) - 1]
  if (!choice) return false
  state.hero.boons ??= {}
  state.hero.boons[choice.id] = boonRank(state, choice.id) + 1
  claim(state, current)
  state.modal = undefined
  log(state, `Boon: ${choice.name} · rank ${boonRank(state, choice.id)}.`)
  if (hasBoon(state, 'scoutEye')) revealMilestones(state)
  return true
}

export function chooseTool(state: RunState, milestoneId: string, command: string): boolean {
  const current = milestone(state, milestoneId)
  if (!current) return false
  const tools = state.hero.traversalTools ?? []
  const modal = state.modal?.kind === 'tool' ? state.modal : undefined
  if (!modal) return false
  if (tools.length >= 2 && modal.replace === undefined) {
    const slot = Number(command) - 1
    if (slot < 0 || slot >= tools.length) return false
    state.modal = { ...modal, replace: slot }
    log(state, `Replace ${toolFor(tools[slot]).name}; choose a Waycache tool.`)
    return true
  }
  const choice = toolChoices(state, current)[Number(command) - 1]
  if (!choice) return false
  if (modal.replace === undefined) tools.push(choice.id)
  else tools[modal.replace] = choice.id
  state.hero.traversalTools = tools
  claim(state, current)
  state.modal = undefined
  log(state, `You bind ${choice.name}.`)
  return true
}

export function openTools(state: RunState): ActionResult {
  const tools = state.hero.traversalTools ?? []
  if (!tools.length) { log(state, 'No ritual traversal tool is bound. Find a Waycache.'); return [] }
  state.modal = { kind: 'tools' }
  return [event('menu')]
}

export function chooseToolUse(state: RunState, command: string): boolean {
  const tool = state.hero.traversalTools?.[Number(command) - 1]
  if (!tool) return false
  const cooldown = toolCooldown(state, tool)
  if (cooldown) { log(state, `${toolFor(tool).name} recovers in ${cooldown} turn${cooldown === 1 ? '' : 's'}.`); return false }
  state.modal = { kind: 'target', action: tool, tool }
  return true
}

const setCooldown = (state: RunState, tool: TraversalToolId) => { (state.hero.cooldowns ??= {})[`tool:${tool}`] = Math.max(1, toolFor(tool).cooldown - boonRank(state, 'coolAsh')) }
const point = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, distance: number) => ({ x: state.hero.x + DIRECTIONS[direction].x * distance, y: state.hero.y + DIRECTIONS[direction].y * distance })
const passableLanding = (state: RunState, target: { x: number; y: number }) => isPassable(state.floor, target.x, target.y)

export function useTool(state: RunState, tool: TraversalToolId, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive = false): ActionResult {
  if (!(state.hero.traversalTools ?? []).includes(tool)) { log(state, 'That ritual tool is no longer bound.'); return [] }
  if (toolCooldown(state, tool)) { log(state, `${toolFor(tool).name} is still recovering.`); return [] }
  const result = tool === 'stoneWedge' ? useStoneWedge(state, direction, overdrive) : tool === 'reedwing' ? useReedwing(state, direction, overdrive) : tool === 'cordAnchor' ? useCordAnchor(state, direction, overdrive) : useAshway(state, direction, overdrive)
  if (!result) return []
  const healing = boonRank(state, 'rootedResolve')
  if (healing) state.hero.health = Math.min(state.hero.maxHealth, state.hero.health + healing)
  if (overdrive) {
    state.hero.traversalTools = (state.hero.traversalTools ?? []).filter(current => current !== tool)
    log(state, `${toolFor(tool).name} burns out after its overdrive.`)
  } else setCooldown(state, tool)
  refreshFov(state)
  return advance(state, [event('spell')])
}

const useStoneWedge = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const target = point(state, direction, 1)
  const offsets = overdrive ? [[0, 0], [DIRECTIONS[direction].y, -DIRECTIONS[direction].x], [-DIRECTIONS[direction].y, DIRECTIONS[direction].x]] : [[0, 0]]
  const cleared = offsets.map(([x, y]) => getTile(state.floor, target.x + x, target.y + y)).filter((tile): tile is NonNullable<ReturnType<typeof getTile>> => Boolean(tile && drillable.has(tile.kind)))
  if (!cleared.length) { log(state, 'Stone Wedge needs blocked ground.'); return false }
  cleared.forEach(tile => { tile.kind = 'floor' })
  log(state, overdrive ? 'The wedge opens a broad passage.' : 'The wedge opens a narrow passage.')
  return true
}

const useReedwing = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const length = overdrive ? 3 : 2
  const landing = point(state, direction, length)
  const crossed = Array.from({ length: length - 1 }, (_, index) => getTile(state.floor, point(state, direction, index + 1).x, point(state, direction, index + 1).y))
  if (!crossed.some(tile => tile && hazardous.has(tile.kind)) || !passableLanding(state, landing)) { log(state, 'Reedwing needs hazardous ground and a clear landing.'); return false }
  state.hero.x = landing.x
  state.hero.y = landing.y
  log(state, 'Reedwing carries you across the hazard.')
  return true
}

const useCordAnchor = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const landing = point(state, direction, overdrive ? 4 : 2)
  if (!passableLanding(state, landing)) { log(state, 'Cord Anchor needs a clear landing.'); return false }
  state.hero.x = landing.x
  state.hero.y = landing.y
  log(state, 'The cord draws you across the gap.')
  return true
}

const useAshway = (state: RunState, direction: Exclude<keyof typeof DIRECTIONS, 'wait'>, overdrive: boolean): boolean => {
  const length = overdrive ? 3 : 2
  const cells = Array.from({ length }, (_, index) => point(state, direction, index + 1))
  const targets = cells.map(cell => ({ cell, tile: getTile(state.floor, cell.x, cell.y) })).filter((entry): entry is { cell: { x: number; y: number }; tile: NonNullable<ReturnType<typeof getTile>> } => Boolean(entry.tile && hazardous.has(entry.tile.kind) && entry.tile.kind !== 'boulder'))
  if (!targets.length) { log(state, 'Ashway Rites need hazardous ground to bind.'); return false }
  state.floor.transientTerrain ??= []
  targets.forEach(({ cell, tile }) => {
    const existing = state.floor.transientTerrain!.find(current => current.x === cell.x && current.y === cell.y)
    if (!existing) state.floor.transientTerrain!.push({ ...cell, original: tile.kind, expiresAt: state.turn + 6 })
    tile.kind = 'floor'
  })
  log(state, 'Warm ash settles into a temporary route.')
  return true
}

export function expireAshways(state: RunState): void {
  const pending = state.floor.transientTerrain ?? []
  const active = pending.filter(current => {
    if (current.expiresAt > state.turn || (state.hero.x === current.x && state.hero.y === current.y)) return true
    const tile = getTile(state.floor, current.x, current.y)
    if (tile?.kind === 'floor') tile.kind = current.original
    return false
  })
  state.floor.transientTerrain = active.length ? active : undefined
}

export function revealMilestones(state: RunState): void {
  for (const current of state.floor.milestones) if (!current.claimed) current.discovered = true
}

export function useTimeKnot(state: RunState): ActionResult {
  const rank = boonRank(state, 'timeKnot')
  const positions = state.hero.safePositions ?? []
  if (!rank) { log(state, 'No Time Knot is bound.'); return [] }
  const current = `${state.hero.x},${state.hero.y}`
  const target = [...positions].reverse().find(point => `${point.x},${point.y}` !== current && isPassable(state.floor, point.x, point.y))
  if (!target) { log(state, 'Time Knot has no earlier safe position.'); return [] }
  state.hero.x = target.x
  state.hero.y = target.y
  state.hero.boons![`timeKnot`] = rank - 1
  if (state.hero.boons!.timeKnot === 0) delete state.hero.boons!.timeKnot
  refreshFov(state)
  log(state, 'Time Knot returns you to an earlier safe position. The world does not rewind.')
  return [event('spell')]
}

export function recordSafePosition(state: RunState): void {
  if (!isPassable(state.floor, state.hero.x, state.hero.y)) return
  const positions = state.hero.safePositions ??= []
  const last = positions.at(-1)
  if (!last || last.x !== state.hero.x || last.y !== state.hero.y) positions.push({ x: state.hero.x, y: state.hero.y })
  if (positions.length > 12) positions.shift()
}
