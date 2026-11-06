import type { Cell, World } from './model.ts';
import type { Stock } from './work-content.ts';

export type VisitorOffer = { id: number; pay: Stock; receive: Stock; sold: boolean };
/** One reserved person identity rides the caravan until a local acceptance. */
export type VisitorApplicant = { id: number; name: string; role: string; sprite: string; appearance: number; accepted: boolean };
export type Visit = Cell & {
  id: number; serial: number; spawnedAt: number; arrivedAt: number | null; departAt: number | null;
  phase: 'arriving' | 'trading' | 'leaving'; entry: Cell; store: number | null;
  next: Cell | null; progress: number; moveTicks: number; route: Cell[]; routeVersion: number; retryAt: number;
  cargo: Stock; offers: VisitorOffer[]; applicant: VisitorApplicant | null;
};
export type VisitReceipt = {
  id: number; serial: number; spawnedAt: number; arrivedAt: number | null; departedAt: number;
  trades: number; recruited: number | null; cargoOut: Stock;
};
export type Visitors = {
  nextAt: number; serial: number; departures: number; active: Visit | null; history: VisitReceipt[];
  externalIn: Stock; externalOut: Stock; trades: number; recruits: number; status: string;
};
export type VisitorCommand =
  | { kind: 'barter'; actor: number; visit: number; offer: number; store: number }
  | { kind: 'recruit'; actor: number; visit: number; applicant: number; store: number };
export type VisitorsWorld = World & { visitors: Visitors };
export type VisitorApi = {
  /** Counts living and downed colonists; dead residents do not occupy a slot. */
  livingCount: () => number;
  /** Includes retained dead/resident identity records, preventing ID reuse. */
  hasResident: (id: number) => boolean;
  /** Validated synchronous factory/registration; empty inventory, no throws or rerolls. */
  join: (applicant: VisitorApplicant, at: Cell) => void;
};
