import { distance, index, inside, neighbors, same, spatial, walkable } from './model.ts';
import type { Cell, Person } from './model.ts';
import type { CombatRef, Raider } from './combat-model.ts';
import type { SpawnTicket } from './raid-model.ts';
import type { BossApi, BossCounterMode, BossEffect, BossEncounter, BossFeature, BossFeatureKind, Bosses, BossWorld } from './boss-model.ts';
import type { BossKind } from './boss-layouts.ts';
import { bossFeaturePlacementAllowed, findBossLayout } from './boss-layouts.ts';
import { BOSS_CONTENT as B } from './boss-content.ts';
import { blocksShot, shotCells } from './combat-geometry.ts';

const cell = (at: Cell): Cell => ({ x: at.x, y: at.y });
const key = (at: Cell): number => index(at.x, at.y);
const bossKind = (kind: string): kind is BossKind => ['bellkeeper', 'sluice', 'threadmother'].includes(kind);
const counterMode = (mode: string): mode is BossCounterMode => ['strike', 'drain', 'cut', 'ignite'].includes(mode);
const encounter = (w: BossWorld, owner: number): BossEncounter | undefined => w.bosses.encounters.find(e => e.owner === owner);
const body = (w: BossWorld, owner: number): Raider | undefined => w.combat.raiders.find(r => r.id === owner && r.health > 0);
const copyCells = (cells: Cell[]): Cell[] => cells.map(cell);
const sortEffects = (w: BossWorld): void => { w.bosses.effects.sort((a, b) => a.due - b.due || a.id - b.id); };

export function initialBosses(): Bosses { return { encounters: [], features: [], effects: [], work: [], status: 'No environmental boss encounter' }; }
function feature(w: BossWorld, owner: number, kind: BossFeatureKind, at: Cell, group: number | null = null, channel: number | null = null): BossFeature {
  const value: BossFeature = { id: w.nextId++, owner, kind, ...cell(at), health: B.featureHealth[kind], group, channel };
  w.bosses.features.push(value); if (kind === 'resonator' || kind === 'anchor') w.terrainVersion++; return value;
}
export function prepareBossSpawn(w: BossWorld, ticket: SpawnTicket): boolean {
  if (!bossKind(ticket.kind) || encounter(w, ticket.id)) return true;
  const needed = ticket.kind === 'bellkeeper' ? 3 : ticket.kind === 'sluice' ? 2 : 8;
  if (w.bosses.encounters.length >= B.maxEncounters || w.bosses.features.length + needed > B.maxFeatures) { w.bosses.status = 'Boss entry deferred: environmental encounter capacity is occupied'; return false; }
  const placement = findBossLayout(w, ticket.kind, ticket.edge);
  if (!placement.layout) { w.bosses.status = `Boss entry deferred: ${placement.reason} (${placement.attempts} candidate sites checked)`; return false; }
  const layout = placement.layout;
  w.bosses.encounters.push({ owner: ticket.id, kind: ticket.kind, wave: w.combat.wave, layout, drained: layout.channels.map(() => false), arrived: false, moves: 0, readyAt: w.tick, exposedUntil: 0, noise: null, feast: null, deadAt: null });
  for (const at of layout.resonators) feature(w, ticket.id, 'resonator', at);
  layout.channels.forEach((channel, n) => feature(w, ticket.id, 'valve', channel.valve, null, n));
  for (const at of layout.anchors) feature(w, ticket.id, 'anchor', at, ticket.id);
  for (const at of layout.webs) feature(w, ticket.id, 'web', at, ticket.id);
  w.bosses.status = `${ticket.kind} encounter established at ${layout.center.x},${layout.center.y}; counters require adjacent work`;
  return true;
}
export function bossBlocksCell(w: BossWorld, at: Cell): boolean { return w.bosses.features.some(f => (f.kind === 'anchor' || f.kind === 'resonator') && same(f, at)); }
export function bossFeatureAt(w: BossWorld, at: Cell): BossFeature | null { return w.bosses.features.find(f => same(f, at)) ?? null; }
export function bossCellMoveTicks(w: BossWorld, at: Cell): 4 | 6 | 8 {
  if (w.bosses.features.some(f => f.kind === 'web' && same(f, at))) return B.movement.web;
  if (w.bosses.encounters.some(e => e.deadAt === null && e.layout.channels.some((channel, n) => !e.drained[n] && channel.cells.some(p => same(p, at))))) return B.movement.wet;
  return B.movement.ordinary;
}
export function bossDamage(w: BossWorld, owner: number, amount: number): number {
  return (encounter(w, owner)?.exposedUntil ?? 0) > w.tick ? amount + Math.floor(amount * B.exposure.percent / 100) : amount;
}
function expose(w: BossWorld, e: BossEncounter): void { e.exposedUntil = Math.max(e.exposedUntil, w.tick + B.exposure.ticks); e.readyAt = Math.max(e.readyAt, e.exposedUntil); }

/** Weighted hearing is bounded by the sixteen-point acoustic budget. */
function hears(w: BossWorld, origin: Cell, target: Cell): boolean {
  if (distance(origin, target) > B.noise.hearing) return false;
  const queue: { at: Cell; cost: number }[] = [{ at: cell(origin), cost: 0 }], costs = new Map([[key(origin), 0]]);
  while (queue.length) {
    let best = 0; for (let n = 1; n < queue.length; n++) if (queue[n]!.cost < queue[best]!.cost) best = n;
    const current = queue.splice(best, 1)[0]!; if (current.cost !== costs.get(key(current.at))) continue;
    if (same(current.at, target)) return true;
    for (const at of neighbors(current.at)) {
      const cost = current.cost + (blocksShot(w, at) ? B.noise.wallCost : 1), id = key(at);
      if (cost > B.noise.hearing || cost >= (costs.get(id) ?? Infinity)) continue;
      costs.set(id, cost); queue.push({ at, cost });
    }
  }
  return false;
}
export function recordBossNoise(w: BossWorld, at: Cell): void {
  if (!inside(at)) return;
  for (const e of w.bosses.encounters) { const r = body(w, e.owner); if (e.kind === 'bellkeeper' && r && hears(w, r, at)) e.noise = { cell: cell(at), at: w.tick }; }
}
function effect(w: BossWorld, e: BossEncounter, kind: BossEffect['kind'], origin: Cell, cells: Cell[], delay: number, damage: number, extra: Partial<Pick<BossEffect, 'feature' | 'channel' | 'group' | 'source'>> = {}): BossEffect | null {
  if (!cells.length || w.bosses.effects.length >= B.maxEffects) return null;
  const value: BossEffect = { id: w.nextId++, owner: e.owner, kind, source: e.owner, origin: cell(origin), cells: copyCells(cells), due: w.tick + delay, damage, feature: null, channel: null, group: null, ...extra };
  w.bosses.effects.push(value); sortEffects(w); return value;
}
function ringCells(w: BossWorld, origin: Cell, radius: number): Cell[] {
  const cells: Cell[] = [];
  for (let y = origin.y - radius; y <= origin.y + radius; y++) for (let x = origin.x - radius; x <= origin.x + radius; x++) {
    const at = { x, y }; if (Math.max(Math.abs(x - origin.x), Math.abs(y - origin.y)) === radius && inside(at) && !blocksShot(w, at)) cells.push(at);
  }
  return cells;
}
function lineClear(w: BossWorld, origin: Cell, target: Cell): boolean {
  const cells = shotCells(w, origin, target); return !!cells.length && same(cells[cells.length - 1]!, target) && !blocksShot(w, target);
}
function nearest(w: BossWorld, r: Raider): Person | undefined {
  return w.people.filter(p => p.life === 'alive' && distance(r, p) <= B.profiles[r.kind as BossKind].notice).sort((a, b) => distance(r, a) - distance(r, b) || a.id - b.id)[0];
}
function stopFeast(w: BossWorld, e: BossEncounter): void {
  e.feast = null; const r = body(w, e.owner); if (r) { r.route = []; r.retryAt = Math.min(r.retryAt, w.tick); }
}
function wetRoute(w: BossWorld, e: BossEncounter, r: Raider, channelIndex: number): Cell[] | null {
  if (e.drained[channelIndex]) return null;
  const channel = e.layout.channels[channelIndex]; if (!channel) return null;
  const cells = e.layout.channels.flatMap((c, n) => e.drained[n] ? [] : c.cells), ids = new Set(cells.filter(c => walkable(w, c)).map(key));
  if (!ids.has(key(r)) || !ids.has(key(channel.pool))) return null;
  const seen = new Set([key(r)]), queue = [cell(r)];
  for (let cursor = 0; cursor < queue.length; cursor++) for (const next of neighbors(queue[cursor]!)) if (ids.has(key(next)) && !seen.has(key(next))) { seen.add(key(next)); queue.push(next); }
  return seen.has(key(channel.pool)) ? cells.filter(c => ids.has(key(c))) : null;
}
function bellTurn(w: BossWorld, e: BossEncounter, r: Raider): boolean {
  if (e.moves % 2) {
    const resonators = w.bosses.features.filter(f => f.owner === r.id && f.kind === 'resonator'); if (!resonators.length) return false;
    for (const resonator of resonators) for (let radius = 1; radius <= B.ring.radii; radius++) effect(w, e, 'ring', resonator, ringCells(w, resonator, radius), B.ring.windup + (radius - 1) * B.ring.gap, B.ring.damage, { feature: resonator.id });
    w.bosses.status = 'Bellkeeper resonators announce three fixed rings; break a resonator to cancel its pulses';
  } else {
    const target = e.noise?.cell ?? nearest(w, r) ?? (distance(r, w.heart) <= B.profiles.bellkeeper.notice ? w.heart : null); if (!target) return false;
    effect(w, e, 'soundseek', r, [target], B.soundseek.windup, B.soundseek.damage);
    w.bosses.status = `Bellkeeper locks sound at ${target.x},${target.y}; move out before the pulse`;
  }
  return true;
}
function sluiceTurn(w: BossWorld, e: BossEncounter, r: Raider): boolean {
  const available = e.layout.channels.map((_, n) => n).filter(n => !e.drained[n]); if (!available.length) return false;
  const selected = available[Math.floor(e.moves / 2) % available.length]!, channel = e.layout.channels[selected]!;
  if (e.moves % 2) { effect(w, e, 'feast', r, [channel.pool], B.feast.windup, 0, { channel: selected }); w.bosses.status = 'Sluice-Maw announces a pool feast; drain its valve before arrival to deny healing'; }
  else { effect(w, e, 'surge', r, channel.cells, B.surge.windup, B.surge.damage, { channel: selected }); w.bosses.status = 'Sluice channel surge announced; leave its saved channel or drain the adjacent valve'; }
  return true;
}
function threadTurn(w: BossWorld, e: BossEncounter, r: Raider): boolean {
  if (e.moves % 2 === 0 && !w.bosses.features.some(f => f.owner === r.id && (f.kind === 'anchor' || f.kind === 'web'))) {
    const value = effect(w, e, 'partition', r, [...e.layout.anchors, ...e.layout.webs], B.partition.windup, 0); if (value) value.group = value.id;
    w.bosses.status = 'Threadmother announces a partition with a central gap; occupied colony ground stays protected'; return !!value;
  }
  const targets = w.people.filter(p => p.life !== 'dead' && distance(r, p) > 1 && distance(r, p) <= B.reel.range && lineClear(w, r, p));
  targets.sort((a, b) => Number(a.life !== 'downed') - Number(b.life !== 'downed') || a.body.health - b.body.health || distance(r, a) - distance(r, b) || a.id - b.id);
  const target = targets[0]; if (!target) return false;
  const line = shotCells(w, r, target).slice(0, -1);
  if (w.bosses.features.length + line.length > B.maxFeatures || line.some(at => bossFeatureAt(w, at))) return false;
  const value = effect(w, e, 'reel', r, [target], B.reel.windup, B.reel.damage); if (!value) return false; value.group = value.id;
  for (const at of line) feature(w, r.id, 'line', at, value.id);
  w.bosses.status = 'Threadmother locks a reel line; move off its marked cell or cut any line segment'; return true;
}
/** Core retains physical motion, shared route search and final collision checks. */
export function bossTurn(w: BossWorld, r: Raider, api: BossApi): boolean {
  if (!bossKind(r.kind)) return false;
  const e = encounter(w, r.id); if (!e || e.deadAt !== null) return false;
  if (e.noise && e.noise.at + B.noise.lifetime < w.tick) e.noise = null;
  if (e.feast) {
    const channel = e.layout.channels[e.feast.channel]!, allowed = wetRoute(w, e, r, e.feast.channel);
    if (!allowed || e.feast.expiresAt <= w.tick) { stopFeast(w, e); return true; }
    if (same(r, channel.pool)) { r.health = Math.min(B.profiles.sluice.health, r.health + B.feast.heal); stopFeast(w, e); w.bosses.status = 'Sluice-Maw reached its wet pool and healed'; }
    else { r.target = null; api.move(r, channel.pool, false, allowed); }
    return true;
  }
  if (w.bosses.effects.some(a => a.owner === r.id && a.kind !== 'fire') || e.readyAt > w.tick) return true;
  if (r.kind === 'sluice' && e.drained.every(Boolean)) return false;
  if (!e.arrived) {
    if (!same(r, e.layout.center)) { r.target = null; api.move(r, e.layout.center, false); return true; }
    e.arrived = true;
  }
  if (r.kind === 'sluice' && !e.layout.channels.some((channel, n) => !e.drained[n] && channel.cells.some(at => same(at, r)))) { r.target = null; api.move(r, e.layout.center, false); return true; }
  const acted = r.kind === 'bellkeeper' ? bellTurn(w, e, r) : r.kind === 'sluice' ? sluiceTurn(w, e, r) : threadTurn(w, e, r);
  if (acted) { e.moves++; e.readyAt = w.tick + B.profiles[r.kind].cooldown; r.route = []; r.target = null; return true; }
  return false;
}
function removeFeatures(w: BossWorld, removed: BossFeature[]): void {
  if (!removed.length) return;
  const ids = new Set(removed.map(f => f.id));
  w.bosses.features = w.bosses.features.filter(f => !ids.has(f.id)); w.bosses.work = w.bosses.work.filter(work => !ids.has(work.target));
  if (removed.some(f => f.kind === 'anchor' || f.kind === 'resonator')) w.terrainVersion++;
  for (const r of w.combat.raiders) if (r.target?.kind === 'feature' && ids.has(r.target.id)) { r.target = null; r.route = []; }
}
function ignite(w: BossWorld, start: BossFeature, source: number): void {
  const e = encounter(w, start.owner); if (!e || e.deadAt !== null) return;
  const candidates = w.bosses.features.filter(f => f.owner === start.owner && (f.kind === 'web' || f.kind === 'anchor'));
  const queue = [{ feature: start, depth: 0 }], seen = new Set([start.id]);
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const current = queue[cursor]!;
    if (!w.bosses.effects.some(a => a.kind === 'fire' && a.feature === current.feature.id)) {
      const cells = [cell(current.feature), ...neighbors(current.feature).filter(at => !blocksShot(w, at))];
      effect(w, e, 'fire', current.feature, cells, Math.min(B.fire.maxDelay, B.fire.windup + current.depth * B.fire.gap), B.fire.damage, { source, feature: current.feature.id, group: current.feature.group });
    }
    for (const next of candidates) if (!seen.has(next.id) && distance(current.feature, next) === 1) { seen.add(next.id); queue.push({ feature: next, depth: current.depth + 1 }); }
  }
  w.bosses.status = 'Ignited connected web; locked fire pulses burn either faction even if their caster dies';
}
export function damageBossFeature(w: BossWorld, id: number, amount: number, source: number, fire = false): void {
  const f = w.bosses.features.find(f => f.id === id); if (!f || f.kind === 'valve') return;
  if (fire && (f.kind === 'web' || f.kind === 'anchor')) ignite(w, f, source);
  f.health = Math.max(0, f.health - amount); if (f.health) return;
  const e = encounter(w, f.owner);
  if (f.kind === 'resonator') { w.bosses.effects = w.bosses.effects.filter(a => a.kind !== 'ring' || a.feature !== f.id); if (e) expose(w, e); removeFeatures(w, [f]); }
  else if (f.kind === 'line') { w.bosses.effects = w.bosses.effects.filter(a => a.kind !== 'reel' || a.group !== f.group); removeFeatures(w, w.bosses.features.filter(other => other.kind === 'line' && other.group === f.group)); }
  else if (f.kind === 'anchor') removeFeatures(w, w.bosses.features.filter(other => other.owner === f.owner && other.group === f.group && (other.kind === 'anchor' || other.kind === 'web')));
  else removeFeatures(w, [f]);
}
export function bossCounterReason(w: BossWorld, mode: BossCounterMode, target: Cell): string | null {
  const f = bossFeatureAt(w, target); if (!f) return 'Choose a live environmental counter';
  if (mode === 'strike') return f.kind === 'resonator' || f.kind === 'anchor' ? null : 'Strike a resonator or web anchor';
  if (mode === 'drain') return f.kind === 'valve' && f.channel !== null && !encounter(w, f.owner)?.drained[f.channel] ? null : 'Choose an undrained valve';
  if (mode === 'cut') return ['anchor', 'web', 'line'].includes(f.kind) ? null : 'Cut a web, anchor or announced reel line';
  return ['anchor', 'web'].includes(f.kind) && !w.bosses.effects.some(a => a.kind === 'fire' && a.feature === f.id) ? null : 'Ignite an unlit web or anchor';
}
function drain(w: BossWorld, f: BossFeature): void {
  const e = encounter(w, f.owner); if (!e || f.channel === null || e.drained[f.channel]) return;
  e.drained[f.channel] = true; w.bosses.effects = w.bosses.effects.filter(a => a.owner !== f.owner || a.channel !== f.channel);
  if (e.feast?.channel === f.channel) stopFeast(w, e); expose(w, e);
  w.bosses.status = `Channel ${f.channel + 1} drained permanently; its surge and feast are cancelled`;
}
export function prepareBossCounters(w: BossWorld, api: BossApi): void {
  w.bosses.work = w.bosses.work.filter(work => w.combat.orders.some(o => o.actor === work.actor && o.mode === work.mode && w.bosses.features.some(f => f.id === work.target && same(f, o.target))));
  for (const order of [...w.combat.orders].sort((a, b) => a.actor - b.actor)) {
    if (!counterMode(order.mode)) continue;
    const person = w.people.find(p => p.id === order.actor), f = bossFeatureAt(w, order.target);
    const clear = (): void => { w.bosses.work = w.bosses.work.filter(work => work.actor !== order.actor); };
    if (!person || person.life !== 'alive' || !f || bossCounterReason(w, order.mode, order.target)) { clear(); api.release(order.actor); continue; }
    if (person.body.routine && person.body.routine.kind !== 'watch' || person.body.recoverAt > w.tick) { clear(); continue; }
    if (person.next || distance(person, f) > 1) { clear(); api.approach(person, f); continue; }
    let work = w.bosses.work.find(work => work.actor === person.id);
    if (!work) { work = { actor: person.id, mode: order.mode, target: f.id, startedAt: w.tick, progress: 0 }; w.bosses.work.push(work); }
    work.progress++; recordBossNoise(w, person); person.body.status = `${order.mode} ${f.kind}: ${work.progress}/${B.counters[order.mode]}`;
    if (work.progress < B.counters[order.mode]) continue;
    clear();
    if (order.mode === 'drain') drain(w, f);
    else damageBossFeature(w, f.id, order.mode === 'ignite' ? 0 : B.counterDamage, person.id, order.mode === 'ignite');
    if (order.mode === 'drain' || order.mode === 'ignite' || !w.bosses.features.some(other => other.id === f.id)) api.release(person.id);
  }
}
function hurtCells(w: BossWorld, value: BossEffect, api: BossApi, neutral = false): void {
  const cells = new Set(value.cells.map(key)), targets: CombatRef[] = [];
  for (const p of w.people) if (p.life === 'alive' && cells.has(key(p))) targets.push({ kind: 'person', id: p.id });
  if (neutral) for (const r of w.combat.raiders) if (cells.has(key(r))) targets.push({ kind: 'raider', id: r.id });
  for (const s of w.structures) if (cells.has(key(s))) targets.push({ kind: 'structure', id: s.id });
  if (cells.has(key(w.heart))) targets.push({ kind: 'heart', id: w.heart.id });
  for (const target of targets) api.damage(target, value.damage, value.origin);
}
function resolve(w: BossWorld, value: BossEffect, api: BossApi): void {
  const e = encounter(w, value.owner), r = body(w, value.owner); if (!e || value.kind !== 'fire' && !r) return;
  if (value.kind === 'soundseek' || value.kind === 'ring') hurtCells(w, value, api);
  else if (value.kind === 'surge') {
    if (value.channel === null || e.drained[value.channel]) return;
    const people = w.people.filter(p => p.life === 'alive' && value.cells.some(at => same(at, p))).map(p => p.id);
    const raiders = w.combat.raiders.filter(r => value.cells.some(at => same(at, r))).map(r => r.id);
    hurtCells(w, value, api, true); const flow = e.layout.channels[value.channel]!.flow;
    for (const id of people) api.shove(id, flow); for (const id of raiders) api.shoveRaider(id, flow);
  } else if (value.kind === 'feast') {
    if (value.channel !== null && !e.drained[value.channel] && wetRoute(w, e, r!, value.channel)) {
      const pool = e.layout.channels[value.channel]!.pool, occupant = spatial(w).get(key(pool));
      if (occupant === undefined || occupant === r!.id) { e.feast = { channel: value.channel, expiresAt: w.tick + B.feast.travelTicks }; r!.route = []; r!.retryAt = Math.min(r!.retryAt, w.tick); }
    }
  } else if (value.kind === 'partition') {
    if (w.bosses.features.some(f => f.owner === value.owner && (f.kind === 'anchor' || f.kind === 'web'))) return;
    for (const [kind, cells] of [['anchor', e.layout.anchors], ['web', e.layout.webs]] as const) for (const at of cells) {
      if (w.bosses.features.length >= B.maxFeatures) break;
      if (bossFeaturePlacementAllowed(w, at, kind === 'anchor')) feature(w, e.owner, kind, at, value.group);
    }
  } else if (value.kind === 'reel') {
    const target = value.cells[0]!, person = w.people.find(p => p.life !== 'dead' && same(p, target)), lines = w.bosses.features.filter(f => f.kind === 'line' && f.group === value.group);
    if (person && lines.length && lineClear(w, value.origin, target)) {
      if (person.life === 'downed') person.body.deathAt = Math.max(w.tick + 1, person.body.deathAt - B.reel.bleedTicks);
      else {
        api.damage({ kind: 'person', id: person.id }, value.damage, value.origin);
        const dx = value.origin.x - person.x, dy = value.origin.y - person.y;
        api.shove(person.id, Math.abs(dx) >= Math.abs(dy) && dx ? { x: Math.sign(dx), y: 0 } : { x: 0, y: Math.sign(dy) });
      }
    }
    removeFeatures(w, lines);
  } else if (value.kind === 'fire') {
    hurtCells(w, value, api, true);
    const f = w.bosses.features.find(f => f.id === value.feature); if (f) damageBossFeature(w, f.id, f.health, value.source);
  }
}
export function bossDied(w: BossWorld, owner: number): void {
  const e = encounter(w, owner); if (!e) return;
  e.deadAt = w.tick; e.noise = null; e.feast = null;
  w.bosses.effects = w.bosses.effects.filter(a => a.owner !== owner || a.kind === 'fire');
  removeFeatures(w, w.bosses.features.filter(f => f.owner === owner));
  if (!w.bosses.effects.some(a => a.owner === owner)) w.bosses.encounters = w.bosses.encounters.filter(other => other !== e);
}
export function advanceBossEffects(w: BossWorld, api: BossApi): void {
  for (const e of w.bosses.encounters) {
    if (e.noise && e.noise.at + B.noise.lifetime < w.tick) e.noise = null;
    // A reserved movement edge can outlive the deadline; retire the feast on
    // the public tick even while core movement is finishing that edge.
    if (e.feast && e.feast.expiresAt <= w.tick) stopFeast(w, e);
  }
  const due = w.bosses.effects.filter(a => a.due <= w.tick).sort((a, b) => a.due - b.due || a.id - b.id);
  for (const value of due) {
    if (!w.bosses.effects.some(a => a.id === value.id)) continue;
    w.bosses.effects = w.bosses.effects.filter(a => a.id !== value.id); resolve(w, value, api);
  }
  w.bosses.encounters = w.bosses.encounters.filter(e => e.deadAt === null || w.bosses.effects.some(a => a.owner === e.owner && a.kind === 'fire'));
}
