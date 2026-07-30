import { comparePolicyScores, policyScoreTuple, type PolicyScore, type PolicyScoreTuple } from './autoplay-policy'

export const AUTOPLAY_PROMOTION_REPORT_VERSION = 1 as const
export interface AutoplayAblation { id: string; history: boolean; belief: boolean; actionMasking: boolean; trainingRecords: number; score: PolicyScore; repeats?: number }
export interface AutoplayPromotionReport { version: typeof AUTOPLAY_PROMOTION_REPORT_VERSION; partition: 'held-out'; heuristic: PolicyScore; candidate: AutoplayAblation; leakageAudit: { visibleOnly: boolean; forbiddenFields: string[] }; verdict: 'promotable' | 'rejected'; reason: string; comparison: PolicyScoreTuple }

const forbidden = ['replay', 'layoutId', 'macroRecipeId', 'routeContractId', 'objectiveId', 'escalation', 'hiddenMap', 'fullMap']
const hidden = (value: unknown): string[] => {
  if (Array.isArray(value)) return value.flatMap(hidden)
  if (!value || typeof value !== 'object') return []
  return Object.entries(value as Record<string, unknown>).flatMap(([key, child]) => [...(forbidden.includes(key) ? [key] : []), ...hidden(child)])
}
export const auditVisiblePolicyLeakage = (value: unknown): { visibleOnly: boolean; forbiddenFields: string[] } => {
  const forbiddenFields = [...new Set(hidden(value))].sort()
  return { visibleOnly: !forbiddenFields.length, forbiddenFields }
}
export const evaluateAutoplayPromotion = (heuristic: PolicyScore, candidate: AutoplayAblation, visibleInput: unknown): AutoplayPromotionReport => {
  const leakageAudit = auditVisiblePolicyLeakage(visibleInput)
  const comparison = policyScoreTuple(candidate.score)
  const policyComparison = comparePolicyScores(candidate.score, heuristic)
  const verdict = leakageAudit.visibleOnly && policyComparison <= 0 ? 'promotable' : 'rejected'
  const reason = !leakageAudit.visibleOnly ? `visible leakage: ${leakageAudit.forbiddenFields.join(',')}` : policyComparison > 0 ? 'candidate loses an earlier lexicographic objective' : policyComparison < 0 ? 'candidate wins lexicographically' : 'candidate ties lexicographically'
  return { version: AUTOPLAY_PROMOTION_REPORT_VERSION, partition: 'held-out', heuristic: structuredClone(heuristic), candidate: structuredClone(candidate), leakageAudit, verdict, reason, comparison }
}
