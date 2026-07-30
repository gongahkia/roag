import { describe, expect, it } from 'vitest'
import { auditVisiblePolicyLeakage, evaluateAutoplayPromotion } from './autoplay-promotion'

const heuristic = { campaignClears: 2, deaths: 1, stalls: 1, explorationValue: 30, resourceEfficiency: 8 }
const candidate = (score = heuristic) => ({ id: 'rppo-history-mask-100', history: true, belief: false, actionMasking: true, trainingRecords: 100, repeats: 3, score })
describe('autoplay promotion gate', () => {
  it('promotes a lexicographic winner and rejects an earlier loser', () => {
    expect(evaluateAutoplayPromotion(heuristic, candidate({ ...heuristic, explorationValue: 31 }), { observation: {} }).verdict).toBe('promotable')
    expect(evaluateAutoplayPromotion(heuristic, candidate({ ...heuristic, deaths: 2, explorationValue: 999 }), { observation: {} })).toMatchObject({ verdict: 'rejected', reason: 'candidate loses an earlier lexicographic objective' })
  })
  it('rejects visible-mode leakage', () => {
    expect(auditVisiblePolicyLeakage({ replay: { layoutId: 'x' } })).toEqual({ visibleOnly: false, forbiddenFields: ['layoutId', 'replay'] })
    expect(evaluateAutoplayPromotion(heuristic, candidate(), { hiddenMap: {} }).verdict).toBe('rejected')
  })
})
