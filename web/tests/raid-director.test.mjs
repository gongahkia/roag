import test from 'node:test';
import assert from 'node:assert/strict';
import { streams, draw, validSeed } from '../src/simulation/random.ts';
import { RAID_EDGES, isBossKind, BOSS_KINDS } from '../src/simulation/raid-model.ts';
import { RAID_CONTENT, RAID_COST, planWave, bossForWave, waveBudget, firstRaidDeadlines, nextRaidDeadlines, spawnCandidates, nextSpawn, appendRaidSummary } from '../src/simulation/raid-director.ts';

function freeze(value) {
  if (value && typeof value === 'object') { Object.values(value).forEach(freeze); Object.freeze(value); }
  return value;
}
function summary(wave) {
  return { wave, budget: waveBudget(wave), spent: waveBudget(wave), startedAt: wave * 2000, endedAt: wave * 2000 + 100,
    spawned: 5, kills: 5, colonistsLost: 0, ammoSpent: 12, structuresLost: 0, repairs: 2 };
}

test('first wave preserves the P05 roster, sole edge draw, seed-42 east entry and schedule', () => {
  const rng = freeze(streams(42)), before = JSON.stringify(rng), next = draw(rng.raids, 4);
  const wave = planWave(1, rng.raids, 26);
  assert.deepEqual(wave.tickets.map(t => t.kind), ['raider', 'raider', 'breacher', 'raider', 'raider']);
  assert.equal(wave.edge, 'east'); assert.equal(wave.captain, false);
  assert.equal(wave.budget, 7); assert.equal(wave.spent, 7); assert.equal(wave.nextId, 31);
  assert.equal(wave.nextRaidsRng, next.state);
  assert.ok(wave.tickets.every(t => t.edge === 'east' && t.entryOffset === 0 && t.squadId === null && t.leaderId === null));
  assert.deepEqual(spawnCandidates(wave.tickets[0]).slice(0, 5), [
    { x: 126, y: 64 }, { x: 126, y: 65 }, { x: 126, y: 63 }, { x: 126, y: 66 }, { x: 126, y: 62 },
  ]);
  assert.deepEqual(firstRaidDeadlines(), { recoveryUntil: 0, warningAt: 10800, spawnAt: 12000 });
  assert.equal(JSON.stringify(rng), before, 'all supplied stream states remain unchanged');
});

test('announced squads and every fourth seeded boss use finite cost and actor-owned group identities', () => {
  let rng = streams(42).raids, nextId = 26;
  for (let wave = 1; wave <= 1000; wave++) {
    const plan = planWave(wave, rng, nextId);
    assert.equal(plan.budget, Math.min(60, 7 + 3 * (wave - 1)));
    assert.equal(plan.spent, plan.tickets.reduce((sum, t) => sum + RAID_COST[t.kind], 0));
    assert.equal(plan.spent, plan.budget);
    assert.ok(plan.tickets.length <= RAID_CONTENT.maxPending);
    assert.deepEqual(plan.tickets.map(t => t.id), Array.from({ length: plan.tickets.length }, (_, i) => nextId + i));
    assert.equal(plan.nextId, nextId + plan.tickets.length, 'group wrappers do not allocate extra actor IDs');
    assert.equal(plan.tickets.filter(t => isBossKind(t.kind)).length, wave % 4 === 0 ? 1 : 0);
    assert.equal(plan.boss, bossForWave(42,wave));
    assert.equal(plan.captain, plan.boss === 'captain');
    if (wave === 2) assert.deepEqual(plan.tickets.slice(0, 2).map(t => t.kind), ['shield', 'archer']);
    if (wave === 3) assert.deepEqual(plan.tickets.slice(0, 3).map(t => t.kind), ['healer', 'rusher', 'rusher']);
    if (wave === 4) {
      assert.equal(plan.budget, 16); assert.equal(plan.tickets[0].kind, 'captain');
      assert.ok(plan.tickets.slice(1).every(t => t.kind === 'raider' || t.kind === 'rusher'));
    }
    for (const ticket of plan.tickets) {
      assert.equal(ticket.edge, plan.edge);
      if (ticket.squadId === null) assert.equal(ticket.leaderId, null);
      else {
        assert.equal(ticket.squadId, ticket.leaderId);
        const leader = plan.tickets.find(t => t.id === ticket.leaderId);
        assert.ok(leader && ['shield', 'healer', ...BOSS_KINDS].includes(leader.kind));
        assert.ok(leader.id <= ticket.id);
        assert.ok(Math.abs(leader.entryOffset - ticket.entryOffset) <= 2, 'squad entries stay together');
      }
    }
    assert.ok(validSeed(plan.nextRaidsRng)); rng = plan.nextRaidsRng; nextId = plan.nextId;
  }
});

test('JSON-restored director inputs reproduce future compositions, entries, IDs and RNG exactly', () => {
  for (const seed of [1, 42, 912341, 2147483646]) {
    let input = { wave: 1, rng: streams(seed).raids, nextId: 100 };
    for (let wave = 1; wave <= 25; wave++) {
      const p = planWave(input.wave, input.rng, input.nextId);
      input = { wave: input.wave + 1, rng: p.nextRaidsRng, nextId: p.nextId };
    }
    let restored = JSON.parse(JSON.stringify(input));
    for (let wave = 0; wave < 100; wave++) {
      const expected = planWave(input.wave, input.rng, input.nextId);
      const actual = planWave(restored.wave, restored.rng, restored.nextId);
      assert.deepEqual(actual, expected);
      input = { wave: input.wave + 1, rng: expected.nextRaidsRng, nextId: expected.nextId };
      restored = { wave: restored.wave + 1, rng: actual.nextRaidsRng, nextId: actual.nextId };
    }
  }
});

test('every edge has a bounded, complete, deterministic fallback without rerolling entry metadata', () => {
  for (const edge of RAID_EDGES) for (const entryOffset of [-63, -12, 0, 12, 62]) {
    const ticket = freeze({ id: 8, kind: 'raider', squadId: null, leaderId: null, edge, entryOffset });
    const cells = spawnCandidates(ticket);
    assert.equal(cells.length, 126); assert.equal(new Set(cells.map(c => `${c.x},${c.y}`)).size, 126);
    assert.ok(cells.every(c => c.x >= 1 && c.y >= 1 && c.x <= 126 && c.y <= 126));
    assert.ok(cells.every(c => edge === 'east' ? c.x === 126 : edge === 'west' ? c.x === 1 : edge === 'north' ? c.y === 1 : c.y === 126));
    assert.equal(edge === 'east' || edge === 'west' ? cells[0].y : cells[0].x, 64 + entryOffset);
    const selected = nextSpawn([ticket], 0, c => c.x === cells.at(-1).x && c.y === cells.at(-1).y);
    assert.deepEqual(selected.cell, cells.at(-1)); assert.equal(selected.ticket.id, ticket.id);
    assert.deepEqual(selected.remaining, []); assert.deepEqual(spawnCandidates(ticket), cells);
  }
});

test('150 live attackers and blocked entries retain all committed IDs; a free slot consumes exactly one ticket', () => {
  const pending = freeze(Array.from({ length: 150 }, (_, i) => ({ id: 100 + i, kind: 'raider', squadId: null, leaderId: null, edge: 'east', entryOffset: 0 })));
  const original = JSON.stringify(pending);
  const capped = nextSpawn(pending, 150, () => { throw Error('No entry search is needed at the live cap'); });
  assert.equal(capped.ticket, null); assert.equal(capped.cell, null); assert.deepEqual(capped.remaining, pending);
  let attempts = 0;
  for (let retry = 0; retry < 20; retry++) {
    const blocked = nextSpawn(pending, 149, () => { attempts++; return false; });
    assert.equal(blocked.ticket, null); assert.deepEqual(blocked.remaining, pending);
  }
  assert.equal(attempts, 20 * 126, 'each retry has a bounded candidate search');
  const result = nextSpawn(pending, 149, c => c.y === 65);
  assert.deepEqual(result.cell, { x: 126, y: 65 }); assert.equal(result.ticket.id, 100);
  assert.deepEqual(result.remaining.map(t => t.id), pending.slice(1).map(t => t.id));
  assert.equal(JSON.stringify(pending), original);
  result.ticket.entryOffset = 10; result.remaining[0].entryOffset = 11;
  assert.equal(JSON.stringify(pending), original, 'returned records do not alias the saved queue');
  assert.equal(nextSpawn([], 0, () => true).ticket, null);
});

test('FIFO never skips a blocked leader or generates a replacement identity', () => {
  const plan = planWave(2, streams(42).raids, 26);
  const pending = freeze([{ ...plan.tickets[0], edge: 'east' }, { ...plan.tickets[1], edge: 'west' }]);
  const blocked = nextSpawn(pending, 0, c => c.x === 1);
  assert.equal(blocked.ticket, null); assert.deepEqual(blocked.remaining, pending);
  const first = nextSpawn(pending, 0, () => true), second = nextSpawn(first.remaining, 1, () => true);
  assert.equal(first.ticket.id, pending[0].id); assert.equal(second.ticket.id, pending[1].id);
  assert.equal(second.ticket.leaderId, first.ticket.id, 'leader identity survives outside the pending queue');
});

test('post-boss recovery schedules the next warning from completion, with bounded saved deadlines', () => {
  assert.deepEqual(nextRaidDeadlines(15000), { recoveryUntil: 16200, warningAt: 16200, spawnAt: 16800 });
  const last = nextRaidDeadlines(1e9 - 1800); assert.equal(last.spawnAt, 1e9);
  assert.throws(() => nextRaidDeadlines(1e9 - 1799), /Invalid raid/);
  assert.throws(() => nextRaidDeadlines(-1), /Invalid raid/);
  const boss = planWave(4, streams(42).raids, 26);
  const later = planWave(5, boss.nextRaidsRng, boss.nextId);
  assert.equal(boss.captain, true); assert.equal(later.captain, false);
  assert.ok(later.tickets.length > 0 && later.tickets.every(t => t.id >= boss.nextId));
});

test('ten thousand completed waves retain only sixteen summaries without mutating or resetting previous results', () => {
  let history = [];
  for (let wave = 1; wave <= 10000; wave++) {
    const previous = freeze(history), before = JSON.stringify(previous), record = freeze(summary(wave));
    history = appendRaidSummary(previous, record);
    assert.equal(JSON.stringify(previous), before);
    assert.equal(history.length, Math.min(wave, 16)); assert.equal(history.at(-1).wave, wave);
  }
  assert.deepEqual(history.map(s => s.wave), Array.from({ length: 16 }, (_, i) => 9985 + i));
  assert.throws(() => appendRaidSummary(history, summary(10000)), /increasing identities/);
  assert.throws(() => appendRaidSummary(history, { ...summary(10001), kills: 4 }), /completed-wave kills/);
  assert.throws(() => appendRaidSummary(history, { ...summary(10001), spent: 61 }), /expenditure/);
  assert.throws(() => appendRaidSummary(Array.from({ length: 17 }, (_, i) => summary(i + 1)), summary(18)), /history exceeds/);
});

test('invalid RNG, wave, IDs, queue shape and capacity fail before any caller state changes', () => {
  for (const wave of [0, -1, 1.5, NaN, Infinity, 1e9 + 1]) assert.throws(() => planWave(wave, 42, 26), /Invalid raid/);
  for (const rng of [0, -1, 1.5, 2147483647, NaN]) assert.throws(() => planWave(1, rng, 26), /Invalid raid RNG/);
  for (const id of [7, -1, 1.5, 1e9 - 3]) assert.throws(() => planWave(1, 42, id), /Invalid raid/);
  const ticket = planWave(1, 42, 26).tickets[0];
  for (const active of [-1, 151, 1.5]) assert.throws(() => nextSpawn([ticket], active, () => true), /Invalid raid/);
  assert.throws(() => nextSpawn([ticket, ticket], 0, () => true), /Duplicate raid/);
  assert.throws(() => nextSpawn(Array(151).fill(ticket), 0, () => true), /queue exceeds/);
  for (const change of [t => t.entryOffset = 63, t => t.edge = 'diagonal', t => t.kind = 'unknown', t => t.leaderId = 99, t => t.squadId = 8]) {
    const malformed = { ...ticket }; change(malformed);
    assert.throws(() => nextSpawn([malformed], 0, () => true), /Invalid raid/);
  }
  assert.equal(waveBudget(1e9), 60, 'difficulty growth saturates instead of overflowing');
});
