import { describe, expect, it } from 'vitest'
import { chooseAutoplayInferenceAction } from './autoplay-inference'

const heuristic = { command: 'h', reason: 'heuristic', score: 1 }
const legalCandidates = [heuristic, { command: 'l', reason: 'legal', score: 0 }]
const artifact = { version: 1 as const, kind: 'jomon-approved-visible-policy' as const, promotion: { verdict: 'promotable' as const, partition: 'held-out' as const, objective: 'campaign-clears-deaths-stalls-exploration-resources' as const }, actions: { state: 'l' } }
describe('autoplay inference adapter', () => {
  it('preserves heuristic behavior while disabled or malformed', () => {
    expect(chooseAutoplayInferenceAction({ fingerprint: 'state', heuristic, legalCandidates })).toMatchObject({ selected: 'h', reason: 'disabled' })
    expect(chooseAutoplayInferenceAction({ enabled: true, artifact: {}, fingerprint: 'state', heuristic, legalCandidates })).toMatchObject({ selected: 'h', reason: 'artifact-invalid' })
  })
  it('masks illegal inference and records legal shadow disagreement', () => {
    expect(chooseAutoplayInferenceAction({ enabled: true, artifact: { ...artifact, actions: { state: 'q' } }, fingerprint: 'state', heuristic, legalCandidates })).toMatchObject({ selected: 'h', reason: 'illegal-action', disagreement: true })
    expect(chooseAutoplayInferenceAction({ enabled: true, artifact, fingerprint: 'state', heuristic, legalCandidates })).toMatchObject({ selected: 'l', reason: 'inference-selected', disagreement: true })
  })
})
