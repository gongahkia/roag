import { NEEDS, RECIPES, DUTIES } from './survival-content.ts';
import type { Duty } from './survival-content.ts';
import { endRoutine, fall, die } from './needs.ts';
import { active, distance, index, inside, neighbors, same, spatial, walkable } from './model.ts';
import type { Cell, CommandInput, Job, Person, Structure, World } from './model.ts';
import { BUILDINGS, emptyStock, MATERIALS, total, WORK_CONTENT as C } from './work-content.ts';
import type { Stock } from './work-content.ts';
export const jobCost = (j: Job): Stock => j.kind === 'build' ? { ...C.buildings[j.building!].cost }
  : j.kind === 'reload' ? { ...emptyStock(), ammo: j.building === 'trap' ? 1 : 4 }
  : j.kind === 'produce' ? { ...RECIPES[j.recipe!].cost } : j.kind === 'repair' ? { ...emptyStock(), wood: j.building === 'wall' ? 0 : 1, stone: j.building === 'wall' ? 1 : 0, food: 0 } : emptyStock();
export const jobTicks = (j: Job): number => j.kind === 'build' ? C.buildings[j.building!].ticks
  : j.kind === 'produce' ? RECIPES[j.recipe!].ticks : j.kind === 'farm' ? Math.max(NEEDS.plantingTicks,NEEDS.tendingTicks,NEEDS.harvestTicks) : j.kind === 'gather' ? C.extractionTicks : j.kind === 'repair' ? C.repairTicks : C.dismantleTicks;
export const isStore = (s: Structure): boolean => s.kind === 'stockpile' || s.kind === 'storage';
export const capacity = (s: Structure): number => s.kind === 'stockpile' ? C.stockpileCapacity : C.buildings[s.kind].capacity;
export function clearTravel(p: Person, keepYield=false): void {
  // Complete an already reserved edge; never snap a worker backwards or forwards.
  p.destination = p.next ? { ...p.next } : null; p.route = []; p.blocked = null; if(!keepYield)p.yieldTo = null; p.failures = 0;
}
export function drop(world: World, at: Cell, stock: Stock): void {
  if (!total(stock)) return;
  let pile = world.piles.find(b => same(b, at));
  if (!pile) { pile = { id: world.nextId++, x: at.x, y: at.y, inventory: emptyStock() }; world.piles.push(pile); }
  for (const k of MATERIALS) { pile.inventory[k] += stock[k]; stock[k] = 0; }
}
export function release(world: World, j: Job): void {
  const p = world.people.find(p => p.id === j.worker);
  if (p) { drop(world, p, p.cargo); p.job = null; clearTravel(p); }
  j.worker = null; j.reservation = null; j.delivery = null;
}
export function finish(world: World, j: Job, cancelled = false): void {
  release(world, j); drop(world, j, j.site); j.state = cancelled ? 'cancelled' : 'done';
  j.status = cancelled ? 'Cancelled; supplies remain in stores or recoverable bundles' : 'Complete';
  if (!cancelled) j.completions++;
  if (j.kind === 'build') world.terrainVersion++;
}
export function placementReason(world: World, target: Cell): string | null {
  if (!inside(target) || target.x < 1 || target.y < 1 || target.x > 126 || target.y > 126 || !walkable(world, target)) return 'Choose open ground away from the heart and structures';
  if (spatial(world).has(index(target.x, target.y))) return 'A worker occupies or has reserved this tile';
  if (world.resources.some(r => r.remaining>0 && same(r, target)) || world.piles.some(p => same(p, target) && total(p.inventory)) || world.structures.some(s => same(s, target))) return 'A source, bundle or structure occupies this tile';
  if (world.jobs.some(j => active(j) && same(j, target))) return 'Another job occupies this tile';
  return null;
}
export function workCommandReason(world: World, input: CommandInput): string | null {
  if (world.terminal) return 'The colony has ended';
  const pending = world.jobs.filter(active);
  switch (input.kind) {
    case 'duty': return !world.people.some(p=>p.id===input.actor&&p.life==='alive')||!DUTIES.includes(input.duty)||!Number.isInteger(input.value)||input.value<0||input.value>3?'Choose a living worker and duty priority 0–3':null;
    case 'produce': { const s=world.structures.find(s=>s.id===input.structure); return !s || (input.recipe==='farm'?s.kind!=='farm':!Object.hasOwn(RECIPES,input.recipe)||s.kind!==RECIPES[input.recipe].facility)?'Choose the matching production facility':pending.some(j=>j.ref===s.id)?'Facility already has work':pending.length>=128?'Work list full':null; }
    case 'gather': return !world.resources.some(r => r.id === input.source && r.remaining > 0) ? 'Choose a finite resource source'
      : pending.some(j => j.kind === 'gather' && j.ref === input.source) ? 'This source is already designated' : pending.length >= 128 ? 'Work list full' : null;
    case 'stockpile': case 'build': return input.kind === 'build' && !BUILDINGS.includes(input.building) ? 'Unknown building' : pending.length >= 128 || world.structures.length + pending.filter(j=>j.kind==='build').length >= 256 ? 'Colony structure/work limit reached' : placementReason(world, input.target);
    case 'reload': {
      const s=world.structures.find(s=>s.id===input.structure && (s.kind==='tower'||s.kind==='trap'));
      return !s ? 'Choose a tower or trap' : pending.some(j=>j.ref===s.id) ? 'Defense already has work; finish or cancel it first' : pending.length>=128 ? 'Work list full' : capacity(s)-s.inventory.ammo<(s.kind==='trap'?1:4) ? 'Magazine needs room for a full batch (tower 4, trap 1)' : null;
    }
    case 'repair': case 'dismantle': {
      const s = world.structures.find(s => s.id === input.structure);
      return !s ? 'Structure no longer exists' : input.kind === 'repair' && (s.kind === 'stockpile' || s.health === 100) ? 'Choose a damaged building'
        : pending.some(j => j.ref === s.id) ? 'Structure already has work' : pending.length >= 128 ? 'Work list full' : null;
    }
    case 'work': return !pending.some(j => j.id === input.job) || !['cancel', 'suspend', 'resume'].includes(input.action) ? 'Choose unfinished work' : null;
    case 'priority': return !pending.some(j => j.id === input.job) || !Number.isInteger(input.value) || input.value < 1 || input.value > 3 ? 'Choose unfinished work and priority 1–3' : null;
    case 'assign': return !pending.some(j => j.id === input.job) || input.worker !== null && !world.people.some(p => p.id === input.worker && p.life === 'alive') ? 'Choose unfinished work and a living worker' : null;
    case 'door': {
      const s = world.structures.find(s => s.id === input.structure && s.kind === 'door');
      return !s || typeof input.open !== 'boolean' ? 'Choose a door' : !input.open && spatial(world).has(index(s.x, s.y)) ? 'Doorway occupied or reserved' : null;
    }
    case 'dev': return world.scenario !== 'work-lab' ? 'Development controls require the work lab'
      : ['injure','down','hungry','tired'].includes(input.action) ? world.people.some(p=>p.id===input.id&&p.life==='alive')?null:'Choose a living worker'
      : input.action === 'kill' ? world.people.some(p => p.id === input.id && p.life !== 'dead') ? null : 'Unknown living worker'
      : input.action === 'damage' ? world.structures.some(s => s.id === input.id && s.kind !== 'stockpile') ? null : 'Unknown building'
      : input.action === 'deplete' && world.resources.some(r => r.id === input.id) ? null : 'Unknown development action';
    default: return 'Unknown work command';
  }
}
export function addJob(world: World, kind: Job['kind'], at: Cell, ref: number | null, building: Job['building']): Job {
  // Retain a bounded history; IDs are never reused and active claims are never pruned.
  if (world.jobs.length >= 256) world.jobs = world.jobs.filter(active).concat(world.jobs.filter(j => !active(j)).slice(-127)).sort((a,b) => a.id-b.id);
  const j: Job = { id: world.nextId++, kind, x: at.x, y: at.y, ref, building, state: 'active', priority: kind === 'haul' ? 1 : 2,
    worker: null, requested: null, progress: 0, site: emptyStock(), reservation: null, delivery: null, recipe:null,stage:0,readyAt:0,cycles:0,status: 'Waiting for worker', completions: 0, retryAt: 0 };
  world.jobs.push(j); return j;
}
export function applyWorkCommand(world: World, input: CommandInput): void {
  if (workCommandReason(world, input)) return;
  switch (input.kind) {
    case 'duty': { const p=world.people.find(p=>p.id===input.actor)!; p.body.duties[input.duty]=input.value; if(p.job){const j=world.jobs.find(j=>j.id===p.job)!;const current=p.body.duties[jobDuty(j)];if(current===0||input.value>current)release(world,j);}if(p.body.routine?.kind==='rescue'&&p.body.duties.healer===0||p.body.routine?.kind==='watch')endRoutine(world,p);break; }
    case 'produce': { const s=world.structures.find(s=>s.id===input.structure)!; const j=addJob(world,input.recipe==='farm'?'farm':'produce',s,s.id,null);j.recipe=input.recipe==='farm'?null:input.recipe;break; }
    case 'gather': { const r = world.resources.find(r => r.id === input.source)!; addJob(world, 'gather', r, r.id, null); break; }
    case 'stockpile': world.structures.push({ id: world.nextId++, ...input.target, kind: 'stockpile', health: 100, open: true, inventory: emptyStock(), embodied: emptyStock() }); break;
    case 'build': addJob(world, 'build', input.target, null, input.building); world.terrainVersion++; break;
    case 'reload': { const s=world.structures.find(s=>s.id===input.structure)!; addJob(world,'reload',s,s.id,s.kind as 'tower'|'trap'); break; }
    case 'repair': case 'dismantle': { const s = world.structures.find(s => s.id === input.structure)!; addJob(world, input.kind, s, s.id, s.kind === 'stockpile' ? null : s.kind); break; }
    case 'work': {
      const j = world.jobs.find(j => j.id === input.job)!;
      if (input.action === 'cancel') finish(world, j, true);
      else if (input.action === 'suspend') { release(world, j); j.state = 'suspended'; j.status = 'Suspended; site supplies retained'; }
      else { j.state = 'active'; j.retryAt = world.tick; j.status = 'Resumed'; }
      break;
    }
    case 'priority': world.jobs.find(j => j.id === input.job)!.priority = input.value; break;
    case 'assign': {
      const j = world.jobs.find(j => j.id === input.job)!; release(world, j);
      const p = world.people.find(p => p.id === input.worker);
      if (p?.job) { const previous = world.jobs.find(j => j.id === p.job)!; release(world, previous); previous.requested = null; }
      if (p) clearTravel(p);
      j.requested = input.worker; j.retryAt = world.tick; j.status = 'Worker assignment changed'; break;
    }
    case 'door': { const s = world.structures.find(s => s.id === input.structure)!; if (s.open !== input.open) { s.open = input.open; world.terrainVersion++; } break; }
    case 'dev': {
      if (input.action === 'damage') world.structures.find(s => s.id === input.id)!.health = 50;
      if (input.action === 'kill') die(world,world.people.find(p=>p.id===input.id)!);
      if (['injure','down','hungry','tired'].includes(input.action)) { const p=world.people.find(p=>p.id===input.id)!;if(input.action==='injure')p.body.health=30; if(input.action==='down')fall(world,p);if(input.action==='hungry')p.body.hunger=NEEDS.mealAt; if(input.action==='tired')p.body.fatigue=NEEDS.restAt; }
      if (input.action === 'deplete') { const r = world.resources.find(r => r.id === input.id)!; world.sourceRemoved[r.kind] += r.remaining; r.remaining = 0; if (r.kind !== 'food') { world.terrain[index(r.x,r.y)] = 1; world.terrainVersion++; } }
      break;
    }
  }
}
export function inventory(world: World, id: number): Stock | undefined { return world.structures.find(s => s.id === id)?.inventory ?? world.piles.find(p => p.id === id)?.inventory; }
export function available(world: World, id: number, kind: keyof Stock): number {
  return (inventory(world,id)?.[kind] ?? 0) - world.jobs.reduce((n,j) => n + (j.reservation?.source === id && j.reservation.kind === kind ? j.reservation.count : 0), 0) - world.people.reduce((n,p)=>n+(p.body.routine?.reservation?.source===id&&p.body.routine.reservation.kind===kind?p.body.routine.reservation.count:0),0);
}
export function approach(world: World, j: {status:string}, p: Person, at: Cell, activity: string): boolean {
  if (!p.next && !p.yieldTo && world.tick >= p.yieldUntil && distance(p, at) <= 1) { clearTravel(p); return true; }
  // Keep an interaction goal stable while traversing a reserved edge. Recomputing
  // the nearest neighbor every tick can turn a walk into an endless two-tile oscillation.
  if (p.destination && distance(p.destination,at)<=1 && walkable(world,p.destination) && !(p.blocked && p.failures>=2 && p.retryAt<=world.tick)) {
    j.status = p.blocked ? `${activity}: ${p.blocked}` : activity; return false;
  }
  const occupied=spatial(world);
  const all=[...neighbors(at),...(walkable(world,at)?[{x:at.x,y:at.y}]:[])].filter(pos=>walkable(world,pos));
  const free=all.filter(pos=>!occupied.has(index(pos.x,pos.y))||occupied.get(index(pos.x,pos.y))===p.id);
  const candidates = (free.length?free:all).sort((a,b) => distance(p,a)-distance(p,b) || a.y-b.y || a.x-b.x);
  if (!candidates.length) { j.status = 'No interaction tile; open access or cancel'; return false; }
  // Switch approach after failed searches; terrain-version changes invalidate the route immediately.
  const goal = candidates[Math.floor(p.failures/2) % candidates.length]!;
  if (!p.destination || !same(p.destination,goal)) { p.destination = {...goal}; p.route = []; p.retryAt = world.tick; }
  j.status = p.blocked ? `${activity}: ${p.blocked}` : activity;
  return false;
}
export function deliver(world: World, j: Job, p: Person): boolean {
  if (!total(p.cargo)) return true;
  const stores = world.structures.filter(s => isStore(s) && capacity(s) > total(s.inventory)).sort((a,b) => distance(p,a)-distance(p,b) || a.id-b.id);
  let s = stores.find(s => s.id === j.delivery);
  if (!s) { s = stores[0]; j.delivery = s?.id ?? null; }
  if (!s) { clearTravel(p,true); j.status = 'Storage full or absent; cargo retained. Establish storage or cancel to drop a bundle'; return false; }
  if (!approach(world,j,p,s,'Hauling output to storage')) return false;
  let room = capacity(s) - total(s.inventory);
  for (const k of MATERIALS) { const n = Math.min(room,p.cargo[k]); s.inventory[k] += n; p.cargo[k] -= n; room -= n; }
  j.status = 'Delivered physical cargo'; j.delivery = null; return !total(p.cargo);
}
function supply(world: World, j: Job, p: Person): boolean {
  const cost = jobCost(j);
  if (total(p.cargo)) {
    if (!approach(world,j,p,j,'Hauling supplies to site')) return false;
    for (const k of MATERIALS) { j.site[k] += p.cargo[k]; p.cargo[k] = 0; }
    j.status = 'Supplies delivered to site'; return false;
  }
  if (MATERIALS.every(k => j.site[k] === cost[k])) return true;
  if (j.reservation && (!inventory(world,j.reservation.source) || available(world,j.reservation.source,j.reservation.kind) < 0)) j.reservation = null;
  if (!j.reservation) {
    const sources = [...world.structures.filter(isStore),...world.piles].sort((a,b) => distance(p,a)-distance(p,b) || a.id-b.id);
    for (const k of MATERIALS) {
      const missing = cost[k] - j.site[k]; if (missing <= 0) continue;
      const source = sources.find(s => available(world,s.id,k)>0);
      if (source) { j.reservation = { source: source.id, kind: k, count: Math.min(missing,C.cargoCapacity,available(world,source.id,k)) }; break; }
    }
    if (!j.reservation) { j.status = 'Missing ingredients; gather and deliver materials'; release(world,j); j.retryAt=world.tick+10; return false; }
  }
  const claim = j.reservation, source = world.structures.find(s => s.id === claim.source) ?? world.piles.find(s => s.id === claim.source)!;
  if (!approach(world,j,p,source,'Collecting reserved supplies')) return false;
  inventory(world,claim.source)![claim.kind] -= claim.count; p.cargo[claim.kind] += claim.count; j.reservation = null;
  j.status = 'Picked up reserved supplies'; return false;
}
function perform(world: World, j: Job, p: Person): void {
  if (j.kind === 'gather') {
    if (total(p.cargo)) { deliver(world,j,p); return; }
    const r = world.resources.find(r => r.id === j.ref);
    if (!r || !r.remaining) { finish(world,j); return; }
    if (!approach(world,j,p,r,'Approaching finite source')) return;
    j.status = 'Extracting'; if (++j.progress < C.extractionTicks) return;
    const n = Math.min(C.cargoCapacity,r.remaining); r.remaining -= n; p.cargo[r.kind] += n; j.progress = 0; j.status = `Extracted ${n} ${r.kind}; carrying`;
    if (!r.remaining && r.kind !== 'food') { world.terrain[index(r.x,r.y)] = 1; world.terrainVersion++; }
    return;
  }
  if (j.kind === 'haul') {
    if (total(p.cargo)) { if (deliver(world,j,p)) finish(world,j); return; }
    const pile = world.piles.find(b => b.id === j.ref);
    if (!pile || !total(pile.inventory)) { finish(world,j); return; }
    if (!j.reservation) { const k = MATERIALS.find(k => available(world,pile.id,k)>0); if (!k) { j.status = 'Bundle reserved by another job'; return; } j.reservation = {source:pile.id,kind:k,count:Math.min(C.cargoCapacity,available(world,pile.id,k))}; }
    if (!approach(world,j,p,pile,'Recovering material bundle')) return;
    const claim = j.reservation; pile.inventory[claim.kind] -= claim.count; p.cargo[claim.kind] += claim.count; j.reservation = null; j.status = 'Picked up recoverable material'; return;
  }
  if (j.kind==='farm'||j.kind==='produce') {
    const facility=world.structures.find(s=>s.id===j.ref);
    if(!facility){finish(world,j,true);return;}
    if(j.stage===3){if(total(p.cargo)&&!deliver(world,j,p))return;j.stage=0;j.progress=0;release(world,j);j.retryAt=world.tick+10;return;}
    const output=j.kind==='farm'?'food':j.recipe!, reserve=j.kind==='farm'?NEEDS.foodReserve:RECIPES[j.recipe!].reserve;
    const existing=[...world.structures.filter(isStore),...world.piles].reduce((n,s)=>n+s.inventory[output],0)+world.people.reduce((n,p)=>n+p.cargo[output],0);
    if(j.stage===0&&j.progress===0&&total(j.site)===0&&total(p.cargo)===0&&existing>=reserve){j.status=`Reserve met: ${existing}/${reserve} ${output}`;release(world,j);j.retryAt=world.tick+50;return;}
    if(j.kind==='farm') {
      if(j.stage===2&&world.tick<j.readyAt){j.status=`Growing; ready at tick ${j.readyAt}`;release(world,j);j.retryAt=j.readyAt;return;}
      if(!approach(world,j,p,j,'Approaching farm'))return;
      j.status=['Planting','Tending','Harvesting'][j.stage]!;
      const required=j.stage===0?NEEDS.plantingTicks:j.stage===1?NEEDS.tendingTicks:NEEDS.harvestTicks;
      if(++j.progress<required)return;j.progress=0;
      if(j.stage===0){j.stage=1;return;}if(j.stage===1){j.stage=2;j.readyAt=world.tick+NEEDS.growthTicks;return;}
      p.cargo.food+=NEEDS.harvestYield;world.produced.food+=NEEDS.harvestYield;world.statistics.harvests++;j.cycles++;j.stage=3;j.status='Harvested once; carrying food';return;
    }
    if(!supply(world,j,p)||!approach(world,j,p,j,'Approaching production facility'))return;
    j.status=`Producing ${j.recipe}`;if(++j.progress<jobTicks(j))return;
    const recipe=RECIPES[j.recipe!];for(const k of MATERIALS){world.consumed[k]+=j.site[k];j.site[k]=0;world.produced[k]+=recipe.output[k];p.cargo[k]+=recipe.output[k];}
    j.progress=0;j.stage=3;j.cycles++;world.statistics.recipes++;world.recipeCounts[j.recipe!]++;j.status='Produced once; carrying output';return;
  }
  const structure = world.structures.find(s => s.id === j.ref);
  if (j.kind !== 'build' && (!structure || !same(j,structure))) { finish(world,j,true); return; }
  if ((j.kind === 'build' || j.kind === 'repair' || j.kind === 'reload') && !supply(world,j,p)) return;
  if (!approach(world,j,p,j,j.kind === 'build' ? 'Approaching construction site' : 'Approaching structure')) return;
  if (j.kind === 'dismantle' && structure) {
    const occupied=spatial(world),id=occupied.get(index(structure.x,structure.y)),occupant=world.people.find(p=>p.id===id);
    if(occupant) {
      if(occupant.life==='alive'&&!occupant.next&&!occupant.yieldTo) {
        const exit=neighbors(structure).find(c=>walkable(world,c)&&!occupied.has(index(c.x,c.y)));
        if(exit){occupant.yieldTo=exit;occupant.yieldUntil=world.tick+16;occupant.route=[];occupant.retryAt=world.tick;}
      }
      j.status='Footprint occupied; yielding before dismantling';return;
    }
  }
  j.status = j.kind === 'build' ? 'Building' : j.kind === 'repair' ? 'Repairing' : j.kind === 'reload' ? 'Reloading' : 'Dismantling';
  if (++j.progress < jobTicks(j)) return;
  if (j.kind === 'build') {
    world.structures.push({ id: world.nextId++, x:j.x,y:j.y,kind:j.building!,health:100,open:j.building==='door',inventory:emptyStock(),embodied:{...j.site} }); j.site = emptyStock();
  } else if(j.kind==='reload') {
    const n=Math.min(j.site.ammo,capacity(structure!)-structure!.inventory.ammo);
    structure!.inventory.ammo+=n;j.site.ammo-=n;
  } else if (j.kind === 'repair') { structure!.health = 100; for (const k of MATERIALS) { world.spent[k] += j.site[k]; j.site[k] = 0; } }
  else removeStructure(world,structure!,j.id);
  world.terrainVersion++; finish(world,j);
}
function removeStructure(world: World, s: Structure, completing: number | null = null): void {
  if (!world.structures.some(b => b.id === s.id)) return;
  world.structures = world.structures.filter(b => b.id !== s.id);
  drop(world,s,s.inventory);
  const salvage=emptyStock();
  for(const k of MATERIALS){salvage[k]=Math.floor(s.embodied[k]*C.salvagePercent/100);world.spent[k]+=s.embodied[k]-salvage[k];}
  drop(world,s,salvage);
  for(const j of world.jobs){
    if(j.reservation?.source===s.id)j.reservation=null;
    if(j.delivery===s.id)j.delivery=null;
    if(j.ref===s.id&&j.id!==completing&&active(j))finish(world,j,true);
  }
  for(const p of world.people){
    if(p.body.routine?.reservation?.source===s.id)p.body.routine.reservation=null;
    if(p.body.routine?.facility===s.id)endRoutine(world,p);
  }
  world.combat.defenses=world.combat.defenses.filter(d=>d.structure!==s.id);
  world.combat.attacks=world.combat.attacks.filter(a=>a.source.kind!=='structure'||a.source.id!==s.id);
  world.terrainVersion++;
}
export function destroyStructure(world: World, s: Structure): void {
  if(!world.structures.some(b=>b.id===s.id))return;
  removeStructure(world,s);world.combat.stats.structuresLost++;
}
export const jobDuty=(j:Job):Duty=>j.kind==='gather'||j.kind==='haul'||j.kind==='farm'?'gatherer':j.kind==='produce'?RECIPES[j.recipe!].duty:'builder';
export function advanceJobs(world: World): void {
  for(const j of world.jobs)if(active(j)&&j.requested!==null&&world.people.find(p=>p.id===j.requested)?.life!=='alive')j.requested=null;
  for (const j of world.jobs) if (j.worker !== null && world.people.find(p => p.id === j.worker)?.life !== 'alive') { release(world,j); j.requested = null; }
  for (const pile of world.piles) if (total(pile.inventory) && !world.jobs.some(j => active(j) && j.kind === 'haul' && j.ref === pile.id) && world.jobs.filter(active).length<128) addJob(world,'haul',pile,pile.id,null);
  const used = new Set<number>();
  for (const j of [...world.jobs].sort((a,b) => b.priority-a.priority || a.id-b.id)) {
    if (j.state !== 'active' || j.retryAt>world.tick) continue;
    if (!j.worker) {
      const p = [...world.people].sort((a,b)=>b.body.duties[jobDuty(j)]-a.body.duties[jobDuty(j)]||a.id-b.id).find(p => p.life === 'alive' && !world.combat.orders.some(o=>o.actor===p.id) && !p.body.routine && p.body.recoverAt<=world.tick && p.body.duties[jobDuty(j)]>0 && p.body.duties[jobDuty(j)]>=p.body.duties.guard && p.job === null && !p.destination && !p.next && (j.requested === null || j.requested === p.id) && (j.requested!==null || !world.jobs.some(other=>other!==j&&other.state==='active'&&other.worker===null&&other.retryAt<=world.tick&&other.priority>=j.priority&&(other.requested===null||other.requested===p.id)&&p.body.duties[jobDuty(other)]>p.body.duties[jobDuty(j)])));
      if (!p) { j.status = j.requested ? 'Assigned worker unavailable' : 'Waiting for free worker with enabled duty'; continue; }
      j.worker = p.id; p.job = j.id;
    }
    const p = world.people.find(p => p.id === j.worker)!;
    if (used.has(p.id)||world.combat.orders.some(o=>o.actor===p.id)||p.body.routine||p.body.recoverAt>world.tick) { if(p.body.routine)j.status=`Interrupted: ${p.body.routine.status}`;continue; } used.add(p.id);
    if((p.body.hunger>=NEEDS.mealAt||p.body.fatigue>=NEEDS.restAt||p.body.health<70)&&world.tick%2){j.status='Needs or injury slow work (50%)';continue;}world.statistics.workTicks++;perform(world,j,p);
  }
  // Empty piles have no remaining claims after pickups. Retain referenced empty piles until those jobs finish.
  world.piles = world.piles.filter(p => total(p.inventory)>0 || world.jobs.some(j => active(j) && (j.ref === p.id || j.reservation?.source === p.id)));
}
export function materialAudit(world: World): Record<keyof Stock, { depleted:number; removed:number; produced:number; generated:number; consumed:number; stored:number; carried:number; site:number; embodied:number; spent:number; lost:number; accounted:number; reserved:number }> {
  return Object.fromEntries(MATERIALS.map(k => {
    const depleted = world.resources.filter(r => r.kind===k).reduce((n,r) => n+r.initial-r.remaining,0), removed=world.sourceRemoved[k];
    const stored = [...world.structures,...world.piles].reduce((n,s) => n+s.inventory[k],0), carried=world.people.reduce((n,p) => n+p.cargo[k]+(p.body.routine?.cargo[k]??0),0), site=world.jobs.reduce((n,j) => n+j.site[k],0), embodied=world.structures.reduce((n,s) => n+s.embodied[k],0);
    return [k,{depleted,removed,produced:depleted-removed+world.produced[k],generated:world.produced[k],consumed:world.consumed[k],stored,carried,site,embodied,spent:world.spent[k],lost:world.lost[k],accounted:stored+carried+site+embodied+world.spent[k]+world.lost[k]+world.consumed[k],reserved:world.jobs.reduce((n,j) => n+(j.reservation?.kind===k?j.reservation.count:0),0)+world.people.reduce((n,p)=>n+(p.body.routine?.reservation?.kind===k?p.body.routine.reservation.count:0),0)}];
  })) as ReturnType<typeof materialAudit>;
}
