import { COMBAT_CONTENT as C } from '../simulation/combat-content.ts';
import { active, distance, index, inside, MOVE_TICKS, same, walkable } from '../simulation/model.ts';
import type { Cell, World } from '../simulation/model.ts';
import type { CombatRef } from '../simulation/combat-model.ts';
import { breachable } from '../simulation/combat-navigation.ts';
import { ENCOUNTER_CONTENT as E } from '../simulation/encounter-content.ts';
import { RAID_KINDS, RAID_EDGES } from '../simulation/raid-model.ts';
import type { RaidKind, RaidEdge } from '../simulation/raid-model.ts';
import { RAID_CONTENT, RAID_COST, waveBudget } from '../simulation/raid-director.ts';

function assert(ok: unknown, message: string): asserts ok {
  if (!ok) throw new Error(`Invalid save: ${message}`);
}
function object(value: unknown, keys: string[]): asserts value is Record<string, unknown> {
  assert(typeof value === 'object' && value !== null && !Array.isArray(value), 'combat object');
  assert(Object.keys(value).sort().join('|') === [...keys].sort().join('|'), 'combat fields');
}
const integer = (value: unknown, minimum = 0, maximum = 1e9): value is number =>
  Number.isSafeInteger(value) && Number(value) >= minimum && Number(value) <= maximum;
function cell(value: unknown): asserts value is Cell {
  object(value, ['x', 'y']);
  assert(integer(value.x, 0, 127) && integer(value.y, 0, 127), 'combat cell');
}
function reference(world: World, value: unknown): asserts value is CombatRef {
  object(value, ['kind', 'id']);
  assert(integer(value.id, 1, world.nextId - 1), 'combat reference identity');
  assert(value.kind === 'heart' ? value.id === world.heart.id
    : value.kind === 'person' ? world.people.some(p => p.id === value.id)
      : value.kind === 'raider' ? world.combat.raiders.some(r => r.id === value.id)
        : value.kind === 'structure' ? world.structures.some(s => s.id === value.id)
          : value.kind === 'barricade' && world.combat.barricades.some(b => b.id === value.id), 'combat reference');
}
function unobstructedRay(origin: Cell, target: Cell): Cell[] {
  const cells: Cell[] = [];
  let x = origin.x, y = origin.y;
  const dx = Math.abs(target.x - x), sx = x < target.x ? 1 : -1;
  const dy = -Math.abs(target.y - y), sy = y < target.y ? 1 : -1;
  let error = dx + dy;
  while (x !== target.x || y !== target.y) {
    const twice = 2 * error;
    if (twice >= dy) { error += dy; x += sx; }
    if (twice <= dx) { error += dx; y += sy; }
    cells.push({ x, y });
  }
  return cells;
}

/** Validate committed identities and immutable geometry, never regenerate a wave. */
export function validateCombat(world: World, ids: Set<number>, occupied: Set<number>): void {
  const combat = world.combat;
  object(combat, ['phase', 'wave', 'warningAt', 'spawnAt', 'completed', 'edge', 'pending', 'budget', 'spent', 'recoveryUntil', 'history', 'waveStart', 'barricades', 'raiders', 'attacks', 'orders', 'defenses', 'cooldowns', 'stats', 'lootWood', 'status']);
  assert(['peace', 'warning', 'combat', 'recovery'].includes(combat.phase), 'combat phase');
  assert(integer(combat.wave) && integer(combat.completed, 0, combat.wave), 'wave counters');
  assert(integer(combat.recoveryUntil) && integer(combat.warningAt, combat.recoveryUntil) && integer(combat.spawnAt, combat.warningAt), 'raid deadlines');
  assert(RAID_EDGES.includes(combat.edge), 'raid approach');
  assert(typeof combat.status === 'string' && combat.status.length < 300, 'combat status');
  assert(Array.isArray(combat.pending) && combat.pending.length <= RAID_CONTENT.maxPending, 'pending raiders');
  assert(Array.isArray(combat.raiders) && combat.raiders.length <= RAID_CONTENT.maxActive && Array.isArray(combat.attacks) && combat.attacks.length <= 256, 'combat entity limits');
  assert(Array.isArray(combat.barricades) && combat.barricades.length <= E.rampart.maxBarricades, 'barricade limit');
  assert(Array.isArray(combat.orders) && combat.orders.length <= world.people.length && Array.isArray(combat.cooldowns) && combat.cooldowns.length <= world.people.length, 'actor combat limits');
  assert(Array.isArray(combat.defenses) && combat.defenses.length <= world.structures.length, 'defense limit');
  object(combat.stats, ['spawned', 'kills', 'shots', 'traps', 'heartDamage', 'colonyDamage', 'structuresLost', 'repairs', 'bosses']);
  for (const n of Object.values(combat.stats)) assert(integer(n), 'combat statistic');
  assert(combat.stats.heartDamage === 100 - world.heart.health, 'heart damage ledger');
  assert(combat.stats.spawned === combat.stats.kills + combat.raiders.length, 'raider kill ledger');
  assert(combat.stats.bosses <= combat.stats.kills && integer(combat.lootWood) && combat.lootWood === combat.stats.kills + 4 * combat.stats.bosses, 'exactly-once raid loot');
  assert(combat.budget === (combat.wave === 0 ? 0 : waveBudget(combat.wave)) && integer(combat.spent, 0, combat.budget), 'raid budget');
  if (world.scenario === 'colony') assert(combat.spent === combat.budget, 'announced colony budget');
  object(combat.waveStart, ['tick', 'spawned', 'kills', 'deaths', 'ammo', 'structuresLost', 'repairs']);
  const start = combat.waveStart;
  assert(integer(start.tick, 0, world.tick) && integer(start.spawned, 0, combat.stats.spawned) && start.kills === start.spawned, 'wave start identity ledger');
  assert(world.statistics && world.consumed && integer(start.deaths, 0, world.statistics.deaths) && integer(start.ammo, 0, world.consumed.ammo) && integer(start.structuresLost, 0, combat.stats.structuresLost) && integer(start.repairs, 0, combat.stats.repairs), 'wave start ledger');
  if (combat.phase === 'peace') assert(combat.wave === 0 && combat.completed === 0 && combat.stats.spawned === 0 && Object.values(start).every(n => n === 0) && !combat.pending.length && !combat.raiders.length && !combat.attacks.length && !combat.barricades.length, 'peace state');
  else assert(combat.wave > 0 && combat.completed === combat.wave - (combat.phase === 'recovery' ? 0 : 1), 'phase wave completion');
  if (combat.phase === 'warning') assert(combat.stats.spawned === start.spawned && !combat.raiders.length && !combat.attacks.length && !combat.barricades.length, 'warning state');
  if (combat.phase === 'recovery') assert(!combat.pending.length && !combat.raiders.length && !combat.attacks.length && !combat.barricades.length, 'recovery state');
  validateHistory(world);

  const addId = (id: number) => {
    assert(integer(id, 8, world.nextId - 1) && !ids.has(id), 'duplicate combat ID'); ids.add(id);
  };
  const squad = (value: { id: number; squadId: number | null; leaderId: number | null }) => {
    assert(value.squadId === null ? value.leaderId === null : integer(value.squadId, 8, value.id) && value.leaderId === value.squadId, 'squad identity');
  };
  let previousTicket = 0, remainingCost = 0;
  for (const ticket of combat.pending) {
    object(ticket, ['id', 'kind', 'squadId', 'leaderId', 'edge', 'entryOffset']);
    addId(ticket.id); squad(ticket);
    assert(RAID_KINDS.includes(ticket.kind) && RAID_EDGES.includes(ticket.edge) && ticket.edge === combat.edge && integer(ticket.entryOffset, -63, 62), 'spawn commitment');
    assert(ticket.id > previousTicket, 'spawn queue order'); previousTicket = ticket.id;
    remainingCost += RAID_COST[ticket.kind];
  }
  for (const barricade of combat.barricades) {
    object(barricade, ['id', 'owner', 'x', 'y', 'health', 'expiresAt']); addId(barricade.id);
    cell({ x: barricade.x, y: barricade.y });
    assert(integer(barricade.health, 1, E.rampart.health) && integer(barricade.expiresAt, world.tick + 1, world.tick + E.rampart.lifetime), 'barricade health/lifetime');
    assert(combat.raiders.some(r => r.id === barricade.owner && r.kind === 'captain') && barricade.owner < barricade.id, 'barricade owner');
    const key = index(barricade.x, barricade.y);
    assert(!occupied.has(key) && world.terrain[key] !== 2 && !same(barricade, world.heart) && !world.structures.some(s => same(s, barricade)) && !world.resources.some(r => r.remaining > 0 && same(r, barricade)) && !world.piles.some(p => same(p, barricade)) && !world.jobs.some(j => active(j) && j.kind === 'build' && same(j, barricade)), 'barricade footprint');
    occupied.add(key);
  }
  for (const raider of combat.raiders) {
    object(raider, ['id', 'kind', 'x', 'y', 'health', 'facing', 'squadId', 'leaderId', 'loweredUntil', 'exposedUntil', 'specialCount', 'charge', 'next', 'progress', 'route', 'routeVersion', 'retryAt', 'cooldown', 'target']);
    addId(raider.id); squad(raider);
    assert(RAID_KINDS.includes(raider.kind) && RAID_EDGES.includes(raider.facing), 'raider kind/facing');
    const tuning = profile(raider.kind); remainingCost += RAID_COST[raider.kind];
    cell({ x: raider.x, y: raider.y });
    assert(walkable(world, raider) && integer(raider.health, 1, tuning.health), 'living raider');
    assert(integer(raider.loweredUntil, 0, world.tick + E.shield.loweredTicks) && (raider.kind === 'shield' || raider.loweredUntil === 0), 'shield recovery');
    assert(integer(raider.exposedUntil, 0, world.tick + E.captain.exposureTicks) && (raider.kind === 'captain' || raider.exposedUntil === 0), 'Captain exposure');
    assert(integer(raider.specialCount) && (raider.kind === 'captain' || raider.specialCount === 0), 'Captain special sequence');
    assert(integer(raider.progress, 0, MOVE_TICKS - 1) && integer(raider.routeVersion, 0, world.terrainVersion) && integer(raider.retryAt), 'raider movement counters');
    assert(integer(raider.cooldown, 0, world.tick + Math.max(tuning.cooldown, raider.kind === 'healer' ? E.heal.cooldown : 0)), 'raider cooldown');
    if (raider.next) { cell(raider.next); assert(distance(raider, raider.next) === 1 && walkable(world, raider.next), 'raider movement edge'); }
    else assert(raider.next === null && raider.progress === 0, 'raider progress without edge');
    assert(Array.isArray(raider.route) && raider.route.length <= world.width * world.height, 'raider route');
    let previous: Cell = raider.next ?? raider;
    for (const position of raider.route) {
      cell(position); assert(distance(previous, position) === 1, 'raider route adjacency');
      if (raider.routeVersion === world.terrainVersion) assert(walkable(world, position) || breachable(world, position), 'raider route crosses natural obstruction');
      previous = position;
    }
    if (raider.target !== null) reference(world, raider.target);
    if (raider.charge !== null) {
      const charge = raider.charge;
      object(charge, ['cells', 'cursor', 'hits']);
      assert(raider.kind === 'captain' && raider.specialCount % 2 === 1 && raider.next === null && !raider.route.length, 'charge owner');
      assert(Array.isArray(charge.cells) && charge.cells.length >= 1 && charge.cells.length <= E.charge.range && integer(charge.cursor, 0, charge.cells.length - 1), 'charge progress');
      for (const position of charge.cells) cell(position);
      const v = vector(raider.facing), first = charge.cells[0]!, origin = { x: first.x - v.x, y: first.y - v.y }; cell(origin);
      const expected = chargeCells(origin, raider.facing);
      assert(equalCells(charge.cells, expected) && same(raider, charge.cursor === 0 ? origin : charge.cells[charge.cursor - 1]!), 'saved charge trajectory');
      assert(Array.isArray(charge.hits) && charge.hits.length <= E.charge.range && new Set(charge.hits).size === charge.hits.length && charge.hits.every(id => integer(id, 1, world.nextId - 1) && id !== raider.id), 'charge hit identities');
    }
    for (const position of [raider, ...(raider.next ? [raider.next] : [])]) {
      const key = index(position.x, position.y); assert(!occupied.has(key), 'duplicate combat occupied/reserved cell'); occupied.add(key);
    }
  }
  const deaths = combat.stats.kills - start.kills;
  assert(combat.stats.spawned - start.spawned + combat.pending.length <= RAID_CONTENT.maxPending && remainingCost + deaths <= combat.spent && combat.spent <= remainingCost + deaths * RAID_COST.captain, 'finite wave expenditure');
  for (const member of [...combat.pending, ...combat.raiders]) {
    const leader = [...combat.pending, ...combat.raiders].find(other => other.id === member.leaderId);
    if (leader) assert(leader.squadId === member.squadId && leader.leaderId === leader.id, 'living squad leader');
    else if (member.leaderId !== null) assert(!ids.has(member.leaderId) && !world.structures.some(s => s.id === member.leaderId) && !world.jobs.some(j => j.id === member.leaderId) && !world.piles.some(p => p.id === member.leaderId), 'historical squad leader');
  }

  const sources = new Set<string>();
  for (const attack of combat.attacks) {
    object(attack, ['id', 'kind', 'recipient', 'source', 'origin', 'target', 'cells', 'due', 'damage', 'faction']);
    addId(attack.id); reference(world, attack.source); cell(attack.origin); cell(attack.target);
    assert(['melee', 'bolt', 'trap', 'arrow', 'heal', 'charge', 'rampart'].includes(attack.kind) && ['colony', 'hostile'].includes(attack.faction), 'attack kind/faction');
    const key = `${attack.source.kind}:${attack.source.id}`;
    assert(!sources.has(key), 'multiple pending attacks from one source'); sources.add(key);
    const person = world.people.find(p => attack.source.kind === 'person' && p.id === attack.source.id && p.life === 'alive');
    const raider = combat.raiders.find(r => attack.source.kind === 'raider' && r.id === attack.source.id);
    const structure = world.structures.find(s => attack.source.kind === 'structure' && s.id === attack.source.id && s.health > 0);
    let damage: number, windup: number, range: number, cooldown: number, until: number | undefined;
    if (person) {
      assert(attack.kind === 'melee' && attack.faction === 'colony', 'guard attack policy');
      damage = C.guard.damage; windup = C.guard.windup; range = 1; cooldown = C.guard.cooldown; until = combat.cooldowns.find(c => c.actor === person.id)?.until;
    } else if (raider) {
      assert(attack.faction === 'hostile' && raider.charge === null, 'raider attack policy');
      const tuning = profile(raider.kind);
      damage = tuning.damage; windup = tuning.windup; range = 1; cooldown = tuning.cooldown; until = raider.cooldown;
      if (attack.kind === 'arrow') { assert(raider.kind === 'archer', 'archer attack'); range = E.archer.range; }
      else if (attack.kind === 'heal') { assert(raider.kind === 'healer', 'healer attack'); damage = 0; windup = E.heal.windup; range = E.heal.range; cooldown = E.heal.cooldown; }
      else if (attack.kind === 'charge' || attack.kind === 'rampart') {
        assert(raider.kind === 'captain' && raider.specialCount > 0 && raider.specialCount % 2 === (attack.kind === 'charge' ? 1 : 0), 'Captain commitment sequence');
        damage = attack.kind === 'charge' ? E.charge.damage : 0; windup = E[attack.kind].windup; cooldown = E[attack.kind].cooldown; range = 4;
      } else assert(attack.kind === 'melee', 'raider attack policy');
    } else if (structure?.kind === 'tower') {
      assert(attack.kind === 'bolt' && attack.faction === 'colony', 'tower attack policy');
      damage = C.tower.damage; windup = C.tower.windup; range = C.tower.range; cooldown = C.tower.cooldown; until = combat.defenses.find(d => d.structure === structure.id)?.cooldown;
    } else {
      assert(structure?.kind === 'trap' && attack.kind === 'trap' && attack.faction === 'colony', 'living attack source');
      damage = C.trap.damage; windup = C.trap.windup; range = 0; cooldown = C.trap.cooldown; until = combat.defenses.find(d => d.structure === structure.id)?.cooldown;
    }
    assert((attack.damage === damage || attack.kind === 'melee' && raider?.kind === 'breacher' && attack.damage === C.breacher.structureDamage) && integer(attack.due, world.tick + 1, world.tick + windup), 'attack timing or damage');
    assert(until === attack.due - windup + cooldown, 'attack launch cooldown');
    if (structure) assert(same(attack.origin, structure), 'stationary attack origin');
    else if (raider) assert(same(attack.origin, raider), 'committed raider origin');
    else assert(distance(attack.origin, person!) <= Math.ceil(windup / MOVE_TICKS), 'mobile attack origin');
    const reach = distance(attack.origin, attack.target);
    assert(reach <= range && (attack.kind === 'trap' ? reach === 0 : attack.kind === 'melee' ? reach === 1 : reach > 0), 'attack range');
    if (attack.kind === 'arrow') assert(reach >= E.archer.minRange && (attack.origin.x === attack.target.x || attack.origin.y === attack.target.y), 'archer cardinal lane');
    if (attack.kind === 'heal') {
      assert(integer(attack.recipient, 8, attack.id - 1) && attack.recipient !== raider!.id, 'healing recipient identity');
      const patient = combat.raiders.find(r => r.id === attack.recipient);
      assert(patient ? patient.squadId === raider!.squadId : !ids.has(attack.recipient!) && !world.structures.some(s => s.id === attack.recipient) && !world.jobs.some(j => j.id === attack.recipient) && !world.piles.some(p => p.id === attack.recipient), 'healing recipient faction/squad');
    } else assert(attack.recipient === null, 'unexpected attack recipient');
    assert(Array.isArray(attack.cells) && attack.cells.length > 0 && attack.cells.length <= C.tower.range + 1, 'attack geometry');
    for (const position of attack.cells) cell(position);
    const expected = attack.kind === 'charge' ? chargeCells(attack.origin, raider!.facing)
      : attack.kind === 'rampart' ? rampartCells(attack.origin, raider!.facing)
        : attack.kind === 'bolt' || attack.kind === 'arrow' ? unobstructedRay(attack.origin, attack.target) : [attack.target];
    assert(equalCells(attack.cells, expected) && same(attack.target, expected[expected.length - 1]!), 'locked attack geometry');
  }
  assert(combat.attacks.filter(a => a.kind === 'bolt').length <= combat.stats.shots && combat.attacks.filter(a => a.kind === 'trap').length <= combat.stats.traps, 'pending attacks paid ammunition');

  const actors = new Set<number>();
  for (const order of combat.orders) {
    object(order, ['actor', 'mode', 'target', 'resume']);
    assert(world.people.some(p => p.id === order.actor && p.life === 'alive') && !actors.has(order.actor), 'battle order owner'); actors.add(order.actor);
    assert(['rally', 'retreat', 'shelter', 'clear'].includes(order.mode), 'battle order mode');
    cell(order.target); if (order.resume !== null) cell(order.resume);
  }
  const defenseIds = new Set<number>();
  for (const defense of combat.defenses) {
    object(defense, ['structure', 'cooldown']);
    const structure = world.structures.find(s => s.id === defense.structure && (s.kind === 'tower' || s.kind === 'trap'));
    assert(structure && !defenseIds.has(defense.structure), 'defense reference'); defenseIds.add(defense.structure);
    assert(integer(defense.cooldown, 0, world.tick + (structure.kind === 'tower' ? C.tower.cooldown : C.trap.cooldown)), 'defense cooldown');
  }
  const cooldownActors = new Set<number>();
  for (const cooldown of combat.cooldowns) {
    object(cooldown, ['actor', 'until']);
    assert(world.people.some(p => p.id === cooldown.actor && p.life === 'alive') && !cooldownActors.has(cooldown.actor), 'guard cooldown owner'); cooldownActors.add(cooldown.actor);
    assert(integer(cooldown.until, 0, world.tick + C.guard.cooldown), 'guard cooldown');
  }
}

function profile(kind: RaidKind) { return kind === 'raider' || kind === 'breacher' ? C[kind] : E.profiles[kind]; }
function vector(facing: RaidEdge): Cell { return facing === 'north' ? { x: 0, y: -1 } : facing === 'east' ? { x: 1, y: 0 } : facing === 'south' ? { x: 0, y: 1 } : { x: -1, y: 0 }; }
function chargeCells(origin: Cell, facing: RaidEdge): Cell[] {
  const v = vector(facing);
  return Array.from({ length: E.charge.range }, (_, i) => ({ x: origin.x + v.x * (i + 1), y: origin.y + v.y * (i + 1) })).filter(inside);
}
function rampartCells(origin: Cell, facing: RaidEdge): Cell[] {
  const v = vector(facing), side = { x: -v.y, y: v.x };
  return [-2, -1, 1, 2].map(n => ({ x: origin.x + E.rampart.forward * v.x + n * side.x, y: origin.y + E.rampart.forward * v.y + n * side.y })).filter(inside);
}
function equalCells(actual: Cell[], expected: Cell[]): boolean { return actual.length === expected.length && actual.every((position, i) => same(position, expected[i]!)); }
function validateHistory(world: World): void {
  const c = world.combat;
  assert(Array.isArray(c.history) && c.history.length === Math.min(c.completed, RAID_CONTENT.historyLimit), 'bounded raid history');
  let previousEnd = 0;
  const totals = { spawned: 0, kills: 0, colonistsLost: 0, ammoSpent: 0, structuresLost: 0, repairs: 0 };
  for (const [i, summary] of c.history.entries()) {
    object(summary, ['wave', 'budget', 'spent', 'startedAt', 'endedAt', 'spawned', 'kills', 'colonistsLost', 'ammoSpent', 'structuresLost', 'repairs']);
    assert(summary.wave === c.completed - c.history.length + i + 1 && summary.budget === waveBudget(summary.wave) && integer(summary.spent, 0, summary.budget), 'raid summary identity/budget');
    assert(integer(summary.startedAt, previousEnd, world.tick) && integer(summary.endedAt, summary.startedAt, world.tick), 'raid summary chronology'); previousEnd = summary.endedAt;
    assert(integer(summary.spawned, 0, RAID_CONTENT.maxPending) && summary.kills === summary.spawned && summary.kills <= summary.spent && summary.spent <= summary.kills * RAID_COST.captain, 'completed raid summary');
    assert(integer(summary.colonistsLost, 0, world.people.length) && integer(summary.ammoSpent) && integer(summary.structuresLost) && integer(summary.repairs), 'raid summary measurements');
    for (const key of Object.keys(totals) as (keyof typeof totals)[]) totals[key] += summary[key];
  }
  assert(totals.spawned <= c.stats.spawned && totals.kills <= c.stats.kills && totals.colonistsLost <= world.statistics.deaths && totals.ammoSpent <= world.consumed.ammo && totals.structuresLost <= c.stats.structuresLost && totals.repairs <= c.stats.repairs, 'raid history lifetime ledger');
  const last = c.history[c.history.length - 1];
  if (last) {
    if (c.phase === 'recovery') assert(last.startedAt === c.waveStart.tick && last.budget === c.budget && last.spent === c.spent && last.spawned === c.stats.spawned - c.waveStart.spawned && last.kills === c.stats.kills - c.waveStart.kills && last.colonistsLost <= world.statistics.deaths - c.waveStart.deaths && last.ammoSpent <= world.consumed.ammo - c.waveStart.ammo && last.structuresLost <= c.stats.structuresLost - c.waveStart.structuresLost && last.repairs <= c.stats.repairs - c.waveStart.repairs && last.endedAt <= c.recoveryUntil, 'recovery summary ledger');
    else assert(c.waveStart.tick >= last.endedAt, 'next wave chronology');
  }
}
