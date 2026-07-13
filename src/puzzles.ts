import type { Biome, Floor, TileKind } from './types'

export interface PuzzlePlacement { dx: number; dy: number; kind: TileKind }
export interface PuzzleSolution { id: string; terrain: readonly TileKind[] }
export interface PuzzleTemplate { id: string; biome: Biome; solutions: readonly PuzzleSolution[]; placements: readonly PuzzlePlacement[] }

export const PUZZLE_TEMPLATES: readonly PuzzleTemplate[] = [
  { id: 'mine-rail-switch', biome: 'mine', solutions: [{ id: 'rail-crossing', terrain: ['rail', 'support'] }, { id: 'rubble-shortcut', terrain: ['rubble'] }], placements: [{ dx: -1, dy: 0, kind: 'rail' }, { dx: 0, dy: 0, kind: 'support' }, { dx: 1, dy: 0, kind: 'rail' }, { dx: 0, dy: -1, kind: 'rubble' }, { dx: 0, dy: 1, kind: 'crumble' }] },
  { id: 'mine-collapse-detour', biome: 'mine', solutions: [{ id: 'crumble-bridge', terrain: ['crumble'] }, { id: 'supported-detour', terrain: ['rail', 'support'] }], placements: [{ dx: -1, dy: 0, kind: 'rail' }, { dx: 0, dy: 0, kind: 'crumble' }, { dx: 1, dy: 0, kind: 'rail' }, { dx: 0, dy: -1, kind: 'support' }, { dx: 0, dy: 1, kind: 'rubble' }] },
  { id: 'wilds-waterway-fork', biome: 'wilds', solutions: [{ id: 'wade-water', terrain: ['water'] }, { id: 'cut-bramble', terrain: ['bramble'] }], placements: [{ dx: -1, dy: 0, kind: 'water' }, { dx: 0, dy: 0, kind: 'web' }, { dx: 1, dy: 0, kind: 'water' }, { dx: 0, dy: -1, kind: 'bramble' }, { dx: 0, dy: 1, kind: 'bramble' }] },
  { id: 'wilds-web-detour', biome: 'wilds', solutions: [{ id: 'cross-web', terrain: ['web'] }, { id: 'cut-bramble', terrain: ['bramble'] }], placements: [{ dx: -1, dy: 0, kind: 'web' }, { dx: 0, dy: 0, kind: 'bramble' }, { dx: 1, dy: 0, kind: 'web' }, { dx: 0, dy: -1, kind: 'water' }, { dx: 0, dy: 1, kind: 'water' }] },
  { id: 'caverns-vent-seal', biome: 'caverns', solutions: [{ id: 'quench-vent', terrain: ['fireVent'] }, { id: 'ignite-gas', terrain: ['gas'] }], placements: [{ dx: -1, dy: 0, kind: 'gas' }, { dx: 0, dy: 0, kind: 'fireVent' }, { dx: 1, dy: 0, kind: 'gas' }, { dx: 0, dy: -1, kind: 'darkness' }, { dx: 0, dy: 1, kind: 'darkness' }] },
  { id: 'caverns-smoke-line', biome: 'caverns', solutions: [{ id: 'burn-gas', terrain: ['gas'] }, { id: 'quench-vent', terrain: ['fireVent'] }], placements: [{ dx: -1, dy: 0, kind: 'fireVent' }, { dx: 0, dy: 0, kind: 'gas' }, { dx: 1, dy: 0, kind: 'fireVent' }, { dx: 0, dy: -1, kind: 'darkness' }, { dx: 0, dy: 1, kind: 'darkness' }] }
]

export const puzzleTemplatesFor = (biome: Biome): readonly PuzzleTemplate[] => PUZZLE_TEMPLATES.filter(template => template.biome === biome)
export const puzzleTemplateById = (id: string): PuzzleTemplate | undefined => PUZZLE_TEMPLATES.find(template => template.id === id)

export const validatePuzzleTemplates = (): string[] => {
  const errors: string[] = []
  const ids = new Set<string>()
  for (const template of PUZZLE_TEMPLATES) {
    if (ids.has(template.id)) errors.push(`duplicate puzzle template: ${template.id}`)
    ids.add(template.id)
    if (template.solutions.length < 2) errors.push(`insufficient puzzle solutions: ${template.id}`)
    const solutionIds = new Set<string>()
    for (const solution of template.solutions) {
      if (!solution.id || solutionIds.has(solution.id)) errors.push(`invalid puzzle solution: ${template.id}`)
      solutionIds.add(solution.id)
      if (!solution.terrain.length || solution.terrain.some(kind => !template.placements.some(placement => placement.kind === kind))) errors.push(`invalid puzzle route: ${template.id}:${solution.id}`)
    }
    if (!template.placements.length) errors.push(`empty puzzle template: ${template.id}`)
  }
  return errors
}

export const validateFloorPuzzles = (floor: Floor): string[] => (floor.puzzleIds ?? []).flatMap(id => {
  const template = puzzleTemplateById(id)
  if (!template) return [`unknown puzzle template: ${id}`]
  if (template.biome !== floor.biome) return [`wrong-biome puzzle template: ${id}`]
  return []
})
