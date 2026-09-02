import { describe, expect, it } from 'vitest'
import { WORLD_ERA_LIMITS, WORLD_ERA_MODEL_VERSION, WORLD_ERA_PACE_MULTIPLIERS, WORLD_ERA_REMIX_CYCLE_UNITS, WORLD_ERA_THRESHOLDS, WorldEraContractError, advanceWorldEraForTemporalAction, createWorldEraState, recordDurableJomonGrowthEvidence, validateWorldEraState, worldEraProjection, type DurableJomonGrowthEvidence, type WorldEraContext } from './world-era'

const contextFor = (eraPace: WorldEraContext['eraPace'] = 'measured', worldTime = 0): WorldEraContext => ({
  worldId: 'world:era-test',
  creationDigest: 'era-test-digest',
  eraPace,
  worldTime,
  jomonVesselId: 'vessel:jomon',
  jomonPropIds: ['prop:chart-table', 'prop:task-ledger']
})

const advance = (state: ReturnType<typeof createWorldEraState>, minutes: number, eraPace: WorldEraContext['eraPace'] = 'measured') => advanceWorldEraForTemporalAction(state, contextFor(eraPace, state.activePlayMinutes + minutes), {
  startedAtWorldTime: state.activePlayMinutes,
  atWorldTime: state.activePlayMinutes + minutes,
  durationMinutes: minutes
})

const evidence = (id: string, kind: DurableJomonGrowthEvidence['kind'], atWorldTime: number): DurableJomonGrowthEvidence => ({
  id,
  kind,
  source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
  atWorldTime
})

describe('versioned world-era contract', () => {
  it('starts at zero in base and crosses exact measured thresholds without ambiguity', () => {
    const start = createWorldEraState(contextFor())
    const before = advance(start, WORLD_ERA_THRESHOLDS.ngPlus - 1)
    const atNgPlus = advance(before, 1)
    const atNgPlusPlus = advance(atNgPlus, WORLD_ERA_THRESHOLDS.ngPlusPlus - WORLD_ERA_THRESHOLDS.ngPlus)

    expect(start).toMatchObject({ era: 'base', activePlayMinutes: 0, progressUnits: 0, transitions: [] })
    expect(before.era).toBe('base')
    expect(atNgPlus).toMatchObject({ era: 'ng-plus', activePlayMinutes: WORLD_ERA_THRESHOLDS.ngPlus, progressUnits: WORLD_ERA_THRESHOLDS.ngPlus })
    expect(atNgPlus.transitions[0]).toMatchObject({ from: 'base', to: 'ng-plus', atWorldTime: WORLD_ERA_THRESHOLDS.ngPlus, modelVersion: WORLD_ERA_MODEL_VERSION, evidence: { trigger: 'active-play-time', thresholdUnits: WORLD_ERA_THRESHOLDS.ngPlus } })
    expect(atNgPlusPlus).toMatchObject({ era: 'ng-plus-plus', activePlayMinutes: WORLD_ERA_THRESHOLDS.ngPlusPlus, transitions: expect.arrayContaining([expect.objectContaining({ from: 'ng-plus', to: 'ng-plus-plus', atWorldTime: WORLD_ERA_THRESHOLDS.ngPlusPlus })]) })
  })

  it.each([
    ['measured', 1],
    ['brisk', 2],
    ['pressing', 3]
  ] as const)('uses the explicit %s active-time multiplier', (eraPace, multiplier) => {
    const minutes = Math.ceil(WORLD_ERA_THRESHOLDS.ngPlus / multiplier)
    const state = advance(createWorldEraState(contextFor(eraPace)), minutes, eraPace)

    expect(WORLD_ERA_PACE_MULTIPLIERS[eraPace]).toBe(multiplier)
    expect(state.pacedActivePlayUnits).toBe(minutes * multiplier)
    expect(state.era).toBe('ng-plus')
    expect(state.transitions[0]!.atWorldTime).toBe(minutes)
  })

  it('accepts only typed, source-checked durable growth and canonicalizes its ledger', () => {
    const start = createWorldEraState(contextFor())
    const afterB = recordDurableJomonGrowthEvidence(start, contextFor(), evidence('growth:b', 'workspace-refit', 0))
    const afterA = recordDurableJomonGrowthEvidence(afterB, contextFor(), evidence('growth:a', 'physical-expansion', 0))
    const afterAFirst = recordDurableJomonGrowthEvidence(start, contextFor(), evidence('growth:a', 'physical-expansion', 0))
    const afterBothOrders = recordDurableJomonGrowthEvidence(afterAFirst, contextFor(), evidence('growth:b', 'workspace-refit', 0))

    expect(afterA.durableGrowthEvidence.map(item => item.id)).toEqual(['growth:a', 'growth:b'])
    expect(afterA.durableGrowthUnits).toBe(3_600)
    expect(worldEraProjection(afterA)).toEqual(worldEraProjection(afterBothOrders))
    expect(() => recordDurableJomonGrowthEvidence(afterA, contextFor(), evidence('growth:a', 'small-craft', 0))).toThrow(WorldEraContractError)

    const forged = structuredClone(afterA) as unknown as { durableGrowthEvidence: Array<Record<string, unknown>> }
    forged.durableGrowthEvidence[0]!.source = { kind: 'jomon-prop', id: 'prop:not-jomon' }
    forged.durableGrowthEvidence[0]!.weight = 999_999
    expect(validateWorldEraState(contextFor(), forged).map(item => item.code)).toEqual(expect.arrayContaining(['world-era.malformed-growth-evidence', 'world-era.invalid-growth-source']))

    expect(() => recordDurableJomonGrowthEvidence(start, contextFor(), {
      id: 'growth:future',
      kind: 'capacity-upgrade',
      source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
      atWorldTime: 1
    })).toThrow(WorldEraContractError)
    expect(() => recordDurableJomonGrowthEvidence(start, contextFor(), {
      id: 'growth:not-jomon',
      kind: 'small-craft',
      source: { kind: 'jomon-prop', id: 'prop:not-jomon' },
      atWorldTime: 0
    })).toThrow('world-era.invalid-growth-source')
  })

  it('records ordered intermediate transitions exactly once when one accepted time action crosses both thresholds', () => {
    const state = advance(createWorldEraState(contextFor()), WORLD_ERA_THRESHOLDS.ngPlusPlus)

    expect(state.transitions.map(transition => `${transition.from}:${transition.to}:${transition.atWorldTime}`)).toEqual([
      `base:ng-plus:${WORLD_ERA_THRESHOLDS.ngPlus}`,
      `ng-plus:ng-plus-plus:${WORLD_ERA_THRESHOLDS.ngPlusPlus}`
    ])
    expect(new Set(state.transitions.map(transition => transition.id)).size).toBe(2)
  })

  it('plateaus NG++ raw escalation while remix cycle/profile remain deterministic', () => {
    const reached = advance(createWorldEraState(contextFor()), WORLD_ERA_THRESHOLDS.ngPlusPlus)
    const nextCycle = advance(reached, WORLD_ERA_REMIX_CYCLE_UNITS)
    const replay = advance(advance(createWorldEraState(contextFor()), WORLD_ERA_THRESHOLDS.ngPlusPlus), WORLD_ERA_REMIX_CYCLE_UNITS)

    expect(reached.profile).toMatchObject({ era: 'ng-plus-plus', rawEscalation: 'plateau', remixCycle: 0 })
    expect(nextCycle.profile).toMatchObject({ era: 'ng-plus-plus', rawEscalation: 'plateau', remixCycle: 1 })
    expect(nextCycle.profile.remixIdentity).not.toBe(reached.profile.remixIdentity)
    expect(nextCycle.profile).toEqual(replay.profile)
    expect(nextCycle.profile.pressureTags).toHaveLength(3)
    expect(nextCycle.profile.opportunityTags).toHaveLength(3)
  })

  it('has a partition-invariant canonical projection without action identity input', () => {
    const once = advance(createWorldEraState(contextFor()), 10)
    const partitioned = advance(advance(createWorldEraState(contextFor()), 4), 6)

    expect(worldEraProjection(partitioned)).toEqual(worldEraProjection(once))
    expect(partitioned.transitions).toEqual(once.transitions)
  })

  it('bounds the canonical durable-growth ledger and its two transition records without evicting accepted evidence', () => {
    let state = createWorldEraState(contextFor())
    for (let index = 0; index < WORLD_ERA_LIMITS.growthEvidence; index++) {
      state = recordDurableJomonGrowthEvidence(state, contextFor(), evidence(`growth:bound:${index.toString().padStart(2, '0')}`, 'tool-installation', 0))
    }
    const beforeOverflow = structuredClone(state)

    expect(state.durableGrowthEvidence).toHaveLength(WORLD_ERA_LIMITS.growthEvidence)
    expect(state.durableGrowthEvidence.map(item => item.id)).toEqual([...state.durableGrowthEvidence.map(item => item.id)].sort())
    expect(state.transitions).toHaveLength(WORLD_ERA_LIMITS.transitions)
    expect(() => recordDurableJomonGrowthEvidence(state, contextFor(), evidence('growth:bound:overflow', 'tool-installation', 0))).toThrow('world-era.growth-limit')
    expect(state).toEqual(beforeOverflow)
  })

  it('rejects malformed time, source, tokens, model versions, and derived totals fail-closed', () => {
    const valid = advance(createWorldEraState(contextFor()), 1)
    const token = structuredClone(valid)
    token.transitions = [{
      id: 'era-transition:forged',
      token: 1,
      from: 'base',
      to: 'ng-plus',
      atWorldTime: 1,
      modelVersion: WORLD_ERA_MODEL_VERSION,
      evidence: { kind: 'combined-progress-threshold', trigger: 'active-play-time', thresholdUnits: WORLD_ERA_THRESHOLDS.ngPlus, activePlayMinutes: 1, pacedActivePlayUnits: 1, durableGrowthUnits: 0 }
    }]
    const badTotals = structuredClone(valid)
    badTotals.progressUnits++
    const badModel = structuredClone(valid)
    badModel.modelVersion = 99 as never

    expect(validateWorldEraState(contextFor('measured', 1), token).map(item => item.code)).toContain('world-era.invalid-transition-token')
    expect(validateWorldEraState(contextFor('measured', 1), badTotals).map(item => item.code)).toContain('world-era.invalid-progress-units')
    expect(validateWorldEraState(contextFor('measured', 1), badModel).map(item => item.code)).toContain('world-era.invalid-model-version')
    expect(() => advanceWorldEraForTemporalAction(valid, contextFor('measured', 1), { startedAtWorldTime: 1, atWorldTime: 1, durationMinutes: 0 })).toThrow(WorldEraContractError)
    expect(() => createWorldEraState(contextFor('pressing', Math.floor(Number.MAX_SAFE_INTEGER / 3) + 1))).toThrow(WorldEraContractError)
  })
})
