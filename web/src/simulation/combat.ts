import { active, distance, index, inside, MOVE_TICKS, neighbors, same, spatial, walkable } from './model.ts';
import type { Cell, CommandInput, Person, World } from './model.ts';
import type { Attack, BattleOrder, Combat, CombatRef, Raider } from './combat-model.ts';
import { COMBAT_CONTENT as C } from './combat-content.ts';
import { blocksShot, clearShot, combatBodyAt, shotCells } from './combat-geometry.ts';
import { breachable, raiderRoute } from './combat-navigation.ts';
import { approach, clearTravel, destroyStructure, drop, finish } from './jobs.ts';
import { endRoutine, fall } from './needs.ts';
import { roll } from './random.ts';
import { emptyStock } from './work-content.ts';

export function initialCombat(): Combat {
  return { phase:'peace', wave:0, warningAt:C.warningAt, spawnAt:C.spawnAt, completed:0, edge:'east', pending:[], raiders:[], attacks:[], orders:[], defenses:[], cooldowns:[], stats:{spawned:0,kills:0,shots:0,traps:0,heartDamage:0,colonyDamage:0,structuresLost:0}, lootWood:0,
    status:'Prepare food, shelter, medicine and defenses. The first raid is announced on day four.' };
}
export function combatCommandReason(w: World, input: CommandInput): string | null {
  if (w.terminal) return 'The colony has ended';
  if (input.kind !== 'battle') return 'Unknown battle command';
  if (!Number.isSafeInteger(input.actor) || input.actor !== 0 && !w.people.some(p => p.id === input.actor && p.life === 'alive')) return 'Choose a living colonist or all colonists';
  if (!['rally','retreat','shelter','release'].includes(input.mode) || !inside(input.target)) return 'Choose a battle order and a tile within the map';
  return input.mode !== 'release' && !walkable(w,input.target) ? 'Choose open ground for the rally or shelter destination' : null;
}
function releaseOrder(w: World, order: BattleOrder): void {
  const p=w.people.find(p=>p.id===order.actor);w.combat.orders=w.combat.orders.filter(o=>o.actor!==order.actor);
  if(!p||p.life!=='alive'||p.body.routine&&p.body.routine.kind!=='watch')return;
  clearTravel(p);if(!p.job&&order.resume)p.destination={...order.resume};p.body.status='Battle order released; resuming colony work';
}
function setOrder(w: World,p: Person,mode: BattleOrder['mode'],target: Cell): void {
  const old=w.combat.orders.find(o=>o.actor===p.id),resume=old?.resume??(!p.job?p.body.routine?.resume??p.destination:null);
  if(p.body.routine?.kind==='watch')endRoutine(w,p);if(!p.body.routine)clearTravel(p);
  const order:BattleOrder={actor:p.id,mode,target:{...target},resume:resume?{...resume}:null};
  if(old)w.combat.orders[w.combat.orders.indexOf(old)]=order;else w.combat.orders.push(order);
  w.combat.orders.sort((a,b)=>a.actor-b.actor);
}
export function applyCombatCommand(w: World,input: CommandInput): void {
  if(input.kind!=='battle'||combatCommandReason(w,input))return;
  const people=w.people.filter(p=>p.life==='alive'&&(input.actor===0||p.id===input.actor));
  for(const p of people){const old=w.combat.orders.find(o=>o.actor===p.id);if(input.mode==='release'){if(old)releaseOrder(w,old);}else setOrder(w,p,input.mode,input.target);}
  w.combat.status=input.mode==='release'?`Released ${people.length} colonists to colony work.`:`${people.length} colonists ordered to ${input.mode} at ${input.target.x},${input.target.y}; urgent care takes precedence.`;
}
function aliveSource(w: World,ref: CombatRef): boolean {
  if(ref.kind==='person')return w.people.some(p=>p.id===ref.id&&p.life==='alive');
  if(ref.kind==='raider')return w.combat.raiders.some(r=>r.id===ref.id&&r.health>0);
  if(ref.kind==='structure')return w.structures.some(s=>s.id===ref.id&&s.health>0);
  return ref.id===w.heart.id&&w.heart.health>0;
}
function damage(w: World,ref: CombatRef,amount: number,faction: Attack['faction']): void {
  if(ref.kind==='raider'){
    if(faction!=='colony')return;const r=w.combat.raiders.find(r=>r.id===ref.id);if(!r)return;r.health=Math.max(0,r.health-amount);
    if(!r.health){w.combat.raiders=w.combat.raiders.filter(other=>other.id!==r.id);w.combat.attacks=w.combat.attacks.filter(a=>a.source.kind!=='raider'||a.source.id!==r.id);w.combat.stats.kills++;w.combat.lootWood++;w.produced.wood++;const loot=emptyStock();loot.wood=1;drop(w,r,loot);}
    return;
  }
  // Friendly bodies intercept colony shots without taking damage.
  if(faction!=='hostile')return;
  if(ref.kind==='person'){
    const p=w.people.find(p=>p.id===ref.id&&p.life!=='dead');if(!p||p.life==='downed')return;
    const loss=Math.min(p.body.health,amount);p.body.health-=loss;w.combat.stats.colonyDamage+=loss;
    if(!p.body.health){fall(w,p);w.combat.attacks=w.combat.attacks.filter(a=>a.source.kind!=='person'||a.source.id!==p.id);}
  }else if(ref.kind==='heart'){
    const loss=Math.min(w.heart.health,amount);w.heart.health-=loss;w.combat.stats.heartDamage+=loss;
  }else{
    const s=w.structures.find(s=>s.id===ref.id);if(!s)return;s.health=Math.max(0,s.health-amount);
    if(!s.health)destroyStructure(w,s);
  }
}
export function resolveCombatImpacts(w: World): void {
  w.combat.attacks=w.combat.attacks.filter(a=>aliveSource(w,a.source));
  const due=w.combat.attacks.filter(a=>a.due<=w.tick).sort((a,b)=>a.due-b.due||a.id-b.id);
  for(const attack of due){
    if(!w.combat.attacks.some(a=>a.id===attack.id)||!aliveSource(w,attack.source))continue;
    w.combat.attacks=w.combat.attacks.filter(a=>a.id!==attack.id);
    const cells=attack.kind==='bolt'?shotCells(w,attack.origin,attack.target):attack.cells;
    for(const cell of cells){
      const body=combatBodyAt(w,cell);if(body){damage(w,body,attack.damage,attack.faction);break;}
      const site=w.jobs.find(j=>j.kind==='build'&&active(j)&&same(j,cell));
      if(site&&attack.faction==='hostile'){finish(w,site,true);break;}
      if(blocksShot(w,cell))break;
    }
  }
}
function guarded(p: Person): boolean {return p.body.duties.guard>0&&p.body.duties.guard>=Math.max(p.body.duties.gatherer,p.body.duties.builder,p.body.duties.cook,p.body.duties.healer);}
function standGoal(w: World,desired: Cell,p: Person): Cell|null {
  return [desired,...neighbors(desired)].filter(c=>walkable(w,c)).sort((a,b)=>distance(a,desired)-distance(b,desired)||distance(a,p)-distance(b,p)||a.y-b.y||a.x-b.x)[0]??null;
}
function destination(p: Person,goal: Cell): void {
  if(!p.next&&same(p,goal)){clearTravel(p,true);return;}
  if(!p.destination||!same(p.destination,goal)){p.destination={...goal};p.route=[];p.retryAt=0;p.failures=0;}
}
export function prepareCombat(w: World): void {
  w.combat.orders=w.combat.orders.filter(o=>w.people.some(p=>p.id===o.actor&&p.life==='alive'));
  w.combat.cooldowns=w.combat.cooldowns.filter(c=>w.people.some(p=>p.id===c.actor&&p.life==='alive'));
  for(const order of w.combat.orders){
    const p=w.people.find(p=>p.id===order.actor)!;if(p.body.routine?.kind==='watch')endRoutine(w,p);
    if(p.body.routine||p.body.recoverAt>w.tick)continue;
    const enemy=order.mode==='rally'?w.combat.raiders.filter(r=>distance(r,order.target)<=C.guard.pursuit).sort((a,b)=>distance(a,p)-distance(b,p)||a.id-b.id)[0]:undefined;
    if(enemy){const progress={status:'Rally: approaching attacker'};approach(w,progress,p,enemy,'Rally: approaching attacker');p.body.status=progress.status;}
    else{const goal=standGoal(w,order.target,p);if(goal){destination(p,goal);p.body.status=`${order.mode==='rally'?'Guarding rally point':'Seeking emergency shelter'} ${goal.x},${goal.y}${p.blocked?': '+p.blocked:''}`;}else{clearTravel(p,true);p.body.status='Battle destination blocked; choose open ground or release order';}}
  }
}
function launch(w: World,source: CombatRef,origin: Cell,target: Cell,kind: Attack['kind'],faction: Attack['faction'],amount: number,delay: number): boolean {
  if(w.combat.attacks.length>=C.maxAttacks||w.combat.attacks.some(a=>a.source.kind===source.kind&&a.source.id===source.id))return false;
  const cells=kind==='bolt'?shotCells(w,origin,target):[{x:target.x,y:target.y}];if(!cells.length)return false;
  w.combat.attacks.push({id:w.nextId++,kind,source:{...source},origin:{x:origin.x,y:origin.y},target:{x:target.x,y:target.y},cells:cells.map(c=>({...c})),due:w.tick+delay,damage:amount,faction});w.combat.attacks.sort((a,b)=>a.due-b.due||a.id-b.id);return true;
}
function defenses(w: World): void {
  w.combat.defenses=w.combat.defenses.filter(d=>w.structures.some(s=>s.id===d.structure&&(s.kind==='tower'||s.kind==='trap')));
  for(const s of [...w.structures].sort((a,b)=>a.id-b.id)){
    if(s.kind!=='tower'&&s.kind!=='trap')continue;let d=w.combat.defenses.find(d=>d.structure===s.id);if(!d){d={structure:s.id,cooldown:0};w.combat.defenses.push(d);}
    if(d.cooldown>w.tick||s.inventory.ammo<=0)continue;
    const target=w.combat.raiders.filter(r=>s.kind==='trap'?same(s,r):distance(s,r)<=C.tower.range&&clearShot(w,s,r,r.id)).sort((a,b)=>distance(s,a)-distance(s,b)||a.id-b.id)[0];if(!target)continue;
    const profile=s.kind==='tower'?C.tower:C.trap;
    if(!launch(w,{kind:'structure',id:s.id},s,target,s.kind==='tower'?'bolt':'trap','colony',profile.damage,profile.windup))continue;
    s.inventory.ammo--;w.consumed.ammo++;d.cooldown=w.tick+profile.cooldown;if(s.kind==='tower')w.combat.stats.shots++;else w.combat.stats.traps++;
  }
}
function guards(w: World): void {
  for(const p of [...w.people].sort((a,b)=>a.id-b.id)){
    const order=w.combat.orders.find(o=>o.actor===p.id);
    if(p.life!=='alive'||p.next||p.body.recoverAt>w.tick||p.body.routine&&p.body.routine.kind!=='watch'||order&&order.mode!=='rally'||!order&&!guarded(p))continue;
    let cooldown=w.combat.cooldowns.find(c=>c.actor===p.id);if(cooldown&&cooldown.until>w.tick)continue;
    const target=w.combat.raiders.filter(r=>distance(p,r)<=1).sort((a,b)=>a.id-b.id)[0];if(!target)continue;
    if(!launch(w,{kind:'person',id:p.id},p,target,'melee','colony',C.guard.damage,C.guard.windup))continue;
    if(cooldown)cooldown.until=w.tick+C.guard.cooldown;else{cooldown={actor:p.id,until:w.tick+C.guard.cooldown};w.combat.cooldowns.push(cooldown);}
  }
}
function automaticOrders(w: World): void {
  const chosen=new Set<number>();
  for(const p of [...w.people].sort((a,b)=>a.id-b.id)){
    if(p.life!=='alive'||w.combat.orders.some(o=>o.actor===p.id))continue;
    const guard=guarded(p),post=guard?w.structures.filter(s=>s.kind==='guardpost').sort((a,b)=>distance(a,p)-distance(b,p)||a.id-b.id)[0]:undefined;let target:Cell|undefined=post;
    if(!target){const candidates:Cell[]=[];for(let y=60;y<=69;y++)for(let x=59;x<=70;x++){const at={x,y};if(walkable(w,at)&&!chosen.has(index(x,y)))candidates.push(at);}target=candidates.sort((a,b)=>distance(a,w.heart)-distance(b,w.heart)||distance(a,p)-distance(b,p)||a.y-b.y||a.x-b.x)[0];}
    if(target){chosen.add(index(target.x,target.y));setOrder(w,p,guard?'rally':'shelter',target);}
  }
}
function raidSchedule(w: World): void {
  const c=w.combat;
  if(w.scenario==='colony'&&c.phase==='peace'&&w.tick>=c.warningAt){c.phase='warning';c.wave=1;c.edge=(['north','east','south','west'] as const)[roll(w.rng,'raids',4)-1]!;c.pending=[...C.wave];c.status=`Raid announced from the ${c.edge}. Arrival at tick ${c.spawnAt}. Build and load towers, prepare medicine and rally defenders.`;}
  if(c.phase==='warning'&&w.tick>=c.spawnAt){c.phase='combat';automaticOrders(w);c.status=`Raid arriving from the ${c.edge}; civilians seek shelter, guards rally. Orders can be changed or released.`;}
  if(c.phase!=='combat')return;const occupied=spatial(w);
  if(c.pending.length&&c.raiders.length<C.maxRaiders){
    const candidates:Cell[]=[];for(let d=0;d<126;d++)for(const sign of d===0?[1]:[1,-1]){const side=64+d*sign;if(side<1||side>126)continue;candidates.push(c.edge==='north'?{x:side,y:1}:c.edge==='south'?{x:side,y:126}:c.edge==='east'?{x:126,y:side}:{x:1,y:side});}
    const entry=candidates.find(p=>walkable(w,p)&&!occupied.has(index(p.x,p.y)));
    if(entry){const kind=c.pending.shift()!;c.raiders.push({id:w.nextId++,kind,health:C[kind].health,x:entry.x,y:entry.y,next:null,progress:0,route:[],routeVersion:w.terrainVersion,retryAt:w.tick,cooldown:0,target:null});c.stats.spawned++;}
    else c.status=`Raid waits at the ${c.edge} edge: every legal landing is occupied or blocked. Pending attackers are retained.`;
  }
}
function refCell(w: World,ref: CombatRef): Cell|undefined {return ref.kind==='heart'?w.heart:ref.kind==='person'?w.people.find(p=>p.id===ref.id&&p.life==='alive'):ref.kind==='structure'?w.structures.find(s=>s.id===ref.id):w.combat.raiders.find(r=>r.id===ref.id);}
function selectTarget(w: World,r: Raider): CombatRef {
  const target=w.people.filter(p=>p.life==='alive'&&distance(p,r)<=C[r.kind].notice).sort((a,b)=>distance(r,a)-distance(r,b)||a.id-b.id)[0];return target?{kind:'person',id:target.id}:{kind:'heart',id:w.heart.id};
}
function melee(w: World,r: Raider,target: Cell): void {
  if(r.cooldown>w.tick)return;const built=w.structures.some(s=>same(s,target))||w.jobs.some(j=>j.kind==='build'&&active(j)&&same(j,target))||same(w.heart,target);
  const amount=r.kind==='breacher'&&built?C.breacher.structureDamage:C[r.kind].damage;
  if(launch(w,{kind:'raider',id:r.id},r,target,'melee','hostile',amount,C[r.kind].windup))r.cooldown=w.tick+C[r.kind].cooldown;
}
function moveRaiders(w: World): void {
  const occupied=spatial(w);let searches=0;
  for(const r of [...w.combat.raiders].sort((a,b)=>a.id-b.id)){
    if(r.next){r.progress++;if(r.progress>=MOVE_TICKS){occupied.delete(index(r.x,r.y));r.x=r.next.x;r.y=r.next.y;r.next=null;r.progress=0;}else continue;}
    const target=selectTarget(w,r),cell=refCell(w,target)!;
    if(!r.target||r.target.kind!==target.kind||r.target.id!==target.id){r.target=target;r.route=[];r.retryAt=w.tick;}
    if(distance(r,cell)<=1){r.route=[];melee(w,r,cell);continue;}
    if(r.routeVersion!==w.terrainVersion){r.route=[];r.routeVersion=w.terrainVersion;r.retryAt=w.tick;}
    if(r.route.length&&distance(r.route[r.route.length-1]!,cell)>1){r.route=[];r.retryAt=w.tick;}
    if(!r.route.length){if(r.retryAt>w.tick||searches>=C.searchesPerTick)continue;searches++;r.route=raiderRoute(w,r,cell,occupied,r.id);r.routeVersion=w.terrainVersion;r.retryAt=w.tick+10;}
    const next=r.route[0];if(!next)continue;
    if(!walkable(w,next)){if(breachable(w,next)&&distance(r,next)===1)melee(w,r,next);else{r.route=[];r.retryAt=w.tick+10;}continue;}
    const owner=occupied.get(index(next.x,next.y));if(owner!==undefined&&owner!==r.id){const person=w.people.find(p=>p.id===owner&&p.life==='alive');if(person)melee(w,r,person);r.route=[];r.retryAt=w.tick+10;continue;}
    r.next=r.route.shift()!;r.progress=0;occupied.set(index(next.x,next.y),r.id);
  }
}
export function advanceCombat(w: World): void {
  raidSchedule(w);moveRaiders(w);defenses(w);guards(w);const c=w.combat;
  if(c.phase==='combat'&&!c.pending.length&&!c.raiders.length){c.phase='recovery';c.completed++;c.attacks=[];for(const order of [...c.orders])releaseOrder(w,order);c.status=`Raid survived. ${c.stats.kills} attackers defeated; rescue injured colonists, repair defenses, reload and resume production.`;}
}
