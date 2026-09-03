import { findAsciiGlyph, terminalGlyphReferenceFor } from './ascii-glyphs'
import { auditMedievalContentSafety, contentSafetyAuditMatches, validateMedievalContentSafety, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { JomonPaletteToken } from './palette'
import { terminalNonColorCueFor, type TerminalGlyphReference, type TerminalNonColorCue, type TerminalPresentationState } from './terminal-semantics'
import type { FoundationJomon, FoundationWorld, JomonDeckPartition, JomonVesselPropKind } from './types'
import { validateFoundationWorld } from './world'

/**
 * A static, renderer-independent future-map input. This contract deliberately
 * does not materialize terminal cells or add spatial state to FoundationWorld.
 */
export const JOMON_DECK_PLAN_CONTRACT_VERSION = 1 as const

export const JOMON_DECK_PLAN_LIMITS = {
  width: 18,
  height: 8,
  areas: 9,
  props: 3,
  accessEdges: 9,
  areaCells: 12,
  structuralCells: 37,
  identityLength: 96
} as const

export const JOMON_DECK_PLAN_AREA_IDS = [
  'berths',
  'cargo-hold',
  'chart-table',
  'galley',
  'gangplank',
  'quay-approach',
  'repair-space',
  'stores',
  'tavern'
] as const
export type JomonDeckPlanAreaId = typeof JOMON_DECK_PLAN_AREA_IDS[number]

export const JOMON_DECK_PLAN_AREA_KINDS = ['shore-approach', 'vessel-access', 'vessel-space'] as const
export type JomonDeckPlanAreaKind = typeof JOMON_DECK_PLAN_AREA_KINDS[number]

export interface JomonDeckPlanCoordinate {
  column: number
  row: number
}

export interface JomonDeckPlanFootprintCell {
  id: string
  coordinate: JomonDeckPlanCoordinate
}

/** A semantic reference, never a renderer-owned raw colour or glyph value. */
export interface JomonDeckPlanSemanticReference {
  glyph: TerminalGlyphReference
  paletteToken: JomonPaletteToken
  presentationState: TerminalPresentationState
  nonColorCue: TerminalNonColorCue
  textEquivalent: string
  accessibilityText: string
  contentSafety: MedievalContentSafetyClassification
}

export interface JomonDeckPlanArea {
  id: JomonDeckPlanAreaId
  kind: JomonDeckPlanAreaKind
  /** Null only for the deliberately local, non-persistent quay approach. */
  partition: JomonDeckPartition | null
  anchor: JomonDeckPlanCoordinate
  footprint: readonly JomonDeckPlanFootprintCell[]
  semantic: JomonDeckPlanSemanticReference
}

export interface JomonDeckPlanStructuralCell {
  id: string
  kind: 'hull-boundary'
  coordinate: JomonDeckPlanCoordinate
  semantic: JomonDeckPlanSemanticReference
}

/** Undirected, orthogonal adjacency only; it does not authorize movement. */
export interface JomonDeckPlanAccessEdge {
  id: string
  fromAreaId: JomonDeckPlanAreaId
  toAreaId: JomonDeckPlanAreaId
}

export interface JomonDeckPlanPropBinding {
  id: string
  propId: string
  propKind: JomonVesselPropKind
  areaId: JomonDeckPlanAreaId
  anchor: JomonDeckPlanCoordinate
}

export interface JomonDeckPlan {
  version: typeof JOMON_DECK_PLAN_CONTRACT_VERSION
  state: 'static-future-map-input'
  vesselId: 'vessel:jomon'
  bounds: { width: number; height: number }
  areas: readonly JomonDeckPlanArea[]
  structuralCells: readonly JomonDeckPlanStructuralCell[]
  accessEdges: readonly JomonDeckPlanAccessEdge[]
  propBindings: readonly JomonDeckPlanPropBinding[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export type JomonDeckPlanDiagnosticCode =
  | 'jomon-deck-plan.malformed-world'
  | 'jomon-deck-plan.invalid-foundation-world'
  | 'jomon-deck-plan.malformed-jomon'
  | 'jomon-deck-plan.invalid-jomon-safety'
  | 'jomon-deck-plan.missing-partition'
  | 'jomon-deck-plan.unknown-partition'
  | 'jomon-deck-plan.duplicate-partition'
  | 'jomon-deck-plan.noncanonical-partition-order'
  | 'jomon-deck-plan.missing-prop'
  | 'jomon-deck-plan.unknown-prop'
  | 'jomon-deck-plan.duplicate-prop'
  | 'jomon-deck-plan.invalid-prop'
  | 'jomon-deck-plan.malformed-plan'
  | 'jomon-deck-plan.invalid-contract-version'
  | 'jomon-deck-plan.invalid-bounds'
  | 'jomon-deck-plan.missing-area'
  | 'jomon-deck-plan.unknown-area'
  | 'jomon-deck-plan.duplicate-area'
  | 'jomon-deck-plan.noncanonical-area-order'
  | 'jomon-deck-plan.invalid-area'
  | 'jomon-deck-plan.invalid-coordinate'
  | 'jomon-deck-plan.out-of-bounds-coordinate'
  | 'jomon-deck-plan.duplicate-coordinate'
  | 'jomon-deck-plan.overlapping-structure'
  | 'jomon-deck-plan.noncanonical-cell-order'
  | 'jomon-deck-plan.invalid-glyph-reference'
  | 'jomon-deck-plan.invalid-semantic-reference'
  | 'jomon-deck-plan.invalid-content-safety'
  | 'jomon-deck-plan.missing-prop-binding'
  | 'jomon-deck-plan.unknown-prop-binding'
  | 'jomon-deck-plan.duplicate-prop-binding'
  | 'jomon-deck-plan.invalid-prop-binding'
  | 'jomon-deck-plan.noncanonical-prop-binding-order'
  | 'jomon-deck-plan.invalid-access-edge'
  | 'jomon-deck-plan.duplicate-access-edge'
  | 'jomon-deck-plan.noncanonical-access-edge-order'
  | 'jomon-deck-plan.disconnected-topology'
  | 'jomon-deck-plan.invalid-safety-audit'
  | 'jomon-deck-plan.unexpected-hidden-world-data'
  | 'jomon-deck-plan.tampered-derived-plan'
  | MedievalContentSafetyDiagnosticCode

export interface JomonDeckPlanDiagnostic {
  recordId: string
  code: JomonDeckPlanDiagnosticCode
}

export type JomonDeckPlanValidation =
  | { version: typeof JOMON_DECK_PLAN_CONTRACT_VERSION; status: 'accepted'; diagnostics: readonly [] }
  | { version: typeof JOMON_DECK_PLAN_CONTRACT_VERSION; status: 'rejected'; diagnostics: readonly JomonDeckPlanDiagnostic[] }

export class JomonDeckPlanContractError extends Error {
  constructor(readonly diagnostics: readonly JomonDeckPlanDiagnostic[]) {
    super(`Jomon deck plan rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'JomonDeckPlanContractError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const keys = [...expected].sort(compare)
  return actual.length === keys.length && actual.every((key, index) => key === keys[index])
}
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value)
const stableId = (value: unknown): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= JOMON_DECK_PLAN_LIMITS.identityLength
  && /^[a-z][a-z0-9-]*(?::[a-z][a-z0-9-]*)*$/u.test(value)
const coordinateId = (coordinate: JomonDeckPlanCoordinate): string => `c${coordinate.column}-r${coordinate.row}`
const compareCoordinate = (left: JomonDeckPlanCoordinate, right: JomonDeckPlanCoordinate): number => left.row - right.row || left.column - right.column
const canonical = (values: readonly string[]): boolean => values.every((value, index) => index === 0 || compare(values[index - 1]!, value) < 0)
const canonicalCoordinates = (values: readonly JomonDeckPlanFootprintCell[]): boolean => values.every((value, index) => index === 0 || compareCoordinate(values[index - 1]!.coordinate, value.coordinate) < 0)
const issue = (recordId: string, code: JomonDeckPlanDiagnosticCode): JomonDeckPlanDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly JomonDeckPlanDiagnostic[]): readonly JomonDeckPlanDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))

const rectangle = (column: number, row: number, width: number, height: number): readonly JomonDeckPlanCoordinate[] => Array.from(
  { length: width * height },
  (_, index) => ({ column: column + (index % width), row: row + Math.floor(index / width) })
)

interface AreaBlueprint {
  id: JomonDeckPlanAreaId
  kind: JomonDeckPlanAreaKind
  partition: JomonDeckPartition | null
  glyphId: string
  cells: readonly JomonDeckPlanCoordinate[]
}

const AREA_BLUEPRINTS: readonly AreaBlueprint[] = [
  { id: 'berths', kind: 'vessel-space', partition: 'berths', glyphId: 'vessel:open-deck', cells: rectangle(7, 1, 4, 3) },
  { id: 'cargo-hold', kind: 'vessel-space', partition: 'cargo-hold', glyphId: 'vessel:open-deck', cells: rectangle(10, 4, 4, 3) },
  { id: 'chart-table', kind: 'vessel-space', partition: 'chart-table', glyphId: 'vessel:open-deck', cells: rectangle(7, 4, 3, 2) },
  { id: 'galley', kind: 'vessel-space', partition: 'galley', glyphId: 'vessel:open-deck', cells: rectangle(11, 1, 3, 3) },
  { id: 'gangplank', kind: 'vessel-access', partition: 'gangplank', glyphId: 'vessel:gangplank', cells: rectangle(3, 5, 1, 1) },
  { id: 'quay-approach', kind: 'shore-approach', partition: null, glyphId: 'route:quay-approach', cells: rectangle(0, 4, 3, 3) },
  { id: 'repair-space', kind: 'vessel-space', partition: 'repair-space', glyphId: 'vessel:open-deck', cells: rectangle(14, 4, 3, 3) },
  { id: 'stores', kind: 'vessel-space', partition: 'stores', glyphId: 'vessel:open-deck', cells: rectangle(4, 1, 3, 3) },
  { id: 'tavern', kind: 'vessel-space', partition: 'tavern', glyphId: 'vessel:open-deck', cells: rectangle(4, 4, 3, 3) }
]

const HULL_COORDINATES: readonly JomonDeckPlanCoordinate[] = [
  ...rectangle(4, 0, 13, 1),
  ...rectangle(3, 1, 1, 4),
  ...rectangle(17, 1, 1, 6),
  ...rectangle(3, 6, 1, 1),
  ...rectangle(4, 7, 13, 1)
].sort(compareCoordinate)

const ACCESS_AREA_PAIRS: readonly (readonly [JomonDeckPlanAreaId, JomonDeckPlanAreaId])[] = [
  ['berths', 'chart-table'],
  ['berths', 'galley'],
  ['cargo-hold', 'chart-table'],
  ['cargo-hold', 'galley'],
  ['cargo-hold', 'repair-space'],
  ['gangplank', 'quay-approach'],
  ['gangplank', 'tavern'],
  ['stores', 'tavern'],
  ['tavern', 'chart-table']
]

interface PropBlueprint {
  propId: string
  propKind: JomonVesselPropKind
  areaId: JomonDeckPlanAreaId
  anchor: JomonDeckPlanCoordinate
}

const PROP_BLUEPRINTS: readonly PropBlueprint[] = [
  { propId: 'prop:chart-table', propKind: 'table', areaId: 'chart-table', anchor: { column: 7, row: 4 } },
  { propId: 'prop:gangplank', propKind: 'gangplank', areaId: 'gangplank', anchor: { column: 3, row: 5 } },
  { propId: 'prop:task-ledger', propKind: 'ledger', areaId: 'tavern', anchor: { column: 4, row: 4 } }
]

const foundationPartitionOrder: readonly JomonDeckPartition[] = ['tavern', 'chart-table', 'cargo-hold', 'repair-space', 'stores', 'berths', 'galley', 'gangplank']
const hiddenWorldKeys = new Set(['worldTime', 'manifest', 'initialWorld', 'causalHistory', 'knownFacts', 'save', 'persistence'])

const semanticForGlyph = (glyphId: string): JomonDeckPlanSemanticReference => {
  const entry = findAsciiGlyph(glyphId)
  if (!entry || !entry.viewportUses.includes('future-jomon-deck')) throw new Error(`deck plan glyph is unavailable: ${glyphId}`)
  return {
    glyph: terminalGlyphReferenceFor(glyphId),
    paletteToken: entry.paletteToken,
    presentationState: entry.presentationState,
    nonColorCue: terminalNonColorCueFor(entry.presentationState),
    textEquivalent: entry.textEquivalent,
    accessibilityText: entry.accessibilityText,
    contentSafety: structuredClone(entry.contentSafety)
  }
}

const footprintFor = (areaId: JomonDeckPlanAreaId, coordinates: readonly JomonDeckPlanCoordinate[]): readonly JomonDeckPlanFootprintCell[] => coordinates.map(coordinate => ({
  id: `deck-cell:${areaId}:${coordinateId(coordinate)}`,
  coordinate: { ...coordinate }
}))

const canonicalAreaPair = (left: JomonDeckPlanAreaId, right: JomonDeckPlanAreaId): readonly [JomonDeckPlanAreaId, JomonDeckPlanAreaId] => compare(left, right) < 0 ? [left, right] : [right, left]
const accessEdgeFor = (left: JomonDeckPlanAreaId, right: JomonDeckPlanAreaId): JomonDeckPlanAccessEdge => {
  const [fromAreaId, toAreaId] = canonicalAreaPair(left, right)
  return { id: `deck-access:${fromAreaId}:${toAreaId}`, fromAreaId, toAreaId }
}

const structuralCells = (): readonly JomonDeckPlanStructuralCell[] => HULL_COORDINATES.map(coordinate => ({
  id: `deck-structure:hull-boundary:${coordinateId(coordinate)}`,
  kind: 'hull-boundary',
  coordinate: { ...coordinate },
  semantic: semanticForGlyph('vessel:hull-planking')
}))

const planContentRecords = (plan: Pick<JomonDeckPlan, 'areas' | 'structuralCells'>) => [
  ...plan.areas.map(area => ({ id: `jomon-deck-plan:area:${area.id}`, domain: 'template' as const, classification: area.semantic.contentSafety })),
  ...plan.structuralCells.map(cell => ({ id: `jomon-deck-plan:structure:${cell.id}`, domain: 'template' as const, classification: cell.semantic.contentSafety }))
]

const planForValidatedJomon = (jomon: FoundationJomon): JomonDeckPlan => {
  const propById = new Map(jomon.props.map(prop => [prop.id, prop]))
  const areas = AREA_BLUEPRINTS.map(blueprint => ({
    id: blueprint.id,
    kind: blueprint.kind,
    partition: blueprint.partition,
    anchor: { ...blueprint.cells[0]! },
    footprint: footprintFor(blueprint.id, blueprint.cells),
    semantic: semanticForGlyph(blueprint.glyphId)
  }))
  const plan = {
    version: JOMON_DECK_PLAN_CONTRACT_VERSION,
    state: 'static-future-map-input' as const,
    vesselId: jomon.id,
    bounds: { width: JOMON_DECK_PLAN_LIMITS.width, height: JOMON_DECK_PLAN_LIMITS.height },
    areas,
    structuralCells: structuralCells(),
    accessEdges: ACCESS_AREA_PAIRS.map(([left, right]) => accessEdgeFor(left, right)).sort((left, right) => compare(left.id, right.id)),
    propBindings: PROP_BLUEPRINTS.map(blueprint => {
      const prop = propById.get(blueprint.propId)!
      return {
        id: `deck-prop-binding:${prop.id}`,
        propId: prop.id,
        propKind: prop.kind,
        areaId: blueprint.areaId,
        anchor: { ...blueprint.anchor }
      }
    }).sort((left, right) => compare(left.id, right.id))
  }
  const audit = auditMedievalContentSafety(planContentRecords(plan))
  if (audit.status === 'rejected') throw new Error('compiled Jomon deck plan violates content safety')
  return { ...plan, contentSafetyAudit: audit }
}

const validCoordinateShape = (value: unknown): value is JomonDeckPlanCoordinate => record(value)
  && hasOnlyKeys(value, ['column', 'row'])
  && safeInteger(value.column)
  && safeInteger(value.row)

const coordinateInBounds = (coordinate: JomonDeckPlanCoordinate): boolean => coordinate.column >= 0
  && coordinate.column < JOMON_DECK_PLAN_LIMITS.width
  && coordinate.row >= 0
  && coordinate.row < JOMON_DECK_PLAN_LIMITS.height

const connectedAreas = (areas: readonly JomonDeckPlanArea[], edges: readonly JomonDeckPlanAccessEdge[]): ReadonlySet<JomonDeckPlanAreaId> => {
  const adjacency = new Map<JomonDeckPlanAreaId, JomonDeckPlanAreaId[]>(areas.map(area => [area.id, []]))
  for (const edge of edges) {
    adjacency.get(edge.fromAreaId)?.push(edge.toAreaId)
    adjacency.get(edge.toAreaId)?.push(edge.fromAreaId)
  }
  const visited = new Set<JomonDeckPlanAreaId>()
  const pending: JomonDeckPlanAreaId[] = ['quay-approach']
  while (pending.length) {
    const current = pending.shift()!
    if (visited.has(current)) continue
    visited.add(current)
    for (const adjacent of adjacency.get(current) ?? []) if (!visited.has(adjacent)) pending.push(adjacent)
  }
  return visited
}

const areOrthogonallyAdjacent = (left: readonly JomonDeckPlanFootprintCell[], right: readonly JomonDeckPlanFootprintCell[]): boolean => left.some(leftCell => right.some(rightCell => {
  const columnDistance = Math.abs(leftCell.coordinate.column - rightCell.coordinate.column)
  const rowDistance = Math.abs(leftCell.coordinate.row - rightCell.coordinate.row)
  return columnDistance + rowDistance === 1
}))

const sourceDiagnostics = (world: unknown, verifyFoundationWorld = true): readonly JomonDeckPlanDiagnostic[] => {
  const diagnostics: JomonDeckPlanDiagnostic[] = []
  if (!record(world)) return [issue('foundation-world', 'jomon-deck-plan.malformed-world')]
  if (verifyFoundationWorld && validateFoundationWorld(world).length) diagnostics.push(issue('foundation-world', 'jomon-deck-plan.invalid-foundation-world'))
  const jomon = world.jomon
  if (!record(jomon) || !hasOnlyKeys(jomon, ['id', 'name', 'contentSafety', 'deckPartitions', 'quays', 'props'])) return canonicalDiagnostics([...diagnostics, issue('foundation-world:jomon', 'jomon-deck-plan.malformed-jomon')])
  if (validateMedievalContentSafety([{ id: 'vessel:jomon', domain: 'place', classification: jomon.contentSafety }]).status === 'rejected') diagnostics.push(issue('foundation-world:jomon', 'jomon-deck-plan.invalid-jomon-safety'))
  if (!Array.isArray(jomon.deckPartitions)) {
    diagnostics.push(issue('foundation-world:jomon:deck-partitions', 'jomon-deck-plan.missing-partition'))
  } else {
    const seen = new Set<string>()
    for (const partition of jomon.deckPartitions) {
      if (!oneOf(foundationPartitionOrder, partition)) diagnostics.push(issue(`foundation-world:jomon:deck-partition:${String(partition)}`, 'jomon-deck-plan.unknown-partition'))
      if (typeof partition === 'string' && seen.has(partition)) diagnostics.push(issue(`foundation-world:jomon:deck-partition:${partition}`, 'jomon-deck-plan.duplicate-partition'))
      if (typeof partition === 'string') seen.add(partition)
    }
    for (const partition of foundationPartitionOrder) if (!seen.has(partition)) diagnostics.push(issue(`foundation-world:jomon:deck-partition:${partition}`, 'jomon-deck-plan.missing-partition'))
    if (!same(jomon.deckPartitions, foundationPartitionOrder)) diagnostics.push(issue('foundation-world:jomon:deck-partitions', 'jomon-deck-plan.noncanonical-partition-order'))
  }
  if (!Array.isArray(jomon.props)) {
    diagnostics.push(issue('foundation-world:jomon:props', 'jomon-deck-plan.missing-prop'))
  } else {
    const expectedById = new Map(PROP_BLUEPRINTS.map(blueprint => [blueprint.propId, blueprint]))
    const seen = new Set<string>()
    for (const candidate of jomon.props) {
      if (!record(candidate) || !stableId(candidate.id)) {
        diagnostics.push(issue('foundation-world:jomon:prop', 'jomon-deck-plan.invalid-prop'))
        continue
      }
      if (seen.has(candidate.id)) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.duplicate-prop'))
      seen.add(candidate.id)
      const expected = expectedById.get(candidate.id)
      if (!expected) {
        diagnostics.push(issue(candidate.id, 'jomon-deck-plan.unknown-prop'))
        continue
      }
      if (candidate.kind !== expected.propKind || candidate.partition !== expected.areaId) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-prop'))
    }
    for (const prop of PROP_BLUEPRINTS) if (!seen.has(prop.propId)) diagnostics.push(issue(prop.propId, 'jomon-deck-plan.missing-prop'))
  }
  return canonicalDiagnostics(diagnostics)
}

const validSemantic = (value: unknown, recordId: string, diagnostics: JomonDeckPlanDiagnostic[]): value is JomonDeckPlanSemanticReference => {
  if (!record(value) || !hasOnlyKeys(value, ['glyph', 'paletteToken', 'presentationState', 'nonColorCue', 'textEquivalent', 'accessibilityText', 'contentSafety'])) {
    diagnostics.push(issue(recordId, 'jomon-deck-plan.invalid-semantic-reference'))
    return false
  }
  const glyph = value.glyph
  if (!record(glyph) || !hasOnlyKeys(glyph, ['vocabulary', 'vocabularyVersion', 'id']) || typeof glyph.id !== 'string') {
    diagnostics.push(issue(recordId, 'jomon-deck-plan.invalid-glyph-reference'))
    return false
  }
  let expected: JomonDeckPlanSemanticReference
  try {
    expected = semanticForGlyph(glyph.id)
  } catch {
    diagnostics.push(issue(recordId, 'jomon-deck-plan.invalid-glyph-reference'))
    return false
  }
  if (!same(value, expected)) diagnostics.push(issue(recordId, 'jomon-deck-plan.invalid-semantic-reference'))
  if (validateMedievalContentSafety([{ id: recordId, domain: 'template', classification: value.contentSafety }]).status === 'rejected') diagnostics.push(issue(recordId, 'jomon-deck-plan.invalid-content-safety'))
  return true
}

/**
 * Validates both the authoritative source and a derived, non-authoritative
 * static plan. It intentionally refuses every unknown field or stale shape.
 */
export const validateJomonDeckPlan = (world: unknown, plan: unknown, verifyFoundationWorld = true): JomonDeckPlanValidation => {
  const diagnostics: JomonDeckPlanDiagnostic[] = [...sourceDiagnostics(world, verifyFoundationWorld)]
  if (!record(plan)) return { version: JOMON_DECK_PLAN_CONTRACT_VERSION, status: 'rejected', diagnostics: canonicalDiagnostics([...diagnostics, issue('jomon-deck-plan', 'jomon-deck-plan.malformed-plan')]) }
  if (Object.keys(plan).some(key => hiddenWorldKeys.has(key))) diagnostics.push(issue('jomon-deck-plan', 'jomon-deck-plan.unexpected-hidden-world-data'))
  if (!hasOnlyKeys(plan, ['version', 'state', 'vesselId', 'bounds', 'areas', 'structuralCells', 'accessEdges', 'propBindings', 'contentSafetyAudit'])) diagnostics.push(issue('jomon-deck-plan', 'jomon-deck-plan.malformed-plan'))
  if (plan.version !== JOMON_DECK_PLAN_CONTRACT_VERSION) diagnostics.push(issue('jomon-deck-plan', 'jomon-deck-plan.invalid-contract-version'))
  if (plan.state !== 'static-future-map-input' || plan.vesselId !== 'vessel:jomon') diagnostics.push(issue('jomon-deck-plan', 'jomon-deck-plan.malformed-plan'))
  if (!record(plan.bounds) || !hasOnlyKeys(plan.bounds, ['width', 'height']) || plan.bounds.width !== JOMON_DECK_PLAN_LIMITS.width || plan.bounds.height !== JOMON_DECK_PLAN_LIMITS.height) diagnostics.push(issue('jomon-deck-plan:bounds', 'jomon-deck-plan.invalid-bounds'))

  const areas = Array.isArray(plan.areas) ? plan.areas : []
  if (!Array.isArray(plan.areas) || areas.length !== JOMON_DECK_PLAN_LIMITS.areas) diagnostics.push(issue('jomon-deck-plan:areas', 'jomon-deck-plan.missing-area'))
  const typedAreas: JomonDeckPlanArea[] = []
  const areaIds: string[] = []
  const occupied = new Set<string>()
  for (const candidate of areas) {
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'kind', 'partition', 'anchor', 'footprint', 'semantic']) || !oneOf(JOMON_DECK_PLAN_AREA_IDS, candidate.id) || !oneOf(JOMON_DECK_PLAN_AREA_KINDS, candidate.kind)) {
      diagnostics.push(issue('jomon-deck-plan:area', 'jomon-deck-plan.invalid-area'))
      continue
    }
    areaIds.push(candidate.id)
    if (!validCoordinateShape(candidate.anchor) || !coordinateInBounds(candidate.anchor) || !Array.isArray(candidate.footprint) || candidate.footprint.length === 0 || candidate.footprint.length > JOMON_DECK_PLAN_LIMITS.areaCells) {
      diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-coordinate'))
      continue
    }
    const footprint: JomonDeckPlanFootprintCell[] = []
    for (const cell of candidate.footprint) {
      if (!record(cell) || !hasOnlyKeys(cell, ['id', 'coordinate']) || !validCoordinateShape(cell.coordinate)) {
        diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-coordinate'))
        continue
      }
      if (!coordinateInBounds(cell.coordinate)) diagnostics.push(issue(cell.id as string, 'jomon-deck-plan.out-of-bounds-coordinate'))
      if (!stableId(cell.id) || cell.id !== `deck-cell:${candidate.id}:${coordinateId(cell.coordinate)}`) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-coordinate'))
      const id = coordinateId(cell.coordinate)
      if (occupied.has(id)) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.duplicate-coordinate'))
      occupied.add(id)
      footprint.push({ id: cell.id as string, coordinate: cell.coordinate })
    }
    if (!canonicalCoordinates(footprint)) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.noncanonical-cell-order'))
    if (!same(candidate.anchor, footprint[0]?.coordinate)) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-coordinate'))
    const expectedPartition = AREA_BLUEPRINTS.find(area => area.id === candidate.id)?.partition
    if (candidate.partition !== expectedPartition) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-area'))
    validSemantic(candidate.semantic, candidate.id, diagnostics)
    typedAreas.push(candidate as unknown as JomonDeckPlanArea)
  }
  if (new Set(areaIds).size !== areaIds.length) diagnostics.push(issue('jomon-deck-plan:areas', 'jomon-deck-plan.duplicate-area'))
  if (!canonical(areaIds)) diagnostics.push(issue('jomon-deck-plan:areas', 'jomon-deck-plan.noncanonical-area-order'))
  for (const id of JOMON_DECK_PLAN_AREA_IDS) if (!areaIds.includes(id)) diagnostics.push(issue(id, 'jomon-deck-plan.missing-area'))

  const structuralCells = Array.isArray(plan.structuralCells) ? plan.structuralCells : []
  if (!Array.isArray(plan.structuralCells) || structuralCells.length !== JOMON_DECK_PLAN_LIMITS.structuralCells) diagnostics.push(issue('jomon-deck-plan:structure', 'jomon-deck-plan.invalid-coordinate'))
  const typedStructure: JomonDeckPlanStructuralCell[] = []
  for (const candidate of structuralCells) {
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'kind', 'coordinate', 'semantic']) || candidate.kind !== 'hull-boundary' || !stableId(candidate.id) || !validCoordinateShape(candidate.coordinate)) {
      diagnostics.push(issue('jomon-deck-plan:structure', 'jomon-deck-plan.invalid-coordinate'))
      continue
    }
    if (!coordinateInBounds(candidate.coordinate)) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.out-of-bounds-coordinate'))
    if (candidate.id !== `deck-structure:hull-boundary:${coordinateId(candidate.coordinate)}`) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-coordinate'))
    const id = coordinateId(candidate.coordinate)
    if (occupied.has(id)) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.overlapping-structure'))
    occupied.add(id)
    validSemantic(candidate.semantic, candidate.id, diagnostics)
    typedStructure.push(candidate as unknown as JomonDeckPlanStructuralCell)
  }
  if (!typedStructure.every((cell, index) => index === 0 || compareCoordinate(typedStructure[index - 1]!.coordinate, cell.coordinate) < 0)) diagnostics.push(issue('jomon-deck-plan:structure', 'jomon-deck-plan.noncanonical-cell-order'))

  const accessEdges = Array.isArray(plan.accessEdges) ? plan.accessEdges : []
  if (!Array.isArray(plan.accessEdges) || accessEdges.length !== JOMON_DECK_PLAN_LIMITS.accessEdges) diagnostics.push(issue('jomon-deck-plan:access', 'jomon-deck-plan.invalid-access-edge'))
  const typedEdges: JomonDeckPlanAccessEdge[] = []
  const edgeIds: string[] = []
  const areaById = new Map(typedAreas.map(area => [area.id, area]))
  for (const candidate of accessEdges) {
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'fromAreaId', 'toAreaId']) || !stableId(candidate.id) || !oneOf(JOMON_DECK_PLAN_AREA_IDS, candidate.fromAreaId) || !oneOf(JOMON_DECK_PLAN_AREA_IDS, candidate.toAreaId) || candidate.fromAreaId === candidate.toAreaId) {
      diagnostics.push(issue('jomon-deck-plan:access', 'jomon-deck-plan.invalid-access-edge'))
      continue
    }
    edgeIds.push(candidate.id)
    const expected = accessEdgeFor(candidate.fromAreaId, candidate.toAreaId)
    if (!same(candidate, expected)) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-access-edge'))
    const from = areaById.get(candidate.fromAreaId)
    const to = areaById.get(candidate.toAreaId)
    if (!from || !to || !areOrthogonallyAdjacent(from.footprint, to.footprint)) diagnostics.push(issue(candidate.id, 'jomon-deck-plan.invalid-access-edge'))
    typedEdges.push(candidate as unknown as JomonDeckPlanAccessEdge)
  }
  if (new Set(edgeIds).size !== edgeIds.length) diagnostics.push(issue('jomon-deck-plan:access', 'jomon-deck-plan.duplicate-access-edge'))
  if (!canonical(edgeIds)) diagnostics.push(issue('jomon-deck-plan:access', 'jomon-deck-plan.noncanonical-access-edge-order'))
  const reached = connectedAreas(typedAreas, typedEdges)
  if (reached.size !== JOMON_DECK_PLAN_AREA_IDS.length || !reached.has('gangplank') || !typedEdges.some(edge => edge.fromAreaId === 'gangplank' && edge.toAreaId === 'quay-approach')) diagnostics.push(issue('jomon-deck-plan:access', 'jomon-deck-plan.disconnected-topology'))

  const propBindings = Array.isArray(plan.propBindings) ? plan.propBindings : []
  if (!Array.isArray(plan.propBindings) || propBindings.length !== JOMON_DECK_PLAN_LIMITS.props) diagnostics.push(issue('jomon-deck-plan:props', 'jomon-deck-plan.missing-prop-binding'))
  const bindingIds: string[] = []
  const sourceProps = record(world) && record(world.jomon) && Array.isArray(world.jomon.props) ? world.jomon.props : []
  const sourcePropById = new Map(sourceProps.filter(record).map(prop => [prop.id, prop]))
  for (const candidate of propBindings) {
    if (!record(candidate) || !hasOnlyKeys(candidate, ['id', 'propId', 'propKind', 'areaId', 'anchor']) || !stableId(candidate.id) || !stableId(candidate.propId) || !oneOf(JOMON_DECK_PLAN_AREA_IDS, candidate.areaId) || !validCoordinateShape(candidate.anchor)) {
      diagnostics.push(issue('jomon-deck-plan:prop', 'jomon-deck-plan.invalid-prop-binding'))
      continue
    }
    bindingIds.push(candidate.id)
    const blueprint = PROP_BLUEPRINTS.find(prop => prop.propId === candidate.propId)
    const sourceProp = sourcePropById.get(candidate.propId)
    if (!blueprint) diagnostics.push(issue(candidate.propId, 'jomon-deck-plan.unknown-prop-binding'))
    if (!blueprint || !sourceProp || candidate.id !== `deck-prop-binding:${candidate.propId}` || candidate.propKind !== blueprint.propKind || candidate.areaId !== blueprint.areaId || !same(candidate.anchor, blueprint.anchor) || sourceProp.kind !== blueprint.propKind || sourceProp.partition !== blueprint.areaId) diagnostics.push(issue(candidate.propId, 'jomon-deck-plan.invalid-prop-binding'))
    const area = areaById.get(candidate.areaId)
    if (!area || !area.footprint.some(cell => same(cell.coordinate, candidate.anchor))) diagnostics.push(issue(candidate.propId, 'jomon-deck-plan.invalid-prop-binding'))
  }
  if (new Set(bindingIds).size !== bindingIds.length) diagnostics.push(issue('jomon-deck-plan:props', 'jomon-deck-plan.duplicate-prop-binding'))
  if (!canonical(bindingIds)) diagnostics.push(issue('jomon-deck-plan:props', 'jomon-deck-plan.noncanonical-prop-binding-order'))
  for (const blueprint of PROP_BLUEPRINTS) if (!bindingIds.includes(`deck-prop-binding:${blueprint.propId}`)) diagnostics.push(issue(blueprint.propId, 'jomon-deck-plan.missing-prop-binding'))

  const candidatePlan = plan as unknown as JomonDeckPlan
  if (!contentSafetyAuditMatches(planContentRecords({ areas: typedAreas, structuralCells: typedStructure }), candidatePlan.contentSafetyAudit)) diagnostics.push(issue('jomon-deck-plan', 'jomon-deck-plan.invalid-safety-audit'))
  if (diagnostics.length === 0 && record(world) && record(world.jomon)) {
    const expected = planForValidatedJomon(world.jomon as unknown as FoundationJomon)
    if (!same(plan, expected)) diagnostics.push(issue('jomon-deck-plan', 'jomon-deck-plan.tampered-derived-plan'))
  }
  const ordered = canonicalDiagnostics(diagnostics)
  return ordered.length === 0
    ? { version: JOMON_DECK_PLAN_CONTRACT_VERSION, status: 'accepted', diagnostics: [] }
    : { version: JOMON_DECK_PLAN_CONTRACT_VERSION, status: 'rejected', diagnostics: ordered }
}

/**
 * Derives the fixed layout only after validating the existing FoundationWorld.
 * Its output remains non-authoritative and can be discarded and rebuilt.
 */
export const deriveJomonDeckPlan = (world: FoundationWorld): JomonDeckPlan => {
  const source = sourceDiagnostics(world)
  if (source.length) throw new JomonDeckPlanContractError(source)
  const plan = planForValidatedJomon(world.jomon)
  const validation = validateJomonDeckPlan(world, plan)
  if (validation.status === 'rejected') throw new JomonDeckPlanContractError(validation.diagnostics)
  return plan
}

/**
 * Internal reducer seam for callers that have already passed the complete
 * FoundationWorld validator. It avoids recursive validation while replaying
 * a command whose geometry is still owned by this module.
 */
export const deriveJomonDeckPlanForVerifiedWorld = (world: FoundationWorld): JomonDeckPlan => {
  const source = sourceDiagnostics(world, false)
  if (source.length) throw new JomonDeckPlanContractError(source)
  const plan = planForValidatedJomon(world.jomon)
  const validation = validateJomonDeckPlan(world, plan, false)
  if (validation.status === 'rejected') throw new JomonDeckPlanContractError(validation.diagnostics)
  return plan
}
