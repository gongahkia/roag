import type { Biome, Companion, CompanionControlMode, CompanionRole, RescuedNpc } from '../types'

const roles: readonly CompanionRole[] = ['guard', 'scout', 'pathmaker', 'ritualist']
const traversalTools = ['stoneWedge', 'reedwing', 'cordAnchor', 'ashwayRites', 'antlerPrybar', 'stoneAdze', 'resinFireBasket', 'woodenLeverRoller'] as const
const roleForBiome: Record<Biome, CompanionRole> = { mine: 'guard', wilds: 'scout', caverns: 'pathmaker', ruins: 'ritualist', furnace: 'guard', floodedRuins: 'scout', cliffs: 'pathmaker', burial: 'ritualist', saltFlats: 'scout', frostReliquary: 'guard' }
const nonEmpty = (value: string): boolean => value.trim().length > 0
const nonNegativeInteger = (value: number): boolean => Number.isInteger(value) && value >= 0
export const COMPANION_ACTIVE_CAPACITY = 3
export const COMPANION_RECOVERY_COST = 10
export const COMPANION_RECOVERY_FLOORS = 1
export type CompanionRosterAction = 'recruit' | 'activate' | 'bench'
export type CompanionLodgeAction = CompanionRosterAction | 'beginRecovery' | 'completeRecovery'
export interface CompanionRosterMutation { changed: boolean; message: string; companions: Companion[]; cashSpent?: number }
export interface CompanionRecoveryMutation extends CompanionRosterMutation { cashSpent: number }

export const companionLeadForRescue = (rescue: RescuedNpc, controlMode: CompanionControlMode = 'autonomous'): Companion => ({
  version: 1,
  id: `companion:${rescue.id}`,
  templateId: `rescue:${rescue.biome}`,
  name: rescue.name,
  role: roleForBiome[rescue.biome],
  recruitment: { kind: 'rescue', rescueId: rescue.id, biome: rescue.biome, floor: rescue.floor },
  rosterStatus: 'lead',
  controlMode,
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
  if (companion.injury === 'recovering' && !nonNegativeInteger(companion.recoveryFloors ?? Number.NaN)) errors.push('recovering companion needs remaining floors')
  if (companion.injury !== 'recovering' && companion.recoveryFloors !== undefined) errors.push('only recovering companions may track remaining floors')
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
  const normalized = companion.injury === 'recovering' && companion.recoveryFloors === undefined ? { ...companion, recoveryFloors: COMPANION_RECOVERY_FLOORS } : { ...companion }
  assertCompanion(normalized, rescues)
  return { ...normalized, recruitment: { ...normalized.recruitment }, abilityState: { cooldowns: { ...normalized.abilityState.cooldowns }, retired: [...normalized.abilityState.retired] }, toolState: { ...normalized.toolState } }
}

export const cloneCompanions = (companions: readonly Companion[], rescues?: readonly RescuedNpc[]): Companion[] => {
  const ids = new Set<string>()
  const rescueIds = new Set<string>()
  for (const source of companions) {
    const companion = source.injury === 'recovering' && source.recoveryFloors === undefined ? { ...source, recoveryFloors: COMPANION_RECOVERY_FLOORS } : source
    assertCompanion(companion, rescues)
    if (ids.has(companion.id)) throw new Error(`invalid companion roster: duplicate id ${companion.id}`)
    if (rescueIds.has(companion.recruitment.rescueId)) throw new Error(`invalid companion roster: duplicate recruitment rescue ${companion.recruitment.rescueId}`)
    ids.add(companion.id)
    rescueIds.add(companion.recruitment.rescueId)
  }
  return companions.map(companion => cloneCompanion(companion, rescues))
}

export const companionLeadsForRescues = (rescues: readonly RescuedNpc[], controlMode: CompanionControlMode = 'autonomous'): Companion[] => cloneCompanions(rescues.map(rescue => companionLeadForRescue(rescue, controlMode)), rescues)
export const loseCompanionForRescue = (companion: Companion, rescueId: string): Companion => companion.recruitment.rescueId === rescueId ? { ...cloneCompanion(companion), rosterStatus: 'lost', permanentlyLost: true } : cloneCompanion(companion)
export const addCompanionLeads = (companions: readonly Companion[], rescues: readonly RescuedNpc[], controlMode: CompanionControlMode = 'autonomous'): Companion[] => {
  const next = cloneCompanions(companions, rescues)
  for (const rescue of rescues) if (!next.some(companion => companion.recruitment.rescueId === rescue.id)) next.push(companionLeadForRescue(rescue, controlMode))
  return cloneCompanions(next.map(companion => ({ ...companion, controlMode })), rescues)
}
export const companionRosterAction = (companion: Companion): CompanionRosterAction | undefined => companion.rosterStatus === 'lead' ? 'recruit' : companion.rosterStatus === 'benched' ? 'activate' : companion.rosterStatus === 'active' ? 'bench' : undefined
export const companionLodgeAction = (companion: Companion): CompanionLodgeAction | undefined => companion.permanentlyLost ? undefined : companion.injury === 'injured' ? 'beginRecovery' : companion.injury === 'recovering' ? companion.recoveryFloors === 0 ? 'completeRecovery' : undefined : companionRosterAction(companion)
export const injureCompanion = (companion: Companion): boolean => {
  if (companion.permanentlyLost || companion.injury !== 'healthy') return false
  companion.injury = 'injured'
  companion.rosterStatus = 'benched'
  delete companion.recoveryFloors
  return true
}
export const beginCompanionRecovery = (companions: readonly Companion[], rescues: readonly RescuedNpc[], id: string, cash: number): CompanionRecoveryMutation => {
  const next = cloneCompanions(companions, rescues)
  const companion = next.find(candidate => candidate.id === id)
  if (!companion || companion.permanentlyLost || companion.injury !== 'injured') return { changed: false, message: 'That companion is not awaiting Lodge treatment.', companions: next, cashSpent: 0 }
  if (cash < COMPANION_RECOVERY_COST) return { changed: false, message: `${companion.name}'s treatment needs ${COMPANION_RECOVERY_COST - cash} more cash.`, companions: next, cashSpent: 0 }
  companion.injury = 'recovering'
  companion.rosterStatus = 'benched'
  companion.recoveryFloors = COMPANION_RECOVERY_FLOORS
  return { changed: true, message: `${companion.name} begins recovery: ${COMPANION_RECOVERY_FLOORS} cleared floor remaining.`, companions: cloneCompanions(next, rescues), cashSpent: COMPANION_RECOVERY_COST }
}
export const progressCompanionRecovery = (companions: readonly Companion[], rescues?: readonly RescuedNpc[]): Companion[] => cloneCompanions(companions, rescues?.length ? rescues : undefined).map(companion => companion.injury === 'recovering' && (companion.recoveryFloors ?? 0) > 0 ? { ...companion, recoveryFloors: (companion.recoveryFloors ?? 0) - 1 } : companion)
export const completeCompanionRecovery = (companions: readonly Companion[], rescues: readonly RescuedNpc[], id: string): CompanionRecoveryMutation => {
  const next = cloneCompanions(companions, rescues)
  const companion = next.find(candidate => candidate.id === id)
  if (!companion || companion.permanentlyLost || companion.injury !== 'recovering') return { changed: false, message: 'That companion is not in Lodge recovery.', companions: next, cashSpent: 0 }
  if ((companion.recoveryFloors ?? 0) > 0) return { changed: false, message: `${companion.name} needs ${companion.recoveryFloors} more cleared floor${companion.recoveryFloors === 1 ? '' : 's'} before returning.`, companions: next, cashSpent: 0 }
  companion.injury = 'healthy'
  companion.rosterStatus = 'benched'
  delete companion.recoveryFloors
  return { changed: true, message: `${companion.name} completes Lodge recovery and is ready for the bench.`, companions: cloneCompanions(next, rescues), cashSpent: 0 }
}
export const changeCompanionRoster = (companions: readonly Companion[], rescues: readonly RescuedNpc[], id: string, action: CompanionRosterAction): CompanionRosterMutation => {
  const next = cloneCompanions(companions, rescues)
  const companion = next.find(candidate => candidate.id === id)
  if (!companion) return { changed: false, message: 'That companion record is unavailable.', companions: next }
  if (action === 'recruit') {
    if (companion.rosterStatus !== 'lead') return { changed: false, message: `${companion.name} is already recruited or unavailable.`, companions: next }
    if (next.some(candidate => candidate.id !== companion.id && !candidate.permanentlyLost && candidate.templateId === companion.templateId)) return { changed: false, message: `${companion.name}'s template is already represented in the lodge.`, companions: next }
    companion.rosterStatus = 'benched'
    return { changed: true, message: `${companion.name} joined the lodge bench at no cost.`, companions: cloneCompanions(next, rescues) }
  }
  if (action === 'activate') {
    if (companion.rosterStatus !== 'benched') return { changed: false, message: `${companion.name} cannot be activated.`, companions: next }
    if (companion.injury !== 'healthy') return { changed: false, message: `${companion.name} is unavailable while ${companion.injury}.`, companions: next }
    if (next.filter(candidate => candidate.rosterStatus === 'active').length >= COMPANION_ACTIVE_CAPACITY) return { changed: false, message: `Active companion capacity is ${COMPANION_ACTIVE_CAPACITY}.`, companions: next }
    companion.rosterStatus = 'active'
    return { changed: true, message: `${companion.name} joined the active party.`, companions: cloneCompanions(next, rescues) }
  }
  if (companion.rosterStatus !== 'active') return { changed: false, message: `${companion.name} is not active.`, companions: next }
  companion.rosterStatus = 'benched'
  return { changed: true, message: `${companion.name} returned to the lodge bench.`, companions: cloneCompanions(next, rescues) }
}
