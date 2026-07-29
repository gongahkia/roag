export type EquipmentEffectKind = 'passive' | 'triggered' | 'action'
export type EquipmentTrigger = 'hit' | 'hurt' | 'spell' | 'move'
export interface EquipmentEffect { id: string; kind: EquipmentEffectKind; requires?: readonly string[]; excludes?: readonly string[]; add?: Readonly<Record<string, number>>; multiply?: Readonly<Record<string, number>>; trigger?: EquipmentTrigger; actionId?: string }

export const validateEquipmentEffects = (effects: readonly EquipmentEffect[] | undefined, itemId: string): void => {
  const ids = new Set<string>()
  for (const effect of effects ?? []) {
    if (!effect.id || ids.has(effect.id)) throw new Error(`invalid equipment effect on ${itemId}`)
    ids.add(effect.id)
    if (!['passive', 'triggered', 'action'].includes(effect.kind)) throw new Error(`invalid equipment effect kind on ${itemId}`)
    if (effect.kind === 'triggered' && !effect.trigger) throw new Error(`missing equipment trigger on ${itemId}`)
    if (effect.trigger && !['hit', 'hurt', 'spell', 'move'].includes(effect.trigger)) throw new Error(`invalid equipment trigger on ${itemId}`)
    if (effect.kind !== 'triggered' && effect.trigger) throw new Error(`unexpected equipment trigger on ${itemId}`)
    if (effect.kind === 'action' && !effect.actionId) throw new Error(`missing equipment action on ${itemId}`)
    if (effect.kind !== 'action' && effect.actionId) throw new Error(`unexpected equipment action on ${itemId}`)
    for (const value of [...Object.values(effect.add ?? {}), ...Object.values(effect.multiply ?? {})]) if (!Number.isFinite(value)) throw new Error(`invalid equipment effect value on ${itemId}`)
  }
}
