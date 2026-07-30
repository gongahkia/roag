import { describe, expect, it } from 'vitest'
import { assertAutoplayFailureDiagnosis, diagnoseAutoplayFailure, summarizeAutoplayFailureCodes, type AutoplayFailureDiagnosticInput } from './autoplay-failure-diagnosis'
import { runAutoplay } from './autoplay-runner'
import { createRun } from './test/factories'

const input = (overrides: Partial<AutoplayFailureDiagnosticInput> = {}): AutoplayFailureDiagnosticInput => {
  const report = runAutoplay(createRun(), { mode: 'omniscient', policy: 'clear', turnLimit: 1, captureTrace: true })
  return {
    seed: report.seed,
    partition: 'development',
    mode: report.mode,
    policy: report.policy,
    heuristicProfile: report.heuristicProfile,
    turnLimit: report.policyMetadata.turnBudget,
    outcome: 'stalled',
    replay: report.replay,
    exitPath: report.final.exitPath,
    trace: report.trace.map(({ turn, replay, command, reason, events }) => ({ turn, replay, command, reason, events })),
    resources: { bombsUsed: 0, ropesUsed: 0, selected: 0, deferred: 0, rejected: 0, projectedRouteGains: 0, criticalRouteSelections: 0 },
    tools: report.toolOutcomes,
    optional: report.optionalOutcomes,
    ...overrides
  }
}

describe('autoplay failure diagnosis', () => {
  it('assigns each actionable primary failure code with reproducible evidence', () => {
    expect(diagnoseAutoplayFailure(input({ outcome: 'dead' })).code).toBe('death')
    expect(diagnoseAutoplayFailure(input({ outcome: 'turn-limit' })).code).toBe('timeout')
    expect(diagnoseAutoplayFailure(input({ error: 'invalid target action' })).code).toBe('illegal-action')
    expect(diagnoseAutoplayFailure(input({ mode: 'visible', exitPath: 'terrain-blocked', trace: [{ ...input().trace[0]!, reason: 'find exit' }] })).code).toBe('hidden-route-miss')
    expect(diagnoseAutoplayFailure(input({ exitPath: 'terrain-blocked', resources: { bombsUsed: 1, ropesUsed: 0, selected: 1, deferred: 0, rejected: 0, projectedRouteGains: 0, criticalRouteSelections: 0 } })).code).toBe('resource-waste')
    expect(diagnoseAutoplayFailure(input()).code).toBe('stall')
  })

  it('reports trace replay divergence before outcome classification', () => {
    const diagnosticInput = input()
    const traceDocument = runAutoplay(createRun(), { mode: 'omniscient', policy: 'clear', turnLimit: 1, captureTrace: true }).traceDocument!
    traceDocument.records[0]!.chosen.command = 'x'
    const diagnosis = diagnoseAutoplayFailure({ ...diagnosticInput, traceDocument })
    expect(diagnosis).toMatchObject({ code: 'replay-divergence', reproduction: { seed: diagnosticInput.seed, partition: 'development', mode: 'omniscient', policy: 'clear', turnLimit: diagnosticInput.turnLimit }, finalDecisions: expect.any(Array), resources: diagnosticInput.resources, traversal: { tools: diagnosticInput.tools, optional: diagnosticInput.optional } })
    expect(() => assertAutoplayFailureDiagnosis(diagnosis)).not.toThrow()
  })

  it('summarizes every failure code deterministically', () => {
    const diagnoses = [diagnoseAutoplayFailure(input()), diagnoseAutoplayFailure(input({ outcome: 'dead' }))]
    expect(summarizeAutoplayFailureCodes(diagnoses)).toEqual({ stall: 1, death: 1, 'illegal-action': 0, 'hidden-route-miss': 0, 'resource-waste': 0, timeout: 0, 'replay-divergence': 0 })
  })
})
