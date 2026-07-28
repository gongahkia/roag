import type { MonsterDefinition } from './content'
import { monsterRoleFor, terrainAffinityFor } from './content'
import type { Biome, MonsterRole, TacticalEncounter, TileKind } from './types'
import type { PlacementRequirements } from './placement-contract'

export interface DirectedEncounterPlan {
  archetype: TacticalEncounter
  answer: string
  roles: readonly MonsterRole[]
  requirements: PlacementRequirements
  fallback: PlacementRequirements
}

export interface EncounterDirectorInput { biome: Biome; areaFloor: number; routePosition: number; pilot: boolean }

const biomeOrder: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']

const plans: Record<TacticalEncounter, Omit<DirectedEncounterPlan, 'requirements' | 'fallback'> & { requirements: (pilot: boolean, terrain: readonly TileKind[]) => PlacementRequirements; fallback: PlacementRequirements }> = {
  guardPost: { archetype: 'guardPost', answer: 'draw the guard away from cover', roles: ['guard', 'pursuer'], requirements: () => ({ nodeKinds: ['objective'], minDistance: 8, cover: true, chokepoint: false }), fallback: { minDistance: 7, cover: true, chokepoint: false } },
  ambush: { archetype: 'ambush', answer: 'take the safe route or break line of sight', roles: ['ambusher', 'skirmisher'], requirements: pilot => pilot ? { edgeModes: ['costly', 'optional'], optional: true, minDistance: 9, cover: true, chokepoint: false } : { minDistance: 8, cover: true, chokepoint: false }, fallback: { minDistance: 7, cover: true, chokepoint: false } },
  artilleryCover: { archetype: 'artilleryCover', answer: 'break line of sight or close on the artillery', roles: ['artillery', 'guard'], requirements: pilot => pilot ? { edgeModes: ['costly'], routeCost: 'costly', minDistance: 10, cover: true, chokepoint: false } : { minDistance: 8, cover: true, chokepoint: false }, fallback: { minDistance: 7, cover: true, chokepoint: false } },
  pursuitLane: { archetype: 'pursuitLane', answer: 'use terrain or reach to control the approach', roles: ['pursuer', 'skirmisher'], requirements: pilot => pilot ? { edgeModes: ['safe'], routeCost: 'safe', minDistance: 8, chokepoint: false } : { minDistance: 7, chokepoint: false }, fallback: { minDistance: 7, chokepoint: false } },
  nativeTerrainPack: { archetype: 'nativeTerrainPack', answer: 'leave the enemy\'s native terrain', roles: ['pursuer', 'controller', 'skirmisher'], requirements: (_pilot, terrain) => ({ terrain: [...terrain], minDistance: 8, chokepoint: false }), fallback: { minDistance: 7, chokepoint: false } },
  objectiveDefense: { archetype: 'objectiveDefense', answer: 'draw defenders away from the objective', roles: ['guard', 'support', 'controller'], requirements: pilot => pilot ? { nodeKinds: ['objective'], minDistance: 10, cover: true, chokepoint: false } : { minDistance: 8, cover: true, chokepoint: false }, fallback: { minDistance: 7, cover: true, chokepoint: false } },
  roamingThreat: { archetype: 'roamingThreat', answer: 'avoid the roaming group, then isolate it', roles: ['scavenger', 'skirmisher', 'pursuer'], requirements: pilot => pilot ? { edgeModes: ['optional'], optional: true, minDistance: 10, chokepoint: false } : { minDistance: 8, chokepoint: false }, fallback: { minDistance: 7, chokepoint: false } }
}

const schedule: readonly TacticalEncounter[] = ['guardPost', 'ambush', 'artilleryCover', 'pursuitLane', 'nativeTerrainPack', 'objectiveDefense', 'roamingThreat']

export const encounterPlansFor = ({ biome, areaFloor, routePosition, pilot }: EncounterDirectorInput, nativeTerrain: readonly TileKind[]): DirectedEncounterPlan[] => {
  const start = (biomeOrder.indexOf(biome) + areaFloor + routePosition) % schedule.length
  const count = areaFloor >= 2 ? 2 : 1
  return Array.from({ length: count }, (_, index) => {
    const plan = plans[schedule[(start + index * 3) % schedule.length]]
    return { ...plan, roles: [...plan.roles], requirements: plan.requirements(pilot, nativeTerrain), fallback: { ...plan.fallback } }
  })
}

export const membersForEncounter = (areaFloor: number, routePosition: number): number => Math.min(4, 2 + Math.floor(areaFloor / 2) + Math.floor(routePosition / 5))

export const definitionForEncounter = (definitions: readonly MonsterDefinition[], plan: DirectedEncounterPlan, member: number, nativeTerrain: readonly TileKind[]): MonsterDefinition | undefined => {
  const role = plan.roles[member % plan.roles.length]
  const byRole = definitions.filter(definition => monsterRoleFor(definition) === role)
  const native = plan.archetype === 'nativeTerrainPack' ? byRole.filter(definition => terrainAffinityFor(definition).some(kind => nativeTerrain.includes(kind))) : byRole
  return native[0] ?? byRole[0] ?? definitions.find(definition => plan.archetype !== 'nativeTerrainPack' || terrainAffinityFor(definition).some(kind => nativeTerrain.includes(kind))) ?? definitions[0]
}
