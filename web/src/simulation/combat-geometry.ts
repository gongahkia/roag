import { active, index, inside, same } from './model.ts';
import type { Cell, World } from './model.ts';
import type { CombatRef } from './combat-model.ts';

/** Factions do not change collision: a friendly body still intercepts a colony shot. */
export function combatBodyAt(w: World, cell: Cell): CombatRef | null {
  const person = w.people.find(p => p.life !== 'dead' && same(p, cell));
  if (person) return { kind: 'person', id: person.id };
  const raider = w.combat.raiders.find(r => same(r, cell));
  if (raider) return { kind: 'raider', id: raider.id };
  const barricade = w.combat.barricades.find(b => same(b, cell));
  if (barricade) return { kind: 'barricade', id: barricade.id };
  const structure = w.structures.find(s => same(s, cell) && s.kind !== 'stockpile' && s.kind !== 'trap' && s.kind !== 'guardpost' && !(s.kind === 'door' && s.open));
  if (structure) return { kind: 'structure', id: structure.id };
  return same(w.heart, cell) ? { kind: 'heart', id: w.heart.id } : null;
}
export function blocksShot(w: World, cell: Cell): boolean {
  return !inside(cell) || w.terrain[index(cell.x, cell.y)] === 2 || same(w.heart, cell)
    || w.combat.barricades.some(b => same(b, cell))
    || w.structures.some(s => same(s, cell) && s.kind !== 'stockpile' && s.kind !== 'trap' && s.kind !== 'guardpost' && !(s.kind === 'door' && s.open))
    || w.jobs.some(j => j.kind === 'build' && active(j) && same(j, cell));
}
/** Bresenham cells include the first blocker. Diagonal steps cannot cut solid corners. */
export function shotCells(w: World, origin: Cell, target: Cell): Cell[] {
  if (!inside(origin) || !inside(target) || same(origin, target)) return [];
  const cells: Cell[] = [];
  let x = origin.x, y = origin.y;
  const dx = Math.abs(target.x - x), sx = x < target.x ? 1 : -1;
  const dy = -Math.abs(target.y - y), sy = y < target.y ? 1 : -1;
  let error = dx + dy;
  for (let n = 0; n < 256 && (x !== target.x || y !== target.y); n++) {
    const previous = { x, y }, twice = 2 * error;
    if (twice >= dy) { error += dy; x += sx; }
    if (twice <= dx) { error += dx; y += sy; }
    if (x !== previous.x && y !== previous.y) {
      const horizontal = { x, y: previous.y }, vertical = { x: previous.x, y };
      if (blocksShot(w, horizontal)) { cells.push(horizontal); break; }
      if (blocksShot(w, vertical)) { cells.push(vertical); break; }
    }
    const cell = { x, y }; cells.push(cell);
    if (blocksShot(w, cell)) break;
  }
  return cells;
}
export function clearShot(w: World, origin: Cell, target: Cell, targetId: number): boolean {
  for (const cell of shotCells(w, origin, target)) {
    const body = combatBodyAt(w, cell);
    if (body) return body.kind === 'raider' && body.id === targetId;
    if (blocksShot(w, cell)) return false;
  }
  return false;
}
