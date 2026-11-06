import { draw, validSeed } from './random.ts';
import { RAID_EDGES, RAID_KINDS, BOSS_KINDS } from './raid-model.ts';
import type { BossRaidKind } from './raid-model.ts';
import type { RaidDeadlines, RaidKind, RaidSummary, SpawnCell, SpawnDecision, SpawnTicket, WavePlan } from './raid-model.ts';

// New colony director, not an extraction-dungeon port. Retained references:
// TacticalSpecialDungeons.cs ScheduleSpecialPressure: explicit saved deadlines;
// RoagRandom.cs: isolated deterministic draws; EnemySquads.cs Roster: paired roles.
export const RAID_CONTENT = Object.freeze({
  firstWarningAt: 10800, firstSpawnAt: 12000,
  recoveryTicks: 1200, warningTicks: 600,
  maxBudget: 60, maxPending: 150, maxActive: 150, historyLimit: 16,
} as const);
export const RAID_COST = Object.freeze({
  raider: 1, rusher: 1, archer: 2, breacher: 3, shield: 3, healer: 3, captain: 12, bellkeeper:12, sluice:12, threadmother:12,
} satisfies Record<RaidKind, number>);
const LIMIT = 1_000_000_000;
function bounded(value: number, minimum: number, maximum: number, label: string): void {
  if (!Number.isSafeInteger(value) || value < minimum || value > maximum)
    throw new Error(`Invalid raid ${label}`);
}
export function waveBudget(wave: number): number {
  bounded(wave, 1, LIMIT, 'wave');
  return Math.min(RAID_CONTENT.maxBudget, 7 + 3 * (wave - 1));
}

/** Planning consumes only the supplied raids stream; it never reads or mutates World. */
export function bossForWave(seed: number, wave: number): BossRaidKind | null {
  if (!validSeed(seed)) throw new Error('Invalid raid seed');
  bounded(wave, 1, LIMIT, 'wave');
  return wave % 4 === 0 ? BOSS_KINDS[(Math.floor(wave / 4) - 1 + (seed + 2) % 4) % 4]! : null;
}
export function planWave(wave: number, raidsRng: number, nextId: number, seed = 42): WavePlan {
  const budget = waveBudget(wave);
  const boss = bossForWave(seed, wave);
  if (!validSeed(raidsRng)) throw new Error('Invalid raid RNG');
  bounded(nextId, 8, LIMIT, 'next ID');
  let state = raidsRng, allocated = nextId, spent = 0;
  const random = (maximum: number): number => {
    const result = draw(state, maximum); state = result.state; return result.value;
  };
  // Preserve P05's sole edge draw and exact first-wave roster (seed 42: east).
  const edge = RAID_EDGES[random(RAID_EDGES.length) - 1]!;
  const tickets: SpawnTicket[] = [];
  const add = (kinds: readonly RaidKind[], grouped: boolean, offset: number): void => {
    const cost = kinds.reduce((sum, kind) => sum + RAID_COST[kind], 0);
    if (spent + cost > budget || tickets.length + kinds.length > RAID_CONTENT.maxPending)
      throw new Error('Raid composition exceeded its finite budget');
    // Reserve the entire group atomically; there are no IDs spent on squad wrappers.
    bounded(allocated + kinds.length, 8, LIMIT, 'ID allocation');
    const leader = grouped ? allocated : null;
    for (const [i, kind] of kinds.entries()) {
      const spread = i === 0 ? 0 : i % 2 === 1 ? (i + 1) / 2 : -i / 2;
      tickets.push({ id: allocated++, kind, squadId: leader, leaderId: leader, edge,
        entryOffset: offset + (grouped ? spread : 0) });
    }
    spent += cost;
  };
  if (wave === 1) {
    add(['raider', 'raider', 'breacher', 'raider', 'raider'], false, 0);
  } else {
    const offset = () => random(25) - 13;
    if (boss) add([boss, 'raider', 'rusher', 'raider', 'rusher'], true, offset());
    else if (wave === 2) add(['shield', 'archer'], true, offset());
    else if (wave === 3) add(['healer', 'rusher', 'rusher'], true, offset());
    const groups: readonly (readonly RaidKind[])[] = [
      ['raider'], ['breacher'], ['shield', 'archer'], ['healer', 'rusher', 'rusher'],
    ];
    while (spent < budget) {
      const choices = groups.filter(group => group.reduce((sum, kind) => sum + RAID_COST[kind], 0) <= budget - spent);
      const group = choices[random(choices.length) - 1]!;
      add(group, group.length > 1, offset());
    }
  }
  return { wave, budget, spent, edge, captain: boss === 'captain', boss, tickets, nextId: allocated, nextRaidsRng: state };
}

export function firstRaidDeadlines(): RaidDeadlines {
  return { recoveryUntil: 0, warningAt: RAID_CONTENT.firstWarningAt, spawnAt: RAID_CONTENT.firstSpawnAt };
}
/** Call only after the previous wave's pending queue and live attackers are empty. */
export function nextRaidDeadlines(completedAt: number): RaidDeadlines {
  bounded(completedAt, 0, LIMIT - RAID_CONTENT.recoveryTicks - RAID_CONTENT.warningTicks, 'completion tick');
  const warningAt = completedAt + RAID_CONTENT.recoveryTicks;
  return { recoveryUntil: warningAt, warningAt, spawnAt: warningAt + RAID_CONTENT.warningTicks };
}

function validateTicket(ticket: SpawnTicket): void {
  bounded(ticket.id, 8, LIMIT - 1, 'ticket ID');
  if (!RAID_KINDS.includes(ticket.kind) || !RAID_EDGES.includes(ticket.edge)) throw new Error('Invalid raid ticket kind or edge');
  bounded(ticket.entryOffset, -63, 62, 'entry offset');
  if (ticket.squadId === null || ticket.leaderId === null) {
    if (ticket.squadId !== null || ticket.leaderId !== null) throw new Error('Invalid raid squad identity');
  } else {
    bounded(ticket.leaderId, 8, ticket.id, 'leader identity');
    if (ticket.squadId !== ticket.leaderId) throw new Error('Invalid raid squad identity');
  }
}

/** Current occupancy may block the planned entry; fallback order never rerolls. */
export function spawnCandidates(ticket: SpawnTicket): SpawnCell[] {
  validateTicket(ticket);
  const center = 64 + ticket.entryOffset, cells: SpawnCell[] = [];
  for (let distance = 0; distance < 126; distance++) {
    for (const sign of distance === 0 ? [1] : [1, -1]) {
      const along = center + distance * sign;
      if (along < 1 || along > 126) continue;
      cells.push(ticket.edge === 'north' ? { x: along, y: 1 }
        : ticket.edge === 'south' ? { x: along, y: 126 }
          : ticket.edge === 'east' ? { x: 126, y: along } : { x: 1, y: along });
    }
  }
  return cells;
}

/** FIFO: a full battlefield or blocked edge retains every committed identity. */
export function nextSpawn(pending: readonly SpawnTicket[], activeCount: number, canEnter: (cell: SpawnCell) => boolean): SpawnDecision {
  bounded(activeCount, 0, RAID_CONTENT.maxActive, 'active attacker count');
  if (pending.length > RAID_CONTENT.maxPending) throw new Error('Raid pending queue exceeds its cap');
  const ids = new Set<number>();
  for (const ticket of pending) {
    validateTicket(ticket);
    if (ids.has(ticket.id)) throw new Error('Duplicate raid pending identity');
    ids.add(ticket.id);
  }
  const retained = () => ({ ticket: null, cell: null, remaining: pending.map(ticket => ({ ...ticket })) });
  if (!pending.length || activeCount >= RAID_CONTENT.maxActive) return retained();
  const ticket = pending[0]!;
  const cell = spawnCandidates(ticket).find(canEnter);
  if (!cell) return retained();
  return { ticket: { ...ticket }, cell, remaining: pending.slice(1).map(item => ({ ...item })) };
}

function validateSummary(summary: RaidSummary): void {
  bounded(summary.wave, 1, LIMIT, 'summary wave');
  bounded(summary.startedAt, 0, LIMIT, 'summary start');
  bounded(summary.endedAt, summary.startedAt, LIMIT, 'summary end');
  if (summary.budget !== waveBudget(summary.wave)) throw new Error('Invalid raid summary budget');
  bounded(summary.spent, 0, summary.budget, 'summary expenditure');
  bounded(summary.spawned, 0, RAID_CONTENT.maxPending, 'summary spawned count');
  bounded(summary.kills, summary.spawned, summary.spawned, 'completed-wave kills');
  for (const count of [summary.colonistsLost, summary.ammoSpent, summary.structuresLost, summary.repairs])
    bounded(count, 0, LIMIT, 'summary measurement');
}
export function appendRaidSummary(history: readonly RaidSummary[], summary: RaidSummary): RaidSummary[] {
  if (history.length > RAID_CONTENT.historyLimit) throw new Error('Raid history exceeds its cap');
  let previousWave = 0;
  for (const item of [...history, summary]) {
    validateSummary(item);
    if (item.wave <= previousWave) throw new Error('Raid summaries must have increasing identities');
    previousWave = item.wave;
  }
  return [...history, summary].slice(-RAID_CONTENT.historyLimit).map(item => ({ ...item }));
}
