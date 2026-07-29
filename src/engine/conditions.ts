import type { ConditionKind, ConditionState } from '../types'

export interface ConditionTarget { conditions?: ConditionState[] }
export interface ConditionTick { burningDamage: number; expired: ConditionKind[] }

export const hasCondition = (target: ConditionTarget, kind: ConditionKind): boolean => Boolean(target.conditions?.some(condition => condition.kind === kind))

export const addCondition = (target: ConditionTarget, condition: ConditionState): void => {
  if (!Number.isInteger(condition.duration) || condition.duration < 1 || !Number.isFinite(condition.potency) || condition.potency < 1) throw new Error(`invalid condition: ${condition.kind}`)
  const existing = target.conditions?.find(current => current.kind === condition.kind)
  if (existing) { existing.duration = Math.max(existing.duration, condition.duration); existing.potency = Math.max(existing.potency, condition.potency); return }
  target.conditions = [...(target.conditions ?? []), { ...condition }]
}

export const tickConditions = (target: ConditionTarget): ConditionTick => {
  let burningDamage = 0
  const expired: ConditionKind[] = []
  const remaining: ConditionState[] = []
  for (const condition of target.conditions ?? []) {
    if (condition.kind === 'burning') burningDamage += condition.potency
    const next = { ...condition, duration: condition.duration - 1 }
    if (next.duration > 0) remaining.push(next)
    else expired.push(condition.kind)
  }
  target.conditions = remaining
  return { burningDamage, expired }
}

export const conditionSpeed = (target: ConditionTarget, speed: number): number => {
  const slow = target.conditions?.filter(condition => condition.kind === 'slowed').reduce((total, condition) => total + condition.potency, 0) ?? 0
  return Math.max(1, speed - slow * 20)
}

export const modifyIncomingDamage = (target: ConditionTarget, amount: number): number => {
  const shield = target.conditions?.filter(condition => condition.kind === 'shielded').reduce((total, condition) => total + condition.potency, 0) ?? 0
  const mark = target.conditions?.filter(condition => condition.kind === 'marked').reduce((total, condition) => total + condition.potency, 0) ?? 0
  return Math.max(1, amount - shield + mark)
}
