import { DUTIES, NEEDS, RECIPES } from '../simulation/survival-content.ts';
import { CONTENT_VERSION, WIDTH, HEIGHT, MOVE_TICKS, index, same, distance, walkable } from '../simulation/model.ts';
import type { World, Cell } from '../simulation/model.ts';
import { STREAMS, validSeed } from '../simulation/random.ts';
import { canonical } from '../simulation/canonical.ts';
import { materialAudit, capacity, isStore, jobCost, jobTicks, available } from '../simulation/jobs.ts';
import { active } from '../simulation/model.ts';
import { BUILDINGS, MATERIALS, total, WORK_CONTENT } from '../simulation/work-content.ts';
import type { Stock } from '../simulation/work-content.ts';
import { terminalReason } from '../simulation/step.ts';
import { validateCombat } from './combat-schema.ts';
export type Controls = { manualPaused:boolean; speed:1|2|4 };
export type Save = { format:'roag-colony'; version:5; contentVersion:typeof CONTENT_VERSION; world:World; controls:Controls };
export const MAX_SAVE_BYTES=2_000_000;
function assert(ok:unknown,message:string):asserts ok {if(!ok)throw new Error(`Invalid save: ${message}`);}
function object(value:unknown,keys:string[]):asserts value is Record<string,unknown> {
  assert(typeof value==='object'&&value!==null&&!Array.isArray(value),'expected object');
  assert(Object.keys(value).sort().join('|')===[...keys].sort().join('|'),'unknown or missing fields');
}
const integer=(n:unknown,min=0,max=1e9):n is number=>Number.isSafeInteger(n)&&Number(n)>=min&&Number(n)<=max;
function cell(value:unknown):asserts value is Cell {object(value,['x','y']);assert(integer(value.x,0,WIDTH-1)&&integer(value.y,0,HEIGHT-1),'cell outside world');}
export function validateSave(value:unknown):Save {
  object(value,['format','version','contentVersion','world','controls']);
  assert(value.format==='roag-colony'&&value.version===5&&value.contentVersion===CONTENT_VERSION,'unsupported format, schema or content version');
  object(value.controls,['manualPaused','speed']);assert(typeof value.controls.manualPaused==='boolean'&&[1,2,4].includes(Number(value.controls.speed))&&typeof value.controls.speed==='number','controls');
  const v=value.world;object(v,['combat','seed','tick','nextId','nextSequence','rng','width','height','terrain','terrainVersion','scenario','generation','heart','people','resources','pending','terminal','jobs','structures','piles','spent','lost','sourceRemoved','produced','consumed','statistics','recipeCounts']);
  assert(validSeed(Number(v.seed))&&typeof v.seed==='number'&&integer(v.tick)&&integer(v.nextId,8)&&integer(v.nextSequence,1)&&integer(v.terrainVersion),'counters/seed');
  assert(v.width===WIDTH&&v.height===HEIGHT&&Array.isArray(v.terrain)&&v.terrain.length===WIDTH*HEIGHT&&v.terrain.every(n=>integer(n,0,2)),'terrain');
  assert(v.scenario==='colony'||v.scenario==='routing-lab'||v.scenario==='work-lab','scenario');
  object(v.rng,[...STREAMS]);for(const label of STREAMS)assert(typeof v.rng[label]==='number'&&validSeed(v.rng[label] as number),'RNG stream');
  object(v.generation,['attempts','fallback']);assert(integer(v.generation.attempts,0,3)&&typeof v.generation.fallback==='boolean','generation');
  object(v.heart,['id','x','y','health']);assert(v.heart.id===1&&v.heart.x===64&&v.heart.y===64&&integer(v.heart.health,0,100),'heart');
  assert(Array.isArray(v.people)&&v.people.length===6&&Array.isArray(v.resources)&&v.resources.length===18&&Array.isArray(v.pending)&&v.pending.length<=128,'entity counts');
  assert(Array.isArray(v.jobs)&&v.jobs.length<=256&&Array.isArray(v.structures)&&v.structures.length<=256&&Array.isArray(v.piles)&&v.piles.length<=2048,'work entity counts');
  const world=v as unknown as World;
  // walkable is used before the full combat validator; reject malformed blocker
  // arrays here so imports cannot throw raw errors while inspecting founders.
  assert(typeof v.combat==='object'&&v.combat!==null&&!Array.isArray(v.combat),'combat object');
  const barriers=(v.combat as Record<string,unknown>).barricades;
  assert(Array.isArray(barriers)&&barriers.length<=8,'barricade limit');
  for(const barrier of barriers){object(barrier,['id','owner','x','y','health','expiresAt']);cell({x:barrier.x,y:barrier.y});}
  assert(world.jobs.filter(active).length<=128&&world.structures.length+world.jobs.filter(j=>active(j)&&j.kind==='build').length<=256,'active work / future structure budget');
  const ids=new Set<number>([1]),occupied=new Set<number>();
  for(let i=0;i<v.people.length;i++) {
    const p=v.people[i];object(p,['id','founder','life','x','y','destination','next','progress','route','routeVersion','retryAt','blocked','job','cargo','yieldTo','yieldUntil','failures','body']);
    assert(p.id===i+2&&p.founder===i&&typeof p.life==='string'&&['alive','downed','dead'].includes(p.life),'founder identity/life');ids.add(p.id as number);
    assert(integer(p.x,0,127)&&integer(p.y,0,127)&&(p.life==='dead'||walkable(world,p as unknown as Cell)),'person location');
    assert(integer(p.progress,0,MOVE_TICKS-1)&&integer(p.routeVersion,0,world.terrainVersion)&&integer(p.retryAt),'movement counters');
    assert(p.blocked===null||typeof p.blocked==='string'&&['Destination is blocked','Route search budget reached; retrying','No route; waiting for access','Occupied; yielding','Yielding to traffic'].includes(p.blocked),'blocked reason');
    if(p.destination!==null)cell(p.destination);
    if(p.yieldTo!==null)cell(p.yieldTo);
    assert(integer(p.yieldUntil)&&integer(p.failures)&& (p.job===null||integer(p.job,8)), 'worker scheduling');stock(p.cargo);assert(total(p.cargo as Stock)<=WORK_CONTENT.cargoCapacity,'cargo capacity');
    if(p.next!==null){cell(p.next);assert(distance(p as unknown as Cell,p.next)===1&&walkable(world,p.next),'movement edge');}
    else assert(p.progress===0,'progress without a step');
    assert(Array.isArray(p.route)&&p.route.length<=WIDTH*HEIGHT,'route length');
    let previous=(p.next??p) as Cell;
    for(const next of p.route){cell(next);assert(distance(previous,next)===1,'route adjacency');if(p.routeVersion===world.terrainVersion)assert(walkable(world,next),'route crosses solid terrain');previous=next;}
    assert(p.destination!==null||p.yieldTo!==null||p.route.length===0&&p.next===null,'route/step without intent');
    if(p.route.length)assert(same(previous,(p.yieldTo??p.destination) as Cell),'route destination');
    if(p.life!=='dead')for(const pos of [p as unknown as Cell,...(p.next?[p.next as Cell]:[])]){const key=index(pos.x,pos.y);assert(!occupied.has(key),'duplicate occupied/reserved cell');occupied.add(key);}
  }
  const resourceCells=new Set<number>();
  for(const r of v.resources){object(r,['id','kind','x','y','initial','remaining']);assert(integer(r.id,8,world.nextId-1)&&!ids.has(r.id),'resource ID');ids.add(r.id as number);assert(typeof r.kind==='string'&&['wood','stone','food'].includes(r.kind)&&integer(r.x,1,126)&&integer(r.y,1,126)&&integer(r.initial,1,81)&&integer(r.remaining,0,r.initial as number),'resource');const key=index(r.x as number,r.y as number);assert(!resourceCells.has(key),'duplicate resource');resourceCells.add(key);if(r.kind!=='food'&&Number(r.remaining)>0)assert(world.terrain[key]===2,'solid resource');}
  validateCombat(world,ids,occupied);
  validateBodies(world);
  validateWork(world,ids);
  assert(Math.max(...ids)<world.nextId,'next ID');
  let lastDue=-1,lastSequence=0;const sequences=new Set<number>();
  for(const c of v.pending){object(c,['sequence','due','input']);assert(integer(c.sequence,1,world.nextSequence-1)&&!sequences.has(c.sequence)&&integer(c.due,world.tick+1),'pending command identity/time');sequences.add(c.sequence);assert((c.due as number)>lastDue||c.due===lastDue&&(c.sequence as number)>lastSequence,'command order');lastDue=c.due as number;lastSequence=c.sequence as number;
    commandShape(world,c.input);
  }
  assert(world.terminal===terminalReason(world),'terminal consistency');
  return structuredClone(value) as unknown as Save;
}
export function makeSave(world:World,controls:Controls):Save {return validateSave({format:'roag-colony',version:5,contentVersion:CONTENT_VERSION,world,controls});}
export function encodeSave(save:Save):string {return canonical(validateSave(save));}
export function parseSave(text:string):Save {
  if(text.length>MAX_SAVE_BYTES)throw new Error('Save file is too large');
  let value:unknown;try{value=JSON.parse(text);}catch{throw new Error('Invalid save: malformed JSON');}
  return validateSave(value);
}
function stock(value: unknown): asserts value is Stock {
  object(value,[...MATERIALS]);for(const k of MATERIALS)assert(integer(value[k],0,100000),'material quantity');
}
function commandShape(world: World, value: unknown): void {
  assert(value!==null&&typeof value==='object','command');const v=value as Record<string,unknown>;
  const ref=(list:{id:number}[],id:unknown)=>list.some(p=>p.id===id);
  switch(v.kind) {
    case 'battle': object(v,['kind','actor','mode','target']);cell(v.target);assert((v.actor===0||ref(world.people,v.actor))&&['rally','retreat','shelter','clear','release'].includes(String(v.mode)),'battle command');break;
    case 'reload': object(v,['kind','structure']);assert(world.structures.some(s=>s.id===v.structure&&(s.kind==='tower'||s.kind==='trap')),'reload reference');break;
    case 'move': object(v,['kind','actor','target']);cell(v.target);assert(ref(world.people,v.actor),'actor reference');break;
    case 'edit': object(v,['kind','target','solid']);cell(v.target);assert(world.scenario==='routing-lab'&&typeof v.solid==='boolean','edit command');break;
    case 'stockpile': object(v,['kind','target']);cell(v.target);break;
    case 'build': object(v,['kind','target','building']);cell(v.target);assert(BUILDINGS.includes(v.building as never),'building');break;
    case 'duty': object(v,['kind','actor','duty','value']);assert(ref(world.people,v.actor)&&DUTIES.includes(v.duty as never)&&integer(v.value,0,3),'duty');break;
    case 'produce': object(v,['kind','structure','recipe']);assert(world.structures.some(s=>s.id===v.structure&&(v.recipe==='farm'?s.kind==='farm':Object.hasOwn(RECIPES,String(v.recipe))&&s.kind===RECIPES[v.recipe as keyof typeof RECIPES].facility)),'production facility');break;
    case 'gather': object(v,['kind','source']);assert(ref(world.resources,v.source),'source reference');break;
    case 'work': object(v,['kind','job','action']);assert(ref(world.jobs,v.job)&&['cancel','suspend','resume'].includes(v.action as string),'work reference/action');break;
    case 'priority': object(v,['kind','job','value']);assert(ref(world.jobs,v.job)&&integer(v.value,1,3),'priority');break;
    case 'assign': object(v,['kind','job','worker']);assert(ref(world.jobs,v.job)&&(v.worker===null||ref(world.people,v.worker)),'assignment');break;
    case 'repair': case 'dismantle': object(v,['kind','structure']);assert(ref(world.structures,v.structure),'structure reference');break;
    case 'door': object(v,['kind','structure','open']);assert(world.structures.some(s=>s.id===v.structure&&s.kind==='door')&&typeof v.open==='boolean','door');break;
    case 'dev': object(v,['kind','action','id']);assert(world.scenario==='work-lab'&&(['kill','down','injure','hungry','tired'].includes(String(v.action))?ref(world.people,v.id):v.action==='deplete'?ref(world.resources,v.id):v.action==='damage'&&ref(world.structures,v.id)),'development reference');break;
    default: throw new Error('Invalid save: unknown command');
  }
}
function validateWork(world: World, ids: Set<number>): void {
  for(const s of [world.spent,world.lost,world.sourceRemoved,world.produced,world.consumed])stock(s);
  const addId=(id:number)=>{assert(integer(id,8,world.nextId-1)&&!ids.has(id),'duplicate work ID');ids.add(id);};
  const positions=new Set<number>();
  for(const s of world.structures) {
    object(s,['id','x','y','kind','health','open','inventory','embodied']);addId(s.id);cell({x:s.x,y:s.y});
    assert(['stockpile',...BUILDINGS].includes(s.kind)&&integer(s.health,1,100)&&typeof s.open==='boolean','structure definition');stock(s.inventory);stock(s.embodied);
    assert(total(s.inventory)<=capacity(s),'storage capacity');
    if(s.kind==='tower'||s.kind==='trap')assert(MATERIALS.every(k=>k==='ammo'||s.inventory[k]===0),'defense magazine contains non-ammunition');
    for(const k of MATERIALS)assert(s.embodied[k]===(s.kind==='stockpile'?0:WORK_CONTENT.buildings[s.kind].cost[k]),'embodied recipe');
    const key=index(s.x,s.y);assert(!positions.has(key)&&world.terrain[key]!==2&&!same(s,world.heart)&&!world.resources.some(r=>r.remaining>0&&same(r,s)),'structure footprint');positions.add(key);
  }
  const pilePositions=new Set<number>();
  for(const p of world.piles) {object(p,['id','x','y','inventory']);addId(p.id);cell({x:p.x,y:p.y});stock(p.inventory);const key=index(p.x,p.y);assert(!pilePositions.has(key)&&walkable(world,p),'bundle location');pilePositions.add(key);}
  let previousId=0;
  const workers=new Set<number>(),targets=new Set<string>();
  for(const j of world.jobs) {
    object(j,['id','kind','x','y','ref','building','state','priority','worker','requested','progress','site','reservation','delivery','status','completions','retryAt','recipe','stage','readyAt','cycles']);addId(j.id);assert(j.id>previousId,'job order');previousId=j.id;cell({x:j.x,y:j.y});
    assert(['gather','haul','build','repair','dismantle','farm','produce','reload'].includes(j.kind)&&['active','suspended','done','cancelled'].includes(j.state)&&integer(j.priority,1,3),'job definition');
    assert(j.kind==='produce'?Object.hasOwn(RECIPES,j.recipe??''):j.recipe===null,'recipe identity');assert(integer(j.stage,0,3)&&integer(j.readyAt)&&integer(j.cycles)&&(j.kind==='farm'||j.kind==='produce'||j.stage===0&&j.readyAt===0&&j.cycles===0),'production stage');
    assert(j.building===null||BUILDINGS.includes(j.building),'job building');assert((j.kind!=='build'&&j.kind!=='repair')||j.building!==null,'building required');
    if(j.kind==='reload')assert(j.building==='tower'||j.building==='trap','reload building');
    assert(j.ref===null||integer(j.ref,8,world.nextId-1),'job target reference');assert(j.kind==='build'?j.ref===null:j.ref!==null,'job target identity');
    assert(integer(j.progress,0,jobTicks(j))&&integer(j.retryAt)&&typeof j.status==='string'&&j.status.length<300&&j.completions===(j.state==='done'?1:0),'work progress/completion');stock(j.site);
    assert(j.requested===null||world.people.some(p=>p.id===j.requested),'requested worker');
    const p=world.people.find(p=>p.id===j.worker);
    assert(j.worker===null||p&&p.life==='alive'&&p.job===j.id&&!workers.has(p.id)&&j.state==='active','worker claim');if(p)workers.add(p.id);
    if(!active(j))assert(j.worker===null&&j.reservation===null&&j.delivery===null&&total(j.site)===0,'settled job');
    if(j.state==='suspended')assert(j.worker===null&&j.reservation===null,'suspended claims');
    if(active(j)) {
      const key=j.kind==='build'?`build:${j.x},${j.y}`:`${['repair','dismantle','farm','produce','reload'].includes(j.kind)?'structure':j.kind}:${j.ref}`;assert(!targets.has(key),'duplicate work target');targets.add(key);
      if(j.kind==='gather')assert(world.resources.some(r=>r.id===j.ref&&same(r,j)),'gather reference');
      if(j.kind==='haul')assert(world.piles.some(b=>b.id===j.ref&&same(b,j)),'bundle reference');
      if(j.kind==='repair'||j.kind==='dismantle')assert(world.structures.some(s=>s.id===j.ref&&same(s,j)&&(s.kind==='stockpile'?j.building===null:j.building===s.kind)),'structure work reference');
      if(j.kind==='reload')assert(world.structures.some(s=>s.id===j.ref&&same(s,j)&&s.kind===j.building&&capacity(s)-s.inventory.ammo>=jobCost(j).ammo),'reload target or magazine space');
      if(j.kind==='farm'||j.kind==='produce')assert(world.structures.some(s=>s.id===j.ref&&same(s,j)&&s.kind===(j.kind==='farm'?'farm':RECIPES[j.recipe!].facility)),'production reference');
      if(j.kind==='build') {const key=index(j.x,j.y);assert(!positions.has(key)&&world.terrain[key]!==2&&!same(j,world.heart)&&!world.resources.some(r=>r.remaining>0&&same(r,j))&&!world.people.some(p=>p.life!=='dead'&&(same(p,j)||p.next&&same(p.next,j)))&&!world.combat.raiders.some(r=>same(r,j)||r.next&&same(r.next,j)),'site footprint');positions.add(key);}
    }
    if(j.reservation) {
      const r=j.reservation;object(r,['source','kind','count']);assert(MATERIALS.includes(r.kind)&&integer(r.count,1,WORK_CONTENT.cargoCapacity)&&j.worker!==null&&j.state==='active','reservation');
      assert(world.structures.some(s=>s.id===r.source&&isStore(s))||world.piles.some(b=>b.id===r.source),'reservation source');assert(available(world,r.source,r.kind)>=0,'overclaimed material');
      assert(j.kind==='build'||j.kind==='repair'||j.kind==='produce'||j.kind==='reload'||j.kind==='haul'&&j.ref===r.source,'reservation purpose');
    }
    assert(j.delivery===null||world.structures.some(s=>s.id===j.delivery&&isStore(s)),'delivery reference');
    const cost=jobCost(j);
    if(j.kind==='build'||j.kind==='repair'||j.kind==='reload'||j.kind==='produce'&&j.stage!==3)for(const k of MATERIALS) {
      const owned=j.site[k]+(p?.cargo[k]??0)+(j.reservation?.kind===k?j.reservation.count:0);assert(owned<=cost[k],'supplies exceed recipe');
      if(j.progress>0&&active(j))assert(j.site[k]===cost[k],'work before supplies delivered');
    } else assert(total(j.site)===0,'nonconstruction site supplies');
    if(j.kind==='produce')assert(j.stage===0||j.stage===3,'recipe stage');
    if(j.kind==='farm'&&j.stage===2)assert(j.readyAt>0,'crop growth deadline');
    if(j.stage===3)assert(j.progress===0&&j.cycles>0&&j.reservation===null&&total(j.site)===0,'settled production cycle');
  }
  for(const p of world.people)assert(p.job===null?total(p.cargo)===0:world.jobs.some(j=>j.id===p.job&&j.worker===p.id&&j.state==='active'),'orphaned cargo/job');
  for(const a of Object.values(materialAudit(world)))assert(a.produced>=0&&a.produced===a.accounted,'material conservation');
}

function validateBodies(w:World):void {
  const facilities=new Set<number>(),patients=new Set<number>();
  object(w.statistics,['eatenFood','eatenMeals','harvests','recipes','rescues','treatments','deaths','workTicks']);
  for(const n of Object.values(w.statistics))assert(integer(n),'survival statistic');
  object(w.recipeCounts,Object.keys(RECIPES));for(const n of Object.values(w.recipeCounts))assert(integer(n),'recipe count');
  assert(Object.values(w.recipeCounts).reduce((a,b)=>a+b,0)===w.statistics.recipes,'recipe statistics');
  for(const p of w.people){
    const b=p.body;object(b,['hunger','fatigue','health','mood','deathAt','recoverAt','meals','rests','duties','routine','status']);
    assert(integer(b.hunger,0,10000)&&integer(b.fatigue,0,10000)&&integer(b.health,0,100)&&integer(b.mood,0,100)&&integer(b.meals)&&integer(b.rests)&&integer(b.deathAt)&&integer(b.recoverAt)&&typeof b.status==='string'&&b.status.length<300,'body');
    assert(p.life==='alive'?b.health>0&&b.deathAt===0:p.life==='downed'?b.health===0&&b.deathAt>w.tick&&b.deathAt<=w.tick+NEEDS.rescueWindow:b.health===0&&b.deathAt===0,'life deadline');
    assert(b.recoverAt<=w.tick+NEEDS.recoveryTicks,'recovery deadline');
    object(b.duties,[...DUTIES]);for(const n of Object.values(b.duties))assert(integer(n,0,3),'duty priority');
    if(p.life!=='alive')assert(p.job===null&&p.next===null&&p.destination===null&&p.route.length===0&&b.routine===null,'inactive life ownership');
    const r=b.routine;if(!r)continue;
    object(r,['kind','facility','patient','reservation','cargo','progress','status','resume']);
    assert(p.life==='alive'&&['eat','rest','treatment','rescue','watch'].includes(r.kind)&&integer(r.progress,0,10000)&&typeof r.status==='string'&&r.status.length<300,'routine');assert(r.kind==='eat'?r.progress<NEEDS.eatTicks:r.kind==='rescue'?r.progress<NEEDS.treatmentTicks:r.kind==='rest'?r.progress<=1000:r.progress===0,'routine progress');
    stock(r.cargo);assert(total(r.cargo)<=1,'care cargo capacity');if(r.resume)cell(r.resume);
    for(const k of MATERIALS)assert(r.cargo[k]===0||(r.kind==='rescue'?k==='medicine':r.kind==='eat'&&(k==='food'||k==='meal')),'care cargo purpose');
    if(r.facility!==null){assert(w.structures.some(s=>s.id===r.facility&&s.kind===(r.kind==='rest'?'bed':r.kind==='treatment'?'infirmary':''))&&!facilities.has(r.facility),'facility claim');facilities.add(r.facility);}
    if(r.patient!==null){assert(r.kind==='rescue'&&r.patient!==p.id&&w.people.some(a=>a.id===r.patient&&a.life!=='dead')&&!patients.has(r.patient),'patient claim');patients.add(r.patient);}else assert(r.kind!=='rescue','rescue patient');
    if(r.reservation){const c=r.reservation;object(c,['source','kind','count']);assert(c.count===1&&(r.kind==='rescue'?c.kind==='medicine':r.kind==='eat'&&['meal','food'].includes(c.kind))&&total(r.cargo)===0,'care supply claim');assert([...w.structures,...w.piles].some(s=>s.id===c.source)&&available(w,c.source,c.kind)>=0,'care supply reference');}
  }
  assert(w.people.reduce((n,p)=>n+p.body.meals,0)===w.statistics.eatenFood+w.statistics.eatenMeals,'meal count');
  assert(w.people.filter(p=>p.life==='dead').length===w.statistics.deaths,'death count');
  for(const k of MATERIALS){
    const generated=(k==='food'?w.statistics.harvests*NEEDS.harvestYield:k==='wood'?w.combat.lootWood:0)+Object.entries(RECIPES).reduce((n,[name,r])=>n+w.recipeCounts[name as keyof typeof RECIPES]*r.output[k],0);
    const consumed=(k==='food'?w.statistics.eatenFood:k==='meal'?w.statistics.eatenMeals:k==='medicine'?w.statistics.rescues+w.statistics.treatments:k==='ammo'?w.combat.stats.shots+w.combat.stats.traps:0)+Object.entries(RECIPES).reduce((n,[name,r])=>n+w.recipeCounts[name as keyof typeof RECIPES]*r.cost[k],0);
    assert(w.produced[k]===generated&&w.consumed[k]===consumed,'production/consumption ledger');
  }
}
