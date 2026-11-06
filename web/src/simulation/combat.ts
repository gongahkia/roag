import { ENVIRONMENT, groundMoveTicks, hotGround } from './biome.ts';
import { active, distance, index, inside, same, spatial, walkable } from './model.ts';
import type { Cell, CommandInput, Person, World } from './model.ts';
import type { Attack, BattleOrder, Combat, CombatRef, Raider } from './combat-model.ts';
import { COMBAT_CONTENT as C } from './combat-content.ts';
import { blocksShot, clearShot, combatBodyAt, shotCells } from './combat-geometry.ts';
import { breachable, raiderRoute } from './combat-navigation.ts';
import { approach, clearTravel, destroyStructure, drop, finish } from './jobs.ts';
import { endRoutine, fall } from './needs.ts';
import { appendRaidSummary, firstRaidDeadlines, nextRaidDeadlines, nextSpawn, planWave } from './raid-director.ts';
import type { SpawnTicket } from './raid-model.ts';
import { isBossKind, BOSS_NAMES } from './raid-model.ts';
import { advanceEncounters, defendedDamage, encounterTurn, resolveEncounterImpact } from './encounters.ts';
import type { EncounterApi } from './encounters.ts';
import { ENCOUNTER_CONTENT as E } from './encounter-content.ts';
import { emptyStock } from './work-content.ts';

export function initialCombat(): Combat {
  return { phase:'peace', wave:0, ...firstRaidDeadlines(), completed:0, edge:'east', pending:[], budget:0, spent:0, history:[],
    waveStart:{tick:0,spawned:0,kills:0,deaths:0,ammo:0,structuresLost:0,repairs:0}, barricades:[],
    raiders:[], attacks:[], orders:[], defenses:[], cooldowns:[],
    stats:{spawned:0,kills:0,shots:0,traps:0,heartDamage:0,colonyDamage:0,structuresLost:0,repairs:0,bosses:0}, lootWood:0,
    status:'Prepare food, shelter, medicine and defenses. The first raid is announced on day four.' };
}
/** All entry identity and squad choices were committed at announcement time. */
export function createRaider(w: World,ticket: SpawnTicket,cell: Cell): Raider {
  const facing=({north:'south',south:'north',east:'west',west:'east'} as const)[ticket.edge];
  return {id:ticket.id,kind:ticket.kind,health:C[ticket.kind].health,...cell,facing,squadId:ticket.squadId,leaderId:ticket.leaderId,
    loweredUntil:0,exposedUntil:0,specialCount:0,charge:null,next:null,progress:0,moveTicks:4,route:[],routeVersion:w.terrainVersion,retryAt:w.tick,cooldown:0,target:null};
}
export function combatCommandReason(w: World, input: CommandInput): string | null {
  if (w.terminal) return 'The colony has ended';
  if (input.kind !== 'battle') return 'Unknown battle command';
  if (!Number.isSafeInteger(input.actor) || input.actor !== 0 && !w.people.some(p => p.id === input.actor && p.life === 'alive')) return 'Choose a living colonist or all colonists';
  if (!['rally','retreat','shelter','clear','release'].includes(input.mode) || !inside(input.target)) return 'Choose a battle order and a tile within the map';
  if(input.mode==='clear')return w.combat.barricades.some(b=>same(b,input.target))?null:'Choose a hostile barricade to clear';
  return input.mode !== 'release' && !walkable(w,input.target) ? 'Choose open ground for the rally or shelter destination' : null;
}
function releaseOrder(w: World, order: BattleOrder): void {
  const p=w.people.find(p=>p.id===order.actor);w.combat.orders=w.combat.orders.filter(o=>o.actor!==order.actor);
  if(!p||p.life!=='alive')return;
  if(p.body.routine&&p.body.routine.kind!=='watch'){p.body.routine.resume=p.job?null:order.resume?{...order.resume}:null;return;}
  clearTravel(p);if(!p.job&&order.resume)p.destination={...order.resume};p.body.status='Battle order released; resuming colony work';
}
function setOrder(w: World,p: Person,mode: BattleOrder['mode'],target: Cell): void {
  const old=w.combat.orders.find(o=>o.actor===p.id),resume=old?old.resume:!p.job?p.body.routine?.resume??p.destination:null;
  if(p.body.routine?.kind==='watch')endRoutine(w,p);if(!p.body.routine)clearTravel(p);
  const order:BattleOrder={actor:p.id,mode,target:{x:target.x,y:target.y},resume:resume?{x:resume.x,y:resume.y}:null};
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
  if(ref.kind==='barricade')return w.combat.barricades.some(b=>b.id===ref.id&&b.health>0);
  if(ref.kind==='structure')return w.structures.some(s=>s.id===ref.id&&s.health>0);
  return ref.id===w.heart.id&&w.heart.health>0;
}
function damage(w: World,ref: CombatRef,amount: number,faction: Attack['faction'],origin: Cell): void {
  if(ref.kind==='barricade'){const b=w.combat.barricades.find(b=>b.id===ref.id);if(!b)return;b.health=Math.max(0,b.health-amount);if(!b.health){w.combat.barricades=w.combat.barricades.filter(other=>other.id!==b.id);w.terrainVersion++;}return;}
  if(ref.kind==='raider'){
    if(faction!=='colony')return;const r=w.combat.raiders.find(r=>r.id===ref.id);if(!r)return;r.health=Math.max(0,r.health-defendedDamage(w,r,amount,origin));
    if(!r.health){
      w.combat.raiders=w.combat.raiders.filter(other=>other.id!==r.id);
      w.combat.attacks=w.combat.attacks.filter(a=>a.source.kind!=='raider'||a.source.id!==r.id);
      const owned=w.combat.barricades.filter(b=>b.owner===r.id);if(owned.length){w.combat.barricades=w.combat.barricades.filter(b=>b.owner!==r.id);w.terrainVersion++;}
      for(const other of w.combat.raiders)if(other.target?.kind==='raider'&&other.target.id===r.id){other.target=null;other.route=[];}
      w.combat.stats.kills++;if(isBossKind(r.kind))w.combat.stats.bosses++;
      const reward=isBossKind(r.kind)?5:1;w.combat.lootWood+=reward;w.produced.wood+=reward;const loot=emptyStock();loot.wood=reward;drop(w,r,loot);
    }
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
    if(resolveEncounterImpact(w,attack,encounterApi(w)))continue;
    const cells=attack.kind==='bolt'||attack.kind==='arrow'?shotCells(w,attack.origin,attack.target):attack.cells;
    for(const cell of cells){
      const body=combatBodyAt(w,cell);if(body){damage(w,body,attack.damage,attack.faction,attack.origin);break;}
      const site=w.jobs.find(j=>j.kind==='build'&&active(j)&&same(j,cell));
      if(site&&attack.faction==='hostile'){finish(w,site,true);break;}
      if(blocksShot(w,cell))break;
    }
  }
}
function guarded(p: Person): boolean {return p.body.duties.guard>0&&p.body.duties.guard>=Math.max(p.body.duties.gatherer,p.body.duties.builder,p.body.duties.cook,p.body.duties.healer);}
function standGoal(w: World,desired: Cell,p: Person,claimed: Set<number>): Cell|null {
  const occupied=spatial(w);
  const legal=(c:Cell)=>distance(c,desired)<=3&&walkable(w,c)&&!claimed.has(index(c.x,c.y))&&(occupied.get(index(c.x,c.y))===undefined||occupied.get(index(c.x,c.y))===p.id);
  // Preserve an established arrival tile, including after arrival, so crowd arbitration does not oscillate.
  if(p.destination&&legal(p.destination))return p.destination;
  if(legal(p))return {x:p.x,y:p.y};
  const candidates:Cell[]=[];
  for(let y=desired.y-3;y<=desired.y+3;y++)for(let x=desired.x-3;x<=desired.x+3;x++){const c={x,y};if(legal(c))candidates.push(c);}
  return candidates.sort((a,b)=>distance(a,desired)-distance(b,desired)||distance(a,p)-distance(b,p)||a.y-b.y||a.x-b.x)[0]??null;
}
function destination(p: Person,goal: Cell): void {
  if(!p.next&&same(p,goal)){clearTravel(p,true);return;}
  if(!p.destination||!same(p.destination,goal)){p.destination={...goal};p.route=[];p.retryAt=0;p.failures=0;}
}
export function prepareCombat(w: World): void {
  // Needs can down a source after impacts resolved earlier in this tick.
  w.combat.attacks=w.combat.attacks.filter(a=>aliveSource(w,a.source));
  const claimed=new Set<number>();
  w.combat.orders=w.combat.orders.filter(o=>w.people.some(p=>p.id===o.actor&&p.life==='alive'));
  w.combat.cooldowns=w.combat.cooldowns.filter(c=>w.people.some(p=>p.id===c.actor&&p.life==='alive'));
  for(const order of w.combat.orders){
    const p=w.people.find(p=>p.id===order.actor)!;if(p.body.routine?.kind==='watch')endRoutine(w,p);
    if(p.body.routine||p.body.recoverAt>w.tick)continue;
    if(order.mode==='clear'){const barrier=w.combat.barricades.find(b=>same(b,order.target));if(!barrier){releaseOrder(w,order);continue;}const progress={status:'Approaching hostile barricade'};approach(w,progress,p,barrier,'Approaching hostile barricade');p.body.status=progress.status;continue;}
    const enemy=order.mode==='rally'?w.combat.raiders.filter(r=>distance(r,order.target)<=C.guard.pursuit).sort((a,b)=>distance(a,p)-distance(b,p)||a.id-b.id)[0]:undefined;
    if(enemy){const progress={status:'Rally: approaching attacker'};approach(w,progress,p,enemy,'Rally: approaching attacker');p.body.status=progress.status;}
    else{const goal=standGoal(w,order.target,p,claimed);if(goal){claimed.add(index(goal.x,goal.y));destination(p,goal);p.body.status=`${order.mode==='rally'?'Guarding rally point':'Seeking emergency shelter'} ${goal.x},${goal.y}${p.blocked?': '+p.blocked:''}`;}else{clearTravel(p,true);p.body.status='Battle destination blocked; choose open ground or release order';}}
  }
}
function launch(w: World,source: CombatRef,origin: Cell,target: Cell,kind: Attack['kind'],faction: Attack['faction'],amount: number,delay: number,extra?:{cells?:Cell[];recipient?:number}): boolean {
  if(w.combat.attacks.length>=C.maxAttacks||w.combat.attacks.some(a=>a.source.kind===source.kind&&a.source.id===source.id))return false;
  const cells=extra?.cells??(kind==='bolt'||kind==='arrow'?shotCells(w,origin,target):[{x:target.x,y:target.y}]);if(!cells.length)return false;
  w.combat.attacks.push({id:w.nextId++,kind,source:{...source},origin:{x:origin.x,y:origin.y},target:{x:target.x,y:target.y},cells:cells.map(c=>({...c})),due:w.tick+delay,damage:amount,faction,recipient:extra?.recipient??null});w.combat.attacks.sort((a,b)=>a.due-b.due||a.id-b.id);return true;
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
    if(p.life!=='alive'||p.next||p.body.recoverAt>w.tick||p.body.routine&&p.body.routine.kind!=='watch'||order&&order.mode!=='rally'&&order.mode!=='clear'||!order&&!guarded(p))continue;
    let cooldown=w.combat.cooldowns.find(c=>c.actor===p.id);if(cooldown&&cooldown.until>w.tick)continue;
    const target=order?.mode==='clear'?w.combat.barricades.find(b=>same(b,order.target)&&distance(p,b)<=1):w.combat.raiders.filter(r=>distance(p,r)<=1).sort((a,b)=>a.id-b.id)[0];if(!target)continue;
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
  if(w.scenario==='colony'&&(c.phase==='peace'||c.phase==='recovery')&&w.tick>=c.warningAt){
    const plan=planWave(c.completed+1,w.rng.raids,w.nextId,w.seed);w.rng.raids=plan.nextRaidsRng;w.nextId=plan.nextId;
    c.phase='warning';c.wave=plan.wave;c.edge=plan.edge;c.pending=plan.tickets;c.budget=plan.budget;c.spent=plan.spent;
    c.waveStart={tick:w.tick,spawned:c.stats.spawned,kills:c.stats.kills,deaths:w.statistics.deaths,ammo:w.consumed.ammo,structuresLost:c.stats.structuresLost,repairs:c.stats.repairs};
    c.status=`Wave ${c.wave} announced from the ${c.edge}${plan.boss?` — the ${BOSS_NAMES[plan.boss]} approaches`:''}. Arrival at tick ${c.spawnAt}. Budget ${c.spent}/${c.budget}; prepare and reload.`;
  }
  if(c.phase==='warning'&&w.tick>=c.spawnAt){c.phase='combat';automaticOrders(w);c.status=`Wave ${c.wave} arriving from the ${c.edge}; civilians seek shelter, guards rally. Orders can be changed or released.`;}
  if(c.phase!=='combat'||!c.pending.length)return;
  const occupied=spatial(w),spawn=nextSpawn(c.pending,c.raiders.length,p=>walkable(w,p)&&!occupied.has(index(p.x,p.y)));
  if(spawn.ticket&&spawn.cell){c.pending=spawn.remaining;c.raiders.push(createRaider(w,spawn.ticket,spawn.cell));c.stats.spawned++;}
  else c.status=`Raid waits at the ${c.edge} edge or actor cap. Pending attackers and their identities are retained.`;
}
function refCell(w: World,ref: CombatRef): Cell|undefined {return ref.kind==='heart'?w.heart:ref.kind==='person'?w.people.find(p=>p.id===ref.id&&p.life==='alive'):ref.kind==='structure'?w.structures.find(s=>s.id===ref.id):ref.kind==='barricade'?w.combat.barricades.find(b=>b.id===ref.id):w.combat.raiders.find(r=>r.id===ref.id);}
function selectTarget(w: World,r: Raider): CombatRef {
  if(r.kind==='breacher'){const tower=w.structures.filter(s=>s.kind==='tower'&&distance(s,r)<=C.breacher.defenseNotice).sort((a,b)=>distance(a,r)-distance(b,r)||a.id-b.id)[0];if(tower)return {kind:'structure',id:tower.id};}
  const target=w.people.filter(p=>p.life==='alive'&&distance(p,r)<=C[r.kind].notice).sort((a,b)=>distance(r,a)-distance(r,b)||a.id-b.id)[0];return target?{kind:'person',id:target.id}:{kind:'heart',id:w.heart.id};
}
function melee(w: World,r: Raider,target: Cell): void {
  if(r.cooldown>w.tick||distance(r,target)>1)return;const built=w.structures.some(s=>same(s,target))||w.jobs.some(j=>j.kind==='build'&&active(j)&&same(j,target))||same(w.heart,target);
  const amount=r.kind==='breacher'&&built?C.breacher.structureDamage:C[r.kind].damage;
  if(launch(w,{kind:'raider',id:r.id},r,target,'melee','hostile',amount,C[r.kind].windup)){r.cooldown=w.tick+C[r.kind].cooldown;if(r.kind==='shield')r.loweredUntil=w.tick+E.shield.loweredTicks;}
}
function encounterApi(w: World,move: EncounterApi['move']=()=>{},movedPeople=new Set<number>()): EncounterApi {
  return {
    launch:(source,origin,target,kind,faction,amount,delay,extra)=>launch(w,source,origin,target,kind,faction,amount,delay,extra),
    damage:(ref,amount,faction,origin)=>damage(w,ref,amount,faction,origin), move,
    breach:(cell)=>{const site=w.jobs.find(j=>j.kind==='build'&&active(j)&&same(j,cell));if(!site)return false;finish(w,site,true);return true;},
    shove:(id,delta)=>{
      const p=w.people.find(p=>p.id===id&&p.life==='alive');
      if(!p||p.next||movedPeople.has(id)||Math.abs(delta.x)+Math.abs(delta.y)!==1)return false;
      const next={x:p.x+delta.x,y:p.y+delta.y};if(!walkable(w,next)||spatial(w).has(index(next.x,next.y)))return false;
      p.x=next.x;p.y=next.y;clearTravel(p);movedPeople.add(id);return true;
    },
  };
}
function face(r: Raider,cell: Cell,tick: number): void {
  const dx=cell.x-r.x,dy=cell.y-r.y;if(!dx&&!dy)return;
  const direction=Math.abs(dx)>=Math.abs(dy)?dx>0?'east':'west':dy>0?'south':'north';
  if(direction!==r.facing&&r.kind==='captain')r.exposedUntil=Math.max(r.exposedUntil,tick+E.captain.exposureTicks);
  r.facing=direction;
}
function moveRaiders(w: World,movedPeople: Set<number>): void {
  const occupied=spatial(w);let searches=0;
  const requests:{raider:Raider;cell:Cell;adjacent:boolean;waitingSince:number}[]=[];
  const enterRoute=(r:Raider):void=>{
    const next=r.route[0];if(!next)return;
    if(!walkable(w,next)){if(breachable(w,next)&&distance(r,next)===1){face(r,next,w.tick);melee(w,r,next);}else{r.route=[];r.retryAt=w.tick+10;}return;}
    const owner=occupied.get(index(next.x,next.y));if(owner!==undefined&&owner!==r.id){const person=w.people.find(p=>p.id===owner&&p.life==='alive');if(person){face(r,person,w.tick);melee(w,r,person);}r.route=[];r.retryAt=w.tick+10;return;}
    face(r,next,w.tick);r.next=r.route.shift()!;r.progress=0;r.moveTicks=groundMoveTicks(w,r.next);occupied.set(index(next.x,next.y),r.id);
  };
  const move=(r:Raider,cell:Cell,adjacent:boolean):void=>{
    if(r.next||r.charge)return;
    if(distance(r,cell)<=(adjacent?1:0)){r.route=[];return;}
    if(r.routeVersion!==w.terrainVersion){r.route=[];r.routeVersion=w.terrainVersion;r.retryAt=Math.min(r.retryAt,w.tick);}
    if(r.route.length&&distance(r.route[r.route.length-1]!,cell)>(adjacent?1:0)){r.route=[];r.retryAt=Math.min(r.retryAt,w.tick);}
    if(!r.route.length){
      if(r.retryAt<=w.tick)requests.push({raider:r,cell:{x:cell.x,y:cell.y},adjacent,waitingSince:r.retryAt});
      return;
    }
    enterRoute(r);
  };
  const api=encounterApi(w,move,movedPeople);
  for(const r of [...w.combat.raiders].sort((a,b)=>a.id-b.id)){
    if(r.charge||w.combat.attacks.some(a=>a.source.kind==='raider'&&a.source.id===r.id))continue;
    if(r.next){r.progress++;if(r.progress>=r.moveTicks){occupied.delete(index(r.x,r.y));r.x=r.next.x;r.y=r.next.y;r.next=null;r.progress=0;r.moveTicks=4;}else continue;}
    if(encounterTurn(w,r,api))continue;
    const target=selectTarget(w,r),cell=refCell(w,target)!;
    if(!r.target||r.target.kind!==target.kind||r.target.id!==target.id){r.target=target;r.route=[];r.retryAt=Math.min(r.retryAt,w.tick);}
    if(distance(r,cell)<=1){r.route=[];face(r,cell,w.tick);melee(w,r,cell);continue;}
    move(r,cell,true);
  }
  // Keep actor motion/AI in stable identity order, but share the bounded search
  // budget by saved waiting age. A blocked early actor cannot monopolize every
  // retry slot while a later actor has never received its first search.
  requests.sort((a,b)=>a.waitingSince-b.waitingSince||a.raider.id-b.raider.id);
  for(const request of requests){
    if(searches>=C.searchesPerTick)break;
    const r=request.raider;
    if(r.next||r.charge||w.combat.attacks.some(a=>a.source.kind==='raider'&&a.source.id===r.id))continue;
    searches++;r.route=raiderRoute(w,r,request.cell,occupied,r.id,C.searchLimit,request.adjacent);
    r.routeVersion=w.terrainVersion;r.retryAt=w.tick+10;
    enterRoute(r);
  }
  advanceEncounters(w,api);
}
export function advanceCombat(w: World,movedPeople=new Set<number>()): void {
  if(w.tick%ENVIRONMENT.heatEvery===0){
    for(const p of w.people)if(p.life==='alive'&&hotGround(w,p))damage(w,{kind:'person',id:p.id},ENVIRONMENT.heatDamage,'hostile',p);
    for(const r of [...w.combat.raiders])if(hotGround(w,r))damage(w,{kind:'raider',id:r.id},ENVIRONMENT.heatDamage,'colony',r);
  }
  raidSchedule(w);moveRaiders(w,movedPeople);defenses(w);guards(w);const c=w.combat;
  // Barricade expiry or destruction can invalidate an old pursuit target in this tick.
  for(const r of c.raiders)if(r.target&&!refCell(w,r.target)){r.target=null;r.route=[];}
  if(c.phase==='combat'&&!c.pending.length&&!c.raiders.length){
    c.phase='recovery';c.completed++;c.attacks=[];const start=c.waveStart;
    c.history=appendRaidSummary(c.history,{wave:c.wave,budget:c.budget,spent:c.spent,startedAt:start.tick,endedAt:w.tick,
      spawned:c.stats.spawned-start.spawned,kills:c.stats.kills-start.kills,colonistsLost:w.statistics.deaths-start.deaths,
      ammoSpent:w.consumed.ammo-start.ammo,structuresLost:c.stats.structuresLost-start.structuresLost,repairs:c.stats.repairs-start.repairs});
    Object.assign(c,nextRaidDeadlines(w.tick));
    for(const order of [...c.orders])releaseOrder(w,order);
    c.status=`Wave ${c.wave} survived. Rescue, repair and reload; wave ${c.wave+1} warning at tick ${c.warningAt}.`;
  }
}
