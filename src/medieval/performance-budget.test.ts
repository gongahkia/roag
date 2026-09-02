import { describe, expect, it } from 'vitest'
import { createDetailedRendererAdapterModel, createDetailedRendererSourceBundle } from './detailed-renderer-adapter'
import { classifyMedievalContent } from './content-safety'
import { createManagementSidebarModel } from './management-sidebar'
import {
  DESKTOP_BROWSER_SUPPORT_POLICY,
  MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES,
  MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES,
  MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES,
  MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION,
  MEDIEVAL_PERFORMANCE_STORAGE_BUDGETS,
  canonicalSerializedByteLength,
  canonicalSerializedJson,
  evaluateRequiredDesktopBrowserCapabilities,
  probeOptionalStorageEstimate,
  resolveMedievalFoundationBenchmarkFixture,
  serializedByteBudgetFor,
  summarizeBenchmarkTimings
} from './performance-budget'
import { JOMON_ASCII_GLYPH_CATALOG } from './ascii-glyphs'
import { defaultCreationSettings, emptyCreationSettingsRecord, saveCreationSettingsProfile } from './settings'
import { addWorldToIndex, emptyWorldIndex } from './storage'
import { defaultTerminalControlPreferences } from './terminal-controls'
import { createTerminalPresentationModel } from './terminal-presentation'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld } from './world'

const selectedWorld = (fixture = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES[0]!) => chooseInitialCourier(createFoundationWorld({ seed: fixture.seed, configuration: fixture.configuration }), 'crew:0')

describe('medieval performance and storage baseline contract', () => {
  it('defines exactly the three current deterministic preset/fidelity fixtures and resolves their real configuration authority', () => {
    expect(MEDIEVAL_PERFORMANCE_STORAGE_BASELINE_VERSION).toBe(1)
    expect(MEDIEVAL_FOUNDATION_BENCHMARK_WARMUP_SAMPLES).toBe(2)
    expect(MEDIEVAL_FOUNDATION_BENCHMARK_MEASURED_SAMPLES).toBe(9)
    expect(MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.map(fixture => [fixture.id, fixture.expectedPreset, fixture.expectedSimulationFidelity, fixture.expectedInstantiatedPeople])).toEqual([
      ['sheltered-reach-focused', 'sheltered-reach', 'focused', 6],
      ['watershed-balanced', 'watershed', 'balanced', 6],
      ['far-coast-deep', 'far-coast', 'deep', 6]
    ])

    const resolved = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.map(resolveMedievalFoundationBenchmarkFixture)
    expect(resolved.map(item => item.resolvedConfiguration.regionSize)).toEqual(['compact', 'standard', 'broad'])
    expect(resolved.map(item => item.resolvedConfiguration.historyYears)).toEqual([200, 300, 400])
    expect(resolved.map(item => item.initialWorldCaps.people)).toEqual([60, 60, 60])
    expect(resolved.map(item => item.generatedInitialPersonSeedCap)).toEqual([60, 60, 60])
    expect(resolved.map(item => item.instantiatedPersistentPeople)).toEqual([6, 6, 6])
    expect(resolved.map(item => item.schedulerCadenceMinutes)).toEqual([resolved[0]!.schedulerCadenceMinutes, resolved[0]!.schedulerCadenceMinutes, resolved[0]!.schedulerCadenceMinutes])
  })

  it('uses canonical UTF-8 byte accounting and deterministic fixture size ceilings without mutating worlds or storage', () => {
    const reordered = { z: 'é', a: [2, 1] }
    const canonical = { a: [2, 1], z: 'é' }
    expect(canonicalSerializedJson(reordered)).toBe(canonicalSerializedJson(canonical))
    expect(canonicalSerializedByteLength({ text: 'é' })).toBe(new TextEncoder().encode('{"text":"é"}').byteLength)
    expect(() => canonicalSerializedJson({ unsupported: undefined })).toThrow('serializable records')

    const worlds = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.map(selectedWorld)
    const before = structuredClone(worlds)
    const advancedWorlds = worlds.map((world, index) => advanceFoundationWorldTime(world, {
      id: `performance-budget-size:${index}`,
      kind: 'wait',
      durationMinutes: 240,
      contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
    }))
    const settings = MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES.reduce((record, fixture, index) => saveCreationSettingsProfile(record, `baseline ${index + 1}`, {
      ...defaultCreationSettings(),
      seed: fixture.seed,
      configuration: { preset: fixture.expectedPreset, advanced: {} },
      advancedMode: true
    }), emptyCreationSettingsRecord())
    const index = worlds.reduce((catalog, world) => addWorldToIndex(catalog, world), emptyWorldIndex())
    const sizes = [
      ...advancedWorlds.map(world => serializedByteBudgetFor('active-world', world)),
      serializedByteBudgetFor('terminal-controls', defaultTerminalControlPreferences()),
      serializedByteBudgetFor('creation-settings', settings),
      serializedByteBudgetFor('world-index', index)
    ]

    expect(sizes.every(size => size.withinBudget)).toBe(true)
    expect(worlds).toEqual(before)
    expect(MEDIEVAL_PERFORMANCE_STORAGE_BUDGETS.filter(budget => budget.classification === 'enforced-deterministic-size-budget').map(budget => budget.id)).toEqual([
      'active-world-json', 'terminal-controls-json', 'creation-settings-json', 'world-index-json'
    ])
  })

  it('summarizes finite timing samples reproducibly without using a timing assertion as a game rule', () => {
    expect(summarizeBenchmarkTimings([5, 1, 4, 2, 3])).toEqual({ samples: 5, minimumMilliseconds: 1, p50Milliseconds: 3, p95Milliseconds: 4.8, maximumMilliseconds: 5 })
    expect(() => summarizeBenchmarkTimings([])).toThrow('non-empty')
    expect(() => summarizeBenchmarkTimings([1, -1])).toThrow('non-negative')
  })

  it('records standards-only browser capability policy and keeps storage estimates optional diagnostics', async () => {
    expect(DESKTOP_BROWSER_SUPPORT_POLICY.supportedChannels).toEqual(['Chrome stable', 'Edge stable', 'Firefox stable', 'Safari stable'])
    expect(DESKTOP_BROWSER_SUPPORT_POLICY.automatedBaseline).toBe('Playwright Chromium')
    expect(DESKTOP_BROWSER_SUPPORT_POLICY.prohibitedBaselineRequirements).toContain('WebGPU')
    expect(evaluateRequiredDesktopBrowserCapabilities({
      'es-modules': true,
      'canvas-2d': true,
      'keyboard-events-and-focus': true,
      'indexeddb-and-structured-clone': true,
      'blob-and-object-urls': true,
      'guarded-font-loading': true
    })).toMatchObject({ supported: true, missing: [] })
    expect(evaluateRequiredDesktopBrowserCapabilities({ 'es-modules': true })).toMatchObject({ supported: false })
    expect(evaluateRequiredDesktopBrowserCapabilities({ 'es-modules': true }).missing).toContain('canvas-2d')
    await expect(probeOptionalStorageEstimate(undefined)).resolves.toEqual({ status: 'unavailable' })
    await expect(probeOptionalStorageEstimate({ estimate: async () => ({ quota: 1024, usage: 128 }) })).resolves.toEqual({ status: 'available', quota: 1024, usage: 128 })
    await expect(probeOptionalStorageEstimate({ estimate: async () => { throw new Error('private mode') } })).resolves.toEqual({ status: 'failed' })
  })

  it('keeps all current projections pure and within the scope of the fixture budget contract', () => {
    const world = selectedWorld(MEDIEVAL_FOUNDATION_BENCHMARK_FIXTURES[1])
    const before = structuredClone(world)
    const terminal = createTerminalPresentationModel(world)
    const sidebar = createManagementSidebarModel(world)
    const bundle = createDetailedRendererSourceBundle({ version: 1, terminal, sidebar, glyphCatalog: JOMON_ASCII_GLYPH_CATALOG, controls: defaultTerminalControlPreferences() })
    const detailed = createDetailedRendererAdapterModel(bundle)

    expect([terminal.map.state, sidebar.worldId, detailed.source.id]).toEqual(['reserved-unmaterialized', world.id, expect.stringMatching(/^detailed-source:/)])
    expect(world).toEqual(before)
  })
})
