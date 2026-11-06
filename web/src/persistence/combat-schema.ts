import { COMBAT_CONTENT as C } from '../simulation/combat-content.ts';
import { distance, index, MOVE_TICKS, same, walkable } from '../simulation/model.ts';
import type { Cell, World } from '../simulation/model.ts';
import type { CombatRef } from '../simulation/combat-model.ts';
import { breachable } from '../simulation/combat-navigation.ts';

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
        : value.kind === 'structure' && world.structures.some(s => s.id === value.id), 'combat reference');
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

/** Validate simulation-owned combat state before admitting its IDs or restoring it. */
export function validateCombat(world: World, ids: Set<number>, occupied: Set<number>): void {
  const combat = world.combat;
  object(combat, ['phase', 'wave', 'warningAt', 'spawnAt', 'completed', 'edge', 'pending', 'raiders', 'attacks', 'orders', 'defenses', 'cooldowns', 'stats', 'lootWood', 'status']);
  assert(['peace', 'warning', 'combat', 'recovery'].includes(combat.phase), 'combat phase');
  assert(integer(combat.wave, 0, 1) && integer(combat.completed, 0, combat.wave), 'first-wave counters');
  assert(integer(combat.warningAt) && integer(combat.spawnAt, combat.warningAt), 'raid deadlines');
  assert(['north', 'east', 'south', 'west'].includes(combat.edge), 'raid approach');
  assert(typeof combat.status === 'string' && combat.status.length < 300, 'combat status');
  assert(Array.isArray(combat.pending) && combat.pending.length <= 150 && combat.pending.every(kind => kind === 'raider' || kind === 'breacher'), 'pending raiders');
  assert(Array.isArray(combat.raiders) && combat.raiders.length <= C.maxRaiders && Array.isArray(combat.attacks) && combat.attacks.length <= C.maxAttacks, 'combat entity limits');
  assert(Array.isArray(combat.orders) && combat.orders.length <= world.people.length && Array.isArray(combat.cooldowns) && combat.cooldowns.length <= world.people.length, 'actor combat limits');
  assert(Array.isArray(combat.defenses) && combat.defenses.length <= world.structures.length, 'defense limit');
  object(combat.stats, ['spawned', 'kills', 'shots', 'traps', 'heartDamage', 'colonyDamage', 'structuresLost']);
  for (const n of Object.values(combat.stats)) assert(integer(n), 'combat statistic');
  assert(combat.stats.spawned === combat.stats.kills + combat.raiders.length, 'raider kill ledger');
  assert(integer(combat.lootWood) && combat.lootWood === combat.stats.kills, 'exactly-once raid loot');
  if (combat.phase === 'peace') assert(combat.wave === 0 && combat.completed === 0 && !combat.pending.length && !combat.raiders.length && !combat.attacks.length, 'peace state');
  else assert(combat.wave === 1, 'active first wave');
  if (combat.phase === 'warning') assert(combat.completed === 0 && !combat.raiders.length && !combat.attacks.length, 'warning state');
  if (combat.phase === 'combat') assert(combat.completed === 0, 'combat completion');
  if (combat.phase === 'recovery') assert(combat.completed === 1 && !combat.pending.length && !combat.raiders.length && !combat.attacks.length, 'recovery state');

  const addId = (id: number) => {
    assert(integer(id, 8, world.nextId - 1) && !ids.has(id), 'duplicate combat ID');
    ids.add(id);
  };
  for (const raider of combat.raiders) {
    object(raider, ['id', 'kind', 'x', 'y', 'health', 'next', 'progress', 'route', 'routeVersion', 'retryAt', 'cooldown', 'target']);
    addId(raider.id);
    assert(raider.kind === 'raider' || raider.kind === 'breacher', 'raider kind');
    cell({ x: raider.x, y: raider.y });
    assert(walkable(world, raider) && integer(raider.health, 1, C[raider.kind].health), 'living raider');
    assert(integer(raider.progress, 0, MOVE_TICKS - 1) && integer(raider.routeVersion, 0, world.terrainVersion) && integer(raider.retryAt), 'raider movement counters');
    assert(integer(raider.cooldown, 0, world.tick + C[raider.kind].cooldown), 'raider cooldown');
    if (raider.next) { cell(raider.next); assert(distance(raider, raider.next) === 1 && walkable(world, raider.next), 'raider movement edge'); }
    else assert(raider.next === null && raider.progress === 0, 'raider progress without edge');
    assert(Array.isArray(raider.route) && raider.route.length <= world.width * world.height, 'raider route');
    let previous: Cell = raider.next ?? raider;
    for (const position of raider.route) {
      cell(position);
      assert(distance(previous, position) === 1, 'raider route adjacency');
      if (raider.routeVersion === world.terrainVersion) assert(walkable(world, position) || breachable(world, position), 'raider route crosses natural obstruction');
      previous = position;
    }
    if (raider.target !== null) reference(world, raider.target);
    for (const position of [raider, ...(raider.next ? [raider.next] : [])]) {
      const key = index(position.x, position.y);
      assert(!occupied.has(key), 'duplicate combat occupied/reserved cell');
      occupied.add(key);
    }
  }

  const sources = new Set<string>();
  for (const attack of combat.attacks) {
    object(attack, ['id', 'kind', 'source', 'origin', 'target', 'cells', 'due', 'damage', 'faction']);
    addId(attack.id);
    reference(world, attack.source);
    cell(attack.origin); cell(attack.target);
    assert(['melee', 'bolt', 'trap'].includes(attack.kind) && ['colony', 'hostile'].includes(attack.faction), 'attack kind/faction');
    const key = `${attack.source.kind}:${attack.source.id}`;
    assert(!sources.has(key), 'multiple pending attacks from one source'); sources.add(key);
    const person = world.people.find(p => attack.source.kind === 'person' && p.id === attack.source.id && p.life === 'alive');
    const raider = combat.raiders.find(r => attack.source.kind === 'raider' && r.id === attack.source.id);
    const structure = world.structures.find(s => attack.source.kind === 'structure' && s.id === attack.source.id && s.health > 0);
    let damage: number, windup: number, range: number;
    if (person) {
      assert(attack.kind === 'melee' && attack.faction === 'colony', 'guard attack policy');
      damage = C.guard.damage; windup = C.guard.windup; range = 1;
    } else if (raider) {
      assert(attack.kind === 'melee' && attack.faction === 'hostile', 'raider attack policy');
      damage = C[raider.kind].damage; windup = C[raider.kind].windup; range = 1;
    } else if (structure?.kind === 'tower') {
      assert(attack.kind === 'bolt' && attack.faction === 'colony', 'tower attack policy');
      damage = C.tower.damage; windup = C.tower.windup; range = C.tower.range;
    } else {
      assert(structure?.kind === 'trap' && attack.kind === 'trap' && attack.faction === 'colony', 'living attack source');
      damage = C.trap.damage; windup = C.trap.windup; range = 0;
    }
    assert((attack.damage === damage || raider?.kind === 'breacher' && attack.damage === C.breacher.structureDamage) && integer(attack.due, world.tick + 1, world.tick + windup), 'attack timing or damage');
    if (structure) assert(same(attack.origin, structure), 'stationary attack origin');
    else assert(distance(attack.origin, (person ?? raider)!) <= Math.ceil(windup / MOVE_TICKS), 'mobile attack origin');
    assert(distance(attack.origin, attack.target) <= range, 'attack range');
    assert(Array.isArray(attack.cells) && attack.cells.length > 0 && attack.cells.length <= C.tower.range + 1, 'attack geometry');
    const expected = attack.kind === 'bolt' ? unobstructedRay(attack.origin, attack.target) : [attack.target];
    assert(attack.cells.length === expected.length, 'attack cell count');
    for (let i = 0; i < attack.cells.length; i++) {
      const position = attack.cells[i]!;
      cell(position);
      assert(same(position, expected[i]!), 'locked attack geometry');
    }
  }

  const actors = new Set<number>();
  for (const order of combat.orders) {
    object(order, ['actor', 'mode', 'target', 'resume']);
    assert(world.people.some(p => p.id === order.actor && p.life === 'alive') && !actors.has(order.actor), 'battle order owner'); actors.add(order.actor);
    assert(['rally', 'retreat', 'shelter'].includes(order.mode), 'battle order mode');
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
