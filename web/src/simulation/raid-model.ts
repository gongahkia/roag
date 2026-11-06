/** P06 contracts are independent of World until the P05 acceptance merge. */
export const RAID_KINDS = ['raider', 'breacher', 'shield', 'archer', 'healer', 'rusher', 'captain', 'bellkeeper', 'sluice', 'threadmother'] as const;
export type RaidKind = typeof RAID_KINDS[number];
export const BOSS_KINDS = ['captain', 'bellkeeper', 'sluice', 'threadmother'] as const;
export type BossRaidKind = typeof BOSS_KINDS[number];
export const isBossKind = (kind: RaidKind): kind is BossRaidKind => BOSS_KINDS.includes(kind as BossRaidKind);
export const BOSS_NAMES: Record<BossRaidKind,string> = { captain:'Ossuary Captain', bellkeeper:'Bellkeeper', sluice:'Sluice-Maw', threadmother:'Threadmother' };
export const RAID_EDGES = ['north', 'east', 'south', 'west'] as const;
export type RaidEdge = typeof RAID_EDGES[number];
export type SpawnCell = { x: number; y: number };

/** IDs are allocated at announcement, before an attacker enters the active world. */
export type SpawnTicket = {
  id: number;
  kind: RaidKind;
  squadId: number | null;
  leaderId: number | null;
  edge: RaidEdge;
  /** Signed displacement from the center (64) of the selected map edge. */
  entryOffset: number;
};
export type WavePlan = {
  wave: number;
  budget: number;
  spent: number;
  edge: RaidEdge;
  captain: boolean;
  boss: BossRaidKind | null;
  tickets: SpawnTicket[];
  nextId: number;
  nextRaidsRng: number;
};
export type RaidDeadlines = { recoveryUntil: number; warningAt: number; spawnAt: number };
export type SpawnDecision = { ticket: SpawnTicket | null; cell: SpawnCell | null; remaining: SpawnTicket[] };

/** Completed-wave measurements; cumulative totals remain the owning world's job. */
export type RaidSummary = {
  wave: number;
  budget: number;
  spent: number;
  startedAt: number;
  endedAt: number;
  spawned: number;
  kills: number;
  colonistsLost: number;
  ammoSpent: number;
  structuresLost: number;
  repairs: number;
};
