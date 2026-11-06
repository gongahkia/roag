import type { Cell, Person, World } from './model.ts';
import type { CombatRef, Raider } from './combat-model.ts';
import type { BossKind, BossLayout } from './boss-layouts.ts';
export type { BossKind, BossLayout } from './boss-layouts.ts';

export type BossCounterMode = 'strike' | 'drain' | 'cut' | 'ignite';
export type BossFeatureKind = 'resonator' | 'valve' | 'anchor' | 'web' | 'line';
export type BossFeature = Cell & { id: number; owner: number; kind: BossFeatureKind; health: number; group: number | null; channel: number | null };
export type BossEffectKind = 'soundseek' | 'ring' | 'surge' | 'feast' | 'partition' | 'reel' | 'fire';
export type BossEffect = {
  id: number; owner: number; kind: BossEffectKind; source: number;
  origin: Cell; cells: Cell[]; due: number; damage: number;
  feature: number | null; channel: number | null; group: number | null;
};
export type BossEncounter = {
  owner: number; kind: BossKind; wave: number; layout: BossLayout;
  drained: boolean[]; arrived: boolean; moves: number; readyAt: number; exposedUntil: number;
  noise: { cell: Cell; at: number } | null;
  feast: { channel: number; expiresAt: number } | null;
  /** Retained only while finite fire pulses still need this historical owner. */
  deadAt: number | null;
};
export type BossCounterWork = { actor: number; mode: BossCounterMode; target: number; startedAt: number; progress: number };
export type Bosses = { encounters: BossEncounter[]; features: BossFeature[]; effects: BossEffect[]; work: BossCounterWork[]; status: string };
export type BossWorld = World & { bosses: Bosses };
/** Core owns collision, shared search limits, physical movement and final damage cleanup. */
export type BossApi = {
  move(raider: Raider, target: Cell, adjacent: boolean, allowedCells?: Cell[]): void;
  shoveRaider(raiderId: number, delta: Cell): boolean;
  shove(personId: number, delta: Cell): boolean;
  damage(ref: CombatRef, amount: number, origin: Cell): void;
  approach(person: Person, target: Cell): void;
  release(actor: number): void;
};

/* Public runtime hooks implemented in bosses.ts:
 * initialBosses(): Bosses
 * prepareBossSpawn(world: BossWorld, ticket: SpawnTicket): boolean
 * bossTurn(world: BossWorld, raider: Raider, api: BossApi): boolean
 * prepareBossCounters(world: BossWorld, api: BossApi): void
 * advanceBossEffects(world: BossWorld, api: BossApi): void
 * bossDied(world: BossWorld, owner: number): void
 * bossCounterReason(world: BossWorld, mode: BossCounterMode, target: Cell): string|null
 * damageBossFeature(world: BossWorld, id: number, amount: number, source: number, fire?: boolean): void
 * recordBossNoise(world: BossWorld, cell: Cell): void
 * bossCellMoveTicks(world: BossWorld, cell: Cell): 4|6|8
 * bossBlocksCell(world: BossWorld, cell: Cell): boolean
 * bossFeatureAt(world: BossWorld, cell: Cell): BossFeature|null
 * bossDamage(world: BossWorld, owner: number, amount: number): number
 */
