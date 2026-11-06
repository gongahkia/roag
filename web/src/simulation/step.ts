import { advanceNeeds, endRoutine } from './needs.ts';
import { index, inside, MOVE_TICKS, neighbors, same, spatial, walkable } from './model.ts';
import type { Cell, CommandInput, Person, World } from './model.ts';
import { applyWorkCommand, advanceJobs, clearTravel, release, workCommandReason } from './jobs.ts';
import { route } from './navigation.ts';
export { route } from './navigation.ts';
export function legalCommand(world: World, input: CommandInput): boolean {
  if (world.terminal || !input || typeof input !== 'object') return false;
  if (input.kind === 'move') return inside(input.target) && world.people.some(p => p.id === input.actor && p.life === 'alive');
  if (input.kind === 'edit') return world.scenario === 'routing-lab' && inside(input.target) && typeof input.solid === 'boolean' && !same(input.target,world.heart) && !world.resources.some(r => same(r,input.target)) && !world.people.some(p => same(p,input.target) || p.next && same(p.next,input.target));
  return workCommandReason(world,input) === null;
}
export function enqueue(world: World, input: CommandInput, due=world.tick+1): boolean {
  if (!Number.isSafeInteger(due) || due<=world.tick || due>1e9 || world.pending.length>=128 || world.nextSequence>=1e9 || !legalCommand(world,input)) return false;
  world.pending.push({sequence:world.nextSequence++,due,input:structuredClone(input)});world.pending.sort((a,b)=>a.due-b.due||a.sequence-b.sequence);return true;
}
export function terminalReason(world: World): World['terminal'] { return world.heart.health<=0 ? 'heart-destroyed' : world.people.every(p=>p.life==='dead') ? 'all-dead' : null; }
function yieldBlocker(world: World, requester: Person, occupied: Map<number,number>): void {
  if (!requester.destination) return;
  // One bounded static probe identifies a local blocker; at most one free adjacent sidestep.
  const probe = route(world,requester,requester.yieldTo??requester.destination,new Map(),requester.id,256);
  const next = probe.path[0]; if (!next) return;
  const blocker = world.people.find(p => p.id === occupied.get(index(next.x,next.y)));
  if (!blocker || blocker.next || blocker.yieldTo || blocker.life !== 'alive' || world.tick < blocker.yieldUntil) return;
  // A worker already holding the requested final tile keeps it; identical destinations wait visibly.
  if (same(blocker,requester.destination) && !blocker.job && !blocker.destination && !requester.job&&!requester.body.routine) return;
  const side = neighbors(blocker).find(c => walkable(world,c) && !occupied.has(index(c.x,c.y)) && !probe.path.slice(0,3).some(p=>same(p,c)));
  if (!side) return;
  blocker.yieldTo = side; blocker.yieldUntil = world.tick+16; blocker.route=[]; blocker.retryAt=world.tick; blocker.blocked='Yielding to traffic';
}
export function step(world: World): void {
  if (world.terminal) return;
  if (world.tick>=1e9) throw new Error('Simulation tick limit reached');
  world.tick++;
  const commands=world.pending.filter(c=>c.due===world.tick);world.pending=world.pending.filter(c=>c.due>world.tick);
  for (const c of commands) {
    if (!legalCommand(world,c.input)) continue;
    if (c.input.kind==='edit') { world.terrain[index(c.input.target.x,c.input.target.y)]=c.input.solid?2:1;world.terrainVersion++; }
    else if (c.input.kind==='move') {
      const input=c.input,p=world.people.find(p=>p.id===input.actor)!;
      if(p.job){const j=world.jobs.find(j=>j.id===p.job)!;release(world,j);j.requested=null;j.retryAt=world.tick+20;}
      if(p.body.routine?.kind==='watch')endRoutine(world,p);
      else if(p.body.routine){p.body.routine.resume={...input.target};continue;}
      clearTravel(p);p.destination={...input.target};p.retryAt=world.tick;
    } else applyWorkCommand(world,c.input);
  }
  advanceNeeds(world);
  advanceJobs(world);
  const occupied=spatial(world); let searches=0;
  for (const p of world.people) {
    if (p.life!=='alive') continue;
    if (p.next) {
      p.progress++;
      if (p.progress>=MOVE_TICKS) { occupied.delete(index(p.x,p.y));p.x=p.next.x;p.y=p.next.y;p.next=null;p.progress=0; }
      else continue;
    }
    if (p.yieldTo && same(p,p.yieldTo)) { p.yieldTo=null;p.route=[];p.retryAt=p.yieldUntil; }
    const goal=p.yieldTo??p.destination;
    if (!goal || !p.yieldTo && world.tick<p.yieldUntil) continue;
    if (same(p,goal)) { p.destination=null;p.route=[];p.blocked=null;p.failures=0;continue; }
    if (p.routeVersion!==world.terrainVersion) { p.route=[];p.retryAt=world.tick;p.failures=0; }
    if (!p.route.length) {
      if (p.retryAt>world.tick || searches>=2) continue;
      searches++;
      const result=route(world,p,goal,occupied,p.id);p.route=result.path;p.routeVersion=world.terrainVersion;p.blocked=result.reason;
      if (!p.route.length) { p.failures++;p.retryAt=world.tick+Math.min(100,10*p.failures);if(p.failures>=2)yieldBlocker(world,p,occupied); }
      else {p.failures=0;p.retryAt=world.tick+10;}
    }
    const next=p.route[0];if(!next)continue;
    const other=occupied.get(index(next.x,next.y));
    if (!walkable(world,next) || other!==undefined && other!==p.id) { p.route=[];p.failures++;p.retryAt=world.tick+10;p.blocked='Occupied; yielding';yieldBlocker(world,p,occupied);continue; }
    p.next=p.route.shift()!;p.progress=0;p.blocked=null;occupied.set(index(p.next.x,p.next.y),p.id);
  }
  // Removed targets can never become valid again: stable IDs are not reused.
  // Retire only these inert commands, keeping sequence counters and all live intent.
  world.pending=world.pending.filter(({input})=>{
    if(input.kind==='work'||input.kind==='priority'||input.kind==='assign')return world.jobs.some(j=>j.id===input.job);
    if(input.kind==='repair'||input.kind==='dismantle'||input.kind==='door'||input.kind==='produce')return world.structures.some(s=>s.id===input.structure);
    if(input.kind==='dev'&&input.action==='damage')return world.structures.some(s=>s.id===input.id);
    return true;
  });
  world.terminal=terminalReason(world);
}
export function renderedPosition(p: Person, alpha: number): Cell {
  const ratio=p.next?Math.min(1,(p.progress+Math.max(0,Math.min(1,alpha)))/MOVE_TICKS):0;
  return {x:p.x+(p.next?p.next.x-p.x:0)*ratio,y:p.y+(p.next?p.next.y-p.y:0)*ratio};
}
