import { active, distance, index, inside, neighbors, same, walkable } from './model.ts';
import type { Cell, World } from './model.ts';
import { COMBAT_CONTENT as C } from './combat-content.ts';

type Node = { cell: Cell; key: number; cost: number; score: number; serial: number };
class Heap {
  nodes: Node[] = [];
  less(a: Node, b: Node): boolean { return a.score < b.score || a.score === b.score && (a.cost < b.cost || a.cost === b.cost && a.serial < b.serial); }
  push(node: Node): void {
    this.nodes.push(node); let i = this.nodes.length - 1;
    while (i > 0) { const p = (i - 1) >> 1; if (!this.less(node, this.nodes[p]!)) break; this.nodes[i] = this.nodes[p]!; i = p; } this.nodes[i] = node;
  }
  pop(): Node | undefined {
    const result = this.nodes[0], tail = this.nodes.pop(); if (!result || !this.nodes.length) return result;
    let i = 0;
    while (i * 2 + 1 < this.nodes.length) {
      let child = i * 2 + 1;
      if (child + 1 < this.nodes.length && this.less(this.nodes[child + 1]!, this.nodes[child]!)) child++;
      if (!this.less(this.nodes[child]!, tail!)) break; this.nodes[i] = this.nodes[child]!; i = child;
    }
    this.nodes[i] = tail!; return result;
  }
}
/** Planned obstruction cost allows a real breach; natural stone is never tunnelled. */
export function breachable(w: World, at: Cell): boolean {
  return inside(at) && w.terrain[index(at.x, at.y)] !== 2 && !same(at, w.heart)
    && (w.combat.barricades.some(b => same(b, at)) || w.structures.some(s => same(s, at)) || w.jobs.some(j => j.kind === 'build' && active(j) && same(j, at)));
}
export function raiderRoute(w: World, from: Cell, objective: Cell, occupied: Map<number, number>, self: number, limit: number = C.searchLimit, adjacent = true): Cell[] {
  const goals = (adjacent ? neighbors(objective) : [objective]).filter(p => walkable(w, p) || breachable(w, p));
  if (!goals.length) return [];
  const goalKeys = new Set(goals.map(p => index(p.x, p.y)));
  const start = index(from.x, from.y); if (goalKeys.has(start)) return [];
  const heap = new Heap(), costs = new Map<number, number>([[start, 0]]), parents = new Map<number, number>();
  const heuristic = (p: Cell) => Math.max(0, distance(p, objective) - (adjacent ? 1 : 0));
  let serial = 0, expanded = 0;
  heap.push({ cell: { x: from.x, y: from.y }, key: start, cost: 0, score: heuristic(from), serial: serial++ });
  while (heap.nodes.length && expanded < limit) {
    const node = heap.pop()!; if (costs.get(node.key) !== node.cost) continue; expanded++;
    if (goalKeys.has(node.key)) {
      const result: Cell[] = []; let key = node.key;
      while (key !== start) { result.push({ x: key % w.width, y: Math.floor(key / w.width) }); key = parents.get(key)!; }
      return result.reverse();
    }
    for (const p of neighbors(node.cell)) {
      const key = index(p.x, p.y), owner = occupied.get(key);
      if (owner !== undefined && owner !== self || !walkable(w, p) && !breachable(w, p)) continue;
      const cost = node.cost + (walkable(w, p) ? 1 : 12);
      if (cost >= (costs.get(key) ?? Infinity)) continue;
      costs.set(key, cost); parents.set(key, node.key);
      heap.push({ cell: p, key, cost, score: cost + heuristic(p), serial: serial++ });
    }
  }
  return [];
}
