import { describe, expect, it } from 'vitest'
import { MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES, MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES } from './performance-budget'
import {
  MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION,
  MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX,
  PERFORMANCE_FIXTURE_CATALOGUE_ACTIVE_IDS,
  PERFORMANCE_FIXTURE_CATALOGUE_CHRONICLE_IDS,
  PERFORMANCE_FIXTURE_DUE_EVENT_SEQUENCE_MINUTES,
  PERFORMANCE_FIXTURE_DUE_EVENT_TOTAL_MINUTES,
  PERFORMANCE_FIXTURE_MEASUREMENT_BOUNDARIES,
  createPerformanceFixtureCatalogue,
  projectPerformanceFixturePresentation,
  resolveMedievalPerformanceFixtureMatrix,
  runPerformanceFixtureScenario,
  selectedPerformanceFixtureWorld
} from './performance-fixtures'

describe('medieval performance fixture contract', () => {
  it('defines the complete preset by fidelity matrix through the production configuration resolver while preserving the diagonal baseline', () => {
    expect(MEDIEVAL_PERFORMANCE_FIXTURE_CONTRACT_VERSION).toBe(1)
    expect(MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.map(fixture => [fixture.id, fixture.seed])).toEqual([
      ['sheltered-reach-focused', 'performance-sheltered-reach'],
      ['sheltered-reach-balanced', 'performance-sheltered-reach-balanced'],
      ['sheltered-reach-deep', 'performance-sheltered-reach-deep'],
      ['watershed-focused', 'performance-watershed-focused'],
      ['watershed-balanced', 'performance-watershed'],
      ['watershed-deep', 'performance-watershed-deep'],
      ['far-coast-focused', 'performance-far-coast-focused'],
      ['far-coast-balanced', 'performance-far-coast-balanced'],
      ['far-coast-deep', 'performance-far-coast']
    ])
    expect(MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.filter(fixture => ['sheltered-reach-focused', 'watershed-balanced', 'far-coast-deep'].includes(fixture.id))).toEqual(MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES)

    const resolved = resolveMedievalPerformanceFixtureMatrix()
    expect(resolved.map(item => [item.fixture.id, item.resolvedConfiguration.preset, item.resolvedConfiguration.simulationFidelity, item.instantiatedPersistentPeople])).toEqual([
      ['sheltered-reach-focused', 'sheltered-reach', 'focused', 6],
      ['sheltered-reach-balanced', 'sheltered-reach', 'balanced', 6],
      ['sheltered-reach-deep', 'sheltered-reach', 'deep', 6],
      ['watershed-focused', 'watershed', 'focused', 6],
      ['watershed-balanced', 'watershed', 'balanced', 6],
      ['watershed-deep', 'watershed', 'deep', 6],
      ['far-coast-focused', 'far-coast', 'focused', 6],
      ['far-coast-balanced', 'far-coast', 'balanced', 6],
      ['far-coast-deep', 'far-coast', 'deep', 6]
    ])
  })

  it('records reproducible selected-world, due-event, retained-state, canonical-byte, and pure-presentation regressions', () => {
    const scenarios = MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.map(runPerformanceFixtureScenario)
    expect(MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.map(selectedPerformanceFixtureWorld).map(world => world.state.courier.initialCourierId)).toEqual(Array(9).fill('crew:0'))
    expect(scenarios).toEqual(MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX.map(runPerformanceFixtureScenario))
    expect(scenarios.map(scenario => [
      scenario.fixtureId,
      scenario.envelopeSizes.before.bytes,
      scenario.envelopeSizes.after.bytes,
      scenario.envelopeSizes.deltaBytes,
      scenario.retainedStateGrowth.delta.catchUpCursors,
      scenario.retainedStateGrowth.delta.catchUpRecords,
      scenario.retainedStateGrowth.delta.causalHistoryTail,
      scenario.dueEvents.singleProjectionCanonicalBytes
    ])).toEqual([
      ['sheltered-reach-focused', 190842, 255769, 64927, 12, 19, 2, 2796],
      ['sheltered-reach-balanced', 191092, 273868, 82776, 16, 25, 2, 3716],
      ['sheltered-reach-deep', 190821, 279353, 88532, 18, 27, 2, 4212],
      ['watershed-focused', 194934, 259808, 64874, 12, 19, 2, 2806],
      ['watershed-balanced', 194803, 283088, 88285, 17, 27, 2, 3959],
      ['watershed-deep', 194808, 294533, 99725, 21, 31, 2, 4950],
      ['far-coast-focused', 208866, 273659, 64793, 12, 19, 2, 2805],
      ['far-coast-balanced', 208488, 302293, 93805, 18, 29, 2, 4199],
      ['far-coast-deep', 208216, 313570, 105354, 22, 33, 2, 5196]
    ])
    expect(scenarios.every(scenario => scenario.envelopeSizes.before.withinBudget && scenario.envelopeSizes.after.withinBudget)).toBe(true)
    expect(scenarios.every(scenario => scenario.dueEvents.partitionInvariant && scenario.dueEvents.partitionedProjectionCanonicalBytes === scenario.dueEvents.singleProjectionCanonicalBytes)).toBe(true)
    expect(scenarios.map(scenario => [scenario.dueEvents.partitionMinutes, scenario.dueEvents.singleActionMinutes])).toEqual(Array(9).fill([[60, 180], 240]))
    expect(PERFORMANCE_FIXTURE_DUE_EVENT_SEQUENCE_MINUTES).toEqual([60, 180])
    expect(PERFORMANCE_FIXTURE_DUE_EVENT_TOTAL_MINUTES).toBe(240)
    expect([MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES.loaded, MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES.nearby, MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES.recurring, MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES.distantIndividualSummary, MEDIEVAL_FOUNDATION_SCHEDULER_CADENCE_MINUTES.distantSettlementSummary]).toEqual([1, 5, 30, 120, 240])
    expect(scenarios.every(scenario => scenario.presentation.terminalMapCells === 114 && scenario.presentation.detailedAdapterMapCells === 114 && scenario.presentation.worldTimeBefore === 0 && scenario.presentation.worldTimeAfter === 0 && scenario.presentation.sidebarKnownFactCount === 13)).toBe(true)
  // Nine fixtures are each derived twice to prove reproduction, then exercise
  // the fixed 60 + 180 minute scheduler sequence. This is deterministic work,
  // not a wall-clock regression assertion.
  }, 60_000)

  it('keeps terminal, sidebar, and detailed-adapter projections deterministic and zero-time without claiming browser responsiveness', () => {
    const world = selectedPerformanceFixtureWorld(MEDIEVAL_PERFORMANCE_FIXTURE_MATRIX[4]!)
    const before = structuredClone(world)
    const first = projectPerformanceFixturePresentation(world)
    const second = projectPerformanceFixturePresentation(world)

    expect(first).toEqual(second)
    expect(world).toEqual(before)
    expect(first.metrics).toMatchObject({ terminalMapCells: 114, detailedAdapterMapCells: 114, worldTimeBefore: 0, worldTimeAfter: 0 })
    expect(PERFORMANCE_FIXTURE_MEASUREMENT_BOUNDARIES.reviewOnlyObservations).toContain('Node wall-clock operation and projection timing')
    expect(PERFORMANCE_FIXTURE_MEASUREMENT_BOUNDARIES.nonClaims).toContain('no browser responsiveness percentile is measured')
  })

  it('builds a bounded, reproducible in-memory active-world and read-only-chronicle catalogue with real index contracts', () => {
    const catalogue = createPerformanceFixtureCatalogue()
    expect(catalogue).toEqual(createPerformanceFixtureCatalogue())
    expect(catalogue.activeWorldFixtureIds).toEqual(PERFORMANCE_FIXTURE_CATALOGUE_ACTIVE_IDS)
    expect(catalogue.chronicleFixtureIds).toEqual(PERFORMANCE_FIXTURE_CATALOGUE_CHRONICLE_IDS)
    expect(catalogue.index).toEqual({
      version: 1,
      activeWorlds: [
        { id: 'world:8dkz9o-up7bjk', label: 'Candle Mooring', initialCourierId: 'crew:0' },
        { id: 'world:ifdch4-954dyk', label: 'Drowned Reach', initialCourierId: 'crew:0' },
        { id: 'world:u1h4u0-1hps7so', label: 'Hollow Current', initialCourierId: 'crew:0' }
      ],
      chronicles: [
        { id: 'chronicle:world:19mcfb7-pjqw3b', label: 'Brackish Wold', reason: 'jomon-loss' },
        { id: 'chronicle:world:59jngy-1a51cqu', label: 'Low Sound', reason: 'crew-extinction' },
        { id: 'chronicle:world:gxedr1-56v03p', label: 'Eel Sound', reason: 'crew-extinction' },
        { id: 'chronicle:world:tvd1uf-18nxesb', label: 'Candle Reach', reason: 'jomon-loss' }
      ]
    })
    expect(catalogue.canonicalBytes).toEqual({ kind: 'world-index', bytes: 635, maximumBytes: 65536, withinBudget: true })
  })
})
