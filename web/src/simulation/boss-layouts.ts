import { active, HEIGHT, index, inside, neighbors, WIDTH } from './model.ts';
import type { Cell, World } from './model.ts';
import type { RaidEdge } from './raid-model.ts';

export type BossKind = 'bellkeeper' | 'sluice' | 'threadmother';
export type BossLayout = {
  kind: BossKind;
  center: Cell;
  resonators: Cell[];
  channels: { cells: Cell[]; flow: Cell; pool: Cell; valve: Cell }[];
  anchors: Cell[];
  webs: Cell[];
};
export type BossPlacementReason = 'Unknown boss kind' | 'Unknown raid approach' | 'Candidate extends outside playable ground'
  | 'Candidate overlaps protected or occupied ground' | 'The colony has no open heart approach'
  | 'Candidate is disconnected from the colony' | 'Blocking fixtures would seal an existing route'
  | 'A counter has no accessible adjacent work tile' | 'No safe site within the bounded search';
export type BossPlacement = { layout: BossLayout | null; reason: BossPlacementReason | null; attempts: number };
export const BOSS_LAYOUT_SEARCH_LIMIT = 64;
export const BOSS_LAYOUT_RADII = [12, 18, 24, 30] as const;
const KINDS: readonly string[] = ['bellkeeper', 'sluice', 'threadmother'];
const EDGES: readonly string[] = ['north', 'east', 'south', 'west'];
const copy = (cell: Cell): Cell => ({ x: cell.x, y: cell.y });
const key = (cell: Cell): number => index(cell.x, cell.y);

export function bossLayoutShape(kind: BossKind, center: Cell): BossLayout {
  const at = (x: number, y: number): Cell => ({ x: center.x + x, y: center.y + y });
  const layout: BossLayout = { kind, center: copy(center), resonators: [], channels: [], anchors: [], webs: [] };
  if (kind === 'bellkeeper') layout.resonators = [at(-3, -2), at(3, -2), at(0, 3)];
  else if (kind === 'sluice') {
    for (const lane of [-2, 2]) {
      const cells = Array.from({ length: 7 }, (_, n) => at(n - 3, lane));
      for (let y = Math.min(0, lane); y <= Math.max(0, lane); y++) if (y !== lane) cells.push(at(0, y));
      layout.channels.push({ cells, flow: { x: 1, y: 0 }, pool: at(3, lane), valve: at(-3, lane / 2) });
    }
  } else {
    layout.anchors = [at(2, -4), at(2, 4)];
    for (const y of [-3, -2, -1, 1, 2, 3]) layout.webs.push(at(2, y));
  }
  return layout;
}

/** All intended placements, including the owner center and permanent walking gap. */
export function bossLayoutCells(layout: BossLayout): Cell[] {
  const cells = [layout.center, ...layout.resonators, ...layout.anchors, ...layout.webs,
    ...layout.channels.flatMap(channel => [...channel.cells, channel.pool, channel.valve])];
  if (layout.kind === 'threadmother') cells.push({ x: layout.center.x + 2, y: layout.center.y });
  const seen = new Set<number>();
  return cells.filter(cell => { const id = key(cell); if (seen.has(id)) return false; seen.add(id); return true; }).map(copy);
}
type Component = { seed: number; reachable: Uint8Array; count: number; founders: number };
type Context = { open: Uint8Array; protected: Set<number>; components: Component[] };

/** Static access deliberately ignores temporary body traffic; placement checks it separately. */
function flood(open: Uint8Array, seed: number, blocked: ReadonlySet<number>): Uint8Array {
  const seen = new Uint8Array(WIDTH * HEIGHT);
  if (!open[seed] || blocked.has(seed)) return seen;
  const queue = new Int32Array(WIDTH * HEIGHT); let head = 0, tail = 1;
  queue[0] = seed; seen[seed] = 1;
  while (head < tail) {
    const current = queue[head++]!, x = current % WIDTH, y = Math.floor(current / WIDTH);
    for (const next of [y > 0 ? current - WIDTH : -1, x < WIDTH - 1 ? current + 1 : -1, y < HEIGHT - 1 ? current + WIDTH : -1, x > 0 ? current - 1 : -1]) {
      if (next < 0 || !open[next] || seen[next] || blocked.has(next)) continue;
      seen[next] = 1; queue[tail++] = next;
    }
  }
  return seen;
}
function context(world: World): Context {
  const open = Uint8Array.from(world.terrain, tile => Number(tile !== 2));
  const protectedCells = new Set<number>([key(world.heart), ...neighbors(world.heart).map(key)]);
  const protect = (cell: Cell): void => { if (inside(cell)) protectedCells.add(key(cell)); };
  const close = (cell: Cell): void => { if (inside(cell)) open[key(cell)] = 0; };
  close(world.heart);
  for (const person of world.people) { protect(person); if (person.next) protect(person.next); }
  for (const raider of world.combat.raiders) { protect(raider); if (raider.next) protect(raider.next); }
  for (const structure of world.structures) {
    protect(structure);
    if (!['stockpile', 'trap', 'guardpost'].includes(structure.kind) && !(structure.kind === 'door' && structure.open)) close(structure);
  }
  for (const job of world.jobs) if (job.kind === 'build' && active(job)) { protect(job); close(job); }
  for (const source of world.resources) protect(source);
  for (const pile of world.piles) protect(pile);
  for (const barricade of world.combat.barricades) { protect(barricade); close(barricade); }
  for (const feature of world.bosses.features) { protect(feature); if (feature.kind === 'resonator' || feature.kind === 'anchor') close(feature); }
  const components: Component[] = [], empty = new Set<number>();
  for (const seed of neighbors(world.heart).map(key)) {
    if (!open[seed] || components.some(component => component.reachable[seed])) continue;
    const reachable = flood(open, seed, empty);
    const count = reachable.reduce((sum, value) => sum + value, 0);
    const founders = world.people.filter(person => person.life !== 'dead' && (reachable[key(person)] || person.next && reachable[key(person.next)])).length;
    components.push({ seed, reachable, count, founders });
  }
  // Counters belong to the occupied colony component. Separate pre-existing
  // heart approaches are also preserved, rather than masking a newly split route
  // with a multi-source flood from both sides of the new obstruction.
  components.sort((a, b) => b.founders - a.founders || b.count - a.count || a.seed - b.seed);
  return { open, protected: protectedCells, components };
}
function assess(ctx: Context, kind: BossKind, center: Cell): BossPlacement {
  const fail = (reason: BossPlacementReason): BossPlacement => ({ layout: null, reason, attempts: 1 });
  const layout = bossLayoutShape(kind, center), footprint = bossLayoutCells(layout);
  if (!footprint.every(cell => inside(cell) && cell.x > 0 && cell.y > 0 && cell.x < WIDTH - 1 && cell.y < HEIGHT - 1)) return fail('Candidate extends outside playable ground');
  if (footprint.some(cell => !ctx.open[key(cell)] || ctx.protected.has(key(cell)))) return fail('Candidate overlaps protected or occupied ground');
  const base = ctx.components[0];
  if (!base) return fail('The colony has no open heart approach');
  if (footprint.some(cell => !base.reachable[key(cell)])) return fail('Candidate is disconnected from the colony');
  const blocking = new Set([...layout.resonators, ...layout.anchors].map(key));
  let baseAfter = base.reachable;
  if (blocking.size) for (const component of ctx.components) {
    const after = flood(ctx.open, component.seed, blocking);
    for (let id = 0; id < component.reachable.length; id++) if (component.reachable[id] && !blocking.has(id) && !after[id]) return fail('Blocking fixtures would seal an existing route');
    if (component === base) baseAfter = after;
  }
  const counters = [...layout.resonators, ...layout.anchors, ...layout.channels.map(channel => channel.valve)];
  if (counters.some(counter => !neighbors(counter).some(adjacent => baseAfter[key(adjacent)] && !blocking.has(key(adjacent))))) return fail('A counter has no accessible adjacent work tile');
  if (!baseAfter[key(layout.center)]) return fail('Candidate is disconnected from the colony');
  return { layout, reason: null, attempts: 1 };
}

/** Pure validation of one complete overlay. No partial fixture placement or terrain writes. */
export function bossLayoutAt(world: World, kind: BossKind, center: Cell): BossPlacement {
  if (!KINDS.includes(kind)) return { layout: null, reason: 'Unknown boss kind', attempts: 0 };
  if (!inside(center)) return { layout: null, reason: 'Candidate extends outside playable ground', attempts: 1 };
  return assess(context(world), kind, center);
}

/** Stable, finite search. Approach changes candidate order; it consumes no RNG stream. */
export function findBossLayout(world: World, kind: BossKind, approach: RaidEdge): BossPlacement {
  if (!KINDS.includes(kind)) return { layout: null, reason: 'Unknown boss kind', attempts: 0 };
  if (!EDGES.includes(approach)) return { layout: null, reason: 'Unknown raid approach', attempts: 0 };
  const ctx = context(world);
  if (!ctx.components.length) return { layout: null, reason: 'The colony has no open heart approach', attempts: 0 };
  const turns = EDGES.indexOf(approach); let attempts = 0;
  for (const radius of BOSS_LAYOUT_RADII) {
    const half = radius / 2;
    const offsets = [[0, -radius], [half, -radius], [radius, -radius], [radius, -half], [radius, 0], [radius, half], [radius, radius], [half, radius],
      [0, radius], [-half, radius], [-radius, radius], [-radius, half], [-radius, 0], [-radius, -half], [-radius, -radius], [-half, -radius]];
    for (const offset of offsets) {
      if (attempts >= BOSS_LAYOUT_SEARCH_LIMIT) break;
      let x = offset[0]!, y = offset[1]!;
      for (let rotation = 0; rotation < turns; rotation++) [x, y] = [-y, x];
      const result = assess(ctx, kind, { x: world.heart.x + x, y: world.heart.y + y }); attempts++;
      if (result.layout) return { ...result, attempts };
    }
  }
  return { layout: null, reason: 'No safe site within the bounded search', attempts };
}

/** Re-created overlays preserve prior routes and avoid occupied colony footprints. */
export function bossFeaturePlacementAllowed(world: World, cell: Cell, blocking: boolean): boolean {
  if (!inside(cell) || cell.x <= 0 || cell.y <= 0 || cell.x >= WIDTH - 1 || cell.y >= HEIGHT - 1) return false;
  const ctx = context(world), id = key(cell), base = ctx.components[0];
  if (!base || !ctx.open[id] || ctx.protected.has(id) || !base.reachable[id]) return false;
  if (!blocking) return true;
  const blocked = new Set([id]);
  for (const component of ctx.components) {
    const after = flood(ctx.open, component.seed, blocked);
    for (let n = 0; n < component.reachable.length; n++) if (n !== id && component.reachable[n] && !after[n]) return false;
    if (component === base && !neighbors(cell).some(adjacent => after[key(adjacent)])) return false;
  }
  return true;
}
