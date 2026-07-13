import type { Actor, RescuedNpc, RunState } from '../types'

export const recordRescue = (state: RunState, actor: Actor | undefined): RescuedNpc => {
  const biome = state.area ?? state.floor.biome
  const npc: RescuedNpc = { id: `rescue:${biome}:${state.floor.index}:${actor?.id ?? `${state.hero.x},${state.hero.y}`}`, name: actor?.name ?? 'Stranded Traveler', biome, floor: state.areaFloor ?? state.floor.index % 4 }
  const roster = state.rescuedNpcs ??= []
  if (!roster.some(existing => existing.id === npc.id)) roster.push(npc)
  return npc
}
