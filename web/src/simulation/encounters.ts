import { active, distance, index, inside, same, spatial, walkable } from './model.ts';
import type { Cell, World } from './model.ts';
import type { Attack, CombatRef, Raider } from './combat-model.ts';
import { COMBAT_CONTENT } from './combat-content.ts';
import { ENCOUNTER_CONTENT as E } from './encounter-content.ts';
import { barricadeSite, chargeCells, encounterBody, facingToward, heartReachable, inFront, lineReaches, preservesHeartAccess, rampartCells } from './encounter-geometry.ts';

export type EncounterApi = {
  launch(source: CombatRef, origin: Cell, target: Cell, kind: Attack['kind'], faction: Attack['faction'], damage: number, delay: number, extra?: { cells?: Cell[]; recipient?: number }): boolean;
  damage(ref: CombatRef, amount: number, faction: Attack['faction'], origin: Cell): void;
  move(raider: Raider, target: Cell, adjacent: boolean): void;
  shove(personId: number, delta: Cell): boolean;
  breach(cell: Cell): boolean;
};
const cell = (c: Cell): Cell => ({ x: c.x, y: c.y });
const ref = (r: Raider): CombatRef => ({ kind: 'raider', id: r.id });
const maximum = (r: Raider): number => COMBAT_CONTENT[r.kind].health;
function objective(w: World, r: Raider, notice: number): { ref: CombatRef; cell: Cell } {
  const person = w.people.filter(p => p.life === 'alive' && distance(r, p) <= notice).sort((a, b) => distance(r, a) - distance(r, b) || a.id - b.id)[0];
  return person ? { ref: { kind: 'person', id: person.id }, cell: person } : { ref: { kind: 'heart', id: w.heart.id }, cell: w.heart };
}
function ownedWindup(w: World, r: Raider): boolean { return w.combat.attacks.some(a => a.source.kind === 'raider' && a.source.id === r.id); }
function shoot(w: World, r: Raider, target: { ref: CombatRef; cell: Cell }, api: EncounterApi): boolean {
  const d = distance(r, target.cell);
  if (d < E.archer.minRange || d > E.archer.range || (r.x !== target.cell.x && r.y !== target.cell.y) || !lineReaches(w, r, target.cell, target.ref)) return false;
  if (r.cooldown <= w.tick && api.launch(ref(r), r, target.cell, 'arrow', 'hostile', E.profiles.archer.damage, E.profiles.archer.windup)) r.cooldown = w.tick + E.profiles.archer.cooldown;
  return true;
}
function archerTurn(w: World, r: Raider, api: EncounterApi): boolean {
  const target = objective(w, r, E.profiles.archer.notice); r.target = target.ref; r.facing = facingToward(r, target.cell);
  if (shoot(w, r, target, api)) return true;
  const occupied = spatial(w), candidates: Cell[] = [];
  for (let range = E.archer.minRange; range <= E.archer.range; range++) for (const v of [{ x: 1, y: 0 }, { x: -1, y: 0 }, { x: 0, y: 1 }, { x: 0, y: -1 }]) {
    const candidate = { x: target.cell.x + v.x * range, y: target.cell.y + v.y * range };
    if (inside(candidate) && walkable(w, candidate) && (!occupied.has(index(candidate.x, candidate.y)) || same(candidate, r)) && lineReaches(w, candidate, target.cell, target.ref)) candidates.push(candidate);
  }
  candidates.sort((a, b) => distance(r, a) - distance(r, b) || distance(b, target.cell) - distance(a, target.cell) || index(a.x, a.y) - index(b.x, b.y));
  if (candidates[0]) { api.move(r, candidates[0], false); return true; }
  return false;
}
function healerTurn(w: World, r: Raider, api: EncounterApi): boolean {
  const allies = w.combat.raiders.filter(a => a.id !== r.id && a.health > 0 && a.squadId === r.squadId && a.health < maximum(a));
  allies.sort((a, b) => a.health * maximum(b) - b.health * maximum(a) || a.id - b.id);
  const target = allies.find(a => distance(r, a) <= E.heal.range && lineReaches(w, r, a, ref(a)));
  if (target) {
    r.target = ref(target); r.facing = facingToward(r, target);
    if (r.cooldown <= w.tick && api.launch(ref(r), r, target, 'heal', 'hostile', 0, E.heal.windup, { recipient: target.id })) r.cooldown = w.tick + E.heal.cooldown;
    return true;
  }
  if (allies[0]) { r.target = ref(allies[0]); api.move(r, allies[0], true); return true; }
  return false;
}
/** Returning true reserves this actor's turn, including waiting for a clear firing/healing lane. */
export function encounterTurn(w: World, r: Raider, api: EncounterApi): boolean {
  if (r.charge || ownedWindup(w, r)) return true;
  if (r.kind === 'archer') return archerTurn(w, r, api);
  if (r.kind === 'healer') return healerTurn(w, r, api);
  if (r.kind === 'rusher') {
    const leader = w.combat.raiders.find(a => a.id === r.leaderId && a.kind === 'healer' && a.health > 0 && a.squadId === r.squadId);
    if (leader && r.health < maximum(r) && !w.people.some(p => p.life === 'alive' && distance(r, p) <= 2)) {
      r.target = ref(leader);
      // Healing locks a cell for its full windup. Stay for care once nearby,
      // instead of alternating an attack step with a retreat step forever.
      if (distance(r, leader) > 2) api.move(r, leader, true);
      else r.route = [];
      return true;
    }
    return false;
  }
  if (r.kind !== 'shield' && r.kind !== 'captain') return false;
  const profile = E.profiles[r.kind], target = objective(w, r, profile.notice), direction = facingToward(r, target.cell);
  r.target = target.ref;
  if (r.kind === 'captain' && r.facing !== direction) r.exposedUntil = w.tick + E.captain.exposureTicks;
  r.facing = direction;
  if (r.kind === 'shield') {
    if (distance(r, target.cell) > 1) return false;
    if (r.cooldown <= w.tick && api.launch(ref(r), r, target.cell, 'melee', 'hostile', profile.damage, profile.windup)) {
      r.cooldown = w.tick + profile.cooldown; r.loweredUntil = w.tick + E.shield.loweredTicks;
    }
    return true;
  }
  if (distance(r, target.cell) > E.captain.specialRange) return false;
  if (r.cooldown > w.tick) return true;
  const charging = r.specialCount % 2 === 0, cells = charging ? chargeCells(r, direction) : rampartCells(r, direction);
  if (!cells.length) return false;
  if (charging) for (const at of cells) {
    const body = encounterBody(w, at);
    if (body && body.kind !== 'raider' || w.jobs.some(j => j.kind === 'build' && active(j) && same(j, at))) break;
    // A charge cannot break natural rock. Use the ordinary bounded route search
    // to get around it instead of repeating an ineffectual special forever.
    if (w.terrain[index(at.x, at.y)] === 2) return false;
  }
  const tuning = charging ? E.charge : E.rampart;
  if (api.launch(ref(r), r, cells[cells.length - 1]!, charging ? 'charge' : 'rampart', 'hostile', charging ? E.charge.damage : 0, tuning.windup, { cells })) {
    r.specialCount++; r.cooldown = w.tick + tuning.cooldown;
  }
  return true;
}

export function defendedDamage(w: World, r: Raider, amount: number, origin: Cell): number {
  if (r.kind === 'captain' && r.exposedUntil > w.tick) return amount + Math.floor(amount * E.captain.exposedPercent / 100);
  if ((r.kind === 'shield' && r.loweredUntil <= w.tick) || (r.kind === 'captain' && !r.charge && !ownedWindup(w, r))) {
    if (inFront(r, r.facing, origin)) return Math.floor(amount * (100 - (r.kind === 'captain' ? E.captain.reductionPercent : E.shield.reductionPercent)) / 100);
  }
  return amount;
}
function endCharge(w: World, r: Raider): void {
  r.charge = null; r.route = []; r.retryAt = w.tick; r.exposedUntil = w.tick + E.captain.exposureTicks;
}
export function advanceEncounters(w: World, api: EncounterApi): void {
  const expired = w.combat.barricades.filter(b => b.expiresAt <= w.tick);
  if (expired.length) {
    w.combat.barricades = w.combat.barricades.filter(b => b.expiresAt > w.tick); w.terrainVersion++;
    for (const r of w.combat.raiders) if (r.target?.kind === 'barricade' && expired.some(b => b.id === r.target!.id)) { r.target = null; r.route = []; }
  }
  for (const r of [...w.combat.raiders].sort((a, b) => a.id - b.id)) {
    const charge = r.charge; if (!charge) continue;
    const next = charge.cells[charge.cursor];
    if (!next || !inside(next) || distance(r, next) !== 1) { endCharge(w, r); continue; }
    if (api.breach(next)) { endCharge(w, r); continue; }
    const body = encounterBody(w, next), delta = { x: next.x - r.x, y: next.y - r.y };
    if (body) {
      if (body.kind === 'raider') { endCharge(w, r); continue; }
      if (!charge.hits.includes(body.id)) { charge.hits.push(body.id); api.damage(body, E.charge.damage, 'hostile', cell(r)); }
      if (body.kind !== 'person' || !api.shove(body.id, delta)) { endCharge(w, r); continue; }
    }
    if (!walkable(w, next) || w.combat.barricades.some(b => same(b, next)) || spatial(w).has(index(next.x, next.y))) { endCharge(w, r); continue; }
    r.x = next.x; r.y = next.y; charge.cursor++;
    if (charge.cursor >= charge.cells.length) endCharge(w, r);
  }
}
export function resolveEncounterImpact(w: World, attack: Attack, _api: EncounterApi): boolean {
  if (attack.kind !== 'heal' && attack.kind !== 'charge' && attack.kind !== 'rampart') return false;
  const owner = attack.source.kind === 'raider' ? w.combat.raiders.find(r => r.id === attack.source.id && r.health > 0) : undefined;
  if (!owner) return true;
  if (attack.kind === 'heal') {
    const patient = w.combat.raiders.find(r => r.id === attack.recipient && r.health > 0 && r.squadId === owner.squadId);
    if (patient && same(patient, attack.target) && distance(owner, patient) <= E.heal.range && lineReaches(w, owner, patient, ref(patient))) patient.health = Math.min(maximum(patient), patient.health + E.heal.amount);
  } else if (attack.kind === 'charge') {
    if (owner.kind === 'captain' && same(owner, attack.origin)) {
      owner.next = null; owner.progress = 0; owner.route = []; owner.charge = { cells: attack.cells.map(cell), cursor: 0, hits: [] };
    }
  } else if (owner.kind === 'captain') {
    for (const candidate of attack.cells) {
      if (w.combat.barricades.length >= E.rampart.maxBarricades) break;
      if (!barricadeSite(w, candidate, spatial(w)) || !preservesHeartAccess(w, candidate, heartReachable(w))) continue;
      w.combat.barricades.push({ id: w.nextId++, owner: owner.id, ...cell(candidate), health: E.rampart.health, expiresAt: w.tick + E.rampart.lifetime });
      w.terrainVersion++;
    }
  }
  return true;
}
