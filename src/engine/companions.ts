import type { Biome, Companion, CompanionRole, RescuedNpc } from '../types'

const roles: readonly CompanionRole[] = ['guard', 'scout', 'pathmaker', 'ritualist']
const traversalTools = ['stoneWedge', 'reedwing', 'cordAnchor', 'ashwayRites', 'antlerPrybar', 'stoneAdze', 'resinFireBasket', 'woodenLeverRoller'] as const
const roleForBiome: Record<Biome, CompanionRole> = { mine: 'guard', wilds: 'scout', caverns: 'pathmaker', ruins: 'ritualist', furnace: 'guard', floodedRuins: 'scout', cliffs: 'pathmaker', burial: 'ritualist', saltFlats: 'scout', frostReliquary: 'guard' }
const nonEmpty = (value: string): boolean => value.trim().length > 0
const nonNegativeInteger = (value: number): boolean => Number.isInteger(value) && value >= 0

export const companionLeadForRescue = (rescue: RescuedNpc): Companion => ({
  version: 1,
  id: `companion:${rescue.id}`,
  templateId: `rescue:${rescue.biome}`,
  name: rescue.name,
  role: roleForBiome[rescue.biome],
  recruitment: { kind: 'rescue', rescueId: rescue.id, biome: rescue.biome, floor: rescue.floor },
  rosterStatus: 'lead',
  controlMode: 'autonomous',
  injury: 'healthy',
  abilityState: { cooldowns: {}, retired: [] },
  toolState: { cooldown: 0, retired: false },
  permanentlyLost: false
})

export const companionErrors = (companion: Companion, rescues?: readonly RescuedNpc[]): string[] => {
  const errors: string[] = []
  if (companion.version !== 1) errors.push('unsupported version')
  if (!nonEmpty(companion.id)) errors.push('missing id')
  if (!nonEmpty(companion.templateId)) errors.push('missing template id')
  if (!nonEmpty(companion.name)) errors.push('missing name')
  if (!roles.includes(companion.role)) errors.push('invalid role')
  if (companion.recruitment.kind !== 'rescue' || !nonEmpty(companion.recruitment.rescueId) || !nonNegativeInteger(companion.recruitment.floor)) errors.push('invalid recruitment source')
  if (!['lead', 'benched', 'active', 'lost'].includes(companion.rosterStatus)) errors.push('invalid roster status')
  if (!['autonomous', 'direct'].includes(companion.controlMode)) errors.push('invalid control mode')
  if (!['healthy', 'injured', 'recovering'].includes(companion.injury)) errors.push('invalid injury state')
  if (!Object.values(companion.abilityState.cooldowns).every(nonNegativeInteger)) errors.push('invalid ability cooldown')
  if (companion.abilityState.retired.some(id => !nonEmpty(id)) || new Set(companion.abilityState.retired).size !== companion.abilityState.retired.length) errors.push('invalid retired abilities')
  if (!nonNegativeInteger(companion.toolState.cooldown)) errors.push('invalid tool cooldown')
  if (companion.toolState.equipped !== undefined && !traversalTools.includes(companion.toolState.equipped)) errors.push('invalid equipped tool')
  if (companion.toolState.retired && companion.toolState.equipped !== undefined) errors.push('retired tool cannot remain equipped')
  if (companion.permanentlyLost !== (companion.rosterStatus === 'lost')) errors.push('permanent loss must match lost roster status')
  if (rescues) {
    const rescue = rescues.find(candidate => candidate.id === companion.recruitment.rescueId)
    if (!rescue && !companion.permanentlyLost) errors.push(`recruitment rescue ${companion.recruitment.rescueId} is missing`)
    if (rescue && (rescue.biome !== companion.recruitment.biome || rescue.floor !== companion.recruitment.floor)) errors.push(`recruitment source ${companion.recruitment.rescueId} does not match rescue history`)
  }
  return errors
}

export const assertCompanion = (companion: Companion, rescues?: readonly RescuedNpc[]): Companion => {
  const errors = companionErrors(companion, rescues)
  if (errors.length) throw new Error(`invalid companion ${companion.id || '<unknown>'}: ${errors.join('; ')}`)
  return companion
}

export const cloneCompanion = (companion: Companion, rescues?: readonly RescuedNpc[]): Companion => {
  assertCompanion(companion, rescues)
  return { ...companion, recruitment: { ...companion.recruitment }, abilityState: { cooldowns: { ...companion.abilityState.cooldowns }, retired: [...companion.abilityState.retired] }, toolState: { ...companion.toolState } }
}

export const cloneCompanions = (companions: readonly Companion[], rescues?: readonly RescuedNpc[]): Companion[] => {
  const ids = new Set<string>()
  const rescueIds = new Set<string>()
  for (const companion of companions) {
    assertCompanion(companion, rescues)
    if (ids.has(companion.id)) throw new Error(`invalid companion roster: duplicate id ${companion.id}`)
    if (rescueIds.has(companion.recruitment.rescueId)) throw new Error(`invalid companion roster: duplicate recruitment rescue ${companion.recruitment.rescueId}`)
    ids.add(companion.id)
    rescueIds.add(companion.recruitment.rescueId)
  }
  return companions.map(companion => cloneCompanion(companion, rescues))
}

export const companionLeadsForRescues = (rescues: readonly RescuedNpc[]): Companion[] => cloneCompanions(rescues.map(companionLeadForRescue), rescues)
export const loseCompanionForRescue = (companion: Companion, rescueId: string): Companion => companion.recruitment.rescueId === rescueId ? { ...cloneCompanion(companion), rosterStatus: 'lost', permanentlyLost: true } : cloneCompanion(companion)
