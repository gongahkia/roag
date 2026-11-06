import { index, same, walkable } from './model.ts';
import type { Cell, Resource, World } from './model.ts';

export const BIOMES = ['woodland', 'marsh', 'ash'] as const;
export type Biome = typeof BIOMES[number];
export type Environment = { biome: Biome; wet: Cell[]; ash: Cell[] };
export const ENVIRONMENT = { maxCells: 128, shallowMoveTicks: 6, heatEvery: 50, heatDamage: 2 } as const;
export function biomeForSeed(seed: number): Biome { return BIOMES[seed % BIOMES.length]!; }
export function initialEnvironment(seed: number): Environment { return { biome: biomeForSeed(seed), wet: [], ash: [] }; }

/** Keep the original resource draws; biomes change finite stocks, not their IDs. */
export function biomeQuantity(biome: Biome, kind: Resource['kind'], quantity: number): number {
  const change = biome === 'marsh' ? kind === 'food' ? 20 : kind === 'wood' ? -10 : 0
    : biome === 'ash' ? kind === 'stone' ? 20 : -10 : 0;
  return Math.max(10, Math.min(81, quantity + change));
}

/** Finite patches use coordinate hashing, without consuming any simulation stream. */
export function placeEnvironment(w: World): void {
  const e = w.environment; e.wet = []; e.ash = [];
  if (e.biome === 'woodland') return;
  const candidates: { cell: Cell; rank: number }[] = [];
  for (let y = 2; y <= 125; y++) for (let x = 2; x <= 125; x++) {
    const cell = { x, y }, radius = Math.max(Math.abs(x - w.heart.x), Math.abs(y - w.heart.y));
    if (radius <= 12 || radius > 48 || !walkable(w, cell) || w.resources.some(r => same(r, cell))) continue;
    // Adjacent cells share a coarse patch rank; stable tile rank breaks ties.
    const patch = Math.floor(x / 4) + 32 * Math.floor(y / 4);
    const rank = Math.imul((patch ^ w.seed) >>> 0, 2654435761) >>> 0;
    candidates.push({ cell, rank });
  }
  candidates.sort((a, b) => a.rank - b.rank || index(a.cell.x, a.cell.y) - index(b.cell.x, b.cell.y));
  const cells = candidates.slice(0, ENVIRONMENT.maxCells).map(c => c.cell).sort((a, b) => index(a.x, a.y) - index(b.x, b.y));
  if (e.biome === 'marsh') e.wet = cells; else e.ash = cells;
}

/** The result is copied onto the actor when an edge is reserved. */
export function groundMoveTicks(w: World, target: Cell): number {
  return w.environment.wet.some(c => same(c, target)) ? ENVIRONMENT.shallowMoveTicks : 4;
}
export function hotGround(w: World, cell: Cell): boolean {
  return w.environment.ash.some(c => same(c, cell)) && !w.structures.some(s => same(s, cell));
}
