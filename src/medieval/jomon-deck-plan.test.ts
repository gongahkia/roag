import { describe, expect, it } from 'vitest'
import { findAsciiGlyph } from './ascii-glyphs'
import { JOMON_PALETTE } from './palette'
import {
  JOMON_DECK_PLAN_AREA_IDS,
  JOMON_DECK_PLAN_CONTRACT_VERSION,
  JOMON_DECK_PLAN_LIMITS,
  JomonDeckPlanContractError,
  deriveJomonDeckPlan,
  validateJomonDeckPlan,
  type JomonDeckPlan,
  type JomonDeckPlanAreaId
} from './jomon-deck-plan'
import { createTerminalPresentationModel, terminalNonColorCueFor } from './terminal-presentation'
import { createFoundationWorld } from './world'

const world = () => createFoundationWorld({ seed: 'jomon-deck-plan-contract', configuration: { preset: 'watershed' } })
const coordinateId = (column: number, row: number): string => `${column}:${row}`

const reachableAreaIds = (plan: JomonDeckPlan): readonly JomonDeckPlanAreaId[] => {
  const adjacency = new Map<JomonDeckPlanAreaId, JomonDeckPlanAreaId[]>(plan.areas.map(area => [area.id, []]))
  for (const edge of plan.accessEdges) {
    adjacency.get(edge.fromAreaId)?.push(edge.toAreaId)
    adjacency.get(edge.toAreaId)?.push(edge.fromAreaId)
  }
  const reached = new Set<JomonDeckPlanAreaId>()
  const pending: JomonDeckPlanAreaId[] = ['quay-approach']
  while (pending.length) {
    const current = pending.shift()!
    if (reached.has(current)) continue
    reached.add(current)
    for (const adjacent of adjacency.get(current) ?? []) if (!reached.has(adjacent)) pending.push(adjacent)
  }
  return [...reached].sort()
}

describe('Jomon deck plan contract', () => {
  it('derives a repeatable static plan from the validated FoundationWorld without mutating its authority', () => {
    const source = world()
    const before = structuredClone(source)
    const first = deriveJomonDeckPlan(source)
    const second = deriveJomonDeckPlan(source)

    expect(first).toEqual(second)
    expect(first.version).toBe(JOMON_DECK_PLAN_CONTRACT_VERSION)
    expect(first.state).toBe('static-future-map-input')
    expect(validateJomonDeckPlan(source, first)).toEqual({ version: JOMON_DECK_PLAN_CONTRACT_VERSION, status: 'accepted', diagnostics: [] })
    expect(source).toEqual(before)
    expect(first).not.toHaveProperty('worldTime')
    expect(first).not.toHaveProperty('manifest')
    expect(first).not.toHaveProperty('state.worldTime')
    expect(JSON.stringify(first)).not.toContain(source.initialWorld.id)
  })

  it('has exactly the required areas, the existing prop bindings, bounded unique cells, and orthogonal quay access', () => {
    const plan = deriveJomonDeckPlan(world())
    const allCells = [...plan.areas.flatMap(area => area.footprint), ...plan.structuralCells]

    expect(plan.areas.map(area => area.id)).toEqual(JOMON_DECK_PLAN_AREA_IDS)
    expect(plan.areas.map(area => area.partition).filter((partition): partition is NonNullable<typeof partition> => partition !== null)).toEqual([
      'berths', 'cargo-hold', 'chart-table', 'galley', 'gangplank', 'repair-space', 'stores', 'tavern'
    ])
    expect(plan.propBindings.map(binding => [binding.propId, binding.propKind, binding.areaId])).toEqual([
      ['prop:chart-table', 'table', 'chart-table'],
      ['prop:gangplank', 'gangplank', 'gangplank'],
      ['prop:task-ledger', 'ledger', 'tavern']
    ])
    expect(plan.bounds).toEqual({ width: JOMON_DECK_PLAN_LIMITS.width, height: JOMON_DECK_PLAN_LIMITS.height })
    expect(plan.structuralCells).toHaveLength(JOMON_DECK_PLAN_LIMITS.structuralCells)
    expect(new Set(allCells.map(cell => coordinateId(cell.coordinate.column, cell.coordinate.row))).size).toBe(allCells.length)
    expect(allCells.every(cell => cell.coordinate.column >= 0 && cell.coordinate.column < plan.bounds.width && cell.coordinate.row >= 0 && cell.coordinate.row < plan.bounds.height)).toBe(true)
    expect(plan.areas.every(area => area.footprint.every((cell, index) => index === 0 || area.footprint[index - 1]!.coordinate.row < cell.coordinate.row || (area.footprint[index - 1]!.coordinate.row === cell.coordinate.row && area.footprint[index - 1]!.coordinate.column < cell.coordinate.column)))).toBe(true)
    expect(reachableAreaIds(plan)).toEqual([...JOMON_DECK_PLAN_AREA_IDS].sort())

    for (const edge of plan.accessEdges) {
      const from = plan.areas.find(area => area.id === edge.fromAreaId)!
      const to = plan.areas.find(area => area.id === edge.toAreaId)!
      expect(from.footprint.some(left => to.footprint.some(right => Math.abs(left.coordinate.column - right.coordinate.column) + Math.abs(left.coordinate.row - right.coordinate.row) === 1))).toBe(true)
    }
    expect(plan.accessEdges).toContainEqual(expect.objectContaining({ fromAreaId: 'gangplank', toAreaId: 'quay-approach' }))
    expect(plan.accessEdges).toContainEqual(expect.objectContaining({ fromAreaId: 'gangplank', toAreaId: 'tavern' }))
  })

  it('uses only closed future-deck glyph semantics, palette roles, paired non-colour cues, accessible text, and safe classifications', () => {
    const plan = deriveJomonDeckPlan(world())
    const semantics = [...plan.areas.map(area => area.semantic), ...plan.structuralCells.map(cell => cell.semantic)]

    for (const semantic of semantics) {
      const glyph = findAsciiGlyph(semantic.glyph.id)
      expect(glyph?.viewportUses).toContain('future-jomon-deck')
      expect(semantic.glyph).toEqual({ vocabulary: 'jomon-original-ascii-glyphs', vocabularyVersion: 1, id: glyph?.id })
      expect(Object.hasOwn(JOMON_PALETTE, semantic.paletteToken)).toBe(true)
      expect(semantic.nonColorCue).toEqual(terminalNonColorCueFor(semantic.presentationState))
      expect(semantic.textEquivalent).toBe(glyph?.textEquivalent)
      expect(semantic.accessibilityText).toBe(glyph?.accessibilityText)
      expect(semantic.contentSafety.exclusions).toEqual({
        'sexual-violence': 'excluded',
        slavery: 'excluded',
        torture: 'excluded',
        'child-harm-or-endangerment': 'excluded'
      })
    }
    expect(plan.contentSafetyAudit.status).toBe('accepted')
    expect(plan.contentSafetyAudit.reviewed).toHaveLength(plan.areas.length + plan.structuralCells.length)
  })

  it('fails closed for tampered sources and malformed, unsafe, overlapping, disconnected, noncanonical, or hidden derived data', () => {
    const source = world()
    const plan = deriveJomonDeckPlan(source)
    const invalidWorld = structuredClone(source) as unknown as { jomon: { deckPartitions: unknown[]; props: unknown[] } }
    invalidWorld.jomon.deckPartitions = [
      ...invalidWorld.jomon.deckPartitions.filter(partition => partition !== 'galley'),
      'tavern',
      'unknown-partition'
    ]
    invalidWorld.jomon.props = [
      ...invalidWorld.jomon.props.filter(prop => (prop as { id?: string }).id !== 'prop:task-ledger'),
      { id: 'prop:chart-table', kind: 'table', partition: 'chart-table' },
      { id: 'prop:unowned', kind: 'table', partition: 'chart-table' }
    ]
    const sourceFailure = validateJomonDeckPlan(invalidWorld, plan)
    expect(sourceFailure.status).toBe('rejected')
    if (sourceFailure.status === 'rejected') expect(sourceFailure.diagnostics.map(diagnostic => diagnostic.code)).toEqual(expect.arrayContaining([
      'jomon-deck-plan.invalid-foundation-world',
      'jomon-deck-plan.missing-partition',
      'jomon-deck-plan.unknown-partition',
      'jomon-deck-plan.duplicate-partition',
      'jomon-deck-plan.missing-prop',
      'jomon-deck-plan.unknown-prop',
      'jomon-deck-plan.duplicate-prop'
    ]))
    expect(() => deriveJomonDeckPlan(invalidWorld as never)).toThrow(JomonDeckPlanContractError)

    const outOfBounds = structuredClone(plan)
    outOfBounds.areas[0]!.footprint[0]!.coordinate.column = plan.bounds.width
    const overlap = structuredClone(plan)
    overlap.areas[0]!.footprint[0]!.coordinate = structuredClone(overlap.areas[1]!.footprint[0]!.coordinate)
    const disconnected = structuredClone(plan)
    disconnected.accessEdges = disconnected.accessEdges.filter(edge => edge.id !== 'deck-access:gangplank:quay-approach')
    const noncanonical = structuredClone(plan)
    noncanonical.areas = [...noncanonical.areas].reverse()
    const invalidGlyph = structuredClone(plan)
    invalidGlyph.areas[0]!.semantic.glyph.id = 'vessel:not-in-catalogue'
    const unsafe = structuredClone(plan)
    const unsafeClassification = unsafe.areas[0]!.semantic.contentSafety as unknown as { exclusions: { torture: unknown } }
    unsafeClassification.exclusions.torture = 'permitted'
    const hidden = { ...structuredClone(plan), worldTime: 4 }

    const codes = (candidate: unknown): readonly string[] => {
      const result = validateJomonDeckPlan(source, candidate)
      expect(result.status).toBe('rejected')
      return result.status === 'rejected' ? result.diagnostics.map(diagnostic => diagnostic.code) : []
    }
    expect(codes(outOfBounds)).toContain('jomon-deck-plan.out-of-bounds-coordinate')
    expect(codes(overlap)).toContain('jomon-deck-plan.duplicate-coordinate')
    expect(codes(disconnected)).toContain('jomon-deck-plan.disconnected-topology')
    expect(codes(noncanonical)).toContain('jomon-deck-plan.noncanonical-area-order')
    expect(codes(invalidGlyph)).toContain('jomon-deck-plan.invalid-glyph-reference')
    expect(codes(unsafe)).toContain('jomon-deck-plan.invalid-content-safety')
    expect(codes(hidden)).toContain('jomon-deck-plan.unexpected-hidden-world-data')
    expect(codes({})).toContain('jomon-deck-plan.malformed-plan')
  })

  it('leaves the current terminal model reserved and zero-cell; this plan is not terminal map materialization', () => {
    const source = world()
    const before = structuredClone(source)
    deriveJomonDeckPlan(source)
    const terminal = createTerminalPresentationModel(source)

    expect(terminal.map.state).toBe('reserved-unmaterialized')
    expect(terminal.map.cells).toEqual([])
    expect(source).toEqual(before)
  })
})
