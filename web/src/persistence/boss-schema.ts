import { BOSS_CONTENT as B } from '../simulation/boss-content.ts';
import { bossLayoutShape } from '../simulation/boss-layouts.ts';
import { canonical } from '../simulation/canonical.ts';
import { active, distance, index, same } from '../simulation/model.ts';
import type { Cell, World } from '../simulation/model.ts';
import type { BossLayout, BossEffect } from '../simulation/boss-model.ts';

function assert(ok: unknown, message: string): asserts ok {
  if (!ok) throw new Error(`Invalid save: ${message}`);
}
function object(value: unknown, keys: string[]): asserts value is Record<string, unknown> {
  assert(typeof value === 'object' && value !== null && !Array.isArray(value), 'boss object');
  assert(Object.keys(value).sort().join('|') === [...keys].sort().join('|'), 'boss fields');
}
const integer = (value: unknown, minimum = 0, maximum = 1e9): value is number =>
  Number.isSafeInteger(value) && Number(value) >= minimum && Number(value) <= maximum;
function cell(value: unknown): asserts value is Cell {
  object(value, ['x', 'y']);
  assert(integer(value.x, 0, 127) && integer(value.y, 0, 127), 'boss cell');
}
function cells(value: unknown, maximum: number): asserts value is Cell[] {
  assert(Array.isArray(value) && value.length <= maximum, 'boss geometry limit');
  const seen = new Set<number>();
  for (const position of value) {
    cell(position); const key = index(position.x, position.y);
    assert(!seen.has(key), 'duplicate boss geometry cell'); seen.add(key);
  }
}
const kinds = ['bellkeeper', 'sluice', 'threadmother'];
const featureKinds = Object.keys(B.featureHealth);
const effectKinds = ['soundseek', 'ring', 'surge', 'feast', 'partition', 'reel', 'fire'];
const modes = Object.keys(B.counters);

/** Reject malformed data before walkability or movement reads boss blockers. */
export function validateBossPreflight(world: World): void {
  const bosses = world.bosses;
  object(bosses, ['encounters', 'features', 'effects', 'work', 'status']);
  assert(Array.isArray(bosses.encounters) && bosses.encounters.length <= B.maxEncounters, 'boss encounter limit');
  assert(Array.isArray(bosses.features) && bosses.features.length <= B.maxFeatures, 'boss feature limit');
  assert(Array.isArray(bosses.effects) && bosses.effects.length <= B.maxEffects, 'boss effect limit');
  assert(Array.isArray(bosses.work) && bosses.work.length <= 6, 'boss work limit');
  assert(typeof bosses.status === 'string' && bosses.status.length < 300, 'boss status');
  for (const encounter of bosses.encounters) {
    object(encounter, ['owner', 'kind', 'wave', 'layout', 'drained', 'arrived', 'moves', 'readyAt', 'exposedUntil', 'noise', 'feast', 'deadAt']);
    assert(kinds.includes(encounter.kind) && typeof encounter.arrived === 'boolean', 'boss encounter kind/arrival');
    validateLayoutShape(encounter.layout);
    assert(encounter.layout.kind === encounter.kind, 'boss layout identity');
    assert(Array.isArray(encounter.drained) && encounter.drained.length === encounter.layout.channels.length && encounter.drained.every(value => typeof value === 'boolean'), 'drained channels');
    if (encounter.noise !== null) {
      object(encounter.noise, ['cell', 'at']); cell(encounter.noise.cell);
    }
    if (encounter.feast !== null) object(encounter.feast, ['channel', 'expiresAt']);
  }
  for (const feature of bosses.features) {
    object(feature, ['id', 'owner', 'kind', 'x', 'y', 'health', 'group', 'channel']);
    assert(featureKinds.includes(feature.kind), 'boss feature kind'); cell({ x: feature.x, y: feature.y });
  }
  for (const effect of bosses.effects) {
    object(effect, ['id', 'owner', 'kind', 'source', 'origin', 'cells', 'due', 'damage', 'feature', 'channel', 'group']);
    assert(effectKinds.includes(effect.kind), 'boss effect kind'); cell(effect.origin); cells(effect.cells, 32);
  }
  for (const work of bosses.work) {
    object(work, ['actor', 'mode', 'target', 'startedAt', 'progress']);
    assert(modes.includes(work.mode), 'boss counter mode');
  }
}

function validateLayoutShape(layout: BossLayout): void {
  object(layout, ['kind', 'center', 'resonators', 'channels', 'anchors', 'webs']);
  assert(kinds.includes(layout.kind), 'boss layout kind'); cell(layout.center);
  cells(layout.resonators, 3); cells(layout.anchors, 2); cells(layout.webs, 6);
  assert(Array.isArray(layout.channels) && layout.channels.length <= 2, 'boss channel limit');
  for (const channel of layout.channels) {
    object(channel, ['cells', 'flow', 'pool', 'valve']);
    cells(channel.cells, 9); cell(channel.flow); cell(channel.pool); cell(channel.valve);
  }
  assert(canonical(layout) === canonical(bossLayoutShape(layout.kind, layout.center)), 'immutable boss layout geometry');
}

/** Full references run after ordinary entity IDs have been validated and reserved. */
export function validateBosses(world: World, ids: Set<number>, occupied: Set<number>): void {
  const bosses = world.bosses, owners = new Map<number, typeof bosses.encounters[number]>();
  const addId = (id: number): void => {
    assert(integer(id, 8, world.nextId - 1) && !ids.has(id), 'duplicate boss ID'); ids.add(id);
  };
  for (const encounter of bosses.encounters) {
    assert(integer(encounter.owner, 8, world.nextId - 1) && !owners.has(encounter.owner), 'boss owner identity');
    owners.set(encounter.owner, encounter);
    assert(integer(encounter.wave, 1, world.combat.wave) && encounter.wave % 4 === 0 && integer(encounter.moves) && integer(encounter.readyAt, 0, world.tick + 60) && integer(encounter.exposedUntil, 0, world.tick + B.exposure.ticks), 'boss encounter counters');
    assert(encounter.arrived || encounter.moves === 0 && encounter.feast === null, 'boss arrival history');
    const owner = world.combat.raiders.find(raider => raider.id === encounter.owner);
    if (encounter.deadAt === null) assert(owner?.kind === encounter.kind && encounter.wave === world.combat.wave, 'living boss encounter owner');
    else {
      assert(integer(encounter.deadAt, 0, world.tick) && !owner && !ids.has(encounter.owner), 'historical boss owner');
      ids.add(encounter.owner);
      assert(encounter.noise === null && encounter.feast === null && !bosses.features.some(feature => feature.owner === encounter.owner) && bosses.effects.some(effect => effect.owner === encounter.owner && effect.kind === 'fire') && bosses.effects.every(effect => effect.owner !== encounter.owner || effect.kind === 'fire'), 'dead boss cleanup');
    }
    if (encounter.noise) assert(encounter.kind === 'bellkeeper' && integer(encounter.noise.at, Math.max(0, world.tick - B.noise.lifetime), world.tick), 'boss noise history');
    if (encounter.feast) assert(encounter.kind === 'sluice' && integer(encounter.feast.channel, 0, encounter.layout.channels.length - 1) && !encounter.drained[encounter.feast.channel] && integer(encounter.feast.expiresAt, world.tick + 1, world.tick + B.feast.travelTicks), 'boss feast route');
  }
  for (const raider of world.combat.raiders) if (kinds.includes(raider.kind)) assert(owners.has(raider.id), 'missing live boss encounter');
  assert(bosses.encounters.filter(encounter => encounter.deadAt !== null).length <= world.combat.stats.bosses, 'historical boss death ledger');
  for (const feature of bosses.features) addId(feature.id);
  for (const effect of bosses.effects) addId(effect.id);
  const positions = new Set<number>();
  for (const feature of bosses.features) {
    const encounter = owners.get(feature.owner);
    assert(encounter && encounter.deadAt === null && feature.owner < feature.id, 'boss feature owner');
    assert(integer(feature.health, 1, B.featureHealth[feature.kind]), 'boss feature health');
    assert(feature.group === null || integer(feature.group, feature.owner, feature.id - 1), 'boss feature group');
    assert(feature.channel === null || integer(feature.channel, 0, encounter.layout.channels.length - 1), 'boss feature channel');
    if (feature.kind === 'resonator') assert(encounter.kind === 'bellkeeper' && feature.group === null && feature.channel === null && encounter.layout.resonators.some(position => same(position, feature)), 'resonator layout');
    else if (feature.kind === 'valve') assert(encounter.kind === 'sluice' && feature.group === null && feature.channel !== null && same(encounter.layout.channels[feature.channel]!.valve, feature), 'valve layout');
    else {
      assert(encounter.kind === 'threadmother' && feature.group !== null && feature.channel === null, 'web/anchor/line ownership');
      if (feature.kind === 'anchor') assert(encounter.layout.anchors.some(position => same(position, feature)), 'anchor layout');
      if (feature.kind === 'web') assert(encounter.layout.webs.some(position => same(position, feature)), 'web layout');
    }
    const key = index(feature.x, feature.y);
    assert(!positions.has(key), 'duplicate boss feature cell'); positions.add(key);
    if (feature.kind === 'resonator' || feature.kind === 'anchor') {
      assert(!occupied.has(key) && !same(feature, world.heart) && !world.structures.some(structure => same(structure, feature)) && !world.jobs.some(job => active(job) && job.kind === 'build' && same(job, feature)), 'solid boss footprint');
      occupied.add(key);
    }
  }
  const burning = new Set<number>();
  for (const effect of bosses.effects) {
    const encounter = owners.get(effect.owner);
    assert(encounter && effect.owner < effect.id && effect.cells.length > 0, 'boss effect owner/geometry');
    assert(effect.source === (effect.kind === 'fire' ? effect.source : effect.owner) && (effect.kind !== 'fire' || world.people.some(person => person.id === effect.source)), 'boss effect source');
    assert(effect.kind === 'fire' || encounter.deadAt === null && encounter.arrived, 'nonfire effect from dead or unarrived boss');
    const content = B[effect.kind], maximum = effect.kind === 'fire' ? B.fire.maxDelay : effect.kind === 'ring' ? B.ring.windup + (B.ring.radii - 1) * B.ring.gap : content.windup;
    const damage = 'damage' in content ? content.damage : 0;
    assert(integer(effect.due, world.tick + 1, world.tick + maximum) && effect.damage === damage, 'boss effect timing/damage');
    assert(effect.feature === null || integer(effect.feature, effect.owner + 1, effect.id - 1), 'boss effect feature identity');
    assert(effect.channel === null || integer(effect.channel, 0, encounter.layout.channels.length - 1), 'boss effect channel');
    assert(effect.group === null || integer(effect.group, effect.owner, world.nextId - 1), 'boss effect group');
    if (effect.kind === 'ring') {
      const feature = bosses.features.find(feature => feature.id === effect.feature);
      assert(encounter.kind === 'bellkeeper' && feature?.kind === 'resonator' && feature.owner === effect.owner && same(feature, effect.origin) && effect.channel === null && effect.group === null, 'resonator ring source');
    } else if (effect.kind === 'surge' || effect.kind === 'feast') {
      assert(encounter.kind === 'sluice' && effect.channel !== null && !encounter.drained[effect.channel] && effect.feature === null && effect.group === null, 'channel effect commitment');
      const channel = encounter.layout.channels[effect.channel]!;
      assert(canonical(effect.cells) === canonical(effect.kind === 'surge' ? channel.cells : [channel.pool]), 'locked channel effect geometry');
    } else if (effect.kind === 'partition' || effect.kind === 'reel') {
      assert(encounter.kind === 'threadmother' && effect.group === effect.id && effect.feature === null && effect.channel === null, 'thread effect commitment');
      if (effect.kind === 'partition') assert(canonical(effect.cells) === canonical([...encounter.layout.anchors, ...encounter.layout.webs]), 'locked partition geometry');
    } else if (effect.kind === 'fire') {
      assert(encounter.kind === 'threadmother' && effect.feature !== null && effect.group !== null && effect.group < effect.feature && effect.channel === null, 'fire feature history');
      assert([...encounter.layout.anchors, ...encounter.layout.webs].some(position => same(position, effect.origin)), 'historical fire origin');
      assert(effect.group === effect.owner || !ids.has(effect.group), 'historical fire group');
      if (encounter.deadAt !== null) assert(effect.due <= encounter.deadAt + B.fire.maxDelay, 'finite fire after owner death');
      assert(!burning.has(effect.feature), 'duplicate committed feature fire'); burning.add(effect.feature);
      const feature = bosses.features.find(feature => feature.id === effect.feature);
      if (feature) assert(feature.owner === effect.owner && (feature.kind === 'anchor' || feature.kind === 'web') && feature.group === effect.group && same(feature, effect.origin), 'living fire source feature');
      else assert(!ids.has(effect.feature), 'historical fire feature identity');
      assert(effect.cells.some(position => same(position, effect.origin)), 'fire retains origin');
    } else assert(encounter.kind === 'bellkeeper' && effect.feature === null && effect.channel === null && effect.group === null, 'soundseek commitment');
    validateEffectGeometry(effect);
    if (effect.kind === 'reel') {
      const expected = ray(effect.origin, effect.cells[0]!).slice(0, -1);
      const actual = bosses.features.filter(feature => feature.kind === 'line' && feature.group === effect.id).sort((a, b) => a.id - b.id).map(({ x, y }) => ({ x, y }));
      assert(canonical(actual) === canonical(expected), 'locked reel line geometry');
    }
  }
  for (const feature of bosses.features) {
    if (feature.kind === 'line') assert(bosses.effects.some(effect => effect.id === feature.group && effect.kind === 'reel' && effect.owner === feature.owner), 'line reel group');
    else if (feature.group !== null && feature.group !== feature.owner) assert(!ids.has(feature.group), 'historical partition group');
  }
  const workers = new Set<number>();
  for (const work of bosses.work) {
    const person = world.people.find(person => person.id === work.actor), feature = bosses.features.find(feature => feature.id === work.target);
    assert(person?.life === 'alive' && !workers.has(work.actor) && feature, 'boss counter ownership'); workers.add(work.actor);
    assert(integer(work.startedAt, 0, world.tick) && integer(work.progress, 1, B.counters[work.mode] - 1) && work.progress <= world.tick - work.startedAt + 1, 'boss counter progress');
    assert(world.combat.orders.some(order => order.actor === work.actor && order.mode === work.mode && same(order.target, feature)), 'boss counter order');
    const encounter = owners.get(feature.owner)!;
    assert(work.mode === 'strike' ? feature.kind === 'resonator' || feature.kind === 'anchor'
      : work.mode === 'drain' ? feature.kind === 'valve' && feature.channel !== null && !encounter.drained[feature.channel]
        : work.mode === 'cut' ? feature.kind === 'anchor' || feature.kind === 'web' || feature.kind === 'line'
          : (feature.kind === 'anchor' || feature.kind === 'web') && !bosses.effects.some(effect => effect.kind === 'fire' && effect.feature === feature.id), 'boss counter target');
  }
}

function validateEffectGeometry(effect: BossEffect): void {
  if (effect.kind === 'soundseek' || effect.kind === 'feast' || effect.kind === 'reel') assert(effect.cells.length === 1, 'boss point effect');
  if (effect.kind === 'reel') assert(distance(effect.origin, effect.cells[0]!) > 1 && distance(effect.origin, effect.cells[0]!) <= B.reel.range, 'boss reel range');
  if (effect.kind === 'ring') {
    const radius = Math.max(Math.abs(effect.cells[0]!.x - effect.origin.x), Math.abs(effect.cells[0]!.y - effect.origin.y));
    assert(integer(radius, 1, B.ring.radii) && effect.cells.every(position => Math.max(Math.abs(position.x - effect.origin.x), Math.abs(position.y - effect.origin.y)) === radius), 'locked boss ring');
  }
  if (effect.kind === 'fire') assert(effect.cells.every(position => distance(effect.origin, position) <= 1), 'locked fire pulse');
}

/** Immutable Bresenham geometry; later obstacles do not rewrite a committed line. */
function ray(origin: Cell, target: Cell): Cell[] {
  const cells: Cell[] = []; let x = origin.x, y = origin.y;
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
