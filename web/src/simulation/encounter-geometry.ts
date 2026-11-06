import { active, distance, index, inside, same, walkable, WIDTH, HEIGHT } from './model.ts';
import type { Cell, World } from './model.ts';
import type { RaidEdge } from './raid-model.ts';
import type { CombatRef } from './combat-model.ts';
import { blocksShot, combatBodyAt, shotCells } from './combat-geometry.ts';
import { ENCOUNTER_CONTENT as E } from './encounter-content.ts';

export function facingVector(facing: RaidEdge): Cell {
  return facing === 'north' ? { x: 0, y: -1 } : facing === 'east' ? { x: 1, y: 0 } : facing === 'south' ? { x: 0, y: 1 } : { x: -1, y: 0 };
}
export function facingToward(origin: Cell, target: Cell): RaidEdge {
  const dx = target.x - origin.x, dy = target.y - origin.y;
  return Math.abs(dx) >= Math.abs(dy) ? dx >= 0 ? 'east' : 'west' : dy >= 0 ? 'south' : 'north';
}
export function inFront(origin: Cell, facing: RaidEdge, attacker: Cell): boolean {
  const v = facingVector(facing), dx = attacker.x - origin.x, dy = attacker.y - origin.y;
  const forward = dx * v.x + dy * v.y, side = dx * v.y - dy * v.x;
  return forward > 0 && forward >= Math.abs(side);
}
export function chargeCells(origin: Cell, facing: RaidEdge): Cell[] {
  const v = facingVector(facing);
  return Array.from({ length: E.charge.range }, (_, i) => ({ x: origin.x + v.x * (i + 1), y: origin.y + v.y * (i + 1) })).filter(inside);
}
export function rampartCells(origin: Cell, facing: RaidEdge): Cell[] {
  const v = facingVector(facing), side = { x: -v.y, y: v.x };
  return [-2, -1, 1, 2].map(n => ({ x: origin.x + E.rampart.forward * v.x + n * side.x, y: origin.y + E.rampart.forward * v.y + n * side.y })).filter(inside);
}
export function encounterBody(w: World, cell: Cell): CombatRef | null {
  const body = combatBodyAt(w, cell);
  if (body) return body;
  const barricade = w.combat.barricades.find(b => same(b, cell));
  return barricade ? { kind: 'barricade', id: barricade.id } : null;
}
/** The same first-body and corner obstruction policy applies to hostile arrows and heals. */
export function lineReaches(w: World, origin: Cell, target: Cell, recipient: CombatRef): boolean {
  for (const cell of shotCells(w, origin, target)) {
    const body = encounterBody(w, cell);
    if (body) return body.kind === recipient.kind && body.id === recipient.id && same(cell, target);
    if (blocksShot(w, cell)) return false;
  }
  return false;
}
export function barricadeSite(w: World, cell: Cell, occupied: Map<number, number>): boolean {
  return inside(cell) && walkable(w, cell) && !occupied.has(index(cell.x, cell.y))
    && !w.combat.barricades.some(b => same(b, cell)) && !w.structures.some(s => same(s, cell))
    && !w.bosses.features.some(f => same(f,cell))
    && !w.jobs.some(j => j.kind === 'build' && active(j) && same(j, cell))
    && !w.resources.some(s => s.remaining > 0 && same(s, cell)) && !w.piles.some(p => same(p, cell));
}
/** Reachability before/after each proposed obstruction prevents sealing any existing heart access. */
export function heartReachable(w: World, extra: Cell | null = null): Uint8Array {
  const seen = new Uint8Array(WIDTH * HEIGHT), blocked = new Set(w.combat.barricades.map(b => index(b.x, b.y)));
  if (extra) blocked.add(index(extra.x, extra.y));
  const open = (cell: Cell): boolean => inside(cell) && !blocked.has(index(cell.x, cell.y)) && walkable(w, cell);
  const queue: Cell[] = [];
  for (const cell of [{ x: w.heart.x, y: w.heart.y - 1 }, { x: w.heart.x + 1, y: w.heart.y }, { x: w.heart.x, y: w.heart.y + 1 }, { x: w.heart.x - 1, y: w.heart.y }]) {
    if (open(cell)) { seen[index(cell.x, cell.y)] = 1; queue.push(cell); }
  }
  for (let i = 0; i < queue.length; i++) {
    const c = queue[i]!;
    for (const next of [{ x: c.x, y: c.y - 1 }, { x: c.x + 1, y: c.y }, { x: c.x, y: c.y + 1 }, { x: c.x - 1, y: c.y }]) {
      const key = index(next.x, next.y);
      if (open(next) && !seen[key]) { seen[key] = 1; queue.push(next); }
    }
  }
  return seen;
}
export function preservesHeartAccess(w: World, cell: Cell, before: Uint8Array): boolean {
  if (distance(cell, w.heart) === 1) return false;
  const after = heartReachable(w, cell), removed = index(cell.x, cell.y);
  for (let i = 0; i < before.length; i++) if (i !== removed && before[i] && !after[i]) return false;
  return true;
}
