import type { Combat, CombatCommand } from './combat-model.ts';
import type { Duty, Recipe } from './survival-content.ts';
import type { Streams } from './random.ts';
import type { Building, Material, Stock } from './work-content.ts';
export const WIDTH = 128, HEIGHT = 128, TICK_MS = 100, MOVE_TICKS = 4, SEARCH_LIMIT = 4096;
export const CONTENT_VERSION = 'colony-p05-1';
export type Cell = { x: number; y: number };
export type Life = 'alive' | 'downed' | 'dead';
export type Routine = { kind: 'eat' | 'rest' | 'treatment' | 'rescue' | 'watch'; facility: number | null; patient: number | null; reservation: Reservation | null; cargo: Stock; progress: number; status: string; resume: Cell | null };
export type Body = { hunger:number; fatigue:number; health:number; mood:number; deathAt:number; recoverAt:number; meals:number; rests:number; duties:Record<Duty,number>; routine:Routine|null; status:string };
export type Person = Cell & { id: number; founder: number; life: Life; destination: Cell | null; next: Cell | null;
  progress: number; route: Cell[]; routeVersion: number; retryAt: number; blocked: string | null;
  job: number | null; cargo: Stock; yieldTo: Cell | null; yieldUntil: number; failures: number; body: Body };
export type Resource = Cell & { id: number; kind: Material; initial: number; remaining: number };
export type Structure = Cell & { id: number; kind: Building | 'stockpile'; health: number; open: boolean; inventory: Stock; embodied: Stock };
export type Pile = Cell & { id: number; inventory: Stock };
export type Reservation = { source: number; kind: Material; count: number };
export type Job = Cell & { id: number; kind: 'gather' | 'haul' | 'build' | 'repair' | 'dismantle' | 'farm' | 'produce' | 'reload'; ref: number | null;
  building: Building | null; state: 'active' | 'suspended' | 'done' | 'cancelled'; priority: number; worker: number | null;
  requested: number | null; progress: number; site: Stock; reservation: Reservation | null; delivery: number | null;
  recipe:Recipe|null; stage:number; readyAt:number; cycles:number; status: string; completions: number; retryAt: number };
export type CommandInput = CombatCommand | { kind: 'move'; actor: number; target: Cell } | { kind: 'edit'; target: Cell; solid: boolean }
  | { kind: 'gather'; source: number } | { kind: 'stockpile'; target: Cell } | { kind: 'build'; target: Cell; building: Building }
  | { kind: 'work'; job: number; action: 'cancel' | 'suspend' | 'resume' }
  | { kind: 'priority'; job: number; value: number } | { kind: 'assign'; job: number; worker: number | null }
  | { kind: 'repair' | 'dismantle'; structure: number } | { kind: 'door'; structure: number; open: boolean }
  | { kind:'produce'; structure:number; recipe:Recipe|'farm' } | { kind:'duty'; actor:number; duty:Duty; value:number }
  | { kind: 'dev'; action: 'damage' | 'kill' | 'deplete' | 'injure' | 'down' | 'hungry' | 'tired'; id: number };
export type Command = { sequence: number; due: number; input: CommandInput };
export type World = { combat:Combat; seed: number; tick: number; nextId: number; nextSequence: number; rng: Streams;
  width: number; height: number; terrain: number[]; terrainVersion: number; scenario: 'colony' | 'routing-lab' | 'work-lab';
  generation: { attempts: number; fallback: boolean }; heart: Cell & { id: number; health: number };
  people: Person[]; resources: Resource[]; pending: Command[]; terminal: 'heart-destroyed' | 'all-dead' | null;
  jobs: Job[]; structures: Structure[]; piles: Pile[]; spent: Stock; lost: Stock; sourceRemoved: Stock; produced:Stock; consumed:Stock; recipeCounts:Record<Recipe,number>; statistics:{ eatenFood:number; eatenMeals:number; harvests:number; recipes:number; rescues:number; treatments:number; deaths:number; workTicks:number } };
export const index = (x: number, y: number): number => y * WIDTH + x;
export const inside = (p: Cell): boolean => Number.isInteger(p.x) && Number.isInteger(p.y) && p.x >= 0 && p.y >= 0 && p.x < WIDTH && p.y < HEIGHT;
export const same = (a: Cell, b: Cell): boolean => a.x === b.x && a.y === b.y;
export const distance = (a: Cell, b: Cell): number => Math.abs(a.x - b.x) + Math.abs(a.y - b.y);
export function neighbors(p: Cell): Cell[] { return [{x:p.x,y:p.y-1},{x:p.x+1,y:p.y},{x:p.x,y:p.y+1},{x:p.x-1,y:p.y}].filter(inside); }
export const active = (j: Job): boolean => j.state === 'active' || j.state === 'suspended';
export function walkable(world: World, p: Cell): boolean {
  return inside(p) && world.terrain[index(p.x,p.y)] !== 2 && !same(p,world.heart)
    && !world.jobs.some(j => j.kind === 'build' && active(j) && same(j,p))
    && !world.structures.some(s => same(s,p) && !['stockpile','trap','guardpost'].includes(s.kind) && !(s.kind === 'door' && s.open));
}
export function spatial(world: World): Map<number, number> {
  const result = new Map<number, number>();
  for (const person of world.people) if (person.life !== 'dead') {
    result.set(index(person.x,person.y), person.id);
    if (person.next) result.set(index(person.next.x,person.next.y), person.id);
  }
  for (const r of world.combat.raiders) {
    result.set(index(r.x,r.y),r.id);
    if(r.next)result.set(index(r.next.x,r.next.y),r.id);
  }
  return result;
}
export function personAt(world: World, p: Cell): Person | undefined {
  const id = spatial(world).get(index(p.x,p.y)); return world.people.find((person) => person.id === id);
}
